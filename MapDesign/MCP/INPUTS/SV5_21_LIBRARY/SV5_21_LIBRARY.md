# SV5_21_LIBRARY — author the local multi-level library shell

TASK: SV5_21_LIBRARY
EXPECTED RESULT: MapDesign/MCP/REPORTS/SV5_21_LIBRARY_RESULT.md
NEXT: SV5_22_STAIR_DATA — LOCKED / DO NOT START

## Required reads and Apply

1. Read this Task, `CONTRACT.md`, `LIBRARY_PROFILE.json`, `SOURCE_LOCK.json`,
   the SV5_20 Result/BINDING, and the SV5_03 BND-10/BND-15 audit rows.
2. Run the packaged `STAGE.py --verify-staged` with the exact FILES SHA.
3. Use the native `single_task_v1` Apply exactly once. Verify that SV5_21 is
   CURRENT and SV5_22 remains LOCKED before implementation.
4. Do not register another Task, manufacture an Archive, or alter Master.

## Work boundary

Implement a deterministic local library-shell planner and exporter in new
SV5-owned files. Read the existing `RmapClusterAssemblyPlanner` only as the
cluster extension boundary; do not modify or replace it. Work in 20×24 local
tile coordinates and four-cell micro-pattern slots. Do not generate, search,
or compare the 624×416 world and do not introduce Sector partitioning.

Create only:

- `Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/Sv5LibraryLayout.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/Sv5LibraryExport.cs`
- their `.meta` files
- `Assets/_Game/Tests/EditMode/Map/SV5/Sv5LibraryLayoutTests.cs` and `.meta`
- `MapDesign/MCP/SV5/29_LIBRARY_V5.md`
- `MapDesign/MCP/GENERATED/SV5_21_LIBRARY/**`
- Result/BINDING and lifecycle files required for Finalize

## Required library geometry

- Produce four deterministic, distinct 20×24 local library layouts.
- Each layout uses a 5×6 grid of 4×4 micro-pattern slots, resolved to explicit
  1×1 cells. The room must read as one connected large space, not 30 unrelated
  mini-rooms.
- Left and right floor heights are asymmetric. Reject self-mirror layouts and
  mirrored duplicates. Use at least three visible zig-zag height reversals.
- Primary platform tiers are separated vertically by 3–5 cells. Platforms are
  4–10 cells long, have broken edges/gaps, and never form a filled SOLID 6×6
  rectangle.
- Provide left/right two-cell-high connection ports at unequal heights. Bind
  them to supported landing areas without claiming whole-world placement.
- Reserve 2–4 diagonal stair bands per layout. Reservations contain endpoints,
  direction, run, rise, and clear headroom, but create no StairMotor, ladder,
  automatic movement, or new collider behavior in this Task.
- Fill the background with non-colliding bookshelf decoration covering 55–80%
  of the local room. Decoration must never become SOLID, TOP_ONLY, Grab, or a
  movement surface.

## Readiness boundary

This Task proves `LibraryShellReady=true` only. It must export
`StairDataReady=false`, `StairMotorReady=false`, `LibraryTraversalReady=false`,
and `WorldPlaced=false`. SV5_22 authors stair data; SV5_23 owns actual stair
contact and Player movement. Do not use the jump recipe as a substitute for
stairs and do not modify Player code, settings, prefab, scenes, or physics.

## Evidence and gates

Export the exact artifacts required by `CONTRACT.md`, including a 624×416 SVG
review sheet with all four layouts. The drawing must distinguish SOLID,
TOP_ONLY, stair reservation, ports, and bookshelf decoration.

Run in this order:

1. Unity compile.
2. Focused `Sv5LibraryLayoutTests` once.
3. The packaged independent checker once.
4. Render the SVG and record a visual audit.
5. Run only the focused cluster-boundary regression named by the contract.

Do not run the 443-test historical SV5 suite or PlayMode in this local shell
Task; SV5_23 and SV5_42 own the later movement/full-scan gates. Do not rerun a
passing gate. On any failure, preserve the first real result and stop.

After all gates pass, run `STAGE.py --post-readonly`, write Result/BINDING,
Finalize only SV5_21, create one atomic Task-owned commit and exact-blob
`SV5_21_LIBRARY_REVIEW.zip`, then stop. Do not start SV5_22 and do not push.

