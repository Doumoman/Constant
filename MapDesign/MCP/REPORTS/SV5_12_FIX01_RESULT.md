# SV5_12_FIX01 Result

TASK: SV5_12_FIX01
TASK_ID: SV5_12_FIX01
STATUS: PASS

## Outcome

SV5_12_FIX01 retires the predecessor's active 4x24 tree constraint and builds a
bounded 10x24 multi-stem canopy inside the existing 12x30 Hub inner volume. The
actual climb graph spans 10 columns and 24 rows, reaches at least four cells to
both sides of its root axis, contains four substantial forks (three whose parent
is already a child limb), and terminates at five climb endpoints.

The lower trunk is five cells wide at the root and its lower/middle/upper
contiguous band averages are 4.750 / 3.125 / 3.125. Individual terminal stems
remain one or two cells thick, the longest straight limb run is four cells, and
no 6x6 solid fill or uninterrupted root-to-canopy central column exists.

Nine asymmetric `BRANCH_PLATFORM` groups provide top-only landing support.
Branch end, side, and underside grab predicates remain zero. `TRUNK_CLIMB` is
the only grabbable tree role; decorative branches and leaves remain visual-only.
Two distinct itemless ascent witnesses capture a real trunk face within the
two-cell initial rise cap, and three required heights have supported recovery
witnesses.

Default and repeat use different bounded canopy variants. Their canonical tree
geometry digests and overview SVG bytes differ while same-profile generation is
deterministic. Required Hub ports, connection witnesses, distinct external
anchors, protected ownership, and the 9-state x 6-resource-order product remain
valid.

## Final profile metrics

| Metric | default | repeat |
|---|---:|---:|
| Tree envelope | 10x24 | 10x24 |
| Root width | 5 | 5 |
| Lower / middle / upper band average | 4.750 / 3.125 / 3.125 | 4.750 / 3.125 / 3.125 |
| Limb span / left reach / right reach | 10 / 4 / 5 | 10 / 5 / 4 |
| Substantial / second-generation forks | 4 / 3 | 4 / 3 |
| Climb endpoints | 5 | 5 |
| Branch platforms / branch grabs | 9 / 0 | 9 / 0 |
| Itemless routes / recoveries | 2 / 3 | 2 / 3 |
| Tree generation time | 4 ms | 5 ms |
| Total profile build time | 33,113.737 ms | 36,859.300 ms |
| Whole-world copy / BFS per candidate | 0 / 0 | 0 / 0 |
| Physical product | 9 states x 6 orders PASS | 9 states x 6 orders PASS |

Required entrances and exits are reachable in both profiles. The physical
product digests remain
`e9ad57c416c9a5c64a5e25938e8ebd6c431515f1942e66819e05d7ba0fc230ed`
and
`ac860be64e35c539fc5d991660c7cc99709ed6f04b260e965d19a0452ba4a111`.

## Verification

- Predecessor rejection: the initial 12-test FIX01 fixture produced the expected
  six failures for 4x24 envelope, five-column canopy, missing reach, three
  endpoints, identical relative geometry, and the legacy schema.
- Final targeted Hub + Tree + FIX01: 72 / 72 PASS; failed/skipped/inconclusive =
  0 / 0 / 0; duration 371.156695 seconds; XML SHA-256
  `513495ed28e74ed5c0ea219ae591f4e8b2363752eb7aa9ff3496642b69eeb808`.
- Independent checker after targeted:
  `PASS_INDEPENDENT_TREE_CANOPY_FIX`; audit SHA-256
  `53b3ddd45b827249509dd396dab2ae6f8505fac0ede7825b7ae901c5829f8aa4`.
- Visual export: `PASS_VISUAL_EXPORT`; default/repeat overview and collision
  overlays were rendered at 12 pixels per tile and inspected. They show the Hub
  inner boundary, envelope, root axis, limb IDs, forks, ports, ascent/recovery
  routes, and the landing-only/no-grab branch legend. Audit SHA-256
  `8455ae36d0b2d128ff7dda1456ec831317107099a05e8a77b67315cff770532d`.
- Full SV5 regression: exactly one execution after all prior gates; 211 / 211
  PASS; failed/skipped/inconclusive = 0 / 0 / 0; duration 1,338.259079 seconds;
  XML SHA-256
  `147389507b438d73c2e7b405bdec8bf5f162bdd23c7cf42733e6a5d833b96d53`.
- Registration bridge immediately before Finalize: `PASS_POST_READONLY`.

## Evidence binding

- Base commit: `3f05858cb80d6437e89123fb6b6dd30a395996fc`
- Base parent: `aeedc340670843d0bbeb43d3aad853a7953ba3b9`
- Registration manifest SHA-256:
  `4e105776c43e7172cce825ea0d1edf439c620f63567135a9a883c41360b65a5f`
- Installed Task/Archive SHA-256:
  `3459698bfab18442d96ce535dbab6cbfc335d32c61af3f0baafbba955163ddd0`
- FIX01 source tree SHA-256:
  `33717da8d1f77e259ce120ae42fe0f8ba2102e014d76f5fe9e10ae5ca9f32598`
- Evidence tree SHA-256, excluding `BINDING.json`, `_work`, and ZIP files:
  `7afaec2f4b65e478a6bcea5b7374d42ec2e66e86bca9e5161ff91ff90266f79f`

The tree digests hash sorted UTF-8 records of the form
`<file-sha256>  <repo-relative-path>`. `BINDING.json` records the member scope,
registration objects, ordered gates, profile digests, and readiness values.

## Scope and readiness

- `TreeCanopyFixReady=true`
- `TreeGrabGeometryReady=true`
- `ComposedGeometryReady=false`
- `PlayerVerified=false`
- `SV5_13_JUMP_CONTRACT` remains `LOCKED`.
- Sector, SectorId, 48x32 partitioning, world-sized candidate traversal, and
  all-endpoint-pair comparison were not introduced.
- The deferred per-terrain settings UI note was not modified.
- No push was performed, and unrelated dirty changes were not modified,
  restored, removed, or staged.
