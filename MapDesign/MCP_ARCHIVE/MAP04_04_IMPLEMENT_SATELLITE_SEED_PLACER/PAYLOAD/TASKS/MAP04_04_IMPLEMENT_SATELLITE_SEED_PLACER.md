# MAP04_04 — Implement Satellite Seed Placer

```yaml
status_control:
  task_key: MAP04_04_IMPLEMENT_SATELLITE_SEED_PLACER
  result_file: REPORTS/MAP04_04_IMPLEMENT_SATELLITE_SEED_PLACER_RESULT.md
```

## TASK TYPE

```text
RUNTIME DETERMINISTIC P02 SATELLITE COUNT + DISTANCE-CONSTRAINED SEED PLACEMENT + EDITMODE TESTS
```

## Objective

MAP04_03의 immutable Core-grown partial `BiomePatchSnapshot`을 입력으로 받아 active four Satellite rule의 seed count를 fresh continued `RNG_BIOME_PATCH`에서 먼저 추첨하고, unassigned·unreserved sector에 같은 biome 기존 patch/seed와 `MinSeedDistance`를 지키는 one-cell Satellite seed patch를 배치한다.

```text
input starter       = 4 Core patches / 20 assigned / 149 unassigned
Satellite rules     = exact 4
Satellite count     = each rule inclusive SeedCountMin..SeedCountMax
total Satellite     = 0..11
output assigned     = 20..31
output unassigned   = 149..138
```

성공한 경우에만 Satellite seed patch와 matching ownership을 포함한 새 immutable partial snapshot을 atomic publish한다. Core patch sector, Core seed, Core binding, source P01 reservation은 절대 바꾸지 않는다.

이번 Task는 Satellite **seed placement만** 한다. Satellite patch를 `MinSectorCount` 이상으로 성장시키고 remaining world를 채우는 일은 `MAP04_05_IMPLEMENT_MULTI_SEED_BIOME_GROWER`의 책임이다. Intrusion, cleanup, generated CSV, final validator, overlay, `PASS_BIOME`/root adapter도 구현하지 않는다.

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
12. `REPORTS/MAP04_03_IMPLEMENT_CORE_PATCH_GROWER_RESULT.md`

MAP04_03 Result에서 아래 실제 evidence를 확인한다.

```text
STATUS: PASS
New CorePatchGrowerTests: 127/127 PASS
Required regressions: 443/443 PASS
Actually executed required total: 570/570 PASS
Starter: 4 patches / 20 assigned / 149 unassigned / mandatory +16 / supplemental 0 / RNG 0
Reservation intrusion / cross-patch overlap: 0 / 0
Final Assets meta: 3093
Existing / unexpected Assets modifications: 0 / 0
NEXT: MAP04_03 only COMPLETE / Current Task NONE / MAP04_04 remains LOCKED
```

Result SHA-256은 exact 아래다.

```text
6f4ace7b730f4df4662fcc4409d90d031555965bec47e1c62180a8632119280e
```

MAP04_03은 사용자 지정 감축 검증에서 targeted/full을 실행 PASS로 주장하지 않고 discovery/arithmetic `4204/4273`으로 분리 기록했다. 이 구분을 보존하고 미실행 large suite를 prior PASS로 바꾸지 않는다.

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

reference CSV는 frozen contract 확인용으로만 읽는다. installed Authoring CSV body를 직접 읽거나 재파싱하지 않는다. exact reference가 없으면 이 Task와 existing typed definitions/API를 authoritative fallback으로 사용한다.

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

### Existing RNG

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/RngStreamScope.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/DeterministicRngSeedDeriver.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/DeterministicRngStream.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/DeterministicRngStreamFactory.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationRngStreams.cs
```

### Existing P00 / P01 Models

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGridIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationId.cs
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

### Existing MAP04_02 / MAP04_03 — exact relevant all

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchIdFactory.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchInitializationPublication.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchGrowthError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchGrowthRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchGrowthDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchGrowthPublication.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchGrowthResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchGrower.cs
```

MAP04_01~03 checked-in model/public API가 Result 요약이나 이 문서의 illustrative signature보다 우선한다. existing constructor/property shape에 맞춰 새 placer를 구현하고 기존 파일을 수정하지 않는다.

### Focused Tests / Assemblies

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/DeterministicRngStreamTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/BiomePatchModelsTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/CorePatchSeedInitializerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/CorePatchGrowerTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

위 C#/asmdef와 matching meta, approved Runtime/Test `Generation` 직계 파일명의 path-only inventory, Authoring CSV/meta count·hash, 전체 meta GUID, task marker 이후 change-scope path만 확인할 수 있다.

금지:

- installed Authoring CSV body 직접 재파싱·수정
- MAP04_05 이후 Task body
- unrelated production/test C# body
- Legacy/Stage/P6/P11 generator body
- Scene/Prefab YAML
- MAP04_01~03 existing production/test 수정

## WRITE ALLOWLIST

### 신규 Runtime production C# — exact 7

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SatellitePatchIdFactory.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SatelliteSeedPlacementError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SatelliteSeedPlacementRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SatelliteSeedPlacementDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SatelliteSeedPlacementPublication.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SatelliteSeedPlacementResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SatelliteSeedPlacer.cs
```

### 신규 Runtime EditMode test — exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SatelliteSeedPlacerTests.cs
```

신규 C# 8개와 matching `.cs.meta` 8개, Result 1만 생성한다. 기존 production/tests/meta/asmdef/asmref는 수정하지 않는다. existing approved directory를 재사용하며 새 directory/folder meta를 만들지 않는다.

## Namespace / Assembly

```text
Runtime namespace: StarNight.Map.WorldGeneration.Generation
Test namespace:    StarNight.Map.Tests.WorldGeneration.Generation
Runtime assembly:  Game.Map.Runtime
Test assembly:     Game.Map.Tests.EditMode
```

`UnityEditor`, `UnityEngine.Object`, ScriptableObject/MonoBehaviour, serialization callback, reflection factory, service locator, singleton/static mutable state를 도입하지 않는다. Unity `6000.3.8f1` current language level에서 compile되도록 record/record struct, `required`, `init`, nullable-reference directive에 의존하지 않는다.

## Frozen P02 Satellite Placement Boundary

```text
Input artifact       = MAP04_03 CorePatchGrowthPublication
Source reservation   = final immutable SiteReservationSnapshot
Output artifact      = Core + one-cell Satellite-seed partial BiomePatchSnapshot
Pass ID               = PASS_BIOME (not executed in this Task)
RNG stream            = fresh RNG_BIOME_PATCH / PASS_BIOME attempt stream
Grid                  = 13 x 13 / 169 sectors / index y*13+x
New role              = SATELLITE only
Input RNG draw count  = 0
```

- source growth publication, initialization, P01 snapshot, input P02 snapshot, definitions, patches, ownership, bindings, seeds를 mutate하지 않는다.
- output은 새 `BiomePatchSnapshot`이며 source/input과 same world seed다.
- Core patches/seeds/bindings/ownership은 byte-equivalent logical identity로 보존한다.
- Satellite seed는 source reservation이 없는 `BiomePatchSeed(Role.Satellite)`다.
- each Satellite patch는 이번 partial stage에서 seed sector 하나만 소유한다. `AllowSingleSector == false`여도 MAP04_05 growth 전 transient one-cell seed patch는 허용한다.
- SecondaryBiomeId는 모든 row에서 empty를 유지한다.
- output은 remaining sectors가 있으므로 `IsComplete == false`다.

## Exact Active Satellite Rules / Canonical Order

canonical rule/roll/placement order는 `PatchRuleId` ordinal이다.

| Order | Rule | Biome | Min/Max sectors | Min seed distance | Count min/max | Seed weight | Touch edge |
|---:|---|---|---:|---:|---:|---:|---|
| 0 | `PATCH_CRATER_SAT` | `BIO_MOON_CRATER` | `2/16` | 3 | `0/3` | 70 | true |
| 1 | `PATCH_DOUGH_SAT` | `BIO_MOON_DOUGH` | `2/14` | 3 | `0/3` | 70 | true |
| 2 | `PATCH_MILL_SAT` | `BIO_ABANDONED_MILL` | `2/10` | 3 | `0/2` | 45 | false |
| 3 | `PATCH_ROOT_SAT` | `BIO_CASSIA_ROOT` | `2/14` | 3 | `0/3` | 70 | false |

- exact four active Satellite rules와 four active required biome definitions를 요구한다.
- `SeedWeight`는 positive typed fact로 보존하되 이번 uniform seed-sector selection weight로 사용하지 않는다. MAP04_05 growth weighting의 입력이다.
- each biome은 existing Core patch exact 1개를 가져야 한다.
- `existing same-biome patch count + SeedCountMax <= BiomeTypeDefinition.MaxPatchCount`여야 한다.
- `BiomeTypeDefinition.MinPatchCount`는 existing Core 하나로 이미 충족되며 count를 강제로 올리지 않는다.
- caller definition enumeration order가 바뀌어도 canonical order와 RNG schedule은 변하지 않는다.

## `SatellitePatchIdFactory` Contract

Satellite instance ID는 biome identity와 zero-based Satellite ordinal만으로 결정한다.

```text
PATCHINST_SAT_<BIOME_ID>_<ORDINAL_D2>
```

exact examples:

```text
PATCHINST_SAT_BIO_MOON_CRATER_00
PATCHINST_SAT_BIO_MOON_CRATER_01
PATCHINST_SAT_BIO_ABANDONED_MILL_00
```

public stateless API:

```text
BiomePatchId Create(string biomeId, int satelliteOrdinal)
bool TryCreate(string biomeId, int satelliteOrdinal, out BiomePatchId patchId)
```

- ordinal exact `0..99`, ASCII decimal two digits다.
- invalid biome ID/ordinal을 거부한다.
- world seed, sector, candidate roll, attempt, RNG output, timestamp, culture를 ID에 넣지 않는다.
- trim/case-fold/Unicode normalization과 static mutable cache를 사용하지 않는다.

## Public Placer API

```text
public sealed class SatelliteSeedPlacer

SatelliteSeedPlacementResult Place(
    CorePatchGrowthPublication growth,
    GenerationProfileDefinition generationProfile,
    IEnumerable<BiomeTypeDefinition> biomeTypes,
    IEnumerable<BiomePatchRuleDefinition> satelliteRules,
    DeterministicRngStream biomePatchRng)
```

호출자는 `WorldGenerationRngStreams`에서 current world seed, `PASS_BIOME`, attempt ordinal로 만든 fresh exact `RNG_BIOME_PATCH` instance를 전달한다. MAP04_01~03은 RNG를 소비하지 않았으므로 input `DrawCount == 0`이어야 한다.

placer는 Registry singleton, factory, CSV/file, clock, current Root/pass state를 자체 조회하지 않는다. stream ID는 public `DeterministicRngStream` shape에 없다면 억지 reflection으로 추정하지 않고 integration test가 exact factory/definition/scope를 증명한다.

## Structural Preflight

RNG draw 전에 가능한 오류를 모두 accumulated/sorted validation한다.

- growth/publication/source/snapshot/collections/items가 non-null이다.
- source/input/output seed와 reference linkage가 consistent하다.
- growth input은 exact four Core-only patch, no Satellite/Intrusion, empty SecondaryBiome, valid 169-row partial snapshot이다.
- current starter는 `4 Core / 20 assigned / 149 unassigned`; generic valid MAP04_03 output도 허용한다.
- Core patch/seed/binding/ownership과 growth records가 bidirectionally consistent하다.
- generation profile exact active `GEN_MOONPALACE_V1`, matching world profile, `BiomeRetryMax == 100`이다.
- biome collection exact required active four, ID unique, patch count ranges valid다.
- Satellite rule collection exact active four table, unique rule/biome, role `SATELLITE`다.
- `1 <= MinSectorCount <= MaxSectorCount <= 169`, `MinSeedDistance 0..24`, `0 <= SeedCountMin <= SeedCountMax <= 99`, positive SeedWeight다.
- `BufferRingSectors == 0`, `AllowSingleSector == false`, rule↔biome identity, `CanTouchWorldEdge` exact starter values를 검증한다.
- P01 reservation rows exact 169/index-coordinate identity이며 matching Core source footprint ownership을 제외한 reserved row가 input에서 unassigned다.
- biomePatchRng non-null, `DrawCount == 0`이다.
- undefined enum/numeric cast, duplicate ID/index, lazy/caller mutation dependency를 거부한다.

structural invalid input은 RNG delta `0`, `InvalidInput`, retry false, publication/diagnostics null, stable sorted errors `>=1`을 반환한다.

`SatelliteSeedPlacementErrorCode` structural frozen order:

```text
MissingGrowthPublication
InvalidGrowthPublication
MissingSourceSiteSnapshot
InvalidSourceSiteSnapshot
MissingGenerationProfile
InvalidGenerationProfile
MissingBiomeTypes
MissingSatelliteRules
NullDefinition
DuplicateDefinitionId
MissingBiomeDefinition
UnexpectedBiomeDefinition
MissingSatelliteRule
UnexpectedSatelliteRule
InvalidBiomeDefinition
InvalidSatelliteRule
DefinitionIdentityMismatch
InvalidCorePatchState
InvalidReservationState
MissingBiomePatchRng
InvalidBiomePatchRngState
PatchCountLimitExceeded
InternalInvariantViolation
```

## Exact RNG Schedule — Counts First

structural preflight가 성공하면 candidate를 보기 전에 exact four count를 모두 먼저 추첨한다.

```text
for rule in PatchRuleId ordinal:
    desired = biomePatchRng.NextInt(
        rule.SeedCountMin,
        rule.SeedCountMax + 1)
```

- inclusive `[min,max]`를 existing unbiased `NextInt(minInclusive,maxExclusive)`로 구현한다.
- exact four rules 모두 one method call씩 사용한다. range width가 1이어도 draw를 생략하지 않는다.
- `%`, float/double scaling, `System.Random`, `UnityEngine.Random`, seed-weight count roll을 사용하지 않는다.
- count four 개를 placement 사이에 끼워 넣지 않는다. candidate retry가 later biome count를 바꾸지 못한다.
- desired count가 0이면 그 rule의 candidate draw/patch 생성은 0이다.

MAP02 known vector world seed `0x0123456789ABCDEF`, `PASS_BIOME`, attempt `0`, initial state `98BC23250806566B`에서 count-only exact vector:

```text
rule order       = CRATER / DOUGH / MILL / ROOT
desired counts   = 2 / 0 / 2 / 3
method calls     = 4
raw DrawCount    = 4
```

production에 vector lookup을 넣지 않는다.

## Raw Candidate Universe

count draw 후 base universe를 한 번 canonical build한다.

```text
candidate is eligible for raw universe when:
    input ownership is unassigned
    AND P01 SectorReservation is unreserved
```

- index `0..168` ascending copied list다.
- existing Core-owned sector, Start/Boss/Village/any Core source reservation을 제외한다.
- entry exterior는 unreserved/unassigned이면 포함한다.
- different-biome distance, altitude, quadrant, perimeter, noise, future route를 base filter에 추가하지 않는다.
- current starter input `149` unassigned 중 non-Core reserved `4`를 제외한 raw universe는 exact `145`다.

accepted Satellite seed sector는 이후 rules/seeds의 global universe에서 제거해 cross-patch overlap을 방지한다.

## Same-Biome Distance / Edge Contract

candidate `c`의 same-biome distance:

```text
SameBiomeDistance(c) = min ManhattanDistance(
    c,
    every sector of existing same-biome Core patch
       union previously accepted same-biome Satellite seed sectors)
```

- exact cardinal open-grid distance이며 blockers를 피해 도는 route distance가 아니다.
- existing same-biome Core patch의 **모든 sector**를 사용한다. Core seed/origin-only를 사용하지 않는다.
- `SameBiomeDistance >= SatelliteRule.MinSeedDistance`여야 한다.
- different-biome Core/Satellite와는 minimum distance를 강제하지 않는다. same sector overlap만 금지한다.
- previously accepted same-biome Satellite seed가 다음 same-biome seed의 distance source가 된다.

`CanTouchWorldEdge == false` rule은 `x==0 || x==12 || y==0 || y==12` candidate를 `WorldEdgeForbidden`으로 거부한다. true rule은 edge candidate를 허용한다. wrap/clamp/diagonal edge 규칙이 없다.

## Exact Individual Seed Redraw Algorithm

counts가 고정된 뒤 rule order, `satelliteOrdinal 0..desired-1` 순서로 each seed를 배치한다.

1. current global raw universe에서 already accepted seed sector를 제외한다.
2. current rule에서 이미 영구 reject된 sector를 제외한 ascending candidate pool을 만든다.
3. attempt마다 `roll = biomePatchRng.NextInt(pool.Count)` 한 번을 호출한다.
4. `candidate = pool[roll]`을 꺼내고 current rule pool에서 즉시 제거한다.
5. first-failure precedence로 edge, same-biome distance를 검사한다.
6. reject이면 current rule permanent rejected set에 넣고 **현재 seed만** redraw한다.
7. accept이면 deterministic PatchId/one-cell Satellite patch/seed/ownership 계획을 기록하고 다음 seed로 간다.

first-failure order:

```text
1. WorldEdgeForbidden
2. SameBiomeDistanceTooSmall
3. Accepted
```

- edge/distance rejection은 same rule에서 이후 seed를 추가해도 valid로 바뀌지 않으므로 rule-local permanent rejected set을 재사용한다.
- other rule은 biome/edge policy가 다르므로 그 rejected set을 공유하지 않는다.
- earlier accepted seed, desired counts, previous rule placements를 candidate rejection 때문에 reroll하거나 이동하지 않는다.
- each requested seed의 attempt limit는 `min(generationProfile.BiomeRetryMax, available pool count at seed start)`이며 starter max exact `100`이다.
- pool empty 또는 limit 내 accept 실패 시 `CandidateAttemptsExhausted`, `RetryRequired`다. no publication, no partial snapshot이다.
- exhaustion 후 count나 prior seed만 별도로 fallback publish하지 않는다. caller가 fresh `PASS_BIOME` attempt stream으로 whole pass retry를 결정한다.

이것이 roadmap의 “Satellite seed 하나 실패 시 해당 seed만 재추첨”을 구현한다. individual redraw 한도 내에서 count와 prior accepted seed는 보존하고, 100회 한도 실패에서만 atomic attempt 전체를 거부한다.

## One-Cell Satellite Patch Construction

accepted record마다:

```text
Patch.Id             = SatellitePatchIdFactory result
Patch.BiomeId        = rule.BiomeId
Patch.PatchRuleId    = rule.PatchRuleId
Patch.Role           = Satellite
Patch.Seeds          = one Satellite seed at selected sector, source site null
Patch.SectorIndices  = selected sector only
Ownership.Primary    = rule.BiomeId
Ownership.Secondary  = empty
Ownership.PatchId    = generated Satellite PatchId
```

- SiteBinding을 만들지 않는다.
- existing Core patch membership/ownership을 다시 쓰지 않는다.
- one-cell Satellite는 transient partial seed state다. MAP04_05가 `MinSectorCount` 이상 성장시키지 못하면 later attempt가 실패해야 한다.
- this Task에서 capacity flood, growth frontier, minimum-size reservation을 하지 않는다.

## `SatelliteSeedPlacementRecord` Contract

accepted seed immutable evidence:

```text
string PatchRuleId
string BiomeId
int SatelliteOrdinal
BiomePatchId PatchId
int SectorIndex
SectorCoord Sector
int SameBiomeDistance
int MinimumSeedDistance
int CandidateRoll
int AttemptCount
int EdgeRejectionCount
int DistanceRejectionCount
```

- canonical order는 PatchRuleId, SatelliteOrdinal이다.
- patch ID/sector unique, source reservation absent, role Satellite다.
- counts/roll/range/distance를 constructor에서 재검증한다.

`SatelliteRulePlacementDiagnostics`는 `SatelliteSeedPlacementDiagnostics.cs`에 함께 두며 아래 immutable facts를 가진다.

```text
string PatchRuleId
string BiomeId
int CountRoll
int DesiredSeedCount
int AcceptedSeedCount
int CandidateMethodCallCount
int CandidateAttemptCount
int EdgeRejectionCount
int DistanceRejectionCount
bool Exhausted
int FailedSatelliteOrdinal
```

`CountRoll`은 `NextInt(SeedCountMin, SeedCountMax + 1)`이 직접 반환한 desired count와 exact 같으며 별도 raw RNG value가 아니다.

## `SatelliteSeedPlacementDiagnostics` Contract

```text
ulong WorldSeed
IReadOnlyList<SatelliteRulePlacementDiagnostics> Rules
IReadOnlyList<SatelliteSeedPlacementRecord> Records
int RawCandidateSectorCount
int CountMethodCallCount
int CandidateMethodCallCount
int TotalRngMethodCallCount
ulong RngDrawCountBefore
ulong RngDrawCountAfter
int DesiredSatelliteSeedCount
int PlacedSatelliteSeedCount
int InitialPatchCount
int InitialAssignedSectorCount
int FinalPatchCount
int FinalAssignedSectorCount
int FinalUnassignedSectorCount
int ReservationIntrusionCount
int PatchOverlapCount
```

- success Rules/Records는 canonical order, copied read-only다.
- success count method calls exact `4`; candidate method calls는 attempt count total이다.
- raw draw delta는 rejection sampling이 실제 소비한 `NextUInt64` count다.
- success conservation: `InitialAssigned + Placed == FinalAssigned`, `FinalAssigned + FinalUnassigned == 169`.
- success reservation intrusion/patch overlap exact `0/0`이다.
- RetryRequired는 Records empty, `FinalPatchCount == InitialPatchCount`, final assigned/unassigned는 rollback input counts다. rule attempt/rejection facts와 failed ordinal만 보존하고 partial patch/sector plan을 노출하지 않는다.

## `SatelliteSeedPlacementPublication` Contract

sealed immutable output envelope:

```text
CorePatchGrowthPublication SourceGrowth
SiteReservationSnapshot SourceSiteSnapshot
BiomePatchSnapshot Snapshot
IReadOnlyList<SatelliteSeedPlacementRecord> SatelliteSeeds
int CorePatchCount
int SatellitePatchCount
int TotalPatchCount
int CoreSiteBindingCount
int AssignedSectorCount
int UnassignedSectorCount
```

- `SourceGrowth`와 `SourceSiteSnapshot` reference identity를 exact 보존한다.
- source/input/output world seed exact same이다.
- exact four Core patch/seed/binding과 all Core sectors를 unchanged 보존한다.
- each record↔Satellite patch↔seed↔ownership을 bidirectionally 재검증한다.
- public mutable dictionary/list/setter/field를 노출하지 않는다.

## Error / Result Contract

spatial failure code frozen continuation:

```text
CandidateAttemptsExhausted
```

candidate rejection reason exact order:

```text
WorldEdgeForbidden
SameBiomeDistanceTooSmall
```

`SatelliteSeedPlacementError` immutable fields:

```text
SatelliteSeedPlacementErrorCode Code
string DefinitionId
string BiomeId
int SatelliteOrdinal
int SectorIndex
int RequiredCount
int AvailableCount
int Shortfall
string Message
```

- unknown identity empty, unknown ordinal/sector `-1`이다.
- counts non-negative, `Shortfall == max(0, RequiredCount - AvailableCount)`다.
- code, definition ID, biome ID, ordinal, sector, counts, message ordinal로 sort/dedupe한다.
- message는 stable non-empty이며 path/stack/time/thread/culture exception text를 포함하지 않는다.

`SatelliteSeedPlacementStatus` exact values:

```text
Completed
InvalidInput
RetryRequired
```

`SatelliteSeedPlacementResult` immutable properties:

```text
SatelliteSeedPlacementStatus Status
bool Succeeded
bool RetryRequired
SatelliteSeedPlacementPublication Publication
SatelliteSeedPlacementDiagnostics Diagnostics
IReadOnlyList<SatelliteSeedPlacementError> Errors
```

- `Completed`: publication/diagnostics non-null, errors `0`, retry false.
- `InvalidInput`: publication/diagnostics null, errors `>=1`, retry false, RNG delta `0`.
- `RetryRequired`: publication null, diagnostics non-null, exhaustion errors `>=1`, retry true.
- desired total 0은 valid `Completed`이며 output snapshot은 logically input-equivalent new immutable snapshot, candidate draws `0`이다.
- unexpected invariant/overflow/model construction failure는 stable `InternalInvariantViolation` 하나로 atomic invalid failure하며 exception text나 partial output을 노출하지 않는다.

## Snapshot Cross-Consistency

successful output은 MAP04_01~03 invariant를 모두 유지한다.

- exact 169 ownership rows/index-coordinate identity
- Patches PatchId ordinal, sectors index, bindings source ID canonical order
- every patch sector↔ownership exact bidirectional membership
- Core patches/seeds/bindings/records unchanged
- Satellite patch exact one seed/sector, role Satellite, source site null, no binding
- every Satellite row has matching PrimaryBiomeId/PatchId and empty SecondaryBiomeId
- orphan seed/binding/ownership/patch sector `0`
- patch overlap/reservation intrusion `0`
- all remaining rows fully unassigned
- `Assigned + Unassigned == 169`
- `IsComplete == false`

## Exact Range / Known-Vector Evidence

current starter constraints:

```text
Initial Core patches / assigned / unassigned = 4 / 20 / 149
Raw unassigned+unreserved candidates         = 145
Satellite desired total range                = 0..11
Final total patch range                      = 4..15
Final assigned range                         = 20..31
Final unassigned range                       = 149..138
Core mutations                               = 0
Reservation intrusion / overlap              = 0 / 0
```

Known RNG count vector fixture is exact `CRATER/DOUGH/MILL/ROOT = 2/0/2/3`. Candidate positions/attempt counts are derived from the synthetic or starter snapshot and must be reported by the actual Result; production에 임의 expected coordinates를 hard-code하지 않는다.

## Determinism / Immutability

same growth input + same fresh RNG initial state는 fresh/reused placer, reversed/shuffled definition enumeration, different culture/time/thread에서 byte-equivalent logical snapshot/records/diagnostics and exact same RNG after state를 만든다.

- no random GUID/timestamp/hash-set enumeration tie-break
- no separate `System.Random`/Unity RNG
- no mutable static cache/counter/current publication
- caller collection/list ownership leakage 없음
- failed call 뒤 same instance valid call은 fresh instance와 동일
- input growth/source/snapshot/definitions의 observable state와 order는 call 전후 exact same
- extra draws in another independent stream do not alter result

## Required Focused Tests

`SatelliteSeedPlacerTests.cs`에 최소 `136`개 실제 NUnit case를 만든다.

필수 coverage:

1. exact four Satellite rules/table/canonical rule order
2. PatchId factory grammar, D2 ordinal `00/01/99`, invalids, culture
3. inclusive count endpoints/ranges and count-first exact four-call schedule
4. known RNG count vector `2/0/2/3`, raw DrawCount `4`
5. zero desired count valid completion/candidate draw zero
6. total range `0..11`, patch/assigned/unassigned conservation
7. starter raw candidate count exact `145`
8. Core sectors, patches, seeds, bindings unchanged
9. P01 Start/Boss/Village/Core reservations excluded
10. unreserved entry exterior included
11. same-biome distance uses every Core patch sector, not Core seed/origin only
12. prior same-biome Satellite seeds enter distance source
13. distance exact equal min accepted, min-1 rejected
14. different-biome adjacency allowed, global sector overlap forbidden
15. touch-edge true allowed; false all four edges/corners rejected
16. rule-local rejected reuse and no cross-rule rejected leakage
17. individual candidate redraw preserves counts/prior seeds
18. candidate roll remove-without-replacement and attempt limit exact
19. exhaustion RetryRequired, empty records, rollback conservation, no publication
20. invalid input accumulates/sorts/dedupes and consumes RNG 0
21. missing/duplicate/unexpected biome/rule, wrong role/identity/ranges
22. patch-count limit and RNG draw-count precondition
23. one-cell Satellite seed/source-null/no-binding partial snapshot consistency
24. caller collection/list immutability and shuffled enumeration
25. same stream state/input determinism and independent-stream isolation
26. forbidden Unity/SystemRandom/time/filesystem/root/pass/static mutable dependency scan

fixture는 installed Authoring CSV body를 읽거나 reflection으로 private invariant를 우회하지 않는다. existing public constructors/factories와 typed in-memory definitions, actual deterministic RNG factory를 사용한다.

## Required Verification — Reduced but Explicit

실제로 실행:

```text
SatelliteSeedPlacerTests                 >=136 PASS
CorePatchGrowerTests                       127/127 PASS
CorePatchSeedInitializerTests              121/121 PASS
BiomePatchModelsTests                      107/107 PASS
DeterministicRngStreamTests                103/103 PASS
Required regression total                  458/458 PASS
Actually executed required total          >=594 PASS
Failed / skipped                             0 / 0
```

large suites는 사용자 지정 감축 profile에 따라 실행하지 않고 discovery/arithmetic로만 확인한다.

```text
Game.Map targeted discovery arithmetic  >=4340
Full EditMode current discovery          >=4409
```

Result에서 actually executed PASS와 discovery-only count를 별도 항목으로 기록한다. targeted/full을 실제 실행하지 않았다면 `PASS`라고 쓰지 않는다. test selection이 의도 fixture와 달랐거나 result evidence가 없는 job은 PASS 합계에 포함하지 않는다.

Unity forced compile 후:

```text
compile errors         = 0
Console errors         = 0
relevant new warnings  = 0
```

## Asset / Change Gates

```text
Baseline Assets meta: 3093
New Runtime C#: 7
New test C#: 1
New matching meta: 8
Final Assets meta: 3101
Exact Assets changes after .APPLIED: 16
Existing Assets modifications: 0
Unexpected Assets changes: 0
Authoring CSV/meta: 50/50 unchanged
Accepted legacy Editor folder meta: 6/6 unchanged
Duplicate GUID groups: 0
```

각 신규 `.cs.meta`는 `fileFormatVersion: 2`, valid unique non-zero lowercase 32-hex GUID, `MonoImporter`여야 한다. Unity가 unrelated folder meta를 만들면 baseline 여부를 확인하고 이 Task가 만든 unexpected file은 제거한다. existing legacy Editor folder meta 6개는 삭제·재생성하지 않는다.

## Result File Contract

`REPORTS/MAP04_04_IMPLEMENT_SATELLITE_SEED_PLACER_RESULT.md` 첫 부분:

```text
# MAP04_04 — Implement Satellite Seed Placer Result

STATUS: PASS
```

Result는 최소 아래를 포함한다.

- PATCH APPLY와 `.APPLIED` hash
- READ/WRITE allowlist 준수
- CREATED/MODIFIED/PREEXISTING_IDENTICAL
- 7 runtime + 1 test exact path와 matching meta/GUID
- exact rule order/count rolls/RNG before-after/method/raw draw evidence
- actual starter desired counts, PatchIds, sectors, attempts, distances
- raw candidates and edge/distance rejection counts
- Core preservation, reservation intrusion/overlap, snapshot conservation
- individual redraw/exhaustion atomic rollback evidence
- actual focused/regression counts와 job IDs
- targeted/full discovery는 executed PASS와 분리
- compile/Console/meta/GUID/Authoring/change-scope evidence
- OUT_OF_SCOPE_FINDINGS
- NEXT: MAP04_04만 COMPLETE/Current Task NONE, MAP04_05 LOCKED

어느 gate든 실패하거나 current project API가 frozen contract와 양립할 수 없으면 `STATUS: BLOCKED` 또는 `STATUS: FAIL`을 정확히 기록한다. 기존 MAP04_01~03 파일을 repair하거나 partial output을 finalize하지 않는다.

## STATUS FINALIZE

전부 PASS일 때만:

```text
MAP04_04_IMPLEMENT_SATELLITE_SEED_PLACER: COMPLETE
Current Task: NONE
Last Completed Task: MAP04_04_IMPLEMENT_SATELLITE_SEED_PLACER
Last Result: REPORTS/MAP04_04_IMPLEMENT_SATELLITE_SEED_PLACER_RESULT.md / STATUS: PASS
MAP04_05_IMPLEMENT_MULTI_SEED_BIOME_GROWER: LOCKED
```

MAP04_05를 자동 시작하거나 Task 파일을 만들지 않는다.

## DO NOT

- existing C#/meta/asmdef/asmref/CSV/Scene/Prefab 수정 금지
- MAP04_01~03 convenience 수정 금지
- Core sector/seed/binding overwrite 금지
- reserved or already assigned sector Satellite seed 금지
- different-biome distance policy 임의 추가 금지
- count draw와 candidate draw interleave 금지
- rejected candidate에서 count/prior seed reroll 금지
- random/timestamp/coordinate-derived PatchId 금지
- Satellite patch MinSectorCount growth 금지
- remaining unassigned sector ownership/cost/altitude/noise/perimeter growth 금지
- Intrusion/cleanup/serializer/generated CSV/file I/O 금지
- validator/overlay/EditorWindow/Gizmo/menu 금지
- `PASS_BIOME` adapter/root/artifact transaction/retry loop 금지
- Git commit/push 금지

## Recommended Commit

```text
feat(map): place deterministic satellite biome seeds
```
