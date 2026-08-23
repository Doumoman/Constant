# MAP07_09 - Create Socket and Slot Editor

```yaml
status_control:
  task_key: MAP07_09_CREATE_SOCKET_AND_SLOT_EDITOR
  result_file: REPORTS/MAP07_09_CREATE_SOCKET_AND_SLOT_EDITOR_RESULT.md
```

## TASK TYPE

```text
EDITOR-ONLY MICROCHUNK SOCKET/BAND/SIGNATURE AND OBJECT SLOT AUTHORING UI + VALIDATOR FEEDBACK + PHASE-BOUNDARY TEST ADVANCE
```

## Objective

MAP07_08 PASS/finalize 뒤 microchunk authoring grid에 붙는 Editor-only socket and object slot editor를 구현한다. 이 Task는 in-memory socket rows, socket band rows, edge signature selection, object slot rows, anchor coordinate editing, pool/category/orientation selection, and existing validator feedback까지 연다.

CSV import/export, starter catalog round-trip, transform preview, reachability heatmap/report, sector assembly, world traversal, generated CSV writer는 구현하지 않는다. MAP07_10 이후 Task body는 읽거나 시작하지 않는다.

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
12. `REPORTS/MAP07_08_CREATE_MICROCHUNK_AUTHORING_GRID_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP07_08_CREATE_MICROCHUNK_AUTHORING_GRID
STATUS: PASS
MAP07_08: COMPLETE ELIGIBLE
MAP07_09_CREATE_SOCKET_AND_SLOT_EDITOR: LOCKED / DO NOT START
SHA-256: 3f0a2ec3c3f8668de33f180521a872a58a7cc7cb3ea11cb451dd5fcb640200d9
```

이 별도 patch가 적용된 뒤에만 MAP07_09를 실행한다. MAP07_10 이후 Task body는 읽거나 시작하지 않는다.

## Approved Input Baseline

```text
Unity: 6000.3.8f1
Runtime assembly: Game.Map.Runtime
Runtime namespace boundary: StarNight.Map.WorldGeneration.*
Editor assembly: MapAuthoring.Editor
Editor EditMode assembly: MapAuthoring.Tests.EditMode
Runtime EditMode assembly: Game.Map.Tests.EditMode
New asmdef/asmref: NO
MAP07_08 Result SHA-256: 3f0a2ec3c3f8668de33f180521a872a58a7cc7cb3ea11cb451dd5fcb640200d9
MAP07_08 Task SHA-256: 6d3b211b593743d9aebf6ba4f0c4fc9ef720d85139e9fe1e687231014ee00f29
MAP07_08 authoring grid editor model/API digest: fe55586945da9aaa3b4bcebb3dd38ac82d2f5287e9f99bc31dc50fd30163abe9
MAP07_07 reachability probe model/API digest: f488c8a65dacb8f7bdd2c107478074c131e3011110058375c06e165bfb1ddaf3
MAP07_06 96-cell validator model/API digest: 54a09f7327c37405a826e4fbc3bea9443e1472ec3f92f15b9495edfba422710c
MAP07_05 object-slot validator model/API digest: 9a3b86991302add138c36acccd18789ad79195cd27cab7fb5fe2c5bb8a520e6a
MAP07_04 socket-edge validator model/API digest: fdfbcb7bf651eb963d899f7e9800e0a8f23826d43ec4ffed25e3c32ee7a0c048
MAP07_03 transform model/API digest: 7031695ec2c4bb333be69ba490c03aef003124d50953d577b4943c634336b031
MAP07_02 tile-layer rules model/API digest: ace56d38399aa6ccea6e0e0e7361f3802b7eb7418b1ef1d081f6a40b04e08160
Updated MAP07_01 model/API digest: 5ed21ca7b86cfebf0095eba6d14bf4bb27be75ce39ffaf46f3c32a516d77613b
MAP07_08 acceptance: 7547/7547 PASS
MAP07_08 failed/skipped: 0/0
MAP07_08 compile/Console/relevant warnings: 0/0/0
Assets meta: 3378
Authoring CSV/meta: 50 / 50
Authoring manifest SHA-256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Generated CSV files created by MAP07_08: 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes by MAP07_08: 0
Duplicate GUID groups: 0
```

Starter authoring facts from Map Package v1.0:

```text
Socket rows carry side, band, traversal kind, edge signature, mandatory flag, and tool requirement.
Socket bands constrain an inclusive edge range:
  L/R bands use y in 0..7.
  D/U bands use x in 0..11.
Object slots are separate from tiles and carry anchor cell, category, pool ID, and orientation.
Existing MAP07_04 and MAP07_05 validators own socket-edge and object-slot semantic validation.
Authoring CSV remains the static source and is not mutated by this Task.
```

## Map Package Reference

Exact installed Map Package v1.0이 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
02_PHASE_ROADMAP/MAP07_MICROCHUNK_AUTHORING.md
03_CSV_SCHEMA/TILE_AUTHORING_FORMAT.md
04_CSV_STARTER/microchunk_catalog.csv
04_CSV_STARTER/microchunk_tile_cells.csv
04_CSV_STARTER/microchunk_sockets.csv
04_CSV_STARTER/socket_band_definitions.csv
04_CSV_STARTER/microchunk_object_slots.csv
04_CSV_STARTER/object_slot_pools.csv
04_CSV_STARTER/edge_signatures.csv
```

Reference는 socket/band/signature fields, object slot fields, and future CSV ownership을 확인하는 용도다. Authoring CSV body를 수정하지 않고 CSV import/export implementation도 하지 않는다.

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

### Existing MAP07 editor grid files

```text
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkAuthoringGridCell.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkAuthoringGridLayer.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkAuthoringGridState.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkAuthoringGridPalette.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkAuthoringGridViewModel.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkAuthoringGridWindow.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks/MicrochunkAuthoringGridTests.cs
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
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks/MicrochunkAuthoringGridTests.cs
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

위 파일과 matching meta, approved Runtime/Editor/Test path-only inventory, Authoring CSV/meta count and aggregate hash, full Assets meta GUID, patch marker 이후 change-scope path만 확인할 수 있다.

금지: MAP07_10+ Task body, CSV import/export body, preview/report body, starter round-trip body, sector assembly, boundary chunk body, Legacy/Stage/P6/P11 generator body, Scene/Prefab YAML.

## WRITE ALLOWLIST

### 신규 Editor production C# - exact 7

```text
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkSocketAuthoringRow.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkSocketBandAuthoringRow.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkSocketAuthoringCollection.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkObjectSlotAuthoringRow.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkObjectSlotAuthoringCollection.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkSocketAndSlotEditorViewModel.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkSocketAndSlotEditorWindow.cs
```

### 신규 Editor EditMode tests - exact 1

```text
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks/MicrochunkSocketAndSlotEditorTests.cs
```

### 기존 phase-boundary test 수정 - exact up to 17

MAP07_09 production symbol `MicrochunkSocketAndSlotEditor`를 허용하고 MAP07_10+ future symbols 금지를 유지하기 위해 필요한 경우 아래 existing test C#만 수정할 수 있다. matching `.cs.meta`는 수정하지 않는다.

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkDefinitionTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkTileLayerRulesTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkTransformerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkSocketEdgeValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkObjectSlotValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/Microchunk96CellValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkReachabilityProbeTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks/MicrochunkAuthoringGridTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map06ExitTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionGrowerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalAttachmentEnumeratorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionModelsTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Type0RouteMaskAssignerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalAccessRuleAssignerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRewardTierCalculatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalReturnPolicyResolverTests.cs
```

### Result report - exact 1

```text
MapDesign/MCP/REPORTS/MAP07_09_CREATE_SOCKET_AND_SLOT_EDITOR_RESULT.md
```

## Required Implementation Contract

### Editor-only assembly boundary

- All production files for this Task must be under `Assets/_Game/Editor/MapAuthoring/Microchunks/`.
- All new tests for this Task must be under `Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks/`.
- New runtime C# is forbidden in this Task.
- New asmdef/asmref is forbidden. Use existing `MapAuthoring.Editor` and `MapAuthoring.Tests.EditMode` assemblies.
- The editor may be a separate window or an integrated tab launched from the existing authoring grid window, but socket/slot state must be testable without relying on screenshots or editor layout timing.

### Socket and band authoring

- `MicrochunkSocketAuthoringRow` stores socket ID, side, band ID, traversal kind, edge signature ID, mandatory flag, and tool requirement.
- Socket ID, band ID, traversal kind, edge signature ID, and tool requirement are canonical non-blank string tokens. Default tool requirement is exact `NONE`.
- Socket side must be exact `L`, `R`, `D`, or `U`. Unknown side values are rejected without clamping or fallback.
- `MicrochunkSocketBandAuthoringRow` stores band ID, side, inclusive start, inclusive end, and optional outer-clearance metadata if existing runtime APIs expose it.
- L/R bands validate `0 <= start <= end <= 7`.
- D/U bands validate `0 <= start <= end <= 11`.
- Band rows are held in deterministic ID order and socket rows reference band IDs without copying or mutating source rows.
- Add, duplicate, remove, and reorder operations must be deterministic and must reject duplicate IDs.

### Object slot authoring

- `MicrochunkObjectSlotAuthoringRow` stores slot ID, anchor coordinate, category token, pool ID, orientation, and optional safety-radius metadata if existing runtime APIs expose it.
- Anchor coordinate must be in 12x8 bounds and use existing `MicrochunkLocalCoord` where possible.
- Pool ID and category token are canonical non-blank strings.
- Orientation must use the existing MAP07 object-orientation contract, including `NONE/L/R/U/D` after the MAP07_03 repair.
- Add, duplicate, remove, and reorder operations must be deterministic and must reject duplicate IDs.

### Projection and validation feedback

- The view model must project the current in-memory grid, socket rows, band rows, and object slot rows into existing runtime definition types without mutating any source collection.
- Tile cell projection remains owned by MAP07_08 authoring grid state.
- Socket-edge feedback may call only the existing MAP07_04 socket-edge validator.
- Object-slot feedback may call only the existing MAP07_05 object-slot validator.
- 96-cell and tile-layer feedback may remain visible from the existing grid view model but must not be reimplemented.
- Reachability feedback, transform preview, CSV import/export, starter round-trip validation, sector assembly, and world traversal are forbidden in this Task.

### Window behavior

- `MicrochunkSocketAndSlotEditorWindow` exposes deterministic controls for:
  - socket list add/duplicate/remove/reorder;
  - socket side, band ID, traversal kind, edge signature ID, mandatory flag, and tool requirement;
  - band list add/duplicate/remove/reorder and inclusive range editing;
  - object slot list add/duplicate/remove/reorder;
  - slot anchor x/y, category, pool ID, and orientation;
  - validation summary from existing socket-edge and object-slot validators.
- The window must not save assets, import CSV, export CSV, generate output CSV, generate preview reports, dirty scenes/prefabs, or change ProjectSettings/Packages.

## Forbidden Implementation

```text
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

Create `MicrochunkSocketAndSlotEditorTests.cs` with deterministic Editor EditMode coverage:

- Socket row default values, canonical token validation, side validation, and duplicate-ID rejection.
- Band row side/range validation for L/R y-ranges and D/U x-ranges.
- Socket-to-band reference validation and deterministic row ordering.
- Add, duplicate, remove, and reorder socket rows without hidden mutation.
- Object slot row default values, anchor bounds, category/pool token validation, orientation contract, and duplicate-ID rejection.
- Add, duplicate, remove, and reorder object slot rows without hidden mutation.
- Projection into existing runtime socket/band/object-slot definition types.
- Projection combines existing grid tile cells with socket and slot rows without changing the grid state.
- Existing MAP07_04 socket-edge validator is used for feedback and detects intentionally bad band/edge fixtures.
- Existing MAP07_05 object-slot validator is used for feedback and detects intentionally bad anchor/pool fixtures.
- Validation feedback is deterministic and does not mutate source rows or projected runtime collections.
- Window/view model commands do not import CSV, export CSV, create ScriptableObject assets, create generated CSV, or dirty scenes/prefabs.
- Existing MAP07_01~MAP07_08 production digests remain preserved.
- MAP07_10+ forbidden production symbols remain absent.

Required actual gates:

```text
MicrochunkSocketAndSlotEditorTests >=380 PASS
MicrochunkAuthoringGridTests 320/320 PASS
MicrochunkReachabilityProbeTests 522/522 PASS
Existing MAP07 regression union 2000/2000 PASS
MAP06 required total 2746/2746 PASS
MAP05 required total 1959/1959 PASS
Actually executed total >=7927 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
```

## Static and Change-Scope Gates

```text
Assets meta 3378 -> 3386
new Editor production C#/meta 7/7
new Editor test C#/meta 1/1
new folder meta 0
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
MAP07_08 production source changes 0
MAP06 production source changes 0
Forbidden MAP07_10+ production hits 0
Assets duplicate GUID groups 0
Unapplied MCP patches 0
```

## Result Report Requirements

Write `MapDesign/MCP/REPORTS/MAP07_09_CREATE_SOCKET_AND_SLOT_EDITOR_RESULT.md` containing:

```text
TASK: MAP07_09_CREATE_SOCKET_AND_SLOT_EDITOR
STATUS: PASS | FAIL | BLOCKED
MAP07_09: COMPLETE ELIGIBLE only if PASS
MAP07_10_IMPLEMENT_MICROCHUNK_CSV_IMPORT: LOCKED / DO NOT START
```

The report must include:

- Applied patch receipt SHA-256.
- MAP07_08 Result SHA-256 `3f0a2ec3c3f8668de33f180521a872a58a7cc7cb3ea11cb451dd5fcb640200d9`.
- MAP07_08 Task SHA-256 `6d3b211b593743d9aebf6ba4f0c4fc9ef720d85139e9fe1e687231014ee00f29`.
- MAP07_09 Task SHA-256 from this file.
- Socket and slot editor deterministic editor model/API digest.
- Preserved authoring grid, reachability, 96-cell, object-slot, socket-edge, transform, tile-layer, and MAP07_01 model/API digests.
- Required test execution counts and failed/skipped totals.
- Unity compile/Console/relevant warning counts.
- Assets meta before/after, new C#/meta counts, folder meta counts, and duplicate GUID groups.
- Authoring CSV/meta count and manifest hash proving no source CSV mutation.
- Generated CSV, Scene, Prefab, ProjectSettings, Packages, asmdef, asmref change counts.
- Confirmation that MAP07_10+ production symbols remain absent.

PASS finalization may only mark MAP07_09 COMPLETE and set Current Task to NONE. MAP07_10 remains LOCKED until a separate MAP07_10 patch is applied.
