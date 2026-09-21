# SV5 route-state analysis entrypoint

SV5_05 is a deterministic logical-analysis adapter. It connects the exact
SV5_04 core/route plan to the existing SV5 graph state machine. It does not
write terrain, create a route, change Player state, run a Bake, or claim a
physical completion result.

## Bound API and source ownership

- `Sv5RouteStatePolicy.Analyze(Sv5CoreReservationPlan, candidates, review)`
  owns SV5_05's read-only analysis and exports.
- Its input retains one `Sv5CoreReservationPlan`, its exact SV5/SV5
  references, and `RouteSource.Graph`; all 11 `RouteId` values are matched to
  actual SV5 edges and actual source/target access ports.
- The SV5 `Sv5WorldGraphPlanner.Evaluate` remains action/state authority.
  SV5_05 invokes it separately for six resource orders. Its actions retain
  `ACQUIRE`, `FORGE|MAKE_SEAL`, `SEAL|OPEN`, and
  `BOSS|PLANNED_COMPLETION_EVENT` semantics.
- `EvaluateWithAnalysisNodes` is a pure, backward-compatible SV5 analysis
  entrypoint. Additional IDs are action-less connections only; they do not
  acquire resources or grant Forge, Seal, Boss, or Exit state. Normal `Plan`
  and `Evaluate` behavior remains unchanged and is covered by focused SV5
  tests.
- `GeneratedCompletionSearch` was read as a static logical-evidence boundary;
  it was neither changed nor run. It is not Player/geometry proof.

## Candidate and contact rules

- Shortcut candidates identify stable source/target anchors, direction,
  required resource mask, Forge/Seal/Boss requirements, and provenance.
- Existing graph anchors must match SV5. Declared general anchors exist only
  in the analysis graph. Unknown anchors, duplicate IDs, self-links, weak
  target-port conditions, and undeclared reverse one-way links are rejected.
- Candidate sets are checked together through SV5, not accepted merely from
  individual candidate outcomes. A logical pass preserves all six canonical
  orders and required actions.
- Route labels are exported separately from edge predicates. A label with
  `GATED` is never parsed as a predicate.
- Shared cells and cardinal passage/clearance face contacts are recorded. The
  three review coordinates and the 109-edge raw AIR witness are retained as
  diagnostics. The witness is not a Player trajectory or completion exploit.

## Current evidence and readiness

- Current representative input has 11 routes, 8 core sites, 2,432 protected
  cells, 45 physical port cells, and six SealBoss state rows.
- The six logical resource orders pass through normal returns and the
  Forge→Seal→Boss→Exit action chain. This is `LOGICAL_STATE_VERIFIED` only.
- `GEOMETRY_STATE_READY=false` and `PLAYER_VERIFIED=false`. Static AIR,
  route-reservation headroom, or an SV5 predicate does not promote either.

## Consumer obligations

- SV5_06 owns Village ENTRY/EXIT linkage, Start EXIT use/non-use, and final
  generated geometry for condition-crossing contacts.
- SV5_09 must recheck route-state boundaries when loop candidates are added.
- SV5_41 must recheck final geometry and Player traversal before any readiness
  promotion. Each obligation is exported with `PENDING` status.

Generated evidence lives in `MCP/GENERATED/SV5_05/`. Consumers rebuild from
their current sources; they do not replace SV5/SV5 or prior SV5 exports.
