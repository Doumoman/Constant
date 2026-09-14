# SV5 Jump Outline v5

`SV5_16_JUMP_OUTLINE` adds only irregular downward backing to the accepted
SV5_15 local jump-room fixture. The 24×32 coordinates are local integer cells,
not a Sector, world placement, or a 624×416 search surface.

## Immutable base

The ten supports, 27 support cells, ordered nine links, and the one
`JS04_RIGHT_GRAB` record are inherited from
`Sv5JumpGrabGeometry.CreateCanonicalLocalFixture()`. Their coordinates and
semantics do not move. In particular, the accepted Grab remains:

- link `JS_LINK_03`, `JS03_ONE_WAY -> JS04_SOLID`, right-to-left;
- takeoff `(18,5)`, landing `(15,7)`, `gap_air=2`, `rise=2`;
- SOLID contact `(15,6)` on the exposed right face;
- hang body/head `(16,6)/(16,7)`;
- pull-up foot/head `(15,7)/(15,8)`.

No outline face receives Grab capability. ONE_WAY remains top-only and never
receives backing.

## Exact downward deformation

Depth is measured downward from each SOLID support's bottom row. Depth zero
emits no cell; depth one emits `support_y-1`; depth two additionally emits
`support_y-2`.

| SOLID owner | Per-column depth | Emitted cells |
|---|---|---:|
| `JS00_ENTRY_SOLID` | `x0=1, x1=1, x2=0` | 2 |
| `JS02_SOLID` | `x12=1, x13=2, x14=1` | 4 |
| `JS04_SOLID` | `x14=2, x15=1` | 3 |
| `JS06_SOLID` | `x4=2, x5=2, x6=1` | 5 |
| `JS08_SOLID` | `x3=2, x4=1` | 3 |

This produces exactly 17 unique full-collision SOLID outline cells. The final
occupancy is the disjoint union `27 base + 17 outline = 44` cells. It contains
no filled 6×6 SOLID window, continuous room floor, ceiling, wall, or rectangular
shell.

## Static validation boundary

All ten supported foot positions and their body/head AIR cells are recomputed
against the final 44-cell occupancy. The four accepted Grab AIR cells are also
checked against that same occupancy. Supports remain `STATIC_SCREEN`, links
remain `PLANNED`, and `PLAYER_VERIFIED` is deferred.

SV5_16 establishes outline geometry only. Composition, swept jump clearance,
Player tuning, world assembly, global endpoint comparison, and reverse-route
completion are outside this task.
