# SV5_16 — irregular actual-cell SOLID support outlines

## Purpose and visual meaning

Make the accepted jump route look less like a row of thin rectangular
platforms. Keep every route top surface exactly where SV5_15 placed it, then
add a small, irregular full-collision SOLID body below each of the five SOLID
supports. Adjacent columns differ by at most one cell except where the approved
profile explicitly uses a two-cell taper.

The result is five compact rocky masses with uneven one- or two-cell undersides,
not a 24x32 rectangular border and not a filled room. ONE_WAY platforms remain
thin top-only platforms. This Task does not make a room shell or place anything
in the world.

## C01 — immutable base geometry

- Consume `Sv5JumpGrabGeometry.CreateCanonicalLocalFixture()` as the sole base.
- Preserve the exact ten support records, 27 support cells, nine ordered route
  links, single Grab edge, Grab clearance, action sequence, entry, and exit.
- Preserve every support top interval, takeoff, landing, gap, rise, mode, and
  required-route flag. Do not translate or resize any support.
- Preserve `JS04_RIGHT_GRAB`: contact `(15,6)`, hang body/head `(16,6)/(16,7)`,
  pull-up foot/head `(15,7)/(15,8)`, `RIGHT` face, `RIGHT_TO_LEFT` approach.
- SV5_13, SV5_14, and SV5_15 source/evidence are immutable.

## C02 — exact irregular backing profile

Create full-collision outline cells directly below the bottom row of each SOLID
support. For a support column at x, depth d emits cells `(x, support.y-1)`
through `(x, support.y-d)`. Depth zero emits nothing.

| Owner SOLID | Column depths `x:depth` | Emitted cells |
|---|---|---:|
| JS00_ENTRY_SOLID | `0:1, 1:1, 2:0` | 2 |
| JS02_SOLID | `12:1, 13:2, 14:1` | 4 |
| JS04_SOLID | `14:2, 15:1` | 3 |
| JS06_SOLID | `4:2, 5:2, 6:1` | 5 |
| JS08_SOLID | `3:2, 4:1` | 3 |

The exact total is 17 new outline cells. Together with the immutable 27 base
support cells, final local occupancy is exactly 44 unique cells.

- Every outline cell has collision `SOLID`, records its owner support and depth,
  is inside 24x32, touches its owner column vertically, and overlaps no base or
  other outline cell.
- No outline cell may be emitted under a ONE_WAY support.
- Each of the five SOLID supports has positive backing, at least one depth-one
  column exists, and at least one depth-two column exists.
- Scan final occupancy and require zero fully occupied 6x6 SOLID windows.
- Do not add wall strips, a floor across the room, a ceiling, isolated decorative
  blocks, or a rectangular perimeter. The approved cells are the entire change.

## C03 — preserve movement space and Grab semantics

- Outline cells extend downward only. They may not replace or move any route
  support, takeoff, landing, contact, hang, pull-up, entry, or exit coordinate.
- Recompute a supported foot plus AIR body/head reservation for all ten route
  supports against final occupancy. All ten must pass.
- Recheck the four SV5_15 Grab AIR cells against final occupancy; all remain AIR.
- Keep exactly one existing `JUMP_GRAB` link and exactly one existing Grab edge.
  New outline faces are not automatically grabbable and create zero new Grab
  edges. Grab still requires an explicit edge attached to an exposed SOLID face.
- Preserve one-way completion. Do not fabricate reverse completion.
- This remains static actual-cell proof. Swept body clearance is SV5_18 and live
  Player verification is SV5_20.

## C04 — typed source and exports

Create new `Sv5JumpOutlineGeometry` and `Sv5JumpOutlineExport` implementations
that derive from the accepted SV5_15 object. Do not copy earlier planners into a
new legacy model.

Export from one canonical typed object graph to
`MapDesign/MCP/GENERATED/SV5_16_JUMP_OUTLINE/`:

- `jump_outline.json`
- `deformation_depths.csv`
- `outline_cells.csv`
- `final_occupancy.csv`
- `route_links.csv`
- `route_clearance.csv`
- `grab_edges.csv`
- `grab_clearance.csv`
- `grab_action_sequence.csv`
- `jump_outline_validation.json`
- `preview/jump_outline.svg`
- `visual_export_audit.json`
- `independent_jump_outline_audit.json`
- targeted/focused NUnit XML and final `BINDING.json`

The SVG shows all 24x32 cells, distinguishes base SOLID, ONE_WAY, and new
outline SOLID, traces the uneven underside of each solid mass, overlays the
ordered route and existing Grab witness, and visibly labels `0-2 cell downward
deformation`, `ONE_WAY unchanged`, `no rectangular room shell`, and `PLAYER
verification deferred`. Render it to PNG and inspect it before full regression.

## C05 — independent rejection and tests

The checker derives all 17 expected outline coordinates from SV5_15 support y
and the profile depths, independently rebuilds the 44-cell union, scans 6x6
windows, compares all nine route links and the Grab witness to SV5_15, and
checks route/Grab AIR reservations against final occupancy.

Add at least 22 meaningful local tests including: exact per-column depth table;
wrong depth; missing/extra/duplicate/overlapping/out-of-bounds outline cell;
outline under ONE_WAY; isolated outline cell; support/top/route mutation; blocked
route body/head; blocked Grab air; new automatic Grab edge; rectangular shell;
6x6 fill; deterministic repeat; export round-trip; visual labels; readiness;
one-way completion; and zero whole-world work.

## C06 — readiness, time, and scope

- New targeted tests use only the 24x32 local fixture and perform zero 624x416
  builds/searches, global endpoint comparisons, or per-candidate world work.
- Compile and run targeted tests, then independent checker, then rendered SVG
  inspection. Only after those pass, run the full SV5 regression exactly once.
- Final focused total is at least 300 with zero failed/skipped/inconclusive.
- On PASS keep Contract/Solid/Grab readiness true and set
  `JumpOutlineReady=true`. Keep `ComposedGeometryReady=false` and
  `PlayerVerified=false`.
- Do not modify Player, tree/Hub, scenes, prefabs, Packages, ProjectSettings,
  world placement, Master, or unrelated dirty files. Run post-readonly before
  Finalize, make one atomic Task-owned commit and exact-blob Review ZIP, keep
  SV5_17 locked, and do not push.
