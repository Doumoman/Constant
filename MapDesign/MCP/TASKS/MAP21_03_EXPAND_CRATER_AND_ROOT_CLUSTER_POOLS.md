```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP21_03_EXPAND_CRATER_AND_ROOT_CLUSTER_POOLS
  task_file: TASKS/MAP21_03_EXPAND_CRATER_AND_ROOT_CLUSTER_POOLS.md
  requires_current_task: NONE
  requires_completed_task: MAP21_02_PRODUCTIONIZE_24_MICROPATTERNS
  requires_result:
    path: REPORTS/MAP21_02_PRODUCTIONIZE_24_MICROPATTERNS_RESULT.md
    status: PASS
    sha256: 8963713cd62f4f25311e876df74551e6e1304c50885652f51a32b46019ca2017
  requires_installed_task:
    path: TASKS/MAP21_02_PRODUCTIONIZE_24_MICROPATTERNS.md
    sha256: 0e29cf56fbc0172aa8fb6fcc7969700207409fc5b15103bec23b85fa70540073
  requires_handoff:
    name: MAP21_03 handoff digest
    sha256: c918b519cdcbc944746187f7dc6d1d3b0b00280f8ca47702e3e4f518ae55f4db
  sets_current_task: MAP21_03_EXPAND_CRATER_AND_ROOT_CLUSTER_POOLS
```

# MAP21_03 - Expand Crater and Root Cluster Pools

```text
TASK: MAP21_03_EXPAND_CRATER_AND_ROOT_CLUSTER_POOLS
PHASE: MAP21 - MoonPalace Vertical Slice
STATUS: CURRENT
NEXT: MAP21_04_EXPAND_MILL_AND_DOUGH_CLUSTER_POOLS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MoonCrater와 CassiaRoot의 Terrain/Quiet/Buffer TerrainCluster pool을 반복 제한이 가능한 최소 production 수량까지 확장한다.

이번 Task는 **Crater/Root cluster pool authoring**만 소유한다. AbandonedMill/MoonDough 확장, sector/world placement, renderer 실행, Tilemap bake는 아직 시작하지 않는다.

| Area | This task owns | This task must not do |
|---|---|---|
| Cluster catalog | MoonCrater 12개, CassiaRoot 12개 cluster identity와 pool kind | Mill/Dough cluster 작성 |
| Footprints | 2~5 active chunk normalized footprint와 entry/exit chunk role | Sector/world placement |
| Spine variants | cluster당 `BASE`/`ALT` 2개 variant, baseline 1개, entry/exit side와 traversal intent | full traversal solver 또는 validation runner 실행 |
| Pattern slots | MAP21_02 biome pattern을 slot별 allowlist로 연결 | pattern renderer 실행, MAP21_02 rewrite |
| Repetition signatures | pool kind별 structural/silhouette signature와 duplicate 검출 | MAP21_10 final tuning 주장 |

핵심 원칙:

```text
This task expands data pools, not generated maps.
Crater and Root get enough authored choices for repetition control.
Every cluster remains static terrain data with no Activity/Event/Special/Population binding.
No generated terrain, Tilemap, Scene, Prefab, runtime object, or previous source CSV is mutated.
```

## 1. 사용자 보고 의무

Result 첫 부분에는 반드시 아래 두 섹션을 둔다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

`User-Facing Implementation Report`에는 아래 질문에 답한다.

```text
이번 작업이 MoonCrater/CassiaRoot cluster pool을 어떻게 늘렸는가?
각 biome별 Terrain/Quiet/Buffer 수량은 얼마인가?
footprint, spine variant, pattern slot은 어떤 책임을 가지는가?
MAP21_02 MicroPattern과 어떻게 연결했는가?
반복 제한용 structural/silhouette signature는 무엇을 막는가?
이번 작업이 Mill/Dough, sector/world generation, renderer, Tilemap, Scene/Prefab/runtime을 건드리지 않았다는 증거는 무엇인가?
legacy regression을 돌리지 않았다는 증거는 무엇인가?
MAP21_04는 왜 아직 시작하지 않았는가?
```

`Responsibility and Added Scripts`는 반드시 표로 작성한다.

```text
| Script or file | Added or changed responsibility | Explicit non-ownership |
```

파일별 책임이 구체적이지 않으면 FAIL이다.

## 2. 선행조건

작업 시작 전에 다음을 확인한다.

```text
MapDesign/MCP/REPORTS/MAP21_02_PRODUCTIONIZE_24_MICROPATTERNS_RESULT.md exists
MAP21_02 Result STATUS: PASS
MAP21_02 Result SHA-256:
8963713cd62f4f25311e876df74551e6e1304c50885652f51a32b46019ca2017

MapDesign/MCP/TASKS/MAP21_02_PRODUCTIONIZE_24_MICROPATTERNS.md exists
MAP21_02 installed Task SHA-256:
0e29cf56fbc0172aa8fb6fcc7969700207409fc5b15103bec23b85fa70540073

MAP21_03 handoff digest:
c918b519cdcbc944746187f7dc6d1d3b0b00280f8ca47702e3e4f518ae55f4db

MAP21_02 source digests:
pattern catalog digest: 64765ac5aeccf149ebf635c54519a58ecc2629f98e6cacd285ee1bfacb77c560
pattern cell digest: d5d9fc1a8ff1b4e019c1fa779731ecbc80ec4a02da695c409ec9ccd4046b01ec
pattern tag digest: 35a9ce555c91ebc786fc90f142250bd931de04de40fbeaef665ddb8398cd1c62
tile shell digest: 04eefb76556b4874c4a60838149d523e084e0d315fc2d833a9458c9fc6ad2514

MAP21_04_EXPAND_MILL_AND_DOUGH_CLUSTER_POOLS: LOCKED
```

불일치 시:

```text
STATUS: BLOCKED
reason: precondition mismatch
created/changed code files: 0
MAP21_04 started: NO
STOP
```

## 3. 허용 범위

허용 파일:

```text
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/MoonPalaceClusterPoolProduction.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/MoonPalace/MoonPalaceClusterPoolPublisher.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/MoonPalace/MoonPalaceClusterPoolProductionTests.cs
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_03/**
MapDesign/MCP/GENERATED/MAP21_03/**
MapDesign/MCP/TASKS/MAP21_03_EXPAND_CRATER_AND_ROOT_CLUSTER_POOLS.md
MapDesign/MCP/REPORTS/MAP21_03_EXPAND_CRATER_AND_ROOT_CLUSTER_POOLS_RESULT.md
MapDesign/MCP_ARCHIVE/MAP21_03_EXPAND_CRATER_AND_ROOT_CLUSTER_POOLS.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

조건부 허용:

```text
Existing MAP11 TerrainCluster public contracts may be referenced read-only for vocabulary compatibility.
Existing MAP21_01 biome/tile shell and MAP21_02 MicroPattern authoring/generated files may be read and hashed.
MAP21_02 Crater/Root pattern ids may be copied into MAP21_03 slot allowlists.
Authoring CSV writes are allowed only under Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_03.
Generated report JSON writes are allowed only under MapDesign/MCP/GENERATED/MAP21_03.
```

금지:

```text
AbandonedMill or MoonDough cluster authoring
MAP10 source CSV rewrite
MAP21_01 profile/tile shell rewrite
MAP21_02 MicroPattern rewrite
cluster placement into sector/world
sector/world generation
pattern renderer execution or behavior change
Activity/Event/SpecialRegion/Population binding
Tilemap bake or Tilemap mutation
Scene / Prefab / ProjectSettings / Packages changes
sprite/audio/background asset import
Addressables group/build change
runtime GameObject instantiate/enable/disable/destroy during tooling actions
generator solve/reroll/execution
validation runner execution
actual replay execution
rollback execution
CSV authoring edit outside MAP21_03 authoring folder
auto-fix / auto-repair
production seed approval
external process launch
manual gameplay test
automatic MAP21_04 start
broad refactor or optimization rewrite
```

회귀 금지:

```text
MAP19_09 scale audit rerun
MAP20_01 generator run rerun
MAP20_02 overlay sample regeneration
MAP20_03 detail sample regeneration
MAP20_04 navigation sample regeneration
MAP20_05 replay/export sample regeneration
MAP20_06 exit audit regeneration
MAP21_01 profile/tile shell regeneration
MAP21_02 MicroPattern regeneration
legacy 19347 regression
prior task category selection
PlayMode selection
unfiltered test run
full regression run
```

## 4. Cluster Pool 계약

`MoonPalaceClusterPoolProduction` must define immutable records only.

Required catalog fields:

```text
cluster_id
schema_version
biome_id
pool_kind
pacing_role
access_class
active_chunk_count
source_starter_cluster_id
source_pattern_pool_digest
footprint_digest
spine_variant_digest
pattern_slot_digest
structural_signature
silhouette_signature
repeat_group
created_utc_excluded_from_canonical_digest
canonical_digest
```

Required footprint fields:

```text
cluster_id
chunk_x
chunk_y
chunk_role
entry_side
exit_side
is_entry_chunk
is_exit_chunk
canonical_digest
```

Required spine variant fields:

```text
cluster_id
spine_variant_id
is_baseline
entry_side
exit_side
route_intent
movement_tokens
high_route_state
recovery_state
protected_mask_policy
canonical_digest
```

Required pattern slot fields:

```text
cluster_id
slot_id
chunk_x
chunk_y
slot_kind
allowed_pattern_ids
biome_id
protected_mask_policy
risk_budget
canonical_digest
```

Rules:

```text
There must be exactly 24 cluster catalog records.
MoonCrater has exactly 12 records; CassiaRoot has exactly 12 records.
AbandonedMill and MoonDough record count must be 0.
Each biome has Terrain 6 / Quiet 3 / Buffer 3 records.
Quiet clusters use exact 2 active chunks.
Buffer clusters use 2..3 active chunks.
Terrain clusters use 3..5 active chunks.
Every footprint is normalized, connected by 4-neighbor adjacency, duplicate-free, and bounded to 4x4 chunks.
Every cluster has exactly two spine variants, one BASE and one ALT; exactly one variant is baseline.
Every active chunk has at least one pattern slot.
Pattern slot allowlists must use only MAP21_02 pattern ids from the same biome.
```

Allowed pool kinds:

```text
Terrain
Quiet
Buffer
```

Allowed repeat groups:

```text
CraterTerrain
CraterQuiet
CraterBuffer
RootTerrain
RootQuiet
RootBuffer
```

## 5. Required 24 Cluster Inventory

The productionized Crater/Root set must contain exactly these cluster ids.

| Biome | Pool | Required cluster ids |
|---|---|---|
| MoonCrater | Terrain | `TC_CRATER_PROD_RIM_ASCENT`, `TC_CRATER_PROD_BROKEN_SLOPE`, `TC_CRATER_PROD_BOWL_CROSS`, `TC_CRATER_PROD_ROCK_SHELF_CHAIN`, `TC_CRATER_PROD_HIGH_LEDGE`, `TC_CRATER_PROD_FALL_RECOVERY` |
| MoonCrater | Quiet | `TC_CRATER_QUIET_DUST_RIM`, `TC_CRATER_QUIET_SHALLOW_BOWL`, `TC_CRATER_QUIET_SHADOW_SHELF` |
| MoonCrater | Buffer | `TC_CRATER_BUFFER_ENTRY_RAMP`, `TC_CRATER_BUFFER_EXIT_LEDGE`, `TC_CRATER_BUFFER_RECOVERY_DIP` |
| CassiaRoot | Terrain | `TC_ROOT_PROD_ARCH_FLOW`, `TC_ROOT_PROD_VERTICAL_TUNNEL`, `TC_ROOT_PROD_HOLLOW_POCKET`, `TC_ROOT_PROD_CANOPY_SWITCHBACK`, `TC_ROOT_PROD_SAP_LEDGE`, `TC_ROOT_PROD_FORK_RECOVERY` |
| CassiaRoot | Quiet | `TC_ROOT_QUIET_ARCH_SHADE`, `TC_ROOT_QUIET_VINE_PASSAGE`, `TC_ROOT_QUIET_ROOT_BRIDGE` |
| CassiaRoot | Buffer | `TC_ROOT_BUFFER_ENTRY_ARCH`, `TC_ROOT_BUFFER_EXIT_TUNNEL`, `TC_ROOT_BUFFER_RECOVERY_CANOPY` |

Rules:

```text
cluster_id values must be unique and stable.
All ids must obey ^TC_[A-Z0-9_]+$.
Do not infer biome/pool from id parsing; typed CSV fields are authoritative.
At least 8 records must source-map to existing MAP11 starter Crater/Root cluster ids or explicit MissingData if no exact source applies.
New production variants must still reference MAP21_02 pattern pool digests.
```

## 6. Static Cluster Safety

Every cluster is static terrain authoring only.

Required static evidence:

```text
activity_slot_count: 0
event_overlay_count: 0
special_region_binding_count: 0
population_slot_count: 0
reward_anchor_count: 0
runtime_binding_count: 0
tilemap_write_count: 0
```

Quiet/Buffer rules:

```text
Quiet clusters must have no hazard tags in required baseline slots.
Buffer clusters may contain recovery/cue markers but no reward, boss, village, forge, shop, or required-resource marker.
Buffer clusters must declare before/after use compatibility as data only; they do not search for landmarks.
```

Repetition rules:

```text
structural_signature must include normalized footprint, entry/exit side, baseline spine intent, and active chunk role sequence.
silhouette_signature must include slot pattern family and MAP21_02 source operation summary.
No duplicate structural_signature is allowed inside the same biome.
No duplicate full structural_signature is allowed across all 24 records.
repeat_group must allow downstream recent-use distance checks, but this Task does not tune final distances.
```

## 7. Snapshot / Digest 계약

Required authoring files:

```text
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_03/moonpalace_crater_root_cluster_catalog.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_03/moonpalace_crater_root_cluster_footprints.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_03/moonpalace_crater_root_cluster_spine_variants.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_03/moonpalace_crater_root_cluster_pattern_slots.csv
```

Required generated artifacts:

```text
MapDesign/MCP/GENERATED/MAP21_03/moonpalace_crater_root_cluster_pool_manifest.json
MapDesign/MCP/GENERATED/MAP21_03/moonpalace_crater_root_cluster_signature_manifest.json
MapDesign/MCP/GENERATED/MAP21_03/moonpalace_crater_root_cluster_digest_manifest.json
```

Digest manifest required fields:

```text
schema_version
task_id
source_MAP21_02_result_digest
source_MAP21_02_task_digest
source_MAP21_03_handoff_digest
source_pattern_catalog_digest
source_pattern_cell_digest
source_pattern_tag_digest
cluster_catalog_digest
cluster_footprint_digest
cluster_spine_variant_digest
cluster_pattern_slot_digest
cluster_signature_digest
MAP21_04_handoff_digest
created_utc_excluded_from_canonical_digest
canonical_digest
```

All canonical digests must exclude `created_utc`, use deterministic order, and be lowercase SHA-256.

## 8. 중복과 하드코딩 방지

Result에 아래 카운터를 기록하고 모두 0이어야 한다.

```text
MAP10 source CSV modified:
MAP21_01 source profile/tile shell modified:
MAP21_02 source MicroPattern modified:
Mill/Dough cluster records authored:
generation pipeline C# modified:
renderer C# modified:
Scene/Prefab/ProjectSettings/Packages modified:
duplicated generator logic count:
duplicated validation logic count:
duplicated renderer logic count:
hard-coded pattern id references outside MAP21_03 authoring/sample constants:
hard-coded digest string copies outside precondition/handoff constants:
```

## 9. Focused Tests

Focused test category:

```text
MAP21_03
```

Required test names:

```text
CraterRootClusterPoolContainsExact24ClustersAndNoMillDoughRecords
CraterRootClusterPoolHasTerrainQuietBufferCountsAndValidActiveChunkRanges
CraterRootClusterFootprintsAreConnectedNormalizedUniqueAndBounded
CraterRootClusterSpineVariantsHaveSingleBaselineAltPortsAndStaticRouteIntent
CraterRootClusterPatternSlotsReferenceOnlySameBiomeMap21_02Patterns
CraterRootClusterPoolRejectsDuplicateIdsBadFootprintsUnknownPatternsAndInvalidPools
CraterRootClusterRepetitionSignaturesAreUniqueAndDeterministic
CraterRootClusterPoolKeepsQuietBufferStaticWithoutActivityEventSpecialPopulationOrRewards
CraterRootClusterPublisherWritesOnlyMap21_03AuthoringAndGeneratedRoots
CraterRootClusterPoolPublishesMap21_04HandoffOnlyAfterFocusedPass
CraterRootClusterPoolDoesNotRunGenerationRendererValidationReplayRollbackPlayModeOrLegacyRegression
```

Required count:

```text
discovered: 11
executed: 11
passed: 11
failed: 0
```

Focused validation may create sample artifacts only under `MapDesign/MCP/GENERATED/MAP21_03`.

## 10. 회귀 금지 정책

이번 Task의 검증은 focused MAP21_03 EditMode selection과 compile/console check까지만 허용한다.

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
MAP20_05 REPLAY EXPORT SAMPLE REGENERATION RUNS: 0
MAP20_06 EXIT AUDIT REGENERATION RUNS: 0
MAP21_01 PROFILE TILE SHELL REGENERATION RUNS: 0
MAP21_02 MICROPATTERN REGENERATION RUNS: 0
VALIDATION RUNNER EXECUTIONS: 0
REPLAY EXECUTIONS: 0
GENERATOR EXECUTIONS: 0
RENDERER EXECUTIONS: 0
ROLLBACK EXECUTIONS: 0
CSV AUTHORING WRITES OUTSIDE MAP21_03 AUTHORING FOLDER: 0
RUNTIME OBJECT SPAWNS: 0
SCENE PREFAB CHANGES: 0
```

## 11. Result 필수 기록

Result 파일:

```text
MapDesign/MCP/REPORTS/MAP21_03_EXPAND_CRATER_AND_ROOT_CLUSTER_POOLS_RESULT.md
```

Required sections:

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
## Cluster Inventory and Pool Summary
## Footprint Spine and Pattern Slot Summary
## Static Safety and Repetition Summary
## Snapshot and Digest Summary
## Focused Validation Summary
## No Legacy Regression Boundary Notes
## Final Status Evidence
```

Required summary fields:

```text
MAP21_02 Result SHA-256 required/actual:
MAP21_02 installed Task SHA-256 required/actual:
MAP21_03 handoff digest required/actual:
source pattern catalog/cell/tag digests:

cluster catalog records:
MoonCrater cluster records:
CassiaRoot cluster records:
AbandonedMill/MoonDough cluster records:
pool kind counts:
source starter mappings:

footprint records:
connected normalized footprints:
active chunk ranges:
spine variant records:
baseline variants:
ALT variants:
pattern slot records:
same-biome MAP21_02 pattern references:
unknown pattern references:

activity/event/special/population/reward/runtime binding counts:
Quiet hazard baseline slot count:
Buffer landmark search executions:
structural signature count:
structural signature duplicates:
silhouette signature duplicates within biome/pool:

authoring files written:
generated sample artifacts created:
cluster pool manifest digest lower-hex SHA-256:
cluster signature manifest digest lower-hex SHA-256:
cluster digest manifest lower-hex SHA-256:
MAP21_04 handoff digest lower-hex SHA-256:

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

## 12. PASS / FAIL / BLOCKED

PASS 조건:

```text
Precondition SHA and handoff digest match
Exactly 24 Crater/Root cluster records exist
MoonCrater and CassiaRoot each have Terrain 6 / Quiet 3 / Buffer 3
AbandonedMill and MoonDough cluster records are 0
All footprints are connected, normalized, duplicate-free, and bounded
Each cluster has BASE/ALT variants and exactly one baseline
Pattern slots reference only same-biome MAP21_02 patterns
Quiet/Buffer clusters remain static and have zero Activity/Event/Special/Population/reward/runtime binding
Structural signatures are unique across all 24 records
Focused MAP21_03 EditMode tests are 11/11 PASS
All no-regression and no-execution counters are zero
Responsibility and Added Scripts table is present and specific
MAP21_04 remains LOCKED and not started
```

FAIL 조건:

```text
Cluster count is not 24 or biome/pool counts are wrong
Mill/Dough records are authored
Footprint is disconnected, non-normalized, duplicated, or out of bounds
Spine variant baseline count is not exactly one per cluster
Pattern slot references wrong-biome or unknown MAP21_02 pattern ids
Quiet/Buffer cluster contains Activity/Event/Special/Population/reward/runtime binding
Duplicate structural signature exists
MAP10, MAP21_01, or MAP21_02 source is rewritten
Cluster/sector/world generation, renderer, validation runner, replay, rollback, PlayMode, legacy regression, unfiltered run, or full regression is executed
Sprite/audio/background assets are imported
Scene/Prefab/Tilemap/Addressables/ProjectSettings/Packages are changed
CSV authoring is written outside MAP21_03 authoring folder
MAP21_04 starts
```

BLOCKED 조건:

```text
Precondition SHA mismatch
Compile error unrelated to this task that prevents focused validation
MAP21_02 Crater/Root MicroPattern pool cannot be read or source-mapped
Existing MAP11 vocabulary compatibility cannot be represented without changing MAP11 authority
Authoring CSV location is unavailable or conflicts with existing authoritative MAP21 files
```

When BLOCKED:

```text
Do not finalize MAP21_03
Do not start MAP21_04
Do not publish MAP21_04 handoff digest
Record exact owner, invariant, and minimum next action
STOP
```

## 13. Finalize / Commit / STOP

PASS일 때만:

```text
MAP21_03_EXPAND_CRATER_AND_ROOT_CLUSTER_POOLS: COMPLETE
MAP21_04_EXPAND_MILL_AND_DOUGH_CLUSTER_POOLS: LOCKED
```

FAIL 또는 BLOCKED일 때:

```text
MAP21_03 remains CURRENT
MAP21_04 remains LOCKED
STOP
```

PASS일 때만 atomic commit을 만든다.

Commit subject:

```text
MAP21_03 expand crater root cluster pools
```

Commit body에는 다음을 포함한다.

```text
MAP21_03 responsibilities
focused test count
cluster count and biome/pool counts
footprint/spine/pattern slot counts
static safety counts
structural signature counts
MAP21_04 handoff digest
legacy regression counters all zero
generation/renderer/replay/rollback/validation counters all zero
Scene/Prefab/Tilemap/Addressables changes zero
```

Git push는 하지 않는다.

Result 작성, 상태 finalize, commit 이후 반드시 멈춘다.

```text
DO NOT START MAP21_04.
DO NOT CREATE MAP21_04 FILES.
DO NOT RERUN MAP19_09 SCALE AUDIT.
DO NOT REGENERATE MAP20_01 THROUGH MAP20_06 ARTIFACTS.
DO NOT REGENERATE MAP21_01 PROFILE TILE SHELL.
DO NOT REGENERATE MAP21_02 MICROPATTERNS.
DO NOT AUTHOR MILL OR DOUGH CLUSTERS.
DO NOT RUN VALIDATION RUNNER.
DO NOT EXECUTE REPLAY.
DO NOT EXECUTE GENERATOR.
DO NOT EXECUTE RENDERER.
DO NOT EXECUTE ROLLBACK.
DO NOT WRITE CSV AUTHORING FILES OUTSIDE MAP21_03 AUTHORING FOLDER.
DO NOT IMPORT SPRITE AUDIO OR BACKGROUND ASSETS.
DO NOT MUTATE SCENE PREFAB TILEMAP ADDRESSABLES OR RUNTIME OBJECTS.
DO NOT RUN LEGACY REGRESSION.
STOP.
```
