```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP09_02_DEFINE_LAYER_PACING_AND_ACCESS_CONTRACTS
  task_file: TASKS/MAP09_02_DEFINE_LAYER_PACING_AND_ACCESS_CONTRACTS.md
  requires_current_task: NONE
  requires_completed_task: MAP09_01_FREEZE_BASELINE_AND_REGISTER_V2_PASSES
  requires_result:
    path: REPORTS/MAP09_01_FREEZE_BASELINE_AND_REGISTER_V2_PASSES_RESULT.md
    status: PASS
    sha256: 3090e6d0c31b0db6c826e9a0adc00ce5804254ccee193984d61f3b1137638d31
  requires_installed_task:
    path: TASKS/MAP09_01_FREEZE_BASELINE_AND_REGISTER_V2_PASSES.md
    sha256: 52cb1e4c1ce89691478d270fc4a7761a8e1b7f6d97a241a2e64947a78c6d41d8
  sets_current_task: MAP09_02_DEFINE_LAYER_PACING_AND_ACCESS_CONTRACTS
```

# MAP09_02 — Define Layer, Pacing, and Access Contracts

```text
TASK: MAP09_02_DEFINE_LAYER_PACING_AND_ACCESS_CONTRACTS
PHASE: MAP09 — V2 Contracts / CSV / Generated Models
STATUS: CURRENT
NEXT: MAP09_03_IMPLEMENT_MICROPATTERN_CONTRACTS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

---

## 0. 작업 목적

V2 생성기의 7개 콘텐츠 계층이 서로의 책임을 침범하지 않도록 immutable 책임 catalog를 구현하고, `PacingRole`과 `AccessClass`를 서로 독립된 두 축으로 고정한다.

이 Task의 핵심은 다음 세 가지다.

1. 기존 MAP01~08의 정본 `route_type` 계약을 재사용하고 duplicate `RouteType`을 만들지 않는다.
2. `MicroPattern`, `MicroChunk`, `TerrainCluster`, `ActivityStructure`, `EventOverlay`, `SpecialRegion`의 독점 책임을 구분한다.
3. “조용함/위험/발견” 같은 페이싱이 “맨몸/도구/숨김/진행 게이트” 같은 접근 조건을 암묵적으로 바꾸지 못하게 한다.

이 Task는 **MicroPattern 16셀, Cluster footprint/Spine, Activity/Event 모델, SpecialRegion footprint, CSV schema 또는 실제 Planner를 구현하지 않는다.**

---

## 1. 작업 전 읽기 전용 감사

코드를 변경하기 전에 실제 프로젝트에서 다음을 확인하고 Result에 기록한다.

1. MAP09_01 Result가 `PASS`이고 SHA-256이 metadata와 exact 일치하는지
2. 설치·Archive된 MAP09_01 Task가 서로 byte-identical이며 SHA-256이 metadata와 exact 일치하는지
3. `06_IMPLEMENTATION_STATUS.md`가 `Current Task: MAP09_02...` 하나만 가리키는지
4. MAP09_01의 `V2PassContract`, `V2PassCatalog`, validator와 focused test 위치·namespace
5. 기존 `SectorRouteMaskDefinition`, `MandatoryRouteMaskLookup`, Type0~4 route 계약의 실제 public API
6. 기존 `OptionalRegionAccessRule`, `OptionalAccessRequirement`, MAP08 mandatory boundary `tool_requirement=NONE` 계약
7. V2 7개 Runtime root와 대응 EditMode test root가 MAP09_00 승인 상태 그대로인지
8. 현재 Authoring CSV/meta 수와 sorted relative-path manifest
9. global Assets meta 수, Map subtree meta 수, duplicate GUID group 수
10. compile/Console error, unapplied MCP candidate, 작업 전 dirty worktree

### 1.1 감사 실패 정책

다음 중 하나라도 맞으면 임의 보정하지 말고 `BLOCKED`로 보고한다.

- 기존 route type의 실제 의미가 아래 §2.1과 다름
- MAP09_01 pass catalog가 Result의 10-pass/digest와 다름
- MAP09_01 Result/Task hash mismatch
- MAP09_02 외 다른 CURRENT Task 또는 unapplied candidate 존재
- approved V2 root가 없거나 GUID 충돌이 있음
- 기존 사용자 변경과 이 Task allowlist가 겹쳐 안전하게 보존할 수 없음

---

## 2. 보존할 정본 계약

### 2.1 기존 RouteType — 신규 타입 생성 금지

`RouteType`은 새 C# enum/class 이름이 아니다. 기존 Authoring/runtime의 integer `route_type`과 MAP05 graph/mask 결과가 정본이다.

```text
Type0 = 선택/비필수 영역용; 제거되어도 mandatory progress 보존
Type1 = L/R
Type2 = L/R/D
Type3 = L/R/U
Type4 = U/D 보장; L/R은 실제 인접 mask를 독립 보존
```

RouteType의 책임은 **Sector 외부 소켓과 필수/선택 연결 계약**이다.

RouteType은 다음을 결정하지 않는다.

- 4×4 tile operation
- Cluster footprint 또는 내부 이동 실루엣
- Activity/Event 발생 여부
- PacingRole
- 보상·NPC·장치 상태
- 12×8 저장 slice 내용

신규 production scope에서 `enum RouteType`, `class RouteType`, `struct RouteType` 또는 의미가 같은 duplicate codec/lookup을 만들지 않는다.

### 2.2 크기와 생성 순서

```text
MicroPattern = 4×4 / 16 cells / local operation brush
MicroChunk   = 12×8 / 96 cells / storage, streaming, validation, boundary projection
Sector Canvas = 48×32

Cluster-first
→ Pattern-second
→ Chunk-slice-last
```

### 2.3 MAP09_01 Pass 순서

이번 Task의 layer order는 pass catalog와 모순될 수 없다.

```text
Pacing
→ SpecialRegionReservation
→ TerrainClusterReservation
→ RouteSpine
→ TraversalEnvelope
→ MicroPattern
→ TerrainCleanup
→ ActivityEventOverlay
→ TileValidation
→ MicroChunkSlice
```

---

## 3. Exact Layer Catalog

공유 Runtime contract로 정확히 다음 7개 layer ID를 stable order로 등록한다.

| Order | Layer ID | 독점 책임 |
|---:|---|---|
| 10 | `RouteType` | Sector 외부 socket topology와 일반 route access authority |
| 20 | `SpecialRegion` | 월드 단계 선예약 footprint·fixed landmark shell·special entry access authority |
| 30 | `TerrainCluster` | 연속 정적 지형 footprint와 내부 traversal structure |
| 40 | `MicroPattern` | 보호영역 밖 4×4 local tile operation |
| 50 | `ActivityStructure` | 제거 가능한 저빈도 강한 플레이 사건 |
| 60 | `EventOverlay` | static shell을 바꾸지 않는 런별 marker/state 변형 |
| 70 | `MicroChunk` | 검증 완료 Canvas의 12×8 저장·slice·provenance·boundary projection |

이 order는 별도의 runtime pass 실행기가 아니다. 계층 소유권과 선후 불변식을 설명하는 stable catalog다.

### 3.1 Exact exclusive responsibilities

최소 다음 stable responsibility ID를 immutable/read-only collection으로 제공한다.

```text
SectorExternalConnectivity
GeneralRouteAccess
WorldReservedLandmark
SpecialEntryAccess
StaticTerrainTraversal
LocalPatternTileOperation
StrongGameplayIncident
MarkerOnlyRunVariation
SliceStorageAndBoundaryProjection
```

정확한 owner matrix:

| Responsibility | Exact owner |
|---|---|
| `SectorExternalConnectivity` | `RouteType` |
| `GeneralRouteAccess` | `RouteType` |
| `WorldReservedLandmark` | `SpecialRegion` |
| `SpecialEntryAccess` | `SpecialRegion` |
| `StaticTerrainTraversal` | `TerrainCluster` |
| `LocalPatternTileOperation` | `MicroPattern` |
| `StrongGameplayIncident` | `ActivityStructure` |
| `MarkerOnlyRunVariation` | `EventOverlay` |
| `SliceStorageAndBoundaryProjection` | `MicroChunk` |

각 responsibility는 exact one owner만 가져야 한다. 누락, duplicate owner, wrong owner는 모두 validation failure다.

### 3.2 Layer별 허용·금지 경계

| Layer | 허용 | 반드시 보존 | 금지 |
|---|---|---|---|
| `RouteType` | 외부 방향 mask, mandatory/optional 연결, 일반 AccessClass authority | 기존 Type0~4와 MAP05 graph identity | 내부 tile shape, Pacing, 사건, 보상 |
| `SpecialRegion` | 선예약 footprint, fixed shell, special entry AccessClass | world/site reservation과 entry/return 연결 | 일반 Cluster 대체, slice/storage 소유 |
| `TerrainCluster` | 정적 footprint, Route Spine/Envelope 호환, pacing/access compatibility | 외부 RouteType, Special 예약, mandatory path | 강한 사건의 활성 상태, 런별 marker |
| `MicroPattern` | protected 밖 Add/Carve/surface 계열 local operation compatibility | Spine, Envelope, ProtectedOpen, inherited AccessClass | 큰 구조·외부 socket·Pacing 배정 |
| `ActivityStructure` | cue/core/reward/recovery 사건 compatibility | 제거 상태 static traversal과 exit | 필수 collision shell, route/access 변경 |
| `EventOverlay` | NPC/reward/state marker compatibility | static shell, collision, inherited access | solid tile mutation, mandatory route 봉쇄 |
| `MicroChunk` | validated tile/provenance 저장과 slice | upstream 구조·Pacing·AccessClass snapshot | 구조 생성, 후보 선택, access upgrade |

`ActivityStructure`와 `EventOverlay`를 generic `Event` 하나로 합치지 않는다.

`MicroChunk`는 resolved metadata를 저장할 수 있지만 그것을 **선택하거나 저작한 owner로 계산하지 않는다.**

---

## 4. PacingRole Contract

### 4.1 의미

`PacingRole`은 플레이 리듬과 분위기의 의도다. 접근 가능 여부나 필요한 도구를 뜻하지 않는다.

exact atomic roles:

```text
None
Quiet
Traversal
Discovery
Risk
Recovery
Safe
Machinery
Flow
Activity
Narrative
Reward
Landmark
Resource
Boss
Integrated
```

- `None`은 default/unset 표시에만 사용하며 실제 assignment 또는 compatibility set에서는 단독 허용하지 않는다.
- 여러 역할이 필요한 콘텐츠는 immutable role set 또는 validated flags로 표현한다.
- undefined bit/value, 빈 set, duplicate token은 거부한다.
- canonical order는 위 선언 순서다.
- exact stable tokens는 대문자 snake case다.

```text
QUIET | TRAVERSAL | DISCOVERY | RISK | RECOVERY | SAFE | MACHINERY | FLOW
ACTIVITY | NARRATIVE | REWARD | LANDMARK | RESOURCE | BOSS | INTEGRATED
```

### 4.2 PDF 표시 문구의 해석

PDF의 `Quiet+Traversal`, `RiskTraversal`, `Activity+Escort`, `MandatoryResource` 같은 문구를 새로운 단일 enum 값으로 복제하지 않는다.

- `Quiet+Traversal` → Pacing `{Quiet, Traversal}`
- `RiskTraversal` → Pacing `{Risk, Traversal}`
- `Activity+Escort` → Pacing `{Activity}` + 후속 Activity 종류 `Escort`
- `MandatoryResource` → Pacing `{Resource}` + 별도 mandatory/access 계약
- `Safe+Landmark` → Pacing `{Safe, Landmark}`

Escort/Maru/PhysicsChain/Tactical/TimeTrial 등은 MAP09_05 이후의 Activity/Event content kind이며 PacingRole이 아니다.

`Mandatory`, `Optional`, `Tool`, `Hidden`, `ProgressionGate`는 AccessClass 쪽 의미이며 PacingRole로 등록하지 않는다.

### 4.3 권한

- 실제 Sector/region `PacingRole` assignment owner는 MAP09_01의 `Pacing` pass다.
- 7개 layer는 Pacing을 직접 배정하지 않고 **호환 가능한 role set만 선언**할 수 있다.
- `PacingRole` 변경은 `AccessClass`, RouteType, socket mask를 바꿀 수 없다.
- 동일 PacingRole은 서로 다른 AccessClass와 조합 가능해야 한다.

MAP09_02에서는 Pacing planner, 가중치, 거리 계산, RNG draw를 구현하지 않는다.

---

## 5. AccessClass Contract

### 5.1 Exact values

`AccessClass`는 해당 연결/진입이 어떤 접근 조건을 갖는지 표현한다.

```text
Unspecified
MandatoryNoTool
OptionalNoTool
OptionalTool
OptionalEnvironment
OptionalExplosive
OptionalHidden
ProgressionGate
```

exact stable tokens:

```text
MANDATORY_NO_TOOL
OPTIONAL_NO_TOOL
OPTIONAL_TOOL
OPTIONAL_ENVIRONMENT
OPTIONAL_EXPLOSIVE
OPTIONAL_HIDDEN
PROGRESSION_GATE
```

- `Unspecified`는 default/unset이며 published contract에서 거부한다.
- case variation, whitespace variation, numeric string, undefined enum을 거부한다.
- AccessClass는 Walk/Jump/Drop/Climb 같은 movement kind를 포함하지 않는다.
- AccessClass는 Quiet/Risk/Reward 같은 Pacing 의미를 포함하지 않는다.

### 5.2 기존 계약과 exact mapping

| Existing approved contract | AccessClass |
|---|---|
| mandatory route + `mandatory_allowed=true` + `tool_requirement=NONE` | `MandatoryNoTool` |
| MAP08 mandatory boundary + `tool_requirement=NONE` | `MandatoryNoTool` |
| OptionalRegion `Basic` | `OptionalNoTool` |
| OptionalRegion `Tool` | `OptionalTool` |
| OptionalRegion `Environment` | `OptionalEnvironment` |
| OptionalRegion `Explosive` | `OptionalExplosive` |
| OptionalRegion `Hidden` | `OptionalHidden` |
| explicitly authored SpecialRegion progression/state gate | `ProgressionGate` |

`ProgressionGate`를 일반 mandatory route, MAP08 boundary, 임의 TerrainCluster 또는 Pattern에 배정할 수 없다.

mandatory 연결은 항상 `MandatoryNoTool`이어야 하며 Tool/Environment/Explosive/Hidden/ProgressionGate로 승격할 수 없다.

### 5.3 권한과 보존

- 일반 Sector edge AccessClass authority: `RouteType`
- 선예약 SpecialRegion의 fixed entry authority: `SpecialRegion`
- `TerrainCluster`, `MicroPattern`, `ActivityStructure`는 compatibility만 선언 가능
- `EventOverlay`는 inherited AccessClass를 보존해야 함
- `MicroChunk`는 resolved AccessClass provenance를 저장할 수 있으나 변경할 수 없음
- Activity/Event 제거 전후 AccessClass와 mandatory reachability가 같아야 함

MAP09_02에서는 실제 edge/entry assignment solver를 구현하지 않는다.

---

## 6. 구현할 Runtime Contract

기존 approved Runtime folder를 사용한다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/Pipeline/
Namespace: StarNight.Map.WorldGeneration.Pipeline
Assembly: Game.Map.Runtime
```

물리 파일 수와 내부 보조 타입 이름은 프로젝트 관례에 맞출 수 있지만, 최소 다음 semantic contract가 명확해야 한다.

```text
GenerationLayerId
LayerResponsibilityId
PacingRole
AccessClass
LayerPacingMode
LayerAccessMode
GenerationLayerContract
GenerationLayerCatalog
GenerationLayerCatalogValidator
GenerationLayerValidationError / Result
```

### 6.1 Layer mode

Pacing mode는 최소 다음을 구분한다.

```text
CompatibilityOnly
PreserveOnly
```

7개 layer 어느 것도 `Assign` 권한을 갖지 않는다. 실제 assignment owner는 `Pacing` pass다.

exact pacing mode:

```text
RouteType / MicroChunk = PreserveOnly
SpecialRegion / TerrainCluster / MicroPattern / ActivityStructure / EventOverlay
  = CompatibilityOnly
```

`CompatibilityOnly`는 content가 허용 role set을 선언할 수 있다는 뜻일 뿐, 실제 Sector의 PacingRole을 선택한다는 뜻이 아니다.

Access mode는 최소 다음을 구분한다.

```text
GeneralAuthority
SpecialEntryAuthority
CompatibilityOnly
PreserveOnly
```

exact authority:

```text
RouteType     = GeneralAuthority
SpecialRegion = SpecialEntryAuthority
TerrainCluster / MicroPattern / ActivityStructure = CompatibilityOnly
EventOverlay / MicroChunk = PreserveOnly
```

### 6.2 Immutability

- catalog와 entry collection은 외부 mutation 불가
- constructor input list/set은 defensive copy
- mutable static singleton state 금지
- culture, reflection discovery order, file enumeration order와 무관
- ordinal ID/token order 사용
- 실패 시 partial catalog publish 금지

### 6.3 Stable digest

catalog는 최소 다음을 canonical UTF-8 field order로 SHA-256 한다.

```text
layer order
layer ID
owned responsibility IDs
pacing mode
access mode
reservation/order invariants
PacingRole token table
AccessClass token table
```

display text, localized text, file path, timestamp, reflection order는 digest에 포함하지 않는다.

### 6.4 Validator exact gates

최소 다음을 accumulated, stable-sorted, deduplicated error로 검출한다.

1. layer count가 7이 아님
2. missing/duplicate layer ID
3. duplicate stable order
4. missing responsibility
5. duplicate responsibility owner
6. wrong responsibility owner
7. `SpecialRegion >= TerrainCluster` order 위반
8. `TerrainCluster >= MicroPattern` order 위반
9. `MicroPattern >= ActivityStructure/EventOverlay` order 위반
10. `MicroChunk`가 final layer가 아님
11. layer가 Pacing assignment authority를 주장함
12. RouteType 이외의 general access authority
13. SpecialRegion 이외의 special entry authority
14. invalid/undefined PacingRole
15. invalid/undefined AccessClass
16. Pacing token에 access 의미가 포함됨
17. Access token에 pacing/movement 의미가 포함됨
18. mandatory mapping이 `MandatoryNoTool`이 아님
19. Activity/Event 제거 시 access를 바꾸도록 선언됨
20. mutable collection exposure 또는 non-deterministic digest

오류 message 문자열 자체를 digest나 정렬의 유일 key로 사용하지 않는다.

---

## 7. 구현 경계

### 7.1 허용

- `WorldGeneration/Pipeline/`의 신규 Runtime C#과 matching `.meta`
- 대응 `Tests/EditMode/.../Pipeline/`의 신규 test C#과 matching `.meta`
- 필요 시 위 신규 production 파일 간 additive 분할
- 이 Task Result 문서

### 7.2 금지

- MAP00~08 production/test 수정
- MAP09_01 production/test 수정
- 기존 RouteType/route mask/optional access enum·codec 수정 또는 duplicate 구현
- 다른 V2 Runtime root에 production type 선행 구현
- MicroPattern 16-cell schema/operation/transform 구현
- TerrainCluster footprint/Spine/Envelope 구현
- Activity/Event/SpecialRegion production model 구현
- Pacing planner, access assignment solver, retry executor, RNG draw 구현
- CSV schema/registry/Authoring/Generated 변경
- Scene/Prefab/ScriptableObject/Editor Window 변경·생성
- ProjectSettings/Packages 수정
- asmdef/asmref 생성·수정
- `WorldGenerationRoot` 실행 연결
- runtime Tilemap/Collider/Streaming/Save 구현
- unrelated dirty worktree 포함

### 7.3 신규 V2 production scope 금지 symbol

```text
enum RouteType
class RouteType
struct RouteType
StageMapGenerator
GridWorld
RoomTemplate
RoomGridTransform
TileMutationService
SectorRecipeResolver
UnityEditor
```

문서 주석/테스트 negative fixture 문자열은 symbol hit와 분리 보고한다.

---

## 8. 필수 테스트

### 8.1 Focused MAP09_02

새 test에 exact category `MAP09_02`를 부여하고 최소 다음을 실제 자동 검증한다.

1. exact layer count 7, order `10..70`
2. exact layer IDs와 책임 owner matrix
3. responsibility 누락 0
4. duplicate responsibility owner 0
5. default catalog validation PASS
6. duplicate layer/order/responsibility negative fixture 각각 실패
7. wrong owner negative fixture 실패
8. Special→Terrain→Pattern→Activity/Event→MicroChunk order
9. MicroChunk final
10. layer Pacing assignment authority 0
11. exact PacingRole/token round-trip과 canonical order
12. invalid/default/undefined PacingRole 거부
13. exact AccessClass/token round-trip과 canonical order
14. invalid/default/case/space/numeric AccessClass 거부
15. mandatory route/boundary → MandatoryNoTool mapping
16. Optional Basic/Tool/Environment/Explosive/Hidden exact mapping
17. ProgressionGate의 일반 mandatory route 사용 거부
18. 같은 PacingRole + 서로 다른 AccessClass 조합 허용
19. 다른 PacingRole + 같은 AccessClass 조합 허용
20. Pacing 변경이 AccessClass/RouteType을 바꾸지 않음
21. Activity/Event remove-safe access declaration
22. MicroChunk provenance-only 선언
23. catalog/entry/role set 외부 mutation 거부
24. repeated catalog digest exact 일치
25. display text/reflection/file order가 digest에 영향 없음
26. 신규 V2 production scope duplicate RouteType/forbidden dependency 0

negative fixture는 throw 여부만 보지 말고 exact error code/accounting을 검증한다.

### 8.2 MAP09 phase regression

최종 코드 상태에서 별도로 실행한다.

```text
MAP09_01 exact category: 26/26 PASS
MAP09_02 exact category: discovered/executed 모두 >0, all PASS
```

MAP09_01 category에 새 test를 섞어 기존 `26` count를 바꾸지 않는다.

### 8.3 Required MAP05~08 regression

기존 exact selection을 변경하지 않고 실제 실행한다.

```text
MAP08 required union:  9220/9220 PASS
MAP07 required:        5422/5422 PASS
MAP06 required:        2746/2746 PASS
MAP05 required:        1959/1959 PASS
Required distinct:    19347/19347 PASS
```

Focused, MAP09_01 regression, MAP05~08 regression의 discovery/executed/PASS/FAIL/SKIP을 분리 보고한다.

timeout, zero-selection, 이전 job replay, compile-only 결과는 PASS 근거가 아니다.

### 8.4 Unity gate

- forced compile error `0`
- Console error `0`
- 이 Task 관련 warning `0`
- Editor idle / ready_for_tools equivalent true
- is_compiling false / play mode stopped

---

## 9. Static Gate

Result 작성 직전에 다음을 검사한다.

```text
Authoring CSV/meta: 50/50
Authoring manifest: f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb
Authoring tracked changes: 0
Generated CSV created: 0
Scene/Prefab tracked changes: 0/0
ProjectSettings/Packages task-owned changes: 0/0
asmdef/asmref tracked changes: 0/0
MAP00~08 production/test modifications: 0/0
MAP09_01 production/test modifications: 0/0
other V2 root production changes: 0
duplicate GUID groups: 0
unapplied MCP candidates: 0
duplicate RouteType production definitions in task scope: 0
forbidden symbol/dependency hits: 0
git diff --check errors: 0
unrelated dirty files staged/included: 0
```

Global Assets/Map meta count는 task-owned 신규 C# 수만큼 증가할 수 있다. before/after와 각 증가 원인을 보고한다.

---

## 10. 실패 처리

다음 중 하나라도 발생하면 다음 Task를 열지 않는다.

- predecessor hash/status mismatch
- existing RouteType/optional access 계약과 §2/§5 mapping 불일치
- 7-layer 또는 owner matrix 불일치
- duplicate responsibility owner
- PacingRole/AccessClass coupling
- mandatory route가 no-tool이 아님
- MAP09_01 `26/26` 또는 required `19347/19347` 실패/skip
- compile/Console error
- 기존 production/test/CSV/asmdef/Scene/Prefab 수정 필요
- forbidden legacy dependency 또는 duplicate RouteType 필요
- task-owned 변경과 unrelated worktree를 안전하게 분리할 수 없음

Result를 `FAIL` 또는 `BLOCKED`로 작성하고 같은 `MAP09_02` repair만 제안한다.

---

## 11. Result 문서 계약

작업 완료 후 다음 파일을 작성한다.

```text
MAP09_02_DEFINE_LAYER_PACING_AND_ACCESS_CONTRACTS_RESULT.md
```

Result 맨 위:

```text
TASK: MAP09_02_DEFINE_LAYER_PACING_AND_ACCESS_CONTRACTS
STATUS: PASS | FAIL | BLOCKED
MAP09_02: COMPLETE ELIGIBLE | NOT COMPLETE
MAP09_03_IMPLEMENT_MICROPATTERN_CONTRACTS: LOCKED / DO NOT START
```

### 필수 Result 섹션

1. `Preflight Audit`
   - predecessor Result/Task/archive hashes, Status, Unity/assembly/folder/API 감사
2. `Legacy Contract Evidence`
   - Type0~4, mandatory no-tool, OptionalRegion mapping, 무수정 증거
3. `Implemented File Inventory`
   - 신규/수정/삭제 C#과 matching meta
4. `Layer Responsibility Catalog`
   - exact 7 layer/order, owner matrix, modes, catalog digest
5. `PacingRole Evidence`
   - values/tokens, combination/immutability, assignment owner 분리
6. `AccessClass Evidence`
   - values/tokens, existing mapping, mandatory/special authority 분리
7. `Duplicate Responsibility Validation`
   - positive/negative fixtures와 exact error accounting
8. `Focused and MAP09 Regression`
   - MAP09_02 및 MAP09_01 discovery/executed/pass/fail/skip
9. `Required Regression`
   - MAP05/06/07/08와 distinct `19347`
10. `Unity Verification`
    - compile/Console/warning/Editor state
11. `Static Gates`
    - Authoring/Generated/meta/GUID/forbidden symbols/change scope/diff check
12. `Out-of-Scope Findings`
    - 기존 dirty 문제; 없으면 `None`
13. `Commit and Phase Decision`
    - atomic commit subject/hash handoff, push 여부, MAP09_03 잠금

수행하지 않은 검증을 PASS로 기록하지 않는다. 실제 commit hash는 Result의 `SELF` 표기와 별도로 CLI 최종 handoff에서 보고할 수 있다.

---

## 12. Commit Requirement

PASS Result와 Status Finalize 후 task-owned 파일만 하나의 atomic commit으로 만든다.

```text
Subject: MAP09_02: define layer pacing and access contracts
```

commit에는 다음만 포함한다.

- 설치·Archive된 MAP09_02 Task MD
- 신규 task-owned Runtime/Test C#과 matching meta
- MAP09_02 Result
- Finalize된 Status

관련 없는 dirty file을 stage하지 않는다. Git push는 하지 않는다. MAP09_03을 자동 시작하지 않는다.

---

## 13. 완료 판정

다음이 모두 만족될 때만 `STATUS: PASS`다.

- 기존 RouteType duplicate 생성 없이 Type0~4 의미 보존
- exact 7 layer와 9 responsibility owner matrix 승인
- responsibility duplicate/missing/wrong owner 자동 검출
- PacingRole과 AccessClass token·immutability·digest 승인
- Pacing assignment와 access authority 분리
- mandatory route/boundary가 `MandatoryNoTool`로 고정
- Activity/Event removal-safe 및 MicroChunk provenance-only 계약 승인
- MAP09_01 `26/26` PASS
- MAP09_02 focused 전부 PASS
- required `19347/19347` PASS
- compile/Console/warning `0/0/0`
- Authoring/Generated/Scene/Prefab/Settings/asmdef 변화 없음
- MAP00~08 및 MAP09_01 production/test 변화 없음
- atomic commit 범위가 이 Task에 한정됨

PASS Result가 사용자 검수를 통과한 뒤에만 `MAP09_03_IMPLEMENT_MICROPATTERN_CONTRACTS`를 별도 single MD Task로 연다.
