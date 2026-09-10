# SV5 core reservation entrypoint

SV5_04 is a deterministic, read-only adapter over one existing
`RmapSpecialReservationPlan` and one existing `Rmap16ClusterAssemblyPlan`.
It does not place sites, consume a new RNG stream, regenerate terrain, or
advance graph/progression state.

## API and ownership

- Source adapter: `Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/
  Sv5CoreReservationPlan.cs`.
- Build only from `Sv5CoreReservationPlanner.Plan(Rmap16ClusterAssemblyPlan)`.
  The overload that receives a core plan requires that exact same RMAP15 plan
  object and its definition/biome provenance; mixed source plans are rejected.
- Future terrain consumers call `EvaluateTerrainCandidates` before writing.
  `Sv5CoreTerrainCandidate` contains the consumer ID, tile coordinate, and
  requested base cell. The decision is diagnostic-only and does not mutate a
  source plan or a terrain cell.
- The existing `RmapSpecialReservationPlan.EvaluateTerrainCells` remains the
  core S/A/O protection authority. SV5_04 additionally protects actual RMAP16
  route passage/headroom/support and RMAP15 SealBoss state geometry.

## Current representative evidence

- The checked plan retains 8 physical sites, 2,432 source core cells (483 S,
  1,919 A, 30 O), 10 slots, 45 physical port cells, and 6 SealBoss state rows.
- The current graph has 11 RMAP16 static routes. They provide 3,019 passage,
  3,019 clearance, and 1,037 existing support reservation rows. They are
  static suitability evidence, not Player traversal or a full-world Bake.
- Twelve physical access groups are current graph route endpoints. Start's
  unused EXIT port and Village's two no-graph-reservation ports remain present
  as `PRESERVED_*`; they are not reported as completed routes.

## Consumer handoff

SV5_05 must read the plan digest, route conditions, and `access_bindings.csv`
without changing physical port ownership. SV5_06 and SV5_41 must call the
candidate gate before any terrain write, honor a rejected coordinate's owner
and reason, and preserve the source route/state conditions. They must rebuild
against their current inputs rather than reuse this representative output as a
new world definition.

Detailed same-plan exports are in `MCP/GENERATED/SV5_04/`: core sites/cells,
access bindings, route semantic cells, state geometry, manifest, and focused
EditMode XML. No SV5_05+, Player, Scene Bake, or runtime progression work was
performed by this entrypoint.
