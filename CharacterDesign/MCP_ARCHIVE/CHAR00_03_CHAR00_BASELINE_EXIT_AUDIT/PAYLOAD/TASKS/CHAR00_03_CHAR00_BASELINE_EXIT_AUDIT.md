# CHAR00_03 — CHAR00 기준선·하네스 종료 감사

```yaml
status_control:
  task_key: CHAR00_03_CHAR00_BASELINE_EXIT_AUDIT
  result_file: REPORTS/CHAR00_03_CHAR00_BASELINE_EXIT_AUDIT_RESULT.md
```

## TASK TYPE

AUDIT

## Objective

CHAR00_01 조사 결과와 CHAR00_02 계약·스키마·고정 fixture를 교차 감사해 캐릭터 기준선이 구현 진입에 충분한지 판정하고 `CHAR00 EXIT: APPROVED` 또는 차단 사유를 기록한다.

이번 Task는 읽기 전용 감사다. 문서 계약, Assets, 코드, 테스트, inputactions, asmdef, Scene, Prefab, MAP 구현을 수정하지 않는다.

## READ ALLOWLIST

```text
CharacterDesign/**
Packages/manifest.json
ProjectSettings/ProjectSettings.asset
ProjectSettings/InputManager.asset
Assets/_Game/Map/Runtime/**
Assets/_Legacy/StarNight/Input/**
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

프로젝트와 레거시 경로는 read-only다. CHAR01_01 이후 Task body는 읽지 않는다.

## WRITE ALLOWLIST

```text
CharacterDesign/MCP/REPORTS/CHAR00_03_CHAR00_BASELINE_EXIT_AUDIT_RESULT.md
```

`06_IMPLEMENTATION_STATUS.md`와 `MASTER_IMPLEMENTATION_TASK_LIST.md`는 TASK EXECUTION에서 수정하지 않는다.

## DO NOT

- 계약·스키마·fixture 문서 수정
- Assets, Packages, ProjectSettings, MapDesign 수정
- C#, test code, inputactions, asmdef, Scene, Prefab 생성·수정
- CHAR01 구현 또는 CHAR03 MAP API 선행 구현
- 잠금 규칙 완화
- commit/push

## Inputs

```text
MCP/REPORTS/CHAR00_01_INVENTORY_CHARACTER_INPUT_PHYSICS_MAP_RESULT.md
MCP/REPORTS/CHAR00_02_LOCK_GAMEPLAY_CONTRACTS_AND_TEST_FIXTURES_RESULT.md
MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md
01_FIXED_SPEC/*.md
03_DATA_SCHEMA/*.md
04_TEST_FIXTURES/*.md
MCP/01_CHARACTER_LOCKED_RULES.md
MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
MCP/06_IMPLEMENTATION_STATUS.md
```

## Required Audit

### 1. 선행 증빙과 상태

- CHAR00_01/02 REPORT가 각각 정확히 `STATUS: PASS`인지 확인한다.
- registry marker가 `REGISTRY_STATE: FILLED_BY_CHAR00_01`인지 확인한다.
- 실행 시 CHAR00_01/02는 COMPLETE, CHAR00_03만 CURRENT, CHAR01_01 이후는 LOCKED인지 확인한다.

### 2. 잠금 계약 교차 정합성

아래 값이 locked rules, fixed specs, schemas, fixtures 사이에서 충돌하지 않아야 한다.

```text
1 logical cell = 1 world unit
Capsule 0.72 x 0.90
2-cell height reachable
2-cell same-level gap pass
3-cell same-level gap basic fail
no wall jump / dash / double jump
Jump=Space / Action=X / Down+X / Bomb=Z / Rope=C
one carry slot / first size <=1x1 / overlap reject
no separate basic attack
input KEEP / velocity KEEP / readiness gate / hysteresis
MAP public contract only / no Tilemap internals
```

### 3. Fixture·schema 완전성

- canonical fixture ID 16개가 정확히 존재한다: Movement 6, Interaction 3, Combat 3, Room Transition 4.
- 각 fixture가 setup/action/expected/failure를 가진다.
- action, damage, inventory, movement tuning schema가 고정 계약을 표현하고 중복 authoritative source를 만들지 않는다.
- 문서에 double-brace placeholder, TODO, TBD, DRAFT, UNKNOWN이 남아 있으면 분류한다. 구현 진입을 모호하게 만드는 항목은 BLOCKED다.

### 4. 선행 구현 부재

- 활성 캐릭터 runtime/test/inputactions/asmdef 신규 구현이 시작되지 않았음을 확인한다.
- CHAR00_02 변경은 지정 16개 문서와 REPORT에 한정됐음을 확인한다.
- 프로젝트 구현 파일 변경 0을 확인한다. 기존 사용자 변경은 prior registry 기준선과 분리한다.

### 5. 의존성 장부

다음을 명확히 분리한다.

```text
CHAR01 entry blocker: 활성 inputactions/asmdef/코드 배치 결정을 CHAR01_01 Task가 명시적으로 소유할 수 있는가
CHAR03 deferred dependency: MAP world query/mutation/boundary/readiness API가 CHAR03_01 전 필요
out-of-scope: stale Map PlayMode asmdef reference
```

CHAR03 의존성은 문서에 명확히 기록돼 있고 CHAR01 핵심 이동의 순수 구현을 막지 않으면 CHAR00 EXIT 차단 사유가 아니다.

### 6. Exit 판정

모든 gate가 PASS면 REPORT에 정확히 다음을 기록한다.

```text
CHAR00 EXIT: APPROVED
CHAR01_01 ENTRY: ELIGIBLE FOR SEPARATE PATCH
```

어느 gate라도 실패하면:

```text
CHAR00 EXIT: REJECTED
CHAR01_01 ENTRY: BLOCKED
```

## Tests

| Gate | PASS 조건 |
|---|---|
| PriorEvidenceAndState | CHAR00_01/02 PASS, registry marker, 상태 체인 정확 |
| ContractCrossConsistency | 고정 계약 전부 교차 일치, 충돌 0 |
| FixtureAndSchemaCompleteness | fixture 16/16 및 4요소, schema 누락·중복 0 |
| NoPrematureImplementation | 활성 캐릭터 구현·프로젝트 변경 0 |
| DependencyLedger | CHAR01 소유권과 CHAR03 지연 의존성이 명시적으로 분리됨 |
| CHAR00ExitDecision | APPROVED/ELIGIBLE 또는 REJECTED/BLOCKED가 증빙과 일치 |

## Unity Verification

코드·asset 무변경 감사다. Unity 6000.3.8f1, `isCompiling=False`, 신규 compile error/relevant warning 0을 확인하거나 연결 불가 시 BLOCKED 사유를 정확히 기록한다. EditMode/PlayMode는 실행하지 않으며 no-code rationale을 기록한다.

## Result File

```text
REPORTS/CHAR00_03_CHAR00_BASELINE_EXIT_AUDIT_RESULT.md
```

REPORT 필수 항목:

```text
TASK
STATUS: PASS / FAIL / BLOCKED
READ
CHANGED
CREATED
TEST (6 gates)
UNITY
CONTRACT_CONFLICTS
UNRESOLVED_TOKENS
DEPENDENCY_LEDGER
OUT_OF_SCOPE_FINDINGS
CHAR00 EXIT
CHAR01_01 ENTRY
NEXT
```

## DONE CONDITIONS

- [ ] 선행 REPORT·registry·상태가 검증됐다.
- [ ] 계약 충돌과 모호한 미해결 토큰이 0이다.
- [ ] fixture 16/16과 schema 완전성이 검증됐다.
- [ ] 캐릭터 선행 구현과 프로젝트 변경이 0이다.
- [ ] CHAR01/CHAR03 의존성이 분리 기록됐다.
- [ ] exit 판정과 REPORT STATUS가 일치한다.

## Completion Rule

Task는 REPORT만 작성한다. PASS 후 `08_STATUS_FINALIZE_RULES.md`가 CHAR00_03을 COMPLETE, Current Task를 NONE으로 변경한다. `CHAR01_01_IMPLEMENT_INPUT_SNAPSHOT_AND_PLAYER_STATES`는 LOCKED로 유지하고 자동 시작하지 않는다.
