# SV5_18_JUMP_CLEARANCE package

This package stages the already registered `SV5_18_JUMP_CLEARANCE` Task after
the completed SV5_17 commit.

The work is deliberately local and bounded. It checks two 24×32 recipes and 18
known links using 1×1 cell occupancy. It does not search the 624×416 world, pair
global endpoints, create Sectors, tune the live Player, or add recovery routes.

Run `STAGE.py --check`, then `--stage`, then `--verify-staged`. After that use
the repository's native `APPLY_PATCH_AND_RUN_CURRENT_TASK.md` flow. The helper
only validates and stages one Inbox Task; it never implements, tests, finalizes,
commits, pushes, or starts SV5_19.

If the package directory already exists with these exact bytes, reuse it. Do not
create ad-hoc CHECK/FILES/VERIFY copies beside it.

