# SV5_17_JUMP_RECIPES package

This package stages SV5_17 after the reviewed SV5_16 commit. It turns the
accepted local jump-room object into two deterministic recipe variants (R0 and
MirrorX), each containing the same explicit mixture of +1 JUMP, +2 JUMP_GRAB,
and level JUMP transitions.

It also preserves the old #012 before/draft-after illustration as reference
data while explicitly refusing to treat that 141-cell draft as production or
Player-verified geometry. This is a local 24x32 recipe task, not a Sector or
624x416 world-placement task.

Run STAGE.py --check, --stage, and --verify-staged, then use the repository's
native single_task_v1 Apply flow. Read every package file before implementation.
