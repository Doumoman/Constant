# RUN MAP07_05

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP07_05_IMPLEMENT_OBJECT_SLOT_VALIDATION.md`, MAP07_04 PASS Result를 순서대로 읽어라.

Exact gates:

```text
MAP07_04 Result STATUS: PASS
MAP07_04 Result SHA-256: 90bb39103282ad08d031ee710802abdeba0adc4799c754ba73eaede4a2b7ade5
MAP07_04 Task SHA-256: a563b469ebcfe9bea8f7f280398f20aa4464fd2aed9ff5ac2000c60f773eb0a6
MAP07_05 Task SHA-256: 141ba64ee4fadee918c69daa94693a89aac21efb10d14f65576c04c4e66515fc
```

Current Task가 MAP07_05가 아니거나 어느 SHA라도 다르면 `BLOCKED`하고 변경하지 마. MAP07_06 이후 Task body는 읽거나 시작하지 마.

이번 Task는 object-slot semantic validation runtime model과 tests까지만 구현한다.

Allowed new writes:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkObjectSlotPoolDefinition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkObjectSlotValidationPolicy.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkObjectSlotValidationViolation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkObjectSlotValidationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkObjectSlotValidator.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkObjectSlotValidatorTests.cs
MapDesign/MCP/REPORTS/MAP07_05_IMPLEMENT_OBJECT_SLOT_VALIDATION_RESULT.md
```

Phase-boundary advance를 위해 Current Task의 exact up-to-17 existing test allowlist만 수정할 수 있다. matching existing meta는 수정하지 마.

Required facts:

```text
Object slot rows in starter package: 9
Starter categories: RESOURCE, MAP_ELEMENT, REWARD, EVENT_TRIGGER, NPC
Supported orientations: NONE, L, R, U, D
Starter required marker codes: M_SLOT_RESOURCE, M_SLOT_HAZARD, M_SLOT_EVENT, M_SAFE
Blocking slot-anchor and radius layers: GroundSolid, Breakable, Hazard, Liquid
Non-blocking for this task: DecorationBack, DecorationFront, Marker, OneWay, NONE
allowed_pool_id is validated against supplied in-memory pool policy only
visible_from_route does not run route visibility or reachability probes
Partial tile data may report missing anchor cells but must not become the 96-cell validator
```

Forbidden this task:

```text
Microchunk96CellValidator
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
MicrochunkObjectSlotValidatorTests >=300 PASS
MicrochunkSocketEdgeValidatorTests 332/332 PASS
MicrochunkTransformerTests 483/483 PASS
MicrochunkTileLayerRulesTests 150/150 PASS
MicrochunkDefinitionTests 146/146 PASS
MAP06 required total 2746/2746 PASS
MAP05 required total 1959/1959 PASS
Actually executed total >=6116 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
Assets meta 3350 -> 3356
new Runtime C#/meta 5/5
new Test C#/meta 1/1
new folder meta 0
Authoring CSV/meta 50/50 and manifest unchanged
Generated CSV 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes 0
MAP07_06+ forbidden production symbols 0
```

전부 PASS일 때만 MAP07_05 COMPLETE/Current Task NONE으로 finalize한다. `MAP07_06_IMPLEMENT_96_CELL_VALIDATOR`는 LOCKED로 유지하고 자동 시작하지 않는다.
