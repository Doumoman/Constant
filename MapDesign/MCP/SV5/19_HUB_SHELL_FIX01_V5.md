# SV5 Hub shell FIX01: real connection geometry

`SV5_11_FIX01` replaces the display-only Hub connection centerlines from
`SV5_11_HUB_SHELL` with deterministic routes over the actual 1x1 occupancy.
The 24x40 shell, its six sockets, the existing core/loop/sidepath rules, and the
locked `SV5_12_TREE_GRAB` boundary are unchanged.

## Geometry and ownership

`Sv5HubConnectionRouter` searches a bounded local AABB with a fixed attempt cap.
It walks cardinal neighbors only and validates the centerline, standing head
clearance, and support cell for each step. The immutable sidepath occupancy is
the baseline; the router uses sparse desired-value overlays and does not copy or
BFS the complete 624x416 world per candidate.

An accepted route may borrow existing AIR without taking ownership. Every cell
whose value changes is written into `Sv5HubShell.FinalOccupancy` with both owner
and provenance set to `HUB_CONNECTION_ACTUAL`, together with its exact before and
final values. The declared external anchor is the single read-only foreign
contact: it must already be passable and it is never changed.

The route body is rejected when it intersects the actual ProtectedAir,
FixedSolid, Type0, progression-gate, core, reservation, loop, or sidepath source
sets. `ProtectedOverlap`, `Type0Overlap`, and `ProgressionBypass` are computed
from those sets and the actual route cells. They are not acceptance claims or
stored constants.

## External identity and progression

External targets are real predecessor Room/SpaceGroup endpoints associated with
an exposed physical AIR point in the same closed-gate cardinal component. The
batch search keeps Room IDs and SpaceGroup IDs distinct and reserves changed
cells between accepted routes. A candidate that bridges closed progression
components is rejected. Four to six of the six shell sockets may become active.

Each accepted connection stores an ordered cardinal centerline, the external
Room and SpaceGroup identity, and a concrete movement witness. The currently
implemented witness direction is recorded explicitly; reverse traversal is not
inferred.

## Post-Hub product

Accepted connection overlays are applied before final physical movement is
constructed. `Sv5SpaceGraphPlan.ApplyHub` then rebuilds movement, topology, and
the legal physical product from the post-Hub occupancy. Each profile performs
one global product validation covering nine legal states and six resource
orders. A predecessor digest is retained only as baseline evidence and is not
used as a substitute for this rebuild.

## Independent evidence

Default and repeat profiles export the tested plan to
`MapDesign/MCP/GENERATED/SV5_11_FIX01`. The C05 evidence includes:

- `hub_shell.json`, `hub_connections.csv`, and
  `hub_connection_cells.csv`;
- `hub_connection_checks.csv`, `constraint_sources.json`, and
  `hub_validation.json`;
- the eight bound constraint CSV snapshots and
  `preview/hub_connection_fix.svg`.

`constraint_sources.json` binds each snapshot by path and SHA-256. The bundled
independent checker reloads those bytes and recomputes cardinal order, AIR/head
clearance, changed-cell ownership, the external-anchor exception, protected
intersections, and aggregate counts without trusting the exported overlap flags.

The final default plan contains four distinct connections and the repeat plan
contains five. Both report zero whole-world copies and zero whole-world BFS runs
per candidate, one global product run, successful 9x6 products, and false values
for `TreeGrabGeometryReady`, `ComposedGeometryReady`, and `PlayerVerified`.

## Regression boundary

FIX01 tests preserve the original vertical-then-horizontal false positive as a
reproduction case and cover protected/Type0/progression rejection, the sole
external-anchor exception, ownership, directional witnesses, post-Hub product
rebuild, determinism, bounded work, exports, and distinct ownership. The existing
SV5_10 one-way sidepath allowance is unchanged. No Sector abstraction,
sector-derived RNG, Player claim, or SV5_12 implementation is introduced.
