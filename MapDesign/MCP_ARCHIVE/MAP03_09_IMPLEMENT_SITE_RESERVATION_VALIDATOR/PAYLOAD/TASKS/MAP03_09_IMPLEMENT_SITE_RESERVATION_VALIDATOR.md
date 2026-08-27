# MAP03_09 — Implement Site Reservation Validator

```yaml
status_control:
  task_key: MAP03_09_IMPLEMENT_SITE_RESERVATION_VALIDATOR
  result_file: REPORTS/MAP03_09_IMPLEMENT_SITE_RESERVATION_VALIDATOR_RESULT.md
```

## TASK TYPE

```text
RUNTIME ATOMIC P01 REQUIRED-SITE VALIDATION + FINAL IMMUTABLE RESERVATION SNAPSHOT PUBLICATION + EDITMODE TESTS
```

## Objective

MAP03_08의 immutable `VillageReservationApproval`을 P01 final gate에서 다시 검증한다.

```text
1 Start + 1 Boss + 1 Forge + 3 CoreResource + 1 Village = 7 reservations
```

exact six validation rules를 모두 통과한 경우에만 deterministic reservation ID/order, exact 169 `SectorReservation`, required entry anchors, exact four `CoreBiomeSeed`를 가진 final immutable `SiteReservationSnapshot`을 atomic publish한다.

```text
RequiredSiteCounts
WorldBounds
FootprintOverlap
DistanceConstraints
EntryAnchors
CoreCapacity
```

검증 실패에서 selected plan, Village, witness를 수정하거나 local repair/RNG redraw를 하지 않는다. `ValidationRejected / RetryRequired`를 반환해 caller가 fresh attempt의 whole `PASS_SITE`를 재시도하게 한다.

이 Task의 성공 산출물은 **원본 VillageReservationApproval identity + final SiteReservationSnapshot을 묶은 `SiteReservationPublication`**이다. generated CSV serializer/file I/O, `PASS_SITE` adapter/root retry 실행, overlay, 100,000-seed batch는 아직 만들지 않는다.

## 전체 연결

```text
MAP03_06 provisional six-site selection plan
  -> MAP03_07 four Core capacity witnesses
  -> MAP03_08 one Village selection
  -> MAP03_09 this Task: six-rule validation + final P01 snapshot publication
  -> MAP03_10 reservation overlay
  -> MAP03_11 100,000-seed batch and MAP03 exit
  -> MAP04 CoreBiomeSeed initialization/growth
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
12. `REPORTS/MAP03_08_IMPLEMENT_VILLAGE_RESERVATION_RESULT.md`

MAP03_08 Result의 exact `STATUS: PASS`, focused `339/339`, regressions `215/215 / 248/248 / 270/270 / 239/239 / 170/170 / 268/268 / 81/81 / 667/667`, targeted `3344/3344`, full `3384/3384`, starter candidates `676/624/52`, selected sites `6+1`, witnesses `4/20`, overlaps/conflicts `0`, RNG `3156->3159`, final Assets meta `3054`, existing Assets modification `0`을 확인한다.

## Map Package Reference

exact installed Map Package v1.0이 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/01_WORLD_FIXED_SPEC.md
01_FIXED_SPEC/02_COORDINATE_AND_ID_RULES.md
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
01_FIXED_SPEC/05_IMPLEMENTATION_ORDER.md
02_PHASE_ROADMAP/MAP03_SPECIAL_SITE_RESERVATION.md
02_PHASE_ROADMAP/MAP04_BIOME_PATCH_GENERATOR.md
03_CSV_SCHEMA/CSV_RELATIONSHIPS.md
05_GENERATED_OUTPUT_SCHEMA/generated_special_sites.csv
04_CSV_STARTER/special_map_catalog.csv
04_CSV_STARTER/special_map_footprint_cells.csv
04_CSV_STARTER/special_map_entry_sockets.csv
```

reference가 없으면 이 Task의 frozen required-count/boundary/overlap/distance/entry/capacity/publication contracts와 existing immutable APIs를 authoritative fallback으로 사용한다. Authoring CSV를 직접 읽거나 재파싱하지 않는다. generated schema는 publication 의미 확인용이며 이 Task에서 row/CSV를 만들지 않는다.

## READ ALLOWLIST

### Existing typed definitions

```text
Assets/_Game/Map/Runtime/WorldGeneration/Data/WorldGenerationDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/WorldRouteDefinitionSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/BiomeDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/BiomeBoundaryDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/SpecialMapDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/SpecialVillageDefinitionSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/StaticDataRegistry.cs
```

### Existing grid / placement / distance / approval / final models

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGridIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteFootprint.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteEntryAnchor.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreBiomeSeed.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorReservation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteOriginCandidate.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteFootprintTransformer.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacementEntry.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacement.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SitePlacementKey.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceIndexResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistancePolicy.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceEvaluationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceIndexBuilder.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSearchOption.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSelectionPlan.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityFloodWitness.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityApproval.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageDistanceBucket.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageReservationCandidate.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageReservationDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageReservationRejection.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageReservationError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageReservationSelection.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageReservationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageReservationSelector.cs
```

### Focused tests / assemblies

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/BiomeBoundaryDefinitionBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/SpecialVillageDefinitionBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/StaticDataRegistryBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/ContentVersionHashCalculatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/GridInitializationPassTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationModelsTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteCandidateEnumerationTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/FootprintPlacementSolverTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteDistanceIndexTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteCandidateCostTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationBacktrackerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/CoreCapacityFloodCheckerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/VillageReservationSelectorTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

위 C#/asmdef와 matching meta, approved `Generation` Runtime/Test 직계 파일명의 path-only inventory, Authoring CSV/meta count/hash, 전체 meta GUID, task marker 이후 change-scope path만 확인할 수 있다.

금지:

- Authoring CSV body 직접 재파싱·수정
- MAP03_10 이후 Task body
- unrelated production/test C# body
- Legacy/Stage/P6/P11 generator body
- Scene/Prefab YAML

## WRITE ALLOWLIST

### 신규 Runtime production C# — exact 8

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationValidationRule.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationValidationViolation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationValidationDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationPublication.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationValidationError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationValidationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSnapshotPublisher.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationValidator.cs
```

### 신규 Runtime EditMode test — exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationValidatorTests.cs
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

## Public Validator API

```text
public sealed class SiteReservationValidator

SiteReservationValidationResult ValidateAndPublish(
    ulong worldSeed,
    VillageReservationApproval approval,
    IEnumerable<SpecialMapDefinition> specialMaps,
    IEnumerable<SpecialMapFootprintCellDefinition> footprintCells,
    IEnumerable<SpecialMapEntrySocketDefinition> entrySockets)
```

호출자는 immutable Registry root의 exact parent-scoped definitions를 전달한다. validator는 Registry singleton, RNG, clock, filesystem, current Root/pass state에서 자체 조회하지 않는다.

`worldSeed`는 output snapshot identity일 뿐 검증 결과/ordering/ID를 추첨하지 않는다. 어떤 `ulong` 값도 유효하며 seed `0`, `4660`, `ulong.MaxValue`를 보존한다.

## Structural Preflight

validation rule 계산과 output allocation 전에 가능한 오류를 accumulated/sorted validation한다.

- approval non-null, exact MAP03_08 `VillageReservationApproval`
- embedded Core approval은 exact six-step plan, exact four canonical witnesses, Village one selection을 가진다.
- selected plan key/depth/order는 exact `Start/Boss/Forge/Cassia/Yeast/Meteor`, Village source는 exact `SITE_PRIMARY_VILLAGE`다.
- supplied specialMaps collection/items non-null, ID unique, exact six active required definitions each once
- supplied footprintCells collection/items non-null, composite key unique, exact seven rows and parent coverage
- supplied entrySockets collection/items non-null, composite ID unique, exact six `ENTRY_L` rows and parent coverage
- non-Village definition role/count/dimension/primary-biome/generation-mode/routes identity가 selected placement와 exact 일치한다.
- Village special-map `1x1`/single footprint-cell row는 reservation source template이고 selected `VillageLayoutDefinition` `1x1|2x1|1x2` dimensions와 candidate rectangle이 final footprint authoritative다. Village source dimension을 layout dimension과 억지로 같게 만들거나 source definition을 mutate하지 않는다.
- non-Village five placements의 transformed footprint/entry snapshot은 source definitions에 same transform을 적용한 결과와 exact equivalent다.
- Village profile/layout/bucket/candidate/entry-template identity는 MAP03_08 approval과 exact 같다.
- undefined enum/numeric cast, invalid key/ordinal/origin/index, null placement/witness, duplicate occupied/list identity를 거부한다.
- structural invalid input은 rule evaluation/publication 없이 `InvalidInput`, retry false, diagnostics/violations/publication null-or-empty를 반환한다.

exact source inventory:

```text
special maps = 6
footprint cells = 7 (five 1-cell + Boss 2-cell)
entry sockets = 6 (one required ENTRY_L per special map)
```

## Exact Reservation Identity / Order Contract

final reservation order는 earlier search order에 Village를 append한 exact order다.

| Order | Kind | Source | Reservation ID |
|---:|---|---|---|
| 0 | Start | `WORLD_MOONPALACE_V1` | `RSV_00_WORLD_MOONPALACE_V1` |
| 1 | Boss | `SITE_MOON_BOSS_VAULT` | `RSV_01_SITE_MOON_BOSS_VAULT` |
| 2 | Forge | `SITE_MOON_SEAL_FORGE` | `RSV_02_SITE_MOON_SEAL_FORGE` |
| 3 | CoreResource | `SITE_CASSIA_SAP_HEART` | `RSV_03_SITE_CASSIA_SAP_HEART` |
| 4 | CoreResource | `SITE_DEEP_STAR_YEAST` | `RSV_04_SITE_DEEP_STAR_YEAST` |
| 5 | CoreResource | `SITE_MOON_CORE_METEOR` | `RSV_05_SITE_MOON_CORE_METEOR` |
| 6 | Village | `SITE_PRIMARY_VILLAGE` | `RSV_06_SITE_PRIMARY_VILLAGE` |

generic derivation은 exact invariant ASCII `RSV_` + two-digit order + `_` + source ID다. seed, coordinate, candidate ordinal, random suffix, timestamp를 ID에 넣지 않는다. generated schema의 PK는 `(seed, site_instance_id)`이므로 같은 logical role ID를 seed별 snapshot에서 재사용할 수 있다.

## Exact Six Validation Rules

rules는 아래 enum order로 전부 평가한다. 한 rule 실패 뒤에도 안전하게 독립 계산 가능한 later rule을 평가해 violations를 누적한다. structural preflight failure만 rule evaluation을 막는다.

### 1. `RequiredSiteCounts`

exact final set:

```text
Start 1
Boss 1
Forge 1
CoreResource 3
Village 1
total 7
```

- exact source IDs/instance ordinal `0`이며 duplicate/missing/unexpected source가 없다.
- six active special-map definition `RequiredCount == 1`을 각각 exact 만족한다.
- Village profile는 `VIL_MOON_PRIMARY`, selected layout은 allowed active exact one이다.
- Start source는 `WORLD_MOONPALACE_V1` exact one이다.

### 2. `WorldBounds`

- grid exact `13x13`, index `y*13+x`, valid indices `0..168`이다.
- every origin, occupied sector, footprint cell mapping, entry footprint/exterior, witness sector가 world-bound다.
- every occupied sector는 `origin + local coordinate`와 exact 같다.
- world wrap/clamp/normalize/diagonal projection이 없다.

### 3. `FootprintOverlap`

- seven reservation occupied sets는 pairwise disjoint다.
- occupied union count는 individual occupied counts의 checked sum과 exact 같다.
- candidate occupied가 다른 reservation entry exterior를 막지 않는다.
- every entry exterior는 seven-site occupied union에 속하지 않는다.
- 여러 entry가 같은 unreserved exterior sector를 공유하는 것은 MAP03_03/08 contract처럼 허용한다.
- diagonal/one-cell halo는 이 rule이 아니다.

### 4. `DistanceConstraints`

non-Village six placements는 existing `SiteDistanceIndexBuilder`와 exact required policy 의미를 재검증한다.

```text
keys = 6
pair records = 15
constraints = 15/15 satisfied
minimum distribution = 2x5 / 3x9 / 4x1
```

Village:

- Start distance는 complete footprints 사이 minimum Manhattan distance이며 selected bucket inclusive range에 exact 포함된다.
- Village와 five non-Start placement 각각의 footprint distance는 `>=2`다.
- three CoreResource union이 하나의 width `<=4` and height `<=4` bounding box에 모두 갇히지 않는다.
- origin/center/entry/tile/route distance, diagonal/Chebyshev, wrap/clamp를 사용하지 않는다.

starter distance checks:

```text
non-Village pair constraints = 15
Village Start bucket check = 1
Village other-site checks = 5
Core cluster check = 1
```

### 5. `EntryAnchors`

- Start는 entry `0`; five non-Village special sites와 Village는 required entry exact `1` each다.
- final entry anchors exact `6`, required `6`, return-path-required `6`이다.
- each entry socket ID/route set/flags는 typed source and selected transform/layout identity와 일치한다.
- entry footprint sector는 own occupied set의 correct boundary cell이며 side delta exact once의 exterior는 world-bound다.
- exterior는 own/other footprint에 속하지 않는 unreserved sector다.
- entry side는 footprint에서 바깥을 향하며 own footprint로 다시 들어가지 않는다.
- allowed route types는 exact unique ascending `1|2|3`이고 special map allowed routes와 compatible하다.
- route graph connection/edge signature/tile reachability는 MAP05/MAP10 소유다.

### 6. `CoreCapacity`

- witness exact order/source는 Forge/Cassia/Yeast/Meteor이고 source reservation kind가 Forge/CoreResource다.
- witness exact `4`, targets `5/5/5/5`, total sectors `20`, pairwise overlap `0`이다.
- each witness는 source footprint와 mandatory buffer를 모두 포함하고 cardinal-connected다.
- witness sector count는 `RequiredWitnessSectorCount`와 exact 같고 available count가 target 이상, shortfall exact `0`이다.
- Village occupied와 all witness intersection은 `0`이다.
- witness를 다시 flood/grow/reassign/shrink하지 않는다.

## Rule / Violation Contract

`SiteReservationValidationRule` exact frozen order:

```text
RequiredSiteCounts
WorldBounds
FootprintOverlap
DistanceConstraints
EntryAnchors
CoreCapacity
```

`SiteReservationRuleResult`는 같은 파일에 두며 immutable fields를 제공한다.

```text
SiteReservationValidationRule Rule
bool Passed
int ViolationCount
int MeasuredCount
int ExpectedCount
string Message
```

- exact six rule results를 enum order로 항상 보관한다.
- `Passed == (ViolationCount == 0)`이다.
- counts는 non-negative, message는 stable non-empty다.

`SiteReservationValidationViolationCode` exact frozen order:

```text
MissingRequiredReservation
UnexpectedReservation
RequiredCountMismatch
FootprintOutsideWorld
FootprintIdentityMismatch
FootprintOverlap
BlocksEntryApproach
EntryApproachOccupied
DistanceBelowMinimum
VillageDistanceBucketMismatch
CoreClusterViolation
MissingRequiredEntry
EntryIdentityMismatch
EntryOutsideWorld
EntryFacesOwnFootprint
EntryExteriorOccupied
EntryRouteTypeMismatch
MissingCapacityWitness
CapacityWitnessIdentityMismatch
CapacityWitnessDisconnected
CapacityWitnessOverlap
CapacityWitnessBlockedByVillage
```

`SiteReservationValidationViolation` immutable fields:

```text
SiteReservationValidationViolationCode Code
SiteReservationValidationRule Rule
string FirstId
string SecondId
int SectorIndex
int MeasuredValue
int ExpectedValue
string Message
```

violations는 rule enum, code enum, first/second ID ordinal, sector index, measured/expected, message ordinal로 sort/dedupe한다. path/stack/time/thread/culture exception text를 포함하지 않는다.

## Diagnostics Contract

`SiteReservationValidationDiagnostics` immutable fields:

```text
IReadOnlyList<SiteReservationRuleResult> Rules
int ReservationCount
int ReservedSectorCount
int UnreservedSectorCount
int EntryAnchorCount
int RequiredEntryCount
int NonVillageDistanceConstraintCount
int VillageDistanceCheckCount
int CoreClusterCheckCount
int CoreWitnessCount
int CoreWitnessSectorCount
int CoreSeedCount
int ViolationCount
```

starter Completed exact:

```text
rules = 6/6 PASS
reservations = 7
reserved/unreserved sectors = 8/161
entry anchors/required = 6/6
non-Village/Village/cluster checks = 15/6/1
witnesses/witness sectors/Core seeds = 4/20/4
violations = 0
```

generic supported Village `2x1/1x2`면 reserved count는 `9`, unreserved `160`이지만 other exact identities는 유지한다.

## Atomic Snapshot Publication Contract

`SiteReservationSnapshotPublisher`는 `internal sealed`이며 validator가 six rule PASS 뒤에만 호출한다. public bypass API, singleton, cached current snapshot을 만들지 않는다.

### Existing six reservations

- exact selected `FootprintPlacement.Footprint`, origin, occupied mapping을 그대로 사용한다.
- Start PrimaryBiomeId empty, entry `0`이다.
- Boss/Forge/Core three PrimaryBiomeId는 matching `SpecialMapDefinition.PrimaryBiomeId` exact다.
- `FootprintPlacementEntry`를 same reservation ID의 `SiteEntryAnchor`로 변환하고 socket/sector/side/routes/flags를 그대로 보존한다.

### Village reservation

- selected candidate `W x H` complete rectangle로 final-oriented `SiteFootprint`을 만든다.
- transform은 exact `R0`; layout dimensions는 already final-oriented이며 추가 mirror/rotation하지 않는다.
- every local cell role은 canonical `VILLAGE`이고 biome/recipe empty다.
- selected entry footprint cell의 `RequiredOpenSides`만 selected side exact one, other cells는 empty다.
- 이것은 MAP03_08이 확정한 single external reservation entry를 표현한다. source Village footprint row의 `L|R` capability나 MAP10의 optional second entry를 미리 두 anchor로 publish하지 않는다.
- entry socket ID/routes/required/return flags는 approval `EntryTemplate`, footprint/exterior/side는 candidate에서 보존한다.
- PrimaryBiomeId empty, SourceDefinitionId exact `SITE_PRIMARY_VILLAGE`다.
- internal 4x4 MicroChunk cells/facilities/layout assembly를 만들지 않는다.

### Sector table

- exact indices `0..168`, coordinate/index exact, WorldGridIndex order다.
- each occupied sector exact one reserved row이며 reservation ID/kind/local coordinate/local role과 footprint cell이 일치한다.
- other sector는 exact unreserved row, null ID/kind, local `-1/-1`, empty role다.
- overlap winner/overwrite/last-write-wins가 없다.

### Core seeds

exact four `CoreBiomeSeed`:

| Source reservation | Biome | Rule | Minimum | Buffer |
|---|---|---|---:|---:|
| Forge | `BIO_ABANDONED_MILL` | `PATCH_MILL_CORE` | 4 | 1 |
| Cassia | `BIO_CASSIA_ROOT` | `PATCH_ROOT_CORE` | 5 | 1 |
| Yeast | `BIO_MOON_DOUGH` | `PATCH_DOUGH_CORE` | 5 | 1 |
| Meteor | `BIO_MOON_CRATER` | `PATCH_CRATER_CORE` | 5 | 1 |

- SeedSector는 matching witness `SeedSectorIndex`의 coordinate이며 source footprint smallest index와 exact 같다.
- source reservation ID는 final reservation ID다.
- source kind는 Forge/CoreResource이고 source footprint는 witness에 포함된다.
- patch instance ID/biome ownership/sector painting은 MAP04에서 만든다.

## `SiteReservationPublication` Contract

immutable properties/API:

```text
VillageReservationApproval SourceApproval
SiteReservationSnapshot Snapshot
IReadOnlyList<SiteReservationId> ReservationIds
int ReservationCount
int ReservedSectorCount
int EntryAnchorCount
int CoreSeedCount
bool TryGetReservationBySourceId(
    string sourceDefinitionId,
    out SiteReservation reservation)
```

- source approval reference identity를 exact 보존하고 clone/mutate하지 않는다.
- reservation IDs는 reservation order로 copied read-only exact seven list다.
- snapshot Seed는 input `worldSeed`, reservations `7`, sectors `169`, entry anchors `6`, Core seeds `4`다.
- reservation ID/source lookup은 ordinal이고 public mutable dictionary를 노출하지 않는다.
- publication constructor는 completed final snapshot만 허용한다.

## Error / Result Contract

`SiteReservationValidationErrorCode` exact frozen order:

```text
MissingApproval
InvalidApproval
MissingSpecialMaps
NullSpecialMap
DuplicateSpecialMapId
MissingRequiredSpecialMap
UnexpectedSpecialMap
InvalidSpecialMap
MissingFootprintCells
NullFootprintCell
DuplicateFootprintCell
MissingRequiredFootprintCell
UnexpectedFootprintCell
InvalidFootprintCell
MissingEntrySockets
NullEntrySocket
DuplicateEntrySocket
MissingRequiredEntrySocket
UnexpectedEntrySocket
InvalidEntrySocket
SelectionIdentityMismatch
VillageIdentityMismatch
CapacityIdentityMismatch
DefinitionIdentityMismatch
InternalInvariantViolation
```

`SiteReservationValidationError` immutable fields:

```text
SiteReservationValidationErrorCode Code
string DefinitionId
string ChildId
int SectorIndex
string Message
```

code, definition ID, child ID, sector index, message ordinal로 sort/dedupe한다. stable non-empty message만 사용하며 exception text를 포함하지 않는다.

`SiteReservationValidationStatus` exact values:

```text
Completed
ValidationRejected
InvalidInput
```

`SiteReservationValidationResult` immutable properties:

```text
SiteReservationValidationStatus Status
bool Succeeded
bool RetryRequired
SiteReservationPublication Publication
SiteReservationValidationDiagnostics Diagnostics
IReadOnlyList<SiteReservationValidationViolation> Violations
IReadOnlyList<SiteReservationValidationError> Errors
```

- Completed: publication/diagnostics non-null, exact rules `6/6 PASS`, violations/errors `0`, retry false.
- ValidationRejected: publication null, diagnostics non-null, violations `>=1`, errors `0`, retry true.
- InvalidInput: publication/diagnostics null, violations `0`, errors `>=1`, retry false.
- rejected/invalid result에 partial reservation/sector/core-seed/snapshot을 publish하지 않는다.
- model construction의 unexpected invariant failure는 stable `InternalInvariantViolation` 하나로 atomic failure하고 partial object/exception text를 노출하지 않는다. expected invalid input을 exception control flow로 처리하지 않는다.

## Exact Starter Integration Gates

MAP03_06 fixture -> MAP03_07 checker -> MAP03_08 selector로 approval을 만든 뒤 validator를 실행한다.

```text
source selection plan = 6
Village = 1
final reservations = 7
starter occupied/reserved sectors = 8
sector table = 169
entry anchors = 6
non-Village distance constraints = 15/15
Village distance checks = 6/6
Core cluster = 1/1
capacity witnesses/sectors/overlap = 4/20/0
Core seeds = 4
validation rules = 6/6
violations/errors = 0/0
RNG consumed by this Task = 0
```

fresh full starter seeds `0`, `4660`, `ulong.MaxValue`는 각각 `Completed`이며 input world seed가 snapshot에 exact 보존돼야 한다. same approval/definitions으로 fresh/reused validator 100회 실행하면 exact reservation IDs/orders/footprints/entries/sectors/Core seeds/rules/diagnostics snapshot이 같다.

## Determinism / Ownership

- special map/footprint/entry caller collection order와 array/list 구현이 달라도 same output이다.
- all IDs/order/pairs/sectors/rules/violations/errors는 frozen canonical order다.
- RNG stream 생성/조회/소비, `System.Random`, `UnityEngine.Random` 사용 `0`이다.
- `en-US`/`tr-TR`, wall clock, frame, thread, filesystem에 무관하다.
- approval/plan/Village/witness/definitions/source collections를 mutate하지 않는다.
- public setters/fields, mutable collection exposure, static mutable cache, lazy public enumeration이 없다.

## Scope Boundary / DO NOT

- MAP03_06 ranking/RNG/backtracking 또는 MAP03_08 bucket/layout selection을 재실행하지 않는다.
- capacity witness를 re-flood/reallocate/shrink하거나 site/Village를 local repair하지 않는다.
- validation failure에서 fallback/redraw/closest-sector overwrite 금지
- generated_special_sites/world_sectors CSV row/serializer/file I/O 금지
- patch instance ID, biome PatchId/painting/growth 금지
- route graph/entry connection, microchunk/tile reachability, Village facilities 금지
- `PASS_SITE` adapter/root artifact transaction/retry/attempt increment 금지
- overlay/Gizmo/EditorWindow/Scene visual 금지
- 100,000-seed batch/exit gate 금지
- existing MAP03_01~08 models/tests 수정 금지
- Authoring/generated CSV/meta, asmdef/asmref, Scene/Prefab, Package/ProjectSettings 변경 금지
- test skip/ignore/assertion 완화, Git operation 금지
- MAP03_10 선행 작업 금지

## Collision Handling

1. 신규 destination이 없으면 생성한다.
2. exact 계약과 바이트 동일한 preexisting destination만 `PREEXISTING_IDENTICAL`로 재사용한다.
3. 다르면 overwrite/merge/delete하지 않고 `STATUS: BLOCKED`다.
4. 기존 `.meta` GUID와 사용자 변경을 보존한다.

## Required Tests

`SiteReservationValidatorTests.cs` actual NUnit cases 최소 `260`개다.

minimum groups:

- approval/special-map/footprint-cell/entry-socket null/missing/duplicate/unexpected/active/identity aggregation
- exact source inventory `6 maps / 7 cells / 6 entries` and parent/composite-key coverage
- selected plan/Village/witness/source-definition identity mismatch gates
- exact reservation order and seven `RSV_{D2}_{source}` IDs
- required kind/source counts `1/1/1/3/1`, missing/duplicate/unexpected vectors
- exhaustive 169 world index/coordinate/origin/local mapping and no wrap/clamp
- seven-footprint pair overlap, union conservation, first/second owner evidence
- occupied-vs-entry collisions and shared unreserved entry exterior allowance
- non-Village exact `6/15/15`, minimum distribution and below-threshold vectors
- Village complete-footprint Start bucket and five other-site distance vectors
- three-Core `4x4` cluster pass/fail exact threshold
- Start zero entry, six special entries, boundary/outward/exterior/route/required/return checks
- entry own-footprint/other-footprint/outside/direction/socket identity violations
- four witness ownership/footprint/mandatory/target/connectivity/pairwise overlap vectors
- Village-vs-witness block and exact `4/20/0` starter evidence
- six rule result order/pass/count and accumulated independent violations
- deterministic seven reservations, 169 sector rows, exact reserved/unreserved mapping
- Village synthetic complete rectangle/R0/VILLAGE role/selected required-open-side mapping
- exact four CoreBiomeSeeds IDs/biomes/rules/minimums/buffer/seed sectors
- `SiteReservationPublication` source identity/lookups/read-only snapshot
- Completed/ValidationRejected/InvalidInput retry/partial-publication invariants
- diagnostic/violation/error exact order/dedupe/stable message invariants
- full starter seeds `0/4660/ulong.MaxValue`: `7 / 8 / 169 / 6 / 4 / 6/6`
- fresh/reused validator 100-run identity, reversed/shuffled array/list stability
- `en-US`/`tr-TR` culture and caller mutation isolation
- RNG draw delta `0`, public mutation-surface/dependency audit
- serializer/CSV/patch/biome/route/layout/pass/root/overlay/batch dependency `0`

금지:

- `[Ignore]`, `[Explicit]`, inconclusive, assumption-based skip
- reflection으로 private state를 바꿔 success를 만드는 test
- test order/current filesystem/wall clock 의존
- fixed seed selected coordinate/layout answer lookup

## Regression / Verification

```text
New SiteReservationValidatorTests: >=260 PASS
MAP03_08 VillageReservationSelectorTests: 339/339 PASS
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
Targeted Game.Map.Tests.EditMode: >=3604/3604 PASS
Full project EditMode: >=3644/3644 PASS
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
Assets meta files = 3054
duplicate GUID groups = 0
Authoring CSV = 50
Authoring CSV meta = 50
```

after expected:

```text
new C# = 9
new matching .cs.meta = 9
Assets meta files = 3063
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

`REPORTS/MAP03_09_IMPLEMENT_SITE_RESERVATION_VALIDATOR_RESULT.md` 필수:

- `STATUS: PASS|BLOCKED|FAIL`
- implementation summary / exact changed-created paths
- approval/source definition/preflight identity evidence
- exact required counts/reservation IDs/orders evidence
- world-bound/overlap/distance/entry/Core capacity six-rule evidence
- atomic reservation/sector/entry/Core-seed/snapshot publication evidence
- starter three-seed `7 / 8 / 169 / 6 / 4 / 6/6 / RNG delta 0` evidence
- status/publication/diagnostics/violation/error/determinism/immutability evidence
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
- MAP03_09만 `COMPLETE`, `MAP03_10_CREATE_SITE_RESERVATION_OVERLAY`는 계속 `LOCKED`, 다른 future task도 `LOCKED`

BLOCKED 전제:

- collision/baseline/precondition/change-budget 문제가 있으면 Assets/status/master를 수정하지 않는다.
- Result에 exact blocker와 필요한 repair patch 범위를 기록한다.

Recommended Commit: `feat(map): validate and publish site reservations`
