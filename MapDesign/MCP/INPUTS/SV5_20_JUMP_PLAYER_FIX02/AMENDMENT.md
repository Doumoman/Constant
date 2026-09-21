# SV5_20_FIX02 — correct borrowed support ownership and jump scheduling

## Authority

This amendment supersedes SV5_20 FIX01 while the installed
`SV5_20_JUMP_PLAYER` Task remains CURRENT. Do not create an MCP_INBOX file, do
not run native Apply again, and do not change lifecycle state before all SV5_20
gates pass. Original SV5_20 requirements remain active except where this file
replaces FIX01's six-cell patch and fixed-delay jump scheduling.

## C01 — FIX01 contract correction

- R0 `(17,4)` and MIRROR_X `(6,4)` already exist as immutable SV5_19
  `RG_GRAB_CATCH` TOP_ONLY cells. FIX01's ADD operations were incorrect.
- Apply exactly four REMOVE operations from `GEOMETRY_PATCH.json`: R0
  `(14,4)`, `(14,5)` and MIRROR_X `(9,4)`, `(9,5)`.
- Do not add, recreate, re-own, or relabel `(17,4)` or `(6,4)`. Borrow them from
  `SV5_19_RECOVERY` with owner `RG_GRAB_CATCH`; they remain TOP_ONLY and
  non-grabbable.
- Preserve SOLID `(14,6)` and `(9,6)`. No fifth operation, fallback carve,
  widening, extra support, or predecessor edit is authorized.
- Effective JS_LINK_02 landings remain R0 `(17,5)` and MX `(6,5)`, followed by
  a supported one-cell walk to the unchanged JS_LINK_03 takeoff.

## C02 — MAIN_MX_00 is a scheduler defect

- The locked failure trace proves MAIN_MX_00 was grounded through step 41,
  left support at step 42, and did not submit Jump until step 49 with
  `vy=-2.17`. This is not evidence that the link geometry is unreachable.
- Remove fixed PREPARE/RUNUP wait counts as the jump trigger. The harness may
  accelerate on support, but must press Jump while the actual Player is still
  grounded and its leading capsule edge is within the threshold defined by
  `SCHEDULER_PROFILE.json`.
- Derive the forward support edge from the composed local occupancy below the
  actual Player. Use current Rigidbody2D velocity, fixedDeltaTime, collider
  half-width, and existing collision skin. Do not hardcode step 41/42 or create
  a per-link magic jump frame.
- Feed Jump through the existing Player snapshot/motor path before the next
  fixed simulation step. Do not set velocity, position, grounded state, or
  collision results directly.
- The proof may use the existing Player's coyote behavior, but the scheduler
  must not require coyote time to compensate for a late trigger.

## C03 — collect all main-link failures in one run

- Before rerunning the monolithic 38-case proof, expose the 18 main links as 18
  independent, non-parallel NUnit PlayMode cases using the same fixture and
  scheduler implementation.
- One failed case must not prevent the remaining main-link cases from being
  executed and reported in `player_main_link_diagnostic_results.xml`.
- This diagnostic is actual Player physics, not a static envelope or duplicated
  simulator. It may reset the Player only before START of each independent case.
- Stop a divergent case when body y drops below -2 or after 240 fixed steps so
  a known miss does not emit hundreds of useless falling states.
- If the diagnostic is not 18/18, report every failed link and its trace in one
  BLOCKED result. Do not change geometry beyond the four authorized removals.

## C04 — evidence and checkers

Preserve both failed physical runs byte-exactly:

- `targeted_playmode_before_fix01.xml`: original MAIN_MX_02 failure.
- `targeted_playmode_before_fix02.xml`: current MAIN_MX_00 late-jump failure.

Create or update in `GENERATED/SV5_20_JUMP_PLAYER/`:

- `player_geometry_fix02.json`
- `player_geometry_patch.csv` with exactly four REMOVE rows
- `player_composed_occupancy.csv`
- `player_effective_links.csv`
- `player_scheduler_audit.json`
- `player_main_link_diagnostic_results.xml`
- `independent_jump_player_fix02_audit.json`
- the original SV5_20 case, trace, validation, visual, and test outputs

`player_composed_occupancy.csv` must retain the two borrowed recovery supports
with their SV5_19 layer/owner. `player_cases.csv` must identify FIX02 and expose
effective takeoff/landing cells.

The FIX01 checker is superseded and must not be run. After diagnostic 18/18 and
targeted suites pass, run the original SV5_20 checker and then the FIX02 checker.

## C05 — unchanged boundaries

- Player runtime, settings, input, prefab, physics constants, and movement
  values remain byte-identical to base commit.
- SV5_13–19 source/evidence remains byte-identical. FIX02 is an SV5_20 local
  composition layer only.
- Item use, reverse-completion requirement, post-START teleport, Sector work,
  world generation/search, global endpoint comparison, and 624×416 traversal
  remain prohibited.
- Jump plus Grab is capped at +2 cells. TOP_ONLY supports are never grabbable.

## C06 — resumed order

1. Run this package's `VERIFY.py --check`.
2. Preserve the current failure as `targeted_playmode_before_fix02.xml`.
3. Correct four-cell composition and the shared support-edge scheduler.
4. Run targeted EditMode, then the 18-case main-link diagnostic.
5. Only at diagnostic 18/18 run the full targeted PlayMode proof.
6. Run original checker, FIX02 checker, and visual inspection.
7. Continue original RMAP02–04 direct regression and one full SV5 regression.
8. Run FIX02 `VERIFY.py --post-readonly`, then Result, Finalize, one atomic
   commit, and exact-blob Review ZIP. Keep SV5_21 locked and do not push.

