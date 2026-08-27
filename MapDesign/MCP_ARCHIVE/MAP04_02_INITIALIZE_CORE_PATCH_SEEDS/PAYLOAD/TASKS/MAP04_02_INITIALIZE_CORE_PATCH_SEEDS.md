# MAP04_02 — Initialize Core Patch Seeds

```yaml
status_control:
  task_key: MAP04_02_INITIALIZE_CORE_PATCH_SEEDS
  result_file: REPORTS/MAP04_02_INITIALIZE_CORE_PATCH_SEEDS_RESULT.md
```

## TASK TYPE

```text
RUNTIME DETERMINISTIC P01 CORE-SITE -> P02 CORE-PATCH SEED INITIALIZATION + EDITMODE TESTS
```

## Objective

MAP03 final `SiteReservationSnapshot`의 exact four `CoreBiomeSeed`와 MAP04_01 immutable P02 models를 연결한다.

각 Core seed의 source reservation footprint **전체**를 동일한 Core patch의 다음 세 역할로 동시에 초기화한다.

```text
1. Core BiomePatchSeed
2. Core BiomePatch owned sector
3. PrimaryBiomeId + PatchId assigned BiomeSectorOwnership
```

각 source reservation마다 deterministic Core `BiomePatchId` 하나와 `BiomePatchSiteBinding` 하나를 만들고, exact 169 rows 중 Core footprint 외의 모든 sector는 unassigned인 immutable partial `BiomePatchSnapshot`을 atomic publish한다.

이번 Task는 footprint seed initialization만 한다. `buffer_ring_sectors`, `min_sector_count`까지 성장시키는 일은 `MAP04_03_IMPLEMENT_CORE_PATCH_GROWER`의 책임이다. Satellite/Intrusion seed, growth cost, RNG, cleanup, generated CSV, final validator, overlay도 구현하지 않는다.

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
12. `REPORTS/MAP04_01_IMPLEMENT_BIOME_PATCH_MODELS_RESULT.md`

MAP04_01 Result에서 아래 실제 evidence를 확인한다.

```text
STATUS: PASS
New BiomePatchModelsTests: 107/107 PASS
Required regressions: 496/496 PASS
Actually executed final validation: 603/603 PASS
Final Assets meta: 3079
Existing Assets modifications: 0
NEXT: MAP04_01 only COMPLETE / Current Task NONE / MAP04_02 remains LOCKED
```

Result SHA-256은 exact 아래다.

```text
b7362725e0a4bdf952372b67ece63e1b0f3e26c4306845d09b5250753eedeb6d
```

MAP04_01은 사용자 지정 감축 검증에서 targeted/full을 실행 PASS로 주장하지 않고 discovery `3956/4025`로 분리 기록했다. 이 구분을 그대로 보존하고 미실행 suite를 prior PASS로 바꾸지 않는다.

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
05_GENERATED_OUTPUT_SCHEMA/generated_biome_patches.csv
```

reference CSV는 계약 확인용으로만 읽는다. installed Authoring CSV를 직접 읽거나 재파싱하지 않는다. exact reference가 없으면 이 Task의 frozen contracts와 existing typed APIs를 authoritative fallback으로 사용한다.

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
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteFootprint.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreBiomeSeed.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorReservation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationPublication.cs
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

MAP04_01 Result가 요약한 public API보다 실제 checked-in model contract가 우선한다. constructor/property shape에 맞춰 새 initializer를 구현하고 기존 MAP04_01 파일을 수정하지 않는다.

### Focused Tests / Assemblies

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/BiomeBoundaryDefinitionBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/StaticDataRegistryBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/GeneratedWorldDataTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationModelsTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/BiomePatchModelsTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

위 C#/asmdef와 matching meta, approved Runtime/Test `Generation` 직계 파일명의 path-only inventory, Authoring CSV/meta count·hash, 전체 meta GUID, task marker 이후 change-scope path만 확인할 수 있다.

금지:

- installed Authoring CSV body 직접 재파싱·수정
- MAP04_03 이후 Task body
- unrelated production/test C# body
- Legacy/Stage/P6/P11 generator body
- Scene/Prefab YAML
- MAP04_01 existing model/test 수정

## WRITE ALLOWLIST

### 신규 Runtime production C# — exact 6

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchIdFactory.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchInitializationError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchInitializationDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchInitializationPublication.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchInitializationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchSeedInitializer.cs
```

### 신규 Runtime EditMode test — exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/CorePatchSeedInitializerTests.cs
```

신규 C# 7개와 matching `.cs.meta` 7개, Result 1만 생성한다. 기존 production/tests/meta/asmdef/asmref는 수정하지 않는다. 기존 approved directory를 재사용하며 새 directory/folder meta를 만들지 않는다.

## Namespace / Assembly

```text
Runtime namespace: StarNight.Map.WorldGeneration.Generation
Test namespace:    StarNight.Map.Tests.WorldGeneration.Generation
Runtime assembly:  Game.Map.Runtime
Test assembly:     Game.Map.Tests.EditMode
```

`UnityEditor`, `UnityEngine.Object`, ScriptableObject/MonoBehaviour, serialization callback, reflection factory, service locator, singleton/static mutable state를 도입하지 않는다. Unity 6000.3.8f1의 current language level에서 compile되도록 record/record struct, `required`, `init`, nullable-reference directive에 의존하지 않는다.

## Frozen P02 Initialization Boundary

```text
Input artifact  = final SITE_RESERVATIONS snapshot
Output artifact = partial BIOME_PATCHES snapshot
Pass ID         = PASS_BIOME (not executed in this Task)
RNG stream      = RNG_BIOME_PATCH (zero draws in this Task)
Grid            = 13 x 13 / 169 sectors / index y*13+x
Created role    = CORE only
Owned now       = each Core source reservation footprint only
Unassigned now  = every other sector
```

- world seed는 input `SiteReservationSnapshot.Seed`를 exact 보존한다.
- P01 snapshot/Reservations/SectorReservations/CoreBiomeSeeds를 clone하거나 mutate하지 않는다.
- P02 output은 새 immutable `BiomePatchSnapshot`이다.
- minimum/buffer witness를 초기 ownership으로 가져오지 않는다. 오직 source reservation footprint cell만 seed/owned다.
- `CoreCapacityFloodWitness`의 additional capacity sector를 복사하지 않는다. MAP04_03이 성장 규칙으로 다시 결정한다.
- RNG factory/stream을 매개변수로 받거나 생성하지 않는다.

## Exact Core Sources / Canonical Order

현재 Moon Palace starter의 exact four source mapping은 아래다.

| Order | Source reservation | Kind | Biome | Core rule | Min | Buffer |
|---:|---|---|---|---|---:|---:|
| 0 | `RSV_02_SITE_MOON_SEAL_FORGE` | Forge | `BIO_ABANDONED_MILL` | `PATCH_MILL_CORE` | 4 | 1 |
| 1 | `RSV_03_SITE_CASSIA_SAP_HEART` | CoreResource | `BIO_CASSIA_ROOT` | `PATCH_ROOT_CORE` | 5 | 1 |
| 2 | `RSV_04_SITE_DEEP_STAR_YEAST` | CoreResource | `BIO_MOON_DOUGH` | `PATCH_DOUGH_CORE` | 5 | 1 |
| 3 | `RSV_05_SITE_MOON_CORE_METEOR` | CoreResource | `BIO_MOON_CRATER` | `PATCH_CRATER_CORE` | 5 | 1 |

canonical initialization order는 source reservation ID ordinal이다. input CoreBiomeSeed, reservation, definition collection order가 바뀌어도 output이 달라지지 않는다.

Start/Boss/Village에는 Core seed/binding/Core patch를 만들지 않는다. exact source set이 missing/duplicate/unexpected이면 structural `InvalidInput`이다.

## `CorePatchIdFactory` Contract

Core patch instance ID는 source reservation identity만으로 결정한다.

```text
PATCHINST_CORE_<SITE_RESERVATION_ID>
```

exact examples:

```text
PATCHINST_CORE_RSV_02_SITE_MOON_SEAL_FORGE
PATCHINST_CORE_RSV_03_SITE_CASSIA_SAP_HEART
PATCHINST_CORE_RSV_04_SITE_DEEP_STAR_YEAST
PATCHINST_CORE_RSV_05_SITE_MOON_CORE_METEOR
```

public stateless API:

```text
BiomePatchId CreateCorePatchId(SiteReservationId sourceReservationId)
bool TryCreateCorePatchId(
    SiteReservationId sourceReservationId,
    out BiomePatchId patchId)
```

- default/invalid source ID를 거부한다.
- seed, coordinate, source collection order, candidate ordinal, attempt, RNG draw, timestamp, culture를 ID에 넣지 않는다.
- trim/case-fold/Unicode normalization을 하지 않는다.
- current `BiomePatchId` canonical grammar를 통과한 exact result만 반환한다.
- factory는 cache/static mutable dictionary를 사용하지 않는다.

## Public Initializer API

```text
public sealed class CorePatchSeedInitializer

CorePatchInitializationResult Initialize(
    SiteReservationSnapshot siteSnapshot,
    IEnumerable<BiomeTypeDefinition> biomeTypes,
    IEnumerable<BiomePatchRuleDefinition> patchRules)
```

호출자는 immutable Registry root의 typed definitions를 전달할 수 있다. initializer는 Registry singleton, CSV/file, RNG, clock, root/pass current state를 자체 조회하지 않는다.

extra valid biome/patch definitions는 허용한다. 하지만 supplied collection/item null, duplicate ID, malformed required definition은 accumulated structural error다.

## Structural Preflight

output object를 만들기 전에 가능한 오류를 accumulated/sorted validation한다.

### P01 snapshot

- `siteSnapshot` non-null, seed any `ulong`, reservations exact `7`, sectors exact `169`, CoreBiomeSeeds exact `4`
- exact one Start, one Boss, one Forge, three CoreResource, one Village
- reservation IDs/source IDs/order/kinds는 MAP03 final contract와 exact 일치
- CoreBiomeSeed source IDs는 exact canonical four, each once
- each CoreBiomeSeed source reservation exists and is Forge/CoreResource
- source reservation `PrimaryBiomeId`와 CoreBiomeSeed `BiomeId` exact 일치
- CoreBiomeSeed `SeedSector`는 source reservation occupied footprint 안에 있고 exact smallest occupied index coordinate다.
- source reservation occupied sector indices는 non-empty, in-range, unique이며 four sources 사이 overlap `0`
- Core source 외 reservation에는 matching CoreBiomeSeed가 없다.

### Typed definitions

- biomeTypes/patchRules collections와 items non-null, IDs unique
- required four BiomeTypeDefinition each exists, active/required이며 `MinCorePatchCount >= 1`
- required four BiomePatchRuleDefinition each exists, active, exact `CORE`, matching BiomeId
- rule range `1 <= MinSectorCount <= MaxSectorCount <= 169`
- CoreBiomeSeed minimum/buffer는 matching rule `MinSectorCount`/`BufferRingSectors`와 exact 일치
- CoreBiomeSeed BiomeId/RuleId와 typed definition identity가 exact 일치

### Output identity precheck

- generated Core PatchId four are valid/unique
- footprint indices do not overlap
- every footprint index has exact grid coordinate identity
- no SourceReservation/PatchId/BiomeId/RuleId half-state

expected invalid input을 exception control flow로 처리하지 않는다. structural invalid input은 initialization을 수행하지 않고 atomic `InvalidInput`, retry false, publication/diagnostics null, errors non-empty를 반환한다.

## Error Contract

`CorePatchInitializationErrorCode` exact frozen order:

```text
MissingSiteSnapshot
InvalidSiteSnapshot
InvalidReservationSet
InvalidCoreSeedSet
NullCoreSeed
DuplicateCoreSeedSource
MissingRequiredCoreSeed
UnexpectedCoreSeed
MissingSourceReservation
InvalidSourceReservation
SeedOutsideSourceFootprint
SourceFootprintOverlap
MissingBiomeTypes
NullBiomeType
DuplicateBiomeTypeId
MissingRequiredBiomeType
InvalidBiomeType
MissingPatchRules
NullPatchRule
DuplicatePatchRuleId
MissingRequiredPatchRule
InvalidPatchRule
DefinitionIdentityMismatch
InvalidGeneratedPatchId
DuplicateGeneratedPatchId
InternalInvariantViolation
```

`CorePatchInitializationError` immutable evidence:

```text
CorePatchInitializationErrorCode Code
string SourceReservationId
string BiomeId
string PatchRuleId
int SectorIndex
string Message
```

- unknown identity는 empty string, unknown sector는 `-1`이다.
- code, source ID, biome ID, rule ID, sector index, message ordinal로 sort/dedupe한다.
- message는 invariant stable non-empty text이며 absolute path, stack, timestamp, thread, culture-dependent exception text를 포함하지 않는다.

## Exact Initialization Algorithm

canonical four CoreBiomeSeed마다 아래를 수행한다.

1. source reservation을 ID로 resolve한다.
2. `CorePatchIdFactory`로 exact PatchId를 만든다.
3. source reservation의 occupied footprint sector 전체를 index ascending으로 가져온다.
4. **각 footprint sector마다** Core `BiomePatchSeed` 하나를 만든다.
5. 동일 footprint sector list를 initial `BiomePatch.SectorIndices`로 사용한다.
6. CoreBiomeSeed `CorePatchRuleId`, `BiomeId`를 patch identity로 사용한다.
7. 동일 source/patch/biome/footprint로 `BiomePatchSiteBinding`을 만든다.
8. 각 footprint sector에 matching assigned `BiomeSectorOwnership`을 만든다.

나머지 exact 169 rows는 `BiomeSectorOwnership.CreateUnassigned`와 동등한 state여야 한다.

```text
PrimaryBiomeId   = empty
SecondaryBiomeId = empty
PatchId          = null / unassigned
```

assigned Core row는:

```text
PrimaryBiomeId   = matching CoreBiomeSeed.BiomeId
SecondaryBiomeId = empty
PatchId          = generated Core PatchId
```

ownership winner/overwrite/last-write-wins는 없다. overlap이 발견되면 output을 만들지 않는다.

## `CorePatchInitializationPublication` Contract

sealed immutable output envelope:

```text
SiteReservationSnapshot SourceSiteSnapshot
BiomePatchSnapshot Snapshot
IReadOnlyList<BiomePatchId> CorePatchIds
int CorePatchCount
int CoreSeedCount
int CoreSiteBindingCount
int AssignedSectorCount
int UnassignedSectorCount
```

- `SourceSiteSnapshot` reference identity를 exact 보존하고 clone/mutate하지 않는다.
- `Snapshot.Seed == SourceSiteSnapshot.Seed`다.
- CorePatchIds는 PatchId ordinal copied read-only list다.
- publication은 exact Core-only partial snapshot만 허용한다.
- publication constructor는 source↔snapshot seed, Core patch count, seed count, binding count, assigned/unassigned conservation을 다시 검증한다.
- public mutable dictionary/list/setter/field를 노출하지 않는다.

## `CorePatchInitializationDiagnostics` Contract

successful initialization의 exact counts/order evidence를 immutable하게 기록한다.

```text
ulong WorldSeed
int SourceReservationCount
int InputCoreSeedCount
int CorePatchCount
int CoreSeedCellCount
int CoreSiteBindingCount
int AssignedSectorCount
int UnassignedSectorCount
int RngDrawCount
IReadOnlyList<SiteReservationId> SourceReservationIds
IReadOnlyList<BiomePatchId> CorePatchIds
```

- source/patch IDs는 canonical order copied read-only다.
- `RngDrawCount`는 exact `0`이다.
- current starter에서 source reservations `4`, patches `4`, bindings `4`다.
- current starter Core footprints가 각 1 sector이므로 Core seed cells/assigned/unassigned는 exact `4 / 4 / 165`다.
- generic multi-cell source footprint test에서는 Core seed cells와 assigned count가 footprint union count로 증가한다.

## Result Contract

`CorePatchInitializationStatus` exact values:

```text
Completed
InvalidInput
```

`CorePatchInitializationResult` immutable properties:

```text
CorePatchInitializationStatus Status
bool Succeeded
bool RetryRequired
CorePatchInitializationPublication Publication
CorePatchInitializationDiagnostics Diagnostics
IReadOnlyList<CorePatchInitializationError> Errors
```

- `Completed`: publication/diagnostics non-null, errors `0`, retry false.
- `InvalidInput`: publication/diagnostics null, errors `>=1`, retry false.
- 이 Task는 RNG/space search를 하지 않으므로 retry-required status를 만들지 않는다.
- unexpected model construction invariant는 stable `InternalInvariantViolation` 하나로 atomic failure하며 partial patch/ownership/snapshot/exception text를 노출하지 않는다.

## Snapshot Cross-Consistency

published MAP04_01 `BiomePatchSnapshot`이 강제해야 하는 기존 invariant를 그대로 만족한다.

- exact 169 sector rows / index-coordinate identity
- Patches PatchId ordinal, Sectors index, Bindings source ID canonical order
- patch↔ownership exact bidirectional membership
- every seed is owned by its enclosing Core patch
- every binding points to an existing Core patch and same biome
- binding occupied sectors are patch cells and assigned same primary biome
- every binding sector has exact one matching-source Core seed
- orphan Core seed/binding/ownership/patch sector `0`
- patch overlap `0`
- `Assigned + Unassigned == 169`
- current output `IsComplete == false`

SecondaryBiomeId는 전부 empty다. source P01 reservation ID는 binding/seed linkage일 뿐 `GeneratedWorldData`나 `SectorCell`에 쓰지 않는다.

## Determinism / Immutability

same input은 아래 조건에서 byte-equivalent logical snapshot을 만든다.

- fresh/reused initializer
- Core seed/reservation/biome/rule collection reverse/shuffle
- `en-US`, `tr-TR`
- world seed `0`, `4660`, `ulong.MaxValue`
- caller list/array mutation after return

결과 ordering/hash/identity에 Dictionary enumeration order, object hash code, current culture, wall clock, thread, filesystem, Unity lifecycle, RNG가 영향을 주지 않는다.

public setter/mutable field, mutable collection downcast, lazy caller enumeration, static mutable publication/cache는 `0`이어야 한다.

## Required Tests

`CorePatchSeedInitializerTests.cs` actual discovered cases 최소 `96`개를 만든다.

최소 검증 범위:

1. CorePatchIdFactory exact four vectors, invalid/default, culture/order independence
2. null/invalid P01 snapshot 및 exact reservation/Core seed set gate
3. missing/duplicate/unexpected Core seed/source reservation
4. source kind/biome/rule/seed-sector/footprint mismatch
5. definition collection null/item null/duplicate/missing/inactive/wrong-role/wrong-biome/range mismatch
6. current starter exact four PatchId/biome/rule/source mapping
7. each footprint cell becomes seed + patch cell + ownership + binding cell
8. generic 2-cell footprint initializes two seed/owned cells, not origin-only
9. exact 169 rows, assigned/unassigned conservation, SecondaryBiome empty
10. no buffer/witness/minimum growth sector is prepainted
11. patch↔sector↔seed↔binding cross-consistency
12. no overlap/overwrite/orphan/half-state
13. empty RNG dependency and diagnostics draw count `0`
14. reversed/shuffled input, fresh/reused, cultures, three world-seed identities
15. defensive copy/public mutation/static mutable/Unity API/time/filesystem scans

fixture는 typed objects를 memory에서 만든다. installed Authoring CSV/file/Registry singleton을 읽지 않는다.

## Verification Gates — User-Directed Reduced Profile

실제로 실행해야 하는 최소 suite:

```text
New CorePatchSeedInitializerTests: >=96 PASS
BiomePatchModelsTests: 107/107 PASS
SiteReservationValidatorTests: 268/268 PASS
BiomeBoundaryDefinitionBuilderTests: 38/38 PASS
StaticDataRegistryBuilderTests: 53/53 PASS
GeneratedWorldDataTests: 56/56 PASS
Actually executed required total: >=618 PASS
Failed / skipped: 0 / 0
Compile errors / Console errors / relevant new warnings: 0 / 0 / 0
```

targeted/full은 사용자 지정 감축 검증에 따라 discovery와 arithmetic만 확인한다.

```text
Game.Map targeted discovery: >=4052
Full EditMode discovery: >=4121
```

discovery count를 실행 PASS로 표기하지 않는다. 실제로 추가 실행한 suite가 있으면 job ID와 actual result를 별도로 기록한다. stale/timeout/0-of-0 job은 PASS evidence가 아니다.

## Asset / Change Gates

```text
Baseline Assets meta: 3079
New Runtime C#: 6
New test C#: 1
New matching meta: 7
Final Assets meta: 3086
Exact Assets changes after .APPLIED: 14
Existing Assets modifications: 0
Unexpected Assets changes: 0
Authoring CSV/meta: 50/50 unchanged
Accepted legacy Editor folder meta: 6/6 unchanged
Duplicate GUID groups: 0
```

각 신규 `.cs.meta`는 `fileFormatVersion: 2`, valid unique non-zero lowercase 32-hex GUID, `MonoImporter`여야 한다. Unity가 unrelated folder meta를 만들면 baseline 여부를 확인하고 이 Task가 만든 unexpected file은 제거한다. 기존 legacy Editor folder meta 6개는 삭제·재생성하지 않는다.

## Result File Contract

`REPORTS/MAP04_02_INITIALIZE_CORE_PATCH_SEEDS_RESULT.md` 첫 부분:

```text
# MAP04_02 — Initialize Core Patch Seeds Result

STATUS: PASS
```

Result는 최소 아래를 포함한다.

- PATCH APPLY와 `.APPLIED` hash
- READ/WRITE allowlist 준수
- CREATED/MODIFIED/PREEXISTING_IDENTICAL
- 6 runtime + 1 test exact path와 matching meta/GUID
- exact PatchId vectors와 four source/biome/rule mapping
- full-footprint seed/owned/ownership/binding evidence
- starter `4 patches / 4 bindings / 4 assigned / 165 unassigned / RNG 0`
- partial snapshot cross-consistency와 no-growth evidence
- actual focused/regression counts와 job IDs
- targeted/full discovery는 executed PASS와 분리
- compile/Console/meta/GUID/Authoring/change-scope evidence
- OUT_OF_SCOPE_FINDINGS
- NEXT: MAP04_02만 COMPLETE/Current Task NONE, MAP04_03 LOCKED

어느 gate든 실패하거나 current project API가 frozen contract와 양립할 수 없으면 `STATUS: BLOCKED` 또는 `STATUS: FAIL`을 정확히 기록하고 기존 MAP04_01 파일을 repair하거나 partial output을 finalize하지 않는다.

## STATUS FINALIZE

전부 PASS일 때만:

```text
MAP04_02_INITIALIZE_CORE_PATCH_SEEDS: COMPLETE
Current Task: NONE
Last Completed Task: MAP04_02_INITIALIZE_CORE_PATCH_SEEDS
Last Result: REPORTS/MAP04_02_INITIALIZE_CORE_PATCH_SEEDS_RESULT.md / STATUS: PASS
MAP04_03_IMPLEMENT_CORE_PATCH_GROWER: LOCKED
```

MAP04_03을 자동 시작하거나 Task 파일을 만들지 않는다.

## DO NOT

- 기존 C#/meta/asmdef/asmref/CSV/Scene/Prefab 수정 금지
- existing MAP04_01 models의 constructor/property를 convenience 목적으로 수정 금지
- Core buffer/minimum-size growth 금지
- capacity witness 20 sectors를 initial ownership으로 복사 금지
- Satellite/Intrusion seed 또는 count draw 금지
- distance/altitude/noise/perimeter/reservation cost 금지
- priority queue/flood-fill/cleanup 금지
- `PASS_BIOME` adapter/root/artifact transaction/retry 구현 금지
- generated CSV serializer/file I/O 금지
- validator/overlay/EditorWindow/Gizmo/menu 금지
- Git commit/push 금지

## Recommended Commit

```text
feat(map): initialize core biome patch seeds
```
