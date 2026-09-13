# SV5 Tree Grab V5

SV5_12 owns the Hub-local central tree geometry. Generation is limited to the
registered 4×24 reservation plus a two-cell local margin. It does not use Sector,
48×32 partitioning, all-endpoint pairing, or a world-sized search per branch.

## Silhouette

The lower trunk is the widest part of the tree and begins with a connected root
silhouette at least three cells wide. Main-trunk width tapers in stages through
lower, middle, and upper bands. One-cell contour offsets and seed-bounded branch
variation prevent a rectangle, exact bilateral symmetry, or a single central
line extending through more than 40 percent of the tree height.

The climbable trunk graph has multiple branch points and multiple endpoints. A
left stem, a right stem, and an off-centre upper stem have different heights and
lengths. Decorative branches and leaf clusters become denser toward the upper
crown without contributing collision or movement cells.

## Collision roles

- `TRUNK_CLIMB` is solid trunk contact geometry with a connected ladder-style
  climb network. After the initial itemless jump and grab, the player may move
  upward or downward on that network without reapplying the two-cell capture cap.
- `BRANCH_PLATFORM` is top-only landing and walking support. Its ends, sides,
  and undersides are not grab surfaces.
- `DECORATIVE_BRANCH` is visual-only geometry with no collision or movement role.
- `LEAF_DECORATION` is visual-only crown geometry with no collision or movement
  role.

Only exposed `TRUNK_CLIMB` faces appear in `tree_surfaces.csv`. No
`BRANCH_PLATFORM` cell may appear there. The first branch-to-trunk capture is an
actual `JUMP_GRAB` with a rise of one or two cells. Subsequent route edges carry
the explicit `TRUNK_CLIMB` traversal state, and the route exits laterally onto a
top-only branch platform. Required Hub ports remain body/head AIR.

## Evidence boundary

`tree_cells.csv` records collision role, top-only, grab, movement, and visual-only
flags. `tree_climb_network.csv` records the continuous climb portion. The overview
shows taper, branch points, and crown density; the collision overlay distinguishes
all four roles and explicitly identifies branch platforms as landing-only and
non-grabbable. `TreeGrabGeometryReady` may become true after static evidence and
the independent audit pass, while `ComposedGeometryReady` and `PlayerVerified`
remain false.
