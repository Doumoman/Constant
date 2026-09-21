# SV5_15_JUMP_GRAB package

This package stages SV5_15 after the reviewed SV5_14 commit. It adds one real,
cell-backed jump/grab/pull-up segment to the 24x32 local jump room.

Grab is not a ladder and is not a generic ability attached to every platform.
Only an explicitly emitted exposed side face of a SOLID cell may be grabbed.
ONE_WAY supports remain top-only and can never provide a grab face.

Run STAGE.py --check, --stage, and --verify-staged, then use the repository's
native single_task_v1 Apply flow. Read all package files before implementation.

