# RMAP12_WORLD_DATA Result

TASK: RMAP12_WORLD_DATA

STATUS: PASS

## Implementation

RMAP12 adds a deterministic, Unity-object-free world-data contract. `RmapWorldDataGenerator` consumes the final RMAP11 pool only through `BuildFinalPool()`, records its `DataVersion` and digest, derives seven independent named stream seeds from explicit request/version/scope input, and creates one reproducible stable ID for each deferred content kind: MicroChunk, BreakableTile, Mechanism, RewardChest, SpecialRegionTrigger, MonsterSpawnSlot and NPCShopSlot.

`RmapWorldDefinition` is immutable. `RmapWorldMutationState` is a separate, ordered mutable record for later breakable/door/mechanism/reward/NPC changes and supports a deterministic text round-trip. There is no Player, Camera, Unity instance ID, clock, scene or static mutable generator state in the model.

## Responsibility and reuse

| Path | Responsibility / reuse judgement |
|---|---|
| `Assets/_Game/Map/Runtime/WorldGeneration/WorldData/RmapWorldDataContract.cs` | NEW cohesive W08–W12 contract. Reuses RMAP11 final-pool API and the project’s stable canonical SHA approach; does not own graph/biome/content generation, Player placement or save-file I/O. |
| `Assets/_Game/Tests/EditMode/Map/RMAP12/RmapWorldDataContractTests.cs` | NEW focused deterministic/stream/isolation/mutation/stable-ID check. No broad regression. |
| `MapDesign/MCP/GENERATED/RMAP12/rmap12_editmode_results.xml` | NEW Unity evidence. |

## Preconditions and validation

- RMAP11 PASS Result SHA verified before Apply: `524b1f3aa1ab77cd4ff9c32ff9ffc91e317b613ae6b069cd1be8fa45d1a00865`.
- RMAP11 installed Task SHA verified: `1f96f5414fb0aa9040017491c9b5f33978822fdcfa0cdd56ea3f78af57d950c5`.
- Installed/archived RMAP12 Task SHA: `f6b649abc0e452fb897e7e4175adee3ab94e4cfe051cb7234be8147ee9c184fb`, byte-identical.
- Unity 6000.3.8f1 focused EditMode `RmapWorldDataContractTests`: **2/2 PASS**. XML SHA: `b2b3fbb8a5acc62b0aeff4d49fb4cb2846aea2a91546f5d91f4734100007147b`.

OUT_OF_SCOPE: 624x416 assembly, graph/biome/content/population implementation, Player/Camera placement adapters, disk save-slot writes, RMAP13 and push. RMAP13 remains LOCKED.

NEXT: Finalize RMAP12 only; do not start RMAP13.
