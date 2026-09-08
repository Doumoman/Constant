```yaml
mcp_patch:
  format: single_task_v1
  task_id: RMAP13_WORLD_GRAPH
  task_file: TASKS/RMAP13_WORLD_GRAPH.md
  requires_current_task: NONE
  requires_completed_task: RMAP12_WORLD_DATA
  requires_result:
    path: REPORTS/RMAP12_WORLD_DATA_RESULT.md
    status: PASS
    sha256: 920e2b598e1effdabeb50711ef31852e4551c78700864afdc26e0ee170a5aaf9
  requires_installed_task:
    path: TASKS/RMAP12_WORLD_DATA.md
    sha256: f6b649abc0e452fb897e7e4175adee3ab94e4cfe051cb7234be8147ee9c184fb
  sets_current_task: RMAP13_WORLD_GRAPH
```

# RMAP13_WORLD_GRAPH — state-aware directional resource progression

TASK: RMAP13_WORLD_GRAPH

EXPECTED_RESULT: MapDesign/MCP/REPORTS/RMAP13_WORLD_GRAPH_RESULT.md

NEXT: RMAP14_BIOMES LOCKED / DO NOT START

## Goal and boundary

Implement W01/W02/W07 so the three core resources — Mooncore Ore,
Condensed Coefficient Sap, and Deep Star Yeast — may be obtained in any of
their six orders before Forge -> Seal -> Boss -> Exit. This is a logical
world-graph and future-space-reservation contract, not boss combat, forge UI,
economy, biome placement, a 624x416 bake, or a physical Player playthrough.

Read and preserve the existing state-aware `GeneratedCompletionSearch` and
`GeneratedCompletionStateKey` first. Its current MAP19 tile-graph input has
strict downstream handoffs and must not be weakened or repurposed. Reuse its
position-plus-progression-state semantics where compatible; add only the
cohesive RMAP13 adapter/planner needed for this earlier graph stage.

RMAP13 consumes a verified `RmapWorldDefinition`, its RunGraph binding, actual
RMAP10 chunk stable-ID anchors, and RMAP12's immutable/mutation split. It must
not add RNG, WorldDefinition, save ownership, static mutable run state, live
Player/Camera/Input references, or a Scene object.

## Read/write allowlist and responsibility judgement

Read/adapt only as directly necessary:

- `Assets/_Game/Map/Runtime/WorldGeneration/WorldData/RmapWorldDataContract.cs` — KEEP RMAP12 request, RunGraph stream binding, immutable definition and anchor stable IDs.
- `Assets/_Game/Map/Runtime/WorldGeneration/Validation/GeneratedCompletionSearch.cs` — KEEP existing final tile-completion API and reuse state-search semantics without changing it.
- `Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionFixedSlotLayers.cs` and `SpecialRegionSiteBridge.cs` — READ existing slot concepts; do not claim a final world coordinate or mutate their later ownership.
- `Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/RmapPortCatalog.cs` and `MoonPalace/RunGeneration/RmapSmallRunHarness.cs` — READ traversal/port and actual chunk provenance only.

Write only direct RMAP13 ownership:

- `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/RmapWorldGraphPlanner.cs` (NEW cohesive graph, state search, role anchors and reservation requirements).
- `Assets/_Game/Tests/EditMode/Map/RMAP13/RmapWorldGraphPlannerTests.cs` (NEW focused fixtures).
- `MapDesign/MCP/GENERATED/RMAP13/` (NEW stable sorted graph/proof/reservation material).
- Task/archive/result/status records required by the protocol.

If an existing direct API cannot accept the early planning graph without
violating its handoff contract, record that reason and use a minimal adapter;
do not imitate completion success with an open-cell or undirected BFS.

## W01 progression state

1. Model Start, each named resource, Forge, Seal, Boss and Exit roles. Every
   role must reference an RMAP12 anchor stable ID and an explicit reservation
   requirement; an unset location stays `PLANNED`, never a fake fixed coordinate.
2. Search state must include position, three acquired-resource bits, forge-made,
   seal-open and boss-complete conditions. Preserve resource acquisition history
   for order proofs; do not use a global mutable player state.
3. A resource is acquired only at its resource node. Forge requires all three;
   Seal requires Forge; Boss requires Seal; Exit requires explicit boss-complete.
   Duplicate acquire/forge has defined no-op/rejection evidence.
4. Distinguish the planning assumption for Boss-complete from future combat's
   runtime event; do not report combat as implemented.

## W02 directional search and evidence

1. Spatial edges are directed L/R/U/D and carry source/target role IDs,
   traversal/condition tokens, source provenance and verification level. Never
   auto-add the reverse edge. State transitions are separate from movement.
2. Use a deterministic BFS/visited key containing position *and* progression
   state. Revisit after gaining a resource must remain explorable.
3. Generate/verify exactly six ordered resource proof paths. A path which
   acquires a different resource early is not proof for that requested order.
4. Reject and diagnose: missing resource at Forge, missing Forge at Seal,
   missing Seal at Boss, missing Boss completion at Exit, one-way direction
   error, and a required return-path break. Diagnostics include position,
   progression state, edge condition and unsatisfied reservation requirement.
5. The graph's planned edges remain `PLANNED_SPACE`; do not label them physical
   Player or tilemap proof. Existing physical port evidence retains its own level.

## W07 return/shortcut policy and reservations

1. Expose an explicit, versioned policy: `Optional` is the provisional default;
   `Required` adds only specified directional shortcut/reservation requirements.
   Neither policy permits a broken normal route to Forge and later progression.
2. Both policies must preserve all six resource orders and the mandatory chain.
   Do not impose global round-trip reachability for every optional branch.
3. Export stable sorted node, edge, reservation and six-proof material. CSV, if
   used, must RFC4180-escape multi-values and retain stable IDs. Include policy,
   digest, logical-vs-physical level and all planned/unset constraints.

## Focused PASS checks

- Actual RMAP12 definition/RunGraph binding produces a deterministic graph,
  stable role anchors, edges and reservation IDs.
- All six resource orders prove the full Forge -> Seal -> Boss -> Exit chain.
- Concrete missing-condition, reverse-one-way and stateful-revisit fixtures fail
  for the recorded reason.
- Optional and Required return policies both preserve the six orders; Required
  exposes its release condition and planned reservation requirement.
- Generated material round-trips or is checked against the API's stable output.
- Run only this focused RMAP13 test plus directly impacted existing
  `GeneratedCompletionSearch` test filter if required to prove compatibility;
  do not run broad/full regressions, a build, Player/Camera tests or RMAP14.

## Completion protocol

PASS only after actual implementation, focused Unity evidence, generated graph
material and a factual Result that distinguishes planned logical graph proof
from physical world validation. Then Finalize RMAP13 only, set Current Task to
NONE, keep RMAP14 LOCKED, and commit only RMAP13-owned files. Do not push.
