# MAP05_04 — Implement Horizontal Backbone Router

```yaml
status_control:
  task_key: MAP05_04_IMPLEMENT_HORIZONTAL_BACKBONE_ROUTER
  result_file: REPORTS/MAP05_04_IMPLEMENT_HORIZONTAL_BACKBONE_ROUTER_RESULT.md
```

## TASK TYPE

```text
RUNTIME IMMUTABLE P03 HORIZONTAL BACKBONE ROUTE CANDIDATES + DETERMINISTIC ROUTER + EDITMODE TESTS
```

## Objective

MAP05_03의 `MandatoryConnectorTree` 6개 abstract edge를 받아 각 terminal pair를 연결할 horizontal-only backbone segment 후보를 만든다.

이번 Task의 output은 sector-level horizontal run 후보뿐이다.

```text
input tree edges = 6
output backbone segments = 6
every segment preserves L/R run
same-row edge = direct horizontal run
different-row edge = two horizontal leg candidates + unresolved vertical gateway placeholder
```

Type2/3 vertical gateway placement, U/D conflict resolution, loops, final `MandatoryRouteGraph`, `SectorCell.RouteMaskId`, generated CSV, route validator, overlay, root/retry는 구현하지 않는다.

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
11. 이 Task
12. `REPORTS/MAP05_03_IMPLEMENT_MINIMUM_CONNECTOR_TREE_RESULT.md`

prior Result exact gate:

```text
TASK: MAP05_03_IMPLEMENT_MINIMUM_CONNECTOR_TREE
STATUS: PASS
TREE NODES: 7 exact
CANDIDATE EDGES: 21 exact
TREE EDGES: 6 exact
TEST EXECUTED TOTAL: 950/950 PASS
ASSET META: 3179 -> 3179
DONE CONDITIONS: PASS
SHA-256: 3fd9078ab5a2f288c0e8e657f510f0d84f1d3d49409ebc731621b38086dfa74d
```

이 별도 patch가 적용된 뒤에만 MAP05_04를 실행한다.

## Map Package Reference

exact installed Map Package v1.0이 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
01_FIXED_SPEC/06_ROUTE_TOPOLOGY_CONSTRAINTS.md
02_PHASE_ROADMAP/MAP05_ROUTE_123_GENERATOR.md
04_CSV_STARTER/generation_profiles.csv
```

reference는 horizontal-run/domain 확인용이다. installed Authoring CSV를 다시 읽거나 파싱하지 않는다. source of truth는 MAP05_01~03 typed artifacts다.

## READ ALLOWLIST

### Existing P03 route artifacts

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteTerminalId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteTerminal.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteTerminalSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteOpenMask.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteMaskLookup.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryConnectorEdgeId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryConnectorCandidateEdge.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryConnectorTree.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryConnectorTreeBuildResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryConnectorTreeBuilder.cs
```

### Existing coordinate / P01 / P02 context

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGridIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchValidationPublication.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomeSectorOwnership.cs
```

### Focused tests / assemblies

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryConnectorTreeBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryTerminalBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/BiomePatchValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map04ExitTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

위 파일과 matching meta, approved Runtime/Test `Generation` 직계 path-only inventory, Authoring CSV/meta count/hash, 전체 meta GUID, task marker 이후 change-scope path만 확인할 수 있다.

금지:

- installed Authoring CSV body 재파싱·수정
- MAP05_05 이후 Task body
- unrelated production/test C# body
- Legacy/Stage/P6/P11 generator body
- Scene/Prefab YAML

## WRITE ALLOWLIST

### 신규 Runtime production C# — exact 8

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/HorizontalBackboneSegmentId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/HorizontalBackboneRouteCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/HorizontalBackboneSegment.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/HorizontalBackbonePlan.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/HorizontalBackboneBuildError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/HorizontalBackboneDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/HorizontalBackboneBuildResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/HorizontalBackboneRouter.cs
```

### 신규 Runtime EditMode test — exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/HorizontalBackboneRouterTests.cs
```

### Existing test boundary-audit transition — exact 2

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryConnectorTreeBuilderTests.cs
```

기존 test 2개는 MAP05_04 output symbols를 더 이상 later-task forbidden으로 보지 않도록 negative symbol audit만 전환한다. MAP05_05+ symbols는 계속 금지한다. `.cs.meta`는 읽기만 하며 SHA/GUID를 보존한다.

신규 C# 9개와 matching `.cs.meta` 9개, Result 1만 생성한다. 기존 production/CSV/meta/asmdef/Scene/Prefab를 수정하지 않는다. 기존 approved directory를 재사용하고 folder meta를 만들지 않는다.

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
Input artifacts  = MANDATORY_CONNECTOR_TREE + MANDATORY_ROUTE_MASK_LOOKUP + SITE_RESERVATIONS + BIOME_PATCHES
Output artifact  = HORIZONTAL_BACKBONE_PLAN
Pass ID          = PASS_ROUTE
RNG stream       = none consumed in this Task
Tree edge count  = 6
Segment count    = 6
```

이번 Task의 RNG consumption은 exact `0`이다. router는 deterministic cost order로 horizontal sector candidates를 선택하며 vertical gateway를 배치하지 않는다.

## Horizontal Cell / Segment Contract

`HorizontalBackboneSegmentId`는 `public readonly struct`, `IEquatable<HorizontalBackboneSegmentId>`, `IComparable<HorizontalBackboneSegmentId>`다.

```text
string Value
bool IsValid
HorizontalBackboneSegmentId(string value)
bool TryCreate(string value, out HorizontalBackboneSegmentId result)
```

- grammar exact `^HSEG_[0-9]{2}_[A-Z0-9_]+$`; default invalid.
- segment ID is `HSEG_` + two-digit tree-edge order + `_` + source edge ID suffix.
- equality/order/hash는 deterministic이며 culture/time/process randomized hash에 의존하지 않는다.

`HorizontalBackboneRouteCell` immutable properties:

```text
SectorCoord Coord
int Ordinal
bool OpensLeft
bool OpensRight
bool IsEndpoint
bool IsReserved
bool RequiresVerticalGateway
int StepCost
```

- every non-placeholder route cell has `OpensLeft == true` and `OpensRight == true`.
- endpoint cells may be terminal approach sectors.
- reserved site footprint cells may only appear as endpoint adapters, never as middle horizontal run cells.
- `RequiresVerticalGateway` marks unresolved row transition anchor only; it does not open U/D.

`HorizontalBackboneSegment` immutable properties:

```text
HorizontalBackboneSegmentId SegmentId
MandatoryConnectorEdgeId SourceTreeEdgeId
MandatoryRouteTerminalId FromTerminalId
MandatoryRouteTerminalId ToTerminalId
IReadOnlyList<HorizontalBackboneRouteCell> Cells
SectorCoord FromApproachSector
SectorCoord ToApproachSector
bool IsSameRow
bool RequiresVerticalGateway
int HorizontalDistance
int TotalCost
```

- same-row segment cells include every sector from min X to max X on one row, inclusive.
- different-row segment records horizontal legs to deterministic gateway anchor candidates but does not connect vertical movement.
- cells are sorted by ordinal and copied read-only.
- duplicate sector within one segment is invalid except same endpoint identity.

## Router API

```text
public sealed class HorizontalBackboneRouter

HorizontalBackboneBuildResult Build(
    MandatoryConnectorTree connectorTree,
    MandatoryRouteMaskLookup routeMaskLookup,
    SiteReservationSnapshot siteSnapshot,
    BiomePatchValidationPublication biomePublication)
```

checked-in public API shape가 다르면 existing typed property name을 사용하되 의미를 바꾸지 않는다. router는 Registry/root/RNG/clock/filesystem/CSV/Unity lifecycle에서 자체 조회하지 않는다.

## Cost / Routing Rules

Cell cost follows MAP05 roadmap at horizontal-candidate granularity:

```text
1  same biome normal cell
2  biome boundary or different biome allowed transition
4  own CorePatch buffer or neutral protected-adjacent cell
8  other site buffer-adjacent cell
∞  other reservation footprint / inactive outside / world outside
```

For this Task:

- same-row tree edge must produce one straight horizontal run if all middle cells are allowed.
- different-row tree edge must choose deterministic horizontal legs on each endpoint row toward a candidate gateway column.
- gateway column choice uses checked total leg cost, then shorter total horizontal distance, then lower gateway X, then source tree edge ID ordinal.
- no U/D open mask, no Type2/Type3 record, no vertical pair reservation.
- no segment may pass through another reservation footprint.
- no middle cell may be outside the 13x13 world.
- segment may touch source/target approach sectors and preserve terminal identity.
- unresolved vertical gateway count is diagnostics only and must be `>0` for any different-row connector.

## Plan / Diagnostics / Result

`HorizontalBackbonePlan` immutable properties/API:

```text
MandatoryConnectorTree SourceConnectorTree
MandatoryRouteMaskLookup SourceRouteMaskLookup
SiteReservationSnapshot SourceSiteSnapshot
BiomePatchValidationPublication SourceBiomePublication
IReadOnlyList<HorizontalBackboneSegment> Segments
int SegmentCount
int TotalHorizontalCellCount
int SameRowSegmentCount
int GatewayPendingSegmentCount
int TotalCost
bool TryGetSegment(HorizontalBackboneSegmentId id, out HorizontalBackboneSegment segment)
IReadOnlyList<HorizontalBackboneSegment> GetSegmentsForTerminal(MandatoryRouteTerminalId terminalId)
```

Expected starter counts are observed from actual connector tree and must be recorded exactly. Hard invariants:

```text
tree edges = 6
segments = 6
all segments L/R-preserving
U/D opened = 0
route graph edges = 0
generated CSV rows = 0
RNG/mutation = 0/0
```

`HorizontalBackboneBuildError` codes exact stable order:

```text
MissingInput
InvalidConnectorTree
InvalidRouteMaskLookup
InvalidSiteSnapshot
InvalidBiomePublication
SegmentCountMismatch
InvalidSegmentIdentity
InvalidHorizontalRun
ForbiddenReservationIntrusion
WorldBoundsViolation
UnsupportedVerticalConnection
SourceMutationDetected
```

errors sort/dedupe: code, first/second ID ordinal, sector index, message ordinal.

`HorizontalBackboneDiagnostics` immutable fields:

```text
int TreeEdgeCount
int SegmentCount
int SameRowSegmentCount
int GatewayPendingSegmentCount
int TotalHorizontalCellCount
int ReservedEndpointCellCount
int ForbiddenReservedMiddleCellCount
int WorldBoundsViolationCount
int OpenUpDownCount
int RouteGraphEdgeCount
int GeneratedCsvRowCount
int RngDrawCount
int SourceMutationCount
```

Result status:

```text
Completed    plan + diagnostics, errors 0, retry false
InvalidInput plan null, errors >=1, retry false
```

## Prior Negative Audit Transition

MAP05_02/MAP05_03 tests may still forbid MAP05_04 output symbols. In this Task, update only those negative symbol lists:

Allowed after MAP05_04:

```text
HorizontalBackboneSegmentId
HorizontalBackboneRouteCell
HorizontalBackboneSegment
HorizontalBackbonePlan
HorizontalBackboneBuildError
HorizontalBackboneDiagnostics
HorizontalBackboneBuildResult
HorizontalBackboneRouter
```

Still forbidden as MAP05_05+:

```text
VerticalGatewayPlanner
VerticalGatewayPlan
UpDownConflict
MandatoryRouteLoop
MandatoryRouteGraph
MandatoryRouteValidator
MandatoryRouteOverlay
MandatoryRoutePass
GeneratedWorldEdge
SectorCell.RouteMaskId writer
```

Do not delete symbol audit cases or reduce case counts. Update assertion names/messages to say MAP05_05+ when needed.

## Determinism / Immutability

- same logical input, shuffled caller-visible exposure, fresh/reused router, `en-US`/`tr-TR`, thread/time 변화에서 exact same segment order/cells/diagnostics.
- source terminal/tree/mask/site/biome artifacts defensive immutable observable state를 유지한다.
- RNG method calls/raw draws exact `0`.
- static cache/current set, filesystem, Unity object state, current culture ordering을 사용하지 않는다.

## Scope Boundary / DO NOT

- Type2/3 gateway placement 금지 — MAP05_05
- U/D conflict/loop 금지 — MAP05_06~07
- `MandatoryRouteGraph`, `SectorCell.RouteMaskId`, generated edges/CSV 금지 — MAP05_08
- final route validator/overlay/batch/root/adapter 금지 — MAP05_09~11
- Type0 optional region, microchunk, tile reachability, SpecialMap assembly 금지
- existing production/meta/asmdef/CSV/Scene/Prefab 수정 금지
- test skip/ignore/assertion 삭제, Git operation 금지

## Required Tests

`HorizontalBackboneRouterTests.cs` actual NUnit cases 최소 `132`개다.

minimum groups:

- segment ID valid/invalid/default/equality/order/hash/culture
- route cell immutability, L/R invariant, endpoint/reserved/gateway flags
- same-row direct inclusive horizontal run
- different-row horizontal leg/gateway-pending plan without U/D opens
- deterministic gateway column tie-break
- reservation footprint middle-cell rejection
- world bounds rejection
- cost ordering exact 1/2/4/8/∞ behavior
- plan lookup by ID and terminal adjacency
- exact 6 source tree edges -> 6 segments
- no route graph, no generated CSV, no Type2/3 assignment
- source reference identity preservation and source mutation isolation
- shuffled/culture/thread/fresh-reused determinism
- RNG/file/time/UnityEditor/static mutable dependency audit
- prior negative audits allow MAP05_04 symbols but still forbid MAP05_05+ symbols

Actually run:

```text
HorizontalBackboneRouterTests           >=132 PASS
MandatoryConnectorTreeBuilderTests      129/129 PASS
MandatoryRouteMaskLookupBuilderTests    127/127 PASS
MandatoryTerminalBuilderTests           120/120 PASS
SiteReservationValidatorTests           268/268 PASS
BiomePatchValidatorTests                196/196 PASS
Map04ExitTests                          110/110 PASS
Actually executed total                 >=1082 PASS
failed/skipped                            0/0
```

large suites discovery-only under reduced profile:

```text
Game.Map targeted discovery >=5873
Full EditMode discovery      >=5984
```

forced refresh/compile/Console/relevant warning `0/0/0`.

## Asset / Meta / Change Gate

clean baseline:

```text
Authoring CSV/meta = 50/50
Assets meta = 3179
accepted legacy Editor folder meta = 6/6
duplicate GUID groups = 0
```

completion:

```text
new Runtime production C# = 8
new Runtime test C# = 1
new matching cs.meta = 9
modified existing test C# = 2
final Assets meta = 3188
task-marker 이후 exact Assets changes = 20
existing production modifications = 0
unexpected Assets changes = 0
new directory/folder meta = 0
```

new meta는 `fileFormatVersion: 2`, unique lowercase 32-hex GUID다. existing test `.meta`, Authoring CSV/meta, progress test Scene, accepted legacy meta를 바이트 보존한다.

## Failure Policy

- contract/test/compile/meta/change-scope 한 조건이라도 불일치하면 `STATUS: FAIL`.
- Unity/Test Runner 접근이 없어 actual compile/tests를 실행하지 못하면 `STATUS: BLOCKED`.
- FAIL/BLOCKED를 production 수정, local repair, assertion 삭제, later Task 구현으로 해결하지 않는다.
- PASS가 아니면 finalize하지 않고 MAP05_05를 열지 않는다.

## Result / Completion

Result: `REPORTS/MAP05_04_IMPLEMENT_HORIZONTAL_BACKBONE_ROUTER_RESULT.md`.

Result는 `<=150 lines`로 아래를 기록한다.

```text
TASK / STATUS / SUMMARY
PATCH APPLY / READ / CREATED / MODIFIED / PREEXISTING_IDENTICAL
SEGMENTS / HORIZONTAL RUNS / GATEWAY PENDING / COST MODEL
PRIOR AUDIT TRANSITION / SOURCE IDENTITY / DETERMINISM / IMMUTABILITY
TEST / UNITY / ASSET META / CHANGE SCOPE / OWNERSHIP AUDIT
OUT_OF_SCOPE_FINDINGS / DONE CONDITIONS / NEXT / Recommended Commit
```

PASS일 때만 MAP05_04 COMPLETE, Current Task NONE으로 finalize한다. `MAP05_05_IMPLEMENT_VERTICAL_GATEWAY_PLANNER`는 LOCKED로 유지하고 자동 시작하지 않는다.

Recommended Commit: `feat(map): add horizontal mandatory backbone router`
