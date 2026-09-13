# SV5_12_FIX01 registration and execution package

This package registers and runs one corrective Task before
`SV5_13_JUMP_CONTRACT`. It replaces the accepted but visually narrow 4x24
Tree Grab pillar with a wide, multi-generation canopy while preserving the
already-correct movement and collision semantics.

The package has four explicit phases:

1. `REGISTER.py --check` performs a read-only package, Git-blob, state,
   collision, and inbox preflight.
2. `REGISTER.py --apply-registration --approval SV5_12_FIX01_REG` inserts only
   one LOCKED Status row and one Master row, then stages exactly one bound Task
   MD in `MapDesign/MCP_INBOX`.
3. The repository's native `APPLY_PATCH_AND_RUN_CURRENT_TASK.md` process
   installs, applies, implements, tests, finalizes, and commits `SV5_12_FIX01`.
4. `REGISTER.py --verify-task-inputs` and `--post-readonly` verify the
   registration bridge without weakening the native Task protocol.

Do not start `SV5_13`. Do not edit the completed SV5_12 Result, Task, Archive,
Binding, inputs, or generated evidence. Do not clean, restore, reset, checkout,
or include unrelated dirty files. Package PASS is not implementation PASS.

The technical acceptance contract is `CONTRACT.md`; exact predecessor Git
objects are in `SOURCE_LOCK.json`; machine limits are in
`TREE_CANOPY_PROFILE.json`. The independent export checker is
`tools/check_tree_canopy_fix.py`.

