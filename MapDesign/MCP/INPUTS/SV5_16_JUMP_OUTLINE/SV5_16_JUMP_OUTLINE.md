---
mcp_patch:
  format: single_task_v1
  task_id: SV5_16_JUMP_OUTLINE
  task_file: TASKS/SV5_16_JUMP_OUTLINE.md
  requires_current_task: NONE
  requires_completed_task: SV5_15_JUMP_GRAB
  requires_result:
    path: REPORTS/SV5_15_JUMP_GRAB_RESULT.md
    status: PASS
    sha256: 533a05e5be51ce1fdb4cb8872a52bd73e25c86a8079c280a9867a4c1addf4268
  requires_installed_task:
    path: TASKS/SV5_15_JUMP_GRAB.md
    sha256: 0cfad31cee048e6961500712fa2d2305a854323c073f10accdc3d6ad25f55547
  sets_current_task: SV5_16_JUMP_OUTLINE
---

# SV5_16_JUMP_OUTLINE — add irregular solid backing without moving the route

TASK: SV5_16_JUMP_OUTLINE
EXPECTED RESULT: MapDesign/MCP/REPORTS/SV5_16_JUMP_OUTLINE_RESULT.md
NEXT: SV5_17_JUMP_RECIPES — LOCKED / DO NOT START

## Required reads and binding

Use native single_task_v1 Apply only after this package's STAGE.py --check,
--stage, and --verify-staged pass. Immediately after Apply run --verify-applied.
Read current MCP rules, Status, Master, all package files, and complete SV5_13-15
Task/Archive/Result/Binding/source/tests/exports. Bind exact inputs and before-
state in GENERATED/SV5_16_JUMP_OUTLINE/BINDING.json.

## Write allowlist

- new Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpOutlineGeometry.cs
- new Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpOutlineExport.cs
- their new .meta files
- new Assets/_Game/Tests/EditMode/Map/SV5/Sv5JumpOutlineTests.cs and .meta
- new MapDesign/MCP/SV5/24_JUMP_OUTLINE_V5.md
- MapDesign/MCP/GENERATED/SV5_16_JUMP_OUTLINE/**
- installed Task, Archive, matching Result, and normal Status lifecycle

Do not modify SV5_13-15 files/evidence, Player, tree/Hub, world placement,
scenes, prefabs, Packages, ProjectSettings, Master, or unrelated files. Do not
restore/reset/checkout/clean/delete unrelated dirty work.

## Required execution

1. Reproduce missing/extra/depth/ONE_WAY/blocked-space failures before PASS.
2. Derive the exact 17 C02 outline cells from the accepted SV5_15 fixture.
3. Build the exact 44-cell final occupancy. Preserve all C01 route and Grab data.
4. Recompute C03 route/Grab clearance from actual final cells.
5. Export every C04 artifact. Render and inspect SVG before full regression.
6. Add at least 22 local tests. No world generation/search, Sector partition, or
   all-endpoint comparison is allowed in the new targeted suite.
7. Run compile, targeted, then:
   python -X utf8 MapDesign/MCP/INPUTS/SV5_16_JUMP_OUTLINE/tools/check_jump_outline.py --project-root .
8. Only after targeted/checker/visual PASS run full SV5 regression exactly once.
9. Run STAGE.py --post-readonly before Finalize. Write PASS Result only after all
   gates pass, finalize, create one atomic Task-owned commit, then create an
   exact-blob SV5_16_JUMP_OUTLINE_REVIEW.zip.

## Stop and report

Package/Git/state/owned-path mismatch blocks before staging. Ordinary code or
targeted failures are iterative and cannot weaken the exact 17-cell profile,
route, Grab, clearance, or tests. Report commit/title, depth table, base/outline/
final cell counts, 6x6 scan, preserved route and Grab coordinates, clearance,
test durations/hashes, checker/visual audits, full-suite run count, readiness,
Review ZIP SHA, and untouched dirty scope. Stop with SV5_17 locked and no push.

