```yaml
mcp_patch:
  format: single_task_v1
  task_id: RMAP12_WORLD_DATA
  task_file: TASKS/RMAP12_WORLD_DATA.md
  requires_current_task: NONE
  requires_completed_task: RMAP11_POOL500
  requires_result:
    path: REPORTS/RMAP11_POOL500_RESULT.md
    status: PASS
    sha256: 524b1f3aa1ab77cd4ff9c32ff9ffc91e317b613ae6b069cd1be8fa45d1a00865
  requires_installed_task:
    path: TASKS/RMAP11_POOL500.md
    sha256: 1f96f5414fb0aa9040017491c9b5f33978822fdcfa0cdd56ea3f78af57d950c5
  sets_current_task: RMAP12_WORLD_DATA
```

# RMAP12_WORLD_DATA — deterministic world definition, mutation and stable IDs

TASK: RMAP12_WORLD_DATA
EXPECTED_RESULT: MapDesign/MCP/REPORTS/RMAP12_WORLD_DATA_RESULT.md
NEXT: RMAP13_WORLD_GRAPH LOCKED / DO NOT START

## Scope

Implement W08–W12 through existing RNG, stable-ID and save/mutation contracts:

- W08: deterministic world request/result from Seed + ContentVersion + GeneratorVersion + final RMAP11 pool version/digest.
- W09: isolated RunGraph, Biome, SpecialReservation, Port, Pattern, Overlay and Population streams, including stable scope/attempt values.
- W10: immutable generated definition separate from mutable breakable/door/mechanism/reward/NPC state and serializable mutation round-trip.
- W11: stable IDs for MicroChunk, BreakableTile, Mechanism, RewardChest, SpecialRegionTrigger, MonsterSpawnSlot and NPCShopSlot.
- W12: generator has no Player, Camera, Unity instance-ID or wall-clock dependency; placement belongs to an adapter only.

## Read / write boundaries

Read `RMAP01/file_bindings.csv`, existing deterministic RNG stream factory, stable-ID/save/mutation APIs, RMAP11 final-pool API and their focused tests. Adapt existing map-runtime generation code or add one cohesive world-data model/test pair under `Assets/_Game/Map/Runtime/WorldGeneration/`; do not alter RMAP11, Player/camera/input behaviour, ports, composer, world graph, biomes or content systems.

## PASS conditions

Focused tests prove repeatable digest/IDs, seven-stream isolation, definition/mutation separation and save round-trip, uniqueness/reproducibility of all seven stable-ID kinds, and absence of Player/Camera/runtime instance inputs. Report actual file responsibility/reuse, RMAP11 binding, exact test XML and all unimplemented content as placeholders only. Do not run broad regressions, builds, RMAP13 or push.

PASS only after actual implementation and focused tests. Then Finalize RMAP12 CURRENT→COMPLETE, keep RMAP13 LOCKED and commit only RMAP12-owned changes.
