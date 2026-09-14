# SV5_18_JUMP_CLEARANCE Result

TASK: SV5_18_JUMP_CLEARANCE
STATUS: PASS

## Outcome

Implemented deterministic body/head swept-clearance proofs for all 18 links
in the two immutable SV5_17 local recipes. The proof operates only on the
24×32 recipe cells and does not create or inspect world geometry.

## Endpoint correction

- Correction: `SV5_18_ENDPOINT_FIX01`
- Corrected effective takeoffs: 4
- Effective-link endpoint digest:
  `9ca58b3070662afece14b812aff892ec8e31bac09938a7445a667f3b2aa8db78`
- SV5_17 source, evidence, occupancy, links, and digests remained unchanged.
- `source_takeoff` and `effective_takeoff` are exported separately.

The four corrected pairs are `(14,4)->(13,4)`, `(9,4)->(10,4)`,
`(4,8)->(5,8)`, and `(19,8)->(18,8)` for R0/MIRROR_X link orders 2 and 6.

## Clearance result

- Input catalog digest:
  `19b4b3c37a1b1b11856eb1c946ce8ea64cbc16517dbfe9ace2ae66e4a5504022`
- Clearance digest:
  `48c3754b8109aa0601760567010b9aece67c5305907dfb76c7b155b3d4c37f13`
- Recipes: 2
- Links: 18/18 PASS
- Trace states: 90
- Explicit Grab contacts: 2
- Body/head occupancy failures: 0
- Disallowed diagonal corner collisions: 0
- Mirror mismatches: 0

Both `JS_LINK_03` traces prove `TAKEOFF -> AIR -> HANG -> PULL_UP` against the
exact exported SOLID Grab contact. Only that contact cell is allowed as the
occupied supercover corner during `HANG -> PULL_UP`; all other corner,
body, and head checks remain strict.

## Verification

- Required negative cases reproduced in local tests: blocked body, blocked
  head, diagonal clipping, wrong endpoint, reversal, overlong trace, excessive
  apex, missing contact, wrong face, missing HANG/PULL_UP, bad mirror, and
  forbidden readiness claims.
- Targeted final: 37/37 PASS, failed/skipped/inconclusive 0,
  NUnit duration 1.234342 seconds, XML SHA-256
  `6213985ba21c171a23ae80495b98eeabaafa2a42151492ff17dffeeb5340d1a5`.
- Targeted physical attempts: 2. The first found one test-only CRLF assertion
  mismatch at 36/37; the assertion was corrected without relaxing the product
  contract. The second is the accepted targeted result.
- Independent checker: `PASS_INDEPENDENT_JUMP_CLEARANCE`, 18 links, 90 trace
  states, 2 Grab contacts. Audit SHA-256
  `048614fa30d70a68f60bbecac7110a241e24373ce45f42320c42b6633e589683`.
- SVG render and visual inspection: PASS. SVG SHA-256
  `f49e19ab5606dd43bc746881bed250c1bf9f897e1e6c62a9c6bc95933c5e9d5c`;
  rendered PNG SHA-256
  `48fd0dee685198457f6602a3de7cf8fd2030a0faba518dde6d9f312dff69895d`.
- Full SV5 regression physical/accepted runs: 1/1.
- Full SV5 regression: 393/393 PASS, failed/skipped/inconclusive 0,
  NUnit duration 1753.3477866 seconds, Unity exit code 0.
- Full XML SHA-256:
  `659b778c152688d9e0461ba2104bb18224deb03edb01c0a15c1373e92519bbb0`.
- Full log SHA-256:
  `6dd67b93e75a853beb26bd036001b32f6c9c6f14903c3cc9efacea4781abf4d2`.
- Prior 356 fullname values were preserved exactly and the only additions were
  the 37 SV5_18 tests; the new 393-name set SHA-256 is
  `99ba3229875cf37e158b45185dc681dcde50942f77c173e16c77fc128a5bc790`.

## Boundaries

- `JumpRecipeReady=true`
- `ComposedGeometryReady=true`
- `SweptClearanceReady=true`
- `RecoveryReady=false`
- `PlayerVerified=false`
- Whole-world builds/searches and global endpoint comparisons: 0
- Player, scenes, prefabs, Packages, ProjectSettings, world placement, and
  SV5_13–17 source/evidence were not changed.
- SV5_19 remains LOCKED.
- Push performed: NO

