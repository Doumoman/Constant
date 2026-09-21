---
mcp_patch:
  format: single_task_v1
  task_id: SV5_19_JUMP_RECOVERY
  task_file: TASKS/SV5_19_JUMP_RECOVERY.md
  requires_current_task: NONE
  requires_completed_task: SV5_18_JUMP_CLEARANCE
  requires_result:
    path: REPORTS/SV5_18_JUMP_CLEARANCE_RESULT.md
    status: PASS
    sha256: 140b6cef8e732c4b5fac897a15da4f5f321d510eb43fe99176df8504c83b0ee5
  requires_installed_task:
    path: TASKS/SV5_18_JUMP_CLEARANCE.md
    sha256: 4d855ec8f5272a91729f0800d2c7d20da3060405f0074f4f231475556b0bee68
  sets_current_task: SV5_19_JUMP_RECOVERY
---

# SV5_19_JUMP_RECOVERY — catch a representative miss and rejoin safely

TASK: SV5_19_JUMP_RECOVERY
EXPECTED RESULT: MapDesign/MCP/REPORTS/SV5_19_JUMP_RECOVERY_RESULT.md
NEXT: SV5_20_JUMP_PLAYER — LOCKED / DO NOT START

## Required reads and binding

After package preflight/stage/verification, use native single_task_v1 Apply and
immediately run `STAGE.py --verify-applied`. Read all package files and complete
SV5_13-18 Task/Archive/Result/Binding/source/tests/exports. Bind exact inputs and
before-state in `GENERATED/SV5_19_JUMP_RECOVERY/BINDING.json`.

## Write allowlist

- new `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpRecovery.cs`
- new `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpRecoveryExport.cs`
- their new `.meta` files
- new `Assets/_Game/Tests/EditMode/Map/SV5/Sv5JumpRecoveryTests.cs` and `.meta`
- new `MapDesign/MCP/SV5/27_JUMP_RECOVERY_V5.md`
- `MapDesign/MCP/GENERATED/SV5_19_JUMP_RECOVERY/**`
- installed Task, Archive, matching Result, and normal Status lifecycle

Predecessor source/evidence, Player, physics tuning, tree/Hub, world placement,
scenes, prefabs, Packages, ProjectSettings, Master, and unrelated files are
read-only. Preserve unrelated dirty work.

## Required execution

1. Reproduce all CONTRACT C08 negative cases before final PASS.
2. Derive exactly one deterministic AIR-sample miss probe for every main link.
3. Compose a sparse mirrored recovery overlay without touching any accepted main
   trace body/head cell or Grab contact.
4. Prove each vertical miss hits the first valid surface and has clear landing
   body/head cells.
5. Build an itemless one-way WALK/JUMP/DROP route from every catch to a same or
   earlier effective-takeoff checkpoint. Do not require reverse completion.
6. Revalidate all 18 main clearance traces after composing the overlay.
7. Export all CONTRACT C07 evidence and inspect the rendered SVG.
8. Add at least 28 local tests. Perform no world generation/search, Sector work,
   all-endpoint comparison, or per-candidate world copies.
9. Run compile and targeted tests, then:
   `python -X utf8 MapDesign/MCP/INPUTS/SV5_19_JUMP_RECOVERY/tools/check_jump_recovery.py --project-root .`
10. Only after targeted/checker/visual PASS run the full SV5 regression under
    CONTRACT C08, then post-readonly, matching PASS Result, Finalize, one atomic
    commit, and exact-blob `SV5_19_JUMP_RECOVERY_REVIEW.zip`.

## Stop and report

Package/Git/state/owned-path mismatch blocks before staging. Ordinary local
implementation/test failures are iterative and cannot weaken probe count,
first-catch rules, movement caps, checkpoint ordering, mirror, main-clearance
preservation, readiness, or test gates. Report recovery digest, overlay/group
counts, 18 probe/route results, checkpoint distribution, mirror proof, tests and
audits, Review ZIP SHA, and untouched dirty scope. Stop with SV5_20 locked and
no push.

