---
mcp_patch:
  format: single_task_v1
  task_id: SV5_02_RULES
  task_file: TASKS/SV5_02_RULES.md
  requires_current_task: NONE
  requires_completed_task: SV5_01_APPROVAL_BASELINE
  requires_result:
    path: REPORTS/SV5_01_APPROVAL_BASELINE_RESULT.md
    status: PASS
    sha256: 78f9e84bf82b9640c51a5ee4bc2847610a296ebd26e0849706d3ce58601a37b6
  requires_installed_task:
    path: TASKS/SV5_01_APPROVAL_BASELINE.md
    sha256: c63acc8cf464ae837d5e83efef9976f5e12f42f1cf8a32f2634ba32cf587d5ed
  sets_current_task: SV5_02_RULES
---

# SV5_02_RULES — R2 active-rule registration

```text
TASK: SV5_02_RULES
INPUT HANDOFF: MapDesign/MCP_INBOX/SV5_02_R2_START.md
INPUT REVISION: R2
SOURCE SPEC: MapDesign/MCP/INPUTS/SV5_02_R2/SV5_02_RULES.md
EXPECTED RESULT: MapDesign/MCP/REPORTS/SV5_02_RULES_RESULT.md
NEXT: SV5_03_BINDINGS remains LOCKED / DO NOT START
```

status_control:
  task_key: SV5_02_RULES
  result_file: REPORTS/SV5_02_RULES_RESULT.md

## Objective

Register the supplied SV5 rule index, sentence coverage, and task ReadSet
without reopening the 45-plan registration or changing gameplay, Assets, Unity,
map data, or RMAP work. This bound Task is issued from the R2 source input;
the source specification itself is not executed directly.

## Required reads

- `SV5_02_R2_README.md`, `SV5_02_R2_VERIFY.py`, and
  `MCP_INBOX/SV5_02_R2_START.md`; run the UTF-8 package check and the local
  pre-apply check with `MapDesign` as map root and the actual local SV5_01
  Result (never either R1/R2 input reference copy).
- `MCP/INPUTS/SV5_02_R2/{REPAIR.md,REVISION.json,SOURCE_LOCK.json,
  SV5_02_RULES.md,PROTOCOL_APPEND.md}` and its three candidate files.
- MCP entry, apply, change-control, finalize, status, Master, and the
  installed/archive SV5_01 Task plus the actual local SV5_01 PASS Result.
- `MCP/SV5/{00_APPROVAL_BASELINE.md,01_SEQUENCE_V5.md,02_PROTOCOL_V5.md}`;
  `MCP/GENERATED/SV5_01/{BASELINE.json,PLAN_LINK.json,BINDING.json}`; and the
  R2-locked SV5 rules, memory, task list, and cited v4 detail sources.

## Write allowlist

- `MCP/SV5/03_RULES_V5.md`, `04_RULE_COVERAGE_V5.csv`,
  `05_RULE_READSET_V5.json`, and the one R2 marker block appended to
  `MCP/SV5/02_PROTOCOL_V5.md`.
- `MCP/GENERATED/SV5_02/{RULE_REGISTRATION.json,BINDING.json,validation.json}`
  and `MCP/REPORTS/SV5_02_RULES_RESULT.md`.
- This installed Task, its byte-identical Archive, and normal SV5_02
  Apply/Finalize state transitions only.

Do not edit source inputs (including R1), prior Task/Archive/Result evidence,
Master, Assets, Scene/Prefab, Player, map-generation code, runtime CSV,
RMAP18/19, or any SV5_03+ Task. Do not push.

## Required work

1. Before Apply, require the R2 package and local pre-apply checks to pass;
   record the actual SV5_01 Result, installed/archive Task, Finalize commit,
   parent, state, and SHA roles. The R2 correction uses
   `MCP/GENERATED/SV5_01` and must not create `MapDesign/GENERATED` aliases.
2. Install/archive this whole bound Task byte-for-byte and open only SV5_02 by
   the normal `single_task_v1` transaction. Keep SV5_03 LOCKED.
3. Register the three R2 candidate files byte-for-byte at their named active
   destinations. Never rewrite or summarize their rule content.
4. Append the exact `SV5_02_RULES_BEGIN/END` block from `PROTOCOL_APPEND.md`
   once only, preserving the existing protocol bytes before it. Reuse an
   already-identical block; never duplicate it.
5. Create the three generated evidence files. BINDING must distinguish the
   R2 INBOX/SOURCE_SPEC/BIND/installed/archive roles, source-lock evidence,
   actual pre-Apply state, SV5_01 Finalize commit and parent. validation must
   record pre-Apply protocol SHA separately from its changed post-registration
   SHA, the 74 covered source statements, unique JUMP-01~14, source/candidate
   byte identity, and `UNITY_NOT_RUN` / `GAME_CODE_NOT_RUN`.
6. Write a matching `STATUS: PASS` Result only after all static checks pass.
   It must state that Finalize and commit have not occurred at Result-writing
   time, and that no Unity launch/compile/test/build/bake occurred.

## Done conditions

- R2 local precheck passes without changing predecessor SHA or creating a fake
  GENERATED path; SV5_01 is COMPLETE, SV5_02 opens from LOCKED, and Current
  transitions only through SV5_02.
- All three active rule files equal their R2 candidates; all ReadSet source
  paths/SHA values match SOURCE_LOCK; Coverage exactly covers the 74 meaningful
  source lines and contains only JUMP-01 through JUMP-14.
- JUMP-07 and JUMP-10 remain `AUTHORING_DEFAULT`; the solid ratio remains
  unset; horizontal 3–4-cell candidates remain unverified; static registration
  is not represented as Player or progression PASS.
- The protocol has exactly one appended marker block, all 45 SV5 IDs/order and
  prior evidence are preserved, and only this Task reaches Finalize.

## Result, Finalize, and stop

After a PASS Result, Finalize only SV5_02 and commit only this Task's Archive,
installed Task, active registration/protocol files, generated evidence, Result,
and final Status. Do not amend the Result to add the post-Finalize commit SHA.
Stop after reporting actual final state and commit; do not open SV5_03.
