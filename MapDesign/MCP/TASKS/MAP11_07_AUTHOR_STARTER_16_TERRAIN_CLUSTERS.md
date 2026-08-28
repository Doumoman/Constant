```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS
  task_file: TASKS/MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS.md
  requires_current_task: NONE
  requires_completed_task: MAP11_06_IMPLEMENT_QUIET_BUFFER_CLUSTER_POOL
  requires_result:
    path: REPORTS/MAP11_06_IMPLEMENT_QUIET_BUFFER_CLUSTER_POOL_RESULT.md
    status: PASS
    sha256: 1f5d7392e68117e75a2bb6e96c86e83de4884f1d80b8347c39ca741d449ec685
  requires_installed_task:
    path: TASKS/MAP11_06_IMPLEMENT_QUIET_BUFFER_CLUSTER_POOL.md
    sha256: 9b6d9835f8ca246410b184c44a5a1ee772f27f8f7eecc3d40aa48528e6abeec1
  sets_current_task: MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS
```

# MAP11_07 — Author Starter 16 TerrainClusters

```text
TASK: MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS
PHASE: MAP11 — TerrainCluster Authoring / Compilation
STATUS: CURRENT
NEXT: MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. User-Meaning Summary

이번 Task는 처음으로 실제 TerrainCluster 콘텐츠를 Authoring CSV에 작성한다.

```text
MoonCrater       4종
CassiaRoot       4종
AbandonedMill    4종
MoonDough        4종
--------------------
starter total   16종
```

각 biome은 다음 네 pacing/archetype을 하나씩 가진다.

```text
Quiet Buffer: 2 active chunks
Traversal:    3 active chunks
Discovery:    4 active chunks
Recovery:     5 active chunks
```

CSV는 Source of Truth이며 importer/catalog가 이를 읽어 기존 MAP09_04와 MAP11_01~06 public compiler로 검증한다. Scene, Tilemap, 실제 Sector 배치, RNG는 수행하지 않는다.

## 1. Responsibility

| 소유 | 소유하지 않음 |
|---|---|
| approved 4 TerrainCluster V2 CSV physical boundary | V2 schema 열/테이블 변경 |
| exact starter 16 content rows | MicroPattern starter 수정 |
| TerrainCluster CSV row/catalog/import validation | 기존 cluster compiler 재구현 |
| 16개 core contract와 route artifact compile proof | runtime pattern/RNG selection |
| 4개 Quiet candidate pool eligibility proof | Sector placement/free-space solve |
| structural diversity/golden content tests | preview window/PlayMode scene |

흐름:

```text
4 TerrainCluster Authoring CSVs
→ exact importer/catalog
→ MAP09_04 contract validation
→ MAP11_01~04 compilation
→ pattern-free MAP11_05 working canvas
→ 4 Quiet candidates through MAP11_06
→ MAP11_08 preview/pattern-diff fixtures
```

## 2. No-Regression Policy

정상 실행은 category `MAP11_07`만 선택한다.

```text
MAP11_07 focused selection: required
Prior MAP09/MAP10/MAP11_01~06 selections: 0
Legacy 19347 selections: 0
PlayMode selections: 0
```

MAP11_07 test 안에서 기존 public compiler를 호출하는 것은 prior category 실행이 아니다. 이전/legacy category를 별도로 선택하지 않는다.

다음 실제 trigger가 있을 때만 owner·원인·최소 범위를 기록한다.

- compile/Console error가 기존 authority를 가리킴
- approved schema가 필수 TerrainCluster 의미를 표현하지 못함
- 기존 importer/schema/public compiler behavior drift
- 기존 production/test/CSV/meta 예상 밖 변경
- baseline/Authoring manifest/GUID drift

Task-owned CSV/importer/test 문제는 task-owned 파일만 고치고 `MAP11_07`만 재실행한다. 기존 schema/authority 수정이 필요하면 수정하지 말고 `STATUS: BLOCKED`로 STOP한다.

## 3. Read-Only Authorities and Representability Preflight

쓰기 전에 정확히 확인한다.

1. MAP11_06 Result/Task SHA와 COMPLETE 상태
2. MAP11_07만 CURRENT, MAP11_08 LOCKED, inbox candidate 0
3. MAP09_07 registry exact 15 tables / 83 columns / 13 V2 FK / digest
4. exact four TerrainCluster descriptors and ordered columns/PK/FK/allowed tokens
5. MAP09_04 cluster/role/port/spine/envelope contract
6. MAP11_01~06 public artifact/result/digest APIs
7. RFC4180 reader, schema catalog, header/field validator authorities
8. typed `MoonpalaceBiomeId`, `PacingRole`, `AccessClass`, RouteType `0..4`
9. Authoring 52 files and MicroPattern `24/453` hashes
10. Generated CSV 0, compile/Console, meta/GUID, dirty/staged paths

### Mandatory representability audit

The approved four descriptors must be sufficient to reconstruct, without hidden fallback:

```text
cluster ID / biome / pacing
normalized active footprint
roles and tile anchors
Entry/Exit ports, sides, access and RouteType compatibility
SpineVariant identity and exact one baseline
nodes/edges, movement, clearance, landing/recovery
envelope kind/cells and protected evidence source
authored timing/high-point/failure/recovery evidence required by MAP11_04,
or an already-approved deterministic projection of those fields
```

Do not add a CSV column, fifth TerrainCluster table, JSON blob, delimiter alias, filename convention parser, or C# hard-coded semantic fallback.

If the existing registry cannot losslessly represent a required starter artifact, report exact missing descriptor/column/semantic owner and `STATUS: BLOCKED` before creating content files. Do not modify MAP09_07 or claim partial starter content.

## 4. Exact Write Boundary

### 4.1 New Runtime authoring boundary

```text
Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/Authoring/TerrainClusterAuthoringRows.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/Authoring/TerrainClusterAuthoringCatalog.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/Authoring/TerrainClusterAuthoringValidation.cs(.meta)
```

같은 책임을 두 Runtime 파일로 안전하게 합칠 수 있다. Runtime은 parsed row DTO를 받아 immutable catalog/artifact를 만들며 filesystem, UnityEditor, RNG에 의존하지 않는다.

### 4.2 New Editor importer

```text
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Import/TerrainClusterCsvImporterV2.cs(.meta)
```

### 4.3 New focused tests

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/TerrainClusters/TerrainClusterStarterContentTests.cs(.meta)
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Import/TerrainClusterCsvImporterV2Tests.cs(.meta)
```

### 4.4 New physical Authoring CSVs

```text
Assets/_Game/Map/Data/WorldGeneration/Authoring/TerrainCluster/terrain_cluster_catalog_v2.csv(.meta)
Assets/_Game/Map/Data/WorldGeneration/Authoring/TerrainCluster/terrain_cluster_cells_v2.csv(.meta)
Assets/_Game/Map/Data/WorldGeneration/Authoring/TerrainCluster/terrain_cluster_spine_edges_v2.csv(.meta)
Assets/_Game/Map/Data/WorldGeneration/Authoring/TerrainCluster/terrain_cluster_envelope_cells_v2.csv(.meta)
```

Reuse the existing `Authoring/TerrainCluster` folder/meta. If any physical file already exists, it must be exact header-only and may be populated in place while preserving its meta/GUID. Do not overwrite non-header user content.

No existing MAP00~MAP11_06 production/test/CSV/meta, schema registry, asmdef, Scene, Prefab, Settings, or Packages file may be modified.

## 5. Exact Starter Content Matrix

The catalog contains exactly these 16 IDs and no other TerrainCluster rows.

| Biome | Cluster ID | Pacing | Active chunks | Entry→Exit |
|---|---|---|---:|---|
| MoonCrater | `TC_CRATER_QUIET_RIM` | Quiet | 2 | L→R |
| MoonCrater | `TC_CRATER_BOWL_ASCENT` | Traversal | 3 | L→U |
| MoonCrater | `TC_CRATER_BROKEN_SLOPE` | Discovery | 4 | U→D |
| MoonCrater | `TC_CRATER_ROCK_SHELF_RECOVERY` | Recovery | 5 | L→D |
| CassiaRoot | `TC_ROOT_QUIET_ARCH` | Quiet | 2 | L→R |
| CassiaRoot | `TC_ROOT_HOLLOW_POCKET` | Traversal | 3 | L→U |
| CassiaRoot | `TC_ROOT_VERTICAL_TUNNEL` | Discovery | 4 | U→D |
| CassiaRoot | `TC_ROOT_FORKED_CANOPY_RECOVERY` | Recovery | 5 | L→D |
| AbandonedMill | `TC_MILL_QUIET_BEAM` | Quiet | 2 | L→R |
| AbandonedMill | `TC_MILL_BEAM_OVERHANG` | Traversal | 3 | L→U |
| AbandonedMill | `TC_MILL_BROKEN_PILLAR` | Discovery | 4 | U→D |
| AbandonedMill | `TC_MILL_ORTHOGONAL_SHAFT_RECOVERY` | Recovery | 5 | L→D |
| MoonDough | `TC_DOUGH_QUIET_SHELF` | Quiet | 2 | L→R |
| MoonDough | `TC_DOUGH_BOUNCE_CUP` | Traversal | 3 | L→U |
| MoonDough | `TC_DOUGH_SOFT_POCKET` | Discovery | 4 | U→D |
| MoonDough | `TC_DOUGH_STICKY_RISE_RECOVERY` | Recovery | 5 | L→D |

All IDs obey the existing `^TC_[A-Z0-9_]+$` authority. Do not infer biome or pacing by parsing the ID; typed CSV fields are authoritative.

## 6. Footprint and Structural Diversity

For every cluster:

- footprint is normalized, connected by four-neighbor adjacency, duplicate-free
- active count equals the matrix exactly
- six-chunk allowlist is unused
- every active chunk owns exact 96 active tiles after MAP11_01 compile
- inactive rectangular-mask chunks remain explicit and own no active terrain
- source and compiled coordinates round-trip

Each cluster has at least two `SpineVariant`s:

```text
SPINE_<CLUSTER_SUFFIX>_BASE
SPINE_<CLUSTER_SUFFIX>_ALT
```

Exactly one is baseline. IDs need not be mechanically parsed at runtime; the exact list is golden authoring content.

No two of the 16 clusters may share the same full structural signature. The task-owned signature includes at least:

```text
normalized footprint coordinates
Entry/Exit sides and owning chunks
baseline ordered node/edge/MovementKind structure
alternate variant structure
high-route divergence/rejoin/high-point topology
major compiled y-span
```

Material, display name, biome token, row order, and MicroPattern ID do not make an otherwise identical structure unique.

Within each biome the four clusters must visibly differ in footprint and route topology; do not author one graph with renamed IDs.

## 7. Role, Port, Route, and Access Requirements

Every cluster satisfies the existing MAP09_04/MAP11_02 rules:

- exact one primary Entry and Exit
- Entry/BuildUp/Core/Recovery/Exit role anchors at least once
- role anchor ↔ graph node ↔ compiled tile identity exact
- primary port neighbor is outside active footprint or explicit inactive tile
- port side matches the matrix
- compatible RouteType values are existing integer `0..4` only
- mandatory Entry→Exit use includes `MandatoryNoTool`
- no new RouteType, AccessClass, role, codec, alias, or fallback

Quiet clusters:

```text
Reward role anchors: exact 0
PacingRole: Quiet
MAP11_06 supported uses: BeforeLandmark, AfterLandmark, UnplacedSpace
```

For each biome, at least one non-Quiet cluster has one optional Reward anchor. Reward remains a static role/slot, not a spawned item.

## 8. Base, High, and Recovery Content

Every cluster must produce a valid MAP11_04 artifact.

- pattern-removed Static Shell Entry→Exit baseline succeeds
- baseline touches every intended active chunk in authored traversal order
- at least one structurally distinct high route
- authored high-point designation exists on the high route
- distinct stable benefit IDs `>=2`
- at least one designated high-route failure node
- every failure node has a source-edge-only recovery witness to baseline
- recovery duration is `2000..5000 ms` inclusive
- no y-only high-route inference, synthetic edge, teleport, or arbitrary tunnel
- Solid/Air conflict count 0
- protected route coordinates remain Air except authored Floor Solid requirements

Timing/high/failure/benefit evidence must originate from approved CSV semantics or an already-approved deterministic projection verified in Section 3. Do not hide starter-specific evidence in tests or ID parsing.

## 9. Pattern-Free MAP11_05 Bridge

MAP11_07 does not select MicroPatterns. For each imported cluster, create a legitimate pattern-free MAP11_05 request using:

```text
authored nonprotected zone set: empty
caller-selected placement intents: empty
Static Shell / AbsoluteProtected: exact predecessor evidence
```

The result must be an immutable full working canvas equal to the pattern-removed source state, with:

```text
renderer request count: 0
renderer delta count: 0
AbsoluteProtected write/change: 0/0
active-cell coverage: exact
```

Do not synthesize a filler/NoChange placement or consume RNG. If MAP11_05 public authority cannot publish a legitimate empty-placement working canvas, report `STATUS: BLOCKED`; do not modify MAP11_05 in this Task.

Pattern zones, selected placements, rendered pattern diff, density comparison, and visual graybox are MAP11_08/MAP14 responsibilities.

## 10. Quiet Pool Proof

Compile the four Quiet clusters through MAP11_06 using deterministic profiles derived only from typed imported fields and fixed policy:

```text
Quiet Buffer ID: QBUF_ + full cluster ID suffix after TC_
supported uses: all three exact use kinds
compatible pacing: Quiet, Traversal, Recovery, Safe, Flow
compatible access: MandatoryNoTool, OptionalNoTool only when source ports allow it
```

This projection must be explicit, centralized, documented, and independent of display text or file order. It may remove only the exact `TC_` prefix after validating the full TerrainCluster ID; no biome/pacing/geometry inference from names is allowed.

All four must pass:

- exact active 2 chunks
- Entry/Exit different owning chunks
- baseline covers both chunks
- per chunk final Solid >=1 and Air >=1
- Reward/Marker/Hazard 0
- protected write/change 0/0

The pool contains exact 4 candidates, one per biome. Queries for every biome × every supported use return the correct single candidate without selection or RNG draw.

## 11. CSV Import and Catalog Contract

The importer reads only the exact four paths in Section 4 and reuses existing RFC4180/schema/header validators.

Requirements:

- UTF-8 BOM
- exact registry header and column order
- LF only and one final LF
- exact row field counts
- no recursive discovery or alternate filename
- every token exact/case-sensitive
- every FK resolves and every composite PK is unique
- rows canonical by registry PK order
- orphan/duplicate/missing child evidence accumulated
- error includes file/record/column provenance
- any error publishes zero catalog/contracts/digest
- no file write, watcher, cache, SO, asset, Generated mirror, Scene, or singleton mutation

Runtime catalog publishes the exact 16 contracts and canonical content digest. Reversed parsed-row enumeration and culture changes preserve results/digest; one semantic coordinate/edge/token mutation changes digest or emits a typed error.

## 12. Exact Non-Ownership

Forbidden:

- MAP09_07 schema descriptor/table/column/FK change
- existing MAP09/MAP10/MAP11_01~06 production/test/CSV/meta change
- MicroPattern CSV/catalog modification
- fifth TerrainCluster CSV or out-of-band JSON/content asset
- C# hard-coded replacement for missing CSV semantics
- 6-chunk content or one-chunk Quiet exception
- runtime candidate weight/RNG/selection
- pattern placement/rendered variation/density tuning
- Activity/Event/SpecialRegion content
- landmark reservation/Sector placement/world assembly
- Generated CSV, Slice, Tilemap, Scene, Prefab, SO, PlayMode
- EditorWindow/WorldGenerationRoot wiring
- asmdef/asmref/Settings/Packages
- trigger 없는 previous/legacy test selection
- unrelated path modify/stage/commit, Git push

New Runtime forbidden symbols:

```text
UnityEditor
System.IO
StageMapGenerator
GridWorld
RoomTemplate
RoomGridTransform
TileMutationService
SectorRecipeResolver
System.Random
UnityEngine.Random
DeterministicRngStreamFactory
Time.deltaTime
Tilemap
```

## 13. Focused Verification

Run category `MAP11_07` only and cover at least:

1. exact four approved descriptors/paths/headers and representability audit
2. BOM/header/field-count/token/PK/FK importer boundary
3. exact 16 IDs, 4 biomes × 4 pacing/count matrix
4. normalized connected footprints and exact 2/3/4/5 counts
5. exact Entry/Exit side matrix and mandatory no-tool compatibility
6. at least two variants, exact one baseline, valid graph/envelopes
7. MAP11_01~03 compile success for all 16 inside MAP11_07 category
8. MAP11_04 base/high/recovery success for all 16
9. benefits >=2, recovery 2000/5000 inclusive boundaries
10. pattern-free MAP11_05 full canvas for all 16, request/delta 0
11. exact four Quiet candidates pass MAP11_06 pool
12. biome × use queries resolve one candidate, RNG/draw 0
13. all 16 structural signatures unique; within-biome four distinct
14. Reward policy and Quiet Reward/Marker/Hazard zero
15. independent golden expectations not generated from imported CSV
16. invalid/orphan/duplicate/semantic mutation accumulated atomic rejection
17. immutable/canonical catalog and stable content/artifact/pool digests
18. reversed input/culture stability and semantic sensitivity
19. Generated/Scene/Prefab/SO/Tilemap/RNG side effects 0
20. task scope/CSV hashes/metas/GUID/diff-check

Do not select prior categories. Task-owned failure is repaired only in task-owned files and reruns `MAP11_07` only.

## 14. Static Gates

```text
Unity compile / Console error / relevant warning: 0 / 0 / 0
MAP11_07 focused: all discovered executed and PASS; skip/inconclusive 0
MAP11_06 Result SHA: 1f5d7392... exact
existing MAP09/MAP10/MAP11_01~06 production/test/meta modifications: 0
MicroPattern definitions / rows: 24 / 453 unchanged
MicroPattern CSV hashes: f9d9e9cc... / e702ae5d... unchanged
pre-task Authoring files / manifest: 52 / 4415ae4a... exact
post-task Authoring files: 56 exact if four new CSVs are created
Generated CSV: 0
other roots/asmdef/Scene/Prefab/Settings/Packages changes: 0
new C#/CSV/meta valid; duplicate GUID 0
unapplied candidate / diff-check / unrelated staged paths: 0 / 0 / 0
```

## 15. Required Result

```text
MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS_RESULT.md
```

Header:

```text
TASK: MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS
STATUS: PASS | BLOCKED
MAP11_07: COMPLETE ELIGIBLE | NOT COMPLETE
MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES: LOCKED / DO NOT START
```

### Required first section: User-Facing Implementation Report

First section must be Korean `## User-Facing Implementation Report` and report:

| 필드 | 필수 내용 |
|---|---|
| 이번 작업의 목적 | 16개 실제 지형이 플레이 경험에 제공하는 것 |
| 추가된 스크립트 | 모든 Runtime/Editor/Test C#과 각 책임 |
| 추가·변경된 데이터 | 네 CSV, cluster 수/row 수/biome 배분 |
| 새로 가능해진 기능 | import/compile/quiet pool까지 실제 가능해진 것 |
| 실제 파이프라인 위치 | MAP11_01~06 소비와 MAP11_08/MAP14 후속 관계 |
| 아직 안 된 것 | pattern variation/preview/placement/Tilemap/Scene |
| 게임에서 보이는 시점 | 현재 Authoring/compiled data인지 실제 화면인지 |

Then `## Responsibility and Added Functions` with actual functions, inputs, outputs, non-ownership, downstream consumers.

Also report:

```text
exact new C#/CSV/meta inventory and responsibilities
four exact CSV headers, data-row counts, bytes, SHA-256, BOM/LF/final-LF
16 cluster matrix and per-cluster artifact/digest summary
footprint count distribution 2/3/4/5 = 4 each
biome count = 4 each
SpineVariant/high/recovery/benefit summary
16 structural signatures and duplicate count
pattern-free MAP11_05 request/delta/protected counts
Quiet pool count/query evidence
pre/post Authoring manifest and Generated 0
MAP11_07 focused counts
REGRESSION TRIGGER owner/reason/minimum scope
PRIOR/LEGACY/PLAYMODE selection counts
```

PASS일 때만 Finalize하고 task-owned C#/test/CSV/meta/protocol files만 atomic commit한다.

```text
Subject: MAP11_07: author starter 16 terrain clusters
Push: NOT PERFORMED
```

PASS여도 MAP11_08을 자동 시작하지 않고 STOP한다.
