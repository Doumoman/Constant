# MAP03_03 — Implement Footprint Placement Solver

```yaml
status_control:
  task_key: MAP03_03_IMPLEMENT_FOOTPRINT_PLACEMENT_SOLVER
  result_file: REPORTS/MAP03_03_IMPLEMENT_FOOTPRINT_PLACEMENT_SOLVER_RESULT.md
```

## TASK TYPE

```text
RUNTIME DETERMINISTIC FOOTPRINT TRANSFORM + WORLD-BOUND/COLLISION PLACEMENT SOLVER + EDITMODE TESTS
```

## Objective

MAP03_02의 raw `SiteOriginCandidate`와 immutable typed special-map definitions를 받아, 승인된 네 transform만 적용한 **개별 placement option**을 만든다.

이번 Task의 소유권은 다음 네 가지뿐이다.

1. footprint cell / required-open-side / entry socket을 하나의 동일 transform 규칙으로 변환
2. transformed footprint의 13×13 world-bound 검사
3. 이미 점유된 footprint 및 보호된 기존 entry approach와의 충돌 검사
4. candidate entry가 world 안쪽의 비점유 일반 sector를 향하는지 검사

거리, 고도, 비용, 가중치, RNG, option 선택, reservation ID, backtracking, Core 용량, Village, `PASS_SITE`는 수행하지 않는다. solver는 전달받은 **candidate 하나 + transform 하나**를 성공 placement 또는 stable rejection으로만 판정한다.

starter empty-blocker exact evaluation:

```text
Start candidates:                  88 x R0 =   88 evaluations /   88 success
Five special-site raw origins:    845 x 4  = 3380 evaluations / 3068 success
All:                                         3468 evaluations / 3156 success
Rejections: FootprintOutsideWorld 52 + EntryOutsideWorld 260 = 312
```

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
12. `REPORTS/MAP03_02_ENUMERATE_START_AND_SITE_CANDIDATES_RESULT.md`

MAP03_02 Result의 exact `STATUS: PASS`, focused `268/268`, targeted `1863/1863`, full `1903/1903`, groups/candidates `6/933`, final Assets meta `3005`, existing Assets modification `0`을 확인한다.

## Map Package Reference

Map Package v1.0 exact installed path가 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/01_WORLD_FIXED_SPEC.md
01_FIXED_SPEC/02_COORDINATE_AND_ID_RULES.md
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
01_FIXED_SPEC/05_IMPLEMENTATION_ORDER.md
02_PHASE_ROADMAP/MAP03_SPECIAL_SITE_RESERVATION.md
04_CSV_STARTER/special_map_catalog.csv
04_CSV_STARTER/special_map_footprint_cells.csv
04_CSV_STARTER/special_map_entry_sockets.csv
```

exact 문서/CSV가 installed tree에 없으면 이 Task의 frozen contracts와 현재 immutable Registry objects를 authoritative fallback으로 사용한다. Authoring CSV를 직접 읽거나 재파싱하지 않는다.

## READ ALLOWLIST

### Existing typed definition roots

```text
Assets/_Game/Map/Runtime/WorldGeneration/Data/SpecialMapDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/SpecialVillageDefinitionSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/StaticDataRegistry.cs
```

### Existing grid, reservation, and candidate models

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGridIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteFootprint.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteEntryAnchor.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteOriginCandidate.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateGroup.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCatalog.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateEnumerator.cs
```

### Focused tests / assemblies

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/SpecialVillageDefinitionBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationModelsTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteCandidateEnumerationTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

위 C#/asmdef와 matching meta, approved `Generation` Runtime/Test 직계 파일명의 path-only inventory, Authoring CSV/meta count·hash, 전체 meta GUID, task marker 이후 change-scope path만 확인할 수 있다.

금지:

- Authoring CSV body 직접 재파싱·수정
- MAP03_04 이후 Task body
- unrelated production/test C# body
- Legacy/Stage/P6/P11 generator body
- Scene/Prefab YAML

## WRITE ALLOWLIST

### 신규 Runtime production C# — exact 6

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteFootprintTransformer.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacementEntry.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacement.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacementBlockers.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacementResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacementSolver.cs
```

### 신규 Runtime EditMode test — exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/FootprintPlacementSolverTests.cs
```

신규 C# 7개와 matching `.cs.meta` 7개, Result 1만 생성한다. 기존 production/tests/meta/asmdef/asmref는 수정하지 않는다. 기존 approved directory를 재사용하며 새 directory/folder meta를 만들지 않는다.

## Namespace / Assembly

```text
Runtime namespace: StarNight.Map.WorldGeneration.Generation
Test namespace:    StarNight.Map.Tests.WorldGeneration.Generation
Runtime assembly:  Game.Map.Runtime
Test assembly:     Game.Map.Tests.EditMode
```

`UnityEditor`, Unity object/lifecycle, record/record struct, `required`, `init`, nullable-reference directive, reflection factory, singleton/static mutable state를 도입하지 않는다.

## Public Solver API

```text
public sealed class FootprintPlacementSolver

FootprintPlacementResult SolveStart(
    SiteOriginCandidate candidate,
    FootprintPlacementBlockers blockers)

FootprintPlacementResult SolveSpecialSite(
    SiteOriginCandidate candidate,
    SiteFootprintTransform transform,
    SpecialMapDefinition specialMap,
    IEnumerable<SpecialMapFootprintCellDefinition> footprintCells,
    IEnumerable<SpecialMapEntrySocketDefinition> entrySockets,
    FootprintPlacementBlockers blockers)
```

호출자는 immutable Registry roots에서 candidate와 exact parent-scoped definition collections를 전달한다. solver는 Registry singleton, filesystem, CSV, generated output에서 자체 조회하지 않는다.

각 호출은 option 하나를 판정한다. solver가 네 transform을 내부 열거하거나 최선 option을 선택하지 않는다. MAP03_04~06 caller가 candidate/transform option을 순회·필터·선택한다.

## Input Gate

공통:

- candidate와 blockers는 non-null이다.
- candidate origin/index identity와 world-bound는 existing `SiteOriginCandidate` invariant를 그대로 보존한다.
- enum numeric cast/undefined transform, kind, side는 거부한다.
- 입력 collection은 즉시 snapshot하고 null item, duplicate logical key, inconsistent parent ID를 stable failure로 반환한다.
- expected validation failure는 exception 대신 failure result다. 기존 immutable model constructor의 programmer-error exception을 catch해 성공으로 바꾸지 않는다.

`SolveStart`:

- candidate kind는 exact `Start`다.
- transform input은 없고 exact `R0`만 사용한다.
- source definition lookup을 요구하지 않는다.
- synthetic `1×1` footprint cell은 local `(0,0)`, role `START`, empty biome/recipe, empty required-open-sides다.
- entry는 `0`개다. MAP03_03은 Start origin 자체만 footprint로 배치한다.

`SolveSpecialSite`:

- candidate kind는 `Boss | Forge | CoreResource` 중 하나다. `Start | Village`는 거부한다.
- candidate `SourceDefinitionId`와 specialMap ID, 모든 footprint/entry parent ID가 ordinal exact 일치한다.
- specialMap은 active, required count positive, positive width/height, recognized exact site role/kind identity다.
- footprintCells는 non-null/non-empty, local coordinate unique, 모두 source width/height 안이고 canonical role/biome/recipe/side payload를 보존한다.
- entrySockets는 non-null/non-empty, socket ID ordinal unique, local coordinate가 source footprint cell에 exact 존재한다.
- 모든 entry의 allowed route types는 non-empty unique `1|2|3`, side defined, bool fields 그대로 보존한다.
- 적어도 하나의 `Required == true` entry가 있어야 한다. optional entry도 변환 후 silently drop하지 않는다.

## Exact Transform Contract

source dimensions는 `W × H`이며 네 transform 모두 output dimensions를 `W × H`로 유지한다.

| Transform | Local coordinate | Side mapping |
|---|---|---|
| `R0` | `(x, y)` | `L/R/U/D` unchanged |
| `MirrorX` | `(W - 1 - x, y)` | `L↔R`, `U/D` unchanged |
| `MirrorY` | `(x, H - 1 - y)` | `U↔D`, `L/R` unchanged |
| `R180` | `(W - 1 - x, H - 1 - y)` | `L↔R`, `U↔D` |

`SiteFootprintTransformer`는 stateless pure API를 제공한다.

```text
bool TryTransformCoordinate(int width, int height,
    SiteFootprintTransform transform,
    int sourceX, int sourceY,
    out int transformedX, out int transformedY)

bool TryTransformSide(SiteFootprintTransform transform,
    SiteEntrySide sourceSide,
    out SiteEntrySide transformedSide)
```

- footprint cell local coordinate와 `RequiredOpenSides`를 함께 변환한다.
- entry socket local coordinate와 side를 exact 같은 함수로 함께 변환한다.
- role, biome ID, recipe ID, socket ID, route types, required/return flags는 값 변경 없이 보존한다.
- transformed cells는 `SiteFootprint`의 canonical `LocalY, LocalX` order, sides는 L/R/U/D order를 따른다.
- R90/R270/diagonal reflection/swap-width-height/clamp/wrap/scale는 없다.
- source objects/collections/nested lists를 mutate하거나 transformed payload를 source object에 cache하지 않는다.

## `FootprintPlacementEntry` Contract

immutable properties:

```text
string EntrySocketId
int LocalX
int LocalY
SectorCoord FootprintSector
SiteEntrySide Side
SectorCoord ExteriorSector
IReadOnlyList<int> AllowedRouteTypes
bool Required
bool ReturnPathRequired
```

invariants:

- canonical non-empty socket ID, non-negative local coordinate, defined side다.
- `FootprintSector`와 `ExteriorSector`는 world-bound다.
- exterior는 footprint sector에 side delta를 exact 한 번 적용한 좌표다.
- route types는 unique ascending copied read-only `1|2|3`다.
- reservation ID, cost, selected state, route connection 결과를 포함하지 않는다.

## `FootprintPlacement` Contract

immutable properties/API:

```text
SiteOriginCandidate Candidate
SiteFootprint Footprint
IReadOnlyList<SectorCoord> OccupiedSectors
IReadOnlyList<FootprintPlacementEntry> Entries

bool TryGetFootprintCell(SectorCoord sector, out SiteFootprintCell cell)
```

- `Footprint.Transform`은 evaluated transform과 exact 같다.
- occupied sector는 `candidate.Origin + final local cell`이며 모두 world-bound/unique다.
- occupied sector는 `WorldGridIndex` 오름차순 copied read-only snapshot이다.
- Entries는 `EntrySocketId` ordinal 순 copied read-only snapshot이다.
- 각 entry local coordinate는 Footprint cell에 존재하고 `FootprintSector`가 해당 occupied sector와 exact 같다.
- entry face `(FootprintSector, Side)`는 unique다.
- caller collection mutation이 completed placement를 바꾸지 않는다.
- reservation ID, reservation order, primary biome choice, cost/score/weight를 만들지 않는다.

## `FootprintPlacementBlockers` Contract

immutable properties/API:

```text
IReadOnlyList<int> OccupiedSectorIndices
IReadOnlyList<int> ProtectedEntryApproachSectorIndices

static FootprintPlacementBlockers Empty { get; }

FootprintPlacementBlockers(
    IEnumerable<int> occupiedSectorIndices,
    IEnumerable<int> protectedEntryApproachSectorIndices)

static FootprintPlacementBlockers FromReservations(
    IEnumerable<SiteReservation> reservations)

bool IsOccupied(int sectorIndex)
bool IsProtectedEntryApproach(int sectorIndex)
```

- 모든 index는 exact `0..168`; 각 set은 duplicate를 거부하고 ascending copied read-only 보관한다.
- 같은 index가 occupied와 protected set 양쪽에 들어가는 inconsistent blocker는 거부한다.
- `Empty`는 immutable singleton value이며 exposed mutable state가 없다.
- `FromReservations`는 null reservation/item, duplicate reservation ID, overlapping occupied sector, world 밖 entry exterior를 거부한다.
- occupied는 모든 reservation occupied sector의 union이다.
- protected approach는 모든 existing entry anchor의 valid exterior sector union이다. 여러 entry가 같은 exterior를 공유하면 deterministic dedupe한다.
- `FromReservations`는 reservation/snapshot/entry를 mutate하거나 winner를 고르지 않는다.

## Placement Validation Order

### Phase 1 — source and transform

input/source identity와 transformed footprint/entry local payload를 검증한다. source/transform 오류가 있으면 world placement를 publish하지 않는다.

### Phase 2 — footprint world placement

각 transformed footprint cell에 대해:

```text
worldX = candidate.Origin.X + localX
worldY = candidate.Origin.Y + localY
```

아래 순서로 판정한다.

1. 어떤 footprint cell이라도 world 밖이면 `FootprintOutsideWorld`.
2. footprint가 blocker occupied sector와 교차하면 `FootprintOverlap`.
3. footprint가 protected existing entry approach와 교차하면 `BlocksExistingEntryApproach`.

Phase 2 실패 시 Phase 3 entry exterior를 계산하지 않는다. 이 precedence가 starter rejection bucket을 고정한다.

### Phase 3 — candidate entry approaches

각 transformed entry에 대해:

1. transformed local coordinate가 transformed footprint cell에 없으면 `EntryNotOnFootprint`.
2. `(footprint sector, side)` duplicate면 `DuplicateEntryFace`.
3. side delta exterior가 world 밖이면 `EntryOutsideWorld`.
4. exterior가 candidate 자체 occupied footprint에 있으면 `EntryFacesOwnFootprint`.
5. exterior가 blocker occupied sector면 `EntryApproachOccupied`.
6. exterior가 blocker protected approach와 같은 것은 허용한다. 이것은 두 future route가 같은 일반 sector를 향하는 표현이며, 이번 Task는 route 연결/면 호환을 판단하지 않는다.

candidate entries끼리 서로 같은 exterior sector를 공유하는 것도 face가 다르면 허용한다. one-cell buffer, diagonal clearance, required-open-side ↔ entry equality, route-mask compatibility를 새로 만들지 않는다.

## Error / Result Contract

`FootprintPlacementErrorCode` exact frozen ordinal order:

```text
MissingCandidate
InvalidCandidate
MissingBlockers
MissingSpecialMap
InvalidSpecialMap
SourceIdentityMismatch
MissingFootprintCells
NullFootprintCell
DuplicateFootprintCell
InvalidFootprintCell
MissingEntrySockets
NullEntrySocket
DuplicateEntrySocketId
InvalidEntrySocket
MissingRequiredEntry
UnsupportedTransform
FootprintOutsideWorld
FootprintOverlap
BlocksExistingEntryApproach
EntryNotOnFootprint
DuplicateEntryFace
EntryOutsideWorld
EntryFacesOwnFootprint
EntryApproachOccupied
```

`FootprintPlacementError` immutable properties:

```text
FootprintPlacementErrorCode Code
string SourceDefinitionId
string EntrySocketId
int SectorIndex
string Message
```

- source/socket ID는 canonical-or-empty, sector index는 relevant exact `0..168` 또는 `-1`이다.
- message는 stable non-empty이며 path/stack/timestamp/thread/current-culture exception text를 포함하지 않는다.
- errors는 code ordinal, source ID ordinal, entry ID ordinal, sector index, message ordinal 순으로 canonical sort한다.
- 같은 logical error identity는 deterministic dedupe한다.
- 가능한 독립 Phase 1 오류와 같은 Phase 안의 cell/entry 오류는 누적한다. 앞 Phase 실패 뒤의 파생 검사는 수행하지 않는다.

`FootprintPlacementResult`:

```text
bool Succeeded
FootprintPlacement Placement
IReadOnlyList<FootprintPlacementError> Errors
```

- success: non-null Placement, errors `0`
- failure: null Placement, errors `>=1`
- partial footprint/entry placement publish 금지
- expected option rejection은 exception이 아니다.

## Starter Exact Definitions / Evaluation Matrix

typed Registry content가 아래 exact starter와 일치하는지 focused fixture에서 먼저 확인한다.

| Source | Kind | Size | Cells | Entry |
|---|---|---:|---|---|
| `SITE_MOON_BOSS_VAULT` | Boss | `2×1` | `(0,0) ENTRY`, `(1,0) ARENA` | `ENTRY_L @ (0,0) L` |
| `SITE_MOON_SEAL_FORGE` | Forge | `1×1` | `(0,0) CORE` | `ENTRY_L @ (0,0) L` |
| `SITE_CASSIA_SAP_HEART` | CoreResource | `1×1` | `(0,0) CORE` | `ENTRY_L @ (0,0) L` |
| `SITE_DEEP_STAR_YEAST` | CoreResource | `1×1` | `(0,0) CORE` | `ENTRY_L @ (0,0) L` |
| `SITE_MOON_CORE_METEOR` | CoreResource | `1×1` | `(0,0) CORE` | `ENTRY_L @ (0,0) L` |

모든 starter entry는 allowed `1|2|3`, required `true`, return-path-required `true`다. footprint cells의 biome/recipe/required-open-sides는 typed definition의 exact 값을 보존한다.

empty blockers, MAP03_02 exact raw candidates, caller가 transform을 `R0, MirrorX, MirrorY, R180` 순으로 평가할 때:

| Group | Evaluations | Success | `FootprintOutsideWorld` | `EntryOutsideWorld` |
|---|---:|---:|---:|---:|
| Start | 88 | 88 | 0 | 0 |
| Boss | 676 | 572 | 52 | 52 |
| Forge | 676 | 624 | 0 | 52 |
| Cassia Sap Heart | 676 | 624 | 0 | 52 |
| Deep Star Yeast | 676 | 624 | 0 | 52 |
| Moon Core Meteor | 676 | 624 | 0 | 52 |
| **Total** | **3468** | **3156** | **52** | **260** |

starter matrix에서 다른 rejection code는 exact `0`이다. Boss origin `(12,12)` four transforms는 Phase 2 `FootprintOutsideWorld`로만 reject한다. Boss의 valid footprint 중 left/right-facing entry boundary 52 options가 `EntryOutsideWorld`다.

## Determinism / Ownership

- footprint/entry input order, blocker/reservation input order, collection implementation이 달라도 exact result/order가 같다.
- seeds `0`, `4660`, `ulong.MaxValue`와 candidate ordinal은 그대로 보존되며 placement membership을 바꾸지 않는다.
- fresh/reused solver 100회, `en-US`/`tr-TR`에서 byte-observable scalar/list order가 동일하다.
- RNG stream/draw, `System.Random`, `UnityEngine.Random`, wall clock, frame, thread, filesystem, Unity object state에 의존하지 않는다.
- source candidate/definition/reservation/caller collection을 mutate하지 않는다.
- public setters/fields, mutable collection exposure, static mutable cache, lazy public enumeration이 없다.

## Scope Boundary / DO NOT

- candidate group/catalog 전체를 solver 내부에서 enumerate/filter하지 않는다.
- distance index/graph distance, Start/site distance, Forge/Boss distance, 4×4 clustering 금지
- altitude/edge/future-capacity/quadrant cost·penalty·weight 금지
- RNG draw/shuffle/weighted choice, best-option selection 금지
- reservation ID/order, `SectorReservation`, `SiteReservation`, snapshot 생성 금지
- backtracking/retry/max-200/PASS_SITE 금지
- Core capacity flood-fill와 `CoreBiomeSeed` 생성 금지
- Village bucket/layout/placement 금지
- `PASS_SITE`, pass adapter, Root registry integration 금지
- generated_special_sites/sector serializer, file I/O, replay bundle 확장 금지
- existing MAP03_01/02 models, grid/root/manifest/overlay 수정 금지
- Authoring/generated CSV/meta, asmdef/asmref, Scene/Prefab, Package/ProjectSettings 변경 금지
- test skip/ignore/assertion 완화, Git operation 금지
- MAP03_04 선행 작업 금지

## Collision Handling

1. 신규 destination이 없으면 생성한다.
2. exact 계약과 바이트 동일한 preexisting destination만 `PREEXISTING_IDENTICAL`로 재사용한다.
3. 다르면 overwrite/merge/delete하지 않고 `STATUS: BLOCKED`다.
4. 기존 `.meta` GUID와 사용자 변경을 보존한다.

## Required Tests

`FootprintPlacementSolverTests.cs` actual NUnit cases 최소 `96`개다.

minimum groups:

- transformer width/height/source-bound/undefined enum gate
- R0/MirrorX/MirrorY/R180 coordinate table on asymmetric `3×2` sparse cells
- four transform side mapping for L/R/U/D
- footprint cells, required-open-sides, entry local coordinate/side use one identical transform
- R90/R270 representation/API absent and no dimension swap
- placement entry/placement/blocker/result constructor null/range/duplicate/order/read-only invariants
- Start synthetic 1×1 R0 placement and all exact 88 candidates success with empty blockers
- exact starter definitions/roles/dimensions/cells/entry payload gate
- Boss and four 1×1 site groups across all 169 origins × four transforms
- exact evaluation matrix `3468/3156/312`, rejection breakdown `52/260`, all other codes `0`
- Boss raw `(12,12)` four transforms reject only as `FootprintOutsideWorld`
- boundary entries never clamp/wrap; valid transformed entry side/exterior exact
- occupied footprint overlap, protected existing entry approach blocking, candidate entry approach occupied
- candidate footprint may be adjacent to occupied sector absent exact collision
- same candidate-entry exterior / protected approach and shared candidate exterior allowed when faces differ
- candidate entry exterior inside own sparse/rectangular footprint rejection
- duplicate entry face, entry-not-on-footprint, missing required entry rejection
- null/mismatched/inactive/wrong-kind/duplicate/invalid definition inputs and no partial placement
- `FromReservations` occupied/protected unions, shuffle stability, duplicate/overlap/out-of-world rejection
- reversed/shuffled array/list definitions/blockers and caller mutation isolation
- seeds `0/4660/ulong.MaxValue`, fresh/reused 100-run identity
- `en-US`/`tr-TR` culture invariance
- public mutation-surface/dependency audit
- RNG/distance/cost/selection/backtracking/capacity/village/pass/file-I/O production dependency `0`

금지:

- `[Ignore]`, `[Explicit]`, inconclusive, assumption-based skip
- reflection으로 private state를 바꿔 success를 만드는 test
- test order/current filesystem/wall clock 의존
- failure matrix를 hard-coded return 값으로 통과시키는 fake solver

## Regression / Verification

```text
New FootprintPlacementSolverTests: >=96 PASS
MAP03_02 SiteCandidateEnumerationTests: 268/268 PASS
MAP03_01 SiteReservationModelsTests: 81/81 PASS
MAP02 phase focused aggregate: 667/667 PASS
SpecialVillageDefinitionBuilderTests: 57/57 PASS
BiomeBoundaryDefinitionBuilderTests: 38/38 PASS
StaticDataRegistryBuilderTests: 53/53 PASS
ContentVersionHashCalculatorTests: 54/54 PASS
Targeted Game.Map.Tests.EditMode: >=1959/1959 PASS
Full project EditMode: >=1999/1999 PASS
Failed: 0
Skipped: 0
```

Unity gate:

- Unity `6000.3.8f1`, MCP instance `Constant`
- refresh/compile clean, relevant new warnings `0`
- PlayMode `NOT RUN`, Visual `NOT APPLICABLE`

## Asset / Meta Gate

before expected:

```text
Assets meta files = 3005
duplicate GUID groups = 0
Authoring CSV = 50
Authoring CSV meta = 50
```

after expected:

```text
new C# = 7
new matching .cs.meta = 7
Assets meta files = 3012
duplicate GUID groups = 0
Authoring CSV = 50 unchanged
Authoring CSV meta = 50 unchanged
```

GUID는 32 lowercase hex, non-zero, project-wide unique다. `.meta`는 `fileFormatVersion: 2`와 `MonoImporter`를 사용한다.

## Exact Change Budget

```text
Created Assets:  14
Modified Assets: 0
Deleted Assets:  0
Created report:  1
```

exact 14 Assets destinations 외 변경이 있으면 `BLOCKED`다.

## Result Contract

`REPORTS/MAP03_03_IMPLEMENT_FOOTPRINT_PLACEMENT_SOLVER_RESULT.md` 필수:

- `STATUS: PASS|BLOCKED|FAIL`
- implementation summary / exact changed-created paths
- transform table 및 side mapping 검증
- exact starter evaluation matrix와 failure-code breakdown
- collision/blocker/source validation evidence
- determinism/immutability/ownership evidence
- focused/regression/targeted/full test counts + job IDs
- Unity refresh/compile/warnings
- before/after meta, duplicate GUID, Authoring count/hash
- scope audit / existing modification count / PREEXISTING_IDENTICAL
- task checklist, recommended commit

PASS 전제:

- exact change set + all compile/test/meta/count/hash/scope gates PASS
- existing Asset modification `0`
- `.APPLIED`는 정확한 `PATCH_ID`, `PATCH_VERSION`, `TASK_KEY`, `TASK_PATH`, `MANIFEST_SHA256`, `TASK_SHA256`를 기록
- `07_PATCH_APPLY_RULES.md` current-task binding에 따라 `.APPLIED`가 존재하고 exact manifest/task SHA와 일치해야 `06_IMPLEMENTATION_STATUS.md`와 `MASTER_IMPLEMENTATION_TASK_LIST.md`를 finalize
- MAP03_03만 `COMPLETE`, MAP03_04만 `CURRENT`, 다른 future task는 `LOCKED`

BLOCKED 전제:

- collision/baseline/precondition/change-budget 문제가 있으면 Assets/status/master를 수정하지 않는다.
- Result에 exact blocker와 필요한 repair patch 범위를 기록한다.

Recommended Commit: `feat(map): solve transformed site footprint placements`
