# CHAR02_02 — Validate Three-Cell Failure and Forbidden Movement

```yaml
status_control:
  task_key: CHAR02_02_VALIDATE_THREE_CELL_FAILURE_AND_FORBIDDEN_MOVEMENT
  result_file: REPORTS/CHAR02_02_VALIDATE_THREE_CELL_FAILURE_AND_FORBIDDEN_MOVEMENT_RESULT.md
```

## TASK TYPE

EDITMODE MOVEMENT RULE VALIDATION

## Objective

CHAR02_01의 test-only MovementCourses 검증 위에 다음 두 이동 문법을 고정한다.

```text
동일 높이 3셀 틈은 기본 이동만으로 통과할 수 없다.
wall jump, dash, double jump는 런타임 action/state/type/member로 존재하지 않는다.
```

이번 Task는 3셀 실패와 금지 이동 부재만 검증한다. CHAR02 전체 종료 감사는 CHAR02_03에서 수행한다.

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
11. `MCP/REPORTS/CHAR02_01_VALIDATE_TWO_CELL_HEIGHT_AND_GAP_RULES_RESULT.md`
12. `MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md`
13. `01_FIXED_SPEC/01_CHARACTER_GAMEPLAY_RULES.md`
14. `01_FIXED_SPEC/02_CHARACTER_INPUT_RULES.md`
15. `01_FIXED_SPEC/03_CHARACTER_MOVEMENT_RULES.md`
16. `01_FIXED_SPEC/07_CHARACTER_TEST_RULES.md`
17. `01_FIXED_SPEC/08_IMPLEMENTATION_ORDER.md`
18. `03_DATA_SCHEMA/CHARACTER_ACTION_SCHEMA.md`
19. `03_DATA_SCHEMA/CHARACTER_MOVEMENT_TUNING_SCHEMA.md`
20. `04_TEST_FIXTURES/MOVEMENT_COURSE_SPEC.md`

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
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

기존 테스트 스타일 확인은 `Assets/_Game/Tests/EditMode/Character/**` 안의 관련 파일만 읽는다.

제한적 검색 허용:

```text
Assets/_Game/Character/**
Assets/_Game/Tests/EditMode/Character/**
Assets/_Game/**/*.asmdef
Assets/_Game/**/*.asmref
Assets/**/*.inputactions
```

## WRITE ALLOWLIST

### EditMode Test-Only Course Validation

다음 기존 test-only support 파일은 3셀 코스와 실패 reason 기록을 위해 필요한 최소 수정만 허용한다.

```text
Assets/_Game/Tests/EditMode/Character/MovementCourses/CharacterMovementCourseConstants.cs
Assets/_Game/Tests/EditMode/Character/MovementCourses/CharacterMovementCourseResult.cs
Assets/_Game/Tests/EditMode/Character/MovementCourses/CharacterMovementCourseSimulator.cs
```

다음 정확한 테스트 파일만 새로 생성할 수 있다.

```text
Assets/_Game/Tests/EditMode/Character/MovementCourses/ThreeCellGapFailureCourseTests.cs
Assets/_Game/Tests/EditMode/Character/MovementCourses/ForbiddenMovementRuleTests.cs
```

Unity가 위 신규 `.cs` 파일에 생성하는 대응 `.meta`도 허용한다.

### Report

```text
CharacterDesign/MCP/REPORTS/CHAR02_02_VALIDATE_THREE_CELL_FAILURE_AND_FORBIDDEN_MOVEMENT_RESULT.md
```

TASK EXECUTION 중 `06_IMPLEMENTATION_STATUS.md`와 `MASTER_IMPLEMENTATION_TASK_LIST.md`는 수정하지 않는다. 상태 변경은 Result PASS 이후 STATUS FINALIZE Phase만 수행한다.

## DO NOT

- Runtime C# 생성·수정
- 기존 TwoCellHeight/TwoCellGap 테스트 결과 완화
- asmdef/asmref/inputactions 생성·수정
- Scene, Prefab, Animator, ScriptableObject, material, sprite 생성·수정
- MAP runtime/API 수정, Tilemap 직접 접근, MAP 좌표/셀 크기 상수 복제
- wall jump, dash, double jump 관련 타입·상태·입력 추가
- 일반 공격 action/state 추가
- PlayMode 테스트 생성·실행
- `Assets/_Legacy/**`, `Assets/_Game/Map/**`, `Packages/**`, `ProjectSettings/**`, `MapDesign/**` 수정
- 기존 파일·폴더 삭제/이동/이름 변경
- 관련 없는 warning 수정, formatting sweep, package install
- git commit/push/branch/reset/rebase/force
- CHAR02_03 선행 감사 작업

## Validation Contract

### Three-Cell Same-Level Gap Failure

검증할 결과:

```text
동일 높이 시작/도착 플랫폼 사이 3.0 world unit 빈 틈은 기본 run + single jump movement로 통과하지 못한다.
기본 입력은 CHAR02_01의 2셀 틈 코스와 같은 movement core path를 사용한다.
실패 조건은 opposite edge를 통과하지 못하거나, 통과하더라도 도착 플랫폼에 착지 가능한 bottom/grounded 상태를 얻지 못하는 것이다.
실패 reason, final x, peak bottom, gap 구간 최저 bottom, landing 여부를 REPORT에 기록한다.
```

필수 test case:

```text
ThreeCellGapCourse_BasicMovementDoesNotClearSameLevelThreeCellGap
ThreeCellGapCourse_UsesSameCorePathAsTwoCellGapCourse
ThreeCellGapCourse_RecordsDeterministicFailureReason
ThreeCellGapCourse_DoesNotChangeTwoCellPassResult
```

### Forbidden Movement Absence

검증할 결과:

```text
wall jump, dash, double jump가 action/state/type/member 이름으로 존재하지 않는다.
basic attack, melee, shoot가 action/state/type/member 이름으로 존재하지 않는다.
CharacterActionId는 Jump/Action/SafeDrop/Bomb/Rope의 5개 값만 유지한다.
공중 상태에서 second jump가 불가능한 기존 CHAR01 테스트가 계속 PASS한다.
```

필수 test case:

```text
ForbiddenMovement_NoWallJumpDashOrDoubleJumpTypesOrMembers
ForbiddenMovement_NoBasicAttackMeleeOrShootActions
ForbiddenMovement_CharacterActionIdRemainsLockedToFiveValues
ForbiddenMovement_SecondJumpStillFailsBeforeGroundedAgain
```

### Regression

- `Game.Character.Tests.EditMode` 전체를 실행한다.
- 기존 CHAR01 필수 36개와 CHAR02_01 필수 8개가 계속 PASS해야 한다.
- 신규 CHAR02_02 필수 8개가 PASS해야 한다.
- 총 test count는 최소 52개여야 한다.

## Implementation Steps

1. Current Task가 `CHAR02_02_VALIDATE_THREE_CELL_FAILURE_AND_FORBIDDEN_MOVEMENT`인지 확인한다.
2. `CHAR02_01` REPORT가 `STATUS: PASS`이고 Current Task after finalize가 NONE인지 확인한다.
3. 작업 전 변경 파일 경로를 기록하고 기존 사용자 변경은 수정하지 않는다.
4. CHAR02_01 MovementCourses support와 기존 Character EditMode test convention을 확인한다.
5. 필요한 경우 constants/result/simulator에 3셀 코스와 실패 reason 기록을 최소 추가한다.
6. Three-cell failure 필수 4개 test case를 구현한다.
7. Forbidden movement 필수 4개 test case를 구현한다.
8. Unity Asset Refresh와 compile 완료를 기다린다.
9. `Game.Character.Tests.EditMode` 전체 EditMode 테스트를 실행한다.
10. 작업 후 변경 파일 경로가 WRITE ALLOWLIST와 REPORT뿐인지 확인한다.
11. Result를 작성한다.
12. 모든 DONE CONDITIONS가 PASS일 때만 `STATUS: PASS`를 기록한다.

## Unity Verification

- Unity Version: 6000.3.8f1
- Asset Refresh: PASS
- Compile Errors: 0
- Relevant New Warnings: 0
- Targeted EditMode Tests: PASS (`Game.Character.Tests.EditMode` 전체)
- Existing CHAR01 Tests: PASS (36/36)
- Existing CHAR02_01 Tests: PASS (8/8)
- Required CHAR02_02 Tests: PASS (8/8)
- Total Character EditMode Tests: PASS (at least 52)
- PlayMode Tests: NOT RUN
- Scene/Prefab Changes: 0

컴파일 오류가 있으면 테스트 미실행이라도 `STATUS: FAIL` 또는 `STATUS: BLOCKED`다.

## Result File

```text
REPORTS/CHAR02_02_VALIDATE_THREE_CELL_FAILURE_AND_FORBIDDEN_MOVEMENT_RESULT.md
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
COURSE RESULTS
FORBIDDEN MOVEMENT SCAN
TEST
UNITY
SCOPE VALIDATION
OUT_OF_SCOPE_FINDINGS
DONE CONDITIONS
NEXT
```

## DONE CONDITIONS

- [ ] `CHAR02_01` PASS와 CHAR02_02 진입 상태가 검증됐다.
- [ ] test-only MovementCourses 파일만 추가 또는 최소 수정됐다.
- [ ] 동일 높이 3셀 틈 기본 이동 실패가 검증됐다.
- [ ] 실패 reason과 deterministic result가 기록됐다.
- [ ] 2셀 높이/2셀 틈 기존 결과가 계속 PASS한다.
- [ ] wall jump, dash, double jump 관련 action/state/type/member가 없다.
- [ ] basic attack, melee, shoot 관련 action/state/type/member가 없다.
- [ ] Runtime, asmdef, inputactions, Scene, Prefab, MAP, Packages, ProjectSettings 변경이 없다.
- [ ] 기존 44개와 신규 CHAR02_02 8개 EditMode test case가 전부 PASS했다.
- [ ] Unity compile error와 relevant new warning이 0이다.
- [ ] REPORT 외 하네스 상태 파일은 Task execution 중 수정하지 않았다.
- [ ] CHAR02_03을 시작하지 않았다.

## Completion Rule

Task는 test-only course validation 파일과 REPORT만 작성한다. PASS 후 `08_STATUS_FINALIZE_RULES.md`가 CHAR02_02를 COMPLETE, Current Task를 NONE으로 변경한다. `CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT`는 LOCKED로 유지하고 자동 시작하지 않는다.
