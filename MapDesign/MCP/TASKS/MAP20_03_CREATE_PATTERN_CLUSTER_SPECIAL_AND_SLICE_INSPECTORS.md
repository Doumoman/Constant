```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP20_03_CREATE_PATTERN_CLUSTER_SPECIAL_AND_SLICE_INSPECTORS
  task_file: TASKS/MAP20_03_CREATE_PATTERN_CLUSTER_SPECIAL_AND_SLICE_INSPECTORS.md
  requires_current_task: NONE
  requires_completed_task: MAP20_02_CREATE_WORLD_OVERLAYS_AND_SECTOR_CANVAS_INSPECTOR
  requires_result:
    path: REPORTS/MAP20_02_CREATE_WORLD_OVERLAYS_AND_SECTOR_CANVAS_INSPECTOR_RESULT.md
    status: PASS
    sha256: a98df52c4c48ca1d138d90bef1999dacb11107e1f4266292458d80937cfbf2ff
  requires_installed_task:
    path: TASKS/MAP20_02_CREATE_WORLD_OVERLAYS_AND_SECTOR_CANVAS_INSPECTOR.md
    sha256: 6be531ec7d28dc2a4d61c464e2ba79060b359f2f031946825c1a274caef253ed
  requires_handoff:
    name: MAP20_03 handoff digest
    sha256: 684a3bf92737aa5073210304b0e3e9d8a8c3857cde45716496df771fe56a835a
  sets_current_task: MAP20_03_CREATE_PATTERN_CLUSTER_SPECIAL_AND_SLICE_INSPECTORS
```

# MAP20_03 - Create Pattern Cluster Special and Slice Inspectors

```text
TASK: MAP20_03_CREATE_PATTERN_CLUSTER_SPECIAL_AND_SLICE_INSPECTORS
PHASE: MAP20 - Tooling / Authoring Control Surface
STATUS: CURRENT
NEXT: MAP20_04_IMPLEMENT_CSV_NAVIGATION_AND_VALIDATION_JUMP
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP20_02의 world overlay / 48x32 sector canvas inspector에서 선택한 sector를 더 깊게 들여다보는 **읽기 전용 상세 inspector 4종**을 추가한다.

이번 Task는 **상세 조회 모델과 Editor 표시**만 소유한다.  
CSV navigation, validation jump, failure browser, runtime HUD, seed bundle export, 생성/rollback 실행은 다음 작업이 소유한다.

이번 Task의 책임:

```text
1. 4x4 MicroPattern candidate/rejection inspector를 만든다.
2. TerrainCluster footprint/path/slot/site binding inspector를 만든다.
3. SpecialRegion footprint/site binding inspector를 만든다.
4. 12x8 Generated Slice cell/socket/provenance inspector를 만든다.
5. MAP20_02의 선택 sector/cell context를 읽어 상세 tab selection으로 연결한다.
6. MissingData를 명시적으로 표시하고 없는 upstream data를 생성하거나 추정하지 않는다.
7. 상세 inspector sample artifact와 deterministic digest를 만든다.
8. Result 첫머리에 추가 script와 각 책임을 사용자용 표로 보고한다.
```

아직 하지 않는 일:

```text
CSV row/column navigation
validation error id jump
failure browser
runtime HUD
seed bundle export
actual Tilemap rendering
generator solve or reroll
rollback execution
authoring CSV mutation
Pattern/Cluster/Special/Slice data editing
```

## 1. 사용자 보고 의무

Result 첫 부분에는 반드시 아래 두 섹션을 둔다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

`User-Facing Implementation Report`에는 아래 질문에 답한다.

```text
이번 작업이 어떤 상세 inspector를 추가했는가?
Pattern inspector에서 어떤 후보/거절 정보를 볼 수 있는가?
Cluster inspector에서 어떤 footprint/path/slot/site 정보를 볼 수 있는가?
Special inspector에서 어떤 footprint/site/entry/fixed-shell 정보를 볼 수 있는가?
Slice inspector에서 어떤 12x8 cell/socket/provenance 정보를 볼 수 있는가?
MissingData는 어떻게 표시되는가?
어떤 기능을 MAP20_04/05로 미뤘는가?
이 작업이 읽기 전용이라는 증거는 무엇인가?
legacy regression을 돌리지 않았다는 증거는 무엇인가?
```

`Responsibility and Added Scripts`는 표로 작성한다.

```text
| Script or file | Added or changed responsibility | Explicit non-ownership |
```

파일별 책임이 없으면 FAIL이다.

## 2. 선행조건

작업 시작 전에 다음을 확인한다.

```text
MapDesign/MCP/REPORTS/MAP20_02_CREATE_WORLD_OVERLAYS_AND_SECTOR_CANVAS_INSPECTOR_RESULT.md exists
MAP20_02 Result STATUS: PASS
MAP20_02 Result SHA-256:
a98df52c4c48ca1d138d90bef1999dacb11107e1f4266292458d80937cfbf2ff

MapDesign/MCP/TASKS/MAP20_02_CREATE_WORLD_OVERLAYS_AND_SECTOR_CANVAS_INSPECTOR.md exists
MAP20_02 installed Task SHA-256:
6be531ec7d28dc2a4d61c464e2ba79060b359f2f031946825c1a274caef253ed

MAP20_03 handoff digest:
684a3bf92737aa5073210304b0e3e9d8a8c3857cde45716496df771fe56a835a

MAP20_04_IMPLEMENT_CSV_NAVIGATION_AND_VALIDATION_JUMP: LOCKED
```

불일치 시:

```text
STATUS: BLOCKED
reason: precondition mismatch
created/changed code files: 0
MAP20_04 started: NO
STOP
```

## 3. 허용 범위

허용:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Tooling/GeneratedDetailInspectionSnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Tooling/GeneratedPatternClusterSpecialSliceInspection.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Tooling/GeneratedDetailInspectorWindow.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Tooling/GeneratedDetailInspectorSamplePublisher.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Tooling/GeneratedDetailInspectorTests.cs
MapDesign/MCP/GENERATED/MAP20_03/**
MapDesign/MCP/TASKS/MAP20_03_CREATE_PATTERN_CLUSTER_SPECIAL_AND_SLICE_INSPECTORS.md
MapDesign/MCP/REPORTS/MAP20_03_CREATE_PATTERN_CLUSTER_SPECIAL_AND_SLICE_INSPECTORS_RESULT.md
MapDesign/MCP_ARCHIVE/MAP20_03_CREATE_PATTERN_CLUSTER_SPECIAL_AND_SLICE_INSPECTORS.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

조건부 허용:

```text
MAP20_02 inspector window may link/open the MAP20_03 detail inspector, but must not gain generation, rollback, validation, or CSV navigation behavior.
Existing MAP20_02 snapshot models may be read or referenced, not rewritten.
Existing MAP10/MAP11/MAP13/MAP16/MAP17 public surfaces may be consumed read-only.
Generated sample JSON may be written only under MapDesign/MCP/GENERATED/MAP20_03.
```

금지:

```text
CSV row/column navigation
validation error id jump
failure browser
runtime HUD
seed bundle export
generator solve or reroll
pattern renderer behavior change
cluster placement behavior change
special-region placement behavior change
slice partition/bake behavior change
rollback execution or rollback behavior change
MAP19_09 scale audit rerun
MAP20_01 generator run rerun
MAP20_02 overlay sample regeneration except read-only input reference
authoring CSV mutation
generated map content mutation outside MAP20_03 sample JSON
legacy 19347 regression
prior task category selection
PlayMode selection
unfiltered test run
full regression run
manual gameplay test
Scene / Prefab / Tilemap authoring mutation
Tilemap.SetTile / SetTiles / SetTilesBlock / ClearAllTiles
runtime GameObject instantiate/enable/disable/destroy
real screenshot capture from camera
production seed approval
automatic MAP20_04 start
broad refactor or optimization rewrite
```

## 4. Detail Tab 계약

Detail inspector tab tokens are exact:

```text
Pattern
Cluster
Special
Slice
```

Tab responsibilities:

| Tab | Shows | Must not do |
|---|---|---|
| `Pattern` | 4x4 candidate id, biome/profile, transform, add/carve summary, protected-mask relation, rejection reason | reroll candidate or change pattern render |
| `Cluster` | TerrainCluster id, role, footprint bounds, route/path summary, socket/slot/site binding summary | recompute placement or path |
| `Special` | SpecialRegion id, site kind, footprint, entry/return, fixed shell, facility/reward markers | edit reservation or spawn content |
| `Slice` | 12x8 slice index, local/world cell projection, socket bands, marker slots, provenance | rebuild slice or bake Tilemap |

Required:

```text
Tab list is centralized in one production catalog.
Every tab has a stable token, short label, selected-record count, missing-data count, and canonical digest contribution.
Missing upstream facts become explicit MissingData records.
The same selected sector/cell can be inspected across all four tabs without changing generated data.
```

## 5. Pattern Inspector 계약

Pattern inspector required facts:

```text
sector coordinate
pattern zone coordinate
pattern local 4x4 coordinate
pattern id
biome/profile id
candidate ordinal
transform token
accepted/rejected state
rejection reason
add solid count
carve air count
protected mask overlap count
affected cell count
source digest or MissingData
```

Required:

```text
4x4 dimension must be exact.
Accepted and rejected candidates are represented by the same data shape.
The inspector must not call candidate RNG or renderer.
```

## 6. Cluster Inspector 계약

Cluster inspector required facts:

```text
sector coordinate
cluster id
cluster role
spine variant id
footprint bounds
footprint cell count
route/path node count
route/path edge count
socket binding count
activity slot binding count
special site binding count
owner/provenance digest or MissingData
```

Required:

```text
Footprint/path/slot/site values are read-only projections.
Cluster inspector must not recompute placement, route path, or slot assignment.
```

## 7. Special Inspector 계약

Special inspector required facts:

```text
sector coordinate
special region id
site kind
region category
footprint bounds
footprint cell count
entry marker count
return marker count
fixed shell marker count
facility marker count
required reward marker count
optional marker count
site binding digest or MissingData
```

Required:

```text
Special inspector must distinguish absent special-region data from empty valid data.
It must not edit SpecialRegion reservations or fixed shell definitions.
```

## 8. Slice Inspector 계약

Slice inspector required facts:

```text
sector coordinate
slice index
chunk coordinate
slice dimensions
slice cell count
cell local coordinate
sector-local coordinate
world coordinate if available
socket band records
marker slot records
provenance records
owner/provenance digest or MissingData
```

Required:

```text
Slice index range is exactly 0..15.
Slice dimensions are exactly 12x8.
Slice cell count is exactly 96.
The inspector must show socket/provenance records without rebuilding the slice.
```

## 9. Window UI 최소 요구

The EditorWindow may use IMGUI or UI Toolkit following existing project conventions.

Required controls:

```text
Read-only source selector or current MAP20_02 sample selector
Selected sector/cell context display
Detail tab selector: Pattern / Cluster / Special / Slice
Record list for the selected tab
Selected record detail panel
MissingData count display
Digest display
Open generated MAP20_03 output folder button
Copy detail digest button
```

Required safety states:

```text
Opening the window does not run generation.
Changing tab changes only view state.
Selecting record changes only view state.
No Run/Rollback/Generate/CSV Jump/Validate button is added in this Task.
If source data is missing, the window displays MissingData instead of triggering generation.
```

## 10. Snapshot / Digest 계약

Required sample artifacts:

```text
MapDesign/MCP/GENERATED/MAP20_03/detail_inspection_snapshot.json
MapDesign/MCP/GENERATED/MAP20_03/pattern_cluster_special_slice_sample.json
MapDesign/MCP/GENERATED/MAP20_03/detail_digest_manifest.json
```

Detail inspection snapshot required fields:

```text
schema_version
task_id
source_MAP20_02_world_overlay_digest
source_MAP20_02_sector_canvas_digest
MAP20_03_handoff_digest
selected_sector_coordinate
selected_cell_coordinate
detail_tab_tokens
tab_summaries
missing_data_records
validation_records
canonical_digest
created_utc_excluded_from_canonical_digest
```

Pattern/Cluster/Special/Slice sample required fields:

```text
schema_version
task_id
pattern_records
cluster_records
special_records
slice_records
missing_data_records
canonical_digest
created_utc_excluded_from_canonical_digest
```

Digest manifest required fields:

```text
schema_version
task_id
detail_snapshot_digest
combined_detail_sample_digest
MAP20_02_handoff_digest
MAP20_04_handoff_digest
canonical_digest
created_utc_excluded_from_canonical_digest
```

Digest rules:

```text
created_utc may be stored but must be excluded from canonical digest.
Record order must be deterministic.
Tab token order must be Pattern / Cluster / Special / Slice.
Digest must be lowercase SHA-256.
Repeat/culture/input-order changes must not alter canonical digest.
```

## 11. 중복과 하드코딩 방지

필수:

```text
Do not duplicate detail tab tokens across multiple production catalogs.
Do not duplicate 4x4, 12x8, 96, 16, 48x32, or 1536 constants when public constants exist.
If a public constant does not exist, define a named tooling constant once and report it.
Do not duplicate generator, pattern renderer, cluster placement, special placement, slice builder, validation, rollback, or CSV navigation logic.
Window owns view/input state only.
Runtime snapshot models own deterministic serialization.
Editor publisher owns sample artifact file writes only.
```

Result에 아래를 기록한다.

```text
duplicated generator logic count:
duplicated pattern renderer logic count:
duplicated cluster placement logic count:
duplicated special placement logic count:
duplicated slice builder logic count:
duplicated validation logic count:
duplicated rollback logic count:
duplicated CSV navigation logic count:
duplicated detail tab catalog count:
hard-coded dimension copies outside named constants:
hard-coded digest string copies outside precondition/constants:
```

목표값:

```text
duplicated generator logic count: 0
duplicated pattern renderer logic count: 0
duplicated cluster placement logic count: 0
duplicated special placement logic count: 0
duplicated slice builder logic count: 0
duplicated validation logic count: 0
duplicated rollback logic count: 0
duplicated CSV navigation logic count: 0
duplicated detail tab catalog count: 0
hard-coded dimension copies outside named constants: 0
hard-coded digest string copies outside precondition/constants: 0
```

## 12. Focused Tests

Focused test category:

```text
MAP20_03
```

Required test names:

```text
DetailInspectorCatalogContainsExactFourReadOnlyTabs
PatternInspectorReportsFourByFourCandidateAcceptanceRejectionAndMissingData
ClusterInspectorReportsFootprintPathSlotAndSiteBindingWithoutRecomputing
SpecialInspectorDistinguishesAbsentSpecialFromEmptyValidSpecial
SliceInspectorReportsSixteenTwelveByEightSlicesCellsSocketsAndProvenance
DetailInspectorWindowOpensWithoutGenerationRollbackValidationCsvJumpOrExport
DetailTabAndRecordSelectionChangeOnlyViewState
DetailSnapshotsSerializeDeterministicallyAcrossRepeatCultureAndInputOrder
DetailInspectorReusesMap20_02ContextAndPublishesMap20_04HandoffOnlyOnPass
DetailInspectorDoesNotSelectLegacyRegressionPriorCategoriesPlayModeOrFullRegression
```

Required count:

```text
discovered: 10
executed: 10
passed: 10
failed: 0
```

Focused validation may create sample artifacts only under:

```text
MapDesign/MCP/GENERATED/MAP20_03
```

## 13. 회귀 금지 정책

이번 Task의 검증은 focused MAP20_03 EditMode selection과 compile/console check까지만 허용한다.

문제가 실제로 발생하지 않은 상태에서 더 넓은 검증을 돌리지 않는다.

금지:

```text
MAP19_09 scale audit rerun
MAP20_01 generator run rerun
MAP20_02 overlay sample regeneration except read-only input reference
legacy 19347 regression
prior task category selection
PlayMode selection
unfiltered test run
full regression run
```

실제 문제가 발생해 더 넓은 검증이 필요하다고 판단되면 조용히 실행하지 않는다.  
Result에 trigger owner, broken invariant, focused proof insufficiency, requested wider verification을 기록하고 STOP한다.

문제가 없다면 Result에 반드시 기록한다.

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
FULL REGRESSION RUNS: 0
MAP19_09 SCALE AUDIT RERUNS: 0
MAP20_01 GENERATOR RUN RERUNS: 0
MAP20_02 OVERLAY SAMPLE REGENERATION RUNS: 0
```

## 14. Result 필수 기록

Result 파일:

```text
MapDesign/MCP/REPORTS/MAP20_03_CREATE_PATTERN_CLUSTER_SPECIAL_AND_SLICE_INSPECTORS_RESULT.md
```

Result에는 아래 섹션을 포함한다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
## Detail Inspector Summary
## Pattern Cluster Special Slice Summary
## Snapshot and Digest Summary
## Focused Validation Summary
## No Legacy Regression Boundary Notes
## Final Status Evidence
```

`Detail Inspector Summary`에는 아래 값을 채운다.

```text
MAP20_02 Result SHA-256 required/actual:
MAP20_02 installed Task SHA-256 required/actual:
MAP20_03 handoff digest required/actual:
MAP20_02 world overlay digest reused:
MAP20_02 sector canvas digest reused:

window menu path:
window opens without generation:
detail tab tokens:
selected sector/cell context:
view-only interaction count:
missing-data behavior:

duplicated generator logic count:
duplicated pattern renderer logic count:
duplicated cluster placement logic count:
duplicated special placement logic count:
duplicated slice builder logic count:
duplicated validation logic count:
duplicated rollback logic count:
duplicated CSV navigation logic count:
duplicated detail tab catalog count:
hard-coded dimension copies outside named constants:
hard-coded digest string copies outside precondition/constants:
```

`Pattern Cluster Special Slice Summary`에는 아래 값을 채운다.

```text
pattern dimension:
pattern required fields present/required:
pattern accepted records:
pattern rejected records:
pattern MissingData records:

cluster required fields present/required:
cluster footprint/path/slot/site binding records:
cluster MissingData records:

special required fields present/required:
special absent-vs-empty distinction:
special MissingData records:

slice dimensions:
slice count:
slice cell count per slice:
slice socket/provenance fields present/required:
slice MissingData records:

Tilemap objects created:
Scene/Prefab/Tilemap mutation count:
runtime object mutation count:
```

`Snapshot and Digest Summary`에는 아래 값을 채운다.

```text
sample artifacts created:
detail snapshot required fields present/required:
combined detail sample required fields present/required:
detail digest manifest required fields present/required:
detail inspection snapshot digest lower-hex SHA-256:
combined detail sample digest lower-hex SHA-256:
detail digest manifest lower-hex SHA-256:
MAP20_04 handoff digest lower-hex SHA-256:
```

`Focused Validation Summary`에는 아래 값을 채운다.

```text
Unity Version:
Compile Errors:
Relevant Warnings:
Relevant Console Errors:
EditMode category:
Discovered:
Executed:
Passed:
Failed:
Skipped:
Inconclusive:
PlayMode Tests:
Scene/Prefab Changes:
```

`No Legacy Regression Boundary Notes`에는 반드시 아래 블록을 포함한다.

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
FULL REGRESSION RUNS: 0
MAP19_09 SCALE AUDIT RERUNS: 0
MAP20_01 GENERATOR RUN RERUNS: 0
MAP20_02 OVERLAY SAMPLE REGENERATION RUNS: 0
```

## 15. PASS 조건

PASS 조건:

```text
MAP20_02 Result and installed Task SHA match
MAP20_03 handoff digest matches
Detail tab tokens are exactly Pattern / Cluster / Special / Slice
Pattern inspector reports 4x4 candidate/rejection data or explicit MissingData
Cluster inspector reports footprint/path/slot/site binding data or explicit MissingData
Special inspector distinguishes absent special data from empty valid special data
Slice inspector reports exact 16 slices, 12x8 dimensions, 96 cells, socket/provenance fields or explicit MissingData
Window opens without generation, rollback, validation, CSV jump, or export
Tab and record selection mutate only view state
Sample snapshot artifacts are deterministic and have lower-hex SHA-256 digests
Focused MAP20_03 EditMode tests are 10/10 PASS
No MAP19_09 scale audit rerun
No MAP20_01 generator run rerun
No MAP20_02 overlay sample regeneration run
No legacy regression, prior categories, PlayMode, unfiltered tests, or full regression
No Scene/Prefab/Tilemap/runtime behavior mutation
Responsibility and Added Scripts table is present and specific
MAP20_04 remains LOCKED and not started
```

## 16. FAIL 조건

FAIL 조건:

```text
Window opens and starts generation, rollback, validation, CSV navigation, or export
Detail tab list contains extra/missing tokens
4x4 or 12x8/96/16 dimensions are wrong
Missing upstream data is silently invented
Absent SpecialRegion data is treated as valid empty special data
Tab or record selection alters canonical snapshot digest
Inspector creates Tilemap/GameObject/Scene/Prefab objects
Generator/pattern/cluster/special/slice/validation/rollback/CSV logic is duplicated or rewritten
MAP19_09 scale audit is rerun without a real documented trigger
MAP20_01 generator run is rerun without a real documented trigger
MAP20_02 overlay sample is regenerated without a real documented trigger
Legacy 19347 or full regression is executed
PlayMode is executed
Scene/Prefab/Tilemap/runtime behavior is mutated
MAP20_04 starts
```

## 17. BLOCKED 조건

BLOCKED 조건:

```text
Precondition SHA mismatch
Compile error unrelated to this task that prevents focused validation
Existing public constants for dimensions cannot be accessed and a named tooling constant cannot be safely introduced
Existing MAP20_02 sample artifacts cannot be referenced or replaced by clearly labeled read-only fixtures
EditorWindow cannot open without triggering generation, rollback, validation, CSV navigation, or export
```

When BLOCKED:

```text
Do not finalize MAP20_03
Do not start MAP20_04
Do not publish MAP20_04 handoff digest
Record exact owner, invariant, and minimum next action
STOP
```

## 18. Finalize 규칙

PASS일 때만:

```text
MAP20_03_CREATE_PATTERN_CLUSTER_SPECIAL_AND_SLICE_INSPECTORS: COMPLETE
MAP20_04_IMPLEMENT_CSV_NAVIGATION_AND_VALIDATION_JUMP: LOCKED
```

FAIL 또는 BLOCKED일 때:

```text
MAP20_03 remains CURRENT
MAP20_04 remains LOCKED
STOP
```

다음 Task는 내가 Result를 검수한 뒤 별도 MD로 준다.

## 19. Commit 규칙

PASS일 때만 atomic commit을 만든다.

Commit subject:

```text
MAP20_03 create detail inspectors
```

Commit body에는 다음을 포함한다.

```text
MAP20_03 responsibilities
focused test count
detail tab tokens
pattern/cluster/special/slice summary
snapshot digests
MAP20_04 handoff digest
legacy regression counters all zero
```

Git push는 하지 않는다.

## 20. STOP

Result 작성, 상태 finalize, commit 이후 반드시 멈춘다.

```text
DO NOT START MAP20_04.
DO NOT CREATE MAP20_04 FILES.
DO NOT RERUN MAP19_09 SCALE AUDIT.
DO NOT RERUN MAP20_01 GENERATOR RUN.
DO NOT REGENERATE MAP20_02 OVERLAY SAMPLE.
DO NOT RUN LEGACY REGRESSION.
STOP.
```
