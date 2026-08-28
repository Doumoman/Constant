```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP11_01_IMPLEMENT_CLUSTER_FOOTPRINT_AND_LOCAL_CANVAS
  task_file: TASKS/MAP11_01_IMPLEMENT_CLUSTER_FOOTPRINT_AND_LOCAL_CANVAS.md
  requires_current_task: NONE
  requires_completed_task: MAP10_08_MAP10_PATTERN_EXIT_TESTS
  requires_result:
    path: REPORTS/MAP10_08_MAP10_PATTERN_EXIT_TESTS_RESULT.md
    status: PASS
    sha256: 3d71ab4e6186e7a8633a7f99be6ebdc2e46bbb17d97c53e86cce6c1bbec93e19
  requires_installed_task:
    path: TASKS/MAP10_08_MAP10_PATTERN_EXIT_TESTS.md
    sha256: fddf4c0c51064bee911f72bff2f1161720cc76769514fbabe98bbf47e6e49b3e
  sets_current_task: MAP11_01_IMPLEMENT_CLUSTER_FOOTPRINT_AND_LOCAL_CANVAS
```

# MAP11_01 — Implement Cluster Footprint and Local Canvas

```text
TASK: MAP11_01_IMPLEMENT_CLUSTER_FOOTPRINT_AND_LOCAL_CANVAS
PHASE: MAP11 — TerrainCluster Authoring / Compilation
STATUS: CURRENT
NEXT: MAP11_02_IMPLEMENT_CLUSTER_ROLES_AND_SOCKET_CONTRACT
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Responsibility

이번 Task는 MAP09_04의 validated `TerrainCluster` footprint를 **변환된 active/inactive chunk mask와 tile-addressable local Canvas**로 컴파일한다.

```text
validated TerrainCluster footprint
→ requested footprint transform
→ normalized chunk bounds
→ active/inactive chunk mask
→ exact local tile mask layer
→ immutable compiled footprint artifact
```

| 소유 | 소유하지 않음 |
|---|---|
| footprint transform과 normalization | role anchor/socket projection |
| chunk/tile bounds와 active/inactive mask | Spine/Envelope 자동 컴파일 |
| source→compiled coordinate mapping | 기본·고점·복구 경로 |
| immutable local Canvas mask와 digest | MicroPattern 선택·렌더링 |
| 2~5 및 allowlisted 6 footprint 소비 | starter 16종 authoring |

이번 Task의 Local Canvas는 **footprint mask 좌표계**다. Solid/Air shell, Surface, Affordance, Material, Hazard, Marker 값을 아직 저작하거나 추론하지 않는다.

## 1. No-Regression Policy

정상 실행은 category `MAP11_01`만 선택한다.

```text
MAP11_01 focused selection: required
Prior MAP09/MAP10 selections: 0
Legacy 19347 selections: 0
PlayMode selections: 0
```

다음 실제 trigger가 있을 때만 관련 최소 범위를 별도로 판단한다.

- compile/Console error가 기존 authority에서 발생
- MAP09_04 footprint public behavior 또는 MAP10 Exit authority drift
- 기존 production/test/CSV/meta 파일의 예상 밖 변경
- asmdef/GUID/namespace ownership 위반

Task-owned 구현이나 focused assertion 결함은 이번 파일만 고치고 `MAP11_01`만 재실행한다. 기존 authority 결함이 확인되면 이전 파일을 수정하지 말고 owner·원인·최소 범위를 기록한 뒤 `STATUS: BLOCKED`로 STOP한다.

## 2. Preflight Authorities

실행 전에 다음을 read-only로 확인한다.

1. MAP10_08 Result status/SHA와 installed/archive Task SHA exact
2. MAP10 Phase Exit `APPROVED`, MAP11_01만 CURRENT, inbox candidate 0
3. MAP09_04 `TerrainClusterId`, `ClusterChunkCoord`, `ClusterFootprint`, validator, canonical digest API
4. standard active chunk `2..5`, exact ID allowlisted `6`, normalized 4-neighbor connectivity authority
5. existing `LocalTileCoord`와 `MicroChunk=12×8` constants
6. MAP09_06 resolved SectorCanvas ownership은 final 48×32 Canvas이며 이번 local mask와 별개임
7. approved TerrainClusters Runtime/Test roots and assemblies
8. MAP10 content `24/453`, Authoring inventory/hash, Generated CSV 0
9. compile/Console, meta/GUID, dirty/staged paths

다음이면 `BLOCKED`다.

- predecessor 또는 MAP10 Exit 불일치
- MAP09_04 footprint authority 수정/복제 없이는 구현 불가
- 기존 TerrainCluster compiler/local-canvas type이 다른 의미로 충돌
- task allowlist가 사용자 변경과 겹침

## 3. Exact Implementation Location

신규 파일만 허용한다.

```text
Runtime:
Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/TerrainClusterFootprintTransform.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/TerrainClusterLocalCanvas.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/TerrainClusterFootprintCompiler.cs(.meta)

Focused test:
Assets/_Game/Tests/EditMode/Map/WorldGeneration/TerrainClusters/TerrainClusterFootprintCompilerTests.cs(.meta)

Namespace:
StarNight.Map.WorldGeneration.TerrainClusters

Assembly:
Game.Map.Runtime / Game.Map.Tests.EditMode
```

위와 같은 책임을 더 적은 신규 C# 파일로 안전하게 구현할 수는 있으나, 기존 production/test 파일 수정은 허용하지 않는다. 실제 파일 inventory와 이유를 Result에 기록한다.

## 4. Footprint Compile Input

compiler는 다음 semantic input을 받는다.

```text
validated TerrainCluster identity/footprint authority
requested ClusterFootprintTransform
exact six-chunk TerrainClusterId allowlist
```

exact transforms:

```text
R0
MirrorX
MirrorY
R180
```

- undefined transform, R90/R270, arbitrary rotation/scale/translation은 거부한다.
- MAP10의 4×4 `MicroPatternTransform`을 수정하거나 cluster transform authority로 재사용하지 않는다.
- MAP09_04의 count/allowlist/connectivity 검증을 통과하지 않은 footprint는 compile하지 않는다.
- invalid input은 partial mask/Canvas/digest를 publish하지 않는다.
- compiler는 RNG, weight, biome, role, port, SpineVariant를 선택하지 않는다.

## 5. Chunk Bounds and Transform

source footprint의 normalized bounding rectangle을 다음과 같이 계산한다.

```text
ChunkWidth  = maxChunkX + 1
ChunkHeight = maxChunkY + 1
TileWidth   = ChunkWidth  * 12
TileHeight  = ChunkHeight * 8
```

source normalized chunk `(x,y)`의 transform:

| Transform | Compiled chunk coordinate |
|---|---|
| `R0` | `(x, y)` |
| `MirrorX` | `(ChunkWidth - 1 - x, y)` |
| `MirrorY` | `(x, ChunkHeight - 1 - y)` |
| `R180` | `(ChunkWidth - 1 - x, ChunkHeight - 1 - y)` |

source local tile `(x,y)`에도 같은 bounds 기준을 적용한다.

| Transform | Compiled local tile coordinate |
|---|---|
| `R0` | `(x, y)` |
| `MirrorX` | `(TileWidth - 1 - x, y)` |
| `MirrorY` | `(x, TileHeight - 1 - y)` |
| `R180` | `(TileWidth - 1 - x, TileHeight - 1 - y)` |

규칙:

- transform 전후 bounds 크기는 동일하다.
- compiled active chunks는 unique, canonical `(y,x)` order, 4-neighbor connected다.
- transform은 active/inactive 의미와 exact cell mass를 보존한다.
- 같은 involution을 두 번 적용하면 source coordinate/mask로 돌아온다.
- coordinate mapping은 source chunk/tile과 compiled chunk/tile을 양방향 조회할 수 있어야 한다.
- input enumeration/culture와 무관한 결과를 낸다.

## 6. Active/Inactive Chunk Mask

compiled bounds 안의 모든 chunk coordinate를 exact 한 번 게시한다.

```text
ClusterChunkMaskState:
Active
Inactive
```

- transformed footprint에 포함된 chunk만 `Active`다.
- bounding rectangle 안이지만 footprint에 없는 chunk는 explicit `Inactive`다.
- active count는 source footprint count와 같아야 한다.
- inactive count는 `ChunkWidth * ChunkHeight - active count`다.
- bounds 밖 좌표는 mask에 존재하지 않는다.
- active chunk `2..5`는 표준이며 `6`은 exact caller allowlist일 때만 성공한다.
- `1`, `7+`, disconnected, diagonal-only, duplicate, negative, unnormalized input은 publish하지 않는다.

## 7. Local Tile Mask Layer

Local Canvas는 bounds의 모든 tile coordinate를 exact 한 번 게시한다.

```text
Cell count = TileWidth * TileHeight
Canonical index = y * TileWidth + x
```

각 immutable cell은 최소 다음을 가진다.

```text
LocalTileCoord compiled coordinate
ClusterChunkCoord owning compiled chunk
LocalTileCoord within-chunk coordinate (x 0..11, y 0..7)
ClusterChunkMaskState Active | Inactive
source chunk coordinate
source local tile coordinate
```

- active chunk의 96개 tile은 모두 `Active`, inactive chunk의 96개 tile은 모두 `Inactive`다.
- active tile count는 `activeChunkCount * 96`이다.
- inactive tile에는 geometry/payload/protection owner를 임의로 부여하지 않는다.
- Local Canvas는 final `SectorCanvasContract`, Generated Slice, Tilemap이 아니다.
- 이 단계의 유일한 tile layer 의미는 footprint `Active/Inactive` mask다.
- 후속 Task가 roles/spine/shell/pattern을 얹을 수 있도록 coordinate lookup과 immutable source mapping을 제공한다.

## 8. Publication, Errors, and Digest

최소 semantic surface:

```text
ClusterFootprintTransform
ClusterChunkMaskState
CompiledClusterChunkCell
CompiledClusterLocalTileCell
TerrainClusterLocalCanvas
TerrainClusterFootprintCompileRequest
TerrainClusterFootprintCompileError / Result
TerrainClusterFootprintCompiler
```

이름은 기존 codebase naming과 충돌할 경우 의미를 유지하는 최소 조정이 가능하며 Result에 exact public surface를 기록한다.

publication rules:

- 모든 collection defensive copy/read-only
- errors accumulated, deduplicated, stable-sorted
- failure에서 partial chunk cells/tile cells/mapping/digest `0`
- successful digest는 ruleset version, cluster ID, source footprint digest/coordinates, transform, bounds, every chunk state, every tile state, bidirectional mapping을 포함
- display text, timestamp, locale, object hash, input/reflection/file order는 제외
- reversed source/allowlist enumeration은 같은 artifact/digest

최소 error distinctions:

```text
MissingInput
InvalidSourceFootprint
SixChunkNotAllowlisted
InvalidTransform
InvalidChunkBounds
TransformMappingMismatch
DisconnectedCompiledFootprint
MissingOrDuplicateChunkCell
ChunkMaskCountMismatch
MissingOrDuplicateTileCell
TileChunkMappingMismatch
TileMaskCountMismatch
NonCanonicalPublication
```

## 9. Exact Non-Ownership

이번 Task에서 금지:

- MAP09_04 existing contracts/validator/digest 수정 또는 duplicate authority
- MAP10 production/test/CSV 수정과 Pattern renderer 호출
- role anchor, Entry/Exit port, sector socket projection
- SpineVariant 선택, movement graph, envelope set 자동 생성
- base/high/recovery route 또는 physics/reachability witness
- Solid/Air shell, terrain density, cleanup, pattern zone
- starter TerrainCluster/CSV/Authoring/Generated 제작
- Sector placement, SpecialRegion, Activity/Event 조립
- final 48×32 SectorCanvas, 12×8 slice, Tilemap/Scene/Prefab/SO
- EditorWindow, PlayMode, WorldGenerationRoot wiring
- asmdef/asmref, Settings/Packages 변경
- 문제 trigger 없는 이전/legacy test 실행
- unrelated path 수정/stage/commit, Git push

신규 Runtime scope 금지 symbol:

```text
UnityEditor
StageMapGenerator
GridWorld
RoomTemplate
RoomGridTransform
TileMutationService
SectorRecipeResolver
System.Random
UnityEngine.Random
```

## 10. Focused Verification

category `MAP11_01`만 실행하며 최소 다음을 직접 검증한다.

1. valid connected footprint sizes 2, 3, 4, 5 compile
2. exact allowlisted 6 success and non-allowlisted 6 rejection
3. 1/7+, duplicate/negative/unnormalized/disconnected/diagonal rejection
4. irregular footprint explicit inactive chunk publication
5. exact chunk bounds and tile dimensions
6. R0/MirrorX/MirrorY/R180 chunk mappings
7. all transforms tile mappings and bounds
8. identity/involution and 4-neighbor connectivity preservation
9. exact chunk active/inactive counts
10. exact tile count and active/inactive `count * 96`
11. tile→chunk/within-chunk/source round-trip
12. canonical ordering and reversed-input determinism
13. immutable publication and deterministic digest
14. atomic accumulated failure with partial output 0
15. no role/spine/shell/pattern/Canvas/Tilemap side effects

Focused assertion/fixture가 실패하면 task-owned 원인을 고친 뒤 `MAP11_01`만 다시 실행한다.

## 11. Static Gates

```text
Unity compile / Console error / relevant warning: 0 / 0 / 0
MAP11_01 focused: all discovered executed and PASS; skip/inconclusive 0
MAP10_08 Result SHA: 3d71ab4e... exact
MicroPattern definitions / physical rows: 24 / 453 unchanged
Catalog CSV SHA: f9d9e9cc... unchanged
Cells CSV SHA: e702ae5d... unchanged
Full 52-file Authoring manifest: 4415ae4a... unchanged
Generated CSV: 0
existing MAP00~MAP10 production/test/CSV/meta modifications: 0
other roots/asmdef/Scene/Prefab/Settings/Packages changes: 0
new C#/meta valid; duplicate GUID 0
unapplied candidate / diff-check / unrelated staged paths: 0 / 0 / 0
```

## 12. Required Result

```text
MAP11_01_IMPLEMENT_CLUSTER_FOOTPRINT_AND_LOCAL_CANVAS_RESULT.md
```

상단:

```text
TASK: MAP11_01_IMPLEMENT_CLUSTER_FOOTPRINT_AND_LOCAL_CANVAS
STATUS: PASS | BLOCKED
MAP11_01: COMPLETE ELIGIBLE | NOT COMPLETE
MAP11_02_IMPLEMENT_CLUSTER_ROLES_AND_SOCKET_CONTRACT: LOCKED / DO NOT START
```

첫 구현 섹션은 반드시 `Responsibility and Added Functions`다.

| Field | Required report |
|---|---|
| Task responsibility | validated footprint를 transformed mask/local Canvas로 compile |
| Added functions | transform, bounds, chunk/tile mask, mapping, result/digest의 실제 기능 |
| Inputs consumed | MAP09_04 footprint authority, 12×8 constants, LocalTileCoord |
| Outputs produced | immutable compiled footprint/local Canvas 또는 atomic errors |
| Explicit non-ownership | roles/spine/shell/pattern/starter/final Canvas 미구현 |
| Downstream consumers | MAP11_02 role/socket projection, MAP11_03 spine/envelope projection |

이후 다음을 실제 증거로 기록한다.

1. predecessor/Status/preflight
2. new file inventory와 public surface
3. transform/bounds/mapping 표
4. standard/allowlisted footprint 및 connectivity 결과
5. chunk/tile active/inactive count
6. immutability/digest/error atomicity
7. focused/no-regression policy
8. static/change scope
9. commit handoff

```text
MAP11_01 focused: discovered/executed/pass/fail/skip/inconclusive
REGRESSION TRIGGER DETECTED: NO | YES(owner/reason/minimum scope)
PRIOR TASK TEST SELECTIONS: 0 (normal path)
LEGACY TEST SELECTIONS: 0 (normal path)
PLAYMODE TEST SELECTIONS: 0
```

PASS일 때만 Status Finalize 후 task-owned production/test/meta/protocol 파일만 atomic commit한다.

```text
Subject: MAP11_01: implement cluster footprint local canvas
Push: NOT PERFORMED
```

Result가 PASS여도 MAP11_02를 자동 시작하지 않는다. 사용자가 Result를 전달하고 별도 검수받을 때까지 계속 LOCKED다.
