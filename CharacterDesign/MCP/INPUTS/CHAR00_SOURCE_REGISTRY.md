# CHAR00_SOURCE_REGISTRY

REGISTRY_STATE: FILLED_BY_CHAR00_01  
OWNER_TASK: CHAR00_01  
UPDATE_RULE: CHAR00_01만 이 파일을 실제 프로젝트 조사 결과로 갱신한다.

이 파일은 캐릭터 구현을 시작하기 전에 기존 프로젝트의 캐릭터·입력·물리·카메라·MAP 접점을 한곳에 고정하기 위한 조사표다.  
`UNKNOWN`은 아직 확인하지 못한 항목이고, `BLOCKER`는 다음 작업으로 넘어가기 전에 사용자 지시나 별도 패치가 필요한 항목이다.

## 작성 규칙

- 확인한 사실은 `CONFIRMED`로 기록한다.
- 파일 경로는 repo-relative 경로로 기록한다.
- 추측한 내용은 규칙으로 고정하지 않는다.
- 기존 프로젝트에 없는 시스템은 새로 만들지 말고 `UNKNOWN` 또는 `BLOCKER`로 둔다.
- MAP 접점은 `MapDesign/**` 문서와 현재 소스 양쪽을 확인한 뒤 기록한다.
- 이 파일을 채우는 작업은 `Assets/**`, `Packages/**`, `ProjectSettings/**`, `MapDesign/**`를 수정하지 않는다.

## 핵심 조사 결론 요약

1. 활성(컴파일되는) 게임 코드는 `Assets/_Game/**`의 MAP 생성 파이프라인뿐이다. 캐릭터 런타임은 현재 존재하지 않는다.
2. 기존 캐릭터 구현 전체는 `Assets/_Legacy/**`에 있으며 `.cs` 672개 전부(672/672) `#if LEGACY_DISABLED`로 감싸져 컴파일에서 제외된다. 어떤 asmdef도 `LEGACY_DISABLED`를 define하지 않는다. 즉 레거시는 읽기 전용 선례이며 실행 코드가 아니다.
3. 레거시 캐릭터 선례는 두 세대가 있다: 구세대 `Assets/_Legacy/_Game/**`(Game.Player/Interaction 등)와 신세대 `Assets/_Legacy/StarNight/**`(RW1 재작성). 신세대가 더 최근이고 완결적이다.
4. 잠금 입력 의미(X=행동, 아래+X=안전 내려놓기, Z=폭탄, C=로프)와 레거시 바인딩(E=상호작용, F=폭탄, Q=로프)은 불일치한다. 점프 장치 키의 유일한 프로젝트 선례는 Space다.
5. 캐릭터가 사용할 MAP 공용 좌표 계약(`WorldCoordinateUtility` 등)은 활성 코드에 존재한다. 그러나 캐릭터용 월드 질의(고체/위험), 지형 변경 요청, 경계 게이트, 방 준비 상태 API는 활성 코드에 아직 없다(부재 확인).

## 0. 조사 기준선

| 항목 | 값 |
|---|---|
| Unity version | CONFIRMED: 6000.3.8f1 (Unity MCP `Application.unityVersion`, productName `별을 물어오는 밤`, 조사 시 isCompiling=False) |
| Project root | CONFIRMED: `C:\Users\mp324\Documents\GitHub\Constant` (이하 경로는 repo-relative) |
| 조사 시점 branch/commit | CONFIRMED: `main` @ `24cb1b9` |
| 조사 전 dirty files | CONFIRMED: staged A 70개(`CharacterDesign/**` 하네스 설치분), M 2개(`ProjectSettings/Packages/dev.yarnspinner/Assembly-CSharp-generated.ysls.json`, `ProjectSettings/ShaderGraphSettings.asset` — 기존 사용자 변경, 본 작업에서 미접촉), untracked 1개(본 registry 초안) |
| 조사자 | CHAR00_01 |

## 1. Assembly / 패키지

| 항목 | 상태 | 경로 또는 값 | 비고 |
|---|---|---|---|
| Runtime asmdef (활성) | CONFIRMED | `Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef` | rootNamespace `StarNight.Map`, references `[]`, 순수 로직(UnityEngine.Tilemaps 참조 0건) |
| Runtime asmdef (캐릭터) | CONFIRMED | 부재 | 활성 캐릭터 어셈블리 없음. 신규 배치 위치는 §8 BLOCKER 참고 |
| Editor/Test asmdef (활성) | CONFIRMED | `Assets/_Game/Editor/MapAuthoring/MapAuthoring.Editor.asmdef`, `Assets/_Game/Editor/MapAuthoring/Tests/EditMode/MapAuthoring.Tests.EditMode.asmdef`, `Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef`, `Assets/_Game/Tests/PlayMode/Map/Game.Map.Tests.PlayMode.asmdef` | EditMode 테스트 .cs 77개. PlayMode asmdef는 테스트 0개이며 레거시 `Game.Stage.Runtime`를 참조하는 stale 상태(사실 기록만, 수정 안 함) |
| Legacy asmdef | CONFIRMED | `Assets/_Legacy/StarNight/Scripts/**`(StarNight.Runtime/Editor/Tests.EditMode/Tests.PlayMode), `Assets/_Legacy/_Game/**`(Game.Core/Player/Interaction/Stage/Narrative/UI/Tools/Integration/WorldObjects + Tests) | 전 코드 `#if LEGACY_DISABLED`. `StarNight.Runtime`은 autoReferenced=false, references: Unity.InputSystem, Unity.TextMeshPro, UnityEngine.UI, YarnSpinner.Unity |
| Input package | CONFIRMED | `com.unity.inputsystem` 1.18.0 (`Packages/manifest.json`), `ProjectSettings/ProjectSettings.asset` `activeInputHandler: 2`(Both) | 레거시는 New Input System 사용. `ProjectSettings/InputManager.asset`은 기본 axes + Debug axes만 존재 |
| Physics package or dependency | CONFIRMED | `com.unity.modules.physics2d` 1.0.0 | 레거시 플레이어는 Rigidbody2D 기반 |
| Test framework | CONFIRMED | `com.unity.test-framework` 1.6.0 | EditMode asmdef defineConstraints `UNITY_INCLUDE_TESTS` |

## 2. 캐릭터 런타임 소스 후보

전부 레거시(컴파일 제외) 선례다. 실행 중인 캐릭터 코드는 없다.

| 역할 | 상태 | 경로 | 현재 책임 | 다음 작업 영향 |
|---|---|---|---|---|
| Player root/controller | CONFIRMED(레거시) | `Assets/_Legacy/StarNight/Scripts/Runtime/Player/PlayerMotor2D.cs` | Rigidbody2D+CapsuleCollider2D 요구, FixedUpdate에서 linearVelocity 직접 구동, 수동 중력, ControlLock | CHARACTER_RUNTIME_READ_PATHS |
| Movement motor | CONFIRMED(레거시) | 위와 동일 + `JumpGraceState.cs`(coyote 0.10s/buffer 0.12s, 지상 게이트로만 점프 소비 — 이중 점프 없음), `P1MovementTuning.cs` | 이동·점프 튜닝 SO | CHARACTER_RUNTIME_READ_PATHS |
| Input reader | CONFIRMED(레거시) | `Assets/_Legacy/StarNight/Scripts/Runtime/Player/PlayerInputAdapter.cs` | InputActionAsset `Gameplay` 맵 이름 기반 조회, testMode 주입 지원, suppress 게이트 | CHARACTER_RUNTIME_READ_PATHS |
| State machine | CONFIRMED | 부재 | 별도 상태 머신 클래스 없음(모터 내 grounded/controlLocked 플래그) | 신규 설계는 CHAR01 TASK 소관 |
| Health/damage owner | CONFIRMED(레거시) | `Assets/_Legacy/StarNight/Scripts/Runtime/Player/PlayerRecovery.cs` | CurrentHealth(기본 4, tuning.MaxHealth), ApplyDamage, Damaged/HealthDepleted 이벤트, SafeCell 복귀(`SafeCellTracker.cs`) | CHARACTER_RUNTIME_READ_PATHS |
| Carry/interaction owner | CONFIRMED(레거시) | `Assets/_Legacy/StarNight/Scripts/Runtime/Objects/CarrySystem.cs`, `CarryableObject2D.cs` | 단일 HeldObject 슬롯, Drop/Throw, horizontalDropOffset 0.85. 구세대 대안: `Assets/_Legacy/_Game/Interaction/Runtime/Carry/**`(ThrowResolver, CarryPlacementResolver 등 세분화) | CHARACTER_RUNTIME_READ_PATHS |
| Bomb/rope/tool owner | CONFIRMED(레거시) | `Assets/_Legacy/StarNight/Scripts/Runtime/Explosions/`(Bomb2D, ExplosionService2D, ExplosionMask3x3, ExplosionChainResolver), `Tools/Rope/`(RopeInstaller2D, RopeClimber2D, RopeAnchor2D 등), `Tools/`(Mining, Pestle, Umbrella, Water, Grapple, PlayerToolInventory2D) | 폭탄 퓨즈/3x3 폭발 마스크/체인, 로프 설치·등반, 도구 인벤토리 | CHARACTER_RUNTIME_READ_PATHS |
| HUD/presentation bridge | CONFIRMED(레거시) | `Assets/_Legacy/StarNight/Scripts/Runtime/UI/StarNightHudView2D.cs`, `StarNightUiRoot2D.cs`, `Player/PlayerVisual2D.cs` | HUD 뷰/루트, 플레이어 표시 | CHAR05_04 참고용 |

## 3. 입력 계약 후보

레거시 자산 기준. 잠금 의미(X/Z/C)와 불일치하므로 신규 캐릭터 입력 계약은 CHAR00_02에서 잠근다.

| 논리 입력 | 상태 | 현재 바인딩 | 소스 경로 | 비고 |
|---|---|---|---|---|
| MoveLeft | CONFIRMED(레거시) | a / leftArrow / 스틱·dpad left | `Assets/_Legacy/StarNight/Input/StarNightControls.inputactions` `Gameplay/Move` | 2DVector 컴포지트 |
| MoveRight | CONFIRMED(레거시) | d / rightArrow / 스틱·dpad right | 동일 | |
| MoveDown | CONFIRMED(레거시) | s / downArrow / 스틱·dpad down | 동일 | 아래+X 안전 내려놓기 계약에 사용할 down 축 존재 |
| Jump | CONFIRMED(레거시) | Space / gamepad buttonSouth | 동일 `Gameplay/Jump` | 프로젝트 유일 선례 = Space. 최종 잠금은 CHAR00_02 |
| Action | CONFIRMED(불일치) | 레거시 Interact = E / buttonWest | 동일 `Gameplay/Interact` | 잠금 의미 X와 불일치. 레거시에 X 바인딩 없음 |
| Bomb | CONFIRMED(불일치) | 레거시 UseBomb = F / rightShoulder | 동일 `Gameplay/UseBomb` | 잠금 의미 Z와 불일치 |
| Rope | CONFIRMED(불일치) | 레거시 UseRope = Q / leftShoulder | 동일 `Gameplay/UseRope` | 잠금 의미 C와 불일치 |
| (참고) UseHeldTool | CONFIRMED(레거시) | LMB / J / rightTrigger | 동일 | 잠금 규칙엔 없는 레거시 전용 액션 |
| (참고) 자산 사본 | CONFIRMED | 사본 2개가 서로 다름 | `Assets/_Legacy/StarNight/Input/StarNightControls.inputactions` vs `Assets/_Legacy/_Game/Interaction/Data/Resources/Input/StarNightControls.inputactions` | 신규 작업 시 단일 소스 선정 필요(둘 다 레거시) |

## 4. 물리 / 충돌 기준 후보

| 항목 | 상태 | 값 또는 경로 | 비고 |
|---|---|---|---|
| Player physics type | CONFIRMED(레거시) | Rigidbody2D + 수동 중력(엔진 중력 대신 tuning.Gravity 24, maxFallSpeed 18, jumpReleaseGravityMultiplier 2.4) | `PlayerMotor2D.cs`, `P1MovementTuning.cs` |
| Player collider shape | CONFIRMED(레거시) | CapsuleCollider2D, colliderSize 0.72×0.90 | 1×1 셀 미만. `P1MovementTuning.ColliderSize` |
| World unit per logical cell | CONFIRMED(레거시) | 1 world unit = 1 cell (`GridWorld.CellSize = 1f`) | `Assets/_Legacy/StarNight/Scripts/Runtime/Grid/GridWorld.cs`. 활성 MAP은 타일을 논리 단위로만 정의하며 월드 스케일 상수 없음 → CHAR00_02에서 계약 고정 |
| Ground check method | CONFIRMED(레거시) | `Physics2D.CapsuleCast` 하향, probeDistance 0.08, grounded 판정에 vy ≤ 0.05 병행 | `GroundProbe2D.cs`, `PlayerMotor2D.SimulateFixedStep` |
| Solid layer/mask | CONFIRMED(부분) | `GroundProbe2D.groundLayers`(LayerMask 직렬화 필드) | 실제 레이어 이름/번호 값은 Scene·Prefab 자산 소유라 코드 조사로 확정 불가 → UNKNOWN(값) |
| One-way/drop-through layer | CONFIRMED(부분) | 레거시 에디터 빌더에 OneWay 사용 흔적(`Assets/_Legacy/StarNight/Scripts/Editor/P*Builder.cs`), 활성 MAP 데이터에 `MicrochunkTileLayer.OneWay` 존재 | 런타임 물리 레이어 구성 값은 UNKNOWN |
| Slope/ledge policy | CONFIRMED | NONE | 레거시·활성 어디에도 경사 정책 없음 |
| 이동 튜닝 저장 형식 선례 | CONFIRMED(레거시) | ScriptableObject(`P1MovementTuning`, CreateAssetMenu `StarNight/P1 Movement Tuning`) | maxRunSpeed 3.75, groundAccel 30, groundDecel 40, airControl 0.75, jumpHeight 2.2(>2셀 규칙 충족 선례), coyote 0.10, buffer 0.12 |

## 5. MAP 접점 후보

| 계약 | 상태 | 경로/API | 비고 |
|---|---|---|---|
| Logical cell coordinate | CONFIRMED | `Assets/_Game/Map/Runtime/WorldGeneration/Domain/`: `WorldTileCoord`, `SectorCoord`, `MicroChunkCoord`, `LocalTileCoord`, `WorldGenConstants` | World 624×416 타일, Sector 48×32, MicroChunk 12×8, 레이어 8(`MicrochunkConstants.LayerCount`) |
| 좌표 변환 진입점 | CONFIRMED | `WorldCoordinateUtility`(IsValid/TryCreate*/TryToWorld/ToWorld/TryFromWorld/ToSector/ToMicroChunk/ToLocalTile) | 캐릭터가 사용할 공용 변환 진입점. 복제 금지 |
| World query API (solid/empty) | CONFIRMED | 부재 — 활성 MAP은 생성 파이프라인·데이터 모델만 제공 | 타일 의미 원천은 `MicrochunkTileLayer`(GroundSolid/OneWay/Breakable/Hazard/Liquid/DecorationBack/DecorationFront/Marker). 캐릭터용 질의 API는 CHAR03_01에서 연결 소관 |
| Hazard query | CONFIRMED | 부재(위와 동일) | `MicrochunkTileLayer.Hazard`, `Liquid`가 미래 원천 |
| Terrain mutation request | CONFIRMED | 부재. 레거시 선례: `Assets/_Legacy/StarNight/Scripts/Runtime/Tiles/TileMutationService.cs`, `TileMutationModels.cs`, `TileBreakMethod.cs` | 잠금 규칙: 캐릭터는 요청/결과 계약만 사용, 직접 삭제 금지 |
| Room boundary gate | CONFIRMED | 부재 | 레거시 `RoomBounds2D` 등록부 선례만 있음. Hysteresis 구현은 레거시 전체에 0건 |
| Generated room readiness | CONFIRMED | 부재 | CHAR03/CHAR06 시점에 MAP 측 준비 상태 계약 필요 |
| Current MAP status/result source | CONFIRMED | `MapDesign/MCP/06_IMPLEMENTATION_STATUS.md` | MAP07_02까지 COMPLETE(83개), Current Task NONE. 생성 CSV 0, Authoring CSV 50개(`Assets/_Game/Map/Data/WorldGeneration/Authoring/**`) |

## 6. 카메라 / 방 전환 후보

| 항목 | 상태 | 경로 또는 값 | 비고 |
|---|---|---|---|
| Camera room owner | CONFIRMED(레거시) | `Assets/_Legacy/StarNight/Scripts/Runtime/World/RoomFocusCamera2D.cs`(+fallback `GridBoundedCamera2D.cs`) | 방 프레이밍 전용, margin 0.5, transitionTime 0.28s, orthoSize 3.5~7 |
| Room size source | CONFIRMED(레거시) | `RoomBounds2D.worldRect` 기본 12×8 | 활성 MAP MicroChunk 12×8과 수치 일치(사실 기록, 의도 여부는 미확정) |
| Transition trigger | CONFIRMED(레거시) | `RoomBounds2D.Contains(worldPoint)` 기반 현재 방 선택 | 카메라 연출 전용이며 이동 월드를 분리하지 않음 — 잠금 규칙과 호환 |
| Input KEEP policy | CONFIRMED | KEEP_REQUIRED(잠금 규칙). 레거시 카메라는 이동/입력에 개입하지 않으므로 위반 선례 없음 | |
| Velocity KEEP policy | CONFIRMED | KEEP_REQUIRED(잠금 규칙). 위와 동일 | |
| Hysteresis policy | CONFIRMED | 부재(레거시 전체 grep 0건) — 신규 구현 필요(CHAR03_02 소관) | |

## 7. 테스트 / 픽스처 후보

| 용도 | 상태 | 경로 | 비고 |
|---|---|---|---|
| Character unit tests | CONFIRMED | 부재(활성). 레거시: `StarNight.Tests.EditMode`, `Assets/_Legacy/_Game/Player/Tests/**` | 신규 작성은 CHAR00_02+ 소관 |
| Movement playmode tests | CONFIRMED | 부재(활성). 레거시: `Assets/_Legacy/_Game/Player/Tests/PlayMode/PlayerMotorPlayModeTests.cs` 등 | |
| Map integration tests | CONFIRMED | 활성 EditMode: `Assets/_Game/Tests/EditMode/Map/**` .cs 77개(직전 MAP07_02 기준 통과 이력은 `MapDesign/MCP/06_IMPLEMENTATION_STATUS.md` 참조) | 캐릭터 회귀 기준선으로 사용 가능 |
| Test scene or fixture prefab | CONFIRMED | 활성: `Assets/_Game/Scenes/MapGenerationProgressTest.unity`. 레거시: `Assets/_Legacy/StarNight/Scenes/Labs/P1_GridLab_30x18.unity` 등 | 레거시 씬은 컴파일 제외 스크립트에 의존하므로 재사용 불가 전제 |
| Generated map seed fixture | CONFIRMED | 부재(generated CSV 0개) | CHAR06 시점에 MAP 생성 파이프라인 실행 결과 필요 |

## 8. 다음 TASK 토큰 치환 후보

CHAR00_01 조사 결과 기준 후보다. 최종 치환은 OPEN 패치 소관이다.

| 토큰 | 실제 경로 후보 |
|---|---|
| `DISCOVERED_READ_PATHS` | `CharacterDesign/**`, `Assets/_Game/Map/Runtime/**`, `Assets/_Game/Tests/**`, `Assets/_Legacy/StarNight/Scripts/Runtime/**`, `Assets/_Legacy/_Game/{Player,Interaction}/**`, `Packages/manifest.json`, `MapDesign/MCP/06_IMPLEMENTATION_STATUS.md` |
| `HARNESS_WRITE_PATHS` | `CharacterDesign/MCP/REPORTS/**`, `CharacterDesign/MCP/INPUTS/**`, `CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md`(STATUS FINALIZE/PATCH APPLY 한정) |
| `CHARACTER_RUNTIME_READ_PATHS` | `Assets/_Legacy/StarNight/Scripts/Runtime/{Player,Objects,Explosions,Tools,World,Grid,Tiles}/**`(선례 read-only), `Assets/_Game/Map/Runtime/WorldGeneration/{Domain,Microchunks}/**`(공용 계약) |
| `CHARACTER_TEST_READ_PATHS` | `Assets/_Game/Tests/EditMode/Map/**`(회귀 기준선), `Assets/_Legacy/StarNight/Scripts/Tests/**`, `Assets/_Legacy/_Game/Player/Tests/**`(선례 read-only) |
| `TASK_SPECIFIC_INTEGRATION_READ_PATHS` | `Assets/_Game/Map/Runtime/WorldGeneration/**`, `Assets/_Game/Map/Data/WorldGeneration/Authoring/**`, `MapDesign/MCP/**` |
| `TASK_SPECIFIC_RUNTIME_WRITE_PATHS` | BLOCKER — 신규 캐릭터 코드 배치 위치 결정 필요. 후보: `Assets/_Game/Character/Runtime/**`(+ 신규 asmdef는 잠금 규칙상 조사 완료 후에만, 도입 시 CHANGE CONTROL 절차) |
| `TASK_SPECIFIC_TEST_WRITE_PATHS` | BLOCKER — 위 결정에 종속. 후보: `Assets/_Game/Tests/EditMode/Character/**`, `Assets/_Game/Tests/PlayMode/Character/**` |

## 9. CHAR00_02에서 고정할 계약 후보

- Cell size / world unit contract: 후보 1 world unit = 1 logical cell(레거시 선례). 활성 MAP 좌표계와의 월드 스케일 매핑을 CHAR00_02에서 명문화
- Player collider dimensions: 후보 0.72×0.90 캡슐(레거시 선례)
- Grounded definition: 후보 CapsuleCast 하향 + vy ≤ 0.05(레거시 선례)
- Jump reach validation fixture: 레거시 jumpHeight 2.2셀(>2셀 규칙 충족 선례), 검증 코스는 `CharacterDesign/04_TEST_FIXTURES/MOVEMENT_COURSE_SPEC.md` 기반
- 2-cell gap validation fixture: maxRunSpeed 3.75 선례, 픽스처 신규 작성 필요
- 3-cell gap failure validation fixture: 픽스처 신규 작성 필요
- Forbidden movement list: no wall jump, no dash, no double jump(레거시 모터도 coyote/buffer 지상 게이트 단일 점프로 위반 선례 없음)
- Combat baseline: no basic attack button, stomp/contact/tool/environment only(레거시에 일반 공격 버튼 부재 확인 — UseHeldTool은 도구 사용 액션)
- Carry baseline: one carry slot, <=1x1 object first(레거시 CarrySystem 단일 슬롯 선례 일치)
- MAP dependency direction: Character queries MAP contract, not Tilemap internals(레거시 GridWorld는 Tilemap 직접 소유 — 신규에서 금지 선례로 기록)
- 입력 계약: 잠금 의미 X/Z/C 기준 신규 액션 맵 정의 필요(레거시 바인딩 불일치), Jump 장치 키 선례 Space

## 10. BLOCKERS

1. `TASK_SPECIFIC_RUNTIME_WRITE_PATHS` / `TASK_SPECIFIC_TEST_WRITE_PATHS`: 신규 캐릭터 코드·테스트 배치 위치와 신규 asmdef 도입 여부는 사용자 승인(OPEN 패치)이 필요하다. 후보는 §8 참고.
2. 입력 자산 불일치: 잠금 의미(X=행동, Z=폭탄, C=로프)를 만족하는 활성 inputactions가 없다. CHAR00_02 계약 고정 시 신규 액션 맵 정의(또는 사용자 지시)가 필요하다.
3. 캐릭터용 MAP 월드 질의·지형 변경 요청·경계 게이트·방 준비 API 부재: CHAR00_02 진행에는 비차단이나, CHAR03_01 도달 전 MAP 측 계약 신설 범위 승인이 필요하다.
