# MAP08_13 - Create Boundary Preview Window

```yaml
status_control:
  task_key: MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW
  result_file: REPORTS/MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW_RESULT.md
```

## TASK TYPE

```text
EDITOR-ONLY MOONPALACE BOUNDARY PREVIEW WINDOW + DIAGNOSTIC REPORT
```

## Objective

MAP08_12 PASS/finalize 뒤 MAP08 coverage validator가 만든 aggregate report를 사람이 검토할 수 있는 Editor preview window로 표시한다.

이 Task는 boundary preview UI만 연다. Runtime coverage rule, Authoring CSV, generated CSV writer, sector assembly, Scene/Prefab output, MAP08_14 exit tests는 구현하지 않는다. `MAP08_14_MAP08_EXIT_TESTS`와 이후 Task body는 읽거나 시작하지 않는다.

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
12. `REPORTS/MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR
STATUS: PASS
MAP08_12: COMPLETE ELIGIBLE
MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW: LOCKED / DO NOT START
SHA-256: 26801f7bc31d354b9639278ec133970e7840f3c7dacb9f0f2a4b6a0e0288896b
```

이 별도 patch가 적용된 뒤에만 MAP08_13을 실행한다. MAP08_14 이후 Task body는 읽거나 시작하지 않는다.

## Approved Input Baseline

```text
Unity: 6000.3.8f1
Runtime assembly: Game.Map.Runtime
Runtime namespace boundary: StarNight.Map.WorldGeneration.*
Runtime EditMode assembly: Game.Map.Tests.EditMode
Editor assembly: MapAuthoring.Editor
Editor EditMode assembly: MapAuthoring.Tests.EditMode
New asmdef/asmref: NO
MAP08_10 external Result SHA-256: 058b5ed32dbca7ca06adf12595ac693f5218dec5f8ed0bfe14d5c12c03563f5a
MAP08_10 installed Task SHA-256: f8050e209aa342602616afadc7cf0c3258731c8034ab9aff94f2bd94918d04f8
MAP08_11 installed Task SHA-256: 67f2852a01e19d61a78160e6cae79c77b4103ccf2d378e98c7e08becfcb3fda5
MAP08_12 Result SHA-256: 26801f7bc31d354b9639278ec133970e7840f3c7dacb9f0f2a4b6a0e0288896b
MAP08_12 installed Task SHA-256: cdaadd203562acdcad7024349499a2df3ac0ccd9860ef580473e984ae9490966
Boundary coverage accepted: true
Boundary coverage aggregate digest: f7ff1c49f5bc33a4ad57799269bc3915806fe0cb60f347ed76eb16ea26f7fc68
Coverage candidates/microchunks/tile rows/socket rows: 31/31/2976/62
Coverage issues: 0
MAP08_12 focused tests: 720/720 PASS
MAP08 required union: 7740/7740 PASS
MAP07 required total: 5422/5422 PASS
MAP06 required total: 2746/2746 PASS
MAP05 required total: 1959/1959 PASS
Completed distinct required subset: 17867/17867 PASS
MAP08_12 failed/skipped: 0/0
MAP08_12 compile/Console/relevant warnings: 0/0/0
Global Assets meta: 3802
Assets/_Game/Map meta: 596
Authoring CSV/meta: 50 / 50
Authoring manifest SHA-256 before MAP08_13: f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb
Generated CSV files created by MAP08_12: 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes by MAP08_12: 0
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

Reference는 preview 표시 항목, CSV schema, socket/signature naming 확인 용도다. MAP08_14+ exit body, generated output body, MAP09 sector assembly body는 읽지 않는다.

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
Assets/_Game/Editor/MapAuthoring/*
Assets/_Game/Editor/MapAuthoring/Microchunks/*
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/*
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks/*
```

금지: MAP08_14+ Task body, generated output body, MAP09 sector assembly implementation body, Legacy/Stage/P6/P11 generator body, Scene/Prefab YAML.

## WRITE ALLOWLIST

### 신규 Editor production C# - exact 7

```text
Assets/_Game/Editor/MapAuthoring/Boundaries/MoonpalaceBoundaryPreviewSelection.cs
Assets/_Game/Editor/MapAuthoring/Boundaries/MoonpalaceBoundaryPreviewOverlayToggle.cs
Assets/_Game/Editor/MapAuthoring/Boundaries/MoonpalaceBoundaryPreviewCell.cs
Assets/_Game/Editor/MapAuthoring/Boundaries/MoonpalaceBoundaryPreviewIssueView.cs
Assets/_Game/Editor/MapAuthoring/Boundaries/MoonpalaceBoundaryPreviewReport.cs
Assets/_Game/Editor/MapAuthoring/Boundaries/MoonpalaceBoundaryPreviewViewModel.cs
Assets/_Game/Editor/MapAuthoring/Boundaries/MoonpalaceBoundaryPreviewWindow.cs
```

Matching `.cs.meta` files are required.

Folder `.meta` allowance:

```text
Assets/_Game/Editor/MapAuthoring/Boundaries.meta
```

Create this folder `.meta` only if the `Boundaries` folder did not already exist. The Result must report whether it was created.

### 신규 Editor EditMode tests - exact 2

```text
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Boundaries/MoonpalaceBoundaryPreviewViewModelTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Boundaries/MoonpalaceBoundaryPreviewWindowTests.cs
```

Matching `.cs.meta` files are required.

Folder `.meta` allowance:

```text
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Boundaries.meta
```

Create this folder `.meta` only if the `Boundaries` test folder did not already exist. The Result must report whether it was created.

### Existing boundary/editor files - exact up to 20

Existing MAP08 boundary Runtime/Test C# and existing MAP07 editor preview/window style files may be read. Modify only if strictly required for public API exposure or shared editor style reuse. Matching existing `.meta` files must not change.

### Result report - exact 1

```text
MapDesign/MCP/REPORTS/MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW_RESULT.md
```

## Preview Contract

### Editor-only boundary

All new production code must compile into `MapAuthoring.Editor`. New Runtime production files are forbidden. Existing MAP08 Runtime validator classes are read-only inputs unless a minimal public API exposure is required and tested.

### Data source

The window must build its view model from the approved MAP08_12 coverage validator/report path. It must not reimplement independent coverage rules that can contradict `MoonpalaceBoundaryCoverageValidator`.

Required immutable source facts:

```text
Accepted: true
Pair reports: 6
Candidates/microchunks/tile rows/socket rows: 31/31/2976/62
Issues: 0
Aggregate stable digest: f7ff1c49f5bc33a4ad57799269bc3915806fe0cb60f347ed76eb16ea26f7fc68
Authoring manifest: f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb
```

### Pair matrix display

The preview must expose the canonical six pair reports and exact expected counts:

```text
PAIR_CRATER_ROOT   6/6/576/12
PAIR_CRATER_MILL   4/4/384/8
PAIR_CRATER_DOUGH  5/5/480/10
PAIR_ROOT_MILL     6/6/576/12
PAIR_ROOT_DOUGH    5/5/480/10
PAIR_MILL_DOUGH    5/5/480/10
```

For each pair, show:

```text
pair id
biome A
biome B
orientation H/V
transition A->B and B->A
profile
route requirement
edge signature
candidate count
microchunk count
tile row count
socket row count
coverage state
issue count
```

### Candidate preview

For a selected pair/profile/orientation/candidate, show a deterministic 12x8 preview model with:

```text
foreground tile layer
background evidence
route/socket markers
warning markers
boundary layer markers
disabled or invalid reason labels
transform direction
mirror state
source microchunk id
source catalog row id
```

Disabled/invalid candidates must remain visible in the list in a non-selecting or greyed state with an explicit reason. Missing CSV evidence or coverage failure must produce a stable issue view instead of throwing.

### Overlay toggles

At minimum provide deterministic toggles for:

```text
Foreground
Background
Route
Sockets
Warnings
BoundaryLayer
Issues
```

The toggles affect only preview display. They must not mutate authoring CSV, generated CSV, ScriptableObject cache, scenes, prefabs, project settings, packages, asmdef, or asmref files.

### Window command surface

The window must support:

```text
Open from Unity menu
Refresh coverage report
Select canonical pair
Select orientation
Select profile
Select candidate
Copy stable digest or report summary to clipboard
```

Refresh is read-only. Copy operations must not create project assets.

### Empty and error states

The view model and window must handle:

```text
no report available
report accepted false
missing pair report
missing candidate evidence
invalid selected index
unknown profile
unknown orientation
coverage issue list non-empty
```

All states must be deterministic and testable without Unity Scene objects.

## Required Tests

### Focused MAP08_13 tests - exact 640

```text
MoonpalaceBoundaryPreviewViewModelTests: 420/420
MoonpalaceBoundaryPreviewWindowTests: 220/220
MAP08_13 focused total: 640/640 PASS
```

Required coverage:

```text
builds accepted report summary from MAP08_12 validator
renders exact six pair rows and counts
preserves aggregate digest
filters by pair/profile/orientation deterministically
shows A->B and B->A transition labels
builds 12x8 preview cells for selected candidate
projects foreground/background/route/socket/warning/boundary overlays
keeps disabled/invalid candidates visible with reasons
handles empty report without exception
handles rejected report without exception
handles missing pair/candidate without exception
copies digest/summary without asset writes
does not mutate Authoring CSV
does not create Generated CSV
does not change Runtime coverage acceptance
menu command is registered in Editor assembly
```

### Required regression suite

```text
MAP08 required union: 8380/8380 PASS
MAP07 required regression: 5422/5422 PASS
MAP06 required regression: 2746/2746 PASS
MAP05 required regression: 1959/1959 PASS
Required subset total: 18507/18507 PASS
Required failed/skipped: 0/0
```

The `8380` MAP08 total is `7740` existing required union plus `640` MAP08_13 focused tests.

## Static Gates

```text
New Editor production C#/matching meta: 7/7
New Editor EditMode test C#/matching meta: 2/2
New Runtime production C#/matching meta: 0/0
New Runtime EditMode test C#/matching meta: 0/0
New folder meta allowance: 0..2, exact paths only
Global Assets meta: 3802 -> 3811/3812/3813 according to actual folder-meta creation
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
MAP08_14+/MAP09+ forbidden production symbol hits: 0/0
Unapplied MCP patches: 0
git diff --check errors before Result: 0
```

## Commit Requirement

After all tests and static gates pass, create exactly one atomic implementation commit for MAP08_13. Include implementation details in the commit body.

Commit subject:

```text
MAP08_13: add moonpalace boundary preview window
```

Commit body must include:

```text
- Add Editor-only boundary preview selection, report, view model, and window contracts
- Render MAP08_12 coverage report with six pair rows and candidate counts
- Show transition direction, profile, orientation, overlays, issue states, and disabled reasons
- Preserve Runtime coverage validator behavior and aggregate digest
- Preserve Authoring CSV and Generated CSV counts
- Verify MAP08_13 focused 640/640 and required subset 18507/18507
- Keep MAP08_14_MAP08_EXIT_TESTS locked / do not start
```

Do not include unrelated files in the commit. Preserve any unrelated user worktree changes.

## Result Report Requirements

Create `MapDesign/MCP/REPORTS/MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW_RESULT.md` with:

```text
TASK: MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW
STATUS: PASS|FAIL|BLOCKED
MAP08_13: COMPLETE ELIGIBLE only if PASS
MAP08_14_MAP08_EXIT_TESTS: LOCKED / DO NOT START
```

Result must report:

```text
Patch apply status
MAP08_12 Result SHA-256
Installed MAP08_12 Task SHA-256
Installed MAP08_13 Task SHA-256
Implemented file inventory
Created folder meta inventory
Coverage report accepted/digest/counts
Exact pair matrix displayed
Focused and regression test counts
Compile/Console/relevant warning counts
Authoring manifest before/after
Generated CSV count
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref change counts
Forbidden MAP08_14+/MAP09+ symbol hits
git diff --check result
Atomic commit subject and commit hash
```

## Done Condition

MAP08_13 is complete only when:

```text
Prior MAP08_12 Result SHA gate PASS
Editor-only preview window opens from menu
View model renders all six pair reports from MAP08_12 coverage data
Candidate preview exposes transition direction, overlays, markers, issue state, and disabled reasons
No Authoring CSV or Generated CSV mutation
No Runtime coverage behavior drift
Focused MAP08_13 tests 640/640 PASS
Required subset 18507/18507 PASS
Compile/Console/relevant warnings 0/0/0
Static gates PASS
Atomic commit created with detailed body
Result report created
MAP08_14 remains LOCKED / DO NOT START
```
