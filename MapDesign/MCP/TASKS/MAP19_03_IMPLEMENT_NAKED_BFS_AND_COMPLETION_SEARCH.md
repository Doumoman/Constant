```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP19_03_IMPLEMENT_NAKED_BFS_AND_COMPLETION_SEARCH
  task_file: TASKS/MAP19_03_IMPLEMENT_NAKED_BFS_AND_COMPLETION_SEARCH.md
  requires_current_task: NONE
  requires_completed_task: MAP19_02_BUILD_TILE_MOVEMENT_GRAPH
  requires_result:
    path: REPORTS/MAP19_02_BUILD_TILE_MOVEMENT_GRAPH_RESULT.md
    status: PASS
    sha256: 28961bf65b6f87e0e80a08cbf268f403fc5b343548ff26786e577d6cc57b8da9
  requires_installed_task:
    path: TASKS/MAP19_02_BUILD_TILE_MOVEMENT_GRAPH.md
    sha256: 8872ba50b80b77b67e8ae3e16ee94bc8a272d58c85041ae284d1b9a312e74978
  sets_current_task: MAP19_03_IMPLEMENT_NAKED_BFS_AND_COMPLETION_SEARCH
```

# MAP19_03 - Implement Naked BFS and Completion Search

```text
TASK: MAP19_03_IMPLEMENT_NAKED_BFS_AND_COMPLETION_SEARCH
PHASE: MAP19 - Traversal Validation / Seed QA Preparation
STATUS: CURRENT
NEXT: MAP19_04_VALIDATE_CLUSTER_RECOVERY_AND_DENSITY
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP19_02에서 만든 pure-data tile movement graph를 소비해, **도구 0 상태의 필수 접근 BFS**와 **완주 가능 상태 search**를 구현한다.

이번 Task는 처음으로 그래프를 탐색한다.  
하지만 여전히 seed batch, 전체 맵 대량 검증, cluster recovery/density, event removal, worst-case, distance/pacing 측정은 수행하지 않는다.

이번 Task의 책임:

```text
1. MAP19_02 tile movement graph를 read-only 입력으로 받는다.
2. 도구 0 상태에서 start/root node부터 mandatory target node까지 BFS reachability를 계산한다.
3. completion state를 (position, resourceMask, forge, seal, boss, specialState)로 모델링한다.
4. movement edge와 explicit state transition만 사용해 completion search를 수행한다.
5. mandatory resource, forge, seal, boss, special region state goal을 모두 만족한 proof를 게시한다.
6. failure 시 owner/reason/key/expected/actual과 가장 가까운 frontier evidence를 deterministic하게 보고한다.
7. MAP19_04 cluster recovery/density validator가 소비할 completion handoff surface를 게시한다.
```

금지:

```text
new tile movement graph generation or mutation
MAP19_01 profile/rule registry mutation
MAP19_02 graph digest rewrite
cluster recovery validation
cluster density validation
event removal validation
worst-case scenario validation
distance/revisit/pacing measurement
failure bundle, screenshot, CSV, or headless runner creation
seed batch run
production seed approval
legacy 19347 regression
prior task category selection
PlayMode selection
unfiltered test run
full regression run
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
MAP19_04 unlock or execution
```

## 1. 사용자 보고 의무

Result의 첫 두 섹션은 반드시 아래 이름으로 작성한다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

`User-Facing Implementation Report`에는 코드 세부사항보다 아래 질문에 답한다.

```text
이번 작업이 추가한 기능은 무엇인가?
도구 0 BFS와 completion search가 각각 무엇을 증명하는가?
어떤 기존 graph/profile/registry digest를 그대로 소비했는가?
BFS가 실제 PlayerController/Physics2D 판정과 어떻게 분리되어 있는가?
무엇을 일부러 하지 않았는가?
```

`Responsibility and Added Scripts`는 표로 작성한다.

```text
| Script or file | Added or changed responsibility | Explicit non-ownership |
```

보고는 막연한 "검증 추가"가 아니라 파일별 책임을 명시한다.

## 2. 선행조건

작업 시작 전에 다음을 확인한다.

```text
MapDesign/MCP/REPORTS/MAP19_02_BUILD_TILE_MOVEMENT_GRAPH_RESULT.md exists
MAP19_02 Result STATUS: PASS
MAP19_02 Result SHA-256:
28961bf65b6f87e0e80a08cbf268f403fc5b343548ff26786e577d6cc57b8da9

MapDesign/MCP/TASKS/MAP19_02_BUILD_TILE_MOVEMENT_GRAPH.md exists
MAP19_02 installed task SHA-256:
8872ba50b80b77b67e8ae3e16ee94bc8a272d58c85041ae284d1b9a312e74978

MAP19_02 tile movement graph digest:
bf6b682dd5797e6dd8cb469289ca26fc8db3f10175a99f77d6316c5afe65d063

MAP19_03 incoming handoff digest from MAP19_02:
cf13a63268b016c737d4b1d7e324dc0595b27da18dfee862dc1dfcd7e448ce43
```

Status 조건:

```text
Current Task before apply: NONE
MAP19_02_BUILD_TILE_MOVEMENT_GRAPH: COMPLETE
MAP19_03_IMPLEMENT_NAKED_BFS_AND_COMPLETION_SEARCH: LOCKED before apply
MAP19_04_VALIDATE_CLUSTER_RECOVERY_AND_DENSITY: LOCKED
```

선행 digest가 다르면 실행하지 않는다.  
줄바꿈 정규화로 바꾸거나 metadata를 자동 수정하지 않는다.

## 3. 입력 표면

가능하면 기존 public 타입과 생성물을 사용한다. 이름이 정확히 다르더라도 동일 semantic owner가 이미 있으면 그 타입을 소비한다.

필수 입력:

```text
GeneratedTraversalProfileRuleLock output from MAP19_01
GeneratedTileMovementGraph output from MAP19_02
Generated population slot/state output from MAP18 where available
Special region state/goals from MAP13/MAP18 where available
Sector/canvas/slice source provenance from MAP16/MAP17 where available
```

최소 소비해야 하는 MAP19_02 evidence:

```text
source sectors: 1
source slices: 16
source cells: 1536
node kinds: Stand, Climb, Bounce, IntersectorSocket
movement edge kinds: Walk, Jump, Drop, Climb, Slide, Bounce
total graph nodes: 1614
total movement edges: 5400
intersector socket links: 24
duplicate node ids: 0
duplicate edge ids: 0
dangling edge references: 0
socket dangling references: 0
```

이번 Task는 graph를 다시 만들지 않는다.

```text
GeneratedTileMovementGraphBuilder invocation count for production search path:
0 or read-only already-built graph reuse only
```

프로젝트 구조상 테스트 fixture에서 graph를 다시 구성해야 한다면, Result에 production과 test fixture 경계를 명확히 적는다.  
production completion search는 MAP19_02 graph contract를 입력으로 받아야 한다.

## 4. 구현 대상

권장 production 파일:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Validation/GeneratedNakedTraversalSearch.cs
Assets/_Game/Map/Runtime/WorldGeneration/Validation/GeneratedCompletionSearch.cs
```

권장 focused test 파일:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Validation/GeneratedCompletionSearchTests.cs
```

프로젝트 내 기존 naming convention이 더 적합하면 따르되, Result의 책임 표에 실제 파일명을 적는다.

## 5. Naked BFS 계약

`GeneratedNakedTraversalSearch`는 MAP19_02 graph를 입력으로 받아 도구 0 상태의 BFS 결과를 만든다.

필수 개념:

```text
NakedTraversalSearchInput
NakedTraversalSearchResult
NakedReachabilityProof
NakedReachabilityFailure
NakedFrontierEvidence
```

Naked BFS는 아래만 허용한다.

```text
start node id
target node ids
allowed movement kinds from MAP19_01/MAP19_02
edge clearance/protection proof already attached by MAP19_02
intersector socket link already attached by MAP19_02
```

Naked BFS는 아래를 사용하지 않는다.

```text
player inventory
optional item movement
runtime physics
tilemap query
collider query
scene object position
camera state
save state
RNG
```

Naked BFS의 visited key는 최소한 다음을 포함한다.

```text
node_id
```

도구 0 경계는 다음처럼 기록한다.

```text
tool_mask: 0
optional_movement_sources: 0
consumed_runtime_physics: NO
consumed_map19_02_graph_digest: bf6b682dd5797e6dd8cb469289ca26fc8db3f10175a99f77d6316c5afe65d063
```

BFS proof는 path 전체를 과하게 출력하지 않아도 된다. 그러나 다음은 필요하다.

```text
start node id
target node id
found or not found
visited node count
visited edge count
shortest edge count if found
movement kind histogram if found
frontier sample if not found
blocking reason histogram if not found
graph digest
proof digest
```

## 6. Completion Search 계약

`GeneratedCompletionSearch`는 아래 상태를 key로 사용한다.

```text
position
resourceMask
forge
seal
boss
specialState
```

권장 enum/value:

```text
position: tile movement graph node id
resourceMask: deterministic bit mask for mandatory resources
forge: NotReached | Available | Activated
seal: NotReached | Opened | Accepted
boss: NotReached | Available | Defeated
specialState: None | Entered | Resolved | Exited
```

프로젝트에 이미 더 정확한 토큰이 있으면 기존 토큰을 사용한다.  
새 상태명은 gameplay runtime 상태와 혼동되지 않도록 `Generated` 또는 `Validation` namespace 아래에 둔다.

Completion Search action은 두 종류만 허용한다.

```text
Move: MAP19_02 movement edge or intersector socket link를 따라 position 변경
StateTransition: 현재 position에 바인딩된 explicit slot/region/goal 규칙으로 resourceMask/forge/seal/boss/specialState 변경
```

금지되는 action:

```text
implicit teleport
runtime object interaction
inventory item grant not backed by generated slot
boss defeat without generated boss slot/state rule
seal acceptance without generated seal rule
special resolution without generated special state rule
terrain mutation
tile carve or forced rescue tunnel
```

Completion goal은 최소한 다음을 포함한다.

```text
all mandatory resources collected or marked reachable
required forge condition satisfied if present
required seal condition satisfied if present
required boss condition satisfied if present
required specialState condition satisfied if present
exit/goal node reachable from final state
```

실제 프로젝트에서 forge/seal/boss/special 필수가 없는 test source라면, `NotPresent` 또는 equivalent contract를 명시하고 digest에 포함한다.

## 7. Determinism and Digest

모든 collection은 canonical 정렬한다.

정렬 기준:

```text
node id ascending ordinal
edge id ascending ordinal
target id ascending ordinal
state key canonical string ascending ordinal
transition id ascending ordinal
failure key ascending ordinal
```

digest는 기존 canonical digest helper를 사용한다.

필수 digest:

```text
naked BFS input digest
naked BFS reachability proof digest
completion search input digest
completion state-space digest
completion proof digest
MAP19_04 handoff digest
```

Result에는 다음 안정성 증거를 기록한다.

```text
repeat digest mismatch count: 0
reverse target/order digest mismatch count: 0
culture digest mismatch count: 0
state transition order digest mismatch count: 0
mutation sensitivity probes passed:
```

## 8. Failure and Atomicity

실패는 deterministic하게 보고한다.

필수 failure fields:

```text
owner
reason
offending key
expected
actual
source digest
frontier evidence when applicable
```

다음 failure probe를 focused test에 포함한다.

```text
missing graph input
MAP19_02 graph digest mismatch
missing start node
missing target node
dangling completion target binding
unreachable mandatory target
state transition without matching position
duplicate state transition id
unsupported movement kind in search input
implicit/forbidden action attempt
```

Failure 이후 다음 surface가 게시되면 FAIL이다.

```text
naked BFS success proof digest
completion success proof digest
MAP19_04 handoff digest
```

원자성 증거:

```text
atomic failure success proofs published: 0
atomic failure MAP19_04 handoff digests published: 0
```

## 9. 금지 API 스캔

production code는 다음 문자열/API를 포함하지 않아야 한다.

```text
UnityEngine
UnityEditor
System.IO
Physics2D
GameObject
Transform
MonoBehaviour
Tilemap
Collider
Rigidbody
NavMesh
Scene
Prefab
Camera
Addressables
Resources
AssetDatabase
PlayerPrefs
Input
PlayerController
SetTile
SetTiles
SetTilesBlock
ClearAllTiles
CompressBounds
Instantiate
Destroy
```

테스트 파일에서 기존 fixture 생성을 위해 Unity Test Framework namespace를 사용하는 것은 허용된다.  
단, production implementation에는 위 API가 들어가면 안 된다.

## 10. Focused-only 검증 정책

정상 검증은 EditMode category `MAP19_03`만 선택한다.

```text
MAP19_03 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15/MAP16/MAP17/MAP18/MAP19_01/MAP19_02 selections: 0
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

## 11. 필수 Focused Tests

다음 test name을 그대로 포함한다. 프로젝트 test framework에 맞춰 클래스/파일은 조정할 수 있다.

```text
NakedBfsFindsToolZeroReachableRequiredNodesOnTileGraph
CompletionSearchTracksPositionResourceForgeSealBossAndSpecialState
CompletionSearchConsumesMandatoryPopulationAndSpecialGatesWithoutRuntimeObjects
CompletionSearchRejectsMissingStartExitTargetsAndDanglingGoalBindings
CompletionSearchDoesNotUsePhysicsTilemapPlayerControllerOrSeedBatch
CompletionSearchPreservesMap19_02GraphDigestsAndRuleRegistryInputs
CompletionSearchFailuresAreAtomicAndReportOwnerReasonExpectedActual
CompletionSearchDigestIsStableAcrossRepeatReverseCultureAndGoalOrder
CompletionSearchPublishesMap19_04HandoffSurface
Map19HandoffKeepsMap19_04Locked
```

Focused test category:

```text
MAP19_03
```

Required count:

```text
discovered: 10
executed: 10
passed: 10
failed: 0
```

## 12. Result 필수 기록

Result 파일:

```text
MapDesign/MCP/REPORTS/MAP19_03_IMPLEMENT_NAKED_BFS_AND_COMPLETION_SEARCH_RESULT.md
```

Result에는 아래 섹션을 반드시 포함한다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
## Naked BFS and Completion Search Summary
## No Seed Regression or Physics Boundary Notes
```

`Naked BFS and Completion Search Summary`에는 아래 값을 채운다.

```text
MAP19_02 Result SHA-256 required/actual:
MAP19_02 installed Task SHA-256 required/actual:
MAP19_02 graph digest reused:
MAP19_03 incoming handoff digest reused:

naked BFS start nodes:
naked BFS target nodes:
naked BFS targets reached:
naked BFS targets unreachable:
naked BFS visited nodes:
naked BFS visited edges:
naked BFS shortest proof count:
naked BFS input digest lower-hex SHA-256:
naked BFS proof digest lower-hex SHA-256:

completion state dimensions:
completion initial states:
completion goal states:
completion states visited:
completion transitions evaluated:
completion move transitions:
completion state transitions:
completion goals satisfied:
completion goals missing:
completion proof shortest transition count:
completion search input digest lower-hex SHA-256:
completion state-space digest lower-hex SHA-256:
completion proof digest lower-hex SHA-256:

MAP19_04 handoff digest lower-hex SHA-256:

missing graph/start/target failure probes:
digest mismatch failure probes:
unreachable target failure probes:
invalid state transition failure probes:
forbidden action failure probes:
atomic failure success proofs published:
atomic failure MAP19_04 handoff digests published:

BFS/completion searches run:
naked traversal proofs run:
seed batch runs:
production seed approvals:
cluster recovery/density validations run:
event removal/worst-case validations run:
distance/revisit/pacing measurements run:
PlayerController/Rigidbody/Collider/Physics2D behavior changes:
Physics2D queries/simulations:
runtime objects spawned:
GameObject instantiate/enable/disable/destroy:
System.IO file write/read calls:
PlayerPrefs writes/reads:
Unity Tilemap component writes:
Tilemap.SetTile/SetTiles/SetTilesBlock/ClearAllTiles calls:
Scene/Prefab/Tilemap mutation:
Addressables/Resources/AssetDatabase loads:
optimization rewrites/broad refactors:
MAP19_04 started:
```

Allowed nonzero counters:

```text
BFS/completion searches run
naked traversal proofs run
completion states visited
completion transitions evaluated
completion move transitions
completion state transitions
```

All forbidden boundary counters must be zero.

## 13. PASS 조건

PASS 조건:

```text
MAP19_02 Result and installed Task SHA match
MAP19_02 graph digest and MAP19_03 handoff digest are reused exactly
Naked BFS uses tool_mask 0 and reaches required targets in the focused source
Completion state key includes position, resourceMask, forge, seal, boss, specialState
Completion search satisfies all focused mandatory goals
Search uses only MAP19_02 graph edges/socket links and explicit state transitions
Failure cases are deterministic and atomic
All required digests are stable lower-hex SHA-256
Focused MAP19_03 EditMode tests are 10/10 PASS
compile errors are 0
relevant Console errors are 0
forbidden APIs and runtime mutation counters are 0
seed batch, legacy regression, PlayMode, unfiltered/full regression are 0
MAP19_04 remains LOCKED / NOT STARTED
```

FAIL 조건:

```text
Any required focused test fails
Any required digest is missing or unstable
Completion search uses runtime physics or scene object state
Search mutates graph, tilemap, scene, prefab, collider, rigidbody, or player tuning
Seed batch or legacy regression runs without explicit trigger approval
MAP19_04 is started or unlocked
Failure publishes success proof or MAP19_04 handoff
Result lacks user-facing responsibility report
```

BLOCKED 조건:

```text
MAP19_02 Result SHA mismatch
MAP19_02 installed Task SHA mismatch
Required MAP19_02 graph surface is not publicly consumable
Required start/target binding source cannot be identified without inventing production fixture
Unity compile prevents focused tests from running before task code can be isolated
```

## 14. Status Finalize

PASS 후에만 status를 finalize한다.

Expected final status:

```text
MAP19_03_IMPLEMENT_NAKED_BFS_AND_COMPLETION_SEARCH: COMPLETE
MAP19_04_VALIDATE_CLUSTER_RECOVERY_AND_DENSITY: LOCKED
Current Task: NONE
```

MAP19_04는 다음 파일을 내가 별도로 줄 때까지 시작하지 않는다.

## 15. Commit

PASS finalize 후 atomic commit을 만든다.

Commit subject:

```text
MAP19_03: implement naked BFS and completion search
```

Commit 범위:

```text
MAP19_03 production files
MAP19_03 focused test file
matching .meta files
MapDesign/MCP/TASKS/MAP19_03_IMPLEMENT_NAKED_BFS_AND_COMPLETION_SEARCH.md
MapDesign/MCP_ARCHIVE/MAP19_03_IMPLEMENT_NAKED_BFS_AND_COMPLETION_SEARCH.md
MapDesign/MCP/REPORTS/MAP19_03_IMPLEMENT_NAKED_BFS_AND_COMPLETION_SEARCH_RESULT.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

금지:

```text
git push
unrelated dirty file staging
committing user changes
MAP19_04 files
```

## 16. Stop Rule

작업 완료 후 반드시 멈춘다.

```text
STOP AFTER MAP19_03 RESULT AND COMMIT.
DO NOT START MAP19_04.
```

