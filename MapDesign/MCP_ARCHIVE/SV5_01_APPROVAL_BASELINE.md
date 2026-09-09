---
mcp_patch:
  format: single_task_v1
  task_id: SV5_01_APPROVAL_BASELINE
  task_file: TASKS/SV5_01_APPROVAL_BASELINE.md
  requires_current_task: NONE
  requires_completed_task: RMAP17_WORLD_BAKE
  requires_result:
    path: REPORTS/RMAP17_WORLD_BAKE_RESULT.md
    status: PASS
    sha256: 53109e3b24e3df8191e634d7ec2e3f06087108965e5f9bc2af937f030a563c6b
  requires_installed_task:
    path: TASKS/RMAP17_WORLD_BAKE.md
    sha256: 5017331f8083ee8cc57cb8fed771c9d636df3248979ce39fa7efc43b2f018e46
  sets_current_task: SV5_01_APPROVAL_BASELINE
---

# SV5_01_APPROVAL_BASELINE — approval baseline registration

```text
TASK: SV5_01_APPROVAL_BASELINE
STATUS: CURRENT after normal Apply
INPUT HANDOFF: MapDesign/MCP_INBOX/SV5_START.md
SOURCE SPEC: MapDesign/MCP/INPUTS/SV5/tasks/SV5_01_APPROVAL_BASELINE.md
EXPECTED RESULT: MapDesign/MCP/REPORTS/SV5_01_APPROVAL_BASELINE_RESULT.md
NEXT: SV5_02_RULES
NEXT STATUS: LOCKED / DO NOT START
```

## Objective and registration boundary

Register the approved 624×416 spatial-composition baseline and the ordered
SV5_01~SV5_45 plan without changing game implementation. This bound Task is
issued from the fixed `source_spec_v1` only after the user-authorized one-time
SV5 registration boundary has added all 45 IDs to the active Master/Status.
That boundary is recorded in `MCP/SV5/00_APPROVAL_BASELINE.md`; it does not
relax the normal `single_task_v1` rules for SV5_02 onward.

The prior root-INBOX handoff files are not task candidates: their bodies do not
declare `mcp_patch.format: single_task_v1`. Preserve them. This Task is the
only bound execution candidate and must be installed and archived byte-for-byte.

## Required reads

- `SV5_README.md`, `SV5_FILES.json`, `SV5_VERIFY.py`, and
  `MCP_INBOX/SV5_START.md`; verify their documented SHA relationships before
  using the package.
- `MCP/INPUTS/SV5/SPACE_V5_MEMORY.md`, `SPACE_V5_RULES.md`,
  `SPACE_V5_TASKS.md`, `TASKS.csv`, `TASKS.json`, `SHA256.json`,
  `PLAN_ORIGIN.json`, `baseline/BASELINE_SHA.json`, `world_cells.csv`,
  `regions.json`, `connections.json`, and `validation.json` only through the
  supplied verifier or bounded summaries.
- The current MCP entry/apply/change/finalize/status rules, Master, installed
  RMAP17 Task and current RMAP17 PASS Result. The package's reported RMAP17
  copy is historical evidence only, not current-state proof.
- `MCP/SV5/00_APPROVAL_BASELINE.md`, `01_SEQUENCE_V5.md`, and
  `02_PROTOCOL_V5.md` as the locally registered plan records.

## Write allowlist

- `MapDesign/MCP/SV5/`, the SV5 section of
  `MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md`, and the corresponding
  SV5 status rows created by the one-time registration boundary.
- `MapDesign/MCP/GENERATED/SV5_01/` and
  `MapDesign/MCP/REPORTS/SV5_01_APPROVAL_BASELINE_RESULT.md`.
- This installed Task, its matching Archive, and only the normal Apply/Finalize
  state transitions for `SV5_01_APPROVAL_BASELINE`.

Do not write Assets, Scenes, Prefabs, Player/collision values, map-generation
code, runtime CSV, ProjectSettings, Packages, RMAP18/19, or any SV5_02+ Task.
Do not alter source inputs, the supplied verifier, prior Tasks/Results, or
unrelated dirty files.

## Required work and evidence

1. Record the pre-registration HEAD, Master/Status hashes, Current Task,
   counts, RMAP17 current Task/Result SHA, and all pre-existing dirty paths.
2. Confirm the 45 IDs are unique and identically ordered in the installed
   plan records and the supplied CSV/JSON/MD. SV5_01 is the sole opened Task;
   SV5_02~SV5_45 remain LOCKED.
3. Run `python -X utf8 SV5_VERIFY.py --manifest-sha
   74d4435f8128d44943b54774416329e027a7c8f584e42225c0504e600e2f5456`.
   Require `PASS_LOCAL_INPUTS`, 32 files, 45 tasks, 624×416 / 259,584 cells,
   valid baseline artifact hashes, 101 regions, and no errors.
4. Create `APPROVAL.md`, `BASELINE.json`, `PLAN_LINK.json`, and `BINDING.json`
   from observed local paths and hashes. Separate the handoff/source/bound/
   installed/archive SHA roles and mark physics/Unity gameplay as NOT_RUN.
5. Verify baseline cells cover each in-bounds coordinate once, the declared
   S/O/A/R counts and regions are valid, and each PDF/PNG/ZIP/data digest
   matches `BASELINE_SHA.json`. Do not treat this static check as a Unity pass.
6. Write a PASS Result only after the byte identity, registration, input,
   baseline, plan-link, source-boundary, and no-game-write checks all pass.

## Result, Finalize, and stop

The Result must include every input role SHA, bound/installed/archive SHA,
output SHA, actual pre/post state, commit baseline, verification results, and
the explicit `NOT_RUN` game/Unity scope. On PASS, Finalize only this Task:
`SV5_01_APPROVAL_BASELINE` CURRENT→COMPLETE and Current→NONE. Commit only
Task-owned plan/document/evidence files; do not push. `SV5_02_RULES`,
RMAP18, and RMAP19 remain LOCKED / NOT STARTED.
