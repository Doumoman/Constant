# MAP17_01 Repair - Installed Task Body SHA Precondition

```text
TYPE: DIRECT REPAIR INSTRUCTION / NOT A single_task_v1 MCP_INBOX TASK
DO NOT PLACE THIS FILE IN MCP_INBOX
DO NOT CREATE A NEW FORMAL MAP TASK
CURRENT FORMAL TASK: MAP17_01_RESOLVE_ASSETS_AND_PLACE_GENERATED_CELLS
CURRENT BLOCKED RESULT SHA-256: e3e5dbec61dd03572b67440cf2dd33ca9d5f732fe97f8456508e7e40aa471ff7
```

## 0. Why This Repair Exists

`MAP17_01_RESOLVE_ASSETS_AND_PLACE_GENERATED_CELLS.md` was accepted by Phase A and installed as the Current Task.

However, the installed Task body still contains an outdated MAP16_09 installed task SHA in `## 2. 선행조건`.

```text
outdated body SHA:
fd24544c6ef2885e8cd7242d1ce1837e23fea140d353bd7959353b54f7b5938c

actual installed MAP16_09 task SHA:
2e2fdbc609bdb780177f502d60b8ca16ead8c03a454f36cfec22659a3000c103
```

This is not a generated terrain implementation failure. It is a task document precondition typo.

## 1. Required Current State

Before editing, verify:

```text
Current Task: MAP17_01_RESOLVE_ASSETS_AND_PLACE_GENERATED_CELLS
MAP17_01 status: CURRENT or BLOCKED-but-not-finalized
MAP17_02 status: LOCKED / NOT STARTED
MAP17_01 Result exists with STATUS: BLOCKED
MAP17_01 blocked result SHA-256:
e3e5dbec61dd03572b67440cf2dd33ca9d5f732fe97f8456508e7e40aa471ff7
unrelated staged files: 0
```

If Current Task is `NONE` or a different task, stop and report `BLOCKED`.

## 2. Exact Repair

Find the installed and archived MAP17_01 task files.

Required installed file:

```text
MapDesign/MCP/TASKS/MAP17_01_RESOLVE_ASSETS_AND_PLACE_GENERATED_CELLS.md
```

Expected archive location is under one of these roots:

```text
MapDesign/MCP_ARCHIVE/
MapDesign/MCP/ARCHIVE/
```

Use `rg` or equivalent to locate any archive copy whose basename is:

```text
MAP17_01_RESOLVE_ASSETS_AND_PLACE_GENERATED_CELLS.md
```

In the installed Task file and every exact archive copy, replace only this old SHA:

```text
fd24544c6ef2885e8cd7242d1ce1837e23fea140d353bd7959353b54f7b5938c
```

with this actual SHA:

```text
2e2fdbc609bdb780177f502d60b8ca16ead8c03a454f36cfec22659a3000c103
```

Do not change any other task text, YAML metadata, implementation scope, test list, or Result evidence requirements.

## 3. Post-repair Verification

After editing, verify:

```text
old SHA occurrences in installed MAP17_01 task: 0
new SHA occurrences in installed MAP17_01 task: at least 2
old SHA occurrences in MAP17_01 archive copies: 0
new SHA occurrences in MAP17_01 archive copies: at least 2 each
MAP17_02 remains LOCKED
unrelated staged files: 0
```

The corrected MAP17_01 task document should now have the same SHA in:

```text
mcp_patch.requires_installed_task.sha256
## 2. 선행조건 / MAP16_09 installed task SHA-256
```

## 4. Continue MAP17_01

After this repair, continue the already-open MAP17_01 task.

Recommended CLI command after the repair is complete:

```text
MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 수행해.

MCP_INBOX에는 새 후보를 넣지 마.
이미 열린 Current Task MAP17_01_RESOLVE_ASSETS_AND_PLACE_GENERATED_CELLS만 이어서 실행해.

Result가 PASS일 때만 Status Finalize와 atomic commit을 수행한 뒤 STOP해.
MAP17_02는 시작하지 마.
관련 없는 worktree 변경은 건드리거나 stage하지 마.
Git push는 하지 마.
```

It is acceptable to overwrite the previous `STATUS: BLOCKED` MAP17_01 Result with the real final Result for this same task.

## 5. No Regression Policy

This repair itself runs no Unity test.

When MAP17_01 continues, follow the MAP17_01 focused-only policy:

```text
MAP17_01 EditMode: required
prior task selections: 0
legacy 19347 selections: 0
PlayMode selections: 0
unfiltered test selections: 0
full regression runs: 0
```

Do not run broader regression unless MAP17_01 itself reports a real trigger.

## 6. Commit Handling

Do not make a separate commit for this repair only.

If MAP17_01 later passes, include the repaired task/archive file together with MAP17_01 implementation, tests, Result, and status finalize in the MAP17_01 atomic commit.

Expected commit subject remains:

```text
MAP17_01: resolve generated cell assets
```

