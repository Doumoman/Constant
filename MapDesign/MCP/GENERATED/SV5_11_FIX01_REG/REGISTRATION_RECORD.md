# SV5_11_FIX01_REG — one-time corrective Task registration

PATCH_ID: SV5_11_FIX01_REG
KIND: EXPLICIT_USER_APPROVED_ONE_TIME_REGISTRATION_CHANGE
TARGET_TASK: SV5_11_FIX01
BASE_COMMIT: 0ab603362b93047a45c6f30bac8e28d57ac57f93

This is not a gameplay Task and does not become CURRENT. It resolves only the
native protocol requirement that a normal `single_task_v1` Task already exist in
Status and Master before Apply.

Exact authorized state delta:

- Status: insert `| SV5_11_FIX01 | LOCKED |` immediately after the one
  `SV5_11_HUB_SHELL` row and before `SV5_12_TREE_GRAB`.
- Master: insert `| 11.F1 | SV5_11_FIX01 | LOCKED |` immediately after order 11
  and before order 12.
- Counts: 293 → 294 rows; 257 COMPLETE / 0 CURRENT / 36 LOCKED becomes
  257 COMPLETE / 0 CURRENT / 37 LOCKED. Current Task remains `NONE`.
- Create exactly one inbox candidate, `MCP_INBOX/SV5_11_FIX01.md`, byte-identical
  to the bundled `SV5_11_FIX01.md`.

Registration must not create the installed Task, Archive, Result, implementation,
tests, or PASS. Those remain owned by the normal native Apply/Execution/Finalize
flow. The registration inputs, record, receipt, Master delta, and final Status
delta are Task-owned and belong in the eventual single atomic FIX01 commit.

The helper refuses a different HEAD, overlapping dirty source, existing FIX01
registration/artifacts, or any pre-existing native inbox candidate. It does not
move or delete old candidates to force the candidate count.

