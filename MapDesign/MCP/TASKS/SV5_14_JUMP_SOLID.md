---
mcp_patch:
  format: single_task_v1
  task_id: SV5_14_JUMP_SOLID
  task_file: TASKS/SV5_14_JUMP_SOLID.md
  requires_current_task: NONE
  requires_completed_task: SV5_13_JUMP_CONTRACT
  requires_result:
    path: REPORTS/SV5_13_JUMP_CONTRACT_RESULT.md
    status: PASS
    sha256: 0379706ace7944389c56cb91c8c1999a8a4c4cea7b8a90e0226ba4a63c8a36df
  requires_installed_task:
    path: TASKS/SV5_13_JUMP_CONTRACT.md
    sha256: 3fe70921236563d0e4f9d361f31b172551ddf712d1a7668921c0fc9dcd7a412c
  sets_current_task: SV5_14_JUMP_SOLID
---

# SV5_14_JUMP_SOLID — build the mixed-support jump route

TASK: SV5_14_JUMP_SOLID
EXPECTED RESULT: MapDesign/MCP/REPORTS/SV5_14_JUMP_SOLID_RESULT.md
NEXT: SV5_15_JUMP_GRAB — LOCKED / DO NOT START

## Required reads and binding

Use native single_task_v1 Apply only after this package's STAGE.py --check,
--stage, and --verify-staged pass. Immediately after Apply run --verify-applied.
Read current MCP execution rules, Status, Master, all files in this package,
SPACE v5 rules/tasks, and the complete SV5_13 Task, Archive, Result, Binding,
source, tests, and generated contract. Bind exact inputs and the before-state in
GENERATED/SV5_14_JUMP_SOLID/BINDING.json.

## Write allowlist

- new Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpSolidGeometry.cs
- new Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpSolidExport.cs
- matching new .meta files
- new Assets/_Game/Tests/EditMode/Map/SV5/Sv5JumpSolidTests.cs and .meta
- new MapDesign/MCP/SV5/22_JUMP_SOLID_V5.md
- MapDesign/MCP/GENERATED/SV5_14_JUMP_SOLID/**
- installed Task, Archive, matching Result, and normal Status lifecycle

Do not modify SV5_13 files, Player runtime/tuning, earlier geometry/evidence,
world placement, scene, prefab, Packages, ProjectSettings, Master, or unrelated
files. Do not restore/reset/checkout/clean/delete unrelated dirty work.

## Required execution

1. Reproduce an all-ONE_WAY fixture and prove it fails the mixed-support rule.
2. Implement a deterministic 24x32 local typed geometry that consumes
   Sv5JumpContract and emits actual support cells.
3. Build the ordered required ascent route under C02-C04. Every counted SOLID
   must be used as an actual takeoff or landing; no decorative credit.
4. Export all C05 artifacts from the canonical object graph. Render and inspect
   the SVG before the full regression.
5. Add the C06 tests. The new targeted suite must use zero 624x416 world builds
   and searches.
6. Run compile and targeted tests, then:
   python -X utf8 MapDesign/MCP/INPUTS/SV5_14_JUMP_SOLID/tools/check_jump_solid.py --project-root .
7. Only after targeted, checker, and visual inspection pass, run the full SV5
   regression exactly once.
8. Run STAGE.py --post-readonly before Finalize. Write PASS Result only after
   all gates pass, finalize, create one atomic Task-owned commit, then create an
   exact-blob SV5_14_JUMP_SOLID_REVIEW.zip.

## Stop and report

Package/Git/state/owned-path collisions block before staging. Ordinary
implementation or targeted failures are iterative and cannot weaken geometry,
route, or test requirements. Report commit/title, support and cell counts,
ordered route length/span, recomputed gap/rise ranges, counted SOLID uses,
clearance results, test durations/XML hashes, checker and visual audit hashes,
full-suite run count, readiness, Review ZIP SHA, and untouched dirty scope.
Stop with SV5_15 locked and no push.

