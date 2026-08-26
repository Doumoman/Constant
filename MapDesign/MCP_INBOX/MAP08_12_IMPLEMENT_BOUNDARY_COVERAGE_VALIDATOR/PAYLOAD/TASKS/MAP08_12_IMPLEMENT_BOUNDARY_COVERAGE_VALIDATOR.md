# MAP08_12 - Implement Boundary Coverage Validator

```yaml
status_control:
  task_key: MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR
  result_file: REPORTS/MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR_RESULT.md
```

## TASK TYPE

```text
MOONPALACE BOUNDARY COVERAGE VALIDATOR + AGGREGATE COVERAGE TESTS
```

## Objective

MAP08_11 PASS/finalize 뒤 MAP08_01~MAP08_11에서 만든 Moonpalace boundary content 전체가 필수 coverage를 만족하는지 검증하는 Runtime validator를 구현한다.

이 Task는 coverage validation만 연다. Boundary preview window, generated CSV writer, sector assembly, Scene/Prefab output은 구현하지 않는다. `MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW`와 이후 Task body는 읽거나 시작하지 않는다.

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
12. `REPORTS/MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES
STATUS: PASS
MAP08_11: COMPLETE ELIGIBLE
MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR: LOCKED / DO NOT START
SHA-256: 9c9ce342563858987b2489ae6aa9a50bee2473be4639b07cd7176ee18bcbde4c
```

이 별도 patch가 적용된 뒤에만 MAP08_12를 실행한다. MAP08_13 이후 Task body는 읽거나 시작하지 않는다.

## Approved Input Baseline

```text
Unity: 6000.3.8f1
Runtime assembly: Game.Map.Runtime
Runtime namespace boundary: StarNight.Map.WorldGeneration.*
Runtime EditMode assembly: Game.Map.Tests.EditMode
Editor assembly: MapAuthoring.Editor
Editor EditMode assembly: MapAuthoring.Tests.EditMode
New asmdef/asmref: NO
MAP08_11 Result SHA-256: 9c9ce342563858987b2489ae6aa9a50bee2473be4639b07cd7176ee18bcbde4c
MAP08_11 installed Task SHA-256: MUST COMPUTE FROM INSTALLED PROJECT FILE
MAP08_11 focused tests: 720/720 PASS
MAP08 pair-authoring categories: 4320/4320 PASS
MAP08_01~05 baseline groups: 2700/2700 PASS
MAP08 required union: 7020/7020 PASS
MAP07 required total: 5422/5422 PASS
MAP06 required total: 2746/2746 PASS
MAP05 required total: 1959/1959 PASS
Completed distinct required subset: 17147/17147 PASS
MAP08_11 failed/skipped: 0/0
MAP08_11 compile/Console/relevant warnings: 0/0/0
Global Assets meta: 3794
Assets/_Game/Map meta: 590
Authoring CSV/meta: 50 / 50
Authoring manifest SHA-256 before MAP08_12: f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb
Generated CSV files created by MAP08_11: 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes by MAP08_11: 0
Duplicate GUID groups: 0
```

The uploaded MAP08_11 Result does not report the installed MAP08_11 Task SHA-256. The implementer must compute:

```text
sha256sum MapDesign/MCP/TASKS/MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES.md
```

and include it in the MAP08_12 Result. Do not skip this source-chain check.

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

Reference는 complete coverage expectations, CSV schema, socket/signature compatibility 확인 용도다. MAP08_13+ Task 문서와 preview/generated/sector body는 읽지 않는다.

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

금지: MAP08_13+ Task body, preview window body, generated output body, MAP09 sector assembly implementation body, Legacy/Stage/P6/P11 generator body, Scene/Prefab YAML.

## WRITE ALLOWLIST

### 신규 Runtime production C# - exact 6

```text
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBoundaryCoverageRequirement.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBoundaryCoverageCandidateEvidence.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBoundaryCoverageIssue.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBoundaryCoveragePairReport.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBoundaryCoverageReport.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBoundaryCoverageValidator.cs
```

Matching `.cs.meta` files are required. Existing `Boundaries` folders from MAP08_01 must be reused; new folder meta must remain `0`.

### 신규 Runtime EditMode tests - exact 2

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries/MoonpalaceBoundaryCoverageValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries/MoonpalaceBoundaryCoverageReportTests.cs
```

Matching `.cs.meta` files are required.

### Existing MAP08 boundary files - exact up to 38

기존 MAP08_01~MAP08_11 boundary Runtime/Test C#는 coverage validator integration을 위해 필요한 최소 범위만 수정할 수 있다. Matching existing `.cs.meta`는 수정하지 않는다.

### Result report - exact 1

```text
MapDesign/MCP/REPORTS/MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR_RESULT.md
```

## Required Coverage Contract

### Complete pair set

Validate exactly six Moonpalace pair rules:

```text
PAIR_CRATER_ROOT
PAIR_CRATER_MILL
PAIR_CRATER_DOUGH
PAIR_ROOT_MILL
PAIR_ROOT_DOUGH
PAIR_MILL_DOUGH
```

The validator must reject missing, duplicate, inactive, self-pair, unknown-biome, or non-canonical pair records.

### Expected authored candidate matrix

The full authored matrix must be exactly:

```text
PAIR_CRATER_ROOT: 6 candidates, 6 microchunks, 576 tile rows, 12 sockets
PAIR_CRATER_MILL: 4 candidates, 4 microchunks, 384 tile rows, 8 sockets
PAIR_CRATER_DOUGH: 5 candidates, 5 microchunks, 480 tile rows, 10 sockets
PAIR_ROOT_MILL: 6 candidates, 6 microchunks, 576 tile rows, 12 sockets
PAIR_ROOT_DOUGH: 5 candidates, 5 microchunks, 480 tile rows, 10 sockets
PAIR_MILL_DOUGH: 5 candidates, 5 microchunks, 480 tile rows, 10 sockets

Total active authored boundary candidates: 31
Total backing boundary microchunks: 31
Total boundary tile rows: 2976
Total boundary sockets: 62
```

Every pair must have at least one HORIZONTAL and at least one VERTICAL candidate. For `BOUND_LAYER`, only VERTICAL candidates are valid. `BOUND_LAYER/HORIZONTAL` count must be `0`.

### Profile and weight coverage

Validate the profile matrix from `biome_boundary_pair_rules.csv`:

```text
PAIR_CRATER_ROOT: BOUND_SOFT_BLEND | BOUND_CLIFF | BOUND_TUNNEL, weights 50 | 25 | 25, default BOUND_SOFT_BLEND
PAIR_CRATER_MILL: BOUND_RUIN | BOUND_SOFT_BLEND, weights 70 | 30, default BOUND_RUIN
PAIR_CRATER_DOUGH: BOUND_CLIFF | BOUND_LAYER | BOUND_SOFT_BLEND, weights 45 | 35 | 20, default BOUND_CLIFF
PAIR_ROOT_MILL: BOUND_RUIN | BOUND_TUNNEL | BOUND_SOFT_BLEND, weights 45 | 35 | 20, default BOUND_RUIN
PAIR_ROOT_DOUGH: BOUND_TUNNEL | BOUND_LAYER | BOUND_SOFT_BLEND, weights 45 | 30 | 25, default BOUND_TUNNEL
PAIR_MILL_DOUGH: BOUND_RUIN | BOUND_LAYER | BOUND_TUNNEL, weights 45 | 30 | 25, default BOUND_RUIN
```

Every active candidate must have a positive weight, an allowed profile for its pair, an allowed orientation for its profile, and `reversible=1` unless an existing approved pair validator documents a stricter asymmetric exception.

### Socket, route, and tool coverage

Horizontal candidates must have exactly L/R WALK sockets using `EDGE_H_MID_WALK`. Vertical candidates must have exactly U/D CLIMB sockets using `EDGE_V_CENTER_CLIMB`.

Every socket must satisfy:

```text
route_layer: MANDATORY
mandatory_allowed: 1
tool_requirement: NONE
minimum_safe_tiles: 2
```

The validator must reject missing sockets, extra sockets, duplicate sides, wrong traversal kind, wrong edge signature, tool requirements, non-mandatory route layer, and unsafe minimum tiles.

### 96-cell and evidence coverage

Every backing microchunk must have exactly `12x8 = 96` unique local cells. Use existing MAP07 coverage types where available.

Every candidate must expose:

```text
foreground tile evidence for both biomes
background evidence for both biomes
route evidence: M_ROUTE_MAIN
socket evidence: M_SOCKET
warning marker category count >= 2
```

The validator must preserve existing MAP08_05 warning semantics and report missing evidence with pair/candidate/microchunk identifiers.

## Required Output Model

`MoonpalaceBoundaryCoverageReport` must include:

```text
accepted
pair_report_count
candidate_count_total
microchunk_count_total
tile_row_count_total
socket_row_count_total
orientation_coverage
profile_coverage
generated_csv_count
authoring_manifest_sha256
issue_list
stable_digest
```

Issue ordering must be deterministic by pair order, orientation, profile, candidate ID, microchunk ID, then issue code.

Minimum issue codes:

```text
MissingPair
UnexpectedPair
MissingOrientation
MissingProfile
UnexpectedProfile
InvalidProfileOrientation
MissingCandidate
DuplicateCandidate
MissingMicrochunk
DuplicateMicrochunk
InvalidTileCoverage
MissingSocket
InvalidSocket
ToolRequired
MissingWarningEvidence
GeneratedCsvPresent
AuthoringMutationDetected
InvalidSourceChain
```

## Preservation Gates

This Task must not author new boundary CSV rows.

```text
Authoring CSV row deltas: +0 / -0
Generated CSV files: 0
Matching CSV meta modified: 0
Scene/Prefab tracked changes: 0/0
ProjectSettings/Packages tracked changes: 0/0
asmdef/asmref tracked changes: 0/0
MAP08_13+/MAP09+ forbidden production symbol hits: 0/0
```

## Required Tests

```text
MoonpalaceBoundaryCoverageValidatorTests: 420/420
MoonpalaceBoundaryCoverageReportTests: 300/300
MAP08_12 focused total: 720/720

MAP08 required union: 7740/7740
MAP07 required regression: 5422/5422
MAP06 required regression: 2746/2746
MAP05 required regression: 1959/1959
Required subset total: 17867/17867
Failed/skipped: 0/0
Unity compile errors: 0
Final Console errors/warnings: 0/0
Relevant warnings: 0
```

## Static Gates

```text
Global Assets meta: 3794 -> 3802
Assets/_Game/Map meta: 590 -> 596
Duplicate GUID groups: 0

Authoring CSV/matching meta: 50/50
Authoring tracked CSV changes: 0
Authoring manifest before: f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb
Authoring manifest after: f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb

Generated CSV files: 0
Scene/Prefab tracked changes: 0/0
ProjectSettings/Packages tracked changes: 0/0
asmdef/asmref tracked changes: 0/0
MAP08_13+/MAP09+ forbidden production symbol hits: 0/0
Unapplied MCP patches: 0
git diff --check errors: 0
```

## Commit Requirement

After implementation PASS and before final handoff:

```text
git status --short
git diff --check
stage only MAP08_12-owned Runtime/Test, MCP, Result, and finalized Status files
exclude unrelated pre-existing files such as solution churn or already-applied older inbox packages
create exactly one git commit
do not git push
```

Commit message format:

```text
MAP08_12: validate moonpalace boundary coverage

- Add aggregate boundary coverage requirement, candidate evidence, pair report, report, issue, and validator contracts
- Validate all six Moonpalace biome pairs, both orientations, allowed profiles, candidate counts, and source-chain manifest
- Validate 31 boundary candidates, 31 microchunks, 2976 tile rows, and 62 mandatory no-tool sockets
- Preserve Authoring CSV rows, generated CSV output, scenes, prefabs, asmdefs, and future task symbols
- Verify MAP08 required union 7740/7740 and required subset 17867/17867
```

The Result must report the created commit hash. If this environment has no valid git repository, report `COMMIT: BLOCKED - repository unavailable` and include exact `git status`/`git commit` failure text.

## Required Result Header

```text
TASK: MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR
STATUS: PASS | FAIL | BLOCKED
MAP08_12: COMPLETE ELIGIBLE | REPAIR REQUIRED | BLOCKED
MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW: LOCKED / DO NOT START
```
