# MAP05_05 — Implement Vertical Gateway Planner

```yaml
status_control:
  task_key: MAP05_05_IMPLEMENT_VERTICAL_GATEWAY_PLANNER
  result_file: REPORTS/MAP05_05_IMPLEMENT_VERTICAL_GATEWAY_PLANNER_RESULT.md
```

## TASK TYPE

```text
RUNTIME IMMUTABLE P03 VERTICAL GATEWAY PAIRS + DETERMINISTIC PLANNER + EDITMODE TESTS
```

## Objective

`MAP05_04`의 `HorizontalBackbonePlan`에서 row transition이 필요한 네 개 segment를 읽고, 각 transition을 상단 `Type2.D` → 중간 `Type4(U+D 보장, L/R 선택적)` junction → 하단 `Type3.U` 구조로 계획한다. 이번 Task의 output은 vertical gateway pair와 Type4 중간 junction 후보이며, U/D conflict 해소, loop, 최종 route graph, `SectorCell.RouteMaskId`, CSV, validator와 overlay는 후속 Task다.

```text
input horizontal segments = 6
pending row transitions = 4
output gateway pairs = 4
anchors = 8 (4 upper Type2.D + 4 lower Type3.U)
Type4 junction cells = every eligible interior cell where both vertical directions are required (deterministic count from input)
same-row segments carried through = 2
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
12. `REPORTS/MAP05_04_IMPLEMENT_HORIZONTAL_BACKBONE_ROUTER_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP05_04_IMPLEMENT_HORIZONTAL_BACKBONE_ROUTER
STATUS: PASS
TREE EDGES / HORIZONTAL SEGMENTS: 6 / 6
SAME-ROW / DIFFERENT-ROW: 2 / 4
PENDING GATEWAY ANCHORS: 8
TEST EXECUTED TOTAL: 1092/1092 PASS
ASSET META: 3188
SHA-256: 6fcb71658dbf3924c1335b8c10ad93f26fca1a62648571b1e9eb08d62d14a6c4
DONE CONDITIONS: PASS
```

이 별도 patch가 적용된 뒤에만 MAP05_05를 실행한다.

## Map Package Reference

exact installed Map Package v1.0이 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
01_FIXED_SPEC/06_ROUTE_TOPOLOGY_CONSTRAINTS.md
02_PHASE_ROADMAP/MAP05_ROUTE_123_GENERATOR.md
04_CSV_STARTER/generation_profiles.csv
```

reference는 Type2/Type3/Type4 gateway 의미 확인용이다. installed Authoring CSV body를 다시 읽거나 파싱하지 않는다. source of truth는 MAP05_01~04 typed artifacts와 이번 planner의 immutable Type4 junction output이다. 기존 MAP05_02 CSV/lookup contract는 이 Task에서 수정하지 않는다.

## READ ALLOWLIST

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteTerminalId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteTerminal.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteTerminalSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteOpenMask.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteMaskLookup.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryConnectorEdgeId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryConnectorTree.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/HorizontalBackboneSegmentId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/HorizontalBackboneRouteCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/HorizontalBackboneSegment.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/HorizontalBackbonePlan.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGridIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchValidationPublication.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomeSectorOwnership.cs
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

matching meta, approved Runtime/Test `Generation` 직계 path-only inventory, Authoring CSV/meta count/hash, 전체 meta GUID와 task-marker 이후 change-scope path만 확인한다. installed CSV body, MAP05_06+ Task body, unrelated production/test body, Legacy/Stage/P6/P11 generator body와 Scene/Prefab YAML은 읽지 않는다.

## WRITE ALLOWLIST

신규 Runtime production C# exact 8:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/VerticalGatewayId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/VerticalGatewayAnchor.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/VerticalGatewayPair.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/VerticalGatewayPlan.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/VerticalGatewayBuildError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/VerticalGatewayDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/VerticalGatewayBuildResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/VerticalGatewayPlanner.cs
```

신규 Runtime EditMode test exact 1:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/VerticalGatewayPlannerTests.cs
```

기존 boundary-audit 전환 exact 3:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryConnectorTreeBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/HorizontalBackboneRouterTests.cs
```

기존 3개는 MAP05_05 output symbols를 허용하도록 negative symbol audit만 전환하며 MAP05_06+ symbols는 계속 금지한다. `.cs.meta`는 읽기만 하며 SHA/GUID를 보존한다. 신규 C# 9개와 matching `.cs.meta` 9개, Result 1만 생성하고 기존 production/CSV/meta/asmdef/Scene/Prefab와 folder meta는 수정하지 않는다.

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
Input artifacts  = HORIZONTAL_BACKBONE_PLAN + MANDATORY_ROUTE_MASK_LOOKUP + SITE_RESERVATIONS + BIOME_PATCHES
Output artifact  = VERTICAL_GATEWAY_PLAN
Pass ID          = PASS_ROUTE
RNG stream       = none consumed in this Task
Pending segments = 4 exact
Gateway pairs    = 4 exact
```

## Gateway Contract

`VerticalGatewayId`는 `public readonly struct`, `IEquatable<VerticalGatewayId>`, `IComparable<VerticalGatewayId>`다.

```text
string Value
bool IsValid
VerticalGatewayId(string value)
bool TryCreate(string value, out VerticalGatewayId result)
```

grammar는 exact `^VGW_[0-9]{2}_[A-Z0-9_]+$`이며 equality/order/hash는 ordinal deterministic이다. ID는 pending segment 순서와 source tree edge suffix로 만든다.

`VerticalGatewayAnchor` immutable properties:

```text
SectorCoord Coord
bool IsUpperAnchor
bool OpensDown
bool OpensUp
bool IsEndpointAdapter
bool IsReserved
int StepCost
```

상단은 `OpensDown == true`, `OpensUp == false`인 Type2.D 역할, 하단은 `OpensUp == true`, `OpensDown == false`인 Type3.U 역할이다. 실제 `SectorCell` mask에는 기록하지 않는다.

`VerticalGatewayPair.cs` 안의 `VerticalGatewayJunctionCell` immutable value type은 각 eligible interior coordinate에 대해 `bool OpensLeft`, `bool OpensRight`, `OpensUp == true`, `OpensDown == true`, `RouteType == 4`를 보관한다. L/R은 horizontal backbone과 실제 인접 연결 상태를 그대로 보존하며 강제로 열거나 닫지 않는다. 이것이 이번 Task의 Type4 zone 표식이며, final route-mask family 생성은 MAP05_08 graph task에서 U+D를 보장하고 L/R 비트를 보존하는 방식으로 수행한다.

Type4는 단일 고정 mask가 아니다. 허용되는 수평 조합은 `U+D`, `L+U+D`, `R+U+D`, `L+R+U+D` 네 가지이며, 네 조합 모두 Type4다. `OpensLeft`/`OpensRight`를 true로 만들기 위한 보정이나 false를 invalid로 처리하는 검사는 금지한다.

`VerticalGatewayPair` immutable properties:

```text
VerticalGatewayId GatewayId
HorizontalBackboneSegmentId SourceSegmentId
VerticalGatewayAnchor Upper
VerticalGatewayAnchor Lower
int GatewayColumn
int VerticalDistance
int TotalCost
bool RequiresUpDownConflictResolution
IReadOnlyList<SectorCoord> SpanCells
IReadOnlyList<VerticalGatewayJunctionCell> Type4JunctionCells
```

upper/lower는 같은 X, upper Y는 lower Y보다 위이며 span은 inclusive ordered다. endpoint adapter만 reserved일 수 있고 reserved footprint는 middle span cell이 될 수 없다. span은 진단용 후보이며 horizontal cell, route mask, graph를 변경하지 않는다. span의 eligible interior cell은 Type4 junction으로 표시하되, U/D는 항상 true, L/R은 실제 수평 연결 상태를 보존하며, Type4 junction 역시 `SectorCell.RouteMaskId`를 직접 쓰지 않는다. same-row segment에는 pair를 만들지 않는다.

## Planner API and Rules

```text
public sealed class VerticalGatewayPlanner

VerticalGatewayBuildResult Build(
    HorizontalBackbonePlan horizontalPlan,
    MandatoryRouteMaskLookup routeMaskLookup,
    SiteReservationSnapshot siteSnapshot,
    BiomePatchValidationPublication biomePublication)
```

source identity와 immutable observable state를 보존한다. pending segment만 처리하고 각 pending segment에 정확히 하나의 upper Type2.D/lower Type3.U pair를 만든다. 같은 column의 eligible interior span cells는 Type4 junction으로 모두 기록한다. 각 Type4 junction은 U/D를 반드시 true로 만들고 L/R은 실제 horizontal backbone 인접성에서 계산한다. 후보 column order는 checked total cost, shorter vertical distance, lower gateway X, source segment ID ordinal이다. 1/2/4/8 비용과 infinity(세계 밖, inactive, 다른 reservation footprint)를 사용한다. diagonal/horizontal detour, U+D repair/offset, Type1 replacement, loop, graph edge, CSV row, `SectorCell.RouteMaskId` write는 금지한다. Type4로 표현 가능한 양방향 연결은 conflict로 세지 않고, Type4로 표현할 수 없는 충돌만 `RequiresUpDownConflictResolution` 진단으로 MAP05_06에 넘긴다.

`VerticalGatewayPlan` immutable API:

```text
HorizontalBackbonePlan SourceHorizontalPlan
MandatoryRouteMaskLookup SourceRouteMaskLookup
SiteReservationSnapshot SourceSiteSnapshot
BiomePatchValidationPublication SourceBiomePublication
IReadOnlyList<VerticalGatewayPair> GatewayPairs
int GatewayPairCount
int PendingSegmentCount
int UpperAnchorCount
int LowerAnchorCount
int Type4JunctionCellCount
int ConflictPendingCount
int TotalVerticalSpanCellCount
int TotalCost
bool TryGetPair(VerticalGatewayId id, out VerticalGatewayPair pair)
IReadOnlyList<VerticalGatewayPair> GetPairsForSegment(HorizontalBackboneSegmentId segmentId)
```

`VerticalGatewayBuildError` stable codes:

```text
MissingInput, InvalidHorizontalBackbonePlan, InvalidRouteMaskLookup,
InvalidSiteSnapshot, InvalidBiomePublication, PendingSegmentCountMismatch,
GatewayPairCountMismatch, InvalidGatewayIdentity, InvalidAnchorOrientation,
InvalidColumnAlignment, ForbiddenReservationIntrusion, WorldBoundsViolation,
InvalidType4Junction, Type4ReservationIntrusion, UnsupportedSameRowGateway,
ConflictResolutionAttempted, SourceMutationDetected
```

errors sort/dedupe by code, first/second ID ordinal, sector index, message ordinal. `VerticalGatewayDiagnostics`는 immutable하며 `HorizontalSegmentCount`, `PendingSegmentCount`, `GatewayPairCount`, `UpperAnchorCount`, `LowerAnchorCount`, `Type4JunctionCellCount`, `ConflictPendingCount`, `TotalVerticalSpanCellCount`, reserved endpoint/middle counts, bounds count, `OpenUpCount`, `OpenDownCount`, graph/CSV counts, `RngDrawCount`, `SourceMutationCount`를 가진다. `OpenUpCount`/`OpenDownCount`는 Type4 planner output의 semantic count이고, `SectorCell.RouteMaskId` writer count는 0이다. Result는 `Completed`(plan, errors 0, retry false) 또는 `InvalidInput`(plan null, errors >=1, retry false)다.

starter invariants:

```text
horizontal/pending/pairs = 6/4/4
upper/lower anchors = 4/4
Type4 junction cells = deterministic interior count; every reported cell has U/D true, L/R independently preserved, and RouteType 4
SectorCell.RouteMaskId writes = 0; Type4 semantic U/D opens are output-only
route graph/generated CSV = 0/0
RNG/mutation = 0/0
```

## Prior Negative Audit Transition

Allowed after MAP05_05: `VerticalGatewayId`, `VerticalGatewayAnchor`, `VerticalGatewayPair`, `VerticalGatewayPlan`, `VerticalGatewayBuildError`, `VerticalGatewayDiagnostics`, `VerticalGatewayBuildResult`, `VerticalGatewayPlanner`.

Still forbidden as MAP05_06+: `UpDownConflictResolver`, `UpDownConflictPlan`, `MandatoryRouteLoop`, `MandatoryRouteGraph`, `MandatoryRouteValidator`, `MandatoryRouteOverlay`, `MandatoryRoutePass`, `GeneratedWorldEdge`, `SectorCell.RouteMaskId writer`. Type4는 단일 고정 ID가 아니라 U+D 보장 및 L/R 선택 상태를 보존하는 route-mask family semantic으로만 기술하며, completed MAP05_02 lookup을 수정하지 않는다. Audit case counts를 줄이거나 삭제하지 말고 MAP05_06+ 메시지로만 바꾼다.

## Determinism / Immutability / Scope

fresh/reused planner, shuffled exposure, en-US/tr-TR, repeated/parallel builds에서 exact same pair order/span/cost/diagnostics를 보장한다. RNG calls/draws는 0이며 static cache/current set, filesystem, clock, Unity object, current-culture ordering을 사용하지 않는다. MAP05_06 conflict, MAP05_07 loop, MAP05_08 graph/CSV/mask, MAP05_09~11 validator/overlay/batch/root/adapter, Type0/microchunk/tile/SpecialMap은 구현하지 않는다. test skip/ignore/assertion 삭제와 Git operation도 금지한다.

## Required Tests

`VerticalGatewayPlannerTests.cs` actual NUnit cases minimum `148`:

- ID valid/invalid/default/equality/order/hash/culture and anchor orientation/immutability
- same-column pair, inclusive span, Type2.D/Type3.U semantics, all four Type4 horizontal combinations with mandatory U+D, exact 4 pending -> 4 pairs/8 anchors
- cost/column tie-break, infinity rejection, reservation middle-span and bounds rejection
- conflict diagnostic only; no offset/loop/graph/CSV/mask writer
- ID/segment lookup, source identity/mutation isolation, shuffled/culture/thread/fresh-reused determinism
- RNG/file/time/UnityEditor/static mutable audit and prior negative audit transition

Actually run:

```text
VerticalGatewayPlannerTests             >=148 PASS
HorizontalBackboneRouterTests           142/142 PASS
MandatoryConnectorTreeBuilderTests      129/129 PASS
MandatoryRouteMaskLookupBuilderTests    127/127 PASS
MandatoryTerminalBuilderTests           120/120 PASS
SiteReservationValidatorTests           268/268 PASS
BiomePatchValidatorTests                196/196 PASS
Map04ExitTests                          110/110 PASS
Actually executed total                 >=1098 PASS
failed/skipped                            0/0
Game.Map targeted discovery            >=5889
Full EditMode discovery                 >=6000
forced refresh/compile/Console/warnings  0/0/0
```

## Asset / Meta / Change Gate

```text
baseline Authoring CSV/meta = 50/50; Assets meta = 3188; legacy Editor folder meta = 6/6; duplicate GUID = 0
new Runtime production C# = 8; new Runtime test C# = 1; new matching cs.meta = 9
modified existing test C# = 3; final Assets meta = 3197; task-marker Assets changes = 21
existing production modifications = 0; unexpected Assets changes = 0; new folder meta = 0
```

new meta는 `fileFormatVersion: 2`, unique lowercase 32-hex GUID다. Existing test `.meta`, Authoring CSV/meta, progress Scene와 accepted legacy meta는 byte-preserve한다.

## Failure Policy / Result

contract/test/compile/meta/change-scope 한 조건이라도 불일치면 `FAIL`, Unity/Test Runner 접근이 없으면 `BLOCKED`다. PASS가 아니면 finalize하지 않고 MAP05_06을 열지 않는다. Result는 `REPORTS/MAP05_05_IMPLEMENT_VERTICAL_GATEWAY_PLANNER_RESULT.md`에 `TASK / STATUS / SUMMARY`, patch/read/create/modify, pairs/anchors/Type4 junctions/spans/cost, audit/source/determinism/immutability, test/Unity/meta/change/ownership, out-of-scope/done/next/commit을 150 lines 이내로 기록한다.

PASS일 때만 MAP05_05 COMPLETE, Current Task NONE으로 finalize하고 `MAP05_06_RESOLVE_UP_DOWN_CONFLICTS`는 LOCKED로 유지한다.

Recommended Commit: `feat(map): add vertical mandatory gateway planner`
