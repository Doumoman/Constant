# RUN MAP07_08

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP07_08_CREATE_MICROCHUNK_AUTHORING_GRID.md`, MAP07_07 PASS Result를 순서대로 읽어라.

Exact gates:

```text
MAP07_07 Result STATUS: PASS
MAP07_07 Result SHA-256: afaf3f058c34457d26491b15c06858ba1c1c7355cf14d5902d65f66a43a1fa19
MAP07_07 Task SHA-256: 0d9ec87691cf31db249b2fed7b411ea6b69a1d8c456469672c96999145add103
MAP07_08 Task SHA-256: 6d3b211b593743d9aebf6ba4f0c4fc9ef720d85139e9fe1e687231014ee00f29
```

Current Task가 MAP07_08이 아니거나 어느 SHA라도 다르면 `BLOCKED`하고 변경하지 마. MAP07_09 이후 Task body는 읽거나 시작하지 마.

이번 Task는 Editor-only 12x8 authoring grid와 8-layer painting state/window까지만 구현한다.

Allowed new writes:

```text
Assets/_Game/Editor/MapAuthoring/Microchunks/
Assets/_Game/Editor/MapAuthoring/Microchunks.meta
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkAuthoringGridCell.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkAuthoringGridLayer.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkAuthoringGridState.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkAuthoringGridPalette.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkAuthoringGridViewModel.cs
Assets/_Game/Editor/MapAuthoring/Microchunks/MicrochunkAuthoringGridWindow.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks/
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks.meta
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks/MicrochunkAuthoringGridTests.cs
MapDesign/MCP/REPORTS/MAP07_08_CREATE_MICROCHUNK_AUTHORING_GRID_RESULT.md
```

Phase-boundary advance를 위해 Current Task의 exact up-to-17 existing test allowlist만 수정할 수 있다. matching existing meta는 수정하지 마.

Required facts:

```text
Grid dimensions: 12x8 = 96
Layer order: GroundSolid, OneWay, Breakable, Hazard, Liquid, DecorationBack, DecorationFront, Marker
Empty layer value: NONE
Projection emits all 96 cells including all-NONE cells
Painting updates only the selected layer
Validation feedback may use only existing tile-layer and 96-cell validators
```

Forbidden this task:

```text
MicrochunkSocketAndSlotEditor
MicrochunkSocketEditor
MicrochunkSlotEditor
MicrochunkCsvImporter
MicrochunkCsvExporter
MicrochunkPreviewReport
MicrochunkReachabilityHeatmap
generated CSV writer
Scene/Prefab/ProjectSettings/asmdef changes
runtime C# changes
```

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
Assets meta 3369 -> 3378
new Editor production folder/meta 1/1
new Editor production C#/meta 6/6
new Editor test folder/meta 1/1
new Editor test C#/meta 1/1
new Runtime C#/meta 0/0
Authoring CSV/meta 50/50 and manifest unchanged
Generated CSV 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes 0
MAP07_09+ forbidden production symbols 0
```

전부 PASS일 때만 MAP07_08 COMPLETE/Current Task NONE으로 finalize한다. `MAP07_09_CREATE_SOCKET_AND_SLOT_EDITOR`는 LOCKED로 유지하고 자동 시작하지 않는다.
