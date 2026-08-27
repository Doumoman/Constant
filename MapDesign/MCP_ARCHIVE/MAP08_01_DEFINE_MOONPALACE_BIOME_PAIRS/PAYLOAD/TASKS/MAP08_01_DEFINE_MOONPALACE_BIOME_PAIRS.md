# MAP08_01 - Define Moonpalace Biome Pairs

```yaml
status_control:
  task_key: MAP08_01_DEFINE_MOONPALACE_BIOME_PAIRS
  result_file: REPORTS/MAP08_01_DEFINE_MOONPALACE_BIOME_PAIRS_RESULT.md
```

## TASK TYPE

```text
MOONPALACE BIOME PAIR CONTRACT + ORIENTATION/WARNING PRECONDITION TESTS
```

## Objective

MAP07_13 PASS/finalize 뒤 MAP08 boundary content phase를 시작한다. 이 Task는 월궁 biome 4종과 unordered 6개 biome pair를 immutable contract로 정의하고, 각 pair가 H/V boundary orientation을 모두 지원해야 하며 mandatory route boundary에서는 `tool_requirement=NONE` 후보만 허용된다는 precondition을 검증한다.

이 단계는 boundary candidate index, resolver, 실제 boundary microchunk authoring, generated CSV writer, sector assembly, Scene/Prefab output을 만들지 않는다. `MAP08_02_IMPLEMENT_BOUNDARY_CANDIDATE_INDEX`와 이후 Task body는 읽거나 시작하지 않는다.

## Mandatory Read Order

1. `00_MCP_ENTRYPOINT.md`
2. `01_PROJECT_LOCKED_RULES.md`
3. `02_MCP_WORK_RULES.md`
4. `03_DATA_CSV_RULES.md`
5. `04_UNITY_MCP_RULES.md`
6. `05_CHANGE_CONTROL_RULES.md`
7. `07_PATCH_APPLY_RULES.md`
8. `08_STATUS_FINALIZE_RULES.md`
9. `MASTER_IMPLEMENTATION_TASK_LIST.md`
10. `06_IMPLEMENTATION_STATUS.md`
11. 이 Task
12. `REPORTS/MAP07_13_MAP07_STARTER_AND_EXIT_TESTS_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP07_13_MAP07_STARTER_AND_EXIT_TESTS
STATUS: PASS
MAP07_13: COMPLETE ELIGIBLE
MAP07 PHASE EXIT: APPROVED
MAP08_01_DEFINE_MOONPALACE_BIOME_PAIRS: LOCKED / DO NOT START
SHA-256: 263a2bbf291e4df25dbe6bc101986e11ebf39bc0fc3d0074759fb7450b6df77e
```

이 별도 patch가 적용된 뒤에만 MAP08_01을 실행한다. MAP08_02 이후 Task body는 읽거나 시작하지 않는다.

## Approved Input Baseline

```text
Unity: 6000.3.8f1
Runtime assembly: Game.Map.Runtime
Runtime namespace boundary: StarNight.Map.WorldGeneration.*
Editor assembly: MapAuthoring.Editor
Runtime EditMode assembly: Game.Map.Tests.EditMode
Editor EditMode assembly: MapAuthoring.Tests.EditMode
New asmdef/asmref: NO
MAP07_13 Result SHA-256: 263a2bbf291e4df25dbe6bc101986e11ebf39bc0fc3d0074759fb7450b6df77e
MAP07_13 Task SHA-256: 698a330dcd7a8ba14ec33cec51b68ea56be9382abd0eefde96eb2a516c81effb
MAP07_13 patch receipt SHA-256: 5964a1611a3c57bd8134ea4d9e78d8a7d45e655cb2e082514045b1a2eb70fa77
MAP07 required total: 5422/5422 PASS
MAP06 required total: 2746/2746 PASS
MAP05 required total: 1959/1959 PASS
Actually executed required total: 10127/10127 PASS
MAP07_13 failed/skipped: 0/0
MAP07_13 compile/Console/relevant warnings: 0/0/0
Assets meta: 3409
Authoring CSV/meta: 50 / 50
Authoring manifest SHA-256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Generated CSV files created by MAP07_13: 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes by MAP07_13: 0
Duplicate GUID groups: 0
MAP07 phase exit: APPROVED
```

## Map Package Reference

Exact installed Map Package v1.0이 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
02_PHASE_ROADMAP/MAP08_BIOME_BOUNDARY_CHUNKS.md
03_CSV_SCHEMA/TILE_AUTHORING_FORMAT.md
03_CSV_SCHEMA/BIOME_BOUNDARY_FORMAT.md
04_CSV_STARTER/biome_definitions.csv
04_CSV_STARTER/biome_boundary_profiles.csv
06_CHECKLISTS/PIPELINE_EXIT_CRITERIA.md
06_CHECKLISTS/VALIDATOR_MATRIX.md
```

Reference는 ownership, existing biome/profile semantics, and MAP08 phase gate 확인 용도다. MAP08_02+ Task 문서와 실제 boundary chunk content body는 읽지 않는다.

## READ ALLOWLIST

### Existing baseline contracts

```text
Assets/_Game/Map/Runtime/WorldGeneration/*
Assets/_Game/Map/Runtime/Data/Csv/*
Assets/_Game/Map/Runtime/Data/Definitions/*
Assets/_Game/Map/Runtime/Data/Registry/*
Assets/_Game/Tests/EditMode/Map/WorldGeneration/*
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/*
```

읽기는 existing public API, namespace/assembly placement, regression test selection, Authoring inventory hash, and Assets meta count 확인에 한정한다.

금지: MAP08_02+ Task body, MAP09 sector assembly implementation body, Legacy/Stage/P6/P11 generator body, Scene/Prefab YAML, generated output body.

## WRITE ALLOWLIST

### 신규 Runtime production C# - exact 6

```text
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBiomeId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBiomePair.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBoundaryOrientation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBoundaryWarningMarker.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBiomePairDefinition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBiomePairCatalog.cs
```

Matching `.cs.meta` files are required. If `Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/` does not exist, create that folder and matching folder `.meta`; otherwise new folder meta count remains `0`.

### 신규 Runtime EditMode tests - exact 2

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries/MoonpalaceBiomePairCatalogTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries/MoonpalaceBiomePairContractTests.cs
```

Matching `.cs.meta` files are required. If the matching test folder does not exist, create it and matching folder `.meta`; otherwise new folder meta count remains `0`.

### 기존 phase-boundary tests - exact up to 20

MAP08_01 symbols를 허용하고 MAP08_02+ future symbols 금지를 유지하기 위해 필요한 경우 existing boundary/namespace guard test C#만 수정할 수 있다. Matching existing `.cs.meta`는 수정하지 않는다.

### Result report - exact 1

```text
MapDesign/MCP/REPORTS/MAP08_01_DEFINE_MOONPALACE_BIOME_PAIRS_RESULT.md
```

## Required Implementation Contract

### Canonical biome set

Define exactly four MoonPalace biomes in stable order:

```text
0 MoonCrater
1 CassiaRoot
2 AbandonedMill
3 MoonDough
```

The public contract must expose canonical ID, display name, stable order, and deterministic parse/format behavior. Invalid/empty/unknown biome IDs must be rejected without fallback.

### Canonical unordered pair set

Define exactly six unordered biome pairs, sorted by canonical biome order:

```text
MoonCrater <-> CassiaRoot
MoonCrater <-> AbandonedMill
MoonCrater <-> MoonDough
CassiaRoot <-> AbandonedMill
CassiaRoot <-> MoonDough
AbandonedMill <-> MoonDough
```

Pair construction must canonicalize reversed input to the same value. Self-pairs, duplicate pair definitions, and missing pair definitions must fail validation.

### Boundary orientation contract

Each of the six pairs must explicitly support both boundary orientations:

```text
Horizontal
Vertical
```

Horizontal and Vertical are orientation contracts, not authored chunk assets. Do not create boundary chunk rows or candidate pool rows in this Task.

### Mandatory boundary and tool contract

For every pair/orientation combination, define the precondition that mandatory route boundaries require candidate content with:

```text
tool_requirement: NONE
mandatory_route_allowed: true
```

This Task only records and tests the precondition. Actual candidate filtering is MAP08_04 ownership.

### Warning marker minimum

Define warning marker categories:

```text
Tile
Background
Resource
Audio
```

Every pair/orientation contract must require at least two distinct marker categories before a transition can be accepted by later content authoring. Actual warning length and marker implementation belong to MAP08_05.

### Determinism and immutability

- Catalog enumeration order must be stable and independent of dictionary iteration.
- Public collections must be immutable or copy-safe.
- Hash/string/signature output must be culture-invariant.
- Pair equality must not depend on input order.
- No runtime dependency may be added to Editor-only code.

## Required Tests

Run only task-relevant focused tests plus required MAP07/MAP06/MAP05 regression gates.

```text
MoonpalaceBiomePairCatalogTests >=220 PASS
MoonpalaceBiomePairContractTests >=180 PASS
MAP07 required total 5422/5422 PASS
MAP06 required total 2746/2746 PASS
MAP05 required total 1959/1959 PASS
Actually executed required total >=10527 PASS
Required failed/skipped 0/0
Unity compile errors 0
Final Console errors/warnings 0/0
Relevant warnings 0
```

## Static Gates

```text
Assets meta 3409 -> 3417 if no new folders are needed
Assets meta 3409 -> 3419 if runtime and test Boundaries folders must be created
New Runtime production C#/matching meta 6/6
New Runtime test C#/matching meta 2/2
New Editor production C#/matching meta 0/0
New Editor test C#/matching meta 0/0
New folder meta 0 or 2, depending on existing Boundaries folder presence
Task-local existing boundary test C# modified <=20
Matching existing boundary-test meta modified 0
Assets duplicate GUID groups 0

Authoring CSV/matching meta 50/50
Authoring manifest SHA-256 unchanged: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Authoring CSV tracked changes 0
Generated CSV files created 0

Scene/Prefab tracked changes 0/0
ProjectSettings/Packages tracked changes 0/0
asmdef/asmref tracked changes 0/0

MAP08_02+ production symbol hits 0
MAP09+ production symbol hits 0
Unapplied MCP patches 0
```

If actual folder presence changes the meta delta, Result must explicitly state which allowed branch was used and list the exact new folder meta paths.

## Forbidden This Task

- MAP08_02 boundary candidate index.
- MAP08_03 boundary resolver.
- MAP08_04 mandatory boundary filtering implementation.
- MAP08_05 warning length/marker rendering implementation.
- MAP08_06~MAP08_11 authored boundary chunk CSV/content rows.
- MAP08_12 validator, MAP08_13 preview window, MAP08_14 exit tests.
- MAP09 sector assembly, recipe resolver, compatibility solver.
- Generated CSV output.
- Authoring CSV mutation.
- Scene, Prefab, ProjectSettings, Packages, asmdef, asmref changes.
- Legacy/Stage/P6/P11 generator dependency as implementation base.
- Git commit/push.

## Result Report Requirements

Create exactly:

```text
MapDesign/MCP/REPORTS/MAP08_01_DEFINE_MOONPALACE_BIOME_PAIRS_RESULT.md
```

Required header:

```text
TASK: MAP08_01_DEFINE_MOONPALACE_BIOME_PAIRS
STATUS: PASS | FAIL | BLOCKED
MAP08_01: COMPLETE ELIGIBLE | REPAIR REQUIRED | BLOCKED
MAP08_02_IMPLEMENT_BOUNDARY_CANDIDATE_INDEX: LOCKED / DO NOT START
```

Required evidence:

- Prior MAP07_13 Result SHA-256 and Task SHA-256.
- Applied patch receipt SHA-256.
- Exact four biome IDs and canonical order.
- Exact six pair IDs in deterministic order.
- H/V support matrix for all six pairs.
- Mandatory boundary `tool_requirement=NONE` evidence for all pair/orientation combinations.
- Warning marker minimum evidence for all pair/orientation combinations.
- Focused test counts and required regression totals.
- Compile/Console/warning counts.
- Assets meta before/after and folder-meta branch.
- Authoring CSV/meta count and manifest SHA.
- Generated CSV, Scene/Prefab, ProjectSettings/Packages, asmdef/asmref change counts.
- Forbidden MAP08_02+/MAP09+ symbol scan result.

If any required gate fails, report `STATUS: FAIL` or `STATUS: BLOCKED` and do not open MAP08_02.
