# SV5 jump Player verification v5

SV5_20 binds the accepted SV5_17 recipe geometry, SV5_18 swept clearance,
and SV5_19 recovery routes to the actual live Player physics stack. The proof
uses two in-memory 24x32 fixtures only; it does not build or search the world.

## Physical binding

- Player prefab: `Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab`
- Movement: the FIX04-approved `CharacterLiveMovementDriver` bytes
- Body: actual `Rigidbody2D` and 0.72 x 0.9 `CapsuleCollider2D`
- Fixed timestep: 0.02 seconds
- Jump velocity: 7.2
- Run/walk speed: 5.5 / 2.8
- Grab probe/window: 0.35 / 0.45

No Player setting, prefab, capsule, Physics2D setting, or movement constant was
retuned. Post-START position and velocity corrections are zero.

## Accepted physical proof

The canonical PlayMode suite proves 18 main links, two continuous forward
routes, and 18 recovery cases. All 38 cases reach their bound terminal. Grab
entry and fresh-Space exit use the actual collider, TOP_ONLY surfaces remain
non-grabbable, one-way landings remain physical, and jump plus Grab rises by at
most two cells. Completion is itemless and one-way; reverse completion is not
required.

JS_LINK_02 retains its AIR passage, JS_LINK_03 uses the SOLID side-Grab and
natural upper landing, and JS_LINK_04 takes off from TOP_ONLY support. R0 and
MIRROR_X occupancy are exact mirrors.

## Harness isolation

FIX05 loads RMAP02 and RMAP03 saved scenes additively, inspects only the loaded
scene, restores the previous active scene, unloads in `finally`, and destroys
test-owned physical roots followed by one frame and
`Physics2D.SyncTransforms()`. The accepted direct gate is exactly 5 RMAP02,
10 RMAP03, and 6 RMAP04 tests in one Unity execution.

## Readiness

- `JumpRecipeReady=true`
- `ComposedGeometryReady=true`
- `SweptClearanceReady=true`
- `RecoveryReady=true`
- `PlayerVerified=true`

World placement remains outside this Task. SV5_21 stays locked.
