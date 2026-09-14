---
mcp_patch:
  format: single_task_v1
  task_id: SV5_17_JUMP_RECIPES
  task_file: TASKS/SV5_17_JUMP_RECIPES.md
  requires_current_task: NONE
  requires_completed_task: SV5_16_JUMP_OUTLINE
  requires_result:
    path: REPORTS/SV5_16_JUMP_OUTLINE_RESULT.md
    status: PASS
    sha256: ae060794bcde7aad625d5c2a401d74f649a4d1cdb2676cff0546fbddfee95fe6
  requires_installed_task:
    path: TASKS/SV5_16_JUMP_OUTLINE.md
    sha256: 282a3f2483f6a1e8ed0b2de1a34833747b4d06536954147a8250df7e374a325c
  sets_current_task: SV5_17_JUMP_RECIPES
---

# SV5_17_JUMP_RECIPES — compose exact mixed local route variants

TASK: SV5_17_JUMP_RECIPES
EXPECTED RESULT: MapDesign/MCP/REPORTS/SV5_17_JUMP_RECIPES_RESULT.md
NEXT: SV5_18_JUMP_CLEARANCE — LOCKED / DO NOT START

## Required reads and binding

Use native single_task_v1 Apply only after this package's STAGE.py --check,
--stage, and --verify-staged pass. Immediately after Apply run --verify-applied.
Read current MCP rules, Status, Master, every package file, and complete
SV5_13-16 Task/Archive/Result/Binding/source/tests/exports. Bind exact inputs and
before-state in GENERATED/SV5_17_JUMP_RECIPES/BINDING.json.

## Write allowlist

- new Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpRecipeCatalog.cs
- new Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpRecipeExport.cs
- their new .meta files
- new Assets/_Game/Tests/EditMode/Map/SV5/Sv5JumpRecipeTests.cs and .meta
- new MapDesign/MCP/SV5/25_JUMP_RECIPES_V5.md
- MapDesign/MCP/GENERATED/SV5_17_JUMP_RECIPES/**
- installed Task, Archive, matching Result, and normal Status lifecycle

Do not modify SV5_13-16 files/evidence, Player, tree/Hub, world placement,
scenes, prefabs, Packages, ProjectSettings, Master, or unrelated files. Do not
restore/reset/checkout/clean/delete unrelated dirty work.

## Required execution

1. Reproduce missing recipe, bad mirror, segment gap/overlap, bad mixture, and
   false #012 equivalence failures before PASS.
2. Build exact R0 and MirrorX recipes from the accepted SV5_16 typed fixture.
3. Partition nine links into the four exact ordered segments and prove the
   7 +1 / 1 +2 Grab / 1 level mixture independently for both variants.
4. Export both 768-cell #012 reference grids and their exact 141 changed cells,
   but never insert them into production occupancy.
5. Export every C05 artifact. Render and inspect SVG before full regression.
6. Add at least 30 local tests. No world generation/search, Sector partition,
   all-endpoint comparison, or per-candidate world work is allowed.
7. Run compile, targeted, then:
   python -X utf8 MapDesign/MCP/INPUTS/SV5_17_JUMP_RECIPES/tools/check_jump_recipes.py --project-root .
8. Only after targeted/checker/visual PASS run full SV5 regression exactly once.
9. Run STAGE.py --post-readonly before Finalize. Write PASS Result only after
   all gates pass, finalize, create one atomic Task-owned commit, then create an
   exact-blob SV5_17_JUMP_RECIPES_REVIEW.zip.

## Stop and report

Package/Git/state/owned-path mismatch blocks before staging. Ordinary code or
targeted failures are iterative and cannot weaken the recipe counts, transforms,
movement mixture, #012 reference hashes, diff count, tests, or readiness gates.
Report commit/title, both recipe digests, per-recipe and catalog counts, exact
movement mixture, four segment proofs, #012 before/after digests and 141-cell
diff, explicit non-equivalence, test durations/hashes, checker/visual audits,
full-suite run count, readiness, Review ZIP SHA, and untouched dirty scope. Stop
with SV5_18 locked and no push.
