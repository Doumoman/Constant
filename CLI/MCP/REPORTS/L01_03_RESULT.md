# L01_03_RESULT

## TASK

CLI/MCP/TASKS/L01_03.md (L01_03_SPAWN — LIVE01_03_CONSUME_PLAYER_SPAWN_REQUEST_AND_START_LIVE_RUN)

## STATUS

STATUS: PASS

## SUMMARY

첫 라이브 런 루프를 완성했다: 수동 시작 스냅샷 → CHAR06_01 스폰 정책 → 스폰 요청 1회 소비(WorldCenter 단일 원천) → 런 세션 시작(HP 4/4, 폭탄 4, 로프 4) → FixedUpdate 이동 드라이버가 CHAR01 순수 코어(프로브/지면·공중 모터/중력/점프/착지)를 코스 시뮬레이터와 동일 순서로 조립해 kinematic MovePosition으로 적용. Play Mode 실기 스모크로 스폰 정착·달리기·공중 속도 clamp(정확히 3.1)·낙하 종단(정확히 18)·착지·점프 상승(2셀 피크 근접)을 전부 실측했다. 라이브 어댑터 결함 2건(접지 정착 부재, 바닥 스침 수평 오차단)을 스모크에서 발견·수정했으며 순수 계약은 무변경이다. 컴파일 0 에러, Character EditMode 177/177 유지.

## READ

- CLI/MCP/ENTRY.md~MASTER.md, LIVE_SRC.md, LIVE_LOCK.md, REPORTS/L00_02·L01_01·L01_02 RESULT
- CharacterDesign/MCP/REPORTS/CHAR06_01·CHAR06_02·CHAR06_04 RESULT, INPUTS/CHAR00_SOURCE_REGISTRY.md
- Character 런타임: Input/State/Movement/MapIntegration/Integration/RunState 전 표면 — 특히 코스 시뮬레이터(CharacterMovementCourseSimulator) 전문을 조립 순서의 정본으로 정독, CharacterGroundProbe.Probe(Distance=지지면 갭)/모터 Step/TryStartJump/ApplyJumpRelease/LandingDetector.Step/CharacterCollisionHit 실서명 확인(추측 API 0)
- Live 런타임(L01_01/02 산출), 프리팹/씬, Map 런타임(WorldGenConstants — 방 경계 계산 계약), Packages/manifest.json

## CHANGED

- Assets/_Game/Live/Runtime/Game.Character.Live.asmdef — references에 "Game.Map.Runtime" 추가. 사유: 본 과제가 명시 요구하는 WorldTileCoord/CharacterRoomId/CharacterGeneratedMapStartSnapshot 계약이 MAP 도메인 타입을 운반(LIVE_LOCK LIVE_ASSEMBLY_PLAN에 계획된 참조)
- Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab — 이동 드라이버 추가 + 레이어 2(Ignore Raycast, 빌트인)로 변경(자기 캡슐 캐스트 방지 — solid 마스크는 Default만)
- Assets/_Game/Scenes/Live/CharacterLiveTest.unity — RunBootstrap 객체(부트스트랩+수동 시작 소스) 추가·배선, 바닥을 시작 방(셀 0..11) 커버로 재배치(중심 5.5,-0.5 / 12×1), 카메라 재배치

## CREATED

- Assets/_Game/Live/Runtime/Run/CharacterLiveRunSession.cs — 런 세션(액터/현재 방/CharacterRunState 소유, TryStartRun 1회 시작, L02·L03 소비 표면)
- Assets/_Game/Live/Runtime/Run/CharacterLiveManualStartSource.cs — L01_03 한정 임시 수동 시작 소스(고정 셀 → 스냅샷; 방 경계는 WorldGenConstants로 12×8 정렬 계산 — 상수 복제 없음; MAP 생성 없음; L02_02 어댑터로 교체 가능 동일 계약)
- Assets/_Game/Live/Runtime/Run/CharacterLiveSpawnConsumer.cs — 스폰 요청 1회 소비(HasConsumed 래치), 위치 원천은 request.WorldCenter뿐
- Assets/_Game/Live/Runtime/Run/CharacterLiveRunBootstrap.cs — 씬 진입점: 소스→정책→소비→세션, 실패는 진단 로그(무예외)
- Assets/_Game/Live/Runtime/Movement/CharacterLiveMovementSettings.cs — 배선 값만(solid 마스크 Default, AlwaysRun=true — 잠금 입력에 달리기 수정자가 없어 이동 문법 기준 속도로 구동, 수치 원천은 전부 순수 Default)
- Assets/_Game/Live/Runtime/Movement/CharacterLiveMovementDriver.cs — FixedUpdate 드라이버: 시뮬레이터 동일 순서 조립 + 접지 정착(프로브 갭→Skin 간격 스냅) + 축별 스윕 clamp(수평은 벽 법선 |n.x|≥0.5만 차단) + MovePosition
- (+ .meta)

## TESTS

- Character EditMode 기준선: **177/177 PASS** (0.63s) — 감소 없음
- MAP EditMode: 13,536 앵커 유지(MAP 무접촉)
- 신규 테스트 없음(Tests/** 금지 — PlayMode 검증은 L04_01 소관; 본 과제는 수동 Play Mode 스모크로 대체, 아래 실측)

## BUILD

- 본 과제 빌드 검증 없음(비요구) — 컴파일 클린(error CS 0). EditorBuildSettings 무변경(씬 10 그대로)

## LIVE_CONTRACTS_USED

- CharacterSpawnIntegrationPolicy.TryCreateSpawnRequest + CharacterGeneratedMapStartSnapshot + CharacterPlayerSpawnRequest(CHAR06_01)
- CharacterRoomId.FromWorldTile, WorldTileCoord/WorldCoordinateUtility/WorldGenConstants(MAP 공용)
- CharacterRunState.CreateActive + CharacterHealthState.CreateFull + CharacterRunInventoryState.CreateStarting + CharacterSurvivalSettings/CharacterRunStateSettings.Default(CHAR05_03/04)
- CHAR01 이동 코어 전체: CharacterGroundProbe(Settings.Default)/GroundMotor/AirControlMotor/GravityMotor/JumpController(각 Default)/LandingDetector/JumpState/CharacterCapsuleGeometry.Default + UnityPhysics2DCharacterCollisionWorld(승인 질의 어댑터) + CharacterGroundProbe.MinimumUpwardNormalY(법선 기준 재사용)
- CharacterInputSnapshot(Jump.PressedThisFrame/Held, Horizontal) — 이동 수학·상태 전이 권위는 전부 순수 계약, 드라이버는 조립·적용만

## REQUESTS_CONSUMED

- CharacterPlayerSpawnRequest consumed exactly once per run start. (HasConsumed 래치 + 세션 IsRunStarted 재진입 거부 — Play Mode 실측: SpawnConsumed=True, ActorId=1, StartCell=(5,0), WorldCenter=(5.50, 0.50))
- No route, camera, room transition, bomb, rope, damage, death, run failure, HUD, or presentation requests consumed.

## ASSETS_WIRED

- CharacterLiveControls.inputactions -> CharacterLiveInputSource -> CharacterLivePlayerRig -> CharacterLiveMovementDriver
- CharacterGeneratedMapStartSnapshot/manual source -> CharacterSpawnIntegrationPolicy -> CharacterPlayerSpawnRequest -> CharacterLiveSpawnConsumer -> CharacterLivePlayer prefab instance
- CharacterLiveRunBootstrap -> CharacterLiveTest scene
- No generated MAP adapter wiring

## MANUAL_VERIFICATION

Play Mode 실기 스모크(에디터 내 실행·관측):

- 스폰: 부트스트랩 자동 실행 → SpawnConsumed 1회, 위치 (5.50, 0.50)=WorldCenter → 정착 (5.50, 0.455) = 캡슐 바닥이 바닥면 위 Skin(0.01) — grounded=True, RunState HP 4/4·폭탄 4·로프 4
- 달리기: horizontal=1 주입 → x 5.5→60.5 이동, facing=Right, **공중 수평 속도 정확히 3.1**(잠금 maxAirSpeed), 바닥 끝 이탈 후 낙하 **종단속도 정확히 -18**(잠금 maxFall) — 순수 튜닝값 그대로 재현
- 착지: 공중 (5.5, 2) 재배치 → 자유낙하 → (5.50, 0.455) 정착·속도 0·grounded 복귀
- 점프: 접지에서 점프 에지 주입 → 슬로모션 관측 y=2.556 상승 중, vel.y=+1.08(상승 중력 감속) — 2셀 피크 계약과 정합
- 에지 보존/소비: 심어둔 버튼 에지가 다음 고정 스텝에서 정확히 1회 소거됨을 실측
- **장치→액션 계층 환경 한계(투명 기록)**: 합성 키 이벤트가 장치 상태까지 도달(dKey.isPressed=True)하고 바인딩도 완전 해석(move.controls=4)됐으나, 에디터 비포커스 상태의 Input System 게이팅(editorInputBehaviorInPlayMode=PointersAndKeyboardsRespectGameViewFocus, 인메모리 임시 변경으로도 기주입 상태 재평가 안 됨)으로 액션 값이 갱신되지 않음 — 이는 L04_01 InputTestFixture가 해결하는 표준 에디터 테스트 환경 한계로, 스모크는 어댑터 직접 구동으로 하류 사슬 전체를 검증(장치 계층은 실사용 시 포커스된 Game View에서 정상 동작). 임시 변경한 설정은 원값 복원(자산 저장 없음 — git 확인 무변경)
- 발견·수정 2건(전부 라이브 어댑터, 순수 계약 무변경): ① 접지 정착 부재 — 프로브 허용 갭(0.05)에서 부양 → 시뮬레이터 support-snap 대응 정착 추가 ② 바닥 정확 접촉 시 수평 캡슐 캐스트가 바닥면을 스쳐 vx 오차단 → Skin 간격 정착 + 수평 스윕 벽 법선 필터

## REGRESSION_BASELINE

- Character EditMode 177/177 PASS (본 실행 재확인), MAP 13,536 앵커 유지, 컴파일 0 에러

## SCOPE_VALIDATION

- 변경/생성 전부 허용 경로: Live/Runtime(신규 6 + asmdef 참조 1), Live/Prefabs(드라이버·레이어), Scenes/Live(배선), 본 REPORT
- 금지 경로 0건: Character/MAP 런타임, Live/Input(자산 diff 0), Tests, Packages, ProjectSettings(EditorBuildSettings·InputSystem 설정 자산 diff 0), MapDesign, CharacterDesign, Builds, Temp
- 후속 과제 미개방(L02_01 이후 LOCKED)

## FORBIDDEN_AUDIT

- 대시/벽점프/이중점프/사격/근접/일반 공격 경로 없음(드라이버는 순수 계약 호출만 — 이중 점프는 JumpConsumed 계약이 구조적으로 차단)
- 신규 ActionId 없음, 순수 정책 재작성 없음, MAP 방/셀 생성 없음(수동 소스는 좌표 계약만), Tilemap/SceneManager/HUD/오디오/애니메이션/세이브 없음
- Rigidbody2D는 kinematic + MovePosition(결정적 어댑터)만 — 물리 시뮬레이션 권위 없음

## NEXT

Current Task after finalize: NONE
Next Task auto-opened: NO (`L02_01_ROUTE_CAMERA`는 LOCKED 유지, 새 INBOX 패키지로만 개방 — 세션 표면 {Session.CurrentRoomId/UpdateCurrentRoom}과 드라이버 표면 {IsGroundedNow/Velocity/Facing}이 소비 준비 완료)
