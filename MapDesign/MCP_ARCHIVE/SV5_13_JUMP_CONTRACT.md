---
mcp_patch:
  format: single_task_v1
  task_id: SV5_13_JUMP_CONTRACT
  task_file: TASKS/SV5_13_JUMP_CONTRACT.md
  requires_current_task: NONE
  requires_completed_task: SV5_12_FIX01
  requires_result:
    path: REPORTS/SV5_12_FIX01_RESULT.md
    status: PASS
    sha256: 62a3050904c258537b276c153ee7cc0878333cee1930e9efacdc4182eace240b
  requires_installed_task:
    path: TASKS/SV5_12_FIX01.md
    sha256: 3459698bfab18442d96ce535dbab6cbfc335d32c61af3f0baafbba955163ddd0
  sets_current_task: SV5_13_JUMP_CONTRACT
---

# SV5_13_JUMP_CONTRACT — exact jump support and measurement data

TASK: SV5_13_JUMP_CONTRACT
EXPECTED RESULT: MapDesign/MCP/REPORTS/SV5_13_JUMP_CONTRACT_RESULT.md
NEXT: SV5_14_JUMP_SOLID — LOCKED / DO NOT START

## Required reads and binding

Use native `single_task_v1` Apply only after this package's `STAGE.py --check`
and `--stage` pass; immediately after Apply run `--verify-applied`. Read current
MCP 00/01/05/07/08/APPLY rules, Status, Master, this installed Task, all package
inputs, SPACE v5 rules/tasks, the SV5_12_FIX01 Task/Archive/Result/Binding, and
the current Player movement/grab constants without changing them. Record exact
inputs and before-state in `GENERATED/SV5_13_JUMP_CONTRACT/BINDING.json`.

## Write allowlist

- new `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpContract.cs`
- new `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpContractExport.cs`
- matching new `.meta` files
- new `Assets/_Game/Tests/EditMode/Map/SV5/Sv5JumpContractTests.cs` and `.meta`
- new `MapDesign/MCP/SV5/21_JUMP_CONTRACT_V5.md`
- `MapDesign/MCP/GENERATED/SV5_13_JUMP_CONTRACT/**`
- installed Task, Archive, matching Result, and normal Status lifecycle

Do not modify predecessor source/evidence, Player runtime, scene, prefab,
Packages, ProjectSettings, Master, or unrelated files. Do not restore, reset,
checkout, clean, delete, or overwrite unrelated dirty work.

## Required execution

1. Create typed immutable support, grab-edge, link, measurement-proof, and
   validation-state records matching CONTRACT C01-C04.
2. Implement one canonical local fixture factory for J1/J2/J3. It must use both
   support kinds and make J3 target a real exposed SOLID grab edge. Do not build
   a final room or traverse the world.
3. Implement exact directional `gap_air` and top-face `rise` calculation with
   explicit rejection reasons. Preserve one-way route semantics.
4. Export all C06 files from the same canonical objects and add the human-readable
   integration document. No handwritten CSV/SVG values disconnected from code.
5. Add the required tests. Confirm the targeted test path performs zero complete
   world builds and finishes using local fixtures.
6. Run compile and targeted tests, then:
   `python -X utf8 MapDesign/MCP/INPUTS/SV5_13_JUMP_CONTRACT/tools/check_jump_contract.py --project-root .`
7. Only after both pass, run the full SV5 regression exactly once. Do not rerun
   the full suite to fix ordinary failures.
8. Run `STAGE.py --post-readonly` before Finalize. Write a PASS Result only after
   all gates pass, finalize, create one atomic Task-owned commit, then build
   `SV5_13_JUMP_CONTRACT_REVIEW.zip` from exact commit blobs.

## Stop/report rules

Package/Git/state/owned-path collisions block before staging. Implementation or
targeted failures are iterative but may not weaken formulas or tests. The final
report includes commit/title, source constants observed, J1/J2/J3 measurements,
support/grab/state counts, test durations and XML SHA values, checker audit SHA,
readiness, full-suite execution count, Review ZIP SHA, and untouched dirty scope.
Stop with SV5_14 locked and no push.

