# MAP08_14 - MAP08 Exit Tests

```yaml
status_control:
  task_key: MAP08_14_MAP08_EXIT_TESTS
  result_file: REPORTS/MAP08_14_MAP08_EXIT_TESTS_RESULT.md
```

## TASK TYPE

```text
MOONPALACE BOUNDARY PHASE EXIT TESTS + EXIT APPROVAL REPORT
```

## Objective

MAP08_13 PASS/finalize 뒤 MAP08 phase 전체를 닫기 위한 exit test suite를 구현한다. 6개 Moonpalace biome pair의 전체 후보, A/B 방향 반전, H/V edge compatibility, mandatory no-tool route, warning marker length/category, MAP08_12 coverage digest, MAP08_13 preview projection 보존을 검증한다.

이 Task는 MAP08 exit tests와 Result report만 연다. 신규 Runtime production, 신규 Editor production, Authoring CSV mutation, generated CSV writer, sector assembly, tilemap baking, Scene/Prefab output은 구현하지 않는다. `MAP09_01_IMPLEMENT_SECTOR_RECIPE_RESOLVER`와 이후 Task body는 읽거나 시작하지 않는다.

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
12. `REPORTS/MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW
STATUS: PASS
MAP08_13: COMPLETE ELIGIBLE
MAP08_14_MAP08_EXIT_TESTS: LOCKED / DO NOT START
SHA-256: cb72264380c94a35ab6abe42f672c06e994f30deadc5a867546a31279b9bf7cd
```

이 별도 patch가 적용된 뒤에만 MAP08_14를 실행한다. MAP09 이후 Task body는 읽거나 시작하지 않는다.

## Approved Input Baseline

```text
Unity: 6000.3.8f1
Runtime assembly: Game.Map.Runtime
Runtime namespace boundary: StarNight.Map.WorldGeneration.*
Runtime EditMode assembly: Game.Map.Tests.EditMode
Editor assembly: MapAuthoring.Editor
Editor EditMode assembly: MapAuthoring.Tests.EditMode
New asmdef/asmref: NO
MAP08_13 Result SHA-256: cb72264380c94a35ab6abe42f672c06e994f30deadc5a867546a31279b9bf7cd
MAP08_13 installed Task SHA-256: 5e932e82ae7ed78e732c26907ac4cd480e43d7bf14ee9978df46d2917310153d
MAP08_12 Result SHA-256: 26801f7bc31d354b9639278ec133970e7840f3c7dacb9f0f2a4b6a0e0288896b
MAP08_12 installed Task SHA-256: cdaadd203562acdcad7024349499a2df3ac0ccd9860ef580473e984ae9490966
Boundary coverage accepted: true
Boundary coverage aggregate digest: f7ff1c49f5bc33a4ad57799269bc3915806fe0cb60f347ed76eb16ea26f7fc68
Coverage candidates/microchunks/tile rows/socket rows: 31/31/2976/62
Coverage issues: 0
MAP08_13 focused tests: 640/640 PASS
MAP08 required union: 8380/8380 PASS
MAP07 required total: 5422/5422 PASS
MAP06 required total: 2746/2746 PASS
MAP05 required total: 1959/1959 PASS
Completed distinct required subset: 18507/18507 PASS
MAP08_13 failed/skipped: 0/0
MAP08_13 compile/Console/relevant warnings: 0/0/0
Global Assets meta: 3813
Assets/_Game/Map meta: 596
Authoring CSV/meta: 50 / 50
Authoring manifest SHA-256 before MAP08_14: f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb
Generated CSV files created by MAP08_13: 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes by MAP08_13: 0
Duplicate GUID groups: 0
```

## Map Package Reference

Exact installed Map Package v1.0이 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
02_PHASE_ROADMAP/MAP08_BOUNDARY_CHUNKS.md
03_CSV_SCHEMA/CSV_SCHEMA_REFERENCE.md
03_CSV_SCHEMA/TILE_AUTHORING_FORMAT.md
04_CSV_STARTER/biome_boundary_profiles.csv
04_CSV_STARTER/biome_boundary_pair_rules.csv
04_CSV_STARTER/boundary_chunk_catalog.csv
04_CSV_STARTER/microchunk_catalog.csv
04_CSV_STARTER/microchunk_tile_cells.csv
04_CSV_STARTER/microchunk_sockets.csv
04_CSV_STARTER/edge_signatures.csv
04_CSV_STARTER/tile_code_dictionary.csv
06_CHECKLISTS/VALIDATOR_MATRIX.md
```

Reference는 MAP08 phase gate와 exit expectation 확인 용도다. MAP09 sector assembly body, generated output body, MAP10+ body는 읽지 않는다.

## READ ALLOWLIST

```text
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/*
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries/*
Assets/_Game/Map/Data/WorldGeneration/Authoring/Boundary/biome_boundary_profiles.csv
Assets/_Game/Map/Data/WorldGeneration/Authoring/Boundary/biome_boundary_pair_rules.csv
Assets/_Game/Map/Data/WorldGeneration/Authoring/Boundary/boundary_chunk_catalog.csv
Assets/_Game/Map/Data/WorldGeneration/Authoring/MicroChunk/microchunk_catalog.csv
Assets/_Game/Map/Data/WorldGeneration/Authoring/MicroChunk/microchunk_tile_cells.csv
Assets/_Game/Map/Data/WorldGeneration/Authoring/MicroChunk/microchunk_sockets.csv
Assets/_Game/Map/Data/WorldGeneration/Authoring/Route/edge_signatures.csv
Assets/_Game/Map/Data/WorldGeneration/Authoring/MicroChunk/tile_code_dictionary.csv
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/*
Assets/_Game/Map/Runtime/Data/Definitions/*
Assets/_Game/Map/Runtime/Data/Registry/*
Assets/_Game/Map/Runtime/Data/Csv/*
Assets/_Game/Editor/MapAuthoring/Boundaries/*
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Boundaries/*
```

금지: MAP09+ Task body, generated output body, sector assembly implementation body, Legacy/Stage/P6/P11 generator body, Scene/Prefab YAML.

## WRITE ALLOWLIST

### 신규 Runtime EditMode tests - exact 3

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries/MoonpalaceBoundaryPhaseExitCoverageTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries/MoonpalaceBoundaryPhaseExitCompatibilityTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries/MoonpalaceBoundaryPhaseExitDeterminismTests.cs
```

Matching `.cs.meta` files are required. Existing `Boundaries` test folder must be reused; new folder meta must remain `0`.

### Existing MAP08 Runtime/Editor/Test files - exact up to 70

Existing MAP08 Runtime, Editor, and test C# may be modified only for test fixture exposure, deterministic helper extraction, or NUnit compatibility. Matching existing `.meta` files must not change. New Runtime production C# and new Editor production C# are forbidden.

### Result report - exact 1

```text
MapDesign/MCP/REPORTS/MAP08_14_MAP08_EXIT_TESTS_RESULT.md
```

## Exit Contract

### Canonical pair closure

The exit suite must verify the exact six pair reports and counts:

```text
PAIR_CRATER_ROOT   6/6/576/12
PAIR_CRATER_MILL   4/4/384/8
PAIR_CRATER_DOUGH  5/5/480/10
PAIR_ROOT_MILL     6/6/576/12
PAIR_ROOT_DOUGH    5/5/480/10
PAIR_MILL_DOUGH    5/5/480/10
TOTAL              31/31/2976/62
```

Each pair must satisfy:

```text
candidate count > 0
horizontal orientation count > 0
vertical orientation count > 0
accepted coverage state
issue count 0
```

### Direction reversal

For every candidate, both biome transition directions must be representable:

```text
A -> B
B -> A
```

The reverse direction must preserve the same canonical pair id, source microchunk id, catalog row id, route requirement, profile, and warning evidence. The transform/mirror state may differ only through approved MAP07 transform policy.

### Edge compatibility

For every mandatory boundary candidate:

```text
tool_requirement: NONE
route role: MANDATORY
horizontal signature: EDGE_H_MID_WALK
vertical signature: EDGE_V_CENTER_CLIMB
socket count: 2 per candidate
socket direction compatibility: symmetric
socket traversal kind compatibility: exact
```

No MAP09 sector recipe, tilemap, or generated output may be used to prove compatibility. The proof is strictly at MAP08 boundary chunk data level.

### Warning evidence

For every active candidate:

```text
warning marker categories available: Tile / Background / Resource / Audio
minimum distinct warning categories: 2
minimum warning evidence count: 2
warning evidence references the entering biome
warning evidence survives A->B/B->A projection
```

`BOUND_LAYER` remains valid only for vertical orientation where MAP08_12 coverage allowed it. Horizontal `BOUND_LAYER` acceptance is forbidden.

### Coverage digest and preview preservation

The exit suite must re-run the MAP08_12 coverage validator and compare:

```text
Accepted: true
Issues: 0
Aggregate digest: f7ff1c49f5bc33a4ad57799269bc3915806fe0cb60f347ed76eb16ea26f7fc68
Authoring manifest: f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb
```

It must also validate that MAP08_13 preview projection still displays the same six pair rows, candidate counts, transition labels, and overlay categories without changing the digest.

### Phase approval output

The Result report is the only phase approval artifact required in this Task. If all exit gates pass, it must include:

```text
MAP08 PHASE EXIT: APPROVED
MAP09_01_IMPLEMENT_SECTOR_RECIPE_RESOLVER: LOCKED / DO NOT START
```

No generated CSV or seed artifact is created by MAP08_14.

## Required Tests

### Focused MAP08_14 tests - exact 840

```text
MoonpalaceBoundaryPhaseExitCoverageTests: 300/300
MoonpalaceBoundaryPhaseExitCompatibilityTests: 300/300
MoonpalaceBoundaryPhaseExitDeterminismTests: 240/240
MAP08_14 focused total: 840/840 PASS
```

Required coverage:

```text
validates exact six-pair matrix and totals
validates every pair has H and V coverage
validates every candidate has 96 unique cells through existing MAP08/MAP07 evidence
validates A->B and B->A projection for all 31 candidates
validates tool_requirement NONE on mandatory boundary candidates
validates horizontal and vertical edge signatures
validates symmetric socket direction/traversal compatibility
validates minimum two warning evidence categories
validates warning evidence references entering biome
forbids horizontal BOUND_LAYER acceptance
preserves MAP08_12 aggregate digest
preserves MAP08_13 preview projection counts
preserves Authoring manifest
does not create Generated CSV
does not require MAP09 sector recipe data
```

### Required regression suite

```text
MAP08 required union: 9220/9220 PASS
MAP07 required regression: 5422/5422 PASS
MAP06 required regression: 2746/2746 PASS
MAP05 required regression: 1959/1959 PASS
Required subset total: 19347/19347 PASS
Required failed/skipped: 0/0
```

The `9220` MAP08 total is `8380` existing required union plus `840` MAP08_14 focused tests.

## Static Gates

```text
New Runtime production C#/matching meta: 0/0
New Editor production C#/matching meta: 0/0
New Runtime EditMode test C#/matching meta: 3/3
New Editor EditMode test C#/matching meta: 0/0
New folder meta: 0
Global Assets meta: 3813 -> 3816
Assets/_Game/Map meta: 596 -> 596
Duplicate GUID groups: 0

Authoring CSV/matching meta: 50/50
Authoring tracked CSV changes: 0
Authoring manifest before: f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb
Authoring manifest after:  f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb

Generated CSV files: 0
Scene/Prefab tracked changes: 0/0
ProjectSettings/Packages tracked changes: 0/0
asmdef/asmref tracked changes: 0/0
MAP09+/MAP10+ forbidden production symbol hits: 0/0
Unapplied MCP patches: 0
git diff --check errors before Result: 0
```

## Commit Requirement

After all tests and static gates pass, create exactly one atomic implementation commit for MAP08_14. Include implementation details in the commit body.

Commit subject:

```text
MAP08_14: approve moonpalace boundary phase exit
```

Commit body must include:

```text
- Add MAP08 phase exit coverage, compatibility, and determinism tests
- Verify six boundary pairs, 31 candidates, 2976 tile rows, and 62 sockets
- Verify A/B direction reversal, edge signatures, mandatory no-tool rules, and warning evidence
- Preserve MAP08_12 coverage digest and MAP08_13 preview projection
- Preserve Authoring CSV and Generated CSV counts
- Verify MAP08_14 focused 840/840 and required subset 19347/19347
- Approve MAP08 phase exit
- Keep MAP09_01_IMPLEMENT_SECTOR_RECIPE_RESOLVER locked / do not start
```

Do not include unrelated files in the commit. Preserve any unrelated user worktree changes.

## Result Report Requirements

Create `MapDesign/MCP/REPORTS/MAP08_14_MAP08_EXIT_TESTS_RESULT.md` with:

```text
TASK: MAP08_14_MAP08_EXIT_TESTS
STATUS: PASS|FAIL|BLOCKED
MAP08_14: COMPLETE ELIGIBLE only if PASS
MAP08 PHASE EXIT: APPROVED only if PASS
MAP09_01_IMPLEMENT_SECTOR_RECIPE_RESOLVER: LOCKED / DO NOT START
```

Result must report:

```text
Patch apply status
MAP08_13 Result SHA-256
Installed MAP08_13 Task SHA-256
Installed MAP08_14 Task SHA-256
Implemented file inventory
Coverage report accepted/digest/counts
Exact pair matrix verified
Direction reversal count
Edge compatibility count
Warning evidence count
Focused and regression test counts
Compile/Console/relevant warning counts
Authoring manifest before/after
Generated CSV count
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref change counts
Forbidden MAP09+/MAP10+ symbol hits
git diff --check result
Atomic commit subject and commit hash
MAP08 phase exit approval line
```

## Done Condition

MAP08_14 is complete only when:

```text
Prior MAP08_13 Result SHA gate PASS
All six pair reports and total counts verified
All 31 candidates support A->B and B->A projection
Mandatory no-tool edge compatibility verified
Warning evidence minimums verified
MAP08_12 aggregate digest preserved
MAP08_13 preview projection preserved
No Authoring CSV or Generated CSV mutation
Focused MAP08_14 tests 840/840 PASS
Required subset 19347/19347 PASS
Compile/Console/relevant warnings 0/0/0
Static gates PASS
Atomic commit created with detailed body
Result report created
MAP08 PHASE EXIT: APPROVED
MAP09_01 remains LOCKED / DO NOT START
```
