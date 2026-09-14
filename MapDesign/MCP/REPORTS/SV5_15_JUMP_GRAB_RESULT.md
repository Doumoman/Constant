# SV5_15_JUMP_GRAB Result

TASK: SV5_15_JUMP_GRAB
TASK_ID: SV5_15_JUMP_GRAB
STATUS: PASS

## Outcome

SV5_15 derives one deterministic 24x32 local jump-room fixture from the
accepted SV5_14 object graph. The canvas is not a Sector and has no world
origin. The implementation performed zero 624x416 world builds or searches,
does not compare global endpoints, and does not change Player tuning.

The SV5_14 support set remains ten supports and 27 actual 1x1 cells. Only
`JS04_SOLID` moves from origin `(14,5)` to `(14,6)`, so `(14,5)` and `(15,5)`
become AIR and `(14,6)` and `(15,6)` become SOLID. All nine route links are
rebuilt through `Sv5JumpContract.Measure`.

## Exact Grab witness

Exactly one route link uses Grab:

- Link: `JS_LINK_03`
- Source/target: `JS03_ONE_WAY -> JS04_SOLID`
- Direction: `RIGHT_TO_LEFT`
- Takeoff/landing: `(18,5) -> (15,7)`
- Recomputed `gap_air=2`, `rise=2`
- Edge: `JS04_RIGHT_GRAB`
- Contact: `(15,6)`, face `RIGHT`, target `JS04_SOLID`
- Hang body/head: `(16,6)` / `(16,7)`, both actual AIR
- Pull-up foot/head: `(15,7)` / `(15,8)`, both actual AIR
- Ordered witness: `TAKEOFF -> CONTACT_HANG -> PULL_UP -> LAND`

No `ONE_WAY` support is grabbable. No ladder or general wall-climb behavior is
introduced. Reverse completion is not required.

## Recomputed route measurements

| Order | Link | Mode | Direction | Takeoff | Landing | gap_air | rise |
|---:|---|---|---|---|---|---:|---:|
| 0 | JS_LINK_00 | JUMP | LEFT_TO_RIGHT | (2,2) | (6,3) | 3 | 1 |
| 1 | JS_LINK_01 | JUMP | LEFT_TO_RIGHT | (8,3) | (12,4) | 3 | 1 |
| 2 | JS_LINK_02 | JUMP | LEFT_TO_RIGHT | (14,4) | (18,5) | 3 | 1 |
| 3 | JS_LINK_03 | JUMP_GRAB | RIGHT_TO_LEFT | (18,5) | (15,7) | 2 | 2 |
| 4 | JS_LINK_04 | JUMP | RIGHT_TO_LEFT | (14,7) | (11,7) | 2 | 0 |
| 5 | JS_LINK_05 | JUMP | RIGHT_TO_LEFT | (9,7) | (6,8) | 2 | 1 |
| 6 | JS_LINK_06 | JUMP | RIGHT_TO_LEFT | (4,8) | (1,9) | 2 | 1 |
| 7 | JS_LINK_07 | JUMP | LEFT_TO_RIGHT | (1,9) | (3,10) | 1 | 1 |
| 8 | JS_LINK_08 | JUMP | LEFT_TO_RIGHT | (4,10) | (7,11) | 2 | 1 |

The resulting ranges are `gap_air=1..3` and `rise=0..2`. Supports remain
`STATIC_SCREEN`, links remain `PLANNED`, and the player-verified count remains
zero.

## Negative proof

The 26 local tests reject:

- Grab on `ONE_WAY`, hidden/non-exposed faces, non-`RIGHT` faces, and approach
  direction mismatch.
- Occupied hang body, hang head, pull-up foot, or pull-up head cells.
- Missing Grab edge, a normal jump carrying a Grab edge, and Grab rise above
  two cells.
- `PLAYER_VERIFIED` promotion in this stage.

The tests also cover exact JS04 translation, old-cell removal, contact
ownership, the ordered action witness, deterministic repeat, export
round-trip, readiness, and zero whole-world work.

## Verification

- Native Apply verification: `PASS_NATIVE_APPLY`.
- Unity 6000.3.8f1 compile: 0 errors.
- Targeted local suite: 26 / 26 passed; failed/skipped/inconclusive =
  0 / 0 / 0; duration `6.51` seconds; NUnit XML SHA-256
  `50656503c8f59f1484dfb015d84223dc41637ca7887b7a1bea3780bd756f200a`.
- Independent checker, after targeted and before visual/full gates:
  `PASS_INDEPENDENT_JUMP_GRAB`; audit SHA-256
  `e414e0e6c4a5d8d1901156256a6b94a4e946368ea63dc357f727595ca91a994f`.
- Chrome-rendered SVG visual inspection, after checker and before full suite:
  `PASS_VISUAL_INSPECTION`; audit SHA-256
  `e1dbcbc36e07dd35e0120ab045b2df3a085c69302a2c21da9e9adf1a1be4ec54`;
  SVG SHA-256
  `0a8eb8807fdd137be22536510c95c38b4fd5e231f6ea6f5f28bde2b10d450a2e`;
  rendered PNG SHA-256
  `ee8ca7820ad1013338710ad57144ef0b33c67107e4d1ba49a1b6acd1c610021c`.
- Full SV5 EditMode regression: exactly one execution after all earlier gates;
  278 / 278 passed; failed/skipped/inconclusive = 0 / 0 / 0; duration
  `1707.86` seconds; NUnit XML SHA-256
  `5903e10fdda3d14d8dc6adac42e546d2cc7e2b5602ff648a9505f8989a2a71a1`.
- Lifecycle pre-finalize check: `PASS_POST_READONLY`.

## Evidence binding

- Base commit: `ad952b3929259c1968ffd62ef60c3b99b9bed42e`
- Package manifest SHA-256:
  `033dd6665e5ac6b54ce80f2af6afc6fe37cd50458524f7ad59cea3d789fbe979`
- Installed Task/Archive SHA-256:
  `0cfad31cee048e6961500712fa2d2305a854323c073f10accdc3d6ad25f55547`
- Final BINDING SHA-256:
  `69e336113913e42e679bdf8de1f0cb210aac7c8a98479ad0e94ff052fed22912`
- Canonical fixture digest:
  `99c00ddaee7b1c0e7967d10560ddfad9b0f01dbc597f969466e3a03c16c760db`
- Jump-grab validation SHA-256:
  `e03b4ca4d5975628363337bbac0e55369a4b1fad71ea3ad8852f613ec0f9d155`

All JSON, CSV, SVG, and validation values derive from the canonical typed
object graph. The exact-blob review ZIP is built after the atomic commit so its
manifest can bind the resulting commit without a self-reference. The commit
title is `SV5_15_JUMP_GRAB: add exact solid-face grab segment`; its SHA and the
review ZIP SHA-256 are reported in the final handoff.

## Scope and readiness

- `JumpContractReady=true`
- `JumpSolidGeometryReady=true`
- `JumpGrabGeometryReady=true`
- `ComposedGeometryReady=false`
- `PlayerVerified=false`
- `SV5_16_JUMP_OUTLINE` remains `LOCKED`.
- Player runtime/tuning, SV5_13/SV5_14 source and evidence, tree/Hub geometry,
  world placement, scenes, prefabs, Packages, ProjectSettings, and Master were
  not modified or staged by this Task.
- The 1,429 observed dirty paths (including Apply and the untracked input
  package) were preserved; task-unrelated paths were not staged.
- No push was performed.
