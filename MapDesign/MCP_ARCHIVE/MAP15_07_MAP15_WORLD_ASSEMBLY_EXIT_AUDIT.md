```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP15_07_MAP15_WORLD_ASSEMBLY_EXIT_AUDIT
  task_file: TASKS/MAP15_07_MAP15_WORLD_ASSEMBLY_EXIT_AUDIT.md
  requires_current_task: NONE
  requires_completed_task: MAP15_06_EXPORT_OVERLAY_AND_BATCH_TEST_WORLD_PLANS
  requires_result:
    path: REPORTS/MAP15_06_EXPORT_OVERLAY_AND_BATCH_TEST_WORLD_PLANS_RESULT.md
    status: PASS
    sha256: 556d28f238c3e19b5c1a12b1d356f21f7ba62f6287fb7ca96bed82a01b27a4f1
  requires_installed_task:
    path: TASKS/MAP15_06_EXPORT_OVERLAY_AND_BATCH_TEST_WORLD_PLANS.md
    sha256: 9572300253a1e33829eb2ac14f7fa4a3ebaf3e24600517a6990df02f06d81156
  sets_current_task: MAP15_07_MAP15_WORLD_ASSEMBLY_EXIT_AUDIT
```

# MAP15_07 - MAP15 World Assembly Exit Audit

```text
TASK: MAP15_07_MAP15_WORLD_ASSEMBLY_EXIT_AUDIT
PHASE: MAP15 - 169-sector World Assembly
STATUS: CURRENT
NEXT: MAP16_01_FINALIZE_CANVAS_LAYERS_AND_FIXED_PRECEDENCE
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP15_01~06의 public world-assembly chain을 하나의 focused phase-exit audit으로 검수한다.

```text
MAP15_01 world topology and solve order
MAP15_02 intersector sockets and boundaries
MAP15_03 multi-sector Special and cluster policy
MAP15_04 world pacing, density and repetition
MAP15_05 neighbor rollback and failure report
MAP15_06 overlay export and focused batch report
-> Map15WorldAssemblyExitAuditTests
-> MAP15 PHASE EXIT verdict
-> MAP16_01 canvas layer/fixed precedence input
```

이번 Task는 production 기능을 새로 구현하지 않는다. MAP15_01~06이 이미 공개한 immutable model/result/digest를 읽어, MAP15 phase gate를 만족하는지 focused EditMode test로 확인한다.

중요: 기존 master 문구의 "MAP00~14 회귀 승인"은 이번 Task에서 **실행하지 않는다**. 문제나 baseline drift가 실제로 발견되지 않는 한 prior category, legacy 19347, PlayMode, unfiltered regression은 금지다. MAP15_07은 회귀를 돌리는 작업이 아니라 `REGRESSION TRIGGER DETECTED: NO`와 regression selection `0`을 Result에 증명하는 작업이다.

MAP15 Phase Gate:

```text
starter reference batch에서 external socket asymmetry 0
reservation conflict not typed 0
missing required boundary pair 0
pacing/density/repetition accepted-case violation 0
rollback scope max exceeded 0
solver upper-bound violation 0
whole-world rerandom/fallback carve/silent widening 0
MAP15_01~06 digest chain preserved
MAP16_01 remains locked
```

Exit approval은 MAP15 reference world assembly contract에 대한 승인이다. 실제 full-world terrain solve, 624x416 Tilemap bake, MicroChunk 12x8 slice/streaming, collider/physics/player PlayMode traversal, Scene/Prefab/GameObject 반영, Activity/Event runtime spawn 및 production seed approval은 아직 승인하지 않는다.

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 추가한 test script, test method별 책임, 입력->출력, phase gate 수치, digest chain, regression selection 0, 아직 구현하지 않은 범위, Editor/게임 가시성을 반드시 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| MAP15 phase-exit focused EditMode audit | new Runtime production model |
| MAP15_01~06 public chain integration verdict | actual full-world terrain solve |
| external socket, boundary, reservation, pacing, rollback gate count | Tilemap bake |
| overlay/batch report consistency check | MicroChunk 12x8 slice/streaming |
| digest chain preservation and replay check | collider/physics/player traversal |
| solver bound and no-fallback proof | Scene/Prefab/GameObject mutation |
| regression trigger absence and selection zero proof | Activity/Event/NPC/reward gameplay spawn |
| exact approval boundary for MAP16 handoff | production seed approval |

The exit audit can build test-owned probes inside the new test file. It cannot patch production code, upstream tests, CSV/schema, Editor windows, scenes or assets.

## 2. Focused-Only and No-Regression Policy

정상 실행은 EditMode category `MAP15_07`만 선택한다.

```text
MAP15_07 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15_01/MAP15_02/MAP15_03/MAP15_04/MAP15_05/MAP15_06 category selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
full regression runs: 0
```

컴파일 확인과 Console 확인은 허용한다. test selection은 `MAP15_07` category로 제한한다.

신규 task-owned failure는 신규 MAP15_07 allowlist 파일만 수정하고 `MAP15_07` category만 재실행한다.

upstream public API defect, 기존 data contradiction, MAP15_01~06 digest/count mismatch, 기존 Result/Task SHA mismatch, 또는 기존 production code 수정이 필요한 문제를 발견하면 기존 파일을 고치지 않는다. owner/invariant/reason/minimum verification을 Result에 기록하고 `BLOCKED`로 STOP한다.

문제가 실제로 발생하지 않은 상태에서 prior category, legacy regression, PlayMode, unfiltered test를 돌리지 않는다. 회귀가 필요하다고 판단한 경우에도 이 Task 안에서 임의로 실행하지 말고 `REGRESSION TRIGGER DETECTED: YES`와 정확한 원인을 Result에 기록한 뒤 STOP한다.

## 3. Read-Only Preflight

```text
MAP15_06 Result: PASS
MAP15_06 Result SHA-256:
556d28f238c3e19b5c1a12b1d356f21f7ba62f6287fb7ca96bed82a01b27a4f1

MAP15_06 installed Task SHA-256:
9572300253a1e33829eb2ac14f7fa4a3ebaf3e24600517a6990df02f06d81156

MAP15_06 COMPLETE / MAP15_07 CURRENT / MAP16_01 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required public authority:

```text
MAP14: sector-local phase-exit handoff digest and no-fallback contract
MAP15_01: WorldPlanInput, WorldSolveOrderResult, 169 sector topology and solve-step order
MAP15_02: WorldIntersectorEdgePlan, 312 internal edges, endpoint/socket/boundary facts
MAP15_03: WorldMultiSectorReservationPlan, Special transactions, claims, edge locks and typed conflicts
MAP15_04: WorldPacingDensityPlan, windows, budgets, signatures, caps and violation evidence
MAP15_05: WorldNeighborRollbackPlan, rollback scope, failure report and decision evidence
MAP15_06: WorldAssemblyOverlayExport, WorldBatchPlanReport and solver upper-bound report
```

MAP15_07 must consume public values. Do not reparse physical CSV unless an approved public importer/API explicitly exposes that data as the source of truth. Do not inspect private fields. If a public accessor is missing, create test-owned projection helpers only inside the new MAP15_07 test file when they can read public values. If upstream source must change, `BLOCKED`.

Do not create generated debug files. The only user-facing persisted file from this work is the normal `*_RESULT.md` written by the MCP workflow.

## 4. Exact Write Boundary

정상 범위는 phase-exit EditMode test 1개와 matching meta다.

```text
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/SectorPlanning/Map15WorldAssemblyExitAuditTests.cs(.meta)
```

```text
Tests assembly: Game.Map.Editor.Tests or existing Editor EditMode map-authoring test assembly
Tests namespace: StarNight.Map.Editor.Tests.WorldGeneration.SectorPlanning
Category: MAP15_07
```

If the target Editor test folder does not exist, create only the minimum folder path and matching Unity folder metas if the project requires them, and report those folder metas explicitly. Do not move existing folders.

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
MAP16 files
```

If the new test cannot compile without changing production code or asmdef/asmref, do not change those files. Report `BLOCKED` with exact symbol/API/assembly reference needed.

## 5. Exit Test Scope

`Map15WorldAssemblyExitAuditTests` must build the current MAP15_01~06 public chain through public APIs and publish a MAP15 exit verdict in the Result.

Minimum test gates:

```text
CurrentMap15ChainPublishesAllRequiredArtifactsForExit
WorldTopologyAndIntersectorEdgesMatchApproved169And312Counts
ExternalSocketAndBoundaryObligationsHaveNoMissingOrAsymmetricEdges
ReservationPolicyHasNoUntypedConflictsAndPreservesSpecialPriority
PacingDensityAndRepetitionGateHasNoAcceptedCaseViolations
NeighborRollbackFailureContainmentStaysWithinInBoundsOneRing
OverlayBatchReportHasFourFocusedCasesAndNoProductionSeedApproval
DigestChainAndReplayRemainDeterministicAcrossRepeatReverseAndCulture
SolverBoundsAndForbiddenFallbackCountersRemainZero
NoRegressionSelectionTilemapScenePrefabGameplayOrFileExportMutation
InvalidExitInputsFailAtomicallyWithoutOpeningMap16
```

Tests may include static helpers in the test file. Helpers must be test-owned and cannot become production planners.

## 6. Minimum Evidence Required

Result must report these values:

```text
world sectors observed: 169/169
sector grid observed: 13x13
world size observed: 624x416
sector size observed: 48x32
internal edges observed: 312/312
edge endpoints observed: 624/624
overlay sectors observed: 169/169
overlay edges observed: 312/312
overlay layers required/covered/missing: 12/12/0
hash records required/covered/missing: 10/10/0
batch cases required/covered/passing/missing: 4/4/4/0
solver upper-bound records required/covered/violated: 10/10/0
external socket asymmetry: 0
missing required boundary pairs: 0
untyped reservation conflicts: 0
accepted pacing-density violations: 0
recent-use repeat violations in accepted case: 0
rollback scope max exceeded: 0
whole-world rerandom actions: 0
fallback carve actions: 0
silent widening actions: 0
generated file writes: 0
Tilemap/Scene/Prefab/GameObject mutation: 0
production seed approvals: 0
MAP15_01~06 digest records valid: actual/actual
MAP15 exit verdict: PASS
MAP16_01 automatic open: false
repeat/reverse/culture digest mismatches: 0
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
FULL REGRESSION RUNS: 0
```

Do not assert exact counts that depend on private or physical CSV internals. Assert exact counts only when they are public approved constants or produced by MAP15_01~06 public models.

## 7. Audit Digest and Determinism Rules

The test may define a test-owned audit digest for reporting.

All digest input must be canonical:

```text
UTF-8
LF newlines
InvariantCulture
stable enum names
stable lower-hex SHA-256
MAP15_01~06 hash records sorted by task id and record kind
sector tokens sorted by row-major sector id
edge tokens sorted by public edge id
batch cases sorted by label
upper-bound records sorted by bound kind then owner
exit gate observations sorted by gate id
no Dictionary iteration order dependency
no current time
no random API
no filesystem path separators in digest payload
no Unity object instance IDs
```

Repeat, reversed enumeration and `tr-TR` culture replay must return the same exit verdict and audit digest.

## 8. No Mutation Proof

MAP15_07 must prove it does not write or mutate:

```text
MAP14 sector planner outputs
MAP15_01 world plan or solve order outputs
MAP15_02 intersector edge outputs
MAP15_03 reservation policy outputs
MAP15_04 pacing-density outputs
MAP15_05 rollback/failure report outputs
MAP15_06 overlay/batch outputs
MAP09~14 authoring CSV/cache
Generated CSV files
debug export files
JSON files
Tilemap cells
Scene/Prefab/GameObject
ScriptableObject assets
EditorWindow/overlay/inspector state
Activity/Event/NPC/reward/combat/crafting/inventory runtime state
WorldGenerationRoot execution wiring
```

No generated file export, no actual full-world terrain execution, no runtime scene wiring and no MAP16 task execution is allowed in this Task.

## 9. Expected Result Report

Result must begin:

```text
TASK: MAP15_07_MAP15_WORLD_ASSEMBLY_EXIT_AUDIT
STATUS: PASS | FAIL | BLOCKED
MAP15_07: COMPLETE ELIGIBLE only when PASS
MAP16_01_FINALIZE_CANVAS_LAYERS_AND_FIXED_PRECEDENCE: LOCKED / DO NOT START
```

`## User-Facing Implementation Report` must include:

- 이번 Task가 MAP15 phase-exit focused audit이며 새 Runtime 기능/Tilemap/Scene/gameplay가 아니라는 점
- 추가한 test script와 각 test method 책임
- 새로 승인되는 기능 범위
- phase gate 수치
- world topology, edge, overlay, batch, bound count
- external socket, boundary, reservation, pacing, rollback verdict
- MAP15_01~06 digest chain and optional audit digest
- production seed approval 0
- whole-world rerandom/fallback carve/silent widening 0
- mutation/file-write/Scene/Prefab/Tilemap/GameObject/spawn 0
- 회귀를 돌리지 않았다는 증거
- 아직 구현하지 않은 범위와 Editor/게임 가시성

`## Responsibility and Added Functions` must include:

- exact script path
- test method별 책임
- helper/probe별 input -> output
- public authority consumed
- production/Editor/CSV/Scene/Prefab/Tilemap 변경 여부
- upstream 수정 여부
- downstream owner: MAP16_01

Focused verification block:

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP15_07]
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
FULL REGRESSION RUNS: 0
```

If PASS:

```text
Commit subject: MAP15_07: audit world assembly exit
Push: NOT PERFORMED
```

If FAIL or BLOCKED, do not finalize Status and do not open MAP16_01.

## 10. Finalize Rules

PASS일 때만 Status Finalize와 atomic commit을 수행한다.

Commit에 포함 가능한 파일:

```text
MCP/TASKS/MAP15_07_MAP15_WORLD_ASSEMBLY_EXIT_AUDIT.md
MCP_ARCHIVE/MAP15_07_MAP15_WORLD_ASSEMBLY_EXIT_AUDIT.md
MCP/REPORTS/MAP15_07_MAP15_WORLD_ASSEMBLY_EXIT_AUDIT_RESULT.md
MCP/06_IMPLEMENTATION_STATUS.md
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/SectorPlanning/Map15WorldAssemblyExitAuditTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/SectorPlanning/Map15WorldAssemblyExitAuditTests.cs.meta
```

If the Editor test folder is newly created and Unity requires folder metas, include only the minimum folder `.meta` files and report them explicitly.

관련 없는 dirty/untracked/staged 파일은 수정·stage·commit하지 않는다.

```text
Git push: forbidden
Next task MAP16_01: do not start
STOP after Result and optional PASS finalize commit
```
