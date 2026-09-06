```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP21_10_TUNE_REPETITION_DENSITY_AND_PACING
  task_file: TASKS/MAP21_10_TUNE_REPETITION_DENSITY_AND_PACING.md
  requires_current_task: NONE
  requires_completed_task: MAP21_09_COMPLETE_FORGE_BOSS_AND_OPTIONAL_REGIONS
  requires_result:
    path: REPORTS/MAP21_09_COMPLETE_FORGE_BOSS_AND_OPTIONAL_REGIONS_RESULT.md
    status: PASS
    sha256: d89be7ffda394341c96eb59196d01c1ec10802c62b82a4bbbc73223e682bf44c
  requires_installed_task:
    path: TASKS/MAP21_09_COMPLETE_FORGE_BOSS_AND_OPTIONAL_REGIONS.md
    sha256: 597bae41ef9bc8a1d1080a340186f3d10b0306b9d90aac184bc6657877e44b22
  requires_handoff:
    name: MAP21_10 handoff digest
    sha256: ce5e4ebc5e13ad55671a8046ad7641218efbedf9c6b1dff8df9f4cd20110de04
  sets_current_task: MAP21_10_TUNE_REPETITION_DENSITY_AND_PACING
```

# MAP21_10 - Tune Repetition, Density, and Pacing

```text
TASK: MAP21_10_TUNE_REPETITION_DENSITY_AND_PACING
PHASE: MAP21 - MoonPalace Vertical Slice
STATUS: CURRENT
NEXT: MAP21_11_LOCK_QA_SEEDS_AND_RUN_COMPLETION_PLAYTESTS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP21_01~09에서 작성된 MoonPalace production content를 다시 만들지 않고, MAP21 vertical slice의 final tuning 기준을 정적 데이터로 잠근다.

이번 Task는 다음 네 가지를 소유한다.

```text
1. global density windows
2. biome/pacing-role target profile
3. Activity/Event/Special density cap
4. Pattern/Cluster/Activity/Event/Boundary/Landmark repetition distance policy
```

이번 Task는 실제 seed를 대량 생성하거나, 플레이 테스트를 돌리거나, world/sector output을 다시 만들지 않는다. MAP21_11이 대표 seed 30개와 completion playtest를 담당한다.

| Area | This task owns | This task must not do |
|---|---|---|
| Density tuning | Quiet/Cluster/Activity/Overlay target windows | actual world generation or seed approval |
| Pacing tuning | biome and pacing-role target weights | route solver rewrite |
| Repetition tuning | structural/silhouette/pattern/activity/event/boundary repeat distance policy | pool regeneration or content rewrite |
| QA handoff | deterministic MAP21_11 tuning handoff manifest | MAP21_11 seed lock or playtest |

Core principle:

```text
This task defines the tuning contract that MAP21_11 will measure.
It does not run MAP21_11 measurements.
It does not mutate MAP21_01~09 content.
It does not execute generator, renderer, validation runner, replay, rollback, PlayMode, legacy regression, or full regression.
```

## 1. 사용자 보고 의무

Result 첫 부분에는 반드시 아래 두 섹션을 둔다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

`User-Facing Implementation Report`에는 아래 질문에 답한다.

```text
이번 작업이 어떤 density/pacing/repetition 책임을 추가했는가?
Quiet 50~60%, Cluster 25~35%, Activity 6~12%, Overlay 3~8% 기준을 어떻게 데이터화했는가?
biome별/role별 target이 source content를 다시 만들지 않고 어떻게 적용 가능한가?
silhouette 반복, cluster 반복, activity/event 반복, boundary 반복을 어떤 distance rule로 제한했는가?
MAP21_11이 측정할 handoff surface는 무엇인가?
이번 Task에서 추가한 script/CSV/JSON과 각 책임은 무엇인가?
MAP21_01~09 source artifact를 rewrite/regenerate하지 않았다는 증거는 무엇인가?
generator/renderer/validation/replay/rollback/PlayMode/legacy regression을 실행하지 않았다는 증거는 무엇인가?
MAP21_11은 왜 아직 시작하지 않았는가?
```

`Responsibility and Added Scripts`는 반드시 표로 작성한다.

```text
| Script or file | Added or changed responsibility | Explicit non-ownership |
```

파일별 책임이 구체적이지 않거나, 사용자가 “그래서 뭘 했는지” 알 수 없으면 FAIL이다.

## 2. 선행조건

Immediate predecessor gate는 strict byte SHA로 확인한다.

```text
MapDesign/MCP/REPORTS/MAP21_09_COMPLETE_FORGE_BOSS_AND_OPTIONAL_REGIONS_RESULT.md exists
MAP21_09 Result STATUS: PASS
MAP21_09 Result SHA-256:
d89be7ffda394341c96eb59196d01c1ec10802c62b82a4bbbc73223e682bf44c

MapDesign/MCP/TASKS/MAP21_09_COMPLETE_FORGE_BOSS_AND_OPTIONAL_REGIONS.md exists
MAP21_09 installed Task SHA-256:
597bae41ef9bc8a1d1080a340186f3d10b0306b9d90aac184bc6657877e44b22

MAP21_10 handoff digest:
ce5e4ebc5e13ad55671a8046ad7641218efbedf9c6b1dff8df9f4cd20110de04

MAP21_11_LOCK_QA_SEEDS_AND_RUN_COMPLETION_PLAYTESTS: LOCKED
```

Historical source Result verification policy:

For MAP21_01~09 source Result files:

```text
require file exists
require TASK line matches the expected task id
require STATUS: PASS
compute and record observed file SHA-256
do not fail only because older historical Result byte SHA differs from an authoring note
```

Expected semantic source anchors:

```text
MAP21_01 biome profile digest:
631000d63f0f2de9c829e662dbb9ab05f3ad4ad5ada542307058515569b3a735

MAP21_02 MicroPattern digest:
cc91a4555da30aaaca6d0bb7a1821d397f59300d4cfeec94587c70db59e1415f

MAP21_03 Crater/Root signature digest:
041b104ebc5755a7ad4b1b67f4726dc88ad77ec103b1b86d70c1453b25826831

MAP21_04 all-biome cluster digest:
d30baf04fcd6eceeabd7b6f467b24ce11ce7c5f44833887bf7b0457e6fbdda72

MAP21_05 Activity/Event digest:
645f95b8c58757179223b8e5b219b60b5a732cb898a69f99594178c76f166a1e

MAP21_06 boundary digest:
955ad4acb3e2294b3dc7c018c21472bf4ef34bbfc4075685324901179bd652ab

MAP21_07 CoreResource digest:
0e03c7cb4780b078822297e9877d345480413a2b3b8d9ed0c3d1d455645a4a93

MAP21_08 Village digest:
9308092afbee03a8c08d5a5fbc2b4eb4a02fc747e5bbc6bd9afe7ce5ec018a73

MAP21_09 Landmark digest:
cca0bf5765f2f80c699c658e81d72d73bf5ce5d6b8bd5276db328cb7e1888469
```

If any required source file is missing, TASK/STATUS is wrong, semantic anchor is absent, or MAP21_09 strict gate fails:

```text
STATUS: BLOCKED
reason: precondition mismatch
created/changed production code files: 0
MAP21_11 started: NO
STOP
```

## 3. 허용 범위

허용 파일:

```text
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/MoonPalaceTuningProfile.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/MoonPalace/MoonPalaceTuningPublisher.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/MoonPalace/MoonPalaceTuningProfileTests.cs
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_10/**
MapDesign/MCP/GENERATED/MAP21_10/**
MapDesign/MCP/TASKS/MAP21_10_TUNE_REPETITION_DENSITY_AND_PACING.md
MapDesign/MCP/REPORTS/MAP21_10_TUNE_REPETITION_DENSITY_AND_PACING_RESULT.md
MapDesign/MCP_ARCHIVE/MAP21_10_TUNE_REPETITION_DENSITY_AND_PACING.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

조건부 허용:

```text
Existing MAP21_01~09 generated manifests may be read and hashed only.
Existing MAP21_01~09 Result files may be read to record TASK/STATUS/SHA and source semantic anchors.
Authoring CSV writes are allowed only under Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_10.
Generated JSON writes are allowed only under MapDesign/MCP/GENERATED/MAP21_10.
```

금지:

```text
MAP21_01~09 source C#, test, CSV, JSON, generated artifact rewrite
MAP21_11 start
world/sector generation or placement
seed lock or seed approval
completion playtest
Tilemap bake or Tilemap mutation
pattern/cluster/sector/world renderer execution or behavior change
validation runner execution
actual replay execution
rollback execution
save/runtime state generation
runtime save/load file write or read
PlayerPrefs write or read
Reward grant
inventory/resource mutation
Boss AI/combat/physics execution
Merchant shop transaction
Maru runtime search or attention execution
door collider, lock, open-close, animation, navigation, or path blocking
Collider generation
Scene / Prefab / ProjectSettings / Packages changes
sprite/audio/background asset import
Addressables group/build change
runtime GameObject instantiate/enable/disable/destroy during tooling actions
CSV authoring edit outside MAP21_10 authoring folder
auto-fix / auto-repair of upstream source
external process launch
manual gameplay test
broad refactor or optimization rewrite
```

회귀 금지:

```text
MAP13 category rerun
MAP18 category rerun
MAP19 category rerun
MAP20 category rerun
MAP21_01~09 category rerun
legacy 19347 regression
prior task category selection
PlayMode selection
unfiltered test run
full regression run
```

## 4. Density Window 계약

Required global windows:

| Window ID | Scope | Min | Target | Max | Meaning |
|---|---|---:|---:|---:|---|
| `DENSITY_QUIET_RATIO` | sector/window | 0.50 | 0.55 | 0.60 | calm or low-pressure space budget |
| `DENSITY_CLUSTER_RATIO` | sector/window | 0.25 | 0.30 | 0.35 | active terrain cluster budget |
| `DENSITY_ACTIVITY_RATIO` | world/window | 0.06 | 0.09 | 0.12 | strong Activity opportunity frequency |
| `DENSITY_OVERLAY_RATIO` | world/window | 0.03 | 0.05 | 0.08 | non-empty EventOverlay frequency |

Rules:

```text
Exactly four global density windows must be published.
Every window must have 0 <= min <= target <= max <= 1.
Quiet target must remain 0.50..0.60.
Cluster target must remain 0.25..0.35.
Activity target must remain 0.06..0.12.
Overlay target must remain 0.03..0.08.
SpecialRegion reserved cells are measured separately and must not be used to fake Quiet/Cluster ratio.
Boundary cells are measured separately and must not be counted as Activity.
The profile may define metrics and targets, but must not generate or approve seed output.
```

## 5. Biome and Pacing Role 계약

Required biome rows:

| Biome | Quiet Target | Cluster Target | Activity Target | Overlay Target | Pacing note |
|---|---:|---:|---:|---:|---|
| `MoonCrater` | 0.51 | 0.34 | 0.10 | 0.06 | more active impact terrain |
| `CassiaRoot` | 0.56 | 0.30 | 0.08 | 0.05 | calmer recovery space |
| `AbandonedMill` | 0.52 | 0.33 | 0.11 | 0.07 | mechanical activity pressure |
| `MoonDough` | 0.58 | 0.27 | 0.07 | 0.04 | soft quiet-heavy traversal |

Required pacing role rows:

```text
StartBuffer
MainRoute
BranchRoute
RecoveryRoute
QuietBuffer
SpecialApproach
BoundarySeam
```

Rules:

```text
Exactly 4 biome tuning rows must be published.
Exactly 7 pacing role rows must be published.
Every biome and role target must stay inside the global window for that layer.
Role rows may bias target ratios, but must not change MAP14/15 solver behavior in this task.
No generated world coordinate or seed-specific statistic may be stored.
```

## 6. Repetition Policy 계약

Required repetition rule rows:

| Rule ID | Subject | Minimum separation |
|---|---|---:|
| `REPEAT_PATTERN_EXACT_ID` | same MicroPattern ID | 3 sector placements |
| `REPEAT_PATTERN_MIRROR_FAMILY` | mirrored silhouette family | 2 sector placements |
| `REPEAT_CLUSTER_EXACT_ID` | same TerrainCluster ID | 6 sector placements |
| `REPEAT_CLUSTER_STRUCTURAL_SIGNATURE` | same cluster structural signature | 4 sector placements |
| `REPEAT_CLUSTER_SILHOUETTE_SIGNATURE` | same cluster silhouette signature | 3 sector placements |
| `REPEAT_ACTIVITY_EXACT_ID` | same Activity ID | 8 sector placements |
| `REPEAT_EVENT_NON_EMPTY_ID` | same non-empty EventOverlay ID | 6 sector placements |
| `REPEAT_BOUNDARY_CANDIDATE_ID` | same boundary candidate ID within same pair/direction | 4 boundary placements |

Rules:

```text
Exactly 8 repetition rules must be published.
Every rule must identify whether it is enforced per sector, per biome, per pair, or per world window.
Structural signature and silhouette signature must be different fields.
Material/color/audio-only changes must not count as structural uniqueness.
Mirror variants may reduce exact-pattern repeats but still count under mirror-family limits.
SpecialRegion landmark/Village/Core/Forge/Boss uniqueness is source-owned and must be referenced, not rewritten.
```

## 7. Required Authoring Files

Create exactly these MAP21_10 authoring CSVs unless a narrower equivalent is strongly justified in the Result.

```text
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_10/moonpalace_density_windows.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_10/moonpalace_biome_tuning_targets.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_10/moonpalace_pacing_role_targets.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_10/moonpalace_repetition_distance_rules.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_10/moonpalace_source_inventory_snapshot.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_10/moonpalace_tuning_handoff.csv
```

Authoring rules:

```text
CSV must use UTF-8 without BOM, LF, one final LF, stable header order, stable row order.
Rows are tuning policy rows only.
No generated world coordinates, seed selections, playtest results, or runtime state may be stored.
No file outside MAP21_10 authoring may be written.
```

## 8. Required Generated Artifacts

Create exactly these generated JSON artifacts.

```text
MapDesign/MCP/GENERATED/MAP21_10/moonpalace_tuning_profile_manifest.json
MapDesign/MCP/GENERATED/MAP21_10/moonpalace_density_pacing_manifest.json
MapDesign/MCP/GENERATED/MAP21_10/moonpalace_repetition_policy_manifest.json
MapDesign/MCP/GENERATED/MAP21_10/moonpalace_tuning_digest_manifest.json
```

Generated artifact rules:

```text
JSON must be deterministic and canonical enough to hash repeatedly.
created_utc may exist only if excluded from canonical digest.
The digest manifest must include observed MAP21_01~09 source Result SHAs, all MAP21_01~09 semantic source digests, every MAP21_10 CSV digest, every MAP21_10 JSON digest, and MAP21_11 handoff digest.
Generated artifacts are static tuning manifests, not generated worlds, seed approvals, playtest reports, or runtime save files.
```

## 9. Required Focused Tests

Add exactly one focused test file:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/MoonPalace/MoonPalaceTuningProfileTests.cs
```

Required test category:

```text
MAP21_10
```

Required focused test cases:

```text
MoonPalaceTuningPublishesExactGlobalDensityWindows
MoonPalaceTuningPublishesFourBiomeTargetsInsideGlobalWindows
MoonPalaceTuningPublishesSevenPacingRolesWithoutChangingSolverBehavior
MoonPalaceTuningReferencesSourceInventoryWithoutRegeneratingContent
MoonPalaceTuningKeepsSpecialRegionsAndBoundariesOutOfFalseDensityCounts
MoonPalaceTuningPublishesEightRepetitionDistanceRules
MoonPalaceTuningSeparatesStructuralSignatureFromSilhouetteAndMaterialOnlyChanges
MoonPalaceTuningActivityAndOverlayWindowsRemainMarkerFrequencyOnly
MoonPalaceTuningPublisherReadsMap21SourcesWithoutRewriting
MoonPalaceTuningPublisherWritesOnlyMap21_10AuthoringAndGeneratedRoots
MoonPalaceTuningDigestsAreDeterministicReverseOrderRepeatAndCultureStable
MoonPalaceTuningRejectsBadWindowsDuplicateRolesMissingSourceAndRuntimeSideEffects
MoonPalaceTuningDoesNotRunGenerationRendererValidationReplayRollbackPlayModeOrLegacyRegression
MoonPalaceTuningPublishesMap21_11HandoffOnlyAfterFocusedPass
```

Only run:

```text
EditMode category MAP21_10
```

Expected:

```text
Discovered: 14
Executed: 14
Passed: 14
Failed: 0
Skipped: 0
Inconclusive: 0
```

Do not run MAP13/MAP18/MAP19/MAP20/MAP21_01~09 categories, PlayMode, or any previous task category.

## 10. Result Required Evidence

The Result must include these sections.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
## Density Window Summary
## Biome and Pacing Role Summary
## Repetition Policy Summary
## Source Inventory and Non-Regeneration Summary
## Static Safety and Non-Execution Summary
## Snapshot and Digest Summary
## Focused Validation Summary
## No Legacy Regression Boundary Notes
## Final Status Evidence
```

Minimum required evidence:

```text
density window records: 4
Quiet min/target/max: 0.50 / 0.55 / 0.60
Cluster min/target/max: 0.25 / 0.30 / 0.35
Activity min/target/max: 0.06 / 0.09 / 0.12
Overlay min/target/max: 0.03 / 0.05 / 0.08
biome tuning rows: 4
pacing role rows: 7
all biome/role targets inside global windows: YES
source inventory rows: 9
source pattern records referenced: 24
source all-biome cluster records referenced: 48
source Activity/Event profile records referenced: 7 / 5
source boundary candidates/projections referenced: 48 / 96
source CoreResource/Village/Landmark records referenced: 3 / 3 / 4
repetition rule records: 8
pattern exact/mirror separation: 3 / 2
cluster exact/structural/silhouette separation: 6 / 4 / 3
activity/event separation: 8 / 6
boundary candidate separation: 4
material-only structural uniqueness count: 0
generated seed count: 0
production seed approval count: 0
completion playtest count: 0
MAP21_01~09 source modifications: 0
generated world count: 0
Tilemap writes: 0
runtime GameObject spawns: 0
save file writes/reads: 0 / 0
Scene/Prefab changes: 0
```

## 11. Focused Validation Boundaries

Validation must stay focused.

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
FULL REGRESSION RUNS: 0
MAP13 CATEGORY RERUNS: 0
MAP18 CATEGORY RERUNS: 0
MAP19 CATEGORY RERUNS: 0
MAP20 CATEGORY RERUNS: 0
MAP21_01 CATEGORY RERUNS: 0
MAP21_02 CATEGORY RERUNS: 0
MAP21_03 CATEGORY RERUNS: 0
MAP21_04 CATEGORY RERUNS: 0
MAP21_05 CATEGORY RERUNS: 0
MAP21_06 CATEGORY RERUNS: 0
MAP21_07 CATEGORY RERUNS: 0
MAP21_08 CATEGORY RERUNS: 0
MAP21_09 CATEGORY RERUNS: 0
VALIDATION RUNNER EXECUTIONS: 0
REPLAY EXECUTIONS: 0
GENERATOR EXECUTIONS: 0
RENDERER EXECUTIONS: 0
ROLLBACK EXECUTIONS: 0
SEED LOCK RUNS: 0
COMPLETION PLAYTEST RUNS: 0
CSV AUTHORING WRITES OUTSIDE MAP21_10 AUTHORING FOLDER: 0
RUNTIME OBJECT SPAWNS: 0
SCENE PREFAB CHANGES: 0
SAVE FILE WRITES/READS: 0 / 0
INVENTORY MUTATIONS: 0
REWARD GRANTS: 0
BOSS AI COMBAT PHYSICS EXECUTIONS: 0 / 0 / 0
SHOP TRANSACTIONS: 0
MARU RUNTIME SEARCH EXECUTIONS: 0
DOOR COLLISION OR LOCK WRITES: 0
```

If a real compile or focused test failure occurs, only the MAP21_10 focused category may be rerun after a local fix. Do not broaden test selection unless the Result explicitly reports a real regression trigger and stops for review.

## 12. Finalize and Commit Rules

Finalize only if all MAP21_10 checks pass.

Status expectations before finalize:

```text
MAP21_10_TUNE_REPETITION_DENSITY_AND_PACING: CURRENT
MAP21_11_LOCK_QA_SEEDS_AND_RUN_COMPLETION_PLAYTESTS: LOCKED
```

Status expectations after finalize:

```text
MAP21_10_TUNE_REPETITION_DENSITY_AND_PACING: COMPLETE
Current Task: NONE
MAP21_11_LOCK_QA_SEEDS_AND_RUN_COMPLETION_PLAYTESTS: LOCKED
```

The Result must state whether Status Finalize and atomic commit were actually performed. If the local protocol intentionally writes Result before finalize, say so explicitly.

Atomic commit scope:

```text
MAP21_10 task file/archive
MAP21_10 Result
MAP21_10 authoring CSV/meta files
MAP21_10 generated JSON files
MoonPalaceTuningProfile.cs and .meta if new
MoonPalaceTuningPublisher.cs and .meta if new
MoonPalaceTuningProfileTests.cs and .meta if new
06_IMPLEMENTATION_STATUS.md finalize change
```

Commit subject:

```text
MAP21_10 tune repetition density and pacing
```

Do not stage unrelated files. Do not push.

## 13. Stop Rule

After Result and commit:

```text
STOP
Do not create MAP21_11 files.
Do not run MAP21_11.
Do not run seed lock, completion playtest, world generation, renderer, validation runner, replay, rollback, PlayMode, legacy regression, or full regression.
```
