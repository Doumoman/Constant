# SV5_18 Jump Clearance v5

## Scope

SV5_18 consumes only the two immutable SV5_17 local recipes
`JUMP012_MIXED_R0` and `JUMP012_MIXED_MX`. Each recipe remains a 24×32 local
jump room with 10 supports, 44 occupied cells, 9 ordered links, 4 segments,
and one explicit Grab edge. No world placement or Player motor behavior is
introduced.

## Endpoint Fix01

`SV5_18_ENDPOINT_FIX01` preserves every SV5_17 source endpoint and adds an
SV5_18-owned effective takeoff for four links whose recorded outer-edge
standing cells are occupied by later outline geometry.

| Recipe | Link | Source takeoff | Effective takeoff |
|---|---|---:|---:|
| `JUMP012_MIXED_R0` | `JS_LINK_02` | `(14,4)` | `(13,4)` |
| `JUMP012_MIXED_MX` | `JS_LINK_02` | `(9,4)` | `(10,4)` |
| `JUMP012_MIXED_R0` | `JS_LINK_06` | `(4,8)` | `(5,8)` |
| `JUMP012_MIXED_MX` | `JS_LINK_06` | `(19,8)` | `(18,8)` |

The effective-link endpoint digest is
`9ca58b3070662afece14b812aff892ec8e31bac09938a7445a667f3b2aa8db78`.
All other takeoff and landing coordinates remain unchanged.

## Discrete clearance proof

Every one of the 18 links owns one deterministic trace. A trace state uses the
same local cell for foot and body, plus the cell immediately above for the
head. Both cells remain in bounds and clear of SOLID and TOP_ONLY occupancy.
Consecutive samples are unique, move at most one cell on each axis, never
reverse the link direction, contain at most 16 states, and remain within the
three-cell apex margin.

Diagonal steps apply conservative supercover to both orthogonal corner cells
and their head cells. The only exception is the exact `HANG -> PULL_UP`
transition of `JS_LINK_03`: its already validated occupied SOLID Grab contact
may be one corner. The other corner and every body/head cell remain clear.

The R0 `JS_LINK_03` proof is:

```text
TAKEOFF (18,5) -> AIR (17,6) -> HANG (16,6) -> PULL_UP (15,7)
SOLID contact: (15,6), RIGHT face
```

MIRROR_X is the exact sample-for-sample transform `x'=23-x`, including the
Grab face and sequence.

## Readiness boundary

The accepted local proof establishes:

- `JumpRecipeReady=true`
- `ComposedGeometryReady=true`
- `SweptClearanceReady=true`
- `RecoveryReady=false`
- `PlayerVerified=false`

This is discrete occupancy evidence. It does not claim live Player physics,
timing, animation, reverse completion, or recovery behavior.

## Evidence

Canonical data is exported under
`MapDesign/MCP/GENERATED/SV5_18_JUMP_CLEARANCE/`. It includes all 18 link
records, 90 trace samples, two explicit SOLID Grab proofs, validation JSON,
the independent audit, targeted and full NUnit reports, and the rendered SVG
inspection evidence.

