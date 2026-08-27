# MAP04_05 — Implement Multi-Seed Biome Grower

```yaml
status_control:
  task_key: MAP04_05_IMPLEMENT_MULTI_SEED_BIOME_GROWER
  result_file: REPORTS/MAP04_05_IMPLEMENT_MULTI_SEED_BIOME_GROWER_RESULT.md
```

## TASK TYPE

```text
RUNTIME DETERMINISTIC P02 MULTI-SEED COST GROWTH + ATOMIC RETRY CONTRACT + EDITMODE TESTS
```

## Objective

MAP04_04의 immutable Core+Satellite seed partial `BiomePatchSnapshot`과 **같은 PASS_BIOME attempt**의 continued `RNG_BIOME_PATCH`를 입력으로 받아, 미할당·미예약 sector를 모든 existing Core/Satellite patch에 stable multi-seed cost로 소유시킨다.

```text
Cost = graphDistance * distanceWeight
     + abs(y - preferredAltitudeCenter) * altitudeWeight
     + deterministicNoise * noiseWeight
     + exposedPerimeterDelta * compactnessWeight
     + reservationPenalty
```

growth 전에 PatchId ordinal×target sector index 순서로 noise table을 전부 고정하고, 일반 patch의 minimum, per-rule maximum, phase hard maximum `59`, biome world-share 상한, cardinal connectivity를 지킨다. source Core/Satellite seed, Core binding, P01 reservation, already-owned sector는 변경하지 않는다.

MAP04_04 starter는 `11 patches / 27 assigned / 142 unassigned`이며 미할당 중 non-Core reserved `4`개는 성장 대상이 아니다. 따라서 viable success target은 `165 assigned / 4 reserved-unassigned / IsComplete false`다. 단, 실제 attempt-0 `2/0/2/3` Satellite 조합은 legal aggregate capacity `161 < 165`이므로 성공을 위조하지 않고, noise draw 전 exact `RetryRequired` 증거를 내야 한다.

이 Task는 grower API와 개별 attempt 결과만 구현한다. production retry loop, Core/Satellite seed 재추첨, Intrusion, cleanup, generated CSV, final validator, overlay, `PASS_BIOME` root adapter는 구현하지 않는다.

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
12. `REPORTS/MAP04_04_IMPLEMENT_SATELLITE_SEED_PLACER_RESULT.md`

MAP04_04 Result에서 아래 actual evidence를 확인한다.

```text
STATUS: PASS
SatelliteSeedPlacerTests: 141/141 PASS
Required regressions: 458/458 PASS
Actually executed required total: 599/599 PASS
Starter count CRATER/DOUGH/MILL/ROOT: 2/0/2/3
Starter patches / assigned / unassigned: 11 / 27 / 142
RNG method/raw draws and DrawCount: 13 / 13 / 0->13
Reservation intrusion / cross-patch overlap: 0 / 0
Final Assets meta: 3101
Existing / unexpected Assets modifications: 0 / 0
NEXT: MAP04_04 only COMPLETE / Current Task NONE / MAP04_05 remains LOCKED
```

Result SHA-256은 exact 아래다.

```text
2706853d660845b059737c15488221f5bd5d68a5d02e0a3d6c65e9375464e334
```

MAP04_04는 targeted/full을 실행 PASS로 주장하지 않고 discovery/arithmetic `4345/4414`로 분리 기록했다. 이 구분을 보존한다.

## Map Package Reference

Map Package v1.0 exact installed path가 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/01_WORLD_FIXED_SPEC.md
01_FIXED_SPEC/02_COORDINATE_AND_ID_RULES.md
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
01_FIXED_SPEC/04_RUNTIME_ARCHITECTURE.md
01_FIXED_SPEC/05_IMPLEMENTATION_ORDER.md
02_PHASE_ROADMAP/MAP04_BIOME_PATCH_GENERATOR.md
04_CSV_STARTER/biome_types.csv
04_CSV_STARTER/biome_patch_rules.csv
04_CSV_STARTER/generation_profiles.csv
04_CSV_STARTER/rng_streams.csv
```

reference CSV는 frozen contract 확인용으로만 읽고 installed Authoring CSV body를 직접 재파싱하지 않는다.

## READ ALLOWLIST

### Existing Domain / Typed Definitions

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/WorldGenerationDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/BiomeDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/WorldRouteDefinitionSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/StaticDataRegistry.cs
```

### Existing RNG / Grid / P01

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/RngStreamScope.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/DeterministicRngSeedDeriver.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/DeterministicRngStream.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/DeterministicRngStreamFactory.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationRngStreams.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGridIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorReservation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSnapshot.cs
```

### Existing MAP04_01 Models — exact all

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchRole.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchSeed.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomeSectorOwnership.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchSiteBinding.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatch.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchSnapshot.cs
```

### Existing MAP04_02~04 — exact relevant all

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchInitializationPublication.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchGrowthPublication.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SatelliteSeedPlacementError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SatelliteSeedPlacementRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SatelliteSeedPlacementDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SatelliteSeedPlacementPublication.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SatelliteSeedPlacementResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SatelliteSeedPlacer.cs
```

MAP04_01~04 checked-in public API가 이 문서의 illustrative signature보다 우선한다. constructor/property shape에 맞춰 구현하고 기존 파일을 수정하지 않는다.

### Focused Tests / Assemblies

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/DeterministicRngStreamTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/BiomePatchModelsTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/CorePatchGrowerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SatelliteSeedPlacerTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

위 C#/asmdef와 matching meta, approved Runtime/Test `Generation` 직계 파일명의 path-only inventory, Authoring CSV/meta count·hash, 전체 meta GUID, task marker 이후 change-scope path만 확인할 수 있다.

금지:

- installed Authoring CSV body 직접 재파싱·수정
- MAP04_06 이후 Task body
- unrelated production/test C# body
- Legacy/Stage/P6/P11 generator body
- Scene/Prefab YAML
- MAP04_01~04 existing production/test 수정

## WRITE ALLOWLIST

### 신규 Runtime production C# — exact 8

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomeGrowthCost.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomeGrowthNoiseTable.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MultiSeedBiomeGrowthError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MultiSeedBiomeGrowthRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MultiSeedBiomeGrowthDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MultiSeedBiomeGrowthPublication.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MultiSeedBiomeGrowthResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MultiSeedBiomeGrower.cs
```

### 신규 Runtime EditMode test — exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MultiSeedBiomeGrowerTests.cs
```

신규 C# 9개와 matching `.cs.meta` 9개, Result 1만 생성한다. 기존 production/tests/meta/asmdef/asmref는 수정하지 않는다. existing approved directory를 재사용하며 새 directory/folder meta를 만들지 않는다.

## Namespace / Assembly

```text
Runtime namespace: StarNight.Map.WorldGeneration.Generation
Test namespace:    StarNight.Map.Tests.WorldGeneration.Generation
Runtime assembly:  Game.Map.Runtime
Test assembly:     Game.Map.Tests.EditMode
```

`UnityEditor`, `UnityEngine.Object`, ScriptableObject/MonoBehaviour, serialization callback, reflection factory, service locator, singleton/static mutable state를 도입하지 않는다. Unity `6000.3.8f1` current language level에서 compile되도록 record/record struct, `required`, `init`, nullable-reference directive에 의존하지 않는다.

## Frozen P02 Growth Boundary

```text
Input envelope       = successful MAP04_04 SatelliteSeedPlacementResult
Input artifact       = SatelliteSeedPlacementPublication
Source reservation   = immutable SiteReservationSnapshot
Output artifact      = Core+Satellite grown partial BiomePatchSnapshot
Pass ID              = PASS_BIOME (not executed here)
RNG stream           = continued same-attempt RNG_BIOME_PATCH
Grid                 = 13 x 13 / 169 sectors / index y*13+x
Growth roles         = CORE + SATELLITE only
Phase patch hard max = 59 sectors
SecondaryBiomeId     = always empty
```

- source result/publication/P01/input snapshot/definitions/patches/ownership/bindings/seeds를 mutate하지 않는다.
- output은 새 `BiomePatchSnapshot`이며 source와 same world seed, same patch IDs, same seeds, same Core bindings을 보존한다.
- new patch/seed/binding을 만들지 않고 existing patch membership와 matching ownership만 추가한다.
- actual non-Core reserved Start/Boss/Village footprint는 hard blocker며 unassigned로 남긴다.
- 모든 unreserved sector와 already-owned Core footprint은 success 시 exact one PrimaryBiome/PatchId를 가진다.
- `IsComplete == false`는 non-Core reserved unassigned row가 남는 모델 규칙의 정확한 결과이며 failure/partial growth을 뜻하지 않는다.

## Exact Active Definitions / Canonical Order

active required biome exact four와 active non-Intrusion rule exact eight을 요구한다. rule canonical order는 `PatchRuleId` ordinal, patch canonical order는 `PatchId` ordinal이다.

| Rule | Role | Biome | Min / rule max | Edge | Max share | Dist / Alt / Noise / Compact |
|---|---|---|---:|---|---:|---:|
| `PATCH_CRATER_CORE` | Core | `BIO_MOON_CRATER` | `5/18` | true | 0.35 | `1.0/0.25/0.45/0.75` |
| `PATCH_CRATER_SAT` | Satellite | `BIO_MOON_CRATER` | `2/16` | true | 0.35 | `1.0/0.25/0.60/0.65` |
| `PATCH_DOUGH_CORE` | Core | `BIO_MOON_DOUGH` | `5/18` | true | 0.35 | `1.0/0.40/0.45/0.70` |
| `PATCH_DOUGH_SAT` | Satellite | `BIO_MOON_DOUGH` | `2/14` | true | 0.35 | `1.0/0.40/0.60/0.65` |
| `PATCH_MILL_CORE` | Core | `BIO_ABANDONED_MILL` | `4/14` | false | 0.35 | `1.0/0.20/0.35/0.85` |
| `PATCH_MILL_SAT` | Satellite | `BIO_ABANDONED_MILL` | `2/10` | false | 0.35 | `1.0/0.20/0.50/0.80` |
| `PATCH_ROOT_CORE` | Core | `BIO_CASSIA_ROOT` | `5/18` | false | 0.35 | `1.0/0.35/0.45/0.70` |
| `PATCH_ROOT_SAT` | Satellite | `BIO_CASSIA_ROOT` | `2/14` | false | 0.35 | `1.0/0.35/0.60/0.60` |

preferred altitude band:

```text
CRATER 0..7
DOUGH  0..7
MILL   1..11
ROOT   2..12
```

- each patch는 role+biome에 matching exact rule을 가진다.
- `MinSectorCount`/`MaxSectorCount`, `CanTouchWorldEdge`, `MaxWorldShare`, four cost weights를 이 Task에서 사용한다.
- `SeedWeight`, `GrowthWeight`, `BranchinessTarget`는 positive/finite typed fact로 validation하지만 위 fixed cost에 없는 임의 항을 추가하지 않는다. branchiness는 MAP04_07 cleanup/09 validator 범위다.

## Public Grower API

```text
public sealed class MultiSeedBiomeGrower

MultiSeedBiomeGrowthResult Grow(
    SatelliteSeedPlacementResult placementResult,
    GenerationProfileDefinition generationProfile,
    IEnumerable<BiomeTypeDefinition> biomeTypes,
    IEnumerable<BiomePatchRuleDefinition> coreAndSatelliteRules,
    DeterministicRngStream biomePatchRng)
```

- `placementResult.Status == Completed`, publication/diagnostics non-null을 요구한다.
- `biomePatchRng.DrawCount == placementResult.Diagnostics.RngDrawCountAfter`여야 한다.
- starter에서 exact before DrawCount는 `13`이다. generic successful placement의 actual after count를 사용하고 `13`을 production에 hard-code하지 않는다.
- public stream이 initial-state identity를 노출하지 않는 부분은 reflection으로 추정하지 않고 actual factory+same-instance integration test로 증명한다.
- grower는 Registry singleton, CSV/file, clock, current Root/pass state, retry loop를 자체 조회하지 않는다.

## Structural Preflight

RNG draw 전에 가능한 structural error를 accumulated/sorted/deduped validation한다.

- result/publication/diagnostics/source/site snapshot/input snapshot/collections/items non-null
- exact same world seed/reference chain and placement diagnostic count conservation
- exact 169 rows/index-coordinate identity, valid partial snapshot, empty SecondaryBiome
- exact four Core, zero Intrusion, all remaining patches Satellite
- every patch/seed/binding/ownership linkage bidirectional and role/rule/biome identity consistent
- Core seed/binding/source footprint immutable linkage and Satellite source-null/no-binding linkage
- exact required active four biome definitions and exact eight active Core/Satellite rules
- unique IDs, valid ranges, finite values, `1 <= Min <= Max <= 59`, share `0 < x <= 1`
- generation profile active `GEN_MOONPALACE_V1`, matching profile, `BiomeRetryMax == 100`
- P01 reservations exact 169 and input ownership does not intrude into nonmatching reservation
- each current patch count `<= rule.MaxSectorCount`, each current biome count `<= world-share cap`
- continued RNG non-null and exact DrawCount precondition
- undefined enum/numeric cast, caller-order/lazy mutation dependency rejection

structural invalid input은 RNG delta `0`, `InvalidInput`, retry false, publication/diagnostics null, stable errors `>=1`을 반환한다.

`MultiSeedBiomeGrowthErrorCode` structural frozen order:

```text
MissingPlacementResult
PlacementNotCompleted
MissingPlacementPublication
MissingPlacementDiagnostics
InvalidPlacementPublication
InvalidSourceSiteSnapshot
MissingGenerationProfile
InvalidGenerationProfile
MissingBiomeTypes
MissingPatchRules
NullDefinition
DuplicateDefinitionId
MissingBiomeDefinition
UnexpectedBiomeDefinition
MissingPatchRule
UnexpectedPatchRule
InvalidBiomeDefinition
InvalidPatchRule
DefinitionIdentityMismatch
InvalidPatchState
InvalidReservationState
MissingBiomePatchRng
InvalidBiomePatchRngState
InternalInvariantViolation
```

## Exact Capacity Gate — before Noise

target ownership은 existing assigned와 unassigned·unreserved row 전체다. unassigned reserved row는 hard blocker로 target에서 뺀다.

```text
TargetOwned = 169 - UnassignedReservedCount
PatchCap(p) = min(p.Rule.MaxSectorCount, 59)
BiomeShareCap(b) = floor(169 * CommonMaxWorldShare of b's patch rules)
BiomeLegalCapacity(b) = min(sum PatchCap of b, BiomeShareCap(b))
AggregateLegalCapacity = sum BiomeLegalCapacity
```

same-biome Core/Satellite rule의 `MaxWorldShare`가 exact 같은지 preflight한다. share는 million scale checked integer로 quantize한 뒤 floor하며 starter `0.35`는 exact `59`다.

```text
Starter target owned                 = 165
CRATER legal capacity                = min(18+16+16, 59) = 50
DOUGH legal capacity                 = min(18,       59) = 18
MILL legal capacity                  = min(14+10+10, 59) = 34
ROOT legal capacity                  = min(18+14+14+14, 59) = 59
Starter aggregate legal capacity     = 161
Starter shortfall                    = 4
```

따라서 actual attempt-0는 exact `InsufficientAggregateCapacity`, `RequiredCount=165`, `AvailableCount=161`, `Shortfall=4`, `RetryRequired`, publication null, growth records `0`, RNG `13->13`이다. rule maximum/world-share를 넘겨 억지로 채우거나 implicit patch를 만들지 않는다.

capacity gate를 통과한 입력에서만 noise table/growth를 시작한다. production grower는 attempt를 재추첨하지 않고 현재 attempt의 retry-required만 반환한다.

spatial failure code frozen continuation:

```text
InsufficientAggregateCapacity
MinimumGrowthBlocked
GrowthFrontierExhausted
```

## Deterministic Noise Table

capacity gate 후 growth claim 전에 immutable `BiomeGrowthNoiseTable`을 완성한다.

```text
for patch in PatchId ordinal:
    for sector in target-unassigned SectorIndex ascending:
        noisePermille[patch, sector] = biomePatchRng.NextInt(1001)
```

- target-unassigned은 unassigned·unreserved row만 포함한다.
- range는 exact integer `0..1000`; `%`, float scaling, `NextDouble01`, System/Unity RNG를 쓰지 않는다.
- method-call count는 exact `PatchCount * TargetUnassignedCount`다.
- raw draw delta는 existing rejection sampling의 actual `DrawCount` delta를 기록한다.
- table 완성 후 growth/tie-break은 RNG를 추가 소비하지 않는다.
- growth 성공/실패, heap pop/stale entry, caller collection order가 noise schedule을 바꾸지 못한다.
- table은 copied read-only values, exact dimensions, stable unsigned checksum, before/after draw count를 제공한다.

## Checked-Integer Cost Contract

float/double total cost로 winner를 비교하지 않는다. 모든 cost weight는 `WeightScale=1000` checked integer로 quantize하고 source value가 thousandth에서 tolerance `1e-6`을 넘으면 invalid이다.

```text
DistanceWeightMilli    = round(distanceWeight * 1000, AwayFromZero)
AltitudeWeightMilli    = round(altitudeWeight * 1000, AwayFromZero)
NoiseWeightMilli       = round(noiseWeight * 1000, AwayFromZero)
CompactnessWeightMilli = round(compactnessWeight * 1000, AwayFromZero)

GraphDistance          = min Manhattan distance to any immutable seed of patch
AltitudeDistance2      = abs(2*y - (preferredMinY + preferredMaxY))
NoisePermille          = fixed table value 0..1000
SamePatchNeighborCount = cardinal neighbors currently owned by patch
ExposedPerimeterDelta  = 4 - 2*SamePatchNeighborCount

GraphTerm2       = 2 * GraphDistance * DistanceWeightMilli
AltitudeTerm2    = AltitudeDistance2 * AltitudeWeightMilli
NoiseTerm2       = round((2 * NoisePermille * NoiseWeightMilli) / 1000, half-up)
PerimeterTerm2   = 2 * ExposedPerimeterDelta * CompactnessWeightMilli
ReservationTerm2 = 0 or 10_000_000
TotalCost2       = checked sum of the five terms
```

`TotalCost2`는 original cost의 twice-milli representation이다. overflow, nonfinite, negative disallowed weight는 stable invalid input으로 atomic 종료한다.

reservation term:

- matching Core footprint: already immutable-owned; candidate가 아님
- other Core footprint: hard blocker `+∞`; candidate가 아님
- actual Start/Boss/Village footprint: hard blocker; output unassigned
- unreserved cardinal 1-ring of `Kind == Boss` or `Kind == Village`: finite `10_000_000`
- other unreserved sector: `0`

finite penalty는 일반 후보보다 뒤로 보내지만 world fill에 필요하면 claim을 허용한다. reservation ID 문자열 heuristic이 아니라 typed `SiteReservationKind`를 사용한다.

## Exact Growth Algorithm

source를 복사한 temporary working arrays에서만 수행한다.

### A. Minimum Guarantee

1. Core/Satellite patch별 current size와 matching rule minimum을 계산한다.
2. under-minimum patch만 PatchId ordinal round에 참여한다.
3. each patch의 cardinal frontier에서 hard blocker, assigned row, edge-forbidden, patch/biome cap 위반을 제거한다.
4. 남은 candidate를 `(TotalCost2, SectorIndex)`로 정렬해 one claim하고 다음 patch로 간다.
5. 한 round에 어떤 deficit도 줄지 않았거나 patch frontier가 비었으면 `MinimumGrowthBlocked` retry-required다.
6. every Core/Satellite patch가 `>= MinSectorCount`가 되면 B로 간다.

`CanTouchWorldEdge == false`인 patch는 growth candidate도 모든 world-edge sector를 거부한다. true patch만 edge를 claim할 수 있다.

### B. Stable Multi-Seed Frontier

1. 모든 non-capped patch의 current cardinal frontier를 stable binary min-heap에 넣는다.
2. winner key는 exact `(TotalCost2, PatchId ordinal, SectorIndex)`다. insertion order/hash/dictionary order는 winner가 아니다.
3. entry는 patch revision을 가진다. pop 시 assigned/block/cap violation은 discard하고, stale revision은 current perimeter로 recompute/reinsert한다.
4. claim 후 patch revision을 올리고 그 patch의 complete current frontier를 current revision으로 enqueue한다.
5. other patch가 cell을 먼저 claim한 stale entry는 pop 시 discard한다.
6. target-unassigned가 `0`이 될 때까지 반복한다.
7. target이 남았는데 heap이 비면 `GrowthFrontierExhausted` retry-required다.

claim은 cardinal frontier에서만 일어나므로 each patch는 seed부터 connected다. diagonal/wrap/clamp를 쓰지 않는다.

## Success / Atomic Failure Contract

success publication:

```text
SatelliteSeedPlacementPublication SourcePlacement
SiteReservationSnapshot SourceSiteSnapshot
BiomePatchSnapshot Snapshot
IReadOnlyList<MultiSeedBiomeGrowthRecord> GrowthRecords
int PatchCount
int InitialAssignedSectorCount
int AddedSectorCount
int FinalAssignedSectorCount
int FinalUnassignedReservedSectorCount
```

success invariant:

- every unreserved row assigned; unassigned row는 exact non-Core reserved footprint only
- each patch sector↔ownership bidirectional, no overlap/orphan/wrong biome
- exact same patch/seed/binding IDs and source identities
- every Core/Satellite patch cardinal connected and within its rule min/max and hard 59
- each biome owned count `<= floor(169 * MaxWorldShare)`
- SecondaryBiome empty, Intrusion count `0`
- source snapshot/result/definitions observable state unchanged
- `InitialAssigned + Added == FinalAssigned`
- `FinalAssigned + FinalUnassignedReserved == 169`

`MultiSeedBiomeGrowthStatus`:

```text
Completed
InvalidInput
RetryRequired
```

- `Completed`: publication/diagnostics non-null, errors `0`, retry false.
- `InvalidInput`: publication/diagnostics null, stable errors `>=1`, retry false, RNG delta `0`.
- `RetryRequired`: publication null, diagnostics non-null, retry error `>=1`, source counts로 rollback한다.
- capacity failure는 noise 전이므로 RNG delta `0`; minimum/frontier failure는 completed noise-table draw delta를 정확히 기록한다.
- unexpected overflow/model construction/invariant failure는 stable `InternalInvariantViolation` 하나로 atomic invalid failure하고 exception text/partial output을 노출하지 않는다.

`MultiSeedBiomeGrowthError` fields:

```text
MultiSeedBiomeGrowthErrorCode Code
string DefinitionId
string BiomeId
BiomePatchId? PatchId
int SectorIndex
int RequiredCount
int AvailableCount
int Shortfall
string Message
```

unknown identity empty/null, unknown sector `-1`, counts non-negative, `Shortfall=max(0,Required-Available)`다. code/definition/biome/patch/sector/count/message ordinal로 sort/dedupe한다.

## Record / Diagnostics Contract

each immutable growth record:

```text
int Sequence
BiomePatchId PatchId
string BiomeId
BiomePatchRole Role
int SectorIndex
SectorCoord Coordinate
int PatchSizeBefore
int PatchSizeAfter
bool WasMinimumPhase
BiomeGrowthCost Cost
```

diagnostics는 최소 아래를 보존한다.

```text
ulong WorldSeed
int InitialPatchCount
int InitialAssignedSectorCount
int TargetUnassignedSectorCount
int HardBlockedReservedSectorCount
int TargetOwnedSectorCount
int AggregateLegalCapacity
int MinimumPhaseClaimCount
int CompetitiveClaimCount
int TotalClaimCount
int FinalAssignedSectorCount
int FinalUnassignedSectorCount
int NoiseValueCount
int NoiseMethodCallCount
ulong RngDrawCountBefore
ulong RngDrawCountAfter
ulong NoiseChecksum
int ReservationPenaltyClaimCount
int PatchOverlapCount
int DisconnectedPatchCount
```

success records는 sequence, diagnostics per-patch/per-biome counts는 ID ordinal copied read-only다. retry는 partial growth records/publication을 노출하지 않고 failure point와 rollback counts만 diagnostics에 보존한다.

## Starter / Viable-Attempt Evidence

attempt-0 exact required test:

```text
Input patches / assigned / unassigned     = 11 / 27 / 142
Unassigned non-Core reserved / target     = 4 / 138
Target owned / legal capacity / shortfall = 165 / 161 / 4
Status                                    = RetryRequired
Publication / growth records              = null / 0
RNG DrawCount                             = 13 -> 13
Source mutation                           = 0
```

또한 actual RNG factory+SatelliteSeedPlacer로 `BiomeRetryMax` 범위의 별도 attempt stream을 만든 integration fixture에서 **capacity를 통과하는 first observed attempt** 하나를 성공 성장시킨다. 이 loop는 test evidence일 뿐 production retry loop이 아니다. Result에 actual attempt ordinal, Satellite count vector, initial DrawCount, patch/target/noise counts, raw draw delta, final per-patch/per-biome counts를 기록한다. expected attempt ordinal/coordinates를 production에 hard-code하지 않는다.

## Determinism / Immutability

same successful placement + same continued RNG state는 fresh/reused grower, reversed/shuffled definitions, different culture/time/thread에서 byte-equivalent logical publication/records/diagnostics and exact same RNG after state를 만든다.

- no random GUID/timestamp/hash-set enumeration tie-break
- no `System.Random`/Unity RNG/clock/filesystem
- no mutable static cache/counter/current publication
- caller collection/list ownership leakage 없음
- failure 후 same instance valid call은 fresh instance와 동일
- source result/publication/P01/P02/definitions order/state unchanged
- another independent stream extra draws do not alter result

## Required Focused Tests

`MultiSeedBiomeGrowerTests.cs`에 최소 `160`개 actual NUnit case를 만든다.

필수 coverage:

1. exact four biome/eight rule table, canonical rule/patch order
2. structural accumulated/sorted/deduped errors and RNG zero
3. placement Completed/publication/diagnostics and continued DrawCount precondition
4. exact 169/index-coordinate/patch↔ownership/source linkage validation
5. attempt-0 capacity `165/161/4`, RetryRequired, RNG `13->13`
6. per-patch rule cap, hard 59, per-biome floor share cap arithmetic
7. capacity boundary exact equal accepted, one-short rejected
8. target excludes exact non-Core reserved footprint but includes all unreserved rows
9. PatchId×SectorIndex noise order, `NextInt(1001)`, dimensions/checksum
10. noise schedule fixed before growth; no claim/stale-pop dependent draw
11. graph distance uses every immutable patch seed and cardinal Manhattan
12. altitude doubled-center for even/odd band centers
13. weight quantization/tolerance, all five exact cost terms, checked overflow
14. noise half-up term endpoints 0/1000 and intermediate values
15. exposed perimeter delta for 1/2/3/4 same-patch neighbors
16. Boss/Village typed 1-ring finite penalty, Start ring zero, actual reservations hard block
17. false-edge rule rejects four edges/corners; true-edge rule may claim
18. Satellite transient one-cell reaches minimum before competitive fill
19. multiple deficit patches, collision, zero-progress minimum retry rollback
20. stable heap tuple, stale revision recompute, already-claimed discard
21. patch/cardinal connectivity and no diagonal/wrap/clamp ownership
22. per-patch maximum and biome cap remove frontier entries exactly
23. frontier exhaustion retry, no publication/partial records, source rollback
24. viable-attempt integration success: all target rows assigned, only reserved unassigned
25. final patch min/max/59/share/connectivity/site Core ownership conservation
26. Core/Satellite seeds and Core bindings exact immutable identity
27. source/caller collections immutable, shuffled enumeration/culture determinism
28. fresh/reused grower, same/independent stream determinism
29. empty target valid zero-claim success/noise dimensions
30. forbidden Unity/SystemRandom/time/filesystem/root/retry/static mutable dependency scan

fixture는 installed Authoring CSV body를 읽거나 reflection으로 private invariant를 우회하지 않는다. checked-in public constructors/factories, typed in-memory definitions, actual deterministic RNG factory를 사용한다.

## Required Verification — Reduced but Explicit

실제로 실행:

```text
MultiSeedBiomeGrowerTests               >=160 PASS
SatelliteSeedPlacerTests                  141/141 PASS
CorePatchGrowerTests                      127/127 PASS
BiomePatchModelsTests                     107/107 PASS
DeterministicRngStreamTests               103/103 PASS
Required regression total                 478/478 PASS
Actually executed required total         >=638 PASS
Failed / skipped                            0 / 0
```

large suites는 사용자 지정 감축 profile에 따라 실행하지 않고 discovery/arithmetic로만 확인한다.

```text
Game.Map targeted discovery arithmetic  >=4505
Full EditMode current discovery          >=4574
```

Result에서 actually executed PASS와 discovery-only count를 별도 항목으로 기록한다. 실행하지 않은 targeted/full을 `PASS`라고 쓰지 않는다.

Unity forced compile 후:

```text
compile errors         = 0
Console errors         = 0
relevant new warnings  = 0
```

## Asset / Change Gates

```text
Baseline Assets meta: 3101
New Runtime C#: 8
New test C#: 1
New matching meta: 9
Final Assets meta: 3110
Exact Assets changes after .APPLIED: 18
Existing Assets modifications: 0
Unexpected Assets changes: 0
Authoring CSV/meta: 50/50 unchanged
Accepted legacy Editor folder meta: 6/6 unchanged
Duplicate GUID groups: 0
```

각 신규 `.cs.meta`는 `fileFormatVersion: 2`, valid unique non-zero lowercase 32-hex GUID, `MonoImporter`여야 한다. Unity가 unrelated folder meta를 만들면 baseline 여부를 확인하고 이 Task가 만든 unexpected file은 제거한다. existing legacy Editor folder meta 6개는 삭제·재생성하지 않는다.

## Result File Contract

`REPORTS/MAP04_05_IMPLEMENT_MULTI_SEED_BIOME_GROWER_RESULT.md` 첫 부분:

```text
# MAP04_05 — Implement Multi-Seed Biome Grower Result

STATUS: PASS
```

Result는 최소 아래를 포함한다.

- PATCH APPLY와 `.APPLIED` hash
- READ/WRITE allowlist 준수
- CREATED/MODIFIED/PREEXISTING_IDENTICAL
- 8 runtime + 1 test exact path와 matching meta/GUID
- attempt-0 capacity `165/161/4`, RetryRequired, RNG `13->13`, rollback
- viable-attempt actual ordinal/count vector/noise schedule/draws/checksum
- exact cost quantization/five terms/frontier/tie-break evidence
- final patch/biome counts, min/max/share/connectivity, assigned/reserved-unassigned conservation
- Core/Satellite seed/Core binding/P01/source immutability
- actual focused/regression counts와 job IDs
- targeted/full discovery는 executed PASS와 분리
- compile/Console/meta/GUID/Authoring/change-scope evidence
- OUT_OF_SCOPE_FINDINGS
- NEXT: MAP04_05만 COMPLETE/Current Task NONE, MAP04_06 LOCKED

어느 gate든 실패하거나 current public API가 frozen contract와 양립할 수 없으면 `STATUS: BLOCKED` 또는 `STATUS: FAIL`을 정확히 기록한다. MAP04_01~04 파일을 repair하거나 partial output을 finalize하지 않는다.

## STATUS FINALIZE

전부 PASS일 때만:

```text
MAP04_05_IMPLEMENT_MULTI_SEED_BIOME_GROWER: COMPLETE
Current Task: NONE
Last Completed Task: MAP04_05_IMPLEMENT_MULTI_SEED_BIOME_GROWER
Last Result: REPORTS/MAP04_05_IMPLEMENT_MULTI_SEED_BIOME_GROWER_RESULT.md / STATUS: PASS
MAP04_06_IMPLEMENT_INTRUSION_PLACEMENT: LOCKED
```

MAP04_06을 자동 시작하거나 Task 파일을 만들지 않는다.

## DO NOT

- existing C#/meta/asmdef/asmref/CSV/Scene/Prefab 수정 금지
- MAP04_01~04 convenience repair 금지
- capacity 부족 attempt의 rule max/59/share 초과 금지
- non-Core reserved footprint 소유 금지
- existing assigned sector winner overwrite 금지
- seed/binding/patch ID 재생성·재선정 금지
- noise on-demand draw, heap-pop draw, float total winner 비교 금지
- diagonal/wrap/clamp/disconnected claim 금지
- implicit Intrusion/InactiveBuffer/new patch 생성 금지
- checkerboard/1-cell neck cleanup 금지
- serializer/generated CSV/file I/O 금지
- final validator/overlay/EditorWindow/Gizmo/menu 금지
- production `PASS_BIOME` adapter/root/retry loop 금지
- Git commit/push 금지

## Recommended Commit

```text
feat(map): grow deterministic multi-seed biome patches
```
