```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP21_09_COMPLETE_FORGE_BOSS_AND_OPTIONAL_REGIONS
  task_file: TASKS/MAP21_09_COMPLETE_FORGE_BOSS_AND_OPTIONAL_REGIONS.md
  requires_current_task: NONE
  requires_completed_task: MAP21_08_COMPLETE_MOONPALACE_VILLAGE
  requires_result:
    path: REPORTS/MAP21_08_COMPLETE_MOONPALACE_VILLAGE_RESULT.md
    status: PASS
    sha256: 7e5e7cfe0bb4e82ed7da405d11a1a05fe06e475208268a9e432840ca43b9d7d8
  requires_installed_task:
    path: TASKS/MAP21_08_COMPLETE_MOONPALACE_VILLAGE.md
    sha256: e3b149d0ddfac0138688e7284aad0afb8a8118abcb056f52f2dd91bf6e863e7b
  requires_handoff:
    name: MAP21_09 handoff digest
    sha256: 36e124fe4f63258eeca5b5097d45a71e80587efaa1fbb50fb6eeee7e36ef224b
  sets_current_task: MAP21_09_COMPLETE_FORGE_BOSS_AND_OPTIONAL_REGIONS
```

# MAP21_09 - Complete Forge, Boss, and Optional Regions

```text
TASK: MAP21_09_COMPLETE_FORGE_BOSS_AND_OPTIONAL_REGIONS
PHASE: MAP21 - MoonPalace Vertical Slice
STATUS: CURRENT
NEXT: MAP21_10_TUNE_REPETITION_DENSITY_AND_PACING
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP13에서 승인된 MoonSealForge, BossSealArena, WanderingMerchantCave, MaruTimeShrine 계약을 MoonPalace vertical slice production **정적 데이터**로 완성한다.

이번 Task는 네 landmark의 production profile, design-local graph projection, Forge resource ledger, MoonSeal output marker, Boss gate/encounter state marker, Merchant/Maru optional local manifests만 소유한다.

실제 아이템 소비, 보상 지급, 보스 AI/전투, 상점 가격/구매, Maru AI/attention, 세이브, 월드 배치, Tilemap/Scene/Prefab/Collider/runtime object는 아직 시작하지 않는다.

| Area | This task owns | This task must not do |
|---|---|---|
| Landmark inventory | Forge, Boss, Merchant, Maru production profiles | new SpecialRegion type system |
| Forge | 3 resource ledger, Grind/Mix/Press/MoonlightCure process, MoonSeal static reward marker | inventory consume, craft execution, reward grant |
| Boss | MoonSeal gate marker, encounter state order, fall/pressure recovery proof | Boss AI, combat, HP, damage, physics |
| Optional regions | Merchant 4 variants, Maru 3 choices, deferred-local shells | shop runtime, Maru AI, rare search execution |
| Data publication | MAP21_09 CSV/JSON and deterministic digests | generated world, Tilemap, renderer, validation runner |

Core principle:

```text
This task completes landmark production data, not landmark gameplay.
MAP13 remains the source of truth for landmark shell/route/state/reset semantics.
MAP21_09 writes only MoonPalace landmark authoring and generated manifests.
No generated world, Tilemap, Scene, Prefab, collider, runtime object, item consume, reward grant, Boss AI, shop transaction, Maru runtime, or save I/O is created.
```

## 1. 사용자 보고 의무

Result 첫 부분에는 반드시 아래 두 섹션을 둔다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

`User-Facing Implementation Report`에는 아래 질문에 답한다.

```text
이번 작업이 Forge/Boss/Merchant/Maru를 production 데이터로 어떻게 완성했는가?
MoonCore/CassiaSap/StarNuruk reward key가 Forge ledger에 어떻게 연결되는가?
Forge가 MoonSeal output marker를 어떻게 게시하고, 실제 inventory consume/reward grant를 하지 않았다는 증거는 무엇인가?
Boss gate가 MoonSeal requirement marker를 어떻게 참조하고, Seal을 소비하지 않는다는 증거는 무엇인가?
Boss encounter/failure/recovery는 어떤 static state marker로 남고, AI/combat/physics를 만들지 않았다는 증거는 무엇인가?
Merchant와 Maru는 왜 optional deferred-local이고, world placement claim이 0이라는 증거는 무엇인가?
이번 Task에서 추가한 script/CSV/JSON과 각 책임은 무엇인가?
legacy regression과 prior-category rerun을 하지 않았다는 증거는 무엇인가?
MAP21_10은 왜 아직 시작하지 않았는가?
```

`Responsibility and Added Scripts`는 반드시 표로 작성한다.

```text
| Script or file | Added or changed responsibility | Explicit non-ownership |
```

파일별 책임이 구체적이지 않거나, 사용자가 “그래서 뭘 했는지” 알 수 없으면 FAIL이다.

## 2. 선행조건

Immediate predecessor gate는 strict byte SHA로 확인한다.

```text
MapDesign/MCP/REPORTS/MAP21_08_COMPLETE_MOONPALACE_VILLAGE_RESULT.md exists
MAP21_08 Result STATUS: PASS
MAP21_08 Result SHA-256:
7e5e7cfe0bb4e82ed7da405d11a1a05fe06e475208268a9e432840ca43b9d7d8

MapDesign/MCP/TASKS/MAP21_08_COMPLETE_MOONPALACE_VILLAGE.md exists
MAP21_08 installed Task SHA-256:
e3b149d0ddfac0138688e7284aad0afb8a8118abcb056f52f2dd91bf6e863e7b

MAP21_09 handoff digest:
36e124fe4f63258eeca5b5097d45a71e80587efaa1fbb50fb6eeee7e36ef224b

MAP21_10_TUNE_REPETITION_DENSITY_AND_PACING: LOCKED
```

Historical source Result verification policy:

For MAP13_07, MAP13_08, MAP13_09, MAP18_06, MAP21_07, and MAP21_08 source Result files:

```text
require file exists
require TASK line matches the expected task id
require STATUS: PASS
compute and record observed file SHA-256
do not fail only because older historical Result byte SHA differs from an authoring note
```

Reason:

```text
Older source Results may differ by local finalization, line ending, or report note edits while preserving the approved TASK/STATUS contract.
The immediate predecessor MAP21_08 remains strict.
All observed source SHAs must be copied into MAP21_09 digest manifest and Result.
No source artifact may be rewritten or regenerated.
```

Expected source semantic anchors to verify read-only:

```text
MAP13_07 has exact four landmarks:
MoonSealForge, BossSealArena, WanderingMerchantCave, MaruTimeShrine

MAP13_07 landmark counts:
MoonSealForge chunks/nodes/edges/routes/states/transitions/resets/markers = 9/13/22/6/14/9/3/12
BossSealArena chunks/nodes/edges/routes/states/transitions/resets/markers = 12/12/16/5/4/4/3/7
WanderingMerchantCave chunks/nodes/edges/routes/states/transitions/resets/markers = 3/7/8/3/3/2/1/7
MaruTimeShrine chunks/nodes/edges/routes/states/transitions/resets/markers = 5/7/12/4/4/3/2/5

MAP13_09 public audit digest:
a7ab6fd571425c4c8e64d7eecad5dd246a3d9a8a08044801800948fc2fa03e4e

MAP18_06 special export surface digest:
358ac8cfe78eec502db049f8940ed0c71458179b89bb451680e837b0797b77b5
MAP18_06 debug snapshot digest:
59efb7fd30df9ec62014cadd04a111b222e7dd13e298789dbab88a661bea22ed

MAP21_07 CoreResource digest:
0e03c7cb4780b078822297e9877d345480413a2b3b8d9ed0c3d1d455645a4a93
MAP21_08 Village digest:
9308092afbee03a8c08d5a5fbc2b4eb4a02fc747e5bbc6bd9afe7ce5ec018a73
```

If any required source file is missing, TASK/STATUS is wrong, semantic anchor is absent, or MAP21_08 strict gate fails:

```text
STATUS: BLOCKED
reason: precondition mismatch
created/changed production code files: 0
MAP21_10 started: NO
STOP
```

## 3. 허용 범위

허용 파일:

```text
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/MoonPalaceLandmarkProduction.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/MoonPalace/MoonPalaceLandmarkPublisher.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/MoonPalace/MoonPalaceLandmarkProductionTests.cs
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_09/**
MapDesign/MCP/GENERATED/MAP21_09/**
MapDesign/MCP/TASKS/MAP21_09_COMPLETE_FORGE_BOSS_AND_OPTIONAL_REGIONS.md
MapDesign/MCP/REPORTS/MAP21_09_COMPLETE_FORGE_BOSS_AND_OPTIONAL_REGIONS_RESULT.md
MapDesign/MCP_ARCHIVE/MAP21_09_COMPLETE_FORGE_BOSS_AND_OPTIONAL_REGIONS.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

조건부 허용:

```text
Existing MAP13 landmark public contracts may be referenced read-only.
Existing MAP13_08/09 audit public output may be referenced read-only.
Existing MAP18_06 special export surface may be referenced read-only.
Existing MAP21_07 CoreResource and MAP21_08 Village generated manifests may be read and hashed only.
Authoring CSV writes are allowed only under Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_09.
Generated JSON writes are allowed only under MapDesign/MCP/GENERATED/MAP21_09.
```

금지:

```text
MAP13 source C#, test, CSV, generated artifact rewrite
MAP18 source C#, test, CSV, generated artifact rewrite
MAP21_01~08 source rewrite or regeneration
MAP21_10 start
world/sector generation or placement
Tilemap bake or Tilemap mutation
pattern/cluster/sector renderer execution or behavior change
validation runner execution
actual replay execution
rollback execution
save/runtime state generation
runtime save/load file write or read
PlayerPrefs write or read
Reward grant
inventory/resource mutation
MoonCore/CassiaSap/StarNuruk reward state mutation
MoonSeal inventory item creation, consume, remove, grant, or save
Boss AI, combat, HP, damage, attack, hitbox, falling object physics, pressure device physics
Boss encounter runtime state machine execution
Merchant NPC spawn, shopkeeper controller, shop inventory, stock, price, purchase, sell, repair, kitchen, crafting, dialogue runtime
Maru NPC spawn, AI, attention runtime, hint search, rare-terrain search execution, reroll execution
door collider, lock, open-close, animation, navigation, or path blocking
Collider generation
Scene / Prefab / ProjectSettings / Packages changes
sprite/audio/background asset import
Addressables group/build change
runtime GameObject instantiate/enable/disable/destroy during tooling actions
CSV authoring edit outside MAP21_09 authoring folder
auto-fix / auto-repair of upstream source
production seed approval
external process launch
manual gameplay test
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
MAP21_08 Village regeneration
legacy 19347 regression
prior task category selection
PlayMode selection
unfiltered test run
full regression run
```

## 4. Landmark Production 계약

Required landmark profiles:

| Landmark ID | Binding | Chunks | Nodes | Edges | Routes | States | Transitions | Resets | Markers |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| `MoonSealForge` | `PlacedMandatorySite` | 9 | 13 | 22 | 6 | 14 | 9 | 3 | 12 |
| `BossSealArena` | `PlacedMandatorySite` | 12 | 12 | 16 | 5 | 4 | 4 | 3 | 7 |
| `WanderingMerchantCave` | `DeferredOptionalLocal` | 3 | 7 | 8 | 3 | 3 | 2 | 1 | 7 |
| `MaruTimeShrine` | `DeferredOptionalLocal` | 5 | 7 | 12 | 4 | 4 | 3 | 2 | 5 |

Aggregate expected counts:

```text
landmark profile records: 4
placed mandatory records: 2
deferred optional records: 2
design chunk records: 29
node records: 39
edge records: 58
route records: 18
state records: 25
transition records: 18
reset records: 9
marker records: 31
world placement claims for optional landmarks: 0
```

Rules:

```text
Forge and Boss must preserve placed mandatory identity.
Merchant and Maru must preserve DeferredOptionalLocal identity.
Merchant and Maru must not claim footprint, world origin, reservation, bridge, fixed slot, or placed ownership.
All coordinates are design-local landmark coordinates only.
No generated world coordinate may be stored.
No state may imply runtime execution.
```

## 5. Forge 계약

Forge process order:

```text
Grind -> Mix -> Press -> MoonlightCure -> MoonSeal
```

Required source resources from MAP21_07:

```text
MoonCore reward key:
SR_STATE_MOON_CORE_SITE_5_REWARD_MOON_CORE_REWARD

CassiaSap reward key:
SR_STATE_CASSIA_SAP_SITE_5_REWARD_CASSIA_SAP_REWARD

StarNuruk reward key:
SR_STATE_STAR_NURUK_SITE_5_REWARD_STAR_NURUK_REWARD
```

Required Forge output:

```text
MoonSeal reward slot: SR_SLOT_MOON_SEAL_REWARD
MoonSeal reward key: SR_STATE_MOON_SEAL_FORGE_9_REWARD_MOON_SEAL_REWARD
MoonSeal amount: 1
MoonSeal required: true
```

Required ledger:

```text
resource ledger records: 3
ledger states: 12
ledger transitions: 9
success transition per resource: Available -> Reserved -> Consumed
failure transition per resource: Reserved -> Returned
failure branches: Grind / Mix / Press
manual reset count: 3
ReturnsAllForgeInputs: true for every failure branch
partial loss count: 0
permanent loss count: 0
inventory consume executions: 0
reward grant executions: 0
save writes/reads: 0 / 0
```

Forge may publish static markers for:

```text
TimingOptimization
MaruAttentionReduction
BossDirection
MoonSealAvailable
```

These are authoring markers only.

## 6. Boss 계약

Boss gate state order:

```text
GateLocked -> GateAccepted -> EncounterActive -> Defeated
```

Required Boss proof:

```text
Boss state records: 4
Boss transition records: 4
MoonSeal requirement marker: 1
MoonSeal consumed count: 0
inventory mutation count: 0
Low route: lower observation -> central recovery -> arena return
High route: upper platform -> falling-object marker -> pressure-device marker -> same arena return
fall/pressure failure recoveries: 2
central recovery target: SL_NODE_BOSS_CENTRAL_RECOVERY
EncounterReset: EncounterActive -> EncounterActive
PreservesSealAcceptance: true
IntroducesNewMovementRule: false
Boss AI/combat/physics/reset executions: 0
duplicate benefit/reward risk: 0
```

Falling object and pressure device entries are static design markers only. They must not create physics bodies, hitboxes, runtime damage, or new movement rules.

## 7. Optional Landmark 계약

Merchant:

```text
landmark id: WanderingMerchantCave
binding: DeferredOptionalLocal
local canvas: 24x16
ShopSafeZone count: 1
EntranceCue count: 2
routes: Low / High / Return
merchant variants: Alien / Rabbit / Spacefarer / Machine
variant count: 4
RNG selection executions: 0
mandatory dependency count: 0
shop inventory/price/purchase writes: 0
NPC spawn/controller/dialogue executions: 0
```

Maru:

```text
landmark id: MaruTimeShrine
binding: DeferredOptionalLocal
ChoicePreview order: 0
choice transitions: Ignored / ShortHint / StrongHint
StrongHint markers: RareTerrainCompass + MaruAttentionIncrease
failure recovery: SafeZone
PersistentChoice: true
PreventsReroll: true
revisit reroll count: 0
duplicate benefit count: 0
Maru AI/attention runtime executions: 0
hint search executions: 0
rare terrain search executions: 0
```

Optional common:

```text
footprint claim: 0
world origin claim: 0
reservation claim: 0
bridge claim: 0
fixed-slot claim: 0
placed ownership claim: 0
progression blocker count: 0
required reward dependency count: 0
```

## 8. Required Authoring Files

Create exactly these MAP21_09 authoring CSVs unless a narrower equivalent is strongly justified in the Result.

```text
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_09/moonpalace_landmark_profiles.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_09/moonpalace_landmark_chunks.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_09/moonpalace_landmark_nodes.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_09/moonpalace_landmark_edges.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_09/moonpalace_landmark_routes.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_09/moonpalace_forge_resource_ledger.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_09/moonpalace_boss_gate_encounter.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_09/moonpalace_optional_landmarks.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_09/moonpalace_landmark_markers.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_09/moonpalace_landmark_state_persistence.csv
```

Authoring rules:

```text
CSV must use UTF-8 without BOM, LF, one final LF, stable header order, stable row order.
Coordinates are design-local landmark coordinates only.
No generated world coordinates may be stored.
No file outside MAP21_09 authoring may be written.
```

## 9. Required Generated Artifacts

Create exactly these generated JSON artifacts.

```text
MapDesign/MCP/GENERATED/MAP21_09/moonpalace_landmark_manifest.json
MapDesign/MCP/GENERATED/MAP21_09/moonpalace_forge_manifest.json
MapDesign/MCP/GENERATED/MAP21_09/moonpalace_boss_manifest.json
MapDesign/MCP/GENERATED/MAP21_09/moonpalace_optional_landmarks_manifest.json
MapDesign/MCP/GENERATED/MAP21_09/moonpalace_landmark_digest_manifest.json
```

Generated artifact rules:

```text
JSON must be deterministic and canonical enough to hash repeatedly.
created_utc may exist only if excluded from canonical digest.
The digest manifest must include observed MAP13/MAP18/MAP21_07/MAP21_08 source Result SHAs, MAP13 audit digest, MAP18 export/debug digests, MAP21_07 CoreResource digest, MAP21_08 Village digest, every MAP21_09 CSV digest, every MAP21_09 JSON digest, and MAP21_10 handoff digest.
Generated artifacts are static production samples, not runtime save files, generated worlds, or live landmark objects.
```

## 10. Required Focused Tests

Add exactly one focused test file:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/MoonPalace/MoonPalaceLandmarkProductionTests.cs
```

Required test category:

```text
MAP21_09
```

Required focused test cases:

```text
MoonPalaceLandmarksContainExactForgeBossMerchantMaruProfiles
MoonPalaceLandmarksPreserveMap13CountsBindingsAndDesignLocalCoordinates
MoonPalaceForgePublishesThreeResourceLedgerAndMoonSealReward
MoonPalaceForgeFailureReturnsAllInputsWithoutInventoryOrSaveMutation
MoonPalaceBossGateAcceptsMoonSealMarkerWithoutConsumingInventory
MoonPalaceBossPublishesEncounterRecoveryAndNoNewMovementRule
MoonPalaceMerchantRemainsDeferredLocalWithFourStaticVariants
MoonPalaceMaruPublishesPersistentChoiceWithoutRerollOrRuntimeSearch
MoonPalaceLandmarkPublisherReadsSourcesWithoutRewriting
MoonPalaceLandmarkPublisherWritesOnlyMap21_09AuthoringAndGeneratedRoots
MoonPalaceLandmarkDigestsAreDeterministicReverseOrderRepeatAndCultureStable
MoonPalaceLandmarksRejectBadResourceKeysGateOrderOptionalClaimsAndRuntimeSideEffects
MoonPalaceLandmarksDoNotRunGenerationRendererValidationReplayRollbackPlayModeOrLegacyRegression
MoonPalaceLandmarksPublishMap21_10HandoffOnlyAfterFocusedPass
```

Only run:

```text
EditMode category MAP21_09
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

Do not run MAP13/MAP18/MAP21_01~08 categories, PlayMode, or any previous task category.

## 11. Result Required Evidence

The Result must include these sections.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
## Landmark Profile and Binding Summary
## Forge Ledger and MoonSeal Summary
## Boss Gate and Encounter Summary
## Optional Merchant and Maru Summary
## Static Safety and Non-Execution Summary
## Snapshot and Digest Summary
## Focused Validation Summary
## No Legacy Regression Boundary Notes
## Final Status Evidence
```

Minimum required evidence:

```text
landmark profile records: 4
landmark IDs: MoonSealForge, BossSealArena, WanderingMerchantCave, MaruTimeShrine
placed mandatory records: 2
deferred optional records: 2
design chunk records: 29
node records: 39
edge records: 58
route records: 18
state records: 25
transition records: 18
reset records: 9
marker records: 31
Forge process stages: Grind / Mix / Press / MoonlightCure / MoonSeal
Forge source resources: MoonCore / CassiaSap / StarNuruk
Forge resource ledger records: 3
Forge ledger states/transitions: 12 / 9
Forge manual reset count: 3
Forge ReturnsAllForgeInputs: 3 / 3
Forge partial/permanent loss count: 0 / 0
MoonSeal reward key: SR_STATE_MOON_SEAL_FORGE_9_REWARD_MOON_SEAL_REWARD
MoonSeal reward grant executions: 0
inventory/resource mutations: 0 / 0
Boss states/transitions: 4 / 4
Boss gate order: GateLocked -> GateAccepted -> EncounterActive -> Defeated
Boss MoonSeal consume count: 0
Boss failure recoveries: 2
Boss IntroducesNewMovementRule: false
Boss AI/combat/physics executions: 0 / 0 / 0
Merchant variants: 4
Merchant shop inventory/price/purchase writes: 0
Maru choice transitions: 3
Maru PersistentChoice / PreventsReroll: true / true
Maru hint/rare-search executions: 0 / 0
optional footprint/world/reservation/bridge/fixed-slot/ownership claims: 0 / 0 / 0 / 0 / 0 / 0
progression blocker count: 0
required reward dependency count outside MoonSeal gate marker: 0
MAP13 source modifications: 0
MAP18 source modifications: 0
MAP21_01~08 source modifications: 0
generated world count: 0
Tilemap writes: 0
runtime GameObject spawns: 0
save file writes/reads: 0 / 0
Scene/Prefab changes: 0
```

## 12. Focused Validation Boundaries

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
MAP21_08 VILLAGE REGENERATION RUNS: 0
VALIDATION RUNNER EXECUTIONS: 0
REPLAY EXECUTIONS: 0
GENERATOR EXECUTIONS: 0
RENDERER EXECUTIONS: 0
ROLLBACK EXECUTIONS: 0
CSV AUTHORING WRITES OUTSIDE MAP21_09 AUTHORING FOLDER: 0
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

If a real compile or focused test failure occurs, only the MAP21_09 focused category may be rerun after a local fix. Do not broaden test selection unless the Result explicitly reports a real regression trigger and stops for review.

## 13. Finalize and Commit Rules

Finalize only if all MAP21_09 checks pass.

Status expectations before finalize:

```text
MAP21_09_COMPLETE_FORGE_BOSS_AND_OPTIONAL_REGIONS: CURRENT
MAP21_10_TUNE_REPETITION_DENSITY_AND_PACING: LOCKED
```

Status expectations after finalize:

```text
MAP21_09_COMPLETE_FORGE_BOSS_AND_OPTIONAL_REGIONS: COMPLETE
Current Task: NONE
MAP21_10_TUNE_REPETITION_DENSITY_AND_PACING: LOCKED
```

The Result must state whether Status Finalize and atomic commit were actually performed. If the local protocol intentionally writes Result before finalize, say so explicitly.

Atomic commit scope:

```text
MAP21_09 task file/archive
MAP21_09 Result
MAP21_09 authoring CSV/meta files
MAP21_09 generated JSON files
MoonPalaceLandmarkProduction.cs and .meta if new
MoonPalaceLandmarkPublisher.cs and .meta if new
MoonPalaceLandmarkProductionTests.cs and .meta if new
06_IMPLEMENTATION_STATUS.md finalize change
```

Commit subject:

```text
MAP21_09 complete forge boss optional regions
```

Do not stage unrelated files. Do not push.

## 14. Stop Rule

After Result and commit:

```text
STOP
Do not create MAP21_10 files.
Do not run MAP21_10.
Do not rerun MAP13/MAP18/MAP21_01~08 categories.
Do not run world generation, renderer, validation runner, replay, rollback, PlayMode, legacy regression, or full regression.
```
