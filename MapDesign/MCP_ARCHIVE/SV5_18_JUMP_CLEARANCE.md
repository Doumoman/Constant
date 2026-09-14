---
mcp_patch:
  format: single_task_v1
  task_id: SV5_18_JUMP_CLEARANCE
  task_file: TASKS/SV5_18_JUMP_CLEARANCE.md
  requires_current_task: NONE
  requires_completed_task: SV5_17_JUMP_RECIPES
  requires_result:
    path: REPORTS/SV5_17_JUMP_RECIPES_RESULT.md
    status: PASS
    sha256: 06bd13ee5b306aa1b72bbccbd5791834b87605dc7fb4b79deae1933db0900ce2
  requires_installed_task:
    path: TASKS/SV5_17_JUMP_RECIPES.md
    sha256: 63b74bfd3ebd30929a0b377e03ca3379929d77012027d0e0285bae1ff1ec0a18
  sets_current_task: SV5_18_JUMP_CLEARANCE
---

# SV5_18_JUMP_CLEARANCE — prove local body/head passage for every recipe link

TASK: SV5_18_JUMP_CLEARANCE
EXPECTED RESULT: MapDesign/MCP/REPORTS/SV5_18_JUMP_CLEARANCE_RESULT.md
NEXT: SV5_19_JUMP_RECOVERY — LOCKED / DO NOT START

## Required reads and binding

Use native single_task_v1 Apply only after this package's STAGE.py `--check`,
`--stage`, and `--verify-staged` pass. Immediately after Apply run
`--verify-applied`. Read current MCP rules, Status, Master, every package file,
and the complete SV5_13-17 Task/Archive/Result/Binding/source/tests/exports.
Bind exact inputs and before-state in
`GENERATED/SV5_18_JUMP_CLEARANCE/BINDING.json`.

## Write allowlist

- new `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpClearance.cs`
- new `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpClearanceExport.cs`
- their new `.meta` files
- new `Assets/_Game/Tests/EditMode/Map/SV5/Sv5JumpClearanceTests.cs` and `.meta`
- new `MapDesign/MCP/SV5/26_JUMP_CLEARANCE_V5.md`
- `MapDesign/MCP/GENERATED/SV5_18_JUMP_CLEARANCE/**`
- installed Task, Archive, matching Result, and normal Status lifecycle

Do not edit predecessor source/evidence, Player, physics constants, tree/Hub,
world placement, scenes, prefabs, Packages, ProjectSettings, Master, or
unrelated files. Preserve unrelated dirty work.

## Required execution

1. Reproduce every required failure in CONTRACT C07 before final PASS.
2. Build deterministic local cell traces for all 18 accepted links.
3. Validate body/head AIR, diagonal supercover, endpoint identity, monotonic
   direction, state count, and apex bound independently for every trace.
4. Bind both `JS_LINK_03` traces to the exact SOLID Grab contact, HANG, and
   PULL_UP cells. Do not grant Grab to ONE_WAY or outline geometry.
5. Prove MIRROR_X sample-for-sample and keep recovery/player readiness false.
6. Export every CONTRACT C06 artifact and visually inspect the rendered SVG.
7. Add at least 24 local tests. Whole-world generation/search, Sector work,
   all-endpoint comparison, and per-candidate world copies are prohibited.
8. Run compile and targeted tests, then:
   `python -X utf8 MapDesign/MCP/INPUTS/SV5_18_JUMP_CLEARANCE/tools/check_jump_clearance.py --project-root .`
9. Only after targeted/checker/visual PASS, run the full SV5 regression under
   CONTRACT C07. Run `STAGE.py --post-readonly` before Finalize.
10. Write PASS Result only after all gates pass, finalize, make one atomic
    Task-owned commit, and create `SV5_18_JUMP_CLEARANCE_REVIEW.zip` from exact
    commit blobs.

## Stop and report

Package/Git/state/owned-path mismatch blocks before staging. Ordinary code or
targeted failures are iterative and cannot weaken local dimensions, recipe/link
counts, trace footprint, diagonal rule, Grab binding, mirror proof, readiness,
or test gates. Report catalog/clearance digests, 18 link results, trace counts,
Grab proofs, mirror proof, test durations/hashes, checker/visual audit, accepted
full-suite result, Review ZIP SHA, and untouched dirty scope. Stop with SV5_19
locked and no push.

