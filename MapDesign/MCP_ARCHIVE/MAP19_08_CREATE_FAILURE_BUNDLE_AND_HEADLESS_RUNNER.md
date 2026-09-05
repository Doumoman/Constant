```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP19_08_CREATE_FAILURE_BUNDLE_AND_HEADLESS_RUNNER
  task_file: TASKS/MAP19_08_CREATE_FAILURE_BUNDLE_AND_HEADLESS_RUNNER.md
  requires_current_task: NONE
  requires_completed_task: MAP19_07_MEASURE_DISTANCE_REVISIT_AND_PACING
  requires_result:
    path: REPORTS/MAP19_07_MEASURE_DISTANCE_REVISIT_AND_PACING_RESULT.md
    status: PASS
    sha256: c563b6ed68dd803e616772a9f93456862466926188e97ba17e83411c41d33c28
  requires_installed_task:
    path: TASKS/MAP19_07_MEASURE_DISTANCE_REVISIT_AND_PACING.md
    sha256: e6aff8cdc2605fd2c6460c85f8ead980c79c1a76890e63151732cb29f2dea1b3
  sets_current_task: MAP19_08_CREATE_FAILURE_BUNDLE_AND_HEADLESS_RUNNER
```

# MAP19_08 - Create Failure Bundle and Headless Runner

```text
TASK: MAP19_08_CREATE_FAILURE_BUNDLE_AND_HEADLESS_RUNNER
PHASE: MAP19 - Traversal Validation / Seed QA Preparation
STATUS: CURRENT
NEXT: MAP19_09_MAP19_SCALE_AND_EXIT_AUDIT
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP19_02~07의 graph/proof/validation/measurement surface를 읽기 전용으로 소비해, 실패를 재현·분석하기 위한 **failure bundle 계약**과 다음 Task가 사용할 **headless runner shell**을 만든다.

이번 Task는 도구를 만든다.  
대량 seed 실행, production seed approval, 1k/10k/100k scale audit은 하지 않는다.

이번 Task의 책임:

```text
1. seed, version, hash, pass decision, failure owner/reason, 좌표, provenance를 담는 failure bundle payload를 정의한다.
2. intermediate pass CSV/text snapshot과 screenshot reference slot을 bundle manifest에 포함한다.
3. range/worker/replay runner argument와 partition plan을 deterministic하게 만든다.
4. runner가 MAP19_02~07 validation chain을 어떤 순서로 호출할지 dry-run plan으로 증명한다.
5. focused test에서 test-only single/few-case runner dry-run과 failure bundle serialization을 검증한다.
6. MAP19_09 scale audit이 소비할 runner/bundle handoff surface를 게시한다.
```

금지:

```text
MAP19_09 scale audit execution
1k seed run
10k seed run
100k seed run
production seed approval
legacy 19347 regression
prior task category selection
PlayMode selection
unfiltered test run
full regression run
real screenshot capture from camera
game camera creation or mutation
Scene / Prefab / Tilemap mutation
runtime object instantiate, enable, disable, destroy
runtime village/shop/NPC/activity/event mutation
runtime destructible tile mutation
runtime moving device simulation
PlayerController / Rigidbody / Collider / Physics2D behavior change
actual movement tuning
Physics2D query or simulation
NavMesh or pathfinding setup
new tile movement graph generation or mutation
MAP19_01 profile/rule registry mutation
MAP19_02 graph digest rewrite
MAP19_03 proof rewrite
MAP19_04 recovery/density proof rewrite
MAP19_05 repetition/removal proof rewrite
MAP19_06 worst-case proof rewrite
MAP19_07 measurement proof rewrite
optimization rewrite or broad refactor
shared fixture consolidation
MAP19_09 unlock or execution
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
failure bundle에는 어떤 정보가 들어가는가?
headless runner는 무엇을 실행할 수 있게 준비하는가?
이번 Task에서 실제 seed scale 실행을 하지 않았다는 증거는 무엇인가?
Runtime 코드와 Editor/CLI 도구 책임은 어떻게 분리했는가?
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
MapDesign/MCP/REPORTS/MAP19_07_MEASURE_DISTANCE_REVISIT_AND_PACING_RESULT.md exists
MAP19_07 Result STATUS: PASS
MAP19_07 Result SHA-256:
c563b6ed68dd803e616772a9f93456862466926188e97ba17e83411c41d33c28

MapDesign/MCP/TASKS/MAP19_07_MEASURE_DISTANCE_REVISIT_AND_PACING.md exists
MAP19_07 installed task SHA-256:
e6aff8cdc2605fd2c6460c85f8ead980c79c1a76890e63151732cb29f2dea1b3

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

MAP19_07 measurement combined digest:
b937f9db37715d37cecdee3e313f5ec2c016b7d9bf3c2d8e5423b3c8205429b1

MAP19_08 incoming handoff digest:
d732453f06b6dc972f6e7c34ac3f8cf86ccc285f50c8e7afca65018fd0515090
```

Status 조건:

```text
Current Task before apply: NONE
MAP19_07_MEASURE_DISTANCE_REVISIT_AND_PACING: COMPLETE
MAP19_08_CREATE_FAILURE_BUNDLE_AND_HEADLESS_RUNNER: LOCKED before apply
MAP19_09_MAP19_SCALE_AND_EXIT_AUDIT: LOCKED
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
GeneratedDistancePacingValidation result from MAP19_07
World seed/version/hash surfaces from existing public generator contracts where available
Pass/provenance/failure owner surfaces from MAP09~18 where public
```

프로젝트 타입 이름이 다르면 같은 semantic owner의 현재 public 타입을 사용한다.  
타입명을 맞추려고 이전 Phase 파일을 대규모 rename/refactor하지 않는다.

world-scale source가 없으면 `REFERENCE` 또는 `FOCUSED_FIXTURE` source kind를 명시한다.  
production seed approval을 주장하지 않는다.

## 4. 구현 대상

권장 Runtime production 파일:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Validation/GeneratedFailureBundle.cs
Assets/_Game/Map/Runtime/WorldGeneration/Validation/GeneratedValidationRunnerPlan.cs
```

권장 Editor/CLI 도구 파일:

```text
Assets/_Game/Map/Editor/WorldGeneration/Validation/GeneratedHeadlessValidationRunner.cs
```

권장 focused test 파일:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Validation/GeneratedFailureBundleHeadlessRunnerTests.cs
```

기존 naming convention이 더 적합하면 따르되, Result 책임 표에 실제 파일명을 적는다.

Runtime 파일은 순수 데이터 계약과 canonical serialization만 소유한다.  
Editor/CLI 파일만 명시된 output directory에 파일을 쓸 수 있다.

## 5. Failure Bundle 계약

Failure bundle은 실패 재현에 필요한 정보 묶음이다.

필수 개념:

```text
GeneratedFailureBundleManifest
GeneratedFailureBundleSeedIdentity
GeneratedFailureBundleVersionIdentity
GeneratedFailureBundlePassSnapshot
GeneratedFailureBundleFailureRecord
GeneratedFailureBundleCoordinateRecord
GeneratedFailureBundleProvenanceRecord
GeneratedFailureBundleAttachmentReference
GeneratedFailureBundleDigest
```

필수 필드:

```text
bundle id
bundle schema version
generator version
data version
world seed or reference seed
source kind
content hash
validation phase id
failing task id
pass decision
failed rule id
failure owner
failure reason
offending key
expected
actual
sector coordinate
slice coordinate
cell coordinate
node id
edge id
marker id
scenario id
provenance chain
upstream digest chain MAP19_02..MAP19_07
intermediate pass snapshot references
screenshot reference slots
created-at policy
canonical bundle digest
```

시간 필드 정책:

```text
created-at must be supplied by caller or omitted from digest
bundle digest must not depend on wall-clock time
runner duration may be reported separately but not included in canonical digest
```

Screenshot 정책:

```text
Screenshot capture is not implemented in this task.
Bundle contains deterministic screenshot reference slots only.
Each slot can hold path, role, coordinate focus, and expected later producer.
Camera read/write/capture count must remain 0.
```

CSV/text snapshot 정책:

```text
Runtime bundle can produce canonical CSV/text payload strings in memory.
Editor/CLI writer may write those strings only under the explicit output directory.
No AssetDatabase import, no project asset mutation, no scene write.
```

## 6. Headless Runner Shell 계약

Headless runner는 다음 Task의 scale audit이 사용할 CLI/Editor entry surface다.

필수 개념:

```text
GeneratedValidationRunnerArguments
GeneratedValidationRunnerRange
GeneratedValidationRunnerWorkerPartition
GeneratedValidationRunnerReplayRequest
GeneratedValidationRunnerPlan
GeneratedValidationRunnerResult
GeneratedValidationRunnerExitCode
```

필수 argument:

```text
--mode plan|replay|range
--seed
--seed-start
--seed-count
--worker-index
--worker-count
--output
--fail-fast
--profile
--expected-generator-version
--expected-data-version
```

이번 Task에서 허용되는 runner 실행:

```text
argument parse
range partition planning
replay request construction
dry-run plan construction
test-only synthetic validation callback
single focused failure bundle write to temporary test output directory
```

이번 Task에서 금지되는 runner 실행:

```text
production seed range validation
1k/10k/100k audit
Unity batchmode process spawn
external process launch
PlayMode run
Scene load
Tilemap bake
Collider/physics validation
real screenshot capture
production seed approval
```

Runner exit code는 deterministic enum으로 둔다.

```text
Success
ValidationFailed
InvalidArguments
PreconditionMismatch
OutputWriteFailed
InternalError
```

## 7. Output and File I/O Boundary

Runtime production code:

```text
may create in-memory manifest/CSV/text strings
may compute canonical digest
must not use System.IO
must not use UnityEngine or UnityEditor
```

Editor/CLI tooling:

```text
may use UnityEditor if placed in an Editor-only folder or assembly
may use System.IO only for explicit --output or test temporary output directory
may create directories and files only under that output root
must reject empty, project Assets, ProjectSettings, Packages, Library, Temp, Logs, or parent-traversal output roots unless test temp root is explicitly supplied
must not call AssetDatabase.Refresh or import generated bundle files as assets
must not mutate scenes, prefabs, tilemaps, colliders, rigidbodies, or runtime objects
```

Required output files for one bundle:

```text
manifest.json
pass_snapshots.csv
failures.csv
coordinates.csv
provenance.csv
screenshot_references.csv
runner_plan.json
```

Focused tests may write these files to a temporary directory and delete or leave them according to existing test convention.  
Result must report exact test-only output file count and production output count separately.

## 8. Failure and Atomicity

실패는 deterministic하게 보고한다.

필수 failure fields:

```text
owner
reason
bundle id or runner request id
offending key
expected
actual
source digest
output path when relevant
```

focused test에는 다음 failure probe를 포함한다.

```text
missing MAP19_07 handoff
MAP19_07 digest mismatch
missing seed/version/hash identity
missing failure owner or reason
invalid coordinate/provenance chain
invalid runner arguments
invalid worker partition
unsafe output directory
output writer failure
attempted production seed range execution
attempted screenshot capture
MAP19_09 start attempt
```

Failure 이후 다음 surface가 게시되면 FAIL이다.

```text
failure bundle success digest
runner plan success digest
MAP19_09 handoff digest
```

원자성 증거:

```text
atomic failure success digests published: 0
atomic failure MAP19_09 handoff digests published: 0
```

## 9. Determinism and Digest

모든 collection은 canonical 정렬한다.

정렬 기준:

```text
pass id ascending ordinal
failure owner ascending ordinal
failure key ascending ordinal
coordinate sector/slice/cell ascending numeric
provenance owner/key ascending ordinal
attachment role/path ascending ordinal
runner seed ascending unsigned numeric
worker index ascending numeric
```

필수 digest:

```text
failure bundle schema digest
failure bundle manifest digest
failure bundle payload digest
runner argument schema digest
runner plan digest
MAP19_09 handoff digest
```

Result에는 다음 안정성 증거를 기록한다.

```text
repeat digest mismatch count: 0
reverse input/order digest mismatch count: 0
culture digest mismatch count: 0
attachment/order digest mismatch count: 0
runner partition order digest mismatch count: 0
mutation sensitivity probes passed:
```

## 10. 금지 API 스캔

Runtime production files must not reference:

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
Process.Start
```

Editor/CLI tooling may reference:

```text
UnityEditor
System.IO
Environment.GetCommandLineArgs
```

Editor/CLI tooling must not reference:

```text
Physics2D
GameObject
Transform
MonoBehaviour
Tilemap
Collider
Rigidbody
NavMesh
SceneManager
PrefabUtility
Camera
Addressables
Resources.Load
AssetDatabase.Refresh
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
Process.Start
```

Result must report runtime forbidden token scan and Editor/CLI forbidden token scan separately.

## 11. Focused-only 검증 정책

정상 검증은 EditMode category `MAP19_08`만 선택한다.

```text
MAP19_08 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15/MAP16/MAP17/MAP18/MAP19_01/MAP19_02/MAP19_03/MAP19_04/MAP19_05/MAP19_06/MAP19_07 selections: 0
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
FailureBundleCapturesSeedVersionHashPassCoordinatesProvenanceAndAttachments
FailureBundleSerializesManifestCsvAndScreenshotReferencesDeterministically
HeadlessRunnerParsesReplayRangeWorkerAndOutputArguments
HeadlessRunnerBuildsDeterministicWorkerPartitionsWithoutRunningScaleAudit
HeadlessRunnerWritesOnlyInsideExplicitOutputRootInFocusedDryRun
FailureBundleRunnerConsumesMap19_02ToMap19_07SurfacesReadOnly
FailureBundleRunnerRejectsMissingDigestUnsafeOutputAndInvalidArguments
FailureBundleRunnerFailuresAreAtomicAndReportOwnerReasonExpectedActual
FailureBundleRunnerDigestIsStableAcrossRepeatReverseCultureAttachmentAndWorkerOrder
Map19HandoffKeepsMap19_09Locked
```

Focused test category:

```text
MAP19_08
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
MapDesign/MCP/REPORTS/MAP19_08_CREATE_FAILURE_BUNDLE_AND_HEADLESS_RUNNER_RESULT.md
```

Result에는 아래 섹션을 포함한다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
## Failure Bundle and Headless Runner Summary
## No Seed Regression or Runtime Mutation Boundary Notes
```

`Failure Bundle and Headless Runner Summary`에는 아래 값을 채운다.

```text
MAP19_07 Result SHA-256 required/actual:
MAP19_07 installed Task SHA-256 required/actual:
MAP19_02 graph digest reused:
MAP19_03 completion proof digest reused:
MAP19_04 combined digest reused:
MAP19_05 combined digest reused:
MAP19_06 combined digest reused:
MAP19_07 measurement combined digest reused:
MAP19_08 incoming handoff digest reused:

failure bundle schema version:
bundle source kind:
bundle manifest fields present/required:
bundle pass snapshots:
bundle failure records:
bundle coordinate records:
bundle provenance records:
bundle screenshot reference slots:
bundle in-memory CSV/text payloads:
test-only output files written:
production output files written:
real screenshot captures:

runner modes supported:
runner argument names supported:
runner worker partitions planned:
runner replay requests planned:
runner dry-run plans created:
test-only synthetic validation callbacks:
production seed range validations:
Unity batchmode process launches:
external process launches:

failure bundle schema digest lower-hex SHA-256:
failure bundle manifest digest lower-hex SHA-256:
failure bundle payload digest lower-hex SHA-256:
runner argument schema digest lower-hex SHA-256:
runner plan digest lower-hex SHA-256:
MAP19_09 handoff digest lower-hex SHA-256:

repeat digest mismatch count:
reverse input/order digest mismatch count:
culture digest mismatch count:
attachment/order digest mismatch count:
runner partition order digest mismatch count:
mutation sensitivity probes passed:

missing handoff/digest failure probes:
identity/provenance failure probes:
invalid runner argument failure probes:
unsafe output failure probes:
output writer failure probes:
attempted production seed range failure probes:
attempted screenshot capture failure probes:
MAP19_09 start attempt probes:
atomic failure success digests published:
atomic failure MAP19_09 handoff digests published:

failure bundle creations:
headless runner plan creations:
headless runner actual scale runs:
seed batch runs:
production seed approvals:
legacy 19347 selections:
PlayMode selections:
unfiltered/full regression selections:
PlayerController/Rigidbody/Collider/Physics2D behavior changes:
Physics2D queries/simulations:
runtime objects spawned:
GameObject instantiate/enable/disable/destroy:
System.IO file write/read calls from Runtime production:
System.IO file write/read calls from Editor/CLI focused dry-run:
PlayerPrefs writes/reads:
Unity Tilemap component writes:
Tilemap.SetTile/SetTiles/SetTilesBlock/ClearAllTiles calls:
Scene/Prefab/Tilemap mutation:
Addressables/Resources/AssetDatabase loads:
camera reads/writes/captures:
optimization rewrites/broad refactors:
MAP19_09 started:
```

Allowed nonzero counters:

```text
failure bundle creations
headless runner plan creations
test-only output files written
System.IO file write/read calls from Editor/CLI focused dry-run
test-only synthetic validation callbacks
```

Must remain zero:

```text
headless runner actual scale runs
seed batch runs
production seed approvals
legacy 19347 selections
PlayMode selections
unfiltered/full regression selections
real screenshot captures
Unity batchmode process launches
external process launches
Runtime production System.IO calls
runtime mutation counters
MAP19_09 started
```

## 14. PASS 조건

PASS 조건:

```text
MAP19_07 Result and installed Task SHA match
MAP19_02 graph, MAP19_03 completion, MAP19_04 combined, MAP19_05 combined, MAP19_06 combined, MAP19_07 combined, and MAP19_08 incoming handoff digests are reused exactly
Failure bundle includes seed/version/hash/pass/failure/coordinate/provenance/attachment reference surfaces
Screenshot reference slots exist but real screenshot capture count is 0
Runner supports plan/replay/range arguments and deterministic worker partitioning
Focused dry-run writes only under explicit temporary output root
No production seed range validation, no scale audit, no production seed approval
Failure cases are deterministic and atomic
All required digests are stable lower-hex SHA-256
Focused MAP19_08 EditMode tests are 10/10 PASS
compile errors are 0
relevant Console errors are 0
Runtime forbidden API counters are 0
Editor/CLI forbidden API counters are 0 except explicitly allowed UnityEditor/System.IO/Environment.GetCommandLineArgs
legacy regression, PlayMode, unfiltered/full regression are 0
MAP19_09 remains LOCKED / NOT STARTED
```

FAIL 조건:

```text
Any required focused test fails
Any required digest is missing or unstable
Runtime production code uses System.IO or UnityEditor
Editor/CLI tool writes outside explicit output root
Editor/CLI tool mutates Assets, ProjectSettings, Packages, Scene, Prefab, Tilemap, Collider, Rigidbody, or runtime object state
Real screenshot capture is implemented or invoked in this task
Unity batchmode or external process is launched
Production seed range validation, seed batch, or legacy regression runs without explicit trigger approval
MAP19_09 is started or unlocked
Failure publishes success digest or MAP19_09 handoff
Result lacks user-facing responsibility report
```

BLOCKED 조건:

```text
MAP19_07 Result SHA mismatch
MAP19_07 installed Task SHA mismatch
Required seed/version/hash identity source is not publicly consumable
Failure owner/provenance chain cannot be identified without inventing production fixture
Project has no safe Editor-only location or assembly for runner shell
Unity compile prevents focused tests from running before task code can be isolated
```

## 15. Status Finalize

PASS 후에만 status를 finalize한다.

Expected final status:

```text
MAP19_08_CREATE_FAILURE_BUNDLE_AND_HEADLESS_RUNNER: COMPLETE
MAP19_09_MAP19_SCALE_AND_EXIT_AUDIT: LOCKED
Current Task: NONE
```

MAP19_09는 다음 파일을 내가 별도로 줄 때까지 시작하지 않는다.

## 16. Commit

PASS finalize 후 atomic commit을 만든다.

Commit subject:

```text
MAP19_08: create failure bundle and headless runner
```

Commit 범위:

```text
MAP19_08 Runtime production files
MAP19_08 Editor/CLI tool file
MAP19_08 focused test file
matching .meta files
MapDesign/MCP/TASKS/MAP19_08_CREATE_FAILURE_BUNDLE_AND_HEADLESS_RUNNER.md
MapDesign/MCP_ARCHIVE/MAP19_08_CREATE_FAILURE_BUNDLE_AND_HEADLESS_RUNNER.md
MapDesign/MCP/REPORTS/MAP19_08_CREATE_FAILURE_BUNDLE_AND_HEADLESS_RUNNER_RESULT.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

금지:

```text
git push
unrelated dirty file staging
committing user changes
MAP19_09 files
generated test output directory if it is intentionally temporary
```

## 17. Stop Rule

작업 완료 후 반드시 멈춘다.

```text
STOP AFTER MAP19_08 RESULT AND COMMIT.
DO NOT START MAP19_09.
```

