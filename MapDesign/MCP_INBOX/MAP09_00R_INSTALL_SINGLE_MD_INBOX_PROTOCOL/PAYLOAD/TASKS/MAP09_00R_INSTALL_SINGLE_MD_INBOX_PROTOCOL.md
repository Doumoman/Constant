# MAP09_00R - Install Single MD Inbox Protocol

```yaml
status_control:
  task_key: MAP09_00R_INSTALL_SINGLE_MD_INBOX_PROTOCOL
  result_file: REPORTS/MAP09_00R_INSTALL_SINGLE_MD_INBOX_PROTOCOL_RESULT.md
```

## TASK TYPE

```text
PROTOCOL-ONLY REMEDIATION / LAST LEGACY FOLDER PATCH
```

## Objective

MAP09_00 v1.0이 완료한 24개 V2 Unity 디렉터리와 모든 MAP00~08 산출물을 그대로 보존하고, 누락된 `single_task_v1` MCP_INBOX 규칙만 설치한다.

이 Task 뒤부터 사용자는 ZIP을 풀지 않는다. 다음 Task는 `MapDesign/MCP_INBOX/<TASK_ID>.md` 하나로 적용되어야 한다. 이 Task는 Assets, Runtime/Editor/Test C#, CSV, asmdef/asmref, Scene/Prefab, ProjectSettings/Packages를 변경하지 않으며 MAP09_01 기능 구현을 시작하지 않는다.

## Mandatory Read Order

1. `00_MCP_ENTRYPOINT.md`
2. `01_PROJECT_LOCKED_RULES.md`
3. `02_MCP_WORK_RULES.md`
4. `05_CHANGE_CONTROL_RULES.md`
5. `07_PATCH_APPLY_RULES.md`
6. `08_STATUS_FINALIZE_RULES.md`
7. `APPLY_PATCH_AND_RUN_CURRENT_TASK.md`
8. `MASTER_IMPLEMENTATION_TASK_LIST.md`
9. `06_IMPLEMENTATION_STATUS.md`
10. 이 Task
11. `REPORTS/MAP09_00_CREATE_V2_MODULE_STRUCTURE_RESULT.md`

Prior exact gates:

```text
MAP09_00 Result STATUS: PASS
MAP09_00 Result SHA-256: 4c825c9ac77257bf293b9be86282e0562e3272ec38f1a4f8f9a4ff860983d478
Installed MAP09_00 Task SHA-256: d3b4d6ffdb149823c1e2686ccded43897127aa0b8ea9bc74a3da0491f457ab63
Installed V2 Master SHA-256: 2f1fa53df4eb3687507c68d51167f681872622ed818e4835773a9c121e8ef4a7
MAP09_00 structure: APPROVED
Target directories/metas: 24/24
MAP00 approved directories: 36/36
Architecture fixtures: 10/10 PASS
Duplicate GUID groups: 0
Global Assets meta: 3840
Assets/_Game/Map meta: 611
Authoring manifest: f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb
Generated CSV: 0
```

The MAP09_00 Result used the v1.0 payload hashes and contains no `MCP SINGLE MD INBOX: APPROVED` gate. This remediation must not claim that protocol existed before this Task.

## WRITE ALLOWLIST

### Existing MCP documents - exact 4

```text
MapDesign/MCP/00_MCP_ENTRYPOINT.md
MapDesign/MCP/05_CHANGE_CONTROL_RULES.md
MapDesign/MCP/07_PATCH_APPLY_RULES.md
MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md
```

### New inert template - exact 1

```text
MapDesign/MCP/TEMPLATES/SINGLE_TASK_PATCH_TEMPLATE.md
```

### Result - exact 1

```text
MapDesign/MCP/REPORTS/MAP09_00R_INSTALL_SINGLE_MD_INBOX_PROTOCOL_RESULT.md
```

No other file is task-owned. `06_IMPLEMENTATION_STATUS.md` is changed only later by Status Finalize, never by Task Execution.

## READ ALLOWLIST

```text
Mandatory Read Order files
MapDesign/MCP_INBOX immediate entries and applied legacy MAP09_00 folder names
MapDesign/MCP_ARCHIVE immediate entries
MapDesign/MCP/TASKS immediate filenames
Git status/diff path inventory
The five approved asmdef hashes and Assets/meta counts from MAP09_00 Result
```

Do not read MAP09_01+ Task bodies, Legacy/Stage/P6/P11 source bodies, Scene/Prefab YAML, or unrelated dirty file contents.

## Required `single_task_v1` File Shape

Every future inbox file must be named exactly `<TASK_ID>.md` and begin with this metadata block before its Task body:

```yaml
mcp_patch:
  format: single_task_v1
  task_id: <TASK_ID>
  task_file: TASKS/<TASK_ID>.md
  requires_current_task: NONE
  requires_completed_task: <PREVIOUS_TASK_ID>
  requires_result:
    path: REPORTS/<PREVIOUS_TASK_ID>_RESULT.md
    status: PASS
    sha256: <64-lowercase-hex>
  requires_installed_task:
    path: TASKS/<PREVIOUS_TASK_ID>.md
    sha256: <64-lowercase-hex>
  sets_current_task: <TASK_ID>
```

The remainder of the same MD is the complete Task body. There is no separate manifest, payload directory, README, run prompt, or ZIP.

## Required Apply State Machine

Update the four MCP documents so they all agree on this exact behavior:

1. Scan `MCP_INBOX` immediate children for legacy unapplied patch directories and `*.md` single-task candidates.
2. Require exactly one total candidate. Zero candidates with `Current Task NONE` means clean stop; two or more candidates means `BLOCKED`.
3. For MAP09_01+, legacy folder candidates are retired and `BLOCKED`. MAP09_00R itself remains the last allowed legacy folder patch.
4. Require filename stem, `task_id`, `task_file` stem, and `sets_current_task` to be identical.
5. Require `Current Task: NONE`, predecessor row `COMPLETE`, new Task row exactly once as `LOCKED`, and Task ID already present in Master.
6. Require previous Result file exact `STATUS: PASS`, exact Result SHA-256, and exact installed previous Task SHA-256.
7. Require every SHA value to be exactly 64 lowercase hexadecimal characters.
8. Compute inbox MD SHA-256, copy the entire file byte-for-byte to `MCP/TASKS/<TASK_ID>.md`, and verify installed SHA equality.
9. If destination exists, reuse only when byte-identical; otherwise `BLOCKED`. Never overwrite a different Task file.
10. Patch Apply may change only two Status fields: `Current Task NONE -> <TASK_ID>` and that Task row `LOCKED -> CURRENT`.
11. Verify total row count unchanged and status delta exact: `COMPLETE 0 / CURRENT +1 / LOCKED -1`.
12. Move the original inbox MD to `MCP_ARCHIVE/<TASK_ID>.md` after successful install/status validation.
13. If archive destination exists, reuse only when byte-identical; otherwise `BLOCKED`.
14. A single MD does not use `.APPLIED`.
15. Task Execution may not edit Status. PASS Result permits Status Finalize to change `CURRENT -> COMPLETE` and `Current Task -> NONE`.
16. Task commit includes the archived single MD, installed Task MD, task-owned implementation/test/meta, Result, and finalized Status only.
17. Do not automatically start the next Task. Stop after commit. Do not push.
18. Unknown Task IDs outside the installed 215-row Master/Status are `BLOCKED` and require a separate contract-change patch.

## Document Responsibilities

- `00_MCP_ENTRYPOINT.md`: bump to v1.4; make `single_task_v1` the normal Phase A input and preserve Phase A→B→C→D→STOP.
- `05_CHANGE_CONTROL_RULES.md`: authorize only the exact Patch Apply two-field Status open; preserve Task Execution prohibition and Finalize close; include archive/install files in atomic commit scope.
- `07_PATCH_APPLY_RULES.md`: bump to v2.0; own all validation, byte-copy, Status delta, archive, collision, and BLOCKED rules above.
- `APPLY_PATCH_AND_RUN_CURRENT_TASK.md`: scan/apply exactly one MD, execute only Current Task, finalize only on PASS, commit, and stop.
- `SINGLE_TASK_PATCH_TEMPLATE.md`: contain the exact metadata schema with inert angle-bracket placeholders, Task body headings, Result header requirements, and next-Task lock rule. It must not contain a live MAP09_01 body or valid fake hashes.

## Collision and Failure Policy

Any precondition, path, hash, state, or byte mismatch is `BLOCKED`. Do not auto-correct Status, overwrite Task/archive files, delete candidates, weaken validation, or fall back to executing the MD body directly from INBOX.

Preserve unrelated changes. If any of the exact four MCP documents has an unrelated overlapping edit that cannot be retained, stop as `BLOCKED`.

## Implementation Steps

1. Confirm `215 rows = 106 COMPLETE / MAP09_00R CURRENT / 108 LOCKED`.
2. Verify MAP09_00 Result/Task/Master hashes and structure PASS evidence.
3. Record pre-task Git path inventory and hashes of the exact four MCP documents.
4. Update the four documents with the exact shared state machine and responsibilities.
5. Create the inert template.
6. Verify no future Task MD was created in INBOX, TASKS, or ARCHIVE.
7. Perform read-only dry-run cases against the written rules/template.
8. Verify task-owned diff is exact four modified documents + one template + Result.
9. Verify Assets, C#, CSV, asmdef/asmref, Scene/Prefab, ProjectSettings/Packages changes are 0.
10. Write the Result; finalize only on PASS; create one atomic commit; stop.

## Required Dry-Run Cases

```text
D01 valid single candidate happy path: PASS
D02 filename/task_id/task_file/sets_current_task mismatch: BLOCKED
D03 Current Task not NONE: BLOCKED
D04 predecessor not COMPLETE: BLOCKED
D05 previous Result missing/not PASS/hash mismatch: BLOCKED
D06 installed previous Task hash mismatch: BLOCKED
D07 unknown or non-LOCKED Task ID: BLOCKED
D08 multiple MD candidates or mixed legacy+MD candidates: BLOCKED
D09 installed Task destination different-content collision: BLOCKED
D10 archive different-content collision: BLOCKED
D11 exact byte-copy and installed SHA equality: PASS
D12 exact two-field Status open and unchanged row count: PASS
D13 Status Finalize closes only after PASS: PASS
D14 automatic next Task start: FORBIDDEN
```

These are protocol/document dry-runs. Do not create a live MAP09_01 file to test them.

## Static Gates

```text
MCP protocol documents modified: 4/4 exact
Single Task template created: 1/1
Unexpected MCP files modified/created: 0/0
Assets files/metas modified/created: 0/0
Runtime/Editor/Test C# modified/created: 0/0
Authoring CSV/meta changes: 0/0
Authoring manifest: unchanged
Generated CSV created: 0
Scene/Prefab changes: 0/0
ProjectSettings/Packages changes: 0/0
asmdef/asmref changes: 0/0
Live MAP09_01 INBOX/TASKS/ARCHIVE files: 0/0/0
MAP09_01+ production symbol hits: 0
git diff --check errors: 0
```

## Commit Requirement

Create exactly one atomic commit after PASS/finalize.

```text
Subject: MAP09_00R: install single MD inbox protocol

Body:
- Preserve the approved MAP09_00 V2 module structure without Asset changes
- Install single_task_v1 candidate, validation, Task install, Status open, and archive rules
- Preserve PASS-gated Result, Status Finalize, atomic commit, stop, and next-task lock behavior
- Add the inert single Task patch template
- Verify all 14 protocol dry-run cases and zero out-of-scope changes
- Keep MAP09_01_FREEZE_BASELINE_AND_REGISTER_V2_PASSES locked / do not start
```

Do not stage unrelated files. Do not push.

## Result Report Requirements

Create `REPORTS/MAP09_00R_INSTALL_SINGLE_MD_INBOX_PROTOCOL_RESULT.md` with:

```text
TASK: MAP09_00R_INSTALL_SINGLE_MD_INBOX_PROTOCOL
STATUS: PASS | FAIL | BLOCKED
MAP09_00R: COMPLETE ELIGIBLE only if PASS
MCP SINGLE MD INBOX: APPROVED only if PASS
MAP09_01_FREEZE_BASELINE_AND_REGISTER_V2_PASSES: LOCKED / DO NOT START
```

Report:

```text
Legacy remediation patch apply and installed payload hashes
MAP09_00 Result/Task/Master hash gates
Exact four document before/after hashes and changed sections
Template path/hash and inert-placeholder validation
All D01-D14 dry-run results
Exact Status counts before apply, after apply, and after finalize
Assets/code/CSV/asmdef/Scene/Prefab/ProjectSettings/Packages zero-change gates
Live MAP09_01 file counts 0/0/0
git diff --check
Atomic commit subject and immutable hash handoff
```

## Done Condition

```text
MAP09_00 structure PASS preserved
Four MCP documents updated exactly
Template created with inert placeholders
D01-D14 all expected outcomes PASS
No live MAP09_01 file created or started
All Assets and implementation changes 0
Result created with MCP SINGLE MD INBOX: APPROVED
Status finalized only after PASS
Atomic commit created
MAP09_01 remains LOCKED / DO NOT START
```
