# SV5_15 Actual SOLID-Face Jump Grab

## Scope

`Sv5JumpGrabGeometry.CreateCanonicalLocalFixture()` derives one deterministic
24×32 local jump room from the accepted SV5_14 fixture. The room is not a
Sector and is never placed in or searched across the 624×416 world. Player
movement parameters, input, physics, ladders, generic wall climbing, and world
composition remain outside this task.

## Exact derived geometry

The ten-support ordered route and all support types are preserved. The sole
geometry change moves `JS04_SOLID` from origin `(14,5)` to `(14,6)` while
retaining its `2×1` size. Consequently `(14,5)` and `(15,5)` are AIR, while
`(14,6)` and `(15,6)` are full-collision SOLID cells. The other 25 occupied
cells are unchanged, for 27 actual emitted support cells in total.

## Required Grab transition

`JS_LINK_03` is the only `JUMP_GRAB` link:

```text
source      JS03_ONE_WAY, top x=18..20, y=5
target      JS04_SOLID, top x=14..15, y=7
direction   RIGHT_TO_LEFT
takeoff     (18,5)
landing     (15,7)
gap_air     2
rise        2
grab face   JS04 right exposed face
contact     (15,6) SOLID
hang body   (16,6) AIR
hang head   (16,7) AIR
pull-up foot / landing (15,7) AIR
pull-up head            (15,8) AIR
```

All nine links are rebuilt through the immutable
`Sv5JumpContract.Measure` authority. `JS_LINK_04` becomes a normal `JUMP` with
rise `0`; the other seven normal links retain rise `1`. Reverse completion is
not required.

## Cell-backed safety and state

The canonical plan reconstructs occupied cells from typed support rectangles
and independently checks the contact owner, exposed right face, hang body/head,
pull-up foot/head, and support below the landing. The exported action witness is
ordered `TAKEOFF → CONTACT_HANG → PULL_UP → LAND`.

ONE_WAY targets, hidden faces, wrong direction/face/contact, occupied hang or
pull-up cells, missing edges, rise above two, and premature
`PLAYER_VERIFIED` claims are rejected. A Grab edge is not inferred from a
support kind: only the single explicit `JS04_RIGHT_GRAB` edge exists.

Passing this static screen establishes:

```text
JumpContractReady=true
JumpSolidGeometryReady=true
JumpGrabGeometryReady=true
ComposedGeometryReady=false
PlayerVerified=false
```

The local proof is not a swept collider or live Player run; those remain owned
by SV5_18 and SV5_20.
