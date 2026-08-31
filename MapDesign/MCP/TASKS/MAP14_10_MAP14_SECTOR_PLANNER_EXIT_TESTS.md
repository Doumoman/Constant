```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP14_10_MAP14_SECTOR_PLANNER_EXIT_TESTS
  task_file: TASKS/MAP14_10_MAP14_SECTOR_PLANNER_EXIT_TESTS.md
  requires_current_task: NONE
  requires_completed_task: MAP14_09_EXPORT_DEBUG_AND_CREATE_GRAYBOX_TESTS
  requires_result:
    path: REPORTS/MAP14_09_EXPORT_DEBUG_AND_CREATE_GRAYBOX_TESTS_RESULT.md
    status: PASS
    sha256: 9d903f60b8781712eed7950c44284aba800aa6fa9dc6023b8e360ba2aed772d1
  requires_installed_task:
    path: TASKS/MAP14_09_EXPORT_DEBUG_AND_CREATE_GRAYBOX_TESTS.md
    sha256: ed399de25b62bc6d59f9e4912859e7e39f8cbebd53f0ca0b3157b75efaff72a1
  sets_current_task: MAP14_10_MAP14_SECTOR_PLANNER_EXIT_TESTS
```

# MAP14_10 - MAP14 Sector Planner Exit Tests

```text
TASK: MAP14_10_MAP14_SECTOR_PLANNER_EXIT_TESTS
PHASE: MAP14 - Cluster-first Sector Planner
STATUS: CURRENT
NEXT: MAP15_01_IMPLEMENT_WORLD_PLAN_AND_SOLVE_ORDER
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP14_01~09의 current public sector-planner chain을 하나의 focused phase-exit gate로 승인한다.

```text
MAP14_01 planner input and pacing
MAP14_02 fixed anchors
MAP14_03 cluster candidates and placement
MAP14_04 spine and traversal envelope
MAP14_05 role zones and pattern render
MAP14_06 quiet/activity/event marker plan
MAP14_07 ownership canvas
MAP14_08 local retry/RNG policy
MAP14_09 debug export and graybox catalog
→ Map14SectorPlannerExitTests
→ MAP14 PHASE EXIT verdict
→ MAP15_01 world plan/solve order input
```

이번 Task는 production 기능을 새로 구현하지 않고, MAP14의 current public code/data가 phase gate를 만족하는지 확인하는 dedicated EditMode exit test만 추가한다.

MAP14 Phase Gate:

```text
Type0/Type1/Type2/Type3/Type4/Boundary/Special 조건의 1-sector·3-sector graybox가
tile reachability, determinism, retry cap, ownership, softlock 0을 통과한다.
```

Exit approval은 MAP14 reference sector-planner layer에 대한 승인이다. 169-sector world solve, MAP15 dependency/order, Tilemap bake, MicroChunk slice, collider/physics/player PlayMode traversal, Scene/Prefab/GameObject 반영, Activity/Event runtime spawn은 아직 승인하지 않는다.

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 추가한 test script, test method별 책임, 입력→출력, route/biome/boundary/Special graybox coverage, tile-path/static reachability 수치, retry cap/ownership/softlock 수치, 회귀 작업 0, 아직 구현하지 않은 범위, Editor/게임 가시성을 반드시 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| MAP14 phase-exit focused EditMode gate | new production runtime planner |
| current MAP14_01~09 public chain integration verdict | MAP15 169-sector world solve |
| 1-sector/3-sector graybox coverage approval | Tilemap bake / MicroChunk slice / streaming |
| static tile reachability on ownership canvas | collider/physics/player PlayMode traversal |
| determinism, retry-cap and no-softlock verdict | Scene/Prefab/GameObject mutation |
| read-only debug/graybox catalog validation | EditorWindow/overlay/generated asset |
| exact approval boundary for MAP15 handoff | Activity/Event/NPC/reward gameplay spawn |

The exit test can build test-owned fixtures and helper probes inside the new test file. It cannot patch production code, upstream tests, CSV/schema, Editor windows, scenes or assets.

## 2. Focused-Only and No-Regression Policy

정상 실행은 EditMode category `MAP14_10`만 선택한다.

```text
MAP14_10 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14_01~09 category selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
```

컴파일 확인과 Console 확인은 허용한다. test selection은 `MAP14_10` category로 제한한다.

신규 task-owned failure는 신규 MAP14_10 allowlist 파일만 수정하고 `MAP14_10` category만 재실행한다.

upstream public API defect, 기존 data contradiction, 기존 Result/Task SHA mismatch, 또는 기존 production code 수정이 필요한 문제를 발견하면 기존 파일을 고치지 않는다. owner/invariant/reason/minimum verification을 Result에 기록하고 `BLOCKED`로 STOP한다.

문제가 실제로 발생하지 않은 상태에서 prior category, legacy regression, PlayMode, unfiltered test를 돌리지 않는다. 회귀가 필요하다고 판단한 경우에도 이 Task 안에서 임의로 실행하지 말고 `REGRESSION TRIGGER DETECTED: YES`와 정확한 원인을 Result에 기록한 뒤 STOP한다.

## 3. Read-Only Preflight

```text
MAP14_09 Result: PASS
MAP14_09 Result SHA-256:
9d903f60b8781712eed7950c44284aba800aa6fa9dc6023b8e360ba2aed772d1

MAP14_09 installed Task SHA-256:
ed399de25b62bc6d59f9e4912859e7e39f8cbebd53f0ca0b3157b75efaff72a1

MAP14_09 COMPLETE / MAP14_10 CURRENT / MAP15_01 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required public authority:

```text
MAP14_01: SectorPlannerInput, PacingAssignment, RouteType, AccessClass, biome/boundary/Special snapshots
MAP14_02: SectorFixedAnchorPlan, external sockets, boundary and Special anchors
MAP14_03: SectorClusterCandidatePlan and SectorClusterPlacementPlan
MAP14_04: SectorSpineEnvelopePlan, nodes, edges, ProtectedOpen and route envelope
MAP14_05: SectorClusterRolePatternPlan and SectorPatternRenderPlan
MAP14_06: SectorQuietActivityEventPlan, Quiet fill and Activity/Event marker decisions
MAP14_07: SectorCanvasOwnershipPlan, owner planes, winners, suppression and coverage
MAP14_08: SectorPlannerRetryPlan, retry policy, caps and RNG trace
MAP14_09: SectorPlannerDebugExport, failure 1-ring and graybox fixture catalog
MAP08: approved boundary pair/candidate identity exposed through MAP14/MAP14_09
MAP13: SpecialRegion fixed/reference/deferred identity exposed through MAP14/MAP14_09
```

MAP14_10 must consume public values. Do not reparse physical CSV and do not inspect private fields. If a public accessor is missing, create test-owned projection helpers only inside the new MAP14_10 test file when they can read public values. If upstream source must change, `BLOCKED`.

## 4. Exact Write Boundary

정상 범위는 phase-exit Editor EditMode test 1개와 matching meta다.

```text
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/SectorPlanning/Map14SectorPlannerExitTests.cs(.meta)
```

```text
Tests assembly: Game.Map.Editor.Tests or existing Editor EditMode map-authoring test assembly
Tests namespace: StarNight.Map.Editor.Tests.WorldGeneration.SectorPlanning
Category: MAP14_10
```

수정·생성 금지:

```text
Runtime production C#
existing C# / test / CSV / meta
Editor production C#
Authoring or Generated CSV/meta
schema registry/test
asmdef / asmref
Scene / Prefab / ScriptableObject / Tilemap / Material / Texture
Settings / Packages
PlayMode test/helper
EditorWindow / overlay / inspector
debug export file, generated report asset, JSON file, CSV file
Tilemap bake or MicroChunk slice exporter
MAP15 files
```

If the target Editor test folder does not exist, create only the minimum folder path and matching Unity folder metas if the project requires them, and report those folder metas explicitly. Do not move existing folders.

## 5. Exit Test Scope

`Map14SectorPlannerExitTests` must build the current MAP14_01~09 public chain through public APIs and publish a MAP14 exit verdict in the Result.

Minimum test gates:

```text
CurrentChainPublishesAllMap14ArtifactsForExit
GrayboxCoverageApprovesRouteBiomeBoundaryAndSpecialRequirements
OneSectorGrayboxesHaveDeterministicTileReachability
ThreeSectorGrayboxesPreserveExternalSocketsAndBoundaryContinuity
OwnershipCanvasHasFullCoverageNoDoubleOwnersAndNoForbiddenConflict
RetryPolicyCapsAbortDeterministicallyAndDoNotRepairByCarving
FailureRingExplainsAbortOrRetryWithoutMutatingSources
StaticSoftlockCandidateCountsAreZeroForRequiredRoutesAndSpecialEntrances
DeterminismHoldsAcrossRepeatReverseCultureSeedAndAttemptEvidence
NoProductionTilePhysicsScenePreviewGameplayOrFileExportMutation
InvalidExitInputsFailAtomicallyWithoutOpeningMap15
```

Use 9~12 tests. Add more only if required to keep assertions readable.

## 6. Tile Reachability Contract

MAP14_10 must include a static, test-owned tile reachability probe over the MAP14_07 ownership canvas and MAP14_04 spine/envelope evidence.

The probe must not use Unity physics, colliders, PlayMode, NavMesh, GameObject state, Tilemap components or Scene objects.

Minimum route checks:

- Every `OneSector` fixture from MAP14_09 has a path from required entry evidence to required exit/return evidence when the fixture declares a required route.
- Every `ThreeSector` fixture preserves compatible external socket continuity between center and neighbors.
- Type0/Type1/Type2/Type3/Type4/Boundary/Special route-condition tags exposed by MAP14_09 have at least one successful tile path or explicit zero-required evidence.
- Type4 keeps the standing rule: U+D mandatory, L/R independent. Legal Type4 socket states are `UD`, `LUD`, `RUD`, `LRUD`.
- Boundary bridge fixtures connect fixed boundary evidence without mutating the boundary pair/candidate identity.
- Special fixtures connect entry/return approach and do not require reward/combat/crafting/NPC execution.
- Activity/Event markers must not be required for static route completion.

The probe must report:

```text
one-sector route checks required / passed / failed
three-sector route checks required / passed / failed
required entry/exit witness count
missing witness count
socket continuity checks required / passed / failed
static softlock candidates by reason
```

If a tile path fails and the failure belongs to production planner data, report `BLOCKED` with owner and minimal evidence instead of adding fallback paths.

## 7. Determinism and Retry Cap Contract

Exit gate must approve the MAP14_08 policy and MAP14_09 graybox publication:

- same inputs repeat to the same MAP14_01~09 digest set.
- reverse input order and `tr-TR` culture do not change exit digests.
- seed and attempt evidence only changes retry/RNG digests where MAP14_08 already declares sensitivity.
- first-pass accept has zero retry nodes and zero MAP14 draw.
- synthetic recoverable retry matrix respects the declared order.
- cap aborts publish deterministic errors.
- forbidden fallback actions stay rejected.
- no exit test introduces new RNG draws beyond MAP14_08 public evidence.

Report:

```text
current digest set count
repeat/reverse/culture matched digest count
retry cap cases required / passed / failed
forbidden fallback cases required / passed / failed
new MAP14_10 RNG draws 0
```

## 8. Ownership and Softlock Contract

Exit gate must approve MAP14_07/MAP14_09 ownership evidence:

- `13,824/13,824` current reference coordinates have terrain owner or explicit no-terrain evidence.
- same-plane double owner is 0.
- forbidden overlap and unresolved conflict are 0.
- ProtectedOpen, Reservation and Marker planes remain consistent.
- Activity/Event marker-only decisions never become terrain owners.
- explicit Empty evidence remains evidence-only.
- SpecialRegion, boundary, route, cluster, pattern, Quiet and marker identities are preserved.
- static softlock candidate count is 0 for required routes, Special entry/return and boundary bridge checks.

Report:

```text
coverage coordinates
terrain/protection/reservation/marker/evidence counts
same-plane double owner / forbidden overlap / unresolved conflict
static softlock reason counts
Activity/Event terrain-owner count
Special/Village/deferred ownership mutation count
```

## 9. Graybox Coverage Contract

MAP14_10 must consume MAP14_09 fixture catalog and verify:

```text
OneSector fixtures: positive and expected count
ThreeSector fixtures: positive and expected count
FailureOneRing fixtures: positive and expected count
RouteType/condition coverage missing: 0
biome coverage missing: 0
boundary pair coverage missing: 0
SpecialRegion coverage missing: 0
PacingRole coverage missing: 0
AccessClass coverage missing: 0
ownership plane coverage missing: 0
retry stage/terminal coverage missing: 0
```

The exit Result must name the actual covered values, not only `PASS`.

## 10. Identity and No-Mutation Proof

Exit tests must prove before/after equality for:

```text
SectorPlannerInput digest
PacingAssignment digest
FixedAnchorPlan digest
ClusterPlacementPlan digest
SpineEnvelopePlan digest
SectorClusterRolePatternPlan digest
SectorPatternRenderPlan digest
SectorQuietActivityEventPlan digest
SectorCanvasOwnershipPlan digest
SectorPlannerRetryPlan digest
SectorPlannerDebugExport digest
SectorPlannerGrayboxFixtureCatalog digest
MAP12 Activity/Event authority digests consumed where public
RouteType and AccessClass identities
external socket IDs
boundary pair/candidate IDs
SpecialRegion binding and region IDs
cluster IDs, variant IDs and footprint cells
ProtectedOpen coordinates and envelope digest
MAP10 pattern render cell identities
Quiet fill cell identities
Activity/Event marker decision identities
retry RNG trace identities
debug token identities
```

The following counters must remain 0:

```text
production source mutation
Runtime production C# addition
retry execution beyond MAP14_08 evidence
new RNG draw
fallback corridor carve
validation relaxation
whole sector rerandom
whole world rerandom
fixed anchor mutation
boundary socket mutation
SpecialRegion reservation mutation
ProtectedOpen/no-write mask removal
Tilemap write
Scene/Prefab/Tilemap/GameObject mutation
EditorWindow/overlay/inspector mutation
generated debug file write
Activity runtime spawn
Event runtime spawn
reward/combat/crafting/inventory/NPC execution
MAP15 start claim
```

## 11. Expected Result Report

Result must begin:

```text
TASK: MAP14_10_MAP14_SECTOR_PLANNER_EXIT_TESTS
STATUS: PASS | FAIL | BLOCKED
MAP14 PHASE EXIT: APPROVED only when PASS
MAP14_10: COMPLETE ELIGIBLE only when PASS
MAP15_01_IMPLEMENT_WORLD_PLAN_AND_SOLVE_ORDER: LOCKED / DO NOT START
```

`## User-Facing Implementation Report` must include:

- 이번 Task가 MAP14 phase-exit focused test이며 production 기능/Tilemap/Scene/gameplay가 아니라는 점
- 추가한 test script와 각 test gate의 책임
- MAP14_01~09 artifact chain actual counts and digest summary
- OneSector / ThreeSector / FailureOneRing fixture count
- RouteType/condition, biome, boundary pair, SpecialRegion, PacingRole, AccessClass, ownership plane, retry stage coverage required/covered/missing
- tile reachability and socket continuity required/passed/failed
- ownership coverage, double-owner, conflict, marker-only and explicit Empty evidence
- retry cap, forbidden fallback and RNG separation evidence
- static softlock candidate count by reason
- MAP14_01~09 identity preservation evidence
- new RNG draw 0, file export 0, Tilemap/Scene/Prefab/GameObject/spawn 0
- 회귀를 돌리지 않았다는 증거
- 미구현 범위와 Editor/게임 가시성

`## Responsibility and Added Functions` must include:

- exact script path
- test class and test method별 책임
- helper/probe별 input -> output
- production/Editor/CSV/Scene/Prefab/Tilemap 변경 여부
- upstream 수정 여부
- downstream owner: MAP15_01

Focused verification block:

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP14_10]
discovered: <N>
executed: <N>
passed: <N>
failed: 0
skipped: 0
inconclusive: 0
compile errors: 0
relevant Console errors after final verification/clear: 0
relevant Console warnings after final verification/clear: 0

REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
```

If PASS:

```text
Commit subject: MAP14_10: approve sector planner phase exit
Push: NOT PERFORMED
```

If FAIL or BLOCKED, do not finalize Status and do not open MAP15_01.

## 12. Finalize Rules

PASS일 때만 Status Finalize와 atomic commit을 수행한다.

Commit에 포함 가능한 파일:

```text
MCP/TASKS/MAP14_10_MAP14_SECTOR_PLANNER_EXIT_TESTS.md
MCP_ARCHIVE/MAP14_10_MAP14_SECTOR_PLANNER_EXIT_TESTS.md
MCP/REPORTS/MAP14_10_MAP14_SECTOR_PLANNER_EXIT_TESTS_RESULT.md
MCP/06_IMPLEMENTATION_STATUS.md
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/SectorPlanning/Map14SectorPlannerExitTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/SectorPlanning/Map14SectorPlannerExitTests.cs.meta
```

관련 없는 dirty/untracked/staged 파일은 수정·stage·commit하지 않는다.

```text
Git push: forbidden
Next task MAP15_01: do not start
STOP after Result and optional PASS finalize commit
```
