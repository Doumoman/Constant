```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP09_07_EXTEND_CSV_REGISTRY_AND_CREATE_COMPATIBILITY_FIXTURES
  task_file: TASKS/MAP09_07_EXTEND_CSV_REGISTRY_AND_CREATE_COMPATIBILITY_FIXTURES.md
  requires_current_task: NONE
  requires_completed_task: MAP09_06_IMPLEMENT_SPECIAL_CANVAS_AND_SLICE_CONTRACTS
  requires_result:
    path: REPORTS/MAP09_06_IMPLEMENT_SPECIAL_CANVAS_AND_SLICE_CONTRACTS_RESULT.md
    status: PASS
    sha256: bb665f3a7e61f6d8804923afaae1f805eca89f3642b67b89c4ed9730ef2b3135
  requires_installed_task:
    path: TASKS/MAP09_06_IMPLEMENT_SPECIAL_CANVAS_AND_SLICE_CONTRACTS.md
    sha256: ebea8d166311b9fee8df2c89cb41be9ff6b438a475e0242c1b3fd019daa7a951
  sets_current_task: MAP09_07_EXTEND_CSV_REGISTRY_AND_CREATE_COMPATIBILITY_FIXTURES
```

# MAP09_07 — Extend CSV Registry and Create Compatibility Fixtures

```text
TASK: MAP09_07_EXTEND_CSV_REGISTRY_AND_CREATE_COMPATIBILITY_FIXTURES
PHASE: MAP09 — V2 Contracts / CSV / Generated Models
STATUS: CURRENT
NEXT: MAP09_08_MAP09_CONTRACT_EXIT_AUDIT
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Task Responsibility

이번 Task는 MAP09_03~06 계약을 향후 CSV로 작성할 수 있도록 **V2 Authoring schema registry, FK graph, schema index**를 추가하고, 기존 MAP07/MAP08 데이터를 변경 없이 V2 provenance로 참조할 수 있음을 focused compatibility fixture로 증명한다.

| 책임 | 이번 Task가 추가하는 기능 | 소유하지 않는 기능 |
|---|---|---|
| V2 CSV schema | 15개 Authoring table의 경로·column·PK·FK·allowed value 계약 | 실제 CSV 파일/콘텐츠 제작, import/export |
| FK/index | V2↔V2 및 허용된 V2→legacy schema edge 검증·조회 | data row FK resolution, runtime registry publish |
| compatibility | MAP07 fixed MicroChunk와 MAP08 boundary candidate의 무손실 projection fixture | 기존 데이터 수정, 후보 재선정 |
| Generated 경계 | Authoring-only schema와 Generated-output 분리 검증 | Canvas 절단, Generated CSV 쓰기 |

물리 CSV는 이번 Task에서 만들지 않는다. 이 결정은 기존 50개 Authoring 기준선을 깨지 않고 schema를 먼저 승인하기 위한 것이다. 실제 starter CSV와 콘텐츠 행은 각 콘텐츠 Phase에서 이 registry를 따라 추가한다.

## 1. No-Regression Policy

문제가 발견되지 않는 정상 경로에서는 현재 focused selection만 실행한다.

```text
MAP09_07 focused only
Prior MAP00~09_06 test selections: 0
Legacy 19347 selections: 0
```

회귀 허용 trigger:

- MAP09_07 focused test 실패
- compile/Console error
- MAP09_06 또는 기존 catalog/fixture digest mismatch
- 기존 production/test/CSV의 예상 밖 수정 필요 또는 실제 drift
- asmdef, GUID, legacy 50-file Authoring manifest drift

trigger가 없으면 이전 Task/category 및 legacy 회귀 실행을 금지한다. Trigger가 있으면 Result에 원인·영향 owner·선택한 최소 범위를 먼저 기록하고 그 범위만 실행한다. 원인을 국소화할 수 없을 때만 더 넓은 범위를 고려한다.

## 2. Preflight

읽기 전용으로 다음을 확인한다.

1. MAP09_06 Result/설치/Archive Task SHA가 metadata와 exact 일치
2. MAP09_07만 CURRENT, root inbox candidate 0
3. MAP09_01~06 live digests가 승인 Result와 일치
4. 기존 `CsvSchemaCatalog`, PK/FK/index API와 Authoring discovery 경계
5. MAP07 public `MicrochunkDefinition`/12×8/96-cell API
6. MAP08 boundary candidate/projection public API와 aggregate digest
7. Authoring CSV/meta `50/50`, manifest 아래 값, Generated CSV `0`
8. asmdef hash, GUID, compile/Console, dirty worktree

```text
Legacy Authoring manifest:
f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb

MAP08 boundary aggregate digest:
f7ff1c49f5bc33a4ad57799269bc3915806fe0cb60f347ed76eb16ea26f7fc68
```

기존 CSV나 production API 수정이 필요해 보이면 자동 수정/회귀하지 말고 `BLOCKED`다.

## 3. V2 Authoring Schema Registry

구현 위치:

```text
Runtime: Assets/_Game/Map/Runtime/WorldGeneration/Data/
Test:    Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/
Namespace: StarNight.Map.WorldGeneration.Data
```

기존 schema/value types를 재사용하고, V2 전용 immutable descriptor/registry/validator/FK index/digest만 신규 파일로 추가한다. 기존 파일은 수정하지 않는다.

최소 제공 surface:

```text
V2AuthoringTableDescriptor
V2AuthoringColumnDescriptor
V2AuthoringForeignKey
V2AuthoringSchemaRegistry
V2AuthoringSchemaValidator / Result / Error
V2AuthoringForeignKeyIndex
V2AuthoringSchemaCanonicalDigest
```

모든 collection은 defensive copy/read-only이며, table/column/PK/FK enumeration은 ordinal deterministic이다. 오류는 누적·stable-sort·dedupe하고 오류가 하나라도 있으면 registry/index/digest를 publish하지 않는다.

### 3.1 Exact 15 Authoring tables

| Owner | Relative Authoring path | 필수 semantic fields |
|---|---|---|
| MicroPattern | `MicroPattern/micro_pattern_catalog_v2.csv` | pattern ID, weight, biome set, transform set, protected policy |
| MicroPattern | `MicroPattern/micro_pattern_cells_v2.csv` | pattern FK, x/y, operation, layer, payload |
| TerrainCluster | `TerrainCluster/terrain_cluster_catalog_v2.csv` | cluster ID, pacing role, biome, footprint/spine variant identity |
| TerrainCluster | `TerrainCluster/terrain_cluster_cells_v2.csv` | cluster FK, chunk coord, role/port/access, optional legacy source IDs |
| TerrainCluster | `TerrainCluster/terrain_cluster_spine_edges_v2.csv` | edge key, cluster FK, movement, start/end, clearance/landing/recovery |
| TerrainCluster | `TerrainCluster/terrain_cluster_envelope_cells_v2.csv` | edge FK, envelope kind, local coordinate |
| Activity | `Activity/activity_catalog_v2.csv` | activity ID, static shell, reward/recovery/removal-safe policy |
| Activity | `Activity/activity_cues_v2.csv` | activity FK, cue ID/kind/marker |
| Activity | `Activity/activity_graph_edges_v2.csv` | activity FK, Mechanism/Progression kind, edge identity/order |
| EventOverlay | `EventOverlay/event_overlay_catalog_v2.csv` | overlay ID, weight, Empty variant |
| EventOverlay | `EventOverlay/event_overlay_markers_v2.csv` | overlay FK, marker ID/kind/coordinate only |
| SpecialRegion | `SpecialRegion/special_region_catalog_v2.csv` | region ID/kind, reservation ID, footprint size |
| SpecialRegion | `SpecialRegion/special_region_cells_v2.csv` | region FK, fixed-shell/slot kind, coordinate/slot identity |
| SpecialRegion | `SpecialRegion/special_region_ports_v2.csv` | region FK, Entry/Return port, side, AccessClass |
| SpecialRegion | `SpecialRegion/special_region_persistence_v2.csv` | region FK, scope, stable persistence key |

Registry는 위 exact set만 포함한다. Physical CSV, `.csv.meta`, ScriptableObject, generated mirror는 만들지 않는다.

### 3.2 PK/FK/index rules

- 모든 table은 최소 1개 PK를 가지며 composite PK order는 contiguous하다.
- child table은 parent stable ID에 explicit FK를 가진다.
- FK target file/column은 V2 registry 또는 승인된 legacy schema catalog에 존재하고 target column은 PK다.
- 허용된 legacy edge는 다음 둘뿐이다.

```text
terrain_cluster_cells_v2.source_microchunk_id
  -> microchunk_catalog.csv.microchunk_id

terrain_cluster_cells_v2.source_boundary_chunk_id
  -> boundary_chunk_catalog.csv.boundary_chunk_id
```

- 두 legacy source ID는 optional이며 값 추론/alias/fallback을 하지 않는다.
- duplicate path/table ID/column/order/PK/FK edge, missing target, FK cycle, Generated target, case-insensitive collision을 거부한다.
- index는 table path, `(file,column)`, PK column, incoming/outgoing FK를 exact lookup한다.
- schema digest는 table path, ordered columns, type/required/default/allowed values, PK order, FK target을 포함한다.
- display text, description, timestamp, reflection/file enumeration order는 digest에서 제외한다.

## 4. MAP07/MAP08 Compatibility Fixtures

Focused test 안에서 existing public API와 현재 Authoring snapshot을 읽어 projection을 만든다. 기존 test category는 선택하지 않는다.

### 4.1 MAP07 fixed MicroChunk

- active source `MicrochunkDefinition`을 수정/복제/재저장하지 않는다.
- source ID, exact `12×8`, 96 unique cells, socket/edge identity와 tile payload를 read-only compatibility view로 투영한다.
- 같은 입력을 두 번 투영한 canonical digest가 동일해야 한다.
- projection은 `terrain_cluster_cells_v2.source_microchunk_id` FK target으로만 사용되며 4×4 MicroPattern 또는 GeneratedSlice source로 승격하지 않는다.
- invalid geometry, missing ID, missing/duplicate cell, unknown FK는 focused negative fixture에서 거부한다.

### 4.2 MAP08 boundary candidates

현재 승인 data에서 다음을 focused fixture로 재계산한다.

```text
Biome pairs: 6
Candidates / source microchunks: 31 / 31
Tile rows / socket rows: 2976 / 62
Directional projections: 62/62
Mandatory tool_requirement NONE: 31/31
Aggregate digest:
f7ff1c49f5bc33a4ad57799269bc3915806fe0cb60f347ed76eb16ea26f7fc68
```

- candidate ID, source microchunk ID, pair/orientation/route/signature를 보존한다.
- A→B/B→A 차이는 기존 transform policy만 허용한다.
- projection은 optional `source_boundary_chunk_id` provenance이며 일반 Cluster ID나 Pattern ID로 변환하지 않는다.
- 기존 MAP08 CSV, index, resolver, preview, digest implementation은 수정하지 않는다.

## 5. Authoring / Generated Separation

```text
Authoring schema descriptor -> future Authoring CSV
Validated SectorCanvas -> GeneratedSlice -> future Generated output
```

- 15개 registry path는 모두 approved Authoring 5개 root 안이다.
- `Generated/` path와 `generated_*` table은 registry에 0개다.
- Authoring FK가 Generated artifact를 target으로 가질 수 없다.
- GeneratedSlice/Canvas/validation stamp/provenance를 Authoring source row로 역승격할 수 없다.
- 이번 Task 종료 시 physical Authoring CSV/meta는 계속 `50/50`, Generated CSV는 `0`이어야 한다.

## 6. 변경 경계

허용:

- `Runtime/WorldGeneration/Data/` 신규 V2 schema registry C#/meta
- 대응 `Tests/EditMode/.../Data/` 신규 focused test C#/meta
- 설치/Archive Task, Result, Finalize Status

금지:

- 기존 C#/test/CSV/meta 수정
- 실제 V2 CSV, dictionary CSV, Generated CSV 생성
- MAP07/MAP08 source data/API/digest 수정
- importer/exporter/file writer/Editor Window 구현
- solver/composer/renderer/slicer/streaming/save 구현
- asmdef/Scene/Prefab/Settings/Packages 변경
- 문제 trigger 없는 이전 Task/legacy test 선택
- unrelated path stage/commit 또는 Git push

## 7. Focused Validation

Category `MAP09_07`만 실행한다.

1. exact 15-table registry와 owner/path 분리
2. semantic field/column order/type/required/allowed-value 계약
3. PK contiguous/unique와 intra-V2 FK resolution
4. 허용된 legacy FK 2개만 존재
5. missing/duplicate/cycle/case collision/Generated target rejection
6. immutable registry, stable index, deterministic digest
7. MAP07 fixed MicroChunk 12×8/96-cell 무손실 projection
8. MAP08 6/31/2976/62/62와 aggregate digest exact
9. Authoring→Generated one-way boundary
10. no RNG/file write/Unity lifecycle/forbidden ownership

Result에 반드시 기록:

```text
MAP09_07 focused: discovered/executed/pass/fail/skip
REGRESSION TRIGGER DETECTED: NO | YES(reason)
PRIOR TASK TEST SELECTIONS: 0 (정상 경로)
LEGACY TEST SELECTIONS: 0 (정상 경로)
```

Static gate:

```text
compile/Console/relevant warning: 0/0/0
legacy Authoring CSV/meta: 50/50 unchanged
legacy Authoring manifest: f630219... exact
physical V2 Authoring CSV/meta created: 0/0
Generated CSV: 0
existing MAP00~09_06 modifications: 0
asmdef/Scene/Prefab/Settings/Packages changes: 0
duplicate GUID/unapplied candidate/diff-check: 0/0/0
unrelated staged/included: 0
```

## 8. Required Result Report

Result 파일:

```text
MAP09_07_EXTEND_CSV_REGISTRY_AND_CREATE_COMPATIBILITY_FIXTURES_RESULT.md
```

상단:

```text
TASK: MAP09_07_EXTEND_CSV_REGISTRY_AND_CREATE_COMPATIBILITY_FIXTURES
STATUS: PASS | FAIL | BLOCKED
MAP09_07: COMPLETE ELIGIBLE | NOT COMPLETE
MAP09_08_MAP09_CONTRACT_EXIT_AUDIT: LOCKED / DO NOT START
```

첫 구현 섹션은 반드시 `Responsibility and Added Functions`이며 다음을 실제 구현 기준으로 보고한다.

| 필드 | 필수 내용 |
|---|---|
| Task responsibility | schema registry/FK/index/compatibility 경계 |
| Added functions | 새 descriptor, validator, index, digest, projection fixture 기능 |
| Inputs consumed | MAP09 contracts, legacy schema, MAP07/MAP08 public data/digests |
| Outputs produced | immutable registry/index/digest와 compatibility evidence |
| Explicit non-ownership | physical CSV/import/write/generated/solver 등 미구현 기능 |
| Downstream consumers | MAP09_08과 MAP10~17 중 실제 schema 소비자 |

이후 predecessor/status/dirty preflight, 구현 파일 inventory, 15-table schema와 digest, FK/index evidence, MAP07/MAP08 compatibility, Generated 분리, focused/regression policy, Unity/static/change scope, commit handoff를 기록한다.

PASS일 때만 Finalize하고 task-owned 파일만 atomic commit한다.

```text
Subject: MAP09_07: add V2 CSV schema compatibility registry
Push: NOT PERFORMED
```

MAP09_08을 자동 시작하지 않는다. 실패 시 같은 MAP09_07 repair 범위만 보고한다.
