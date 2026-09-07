```yaml
mapdesign_work_order:
  format: direct_visible_map_work_order_v2
  task_id: VIS02_GENERATOR_WINDOW_ACTUAL_RUN_AND_REGENERATE_SCENE
  based_on_previous_visible_result:
    path: MapDesign/MCP/REPORTS/VIS01_BUILD_MOONPALACE_GRAYBOX_EXAMPLE_V1_RESULT.md
    status: PASS
    sha256: 7b06ee95ce0973d996c5af72445f6d3840ace57ec6b4ee581cd4fe0fca6a5036
  requires_completed_task: VIS01_BUILD_MOONPALACE_GRAYBOX_EXAMPLE_V1
  expected_duration: 1_to_2_hours
  work_style: larger_visible_editor_integration
```

# VIS02 - Generator Window Actual Run and Regenerate Scene

```text
TASK: VIS02_GENERATOR_WINDOW_ACTUAL_RUN_AND_REGENERATE_SCENE
PHASE: Visible Map Generation / MoonPalace
STATUS: CURRENT
NEXT: VIS03_GENERATE_3X3_ONE_RING_GRAYBOX_SCENE
NEXT STATUS: NOT STARTED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

VIS01은 첫 번째 48x32 MoonPalace one-sector graybox Scene을 만들었다.

VIS02의 목표는 그 생성 과정을 Unity Editor 안에서 다시 실행 가능한 실제 작업 흐름으로 연결하는 것이다.

이번 작업의 PASS 기준:

```text
Unity Editor에서 MoonPalace graybox generation을 Dry-run / Run으로 실행할 수 있다.
Run은 VIS01 isolated Scene과 CSV/JSON manifest를 실제로 재생성한다.
Dry-run은 같은 입력의 예상 digest/count/validation만 보여주고 Scene을 저장하지 않는다.
```

이 작업은 전체 맵 생성이 아니다. 한 섹터 예시 Scene을 “한 번 만든 파일”에서 “Editor에서 다시 생성 가능한 흐름”으로 끌어올리는 작업이다.

## 1. 핵심 책임

| Area | This task owns | This task must not do |
|---|---|---|
| Editor entrypoint | MoonPalace graybox Dry-run/Run command or window integration | full Generator Window rewrite |
| Run request | seed id/value, sector, output root, dry-run/run mode, deterministic digest | arbitrary user seed approval |
| Actual run | VIS01 generator and Scene builder 호출, isolated Scene 재생성 | full 13x13 world generation |
| Dry-run | count/digest/validation preview without Scene mutation | fake success without running generation logic |
| Safety | scoped rollback/overwrite only inside VIS01/VIS02 roots | existing Scene/Prefab/Build Settings mutation |

This task may add a small dedicated MoonPalace graybox window or menu item if modifying the existing generic Generator Window is risky. If the existing `GeneratedTerrainGeneratorWindow` has a clean extension point, wire through that extension point. Do not perform a broad Editor UI rewrite.

## 2. 사용자 보고 의무

Result 첫 부분에는 반드시 아래 두 섹션을 둔다.

```text
# User-Facing Implementation Report
# Responsibility and Added Scripts
```

`User-Facing Implementation Report`에는 아래 질문에 답한다.

```text
Unity Editor에서 어떤 메뉴/창/버튼으로 MoonPalace graybox generation을 실행할 수 있는가?
Dry-run과 Run은 각각 무엇을 만들고 무엇을 저장하지 않는가?
Run이 VIS01 Scene을 실제로 삭제/교체/재생성했다는 증거는 무엇인가?
생성 결과가 VIS01과 같은 seed/sector/500개 후보/16 MicroChunk 구조를 유지한다는 증거는 무엇인가?
run history, manifest, digest, rollback boundary는 어디에 기록되는가?
기존 Scene/Prefab/Build Settings를 변경하지 않았다는 증거는 무엇인가?
이번 Task에서 추가/변경한 script와 각 책임은 무엇인가?
아직 full world, live traversal, production art, NPC/combat/shop/save, player build가 아니라는 경계는 무엇인가?
legacy 19347, prior category, PlayMode, full/unfiltered test를 하지 않았다는 증거는 무엇인가?
VIS03은 왜 아직 시작하지 않았는가?
```

`Responsibility and Added Scripts`는 반드시 표로 작성한다.

```text
| Script or file | Added or changed responsibility | Explicit non-ownership |
```

파일별 책임이 구체적이지 않으면 FAIL이다.

## 3. 선행조건

작업 시작 전에 아래를 확인한다.

```text
MapDesign/MCP/REPORTS/VIS01_BUILD_MOONPALACE_GRAYBOX_EXAMPLE_V1_RESULT.md exists
TASK: VIS01_BUILD_MOONPALACE_GRAYBOX_EXAMPLE_V1
STATUS: PASS
Result SHA-256:
7b06ee95ce0973d996c5af72445f6d3840ace57ec6b4ee581cd4fe0fca6a5036

VIS01 required scene exists:
Assets/_Game/Map/Scenes/MoonPalace/VIS01/MoonPalaceGrayboxExample_VIS01.unity

VIS01 generator-owned source compiles.
Current official MAP task is NONE.
```

If VIS01 Result is missing/not PASS, or the VIS01 Scene is missing:

```text
STATUS: BLOCKED
reason: VIS01 visible scene baseline not available
created/changed production code files: 0
STOP
```

## 4. 허용 범위

허용 파일:

```text
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/Visualization/MoonPalaceGrayboxRunRequest.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/Visualization/MoonPalaceGrayboxRunResult.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/MoonPalace/Visualization/MoonPalaceGrayboxGeneratorWindow.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/MoonPalace/Visualization/MoonPalaceGrayboxRunCoordinator.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/MoonPalace/Visualization/MoonPalaceGrayboxRunHistoryPublisher.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/MoonPalace/Visualization/MoonPalaceGrayboxGeneratorWindowTests.cs
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/VIS02/**
Assets/_Game/Map/Scenes/MoonPalace/VIS01/**
MapDesign/MCP/GENERATED/VIS02/**
MapDesign/MCP/TASKS/VIS02_GENERATOR_WINDOW_ACTUAL_RUN_AND_REGENERATE_SCENE.md
MapDesign/MCP/REPORTS/VIS02_GENERATOR_WINDOW_ACTUAL_RUN_AND_REGENERATE_SCENE_RESULT.md
MapDesign/MCP_ARCHIVE/VIS02_GENERATOR_WINDOW_ACTUAL_RUN_AND_REGENERATE_SCENE.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

조건부 허용:

```text
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Tooling/GeneratedTerrainGeneratorWindow.cs
may be minimally changed only if a small extension hook is required.

VIS01 source files may be referenced and called, but not rewritten unless a VIS02-owned compile error requires a narrow public method exposure.

The VIS01 isolated Scene may be regenerated because that is this task's visible output.
```

금지:

```text
MAP00~MAP21 source rewrite
VIS01 algorithm rewrite for different generation results
MAP21 authoring CSV rewrite
hardcoded sample map counted as generation
silent fallback carve
whole-world 13x13 generation
3x3 one-ring generation
existing user Scene/Prefab mutation outside Assets/_Game/Map/Scenes/MoonPalace/VIS01
Build Settings mutation
Addressables group/build change
runtime gameplay GameObject mutation outside isolated Editor Scene build
NPC/combat/shop/reward/save runtime execution
PlayMode tests
legacy 19347 regression
prior category rerun
unfiltered/full test run
player build execution
VIS03 or later task start
```

Compile repair exception:

```text
If VIS02-owned code fails to compile, fix only VIS02-owned files.
If older VIS01 source needs a small public entrypoint to be called safely, add the smallest compatible method and report it explicitly.
If older MAP source fails to compile, return STATUS: BLOCKED with exact owner and error.
```

## 5. 구현 요구

### 5.1 Run request/result model

Create:

```text
MoonPalaceGrayboxRunRequest.cs
MoonPalaceGrayboxRunResult.cs
```

Required request fields:

```text
mode: DryRun or Run
seed_id: MP_QA_01
seed_value: 1924737067
sector_x: 6
sector_y: 6
candidate_count: 500
output_scene_path
authoring_output_root
generated_output_root
request_digest
```

Required result fields:

```text
mode
success
scene_written
scene_deleted_before_write
scene_recreated
catalog_digest
candidate_digest
logical_map_digest
scene_manifest_digest
csv_count
json_count
scene_count
validation_summary
write_scope_summary
non_execution_summary
failure_reason
```

Dry-run result:

```text
scene_written: false
scene_deleted_before_write: false
scene_recreated: false
csv/json write count: 0 unless writing a VIS02 dry-run report under MapDesign/MCP/GENERATED/VIS02
logical generation must execute in memory
counts and digest must be present
```

Run result:

```text
scene_written: true
scene_recreated: true
VIS01 scene path is written
VIS01 CSV/JSON generation artifacts may be refreshed only under VIS01 roots
VIS02 run history/manifest is written under VIS02 roots
```

### 5.2 Editor command/window

Create:

```text
MoonPalaceGrayboxGeneratorWindow.cs
MoonPalaceGrayboxRunCoordinator.cs
```

Minimum UI:

```text
Menu item under MapDesign/MoonPalace/Graybox Generator
readonly seed id/value fields defaulting to MP_QA_01 / 1924737067
readonly sector fields defaulting to 6,6
Dry-run button
Run button
Open Scene button or ping scene asset command
last run summary area showing digest/count/status
```

The UI may be plain and functional. It does not need polished styling.

If a dedicated window is too risky in the current Unity version, a menu command pair is acceptable:

```text
MapDesign/MoonPalace/Graybox/Dry Run VIS01 Example
MapDesign/MoonPalace/Graybox/Run VIS01 Example Scene
MapDesign/MoonPalace/Graybox/Open VIS01 Example Scene
```

But the Result must explain the actual entrypoint.

### 5.3 Actual generation binding

The Run coordinator must call the existing VIS01 generation flow rather than duplicating it.

Required sequence:

```text
build MoonPalaceRuntimeCatalogSnapshot
build/select 500 MicroPattern candidates
compose 16 reachable 12x8 MicroChunks
generate 48x32 one-sector logical result
validate route/recovery/seam/protection/marker summary
for Run only: rebuild isolated VIS01 Scene
write VIS02 run history/manifest
```

Required fixed counts:

```text
raw 4x4 mask count: 65,536
accepted 4x4 candidate count: 500
sector pattern placements: 96
microchunks: 16
logical layer records: 10,752
route/recovery/seam failures: 0/0/0
unreachable microchunks: 0
fallback carve/silent repair: 0/0
```

### 5.4 Scoped overwrite and run history

Create:

```text
MoonPalaceGrayboxRunHistoryPublisher.cs
```

Required VIS02 generated files:

```text
MapDesign/MCP/GENERATED/VIS02/moonpalace_vis02_dry_run_result.json
MapDesign/MCP/GENERATED/VIS02/moonpalace_vis02_run_result.json
MapDesign/MCP/GENERATED/VIS02/moonpalace_vis02_run_history.csv
MapDesign/MCP/GENERATED/VIS02/moonpalace_vis02_write_scope_manifest.json
MapDesign/MCP/GENERATED/VIS02/moonpalace_vis02_digest_manifest.json
```

The write scope manifest must list every path written by Dry-run and Run and classify it as:

```text
VIS01_REFRESH
VIS02_HISTORY
TASK_STATUS_OR_REPORT
```

It must prove:

```text
existing Scene/Prefab mutations outside VIS01: 0
Build Settings mutations: 0
Addressables mutations: 0
full-world output writes: 0
```

## 6. Focused Tests

Run only focused EditMode tests with category:

```text
VIS02
```

Create:

```text
MoonPalaceGrayboxGeneratorWindowTests.cs
```

Required test names:

```text
MoonPalaceGrayboxRunRequestDefaultsToMpQa01CenterSectorAnd500Candidates
MoonPalaceGrayboxDryRunExecutesGenerationWithoutWritingScene
MoonPalaceGrayboxRunRegeneratesIsolatedVis01SceneAndManifest
MoonPalaceGrayboxRunCoordinatorReusesVis01GenerationFlowWithoutStaticMapCopy
MoonPalaceGrayboxRunPreserves1536Cells96Patterns16ChunksAnd10752LayerRecords
MoonPalaceGrayboxRunKeepsRouteRecoverySeamAndUnreachableFailuresZero
MoonPalaceGrayboxRunHistoryRecordsDryRunRunDigestsAndWriteScope
MoonPalaceGrayboxWindowOrMenuEntrypointExistsAndReportsLastRunSummary
MoonPalaceGrayboxOpenSceneCommandTargetsOnlyVis01Scene
MoonPalaceGrayboxWriteScopeRejectsExistingScenePrefabBuildSettingsAndAddressablesMutation
MoonPalaceGrayboxDigestIsRepeatReverseAndCultureStable
MoonPalaceGrayboxWorkDoesNotRunLegacyRegressionPlayModeBuildFullWorldOrVis03
```

Expected focused test result:

```text
Discovered: 12
Executed: 12
Passed: 12
Failed: 0
Skipped: 0
Inconclusive: 0
```

No other category may run unless there is an actual compile error and the result explains why.

## 7. Result Requirements

Write:

```text
MapDesign/MCP/REPORTS/VIS02_GENERATOR_WINDOW_ACTUAL_RUN_AND_REGENERATE_SCENE_RESULT.md
```

The Result must include:

```text
TASK: VIS02_GENERATOR_WINDOW_ACTUAL_RUN_AND_REGENERATE_SCENE
STATUS: PASS or FAIL or BLOCKED

# User-Facing Implementation Report
# Responsibility and Added Scripts
# Editor Entrypoint Summary
# Dry Run Summary
# Actual Run and Scene Regeneration Summary
# Write Scope and Safety Summary
# Artifact and Digest Summary
# Focused Validation Summary
# No Legacy Regression Boundary Notes
# Final Status Evidence
```

Required PASS evidence:

```text
editor entrypoint path/menu: present
dry-run executed: 1
dry-run scene writes: 0
actual run executed: 1
actual run scene writes: 1
scene path: Assets/_Game/Map/Scenes/MoonPalace/VIS01/MoonPalaceGrayboxExample_VIS01.unity
scene recreated: YES
seed id/value: MP_QA_01 / 1924737067
sector: 6,6
accepted 4x4 candidate count: 500
sector pattern placements: 96 / 96
microchunks: 16 / 16
logical layer records: 10,752
route/recovery/seam failures: 0 / 0 / 0
unreachable microchunk count: 0
fallback carve/silent repair: 0 / 0
VIS02 generated files: 5
existing Scene/Prefab mutations outside VIS01: 0
Build Settings mutations: 0
Addressables mutations: 0
full-world writes/runs: 0 / 0
```

Required responsibility table:

```text
| Script or file | Added or changed responsibility | Explicit non-ownership |
| MoonPalaceGrayboxRunRequest.cs | ... | ... |
| MoonPalaceGrayboxRunResult.cs | ... | ... |
| MoonPalaceGrayboxGeneratorWindow.cs | ... | ... |
| MoonPalaceGrayboxRunCoordinator.cs | ... | ... |
| MoonPalaceGrayboxRunHistoryPublisher.cs | ... | ... |
| MoonPalaceGrayboxGeneratorWindowTests.cs | ... | ... |
| VIS02 generated artifacts | ... | ... |
```

Required no-regression proof:

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
FULL REGRESSION RUNS: 0
FULL WORLD GENERATION RUNS: 0
EXISTING SCENE/PREFAB MUTATIONS: 0
BUILD SETTINGS MUTATIONS: 0
PLAYER BUILD EXECUTIONS: 0
VIS03 files/runs: 0 / 0
```

If PASS, the Result must say:

```text
MoonPalace one-sector graybox generation can now be regenerated from an Editor entrypoint.
This is not yet full-world generation, production art, live traversal, NPC/combat/shop/save runtime, or player build approval.
```

## 8. Commit Rule

If and only if Result status is PASS:

```text
record VIS02 as COMPLETE in MapDesign/MCP/06_IMPLEMENTATION_STATUS.md using the smallest clear status addition
commit only VIS02-owned source/test/artifact/task/report/status files and regenerated VIS01 isolated scene/artifacts
do not commit unrelated files
do not push
STOP
```

Do not start VIS03.
