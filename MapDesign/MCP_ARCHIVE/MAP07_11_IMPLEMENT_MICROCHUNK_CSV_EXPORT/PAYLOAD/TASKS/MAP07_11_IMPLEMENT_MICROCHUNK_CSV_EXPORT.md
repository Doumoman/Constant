# MAP07_11 - Implement Microchunk CSV Export

```yaml
status_control:
  task_key: MAP07_11_IMPLEMENT_MICROCHUNK_CSV_EXPORT
  result_file: REPORTS/MAP07_11_IMPLEMENT_MICROCHUNK_CSV_EXPORT_RESULT.md
```

## TASK TYPE

```text
EDITOR-ONLY SELECTED MICROCHUNK AUTHORING CSV EXPORT + EXACT ROW REPLACEMENT + UTF-8 BOM/STABLE SORT
```

## Objective

MAP07_10 PASS/finalize 뒤 in-memory editor state를 selected microchunk ID의 Authoring CSV rows로 내보내는 Editor-only exporter를 구현한다. 이 Task는 catalog, tile cells, sockets, object slots, variants, and owned socket-band rows의 deterministic row replacement plan, UTF-8 BOM preservation, RFC4180 serialization, and stable sort까지 연다.

Preview/report, reachability heatmap, starter catalog full round-trip, sector assembly, world traversal, generated CSV writer는 구현하지 않는다. MAP07_12 이후 Task body는 읽거나 시작하지 않는다.

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
12. `REPORTS/MAP07_10_IMPLEMENT_MICROCHUNK_CSV_IMPORT_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP07_10_IMPLEMENT_MICROCHUNK_CSV_IMPORT
STATUS: PASS
MAP07_10: COMPLETE ELIGIBLE
MAP07_11_IMPLEMENT_MICROCHUNK_CSV_EXPORT: LOCKED / DO NOT START
SHA-256: 9bf311d95b4a16518d6e8dea296fd7694c30d225a719c394c91c9addc94c5d7b
```

이 별도 patch가 적용된 뒤에만 MAP07_11을 실행한다. MAP07_12 이후 Task body는 읽거나 시작하지 않는다.

## Approved Input Baseline

```text
Unity: 6000.3.8f1
Runtime assembly: Game.Map.Runtime
Runtime namespace boundary: StarNight.Map.WorldGeneration.*
Editor assembly: MapAuthoring.Editor
Editor EditMode assembly: MapAuthoring.Tests.EditMode
Runtime EditMode assembly: Game.Map.Tests.EditMode
New asmdef/asmref: NO
MAP07_10 Result SHA-256: 9bf311d95b4a16518d6e8dea296fd7694c30d225a719c394c91c9addc94c5d7b
MAP07_10 Task SHA-256: a21f95a87c1f962fed4672376d55eb740af6fa5d8b0aa8ec286ba782b2f54735
MAP07_10 CSV importer model/API digest: 14bf29aa6edab12ed11caffbd38770690a16ac0a13c82e1ec3fc2c25739b26c6
MAP07_09 socket and slot editor model/API digest: fee0842a7066866ec9f665fbd924c2fead24300c190d9be8e6e42ff0b435dffa
MAP07_08 authoring grid editor model/API digest: fe55586945da9aaa3b4bcebb3dd38ac82d2f5287e9f99bc31dc50fd30163abe9
MAP07_07 reachability probe model/API digest: f488c8a65dacb8f7bdd2c107478074c131e3011110058375c06e165bfb1ddaf3
MAP07_06 96-cell validator model/API digest: 54a09f7327c37405a826e4fbc3bea9443e1472ec3f92f15b9495edfba422710c
MAP07_05 object-slot validator model/API digest: 9a3b86991302add138c36acccd18789ad79195cd27cab7fb5fe2c5bb8a520e6a
MAP07_04 socket-edge validator model/API digest: fdfbcb7bf651eb963d899f7e9800e0a8f23826d43ec4ffed25e3c32ee7a0c048
MAP07_03 transform model/API digest: 7031695ec2c4bb333be69ba490c03aef003124d50953d577b4943c634336b031
MAP07_02 tile-layer rules model/API digest: ace56d38399aa6ccea6e0e0e7361f3802b7eb7418b1ef1d081f6a40b04e08160
Updated MAP07_01 model/API digest: 5ed21ca7b86cfebf0095eba6d14bf4bb27be75ce39ffaf46f3c32a516d77613b
MAP07_10 acceptance: 8347/8347 PASS
MAP07_10 failed/skipped: 0/0
MAP07_10 compile/Console/relevant warnings: 0/0/0
Assets meta: 3393
Authoring CSV/meta: 50 / 50
Authoring manifest SHA-256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Generated CSV files created by MAP07_10: 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes by MAP07_10: 0
Duplicate GUID groups: 0
```

Starter authoring facts from Map Package v1.0:

```text
Export target is Authoring CSV, not generated CSV.
Selected export target is one microchunk ID.
microchunk_tile_cells export must write exactly 96 rows for complete tile data, including all-NONE cells.
Rows for the selected ID replace old rows atomically in the target file.
UTF-8 BOM is required for Authoring CSV.
Export ordering must be stable and schema-primary-key based.
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
```

Reference는 export target files, headers, primary keys, and future preview/exit ownership을 확인하는 용도다. Starter 전체 round-trip은 MAP07_13 소유이며 이 Task에서 요구하지 않는다.

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

위 glob은 기존 MAP01 CSV reader/schema/definition/registry/import-report conventions와 MAP07_10 importer를 읽기 위한 것이다. 새 runtime code 작성은 금지된다.

### Existing MAP07 editor import/grid/socket state

```text
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkAuthoringGridCell.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkAuthoringGridLayer.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkAuthoringGridState.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkAuthoringGridPalette.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkAuthoringGridViewModel.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkSocketAuthoringRow.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkSocketBandAuthoringRow.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkSocketAuthoringCollection.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkObjectSlotAuthoringRow.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkObjectSlotAuthoringCollection.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkSocketAndSlotEditorViewModel.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvImportSource.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvImportRequest.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvImportIssue.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvImportResult.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvImporter.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvImportWindow.cs
Assets/_Game/Editor/MapAuthoring/MapAuthoring.Editor.asmdef
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/MapAuthoring.Tests.EditMode.asmdef
```

### Existing tests for style and boundary advance

```text
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks/MicrochunkCsvImporterTests.cs
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
```

위 파일과 matching meta, approved Runtime/Editor/Test path-only inventory, Authoring CSV/meta count and aggregate hash, full Assets meta GUID, patch marker 이후 change-scope path만 확인할 수 있다.

금지: MAP07_12+ Task body, preview/report body, starter full round-trip body, sector assembly, boundary chunk body, Legacy/Stage/P6/P11 generator body, Scene/Prefab YAML.

## WRITE ALLOWLIST

### 신규 Editor production C# - exact 6

```text
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvExportRequest.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvExportIssue.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvExportPlan.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvExportResult.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvExporter.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvExportWindow.cs
```

### 신규 Editor EditMode tests - exact 1

```text
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks/MicrochunkCsvExporterTests.cs
```

### 기존 phase-boundary test 수정 - exact up to 17

MAP07_11 production symbol `MicrochunkCsvExporter`를 허용하고 MAP07_12+ future symbols 금지를 유지하기 위해 필요한 경우 아래 existing test C#만 수정할 수 있다. matching `.cs.meta`는 수정하지 않는다.

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
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks/MicrochunkCsvImporterTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map06ExitTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionGrowerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalAttachmentEnumeratorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionModelsTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Type0RouteMaskAssignerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalAccessRuleAssignerTests.cs
```

### Result report - exact 1

```text
MapDesign/MCP/REPORTS/MAP07_11_IMPLEMENT_MICROCHUNK_CSV_EXPORT_RESULT.md
```

## Required Implementation Contract

### Editor-only assembly boundary

- All production files for this Task must be under `Assets/_Game/Editor/MapAuthoring/Microchunks/`.
- All new tests for this Task must be under `Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks/`.
- New runtime C# is forbidden in this Task.
- New asmdef/asmref is forbidden. Use existing `MapAuthoring.Editor` and `MapAuthoring.Tests.EditMode` assemblies.
- Existing MAP01 CSV serialization conventions and MAP07_10 importer should be reused when available.

### Export request and plan

- `MicrochunkCsvExportRequest` contains exactly one selected microchunk ID and detached editor state.
- The selected ID must be canonical non-blank and must not be guessed from current UI selection.
- `MicrochunkCsvExportPlan` records per-file row removals, row insertions, final row order, before/after SHA-256, and issue list before any file is written.
- Plan generation must be deterministic and side-effect-free.
- Export must fail if the selected ID is missing from catalog and the request does not explicitly allow a new catalog row.
- Duplicate selected catalog rows are an export failure.

### Row replacement and serialization

- `microchunk_tile_cells.csv` export writes exactly 96 selected-ID rows for complete tile data, including all-`NONE` cells.
- `microchunk_catalog.csv`, `microchunk_sockets.csv`, `microchunk_object_slots.csv`, and `microchunk_variants.csv` replace only rows for the selected ID.
- `socket_band_definitions.csv` may replace only rows owned by the selected microchunk under the existing schema. If the schema has global-only bands without selected-ID ownership, the exporter must report a deterministic non-owned-band issue and leave shared band rows unchanged.
- Export serialization must preserve exact headers.
- Output files must be UTF-8 with BOM.
- RFC4180 escaping must handle comma, quote, CRLF/LF, empty field, and multiline content.
- Final row ordering must be stable and schema-primary-key based. Non-selected rows retain relative order when their primary-key order is equal.
- Export must write through an atomic plan application path for real files, but tests may use temporary in-memory or temp-folder files.

### Validation feedback

- Export preflight should run or expose existing feedback from:
  - MAP07_02 tile-layer rules;
  - MAP07_06 96-cell validator;
  - MAP07_04 socket-edge validator;
  - MAP07_05 object-slot validator.
- The exporter must not run reachability heatmap/report generation or preview/report UI in this Task.
- Full starter catalog round-trip remains MAP07_13 ownership.

### Window behavior

- `MicrochunkCsvExportWindow` provides deterministic controls for selected ID, preflight, export plan preview summary, and explicit export execution.
- The window may use current imported/editor state but must not auto-save on open or selection change.
- The window must not generate report assets, preview screenshots, generated CSV, Scene/Prefab changes, or ProjectSettings/Packages changes.

## Forbidden Implementation

```text
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

Create `MicrochunkCsvExporterTests.cs` with deterministic Editor EditMode coverage:

- Selected ID required, missing catalog row failure unless explicit create is allowed, and duplicate catalog row failure.
- Complete export emits exactly 96 tile-cell rows including all-`NONE` cells.
- Catalog, tile, socket, slot, variant selected-ID rows replace old selected-ID rows only.
- Shared non-owned socket-band rows are preserved or rejected according to existing schema ownership.
- UTF-8 BOM is present on every exported Authoring CSV file.
- RFC4180 escaping covers comma, quote, CRLF/LF, empty field, and multiline values.
- Header order is preserved exactly.
- Final row order is stable and primary-key based.
- Export plan generation is side-effect-free and reports before/after SHA-256.
- Plan application is atomic for temp-folder files and leaves originals unchanged on simulated failure.
- Exported fixture bytes can be re-imported by MAP07_10 importer for the selected ID.
- Existing validator feedback can consume exported/re-imported state.
- Export window/view model commands do not generate reports, generated CSV, Scene/Prefab changes, or settings changes.
- Existing MAP07_01~MAP07_10 production digests remain preserved.
- MAP07_12+ forbidden production symbols remain absent.

Required actual gates:

```text
MicrochunkCsvExporterTests >=460 PASS
MicrochunkCsvImporterTests 420/420 PASS
MicrochunkSocketAndSlotEditorTests 380/380 PASS
MicrochunkAuthoringGridTests 320/320 PASS
MicrochunkReachabilityProbeTests 522/522 PASS
Existing MAP07 regression union 2000/2000 PASS
MAP06 required total 2746/2746 PASS
MAP05 required total 1959/1959 PASS
Actually executed total >=8807 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
```

## Static and Change-Scope Gates

```text
Assets meta 3393 -> 3400
new Editor production C#/meta 6/6
new Editor test C#/meta 1/1
new folder meta 0
new Runtime C#/meta 0/0
existing boundary test C# modified <=17
matching existing test meta modified 0
Authoring CSV/meta 50/50
Authoring manifest unchanged unless an approved temp-fixture-only path is used
Task-local Authoring source tracked changes 0
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
MAP07_10 production source changes 0
MAP06 production source changes 0
Forbidden MAP07_12+ production hits 0
Assets duplicate GUID groups 0
Unapplied MCP patches 0
```

## Result Report Requirements

Write `MapDesign/MCP/REPORTS/MAP07_11_IMPLEMENT_MICROCHUNK_CSV_EXPORT_RESULT.md` containing:

```text
TASK: MAP07_11_IMPLEMENT_MICROCHUNK_CSV_EXPORT
STATUS: PASS | FAIL | BLOCKED
MAP07_11: COMPLETE ELIGIBLE only if PASS
MAP07_12_CREATE_MICROCHUNK_PREVIEW_AND_REPORT: LOCKED / DO NOT START
```

The report must include:

- Applied patch receipt SHA-256.
- MAP07_10 Result SHA-256 `9bf311d95b4a16518d6e8dea296fd7694c30d225a719c394c91c9addc94c5d7b`.
- MAP07_10 Task SHA-256 `a21f95a87c1f962fed4672376d55eb740af6fa5d8b0aa8ec286ba782b2f54735`.
- MAP07_11 Task SHA-256 from this file.
- CSV exporter deterministic editor model/API digest.
- Preserved importer, socket/slot editor, authoring grid, reachability, 96-cell, object-slot, socket-edge, transform, tile-layer, and MAP07_01 model/API digests.
- Required test execution counts and failed/skipped totals.
- Unity compile/Console/relevant warning counts.
- Assets meta before/after, new C#/meta counts, folder meta counts, and duplicate GUID groups.
- Authoring CSV/meta count and exact source-mutation scope.
- Generated CSV, Scene, Prefab, ProjectSettings, Packages, asmdef, asmref change counts.
- Confirmation that MAP07_12+ production symbols remain absent.

PASS finalization may only mark MAP07_11 COMPLETE and set Current Task to NONE. MAP07_12 remains LOCKED until a separate MAP07_12 patch is applied.
