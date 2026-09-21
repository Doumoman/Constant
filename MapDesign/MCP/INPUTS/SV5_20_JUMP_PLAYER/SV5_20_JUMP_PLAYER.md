---
mcp_patch:
  format: single_task_v1
  task_id: SV5_20_JUMP_PLAYER
  task_file: TASKS/SV5_20_JUMP_PLAYER.md
  requires_current_task: NONE
  requires_completed_task: SV5_19_JUMP_RECOVERY
  requires_result:
    path: REPORTS/SV5_19_JUMP_RECOVERY_RESULT.md
    status: PASS
    sha256: 324f19e4098133b4be6b22d7a2dc72823f8e5637fb07cad6cc1faf02ad5e2bb2
  requires_installed_task:
    path: TASKS/SV5_19_JUMP_RECOVERY.md
    sha256: d5961f0f6f89339ded8d82134df10e427d6654e6e0c84aa28d026c01b1d6ecd4
  sets_current_task: SV5_20_JUMP_PLAYER
---

# SV5_20_JUMP_PLAYER — verify accepted local recipes with the actual Player

TASK: SV5_20_JUMP_PLAYER
EXPECTED RESULT: MapDesign/MCP/REPORTS/SV5_20_JUMP_PLAYER_RESULT.md
NEXT: SV5_21_LIBRARY — LOCKED / DO NOT START

## Required reads and binding

After package preflight/stage/verification, use native single_task_v1 Apply and
immediately run `STAGE.py --verify-applied`. Read all package files, complete
SV5_13–19 Task/Archive/Result/Binding/source/tests/exports, and the current live
Player movement/grab/prefab inputs and the FIX04 exact binding amendment.
Record exact base-commit blob identities, the approved current Driver identity,
observed movement constants, and before-state in
`GENERATED/SV5_20_JUMP_PLAYER/BINDING.json`.

## Write allowlist

- new `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpPlayerVerification.cs`
- new `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpPlayerExport.cs`
- their new `.meta` files
- new `Assets/_Game/Tests/EditMode/Map/SV5/Sv5JumpPlayerProofTests.cs` and `.meta`
- new `Assets/_Game/Tests/PlayMode/Map/SV5/Sv5JumpPlayerPlayModeTests.cs` and `.meta`
- exact approved `Assets/_Game/Live/Runtime/Movement/CharacterLiveMovementDriver.cs`
- actual-capsule fixture/isolation/recontact-only changes in
  `Assets/_Game/Tests/PlayMode/Character/RMAP03/CharacterLiveGrabPlayModeTests.cs`
- SavedScene/fixture lifecycle-only changes in
  `Assets/_Game/Tests/PlayMode/Character/RMAP02/GeneratedTilemapPlayerRunPlayModeTests.cs`
- new `MapDesign/MCP/SV5/28_JUMP_PLAYER_V5.md`
- `MapDesign/MCP/GENERATED/SV5_20_JUMP_PLAYER/**`
- the FIX04 amendment files and corrected package manifest/check helpers
- installed Task, Archive, matching Result, and normal Status lifecycle

The approved Driver bytes are fixed by `FIX04_PLAYER_BINDING_AMENDMENT.json`;
no further Driver edit is allowed. All other Player runtime/settings/input/
prefab files and SV5_13–19 source/evidence remain read-only. Do not edit scenes,
Packages, ProjectSettings, Master, world placement, or unrelated files.
Preserve unrelated dirty work.

## Required execution

1. Bind the actual live Player prefab, approved FIX04 movement driver,
   Rigidbody2D, CapsuleCollider2D, unchanged movement constants, and safe Grab
   surface implementation. Preserve the base snapshot plus exact amendment.
2. Compose only the two in-memory 24×32 physical fixtures from accepted
   occupancy plus recovery overlay; do not build or scan the world.
3. Run 18 actual main-link cases and two continuous forward full-route cases.
   Prove actual Grab entry/exit and actual top-only collision behavior.
4. Run 18 physical miss/catch/rejoin cases bound to the immutable SV5_19 probes
   and same/earlier checkpoints. A pre-start reset is allowed; post-start
   teleport is not.
5. Preserve itemless one-way semantics. Reverse completion is not required.
   Jump plus Grab may gain at most two cells; branches are SOLID-grabbable only
   where explicitly bound, while platform geometry remains non-grabbable.
6. Export actual fixed-step traces and all CONTRACT C07 evidence from the same
   canonical objects. Render and inspect the SVG.
7. Add the required EditMode negative/export tests and PlayMode physical cases.
8. Run compile, targeted EditMode/PlayMode, write the exact generated binding
   amendment with the package tool, then:
   `python -X utf8 MapDesign/MCP/INPUTS/SV5_20_JUMP_PLAYER/tools/check_jump_player.py --project-root .`
9. After targeted/checker/visual PASS, run RMAP02–04 direct Player PlayMode
   regressions once as exactly 5+10+6=21 contract tests. Apply FIX05 additive
   SavedScene unload/restore and fixture cleanup; temporary focused diagnostics
   are not regression tests. Then run the full SV5 EditMode regression exactly
   once.
10. Run post-readonly. Write PASS Result only after all gates pass, finalize,
    make one atomic Task-owned commit, and build the exact-blob Review ZIP.

## Stop and report

Package/Git/state/owned-path mismatch blocks before staging. Ordinary local
implementation/test failures are iterative, but they cannot be fixed by Player
retuning, further Driver edits, geometry/evidence edits, fake physics,
post-start teleport, reverse requirements, or world/global work. Report exact
base and amended Player bindings/constants,
38 physical case results, fixed-step counts, Grab and one-way observations,
determinism, tests/audits, full-suite count, Review ZIP SHA, and untouched dirty
scope. Stop with SV5_21 locked and no push.
