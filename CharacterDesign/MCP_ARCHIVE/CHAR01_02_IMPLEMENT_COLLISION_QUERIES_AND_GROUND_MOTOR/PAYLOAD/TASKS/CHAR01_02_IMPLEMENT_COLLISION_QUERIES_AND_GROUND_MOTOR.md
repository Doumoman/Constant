# CHAR01_02 — Collision Queries and Ground Motor

```yaml
status_control:
  task_key: CHAR01_02_IMPLEMENT_COLLISION_QUERIES_AND_GROUND_MOTOR
  result_file: REPORTS/CHAR01_02_IMPLEMENT_COLLISION_QUERIES_AND_GROUND_MOTOR_RESULT.md
```

## TASK TYPE

UNITY RUNTIME IMPLEMENTATION + EDITMODE TESTS

## Objective

CHAR01_01의 입력·상태 모델 위에 캐릭터 충돌 질의 추상화, 지면 판정, 지상 수평 이동 모터를 구현한다.

이번 Task는 걷기·달리기·가속·감속·방향 전환과 grounded 판정까지만 다룬다. 점프 속도, 중력, 공중 제어, 착지 이벤트, 2셀/3셀 코스 검증, MAP 연동은 만들지 않는다.

## Mandatory Read Order

1. `MCP/00_MCP_ENTRYPOINT.md`
2. `MCP/01_CHARACTER_LOCKED_RULES.md`
3. `MCP/02_MCP_WORK_RULES.md`
4. `MCP/03_CHARACTER_DATA_RULES.md`
5. `MCP/04_UNITY_MCP_RULES.md`
6. `MCP/05_CHANGE_CONTROL_RULES.md`
7. `MCP/07_PATCH_APPLY_RULES.md`
8. `MCP/08_STATUS_FINALIZE_RULES.md`
9. `MCP/06_IMPLEMENTATION_STATUS.md`
10. 이 TASK
11. `MCP/REPORTS/CHAR01_01_IMPLEMENT_INPUT_SNAPSHOT_AND_PLAYER_STATES_RESULT.md`
12. `MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md`
13. `01_FIXED_SPEC/01_CHARACTER_GAMEPLAY_RULES.md`
14. `01_FIXED_SPEC/02_CHARACTER_INPUT_RULES.md`
15. `01_FIXED_SPEC/03_CHARACTER_MOVEMENT_RULES.md`
16. `01_FIXED_SPEC/07_CHARACTER_TEST_RULES.md`
17. `03_DATA_SCHEMA/CHARACTER_MOVEMENT_TUNING_SCHEMA.md`
18. `04_TEST_FIXTURES/MOVEMENT_COURSE_SPEC.md`

## READ ALLOWLIST

본문 읽기 허용:

```text
CharacterDesign/**
Packages/manifest.json
ProjectSettings/ProjectSettings.asset
Assets/_Game/Character/Runtime/**
Assets/_Game/Tests/EditMode/Character/**
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
Assets/_Game/Tests/EditMode/Map/**/*.cs
Assets/_Legacy/StarNight/Scripts/Runtime/Player/PlayerMotor2D.cs
Assets/_Legacy/StarNight/Scripts/Runtime/Player/GroundProbe2D.cs
Assets/_Legacy/StarNight/Scripts/Runtime/Player/P1MovementTuning.cs
Assets/_Legacy/StarNight/Scripts/Tests/**/*.cs
Assets/_Legacy/_Game/Player/Tests/**/*.cs
```

기존 테스트 스타일 확인은 최대 4개 NUnit 테스트 파일 본문만 읽는다. 레거시는 read-only 선례이며 복사하거나 활성 참조로 되살리지 않는다.

제한적 검색 허용:

```text
Assets/_Game/Character/**
Assets/_Game/Tests/EditMode/Character/**
Assets/_Game/**/*.asmdef
Assets/_Game/**/*.asmref
```

위 검색은 존재 여부, 파일명, asmdef JSON 경계, 금지 dependency 확인에만 사용한다.

## WRITE ALLOWLIST

### Runtime Movement

다음 디렉터리, 대응 Unity folder `.meta`, 정확한 런타임 파일만 생성 또는 수정할 수 있다.

```text
Assets/_Game/Character/Runtime/Movement/
Assets/_Game/Character/Runtime/Movement/CharacterCapsuleGeometry.cs
Assets/_Game/Character/Runtime/Movement/CharacterCollisionHit.cs
Assets/_Game/Character/Runtime/Movement/ICharacterCollisionWorld.cs
Assets/_Game/Character/Runtime/Movement/UnityPhysics2DCharacterCollisionWorld.cs
Assets/_Game/Character/Runtime/Movement/CharacterGroundProbeSettings.cs
Assets/_Game/Character/Runtime/Movement/CharacterGroundProbeResult.cs
Assets/_Game/Character/Runtime/Movement/CharacterGroundProbe.cs
Assets/_Game/Character/Runtime/Movement/CharacterGroundMotorSettings.cs
Assets/_Game/Character/Runtime/Movement/CharacterGroundMotorState.cs
Assets/_Game/Character/Runtime/Movement/CharacterGroundMotor.cs
```

### Existing State Integration

다음 파일은 CHAR01_02 모터 연동을 위해 필요한 최소 API 추가만 허용한다. 기존 CHAR01_01 테스트 12개가 계속 PASS해야 한다.

```text
Assets/_Game/Character/Runtime/State/CharacterPlayerState.cs
Assets/_Game/Character/Runtime/State/CharacterPlayerStateSnapshot.cs
```

### EditMode Tests

다음 정확한 테스트 파일만 생성 또는 수정할 수 있다.

```text
Assets/_Game/Tests/EditMode/Character/CharacterGroundProbeTests.cs
Assets/_Game/Tests/EditMode/Character/CharacterGroundMotorTests.cs
Assets/_Game/Tests/EditMode/Character/CharacterMovementBoundaryTests.cs
```

Unity가 위 신규 `.cs` 파일과 신규 `Movement` 폴더에 생성하는 대응 `.meta`도 허용한다.

### Report

```text
CharacterDesign/MCP/REPORTS/CHAR01_02_IMPLEMENT_COLLISION_QUERIES_AND_GROUND_MOTOR_RESULT.md
```

TASK EXECUTION 중 `06_IMPLEMENTATION_STATUS.md`와 `MASTER_IMPLEMENTATION_TASK_LIST.md`는 수정하지 않는다. 상태 변경은 Result PASS 이후 STATUS FINALIZE Phase만 수행한다.

## DO NOT

- `.inputactions`, InputActionAsset, Input System wrapper 생성·수정
- `Game.Character.Runtime.asmdef` 또는 `Game.Character.Tests.EditMode.asmdef` 수정
- MAP runtime/API 수정, Tilemap 직접 접근, MAP 좌표/셀 크기 상수 복제
- Scene, Prefab, Animator, ScriptableObject, material, sprite 생성·수정
- 점프 속도, jump buffer/coyote 적용, 중력, 낙하, 공중 제어, 착지 이벤트 구현
- 2셀 높이 도달, 2셀 틈 통과, 3셀 틈 실패 코스 검증 구현
- 일반 공격 action/state 추가
- wall jump, dash, double jump 관련 타입·상태 추가
- PlayMode 테스트 생성·실행
- `Assets/_Legacy/**`, `Assets/_Game/Map/**`, `Packages/**`, `ProjectSettings/**`, `MapDesign/**` 수정
- 기존 파일·폴더 삭제/이동/이름 변경
- 관련 없는 warning 수정, formatting sweep, package install
- git commit/push/branch/reset/rebase/force
- CHAR01_03 선행 작업

## Runtime Contract

### Namespace

신규 runtime namespace:

```text
StarNight.Character.Movement
```

Tests namespace:

```text
StarNight.Character.Tests
```

### Collision Query Contract

`CharacterCapsuleGeometry`:

- locked collider baseline `0.72 x 0.90` world unit을 기본값으로 제공한다.
- 중심점, 크기, 하향 probe 거리 계산에 사용할 수 있다.
- 셀 크기나 MAP 좌표 상수를 정의하지 않는다.

`ICharacterCollisionWorld`:

- 캐릭터 코드가 실제 Physics2D 호출 또는 fake world를 교체할 수 있는 interface다.
- 최소 기능: capsule cast 또는 equivalent swept query.
- Tilemap, MAP 데이터 모델, scene object lookup에 의존하지 않는다.

`UnityPhysics2DCharacterCollisionWorld`:

- runtime adapter이며 `Physics2D`를 통해 2D collision query만 수행한다.
- LayerMask/ContactFilter2D 계열 사용은 허용한다.
- MAP Tilemap, `WorldCoordinateUtility`, generated map internals를 직접 읽지 않는다.

`CharacterCollisionHit`:

- hit 여부, point, normal, distance, collider reference 또는 stable id 중 필요한 값을 담는다.
- 테스트 fake world가 Unity scene 없이 결과를 만들 수 있어야 한다.

### Ground Probe Contract

`CharacterGroundProbeSettings`:

- probe distance 기본값은 `0.08`.
- rising velocity를 grounded로 보지 않는 임계값 기본값은 `0.05`.
- 값은 0 이상이어야 한다.

`CharacterGroundProbe`:

- capsule baseline을 사용해 아래 방향 ground probe를 수행한다.
- 충돌이 probe distance 안에 있고 surface normal이 upward로 해석 가능하며 vertical velocity가 상승 임계값 이하일 때 grounded다.
- query miss, 너무 먼 hit, 너무 수평/벽 normal, 빠른 상승 상태는 grounded가 아니다.
- one-way/drop-through 정책은 이번 Task에서 구현하지 않고 interface 확장 여지만 남긴다.

`CharacterGroundProbeResult`:

- grounded 여부, hit normal, hit distance, support velocity 또는 support id를 담을 수 있다.
- 없거나 미확정인 값은 nullable 또는 명시적 empty로 표현한다.

### Ground Motor Contract

`CharacterGroundMotorSettings`:

- `walkSpeed`, `runSpeed`, `groundAcceleration`, `groundDeceleration` 필드를 제공한다.
- 기본값은 레거시 선례와 locked schema를 참고하되, PASS 기준은 값 자체가 아니라 동작이다.
- `runSpeed > walkSpeed > 0`, acceleration/deceleration > 0을 검증한다.

`CharacterGroundMotor`:

- horizontal input을 `[-1, 1]`로 clamp한다.
- run flag가 false면 walkSpeed, true면 runSpeed를 목표 속도로 사용한다.
- 지상에서 입력 방향으로 acceleration을 적용한다.
- 입력이 없으면 deceleration으로 0에 접근한다.
- 방향 전환 시 overshoot 없이 목표 속도로 접근한다.
- vertical velocity는 변경하지 않는다.
- jump/gravity/air control/landing을 적용하지 않는다.
- grounded가 아니면 horizontal ground acceleration을 적용하지 않는다. 공중 제어는 CHAR01_03 소관이다.
- facing은 horizontal input이 0이 아닐 때만 갱신하고, 0이면 기존 facing을 유지한다.

`CharacterGroundMotorState`:

- velocity, facing, grounded/locomotion 상태를 immutable 또는 명시적 state object로 전달할 수 있다.
- Animator, sound, render frame 성공 여부에 의존하지 않는다.

## Implementation Steps

1. Current Task가 `CHAR01_02_IMPLEMENT_COLLISION_QUERIES_AND_GROUND_MOTOR`인지 확인한다.
2. `CHAR01_01` REPORT가 `STATUS: PASS`이고 Current Task after finalize가 NONE인지 확인한다.
3. 작업 전 변경 파일 경로를 기록하고 기존 사용자 변경은 수정하지 않는다.
4. CHAR01_01 runtime/test API와 기존 asmdef 경계를 확인한다.
5. 승인된 `Runtime/Movement` 디렉터리와 필요한 `.meta`를 생성한다.
6. Collision Query Contract를 구현한다.
7. Ground Probe Contract를 구현한다.
8. Ground Motor Contract를 구현한다.
9. 필요한 경우 `CharacterPlayerState`/`CharacterPlayerStateSnapshot`에 최소 연동 API만 추가한다.
10. 지정된 EditMode 테스트 3개와 필수 test case를 구현한다.
11. Unity Asset Refresh와 compile 완료를 기다린다.
12. `Game.Character.Tests.EditMode` 전체 EditMode 테스트를 실행한다. CHAR01_01 기존 12개도 계속 PASS해야 한다.
13. 작업 후 변경 파일 경로가 WRITE ALLOWLIST와 REPORT뿐인지 확인한다.
14. Result를 작성한다.
15. 모든 DONE CONDITIONS가 PASS일 때만 `STATUS: PASS`를 기록한다.

## Required Tests

### `CharacterGroundProbeTests`

필수 test case:

```text
GroundProbe_UsesLockedCapsuleSize
GroundProbe_ReturnsGroundedForValidDownwardHit
GroundProbe_RejectsMissTooFarWallNormalAndRisingVelocity
GroundProbe_DoesNotRequireMapOrTilemapTypes
```

### `CharacterGroundMotorTests`

필수 test case:

```text
GroundMotor_AcceleratesTowardWalkSpeed
GroundMotor_AcceleratesTowardRunSpeed
GroundMotor_DeceleratesTowardZeroWithoutInput
GroundMotor_ClampsHorizontalIntentAndPreventsOvershoot
GroundMotor_PreservesVerticalVelocity
GroundMotor_DoesNotMoveWhenAirborne
```

### `CharacterMovementBoundaryTests`

필수 test case:

```text
MovementRuntime_DoesNotDeclareJumpGravityAirControlOrLandingTypes
MovementRuntime_DoesNotDeclareForbiddenMovementFeatures
```

테스트 이름은 위와 정확히 일치해야 한다. 추가 test case는 허용되지만 위 12개는 반드시 존재하고 PASS해야 한다. 기존 CHAR01_01 테스트 12개도 같은 test assembly에서 계속 PASS해야 한다.

## Unity Verification

- Unity Version: 6000.3.8f1
- Asset Refresh: PASS
- Compile Errors: 0
- Relevant New Warnings: 0
- Targeted EditMode Tests: PASS (`Game.Character.Tests.EditMode` 전체)
- Required CHAR01_02 Tests: PASS (12/12)
- Existing CHAR01_01 Tests: PASS (12/12)
- PlayMode Tests: NOT RUN
- Scene/Prefab Changes: 0

컴파일 오류가 있으면 테스트 미실행이라도 `STATUS: FAIL` 또는 `STATUS: BLOCKED`다.

## Result File

```text
REPORTS/CHAR01_02_IMPLEMENT_COLLISION_QUERIES_AND_GROUND_MOTOR_RESULT.md
```

REPORT 필수 항목:

```text
TASK
STATUS: PASS / FAIL / BLOCKED
SUMMARY
READ
CREATED
CHANGED
IMPLEMENTATION
TEST
UNITY
SCOPE VALIDATION
OUT_OF_SCOPE_FINDINGS
DONE CONDITIONS
NEXT
```

## DONE CONDITIONS

- [ ] `CHAR01_01` PASS와 finalize 상태가 검증됐다.
- [ ] 승인된 `Runtime/Movement` 경로와 정확한 테스트 파일만 추가됐다.
- [ ] Collision query abstraction과 Unity Physics2D adapter가 구현됐다.
- [ ] Ground probe가 locked capsule baseline, probe distance, vertical velocity gate를 사용한다.
- [ ] Ground motor가 walk/run acceleration, deceleration, facing update, vertical velocity preservation을 구현한다.
- [ ] 공중 상태에서 지상 모터가 horizontal acceleration을 적용하지 않는다.
- [ ] jump/gravity/air control/landing은 구현하지 않았다.
- [ ] 일반 공격·wall jump·dash·double jump 관련 action/state/type이 없다.
- [ ] MAP/Tilemap/Scene/Prefab/inputactions/asmdef/Packages/ProjectSettings 변경이 없다.
- [ ] CHAR01_02 필수 12개 EditMode test case가 전부 PASS했다.
- [ ] CHAR01_01 기존 12개 EditMode test case가 전부 PASS했다.
- [ ] Unity compile error와 relevant new warning이 0이다.
- [ ] REPORT 외 하네스 상태 파일은 Task execution 중 수정하지 않았다.
- [ ] CHAR01_03을 시작하지 않았다.

## Completion Rule

Task는 runtime/test 파일과 REPORT만 작성한다. PASS 후 `08_STATUS_FINALIZE_RULES.md`가 CHAR01_02를 COMPLETE, Current Task를 NONE으로 변경한다. `CHAR01_03_IMPLEMENT_JUMP_AIR_CONTROL_AND_LANDING`는 LOCKED로 유지하고 자동 시작하지 않는다.
