# RUN MAP07_06

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP07_06_IMPLEMENT_96_CELL_VALIDATOR.md`, MAP07_05 PASS Result를 순서대로 읽어라.

Exact gates:

```text
MAP07_05 Result STATUS: PASS
MAP07_05 Result SHA-256: 4d805c6ff1702e4e8ecea3be7a337584e4e2856b7d5106d51d1e42c31954029c
MAP07_05 Task SHA-256: 141ba64ee4fadee918c69daa94693a89aac21efb10d14f65576c04c4e66515fc
MAP07_06 Task SHA-256: 38a601ca63dff23622564cf36b3c02aa2f55849808c69b3a58bf60d2a8d7c6fa
```

Current Task가 MAP07_06이 아니거나 어느 SHA라도 다르면 `BLOCKED`하고 변경하지 마. MAP07_07 이후 Task body는 읽거나 시작하지 마.

이번 Task는 96-cell coverage validation runtime model과 tests까지만 구현한다.

Allowed new writes:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/Microchunk96CellRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/Microchunk96CellValidationPolicy.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/Microchunk96CellValidationViolation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/Microchunk96CellValidationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/Microchunk96CellValidator.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/Microchunk96CellValidatorTests.cs
MapDesign/MCP/REPORTS/MAP07_06_IMPLEMENT_96_CELL_VALIDATOR_RESULT.md
```

Phase-boundary advance를 위해 Current Task의 exact up-to-17 existing test allowlist만 수정할 수 있다. matching existing meta는 수정하지 마.

Required facts:

```text
Microchunk dimensions: 12x8 = 96
Valid local_x range: 0..11
Valid local_y range: 0..7
tile_data_complete=1 requires exactly 96 coordinate rows
Empty cells must be explicit NONE rows, not omitted sparse rows
Duplicate and out-of-range rows never satisfy missing coordinates
Partial/draft policy may allow missing rows but still rejects duplicate and out-of-range rows
```

Forbidden this task:

```text
MicrochunkReachabilityProbe
MicrochunkAuthoringWindow
MicrochunkSocketAndSlotEditor
MicrochunkCsvImporter
MicrochunkCsvExporter
MicrochunkPreviewReport
generated CSV writer
Scene/Prefab/ProjectSettings/asmdef changes
```

Required actual gates:

```text
Microchunk96CellValidatorTests >=384 PASS
MicrochunkObjectSlotValidatorTests 483/483 PASS
MicrochunkSocketEdgeValidatorTests 332/332 PASS
MicrochunkTransformerTests 483/483 PASS
MicrochunkTileLayerRulesTests 150/150 PASS
MicrochunkDefinitionTests 146/146 PASS
MAP06 required total 2746/2746 PASS
MAP05 required total 1959/1959 PASS
Actually executed total >=6683 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
Assets meta 3356 -> 3362
new Runtime C#/meta 5/5
new Test C#/meta 1/1
new folder meta 0
Authoring CSV/meta 50/50 and manifest unchanged
Generated CSV 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes 0
MAP07_07+ forbidden production symbols 0
```

전부 PASS일 때만 MAP07_06 COMPLETE/Current Task NONE으로 finalize한다. `MAP07_07_IMPLEMENT_MICROCHUNK_REACHABILITY_PROBE`는 LOCKED로 유지하고 자동 시작하지 않는다.
