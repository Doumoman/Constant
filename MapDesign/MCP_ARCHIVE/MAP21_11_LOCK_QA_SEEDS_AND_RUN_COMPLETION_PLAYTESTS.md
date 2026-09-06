```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP21_11_LOCK_QA_SEEDS_AND_RUN_COMPLETION_PLAYTESTS
  task_file: TASKS/MAP21_11_LOCK_QA_SEEDS_AND_RUN_COMPLETION_PLAYTESTS.md
  requires_current_task: NONE
  requires_completed_task: MAP21_10_TUNE_REPETITION_DENSITY_AND_PACING
  requires_result:
    path: REPORTS/MAP21_10_TUNE_REPETITION_DENSITY_AND_PACING_RESULT.md
    status: PASS
    sha256: 348581da96ace8e893141731e22098691931f7f9c15836d97a703f4e5bcb914b
  requires_installed_task:
    path: TASKS/MAP21_10_TUNE_REPETITION_DENSITY_AND_PACING.md
    sha256: a957d4b93022fa4aa333c504f0b0c5008973ff56a98007a1498fc517c6d6e1fb
  requires_handoff:
    name: MAP21_11 handoff digest
    sha256: cc85dc9b3cee0d47da517f6eb77546e345c341db2bd5409510804305d01c1646
  sets_current_task: MAP21_11_LOCK_QA_SEEDS_AND_RUN_COMPLETION_PLAYTESTS
```

# MAP21_11 - Lock QA Seeds and Run Completion Playtests

```text
TASK: MAP21_11_LOCK_QA_SEEDS_AND_RUN_COMPLETION_PLAYTESTS
PHASE: MAP21 - MoonPalace Vertical Slice
STATUS: CURRENT
NEXT: MAP21_12_VERTICAL_SLICE_RELEASE_AUDIT
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP21_10에서 고정한 tuning contract를 기준으로 MoonPalace vertical slice 대표 QA seed 30개를 lock하고, 각 seed에 대해 자동 completion playtest telemetry를 수집한다.

이번 Task는 seed set, per-seed content hash, completion scenario, telemetry summary, failure bundle index, MAP21_12 handoff만 소유한다.

이 작업은 legacy regression이 아니다. 실행은 MAP21_11 전용 seed/playtest harness로 제한한다.

| Area | This task owns | This task must not do |
|---|---|---|
| QA seed lock | exact 30 deterministic seed IDs and seed values | random manual seed picking |
| Completion playtest | 30 automated completion scenarios and telemetry rows | manual gameplay or PlayMode scene play |
| Metric audit | distance, revisit, seam, death, density, repetition measurement | MAP21_10 tuning rewrite |
| Failure handling | first failing seed bundle index if any failure occurs | auto-repair, source regeneration, broad regression |
| QA handoff | MAP21_12 release-audit handoff digest | MAP21_12 start or final release approval |

Core principle:

```text
This task measures the locked MoonPalace slice with 30 focused automated seeds.
It does not rerun legacy MAP validation.
It does not rewrite MAP21_01~10 source content.
It does not approve final release; MAP21_12 owns final vertical slice release audit.
```

## 1. 사용자 보고 의무

Result 첫 부분에는 반드시 아래 두 섹션을 둔다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

`User-Facing Implementation Report`에는 아래 질문에 답한다.

```text
이번 작업이 QA seed 30개를 어떤 방식으로 고정했는가?
각 seed의 content hash와 completion scenario 결과는 무엇인가?
completion route가 Start -> CoreResource 3 -> Forge -> Boss completion을 어떻게 통과했는가?
Village/Merchant/Maru optional content가 completion blocker가 아니라는 증거는 무엇인가?
distance, revisit, seam, death telemetry의 전체 요약과 seed별 outlier는 무엇인가?
MAP21_10 density/repetition tuning 기준을 어떻게 측정했고, 통과/실패 여부는 무엇인가?
실패 seed가 있다면 어떤 failure bundle을 만들었고 왜 자동 수정하지 않았는가?
이번 Task에서 추가한 script/CSV/JSON과 각 책임은 무엇인가?
legacy 19347, prior category, PlayMode, unfiltered/full regression을 하지 않았다는 증거는 무엇인가?
MAP21_12는 왜 아직 시작하지 않았는가?
```

`Responsibility and Added Scripts`는 반드시 표로 작성한다.

```text
| Script or file | Added or changed responsibility | Explicit non-ownership |
```

파일별 책임이 구체적이지 않거나, 사용자가 “그래서 뭘 했는지” 알 수 없으면 FAIL이다.

## 2. 선행조건

Immediate predecessor gate는 strict byte SHA로 확인한다.

```text
MapDesign/MCP/REPORTS/MAP21_10_TUNE_REPETITION_DENSITY_AND_PACING_RESULT.md exists
MAP21_10 Result STATUS: PASS
MAP21_10 Result SHA-256:
348581da96ace8e893141731e22098691931f7f9c15836d97a703f4e5bcb914b

MapDesign/MCP/TASKS/MAP21_10_TUNE_REPETITION_DENSITY_AND_PACING.md exists
MAP21_10 installed Task SHA-256:
a957d4b93022fa4aa333c504f0b0c5008973ff56a98007a1498fc517c6d6e1fb

MAP21_11 handoff digest:
cc85dc9b3cee0d47da517f6eb77546e345c341db2bd5409510804305d01c1646

MAP21_12_VERTICAL_SLICE_RELEASE_AUDIT: LOCKED
```

Historical source Result verification policy:

For MAP19_08, MAP19_09, MAP20_06, and MAP21_01~10 source Result files:

```text
require file exists
require TASK line matches the expected task id
require STATUS: PASS
compute and record observed file SHA-256
do not fail only because older historical Result byte SHA differs from an authoring note
```

Expected semantic source anchors:

```text
MAP19_08 failure bundle schema digest:
5d96dbb4e42b3a39f20ee6d48f8acf1aed13acd23c3743a7b4683bdd314cd483

MAP19_09 MAP19 exit digest:
0f5bac81722f727dd02772c76323c9fa69ce91ad3d1a3722cef2aa47381ccec2

MAP20_06 MAP20 phase exit digest:
552f7f2d8884811977e001c1159be52c218346f21dcc9f96f7df06d169eb7301

MAP21_10 tuning digest:
b3e57217b52222d0665c437649749c8803c45c09a6ac09402a591f210fdedc8d

MAP21_10 confirms:
seed_locked=false
completion_playtest_executed=false
```

If any required source file is missing, TASK/STATUS is wrong, semantic anchor is absent, or MAP21_10 strict gate fails:

```text
STATUS: BLOCKED
reason: precondition mismatch
created/changed production code files: 0
MAP21_12 started: NO
STOP
```

## 3. 허용 범위

허용 파일:

```text
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/MoonPalaceQaSeedSet.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/MoonPalace/MoonPalaceQaSeedPublisher.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/MoonPalace/MoonPalaceQaSeedCompletionTests.cs
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_11/**
MapDesign/MCP/GENERATED/MAP21_11/**
MapDesign/MCP/TASKS/MAP21_11_LOCK_QA_SEEDS_AND_RUN_COMPLETION_PLAYTESTS.md
MapDesign/MCP/REPORTS/MAP21_11_LOCK_QA_SEEDS_AND_RUN_COMPLETION_PLAYTESTS_RESULT.md
MapDesign/MCP_ARCHIVE/MAP21_11_LOCK_QA_SEEDS_AND_RUN_COMPLETION_PLAYTESTS.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

조건부 허용:

```text
Existing MAP19_08 failure bundle schema may be referenced read-only.
Existing MAP19_09 validation chain summary may be referenced read-only.
Existing MAP20_06 tooling exit audit may be referenced read-only.
Existing MAP21_01~10 generated manifests may be read and hashed only.
Task-owned MAP21_11 automated seed/playtest execution may write only under MapDesign/MCP/GENERATED/MAP21_11.
Authoring CSV writes are allowed only under Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_11.
```

금지:

```text
MAP19/MAP20/MAP21_01~10 source C#, test, CSV, JSON, generated artifact rewrite
MAP21_12 start
MAP21_10 tuning rewrite
source content regeneration
pool expansion or replacement
new biome/profile/tile shell/pattern/cluster/activity/event/boundary/special content
manual gameplay test
Unity PlayMode test selection
legacy 19347 regression
prior task category selection
unfiltered test run
full regression run
external process launch
scene play, Scene changes, Prefab changes, ProjectSettings changes, Packages changes
Tilemap bake or Tilemap mutation outside task-owned generated telemetry
runtime GameObject instantiate/enable/disable/destroy during tooling actions
save/runtime state generation outside in-memory telemetry
runtime save/load file write or read
PlayerPrefs write or read
Reward grant
inventory/resource mutation
Boss AI/combat/physics execution
Merchant shop transaction
Maru runtime search or attention execution
door collider, lock, open-close, animation, navigation, or path blocking
Collider generation
Addressables group/build change
sprite/audio/background asset import
CSV authoring edit outside MAP21_11 authoring folder
auto-fix / auto-repair after failed seed
broad refactor or optimization rewrite
```

MAP21_11-specific allowed execution:

```text
automated QA seed generation count: exactly 30
automated completion simulation/playtest count: exactly 30
MAP21_11 focused metric evaluation count: exactly 30
failure bundle creation count: 0 on PASS, 1..5 on BLOCKED failure
```

These allowed executions are task-owned QA measurements and must not be reported as legacy regression.

## 4. Locked QA Seed 계약

Seed derivation:

```text
seed_derivation_version: MAP21_11_QA_SEED_V1
source_handoff_digest: cc85dc9b3cee0d47da517f6eb77546e345c341db2bd5409510804305d01c1646
derivation_material: MAP21_11_QA_SEED_V1|{source_handoff_digest}|{zero_based_index_00_to_29}
seed_value: first 8 bytes of SHA-256 derivation material, big-endian, masked to signed-positive 31-bit integer
```

Required exact seed list:

| Seed ID | Seed value |
|---|---:|
| `MP_QA_01` | 1924737067 |
| `MP_QA_02` | 1697017134 |
| `MP_QA_03` | 684258236 |
| `MP_QA_04` | 1744116978 |
| `MP_QA_05` | 894083500 |
| `MP_QA_06` | 1937634980 |
| `MP_QA_07` | 1080761698 |
| `MP_QA_08` | 1987855856 |
| `MP_QA_09` | 1513827622 |
| `MP_QA_10` | 1395430098 |
| `MP_QA_11` | 380450865 |
| `MP_QA_12` | 1757128282 |
| `MP_QA_13` | 1913123366 |
| `MP_QA_14` | 1660925428 |
| `MP_QA_15` | 2082689511 |
| `MP_QA_16` | 2123183810 |
| `MP_QA_17` | 2076488598 |
| `MP_QA_18` | 222861178 |
| `MP_QA_19` | 1013514900 |
| `MP_QA_20` | 964092742 |
| `MP_QA_21` | 1839314896 |
| `MP_QA_22` | 434218138 |
| `MP_QA_23` | 621327962 |
| `MP_QA_24` | 1151743105 |
| `MP_QA_25` | 30728136 |
| `MP_QA_26` | 636174302 |
| `MP_QA_27` | 1200663868 |
| `MP_QA_28` | 512848102 |
| `MP_QA_29` | 1420300145 |
| `MP_QA_30` | 2145260252 |

Rules:

```text
Exactly 30 QA seed records must be published.
Seed ID order must be stable MP_QA_01 through MP_QA_30.
All seed values must be unique.
Each seed must produce one content hash.
Content hashes must be lowercase 64-hex SHA-256.
Seed lock cannot be changed by local run order or culture.
No manual seed substitution is allowed.
```

## 5. Completion Scenario 계약

Each seed must run one automated completion scenario.

Required completion route checkpoints:

```text
Start
MoonCore required reward reachable
CassiaSap required reward reachable
StarNuruk required reward reachable
Forge reachable
Forge MoonSeal marker reachable
Boss gate reachable
Boss GateAccepted state reachable
Boss Defeated state reachable
Return/Completion marker reachable
```

Optional content rules:

```text
Village may be present but must not be required for completion.
Merchant may be present but must not be required for completion.
Maru may be present but must not be required for completion.
Optional content may improve telemetry but must not block route completion.
```

Required pass criteria:

```text
completion scenario records: 30
completion executed count: 30
completion pass count: 30
completion fail count: 0
missing mandatory checkpoint count: 0
softlock count: 0
death count: 0
unrecoverable failure count: 0
content hash mismatch count: 0
replay mismatch count: 0
optional-content blocker count: 0
```

If any seed fails:

```text
STATUS: BLOCKED
write first failing seed bundle using MAP19_08-compatible schema under MapDesign/MCP/GENERATED/MAP21_11/failures
failure bundle count: 1..5
do not auto-repair
do not replace seed
do not broaden validation
MAP21_12 started: NO
STOP
```

## 6. Telemetry 계약

Per seed telemetry fields:

```text
seed_id
seed_value
content_hash
completion_pass
completion_route_digest
estimated_completion_time_seconds
main_route_distance_tiles
branch_route_distance_tiles
total_route_distance_tiles
revisit_count
max_revisit_same_sector
seam_crossing_count
bad_seam_count
death_count
softlock_count
mandatory_checkpoint_count
optional_content_blocker_count
quiet_ratio
cluster_ratio
activity_ratio
overlay_ratio
pattern_repeat_violation_count
cluster_repeat_violation_count
activity_repeat_violation_count
event_repeat_violation_count
boundary_repeat_violation_count
```

Required aggregate thresholds:

```text
estimated_completion_time_seconds min/median/max must be reported
total_route_distance_tiles min/median/max must be reported
revisit_count min/median/max must be reported
seam_crossing_count min/median/max must be reported
death_count total: 0
softlock_count total: 0
bad_seam_count total: 0
optional_content_blocker_count total: 0
density window violations total: 0
repetition rule violations total: 0
completion route digest records: 30
unique content hashes: 30
```

Density windows from MAP21_10 must be measured:

```text
Quiet ratio inside 0.50..0.60
Cluster ratio inside 0.25..0.35
Activity ratio inside 0.06..0.12
Overlay ratio inside 0.03..0.08
```

Repetition rules from MAP21_10 must be measured:

```text
pattern exact ID separation >= 3
pattern mirror family separation >= 2
cluster exact ID separation >= 6
cluster structural signature separation >= 4
cluster silhouette signature separation >= 3
activity exact ID separation >= 8
non-empty event overlay ID separation >= 6
boundary candidate ID separation >= 4 per pair/direction window
```

## 7. Required Authoring Files

Create exactly these MAP21_11 authoring CSVs unless a narrower equivalent is strongly justified in the Result.

```text
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_11/moonpalace_qa_seed_manifest.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_11/moonpalace_completion_scenarios.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_11/moonpalace_completion_telemetry_targets.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_11/moonpalace_failure_bundle_index.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_11/moonpalace_release_audit_handoff.csv
```

Authoring rules:

```text
CSV must use UTF-8 without BOM, LF, one final LF, stable header order, stable row order.
Rows are QA seed and telemetry policy rows only.
No runtime save state may be stored.
No file outside MAP21_11 authoring may be written.
```

## 8. Required Generated Artifacts

Create exactly these generated JSON artifacts.

```text
MapDesign/MCP/GENERATED/MAP21_11/moonpalace_qa_seed_lock_manifest.json
MapDesign/MCP/GENERATED/MAP21_11/moonpalace_completion_playtest_manifest.json
MapDesign/MCP/GENERATED/MAP21_11/moonpalace_completion_telemetry_summary.json
MapDesign/MCP/GENERATED/MAP21_11/moonpalace_density_repetition_measurement.json
MapDesign/MCP/GENERATED/MAP21_11/moonpalace_qa_failure_bundle_index.json
MapDesign/MCP/GENERATED/MAP21_11/moonpalace_qa_digest_manifest.json
```

Failure-only generated artifacts:

```text
MapDesign/MCP/GENERATED/MAP21_11/failures/**
```

Generated artifact rules:

```text
JSON must be deterministic and canonical enough to hash repeatedly.
created_utc may exist only if excluded from canonical digest.
The digest manifest must include observed MAP19/MAP20/MAP21 source Result SHAs, MAP19 failure bundle schema digest, MAP19 exit digest, MAP20 phase exit digest, MAP21_10 tuning digest, every MAP21_11 CSV digest, every MAP21_11 JSON digest, per-seed content hashes, and MAP21_12 handoff digest.
Generated artifacts are QA measurement outputs, not runtime save files or final release approval.
```

## 9. Required Focused Tests

Add exactly one focused test file:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/MoonPalace/MoonPalaceQaSeedCompletionTests.cs
```

Required test category:

```text
MAP21_11
```

Required focused test cases:

```text
MoonPalaceQaSeedSetPublishesExactThirtyDerivedSeeds
MoonPalaceQaSeedContentHashesAreStableUniqueAndLowerHex
MoonPalaceCompletionScenariosReachAllMandatoryCheckpointsForThirtySeeds
MoonPalaceCompletionScenariosDoNotRequireVillageMerchantOrMaru
MoonPalaceCompletionTelemetryReportsTimeDistanceRevisitSeamAndDeathMetrics
MoonPalaceCompletionTelemetryHasNoDeathSoftlockBadSeamOrOptionalBlocker
MoonPalaceDensityMeasurementsStayInsideMap21_10Windows
MoonPalaceRepetitionMeasurementsSatisfyMap21_10DistanceRules
MoonPalaceFailureBundleUsesMap19SchemaAndPublishesOnlyOnFailure
MoonPalaceQaPublisherReadsSourcesWithoutRewriting
MoonPalaceQaPublisherWritesOnlyMap21_11AuthoringAndGeneratedRoots
MoonPalaceQaDigestsAreDeterministicReverseOrderRepeatAndCultureStable
MoonPalaceQaRejectsSeedSubstitutionHashMismatchMissingCheckpointAndRuntimeSideEffects
MoonPalaceQaDoesNotRunLegacyRegressionPriorCategoriesPlayModeUnfilteredOrFullRegression
MoonPalaceQaPublishesMap21_12HandoffOnlyAfterThirtyFocusedPasses
```

Only run:

```text
EditMode category MAP21_11
```

Expected:

```text
Discovered: 15
Executed: 15
Passed: 15
Failed: 0
Skipped: 0
Inconclusive: 0
```

Do not run MAP13/MAP18/MAP19/MAP20/MAP21_01~10 categories, PlayMode, or any previous task category.

## 10. Result Required Evidence

The Result must include these sections.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
## QA Seed Lock Summary
## Completion Scenario Summary
## Telemetry Summary
## Density and Repetition Measurement Summary
## Failure Bundle Summary
## Static Safety and Non-Execution Summary
## Snapshot and Digest Summary
## Focused Validation Summary
## No Legacy Regression Boundary Notes
## Final Status Evidence
```

Minimum required evidence:

```text
QA seed records: 30
exact derived seed IDs: MP_QA_01..MP_QA_30
unique seed values: 30
content hash records: 30
unique content hashes: 30
completion scenario records: 30
completion executed count: 30
completion pass count: 30
completion fail count: 0
mandatory checkpoint count per seed: 10 / 10
missing mandatory checkpoint count: 0
Village required for completion count: 0
Merchant required for completion count: 0
Maru required for completion count: 0
death count total: 0
softlock count total: 0
unrecoverable failure count: 0
bad seam count total: 0
optional-content blocker count: 0
content hash mismatch count: 0
replay mismatch count: 0
estimated completion time min/median/max: reported
total route distance min/median/max: reported
revisit count min/median/max: reported
seam crossing count min/median/max: reported
density window violations: 0
repetition rule violations: 0
failure bundle count: 0 on PASS
generated seed count: 30
production seed approval count: 30 locked QA seeds only
completion playtest count: 30
manual gameplay test count: 0
PlayMode selection count: 0
MAP19/MAP20/MAP21_01~10 source modifications: 0
MAP21_10 tuning rewrites: 0
generated world writes outside MAP21_11: 0
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
MAP21_10 CATEGORY RERUNS: 0
MAP21_11 FOCUSED QA SEED RUNS: 30
MAP21_11 FOCUSED COMPLETION PLAYTEST RUNS: 30
VALIDATION RUNNER EXECUTIONS OUTSIDE MAP21_11: 0
REPLAY EXECUTIONS OUTSIDE MAP21_11: 0
GENERATOR EXECUTIONS OUTSIDE MAP21_11: 0
RENDERER EXECUTIONS: 0
ROLLBACK EXECUTIONS: 0
SEED LOCK RUNS OUTSIDE MAP21_11: 0
CSV AUTHORING WRITES OUTSIDE MAP21_11 AUTHORING FOLDER: 0
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

If a real MAP21_11 compile or focused test failure occurs, only the MAP21_11 focused category may be rerun after a local fix. Do not broaden test selection unless the Result explicitly reports a real regression trigger and stops for review.

If a seed completion failure occurs, do not fix and rerun automatically. Publish the failure bundle and return `STATUS: BLOCKED`.

## 12. Finalize and Commit Rules

Finalize only if all MAP21_11 checks pass.

Status expectations before finalize:

```text
MAP21_11_LOCK_QA_SEEDS_AND_RUN_COMPLETION_PLAYTESTS: CURRENT
MAP21_12_VERTICAL_SLICE_RELEASE_AUDIT: LOCKED
```

Status expectations after finalize:

```text
MAP21_11_LOCK_QA_SEEDS_AND_RUN_COMPLETION_PLAYTESTS: COMPLETE
Current Task: NONE
MAP21_12_VERTICAL_SLICE_RELEASE_AUDIT: LOCKED
```

The Result must state whether Status Finalize and atomic commit were actually performed. If the local protocol intentionally writes Result before finalize, say so explicitly.

Atomic commit scope:

```text
MAP21_11 task file/archive
MAP21_11 Result
MAP21_11 authoring CSV/meta files
MAP21_11 generated JSON files
MAP21_11 failure bundle files if any
MoonPalaceQaSeedSet.cs and .meta if new
MoonPalaceQaSeedPublisher.cs and .meta if new
MoonPalaceQaSeedCompletionTests.cs and .meta if new
06_IMPLEMENTATION_STATUS.md finalize change
```

Commit subject:

```text
MAP21_11 lock qa seeds and completion playtests
```

Do not stage unrelated files. Do not push.

## 13. Stop Rule

After Result and commit:

```text
STOP
Do not create MAP21_12 files.
Do not run MAP21_12.
Do not run legacy regression, prior categories, PlayMode, unfiltered tests, or full regression.
```
