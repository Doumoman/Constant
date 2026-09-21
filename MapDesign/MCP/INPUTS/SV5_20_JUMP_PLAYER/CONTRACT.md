# SV5_20 — actual local Player traversal proof

## Purpose

Verify that the approved live Player can physically traverse the two accepted
24×32 jump recipes and their representative recovery routes. Use the real
Player prefab, movement driver, Rigidbody2D, CapsuleCollider2D, solid
Collider2D, top-only PlatformEffector2D, and safe Grab surface behavior. This
Task is a local Player proof; it is not world generation and it does not retune
movement to make the recipes pass. `SV5_20_FIX04_PLAYER_BINDING` supersedes
only the original base-only Driver binding after actual-capsule RMAP03 proof.

## C01 — frozen inputs and scope

- Input is the accepted SV5_19 recovery digest
  `f8b2e69686dbd9b7a29b82e4e4a8ca2c46c6183a005a5373e986e04b21f4288b`.
- Preserve the two 24×32 recipes, 18 main links, 90 accepted clearance states,
  16 recovery-overlay cells, 18 miss probes, and 18 recovery routes.
- SV5_13–19 source/evidence and all live Player settings, prefab, scenes, and
  input assets are read-only. The sole Player-source exception is the exact
  approved Driver byte binding in `FIX04_PLAYER_BINDING_AMENDMENT.json`; no
  further Driver edit is allowed.
- Whole-world generation/search, global endpoint-pair comparison, Sector
  partitioning, 48×32 or 624×416 traversal, and per-candidate world copies are
  prohibited.

## C02 — actual Player binding

- Instantiate `Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab`; prove the
  active components are the existing `CharacterLiveMovementDriver`,
  `Rigidbody2D`, and `CapsuleCollider2D`.
- Build only an in-memory 24×32 test fixture from canonical SV5_17 occupancy
  plus the canonical SV5_19 recovery overlay. SOLID uses real static
  Collider2D, TOP_ONLY uses real PlatformEffector2D behavior, and the two
  accepted Grab contacts use the existing safe `CharacterLiveGrabSurface`.
- Drive movement through the existing Player input/snapshot and fixed-step
  motor path. A case may place/reset the Player before its first fixed step;
  after the case starts, Transform/Rigidbody teleporting is forbidden.
- Record the exact base-commit blob identity of every Player source/prefab read
  and all current movement/grab constants. Preserve `player_bindings.json` as
  the base-provenance snapshot and add a byte-identical generated
  `player_binding_amendment.json` for the approved Driver. Do not duplicate the
  motor in a new simulator and do not change a constant merely to pass this
  Task.

## C03 — forward main traversal

- Run exactly one actual physical case for every main link: 9 in R0 and 9 in
  MIRROR_X. Each starts at that link's effective takeoff and reaches its bound
  target support or accepted Grab/exit state through fixed simulation.
- Run one continuous forward full-route case per recipe. It begins at link 0's
  effective takeoff and reaches link 8's final target without a post-start
  reset or teleport.
- `JS_LINK_03` in each recipe must observe actual safe Grab entry and an actual
  Player exit to the target. TOP_ONLY contacts must use the real one-way
  collision path and remain non-grabbable.
- Player motion must remain itemless. Reverse link traversal and reverse recipe
  completion are not required; an accepted route may be one-way.
- The authored design cap remains jump plus ledge Grab at no more than +2 cells.
  Do not reinterpret this as a two-cell free jump or grant Grab to a platform.

## C04 — physical representative-miss recovery

- Run one actual recovery case for each of the 18 immutable SV5_19 probes.
- Before the first fixed step, place the actual Player at that probe's bound
  body position with zero velocity. Then use gravity and real collision to hit
  the same first catch surface recorded by SV5_19.
- From the catch, drive the actual Player along the bound one-way recovery route
  until it reaches the same or earlier effective-takeoff checkpoint. For an
  `ALREADY_AT_CHECKPOINT` route, physical landing at that checkpoint completes
  the case without a fabricated movement link.
- Every case must preserve clear body/head occupancy, real support, checkpoint
  order `<= failed_link_order`, itemless motion, and zero post-start teleports.
- This proves the 18 representative misses only. It is not a claim that every
  possible analog trajectory is recoverable.

## C05 — trace and evidence integrity

- Record every fixed-step case from START through PASS_TARGET. Recovery cases
  additionally record RECOVERY_CATCH and REJOIN. The two Grab cases record
  GRAB_ENTER and GRAB_EXIT.
- A trace records input snapshot, body position, velocity, grounded/grab/climb/
  one-way state, event, and fixed-step order. Values must come from the actual
  Player components, not copied from SV5_18 logical samples.
- Re-run each recipe's full-route case once from a fresh fixture and require the
  same outcome and terminal target within a documented fixed-step tolerance.
- Any failed case keeps `PlayerVerified=false`. Do not move geometry, modify
  predecessor evidence, relax the +2 rule, or retune Player to convert failure
  to PASS.

## C06 — readiness

- Preserve `JumpRecipeReady`, `ComposedGeometryReady`,
  `SweptClearanceReady`, and `RecoveryReady` as true.
- Set `PlayerVerified=true` only when all 18 main-link cases, both continuous
  forward routes, all 18 recovery cases, determinism repeats, independent
  checker, and required regressions pass.
- Keep world-placement readiness outside this Task.

## C07 — required exports

Create `MapDesign/MCP/GENERATED/SV5_20_JUMP_PLAYER/` containing:

- `BINDING.json`
- `jump_player.json`
- `player_bindings.json`
- `player_binding_amendment.json`
- `player_measurements.json`
- `player_cases.csv`
- `player_trace.csv`
- `player_validation.json`
- `independent_jump_player_audit.json`
- `targeted_editmode_results.xml`
- `targeted_playmode_results.xml`
- `player_direct_regression_results.xml`
- `focused_results.xml`
- `preview/jump_player.svg`
- `visual_export_audit.json`

The SVG overlays recorded actual Player traces on both local recipes and marks
main targets, Grab entry/exit, miss catches, recovery rejoins, and full-route
terminal targets. Render and inspect it before the final regression.

## C08 — tests and execution control

- Add meaningful EditMode contract/export negative tests and PlayMode physical
  tests covering all 38 required cases. Negative tests reject a fake Player,
  missing Rigidbody2D/collider/driver, post-start teleport, logical-trace reuse,
  wrong first catch, later checkpoint, missing Grab entry/exit, platform Grab,
  item use, reverse-required claim, +2 cap violation, and false readiness.
- Run compile, targeted EditMode, and targeted PlayMode first. Write the exact
  binding amendment with `tools/write_player_binding_amendment.py`, then run:
  `python -X utf8 MapDesign/MCP/INPUTS/SV5_20_JUMP_PLAYER/tools/check_jump_player.py --project-root .`
- After targeted/checker/visual PASS, run the direct RMAP02/RMAP03/RMAP04
  PlayMode regressions once, followed by one accepted full SV5 EditMode
  regression. Do not repeatedly run the full suite while fixing ordinary local
  failures.
- `SV5_20_FIX05_RMAP_SCENE_ISOLATION` authorizes lifecycle-only corrections in
  the RMAP02 and RMAP03 PlayMode test files. Saved scenes must be loaded
  additively and unloaded in `finally`; physical fixtures must be destroyed,
  followed by one frame and `Physics2D.SyncTransforms()`. Remove the four
  temporary `RMAP03_Focused_*Diagnostic` methods before the final direct gate.
  The accepted contract gate is exactly 21 tests: 5 RMAP02, 10 RMAP03, and 6
  RMAP04. Do not weaken an assertion or alter product/fixture behavior.
- Use installed Unity 6000.3.8f1. Ignore, but do not kill, the Unity CLI MCP
  server when checking Editor conflicts. Never synthesize or merge NUnit XML.
- Run `STAGE.py --post-readonly` before Finalize. On PASS create the integration
  document, matching Result, one atomic Task-owned commit, and an exact-blob
  `SV5_20_JUMP_PLAYER_REVIEW.zip`.
- Do not modify Player runtime beyond the exact FIX04 Driver bytes. The
  RMAP03 PlayMode test may retain only its actual-capsule fixture isolation and
  natural recontact proof and must ship in the Task-owned commit. Do not modify
  Player tuning, scenes, prefabs, Packages, ProjectSettings, Master, world
  placement, or predecessor evidence. Do not start SV5_21 or push.
