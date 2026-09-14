# SV5 jump recovery v5

SV5_19 adds a deterministic local recovery overlay to the accepted SV5_18
clearance proof. It operates only on the two 24×32 recipe variants and does
not represent a Sector, world placement, reverse completion, or live Player
physics.

## Representative miss rule

Each of the nine main links in each recipe owns exactly one probe. The origin
is the lower-middle sample among that link's ordered `AIR` states, using
`(count - 1) / 2`. The probe falls vertically and is caught by the first
occupied `SOLID` or `TOP_ONLY` cell below it. The landing body and head are the
two cells immediately above the catch and must remain clear.

## Recovery overlay

R0 contains three horizontal `TOP_ONLY` catch groups:

| Group | Cells | Width |
|---|---|---:|
| `RG_ENTRY_CATCH` | `(3..5,1)` | 3 |
| `RG_MID_CATCH` | `(9..11,2)` | 3 |
| `RG_GRAB_CATCH` | `(16..17,4)` | 2 |

MIRROR_X applies `x'=23-x` exactly. Each recipe therefore adds three groups
and eight uniquely owned cells. The overlay is disjoint from all SV5_17
occupancy, all SV5_18 main body/head trace cells, and both explicit Grab
contacts. It creates no 6×6 SOLID fill.

## Rejoin routes

The checkpoint order by failed link order is `0,1,2,3,2,1,6,0,6`. Orders
`4,5,7,8` land exactly on their selected checkpoint and export
`ALREADY_AT_CHECKPOINT` with zero movement links. The remaining five routes
use one itemless `WALK` or `DROP` link. Every trace uses adjacent local cells,
strict diagonal supercover, clear body/head cells, supported endpoints, and at
most 16 states. `JUMP` is permitted only up to one cell of rise, although the
canonical routes do not need it.

Every checkpoint is the same or earlier than the failed main link. Reverse
recovery and reverse main-route completion are not claimed.

## Readiness boundary

- `JumpRecipeReady=true`
- `ComposedGeometryReady=true`
- `SweptClearanceReady=true`
- `RecoveryReady=true`
- `PlayerVerified=false`

SV5_20 remains responsible for concrete Player verification.
