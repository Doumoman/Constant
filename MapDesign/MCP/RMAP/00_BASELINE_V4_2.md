# RMAP v4.2 Baseline and Registration Boundary

## Purpose

This document records the baseline for the first registration of the RMAP01~19
execution plan. It is a documentation contract change, not a claim that the
new gameplay or world pipeline has already been implemented.

## One-time registration boundary

- The source is `MapDesign/MCP_INBOX/RMAP01_REBASE.md`.
- This is the sole exception that registers previously unknown RMAP IDs before
  ordinary `single_task_v1` issuance can validate them.
- RMAP01 is installed byte-for-byte, opened once, investigated, finalized, and
  committed. RMAP02~19 remain `LOCKED`.
- From RMAP02 onward, the normal SHA-256 predecessor Result and installed Task
  checks in `07_PATCH_APPLY_RULES.md` apply without relaxation.
- No generic protocol rule is changed. This boundary cannot be reused for an
  unrelated unregistered task.

## Observed baseline

| Item | Observed value | Interpretation |
| --- | --- | --- |
| Branch / HEAD | `main` / `f4da626c5cc509954a41e67b24e5d7113d01a50b` | Registration baseline. |
| Current Task | `NONE` | No task was resumed or reset. |
| Status rows | `221 COMPLETE / 0 CURRENT / 0 LOCKED` | Counted from the active status table before RMAP registration. |
| RMAP IDs in MCP documents | `0` | No duplicate registration existed. |
| Inbox candidates | `RMAP01_REBASE.md` only | The sole immediate-child Markdown input. |
| Source PDF | not present in the project scan | The v4.2 task body is the available registration authority. |

The pre-existing dirty paths are outside this task and are preserved: the
VIS01 Scene, `Constant.slnx`, three untracked TerrainClusters `.meta` files,
an untracked MAP17 inbox body, and two unrelated MCP reports. They are neither
modified nor staged by RMAP01.

## RUN06 completion evidence

| Evidence | Path / value | Local verification |
| --- | --- | --- |
| Installed Task | `MCP/TASKS/RUN06_TUNE_GENERATED_RUN_SHAPES_AND_REMOVE_BAD_VARIANTS.md` | SHA-256 `42f3a6b575c52c8463951f144da31f4b8dfdb52fdf093d1e894dac47810c467b` |
| Result | `MCP/REPORTS/RUN06_TUNE_GENERATED_RUN_SHAPES_AND_REMOVE_BAD_VARIANTS_RESULT.md` | `TASK` matches RUN06; independent `STATUS: PASS`; SHA-256 `4a1dfe19d47b624f06e1a6e98ca448da916a3e6d3d098f5ade68eb3b1a1ad6f7` |
| Committed version | `f4da626c5cc509954a41e67b24e5d7113d01a50b` | `git log -- <Task> <Result>` identifies the current RUN06 commit. |
| Result self-report | `a409c7343c74d4220dc76e7476982296e8ef7cb4` | This is the direct parent of `f4da…`; it is a historical self-report, not the current committed version. |
| Worktree comparison | no diff for either RUN06 file | `text=auto` is an attribute; no content or EOL-only change was observed. |

`06_IMPLEMENTATION_STATUS.md` names RUN06 as COMPLETE in its package and
history summaries. Its old `Last Completed Task` block still names RUN01. That
legacy block is recorded as stale evidence only; RMAP01 does not rewrite it.

## Preserved prior plan history

The older MAP/VIS/RUN completion records stay preserved. The 29 historical
`RMAP00_01`~`RMAP04_07` names are a requirement crosswalk only; they are not
opened, renamed, or treated as the v4.2 execution queue. The full crosswalk is
in [01_SEQUENCE_V4_2.md](01_SEQUENCE_V4_2.md).

## Scope exclusions

No C#, Scene, Prefab, Authoring data, ProjectSettings, Packages, Unity refresh,
compile, build, or regression test is part of this task. Existing visual
evidence is inspected as historical evidence only.
