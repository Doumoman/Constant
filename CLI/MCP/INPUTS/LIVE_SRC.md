# Live Integration Source Registry

```text
REGISTRY_STATE: FILLED_BY_L00_01
OWNER_TASK: L00_01_SURVEY
```

## Unity

```text
Unity version: 6000.3.8f1
Active build target: StandaloneWindows64 (Standalone, development=false)
Input System package: com.unity.inputsystem 1.18.0
Test framework: com.unity.test-framework 1.6.0
URP: com.unity.render-pipelines.universal 17.3.0
```

## Build Scenes (EditorBuildSettings, 10개 — 전부 Legacy)

```text
Assets/_Legacy/_Game/Scenes/00_Boot.unity
Assets/_Legacy/_Game/Scenes/01_Title.unity
Assets/_Legacy/_Game/Scenes/02_RunShell.unity
Assets/_Legacy/_Game/Scenes/10_Prologue_0_1.unity
Assets/_Legacy/StarNight/Scenes/Game/P5_MoonPalace_1-1_CraterWorkshop.unity
Assets/_Legacy/StarNight/Scenes/Game/P10_MoonPalace_FirstBranches.unity
Assets/_Legacy/StarNight/Scenes/Game/P11_FullJourney_CommonEndings.unity
Assets/_Legacy/StarNight/Scenes/Game/P12_StarlessSea_DawnVoyage.unity
Assets/_Legacy/_Game/Scenes/11_Moon_1_1.unity
Assets/_Legacy/_Game/Scenes/99_GridLab.unity
```

## Scene Candidates (live player integration)

- 활성 씬 1개뿐: `Assets/_Game/Scenes/MapGenerationProgressTest.unity` — MAP 생성 진행 진단 전용(MapGenerationProgressSceneAdapter). 플레이어 통합용 아님
- Legacy 씬 19개는 컴파일 제외 생태계(LEGACY_DISABLED) 위에 있어 부적합 — 읽기 전용 선례
- **권고: 신규 라이브 씬 생성** — 제안 위치 `Assets/_Game/Scenes/Live/` (예: `CharacterLiveTest.unity`), 카메라+라이트+부트스트랩 1객체 구성

## Prefab Candidates (player, camera, HUD, map, run bootstrap)

- `Assets/_Game/**` 내 프리팹 **0개** — 전부 신규 제작 대상
- Legacy 프리팹 226개(플레이어/오브젝트/UI 선례 포함, `Assets/_Legacy/StarNight/**`, `Assets/_Legacy/_Game/**`) — 읽기 전용 참고만
- 에셋팩(`Assets/2D Fantasy sprite bundle/**`) 장식 프리팹 — 아트 소스 후보(로직 없음)
- **권고: 신규 프리팹 루트** — 제안 위치 `Assets/_Game/Live/Prefabs/` (Player, CameraRig, RunBootstrap, Hud)

## Input Surfaces

- `.inputactions` 자산: Legacy 2본뿐(`Assets/_Legacy/StarNight/Input/StarNightControls.inputactions`, `Assets/_Legacy/_Game/Interaction/Data/Resources/Input/StarNightControls.inputactions`) — Interact=E/UseBomb=F/UseRope=Q로 잠금 의미와 불일치, 컴파일 제외 선례(잠금 규칙 명시)
- Input System 1.18.0 설치·활성 — 신규 자산 제작 가능
- **권고: 신규 `.inputactions` 생성** — 제안 위치 `Assets/_Game/Live/Input/CharacterLiveControls.inputactions`, 잠금 바인딩 그대로: Move=수평 축, Down=하강 축, Jump=Space, Action=X, Bomb=Z, Rope=C
- 소비 지점: `StarNight.Character.Input.CharacterInputSnapshot`/`CharacterButtonSnapshot`/`CharacterInputBuffer` (잠금 ActionId 5종 {Jump, Action, SafeDrop, Bomb, Rope} — 신규 ActionId 금지)

## Runtime Bootstrap Candidates

- 활성 MonoBehaviour는 MAP 진단 4개뿐(BiomePatchOverlay, MapGenerationProgressSceneAdapter, SiteReservationOverlay, WorldTopologyOverlay) — 게임플레이 부트스트랩 부재
- Character 런타임 13모듈 133파일은 전부 순수(비-MonoBehaviour) — 구성(조립) 대상
- 기존 라이브 어댑터 1개: `StarNight.Character.Movement.UnityPhysics2DCharacterCollisionWorld` (ICharacterCollisionWorld의 Physics2D 질의 구현, CHAR01 승인) — 부트스트랩에서 주입
- **권고: 신규 부트스트랩 MonoBehaviour** — 제안 위치 `Assets/_Game/Live/Runtime/` + 신규 asmdef `Game.Character.Live` (references: Game.Character.Runtime, Game.Map.Runtime, InputSystem) — 순수 정책 재작성 금지, 조립만

## Generated MAP Output / Snapshot Adapter Candidates

- MAP 런타임에 "생성 월드 전체를 반환하는 파사드 파이프라인" **부재** — 오케스트레이션은 테스트 어셈블리(Map03~05 ExitTests의 FullPipeline/FullBatch)에만 존재
- 사용 가능한 공용 도메인 표면: `WorldTileCoord`/`SectorCoord`/`MicroChunkCoord`/`WorldCoordinateUtility`/`WorldGenConstants`, `MicrochunkDefinition`/`MicrochunkTileCell`/`MicrochunkTileLayer(Occupancy)`/`MicrochunkTransformer`/각 Validator, `MoonpalaceBoundaryCandidateIndex(er)`, `MicrochunkDefinitions`(Data)
- 캐릭터측 수신 계약(완비): `CharacterGeneratedRunSnapshot`(+Room/Microchunk/Item 스냅샷), `CharacterGeneratedMapStartSnapshot`, `CharacterGeneratedRouteEdgeSnapshot`, `ICharacterMapWorldQuery`, `ICharacterRoomReadinessSource`
- **L02_02 어댑터 방향**: MAP 도메인 표면 → 캐릭터 스냅샷 투영 + `ICharacterMapWorldQuery`/`ICharacterRoomReadinessSource` 라이브 구현. MAP측 파사드가 필요해지면 MAP 하니스 CHANGE CONTROL 소관(캐릭터측에서 MAP 생성 로직 복제 금지)

## Request Consumer Insertion Candidates (전부 캐릭터 완비 계약, 소비자만 부재)

```text
스폰: CharacterPlayerSpawnRequest (Integration)
루트/카메라: CharacterGeneratedRouteTransitionRequest, CharacterCameraRoomTransitionPolicy/Decision (RoomTransition)
이동: CharacterGroundMotor/AirControl/Gravity/Jump/Landing + UnityPhysics2DCharacterCollisionWorld
휴대/투척: CharacterCarry*/Throw* (Interaction), CharacterObjectStopRequest/Stomp/Impact (Combat)
폭탄: CharacterBombPlacementRequest/SpendRequest/CharacterBombFuse/CharacterExplosionRequest/CharacterTerrainMutationRequest
로프: CharacterRopePlacementRequest/SpendRequest/CharacterRopeSegmentRequest/CharacterRopeClimbMotorRequest
생존: CharacterSurvivalDamageRequest/CharacterDeathRequest/CharacterRunFailureRequest (+어댑터 5종)
런 상태: CharacterRunState/CharacterRunInventoryPolicy
연출: CharacterPresentationEventRequest + CharacterPresentationBridge.NormalizeBatch
```

## HUD / Presentation Binding Candidates

- 데이터 소스 완비: `CharacterHudSnapshot.FromRunState` {체력/최대/무적/폭탄/로프/런 상태/복귀 토큰}
- 활성 UI 자산 0개 — Legacy UI(`Assets/_Legacy/StarNight/Scripts/Runtime/UI/**`, `Assets/_Legacy/_Game/UI/**`)는 읽기 전용 선례
- **권고: 신규 uGUI Canvas 프리팹** — 제안 위치 `Assets/_Game/Live/Prefabs/Hud.prefab` + 바인더 MonoBehaviour(스냅샷 폴링, 연출 이벤트 소비)

## PlayMode Test / Test Scene Candidates

- 기존 PlayMode: `Game.Map.Tests.PlayMode` — 테스트 .cs 0개(빈 asmdef, Legacy `Game.Stage.Runtime` 참조는 실존 어셈블리)
- Character PlayMode asmdef **부재** — L04_01에서 신규 `Assets/_Game/Tests/PlayMode/Character/` + `Game.Character.Tests.PlayMode.asmdef` 필요
- EditMode 기준선: Game.Character.Tests.EditMode 177 / Game.Map.Tests.EditMode 13,536 (전건 PASS, CHAR06_03)

## Recommended Path Tokens for L00_02

```text
LIVE_RUNTIME: Assets/_Game/Live/Runtime/**        (부트스트랩·소비자·어댑터 MonoBehaviour + Game.Character.Live.asmdef)
LIVE_INPUT: Assets/_Game/Live/Input/**            (신규 .inputactions)
LIVE_PREFABS: Assets/_Game/Live/Prefabs/**        (Player/CameraRig/RunBootstrap/Hud)
LIVE_SCENES: Assets/_Game/Scenes/Live/**          (CharacterLiveTest.unity 등)
LIVE_PLAYMODE: Assets/_Game/Tests/PlayMode/Character/**
READONLY_PRECEDENT: Assets/_Legacy/** (읽기 전용), Assets/2D Fantasy sprite bundle/** (아트 소스)
FORBIDDEN_KEEP: Assets/_Game/Character/Runtime/** 재작성 금지(순수 계약 잠금), Assets/_Game/Map/Runtime/** 수정 금지
```

## Blockers / Missing Surfaces

- 차단 없음(BLOCKED 사유 아님). 결손 표면(전부 신규 제작 대상으로 계획됨): 라이브 씬·플레이어/카메라/HUD 프리팹·활성 .inputactions·부트스트랩·MAP 생성 파사드(어댑터로 대체 가능)·Character PlayMode asmdef
- 주의 1: MCP 브리지(unity-mcp) 소켓이 10분+ 장시간 실행 중 사망하며 에러 로그를 주입하는 도구 이슈(CHAR06_03 기록) — L04_01 PlayMode 장기 실행 시 재현 가능, 실행 중 폴링 자제 완화책 유효
- 주의 2: EditorBuildSettings 씬 10개가 전부 Legacy — L04_02 빌드 검증 시 라이브 씬 추가/구성 결정 필요(ProjectSettings 쓰기 허용이 그 과제 allowlist에 있어야 함)

## Pre-existing Dirty Files

- 없음 — 직전 커밋(`c2e21b5`)까지 작업 트리 클린. 현재 더러움은 본 하니스 적용분(CLI/MCP/MASTER.md, STATUS.md, TASKS/L00_01.md)뿐
```
