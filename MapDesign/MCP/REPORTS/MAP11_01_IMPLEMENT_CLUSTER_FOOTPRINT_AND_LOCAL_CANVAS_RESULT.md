TASK: MAP11_01_IMPLEMENT_CLUSTER_FOOTPRINT_AND_LOCAL_CANVAS
STATUS: PASS
MAP11_01: COMPLETE ELIGIBLE
MAP11_02_IMPLEMENT_CLUSTER_ROLES_AND_SOCKET_CONTRACT: LOCKED / DO NOT START

## Responsibility and Added Functions

| Field | Report |
|---|---|
| Task responsibility | MAP09_04가 validated한 TerrainCluster identity/footprint를 요청 transform에 따라 active/inactive chunk mask와 tile-addressable local footprint Canvas로 compile한다. |
| Added functions | exact R0/MirrorX/MirrorY/R180 transform, normalized chunk/tile bounds, exhaustive chunk/tile mask publication, source↔compiled lookup, immutable result/error publication, deterministic footprint/artifact digest를 추가했다. |
| Inputs consumed | 기존 `TerrainClusterContractValidator`, `TerrainClusterId`, `ClusterFootprint`, `ClusterChunkCoord`, `LocalTileCoord`, `WorldGenConstants`의 12×8/96 authority와 exact six-chunk ID allowlist를 소비한다. |
| Outputs produced | 성공 시 immutable `TerrainClusterLocalCanvas`; 실패 시 stable-sorted/deduplicated errors와 chunk/tile/mapping/digest가 0인 atomic failure를 게시한다. |
| Explicit non-ownership | role/socket projection, Spine/envelope/route, shell/pattern, starter content, final 48×32 SectorCanvas, Tilemap/Scene/Prefab/SO는 구현하거나 호출하지 않았다. |
| Downstream consumers | MAP11_02 role/socket projection과 MAP11_03 spine/envelope projection이 compiled coordinate lookup과 footprint mask를 소비할 수 있다. |

## Predecessor, Status, and Apply Evidence

The only immediate `MCP_INBOX` Markdown candidate passed the `single_task_v1` identity, predecessor, hash, Status/Master membership, collision, encoding, and clean-staging gates before Task execution. The installed and archived copies are byte-identical to the candidate.

```text
Preflight HEAD: 8c5cc4ce8774308c3f6f60042256ed371ea4d37d
MAP10_08 Result status / phase exit: PASS / APPROVED
MAP10_08 Result required/actual SHA-256: 3d71ab4e6186e7a8633a7f99be6ebdc2e46bbb17d97c53e86cce6c1bbec93e19
MAP10_08 installed/archive Task SHA-256: fddf4c0c51064bee911f72bff2f1161720cc76769514fbabe98bbf47e6e49b3e
MAP11_01 inbox/installed/archive SHA-256: 73871d0fda4e1dc7c57d2c3238ce02430b40f747662a2f224793915edc6cd8b0
Patch status delta: COMPLETE 0 / CURRENT +1 / LOCKED -1 / total rows 215 unchanged
Immediate MCP_INBOX Markdown candidates after apply: 0
Legacy unapplied candidates after apply: 0
Pre-apply staged paths: 0
Current Task during execution: MAP11_01_IMPLEMENT_CLUSTER_FOOTPRINT_AND_LOCAL_CANVAS
MAP11_02 status during execution: LOCKED
```

## New File Inventory and Public Surface

Exactly four new C# files and their four Unity-generated matching meta files were added. No existing production or test file was modified.

| File | Responsibility | SHA-256 |
|---|---|---|
| `TerrainClusterFootprintTransform.cs` | transform and Active/Inactive enums plus internal exact transform utility | `20109fe2d146ca264bc79415a2c0e16ed0c83fae91dd9e59520ed60eca8f38c7` |
| `TerrainClusterLocalCanvas.cs` | immutable cells, Canvas lookups, request, errors, and atomic result | `23de3d409b92e7a041e86d99cb7245443e9ccd14148f58232265c196572b0dec` |
| `TerrainClusterFootprintCompiler.cs` | MAP09_04 validation consumption, bounds/mask/mapping compilation, invariant gates, digest | `9573f009c8dbf23a847dba30374a64672750ed6bddc98f9fa5b3f5644b5e6fde` |
| `TerrainClusterFootprintCompilerTests.cs` | MAP11_01-only focused contract/invariant/determinism coverage | `1efac258c9a71e6a61f4b27701b3b01e595b13c460bac9ef443bdc69e1165f7b` |

Unity reflection confirmed the new public types in `Game.Map.Runtime`:

```text
ClusterFootprintTransform
ClusterChunkMaskState
CompiledClusterChunkCell
CompiledClusterLocalTileCell
TerrainClusterLocalCanvas
TerrainClusterFootprintCompileRequest
TerrainClusterFootprintCompileErrorCode / TerrainClusterFootprintCompileError
TerrainClusterFootprintCompileResult
TerrainClusterFootprintCompiler.Compile
```

`TerrainClusterLocalCanvas` publishes exact dimensions and read-only chunk/tile collections plus `TryGetChunkCell`, `TryGetTileCell`, `TryGetCompiledChunk`, `TryGetSourceChunk`, `TryGetCompiledTile`, and `TryGetSourceTile`. `TerrainClusterFootprintCompileResult` publishes `IsSuccess`, `LocalCanvas`, errors, zero-on-failure collection/mapping surfaces, and `CanonicalDigest`.

## Transform, Bounds, and Mapping

The compiler invokes the existing MAP09_04 `TerrainClusterContractValidator`; it does not duplicate count, allowlist, normalization, or connectivity authority. Successful source chunks are already canonical and validated before any output is built.

| Transform | Source chunk `(x,y)` → compiled | Source local tile `(x,y)` → compiled |
|---|---|---|
| R0 | `(x,y)` | `(x,y)` |
| MirrorX | `(ChunkWidth-1-x,y)` | `(TileWidth-1-x,y)` |
| MirrorY | `(x,ChunkHeight-1-y)` | `(x,TileHeight-1-y)` |
| R180 | `(ChunkWidth-1-x,ChunkHeight-1-y)` | `(TileWidth-1-x,TileHeight-1-y)` |

```text
ChunkWidth / ChunkHeight: max source x + 1 / max source y + 1
TileWidth / TileHeight: ChunkWidth * 12 / ChunkHeight * 8
Chunk canonical index: y * ChunkWidth + x
Tile canonical index: y * TileWidth + x
Transform bounds size change: 0
R0/MirrorX/MirrorY/R180 source↔compiled round-trip failures: 0
Transformed 4-neighbor connectivity failures: 0
Undefined transform publication: rejected atomically
```

## Footprint, Mask, and Connectivity Evidence

| Fixture | Chunk bounds | Chunk total / active / inactive | Tile bounds | Tile total / active / inactive | Result |
|---|---:|---:|---:|---:|---|
| standard 2 | 2×1 | 2 / 2 / 0 | 24×8 | 192 / 192 / 0 | PASS |
| standard 3 | 3×1 | 3 / 3 / 0 | 36×8 | 288 / 288 / 0 | PASS |
| standard 4 | 4×1 | 4 / 4 / 0 | 48×8 | 384 / 384 / 0 | PASS |
| standard 5 | 5×1 | 5 / 5 / 0 | 60×8 | 480 / 480 / 0 | PASS |
| exact allowlisted 6 | 6×1 | 6 / 6 / 0 | 72×8 | 576 / 576 / 0 | PASS |
| irregular `(0,0)(1,0)(2,0)(0,1)` | 3×2 | 6 / 4 / 2 | 36×16 | 576 / 384 / 192 | PASS |

The same six-chunk contract without its exact ID in the caller allowlist returned `SixChunkNotAllowlisted` with no output. Count 1, count 7, duplicate, negative, shifted/unnormalized, disconnected, and diagonal-only footprints all returned atomic `InvalidSourceFootprint`. Every active chunk owns exactly 96 Active tiles, every explicit inactive chunk owns exactly 96 Inactive tiles, and bounds-outside lookups return false without implicit cells.

## Immutability, Digest, and Error Atomicity

- Request allowlists are defensively copied, deduplicated, and ordinally sorted.
- Chunk/tile cells and all Canvas collections are immutable/read-only; mutation attempts throw `NotSupportedException`.
- Source and compiled coordinate dictionaries provide exact bidirectional round trips for every chunk and tile in the normalized rectangle.
- Errors are accumulated, deduplicated, and stable-sorted by code/path/detail.
- Missing source plus undefined transform published both ordered errors and exactly 0 chunk cells, tile cells, mappings, and digest.
- The source-footprint digest includes ruleset, cluster ID, 12×8 constants, and canonical source coordinates.
- The artifact digest includes ruleset, cluster ID, source-footprint digest/coordinates, transform, bounds, every chunk/tile state, and both mapping directions.
- Reversed footprint enumeration, changed display text, and `tr-TR` versus `ko-KR` culture produced the same source/artifact digest. Changing the transform changed only the artifact digest while preserving mass.

## Focused Verification and No-Regression Policy

Only the `MAP11_01` EditMode category in `Game.Map.Tests.EditMode` was selected. The initial launch exposed a task-owned compile gate: this project's NUnit surface does not provide `[NonParallelizable]`. The single unsupported test attribute was removed; no existing authority or non-task file was changed. The same MAP11_01 category alone then compiled and passed.

```text
Initial MAP11_01 focused compile-gated launch: discovered 0 / executed 0 / passed 0 / failed 0 / skipped 0 / inconclusive 0
Initial task-owned compile diagnostics: 2 (missing NonParallelizableAttribute / NonParallelizable)
Final MAP11_01 focused: discovered 23 / executed 23 / passed 23 / failed 0 / skipped 0 / inconclusive 0
REGRESSION TRIGGER DETECTED: YES (owner: MAP11_01 task-owned fixture; reason: unsupported NUnit attribute; minimum scope: remove that attribute and rerun MAP11_01 only)
PRIOR TASK TEST SELECTIONS: 0
LEGACY TEST SELECTIONS: 0
PLAYMODE TEST SELECTIONS: 0
```

## Static and Change-Scope Gates

```text
Unity Version: 6000.3.8f1
Final Unity compile errors: 0
Final Unity Console errors: 0
Final relevant Unity warnings: 0
Final Console error/warning entries after clear and recheck: 0
MAP11_01 focused tests: 23 / 23 PASS
MicroPattern definitions / physical rows: 24 / 453 unchanged
Catalog CSV SHA-256: f9d9e9cc60c4e4d7561c5aa6502228c18fc9566e3e0febab206ea3264b408267 unchanged
Cells CSV SHA-256: e702ae5d02d7ec9d2cda129c1361699e37d942c280c8f9bd1f3200f155084381 unchanged
Authoring CSV inventory: 52 unchanged
Full Authoring manifest: 4415ae4af5196d6793f5d0152c0688e5bf35dc4ad23442791e45d3cfd81d0851 unchanged
Generated CSV: 0
Valid asset GUID rows: 3917
Duplicate GUID groups / missing asset metas: 0 / 0
New C# / matching meta: 4 / 4
Existing MAP00 through MAP10 production/test/CSV/meta modifications: 0
Other roots/asmdef/Scene/Prefab/Settings/Packages changes: 0
Unapplied candidate / unrelated staged paths: 0 / 0
```

New Unity GUIDs:

```text
TerrainClusterFootprintTransform.cs.meta: aca345162eb092f42b1cae1121137882
TerrainClusterLocalCanvas.cs.meta: 1632cd6ee89d52941b8be72f37649da3
TerrainClusterFootprintCompiler.cs.meta: 3026e5211f11999489a6fdc87f0557b0
TerrainClusterFootprintCompilerTests.cs.meta: a3d95bf1a761ca448879edb284f43aed
```

## Commit Handoff

```text
Subject: MAP11_01: implement cluster footprint local canvas
Commit scope: three Runtime C#/meta, one focused test C#/meta, installed/archive Task, this Result, finalized Status
Atomic commit actor: SELF
Push: NOT PERFORMED
Next task: MAP11_02 remains LOCKED / DO NOT START
```
