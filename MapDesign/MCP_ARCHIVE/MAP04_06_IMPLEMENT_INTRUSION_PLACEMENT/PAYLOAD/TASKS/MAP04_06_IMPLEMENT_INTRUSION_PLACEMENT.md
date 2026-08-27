# MAP04_06 — Implement Intrusion Placement

```yaml
status_control:
  task_key: MAP04_06_IMPLEMENT_INTRUSION_PLACEMENT
  result_file: REPORTS/MAP04_06_IMPLEMENT_INTRUSION_PLACEMENT_RESULT.md
```

## TASK TYPE

```text
RUNTIME DETERMINISTIC P02 ALLOWED-PAIR ONE-CELL INTRUSION TRANSFER + EDITMODE TESTS
```

## Objective

MAP04_05의 successful immutable grown `BiomePatchSnapshot`과 같은 `PASS_BIOME` attempt의 continued `RNG_BIOME_PATCH`를 입력으로 받아 active Root/Mill Intrusion count를 count-first로 추첨한다. 그런 다음 active boundary pair가 `TUNNEL_INTRUSION`을 허용하는 actual inter-biome cardinal boundary의 host-side sector만 one-cell `BiomePatchRole.Intrusion` patch로 transfer한다.

```text
Input viable fixture = 14 Core/Satellite patches
Assigned/unassigned  = 165 / 4 reserved
Input RNG DrawCount  = 1907
Intrusion rules      = exact 2 (MILL / ROOT)
Intrusion count      = each 0..2 inclusive
Output new patches   = 0..4, each exact one cell
Assigned/unassigned  = unchanged 165 / 4
```

candidate sector는 unreserved, non-edge, non-seed, non-site-binding이어야 하며 donor Core/Satellite patch에서 제거해도 rule minimum과 cardinal connectivity를 유지해야 한다. 새 Intrusion seed는 source site가 없고, 새 patch는 exact one sector만 소유한다.

성공한 경우에만 새 immutable snapshot을 atomic publish한다. source MAP04_05 result/publication/P01/P02 object와 caller definitions는 mutate하지 않는다. general Core/Satellite one-cell patch를 만들지 않고 explicit `AllowSingleSector == true` Intrusion만 one-cell을 허용한다.

이 Task는 Intrusion placement만 한다. Core/Satellite 재성장, Intrusion multi-cell growth, cleanup, checkerboard/neck 후처리, export, final validator, overlay, `PASS_BIOME` root adapter/retry loop는 구현하지 않는다.

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
12. `REPORTS/MAP04_05_IMPLEMENT_MULTI_SEED_BIOME_GROWER_RESULT.md`

MAP04_05 Result에서 아래 actual evidence를 확인한다.

```text
STATUS: PASS
MultiSeedBiomeGrowerTests: 164/164 PASS
Required regressions: 478/478 PASS
Actually executed required total: 642/642 PASS
Attempt-0 capacity target/legal/shortfall: 165/161/4, RetryRequired, RNG 13->13
Viable world seed / attempt: 0x0123456789ABCDF9 / 24
Viable Satellite count: CRATER/DOUGH/MILL/ROOT = 3/3/1/3
Viable patches / assigned / reserved-unassigned: 14 / 165 / 4
Viable RNG DrawCount: 17->1907
Patch overlap / disconnected / source mutation: 0 / 0 / 0
Final Assets meta: 3110
Existing / unexpected Assets modifications: 0 / 0
NEXT: MAP04_05 only COMPLETE / Current Task NONE / MAP04_06 remains LOCKED
```

Result SHA-256은 exact 아래다.

```text
ab23a2d0e30cb21df7fca6f098607cf20ccd5a3cc9a9da4f43f8fdb344ba6e2f
```

MAP04_05는 targeted/full을 실행 PASS로 주장하지 않고 discovery/arithmetic `4509/4577`로 분리 기록했다. 이 구분을 보존한다.

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
04_CSV_STARTER/biome_boundary_profiles.csv
04_CSV_STARTER/biome_boundary_pair_rules.csv
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

### Existing MAP04_02~05 — exact relevant all

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchInitializationPublication.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchGrowthPublication.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SatelliteSeedPlacementRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SatelliteSeedPlacementDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SatelliteSeedPlacementPublication.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SatelliteSeedPlacementResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SatelliteSeedPlacer.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomeGrowthCost.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomeGrowthNoiseTable.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MultiSeedBiomeGrowthError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MultiSeedBiomeGrowthRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MultiSeedBiomeGrowthDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MultiSeedBiomeGrowthPublication.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MultiSeedBiomeGrowthResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MultiSeedBiomeGrower.cs
```

MAP04_01~05 checked-in public API가 이 문서의 illustrative signature보다 우선한다. existing constructor/property shape에 맞춰 구현하고 기존 파일을 수정하지 않는다.

### Focused Tests / Assemblies

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/DeterministicRngStreamTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/BiomePatchModelsTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SatelliteSeedPlacerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MultiSeedBiomeGrowerTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

위 C#/asmdef와 matching meta, approved Runtime/Test `Generation` 직계 파일명의 path-only inventory, Authoring CSV/meta count·hash, 전체 meta GUID, task marker 이후 change-scope path만 확인할 수 있다.

금지:

- installed Authoring CSV body 직접 재파싱·수정
- MAP04_07 이후 Task body
- unrelated production/test C# body
- Legacy/Stage/P6/P11 generator body
- Scene/Prefab YAML
- MAP04_01~05 existing production/test 수정

## WRITE ALLOWLIST

### 신규 Runtime production C# — exact 7

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/IntrusionPatchIdFactory.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/IntrusionPlacementError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/IntrusionPlacementRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/IntrusionPlacementDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/IntrusionPlacementPublication.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/IntrusionPlacementResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/IntrusionPlacer.cs
```

### 신규 Runtime EditMode test — exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/IntrusionPlacerTests.cs
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

## Frozen P02 Intrusion Boundary

```text
Input envelope       = successful MAP04_05 MultiSeedBiomeGrowthResult
Input artifact       = MultiSeedBiomeGrowthPublication
Source reservation   = immutable SiteReservationSnapshot
Output artifact      = Core + Satellite + Intrusion partial BiomePatchSnapshot
Pass ID              = PASS_BIOME (not executed here)
RNG stream           = continued same-attempt RNG_BIOME_PATCH
Grid                 = 13 x 13 / 169 sectors / index y*13+x
New role             = INTRUSION only
Intrusion patch size = exact 1
SecondaryBiomeId     = always empty
```

- source result/publication/P01/input snapshot/definitions/patches/ownership/bindings/seeds를 mutate하지 않는다.
- output은 새 `BiomePatchSnapshot`이며 same world seed, same P01 reference, same existing patch IDs/seeds/Core bindings을 보존한다.
- selected donor sector만 output에서 old Core/Satellite patch membership·ownership을 빼고 새 Intrusion patch membership·ownership으로 exact transfer한다.
- input/source row는 그 자체로 immutable이며 observable source mutation은 `0`이다.
- nonselected row, nonselected patch, all P01 reservations, all seeds, all Core bindings은 logical identity를 보존한다.
- assigned/unassigned count는 transfer 전후 동일하다. viable fixture는 `165/4`, `IsComplete == false`를 유지한다.

## Exact Active Intrusion Rules

PatchRuleId ordinal order는 exact `MILL`, `ROOT`다.

| Order | Rule | Biome | Min/max sectors | Min seed distance | Count min/max | Weight | Edge | Single | Intrusion share |
|---:|---|---|---:|---:|---:|---:|---|---|---:|
| 0 | `PATCH_MILL_INTRUSION` | `BIO_ABANDONED_MILL` | `1/4` | 2 | `0/2` | 15 | false | true | 0.10 |
| 1 | `PATCH_ROOT_INTRUSION` | `BIO_CASSIA_ROOT` | `1/5` | 2 | `0/2` | 20 | false | true | 0.10 |

- exact two active Intrusion rules와 prior Core/Satellite donor rule exact eight을 요구한다.
- `AllowSingleSector == true`, `MinSectorCount == 1`, `CanTouchWorldEdge == false`, `BufferRingSectors == 0`을 요구한다.
- `MaxSectorCount` 4/5는 rule validity fact이지 one Intrusion patch를 그 크기까지 성장시키는 지시가 아니다. 이 Task의 every Intrusion patch는 exact one cell이다.
- `SeedWeight`, cost/altitude/noise/compactness/branchiness weights는 positive/finite typed facts로 validate하지만 count/candidate weighting에 임의로 쓰지 않는다.
- biome type `MinPatchCount/MaxPatchCount`는 Core/Satellite normal patch count에 대한 규칙이며 explicit Intrusion patch는 별도로 계수한다. 그렇지 않으면 Root `Core 1 + Satellite 3 + Intrusion 0..2`가 starter schema에서 자체 모순이 된다.
- intrusion role share cap은 `floor(169 * 0.10) == 16` cell을 별도로 적용한다. 전체 Root/Mill primary-biome share에는 donor transfer 후 normal biome cap도 계속 적용한다.

## Exact Allowed Pair Derivation

active boundary profile `BOUND_TUNNEL`의 type은 exact `TUNNEL_INTRUSION`이다. active pair rule의 `AllowedBoundaryProfileIds`에 그 profile ID가 있고, pair의 두 biome 중 하나가 Intrusion rule biome이며 other biome이 current host ownership일 때만 relation이 허용된다.

| Intruder | Host | Pair rule | Allowed |
|---|---|---|---|
| ROOT | CRATER | `PAIR_CRATER_ROOT` | yes |
| ROOT | MILL | `PAIR_ROOT_MILL` | yes |
| ROOT | DOUGH | `PAIR_ROOT_DOUGH` | yes |
| MILL | ROOT | `PAIR_ROOT_MILL` | yes |
| MILL | DOUGH | `PAIR_MILL_DOUGH` | yes |
| MILL | CRATER | `PAIR_CRATER_MILL` | **no** (`BOUND_TUNNEL` absent) |

- same-biome intrusion은 허용하지 않는다.
- `PAIR_CRATER_DOUGH`는 Intrusion rule biome가 없어 허용하지 않는다.
- pair row를 reverse-generated/canonicalized하지 않고 supplied pair identity를 그대로 record한다. relation 판정만 BiomeA/B 중 intruder/host membership을 양방향으로 검사한다.
- profile display name, notes, chunk catalog, default profile, resource/element pool을 heuristic으로 쓰지 않는다.

## `IntrusionPatchIdFactory` Contract

ID는 intruder biome identity와 zero-based same-biome Intrusion ordinal로만 결정한다.

```text
PATCHINST_INTR_<BIOME_ID>_<ORDINAL_D2>
```

exact examples:

```text
PATCHINST_INTR_BIO_ABANDONED_MILL_00
PATCHINST_INTR_BIO_CASSIA_ROOT_00
PATCHINST_INTR_BIO_CASSIA_ROOT_01
```

public stateless API:

```text
BiomePatchId Create(string biomeId, int intrusionOrdinal)
bool TryCreate(string biomeId, int intrusionOrdinal, out BiomePatchId patchId)
```

- ordinal exact `0..99`, ASCII decimal two digits다.
- invalid biome ID/ordinal을 거부한다.
- host biome, pair ID, sector, RNG output, attempt, timestamp, culture를 ID에 넣지 않는다.
- trim/case-fold/Unicode normalization/static mutable cache를 사용하지 않는다.

## Public Placer API

```text
public sealed class IntrusionPlacer

IntrusionPlacementResult Place(
    MultiSeedBiomeGrowthResult growthResult,
    GenerationProfileDefinition generationProfile,
    IEnumerable<BiomeTypeDefinition> biomeTypes,
    IEnumerable<BiomePatchRuleDefinition> allPatchRules,
    IEnumerable<BiomeBoundaryProfileDefinition> boundaryProfiles,
    IEnumerable<BiomeBoundaryPairRuleDefinition> boundaryPairRules,
    DeterministicRngStream biomePatchRng)
```

- `growthResult.Status == Completed`, publication/diagnostics non-null을 요구한다.
- `biomePatchRng.DrawCount == growthResult.Diagnostics.RngDrawCountAfter`여야 한다.
- actual viable fixture before DrawCount는 `1907`이다. generic successful growth의 actual after count를 사용하고 `1907`을 production에 hard-code하지 않는다.
- stream ID/initial state가 public shape에 없다면 reflection으로 추정하지 않고 actual factory+same-instance integration test로 증명한다.
- Registry singleton, CSV/file, clock, current Root/pass state, retry loop를 자체 조회하지 않는다.

## Structural Preflight

RNG draw 전에 가능한 structural error를 accumulated/sorted/deduped validation한다.

- result/publication/diagnostics/source chain/site snapshot/input snapshot/collections/items non-null
- `Completed`, world seed/reference linkage, growth diagnostic count/draw conservation
- exact 169 rows/index-coordinate identity, `165 assigned / 4 non-Core reserved-unassigned` generic invariant, empty SecondaryBiome
- exact four Core, zero existing Intrusion, remaining patches Satellite
- every patch/seed/binding/ownership linkage bidirectional and role/rule/biome identity consistent
- all Core/Satellite patches cardinal connected and their rule minimum/maximum/hard 59 valid
- all unassigned rows exact non-Core P01 reserved footprint and all unreserved rows assigned
- exact required active four biome definitions, exact ten active patch rules
- exact active boundary profile six and boundary pair rules six with unique IDs
- `BOUND_TUNNEL` exists/active/type `TUNNEL_INTRUSION`; pair allowed lists/weights/default identity valid
- exact two Intrusion rules table, all donor rules valid, finite values/share/ranges valid
- generation profile active `GEN_MOONPALACE_V1`, matching profile, `BiomeRetryMax == 100`
- continued RNG non-null and exact DrawCount precondition
- undefined enum/numeric cast, duplicate ID/index, lazy/caller mutation dependency rejection

structural invalid input은 RNG delta `0`, `InvalidInput`, retry false, publication/diagnostics null, stable errors `>=1`을 반환한다.

`IntrusionPlacementErrorCode` structural frozen order:

```text
MissingGrowthResult
GrowthNotCompleted
MissingGrowthPublication
MissingGrowthDiagnostics
InvalidGrowthPublication
InvalidSourceSiteSnapshot
MissingGenerationProfile
InvalidGenerationProfile
MissingBiomeTypes
MissingPatchRules
MissingBoundaryProfiles
MissingBoundaryPairRules
NullDefinition
DuplicateDefinitionId
MissingBiomeDefinition
UnexpectedBiomeDefinition
MissingPatchRule
UnexpectedPatchRule
MissingBoundaryProfile
UnexpectedBoundaryProfile
MissingBoundaryPairRule
UnexpectedBoundaryPairRule
InvalidBiomeDefinition
InvalidPatchRule
InvalidBoundaryProfile
InvalidBoundaryPairRule
DefinitionIdentityMismatch
InvalidPatchState
InvalidReservationState
MissingBiomePatchRng
InvalidBiomePatchRngState
InternalInvariantViolation
```

## Exact Count-First RNG Schedule

structural preflight 성공 후 candidate를 보기 전 exact two count를 모두 먼저 추첨한다.

```text
for rule in PatchRuleId ordinal (MILL, ROOT):
    desired = biomePatchRng.NextInt(
        rule.SeedCountMin,
        rule.SeedCountMax + 1)
```

- inclusive `[min,max]`를 existing unbiased half-open API로 구현한다.
- exact two rule는 desired 0이어도 each one method call을 사용한다.
- count를 candidate draw 사이에 넣거나 `%`, float scaling, SeedWeight를 쓰지 않는다.
- count 완료 후 rule ordinal, intrusion ordinal 순서로 candidate selection을 한다.

actual viable factory fixture known vector:

```text
World seed / PASS_BIOME attempt = 0x0123456789ABCDF9 / 24
Input DrawCount                  = 1907
Rule order                       = MILL / ROOT
Desired counts                   = 1 / 2
Count NextUInt64 outputs         = CB8386606F087EA4 / 9018672136A34305
Count method/raw draws           = 2 / 2
DrawCount after counts           = 1909
```

production에 vector lookup table를 만들지 않고 actual factory/stream으로 계산한다.

## Exact Candidate Contract

each desired intrusion placement 직전 current temporary snapshot에서 candidate를 새로 열거한다. candidate는 모든 조건을 만족해야 한다.

1. sector index/coord valid, currently assigned, P01 unreserved
2. current owner patch role exact Core or Satellite; Intrusion을 host/anchor로 쓰지 않음
3. host biome != intruder biome
4. sector가 world edge `x==0 || x==12 || y==0 || y==12`가 아님
5. sector가 any Core/Satellite seed가 아님
6. sector가 any Core site binding footprint cell이 아님
7. host donor patch에서 제거 후 `SectorCount >= donorRule.MinSectorCount`
8. donor 제거 후 remaining sectors가 non-empty cardinal connected
9. donor의 모든 seed/binding cell이 remaining membership에 존재
10. active pair relation이 host/intruder를 포함하고 `BOUND_TUNNEL` allowed
11. candidate에 intruder biome의 **source Core/Satellite** sector cardinal neighbor가 pair `MinSharedEdgeCount` 이상
12. same intrusion rule의 prior accepted seed와 Manhattan distance `>= MinSeedDistance`
13. transfer 후 intruder primary-biome total이 normal biome world-share cap 이하
14. same intruder role의 total Intrusion sectors가 intrusion share cap 이하
15. candidate를 이전 Intrusion에서 연쇄적으로 파생한 boundary로 인정하지 않음

cardinal neighbor order는 exact `L,R,U,D` existing grid contract다. qualifying anchor가 여러 개면 smallest `SectorIndex`를 record evidence로 선택하지만 candidate eligibility/winner를 추가 가중하지 않는다.

donor connectivity는 candidate를 제거한 remaining membership에서 smallest index BFS로 계산한다. diagonal, wrap, clamp, seed-only shortcut, bounding box를 쓰지 않는다.

## Candidate Selection / Atomic Algorithm

```text
1. structural preflight
2. MILL/ROOT desired counts first
3. temporary deep logical working state
4. for rule in MILL/ROOT ordinal:
5.   for intrusionOrdinal in 0..desired-1:
6.     enumerate all currently legal candidates
7.     sort by SectorIndex ascending
8.     if empty: retry-required and rollback
9.     selected = candidates[biomePatchRng.NextInt(candidates.Count)]
10.    transfer selected host ownership into a new one-cell Intrusion patch
11. publish only after every desired placement succeeds
```

- each placement은 candidate method call exact one이다. invalid candidate를 draw 후 rejection/redraw하지 않고 먼저 deterministic filter한다.
- desired total 0은 valid Completed이며 count draws 2, candidate draws 0, logically input-equivalent new snapshot을 publish한다.
- chosen first intrusion으로 later candidate universe가 바뀐다. same-rule distance, donor size/connectivity, ownership conflict를 current temporary state에서 재계산한다.
- prior accepted Intrusion patch는 host도 anchor도 아니므로 one-cell intrusion chain/growth를 만들지 않는다.
- spatial exhaustion은 count와 already-used candidate RNG facts을 diagnostics에 보존하지만 publication/records/partial patch는 노출하지 않고 source counts로 rollback한다.

actual viable fixture에서 count `1+2=3`이 모두 배치되면 candidate method/raw draws `3/3`, total method/raw draws `5/5`, DrawCount `1907->1912`, patch count `14->17`이다. candidate counts/selected coordinates/host patches/pairs은 actual public state에서 계산해 Result에 기록하고 production에 hard-code하지 않는다.

## Exact One-Cell Transfer

selected sector에 대해 output을 이렇게 만든다.

```text
Old donor patch: same ID/biome/rule/role/seeds, SectorIndices - selected
New patch ID:    PATCHINST_INTR_<INTRUDER_BIOME>_<D2>
New patch biome: intrusion rule BiomeId
New patch rule:  intrusion PatchRuleId
New patch role:  Intrusion
New patch seed:  selected sector / Role Intrusion / SourceSiteReservationId null
New patch cells: exact selected sector one
New ownership:   assigned / PrimaryBiomeId intruder / SecondaryBiomeId empty / new PatchId
```

- site binding을 새로 만들지 않는다.
- selected old ownership은 output에 존재하지 않고 new ownership exact one만 존재한다.
- source old patch/ownership object는 mutate하지 않는다.
- Intrusion patch sector count/seed count exact `1/1`, `AllowSingleSector==true`다.
- normal Core/Satellite patch가 one-cell이 되는 transfer는 donor minimum gate에서 거부한다.
- assigned/unassigned conservation, patch↔ownership bidirectional consistency, empty SecondaryBiome를 유지한다.

## Record / Diagnostics Contract

each immutable `IntrusionPlacementRecord`:

```text
int Sequence
string IntrusionRuleId
string IntruderBiomeId
int IntrusionOrdinal
BiomePatchId IntrusionPatchId
int SectorIndex
SectorCoord Coordinate
string HostBiomeId
BiomePatchId DonorPatchId
BiomePatchRole DonorRole
int DonorSizeBefore
int DonorSizeAfter
string BoundaryPairRuleId
string BoundaryProfileId
int SharedIntruderEdgeCount
int AnchorSectorIndex
int CandidateCountBeforeDraw
int CandidateRoll
int SameRuleNearestIntrusionDistance
```

nearest distance가 아직 없으면 `-1`, known anchor/candidate/roll/count는 non-negative다. records는 rule ordinal, Intrusion ordinal/sequence 순 copied read-only다.

`IntrusionPlacementDiagnostics`는 최소 아래를 보존한다.

```text
ulong WorldSeed
IReadOnlyList<IntrusionRulePlacementDiagnostics> Rules
IReadOnlyList<IntrusionPlacementRecord> Records
int InitialPatchCount
int InitialAssignedSectorCount
int InitialUnassignedSectorCount
int DesiredIntrusionCount
int PlacedIntrusionCount
int FinalPatchCount
int FinalAssignedSectorCount
int FinalUnassignedSectorCount
int CountMethodCallCount
int CandidateMethodCallCount
int TotalRngMethodCallCount
ulong RngDrawCountBefore
ulong RngDrawCountAfter
int DonorMinimumViolationCount
int DonorDisconnectCount
int ProtectedCellTransferCount
int DisallowedPairCount
int ReservationIntrusionCount
int PatchOverlapCount
```

success conservation:

```text
FinalPatchCount = InitialPatchCount + PlacedIntrusionCount
FinalAssigned   = InitialAssigned
FinalUnassigned = InitialUnassigned
Count calls     = 2
Candidate calls = PlacedIntrusionCount
all violation counters = 0
```

RetryRequired는 publication null, records empty, final counts rollback input counts다. per-rule desired/attempted/candidate count, failed ordinal, actual RNG before/after는 diagnostics에 보존한다.

## Error / Result Contract

spatial failure code frozen continuation:

```text
NoLegalIntrusionCandidate
```

candidate rejection reason exact order:

```text
ReservedSector
WorldEdgeForbidden
ProtectedSeedSector
ProtectedSiteBindingSector
SameBiomeHost
DisallowedBoundaryPair
MissingIntruderSharedEdge
DonorBelowMinimum
DonorDisconnected
IntrusionSeedDistanceTooSmall
BiomeShareExceeded
IntrusionShareExceeded
```

`IntrusionPlacementError` immutable fields:

```text
IntrusionPlacementErrorCode Code
string DefinitionId
string IntruderBiomeId
string HostBiomeId
int IntrusionOrdinal
int SectorIndex
int RequiredCount
int AvailableCount
int Shortfall
string Message
```

unknown identity empty, unknown ordinal/sector `-1`다. counts non-negative, `Shortfall=max(0,Required-Available)`다. code/definition/intruder/host/ordinal/sector/count/message ordinal로 sort/dedupe한다.

`IntrusionPlacementStatus`:

```text
Completed
InvalidInput
RetryRequired
```

`IntrusionPlacementResult` immutable properties:

```text
IntrusionPlacementStatus Status
bool Succeeded
bool RetryRequired
IntrusionPlacementPublication Publication
IntrusionPlacementDiagnostics Diagnostics
IReadOnlyList<IntrusionPlacementError> Errors
```

- `Completed`: publication/diagnostics non-null, errors `0`, retry false.
- `InvalidInput`: publication/diagnostics null, stable errors `>=1`, retry false, RNG delta `0`.
- `RetryRequired`: publication null, diagnostics non-null, errors `>=1`, retry true.
- unexpected invariant/overflow/model construction failure는 stable `InternalInvariantViolation` 하나로 atomic invalid failure하고 exception text/partial output을 노출하지 않는다.

## Publication / Snapshot Cross-Consistency

`IntrusionPlacementPublication` immutable output:

```text
MultiSeedBiomeGrowthPublication SourceGrowth
SiteReservationSnapshot SourceSiteSnapshot
BiomePatchSnapshot Snapshot
IReadOnlyList<IntrusionPlacementRecord> Intrusions
int CorePatchCount
int SatellitePatchCount
int IntrusionPatchCount
int TotalPatchCount
int CoreSiteBindingCount
int AssignedSectorCount
int UnassignedSectorCount
```

success output은:

- exact 169 ownership rows/index-coordinate identity
- input Core/Satellite patch ID/seed/binding preservation
- selected donor membership decrement and new Intrusion membership exact bidirectional consistency
- every Core/Satellite donor remains cardinal connected and within rule min/max
- every Intrusion patch one seed/one sector/source-null/no binding
- every Intrusion relation allowed pair + actual source Core/Satellite shared edge
- same-rule intrusion seeds distance rule pass
- no reserved/edge/protected cell transfer
- no orphan seed/binding/ownership/patch cell, no overlap
- PrimaryBiome/PatchId exact match, SecondaryBiome empty
- assigned/unassigned `165/4` generic conservation
- source/caller observable mutation `0`

## Determinism / Immutability

same growth input + same continued RNG state는 fresh/reused placer, reversed/shuffled definitions, different culture/time/thread에서 byte-equivalent logical result/publication/records/diagnostics and exact same RNG after state를 만든다.

- no random GUID/timestamp/hash-set enumeration tie-break
- no `System.Random`/Unity RNG/clock/filesystem
- no mutable static cache/counter/current publication
- caller collection/list ownership leakage 없음
- failed call 후 same instance valid call은 fresh instance와 동일
- source result/publication/P01/P02/definitions observable state/order unchanged
- another independent stream extra draws do not alter result

## Required Focused Tests

`IntrusionPlacerTests.cs`에 최소 `150`개 actual NUnit case를 만든다.

필수 coverage:

1. exact two Intrusion rule table/order and exact six profile/pair tables
2. ID factory grammar, D2 ordinal `00/01/99`, invalids, culture
3. structural accumulated/sorted/deduped errors and RNG zero
4. growth Completed/publication/diagnostics and continued DrawCount precondition
5. exact 169/P01/P02/patch↔ownership/source linkage validation
6. exact allowed five directed intrusion relations and rejected MILL→CRATER
7. `BOUND_TUNNEL` active/type/allowed-list derivation, no display/notes heuristic
8. count-first exact two calls, inclusive range/endpoints/zero-count
9. viable known count vector MILL/ROOT `1/2`, raw outputs, DrawCount `1907->1909`
10. count complete before any candidate; candidate failure cannot reroll counts
11. unreserved host-side actual cardinal boundary and MinSharedEdgeCount
12. source Core/Satellite anchor only; prior Intrusion cannot chain/anchor/host
13. candidate SectorIndex canonical order and one unbiased call per placement
14. no invalid-candidate redraw; dynamic candidate recompute after transfer
15. all four world edges/corners rejected for both rules
16. Core/Satellite seed cells and Core binding cells protected
17. P01 Start/Boss/Village/Core reservations protected
18. donor exact-min removal rejected; min+1 accepted
19. articulation-cell removal rejected; leaf/non-articulation accepted
20. donor Core/Satellite role/rule/seed/binding preservation
21. same-rule intrusion distance exact min accepted, min-1 rejected
22. different intrusion rule proximity permitted if all pair rules pass
23. normal biome share/intrusion role share exact boundary and overflow rejection
24. one-cell Intrusion seed/patch/ownership/source-null/no-binding contract
25. selected transfer conservation; nonselected rows/patches unchanged
26. zero desired Completed/input-equivalent snapshot/count draws only
27. candidate exhaustion RetryRequired/RNG evidence/atomic rollback/no partial records
28. viable factory integration: expected count, actual placements/pairs/coords, RNG schedule
29. shuffled definitions/culture/fresh-reused/independent-stream determinism
30. forbidden Unity/SystemRandom/time/filesystem/root/retry/static mutable dependency scan

fixture는 installed Authoring CSV body를 읽거나 reflection으로 private invariant를 우회하지 않는다. checked-in public constructors/factories, typed in-memory definitions, actual deterministic RNG factory를 사용한다.

## Required Verification — Reduced but Explicit

실제로 실행:

```text
IntrusionPlacerTests                       >=150 PASS
MultiSeedBiomeGrowerTests                    164/164 PASS
SatelliteSeedPlacerTests                     141/141 PASS
BiomePatchModelsTests                        107/107 PASS
DeterministicRngStreamTests                  103/103 PASS
Required regression total                    515/515 PASS
Actually executed required total            >=665 PASS
Failed / skipped                               0 / 0
```

large suites는 사용자 지정 감축 profile에 따라 실행하지 않고 discovery/arithmetic로만 확인한다.

```text
Game.Map targeted discovery arithmetic     >=4659
Full EditMode current discovery             >=4727
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
Baseline Assets meta: 3110
New Runtime C#: 7
New test C#: 1
New matching meta: 8
Final Assets meta: 3118
Exact Assets changes after .APPLIED: 16
Existing Assets modifications: 0
Unexpected Assets changes: 0
Authoring CSV/meta: 50/50 unchanged
Accepted legacy Editor folder meta: 6/6 unchanged
Duplicate GUID groups: 0
```

각 신규 `.cs.meta`는 `fileFormatVersion: 2`, valid unique non-zero lowercase 32-hex GUID, `MonoImporter`여야 한다. Unity가 unrelated folder meta를 만들면 baseline 여부를 확인하고 이 Task가 만든 unexpected file은 제거한다. existing legacy Editor folder meta 6개는 삭제·재생성하지 않는다.

## Result File Contract

`REPORTS/MAP04_06_IMPLEMENT_INTRUSION_PLACEMENT_RESULT.md` 첫 부분:

```text
# MAP04_06 — Implement Intrusion Placement Result

STATUS: PASS
```

Result는 최소 아래를 포함한다.

- PATCH APPLY와 `.APPLIED` hash
- READ/WRITE allowlist 준수
- CREATED/MODIFIED/PREEXISTING_IDENTICAL
- 7 runtime + 1 test exact path와 matching meta/GUID
- exact allowed/rejected pair table and profile derivation evidence
- actual viable count vector, raw outputs, RNG before/after/method/raw draws
- per placement candidate count/roll/sector/coord/host/donor/pair/anchor/distance
- donor before/after minimum/connectivity, protected/reserved/edge counters
- patch/assigned/unassigned/biome conservation and source immutability
- zero-count and exhaustion atomic rollback evidence
- actual focused/regression counts와 job IDs
- targeted/full discovery는 executed PASS와 분리
- compile/Console/meta/GUID/Authoring/change-scope evidence
- OUT_OF_SCOPE_FINDINGS
- NEXT: MAP04_06만 COMPLETE/Current Task NONE, MAP04_07 LOCKED

어느 gate든 실패하거나 current public API가 frozen contract와 양립할 수 없으면 `STATUS: BLOCKED` 또는 `STATUS: FAIL`을 정확히 기록한다. MAP04_01~05 파일을 repair하거나 partial output을 finalize하지 않는다.

## STATUS FINALIZE

전부 PASS일 때만:

```text
MAP04_06_IMPLEMENT_INTRUSION_PLACEMENT: COMPLETE
Current Task: NONE
Last Completed Task: MAP04_06_IMPLEMENT_INTRUSION_PLACEMENT
Last Result: REPORTS/MAP04_06_IMPLEMENT_INTRUSION_PLACEMENT_RESULT.md / STATUS: PASS
MAP04_07_IMPLEMENT_PATCH_CLEANUP: LOCKED
```

MAP04_07을 자동 시작하거나 Task 파일을 만들지 않는다.

## DO NOT

- existing C#/meta/asmdef/asmref/CSV/Scene/Prefab 수정 금지
- MAP04_01~05 convenience repair 금지
- arbitrary/non-tunnel/same-biome pair Intrusion 금지
- Intrusion-to-Intrusion chain/anchor/host 금지
- reserved/edge/seed/site-binding sector transfer 금지
- donor minimum 미만/fragmentation transfer 금지
- normal Core/Satellite one-cell patch 생성 금지
- Intrusion patch multi-cell growth 금지
- count/candidate interleave, invalid candidate redraw, float/modulo selection 금지
- source result/publication/snapshot in-place mutation 금지
- cleanup/checkerboard/1-cell neck 후처리 금지
- serializer/generated CSV/file I/O 금지
- final validator/overlay/EditorWindow/Gizmo/menu 금지
- production `PASS_BIOME` adapter/root/retry loop 금지
- Git commit/push 금지

## Recommended Commit

```text
feat(map): place allowed one-cell biome intrusions
```
