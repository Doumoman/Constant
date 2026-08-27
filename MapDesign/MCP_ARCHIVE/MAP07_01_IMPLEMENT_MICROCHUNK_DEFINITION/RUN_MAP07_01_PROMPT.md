# RUN MAP07_01

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP07_01_IMPLEMENT_MICROCHUNK_DEFINITION.md`, MAP06_10 PASS Result를 순서대로 읽어라.

Exact gates:

```text
MAP06_10 Result STATUS: PASS
MAP06 PHASE EXIT: APPROVED
MAP06_10 Result SHA-256: 690a7cef9dbf1d22416e38b3675d76b0ef758062de2425e8e4841381f0d9bdeb
MAP06_10 repaired Task SHA-256: 623da5aaf2f8c72dd830fb5f859c4b05a631a93b7f4fa2a3aa67adc823f95cdb
MAP07_01 Task SHA-256: 912028220492f7e9dff40db93711dd590dcd73531131d133cd0270c4862d368c
```

Current Task가 MAP07_01이 아니거나 어느 SHA라도 다르면 `BLOCKED`하고 변경하지 마. MAP07_02 이후 Task body는 읽거나 시작하지 마.

이번 Task는 12x8 microchunk definition model과 structural tests까지만 구현한다.

Allowed new writes:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkLocalCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTileCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkSocketDefinition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkObjectSlotDefinition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkDefinition.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkDefinitionTests.cs
MapDesign/MCP/REPORTS/MAP07_01_IMPLEMENT_MICROCHUNK_DEFINITION_RESULT.md
```

If missing, exact folder metas may also be created:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks.meta
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks.meta
```

Phase-boundary advance를 위해 Current Task의 exact up-to-15 existing test allowlist만 수정할 수 있다. matching existing meta는 수정하지 마.

Required facts:

```text
microchunk size = 12 x 8 = 96
layer count = 8
starter catalog/tile_cells/sockets/object_slots = 14/1344/25/9
complete definitions require 96 unique local coords
partial definitions require TileDataComplete=false
NONE layer code is data, not omission
MAP06 overlay digest = 9cbd3833bf5e8b771f46cc3442c1c60d63493d8ffb7e8aa3c9a701f6a097fefd
Authoring manifest = 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
```

Forbidden this task:

```text
MicrochunkTileLayerRules
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
MicrochunkDefinitionTests >=128 PASS
Map06ExitTests 180/180 PASS
OptionalRegionOverlayTests 180/180 PASS
OptionalRegionOverlaySceneDrawerTests 40/40 PASS
OptionalRegionValidatorTests 321/321 PASS
InactiveBufferAssignerTests 281/281 PASS
OptionalReturnPolicyResolverTests 289/289 PASS
OptionalRewardTierCalculatorTests 279/279 PASS
OptionalAccessRuleAssignerTests 289/289 PASS
Type0RouteMaskAssignerTests 257/257 PASS
OptionalRegionGrowerTests 234/234 PASS
OptionalAttachmentEnumeratorTests 202/202 PASS
OptionalRegionModelsTests 194/194 PASS
Existing MAP05 aggregate 1959/1959 PASS
Actually executed total >=4833 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
Assets meta 3323 -> 3332..3334 depending on preexisting Microchunks folders
new Runtime C#/meta 8/8
new Test C#/meta 1/1
new folder meta 0..2 only
Authoring CSV/meta 50/50 and manifest unchanged
Generated CSV 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes 0
MAP07_02+ forbidden production symbols 0
```

전부 PASS일 때만 MAP07_01 COMPLETE/Current Task NONE으로 finalize한다. `MAP07_02_IMPLEMENT_TILE_LAYER_RULES`는 LOCKED로 유지하고 자동 시작하지 않는다.
