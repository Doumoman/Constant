# SV5_12_FIX01 — expanded multi-stem Tree Grab canopy contract

## Defect being corrected

SV5_12 is technically valid but fails design review. Its runtime profile retains
tree_reserved_width=4; the resulting overview fits the major forks, three
endpoints, and seven platforms into a narrow column. One-cell lateral stubs pass
the numeric branch count while the silhouette still reads as a pillar or cactus.
Default and repeat overview/collision SVGs are byte-identical, so the promised
seed-bounded visual tree-form variation is not present.

This Task corrects that spatial and visual defect only. The valid SV5_12
collision roles, Hub handoff, itemless movement, recovery, physical product,
one-way allowance, and SV5_13 boundary remain.

## C01 — replace the four-cell reservation with a bounded tree envelope

- Retire tree_reserved_width=4 as the active canopy constraint.
- Use a default 10x24 tree envelope inside the existing 12x30 Hub inner volume.
- The accepted envelope is 8-10 cells wide and 22-26 cells high. It stays inside
  the Hub inner volume; local validation may inspect at most a two-cell margin.
- Envelope ownership is a placement budget, not SOLID fill. Required Hub side
  passages, active port necks, body/head clearance, and connection witnesses stay
  open.
- The lower root/trunk begins 4-5 cells wide, the middle trunk uses 2-4 cells,
  and each upper stem uses 1-2 cells. Band averages taper upward, but one-cell
  contour changes keep the silhouette irregular.
- No filled rectangle reaches 6x6. Do not solve the wider envelope by filling a
  wall and carving a cosmetic outline into the preview.

## C02 — substantial multi-generation stem graph

- The climbable geometry is represented as ordered limbs with explicit parent,
  generation, and cell-path identity. A limb path is cardinal, connected, and
  bound to actual TRUNK_CLIMB cells.
- Create at least three substantial forks. At least one is a second-generation
  fork: a non-root child limb must itself split into two or more child limbs.
- A child counts as substantial only when it advances at least three actual cells
  from its fork or leads to another fork. A one-cell side nub never counts.
- Produce 4-6 terminal climb endpoints. At least one substantial limb reaches
  left, one reaches right, and one continues upward off the root axis.
- Actual limb cells span at least eight columns and at least twenty rows.
  Relative to the root axis, limb geometry reaches at least three cells left and
  three cells right.
- Forbid a single uninterrupted central column from root to canopy. The longest
  straight main run remains at most five cells, and terminal limbs have unequal
  heights or lengths.

## C03 — branch platforms and collision semantics

- Place 7-10 BRANCH_PLATFORM groups at asymmetric heights and lengths. Exact
  same-height mirror pairs are forbidden.
- BRANCH_PLATFORM is landing/walking top-only support. Its ends, sides, and
  underside have no grab surface and must not appear in the grabbable-surface
  export.
- TRUNK_CLIMB remains the only grabbable tree role. Its exposed faces form one
  connected ladder-style climb graph.
- DECORATIVE_BRANCH and LEAF_DECORATION remain non-colliding, visual-only cells.
  They may enrich the crown but cannot satisfy limb reach, traversal, support,
  fork, or endpoint requirements.
- The lower tree is sparse; decorative branch and leaf density increases in the
  upper crown. Decoration must follow the actual stem graph rather than form an
  unrelated cloud around a narrow collision pillar.

## C04 — real itemless movement and Hub preservation

- Preserve the movement sequence: stand on a branch, jump to an exposed trunk
  face, initially grab with at most two cells of rise, climb continuously on the
  trunk graph, exit laterally, and land on another top-only branch.
- The two-cell cap applies to initial capture, not every subsequent climb edge.
- Provide at least two itemless lower-to-upper routes with different actual limb
  or contact sequences and recovery to at least three lower supported landings.
- Reverse completion is optional. Every recorded direction must match the actual
  witness; no item, flight, explosion, future ability, AIR-centerline alias, or
  decorative cell may be used.
- Preserve every active Hub entrance, exit, socket, port neck, external anchor,
  protected set, and the nine legal states x six resource orders physical product.

## C05 — deterministic variation without global search

- Same seed/profile/source digest/input order produces byte-stable geometry,
  evidence, and digests.
- Default and repeat must produce different canonical geometry projections and
  different overview SVG bytes while both satisfy the same limits.
- The canonical geometry projection excludes plan digest, timestamps, timing,
  input hashes, and other metadata-only fields. Metadata-only differences do not
  count as variation.
- Build limbs and platforms within the tree envelope. Do not compare all world
  endpoints, copy the 624x416 world per candidate, or run a world-sized BFS per
  limb/platform candidate.
- Do not restore Sector, 48x32 partitioning, SectorId, sector-pair packing, or
  sector-derived RNG. Per-profile final product validation still runs once.

## C06 — required exports and independent recomputation

Default and repeat each export under GENERATED/SV5_12_FIX01/profile:

- tree_canopy.json
- tree_cells.csv
- tree_limbs.csv
- tree_forks.csv
- tree_platforms.csv
- tree_surfaces.csv
- tree_movement_witness.csv
- tree_recovery_witness.csv
- tree_validation.json
- hub_validation.json
- preview/tree_grab_overview.svg
- preview/tree_collision_overlay.svg

tree_limbs.csv records limb ID, parent ID, generation, ordered actual cell path,
progress, fork continuation, and terminal status. tree_forks.csv records fork
ID, parent limb, generation, coordinate, child limb IDs, and substantial status.
tree_platforms.csv records platform ID, inclusive horizontal span, height,
top-only, grabbable, and landing status.

The checker recomputes envelope, occupied and limb span, left/right reach,
parent-child consistency, substantial forks, second-generation fork, terminal
endpoints, platform semantics and asymmetry, role safety, and canonical
default/repeat variation. It must not accept copied summary flags.

The previews show the full 12x30 Hub inner context, envelope boundary, root axis,
ordered limb IDs, fork markers, branch platforms, required ports, traversal, and
recovery. One tile is rendered at no less than 12 pixels. The legend explicitly
states BRANCH_PLATFORM — landing only; no grab.

## C07 — tests, review, and lifecycle

- Preserve all 199 predecessor SV5 tests and add at least 12 meaningful FIX01
  tests. Final full regression is at least 211 tests with zero failure, skip, or
  inconclusive result.
- Required failures include four-cell envelope, canopy span below eight, missing
  left/right reach, one-cell fake branch, fewer than three forks, no
  second-generation fork, endpoint count outside 4-6, mirrored platforms,
  grabbable branch platform, identical default/repeat geometry projection,
  identical overview SVG, blocked Hub port, and world-sized candidate work.
- Required successes include 10x24 envelope, substantial two-generation stem
  graph, asymmetric 7-10 platforms, two itemless routes, three recoveries,
  deterministic replay, different default/repeat forms, and the 9x6 product.
- Run compile and targeted Hub/Tree/FIX01 tests first, then the independent
  checker and visual inspection. Run the full SV5 regression exactly once only
  after those gates pass.
- Keep whole-world copy/BFS per candidate at zero and global product runs at
  one per profile. Each profile build remains within 45 seconds.
- On PASS set TreeCanopyFixReady=true. Preserve TreeGrabGeometryReady=true and
  keep ComposedGeometryReady=false and PlayerVerified=false.
- Finalize only with a matching PASS Result and one atomic Task-owned commit.
  Do not start SV5_13 or push.
