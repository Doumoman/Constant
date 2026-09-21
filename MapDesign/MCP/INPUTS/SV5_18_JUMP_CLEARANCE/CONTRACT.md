# SV5_18 — local cell-space jump clearance

## Purpose

Prove that every ordered link in the accepted SV5_17 jump recipes has enough
empty 1×1 cells for the player's feet, body, and head to pass from takeoff to
landing. This Task validates a small 24×32 local room. It does not simulate the
live Player motor and does not inspect or generate the whole world.

## C01 — frozen input and scope

- Inputs are exactly `JUMP012_MIXED_R0` and `JUMP012_MIXED_MX` from the accepted
  SV5_17 catalog digest
  `19b4b3c37a1b1b11856eb1c946ce8ea64cbc16517dbfe9ace2ae66e4a5504022`.
- Preserve both recipes: 10 supports, 44 occupied cells, 9 ordered links,
  4 ordered segments, and 1 explicit Grab edge per recipe.
- Preserve the 7 `+1 JUMP`, 1 `+2 JUMP_GRAB`, and 1 level `JUMP` mixture.
- Coordinates remain local integer cells with `0<=x<24` and `0<=y<32`.
- Do not use Sector, 48×32 partitioning, global endpoint pairs, world placement,
  or a 624×416 search surface.

## C02 — player clearance footprint

- A trace state is identified by a foot/body cell `(x,y)` and a head cell
  `(x,y+1)`. Both must be inside the local room and unoccupied.
- Takeoff and landing coordinates in `recipe_links.csv` are standing foot/body
  cells. Their supporting tile is below and is not part of the body footprint.
- SOLID and TOP_ONLY cells are both occupied for body/head overlap checks.
- Every trace starts at the exact takeoff and ends at the exact landing.
- Consecutive trace states move by at most one cell on each axis, are not
  duplicates, and never reverse the link's horizontal direction.
- A diagonal step additionally requires the two orthogonal corner alternatives,
  and their head cells, to be clear. This conservative supercover rule prevents
  corner clipping between samples.
- Each trace contains at most 16 states. Its highest body cell is no more than
  three cells above the higher endpoint.
- This is a discrete occupancy proof, not a claim about gravity, velocity,
  animation, input timing, coyote time, or Rigidbody tuning.

## C03 — normal jump links

- Each normal `JUMP` link has one deterministic ordered trace over final recipe
  occupancy.
- The trace may use only `TAKEOFF`, `AIR`, and `LANDING` phases.
- No normal jump may claim Grab contact or gain more height than its accepted
  link rise.
- The seven `+1` links and one level link must all pass independently in both
  recipe variants.

## C04 — two-cell jump-and-grab link

- `JS_LINK_03` is the only `JUMP_GRAB` link in each recipe and its rise is
  exactly two cells.
- Its ordered trace contains the exact SV5_17 Grab states:
  `TAKEOFF -> AIR... -> HANG -> PULL_UP`.
- `HANG` uses the exported hang-body/head cells. `PULL_UP` uses the exported
  pull-up body/head cells and is also the link landing.
- The contact cell is an occupied SOLID cell with the exact exposed face.
  ONE_WAY and outline cells never become implicit Grab contacts.
- The R0 and MIRROR_X Grab proofs must remain exact horizontal mirrors.
- A rise greater than two, missing contact, blocked head cell, incorrect face,
  or trace that skips HANG/PULL_UP is rejected.

## C05 — deterministic mirror and readiness

- For each link order and sample order, MIRROR_X is exactly `x'=23-x` of R0;
  y, phase, and sample count remain unchanged.
- Clearance generation is deterministic and emits a stable digest.
- `JumpRecipeReady=true`, `ComposedGeometryReady=true`, and
  `SweptClearanceReady=true` only when all 18 links pass.
- `RecoveryReady=false` and `PlayerVerified=false` remain explicit. Reverse
  completion remains optional and is not introduced here.

## C06 — required exports

Create `MapDesign/MCP/GENERATED/SV5_18_JUMP_CLEARANCE/` containing:

- `BINDING.json`
- `jump_clearance.json`
- `clearance_links.csv`
- `clearance_trace.csv`
- `clearance_grab_contacts.csv`
- `clearance_validation.json`
- `independent_jump_clearance_audit.json`
- `targeted_results.xml`
- `focused_results.xml`
- `preview/jump_clearance.svg`
- `visual_export_audit.json`

The CSV files must retain the column names consumed by the bundled independent
checker. The SVG must show both variants, occupied tiles, ordered trace cells,
body/head footprint, and the Grab contact distinctly.

## C07 — tests and time control

- First reproduce failures for blocked body, blocked head, diagonal corner
  clipping, wrong endpoint, horizontal reversal, overlong trace, excessive apex,
  missing Grab contact, wrong Grab face, missing HANG/PULL_UP, bad mirror, and
  false readiness.
- Add at least 24 meaningful local tests. Include passing proofs for all normal
  links, both Grab links, exact mirror, deterministic repeat, export round-trip,
  and readiness boundaries.
- Targeted tests and the independent checker run before any full regression.
- Targeted execution must perform zero whole-world builds, searches, endpoint
  comparisons, or per-candidate world work.
- After targeted/checker/visual PASS, run one accepted full SV5 EditMode
  regression. Use the actual installed Unity 6000.3.8f1 Editor path. A Unity CLI
  MCP server process is not an Editor conflict and must not be terminated.
- A run with no NUnit result or a proven MCP transport-only error is
  infrastructure-invalid, must be preserved, and may be replaced once by a
  dedicated batchmode run. Never synthesize or merge NUnit results.
- Only a real complete NUnit PASS permits Finalize.

## C08 — lifecycle boundary

- On PASS create `MapDesign/MCP/SV5/26_JUMP_CLEARANCE_V5.md`, a matching Result,
  one atomic Task-owned commit, and an exact-blob Review ZIP.
- Do not modify Player code, physics constants, scenes, prefabs, world placement,
  tree/Hub logic, Packages, ProjectSettings, Master, or SV5_13-17 evidence.
- Do not start SV5_19, do not push, and do not restore/reset/checkout/clean or
  delete unrelated user changes.

