# CHAR00_02 — 게임플레이 계약·소유권·고정 테스트룸 확정

```yaml
status_control:
  task_key: CHAR00_02_LOCK_GAMEPLAY_CONTRACTS_AND_TEST_FIXTURES
  result_file: REPORTS/CHAR00_02_LOCK_GAMEPLAY_CONTRACTS_AND_TEST_FIXTURES_RESULT.md
```

## TASK TYPE

PLAN / DATA

## Objective

`CHAR00_SOURCE_REGISTRY.md`의 조사 사실을 근거로 이후 캐릭터 구현의 게임플레이·입력·물리·상호작용·MAP 의존성 계약과 고정 검증 코스를 문서로 잠근다.

이번 Task는 문서 확정만 수행한다. 캐릭터 런타임 코드, 테스트 코드, 입력 자산, asmdef, MAP API는 만들지 않는다.

## READ ALLOWLIST

```text
CharacterDesign/**
Assets/_Game/Map/Runtime/**
Assets/_Game/Tests/EditMode/Map/**
Assets/_Legacy/StarNight/Input/**
Assets/_Legacy/StarNight/Scripts/Runtime/**
Assets/_Legacy/_Game/Player/**
Assets/_Legacy/_Game/Interaction/**
Packages/manifest.json
ProjectSettings/ProjectSettings.asset
ProjectSettings/InputManager.asset
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

레거시와 MAP 경로는 read-only다.

## WRITE ALLOWLIST

```text
CharacterDesign/01_FIXED_SPEC/01_CHARACTER_GAMEPLAY_RULES.md
CharacterDesign/01_FIXED_SPEC/02_CHARACTER_INPUT_RULES.md
CharacterDesign/01_FIXED_SPEC/03_CHARACTER_MOVEMENT_RULES.md
CharacterDesign/01_FIXED_SPEC/04_CHARACTER_INTERACTION_RULES.md
CharacterDesign/01_FIXED_SPEC/05_CHARACTER_COMBAT_RULES.md
CharacterDesign/01_FIXED_SPEC/06_CHARACTER_MAP_INTEGRATION_RULES.md
CharacterDesign/01_FIXED_SPEC/07_CHARACTER_TEST_RULES.md
CharacterDesign/01_FIXED_SPEC/08_IMPLEMENTATION_ORDER.md
CharacterDesign/03_DATA_SCHEMA/CHARACTER_ACTION_SCHEMA.md
CharacterDesign/03_DATA_SCHEMA/CHARACTER_DAMAGE_SCHEMA.md
CharacterDesign/03_DATA_SCHEMA/CHARACTER_INVENTORY_SCHEMA.md
CharacterDesign/03_DATA_SCHEMA/CHARACTER_MOVEMENT_TUNING_SCHEMA.md
CharacterDesign/04_TEST_FIXTURES/MOVEMENT_COURSE_SPEC.md
CharacterDesign/04_TEST_FIXTURES/INTERACTION_COURSE_SPEC.md
CharacterDesign/04_TEST_FIXTURES/COMBAT_COURSE_SPEC.md
CharacterDesign/04_TEST_FIXTURES/ROOM_TRANSITION_COURSE_SPEC.md
CharacterDesign/MCP/REPORTS/CHAR00_02_LOCK_GAMEPLAY_CONTRACTS_AND_TEST_FIXTURES_RESULT.md
```

`06_IMPLEMENTATION_STATUS.md`와 `MASTER_IMPLEMENTATION_TASK_LIST.md`는 TASK EXECUTION에서 수정하지 않는다.

## DO NOT

- Assets, Packages, ProjectSettings, MapDesign 수정
- C#, test code, inputactions, asmdef, Scene, Prefab 생성·수정
- 레거시 활성화 또는 복사 구현
- MAP world query/mutation/room API 선행 구현
- CHAR00_03 이후 Task body 읽기·시작
- commit/push

## Inputs

- `MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md`
- `MCP/REPORTS/CHAR00_01_INVENTORY_CHARACTER_INPUT_PHYSICS_MAP_RESULT.md`
- `01_CHARACTER_LOCKED_RULES.md`

필수 prior facts:

```text
Unity = 6000.3.8f1
baseline = main @ 24cb1b9
active character runtime = none
legacy code = Assets/_Legacy/**, LEGACY_DISABLED
Input System = 1.18.0, active handler Both
legacy Jump = Space
locked meanings = X / Down+X / Z / C
legacy mismatch = E / F / Q
MAP coordinate contract = present
character MAP query/mutation/room readiness APIs = absent
```

## Contracts To Lock

### 좌표·크기

```text
1 logical cell = 1 world unit
player collider baseline = Capsule 0.72 x 0.90 world unit
player remains smaller than one 1x1 terrain cell
```

### 이동 문법

```text
basic jump reaches a 2-cell-high platform
same-level 2-cell gap is traversable by running
same-level 3-cell gap is not traversable by basic movement alone
3-cell gap requires a tool or environment route
no wall jump / no dash / no double jump
```

### 입력

```text
Move = horizontal axis
Down = down axis
Jump = Space keyboard baseline
Action = X
Safe drop/place = Down + X
Bomb = Z
Rope = C
```

레거시 E/F/Q 불일치와 활성 inputactions 부재를 기록하되 이번 Task에서 자산을 만들지 않는다.

### 상호작용·전투

```text
no separate basic attack button
combat routes = stomp / contact / thrown object / bomb / tool / environment
carry slots = one
first carried object size <= 1x1
safe drop rejects overlap instead of forcing placement
```

### 물리·접지

```text
Rigidbody2D-compatible motor; manual gravity allowed
grounded = downward capsule cast + vertical velocity gate
probe distance baseline = 0.08
grounded vy threshold baseline = <= 0.05
no slope policy until separately approved
```

### MAP·방 전환

```text
use MAP coordinate/query contract; do not access Tilemap internals
terrain mutation is request/result based
room transition keeps input and velocity
ungenerated destination blocks transition
hysteresis is required
```

캐릭터용 MAP query/mutation/boundary/readiness API가 없으며 `CHAR03_01` 전에 별도 MAP 계약이 필요함을 의존성으로 남긴다.

### 코드·테스트 배치 후보

```text
runtime: Assets/_Game/Character/Runtime/**
EditMode: Assets/_Game/Tests/EditMode/Character/**
PlayMode: Assets/_Game/Tests/PlayMode/Character/**
asmdef: not created or approved in CHAR00_02
```

## Required Fixture IDs

### MOVEMENT_COURSE_SPEC

```text
two_cell_height_jump_course
two_cell_same_level_gap_run_course
three_cell_same_level_gap_basic_movement_failure_course
forbidden_wall_jump_course
forbidden_dash_course
forbidden_double_jump_course
```

### INTERACTION_COURSE_SPEC

```text
carry_one_slot_course
safe_down_x_place_reject_overlap_course
directional_throw_course
```

### COMBAT_COURSE_SPEC

```text
stomp_contact_course
no_basic_attack_button_course
thrown_object_impact_course
```

### ROOM_TRANSITION_COURSE_SPEC

```text
input_keep_transition_course
velocity_keep_transition_course
ungenerated_room_boundary_block_course
hysteresis_boundary_course
```

각 코스는 setup, action, expected result, failure condition을 명시한다.

## Tests

| Gate | PASS 조건 |
|---|---|
| ContractsLocked | 좌표·이동·입력·물리·전투·MAP 계약이 모순 없이 고정됨 |
| FixturesLocked | 위 16개 fixture ID와 판정 조건이 문서에 존재함 |
| NoRuntimeMutation | Assets/Packages/ProjectSettings/MapDesign 및 C#/asset 변경 0 |
| ReportExact | 지정 REPORT 하나가 정확한 상태와 실제 변경 목록을 포함함 |

## Unity Verification

코드·asset 무변경 Task다. Unity version과 `isCompiling` 상태, 신규 compile error 0 또는 명시적인 no-code compile rationale을 REPORT에 기록한다.

## Result File

```text
REPORTS/CHAR00_02_LOCK_GAMEPLAY_CONTRACTS_AND_TEST_FIXTURES_RESULT.md
```

REPORT는 TASK, 독립된 STATUS, READ, CHANGED, CREATED, 4개 gate, Unity, blockers, out-of-scope findings를 포함한다.

## DONE CONDITIONS

- [ ] prior PASS result와 registry가 검증됐다.
- [ ] 필수 계약이 모든 지정 문서에 고정됐다.
- [ ] 16개 fixture ID와 판정 조건이 고정됐다.
- [ ] 프로젝트 구현 파일 변경이 0개다.
- [ ] REPORT가 정확히 `STATUS: PASS`다.

## Completion Rule

Task는 REPORT만 작성한다. PASS 후 `08_STATUS_FINALIZE_RULES.md`가 CHAR00_02를 COMPLETE, Current Task를 NONE으로 변경한다. CHAR00_03은 LOCKED로 유지한다.
