# MAP07_08 - Create Microchunk Authoring Grid

```yaml
status_control:
  task_key: MAP07_08_CREATE_MICROCHUNK_AUTHORING_GRID
  result_file: REPORTS/MAP07_08_CREATE_MICROCHUNK_AUTHORING_GRID_RESULT.md
```

## TASK TYPE

```text
EDITOR-ONLY MICROCHUNK 12x8 AUTHORING GRID + 8-LAYER PAINTING STATE + PHASE-BOUNDARY TEST ADVANCE
```

## Objective

MAP07_07 PASS/finalize 뒤 microchunk 제작용 Editor-only 12x8 고정 grid와 8개 tile layer painting state/view model/window를 구현한다. 이 Task는 디자이너가 한 microchunk의 96개 local cell에 layer별 tile code를 칠하고, 그 상태를 기존 runtime model의 `MicrochunkTileCell`/`MicrochunkDefinition` projection으로 확인하는 범위까지만 연다.

Socket/slot editor, CSV import/export, transform preview, reachability heatmap/report, starter catalog round-trip, sector assembly, world traversal, generated CSV writer는 구현하지 않는다. MAP07_09 이후 Task body는 읽거나 시작하지 않는다.

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
12. `REPORTS/MAP07_07_IMPLEMENT_MICROCHUNK_REACHABILITY_PROBE_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP07_07_IMPLEMENT_MICROCHUNK_REACHABILITY_PROBE
STATUS: PASS
MAP07_07: COMPLETE ELIGIBLE
MAP07_08_CREATE_MICROCHUNK_AUTHORING_GRID: LOCKED / DO NOT START
SHA-256: afaf3f058c34457d26491b15c06858ba1c1c7355cf14d5902d65f66a43a1fa19
```

이 별도 patch가 적용된 뒤에만 MAP07_08을 실행한다. MAP07_09 이후 Task body는 읽거나 시작하지 않는다.

## Approved Input Baseline

```text
Unity: 6000.3.8f1
Runtime assembly: Game.Map.Runtime
Runtime namespace boundary: StarNight.Map.WorldGeneration.*
Editor assembly: MapAuthoring.Editor
Editor EditMode assembly: MapAuthoring.Tests.EditMode
Runtime EditMode assembly: Game.Map.Tests.EditMode
New asmdef/asmref: NO
MAP07_07 Result SHA-256: afaf3f058c34457d26491b15c06858ba1c1c7355cf14d5902d65f66a43a1fa19
MAP07_07 Task SHA-256: 0d9ec87691cf31db249b2fed7b411ea6b69a1d8c456469672c96999145add103
MAP07_07 reachability probe model/API digest: f488c8a65dacb8f7bdd2c107478074c131e3011110058375c06e165bfb1ddaf3
MAP07_06 96-cell validator model/API digest: 54a09f7327c37405a826e4fbc3bea9443e1472ec3f92f15b9495edfba422710c
MAP07_05 object-slot validator model/API digest: 9a3b86991302add138c36acccd18789ad79195cd27cab7fb5fe2c5bb8a520e6a
MAP07_04 socket-edge validator model/API digest: fdfbcb7bf651eb963d899f7e9800e0a8f23826d43ec4ffed25e3c32ee7a0c048
MAP07_03 transform model/API digest: 7031695ec2c4bb333be69ba490c03aef003124d50953d577b4943c634336b031
MAP07_02 tile-layer rules model/API digest: ace56d38399aa6ccea6e0e0e7361f3802b7eb7418b1ef1d081f6a40b04e08160
Updated MAP07_01 model/API digest: 5ed21ca7b86cfebf0095eba6d14bf4bb27be75ce39ffaf46f3c32a516d77613b
MAP07_07 acceptance: 7227/7227 PASS
MAP07_07 failed/skipped: 0/0
MAP07_07 compile/Console/relevant warnings: 0/0/0
Assets meta: 3369
Authoring CSV/meta: 50 / 50
Authoring manifest SHA-256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Generated CSV files created by MAP07_07: 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes by MAP07_07: 0
Duplicate GUID groups: 0
```

Starter authoring facts from Map Package v1.0:

```text
Microchunk dimensions: 12x8 = 96
Microchunk tile layers in exact order:
  GroundSolid
  OneWay
  Breakable
  Hazard
  Liquid
  DecorationBack
  DecorationFront
  Marker
Every complete authored chunk has exactly 96 local cell rows
Empty per-layer value is exact tile code NONE
Authoring CSV remains the static source and is not mutated by this Task
```

## Map Package Reference

Exact installed Map Package v1.0이 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
02_PHASE_ROADMAP/MAP07_MICROCHUNK_AUTHORING.md
03_CSV_SCHEMA/TILE_AUTHORING_FORMAT.md
04_CSV_STARTER/microchunk_catalog.csv
04_CSV_STARTER/microchunk_tile_cells.csv
04_CSV_STARTER/tile_code_dictionary.csv
```

Reference는 12x8 authoring grid, 8 layer order, tile code source meaning, and future CSV ownership을 확인하는 용도다. Authoring CSV body를 수정하지 않고 CSV import/export implementation도 하지 않는다.

## READ ALLOWLIST

### Existing MAP07 runtime models/rules/transforms/validators

```text
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkLocalCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTileCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkSocketDefinition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkObjectSlotDefinition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkDefinition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTileLayerOccupancy.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTileLayerRuleViolation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTileLayerRuleResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTileLayerRules.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTransformOptions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTransformUtility.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTransformResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTransformer.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkSocketBandDefinition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkEdgeSignatureDefinition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkSocketEdgeValidationViolation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkSocketEdgeValidationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkSocketEdgeValidator.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkObjectSlotPoolDefinition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkObjectSlotValidationPolicy.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkObjectSlotValidationViolation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkObjectSlotValidationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkObjectSlotValidator.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/Microchunk96CellRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/Microchunk96CellValidationPolicy.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/Microchunk96CellValidationViolation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/Microchunk96CellValidationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/Microchunk96CellValidator.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTraversalNode.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTraversalEdge.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkReachabilityPolicy.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkReachabilityViolation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkReachabilityResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkReachabilityProbe.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Editor/MapAuthoring/MapAuthoring.Editor.asmdef
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/MapAuthoring.Tests.EditMode.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

### Existing editor files for style and assembly boundary

```text
Assets/_Game/Editor/MapAuthoring/Preview/OptionalRegionOverlaySceneDrawer.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Preview/OptionalRegionOverlaySceneDrawerTests.cs
```

### Existing tests for style and boundary advance

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkDefinitionTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkTileLayerRulesTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkTransformerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkSocketEdgeValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkObjectSlotValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/Microchunk96CellValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkReachabilityProbeTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map06ExitTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionGrowerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalAttachmentEnumeratorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionModelsTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Type0RouteMaskAssignerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalAccessRuleAssignerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRewardTierCalculatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalReturnPolicyResolverTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/InactiveBufferAssignerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map05ExitTests.cs
```

위 파일과 matching meta, approved Runtime/Editor/Test path-only inventory, Authoring CSV/meta count and aggregate hash, full Assets meta GUID, patch marker 이후 change-scope path만 확인할 수 있다.

금지: MAP07_09+ Task body, socket/slot editor body, CSV import/export body, preview/report body, sector assembly, boundary chunk body, Legacy/Stage/P6/P11 generator body, Scene/Prefab YAML.

## WRITE ALLOWLIST

### 신규 Editor production folder/meta - exact 2

```text
Assets/_Game/Editor/MapAuthoring/Microchunks/
Assets/_Game/Editor/MapAuthoring/Microchunks.meta
```

### 신규 Editor production C# - exact 6

```text
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkAuthoringGridCell.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkAuthoringGridLayer.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkAuthoringGridState.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkAuthoringGridPalette.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkAuthoringGridViewModel.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkAuthoringGridWindow.cs
```

### 신규 Editor EditMode test folder/meta - exact 2

```text
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks/
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks.meta
```

### 신규 Editor EditMode tests - exact 1

```text
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks/MicrochunkAuthoringGridTests.cs
```

### 기존 phase-boundary test 수정 - exact up to 17

MAP07_08 production symbol `MicrochunkAuthoringGrid`를 허용하고 MAP07_09+ future symbols 금지를 유지하기 위해 필요한 경우 아래 existing test C#만 수정할 수 있다. matching `.cs.meta`는 수정하지 않는다.

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkDefinitionTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkTileLayerRulesTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkTransformerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkSocketEdgeValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkObjectSlotValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/Microchunk96CellValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkReachabilityProbeTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map06ExitTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionGrowerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalAttachmentEnumeratorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionModelsTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Type0RouteMaskAssignerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalAccessRuleAssignerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRewardTierCalculatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalReturnPolicyResolverTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/InactiveBufferAssignerTests.cs
```

### Result report - exact 1

```text
MapDesign/MCP/REPORTS/MAP07_08_CREATE_MICROCHUNK_AUTHORING_GRID_RESULT.md
```

## Required Implementation Contract

### Editor-only assembly boundary

- All production files for this Task must be under `Assets/_Game/Editor/MapAuthoring/Microchunks/`.
- All new tests for this Task must be under `Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks/`.
- New runtime C# is forbidden in this Task.
- New asmdef/asmref is forbidden. Use existing `MapAuthoring.Editor` and `MapAuthoring.Tests.EditMode` assemblies.
- The grid window may use IMGUI or UI Toolkit. The underlying state and view model must be testable without relying on screenshots or editor layout timing.

### Grid state model

- `MicrochunkAuthoringGridCell` stores exact local coordinate `(x,y)` with bounds `0..11` and `0..7`.
- `MicrochunkAuthoringGridLayer` exposes the exact eight layer order:
  - `GroundSolid`
  - `OneWay`
  - `Breakable`
  - `Hazard`
  - `Liquid`
  - `DecorationBack`
  - `DecorationFront`
  - `Marker`
- `MicrochunkAuthoringGridState` owns exactly 96 cells in row-major order.
- Each cell has one selected tile code per layer.
- Empty layer value is exact `NONE`.
- All rows are present even when every layer is `NONE`.
- Constructor and mutation APIs must reject out-of-range local coordinates and unknown layer indices without silently clamping.

### Painting and palette model

- `MicrochunkAuthoringGridPalette` stores the currently selected layer and tile code.
- Palette must include an exact `NONE` swatch and must allow switching among all eight layers.
- Painting one cell sets only the selected layer on that cell.
- Erasing one cell sets only the selected layer on that cell to `NONE`.
- Rectangle/multi-cell painting is allowed only if it uses the same per-cell mutation path and preserves deterministic row-major application order.
- Clearing a layer is allowed; clearing all layers is allowed. Neither operation may remove cells from the 96-cell state.

### Runtime projection

- The view model must project the current editor state into exactly 96 `MicrochunkTileCell`/coverage records using existing MAP07 runtime types where available.
- The projection must preserve row-major order and local coordinate labels.
- The projection must include all-`NONE` cells.
- The projection may run existing MAP07_02 tile-layer and MAP07_06 96-cell validators as inline grid feedback.
- Socket-edge, object-slot, reachability, transform preview, CSV import/export, and starter catalog round-trip validation are not implemented here.
- Projection must not mutate Authoring CSV, generated CSV, runtime definitions, or source collections.

### Window behavior

- `MicrochunkAuthoringGridWindow` creates a deterministic Editor window for the 12x8 grid.
- Required controls:
  - layer selector with the eight exact layers;
  - tile code text entry or deterministic palette list;
  - `NONE`/erase control;
  - 12x8 fixed grid display;
  - clear selected layer;
  - clear all layers;
  - simple validation summary from the allowed inline validators.
- The window must not save assets, write CSV, import CSV, open socket/slot editing panels, generate preview reports, or touch scene/prefab state.

## Forbidden Implementation

```text
MicrochunkSocketAndSlotEditor
MicrochunkSocketEditor
MicrochunkSlotEditor
MicrochunkCsvImporter
MicrochunkCsvExporter
MicrochunkPreviewReport
MicrochunkReachabilityHeatmap
MicrochunkStarterCatalogRoundTrip
BoundaryChunkResolver
SectorRecipeResolver
GeneratedSectorMicrochunkWriter
PopulationSlotIndex
StableSpawnId
WorldTraversalValidator
```

## Required Tests

Create `MicrochunkAuthoringGridTests.cs` with deterministic Editor EditMode coverage:

- Exact 12x8 dimensions and 96 row-major cell creation.
- Exact eight layer order and `NONE` default for every layer.
- Local coordinate bounds reject `x < 0`, `x > 11`, `y < 0`, and `y > 7`.
- Palette selection changes only the active layer and active tile code.
- Single-cell paint/erase modifies one layer without touching the other seven layers.
- Rectangle/multi-cell painting applies deterministic row-major order.
- Clear selected layer preserves all 96 cells and other layers.
- Clear all layers leaves 96 all-`NONE` cells.
- Runtime projection emits exactly 96 records including empty cells.
- Projection preserves row-major order and coordinates.
- Inline tile-layer feedback detects forbidden occupied-layer combinations through existing rules.
- Inline 96-cell feedback uses the existing coverage validator or equivalent allowed existing API.
- Window/view model commands do not import CSV, export CSV, create ScriptableObject assets, create generated CSV, or dirty scenes/prefabs.
- Source grid state and projected runtime collections are not mutated by validation.
- Existing MAP07_01~MAP07_07 production digests remain preserved.
- MAP07_09+ forbidden production symbols remain absent.

Required actual gates:

```text
MicrochunkAuthoringGridTests >=320 PASS
MicrochunkReachabilityProbeTests 522/522 PASS
Microchunk96CellValidatorTests 406/406 PASS
MicrochunkObjectSlotValidatorTests 483/483 PASS
MicrochunkSocketEdgeValidatorTests 332/332 PASS
MicrochunkTransformerTests 483/483 PASS
MicrochunkTileLayerRulesTests 150/150 PASS
MicrochunkDefinitionTests 146/146 PASS
Existing MAP07 regression union 2000/2000 PASS
MAP06 required total 2746/2746 PASS
MAP05 required total 1959/1959 PASS
Actually executed total >=7547 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
```

## Static and Change-Scope Gates

```text
Assets meta 3369 -> 3378
new Editor production folder/meta 1/1
new Editor production C#/meta 6/6
new Editor test folder/meta 1/1
new Editor test C#/meta 1/1
new Runtime C#/meta 0/0
existing boundary test C# modified <=17
matching existing test meta modified 0
Authoring CSV/meta 50/50 and manifest unchanged
Generated CSV files created 0
Scene/Prefab tracked changes 0/0
ProjectSettings/Packages changes 0/0
asmdef/asmref changes 0/0
MAP07_01 production source changes 0
MAP07_02 production source changes 0
MAP07_03 production source changes 0
MAP07_04 production source changes 0
MAP07_05 production source changes 0
MAP07_06 production source changes 0
MAP07_07 production source changes 0
MAP06 production source changes 0
Forbidden MAP07_09+ production hits 0
Assets duplicate GUID groups 0
Unapplied MCP patches 0
```

## Result Report Requirements

Write `MapDesign/MCP/REPORTS/MAP07_08_CREATE_MICROCHUNK_AUTHORING_GRID_RESULT.md` containing:

```text
TASK: MAP07_08_CREATE_MICROCHUNK_AUTHORING_GRID
STATUS: PASS | FAIL | BLOCKED
MAP07_08: COMPLETE ELIGIBLE only if PASS
MAP07_09_CREATE_SOCKET_AND_SLOT_EDITOR: LOCKED / DO NOT START
```

The report must include:

- Applied patch receipt SHA-256.
- MAP07_07 Result SHA-256 `afaf3f058c34457d26491b15c06858ba1c1c7355cf14d5902d65f66a43a1fa19`.
- MAP07_07 Task SHA-256 `0d9ec87691cf31db249b2fed7b411ea6b69a1d8c456469672c96999145add103`.
- MAP07_08 Task SHA-256 from this file.
- Authoring grid deterministic editor model/API digest.
- Preserved reachability, 96-cell, object-slot, socket-edge, transform, tile-layer, and MAP07_01 model/API digests.
- Required test execution counts and failed/skipped totals.
- Unity compile/Console/relevant warning counts.
- Assets meta before/after, new C#/meta counts, folder meta counts, and duplicate GUID groups.
- Authoring CSV/meta count and manifest hash proving no source CSV mutation.
- Generated CSV, Scene, Prefab, ProjectSettings, Packages, asmdef, asmref change counts.
- Confirmation that MAP07_09+ production symbols remain absent.

PASS finalization may only mark MAP07_08 COMPLETE and set Current Task to NONE. MAP07_09 remains LOCKED until a separate MAP07_09 patch is applied.
