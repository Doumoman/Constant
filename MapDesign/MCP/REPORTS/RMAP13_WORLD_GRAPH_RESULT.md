# RMAP13_WORLD_GRAPH Result

TASK: RMAP13_WORLD_GRAPH

STATUS: PASS

## Preconditions and Apply evidence

- RMAP12 close Result SHA verified before Apply: `920e2b598e1effdabeb50711ef31852e4551c78700864afdc26e0ee170a5aaf9`.
- RMAP12 installed Task SHA verified before Apply: `f6b649abc0e452fb897e7e4175adee3ab94e4cfe051cb7234be8147ee9c184fb`.
- RMAP13 inbox, installed Task and archive are byte-identical. Their shared SHA-256 is `00b926dcfcb3fc5ed4e76a5f541fe68064346a2036fe473aeaa7308063685f12`.
- Normal single-task Apply changed only RMAP13 from `LOCKED` to `CURRENT` and Current Task from `NONE` to `RMAP13_WORLD_GRAPH`; RMAP12 stayed `COMPLETE` and RMAP14 stayed `LOCKED`.

## Implementation, responsibilities, and reuse

| Path | Responsibility and reuse judgement |
|---|---|
| `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/RmapWorldGraphPlanner.cs` | NEW cohesive RMAP13 logical graph planner. It generates planned directional nodes/edges, state-aware BFS proofs, return-policy requirements, stable anchor-linked reservation IDs, and pure RFC4180 export strings. It owns neither RNG, world definition, save state, coordinates, terrain baking, Player, Camera, combat, nor UI. |
| `Assets/_Game/Map/Runtime/WorldGeneration/WorldData/RmapWorldDataContract.cs` | REUSED unchanged. RMAP13 consumes a verified immutable definition, its RunGraph binding, and eight existing RMAP10 MicroChunk stable-ID anchors. No RMAP13 RNG or WorldDefinition duplicate was added. |
| `Assets/_Game/Map/Runtime/WorldGeneration/Validation/GeneratedCompletionSearch.cs` | REUSED unchanged at the state-semantics boundary: `RmapWorldGraphState` composes the existing `GeneratedCompletionStateKey` for position/resource/forge/seal/boss state. The final MAP19 tile-graph completion API was deliberately not repurposed because its validated downstream handoffs require a later physical tile graph. |
| `Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionFixedSlotLayers.cs` and `SpecialRegionSiteBridge.cs` | READ/KEPT. RMAP13 records only location-less planned reservation requirements, so it does not seize later special-region/slot ownership or invent fixed world coordinates. |
| `Assets/_Game/Tests/EditMode/Map/RMAP13/RmapWorldGraphPlannerTests.cs` | NEW focused tests for six ordered proofs, return-path/one-way failure diagnosis, both shortcut policies, deterministic RMAP12 anchor binding, and exported ID consistency. |
| `MapDesign/MCP/GENERATED/RMAP13/` | NEW test-derived, stable sorted graph material and focused Unity evidence. |

## W01 / W02 / W07 evidence

- **W01:** Start, Mooncore Ore, Condensed Coefficient Sap, Deep Star Yeast, Forge, Seal, Boss and Exit each reference one actual RMAP12 MicroChunk stable-ID anchor plus a planned reservation. The search state contains position, resource mask, ordered-resource cursor, Forge-made, Seal-open and Boss-complete values. Resource actions occur only at their role node; Forge requires all three resources, Seal requires Forge, Boss completion requires Seal, and Exit requires Boss completion. `BOSS|PLANNED_COMPLETION_EVENT` explicitly marks a future combat event rather than claiming combat implementation.
- **W02:** every spatial edge is independently authored with source/target, L/R/U/D direction, condition, one-way source connection ID and `PlannedSpace` verification level. The planner never adds reverse edges. Its visited key includes the composed existing completion state plus the requested-order cursor, allowing a return to the same location after a resource transition. All six exact resource orders produce a Forge -> Seal -> Boss -> Exit proof; a missing return reservation and a reverse use of a one-way connection are rejected with diagnostic code and state/condition detail.
- **W07:** `Optional` is the provisional default and contains no shortcut edge. `Required` adds three explicitly released resource-return shortcut requirements, each `PLANNED`; neither policy changes the normal return route or relaxes the six-order/mandatory-chain checks. No world coordinate, terrain excavation frequency, or physical shortcut is asserted.

## Generated graph material

These exports are from the passing `Required`-policy representative (`seed=1304`, `CONTENT_V1`, `GENERATOR_V1`) and are API-generated strings decoded from the passing test output, not hand-authored catalog input. They are stable-sorted and RFC4180-escaped. All edges/reservations remain logical planned requirements, not Player/Tilemap validation.

| Material | SHA-256 |
|---|---|
| `rmap13_nodes.csv` | `08a47f772b89a224b41be46c684e49d40266d7ecb48c4ad604d472b8b676b392` |
| `rmap13_edges.csv` | `e40863a241769ed47c4db7532c77115d6d5ed9ec49b051c7f8caaf53068c474a` |
| `rmap13_reservations.csv` | `a6b87e8896b2e0ae651abdc295d7753dae6f052ed0b383ac3432df5911937ba8` |
| `rmap13_proofs.csv` | `b9383171629b424431f0a437288e32e9ad495d468f1c9225a25b4d61cab70c94` |

## Focused validation

- Unity `6000.3.8f1`, EditMode filter `RmapWorldGraphPlannerTests`: **4/4 PASS**, failed/skipped `0/0`; XML `rmap13_editmode_results.xml`, SHA-256 `2d96bf1c84497fbeafc40c51907833861e2ac53eddc9f3272939bae7354289d3`.
- Unity `6000.3.8f1`, direct existing EditMode filter `GeneratedCompletionSearchTests`: **10/10 PASS**, failed/skipped `0/0`; XML `rmap13_existing_completion_results.xml`, SHA-256 `eb274afca88b4be2582bab60e85a3de8a9d68b2fd756907f576f7c3ff02002c6`.
- No broad/unfiltered regression, build, Player/Camera/Input test, scene mutation, physical Tilemap proof, full-world bake, RMAP14 work, or push was run.

## Finalization boundary

PASS is limited to the RMAP13 logical progression graph and future space-reservation contract. RMAP14_BIOMES remains `LOCKED` / `NOT STARTED`; physical world placement and Player traversal remain later work. Finalize RMAP13 only, then commit only RMAP13-owned files.
