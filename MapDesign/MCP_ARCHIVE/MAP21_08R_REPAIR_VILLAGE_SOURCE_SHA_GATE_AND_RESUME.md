```yaml
mcp_repair:
  format: blocked_current_task_repair_v1
  repair_id: MAP21_08R_REPAIR_VILLAGE_SOURCE_SHA_GATE_AND_RESUME
  repair_of: MAP21_08_COMPLETE_MOONPALACE_VILLAGE
  reason: MAP13_04 source Result byte SHA mismatch in precondition gate
  expected_current_task: MAP21_08_COMPLETE_MOONPALACE_VILLAGE
  expected_blocked_result:
    path: REPORTS/MAP21_08_COMPLETE_MOONPALACE_VILLAGE_RESULT.md
    status: BLOCKED
    sha256: 58ee844830b25e86b7254a15afef73503a29e11d7a72aa640ca3014a316cf5d2
  next_task_must_remain_locked: MAP21_09_COMPLETE_FORGE_BOSS_AND_OPTIONAL_REGIONS
```

# MAP21_08R - Repair Village Source SHA Gate and Resume MAP21_08

```text
REPAIR: MAP21_08R_REPAIR_VILLAGE_SOURCE_SHA_GATE_AND_RESUME
REPAIR OF: MAP21_08_COMPLETE_MOONPALACE_VILLAGE
STATUS: REPAIR CURRENT BLOCKED TASK ONLY
NEXT: MAP21_09_COMPLETE_FORGE_BOSS_AND_OPTIONAL_REGIONS
NEXT STATUS: MUST REMAIN LOCKED
```

## 0. Purpose

This is not a new MAP phase task.

`MAP21_08_COMPLETE_MOONPALACE_VILLAGE` was correctly installed and opened, but it stopped before implementation because the task file used an exact byte SHA for an older MAP13_04 Result file.

Observed blocker:

```text
precondition: MAP13_04 Village shell Result SHA-256
expected: 9b4ef18603b191981bcb5acb29b76eca4eb0889fe9d6017a5f7788432f35c4ca
actual:   f5419f1218885ebe89a24d8106a481df93da80d9c25821f4398748f2ab96ab26
result task line: TASK: MAP13_04_IMPLEMENT_VILLAGE_SHELL_FACILITIES_AND_ACCESS
result status line: STATUS: PASS
```

The mismatch is a precondition metadata mismatch, not a MAP21_08 implementation failure.

This repair must:

```text
1. Patch the installed MAP21_08 task file and its archived source copy.
2. Keep MAP21_08 as the current task.
3. Keep MAP21_09 locked.
4. Resume MAP21_08 implementation from the corrected task.
5. Produce a final MAP21_08 Result.
6. Run only focused MAP21_08 EditMode tests.
7. Perform Status Finalize and atomic commit only if MAP21_08 passes.
```

## 1. User-Facing Report Requirement

The final `MAP21_08_COMPLETE_MOONPALACE_VILLAGE_RESULT.md` must keep the normal MAP21_08 report shape and include:

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
## Village Layout and Facility Summary
## Road Door and Access Witness Summary
## State Variant and Marker Summary
## Static Safety and Non-Execution Summary
## Snapshot and Digest Summary
## Focused Validation Summary
## No Legacy Regression Boundary Notes
## Final Status Evidence
```

It must also include a short repair note:

```text
## Repair Note
MAP21_08 initially blocked on a MAP13_04 source Result byte SHA mismatch.
The repair changed historical MAP13/MAP18 source Result checks from exact byte SHA gates to read-only TASK/STATUS/PASS checks with observed SHA recording.
MAP21_07 remains the exact immediate predecessor gate.
No MAP13/MAP18/MAP21_04/05/06/07 source artifact was rewritten or regenerated.
```

## 2. Preconditions Before Repair

Confirm:

```text
MapDesign/MCP/TASKS/MAP21_08_COMPLETE_MOONPALACE_VILLAGE.md exists
MapDesign/MCP_ARCHIVE/MAP21_08_COMPLETE_MOONPALACE_VILLAGE.md exists
MapDesign/MCP/REPORTS/MAP21_08_COMPLETE_MOONPALACE_VILLAGE_RESULT.md exists
MAP21_08 Result STATUS: BLOCKED
MAP21_08 Result reason mentions MAP13_04 source Result SHA-256 mismatch
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md has MAP21_08 CURRENT
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md has MAP21_09 LOCKED
```

Confirm the real source Result that blocked:

```text
MapDesign/MCP/REPORTS/MAP13_04_IMPLEMENT_VILLAGE_SHELL_FACILITIES_AND_ACCESS_RESULT.md exists
TASK: MAP13_04_IMPLEMENT_VILLAGE_SHELL_FACILITIES_AND_ACCESS
STATUS: PASS
Observed SHA-256:
f5419f1218885ebe89a24d8106a481df93da80d9c25821f4398748f2ab96ab26
```

If any condition above fails:

```text
STATUS: BLOCKED
reason: repair precondition mismatch
created/changed production code files: 0
MAP21_09 started: NO
STOP
```

## 3. Repair Edit

Patch both files identically:

```text
MapDesign/MCP/TASKS/MAP21_08_COMPLETE_MOONPALACE_VILLAGE.md
MapDesign/MCP_ARCHIVE/MAP21_08_COMPLETE_MOONPALACE_VILLAGE.md
```

Replace the strict historical source Result byte-SHA gate in section `## 2. 선행조건` with this policy.

```text
Historical MAP13/MAP18 source Result verification policy:

For MAP13_04, MAP13_05, MAP13_09, and MAP18_06 Result files:
- require file exists
- require TASK line matches the expected task id
- require STATUS: PASS
- compute and record the observed file SHA-256
- do not fail only because the observed byte SHA differs from the authoring note

Reason:
These older source Results may differ by local finalization, line ending, or report note edits while preserving the approved TASK/STATUS contract. MAP21_08 must not rewrite or regenerate them.

The observed source Result SHAs must be copied into the MAP21_08 digest manifest and final Result.

Immediate predecessor gate remains strict:
- MAP21_07 Result SHA-256 must equal 68170bda01e3f2df4fbf14e9e841b04be58b3b61b1b18de9af3e5e2a3a486c8b
- MAP21_07 installed Task SHA-256 must equal 4bee068adbf40ee2d55afaea0b35d6e3d635079de2ee6f23f9e0636b0fbf4321
- MAP21_08 handoff digest must equal 7cd8adb0e27cffad1a4187400c6e72cd47e24291663aa5fe69457096efb535e0
```

Keep all MAP21_08 production data requirements unchanged.

Do not change:

```text
task id
current task
next task
allowed files
forbidden files
Village layout/facility/road/door/state contracts
focused MAP21_08 test list
no-regression counters
finalize and commit rules
```

## 4. Resume MAP21_08 Work

After the repair edit, resume the current `MAP21_08_COMPLETE_MOONPALACE_VILLAGE` task.

The task still owns only MoonPalace Village production static data:

```text
1x1 / 2x1 / 1x2 Village layout profiles
Kitchen and Repair fixed facilities
OptionalRest / OptionalStorage / OptionalMarket / OptionalLore facility authoring
central road cells
door markers
road-return witnesses
3 NPC markers per layout
2 inventory markers per layout
1 shopkeeper marker per layout
Normal / Friendly / IndividualHostile / AllHostile / Evacuation state variant manifests
deterministic MAP21_08 CSV and JSON digests
MAP21_09 handoff digest only after focused pass
```

Do not create gameplay systems:

```text
NPC spawn
NPC AI
combat
shop inventory
price
purchase/sell
repair/crafting/kitchen logic
door collider
door lock/open/close/path blocking
hostile/evacuation runtime state machine
save/load
PlayerPrefs
world/sector placement
generated world
Tilemap
Scene
Prefab
Collider
Addressables
runtime GameObject
renderer
validation runner
replay
rollback
```

## 5. Focused Validation Only

Run only:

```text
EditMode category MAP21_08
```

Expected:

```text
Discovered: 15
Executed: 15
Passed: 15
Failed: 0
Skipped: 0
Inconclusive: 0
```

No broad validation:

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
FULL REGRESSION RUNS: 0
MAP13 CATEGORY RERUNS: 0
MAP18 CATEGORY RERUNS: 0
MAP19_09 SCALE AUDIT RERUNS: 0
MAP20_01 GENERATOR RUN RERUNS: 0
MAP20_02 OVERLAY SAMPLE REGENERATION RUNS: 0
MAP20_03 DETAIL SAMPLE REGENERATION RUNS: 0
MAP20_04 NAVIGATION SAMPLE REGENERATION RUNS: 0
MAP20_05 REPLAY EXPORT SAMPLE REGENERATION RUNS: 0
MAP20_06 EXIT AUDIT REGENERATION RUNS: 0
MAP21_01 PROFILE TILE SHELL REGENERATION RUNS: 0
MAP21_02 MICROPATTERN REGENERATION RUNS: 0
MAP21_03 CRATER ROOT CLUSTER REGENERATION RUNS: 0
MAP21_04 MILL DOUGH CLUSTER REGENERATION RUNS: 0
MAP21_05 ACTIVITY EVENT REGENERATION RUNS: 0
MAP21_06 BOUNDARY REGENERATION RUNS: 0
MAP21_07 CORE RESOURCE REGENERATION RUNS: 0
```

## 6. Allowed Files

Repair edit allowed:

```text
MapDesign/MCP/TASKS/MAP21_08_COMPLETE_MOONPALACE_VILLAGE.md
MapDesign/MCP_ARCHIVE/MAP21_08_COMPLETE_MOONPALACE_VILLAGE.md
MapDesign/MCP/REPORTS/MAP21_08_COMPLETE_MOONPALACE_VILLAGE_RESULT.md
MapDesign/MCP_ARCHIVE/MAP21_08R_REPAIR_VILLAGE_SOURCE_SHA_GATE_AND_RESUME.md
```

After repair, MAP21_08 implementation allowed files remain:

```text
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/MoonPalaceVillageProduction.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/MoonPalace/MoonPalaceVillagePublisher.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/MoonPalace/MoonPalaceVillageProductionTests.cs
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_08/**
MapDesign/MCP/GENERATED/MAP21_08/**
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

Forbidden:

```text
MAP13/MAP18/MAP21_04/05/06/07 source rewrite or regeneration
MAP21_09 start
any unrelated worktree change
any unrelated staged file
Git push
```

## 7. Finalize and Commit

If MAP21_08 passes, finalize the original current task:

```text
MAP21_08_COMPLETE_MOONPALACE_VILLAGE: COMPLETE
Current Task: NONE
MAP21_09_COMPLETE_FORGE_BOSS_AND_OPTIONAL_REGIONS: LOCKED
```

Atomic commit subject:

```text
MAP21_08 complete moonpalace village
```

Commit scope may include:

```text
MAP21_08 repaired task file/archive
MAP21_08R repair archive if used by local protocol
MAP21_08 Result
MAP21_08 authoring CSV/meta files
MAP21_08 generated JSON files
MoonPalaceVillageProduction.cs and .meta if new
MoonPalaceVillagePublisher.cs and .meta if new
MoonPalaceVillageProductionTests.cs and .meta if new
06_IMPLEMENTATION_STATUS.md finalize change
```

Do not stage unrelated files. Do not push.

## 8. Stop Rule

After the final MAP21_08 Result and commit:

```text
STOP
Do not create MAP21_09 files.
Do not run MAP21_09.
Do not run MAP13/MAP18 categories.
Do not run PlayMode, legacy regression, unfiltered tests, or full regression.
```
