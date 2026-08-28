# MAP11_08 Create Cluster Preview and PlayMode Fixtures R2 Repair Result

TASK: MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES  
REPAIRS: MAP11_08R_CORRECT_TERRAIN_CLUSTER_COLUMN_TOTAL, MAP11_08R2_RESHAPE_RECOVERY_CLUSTERS_TO_SECTOR_FRAME  
STATUS: PASS  
MAP11_08: COMPLETE ELIGIBLE  
MAP11_09_MAP11_CLUSTER_EXIT_TESTS: LOCKED / DO NOT START

## User-Facing Implementation Report

| 항목 | 결과 |
|---|---|
| 이번 작업의 목적 | TerrainCluster 16종과 각 2개 traversal variant를 48×32 Sector frame 안에서 검사하고, PatternFree/Pattern A/Pattern B, route/protection/density/digest를 비교할 수 있는 Editor preview와 test-only PlayMode graybox를 완성했다. |
| R2 보정 | 기존 5×1 Recovery footprint 4종을 승인된 서로 다른 5-chunk sector-fit shape로 재작성했다. chunk 내부 tile offset, stable ID, directed topology, movement/access/route authority는 보존했다. |
| 영구 authoring gate | normalized/connected footprint 검증 뒤 chunk bounding box가 4×4를 넘으면 cluster ID, 관측 width/height, 허용 4×4를 포함한 stable `InvalidFootprint` 오류로 atomic zero-publication한다. 5×1/1×5 invalid fixture와 유효 5-chunk fixture를 추가했다. |
| 새로 보이는 기능 | `Tools/MapDesign/TerrainCluster Preview`에서 16 cluster, biome, 2 variants, PatternFree/A/B, local/compare/sector/density/digest panel과 EN/EX/B/H/R/SP/EV/AP/S/A/P±/CH/SEC overlay를 Reload와 함께 검사할 수 있다. |
| PlayMode 표시 | 네 biome/pacing/chunk-count 대표를 48×32 test-only frame에 임시 root/camera/semantic primitives로 표시하고, 모든 좌표가 frame 내부인지와 teardown을 검증한다. persistent Scene/Prefab/Tilemap은 만들지 않는다. |
| 실제 pipeline 위치 | MAP11_07 CSV/import/catalog를 입력으로 받고 MAP11_01 footprint/local canvas → MAP11_02 role/socket → MAP11_03 traversal/protection → MAP11_04 shell/routes → MAP11_05 pattern renderer → MAP11_06 Quiet query의 public 결과를 read-only snapshot으로 조합한다. |
| 아직 production이 아닌 것 | world/sector placement, socket connection/free-space solving, candidate weights/RNG/retry, production game UI, Tilemap/physics, Activity/Event/SpecialRegion은 이 작업 소유가 아니다. MAP11_09는 계속 LOCKED다. |
| 검증 결과 | 최종 compile/Console error 0, MAP11_07 11/11 PASS, MAP11_08 EditMode 5/5 PASS, MAP11_08 PlayMode 4/4 PASS다. |
| 부작용 | Authoring write-back/Generated CSV/Scene/Prefab/Tilemap/SO/Texture/Material/Sprite/asmdef/Settings/Packages 변경 0, leaked GameObject 0, 관련 없는 포함 경로 0, Git push 미수행이다. |

## Responsibility and Added Functions

| 파일 | 개별 책임 | 입력 → 출력 / 비소유 |
|---|---|---|
| `TerrainClusterPreviewModel.cs` | immutable/canonical preview snapshot, sector translation, Pattern A/B diagnostic origin, overlay/density/route/Quiet/digest evidence와 atomic error를 구성한다. `LoadCatalog`, `Build`, `DiagnosticPatternIds`가 public entry다. | MAP11_07 catalog + public compiler reports → read-only preview snapshot. gameplay/world mutation 비소유. |
| `TerrainClusterPreviewWindow.cs` | exact menu/title, Reload, cluster/biome/variant/mode selector, overlay toggle와 local/compare/sector/density/digest panel을 표시한다. | preview model → Editor UI. Scene/asset 저장 비소유. |
| `TerrainClusterPreviewTests.cs` | 13 tables/89 columns, 16×2 PatternFree snapshots, 4개 A/B 대표, culture/repeat/immutability, window binding과 side-effect 0을 검증한다. | Editor APIs/public model → MAP11_08 EditMode evidence. |
| `TerrainClusterGrayboxPlayModeTests.cs` | 2/3/4/5-chunk 대표의 deterministic test-only frame과 temporary root/camera/overlay lifecycle를 검증한다. MoonDough 대표는 실제 3×3 5-chunk footprint를 사용한다. | immutable fixture → MAP11_08 PlayMode evidence. production helper/asset 비소유. |
| `TerrainClusterAuthoringValidation.cs` | 기존 normalized/unique/four-neighbor connected/2..5 gate 뒤 4×4 Sector/MicroChunk bounding gate를 적용하고 overflow를 atomic reject한다. | parsed 13-table rows → catalog 또는 ordered errors. |
| `TerrainClusterCsvImporterV2Tests.cs` | 모든 16종의 ≤4×4 chunk/≤48×32 tile bounds, exact Recovery shapes, 5×1/1×5 atomic rejection을 검증한다. | physical CSV bytes/importer → MAP11_07 owner evidence. |

새 C#과 matching meta는 Editor production `2/2`, EditMode `1/1`, PlayMode `1/1`이다. R2에서 기존 C# 수정은 위 validation/test 2개뿐이며 `TerrainClusterAuthoringRows.cs`는 기존 `InvalidFootprint`가 충분해 변경하지 않았다.

## Repair Lineage and Installation

| 증거 | 값 |
|---|---|
| R2 적용 전 Current/row | `MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES` / `CURRENT` |
| R2 적용 전 MAP11_09 | `LOCKED` |
| 기존 BLOCKED Result SHA-256 | `9ebe415a79d26f83f473bd574548872c663531957b12891e74671673dd2c0ba9` |
| original MAP11_08 Task SHA-256 | `fe790c7380326e7b3b9a02d1332b7ad3ab3233af045485d0e552f44b22990e30` |
| R addendum TASKS/archive SHA-256 | `79a668030e333fe62e5e761f9e31830bf0f105f5b437778c33cf08e01ef9d170`, byte-identical |
| R2 inbox/TASKS/archive SHA-256 | `99147bdf12f181b75b8dcd6cb87be8171d6f90f17600c8d66823f4ed61fbb10e`, byte-identical |
| R2 설치 중 Status/Master 변경 | 0 |
| R2 inbox source | 설치 SHA 확인 후 제거 |
| staged paths before Result/finalize | 0 |

Original Task + R + R2를 effective specification으로 사용했다. schema는 full `24/143/44`, TerrainCluster `13/89` 그대로이며 schema/descriptor/digest source는 변경하지 않았다.

## Exact Recovery CSV Reauthoring

기존 네 cluster는 모두 chunks `(0,0)..(4,0)`, bounds `5×1 = 60×8 tiles`, primary Entry `(0,1) L`, primary Exit `(58,0) D`였다.

| Cluster | 새 active chunks | bounds | 새 Entry / Exit | baseline ordered node coordinates |
|---|---|---:|---|---|
| `TC_CRATER_ROCK_SHELF_RECOVERY` | `(0,0),(1,0),(2,0),(2,1),(3,1)` | `4×2 = 48×16` | `(0,1) L / (46,8) D` | `ENTRY(0,1) > BUILD_UP(4,1) > TOUCH_1(14,1) > CORE(26,1) > TOUCH_3(26,9) > RECOVERY(42,9) > EXIT(46,8)` |
| `TC_ROOT_FORKED_CANOPY_RECOVERY` | `(0,1),(1,0),(1,1),(1,2),(2,1)` | `3×3 = 36×24` | `(0,9) L / (34,8) D` | `ENTRY(0,9) > BUILD_UP(4,9) > TOUCH_1(14,1) > CORE(14,9) > TOUCH_3(14,17) > RECOVERY(30,9) > EXIT(34,8)` |
| `TC_MILL_ORTHOGONAL_SHAFT_RECOVERY` | `(0,2),(1,0),(1,1),(1,2),(2,0)` | `3×3 = 36×24` | `(0,17) L / (34,0) D` | `ENTRY(0,17) > BUILD_UP(4,17) > TOUCH_1(14,1) > CORE(14,9) > TOUCH_3(14,17) > RECOVERY(30,1) > EXIT(34,0)` |
| `TC_DOUGH_STICKY_RISE_RECOVERY` | `(0,0),(0,1),(1,1),(1,2),(2,2)` | `3×3 = 36×24` | `(0,1) L / (34,16) D` | `ENTRY(0,1) > BUILD_UP(4,1) > TOUCH_1(2,9) > CORE(14,9) > TOUCH_3(14,17) > RECOVERY(30,17) > EXIT(34,16)` |

두 variant와 exact baseline 1개, role kinds/Reward, footprint/spine variant IDs, benefit IDs, L→D primary sides는 그대로다. Alternate high route는 BuildUp divergence → HIGH → Core rejoin 두 edge를 유지하고 baseline과 구조적으로 다르다. HIGH failure → RECOVERY source edge는 네 cluster 모두 `WALK / 3500 ms`로 기존 `2000..5000 ms` 안에 있다. node/edge start/end, landing/recovery, envelope evidence를 같은 chunk-local 변환으로 옮겼고 protected/shell conflict는 focused compile chain에서 0이다.

| 실제 수정 CSV | 네 cluster 소유 행 책임 | reauthored rows |
|---|---|---:|
| `terrain_cluster_cells_v2.csv` | exact five active chunk coordinates | 20 |
| `terrain_cluster_role_anchors_v2.csv` | explicit Entry/BuildUp/Core/Recovery/Reward/Exit coordinates | 24 |
| `terrain_cluster_ports_v2.csv` | primary Entry/Exit coordinates; side/access/route types 불변 | 8 |
| `terrain_cluster_nodes_v2.csv` | both-variant node coordinates | 68 |
| `terrain_cluster_spine_edges_v2.csv` | start/end/landing/recovery coordinates; ID/topology/movement/timing 불변 | 60 |
| `terrain_cluster_envelope_cells_v2.csv` | edge envelope coordinates와 canonical PK order | 360 |

`role_variant_links/high_routes/high_route_edges/high_route_failures`는 stable ID와 topology를 보존했으므로 변경이 필요하지 않았다. `catalog/variants/high_route_benefits` 변경 0, 다른 12 cluster 소유 행 변경 0, CSV header/schema/meta/GUID 변경 0이다. 모든 13 CSV는 UTF-8 BOM, CR 0/LF-only, 한 개 final LF, exact header, canonical PK order를 유지한다.

## Permanent 4×4 Sector-Fit Gate

검증 순서는 기존 normalization, duplicate-free, four-neighbor connected, 2..5 chunks 확인 다음이다.

```text
chunkWidth  = maxX - minX + 1
chunkHeight = maxY - minY + 1
required: chunkWidth <= 4 && chunkHeight <= 4
```

초과 시 기존 stable `InvalidFootprint`를 사용해 예를 들어
`TC_CRATER_ROCK_SHELF_RECOVERY footprint bounds observed 5x1 chunks; allowed 4x4.`
를 보고하고 catalog/digest를 게시하지 않으며 `AtomicPublishRejected`를 함께 유지한다. 5×1과 1×5 invalid byte fixtures 모두 zero-publication하고, 승인된 네 5-chunk shape와 다른 12종은 모두 `<=4×4 chunks / <=48×32 tiles`로 publish된다.

## Digests and Physical Evidence

| 증거 | 결과 |
|---|---|
| catalog stable digest | `9d26786af477731d57503f16cc899210da6636f48dfb0542791e8fa591bd3bf7` |
| structural-signature set digest | `2884a639d9cef923e8b86a7fba2c0430cdfad2de11a63fd138d51dacdce13d8a` |
| structural signatures | 16 generated / duplicates 0 |
| full Authoring 65-CSV manifest digest | `ff4761537986a4c9433775359d9b62ad806914ef30462a320c97b355126a5b6c` |
| Authoring CSV/meta | `65/65` |
| TerrainCluster CSV/meta | `13/13` |
| Generated CSV | 0 |
| catalog entries | `16/16` |
| variants / baselines | `32 / 16` |
| Recovery active chunks | `5/5/5/5` |
| pattern-free compile chain | `16/16`, exact active coverage, protected writes/changes `0/0` |
| Quiet pool | exact four; existing eligibility/query behavior and RNG draws 0 |

Full Authoring manifest는 MAP09의 canonical procedure와 동일하게 relative path 순 정렬 후 각 normalized BOM CSV SHA를 `path<TAB>sha`로 결합해 SHA-256했다.

| 변경 CSV | bytes | SHA-256 |
|---|---:|---|
| `terrain_cluster_cells_v2.csv` | 2012 | `762fc7ff95c86be8264391901a2261f582377d420a8a3538966ba4e61f4a76ef` |
| `terrain_cluster_role_anchors_v2.csv` | 6332 | `524619d5f18bb12f0c3345c77b3bc933978c55e78380844a2a56a7e3163245c8` |
| `terrain_cluster_ports_v2.csv` | 4229 | `b36db8d0d4dc2181b861a32c9042b5d519f1b09b017fd6f350972a1b05277287` |
| `terrain_cluster_nodes_v2.csv` | 22607 | `3372a768eabde8dee71b57f635bb6a63e18312d09f8379c8df55290d8799f386` |
| `terrain_cluster_spine_edges_v2.csv` | 47483 | `4bceac57c31030a6eb04dc9f5734a3f559e4f7651a41b0a96e963695a75d3248` |
| `terrain_cluster_envelope_cells_v2.csv` | 130282 | `070213293b37acaeee125bc3273adac2ad7863f45127a406222c48c49e5c4918` |

## Preview and Frame Evidence

모든 16 cluster × 2 variant = 32 PatternFree snapshots가 성공했고 active coordinates는 translation-only로 `[0..47]×[0..31]` 안에 들어간다. Recovery local bounds는 Crater `48×16`, Root/Mill/Dough `36×24`; frame offset은 각각 `0,8`, `6,4`, `6,4`, `6,4`다.

| Representative | PatternFree stable digest | Pattern A / changed | Pattern B / changed | protected writes/changes |
|---|---|---|---|---:|
| Crater Quiet | `23343cbdafdd9f127877e6651fa4c372589f4f74c45c9a8bbbb7d1947cbccd60` | `MP_CRATER_BOWL` / 8 | `MP_CRATER_ROCK_SHELF` / 7 | `0/0` |
| Root Traversal | `bb5238a429bfc2f94cdc1e85d705d73e22e2b19f619fef22e7defc14a3b644ff` | `MP_ROOT_ARCH` / 12 | `MP_ROOT_HOLLOW_POCKET` / 9 | `0/0` |
| Mill Discovery | `f9f4bb3c280ca9f7e62fc5bb556355995e7da25ff0e55adeb73a5e66fe53f20a` | `MP_MILL_BROKEN_PILLAR` / 7 | `MP_MILL_ORTHOGONAL_CARVE` / 5 | `0/0` |
| Dough Recovery | `1d038b9966e961f04a06784a95116b33f3aa75f56f80e08f388b504b0262328c` | `MP_DOUGH_BOUNCE_CUP` / 10 | `MP_DOUGH_STICKY_SHELF` / 6 | `0/0` |

각 A/B는 target 16, non-empty diff, deterministic repeated digest를 가지며 PatternFree는 target/changed `0/0`이다. menu/window는 exact title/path, Reload, 16 selectors, biome당 4, variant 2, compare panels 3을 열고 닫은 뒤 Scene root/dirty state와 Generated inventory를 바꾸지 않았다.

## Focused Verification Ledger

요청된 category/mode만 선택했다. 각 request와 retry 이유는 다음과 같다.

| 순서 | Selection / Job | Executed | 결과 / retry 이유 |
|---:|---|---:|---|
| 1 | MAP11_07 EditMode `8f1a5897aca644509d708b0c2100df53` | 11 | 5 PASS / 6 FAIL; envelope numeric PK order 보정 필요 |
| 2 | MAP11_07 EditMode `9232afd2af974082932a2033166b3671` | 11 | 5 PASS / 6 FAIL; 첫 정렬 스크립트가 owned 360행 column array를 펼친 tooling defect |
| 3 | MAP11_07 EditMode `71d4660275694d3681051ee1be24402f` | 11 | 5 PASS / 6 FAIL; 로컬 복구 후 Unity가 직전 snapshot 유지. Editor direct read는 1201 lines/bad fields 0, 이후 refresh |
| 4 | MAP11_07 EditMode `dca84c9195e04e54a4e1fb8a54582308` | 11 | **11 PASS / 0 FAIL** |
| 5 | MAP11_08 EditMode `cc5dae9f6e4442de94851c81084770d9` | 5 | 4 PASS / 1 FAIL; 승인 catalog digest golden 갱신 필요 |
| 6 | MAP11_08 EditMode `eef7dd25d4cd4c60be022d338c54e0d9` | 5 | **5 PASS / 0 FAIL** |
| 7 | MAP11_08 PlayMode `716a7aeecace4a209617c85f4f45e225` | 4 | 3 PASS / 1 FAIL; Dough fixture가 count 기반 5×1 생성 |
| 8 | MAP11_08 PlayMode `bdefd85d46904ed3ba67d9112c40ff1f` | 0 | runner initialization timeout; 테스트 미실행이므로 PASS 증거 아님. Editor stop/ready 복구 |
| 9 | MAP11_08 PlayMode `fe52629539654529a27c008485eddc57` | 4 | **4 PASS / 0 FAIL** |

마지막 job 조회 중 active-instance routing이 오래된 다른 Unity 항목을 한 번 가리켰으나 `Constant@ced6e0df`를 다시 pin해 동일 job의 4/4 PASS를 확인했다. 최종 Editor는 play/transition false, 원래 `MapGenerationProgressTest` scene 활성, leaked task root 0, compile error 0, Console error 0이다.

| 금지 selection | 실행 |
|---|---:|
| MAP09 categories | 0 |
| MAP10 categories | 0 |
| MAP11_01~06 categories | 0 |
| legacy 19347 | 0 |
| unfiltered/full PlayMode suite | 0 |
| MAP11_09 | 0 |

Focused MAP11_07 내부에서 기존 public compiler API가 호출된 것만 허용 범위이며 별도 owner category는 선택하지 않았다.

## Change and Side-Effect Ledger

| 항목 | 결과 |
|---|---|
| schema/descriptor/registry/digest source changes | 0 |
| other 12 cluster rows changed | 0 |
| catalog/variant/benefit rows changed | 0 |
| CSV meta/GUID changed | 0 |
| Authoring/Generated write-back by preview | 0 / 0 |
| Runtime gameplay/CSV importer implementation changes | 0 |
| Scene/Prefab/Tilemap/SO/Texture/Material/Sprite persistent changes | 0 |
| asmdef/asmref/Settings/Packages changes | 0 |
| Unity-generated folder metas retained | 0 |
| unrelated modified/staged/included paths | 0 |
| Git push | NOT PERFORMED |

## REGRESSION TRIGGER

```text
REGRESSION TRIGGER: YES — RESOLVED IN APPROVED R2 OWNER SCOPE
OWNER: MAP11_07 TerrainCluster authoring content/validation + MAP11_08 fixture golden
REASON: approved 5×1 Recovery content could not fit a translation-only 48×32 Sector frame
RESOLUTION: exact R2 five-chunk shapes, 4×4 permanent gate, updated preview/playmode fixtures
MINIMUM EXECUTED SCOPE: MAP11_07 focused, MAP11_08 EditMode focused, MAP11_08 PlayMode focused
```

## Finalization Decision

- MAP11_08 completion criteria: PASS.
- MAP11_08 Status Finalize and one atomic task commit: ELIGIBLE.
- MAP11_09: LOCKED / DO NOT START.
- unrelated staged/included paths: 0.
- Git push: NOT PERFORMED.
