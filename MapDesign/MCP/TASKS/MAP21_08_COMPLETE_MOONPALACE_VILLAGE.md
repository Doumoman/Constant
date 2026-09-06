```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP21_08_COMPLETE_MOONPALACE_VILLAGE
  task_file: TASKS/MAP21_08_COMPLETE_MOONPALACE_VILLAGE.md
  requires_current_task: NONE
  requires_completed_task: MAP21_07_COMPLETE_THREE_CORE_RESOURCE_REGIONS
  requires_result:
    path: REPORTS/MAP21_07_COMPLETE_THREE_CORE_RESOURCE_REGIONS_RESULT.md
    status: PASS
    sha256: 68170bda01e3f2df4fbf14e9e841b04be58b3b61b1b18de9af3e5e2a3a486c8b
  requires_installed_task:
    path: TASKS/MAP21_07_COMPLETE_THREE_CORE_RESOURCE_REGIONS.md
    sha256: 4bee068adbf40ee2d55afaea0b35d6e3d635079de2ee6f23f9e0636b0fbf4321
  requires_handoff:
    name: MAP21_08 handoff digest
    sha256: 7cd8adb0e27cffad1a4187400c6e72cd47e24291663aa5fe69457096efb535e0
  sets_current_task: MAP21_08_COMPLETE_MOONPALACE_VILLAGE
```

# MAP21_08 - Complete MoonPalace Village

```text
TASK: MAP21_08_COMPLETE_MOONPALACE_VILLAGE
PHASE: MAP21 - MoonPalace Vertical Slice
STATUS: CURRENT
NEXT: MAP21_09_COMPLETE_FORGE_BOSS_AND_OPTIONAL_REGIONS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP13에서 승인된 Village shell/facility/state variant 계약을 MoonPalace vertical slice production data로 완성한다.

이번 Task는 **MoonPalace Village layout profile, road/facility/door marker, shopkeeper marker, five state variants, static manifest**만 소유한다. 실제 NPC spawn, shop inventory/price/purchase, door collider/lock/open-close, hostile combat, evacuation state machine, save/load, world placement, renderer, Tilemap, runtime object는 아직 시작하지 않는다.

| Area | This task owns | This task must not do |
|---|---|---|
| Village layouts | 1x1, 2x1, 1x2 production layout profiles | live world placement |
| Facilities | Kitchen 1 + Repair 1 + Optional 3/4 per layout | item crafting, repair logic, shop execution |
| Roads and doors | central-road cells, door markers, road-return witness | door collision, lock, animation, path blocking |
| State variants | Normal/Friendly/IndividualHostile/AllHostile/Evacuation marker snapshots | NPC AI, combat, transition runtime |
| Shopkeeper marker | one shopkeeper marker per layout with static payload identity | shop inventory, price, purchase, save |

핵심 원칙:

```text
This task completes Village production data, not Village gameplay.
MAP13 remains the source of truth for Village shell and state variant semantics.
MAP21_08 writes only MoonPalace Village authoring and generated manifests.
No generated world, Tilemap, Scene, Prefab, collider, runtime object, NPC AI, shop transaction, door collision, or save I/O is created.
```

## 1. 사용자 보고 의무

Result 첫 부분에는 반드시 아래 두 섹션을 둔다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

`User-Facing Implementation Report`에는 아래 질문에 답한다.

```text
이번 작업이 MoonPalace Village를 production 데이터로 어떻게 완성했는가?
1x1/2x1/1x2 layout의 bounds, road cells, facility count, seam evidence는 무엇인가?
Kitchen/Repair fixed facility와 Optional facility는 어떻게 구분되는가?
shopkeeper marker는 무엇을 의미하고 무엇을 실행하지 않는가?
Normal/Friendly/IndividualHostile/AllHostile/Evacuation variant는 어떤 marker 상태를 보존하는가?
MAP13_04/05/09와 MAP18 special export를 어떻게 읽기 전용으로 연결했는가?
이번 작업이 NPC spawn, shop, door collision, combat, save/load, Tilemap/Scene/Prefab/runtime을 만들지 않았다는 증거는 무엇인가?
이번 Task에서 추가한 script/CSV/JSON과 각 책임은 무엇인가?
legacy regression과 prior-category rerun을 하지 않았다는 증거는 무엇인가?
MAP21_09는 왜 아직 시작하지 않았는가?
```

`Responsibility and Added Scripts`는 반드시 표로 작성한다.

```text
| Script or file | Added or changed responsibility | Explicit non-ownership |
```

파일별 책임이 구체적이지 않거나, 사용자가 “그래서 뭘 했는지” 알 수 없으면 FAIL이다.

## 2. 선행조건

작업 시작 전에 다음을 확인한다.

```text
MapDesign/MCP/REPORTS/MAP21_07_COMPLETE_THREE_CORE_RESOURCE_REGIONS_RESULT.md exists
MAP21_07 Result STATUS: PASS
MAP21_07 Result SHA-256:
68170bda01e3f2df4fbf14e9e841b04be58b3b61b1b18de9af3e5e2a3a486c8b

MapDesign/MCP/TASKS/MAP21_07_COMPLETE_THREE_CORE_RESOURCE_REGIONS.md exists
MAP21_07 installed Task SHA-256:
4bee068adbf40ee2d55afaea0b35d6e3d635079de2ee6f23f9e0636b0fbf4321

MAP21_08 handoff digest:
7cd8adb0e27cffad1a4187400c6e72cd47e24291663aa5fe69457096efb535e0

Historical MAP13/MAP18 source Result verification policy:

For MAP13_04, MAP13_05, MAP13_09, and MAP18_06 Result files:
- require file exists
- require TASK line matches the expected task id
- require STATUS: PASS
- compute and record the observed file SHA-256
- do not fail only because the observed byte SHA differs from the authoring note

Reason:
These older source Results may differ by local finalization, line ending, or report note edits while preserving the approved TASK/STATUS contract. MAP21_08 must not rewrite or regenerate them.

The observed source Result SHAs must be copied into the MAP21_08 digest manifest and final Result.

Immediate predecessor gate remains strict:
- MAP21_07 Result SHA-256 must equal 68170bda01e3f2df4fbf14e9e841b04be58b3b61b1b18de9af3e5e2a3a486c8b
- MAP21_07 installed Task SHA-256 must equal 4bee068adbf40ee2d55afaea0b35d6e3d635079de2ee6f23f9e0636b0fbf4321
- MAP21_08 handoff digest must equal 7cd8adb0e27cffad1a4187400c6e72cd47e24291663aa5fe69457096efb535e0

Historical source Result identities:
- `MapDesign/MCP/REPORTS/MAP13_04_IMPLEMENT_VILLAGE_SHELL_FACILITIES_AND_ACCESS_RESULT.md`
  - required TASK: `MAP13_04_IMPLEMENT_VILLAGE_SHELL_FACILITIES_AND_ACCESS`
  - required STATUS: `PASS`
- `MapDesign/MCP/REPORTS/MAP13_05_IMPLEMENT_VILLAGE_STATE_VARIANTS_RESULT.md`
  - required TASK: `MAP13_05_IMPLEMENT_VILLAGE_STATE_VARIANTS`
  - required STATUS: `PASS`
- `MapDesign/MCP/REPORTS/MAP13_09_MAP13_SPECIAL_REGION_EXIT_TESTS_RESULT.md`
  - required TASK: `MAP13_09_MAP13_SPECIAL_REGION_EXIT_TESTS`
  - required STATUS: `PASS`
- `MapDesign/MCP/REPORTS/MAP18_06_IMPLEMENT_SPECIAL_STATE_EXPORT_AND_DEBUG_RESULT.md`
  - required TASK: `MAP18_06_IMPLEMENT_SPECIAL_STATE_EXPORT_AND_DEBUG`
  - required STATUS: `PASS`

MAP13 public audit digest:
a7ab6fd571425c4c8e64d7eecad5dd246a3d9a8a08044801800948fc2fa03e4e

MAP18 special export surface digest:
358ac8cfe78eec502db049f8940ed0c71458179b89bb451680e837b0797b77b5
MAP18 debug snapshot digest:
59efb7fd30df9ec62014cadd04a111b222e7dd13e298789dbab88a661bea22ed

MAP21 source digests:
MAP21_04 all-biome cluster manifest: d30baf04fcd6eceeabd7b6f467b24ce11ce7c5f44833887bf7b0457e6fbdda72
MAP21_05 activity/event digest manifest: 645f95b8c58757179223b8e5b219b60b5a732cb898a69f99594178c76f166a1e
MAP21_06 boundary digest manifest: 955ad4acb3e2294b3dc7c018c21472bf4ef34bbfc4075685324901179bd652ab
MAP21_07 core resource digest manifest: 0e03c7cb4780b078822297e9877d345480413a2b3b8d9ed0c3d1d455645a4a93

MAP21_09_COMPLETE_FORGE_BOSS_AND_OPTIONAL_REGIONS: LOCKED
```

불일치 시:

```text
STATUS: BLOCKED
reason: precondition mismatch
created/changed code files: 0
MAP21_09 started: NO
STOP
```

## 3. 허용 범위

허용 파일:

```text
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/MoonPalaceVillageProduction.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/MoonPalace/MoonPalaceVillagePublisher.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/MoonPalace/MoonPalaceVillageProductionTests.cs
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_08/**
MapDesign/MCP/GENERATED/MAP21_08/**
MapDesign/MCP/TASKS/MAP21_08_COMPLETE_MOONPALACE_VILLAGE.md
MapDesign/MCP/REPORTS/MAP21_08_COMPLETE_MOONPALACE_VILLAGE_RESULT.md
MapDesign/MCP_ARCHIVE/MAP21_08_COMPLETE_MOONPALACE_VILLAGE.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

조건부 허용:

```text
Existing MAP13 Village shell/state public contracts may be referenced read-only.
Existing MAP13_09 preview/audit public output may be referenced read-only.
Existing MAP18_06 special export public types may be referenced read-only for Village absent/declared export compatibility.
Existing MAP21_04/05/06/07 manifests may be read and hashed only.
Authoring CSV writes are allowed only under Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_08.
Generated JSON writes are allowed only under MapDesign/MCP/GENERATED/MAP21_08.
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
MAP21_07 CoreResource rewrite or regeneration
Forge/Boss/Merchant/Maru production work
Village placement into sector/world
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
NPC spawn, AI, faction, patrol, dialogue, shopkeeper controller, or combat hookup
shop inventory, price, purchase, sell, repair, kitchen, crafting, or stock logic
door collider, lock, open-close, animation, navigation, or path blocking
hostile/evacuation runtime state machine execution
Reward grant
inventory/resource mutation
Collider generation
Scene / Prefab / ProjectSettings / Packages changes
sprite/audio/background asset import
Addressables group/build change
runtime GameObject instantiate/enable/disable/destroy during tooling actions
CSV authoring edit outside MAP21_08 authoring folder
auto-fix / auto-repair
production seed approval
external process launch
manual gameplay test
automatic MAP21_09 start
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
MAP21_07 CoreResource regeneration
legacy 19347 regression
prior task category selection
PlayMode selection
unfiltered test run
full regression run
```

## 4. Village Layout 계약

MAP21_08 must preserve the three MAP13 Village shell shapes and exact state variant vocabulary.

Required production layout inventory:

| Layout ID | Shape | Bounds | Road cells | Facility count | Fixed facilities | Optional facilities | Seam evidence |
|---|---|---:|---:|---:|---:|---:|---|
| `VLG_MOONPALACE_1X1_OVERVIEW` | `1x1` | `48x32` | 48 | 5 | 2 | 3 | none |
| `VLG_MOONPALACE_2X1_MARKET` | `2x1` | `96x32` | 96 | 6 | 2 | 4 | x=47/48 cardinal pair 1 |
| `VLG_MOONPALACE_1X2_ASCENT` | `1x2` | `48x64` | 64 | 5 | 2 | 3 | y=31/32 cardinal pair 1 |

Required profile fields:

```text
village_layout_id
schema_version
source_map13_shape
bounds_width
bounds_height
active_sector_count
road_cell_count
facility_count
fixed_facility_count
optional_facility_count
door_marker_count
npc_marker_count
inventory_marker_count
shopkeeper_marker_count
state_variant_count
source_map13_audit_digest
source_map18_export_digest
facility_digest
road_digest
door_digest
state_digest
canonical_digest
```

Rules:

```text
There must be exactly 3 Village layout profiles.
Every layout has Kitchen fixed facility exactly 1 and Repair fixed facility exactly 1.
1x1 and 1x2 layouts have exactly 3 Optional facilities.
2x1 layout has exactly 4 Optional facilities.
Every facility has exactly one door marker and one road-return witness.
Every layout has exactly 3 NPC markers.
Every layout has exactly 2 inventory markers.
Every layout has exactly 1 shopkeeper marker.
Every layout has exactly 5 state variants.
Village remains optional/reference-local and must not become a mandatory progression blocker.
Village must not introduce required reward dependency, CoreResource dependency, Forge/Boss dependency, or completion blocker.
```

## 5. Facility, Marker, and State 계약

Required facility kinds:

```text
Kitchen
Repair
OptionalRest
OptionalStorage
OptionalMarket
OptionalLore
```

`OptionalLore` is required only for the 2x1 layout so that it has 6 facilities.

Required state variants:

```text
Normal
Friendly
IndividualHostile
AllHostile
Evacuation
```

Required marker matrix per layout:

| Variant | NPC marker states | Inventory marker states | Door marker states |
|---|---|---|---|
| `Normal` | Normal 3 | Standard 2 | Standard 5 or 6 |
| `Friendly` | Friendly 3 | FriendlyAccess 2 | Welcome 5 or 6 |
| `IndividualHostile` | exact target 1 Hostile + Normal 2 | Standard 2 | Standard 5 or 6 |
| `AllHostile` | Hostile 3 | Unavailable 2 | Alert 5 or 6 |
| `Evacuation` | Evacuated 3 | Evacuated 2 | Evacuated 5 or 6 |

Rules:

```text
State variants are marker snapshots only.
Normal/Friendly/Hostile/Evacuation do not change road cells, facility coordinates, door coordinates, access witnesses, collision, persistence, or final ownership.
IndividualHostile must target exactly one NPC marker per layout.
Shopkeeper marker is static authoring payload only and must not create a shopkeeper GameObject, shop inventory, price, purchase, repair, crafting, dialogue, or save data.
Door states are presentation markers only and must not lock/unlock/open/close/collide or path-block.
Inventory markers are static availability markers only and must not mutate player inventory or stock.
```

## 6. Required Authoring Files

Create exactly these MAP21_08 authoring CSVs unless a narrower equivalent is strongly justified in the Result.

```text
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_08/moonpalace_village_profiles.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_08/moonpalace_village_facilities.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_08/moonpalace_village_roads.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_08/moonpalace_village_doors.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_08/moonpalace_village_markers.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_08/moonpalace_village_state_variants.csv
```

Authoring rules:

```text
CSV must use UTF-8 without BOM, LF, one final LF, stable header order, stable row order.
Coordinates are region-local Village coordinates only.
No generated world coordinates may be stored.
No file outside MAP21_08 authoring may be written.
```

## 7. Required Generated Artifacts

Create exactly these generated JSON artifacts.

```text
MapDesign/MCP/GENERATED/MAP21_08/moonpalace_village_manifest.json
MapDesign/MCP/GENERATED/MAP21_08/moonpalace_village_access_manifest.json
MapDesign/MCP/GENERATED/MAP21_08/moonpalace_village_state_manifest.json
MapDesign/MCP/GENERATED/MAP21_08/moonpalace_village_digest_manifest.json
```

Generated artifact rules:

```text
JSON must be deterministic and canonical enough to hash repeatedly.
created_utc may exist only if excluded from canonical digest.
The digest manifest must include MAP13 Village and audit digests, MAP18 export/debug digests, MAP21_04/05/06/07 source digests, every MAP21_08 CSV digest, every MAP21_08 JSON digest, and MAP21_09 handoff digest.
The generated artifacts are static production samples, not runtime save files, generated worlds, or live Village objects.
```

## 8. Required Focused Tests

Add exactly one focused test file:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/MoonPalace/MoonPalaceVillageProductionTests.cs
```

Required test category:

```text
MAP21_08
```

Required focused test cases:

```text
MoonPalaceVillageContainsExactThreeLayoutsAndApprovedShapes
MoonPalaceVillagePublishesFacilityCountsKitchenRepairAndOptionalSlots
MoonPalaceVillageRoadsDoorsAndReturnWitnessesPreserveShellAccessAndSeams
MoonPalaceVillagePublishesExactFiveStateVariantsPerLayout
MoonPalaceVillageMarkersPreserveNpcInventoryDoorStateMatrix
MoonPalaceVillageShopkeeperIsStaticMarkerOnlyAndDoesNotCreateShopRuntime
MoonPalaceVillageIndividualHostileTargetsExactlyOneNpcPerLayout
MoonPalaceVillageHostileAndEvacuationVariantsDoNotMutateRoadDoorCollisionOrAccess
MoonPalaceVillageRemainsOptionalReferenceLocalWithoutProgressionOrRewardDependency
MoonPalaceVillagePublisherReadsMap13Map18AndMap21SourcesWithoutRewriting
MoonPalaceVillagePublisherWritesOnlyMap21_08AuthoringAndGeneratedRoots
MoonPalaceVillageDigestsAreDeterministicReverseOrderRepeatAndCultureStable
MoonPalaceVillageRejectsDuplicateLayoutsBadFacilitiesMissingVariantsBadDoorsAndRuntimeSideEffects
MoonPalaceVillageDoesNotRunGenerationRendererValidationReplayRollbackPlayModeOrLegacyRegression
MoonPalaceVillagePublishesMap21_09HandoffOnlyAfterFocusedPass
```

Only run:

```text
EditMode category MAP21_08
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

Do not run MAP13/MAP18 categories, PlayMode, or any previous task category.

## 9. Result Required Evidence

The Result must include these sections.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
## Village Layout and Facility Summary
## Road Door and Access Witness Summary
## State Variant and Marker Summary
## Static Safety and Non-Execution Summary
## Snapshot and Digest Summary
## Focused Validation Summary
## No Legacy Regression Boundary Notes
## Final Status Evidence
```

Minimum required evidence:

```text
Village layout profile records: 3
Layout IDs: VLG_MOONPALACE_1X1_OVERVIEW, VLG_MOONPALACE_2X1_MARKET, VLG_MOONPALACE_1X2_ASCENT
layout shapes: 1x1 / 2x1 / 1x2
bounds: 48x32 / 96x32 / 48x64
road cells: 48 / 96 / 64
facility records: 16
fixed facilities: Kitchen 3 / Repair 3
optional facilities: 10
facility count per layout: 5 / 6 / 5
door markers: 16
road-return witnesses: 16 / 16
NPC markers: 9
inventory markers: 6
shopkeeper markers: 3
state variant records: 15
five variants per layout: 5 / 5 / 5
IndividualHostile exact target count: 3 / 3
AllHostile variant count: 3
Evacuation variant count: 3
Village progression blocker count: 0
Village required reward dependency count: 0
door collision/lock/path-block writes: 0
shop inventory/price/purchase writes: 0
NPC spawn/AI/combat executions: 0
MAP13 source modifications: 0
MAP18 source modifications: 0
MAP21_04/05/06/07 source modifications: 0
generated world count: 0
Tilemap writes: 0
runtime GameObject spawns: 0
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
MAP21_07 CORE RESOURCE REGENERATION RUNS: 0
VALIDATION RUNNER EXECUTIONS: 0
REPLAY EXECUTIONS: 0
GENERATOR EXECUTIONS: 0
RENDERER EXECUTIONS: 0
ROLLBACK EXECUTIONS: 0
CSV AUTHORING WRITES OUTSIDE MAP21_08 AUTHORING FOLDER: 0
RUNTIME OBJECT SPAWNS: 0
SCENE PREFAB CHANGES: 0
SAVE FILE WRITES/READS: 0 / 0
NPC SPAWNS: 0
SHOP TRANSACTIONS: 0
DOOR COLLISION OR LOCK WRITES: 0
```

If a real compile or focused test failure occurs, only the MAP21_08 focused category may be rerun after a local fix. Do not broaden test selection unless the Result explicitly reports a real regression trigger and stops for review.

## 11. Finalize and Commit Rules

Finalize only if all MAP21_08 checks pass.

Status expectations before finalize:

```text
MAP21_08_COMPLETE_MOONPALACE_VILLAGE: CURRENT
MAP21_09_COMPLETE_FORGE_BOSS_AND_OPTIONAL_REGIONS: LOCKED
```

Status expectations after finalize:

```text
MAP21_08_COMPLETE_MOONPALACE_VILLAGE: COMPLETE
Current Task: NONE
MAP21_09_COMPLETE_FORGE_BOSS_AND_OPTIONAL_REGIONS: LOCKED
```

The Result must state whether Status Finalize and atomic commit were actually performed. If the local protocol intentionally writes Result before finalize, say so explicitly.

Atomic commit scope:

```text
MAP21_08 task file/archive
MAP21_08 Result
MAP21_08 authoring CSV/meta files
MAP21_08 generated JSON files
MoonPalaceVillageProduction.cs and .meta if new
MoonPalaceVillagePublisher.cs and .meta if new
MoonPalaceVillageProductionTests.cs and .meta if new
06_IMPLEMENTATION_STATUS.md finalize change
```

Commit subject:

```text
MAP21_08 complete moonpalace village
```

Do not stage unrelated files. Do not push.

## 12. Stop Rule

After Result and commit:

```text
STOP
Do not create MAP21_09 files.
Do not run MAP21_09.
Do not rerun MAP13 or MAP18 categories.
Do not run world generation, renderer, validation runner, replay, rollback, PlayMode, legacy regression, or full regression.
```
