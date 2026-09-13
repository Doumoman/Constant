# SV5_12_FIX01 Tree Canopy Geometry Contract

## Scope

`SV5_12_FIX01` replaces the predecessor's narrow 4x24 tree reservation with a
bounded multi-stem canopy. It does not start `SV5_13_JUMP_CONTRACT`, change
player runtime, reintroduce Sector partitioning, or implement per-terrain UI.

## Local generation envelope

- The default tree envelope is 10x24 cells and must remain within the Hub's
  12x30 inner volume.
- Allowed envelope sizes are 8..10 cells wide and 22..26 cells high.
- Geometry and movement inspection is limited to the envelope plus a two-cell
  margin. No whole-world candidate copy, whole-world BFS, all-endpoint-pair
  comparison, `SectorId`, or 48x32 Sector subdivision is permitted.
- The envelope is an ownership/search bound, not a request to fill the volume
  with SOLID cells.

## Canopy and collision roles

- `TRUNK_CLIMB` forms one connected climbable graph. The bottom three rows form
  a 4..5-cell root silhouette, the middle narrows, and terminal stems are only
  1..2 cells thick.
- The graph contains at least three substantial forks, at least one fork whose
  parent is already a child limb, and four to six distinct terminal endpoints.
- Actual limb paths span at least eight columns and twenty rows, reaching at
  least three cells to each side of the root axis. A limb child advances at
  least three cardinal cells or reaches a further fork. No straight limb run
  exceeds five cells.
- `BRANCH_PLATFORM` is top-only landing support. Seven to ten asymmetric
  platform records are required. Its ends, sides, and underside never export a
  grab surface; branch grab count remains zero.
- `DECORATIVE_BRANCH` and `LEAF_DECORATION` are visual-only AIR overlays. Their
  density increases toward the upper crown and they never count as movement.

## Movement evidence

The two itemless ascent witnesses start on different lower platforms, capture
an actual exposed `TRUNK_CLIMB` face with a rise of at most two cells, continue
through the connected climb graph, and leave toward upper support. They use
different contact sequences. Reverse completion is not required. Three
required heights have explicit fall-recovery witnesses ending above real solid
support with body and head AIR.

Hub ports, port necks, connection movement cells, external anchors, protection
sets, and the 9-state by 6-order physical product remain valid and unobstructed.

## Determinism and evidence

The same input plan produces byte-stable geometry and exports. The default and
repeat profiles deliberately choose different bounded canopy variants, so their
canonical tree-cell geometry and overview SVG bytes must differ while both
satisfy this contract.

Each profile exports `tree_canopy.json`, tree cells, ordered limbs, forks,
platforms, grab surfaces, ascent/recovery witnesses, validation JSON, Hub
validation, and both overview/collision SVGs. The collision legend explicitly
states that branch platforms are landing-only and have no grab behavior.

`TreeCanopyFixReady` and `TreeGrabGeometryReady` may be true only when all local
diagnostics pass. `ComposedGeometryReady` and `PlayerVerified` remain false.
