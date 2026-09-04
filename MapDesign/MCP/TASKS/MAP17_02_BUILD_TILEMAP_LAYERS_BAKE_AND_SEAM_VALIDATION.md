```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP17_02_BUILD_TILEMAP_LAYERS_BAKE_AND_SEAM_VALIDATION
  task_file: TASKS/MAP17_02_BUILD_TILEMAP_LAYERS_BAKE_AND_SEAM_VALIDATION.md
  requires_current_task: NONE
  requires_completed_task: MAP17_01_RESOLVE_ASSETS_AND_PLACE_GENERATED_CELLS
  requires_result:
    path: REPORTS/MAP17_01_RESOLVE_ASSETS_AND_PLACE_GENERATED_CELLS_RESULT.md
    status: PASS
    sha256: 1cb8a5cb86f5499639c64c94c8b5b59a6ad354c0aed88e67404f7acd2ae68776
  requires_installed_task:
    path: TASKS/MAP17_01_RESOLVE_ASSETS_AND_PLACE_GENERATED_CELLS.md
    sha256: 33a65e88a0d6df1946a1d3ff835970814536fc6737c94ebb892b8ae04e4526cb
  sets_current_task: MAP17_02_BUILD_TILEMAP_LAYERS_BAKE_AND_SEAM_VALIDATION
```

# MAP17_02 - Build Tilemap Layers Bake and Seam Validation

```text
TASK: MAP17_02_BUILD_TILEMAP_LAYERS_BAKE_AND_SEAM_VALIDATION
PHASE: MAP17 - Tilemap Bake / Streaming / Save Preparation
STATUS: CURRENT
NEXT: MAP17_03_IMPLEMENT_COLLIDER_CACHE_AND_RUNTIME_HANDLES
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP17_01의 in-memory placement plan을 실제 Tilemap component에 쓰기 전 단계의 **logical Tilemap layer bake packet**으로 변환한다.

이번 Task의 책임은 다음 세 가지다.

```text
1. sector-local 1536 cells를 7개 logical Tilemap layer buffer로 굽는다.
2. layer별 gap/overlap/duplicate와 source provenance 손실을 검증한다.
3. 4x4 MicroPattern seam과 12x8 MicroChunk seam 노출을 deterministic report로 분류한다.
```

이번 Task의 "bake"는 **Unity Scene의 Tilemap 컴포넌트에 쓰는 bake가 아니다.**

금지:

```text
Tilemap.SetTile / SetTiles / CompressBounds 호출
Tilemap asset mutation
Scene / Prefab mutation
Collider build or rebuild
GameObject / Prefab instantiate
streaming / save / load 구현
stable spawn id 생성
production seed 승인
```

MAP17_03이 runtime handle과 collider cache를 만들 수 있도록, 이번 Task는 순수 데이터 bake 결과와 seam 검증 결과만 넘긴다.

## 1. 사용자 보고 의무

Result의 첫 두 섹션은 반드시 아래 이름으로 작성한다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

`## User-Facing Implementation Report`에는 한국어로 다음을 설명한다.

```text
이번 Task가 추가한 기능
이번 Task가 만든 logical bake packet이 실제 Unity Tilemap write와 어떻게 다른지
어떤 seam/gap/overlap을 검증했는지
MAP17_03에 넘기는 산출물
회귀 테스트를 돌리지 않았는지, 돌렸다면 실제 트리거가 무엇이었는지
```

`## Responsibility and Added Scripts`에는 표로 다음을 작성한다.

| Script or file | Added or changed responsibility | Explicit non-ownership |
|---|---|---|
| 실제 파일 경로 | 이 파일이 맡은 책임 | 이 파일이 절대 하지 않는 일 |

Result에서 "테스트 PASS"만 쓰고 스크립트 책임 설명을 생략하면 이 Task는 `FAIL`이다.

## 2. 선행조건

작업 전에 다음을 확인한다.

```text
MAP17_01 Result exists
MAP17_01 Result STATUS: PASS
MAP17_01 Result SHA-256:
1cb8a5cb86f5499639c64c94c8b5b59a6ad354c0aed88e67404f7acd2ae68776

MAP17_01 installed task SHA-256:
33a65e88a0d6df1946a1d3ff835970814536fc6737c94ebb892b8ae04e4526cb

MAP17_01 placement digest:
d8dac9d9bf7c25b179cc2b33c6d0cf7b9323abd39de44b6ca2457216e23df334

MAP17_01 world projection digest:
5fb394e497fea2fa90e90177891dd5a971e3afa4af449e5be1935061fb6df8bf

Current Task before apply: NONE
MAP17_01: COMPLETE
MAP17_02: LOCKED before apply
MAP17_03: LOCKED
unrelated staged files: 0
```

선행 Result나 installed Task SHA가 다르면 임의로 맞추지 않는다. `BLOCKED`로 멈추고 실제 SHA를 Result에 기록한다.

## 3. 입력 계약

다음 MAP17_01/MAP16 산출물을 읽어 사용한다. 실제 타입명은 프로젝트의 현재 public API를 따른다.

```text
GeneratedTerrainGeometrySnapshot
BakingCanonicalDigest
GeneratedTerrainAssetRegistrySnapshot
GeneratedTerrainAssetResolution
GeneratedCellPlacementPlan
GeneratedCellPlacementPlanner
GeneratedCellPlacementDigest
GeneratedMicroChunkSliceSet
GeneratedMicroChunkMarkerSlotSet
```

기준 수량:

```text
sector width/height: 48/32
sector cells: 1536
logical final layers per cell: 7
sector layer records: 10752
micro chunk slices per sector: 16
micro chunk cells: 96
socket side signatures: 64
marker slots: 24
tile code registry entries: 12
prefab id registry entries: 24
```

MAP17_01에서 production Tile/Prefab registry가 아직 없다고 보고했으므로, 이번 Task도 그 사실을 보존한다. Reference registry는 focused proof용이며 production asset approval이 아니다.

## 4. 핵심 산출물

프로젝트 패턴에 맞게 파일과 타입을 조정할 수 있지만, 다음 semantic responsibility는 분명해야 한다.

| Required concept | Responsibility |
|---|---|
| `GeneratedTilemapLayerId` | 7개 final logical layer의 stable id |
| `GeneratedTilemapCellBakeRecord` | 한 layer/cell의 tile code, source, provenance, occupancy |
| `GeneratedTilemapLayerBuffer` | 한 layer의 1536-cell immutable buffer |
| `GeneratedTilemapBakeCommand` | 향후 Tilemap write adapter가 소비할 수 있는 pure data command |
| `GeneratedTilemapBakePlan` | 7개 layer buffer, socket/slot refs, diagnostics, digest |
| `GeneratedTilemapBakeFailure` | missing/duplicate/gap/overlap/seam/stale input 실패 reason |
| `GeneratedTilemapBakeResult` | success/failure wrapper |
| `GeneratedTilemapBakeDigest` | bake plan canonical digest |
| `GeneratedTilemapSeamCoordinate` | 4x4/12x8 seam edge coordinate |
| `GeneratedTilemapSeamExposure` | seam side pair의 material/tile/provenance exposure record |
| `GeneratedTilemapSeamReport` | pattern seam, microchunk seam, approved/unapproved discontinuity summary |
| `GeneratedTilemapLayerBaker` | placement plan을 logical tilemap layer bake plan으로 변환 |
| `GeneratedTilemapSeamValidator` | 4x4/12x8 seam exposure를 분류하고 forbidden discontinuity를 reject |

Suggested production files:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedTilemapLayerBakePlan.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedTilemapLayerBaker.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedTilemapSeamValidation.cs(.meta)
```

Suggested focused test file:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/GeneratedTilemapLayerBakerTests.cs(.meta)
```

파일을 더 적게 또는 다르게 구성해도 된다. 다만 Result의 `Responsibility and Added Scripts`에서 실제 구성과 책임을 정확히 보고해야 한다.

## 5. Logical Bake 규칙

### 5.1 Layer buffer

`GeneratedTilemapLayerBaker`는 MAP17_01 placement plan을 7개 logical Tilemap layer buffer로 변환한다.

필수:

```text
layer count: 7/7
records per layer: 1536/1536
total layer records: 10752/10752
unique layer-cell keys: 10752/10752
sector cell coverage: 1536/1536
missing layer-cell records: 0
duplicate layer-cell records: 0
out-of-bounds layer-cell records: 0
```

각 cell의 coordinate authority는 integer grid다. Unity world position, Transform, Camera, Tilemap origin은 이번 Task의 authority가 아니다.

### 5.2 Tile reference

`GeneratedTilemapCellBakeRecord`는 MAP17_01에서 해석한 tile code reference를 보존한다.

필수:

```text
tile code resolution reused from MAP17_01
prefab id resolution reused only as marker/slot reference
registry order does not affect bake digest
unresolved tile code fails atomically
unresolved prefab id in marker slot fails atomically
```

Unity `TileBase`, `Tilemap`, `Sprite`, `Prefab` object reference를 load하지 않는다. 만약 기존 프로젝트에 public production registry가 이미 있다면 read-only로만 확인하고, 이번 Task에서 등록/수정하지 않는다.

### 5.3 Gap / overlap

다음은 atomic failure다.

```text
same layer + same sector local index duplicate
missing required layer record
sector local index outside 0..1535
cell coordinate outside 48x32
layer id outside approved 7 layers
placement cell missing from logical bake
```

Overlap을 자동으로 resolve하지 않는다. MAP16/MAP17_01 provenance를 바꾸지 말고 실패 reason으로 보고한다.

### 5.4 Seam validation

4x4 MicroPattern seam과 12x8 MicroChunk seam을 별도로 계산한다.

Expected sector seam adjacency counts:

```text
4x4 MicroPattern seam adjacency pairs: 688
12x8 MicroChunk seam adjacency pairs: 240
4x4-only seam adjacency pairs: 448
```

정의:

```text
4x4 vertical boundaries: x = 4,8,12,16,20,24,28,32,36,40,44 over 32 rows
4x4 horizontal boundaries: y = 4,8,12,16,20,24,28 over 48 columns
12x8 vertical boundaries: x = 12,24,36 over 32 rows
12x8 horizontal boundaries: y = 8,16,24 over 48 columns
```

Seam report는 최소 다음을 분리한다.

```text
approved continuous pair
approved material transition pair
socket/opening pair
protected route seam pair
unapproved solid/air discontinuity
unapproved hazard/protection discontinuity
unapproved provenance break
missing neighbor pair
out-of-bounds pair
```

Forbidden seam exposure가 있으면 repair하지 않고 실패한다.

### 5.5 Digest

`BakingCanonicalDigest`를 사용해서 logical bake digest와 seam report digest를 만든다.

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
Unity Tilemap component write
Tilemap.SetTile / SetTiles / SetTilesBlock / ClearAllTiles / CompressBounds
Tile asset mutation
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
MAP17_03 unlock or execution
PlayMode tests
legacy 19347 regression
unfiltered test runs
full regression runs
```

## 7. Focused-only 검증 정책

정상 검증은 EditMode category `MAP17_02`만 선택한다.

```text
MAP17_02 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15/MAP16/MAP17_01 selections: 0
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
LayerBakePlanPublishesSevenDeterministic1536CellBuffers
BakeConsumesPlacementPlanWithoutReloadingAssetsOrScenes
TileCodesResolveToLayerCellsWithoutUnityTilemapMutation
OverlapGapDuplicateAndOutOfBoundsCellsFailAtomically
MicroPatternAndMicroChunkSeamsAreEnumeratedSeparately
SeamValidationRejectsUnapprovedDiscontinuitiesWithoutRepair
SocketMarkerSlotAndProvenanceReferencesSurviveBakeHandoff
BakeAndSeamDigestsAreStableAcrossRepeatReverseCultureAndRegistryOrder
BakerDoesNotSetTilesBuildCollidersInstantiatePrefabsOrWriteFiles
Map17HandoffKeepsMap17_03Locked
```

Expected focused result:

```text
mode: EditMode
category_names: [MAP17_02]
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
MAP17_01 placement digest reused:
MAP17_01 world projection digest reused:
source placement cells observed: 1536/1536
source placement layer refs observed: 10752/10752
source tile code registry refs observed/resolved/missing: 12/12/0
source prefab id registry refs observed/resolved/missing: 24/24/0
source socket side signatures preserved: 64/64
source marker slots preserved: 24/24

logical tilemap layer count: 7/7
logical records per layer: 1536/1536 each
logical total layer records: 10752/10752
unique layer-cell keys: 10752/10752
sector cell coverage: 1536/1536
missing/duplicate/out-of-bounds layer records: 0/0/0
forbidden overlap/gap failures detected by probes:

4x4 MicroPattern seam adjacency pairs: 688/688
12x8 MicroChunk seam adjacency pairs: 240/240
4x4-only seam adjacency pairs: 448/448
approved seam pairs:
unapproved seam pairs: 0
missing/out-of-bounds seam neighbor pairs: 0/0
seam failure probes passed:

logical bake digest lower-hex SHA-256: YES
logical bake digest:
seam report digest lower-hex SHA-256: YES
seam report digest:
repeat/reverse/culture/registry-order digest mismatches: 0/0/0/0
mutation sensitivity probes passed:

Unity Tilemap component writes: 0
Tilemap.SetTile/SetTiles/SetTilesBlock/ClearAllTiles calls: 0/0/0/0
Tilemap bakes to Scene: 0
collider rebuilds: 0
GameObject/Prefab instantiation: 0/0
Scene/Prefab/Tilemap mutation: 0/0/0
Generated CSV/assets committed: 0
stable spawn ids created: 0
runtime objects spawned: 0
production seed approvals: 0
MAP17_03 started: NO
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
MapDesign/MCP/TASKS/MAP17_02_BUILD_TILEMAP_LAYERS_BAKE_AND_SEAM_VALIDATION.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
MapDesign/MCP/REPORTS/MAP17_02_BUILD_TILEMAP_LAYERS_BAKE_AND_SEAM_VALIDATION_RESULT.md
```

Allowed read-only references:

```text
MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST_V2.md
MapDesign/MCP/REPORTS/MAP17_01_RESOLVE_ASSETS_AND_PLACE_GENERATED_CELLS_RESULT.md
Assets/_Game/Map/Runtime/WorldGeneration/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/
```

Do not edit Master unless the current MCP protocol requires current-task bookkeeping there. If Master must be edited, Result must report the exact reason and changed lines.

If a required production registry or existing Tilemap adapter lives outside the allowed roots, read it if necessary but do not edit it. If editing outside the write boundary seems necessary, stop as `BLOCKED`.

## 11. Completion and commit

PASS 조건:

```text
MAP17_02 focused tests PASS
compile errors 0
relevant Console errors 0
Result includes user-facing implementation report
Result includes responsibility/scripts table
logical bake packet and seam report created
no Unity Tilemap component write or Scene bake
no regression runs unless explicitly triggered and reported
no Scene/Prefab/GameObject mutation
MAP17_03 remains LOCKED / NOT STARTED
```

PASS일 때만 status finalize를 수행한다.

Expected final status:

```text
MAP17_02_BUILD_TILEMAP_LAYERS_BAKE_AND_SEAM_VALIDATION: COMPLETE
MAP17_03_IMPLEMENT_COLLIDER_CACHE_AND_RUNTIME_HANDLES: LOCKED
Current Task: NONE
```

Atomic commit subject:

```text
MAP17_02: build logical tilemap bake layers
```

Git push는 하지 않는다.

관련 없는 dirty worktree 변경은 수정하거나 stage하지 않는다.

