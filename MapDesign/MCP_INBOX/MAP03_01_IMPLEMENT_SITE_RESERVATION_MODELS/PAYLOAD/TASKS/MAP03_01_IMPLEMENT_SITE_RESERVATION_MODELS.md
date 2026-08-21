# MAP03_01 — Implement Site Reservation Models

```yaml
status_control:
  task_key: MAP03_01_IMPLEMENT_SITE_RESERVATION_MODELS
  result_file: REPORTS/MAP03_01_IMPLEMENT_SITE_RESERVATION_MODELS_RESULT.md
```

## TASK TYPE

```text
RUNTIME IMMUTABLE P01 SITE-RESERVATION VALUE/AGGREGATE MODELS + EDITMODE TESTS
```

## Objective

MAP02에서 승인된 exact 13×13/169-cell P00 grid 위에 P01 Site Reservation이 사용할 compile-time typed immutable 데이터 계약을 만든다.

이번 Task의 산출물은 typed reservation ID, site kind/footprint transform/entry side token, final-oriented footprint cell 집합, entry anchor, `CoreBiomeSeed`, per-sector reservation, site reservation, complete `SiteReservationSnapshot`이다. 모델의 자기 일관성·불변성·결정적 순서만 구현한다.

후보 열거, footprint mirror 적용, 충돌 탐색, 거리 index/cost, backtracking, Core capacity flood-fill, village bucket 추첨, `PASS_SITE` adapter/execution, generated CSV는 아직 구현하지 않는다.

## Mandatory Read Order

1. `00_MCP_ENTRYPOINT.md`
2. `01_PROJECT_LOCKED_RULES.md`
3. `02_MCP_WORK_RULES.md`
4. `03_DATA_CSV_RULES.md`
5. `04_UNITY_MCP_RULES.md`
6. `05_CHANGE_CONTROL_RULES.md`
7. `07_PATCH_APPLY_RULES.md`
8. `08_STATUS_FINALIZE_RULES.md`
9. `MASTER_IMPLEMENTATION_TASK_LIST.md`
10. `06_IMPLEMENTATION_STATUS.md`
11. 이 Task
12. `REPORTS/MAP02_08_MAP02_EXIT_TESTS_RESULT.md`

MAP02_08 Result에서 exact 아래를 확인한다.

```text
STATUS: PASS
MAP02 EXIT: APPROVED
MAP03 ENTRY: ELIGIBLE FOR SEPARATE PATCH
MAP03_01: LOCKED / DO NOT START
```

이 별도 patch가 적용된 뒤에만 MAP03_01을 실행한다.

## Map Package Reference

Map Package v1.0 exact installed path가 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/01_WORLD_FIXED_SPEC.md
01_FIXED_SPEC/02_COORDINATE_AND_ID_RULES.md
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
01_FIXED_SPEC/04_RUNTIME_ARCHITECTURE.md
01_FIXED_SPEC/05_IMPLEMENTATION_ORDER.md
02_PHASE_ROADMAP/MAP03_SPECIAL_SITE_RESERVATION.md
05_GENERATED_OUTPUT_SCHEMA/generated_special_sites.csv
```

exact 문서가 installed tree에 없으면 이 Task의 frozen contracts를 authoritative fallback으로 사용한다. 대체 GDD, 과거 하네스, Legacy generator를 broad search하지 않는다.

## READ ALLOWLIST

### Existing Domain / Data APIs

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/BiomeDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/BiomeBoundaryDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/BiomeBoundaryDefinitionSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/SpecialMapDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/VillageDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/SpecialVillageDefinitionSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/StaticDataRegistry.cs
```

### Existing Generation APIs

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedSectorRole.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedWorldData.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGridIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorNeighborIndices.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GridInitializationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationPassContracts.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationArtifactStore.cs
```

### Focused Tests / Assemblies

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/CoordinateValueTypeTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/BiomeBoundaryDefinitionBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/SpecialVillageDefinitionBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/StaticDataRegistryBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/GeneratedWorldDataTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/GridInitializationPassTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map02ExitTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

위 C#/asmdef와 matching meta, 기존 `Generation` Runtime/Test 직계 파일명의 path-only inventory, Authoring CSV/meta count·hash, 전체 meta GUID, task marker 이후 change-scope path만 확인할 수 있다.

금지:

- Authoring CSV body 재해석 또는 수정
- MAP03_02 이후 Task body
- Legacy/Stage/P6/P11 generator body
- unrelated production/test C# body
- Scene/Prefab YAML

## WRITE ALLOWLIST

### 신규 Runtime production C# — exact 8

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteFootprint.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteEntryAnchor.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreBiomeSeed.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorReservation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSnapshot.cs
```

### 신규 Runtime EditMode test — exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationModelsTests.cs
```

신규 C# 9개와 matching `.cs.meta` 9개, Result 1만 생성한다. 기존 production/tests/meta/asmdef/asmref는 수정하지 않는다. 기존 approved directory를 재사용하며 새 directory/folder meta를 만들지 않는다.

## Namespace / Assembly

```text
Runtime namespace: StarNight.Map.WorldGeneration.Generation
Test namespace:    StarNight.Map.Tests.WorldGeneration.Generation
Runtime assembly:  Game.Map.Runtime
Test assembly:     Game.Map.Tests.EditMode
```

`UnityEditor`, `UnityEngine.Object`, ScriptableObject/MonoBehaviour, serialization callback, reflection factory, service locator, singleton/static mutable state를 도입하지 않는다. Unity 6000.3.8f1의 현재 language level에서 compile되도록 record/record struct, `required`, `init`, nullable-reference directive에 의존하지 않는다.

## Frozen P01 Boundary

```text
Input artifact  = GRID
Output artifact = SITE_RESERVATIONS
Pass ID         = PASS_SITE
RNG stream      = RNG_WORLD_SITE / WORLD
Grid            = 13 x 13 / 169 sectors / index y*13+x / lower-left origin
Allowed transform tokens = R0 | MIRROR_X | MIRROR_Y | R180
Allowed entry side tokens = L | R | U | D
```

P01 output은 별도 immutable artifact다. 기존 `GridInitializationResult`, `GeneratedWorldData`, `SectorCell`을 mutate하지 않는다. 모델 constructor도 Registry/RNG/clock/filesystem/Unity lifecycle을 읽지 않는다.

## `SiteReservationId` Contract

`SiteReservationId`는 `public readonly struct`, `IEquatable<SiteReservationId>`, `IComparable<SiteReservationId>`다.

```text
string Value
bool IsValid
SiteReservationId(string value)
bool TryCreate(string value, out SiteReservationId result)
```

- canonical grammar는 exact `^[A-Z0-9_]+$`이며 empty/whitespace/lowercase/hyphen/non-ASCII를 거부한다.
- ordinal case-sensitive equality/order와 deterministic hash를 제공한다.
- `ToString()`은 valid instance에서 exact `Value`다.
- default struct는 `IsValid == false`; 다른 모든 reservation model constructor는 default ID를 거부한다.
- ID 자동 생성, seed/order 접두사 규칙, random suffix는 이 Task에서 만들지 않는다. MAP03_06이 caller-supplied canonical ID를 결정한다.

## Enum / Token Contract

`SiteReservationEnums.cs`는 exact enum과 stateless `SiteReservationTokenCodec`을 제공한다.

```text
SiteReservationKind: Start, CoreResource, Forge, Boss, Village
SiteFootprintTransform: R0, MirrorX, MirrorY, R180
SiteEntrySide: L, R, U, D
```

exact case-sensitive token mapping:

| Enum | Token |
|---|---|
| Start | `START` |
| CoreResource | `CORE_RESOURCE` |
| Forge | `FORGE` |
| Boss | `BOSS` |
| Village | `VILLAGE` |
| R0 | `R0` |
| MirrorX | `MIRROR_X` |
| MirrorY | `MIRROR_Y` |
| R180 | `R180` |
| L/R/U/D | `L/R/U/D` |

각 enum의 exact `TryParse...`와 `ToToken`을 제공한다. null/empty/space/case/numeric/undefined enum을 거부한다. entry side의 exact opposite와 delta를 제공하되 local coordinate mirror/rotation 함수는 구현하지 않는다.

```text
L = (-1, 0), R = (1, 0), U = (0, 1), D = (0, -1)
Opposite: L<->R, U<->D
```

## `SiteFootprintCell` / `SiteFootprint` Contract

두 타입은 `SiteFootprint.cs`에 둔다. source definition의 transform이 이미 적용된 **final-oriented local footprint**를 표현한다.

`SiteFootprintCell` immutable properties:

```text
int LocalX
int LocalY
string LocalRole
string RequiredPrimaryBiomeId
string FixedSectorRecipeId
IReadOnlyList<SiteEntrySide> RequiredOpenSides
```

- local x/y는 음수가 아니어야 한다.
- `LocalRole`은 canonical non-empty ID, biome/recipe는 canonical ID 또는 empty다.
- required sides는 copied read-only set이며 duplicate/undefined를 거부하고 L/R/U/D canonical order로 보관한다.

`SiteFootprint` immutable properties/API:

```text
int Width
int Height
SiteFootprintTransform Transform
IReadOnlyList<SiteFootprintCell> Cells
SiteFootprint(int width, int height, SiteFootprintTransform transform, IEnumerable<SiteFootprintCell> cells)
bool TryGetCell(int localX, int localY, out SiteFootprintCell cell)
```

- width/height는 `1..WorldGenConstants.SectorColumns/Rows` 범위다.
- cell은 non-null/non-empty이며 각 local coordinate가 `[0,width) × [0,height)` 안에 있어야 한다.
- duplicate local coordinate를 거부한다.
- sparse footprint는 허용하고 자동으로 rectangle을 채우지 않는다.
- caller order와 무관하게 `LocalY`, `LocalX` 오름차순 copied read-only snapshot을 보관한다.
- source transform 계산·coordinate mirror는 MAP03_03 책임이다. 이 모델은 handed-in final-oriented cell을 다시 변환하지 않는다.

## `SiteEntryAnchor` Contract

immutable properties/API:

```text
SiteReservationId ReservationId
string EntrySocketId
SectorCoord FootprintSector
SiteEntrySide Side
IReadOnlyList<int> AllowedRouteTypes
bool Required
bool ReturnPathRequired
bool TryGetExteriorSector(out SectorCoord exteriorSector)
```

- `FootprintSector`는 exact world grid 안의 occupied site sector다.
- side는 footprint sector에서 바깥 일반 sector를 향한다.
- route types는 exact `1|2|3` domain의 non-empty unique copied set이며 오름차순으로 보관한다.
- `TryGetExteriorSector`는 side delta를 한 번 적용하고 grid 밖이면 false를 반환한다. clamp/wrap/normalize하지 않는다.
- entry compatibility, exterior occupancy, route connection, required count는 검사하지 않는다.

## `CoreBiomeSeed` Contract

immutable properties:

```text
SiteReservationId SourceReservationId
string BiomeId
string CorePatchRuleId
SectorCoord SeedSector
int MinimumCoreSectorCount
int BufferRingSectors
```

- source ID와 canonical biome/core-patch IDs를 보존한다.
- seed sector는 world grid 안이어야 한다.
- minimum은 `>=1`, buffer는 `>=0`이다.
- flood-fill, available capacity, altitude, edge 접촉, patch growth를 계산하지 않는다.

## `SectorReservation` Contract

exact 169-cell snapshot의 한 cell을 표현한다.

```text
int Index
SectorCoord Coordinate
bool IsReserved
SiteReservationId? ReservationId
SiteReservationKind? Kind
int LocalX
int LocalY
string LocalRole

SectorReservation CreateUnreserved(int index, SectorCoord coordinate)
SectorReservation CreateReserved(int index, SectorCoord coordinate,
    SiteReservationId reservationId, SiteReservationKind kind,
    int localX, int localY, string localRole)
```

- index/coordinate는 existing `WorldGridIndex`와 exact 일치한다.
- unreserved는 null ID/kind, local `-1/-1`, empty role다.
- reserved는 valid ID/kind, non-negative local coordinate, canonical non-empty local role다.
- constructor/factory는 overlap을 해소하거나 role을 `GeneratedWorldData`에 기록하지 않는다.

## `SiteReservation` Contract

immutable properties/API:

```text
SiteReservationId ReservationId
SiteReservationKind Kind
string SourceDefinitionId
SectorCoord Origin
SiteFootprint Footprint
string PrimaryBiomeId
int ReservationOrder
IReadOnlyList<SiteEntryAnchor> EntryAnchors
IReadOnlyList<SectorCoord> OccupiedSectors
bool TryGetFootprintCell(SectorCoord sector, out SiteFootprintCell cell)
```

- origin + final-oriented local cell로 occupied sector를 만들며 모두 world grid 안이어야 한다.
- SourceDefinitionId는 canonical non-empty ID다. Start는 world/profile ID를 사용할 수 있고 special/village catalog 존재를 이 모델에서 lookup하지 않는다.
- PrimaryBiomeId는 canonical ID 또는 empty이며 Kind별 필수 여부를 이 Task에서 강제하지 않는다.
- reservation order는 `>=0`이다.
- entry anchor의 ReservationId가 self와 같고 footprint sector가 occupied set에 속해야 한다.
- entry socket ID는 한 reservation 안에서 ordinal unique다.
- entry는 socket ID ordinal 순, occupied sectors는 `WorldGridIndex` 순으로 copied read-only 보관한다.
- entry가 0개인 중간 모델은 표현 가능하다. required entry/site count는 later validator 책임이다.

## `SiteReservationSnapshot` Contract

P01의 complete immutable artifact다.

```text
ulong Seed
SiteReservation StartReservation
SectorCoord StartAnchor
IReadOnlyList<SiteReservation> Reservations
IReadOnlyList<SectorReservation> Sectors
IReadOnlyList<SiteEntryAnchor> EntryAnchors
IReadOnlyList<CoreBiomeSeed> CoreBiomeSeeds

SiteReservationSnapshot(ulong seed,
    IEnumerable<SiteReservation> reservations,
    IEnumerable<SectorReservation> sectors,
    IEnumerable<CoreBiomeSeed> coreBiomeSeeds)

SectorReservation GetSector(int index)
SectorReservation GetSector(SectorCoord coordinate)
bool TryGetReservation(SiteReservationId id, out SiteReservation reservation)
```

snapshot invariant:

- reservation은 non-null/non-empty, ReservationId와 ReservationOrder 각각 unique다.
- exact one `Kind == Start`; `StartReservation`은 그 instance이고 `StartAnchor == StartReservation.Origin`이다.
- sector entries는 exact 169, index set `0..168`, coordinate/index exact, caller order와 무관하게 index 순이다.
- 모든 reservation occupied cell은 exact one reserved sector entry와 ID/kind/local/role이 일치한다.
- reserved sector가 어떤 reservation footprint에도 없거나 unreserved entry가 occupied cell을 가리키면 거부한다.
- site footprint 간 overlap은 숨기거나 winner를 선택하지 않고 constructor가 거부한다.
- entry anchors는 reservations에서 flatten하고 `(ReservationId, EntrySocketId)` 순으로 고정한다.
- core seed source는 existing reservation이며 kind가 CoreResource 또는 Forge여야 한다. source reservation당 최대 하나, seed list는 reservation ID ordinal 순이다.
- Reservations는 ReservationOrder, ReservationId ordinal 순으로 copied read-only 보관한다.
- source collection과 nested list를 방어 복사하고 public mutable collection/setter를 노출하지 않는다.

snapshot은 required special site count, distance, quadrant clustering, village distribution, entry exterior collision, Core capacity를 승인하지 않는다. 그것들은 MAP03_02~09 책임이다.

## Determinism / Culture / Ownership

- 같은 logical input의 collection insertion order가 달라도 exact ordering/equality-observable properties가 같다.
- ordinal ID/token 규칙만 사용하며 current culture, wall clock, frame, thread, filesystem, Unity object state에 의존하지 않는다.
- 최소 `en-US`, `tr-TR`에서 ID/token/order 결과가 같다.
- caller-owned arrays/lists mutation이 완성 모델을 바꾸지 않는다.
- static mutable cache, global current snapshot, lazy enumeration, exposed mutable array/dictionary를 만들지 않는다.
- `GeneratedWorldData`, `SectorCell`, Registry definition을 clone/filter/mutate하지 않는다.

## Scope Boundary / DO NOT

- `PASS_SITE`, `SpecialSiteReservationPass`, Root registry/adapter 구현 금지
- Start/special/village candidate 열거 또는 RNG draw 금지
- `R0/MIRROR_X/MIRROR_Y/R180` coordinate transform 적용 금지
- collision solver, distance index, candidate cost, backtracking 금지
- Core capacity flood-fill, biome painting/patch ID 할당 금지
- village `20/50/30` bucket 추첨/layout 선택 금지
- site instance/reservation ID 자동 생성 금지
- generated_special_sites serializer/file I/O/replay bundle 확장 금지
- existing GeneratedWorldData/SectorCell/serializer/root/manifest/overlay 수정 금지
- JSON/ScriptableObject/cache/EditorWindow/Gizmo/visual object 생성 금지
- Authoring/generated CSV/meta, asmdef/asmref, Scene/Prefab, Package/ProjectSettings 변경 금지
- test skip/ignore/assertion 완화, Git operation 금지
- MAP03_02 선행 작업 금지

## Collision Handling

1. 신규 destination이 없으면 생성한다.
2. 동일 경로가 exact 계약과 바이트 동일하면 `PREEXISTING_IDENTICAL`로 기록하고 재사용할 수 있다.
3. 다르면 덮어쓰기·병합·삭제하지 않고 `STATUS: BLOCKED`다.
4. 기존 `.meta`와 사용자 변경을 보존한다.

## Required Tests

`SiteReservationModelsTests.cs` actual NUnit cases 최소 `64`개다. parameterized case는 Test Runner actual case로 집계돼야 한다.

minimum groups:

- ReservationId valid/invalid/default/equality/order/hash/TryCreate/culture
- exact kind/transform/side token parse/format, undefined/case/numeric rejection, side delta/opposite
- footprint cell fields, copied sides, duplicate/undefined rejection
- footprint width/height/cell bounds, sparse support, duplicate rejection, deterministic sort, lookup, source mutation isolation
- entry anchor identity, route set sort/copy/rejection, four exterior deltas, four boundary false/no clamp
- CoreBiomeSeed ID/sector/count/buffer validation and immutable fields
- unreserved/reserved SectorReservation exact defaults and WorldGridIndex mismatch rejection
- SiteReservation occupied mapping, ordering, copied entries, duplicate socket, wrong ID, non-footprint entry, world-bound rejection
- valid complete snapshot exact 169 cells, one Start, deterministic ordering/lookups/flattened anchors/core seeds
- snapshot missing/duplicate Start, duplicate ID/order, missing/extra/wrong sector, overlap, orphan/wrong-kind/duplicate core seed rejection
- source/nested collection mutation isolation and public mutation-surface reflection audit
- `en-US`/`tr-TR` culture invariance
- production dependency audit: UnityEditor/UnityEngine.Object/RNG/time/file I/O/static mutable state `0`

금지:

- `[Ignore]`, `[Explicit]`, inconclusive, assumption skip
- reflection으로 private field를 mutate해 success를 만드는 test
- current test order/working directory/existing filesystem에 의존
- invalid expected 값을 현재 구현에 맞춰 완화

## Regression / Verification

```text
New SiteReservationModelsTests: >=64 PASS
MAP02 phase focused aggregate: 667/667 PASS
SpecialVillageDefinitionBuilderTests: 48/48 PASS
BiomeBoundaryDefinitionBuilderTests: 36/36 PASS
StaticDataRegistryBuilderTests: 53/53 PASS
ContentVersionHash: 54/54 PASS
Previous Game.Map targeted baseline: 1514/1514 PASS
Game.Map targeted total: >=1578 PASS
Previous full project EditMode baseline: 1554/1554 PASS
Full project EditMode total: >=1618 PASS
failed = 0 / skipped = 0
Unity 6000.3.8f1 / forced refresh / compile error 0 / relevant warning 0
PlayMode NOT RUN / Visual NOT APPLICABLE / saved Scene-Prefab changes NONE
```

Unity compile/test 증거가 없으면 `BLOCKED`다.

## Asset / Meta / Change Gate

clean baseline:

```text
Authoring CSV/meta = 50/50
Assets meta = 2989
accepted legacy Editor folder meta = 6/6
duplicate GUID groups = 0
```

완료 시:

```text
new Runtime production C# = 8
new Runtime test C# = 1
new matching cs.meta = 9
final Assets meta = 2998
task-marker 이후 exact Assets changes = 18
existing Assets modifications = 0
unexpected Assets changes = 0
new directory/folder meta = 0
```

Authoring CSV/meta `50/50`과 accepted legacy folder meta `6/6`의 bytes/hash를 보존하고, 신규 meta는 `fileFormatVersion: 2`, unique lowercase 32-hex GUID, project duplicate GUID `0`이어야 한다.

## Failure Policy

- constructor/API/test/compile/meta/change-scope 한 조건이라도 불일치하면 `STATUS: FAIL`이다.
- Unity/Test Runner 접근이 없어 실제 compile/regression을 수행할 수 없으면 `STATUS: BLOCKED`다.
- FAIL/BLOCKED를 existing production 수정, assertion 완화, later Task 구현으로 해결하지 않는다.
- PASS가 아니면 STATUS FINALIZE를 수행하지 않고 MAP03_02를 열지 않는다.

## Result / Completion

Result: `REPORTS/MAP03_01_IMPLEMENT_SITE_RESERVATION_MODELS_RESULT.md`.

필수 섹션:

```text
TASK
STATUS
SUMMARY
PATCH APPLY
READ
MASTER BACKLOG CHECK
MAP02 EXIT GATE CHECK
CREATED
MODIFIED
PREEXISTING_IDENTICAL
RESERVATION ID
ENUM AND TOKENS
FOOTPRINT
ENTRY ANCHOR
CORE BIOME SEED
SECTOR RESERVATION
SITE RESERVATION
SITE RESERVATION SNAPSHOT
DETERMINISM AND IMMUTABILITY
TEST
UNITY
ASSET META VALIDATION
CHANGE SCOPE
PRODUCTION OWNERSHIP AUDIT
OUT_OF_SCOPE_FINDINGS
DONE CONDITIONS
NEXT
Recommended Commit
```

PASS Result에는 focused actual case 수, targeted/full counts, new/existing change inventory, final meta/GUID, exact no-later-work evidence를 기록한다.

모든 조건 PASS 시에만 MAP03_01 COMPLETE, Current Task NONE으로 finalize한다. `MAP03_02_ENUMERATE_START_AND_SITE_CANDIDATES`는 LOCKED로 유지하고 자동 시작하지 않는다.

Recommended Commit: `feat(map): define immutable site reservation models`
