# SV5_12 — physical branch handoff and central Tree Grab contract

## Purpose

Turn the Hub's reserved central tree volume into actual 1×1 solid/grabbable
geometry that lets an itemless player move between Hub heights. Before doing so,
close two residual FIX01 handoff defects: logical connections cannot alias the
same physical external anchor, and AIR continuity cannot be called a player
movement witness.

This is the registered SV5_12 Task. Do not create `SV5_11_FIX02` and do not start
SV5_13.

## C01 — physical Hub handoff gate

- Each accepted Hub connection has a distinct `(external_x, external_y)` anchor.
  Distinct Room/SpaceGroup strings alone do not count as distinct connections.
- Default and repeat each retain 4–6 active connections and at least four distinct
  physical external anchors.
- Connection centerlines outside the Hub footprint do not overlap or merge before
  their own external anchors. Shared Hub interior cells are allowed only where the
  Hub shell intentionally joins them.
- The external anchor remains unchanged read-only AIR; protected, Type0,
  progression, core, reservation, loop, and sidepath body intersections remain 0.
- Re-route deterministically if aliases/merges exist. Do not rename duplicate IDs
  or offset only the exported coordinates to manufacture uniqueness.

## C02 — real itemless movement witness

- A connection witness is an ordered sequence of movement states/edges produced
  by the active platformer traversal rules over final occupancy. It is not the AIR
  centerline copied into a differently named collection.
- Standing states require body/head AIR and valid support. WALK/STEP/JUMP/DROP/
  JUMP_GRAB transitions record source, destination, delta, and supporting or
  grabbable contact cells.
- Upward jump+grab gain is at most two tiles. A longer empty vertical column is
  not traversable. No item, ladder, flight, explosion, or future ability is used.
- At least one direction per connection must pass. Reverse completion is optional
  and the recorded direction must match the actual witness.
- The independent checker recomputes every local edge from exported occupancy and
  contact cells; a production `route_verified=true` flag is not sufficient.

## C03 — central tree geometry

- Replace the 4×24 reserved placeholder with a deterministic irregular solid tree
  trunk and branches assembled from 1×1 cells. The tree remains within the 12×30
  Hub inner volume except explicitly bounded branch tips.
- Use solid/grabbable surfaces, not floating platform-only stairs. The silhouette
  may bend by one or two cells and must not become a straight rectangular ladder.
- Branches form at least three vertical levels with typical gaps of 2–4 cells.
  No solid rectangular fill may reach 6×6.
- Keep two-cell body/head clearance around required movement positions. Do not
  close active port necks or external connection routes.
- Provide at least two itemless bottom-to-upper-level routes. Routes may share the
  trunk but must differ by at least one real branch/contact sequence.
- Provide safe recovery from every required branch level to a lower Hub landing.
  A failed grab must not create an unrecoverable pit or progression bypass.

## C04 — graph/product integration

- Tree solid, carved clearance, grab surfaces, movement states, and corrected Hub
  connections are applied to final occupancy before final movement/topology.
- Rebuild the nine legal states × six resource orders physical product once per
  profile. All 54 combinations and six projection proofs pass.
- Candidate-local work uses sparse overlays and bounded searches. There is no
  624×416 copy or BFS per tree/connection candidate.
- Do not restore Sector/48×32 partitioning, SectorId, sector pair packing, or
  sector-derived RNG.

## C05 — evidence and independent audit

Default and repeat each export:

- `hub_connections.csv` and `hub_connection_cells.csv` with corrected unique
  physical anchors
- `hub_connection_movement.csv` with typed player movement edges
- `tree_grab.json`
- `tree_cells.csv`
- `tree_surfaces.csv`
- `tree_movement_witness.csv`
- `tree_recovery_witness.csv`
- `tree_validation.json`
- `preview/tree_grab.svg`

`tree_cells.csv` records coordinates, role, before/final value, owner, and
clearance/support state. `tree_surfaces.csv` identifies the exact solid contact
cell and exposed grab face. Movement exports bind to these cells and final
occupancy. The independent checker recomputes unique physical branches, movement
deltas, two-cell upward cap, contact existence, clearance, route diversity,
recovery, readiness flags, and product counts.

## C06 — readiness, tests, and time control

- Correct the existing Hub/FIX01 tests that accepted coordinate aliases or AIR-
  only witnesses. Add at least 10 meaningful SV5_12 tests.
- Required failures: duplicate external anchor, centerline reuse, six-cell empty
  vertical climb, missing grab contact, blocked head clearance, rise above two,
  disconnected upper level, missing recovery, 6×6 fill, and false readiness.
- Required successes: one-way route, two-cell jump+grab, two distinct tree routes,
  deterministic repeat, 9×6 product, and default/repeat export round-trip.
- Run targeted Hub+Tree tests first, then the independent checker. Only after both
  pass, run the full SV5 regression exactly once. Do not spend another 20-minute
  run while ordinary targeted failures remain.
- Final focused count is at least 179 with 0 failed/skipped. Record durations and
  exact XML SHA values.
- On PASS set `TreeGrabGeometryReady=true`. Keep
  `ComposedGeometryReady=false` and `PlayerVerified=false`; this Task validates
  generated geometry/movement contracts, not a live Unity Player run.
- Finalize only with a matching PASS Result, create one atomic Task-owned commit,
  then produce `SV5_12_TREE_GRAB_REVIEW.zip` from exact commit blobs. Do not push.

