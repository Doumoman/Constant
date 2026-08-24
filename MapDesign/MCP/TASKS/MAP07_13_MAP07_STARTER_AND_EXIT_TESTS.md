# MAP07_13 - MAP07 Starter And Exit Tests

```yaml
status_control:
  task_key: MAP07_13_MAP07_STARTER_AND_EXIT_TESTS
  result_file: REPORTS/MAP07_13_MAP07_STARTER_AND_EXIT_TESTS_RESULT.md
```

## TASK TYPE

```text
MAP07 STARTER CATALOG FULL ROUND-TRIP + PHASE EXIT AUDIT TESTS ONLY
```

## Objective

MAP07_12 PASS/finalize 뒤 MAP07 전체의 starter microchunk catalog, 96-cell completeness, transform/socket/object-slot/reachability validation, import-preview-export round-trip, Authoring CSV preservation, and MAP07 phase exit gates를 검증한다.

이 Task는 MAP07 마지막 gate다. 신규 production code는 만들지 않는다. MAP08 boundary content, sector assembly, generated CSV writer, runtime world traversal, Scene/Prefab/ProjectSettings changes는 구현하지 않는다. MAP08_01 이후 Task body는 읽거나 시작하지 않는다.

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
12. `REPORTS/MAP07_12_CREATE_MICROCHUNK_PREVIEW_AND_REPORT_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP07_12_CREATE_MICROCHUNK_PREVIEW_AND_REPORT
STATUS: PASS
MAP07_12: COMPLETE ELIGIBLE
MAP07_13_MAP07_STARTER_AND_EXIT_TESTS: LOCKED / DO NOT START
SHA-256: 869e5e640495e1ec4f7e376133d2525c9e0efe669296e949c7fe7b7d37c92876
```

이 별도 patch가 적용된 뒤에만 MAP07_13을 실행한다. MAP08_01 이후 Task body는 읽거나 시작하지 않는다.

## Approved Input Baseline

```text
Unity: 6000.3.8f1
Runtime assembly: Game.Map.Runtime
Runtime namespace boundary: StarNight.Map.WorldGeneration.*
Editor assembly: MapAuthoring.Editor
Editor EditMode assembly: MapAuthoring.Tests.EditMode
Runtime EditMode assembly: Game.Map.Tests.EditMode
New asmdef/asmref: NO
MAP07_12 Result SHA-256: 869e5e640495e1ec4f7e376133d2525c9e0efe669296e949c7fe7b7d37c92876
MAP07_12 Task SHA-256: 73544122f13653fa3762e87fdc75b7a415482b7336e0ac3fe3a75760d51ec9b0
MAP07_12 preview/report model/API digest: 4545e7962dc4da03ec04fe57d3b90d28bb60c50474a8c6d93b63eb392168191b
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
MAP07_12 acceptance: 9327/9327 PASS
MAP07_12 failed/skipped: 0/0
MAP07_12 compile/Console/relevant warnings: 0/0/0
Assets meta: 3407
Authoring CSV/meta: 50 / 50
Authoring manifest SHA-256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Generated CSV files created by MAP07_12: 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes by MAP07_12: 0
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
06_CHECKLISTS/PIPELINE_EXIT_CRITERIA.md
06_CHECKLISTS/VALIDATOR_MATRIX.md
```

Reference는 starter coverage, Authoring/generated ownership, and MAP07 exit criteria를 확인하는 용도다. MAP08 boundary content는 읽지 않는다.

## READ ALLOWLIST

### Existing MAP07 runtime/editor contracts

```text
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/*
Assets/_Game/Map/Runtime/Data/Csv/*
Assets/_Game/Map/Runtime/Data/Definitions/*
Assets/_Game/Map/Runtime/Data/Registry/*
Assets/_Game/Editor/MapAuthoring/Microchunks/*
Assets/_Game/Editor/MapAuthoring/MapAuthoring.Editor.asmdef
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/MapAuthoring.Tests.EditMode.asmdef
```

### Existing MAP07 tests

```text
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks/MicrochunkPreviewAndReportTests.cs
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

금지: MAP08_01+ Task body, sector assembly, boundary chunk body, Legacy/Stage/P6/P11 generator body, Scene/Prefab YAML.

## WRITE ALLOWLIST

### 신규 Editor EditMode tests - exact 2

```text
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks/MicrochunkStarterCatalogRoundTripTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks/Map07ExitTests.cs
```

### 기존 phase-boundary test 수정 - exact up to 18

MAP07_13 test symbols를 허용하고 MAP08+ future symbols 금지를 유지하기 위해 필요한 경우 Existing MAP07 tests 목록의 existing test C#만 수정할 수 있다. matching `.cs.meta`는 수정하지 않는다.

### Result report - exact 1

```text
MapDesign/MCP/REPORTS/MAP07_13_MAP07_STARTER_AND_EXIT_TESTS_RESULT.md
```

## Required Implementation Contract

### Test-only boundary

- New production C# is forbidden in this Task.
- New runtime C# is forbidden in this Task.
- New editor production C# is forbidden in this Task.
- New asmdef/asmref is forbidden.
- Tests may create temp-folder copies of Authoring CSV for round-trip verification, but must not mutate project Authoring CSV.
- The real Authoring source must remain `50 CSV / 50 matching meta` with unchanged manifest SHA.

### Starter catalog full validation

- Every starter microchunk catalog row must be validated against tile cells, sockets, socket-band definitions, object slots, and variants.
- Complete tile data chunks must have exactly 96 unique in-bounds cells.
- Missing/duplicate/out-of-range local cells must fail the starter audit.
- Every allowed transform (`R0`, `MIRROR_X`, `MIRROR_Y`, `R180`) must preserve tile/socket/object-slot validity.
- 90-degree rotation remains forbidden.
- Mandatory socket pairs must be checked through the existing reachability probe.

### Round-trip and preview/export integration

- Import selected starter microchunk rows into detached editor state.
- Build preview/report for each selected starter sample without source mutation.
- Export through temp-folder Authoring copies and verify exact selected-ID row replacement.
- Re-import exported temp output and compare normalized editor state.
- UTF-8 BOM, RFC4180 escaping, schema headers, stable sort, and exactly 96 exported tile rows must be covered.
- The audit must preserve shared/global socket-band rows unless selected-ID ownership is explicit.

### MAP07 phase exit

- MAP07 exit is approved only if all focused MAP07 gates, MAP06 required gates, MAP05 required gates, compile/Console/static/change-scope gates, and forbidden-symbol gates pass.
- MAP08 remains locked after MAP07_13 PASS/finalize; MAP08_01 must require a separate patch.

## Required Tests

Run only task-relevant focused tests plus required MAP07/MAP06/MAP05 regression gates.

```text
MicrochunkStarterCatalogRoundTripTests >=620 PASS
Map07ExitTests >=180 PASS
MicrochunkPreviewAndReportTests 520/520 PASS
MicrochunkCsvExporterTests 460/460 PASS
MicrochunkCsvImporterTests 420/420 PASS
MicrochunkSocketAndSlotEditorTests 380/380 PASS
MicrochunkAuthoringGridTests 320/320 PASS
MicrochunkReachabilityProbeTests 522/522 PASS
Existing MAP07 regression union 2000/2000 PASS
MAP06 required total 2746/2746 PASS
MAP05 required total 1959/1959 PASS
Actually executed required total >=10127 PASS
Required failed/skipped 0/0
Unity compile errors 0
Final Console errors/warnings 0/0
Relevant warnings 0
```

## Static Gates

```text
Assets meta 3407 -> 3409
New production C#/matching meta 0/0
New Editor test C#/matching meta 2/2
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

MAP07_01~MAP07_12 production source changes 0
MAP06 production source changes 0
Forbidden MAP08+ production symbol hits 0
Unapplied MCP patches 0
```

## Forbidden This Task

- MAP08 boundary pair/content work.
- MAP09 sector assembly or recipe resolver.
- World-level traversal, MAP13 validation search, generated output writer.
- Runtime production C#.
- Editor production C#.
- Authoring CSV mutation outside temp fixtures.
- Generated CSV output.
- Scene, Prefab, ProjectSettings, Packages, asmdef, asmref changes.
- Any Legacy/Stage/P6/P11 generator dependency as implementation base.

## Result Report Requirements

`REPORTS/MAP07_13_MAP07_STARTER_AND_EXIT_TESTS_RESULT.md` must include:

```text
TASK: MAP07_13_MAP07_STARTER_AND_EXIT_TESTS
STATUS: PASS | FAIL | BLOCKED
MAP07_13: COMPLETE ELIGIBLE only if PASS
MAP07 PHASE EXIT: APPROVED only if PASS
MAP08_01_DEFINE_MOONPALACE_BIOME_PAIRS: LOCKED / DO NOT START
```

Required evidence:

- MAP07_12 Result SHA-256 `869e5e640495e1ec4f7e376133d2525c9e0efe669296e949c7fe7b7d37c92876`.
- MAP07_12 Task SHA-256 `73544122f13653fa3762e87fdc75b7a415482b7336e0ac3fe3a75760d51ec9b0`.
- MAP07_13 Task SHA-256 from this file.
- Starter round-trip and MAP07 exit test totals.
- Preserved MAP07_12 preview/report digest `4545e7962dc4da03ec04fe57d3b90d28bb60c50474a8c6d93b63eb392168191b`.
- All required test totals and static gates.
- Confirmation that MAP08+ was not read or started.
- Confirmation that Authoring CSV, generated CSV, Scene/Prefab, ProjectSettings/Packages, asmdef/asmref, runtime production code, and editor production code were not changed.
