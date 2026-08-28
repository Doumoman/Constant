```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES
  task_file: TASKS/MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES.md
  requires_current_task: NONE
  requires_completed_task: MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS
  requires_result:
    path: REPORTS/MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS_RESULT.md
    status: PASS
    sha256: f9cb7c52d52c2f0f55c98574c86c6455ac46472f2486be079e7150a9540fe8b4
  requires_installed_task:
    path: TASKS/MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS.md
    sha256: 87c8a482ce73da2d4238926aa0976916b809eae28b517cec3a17fb573a9f8dfd
  sets_current_task: MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES
```

# MAP11_08 — Create Cluster Preview and PlayMode Fixtures

```text
TASK: MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES
PHASE: MAP11 — TerrainCluster Authoring / Compilation
STATUS: CURRENT
NEXT: MAP11_09_MAP11_CLUSTER_EXIT_TESTS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. User-Meaning Summary

이번 Task는 MAP11_07에서 완성한 TerrainCluster 16종을 사람이 처음으로 눈으로 검사할 수 있게 만든다.

Editor Preview에서 다음을 함께 본다.

```text
12x8 chunk footprint
Entry / Exit / Role anchors
baseline / alternate spine
Traversal Envelope / AbsoluteProtected
base / high / recovery route
pattern-free shell
same spine + two MicroPattern fixture diffs
solid/protected/pattern density
48x32 one-sector scale frame
```

PlayMode에서는 실제 Sector Planner를 만들지 않는다. 대표 Cluster 네 개를 48×32 진단 프레임에 번역해 시각 primitive를 생성·검증하고 즉시 제거하는 test-only graybox만 제공한다.

이 Task를 PASS해도 production world, Tilemap, collider, camera-room 전환 또는 실제 게임 Scene에는 아직 연결되지 않는다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| read-only TerrainCluster preview model/window | TerrainCluster CSV 편집·자동 수정 |
| 실제 16종 footprint/route/protection overlay | 새 cluster compiler 또는 route solver |
| diagnostic MicroPattern A/B fixture diff | production pattern candidate selection/RNG |
| density/count/provenance inspection | density auto-tune/cleanup/Exit 승인 |
| 48×32 test-only one-sector frame | MAP14 Sector Planner/cluster placement |
| focused EditMode + PlayMode visual fixtures | gameplay Scene/Prefab/Tilemap/physics |

흐름:

```text
13 physical TerrainCluster CSVs
→ MAP11_07 importer/catalog
→ MAP11_01~06 public compilers
→ immutable preview snapshot
→ Editor panels / test-only PlayMode graybox
```

Editor/UI/PlayMode 계층에서 compiler, transform, protected-mask, renderer, signature 또는 reachability 의미를 재구현하지 않는다. 기존 public evidence만 표시·투영한다.

## 2. Focused-Test and Regression Policy

정상 실행에서 허용되는 선택은 이번 Task의 두 category뿐이다.

```text
EditMode category MAP11_08: required
PlayMode category MAP11_08: required
```

이 PlayMode 실행은 Task가 명시적으로 소유하는 ephemeral graybox fixture만 대상으로 한다. 전체 PlayMode suite를 실행하지 않는다.

정상 선택 수:

```text
MAP09/MAP10 categories: 0
MAP11_01~07 categories: 0
legacy 19347: 0
unfiltered PlayMode: 0
MAP11_09: 0
```

MAP11_08 tests 내부에서 기존 public API를 호출하는 것은 prior category 선택이 아니다.

다음 실제 문제가 관측될 때만 owner·원인·최소 범위를 Result에 기록한다.

- compile/Console error가 기존 public API drift를 가리킴
- 승인된 13 CSV/16 catalog/digest가 preflight와 불일치
- existing file/meta/asmdef/GUID 예상 밖 변경
- focused fixture가 기존 artifact의 representability 결손을 증명

Task-owned preview/test 문제는 신규 파일만 고치고 해당 MAP11_08 mode만 재실행한다. 기존 production/CSV 변경이 필요하면 수정하지 말고 `BLOCKED`로 STOP한다.

Test runner initialization timeout으로 executed 0인 요청은 PASS나 회귀 실행으로 세지 말고 별도 보고한다.

## 3. Read-Only Preflight

쓰기 전에 다음을 확인한다.

1. MAP11_07 Result/Task SHA와 `COMPLETE` 상태
2. Current Task가 `MAP11_08` 하나뿐이며 MAP11_09는 `LOCKED`
3. inbox candidate 0, unrelated staged path 0
4. Unity compile/Console relevant error 0
5. Authoring CSV/meta `65/65`, Generated CSV `0`
6. TerrainCluster CSV/meta `13/13`, importer/catalog entries `16/16`
7. catalog digest exact:

```text
cc9c88df963b2ac6ce462f76767b6de6252c09de05a5f38f8eb2c327a3c91582
```

8. schema `24 tables / 143 columns / 44 FK`; TerrainCluster `13 tables / 91 columns`
9. biome/pacing distribution exact `4×4`; footprint `2/3/4/5 = 4/4/4/4`
10. variants 32, exact two and exact one baseline per cluster
11. structural signatures 16, duplicates 0
12. Quiet candidates 4, biome/use query exact one, RNG draw 0
13. MAP11_05 pattern-free contract and MAP11_07 public compile chain available
14. MicroPattern physical definitions/cells `24/453` and catalog/profile APIs available

Do not rerun MAP11_05 or MAP11_07 merely to establish preflight. Use the approved Result and read-only current artifact checks.

Any baseline drift is `BLOCKED` before new assets are created.

## 4. Exact File and Assembly Boundary

New Editor production:

```text
Assets/_Game/Editor/MapAuthoring/WorldGeneration/TerrainClusters/TerrainClusterPreviewModel.cs(.meta)
Assets/_Game/Editor/MapAuthoring/WorldGeneration/TerrainClusters/TerrainClusterPreviewWindow.cs(.meta)
```

New Editor focused test:

```text
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/TerrainClusters/TerrainClusterPreviewTests.cs(.meta)
```

New PlayMode focused fixture/test:

```text
Assets/_Game/Tests/PlayMode/Map/WorldGeneration/TerrainClusters/TerrainClusterGrayboxPlayModeTests.cs(.meta)
```

One additional PlayMode test-only helper C# plus meta in the same folder is allowed only if visual primitive lifecycle responsibility cannot remain clear in the test file. Report the reason and file responsibility.

Use existing assemblies and namespaces:

```text
Editor production: MapAuthoring.Editor
Editor tests: MapAuthoring.Tests.EditMode
PlayMode tests: Game.Map.Tests.PlayMode
Editor namespace: StarNight.MapAuthoring.WorldGeneration.TerrainClusters
Editor test namespace: StarNight.MapAuthoring.Tests.EditMode.WorldGeneration.TerrainClusters
PlayMode namespace: StarNight.Map.Tests.PlayMode.WorldGeneration.TerrainClusters
```

Do not modify an asmdef/asmref. If the existing PlayMode assembly cannot consume the minimum test-only graybox input without an assembly change, return `BLOCKED` with the exact missing reference. Do not copy production import/compiler logic into PlayMode tests.

No existing C#/test/CSV/meta, Authoring/Generated data, Scene, Prefab, SO, Texture, Material, Settings, or Packages file may be modified.

## 5. Preview Model — Existing Authorities Only

Minimum semantic surface may be named to fit existing style but must expose equivalent responsibility:

```text
TerrainClusterPreviewRequest
TerrainClusterPreviewMode
TerrainClusterPreviewCell / OverlaySegment / Anchor
TerrainClusterPatternDiffSnapshot
TerrainClusterDensitySnapshot
TerrainClusterSectorFrameSnapshot
TerrainClusterPreviewSnapshot
TerrainClusterPreviewBuildError / Result
TerrainClusterPreviewModel
```

The model calls existing authorities in this order:

1. MAP11_07 exact 13-path physical importer and immutable catalog
2. MAP11_01 footprint/local canvas compiler
3. MAP11_02 role/socket compiler
4. MAP11_03 spine/envelope/protection compiler
5. MAP11_04 base/high/recovery/Static Shell compiler
6. MAP11_05 pattern-free or diagnostic caller-selected pattern render
7. MAP11_06 Quiet profile/pool evidence when the cluster is Quiet
8. MAP10 catalog/transform/planner/renderer/signature authority for diagnostic patterns

Do not duplicate CSV parsing, footprint normalization, route/envelope compilation, pattern transform/rendering, structural signature, or Quiet eligibility logic.

Every snapshot is immutable, defensive-copy/read-only, canonical ordered, culture independent, and owns a stable digest. Any error publishes no partial snapshot/digest.

## 6. Required Preview Content

For every one of the 16 clusters and both SpineVariants, the model must expose:

- cluster/biome/pacing/footprint/variant IDs
- active/inactive 12×8 chunk footprint and local tile bounds
- primary Entry/Exit ports, sides, access and role anchors
- ordered spine nodes/edges with `MovementKind`
- centerline, floor, clearance, jump/drop, landing and recovery envelope cells
- AbsoluteProtected union/provenance
- Static Shell Solid/Air state and full active-cell coverage
- baseline path, high route divergence/rejoin/high point and benefits
- failure nodes and recovery witness/estimated duration
- structural signature and source/compiler/report digests
- Quiet pool eligibility/query evidence when applicable

The preview must distinguish these concepts with text/tokens and optional colors. Do not encode meaning using color alone.

Required overlay tokens or equivalent labels:

```text
EN Entry | EX Exit | B Base | H High | R Recovery
SP Spine | EV Envelope | AP AbsoluteProtected
S Solid | A Air | P+ Pattern Add | P- Pattern Carve
CH 12x8 chunk boundary | SEC 48x32 sector frame
```

## 7. Diagnostic Pattern A/B Fixtures

Pattern previews are inspection fixtures, not production placement content.

Representative clusters cover all four biomes, pacing roles, and footprint sizes:

| Biome/Pacing/Chunks | Cluster | Pattern A | Pattern B |
|---|---|---|---|
| MoonCrater / Quiet / 2 | `TC_CRATER_QUIET_RIM` | `MP_CRATER_BOWL` | `MP_CRATER_ROCK_SHELF` |
| CassiaRoot / Traversal / 3 | `TC_ROOT_HOLLOW_POCKET` | `MP_ROOT_ARCH` | `MP_ROOT_HOLLOW_POCKET` |
| AbandonedMill / Discovery / 4 | `TC_MILL_BROKEN_PILLAR` | `MP_MILL_BROKEN_PILLAR` | `MP_MILL_ORTHOGONAL_CARVE` |
| MoonDough / Recovery / 5 | `TC_DOUGH_STICKY_RISE_RECOVERY` | `MP_DOUGH_BOUNCE_CUP` | `MP_DOUGH_STICKY_SHELF` |

For each representative cluster:

1. Keep the exact same compiled footprint, selected SpineVariant, Static Shell, route and protection evidence for PatternFree/A/B.
2. Use only allowed transforms from the physical MicroPattern catalog.
3. Enumerate cluster-local 4×4 origins in canonical coordinate order.
4. A diagnostic origin is valid only when every transformed non-NoChange target is active and outside AbsoluteProtected, and the corresponding MAP11_05 diagnostic zone permission can be expressed without conflict.
5. Choose the first valid origin deterministically; RNG draws remain 0.
6. Build exact caller-selected zones/intents and call actual MAP11_05/MAP10 authorities.
7. Pattern A and B must each publish a successful working canvas and a non-empty diff.
8. Baseline/high/recovery/AbsoluteProtected coordinates and values must remain unchanged.

This canonical first-valid enumeration is Editor diagnostic fixture logic only. Do not expose it as a runtime candidate selector or claim Sector Planner validity.

If one exact named pattern has no valid origin for its representative cluster, report cluster/pattern/rejection evidence and return `BLOCKED`. Do not substitute another pattern silently or modify CSV/cluster geometry.

Pattern snapshot records:

```text
pattern ID / transform / origin / placement ID
application plan and render digests
before/after/diff cells and layer provenance
protected writes/changes
pattern-free/A/B density and changed-count comparison
```

## 8. Density Inspection

Density is diagnostic evidence in this Task, not an auto-tuning rule.

For PatternFree, A and B publish at least:

```text
active cells
Solid / Air counts and ratios
AbsoluteProtected count and ratio
pattern target / changed counts and ratios
per-layer non-default counts
per-active-chunk Solid/Air counts
```

Use exact integer counts as digest authority; ratios are display values derived with invariant culture. Do not rewrite patterns, add cleanup, or fail merely because a diagnostic ratio differs from a future production density target.

MAP11_09 owns phase density approval. MAP14 owns production pattern placement/density construction.

## 9. 48×32 One-Sector Diagnostic Frame

The preview model projects one selected Cluster into a fixed `48×32` tile frame representing `4×4` MicroChunks of `12×8` tiles.

Rules:

- translate the normalized cluster bounds to a deterministic centered diagnostic offset;
- translation only: no rotation/mirror/scale;
- every active tile/overlay remains inside `[0..47]×[0..31]`;
- show all 12×8 chunk grid lines and the cluster's active footprint;
- empty frame space remains explicitly `UNOWNED_DIAGNOSTIC`, not Quiet fill or generated terrain;
- do not connect external sockets, place another Cluster, solve free space, or claim a valid Sector plan.

Editor Preview shows this sector frame for any of the 16 clusters.

PlayMode focused fixtures use exactly these representatives:

```text
TC_CRATER_QUIET_RIM                 2 chunks / Quiet
TC_ROOT_HOLLOW_POCKET              3 chunks / Traversal
TC_MILL_BROKEN_PILLAR              4 chunks / Discovery
TC_DOUGH_STICKY_RISE_RECOVERY      5 chunks / Recovery
```

The PlayMode test creates a temporary root and orthographic diagnostic camera, renders the immutable frame using test-only primitives, validates frame/grid/overlay/cardinality/bounds, yields at least one frame, then destroys every created object.

It must not:

- load or save a Scene
- create/modify Prefab, Material, Texture, Sprite or asset files
- use Tilemap, collider, Rigidbody, physics or player controller
- mutate a gameplay camera/root
- use `DontDestroyOnLoad`, static persistent cache, RNG or time-dependent layout

Pixel-perfect screenshot comparison is not required. Verify semantic primitive counts, coordinates, labels/layers, camera framing and complete teardown.

If the PlayMode test assembly cannot import the Editor preview model, keep actual physical/import/compiler proof in EditMode and provide the PlayMode fixture an immutable test-only frame snapshot through the narrowest current assembly-compatible surface. Do not duplicate physical CSV parsing or MAP11 compiler logic. Any extra helper is test-only and must be reported.

## 10. EditorWindow Contract

```text
Menu: Tools/MapDesign/TerrainCluster Preview
Title: TerrainCluster Preview
```

Required controls/panels:

- explicit `Reload` and first-open read-only import
- biome filter and exact 16-cluster selector
- baseline/alternate SpineVariant selector
- view mode `PatternFree / Pattern A / Pattern B / Compare`
- overlay toggles for Footprint, Roles/Ports, Spine, Envelope, AbsoluteProtected, Base/High/Recovery, Pattern Diff, Density, Sector Frame
- cluster-local tile panel with 12×8 chunk boundaries
- PatternFree/A/B side-by-side diff panel in Compare mode
- 48×32 sector-frame panel
- density/count/digest/error detail panel
- legend with tokens/text and colors

Window must show inline errors without exception loops or Console spam.

Forbidden UI actions:

```text
CSV edit/save/export/auto-fix
Generate/Apply/Commit
Scene/Prefab/Tilemap creation
production placement/RNG controls
continuous file watcher/AssetDatabase refresh
static mutable/domain-reload persistent preview cache
```

## 11. Focused Verification

### 11.1 EditMode category `MAP11_08`

Verify at least:

1. exact physical import `13 files / 16 clusters` and approved catalog digest
2. all 16 clusters × both variants build pattern-free snapshots successfully
3. footprint/chunk/role/port/spine/envelope/protection/route overlays equal existing public evidence
4. base/high/recovery topology and recovery durations are displayed without inference
5. four representative Pattern A/B pairs build with deterministic valid origin and actual MAP11_05/MAP10 calls
6. A/B both have non-empty diff while identical structural/protected evidence is preserved
7. protected renderer writes/changes remain `0/0`
8. density integer counts/ratios match working canvases and per-chunk totals
9. all 16 one-sector translations fit 48×32 and preserve coordinates by exact translation
10. reversed input/culture repeat produces identical snapshot/digest
11. caller mutation cannot change snapshots
12. menu/window opens, exact 16 selectors bind, default/Compare panels render, Reload succeeds, Console error 0
13. CSV/meta/Scene/Prefab/Generated modification 0

UI tests verify model evidence and bindings, not pixel-perfect pixels.

### 11.2 PlayMode category `MAP11_08`

Verify the exact four representative fixtures:

1. temporary root/camera and 48×32 frame are created
2. 4×4 chunk grid and selected active footprint match the snapshot
3. route/protection/pattern/density primitive cardinalities match immutable input
4. every rendered coordinate is inside the sector frame
5. same request produces same layout/order without RNG
6. at least one frame completes without exception/Console error
7. teardown removes every task-created GameObject and changes no Scene/asset

No player movement, physics reachability, camera-room transition or screenshot golden is claimed.

## 12. Exact Change and Non-Ownership Boundary

Allowed:

- files in Section 4 only, plus one justified PlayMode helper
- installed/archive Task
- Result
- PASS-only Status Finalize/atomic commit

Forbidden:

- existing MAP00~11_07 production/test/CSV/meta modification
- Authoring or Generated writes
- asmdef/asmref change
- persistent Scene/Prefab/SO/Texture/Material/Sprite asset
- actual SectorCanvas/WorldGenerationRoot/Tilemap
- cluster placement/socket connection/free-space solving
- candidate weights/RNG/retry/cleanup/density tuning
- Activity/Event/SpecialRegion content
- MAP11_09 exit approval
- prior/legacy/unfiltered test selection without actual trigger
- unrelated modify/stage/commit or Git push

New production Editor code must not contain gameplay mutation dependencies such as:

```text
StageMapGenerator
SectorRecipeResolver
GridWorld
RoomTemplate
TileMutationService
Tilemap
System.Random
UnityEngine.Random
```

## 13. Required Result

```text
MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES_RESULT.md
```

Header:

```text
TASK: MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES
STATUS: PASS | FAIL | BLOCKED
MAP11_08: COMPLETE ELIGIBLE | NOT COMPLETE
MAP11_09_MAP11_CLUSTER_EXIT_TESTS: LOCKED / DO NOT START
```

First section must be Korean `## User-Facing Implementation Report` and report:

| Field | Required content |
|---|---|
| 이번 작업의 목적 | 사용자가 무엇을 눈으로 검사할 수 있게 되었는지 |
| 추가된 스크립트 | 모든 Editor/EditMode/PlayMode C#과 각 책임 |
| 새로 가능해진 기능 | 16종 overlay, A/B diff, density, sector frame |
| 실제 파이프라인 위치 | MAP11_07 입력과 MAP11_09/MAP14 후속 관계 |
| 아직 안 된 것 | production placement/world/Tilemap/physics 명시 |
| Editor/game 표시 시점 | Editor menu와 test-only PlayMode, production game 구분 |

Then `## Responsibility and Added Functions` with actual public functions, inputs, outputs, non-ownership, and downstream consumers.

Mandatory evidence:

- exact file inventory and each responsibility
- MAP11_07 Result/Task SHA and preflight state
- exact 16 selector/32 variant snapshot matrix
- overlay counts/digests for representative fixtures
- PatternFree/A/B placements/diffs/densities and protected `0/0`
- all 16 sector translations and four PlayMode representatives
- menu/window open/close/reload evidence
- EditMode and PlayMode focused counts separately
- prior/legacy/unfiltered selection counts
- CSV/meta/Authoring/Generated hashes/counts unchanged
- Scene/Prefab/SO/Texture/Material/Tilemap changes 0
- regression trigger `NO` or exact owner/reason/minimum scope
- unrelated staged/included paths 0

PASS일 때만 MAP11_08을 Finalize하고 task-owned preview/test/meta/protocol 파일만 atomic commit한다.

```text
Subject: MAP11_08: add cluster preview and graybox fixtures
Push: NOT PERFORMED
```

PASS여도 MAP11_09는 자동 시작하지 않고 STOP한다.
