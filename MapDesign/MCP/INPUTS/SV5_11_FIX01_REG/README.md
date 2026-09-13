# SV5_11_FIX01 registration and execution package

This package registers and runs one corrective Task before `SV5_12_TREE_GRAB`.
It fixes the accepted SV5_11 shell's synthetic external connection lines.

The package has four explicit phases:

1. `REGISTER.py --check` performs a read-only package, Git-blob, state, collision,
   and inbox preflight.
2. `REGISTER.py --apply-registration --approval SV5_11_FIX01_REG` inserts only the
   one LOCKED Status row and one Master row, then stages exactly one bound Task MD
   in `MapDesign/MCP_INBOX`.
3. The repository's native `APPLY_PATCH_AND_RUN_CURRENT_TASK.md` process installs,
   applies, implements, tests, finalizes, and commits `SV5_11_FIX01`.
4. `REGISTER.py --verify-task-inputs` and `--post-readonly` verify the registration
   bridge without weakening the native Task protocol.

Do not start `SV5_12`. Do not edit the predecessor Result, Task, Archive, Binding,
or generated evidence. Do not clean, restore, reset, checkout, or include unrelated
dirty files. Package PASS is not implementation PASS.

The technical acceptance contract is `CONTRACT.md`; exact predecessor Git objects
are in `SOURCE_LOCK.json`; machine limits are in `HUB_CONNECTION_PROFILE.json`.
The independent export checker is `tools/check_hub_connection_fix.py`.

