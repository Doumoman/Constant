```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP19_02_BUILD_TILE_MOVEMENT_GRAPH
  task_file: TASKS/MAP19_02_BUILD_TILE_MOVEMENT_GRAPH.md
  requires_current_task: NONE
  requires_completed_task: MAP19_01_LOCK_TRAVERSAL_PROFILE_AND_RULE_REGISTRY
  requires_result:
    path: REPORTS/MAP19_01_LOCK_TRAVERSAL_PROFILE_AND_RULE_REGISTRY_RESULT.md
    status: PASS
    sha256: 713918e2f703c849b59c312143fe2d422e7ba89c100c32883fed172b60eeaec4
  requires_installed_task:
    path: TASKS/MAP19_01_LOCK_TRAVERSAL_PROFILE_AND_RULE_REGISTRY.md
    sha256: d620b805bb201be2a049b211381d1ba4fa482d150f0689fcfc03280da17f2101
  sets_current_task: MAP19_02_BUILD_TILE_MOVEMENT_GRAPH
```

# MAP19_02 - Build Tile Movement Graph

```text
TASK: MAP19_02_BUILD_TILE_MOVEMENT_GRAPH
PHASE: MAP19 - Traversal Validation / Seed QA Preparation
STATUS: CURRENT
NEXT: MAP19_03_IMPLEMENT_NAKED_BFS_AND_COMPLETION_SEARCH
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP19_01에서 잠근 Traversal Profile과 Validation Rule Registry를 사용해, baked/generated terrain cell을 **pure-data tile movement graph**로 변환한다.

이번 Task는 그래프를 만든다.  
하지만 그래프를 탐색해서 완주 가능성을 판정하지 않는다. BFS, completion search, naked route proof, seed batch QA는 전부 `MAP19_03` 이후 책임이다.

이번 Task의 책임:

```text
1. Stand, Climb, Bounce, IntersectorSocket node를 stable graph node로 만든다.
2. Walk, Jump, Drop, Climb, Slide, Bounce movement edge를 MAP19_01 profile/rule registry 기준으로 만든다.
3. Sector/Slice boundary를 넘는 intersector socket link를 pure-data edge로 연결한다.
4. node/edge/provenance/dangling-reference/duplicate-id/digest 검증을 수행한다.
5. MAP19_03 BFS가 소비할 tile movement graph handoff surface를 게시한다.
```

금지:

```text
BFS or completion search
naked traversal proof
route reachability approval
cluster recovery/density validation execution
event removal validation execution
worst-case scenario validation
distance/revisit/pacing measurement
failure bundle, screenshot, CSV, headless runner creation
seed batch run, production seed approval
PlayerController / Rigidbody / Collider / Physics2D behavior change
actual movement tuning
Physics2D query or simulation
NavMesh or pathfinding setup
GameObject / Prefab instantiate, enable, disable, destroy
Unity Tilemap / Collider / Rigidbody creation or write
Tilemap.SetTile / SetTiles / SetTilesBlock / ClearAllTiles / CompressBounds
Scene / Prefab / Tilemap mutation
Addressables / Resources / AssetDatabase load
camera, streaming, save integration
optimization rewrite or broad refactor
shared fixture consolidation
MAP19_03 unlock or execution
```

## 1. 사용자 보고 의무

Result의 첫 두 섹션은 반드시 아래 이름으로 작성한다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

`## User-Facing Implementation Report`에는 한국어로 다음을 설명한다.

```text
이번 Task가 추가한 기능
tile movement graph가 BFS/완주 검증과 어떻게 다른지
어떤 node kind와 movement edge kind를 만들었는지
MAP19_01 Traversal Profile/Rule Registry 값을 어떻게 사용했는지
intersector socket link가 어떤 경계 연결을 표현하는지
MAP19_03에 넘기는 graph handoff surface
중복 코드나 하드코딩 후보를 발견했는지
회귀 테스트를 돌리지 않았는지, 돌렸다면 실제 트리거가 무엇이었는지
```

`## Responsibility and Added Scripts`에는 표로 다음을 작성한다.

| Script or file | Added or changed responsibility | Explicit non-ownership |
|---|---|---|
| 실제 파일 경로 | 이 파일이 맡은 책임 | 이 파일이 절대 하지 않는 일 |

Result에서 "테스트 PASS"만 쓰고 스크립트 책임 설명을 생략하면 이 Task는 `FAIL`이다.

또한 Result에는 아래 섹션을 포함한다.

```text
## Tile Movement Graph Summary
## No BFS Seed Regression or Physics Boundary Notes
```

## 2. 선행조건

작업 전에 다음을 확인한다.

```text
MAP19_01 Result exists
MAP19_01 Result STATUS: PASS
MAP19_01 Result SHA-256:
713918e2f703c849b59c312143fe2d422e7ba89c100c32883fed172b60eeaec4

MAP19_01 installed task SHA-256:
d620b805bb201be2a049b211381d1ba4fa482d150f0689fcfc03280da17f2101

MAP19_01 traversal profile digest:
12415531bfa37bc8db427f672fefe47c92fee65bf61fccd94887af09669c2d68

MAP19_01 rule registry digest:
556f6885caa0baeb92073fa3410d6a8df36a2000626f1737de5a1bcc65f3e747

MAP19_01 movement-envelope matrix digest:
f65ab5389cdbedf57b8d0084c0abc1386d3f30700cdf220fbeaced825a5e521f

MAP19_02 incoming handoff digest:
09f54b43e12dd6cdcf102d84f5d70b5b78ba3ff29954d721a94fbe5bf5197963

MAP19_01 capability groups / capability values:
7 / 18

MAP19_01 movement kinds / envelope kinds / validation rules:
6 / 7 / 12

Current Task before apply: NONE
MAP19_01: COMPLETE
MAP19_02: LOCKED before apply
MAP19_03: LOCKED
unrelated staged files: 0
```

선행 Result나 installed Task SHA가 다르면 임의로 맞추지 않는다. `BLOCKED`로 멈추고 실제 SHA를 Result에 기록한다.

## 3. 입력 계약

다음 산출물을 읽어 사용한다. 실제 타입명은 프로젝트의 현재 public API를 따른다.

```text
GeneratedTraversalProfile
GeneratedTraversalRuleRegistry
GeneratedTraversalProfileRuleLock
TraversalMovementKind
TraversalEnvelopeKind
GeneratedSlicePartition
Generated96CellSlice
GeneratedSliceCell
GeneratedSectorCanvas
GeneratedFinalRouteValidation
GeneratedPopulationExitAuditSurface
BakingCanonicalDigest
```

프로젝트에 위 이름과 정확히 일치하는 타입이 없으면, 동일 semantic owner를 가진 현재 public 타입을 사용한다. 타입명을 맞추기 위해 MAP16/MAP18/MAP19_01 기존 파일을 대규모 변경하지 않는다.

Read-only MCP references:

```text
MapDesign/MCP/REPORTS/MAP19_01_LOCK_TRAVERSAL_PROFILE_AND_RULE_REGISTRY_RESULT.md
MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST_V2.md
MapDesign/MCP/TASKS/MAP19_01_LOCK_TRAVERSAL_PROFILE_AND_RULE_REGISTRY.md
```

MCP report/task reads are allowed for audit evidence. Production code must not use `System.IO` to read these reports.

기준:

```text
required node kinds: Stand, Climb, Bounce, IntersectorSocket
required movement edge kinds: Walk, Jump, Drop, Climb, Slide, Bounce
required boundary link kind: IntersectorSocketLink
required provenance fields: sector, slice, cell, source owner, source digest
required MAP19_01 profile/rule/matrix digest passthrough: 3/3
BFS runs in this task: 0
completion searches in this task: 0
seed batch runs in this task: 0
Physics2D queries in this task: 0
runtime object spawns in this task: 0
```

Generated terrain cell source나 MAP19_01 locked profile/rule registry를 확인할 수 없으면 임의 graph fixture를 production에 만들지 않는다. `BLOCKED`로 멈추고 누락 source를 보고한다.

## 4. 핵심 산출물

프로젝트 패턴에 맞게 파일과 타입을 조정할 수 있지만, 다음 semantic responsibility는 분명해야 한다.

| Required concept | Responsibility |
|---|---|
| `GeneratedTileMovementNodeKind` | Stand, Climb, Bounce, IntersectorSocket node kind를 정의한다. |
| `GeneratedTileMovementNode` | node id, kind, sector/slice/cell coordinate, surface/provenance, clearance proof를 담는다. |
| `GeneratedTileMovementEdge` | from/to node, movement kind, envelope requirement, cost, rule evidence를 담는다. |
| `GeneratedIntersectorSocketLink` | sector/slice boundary socket pair와 direction/provenance를 담는다. |
| `GeneratedTileMovementGraph` | nodes, edges, socket links, source digests, graph digest를 묶는다. |
| `GeneratedTileMovementGraphStats` | node/edge/link/count/rejection/dangling/duplicate 수치를 담는다. |
| `GeneratedTileMovementGraphFailure` | missing source, invalid node, dangling edge, unsupported movement, digest mismatch, forbidden side effect를 deterministic하게 보고한다. |
| `GeneratedTileMovementGraphBuilder` | terrain cell source와 MAP19_01 profile/rules를 사용해 pure-data graph를 만든다. |

Suggested production files:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Validation/GeneratedTileMovementGraph.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Validation/GeneratedTileMovementGraphBuilder.cs(.meta)
```

Suggested focused test file:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Validation/GeneratedTileMovementGraphBuilderTests.cs(.meta)
```

파일을 더 적게 또는 다르게 구성해도 된다. 다만 Result의 `Responsibility and Added Scripts`에서 실제 구성과 책임을 정확히 보고해야 한다.

## 5. Tile movement graph 규칙

### 5.1 Node kinds

다음 node kind를 모두 표현한다.

```text
Stand
Climb
Bounce
IntersectorSocket
```

각 node는 다음 metadata를 가진다.

```text
stable node id
node kind
sector coordinate
slice coordinate
cell coordinate
source owner
source digest
clearance/protection proof
```

Node ID는 machine path, timestamp, Unity instance ID 없이 stable lower-hex digest primitive로 만든다.

### 5.2 Edge kinds

Movement edge는 MAP19_01의 six MovementKind를 그대로 사용한다.

```text
Walk
Jump
Drop
Climb
Slide
Bounce
```

각 edge는 다음 metadata를 가진다.

```text
stable edge id
from node id
to node id
movement kind
required envelope kinds
rule ids consumed
cost
failure policy
```

Edge 생성은 local adjacency/envelope rule check까지만 수행한다.  
출발점에서 목표점까지 도달 가능한지 확인하는 search는 하지 않는다.

### 5.3 Movement-envelope compatibility

MAP19_01 matrix를 그대로 사용한다.

```text
Walk requires Floor, Clearance, Landing, Recovery
Jump requires Clearance, JumpArc, Landing, Recovery
Drop requires Clearance, DropColumn, Landing, Recovery
Climb requires Clearance, Landing, Recovery
Slide requires Floor, Clearance, Landing, Recovery
Bounce requires Clearance, JumpArc, Landing, Recovery
```

필수:

```text
MAP19_01 matrix digest reused exactly
matrix local copy mismatch count: 0
unsupported movement kind count: 0
unsupported envelope kind count: 0
rule registry threshold lookups:
hardcoded duplicate traversal thresholds in production: 0
```

### 5.4 Intersector socket links

Sector/Slice boundary 연결은 `IntersectorSocketLink`로 분리한다.

필수:

```text
socket link has from socket and to socket
socket link references existing graph nodes
socket direction is explicit
socket provenance is explicit
socket dangling references: 0
socket duplicate link ids: 0
```

Intersector link는 BFS가 아니다. 경계를 넘는 후보 edge를 표현할 뿐, route completion을 승인하지 않는다.

### 5.5 Graph integrity

필수:

```text
duplicate node ids: 0
duplicate edge ids: 0
dangling edge references: 0
edge references missing node: 0
node placed inside hard solid without valid climb/bounce/socket rule: 0
movement edge missing required envelope proof: 0
graph contains required node kind coverage
graph contains required movement edge kind coverage
```

Graph가 비어 있거나 필수 node/edge kind를 만들 수 없으면 partial graph를 게시하지 않고 `BLOCKED` 또는 `FAIL`로 멈춘다.

### 5.6 Digest and handoff

`BakingCanonicalDigest` 또는 기존 canonical digest primitive를 사용한다.

필수:

```text
LF normalization
UTF-8 no BOM
lower-hex SHA-256
stable repeat
stable reverse input order
stable culture
stable cell order
stable edge order
mutation sensitivity
```

Handoff surface는 다음 digest를 포함한다.

```text
MAP19_01 traversal profile digest
MAP19_01 rule registry digest
MAP19_01 movement-envelope matrix digest
MAP19_02 incoming handoff digest
tile movement node set digest
tile movement edge set digest
intersector socket link digest
tile movement graph digest
MAP19_03 handoff digest
```

Digest material에는 machine path, timestamp, frame count, Unity object instance ID를 넣지 않는다.

### 5.7 Failure policy

다음은 atomic failure다.

```text
missing MAP19_01 locked profile/rule registry
MAP19_01 profile digest mismatch
MAP19_01 rule registry digest mismatch
MAP19_01 matrix digest mismatch
missing generated terrain cell source
missing required node kind
missing required movement edge kind
unsupported MovementKind or EnvelopeKind
duplicate node id
duplicate edge id
dangling edge reference
dangling socket link reference
edge missing required envelope proof
hardcoded duplicate traversal threshold in production
attempted BFS/completion/seed runner/Physics2D/player tuning work
```

Failure는 owner, reason, offending key, expected/actual value를 deterministic하게 보고한다. Failure 이후 graph handoff나 MAP19_03 handoff digest가 게시되면 `FAIL`이다.

## 6. 명시적 금지 범위

이번 Task에서 다음을 하지 않는다.

```text
BFS or completion search
naked traversal proof
route reachability approval
cluster recovery/density validation execution
event removal validation execution
worst-case scenario validation
distance/revisit/pacing measurement
failure bundle, screenshot, CSV, headless runner creation
seed batch run
production seed approval
PlayerController / Rigidbody / Collider / Physics2D behavior change
actual movement tuning
actual input handling
Physics2D simulation or query
NavMesh or pathfinding setup
GameObject / Prefab instantiate, enable, disable, destroy
Unity Tilemap component write
Tilemap.SetTile / SetTiles / SetTilesBlock / ClearAllTiles / CompressBounds
TilemapCollider2D / CompositeCollider2D / Collider2D creation
Rigidbody2D creation
Scene mutation
Prefab mutation
Camera or streaming loader integration
Addressables / Resources / AssetDatabase load
System.IO file write/read for production
PlayerPrefs write/read
actual optimization rewrite
large refactor of generated terrain pipeline
shared fixture consolidation
MAP19_03 unlock or execution
PlayMode tests
legacy 19347 regression
unfiltered test runs
full regression runs
```

## 7. Focused-only 검증 정책

정상 검증은 EditMode category `MAP19_02`만 선택한다.

```text
MAP19_02 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15/MAP16/MAP17/MAP18/MAP19_01 selections: 0
legacy 19347 selections: 0
PlayMode selections: 0
unfiltered test selections: 0
full regression runs: 0
```

Compile check와 relevant Console check는 허용한다.

실제 문제가 발생해 더 넓은 검증이 필요하다고 판단되면 조용히 회귀를 돌리지 않는다.
그 경우 Result의 회귀 트리거 항목을 긍정 상태로 바꾸고, 아래 소유자 정보를 기록한 뒤 멈춘다.

```text
trigger owner:
broken invariant:
why focused proof is insufficient:
requested wider verification:
```

문제가 없다면 Result에 반드시 기록한다.

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
FULL REGRESSION RUNS: 0
```

## 8. 필수 Focused Tests

다음 test name을 그대로 포함한다. 프로젝트 test framework에 맞춰 클래스/파일은 조정할 수 있다.

```text
TileMovementGraphCreatesStandClimbBounceAndSocketNodes
TileMovementGraphBuildsWalkJumpDropClimbSlideBounceEdgesFromLockedProfile
TileMovementGraphCreatesIntersectorSocketLinksWithoutCompletionSearch
TileMovementGraphRejectsSolidBlockedClearanceMissingAndDanglingEdges
TileMovementGraphUsesRuleRegistryThresholdsWithoutDuplicatingTraversalTuning
TileMovementGraphDigestsAreStableAcrossRepeatReverseCultureAndCellOrder
TileMovementGraphFailuresAreAtomicAndReportOwnerReasonExpectedActual
TileMovementGraphDoesNotRunBfsSeedBatchPhysicsOrMutateScenes
TileMovementGraphPublishesMap19_03HandoffSurface
Map19HandoffKeepsMap19_03Locked
```

Expected focused result:

```text
mode: EditMode
category_names: [MAP19_02]
discovered: 10
executed: 10
passed: 10
failed: 0
skipped: 0
inconclusive: 0
```

If the project already has equivalent focused tests and the exact count differs, explain why in Result. Silent substitution is not allowed.

## 9. Result 필수 증거

Result에는 아래 값을 실제 수치로 기록한다.

```text
MAP19_01 Result SHA-256 required/actual:
MAP19_01 installed Task SHA-256 required/actual:
MAP19_01 traversal profile digest reused:
MAP19_01 rule registry digest reused:
MAP19_01 movement-envelope matrix digest reused:
MAP19_02 incoming handoff digest reused:

graph source sectors:
graph source slices:
graph source cells:
node kinds published:
Stand nodes:
Climb nodes:
Bounce nodes:
IntersectorSocket nodes:
total graph nodes:
duplicate node ids:

movement edge kinds published:
Walk edges:
Jump edges:
Drop edges:
Climb edges:
Slide edges:
Bounce edges:
total movement edges:
duplicate edge ids:
dangling edge references:
edge references missing node:
movement edge missing envelope proof:

intersector socket links:
socket duplicate link ids:
socket dangling references:
socket directions published:
socket provenance records:

rule registry threshold lookups:
hardcoded duplicate traversal thresholds in production:
matrix local copy mismatch count:
unsupported movement kind count:
unsupported envelope kind count:
solid-blocked node rejections:
clearance-missing edge rejections:
protected-envelope edge rejections:

tile movement node set digest lower-hex SHA-256: YES
tile movement node set digest:
tile movement edge set digest lower-hex SHA-256: YES
tile movement edge set digest:
intersector socket link digest lower-hex SHA-256: YES
intersector socket link digest:
tile movement graph digest lower-hex SHA-256: YES
tile movement graph digest:
MAP19_03 handoff digest lower-hex SHA-256: YES
MAP19_03 handoff digest:
repeat/reverse/culture/cell-order/edge-order digest mismatches: 0/0/0/0/0
mutation sensitivity probes passed:

missing profile/rule/source failure probes:
digest mismatch failure probes:
missing node/edge kind failure probes:
unsupported movement/envelope failure probes:
duplicate node/edge failure probes:
dangling edge/socket failure probes:
missing envelope proof failure probes:
hardcoded threshold duplication failure probes:
attempted BFS/completion/seed-runner/Physics2D/player-tuning failure probes:
atomic failure graph surfaces published:
atomic failure MAP19_03 handoff digests published:

tile graph nodes generated:
tile graph edges generated:
BFS/completion searches run: 0/0
naked traversal proofs run: 0
cluster recovery/density validations run: 0/0
event removal/worst-case validations run: 0/0
distance/revisit/pacing measurements run: 0
seed batch runs: 0
production seed approvals: 0
PlayerController/Rigidbody/Collider/Physics2D behavior changes: 0/0/0/0
Physics2D queries/simulations: 0/0
runtime objects spawned: 0
GameObject instantiate/enable/disable/destroy: 0/0/0/0
System.IO file write/read calls: 0/0
PlayerPrefs writes/reads: 0/0
Unity Tilemap component writes: 0
Tilemap.SetTile/SetTiles/SetTilesBlock/ClearAllTiles calls: 0/0/0/0
TilemapCollider2D/CompositeCollider2D/Collider2D creations: 0/0/0
Rigidbody2D creations: 0
NavMesh/pathfinding setup: 0/0
Scene/Prefab/Tilemap mutation: 0/0/0
Camera reads/writes: 0/0
Addressables/Resources/AssetDatabase loads: 0/0/0
optimization rewrites/broad refactors: 0/0
MAP19_03 started: NO
```

## 10. Write boundary

Allowed production source roots:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Validation/
```

Allowed test roots:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Validation/
```

Allowed MCP files:

```text
MapDesign/MCP/TASKS/MAP19_02_BUILD_TILE_MOVEMENT_GRAPH.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
MapDesign/MCP/REPORTS/MAP19_02_BUILD_TILE_MOVEMENT_GRAPH_RESULT.md
```

Allowed read-only references:

```text
MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST_V2.md
MapDesign/MCP/REPORTS/MAP19_01_LOCK_TRAVERSAL_PROFILE_AND_RULE_REGISTRY_RESULT.md
MapDesign/MCP/TASKS/MAP19_01_LOCK_TRAVERSAL_PROFILE_AND_RULE_REGISTRY.md
Assets/_Game/Map/Runtime/WorldGeneration/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/
```

Do not edit Master unless the current MCP protocol requires current-task bookkeeping there. If Master must be edited, Result must report the exact reason and changed lines.

If editing outside the write boundary seems necessary, stop as `BLOCKED`.

## 11. Completion and commit

PASS 조건:

```text
MAP19_02 focused tests PASS
compile errors 0
relevant Console errors 0
Result includes user-facing implementation report
Result includes responsibility/scripts table
Result includes tile movement graph summary
Result includes no BFS seed regression or physics boundary notes
Tile movement graph publishes Stand, Climb, Bounce and IntersectorSocket nodes
Tile movement graph publishes Walk, Jump, Drop, Climb, Slide and Bounce movement edges
Intersector socket links are represented without completion search
MAP19_01 profile/rule/matrix digests are preserved
graph integrity has duplicate/dangling/missing proof counts at 0
MAP19_03 handoff digest is created
no BFS, completion search, route approval, seed batch, failure bundle, CSV or headless runner work
no player physics/controller/tuning changes
no actual Unity object, Tilemap, Collider, Rigidbody, Physics2D, NavMesh, Scene or Prefab mutation
no optimization rewrite or broad refactor
no regression runs unless explicitly triggered and reported
MAP19_03 remains LOCKED / NOT STARTED
```

PASS일 때만 status finalize를 수행한다.

Expected final status:

```text
MAP19_02_BUILD_TILE_MOVEMENT_GRAPH: COMPLETE
MAP19_03_IMPLEMENT_NAKED_BFS_AND_COMPLETION_SEARCH: LOCKED
Current Task: NONE
```

Atomic commit subject:

```text
MAP19_02: build tile movement graph
```

Git push는 하지 않는다.

관련 없는 dirty worktree 변경은 수정하거나 stage하지 않는다.
