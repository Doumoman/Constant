```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP20_04_IMPLEMENT_CSV_NAVIGATION_AND_VALIDATION_JUMP
  task_file: TASKS/MAP20_04_IMPLEMENT_CSV_NAVIGATION_AND_VALIDATION_JUMP.md
  requires_current_task: NONE
  requires_completed_task: MAP20_03_CREATE_PATTERN_CLUSTER_SPECIAL_AND_SLICE_INSPECTORS
  requires_result:
    path: REPORTS/MAP20_03_CREATE_PATTERN_CLUSTER_SPECIAL_AND_SLICE_INSPECTORS_RESULT.md
    status: PASS
    sha256: 519fede231c4d4837e076ac87aa61b3c1067d288bc382e91e9ccbc6ac5c691bd
  requires_installed_task:
    path: TASKS/MAP20_03_CREATE_PATTERN_CLUSTER_SPECIAL_AND_SLICE_INSPECTORS.md
    sha256: 402ec4eef2419b7510fc8c51628c500eb7ef012da2ca7a68e9f3bd124e56260c
  requires_handoff:
    name: MAP20_04 handoff digest
    sha256: 863018695423be601328975e69356f2ece76ec4db57b024db9406d7131a43285
  sets_current_task: MAP20_04_IMPLEMENT_CSV_NAVIGATION_AND_VALIDATION_JUMP
```

# MAP20_04 - Implement CSV Navigation and Validation Jump

```text
TASK: MAP20_04_IMPLEMENT_CSV_NAVIGATION_AND_VALIDATION_JUMP
PHASE: MAP20 - Tooling / Authoring Control Surface
STATUS: CURRENT
NEXT: MAP20_05_IMPLEMENT_REPLAY_AUTHORING_INTEGRATION_HUD_AND_EXPORT
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP20_02/03 inspector에서 보이는 validation marker나 오류 ID를 눌렀을 때, 사람이 정확한 source 위치와 inspector 대상까지 따라갈 수 있는 **읽기 전용 CSV navigation / validation jump 계약**을 만든다.

이번 Task는 **찾아가기 기능**만 소유한다.  
CSV 수정, 자동 repair, validation 재실행, failure browser, runtime HUD, seed bundle export는 다음 작업이 소유한다.

이번 Task의 책임:

```text
1. CSV source location index를 만든다.
2. Validation error id -> CSV file/row/column/field/record id 매핑을 만든다.
3. Validation error id -> Tile / Pattern / Cluster / Socket / Slot inspector target 매핑을 만든다.
4. MAP20_02/03 inspector가 selection-only jump target을 소비할 수 있게 한다.
5. Jump target을 표시/복사/선택할 수 있는 Editor UI를 만든다.
6. MissingData 또는 source-unavailable 상태를 명시하고 추정 좌표를 만들지 않는다.
7. CSV navigation sample artifact와 deterministic digest를 만든다.
8. Result 첫머리에 추가 script와 각 책임을 사용자용 표로 보고한다.
```

아직 하지 않는 일:

```text
CSV authoring edit/save
auto-fix / auto-repair
validation runner execution
failure browser
runtime HUD
seed bundle export
production seed approval
external editor/process launch
```

## 1. 사용자 보고 의무

Result 첫 부분에는 반드시 아래 두 섹션을 둔다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

`User-Facing Implementation Report`에는 아래 질문에 답한다.

```text
이번 작업이 어떤 source navigation 기능을 추가했는가?
오류 ID에서 CSV file/row/column/field를 어떻게 찾는가?
오류 ID에서 Tile/Pattern/Cluster/Socket/Slot inspector target을 어떻게 찾는가?
source가 없거나 MissingData일 때 어떻게 표시하는가?
사용자가 누를 수 있는 UI 동작은 무엇이고, 무엇은 하지 않는가?
MAP20_05로 미룬 기능은 무엇인가?
이 작업이 CSV를 수정하지 않았다는 증거는 무엇인가?
이 작업이 legacy regression을 돌리지 않았다는 증거는 무엇인가?
```

`Responsibility and Added Scripts`는 표로 작성한다.

```text
| Script or file | Added or changed responsibility | Explicit non-ownership |
```

파일별 책임이 없으면 FAIL이다.

## 2. 선행조건

작업 시작 전에 다음을 확인한다.

```text
MapDesign/MCP/REPORTS/MAP20_03_CREATE_PATTERN_CLUSTER_SPECIAL_AND_SLICE_INSPECTORS_RESULT.md exists
MAP20_03 Result STATUS: PASS
MAP20_03 Result SHA-256:
519fede231c4d4837e076ac87aa61b3c1067d288bc382e91e9ccbc6ac5c691bd

MapDesign/MCP/TASKS/MAP20_03_CREATE_PATTERN_CLUSTER_SPECIAL_AND_SLICE_INSPECTORS.md exists
MAP20_03 installed Task SHA-256:
402ec4eef2419b7510fc8c51628c500eb7ef012da2ca7a68e9f3bd124e56260c

MAP20_04 handoff digest:
863018695423be601328975e69356f2ece76ec4db57b024db9406d7131a43285

MAP20_05_IMPLEMENT_REPLAY_AUTHORING_INTEGRATION_HUD_AND_EXPORT: LOCKED
```

불일치 시:

```text
STATUS: BLOCKED
reason: precondition mismatch
created/changed code files: 0
MAP20_05 started: NO
STOP
```

## 3. 허용 범위

허용:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Tooling/GeneratedCsvNavigationIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Tooling/GeneratedValidationJumpTarget.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Tooling/GeneratedCsvNavigationWindow.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Tooling/GeneratedCsvNavigationSamplePublisher.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Tooling/GeneratedCsvNavigationTests.cs
MapDesign/MCP/GENERATED/MAP20_04/**
MapDesign/MCP/TASKS/MAP20_04_IMPLEMENT_CSV_NAVIGATION_AND_VALIDATION_JUMP.md
MapDesign/MCP/REPORTS/MAP20_04_IMPLEMENT_CSV_NAVIGATION_AND_VALIDATION_JUMP_RESULT.md
MapDesign/MCP_ARCHIVE/MAP20_04_IMPLEMENT_CSV_NAVIGATION_AND_VALIDATION_JUMP.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

조건부 허용:

```text
MAP20_02/03 inspector windows may consume jump targets as selection state only.
Existing MAP20_02/03 snapshot models may be read or referenced, not rewritten.
Existing CSV parser/export helpers may be consumed if public and stable.
Generated sample JSON/CSV navigation fixtures may be written only under MapDesign/MCP/GENERATED/MAP20_04.
```

금지:

```text
CSV authoring table mutation
CSV auto-fix
CSV import/export rewrite
generated terrain output mutation outside MAP20_04 sample artifacts
validation runner execution
validation proof rewrite
failure browser implementation
runtime HUD implementation
seed bundle export implementation
generator solve or reroll
pattern renderer behavior change
cluster/special/slice behavior change
rollback execution or rollback behavior change
MAP19_09 scale audit rerun
MAP20_01 generator run rerun
MAP20_02 overlay sample regeneration
MAP20_03 detail sample regeneration except read-only input reference
legacy 19347 regression
prior task category selection
PlayMode selection
unfiltered test run
full regression run
manual gameplay test
Scene / Prefab / Tilemap authoring mutation
Tilemap.SetTile / SetTiles / SetTilesBlock / ClearAllTiles
runtime GameObject instantiate/enable/disable/destroy
real screenshot capture from camera
external editor or external process launch
production seed approval
automatic MAP20_05 start
broad refactor or optimization rewrite
```

## 4. CSV Source Location 계약

`GeneratedCsvSourceLocation` must represent exact source position.

Required fields:

```text
source_kind
csv_file_path
csv_file_digest
row_number_1_based
column_number_1_based
column_name
field_name
record_id
record_kind
line_digest
source_available
missing_reason
```

Rules:

```text
row_number_1_based >= 1
column_number_1_based >= 1
csv_file_path uses normalized forward slashes
csv_file_digest and line_digest are lower-hex SHA-256 or NONE when source_available is false
source unavailable is explicit; do not invent a row/column
source location is read-only; no writer API is exposed
```

Allowed source kinds:

```text
AuthoringCsv
GeneratedCsv
GeneratedJson
InMemoryOnly
MissingData
```

## 5. Jump Target 계약

Jump target kind tokens are exact:

```text
Tile
Pattern
Cluster
Socket
Slot
```

Required fields:

```text
validation_error_id
severity
owner
message
source_location
jump_target_kind
sector_coordinate
local_cell_coordinate
world_cell_coordinate
pattern_id
cluster_id
socket_id
slot_id
inspector_tab_token
inspector_record_id
selection_path
replay_reference
source_available
missing_reason
canonical_digest
```

Rules:

```text
Every jump target must have exactly one jump_target_kind.
Every jump target must have exactly one source_location object.
Tile target must include local cell or explicit MissingData.
Pattern target must include pattern id or explicit MissingData.
Cluster target must include cluster id or explicit MissingData.
Socket target must include socket id or explicit MissingData.
Slot target must include slot id or explicit MissingData.
selection_path must be deterministic and human-readable.
```

## 6. Editor UI 최소 요구

The EditorWindow may use IMGUI or UI Toolkit following existing project conventions.

Required controls:

```text
Validation error id search field
Jump target kind filter: Tile / Pattern / Cluster / Socket / Slot
Source availability filter
Result list
Selected source location panel
Selected inspector target panel
Copy CSV path:row:column button
Copy validation error id button
Copy inspector selection path button
Jump to inspector selection button
Open generated MAP20_04 output folder button
```

Required safety states:

```text
Opening the window does not run validation.
Searching changes only view state.
Filtering changes only view state.
Jump to inspector selection changes only inspector selection state.
Copy buttons change only clipboard or returned string in tests.
No Save, Apply, Fix, Import, Export, Generate, Rollback, Validate, Run, or Replay button is added in this Task.
If source data is missing, the window displays MissingData instead of triggering regeneration.
```

## 7. Inspector 연결 계약

MAP20_02/03 inspector integration is selection-only.

Required:

```text
World overlay inspector can receive sector/cell target selection.
Detail inspector can receive Pattern/Cluster/Special/Slice-related selection path if available.
CSV navigation must not force the detail inspector to rebuild or regenerate samples.
CSV navigation must not invoke validation runner.
CSV navigation must not write selected state to project assets.
```

Target mapping:

| Jump target kind | Inspector selection target |
|---|---|
| `Tile` | MAP20_02 sector canvas selected cell |
| `Pattern` | MAP20_03 Pattern tab selected record |
| `Cluster` | MAP20_03 Cluster tab selected record |
| `Socket` | MAP20_03 Slice tab or sector boundary/socket selected record |
| `Slot` | MAP20_03 Cluster/Special/Slice slot selected record |

If a specific tab cannot be selected because data is missing:

```text
selection_path still identifies the intended tab and record id.
source_available remains false or MissingData.
No fallback generation is allowed.
```

## 8. Snapshot / Digest 계약

Required sample artifacts:

```text
MapDesign/MCP/GENERATED/MAP20_04/csv_navigation_index.json
MapDesign/MCP/GENERATED/MAP20_04/validation_jump_sample.json
MapDesign/MCP/GENERATED/MAP20_04/source_navigation_digest_manifest.json
```

CSV navigation index required fields:

```text
schema_version
task_id
source_MAP20_03_detail_snapshot_digest
source_MAP20_03_combined_detail_digest
MAP20_04_handoff_digest
source_records
jump_target_records
missing_data_records
canonical_digest
created_utc_excluded_from_canonical_digest
```

Validation jump sample required fields:

```text
schema_version
task_id
jump_target_kind_tokens
sample_error_ids
sample_jump_targets
source_location_count
selection_path_count
missing_data_count
canonical_digest
created_utc_excluded_from_canonical_digest
```

Digest manifest required fields:

```text
schema_version
task_id
csv_navigation_index_digest
validation_jump_sample_digest
MAP20_03_handoff_digest
MAP20_05_handoff_digest
canonical_digest
created_utc_excluded_from_canonical_digest
```

Digest rules:

```text
created_utc may be stored but must be excluded from canonical digest.
Record order must be deterministic.
Jump target kind order must be Tile / Pattern / Cluster / Socket / Slot.
Digest must be lowercase SHA-256.
Repeat/culture/input-order changes must not alter canonical digest.
```

## 9. 중복과 하드코딩 방지

필수:

```text
Do not duplicate jump target kind tokens across multiple production catalogs.
Do not duplicate CSV parsing/exporting logic if a public parser already exists.
Do not duplicate generator, validation, rollback, detail inspector, or overlay logic.
Do not hard-code source file paths outside sample/precondition constants.
Do not hard-code digest constants outside precondition/handoff constants.
Window owns view/input/copy/selection state only.
Runtime index model owns deterministic serialization.
Editor publisher owns sample artifact file writes only.
```

Result에 아래를 기록한다.

```text
duplicated generator logic count:
duplicated validation logic count:
duplicated rollback logic count:
duplicated overlay/detail inspector logic count:
duplicated CSV parser/export logic count:
duplicated jump target catalog count:
hard-coded source path copies outside sample/precondition constants:
hard-coded digest string copies outside precondition/constants:
```

목표값:

```text
duplicated generator logic count: 0
duplicated validation logic count: 0
duplicated rollback logic count: 0
duplicated overlay/detail inspector logic count: 0
duplicated CSV parser/export logic count: 0
duplicated jump target catalog count: 0
hard-coded source path copies outside sample/precondition constants: 0
hard-coded digest string copies outside precondition/constants: 0
```

## 10. Focused Tests

Focused test category:

```text
MAP20_04
```

Required test names:

```text
CsvNavigationIndexMapsErrorIdToExactFileRowColumnFieldAndRecord
ValidationJumpTargetsUseExactTilePatternClusterSocketAndSlotKinds
CsvNavigationReportsMissingSourceWithoutInventingRowColumnOrInspectorData
CsvNavigationWindowOpensWithoutValidationGenerationRollbackReplayOrCsvWrite
JumpToInspectorSelectionChangesOnlyViewState
CsvNavigationArtifactsSerializeDeterministicallyAcrossRepeatCultureAndInputOrder
CsvNavigationPublishesMap20_05HandoffOnlyAfterFocusedPass
CsvNavigationDoesNotSelectLegacyRegressionPriorCategoriesPlayModeOrFullRegression
```

Required count:

```text
discovered: 8
executed: 8
passed: 8
failed: 0
```

Focused validation may create sample artifacts only under:

```text
MapDesign/MCP/GENERATED/MAP20_04
```

## 11. 회귀 금지 정책

이번 Task의 검증은 focused MAP20_04 EditMode selection과 compile/console check까지만 허용한다.

문제가 실제로 발생하지 않은 상태에서 더 넓은 검증을 돌리지 않는다.

금지:

```text
MAP19_09 scale audit rerun
MAP20_01 generator run rerun
MAP20_02 overlay sample regeneration
MAP20_03 detail sample regeneration except read-only input reference
validation runner execution
legacy 19347 regression
prior task category selection
PlayMode selection
unfiltered test run
full regression run
```

실제 문제가 발생해 더 넓은 검증이 필요하다고 판단되면 조용히 실행하지 않는다.  
Result에 trigger owner, broken invariant, focused proof insufficiency, requested wider verification을 기록하고 STOP한다.

문제가 없다면 Result에 반드시 기록한다.

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
VALIDATION RUNNER EXECUTIONS: 0
CSV AUTHORING WRITES: 0
```

## 12. Result 필수 기록

Result 파일:

```text
MapDesign/MCP/REPORTS/MAP20_04_IMPLEMENT_CSV_NAVIGATION_AND_VALIDATION_JUMP_RESULT.md
```

Result에는 아래 섹션을 포함한다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
## CSV Navigation Summary
## Validation Jump Summary
## Snapshot and Digest Summary
## Focused Validation Summary
## No Legacy Regression Boundary Notes
## Final Status Evidence
```

`CSV Navigation Summary`에는 아래 값을 채운다.

```text
MAP20_03 Result SHA-256 required/actual:
MAP20_03 installed Task SHA-256 required/actual:
MAP20_04 handoff digest required/actual:
MAP20_03 detail snapshot digest reused:
MAP20_03 combined detail digest reused:

source kind tokens:
source location required fields present/required:
source records:
source available records:
source missing records:
file/row/column records:
copy path-row-column action count:
CSV authoring writes:

duplicated generator logic count:
duplicated validation logic count:
duplicated rollback logic count:
duplicated overlay/detail inspector logic count:
duplicated CSV parser/export logic count:
duplicated jump target catalog count:
hard-coded source path copies outside sample/precondition constants:
hard-coded digest string copies outside precondition/constants:
```

`Validation Jump Summary`에는 아래 값을 채운다.

```text
jump target kind tokens:
jump target required fields present/required:
validation error id records:
Tile targets:
Pattern targets:
Cluster targets:
Socket targets:
Slot targets:
MissingData targets:
selection-only inspector jumps:
validation runner executions:
external process launches:
```

`Snapshot and Digest Summary`에는 아래 값을 채운다.

```text
sample artifacts created:
csv navigation index required fields present/required:
validation jump sample required fields present/required:
source navigation digest manifest required fields present/required:
csv navigation index digest lower-hex SHA-256:
validation jump sample digest lower-hex SHA-256:
source navigation digest manifest lower-hex SHA-256:
MAP20_05 handoff digest lower-hex SHA-256:
```

`Focused Validation Summary`에는 아래 값을 채운다.

```text
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

`No Legacy Regression Boundary Notes`에는 반드시 아래 블록을 포함한다.

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
VALIDATION RUNNER EXECUTIONS: 0
CSV AUTHORING WRITES: 0
```

## 13. PASS 조건

PASS 조건:

```text
MAP20_03 Result and installed Task SHA match
MAP20_04 handoff digest matches
CSV source location maps error id to exact file/row/column/field/record id
Jump target kinds are exactly Tile / Pattern / Cluster / Socket / Slot
Missing source/data remains explicit and does not invent row/column or inspector data
Window opens without validation, generation, rollback, replay, export, or CSV write
Jump to inspector changes only view/selection state
Sample artifacts are deterministic and have lower-hex SHA-256 digests
Focused MAP20_04 EditMode tests are 8/8 PASS
No MAP19_09 scale audit rerun
No MAP20_01 generator run rerun
No MAP20_02 overlay sample regeneration
No MAP20_03 detail sample regeneration
No validation runner execution
No CSV authoring write
No legacy regression, prior categories, PlayMode, unfiltered tests, or full regression
No Scene/Prefab/Tilemap/runtime behavior mutation
Responsibility and Added Scripts table is present and specific
MAP20_05 remains LOCKED and not started
```

## 14. FAIL 조건

FAIL 조건:

```text
Window opens and starts validation, generation, rollback, replay, export, or CSV write
Jump target list contains extra/missing target kinds
Error id resolves to guessed or invented row/column
Missing source data is silently treated as valid source
Jump changes generated data, source CSV, project assets, or runtime objects
CSV parser/export/generator/validation/rollback/detail inspector logic is duplicated or rewritten
MAP19_09 scale audit is rerun without a real documented trigger
MAP20_01 generator run is rerun without a real documented trigger
MAP20_02 overlay sample is regenerated without a real documented trigger
MAP20_03 detail sample is regenerated without a real documented trigger
Validation runner is executed
CSV authoring file is written
Legacy 19347 or full regression is executed
PlayMode is executed
Scene/Prefab/Tilemap/runtime behavior is mutated
MAP20_05 starts
```

## 15. BLOCKED 조건

BLOCKED 조건:

```text
Precondition SHA mismatch
Compile error unrelated to this task that prevents focused validation
Existing source location cannot be represented without modifying CSV parser/export surfaces
Existing inspectors cannot accept selection-only target without generation/validation side effects
EditorWindow cannot open without triggering validation, generation, rollback, replay, export, or CSV write
```

When BLOCKED:

```text
Do not finalize MAP20_04
Do not start MAP20_05
Do not publish MAP20_05 handoff digest
Record exact owner, invariant, and minimum next action
STOP
```

## 16. Finalize 규칙

PASS일 때만:

```text
MAP20_04_IMPLEMENT_CSV_NAVIGATION_AND_VALIDATION_JUMP: COMPLETE
MAP20_05_IMPLEMENT_REPLAY_AUTHORING_INTEGRATION_HUD_AND_EXPORT: LOCKED
```

FAIL 또는 BLOCKED일 때:

```text
MAP20_04 remains CURRENT
MAP20_05 remains LOCKED
STOP
```

다음 Task는 내가 Result를 검수한 뒤 별도 MD로 준다.

## 17. Commit 규칙

PASS일 때만 atomic commit을 만든다.

Commit subject:

```text
MAP20_04 implement csv navigation jump
```

Commit body에는 다음을 포함한다.

```text
MAP20_04 responsibilities
focused test count
source location fields
jump target kind tokens
snapshot digests
MAP20_05 handoff digest
legacy regression counters all zero
CSV authoring writes zero
```

Git push는 하지 않는다.

## 18. STOP

Result 작성, 상태 finalize, commit 이후 반드시 멈춘다.

```text
DO NOT START MAP20_05.
DO NOT CREATE MAP20_05 FILES.
DO NOT RERUN MAP19_09 SCALE AUDIT.
DO NOT RERUN MAP20_01 GENERATOR RUN.
DO NOT REGENERATE MAP20_02 OVERLAY SAMPLE.
DO NOT REGENERATE MAP20_03 DETAIL SAMPLE.
DO NOT RUN VALIDATION RUNNER.
DO NOT WRITE CSV AUTHORING FILES.
DO NOT RUN LEGACY REGRESSION.
STOP.
```
