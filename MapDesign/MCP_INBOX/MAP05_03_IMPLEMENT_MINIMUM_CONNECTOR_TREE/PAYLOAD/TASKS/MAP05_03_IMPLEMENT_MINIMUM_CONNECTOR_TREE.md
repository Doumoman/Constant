# MAP05_03 — Implement Minimum Connector Tree

```yaml
status_control:
  task_key: MAP05_03_IMPLEMENT_MINIMUM_CONNECTOR_TREE
  result_file: REPORTS/MAP05_03_IMPLEMENT_MINIMUM_CONNECTOR_TREE_RESULT.md
```

## TASK TYPE

```text
RUNTIME IMMUTABLE P03 MANDATORY CONNECTOR TREE CANDIDATE + DETERMINISTIC BUILDER + EDITMODE TESTS
```

## Objective

MAP05_01의 exact 7-terminal set과 MAP05_02의 exact Type1/2/3 mask lookup을 받아, mandatory route generator가 실제 sector route를 찾기 전에 사용할 minimum connector tree 후보를 만든다.

이번 Task의 output은 terminal-to-terminal abstract tree뿐이다.

```text
nodes = 7 mandatory terminals
edges = 6 undirected connector candidate edges
connected = true
acyclic = true
all terminals covered = true
```

sector path, horizontal run, Type2/3 gateway placement, U/D conflict resolution, loops, final route graph, `SectorCell.RouteMaskId`, generated CSV, validator, overlay, root/retry는 구현하지 않는다.

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
12. `REPORTS/MAP05_02_IMPLEMENT_ROUTE_MASK_LOOKUP_RESULT.md`

prior Result exact gate:

```text
TASK: MAP05_02_IMPLEMENT_ROUTE_MASK_LOOKUP
STATUS: PASS
ROUTE MASK IDS: 3 exact
TEST: MandatoryRouteMaskLookupBuilderTests 127/127 PASS
UNITY: compile/Console/warnings 0/0/0
ASSET META: 3161 -> 3170
DONE CONDITIONS: PASS
SHA-256: c053a0bfaa35967e2fe0afd0b3416f7e090c0238626f4e8ed632a3afd858b067
```

이 별도 patch가 적용된 뒤에만 MAP05_03을 실행한다.

## Map Package Reference

exact installed Map Package v1.0이 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
01_FIXED_SPEC/06_ROUTE_TOPOLOGY_CONSTRAINTS.md
02_PHASE_ROADMAP/MAP05_ROUTE_123_GENERATOR.md
04_CSV_STARTER/generation_profiles.csv
```

reference는 tree/cost/domain 확인용이다. installed Authoring CSV를 다시 읽거나 파싱하지 않는다. source of truth는 MAP05_01 terminal set과 MAP05_02 mask lookup이다.

## READ ALLOWLIST

### Existing P03 terminals

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteTerminalId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteTerminalKind.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteTerminal.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteTerminalSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryTerminalBuildResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryTerminalBuilder.cs
```

### Existing P03 route mask lookup

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteOpenMask.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteMaskId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteMaskKind.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteMaskRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteMaskLookup.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteMaskLookupBuildResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilder.cs
```

### Existing coordinate/domain context

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationEnums.cs
```

### Focused tests / assemblies

```text
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
- MAP05_04 이후 Task body
- unrelated production/test C# body
- Legacy/Stage/P6/P11 generator body
- Scene/Prefab YAML

## WRITE ALLOWLIST

### 신규 Runtime production C# — exact 8

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryConnectorEdgeId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryConnectorEdgeCost.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryConnectorCandidateEdge.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryConnectorTree.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryConnectorTreeBuildError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryConnectorTreeDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryConnectorTreeBuildResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryConnectorTreeBuilder.cs
```

### 신규 Runtime EditMode test — exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryConnectorTreeBuilderTests.cs
```

신규 C# 9개와 matching `.cs.meta` 9개, Result 1만 생성한다. existing Assets/CSV/meta/asmdef/Scene/Prefab를 수정하지 않는다. 기존 approved directory를 재사용하고 folder meta를 만들지 않는다.

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
Input artifacts  = MANDATORY_TERMINALS + MANDATORY_ROUTE_MASK_LOOKUP
Output artifact  = MANDATORY_CONNECTOR_TREE
Pass ID          = PASS_ROUTE
RNG stream       = none consumed in this Task
Node count       = 7
Tree edge count  = 6
```

이번 Task의 RNG consumption은 exact `0`이다. tree는 terminal approach sectors에서 파생되며 sector path나 route mask assignment를 하지 않는다.

## Edge ID / Cost Contract

`MandatoryConnectorEdgeId`는 `public readonly struct`, `IEquatable<MandatoryConnectorEdgeId>`, `IComparable<MandatoryConnectorEdgeId>`다.

```text
string Value
bool IsValid
MandatoryConnectorEdgeId(string value)
bool TryCreate(string value, out MandatoryConnectorEdgeId result)
```

- grammar exact `^EDGE_[0-9]{2}_[A-Z0-9_]+__TO__[A-Z0-9_]+$`; default invalid.
- edge ID는 endpoint terminal IDs를 ordinal ascending canonical order로 사용한다.
- equality/order/hash는 deterministic이며 culture/time/process randomized hash에 의존하지 않는다.

`MandatoryConnectorEdgeCost` immutable value:

```text
int ManhattanDistance
int ReservationOrderSpread
int KindPenalty
int SharedApproachPenalty
int TotalCost
```

checked arithmetic으로 계산한다.

```text
TotalCost =
  ManhattanDistance * 1000
+ ReservationOrderSpread * 10
+ KindPenalty
+ SharedApproachPenalty
```

cost rules:

- `ManhattanDistance = abs(a.ApproachSector.X-b.ApproachSector.X)+abs(a.ApproachSector.Y-b.ApproachSector.Y)`
- `ReservationOrderSpread = abs(a.TerminalOrder-b.TerminalOrder)`
- `KindPenalty = 0` for Start-involved edge, `3` for SiteEntry-to-SiteEntry edge
- `SharedApproachPenalty = 100000` when two different terminals share the same approach sector
- negative/overflow cost is invalid.

이 cost는 tree 후보 선정을 위한 abstract cost일 뿐 실제 routing cost가 아니다. MAP05_04 이후 router는 sector-level cost를 별도로 계산한다.

## Candidate Edge / Tree Contract

`MandatoryConnectorCandidateEdge` immutable properties:

```text
MandatoryConnectorEdgeId EdgeId
MandatoryRouteTerminalId FromTerminalId
MandatoryRouteTerminalId ToTerminalId
int FromTerminalOrder
int ToTerminalOrder
SectorCoord FromApproachSector
SectorCoord ToApproachSector
MandatoryConnectorEdgeCost Cost
bool IsTreeEdge
```

- endpoint order is canonical: lower terminal order first, then terminal ID ordinal.
- self-loop, duplicate unordered pair, invalid endpoint ID, missing terminal, invalid coordinate를 거부한다.
- `IsTreeEdge`는 complete graph candidate와 final tree edge를 같은 type으로 표현하기 위한 immutable flag다.

`MandatoryConnectorTree` immutable properties/API:

```text
MandatoryRouteTerminalSet SourceTerminalSet
MandatoryRouteMaskLookup SourceRouteMaskLookup
IReadOnlyList<MandatoryConnectorCandidateEdge> CandidateEdges
IReadOnlyList<MandatoryConnectorCandidateEdge> TreeEdges
int NodeCount
int CandidateEdgeCount
int TreeEdgeCount
int TotalTreeCost
bool IsConnected
bool IsAcyclic
bool CoversAllTerminals
bool TryGetTreeEdge(MandatoryConnectorEdgeId id, out MandatoryConnectorCandidateEdge edge)
IReadOnlyList<MandatoryConnectorCandidateEdge> GetTreeEdgesForTerminal(MandatoryRouteTerminalId terminalId)
```

expected candidate counts:

```text
complete graph candidates = 21
tree edges = 6
nodes = 7
```

- source terminal set and route mask lookup reference identities를 보존하고 mutate/clone하지 않는다.
- candidate edges sorted by cost total, endpoint orders, endpoint IDs, edge ID.
- final tree selected by deterministic Kruskal over candidate edges.
- tree edge list sorted by selection order, then edge ID ordinal.
- no terminal pair path, no sector intermediate nodes, no route type/mask assignment.

## Builder API

```text
public sealed class MandatoryConnectorTreeBuilder

MandatoryConnectorTreeBuildResult Build(
    MandatoryRouteTerminalSet terminalSet,
    MandatoryRouteMaskLookup routeMaskLookup)
```

checked-in public API shape가 다르면 existing typed property name을 사용하되 의미를 바꾸지 않는다. builder는 Registry/root/RNG/clock/filesystem/CSV/Unity lifecycle에서 자체 조회하지 않는다.

## Structural Preflight

output allocation 전에 가능한 오류를 accumulated, ordinal sorted, deduped한다.

- inputs non-null
- terminal set exact `7 = 1 Start + 6 SiteEntry`
- terminal orders exact `0..6`
- terminal IDs unique and valid
- every terminal required and return-path-required
- every terminal approach sector world-bound
- route mask lookup exact `3 = Type1/Type2/Type3`
- route mask lookup has exact T1/T2/T3 open mask matrix
- complete graph unordered pair count exact `21`
- candidate costs checked, non-negative, deterministic
- tree edge count exact `6`
- tree connected, acyclic, all terminals covered
- no self-loop/duplicate edge/duplicate ID

invalid input은 RNG/file/mutation `0`, tree null, diagnostics null-or-empty, sorted errors `>=1`, retry false다. constructor exception text/stack/path/culture를 message에 넣지 않는다.

## Exact Tree Build Order

```text
1. preflight terminal set and route mask lookup (RNG 0)
2. copy terminal references in TerminalOrder -> TerminalId ordinal order
3. enumerate all unordered terminal pairs into 21 candidate edges
4. calculate abstract deterministic edge cost
5. stable sort candidates by cost and endpoint tie-breakers
6. run deterministic Kruskal union-find until 6 edges selected
7. validate connected/acyclic/all-covered invariants
8. atomically create MandatoryConnectorTree
```

No shuffle, weight, random tie-break, nearest-neighbor chain, terminal list order mutation, coordinate sort outside stated tie-breakers, or route path search를 사용하지 않는다.

## Diagnostics / Result

`MandatoryConnectorTreeBuildError` fields:

```text
MandatoryConnectorTreeBuildErrorCode Code
string FirstId
string SecondId
int SectorIndex
string Message
```

codes exact stable order:

```text
MissingInput
InvalidTerminalSet
InvalidRouteMaskLookup
TerminalCountMismatch
TerminalIdentityMismatch
CandidateEdgeCountMismatch
DuplicateEdgeIdentity
InvalidEdgeCost
TreeEdgeCountMismatch
DisconnectedTree
CyclicTree
MissingTerminalCoverage
```

errors sort/dedupe: code, first/second ID ordinal, sector index, message ordinal.

diagnostics immutable fields:

```text
int TerminalCount
int StartTerminalCount
int SiteEntryTerminalCount
int RouteMaskCount
int CandidateEdgeCount
int TreeEdgeCount
int TotalTreeCost
int ConnectedComponentCount
int CoveredTerminalCount
int SharedApproachCandidateCount
int RngDrawCount
int SourceMutationCount
```

starter expected:

```text
terminals = 7 = 1 Start + 6 SiteEntry
route masks = 3
candidate edges = 21
tree edges = 6
connected components = 1
covered terminals = 7
RNG/mutation = 0/0
```

Result status:

```text
Completed    tree + diagnostics, errors 0, retry false
InvalidInput tree null, errors >=1, retry false
```

이 Task에는 route-generation rejection/retry status가 없다. routing 실패와 `route_retry_max=200`은 MAP05_04 이후 P03 route pass 재시도 범위다.

## Determinism / Immutability

- same logical input, shuffled caller-visible exposure, fresh/reused builder, `en-US`/`tr-TR`, thread/time 변화에서 exact same candidate order/tree edges/diagnostics.
- source terminal set and route mask lookup defensive immutable observable state를 유지한다.
- RNG method calls/raw draws exact `0`.
- static cache/current set, filesystem, Unity object state, current culture ordering을 사용하지 않는다.
- MAP05_01 terminal models and MAP05_02 route mask lookup을 수정하지 않는다.

## Scope Boundary / DO NOT

- horizontal router/Type1 assignment 금지 — MAP05_04
- Type2/3 gateway/U-D conflict/loop 금지 — MAP05_05~07
- `MandatoryRouteGraph`, `SectorCell.RouteMaskId`, generated edges/CSV 금지 — MAP05_08
- final route validator/overlay/batch/root/adapter 금지 — MAP05_09~11
- Type0 optional region, microchunk, tile reachability, SpecialMap assembly 금지
- synthetic cap/dead-end/Type0 terminal/extra beacon 금지
- existing production/test/meta/asmdef/CSV/Scene/Prefab 수정 금지
- test skip/ignore/assertion 완화, Git operation 금지

## Required Tests

`MandatoryConnectorTreeBuilderTests.cs` actual NUnit cases 최소 `118`개다.

minimum groups:

- edge ID valid/invalid/default/equality/order/hash/culture
- edge cost exact checked arithmetic, overflow and negative rejection
- candidate edge canonical endpoint order and self-loop rejection
- exact starter success: 7 nodes, 21 candidate edges, 6 tree edges
- tree connected/acyclic/all-covered invariants
- deterministic Kruskal tie-break selection independent of input exposure order
- duplicate terminal ID/order, missing Start, missing SiteEntry, invalid approach rejection
- invalid route mask lookup rejection
- candidate duplicate unordered pair and duplicate edge ID rejection
- shared approach penalty representable and deterministic
- source reference identity preservation and source mutation isolation
- lookup by edge ID and terminal adjacency returns read-only stable views
- shuffled/culture/thread/fresh-reused determinism
- RNG/file/time/UnityEditor/static mutable dependency audit
- no horizontal router/gateway/graph/CSV/root/later-task production symbol

Actually run:

```text
MandatoryConnectorTreeBuilderTests      >=118 PASS
MandatoryRouteMaskLookupBuilderTests    127/127 PASS
MandatoryTerminalBuilderTests           120/120 PASS
SiteReservationValidatorTests           268/268 PASS
BiomePatchValidatorTests                196/196 PASS
Map04ExitTests                          110/110 PASS
Actually executed total                 >=939 PASS
failed/skipped                            0/0
```

large suites discovery-only under reduced profile:

```text
Game.Map targeted discovery >=5730
Full EditMode discovery      >=5841
```

forced refresh/compile/Console/relevant warning `0/0/0`.

## Asset / Meta / Change Gate

clean baseline:

```text
Authoring CSV/meta = 50/50
Assets meta = 3170
accepted legacy Editor folder meta = 6/6
duplicate GUID groups = 0
```

completion:

```text
new Runtime production C# = 8
new Runtime test C# = 1
new matching cs.meta = 9
final Assets meta = 3179
task-marker 이후 exact Assets changes = 18
existing Assets modifications = 0
unexpected Assets changes = 0
new directory/folder meta = 0
```

new meta는 `fileFormatVersion: 2`, unique lowercase 32-hex GUID다. Authoring CSV/meta, progress test Scene, accepted legacy meta를 바이트 보존한다.

## Failure Policy

- contract/test/compile/meta/change-scope 한 조건이라도 불일치하면 `STATUS: FAIL`.
- Unity/Test Runner 접근이 없어 actual compile/tests를 실행하지 못하면 `STATUS: BLOCKED`.
- FAIL/BLOCKED를 source 수정, local repair, assertion 완화, later Task 구현으로 해결하지 않는다.
- PASS가 아니면 finalize하지 않고 MAP05_04를 열지 않는다.

## Result / Completion

Result: `REPORTS/MAP05_03_IMPLEMENT_MINIMUM_CONNECTOR_TREE_RESULT.md`.

Result는 `<=150 lines`로 아래를 기록한다.

```text
TASK / STATUS / SUMMARY
PATCH APPLY / READ / CREATED / MODIFIED / PREEXISTING_IDENTICAL
TREE NODES / CANDIDATE EDGES / TREE EDGES / COST MODEL
SOURCE IDENTITY / DETERMINISM / IMMUTABILITY
TEST / UNITY / ASSET META / CHANGE SCOPE / OWNERSHIP AUDIT
OUT_OF_SCOPE_FINDINGS / DONE CONDITIONS / NEXT / Recommended Commit
```

PASS일 때만 MAP05_03 COMPLETE, Current Task NONE으로 finalize한다. `MAP05_04_IMPLEMENT_HORIZONTAL_BACKBONE_ROUTER`은 LOCKED로 유지하고 자동 시작하지 않는다.

Recommended Commit: `feat(map): build mandatory connector tree candidate`
