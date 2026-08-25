# MAP08_09 - Author Root Mill Boundaries

```yaml
status_control:
  task_key: MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES
  result_file: REPORTS/MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES_RESULT.md
```

## TASK TYPE

```text
PAIR_ROOT_MILL AUTHORING CSV CANDIDATE MATRIX + VALIDATION TESTS
```

## Objective

MAP08_08 PASS/finalize 뒤 `PAIR_ROOT_MILL` CassiaRoot<->AbandonedMill boundary 후보를 실제 Authoring CSV와 Runtime validator로 완성한다.

이 Task는 Root<->Mill pair 하나만 연다. Root<->Dough, Mill<->Dough boundary content authoring, generated CSV writer, sector assembly, Scene/Prefab output은 구현하지 않는다. `MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES`와 이후 Task body는 읽거나 시작하지 않는다.

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
12. `REPORTS/MAP08_08_AUTHOR_CRATER_DOUGH_BOUNDARIES_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP08_08_AUTHOR_CRATER_DOUGH_BOUNDARIES
STATUS: PASS
MAP08_08: COMPLETE ELIGIBLE
MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES: LOCKED / DO NOT START
SHA-256: df4137defbb8e44cba12ef3b74cd8635044b886657525ec128e05dd5b1bd67c9
```

이 별도 patch가 적용된 뒤에만 MAP08_09를 실행한다. MAP08_10 이후 Task body는 읽거나 시작하지 않는다.

## Approved Input Baseline

```text
Unity: 6000.3.8f1
Runtime assembly: Game.Map.Runtime
Runtime namespace boundary: StarNight.Map.WorldGeneration.*
Runtime EditMode assembly: Game.Map.Tests.EditMode
Editor assembly: MapAuthoring.Editor
Editor EditMode assembly: MapAuthoring.Tests.EditMode
New asmdef/asmref: NO
MAP08_08 Result SHA-256: df4137defbb8e44cba12ef3b74cd8635044b886657525ec128e05dd5b1bd67c9
MAP08_08 installed Task SHA-256: 92106729b49fa13c0fbb95f0338b3b619582a1696f10ac88604401469907d769
MAP08_08 focused tests: 720/720 PASS
MAP08 required total: 4860/4860 PASS
MAP07 required total: 5422/5422 PASS
MAP06 required total: 2746/2746 PASS
MAP05 required total: 1959/1959 PASS
Actually executed required subset total: 14987/14987 PASS
MAP08_08 failed/skipped: 0/0
MAP08_08 compile/Console/relevant warnings: 0/0/0
Global Assets meta: 3699
Assets/_Game/Map meta: 578
Authoring CSV/meta: 50 / 50
Authoring manifest SHA-256 before MAP08_09: 61d5462d00b7d4f435297523be15d0bef636dfc84a87b05004b209928bacce1b
Generated CSV files created by MAP08_08: 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes by MAP08_08: 0
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

Reference는 `PAIR_ROOT_MILL` authoring matrix, CSV schema, socket/signature compatibility 확인 용도다. MAP08_10+ Task 문서와 다른 pair boundary content body는 읽지 않는다.

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
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/*
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/*
```

금지: MAP08_10+ Task body, other-pair authored boundary content body, generated output body, MAP09 sector assembly implementation body, Legacy/Stage/P6/P11 generator body, Scene/Prefab YAML.

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
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceRootMillBoundaryAuthoringContract.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceRootMillBoundaryCandidateMatrix.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceRootMillBoundaryContentReport.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceRootMillBoundaryValidator.cs
```

Matching `.cs.meta` files are required. Existing `Boundaries` folders from MAP08_01 must be reused; new folder meta must remain `0`.

### 신규 Runtime EditMode tests - exact 2

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries/MoonpalaceRootMillBoundaryAuthoringTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries/MoonpalaceRootMillBoundaryValidatorTests.cs
```

Matching `.cs.meta` files are required.

### Existing MAP08 boundary files - exact up to 28

기존 MAP08_01~MAP08_08 boundary Runtime/Test C#는 Root<->Mill authoring validation integration을 위해 필요한 최소 범위만 수정할 수 있다. Matching existing `.cs.meta`는 수정하지 않는다.

### Result report - exact 1

```text
MapDesign/MCP/REPORTS/MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES_RESULT.md
```

## Required Authoring Contract

### Pair identity

The only owned pair is:

```text
boundary_pair_rule_id: PAIR_ROOT_MILL
biome_a_id: BIO_CASSIA_ROOT
biome_b_id: BIO_ABANDONED_MILL
allowed_boundary_profile_ids: BOUND_RUIN | BOUND_TUNNEL | BOUND_SOFT_BLEND
boundary_profile_weights: 45 | 35 | 20
default_boundary_profile_id: BOUND_RUIN
```

No other pair rule may be changed or newly authored in this Task.

### Candidate matrix

Author exactly one active boundary candidate for every valid Root<->Mill profile/orientation combination:

```text
BOUND_RUIN       / HORIZONTAL
BOUND_RUIN       / VERTICAL
BOUND_TUNNEL     / HORIZONTAL
BOUND_TUNNEL     / VERTICAL
BOUND_SOFT_BLEND / HORIZONTAL
BOUND_SOFT_BLEND / VERTICAL
```

Required candidate IDs:

```text
BCH_ROOT_MILL_H_RUIN_01
BCH_ROOT_MILL_V_RUIN_01
BCH_ROOT_MILL_H_TUNNEL_01
BCH_ROOT_MILL_V_TUNNEL_01
BCH_ROOT_MILL_H_SOFT_01
BCH_ROOT_MILL_V_SOFT_01
```

Required microchunk IDs:

```text
MC_BOUND_ROOT_MILL_H_RUIN_01
MC_BOUND_ROOT_MILL_V_RUIN_01
MC_BOUND_ROOT_MILL_H_TUNNEL_01
MC_BOUND_ROOT_MILL_V_TUNNEL_01
MC_BOUND_ROOT_MILL_H_SOFT_01
MC_BOUND_ROOT_MILL_V_SOFT_01
```

### Edge signatures and sockets

Horizontal candidates must use L/R `WALK` sockets with `EDGE_H_MID_WALK`. Vertical candidates must use U/D `CLIMB` sockets with `EDGE_V_CENTER_CLIMB`. All sockets must be `MANDATORY`, `mandatory_allowed=1`, `tool_requirement=NONE`, and `minimum_safe_tiles=2`.

### Tile and warning evidence

Every owned microchunk must have exact `12x8 = 96` local coordinate coverage. Every owned candidate must include:

```text
foreground tile evidence: G_CASSIA_WOOD + G_MILL_METAL
background evidence: DB_ROOT + DB_MILL
route evidence: M_ROUTE_MAIN
socket evidence: M_SOCKET
```

Tile and Background warning evidence category count must be exactly `2` for every owned candidate. Existing warning contract semantics from MAP08_05 must not be weakened.

## Required CSV Deltas

Authoring CSV changes must be additions only:

```text
boundary_chunk_catalog.csv:  +6 / -0
microchunk_catalog.csv:       +6 / -0
microchunk_tile_cells.csv:  +576 / -0
microchunk_sockets.csv:      +12 / -0
```

Required preservation counters:

```text
Existing rows modified: 0
Other pair rows modified: 0
CraterRoot rows modified: 0
CraterMill rows modified: 0
CraterDough rows modified: 0
Generated CSV files created: 0
Matching CSV meta modified: 0
Scene/Prefab tracked changes: 0
ProjectSettings/Packages tracked changes: 0
asmdef/asmref tracked changes: 0
```

## Required Runtime Contract

Create immutable DTO/report/validator surface for Root<->Mill equivalent to the already-approved prior pair pattern:

```text
MoonpalaceRootMillBoundaryAuthoringContract
MoonpalaceRootMillBoundaryCandidateMatrix
MoonpalaceRootMillBoundaryContentReport
MoonpalaceRootMillBoundaryValidator
```

The validator must check pair identity, exact ID sets, exact 6-entry profile/orientation matrix, positive candidate weights, reversible candidate semantics where applicable, 96-cell coverage, required evidence, socket shape, additive-only CSV preservation, no generated CSV output, and no future MAP08_10+ symbols.

## Required Tests

```text
MoonpalaceRootMillBoundaryAuthoringTests: 360/360
MoonpalaceRootMillBoundaryValidatorTests: 360/360
MAP08_09 focused total: 720/720

MAP08 required focused total: 5580/5580
MAP07 required regression: 5422/5422
MAP06 required regression: 2746/2746
MAP05 required regression: 1959/1959
Required subset total: 15707/15707
Failed/skipped: 0/0
Unity compile errors: 0
Final Console errors/warnings: 0/0
Relevant warnings: 0
```

## Static Gates

```text
Global Assets meta: 3699 -> 3705
Assets/_Game/Map meta: 578 -> 582
Duplicate GUID groups: 0

Authoring CSV/matching meta: 50/50
Authoring tracked CSV changes: exact 4
Authoring row deltas: +6 / +6 / +576 / +12
Authoring manifest before: 61d5462d00b7d4f435297523be15d0bef636dfc84a87b05004b209928bacce1b
Authoring manifest after: MUST REPORT

Generated CSV files: 0
Scene/Prefab tracked changes: 0/0
ProjectSettings/Packages tracked changes: 0/0
asmdef/asmref tracked changes: 0/0
MAP08_10+/MAP09+ forbidden production symbol hits: 0/0
Unapplied MCP patches: 0
git diff --check errors: 0
```

## Commit Requirement

After implementation PASS and before final handoff:

```text
git status --short
git diff --check
stage only MAP08_09-owned Authoring CSV, Runtime/Test, MCP, Result, and finalized Status files
exclude unrelated pre-existing files such as solution churn or already-applied older inbox packages
create exactly one git commit
do not git push
```

Commit message format:

```text
MAP08_09: author root mill boundary candidates

- Add six PAIR_ROOT_MILL boundary candidates for RUIN, TUNNEL, and SOFT_BLEND H/V coverage
- Add six matching 12x8 boundary microchunks with Root/Mill tile and background evidence
- Add mandatory no-tool L/R walk and U/D climb socket rows
- Add Root-Mill authoring contract, candidate matrix, report, and validator
- Add focused authoring and validator EditMode coverage
- Preserve existing Authoring rows, CSV meta files, generated CSV output, scenes, prefabs, asmdefs, and future task symbols
- Verify MAP08 focused 5580/5580 and required subset 15707/15707
```

The Result must report the created commit hash. If this environment has no valid git repository, report `COMMIT: BLOCKED - repository unavailable` and include exact `git status`/`git commit` failure text.

## Required Result Header

```text
TASK: MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES
STATUS: PASS | FAIL | BLOCKED
MAP08_09: COMPLETE ELIGIBLE | REPAIR REQUIRED | BLOCKED
MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES: LOCKED / DO NOT START
```
