# CHAR01_01 — Input Snapshot and Player States

```yaml
status_control:
  task_key: CHAR01_01_IMPLEMENT_INPUT_SNAPSHOT_AND_PLAYER_STATES
  result_file: REPORTS/CHAR01_01_IMPLEMENT_INPUT_SNAPSHOT_AND_PLAYER_STATES_RESULT.md
```

## TASK TYPE

UNITY RUNTIME IMPLEMENTATION + EDITMODE TESTS

## Objective

캐릭터 첫 활성 런타임으로 논리 입력 스냅샷, 입력 버퍼, 입력 잠금 reason set, 플레이어 상태 스냅샷을 구현한다.

이번 Task는 `CHAR00_03`에서 승인된 신규 캐릭터 배치 결정을 실제로 연다. 범위는 입력과 상태 모델까지이며 Rigidbody2D 모터, 충돌 질의, 점프 물리, MAP 연동, inputactions 자산은 만들지 않는다.

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
11. `MCP/REPORTS/CHAR00_03_CHAR00_BASELINE_EXIT_AUDIT_RESULT.md`
12. `MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md`
13. `01_FIXED_SPEC/02_CHARACTER_INPUT_RULES.md`
14. `01_FIXED_SPEC/03_CHARACTER_MOVEMENT_RULES.md`
15. `01_FIXED_SPEC/07_CHARACTER_TEST_RULES.md`
16. `03_DATA_SCHEMA/CHARACTER_ACTION_SCHEMA.md`
17. `03_DATA_SCHEMA/CHARACTER_MOVEMENT_TUNING_SCHEMA.md`
18. `04_TEST_FIXTURES/MOVEMENT_COURSE_SPEC.md`

## READ ALLOWLIST

본문 읽기 허용:

```text
CharacterDesign/**
Packages/manifest.json
ProjectSettings/ProjectSettings.asset
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
Assets/_Game/Tests/EditMode/Map/**/*.cs
Assets/_Legacy/StarNight/Scripts/Runtime/Player/PlayerInputAdapter.cs
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

위 검색은 존재 여부, 파일명, asmdef JSON 경계 확인에만 사용한다.

## WRITE ALLOWLIST

### Runtime

다음 디렉터리, 대응 Unity folder `.meta`, 정확한 런타임 파일만 생성 또는 수정할 수 있다.

```text
Assets/_Game/Character/
Assets/_Game/Character/Runtime/
Assets/_Game/Character/Runtime/Input/
Assets/_Game/Character/Runtime/State/
Assets/_Game/Character/Runtime/Game.Character.Runtime.asmdef
Assets/_Game/Character/Runtime/Input/CharacterActionId.cs
Assets/_Game/Character/Runtime/Input/CharacterButtonSnapshot.cs
Assets/_Game/Character/Runtime/Input/CharacterInputSnapshot.cs
Assets/_Game/Character/Runtime/Input/CharacterInputBuffer.cs
Assets/_Game/Character/Runtime/Input/CharacterInputLockSet.cs
Assets/_Game/Character/Runtime/State/CharacterFacingDirection.cs
Assets/_Game/Character/Runtime/State/CharacterLocomotionState.cs
Assets/_Game/Character/Runtime/State/CharacterPlayerState.cs
Assets/_Game/Character/Runtime/State/CharacterPlayerStateSnapshot.cs
```

### EditMode Tests

다음 디렉터리, 대응 Unity folder `.meta`, 정확한 테스트 파일만 생성 또는 수정할 수 있다.

```text
Assets/_Game/Tests/EditMode/Character/
Assets/_Game/Tests/EditMode/Character/Game.Character.Tests.EditMode.asmdef
Assets/_Game/Tests/EditMode/Character/CharacterInputSnapshotTests.cs
Assets/_Game/Tests/EditMode/Character/CharacterInputBufferTests.cs
Assets/_Game/Tests/EditMode/Character/CharacterPlayerStateTests.cs
```

Unity가 위 `.asmdef`와 `.cs` 파일에 생성하는 대응 `.meta`도 허용한다.

### Report

```text
CharacterDesign/MCP/REPORTS/CHAR01_01_IMPLEMENT_INPUT_SNAPSHOT_AND_PLAYER_STATES_RESULT.md
```

TASK EXECUTION 중 `06_IMPLEMENTATION_STATUS.md`와 `MASTER_IMPLEMENTATION_TASK_LIST.md`는 수정하지 않는다. 상태 변경은 Result PASS 이후 STATUS FINALIZE Phase만 수행한다.

## DO NOT

- `.inputactions`, InputActionAsset, Input System wrapper 생성·수정
- `UnityEngine.InputSystem` compile-time dependency 추가
- Rigidbody2D, Collider2D, Physics2D, Tilemap, Animator, Scene, Prefab 생성·수정
- 실제 이동 motor, 충돌 질의, 점프 속도, 중력, 낙하, 착지 구현
- MAP runtime/API 수정, MAP 좌표/셀 크기 상수 복제
- 일반 공격 action/state 추가
- wall jump, dash, double jump 관련 타입·상태 추가
- PlayMode 테스트 생성·실행
- `Assets/_Legacy/**`, `Assets/_Game/Map/**`, `Packages/**`, `ProjectSettings/**`, `MapDesign/**` 수정
- 기존 파일·폴더 삭제/이동/이름 변경
- 관련 없는 warning 수정, formatting sweep, package install
- git commit/push/branch/reset/rebase/force
- CHAR01_02 선행 작업

## Approved Assembly Contract

`CHAR00_03` exit audit로 CHAR01_01이 첫 캐릭터 코드 배치를 소유한다. 신규 asmdef 도입은 이 Task의 정확한 두 파일로만 승인된다.

`Game.Character.Runtime.asmdef`:

```text
name: Game.Character.Runtime
rootNamespace: StarNight.Character
references: []
autoReferenced: true
```

`Game.Character.Tests.EditMode.asmdef`:

```text
name: Game.Character.Tests.EditMode
rootNamespace: StarNight.Character.Tests
references include Game.Character.Runtime
defineConstraints include UNITY_INCLUDE_TESTS
```

테스트 assembly는 프로젝트의 기존 Unity Test Framework convention에 맞춰 필요한 test runner references만 추가한다. stale `Game.Stage.Runtime` 참조를 복사하지 않는다.

## Runtime Contract

### Namespace

Runtime namespace는 아래 둘만 사용한다.

```text
StarNight.Character.Input
StarNight.Character.State
```

Tests namespace는 아래 하나를 사용한다.

```text
StarNight.Character.Tests
```

### Required Input Types

`CharacterActionId`:

- 필수 값: `Jump`, `Action`, `SafeDrop`, `Bomb`, `Rope`
- 금지 값: `Attack`, `BasicAttack`, `Melee`, `Shoot`
- `SafeDrop`은 아래 방향 + `Action` 조합의 논리 action이며 별도 장치 버튼이 아니다.

`CharacterButtonSnapshot`:

- `pressedThisFrame`
- `held`
- `releasedThisFrame`
- `consumed`
- `timestamp` 또는 `tick`

`CharacterInputSnapshot`:

- horizontal movement intent를 표현한다.
- down held 상태를 표현한다.
- Jump, Action, Bomb, Rope의 button snapshot을 보유한다.
- Down+Action 조합에서 `SafeDrop` intent를 계산할 수 있다.
- Action과 SafeDrop이 같은 tick에 동시에 요청되면 `SafeDrop`이 우선한다.
- 별도 일반 공격 intent를 노출하지 않는다.

`CharacterInputBuffer`:

- Update/render frame에서 수집한 `pressedThisFrame`이 다음 physics tick 전 소실되지 않는다.
- action별 buffer window를 받을 수 있다.
- 소비된 action은 같은 tick에서 중복 반환되지 않는다.
- 만료된 action은 반환되지 않는다.
- `SafeDrop` 소비는 같은 source의 `Action` press를 중복 소비하지 못하게 한다.

`CharacterInputLockSet`:

- 단일 bool이 아니라 reason set이다.
- reason 추가/제거/조회가 가능하다.
- lock reason 하나를 제거해도 다른 reason이 남아 있으면 locked 상태를 유지한다.
- 카메라룸 전환 자체를 입력 lock reason으로 추가하지 않는다.

### Required State Types

`CharacterFacingDirection`:

- 필수 값: `Left`, `Right`
- horizontal input이 0이면 기존 facing을 유지한다.

`CharacterLocomotionState`:

- 필수 값: `Grounded`, `Airborne`
- 점프 물리, 중력, 충돌 질의는 이 Task에서 구현하지 않는다.

`CharacterPlayerState`:

- facing, locomotion, carry flag, stun flag, dead flag, input lock set을 추적한다.
- dead 또는 stunned이면 input을 받을 수 없다.
- lock reason이 있으면 input을 받을 수 없다.
- 카메라룸 전환만으로 input을 차단하지 않는다.
- movement motor가 사용할 수 있는 immutable snapshot을 만들 수 있다.

`CharacterPlayerStateSnapshot`:

- frame/tick decision에 사용할 readonly 값 객체다.
- Animator, sound, render frame 성공 여부에 의존하지 않는다.

## Implementation Steps

1. Current Task가 `CHAR01_01_IMPLEMENT_INPUT_SNAPSHOT_AND_PLAYER_STATES`인지 확인한다.
2. `CHAR00_03` REPORT가 `STATUS: PASS`, `CHAR00 EXIT: APPROVED`, `CHAR01_01 ENTRY: ELIGIBLE FOR SEPARATE PATCH`인지 확인한다.
3. 작업 전 변경 파일 경로를 기록하고 기존 사용자 변경은 수정하지 않는다.
4. 기존 asmdef와 NUnit convention을 확인한다.
5. 승인된 Character runtime/test 디렉터리와 필요한 `.meta`를 생성한다.
6. 승인된 두 asmdef가 없으면 생성하고, 이미 있으면 계약과 충돌하지 않는지 확인한다.
7. Required Input Types를 구현한다.
8. Required State Types를 구현한다.
9. 지정된 EditMode 테스트 3개를 구현한다.
10. Unity Asset Refresh와 compile 완료를 기다린다.
11. 지정된 Character EditMode 테스트만 실행한다.
12. 작업 후 변경 파일 경로가 WRITE ALLOWLIST와 REPORT뿐인지 확인한다.
13. Result를 작성한다.
14. 모든 DONE CONDITIONS가 PASS일 때만 `STATUS: PASS`를 기록한다.

## Required Tests

### `CharacterInputSnapshotTests`

필수 test case:

```text
CharacterActionId_DoesNotContainBasicAttack
Snapshot_ReportsSafeDropWhenDownAndActionPressed
Snapshot_PrioritizesSafeDropOverPlainAction
Snapshot_KeepsJumpBombAndRopeAsSeparateActions
```

### `CharacterInputBufferTests`

필수 test case:

```text
PressedAction_SurvivesUntilFirstPhysicsTick
ConsumedAction_IsNotReturnedTwiceInSameTick
ExpiredAction_IsNotReturned
SafeDropConsumption_DoesNotAlsoReturnPlainAction
```

### `CharacterPlayerStateTests`

필수 test case:

```text
InputLocks_AreReasonSetAndClearIndependently
CameraRoomTransition_DoesNotCreateInputLock
StateSnapshot_TracksFacingLocomotionCarryStunAndDeath
DeadOrStunnedState_CannotAcceptInput
```

테스트 이름은 위와 정확히 일치해야 한다. 추가 test case는 허용되지만 위 12개는 반드시 존재하고 PASS해야 한다.

## Unity Verification

- Unity Version: 6000.3.8f1
- Asset Refresh: PASS
- Compile Errors: 0
- Relevant New Warnings: 0
- Targeted EditMode Tests: PASS
- PlayMode Tests: NOT RUN
- Scene/Prefab Changes: 0

컴파일 오류가 있으면 테스트 미실행이라도 `STATUS: FAIL` 또는 `STATUS: BLOCKED`다.

## Result File

```text
REPORTS/CHAR01_01_IMPLEMENT_INPUT_SNAPSHOT_AND_PLAYER_STATES_RESULT.md
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

- [ ] `CHAR00_03` PASS와 CHAR01_01 진입 승인이 검증됐다.
- [ ] 승인된 Character runtime/test 경로와 정확한 두 asmdef만 열렸다.
- [ ] Required Input Types가 구현됐다.
- [ ] Required State Types가 구현됐다.
- [ ] SafeDrop 우선순위와 action 소비 중복 방지가 테스트됐다.
- [ ] 입력 lock이 reason set으로 동작하고 카메라룸 전환이 lock이 아님이 테스트됐다.
- [ ] 일반 공격, wall jump, dash, double jump 관련 action/state가 없다.
- [ ] inputactions, Rigidbody2D, Collider2D, Physics2D, Tilemap, Animator, Scene, Prefab 변경이 없다.
- [ ] 지정된 12개 EditMode test case가 전부 PASS했다.
- [ ] Unity compile error와 relevant new warning이 0이다.
- [ ] REPORT 외 하네스 상태 파일은 Task execution 중 수정하지 않았다.
- [ ] CHAR01_02를 시작하지 않았다.

## Completion Rule

Task는 runtime/test 파일과 REPORT만 작성한다. PASS 후 `08_STATUS_FINALIZE_RULES.md`가 CHAR01_01을 COMPLETE, Current Task를 NONE으로 변경한다. `CHAR01_02_IMPLEMENT_COLLISION_QUERIES_AND_GROUND_MOTOR`는 LOCKED로 유지하고 자동 시작하지 않는다.
