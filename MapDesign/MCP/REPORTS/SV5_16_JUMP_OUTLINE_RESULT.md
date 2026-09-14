# SV5_16_JUMP_OUTLINE Result

TASK: SV5_16_JUMP_OUTLINE
TASK_ID: SV5_16_JUMP_OUTLINE
STATUS: PASS

## Outcome

SV5_16 derives one deterministic 24x32 local jump-room fixture from the
accepted SV5_15 object graph. The canvas is not a Sector and has no world
origin. The implementation performed zero 624x416 world builds or searches,
does not compare global endpoints, and does not change Player tuning.

All ten SV5_15 supports, their 27 actual 1x1 cells, all nine ordered route
links, and the single accepted Grab edge remain fixed. Exactly 17 new SOLID
outline cells extend downward beneath five SOLID supports, producing exactly
44 final occupied cells. No backing cell is added beneath a ONE_WAY support.

## Exact deformation profile

Depth means the count of new cells emitted downward from `support.y - 1`.

| Support | Column depths | Added cells |
|---|---|---:|
| `JS00_ENTRY_SOLID` | `x0=1, x1=1, x2=0` | 2 |
| `JS02_SOLID` | `x12=1, x13=2, x14=1` | 4 |
| `JS04_SOLID` | `x14=2, x15=1` | 3 |
| `JS06_SOLID` | `x4=2, x5=2, x6=1` | 5 |
| `JS08_SOLID` | `x3=2, x4=1` | 3 |
| **Total** | depth range `0..2` | **17** |

The final occupancy is the disjoint union `27 base + 17 outline = 44`.
There are zero filled SOLID 6x6 windows and no rectangular room shell.

## Preserved route and Grab witness

- Route links: 9, unchanged; all 10 route body/head clearance cells remain AIR.
- Grab links/edges: 1 / 1; new automatic Grab edges: 0.
- Link: `JS_LINK_03`, `JS03_ONE_WAY -> JS04_SOLID`, `RIGHT_TO_LEFT`.
- Takeoff/landing: `(18,5) -> (15,7)`; `gap_air=2`, `rise=2`.
- Edge: `JS04_RIGHT_GRAB`; contact `(15,6)`, face `RIGHT`.
- Hang body/head: `(16,6)` / `(16,7)`; pull-up foot/head:
  `(15,7)` / `(15,8)`; all required Grab-space cells remain AIR.
- Ordered witness remains `TAKEOFF -> CONTACT_HANG -> PULL_UP -> LAND`.

## Negative proof

The 32 local tests reject missing, extra, wrong, and duplicate depth entries;
missing, extra, duplicate, overlapping, out-of-bounds, ONE_WAY-owned, and
isolated outline cells; support/route mutation; blocked route or Grab air;
new automatic Grab edges; filled SOLID 6x6 windows; rectangular shells; and
premature composition or Player verification. They also prove deterministic
export and that the local suite performs no world generation or search.

## Verification

- Native Apply verification: `PASS_NATIVE_APPLY`.
- Unity 6000.3.8f1 compile: 0 errors.
- Targeted local suite: 32 / 32 passed; failed/skipped/inconclusive =
  0 / 0 / 0; duration `7.35` seconds; NUnit XML SHA-256
  `ff2a4172647022cb8d4fb6a4659b84f7e25623e194c8fc99ececa0dc336439a4`.
- Independent checker, after targeted and before visual/full gates:
  `PASS_INDEPENDENT_JUMP_OUTLINE`; audit SHA-256
  `70c0de14e70560e194fab0d5c46daf133506ead1b79db0690062b1aa693deed5`.
- Chrome-rendered SVG visual inspection, after checker and before full suite:
  `PASS_VISUAL_INSPECTION`; audit SHA-256
  `ed2a73d12341871c68516e3de642d71dfd92a5ff474dba5eb20cca3086a1cba6`;
  SVG SHA-256
  `72fb03f995569e6c8c273d04f35047be32ce4ad223cb1e7202ac0705c5f0b3e6`;
  rendered PNG SHA-256
  `912d7bb5c8ef6149be2be336f9d0cfdc304e123bfe0816184996beb748a38d82`.
- Full SV5 EditMode regression: exactly one execution after all earlier gates;
  official Unity collector result 310 / 310 passed;
  failed/skipped/inconclusive = 0 / 0 / 0; duration `1690.6510323` seconds;
  NUnit XML SHA-256
  `f6f532f00fadfebfa941683b9afc0a1310eaf0974ef72446904f0e6db2a1fa02`.
  The local API wrapper timed out after Unity had saved this completed result;
  no retry or second full-regression execution was performed.
- Lifecycle pre-finalize check: `PASS_POST_READONLY`.

## Evidence binding

- Base commit: `289015d201186fe79da733a5fe99fdb6844047fb`
- Package manifest SHA-256:
  `c22b159aa620f160286e1c2e837d6ecc9f7b97b3bf5de01caccc10c0dfca1d1b`
- Installed Task/Archive SHA-256:
  `282a3f2483f6a1e8ed0b2de1a34833747b4d06536954147a8250df7e374a325c`
- Final BINDING SHA-256:
  `8bc08547e10cbdf202a248f21ce2704145194e301ec8d1af16f4fd845197e68c`
- Canonical fixture digest:
  `0ac93bf0ddc1a203baa47517096516c2df98d67978d3c80131a13edcedd5df9c`
- Jump-outline validation SHA-256:
  `44ecf1bab16ad13d9ee625379d97ef28d747b9f0ec0b7c13d9bf5a277ff394af`

All JSON, CSV, SVG, and validation values derive from the canonical typed
object graph. The exact-blob review ZIP is built after the atomic commit so its
manifest can bind the resulting commit without a self-reference. The commit
title is `SV5_16_JUMP_OUTLINE: add exact irregular solid backing`; its SHA and
the review ZIP SHA-256 are reported in the final handoff.

## Scope and readiness

- `JumpContractReady=true`
- `JumpSolidGeometryReady=true`
- `JumpGrabGeometryReady=true`
- `JumpOutlineReady=true`
- `ComposedGeometryReady=false`
- `PlayerVerified=false`
- `SV5_17_JUMP_RECIPES` remains `LOCKED`.
- Player runtime/tuning, SV5_13-15 source and evidence, tree/Hub geometry,
  world placement, scenes, prefabs, Packages, ProjectSettings, and Master were
  not modified or staged by this Task.
- The 1,438 observed dirty paths (including Apply and the untracked input
  package) were preserved; task-unrelated paths were not staged.
- No push was performed.
