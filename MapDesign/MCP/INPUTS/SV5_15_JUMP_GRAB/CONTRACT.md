# SV5_15 — actual SOLID-face jump/grab/pull-up geometry

## Purpose and exact interpretation

Add one genuine Grab-dependent transition to the accepted SV5_14 local jump
room. The player jumps toward one explicitly exposed vertical face of one
SOLID support, occupies an adjacent AIR hang position, then pulls up into AIR
above that same support. This is a cell-level movement sequence.

It is not ladder logic, not automatic climbing, not an abstract graph edge,
and not permission to grab every SOLID. ONE_WAY supports are platforms with
top-only collision and have no grabbable side face. Reverse completion is not
required.

## C01 — preserve the accepted local room

- Start from `Sv5JumpSolidGeometry.CreateCanonicalLocalFixture()` and retain
  its 24x32 local canvas, ten support IDs, ordered route, entry, and exit.
- Do not modify any SV5_13 or SV5_14 source, tests, documents, or evidence.
- Build a new derived typed fixture in `Sv5JumpGrabGeometry`; do not copy the
  previous implementation into a parallel legacy model.
- The only support geometry change is translating `JS04_SOLID` from `(14,5)`
  to `(14,6)`. Its width/height remain `2x1`. All other supports remain byte-
  equivalent in typed values to the SV5_14 canonical supports.
- Re-emit all 27 actual support cells from the derived supports. The moved
  SOLID cells are `(14,6)` and `(15,6)`. Their old cells `(14,5)` and `(15,5)`
  must be AIR in the derived fixture.
- This remains a local room. Do not place, scan, copy, BFS, or mutate the
  624x416 world. The 24x32 canvas is not a Sector or spatial partition.

## C02 — exact required Grab link

Rebuild the nine ordered links through `Sv5JumpContract.Measure`.

- `JS_LINK_03` is the only `JUMP_GRAB` link.
- Source: `JS03_ONE_WAY`, top interval `x=18..20`, `top_y=5`.
- Target: moved `JS04_SOLID`, occupied cells `(14,6)` and `(15,6)`, top
  interval `x=14..15`, `top_y=7`.
- Direction: `RIGHT_TO_LEFT`; takeoff `(18,5)`; landing `(15,7)`.
- The immutable SV5_13 formula must recompute `gap_air=2` and `rise=2`.
- Attach exactly one `Sv5JumpGrabEdge`: target support `JS04_SOLID`, contact
  `(15,6)`, face `RIGHT`, approach `RIGHT_TO_LEFT`, hang body `(16,6)`, and
  pull-up destination `(15,7)`. It must be exposed and statically safe.
- `JS_LINK_04` remains a normal `JUMP`; after the target translation its rise
  is `0`. The other seven normal links keep rise `1`. Every normal link must
  have rise `0..1`; no second Grab edge may be invented.
- The route remains one-way and ordered from the existing entry to exit.

## C03 — actual contact, hang, and pull-up cells

Validate the Grab against final derived occupancy rather than flags alone.

- Contact `(15,6)` exists and is a SOLID cell owned by `JS04_SOLID`.
- The cell immediately outside its right face, hang body `(16,6)`, is AIR.
- Hang head `(16,7)` is AIR.
- Pull-up foot/landing `(15,7)` is AIR and is supported by contact cell
  `(15,6)` directly below.
- Pull-up head `(15,8)` is AIR.
- All five coordinates lie inside the 24x32 canvas.
- Export the ordered action witness `TAKEOFF -> CONTACT/HANG -> PULL_UP ->
  LAND`. Contact is a hand-facing SOLID cell; hang and pull-up positions are
  player AIR positions and must never be emitted as support cells.
- This discrete static witness does not claim a swept Player collider test.
  Full body/motion clearance remains SV5_18; live Player verification remains
  SV5_20.

## C04 — forbidden shortcuts and false claims

- A Grab targeting ONE_WAY must fail.
- A hidden/non-exposed face, wrong face/direction, mismatched contact corner,
  occupied hang body/head, occupied pull-up foot/head, missing edge, rise over
  two, or `PLAYER_VERIFIED` state must fail.
- Normal `JUMP` may not carry a Grab edge. `JUMP_GRAB` must carry exactly its
  matching edge and target.
- Do not add a ladder, climbable trunk, wall-climb controller, input handling,
  Player tuning, new physics, or generic `CanGrab` on all cells.
- Do not weaken `Sv5JumpContract`, alter SV5_14 geometry in place, or count a
  boolean as proof when the exported cells contradict it.

## C05 — source, exports, and visual evidence

Create new `Sv5JumpGrabGeometry`, `Sv5JumpGrabExport`, and tests. Export from
one canonical typed object graph to
`MapDesign/MCP/GENERATED/SV5_15_JUMP_GRAB/`:

- `jump_grab.json`
- `supports.csv`
- `support_cells.csv`
- `route_links.csv`
- `grab_edges.csv`
- `grab_clearance.csv`
- `grab_action_sequence.csv`
- `jump_grab_validation.json`
- `preview/jump_grab.svg`
- `visual_export_audit.json`
- `independent_jump_grab_audit.json`
- targeted/focused NUnit XML and final `BINDING.json`

The SVG must show the entire 24x32 boundary, all support cells, the ordered
route, the JUMP_GRAB link in a distinct color, target contact face, hand/contact
marker, hang body/head, pull-up foot/head, entry/exit, and a legend stating
`SOLID face only`, `ONE_WAY is not grabbable`, and `PLAYER verification deferred`.
Render the SVG to PNG and inspect it before the full regression.

## C06 — independent validation and tests

The checker reconstructs every support rectangle and actual cell, compares the
derived supports against SV5_14, recomputes all nine gap/rise values, proves the
single Grab target/contact/face, and checks the four AIR clearance cells against
final occupancy. It must not accept self-reported safety fields without cell
proof.

Add at least 20 meaningful local tests covering the passing fixture and every
failure in C04, exact support translation, old-cell removal, contact ownership,
ordered action witness, deterministic repeat, export round-trip, readiness,
one-way completion, and zero whole-world work.

## C07 — readiness and execution order

- Compile and run new local targeted tests first, then the independent checker,
  then render and visually inspect the SVG.
- Only after those gates pass, run the full SV5 EditMode regression exactly
  once. Final focused total is at least 272 with zero failed, skipped, or
  inconclusive tests.
- On PASS set `JumpContractReady=true`, `JumpSolidGeometryReady=true`, and
  `JumpGrabGeometryReady=true`. Keep `ComposedGeometryReady=false` and
  `PlayerVerified=false`.
- Run `STAGE.py --post-readonly` before Finalize. Finalize once, make one atomic
  Task-owned commit, create an exact-blob Review ZIP, keep SV5_16 locked, and do
  not push.

