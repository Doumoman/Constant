# MAP08_10 - Author Root Dough Boundaries

```yaml
status_control:
  task_key: MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES
  result_file: REPORTS/MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES_RESULT.md
```

## TASK TYPE

```text
PAIR_ROOT_DOUGH AUTHORING CSV CANDIDATE MATRIX + VALIDATION TESTS
```

## Objective

MAP08_09 PASS/finalize 뒤 `PAIR_ROOT_DOUGH` CassiaRoot<->MoonDough boundary 후보를 실제 Authoring CSV와 Runtime validator로 완성한다.

이 Task는 Root<->Dough pair 하나만 연다. Mill<->Dough boundary content authoring, generated CSV writer, sector assembly, Scene/Prefab output은 구현하지 않는다. `MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES`와 이후 Task body는 읽거나 시작하지 않는다.

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
12. `REPORTS/MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES
STATUS: PASS
MAP08_09: COMPLETE ELIGIBLE
MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES: LOCKED / DO NOT START
SHA-256: c923b445d9dc7b1d057ec368cb154a5745f2e473e67a736fcf8ee20f66a9ef87
```

이 별도 patch가 적용된 뒤에만 MAP08_10을 실행한다. MAP08_11 이후 Task body는 읽거나 시작하지 않는다.

## Approved Input Baseline

```text
Unity: 6000.3.8f1
Runtime assembly: Game.Map.Runtime
Runtime namespace boundary: StarNight.Map.WorldGeneration.*
Runtime EditMode assembly: Game.Map.Tests.EditMode
Editor assembly: MapAuthoring.Editor
Editor EditMode assembly: MapAuthoring.Tests.EditMode
New asmdef/asmref: NO
MAP08_09 Result SHA-256: c923b445d9dc7b1d057ec368cb154a5745f2e473e67a736fcf8ee20f66a9ef87
MAP08_09 installed Task SHA-256: c559168ad86e866124cc87843926d66b98f59ad1243a33755fce7f78621fd01f
MAP08_09 focused tests: 720/720 PASS
MAP08 required total: 5580/5580 PASS
MAP07 required total: 5422/5422 PASS
MAP06 required total: 2746/2746 PASS
MAP05 required total: 1959/1959 PASS
Actually executed required subset total: 15707/15707 PASS
MAP08_09 failed/skipped: 0/0
MAP08_09 compile/Console/relevant warnings: 0/0/0
Global Assets meta: 3705
Assets/_Game/Map meta: 582
Authoring CSV/meta: 50 / 50
Authoring manifest SHA-256 before MAP08_10: b67b1235806a1acb4d5163917aa97ac93863e3cfba29c7842f656afc0d57096a
Generated CSV files created by MAP08_09: 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes by MAP08_09: 0
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

Reference는 `PAIR_ROOT_DOUGH` authoring matrix, CSV schema, socket/signature compatibility 확인 용도다. MAP08_11+ Task 문서와 다른 pair boundary content body는 읽지 않는다.

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

금지: MAP08_11+ Task body, other-pair authored boundary content body, generated output body, MAP09 sector assembly implementation body, Legacy/Stage/P6/P11 generator body, Scene/Prefab YAML.

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
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceRootDoughBoundaryAuthoringContract.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceRootDoughBoundaryCandidateMatrix.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceRootDoughBoundaryContentReport.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceRootDoughBoundaryValidator.cs
```

Matching `.cs.meta` files are required. Existing `Boundaries` folders from MAP08_01 must be reused; new folder meta must remain `0`.

### 신규 Runtime EditMode tests - exact 2

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries/MoonpalaceRootDoughBoundaryAuthoringTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries/MoonpalaceRootDoughBoundaryValidatorTests.cs
```

Matching `.cs.meta` files are required.

### Existing MAP08 boundary files - exact up to 32

기존 MAP08_01~MAP08_09 boundary Runtime/Test C#는 Root<->Dough authoring validation integration을 위해 필요한 최소 범위만 수정할 수 있다. Matching existing `.cs.meta`는 수정하지 않는다.

### Result report - exact 1

```text
MapDesign/MCP/REPORTS/MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES_RESULT.md
```

## Required Authoring Contract

### Pair identity

The only owned pair is:

```text
boundary_pair_rule_id: PAIR_ROOT_DOUGH
biome_a_id: BIO_CASSIA_ROOT
biome_b_id: BIO_MOON_DOUGH
allowed_boundary_profile_ids: BOUND_TUNNEL | BOUND_LAYER | BOUND_SOFT_BLEND
boundary_profile_weights: 45 | 30 | 25
default_boundary_profile_id: BOUND_TUNNEL
```

No other pair rule may be changed or newly authored in this Task.

### Candidate matrix

Author exactly one active boundary candidate for every valid Root<->Dough profile/orientation combination:

```text
BOUND_TUNNEL     / HORIZONTAL
BOUND_TUNNEL     / VERTICAL
BOUND_LAYER      / VERTICAL
BOUND_SOFT_BLEND / HORIZONTAL
BOUND_SOFT_BLEND / VERTICAL
```

`BOUND_LAYER` is Vertical-only and must not produce a Horizontal candidate.

Required candidate IDs:

```text
BCH_ROOT_DOUGH_H_TUNNEL_01
BCH_ROOT_DOUGH_V_TUNNEL_01
BCH_ROOT_DOUGH_V_LAYER_01
BCH_ROOT_DOUGH_H_SOFT_01
BCH_ROOT_DOUGH_V_SOFT_01
```

Required microchunk IDs:

```text
MC_BOUND_ROOT_DOUGH_H_TUNNEL_01
MC_BOUND_ROOT_DOUGH_V_TUNNEL_01
MC_BOUND_ROOT_DOUGH_V_LAYER_01
MC_BOUND_ROOT_DOUGH_H_SOFT_01
MC_BOUND_ROOT_DOUGH_V_SOFT_01
```

### Edge signatures and sockets

Horizontal candidates must use L/R `WALK` sockets with `EDGE_H_MID_WALK`. Vertical candidates must use U/D `CLIMB` sockets with `EDGE_V_CENTER_CLIMB`. All sockets must be `MANDATORY`, `mandatory_allowed=1`, `tool_requirement=NONE`, and `minimum_safe_tiles=2`.

### Tile and warning evidence

Every owned microchunk must have exact `12x8 = 96` local coordinate coverage. Every owned candidate must include:

```text
foreground tile evidence: G_CASSIA_WOOD + G_DOUGH_SOLID
background evidence: DB_ROOT + DB_DOUGH
route evidence: M_ROUTE_MAIN
socket evidence: M_SOCKET
```

Tile and Background warning evidence category count must be exactly `2` for every owned candidate. Existing warning contract semantics from MAP08_05 must not be weakened.

## Required CSV Deltas

Authoring CSV changes must be additions only:

```text
boundary_chunk_catalog.csv:  +5 / -0
microchunk_catalog.csv:       +5 / -0
microchunk_tile_cells.csv:  +480 / -0
microchunk_sockets.csv:      +10 / -0
```

Required preservation counters:

```text
Existing rows modified: 0
Other pair rows modified: 0
CraterRoot rows modified: 0
CraterMill rows modified: 0
CraterDough rows modified: 0
RootMill rows modified: 0
Generated CSV files created: 0
Matching CSV meta modified: 0
Scene/Prefab tracked changes: 0
ProjectSettings/Packages tracked changes: 0
asmdef/asmref tracked changes: 0
```

## Required Runtime Contract

Create immutable DTO/report/validator surface for Root<->Dough equivalent to the already-approved prior pair pattern:

```text
MoonpalaceRootDoughBoundaryAuthoringContract
MoonpalaceRootDoughBoundaryCandidateMatrix
MoonpalaceRootDoughBoundaryContentReport
MoonpalaceRootDoughBoundaryValidator
```

The validator must check pair identity, exact ID sets, exact 5-entry profile/orientation matrix, positive candidate weights, `BOUND_LAYER/HORIZONTAL` invalid count `0`, reversible candidate semantics where applicable, 96-cell coverage, required evidence, socket shape, additive-only CSV preservation, no generated CSV output, and no future MAP08_11+ symbols.

## Required Tests

```text
MoonpalaceRootDoughBoundaryAuthoringTests: 360/360
MoonpalaceRootDoughBoundaryValidatorTests: 360/360
MAP08_10 focused total: 720/720

MAP08 required focused total: 6300/6300
MAP07 required regression: 5422/5422
MAP06 required regression: 2746/2746
MAP05 required regression: 1959/1959
Required subset total: 16427/16427
Failed/skipped: 0/0
Unity compile errors: 0
Final Console errors/warnings: 0/0
Relevant warnings: 0
```

## Static Gates

```text
Global Assets meta: 3705 -> 3711
Assets/_Game/Map meta: 582 -> 586
Duplicate GUID groups: 0

Authoring CSV/matching meta: 50/50
Authoring tracked CSV changes: exact 4
Authoring row deltas: +5 / +5 / +480 / +10
Authoring manifest before: b67b1235806a1acb4d5163917aa97ac93863e3cfba29c7842f656afc0d57096a
Authoring manifest after: MUST REPORT

Generated CSV files: 0
Scene/Prefab tracked changes: 0/0
ProjectSettings/Packages tracked changes: 0/0
asmdef/asmref tracked changes: 0/0
MAP08_11+/MAP09+ forbidden production symbol hits: 0/0
Unapplied MCP patches: 0
git diff --check errors: 0
```

## Commit Requirement

After implementation PASS and before final handoff:

```text
git status --short
git diff --check
stage only MAP08_10-owned Authoring CSV, Runtime/Test, MCP, Result, and finalized Status files
exclude unrelated pre-existing files such as solution churn or already-applied older inbox packages
create exactly one git commit
do not git push
```

Commit message format:

```text
MAP08_10: author root dough boundary candidates

- Add five PAIR_ROOT_DOUGH boundary candidates for TUNNEL, LAYER, and SOFT_BLEND coverage
- Add five matching 12x8 boundary microchunks with Root/Dough tile and background evidence
- Add mandatory no-tool L/R walk and U/D climb socket rows
- Add Root-Dough authoring contract, candidate matrix, report, and validator
- Add focused authoring and validator EditMode coverage
- Preserve existing Authoring rows, CSV meta files, generated CSV output, scenes, prefabs, asmdefs, and future task symbols
- Verify MAP08 focused 6300/6300 and required subset 16427/16427
```

The Result must report the created commit hash. If this environment has no valid git repository, report `COMMIT: BLOCKED - repository unavailable` and include exact `git status`/`git commit` failure text.

## Required Result Header

```text
TASK: MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES
STATUS: PASS | FAIL | BLOCKED
MAP08_10: COMPLETE ELIGIBLE | REPAIR REQUIRED | BLOCKED
MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES: LOCKED / DO NOT START
```
