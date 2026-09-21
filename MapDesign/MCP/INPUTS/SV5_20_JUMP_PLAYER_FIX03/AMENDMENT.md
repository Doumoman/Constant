# SV5_20_FIX03 — resolve the three symmetric actual-Player defects

## Authority

This amendment supersedes SV5_20 FIX01 and FIX02 while the installed
`SV5_20_JUMP_PLAYER` Task remains CURRENT. Do not create or stage an
MCP_INBOX Task, do not run native Apply again, and do not change lifecycle
state until every SV5_20 gate passes. The original SV5_20 contract remains in
force except where this amendment replaces FIX02 geometry and scheduler rules.

## Proven diagnosis

The locked 18-case diagnostic is 12 PASS / 6 FAIL. The six failures are exact
R0/MIRROR_X pairs and therefore represent three defects, not six unrelated
failures.

1. `JS_LINK_02`: Jump begins with `vy=6.87`, then becomes `0.00` on the next
   fixed step. The Player head hits the still-solid top cell of the JS04
   column. This is a local ceiling collision.
2. `JS_LINK_03`: safe Grab enters and holds, but the recorded GRAB_EXIT has
   `vy=0.00`; the Player remains frozen at the anchor through step 240. The
   harness observed a release, not the existing RMAP03 Space jump exit.
3. `JS_LINK_06`: grounded run-up reaches the later JS08 solid, its horizontal
   velocity is cancelled, and it jumps vertically back to the start. JS08 is
   future support geometry placed inside the preceding link's corridor.

## C01 — exact local geometry composition

Apply the exact 16 unique-cell operations in `GEOMETRY_PATCH.json` to the
SV5_17 occupancy plus SV5_19 recovery overlay. Do not edit either predecessor.

- Complete the JS04 clearance column by removing R0 `(14,4..6)` and MIRROR_X
  `(9,4..6)`. This replaces FIX02's four removals with six and removes the
  proven LINK_02 head impact.
- Replace only the blocking JS08 cluster with a pass-through step at the same
  top elevation:
  - R0: remove `(3,7)`, `(3,8)`, `(4,8)` and convert `(3,9)`, `(4,9)` from
    SOLID to TOP_ONLY.
  - MIRROR_X: remove `(20,7)`, `(20,8)`, `(19,8)` and convert `(20,9)`,
    `(19,9)` from SOLID to TOP_ONLY.
- The four converted top cells keep JS_LINK_07's landing and JS_LINK_08's
  takeoff coordinates unchanged. They use real PlatformEffector2D behavior,
  are non-grabbable, and contain no Grab marker.
- Preserve all other geometry. Do not widen a route, add a catch platform, or
  perform a fallback carve.
- Retain the borrowed SV5_19 `RG_GRAB_CATCH` TOP_ONLY cells at R0 `(16,4)`,
  `(17,4)` and MIRROR_X `(6,4)`, `(7,4)` with their original owner.

## C02 — walkable-frontier jump trigger

- Replace the current-cell edge calculation with a forward walkable-support
  frontier. Starting below the actual Player, scan only the contiguous local
  support cells in the requested direction while the corresponding body and
  head cells are clear in the composed occupancy.
- The frontier stops before the first unsupported or body/head-blocked cell.
  It must not stop at an internal boundary between two contiguous walkable
  support cells.
- Submit Jump through the existing snapshot/motor path while grounded and
  when the leading capsule edge reaches the derived frontier threshold. Never
  set Transform, Rigidbody2D velocity, grounded state, or collision results.
- No hardcoded fixed-step number, per-link jump frame, coyote dependency, or
  copied movement simulator is allowed.
- Continue normal air control toward the bound landing. Horizontal input may
  brake after entering the target support interval, but must be derived from
  the target interval rather than from a link-specific delay.

## C03 — real RMAP03 Space Grab exit

For the two `JS_LINK_03` cases use the existing RMAP03 behavior exactly:

1. approach and Jump toward the accepted safe SOLID edge;
2. observe `IsGrabbing=true` for two consecutive fixed observations;
3. submit one fresh Space/Jump press through the existing input snapshot with
   horizontal input toward the bound top landing and `Down=false`;
4. require observed Grab exit plus positive vertical velocity within the next
   two fixed observations;
5. continue existing air control until the Player lands on the bound target.

Do not call a mere Grab-button release `GRAB_EXIT`. Export
`GRAB_SPACE_EXIT` only when the real Player leaves Grab with a positive jump
impulse. Do not implement pull-up, teleport, wall-kick, ladder, or a new Grab
motor. If the impulse is absent, fail immediately with
`GRAB_EXIT_NO_VERTICAL_IMPULSE` instead of waiting to step 240.

## C04 — evidence and bounded execution

Before changing outputs, preserve the attached diagnostic byte-exactly as
`targeted_playmode_before_fix03.xml` with SHA-256
`640170e9e2dc6e7c27a659d00dc43840ec0d9ef84033cee8c846e80902fc60ba`.
Keep the earlier `targeted_playmode_before_fix01.xml` and
`targeted_playmode_before_fix02.xml` byte-exact.

Create or update in `GENERATED/SV5_20_JUMP_PLAYER/`:

- `player_geometry_fix03.json`
- `player_geometry_patch.csv` with exactly 12 REMOVE and 4 CONVERT rows
- `player_composed_occupancy.csv`
- `player_effective_links.csv`
- `player_scheduler_audit.json`
- `player_main_link_diagnostic_results.xml`
- `independent_jump_player_fix03_audit.json`
- the original SV5_20 outputs

Execution order is deliberately bounded:

1. run this package's `VERIFY.py --check`;
2. preserve the 12/18 diagnostic as `targeted_playmode_before_fix03.xml`;
3. implement the exact composition and shared scheduler/Grab state machine;
4. run compile and targeted EditMode;
5. run only the six formerly failing independent cases first;
6. if and only if those are 6/6, run the 18-case diagnostic;
7. if and only if that is 18/18, run the full 38-case targeted PlayMode proof;
8. run the original SV5_20 checker, then the FIX03 checker and visual audit;
9. run RMAP02–04 direct regression once and full SV5 regression once;
10. run FIX03 `VERIFY.py --post-readonly`, Result, Finalize, one atomic commit,
    and exact-blob Review ZIP. Keep SV5_21 locked and do not push.

Do not run the superseded FIX01 or FIX02 checkers. On any failed gate, report
all failures from that gate and stop before broader suites.

## C05 — unchanged boundaries

- Player runtime, settings, input, prefab, physics constants, and movement
  values remain byte-identical to base commit.
- SV5_13–19 source/evidence remains byte-identical. FIX03 is an SV5_20 local
  fixture-composition correction only.
- Item use, reverse-completion requirement, post-START teleport, Sector work,
  48x32/624x416 work, world generation/search, and global endpoint comparison
  remain prohibited.
- Jump plus Grab remains capped at +2 cells. TOP_ONLY is never grabbable.
