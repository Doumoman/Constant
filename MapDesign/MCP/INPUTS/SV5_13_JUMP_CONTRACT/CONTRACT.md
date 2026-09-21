# SV5_13 — jump-map data and distance contract

## Purpose

Define the exact 1x1-cell records and measurement rules that SV5_14 through
SV5_20 must consume. This Task produces typed data, canonical local fixtures,
exports, tests, and an independent audit. It does not generate the final jump
room and must not claim a live Player run.

## C01 — support records

Each support records a stable ID, `SOLID` or `ONE_WAY`, integer origin and size,
top support height, inclusive horizontal top interval, active-route status,
decorative-only status, and validation state.

- `SOLID` has top, side, and underside collision. A grab edge may exist only on
  an exposed safe left or right face of an active `SOLID`.
- `ONE_WAY` supplies top-only landing support. Its side and underside never
  create a grab edge.
- Decorative or unreachable solid cells never satisfy active-route SOLID use.
- A later task may add geometry, but it must consume this contract rather than
  redefine material names or distance formulas.

## C02 — exact distance semantics

`gap_air` is the count of empty integer tile columns strictly between the two
facing support boundaries. It is not center distance, pivot distance, path
length, Euclidean distance, or the number of supporting cells.

For left-to-right movement:

`gap_air = target_left_x - source_right_x - 1`

For right-to-left movement:

`gap_air = source_left_x - target_right_x - 1`

`takeoff` and `landing` are actual foot support positions or inclusive spans.
`rise = target_top_y - source_top_y`. Origin Y, center Y, and collider pivot do
not replace the two support heights. Horizontal overlap produces gap 0, not a
negative gap.

## C03 — movement and grab records

A link records source, target, direction, takeoff, landing, `gap_air`, `rise`,
mode, optional grab edge, and validation state.

- Normal upward movement is capped at +1 cell.
- `JUMP_GRAB` is capped at +2 cells and must reference a real target `SOLID`
  grab edge.
- A grab edge records contact cell and face, approach direction, hanging-body
  cell, pull-up destination, and safety. Contact, hang, and pull-up positions
  are different facts and must all be present.
- `ONE_WAY`, background, decorative, blocked, or underside faces cannot be
  counted as grabbable.
- One-way traversal is allowed. Reverse completion is not silently required.

## C04 — validation states

The only states are `PLANNED`, `STATIC_SCREEN`, and `PLAYER_VERIFIED`.

- `PLANNED`: typed record and arithmetic only.
- `STATIC_SCREEN`: final-cell collision/support/clearance evidence exists, but
  no live Player success is claimed.
- `PLAYER_VERIFIED`: requires a concrete Player run ID and PASS result from the
  later SV5_20 task.

SV5_13 must export zero `PLAYER_VERIFIED` objects and keep
`PlayerVerified=false`. AIR flood, a screenshot, or a production boolean cannot
promote an object to `PLAYER_VERIFIED`.

## C05 — canonical local fixtures

Export deterministic local examples with literal IDs:

- J1: `gap_air=3`, `rise=1`, `JUMP`.
- J2: `gap_air=4`, `rise=1`, `JUMP`; this remains unguaranteed until live Player
  validation and must not be labeled `PLAYER_VERIFIED`.
- J3: `gap_air=2`, `rise=2`, `JUMP_GRAB`, landing/grab target `SOLID`.

Across the active examples, use at least one real `SOLID`, one `ONE_WAY`, and
one real `JUMP_GRAB`. J1/J2/J3 are measurement fixtures, not proof that every
random 3- or 4-cell jump is completable.

## C06 — exact exports

Create `MapDesign/MCP/GENERATED/SV5_13_JUMP_CONTRACT/` containing:

- `jump_contract.json`
- `supports.csv`
- `grab_edges.csv`
- `links.csv`
- `measurement_proofs.csv`
- `validation_states.csv`
- `contract_validation.json`
- `preview/jump_contract.svg`
- `independent_jump_contract_audit.json`
- targeted and focused NUnit XML
- final `BINDING.json`

CSV headers are fixed by the checker. JSON records include readiness flags,
canonical IDs, zero Player-verified count, and zero whole-world builds in new
targeted tests. The SVG must visibly distinguish SOLID, ONE_WAY, grab contact,
takeoff/landing, gap_air, rise, and validation state.

## C07 — tests and time control

Add at least 14 meaningful tests covering both directions, exact boundary gap,
top-surface rise, overlap gap 0, invalid center-distance substitution, invalid
origin-Y rise, normal rise above one, grab rise above two, ONE_WAY grab rejection,
missing hang clearance, missing pull-up space, decorative support rejection,
state promotion rejection, J1/J2/J3 round-trip, and deterministic export.

New tests use tiny local fixtures and must not build/search the 624x416 world.
Run compile and targeted tests first, then the independent checker. Only after
those pass, run the full SV5 regression exactly once. Final focused count is at
least 225 with zero failed/skipped/inconclusive. Do not loop the full suite.

## C08 — readiness and exclusions

On PASS set `JumpContractReady=true`. Keep `JumpSolidGeometryReady=false`,
`JumpGrabGeometryReady=false`, `ComposedGeometryReady=false`, and
`PlayerVerified=false`. Do not modify Player velocity, jump force, collider,
camera, predecessor tree/hub geometry, protected/core/progression data, or world
placement. Do not introduce Sector/48x32 grouping, all-endpoint-pair comparison,
or per-candidate world copy/BFS. Keep SV5_14 locked and do not push.

