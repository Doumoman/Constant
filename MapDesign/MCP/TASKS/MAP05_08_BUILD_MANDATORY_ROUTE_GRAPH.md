# MAP05_08 — Build Mandatory Route Graph

```yaml
status_control:
  task_key: MAP05_08_BUILD_MANDATORY_ROUTE_GRAPH
  result_file: REPORTS/MAP05_08_BUILD_MANDATORY_ROUTE_GRAPH_RESULT.md
```

## TASK TYPE

```text
RUNTIME MANDATORY ROUTE GRAPH + TYPE1/2/3/4 MASK FAMILY STAMP + GENERATED EDGE CSV BYTES + EDITMODE TESTS
```

## Objective

`MAP05_07`의 `MandatoryRouteLoopPlan`까지 포함한 P03 mandatory route artifacts를 읽고 최종 `MandatoryRouteGraph`를 만든다. 이번 Task는 처음으로 mandatory route를 `SectorCell.RouteMaskId`, `mandatory_graph_node`, `generated_world_edges.csv` byte artifact까지 기록한다.

Type4 규칙은 계속 고정한다. Type4는 U+D가 반드시 열린 cell family이며 L/R은 source horizontal adjacency를 그대로 보존한다. 합법 조합은 `U+D`, `L+U+D`, `R+U+D`, `L+R+U+D` 네 가지다. loop/graph 생성을 위해 L/R을 강제로 열거나 닫지 않는다.

```text
input terminals/tree/backbone/gateways/conflicts/loops = 7 / 6 / 6 / 4 / 0 / 2
minimum accepted loops = 2
mandatory graph nodes = starter deterministic and >= route cells
generated_world_edges.csv = deterministic byte[] only
Authoring CSV schema/body modifications = 0
```

## Mandatory Read Order

1. `00_MCP_ENTRYPOINT.md`
2. `01_PROJECT_LOCKED_RULES.md`
3. `02_MCP_WORK_RULES.md`
4. `03_DATA_CSV_RULES.md`
5. `04_UNITY_MCP_RULES.md`
6. `05_CHANGE_CONTROL_RULES.md`
7. `07_PATCH_APPLY_RULES.md`
8. `08_STATUS_FINALIZE_RULES.md`
9. `MASTER_IMPLEMENTATION_TASK_LIST.md`
10. `06_IMPLEMENTATION_STATUS.md`
11. this Task
12. `REPORTS/MAP05_07_ADD_MANDATORY_ROUTE_LOOPS_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP05_07_ADD_MANDATORY_ROUTE_LOOPS
STATUS: PASS
TERMINAL-PAIR CANDIDATES / ELIGIBLE: 7 / 7
ACCEPTED / INDEPENDENT LOOPS: 2 / 2
SHARED CELL COUNT / TOTAL COST: 4 / 17
TYPE4 RULE: U+D mandatory; L/R preserved without canonicalization
TEST EXECUTED TOTAL: 1654/1654 PASS
ASSET META: 3215
SHA-256: cbe4f9a136d488df134a6eee676e13950d5dfd15238abf3188a81ce532fbdf65
DONE CONDITIONS: PASS
```

## Map Package Reference

```text
01_FIXED_SPEC/06_ROUTE_TOPOLOGY_CONSTRAINTS.md
01_FIXED_SPEC/07_OPTIONAL_EDGE_OVERLAY.md
02_PHASE_ROADMAP/MAP05_ROUTE_123_GENERATOR.md
03_CSV_SCHEMA/CSV_DATA_DICTIONARY.csv          # generated_world_edges.csv 11 rows only
05_GENERATED_OUTPUT_SCHEMA/generated_world_edges.csv # header template only
```

Authoring CSV body는 다시 읽거나 수정하지 않는다. source of truth는 MAP05_01~07 typed artifacts와 existing `GeneratedWorldData` snapshot이다. Type4 route-mask family는 generated mandatory graph 내부 family로 등록하며 static Authoring CSV schema/data migration은 시작하지 않는다.

## READ ALLOWLIST

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedWorldData.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedWorldDataCsvSerializer.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteTerminalSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteMaskLookup.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryConnectorTree.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/HorizontalBackbonePlan.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/VerticalGatewayPlan.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/UpDownConflictResolutionPlan.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteLoopPlan.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchValidationPublication.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/GeneratedWorldDataTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteLoopPlannerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/UpDownConflictResolverTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/VerticalGatewayPlannerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/HorizontalBackboneRouterTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryConnectorTreeBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryTerminalBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/BiomePatchValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map04ExitTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

matching meta, approved `Generation` 직계 path-only inventory, Authoring CSV/meta count/hash, generated output schema header, 전체 meta GUID와 task-marker 이후 change-scope만 확인한다. MAP05_09+ Task body와 unrelated production/Scene/Prefab YAML은 읽지 않는다.

## WRITE ALLOWLIST

신규 Runtime production C# exact 13:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteMaskFamily.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphNodeId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphEdgeId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphNode.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphEdge.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraph.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphBuildError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphBuildResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphBuilder.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedWorldEdge.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedWorldEdgesCsvSerializer.cs
```

기존 Runtime production C# allowed exact 3:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedWorldData.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedWorldDataCsvSerializer.cs
```

위 3개는 route stamp를 immutable하게 복사하거나 기존 sector serializer regression을 보존하는 데 필요한 최소 변경만 허용한다. `route_mask_id`/`mandatory_graph_node` 외 기존 CSV 열 순서와 byte contract를 바꾸지 않는다.

신규 Runtime EditMode test exact 1:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteGraphBuilderTests.cs
```

기존 test C# allowed exact 7:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/GeneratedWorldDataTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteLoopPlannerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/UpDownConflictResolverTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/VerticalGatewayPlannerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/HorizontalBackboneRouterTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryConnectorTreeBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs
```

기존 tests는 MAP05_08 output symbols를 허용하고 MAP05_09+ symbols는 계속 금지하도록만 전환한다. 신규 C# 14개와 matching `.cs.meta` 14개, Result 1개만 생성한다. Authoring CSV/meta/asmdef/Scene/Prefab는 수정하지 않는다.

## Namespace / Assembly

```text
Runtime namespace: StarNight.Map.WorldGeneration.Generation
Test namespace:    StarNight.Map.Tests.WorldGeneration.Generation
Runtime assembly:  Game.Map.Runtime
Test assembly:     Game.Map.Tests.EditMode
```

`UnityEditor`, Unity object/lifecycle, record/record struct, `required`, `init`, nullable-reference directive, reflection factory, singleton/static mutable state를 도입하지 않는다.

## Frozen P03 Boundary

```text
Input artifacts = TERMINALS + MASK_LOOKUP + CONNECTOR_TREE + HORIZONTAL_BACKBONE_PLAN + VERTICAL_GATEWAY_PLAN + UP_DOWN_CONFLICT_RESOLUTION_PLAN + MANDATORY_ROUTE_LOOP_PLAN
Output artifacts = MANDATORY_ROUTE_GRAPH + ROUTE-STAMPED GENERATED_WORLD_DATA + GENERATED_WORLD_EDGES CSV BYTES
Pass ID = PASS_ROUTE
RNG stream = none consumed in this Task
Minimum independent loops preserved = 2
```

## Type1/2/3/4 Mask Family Contract

`MandatoryRouteMaskFamily` registers exact mandatory mask IDs:

```text
ROUTE_T1_LR    = L+R
ROUTE_T2_LRD   = L+R+D
ROUTE_T3_LRU   = L+R+U
ROUTE_T4_UD    = U+D
ROUTE_T4_LUD   = L+U+D
ROUTE_T4_RUD   = R+U+D
ROUTE_T4_LRUD  = L+R+U+D
```

Type1/2/3 must continue to agree with `MandatoryRouteMaskLookup`. Type4 is graph-local generated mandatory family in this Task; do not modify Authoring `sector_route_masks.csv`. Every Type4 cell must have `OpenUp && OpenDown`; L/R is copied from actual horizontal graph adjacency. No canonicalization to `ROUTE_T4_LRUD`; no forced horizontal opens; no false conflict for `UD`, `LUD`, `RUD`, or `LRUD`.

Any mandatory graph cell with an unsupported open combination is a hard build error. Type0 masks remain excluded from mandatory graph construction.

## Mandatory Route Graph Contract

`MandatoryRouteGraphNodeId` and `MandatoryRouteGraphEdgeId` are `public readonly struct`, `IEquatable<T>`, `IComparable<T>`, ordinal deterministic, exact grammar:

```text
NODE_[0-9]{03}_[A-Z0-9_]+
EDGE_[0-9]{03}_[LRUD]_[A-Z0-9_]+
```

`MandatoryRouteGraphNode` stores sector coordinate/index, terminal/site/loop/gateway source IDs, route mask ID, open L/R/U/D, shortest distance from Start, and `MandatoryGraphNode=true`.

`MandatoryRouteGraphEdge` stores directed from/to, side, reverse side, layer `MANDATORY`, traversal kind, edge signature ID, cost tiles, source artifact ID, and open state. One undirected connection must produce two directed rows with opposite sides and matching cost/source identity.

`MandatoryRouteGraphCell` is the route-stamped cell projection used to create a new immutable `GeneratedWorldData` snapshot. It must preserve existing biome/patch/site/boundary/recipe/reservation fields by value and set only:

```text
Role = Mandatory for route cells that are not ReservedSite footprint cells
RouteMaskId = resolved Type1/2/3/4 family ID
MandatoryGraphNode = true
ShortestDistanceFromStart = deterministic BFS distance from Start
```

ReservedSite footprint cells are not converted into ordinary Type1/2/3/4 cells unless they are the approved entry adapter. Site identity must be preserved.

`MandatoryRouteGraph` exposes immutable ordered node/edge/cell snapshots, lookup by sector and edge ID, source artifact identities, route-stamped `GeneratedWorldData`, and generated edge CSV bytes. It must not mutate any input artifact.

## Edge And CSV Contract

`GeneratedWorldEdge` models exact 11-column `generated_world_edges.csv` rows:

```text
seed,from_sector_x,from_sector_y,side,to_sector_x,to_sector_y,edge_layer,traversal_kind,open,edge_signature_id,cost_tiles
```

Serializer rules:

- filename constant exact `generated_world_edges.csv`
- UTF-8 BOM once, CRLF record separator, final CRLF
- header must byte-match Map Package template
- rows sorted by from sector index, side order `L,R,U,D`, then to sector index
- `edge_layer` exact `MANDATORY`
- horizontal edge traversal exact `WALK`, edge signature `EDGE_H_MID_WALK`
- vertical base edge traversal exact `DROP_CLIMB_PAIR`, edge signature `EDGE_V_CENTER_CLIMB`
- `open` exact `1` for emitted rows; closed rows are not emitted
- `cost_tiles` invariant checked int, non-negative
- no timestamp/path/GUID/JSON/extra columns/filesystem write

Sector CSV serialization remains `generated_world_sectors.csv` v1 13-column. This Task may change values in existing `route_mask_id`, `shortest_distance_from_start`, and `mandatory_graph_node` fields only through a new immutable `GeneratedWorldData` snapshot. It must not add columns or change row order.

## Builder Ordering

Graph assembly order is fixed:

1. import connector tree edges
2. import horizontal backbone cells
3. import vertical gateway pairs
4. apply up/down conflict resolution plan
5. add accepted mandatory loops
6. compute open L/R/U/D per sector
7. resolve Type1/2/3/4 route mask family
8. compute BFS distance from Start across emitted mandatory edges
9. build route-stamped `GeneratedWorldData`
10. serialize `generated_world_edges.csv` bytes

Tie-breaks use checked total cost, source phase order, source ID ordinal, sector index, side order `L,R,U,D`. Culture, thread scheduling, hash randomization, caller collection order, and filesystem order must not affect output.

## Required Tests

`MandatoryRouteGraphBuilderTests.cs` actual NUnit cases minimum `252`:

- ID validation/equality/order/hash/culture and immutable graph value contracts
- Type1/2/3 lookup agreement and Type4 `UD/LUD/RUD/LRUD` family resolution
- no Type4 L/R canonicalization and no forced horizontal open/close
- starter graph imports 7 terminals, 6 tree edges, 6 backbone paths, 4 gateway pairs, 0 conflict resolutions, 2 loops
- directed edge symmetry, side/reverse-side correctness, duplicate edge rejection
- route-stamped `SectorCell.RouteMaskId`, `MandatoryGraphNode`, shortest distance, role preservation
- generated sectors CSV v1 remains 13 columns and byte deterministic
- generated edges CSV exact 11 columns, BOM/CRLF/header/order/token/cost contract
- Start BFS reaches all mandatory terminals and accepted loop anchors
- invalid unsupported masks, broken reciprocity, missing terminal, route into inactive/reserved interior, source mutation rejection
- fresh/reused/shuffled/culture/thread determinism
- no RNG/filesystem/clock/UnityEditor/static mutable state; prior negative audit transition

Actually run:

```text
MandatoryRouteGraphBuilderTests       >=252 PASS
MandatoryRouteLoopPlannerTests        212/212 PASS
UpDownConflictResolverTests           194/194 PASS
VerticalGatewayPlannerTests           156/156 PASS
HorizontalBackboneRouterTests         142/142 PASS
MandatoryConnectorTreeBuilderTests    129/129 PASS
MandatoryRouteMaskLookupBuilderTests  127/127 PASS
MandatoryTerminalBuilderTests         120/120 PASS
GeneratedWorldDataTests                56/56 PASS
SiteReservationValidatorTests         268/268 PASS
BiomePatchValidatorTests              196/196 PASS
Map04ExitTests                        110/110 PASS
Actually executed total               >=1962 PASS
failed/skipped                          0/0
Game.Map targeted discovery          >=6697
Full EditMode discovery               >=6809
forced refresh/compile/Console/warnings 0/0/0
```

## Asset / Meta / Change Gate

```text
baseline Authoring CSV/meta = 50/50; Assets meta = 3215; legacy Editor folder meta = 6/6; duplicate GUID = 0
new Runtime production C# = 13; new Runtime test C# = 1; new matching cs.meta = 14
modified existing production C# <= 3; modified existing test C# = 7
final Assets meta = 3229; task-marker Assets changes <= 38
Authoring CSV/body/meta modifications = 0
asmdef/Scene/Prefab/Package/ProjectSettings modifications = 0
unexpected Assets changes = 0; new folder meta = 0
```

New meta uses `fileFormatVersion: 2` and unique lowercase 32-hex GUIDs. Existing test `.meta`, Authoring CSV/meta, progress Scene and accepted legacy meta are byte-preserved.

## Failure Policy / Result

Contract/test/compile/meta/change-scope mismatch is `FAIL`; Unity/Test Runner unavailable is `BLOCKED`. Do not finalize or open MAP05_09 unless PASS. Result must be `REPORTS/MAP05_08_BUILD_MANDATORY_ROUTE_GRAPH_RESULT.md` within 150 lines and record task/status, patch/read/create/modify, Type1/2/3/4 mask counts, graph node/edge/cell counts, generated sector/edge CSV bytes, BFS reachability, diagnostics, deterministic/immutability, test/Unity/meta/change/ownership, out-of-scope, done, next and commit.

PASS일 때만 MAP05_08 COMPLETE, Current Task NONE으로 finalize하고 `MAP05_09_IMPLEMENT_MANDATORY_ROUTE_VALIDATOR`는 LOCKED로 유지한다.

Recommended Commit: `feat(map): build mandatory route graph`
