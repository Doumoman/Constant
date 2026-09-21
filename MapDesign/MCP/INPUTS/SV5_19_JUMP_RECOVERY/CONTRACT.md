# SV5_19 — local missed-jump recovery routes

## Purpose

Add a small, deterministic recovery layer beneath the accepted SV5_18 jump
traces. A representative miss from every required jump must land on a lower
surface and follow an itemless one-way route back to the same or an earlier
checkpoint. This is not reverse completion of the jump room and is not a live
Player physics test.

## C01 — frozen inputs and local scope

- Input is the accepted SV5_18 clearance digest
  `48c3754b8109aa0601760567010b9aece67c5305907dfb76c7b155b3d4c37f13`.
- Preserve both 24×32 recipes, all SV5_17 occupancy, all 18 effective links,
  all 90 clearance states, the four endpoint corrections, and both Grab proofs.
- Work only in local integer cells. Whole-world generation/search, global
  endpoint comparison, Sector partitioning, and per-candidate world copies are
  prohibited.
- SV5_17/18 source and evidence remain immutable. SV5_19 owns a separate
  recovery overlay and recovery movement proof.

## C02 — representative miss probe

- Create exactly one miss probe for each of the 18 main links.
- Select it deterministically from that link's SV5_18 trace: take the lower
  middle element of the ordered `AIR` samples. With `n` AIR samples, use index
  `(n-1)//2`. Every accepted link has at least one AIR sample.
- The probe origin is that sample's body cell. Cast straight downward through
  the combined base plus recovery occupancy.
- The catch tile is the first occupied SOLID or TOP_ONLY cell below the origin.
  The landing body is one cell above it and the landing head is two cells above
  it. Every intervening body/head cell must be clear.
- A probe that exits below `y=0`, skips a nearer occupied tile, lands with a
  blocked body/head cell, or has no catch is rejected.
- This proves a representative failed-jump path. It does not claim that every
  possible analog velocity or every point along an arc is caught.

## C03 — recovery overlay geometry

- Add 3–9 recovery catch/support groups per recipe and at most 36 new occupied
  cells per recipe. Each connected horizontal catch run is 2–5 cells wide.
- Recovery cells may be SOLID or TOP_ONLY. They must be uniquely owned and may
  not overlap SV5_17 occupancy, any SV5_18 main body/head trace cell, or the
  explicit Grab contact.
- No combined 6×6 solid fill is allowed. The overlay must stay inside the 24×32
  local room and remain an exact horizontal mirror between R0 and MIRROR_X.
- Recovery geometry must not alter the accepted main traces; all 18 original
  clearance proofs are revalidated after overlay composition.

## C04 — itemless one-way recovery route

- Every caught probe owns a deterministic route from its landing body cell to
  an effective takeoff checkpoint whose main-link order is less than or equal
  to the failed link order.
- A recovery route may use `WALK`, `JUMP`, and `DROP`. `JUMP` rise is at most
  one cell; `DROP` cannot rise; `WALK` remains level. No item, ladder, flight,
  explosion, teleport, or future ability is allowed.
- Each recovery movement link contains at most 16 adjacent body/head trace
  states. Body and head remain clear; standing endpoints have a supporting
  occupied cell below; diagonal movement obeys strict supercover.
- Consecutive recovery movement links connect exactly. The final body cell is
  the selected checkpoint's SV5_18 effective takeoff.
- If the catch landing already equals the selected checkpoint, export
  `route_kind=ALREADY_AT_CHECKPOINT` with zero movement links. Otherwise use
  `route_kind=LINKED` and at least one movement link.
- At least two distinct checkpoint link orders are used per recipe. Duplicate
  routes may share geometry but must remain bound to their own failed link.
- Recovery is one-way. Reverse recovery and reverse main-route completion are
  explicitly not required.

## C05 — no progression shortcut claim

- A failed-link recovery may not end at a checkpoint later than the failed
  link. Its exported `max_rejoin_order` equals the failed link order.
- Recovery readiness means only `miss -> catch -> same/earlier checkpoint`.
  It does not certify that the recovery overlay cannot be exploited under live
  Player timing; that remains part of SV5_20 Player verification.

## C06 — readiness

- `JumpRecipeReady=true`, `ComposedGeometryReady=true`, and
  `SweptClearanceReady=true` remain required.
- Set `RecoveryReady=true` only when all 18 probes are caught, all 18 recovery
  routes pass, the overlay is mirrored, and every main trace still passes.
- Keep `PlayerVerified=false`.

## C07 — required exports

Create `MapDesign/MCP/GENERATED/SV5_19_JUMP_RECOVERY/` containing:

- `BINDING.json`
- `jump_recovery.json`
- `recovery_overlay.csv`
- `recovery_miss_probes.csv`
- `recovery_routes.csv`
- `recovery_links.csv`
- `recovery_trace.csv`
- `recovery_validation.json`
- `independent_jump_recovery_audit.json`
- `targeted_results.xml`
- `focused_results.xml`
- `preview/jump_recovery.svg`
- `visual_export_audit.json`

The SVG shows base supports, the main route, miss rays, catch surfaces, recovery
routes, and rejoin checkpoints for both variants.

## C08 — tests and execution control

- Reproduce failures for missing catch, skipped nearer surface, blocked landing
  body/head, overlay/main-trace overlap, bad mirror, disconnected route, link
  discontinuity, unsupported endpoint, jump rise above one, rising DROP,
  later-checkpoint shortcut, single-checkpoint collapse, reverse-required claim,
  6×6 fill, and false readiness.
- Add at least 28 meaningful local tests. Include all 18 probe/route successes,
  deterministic repeat, exact mirror, main-clearance preservation, export
  round-trip, and readiness boundaries.
- Run targeted tests and the independent checker before the full suite. Local
  targeted work must perform zero world builds/searches/global comparisons.
- After targeted/checker/visual PASS, run one accepted full SV5 EditMode
  regression. Use the installed Unity 6000.3.8f1 Editor. Ignore, but do not kill,
  the Unity CLI MCP server when checking for Editor conflicts.
- Infrastructure-invalid runs with no NUnit result are preserved and may be
  replaced once; results are never synthesized or merged.
- Run `STAGE.py --post-readonly` before Finalize. On PASS create
  `MapDesign/MCP/SV5/27_JUMP_RECOVERY_V5.md`, a matching Result, one atomic
  Task-owned commit, and an exact-blob Review ZIP.
- Do not modify Player, scenes, prefabs, Packages, ProjectSettings, Master,
  world placement, or predecessor evidence. Do not start SV5_20 or push.
