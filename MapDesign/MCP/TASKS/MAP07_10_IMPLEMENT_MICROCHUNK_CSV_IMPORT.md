# MAP07_10 - Implement Microchunk CSV Import

```yaml
status_control:
  task_key: MAP07_10_IMPLEMENT_MICROCHUNK_CSV_IMPORT
  result_file: REPORTS/MAP07_10_IMPLEMENT_MICROCHUNK_CSV_IMPORT_RESULT.md
```

## TASK TYPE

```text
EDITOR-ONLY SELECTED MICROCHUNK AUTHORING CSV IMPORT + IN-MEMORY GRID/SOCKET/SLOT STATE HYDRATION + PHASE-BOUNDARY TEST ADVANCE
```

## Objective

MAP07_09 PASS/finalize 뒤 selected microchunk ID의 Authoring CSV rows를 읽어 MAP07_08 grid state와 MAP07_09 socket/slot editor state로 가져오는 Editor-only importer를 구현한다. 이 Task는 read-only Authoring CSV source에서 catalog, tile cells, sockets, socket bands, object slots, and variant rows를 읽고 deterministic in-memory editor state를 구성하는 범위까지만 연다.

CSV export, row replacement, source CSV mutation, generated CSV writer, transform preview, reachability heatmap/report, starter catalog round-trip, sector assembly, world traversal은 구현하지 않는다. MAP07_11 이후 Task body는 읽거나 시작하지 않는다.

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
12. `REPORTS/MAP07_09_CREATE_SOCKET_AND_SLOT_EDITOR_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP07_09_CREATE_SOCKET_AND_SLOT_EDITOR
STATUS: PASS
MAP07_09: COMPLETE ELIGIBLE
MAP07_10_IMPLEMENT_MICROCHUNK_CSV_IMPORT: LOCKED / DO NOT START
SHA-256: 7bc550e92359f4f24c642b24000be1e1a8198fdeb014ce1685555bf5f83a0340
```

이 별도 patch가 적용된 뒤에만 MAP07_10을 실행한다. MAP07_11 이후 Task body는 읽거나 시작하지 않는다.

## Approved Input Baseline

```text
Unity: 6000.3.8f1
Runtime assembly: Game.Map.Runtime
Runtime namespace boundary: StarNight.Map.WorldGeneration.*
Editor assembly: MapAuthoring.Editor
Editor EditMode assembly: MapAuthoring.Tests.EditMode
Runtime EditMode assembly: Game.Map.Tests.EditMode
New asmdef/asmref: NO
MAP07_09 Result SHA-256: 7bc550e92359f4f24c642b24000be1e1a8198fdeb014ce1685555bf5f83a0340
MAP07_09 Task SHA-256: 5e870b792acdaff3ffb12058919f8973cd0fa50dcfd505b662c323f47a6f1a87
MAP07_09 socket and slot editor model/API digest: fee0842a7066866ec9f665fbd924c2fead24300c190d9be8e6e42ff0b435dffa
MAP07_08 authoring grid editor model/API digest: fe55586945da9aaa3b4bcebb3dd38ac82d2f5287e9f99bc31dc50fd30163abe9
MAP07_07 reachability probe model/API digest: f488c8a65dacb8f7bdd2c107478074c131e3011110058375c06e165bfb1ddaf3
MAP07_06 96-cell validator model/API digest: 54a09f7327c37405a826e4fbc3bea9443e1472ec3f92f15b9495edfba422710c
MAP07_05 object-slot validator model/API digest: 9a3b86991302add138c36acccd18789ad79195cd27cab7fb5fe2c5bb8a520e6a
MAP07_04 socket-edge validator model/API digest: fdfbcb7bf651eb963d899f7e9800e0a8f23826d43ec4ffed25e3c32ee7a0c048
MAP07_03 transform model/API digest: 7031695ec2c4bb333be69ba490c03aef003124d50953d577b4943c634336b031
MAP07_02 tile-layer rules model/API digest: ace56d38399aa6ccea6e0e0e7361f3802b7eb7418b1ef1d081f6a40b04e08160
Updated MAP07_01 model/API digest: 5ed21ca7b86cfebf0095eba6d14bf4bb27be75ce39ffaf46f3c32a516d77613b
MAP07_09 acceptance: 7927/7927 PASS
MAP07_09 failed/skipped: 0/0
MAP07_09 compile/Console/relevant warnings: 0/0/0
Assets meta: 3386
Authoring CSV/meta: 50 / 50
Authoring manifest SHA-256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Generated CSV files created by MAP07_09: 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes by MAP07_09: 0
Duplicate GUID groups: 0
```

Starter authoring facts from Map Package v1.0:

```text
Import input is Authoring CSV, not generated CSV.
Selected import target is one microchunk ID.
microchunk_catalog controls microchunk-level metadata and tile_data_complete.
microchunk_tile_cells controls 12x8 layer values.
microchunk_sockets and socket_band_definitions hydrate socket rows and band rows.
microchunk_object_slots hydrates object slot rows.
Empty per-layer tile code remains exact NONE.
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
04_CSV_STARTER/microchunk_variants.csv
04_CSV_STARTER/tile_code_dictionary.csv
04_CSV_STARTER/object_slot_pools.csv
04_CSV_STARTER/edge_signatures.csv
```

Reference는 import field names, selected ID filtering, and future export ownership을 확인하는 용도다. Authoring CSV body를 수정하지 않고 CSV export implementation도 하지 않는다.

## READ ALLOWLIST

### Existing CSV/import infrastructure

```text
Assets/_Game/Map/Runtime/Data/Csv/*
Assets/_Game/Map/Runtime/Data/Definitions/*
Assets/_Game/Map/Runtime/Data/Registry/*
Assets/_Game/Map/Editor/MapAuthoring/*
Assets/_Game/Editor/MapAuthoring/*
Assets/_Game/Tests/EditMode/Map/Data/*
```

위 glob은 기존 MAP01 CSV reader/schema/definition/registry/import-report conventions를 읽기 위한 것이다. 새 runtime code 작성은 금지된다.

### Existing MAP07 runtime and editor models

```text
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkLocalCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTileCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkSocketDefinition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkObjectSlotDefinition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkDefinition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkSocketBandDefinition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkEdgeSignatureDefinition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkObjectSlotPoolDefinition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkSocketEdgeValidator.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkObjectSlotValidator.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/Microchunk96CellValidator.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkAuthoringGridCell.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkAuthoringGridLayer.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkAuthoringGridState.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkAuthoringGridPalette.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkAuthoringGridViewModel.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkAuthoringGridWindow.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkSocketAuthoringRow.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkSocketBandAuthoringRow.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkSocketAuthoringCollection.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkObjectSlotAuthoringRow.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkObjectSlotAuthoringCollection.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkSocketAndSlotEditorViewModel.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkSocketAndSlotEditorWindow.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Editor/MapAuthoring/MapAuthoring.Editor.asmdef
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/MapAuthoring.Tests.EditMode.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

### Existing tests for style and boundary advance

```text
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks/MicrochunkSocketAndSlotEditorTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks/MicrochunkAuthoringGridTests.cs
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
```

위 파일과 matching meta, approved Runtime/Editor/Test path-only inventory, Authoring CSV/meta count and aggregate hash, full Assets meta GUID, patch marker 이후 change-scope path만 확인할 수 있다.

금지: MAP07_11+ Task body, CSV export body, preview/report body, starter round-trip body, sector assembly, boundary chunk body, Legacy/Stage/P6/P11 generator body, Scene/Prefab YAML.

## WRITE ALLOWLIST

### 신규 Editor production C# - exact 6

```text
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvImportSource.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvImportRequest.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvImportIssue.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvImportResult.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvImporter.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvImportWindow.cs
```

### 신규 Editor EditMode tests - exact 1

```text
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks/MicrochunkCsvImporterTests.cs
```

### 기존 phase-boundary test 수정 - exact up to 17

MAP07_10 production symbol `MicrochunkCsvImporter`를 허용하고 MAP07_11+ future symbols 금지를 유지하기 위해 필요한 경우 아래 existing test C#만 수정할 수 있다. matching `.cs.meta`는 수정하지 않는다.

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkDefinitionTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkTileLayerRulesTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkTransformerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkSocketEdgeValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkObjectSlotValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/Microchunk96CellValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkReachabilityProbeTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks/MicrochunkAuthoringGridTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks/MicrochunkSocketAndSlotEditorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map06ExitTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionGrowerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalAttachmentEnumeratorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionModelsTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Type0RouteMaskAssignerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalAccessRuleAssignerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRewardTierCalculatorTests.cs
```

### Result report - exact 1

```text
MapDesign/MCP/REPORTS/MAP07_10_IMPLEMENT_MICROCHUNK_CSV_IMPORT_RESULT.md
```

## Required Implementation Contract

### Editor-only assembly boundary

- All production files for this Task must be under `Assets/_Game/Editor/MapAuthoring/Microchunks/`.
- All new tests for this Task must be under `Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks/`.
- New runtime C# is forbidden in this Task.
- New asmdef/asmref is forbidden. Use existing `MapAuthoring.Editor` and `MapAuthoring.Tests.EditMode` assemblies.
- Existing MAP01 CSV reader/schema/registry code should be reused when available. If the importer needs a small adapter, it must remain Editor-only and deterministic.

### Import source and request

- `MicrochunkCsvImportSource` represents read-only source CSV tables or byte snapshots for catalog, tile cells, sockets, socket bands, object slots, variants, and reference dictionaries.
- `MicrochunkCsvImportRequest` contains exactly one selected microchunk ID.
- The selected ID must be canonical non-blank and must not be guessed from file order.
- Missing selected catalog row is an import failure.
- Duplicate selected catalog rows are an import failure.
- Import must never write back to Authoring CSV or generated CSV.

### Tile cell import

- For `tile_data_complete=1`, the selected ID must provide exactly 96 unique in-bounds local cells.
- Duplicate local coordinates, missing cells, and out-of-range x/y are import failures for a complete tile set.
- For non-complete catalog rows, importer still creates a 96-cell editor grid with missing cells filled as exact `NONE`, but it must report deterministic non-complete import issues.
- Empty layer values and omitted per-layer values become exact `NONE` only through the documented importer path.
- The final grid state must be row-major and detached from source CSV row objects.

### Socket, band, slot, and variant import

- Socket rows for the selected ID hydrate MAP07_09 socket rows and reference imported or shared band IDs.
- Socket bands hydrate MAP07_09 band rows with side-compatible inclusive ranges.
- Object slot rows for the selected ID hydrate MAP07_09 object slot rows with anchor coordinate, category, pool ID, and orientation.
- Variant rows may be read and preserved in the import result as metadata, but transform preview or variant execution is forbidden.
- Import issues are ordered by file name, selected ID, row number, column name, and issue code.
- The importer must preserve source row order only for diagnostics. Editor state collections must use deterministic canonical ordering.

### Validation feedback

- Import success should run or expose existing feedback from:
  - MAP07_02 tile-layer rules;
  - MAP07_06 96-cell validator;
  - MAP07_04 socket-edge validator;
  - MAP07_05 object-slot validator.
- Reachability validation may be listed as available input but must not produce heatmaps, path previews, or reports in this Task.
- The importer must not mutate imported grid/socket/slot state when validation feedback is requested.

### Window behavior

- `MicrochunkCsvImportWindow` provides deterministic controls for selecting a microchunk ID and importing read-only Authoring CSV into in-memory editor state.
- The window may hand off imported state to existing grid and socket/slot editor view models.
- The window must not save assets, export CSV, replace rows, generate output CSV, generate preview reports, dirty scenes/prefabs, or change ProjectSettings/Packages.

## Forbidden Implementation

```text
MicrochunkCsvExporter
MicrochunkCsvExportWindow
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

Create `MicrochunkCsvImporterTests.cs` with deterministic Editor EditMode coverage:

- Selected ID required, missing catalog row failure, and duplicate catalog row failure.
- RFC4180 quoted/comma/newline/BOM cases through existing CSV infrastructure or an approved Editor adapter.
- Complete tile data imports exactly 96 unique in-bounds row-major cells.
- Duplicate, missing, and out-of-range tile-cell diagnostics for complete rows.
- Non-complete catalog rows import 96 editor cells with missing cells filled as `NONE` and deterministic issues.
- Layer values import into the existing 8-layer grid state without mutating source rows.
- Socket rows import side, band ID, traversal kind, edge signature, mandatory flag, and tool requirement.
- Socket band rows import L/R y-ranges and D/U x-ranges and reject side-incompatible ranges.
- Object slot rows import anchor, category, pool ID, and orientation.
- Variant rows are read as metadata only; transform preview remains absent.
- Import result diagnostics order is stable by file, ID, row, column, issue code.
- Existing tile-layer, 96-cell, socket-edge, and object-slot validators can consume imported state.
- Import window/view model commands do not export CSV, create ScriptableObject assets, create generated CSV, or dirty scenes/prefabs.
- Existing MAP07_01~MAP07_09 production digests remain preserved.
- MAP07_11+ forbidden production symbols remain absent.

Required actual gates:

```text
MicrochunkCsvImporterTests >=420 PASS
MicrochunkSocketAndSlotEditorTests 380/380 PASS
MicrochunkAuthoringGridTests 320/320 PASS
MicrochunkReachabilityProbeTests 522/522 PASS
Existing MAP07 regression union 2000/2000 PASS
MAP06 required total 2746/2746 PASS
MAP05 required total 1959/1959 PASS
Actually executed total >=8347 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
```

## Static and Change-Scope Gates

```text
Assets meta 3386 -> 3393
new Editor production C#/meta 6/6
new Editor test C#/meta 1/1
new folder meta 0
new Runtime C#/meta 0/0
existing boundary test C# modified <=17
matching existing test meta modified 0
Authoring CSV/meta 50/50 and manifest unchanged
Authoring CSV tracked changes 0
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
MAP07_09 production source changes 0
MAP06 production source changes 0
Forbidden MAP07_11+ production hits 0
Assets duplicate GUID groups 0
Unapplied MCP patches 0
```

## Result Report Requirements

Write `MapDesign/MCP/REPORTS/MAP07_10_IMPLEMENT_MICROCHUNK_CSV_IMPORT_RESULT.md` containing:

```text
TASK: MAP07_10_IMPLEMENT_MICROCHUNK_CSV_IMPORT
STATUS: PASS | FAIL | BLOCKED
MAP07_10: COMPLETE ELIGIBLE only if PASS
MAP07_11_IMPLEMENT_MICROCHUNK_CSV_EXPORT: LOCKED / DO NOT START
```

The report must include:

- Applied patch receipt SHA-256.
- MAP07_09 Result SHA-256 `7bc550e92359f4f24c642b24000be1e1a8198fdeb014ce1685555bf5f83a0340`.
- MAP07_09 Task SHA-256 `5e870b792acdaff3ffb12058919f8973cd0fa50dcfd505b662c323f47a6f1a87`.
- MAP07_10 Task SHA-256 from this file.
- CSV importer deterministic editor model/API digest.
- Preserved socket/slot editor, authoring grid, reachability, 96-cell, object-slot, socket-edge, transform, tile-layer, and MAP07_01 model/API digests.
- Required test execution counts and failed/skipped totals.
- Unity compile/Console/relevant warning counts.
- Assets meta before/after, new C#/meta counts, folder meta counts, and duplicate GUID groups.
- Authoring CSV/meta count and manifest hash proving no source CSV mutation.
- Generated CSV, Scene, Prefab, ProjectSettings, Packages, asmdef, asmref change counts.
- Confirmation that MAP07_11+ production symbols remain absent.

PASS finalization may only mark MAP07_10 COMPLETE and set Current Task to NONE. MAP07_11 remains LOCKED until a separate MAP07_11 patch is applied.
