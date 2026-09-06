```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP21_05_PRODUCTIONIZE_ACTIVITIES_AND_EVENT_OVERLAYS
  task_file: TASKS/MAP21_05_PRODUCTIONIZE_ACTIVITIES_AND_EVENT_OVERLAYS.md
  requires_current_task: NONE
  requires_completed_task: MAP21_04_EXPAND_MILL_AND_DOUGH_CLUSTER_POOLS
  requires_result:
    path: REPORTS/MAP21_04_EXPAND_MILL_AND_DOUGH_CLUSTER_POOLS_RESULT.md
    status: PASS
    sha256: 7cc7c7fc470277b8266eb076e741b1e887353509daf71a0bf135e919d3ef2710
  requires_installed_task:
    path: TASKS/MAP21_04_EXPAND_MILL_AND_DOUGH_CLUSTER_POOLS.md
    sha256: fc65f3fde6c380b0c3193afd96a8105b905b01ed00129e55d19cd93db111c137
  requires_handoff:
    name: MAP21_05 handoff digest
    sha256: 585235b77d97087d29d786d733120fbcd4a50a9c998e8e77b007e9c9bb8baff2
  sets_current_task: MAP21_05_PRODUCTIONIZE_ACTIVITIES_AND_EVENT_OVERLAYS
```

# MAP21_05 - Productionize Activities and Event Overlays

```text
TASK: MAP21_05_PRODUCTIONIZE_ACTIVITIES_AND_EVENT_OVERLAYS
PHASE: MAP21 - MoonPalace Vertical Slice
STATUS: CURRENT
NEXT: MAP21_06_EXPAND_ALL_SIX_BOUNDARY_POOLS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP12에서 승인된 Activity 7종과 EventOverlay 5종을 월궁 vertical slice의 production cluster pool에 연결한다.

이번 Task는 **MoonPalace Activity/Event authoring, static compatibility, removal-safety digest, read-only manifest**만 소유한다. 실제 gameplay object, NPC AI, 보상 지급, 운석 낙하, Maru rewind 실행, sector/world 배치, renderer, validation runner, replay, save runtime state는 아직 시작하지 않는다.

| Area | This task owns | This task must not do |
|---|---|---|
| Activity profiles | MAP12의 7개 Activity ID를 월궁 production profile로 고정 | 새 Activity type 발명 |
| Cluster bindings | MAP21_03/04의 48 cluster manifest를 읽고 activity별 primary/fallback cluster를 연결 | cluster pool 재생성 또는 배치 |
| Activity slots | MAP12 slot semantics를 월궁 slot/marker authoring으로 투영 | Prefab, physics, reward, damage, NPC object 생성 |
| EventOverlay profiles | Meteor, Merchant, RareCreature, Maru, Empty 5개 variant를 marker-only profile로 고정 | Event payload 실행 |
| Safety manifests | shell/removal/critical target/recovery/static no-mutation proof digest | world generation, route validation runner, replay |

핵심 원칙:

```text
This task turns approved MAP12 Activity/Event contracts into MoonPalace production data.
It does not run those activities in the game.
It must read MAP21_03 and MAP21_04 cluster pools, not rewrite them.
It must not create generated worlds, Tilemaps, Scene/Prefab changes, runtime GameObjects, or save state.
```

## 1. 사용자 보고 의무

Result 첫 부분에는 반드시 아래 두 섹션을 둔다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

`User-Facing Implementation Report`에는 아래 질문에 답한다.

```text
이번 작업이 MAP12 Activity/Event starter를 월궁 production 데이터로 어떻게 옮겼는가?
추가된 Activity 7개와 EventOverlay 5개는 각각 무엇을 의미하는가?
각 Activity가 어떤 production cluster primary/fallback에 연결되었는가?
EventOverlay 5개 중 non-empty와 Empty가 어떻게 구분되는가?
이 작업이 실제 NPC/운석/보상/Maru 실행을 만들지 않았다는 증거는 무엇인가?
MAP21_03/04 cluster pool을 읽기 전용으로 보존했다는 증거는 무엇인가?
이번 Task에서 만든 CSV/JSON과 각 책임은 무엇인가?
legacy regression과 prior-category rerun을 하지 않았다는 증거는 무엇인가?
MAP21_06은 왜 아직 시작하지 않았는가?
```

`Responsibility and Added Scripts`는 반드시 표로 작성한다.

```text
| Script or file | Added or changed responsibility | Explicit non-ownership |
```

파일별 책임이 구체적이지 않거나, “뭘 했는지”가 사용자 입장에서 알 수 없으면 FAIL이다.

## 2. 선행조건

작업 시작 전에 다음을 확인한다.

```text
MapDesign/MCP/REPORTS/MAP21_04_EXPAND_MILL_AND_DOUGH_CLUSTER_POOLS_RESULT.md exists
MAP21_04 Result STATUS: PASS
MAP21_04 Result SHA-256:
7cc7c7fc470277b8266eb076e741b1e887353509daf71a0bf135e919d3ef2710

MapDesign/MCP/TASKS/MAP21_04_EXPAND_MILL_AND_DOUGH_CLUSTER_POOLS.md exists
MAP21_04 installed Task SHA-256:
fc65f3fde6c380b0c3193afd96a8105b905b01ed00129e55d19cd93db111c137

MAP21_05 handoff digest:
585235b77d97087d29d786d733120fbcd4a50a9c998e8e77b007e9c9bb8baff2

MAP12 Activity/Event phase exit Result exists:
MapDesign/MCP/REPORTS/MAP12_07_MAP12_ACTIVITY_EXIT_TESTS_RESULT.md
MAP12_07 Result STATUS: PASS
MAP12_07 Result SHA-256:
cfc29b7757130f144e3b57198f048d450409f2cb088fd7ab8e7465ee27b6ff06

MAP12 Activity/Event approved digests:
aggregate digest: 46330eb01dd302bf80dab6eacf88dea59f107cbecc9225b2243a395c1d0dbc8b
Activity catalog digest: 3ef83fae74d935a2469ab587414d0498cb423609b171d1c7633423e297318c3a
Event catalog digest: 2d2878f62605927a7b70a405a06079b3ebad7767e3bd7db9b6b2431177ea95a0

MAP21_04 all-biome cluster manifest digest:
d30baf04fcd6eceeabd7b6f467b24ce11ce7c5f44833887bf7b0457e6fbdda72

MAP21_04 cluster digest manifest:
0a4ffb22812a37dbebc0b3e3f61d01888986c99a78088b6cd86ed61db2969aab

MAP21_06_EXPAND_ALL_SIX_BOUNDARY_POOLS: LOCKED
```

불일치 시:

```text
STATUS: BLOCKED
reason: precondition mismatch
created/changed code files: 0
MAP21_06 started: NO
STOP
```

## 3. 허용 범위

허용 파일:

```text
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/MoonPalaceActivityEventProduction.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/MoonPalace/MoonPalaceActivityEventPublisher.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/MoonPalace/MoonPalaceActivityEventProductionTests.cs
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_05/**
MapDesign/MCP/GENERATED/MAP21_05/**
MapDesign/MCP/TASKS/MAP21_05_PRODUCTIONIZE_ACTIVITIES_AND_EVENT_OVERLAYS.md
MapDesign/MCP/REPORTS/MAP21_05_PRODUCTIONIZE_ACTIVITIES_AND_EVENT_OVERLAYS_RESULT.md
MapDesign/MCP_ARCHIVE/MAP21_05_PRODUCTIONIZE_ACTIVITIES_AND_EVENT_OVERLAYS.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

조건부 허용:

```text
Existing MAP12 Activity/Event public contracts may be referenced read-only.
Existing MAP21_03 and MAP21_04 cluster authoring/generated files may be read and hashed.
Existing MoonPalaceClusterPoolUnion.cs may be referenced read-only for the 48-cluster manifest shape.
If a tiny shared read-only helper is unavoidable, it must live in MoonPalaceActivityEventProduction.cs and must not change existing public generation/rendering behavior.
Authoring CSV writes are allowed only under Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_05.
Generated JSON writes are allowed only under MapDesign/MCP/GENERATED/MAP21_05.
```

금지:

```text
MAP12 source CSV or production C# rewrite
MAP21_01 profile/tile shell rewrite
MAP21_02 MicroPattern rewrite
MAP21_03 Crater/Root cluster rewrite or regeneration
MAP21_04 Mill/Dough cluster rewrite or regeneration
cluster placement into sector/world
sector/world generation
pattern renderer execution or behavior change
validation runner execution
actual replay execution
rollback execution
save/runtime state generation
Activity state machine execution
Event payload execution
Meteor physics, projectile, damage, or tile destruction
WanderingMerchant NPC spawn, shop inventory, price, or purchase logic
RareCreature NPC spawn, AI, reward, or capture logic
Maru rewind, attention, hint, persistence, or UI execution
Reward grant or inventory mutation
Tilemap bake or Tilemap mutation
Collider generation
Scene / Prefab / ProjectSettings / Packages changes
sprite/audio/background asset import
Addressables group/build change
runtime GameObject instantiate/enable/disable/destroy during tooling actions
CSV authoring edit outside MAP21_05 authoring folder
auto-fix / auto-repair
production seed approval
external process launch
manual gameplay test
automatic MAP21_06 start
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
MAP21_04 Mill/Dough cluster regeneration
legacy 19347 regression
prior task category selection
PlayMode selection
unfiltered test run
full regression run
```

## 4. Activity Production 계약

MAP21_05 must preserve the exact MAP12 Activity IDs. Do not rename them and do not invent additional Activity IDs.

Required Activity profile fields:

```text
activity_id
schema_version
source_activity_id
activity_kind
biome_id
strength_class
weight
primary_cluster_id
fallback_cluster_ids
required_slot_semantics
cue_policy
activation_policy
reward_policy
recovery_policy
removal_policy
static_shell_digest
removal_safety_digest
cluster_binding_digest
slot_digest
canonical_digest
```

Required Activity inventory:

| Activity ID | Biome | Strength | Primary cluster | Required fallback cluster |
|---|---|---|---|---|
| `ACT_CRATER_BOULDER_CHAIN` | `MoonCrater` | `Strong` | `TC_CRATER_PROD_ROCK_SHELF_CHAIN` | `TC_CRATER_PROD_FALL_RECOVERY` |
| `ACT_CRATER_RICOCHET_MINE` | `MoonCrater` | `Strong` | `TC_CRATER_PROD_BROKEN_SLOPE` | `TC_CRATER_PROD_BOWL_CROSS` |
| `ACT_DOUGH_TIME_TRIAL` | `MoonDough` | `Ordinary` | `TC_DOUGH_PROD_BOUNCE_CUP` | `TC_DOUGH_PROD_SQUISH_CROSS` |
| `ACT_MARU_REWIND_ANOMALY` | `MoonDough` | `Strong` | `TC_DOUGH_PROD_RECOVERY_PAD_CHAIN` | `TC_DOUGH_PROD_STICKY_SHELF` |
| `ACT_MILL_ESCORT_CART` | `AbandonedMill` | `Ordinary` | `TC_MILL_PROD_BEAM_OVERHANG` | `TC_MILL_PROD_RUST_LEDGE` |
| `ACT_MILL_GEAR_GRID` | `AbandonedMill` | `Ordinary` | `TC_MILL_PROD_GEAR_GALLERY` | `TC_MILL_PROD_BROKEN_PILLAR` |
| `ACT_MILL_PESTLE_WORKSHOP` | `AbandonedMill` | `Strong` | `TC_MILL_PROD_ORTHOGONAL_SHAFT` | `TC_MILL_PROD_FALL_RECOVERY` |

Rules:

```text
There must be exactly 7 Activity profile records.
Every source_activity_id equals activity_id and is present in MAP12.
Every primary/fallback cluster exists in the MAP21_04 all-biome 48-cluster manifest.
Every primary/fallback cluster biome matches the Activity biome.
Every primary/fallback cluster pool kind must be Terrain, not Quiet or Buffer.
Every Activity has at least one Cue slot, one Activation/Core slot, one Reward or RewardIntent slot, one SafePocket, one Recovery, and one Reset/Exit preservation proof.
Removal safety must prove static shell identity and critical target preservation.
Activity production data may contain marker payload identifiers, but may not execute or instantiate payloads.
CassiaRoot receives no MAP21_05 Activity profile; it remains available to cluster/route generation through the 48-cluster pool and explicit Empty Event compatibility.
```

Required Activity slot semantics:

```text
Cue
Trigger
Device
Hazard
Projectile
Npc
Reward
SafePocket
Recovery
Reset
ExitPreservation
```

The task may omit a slot semantic only when the source MAP12 Activity did not use that semantic, but the Result must report the actual per-Activity slot count and missing optional semantics.

## 5. EventOverlay Production 계약

MAP21_05 must preserve the exact MAP12 EventOverlay IDs.

Required Event profile fields:

```text
event_id
schema_version
source_event_id
event_kind
operation
weight
cooldown_gap
marker_count
compatible_activity_ids
compatible_cluster_scope
payload_identity
empty_variant
marker_digest
compatibility_digest
canonical_digest
```

Required Event inventory:

| Event ID | Kind / operation | Required compatibility | Marker count |
|---|---|---|---:|
| `EVT_METEOR_FALL` | `State / SetState` | Terrain cluster CORE marker candidates, no Quiet/Buffer | 1 |
| `EVT_WANDERING_MERCHANT` | `Npc / SpawnNpc` | `ACT_MILL_ESCORT_CART` Npc slot only | 1 |
| `EVT_RARE_CREATURE` | `Npc / SpawnNpc` | `ACT_MILL_ESCORT_CART` Npc slot only | 1 |
| `EVT_MARU_INTERVENTION` | `State / SetState` | `ACT_MARU_REWIND_ANOMALY` Device slot only | 1 |
| `EVT_EMPTY` | `Empty / none` | Explicit fallback for all 7 Activity profiles and unassigned cluster opportunities | 0 |

Rules:

```text
There must be exactly 5 Event profile records.
There must be exactly one Empty variant and its marker/weight/gap/payload are all zero or none.
Every non-empty Event has exactly one marker definition.
Event marker definitions are static authoring rows only.
Meteor may publish a State payload identity, but no falling object, projectile, damage, destruction, or physics simulation.
Merchant and RareCreature may publish NPC payload identities, but no NPC GameObject, shop, inventory, price, AI, reward, or capture behavior.
Maru may publish State payload identity, but no rewind, attention, hint, persistence, UI, or reroll prevention execution.
Event assignment/cooldown logic may be represented as static compatibility data only; do not run the population or validation pipeline.
```

## 6. Required Authoring Files

Create exactly these MAP21_05 authoring CSVs unless a narrower equivalent is strongly justified in the Result.

```text
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_05/moonpalace_activity_profiles.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_05/moonpalace_activity_cluster_bindings.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_05/moonpalace_activity_slots.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_05/moonpalace_event_overlay_profiles.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_05/moonpalace_event_overlay_markers.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_05/moonpalace_activity_event_compatibility.csv
```

Authoring rules:

```text
CSV must use UTF-8 without BOM, LF, one final LF, stable header order, stable row order.
No generated world coordinates may be stored.
Coordinates are local Activity shell or marker coordinates only.
No file outside MAP21_05 authoring may be written.
```

## 7. Required Generated Artifacts

Create exactly these generated JSON artifacts.

```text
MapDesign/MCP/GENERATED/MAP21_05/moonpalace_activity_event_manifest.json
MapDesign/MCP/GENERATED/MAP21_05/moonpalace_activity_removal_safety_manifest.json
MapDesign/MCP/GENERATED/MAP21_05/moonpalace_event_overlay_manifest.json
MapDesign/MCP/GENERATED/MAP21_05/moonpalace_activity_event_digest_manifest.json
```

Generated artifact rules:

```text
JSON must be deterministic and canonical enough to hash repeatedly.
created_utc may exist only if excluded from canonical digest.
The digest manifest must include source MAP12 digest, MAP21_04 all-biome cluster digest, every MAP21_05 CSV digest, every MAP21_05 JSON digest, and MAP21_06 handoff digest.
The generated artifacts are static publication samples, not runtime save files.
```

## 8. Required Focused Tests

Add exactly one focused test file:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/MoonPalace/MoonPalaceActivityEventProductionTests.cs
```

Required test category:

```text
MAP21_05
```

Required focused test cases:

```text
MoonPalaceActivitiesContainExactSevenMap12IdsAndNoInventedActivityTypes
MoonPalaceActivitiesBindOnlyToExistingSameBiomeTerrainProductionClusters
MoonPalaceActivitySlotsPreserveCueActivationRewardRecoveryResetAndRemovalProofs
MoonPalaceActivityRemovalSafetyPreservesStaticShellCriticalTargetsAndRecovery
MoonPalaceEventsContainMeteorMerchantRareCreatureMaruAndSingleEmptyVariant
MoonPalaceEventsRemainMarkerOnlyAndRejectRuntimePayloadExecutionRequests
MoonPalaceEventCompatibilityKeepsMerchantRareCreatureMaruScopedAndEmptyExplicit
MoonPalaceActivityEventPublisherReadsMap12AndMap21ClusterArtifactsWithoutRewriting
MoonPalaceActivityEventPublisherWritesOnlyMap21_05AuthoringAndGeneratedRoots
MoonPalaceActivityEventDigestsAreDeterministicReverseOrderRepeatAndCultureStable
MoonPalaceActivityEventRejectsDuplicateIdsUnknownClustersWrongBiomeQuietBufferAndMissingEmpty
MoonPalaceActivityEventDoesNotRunGenerationRendererValidationReplayRollbackPlayModeOrLegacyRegression
MoonPalaceActivityEventPublishesMap21_06HandoffOnlyAfterFocusedPass
```

Only run:

```text
EditMode category MAP21_05
```

Expected:

```text
Discovered: 13
Executed: 13
Passed: 13
Failed: 0
Skipped: 0
Inconclusive: 0
```

Do not run PlayMode or any previous task category.

## 9. Result Required Evidence

The Result must include these sections.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
## Activity Production Summary
## EventOverlay Production Summary
## Cluster Binding and Compatibility Summary
## Static Safety and Non-Execution Summary
## Snapshot and Digest Summary
## Focused Validation Summary
## No Legacy Regression Boundary Notes
## Final Status Evidence
```

Minimum required evidence:

```text
activity profile records: 7
Activity IDs: exact seven MAP12 IDs
Activity primary/fallback binding rows: 7
Activity slot records: report exact count
Activity removal safety proofs: 7 / 7
unknown activity IDs: 0
unknown cluster IDs: 0
wrong-biome cluster bindings: 0
Quiet/Buffer Activity bindings: 0

event profile records: 5
Event IDs: EVT_METEOR_FALL, EVT_WANDERING_MERCHANT, EVT_RARE_CREATURE, EVT_MARU_INTERVENTION, EVT_EMPTY
non-empty event marker records: 4
empty variant records: 1
empty marker count / weight / gap: 0 / 0 / 0
event payload executions: 0

MAP21_03/04 cluster artifact modifications: 0
MAP12 source modifications: 0
generated world count: 0
Tilemap writes: 0
runtime GameObject spawns: 0
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
VALIDATION RUNNER EXECUTIONS: 0
REPLAY EXECUTIONS: 0
GENERATOR EXECUTIONS: 0
RENDERER EXECUTIONS: 0
ROLLBACK EXECUTIONS: 0
CSV AUTHORING WRITES OUTSIDE MAP21_05 AUTHORING FOLDER: 0
RUNTIME OBJECT SPAWNS: 0
SCENE PREFAB CHANGES: 0
```

If a real compile or focused test failure occurs, only the MAP21_05 focused category may be rerun after a local fix. Do not broaden test selection unless the Result explicitly reports a real regression trigger and stops for review.

## 11. Finalize and Commit Rules

Finalize only if all MAP21_05 checks pass.

Status expectations before finalize:

```text
MAP21_05_PRODUCTIONIZE_ACTIVITIES_AND_EVENT_OVERLAYS: CURRENT
MAP21_06_EXPAND_ALL_SIX_BOUNDARY_POOLS: LOCKED
```

Status expectations after finalize:

```text
MAP21_05_PRODUCTIONIZE_ACTIVITIES_AND_EVENT_OVERLAYS: COMPLETE
Current Task: NONE
MAP21_06_EXPAND_ALL_SIX_BOUNDARY_POOLS: LOCKED
```

Atomic commit scope:

```text
MAP21_05 task file/archive
MAP21_05 Result
MAP21_05 authoring CSV/meta files
MAP21_05 generated JSON files
MoonPalaceActivityEventProduction.cs and .meta if new
MoonPalaceActivityEventPublisher.cs and .meta if new
MoonPalaceActivityEventProductionTests.cs and .meta if new
06_IMPLEMENTATION_STATUS.md finalize change
```

Commit subject:

```text
MAP21_05 productionize activities and event overlays
```

Do not stage unrelated files. Do not push.

## 12. Stop Rule

After Result and commit:

```text
STOP
Do not create MAP21_06 files.
Do not run MAP21_06.
Do not rerun MAP21_04 generation.
Do not run world generation, renderer, validation runner, replay, rollback, PlayMode, legacy regression, or full regression.
```
