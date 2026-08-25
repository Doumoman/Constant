# L04_01_RESULT

## TASK

CLI/MCP/TASKS/L04_01.md (L04_01_PLAYMODE — LIVE04_01_CREATE_PLAYMODE_KEYBOARD_AND_GENERATED_RUN_SMOKE)

## STATUS

STATUS: PASS

## SUMMARY

라이브 통합 스택(키보드 입력→생성 런→루트/카메라→도구→HUD/피드백)을 검증하는 PlayMode 스모크 스위트를 `Assets/_Game/Tests/PlayMode/Character/`에 신설했다(asmdef+테스트 4파일, **9 테스트 전부 실행·PASS**). 씬 부트는 실제 CharacterLiveTest 씬을 LoadSceneInPlayMode로 로드해 부트스트랩/세션/리그/무브먼트/방·카메라 드라이버/HUD 바인더 기동과 HUD 실데이터 표시를 실측하고, 키보드는 InputTestFixture 합성 장치로 잠금 바인딩 6종이 CharacterLiveInputSource/Adapter를 거쳐 잠금 CharacterActionId(SafeDrop 조합 우선 포함)로 흐르는지 검증한다(레거시 E/F/Q 무반응 포함). 생성 런은 공용 MAP 계약으로 12×8 정의를 조립해 L02_02 어댑터로 투영, 시작 스냅샷으로 런을 시작해 투영 루트/준비 소스로 A→B 전환(경계+hysteresis 1회 수락, 카메라 (18,4) 스냅, 플레이어 무텔레포트, 미생성 셀 false)을 실측한다. 도구/HUD는 L03 소비자·연출 정확히 1회 소비/중복 무변조/명령 데이터 유지/우선순위 피드백을 재검증한다. 제품 코드 무수정(테스트 전용 더블만), 컴파일 0 에러, EditMode 177/177 유지. 초기 실패 2건은 테스트 코드의 Input System 이벤트 시퀀싱 문제로 진단·수정했다(제품 결함 아님).

## READ

- CLI/MCP/ENTRY.md~MASTER.md, INPUTS/LIVE_SRC·LIVE_LOCK, REPORTS/L00_02·L01_01·L01_02·L01_03·L02_03·L03_01·L03_02 RESULT
- CharacterDesign REPORTS: CHAR05_05·CHAR06_04
- Live 전체(Input 자산 JSON 정독 — Move Value/Axis+컴포지트 극성 A/D·화살표, 액션 6종만; Runtime Input/Run/Movement/Rooms/Adapters/Tools/Hud/Presentation; Prefabs; Scenes), Character 런타임(Input: CharacterActionId 잠금 5종·CharacterInputSnapshot.IsPressedThisFrame·SafeDrop 우선 규칙, Integration/RoomTransition/RunState), MAP 런타임(MicrochunkDefinition 19-인자 ctor·96셀 완전성·MicrochunkTileCell 9-코드 ctor·MicrochunkTransform R0, WorldCoordinateUtility)
- 기존 테스트 트리: Game.Character.Tests.EditMode asmdef·Game.Map.Tests.PlayMode asmdef(optionalUnityReferences TestAssemblies 패턴), Unity.InputSystem.TestFramework 가용 확인
- Packages/manifest.json, ProjectSettings/ProjectSettings.asset (읽기)

## CHANGED

- 없음 (기존 파일 수정 0건 — 기존 asmdef 갱신도 불필요)

## CREATED

`Assets/_Game/Tests/PlayMode/Character/` (신규 5파일 + .meta):

- Game.Character.Live.PlayMode.Tests.asmdef — 로컬 Map PlayMode 패턴(optionalUnityReferences TestAssemblies); references [Character.Runtime, Map.Runtime, Character.Live, Character.Live.Hud, Unity.InputSystem, Unity.InputSystem.TestFramework, UnityEngine.UI]
- CharacterLiveScenePlayModeTests.cs — 씬 부트 스모크 1종(+격리 TearDown: 씬 루트 정리)
- CharacterLiveInputPlayModeTests.cs — InputTestFixture 상속, 키보드 스모크 3종
- CharacterLiveGeneratedRunPlayModeTests.cs — 생성 런 스모크 2종(공용 계약 조립 픽스처)
- CharacterLiveToolsHudPlayModeTests.cs — 도구/HUD 스모크 3종(테스트 전용 더블: FakeCarryTarget/FakePlacementQuery)

## TESTS

- **신규 PlayMode: 9/9 PASS** (0.92s, failed 0/skipped 0, resultState Passed — 컴파일만이 아니라 실행 실측)
- Character EditMode 기준선: **177/177 PASS** (6.58s) — 감소 없음
- MAP EditMode: 13,536 앵커 유지(MAP 무접촉)

## BUILD

- 본 과제 빌드 없음(비요구 — L04_02 소관) — 컴파일 클린(error CS 0)

## PLAYMODE_RESULTS

| 픽스처 | 테스트 | 결과 |
|---|---|---|
| ScenePlayMode | SceneBoot_LiveStackStarts_AndHudBindsRealData | PASS |
| InputPlayMode | MoveAndDown_AreAxisStyleActions | PASS |
| InputPlayMode | Keyboard_MoveAxis_FlowsThroughAdapter | PASS |
| InputPlayMode | Keyboard_Buttons_MapToLockedActionIds | PASS |
| GeneratedRunPlayMode | Projection_IsUsable_AndUngeneratedCellsBlocked | PASS |
| GeneratedRunPlayMode | GeneratedRun_RoutesAToB_CameraMoves_PlayerNotTeleported | PASS |
| ToolsHudPlayMode | Carry_AcceptOnce_DuplicateAndInvalidDoNotMutate | PASS |
| ToolsHudPlayMode | BombAndRope_SpendAndQueueExactlyOnce_CommandDataOnly | PASS |
| ToolsHudPlayMode | HudSnapshot_ProjectsRunData_AndPresentationFeedbackOnce | PASS |

수정 이력(투명 보고): 최초 전체 실행 9개 중 입력 2건 실패 → 라벨 부착 재실행으로 실패 지점 특정 — (1) Move 축: 방향 전환 시 해제·프레임 경계 없이 연속 합성 이벤트를 보내 컴포지트 판별이 이전 방향을 유지, (2) 연속 Release 2건 중 S 해제 미반영으로 DownHeld 잔존. 둘 다 **테스트 코드의 이벤트 시퀀싱 문제**(제품 코드·자산 결함 아님 — 컴포지트 극성 A/D·화살표 정상 실측)로, 해제/입력 사이 프레임 경계 삽입 + 기준선 assert 추가로 수정 → 3/3 → 전체 9/9 PASS.

## SCENE_BOOT_SMOKE

- 실제 씬 LoadSceneInPlayMode(빌드 설정 무변경 경로) → 5프레임 내 기동 실측:
- RunBootstrap.IsRunStarted=true + Session.IsSpawnConsumed, PlayerRig.IsBound, MovementDriver.IsDriving, RoomTransitionDriver 존재, CameraRoomDriver.HasCameraRoom(초기 정착)
- HUD 바인더 **정확히 1개** + bootstrap/로그/소비자 참조 유효(깨진 직렬화 참조 없음)
- HUD 실데이터: HP 4/4 · BOMB 4 · ROPE 4 · ROOM S0,0 C0,0 · RUN Active · 피드백 빈 문자열
- 연출/HUD 소비 후 플레이어 Body.position 무변조 실측

## KEYBOARD_INPUT_SMOKE

- 자산 구조: Move=Value/Axis(잠금 축 유지), Down/Jump/Action/Bomb/Rope=Button, **액션 정확히 6종**
- 축: D=+1, A=-1, →=+1, ←=-1(양 컴포지트), S·↓=DownHeld(수평 0)
- 버튼→잠금 ActionId: Space→Jump(눌림 에지 1회 소비 후 소거·held 유지·해제 에지), X→Action(단독), **S+X→SafeDrop 우선(단독 Action 억제 — 잠금 조합 규칙)**, Z→Bomb, C→Rope
- 레거시 금지 키 E/F/Q: 축·Down·행동 4종 전부 무반응 실측

## GENERATED_RUN_SMOKE

- 공용 MAP 계약 픽스처: MicrochunkDefinition(12×8, 96셀, 바닥 G1/나머지 NONE) 2배치(청크 (0,0)/(1,0)), 선언 루트 A→B(BasicMovement), 시작 (5,1)
- 투영: 어댑터 진단 0, ValidationResult.Passed=true, IsUsable=true, 방 2, 시작 성립
- 월드 질의: 바닥 (5,0)=IsSolid, 생성-빈 (5,3)=IsEmpty, **미생성 (40,3)=false**(통과 공간 아님), 미배치 방 준비 없음(게이트 차단 경로)
- 런 시작: 투영 Start 스냅샷→CHAR06_01 스폰 정책→세션 시작
- A→B 전환: 경계(x=12)+margin 통과 위치에서 Evaluate 4회 → **요청 정확히 1회**(hysteresis 2샘플+경계당 1회 보장) → 투영 루트/준비 소스로 소비 수락(route 1) → 세션 방 B 갱신
- 카메라 A 중심 (6,4,-10) → B 중심 **(18,4,-10)** 스냅 실측, **플레이어 위치 무변조**(전환 전후 동일)

## TOOL_CONSUMER_SMOKE

- 휴대: 혼합 후보(적격/과대/이미 휴대됨)에서 적격 1개만 부착 1회; 중복 요청 무변조; AlreadyCarrying/TargetAlreadyCarried/InvalidCarryTarget/NoCarryTarget 진단; 투척 초기 속도 (7,0)=계약 방향×속력, 중복 투척 무해제; 드롭 1회·빈 슬롯 거부·**막힌 목적지 슬롯 유지**
- 폭탄: 소모 4→3→0 정확(수락당 1회), 중복/미생성·고체 셀 무소모, 퓨즈 2.5s 폭발 **정확히 1회**, 지형 명령 큐 1건에 **파괴 가능 변경 요청 2건(DestroyBreakable 의도)** — 명령 데이터로만 유지(Tilemap/씬 무변조), 재고 소진 NoBombStock, sink 부재 MissingTerrainSink
- 로프: 세그먼트 6(최대)/1(고체 중단) 정확, 소모 정확, 중복·차단·미생성·재고·sink 진단 전부 무변조, 명령 payload(세그먼트 (8,1)부터) 검증

## HUD_FEEDBACK_SMOKE

- 스냅샷: 세션 부재 안정 빈 값(NO RUN/-) → 시작 후 4/4·4·4·Active·S0,0 C0,0 결정적 투영
- 연출: 4건 배치(중복 1 포함) → 수락 3, 우선순위 순서(RUN FAILURE→DAMAGE -1→BOMB PLACED), **중복 이벤트 피드백 1건**, 스냅샷 LatestFeedback 반영
- 씬 실기: 중복 포함 배치 → 수락 1/중복 1/로그 1건 → 다음 프레임 FeedbackText "ROPE PLACED (5,1)" — 오디오/Animator/세이브/씬 로드 호출 없음(테스트도 해당 API 미사용)

## CONSOLE_AUDIT

- 예상외 에러 0건 — 전체 실행 후 에러 채널에는 테스트 러너의 "Saving results to: .../TestResults.xml" 알림 2건뿐(러너 정상 출력, 예외/컴파일 에러 아님). PlayMode 테스트는 예상외 에러 로그 발생 시 자체 실패하므로 9/9 PASS 자체가 무에러 증거

## REGRESSION_BASELINE

- Character EditMode 177/177 PASS(본 실행, 6.58s), 신규 PlayMode 9/9 PASS, MAP 13,536 앵커 유지, 컴파일 0 에러

## SCOPE_VALIDATION

- 생성: Tests/PlayMode/Character/ 5파일(+.meta)뿐 — 허용 경로와 정확히 일치(git status 실측)
- 제품 무변조: Character/MAP/Live 런타임, 씬, 프리팹, 입력 자산, Packages, ProjectSettings, 빌드 출력 전부 무접촉 — 테스트 통과를 위한 제품 훅 추가 0건
- 후속 과제 미개방(L04_02 LOCKED 유지)

## FORBIDDEN_AUDIT

- 제품 런타임/씬/프리팹/입력 자산/패키지/프로젝트 설정/세이브/오디오/애니메이션 편집 0건
- 테스트는 기존 공개 표면만 소비(SerializedObject는 테스트 GO 자체 배선에만 사용), EditorBuildSettings 무접촉(LoadSceneInPlayMode 경로)
- 신규 ActionId 없음(잠금 5종 검증만), 미래 과제 참조 없음

## NEXT

Current Task after finalize: NONE
Next Task auto-opened: NO (`L04_02_FINAL`은 LOCKED 유지, 새 INBOX 패키지로만 개방 — 빌드+최종 출구 감사만 남음)
