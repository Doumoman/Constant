```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP20_02_CREATE_WORLD_OVERLAYS_AND_SECTOR_CANVAS_INSPECTOR
  task_file: TASKS/MAP20_02_CREATE_WORLD_OVERLAYS_AND_SECTOR_CANVAS_INSPECTOR.md
  requires_current_task: NONE
  requires_completed_task: MAP20_01_CREATE_GENERATOR_WINDOW_AND_SCOPED_ROLLBACK
  requires_result:
    path: REPORTS/MAP20_01_CREATE_GENERATOR_WINDOW_AND_SCOPED_ROLLBACK_RESULT.md
    status: PASS
    sha256: 360e759b56d561075d67de37590e9ff6ebe5e8593cdde750d2d5d2546a1606c0
  requires_installed_task:
    path: TASKS/MAP20_01_CREATE_GENERATOR_WINDOW_AND_SCOPED_ROLLBACK.md
    sha256: ee0ee7d11296049995e04e753afc1e79b3ee95b0dbde104661d00deb46686bcb
  requires_handoff:
    name: MAP20_02 handoff digest
    sha256: ce4c0cb859a0b8cbd8b4179b5b2e346fa474b95ae2abbcd0142f34d396c46003
  sets_current_task: MAP20_02_CREATE_WORLD_OVERLAYS_AND_SECTOR_CANVAS_INSPECTOR
```

# MAP20_02 - Create World Overlays and Sector Canvas Inspector

```text
TASK: MAP20_02_CREATE_WORLD_OVERLAYS_AND_SECTOR_CANVAS_INSPECTOR
PHASE: MAP20 - Tooling / Authoring Control Surface
STATUS: CURRENT
NEXT: MAP20_03_CREATE_PATTERN_CLUSTER_SPECIAL_AND_SLICE_INSPECTORS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP20_01의 Generator Window가 만든 run artifact와 MAP09~19의 public planning/validation surfaces를 읽어서, 사람이 world와 sector를 검사할 수 있는 **읽기 전용 overlay/inspector**를 추가한다.

이번 Task는 **시각화와 검사 모델**만 소유한다.  
생성기 실행, rollback 실행, CSV 점프, pattern/slice 상세 inspector, runtime HUD/export는 다음 작업이 소유한다.

이번 Task의 책임:

```text
1. World overlay layer snapshot을 정의한다.
2. Site / Biome / Route / Boundary / Pacing / Cluster / Activity / Special / Population / Validation layer를 toggle 가능한 읽기 전용 모델로 묶는다.
3. 13x13 world sector grid와 selected sector summary를 표시한다.
4. 48x32 sector canvas inspector에서 Owner / Spine / Envelope / Density layer를 검사한다.
5. 선택 sector와 선택 cell의 source owner, provenance, density, route/envelope 여부, validation marker를 보여준다.
6. Overlay/sector inspection sample artifact와 deterministic digest를 만든다.
7. Result 첫머리에 추가 script와 각 책임을 사용자용 표로 보고한다.
```

아직 하지 않는 일:

```text
4x4 pattern candidate/rejection inspector
TerrainCluster footprint/path/slot detailed inspector
SpecialRegion footprint/site binding detailed inspector
12x8 slice/socket/provenance inspector
CSV row/column navigation
failure browser
runtime HUD
seed bundle export
actual Tilemap rendering
generator solve or reroll
rollback execution
```

## 1. 사용자 보고 의무

Result 첫 부분에는 반드시 아래 두 섹션을 둔다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

`User-Facing Implementation Report`에는 아래 질문에 답한다.

```text
이번 작업이 어떤 overlay/inspector를 추가했는가?
World overlay에서 볼 수 있는 layer는 무엇인가?
48x32 sector canvas에서 볼 수 있는 layer는 무엇인가?
선택 sector/cell에서 어떤 정보를 확인할 수 있는가?
어떤 정보는 MAP20_03/04/05로 미뤘는가?
이 도구가 읽기 전용이라는 증거는 무엇인가?
generator/rollback/MAP19 scale audit을 실행하지 않았다는 증거는 무엇인가?
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
MapDesign/MCP/REPORTS/MAP20_01_CREATE_GENERATOR_WINDOW_AND_SCOPED_ROLLBACK_RESULT.md exists
MAP20_01 Result STATUS: PASS
MAP20_01 Result SHA-256:
360e759b56d561075d67de37590e9ff6ebe5e8593cdde750d2d5d2546a1606c0

MapDesign/MCP/TASKS/MAP20_01_CREATE_GENERATOR_WINDOW_AND_SCOPED_ROLLBACK.md exists
MAP20_01 installed Task SHA-256:
ee0ee7d11296049995e04e753afc1e79b3ee95b0dbde104661d00deb46686bcb

MAP20_02 handoff digest:
ce4c0cb859a0b8cbd8b4179b5b2e346fa474b95ae2abbcd0142f34d396c46003

MAP20_03_CREATE_PATTERN_CLUSTER_SPECIAL_AND_SLICE_INSPECTORS: LOCKED
```

불일치 시:

```text
STATUS: BLOCKED
reason: precondition mismatch
created/changed code files: 0
MAP20_03 started: NO
STOP
```

## 3. 허용 범위

허용:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Tooling/GeneratedWorldOverlaySnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Tooling/GeneratedSectorCanvasInspection.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Tooling/GeneratedWorldOverlayInspectorWindow.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Tooling/GeneratedWorldOverlaySamplePublisher.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Tooling/GeneratedWorldOverlayInspectorTests.cs
MapDesign/MCP/GENERATED/MAP20_02/**
MapDesign/MCP/TASKS/MAP20_02_CREATE_WORLD_OVERLAYS_AND_SECTOR_CANVAS_INSPECTOR.md
MapDesign/MCP/REPORTS/MAP20_02_CREATE_WORLD_OVERLAYS_AND_SECTOR_CANVAS_INSPECTOR_RESULT.md
MapDesign/MCP_ARCHIVE/MAP20_02_CREATE_WORLD_OVERLAYS_AND_SECTOR_CANVAS_INSPECTOR.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

조건부 허용:

```text
MAP20_01 Generator Window may link/open the MAP20_02 inspector, but must not gain new generation behavior.
Existing MAP20_01 run artifact model may be read or referenced, not rewritten.
Existing MAP09~19 public constants/results may be consumed read-only.
Generated sample JSON may be written only under MapDesign/MCP/GENERATED/MAP20_02.
```

금지:

```text
generator solve or reroll
pattern renderer behavior change
sector planner behavior change
cluster/activity/event/special/population placement behavior change
MAP19 validation proof rewrite
MAP19_09 scale audit rerun
rollback execution or rollback behavior change
CSV authoring table mutation
CSV row/column navigation implementation
Pattern/Cluster/Special/Slice detailed inspector implementation
runtime HUD
failure browser
seed bundle export
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
automatic MAP20_03 start
broad refactor or optimization rewrite
```

## 4. World Overlay Layer 계약

World overlay layer tokens are exact:

```text
Site
Biome
Route
Boundary
Pacing
Cluster
Activity
Special
Population
Validation
```

Layer responsibilities:

| Layer | Shows | Must not do |
|---|---|---|
| `Site` | fixed/optional site reservation markers and selected site labels | edit reservations |
| `Biome` | sector biome/category color and biome-pair boundary hints | recalculate biome generation |
| `Route` | mandatory/high/recovery route markers and pass/fail state | recompute traversal |
| `Boundary` | intersector socket/boundary pair labels | repair sockets |
| `Pacing` | pacing role, density band, revisit/distance summary | change pacing assignment |
| `Cluster` | cluster id/role/footprint summary per sector | render or reroll clusters |
| `Activity` | activity shell/slot/event overlay summary | spawn activity runtime |
| `Special` | SpecialRegion site/entry/fixed-shell summary | edit special region |
| `Population` | mandatory/unique/content/hazard budget summary | spawn gameplay objects |
| `Validation` | MAP19 pass/fail/owner markers and failure-bundle links | run validation |

Required:

```text
Layer list is centralized in one production catalog.
Each layer has a stable display token, short label, owner category, and enabled flag.
World grid size is exactly 13x13 / 169 sectors.
Overlay snapshot is read-only after publication.
Missing upstream layer facts become explicit MissingData markers, not invented data.
```

## 5. Sector Canvas Inspector 계약

Sector canvas size:

```text
width: 48
height: 32
cell count: 1536
```

Sector canvas layer tokens are exact:

```text
Owner
Spine
Envelope
Density
```

Layer responsibilities:

| Layer | Shows | Must not do |
|---|---|---|
| `Owner` | final owner/source/provenance per cell | change winner |
| `Spine` | route spine cells and movement kind labels | recompute route graph |
| `Envelope` | protected traversal envelope and quiet/buffer relation | carve/repair terrain |
| `Density` | solid/open/owned/unowned density state and local warnings | clean up cells |

Cell selection must expose:

```text
sector coordinate
local cell coordinate
world cell coordinate if available
owner layer
source owner
provenance id
spine marker
envelope marker
density marker
validation marker
missing-data marker
```

Required:

```text
All cell coordinates are in 0..47 and 0..31.
The inspector can render/select a 48x32 grid without creating Tilemap objects.
The inspector must tolerate MissingData cells and display them distinctly.
```

## 6. Window UI 최소 요구

The EditorWindow may use IMGUI or UI Toolkit following existing project conventions.

Required controls:

```text
Run artifact path or current sample selector
World overlay layer toggles
13x13 sector grid
Selected sector coordinate display
Selected sector summary panel
48x32 sector canvas grid
Sector canvas layer toggles
Selected cell detail panel
Validation marker list
Open generated MAP20_02 output folder button
Copy overlay digest button
```

Required safety states:

```text
Opening the window does not run generation.
Layer toggles change only view state.
Selecting sector/cell changes only selection state.
No Run/Rollback/Generate button is added in this Task.
If data is missing, the window displays MissingData instead of triggering generation.
```

## 7. Snapshot / Digest 계약

Required sample artifacts:

```text
MapDesign/MCP/GENERATED/MAP20_02/world_overlay_snapshot.json
MapDesign/MCP/GENERATED/MAP20_02/sector_canvas_inspection_sample.json
MapDesign/MCP/GENERATED/MAP20_02/overlay_digest_manifest.json
```

World overlay snapshot required fields:

```text
schema_version
task_id
world_width_sectors
world_height_sectors
sector_count
source_run_artifact_digest
MAP20_01_handoff_digest
enabled_layer_tokens
sector_records
missing_data_records
validation_records
canonical_digest
created_utc_excluded_from_canonical_digest
```

Sector canvas inspection required fields:

```text
schema_version
task_id
sector_coordinate
sector_width
sector_height
cell_count
enabled_canvas_layer_tokens
selected_cell_record
cell_records
missing_data_records
canonical_digest
created_utc_excluded_from_canonical_digest
```

Digest rules:

```text
created_utc may be stored but must be excluded from canonical digest.
Record order must be deterministic.
Layer token order must be the centralized catalog order.
Digest must be lowercase SHA-256.
Repeat/culture/input-order changes must not alter canonical digest.
```

## 8. 중복과 하드코딩 방지

필수:

```text
Do not duplicate world overlay layer tokens across multiple production catalogs.
Do not duplicate sector canvas layer tokens across multiple production catalogs.
Do not duplicate 13x13, 169, 48x32, or 1536 constants when public constants exist.
If a public constant does not exist, define a named tooling constant once and report it.
Do not duplicate generator, validation, rollback, or CSV navigation logic.
Window owns view/input state only.
Runtime snapshot models own deterministic serialization.
Editor publisher owns sample artifact file writes only.
```

Result에 아래를 기록한다.

```text
duplicated generator logic count:
duplicated validation logic count:
duplicated rollback logic count:
duplicated layer catalog count:
hard-coded dimension copies outside named constants:
hard-coded digest string copies outside precondition/constants:
```

목표값:

```text
duplicated generator logic count: 0
duplicated validation logic count: 0
duplicated rollback logic count: 0
duplicated layer catalog count: 0
hard-coded dimension copies outside named constants: 0
hard-coded digest string copies outside precondition/constants: 0
```

## 9. Focused Tests

Focused test category:

```text
MAP20_02
```

Required test names:

```text
WorldOverlayCatalogContainsExactTenReadOnlyLayers
WorldOverlaySnapshotUsesThirteenByThirteenAndOneHundredSixtyNineSectors
SectorCanvasInspectorUsesFortyEightByThirtyTwoAndFifteenThirtySixCells
SectorCanvasSelectionReportsOwnerSpineEnvelopeDensityAndMissingData
OverlayWindowOpensWithoutGenerationRollbackValidationOrCsvJump
LayerTogglesChangeOnlyViewStateAndNotSnapshotDigest
OverlaySnapshotsSerializeDeterministicallyAcrossRepeatCultureAndInputOrder
OverlayInspectorDoesNotSelectLegacyRegressionPriorCategoriesPlayModeOrFullRegression
```

Required count:

```text
discovered: 8
executed: 8
passed: 8
failed: 0
```

Focused validation may create sample artifacts only under:

```text
MapDesign/MCP/GENERATED/MAP20_02
```

## 10. 회귀 금지 정책

이번 Task의 검증은 focused MAP20_02 EditMode selection과 compile/console check까지만 허용한다.

문제가 실제로 발생하지 않은 상태에서 더 넓은 검증을 돌리지 않는다.

금지:

```text
MAP19_09 scale audit rerun
MAP20_01 generator run rerun
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
```

## 11. Result 필수 기록

Result 파일:

```text
MapDesign/MCP/REPORTS/MAP20_02_CREATE_WORLD_OVERLAYS_AND_SECTOR_CANVAS_INSPECTOR_RESULT.md
```

Result에는 아래 섹션을 포함한다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
## World Overlay Summary
## Sector Canvas Inspector Summary
## Snapshot and Digest Summary
## Focused Validation Summary
## No Legacy Regression Boundary Notes
## Final Status Evidence
```

`World Overlay Summary`에는 아래 값을 채운다.

```text
MAP20_01 Result SHA-256 required/actual:
MAP20_01 installed Task SHA-256 required/actual:
MAP20_02 handoff digest required/actual:
MAP19 exit digest reused:

window menu path:
window opens without generation:
world grid dimensions:
world sector count:
world overlay layer tokens:
missing-data behavior:
view-only interaction count:

duplicated generator logic count:
duplicated validation logic count:
duplicated rollback logic count:
duplicated layer catalog count:
hard-coded dimension copies outside named constants:
hard-coded digest string copies outside precondition/constants:
```

`Sector Canvas Inspector Summary`에는 아래 값을 채운다.

```text
sector canvas dimensions:
sector canvas cell count:
sector canvas layer tokens:
selected sector evidence:
selected cell evidence fields present/required:
out-of-bounds cell records:
Tilemap objects created:
Scene/Prefab/Tilemap mutation count:
runtime object mutation count:
```

`Snapshot and Digest Summary`에는 아래 값을 채운다.

```text
sample artifacts created:
world overlay snapshot required fields present/required:
sector canvas inspection required fields present/required:
overlay digest manifest created:
world overlay snapshot digest lower-hex SHA-256:
sector canvas inspection digest lower-hex SHA-256:
overlay digest manifest lower-hex SHA-256:
MAP20_03 handoff digest lower-hex SHA-256:
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
```

## 12. PASS 조건

PASS 조건:

```text
MAP20_01 Result and installed Task SHA match
MAP20_02 handoff digest matches
World overlay layer tokens are exactly the required 10 tokens
World grid is exactly 13x13 / 169 sectors
Sector canvas is exactly 48x32 / 1536 cells
Sector canvas layer tokens are exactly Owner / Spine / Envelope / Density
Window opens without generation, rollback, validation, or CSV jump
Layer toggles and selection mutate only view state
Missing upstream facts appear as MissingData markers
Sample snapshot artifacts are deterministic and have lower-hex SHA-256 digests
Focused MAP20_02 EditMode tests are 8/8 PASS
No MAP19_09 scale audit rerun
No MAP20_01 generator run rerun
No legacy regression, prior categories, PlayMode, unfiltered tests, or full regression
No Scene/Prefab/Tilemap/runtime behavior mutation
Responsibility and Added Scripts table is present and specific
MAP20_03 remains LOCKED and not started
```

## 13. FAIL 조건

FAIL 조건:

```text
Window opens and starts generation, rollback, validation, or CSV navigation
World overlay layer list contains extra/missing tokens
Sector canvas layer list contains extra/missing tokens
13x13/169 or 48x32/1536 dimensions are wrong
Missing upstream data is silently invented
Layer toggles alter canonical snapshot digest
Inspector creates Tilemap/GameObject/Scene/Prefab objects
Generator/validation/rollback/CSV logic is duplicated or rewritten
MAP19_09 scale audit is rerun without a real documented trigger
MAP20_01 generator run is rerun without a real documented trigger
Legacy 19347 or full regression is executed
PlayMode is executed
Scene/Prefab/Tilemap/runtime behavior is mutated
MAP20_03 starts
```

## 14. BLOCKED 조건

BLOCKED 조건:

```text
Precondition SHA mismatch
Compile error unrelated to this task that prevents focused validation
Existing public constants for dimensions cannot be accessed and a named tooling constant cannot be safely introduced
Existing MAP20_01 sample run artifact cannot be referenced or replaced by a clearly labeled read-only fixture
EditorWindow cannot open without triggering generation or rollback
```

When BLOCKED:

```text
Do not finalize MAP20_02
Do not start MAP20_03
Do not publish MAP20_03 handoff digest
Record exact owner, invariant, and minimum next action
STOP
```

## 15. Finalize 규칙

PASS일 때만:

```text
MAP20_02_CREATE_WORLD_OVERLAYS_AND_SECTOR_CANVAS_INSPECTOR: COMPLETE
MAP20_03_CREATE_PATTERN_CLUSTER_SPECIAL_AND_SLICE_INSPECTORS: LOCKED
```

FAIL 또는 BLOCKED일 때:

```text
MAP20_02 remains CURRENT
MAP20_03 remains LOCKED
STOP
```

다음 Task는 내가 Result를 검수한 뒤 별도 MD로 준다.

## 16. Commit 규칙

PASS일 때만 atomic commit을 만든다.

Commit subject:

```text
MAP20_02 create world overlay inspector
```

Commit body에는 다음을 포함한다.

```text
MAP20_02 responsibilities
focused test count
world overlay layer tokens
sector canvas layer tokens
snapshot digests
MAP20_03 handoff digest
legacy regression counters all zero
```

Git push는 하지 않는다.

## 17. STOP

Result 작성, 상태 finalize, commit 이후 반드시 멈춘다.

```text
DO NOT START MAP20_03.
DO NOT CREATE MAP20_03 FILES.
DO NOT RERUN MAP19_09 SCALE AUDIT.
DO NOT RERUN MAP20_01 GENERATOR RUN.
DO NOT RUN LEGACY REGRESSION.
STOP.
```
