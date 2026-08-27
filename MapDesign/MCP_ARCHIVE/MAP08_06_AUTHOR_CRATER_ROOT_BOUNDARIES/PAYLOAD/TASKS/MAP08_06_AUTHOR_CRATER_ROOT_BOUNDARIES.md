# MAP08_06 - Author Crater Root Boundaries

```yaml
status_control:
  task_key: MAP08_06_AUTHOR_CRATER_ROOT_BOUNDARIES
  result_file: REPORTS/MAP08_06_AUTHOR_CRATER_ROOT_BOUNDARIES_RESULT.md
```

## TASK TYPE

```text
PAIR_CRATER_ROOT AUTHORING CSV CANDIDATE MATRIX + VALIDATION TESTS
```

## Objective

MAP08_05 PASS/finalize 뒤 `PAIR_CRATER_ROOT` MoonCrater↔CassiaRoot boundary 후보를 실제 Authoring CSV와 Runtime validator로 완성한다.

이 Task는 Crater↔Root pair 하나만 연다. Crater↔Mill, Crater↔Dough, Root↔Mill, Root↔Dough, Mill↔Dough boundary content authoring, generated CSV writer, sector assembly, Scene/Prefab output은 구현하지 않는다. `MAP08_07_AUTHOR_CRATER_MILL_BOUNDARIES`와 이후 Task body는 읽거나 시작하지 않는다.

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
12. `REPORTS/MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT
STATUS: PASS
MAP08_05: COMPLETE ELIGIBLE
MAP08_06_AUTHOR_CRATER_ROOT_BOUNDARIES: LOCKED / DO NOT START
SHA-256: ff8e384a5d450d356017cb91ad52a25b1143931a823c48b67023a7b6be599ac0
```

이 별도 patch가 적용된 뒤에만 MAP08_06을 실행한다. MAP08_07 이후 Task body는 읽거나 시작하지 않는다.

## Approved Input Baseline

```text
Unity: 6000.3.8f1
Runtime assembly: Game.Map.Runtime
Runtime namespace boundary: StarNight.Map.WorldGeneration.*
Runtime EditMode assembly: Game.Map.Tests.EditMode
Editor assembly: MapAuthoring.Editor
Editor EditMode assembly: MapAuthoring.Tests.EditMode
New asmdef/asmref: NO
MAP08_05 Result SHA-256: ff8e384a5d450d356017cb91ad52a25b1143931a823c48b67023a7b6be599ac0
MAP08_05 Task SHA-256: 7336541b62db64f0a4e40c2a892a6a07000fa011c87934a68fb9f785c10842a6
MAP08_05 patch receipt SHA-256: c31e1bde9497cdcfe89e5ea2430ce5415635efce3cb53688515fbbde70d4252e
MAP08_05 focused tests: 520/520 PASS
MAP08_04 focused tests: 520/520 PASS
MAP08_03 focused tests: 680/680 PASS
MAP08_02 focused tests: 580/580 PASS
MAP08_01 focused tests: 400/400 PASS
MAP08 focused total: 2700/2700 PASS
MAP07 required total: 5422/5422 PASS
MAP06 required total: 2746/2746 PASS
MAP05 required total: 1959/1959 PASS
Actually executed required total: 12827/12827 PASS
MAP08_05 failed/skipped: 0/0
MAP08_05 compile/Console/relevant warnings: 0/0/0
MAP08_05 historical Assets meta: 3455
Concurrent Character merge Assets meta delta: +226
Assets meta at MAP08_06 start: 3681
Assets/_Game/Map meta at MAP08_06 start: 566
Authoring CSV/meta: 50 / 50
Authoring manifest SHA-256 before MAP08_06: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Generated CSV files created by MAP08_05: 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes by MAP08_05: 0
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

Reference는 `PAIR_CRATER_ROOT` authoring matrix, CSV schema, socket/signature compatibility 확인 용도다. MAP08_07+ Task 문서와 다른 pair boundary content body는 읽지 않는다.

## READ ALLOWLIST

### Existing MAP08 boundary contracts

```text
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/*
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries/*
```

### Authoring CSV source files

```text
Assets/_Game/Map/Data/WorldGeneration/Authoring/Boundary/biome_boundary_profiles.csv
Assets/_Game/Map/Data/WorldGeneration/Authoring/Boundary/biome_boundary_pair_rules.csv
Assets/_Game/Map/Data/WorldGeneration/Authoring/Boundary/boundary_chunk_catalog.csv
Assets/_Game/Map/Data/WorldGeneration/Authoring/MicroChunk/microchunk_catalog.csv
Assets/_Game/Map/Data/WorldGeneration/Authoring/MicroChunk/microchunk_tile_cells.csv
Assets/_Game/Map/Data/WorldGeneration/Authoring/MicroChunk/microchunk_sockets.csv
Assets/_Game/Map/Data/WorldGeneration/Authoring/Route/edge_signatures.csv
Assets/_Game/Map/Data/WorldGeneration/Authoring/MicroChunk/tile_code_dictionary.csv
```

### Required regression scope

```text
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/*
Assets/_Game/Map/Runtime/Data/Definitions/*
Assets/_Game/Map/Runtime/Data/Registry/*
Assets/_Game/Map/Runtime/Data/Csv/*
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/*
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/*
```

금지: MAP08_07+ Task body, other-pair authored boundary content body, generated output body, MAP09 sector assembly implementation body, Legacy/Stage/P6/P11 generator body, Scene/Prefab YAML.

## WRITE ALLOWLIST

### Authoring CSV edits - exact 4

```text
Assets/_Game/Map/Data/WorldGeneration/Authoring/Boundary/boundary_chunk_catalog.csv
Assets/_Game/Map/Data/WorldGeneration/Authoring/MicroChunk/microchunk_catalog.csv
Assets/_Game/Map/Data/WorldGeneration/Authoring/MicroChunk/microchunk_tile_cells.csv
Assets/_Game/Map/Data/WorldGeneration/Authoring/MicroChunk/microchunk_sockets.csv
```

Matching `.csv.meta` files must not be modified.

### 신규 Runtime production C# - exact 4

```text
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceCraterRootBoundaryAuthoringContract.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceCraterRootBoundaryCandidateMatrix.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceCraterRootBoundaryContentReport.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceCraterRootBoundaryValidator.cs
```

Matching `.cs.meta` files are required. Existing `Boundaries` folders from MAP08_01 must be reused; new folder meta must remain `0`.

### 신규 Runtime EditMode tests - exact 2

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries/MoonpalaceCraterRootBoundaryAuthoringTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries/MoonpalaceCraterRootBoundaryValidatorTests.cs
```

Matching `.cs.meta` files are required.

### Existing MAP08 boundary files - exact up to 20

기존 MAP08_01~MAP08_05 boundary Runtime/Test C#는 Crater↔Root authoring validation integration을 위해 필요한 최소 범위만 수정할 수 있다. Matching existing `.cs.meta`는 수정하지 않는다.

### 기존 phase-boundary tests - exact up to 30

MAP08_06 symbols를 허용하고 MAP08_07+ future symbols 금지를 유지하기 위해 필요한 경우 existing boundary/namespace guard test C#만 수정할 수 있다. Matching existing `.cs.meta`는 수정하지 않는다.

### Result report - exact 1

```text
MapDesign/MCP/REPORTS/MAP08_06_AUTHOR_CRATER_ROOT_BOUNDARIES_RESULT.md
```

## Required Authoring Contract

### Pair identity

The only owned pair is:

```text
boundary_pair_rule_id: PAIR_CRATER_ROOT
biome_a_id: BIO_MOON_CRATER
biome_b_id: BIO_CASSIA_ROOT
allowed_boundary_profile_ids: BOUND_SOFT_BLEND | BOUND_CLIFF | BOUND_TUNNEL
boundary_profile_weights: 50 | 25 | 25
default_boundary_profile_id: BOUND_SOFT_BLEND
```

No other pair rule may be changed or newly authored in this Task.

### Candidate matrix

Author or preserve exactly one active boundary candidate for every Crater↔Root profile/orientation combination:

```text
BOUND_SOFT_BLEND / HORIZONTAL
BOUND_SOFT_BLEND / VERTICAL
BOUND_CLIFF      / HORIZONTAL
BOUND_CLIFF      / VERTICAL
BOUND_TUNNEL     / HORIZONTAL
BOUND_TUNNEL     / VERTICAL
```

The existing starter row `BCH_CRATER_ROOT_H_SOFT_01` may be preserved if it satisfies the contract. Do not rename or duplicate it. Add only the missing Crater↔Root rows needed to reach the exact 6-candidate matrix.

Required candidate IDs:

```text
BCH_CRATER_ROOT_H_SOFT_01
BCH_CRATER_ROOT_V_SOFT_01
BCH_CRATER_ROOT_H_CLIFF_01
BCH_CRATER_ROOT_V_CLIFF_01
BCH_CRATER_ROOT_H_TUNNEL_01
BCH_CRATER_ROOT_V_TUNNEL_01
```

Required new or preserved microchunk IDs:

```text
MC_BOUND_CRATER_ROOT_H_01
MC_BOUND_CRATER_ROOT_V_SOFT_01
MC_BOUND_CRATER_ROOT_H_CLIFF_01
MC_BOUND_CRATER_ROOT_V_CLIFF_01
MC_BOUND_CRATER_ROOT_H_TUNNEL_01
MC_BOUND_CRATER_ROOT_V_TUNNEL_01
```

### Edge signatures and sockets

Horizontal candidates must use:

```text
orientation: HORIZONTAL
entry_edge_signature_id: EDGE_H_MID_WALK
exit_edge_signature_id: EDGE_H_MID_WALK
socket sides: L and R
socket traversal_kind: WALK
socket route_layer: MANDATORY
mandatory_allowed: 1
tool_requirement: NONE
minimum_safe_tiles: 2
```

Vertical candidates must use:

```text
orientation: VERTICAL
entry_edge_signature_id: EDGE_V_CENTER_CLIMB
exit_edge_signature_id: EDGE_V_CENTER_CLIMB
socket sides: U and D
socket traversal_kind: CLIMB
socket route_layer: MANDATORY
mandatory_allowed: 1
tool_requirement: NONE
minimum_safe_tiles: 2
```

All 6 candidates must be `active=1`, `reversible=1`, have positive weight, and use only profile IDs allowed by `PAIR_CRATER_ROOT`.

### Tile data

Every owned Crater↔Root boundary microchunk must have complete 12x8 tile data:

```text
width_tiles: 12
height_tiles: 8
microchunk_tile_cells rows per microchunk: 96
local_x range: 0..11
local_y range: 0..7
tile_data_complete: 1
usage_class: BOUNDARY
biome_ids: BIO_MOON_CRATER|BIO_CASSIA_ROOT
```

The 6 owned microchunks must total exactly 576 tile-cell rows. Existing `MC_BOUND_CRATER_ROOT_H_01` rows may be preserved and counted as the H/SOFT candidate.

Each candidate must visibly communicate the Crater↔Root transition with at least:

```text
Tile category evidence: G_MOON_ROCK and G_CASSIA_WOOD both present where solid terrain is used.
Background category evidence: DB_CRATER and DB_ROOT both present in the warning/transition area.
Route evidence: M_ROUTE_MAIN on the mandatory passable band.
Socket evidence: M_SOCKET at the corresponding entry/exit edge cells.
```

This satisfies the MAP08_05 warning category minimum through Tile + Background. Resource and Audio marker categories are not required for this Task.

### CSV ownership

Allowed CSV row ownership:

```text
boundary_chunk_catalog.csv: Crater↔Root candidate rows only
microchunk_catalog.csv: Crater↔Root boundary microchunk rows only
microchunk_tile_cells.csv: Crater↔Root boundary tile rows only
microchunk_sockets.csv: Crater↔Root boundary socket rows only
```

Do not mutate:

- `biome_boundary_pair_rules.csv`
- `biome_boundary_profiles.csv`
- `edge_signatures.csv`
- `tile_code_dictionary.csv`
- `microchunk_object_slots.csv`
- any non-Crater↔Root microchunk row
- generated output CSV

## Required Runtime Validation Contract

The validator/report must prove:

```text
CraterRootCandidateCount = 6
CraterRootProfileOrientationMatrix = complete
CraterRootBoundaryChunkIds = exact required set
CraterRootMicrochunkIds = exact required set
CraterRootTileRows = 576
RowsPerOwnedMicrochunk = 96
HorizontalSocketShape = L/R EDGE_H_MID_WALK
VerticalSocketShape = U/D EDGE_V_CENTER_CLIMB
MandatoryAllowed = true for all owned candidates/sockets
ToolRequirement = NONE for all owned candidates/sockets
WarningMarkerCategories >= 2 for all owned candidates
GeneratedCsvCreated = 0
OtherPairRowsModified = 0
```

The validator must not resolve a winner, create generated CSV, or infer sector recipe choices.

## Required Tests

Run only task-relevant focused tests plus required MAP08/MAP07/MAP06/MAP05 regression gates.

```text
MoonpalaceCraterRootBoundaryAuthoringTests >=360 PASS
MoonpalaceCraterRootBoundaryValidatorTests >=360 PASS
MoonpalaceBoundaryWarningContractTests 260/260 PASS
MoonpalaceBoundaryWarningProbeTests 260/260 PASS
MoonpalaceMandatoryBoundaryFilterTests 320/320 PASS
MoonpalaceBoundaryToolRequirementTests 200/200 PASS
MoonpalaceBoundaryChunkResolverTests 420/420 PASS
MoonpalaceBoundaryTransformPolicyTests 260/260 PASS
MoonpalaceBoundaryCandidateIndexTests 360/360 PASS
MoonpalaceBoundaryCandidateKeyTests 220/220 PASS
MoonpalaceBiomePairCatalogTests 220/220 PASS
MoonpalaceBiomePairContractTests 180/180 PASS
MAP08 focused total >=3420 PASS
MAP07 required total 5422/5422 PASS
MAP06 required total 2746/2746 PASS
MAP05 required total 1959/1959 PASS
Actually executed required total >=13547 PASS
Required failed/skipped 0/0
Unity compile errors 0
Final Console errors/warnings 0/0
Relevant warnings 0
```

## Static Gates

```text
Assets meta 3681 -> 3687
Assets/_Game/Map meta 566 -> 570
New Runtime production C#/matching meta 4/4
New Runtime test C#/matching meta 2/2
New Runtime folder meta 0
New Editor production C#/matching meta 0/0
New Editor test C#/matching meta 0/0
Existing MAP08 boundary production/test C# modified <=20
Matching existing boundary production/test meta modified 0
Task-local existing boundary test C# modified <=30
Matching existing boundary-test meta modified 0
Assets duplicate GUID groups 0

Authoring CSV/matching meta inventory 50/50
Authoring CSV tracked changes exact 4
Authoring CSV matching meta modified 0
Authoring manifest SHA-256 before: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Authoring manifest SHA-256 after: must be reported
Generated CSV files created 0

Scene/Prefab tracked changes 0/0
ProjectSettings/Packages tracked changes 0/0
asmdef/asmref tracked changes 0/0
MAP08_07+ / MAP09+ forbidden production symbol hits 0 / 0
Unapplied MCP patches 0
```

## Commit Requirement

After all implementation and validation gates pass, create one git commit for this Task when the target project is a valid git repository.

Required commit behavior:

```text
Stage only task-owned Authoring CSV rows/files, new Runtime/Test files, matching new .cs.meta files, MCP receipt/status/task documents, and the MAP08_06 Result report.
Do not stage unrelated pre-existing worktree items.
Do not run git push.
Use a detailed commit body describing CSV row ownership, candidate matrix, validation gates, and preserved ownership boundaries.
If git is unavailable, report the exact blocker and do not fake a commit hash.
```

Suggested commit subject:

```text
MAP08_06: author crater root boundaries
```

The Result report must include final `git status --short` scope and either the created commit hash or the precise reason no commit was created.

## Forbidden

Do not implement or mutate:

- MAP08_07~MAP08_11 other pair boundary CSV/content rows.
- MAP08_12 coverage validator.
- MAP08_13 preview window.
- MAP08_14 exit tests.
- MAP09 sector recipe or assembly logic.
- Generated CSV writer/output.
- Scene, Prefab, ScriptableObject, Tilemap, ProjectSettings, Packages, asmdef, asmref.
- Legacy/Stage/P6/P11 generator body.

## Required Result Header

```text
TASK: MAP08_06_AUTHOR_CRATER_ROOT_BOUNDARIES
STATUS: PASS | FAIL | BLOCKED
MAP08_06: COMPLETE ELIGIBLE | REPAIR REQUIRED | BLOCKED
MAP08_07_AUTHOR_CRATER_MILL_BOUNDARIES: LOCKED / DO NOT START
```

## Required Result Evidence

The Result must include:

- Prior MAP08_05 Result SHA-256 and Task SHA-256.
- Applied MAP08_06 patch receipt SHA-256.
- Exact Crater↔Root candidate ID set and microchunk ID set.
- Profile/orientation matrix proof.
- CSV row delta summary for the exact 4 owned Authoring CSV files.
- 6 owned candidate rows, 6 owned microchunk catalog rows, 576 owned tile-cell rows, and 12 owned socket rows proof.
- Warning category evidence for every owned candidate.
- Edge signature and socket compatibility proof.
- Resolver/filter/warning ownership preservation evidence.
- Exact Unity test counts.
- Compile/Console/warning counts.
- Assets meta counts and duplicate GUID scan.
- Authoring manifest before/after SHA-256.
- Generated CSV / Scene / Prefab / ProjectSettings / Packages / asmdef / asmref unchanged proof.
- Forbidden MAP08_07+/MAP09+ symbol scan result.
- Commit hash or exact no-commit blocker.

If any required gate fails, report `STATUS: FAIL` or `STATUS: BLOCKED` and do not open MAP08_07.
