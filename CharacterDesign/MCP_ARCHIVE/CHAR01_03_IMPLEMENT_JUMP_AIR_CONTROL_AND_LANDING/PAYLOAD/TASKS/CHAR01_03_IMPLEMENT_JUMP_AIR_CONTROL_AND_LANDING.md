# CHAR01_03 — Jump, Air Control, and Landing

```yaml
status_control:
  task_key: CHAR01_03_IMPLEMENT_JUMP_AIR_CONTROL_AND_LANDING
  result_file: REPORTS/CHAR01_03_IMPLEMENT_JUMP_AIR_CONTROL_AND_LANDING_RESULT.md
```

## TASK TYPE

UNITY RUNTIME IMPLEMENTATION + EDITMODE TESTS

## Objective

CHAR01_01 입력·상태 모델과 CHAR01_02 충돌·지상 모터 위에 점프, 점프 버퍼, 코요테 시간, 가변 점프 release, 공중 수평 제어, 중력/낙하 제한, 착지 전환을 구현한다.

이번 Task는 핵심 이동 구현의 마지막 구현 단계다. 2셀 높이 도달, 2셀 틈 통과, 3셀 틈 실패 같은 코스 기반 게임플레이 검증은 CHAR02에서 수행한다.

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
11. `MCP/REPORTS/CHAR01_02_IMPLEMENT_COLLISION_QUERIES_AND_GROUND_MOTOR_RESULT.md`
12. `MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md`
13. `01_FIXED_SPEC/01_CHARACTER_GAMEPLAY_RULES.md`
14. `01_FIXED_SPEC/02_CHARACTER_INPUT_RULES.md`
15. `01_FIXED_SPEC/03_CHARACTER_MOVEMENT_RULES.md`
16. `01_FIXED_SPEC/07_CHARACTER_TEST_RULES.md`
17. `03_DATA_SCHEMA/CHARACTER_ACTION_SCHEMA.md`
18. `03_DATA_SCHEMA/CHARACTER_MOVEMENT_TUNING_SCHEMA.md`
19. `04_TEST_FIXTURES/MOVEMENT_COURSE_SPEC.md`

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
Assets/_Legacy/StarNight/Scripts/Runtime/Player/JumpGraceState.cs
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

다음 정확한 런타임 파일만 생성 또는 수정할 수 있다.

```text
Assets/_Game/Character/Runtime/Movement/CharacterJumpSettings.cs
Assets/_Game/Character/Runtime/Movement/CharacterJumpState.cs
Assets/_Game/Character/Runtime/Movement/CharacterJumpController.cs
Assets/_Game/Character/Runtime/Movement/CharacterGravitySettings.cs
Assets/_Game/Character/Runtime/Movement/CharacterGravityMotor.cs
Assets/_Game/Character/Runtime/Movement/CharacterAirControlSettings.cs
Assets/_Game/Character/Runtime/Movement/CharacterAirControlMotor.cs
Assets/_Game/Character/Runtime/Movement/CharacterLandingDetector.cs
```

### Existing Runtime Integration

다음 파일은 CHAR01_03 연동에 필요한 최소 API 추가 또는 조정만 허용한다. 기존 CHAR01_01/02 테스트가 계속 PASS해야 한다.

```text
Assets/_Game/Character/Runtime/Movement/CharacterGroundMotorState.cs
Assets/_Game/Character/Runtime/State/CharacterPlayerState.cs
Assets/_Game/Character/Runtime/State/CharacterPlayerStateSnapshot.cs
```

### EditMode Tests

다음 테스트 파일만 생성 또는 수정할 수 있다.

```text
Assets/_Game/Tests/EditMode/Character/CharacterJumpControllerTests.cs
Assets/_Game/Tests/EditMode/Character/CharacterAirAndGravityMotorTests.cs
Assets/_Game/Tests/EditMode/Character/CharacterLandingAndMovementBoundaryTests.cs
Assets/_Game/Tests/EditMode/Character/CharacterMovementBoundaryTests.cs
```

`CharacterMovementBoundaryTests.cs`는 CHAR01_02 당시 금지했던 Jump/Gravity/AirControl/Landing 타입을 이번 Task에서 허용하도록 업데이트할 수 있다. 단, wall jump, dash, double jump, basic attack 금지는 계속 유지해야 한다.

Unity가 위 신규 `.cs` 파일에 생성하는 대응 `.meta`도 허용한다.

### Report

```text
CharacterDesign/MCP/REPORTS/CHAR01_03_IMPLEMENT_JUMP_AIR_CONTROL_AND_LANDING_RESULT.md
```

TASK EXECUTION 중 `06_IMPLEMENTATION_STATUS.md`와 `MASTER_IMPLEMENTATION_TASK_LIST.md`는 수정하지 않는다. 상태 변경은 Result PASS 이후 STATUS FINALIZE Phase만 수행한다.

## DO NOT

- `.inputactions`, InputActionAsset, Input System wrapper 생성·수정
- `Game.Character.Runtime.asmdef` 또는 `Game.Character.Tests.EditMode.asmdef` 수정
- MAP runtime/API 수정, Tilemap 직접 접근, MAP 좌표/셀 크기 상수 복제
- Scene, Prefab, Animator, ScriptableObject, material, sprite 생성·수정
- 2셀 높이 도달, 2셀 틈 통과, 3셀 틈 실패 코스 검증 구현
- wall jump, dash, double jump 관련 타입·상태·입력 추가
- 일반 공격 action/state 추가
- PlayMode 테스트 생성·실행
- `Assets/_Legacy/**`, `Assets/_Game/Map/**`, `Packages/**`, `ProjectSettings/**`, `MapDesign/**` 수정
- 기존 파일·폴더 삭제/이동/이름 변경
- 관련 없는 warning 수정, formatting sweep, package install
- git commit/push/branch/reset/rebase/force
- CHAR01_04 선행 감사 작업

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

### Jump Contract

`CharacterJumpSettings`:

- 필수 필드: `jumpVelocity`, `coyoteTime`, `jumpBufferTime`, 가변 점프 release 계수 또는 cut 계수.
- 기본값은 movement tuning schema와 레거시 선례를 따른다.
- `jumpVelocity > 0`, `coyoteTime >= 0`, `jumpBufferTime >= 0`을 검증한다.

`CharacterJumpState`:

- grounded tick/time, last jump press tick/time, jump consumed state, variable jump release state를 추적한다.
- grounded를 다시 획득하면 jump consumed 상태를 reset한다.
- 공중에서 두 번째 점프를 허용하지 않는다.

`CharacterJumpController`:

- Jump press가 점프 버퍼 시간 안에 있고 grounded 또는 coyote window 안이면 jump를 시작한다.
- 점프 시작 시 vertical velocity를 `jumpVelocity`로 설정하고 Jump action을 소비한다.
- 같은 Jump press는 한 번만 소비된다.
- grounded/coyote 조건이 없으면 Jump press를 보존하되 window 만료 후 폐기한다.
- jump release가 상승 중이면 가변 점프 release 정책으로 상승을 줄인다.
- wall jump, dash jump, double jump를 구현하지 않는다.

### Gravity and Air Control Contract

`CharacterGravitySettings`:

- 필수 필드: `riseGravity`, `fallGravity`, `maxFallSpeed`.
- `fallGravity`는 하강 중 적용되며 `maxFallSpeed`로 낙하 속도를 clamp한다.
- 값은 모두 0보다 커야 한다.

`CharacterGravityMotor`:

- 상승 중에는 rise gravity를 적용한다.
- 하강 중에는 fall gravity를 적용한다.
- vertical velocity가 `-maxFallSpeed`보다 더 작아지지 않는다.
- grounded이면 중력으로 불필요한 하강 누적을 만들지 않는다.

`CharacterAirControlSettings`:

- 필수 필드: `airAcceleration`.
- `airAcceleration >= 0`을 검증한다.

`CharacterAirControlMotor`:

- airborne 상태에서만 horizontal input을 `[-1, 1]`로 clamp해 수평 속도를 목표 속도 방향으로 이동시킨다.
- 지상에서는 ground motor 소관이므로 공중 제어를 적용하지 않는다.
- vertical velocity를 변경하지 않는다.
- wall jump, dash, double jump 관련 side effect가 없다.

### Landing Contract

`CharacterLandingDetector`:

- 이전 tick이 airborne이고 이번 tick이 grounded이면 landing을 감지한다.
- 착지는 vertical velocity를 0 이하 안전값으로 정리할 수 있다.
- 착지는 jump consumed state를 reset한다.
- 착지 감지는 Animator, 사운드, render frame 성공 여부에 의존하지 않는다.

## Implementation Steps

1. Current Task가 `CHAR01_03_IMPLEMENT_JUMP_AIR_CONTROL_AND_LANDING`인지 확인한다.
2. `CHAR01_02` REPORT가 `STATUS: PASS`이고 Current Task after finalize가 NONE인지 확인한다.
3. 작업 전 변경 파일 경로를 기록하고 기존 사용자 변경은 수정하지 않는다.
4. CHAR01_01/02 runtime/test API와 기존 asmdef 경계를 확인한다.
5. Jump Contract를 구현한다.
6. Gravity and Air Control Contract를 구현한다.
7. Landing Contract를 구현한다.
8. 필요한 경우 `CharacterGroundMotorState`, `CharacterPlayerState`, `CharacterPlayerStateSnapshot`에 최소 연동 API만 추가한다.
9. `CharacterMovementBoundaryTests.cs`를 CHAR01_03 기준으로 업데이트한다.
10. 지정된 EditMode 테스트 3개와 필수 test case를 구현한다.
11. Unity Asset Refresh와 compile 완료를 기다린다.
12. `Game.Character.Tests.EditMode` 전체 EditMode 테스트를 실행한다. CHAR01_01 12개와 CHAR01_02 12개도 계속 PASS해야 한다.
13. 작업 후 변경 파일 경로가 WRITE ALLOWLIST와 REPORT뿐인지 확인한다.
14. Result를 작성한다.
15. 모든 DONE CONDITIONS가 PASS일 때만 `STATUS: PASS`를 기록한다.

## Required Tests

### `CharacterJumpControllerTests`

필수 test case:

```text
JumpBuffer_PressBeforeGroundedTriggersOnGroundedTick
CoyoteTime_AllowsJumpShortlyAfterLeavingGround
Jump_IsConsumedOnceAndSetsUpwardVelocity
Jump_DoesNotAllowSecondJumpBeforeGroundedAgain
```

### `CharacterAirAndGravityMotorTests`

필수 test case:

```text
AirControl_AcceleratesHorizontallyOnlyWhileAirborne
AirControl_ClampsHorizontalIntentAndPreservesVerticalVelocity
Gravity_UsesRiseGravityWhenAscendingAndFallGravityWhenDescending
Gravity_ClampsToMaxFallSpeed
VariableJumpRelease_ReducesUpwardVelocityOnlyWhileAscending
```

### `CharacterLandingAndMovementBoundaryTests`

필수 test case:

```text
LandingDetector_FiresOnlyOnAirborneToGroundedTransition
LandingDetector_ResetsJumpConsumedState
MovementRuntime_DoesNotDeclareForbiddenMovementOrBasicAttackFeatures
```

테스트 이름은 위와 정확히 일치해야 한다. 추가 test case는 허용되지만 위 12개는 반드시 존재하고 PASS해야 한다. 기존 CHAR01_01 12개와 CHAR01_02 12개도 같은 test assembly에서 계속 PASS해야 한다.

## Unity Verification

- Unity Version: 6000.3.8f1
- Asset Refresh: PASS
- Compile Errors: 0
- Relevant New Warnings: 0
- Targeted EditMode Tests: PASS (`Game.Character.Tests.EditMode` 전체)
- Required CHAR01_03 Tests: PASS (12/12)
- Existing CHAR01_01 Tests: PASS (12/12)
- Existing CHAR01_02 Tests: PASS (12/12)
- PlayMode Tests: NOT RUN
- Scene/Prefab Changes: 0

컴파일 오류가 있으면 테스트 미실행이라도 `STATUS: FAIL` 또는 `STATUS: BLOCKED`다.

## Result File

```text
REPORTS/CHAR01_03_IMPLEMENT_JUMP_AIR_CONTROL_AND_LANDING_RESULT.md
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

- [ ] `CHAR01_02` PASS와 finalize 상태가 검증됐다.
- [ ] 승인된 Movement runtime/test 파일만 추가 또는 수정됐다.
- [ ] Jump buffer, coyote time, single jump consumption이 구현됐다.
- [ ] 가변 점프 release가 상승 중에만 적용된다.
- [ ] rise/fall gravity와 max fall speed clamp가 구현됐다.
- [ ] airborne 상태의 air control이 구현되고 vertical velocity를 보존한다.
- [ ] landing transition과 jump consumed reset이 구현됐다.
- [ ] 2셀/3셀 코스 검증은 구현하지 않았다.
- [ ] 일반 공격·wall jump·dash·double jump 관련 action/state/type이 없다.
- [ ] MAP/Tilemap/Scene/Prefab/inputactions/asmdef/Packages/ProjectSettings 변경이 없다.
- [ ] CHAR01_03 필수 12개 EditMode test case가 전부 PASS했다.
- [ ] CHAR01_01 기존 12개와 CHAR01_02 기존 12개가 전부 PASS했다.
- [ ] Unity compile error와 relevant new warning이 0이다.
- [ ] REPORT 외 하네스 상태 파일은 Task execution 중 수정하지 않았다.
- [ ] CHAR01_04를 시작하지 않았다.

## Completion Rule

Task는 runtime/test 파일과 REPORT만 작성한다. PASS 후 `08_STATUS_FINALIZE_RULES.md`가 CHAR01_03을 COMPLETE, Current Task를 NONE으로 변경한다. `CHAR01_04_CHAR01_CORE_MOVEMENT_EXIT_AUDIT`는 LOCKED로 유지하고 자동 시작하지 않는다.
