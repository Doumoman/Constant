---
mcp_patch:
  format: single_task_v1
  task_id: SV5_12_TREE_GRAB
  task_file: TASKS/SV5_12_TREE_GRAB.md
  requires_current_task: NONE
  requires_completed_task: SV5_11_FIX01
  requires_result:
    path: REPORTS/SV5_11_FIX01_RESULT.md
    status: PASS
    sha256: ab98863018e478999665010e1c61d77b9c9d84215be8887942f8c2da240a4949
  requires_installed_task:
    path: TASKS/SV5_11_FIX01.md
    sha256: 73b3259eb68bd24884006a097c12bd4da58d03d63f5e06db64a48ba249b8b1a4
  sets_current_task: SV5_12_TREE_GRAB
---

# SV5_12_TREE_GRAB — actual central tree grab geometry

TASK: SV5_12_TREE_GRAB
EXPECTED RESULT: MapDesign/MCP/REPORTS/SV5_12_TREE_GRAB_RESULT.md
NEXT: SV5_13_JUMP_CONTRACT — LOCKED / DO NOT START

## Binding and required reads

Use native `single_task_v1` Apply only after `INPUTS/SV5_12_TREE_GRAB/STAGE.py
--check` and `--stage` pass. Immediately after Apply run `--verify-applied`.

Read current MCP 00/01/05/07/08/APPLY rules, Status, Master, this installed Task,
all files in `INPUTS/SV5_12_TREE_GRAB`, the predecessor Task/Archive/Result/Binding,
the FIX01 Hub runtime/tests/exports/checker, and current movement/topology/product
implementations used by loops and sidepaths. Bind exact hashes and before-state in
`GENERATED/SV5_12_TREE_GRAB/BINDING.json`.

## Write allowlist

- `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5HubShell.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5HubConnectionRouter.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5HubShellExport.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5SpaceGraphPlan.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5SpaceGraphPlanner.cs`
  only where necessary to attach final tree occupancy/product
- new `Sv5TreeGrab.cs`, `Sv5TreeGrabExport.cs`, and matching `.meta` files
- `Assets/_Game/Tests/EditMode/Map/SV5/Sv5HubShellFix01Tests.cs`
- new `Sv5TreeGrabTests.cs` and matching `.meta`
- `MapDesign/MCP/SV5/20_TREE_GRAB_V5.md`
- `MapDesign/MCP/GENERATED/SV5_12_TREE_GRAB/**`
- installed Task, Archive, matching Result, and normal Status lifecycle

Do not modify predecessor Task/Archive/Result/Binding/generated evidence, unrelated
source, Player runtime, scene, prefab, Packages, ProjectSettings, or retired exports.
Do not restore/reset/checkout/clean/delete unrelated dirty files. If an allowlisted
existing source has a pre-existing unrelated change, stop as BLOCKED.

## Required execution

1. Reproduce the review finding: default has 4 logical connections but only one
   distinct physical external anchor; repeat has 5 logical and 3 physical anchors.
   Add permanent regression failures before changing selection.
2. Re-route selection so every accepted external anchor is distinct and route
   bodies do not merge outside the Hub. Preserve 4–6 connections and all constraint
   exclusions in default/repeat.
3. Replace AIR-only `MovementWitness` verification with typed edges from the active
   platformer movement rules. Export and independently recompute them. One-way is
   allowed; empty vertical climbs are not.
4. Compile the reserved central volume into irregular solid/grabbable tree cells,
   branches, clearance, landings, two bottom-to-upper routes, and recovery witnesses
   exactly as C03 specifies. Upward jump+grab gain is at most two cells.
5. Attach corrected connections and tree cells to final occupancy. Rebuild final
   movement/topology and one 9-state × 6-order product per profile.
6. Export every C05 artifact and performance/rejection data. No Sector symbols and
   no per-candidate whole-world work.
7. Run targeted Hub+Tree tests. When green, run the independent checker. Only when
   both are green, run one full SV5 regression. Never loop the full suite.
8. Run `STAGE.py --post-readonly` before Finalize. Write PASS Result only after all
   gates pass, finalize, make one atomic commit, and create exact-blob Review ZIP.

## Stop conditions and report

Package/Git/state/collision/owned-dirty mismatches block before staging. Ordinary
implementation/test failures are iterative and must not weaken the contract. If
four distinct physical anchors or two actual tree routes cannot be produced, report
the concrete coordinates/rejection counts and stop without fake IDs or readiness.

Final report includes commit/title, test counts/durations/XML hashes, default/repeat
physical anchor counts, connection directions, tree routes/levels/recovery counts,
9×6 product, build timings, readiness flags, Review ZIP SHA, and untouched dirty
scope. Stop with SV5_13 LOCKED and no push.

