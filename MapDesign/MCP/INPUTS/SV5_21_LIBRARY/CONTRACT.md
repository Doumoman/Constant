# SV5_21_LIBRARY contract

## C01 — source and ownership

- Base commit is `b95289b6c2eb727dd107d01ce89279d1556a60f9`.
- SV5_20 must be COMPLETE, Current Task must be NONE before Apply, and
  SV5_22 must remain LOCKED.
- Read `RmapClusterAssemblyPlanner` as the established cluster boundary; do not
  edit it. The new library planner/exporter owns only local library data.
- Preserve all SV5_20 Player source, tests, evidence, Result, and binding.

## C02 — local scale and representation

- Canonical output contains exactly four layouts, each 20×24 local tiles.
- Each layout records exactly 480 explicit 1×1 cells and maps them to a 5×6
  grid of 4×4 micro-pattern slots.
- Allowed collision bases are `AIR`, `SOLID`, and `TOP_ONLY` only.
- Generation is seed-driven and deterministic. It performs zero world builds,
  zero world searches, zero global endpoint comparisons, and zero Sector
  partitions.

## C03 — one-room library form

- Platforms and openings must form one visually coherent tall library, not a
  random collection of isolated 4×4 rooms.
- Every layout has at least five primary tiers. Consecutive tier heights differ
  by 3–5 tiles. Individual platform runs are 4–10 tiles long.
- Each layout contains at least three left/right height-direction reversals.
- The left and right sides have unequal floor heights. A layout cannot equal
  its own horizontal mirror, and no accepted pair may be horizontal mirrors.
- No 6×6 region may be entirely SOLID. Broken platform ends and breathing gaps
  remain visible without turning the layout into mostly empty AIR.

## C04 — ports and local support

- Every layout has one LEFT and one RIGHT required port, each exactly two AIR
  cells high and one or two cells wide.
- Port lower Y values are unequal. Each opens into the main interior and has a
  nearby physical landing/support record.
- Ports are local connection sockets only. This Task does not place them in a
  world, bind them to a Sector, or claim end-to-end Player traversal.

## C05 — future diagonal stairs

- Every layout reserves 2–4 diagonal stair bands with stable ID, direction,
  start/end, run, rise, and clearance bounds.
- Each reservation changes both X and Y, has rise 3–5, has run at least rise,
  and stays within the 20×24 bounds with two cells of head clearance.
- A reservation is metadata over AIR; it is not a ladder, platform collider,
  Grab surface, teleport, automatic climb, or implemented stair motor.
- Export `StairDataReady=false` and `StairMotorReady=false`. SV5_22 and SV5_23
  own those transitions.

## C06 — bookshelf decoration

- Bookshelves are background-only, non-colliding rectangles.
- Their union covers 55–80% of the room's 480 background cells per layout.
- Decoration cannot alter collision cells, support, ports, Grab eligibility, or
  future stair reservation geometry.

## C07 — determinism and diversity

- Repeating the same seed produces byte-identical layout data and digest.
- A documented different seed produces four valid layouts with at least two
  occupancy digests different from the canonical seed.
- Accepted layout IDs, cell order, platform order, port order, stair order, and
  decoration order are stable and independent of collection enumeration.
- Randomness may choose bounded recipes; it may not relax a failed invariant.

## C08 — required exports

Create `MapDesign/MCP/GENERATED/SV5_21_LIBRARY/` containing:

- `BINDING.json`
- `library.json`
- `library_cells.csv`
- `library_micro_slots.csv`
- `library_platforms.csv`
- `library_ports.csv`
- `library_stair_reservations.csv`
- `library_decor.csv`
- `library_validation.json`
- `seed_comparison.json`
- `targeted_results.xml`
- `cluster_boundary_results.xml`
- `independent_library_audit.json`
- `preview/library.svg`
- `visual_export_audit.json`

The SVG must use `viewBox="0 0 624 416"` and show all four complete layouts in
a 2×2 review sheet with a legend. SOLID, TOP_ONLY, stair reservations, ports,
and bookshelf decoration must be visually distinct.

CSV headers are fixed as follows:

- cells: `layout_id,x,y,base,source_kind,source_id`
- micro slots: `layout_id,slot_x,slot_y,pattern_id,variant`
- platforms: `layout_id,platform_id,min_x,max_x,y,base,role`
- ports: `layout_id,port_id,side,x,y,width,height,flow`
- stair reservations:
  `layout_id,stair_id,direction,start_x,start_y,end_x,end_y,run,rise,min_clearance`
- decor:
  `layout_id,decor_id,kind,min_x,min_y,max_x,max_y,collidable`

## C09 — tests and checker

- Focused EditMode tests cover dimensions, 480-cell completeness, 4×4 slot
  mapping, deterministic ordering, seed diversity, asymmetric tiers, no mirror
  duplicate, platform ranges, no 6×6 SOLID fill, ports, stairs, decoration,
  readiness flags, and negative invalid layouts.
- Run `Sv5LibraryLayoutTests` exactly once after compile.
- Run `tools/check_library.py --project-root .` exactly once after export.
- Render and inspect the SVG once and write the visual audit.
- Run one cluster-boundary filter containing only `Sv5LibraryLayoutTests` and
  existing `RmapClusterAssemblyPlannerTests`; record its exact discovered count.
- Do not run PlayMode or the full historical SV5 suite in this Task.

## C10 — immutable and forbidden scope

- Do not modify `RmapClusterAssemblyPlanner`, SV5_20 files, Player runtime,
  Player settings/prefab, scenes, physics settings, packages, Master, or any
  predecessor evidence.
- Do not add `SectorId`, a fixed 48×32 partition, global endpoint pairing, or a
  624×416 generation loop.
- Do not implement stair input, contact, movement, collider, or Player behavior.
- No `_work`, `CHECK`, `FILES_VERIFY`, or temporary diagnostic artifacts may
  remain in the final generated folder or Review ZIP.

## C11 — completion

- `LibraryShellReady=true`; `StairDataReady=false`, `StairMotorReady=false`,
  `LibraryTraversalReady=false`, and `WorldPlaced=false`.
- Run post-readonly before Finalize. Result, BINDING, Task/Archive, Status, code,
  tests, documentation, and required exports form one atomic commit.
- Produce an exact-blob Review ZIP, keep SV5_22 LOCKED, and do not push.
