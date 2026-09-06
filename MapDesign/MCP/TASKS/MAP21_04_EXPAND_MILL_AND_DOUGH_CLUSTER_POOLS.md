```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP21_04_EXPAND_MILL_AND_DOUGH_CLUSTER_POOLS
  task_file: TASKS/MAP21_04_EXPAND_MILL_AND_DOUGH_CLUSTER_POOLS.md
  requires_current_task: NONE
  requires_completed_task: MAP21_03_EXPAND_CRATER_AND_ROOT_CLUSTER_POOLS
  requires_result:
    path: REPORTS/MAP21_03_EXPAND_CRATER_AND_ROOT_CLUSTER_POOLS_RESULT.md
    status: PASS
    sha256: 279bf1c735a81a8c49da62a1e2d84567ef0691987f19a13634ef750579c64d08
  requires_installed_task:
    path: TASKS/MAP21_03_EXPAND_CRATER_AND_ROOT_CLUSTER_POOLS.md
    sha256: 1a414d0bbbb90fa048e7b5767ca027f07f74f8c9a5f4060dfe4ce3840c6ca2a8
  requires_handoff:
    name: MAP21_04 handoff digest
    sha256: d0f96239ae6a8dc44ff11e36ff387618565c834f69bd5a18dc307994b71fef3c
  sets_current_task: MAP21_04_EXPAND_MILL_AND_DOUGH_CLUSTER_POOLS
```

# MAP21_04 - Expand Mill and Dough Cluster Pools

```text
TASK: MAP21_04_EXPAND_MILL_AND_DOUGH_CLUSTER_POOLS
PHASE: MAP21 - MoonPalace Vertical Slice
STATUS: CURRENT
NEXT: MAP21_05_PRODUCTIONIZE_ACTIVITIES_AND_EVENT_OVERLAYS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

AbandonedMill과 MoonDough의 Terrain/Quiet/Buffer TerrainCluster pool을 반복 제한이 가능한 최소 production 수량까지 확장한다.

이번 Task는 **Mill/Dough cluster pool authoring과 four-biome read-only combined manifest**만 소유한다. Crater/Root authoring은 MAP21_03 결과를 읽기 전용으로 참조하고, sector/world placement와 renderer 실행은 아직 시작하지 않는다.

| Area | This task owns | This task must not do |
|---|---|---|
| Mill/Dough catalog | AbandonedMill 12개, MoonDough 12개 cluster identity와 pool kind | Crater/Root authoring rewrite |
| Footprints | 2~5 active chunk normalized footprint와 entry/exit chunk role | Sector/world placement |
| Spine variants | cluster당 `BASE`/`ALT` 2개 variant, baseline 1개, entry/exit side와 traversal intent | traversal solver 또는 validation runner 실행 |
| Pattern slots | MAP21_02 Mill/Dough pattern을 slot별 allowlist로 연결 | pattern renderer 실행, MAP21_02 rewrite |
| Combined manifest | MAP21_03 Crater/Root + MAP21_04 Mill/Dough 총 48개 cluster digest chain | MAP21_03 artifact regeneration |

핵심 원칙:

```text
This task finishes the four-biome cluster pool inventory, not generated maps.
MAP21_03 Crater/Root outputs are read and hashed only.
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
이번 작업이 AbandonedMill/MoonDough cluster pool을 어떻게 늘렸는가?
각 biome별 Terrain/Quiet/Buffer 수량은 얼마인가?
MAP21_03 Crater/Root 결과는 어떻게 읽기 전용으로 보존했는가?
four-biome combined manifest는 무엇을 보증하는가?
footprint, spine variant, pattern slot, repetition signature는 어떤 책임을 가지는가?
MAP21_02 MicroPattern과 어떻게 연결했는가?
이번 작업이 Crater/Root rewrite, sector/world generation, renderer, Tilemap, Scene/Prefab/runtime을 건드리지 않았다는 증거는 무엇인가?
legacy regression을 돌리지 않았다는 증거는 무엇인가?
MAP21_05는 왜 아직 시작하지 않았는가?
```

`Responsibility and Added Scripts`는 반드시 표로 작성한다.

```text
| Script or file | Added or changed responsibility | Explicit non-ownership |
```

파일별 책임이 구체적이지 않으면 FAIL이다.

## 2. 선행조건

작업 시작 전에 다음을 확인한다.

```text
MapDesign/MCP/REPORTS/MAP21_03_EXPAND_CRATER_AND_ROOT_CLUSTER_POOLS_RESULT.md exists
MAP21_03 Result STATUS: PASS
MAP21_03 Result SHA-256:
279bf1c735a81a8c49da62a1e2d84567ef0691987f19a13634ef750579c64d08

MapDesign/MCP/TASKS/MAP21_03_EXPAND_CRATER_AND_ROOT_CLUSTER_POOLS.md exists
MAP21_03 installed Task SHA-256:
1a414d0bbbb90fa048e7b5767ca027f07f74f8c9a5f4060dfe4ce3840c6ca2a8

MAP21_04 handoff digest:
d0f96239ae6a8dc44ff11e36ff387618565c834f69bd5a18dc307994b71fef3c

MAP21_02 source digests:
pattern catalog digest: 64765ac5aeccf149ebf635c54519a58ecc2629f98e6cacd285ee1bfacb77c560
pattern cell digest: d5d9fc1a8ff1b4e019c1fa779731ecbc80ec4a02da695c409ec9ccd4046b01ec
pattern tag digest: 35a9ce555c91ebc786fc90f142250bd931de04de40fbeaef665ddb8398cd1c62

MAP21_03 Crater/Root digests:
cluster catalog digest: 6ce0618c6973993f9713e0ac24fc4fc23db08cd13836766fe477b9afef333cc0
cluster footprint digest: 638319ac268c2d754e1ab8f6d9f4dd1d42ac4d6dcea20fb0054dd1ba8e8118ad
cluster spine variant digest: fd1ab5dd284d203065439373ca1dab13b597baf215f4fce78dd16760aab70bdd
cluster pattern slot digest: 053dc0673c87d8e067786a61b0713c9ce3926025d737363b67647232dfd73842
cluster signature digest: df017920326a4c82ec22358c51f70f04b0faf34c589d5b983bb0427410eb4a8f

MAP21_05_PRODUCTIONIZE_ACTIVITIES_AND_EVENT_OVERLAYS: LOCKED
```

불일치 시:

```text
STATUS: BLOCKED
reason: precondition mismatch
created/changed code files: 0
MAP21_05 started: NO
STOP
```

## 3. 허용 범위

허용 파일:

```text
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/MoonPalaceClusterPoolProduction.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/MoonPalaceClusterPoolUnion.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/MoonPalace/MoonPalaceMillDoughClusterPoolPublisher.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/MoonPalace/MoonPalaceMillDoughClusterPoolProductionTests.cs
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_04/**
MapDesign/MCP/GENERATED/MAP21_04/**
MapDesign/MCP/TASKS/MAP21_04_EXPAND_MILL_AND_DOUGH_CLUSTER_POOLS.md
MapDesign/MCP/REPORTS/MAP21_04_EXPAND_MILL_AND_DOUGH_CLUSTER_POOLS_RESULT.md
MapDesign/MCP_ARCHIVE/MAP21_04_EXPAND_MILL_AND_DOUGH_CLUSTER_POOLS.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

조건부 허용:

```text
Existing MoonPalaceClusterPoolProduction.cs may be modified only to expose shared all-biome validation/serialization helpers and must preserve MAP21_03 digests.
Existing MAP11 TerrainCluster public contracts may be referenced read-only for vocabulary compatibility.
Existing MAP21_01, MAP21_02, and MAP21_03 authoring/generated files may be read and hashed.
MAP21_02 Mill/Dough pattern ids may be copied into MAP21_04 slot allowlists.
Authoring CSV writes are allowed only under Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_04.
Generated report JSON writes are allowed only under MapDesign/MCP/GENERATED/MAP21_04.
```

금지:

```text
MoonCrater or CassiaRoot authoring rewrite
MAP10 source CSV rewrite
MAP21_01 profile/tile shell rewrite
MAP21_02 MicroPattern rewrite
MAP21_03 Crater/Root artifact regeneration
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
CSV authoring edit outside MAP21_04 authoring folder
auto-fix / auto-repair
production seed approval
external process launch
manual gameplay test
automatic MAP21_05 start
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
MAP21_03 Crater/Root cluster regeneration
legacy 19347 regression
prior task category selection
PlayMode selection
unfiltered test run
full regression run
```

## 4. Cluster Pool 계약

MAP21_04 must reuse the MAP21_03 cluster pool data model and must not create a second incompatible schema.

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

Required footprint/spine/slot fields are the same as MAP21_03:

```text
footprint: cluster_id, chunk_x, chunk_y, chunk_role, entry_side, exit_side, is_entry_chunk, is_exit_chunk, canonical_digest
spine: cluster_id, spine_variant_id, is_baseline, entry_side, exit_side, route_intent, movement_tokens, high_route_state, recovery_state, protected_mask_policy, canonical_digest
slot: cluster_id, slot_id, chunk_x, chunk_y, slot_kind, allowed_pattern_ids, biome_id, protected_mask_policy, risk_budget, canonical_digest
```

Rules:

```text
There must be exactly 24 new Mill/Dough cluster catalog records.
AbandonedMill has exactly 12 records; MoonDough has exactly 12 records.
MoonCrater and CassiaRoot authored record count in MAP21_04 files must be 0.
The combined manifest must contain exactly 48 records: 24 from MAP21_03 plus 24 from MAP21_04.
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
MillTerrain
MillQuiet
MillBuffer
DoughTerrain
DoughQuiet
DoughBuffer
```

## 5. Required 24 Cluster Inventory

The productionized Mill/Dough set must contain exactly these cluster ids.

| Biome | Pool | Required cluster ids |
|---|---|---|
| AbandonedMill | Terrain | `TC_MILL_PROD_BEAM_OVERHANG`, `TC_MILL_PROD_BROKEN_PILLAR`, `TC_MILL_PROD_ORTHOGONAL_SHAFT`, `TC_MILL_PROD_GEAR_GALLERY`, `TC_MILL_PROD_RUST_LEDGE`, `TC_MILL_PROD_FALL_RECOVERY` |
| AbandonedMill | Quiet | `TC_MILL_QUIET_BEAM_WALK`, `TC_MILL_QUIET_RUST_BALCONY`, `TC_MILL_QUIET_GEAR_SHADOW` |
| AbandonedMill | Buffer | `TC_MILL_BUFFER_ENTRY_BEAM`, `TC_MILL_BUFFER_EXIT_SHAFT`, `TC_MILL_BUFFER_RECOVERY_PLATFORM` |
| MoonDough | Terrain | `TC_DOUGH_PROD_BOUNCE_CUP`, `TC_DOUGH_PROD_SOFT_POCKET`, `TC_DOUGH_PROD_STICKY_SHELF`, `TC_DOUGH_PROD_FERMENT_RISE`, `TC_DOUGH_PROD_RECOVERY_PAD_CHAIN`, `TC_DOUGH_PROD_SQUISH_CROSS` |
| MoonDough | Quiet | `TC_DOUGH_QUIET_SOFT_SHELF`, `TC_DOUGH_QUIET_FERMENT_POCKET`, `TC_DOUGH_QUIET_RECOVERY_PAD` |
| MoonDough | Buffer | `TC_DOUGH_BUFFER_ENTRY_CUP`, `TC_DOUGH_BUFFER_EXIT_SHELF`, `TC_DOUGH_BUFFER_RECOVERY_BOWL` |

Rules:

```text
cluster_id values must be unique and stable.
All ids must obey ^TC_[A-Z0-9_]+$.
Do not infer biome/pool from id parsing; typed CSV fields are authoritative.
At least 8 records must source-map to existing MAP11 starter Mill/Dough cluster ids or explicit MissingData if no exact source applies.
New production variants must reference MAP21_02 pattern pool digests.
```

## 6. Static Cluster Safety and Combined Uniqueness

Every new cluster is static terrain authoring only.

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
No duplicate structural_signature is allowed inside the new 24 records.
No duplicate full structural_signature is allowed across combined 48 Crater/Root/Mill/Dough records.
repeat_group must allow downstream recent-use distance checks, but this Task does not tune final distances.
```

## 7. Snapshot / Digest 계약

Required authoring files:

```text
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_04/moonpalace_mill_dough_cluster_catalog.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_04/moonpalace_mill_dough_cluster_footprints.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_04/moonpalace_mill_dough_cluster_spine_variants.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_04/moonpalace_mill_dough_cluster_pattern_slots.csv
```

Required generated artifacts:

```text
MapDesign/MCP/GENERATED/MAP21_04/moonpalace_mill_dough_cluster_pool_manifest.json
MapDesign/MCP/GENERATED/MAP21_04/moonpalace_mill_dough_cluster_signature_manifest.json
MapDesign/MCP/GENERATED/MAP21_04/moonpalace_all_biome_cluster_pool_manifest.json
MapDesign/MCP/GENERATED/MAP21_04/moonpalace_mill_dough_cluster_digest_manifest.json
```

Digest manifest required fields:

```text
schema_version
task_id
source_MAP21_03_result_digest
source_MAP21_03_task_digest
source_MAP21_04_handoff_digest
source_pattern_catalog_digest
source_pattern_cell_digest
source_pattern_tag_digest
source_crater_root_cluster_digest
mill_dough_cluster_catalog_digest
mill_dough_cluster_footprint_digest
mill_dough_cluster_spine_variant_digest
mill_dough_cluster_pattern_slot_digest
mill_dough_cluster_signature_digest
all_biome_cluster_pool_digest
MAP21_05_handoff_digest
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
MAP21_03 Crater/Root source modified:
Crater/Root records authored in MAP21_04 files:
generation pipeline C# modified:
renderer C# modified:
Scene/Prefab/ProjectSettings/Packages modified:
duplicated generator logic count:
duplicated validation logic count:
duplicated renderer logic count:
duplicated cluster pool schema count:
hard-coded pattern id references outside MAP21_04 authoring/sample constants:
hard-coded digest string copies outside precondition/handoff constants:
```

## 9. Focused Tests

Focused test category:

```text
MAP21_04
```

Required test names:

```text
MillDoughClusterPoolContainsExact24ClustersAndNoCraterRootAuthoredRecords
MillDoughClusterPoolHasTerrainQuietBufferCountsAndValidActiveChunkRanges
MillDoughClusterFootprintsAreConnectedNormalizedUniqueAndBounded
MillDoughClusterSpineVariantsHaveSingleBaselineAltPortsAndStaticRouteIntent
MillDoughClusterPatternSlotsReferenceOnlySameBiomeMap21_02Patterns
MillDoughClusterPoolRejectsDuplicateIdsBadFootprintsUnknownPatternsAndInvalidPools
MillDoughClusterRepetitionSignaturesAreUniqueLocallyAndAcrossCombined48
MillDoughClusterPoolKeepsQuietBufferStaticWithoutActivityEventSpecialPopulationOrRewards
MillDoughClusterPublisherReadsCraterRootArtifactsWithoutRegeneratingOrRewriting
MillDoughClusterPublisherWritesOnlyMap21_04AuthoringAndGeneratedRoots
MillDoughClusterPoolPublishesMap21_05HandoffOnlyAfterFocusedPass
MillDoughClusterPoolDoesNotRunGenerationRendererValidationReplayRollbackPlayModeOrLegacyRegression
```

Required count:

```text
discovered: 12
executed: 12
passed: 12
failed: 0
```

Focused validation may create sample artifacts only under `MapDesign/MCP/GENERATED/MAP21_04`.

## 10. 회귀 금지 정책

이번 Task의 검증은 focused MAP21_04 EditMode selection과 compile/console check까지만 허용한다.

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
MAP21_03 CRATER ROOT CLUSTER REGENERATION RUNS: 0
VALIDATION RUNNER EXECUTIONS: 0
REPLAY EXECUTIONS: 0
GENERATOR EXECUTIONS: 0
RENDERER EXECUTIONS: 0
ROLLBACK EXECUTIONS: 0
CSV AUTHORING WRITES OUTSIDE MAP21_04 AUTHORING FOLDER: 0
RUNTIME OBJECT SPAWNS: 0
SCENE PREFAB CHANGES: 0
```

## 11. Result 필수 기록

Result 파일:

```text
MapDesign/MCP/REPORTS/MAP21_04_EXPAND_MILL_AND_DOUGH_CLUSTER_POOLS_RESULT.md
```

Required sections:

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
## Cluster Inventory and Combined Pool Summary
## Footprint Spine and Pattern Slot Summary
## Static Safety and Repetition Summary
## Snapshot and Digest Summary
## Focused Validation Summary
## No Legacy Regression Boundary Notes
## Final Status Evidence
```

Required summary fields:

```text
MAP21_03 Result SHA-256 required/actual:
MAP21_03 installed Task SHA-256 required/actual:
MAP21_04 handoff digest required/actual:
source pattern catalog/cell/tag digests:
source Crater/Root cluster digests required/actual:

new cluster catalog records:
AbandonedMill cluster records:
MoonDough cluster records:
MoonCrater/CassiaRoot authored records in MAP21_04:
combined all-biome cluster records:
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
Buffer forbidden marker reference count:
Buffer landmark search executions:
local structural signature count:
combined structural signature count:
combined structural signature duplicates:
silhouette signature duplicates within biome/pool:

authoring files written:
generated sample artifacts created:
mill dough cluster pool manifest digest lower-hex SHA-256:
mill dough cluster signature manifest digest lower-hex SHA-256:
all biome cluster pool manifest digest lower-hex SHA-256:
cluster digest manifest lower-hex SHA-256:
MAP21_05 handoff digest lower-hex SHA-256:

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
Exactly 24 Mill/Dough cluster records exist
AbandonedMill and MoonDough each have Terrain 6 / Quiet 3 / Buffer 3
MoonCrater and CassiaRoot authored records in MAP21_04 files are 0
Combined all-biome manifest contains exactly 48 records
All footprints are connected, normalized, duplicate-free, and bounded
Each new cluster has BASE/ALT variants and exactly one baseline
Pattern slots reference only same-biome MAP21_02 patterns
Quiet/Buffer clusters remain static and have zero Activity/Event/Special/Population/reward/runtime binding
Structural signatures are unique across combined 48 records
MAP21_03 Crater/Root artifacts are read-only and not regenerated
Focused MAP21_04 EditMode tests are 12/12 PASS
All no-regression and no-execution counters are zero
Responsibility and Added Scripts table is present and specific
MAP21_05 remains LOCKED and not started
```

FAIL 조건:

```text
Cluster count is not 24 or biome/pool counts are wrong
Crater/Root records are authored in MAP21_04 files
Combined manifest does not contain exactly 48 records
Footprint is disconnected, non-normalized, duplicated, or out of bounds
Spine variant baseline count is not exactly one per cluster
Pattern slot references wrong-biome or unknown MAP21_02 pattern ids
Quiet/Buffer cluster contains Activity/Event/Special/Population/reward/runtime binding
Duplicate combined structural signature exists
MAP10, MAP21_01, MAP21_02, or MAP21_03 source is rewritten
Cluster/sector/world generation, renderer, validation runner, replay, rollback, PlayMode, legacy regression, unfiltered run, or full regression is executed
Sprite/audio/background assets are imported
Scene/Prefab/Tilemap/Addressables/ProjectSettings/Packages are changed
CSV authoring is written outside MAP21_04 authoring folder
MAP21_05 starts
```

BLOCKED 조건:

```text
Precondition SHA mismatch
Compile error unrelated to this task that prevents focused validation
MAP21_02 Mill/Dough MicroPattern pool cannot be read or source-mapped
MAP21_03 Crater/Root artifacts cannot be read without regeneration
Existing shared cluster pool schema cannot represent Mill/Dough without incompatible duplication
Authoring CSV location is unavailable or conflicts with existing authoritative MAP21 files
```

When BLOCKED:

```text
Do not finalize MAP21_04
Do not start MAP21_05
Do not publish MAP21_05 handoff digest
Record exact owner, invariant, and minimum next action
STOP
```

## 13. Finalize / Commit / STOP

PASS일 때만:

```text
MAP21_04_EXPAND_MILL_AND_DOUGH_CLUSTER_POOLS: COMPLETE
MAP21_05_PRODUCTIONIZE_ACTIVITIES_AND_EVENT_OVERLAYS: LOCKED
```

FAIL 또는 BLOCKED일 때:

```text
MAP21_04 remains CURRENT
MAP21_05 remains LOCKED
STOP
```

PASS일 때만 atomic commit을 만든다.

Commit subject:

```text
MAP21_04 expand mill dough cluster pools
```

Commit body에는 다음을 포함한다.

```text
MAP21_04 responsibilities
focused test count
new cluster count and biome/pool counts
combined all-biome cluster count
footprint/spine/pattern slot counts
static safety counts
combined structural signature counts
MAP21_05 handoff digest
legacy regression counters all zero
generation/renderer/replay/rollback/validation counters all zero
Scene/Prefab/Tilemap/Addressables changes zero
```

Git push는 하지 않는다.

Result 작성, 상태 finalize, commit 이후 반드시 멈춘다.

```text
DO NOT START MAP21_05.
DO NOT CREATE MAP21_05 FILES.
DO NOT RERUN MAP19_09 SCALE AUDIT.
DO NOT REGENERATE MAP20_01 THROUGH MAP20_06 ARTIFACTS.
DO NOT REGENERATE MAP21_01 PROFILE TILE SHELL.
DO NOT REGENERATE MAP21_02 MICROPATTERNS.
DO NOT REGENERATE MAP21_03 CRATER ROOT CLUSTERS.
DO NOT AUTHOR CRATER OR ROOT CLUSTERS IN MAP21_04.
DO NOT RUN VALIDATION RUNNER.
DO NOT EXECUTE REPLAY.
DO NOT EXECUTE GENERATOR.
DO NOT EXECUTE RENDERER.
DO NOT EXECUTE ROLLBACK.
DO NOT WRITE CSV AUTHORING FILES OUTSIDE MAP21_04 AUTHORING FOLDER.
DO NOT IMPORT SPRITE AUDIO OR BACKGROUND ASSETS.
DO NOT MUTATE SCENE PREFAB TILEMAP ADDRESSABLES OR RUNTIME OBJECTS.
DO NOT RUN LEGACY REGRESSION.
STOP.
```
