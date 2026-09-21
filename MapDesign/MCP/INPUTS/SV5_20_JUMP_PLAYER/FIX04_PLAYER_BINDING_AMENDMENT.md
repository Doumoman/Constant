# SV5_20 FIX04 — exact Player binding amendment

This amendment resolves the conflict between the original base-only Player
binding and the actual-capsule Grab correction proven while SV5_20 was
CURRENT. It does not remove Player binding checks and does not permit further
Player tuning.

The original `player_bindings.json` remains the immutable base-commit
provenance snapshot. The final active binding is the combination of that
snapshot and the byte-identical generated copy of
`FIX04_PLAYER_BINDING_AMENDMENT.json`.

Exactly one live Player file is amended:

- `Assets/_Game/Live/Runtime/Movement/CharacterLiveMovementDriver.cs`
- base SHA-256: `df58bb6edca4179cefb3d149baf032f03724f2aac4f5afafb8cc10a66b40a836`
- approved SHA-256: `77ac90e6c65f4dc83a104deb1db6c02177b1c50c2797426c6e3b07aeedc0d8f8`
- approved bytes: `39703`

The approved delta is limited to actual-capsule Grab probing and separation,
moving-collider anchor ownership, and released-collider upward escape. Player
movement constants, settings, prefab, capsule, input, and Physics2D settings
remain unchanged and base-bound.

The existing RMAP03 PlayMode test file may retain the actual-capsule fixture,
isolation, and natural recontact verification authored during this correction.
It is test-only and must be included in the final Task-owned commit. It does
not authorize product tuning or a fabricated post-START movement correction.

The approved evidence is fixed in the JSON amendment. Previously accepted
targeted EditMode, 38-case PlayMode, and RMAP03 results are not rerun merely to
install this amendment. The independent checker validates the exact approved
Driver bytes and the byte-identical generated amendment before proceeding.
