---
mcp_patch:
  format: single_task_v1
  task_id: SV5_15_JUMP_GRAB
  task_file: TASKS/SV5_15_JUMP_GRAB.md
  requires_current_task: NONE
  requires_completed_task: SV5_14_JUMP_SOLID
  requires_result:
    path: REPORTS/SV5_14_JUMP_SOLID_RESULT.md
    status: PASS
    sha256: 356d1ce122a95b98e671710fc6bcf694371f02edc9e5800470472aee309d48d2
  requires_installed_task:
    path: TASKS/SV5_14_JUMP_SOLID.md
    sha256: 3cb140ff326f5bfb5458435d465f6128d1b624eb3fdb881fd1e36c3966cc9a58
  sets_current_task: SV5_15_JUMP_GRAB
---

# SV5_15_JUMP_GRAB — add one actual SOLID-face Grab segment

TASK: SV5_15_JUMP_GRAB
EXPECTED RESULT: MapDesign/MCP/REPORTS/SV5_15_JUMP_GRAB_RESULT.md
NEXT: SV5_16_JUMP_OUTLINE — LOCKED / DO NOT START

## Required reads and binding

Use native single_task_v1 Apply only after this package's STAGE.py --check,
--stage, and --verify-staged pass. Immediately after Apply run --verify-applied.
Read current MCP rules, Status, Master, all package files, the complete SV5_13
contract and SV5_14 Task/Archive/Result/Binding/source/tests/exports. Bind exact
inputs and before-state in GENERATED/SV5_15_JUMP_GRAB/BINDING.json.

## Write allowlist

- new Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpGrabGeometry.cs
- new Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpGrabExport.cs
- their new .meta files
- new Assets/_Game/Tests/EditMode/Map/SV5/Sv5JumpGrabTests.cs and .meta
- new MapDesign/MCP/SV5/23_JUMP_GRAB_V5.md
- MapDesign/MCP/GENERATED/SV5_15_JUMP_GRAB/**
- installed Task, Archive, matching Result, and normal Status lifecycle

Do not modify SV5_13/SV5_14 files or evidence, Player runtime/tuning, tree/Hub,
world placement, scenes, prefabs, Packages, ProjectSettings, Master, or unrelated
files. Do not restore/reset/checkout/clean/delete unrelated dirty work.

## Required execution

1. Reproduce rejection fixtures for ONE_WAY Grab and occupied hang/pull-up AIR.
2. Derive the exact C01 support cells from the accepted SV5_14 local fixture.
3. Build the exact C02 link and C03 action witness through
   Sv5JumpContract.Measure; validate against actual final occupancy.
4. Export every C05 artifact. Render and inspect the SVG before full regression.
5. Add at least 20 local tests. New targeted tests perform zero 624x416 world
   builds/searches and introduce no Sector partition or all-endpoint comparison.
6. Run compile, targeted, then:
   python -X utf8 MapDesign/MCP/INPUTS/SV5_15_JUMP_GRAB/tools/check_jump_grab.py --project-root .
7. Only after targeted/checker/visual PASS, run the full SV5 regression exactly once.
8. Run STAGE.py --post-readonly before Finalize. Write PASS Result only after all
   gates pass, finalize, create one atomic Task-owned commit, then make an
   exact-blob SV5_15_JUMP_GRAB_REVIEW.zip.

## Stop and report

Package/Git/state/owned-path mismatch blocks before staging. Ordinary code or
targeted failures are iterative and cannot weaken cell, contact, clearance, or
test requirements. Report commit/title, support/cell counts, exact Grab link and
coordinates, all recomputed gap/rise values, negative cases, test durations and
hashes, visual/checker audits, full-suite run count, readiness, Review ZIP SHA,
and untouched dirty scope. Stop with SV5_16 locked and no push.

