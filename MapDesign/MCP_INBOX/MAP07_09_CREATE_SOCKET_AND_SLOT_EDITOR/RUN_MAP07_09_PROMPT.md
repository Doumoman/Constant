# RUN MAP07_09

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP07_09_CREATE_SOCKET_AND_SLOT_EDITOR.md`, MAP07_08 PASS Result를 순서대로 읽어라.

Exact gates:

```text
MAP07_08 Result STATUS: PASS
MAP07_08 Result SHA-256: 3f0a2ec3c3f8668de33f180521a872a58a7cc7cb3ea11cb451dd5fcb640200d9
MAP07_08 Task SHA-256: 6d3b211b593743d9aebf6ba4f0c4fc9ef720d85139e9fe1e687231014ee00f29
MAP07_09 Task SHA-256: 5e870b792acdaff3ffb12058919f8973cd0fa50dcfd505b662c323f47a6f1a87
```

Current Task가 MAP07_09가 아니거나 어느 SHA라도 다르면 `BLOCKED`하고 변경하지 마. MAP07_10 이후 Task body는 읽거나 시작하지 마.

이번 Task는 Editor-only socket/band/signature and object-slot editor까지만 구현한다.

Allowed new writes:

```text
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkSocketAuthoringRow.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkSocketBandAuthoringRow.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkSocketAuthoringCollection.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkObjectSlotAuthoringRow.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkObjectSlotAuthoringCollection.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkSocketAndSlotEditorViewModel.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkSocketAndSlotEditorWindow.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks/MicrochunkSocketAndSlotEditorTests.cs
MapDesign/MCP/REPORTS/MAP07_09_CREATE_SOCKET_AND_SLOT_EDITOR_RESULT.md
```

Phase-boundary advance를 위해 Current Task의 exact up-to-17 existing test allowlist만 수정할 수 있다. matching existing meta는 수정하지 마.

Required facts:

```text
Socket side: L, R, D, U only
L/R band range: y 0..7 inclusive
D/U band range: x 0..11 inclusive
Default tool requirement: NONE
Object slot orientation: existing NONE/L/R/U/D contract
Validation feedback: existing MAP07_04 socket-edge validator and MAP07_05 object-slot validator only
```

Forbidden this task:

```text
MicrochunkCsvImporter
MicrochunkCsvExporter
MicrochunkPreviewReport
MicrochunkReachabilityHeatmap
MicrochunkStarterCatalogRoundTrip
generated CSV writer
Scene/Prefab/ProjectSettings/asmdef changes
runtime C# changes
```

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
Assets meta 3378 -> 3386
new Editor production C#/meta 7/7
new Editor test C#/meta 1/1
new folder meta 0
new Runtime C#/meta 0/0
Authoring CSV/meta 50/50 and manifest unchanged
Generated CSV 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes 0
MAP07_10+ forbidden production symbols 0
```

전부 PASS일 때만 MAP07_09 COMPLETE/Current Task NONE으로 finalize한다. `MAP07_10_IMPLEMENT_MICROCHUNK_CSV_IMPORT`는 LOCKED로 유지하고 자동 시작하지 않는다.
