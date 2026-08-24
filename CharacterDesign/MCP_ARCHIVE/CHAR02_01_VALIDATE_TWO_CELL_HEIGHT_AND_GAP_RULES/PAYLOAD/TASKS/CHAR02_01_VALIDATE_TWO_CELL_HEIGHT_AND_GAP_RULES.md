# CHAR02_01 — Validate Two-Cell Height and Gap Rules

```yaml
status_control:
  task_key: CHAR02_01_VALIDATE_TWO_CELL_HEIGHT_AND_GAP_RULES
  result_file: REPORTS/CHAR02_01_VALIDATE_TWO_CELL_HEIGHT_AND_GAP_RULES_RESULT.md
```

## TASK TYPE

EDITMODE MOVEMENT RULE VALIDATION

## Objective

CHAR01에서 완성한 순수 이동 코어를 고정 test-only 코스 시뮬레이션에 연결해 다음 두 이동 문법을 검증한다.

```text
기본 점프로 2셀 높이 발판에 도달 가능
달리기 기반 이동으로 동일 높이 2셀 틈 통과 가능
```

이번 Task는 2셀 높이와 2셀 틈만 검증한다. 3셀 틈 실패, wall jump/dash/double jump 부재 재검증은 CHAR02_02에서 수행한다.

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
11. `MCP/REPORTS/CHAR01_04_CHAR01_CORE_MOVEMENT_EXIT_AUDIT_RESULT.md`
12. `MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md`
13. `01_FIXED_SPEC/01_CHARACTER_GAMEPLAY_RULES.md`
14. `01_FIXED_SPEC/02_CHARACTER_INPUT_RULES.md`
15. `01_FIXED_SPEC/03_CHARACTER_MOVEMENT_RULES.md`
16. `01_FIXED_SPEC/07_CHARACTER_TEST_RULES.md`
17. `01_FIXED_SPEC/08_IMPLEMENTATION_ORDER.md`
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

다음 디렉터리, 대응 Unity folder `.meta`, 정확한 테스트 파일만 생성 또는 수정할 수 있다.

```text
Assets/_Game/Tests/EditMode/Character/MovementCourses/
Assets/_Game/Tests/EditMode/Character/MovementCourses/CharacterMovementCourseConstants.cs
Assets/_Game/Tests/EditMode/Character/MovementCourses/CharacterMovementCourseResult.cs
Assets/_Game/Tests/EditMode/Character/MovementCourses/CharacterMovementCourseSimulator.cs
Assets/_Game/Tests/EditMode/Character/MovementCourses/TwoCellHeightCourseTests.cs
Assets/_Game/Tests/EditMode/Character/MovementCourses/TwoCellGapCourseTests.cs
```

Unity가 위 신규 폴더와 `.cs` 파일에 생성하는 대응 `.meta`도 허용한다.

### Report

```text
CharacterDesign/MCP/REPORTS/CHAR02_01_VALIDATE_TWO_CELL_HEIGHT_AND_GAP_RULES_RESULT.md
```

TASK EXECUTION 중 `06_IMPLEMENTATION_STATUS.md`와 `MASTER_IMPLEMENTATION_TASK_LIST.md`는 수정하지 않는다. 상태 변경은 Result PASS 이후 STATUS FINALIZE Phase만 수행한다.

## DO NOT

- Runtime C# 생성·수정
- 기존 Character test 파일 수정
- asmdef/asmref/inputactions 생성·수정
- Scene, Prefab, Animator, ScriptableObject, material, sprite 생성·수정
- MAP runtime/API 수정, Tilemap 직접 접근, MAP 좌표/셀 크기 상수 복제
- 3셀 틈 실패 검증 구현
- wall jump, dash, double jump 관련 타입·상태·입력 추가
- 일반 공격 action/state 추가
- PlayMode 테스트 생성·실행
- `Assets/_Legacy/**`, `Assets/_Game/Map/**`, `Packages/**`, `ProjectSettings/**`, `MapDesign/**` 수정
- 기존 파일·폴더 삭제/이동/이름 변경
- 관련 없는 warning 수정, formatting sweep, package install
- git commit/push/branch/reset/rebase/force
- CHAR02_02 선행 작업

## Validation Contract

### Course Units

- test-only course는 `1 logical cell = 1 world unit`을 명시적으로 기록한다.
- 이 기록은 테스트 fixture의 검증 상수이며 runtime MAP 좌표 또는 셀 크기 소스가 아니다.
- player collider baseline은 runtime `CharacterCapsuleGeometry.Default`에서 읽어 검증한다.
- 코스 시뮬레이션은 EditMode 순수 C#으로 실행하며 Scene, Prefab, Rigidbody2D, Tilemap, Animator에 의존하지 않는다.

### Two-Cell Height Course

검증할 결과:

```text
기본 Jump 입력 1회로 player bottom이 시작 플랫폼 기준 +2.0 world unit 높이에 도달한다.
최대 높이 판정은 collider bottom 또는 support contact 기준으로 기록한다.
필요한 도달 시간, 최고 높이, 사용된 jumpVelocity/riseGravity를 REPORT에 기록한다.
```

필수 test case:

```text
TwoCellHeightCourse_UsesOneWorldUnitCellsAndLockedCapsule
TwoCellHeightCourse_BasicJumpReachesTwoCellPlatformHeight
TwoCellHeightCourse_UsesSingleJumpInputOnly
TwoCellHeightCourse_DoesNotRequireSceneOrTilemap
```

### Two-Cell Same-Level Gap Course

검증할 결과:

```text
동일 높이 시작/도착 플랫폼 사이 2.0 world unit 빈 틈을 run-speed 기반 이동으로 통과한다.
시뮬레이션은 CHAR01 ground/air/jump/gravity 코어를 사용한다.
최소 성공 조건은 player bottom이 도착 플랫폼 높이 이상을 유지하며 opposite edge를 통과하고 착지 가능한 x 위치에 도달하는 것이다.
총 소요 시간, 최고 높이, 최종 x, 착지 여부를 REPORT에 기록한다.
```

필수 test case:

```text
TwoCellGapCourse_RunSpeedClearsSameLevelTwoCellGap
TwoCellGapCourse_RecordsDeterministicFrameTolerance
TwoCellGapCourse_UsesCharacterMovementCoreNotHardcodedTrajectory
TwoCellGapCourse_DoesNotValidateThreeCellFailureYet
```

### Regression

- `Game.Character.Tests.EditMode` 전체를 실행한다.
- 기존 CHAR01 필수 36개 test case가 계속 PASS해야 한다.
- 신규 CHAR02_01 필수 8개 test case가 PASS해야 한다.
- 총 test count는 최소 44개여야 한다.

## Implementation Steps

1. Current Task가 `CHAR02_01_VALIDATE_TWO_CELL_HEIGHT_AND_GAP_RULES`인지 확인한다.
2. `CHAR01_04` REPORT가 `STATUS: PASS`, `CHAR01 EXIT: APPROVED`, `CHAR02_01 ENTRY: ELIGIBLE FOR SEPARATE PATCH`인지 확인한다.
3. 작업 전 변경 파일 경로를 기록하고 기존 사용자 변경은 수정하지 않는다.
4. CHAR01 movement runtime API와 기존 Character EditMode test convention을 확인한다.
5. `MovementCourses` test-only 디렉터리와 필요한 `.meta`를 생성한다.
6. course constants/result/simulator를 구현한다.
7. Two-cell height 필수 4개 test case를 구현한다.
8. Two-cell gap 필수 4개 test case를 구현한다.
9. Unity Asset Refresh와 compile 완료를 기다린다.
10. `Game.Character.Tests.EditMode` 전체 EditMode 테스트를 실행한다.
11. 작업 후 변경 파일 경로가 WRITE ALLOWLIST와 REPORT뿐인지 확인한다.
12. Result를 작성한다.
13. 모든 DONE CONDITIONS가 PASS일 때만 `STATUS: PASS`를 기록한다.

## Unity Verification

- Unity Version: 6000.3.8f1
- Asset Refresh: PASS
- Compile Errors: 0
- Relevant New Warnings: 0
- Targeted EditMode Tests: PASS (`Game.Character.Tests.EditMode` 전체)
- Existing CHAR01 Tests: PASS (36/36)
- Required CHAR02_01 Tests: PASS (8/8)
- Total Character EditMode Tests: PASS (at least 44)
- PlayMode Tests: NOT RUN
- Scene/Prefab Changes: 0

컴파일 오류가 있으면 테스트 미실행이라도 `STATUS: FAIL` 또는 `STATUS: BLOCKED`다.

## Result File

```text
REPORTS/CHAR02_01_VALIDATE_TWO_CELL_HEIGHT_AND_GAP_RULES_RESULT.md
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
TEST
UNITY
SCOPE VALIDATION
OUT_OF_SCOPE_FINDINGS
DONE CONDITIONS
NEXT
```

## DONE CONDITIONS

- [ ] `CHAR01_04` PASS와 CHAR02_01 진입 승인이 검증됐다.
- [ ] test-only MovementCourses 파일만 추가됐다.
- [ ] 1 world unit cell과 0.72 x 0.90 capsule baseline이 검증됐다.
- [ ] 2셀 높이 기본 점프 도달이 검증됐다.
- [ ] 동일 높이 2셀 틈 통과가 검증됐다.
- [ ] 시뮬레이션이 CHAR01 movement core를 사용하고 하드코딩 궤적이 아니다.
- [ ] 3셀 틈 실패, wall jump, dash, double jump 검증은 구현하지 않았다.
- [ ] Runtime, asmdef, inputactions, Scene, Prefab, MAP, Packages, ProjectSettings 변경이 없다.
- [ ] 기존 CHAR01 36개와 신규 CHAR02_01 8개 EditMode test case가 전부 PASS했다.
- [ ] Unity compile error와 relevant new warning이 0이다.
- [ ] REPORT 외 하네스 상태 파일은 Task execution 중 수정하지 않았다.
- [ ] CHAR02_02를 시작하지 않았다.

## Completion Rule

Task는 test-only course validation 파일과 REPORT만 작성한다. PASS 후 `08_STATUS_FINALIZE_RULES.md`가 CHAR02_01을 COMPLETE, Current Task를 NONE으로 변경한다. `CHAR02_02_VALIDATE_THREE_CELL_FAILURE_AND_FORBIDDEN_MOVEMENT`는 LOCKED로 유지하고 자동 시작하지 않는다.
