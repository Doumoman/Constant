```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP19_07_MEASURE_DISTANCE_REVISIT_AND_PACING
  task_file: TASKS/MAP19_07_MEASURE_DISTANCE_REVISIT_AND_PACING.md
  requires_current_task: NONE
  requires_completed_task: MAP19_06_VALIDATE_WORST_CASE_SCENARIOS
  requires_result:
    path: REPORTS/MAP19_06_VALIDATE_WORST_CASE_SCENARIOS_RESULT.md
    status: PASS
    sha256: 2d02e48a76f39cacb3f075600e540160dd745788c374bdf835c234baf4932cb8
  requires_installed_task:
    path: TASKS/MAP19_06_VALIDATE_WORST_CASE_SCENARIOS.md
    sha256: 015d966995e884abfce9df7833b75b4d829039045d7ac54ddcc39f46b5c4b13e
  sets_current_task: MAP19_07_MEASURE_DISTANCE_REVISIT_AND_PACING
```

# MAP19_07 - Measure Distance, Revisit, and Pacing

```text
TASK: MAP19_07_MEASURE_DISTANCE_REVISIT_AND_PACING
PHASE: MAP19 - Traversal Validation / Seed QA Preparation
STATUS: CURRENT
NEXT: MAP19_08_CREATE_FAILURE_BUNDLE_AND_HEADLESS_RUNNER
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP19_02~06의 graph/proof/validation surface를 읽기 전용으로 소비해, generated route의 **거리**, **재방문**, **반복 복도 비율**, **사건 간격**을 pure-data로 측정한다.

이번 Task는 측정기와 focused threshold audit만 만든다.  
대량 seed, failure bundle, screenshot, headless runner, production seed approval은 하지 않는다.

이번 Task의 책임:

```text
1. minimum, normal, optional route distance를 같은 단위로 측정한다.
2. revisit count와 revisit ratio를 route path/proof 기준으로 측정한다.
3. repeated corridor ratio가 35% 이하인지 검사한다.
4. activity/event/special/mandatory pacing marker 사이의 간격을 측정한다.
5. threshold registry를 한 곳에 두고 테스트/validator가 같은 값을 참조하게 한다.
6. MAP19_08 failure bundle/headless runner가 소비할 measurement handoff surface를 게시한다.
```

금지:

```text
seed batch run
production seed approval
failure bundle creation
screenshot export
CSV export
headless runner creation
range worker or replay runner creation
legacy 19347 regression
prior task category selection
PlayMode selection
unfiltered test run
full regression run
new tile movement graph generation or mutation
MAP19_01 profile/rule registry mutation
MAP19_02 graph digest rewrite
MAP19_03 proof rewrite
MAP19_04 recovery/density proof rewrite
MAP19_05 repetition/removal proof rewrite
MAP19_06 worst-case proof rewrite
runtime object query or mutation
runtime activity/event object mutation
runtime destructible tile mutation
runtime moving device simulation
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
MAP19_08 unlock or execution
```

## 1. 사용자 보고 의무

Result 첫 부분에는 반드시 아래 두 섹션을 둔다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

`User-Facing Implementation Report`에는 아래 질문에 답한다.

```text
이번 작업이 어떤 기능을 추가했는가?
minimum/normal/optional distance가 각각 무엇을 뜻하는가?
revisit와 repeated corridor ratio는 무엇을 잡기 위한 수치인가?
activity/event/special pacing gap은 어떻게 측정했는가?
어떤 기존 graph/proof/digest를 읽기 전용으로 소비했는가?
무엇을 일부러 하지 않았는가?
```

`Responsibility and Added Scripts`는 표로 작성한다.

```text
| Script or file | Added or changed responsibility | Explicit non-ownership |
```

파일별 책임이 없으면 FAIL이다.

## 2. 선행조건

작업 시작 전에 다음을 확인한다.

```text
MapDesign/MCP/REPORTS/MAP19_06_VALIDATE_WORST_CASE_SCENARIOS_RESULT.md exists
MAP19_06 Result STATUS: PASS
MAP19_06 Result SHA-256:
2d02e48a76f39cacb3f075600e540160dd745788c374bdf835c234baf4932cb8

MapDesign/MCP/TASKS/MAP19_06_VALIDATE_WORST_CASE_SCENARIOS.md exists
MAP19_06 installed task SHA-256:
015d966995e884abfce9df7833b75b4d829039045d7ac54ddcc39f46b5c4b13e

MAP19_02 graph digest:
bf6b682dd5797e6dd8cb469289ca26fc8db3f10175a99f77d6316c5afe65d063

MAP19_03 completion proof digest:
6bc3bf96b766337011fd08d3436fd46e12b30ae7b0af5cfc0598646e8e338fca

MAP19_04 combined digest:
6e393133118683be5104af6e4a4f2eff39cee61dc2c1dc8c240fba969512b816

MAP19_05 combined digest:
3a3e02bb35f99c052b18588fa4ed6a0bdcf01bc8cac860721f28908115c840fa

MAP19_06 worst-case combined digest:
9f8e46d45071e7ed2e3b189a0467a1381aa7eb05a516471b4c4337f52ad6420d

MAP19_07 incoming handoff digest:
5b9ef46451a1bee65b41c82d5011d9b0df4ba9c5cd2d939d395acbc8a320ad25
```

Status 조건:

```text
Current Task before apply: NONE
MAP19_06_VALIDATE_WORST_CASE_SCENARIOS: COMPLETE
MAP19_07_MEASURE_DISTANCE_REVISIT_AND_PACING: LOCKED before apply
MAP19_08_CREATE_FAILURE_BUNDLE_AND_HEADLESS_RUNNER: LOCKED
```

선행 SHA/digest가 다르면 실행하지 않는다.

## 3. 입력 표면

필수 입력:

```text
GeneratedTileMovementGraph from MAP19_02
GeneratedCompletionSearch proof/search from MAP19_03
GeneratedClusterRecoveryDensityValidation result from MAP19_04
GeneratedRepetitionEventRemovalValidation result from MAP19_05
GeneratedWorstCaseScenarioValidation result from MAP19_06
Activity/Event/Special/Mandatory marker surfaces from MAP12/MAP13/MAP18 where public
World/sector route ordering surface from MAP14/MAP15 where public
Generated slice/cell/provenance surfaces from MAP16/MAP17
```

프로젝트 타입 이름이 다르면 같은 semantic owner의 현재 public 타입을 사용한다.  
타입명을 맞추려고 이전 Phase 파일을 대규모 rename/refactor하지 않는다.

## 4. 구현 대상

권장 production 파일:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Validation/GeneratedDistancePacingValidation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Validation/GeneratedDistancePacingValidator.cs
```

권장 focused test 파일:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Validation/GeneratedDistancePacingValidatorTests.cs
```

기존 naming convention이 더 적합하면 따르되, Result 책임 표에 실제 파일명을 적는다.

## 5. Distance Measurement 계약

거리 측정은 tile graph proof/path의 edge cost를 합산한다.

필수 route class:

```text
MinimumCritical
NormalCompletion
OptionalCompletion
WorstCaseCompletion
```

정의:

```text
MinimumCritical:
  start에서 필수 목표와 exit까지 가는 최소 critical path.

NormalCompletion:
  Activity/Event 도움을 포함하지 않는 기본 completion route.

OptionalCompletion:
  optional branch, special reward, side resource를 포함한 route.

WorstCaseCompletion:
  MAP19_06 scenario transform 중 CombinedAdverseStaticShell 또는 그에 준하는 불리 조건 route.
```

거리 단위:

```text
distance unit: graph edge cost in tile-step equivalent
movement cost source: MAP19_01 rule registry through MAP19_02 edge metadata
socket link cost: explicit intersector link cost or 1 tile-step equivalent if no authored cost exists
```

Threshold registry는 한 곳에 둔다.

```text
MinimumCritical target: 500..900
NormalCompletion target: 800..1400
OptionalCompletion target: 1500..2800
RepeatedCorridor max ratio: 35%
```

production code와 test가 같은 registry를 참조해야 한다.  
테스트에 숫자를 다시 복사하지 않는다.

현재 focused fixture가 world-scale이 아니면, Result에 다음을 명시한다.

```text
world-scale source available:
focused fixture source kind:
world-scale threshold approval performed:
```

world-scale source가 없을 때 production seed approval을 주장하면 FAIL이다.  
그래도 측정기와 threshold audit은 focused fixture에서 deterministic하게 검증한다.

## 6. Revisit and Repeated Corridor 계약

재방문 측정은 route proof의 node/edge visit history로 계산한다.

필수 수치:

```text
unique route nodes
total route node visits
revisited node count
revisit ratio
unique route edges
total route edge visits
repeated corridor edge count
repeated corridor ratio
repeated corridor threshold: <= 35%
```

Repeated corridor는 아래처럼 정의한다.

```text
same corridor signature appears more than once in one route proof
same source owner and same local direction band are traversed repeatedly
movement is not a deliberate return path marked by recovery/special/loop owner
```

반복 복도 검증은 MAP19_05 structural repetition 결과를 소비하되 다시 쓰지 않는다.

## 7. Pacing Gap 계약

사건 간격은 route 위에 투영된 marker 순서로 측정한다.

Marker kind:

```text
MandatoryResource
Activity
EventOverlay
SpecialEntry
SpecialReward
Village
Forge
Seal
Boss
Exit
```

각 marker는 다음을 가진다.

```text
marker id
marker kind
source owner
bound node id or bound route segment
route distance from start
previous marker distance gap
next marker distance gap
```

검증 규칙:

```text
marker가 graph node나 route segment에 bind되지 않으면 실패한다.
동일 marker kind가 같은 pacing band에 과도하게 몰리면 실패한다.
필수 marker 순서가 completion proof와 모순되면 실패한다.
event/activity marker가 제거 scenario의 static completion에 필수로 남아 있으면 실패한다.
```

정확한 재미 조율은 아직 하지 않는다.  
이번 Task는 measurement와 obvious clustering failure만 잡는다.

## 8. Failure and Atomicity

실패는 deterministic하게 보고한다.

필수 failure fields:

```text
owner
reason
measurement id
route class
offending key
expected
actual
source digest
route or marker evidence
```

focused test에는 다음 failure probe를 포함한다.

```text
missing MAP19_06 handoff
MAP19_06 digest mismatch
missing route class binding
missing marker binding
minimum route distance outside target
normal route distance outside target
optional route distance outside target
repeated corridor ratio above 35 percent
pacing marker cluster violation
forbidden seed/headless/failure-bundle attempt
MAP19_08 start attempt
```

Failure 이후 다음 surface가 게시되면 FAIL이다.

```text
distance success digest
revisit success digest
pacing success digest
MAP19_08 handoff digest
```

원자성 증거:

```text
atomic failure success digests published: 0
atomic failure MAP19_08 handoff digests published: 0
```

## 9. Determinism and Digest

모든 collection은 canonical 정렬한다.

정렬 기준:

```text
route class ascending ordinal
route id ascending ordinal
node id ascending ordinal
edge id ascending ordinal
marker kind ascending ordinal
marker id ascending ordinal
failure key ascending ordinal
```

필수 digest:

```text
distance measurement input digest
distance measurement digest
revisit measurement digest
pacing measurement digest
distance+revisit+pacing combined digest
MAP19_08 handoff digest
```

Result에는 다음 안정성 증거를 기록한다.

```text
repeat digest mismatch count: 0
reverse route/order digest mismatch count: 0
culture digest mismatch count: 0
marker order digest mismatch count: 0
mutation sensitivity probes passed:
```

## 10. 금지 API 스캔

production code는 다음 API/namespace를 참조하지 않아야 한다.

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
UnityEngine.Input
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

## 11. Focused-only 검증 정책

정상 검증은 EditMode category `MAP19_07`만 선택한다.

```text
MAP19_07 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15/MAP16/MAP17/MAP18/MAP19_01/MAP19_02/MAP19_03/MAP19_04/MAP19_05/MAP19_06 selections: 0
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

## 12. 필수 Focused Tests

다음 test name을 그대로 포함한다.

```text
DistancePacingValidatorMeasuresMinimumNormalOptionalAndWorstCaseRoutes
DistancePacingValidatorUsesSharedThresholdRegistryWithoutDuplicateConstants
DistancePacingValidatorComputesRevisitAndRepeatedCorridorRatio
DistancePacingValidatorBindsActivityEventSpecialAndMandatoryMarkersToRoute
DistancePacingValidatorConsumesMap19_02ToMap19_06SurfacesReadOnly
DistancePacingValidatorRejectsMissingHandoffDigestMismatchAndMissingBindings
DistancePacingValidatorFailuresAreAtomicAndReportOwnerReasonExpectedActual
DistancePacingValidatorDigestIsStableAcrossRepeatReverseCultureAndMarkerOrder
DistancePacingValidatorPublishesMap19_08HandoffSurface
Map19HandoffKeepsMap19_08Locked
```

Focused test category:

```text
MAP19_07
```

Required count:

```text
discovered: 10
executed: 10
passed: 10
failed: 0
```

## 13. Result 필수 기록

Result 파일:

```text
MapDesign/MCP/REPORTS/MAP19_07_MEASURE_DISTANCE_REVISIT_AND_PACING_RESULT.md
```

Result에는 아래 섹션을 포함한다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
## Distance Revisit and Pacing Summary
## No Seed Regression or Physics Boundary Notes
```

`Distance Revisit and Pacing Summary`에는 아래 값을 채운다.

```text
MAP19_06 Result SHA-256 required/actual:
MAP19_06 installed Task SHA-256 required/actual:
MAP19_02 graph digest reused:
MAP19_03 completion proof digest reused:
MAP19_04 combined digest reused:
MAP19_05 combined digest reused:
MAP19_06 combined digest reused:
MAP19_07 incoming handoff digest reused:

world-scale source available:
focused fixture source kind:
world-scale threshold approval performed:

route classes measured:
minimum critical distance:
normal completion distance:
optional completion distance:
worst-case completion distance:
minimum critical target range:
normal completion target range:
optional completion target range:
distance range violations:

unique route nodes:
total route node visits:
revisited node count:
revisit ratio:
unique route edges:
total route edge visits:
repeated corridor edge count:
repeated corridor ratio:
repeated corridor threshold:
repeated corridor violations:

pacing markers checked:
mandatory/resource markers:
activity markers:
event markers:
special markers:
village/forge/seal/boss/exit markers:
marker binding failures:
minimum marker gap:
maximum marker gap:
pacing cluster violations:

distance measurement input digest lower-hex SHA-256:
distance measurement digest lower-hex SHA-256:
revisit measurement digest lower-hex SHA-256:
pacing measurement digest lower-hex SHA-256:
distance+revisit+pacing combined digest lower-hex SHA-256:
MAP19_08 handoff digest lower-hex SHA-256:

repeat digest mismatch count:
reverse route/order digest mismatch count:
culture digest mismatch count:
marker order digest mismatch count:
mutation sensitivity probes passed:

missing handoff/binding failure probes:
digest mismatch failure probes:
distance range failure probes:
repeated corridor failure probes:
pacing marker cluster failure probes:
forbidden seed/headless/failure-bundle probes:
atomic failure success digests published:
atomic failure MAP19_08 handoff digests published:

distance/revisit/pacing measurements run:
BFS/completion searches run:
worst-case validations run:
failure bundle/headless runner creations:
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
MAP19_08 started:
```

Allowed nonzero counters:

```text
distance/revisit/pacing measurements run
BFS/completion searches run only if MAP19_03 pure-data search is used to derive measured route proofs
```

All wider verification and runtime mutation counters must be zero.

## 14. PASS 조건

PASS 조건:

```text
MAP19_06 Result and installed Task SHA match
MAP19_02 graph, MAP19_03 completion, MAP19_04 combined, MAP19_05 combined, MAP19_06 combined, and MAP19_07 incoming handoff digests are reused exactly
Minimum, normal, optional, and worst-case route classes are measured
Threshold registry contains 500..900, 800..1400, 1500..2800, and repeated corridor <=35% in one shared source
World-scale approval is only claimed when a world-scale source is actually present
Revisit and repeated corridor metrics are deterministic
Activity/Event/Special/Mandatory pacing markers are bound to route nodes or segments
Failure cases are deterministic and atomic
All required digests are stable lower-hex SHA-256
Focused MAP19_07 EditMode tests are 10/10 PASS
compile errors are 0
relevant Console errors are 0
forbidden APIs and runtime mutation counters are 0
failure bundle, headless runner, seed batch, legacy regression, PlayMode, unfiltered/full regression are 0
MAP19_08 remains LOCKED / NOT STARTED
```

FAIL 조건:

```text
Any required focused test fails
Any required digest is missing or unstable
Threshold constants are duplicated across production/test instead of one registry
Material-only variation is counted as route distance variety
Measurement mutates runtime objects, graph, proof, scene, prefab, tilemap, collider, rigidbody, or player state
World-scale approval is claimed from a focused fixture only
Failure bundle, headless runner, seed batch, or legacy regression runs without explicit trigger approval
MAP19_08 is started or unlocked
Failure publishes success digest or MAP19_08 handoff
Result lacks user-facing responsibility report
```

BLOCKED 조건:

```text
MAP19_06 Result SHA mismatch
MAP19_06 installed Task SHA mismatch
Required route class or pacing marker binding source is not publicly consumable
Distance unit cannot be derived from graph edge metadata or rule registry without inventing tuning
Unity compile prevents focused tests from running before task code can be isolated
```

## 15. Status Finalize

PASS 후에만 status를 finalize한다.

Expected final status:

```text
MAP19_07_MEASURE_DISTANCE_REVISIT_AND_PACING: COMPLETE
MAP19_08_CREATE_FAILURE_BUNDLE_AND_HEADLESS_RUNNER: LOCKED
Current Task: NONE
```

MAP19_08은 다음 파일을 내가 별도로 줄 때까지 시작하지 않는다.

## 16. Commit

PASS finalize 후 atomic commit을 만든다.

Commit subject:

```text
MAP19_07: measure distance revisit and pacing
```

Commit 범위:

```text
MAP19_07 production files
MAP19_07 focused test file
matching .meta files
MapDesign/MCP/TASKS/MAP19_07_MEASURE_DISTANCE_REVISIT_AND_PACING.md
MapDesign/MCP_ARCHIVE/MAP19_07_MEASURE_DISTANCE_REVISIT_AND_PACING.md
MapDesign/MCP/REPORTS/MAP19_07_MEASURE_DISTANCE_REVISIT_AND_PACING_RESULT.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

금지:

```text
git push
unrelated dirty file staging
committing user changes
MAP19_08 files
```

## 17. Stop Rule

작업 완료 후 반드시 멈춘다.

```text
STOP AFTER MAP19_07 RESULT AND COMMIT.
DO NOT START MAP19_08.
```

