# SV5_13 Jump Support and Measurement Contract

## Scope

SV5_13 defines immutable local data consumed by SV5_14 through SV5_20. It does
not construct a jump room, place anything in the 624×416 world, or claim a live
Player traversal. `Sv5JumpContract.CreateCanonicalLocalFixture()` is the single
factory for the J1/J2/J3 examples and all JSON, CSV, validation, and SVG exports
are derived from that object graph.

## Support kinds

- `SOLID` occupies its top, sides, and underside. Only an active,
  non-decorative SOLID can own an exposed Grab face.
- `ONE_WAY` is top-only landing support. Its side and underside never produce a
  Grab edge. A route may use it in one direction without inventing a reverse
  completion requirement.
- Every support stores a stable ID, integer origin and size, `top_y`, the
  inclusive `top_x_min..top_x_max` interval, active/decorative flags, and a
  validation state.

## Exact measurement

For left-to-right movement, the facing cell boundaries are the source's
inclusive right cell and the target's inclusive left cell:

```text
gap_air = max(0, target_left_x - source_right_x - 1)
```

For right-to-left movement:

```text
gap_air = max(0, source_left_x - target_right_x - 1)
```

Thus `gap_air` counts empty integer columns strictly between the supports.
Horizontal overlap is zero. It is never replaced by center distance, pivot
distance, Euclidean distance, or the takeoff-to-landing span.

`takeoff` and `landing` are explicit foot coordinates on the inclusive top
intervals. Their vertical coordinate must equal the corresponding `top_y`.
Rise is always:

```text
rise = target_top_y - source_top_y
```

Origin Y and collider center Y are not substitutes. A normal `JUMP` rejects an
upward rise above one cell. `JUMP_GRAB` rejects an upward rise above two cells
and also rejects a missing or invalid target Grab edge.

## Grab edge and clearance

A Grab edge stores separate contact, hanging-body, and pull-up coordinates.
The contact must be the top occupied cell on the facing left/right boundary of
the target SOLID. The hanging-body cell must be immediately outside that face,
and the pull-up destination must lie on the target's top support interval.
Exposure, hanging-body clearance, and pull-up clearance are independent facts;
all three are required for `safe=true`.

ONE_WAY, inactive, decorative, background, blocked, unsafe, underside, and
directionally mismatched contacts are rejected with explicit reason codes.

## Validation states

- `PLANNED` means typed records and exact arithmetic only.
- `STATIC_SCREEN` means local collision/support/clearance evidence exists but a
  successful live Player run is not claimed.
- `PLAYER_VERIFIED` requires the later SV5_20 concrete run ID and PASS result.

SV5_13 rejects promotion to `PLAYER_VERIFIED`, exports a zero verified count,
and keeps `PlayerVerified=false`. A screenshot, AIR flood, or production boolean
cannot promote the state.

## Canonical local fixtures

| ID | Direction | Source | Target | gap_air | rise | Mode | State |
|---|---|---|---|---:|---:|---|---|
| J1 | left-to-right | SOLID | ONE_WAY | 3 | 1 | JUMP | PLANNED |
| J2 | right-to-left | ONE_WAY | SOLID | 4 | 1 | JUMP | PLANNED |
| J3 | left-to-right | ONE_WAY | SOLID | 2 | 2 | JUMP_GRAB | PLANNED |

J3 owns one real exposed left face on its target SOLID, plus distinct hang-body
and pull-up cells. These examples establish measurement records, not universal
Player reachability for every three- or four-cell jump.

## Readiness boundary

On a passing SV5_13 contract and independent audit:

```text
JumpContractReady=true
JumpSolidGeometryReady=false
JumpGrabGeometryReady=false
ComposedGeometryReady=false
PlayerVerified=false
```

All new tests use only the small local fixture. Complete-world builds and
complete-world searches in the targeted path remain zero. SV5_14 is still
LOCKED and owns the first generated SOLID jump-route geometry.
