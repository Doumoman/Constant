# MAP07_12 - Create Microchunk Preview And Report

```yaml
status_control:
  task_key: MAP07_12_CREATE_MICROCHUNK_PREVIEW_AND_REPORT
  result_file: REPORTS/MAP07_12_CREATE_MICROCHUNK_PREVIEW_AND_REPORT_RESULT.md
```

## TASK TYPE

```text
EDITOR-ONLY MICROCHUNK TRANSFORM PREVIEW + VALIDATION REPORT + REACHABILITY HEATMAP
```

## Objective

MAP07_11 PASS/finalize 뒤 selected microchunk editor state를 transform별로 미리 보고, tile/socket/object-slot/reachability 문제를 좌표 기반 report로 확인하는 Editor-only preview/report layer를 구현한다. 이 Task는 UI-facing diagnostics, transform preview cells, reachability heatmap, and deterministic issue aggregation까지만 연다.

Starter catalog full round-trip, MAP07 phase exit tests, sector assembly, boundary chunk content, generated CSV writer, Scene/Prefab output, runtime gameplay traversal은 구현하지 않는다. MAP07_13 이후 Task body는 읽거나 시작하지 않는다.

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
12. `REPORTS/MAP07_11_IMPLEMENT_MICROCHUNK_CSV_EXPORT_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP07_11_IMPLEMENT_MICROCHUNK_CSV_EXPORT
STATUS: PASS
MAP07_11: COMPLETE ELIGIBLE
MAP07_12_CREATE_MICROCHUNK_PREVIEW_AND_REPORT: LOCKED / DO NOT START
SHA-256: 340cbed5424208ebeef144028c1806ea6a9039e8a6c14a5f39a824b042b062c6
```

이 별도 patch가 적용된 뒤에만 MAP07_12를 실행한다. MAP07_13 이후 Task body는 읽거나 시작하지 않는다.

## Approved Input Baseline

```text
Unity: 6000.3.8f1
Runtime assembly: Game.Map.Runtime
Runtime namespace boundary: StarNight.Map.WorldGeneration.*
Editor assembly: MapAuthoring.Editor
Editor EditMode assembly: MapAuthoring.Tests.EditMode
Runtime EditMode assembly: Game.Map.Tests.EditMode
New asmdef/asmref: NO
MAP07_11 Result SHA-256: 340cbed5424208ebeef144028c1806ea6a9039e8a6c14a5f39a824b042b062c6
MAP07_11 Task SHA-256: 1359b31bd70bd8288f86fb2d994267d480b7130a96a45e25541de1c05ba7e6ca
MAP07_11 CSV exporter model/API digest: abd090a627f295cc91593e49b78e2c7871ff3210c5ace87af43677027898f976
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
MAP07_11 acceptance: 8807/8807 PASS
MAP07_11 failed/skipped: 0/0
MAP07_11 compile/Console/relevant warnings: 0/0/0
Assets meta: 3400
Authoring CSV/meta: 50 / 50
Authoring manifest SHA-256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Generated CSV files created by MAP07_11: 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes by MAP07_11: 0
Duplicate GUID groups: 0
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

Reference는 debug 화면, reachability report, Authoring/generated ownership, and MAP07_13 exit ownership을 확인하는 용도다. Starter 전체 round-trip과 phase exit는 MAP07_13 소유이며 이 Task에서 요구하지 않는다.

## READ ALLOWLIST

### Existing MAP07 runtime contracts

```text
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/*
Assets/_Game/Map/Runtime/Data/Csv/*
Assets/_Game/Map/Runtime/Data/Definitions/*
Assets/_Game/Map/Runtime/Data/Registry/*
```

### Existing MAP07 editor state/import/export layer

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
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvExportRequest.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvExportIssue.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvExportPlan.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvExportResult.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvExporter.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvExportWindow.cs
Assets/_Game/Editor/MapAuthoring/MapAuthoring.Editor.asmdef
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/MapAuthoring.Tests.EditMode.asmdef
```

### Existing tests for style and boundary advance

```text
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks/MicrochunkCsvExporterTests.cs
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
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalAccessRuleAssignerTests.cs
```

위 파일과 matching meta, approved Runtime/Editor/Test path-only inventory, Authoring CSV/meta count and aggregate hash, full Assets meta GUID, patch marker 이후 change-scope path만 확인할 수 있다.

금지: MAP07_13 Task body, starter full round-trip body, MAP08+ body, sector assembly, boundary chunk body, Legacy/Stage/P6/P11 generator body, Scene/Prefab YAML.

## WRITE ALLOWLIST

### 신규 Editor production C# - exact 6

```text
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkPreviewRequest.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkPreviewIssue.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkPreviewCellOverlay.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkPreviewReport.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkPreviewBuilder.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkPreviewWindow.cs
```

### 신규 Editor EditMode tests - exact 1

```text
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks/MicrochunkPreviewAndReportTests.cs
```

### 기존 phase-boundary test 수정 - exact up to 18

MAP07_12 production symbols `MicrochunkPreviewBuilder`, `MicrochunkPreviewReport`, and `MicrochunkPreviewWindow`를 허용하고 MAP07_13+ future symbols 금지를 유지하기 위해 필요한 경우 위 Existing tests for style and boundary advance 목록의 existing test C#만 수정할 수 있다. matching `.cs.meta`는 수정하지 않는다.

### Result report - exact 1

```text
MapDesign/MCP/REPORTS/MAP07_12_CREATE_MICROCHUNK_PREVIEW_AND_REPORT_RESULT.md
```

## Required Implementation Contract

### Editor-only assembly boundary

- All production files for this Task must be under `Assets/_Game/Editor/MapAuthoring/Microchunks/`.
- All new tests for this Task must be under `Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks/`.
- New runtime C# is forbidden in this Task.
- New asmdef/asmref is forbidden. Use existing `MapAuthoring.Editor` and `MapAuthoring.Tests.EditMode` assemblies.
- Existing MAP07 runtime validators/probe/transformer are read-only services; do not rewrite their core rules in Editor code.

### Preview request and transform projection

- `MicrochunkPreviewRequest` contains exactly one selected microchunk ID, detached editor state, selected transform set, overlay toggles, and validation/report options.
- Selected ID must be canonical non-blank and must not be guessed from current UI selection.
- Supported transforms are exactly `R0`, `MIRROR_X`, `MIRROR_Y`, and `R180`.
- 90-degree rotation remains forbidden.
- Preview projection must produce deterministic 12x8 local coordinate output for every selected transform.
- Tile, socket, and object-slot overlays must use existing transform and validation contracts; no ad hoc coordinate math may contradict `MicrochunkTransformer`.

### Validation report

- `MicrochunkPreviewReport` aggregates deterministic issues from:
  - MAP07_02 tile-layer rules;
  - MAP07_06 96-cell validator;
  - MAP07_04 socket-edge validator;
  - MAP07_05 object-slot validator;
  - MAP07_07 reachability probe;
  - MAP07_03 transform validation.
- Each issue must include stable severity, code, message, selected microchunk ID, optional transform, and local coordinate when available.
- Report ordering must be deterministic: severity/order/category/code/transform/local coordinate/source order.
- The report must keep import/export issues as input diagnostics but must not mutate Authoring CSV or detached editor state.

### Reachability heatmap

- Reachability heatmap must be derived from the existing `MicrochunkReachabilityProbe`.
- Heatmap cells must distinguish at least unreachable, reachable, path witness, socket entry/exit, and blocked/solid when information is available.
- Mandatory socket-pair witnesses must be exposed in the report without changing probe semantics.
- Preview/report code must not implement world-level traversal, sector assembly traversal, or MAP13 validation search.

### Editor window behavior

- `MicrochunkPreviewWindow` shows selected ID, transform preview, overlay toggles, issue list, and coordinate detail.
- Preview generation must be explicit and deterministic; no auto-save and no source file mutation.
- Window may copy/report diagnostics in memory, but must not write generated CSV, Authoring CSV, Scene, Prefab, ProjectSettings, or package files.
- The UI must tolerate empty selection, invalid selected ID, incomplete 96-cell data, missing sockets, and validator failures without throwing.

## Required Tests

Run only task-relevant focused tests plus required MAP07/MAP06/MAP05 regression gates.

```text
MicrochunkPreviewAndReportTests >=520 PASS
MicrochunkCsvExporterTests 460/460 PASS
MicrochunkCsvImporterTests 420/420 PASS
MicrochunkSocketAndSlotEditorTests 380/380 PASS
MicrochunkAuthoringGridTests 320/320 PASS
MicrochunkReachabilityProbeTests 522/522 PASS
Existing MAP07 regression union 2000/2000 PASS
MAP06 required total 2746/2746 PASS
MAP05 required total 1959/1959 PASS
Actually executed required total >=9327 PASS
Required failed/skipped 0/0
Unity compile errors 0
Final Console errors/warnings 0/0
Relevant warnings 0
```

## Static Gates

```text
Assets meta 3400 -> 3407
New Editor production C#/matching meta 6/6
New Editor test C#/matching meta 1/1
New folder meta 0
New Runtime C#/matching meta 0/0
Task-local existing boundary test C# modified <=18
Matching existing boundary-test meta modified 0
Assets duplicate GUID groups 0

Authoring CSV/matching meta 50/50
Authoring manifest SHA-256 unchanged: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Authoring CSV tracked changes 0
Generated CSV files created 0

Scene/Prefab tracked changes 0/0
ProjectSettings/Packages tracked changes 0/0
asmdef/asmref tracked changes 0/0

MAP07_01~MAP07_11 production source changes 0
MAP06 production source changes 0
Forbidden MAP07_13+ production symbol hits 0
Unapplied MCP patches 0
```

## Forbidden This Task

- MAP07_13 starter full catalog round-trip.
- MAP07 phase exit approval.
- MAP08 boundary candidate/content work.
- Sector assembly, recipe resolver, world-level traversal, generated output writer.
- Runtime production C#.
- Authoring CSV mutation.
- Generated CSV output.
- Scene, Prefab, ProjectSettings, Packages, asmdef, asmref changes.
- Any Legacy/Stage/P6/P11 generator dependency as implementation base.

## Result Report Requirements

`REPORTS/MAP07_12_CREATE_MICROCHUNK_PREVIEW_AND_REPORT_RESULT.md` must include:

```text
TASK: MAP07_12_CREATE_MICROCHUNK_PREVIEW_AND_REPORT
STATUS: PASS | FAIL | BLOCKED
MAP07_12: COMPLETE ELIGIBLE only if PASS
MAP07_13_MAP07_STARTER_AND_EXIT_TESTS: LOCKED / DO NOT START
```

Required evidence:

- MAP07_11 Result SHA-256 `340cbed5424208ebeef144028c1806ea6a9039e8a6c14a5f39a824b042b062c6`.
- MAP07_11 Task SHA-256 `1359b31bd70bd8288f86fb2d994267d480b7130a96a45e25541de1c05ba7e6ca`.
- MAP07_12 Task SHA-256 from this file.
- Preview/report deterministic Editor model/API digest.
- Preserved MAP07_11 CSV exporter digest `abd090a627f295cc91593e49b78e2c7871ff3210c5ace87af43677027898f976`.
- All required test totals and static gates.
- Confirmation that MAP07_13+ was not read or started.
- Confirmation that Authoring CSV, generated CSV, Scene/Prefab, ProjectSettings/Packages, asmdef/asmref, and runtime production code were not changed.
