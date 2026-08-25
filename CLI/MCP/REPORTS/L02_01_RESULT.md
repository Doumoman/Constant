# L02_01_RESULT

## TASK

CLI/MCP/TASKS/L02_01.md (L02_01_ROUTE_CAMERA — LIVE02_01_CONSUME_ROUTE_AND_CAMERA_ROOM_TRANSITION_REQUESTS)

## STATUS

STATUS: PASS

## SUMMARY

라이브 방/카메라 계층을 완성했다: 플레이어 위치를 고정 스텝 샘플해 CHAR03_02 카메라룸 전환 정책(경계+준비 게이트+hysteresis 0.25/2샘플)에 위임하고, 전환 요청은 CHAR06_01 루트 정책(선언 엣지 검증)을 거쳐 세션 현재 방과 카메라 스냅에 반영한다. 수동 방 2개+양방향 선언 루트(L02_01 한정 임시 소스, L02_02 교체 가능 동일 계약)로 Play Mode 실기 스모크 4종을 전부 실측: 선언 경계 통과 1회 수락(A→B, 카메라 (18,4) 스냅), KEEP(전환 후에도 속도·입력 유지), 차단(미등록 방 게이트 차단 + 준비-미선언 방 UndeclaredRouteEdge 진단 거부 — 세션/카메라 무변경), 역방향(B→A 1회 수락, 카메라 (6,4) 복귀). 컴파일 0 에러, Character EditMode 177/177 유지.

## READ

- CLI/MCP/ENTRY.md~MASTER.md, LIVE_SRC/LIVE_LOCK, REPORTS/L00_02·L01_01·L01_02·L01_03 RESULT
- CharacterDesign REPORTS: CHAR03_02(정책 계약)·CHAR03_03·CHAR06_01·CHAR06_02·CHAR06_04, CHAR00_SOURCE_REGISTRY
- Character 런타임: RoomTransition 전문(CharacterCameraRoomTransitionPolicy 209줄 정독 — SetActiveRoom/Evaluate/Result/Settings.Default, 전환 시 활성 방 자동 갱신·경계당 요청 1회 보장), MapIntegration(게이트/브리지 TryGetTileCoordinate·GetCellOrigin), Integration(TryCreateRouteTransitionRequestForRooms), RunState
- Live 런타임 전체, 프리팹/씬, Map 런타임(ToWorld/WorldGenConstants), Packages, EditorBuildSettings(무변경 확인)

## CHANGED

- Assets/_Game/Scenes/Live/CharacterLiveTest.unity — FloorB(방 B 셀 커버, 12×1 @ (17.5,-0.5)) + RoomSystem 객체(수동 루트 소스·방 전환 드라이버·카메라 드라이버) 추가·배선

## CREATED

- Assets/_Game/Live/Runtime/Rooms/CharacterLiveRoomReadinessSource.cs — ICharacterRoomReadinessSource 데이터 등록부(판정 로직 없음 — CHAR03 게이트 소관)
- Assets/_Game/Live/Runtime/Rooms/CharacterLiveManualRouteSource.cs — L02_01 한정 임시 수동 방/루트 소스: 인접 방 2개 준비 등록 + 양방향 선언 엣지(BasicMovement) — MAP 생성 없음, L02_02 어댑터 교체 가능 동일 계약
- Assets/_Game/Live/Runtime/Rooms/CharacterLiveRoomCenterResolver.cs — 방 중심/앵커 타일 해석(ToWorld+GetCellOrigin+WorldGenConstants — 상수 복제 없음)
- Assets/_Game/Live/Runtime/Rooms/CharacterLiveRouteTransitionConsumer.cs — 전환 요청 소비자: CHAR06_01 루트 정책 위임 → 수락 시에만 세션 현재 방 갱신, 거부 시 진단 기록(AcceptedCount/RejectedCount/LastDiagnostic — L02_03·L04_01 감사 표면)
- Assets/_Game/Live/Runtime/Rooms/CharacterLiveRoomTransitionDriver.cs — 고정 스텝 위치 샘플→정책 평가→소비→카메라; 루트 거부 시 정책 활성 방을 세션 방으로 재정착(정책-세션 일관성); 초기 정착(스폰 방 앵커+카메라 초기 스냅)
- Assets/_Game/Live/Runtime/Camera/CharacterLiveCameraRoomDriver.cs — 수락 방 중심으로 카메라 결정적 스냅(z 유지, Cinemachine 불사용)
- (+ .meta)

## TESTS

- Character EditMode 기준선: **177/177 PASS** (6.62s) — 감소 없음
- MAP EditMode: 13,536 앵커 유지(MAP 무접촉)
- 신규 테스트 없음(Tests/** 금지 — PlayMode 파일은 L04_01 소관; 수동 Play Mode 스모크로 검증, 아래 실측)

## BUILD

- 본 과제 빌드 검증 없음(비요구) — 컴파일 클린(error CS 0). EditorBuildSettings 무변경

## LIVE_CONTRACTS_USED

- CharacterCameraRoomTransitionPolicy + CharacterRoomTransitionSettings.Default(hysteresis 0.25/2샘플 잠금) + CharacterRoomTransitionResult/Request/Decision(CHAR03_02)
- CharacterRoomBoundaryGate + ICharacterRoomReadinessSource(CHAR03_01 — 준비 판정 위임, 중복 구현 없음)
- CharacterRouteIntegrationPolicy.TryCreateRouteTransitionRequestForRooms + CharacterGeneratedRouteEdgeSnapshot + CharacterRouteBoundarySide/Requirement + CharacterIntegrationDiagnostic(CHAR06_01)
- CharacterMapCoordinateBridge(TryGetTileCoordinate — 정책 내부/GetCellOrigin), WorldCoordinateUtility.ToWorld + LocalTileCoord + WorldGenConstants(MAP 공용)
- CharacterLiveRunSession.UpdateCurrentRoom(L01_03 표면)

## REQUESTS_CONSUMED

- CharacterRoomTransitionRequest and CharacterGeneratedRouteTransitionRequest consumed by live route/camera layer. (안정화된 경계 통과 1건당 정확히 1회 — Play Mode 실측 accepted=2: A→B route 1, B→A route 2)
- No generated MAP adapter, carry, drop, throw, bomb, rope, damage, death, run failure, HUD, or presentation requests consumed.

## ASSETS_WIRED

- CharacterLiveMovementDriver/player position -> CharacterCameraRoomTransitionPolicy -> CharacterRoomTransitionRequest
- Manual route source -> CharacterRouteIntegrationPolicy -> CharacterGeneratedRouteTransitionRequest -> CharacterLiveRouteTransitionConsumer
- CharacterLiveRouteTransitionConsumer -> CharacterLiveRunSession.CurrentRoomId
- Accepted target room -> CharacterLiveCameraRoomDriver -> CharacterLiveTest camera
- No generated MAP adapter wiring

## MANUAL_VERIFICATION

Play Mode 실기 스모크(어댑터 직접 구동 — L01_03과 동일 방식):

- **초기 정착**: 런 시작 시 세션 방=A(0,0/0,0), 카메라 방 A 중심 (6,4,-10) 초기 스냅
- **선언 경계 통과**: 우측 달리기 → x=12 경계+margin 통과·2샘플 안정화 → 세션 방 **B(0,0/1,0)로 정확히 1회** 갱신(accepted=1, route 1), 카메라 **(18,4)** 스냅
- **KEEP**: 전환 순간 입력·속도 무변조 — 전환 후에도 계속 달려 vel.x=3.1 유지(공중 clamp) 실측; 정책 API 자체가 입력/속도를 받지 않아 구조 보장
- **차단(게이트)**: 방 B 너머 미등록 방들(x=24~92 다수 경계)을 물리적으로 통과해도 BlockedMissingRoom으로 세션·카메라 B 유지, accepted 불변
- **차단(미선언)**: 방 C를 준비 등록(루트 미선언) + 임시 바닥(플레이 한정, 씬 미저장) → 경계 통과 시 소비자가 `UndeclaredRouteEdge` 진단으로 거부 — 세션·카메라 B 유지. rejected=37은 미선언 방 체류 중 재정착→재평가 루프의 반복 진단(상태 일관 유지 — 실제 생성 맵에서는 선언 루트만 존재하므로 정상 소음, L02_03 감사 시 참고)
- **역방향**: 방 B에서 좌측 달리기 → B→A 1회 수락(accepted=2, route 2), 카메라 (6,4) 복귀 — hysteresis 재통과 확인
- 플레이어 텔레포트 없음(스모크용 재배치는 검증 스크립트가 수행한 것으로 전환 계층은 위치 무변조)

## REGRESSION_BASELINE

- Character EditMode 177/177 PASS (본 실행 재확인), MAP 13,536 앵커 유지, 컴파일 0 에러

## SCOPE_VALIDATION

- 변경: 씬 1건(배선). 생성: Rooms 5파일 + Camera 1파일(+.meta) — 전부 허용 경로
- 금지 경로 0건: Character/MAP 런타임, Live/Input(diff 0), 프리팹(diff 0 — 플레이어 로컬 샘플링 불필요 판단), Tests, Packages, ProjectSettings, MapDesign, CharacterDesign, Builds, Temp
- 후속 과제 미개방(L02_02 이후 LOCKED)

## FORBIDDEN_AUDIT

- 준비 판정 중복 구현 없음(게이트 위임), 정책 재작성 없음(상태ful 정책 원본 사용)
- 일반 전환에서 플레이어 텔레포트 없음, 입력/속도/버퍼/인벤토리/체력/런 카운트/세이브/오디오/애니메이션 무변조
- Cinemachine 불사용, 신규 ActionId 없음, basic attack/melee/shoot/dash/wall jump/double jump 없음
- MAP 생성/Tilemap 없음(수동 소스는 좌표·계약 데이터만)

## NEXT

Current Task after finalize: NONE
Next Task auto-opened: NO (`L02_02_MAP_ADAPTER`는 LOCKED 유지, 새 INBOX 패키지로만 개방 — 수동 시작/방/루트 소스 3종이 전부 동일 계약 교체 지점으로 준비됨)
