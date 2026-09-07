TASK: VIS01_BUILD_MOONPALACE_GRAYBOX_EXAMPLE_V1
STATUS: PASS

# User-Facing Implementation Report

이번 작업으로 Unity에서 직접 열 수 있는 격리 Scene `Assets/_Game/Map/Scenes/MoonPalace/VIS01/MoonPalaceGrayboxExample_VIS01.unity`를 만들었다. Scene root는 `MoonPalace_Graybox_VIS01`이고, 48x32 terrain Tilemap, route/recovery Tilemap, marker/protection/4x4·12x8 boundary Tilemap, orthographic camera, in-scene legend, seed/sector/digest metadata를 포함한다. 기존 Scene이나 Prefab은 열거나 저장하거나 수정하지 않았고 Build Settings에도 추가하지 않았다.

`MoonPalaceRuntimeCatalogSnapshot`은 MAP21_01~11의 실제 production CSV 14개를 RFC4180으로 읽었다. 관측값은 biome 4, tile code 10, production MicroPattern 24, production pattern cell 384, terrain cluster 48, spine variant 96, activity 7, event overlay 5, biome pair 6, boundary candidate 48, core resource region 3, density window 4, `MP_QA_01=1924737067`이다. 각 source path, record count, header hash, content SHA-256, typed/FK/duplicate 오류를 기록했고 오류 및 source mutation은 모두 0이다.

4x4 후보는 `0x0000..0xFFFF` 65,536개를 전부 열거했다. all-solid, 허용 범위 밖 open count, largest open component 4 미만, isolated one-cell open pocket, socket/support 부재, duplicate/out-of-bounds를 명시적으로 거른 뒤 21,104개 eligible mask에서 route-capability reserve와 density/socket/silhouette/mirror/support/detail diversity bucket round-robin을 사용해 500개를 선택했다. 숫자가 작은 mask 500개를 취하지 않았다. 선택군은 density 4 bucket, edge socket 9 bucket, silhouette 477개, mirror family 458개, support class 2개, detail eligibility 2개를 포함한다.

기존 MAP21_02 24개는 source-authored presentation starter pattern으로 그대로 유지된다. VIS01 500개는 graybox 구조 mask이며, 각 4x4 placement는 실제 mask identity와 `IDENTITY_NO_ROTATION`을 보존하면서 seed 기반으로 MAP21_02 family 하나를 presentation link로 가진다. MAP21_02 CSV rewrite는 0이다.

각 12x8 MicroChunk는 3x2 slot의 generated 4x4 candidate 여섯 개를 직접 선택한다. 아래쪽 세 pattern의 north socket과 위쪽 세 pattern의 south socket이 두 수평 경로를 만들고, 가운데 column-capable pattern 두 개가 수직 연결을 만든다. 완성된 96-cell map에서 entry→exit BFS, required/recovery/protected cell 개방을 검증한다. 각 chunk의 composition attempt는 정확히 1이고, invalid reroll, fallback carve, silent repair는 없다. 16개 chunk는 4x4로 배치되어 48x32 sector를 이루며 수평·수직 인접 socket seam을 다시 검증한다.

`MP_QA_01 / 1924737067`과 sector `(6,6)`은 candidate slot, biome, terrain cluster, spine variant, MAP21_02 family, tile code, boundary, activity/event marker 선택의 deterministic integer mixing material로 사용됐다. 결과는 open 1,249 / solid 287 cell이며 required 304, recovery 192, protected 480, activity/event marker 32 cell을 가진다.

Scene에서는 solid/open을 dark gray/pale blue-gray terrain으로, required/recovery를 bright green/cyan으로, marker/protection을 violet/blue로 표시한다. 12x8 MicroChunk boundary는 짙은 overlay, 4x4 MicroPattern boundary는 얇은 overlay로 표시하며 `Legend_*` GameObject와 TextMesh가 의미를 설명한다. `Metadata` TextMesh는 seed id/value, sector, logical digest, scene manifest digest를 저장한다.

This is the first visible one-sector Unity graybox scene.
It is not yet production art, live traversal, full-world streaming, NPC/combat/shop/save runtime, or player build approval.

# Responsibility and Added Scripts

| Script or file | Added or changed responsibility | Explicit non-ownership |
|---|---|---|
| `MoonPalaceRuntimeCatalogSnapshot.cs` | 실제 MAP21 CSV 14개를 read-only RFC4180 load하고 source/count/header/content hash, typed/FK/duplicate 오류, immutable catalog IDs와 canonical digest를 만든다. | CSV rewrite, runtime singleton install, full-world catalog orchestration을 소유하지 않는다. |
| `MoonPalaceMicroPatternCandidateLibrary.cs` | 65,536 binary mask를 열거·필터·채점하고 route reserve 및 multi-bucket diversity로 500개 구조 후보와 metadata/digest를 만든다. | MAP21_02 authoring, production art 승인, 90도 회전 배치를 소유하지 않는다. |
| `MoonPalaceReachableMicroChunkComposer.cs` | generated candidate 정확히 6개로 12x8 cell map을 만들고 sockets, route/recovery/protection/marker, BFS verdict, attempt와 digest를 기록한다. | 사후 carve, silent repair, player-physics-perfect traversal을 소유하지 않는다. |
| `MoonPalaceOneSectorGrayboxGenerator.cs` | seed/sector를 사용해 reachable chunk 16개, pattern placement 96개, unique cell 1,536개, 7-layer record 10,752개, source selection manifest와 seam validation을 만든다. | 13x13 full world, streaming, NPC/combat/shop/save runtime을 소유하지 않는다. |
| `MoonPalaceGrayboxExampleSceneBuilder.cs` | VIS01 CSV/JSON을 LF/no-BOM으로 발행하고 isolated additive Scene, 3개 Tilemap, 8개 debug Tile asset, camera, legend, metadata를 만든다. | 기존 Scene/Prefab, Build Settings, Addressables, collider, player build를 소유하지 않는다. |
| `MoonPalaceGrayboxExampleTests.cs` | VIS01 category 전용 12개 EditMode test로 source load, mask diversity, chunk rejection/reachability, sector counts, Scene tile persistence, scoped publish, determinism, forbidden execution 0을 검증한다. | prior category, PlayMode, legacy 19347, unfiltered/full regression을 선택하지 않는다. |
| VIS01 CSV/JSON/Scene artifacts | candidate/placement/chunk/cell/layer/selection/validation evidence, catalog/generation/scene/digest manifest, 실제 Unity Scene과 debug Tile을 저장한다. | production source CSV, production art, live player traversal 증거로 사용하지 않는다. |

# MicroPattern Candidate Filter Summary

- raw binary mask enumeration: `65,536`
- eligible after explicit filters: `21,104`
- accepted structural candidates: `500`
- bit meaning: `1=solid`, `0=open`; local coordinates `x/y=0..3`
- rejection counts: all solid `1`, open count outside rule `712`, largest component below 4 `15,182`, isolated one-cell pocket `28,537`, no socket/support `0`, duplicate mask `0`, coordinate bounds `0`
- selection: route-capability reserve followed by deterministic diversity-bucket round-robin and stable score/order tie-break
- diversity: density `4`, edge socket `9`, silhouette `477`, mirror family `458`, support/affordance `2`, hazard/detail eligibility `2`
- rotation placement count: `0`; actual mask identities are retained
- candidate digest: `a32e48b168d5b5a6ae2cd4a1aa51ebf75ae58e96c73c6090ce6606ba407f3096`

# Reachable MicroChunk Composition Summary

- chunk grid per chunk: `3x2` MicroPatterns
- patterns per chunk: `6 / 6`
- chunk dimensions/cells: `12x8 / 96`
- generated chunks: `16 / 16`
- composition attempt count per chunk: `1`
- entry/exit rule: west/south entry sockets reach east/north exit sockets through generated open cells
- required/recovery/protected violations: `0 / 0 / 0`
- unreachable chunks: `0`
- fallback carve / silent auto-repair: `0 / 0`
- negative probe: all-solid required path and any nonzero fallback-carve claim are rejected without changing the supplied cells

# One-Sector Generation Summary

- seed id/value: `MP_QA_01 / 1924737067`
- sector: `6,6`
- final canvas: `48x32`, `1,536 / 1,536` unique coordinates
- sector MicroPattern grid/placements: `12x8`, `96 / 96`
- MicroChunk grid/count: `4x4`, `16 / 16`
- patterns per MicroChunk: `6 / 6`
- cells per MicroChunk: `96 / 96`
- logical layers/records: `7 / 10,752`
- route/recovery/seam failures: `0 / 0 / 0`
- selection manifest records: `922`
- logical map digest: `23be9f5b4311007708617eb2e45ab04f251c9a8f50e0416ebfe971d161c64a26`

# Unity Scene Output Summary

- scene: `Assets/_Game/Map/Scenes/MoonPalace/VIS01/MoonPalaceGrayboxExample_VIS01.unity`
- scene root: `MoonPalace_Graybox_VIS01`
- hierarchy: `Grid`, `Terrain_Solid_Open`, `Route_Recovery_Overlay`, `Marker_Protection_Boundaries`, `VIS01_Camera`, `Legend`, `Metadata`
- terrain Tilemap reopened bounds/cells: `48x32 / 1,536`
- route and annotation Tilemaps reopened with nonzero persistent tiles: `PASS`
- debug Tile assets: `8`, all inside the VIS01 Scene directory, all with persistent non-null sprites
- generated Unity scene count: `1`
- Scene added to Build Settings: `NO`
- physical Scene SHA-256: `5883e64bee823e9fc647b8e2666ec8771a26adfcaafbe7032a9bbbd0226d8657`
- scene manifest digest: `2ff5a0eaf11107e8589966dd738e53695375a452942b0467accfe68a1da2618a`

# Artifact and Digest Summary

All CSV/JSON files are UTF-8 without BOM, LF-only, have exactly one final LF, are culture-invariant, and exclude `created_utc` from canonical identity.

| Kind | Relative path | SHA-256 |
|---|---|---|
| CSV | `Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/VIS01/moonpalace_vis01_cells.csv` | `bb1309bc45f98c8bee664a381f6d7fa1c132b4855dc82d97857748702d25830c` |
| CSV | `Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/VIS01/moonpalace_vis01_layers.csv` | `98f99a5ab812c2f297bf8b05569cdb479bf37d16fe4b1a43c29c2738a1c01a12` |
| CSV | `Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/VIS01/moonpalace_vis01_microchunks.csv` | `c90abf62291d564ed30b6302953cf64db1247350e84ac89161fe9bc98e651a53` |
| CSV | `Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/VIS01/moonpalace_vis01_pattern_candidates_500.csv` | `1202ee37b2b086c6c59f7a7431953f91ddfff64af4b3651cd8a2ef797e5dd941` |
| CSV | `Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/VIS01/moonpalace_vis01_pattern_placements.csv` | `0b4dbc7604011359d48df63ab4dd37e3a834f41695585c6d40957c028911e49f` |
| CSV | `Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/VIS01/moonpalace_vis01_selection_manifest.csv` | `b2dba99f2d768b51652fe94a919c5c3ba4fb332a5e6a3a77a799e2452d5babc3` |
| CSV | `Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/VIS01/moonpalace_vis01_validation_summary.csv` | `1535d9b959fa9bc0a58bad76a841ef1ac039d2fd9116cd48a259c0d103602cdc` |
| JSON | `MapDesign/MCP/GENERATED/VIS01/moonpalace_vis01_manifest.json` | `c5ead8092d2ed3b400cb038de0116631dbd8aebbb0e4d3312c05f97182e4a418` |
| JSON | `MapDesign/MCP/GENERATED/VIS01/moonpalace_vis01_catalog_snapshot.json` | `3424f3df782ef9704db151440c8a458b009790700bcd634ea55f288481d6947e` |
| JSON | `MapDesign/MCP/GENERATED/VIS01/moonpalace_vis01_pattern_candidates_500.json` | `f32b93eb0ed8a80892fa70aa0ddf15bc5af6fe890b80978e5ad82b069257014d` |
| JSON | `MapDesign/MCP/GENERATED/VIS01/moonpalace_vis01_generation_result.json` | `b8ceeb4acee8bd9e4813a5bc74cd2bae6ad0d85554f2fcafc64cee99558c1d76` |
| JSON | `MapDesign/MCP/GENERATED/VIS01/moonpalace_vis01_scene_manifest.json` | `cc898be7eb924c0862355605b5379d6c1246ed10d8aa816da60c1f492c17b36a` |
| JSON | `MapDesign/MCP/GENERATED/VIS01/moonpalace_vis01_digest_manifest.json` | self excluded; canonical digest below |

- generated CSV count: `7`
- generated JSON count: `6`
- catalog digest: `e8bb64baef91083a912c8110660ab062f5c9e69c37d16b3bada430541c07e357`
- candidate digest: `a32e48b168d5b5a6ae2cd4a1aa51ebf75ae58e96c73c6090ce6606ba407f3096`
- logical map digest: `23be9f5b4311007708617eb2e45ab04f251c9a8f50e0416ebfe971d161c64a26`
- scene manifest digest: `2ff5a0eaf11107e8589966dd738e53695375a452942b0467accfe68a1da2618a`
- digest manifest canonical digest: `362407513e7eaf458ed44fb7be55f4a59a8c60fce5501c2b750c25af85e29f7f`
- installed Task: `MapDesign/MCP/TASKS/VIS01_BUILD_MOONPALACE_GRAYBOX_EXAMPLE_V1.md`
- archived work order: `MapDesign/MCP_ARCHIVE/VIS01_BUILD_MOONPALACE_GRAYBOX_EXAMPLE_V1.md`
- installed/archive byte equality: `TRUE`
- installed/archive SHA-256: `838c4d704e0d708fc3702ad870b58d168f3642584a628659db363b4eb4cda3ff`

# Focused Validation Summary

Final command selection:

```text
unity test <project> --mode EditMode --output <temp>/moonpalace-vis01-editmode-results.xml --timeout 600 --format json -- -testCategory VIS01
```

```text
Category: VIS01 only
Discovered: 12
Executed: 12
Passed: 12
Failed: 0
Skipped: 0
Inconclusive: 0
Final duration: 4.065445 seconds
```

The final 12 tests have exactly the required names. Scene validation closes/reopens the saved asset and verifies root, Grid, three named Tilemaps, orthographic Camera, Legend, Metadata, persistent debug Tile sprites, 48x32 bounds, 1,536 terrain tiles, and nonempty overlays. Repeat/reverse/tr-TR snapshot comparison produced identical output bytes and digests.

# No Legacy Regression Boundary Notes

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
FULL REGRESSION RUNS: 0
FULL WORLD GENERATION RUNS: 0
EXISTING SCENE/PREFAB MUTATIONS: 0
PLAYER BUILD EXECUTIONS: 0
VIS02 files/runs: 0 / 0
```

All Unity test attempts used `EditMode` plus `-testCategory VIS01`; no other category was selected. No PlayMode, legacy 19347, full/unfiltered test, full-world generation, player build, push, or VIS02 action was run.

# Final Status Evidence

```text
seed id/value: MP_QA_01 / 1924737067
sector: 6,6
raw 4x4 mask count: 65,536
accepted 4x4 candidate count: 500
production MAP21_02 pattern count observed: 24
final canvas size: 48 x 32
unique coordinates: 1,536 / 1,536
sector pattern placements: 96 / 96
microchunks: 16 / 16
patterns per microchunk: 6 / 6
cells per microchunk: 96 / 96
logical layer records: 10,752
route failure count: 0
recovery failure count: 0
seam failure count: 0
unreachable microchunk count: 0
fallback carve count: 0
silent auto-repair count: 0
catalog source mutation count: 0
scene path: Assets/_Game/Map/Scenes/MoonPalace/VIS01/MoonPalaceGrayboxExample_VIS01.unity
scene root object: MoonPalace_Graybox_VIS01
generated Unity scene count: 1
generated CSV count: 7
generated JSON count: 6
logical map digest: present
scene manifest digest: present
```

PASS boundary: MoonPalace production CSV와 500개 filtered 4x4 structural candidates를 사용해 한 섹터의 reachable 12x8 MicroChunk 조합과 Unity graybox example scene을 만들 수 있다.
