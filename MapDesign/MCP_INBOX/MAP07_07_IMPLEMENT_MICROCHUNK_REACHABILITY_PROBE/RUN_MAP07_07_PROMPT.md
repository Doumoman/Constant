# RUN MAP07_07

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP07_07_IMPLEMENT_MICROCHUNK_REACHABILITY_PROBE.md`, MAP07_06 PASS Result를 순서대로 읽어라.

Exact gates:

```text
MAP07_06 Result STATUS: PASS
MAP07_06 Result SHA-256: 81681d92aac6bff244dc7f655014c89cabb43baa178b3355fe701c6046b1a6e0
MAP07_06 Task SHA-256: 38a601ca63dff23622564cf36b3c02aa2f55849808c69b3a58bf60d2a8d7c6fa
MAP07_07 Task SHA-256: 0d9ec87691cf31db249b2fed7b411ea6b69a1d8c456469672c96999145add103
```

Current Task가 MAP07_07이 아니거나 어느 SHA라도 다르면 `BLOCKED`하고 변경하지 마. MAP07_08 이후 Task body는 읽거나 시작하지 마.

이번 Task는 local microchunk reachability probe runtime model과 tests까지만 구현한다.

Allowed new writes:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTraversalNode.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTraversalEdge.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkReachabilityPolicy.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkReachabilityViolation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkReachabilityResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkReachabilityProbe.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkReachabilityProbeTests.cs
MapDesign/MCP/REPORTS/MAP07_07_IMPLEMENT_MICROCHUNK_REACHABILITY_PROBE_RESULT.md
```

Phase-boundary advance를 위해 Current Task의 exact up-to-17 existing test allowlist만 수정할 수 있다. matching existing meta는 수정하지 마.

Required facts:

```text
Mandatory no-tool socket: mandatory_allowed=true and tool_requirement=NONE
Socket entry cells derive from side plus supplied band definition
L/R entry uses x=0 or x=11 and horizontal band y range
D/U entry uses y=0 or y=7 and vertical band x range
Coverage gate must pass before path success
Blocking layers: GroundSolid, Breakable, Hazard, Liquid
Non-blocking: OneWay, DecorationBack, DecorationFront, Marker, NONE
Movement kinds: FLOOD, WALK, JUMP, DROP, CLIMB, SOCKET_ENTRY
```

Forbidden this task:

```text
MicrochunkAuthoringWindow
MicrochunkAuthoringGrid
MicrochunkSocketAndSlotEditor
MicrochunkCsvImporter
MicrochunkCsvExporter
MicrochunkPreviewReport
generated CSV writer
Scene/Prefab/ProjectSettings/asmdef changes
```

Required actual gates:

```text
MicrochunkReachabilityProbeTests >=480 PASS
Microchunk96CellValidatorTests 406/406 PASS
MicrochunkObjectSlotValidatorTests 483/483 PASS
MicrochunkSocketEdgeValidatorTests 332/332 PASS
MicrochunkTransformerTests 483/483 PASS
MicrochunkTileLayerRulesTests 150/150 PASS
MicrochunkDefinitionTests 146/146 PASS
MAP06 required total 2746/2746 PASS
MAP05 required total 1959/1959 PASS
Actually executed total >=7185 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
Assets meta 3362 -> 3369
new Runtime C#/meta 6/6
new Test C#/meta 1/1
new folder meta 0
Authoring CSV/meta 50/50 and manifest unchanged
Generated CSV 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes 0
MAP07_08+ forbidden production symbols 0
```

전부 PASS일 때만 MAP07_07 COMPLETE/Current Task NONE으로 finalize한다. `MAP07_08_CREATE_MICROCHUNK_AUTHORING_GRID`는 LOCKED로 유지하고 자동 시작하지 않는다.
