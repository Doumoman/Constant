# SV5_20_JUMP_PLAYER package

This package stages the registered SV5_20 Task after accepted SV5_19.

The Task verifies the two 24×32 jump recipes with the approved live Player,
real Rigidbody2D/colliders, forward-only main traversal, and representative
miss recovery. It does not retune Player movement or inspect the whole world.

FIX04 preserves the original base Player snapshot and exact-locks one approved
`CharacterLiveMovementDriver.cs` correction proven by targeted EditMode,
38-case PlayMode, and RMAP03. Read `FIX04_PLAYER_BINDING_AMENDMENT.md` before
continuing. Do not rerun already accepted targeted suites merely to install the
amendment.

FIX05 additionally binds test-only SavedScene and physical-fixture cleanup for
the combined RMAP02–04 direct regression. Read
`FIX05_RMAP_SCENE_ISOLATION_AMENDMENT.md`. It does not authorize any product or
Scene-asset change.

Run `STAGE.py --check`, `--stage`, and `--verify-staged`, then use the native
repository Apply flow. Do not make ad-hoc CHECK/FILES/VERIFY copies.
