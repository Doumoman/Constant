```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP19_06_VALIDATE_WORST_CASE_SCENARIOS
  task_file: TASKS/MAP19_06_VALIDATE_WORST_CASE_SCENARIOS.md
  requires_current_task: NONE
  requires_completed_task: MAP19_05_VALIDATE_REPETITION_AND_EVENT_REMOVAL
  requires_result:
    path: REPORTS/MAP19_05_VALIDATE_REPETITION_AND_EVENT_REMOVAL_RESULT.md
    status: PASS
    sha256: f827465fa0560fa373f102686fbe6fd05fbcfb04f7db848b77eb5fa262b6ca14
  requires_installed_task:
    path: TASKS/MAP19_05_VALIDATE_REPETITION_AND_EVENT_REMOVAL.md
    sha256: 4b93c7de3006cbce9e1633ad7c4ca71322c685e4e1f12ed77578fe4bc8fbf47f
  sets_current_task: MAP19_06_VALIDATE_WORST_CASE_SCENARIOS
```

# MAP19_06 - Validate Worst Case Scenarios

```text
TASK: MAP19_06_VALIDATE_WORST_CASE_SCENARIOS
PHASE: MAP19 - Traversal Validation / Seed QA Preparation
STATUS: CURRENT
NEXT: MAP19_07_MEASURE_DISTANCE_REVISIT_AND_PACING
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP19_02~05의 graph, completion, recovery/density, repetition/removal proof를 읽기 전용으로 소비해, 플레이어에게 불리한 대표 scenario에서도 generated static shell이 완주 가능한지 검증한다.

이번 Task는 **scenario transform 검증**만 한다.  
대량 seed QA, 거리/재방문/페이싱 측정, failure bundle/headless runner 생성은 하지 않는다.

이번 Task의 책임:

```text
1. 도구 0 scenario를 명시적으로 고정하고 completion을 다시 검증한다.
2. 마을 미방문 scenario에서 shop/facility/NPC 도움 없이 completion을 검증한다.
3. hostile village scenario에서 village/shop 도움 transition을 제거하고 completion을 검증한다.
4. evacuated village scenario에서 NPC/shop/facility optional 도움을 제거하고 completion을 검증한다.
5. 파괴 가능 타일 소실과 장치 최악 위치를 pure-data scenario transform으로 모델링한다.
6. 각 scenario가 graph/proof를 수정하지 않고 통과하는지 deterministic proof/digest를 게시한다.
7. MAP19_07 distance/revisit/pacing 측정이 소비할 worst-case handoff surface를 게시한다.
```

금지:

```text
seed batch run
production seed approval
distance/revisit/pacing measurement
failure bundle, screenshot, CSV, or headless runner creation
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
runtime village/shop/NPC object mutation
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
MAP19_07 unlock or execution
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
각 worst-case scenario가 어떤 불리한 상황을 의미하는가?
파괴 타일과 장치 최악 위치를 runtime mutation 없이 어떻게 모델링했는가?
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
MapDesign/MCP/REPORTS/MAP19_05_VALIDATE_REPETITION_AND_EVENT_REMOVAL_RESULT.md exists
MAP19_05 Result STATUS: PASS
MAP19_05 Result SHA-256:
f827465fa0560fa373f102686fbe6fd05fbcfb04f7db848b77eb5fa262b6ca14

MapDesign/MCP/TASKS/MAP19_05_VALIDATE_REPETITION_AND_EVENT_REMOVAL.md exists
MAP19_05 installed task SHA-256:
4b93c7de3006cbce9e1633ad7c4ca71322c685e4e1f12ed77578fe4bc8fbf47f

MAP19_02 graph digest:
bf6b682dd5797e6dd8cb469289ca26fc8db3f10175a99f77d6316c5afe65d063

MAP19_03 completion proof digest:
6bc3bf96b766337011fd08d3436fd46e12b30ae7b0af5cfc0598646e8e338fca

MAP19_04 recovery/density combined digest:
6e393133118683be5104af6e4a4f2eff39cee61dc2c1dc8c240fba969512b816

MAP19_05 repetition/removal combined digest:
3a3e02bb35f99c052b18588fa4ed6a0bdcf01bc8cac860721f28908115c840fa

MAP19_06 incoming handoff digest:
37643ee61d7ccd85f95018542382e119b418be544ee0d4820551012413535907
```

Status 조건:

```text
Current Task before apply: NONE
MAP19_05_VALIDATE_REPETITION_AND_EVENT_REMOVAL: COMPLETE
MAP19_06_VALIDATE_WORST_CASE_SCENARIOS: LOCKED before apply
MAP19_07_MEASURE_DISTANCE_REVISIT_AND_PACING: LOCKED
```

선행 SHA/digest가 다르면 실행하지 않는다.

## 3. 입력 표면

필수 입력:

```text
GeneratedTileMovementGraph from MAP19_02
GeneratedCompletionSearch contract/proof from MAP19_03
GeneratedClusterRecoveryDensityValidation result from MAP19_04
GeneratedRepetitionEventRemovalValidation result from MAP19_05
Village shell/state/shop/facility surfaces from MAP13/MAP18 where public
Sector modification/destructible/device state contracts from MAP17/MAP18 where public
Generated slice/cell/provenance surfaces from MAP16/MAP17
```

프로젝트 타입 이름이 다르면 같은 semantic owner의 현재 public 타입을 사용한다.  
타입명을 맞추려고 이전 Phase 파일을 대규모 rename/refactor하지 않는다.

## 4. 구현 대상

권장 production 파일:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Validation/GeneratedWorstCaseScenarioValidation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Validation/GeneratedWorstCaseScenarioValidator.cs
```

권장 focused test 파일:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Validation/GeneratedWorstCaseScenarioValidatorTests.cs
```

기존 naming convention이 더 적합하면 따르되, Result 책임 표에 실제 파일명을 적는다.

## 5. Scenario Transform 계약

Worst-case는 runtime simulation이 아니라 read-only input에서 파생되는 pure-data transform이다.

필수 개념:

```text
GeneratedWorstCaseScenarioInput
GeneratedWorstCaseScenarioCatalog
GeneratedWorstCaseScenarioTransform
GeneratedWorstCaseScenarioResult
GeneratedWorstCaseScenarioProof
GeneratedWorstCaseScenarioFailure
```

Scenario transform은 다음만 할 수 있다.

```text
allowed tool mask를 0으로 고정
optional shop/facility/NPC transition 제거
hostile/evacuated village 도움 transition 제거
Activity/Event 도움 transition 제거 상태 재사용
destructible tile loss를 logical blocked/removed cell overlay로 가정
moving/device worst position을 logical disabled or worst-state transition filter로 가정
MAP19_03 completion search input을 pure-data로 재구성
```

Scenario transform은 다음을 할 수 없다.

```text
tilemap write
scene object enable/disable
GameObject mutation
runtime collision query
Physics2D simulation
PlayerController state mutation
implicit teleport
forced reward grant
forced terrain carve
source graph/proof/digest rewrite
```

각 scenario는 최소한 다음을 가진다.

```text
scenario id
scenario kind
source owner
input digest
removed transition ids
logical overlay ids
retained goal ids
completion proof digest
pass/fail decision
failure evidence if any
```

## 6. Required Worst-case Scenarios

필수 scenario kind:

```text
ZeroTool
VillageSkipped
VillageHostile
VillageEvacuated
DestructibleTileLoss
MovingDeviceWorstPosition
CombinedAdverseStaticShell
```

각 scenario의 의미:

```text
ZeroTool:
  optional tool, shop item, assisted movement source를 전부 사용하지 않는다.

VillageSkipped:
  village/shop/facility/NPC 방문으로 얻는 optional transition을 제거한다.

VillageHostile:
  shop/facility 도움과 safe interaction transition을 사용할 수 없다고 가정한다.

VillageEvacuated:
  NPC/shop/facility optional 도움 transition이 없다고 가정한다.

DestructibleTileLoss:
  파괴 가능하거나 교체 가능한 non-critical tile이 불리한 상태로 사라졌다고 logical overlay만 적용한다.

MovingDeviceWorstPosition:
  움직이는 장치나 상태형 장치가 가장 덜 도와주는 상태로 고정되었다고 transition filter만 적용한다.

CombinedAdverseStaticShell:
  위 조건 중 static shell에 적용 가능한 조건을 함께 적용하되, MAP19_05 static completion과 충돌하지 않아야 한다.
```

성공 조건:

```text
각 scenario에서 required completion goal이 만족된다.
각 scenario에서 exit/goal node가 graph-backed path로 도달 가능하다.
각 scenario에서 recovery/density proof의 protected-critical target을 깨지 않는다.
logical overlay가 protected envelope, mandatory route, required landing, boundary socket을 무효화하면 실패한다.
```

## 7. Destruction and Device Boundary

파괴 타일/장치 최악 위치는 실제 타일/오브젝트 조작이 아니다.

Destructible tile 검증:

```text
destructible or replaceable source만 후보가 된다.
mandatory route, traversal envelope, required landing, recovery floor, boundary socket source는 protected-critical로 취급한다.
protected-critical 후보가 destruction overlay에 들어가면 실패한다.
non-critical 후보 제거 후에도 completion search가 통과해야 한다.
```

Device worst-position 검증:

```text
moving/stateful device source만 후보가 된다.
device가 필수 이동 edge를 제공하는 유일한 source이면 explicit fallback route가 필요하다.
fallback이 없으면 failure owner는 device/route binding으로 기록한다.
device disabled/worst-state는 transition filter로만 표현한다.
```

필수 Result 수치:

```text
destructible candidates checked:
destructible protected-critical rejects:
destructible overlays applied:
destructible scenario completions passed:
device candidates checked:
device worst-state transforms:
device fallback completions passed:
device fallback violations:
```

## 8. Failure and Atomicity

실패는 deterministic하게 보고한다.

필수 failure fields:

```text
owner
reason
scenario id
case id
offending key
expected
actual
source digest
frontier or overlay evidence
```

focused test에는 다음 failure probe를 포함한다.

```text
missing MAP19_05 handoff
MAP19_05 digest mismatch
missing scenario binding
zero-tool completion failure
village skipped completion failure
hostile village dependency failure
evacuated village dependency failure
protected-critical destructible overlay failure
device fallback missing failure
combined adverse scenario completion failure
forbidden runtime mutation attempt
MAP19_07 start attempt
```

Failure 이후 다음 surface가 게시되면 FAIL이다.

```text
worst-case success digest
scenario proof digest
MAP19_07 handoff digest
```

원자성 증거:

```text
atomic failure success digests published: 0
atomic failure MAP19_07 handoff digests published: 0
```

## 9. Determinism and Digest

모든 collection은 canonical 정렬한다.

정렬 기준:

```text
scenario id ascending ordinal
scenario kind ascending ordinal
source owner ascending ordinal
transition id ascending ordinal
overlay id ascending ordinal
node id ascending ordinal
failure key ascending ordinal
```

필수 digest:

```text
worst-case scenario input digest
scenario catalog digest
scenario proof digest
destructible/device boundary digest
worst-case combined digest
MAP19_07 handoff digest
```

Result에는 다음 안정성 증거를 기록한다.

```text
repeat digest mismatch count: 0
reverse scenario/order digest mismatch count: 0
culture digest mismatch count: 0
scenario transform order digest mismatch count: 0
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

정상 검증은 EditMode category `MAP19_06`만 선택한다.

```text
MAP19_06 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15/MAP16/MAP17/MAP18/MAP19_01/MAP19_02/MAP19_03/MAP19_04/MAP19_05 selections: 0
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
WorstCaseValidatorPassesZeroToolVillageSkippedHostileAndEvacuatedScenarios
WorstCaseValidatorModelsDestructibleTileLossAsLogicalOverlayOnly
WorstCaseValidatorModelsMovingDeviceWorstPositionAsTransitionFilterOnly
WorstCaseValidatorPreservesProtectedCriticalRoutesLandingsAndSockets
WorstCaseValidatorConsumesMap19_02ToMap19_05SurfacesReadOnly
WorstCaseValidatorRejectsMissingHandoffDigestMismatchAndMissingBindings
WorstCaseValidatorFailuresAreAtomicAndReportOwnerReasonExpectedActual
WorstCaseValidatorDigestIsStableAcrossRepeatReverseCultureAndScenarioOrder
WorstCaseValidatorPublishesMap19_07HandoffSurface
Map19HandoffKeepsMap19_07Locked
```

Focused test category:

```text
MAP19_06
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
MapDesign/MCP/REPORTS/MAP19_06_VALIDATE_WORST_CASE_SCENARIOS_RESULT.md
```

Result에는 아래 섹션을 포함한다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
## Worst-case Scenario Summary
## No Seed Regression or Physics Boundary Notes
```

`Worst-case Scenario Summary`에는 아래 값을 채운다.

```text
MAP19_05 Result SHA-256 required/actual:
MAP19_05 installed Task SHA-256 required/actual:
MAP19_02 graph digest reused:
MAP19_03 completion proof digest reused:
MAP19_04 combined digest reused:
MAP19_05 combined digest reused:
MAP19_06 incoming handoff digest reused:

scenario kinds checked:
scenario instances checked:
zero-tool scenarios checked/passed:
village skipped scenarios checked/passed:
hostile village scenarios checked/passed:
evacuated village scenarios checked/passed:
destructible tile loss scenarios checked/passed:
moving device worst-position scenarios checked/passed:
combined adverse static shell scenarios checked/passed:
scenario completion searches:
scenario goals satisfied:
scenario goals missing:
scenario proof shortest transition min/max:

destructible candidates checked:
destructible protected-critical rejects:
destructible overlays applied:
destructible scenario completions passed:
device candidates checked:
device worst-state transforms:
device fallback completions passed:
device fallback violations:

worst-case scenario input digest lower-hex SHA-256:
scenario catalog digest lower-hex SHA-256:
scenario proof digest lower-hex SHA-256:
destructible/device boundary digest lower-hex SHA-256:
worst-case combined digest lower-hex SHA-256:
MAP19_07 handoff digest lower-hex SHA-256:

repeat digest mismatch count:
reverse scenario/order digest mismatch count:
culture digest mismatch count:
scenario transform order digest mismatch count:
mutation sensitivity probes passed:

missing handoff/binding failure probes:
digest mismatch failure probes:
zero-tool/village failure probes:
destructible protected-critical failure probes:
device fallback failure probes:
combined adverse failure probes:
forbidden API or mutation failure probes:
atomic failure success digests published:
atomic failure MAP19_07 handoff digests published:

worst-case validations run:
BFS/completion searches run:
distance/revisit/pacing measurements run:
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
MAP19_07 started:
```

Allowed nonzero counters:

```text
worst-case validations run
BFS/completion searches run only for MAP19_03 pure-data completion search over scenario transforms
```

All wider verification and runtime mutation counters must be zero.

## 14. PASS 조건

PASS 조건:

```text
MAP19_05 Result and installed Task SHA match
MAP19_02 graph, MAP19_03 completion, MAP19_04 combined, MAP19_05 combined, and MAP19_06 incoming handoff digests are reused exactly
ZeroTool, VillageSkipped, VillageHostile, VillageEvacuated, DestructibleTileLoss, MovingDeviceWorstPosition, CombinedAdverseStaticShell scenarios are checked
Each success scenario completes through MAP19_03 pure-data completion search
Protected-critical routes, landings, recovery floors, envelopes, and boundary sockets are not invalidated by destruction/device transforms
Failure cases are deterministic and atomic
All required digests are stable lower-hex SHA-256
Focused MAP19_06 EditMode tests are 10/10 PASS
compile errors are 0
relevant Console errors are 0
forbidden APIs and runtime mutation counters are 0
distance/revisit/pacing, failure bundle, headless runner, seed batch, legacy regression, PlayMode, unfiltered/full regression are 0
MAP19_07 remains LOCKED / NOT STARTED
```

FAIL 조건:

```text
Any required focused test fails
Any required digest is missing or unstable
Worst-case validator mutates runtime village/shop/NPC/activity/event objects
Destructible/device scenario writes tilemap, scene, prefab, collider, rigidbody, or player state
Scenario completion requires implicit teleport, forced grant, or terrain carve
Seed batch or legacy regression runs without explicit trigger approval
MAP19_07 is started or unlocked
Failure publishes success digest or MAP19_07 handoff
Result lacks user-facing responsibility report
```

BLOCKED 조건:

```text
MAP19_05 Result SHA mismatch
MAP19_05 installed Task SHA mismatch
Required village/destructible/device binding source is not publicly consumable
Required scenario binding cannot be identified without inventing production fixture
Unity compile prevents focused tests from running before task code can be isolated
```

## 15. Status Finalize

PASS 후에만 status를 finalize한다.

Expected final status:

```text
MAP19_06_VALIDATE_WORST_CASE_SCENARIOS: COMPLETE
MAP19_07_MEASURE_DISTANCE_REVISIT_AND_PACING: LOCKED
Current Task: NONE
```

MAP19_07은 다음 파일을 내가 별도로 줄 때까지 시작하지 않는다.

## 16. Commit

PASS finalize 후 atomic commit을 만든다.

Commit subject:

```text
MAP19_06: validate worst case scenarios
```

Commit 범위:

```text
MAP19_06 production files
MAP19_06 focused test file
matching .meta files
MapDesign/MCP/TASKS/MAP19_06_VALIDATE_WORST_CASE_SCENARIOS.md
MapDesign/MCP_ARCHIVE/MAP19_06_VALIDATE_WORST_CASE_SCENARIOS.md
MapDesign/MCP/REPORTS/MAP19_06_VALIDATE_WORST_CASE_SCENARIOS_RESULT.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

금지:

```text
git push
unrelated dirty file staging
committing user changes
MAP19_07 files
```

## 17. Stop Rule

작업 완료 후 반드시 멈춘다.

```text
STOP AFTER MAP19_06 RESULT AND COMMIT.
DO NOT START MAP19_07.
```

