```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP20_01_CREATE_GENERATOR_WINDOW_AND_SCOPED_ROLLBACK
  task_file: TASKS/MAP20_01_CREATE_GENERATOR_WINDOW_AND_SCOPED_ROLLBACK.md
  requires_current_task: NONE
  requires_completed_task: MAP19_09_MAP19_SCALE_AND_EXIT_AUDIT
  requires_result:
    path: REPORTS/MAP19_09_MAP19_SCALE_AND_EXIT_AUDIT_RESULT.md
    status: PASS
    sha256: 1dfbe8c14380266b42bbd34d8cd0a39e394ff9e8b79ac9461c5a5af26dcbbadf
  requires_installed_task:
    path: TASKS/MAP19_09_MAP19_SCALE_AND_EXIT_AUDIT.md
    sha256: b772a2417567ffeaf7afe59935ca1fa325fd31cab737ce94fba06ec300b979a0
  requires_handoff:
    name: MAP20_01 handoff digest
    sha256: 2e69b77f8720d30e6c1d9fcdd7bdeb883ea36540033422e4908367ce60377c3ce
  sets_current_task: MAP20_01_CREATE_GENERATOR_WINDOW_AND_SCOPED_ROLLBACK
```

# MAP20_01 - Create Generator Window and Scoped Rollback

```text
TASK: MAP20_01_CREATE_GENERATOR_WINDOW_AND_SCOPED_ROLLBACK
PHASE: MAP20 - Tooling / Authoring Control Surface
STATUS: CURRENT
NEXT: MAP20_02_CREATE_WORLD_OVERLAYS_AND_SECTOR_CANVAS_INSPECTOR
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP20의 첫 작업으로, generated map pipeline을 사람이 안전하게 실행하고 되돌릴 수 있는 최소 Editor 제어판을 만든다.

이번 Task는 **생성기 동작을 새로 바꾸는 작업이 아니다.**  
이미 MAP09~19에서 만든 planner, render, slice, bake, validation, scale-audit surface를 호출하는 얇은 도구 계층을 만든다.

이번 Task의 책임:

```text
1. Unity Editor 메뉴에서 열 수 있는 Generated Terrain Generator Window를 추가한다.
2. seed, generator version, data version, scope, input hash, pass/fail, artifact hash를 기록한다.
3. Pattern / Sector / 1-ring / World 네 가지 실행 scope를 선택할 수 있게 한다.
4. 실행 전 rollback snapshot을 만들고, 실패 또는 사용자의 rollback 요청 시 scope 안의 generated output만 되돌린다.
5. 실패 시 MAP19_08 failure bundle과 MAP19_09 scale audit handoff를 연결할 수 있는 replay/failure reference를 남긴다.
6. Result 첫머리에 추가 script와 각 책임을 사용자용 표로 보고한다.
```

아직 하지 않는 일:

```text
world overlay visualization
48x32 sector canvas inspector
4x4 pattern inspector
12x8 slice inspector
CSV row/column jump
runtime HUD
production seed approval
gameplay object spawn
Scene/Prefab/Tilemap authoring mutation
```

## 1. 사용자 보고 의무

Result 첫 부분에는 반드시 아래 두 섹션을 둔다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

`User-Facing Implementation Report`에는 아래 질문에 답한다.

```text
이번 작업이 어떤 창/도구를 추가했는가?
사용자가 입력하거나 선택할 수 있는 값은 무엇인가?
Pattern / Sector / 1-ring / World scope는 각각 어디까지 실행하는가?
seed/version/hash/pass artifact에는 무엇이 들어가는가?
rollback snapshot은 무엇을 저장하고 무엇을 건드리지 않는가?
실패 시 어떤 replay/failure reference가 남는가?
이번 작업에서 생성기 로직 자체를 변경하지 않았다는 증거는 무엇인가?
이번 작업에서 legacy regression을 돌리지 않았다는 증거는 무엇인가?
```

`Responsibility and Added Scripts`는 표로 작성한다.

```text
| Script or file | Added or changed responsibility | Explicit non-ownership |
```

파일별 책임이 없으면 FAIL이다.

## 2. 선행조건

작업 시작 전에 다음을 확인한다.

```text
MapDesign/MCP/REPORTS/MAP19_09_MAP19_SCALE_AND_EXIT_AUDIT_RESULT.md exists
MAP19_09 Result STATUS: PASS
MAP19_09 Result SHA-256:
1dfbe8c14380266b42bbd34d8cd0a39e394ff9e8b79ac9461c5a5af26dcbbadf

MapDesign/MCP/TASKS/MAP19_09_MAP19_SCALE_AND_EXIT_AUDIT.md exists
MAP19_09 installed Task SHA-256:
b772a2417567ffeaf7afe59935ca1fa325fd31cab737ce94fba06ec300b979a0

MAP20_01 handoff digest:
2e69b77f8720d30e6c1d9fcdd7bdeb883ea36540033422e4908367ce60377c3ce

MAP20_02_CREATE_WORLD_OVERLAYS_AND_SECTOR_CANVAS_INSPECTOR: LOCKED
```

불일치 시:

```text
STATUS: BLOCKED
reason: precondition mismatch
created/changed code files: 0
MAP20_02 started: NO
STOP
```

## 3. 허용 범위

허용:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Tooling/GeneratedTerrainRunArtifact.cs
Assets/_Game/Map/Runtime/WorldGeneration/Tooling/GeneratedTerrainRunScope.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Tooling/GeneratedTerrainGeneratorWindow.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Tooling/GeneratedTerrainRunCoordinator.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Tooling/GeneratedTerrainGeneratorWindowTests.cs
MapDesign/MCP/GENERATED/MAP20_01/**
MapDesign/MCP/TASKS/MAP20_01_CREATE_GENERATOR_WINDOW_AND_SCOPED_ROLLBACK.md
MapDesign/MCP/REPORTS/MAP20_01_CREATE_GENERATOR_WINDOW_AND_SCOPED_ROLLBACK_RESULT.md
MapDesign/MCP_ARCHIVE/MAP20_01_CREATE_GENERATOR_WINDOW_AND_SCOPED_ROLLBACK.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

조건부 허용:

```text
Existing headless/scale audit editor runner may be referenced, not rewritten.
Existing generated output directories may be read for before-state digests.
Existing generated output under the selected scope may be replaced only through the new rollback-safe coordinator.
Editor menu registration may add one menu item under existing Map/MapAuthoring menu conventions.
```

금지:

```text
planner algorithm rewrite
pattern renderer behavior rewrite
cluster placement behavior rewrite
activity/event/special/population behavior rewrite
movement/physics/player behavior change
MAP19 validation proof rewrite
MAP19 scale audit rerun
legacy 19347 regression
prior task category selection
PlayMode selection
unfiltered test run
full regression run
manual gameplay test
Scene / Prefab / Tilemap authoring mutation
Tilemap.SetTile / SetTiles / SetTilesBlock / ClearAllTiles outside generated-output test doubles
real screenshot capture from camera
runtime GameObject instantiate/enable/disable/destroy
production seed approval
automatic MAP20_02 start
broad refactor or optimization rewrite
```

## 4. Scope 계약

Window는 정확히 네 scope만 제공한다.

| Scope | Required inputs | Execution ownership | Rollback boundary |
|---|---|---|---|
| `Pattern` | seed, biome/profile, pattern id or auto candidate | one 4x4 pattern render request | generated pattern artifact only |
| `Sector` | seed, sector coordinate or id | one sector plan/render/slice request | one sector output directory |
| `OneRing` | seed, center sector coordinate or id | center sector plus in-bounds Moore 1-ring | at most 9 sector output directories |
| `World` | seed, world version | full generated world artifact request | world generated output root for that run id |

필수:

```text
Scope enum/string tokens are exact: Pattern, Sector, OneRing, World.
OneRing uses the MAP15 rollback definition: center + in-bounds Moore 1-ring, max 9 sectors.
World scope does not mean production approval.
Scope execution must produce a dry-run/plan artifact even when no generation output changes.
```

## 5. Run Artifact 계약

Every run attempt creates a deterministic run artifact.

Required fields:

```text
schema_version
task_id
run_id
scope
seed
generator_version
data_version
input_digest
pre_run_output_digest
post_run_output_digest
pass_state
failure_owner
failure_reason
rollback_available
rollback_snapshot_digest
rollback_applied
changed_path_count
created_path_count
deleted_path_count
replay_reference
failure_bundle_reference
MAP19_exit_digest
MAP20_01_handoff_digest
created_utc_excluded_from_canonical_digest
canonical_digest
```

Digest rules:

```text
created_utc may be stored but must be excluded from canonical digest.
Path lists must be normalized with forward slash and sorted ordinally.
Digest must be lowercase SHA-256.
The same in-memory run facts must produce the same canonical digest across repeat/culture/path-order changes.
```

## 6. Rollback 정책

Rollback is allowed only for generated output owned by the selected scope.

필수:

```text
Before running, capture a rollback snapshot of the selected generated-output boundary.
Do not snapshot or restore Assets, Scenes, Prefabs, ScriptableObjects, or source CSV authoring tables.
Do not delete files outside the selected scope.
Do not rollback by git command.
Do not use git checkout/reset/clean.
Rollback operation must be atomic per scope: either all selected generated outputs are restored or the Result reports BLOCKED with no success digest.
Rollback after success may be manually invoked through the window/coordinator and must produce a rollback artifact.
Rollback after failure must happen automatically unless the failure occurs before any output mutation.
```

Rollback output:

```text
MapDesign/MCP/GENERATED/MAP20_01/runs/<run_id>/run_artifact.json
MapDesign/MCP/GENERATED/MAP20_01/runs/<run_id>/rollback_snapshot_manifest.json
MapDesign/MCP/GENERATED/MAP20_01/runs/<run_id>/rollback_result.json
```

## 7. Window UI 최소 요구

The EditorWindow may use IMGUI or UI Toolkit following existing project conventions.

Required controls:

```text
Seed input
Scope selector: Pattern / Sector / OneRing / World
Version/hash display
Run button
Step button or dry-run plan button
Rollback button
Last run status display
Last artifact digest display
Open output folder button
Copy replay reference button
```

Required safety states:

```text
Run button disabled while a run is active.
Rollback button disabled when rollback_available is false.
World scope requires an explicit checkbox or confirmation flag in the window state.
Failure state displays owner/reason/replay reference.
The window must open without triggering generation.
```

## 8. 중복과 하드코딩 방지

필수:

```text
Do not duplicate generation pipeline logic in the window.
Do not duplicate MAP19 validation logic.
Do not hard-code the same scope list in more than one production location.
Do not hard-code digest constants outside precondition/handoff constants.
Coordinator owns execution state; window owns display/input only.
Runtime artifact model owns deterministic serialization; editor coordinator owns file writes.
```

Result에 아래를 기록한다.

```text
duplicated generator logic count:
duplicated validation logic count:
hard-coded scope list copies:
hard-coded digest string copies outside precondition/constants:
```

목표값:

```text
duplicated generator logic count: 0
duplicated validation logic count: 0
hard-coded scope list copies: 1 centralized definition
hard-coded digest string copies outside precondition/constants: 0
```

## 9. Focused Tests

Focused test category:

```text
MAP20_01
```

Required test names:

```text
GeneratorWindowOpensWithoutStartingGeneration
GeneratorRunArtifactCapturesSeedVersionHashScopePassAndReplay
GeneratorRunScopesAreExactlyPatternSectorOneRingAndWorld
OneRingScopeUsesInBoundsMooreRadiusOneAndMaxNineSectors
RunCoordinatorCreatesRollbackSnapshotBeforeOutputMutation
RollbackRestoresOnlySelectedGeneratedOutputScope
FailureRunPublishesReplayAndNoMap20_02Handoff
GeneratorWindowDoesNotSelectLegacyRegressionPriorCategoriesPlayModeOrFullRegression
```

Required count:

```text
discovered: 8
executed: 8
passed: 8
failed: 0
```

Focused validation may create temporary generated outputs only under:

```text
MapDesign/MCP/GENERATED/MAP20_01/test_runs
```

## 10. 회귀 금지 정책

이번 Task의 검증은 focused MAP20_01 EditMode selection과 compile/console check까지만 허용한다.

문제가 실제로 발생하지 않은 상태에서 더 넓은 검증을 돌리지 않는다.

금지:

```text
MAP19_09 scale audit rerun
legacy 19347 regression
prior task category selection
PlayMode selection
unfiltered test run
full regression run
```

실제 문제가 발생해 더 넓은 검증이 필요하다고 판단되면 조용히 실행하지 않는다.  
Result에 아래 정보를 기록하고 STOP한다.

```text
REGRESSION TRIGGER DETECTED: YES
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
MAP19_09 SCALE AUDIT RERUNS: 0
```

## 11. Result 필수 기록

Result 파일:

```text
MapDesign/MCP/REPORTS/MAP20_01_CREATE_GENERATOR_WINDOW_AND_SCOPED_ROLLBACK_RESULT.md
```

Result에는 아래 섹션을 포함한다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
## Generator Window and Scope Summary
## Run Artifact and Rollback Summary
## Focused Validation Summary
## No Legacy Regression Boundary Notes
## Final Status Evidence
```

`Generator Window and Scope Summary`에는 아래 값을 채운다.

```text
MAP19_09 Result SHA-256 required/actual:
MAP19_09 installed Task SHA-256 required/actual:
MAP20_01 handoff digest required/actual:
MAP19 exit digest reused:

window menu path:
window opens without run:
scope tokens:
world scope confirmation:
run controls:
rollback controls:
status/digest/replay display:

duplicated generator logic count:
duplicated validation logic count:
hard-coded scope list copies:
hard-coded digest string copies outside precondition/constants:
```

`Run Artifact and Rollback Summary`에는 아래 값을 채운다.

```text
run artifact schema version:
run artifact required fields present/required:
sample run artifacts created:
sample rollback snapshots created:
sample rollback operations executed:
rollback output roots:
changed path count in focused tests:
created path count in focused tests:
deleted path count in focused tests:
paths touched outside MAP20_01 generated output:
Scene/Prefab/Tilemap mutation count:
runtime object mutation count:

run artifact schema digest lower-hex SHA-256:
sample run artifact digest lower-hex SHA-256:
rollback snapshot digest lower-hex SHA-256:
rollback result digest lower-hex SHA-256:
MAP20_02 handoff digest lower-hex SHA-256:
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
```

## 12. PASS 조건

PASS 조건:

```text
MAP19_09 Result and installed Task SHA match
MAP20_01 handoff digest matches
EditorWindow opens through a menu item and does not start generation on open
Scope tokens are exactly Pattern / Sector / OneRing / World
Run artifact records seed/version/hash/scope/pass/failure/replay/rollback fields
Rollback snapshot is captured before output mutation
Rollback restores only selected generated output scope
World scope has an explicit confirmation state
Focused MAP20_01 EditMode tests are 8/8 PASS
No MAP19_09 scale audit rerun
No legacy regression, prior categories, PlayMode, unfiltered tests, or full regression
No Scene/Prefab/Tilemap/runtime behavior mutation
Responsibility and Added Scripts table is present and specific
MAP20_02 remains LOCKED and not started
```

## 13. FAIL 조건

FAIL 조건:

```text
Window opens and starts generation automatically
Scope list contains extra or missing tokens
OneRing exceeds radius 1 or max 9 sectors
Rollback touches files outside the selected generated-output scope
Rollback uses git reset/checkout/clean
Run artifact omits seed/version/hash/scope/pass/replay/rollback fields
Failure publishes MAP20_02 handoff
MAP19_09 scale audit is rerun without a real documented trigger
Legacy 19347 or full regression is executed
PlayMode is executed
Scene/Prefab/Tilemap/runtime behavior is mutated
Generator pipeline logic is duplicated or rewritten
MAP20_02 starts
```

## 14. BLOCKED 조건

BLOCKED 조건:

```text
Precondition SHA mismatch
Compile error unrelated to this task that prevents focused validation
Existing generation public API cannot be called without rewriting generator behavior
Safe generated-output root cannot be established
Rollback snapshot cannot be made atomic
```

When BLOCKED:

```text
Do not finalize MAP20_01
Do not start MAP20_02
Do not publish MAP20_02 handoff digest
Record exact owner, invariant, and minimum next action
STOP
```

## 15. Finalize 규칙

PASS일 때만:

```text
MAP20_01_CREATE_GENERATOR_WINDOW_AND_SCOPED_ROLLBACK: COMPLETE
MAP20_02_CREATE_WORLD_OVERLAYS_AND_SECTOR_CANVAS_INSPECTOR: LOCKED
```

FAIL 또는 BLOCKED일 때:

```text
MAP20_01 remains CURRENT
MAP20_02 remains LOCKED
STOP
```

다음 Task는 내가 Result를 검수한 뒤 별도 MD로 준다.

## 16. Commit 규칙

PASS일 때만 atomic commit을 만든다.

Commit subject:

```text
MAP20_01 create generator window rollback
```

Commit body에는 다음을 포함한다.

```text
MAP20_01 responsibilities
focused test count
scope tokens
sample artifact digest
rollback digest
MAP20_02 handoff digest
legacy regression counters all zero
```

Git push는 하지 않는다.

## 17. STOP

Result 작성, 상태 finalize, commit 이후 반드시 멈춘다.

```text
DO NOT START MAP20_02.
DO NOT CREATE MAP20_02 FILES.
DO NOT RERUN MAP19_09 SCALE AUDIT.
DO NOT RUN LEGACY REGRESSION.
STOP.
```
