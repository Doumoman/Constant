# 《별을 물어오는 밤 / 마펠렁키》 전체 구현 Task Backlog v1.0

> 기준일: 2026-08-12  
> 기준 사양: GDD v0.3 / Map Package v1.0 / MCP Starter v1.2  
> 원칙: **MCP 패치 1개 = Task 1개 = Result 1개 = PASS 후 수동으로 다음 Task 개방**

## 0. 현재 위치와 정정 사항

```text
완료: MAP00_01 ~ MAP00_10, MAP01_01 ~ MAP01_17, MAP02_01 ~ MAP02_02
실제 다음: MAP02_03_IMPLEMENT_GRID_INITIALIZATION_PASS
MAP00 Phase: COMPLETE / EXIT APPROVED
MAP00_10 Result: PASS 검수 완료
MAP01 Phase: COMPLETE / EXIT APPROVED
MAP01_01~17 Result: PASS 검수 완료
MAP02_01~02 Result: PASS 검수 완료
MAP02_03 패치: PACKAGED v1.0 / RESULT 대기
Auto Start: NO
```

MAP00_01~10은 프로젝트 감사, 폴더·assembly 경계 계획, 36개 폴더 생성, 아키텍처 테스트 생성, `WorldGenConstants`, 좌표 값 타입·변환·exhaustive 테스트, 좌표 디버그 표시와 최종 exit audit까지 완료했다.

- `MAP00 EXIT: APPROVED`
- targeted EditMode 53/53, visual 9/9, compile error 0
- assembly/namespace/magic-number/Legacy dependency gate PASS

MAP01은 fixed Authoring CSV/meta `50/50`, immutable schema/typed definitions/FK/Registry/hash/atomic publish/report/window/failure fixtures를 완료했다. MAP01_17 exit audit에서 필수 ID `25/25`, CSV ERROR/WARNING/FK `0/0/0`, targeted `867/867`, full EditMode `887/887`을 PASS했고 `MAP01 PHASE GATE APPROVED`를 확정했다. MAP02_01은 immutable 169-cell `GeneratedWorldData`와 exact 13-column CSV v1 serializer를 구현해 focused `56/56`, targeted `923/923`, full EditMode `943/943`을 PASS했다. MAP02_02는 domain-separated SHA-256/SplitMix64 독립 RNG streams를 구현해 focused `103/103`, targeted `1026/1026`, full EditMode `1046/1046`, known vectors `6/6` each, compile/Console `0/0`을 PASS했다. Unity가 관리하는 legacy Editor folder meta 6개는 v1.2 감사에서 유효·GUID-unique baseline으로 수용됐다.

## 1. 전역 고정값

| 항목 | 고정값 |
|---|---|
| World | 624×416 logical tiles |
| Sector | 48×32 tiles |
| Sector Grid | 13×13 = 169 sectors |
| MicroChunk | 12×8 = 96 cells |
| Sector 내부 | 4×4 = 16 MicroChunks |
| 정적 원본 | Authoring CSV |
| Import Cache | ScriptableObject는 선택적 cache/preview일 뿐 원본 아님 |
| Generated CSV | Seed 결과와 QA 출력; Authoring 원본과 분리 |
| Runtime namespace | `StarNight.Map.WorldGeneration.*` |
| Runtime assembly | `Game.Map.Runtime` |
| 신규 전용 asmdef | 만들지 않음 |

## 2. 실행 규칙

1. 아래 순서를 건너뛰지 않는다.
2. 각 Task는 별도 MCP 패치로만 CURRENT가 된다.
3. Result가 `STATUS: PASS`일 때만 STATUS FINALIZE를 수행한다.
4. STATUS FINALIZE는 다음 Task를 자동으로 열지 않는다.
5. FAIL/BLOCKED이면 같은 단계에서 원인을 해결하고 다음 Phase로 가지 않는다.
6. 각 Task 패치는 정확한 READ/WRITE ALLOWLIST와 테스트 수를 별도로 고정한다.
7. 기존 Legacy/Stage/P6/P11 생성기를 신규 광역 생성기의 구현 기반으로 사용하지 않는다.

### Phase별 Task 수

| Phase | Task 수 | 현재 상태 |
|---|---:|---|
| MAP00 | 10 | 10 COMPLETE / EXIT APPROVED |
| MAP01 | 17 | 17 COMPLETE / EXIT APPROVED |
| MAP02 | 8 | 2 COMPLETE / 1 CURRENT / 5 LOCKED — MAP02_03 NEXT |
| MAP03 | 11 | LOCKED |
| MAP04 | 11 | LOCKED |
| MAP05 | 11 | LOCKED |
| MAP06 | 10 | LOCKED |
| MAP07 | 13 | LOCKED |
| MAP08 | 14 | LOCKED |
| MAP09 | 13 | LOCKED |
| MAP10 | 12 | LOCKED |
| MAP11 | 14 | LOCKED |
| MAP12 | 14 | LOCKED |
| MAP13 | 16 | LOCKED |
| MAP14 | 13 | LOCKED |
| MAP15 | 18 | LOCKED |
| **합계** | **205** | **29 COMPLETE / 1 CURRENT / 175 LOCKED** |

---

## MAP00 — 프로젝트·좌표 기반

**Phase Gate:** compile error 0, 좌표 테스트 전부 PASS, 숫자 하드코딩 중복 0.

- [x] `MAP00_01_PROJECT_AUDIT` — 기존 프로젝트 구조, asmdef, namespace, Legacy 충돌을 읽기 전용 감사한다.
- [x] `MAP00_02_FOLDER_AND_ASMDEF_PLAN` — 실제 사용할 36개 폴더와 기존 assembly 재사용 경계를 확정한다.
- [x] `MAP00_03_CREATE_MAP_MODULE_STRUCTURE` — 승인 폴더 36개와 folder meta를 생성한다.
- [x] `MAP00_04_CREATE_TEST_STRUCTURE` — 구조·namespace·dependency 경계 테스트 3개를 만든다.
- [x] `MAP00_05_DEFINE_WORLDGEN_CONSTANTS` — 624/416/48/32/13/12/8/4/96/169/16을 단일 상수 계약으로 구현한다.
- [x] `MAP00_06_IMPLEMENT_COORDINATE_VALUE_TYPES` — WorldTile/Sector/MicroChunk/LocalTile 좌표 readonly 값 타입과 equality/hash/string을 구현한다.
- [x] `MAP00_07_IMPLEMENT_COORDINATE_CONVERSIONS` — 좌표 왕복 변환, 범위 검사, `TryCreate`, 월드 밖 입력 거부를 구현한다.
- [x] `MAP00_08_CREATE_COORDINATE_TESTS` — 네 모서리, 모든 169×16 청크 경계, invalid 좌표, 왕복 변환을 EditMode로 검증한다.
- [x] `MAP00_09_CREATE_COORDINATE_DEBUG_VIEW` — Scene/Editor에서 마우스 위치의 World/Sector/MicroChunk/Local 좌표를 동시에 표시한다.
- [x] `MAP00_10_MAP00_EXIT_AUDIT` — compile/test/assembly/magic-number/Legacy dependency를 최종 감사하고 MAP01 진입을 승인한다.

---

## MAP01 — CSV 로더와 정적 데이터 Registry

**Phase Gate:** starter package ERROR 0, 필수 ID 존재, 실패 import 시 이전 Registry 유지.

- [x] `MAP01_01_INSTALL_CSV_AUTHORING_BASELINE` — 정적 CSV 49개와 데이터 사전 1개를 확정 Authoring 경로에 바이트 그대로 설치한다.
- [x] `MAP01_02_IMPLEMENT_CSV_SCHEMA_CATALOG` — `CSV_DATA_DICTIONARY.csv`에서 파일·열·타입·필수·PK·FK·default 계약을 읽는다.
- [x] `MAP01_03_IMPLEMENT_RFC4180_READER` — quoted field, comma, CRLF/LF, escaped quote, multiline, UTF-8 BOM을 위치 정보와 함께 읽는다.
- [x] `MAP01_04_IMPLEMENT_HEADER_AND_FIELD_VALIDATION` — 헤더 누락·추가·순서 불일치와 required/default 규칙을 파일·행·열로 보고한다.
- [x] `MAP01_05_IMPLEMENT_PRIMARY_KEY_INDEX` — 모든 파일의 단일/복합 PK를 1차 수집하고 중복 행 양쪽 위치를 보고한다.
- [x] `MAP01_06_IMPLEMENT_SCALAR_AND_LIST_PARSERS` — invariant int/ulong/float/bool/hex/enum과 pipe-list trim·빈 항목 금지를 구현한다.
- [x] `MAP01_07_IMPLEMENT_WORLD_ROUTE_DEFINITIONS` — world, generation, RNG, route mask, sector recipe, edge signature 정의 객체를 만든다.
- [x] `MAP01_08_IMPLEMENT_BIOME_BOUNDARY_DEFINITIONS` — biome, patch, boundary profile/pair/catalog 정의 객체를 만든다.
- [x] `MAP01_09_IMPLEMENT_SPECIAL_VILLAGE_DEFINITIONS` — special map, footprint, entry, reward, event, village, shop 정의 객체를 만든다.
- [x] `MAP01_10_IMPLEMENT_MICROCHUNK_POPULATION_ITEM_DEFINITIONS` — microchunk, tile code, slot, population, resource, map element, battery, tool, prefab 정의 객체를 만든다.
- [x] `MAP01_11_IMPLEMENT_FOREIGN_KEY_RESOLVER` — 2차/3차 pass에서 단일·list FK를 해결하고 끊긴 참조의 원본/대상을 보고한다.
- [x] `MAP01_12_IMPLEMENT_STATIC_DATA_REGISTRY` — ID Dictionary와 필요한 역색인을 immutable/read-only 형태로 publish한다.
- [x] `MAP01_13_IMPLEMENT_CONTENT_VERSION_HASH` — 파일·행 순서에 독립적인 정규화 SHA-256 `ContentVersionHash`를 만든다.
- [x] `MAP01_14_IMPLEMENT_ATOMIC_PUBLISH_AND_IMPORT_REPORT` — 오류 1개라도 있으면 Registry를 교체하지 않고 전체 오류를 `CsvImportReport.json`에 기록한다.
- [x] `MAP01_15_CREATE_CSV_IMPORT_WINDOW` — 전체 재임포트, 파일별 행/오류/해시, 오류 위치·FK 대상 이동 UI를 만든다.
- [x] `MAP01_16_CREATE_CSV_FAILURE_FIXTURES_AND_TESTS` — 중복 ID, enum/숫자, missing FK, BOM, 순서 변경, previous Registry 보존을 테스트한다.
- [x] `MAP01_17_MAP01_EXIT_AUDIT` — 49개 starter 전체 import, 필수 World/Biome/RouteMask/Battery ID, ERROR 0을 최종 승인한다.

---

## MAP02 — 13×13 토폴로지 회색박스

**Phase Gate:** 169셀·이웃·seed replay 결정론 PASS, 뒤집힘 없는 13×13 overlay.

- [x] `MAP02_01_IMPLEMENT_GENERATED_WORLD_DATA` — 169개 `SectorCell`, 역할·biome·patch·route·site 필드와 직렬화 형식을 고정한다.
- [x] `MAP02_02_IMPLEMENT_DETERMINISTIC_RNG_STREAMS` — site/biome/route/type0/recipe/population별 독립 RNG stream과 salt를 구현한다.
- [ ] `MAP02_03_IMPLEMENT_GRID_INITIALIZATION_PASS` — `(y*13+x)` 인덱스와 L/R/U/D 이웃, 월드 밖 -1을 생성한다.
- [ ] `MAP02_04_IMPLEMENT_WORLD_GENERATION_ROOT` — CSV의 pass 순서, input/output artifact, 실패 정책을 실행하는 root를 만든다.
- [ ] `MAP02_05_IMPLEMENT_PASS_EXECUTION_RECORDS` — pass ID, seed, 시작/소요 시간, retry count, 실패 원인을 기록한다.
- [ ] `MAP02_06_IMPLEMENT_SEED_MANIFEST_AND_REPLAY_RECORDER` — 빈 월드와 중간 상태를 CSV/manifest로 저장하고 재생한다.
- [ ] `MAP02_07_CREATE_WORLD_TOPOLOGY_OVERLAY` — 13×13 좌표·월드 타일 범위·Role·이웃 상태를 Scene/Game View에 표시한다.
- [ ] `MAP02_08_MAP02_EXIT_TESTS` — 인덱스/이웃/100회 동일 hash/RNG stream 독립/manifest replay를 검증한다.

---

## MAP03 — Start·특수맵·Forge·Boss·마을 선예약

**Phase Gate:** 필수 site 전부 예약, 겹침 0, 거리 규칙 PASS, CorePatch 용량 확보.

- [ ] `MAP03_01_IMPLEMENT_SITE_RESERVATION_MODELS` — footprint, entry anchor, reservation ID, CoreBiomeSeed 데이터 계약을 만든다.
- [ ] `MAP03_02_ENUMERATE_START_AND_SITE_CANDIDATES` — Start 외곽 ring, Boss, Forge, 핵심 자원 3개 후보를 열거한다.
- [ ] `MAP03_03_IMPLEMENT_FOOTPRINT_PLACEMENT_SOLVER` — 회전/반전 footprint를 월드 경계 안에 배치하고 충돌을 검사한다.
- [ ] `MAP03_04_IMPLEMENT_SITE_DISTANCE_INDEX` — 핵심 site 상호 3, Start↔첫 site 2, Forge↔Boss 2 규칙을 빠르게 검사한다.
- [ ] `MAP03_05_IMPLEMENT_SITE_CANDIDATE_COST` — 고도, edge, 거리, Core 용량, quadrant clustering 비용을 계산한다.
- [ ] `MAP03_06_IMPLEMENT_RESERVATION_BACKTRACKING` — 제약이 큰 site 우선, 이전 예약 backtrack, 최대 200회 전체 retry를 구현한다.
- [ ] `MAP03_07_IMPLEMENT_CORE_CAPACITY_FLOOD_CHECK` — footprint+buffer+Core 최소 크기의 성장 가능 영역을 검사한다.
- [ ] `MAP03_08_IMPLEMENT_VILLAGE_RESERVATION` — Start 거리 20/50/30 bucket, 1×1·2×1·1×2 footprint, entry 충돌을 처리한다.
- [ ] `MAP03_09_IMPLEMENT_SITE_RESERVATION_VALIDATOR` — required count, 경계, overlap, distance, entry 방향, Core 용량을 검증한다.
- [ ] `MAP03_10_CREATE_SITE_RESERVATION_OVERLAY` — footprint, local cell, entry arrow, Core 예상 영역, 탈락 사유를 표시한다.
- [ ] `MAP03_11_MAP03_BATCH_AND_EXIT_TESTS` — 결정론, 10만 seed village 분포, 필수 예약 실패율과 retry 원인을 검증한다.

---

## MAP04 — 반복 바이옴 Core/Satellite/Intrusion Patch

**Phase Gate:** 모든 필수 site가 올바른 CorePatch 안에 있고 미할당 0, 크기 제한 PASS.

- [ ] `MAP04_01_IMPLEMENT_BIOME_PATCH_MODELS` — PatchId, Core/Satellite/Intrusion, Primary/SecondaryBiome 소유권을 정의한다.
- [ ] `MAP04_02_INITIALIZE_CORE_PATCH_SEEDS` — 각 site footprint 전체를 해당 biome CorePatch의 강제 seed로 만든다.
- [ ] `MAP04_03_IMPLEMENT_CORE_PATCH_GROWER` — footprint+buffer+최소 크기까지 reservation을 침범하지 않고 우선 성장시킨다.
- [ ] `MAP04_04_IMPLEMENT_SATELLITE_SEED_PLACER` — 같은 biome 반복 patch seed 수·최소 거리·분산을 결정한다.
- [ ] `MAP04_05_IMPLEMENT_MULTI_SEED_BIOME_GROWER` — 거리·고도·noise·perimeter·reservation 비용으로 남은 셀을 채운다.
- [ ] `MAP04_06_IMPLEMENT_INTRUSION_PLACEMENT` — 허용 pair에만 1-cell intrusion을 생성하고 일반 1-cell patch를 금지한다.
- [ ] `MAP04_07_IMPLEMENT_PATCH_CLEANUP` — checkerboard와 1-cell neck을 정리하되 site 소유권을 바꾸지 않는다.
- [ ] `MAP04_08_EXPORT_BIOME_PATCH_RESULTS` — generated patch CSV와 섹터별 PrimaryBiome/PatchId를 출력한다.
- [ ] `MAP04_09_IMPLEMENT_BIOME_PATCH_VALIDATOR` — 2~59셀, Core 포함, 반복 수, 독점, 미할당을 검사한다.
- [ ] `MAP04_10_CREATE_BIOME_PATCH_OVERLAY` — 색, PatchId 외곽, 역할, 크기, perimeter, compactness를 표시한다.
- [ ] `MAP04_11_MAP04_BATCH_AND_EXIT_TESTS` — 1,000 seed에서 site 오소속 0과 결정론·retry 범위를 검증한다.

---

## MAP05 — Type 1·2·3 필수 진행망

**Phase Gate:** Start에서 모든 필수 site 맨몸 BFS 100%, mask 불일치 0.

- [ ] `MAP05_01_BUILD_MANDATORY_TERMINALS` — Start·Core site·Forge·Boss·Village entry를 graph terminal로 만든다.
- [ ] `MAP05_02_IMPLEMENT_ROUTE_MASK_LOOKUP` — open L/R/U/D 조합을 등록된 Type1/2/3 RouteMask ID로만 변환한다.
- [ ] `MAP05_03_IMPLEMENT_MINIMUM_CONNECTOR_TREE` — terminal을 단순 선형이 아닌 최소 연결 tree 후보로 잇는다.
- [ ] `MAP05_04_IMPLEMENT_HORIZONTAL_BACKBONE_ROUTER` — L/R 관통과 site buffer 비용을 지키는 수평 route를 찾는다.
- [ ] `MAP05_05_IMPLEMENT_VERTICAL_GATEWAY_PLANNER` — 상단 Type2.D와 하단 Type3.U 쌍만으로 수직 연결한다.
- [ ] `MAP05_06_RESOLVE_UP_DOWN_CONFLICTS` — 한 필수 셀의 U+D 동시 요구를 옆 칸 gateway pair로 분리한다.
- [ ] `MAP05_07_ADD_MANDATORY_ROUTE_LOOPS` — 자유 공략을 위해 core/중앙망 사이 loop 2개 이상을 추가한다.
- [ ] `MAP05_08_BUILD_MANDATORY_ROUTE_GRAPH` — 최종 mask와 edge를 graph/CSV/SectorCell에 기록한다.
- [ ] `MAP05_09_IMPLEMENT_MANDATORY_ROUTE_VALIDATOR` — Type 규칙, pair 대칭, terminal BFS, 독립 방문 후보를 검증한다.
- [ ] `MAP05_10_CREATE_MANDATORY_ROUTE_OVERLAY` — 필수 edge, Type 숫자·화살표, site별 거리 heatmap을 표시한다.
- [ ] `MAP05_11_MAP05_BATCH_AND_EXIT_TESTS` — 10,000 seed 실패율, retry 원인, 필수 도달 실패 0을 검증한다.

---

## MAP06 — Type 0 선택 영역

**Phase Gate:** 필수망 변경 0, 모든 Type0 `!(L&&R)`, 복귀 가능, clue 누락 0.

- [ ] `MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS` — region, depth, access rule, reward tier, return policy를 정의한다.
- [ ] `MAP06_02_ENUMERATE_OPTIONAL_ATTACHMENTS` — 필수망 인접 미사용 섹터에서 접점 후보를 수집한다.
- [ ] `MAP06_03_IMPLEMENT_OPTIONAL_REGION_GROWER` — 접점에서 깊이 1~4 비관통 군집을 성장시킨다.
- [ ] `MAP06_04_ASSIGN_TYPE0_ROUTE_MASKS` — CSV에 등록된 Type0 mask만 사용하고 L/R 동시 개방을 금지한다.
- [ ] `MAP06_05_ASSIGN_ACCESS_RULES_AND_CLUES` — Basic/Tool/Environment/Explosive/Hidden과 visible clue ID를 배정한다.
- [ ] `MAP06_06_CALCULATE_OPTIONAL_REWARD_TIER` — 깊이·도구·연료·clue 난이도로 reward tier를 계산한다.
- [ ] `MAP06_07_IMPLEMENT_RETURN_POLICY` — 원래 필수망 또는 안전 종료로 복귀하는 단일 입구·장치를 기록한다.
- [ ] `MAP06_08_ASSIGN_INACTIVE_BUFFERS` — 남은 셀을 명시적 inactive/장식 boundary로 전환한다.
- [ ] `MAP06_09_IMPLEMENT_OPTIONAL_REGION_VALIDATOR` — 필수망 동일성, 복귀, clue, mandatory reward 금지를 검사한다.
- [ ] `MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS` — access 색·깊이·접점·복귀 표시와 결정론 테스트를 완료한다.

---

## MAP07 — 12×8 마이크로청크 제작 시스템

**Phase Gate:** 완성 청크마다 96 unique cells, socket/transform/reachability/slot 검증 PASS.

- [ ] `MAP07_01_IMPLEMENT_MICROCHUNK_DEFINITION` — 12×8 셀, 8 layer, socket, slot, marker 데이터 모델을 만든다.
- [ ] `MAP07_02_IMPLEMENT_TILE_LAYER_RULES` — Ground/OneWay/Breakable/Hazard/Liquid/Decoration/Marker 중복 허용표를 구현한다.
- [ ] `MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS` — R0/MirrorX/MirrorY/R180의 타일·socket·slot 변환을 구현하고 90도 회전을 금지한다.
- [ ] `MAP07_04_IMPLEMENT_SOCKET_EDGE_VALIDATION` — side, band, traversal kind, edge signature와 실제 열린 외곽 타일을 대조한다.
- [ ] `MAP07_05_IMPLEMENT_OBJECT_SLOT_VALIDATION` — anchor, category, pool, solid 내부 배치 금지와 안전 반경을 검사한다.
- [ ] `MAP07_06_IMPLEMENT_96_CELL_VALIDATOR` — 0..11×0..7의 누락·중복·범위 초과와 `NONE` 셀 생략을 검출한다.
- [ ] `MAP07_07_IMPLEMENT_MICROCHUNK_REACHABILITY_PROBE` — mandatory socket pair의 flood/jump/drop/climb 연결을 검사한다.
- [ ] `MAP07_08_CREATE_MICROCHUNK_AUTHORING_GRID` — 12×8 고정 grid와 8 layer painting UI를 만든다.
- [ ] `MAP07_09_CREATE_SOCKET_AND_SLOT_EDITOR` — socket band/signature와 object slot anchor/pool 편집 UI를 만든다.
- [ ] `MAP07_10_IMPLEMENT_MICROCHUNK_CSV_IMPORT` — 선택 ID의 catalog/cells/sockets/slots/variants를 editor 상태로 읽는다.
- [ ] `MAP07_11_IMPLEMENT_MICROCHUNK_CSV_EXPORT` — 정확히 96행, UTF-8 BOM, ID 행 교체, stable sort로 저장한다.
- [ ] `MAP07_12_CREATE_MICROCHUNK_PREVIEW_AND_REPORT` — transform preview, reachability heatmap, 오류 좌표를 표시한다.
- [ ] `MAP07_13_MAP07_STARTER_AND_EXIT_TESTS` — starter 청크 전체의 96셀·변환·socket·slot·round-trip을 검증한다.

---

## MAP08 — 바이옴 경계 청크

**Phase Gate:** 월궁 6개 biome pair에 H/V·mandatory 후보 존재, tool requirement NONE, 경고 marker 2종 이상.

- [ ] `MAP08_01_DEFINE_MOONPALACE_BIOME_PAIRS` — 네 biome의 실제 인접 가능 6개 pair와 방향을 확정한다.
- [ ] `MAP08_02_IMPLEMENT_BOUNDARY_CANDIDATE_INDEX` — biome pair/profile/orientation/route/signature 키로 후보를 색인한다.
- [ ] `MAP08_03_IMPLEMENT_BOUNDARY_CHUNK_RESOLVER` — weight·reversible·A→B/B→A transform을 적용해 후보를 고른다.
- [ ] `MAP08_04_FILTER_MANDATORY_BOUNDARIES` — 필수 route 경계에서 `tool_requirement=NONE` 후보만 허용한다.
- [ ] `MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT` — 다음 biome의 tile/background/resource/audio marker 중 2개 이상과 warning length를 검사한다.
- [ ] `MAP08_06_AUTHOR_CRATER_ROOT_BOUNDARIES` — MoonCrater↔CassiaRoot H/V 경계 후보와 route 변형을 제작한다.
- [ ] `MAP08_07_AUTHOR_CRATER_MILL_BOUNDARIES` — MoonCrater↔AbandonedMill H/V 경계 후보와 route 변형을 제작한다.
- [ ] `MAP08_08_AUTHOR_CRATER_DOUGH_BOUNDARIES` — MoonCrater↔MoonDough H/V 경계 후보와 route 변형을 제작한다.
- [ ] `MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES` — CassiaRoot↔AbandonedMill H/V 경계 후보와 route 변형을 제작한다.
- [ ] `MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES` — CassiaRoot↔MoonDough H/V 경계 후보와 route 변형을 제작한다.
- [ ] `MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES` — AbandonedMill↔MoonDough H/V 경계 후보와 route 변형을 제작한다.
- [ ] `MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR` — pair/orientation/signature/96셀/mandatory 도구 규칙을 검증한다.
- [ ] `MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW` — transition 방향, pair 후보 수, 불가 후보, marker를 표시한다.
- [ ] `MAP08_14_MAP08_EXIT_TESTS` — 6 pair 전체 후보·방향 반전·edge compatibility·경고 길이를 검증한다.

---

## MAP09 — 48×32 섹터의 4×4 청크 조립

**Phase Gate:** 모든 recipe 16셀, 외부 socket 쌍, Type2/3 실제 연결, 10,000 solve 실패율 ≤0.1%.

- [ ] `MAP09_01_IMPLEMENT_SECTOR_RECIPE_RESOLVER` — biome/route/boundary/site 조건으로 등록된 recipe 후보를 만든다.
- [ ] `MAP09_02_FIX_EXTERNAL_SOCKET_CELLS` — 이웃 섹터와 맞닿는 external socket 셀을 가장 먼저 고정한다.
- [ ] `MAP09_03_FIX_BOUNDARY_MICROCHUNK_CELLS` — boundary sector의 전환 방향과 profile 셀을 두 번째로 고정한다.
- [ ] `MAP09_04_FIX_MANDATORY_PATH_CELLS` — recipe path 순서와 site anchor를 청크 셀 역할로 고정한다.
- [ ] `MAP09_05_BUILD_STABLE_MICROCHUNK_CANDIDATES` — pool/filter 결과를 stable ID 순으로 만들고 최근 사용 제한 입력을 받는다.
- [ ] `MAP09_06_IMPLEMENT_MICROCHUNK_COMPATIBILITY` — 인접 edge signature, biome tag, route role, budget 호환을 계산한다.
- [ ] `MAP09_07_IMPLEMENT_MRV_CONSTRAINT_SOLVER` — 최소 후보 셀 우선 선택과 즉시 constraint propagation을 구현한다.
- [ ] `MAP09_08_IMPLEMENT_BACKTRACK_AND_RETRY_LIMITS` — 10,000 node/20 조합/8 recipe 제한과 1-ring 실패 보고를 구현한다.
- [ ] `MAP09_09_INTEGRATE_TILE_REACHABILITY_PROBE` — 16셀 완성 뒤 외부 socket과 mandatory path의 실제 타일 이동을 검사한다.
- [ ] `MAP09_10_EXPORT_SECTOR_ASSEMBLY_RESULTS` — 16 placement ID·transform·choice order를 generated CSV에 기록한다.
- [ ] `MAP09_11_CREATE_SECTOR_SOLVER_DEBUG_VIEW` — 셀별 후보 수, 선택 ID, 불일치 edge, backtrack 원인을 표시한다.
- [ ] `MAP09_12_CREATE_SECTOR_ASSEMBLY_UNIT_TESTS` — Type0/1/2/3/Boundary recipe와 socket/path/미배치 0을 검증한다.
- [ ] `MAP09_13_MAP09_BATCH_AND_EXIT_TESTS` — 10,000 랜덤 섹터 solve, 결정론, 실패율/성능 기준을 승인한다.

---

## MAP10 — 특수맵과 마을 조립

**Phase Gate:** Core 3·Forge·Boss·Village 회색박스, entry→trigger→reward→return, 시설 5/6 PASS.

- [ ] `MAP10_01_VALIDATE_SITE_FOOTPRINT_LOCAL_CELLS` — reservation footprint와 CSV local sector 좌표·면적을 대조한다.
- [ ] `MAP10_02_IMPLEMENT_SPECIAL_MAP_ASSEMBLER` — ReservedSite 역할을 유지하며 local cell의 지정 recipe만 배치한다.
- [ ] `MAP10_03_CONNECT_SPECIAL_MAP_ENTRIES` — site entry socket과 Type1/2/3 이웃 섹터 socket을 연결한다.
- [ ] `MAP10_04_PLACE_EVENT_AND_REWARD_ANCHORS` — mandatory trigger/reward/return anchor를 고정하고 일반 solver 덮어쓰기를 막는다.
- [ ] `MAP10_05_IMPLEMENT_VILLAGE_LAYOUT_RESOLVER` — 1×1·2×1·1×2 layout과 5/6시설 target을 선택한다.
- [ ] `MAP10_06_PLACE_FIXED_VILLAGE_FACILITIES` — PublicKitchen과 ToolRepair를 먼저 서로 다른 slot에 배치한다.
- [ ] `MAP10_07_PLACE_OPTIONAL_VILLAGE_FACILITIES` — 비복원 weighted pick과 기능군 cap으로 나머지 3/4시설을 배치한다.
- [ ] `MAP10_08_IMPLEMENT_EVACUATED_VILLAGE_VARIANT` — NPC·중요 재고 제거와 evacuated pool 교체를 적용한다.
- [ ] `MAP10_09_IMPLEMENT_SPECIAL_MAP_REACHABILITY_VALIDATOR` — entry→trigger→reward→exit의 tool-free 이동을 검사한다.
- [ ] `MAP10_10_IMPLEMENT_VILLAGE_VALIDATOR` — 시설 수·고정 2개·중복·문 접근·미방문 완주를 검사한다.
- [ ] `MAP10_11_EXPORT_SITE_AND_VILLAGE_RESULTS` — site placements와 facility slot/위치를 generated output에 기록한다.
- [ ] `MAP10_12_MAP10_DEBUG_AND_EXIT_TESTS` — footprint/slot/anchor overlay와 모든 필수 site 조립 테스트를 완료한다.

---

## MAP11 — Tilemap Bake·Streaming·Save

**Phase Gate:** 624×416 회색 타일맵 이동 가능, 5×5 active/7×7 preload, save replay 동일.

- [ ] `MAP11_01_IMPLEMENT_TILE_AND_PREFAB_RESOLVER` — TileCode/Prefab ID를 project asset 참조로 resolve하고 missing ID를 차단한다.
- [ ] `MAP11_02_IMPLEMENT_TRANSFORMED_CELL_PLACEMENT` — 청크 transform 뒤 96셀을 섹터 local 48×32 좌표로 변환한다.
- [ ] `MAP11_03_CREATE_SECTOR_TILEMAP_LAYERS` — Ground/OneWay/Breakable/Hazard/Liquid/Decoration/Marker layer 구조를 만든다.
- [ ] `MAP11_04_IMPLEMENT_TILEMAP_SECTOR_BAKER` — 16청크를 layer별로 기록하고 경계 중복·공백을 검사한다.
- [ ] `MAP11_05_IMPLEMENT_SECTOR_COLLIDER_REBUILD` — 섹터 단위 collider 갱신과 cache 시간을 측정한다.
- [ ] `MAP11_06_IMPLEMENT_SECTOR_RUNTIME_HANDLE` — Unloaded/Preloaded/Active/SleepingModified 상태와 소유 GameObject를 관리한다.
- [ ] `MAP11_07_IMPLEMENT_7X7_PRELOAD_WINDOW` — data/Tile 참조만 준비하고 GameObject/Collider는 비활성 상태로 둔다.
- [ ] `MAP11_08_IMPLEMENT_5X5_ACTIVE_STREAMING` — 플레이어 섹터 변경 시 active set diff만 적용한다.
- [ ] `MAP11_09_IMPLEMENT_BOUNDARY_PREACTIVATION` — 카메라가 경계를 넘기 전 다음 섹터를 Active로 승격한다.
- [ ] `MAP11_10_IMPLEMENT_SECTOR_MODIFICATION_BITSETS` — 파괴·획득·장치 상태를 0..1535 local index로 저장한다.
- [ ] `MAP11_11_IMPLEMENT_WORLD_SAVE_MANIFEST` — seed/content hash/profile/version과 modified sector만 저장한다.
- [ ] `MAP11_12_IMPLEMENT_REGENERATE_AND_APPLY_SAVE` — 정적 월드를 seed로 재생성한 뒤 변경 bitset을 적용한다.
- [ ] `MAP11_13_CREATE_BAKE_STREAM_SAVE_TESTS` — 48×32 위치, transform, streaming 상한, 파괴 save/reload, hash mismatch를 검증한다.
- [ ] `MAP11_14_MAP11_PERFORMANCE_AND_EXIT_AUDIT` — bake/collider/transition spike와 전체 회색 월드 이동을 승인한다.

---

## MAP12 — 자원·맵 요소·상점·보상 배치

**Phase Gate:** 필수 자원 누락/중복 0, 모든 spawn은 slot 기반, budget 초과 0.

- [ ] `MAP12_01_IMPLEMENT_POPULATION_SLOT_INDEX` — microchunk object slot을 sector/chunk/category/pool별로 색인한다.
- [ ] `MAP12_02_IMPLEMENT_STABLE_SPAWN_IDS` — seed/sector/chunk/slot/entry로 영구 추적 가능한 spawn ID를 만든다.
- [ ] `MAP12_03_PLACE_MANDATORY_EVENTS_AND_CORE_RESOURCES` — 필수 trigger와 핵심 자원 required count를 가장 먼저 고정한다.
- [ ] `MAP12_04_IMPLEMENT_UNIQUE_REWARD_ALLOCATOR` — 특수 아이템·설계도 조각의 unique/max count를 배정한다.
- [ ] `MAP12_05_IMPLEMENT_SHOP_POPULATION` — 기본 shopkeeper 종족, 진열 3~5개, stable item ID를 생성한다.
- [ ] `MAP12_06_IMPLEMENT_RESOURCE_SPAWN_FILTERS` — biome/slot/pool/quantity/tool 규칙으로 자원 후보를 필터링한다.
- [ ] `MAP12_07_IMPLEMENT_MAP_ELEMENT_PLACEMENT` — interaction tag, 근접 금지, 상태 연쇄 cap으로 맵 요소를 배치한다.
- [ ] `MAP12_08_IMPLEMENT_HAZARD_AND_ENEMY_PLACEMENT` — 필수 보상 안전 반경 뒤에 hazard/enemy budget을 적용한다.
- [ ] `MAP12_09_IMPLEMENT_HIERARCHICAL_BUDGETS` — microchunk/sector/patch/world threat·cognitive·resource 예산을 차감한다.
- [ ] `MAP12_10_IMPLEMENT_REPETITION_AND_NEIGHBOR_RULES` — 동일 prefab 반복 거리와 forbidden neighbor tag를 적용한다.
- [ ] `MAP12_11_PLACE_REWARDS_AND_DECORATION` — 후순위 reward/decoration/empty slot을 처리하고 선순위 배치를 옮기지 않는다.
- [ ] `MAP12_12_EXPORT_GENERATED_SPAWNS` — 모든 spawn의 위치·정의·source slot·수량·unique/persistent를 CSV로 기록한다.
- [ ] `MAP12_13_IMPLEMENT_POPULATION_VALIDATOR_AND_DEBUG` — required count, unique, 안전 거리, budget과 slot 선택 과정을 표시한다.
- [ ] `MAP12_14_MAP12_DETERMINISM_AND_EXIT_TESTS` — 동일 seed hash, battery invariant, 필수 자원과 상점 규칙을 검증한다.

---

## MAP13 — 자동 검증과 대량 Seed QA

**Phase Gate:** 명령행 seed 재현, 필수 승인 규칙 실패 0, 100,000 seed 목표.

- [ ] `MAP13_01_IMPLEMENT_VALIDATION_RULE_REGISTRY` — CSV rule ID/severity/order/threshold와 결과 형식을 등록한다.
- [ ] `MAP13_02_BUILD_TILE_TRAVERSAL_NODES` — baked collision에서 서기 가능한 위치와 상태 node를 만든다.
- [ ] `MAP13_03_BUILD_MOVEMENT_EDGES` — walk/jump/drop/climb/fixed-lift 이동 edge를 생성한다.
- [ ] `MAP13_04_CONNECT_INTERSECTOR_TRAVERSAL` — 실제 열린 socket의 양쪽 타일 graph를 연결한다.
- [ ] `MAP13_05_IMPLEMENT_NAKED_MANDATORY_BFS` — 도구·연료·전지 0에서 Start→각 필수 trigger를 검사한다.
- [ ] `MAP13_06_IMPLEMENT_COMPLETION_STATE_SEARCH` — `(position, resourceMask, forge, seal, boss)` 상태 Dijkstra/A*를 구현한다.
- [ ] `MAP13_07_MEASURE_COMPLETION_DISTANCE` — minimum 500~900, normal 800~1400을 타일 이동 비용으로 계산한다.
- [ ] `MAP13_08_MEASURE_REVISIT_RATIO` — reused traversal cost/total cost로 0.35 이하를 검사한다.
- [ ] `MAP13_09_VALIDATE_ZERO_TOOL_SCENARIO` — 곡괭이·삽·로프·연료·전지 0 완주를 검사한다.
- [ ] `MAP13_10_VALIDATE_VILLAGE_SKIPPED_SCENARIO` — 마을 미방문 상태에서도 완주 가능한지 검사한다.
- [ ] `MAP13_11_VALIDATE_HOSTILE_SHOPS_SCENARIO` — 모든 상점 적대/이용 불가 상태 완주를 검사한다.
- [ ] `MAP13_12_VALIDATE_DESTRUCTION_AND_MOVING_WORST_CASE` — 파괴 가능 타일 소실과 이동 오브젝트 최악 위치를 검사한다.
- [ ] `MAP13_13_EXPORT_VALIDATION_RESULTS` — rule/severity/pass/좌표/관련 ID/측정·기대값을 CSV로 기록한다.
- [ ] `MAP13_14_CREATE_SEED_FAILURE_BUNDLE` — seed, content hash, pass 중간 CSV, 실패 좌표, screenshot 정보를 묶는다.
- [ ] `MAP13_15_IMPLEMENT_HEADLESS_BATCH_SEED_RUNNER` — command-line seed/range/worker/output과 동일 seed 1회 재현을 구현한다.
- [ ] `MAP13_16_MAP13_SCALE_AND_EXIT_AUDIT` — 1k→10k→100k 순으로 실패율·성능·필수 실패 0을 승인한다.

---

## MAP14 — Editor·Debug·Seed Replay 도구

**Phase Gate:** 비개발자 생성/검증/export 가능, 오류 위치 3클릭 이내, 169섹터 한 화면 판독.

- [ ] `MAP14_01_CREATE_WORLD_GENERATOR_WINDOW_SHELL` — profile/seed/content hash와 pass 00~09 실행 상태를 표시한다.
- [ ] `MAP14_02_IMPLEMENT_PASS_STEP_AND_ROLLBACK` — 한 단계 실행과 현재 단계 이후 결과만 삭제·재실행을 구현한다.
- [ ] `MAP14_03_CREATE_WORLD_OVERLAY_TABS` — Biome/Site/Route/Type0/Recipe/Microchunk/Population/Validation overlay를 통합한다.
- [ ] `MAP14_04_CREATE_SECTOR_INSPECTOR_PANEL` — 선택 섹터의 biome/patch/mask/site/recipe/16청크/후보를 표시한다.
- [ ] `MAP14_05_IMPLEMENT_CSV_SOURCE_NAVIGATION` — CSV ID·오류에서 정확한 파일/행/열을 연다.
- [ ] `MAP14_06_IMPLEMENT_VALIDATION_CAMERA_JUMP` — 실패 rule에서 관련 tile/socket/slot로 Scene 카메라를 이동한다.
- [ ] `MAP14_07_CREATE_SEED_REPLAY_BROWSER` — failure bundle 목록, manifest, pass result, replay 실행을 제공한다.
- [ ] `MAP14_08_VALIDATE_REPLAY_CONTENT_HASH` — 현재 content hash와 bundle hash 불일치를 차단하거나 명시한다.
- [ ] `MAP14_09_INTEGRATE_MICROCHUNK_AUTHORING_WINDOW` — MAP07 편집기를 World/Sector 선택 흐름과 연결한다.
- [ ] `MAP14_10_INTEGRATE_BOUNDARY_PREVIEW` — MAP08 pair/orientation 후보와 실제 월드 경계를 연결한다.
- [ ] `MAP14_11_CREATE_RUNTIME_WORLD_DEBUG_HUD` — seed, 좌표, active/preload sector, pass 상태만 표시하는 개발 HUD를 만든다.
- [ ] `MAP14_12_IMPLEMENT_GENERATED_OUTPUT_EXPORT` — manifest와 generated CSV 전체를 한 seed bundle로 내보낸다.
- [ ] `MAP14_13_MAP14_UI_AND_EXIT_TESTS` — 좌표 매핑, source navigation, replay hash, HUD toggle, 3클릭 접근성을 검증한다.

---

## MAP15 — 월궁 624×416 Vertical Slice

**Phase Gate:** fixed seed 30개 필수 실패 0, 실제 플레이 30회, 이동·반복·성능 기준 승인.

- [ ] `MAP15_01_REPLACE_GRAYBOX_WITH_MOONPALACE_TILE_SHELL` — Type0/1/2/3 회색 청크를 월궁 타일·collision shell로 교체한다.
- [ ] `MAP15_02_AUTHOR_MOON_CRATER_CHUNK_POOLS` — Core/Satellite/route/optional용 월의 분화구 청크 풀을 채운다.
- [ ] `MAP15_03_AUTHOR_CASSIA_ROOT_CHUNK_POOLS` — Core/Satellite/route/optional용 계수나무 뿌리 청크 풀을 채운다.
- [ ] `MAP15_04_AUTHOR_ABANDONED_MILL_CHUNK_POOLS` — Core/Satellite/route/optional용 버려진 방앗간 청크 풀을 채운다.
- [ ] `MAP15_05_AUTHOR_MOON_DOUGH_CHUNK_POOLS` — Core/Satellite/route/optional용 달떡 지대 청크 풀을 채운다.
- [ ] `MAP15_06_EXPAND_ALL_SIX_BOUNDARY_POOLS` — 6개 biome pair의 H/V/profile/route 후보를 최소 제작량까지 늘린다.
- [ ] `MAP15_07_EXPAND_SECTOR_RECIPE_POOLS` — Type0/1/2/3/Boundary pool 후보를 최근 사용 제한이 가능한 수량으로 늘린다.
- [ ] `MAP15_08_IMPLEMENT_THREE_CORE_RESOURCE_SITES` — 월핵 원석·계수수액·별누룩 site 내부 동선과 mandatory reward를 완성한다.
- [ ] `MAP15_09_IMPLEMENT_FORGE_AND_SEAL_FLOW` — 핵심 자원→Forge→열쇠/인장→Boss 개방 상태 전이를 연결한다.
- [ ] `MAP15_10_IMPLEMENT_BOSS_SITE_GRAYBOX` — Boss entry, 봉인, return, 완료 trigger의 월궁 회색박스를 완성한다.
- [ ] `MAP15_11_COMPLETE_MOONPALACE_VILLAGE` — 시설 5~6개, 상점 주인 종족, 대피 variant와 접근 동선을 완성한다.
- [ ] `MAP15_12_CONNECT_TOOLS_AND_BATTERIES_TO_TYPE0` — 곡괭이·삽·로프·전지 5종의 선택 접근과 clue/reward를 Type0에 연결한다.
- [ ] `MAP15_13_IMPLEMENT_RECENT_USE_REPETITION_LIMITS` — 인접 섹터에서 같은 핵심 silhouette/recipe 반복을 제한한다.
- [ ] `MAP15_14_SELECT_AND_LOCK_30_QA_SEEDS` — 자동 검증을 통과한 대표 seed 30개와 content hash를 고정한다.
- [ ] `MAP15_15_CREATE_MOONPALACE_PLAYMODE_TESTS` — 도구 없음·마을 미방문·상점 적대·자유 site 순서 자동 완주를 검증한다.
- [ ] `MAP15_16_COLLECT_30_PLAYTEST_TELEMETRY_RUNS` — 시간·거리·재방문·마을 발견·site 순서·Type0·battery·death를 수집한다.
- [ ] `MAP15_17_TUNE_REPETITION_DISTANCE_AND_CONTENT_GAPS` — 반복 불만·거리 분포·후보 부족을 CSV/청크 데이터로 조정한다.
- [ ] `MAP15_18_VERTICAL_SLICE_RELEASE_AUDIT` — 필수 실패 0, 거리, 반복, streaming, save, content warnings와 빌드를 최종 승인한다.

---

## 3. Phase 진입 순서

```text
MAP00 좌표 기반
→ MAP01 CSV/Registry
→ MAP02 13×13 Grid
→ MAP03 Site Reservation
→ MAP04 Biome Patch
→ MAP05 Mandatory Type1/2/3
→ MAP06 Optional Type0
→ MAP07 MicroChunk Authoring
→ MAP08 Boundary Content
→ MAP09 Sector Assembly
→ MAP10 SpecialMap/Village Assembly
→ MAP11 Tilemap/Streaming/Save
→ MAP12 Population
→ MAP13 Validation/Seed QA
→ MAP14 Editor/Replay Tools
→ MAP15 MoonPalace Vertical Slice
```

## 4. 현재 실행 대기열

```text
NEXT  : MAP02_03_IMPLEMENT_GRID_INITIALIZATION_PASS v1.0
THEN  : MAP02_04_IMPLEMENT_WORLD_GENERATION_ROOT
LOCKED: MAP02_04 이후 전부
NO RUN: MAP02_02 및 이전 완료 Task 패키지
```

`MAP02_02_IMPLEMENT_DETERMINISTIC_RNG_STREAMS` Result는 v1.1/v1.2 remediation evidence를 포함한 exact `STATUS: PASS`로 검수했다. focused `103/103`, targeted `1026/1026`, full EditMode `1046/1046`, six known vectors, compile/Console `0/0`, Authoring CSV/meta `50/50`, final Assets meta `2954`, duplicate GUID `0`을 승인했다. 다음 exact Task는 `MAP02_03_IMPLEMENT_GRID_INITIALIZATION_PASS` 하나이며 별도 v1.0 패치로만 연다. MAP02_03 PASS 검수 전에 MAP02_04를 만들거나 실행하지 않는다.
