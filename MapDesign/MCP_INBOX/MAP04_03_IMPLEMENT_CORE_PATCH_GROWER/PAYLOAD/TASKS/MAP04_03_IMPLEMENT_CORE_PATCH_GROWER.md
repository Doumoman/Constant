# MAP04_03 — Implement Core Patch Grower

```yaml
status_control:
  task_key: MAP04_03_IMPLEMENT_CORE_PATCH_GROWER
  result_file: REPORTS/MAP04_03_IMPLEMENT_CORE_PATCH_GROWER_RESULT.md
```

## TASK TYPE

```text
RUNTIME DETERMINISTIC P02 CORE BUFFER/MINIMUM GROWTH + EDITMODE TESTS
```

## Objective

MAP04_02가 atomic publish한 exact four Core-only partial `BiomePatchSnapshot`을 입력으로 받아, 각 CorePatch를 자기 source footprint의 cardinal buffer와 active Core rule 최소 크기까지 **다른 P01 reservation을 침범하지 않고 우선 성장**시킨다.

```text
initial starter = 4 patches / 4 assigned / 165 unassigned
target starter  = 4 patches / 20 assigned / 149 unassigned
Core targets    = max(full in-world mandatory buffer, rule minimum)
RNG draws       = 0
```

성공한 경우에만 새 immutable partial `BiomePatchSnapshot`을 atomic publish한다. MAP04_02 publication과 source P01 snapshot은 reference identity를 보존하며 mutate하지 않는다.

이번 Task는 Core mandatory growth만 한다. Satellite/Intrusion seed, remaining-world multi-seed cost, altitude/noise weight, RNG, cleanup, generated CSV, final validator, overlay, `PASS_BIOME`/root adapter를 구현하지 않는다.

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
12. `REPORTS/MAP04_02_INITIALIZE_CORE_PATCH_SEEDS_RESULT.md`

MAP04_02 Result에서 아래 실제 evidence를 확인한다.

```text
STATUS: PASS
New CorePatchSeedInitializerTests: 121/121 PASS
Required regressions: 522/522 PASS
Actually executed required total: 643/643 PASS
Starter: 4 patches / 4 bindings / 4 seed cells / 4 assigned / 165 unassigned / RNG 0
Final Assets meta: 3086
Existing / unexpected Assets modifications: 0 / 0
NEXT: MAP04_02 only COMPLETE / Current Task NONE / MAP04_03 remains LOCKED
```

Result SHA-256은 exact 아래다.

```text
d10a2350723ebe2d47b26a89f59d0c605eb242fa9d1fe432e811bb39ee608ee8
```

MAP04_02는 사용자 지정 감축 검증에서 targeted/full을 실행 PASS로 주장하지 않고 discovery/arithmetic `4077/4146`으로 분리 기록했다. 이 구분을 보존하고 미실행 large suite를 prior PASS로 바꾸지 않는다.

## Map Package Reference

Map Package v1.0 exact installed path가 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/01_WORLD_FIXED_SPEC.md
01_FIXED_SPEC/02_COORDINATE_AND_ID_RULES.md
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
01_FIXED_SPEC/04_RUNTIME_ARCHITECTURE.md
01_FIXED_SPEC/05_IMPLEMENTATION_ORDER.md
02_PHASE_ROADMAP/MAP03_SPECIAL_SITE_RESERVATION.md
02_PHASE_ROADMAP/MAP04_BIOME_PATCH_GENERATOR.md
04_CSV_STARTER/biome_types.csv
04_CSV_STARTER/biome_patch_rules.csv
04_CSV_STARTER/generation_profiles.csv
```

reference CSV는 frozen contract 확인용으로만 읽는다. installed Authoring CSV body를 직접 읽거나 재파싱하지 않는다. exact reference가 없으면 이 Task와 existing typed definitions/API를 authoritative fallback으로 사용한다.

## READ ALLOWLIST

### Existing Domain / Typed Definitions

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/BiomeDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/BiomeBoundaryDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/BiomeBoundaryDefinitionSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/StaticDataRegistry.cs
```

### Existing P00 / P01 Models

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGridIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorNeighborIndices.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteFootprint.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreBiomeSeed.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorReservation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityRequirement.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityFloodWitness.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityApproval.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityFloodChecker.cs
```

MAP03 capacity artifacts는 independent regression/evidence 비교용이다. 새 grower public API의 input으로 받거나 witness sector list를 ownership으로 복사하지 않는다.

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

### Existing MAP04_02 Initializer — exact all

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchIdFactory.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchInitializationError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchInitializationDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchInitializationPublication.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchInitializationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchSeedInitializer.cs
```

MAP04_01/02 checked-in model/public API가 Result 요약이나 이 문서의 illustrative signature보다 우선한다. 기존 constructor/property shape에 맞춰 새 grower를 구현하고 기존 파일을 수정하지 않는다.

### Focused Tests / Assemblies

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/CorePatchSeedInitializerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/BiomePatchModelsTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/CoreCapacityFloodCheckerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationValidatorTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

위 C#/asmdef와 matching meta, approved Runtime/Test `Generation` 직계 파일명의 path-only inventory, Authoring CSV/meta count·hash, 전체 meta GUID, task marker 이후 change-scope path만 확인할 수 있다.

금지:

- installed Authoring CSV body 직접 재파싱·수정
- MAP04_04 이후 Task body
- unrelated production/test C# body
- Legacy/Stage/P6/P11 generator body
- Scene/Prefab YAML
- MAP04_01/02 existing production/test 수정

## WRITE ALLOWLIST

### 신규 Runtime production C# — exact 6

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchGrowthError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchGrowthRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchGrowthDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchGrowthPublication.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchGrowthResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchGrower.cs
```

### 신규 Runtime EditMode test — exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/CorePatchGrowerTests.cs
```

신규 C# 7개와 matching `.cs.meta` 7개, Result 1만 생성한다. 기존 production/tests/meta/asmdef/asmref는 수정하지 않는다. existing approved directory를 재사용하며 새 directory/folder meta를 만들지 않는다.

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
Input artifact     = MAP04_02 CorePatchInitializationPublication
Source reservation = final immutable SiteReservationSnapshot
Output artifact    = Core-grown partial BiomePatchSnapshot
Pass ID            = PASS_BIOME (not executed in this Task)
RNG stream         = RNG_BIOME_PATCH (zero draws in this Task)
Grid               = 13 x 13 / 169 sectors / index y*13+x
Growth role        = CORE only
Growth stop        = max(in-world mandatory buffer count, rule MinSectorCount)
```

- input publication, source P01 snapshot, input P02 snapshot, definitions, patches, ownership rows, bindings, seeds를 clone-in-place하거나 mutate하지 않는다.
- output은 새 `BiomePatchSnapshot`이며 input과 같은 world seed와 exact Core patch/seed/binding identity를 보존한다.
- input Core seeds와 site-binding footprint cell list는 증가시키지 않는다. 성장 sector는 patch membership/ownership만 추가된다.
- SecondaryBiomeId는 모든 row에서 empty를 유지한다.
- output은 Satellite/Intrusion 전 단계이므로 `IsComplete == false`다.
- RNG factory/stream을 매개변수로 받거나 생성하지 않는다.

## Exact Core Sources / Canonical Order

| Order | Source reservation | Biome | Core rule | Min | Max | Buffer | Touch edge |
|---:|---|---|---|---:|---:|---:|---|
| 0 | `RSV_02_SITE_MOON_SEAL_FORGE` | `BIO_ABANDONED_MILL` | `PATCH_MILL_CORE` | 4 | 14 | 1 | false |
| 1 | `RSV_03_SITE_CASSIA_SAP_HEART` | `BIO_CASSIA_ROOT` | `PATCH_ROOT_CORE` | 5 | 18 | 1 | false |
| 2 | `RSV_04_SITE_DEEP_STAR_YEAST` | `BIO_MOON_DOUGH` | `PATCH_DOUGH_CORE` | 5 | 18 | 1 | true |
| 3 | `RSV_05_SITE_MOON_CORE_METEOR` | `BIO_MOON_CRATER` | `PATCH_CRATER_CORE` | 5 | 18 | 1 | true |

canonical growth/record order는 source reservation ID ordinal이다. input patch, binding, seed, definition collection order가 달라도 logical output은 같아야 한다.

Start/Boss/Village reservation에는 patch를 만들거나 ownership을 할당하지 않는다. exact four source/binding/patch/rule mapping이 missing/duplicate/unexpected이면 structural `InvalidInput`이다.

## Public Grower API

```text
public sealed class CorePatchGrower

CorePatchGrowthResult Grow(
    CorePatchInitializationPublication initialization,
    IEnumerable<BiomeTypeDefinition> biomeTypes,
    IEnumerable<BiomePatchRuleDefinition> patchRules)
```

grower는 Registry singleton, CSV/file, RNG, clock, current root/pass state, MAP03 capacity approval을 자체 조회하지 않는다.

## Structural Preflight

sector 계산과 output allocation 전에 아래를 모두 accumulated validation한다.

- initialization/publication/source/snapshot/collections/items가 non-null이다.
- source와 input P02 seed는 exact 같고 169 ownership rows/index-coordinate identity를 가진다.
- input은 exact four Core patches/bindings과 각 source footprint union만 assigned된 valid MAP04_02 partial state다. current starter는 four one-cell footprints라 `4 assigned / 165 unassigned`지만 generic multi-cell footprint publication도 허용한다.
- patch IDs는 `PATCHINST_CORE_<SOURCE_RESERVATION_ID>` exact identity다.
- every patch/binding/seed/ownership linkage와 source reservation footprint가 bidirectionally consistent하다.
- existing assigned rows는 오직 matching source footprint이며 Satellite/Intrusion/SecondaryBiome/unknown ownership이 없다.
- active required biome exact four와 active Core rule exact four가 unique하고 canonical identity를 가진다.
- rule role `CORE`, biome/rule identity match, `1 <= MinSectorCount <= MaxSectorCount <= 169`, `0 <= BufferRingSectors <= 12`다.
- `max(MinSectorCount, in-world mandatory buffer count) <= MaxSectorCount`다.
- undefined enum/numeric cast, duplicate ID/index, invalid reservation footprint, invalid collection ordering assumptions을 거부한다.

structural invalid input은 growth를 시작하지 않고 `InvalidInput`, retry false, publication/diagnostics null, stable sorted errors `>=1`을 반환한다.

`CorePatchGrowthErrorCode` structural values의 frozen order:

```text
MissingInitialization
InvalidInitialization
MissingSourceSiteSnapshot
InvalidSourceSiteSnapshot
MissingBiomeTypes
MissingPatchRules
NullDefinition
DuplicateDefinitionId
MissingBiomeDefinition
MissingCorePatchRule
InvalidBiomeDefinition
InvalidCorePatchRule
DefinitionIdentityMismatch
MissingCorePatch
MissingCoreBinding
InvalidCorePatch
InvalidCoreBinding
InvalidCoreSeed
InvalidOwnership
UnexpectedAssignedSector
TargetExceedsMaximum
InternalInvariantViolation
```

## Fixed Grid / Neighbor Contract

```text
grid width/height = 13/13
valid indices     = 0..168
index             = y*13+x
connectivity      = cardinal only
diagonal          = false
```

- 모든 output sector list는 `WorldGridIndex` ascending copied read-only snapshot이다.
- valid cardinal neighbors는 sector index ascending으로 열거한다.
- world wrap/clamp/diagonal/corner cutting이 없다.
- source site entry exterior는 reservation cell이 아니면 일반 biome sector로 성장 가능하다. entry exterior를 별도 blocker로 만들지 않는다.

## Reservation Hard Blocker Contract

P01 exact 169 `SectorReservations`가 authoritative blocker다.

```text
matching own source footprint = already forced-owned / available
every other reserved sector   = +infinity / claim forbidden
unreserved sector             = eligible subject to ownership/frontier rules
```

- Start, Boss, Village, 다른 Core site footprint는 모두 hard blocker다.
- reservation kind/owner에 따라 penalty로 약화하거나 침범하지 않는다.
- matching own source footprint 이외의 reserved row를 CorePatch sector로 추가하면 안 된다.
- entry exterior, capacity witness-only cells, unreserved buffer cells은 reservation이 아니므로 eligible하다.

## Mandatory Buffer Contract

각 binding의 source footprint 전체가 multi-source origin이다.

```text
FootprintDistance(s) = min ManhattanDistance(s, footprintCell)

MandatoryBuffer = all in-world sectors where
                  FootprintDistance <= CorePatchRule.BufferRingSectors
```

- sparse/multi-cell footprint는 각 occupied cell cardinal-distance ring의 union이다.
- rectangle bounding box, Chebyshev/diagonal ring, origin-only ring을 사용하지 않는다.
- theoretical ring이 world 밖으로 나가고 `CanTouchWorldEdge == false`면 `BufferOutsideWorld` retry-required failure다.
- `CanTouchWorldEdge == true`면 outside theoretical coordinate는 unique count만 기록하고 in-world portion을 사용한다. clamp/wrap하지 않는다.
- MandatoryBuffer가 다른 P01 reservation을 포함하면 `BufferBlockedByReservation`이다.
- 서로 다른 Core의 MandatoryBuffer가 한 sector라도 겹치면 pair 양쪽 `MandatoryBufferConflict`다.
- conflict를 숨기기 위해 buffer를 줄이거나 cell을 빼거나 owner를 비용으로 결정하지 않는다.

valid spatial input에 대한 target:

```text
TargetSectorCount = max(
    CorePatchRule.MinSectorCount,
    MandatoryBuffer.Count)
```

MaxSectorCount까지 채우지 않는다. starter는 four 1-cell footprints와 buffer `1`이므로 각 target은 exact `5`; Forge도 `max(4,5)=5`다.

## Exact Atomic Growth Algorithm

structural preflight 후 temporary working ownership에서만 아래를 수행한다.

### A. Mandatory claim

1. exact four MandatoryBuffer를 모두 계산하고 reservation/outside/cross-Core conflict를 먼저 검증한다.
2. 모든 hard gate가 통과한 뒤 each patch own initial footprint를 보존한다.
3. source ID canonical order로 mandatory in-world sector를 index ascending claim한다.
4. 이미 own patch인 cell은 no-op, unassigned cell은 matching PrimaryBiomeId/PatchId ownership으로 바꾼다.
5. 다른 patch가 소유한 cell은 last-write-wins하지 않고 `MandatoryBufferConflict`다.

### B. Minimum-size supplement

MandatoryBuffer claim 뒤 `SectorCount < TargetSectorCount`인 patch만 deterministic rounds로 보충한다.

eligible candidate:

```text
in world
AND cardinal-adjacent to current own patch
AND currently unassigned
AND not reserved by P01
AND not another Core mandatory/owned sector
```

각 deficient patch는 한 round에 best candidate 하나를 proposal한다.

```text
CandidateKey = (
    FootprintDistance ascending,
    ExposedPerimeterDelta ascending,
    SectorIndex ascending)

ExposedPerimeterDelta = 4 - 2 * CurrentOwnCardinalNeighborCount
```

all proposals의 global claim order:

```text
FootprintDistance ascending
ExposedPerimeterDelta ascending
SourceReservationId ordinal
SectorIndex ascending
```

- 한 patch는 한 round에 최대 한 cell만 성공 claim한다.
- earlier proposal과 sector가 충돌해 invalid가 된 later proposal은 그 round에 다른 후보로 즉시 대체하지 않고 다음 round에서 recompute한다.
- claim 뒤 frontier/perimeter를 다시 계산한다.
- target에 도달한 patch는 이후 proposal하지 않는다.
- deficient patch가 남았는데 한 round의 successful claim이 0이면 `InsufficientUnreservedCapacity` retry-required failure다.
- target을 넘겨 claim하지 않고, initial+added cardinal connectivity를 유지한다.

spatial failure가 하나라도 있으면 output snapshot/publication을 만들지 않는다. source/input은 그대로 두고 `RetryRequired`, publication null, immutable diagnostics와 stable errors를 반환한다. caller가 future `PASS_BIOME`/필요 시 상위 `PASS_SITE` retry를 결정한다. 이 Task에서 retry loop나 root state transition을 구현하지 않는다.

## Explicit MAP03 Witness Independence

- `CoreCapacityApproval`, `CoreCapacityFloodWitness`, witness sector list를 grower 인자로 받지 않는다.
- witness list를 patch sector로 복사·union·hint·tie-break에 사용하지 않는다.
- mandatory buffer와 minimum supplement를 source footprint, P01 reservations, typed rules에서 독립 재계산한다.
- starter output sector union이 prior disjoint 20-cell capacity evidence와 일치할 수 있으나, 그것은 독립 결과 비교 evidence이지 implementation dependency가 아니다.
- capacity witness object를 바꾸거나 제거해도 같은 direct grower input의 logical output은 달라지지 않는다.

## `CorePatchGrowthRecord` Contract

per Core immutable evidence:

```text
BiomePatchId PatchId
SiteReservationId SourceReservationId
string BiomeId
string CorePatchRuleId
int InitialSectorCount
int MandatoryBufferSectorCount
int OutsideTheoreticalBufferCount
int MinimumSectorCount
int MaximumSectorCount
int TargetSectorCount
int MandatoryAddedSectorCount
int SupplementalAddedSectorCount
int FinalSectorCount
int GrowthRoundCount
IReadOnlyList<int> FootprintSectorIndices
IReadOnlyList<int> MandatoryBufferSectorIndices
IReadOnlyList<int> AddedSectorIndices
IReadOnlyList<int> FinalSectorIndices
```

- list는 copied, unique, ascending, read-only다.
- `Added = Final - Initial`, `Final = Initial union Added`다.
- MandatoryAdded와 SupplementalAdded는 disjoint하며 sum이 Added count다.
- `FinalSectorCount == TargetSectorCount`, `Minimum <= Final <= Maximum`이다.
- footprint/buffer/final set inclusion과 cardinal connectivity를 constructor에서 재검증한다.

## `CorePatchGrowthDiagnostics` Contract

```text
ulong WorldSeed
IReadOnlyList<CorePatchGrowthRecord> Records
int CorePatchCount
int InitialAssignedSectorCount
int MandatoryAddedSectorCount
int SupplementalAddedSectorCount
int TotalAddedSectorCount
int FinalAssignedSectorCount
int FinalUnassignedSectorCount
int ReservedSectorCount
int ReservationIntrusionCount
int CrossPatchOverlapCount
int RngDrawCount
```

- Records는 source ID canonical order다.
- conservation: `InitialAssigned + TotalAdded == FinalAssigned`, `FinalAssigned + FinalUnassigned == 169`.
- successful diagnostics는 reservation intrusion/cross-patch overlap/RNG exact `0`이다.
- retry failure diagnostics는 `Records`를 empty로 두고 rollback된 input 기준 assigned/unassigned conservation과 계산 가능한 aggregate facts만 보존한다. added/final-growth counts는 partial working state가 아니라 externally observable rollback state를 나타내며 partial snapshot/ownership은 노출하지 않는다.

## `CorePatchGrowthPublication` Contract

sealed immutable output envelope:

```text
CorePatchInitializationPublication SourceInitialization
SiteReservationSnapshot SourceSiteSnapshot
BiomePatchSnapshot Snapshot
IReadOnlyList<CorePatchGrowthRecord> Records
int CorePatchCount
int CoreSeedCount
int CoreSiteBindingCount
int AssignedSectorCount
int UnassignedSectorCount
```

- `SourceInitialization`와 `SourceSiteSnapshot` reference identity를 exact 보존한다.
- source/input/output seed가 exact 같다.
- exact four Core patch IDs, four seeds, four bindings과 source linkage를 보존한다.
- snapshot patch membership과 ownership rows를 records와 bidirectionally 재검증한다.
- public mutable dictionary/list/setter/field를 노출하지 않는다.

## Result Contract

`CorePatchGrowthStatus` exact values:

```text
Completed
InvalidInput
RetryRequired
```

spatial error code frozen continuation:

```text
BufferOutsideWorld
BufferBlockedByReservation
MandatoryBufferConflict
InsufficientUnreservedCapacity
```

`CorePatchGrowthError` immutable fields:

```text
CorePatchGrowthErrorCode Code
BiomePatchId PatchId
SiteReservationId SourceReservationId
SiteReservationId OtherSourceReservationId
int SectorIndex
int RequiredCount
int AvailableCount
int Shortfall
string Message
```

- unknown identity는 default/empty, unknown sector는 `-1`이다.
- counts는 non-negative, `Shortfall == max(0, RequiredCount - AvailableCount)`다.
- code, source ID, other source ID, patch ID, sector index, counts, message ordinal로 sort/dedupe한다.
- message는 stable non-empty이며 absolute path, stack, timestamp, thread, culture-dependent exception text를 포함하지 않는다.

`CorePatchGrowthResult` immutable properties:

```text
CorePatchGrowthStatus Status
bool Succeeded
bool RetryRequired
CorePatchGrowthPublication Publication
CorePatchGrowthDiagnostics Diagnostics
IReadOnlyList<CorePatchGrowthError> Errors
```

- `Completed`: publication/diagnostics non-null, errors `0`, retry false.
- `InvalidInput`: publication/diagnostics null, errors `>=1`, retry false.
- `RetryRequired`: publication null, diagnostics non-null, spatial errors `>=1`, retry true.
- unexpected invariant/overflow/model construction failure는 stable `InternalInvariantViolation` 하나로 atomic invalid failure하며 exception text나 partial output을 노출하지 않는다.

## Snapshot Cross-Consistency

successful output은 기존 MAP04_01 snapshot invariant를 모두 유지한다.

- exact 169 sector rows / index-coordinate identity
- Patches PatchId ordinal, sectors index, bindings source ID canonical order
- patch↔ownership exact bidirectional membership
- every seed remains owned by its original Core patch
- every binding footprint remains inside same biome Core patch
- every added row has matching PrimaryBiomeId/PatchId and empty SecondaryBiomeId
- orphan seed/binding/ownership/patch sector `0`
- cross-patch overlap `0`
- all non-owned rows remain fully unassigned
- `Assigned + Unassigned == 169`
- `IsComplete == false`

## Starter Exact Evidence

current Moon Palace starter successful result:

```text
Core patches                     = 4
Core seeds                       = 4
Core bindings                    = 4
Initial assigned / unassigned    = 4 / 165
Per-patch target                 = 5 / 5 / 5 / 5
Final patch sector counts        = 5 / 5 / 5 / 5
Mandatory added                  = 16
Supplemental added               = 0
Total added                      = 16
Final assigned / unassigned      = 20 / 149
Reservation intrusion            = 0
Cross-patch overlap              = 0
RNG draws                        = 0
```

starter source footprints are each one cell and their full buffer-1 cross is in-world/disjoint/unreserved. Therefore all targets are satisfied in mandatory phase and supplemental rounds are zero.

## Determinism / Immutability

same direct input은 fresh/reused grower, reversed/shuffled definition enumeration, different culture/time/thread, extra unrelated RNG stream activity에서 byte-equivalent logical snapshot/records/diagnostics를 만든다.

- no random/timestamp/GUID/hash-set iteration tie-break
- no mutable static cache/counter
- no caller collection/list ownership leakage
- returned lists reject mutation/downcast leakage
- failed call 뒤 same instance valid call은 fresh instance와 identical하다
- input publication/snapshot/definitions의 observable state와 collection order는 call 전후 exact 같다

## Required Focused Tests

`CorePatchGrowerTests.cs`에 최소 `120`개 실제 NUnit case를 만든다.

필수 coverage:

1. starter exact `4 -> 20 assigned`, `165 -> 149 unassigned`, per-patch `5/5/5/5`
2. mandatory added `16`, supplemental `0`, reservation/overlap/RNG `0`
3. exact four source/biome/rule/patch mapping과 canonical record order
4. full-footprint Manhattan union buffer; origin-only/bounding-box/Chebyshev 거부
5. buffer `0/1/2`, multi-cell footprint, corners/edges/interior vectors
6. `CanTouchWorldEdge` false failure와 true truncated buffer success
7. Start/Boss/Village/other-Core reservation hard blocker 각각
8. entry exterior가 unreserved이면 eligible
9. cross-Core mandatory overlap pair diagnostics/order
10. target `max(buffer count,min)`, no max-fill, no overgrowth
11. supplemental candidate distance/perimeter/source/sector tie-break exact vectors
12. one-claim-per-patch round, collision recompute, connectivity
13. insufficient capacity retry true, no publication/partial mutation
14. structural error accumulation/sort/dedupe와 retry false
15. null/duplicate/missing/unexpected definition/patch/binding/seed/ownership cases
16. existing unexpected assigned row와 SecondaryBiome rejection
17. snapshot bidirectional membership and conservation
18. source initialization/site snapshot reference identity
19. input/output list immutability와 caller mutation isolation
20. shuffled/reversed enumeration, culture, repeated instance determinism
21. capacity approval/witness absent/independent behavior
22. forbidden RNG/Unity/time/filesystem/static mutable dependency scan

fixture setup은 installed Authoring CSV를 읽거나 reflection으로 private invariant를 우회하지 않는다. existing public constructors/factories와 typed in-memory definitions를 사용한다.

## Required Verification — Reduced but Explicit

실제로 실행:

```text
CorePatchGrowerTests                    >=120 PASS
CorePatchSeedInitializerTests            121/121 PASS
BiomePatchModelsTests                    107/107 PASS
CoreCapacityFloodCheckerTests            215/215 PASS
Required regression total                443/443 PASS
Actually executed required total        >=563 PASS
Failed / skipped                           0 / 0
```

large suites는 사용자 지정 감축 profile에 따라 실행하지 않고 discovery/arithmetic로만 확인한다.

```text
Game.Map targeted discovery arithmetic  >=4197
Full EditMode current discovery          >=4266
```

Result에서 actually executed PASS와 discovery-only count를 별도 항목으로 기록한다. targeted/full을 실제 실행하지 않았다면 `PASS`라고 쓰지 않는다. test selection이 의도 fixture와 달랐거나 결과 evidence가 없는 job은 PASS 합계에 포함하지 않는다.

Unity forced compile 후:

```text
compile errors         = 0
Console errors         = 0
relevant new warnings  = 0
```

## Asset / Change Gates

```text
Baseline Assets meta: 3086
New Runtime C#: 6
New test C#: 1
New matching meta: 7
Final Assets meta: 3093
Exact Assets changes after .APPLIED: 14
Existing Assets modifications: 0
Unexpected Assets changes: 0
Authoring CSV/meta: 50/50 unchanged
Accepted legacy Editor folder meta: 6/6 unchanged
Duplicate GUID groups: 0
```

각 신규 `.cs.meta`는 `fileFormatVersion: 2`, valid unique non-zero lowercase 32-hex GUID, `MonoImporter`여야 한다. Unity가 unrelated folder meta를 만들면 baseline 여부를 확인하고 이 Task가 만든 unexpected file은 제거한다. existing legacy Editor folder meta 6개는 삭제·재생성하지 않는다.

## Result File Contract

`REPORTS/MAP04_03_IMPLEMENT_CORE_PATCH_GROWER_RESULT.md` 첫 부분:

```text
# MAP04_03 — Implement Core Patch Grower Result

STATUS: PASS
```

Result는 최소 아래를 포함한다.

- PATCH APPLY와 `.APPLIED` hash
- READ/WRITE allowlist 준수
- CREATED/MODIFIED/PREEXISTING_IDENTICAL
- 6 runtime + 1 test exact path와 matching meta/GUID
- exact mandatory buffer/target/frontier ordering evidence
- starter `4 patches / 20 assigned / 149 unassigned / mandatory +16 / supplemental 0 / RNG 0`
- reservation intrusion/cross-patch overlap `0/0`
- source/input/output immutability와 atomic failure evidence
- MAP03 capacity witness non-dependency evidence
- actual focused/regression counts와 job IDs
- targeted/full discovery는 executed PASS와 분리
- compile/Console/meta/GUID/Authoring/change-scope evidence
- OUT_OF_SCOPE_FINDINGS
- NEXT: MAP04_03만 COMPLETE/Current Task NONE, MAP04_04 LOCKED

어느 gate든 실패하거나 current project API가 frozen contract와 양립할 수 없으면 `STATUS: BLOCKED` 또는 `STATUS: FAIL`을 정확히 기록한다. 기존 MAP04_01/02 파일을 repair하거나 partial output을 finalize하지 않는다.

## STATUS FINALIZE

전부 PASS일 때만:

```text
MAP04_03_IMPLEMENT_CORE_PATCH_GROWER: COMPLETE
Current Task: NONE
Last Completed Task: MAP04_03_IMPLEMENT_CORE_PATCH_GROWER
Last Result: REPORTS/MAP04_03_IMPLEMENT_CORE_PATCH_GROWER_RESULT.md / STATUS: PASS
MAP04_04_IMPLEMENT_SATELLITE_SEED_PLACER: LOCKED
```

MAP04_04를 자동 시작하거나 Task 파일을 만들지 않는다.

## DO NOT

- existing C#/meta/asmdef/asmref/CSV/Scene/Prefab 수정 금지
- MAP04_01 models/MAP04_02 initializer 편의 수정 금지
- MAP03 capacity witness list 복사·input dependency 금지
- 다른 P01 reservation 침범/penalty 완화 금지
- diagonal/bounding-box/origin-only buffer 금지
- last-write-wins/partial publication 금지
- MaxSectorCount까지 과성장 금지
- Satellite/Intrusion seed 또는 count draw 금지
- altitude/noise/weighted full-map cost와 RNG 금지
- remaining 149 unassigned sector 채우기 금지
- cleanup/serializer/generated CSV/file I/O 금지
- validator/overlay/EditorWindow/Gizmo/menu 금지
- `PASS_BIOME` adapter/root/artifact transaction/retry loop 금지
- Git commit/push 금지

## Recommended Commit

```text
feat(map): grow core biome patches to mandatory size
```
