# SV5_12_TREE_GRAB Result

TASK: SV5_12_TREE_GRAB
TASK_ID: SV5_12_TREE_GRAB
STATUS: PASS

## Outcome

SV5_12 closes the two Hub handoff defects and compiles the reserved central
volume into deterministic, occupancy-backed tree geometry. Every accepted Hub
connection now has a distinct physical external anchor, and every connection
witness is a typed platformer traversal rather than an AIR centerline alias.

The tree uses four explicit roles. `TRUNK_CLIMB` forms one connected, continuous
ladder-style climb graph after an itemless initial jump/grab of at most two cells.
`BRANCH_PLATFORM` is top-only landing and walking support with no grab behavior
on its end, side, or lower face. `DECORATIVE_BRANCH` and `LEAF_DECORATION` are
non-colliding visual roles and are never counted as traversal.

The silhouette has a connected root footprint, a thick irregular lower trunk,
stepwise taper, four asymmetric major branch points, and three climb endpoints.
It has no central straight run longer than four cells. Two itemless ascent routes
use different actual trunk/contact sequences; three lower supported landings
provide fall recovery. Required Hub entrances, exits, port necks, and all
predecessor protection sets remain reachable and unobstructed.

## Final profile metrics

| Metric | default | repeat |
|---|---:|---:|
| Physical Hub connections / distinct anchors | 4 / 4 | 4 / 4 |
| Connection direction | 4 x `HUB_TO_EXTERNAL` | 4 x `HUB_TO_EXTERNAL` |
| Changed Hub connection cells | 289 | 143 |
| Lower / middle / upper average trunk width | 3.500 / 2.375 / 1.400 | 3.500 / 2.375 / 1.400 |
| Major branch points | 4 | 4 |
| Climbable trunk endpoints | 3 | 3 |
| Branch platforms | 7 | 7 |
| Branch grab predicates | 0 | 0 |
| Tree routes / recovery routes | 2 / 3 | 2 / 3 |
| Tree cells / solid cells | 169 / 93 | 169 / 93 |
| Initial jump/grab maximum rise | 2 cells | 2 cells |
| Required entrances / exits reachable | YES / YES | YES / YES |
| Tree generation time | 23 ms | 5 ms |
| Total profile build time | 36,176.852 ms | 39,786.487 ms |
| Whole-world copy / BFS per candidate | 0 / 0 | 0 / 0 |
| Physical product | 9 states x 6 orders PASS | 9 states x 6 orders PASS |

The default and repeat products have digests
`e9ad57c416c9a5c64a5e25938e8ebd6c431515f1942e66819e05d7ba0fc230ed`
and
`ac860be64e35c539fc5d991660c7cc99709ed6f04b260e965d19a0452ba4a111`.
Same-profile repeat generation is byte-stable. Profile seeds retain bounded
variation through their tree and plan digests.

## Verification

- Unity compilation gate: PASS (the authoritative Unity compile/test path loaded
  the project and completed the compile probe before targeted execution).
- Targeted Hub + Tree: 60 / 60 PASS; failed/skipped = 0 / 0; duration
  385.098946 seconds; XML SHA-256
  `662177a72db659e1f55ddfae193ac7afe18bd2938850d032c546ca56fa516351`.
- Independent checker, after targeted and before full regression:
  `PASS_INDEPENDENT_TREE_GRAB`; both profiles have 4 logical connections,
  4 distinct anchors, 4 typed connection witnesses, 2 tree routes, and
  3 recovery routes; audit SHA-256
  `f15bb18326a696fb80bd32d6973fb69f228483f02f349cb2c495c8a6ddd7a3dc`.
- Visual export audit: `PASS_VISUAL_EXPORT`; both overview and collision overlay
  parse as SVG and were raster-inspected. The overlay legend states that branch
  platforms are landing-only and cannot be grabbed. Audit SHA-256
  `82ffc05b0adb0c48863a36aad681d26d07d08b465b37dbf4bf66e795561c094d`.
- Full SV5 regression: exactly 1 execution after all prior gates; 199 / 199 PASS;
  failed/skipped/inconclusive = 0 / 0 / 0; duration 1,406.999434 seconds;
  XML SHA-256
  `3039f8076ad28f90087aca903fa874a44cb9433620c9da664ee0fa33f4f32a38`.
- Package bridge immediately before Finalize: `PASS_POST_READONLY`.

## Visual evidence

- `default/preview/tree_grab_overview.svg` and repeat equivalent show the full
  tapered asymmetric silhouette, root width, major stems, branches, and canopy.
- `default/preview/tree_collision_overlay.svg` and repeat equivalent distinguish
  `TRUNK_CLIMB`, `BRANCH_PLATFORM`, `DECORATIVE_BRANCH`, and
  `LEAF_DECORATION`, with entrances, exits, and traversal routes.
- Overview SHA-256:
  `c3be92572a6d4dab01756815a399de48bf1ffe50cc3ddfb012e7844eaefe50fb`.
- Collision overlay SHA-256:
  `e7814b3aade399fb4a804654e51ecc205968a436ca0a593034de739e4a32300e`.

## Evidence binding

- Base commit: `aeedc340670843d0bbeb43d3aad853a7953ba3b9`
- Base parent: `0ab603362b93047a45c6f30bac8e28d57ac57f93`
- Corrected package manifest SHA-256:
  `18c3549d8e494174962c3a1fef9126394ffc71a83f83100d6e7ee6313f88fc32`
- Installed Task/Archive SHA-256:
  `87f54b58c555632f0a11e2495b7351c8ac69b8ebb37240e1d56c97dd4b149204`
- Task source tree SHA-256:
  `714e8978b3744fce04177d9ee3df88c5f5aa5ab1d781c015e17107cfccad1d6e`
- Final evidence tree SHA-256, excluding `BINDING.json` and `_work`:
  `1c196117e8c062bddea59f5d4694ab9dcf37a8646ce48f077692a7fcdd66c2f5`

The tree digests hash sorted UTF-8 records of the form
`<file-sha256>  <repo-relative-path>`. `BINDING.json` records the member scope,
profile digests, verification sequence, and readiness values.

## Scope and readiness

- `TreeGrabGeometryReady=true`
- `ComposedGeometryReady=false`
- `PlayerVerified=false`
- `SV5_13_JUMP_CONTRACT` remains `LOCKED`.
- Sector, SectorId, and 48x32 partition concepts were not introduced.
- The SV5_10 one-way sidepath rule is unchanged.
- Per-terrain authoring settings UI is intentionally deferred. Its need is
  recorded separately in
  `REPORTS/MAP_GENERATOR_PER_TERRAIN_SETTINGS_UI_CONTRACT_NOTE.md`; no such UI was
  implemented by SV5_12.
- No push was performed.
- Existing unrelated dirty changes were not modified, restored, deleted, or
  staged. SV5_12-only `_work` diagnostics were removed after all gates passed.
