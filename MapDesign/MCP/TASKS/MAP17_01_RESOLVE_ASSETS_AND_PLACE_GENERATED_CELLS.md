```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP17_01_RESOLVE_ASSETS_AND_PLACE_GENERATED_CELLS
  task_file: TASKS/MAP17_01_RESOLVE_ASSETS_AND_PLACE_GENERATED_CELLS.md
  requires_current_task: NONE
  requires_completed_task: MAP16_09_CONSOLIDATE_GENERATED_TERRAIN_CONTRACT_PRIMITIVES
  requires_result:
    path: REPORTS/MAP16_09_CONSOLIDATE_GENERATED_TERRAIN_CONTRACT_PRIMITIVES_RESULT.md
    status: PASS
    sha256: 0714dfef77f3659dba9188cb294ecdaad4a25933e69629884bf4acb97b5afb1d
  requires_installed_task:
    path: TASKS/MAP16_09_CONSOLIDATE_GENERATED_TERRAIN_CONTRACT_PRIMITIVES.md
    sha256: 2e2fdbc609bdb780177f502d60b8ca16ead8c03a454f36cfec22659a3000c103
  sets_current_task: MAP17_01_RESOLVE_ASSETS_AND_PLACE_GENERATED_CELLS
```

# MAP17_01 - Resolve Assets and Place Generated Cells

```text
TASK: MAP17_01_RESOLVE_ASSETS_AND_PLACE_GENERATED_CELLS
PHASE: MAP17 - Tilemap Bake / Streaming / Save Preparation
STATUS: CURRENT
NEXT: MAP17_02_BUILD_TILEMAP_LAYERS_BAKE_AND_SEAM_VALIDATION
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP16까지 생성된 `GeneratedMicroChunkSliceSet`, marker slots, final canvas provenance를 실제 Tilemap bake 전에 사용할 수 있는 **in-memory generated cell placement plan**으로 변환한다.

이번 Task의 책임은 다음 세 가지다.

```text
1. generated cell의 sector-local 좌표와 world 좌표를 확정한다.
2. generated tile code / prefab id reference가 registry snapshot 안에서 해석 가능한지 검증한다.
3. MAP17_02가 Tilemap bake를 시작할 수 있도록 placement plan과 digest를 제공한다.
```

이번 Task는 **Tilemap을 굽지 않는다.**  
Prefab/GameObject를 instantiate하지 않고, collider를 만들지 않고, Scene/Prefab/Tilemap asset을 수정하지 않는다.

## 1. 사용자 보고 의무

Result의 첫 두 섹션은 반드시 아래 이름으로 작성한다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

`## User-Facing Implementation Report`에는 한국어로 다음을 설명한다.

```text
이번 Task가 어떤 기능을 추가했는지
이번 Task가 일부러 하지 않은 일
MAP17_02에 넘기는 산출물
회귀 테스트를 돌리지 않았는지, 돌렸다면 어떤 실제 문제가 트리거였는지
```

`## Responsibility and Added Scripts`에는 표로 다음을 작성한다.

| Script or file | Added or changed responsibility | Explicit non-ownership |
|---|---|---|
| 실제 파일 경로 | 이 파일이 맡은 책임 | 이 파일이 절대 하지 않는 일 |

Result에서 "테스트 PASS"만 쓰고 스크립트 책임 설명을 생략하면 이 Task는 `FAIL`이다.

## 2. 선행조건

작업 전에 다음을 확인한다.

```text
MAP16_09 Result exists
MAP16_09 Result STATUS: PASS
MAP16_09 Result SHA-256:
0714dfef77f3659dba9188cb294ecdaad4a25933e69629884bf4acb97b5afb1d

MAP16_09 installed task SHA-256:
2e2fdbc609bdb780177f502d60b8ca16ead8c03a454f36cfec22659a3000c103

Current Task before apply: NONE
MAP16_09: COMPLETE
MAP17_01: LOCKED before apply
MAP17_02: LOCKED
unrelated staged files: 0
```

선행 task SHA가 line ending 차이만으로 달라지는 경우에도 임의로 고치지 않는다. `BLOCKED`로 멈추고 실제 SHA를 Result에 기록한다.

## 3. 입력 계약

다음 MAP16 산출물을 읽어 사용한다. 실제 타입명은 프로젝트의 현재 public API를 따른다.

```text
GeneratedTerrainGeometrySnapshot
BakingCanonicalDigest
SectorFinalCanvasLayerPlan
GeneratedMicroChunkSliceSet
GeneratedMicroChunkMarkerSlotSet
GeneratedTerrainExportPacket
GeneratedTerrainReplayVerifier
MAP16_08 exit audit digest
```

필수 geometry 값은 MAP16_09 snapshot에서만 가져온다.

```text
sector=48x32 cells=1536
micro_chunk=12x8 cells=96
chunk_grid=4x4 count=16
world_sectors=13x13 count=169
world=624x416 cells=259584
layers_per_cell=7 sector_layer_records=10752
chunk_rotation_allowed=false
```

새 geometry literal authority를 만들지 않는다.

## 4. 핵심 산출물

프로젝트 패턴에 맞게 파일과 타입을 조정할 수 있지만, 다음 semantic responsibility는 분명해야 한다.

| Required concept | Responsibility |
|---|---|
| `GeneratedTerrainTileCode` | generated terrain이 참조하는 tile code value object |
| `GeneratedTerrainPrefabId` | marker slot이 참조하는 prefab id value object |
| `GeneratedTerrainAssetRegistrySnapshot` | tile code와 prefab id의 deterministic read-only registry snapshot |
| `GeneratedTerrainAssetResolution` | registry reference resolution result와 diagnostics |
| `GeneratedCellPlacementId` | generated cell placement의 stable deterministic id |
| `GeneratedCellPlacementCoordinate` | sector-local, chunk-local, slice-local, world coordinate 묶음 |
| `GeneratedCellPlacementLayer` | final canvas layer/material/source provenance projection |
| `GeneratedCellPlacementRecord` | 한 generated cell의 좌표, layer refs, tile ref, optional slot refs |
| `GeneratedCellPlacementPlan` | sector 또는 world projection 단위의 read-only placement records |
| `GeneratedCellPlacementFailure` | missing/duplicate/invalid/stale input 실패 reason |
| `GeneratedCellPlacementResult` | success/failure wrapper |
| `GeneratedCellPlacementDigest` | placement plan canonical digest |
| `GeneratedCellPlacementPlanner` | MAP16 slice/slot/export packet을 placement plan으로 변환 |

Suggested production files:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedTerrainAssetResolution.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedCellPlacementPlan.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedCellPlacementPlanner.cs(.meta)
```

Suggested focused test file:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/GeneratedCellPlacementPlannerTests.cs(.meta)
```

파일을 더 적게 또는 다르게 구성해도 된다. 다만 Result의 `Responsibility and Added Scripts`에서 실제 구성과 책임을 정확히 보고해야 한다.

## 5. 구현 규칙

### 5.1 좌표 투영

`GeneratedCellPlacementPlanner`는 MAP16 slice cell을 다음 좌표로 투영한다.

```text
slice-local cell coordinate
micro-chunk local coordinate
sector-local coordinate
sector index coordinate
world cell coordinate
```

기준 수량:

```text
micro chunk slices per sector: 16/16
cells per slice: 96
sector cells: 1536/1536
layers per cell: 7
sector layer refs: 10752/10752
world sectors for projection probe: 169/169
world projected cells for projection probe: 259584/259584
```

좌표는 integer grid가 authority다. float world position은 필요하면 derived diagnostic으로만 둔다.

### 5.2 Asset reference resolution

TileCode와 PrefabId는 string으로 흘려보내지 말고 value object 또는 normalized key로 검증한다.

필수 조건:

```text
tile code duplicate entries fail atomically
prefab id duplicate entries fail atomically
missing tile code references fail atomically
missing prefab id references fail atomically
invalid empty/whitespace/control-character ids fail atomically
registry order must not affect digest
```

프로젝트에 production tile/prefab registry가 아직 없다면:

```text
REFERENCE MAP17_01 ASSET REGISTRY
```

라는 이름이 명확한 test/reference snapshot만 만든다. 이것을 production asset approval로 보고하지 않는다.

### 5.3 Layer와 provenance 보존

MAP16 final canvas layer precedence, source/provenance token, protection/debug layer 의미를 변경하지 않는다.

이번 Task는 overlap을 새로 해결하지 않는다. MAP16에서 이미 확정된 final layer records를 placement layer refs로 투영만 한다.

### 5.4 Digest

`BakingCanonicalDigest`를 사용해서 placement plan digest를 만든다.

필수:

```text
LF normalization
UTF-8 no BOM
lower-hex SHA-256
stable repeat
stable reverse input order
stable culture
stable registry order
mutation sensitivity
```

Digest canonical line은 domain field order를 명시한다. display name이나 file system order를 dependency key로 쓰지 않는다.

## 6. 명시적 금지 범위

이번 Task에서 다음을 하지 않는다.

```text
Tilemap bake
Tilemap asset mutation
Scene mutation
Prefab mutation
GameObject instantiate
Collider build/rebuild
runtime spawned objects
stable spawn id creation
streaming/load/save implementation
Authoring CSV edits
Generated CSV commits
production seed approval
MAP17_02 unlock or execution
PlayMode tests
legacy 19347 regression
unfiltered test runs
full regression runs
```

## 7. Focused-only 검증 정책

정상 검증은 EditMode category `MAP17_01`만 선택한다.

```text
MAP17_01 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15/MAP16 selections: 0
legacy 19347 selections: 0
PlayMode selections: 0
unfiltered test selections: 0
full regression runs: 0
```

Compile check와 relevant Console check는 허용한다.

실제 문제가 발생해 더 넓은 검증이 필요하다고 판단되면 조용히 회귀를 돌리지 않는다. Result에 다음을 기록하고 멈춘다.

```text
REGRESSION TRIGGER DETECTED: YES
trigger owner:
broken invariant:
why focused proof is insufficient:
requested wider verification:
```

문제가 없다면 Result에 반드시 기록한다.

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
FULL REGRESSION RUNS: 0
```

## 8. 필수 Focused Tests

다음 test name을 그대로 포함한다. 프로젝트 test framework에 맞춰 클래스/파일은 조정할 수 있다.

```text
AssetRegistrySnapshotValidatesTileCodesAndPrefabIdsWithoutLoadingSceneObjects
GeneratedCellsProjectFromSliceLocalToSectorAndWorldCoordinates
PlacementPlanPublishesAllCellsLayersSocketsSlotsAndDigests
LayerPrecedenceAndSourceProvenanceRemainByteCompatibleWithMap16
MissingDuplicateOrInvalidAssetReferencesFailAtomically
CoordinateProjectionRejectsDuplicateMissingOutOfBoundsAndStaleGeometry
ReferenceWorldPlacementProjectionCovers169SectorsWithoutBaking
PlacementDigestIsStableAcrossRepeatReverseCultureAndRegistryOrder
PlannerDoesNotBakeTilemapsInstantiatePrefabsOrMutateScenes
Map17HandoffKeepsMap17_02Locked
```

Expected focused result:

```text
mode: EditMode
category_names: [MAP17_01]
discovered: 10
executed: 10
passed: 10
failed: 0
skipped: 0
inconclusive: 0
```

If the project already has equivalent focused tests and the exact count differs, explain why in Result. Silent substitution is not allowed.

## 9. Result 필수 증거

Result에는 아래 값을 실제 수치로 기록한다.

```text
MAP16_09 source geometry snapshot values covered: 23/23
source micro chunk slices observed: 16/16
source generated cells observed: 1536/1536
source layer refs observed: 10752/10752
source sockets preserved:
source marker slots preserved:
tile code registry entries observed/resolved/missing:
prefab id registry entries observed/resolved/missing:
placed sector cells: 1536/1536
placed layer refs: 10752/10752
cell placement ids unique: 1536/1536
sector duplicate/missing/out-of-bounds placements: 0/0/0
world projected sectors: 169/169
world projected cells: 259584/259584
world Tilemap bakes: 0
slot refs preserved:
source provenance refs preserved:
missing asset failure probes:
duplicate asset failure probes:
invalid id failure probes:
stale geometry failure probes:
placement digest lower-hex SHA-256: YES
repeat/reverse/culture/registry-order digest mismatches: 0/0/0/0
mutation sensitivity probes passed:
Tilemap bakes: 0
collider rebuilds: 0
GameObject/Prefab instantiation: 0/0
Scene/Prefab/Tilemap mutation: 0/0/0
Generated CSV/assets committed: 0
stable spawn ids created: 0
runtime objects spawned: 0
production seed approvals: 0
MAP17_02 started: NO
```

## 10. Write boundary

Allowed production source roots:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Baking/
```

Allowed test roots:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/
```

Allowed MCP files:

```text
MapDesign/MCP/TASKS/MAP17_01_RESOLVE_ASSETS_AND_PLACE_GENERATED_CELLS.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
MapDesign/MCP/REPORTS/MAP17_01_RESOLVE_ASSETS_AND_PLACE_GENERATED_CELLS_RESULT.md
```

Allowed read-only references:

```text
MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST_V2.md
MapDesign/MCP/REPORTS/MAP16_09_CONSOLIDATE_GENERATED_TERRAIN_CONTRACT_PRIMITIVES_RESULT.md
Assets/_Game/Map/Runtime/WorldGeneration/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/
```

Do not edit Master unless the current MCP protocol requires current-task bookkeeping there. If Master must be edited, Result must report the exact reason and changed lines.

If a required production registry lives outside the allowed roots, read it if necessary but do not edit it. If editing outside the write boundary seems necessary, stop as `BLOCKED`.

## 11. Completion and commit

PASS 조건:

```text
MAP17_01 focused tests PASS
compile errors 0
relevant Console errors 0
Result includes user-facing implementation report
Result includes responsibility/scripts table
no regression runs unless explicitly triggered and reported
no Tilemap/Scene/Prefab/GameObject mutation
MAP17_02 remains LOCKED / NOT STARTED
```

PASS일 때만 status finalize를 수행한다.

Expected final status:

```text
MAP17_01_RESOLVE_ASSETS_AND_PLACE_GENERATED_CELLS: COMPLETE
MAP17_02_BUILD_TILEMAP_LAYERS_BAKE_AND_SEAM_VALIDATION: LOCKED
Current Task: NONE
```

Atomic commit subject:

```text
MAP17_01: resolve generated cell assets
```

Git push는 하지 않는다.

관련 없는 dirty worktree 변경은 수정하거나 stage하지 않는다.
