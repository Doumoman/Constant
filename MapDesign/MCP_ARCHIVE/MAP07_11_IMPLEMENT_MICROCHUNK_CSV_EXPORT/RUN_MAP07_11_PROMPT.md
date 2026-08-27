# RUN MAP07_11

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP07_11_IMPLEMENT_MICROCHUNK_CSV_EXPORT.md`, MAP07_10 PASS Result를 순서대로 읽어라.

Exact gates:

```text
MAP07_10 Result STATUS: PASS
MAP07_10 Result SHA-256: 9bf311d95b4a16518d6e8dea296fd7694c30d225a719c394c91c9addc94c5d7b
MAP07_10 Task SHA-256: a21f95a87c1f962fed4672376d55eb740af6fa5d8b0aa8ec286ba782b2f54735
MAP07_11 Task SHA-256: 1359b31bd70bd8288f86fb2d994267d480b7130a96a45e25541de1c05ba7e6ca
```

Current Task가 MAP07_11이 아니거나 어느 SHA라도 다르면 `BLOCKED`하고 변경하지 마. MAP07_12 이후 Task body는 읽거나 시작하지 마.

이번 Task는 selected microchunk ID의 Authoring CSV export까지만 구현한다.

Allowed new writes:

```text
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvExportRequest.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvExportIssue.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvExportPlan.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvExportResult.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvExporter.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvExportWindow.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks/MicrochunkCsvExporterTests.cs
MapDesign/MCP/REPORTS/MAP07_11_IMPLEMENT_MICROCHUNK_CSV_EXPORT_RESULT.md
```

Phase-boundary advance를 위해 Current Task의 exact up-to-17 existing test allowlist만 수정할 수 있다. matching existing meta는 수정하지 마.

Required facts:

```text
Export target: Authoring CSV only
Selected export target: exactly one microchunk ID
microchunk_tile_cells selected ID rows: exactly 96
Selected ID rows replace old rows atomically
UTF-8 BOM required
RFC4180 escaping required
Stable sort: schema primary key based
Plan generation: deterministic and side-effect-free
Shared socket-band rows remain unchanged when not selected-ID owned
```

Forbidden this task:

```text
MicrochunkPreviewReport
MicrochunkReachabilityHeatmap
MicrochunkStarterCatalogRoundTrip
sector assembly
world-level traversal
generated CSV writer
Scene/Prefab/ProjectSettings/asmdef changes
runtime C# changes
MAP07_12+ production symbols
```

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
Assets meta 3393 -> 3400
new Editor production C#/meta 6/6
new Editor test C#/meta 1/1
new folder meta 0
new Runtime C#/meta 0/0
Authoring CSV/meta 50/50 and manifest unchanged unless temp-fixture-only path used
Task-local Authoring source tracked changes 0
Generated CSV 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes 0
Forbidden MAP07_12+ production hits 0
```

전부 PASS일 때만 MAP07_11 COMPLETE/Current Task NONE으로 finalize한다. `MAP07_12_CREATE_MICROCHUNK_PREVIEW_AND_REPORT`는 LOCKED로 유지하고 자동 시작하지 않는다.
