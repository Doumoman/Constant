# RUN MAP07_02

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP07_02_IMPLEMENT_TILE_LAYER_RULES.md`, MAP07_01 PASS Result를 순서대로 읽어라.

Exact gates:

```text
MAP07_01 Result STATUS: PASS
MAP07_01 Result SHA-256: b11e740b808effe5a528a68497527edd0ab92fcc8c1a823dd6baa0d39363f474
MAP07_01 Task SHA-256: 912028220492f7e9dff40db93711dd590dcd73531131d133cd0270c4862d368c
MAP07_02 Task SHA-256: 0b69d8f46654bd2af5e441d603210a1889351cff478b688a23b6b87c697ea9c7
```

Current Task가 MAP07_02가 아니거나 어느 SHA라도 다르면 `BLOCKED`하고 변경하지 마. MAP07_03 이후 Task body는 읽거나 시작하지 마.

이번 Task는 tile-layer compatibility rule matrix와 tests까지만 구현한다.

Allowed new writes:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTileLayerOccupancy.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTileLayerRuleViolation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTileLayerRuleResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTileLayerRules.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkTileLayerRulesTests.cs
MapDesign/MCP/REPORTS/MAP07_02_IMPLEMENT_TILE_LAYER_RULES_RESULT.md
```

Phase-boundary advance를 위해 Current Task의 exact up-to-15 existing test allowlist만 수정할 수 있다. matching existing meta는 수정하지 마.

Required facts:

```text
MAP07_01 model digest = 673f8a5057a28e6b2dbceac1a43f4eee4b30f0ec2a3738939107759e229cb7d5
MicrochunkDefinitionTests = 146/146 PASS
MAP07_01 acceptance = 4851/4851 PASS
Assets meta baseline = 3334
Authoring manifest = 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Allowed: single layer, DecorationBack/Front with all, DecorationBack+DecorationFront, Ground/OneWay/Breakable/Hazard + Marker, Marker alone, empty cell
Forbidden: Ground+Breakable, Ground+OneWay, Breakable+OneWay, Ground+Liquid, Breakable+Liquid, Hazard+blocking/liquid, Liquid+Marker, Liquid+OneWay, all unlisted non-decoration pairs
```

Forbidden this task:

```text
MicrochunkTransformer
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
MicrochunkTileLayerRulesTests >=128 PASS
MicrochunkDefinitionTests 146/146 PASS
MAP06 aggregate 2746/2746 PASS
MAP05 aggregate 1959/1959 PASS
Actually executed total >=4979 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
Assets meta 3334 -> 3339
new Runtime C#/meta 4/4
new Test C#/meta 1/1
new folder meta 0
Authoring CSV/meta 50/50 and manifest unchanged
Generated CSV 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes 0
MAP07_03+ forbidden production symbols 0
```

전부 PASS일 때만 MAP07_02 COMPLETE/Current Task NONE으로 finalize한다. `MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS`는 LOCKED로 유지하고 자동 시작하지 않는다.
