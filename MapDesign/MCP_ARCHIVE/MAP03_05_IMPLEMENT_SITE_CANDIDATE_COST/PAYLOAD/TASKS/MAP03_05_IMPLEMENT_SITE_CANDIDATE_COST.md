# MAP03_05 — Implement Site Candidate Cost

```yaml
status_control:
  task_key: MAP03_05_IMPLEMENT_SITE_CANDIDATE_COST
  result_file: REPORTS/MAP03_05_IMPLEMENT_SITE_CANDIDATE_COST_RESULT.md
```

## TASK TYPE

```text
RUNTIME DETERMINISTIC SINGLE-OPTION SITE COST BREAKDOWN + PARTIAL HARD-CONSTRAINT SIGNALS + EDITMODE TESTS
```

## Objective

MAP03_03의 성공 `FootprintPlacement` option 하나를 MAP03_04의 distance policy와 현재 partial placements에 대입해 deterministic integer cost breakdown을 만든다.

```text
TotalCost = AltitudePenalty
          + EdgePenalty
          + DistanceConstraintPenalty
          + FutureCoreCapacityPenalty
          + QuadrantClusteringPenalty
```

이번 Task는 **option 하나를 평가할 뿐** 후보 전체를 열거·정렬·추첨·선택하지 않는다. MAP03_06이 이 결과를 사용해 후보 조합을 선택하고 backtrack한다. MAP03_07의 실제 Core capacity flood가 아직 없으므로 capacity는 caller가 전달한 optional forecast count만 점수화하며 hard approval로 간주하지 않는다.

## 지금까지의 연결

```text
MAP03_02 raw origins
    -> MAP03_03 transform/world-bound/collision 성공 placements
    -> MAP03_04 footprint-aware distance index/policy
    -> MAP03_05 이 Task: single placement option의 비용과 hard signal
    -> MAP03_06 future: option ordering/selection/backtracking
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
12. `REPORTS/MAP03_04_IMPLEMENT_SITE_DISTANCE_INDEX_RESULT.md`

MAP03_04 Result의 exact `STATUS: PASS`, focused `239/239`, exhaustive `28561`, exact policy `6 keys / 15 records / 15 constraints`, regressions `170/170 / 268/268 / 81/81 / 667/667`, targeted `2272/2272`, full `2312/2312`, final Assets meta `3020`, existing Assets modification `0`을 확인한다.

## Map Package Reference

exact installed Map Package v1.0이 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/01_WORLD_FIXED_SPEC.md
01_FIXED_SPEC/02_COORDINATE_AND_ID_RULES.md
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
01_FIXED_SPEC/05_IMPLEMENTATION_ORDER.md
02_PHASE_ROADMAP/MAP03_SPECIAL_SITE_RESERVATION.md
04_CSV_STARTER/biome_types.csv
04_CSV_STARTER/biome_patch_rules.csv
04_CSV_STARTER/special_map_catalog.csv
```

reference가 없으면 이 Task의 frozen formulas와 immutable typed definitions를 authoritative fallback으로 사용한다. Authoring CSV를 직접 읽거나 재파싱하지 않는다.

## READ ALLOWLIST

### Existing typed definitions

```text
Assets/_Game/Map/Runtime/WorldGeneration/Data/WorldRouteDefinitionSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/BiomeBoundaryDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/SpecialMapDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/SpecialVillageDefinitionSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/StaticDataRegistry.cs
```

### Existing generation models

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGridIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteOriginCandidate.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteFootprint.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacement.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SitePlacementKey.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistancePolicy.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceIndexBuilder.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceIndexResult.cs
```

### Focused tests / assemblies

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/BiomeBoundaryDefinitionBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/SpecialVillageDefinitionBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationModelsTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteCandidateEnumerationTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/FootprintPlacementSolverTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteDistanceIndexTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

위 C#/asmdef와 matching meta, approved `Generation` Runtime/Test 직계 파일명의 path-only inventory, Authoring CSV/meta count·hash, 전체 meta GUID, task marker 이후 change-scope path만 확인할 수 있다.

금지:

- Authoring CSV body 직접 재파싱·수정
- MAP03_06 이후 Task body
- unrelated production/test C# body
- Legacy/Stage/P6/P11 generator body
- Scene/Prefab YAML

## WRITE ALLOWLIST

### 신규 Runtime production C# — exact 6

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCostWeights.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCostContext.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCostBreakdown.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCostError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCostResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCostCalculator.cs
```

### 신규 Runtime EditMode test — exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteCandidateCostTests.cs
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

## `SiteCandidateCostWeights` Contract

immutable integer properties:

```text
int AltitudePerSector
int EdgeClearanceDeficit
int DistanceDeficit
int FutureCoreCapacityShortfall
int CoreCluster

static SiteCandidateCostWeights Default { get; }
```

exact default:

```text
AltitudePerSector          = 10
EdgeClearanceDeficit       = 25
DistanceDeficit            = 1000
FutureCoreCapacityShortfall= 100
CoreCluster                = 10000
```

- 각 weight는 `>=0`; public mutable/static mutable state가 없다.
- custom weights는 테스트/향후 tuning seam이며 Authoring CSV를 바꾸지 않는다.
- 계산은 checked `long`을 사용한다.

## `SiteCandidateCostContext` Contract

immutable properties:

```text
SiteDistancePolicy DistancePolicy
IReadOnlyList<FootprintPlacement> ExistingPlacements
int FutureCoreAvailableSectorCount
bool HasFutureCoreCapacityEstimate
```

constructor:

```text
SiteCandidateCostContext(
    SiteDistancePolicy distancePolicy,
    IEnumerable<FootprintPlacement> existingPlacements,
    int futureCoreAvailableSectorCount)
```

- policy는 non-null이다.
- existing placements는 null item 없이 key canonical order로 copied read-only snapshot한다.
- key는 unique하고 policy에 존재해야 한다. footprint overlap을 숨기지 않는다.
- `futureCoreAvailableSectorCount == -1`은 forecast unavailable, `0..169`는 available count다. 그 외 값은 오류다.
- count는 candidate footprint를 포함해 future CorePatch가 사용할 수 있다고 caller가 추정한 sector 수다.
- source list/placement/policy를 mutate하지 않는다.

## Public Calculator API

```text
public sealed class SiteCandidateCostCalculator

SiteCandidateCostResult Calculate(
    FootprintPlacement candidate,
    SiteCandidateCostContext context,
    SpecialMapDefinition specialMap,
    BiomeTypeDefinition primaryBiome,
    BiomePatchRuleDefinition corePatchRule,
    SiteCandidateCostWeights weights)
```

각 호출은 option 하나만 평가한다.

### Start input

- candidate kind exact `Start`.
- `specialMap`, `primaryBiome`, `corePatchRule`은 모두 null이어야 한다.
- future capacity count는 exact `-1`.
- altitude/edge/capacity/cluster units는 `0`; existing special placements가 있으면 applicable Start distance constraints만 계산한다.

### Special-site input

- candidate kind는 `Boss | Forge | CoreResource`; Village는 MAP03_08까지 거부한다.
- specialMap/primaryBiome/corePatchRule은 non-null, active, candidate source/kind/primary-biome identity와 ordinal exact 일치한다.
- corePatchRule은 해당 biome의 exact active `CORE` rule이다.
- biome preferred altitude는 `0 <= min <= max <= 12`다.
- core rule은 positive min sector count, non-negative buffer ring, defined edge flag를 가진다.
- CoreResource/Forge는 capacity `-1` 또는 `0..169`를 허용한다.
- Boss는 Core seed가 아니므로 capacity는 exact `-1`이어야 한다.

공통:

- candidate key는 policy에 존재하고 existing keys와 중복되지 않는다.
- candidate + existing placements는 cross-footprint overlap이 없어야 한다.
- expected invalid input은 sorted failure result이며 partial breakdown은 publish하지 않는다.

## Exact Component Formulas

### 1. Altitude

각 occupied sector의 Y가 preferred band `[minY,maxY]` 밖으로 벗어난 거리 중 maximum:

```text
cellDistance = y < minY ? minY - y
             : y > maxY ? y - maxY
             : 0

AltitudeUnits   = max(cellDistance)
AltitudePenalty = AltitudeUnits * AltitudePerSector
```

footprint 전체가 band 안이면 `0`이다. origin/entry/footprint 평균·중심은 사용하지 않는다.

### 2. Edge clearance

```text
ActualEdgeRing = min(min(x, 12-x, y, 12-y)) over occupied sectors
RequiredEdgeRing = corePatchRule.CanTouchWorldEdge
                 ? 0
                 : corePatchRule.BufferRingSectors

EdgeUnits   = max(0, RequiredEdgeRing - ActualEdgeRing)
EdgePenalty = EdgeUnits * EdgeClearanceDeficit
```

이것은 soft preference다. world-bound/collision은 이미 MAP03_03에서 승인됐고, 실제 capacity는 MAP03_07이 판정한다.

### 3. Partial distance constraints

candidate와 각 existing placement pair에 대해 policy constraint를 조회하고 MAP03_04 footprint distance를 사용한다.

```text
deficit = max(0, MinimumDistance - ActualDistance)

DistanceUnits   = sum(deficit)
DistancePenalty = DistanceUnits * DistanceDeficit
```

- applicable existing pair마다 constraint가 반드시 있어야 한다.
- `DistanceViolationCount`는 deficit > 0인 pair 수다.
- distance deficit은 exact hard signal이다.

### 4. Future Core capacity forecast

CoreResource/Forge에서 forecast가 제공된 경우만:

```text
RequiredCoreSectorCount = corePatchRule.MinSectorCount
CapacityUnits = max(0,
    RequiredCoreSectorCount - FutureCoreAvailableSectorCount)
CapacityPenalty = CapacityUnits * FutureCoreCapacityShortfall
```

- forecast `-1`이면 units/penalty `0`, `HasFutureCoreCapacityEstimate == false`다.
- Boss/Start는 units `0`이다.
- capacity shortfall은 **soft forecast**다. connected flood, buffer inclusion, blockers, biome ownership을 계산하지 않으며 MAP03_07 hard gate를 대체하지 않는다.

### 5. Three-Core 4×4 clustering

candidate가 CoreResource일 때 candidate + existing CoreResource placements를 본다.

- core count `<3`: units `0`.
- core count `>3` 또는 duplicate required Core source: 오류.
- core count exact `3`: 모든 occupied footprint cells union의 bounding box를 계산한다.

```text
windowWidth  = maxX - minX + 1
windowHeight = maxY - minY + 1
CoreClusterDetected = windowWidth <= 4 && windowHeight <= 4
ClusterUnits = CoreClusterDetected ? 1 : 0
ClusterPenalty = ClusterUnits * CoreCluster
```

이것은 fixed `three Core sites in same 4×4 window` hard signal이다. Forge/Boss/Start는 count에 포함하지 않는다.

### Total / hard signal

```text
TotalCost = checked long sum of five weighted penalties

HardConstraintsSatisfied =
    DistanceUnits == 0 && ClusterUnits == 0
```

altitude, edge, capacity forecast는 ranking용 soft cost다. distance/cluster hard signal은 MAP03_06이 candidate rejection에 사용한다. 이 Task가 option을 선택하거나 예외적으로 hard violation을 승인하지 않는다.

## `SiteCandidateCostBreakdown` Contract

immutable properties:

```text
SitePlacementKey CandidateKey
int CandidateOriginIndex
SiteFootprintTransform Transform

int AltitudeUnits
long AltitudePenalty
int EdgeUnits
long EdgePenalty
int DistanceUnits
long DistanceConstraintPenalty
int DistanceConstraintCountChecked
int DistanceViolationCount
int FutureCoreCapacityUnits
long FutureCoreCapacityPenalty
bool HasFutureCoreCapacityEstimate
int RequiredCoreSectorCount
int FutureCoreAvailableSectorCount
int CoreClusterUnits
long QuadrantClusteringPenalty
bool CoreClusterDetected
int CoreWindowWidth
int CoreWindowHeight
bool HardConstraintsSatisfied
long TotalCost
```

- fields는 exact formulas와 합계 identity를 constructor invariant로 검증한다.
- Start/non-applicable values는 `0`, capacity unavailable count는 `-1`, core window unavailable width/height는 `-1/-1`이다.
- candidate selection rank/selected flag/RNG draw/reservation ID를 포함하지 않는다.

## Error / Result Contract

`SiteCandidateCostErrorCode` exact frozen ordinal order:

```text
MissingCandidate
InvalidCandidate
MissingContext
MissingWeights
InvalidWeights
MissingDistancePolicy
InvalidExistingPlacement
DuplicateExistingPlacementKey
CandidateAlreadyPlaced
OverlappingPlacement
MissingSpecialMap
UnexpectedSpecialMap
InvalidSpecialMap
SourceIdentityMismatch
MissingPrimaryBiome
UnexpectedPrimaryBiome
InvalidPrimaryBiome
MissingCorePatchRule
UnexpectedCorePatchRule
InvalidCorePatchRule
MissingPolicyKey
UnexpectedExistingKey
MissingDistanceConstraint
InvalidFutureCapacityEstimate
InvalidCoreResourceSet
CostOverflow
```

`SiteCandidateCostError`는 code, candidate/existing source ID canonical-or-empty, sector index `0..168|-1`, stable non-empty message를 가진다. errors는 code, candidate ID, existing ID, sector index, message ordinal 순으로 sort/dedupe한다. path/stack/timestamp/thread/culture exception text를 포함하지 않는다.

`SiteCandidateCostResult`:

```text
bool Succeeded
SiteCandidateCostBreakdown Breakdown
IReadOnlyList<SiteCandidateCostError> Errors
```

- success: non-null Breakdown, errors `0`
- failure: null Breakdown, errors `>=1`
- partial cost publish 금지

## Starter Exact Definition Gate

| Biome | Preferred Y | CORE min sectors | Can touch edge | Buffer |
|---|---:|---:|---:|---:|
| `BIO_MOON_CRATER` | `0..7` | `5` | true | `1` |
| `BIO_CASSIA_ROOT` | `2..12` | `5` | false | `1` |
| `BIO_ABANDONED_MILL` | `1..11` | `4` | false | `1` |
| `BIO_MOON_DOUGH` | `0..7` | `5` | true | `1` |

- Meteor→Crater, Cassia→Root, Yeast→Dough, Forge/Boss→Mill identity를 확인한다.
- capacity forecast는 Core 3 + Forge에만 적용하고 Boss에는 적용하지 않는다.

## Exact Reference Vectors

altitude units:

```text
Crater y=0/7 -> 0; y=8 -> 1; y=12 -> 5
Cassia y=0 -> 2; y=2/12 -> 0
Mill y=0/12 -> 1; y=1/11 -> 0
Dough y=0/7 -> 0; y=8 -> 1
```

edge units:

```text
Root/Mill edge ring 0 -> 1; ring 1 -> 0
Crater/Dough any valid edge ring -> 0
```

capacity units:

```text
Forge required4 / available3 -> 1
Core required5 / available4 -> 1
available == required -> 0
forecast unavailable -1 -> 0 with HasEstimate false
```

hard signals:

```text
distance actual3 / required4 -> DistanceUnits 1 / violation1
three Core union bounding box 4×4 -> ClusterUnits 1
three Core union bounding box 5×4 -> ClusterUnits 0
```

default-weight aggregate vector:

```text
AltitudeUnits 2       -> 20
EdgeUnits 1           -> 25
DistanceUnits 1       -> 1000
CapacityUnits 1       -> 100
ClusterUnits 1        -> 10000
TotalCost             = 11145
HardConstraintsSatisfied = false
```

## Determinism / Ownership

- existing placement input order/list implementation이 달라도 exact breakdown/error order가 같다.
- seeds `0`, `4660`, `ulong.MaxValue`, wall clock, frame, thread, current culture가 cost를 바꾸지 않는다.
- candidate ordinal/transform은 identity evidence로 보존되지만 occupied cells가 같으면 cost components는 같다.
- fresh/reused calculator 100회, `en-US`/`tr-TR`에서 observable result가 동일하다.
- source placement/policy/definition/context/list를 mutate하지 않는다.
- public setters/fields, mutable collection exposure, static mutable cache, lazy public enumeration이 없다.

## Scope Boundary / DO NOT

- raw origin/transform option enumeration, world-bound/collision 재계산 금지
- candidate list sort/rank/tie-break/shuffle/select 금지
- RNG stream/draw/weighted choice 금지
- reservation ID/order/SectorReservation/SiteReservation/snapshot 생성 금지
- backtracking/retry/max-200/PASS_SITE 금지
- future capacity flood-fill/connected area/buffer ownership hard approval 금지
- Village distance bucket/layout/placement 금지
- route graph/blocked edge/tile movement distance 금지
- biome growth/noise/perimeter/patch ownership 계산 금지
- pass/root adapter, serializer/file I/O/replay 확장 금지
- existing MAP03_01~04 models/tests 수정 금지
- Authoring/generated CSV/meta, asmdef/asmref, Scene/Prefab, Package/ProjectSettings 변경 금지
- test skip/ignore/assertion 완화, Git operation 금지
- MAP03_06 선행 작업 금지

## Collision Handling

1. 신규 destination이 없으면 생성한다.
2. exact 계약과 바이트 동일한 preexisting destination만 `PREEXISTING_IDENTICAL`로 재사용한다.
3. 다르면 overwrite/merge/delete하지 않고 `STATUS: BLOCKED`다.
4. 기존 `.meta` GUID와 사용자 변경을 보존한다.

## Required Tests

`SiteCandidateCostTests.cs` actual NUnit cases 최소 `128`개다.

minimum groups:

- weights default exact `10/25/1000/100/10000`, non-negative/custom/immutability/overflow tests
- context null/duplicate/order/policy/capacity `-1|0..169`/read-only tests
- exact starter biome/core-rule identity table
- altitude exact vectors and footprint max-distance behavior
- edge-ring exhaustive 169 sectors, edge-touch flag and buffer deficit vectors
- partial distance applicable constraints, exact/equal/deficit/multiple-deficit sums
- unknown/existing/missing policy key/constraint and overlap errors
- capacity Core/Forge/Boss/Start applicability, required/available/equal/unknown vectors
- exact three Core source set, `<3`, `4×4` reject signal, `5×4` pass, duplicate/>3 errors
- default aggregate `11145` and hard-signal identity
- altitude/edge/capacity soft penalties do not flip hard signal
- distance/cluster each flip hard signal without throwing or selecting
- Start null typed inputs and special-site exact typed input gates
- null/mismatched/inactive/wrong-kind/wrong-biome/wrong-core-rule inputs and no partial cost
- reversed/shuffled existing placements, array/list, caller mutation isolation
- same occupied cells with different candidate ordinal/transform yield same cost components
- seeds `0/4660/ulong.MaxValue`, fresh/reused 100-run identity
- `en-US`/`tr-TR` culture invariance and public mutation-surface audit
- RNG/ranking/selection/backtracking/flood/village/pass/route/tile-distance/file-I/O production dependency `0`

금지:

- `[Ignore]`, `[Explicit]`, inconclusive, assumption-based skip
- reflection으로 private state를 바꿔 success를 만드는 test
- test order/current filesystem/wall clock 의존
- reference vectors를 hard-coded return하는 fake calculator

## Regression / Verification

```text
New SiteCandidateCostTests: >=128 PASS
MAP03_04 SiteDistanceIndexTests: 239/239 PASS
MAP03_03 FootprintPlacementSolverTests: 170/170 PASS
MAP03_02 SiteCandidateEnumerationTests: 268/268 PASS
MAP03_01 SiteReservationModelsTests: 81/81 PASS
MAP02 phase focused aggregate: 667/667 PASS
SpecialVillageDefinitionBuilderTests: 57/57 PASS
BiomeBoundaryDefinitionBuilderTests: 38/38 PASS
StaticDataRegistryBuilderTests: 53/53 PASS
ContentVersionHashCalculatorTests: 54/54 PASS
Targeted Game.Map.Tests.EditMode: >=2400/2400 PASS
Full project EditMode: >=2440/2440 PASS
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
Assets meta files = 3020
duplicate GUID groups = 0
Authoring CSV = 50
Authoring CSV meta = 50
```

after expected:

```text
new C# = 7
new matching .cs.meta = 7
Assets meta files = 3027
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

`REPORTS/MAP03_05_IMPLEMENT_SITE_CANDIDATE_COST_RESULT.md` 필수:

- `STATUS: PASS|BLOCKED|FAIL`
- implementation summary / exact changed-created paths
- exact weights/component formulas/starter definition gate
- altitude/edge/distance/capacity/cluster vector evidence
- aggregate `11145`, soft/hard signal evidence
- input/error/determinism/immutability/ownership evidence
- focused/regression/targeted/full test counts + job IDs
- Unity refresh/compile/warnings
- before/after meta, duplicate GUID, Authoring count/hash
- scope audit / existing modification count / PREEXISTING_IDENTICAL
- task checklist, recommended commit

PASS 전제:

- exact change set + all compile/test/meta/count/hash/scope gates PASS
- existing Asset modification `0`
- `.APPLIED`는 정확한 `PATCH_ID`, `PATCH_VERSION`, `TASK_KEY`, `TASK_PATH`, `MANIFEST_SHA256`, `TASK_SHA256`를 기록
- `07_PATCH_APPLY_RULES.md` current-task binding에 따라 `.APPLIED`가 존재하고 exact manifest/task SHA와 일치해야 status/master finalize
- MAP03_05만 `COMPLETE`, `MAP03_06_IMPLEMENT_RESERVATION_BACKTRACKING`은 계속 `LOCKED`, 다른 future task도 `LOCKED`

BLOCKED 전제:

- collision/baseline/precondition/change-budget 문제가 있으면 Assets/status/master를 수정하지 않는다.
- Result에 exact blocker와 필요한 repair patch 범위를 기록한다.

Recommended Commit: `feat(map): score deterministic site placement candidates`
