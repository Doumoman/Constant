# CHAR01_04 — CHAR01 Core Movement Exit Audit

```yaml
status_control:
  task_key: CHAR01_04_CHAR01_CORE_MOVEMENT_EXIT_AUDIT
  result_file: REPORTS/CHAR01_04_CHAR01_CORE_MOVEMENT_EXIT_AUDIT_RESULT.md
```

## TASK TYPE

AUDIT

## Objective

CHAR01_01~03에서 구현한 캐릭터 핵심 이동 코어를 교차 감사해 CHAR02 이동 문법 검증 단계로 넘어갈 수 있는지 판정한다.

이번 Task는 읽기 전용 감사다. Runtime, Test, Assets, asmdef, inputactions, MAP, Scene, Prefab, Packages, ProjectSettings를 수정하지 않는다.

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
12. `MCP/REPORTS/CHAR01_02_IMPLEMENT_COLLISION_QUERIES_AND_GROUND_MOTOR_RESULT.md`
13. `MCP/REPORTS/CHAR01_03_IMPLEMENT_JUMP_AIR_CONTROL_AND_LANDING_RESULT.md`
14. `MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md`
15. `01_FIXED_SPEC/01_CHARACTER_GAMEPLAY_RULES.md`
16. `01_FIXED_SPEC/02_CHARACTER_INPUT_RULES.md`
17. `01_FIXED_SPEC/03_CHARACTER_MOVEMENT_RULES.md`
18. `01_FIXED_SPEC/07_CHARACTER_TEST_RULES.md`
19. `01_FIXED_SPEC/08_IMPLEMENTATION_ORDER.md`
20. `03_DATA_SCHEMA/CHARACTER_ACTION_SCHEMA.md`
21. `03_DATA_SCHEMA/CHARACTER_MOVEMENT_TUNING_SCHEMA.md`
22. `04_TEST_FIXTURES/MOVEMENT_COURSE_SPEC.md`

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

제한적 검색 허용:

```text
Assets/_Game/Character/**
Assets/_Game/Tests/EditMode/Character/**
Assets/_Game/**/*.asmdef
Assets/_Game/**/*.asmref
Assets/**/*.inputactions
Assets/**/*.unity
Assets/**/*.prefab
```

Scene/Prefab은 경로와 변경 여부만 확인한다. YAML 본문은 읽지 않는다.

## WRITE ALLOWLIST

```text
CharacterDesign/MCP/REPORTS/CHAR01_04_CHAR01_CORE_MOVEMENT_EXIT_AUDIT_RESULT.md
```

`06_IMPLEMENTATION_STATUS.md`와 `MASTER_IMPLEMENTATION_TASK_LIST.md`는 TASK EXECUTION에서 수정하지 않는다.

## DO NOT

- Runtime/Test C# 생성·수정
- asmdef/asmref/inputactions 생성·수정
- Scene, Prefab, Animator, ScriptableObject, material, sprite 생성·수정
- MAP runtime/API 수정, Tilemap 직접 접근, MAP 좌표/셀 크기 상수 복제
- 2셀 높이 도달, 2셀 틈 통과, 3셀 틈 실패 코스 검증 구현
- wall jump, dash, double jump 관련 타입·상태·입력 추가
- 일반 공격 action/state 추가
- PlayMode 테스트 생성·실행
- `Assets/_Legacy/**`, `Assets/_Game/Map/**`, `Packages/**`, `ProjectSettings/**`, `MapDesign/**` 수정
- 기존 파일·폴더 삭제/이동/이름 변경
- 관련 없는 warning 수정, formatting sweep, package install
- git commit/push/branch/reset/rebase/force
- CHAR02 선행 작업

## Required Audit

### 1. 선행 증빙과 상태

- CHAR01_01, CHAR01_02, CHAR01_03 REPORT가 각각 정확히 `STATUS: PASS`인지 확인한다.
- 실행 시 CHAR00_01~03, CHAR01_01~03은 COMPLETE, CHAR01_04만 CURRENT, CHAR02_01 이후는 LOCKED인지 확인한다.
- source registry marker가 `REGISTRY_STATE: FILLED_BY_CHAR00_01`인지 확인한다.

### 2. Runtime assembly와 경계

- `Game.Character.Runtime`과 `Game.Character.Tests.EditMode` assembly가 존재하고 CHAR01_01 이후 이름·rootNamespace·reference 경계가 깨지지 않았는지 확인한다.
- Character runtime namespace가 Input, State, Movement 경계 안에 있는지 확인한다.
- Runtime이 MAP Tilemap internals, generated map internals, Scene object lookup에 의존하지 않는지 확인한다.

### 3. 핵심 이동 계약 커버리지

다음 구현 단위가 존재하고 서로 충돌하지 않아야 한다.

```text
input snapshot / input buffer / input lock reason set
player state snapshot / facing / locomotion state
collision query abstraction / Physics2D adapter
capsule 0.72 x 0.90 baseline / ground probe 0.08 / rising velocity gate 0.05
ground walk/run acceleration and deceleration
jump buffer 0.12 / coyote time 0.10 / single jump consumption
variable jump release / rise-fall gravity / max fall speed
airborne-only air control / landing transition / jump consumed reset
```

이번 감사는 2셀/3셀 코스 결과를 PASS로 판정하지 않는다. 그 검증은 CHAR02 소관이다.

### 4. 회귀 테스트

- `Game.Character.Tests.EditMode` 전체를 실행한다.
- CHAR01_01 필수 12개, CHAR01_02 필수 12개, CHAR01_03 필수 12개가 모두 PASS해야 한다.
- 테스트 이름을 바꾸거나 Ignore, Explicit, 조건부 조기 return으로 숨기면 FAIL이다.

### 5. 금지 기능과 변경 범위

- 일반 공격, wall jump, dash, double jump 관련 action/state/type/member가 없어야 한다.
- inputactions, asmdef, Scene, Prefab, Animator, ScriptableObject, Packages, ProjectSettings 변경이 없어야 한다.
- REPORT 외 하네스 상태 파일을 Task execution 중 수정하지 않아야 한다.

### 6. 의존성 장부

다음을 명확히 분리한다.

```text
CHAR02 entry blocker: 2-cell height / 2-cell gap / 3-cell fail course validation can start from current pure movement core
CHAR03 deferred dependency: MAP world query / terrain mutation request / room boundary gate / room readiness API remains deferred
out-of-scope: stale Map PlayMode asmdef reference
```

CHAR03 의존성은 CHAR02 이동 문법 검증 시작을 막지 않으면 CHAR01 EXIT 차단 사유가 아니다.

### 7. Exit 판정

모든 gate가 PASS면 REPORT에 정확히 다음을 기록한다.

```text
CHAR01 EXIT: APPROVED
CHAR02_01 ENTRY: ELIGIBLE FOR SEPARATE PATCH
```

어느 gate라도 실패하면:

```text
CHAR01 EXIT: REJECTED
CHAR02_01 ENTRY: BLOCKED
```

## Tests

| Gate | PASS 조건 |
|---|---|
| PriorEvidenceAndState | CHAR01_01~03 PASS, 상태 체인 정확, registry marker 정확 |
| RuntimeAssemblyAndBoundary | assembly/name/reference/namespace/MAP 의존성 경계 유지 |
| CoreMovementContractCoverage | 입력·상태·지면·점프·공중·착지 구현 단위 존재와 충돌 0 |
| RegressionTests | Game.Character.Tests.EditMode 전체 PASS, 필수 36개 PASS |
| ForbiddenFeatureAndScope | 금지 기능 부재, WRITE ALLOWLIST 외 변경 0 |
| DependencyLedger | CHAR02 진입과 CHAR03 지연 의존성 분리 |
| CHAR01ExitDecision | APPROVED/ELIGIBLE 또는 REJECTED/BLOCKED가 증빙과 일치 |

## Unity Verification

- Unity Version: 6000.3.8f1
- Asset Refresh: PASS
- Compile Errors: 0
- Relevant New Warnings: 0
- Targeted EditMode Tests: PASS (`Game.Character.Tests.EditMode` 전체)
- Required Tests: PASS (36/36)
- PlayMode Tests: NOT RUN
- Scene/Prefab Changes: 0

컴파일 오류가 있으면 테스트 미실행이라도 `STATUS: FAIL` 또는 `STATUS: BLOCKED`다.

## Result File

```text
REPORTS/CHAR01_04_CHAR01_CORE_MOVEMENT_EXIT_AUDIT_RESULT.md
```

REPORT 필수 항목:

```text
TASK
STATUS: PASS / FAIL / BLOCKED
SUMMARY
READ
CHANGED
CREATED
TEST
UNITY
CONTRACT_COVERAGE
FORBIDDEN_FEATURE_SCAN
DEPENDENCY_LEDGER
OUT_OF_SCOPE_FINDINGS
CHAR01 EXIT
CHAR02_01 ENTRY
DONE CONDITIONS
NEXT
```

## DONE CONDITIONS

- [ ] CHAR01_01~03 PASS와 상태 체인이 검증됐다.
- [ ] Runtime assembly와 namespace 경계가 검증됐다.
- [ ] 핵심 이동 계약 커버리지가 검증됐다.
- [ ] 필수 36개 EditMode test case가 전부 PASS했다.
- [ ] 금지 기능과 범위 위반이 없다.
- [ ] CHAR02/CHAR03 의존성이 분리 기록됐다.
- [ ] exit 판정과 REPORT STATUS가 일치한다.
- [ ] REPORT 외 파일을 수정하지 않았다.

## Completion Rule

Task는 REPORT만 작성한다. PASS 후 `08_STATUS_FINALIZE_RULES.md`가 CHAR01_04를 COMPLETE, Current Task를 NONE으로 변경한다. `CHAR02_01_VALIDATE_TWO_CELL_HEIGHT_AND_GAP_RULES`는 LOCKED로 유지하고 자동 시작하지 않는다.
