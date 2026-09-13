# SV5_11_FIX01 — actual-cell Hub connection contract

## Defect being corrected

SV5_11 produced a valid 24×40 shell, but its five external connections were only
synthetic Manhattan centerlines. `Path()` did not consult occupancy or movement,
three overlap/bypass properties always returned `false`, and the final physical
product did not include Hub connection geometry. Tests and the checker accepted
the exported claims instead of recomputing them.

This Task corrects that defect only. The shell shape, six socket layout, tree
reservation, existing core/loop/sidepath behavior, and `SV5_12` boundary remain.

## C01 — concrete 1×1 geometry

- Every accepted connection owns an ordered 1×1-cell centerline from one active
  Hub port anchor to one real external endpoint.
- The route is not a display-only line. Its centerline, head-clearance cells, and
  any changed support/carve cells are applied to `HubShell.FinalOccupancy` before
  movement/product validation.
- Existing AIR may be borrowed without claiming ownership. Any changed cell must
  carry `HUB_CONNECTION_ACTUAL` ownership and exact before/after values.
- Centerline steps are cardinal and unique. A connection uses no teleport, remote
  cut, or vertical wall that the current movement rules cannot traverse.
- Clearance is two cells where a standing passage requires it. The actual movement
  witness decides traversability; a raw Manhattan path is insufficient.

## C02 — locality and protected ownership

- Connection body cells must not alter or cross protected AIR, Type0, progression
  gate, core, reservation, loop, or sidepath-owned cells.
- The declared external anchor is the sole permitted read-only contact with its
  owning external route. It must already be passable and must not be changed.
- Shell port/aperture contact is allowed inside the accepted Hub shell. All other
  foreign contacts or shared ownership reject the candidate.
- Each accepted connection stores computed overlap/bypass results. Expression-
  bodied constants or values copied only for export are forbidden.

## C03 — actual movement and topology

- Four to six active sockets connect to distinct real external Room IDs and
  distinct SpaceGroup IDs.
- For every connection, at least one explicitly recorded direction has a valid
  movement witness between port and external anchor. Reverse completion is not
  required; `HUB_TO_EXTERNAL`, `EXTERNAL_TO_HUB`, or `BIDIRECTIONAL` is recorded.
- All active Hub ports remain in one passable Hub component.
- The post-Hub occupancy is used to rebuild the final movement graph and the full
  legal physical product. Nine legal states × six resource orders must pass.
- No connection may open a progression bypass. A semantic digest equality against
  the pre-Hub product is not a substitute for post-Hub computation.

## C04 — bounded work, no retired grid abstraction

- Candidate routing uses a bounded local search box and an explicit attempt cap.
  It may reuse one immutable baseline occupancy/index per profile and sparse
  overlays; it must not copy or BFS the 624×416 world for every candidate.
- Expensive global topology/product validation runs once after the final accepted
  batch for each default/repeat profile.
- Do not add or restore Sector, fixed 48×32 partitioning, SectorId, sector-pair
  packing, or sector-derived RNG. World coordinates, Room IDs, SpaceGroup IDs,
  actual ownership, and bounded AABBs are sufficient.
- Do not add parallel worker complexity merely to hide an invalid route. Correct
  deterministic local geometry first.

## C05 — independent evidence

Each default/repeat export contains at least:

- `hub_shell.json`
- `hub_connections.csv`
- `hub_connection_cells.csv`
- `hub_connection_checks.csv`
- `constraint_sources.json`
- `hub_validation.json`
- `preview/hub_connection_fix.svg`

`hub_connection_cells.csv` records connection ID, role, sequence, coordinates,
before/final/head/support values, changed flag, and ownership. The connection CSV
records endpoints, direction, path length, computed overlap/bypass values, and
movement-witness result.

`constraint_sources.json` binds the actual upstream CSV sources and their SHA-256,
coordinate columns, categories, and optional filters. The independent checker
must read those upstream bytes and recompute body intersections, cardinal order,
ownership, clearance, endpoint exception, and validation counts. It must not pass
because the three old booleans say `false`.

## C06 — tests and completion

- Preserve all 155 predecessor focused tests and correct obsolete assertions.
- Add at least 10 meaningful FIX01 tests. Required cases include the original
  false-positive L route, protected body crossing, Type0 crossing, progression
  bypass, external-anchor-only exception, changed-cell ownership, one-way valid
  witness, post-Hub product rebuild, deterministic output, and bounded work.
- Run targeted Hub/FIX01 tests first. Run the independent checker second. Only
  after both pass, run exactly one complete SV5 focused regression; do not repeat
  a 20-minute full suite while ordinary failures remain.
- Final focused count is at least 165, with 0 failed and 0 skipped. Record actual
  duration, XML SHA, independent audit, generated digests, and performance data.
- `TreeGrabGeometryReady`, `ComposedGeometryReady`, and `PlayerVerified` stay
  `false`. Do not claim Unity Player traversal or start `SV5_12`.
- Finalize and make one atomic Task-owned commit. Preserve unrelated dirty files,
  do not push, and produce `SV5_11_FIX01_REVIEW.zip` from exact commit blobs with
  a review manifest.

