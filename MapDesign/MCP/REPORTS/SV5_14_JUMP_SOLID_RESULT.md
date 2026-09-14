# SV5_14_JUMP_SOLID Result

TASK: SV5_14_JUMP_SOLID
TASK_ID: SV5_14_JUMP_SOLID
STATUS: PASS

## Outcome

SV5_14 creates one deterministic 24×32 local jump-room geometry. This canvas
is not a Sector, has no authoritative world origin, and was neither placed in
nor searched across the 624×416 world. The implementation consumes
`Sv5JumpContract.Measure` for every required route link and does not redefine
SV5_13 support kinds, measurement formulas, or validation states.

The canonical fixture contains ten required supports and 27 actual 1×1 support
cells. Five supports are full-collision `SOLID` and five are top-only
`ONE_WAY`. Every SOLID is active, non-decorative, and used as an actual takeoff
or landing. An explicit lower entry socket begins at `JS00_ENTRY_SOLID`; an
explicit upper exit socket ends at `JS09_EXIT_ONE_WAY`.

## Ordered ascent route

The ten-support, nine-link one-direction ascent spans nine top-surface cells.
Every link is `JUMP`, has `rise=+1`, and has facing-edge `gap_air` in `1..3`.
All takeoff and landing coordinates lie on the corresponding inclusive top
support intervals. SOLID use is:

| Support | Takeoffs | Landings | Counted |
|---|---:|---:|---|
| JS00_ENTRY_SOLID | 1 | 0 | yes |
| JS02_SOLID | 1 | 1 | yes |
| JS04_SOLID | 1 | 1 | yes |
| JS06_SOLID | 1 | 1 | yes |
| JS08_SOLID | 1 | 1 | yes |

The route deliberately does not invent reverse completion. It creates zero
Grab edges and zero `JUMP_GRAB` links; the genuine Grab segment remains owned
by SV5_15.

## Cell and clearance proof

- Support rectangles/cells: `10 / 27`
- Active required SOLID/ONE_WAY: `5 / 5`
- Required route supports/links: `10 / 9`
- Route vertical span: `9`
- Recomputed `gap_air` range: `1..3`
- Recomputed `rise` range: `1..1`
- Required support clearance records: `10`, passing: `10`
- Occupied 6×6 all-SOLID windows: `0`
- Whole-world builds/searches in new targeted tests: `0 / 0`

Each clearance record uses a supported foot coordinate plus the two reserved
AIR positions represented by its body/top coordinate and the cell one row
above. The independent checker validates them against final local occupancy.

## Verification

- Native Apply verification: `PASS_NATIVE_APPLY`.
- Unity 6000.3.8f1 import/compile and targeted local suite: 21 / 21 passed;
  failed/skipped/inconclusive = 0 / 0 / 0; duration `0.9403321` seconds;
  NUnit XML SHA-256
  `d978b94b10d162bdad2523ff53b79a79887277bf41b35eaf5f495c4862d6b521`.
- The targeted suite reproduces all-ONE_WAY, decorative/unreachable SOLID,
  cell mismatch, overlap/bounds/6×6, route order/support/gap/rise/span,
  clearance, determinism, export, and valid mixed-route cases using local
  fixtures only.
- Independent checker, after targeted and before visual/full gates:
  `PASS_INDEPENDENT_JUMP_SOLID`; audit SHA-256
  `ab41f1fd729087c22ea4f3b5557c6bc54680569c807eec8fbbbf3a3c2707d152`.
- Chrome-rendered SVG visual inspection, after checker and before full suite:
  `PASS_VISUAL_INSPECTION`; audit SHA-256
  `947f9f2ddd4deeb976096f26dd1bb759cec686573bca33da6e9d9f753f687e5b`;
  SVG SHA-256
  `2bd1cc9031dbe816704df337edaa5f1268ba4bf1a316fbb78a2d98c052dc834d`;
  rendered PNG SHA-256
  `545529c4bfbc3f1d4960276a31f48d0c3f26713ecd38319109be714d082d1e55`.
- Full SV5 EditMode regression: exactly one execution after all earlier gates;
  252 / 252 passed; failed/skipped/inconclusive = 0 / 0 / 0; duration
  `1430.6319121` seconds; NUnit XML SHA-256
  `73061f5a5ac2a231ee9b86f91c209d54a83d776141fa2e7e8a4627a19044f00e`.
- Lifecycle pre-finalize check: `PASS_POST_READONLY`.

## Evidence binding

- Base commit: `33a635905345ff2a42facc3add5c6609eea9357c`
- Package manifest SHA-256:
  `ba726c6e68121d6f68cd0ac8da13b3d80740a6c25fa97f6a2e2a1357d959e8a8`
- Installed Task/Archive SHA-256:
  `3cb140ff326f5bfb5458435d465f6128d1b624eb3fdb881fd1e36c3966cc9a58`
- Final BINDING SHA-256:
  `d77472d93714b0e3bc63d74b83c4cd63a2719fb6fddb74aa1786814d532fab83`
- Canonical fixture digest:
  `8f693abdb50dd4d31c9a69840f379936e2a28faf646681698e05adb725fc3d6e`
- Jump-solid validation SHA-256:
  `4c3d677c00c1e8dec2d77cab5b97f33c035cf9b6e52a19ed8042969b584569fe`

All JSON, CSV, SVG, and validation values derive from the canonical typed
object graph. The exact-blob review ZIP is built after the atomic commit so its
manifest can bind the resulting commit without a self-reference. The commit
title is `SV5_14_JUMP_SOLID: build mixed-support local ascent route`; its SHA
and the review ZIP SHA-256 are reported in the final handoff.

## Scope and readiness

- `JumpContractReady=true`
- `JumpSolidGeometryReady=true`
- `JumpGrabGeometryReady=false`
- `ComposedGeometryReady=false`
- `PlayerVerified=false`
- `SV5_15_JUMP_GRAB` remains `LOCKED`.
- Player runtime/tuning, SV5_13 contract source/evidence, earlier tree/Hub
  geometry, world placement, scenes, prefabs, Packages, ProjectSettings,
  Master, and the 1,385 observed unrelated dirty paths were not modified or
  staged by this Task.
- No push was performed.
