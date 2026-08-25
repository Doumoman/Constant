# MAP08_11 - Author Mill Dough Boundaries

```yaml
status_control:
  task_key: MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES
  result_file: REPORTS/MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES_RESULT.md
```

## TASK TYPE

```text
PAIR_MILL_DOUGH AUTHORING CSV CANDIDATE MATRIX + VALIDATION TESTS
```

## Objective

MAP08_10 PASS/finalize 뒤 `PAIR_MILL_DOUGH` AbandonedMill<->MoonDough boundary 후보를 실제 Authoring CSV와 Runtime validator로 완성한다.

이 Task는 Mill<->Dough pair 하나만 연다. 전체 six-pair coverage validator, preview window, MAP08 exit test, generated CSV writer, sector assembly, Scene/Prefab output은 구현하지 않는다. `MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR`와 이후 Task body는 읽거나 시작하지 않는다.

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
12. `REPORTS/MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES
STATUS: PASS
MAP08_10: COMPLETE ELIGIBLE
MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES: LOCKED / DO NOT START
SHA-256: 058b5ed32dbca7ca06adf12595ac693f5218dec5f8ed0bfe14d5c12c03563f5a
```

이 별도 patch가 적용된 뒤에만 MAP08_11을 실행한다. MAP08_12 이후 Task body는 읽거나 시작하지 않는다.

## Approved Input Baseline

```text
Unity: 6000.3.8f1
Runtime assembly: Game.Map.Runtime
Runtime namespace boundary: StarNight.Map.WorldGeneration.*
Runtime EditMode assembly: Game.Map.Tests.EditMode
Editor assembly: MapAuthoring.Editor
Editor EditMode assembly: MapAuthoring.Tests.EditMode
New asmdef/asmref: NO
MAP08_10 Result SHA-256: 058b5ed32dbca7ca06adf12595ac693f5218dec5f8ed0bfe14d5c12c03563f5a
MAP08_10 installed Task SHA-256: f8050e209aa342602616afadc7cf0c3258731c8034ab9aff94f2bd94918d04f8
MAP08_10 focused tests: 720/720 PASS
MAP08 required total: 6300/6300 PASS
MAP07 required total: 5422/5422 PASS
MAP06 required total: 2746/2746 PASS
MAP05 required total: 1959/1959 PASS
Actually executed required subset total: 16427/16427 PASS
MAP08_10 failed/skipped: 0/0
MAP08_10 compile/Console/relevant warnings: 0/0/0
Global Assets meta at accepted current baseline: 3788
Assets/_Game/Map meta: 586
Authoring CSV/meta: 50 / 50
Authoring manifest SHA-256 before MAP08_11: 0842d140f399da076cf41218b360e784cee776c62266bd251f4debb18657a950
Generated CSV files created by MAP08_10: 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes by MAP08_10: 0
Duplicate GUID groups: 0
```

`MAP08_10` 실행 중 신규 소켓 통로가 차단된 상태를 MAP07 회귀가 검출했다. 따라서 이 Task는 처음부터 H 2-cell-high, V 3-cell-wide clear corridor를 R0와 허용 mirror transform 모두에서 보장해야 한다.

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

Reference는 `PAIR_MILL_DOUGH` authoring matrix, CSV schema, socket/signature compatibility 확인 용도다. MAP08_12+ Task 문서와 generated output body는 읽지 않는다.

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

금지: MAP08_12+ Task body, generated output body, MAP09 sector assembly implementation body, Legacy/Stage/P6/P11 generator body, Scene/Prefab YAML.

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
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceMillDoughBoundaryAuthoringContract.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceMillDoughBoundaryCandidateMatrix.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceMillDoughBoundaryContentReport.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceMillDoughBoundaryValidator.cs
```

Matching `.cs.meta` files are required. Existing `Boundaries` folders from MAP08_01 must be reused; new folder meta must remain `0`.

### 신규 Runtime EditMode tests - exact 2

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries/MoonpalaceMillDoughBoundaryAuthoringTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries/MoonpalaceMillDoughBoundaryValidatorTests.cs
```

Matching `.cs.meta` files are required.

### Existing pair authoring integration tests - exact 5 expected

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries/MoonpalaceCraterRootBoundaryAuthoringTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries/MoonpalaceCraterMillBoundaryAuthoringTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries/MoonpalaceCraterDoughBoundaryAuthoringTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries/MoonpalaceRootMillBoundaryAuthoringTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries/MoonpalaceRootDoughBoundaryAuthoringTests.cs
```

오직 non-owned candidate count 기대값을 새 5개 후보만큼 올리는 one-line integration edit만 허용한다. Matching existing `.cs.meta`는 수정하지 않는다.

### Result report - exact 1

```text
MapDesign/MCP/REPORTS/MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES_RESULT.md
```

## Required Authoring Contract

### Pair identity

The only owned pair is:

```text
boundary_pair_rule_id: PAIR_MILL_DOUGH
biome_a_id: BIO_ABANDONED_MILL
biome_b_id: BIO_MOON_DOUGH
allowed_boundary_profile_ids: BOUND_RUIN | BOUND_LAYER | BOUND_TUNNEL
boundary_profile_weights: 45 | 30 | 25
default_boundary_profile_id: BOUND_RUIN
```

No other pair rule may be changed or newly authored in this Task.

### Candidate matrix

Author exactly one active, reversible, positive-weight boundary candidate for every valid Mill<->Dough profile/orientation combination:

```text
BOUND_RUIN   / HORIZONTAL
BOUND_RUIN   / VERTICAL
BOUND_LAYER  / VERTICAL
BOUND_TUNNEL / HORIZONTAL
BOUND_TUNNEL / VERTICAL
```

`BOUND_LAYER` is Vertical-only and must not produce a Horizontal candidate.

Required candidate IDs:

```text
BCH_MILL_DOUGH_H_RUIN_01
BCH_MILL_DOUGH_V_RUIN_01
BCH_MILL_DOUGH_V_LAYER_01
BCH_MILL_DOUGH_H_TUNNEL_01
BCH_MILL_DOUGH_V_TUNNEL_01
```

Required microchunk IDs:

```text
MC_BOUND_MILL_DOUGH_H_RUIN_01
MC_BOUND_MILL_DOUGH_V_RUIN_01
MC_BOUND_MILL_DOUGH_V_LAYER_01
MC_BOUND_MILL_DOUGH_H_TUNNEL_01
MC_BOUND_MILL_DOUGH_V_TUNNEL_01
```

### Edge signatures, sockets, and clearance

Horizontal candidates must use exactly L/R `WALK` sockets with `EDGE_H_MID_WALK`. Vertical candidates must use exactly U/D `CLIMB` sockets with `EDGE_V_CENTER_CLIMB`. All sockets must be `MANDATORY`, `mandatory_allowed=1`, `tool_requirement=NONE`, and `minimum_safe_tiles=2`.

```text
HORIZONTAL: 좌↔우 전체를 관통하는 2-cell-high clear corridor
VERTICAL:   상↔하 전체를 관통하는 3-cell-wide clear corridor
```

통로는 R0와 allowed reverse transform (`MirrorX` for Horizontal, `MirrorY` for Vertical) 모두에서 MAP07 socket-clearance, reachability, starter exit/round-trip gate를 통과해야 한다. 통로 내부에 collision-blocking ground/breakable cell을 두지 않는다.

### Tile and warning evidence

Every owned microchunk must have exact `12x8 = 96` local coordinate coverage. Every owned candidate must include:

```text
foreground tile evidence: G_MILL_METAL + G_DOUGH_SOLID
background evidence: DB_MILL + DB_DOUGH
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
RootDough rows modified: 0
Generated CSV files created: 0
Matching CSV meta modified: 0
Scene/Prefab tracked changes: 0
ProjectSettings/Packages tracked changes: 0
asmdef/asmref tracked changes: 0
```

## Required Runtime Contract

Create immutable DTO/report/validator surface for Mill<->Dough equivalent to the already-approved prior pair pattern:

```text
MoonpalaceMillDoughBoundaryAuthoringContract
MoonpalaceMillDoughBoundaryCandidateMatrix
MoonpalaceMillDoughBoundaryContentReport
MoonpalaceMillDoughBoundaryValidator
```

The validator must check pair identity, exact ID sets, exact 5-entry profile/orientation matrix, positive candidate weights, `BOUND_LAYER/HORIZONTAL` invalid count `0`, reversible candidate semantics, 96-cell coverage, required evidence, socket shape and transform clearance, additive-only CSV preservation, no generated CSV output, and no future MAP08_12+ symbols.

Expected prior-pair non-owned candidate count updates:

```text
CraterRoot:  20 -> 25
CraterMill:  22 -> 27
CraterDough: 21 -> 26
RootMill:    20 -> 25
RootDough:   21 -> 26
```

## Required Tests

```text
MoonpalaceMillDoughBoundaryAuthoringTests: 360/360
MoonpalaceMillDoughBoundaryValidatorTests: 360/360
MAP08_11 focused total: 720/720

MAP08 required focused total: 7020/7020
MAP07 required regression: 5422/5422
MAP06 required regression: 2746/2746
MAP05 required regression: 1959/1959
Required subset total: 17147/17147
Failed/skipped: 0/0
Unity compile errors: 0
Final Console errors/warnings: 0/0
Relevant warnings: 0
```

MAP07 required regression은 신규 5개 microchunk의 R0/allowed mirror socket clearance를 포함해야 한다. 캐시가 이전 CSV를 유지한 정황이 있으면 domain reload 후 같은 authoritative job을 확인하되, 실행 중인 job을 중복 시작하지 않는다.

## Static Gates

```text
Global Assets meta logical delta: 3788 -> 3794 (+6)
Assets/_Game/Map meta: 586 -> 590 (+4)
Duplicate GUID groups: 0

Authoring CSV/matching meta: 50/50
Authoring tracked CSV changes: exact 4
Authoring row deltas: +5 / +5 / +480 / +10
Authoring manifest before: 0842d140f399da076cf41218b360e784cee776c62266bd251f4debb18657a950
Authoring manifest after: MUST REPORT

Generated CSV files: 0
Scene/Prefab tracked changes: 0/0
ProjectSettings/Packages tracked changes: 0/0
asmdef/asmref tracked changes: 0/0
MAP08_12+/MAP09+ forbidden production symbol hits: 0/0
Unapplied MCP patches: 0
git diff --check errors: 0
```

병렬 Live/character 커밋으로 non-Map global meta baseline이 이동했다면 현재 HEAD의 실제 baseline을 보존하고 `+6` 산술과 Map-only `+4`를 별도 증명한다. 그 드리프트는 `Assets/_Game/Map` 및 이 Task 소유 범위를 침범하지 않아야 한다.

## Commit Requirement

After implementation PASS and before final handoff:

```text
git status --short
git diff --check
stage only MAP08_11-owned Authoring CSV, Runtime/Test, MCP, Result, and finalized Status files
create one detailed git commit
report the exact commit hash in the final handoff
do not stage unrelated pre-existing worktree files
do not git push
```

## Required Result Header

```text
TASK: MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES
STATUS: PASS | FAIL | BLOCKED
MAP08_11: COMPLETE ELIGIBLE | REPAIR REQUIRED | BLOCKED
MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR: LOCKED / DO NOT START
```

## Finish Rule

- PASS: Result와 finalized Status를 atomic commit하고 커밋 해시를 최종 handoff에 기록한다. MAP08_12는 자동 시작하지 않는다.
- FAIL/BLOCKED: MAP08_11 repair/resume만 허용하고 MAP08_12 이후는 LOCKED로 유지한다.

