# SV5_18_ENDPOINT_FIX01 — effective takeoff correction

STATUS: APPROVED CORRECTION TO CURRENT SV5_18

This correction supersedes only the conflicting endpoint clauses of CONTRACT
C01/C02 and the diagonal rule for the exact Grab pull-up transition. Every
other SV5_18 requirement remains unchanged.

## Cause

SV5_17 records four takeoff points on the outer edge of their source supports,
but later decorative SOLID outline cells occupy the recorded body/head cells.
The source recipe remains valid design lineage, but those four markers cannot
be used as standing cells for clearance validation.

## Corrected interpretation

- Keep SV5_17 source, evidence, supports, occupancy, links, counts, and digests
  byte-identical.
- Do not delete or carve the colliding outline cells.
- In the new SV5_18 clearance layer, preserve both the source takeoff and a
  distinct effective takeoff.
- Move only these four effective takeoffs one cell inward on the same source
  support:

| Recipe | Link | Source takeoff | Effective takeoff |
|---|---|---:|---:|
| `JUMP012_MIXED_R0` | `JS_LINK_02` | `(14,4)` | `(13,4)` |
| `JUMP012_MIXED_MX` | `JS_LINK_02` | `(9,4)` | `(10,4)` |
| `JUMP012_MIXED_R0` | `JS_LINK_06` | `(4,8)` | `(5,8)` |
| `JUMP012_MIXED_MX` | `JS_LINK_06` | `(19,8)` | `(18,8)` |

- All other takeoff and landing coordinates remain exact.
- This is clearance metadata owned by SV5_18, not a mutation of SV5_17.
- The canonical effective-link endpoint digest is
  `9ca58b3070662afece14b812aff892ec8e31bac09938a7445a667f3b2aa8db78`.
- Export `source_takeoff_x/y`, effective `takeoff_x/y`, `endpoint_adjusted`, and
  `correction_id` in `clearance_links.csv`.
- Exactly four rows use `endpoint_adjusted=true` and
  `correction_id=SV5_18_ENDPOINT_FIX01`; every other row uses false and empty.

## Grab pull-up corner exception

The conservative diagonal supercover rule still applies to every ordinary
trace step. Its only exception is the exact `HANG -> PULL_UP` step of
`JS_LINK_03`: one orthogonal corner is deliberately the already validated
SOLID Grab contact. That one contact cell may be occupied; the other corner and
all body/head cells must remain clear. No other occupied corner is allowed.

## Authority and lifecycle

This correction does not register or start a FIX Task. Continue the already
CURRENT `SV5_18_JUMP_CLEARANCE`. The existing Task and Archive bytes remain
unchanged. The write allowlist remains the original SV5_18 new files only;
predecessor source/evidence stays immutable. Record this correction ID, four
source/effective pairs, and the Grab exception in BINDING, validation, Result,
and Review ZIP. SV5_19 remains LOCKED and push remains prohibited.

