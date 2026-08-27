# MAP03_08 — Implement Village Reservation

```yaml
status_control:
  task_key: MAP03_08_IMPLEMENT_VILLAGE_RESERVATION
  result_file: REPORTS/MAP03_08_IMPLEMENT_VILLAGE_RESERVATION_RESULT.md
```

## TASK TYPE

```text
RUNTIME DETERMINISTIC VILLAGE DISTANCE-BUCKET + LAYOUT-WEIGHT + FOOTPRINT/ENTRY RESERVATION + EDITMODE TESTS
```

## Objective

MAP03_07의 immutable `CoreCapacityApproval`을 입력으로 받아 primary Village 하나를 예약한다.

```text
VIL_MOON_PRIMARY + SITE_PRIMARY_VILLAGE + one allowed VillageLayoutDefinition
```

`RNG_WORLD_SITE`에서 Start 거리 bucket을 exact `20/50/30`으로 먼저 고르고, 선택 bucket에 viable candidate가 있는 active allowed layout만 definition weight로 고른 뒤 해당 layout의 canonical candidate pool에서 한 candidate를 unbiased하게 고른다.

Village occupied footprint는 기존 six-site footprint, 기존 entry approach, MAP03_07 four Core capacity witness를 침범하지 않아야 한다. Village entry exterior는 world 안이며 기존 occupied footprint를 향하지 않아야 한다. 선택 bucket이 고갈되면 다른 bucket으로 fallback/redraw하거나 한 site를 옮기지 않고 `ReservationRejected / RetryRequired`를 반환한다.

이 Task의 성공 산출물은 **원본 CoreCapacityApproval + one immutable Village selection을 묶은 `VillageReservationApproval`**이다. reservation ID, `CoreBiomeSeed`, final `SiteReservation`/`SectorReservation[169]`/snapshot publication은 MAP03_09 이후 범위다. Village 내부 4x4 MicroChunk layout cell, facility/shop/merchant 배치는 MAP10 범위다.

## 전체 연결

```text
MAP03_06 provisional six-site selection plan
  -> MAP03_07 four disjoint Core capacity witnesses
  -> MAP03_08 this Task: Village bucket/layout/sector footprint/entry approval
  -> MAP03_09 final site reservation validation/publication boundary
  -> MAP03_10 overlay
  -> MAP03_11 100,000-seed distribution/exit tests
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
12. `REPORTS/MAP03_07_IMPLEMENT_CORE_CAPACITY_FLOOD_CHECK_RESULT.md`

MAP03_07 Result의 exact `STATUS: PASS`, focused `215/215`, regressions `248/248 / 270/270 / 239/239 / 170/170 / 268/268 / 81/81 / 667/667`, targeted `3005/3005`, full `3045/3045`, starter selected/witness/total/overlap/RNG `6 / 4 / 20 / 0 / 3156->3156`, final Assets meta `3045`, existing Assets modification `0`을 확인한다.

## Map Package Reference

exact installed Map Package v1.0이 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/01_WORLD_FIXED_SPEC.md
01_FIXED_SPEC/02_COORDINATE_AND_ID_RULES.md
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
01_FIXED_SPEC/05_IMPLEMENTATION_ORDER.md
02_PHASE_ROADMAP/MAP03_SPECIAL_SITE_RESERVATION.md
02_PHASE_ROADMAP/MAP10_SPECIAL_MAP_AND_VILLAGE_ASSEMBLY.md
04_CSV_STARTER/special_map_catalog.csv
04_CSV_STARTER/special_map_entry_sockets.csv
04_CSV_STARTER/village_profiles.csv
04_CSV_STARTER/village_layout_catalog.csv
```

reference가 없으면 이 Task의 frozen bucket/layout/footprint/entry contracts와 existing immutable typed definitions를 authoritative fallback으로 사용한다. Authoring CSV를 직접 읽거나 재파싱하지 않는다. `village_layout_cells.csv` body는 MAP10 소유이므로 이 Task에서 읽지 않는다.

## READ ALLOWLIST

### Existing typed definitions / RNG

```text
Assets/_Game/Map/Runtime/WorldGeneration/Data/WorldGenerationDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/WorldRouteDefinitionSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/SpecialMapDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/SpecialVillageDefinitionSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/StaticDataRegistry.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/RngStreamScope.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/DeterministicRngStream.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/DeterministicRngStreamFactory.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationRngStreams.cs
```

### Existing grid / reservation / capacity models

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGridIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteOriginCandidate.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteFootprint.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacementEntry.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacement.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SitePlacementKey.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSelectionPlan.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityRequirement.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityFloodWitness.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityApproval.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityFloodDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityFloodRejection.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityFloodError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityFloodResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityFloodChecker.cs
```

### Focused tests / assemblies

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/BiomeBoundaryDefinitionBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/SpecialVillageDefinitionBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/StaticDataRegistryBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/ContentVersionHashCalculatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/DeterministicRngStreamTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/GridInitializationPassTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationModelsTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteCandidateEnumerationTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/FootprintPlacementSolverTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteDistanceIndexTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteCandidateCostTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationBacktrackerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/CoreCapacityFloodCheckerTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

위 C#/asmdef와 matching meta, approved `Generation` Runtime/Test 직계 파일명의 path-only inventory, Authoring CSV/meta count/hash, 전체 meta GUID, task marker 이후 change-scope path만 확인할 수 있다.

금지:

- Authoring CSV body 직접 재파싱·수정
- `village_layout_cells.csv`와 facility/shop body
- MAP03_09 이후 Task body
- unrelated production/test C# body
- Legacy/Stage/P6/P11 generator body
- Scene/Prefab YAML

## WRITE ALLOWLIST

### 신규 Runtime production C# — exact 8

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageDistanceBucket.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageReservationCandidate.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageReservationDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageReservationRejection.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageReservationError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageReservationSelection.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageReservationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageReservationSelector.cs
```

### 신규 Runtime EditMode test — exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/VillageReservationSelectorTests.cs
```

신규 C# 9개와 matching `.cs.meta` 9개, Result 1만 생성한다. 기존 production/tests/meta/asmdef/asmref는 수정하지 않는다. 기존 approved directory를 재사용하며 새 directory/folder meta를 만들지 않는다.

## Namespace / Assembly

```text
Runtime namespace: StarNight.Map.WorldGeneration.Generation
Test namespace:    StarNight.Map.Tests.WorldGeneration.Generation
Runtime assembly:  Game.Map.Runtime
Test assembly:     Game.Map.Tests.EditMode
```

`UnityEditor`, Unity object/lifecycle, record/record struct, `required`, `init`, nullable-reference directive, reflection factory, singleton/static mutable state를 도입하지 않는다.

## Public Selector API

```text
public sealed class VillageReservationSelector

VillageReservationResult Reserve(
    CoreCapacityApproval coreCapacityApproval,
    VillageProfileDefinition villageProfile,
    SpecialMapDefinition villageSpecialMap,
    IEnumerable<SpecialMapEntrySocketDefinition> villageEntrySockets,
    IEnumerable<VillageLayoutDefinition> villageLayouts,
    DeterministicRngStream siteRng)
```

호출자는 immutable Registry root에서 exact definitions를 전달하고 MAP03_06 backtracker에 사용한 **같은 continued `RNG_WORLD_SITE` instance**를 전달한다. selector는 Registry singleton, factory, filesystem, CSV, Root/pass state에서 자체 조회하지 않는다.

starter integration에서 MAP03_06 직후 `siteRng.DrawCount == 3156`이고 MAP03_07은 RNG를 소비하지 않는다. generic public API는 특정 draw count를 hard-code하지 않지만 diagnostics에 actual before/after를 보존한다.

## Structural Preflight

RNG draw 전에 가능한 오류를 모두 accumulated/sorted validation한다.

- `CoreCapacityApproval` non-null, exact six-site selection plan, exact four canonical witnesses, total witness `20` starter identity
- Village profile exact one active `VIL_MOON_PRIMARY`, world `WORLD_MOONPALACE_V1`, `MaximumSectorCount == 2`
- special map exact active `SITE_PRIMARY_VILLAGE`, role `VILLAGE`, required count `1`, generation mode `VILLAGE_LAYOUT`
- special map primary biome empty, min Start distance `0`, min other-core-site distance `2`, allowed route types unique `1|2|3`
- entry template exact one `ENTRY_L`, local `(0,0)`, source side `L`, required/return true, allowed routes `1|2|3`
- profile allowed layout IDs non-empty/unique and every allowed layout exact once supplied
- unexpected supplied layout, null/duplicate/missing layout, inactive layout, parent/profile identity mismatch 거부
- layout weight positive, target facility count within profile `5..6`, dimension positive
- supported sector footprint는 exact `1x1 | 2x1 | 1x2`, area `<= MaximumSectorCount`; `2x2`, `1x3`, `3x1` 및 larger 거부
- layout entry-side tokens non-empty/unique/defined; starter exact `L|R`
- caller collections 즉시 snapshot; undefined enum/numeric cast, invalid world index, overlapping six-site footprints, invalid witness ownership/overlap 거부
- structural invalid input은 RNG delta `0`, candidates/diagnostics/approval 없이 `InvalidInput`, retry false를 반환한다.

starter exact definitions:

| Identity | Width×Height | Target facilities | Entry sides | Weight |
|---|---:|---:|---|---:|
| `VLAY_STANDARD_5_A` | `1×1` | 5 | `L|R` | 100 |
| `VLAY_STANDARD_6_A` | `1×1` | 6 | `L|R` | 70 |

## `VillageDistanceBucket` Contract

`VillageDistanceBucket.cs`는 immutable bucket과 strict parser를 함께 제공한다.

```text
VillageDistanceBucket
int BucketOrdinal
int MinDistanceInclusive
int MaxDistanceInclusive
int Weight
int RollMinInclusive
int RollMaxInclusive
bool Contains(int distance)

VillageDistanceBucketCatalog
IReadOnlyList<VillageDistanceBucket> Buckets
int TotalWeight
VillageDistanceBucket SelectByRoll(int roll)
static bool TryParse(string value,
    out VillageDistanceBucketCatalog catalog,
    out string error)
```

exact authoritative starter string:

```text
2-3:20|4-6:50|7-10:30
```

exact roll mapping:

| Roll | Distance | Weight |
|---:|---|---:|
| `0..19` | `2..3` | 20 |
| `20..69` | `4..6` | 50 |
| `70..99` | `7..10` | 30 |

- ASCII decimal, exact `min-max:weight` and `|`; whitespace/sign/empty/leading-zero/overflow/trailing delimiter 거부
- exact three ranges, strictly ascending, non-overlap, positive weight, total exact `100`
- gap `3->4`, `6->7`처럼 integer-contiguous이며 exact starter ranges/weights가 아니면 이 world profile에서 structural error다.
- parser는 source definition/string을 mutate/cache하지 않고 ordinal/culture-invariant다.

## Exact RNG Schedule

preflight success 뒤 아래 순서만 사용한다.

```text
1. bucketRoll    = siteRng.NextInt(100)
2. canonical candidate enumeration/filter for selected bucket (RNG 0)
3. layoutRoll    = siteRng.NextInt(sum of viable layout SelectionWeight)
4. candidateRoll = siteRng.NextInt(selected layout viable candidate count)
```

- `NextInt`의 existing rejection-sampling 구현을 그대로 사용한다. `%`, float/double scale, `System.Random`, `UnityEngine.Random` 금지.
- selected bucket 전체에 viable candidate가 `0`이면 step 1 뒤 즉시 `ReservationRejected`; layout/candidate draw나 bucket fallback/redraw가 없다.
- viable candidate가 있는 layout만 layout weight table에 들어간다. table은 layout ID ordinal이며 원래 positive `SelectionWeight`를 그대로 사용한다.
- selected layout candidate는 canonical list에서 uniform selection한다.
- completed call은 NextInt method call exact `3`; diagnostics의 DrawCount delta는 rejection sampling이 실제 소비한 raw `NextUInt64` count다.
- same continued stream state/input은 exact same bucket/layout/candidate/after state다.

## Exact Rectangular Candidate Enumeration

layout `W×H`마다 footprint가 world-bound인 origin을 row-major index order로 열거한다.

```text
y = 0 .. 13-H
x = 0 .. 13-W
originIndex = y*13+x
```

각 origin에서 layout `EntrySides`를 exact `L,R,U,D` canonical order로 열거한다. Village는 rectangular sector footprint 전체를 점유한다.

```text
Occupied = every (origin.X + localX, origin.Y + localY)
           for localY 0..H-1, localX 0..W-1
```

entry boundary anchor는 아래 lower-median 규칙이다.

| Side | local entry footprint sector | exterior delta |
|---|---|---|
| L | `(0, (H-1)/2)` | `(-1,0)` |
| R | `(W-1, (H-1)/2)` | `(+1,0)` |
| D | `((W-1)/2, 0)` | `(0,-1)` |
| U | `((W-1)/2, H-1)` | `(0,+1)` |

entry exterior가 world 밖이면 candidate source에서 제외하고 `EntryOutsideWorld` count를 기록한다. candidate canonical order/key:

```text
layout ID ordinal
-> originIndex ascending
-> side L,R,U,D
```

`CandidateOrdinal`은 world-bound entry candidate의 global canonical 위치다. caller layout/list order와 무관하다. starter raw evaluation/source counts:

```text
layouts = 2
raw entry evaluations = 2 * 169 * 2 = 676
entry outside world = 52
world-bound source candidates = 624
```

Village layout sector rectangle만 예약하며 internal `VillageLayoutCellDefinition`, 4x4 MicroChunk, facility slots를 만들거나 검증하지 않는다.

## Distance / Collision Filter Contract

candidate의 Start distance는 footprint-aware cardinal metric이다.

```text
StartDistance = min ManhattanDistance(villageOccupied, startOccupied)
```

tile/microchunk/route-graph distance, origin-only distance, Chebyshev/diagonal, wrap/clamp를 사용하지 않는다. selected bucket의 inclusive range 안에 있어야 한다.

existing sets:

```text
ExistingOccupied = union of six selected placement occupied sectors
ExistingEntryApproaches = union of six selected placement entry exterior sectors
ProtectedCoreWitness = union of exact four MAP03_07 witness sectors
```

candidate first-failure precedence:

1. Village occupied ∩ ExistingOccupied != empty -> `FootprintOverlap`
2. Village occupied ∩ ProtectedCoreWitness != empty -> `ProtectedCoreWitness`
3. Village occupied ∩ ExistingEntryApproaches != empty -> `BlocksExistingEntryApproach`
4. Village entry exterior ∈ ExistingOccupied -> `EntryApproachOccupied`
5. Village↔each non-Start existing placement footprint distance `< villageSpecialMap.MinGraphDistanceToOtherCoreSites` -> `OtherSiteDistanceTooSmall`
6. StartDistance outside selected bucket -> `StartDistanceOutsideSelectedBucket`
7. otherwise viable

- Start는 bucket rule로만 검사하고 step 5의 other-site set에서 제외한다.
- existing protected entry approach와 Village entry exterior가 같은 일반 sector를 향하는 것은 existing MAP03_03 contract처럼 허용한다.
- Village entry exterior가 ProtectedCoreWitness에 속하는 것도 occupied collision이 아니므로 이 Task에서 허용한다.
- one-cell halo, diagonal clearance, route mask/socket face compatibility, altitude/cost/quadrant penalty를 새로 만들지 않는다.
- candidate 하나는 first-failure reason 하나만 가져 layout diagnostic 합이 source count와 exact 같아야 한다.

## `VillageReservationCandidate` Contract

immutable properties:

```text
string VillageProfileId
string SpecialMapId
string LayoutId
int LayoutWeight
SectorCoord Origin
int OriginIndex
int CandidateOrdinal
int FootprintWidthSectors
int FootprintHeightSectors
IReadOnlyList<int> OccupiedSectorIndices
SiteEntrySide EntrySide
int EntryFootprintSectorIndex
int EntryExteriorSectorIndex
int StartDistance
int BucketOrdinal
```

- occupied indices는 count `W*H`, unique, WorldGridIndex ascending copied read-only다.
- entry footprint index는 occupied에 속하고 exterior는 side delta exact once이며 world-bound/not own occupied다.
- IDs/weight/dimensions/entry side는 exact selected definitions/canonical candidate와 일치한다.
- source definitions, plan, witness, caller lists를 mutate하지 않는다.

## Selection / Approval Contract

`VillageReservationSelection` immutable properties:

```text
VillageProfileDefinition Profile
SpecialMapDefinition SpecialMap
SpecialMapEntrySocketDefinition EntryTemplate
VillageLayoutDefinition Layout
VillageDistanceBucket DistanceBucket
VillageReservationCandidate Candidate
int BucketRoll
int LayoutRoll
int CandidateRoll
```

`VillageReservationApproval`은 같은 `VillageReservationSelection.cs`에 두며 immutable properties/API를 제공한다.

```text
CoreCapacityApproval CoreCapacityApproval
VillageReservationSelection Village
int ExistingSiteCount
int CapacityWitnessCount
int TotalSelectedSiteCount
bool OccupiesSector(int sectorIndex)
```

- existing site `6`, Village `1`, total selected site `7`; capacity witness `4`를 보존한다.
- original approval/plan/witness reference identity를 교체·clone·mutate하지 않는다.
- starter Village occupied와 ExistingOccupied/ProtectedCoreWitness overlap exact `0`이다.
- reservation ID/order, `SiteReservation`, `SectorReservation`, final snapshot, `CoreBiomeSeed`를 포함하지 않는다.

## Diagnostics Contract

`VillageLayoutCandidateDiagnostics` immutable fields:

```text
string LayoutId
int SelectionWeight
int RawEntryEvaluationCount
int EntryOutsideWorldCount
int SourceCandidateCount
int FootprintOverlapCount
int ProtectedCoreWitnessCount
int BlocksExistingEntryApproachCount
int EntryApproachOccupiedCount
int OtherSiteDistanceTooSmallCount
int StartDistanceOutsideSelectedBucketCount
int ViableCandidateCount
```

`VillageReservationDiagnostics` immutable fields:

```text
VillageDistanceBucket SelectedBucket
int BucketRoll
IReadOnlyList<VillageLayoutCandidateDiagnostics> Layouts
int RawEntryEvaluationCount
int SourceCandidateCount
int ViableLayoutCount
int ViableCandidateCount
int RngMethodCallCount
ulong RngDrawCountBefore
ulong RngDrawCountAfter
int LayoutRoll
int CandidateRoll
```

- layout diagnostics는 allowed layout ID ordinal이다.
- 각 layout은 `EntryOutside + Source == Raw`, source filter reason counts + viable == Source를 exact 만족한다.
- bucket exhaustion에서는 method calls `1`, layout/candidate roll `-1`, after는 actual bucket draw 뒤 값이다.
- Completed에서는 method calls `3`, rolls all non-negative, after-before는 actual SplitMix64 draw delta다.
- diagnostics는 MAP03_10 overlay/MAP03_11 stats용 facts이며 selection을 바꾸지 않는다.

## Error / Rejection / Result Contract

`VillageReservationErrorCode` exact frozen order:

```text
MissingCoreCapacityApproval
InvalidCoreCapacityApproval
MissingVillageProfile
InvalidVillageProfile
MissingVillageSpecialMap
InvalidVillageSpecialMap
MissingEntrySockets
NullEntrySocket
UnexpectedEntrySocket
InvalidEntrySocket
MissingLayouts
NullLayout
DuplicateLayoutId
MissingAllowedLayout
UnexpectedLayout
InvalidLayout
InvalidDistanceBuckets
MissingSiteRng
InvalidSelectedPlacement
InvalidCapacityWitness
DefinitionIdentityMismatch
InternalInvariantViolation
```

error fields:

```text
VillageReservationErrorCode Code
string DefinitionId
int SectorIndex
string Message
```

code, definition ID, sector index, message ordinal로 sort/dedupe한다. stable non-empty message만 사용하고 path/stack/time/thread/culture exception text를 넣지 않는다.

`VillageCandidateRejectionReason` exact order:

```text
EntryOutsideWorld
FootprintOverlap
ProtectedCoreWitness
BlocksExistingEntryApproach
EntryApproachOccupied
OtherSiteDistanceTooSmall
StartDistanceOutsideSelectedBucket
```

`VillageReservationRejectionReason` exact value:

```text
SelectedBucketHasNoViableCandidate
```

`VillageReservationRejection` fields:

```text
VillageReservationRejectionReason Reason
int BucketOrdinal
int MinDistanceInclusive
int MaxDistanceInclusive
int SourceCandidateCount
int ViableCandidateCount
string Message
```

`VillageReservationStatus` exact values:

```text
Completed
ReservationRejected
InvalidInput
```

`VillageReservationResult` immutable properties:

```text
VillageReservationStatus Status
bool Succeeded
bool RetryRequired
VillageReservationApproval Approval
VillageReservationDiagnostics Diagnostics
IReadOnlyList<VillageReservationRejection> Rejections
IReadOnlyList<VillageReservationError> Errors
```

- Completed: approval/diagnostics non-null, rejections/errors `0`, retry false.
- ReservationRejected: approval null, diagnostics non-null, exact rejection `1`, errors `0`, retry true.
- InvalidInput: approval/diagnostics null, rejections `0`, errors `>=1`, retry false, RNG delta `0`.
- rejected/invalid result에 partial selection/approval을 publish하지 않는다.

## Exact Starter Integration Gates

MAP03_06 fixture와 MAP03_07 checker로 approval을 만든 뒤 같은 site stream을 이어 사용한다.

```text
existing selected placements = 6
capacity witnesses = 4
capacity witness sectors = 20
RNG DrawCount before Village = 3156
profile/layouts/special map/entry = 1 / 2 / 1 / 1
raw/source Village candidates = 676 / 624
selected bucket/layout/candidate = 1 / 1 / 1
selected Village occupied sectors = 1 (starter layouts are 1x1)
final selected sites = 7
occupied overlap = 0
protected witness overlap = 0
entry conflict = 0
completed NextInt method calls = 3
```

fresh full starter seeds `0`, `4660`, `ulong.MaxValue`는 각각 `Completed`여야 한다. selected bucket의 exact range, definition identity, collision/distance/witness gates, same-input rerun snapshot identity를 검증한다. 특정 selected coordinate/layout/bucket answer를 production lookup table로 hard-code하지 않는다.

100,000-seed 20/50/30 statistical acceptance gate는 MAP03_11 소유다. 이 Task는 roll boundary exhaustive `0..99`, unbiased existing NextInt usage, starter/determinism만 검증한다.

## Determinism / Ownership

- profile allowed layout order, supplied array/list order, entry collection order와 무관하다.
- origin/index/side/candidate/layout output은 canonical order다.
- bucket/layout/candidate 이외 RNG draw가 없고 다른 RNG stream을 만들거나 소비하지 않는다.
- same fresh world seed + same MAP03_06 continuation은 exact same result/RNG after state다.
- `en-US`/`tr-TR`, wall clock, frame, thread, filesystem에 무관하다.
- approval/plan/witness/definitions/caller collections를 mutate하지 않는다.
- public setters/fields, mutable collection exposure, static mutable cache, lazy public enumeration이 없다.

## Scope Boundary / DO NOT

- MAP03_06 option ranking/backtracking을 복제·수정·재실행하지 않는다.
- MAP03_07 flood/witness를 다시 계산·축소·재할당하지 않는다.
- selected bucket fallback/redraw, selected-site local move, alternate plan repair 금지
- Village internal 4x4 layout cells, facility slots, fixed/optional facility/shop/shopkeeper/merchant 선택 금지
- reservation ID/order, `CoreBiomeSeed`, final `SiteReservation`/`SectorReservation[169]`/snapshot 생성 금지
- final required-count/distance/entry validator와 generated_special_sites serializer 금지
- full `PASS_SITE` adapter/root retry/attempt increment 금지
- biome painting/growth, route graph, microchunk assembly, tile movement 금지
- existing MAP03_01~07 models/tests 수정 금지
- Authoring/generated CSV/meta, asmdef/asmref, Scene/Prefab, Package/ProjectSettings 변경 금지
- test skip/ignore/assertion 완화, Git operation 금지
- MAP03_09 선행 작업 금지

## Collision Handling

1. 신규 destination이 없으면 생성한다.
2. exact 계약과 바이트 동일한 preexisting destination만 `PREEXISTING_IDENTICAL`로 재사용한다.
3. 다르면 overwrite/merge/delete하지 않고 `STATUS: BLOCKED`다.
4. 기존 `.meta` GUID와 사용자 변경을 보존한다.

## Required Tests

`VillageReservationSelectorTests.cs` actual NUnit cases 최소 `220`개다.

minimum groups:

- profile/special-map/entry/layout null/missing/duplicate/unexpected/active/identity/range aggregation
- exact `VIL_MOON_PRIMARY`, `SITE_PRIMARY_VILLAGE`, `ENTRY_L`, allowed layouts and `5..6` target identity
- strict bucket grammar invalid whitespace/sign/zero/overflow/order/overlap/gap/total/nonstarter vectors
- exhaustive rolls `0..99`, exact `20/50/30` counts and inclusive distance boundaries
- supported `1x1/2x1/1x2`, max area 2, rejected `2x2/1x3/3x1`
- row-major origins, canonical layout/side order, lower-median entry vectors
- starter exact raw/source/entry-out counts `676/624/52`
- world-bound occupied/entry, sorted unique rectangle cells, side delta/no-wrap
- footprint overlap, protected witness, existing entry blocked, entry occupied first-failure precedence
- shared existing/Village entry exterior allowed and witness-only entry exterior allowed
- footprint-aware Start Manhattan distance for 1x1/2x1/1x2, origin-only/diagonal exclusion
- other five non-Start placement minimum distance `2`, Start excluded from that gate
- chosen-bucket filter and exact no-fallback/no-redraw rejection with retry true
- viable-only layout table, weights `100/70`, ordinal cumulative roll boundary
- selected-layout candidate uniform NextInt index and canonical snapshot
- preflight/bucket-rejected/completed RNG method-call and actual DrawCount evidence
- Completed/ReservationRejected/InvalidInput publication/retry invariants
- diagnostics per-layout conservation equations and canonical read-only snapshots
- error/rejection exact order/dedupe/stable messages
- full starter seeds `0/4660/ulong.MaxValue`: `6+1`, witnesses `4/20`, overlap `0`, entry conflict `0`
- same continuation fresh/reused selector 100-run identity
- reversed/shuffled definitions and array/list stability
- `en-US`/`tr-TR` culture and caller mutation isolation
- public mutation-surface/dependency audit
- internal layout/facility/final reservation/biome/pass/root/file-I/O production dependency `0`

금지:

- `[Ignore]`, `[Explicit]`, inconclusive, assumption-based skip
- reflection으로 private state를 바꿔 success를 만드는 test
- test order/current filesystem/wall clock 의존
- fixed seed selected coordinate/layout answer lookup

## Regression / Verification

```text
New VillageReservationSelectorTests: >=220 PASS
MAP03_07 CoreCapacityFloodCheckerTests: 215/215 PASS
MAP03_06 SiteReservationBacktrackerTests: 248/248 PASS
MAP03_05 SiteCandidateCostTests: 270/270 PASS
MAP03_04 SiteDistanceIndexTests: 239/239 PASS
MAP03_03 FootprintPlacementSolverTests: 170/170 PASS
MAP03_02 SiteCandidateEnumerationTests: 268/268 PASS
MAP03_01 SiteReservationModelsTests: 81/81 PASS
MAP02 phase focused aggregate: 667/667 PASS
SpecialVillageDefinitionBuilderTests: 57/57 PASS
BiomeBoundaryDefinitionBuilderTests: 38/38 PASS
StaticDataRegistryBuilderTests: 53/53 PASS
ContentVersionHashCalculatorTests: 54/54 PASS
Targeted Game.Map.Tests.EditMode: >=3225/3225 PASS
Full project EditMode: >=3265/3265 PASS
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
Assets meta files = 3045
duplicate GUID groups = 0
Authoring CSV = 50
Authoring CSV meta = 50
```

after expected:

```text
new C# = 9
new matching .cs.meta = 9
Assets meta files = 3054
duplicate GUID groups = 0
Authoring CSV = 50 unchanged
Authoring CSV meta = 50 unchanged
```

GUID는 32 lowercase hex, non-zero, project-wide unique다. `.meta`는 `fileFormatVersion: 2`와 `MonoImporter`를 사용한다.

## Exact Change Budget

```text
Created Assets:  18
Modified Assets: 0
Deleted Assets:  0
Created report:  1
```

exact 18 Assets destinations 외 변경이 있으면 `BLOCKED`다.

## Result Contract

`REPORTS/MAP03_08_IMPLEMENT_VILLAGE_RESERVATION_RESULT.md` 필수:

- `STATUS: PASS|BLOCKED|FAIL`
- implementation summary / exact changed-created paths
- profile/special-map/entry/layout/bucket strict identity evidence
- exact RNG before/after/method-call/roll schedule
- raw/source/entry-out candidate counts and canonical order evidence
- footprint/distance/collision/Core-witness filter evidence
- layout weight/viable pool/selected candidate evidence
- starter three-seed `6+1 / 4 witnesses / 20 sectors / overlap 0` evidence
- status/approval/diagnostics/rejection/error/determinism/immutability evidence
- focused/regression/targeted/full test counts + job IDs
- Unity refresh/compile/warnings
- before/after meta, duplicate GUID, Authoring count/hash
- scope audit / existing modification count / PREEXISTING_IDENTICAL
- task checklist, recommended commit

PASS 전제:

- exact change set + all compile/test/meta/count/hash/scope gates PASS
- existing Asset modification `0`
- `.APPLIED` exact patch/manifest/task binding
- current-task binding이 일치해야 status/master finalize
- MAP03_08만 `COMPLETE`, `MAP03_09_IMPLEMENT_SITE_RESERVATION_VALIDATOR`는 계속 `LOCKED`, 다른 future task도 `LOCKED`

BLOCKED 전제:

- collision/baseline/precondition/change-budget 문제가 있으면 Assets/status/master를 수정하지 않는다.
- Result에 exact blocker와 필요한 repair patch 범위를 기록한다.

Recommended Commit: `feat(map): reserve deterministic primary village`
