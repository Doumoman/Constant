```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP20_05_IMPLEMENT_REPLAY_AUTHORING_INTEGRATION_HUD_AND_EXPORT
  task_file: TASKS/MAP20_05_IMPLEMENT_REPLAY_AUTHORING_INTEGRATION_HUD_AND_EXPORT.md
  requires_current_task: NONE
  requires_completed_task: MAP20_04_IMPLEMENT_CSV_NAVIGATION_AND_VALIDATION_JUMP
  requires_result:
    path: REPORTS/MAP20_04_IMPLEMENT_CSV_NAVIGATION_AND_VALIDATION_JUMP_RESULT.md
    status: PASS
    sha256: f9bac092ce7e1177e9eea6ca61a46ea671330790b55355206c06027ac1e77235
  requires_installed_task:
    path: TASKS/MAP20_04_IMPLEMENT_CSV_NAVIGATION_AND_VALIDATION_JUMP.md
    sha256: f398401662bcb9134db8269fa6dbc4d42752ae90b1a442b16a6b8e6610cf9106
  requires_handoff:
    name: MAP20_05 handoff digest
    sha256: 729920113fefff78e3e5584d34c3ce682f3017a545e3e3bf8ae0bbdcc8b20f55
  sets_current_task: MAP20_05_IMPLEMENT_REPLAY_AUTHORING_INTEGRATION_HUD_AND_EXPORT
```

# MAP20_05 - Implement Replay Authoring Integration HUD and Export

```text
TASK: MAP20_05_IMPLEMENT_REPLAY_AUTHORING_INTEGRATION_HUD_AND_EXPORT
PHASE: MAP20 - Tooling / Authoring Control Surface
STATUS: CURRENT
NEXT: MAP20_06_MAP20_TOOLING_EXIT_TESTS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP20_01~04에서 만든 read-only tooling을 마지막 통합 표면으로 묶는다.

이번 Task는 다음 **다섯 가지 표시/출력 계약**만 소유한다.

| Area | This task owns | This task must not do |
|---|---|---|
| Failure browser | MAP19 failure bundle을 읽기 전용 record로 표시하고, 없으면 explicit empty/missing state로 표시 | failure 생성, repair, rerun, replay |
| Replay authoring | validation jump target에서 `RequestOnly` replay request payload 생성 | actual replay 실행, generator/validator/rollback 호출 |
| Fixed/generated split | MAP07 fixed content와 MAP16/17/18 generated content identity 분리 표시 | MAP07/16/17/18 source rewrite 또는 migration |
| Runtime HUD | seed, sector, pass, coordinate, selected error/failure를 display-only state로 포맷 | Scene/Prefab wiring, auto-spawn, runtime object mutation |
| Seed bundle export | deterministic sample bundle을 `MapDesign/MCP/GENERATED/MAP20_05` 아래에만 출력 | production seed approval, CSV write, project asset mutation |

이번 Task의 최종 목적은 MAP20_06 exit test가 볼 수 있는 통합 자료와 handoff digest를 제공하는 것이다.

## 1. 사용자 보고 의무

Result 첫 부분에는 반드시 아래 두 섹션을 둔다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

`User-Facing Implementation Report`에는 기능보다 책임을 먼저 설명한다.

필수 답변:

```text
무슨 사용자-facing 툴링을 추가했는가?
failure browser가 실제 failure와 FocusedFixture를 어떻게 구분하는가?
replay request는 무엇을 기록하고 무엇을 실행하지 않는가?
MAP07 fixed/generated split과 MAP08 boundary link는 어떤 identity를 보존하는가?
runtime HUD는 어떤 상태만 표시하며 어떤 runtime 변경을 하지 않는가?
seed bundle sample에는 무엇이 들어가며 어디에만 기록되는가?
실제 replay/generation/validation/rollback/CSV write가 없었다는 증거는 무엇인가?
legacy regression을 돌리지 않았다는 증거는 무엇인가?
```

`Responsibility and Added Scripts`는 반드시 표로 작성한다.

```text
| Script or file | Added or changed responsibility | Explicit non-ownership |
```

파일별 책임이 구체적이지 않으면 FAIL이다.

## 2. 선행조건

작업 시작 전에 다음을 확인한다.

```text
MapDesign/MCP/REPORTS/MAP20_04_IMPLEMENT_CSV_NAVIGATION_AND_VALIDATION_JUMP_RESULT.md exists
MAP20_04 Result STATUS: PASS
MAP20_04 Result SHA-256:
f9bac092ce7e1177e9eea6ca61a46ea671330790b55355206c06027ac1e77235

MapDesign/MCP/TASKS/MAP20_04_IMPLEMENT_CSV_NAVIGATION_AND_VALIDATION_JUMP.md exists
MAP20_04 installed Task SHA-256:
f398401662bcb9134db8269fa6dbc4d42752ae90b1a442b16a6b8e6610cf9106

MAP20_05 handoff digest:
729920113fefff78e3e5584d34c3ce682f3017a545e3e3bf8ae0bbdcc8b20f55

MAP20_06_MAP20_TOOLING_EXIT_TESTS: LOCKED
```

불일치 시:

```text
STATUS: BLOCKED
reason: precondition mismatch
created/changed code files: 0
MAP20_06 started: NO
STOP
```

## 3. 허용 범위

허용 파일:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Tooling/GeneratedReplayAuthoringIntegration.cs
Assets/_Game/Map/Runtime/WorldGeneration/Tooling/GeneratedRuntimeDebugHud.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Tooling/GeneratedReplayAuthoringWindow.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Tooling/GeneratedReplayAuthoringSamplePublisher.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Tooling/GeneratedReplayAuthoringIntegrationTests.cs
MapDesign/MCP/GENERATED/MAP20_05/**
MapDesign/MCP/TASKS/MAP20_05_IMPLEMENT_REPLAY_AUTHORING_INTEGRATION_HUD_AND_EXPORT.md
MapDesign/MCP/REPORTS/MAP20_05_IMPLEMENT_REPLAY_AUTHORING_INTEGRATION_HUD_AND_EXPORT_RESULT.md
MapDesign/MCP_ARCHIVE/MAP20_05_IMPLEMENT_REPLAY_AUTHORING_INTEGRATION_HUD_AND_EXPORT.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

조건부 허용:

```text
Existing MAP20_01~04 tooling types may be referenced through public read-only APIs.
Existing MAP20_04 generated JSON may be read as input.
Existing MAP19 failure bundle JSON may be read if present.
If no actual MAP19 failure bundle exists, record explicit empty/missing state.
Focused tests may inject FocusedFixture failure records, but Result must count them separately.
HUD component class may exist, but no Scene/Prefab/Resources/bootstrap/runtime auto-spawn wiring is allowed.
```

공통 금지:

```text
actual replay execution
generator solve/reroll/execution
validation runner execution
rollback execution
CSV authoring edit/save/import/export rewrite
auto-fix / auto-repair
Tilemap bake or Tilemap mutation
Scene / Prefab / ProjectSettings / Packages changes
runtime GameObject instantiate/enable/disable/destroy during tooling actions
asset database mutation outside new scripts/meta and MAP20_05 generated artifacts
production seed approval
external process launch
manual gameplay test
automatic MAP20_06 start
broad refactor or optimization rewrite
```

이전 결과물 재실행 금지:

```text
MAP19_09 scale audit rerun
MAP20_01 generator run rerun
MAP20_02 overlay sample regeneration
MAP20_03 detail sample regeneration
MAP20_04 navigation sample regeneration
legacy 19347 regression
prior task category selection
PlayMode selection
unfiltered test run
full regression run
```

## 4. 계약 상세

### 4.1 Replay Request

Required fields:

```text
schema_version
task_id
source_MAP20_04_handoff_digest
request_id
seed
world_id
sector_coordinate
cell_coordinate
validation_error_id
jump_target_kind
selection_path
failure_bundle_id
authoring_context_digest
requested_action
execution_state
created_utc_excluded_from_canonical_digest
canonical_digest
```

Rules:

```text
requested_action is one of AuthorReplayRequest / OpenFailure / OpenSource / OpenInspector / ExportSeedBundle.
execution_state must be RequestOnly.
request_id and canonical_digest must be deterministic from stable fields.
MissingData remains explicit and must not trigger regeneration.
```

### 4.2 Failure Browser

Allowed source kinds:

```text
ActualFailureBundle
FocusedFixture
MissingData
```

Required fields:

```text
failure_record_id
source_kind
source_path
source_digest
owner_task
failure_category
severity
seed
sector_coordinate
cell_coordinate
validation_error_id
navigation_selection_path
replay_request_id
actual_failure_available
fixture_kind
missing_reason
canonical_digest
```

Rules:

```text
No actual failure may be invented from MissingData.
FocusedFixture records are test-owned and must never be counted as actual failures.
Browser actions are read/filter/select/copy only.
```

### 4.3 Fixed/Generated Split and Boundary Link

Content origin tokens:

```text
FixedMap07
GeneratedMap16Slice
GeneratedMap17Runtime
GeneratedMap18Population
MissingData
```

Boundary link required fields:

```text
boundary_link_id
pair_id
source_biome
target_biome
candidate_id
projection_id
socket_id
sector_coordinate
cell_coordinate
source_digest
link_state
canonical_digest
```

Rules:

```text
The contract must represent all six MAP08 approved biome pairs.
Do not use the PDF page 29 four-pair subset as source of truth.
No MAP07, MAP08, MAP16, MAP17, or MAP18 source files may be rewritten.
Boundary links are references for navigation/export only.
```

### 4.4 Runtime HUD

Required HUD state fields:

```text
schema_version
task_id
hud_state_id
seed
world_id
sector_coordinate
cell_coordinate
active_pass
active_tool_mode
selected_validation_error_id
selected_failure_record_id
selected_source_path
selected_selection_path
status_line
runtime_mutation_count
canonical_digest
```

Rules:

```text
HUD state is display-only.
HUD must not instantiate itself or register itself automatically.
HUD tests must keep runtime_mutation_count at 0.
```

### 4.5 Seed Bundle Export

Required bundle fields:

```text
schema_version
task_id
source_MAP20_04_result_digest
source_MAP20_04_task_digest
source_MAP20_05_handoff_digest
seed
world_id
sector_coordinate
generator_version
pass_digest_summary
csv_navigation_index_digest
validation_jump_sample_digest
failure_browser_digest
replay_request_digest
runtime_hud_digest
map07_fixed_generated_split_digest
map08_boundary_link_digest
created_utc_excluded_from_canonical_digest
canonical_digest
```

Rules:

```text
Export writes only under MapDesign/MCP/GENERATED/MAP20_05.
Export does not approve production seeds.
Export does not call generator, validator, replay, rollback, or external processes.
Record order and JSON key order must be deterministic.
Digests must be lower-hex SHA-256.
```

## 5. Editor UI 최소 요구

`GeneratedReplayAuthoringWindow` may use IMGUI or UI Toolkit following existing project conventions.

Required controls:

```text
Failure search/filter field
Actual/fixture/missing source filter
Failure record list
Selected failure details panel
Replay authoring request preview panel
MAP07 fixed/generated origin panel
MAP08 boundary link panel
Runtime HUD state preview panel
Seed bundle export sample button
Copy failure id button
Copy replay request id button
Copy seed bundle path button
Open generated MAP20_05 output folder button
```

Safety:

```text
Opening, searching, filtering, selecting, copying, and previewing change only editor view state.
Creating a replay request creates RequestOnly data only.
Seed bundle export writes only the MAP20_05 sample artifact.
No Save, Apply, Fix, Import, Generate, Rollback, Validate, Run, Replay, or Approve Production Seed button is added.
```

## 6. Snapshot / Digest 계약

Required sample artifacts:

```text
MapDesign/MCP/GENERATED/MAP20_05/failure_browser_index.json
MapDesign/MCP/GENERATED/MAP20_05/replay_authoring_request_sample.json
MapDesign/MCP/GENERATED/MAP20_05/runtime_hud_state_sample.json
MapDesign/MCP/GENERATED/MAP20_05/seed_bundle_export_sample.json
MapDesign/MCP/GENERATED/MAP20_05/replay_authoring_digest_manifest.json
```

Digest manifest required fields:

```text
schema_version
task_id
source_MAP20_04_result_digest
source_MAP20_04_task_digest
MAP20_05_handoff_digest
failure_browser_index_digest
replay_authoring_request_digest
runtime_hud_state_digest
seed_bundle_export_sample_digest
MAP20_06_handoff_digest
created_utc_excluded_from_canonical_digest
canonical_digest
```

All canonical digests must exclude `created_utc`, use deterministic order, and be lowercase SHA-256.

## 7. 중복과 하드코딩 방지

Result에 아래 카운터를 기록하고 모두 0이어야 한다.

```text
duplicated generator logic count:
duplicated validation logic count:
duplicated rollback logic count:
duplicated overlay/detail inspector logic count:
duplicated CSV parser/export logic count:
duplicated navigation publisher logic count:
duplicated jump target catalog count:
hard-coded source path copies outside sample/precondition constants:
hard-coded digest string copies outside precondition/constants:
```

## 8. Focused Tests

Focused test category:

```text
MAP20_05
```

Required test names:

```text
ReplayAuthoringBuildsRequestFromNavigationTargetWithoutExecutingReplay
FailureBrowserSeparatesActualEmptyMissingAndFocusedFixtureRecords
SeedBundleExportIncludesSeedVersionHashesPassesNavigationFailuresAndHud
Map07FixedGeneratedSplitUsesExactOriginTokensWithoutSourceRewrites
Map08BoundaryLinksPreserveApprovedPairCandidateProjectionIdentity
RuntimeHudStateFormatsSeedSectorPassAndSelectionWithoutRuntimeMutation
SeedBundleExporterWritesOnlyUnderMap20_05GeneratedOutput
ReplayAuthoringDigestsAreStableAcrossRepeatCultureAndInputOrder
ReplayAuthoringPublishesMap20_06HandoffOnlyAfterFocusedPass
ReplayAuthoringDoesNotSelectLegacyRegressionPriorCategoriesPlayModeOrFullRegression
```

Required count:

```text
discovered: 10
executed: 10
passed: 10
failed: 0
```

Focused validation may create sample artifacts only under `MapDesign/MCP/GENERATED/MAP20_05`.

## 9. 회귀 금지 정책

이번 Task의 검증은 focused MAP20_05 EditMode selection과 compile/console check까지만 허용한다.

문제가 실제로 발생하지 않은 상태에서 더 넓은 검증을 돌리지 않는다. 실제 문제가 발생해 더 넓은 검증이 필요하면 조용히 실행하지 말고, Result에 trigger owner, broken invariant, focused proof insufficiency, requested wider verification을 기록하고 STOP한다.

문제가 없다면 Result에 반드시 아래 블록을 포함한다.

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
FULL REGRESSION RUNS: 0
MAP19_09 SCALE AUDIT RERUNS: 0
MAP20_01 GENERATOR RUN RERUNS: 0
MAP20_02 OVERLAY SAMPLE REGENERATION RUNS: 0
MAP20_03 DETAIL SAMPLE REGENERATION RUNS: 0
MAP20_04 NAVIGATION SAMPLE REGENERATION RUNS: 0
VALIDATION RUNNER EXECUTIONS: 0
REPLAY EXECUTIONS: 0
GENERATOR EXECUTIONS: 0
ROLLBACK EXECUTIONS: 0
CSV AUTHORING WRITES: 0
RUNTIME OBJECT SPAWNS: 0
SCENE PREFAB CHANGES: 0
```

## 10. Result 필수 기록

Result 파일:

```text
MapDesign/MCP/REPORTS/MAP20_05_IMPLEMENT_REPLAY_AUTHORING_INTEGRATION_HUD_AND_EXPORT_RESULT.md
```

Required sections:

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
## Replay Authoring and Failure Browser Summary
## Fixed Generated and Boundary Link Summary
## Runtime HUD Summary
## Seed Bundle Export Summary
## Snapshot and Digest Summary
## Focused Validation Summary
## No Legacy Regression Boundary Notes
## Final Status Evidence
```

Required summary fields:

```text
MAP20_04 Result SHA-256 required/actual:
MAP20_04 installed Task SHA-256 required/actual:
MAP20_05 handoff digest required/actual:
replay request required fields present/required:
replay requests created:
replay requests executed:
failure source kind tokens:
actual failure records:
focused fixture failure records:
missing failure records:
failure browser repair/rerun/replay actions:

fixed/generated origin tokens:
fixed origin records:
generated origin records:
missing origin records:
source files rewritten for split:
MAP08 approved pair capacity:
MAP08 boundary links created:
boundary regeneration executions:
PDF boundary subset used as source of truth:

HUD state required fields present/required:
HUD component auto-spawn path count:
runtime GameObject mutation count:
Scene/Prefab HUD wiring count:
runtime HUD sample states:

seed bundle required fields present/required:
seed bundle sample files written:
seed bundle write roots:
production seed approvals:
authoring CSV writes:
generator/validator/replay/rollback executions:

sample artifacts created:
failure browser index digest lower-hex SHA-256:
replay authoring request digest lower-hex SHA-256:
runtime HUD state digest lower-hex SHA-256:
seed bundle export sample digest lower-hex SHA-256:
digest manifest lower-hex SHA-256:
MAP20_06 handoff digest lower-hex SHA-256:

Unity Version:
Compile Errors:
Relevant Warnings:
Relevant Console Errors:
EditMode category:
Discovered:
Executed:
Passed:
Failed:
Skipped:
Inconclusive:
PlayMode Tests:
Scene/Prefab Changes:
```

## 11. PASS / FAIL / BLOCKED

PASS 조건:

```text
Precondition SHA and handoff digest match
Replay authoring request is RequestOnly and executes no replay
Failure browser separates ActualFailureBundle / FocusedFixture / MissingData
No actual failure is invented when no MAP19 failure bundle exists
MAP07 fixed/generated origin tokens are exact and source files are not rewritten
MAP08 boundary link can represent all six approved pairs and does not use the PDF four-pair subset as truth
Runtime HUD has zero auto-spawn, runtime mutation, and Scene/Prefab wiring
Seed bundle sample writes only under MapDesign/MCP/GENERATED/MAP20_05
Sample artifacts are deterministic and have lower-hex SHA-256 digests
Focused MAP20_05 EditMode tests are 10/10 PASS
All no-regression and no-execution counters are zero
Responsibility and Added Scripts table is present and specific
MAP20_06 remains LOCKED and not started
```

FAIL 조건:

```text
Replay request executes replay or invokes generator/validator/rollback
Failure browser invents actual failures from missing data
FocusedFixture records are reported as actual failures
Seed bundle export writes outside MapDesign/MCP/GENERATED/MAP20_05
Seed bundle export approves production seeds
MAP07 fixed/generated split rewrites source data
MAP08 boundary link uses the PDF four-pair subset as source of truth
Runtime HUD auto-spawns or mutates runtime GameObjects
Scene/Prefab/Tilemap/ProjectSettings/Packages are changed
CSV authoring file is written
Any forbidden prior task rerun, PlayMode, legacy regression, unfiltered run, or full regression is executed
MAP20_06 starts
```

BLOCKED 조건:

```text
Precondition SHA mismatch
Compile error unrelated to this task that prevents focused validation
Existing MAP20_04 data cannot be consumed without regenerating MAP20_04 outputs
Failure data absence cannot be represented as MissingData
HUD cannot be represented without Scene/Prefab/runtime auto-spawn mutation
Seed bundle cannot be generated without calling generator/validator/replay/rollback
```

When BLOCKED:

```text
Do not finalize MAP20_05
Do not start MAP20_06
Do not publish MAP20_06 handoff digest
Record exact owner, invariant, and minimum next action
STOP
```

## 12. Finalize / Commit / STOP

PASS일 때만:

```text
MAP20_05_IMPLEMENT_REPLAY_AUTHORING_INTEGRATION_HUD_AND_EXPORT: COMPLETE
MAP20_06_MAP20_TOOLING_EXIT_TESTS: LOCKED
```

FAIL 또는 BLOCKED일 때:

```text
MAP20_05 remains CURRENT
MAP20_06 remains LOCKED
STOP
```

PASS일 때만 atomic commit을 만든다.

Commit subject:

```text
MAP20_05 implement replay authoring hud export
```

Commit body에는 다음을 포함한다.

```text
MAP20_05 responsibilities
focused test count
replay request count and execution count
failure actual/fixture/missing counts
fixed/generated origin tokens
MAP08 boundary pair capacity
runtime HUD mutation and auto-spawn counts
seed bundle sample path and digest
MAP20_06 handoff digest
legacy regression counters all zero
CSV authoring writes zero
```

Git push는 하지 않는다.

Result 작성, 상태 finalize, commit 이후 반드시 멈춘다.

```text
DO NOT START MAP20_06.
DO NOT CREATE MAP20_06 FILES.
DO NOT RERUN MAP19_09 SCALE AUDIT.
DO NOT RERUN MAP20_01 GENERATOR RUN.
DO NOT REGENERATE MAP20_02 OVERLAY SAMPLE.
DO NOT REGENERATE MAP20_03 DETAIL SAMPLE.
DO NOT REGENERATE MAP20_04 NAVIGATION SAMPLE.
DO NOT RUN VALIDATION RUNNER.
DO NOT EXECUTE REPLAY.
DO NOT EXECUTE GENERATOR.
DO NOT EXECUTE ROLLBACK.
DO NOT WRITE CSV AUTHORING FILES.
DO NOT MUTATE SCENE PREFAB TILEMAP OR RUNTIME OBJECTS.
DO NOT RUN LEGACY REGRESSION.
STOP.
```
