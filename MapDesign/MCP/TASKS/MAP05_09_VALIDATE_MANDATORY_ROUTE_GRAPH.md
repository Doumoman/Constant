# MAP05_09 — Validate Mandatory Route Graph

```yaml
status_control:
  task_key: MAP05_09_VALIDATE_MANDATORY_ROUTE_GRAPH
  result_file: REPORTS/MAP05_09_VALIDATE_MANDATORY_ROUTE_GRAPH_RESULT.md
```

## TASK TYPE

```text
RUNTIME MANDATORY ROUTE GRAPH VALIDATOR + IMMUTABLE VALIDATION REPORT + EDITMODE TESTS
```

## Objective

`MAP05_08`의 `MandatoryRouteGraph`, route-stamped `GeneratedWorldData`, generated edge records/CSV bytes를 읽고 Type1/2/3/4 mandatory route 규칙을 검증한다. 이번 Task는 validator와 report만 만든다. graph, route mask, `SectorCell`, generated CSV bytes, root, overlay는 수정하지 않는다.

Type4 규칙은 계속 고정한다. Type4는 U+D가 반드시 열려야 하며 L/R은 실제 graph adjacency를 보존한다. `UD`, `LUD`, `RUD`, `LRUD` 네 조합은 모두 합법이다. validator는 이 네 조합을 단일 `LRUD`로 canonicalize하지 않는다.

```text
input graph nodes/directed edges/route cells = 47 / 96 / 47
input mask counts T1/T2/T3/T4_UD/T4_LUD/T4_RUD/T4_LRUD = 20/4/4/17/0/0/2
mandatory terminals reachable from Start = 7/7
accepted loops represented = 2/2
output = validation report only
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
12. `REPORTS/MAP05_08_BUILD_MANDATORY_ROUTE_GRAPH_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP05_08_BUILD_MANDATORY_ROUTE_GRAPH
STATUS: PASS
GRAPH NODES / DIRECTED EDGES / UNDIRECTED EDGES / ROUTE CELLS: 47 / 96 / 48 / 47
MASK COUNTS T1/T2/T3/T4_UD/T4_LUD/T4_RUD/T4_LRUD: 20/4/4/17/0/0/2
MANDATORY TERMINALS REACHABLE FROM START: 7/7
ACCEPTED LOOPS REPRESENTED: 2/2
GENERATED SECTORS CSV BYTES: 16838
GENERATED EDGES CSV BYTES / ROWS: 7094 / 96
TEST EXECUTED TOTAL: 1991/1991 PASS
ASSET META: 3229
SHA-256: 7c9820290ec5269222b8c145603a9ae53a2ea7f8d1df7b0ca6029e1be3647a99
DONE CONDITIONS: PASS
```

## Map Package Reference

If present, use these only as reference. If the repository does not contain them, that absence is not a blocker because MAP05_08 already validated the frozen generated-edge schema and graph contract.

```text
01_FIXED_SPEC/06_ROUTE_TOPOLOGY_CONSTRAINTS.md
01_FIXED_SPEC/07_OPTIONAL_EDGE_OVERLAY.md
02_PHASE_ROADMAP/MAP05_ROUTE_123_GENERATOR.md
05_GENERATED_OUTPUT_SCHEMA/generated_world_edges.csv
05_GENERATED_OUTPUT_SCHEMA/generated_world_sectors.csv
```

Authoring CSV body/schema는 읽거나 수정하지 않는다. Source of truth는 MAP05_08 typed artifacts와 serializer contracts다.

## READ ALLOWLIST

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedWorldData.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedWorldDataCsvSerializer.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedWorldEdge.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedWorldEdgesCsvSerializer.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteMaskFamily.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphNodeId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphEdgeId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphNode.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphEdge.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraph.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphBuildResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphBuilder.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteTerminalSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteLoopPlan.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteGraphBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteLoopPlannerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/UpDownConflictResolverTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/VerticalGatewayPlannerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/HorizontalBackboneRouterTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryConnectorTreeBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryTerminalBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/GeneratedWorldDataTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/BiomePatchValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map04ExitTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

matching meta, approved `Generation` 직계 path-only inventory, Authoring CSV/meta count/hash, generated output schema header if present, 전체 meta GUID와 task-marker 이후 change-scope만 확인한다. MAP05_10+ Task body와 unrelated production/Scene/Prefab YAML은 읽지 않는다.

## WRITE ALLOWLIST

신규 Runtime production C# exact 8:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteValidationRuleId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteValidationSeverity.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteValidationViolation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteValidationSummary.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteValidationReport.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteValidationDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteValidationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphValidator.cs
```

신규 Runtime EditMode test exact 1:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteGraphValidatorTests.cs
```

기존 test C# allowed exact 8:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteGraphBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/GeneratedWorldDataTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteLoopPlannerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/UpDownConflictResolverTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/VerticalGatewayPlannerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/HorizontalBackboneRouterTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryConnectorTreeBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs
```

기존 tests는 MAP05_09 output symbols를 허용하고 MAP05_10+ symbols는 계속 금지하도록만 전환한다. 신규 C# 9개와 matching `.cs.meta` 9개, Result 1개만 생성한다. 기존 production/CSV/meta/asmdef/Scene/Prefab는 수정하지 않는다.

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
Input artifacts = MANDATORY_ROUTE_GRAPH + ROUTE-STAMPED GENERATED_WORLD_DATA + GENERATED_WORLD_EDGE RECORDS/BYTES + TERMINALS + LOOP PLAN
Output artifact = MANDATORY_ROUTE_VALIDATION_REPORT
Pass ID = PASS_ROUTE
RNG stream = none consumed in this Task
Filesystem writes = 0
```

## Validator Contract

`MandatoryRouteValidationRuleId` is a `public readonly struct`, `IEquatable<T>`, `IComparable<T>`, exact grammar:

```text
VAL_ROUTE_[A-Z0-9_]+
```

Required rule IDs:

```text
VAL_ROUTE_MASK_FAMILY
VAL_ROUTE_TYPE4_UD_REQUIRED
VAL_ROUTE_TYPE4_LR_PRESERVED
VAL_ROUTE_EDGE_RECIPROCITY
VAL_ROUTE_EDGE_SIDE_MATCH
VAL_ROUTE_TERMINAL_BFS
VAL_ROUTE_LOOP_REPRESENTED
VAL_ROUTE_SECTOR_STAMP
VAL_ROUTE_GENERATED_SECTOR_CSV
VAL_ROUTE_GENERATED_EDGE_CSV
VAL_ROUTE_NO_TYPE0_INTRUSION
VAL_ROUTE_SOURCE_IMMUTABILITY
```

`MandatoryRouteValidationViolation` stores rule ID, severity, graph node/edge IDs, sector coordinate/index, source artifact ID, stable message token, and deterministic sort key. Report ordering is severity, rule ID ordinal, sector index, edge ID ordinal, message token ordinal.

`MandatoryRouteValidationReport` exposes immutable violations, errors, warnings, rule pass/fail counts, terminal reachability count, loop representation count, mask family counts, directed/undirected edge counts, generated CSV byte sizes, and source artifact identities.

The validator must be fail-closed:

- Type1 exact L/R only
- Type2 exact L/R/D
- Type3 exact L/R/U
- Type4 exact U+D with actual L/R preserved: `UD`, `LUD`, `RUD`, `LRUD`
- every directed edge has exactly one reverse edge with opposite side and same open/cost/layer/source
- all emitted generated edge rows map to open graph edges and no extra rows exist
- every route-stamped sector has a supported mask and graph node flag
- Start BFS reaches all 7 mandatory terminals
- accepted loops 2/2 remain represented without collapsing into the tree
- no mandatory edge enters Type0, inactive, world-outside, or unapproved reserved interior

No graph repair, CSV rewrite, route mask replacement, or `SectorCell` mutation is allowed.

## Required Tests

`MandatoryRouteGraphValidatorTests.cs` actual NUnit cases minimum `240`:

- rule ID validation/equality/order/hash/culture and immutable report contracts
- exact PASS report for MAP05_08 starter vector: nodes 47, directed edges 96, route cells 47
- Type1/2/3 mask family validation and all four Type4 `UD/LUD/RUD/LRUD` combinations
- Type4 U+D missing, L/R canonicalization, forced L/R, unsupported mask failures
- directed edge reciprocity, side/reverse-side, duplicate/missing/extra generated edge row failures
- generated sector CSV stamp consistency: route mask, mandatory node, distance, 13-column contract
- terminal BFS 7/7 and accepted loop 2/2 representation checks
- Type0/inactive/world-outside/reserved interior rejection
- deterministic violation sorting, dedupe, fresh/reused/shuffled/culture/thread validation
- source mutation isolation and no RNG/filesystem/clock/UnityEditor/static mutable state
- prior negative audit transition

Actually run:

```text
MandatoryRouteGraphValidatorTests      >=240 PASS
MandatoryRouteGraphBuilderTests         281/281 PASS
MandatoryRouteLoopPlannerTests          212/212 PASS
UpDownConflictResolverTests             194/194 PASS
VerticalGatewayPlannerTests             156/156 PASS
HorizontalBackboneRouterTests           142/142 PASS
MandatoryConnectorTreeBuilderTests      129/129 PASS
MandatoryRouteMaskLookupBuilderTests    127/127 PASS
MandatoryTerminalBuilderTests           120/120 PASS
GeneratedWorldDataTests                  56/56 PASS
SiteReservationValidatorTests           268/268 PASS
BiomePatchValidatorTests                196/196 PASS
Map04ExitTests                          110/110 PASS
Actually executed total                 >=2231 PASS
failed/skipped                            0/0
Game.Map targeted discovery            >=6966
Full EditMode discovery                 >=7078
forced refresh/compile/Console/warnings  0/0/0
```

## Asset / Meta / Change Gate

```text
baseline Authoring CSV/meta = 50/50; Assets meta = 3229; legacy Editor folder meta = 6/6; duplicate GUID = 0
new Runtime production C# = 8; new Runtime test C# = 1; new matching cs.meta = 9
modified existing production C# = 0; modified existing test C# = 8
final Assets meta = 3238; task-marker Assets changes = 26
Authoring CSV/body/meta modifications = 0
generated CSV byte producer modifications = 0
asmdef/Scene/Prefab/Package/ProjectSettings modifications = 0
unexpected Assets changes = 0; new folder meta = 0
```

New meta uses `fileFormatVersion: 2` and unique lowercase 32-hex GUIDs. Existing test `.meta`, Authoring CSV/meta, progress Scene and accepted legacy meta are byte-preserved.

## Failure Policy / Result

Contract/test/compile/meta/change-scope mismatch is `FAIL`; Unity/Test Runner unavailable is `BLOCKED`. Do not finalize or open MAP05_10 unless PASS. Result must be `REPORTS/MAP05_09_VALIDATE_MANDATORY_ROUTE_GRAPH_RESULT.md` within 150 lines and record task/status, patch/read/create/modify, validation rules, graph/mask/edge/CSV/BFS/loop counts, violation/error/warning counts, deterministic/immutability, test/Unity/meta/change/ownership, out-of-scope, done, next and commit.

PASS일 때만 MAP05_09 COMPLETE, Current Task NONE으로 finalize하고 `MAP05_10_CREATE_MANDATORY_ROUTE_OVERLAY`는 LOCKED로 유지한다.

Recommended Commit: `feat(map): validate mandatory route graph`
