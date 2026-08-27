# RUN MAP07_03

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS.md`, MAP07_02 PASS Result를 순서대로 읽어라.

Exact gates:

```text
MAP07_02 Result STATUS: PASS
MAP07_02 Result SHA-256: 98240add84d955ffdc50c3e22e18eb3a0255d9a1d397e9d6c2039e2488dafc4e
MAP07_02 repaired/current Task SHA-256: c9cb155bdb0b9f2d047b8305c35f32392d691988f612bc107849d0a9f3292edb
MAP07_03 Task SHA-256: 82434805780000e3695cbdda45d5888c4234ba617bdc5bcded843643b4c7aac8
```

Current Task가 MAP07_03이 아니거나 어느 SHA라도 다르면 `BLOCKED`하고 변경하지 마. MAP07_04 이후 Task body는 읽거나 시작하지 마.

이번 Task는 microchunk transform runtime model과 tests까지만 구현한다.

Allowed new writes:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTransformOptions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTransformUtility.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTransformResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTransformer.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkTransformerTests.cs
MapDesign/MCP/REPORTS/MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS_RESULT.md
```

Phase-boundary advance를 위해 Current Task의 exact up-to-17 existing test allowlist만 수정할 수 있다. matching existing meta는 수정하지 마.

Required transform facts:

```text
R0:       (x, y) -> (x, y)
MIRROR_X: (x, y) -> (11 - x, y), L/R swap only
MIRROR_Y: (x, y) -> (x, 7 - y), U/D swap only
R180:     (x, y) -> (11 - x, 7 - y), L/R and U/D swap
R90/R270/arbitrary 90-degree rotation: forbidden
Default tile-code behavior: preserve exact IDs, including NONE
Default socket-band behavior: preserve exact band_id
Optional remappers: allowed only when explicit and deterministic
```

Forbidden this task:

```text
MicrochunkSocketEdgeValidator
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
MicrochunkTransformerTests >=180 PASS
MicrochunkTileLayerRulesTests 150/150 PASS
MicrochunkDefinitionTests 146/146 PASS
MAP06 required total 2746/2746 PASS
MAP05 required total 1959/1959 PASS
Actually executed total >=5181 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
Assets meta 3339 -> 3344
new Runtime C#/meta 4/4
new Test C#/meta 1/1
new folder meta 0
Authoring CSV/meta 50/50 and manifest unchanged
Generated CSV 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes 0
MAP07_04+ forbidden production symbols 0
```

전부 PASS일 때만 MAP07_03 COMPLETE/Current Task NONE으로 finalize한다. `MAP07_04_IMPLEMENT_SOCKET_EDGE_VALIDATION`은 LOCKED로 유지하고 자동 시작하지 않는다.
