```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP19_05_VALIDATE_REPETITION_AND_EVENT_REMOVAL
  task_file: TASKS/MAP19_05_VALIDATE_REPETITION_AND_EVENT_REMOVAL.md
  requires_current_task: NONE
  requires_completed_task: MAP19_04_VALIDATE_CLUSTER_RECOVERY_AND_DENSITY
  requires_result:
    path: REPORTS/MAP19_04_VALIDATE_CLUSTER_RECOVERY_AND_DENSITY_RESULT.md
    status: PASS
    sha256: 3d6a12fc4171b92787ccc77356a176572750ec250c6f0b140db5c005f36a128e
  requires_installed_task:
    path: TASKS/MAP19_04_VALIDATE_CLUSTER_RECOVERY_AND_DENSITY.md
    sha256: 7e6d29b179a839193c343a56bde7796db4f0385da8f955457837d2a4a7c4b188
  sets_current_task: MAP19_05_VALIDATE_REPETITION_AND_EVENT_REMOVAL
```

# MAP19_05 - Validate Repetition and Event Removal

```text
TASK: MAP19_05_VALIDATE_REPETITION_AND_EVENT_REMOVAL
PHASE: MAP19 - Traversal Validation / Seed QA Preparation
STATUS: CURRENT
NEXT: MAP19_06_VALIDATE_WORST_CASE_SCENARIOS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP19_02 graph, MAP19_03 completion proof, MAP19_04 recovery/density proof를 읽기 전용으로 소비해, generated map의 **구조 반복**과 **Activity/Event 제거 상태의 static completion**을 검증한다.

이번 Task는 두 가지만 한다.

```text
1. Pattern Mirror, Cluster 반복, Activity 반복이 너무 가까이 붙는지 검사한다.
2. 모든 Activity/Event overlay를 제거한 static shell에서도 MAP19_03 completion search가 통과하는지 검사한다.
```

이번 Task가 하지 않는 것:

```text
worst-case scenario validation
distance/revisit/pacing measurement
failure bundle or headless runner
seed batch run
production seed approval
legacy 19347 regression
PlayMode or unfiltered/full regression
runtime physics, player tuning, scene/prefab/tilemap mutation
MAP19_06 unlock or execution
```

## 1. 사용자 보고 의무

Result 첫 부분에는 반드시 아래 두 섹션을 둔다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

보고 내용은 아래 질문에 답해야 한다.

```text
이번 작업이 어떤 기능을 추가했는가?
반복 검증은 어떤 종류의 지루한/복붙 구조를 잡는가?
Activity/Event 제거 검증은 무엇을 보장하는가?
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
MapDesign/MCP/REPORTS/MAP19_04_VALIDATE_CLUSTER_RECOVERY_AND_DENSITY_RESULT.md exists
MAP19_04 Result STATUS: PASS
MAP19_04 Result SHA-256:
3d6a12fc4171b92787ccc77356a176572750ec250c6f0b140db5c005f36a128e

MapDesign/MCP/TASKS/MAP19_04_VALIDATE_CLUSTER_RECOVERY_AND_DENSITY.md exists
MAP19_04 installed task SHA-256:
7e6d29b179a839193c343a56bde7796db4f0385da8f955457837d2a4a7c4b188

MAP19_02 graph digest:
bf6b682dd5797e6dd8cb469289ca26fc8db3f10175a99f77d6316c5afe65d063

MAP19_03 completion proof digest:
6bc3bf96b766337011fd08d3436fd46e12b30ae7b0af5cfc0598646e8e338fca

MAP19_04 recovery proof digest:
b371b67691697eb719309f59131c4b85ec5c6402d64d7f183aec7d03ff1c92b3

MAP19_04 density digest:
a33ccb810d8ec39168fe222958850176821792525563e472d7eac3ad07b25b11

MAP19_04 combined digest:
6e393133118683be5104af6e4a4f2eff39cee61dc2c1dc8c240fba969512b816

MAP19_05 incoming handoff digest:
0bdcc4ff5e24e91c11156a64b04acd18508cb42e4f26e72c90a5705793db7087
```

Status 조건:

```text
Current Task before apply: NONE
MAP19_04_VALIDATE_CLUSTER_RECOVERY_AND_DENSITY: COMPLETE
MAP19_05_VALIDATE_REPETITION_AND_EVENT_REMOVAL: LOCKED before apply
MAP19_06_VALIDATE_WORST_CASE_SCENARIOS: LOCKED
```

선행 SHA/digest가 다르면 실행하지 않는다.

## 3. 입력 표면

필수 입력:

```text
GeneratedTileMovementGraph from MAP19_02
GeneratedCompletionSearch proof/search from MAP19_03
GeneratedClusterRecoveryDensityValidation result from MAP19_04
MicroPattern repetition/signature contracts from MAP10
TerrainCluster structural/role/signature contracts from MAP11/MAP14
Activity/Event authoring and placement surfaces from MAP12/MAP18
Generated slice/cell/provenance surfaces from MAP16/MAP17
```

프로젝트 타입 이름이 다르면 같은 semantic owner의 현재 public 타입을 사용한다.  
타입명을 맞추려고 이전 Phase 파일을 대규모 rename/refactor하지 않는다.

## 4. 구현 대상

권장 production 파일:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Validation/GeneratedRepetitionEventRemovalValidation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Validation/GeneratedRepetitionEventRemovalValidator.cs
```

권장 focused test 파일:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Validation/GeneratedRepetitionEventRemovalValidatorTests.cs
```

기존 naming convention이 더 적합하면 따르되, Result 책임 표에 실제 파일명을 적는다.

## 5. Repetition Validation 계약

반복 검증은 "겉보기만 다른 같은 구조"를 잡는다.

검사 대상:

```text
MicroPattern repetition signature
Pattern Mirror pair
TerrainCluster structural signature
TerrainCluster role and biome sequence
Activity archetype repetition
Activity/Event overlay placement repetition
```

필수 개념:

```text
GeneratedRepetitionValidationInput
GeneratedRepetitionValidationResult
GeneratedRepetitionSignature
GeneratedRepetitionWindow
GeneratedRepetitionFailure
```

각 signature는 아래를 구분해야 한다.

```text
visual/material-only difference
structural tile silhouette
route/spine role
movement affordance
activity/event role
source owner/provenance
```

검증 규칙:

```text
Pattern Mirror가 인접 또는 같은 cluster local band에 과도하게 반복되면 실패한다.
동일 TerrainCluster structural signature가 너무 가까운 window 안에 반복되면 실패한다.
동일 Activity archetype 또는 Event overlay가 같은 pacing band에 과도하게 반복되면 실패한다.
재질, decoration, marker label만 다른 항목은 별도 구조로 계산하지 않는다.
```

거리/페이싱의 정량 "측정 Phase"는 MAP19_07 책임이다.  
이번 Task는 반복 금지용 local/window counter와 source order만 사용한다.

필수 Result 수치:

```text
repetition sources checked:
pattern signatures checked:
pattern mirror pairs checked:
pattern mirror violations:
cluster signatures checked:
cluster repetition windows checked:
cluster repetition violations:
activity/event signatures checked:
activity/event repetition windows checked:
activity/event repetition violations:
material-only duplicate collapses:
repetition validation digest:
```

## 6. Activity/Event Removal 계약

Activity/Event 제거 검증은 static terrain shell의 완주 가능성을 증명한다.

검증 방법:

```text
1. 기존 generated graph/proof를 읽기 전용으로 받는다.
2. ActivityStructure와 EventOverlay에서 온 transition/action만 제거한 removal scenario를 만든다.
3. TerrainCluster, SpecialRegion, mandatory population, forge/seal/boss/special static goal은 유지한다.
4. MAP19_03 completion search 계약을 사용해 제거 상태 completion proof를 다시 계산한다.
5. 실패 시 제거된 activity/event에 의존하던 target, transition, route를 deterministic하게 보고한다.
```

금지:

```text
activity object disable/enable
event object disable/enable
runtime scene query
tile carve rescue
implicit teleport
forced transition grant
changing generated graph
changing generated slots
```

필수 Result 수치:

```text
activity structures removed:
event overlays removed:
static transitions retained:
removed transitions:
removal scenario completion searches:
removal scenario goals satisfied:
removal scenario goals missing:
removal scenario proof shortest transition count:
removal dependency violations:
removal validation digest:
```

## 7. Failure and Atomicity

실패는 deterministic하게 보고한다.

필수 failure fields:

```text
owner
reason
case id
offending key
expected
actual
source digest
window or completion frontier evidence
```

focused test에는 다음 failure probe를 포함한다.

```text
missing MAP19_04 handoff
MAP19_04 digest mismatch
pattern mirror violation
cluster structural repetition violation
activity repetition violation
material-only duplicate collapse
removal completion failure
removed transition dependency failure
forbidden runtime mutation attempt
MAP19_06 start attempt
```

Failure 이후 다음 surface가 게시되면 FAIL이다.

```text
repetition success digest
removal success digest
MAP19_06 handoff digest
```

원자성 증거:

```text
atomic failure success digests published: 0
atomic failure MAP19_06 handoff digests published: 0
```

## 8. Determinism and Digest

모든 collection은 canonical 정렬한다.

정렬 기준:

```text
source owner ascending ordinal
signature id ascending ordinal
window id ascending ordinal
cluster id ascending ordinal
pattern id ascending ordinal
activity/event id ascending ordinal
state key ascending ordinal
failure key ascending ordinal
```

필수 digest:

```text
repetition validation input digest
repetition validation digest
event removal input digest
event removal proof digest
repetition+removal combined digest
MAP19_06 handoff digest
```

Result에는 다음 안정성 증거를 기록한다.

```text
repeat digest mismatch count: 0
reverse source/order digest mismatch count: 0
culture digest mismatch count: 0
signature order digest mismatch count: 0
removal transition order digest mismatch count: 0
mutation sensitivity probes passed:
```

## 9. 금지 API 스캔

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

## 10. Focused-only 검증 정책

정상 검증은 EditMode category `MAP19_05`만 선택한다.

```text
MAP19_05 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15/MAP16/MAP17/MAP18/MAP19_01/MAP19_02/MAP19_03/MAP19_04 selections: 0
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

다음 test name을 그대로 포함한다.

```text
RepetitionValidatorDetectsPatternMirrorClusterAndActivityWindows
RepetitionValidatorCollapsesMaterialOnlyDuplicatesWithoutChangingStructure
EventRemovalValidatorCompletesStaticShellAfterRemovingActivitiesAndEvents
EventRemovalValidatorKeepsSpecialMandatoryForgeSealBossGoalsExplicit
RepetitionEventRemovalConsumesMap19_02ToMap19_04SurfacesReadOnly
RepetitionEventRemovalRejectsMissingHandoffAndDigestMismatches
RepetitionEventRemovalFailuresAreAtomicAndReportOwnerReasonExpectedActual
RepetitionEventRemovalDigestIsStableAcrossRepeatReverseCultureAndOrder
RepetitionEventRemovalPublishesMap19_06HandoffSurface
Map19HandoffKeepsMap19_06Locked
```

Focused test category:

```text
MAP19_05
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
MapDesign/MCP/REPORTS/MAP19_05_VALIDATE_REPETITION_AND_EVENT_REMOVAL_RESULT.md
```

Result에는 아래 섹션을 포함한다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
## Repetition and Event Removal Summary
## No Seed Regression or Physics Boundary Notes
```

`Repetition and Event Removal Summary`에는 아래 값을 채운다.

```text
MAP19_04 Result SHA-256 required/actual:
MAP19_04 installed Task SHA-256 required/actual:
MAP19_02 graph digest reused:
MAP19_03 completion proof digest reused:
MAP19_04 combined digest reused:
MAP19_05 incoming handoff digest reused:

repetition sources checked:
pattern signatures checked:
pattern mirror pairs checked:
pattern mirror violations:
cluster signatures checked:
cluster repetition windows checked:
cluster repetition violations:
activity/event signatures checked:
activity/event repetition windows checked:
activity/event repetition violations:
material-only duplicate collapses:
repetition validation input digest lower-hex SHA-256:
repetition validation digest lower-hex SHA-256:

activity structures removed:
event overlays removed:
static transitions retained:
removed transitions:
removal scenario completion searches:
removal scenario goals satisfied:
removal scenario goals missing:
removal scenario proof shortest transition count:
removal dependency violations:
event removal input digest lower-hex SHA-256:
event removal proof digest lower-hex SHA-256:

repetition+removal combined digest lower-hex SHA-256:
MAP19_06 handoff digest lower-hex SHA-256:

repeat digest mismatch count:
reverse source/order digest mismatch count:
culture digest mismatch count:
signature order digest mismatch count:
removal transition order digest mismatch count:
mutation sensitivity probes passed:

missing handoff failure probes:
digest mismatch failure probes:
repetition violation failure probes:
material-only duplicate failure probes:
removal completion failure probes:
removed dependency failure probes:
forbidden API or mutation failure probes:
atomic failure success digests published:
atomic failure MAP19_06 handoff digests published:

repetition validations run:
event removal validations run:
BFS/completion searches run:
worst-case validations run:
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
MAP19_06 started:
```

Allowed nonzero counters:

```text
repetition validations run
event removal validations run
BFS/completion searches run only for MAP19_03 pure-data completion search over the removal scenario
```

All wider verification and runtime mutation counters must be zero.

## 13. PASS 조건

PASS 조건:

```text
MAP19_04 Result and installed Task SHA match
MAP19_02 graph, MAP19_03 completion, MAP19_04 combined, and MAP19_05 incoming handoff digests are reused exactly
Pattern Mirror, Cluster repetition, and Activity/Event repetition checks produce deterministic evidence
Material-only differences are collapsed and not counted as structural variety
All Activity/Event overlays can be removed while static shell completion still passes
Removed transition dependency violations are zero in success source
Failure cases are deterministic and atomic
All required digests are stable lower-hex SHA-256
Focused MAP19_05 EditMode tests are 10/10 PASS
compile errors are 0
relevant Console errors are 0
forbidden APIs and runtime mutation counters are 0
seed batch, legacy regression, PlayMode, unfiltered/full regression are 0
MAP19_06 remains LOCKED / NOT STARTED
```

FAIL 조건:

```text
Any required focused test fails
Any required digest is missing or unstable
Repetition validator treats material-only changes as structural uniqueness
Activity/Event removal requires runtime object state or terrain mutation
Static shell completion fails after Activity/Event removal
Seed batch or legacy regression runs without explicit trigger approval
MAP19_06 is started or unlocked
Failure publishes success digest or MAP19_06 handoff
Result lacks user-facing responsibility report
```

BLOCKED 조건:

```text
MAP19_04 Result SHA mismatch
MAP19_04 installed Task SHA mismatch
Required repetition signature source is not publicly consumable
Required Activity/Event removal binding cannot be identified without inventing production fixture
Unity compile prevents focused tests from running before task code can be isolated
```

## 14. Status Finalize

PASS 후에만 status를 finalize한다.

Expected final status:

```text
MAP19_05_VALIDATE_REPETITION_AND_EVENT_REMOVAL: COMPLETE
MAP19_06_VALIDATE_WORST_CASE_SCENARIOS: LOCKED
Current Task: NONE
```

MAP19_06은 다음 파일을 내가 별도로 줄 때까지 시작하지 않는다.

## 15. Commit

PASS finalize 후 atomic commit을 만든다.

Commit subject:

```text
MAP19_05: validate repetition and event removal
```

Commit 범위:

```text
MAP19_05 production files
MAP19_05 focused test file
matching .meta files
MapDesign/MCP/TASKS/MAP19_05_VALIDATE_REPETITION_AND_EVENT_REMOVAL.md
MapDesign/MCP_ARCHIVE/MAP19_05_VALIDATE_REPETITION_AND_EVENT_REMOVAL.md
MapDesign/MCP/REPORTS/MAP19_05_VALIDATE_REPETITION_AND_EVENT_REMOVAL_RESULT.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

금지:

```text
git push
unrelated dirty file staging
committing user changes
MAP19_06 files
```

## 16. Stop Rule

작업 완료 후 반드시 멈춘다.

```text
STOP AFTER MAP19_05 RESULT AND COMMIT.
DO NOT START MAP19_06.
```

