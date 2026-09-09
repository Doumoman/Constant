---
mcp_patch:
  format: single_task_v1
  task_id: RMAP17_WORLD_BAKE
  task_file: TASKS/RMAP17_WORLD_BAKE.md
  requires_current_task: NONE
  requires_completed_task: RMAP16_CLUSTERS
  requires_result:
    path: REPORTS/RMAP16_CLUSTERS_RESULT.md
    status: PASS
    sha256: e25ebd333873179c437ddc38cd61e338819d999b4cc703f4d197e9aa19ba9514
  requires_installed_task:
    path: TASKS/RMAP16_CLUSTERS.md
    sha256: 9b7c78e2c8257a5baf1d679b562763a9db4e478f42676e4d94e730ef4b2691ca
  sets_current_task: RMAP17_WORLD_BAKE
---

# RMAP17_WORLD_BAKE - Full static world Tilemap, Collider, Player and Camera bake

```text
TASK: RMAP17_WORLD_BAKE
STATUS: CURRENT after normal Apply
INPUT: MapDesign/MCP_INBOX/RMAP17_WORLD_BAKE.md
EXPECTED_RESULT: MapDesign/MCP/REPORTS/RMAP17_WORLD_BAKE_RESULT.md
NEXT: RMAP18_WORLD_STATE
NEXT STATUS: LOCKED / DO NOT START
```

## Objective

Bake the verified RMAP16 624 x 416 static terrain into an actual Unity scene using Tilemaps and collider components, with the production Player placed at the RMAP15 Start and a continuous bounded camera. This is a static-world bake only: do not add RMAP18 mutable world state, progression controllers, save logic, or gameplay unlock effects.

## Required read and adaptation boundary

- Read the MCP entrypoint, apply/finalize rules, installed RMAP16 task/result, RMAP15 result and the RMAP16 `Rmap16BakeSnapshot` public API. Consume the verified RMAP16 snapshot and its cell digest directly; do not create a second world-generation path or substitute a fixture.
- Read the existing RMAP01 bake/collider/world/player/camera bindings and the verified small-run scene builder (`Assets/_Game/Live/Editor/RMAP10/RmapSmallRunSceneBuilder.cs`). Reuse its production-player, one-way, climb and camera conventions where they remain valid.
- Keep RMAP12 definition/RNG, RMAP13 graph, RMAP14 biome ownership and RMAP15 reservation/Start/fixed cells unchanged. Preserve RMAP16 S/A/O terrain, ownership, protected cells and overlays exactly.
- Keep existing unrelated scenes and user changes untouched. Use a dedicated RMAP17 scene and a builder-owned root so rebuild cleanup is limited to RMAP17-owned objects.

## Write allowlist

- `Assets/_Game/Map/Runtime/WorldGeneration/Baking/RmapWorldBakeExecutor.cs` and its matching meta file if Unity creates it.
- `Assets/_Game/Live/Editor/RMAP17/RmapWorldBakeSceneBuilder.cs` and its matching meta file if Unity creates it.
- `Assets/_Game/Tests/EditMode/Map/RMAP17/RmapWorldBakeExecutorTests.cs` and its matching meta file if Unity creates it.
- `Assets/_Game/Map/Scenes/MoonPalace/RMAP17/MoonPalaceWorldBake_RMAP17.unity` and its matching meta file if Unity creates it.
- `MapDesign/MCP/GENERATED/RMAP17/`, this task's Result, task/archive records, and the prescribed status transitions only.

## A04 - exact full-world bake

1. Use the full 624 x 416 coordinate range, 1 x 1 tile size, and preserve local/world tile coordinates. Air remains empty within the declared world bounds.
2. The baked plan must expose source plan/cell digests, exact source cell count, S/A/O counts, overlay counts and the RMAP15 Start location. Its cell-to-layer mapping must be deterministically derived from the RMAP16 snapshot.
3. Apply S as the base-solid Tilemap, O as a dedicated one-way Tilemap, and ladder/climb/Grab affordances on dedicated overlay layers. Do not build one GameObject per terrain cell.
4. Base terrain must have real static collision. One-way terrain must use the existing one-way collision/effector convention. Climb layers must provide the existing live climb trigger/surface convention. All scene layer, collider and tile inventories must be inspectable.
5. Build a dedicated scene idempotently. Rebuilding removes only the builder-owned RMAP17 root, then recreates Grid, Tilemaps, colliders, player and camera bindings from the one plan. Save the scene and assets.

## E08 - Player and camera binding

1. Instantiate the production Player prefab at the RMAP15 Start, with its verified 0.4 x 0.8 collider/movement configuration. The starting support must be present in the baked solid collision layer.
2. Bind the existing production follow-camera driver to that Player. Camera bounds must cover the full 624 x 416 world continuously and clamp at its edges; do not change global camera or player prefabs.
3. Rebuild safety must be exercised: a second builder execution must replace rather than accumulate builder-owned terrain, collider, player or camera objects.

## Evidence and focused validation

- Add focused EditMode tests that generate the same full plan twice and verify source-cell digest, exact full-world count, S/A/O and overlay categorization, in-bounds coordinates, RMAP15 Start support, and no RMAP16 protected-cell reinterpretation.
- Build the actual RMAP17 scene through its editor entry point, reload/inspect it, and record Tilemap tile counts, collider/component inventory, production Player placement/configuration, camera binding/bounds and second-build cleanup evidence.
- Run only the focused RMAP17 Unity EditMode filter. Record Unity version, filter, result count and XML. Do not claim broad/full regression or whole-game traversal coverage.
- Generate a manifest and review material in `GENERATED/RMAP17`, including a terrain overview and enlarged views that make the static Tilemap layers, start area, terrain/one-way/ladder overlays and camera/world bounds reviewable. Package the actual current Result, manifest, XML, images and relevant summaries as `RMAP17_REVIEW.zip`.

## Result, finalize and stop

- Result must state `STATUS: PASS` only if the actual static scene, exact snapshot handoff, focused tests, player/camera binding and generated evidence all pass. It must include task/archive/source/result/scene artifact SHA-256 values and any limitation honestly.
- On PASS, finalize only RMAP17: set `RMAP17_WORLD_BAKE` from CURRENT to COMPLETE and Current Task to NONE, then make one RMAP17-only commit containing the normal Apply, implementation, evidence, Result and final status.
- RMAP18_WORLD_STATE remains LOCKED. Do not apply/start RMAP18 and do not push.
