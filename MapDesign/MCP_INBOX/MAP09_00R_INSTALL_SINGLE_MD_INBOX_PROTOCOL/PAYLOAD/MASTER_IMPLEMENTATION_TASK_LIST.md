# 《별을 물어오는 밤 / 마펠렁키》 MAP09 이후 구현 Task Backlog v2.4 Compact

> 기준일: 2026-08-27
>
> 기준선: `MAP00_01 ~ MAP08_14 COMPLETE`, `MAP08 PHASE EXIT: APPROVED`
>
> 변경 목적: 완료된 V2 모듈 구조는 보존하고 누락된 단일 MD MCP_INBOX 프로토콜만 보정한 뒤, 108개 구현 Task를 압축 해제 없는 1 Task MD 방식으로 진행

---

## 0. 현재 위치

```text
COMPLETE: MAP00_01 ~ MAP08_14, MAP09_00_CREATE_V2_MODULE_STRUCTURE
CURRENT : MAP09_00R_INSTALL_SINGLE_MD_INBOX_PROTOCOL
NEXT    : MAP09_01_FREEZE_BASELINE_AND_REGISTER_V2_PASSES (LOCKED)
LOCKED  : MAP09_01 이후 전부
```

`MAP08_14_MAP08_EXIT_TESTS_RESULT` 기준:

- MAP08 required union: `9220/9220 PASS`
- MAP07 required regression: `5422/5422 PASS`
- MAP06 required regression: `2746/2746 PASS`
- MAP05 required regression: `1959/1959 PASS`
- Required subset total: `19347/19347 PASS`
- MAP08 boundary candidates: `31`, directional projections: `62/62`
- Authoring manifest 변화: `0`
- MAP09+ production symbol: `0`

MAP00~08은 다시 구현하지 않는다. 신규 작업은 승인된 좌표·CSV/Registry·Site Reservation·Biome Patch·Mandatory/Optional Route·MicroChunk·Boundary 계약 위에 additive 계층으로만 추가한다.

`MAP09_00`은 V2 기능 루트 24개를 설치하고 PASS했다. 실행된 v1.0 Task에는 `single_task_v1`이 없었으므로 `MAP09_00R`은 Unity 구조를 다시 건드리지 않고 MCP 운영 문서 4개와 template 1개만 설치한다. 이 보정이 PASS/finalize되기 전에는 `MAP09_01`을 열지 않는다.

---

## 1. 압축 원칙

### 1.1 줄인 것

- 동일 immutable artifact의 모델·validator·index를 하나의 Task로 통합한다.
- 동일 Editor 화면의 preview·report·source navigation을 하나의 Task로 통합한다.
- 같은 CSV catalog에 속하는 starter authoring을 biome 또는 콘텐츠군 단위로 묶는다.
- unit/batch/static gate가 같은 Phase Exit를 증명하면 개별 감사 Task를 Exit Task에 합친다.
- Task와 C# 파일의 1:1 대응을 요구하지 않는다.

### 1.2 줄이지 않은 것

- Phase Gate와 `PASS 전 다음 Phase 금지`
- 한 번에 한 Task만 `CURRENT`
- RouteType과 Pacing/Activity의 책임 분리
- SpecialRegion 선예약과 일반 TerrainCluster 예약의 우선순위
- Activity/Event 제거 후에도 통과 가능한 static shell 증명
- `Cluster-first → Pattern-second → Chunk-slice-last`
- 4×4 MicroPattern과 12×8 MicroChunk의 역할 분리
- deterministic CSV/seed/RNG stream과 Generated/Authoring 분리
- 임의 통로 굴착·전체 sector 재랜덤·validation 완화 금지
- legacy generator 및 legacy type 이름 재사용 금지

### 1.3 폐기되는 기존 방식

```text
4×4 sector cell 16개
→ 완성형 12×8 MicroChunk를 각 셀에서 독립 추첨
→ 우연히 큰 지형과 이동 흐름이 생기기를 기대
```

기존 `MAP09_01_IMPLEMENT_SECTOR_RECIPE_RESOLVER` 및 과거 MAP09~15 패키지는 실행하지 않는다.

---

## 2. 새 조립 파이프라인

```text
기존 World/Site/Biome/Route/Boundary 결과
→ PacingRole
→ SpecialRegion footprint 선예약
→ TerrainCluster footprint 선예약
→ Route Spine
→ Traversal Envelope
→ 4×4 MicroPattern Add/Carve
→ Cleanup·Affordance·Material
→ ActivityStructure·EventOverlay 후순위 배치
→ 48×32 Sector Canvas 검증
→ 12×8 Generated MicroChunk 16개로 절단
→ Tilemap Bake·Streaming·Save
→ Population·Runtime State
→ Validation·Seed QA
```

### 계층별 책임

| 계층 | 책임 |
|---|---|
| `RouteType` | 섹터 외부 소켓과 필수/선택 연결 계약 |
| `MicroPattern` | 4×4 Add/Carve·표면·이동 성질·재질 연산 |
| `MicroChunk` | 12×8·96셀 저장, 스트리밍, 검증, 경계 투영 |
| `TerrainCluster` | 2~5청크 중심의 연속 정적 지형 흐름 |
| `ActivityStructure` | 낮은 빈도의 강한 플레이 사건 |
| `EventOverlay` | 정적 Shell을 유지하는 런별 NPC·보상·상태 변형 |
| `SpecialRegion` | 월드 단계에서 선예약되는 마을·자원·Forge·Boss |

일반 TerrainCluster는 2~5개 활성 청크를 기본으로 한다. 6청크는 CSV allowlist에 명시된 Cluster/Activity만 허용한다.

---

## 3. 보존되는 고정 계약

- World `624×416`, Sector `48×32`, `13×13 = 169`
- MicroChunk `12×8 = 96셀`, Sector당 `4×4 = 16`
- MicroPattern `4×4 = 16셀`, 12×8 안에 정확히 `3×2`
- 12×8 MicroChunk 90도 회전 금지
- Type1 L/R, Type2 L/R/D, Type3 L/R/U
- Type4 U/D 보장, L/R은 별도 mask
- Type0 제거 후에도 필수 진행 가능
- 필수 경계 `tool_requirement=NONE`
- 경계 진입 전 Tile/Background/Resource/Audio 중 최소 2종 예고
- CSV Authoring은 Source of Truth
- ScriptableObject는 import cache/editor preview
- Generated CSV는 seed/QA 출력이며 Authoring 입력으로 재사용 금지
- SaveData는 runtime mutation만 저장
- 동일 `Seed + DataVersion + GeneratorVersion`은 동일 결과
- pass별 RNG stream 분리와 Stable ID 정렬
- `StageMapGenerator`, legacy P6/P11 생성기 사용 금지
- `GridWorld`, `RoomTemplate`, `RoomGridTransform`, `TileMutationService` 이름 재사용 금지

---

## 4. Phase 요약

| Phase | 목적 | Task 수 | 초기 상태 |
|---|---|---:|---|
| MAP09_00 | V2 additive 모듈 구조 | 1 | COMPLETE |
| MAP09_00R | 단일 MD inbox protocol 보정 | 1 | CURRENT |
| MAP09 | V2 계약·CSV·Generated 모델 | 8 | 8 LOCKED; MAP09_01 is next after transition |
| MAP10 | 4×4 MicroPattern 제작·렌더링 | 8 | LOCKED |
| MAP11 | TerrainCluster 제작·컴파일 | 9 | LOCKED |
| MAP12 | ActivityStructure·EventOverlay | 7 | LOCKED |
| MAP13 | SpecialRegion·Village·랜드마크 | 9 | LOCKED |
| MAP14 | Cluster-first Sector Planner | 10 | LOCKED |
| MAP15 | 169-sector World Assembly | 7 | LOCKED |
| MAP16 | Canvas 확정·12×8 Slice | 8 | LOCKED |
| MAP17 | Tilemap Bake·Streaming·Save | 8 | LOCKED |
| MAP18 | Population·Runtime State | 7 | LOCKED |
| MAP19 | 이동 검증·대량 Seed QA | 9 | LOCKED |
| MAP20 | Editor·Debug·Replay | 6 | LOCKED |
| MAP21 | 월궁 Vertical Slice | 12 | LOCKED |
| **신규 합계** |  | **110** | **1 COMPLETE / 1 CURRENT / 108 LOCKED** |

기존 완료 Task 105개를 포함한 전체 재기준 총계는 `215 Task = 106 COMPLETE + 1 CURRENT + 108 LOCKED`다.

---

## MAP09_00 — V2 구조 전환

**목적:** 기존 MAP00~08 파일과 GUID를 보존한 채 V2 기능별 Runtime·EditMode test·Authoring/Generated 루트를 additive로 만든다.

**Phase Gate:** 지정된 24개 디렉터리와 Unity folder meta가 존재하고, 기존 파일 이동·삭제·이름 변경·C#/CSV/asmdef 변경이 0이다.

- [x] `MAP09_00_CREATE_V2_MODULE_STRUCTURE` — 기존 36개 승인 루트와 후속 `Microchunks`·`Boundaries` 구조를 보존하고 24개 V2 기능 루트를 추가했다.

---

## MAP09_00R — 단일 MD Inbox Protocol 보정

**목적:** MAP09_00 구조 PASS를 보존하고 누락된 `single_task_v1` apply/status/archive 규칙만 설치한다.

**Phase Gate:** MCP 문서 4개와 template 1개가 exact contract를 가지며, 단일 MD의 validate→Task install→Status open→Archive dry-run이 PASS하고 Assets 변경이 0이다.

- [ ] `MAP09_00R_INSTALL_SINGLE_MD_INBOX_PROTOCOL` — `00_MCP_ENTRYPOINT`, `05_CHANGE_CONTROL_RULES`, `07_PATCH_APPLY_RULES`, `APPLY_PATCH_AND_RUN_CURRENT_TASK`와 inert template을 갱신해 MAP09_01부터 MD 하나만 사용하게 한다.

---

## MAP09 — V2 계약·CSV·Generated 모델

**목적:** MAP00~08 기준선을 고정하고 새 계층의 데이터 소유권과 pass 입출력을 먼저 확정한다.

**Phase Gate:** 신규 immutable 모델·CSV schema·FK·generated artifact가 승인되고 기존 필수 회귀 `19347/19347`이 유지된다.

- [ ] `MAP09_01_FREEZE_BASELINE_AND_REGISTER_V2_PASSES` — MAP08_14 Result, Authoring manifest, 필수 회귀 category, 금지 symbol을 기준선으로 고정하고 `Pacing→Reservation→Cluster→Spine→Envelope→Pattern→Activity→Validate→Slice` pass 입출력·실패 정책을 등록한다.
- [ ] `MAP09_02_DEFINE_LAYER_PACING_AND_ACCESS_CONTRACTS` — RouteType/MicroPattern/MicroChunk/TerrainCluster/Activity/Event/Special의 책임과 PacingRole·AccessClass 분리를 구현하고 중복 책임을 검출한다.
- [ ] `MAP09_03_IMPLEMENT_MICROPATTERN_CONTRACTS` — 4×4 16셀, operation/layer/weight/biome/transform/protected policy를 immutable 모델과 validation 계약으로 만든다.
- [ ] `MAP09_04_IMPLEMENT_CLUSTER_SPINE_ENVELOPE_CONTRACTS` — TerrainCluster footprint·role cell·entry/exit와 Walk/Jump/Drop/Climb/Slide/Bounce Spine/Envelope를 한 authoring contract로 만든다.
- [ ] `MAP09_05_IMPLEMENT_ACTIVITY_AND_EVENT_CONTRACTS` — static shell, activation/cue/reward/recovery, removal-safe, EventOverlay marker-only 규칙을 정의한다.
- [ ] `MAP09_06_IMPLEMENT_SPECIAL_CANVAS_AND_SLICE_CONTRACTS` — SpecialRegion footprint/slot/persistence, 48×32 Sector Canvas, 12×8 Generated Slice/provenance 모델을 정의한다.
- [ ] `MAP09_07_EXTEND_CSV_REGISTRY_AND_CREATE_COMPATIBILITY_FIXTURES` — 신규 Authoring schema/FK/index를 additive로 설치하고 MAP07 fixed MicroChunk·MAP08 boundary candidate 투영 fixture와 Generated 경로 분리를 검증한다.
- [ ] `MAP09_08_MAP09_CONTRACT_EXIT_AUDIT` — immutable publish, hash, FK, legacy dependency 0, MAP00~08 전체 회귀를 승인한다.

---

## MAP10 — 4×4 MicroPattern 제작·렌더링

**목적:** MicroPattern을 작은 방이 아닌 Cluster Canvas 조각용 결정론적 브러시로 구현한다.

**Phase Gate:** starter 24패턴의 16셀·변환·ProtectedOpen·round-trip·결정론이 PASS한다.

- [ ] `MAP10_01_IMPLEMENT_PATTERN_CELL_SCHEMA_AND_VALIDATION` — 정확히 16셀과 `NO_CHANGE/ADD_SOLID/CARVE_AIR/SURFACE/AFFORDANCE/MATERIAL/HAZARD/MARKER`를 읽고 누락·중복·범위·layer 불일치를 보고한다.
- [ ] `MAP10_02_IMPLEMENT_PATTERN_TRANSFORMS_AND_PROTECTED_MASK` — R0/MirrorX/MirrorY/R180 변환과 Spine/Envelope 겹침 연산의 `NO_CHANGE` 강제를 구현한다.
- [ ] `MAP10_03_IMPLEMENT_ORDERED_PATTERN_RENDERER` — Add/Carve→Surface/Affordance→Material/Hazard/Marker 우선순위와 동일 셀 충돌 규칙을 적용한다.
- [ ] `MAP10_04_IMPLEMENT_BIOME_PROFILES_CANDIDATES_AND_RNG` — four biome의 pool/density/silhouette 규칙, stable 후보 index, pattern 전용 RNG를 구현한다.
- [ ] `MAP10_05_IMPLEMENT_REPETITION_SIGNATURE_AND_LOCAL_CLEANUP` — Mirror 포함 silhouette hash, 동일 Pattern 3연속 금지, 1셀 noise·head snag·탈출 불가 pit 정리를 구현한다.
- [ ] `MAP10_06_AUTHOR_STARTER_24_MICROPATTERNS` — Add/Carve, Surface/Affordance, Material/Hazard/Marker 기본 패턴 24개와 biome/profile 배정을 CSV로 작성한다.
- [ ] `MAP10_07_CREATE_PATTERN_PREVIEW_AND_FOCUSED_TESTS` — 원본/변환, operation 순서, protected rejection, hash, 적용 전후 diff와 failure fixture를 제공한다.
- [ ] `MAP10_08_MAP10_PATTERN_EXIT_TESTS` — import/export, 16셀, 변환, 결정론, 보호영역 침범 0, 회귀를 승인한다.

---

## MAP11 — TerrainCluster 제작·컴파일

**목적:** 플레이어가 하나의 산비탈·동굴·통로로 인식하는 연속 정적 지형을 정의하고 Canvas로 컴파일한다.

**Phase Gate:** starter TerrainCluster 16종이 기본·고점·복구 경로, density, event removal 상태를 통과한다.

- [ ] `MAP11_01_IMPLEMENT_CLUSTER_FOOTPRINT_AND_LOCAL_CANVAS` — active/inactive mask, 2~5 기본·allowlisted 6 예외, bounds, transform, 연결성, local tile layer를 구현한다.
- [ ] `MAP11_02_IMPLEMENT_CLUSTER_ROLES_AND_SOCKET_CONTRACT` — Entry/BuildUp/Core/Recovery/Reward/Exit를 tile anchor로 투영하고 sector socket/internal spine 연결을 검증한다.
- [ ] `MAP11_03_COMPILE_ROUTE_SPINE_AND_TRAVERSAL_ENVELOPE` — movement segment를 centerline/floor/clearance/jump arc/drop column/landing/recovery protected set으로 변환한다.
- [ ] `MAP11_04_IMPLEMENT_BASE_HIGH_AND_RECOVERY_ROUTES` — pattern 제거 상태의 entry→exit 기본 Shell, 이득 2종 이상의 고점 경로, 2~5초 기본 경로 복귀 witness를 만든다.
- [ ] `MAP11_05_IMPLEMENT_CLUSTER_PATTERN_ZONES_AND_RENDERER` — Add/Carve·Affordance·Marker·절대 보호 zone과 MAP10 renderer 적용을 구현한다.
- [ ] `MAP11_06_IMPLEMENT_QUIET_BUFFER_CLUSTER_POOL` — landmark 전후와 미배치 공간을 역할 없는 AIR가 아닌 짧은 정적 이동 지형으로 채운다.
- [ ] `MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS` — MoonCrater/CassiaRoot/AbandonedMill/MoonDough의 starter terrain 16종을 biome 묶음으로 제작한다.
- [ ] `MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES` — footprint, spine, envelope, low/high/recovery, pattern diff, density와 1-sector graybox를 제공한다.
- [ ] `MAP11_09_MAP11_CLUSTER_EXIT_TESTS` — 16종 결정론, reachability, recovery, event removal, density, 회귀를 승인한다.

---

## MAP12 — ActivityStructure·EventOverlay

**목적:** 강한 사건을 정적 지형과 분리하고 낮은 빈도로만 활성화한다.

**Phase Gate:** Activity/Event를 모두 제거해도 static shell이 통과 가능하며, 빈도·cap·safety가 결정론적으로 지켜진다.

- [ ] `MAP12_01_IMPLEMENT_ACTIVITY_SHELL_AND_SLOT_COMPILER` — Cue/Core/Reward/Recovery shell과 장치·발사체·압력판·추격·보상 slot을 Canvas에 투영한다.
- [ ] `MAP12_02_IMPLEMENT_ACTIVITY_REMOVAL_CUE_AND_SAFETY_PROOF` — prefab 제거 상태 통과, 시야/오디오 전조, 안전 pocket, 출구·보상 영구 파괴 금지를 검사한다.
- [ ] `MAP12_03_IMPLEMENT_ACTIVITY_COMPATIBILITY_FREQUENCY_AND_CAPS` — biome/pacing/footprint/clearance 후보와 월드·patch·sector `6~12%` 목표 및 강한 Activity cap을 적용한다.
- [ ] `MAP12_04_IMPLEMENT_EVENT_OVERLAY_ASSIGNMENT_RULES` — `3~8%` 목표, marker-only, Special overlap, cooldown, empty variant, 별도 RNG stream을 구현한다.
- [ ] `MAP12_05_AUTHOR_STARTER_ACTIVITIES_AND_OVERLAYS` — starter Activity 7종과 운석·상인·희귀 생물·마루 개입 최소 variant를 작성한다.
- [ ] `MAP12_06_CREATE_ACTIVITY_PREVIEW_AND_PLAYMODE_FIXTURES` — static/active/removed 상태, cue, 안전 구역, 중단·재진입·최악 위치를 검증한다.
- [ ] `MAP12_07_MAP12_ACTIVITY_EXIT_TESTS` — 빈도, cap, 결정론, removal proof, softlock 0, 회귀를 승인한다.

---

## MAP13 — SpecialRegion·Village·필수 랜드마크

**목적:** 마을·핵심 자원·Forge·Boss를 일반 조립보다 먼저 예약하고 fixed shell과 replaceable slot을 분리한다.

**Phase Gate:** 모든 필수 region이 예약과 일치하고 도구 없이 진입 가능하며 마을 미방문·자원/Forge 실패에도 softlock이 없다.

- [ ] `MAP13_01_IMPLEMENT_SPECIAL_FOOTPRINT_SITE_BRIDGE_AND_COORDINATES` — MAP03 Reservation ID를 1×1·2×1·1×2 region footprint, local sector/tile 좌표, transform과 연결한다.
- [ ] `MAP13_02_IMPLEMENT_ENTRY_BUFFER_PRIORITY_AND_COLLISION_RULES` — mandatory socket, 내부 apron, 양방향 exit, 전후 Quiet 1청크, Boss/Forge/Core/Village/rare/Cluster/Activity 우선순위를 구현한다.
- [ ] `MAP13_03_SEPARATE_FIXED_SHELL_SLOTS_AND_PERSISTENCE` — collision shell과 Facility/NPC/Enemy/Event/Reward slot을 분리하고 필수 자원 영구 소실을 막는다.
- [ ] `MAP13_04_IMPLEMENT_VILLAGE_SHELL_FACILITIES_AND_ACCESS` — 1×1·2×1·1×2 layout, 중앙 도로, Kitchen/Repair 고정 2개, optional 3~4개, 모든 시설의 도로 복귀를 구현한다.
- [ ] `MAP13_05_IMPLEMENT_VILLAGE_STATE_VARIANTS` — 정상/호의/개별 적대/전체 적대/대피에서 shell 이동을 보존하고 NPC·재고·문 marker만 바꾼다.
- [ ] `MAP13_06_AUTHOR_THREE_CORE_RESOURCE_REGIONS` — MoonCore/CassiaSap/StarNuruk의 환경 해법, 저점·고점·복구·필수 보상을 작성한다.
- [ ] `MAP13_07_AUTHOR_FORGE_BOSS_AND_OPTIONAL_REGIONS` — MoonSealForge, BossSealArena, 상인 동굴, Maru 성소 shell·state·reset을 작성한다.
- [ ] `MAP13_08_CREATE_SPECIAL_VALIDATOR_AND_PREVIEW` — multi-sector footprint, buffer, site binding, fixed/replaceable layer, entry→trigger→reward→return, state variant를 검증한다.
- [ ] `MAP13_09_MAP13_SPECIAL_REGION_EXIT_TESTS` — 필수 site 수, overlap 0, 마을 미방문, 자원 소실 0, Forge 반환, Boss gate를 승인한다.

---

## MAP14 — Cluster-first Sector Planner

**목적:** 48×32 sector를 Cluster-first, Pattern-second로 계획하고 문제 pattern→cluster→footprint 순으로만 재선택한다.

**Phase Gate:** Type0/1/2/3/4/Boundary/Special 조건의 1-sector·3-sector graybox가 tile reachability와 결정론을 통과한다.

- [ ] `MAP14_01_BUILD_PLANNER_INPUT_AND_PACING_ROLE` — biome/patch/route/boundary/site/optional/neighbor snapshot과 world progress·landmark 거리 기반 PacingRole을 만든다.
- [ ] `MAP14_02_FIX_ROUTE_BOUNDARY_AND_SPECIAL_ANCHORS` — external L/R/U/D, MAP08 boundary fixed slice/warning, MAP13 footprint/buffer를 가장 먼저 고정한다.
- [ ] `MAP14_03_BUILD_AND_PLACE_CLUSTER_CANDIDATES` — biome/pacing/socket/free footprint/density로 stable 후보를 만들고 제약 큰 footprint부터 배치한다.
- [ ] `MAP14_04_BUILD_SECTOR_SPINE_AND_ENVELOPE` — external anchors, Special entry, Cluster entry/exit를 mandatory/optional tile graph와 protected set으로 연결한다.
- [ ] `MAP14_05_ASSIGN_CLUSTER_ROLES_AND_RENDER_PATTERNS` — role cell·pattern zone을 고정하고 MAP10 renderer를 적용한다.
- [ ] `MAP14_06_FILL_QUIET_SPACE_AND_ASSIGN_ACTIVITY_EVENT` — 남은 영역을 Quiet/Buffer로 채운 뒤 frequency/cap 조건의 Activity와 Event marker를 후순위 배치한다.
- [ ] `MAP14_07_IMPLEMENT_CANVAS_OWNERSHIP_AND_CONFLICTS` — Special/Boundary/Spine/Cluster/Pattern/Activity/Event 우선순위와 이중 소유를 검사한다.
- [ ] `MAP14_08_IMPLEMENT_LOCAL_RETRY_AND_RNG_POLICY` — pattern→transform→cluster→footprint retry, node/attempt 상한, pass별 RNG, 임의 통로 굴착 금지를 구현한다.
- [ ] `MAP14_09_EXPORT_DEBUG_AND_CREATE_GRAYBOX_TESTS` — plan/failure 1-ring을 출력하고 모든 RouteType·biome·boundary·Special의 1-sector/3-sector fixture를 제공한다.
- [ ] `MAP14_10_MAP14_SECTOR_PLANNER_EXIT_TESTS` — solve 결정론, actual tile path, retry 상한, ownership, softlock 0을 승인한다.

---

## MAP15 — 169-sector World Assembly

**목적:** Sector Planner를 기존 world graph에 연결하고 169섹터의 경계·리듬·반복·다중 sector reservation을 조정한다.

**Phase Gate:** starter seed 집합에서 external socket 비대칭, 예약 충돌, 필수 경계 누락, pacing/density/repetition 위반이 0이다.

- [ ] `MAP15_01_IMPLEMENT_WORLD_PLAN_AND_SOLVE_ORDER` — 169 sector 입력/output hash, dependency, retry와 Special/Route/Boundary 제약 우선 deterministic solve order를 만든다.
- [ ] `MAP15_02_INTEGRATE_INTERSECTOR_SOCKETS_AND_BOUNDARIES` — 양쪽 tile anchor/traversal/apron/signature와 MAP08 pair/profile/warning을 실제 world edge에 확정한다.
- [ ] `MAP15_03_IMPLEMENT_MULTI_SECTOR_SPECIAL_AND_CLUSTER_POLICY` — 2-sector Village/Special transaction과 일반 Cluster sector-contained·명시적 cross-sector allowlist를 구현한다.
- [ ] `MAP15_04_IMPLEMENT_WORLD_PACING_DENSITY_AND_REPETITION` — Quiet/Cluster/Activity/Event/Landmark window, solid/reachable budget, Pattern/Cluster/Activity 최근 사용 거리를 적용한다.
- [ ] `MAP15_05_IMPLEMENT_NEIGHBOR_ROLLBACK_AND_FAILURE_REPORT` — 실패 sector+1-ring만 되돌리고 최초 모순·관련 edge/reservation/candidate를 보고한다.
- [ ] `MAP15_06_EXPORT_OVERLAY_AND_BATCH_TEST_WORLD_PLANS` — 169 plan/placement/edge/hash를 출력하고 multi-seed graph·reservation·solver 상한을 검증한다.
- [ ] `MAP15_07_MAP15_WORLD_ASSEMBLY_EXIT_AUDIT` — starter batch 실패 0, replay, legacy dependency 0, MAP00~14 회귀를 승인한다.

---

## MAP16 — Canvas 확정·12×8 Slice

**목적:** 검증된 48×32 Canvas를 16개의 12×8 Generated MicroChunk로 절단해 MAP07/08 계약과 연결한다.

**Phase Gate:** 모든 sector가 1536타일을 중복·공백 없이 소유하고 각 slice가 정확히 96셀·derived socket·provenance를 가진다.

- [ ] `MAP16_01_FINALIZE_CANVAS_LAYERS_AND_FIXED_PRECEDENCE` — terrain/affordance/material/hazard/marker/protection/source owner를 publish하고 MAP07 fixed slice·MAP08 boundary 우선순위를 적용한다.
- [ ] `MAP16_02_VALIDATE_PROTECTION_CLEANUP_AND_DENSITY` — ProtectedOpen 침범 0, 1셀 noise/head snag/pit 정리, solid `40~65%`, reachable `35~55%`, 무역할 AIR 최대 `8×6`을 검사한다.
- [ ] `MAP16_03_VALIDATE_FINAL_ROUTE_AND_RECOVERY` — 기본 entry→exit, external socket, 고점 실패→기본 경로 witness를 최종 tile에서 재계산한다.
- [ ] `MAP16_04_IMPLEMENT_PATTERN_CHUNK_COORDINATES_AND_PARTITION` — 4×4→12×8 round-trip과 `(chunkY*4+chunkX)` index로 48×32를 16 slice에 분할한다.
- [ ] `MAP16_05_BUILD_96_CELL_SLICES_AND_DERIVE_SOCKETS` — 각 slice 96 unique cell/layer와 실제 열린 edge 기반 socket band/signature/traversal을 만든다.
- [ ] `MAP16_06_PROJECT_MARKERS_SLOTS_AND_PROVENANCE` — Cluster/Activity/Special/Event marker를 local slot ID로 변환하고 cell source를 추적한다.
- [ ] `MAP16_07_EXPORT_REPLAY_AND_DEBUG_GENERATED_TERRAIN` — plan/slice/cell/socket/slot CSV를 저장하고 Authoring 역수입 없이 hash replay와 Canvas/Slice overlay를 제공한다.
- [ ] `MAP16_08_MAP16_SLICE_AND_OUTPUT_EXIT_TESTS` — 169×1536 coverage, slice 96셀, socket 대칭, CSV round-trip, replay를 승인한다.

---

## MAP17 — Tilemap Bake·Streaming·Save

**목적:** Generated Slice를 경계가 보이지 않는 연속 Tilemap으로 굽고 sector 단위로 streaming·save한다.

**Phase Gate:** 624×416 회색 월드 이동, 5×5 Active/7×7 Preload, 경계 전환, 파괴 save/reload가 PASS한다.

- [ ] `MAP17_01_RESOLVE_ASSETS_AND_PLACE_GENERATED_CELLS` — TileCode/Prefab ID를 검증하고 16 slice local cell을 sector/world tile 좌표로 변환한다.
- [ ] `MAP17_02_BUILD_TILEMAP_LAYERS_BAKE_AND_SEAM_VALIDATION` — layer별 1536셀 기록, overlap/gap, 4×4·12×8 seam 노출을 검사한다.
- [ ] `MAP17_03_IMPLEMENT_COLLIDER_CACHE_AND_RUNTIME_HANDLES` — sector collider rebuild/cache와 Unloaded/Preloaded/Active/SleepingModified 상태를 구현한다.
- [ ] `MAP17_04_IMPLEMENT_PRELOAD_ACTIVE_AND_PREACTIVATION` — 7×7 data preload, 5×5 active diff, camera 경계 전 다음 sector 승격을 구현한다.
- [ ] `MAP17_05_IMPLEMENT_SECTOR_MODIFICATION_STORAGE` — 파괴·획득·장치 상태를 0..1535 local index와 stable spawn ID로 기록한다.
- [ ] `MAP17_06_IMPLEMENT_SAVE_MANIFEST_REGENERATION_AND_APPLY` — seed/version/hash와 modified sector만 저장하고 재생성 후 mutation을 적용한다.
- [ ] `MAP17_07_CREATE_BAKE_STREAM_SAVE_PERFORMANCE_TESTS` — 위치, seam, streaming 상한, transition, save/reload, hash mismatch, spike를 측정한다.
- [ ] `MAP17_08_MAP17_RUNTIME_EXIT_AUDIT` — 전체 회색 월드 이동과 bake/collider/stream/save 기준을 승인한다.

---

## MAP18 — Population·Runtime State

**목적:** 지형을 변경하지 않고 slot/marker에 자원·상점·적·장치·Activity·Event를 배치하고 상태를 저장한다.

**Phase Gate:** 모든 spawn이 승인 slot에서만 생성되고 필수 자원·고유 보상·Activity·Special 상태가 save/reload 후 동일하다.

- [ ] `MAP18_01_BUILD_SLOT_INDEX_AND_STABLE_SPAWN_IDS` — sector/slice/source/category/pool index와 seed/sector/slice/slot 기반 영구 ID를 만든다.
- [ ] `MAP18_02_PLACE_MANDATORY_AND_UNIQUE_CONTENT` — 필수 trigger·3개 핵심 자원·월드 unique/max count를 선배치한다.
- [ ] `MAP18_03_POPULATE_SHOPS_RESOURCES_AND_MAP_ELEMENTS` — village/shop inventory, biome/resource/tool filter, interaction/neighbor/safe radius를 적용한다.
- [ ] `MAP18_04_PLACE_HAZARDS_ENEMIES_AND_HIERARCHICAL_BUDGETS` — 필수 경로·보상·복구 바닥 보호 후 slot/cluster/sector/patch/world budget을 차감한다.
- [ ] `MAP18_05_INSTANTIATE_ACTIVITY_AND_EVENT_RUNTIME_STATES` — cue→active→resolved/reset과 Event empty/active variant, 재진입, save key를 연결한다.
- [ ] `MAP18_06_IMPLEMENT_SPECIAL_STATE_EXPORT_AND_DEBUG` — 자원·Forge·Boss·Village 상태 persistence, Generated spawn/state CSV, 선택/budget debug를 구현한다.
- [ ] `MAP18_07_MAP18_POPULATION_EXIT_TESTS` — 동일 seed hash, required/unique, slot-only, budget, removal, save/reload를 승인한다.

---

## MAP19 — 이동 검증·대량 Seed QA

**목적:** baked collision과 실제 이동 파라미터로 완주·복구·밀도·반복·최악 상태를 검증한다.

**Phase Gate:** command-line seed 재현, 승인 범위의 필수 규칙 실패 0, 1k→10k→100k 확장 기준을 달성한다.

- [ ] `MAP19_01_LOCK_TRAVERSAL_PROFILE_AND_RULE_REGISTRY` — collider, 점프, 공중 제어, climb, bounce, 낙하, 추격 폭과 validation rule/severity/threshold를 versioning한다.
- [ ] `MAP19_02_BUILD_TILE_MOVEMENT_GRAPH` — stand/climb/bounce node, walk/jump/drop/climb/slide/bounce edge와 intersector socket을 만든다.
- [ ] `MAP19_03_IMPLEMENT_NAKED_BFS_AND_COMPLETION_SEARCH` — 도구 0 필수 접근과 `(position, resourceMask, forge, seal, boss, specialState)` 완주 search를 구현한다.
- [ ] `MAP19_04_VALIDATE_CLUSTER_RECOVERY_AND_DENSITY` — 기본/고점/2~5초 복구, solid/reachable, 8×6 AIR, head snag/pit을 검사한다.
- [ ] `MAP19_05_VALIDATE_REPETITION_AND_EVENT_REMOVAL` — Pattern Mirror, Cluster/Activity 반복 거리와 모든 Activity/Event 제거 상태 완주를 검사한다.
- [ ] `MAP19_06_VALIDATE_WORST_CASE_SCENARIOS` — 도구 0, 마을 미방문, hostile/evacuated village, 파괴 타일·장치 최악 위치를 검사한다.
- [ ] `MAP19_07_MEASURE_DISTANCE_REVISIT_AND_PACING` — 최소 500~900, 일반 800~1400, 선택 1500~2800, 반복 복도 35% 이하와 사건 간격을 측정한다.
- [ ] `MAP19_08_CREATE_FAILURE_BUNDLE_AND_HEADLESS_RUNNER` — seed/version/hash/pass CSV/좌표/provenance/screenshot bundle과 range/worker/replay runner를 만든다.
- [ ] `MAP19_09_MAP19_SCALE_AND_EXIT_AUDIT` — 1k→10k→100k 실패율·성능·필수 실패 0을 단계별 승인한다.

---

## MAP20 — Editor·Debug·Replay

**목적:** Pattern→Cluster→Sector→World→Bake→Validation 원인을 Editor에서 3클릭 이내 추적한다.

**Phase Gate:** seed 생성·step·rollback·CSV 이동·failure jump·replay·bundle export가 PASS한다.

- [ ] `MAP20_01_CREATE_GENERATOR_WINDOW_AND_SCOPED_ROLLBACK` — seed/version/hash/pass artifact와 pattern/sector/1-ring/world 범위 실행·rollback을 제공한다.
- [ ] `MAP20_02_CREATE_WORLD_OVERLAYS_AND_SECTOR_CANVAS_INSPECTOR` — Site/Biome/Route/Boundary/Pacing/Cluster/Activity/Special/Population/Validation과 48×32 owner/spine/envelope/density를 표시한다.
- [ ] `MAP20_03_CREATE_PATTERN_CLUSTER_SPECIAL_AND_SLICE_INSPECTORS` — 4×4 후보/rejection, footprint/path/slot/site binding, 12×8 cell/socket/provenance를 통합 표시한다.
- [ ] `MAP20_04_IMPLEMENT_CSV_NAVIGATION_AND_VALIDATION_JUMP` — 오류 ID에서 정확한 파일/행/열과 tile/pattern/cluster/socket/slot로 이동한다.
- [ ] `MAP20_05_IMPLEMENT_REPLAY_AUTHORING_INTEGRATION_HUD_AND_EXPORT` — failure browser, MAP07 fixed/generated 분리, MAP08 boundary 연결, runtime HUD, seed bundle export를 제공한다.
- [ ] `MAP20_06_MAP20_TOOLING_EXIT_TESTS` — 좌표, rollback scope, source navigation, replay hash, HUD, 3클릭 접근성을 승인한다.

---

## MAP21 — 월궁 624×416 Vertical Slice

**목적:** 새 파이프라인에 실제 월궁 콘텐츠를 채우고 fixed seed 30개·플레이 30회로 품질을 승인한다.

**Phase Gate:** 필수 실패 0, 청크 경계 인지 억제, 거리·밀도·반복·Activity 빈도·streaming·save·player build를 통과한다.

- [ ] `MAP21_01_LOCK_MOONPALACE_PROFILES_AND_TILE_SHELL` — 실제 이동 수치, biome density/pacing, TileCode/collision/material/audio/background를 고정한다.
- [ ] `MAP21_02_PRODUCTIONIZE_24_MICROPATTERNS` — starter 24패턴의 실루엣·재질·affordance·hazard를 실제 월궁 타일로 완성한다.
- [ ] `MAP21_03_EXPAND_CRATER_AND_ROOT_CLUSTER_POOLS` — MoonCrater/CassiaRoot의 terrain·Quiet·Buffer pool을 반복 제한 가능 수량으로 늘린다.
- [ ] `MAP21_04_EXPAND_MILL_AND_DOUGH_CLUSTER_POOLS` — AbandonedMill/MoonDough의 terrain·Quiet·Buffer pool을 늘린다.
- [ ] `MAP21_05_PRODUCTIONIZE_ACTIVITIES_AND_EVENT_OVERLAYS` — Activity 7종과 운석·상인·희귀 생물·Maru·empty variant를 완성한다.
- [ ] `MAP21_06_EXPAND_ALL_SIX_BOUNDARY_POOLS` — MAP08 6 pair의 H/V/profile/route 후보를 production 수량으로 늘리고 warning evidence를 유지한다.
- [ ] `MAP21_07_COMPLETE_THREE_CORE_RESOURCE_REGIONS` — MoonCore/CassiaSap/StarNuruk의 환경 해법·필수 보상·복구·persistence를 완성한다.
- [ ] `MAP21_08_COMPLETE_MOONPALACE_VILLAGE` — 시설 5~6, 고정 2, optional 3~4, shopkeeper, hostile/evacuated variant를 완성한다.
- [ ] `MAP21_09_COMPLETE_FORGE_BOSS_AND_OPTIONAL_REGIONS` — 자원→Forge→Seal→Boss 상태와 상인 동굴·Maru 성소 등 optional region을 완성한다.
- [ ] `MAP21_10_TUNE_REPETITION_DENSITY_AND_PACING` — Quiet 50~60%, Cluster 25~35%, Activity 6~12%, Overlay 3~8%와 silhouette 반복을 조정한다.
- [ ] `MAP21_11_LOCK_QA_SEEDS_AND_RUN_COMPLETION_PLAYTESTS` — 대표 seed 30개/hash, 자동 완주 시나리오, 플레이 30회의 시간·거리·재방문·seam·death telemetry를 수집하고 조정한다.
- [ ] `MAP21_12_VERTICAL_SLICE_RELEASE_AUDIT` — 필수 실패 0, 거리, 밀도, 반복, seam, streaming, save, warnings, player build를 최종 승인한다.

---

## 5. Phase 진입 순서

```text
MAP00~08 승인 기반
→ MAP09_00 Additive Module Structure
→ MAP09_00R Single MD Inbox Protocol
→ MAP09 V2 Contracts
→ MAP10 MicroPattern
→ MAP11 TerrainCluster
→ MAP12 Activity/Event
→ MAP13 SpecialRegion
→ MAP14 Sector Planner
→ MAP15 World Assembly
→ MAP16 Canvas/12×8 Slice
→ MAP17 Tilemap/Streaming/Save
→ MAP18 Population/Runtime State
→ MAP19 Validation/Seed QA
→ MAP20 Editor/Debug/Replay
→ MAP21 MoonPalace Vertical Slice
```

---

## 6. 실행 규칙

1. 한 번에 하나의 Task만 `CURRENT`로 연다.
2. MAP09_00 이후 각 Task는 `MCP_INBOX/<TASK_ID>.md` 단일 patch와 `<TASK_ID>_RESULT.md` 하나를 가진다.
3. Result `PASS` 전 다음 Task를 열지 않는다.
4. Phase Exit 승인 전 다음 MAP Phase를 열지 않는다.
5. FAIL/BLOCKED이면 같은 Task에서 원인을 해결한다.
6. MAP00~08 production 계약 변경은 별도 `CONTRACT_CHANGE_REQUEST`와 전체 회귀를 요구한다.
7. Generated CSV를 Authoring CSV로 복사·승격하지 않는다.
8. solver 실패를 임의 통로 굴착, 전체 sector 재랜덤, validation 완화로 숨기지 않는다.
9. Task는 하나의 검증 가능한 계약 단위이며 파일 수와 1:1일 필요가 없다.
10. Result는 manifest/hash, compile/Console, focused test, required regression, static gate, change scope를 보고한다.

---

## 7. 현재 실행 대기열

```text
CURRENT: MAP09_00R_INSTALL_SINGLE_MD_INBOX_PROTOCOL
NEXT   : MAP09_01_FREEZE_BASELINE_AND_REGISTER_V2_PASSES (LOCKED)
THEN   : MAP09_02_DEFINE_LAYER_PACING_AND_ACCESS_CONTRACTS
LOCKED : MAP09_01 ~ MAP21_12
NO RUN: 기존 MAP09_01_IMPLEMENT_SECTOR_RECIPE_RESOLVER 및 폐기된 과거 MAP09~15 패키지
```

`MAP09_00R`이 마지막 legacy ZIP이다. `MAP09_01`은 새 solver 구현이 아니라 MAP08 승인 결과와 V2 pass 경계를 설치하는 첫 `single_task_v1` Task다.
