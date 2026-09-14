# SV5_13_JUMP_CONTRACT Result

TASK: SV5_13_JUMP_CONTRACT
TASK_ID: SV5_13_JUMP_CONTRACT
STATUS: PASS

## Outcome

SV5_13 establishes an immutable local data contract for jump supports, exact
boundary measurements, movement links, real SOLID Grab contacts, clearance
facts, and validation states. It creates no jump room and performs no complete
world generation or search.

`SOLID` and `ONE_WAY` remain distinct. ONE_WAY contributes top-only support and
cannot own a Grab edge. A Grab contact must belong to the target active SOLID's
actual exposed facing boundary, and its contact cell, hanging-body cell, and
pull-up destination are recorded separately. Missing exposure, hanging-body
clearance, pull-up space, active-route use, or safe material produces an
explicit rejection reason.

For left-to-right links, `gap_air = target_left_x - source_right_x - 1`.
For right-to-left links, `gap_air = source_left_x - target_right_x - 1`.
Overlap clamps to zero. `rise = target_top_y - source_top_y`; origin, center,
pivot, Euclidean, and path distances are not substitutes. Normal upward JUMP
is capped at one cell and JUMP_GRAB at two cells.

## Canonical local fixtures

| ID | Direction | Source kind | Target kind | Takeoff | Landing | gap_air | rise | Mode | State |
|---|---|---|---|---|---|---:|---:|---|---|
| J1 | LEFT_TO_RIGHT | SOLID | ONE_WAY | (1,1) | (5,2) | 3 | 1 | JUMP | PLANNED |
| J2 | RIGHT_TO_LEFT | ONE_WAY | SOLID | (14,1) | (9,2) | 4 | 1 | JUMP | PLANNED |
| J3 | LEFT_TO_RIGHT | ONE_WAY | SOLID | (21,1) | (24,3) | 2 | 2 | JUMP_GRAB | PLANNED |

J3 targets `J3_TARGET_SOLID`. Its exposed left-face contact is `(24,2)`, its
hanging-body cell is `(23,2)`, and its pull-up destination is `(24,3)`.

The canonical fixture contains six supports: three active SOLID and three
active ONE_WAY. It contains one safe Grab edge, three links, three measurement
proofs, and ten validation-state records. State counts are PLANNED 3,
STATIC_SCREEN 7, and PLAYER_VERIFIED 0. One-way links do not invent a reverse
completion requirement.

## Player constants observed without modification

The active SV5/RMAP02 fixture contract remains a `0.4×0.8` Player with a
foot-bottom pivot. Current live movement settings remain walk/run `2.8/5.5`,
air acceleration/cap `40/5.5`, jump velocity `7.2`, rise/fall gravity `20/26`,
maximum fall speed `20`, coyote/buffer `0.08/0.10`, and release cut `0.42`.

Grab settings remain probe `0.35`, vertical window `0.45`, side offset `0.21`,
hang offset `0.62`, reentry delay `0.12`, and maximum upward capture velocity
`0.01`. Existing safe surface kinds remain StaticSafe, DestructibleSafe, and
MovingSafe; ONE_WAY is not Grab-safe. The separate core capsule baseline
`0.72×0.90` was observed and left unchanged. Existing physical measurements
`1.237` held-jump height and `3.425` run distance remain reference evidence,
not a guarantee that every J1/J2 candidate is Player-completable.

## Verification

- Native Apply verification: `PASS_NATIVE_APPLY`.
- Unity compile/import and targeted local fixture suite: 20 / 20 passed;
  failed/skipped/inconclusive = 0 / 0 / 0; duration `0.8849065` seconds;
  NUnit XML SHA-256
  `ca400162d65cf1ea136a6d6c9a8b02cd0b402a9848494dc9cb9b0bdc5c851423`.
- New targeted tests build and search the complete world zero times.
- Independent checker, run after targeted and before the full regression:
  `PASS_INDEPENDENT_JUMP_CONTRACT`; audit SHA-256
  `d4565aa84218fcac27efccd5e67fca91605809efb72df3b71ffe828ad6b36da6`.
- Full SV5 EditMode regression: exactly one execution after the earlier gates;
  231 / 231 passed; failed/skipped/inconclusive = 0 / 0 / 0; duration
  `1379.470369` seconds; NUnit XML SHA-256
  `46949de4f0b6f39502ab8f08455c97e44d63fa32bb11277a06204f406421baa8`.
- Lifecycle pre-finalize check: `PASS_POST_READONLY`.

## Evidence binding

- Base commit: `5e09c37146c21e16bb165ebdd47745205e5a0381`
- Package manifest SHA-256:
  `1e2e9b5952976f6380b5db23742b905e8168f9e4a18b1cba886f99d5f1ae7cdf`
- Installed Task/Archive SHA-256:
  `3fe70921236563d0e4f9d361f31b172551ddf712d1a7668921c0fc9dcd7a412c`
- Final BINDING SHA-256:
  `a56f348f5ea3e75450b6948a4c92fb2d63fcb10813e8524e649e36bd6abf43e4`
- Canonical fixture digest:
  `4f45bcf9464eac2c34df6943adb0d1a27c926dcdd22e89489f1983adcc78d330`
- Contract validation SHA-256:
  `388d11310fc617bd22560ebe2f5119dfdcf632db5275506e9e02328f0e88851e`

All JSON, CSV, validation, and SVG values are emitted from the canonical typed
objects. The exact-blob review archive is built only after the atomic commit so
its manifest can bind the resulting commit without self-reference. The commit
title is `SV5_13_JUMP_CONTRACT: define exact jump support contract`; its SHA and
the review ZIP SHA-256 are reported in the final handoff.

## Scope and readiness

- `JumpContractReady=true`
- `JumpSolidGeometryReady=false`
- `JumpGrabGeometryReady=false`
- `ComposedGeometryReady=false`
- `PlayerVerified=false`
- `SV5_14_JUMP_SOLID` remains `LOCKED`.
- Player runtime, Player tuning, the predecessor tree and Hub sources/evidence,
  scenes, prefabs, Packages, ProjectSettings, Master, and unrelated dirty files
  were not modified by this Task.
- No push was performed.
