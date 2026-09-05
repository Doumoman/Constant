```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP19_04_VALIDATE_CLUSTER_RECOVERY_AND_DENSITY
  task_file: TASKS/MAP19_04_VALIDATE_CLUSTER_RECOVERY_AND_DENSITY.md
  requires_current_task: NONE
  requires_completed_task: MAP19_03_IMPLEMENT_NAKED_BFS_AND_COMPLETION_SEARCH
  requires_result:
    path: REPORTS/MAP19_03_IMPLEMENT_NAKED_BFS_AND_COMPLETION_SEARCH_RESULT.md
    status: PASS
    sha256: 184f11c2610577e4bd2851c21b6c33b9c667e50d2e8743851642989ea756c915
  requires_installed_task:
    path: TASKS/MAP19_03_IMPLEMENT_NAKED_BFS_AND_COMPLETION_SEARCH.md
    sha256: 451cdacd8c595a09443c2fa7d432fc584c6b74881e6f39a8c52fe375d71ca4e1
  sets_current_task: MAP19_04_VALIDATE_CLUSTER_RECOVERY_AND_DENSITY
```

# MAP19_04 - Validate Cluster Recovery and Density

```text
TASK: MAP19_04_VALIDATE_CLUSTER_RECOVERY_AND_DENSITY
PHASE: MAP19 - Traversal Validation / Seed QA Preparation
STATUS: CURRENT
NEXT: MAP19_05_VALIDATE_REPETITION_AND_EVENT_REMOVAL
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP19_02 tile movement graph와 MAP19_03 naked/completion proof를 읽기 전용으로 소비해, generated cluster 단위의 **복구 가능성**과 **기본 지형 밀도 안전성**을 검증한다.

이번 Task는 cluster 내부와 cluster 경계 주변이 플레이 가능한 지형인지 검사한다.  
하지만 seed batch, event removal, worst-case scenario, distance/revisit/pacing 측정은 아직 하지 않는다.

이번 Task의 책임:

```text
1. 기본 route, high route, recovery route가 graph 위에서 다시 합류 가능한지 검사한다.
2. 2~5초 복구 구간이 finite recovery proof로 닫히는지 검사한다.
3. cluster canvas의 solid/reachable 비율과 8x6 AIR window 최소 조건을 검사한다.
4. head snag, one-way pit, unreachable pocket, hard-solid obstruction 후보를 데이터 규칙으로 잡는다.
5. cluster별 failure owner/reason/key/expected/actual/provenance를 deterministic하게 보고한다.
6. MAP19_05 repetition/event-removal validator가 소비할 cluster validation handoff surface를 게시한다.
```

금지:

```text
new tile movement graph generation or mutation
MAP19_01 profile/rule registry mutation
MAP19_02 graph digest rewrite
MAP19_03 proof rewrite
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
MAP19_05 unlock or execution
```

## 1. 사용자 보고 의무

Result의 첫 두 섹션은 반드시 아래 이름으로 작성한다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

`User-Facing Implementation Report`에는 아래 질문에 답한다.

```text
이번 작업이 추가한 기능은 무엇인가?
복구 가능성 검증은 어떤 실패를 잡는가?
밀도/8x6 AIR/head snag/pit 검증은 어떤 실패를 잡는가?
MAP19_02 graph와 MAP19_03 proof를 어떻게 읽기 전용으로 소비했는가?
무엇을 일부러 하지 않았는가?
```

`Responsibility and Added Scripts`는 표로 작성한다.

```text
| Script or file | Added or changed responsibility | Explicit non-ownership |
```

보고는 파일별 책임을 명시한다.  
막연한 "검증 추가"만 적으면 FAIL이다.

## 2. 선행조건

작업 시작 전에 다음을 확인한다.

```text
MapDesign/MCP/REPORTS/MAP19_03_IMPLEMENT_NAKED_BFS_AND_COMPLETION_SEARCH_RESULT.md exists
MAP19_03 Result STATUS: PASS
MAP19_03 Result SHA-256:
184f11c2610577e4bd2851c21b6c33b9c667e50d2e8743851642989ea756c915

MapDesign/MCP/TASKS/MAP19_03_IMPLEMENT_NAKED_BFS_AND_COMPLETION_SEARCH.md exists
MAP19_03 installed task SHA-256:
451cdacd8c595a09443c2fa7d432fc584c6b74881e6f39a8c52fe375d71ca4e1

MAP19_02 graph digest reused by MAP19_03:
bf6b682dd5797e6dd8cb469289ca26fc8db3f10175a99f77d6316c5afe65d063

MAP19_03 completion proof digest:
6bc3bf96b766337011fd08d3436fd46e12b30ae7b0af5cfc0598646e8e338fca

MAP19_04 incoming handoff digest from MAP19_03:
694a70da6977d6cb7948976b8f2a512fa6ecf6c2d15f5f9f83d56dada59c9b2e
```

Status 조건:

```text
Current Task before apply: NONE
MAP19_03_IMPLEMENT_NAKED_BFS_AND_COMPLETION_SEARCH: COMPLETE
MAP19_04_VALIDATE_CLUSTER_RECOVERY_AND_DENSITY: LOCKED before apply
MAP19_05_VALIDATE_REPETITION_AND_EVENT_REMOVAL: LOCKED
```

선행 digest가 다르면 실행하지 않는다.  
줄바꿈 정규화나 metadata 자동 수정으로 우회하지 않는다.

## 3. 입력 표면

가능하면 기존 public 타입과 생성물을 사용한다. 이름이 정확히 다르더라도 동일 semantic owner가 이미 있으면 그 타입을 소비한다.

필수 입력:

```text
GeneratedTileMovementGraph from MAP19_02
GeneratedNakedTraversalSearch proof from MAP19_03
GeneratedCompletionSearch proof from MAP19_03
Cluster footprint, route spine, high route, recovery route contracts from MAP11/MAP14/MAP16
Generated slice/cell/provenance surfaces from MAP16/MAP17
Activity/event/special/population slots only as read-only occupancy or marker sources where already public
```

이번 Task는 graph나 completion proof를 다시 만들지 않는다.  
production validator는 이미 만들어진 graph/proof contract를 입력으로 받는다.

프로젝트 구조상 focused test에서 fixture graph/proof를 구성해야 한다면 Result에 아래를 분리해서 적는다.

```text
production graph builder invocations:
test fixture graph builder invocations:
production completion search invocations:
test fixture completion search invocations:
```

## 4. 구현 대상

권장 production 파일:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Validation/GeneratedClusterRecoveryDensityValidation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Validation/GeneratedClusterRecoveryDensityValidator.cs
```

권장 focused test 파일:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Validation/GeneratedClusterRecoveryDensityValidatorTests.cs
```

프로젝트 내 기존 naming convention이 더 적합하면 따르되, Result의 책임 표에 실제 파일명을 적는다.

## 5. Recovery Validation 계약

복구 검증은 graph 위의 reachability 문제로만 계산한다.

검사 대상:

```text
base route branch rejoin
high route fall or miss recovery
explicit recovery route
2~5 second recovery window
cluster entry to primary spine
primary spine to cluster exit
intersector socket recovery near cluster boundary
```

필수 개념:

```text
ClusterRecoveryValidationInput
ClusterRecoveryValidationResult
ClusterRecoveryCase
ClusterRecoveryProof
ClusterRecoveryFailure
```

각 recovery case는 최소한 다음을 가진다.

```text
cluster id
source owner
case kind
start node id
target node id or accepted target set
allowed movement kinds
max recovery cost or expected window class
graph digest
completion proof digest
```

2~5초 복구는 실제 시간 시뮬레이션이 아니다.  
MAP19_01 rule registry threshold 또는 기존 traversal profile 값을 사용해 edge cost upper bound로만 판정한다.

필수 Result 수치:

```text
clusters checked
recovery cases checked
base route recovery cases
high route recovery cases
explicit recovery cases
2-5s recovery cases
recovery cases passed
recovery cases failed
average recovery edge count
max recovery edge count
recovery proof digest
```

## 6. Density and Shape Validation 계약

밀도 검증은 generated cell/canvas/slice 표면의 데이터만 사용한다.

검사 대상:

```text
solid cell ratio
reachable air cell ratio
unreachable air pocket count
minimum contiguous 8x6 AIR window
head snag pattern count
one-way pit pattern count
hard-solid obstruction count
overprotected envelope obstruction count
boundary socket obstruction count
```

`8x6 AIR`는 1x1 tile 기준의 직사각형 window다.  
window 안의 required cell은 solid/hard-solid/protected-blocking owner에 의해 막히면 안 된다.

Head snag 후보는 다음처럼 정의한다.

```text
reachable floor-adjacent air
player-height clearance path exists immediately before the cell
upper clearance cell is blocked in a way that would catch jump/head movement
case is not already protected by an authored low-ceiling marker
```

One-way pit 후보는 다음처럼 정의한다.

```text
reachable entry air exists
downward movement into pocket exists
no graph-backed recovery edge/path returns to accepted recovery target
case is not authored as a deliberate challenge with valid recovery route
```

필수 Result 수치:

```text
density clusters checked
solid ratio min/max
reachable air ratio min/max
8x6 AIR windows required/found
unreachable pockets found
head snag candidates
one-way pit candidates
hard-solid obstructions
boundary socket obstructions
density validation digest
```

## 7. Failure and Atomicity

실패는 deterministic하게 보고한다.

필수 failure fields:

```text
owner
reason
cluster id
case id
offending key
expected
actual
source digest
frontier or window evidence when applicable
```

다음 failure probe를 focused test에 포함한다.

```text
missing MAP19_03 proof input
MAP19_03 handoff digest mismatch
missing cluster binding
missing recovery target
unreachable recovery target
recovery cost exceeds 2-5s bound
missing 8x6 AIR window
unreachable air pocket
head snag candidate
one-way pit candidate
hard-solid obstruction
forbidden API or mutation attempt
```

Failure 이후 다음 surface가 게시되면 FAIL이다.

```text
cluster recovery success digest
density success digest
MAP19_05 handoff digest
```

원자성 증거:

```text
atomic failure success digests published: 0
atomic failure MAP19_05 handoff digests published: 0
```

## 8. Determinism and Digest

모든 collection은 canonical 정렬한다.

정렬 기준:

```text
cluster id ascending ordinal
case id ascending ordinal
node id ascending ordinal
cell coordinate x/y ascending numeric
failure key ascending ordinal
window origin x/y ascending numeric
```

필수 digest:

```text
recovery validation input digest
recovery proof digest
density validation input digest
density validation digest
cluster recovery+density combined digest
MAP19_05 handoff digest
```

Result에는 다음 안정성 증거를 기록한다.

```text
repeat digest mismatch count: 0
reverse cluster/order digest mismatch count: 0
culture digest mismatch count: 0
cell order digest mismatch count: 0
mutation sensitivity probes passed:
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

테스트 파일에서 Unity Test Framework namespace를 사용하는 것은 허용된다.  
production implementation에는 위 API가 들어가면 안 된다.

## 10. Focused-only 검증 정책

정상 검증은 EditMode category `MAP19_04`만 선택한다.

```text
MAP19_04 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15/MAP16/MAP17/MAP18/MAP19_01/MAP19_02/MAP19_03 selections: 0
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
ClusterRecoveryValidatorPassesBaseHighAndExplicitRecoveryCases
ClusterRecoveryValidatorEnforcesTwoToFiveSecondRecoveryBounds
ClusterDensityValidatorFindsRequiredEightBySixAirWindows
ClusterDensityValidatorRejectsUnreachablePocketsHeadSnagsAndOneWayPits
ClusterRecoveryDensityValidatorConsumesMap19_02GraphAndMap19_03ProofReadOnly
ClusterRecoveryDensityValidatorRejectsMissingBindingsAndDigestMismatches
ClusterRecoveryDensityFailuresAreAtomicAndReportOwnerReasonExpectedActual
ClusterRecoveryDensityDigestIsStableAcrossRepeatReverseCultureAndCellOrder
ClusterRecoveryDensityPublishesMap19_05HandoffSurface
Map19HandoffKeepsMap19_05Locked
```

Focused test category:

```text
MAP19_04
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
MapDesign/MCP/REPORTS/MAP19_04_VALIDATE_CLUSTER_RECOVERY_AND_DENSITY_RESULT.md
```

Result에는 아래 섹션을 반드시 포함한다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
## Cluster Recovery and Density Summary
## No Seed Regression or Physics Boundary Notes
```

`Cluster Recovery and Density Summary`에는 아래 값을 채운다.

```text
MAP19_03 Result SHA-256 required/actual:
MAP19_03 installed Task SHA-256 required/actual:
MAP19_02 graph digest reused:
MAP19_03 completion proof digest reused:
MAP19_04 incoming handoff digest reused:

clusters checked:
recovery cases checked:
base route recovery cases:
high route recovery cases:
explicit recovery cases:
2-5s recovery cases:
recovery cases passed:
recovery cases failed:
average recovery edge count:
max recovery edge count:
recovery validation input digest lower-hex SHA-256:
recovery proof digest lower-hex SHA-256:

density clusters checked:
solid ratio min/max:
reachable air ratio min/max:
8x6 AIR windows required/found:
unreachable pockets found:
head snag candidates:
one-way pit candidates:
hard-solid obstructions:
boundary socket obstructions:
density validation input digest lower-hex SHA-256:
density validation digest lower-hex SHA-256:

cluster recovery+density combined digest lower-hex SHA-256:
MAP19_05 handoff digest lower-hex SHA-256:

repeat digest mismatch count:
reverse cluster/order digest mismatch count:
culture digest mismatch count:
cell order digest mismatch count:
mutation sensitivity probes passed:

missing proof/binding failure probes:
digest mismatch failure probes:
unreachable recovery failure probes:
recovery bound failure probes:
density window failure probes:
pocket/head-snag/pit failure probes:
hard-solid/boundary obstruction failure probes:
forbidden API or mutation failure probes:
atomic failure success digests published:
atomic failure MAP19_05 handoff digests published:

cluster recovery/density validations run:
BFS/completion searches run:
event removal/worst-case validations run:
distance/revisit/pacing measurements run:
seed batch runs:
production seed approvals:
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
MAP19_05 started:
```

Allowed nonzero counters:

```text
cluster recovery/density validations run
BFS/completion searches run only if reused as local helper inside focused validator evidence
```

If BFS/completion search is invoked again, it must be only the already implemented MAP19_03 pure-data search over the same focused graph/proof boundary.  
Seed batch and wider verification counters must remain zero.

## 13. PASS 조건

PASS 조건:

```text
MAP19_03 Result and installed Task SHA match
MAP19_02 graph digest and MAP19_03 proof/handoff digests are reused exactly
Base, high, explicit, and 2-5s recovery cases pass in focused source
Density rules publish solid/reachable/AIR-window/head-snag/pit evidence
8x6 AIR windows are present where required
Unreachable pockets, head snag candidates, one-way pit candidates, hard-solid obstructions are zero in success source
Failure cases are deterministic and atomic
All required digests are stable lower-hex SHA-256
Focused MAP19_04 EditMode tests are 10/10 PASS
compile errors are 0
relevant Console errors are 0
forbidden APIs and runtime mutation counters are 0
seed batch, legacy regression, PlayMode, unfiltered/full regression are 0
MAP19_05 remains LOCKED / NOT STARTED
```

FAIL 조건:

```text
Any required focused test fails
Any required digest is missing or unstable
Recovery validation uses runtime physics or scene object state
Density validation mutates graph, tilemap, scene, prefab, collider, rigidbody, or player tuning
Seed batch or legacy regression runs without explicit trigger approval
MAP19_05 is started or unlocked
Failure publishes success digest or MAP19_05 handoff
Result lacks user-facing responsibility report
```

BLOCKED 조건:

```text
MAP19_03 Result SHA mismatch
MAP19_03 installed Task SHA mismatch
Required MAP19_02 graph or MAP19_03 proof surface is not publicly consumable
Cluster binding or density cell source cannot be identified without inventing production fixture
Unity compile prevents focused tests from running before task code can be isolated
```

## 14. Status Finalize

PASS 후에만 status를 finalize한다.

Expected final status:

```text
MAP19_04_VALIDATE_CLUSTER_RECOVERY_AND_DENSITY: COMPLETE
MAP19_05_VALIDATE_REPETITION_AND_EVENT_REMOVAL: LOCKED
Current Task: NONE
```

MAP19_05는 다음 파일을 내가 별도로 줄 때까지 시작하지 않는다.

## 15. Commit

PASS finalize 후 atomic commit을 만든다.

Commit subject:

```text
MAP19_04: validate cluster recovery and density
```

Commit 범위:

```text
MAP19_04 production files
MAP19_04 focused test file
matching .meta files
MapDesign/MCP/TASKS/MAP19_04_VALIDATE_CLUSTER_RECOVERY_AND_DENSITY.md
MapDesign/MCP_ARCHIVE/MAP19_04_VALIDATE_CLUSTER_RECOVERY_AND_DENSITY.md
MapDesign/MCP/REPORTS/MAP19_04_VALIDATE_CLUSTER_RECOVERY_AND_DENSITY_RESULT.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

금지:

```text
git push
unrelated dirty file staging
committing user changes
MAP19_05 files
```

## 16. Stop Rule

작업 완료 후 반드시 멈춘다.

```text
STOP AFTER MAP19_04 RESULT AND COMMIT.
DO NOT START MAP19_05.
```

