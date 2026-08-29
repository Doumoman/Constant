```yaml
mcp_repair:
  format: current_task_repair_v1
  repair_id: MAP12_05R_EXTEND_ACTIVITY_EVENT_AUTHORING_SCHEMA
  repairs_current_task: MAP12_05_AUTHOR_STARTER_ACTIVITIES_AND_OVERLAYS
  requires_current_task: MAP12_05_AUTHOR_STARTER_ACTIVITIES_AND_OVERLAYS
  requires_blocked_result:
    path: REPORTS/MAP12_05_AUTHOR_STARTER_ACTIVITIES_AND_OVERLAYS_RESULT.md
    status: BLOCKED
    sha256: bd02f0efe791fbb25e4de4beeea04c3ba45e47ea5ae20dab04da31e139a0e483
  requires_installed_task:
    path: TASKS/MAP12_05_AUTHOR_STARTER_ACTIVITIES_AND_OVERLAYS.md
    sha256: 1399da3436d8e4ea1b3c29c0381ab45adf7908a5832e2718a3c574d915531ba3
  preserves_current_task: MAP12_05_AUTHOR_STARTER_ACTIVITIES_AND_OVERLAYS
  next_task_remains_locked: MAP12_06_CREATE_ACTIVITY_PREVIEW_AND_PLAYMODE_FIXTURES
```

# MAP12_05R — Extend Activity/Event Authoring Schema

```text
REPAIR: MAP12_05R_EXTEND_ACTIVITY_EVENT_AUTHORING_SCHEMA
CURRENT TASK: MAP12_05_AUTHOR_STARTER_ACTIVITIES_AND_OVERLAYS
STATUS EFFECT: NONE — MAP12_05 stays CURRENT until final PASS
NEXT: MAP12_06_CREATE_ACTIVITY_PREVIEW_AND_PLAYMODE_FIXTURES stays LOCKED
```

## 0. Repair 결정

MAP12_05 preflight는 현재 Activity/EventOverlay 5-table/25-column schema가 이미 승인된 MAP09_05와 MAP12_03/04 의미를 손실 없이 저장할 수 없음을 증명했다.

```text
Current whole registry: 24 tables / 143 columns / 44 FK
Current Activity/Event:  5 tables / 25 columns

Repaired whole registry: 29 tables / 189 columns / 59 FK
Repaired Activity/Event: 10 tables / 71 columns
```

이 addendum은 schema contract revision을 명시적으로 승인한다.

- 기존 Activity/Event 5개 path를 보존하고 필요한 column만 확장한다.
- normalized companion table 5개만 추가한다.
- non-Activity/Event descriptor는 byte/semantic-identical하게 보존한다.
- Authoring→Generated 방향, legacy FK 2개, PK/FK/token 검증을 보존한다.
- ID 파싱, filename 의미, JSON/blob, delimiter list, display-name fallback을 만들지 않는다.
- schema repair PASS 뒤 원래 MAP12_05 starter content 구현을 같은 CURRENT Task에서 재개한다.

## 1. 적용과 preflight

이 파일은 새 Master Task가 아니다. normal `NONE → CURRENT` open flow를 실행하지 않는다.

1. Status에서 MAP12_05가 exact `CURRENT`, MAP12_06이 `LOCKED`인지 확인한다.
2. BLOCKED Result와 original MAP12_05 installed Task SHA가 metadata와 exact 일치해야 한다.
3. current schema는 exact `24/143/44`, digest `78a0df2056db7b12241c127ba85c573e26859503856cd8c8ea1a12648c8f4b57`이어야 한다.
4. Activity/Event descriptor는 exact `5 tables / 25 columns`여야 한다.
5. target Activity/Event physical CSV/meta는 `0/0`이어야 한다.
6. current Authoring CSV/meta는 `65/65`, Generated CSV는 `0`이어야 한다.
7. 다른 inbox candidate와 unrelated staged path는 0이어야 한다.

repair를 다음 두 위치에 byte-identical 설치한다.

```text
MCP/TASKS/MAP12_05R_EXTEND_ACTIVITY_EVENT_AUTHORING_SCHEMA.md
MCP_ARCHIVE/MAP12_05R_EXTEND_ACTIVITY_EVENT_AUTHORING_SCHEMA.md
```

Master/Status는 repair 설치 시 변경하지 않는다. original MAP12_05 Task와 이 addendum이 합쳐진 것이 effective specification이다.

## 2. Revised Activity descriptors

Activity set은 기존 3개 + 신규 4개, 총 7개다.

### 2.1 Expanded Activity catalog

```text
Activity/activity_catalog_v2.csv
activity_id,static_shell_id,reward_policy,recovery_policy,removal_safe,terrain_cluster_id,spine_variant_id,entry_traversal_node_id,exit_traversal_node_id,preserve_static_traversal,preserve_access_class,permanent_solid_mutation_allowed,mandatory_exit_destruction_allowed,min_active_chunks,max_active_chunks,clearance_width,clearance_height,placement_weight,strength_class
```

Rules:

- first five existing columns keep their order/type/meaning.
- `terrain_cluster_id` FK → `terrain_cluster_catalog_v2.cluster_id`.
- `spine_variant_id` FK → `terrain_cluster_variants_v2.spine_variant_id`; owning cluster exact match.
- entry/exit node FK → `terrain_cluster_nodes_v2.node_id`; owning baseline variant exact match.
- preserve flags must be true; permanent mutation/exit destruction flags must be false.
- chunk bounds are positive and ordered; clearance width/height are positive.
- placement weight `1..10000`; strength exact `ORDINARY | STRONG`.
- derived runtime digest values are not authored. Existing compiler builds traversal/access/static-shell evidence from explicit references.

### 2.2 Expanded Activity cues

```text
Activity/activity_cues_v2.csv
activity_id,cue_id,cue_kind,marker_id,slot_id,detectable_before_activation
```

- first four columns retain existing meaning.
- `slot_id` explicit FK to Activity slot table and same Activity owner.
- detect-before-activation is exact Boolean and starter cues require true.

### 2.3 Preserved graph-edge table

```text
Activity/activity_graph_edges_v2.csv
activity_id,edge_id,graph_kind,edge_kind,from_node_id,to_node_id,edge_order
```

Header/order/meaning stay unchanged. Add explicit FK edges from `from_node_id` and `to_node_id` to the new node table. Both nodes must share Activity and graph kind.

### 2.4 New slot table

```text
Activity/activity_slots_v2.csv
activity_id,slot_id,slot_kind,local_x,local_y
```

- PK: globally unique `slot_id`; Activity FK explicit.
- slot kind uses exact MAP09_05 `Cue/Trigger/Device/Hazard/Projectile/Reward/Recovery/Reset/Npc` codec tokens.
- coordinate is explicit current TerrainCluster local tile; no PDF coordinate or automatic placement.

### 2.5 New graph-node table

```text
Activity/activity_graph_nodes_v2.csv
activity_id,graph_kind,node_id,node_kind,slot_id,is_start,is_terminal
```

- PK: globally unique `node_id`; Activity FK explicit.
- graph kind exact `MECHANISM | PROGRESSION`.
- mechanism node kind uses existing CueEmitter/Trigger/Device/Hazard/ProjectileEmitter/RewardEmitter/RecoveryController/ResetController tokens.
- progression node kind uses existing Cue/Activation/Core/Reward/Recovery/Reset/Exit tokens.
- mechanism nodes require compatible `slot_id`; progression slot is optional and never inferred.
- only progression Cue start and Exit terminal may publish true starter flags.

### 2.6 New removal-safety cell table

```text
Activity/activity_safety_cells_v2.csv
activity_id,safety_cell_kind,local_x,local_y
```

- PK: `(activity_id,safety_cell_kind,local_x,local_y)`.
- exact kinds `SAFE_POCKET | RECOVERY`.
- each Activity requires both non-empty sets.
- no permanent-solid-write row kind exists because such mutation is forbidden in catalog policy.

### 2.7 New Activity compatibility table

```text
Activity/activity_compatibility_v2.csv
activity_id,compatibility_kind,value_token
```

- PK: all three columns; Activity FK explicit.
- exact kinds `BIOME | PACING | ACCESS`.
- `value_token` validates against existing MoonpalaceBiomeId, PacingRole, AccessClass codecs according to kind.
- every Activity requires at least one row of each kind; duplicate/default/unknown tokens fail.

## 3. Revised EventOverlay descriptors

Event set은 기존 2개 + 신규 1개, 총 3개다.

### 3.1 Expanded Event catalog

```text
EventOverlay/event_overlay_catalog_v2.csv
overlay_id,selection_weight,variant_kind,is_empty,terrain_cluster_id,activity_id,minimum_progression_gap
```

- first four columns retain existing order/type/meaning.
- non-empty requires explicit TerrainCluster FK; Activity FK is optional.
- when Activity is present its static shell must reference the same TerrainCluster.
- minimum gap is integer `>=0`; non-empty weight `1..10000`.
- Empty uses weight/gap `0/0` and no marker rows. Empty shell-reference handling must follow the existing MAP09_05 validator exactly; do not add wildcard/default behavior.

### 3.2 Expanded marker-assignment table

```text
EventOverlay/event_overlay_markers_v2.csv
overlay_id,marker_id,marker_kind,local_x,local_y,operation,payload_id,target_source_kind,target_owner_id,target_slot_kind
```

- first five columns preserve existing meaning.
- operation uses existing EnableMarker/DisableMarker/SpawnNpc/SpawnReward/SetState tokens.
- payload ID is non-empty stable token for non-empty variants.
- source kind exact `TERRAIN_CLUSTER | ACTIVITY | SPECIAL_REGION`.
- owner ID and slot kind are explicit; no ID parsing or polymorphic schema FK.
- importer/catalog validator resolves the owner against the matching current catalog and verifies coordinate/slot/provenance.
- Empty owns zero marker rows.

### 3.3 New Event compatibility table

```text
EventOverlay/event_overlay_compatibility_v2.csv
overlay_id,compatibility_kind,value_token
```

- PK: all three columns; overlay FK explicit.
- kinds exact `BIOME | PACING | ACCESS | ACTIVITY | SPECIAL_SLOT`.
- value validates against the corresponding existing token/ID domain.
- Empty compatibility must be explicit and must not use `ANY`, wildcard, missing-row fallback, or filename inference.

## 4. Registry and FK contract

After revision:

```text
Whole registry: 29 tables / 189 columns / 59 FK
Activity:        7 tables / 51 columns
EventOverlay:    3 tables / 20 columns
Combined:       10 tables / 71 columns
Legacy FK:       2 unchanged
Generated tables/targets: 0 / 0
```

New FK edges are exactly:

```text
activity catalog → TerrainCluster catalog / variant / entry node / exit node
activity cues → activity slots
activity graph edges → activity graph nodes (from / to)
activity slots / nodes / safety / compatibility → activity catalog
activity graph nodes → activity slots
event catalog → TerrainCluster catalog / optional Activity catalog
event compatibility → event catalog
```

Existing Activity child→catalog and Event marker→catalog FKs remain. Dynamic Event target owner and compatibility value domains are validated by the Activity/Event authoring catalog, not encoded as an ambiguous cross-domain FK.

All descriptor collections remain immutable, ordinal deterministic, accumulated-error/zero-publication. Canonical digest includes new tables/columns/types/required/default/allowed/PK/FK semantics.

## 5. Exact schema source/test boundary

Schema repair may modify only:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Data/V2AuthoringSchemaRegistry.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/V2AuthoringSchemaRegistryTests.cs
```

Additionally locate the exact existing `MAP09_08` focused test source that holds the `24/143/44` schema expectation. If it contains an exact golden schema block, update only that block to `29/189/59` and the new digest/membership. Do not change its inventory, ownership, safety, pass-order, Generated, or legacy assertions.

Do not modify:

```text
V2AuthoringSchemaContracts.cs
V2AuthoringSchemaValidation.cs
V2AuthoringSchemaCanonicalDigest.cs
```

If existing generic descriptor types cannot express this schema, return `BLOCKED`. Do not expand the repair into framework changes.

## 6. Minimal owner verification

This schema revision is an actual regression trigger owned by MAP09_07. After editing schema and before creating content, run exactly:

```text
MAP09_07 focused: required once
MAP09_08 focused: required once
```

Requirements:

- compile/Console/relevant warnings `0/0/0`
- exact `29/189/59`, 10 Activity/Event descriptor membership
- two legacy FKs and all other descriptor semantic slices unchanged
- reversed enumeration/culture-stable digest
- Generated table/target 0

Do not run MAP09_01~06, MAP10, MAP11, MAP12_01~04 selections, legacy `19347`, or PlayMode. If owner verification fails, create no physical Activity/Event CSV or starter code; keep MAP12_05 CURRENT and report `BLOCKED`.

## 7. Resume original MAP12_05 after schema PASS

After both owner categories PASS, resume the original MAP12_05 specification with these exact superseding points:

1. representability authority is repaired `29/189/59`, not `24/143/44`.
2. Activity/Event physical CSV count is 10, not 5.
3. Add exact companion CSV/meta:

```text
Activity/activity_slots_v2.csv(.meta)
Activity/activity_graph_nodes_v2.csv(.meta)
Activity/activity_safety_cells_v2.csv(.meta)
Activity/activity_compatibility_v2.csv(.meta)
EventOverlay/event_overlay_compatibility_v2.csv(.meta)
```

4. create/populate all 10 descriptor files atomically; Authoring inventory becomes `65/65 → 75/75`, Generated stays 0.
5. original Runtime catalog 2, Editor importer 1, focused test 1 boundaries remain unchanged.
6. importer reads exact 10 paths and creates no Generated/asset/SO output.
7. original exact Activity 7 IDs/cluster bindings/strength/weights and Event 5 IDs/kinds/weights/gaps remain binding.
8. original PDF-coordinate prohibition, explicit Empty, marker-only Event, removal-safe Activity, no actual gameplay rules remain binding.

Schema owner categories are not rerun after they PASS unless either allowed schema source/test changes again. Task-owned importer/catalog/CSV/test failures are repaired with `MAP12_05` selection only.

## 8. MAP12_05 focused verification

Run category `MAP12_05` only after schema owner PASS. In addition to original requirements, prove:

1. exact 10 headers/files and 29/189/59 registry binding
2. exact Activity tables `7/51`, Event tables `3/20`
3. all slot/node/cue/edge FK ownership and compatible node-slot kinds
4. explicit cluster/variant/entry/exit binding for all seven Activities
5. explicit safe/recovery sets and four removal policy flags
6. compatibility row completeness and numeric placement profile round-trip
7. Event operation/payload/source owner/slot and compatibility/gap round-trip
8. `EVT_EMPTY` has zero markers and only explicit compatible opportunities
9. all seven through MAP12_01~03 public APIs and all five Events through MAP12_04 public API
10. non-Activity/Event descriptor digest slices and existing 65 CSV/meta remain unchanged

Normal selection summary:

```text
REGRESSION TRIGGER DETECTED: YES
Trigger owner: MAP09_07 schema authority
Reason: approved Activity/Event descriptor contract revision
Minimum verification: MAP09_07 + MAP09_08 once

MAP12_05 focused: required
MAP09_07/08: one triggered execution each
MAP09_01~06: 0
MAP10/MAP11/MAP12_01~04 selections: 0
Legacy 19347: 0
PlayMode/unfiltered: 0/0
```

## 9. Atomic failure and change scope

If schema verification fails:

- no physical Activity/Event CSV, catalog, importer, or test creation
- report exact modified schema files and failure
- no Finalize/commit/push

If content verification fails after schema PASS:

- repair only original MAP12_05-owned catalog/importer/test/10 CSVs
- rerun MAP12_05 only
- do not rerun MAP09_07/08 unless schema files changed again

PASS scope:

```text
modified Runtime schema C#/meta: 1 / 0
modified schema owner test C#/meta: 1 / 0
optional modified MAP09_08 test C#/meta: at most 1 / 0
new Runtime catalog C#/meta: 2 / 2
new Editor importer C#/meta: 1 / 1
new focused test C#/meta: 1 / 1
new Activity/Event CSV/meta: 10 / 10
other existing C#/test/CSV/meta changes: 0
Generated/Scene/Prefab/Tilemap/asmdef/Settings/Packages changes: 0
```

No unrelated file may be modified, staged, or included. Preserve the three pre-existing unrelated untracked meta files.

## 10. Required Result and report

Rewrite the same Result path:

```text
REPORTS/MAP12_05_AUTHOR_STARTER_ACTIVITIES_AND_OVERLAYS_RESULT.md
```

Header:

```text
TASK: MAP12_05_AUTHOR_STARTER_ACTIVITIES_AND_OVERLAYS
STATUS: PASS | BLOCKED
MAP12_05: COMPLETE ELIGIBLE | NOT COMPLETE
MAP12_06_CREATE_ACTIVITY_PREVIEW_AND_PLAYMODE_FIXTURES: LOCKED / DO NOT START
```

First sections remain:

```text
## User-Facing Implementation Report
## Responsibility and Added Functions
```

Required evidence:

- original Task SHA, repair SHA, prior BLOCKED Result SHA
- every modified/added C#/CSV and each responsibility/input/output
- schema before/after counts, FK list, canonical digest, unchanged descriptor slices
- MAP09_07/08 exact focused counts and trigger reason/minimality
- 10 CSV headers/row counts/bytes/hash/BOM/LF/final-LF
- 7 Activity and 5 Event matrices with profiles, graph/safety/marker evidence
- MAP12_05 exact focused counts
- newly enabled functions, pipeline position, downstream MAP12_06 input
- Not Implemented: Prefab/physics/state/NPC/reward/preview/PlayMode/actual placement
- Editor/Test Runner/game visibility
- complete static/change/staged/push audit

PASS일 때만 Status Finalize와 atomic commit을 수행한다.

```text
Commit subject: MAP12_05: author starter activities and overlays
Git push: NOT PERFORMED
```

PASS 후에도 MAP12_06은 시작하지 않고 STOP한다.
