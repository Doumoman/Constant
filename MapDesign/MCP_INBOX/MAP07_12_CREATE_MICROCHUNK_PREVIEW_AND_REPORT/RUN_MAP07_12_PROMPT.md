# RUN MAP07_12

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP07_12_CREATE_MICROCHUNK_PREVIEW_AND_REPORT.md`, MAP07_11 PASS Result를 순서대로 읽어라.

Exact gates:

```text
MAP07_11 Result STATUS: PASS
MAP07_11 Result SHA-256: 340cbed5424208ebeef144028c1806ea6a9039e8a6c14a5f39a824b042b062c6
MAP07_11 Task SHA-256: 1359b31bd70bd8288f86fb2d994267d480b7130a96a45e25541de1c05ba7e6ca
MAP07_12 Task SHA-256: 73544122f13653fa3762e87fdc75b7a415482b7336e0ac3fe3a75760d51ec9b0
```

Current Task가 MAP07_12가 아니거나 어느 SHA라도 다르면 `BLOCKED`하고 변경하지 마. MAP07_13 이후 Task body는 읽거나 시작하지 마.

이번 Task는 selected microchunk ID의 transform preview, validation report, reachability heatmap까지만 구현한다.

Allowed new writes:

```text
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkPreviewRequest.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkPreviewIssue.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkPreviewCellOverlay.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkPreviewReport.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkPreviewBuilder.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkPreviewWindow.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks/MicrochunkPreviewAndReportTests.cs
MapDesign/MCP/REPORTS/MAP07_12_CREATE_MICROCHUNK_PREVIEW_AND_REPORT_RESULT.md
```

Phase-boundary advance를 위해 Current Task의 exact up-to-18 existing test allowlist만 수정할 수 있다. matching existing meta는 수정하지 마.

Required facts:

```text
Preview target: exactly one selected microchunk ID
Supported transforms: R0 / MIRROR_X / MIRROR_Y / R180
90-degree rotation remains forbidden
Report aggregation: tile-layer / 96-cell / socket-edge / object-slot / reachability / transform
Reachability heatmap derives from existing MicrochunkReachabilityProbe
Issue ordering is deterministic
Window is explicit preview/report only
```

Forbidden this task:

```text
MAP07_13 starter full catalog round-trip
MAP07 phase exit approval
MAP08+ work
sector assembly
world-level traversal
generated CSV writer
Authoring CSV mutation
Scene/Prefab/ProjectSettings/asmdef changes
runtime C# changes
```

Required actual gates:

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
Actually executed total >=9327 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
Assets meta 3400 -> 3407
new Editor production C#/meta 6/6
new Editor test C#/meta 1/1
new folder meta 0
new Runtime C#/meta 0/0
Authoring CSV/meta 50/50 and manifest unchanged
Authoring CSV tracked changes 0
Generated CSV 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes 0
Forbidden MAP07_13+ production symbol hits 0
```

전부 PASS일 때만 MAP07_12 COMPLETE/Current Task NONE으로 finalize한다. `MAP07_13_MAP07_STARTER_AND_EXIT_TESTS`는 LOCKED로 유지하고 자동 시작하지 않는다.
