# SV5_14 Mixed-Support Jump Geometry

## Scope

`Sv5JumpSolidGeometry.CreateCanonicalLocalFixture()` defines one deterministic
24×32 jump-room coordinate system. The dimensions describe this room only; the
fixture is not a Sector and has no production-world placement. The illustrative
origin `(360,224)` is retained only as non-authoritative review metadata.

SV5_14 creates support geometry and a one-direction ascent. It does not create a
Grab edge or `JUMP_GRAB` link, deform the room outline, compose the 624×416
world, compare global endpoints, or claim a live Player traversal.

## Canonical support geometry

The fixture contains ten required supports, five `SOLID` and five `ONE_WAY`.
Each typed rectangle emits every occupied 1×1 cell. `SOLID` cells use full solid
collision and `ONE_WAY` cells use top-only collision. Widths are 2–3 cells and
all heights are one cell, within the profile's independent kind limits.

The ordered route is:

```text
JS00_ENTRY_SOLID
→ JS01_ONE_WAY
→ JS02_SOLID
→ JS03_ONE_WAY
→ JS04_SOLID
→ JS05_ONE_WAY
→ JS06_SOLID
→ JS07_ONE_WAY
→ JS08_SOLID
→ JS09_EXIT_ONE_WAY
```

Every required SOLID is the source or target of a route link. There are no
decorative solids credited toward the mixed-support requirement. The route
starts at an explicit lower entry socket at top height 2 and ends at an
explicit upper exit socket at top height 11, for a nine-cell vertical span.

## Link and clearance rules

All nine links call `Sv5JumpContract.Measure` and remain `JUMP`. Facing-edge
`gap_air` values are 1–3 and every top-face `rise` is +1. Takeoff and landing
points lie on the inclusive top interval of their source and target supports.
The ascent is intentionally one-way; no reverse completion is fabricated.

Each required support has one supported foot coordinate and two reserved AIR
cells represented by the body coordinate at the top boundary and the head
coordinate one row above it. Reservations are checked against the complete
final local occupancy and the 24×32 bounds. Every overlapping 6×6 window is
also scanned for forbidden all-SOLID occupancy.

## Validation state and readiness

Support cells are `STATIC_SCREEN` geometry evidence. Route links remain
`PLANNED` because this task does not run the Player. Passing SV5_14 establishes:

```text
JumpContractReady=true
JumpSolidGeometryReady=true
JumpGrabGeometryReady=false
ComposedGeometryReady=false
PlayerVerified=false
```

The canonical object graph is the sole source for JSON, CSV, validation, and
SVG exports. The targeted suite uses this small local fixture and performs zero
complete-world builds and zero complete-world searches. SV5_15 owns the first
real Grab segment and remains locked after this task.
