# SV5_13_JUMP_CONTRACT package

This package stages the already registered `SV5_13_JUMP_CONTRACT` Task after
the reviewed `SV5_12_FIX01` commit.

SV5_13 defines data and measurement semantics only. It does not build the jump
room, alter Player abilities, or start SV5_14. The canonical J1/J2/J3 fixtures
are small local records; do not instantiate or search the 624x416 world in each
new unit test.

Run `STAGE.py --check`, then `STAGE.py --stage`, then use the repository's
native `APPLY_PATCH_AND_RUN_CURRENT_TASK.md` flow. Read every file in this
folder before implementation. The helper does not implement, test, finalize,
commit, push, or start another Task.

