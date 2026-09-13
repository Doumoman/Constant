# SV5_12_FIX01_REG — one-time corrective Task registration

PATCH_ID: SV5_12_FIX01_REG
KIND: EXPLICIT_USER_APPROVED_ONE_TIME_REGISTRATION_CHANGE
TARGET_TASK: SV5_12_FIX01
BASE_COMMIT: 3f05858cb80d6437e89123fb6b6dd30a395996fc

This is not a gameplay Task and does not become CURRENT. It resolves only the
native protocol requirement that a normal `single_task_v1` Task already exist
in Status and Master before Apply.

Exact authorized state delta:

- Status: insert `| SV5_12_FIX01 | LOCKED |` immediately after the one
  `SV5_12_TREE_GRAB` row and before `SV5_13_JUMP_CONTRACT`.
- Master: insert `| 12.F1 | SV5_12_FIX01 | LOCKED |` immediately after order 12
  and before order 13.
- Counts: 294 -> 295 rows; 259 COMPLETE / 0 CURRENT / 35 LOCKED becomes
  259 COMPLETE / 0 CURRENT / 36 LOCKED. Current Task remains `NONE`.
- Create exactly one inbox candidate, `MCP_INBOX/SV5_12_FIX01.md`,
  byte-identical to the bundled `SV5_12_FIX01.md`.

Registration must not create the installed Task, Archive, Result,
implementation, tests, generated PASS evidence, or Finalize commit. Those remain
owned by the normal native Apply/Execution/Finalize flow. The registration
inputs, record, receipt, Master delta, and final Status delta are Task-owned and
belong in the eventual single atomic FIX01 commit.

The helper refuses a different HEAD, overlapping dirty source, existing FIX01
registration/artifacts, or any pre-existing native inbox candidate. It does not
move or delete old candidates to force the candidate count.

