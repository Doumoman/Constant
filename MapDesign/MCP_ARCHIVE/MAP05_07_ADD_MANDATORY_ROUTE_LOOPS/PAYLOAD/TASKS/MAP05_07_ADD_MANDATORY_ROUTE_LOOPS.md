# MAP05_07 — Add Mandatory Route Loops

```yaml
status_control:
  task_key: MAP05_07_ADD_MANDATORY_ROUTE_LOOPS
  result_file: REPORTS/MAP05_07_ADD_MANDATORY_ROUTE_LOOPS_RESULT.md
```

## TASK TYPE

```text
RUNTIME IMMUTABLE MANDATORY ROUTE LOOP PLAN + DETERMINISTIC PLANNER + EDITMODE TESTS
```

## Objective

`MAP05_06`의 `UpDownConflictResolutionPlan`과 기존 mandatory tree/backbone/gateway 입력을 읽고, Start·Core·Forge·Boss·Village entry 필수망 사이에 최소 2개의 독립적인 loop 후보를 계획한다. 이번 Task는 loop 계획과 진단만 만든다. 최종 graph, route-mask family 등록, `SectorCell.RouteMaskId`, generated CSV, validator와 overlay는 후속 Task다.

Type4 규칙은 그대로 고정한다. 모든 Type4 셀은 U+D를 보장하고 L/R은 실제 수평 인접 상태를 보존한다. `U+D`, `L+U+D`, `R+U+D`, `L+R+U+D` 네 조합은 모두 유효하며 loop를 만들기 위해 L/R을 열거나 닫지 않는다.

```text
input terminals/tree/backbone/gateways = 7 / 6 / 6 / 4
minimum mandatory loops = 2
starter loop candidates = deterministic and >= 2
graph/CSV/RouteMaskId writes = 0 / 0 / 0
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
12. `REPORTS/MAP05_06_RESOLVE_UP_DOWN_CONFLICTS_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP05_06_RESOLVE_UP_DOWN_CONFLICTS
STATUS: PASS
INPUT VERTICAL GATEWAY PAIRS: 4
STARTER TYPE4 CANDIDATES: 11
STARTER CONFLICT / RESOLVED / UNRESOLVED: 0 / 0 / 0
TEST EXECUTED TOTAL: 1442/1442 PASS
ASSET META: 3206
SHA-256: 430930f35e6bd3be0ee8ffc9bc4aa06daeb90cf2828c50ac4148368bc24fed79
DONE CONDITIONS: PASS
```


## Map Package Reference

```text
01_FIXED_SPEC/06_ROUTE_TOPOLOGY_CONSTRAINTS.md
02_PHASE_ROADMAP/MAP05_ROUTE_123_GENERATOR.md
04_CSV_STARTER/generation_profiles.csv
```

Authoring CSV body는 다시 읽거나 파싱하지 않는다. source of truth는 MAP05_01~06 typed artifacts와 이번 immutable loop plan이다. MAP05_02 Type1/2/3 lookup은 수정하지 않는다.

## READ ALLOWLIST

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteTerminalSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryConnectorTree.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/HorizontalBackbonePlan.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/VerticalGatewayPlan.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/UpDownConflictResolutionPlan.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteMaskLookup.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchValidationPublication.cs
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

matching meta, approved `Generation` 직계 path-only inventory, Authoring CSV/meta count/hash, 전체 meta GUID와 task-marker 이후 change-scope만 확인한다. MAP05_08+ Task body와 unrelated production/Scene/Prefab YAML은 읽지 않는다.

## WRITE ALLOWLIST

신규 Runtime production C# exact 8:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteLoopId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteLoopCandidate.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteLoop.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteLoopPlan.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteLoopBuildError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteLoopDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteLoopBuildResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteLoopPlanner.cs
```

신규 Runtime EditMode test exact 1:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteLoopPlannerTests.cs
```

기존 negative symbol audit 전환 exact 5:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryConnectorTreeBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/HorizontalBackboneRouterTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/VerticalGatewayPlannerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/UpDownConflictResolverTests.cs
```

기존 5개는 MAP05_07 output symbols를 허용하도록 negative audit만 전환하고 MAP05_08+ symbols는 계속 금지한다. 신규 C# 9개와 matching `.cs.meta` 9개, Result 1개만 생성한다. 기존 production/CSV/meta/asmdef/Scene/Prefab는 수정하지 않는다.

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
Input artifacts  = TERMINALS + CONNECTOR_TREE + HORIZONTAL_BACKBONE_PLAN + VERTICAL_GATEWAY_PLAN + UP_DOWN_CONFLICT_RESOLUTION_PLAN
Output artifact  = MANDATORY_ROUTE_LOOP_PLAN
Pass ID          = PASS_ROUTE
RNG stream       = none consumed in this Task
Minimum loop count = 2
```

## Loop Contract

`MandatoryRouteLoopId`는 `public readonly struct`, `IEquatable<MandatoryRouteLoopId>`, `IComparable<MandatoryRouteLoopId>`이며 exact grammar `^LOOP_[0-9]{2}_[A-Z0-9_]+$`를 사용한다. equality/order/hash는 ordinal deterministic이다.

`MandatoryRouteLoopCandidate`는 두 distinct terminal/gateway anchors, ordered waypoint coordinates, source connector/tree/backbone/gateway IDs, site/biome identity, checked total cost와 `IsIndependent`를 immutable하게 보존한다. loop는 world bounds·reservation·inactive·existing mandatory path intrusion을 통과해야 한다.

`MandatoryRouteLoop`는 candidate를 승인한 immutable loop artifact다. start/end terminal, inclusive ordered cells, vertical junction references, shared-cell count, unique-cell count, cost와 independence witness를 노출한다. 기존 tree/backbone/vertical/conflict source는 참조로 보존하고 변경하지 않는다.

`MandatoryRouteLoopPlan`은 source identities, candidate/accepted loop 목록, `MinimumLoopCount`, `LoopCount`, `IndependentLoopCount`, `SharedCellCount`, `TotalCost`와 ordinal lookup API를 제공한다. starter는 `LoopCount >= 2`, `IndependentLoopCount >= 2`여야 하며 graph/CSV/mask writer count는 0이다.

Loop 후보 순서는 checked total cost, greater unique-cell coverage, shorter overlap, lower first sector index, loop ID ordinal 순이다. 최소 2개를 고른 뒤 동일한 edge/terminal pair를 중복 사용하지 않는다. Type4 junction의 U+D는 유지하고 L/R은 source horizontal adjacency에서 복사한다. L/R을 canonicalize하거나 Type4 단일 ID로 축약하지 않는다.

## Required Tests

`MandatoryRouteLoopPlannerTests.cs` actual NUnit cases minimum `176`:

- loop ID validation/equality/order/hash/culture and immutable value contracts
- exact terminal/tree/backbone/gateway source identity preservation
- minimum two loops, distinct terminal/edge pairs, independence witness and stable ordering
- bounds/reservation/inactive/duplicate/overlap rejection and unresolved diagnostics
- Type4 `UD/LUD/RUD/LRUD` preservation; no forced L/R and no RouteMaskId write
- deterministic fresh/reused/shuffled/culture/thread builds and source mutation isolation
- no RNG/filesystem/clock/UnityEditor/static mutable state; prior negative audit transition

Actually run:

```text
MandatoryRouteLoopPlannerTests       >=176 PASS
UpDownConflictResolverTests          194/194 PASS
VerticalGatewayPlannerTests          156/156 PASS
HorizontalBackboneRouterTests         142/142 PASS
MandatoryConnectorTreeBuilderTests    129/129 PASS
MandatoryRouteMaskLookupBuilderTests  127/127 PASS
MandatoryTerminalBuilderTests         120/120 PASS
SiteReservationValidatorTests         268/268 PASS
BiomePatchValidatorTests              196/196 PASS
Map04ExitTests                        110/110 PASS
Actually executed total               >=1618 PASS
failed/skipped                          0/0
Game.Map targeted discovery          >=6409
Full EditMode discovery               >=6521
forced refresh/compile/Console/warnings 0/0/0
```

## Asset / Meta / Change Gate

```text
baseline Authoring CSV/meta = 50/50; Assets meta = 3206; legacy Editor folder meta = 6/6; duplicate GUID = 0
new Runtime production C# = 8; new Runtime test C# = 1; new matching cs.meta = 9
modified existing test C# = 5; final Assets meta = 3215; task-marker Assets changes = 23
existing production modifications = 0; unexpected Assets changes = 0; new folder meta = 0
```

New meta uses `fileFormatVersion: 2` and unique lowercase 32-hex GUIDs. Existing test `.meta`, Authoring CSV/meta, progress Scene and accepted legacy meta are byte-preserved.

## Failure Policy / Result

Contract/test/compile/meta/change-scope mismatch is `FAIL`; Unity/Test Runner unavailable is `BLOCKED`. Do not finalize or open MAP05_08 unless PASS. Result must be `REPORTS/MAP05_07_ADD_MANDATORY_ROUTE_LOOPS_RESULT.md` within 150 lines and record task/status, patch/read/create/modify, loop/candidate/independence counts, diagnostics, deterministic/immutability, test/Unity/meta/change/ownership, out-of-scope, done, next and commit.

PASS일 때만 MAP05_07 COMPLETE, Current Task NONE으로 finalize하고 `MAP05_08_BUILD_MANDATORY_ROUTE_GRAPH`는 LOCKED로 유지한다.

Recommended Commit: `feat(map): add mandatory route loops`
