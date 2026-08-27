# RUN MAP07_04

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP07_04_IMPLEMENT_SOCKET_EDGE_VALIDATION.md`, MAP07_03 PASS Result를 순서대로 읽어라.

Exact gates:

```text
MAP07_03 Result STATUS: PASS
MAP07_03 Result SHA-256: 062206bf753f1dce3a9c6a43107e24090bf9abdc253fc9e69eec478a2fafa505
MAP07_03 repaired/current Task SHA-256: f9aee2e6fe0c0a3222eae894cb562ef2100813c4a91e16461fd03e5d5d4cb170
MAP07_04 Task SHA-256: a563b469ebcfe9bea8f7f280398f20aa4464fd2aed9ff5ac2000c60f773eb0a6
```

Current Task가 MAP07_04가 아니거나 어느 SHA라도 다르면 `BLOCKED`하고 변경하지 마. MAP07_05 이후 Task body는 읽거나 시작하지 마.

이번 Task는 socket-edge validation runtime model과 tests까지만 구현한다.

Allowed new writes:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkSocketBandDefinition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkEdgeSignatureDefinition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkSocketEdgeValidationViolation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkSocketEdgeValidationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkSocketEdgeValidator.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkSocketEdgeValidatorTests.cs
MapDesign/MCP/REPORTS/MAP07_04_IMPLEMENT_SOCKET_EDGE_VALIDATION_RESULT.md
```

Phase-boundary advance를 위해 Current Task의 exact up-to-17 existing test allowlist만 수정할 수 있다. matching existing meta는 수정하지 마.

Required facts:

```text
L/R sockets require HORIZONTAL_EDGE band/signature
U/D sockets require VERTICAL_EDGE band/signature
EDGE_SOLID/SOLID signature cannot be referenced by socket rows
Signature band/traversal/tool/mandatory fields must match socket fields
Socket minimum_safe_tiles must satisfy band minimum_clearance_tiles
Blocking clearance layers: GroundSolid, Breakable, Hazard, Liquid
Non-blocking for this task: DecorationBack, DecorationFront, Marker, OneWay, NONE
```

Forbidden this task:

```text
MicrochunkObjectSlotValidator
Microchunk96CellValidator
MicrochunkReachabilityProbe
MicrochunkAuthoringWindow
MicrochunkCsvImporter
MicrochunkCsvExporter
MicrochunkPreviewReport
generated CSV writer
Scene/Prefab/ProjectSettings/asmdef changes
```

Required actual gates:

```text
MicrochunkSocketEdgeValidatorTests >=260 PASS
MicrochunkTransformerTests 483/483 PASS
MicrochunkTileLayerRulesTests 150/150 PASS
MicrochunkDefinitionTests 146/146 PASS
MAP06 required total 2746/2746 PASS
MAP05 required total 1959/1959 PASS
Actually executed total >=5744 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
Assets meta 3344 -> 3350
new Runtime C#/meta 5/5
new Test C#/meta 1/1
new folder meta 0
Authoring CSV/meta 50/50 and manifest unchanged
Generated CSV 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes 0
MAP07_05+ forbidden production symbols 0
```

전부 PASS일 때만 MAP07_04 COMPLETE/Current Task NONE으로 finalize한다. `MAP07_05_IMPLEMENT_OBJECT_SLOT_VALIDATION`은 LOCKED로 유지하고 자동 시작하지 않는다.
