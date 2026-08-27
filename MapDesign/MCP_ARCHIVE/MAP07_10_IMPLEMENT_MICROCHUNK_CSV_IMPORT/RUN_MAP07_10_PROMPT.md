# RUN MAP07_10

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP07_10_IMPLEMENT_MICROCHUNK_CSV_IMPORT.md`, MAP07_09 PASS Result를 순서대로 읽어라.

Exact gates:

```text
MAP07_09 Result STATUS: PASS
MAP07_09 Result SHA-256: 7bc550e92359f4f24c642b24000be1e1a8198fdeb014ce1685555bf5f83a0340
MAP07_09 Task SHA-256: 5e870b792acdaff3ffb12058919f8973cd0fa50dcfd505b662c323f47a6f1a87
MAP07_10 Task SHA-256: a21f95a87c1f962fed4672376d55eb740af6fa5d8b0aa8ec286ba782b2f54735
```

Current Task가 MAP07_10이 아니거나 어느 SHA라도 다르면 `BLOCKED`하고 변경하지 마. MAP07_11 이후 Task body는 읽거나 시작하지 마.

이번 Task는 selected microchunk ID의 Authoring CSV import까지만 구현한다.

Allowed new writes:

```text
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvImportSource.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvImportRequest.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvImportIssue.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvImportResult.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvImporter.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkCsvImportWindow.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks/MicrochunkCsvImporterTests.cs
MapDesign/MCP/REPORTS/MAP07_10_IMPLEMENT_MICROCHUNK_CSV_IMPORT_RESULT.md
```

Phase-boundary advance를 위해 Current Task의 exact up-to-17 existing test allowlist만 수정할 수 있다. matching existing meta는 수정하지 마.

Required facts:

```text
Input source: Authoring CSV only
Selected import target: exactly one microchunk ID
Complete tile data: exactly 96 unique in-bounds cells
Non-complete catalog row: import 96 editor cells with missing cells as NONE and deterministic issues
Import hydrates existing grid and socket/slot editor state
CSV export and source row replacement are forbidden
```

Forbidden this task:

```text
MicrochunkCsvExporter
MicrochunkCsvExportWindow
MicrochunkPreviewReport
MicrochunkReachabilityHeatmap
MicrochunkStarterCatalogRoundTrip
generated CSV writer
Scene/Prefab/ProjectSettings/asmdef changes
runtime C# changes
```

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
Assets meta 3386 -> 3393
new Editor production C#/meta 6/6
new Editor test C#/meta 1/1
new folder meta 0
new Runtime C#/meta 0/0
Authoring CSV/meta 50/50 and manifest unchanged
Authoring CSV tracked changes 0
Generated CSV 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes 0
MAP07_11+ forbidden production symbols 0
```

전부 PASS일 때만 MAP07_10 COMPLETE/Current Task NONE으로 finalize한다. `MAP07_11_IMPLEMENT_MICROCHUNK_CSV_EXPORT`는 LOCKED로 유지하고 자동 시작하지 않는다.
