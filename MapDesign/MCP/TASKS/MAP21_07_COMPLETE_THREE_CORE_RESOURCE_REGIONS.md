```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP21_07_COMPLETE_THREE_CORE_RESOURCE_REGIONS
  task_file: TASKS/MAP21_07_COMPLETE_THREE_CORE_RESOURCE_REGIONS.md
  requires_current_task: NONE
  requires_completed_task: MAP21_06_EXPAND_ALL_SIX_BOUNDARY_POOLS
  requires_result:
    path: REPORTS/MAP21_06_EXPAND_ALL_SIX_BOUNDARY_POOLS_RESULT.md
    status: PASS
    sha256: fba1770c479fb039d922dc8952ed70ec659416c69b93ce8fcf362b2f7c1e1db1
  requires_installed_task:
    path: TASKS/MAP21_06_EXPAND_ALL_SIX_BOUNDARY_POOLS.md
    sha256: 3b79ea1aa6303538c3160da0de5ba81a622f1f81e4ba8263ef8ac2daebe23dab
  requires_handoff:
    name: MAP21_07 handoff digest
    sha256: c7c47d7dfe5928fe7e5f10f382ce2ccc3b9d20f218c1512c3e1ae60bb74e101b
  sets_current_task: MAP21_07_COMPLETE_THREE_CORE_RESOURCE_REGIONS
```

# MAP21_07 - Complete Three Core Resource Regions

```text
TASK: MAP21_07_COMPLETE_THREE_CORE_RESOURCE_REGIONS
PHASE: MAP21 - MoonPalace Vertical Slice
STATUS: CURRENT
NEXT: MAP21_08_COMPLETE_MOONPALACE_VILLAGE
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP13에서 승인된 MoonCore, CassiaSap, StarNuruk 세 CoreResource starter를 MoonPalace vertical slice production data로 완성한다.

이번 Task는 **세 CoreResource region의 production profile, local design chunks, solution graph, reward/persistence proof, static manifest**만 소유한다. 실제 장치 MonoBehaviour, 물리 시뮬레이션, 보상 지급, inventory/save I/O, world placement, renderer, Tilemap, runtime object는 아직 시작하지 않는다.

| Area | This task owns | This task must not do |
|---|---|---|
| CoreResource profiles | MoonCore/CassiaSap/StarNuruk production identity와 biome mapping | Forge/Boss/Village/optional region 작업 |
| Local design canvas | 각 region의 36x16 design canvas와 five active 12x8 chunks | sector/world placement |
| Solution graph | Low/High/Failure/Recovery graph와 witness digest | physics, device, damage, item use 실행 |
| Reward proof | exact-one required reward slot/key와 optional benefit marker | reward grant, inventory mutation, save write |
| Persistence proof | 7 checkpoints per region과 MAP18 export identity compatibility | runtime save file generation |

핵심 원칙:

```text
This task completes the three core resource region data, not gameplay execution.
MAP13 is the source of truth for CoreResource structure and persistence keys.
MAP21_07 writes only MoonPalace production CoreResource authoring and generated manifests.
No generated world, Tilemap, Scene, Prefab, collider, runtime object, save I/O, reward grant, or physics simulation is created.
```

## 1. 사용자 보고 의무

Result 첫 부분에는 반드시 아래 두 섹션을 둔다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

`User-Facing Implementation Report`에는 아래 질문에 답한다.

```text
이번 작업이 MoonCore/CassiaSap/StarNuruk을 production 데이터로 어떻게 완성했는가?
각 region의 biome, design canvas, active chunk, Low/High/Recovery 구조는 무엇인가?
required reward slot/key와 persistence checkpoint는 어떻게 보존되었는가?
MAP13 starter/exit와 MAP18 special export identity를 어떻게 읽기 전용으로 연결했는가?
이번 작업이 장치 물리, 보상 지급, inventory/save, world placement, Tilemap/Scene/Prefab/runtime을 만들지 않았다는 증거는 무엇인가?
이번 Task에서 추가한 script/CSV/JSON과 각 책임은 무엇인가?
legacy regression과 prior-category rerun을 하지 않았다는 증거는 무엇인가?
MAP21_08은 왜 아직 시작하지 않았는가?
```

`Responsibility and Added Scripts`는 반드시 표로 작성한다.

```text
| Script or file | Added or changed responsibility | Explicit non-ownership |
```

파일별 책임이 구체적이지 않거나, 사용자가 “그래서 뭘 했는지” 알 수 없으면 FAIL이다.

## 2. 선행조건

작업 시작 전에 다음을 확인한다.

```text
MapDesign/MCP/REPORTS/MAP21_06_EXPAND_ALL_SIX_BOUNDARY_POOLS_RESULT.md exists
MAP21_06 Result STATUS: PASS
MAP21_06 Result SHA-256:
fba1770c479fb039d922dc8952ed70ec659416c69b93ce8fcf362b2f7c1e1db1

MapDesign/MCP/TASKS/MAP21_06_EXPAND_ALL_SIX_BOUNDARY_POOLS.md exists
MAP21_06 installed Task SHA-256:
3b79ea1aa6303538c3160da0de5ba81a622f1f81e4ba8263ef8ac2daebe23dab

MAP21_07 handoff digest:
c7c47d7dfe5928fe7e5f10f382ce2ccc3b9d20f218c1512c3e1ae60bb74e101b

MAP13 SpecialRegion phase exit Result exists:
MapDesign/MCP/REPORTS/MAP13_09_MAP13_SPECIAL_REGION_EXIT_TESTS_RESULT.md
MAP13_09 Result STATUS: PASS
MAP13_09 Result SHA-256:
637fec406f42bf845be5ae9313a036b3ec49f66467539a3552c1f94ad68bd5e2
MAP13 public audit digest:
a7ab6fd571425c4c8e64d7eecad5dd246a3d9a8a08044801800948fc2fa03e4e

MAP18 special state export Result exists:
MapDesign/MCP/REPORTS/MAP18_06_IMPLEMENT_SPECIAL_STATE_EXPORT_AND_DEBUG_RESULT.md
MAP18_06 Result STATUS: PASS
MAP18_06 Result SHA-256:
ad2b88be043cb7e18289909a7ad44d76c9143a65e5228c11c24f1a60b86831fd
MAP18 special export surface digest:
358ac8cfe78eec502db049f8940ed0c71458179b89bb451680e837b0797b77b5
MAP18 debug snapshot digest:
59efb7fd30df9ec62014cadd04a111b222e7dd13e298789dbab88a661bea22ed

MAP21_04 all-biome cluster manifest digest:
d30baf04fcd6eceeabd7b6f467b24ce11ce7c5f44833887bf7b0457e6fbdda72
MAP21_05 activity/event digest manifest:
645f95b8c58757179223b8e5b219b60b5a732cb898a69f99594178c76f166a1e
MAP21_06 boundary digest manifest:
955ad4acb3e2294b3dc7c018c21472bf4ef34bbfc4075685324901179bd652ab

MAP21_08_COMPLETE_MOONPALACE_VILLAGE: LOCKED
```

불일치 시:

```text
STATUS: BLOCKED
reason: precondition mismatch
created/changed code files: 0
MAP21_08 started: NO
STOP
```

## 3. 허용 범위

허용 파일:

```text
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/MoonPalaceCoreResourceProduction.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/MoonPalace/MoonPalaceCoreResourcePublisher.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/MoonPalace/MoonPalaceCoreResourceProductionTests.cs
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_07/**
MapDesign/MCP/GENERATED/MAP21_07/**
MapDesign/MCP/TASKS/MAP21_07_COMPLETE_THREE_CORE_RESOURCE_REGIONS.md
MapDesign/MCP/REPORTS/MAP21_07_COMPLETE_THREE_CORE_RESOURCE_REGIONS_RESULT.md
MapDesign/MCP_ARCHIVE/MAP21_07_COMPLETE_THREE_CORE_RESOURCE_REGIONS.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

조건부 허용:

```text
Existing MAP13 CoreResource public definitions/compiler/catalog may be referenced read-only.
Existing MAP13_09 preview/audit public output may be referenced read-only.
Existing MAP18_06 special export public types may be referenced read-only for persistence/export identity compatibility.
Existing MAP21_01/03/04/05/06 manifests may be read and hashed only.
Authoring CSV writes are allowed only under Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_07.
Generated JSON writes are allowed only under MapDesign/MCP/GENERATED/MAP21_07.
```

금지:

```text
MAP13 source C#, test, CSV, or generated artifact rewrite
MAP18 source C#, test, CSV, or generated artifact rewrite
MAP21_01 profile/tile shell rewrite
MAP21_02 MicroPattern rewrite
MAP21_03/04 cluster pool rewrite or regeneration
MAP21_05 Activity/Event rewrite or regeneration
MAP21_06 boundary pool rewrite or regeneration
Forge/Boss/Village/Merchant/Maru production work
core resource placement into sector/world
sector/world generation
final 12x8 slice generation
Tilemap bake or Tilemap mutation
pattern renderer execution or behavior change
validation runner execution
actual replay execution
rollback execution
save/runtime state generation
runtime save/load file write or read
PlayerPrefs write or read
Reward grant
inventory/resource mutation
device MonoBehaviour, physics, projectile, damage, valve, boulder, water, gas, bounce, or collision execution
NPC/Enemy/Boss spawn or AI hookup
Collider generation
Scene / Prefab / ProjectSettings / Packages changes
sprite/audio/background asset import
Addressables group/build change
runtime GameObject instantiate/enable/disable/destroy during tooling actions
CSV authoring edit outside MAP21_07 authoring folder
auto-fix / auto-repair
production seed approval
external process launch
manual gameplay test
automatic MAP21_08 start
broad refactor or optimization rewrite
```

회귀 금지:

```text
MAP13 category rerun
MAP18 category rerun
MAP19_09 scale audit rerun
MAP20_01 generator run rerun
MAP20_02 overlay sample regeneration
MAP20_03 detail sample regeneration
MAP20_04 navigation sample regeneration
MAP20_05 replay/export sample regeneration
MAP20_06 exit audit regeneration
MAP21_01 profile tile shell regeneration
MAP21_02 MicroPattern regeneration
MAP21_03 Crater/Root cluster regeneration
MAP21_04 Mill/Dough cluster regeneration
MAP21_05 Activity/Event regeneration
MAP21_06 boundary regeneration
legacy 19347 regression
prior task category selection
PlayMode selection
unfiltered test run
full regression run
```

## 4. CoreResource Production 계약

MAP21_07 must preserve the exact MAP13 CoreResource region IDs and authoritative persistence keys.

Required region inventory:

| Region ID | Resource | Biome | Mechanism | Active chunks | Required reward key |
|---|---|---|---|---:|---|
| `SR_MOON_CORE_SITE_5` | `MoonCore` | `MoonCrater` | `ImpactChain` | 5 | `SR_STATE_MOON_CORE_SITE_5_REWARD_MOON_CORE_REWARD` |
| `SR_CASSIA_SAP_SITE_5` | `CassiaSap` | `CassiaRoot` | `WaterChannel` | 5 | `SR_STATE_CASSIA_SAP_SITE_5_REWARD_CASSIA_SAP_REWARD` |
| `SR_STAR_NURUK_SITE_5` | `StarNuruk` | `MoonDough` | `FermentationPressure` | 5 | `SR_STATE_STAR_NURUK_SITE_5_REWARD_STAR_NURUK_REWARD` |

Required profile fields:

```text
region_id
schema_version
resource_kind
biome_id
mechanism_kind
design_canvas_width
design_canvas_height
active_chunk_count
low_route_id
high_route_id
failure_branch_id
recovery_route_id
required_reward_slot_id
required_reward_persistence_key
optional_benefit_count
persistence_checkpoint_count
source_map13_audit_digest
source_map18_export_digest
chunk_digest
graph_digest
reward_digest
persistence_digest
canonical_digest
```

Rules:

```text
There must be exactly 3 CoreResource profile records.
Every region has a 36x16 local design canvas.
Every region has exactly five active 12x8 design chunks.
Every region has exactly one Low route, one High route, one Failure branch, and one Recovery route.
Every Low route is MandatoryNoTool and must reach the required reward and return.
Every High route is optional mastery content and must converge on the same required reward or existing Low route.
Every Failure branch must recover through a Recovery route that joins the existing Low RecoveryJoin node.
Every region has exactly one required reward definition.
Every region has exactly two optional benefit markers.
Every region has exactly seven persistence checkpoint records.
Legacy short persistence keys must not be accepted.
No region may depend on Village, inventory state, external tool, reward already claimed state, or runtime object execution to remain completable.
```

## 5. Graph and Persistence 계약

Production graph counts must preserve the approved MAP13 starter totals unless the Result explains a strictly narrower equivalent that keeps all required proofs.

| Region | Nodes | Edges | Low edges | High edges | Failure edges | Recovery edges | Persistence checkpoints |
|---|---:|---:|---:|---:|---:|---:|---:|
| `SR_MOON_CORE_SITE_5` | 14 | 16 | 5 | 8 | 1 | 2 | 7 |
| `SR_CASSIA_SAP_SITE_5` | 15 | 17 | 7 | 7 | 1 | 2 | 7 |
| `SR_STAR_NURUK_SITE_5` | 15 | 18 | 8 | 7 | 1 | 2 | 7 |
| **TOTAL** | **44** | **51** | **20** | **22** | **3** | **6** | **21** |

Required node/edge fields:

```text
region_id
node_id_or_edge_id
route_id
route_kind
node_role_or_edge_role
local_x
local_y
order
required
access_class
mechanism_token
dependency_kind
target_reward_slot_or_none
canonical_digest
```

Required persistence checkpoint fields:

```text
region_id
checkpoint_id
checkpoint_kind
required_reward_persistence_key
availability_state
claim_state
duplicate_risk
permanent_loss_risk
canonical_digest
```

Required checkpoint states:

```text
InitialAvailable
InterruptedAvailable
FailedAvailable
RegeneratedAvailable
Claimed
RevisitedClaimed
RecoveryJoinAvailable
```

The task may name these with existing project enum values if they already exist, but the seven semantic states must be reported.

## 6. Required Authoring Files

Create exactly these MAP21_07 authoring CSVs unless a narrower equivalent is strongly justified in the Result.

```text
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_07/moonpalace_core_resource_profiles.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_07/moonpalace_core_resource_chunks.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_07/moonpalace_core_resource_nodes.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_07/moonpalace_core_resource_edges.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_07/moonpalace_core_resource_rewards.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_07/moonpalace_core_resource_persistence.csv
```

Authoring rules:

```text
CSV must use UTF-8 without BOM, LF, one final LF, stable header order, stable row order.
Coordinates are local 36x16 design-canvas coordinates only.
No generated world coordinates may be stored.
No file outside MAP21_07 authoring may be written.
```

## 7. Required Generated Artifacts

Create exactly these generated JSON artifacts.

```text
MapDesign/MCP/GENERATED/MAP21_07/moonpalace_core_resource_manifest.json
MapDesign/MCP/GENERATED/MAP21_07/moonpalace_core_resource_route_manifest.json
MapDesign/MCP/GENERATED/MAP21_07/moonpalace_core_resource_persistence_manifest.json
MapDesign/MCP/GENERATED/MAP21_07/moonpalace_core_resource_digest_manifest.json
```

Generated artifact rules:

```text
JSON must be deterministic and canonical enough to hash repeatedly.
created_utc may exist only if excluded from canonical digest.
The digest manifest must include MAP13 audit digest, MAP18 export digest, MAP21_04/05/06 source digests, every MAP21_07 CSV digest, every MAP21_07 JSON digest, and MAP21_08 handoff digest.
The generated artifacts are static production samples, not runtime save files or generated worlds.
```

## 8. Required Focused Tests

Add exactly one focused test file:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/MoonPalace/MoonPalaceCoreResourceProductionTests.cs
```

Required test category:

```text
MAP21_07
```

Required focused test cases:

```text
MoonPalaceCoreResourcesContainExactThreeRegionsAndAuthoritativeKeys
MoonPalaceCoreResourcesPublishThirtySixBySixteenCanvasAndFiveActiveChunksEach
MoonPalaceCoreResourceGraphsPreserveApprovedNodeEdgeAndRouteCounts
MoonPalaceCoreResourceLowRoutesAreMandatoryNoToolAndReachRewardAndReturn
MoonPalaceCoreResourceHighRoutesAreOptionalAndConvergeWithoutBlockingCompletion
MoonPalaceCoreResourceFailureBranchesRecoverToExistingLowRecoveryJoin
MoonPalaceCoreResourceRewardsPublishExactOneRequiredAndTwoOptionalBenefitsEach
MoonPalaceCoreResourcePersistencePublishesSevenCheckpointsAndRejectsLegacyShortKeys
MoonPalaceCoreResourcePublisherReadsMap13Map18AndMap21SourcesWithoutRewriting
MoonPalaceCoreResourcePublisherWritesOnlyMap21_07AuthoringAndGeneratedRoots
MoonPalaceCoreResourceDigestsAreDeterministicReverseOrderRepeatAndCultureStable
MoonPalaceCoreResourceRejectsDuplicateIdsBadCanvasBadGraphMissingRewardWrongBiomeAndRuntimeSideEffects
MoonPalaceCoreResourceDoesNotRunGenerationRendererValidationReplayRollbackPlayModeOrLegacyRegression
MoonPalaceCoreResourcePublishesMap21_08HandoffOnlyAfterFocusedPass
```

Only run:

```text
EditMode category MAP21_07
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

Do not run MAP13/MAP18 categories, PlayMode, or any previous task category.

## 9. Result Required Evidence

The Result must include these sections.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
## CoreResource Profile Summary
## Local Canvas Chunk and Graph Summary
## Reward and Persistence Summary
## Static Safety and Non-Execution Summary
## Snapshot and Digest Summary
## Focused Validation Summary
## No Legacy Regression Boundary Notes
## Final Status Evidence
```

Minimum required evidence:

```text
CoreResource profile records: 3
Region IDs: SR_MOON_CORE_SITE_5, SR_CASSIA_SAP_SITE_5, SR_STAR_NURUK_SITE_5
Biome mapping: MoonCore->MoonCrater, CassiaSap->CassiaRoot, StarNuruk->MoonDough
design canvas per region: 36x16
active chunks per region: 5 / 5 / 5
node rows: 44
edge rows: 51
Low route records: 3
High route records: 3
Failure branch records: 3
Recovery route records: 3
MandatoryNoTool Low routes: 3 / 3
RecoveryJoin closure: 3 / 3
required reward records: 3
optional benefit records: 6
persistence checkpoint records: 21
legacy short persistence keys accepted: 0
duplicate reward risk count: 0
permanent loss count: 0
MAP13 source modifications: 0
MAP18 source modifications: 0
MAP21_04/05/06 source modifications: 0
generated world count: 0
Tilemap writes: 0
runtime GameObject spawns: 0
reward grants: 0
inventory/resource mutations: 0
save file writes/reads: 0 / 0
Scene/Prefab changes: 0
```

## 10. Focused Validation Boundaries

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
MAP19_09 SCALE AUDIT RERUNS: 0
MAP20_01 GENERATOR RUN RERUNS: 0
MAP20_02 OVERLAY SAMPLE REGENERATION RUNS: 0
MAP20_03 DETAIL SAMPLE REGENERATION RUNS: 0
MAP20_04 NAVIGATION SAMPLE REGENERATION RUNS: 0
MAP20_05 REPLAY EXPORT SAMPLE REGENERATION RUNS: 0
MAP20_06 EXIT AUDIT REGENERATION RUNS: 0
MAP21_01 PROFILE TILE SHELL REGENERATION RUNS: 0
MAP21_02 MICROPATTERN REGENERATION RUNS: 0
MAP21_03 CRATER ROOT CLUSTER REGENERATION RUNS: 0
MAP21_04 MILL DOUGH CLUSTER REGENERATION RUNS: 0
MAP21_05 ACTIVITY EVENT REGENERATION RUNS: 0
MAP21_06 BOUNDARY REGENERATION RUNS: 0
VALIDATION RUNNER EXECUTIONS: 0
REPLAY EXECUTIONS: 0
GENERATOR EXECUTIONS: 0
RENDERER EXECUTIONS: 0
ROLLBACK EXECUTIONS: 0
CSV AUTHORING WRITES OUTSIDE MAP21_07 AUTHORING FOLDER: 0
RUNTIME OBJECT SPAWNS: 0
SCENE PREFAB CHANGES: 0
SAVE FILE WRITES/READS: 0 / 0
REWARD GRANTS: 0
INVENTORY MUTATIONS: 0
```

If a real compile or focused test failure occurs, only the MAP21_07 focused category may be rerun after a local fix. Do not broaden test selection unless the Result explicitly reports a real regression trigger and stops for review.

## 11. Finalize and Commit Rules

Finalize only if all MAP21_07 checks pass.

Status expectations before finalize:

```text
MAP21_07_COMPLETE_THREE_CORE_RESOURCE_REGIONS: CURRENT
MAP21_08_COMPLETE_MOONPALACE_VILLAGE: LOCKED
```

Status expectations after finalize:

```text
MAP21_07_COMPLETE_THREE_CORE_RESOURCE_REGIONS: COMPLETE
Current Task: NONE
MAP21_08_COMPLETE_MOONPALACE_VILLAGE: LOCKED
```

The Result must state whether Status Finalize and atomic commit were actually performed. If the local protocol intentionally writes Result before finalize, say so explicitly.

Atomic commit scope:

```text
MAP21_07 task file/archive
MAP21_07 Result
MAP21_07 authoring CSV/meta files
MAP21_07 generated JSON files
MoonPalaceCoreResourceProduction.cs and .meta if new
MoonPalaceCoreResourcePublisher.cs and .meta if new
MoonPalaceCoreResourceProductionTests.cs and .meta if new
06_IMPLEMENTATION_STATUS.md finalize change
```

Commit subject:

```text
MAP21_07 complete three core resource regions
```

Do not stage unrelated files. Do not push.

## 12. Stop Rule

After Result and commit:

```text
STOP
Do not create MAP21_08 files.
Do not run MAP21_08.
Do not rerun MAP13 or MAP18 categories.
Do not run world generation, renderer, validation runner, replay, rollback, PlayMode, legacy regression, or full regression.
```
