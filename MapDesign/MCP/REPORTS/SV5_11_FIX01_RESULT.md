# SV5_11_FIX01 Result

TASK: SV5_11_FIX01
TASK_ID: SV5_11_FIX01
STATUS: PASS

## Outcome

SV5_11_FIX01 preserves the SV5_11 24x40 Hub shell and replaces its synthetic
vertical-then-horizontal connection lines with bounded deterministic routes over
the actual 1x1 occupancy. Accepted centerline, head-clearance, and support changes
are applied to final Hub occupancy. Every changed cell is owned by
`HUB_CONNECTION_ACTUAL`; borrowed AIR remains unowned, and the external anchor is
the sole read-only foreign contact.

ProtectedAir, FixedSolid, Type0, progression gate, core, reservation, loop, and
sidepath collisions are checked from actual source cell sets. The protected,
Type0, and progression values stored on a connection are computed from its body
cells. Distinct external Room and SpaceGroup identities are preserved, and every
accepted route stores a cardinal `HUB_TO_EXTERNAL` movement witness.

Post-Hub movement, topology, and the complete nine-state by six-resource-order
physical product are rebuilt from final occupancy once per profile. The SV5_10
one-way sidepath rule is unchanged and no Sector abstraction was introduced.

## Final profiles

| Profile | Connections | Centerline cells | Changed cells | Distinct rooms/groups | Route attempts | Whole-world copy/BFS per candidate | Product runs | Build time |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| default | 4 | 230 | 329 | 4 / 4 | 4,096 | 0 / 0 | 1 | 23,680.927 ms |
| repeat | 5 | 231 | 370 | 5 / 5 | 4,096 | 0 / 0 | 1 | 21,320.795 ms |

Both profiles validate 9 legal states x 6 resource orders. Their final product
digests are `e9ad57c416c9a5c64a5e25938e8ebd6c431515f1942e66819e05d7ba0fc230ed`
(default) and
`341614d0c7df330629f9d66776dd307c592f713e50a2b3b96ba152a52ad3255c`
(repeat).

## Verification

- Final targeted Hub/FIX01: 30 / 30 PASS; failed/skipped = 0 / 0;
  duration 310.987297 seconds.
- Independent checker immediately after targeted:
  `PASS_INDEPENDENT_HUB_CONNECTIONS`.
- Complete SV5 regression: exactly 1 execution, 169 / 169 PASS;
  failed/skipped/inconclusive = 0 / 0 / 0; duration 1312.552318 seconds.
- Independent checker against the post-regression export tree:
  `PASS_INDEPENDENT_HUB_CONNECTIONS`.
- Registration bridge immediately before Finalize: `PASS_POST_READONLY`.

The independent audit directly reloads all eight bound constraint sources for
each profile and recomputes cardinal ordering, occupancy/head clearance,
changed-cell ownership, the external-anchor exception, protected intersections,
and validation counts. It reports default 4 connections/329 changed cells and
repeat 5 connections/370 changed cells with no errors.

## Evidence binding

- Base commit: `0ab603362b93047a45c6f30bac8e28d57ac57f93`
- Base parent: `3408a0ce540f0c9c947b5f2661f68122e06137c3`
- Registration manifest SHA-256:
  `e52a0e75636c09c6c11e63f75d69433b880bc856a78df7a05d4aaaa71af1ca00`
- Installed Task/Archive SHA-256:
  `73b3259eb68bd24884006a097c12bd4da58d03d63f5e06db64a48ba249b8b1a4`
- Targeted XML SHA-256:
  `b777e1f29a3360214d39c929ae1f609339ccaf2a1cd311a744aa0a648c9daac7`
- Focused XML SHA-256:
  `b5e3ac1c8bf50d8f0465ae058a75fd3b5451db286c054e03cdfbb6e501e3bc6a`
- Independent audit SHA-256:
  `4f2b940173fb45e973e0577e38af3ca605e7a0f2d4898612579e3cd9451ba9e8`
- FIX01 source tree SHA-256:
  `9667673ac489e93eec26dc56836bc5c041e87ee487e4a4b36f6c128344b66e7f`
- Final evidence tree SHA-256, excluding `BINDING.json` and `_work`:
  `ae66ad5f0fe0aa99aa7f74476dcd9d0124f2ce7c01d28c53e33f46f15a72c9a6`

The source/evidence tree digests hash sorted UTF-8 lines of the form
`<file-sha256>  <repo-relative-path>`. `BINDING.json` records the complete member
sets, profile digests, registration objects, and verification sequence.

## Scope and readiness

- `TreeGrabGeometryReady=false`
- `ComposedGeometryReady=false`
- `PlayerVerified=false`
- `SV5_12_TREE_GRAB` remains `LOCKED`; no SV5_12 work was started.
- No push was performed.
- Existing unrelated dirty changes were not modified, restored, deleted, or
  staged.
- Iteration-only `_work` diagnostics are excluded from the atomic commit and
  Review ZIP.
