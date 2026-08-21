# MAP03_07 — Implement Core Capacity Flood Check

```yaml
status_control:
  task_key: MAP03_07_IMPLEMENT_CORE_CAPACITY_FLOOD_CHECK
  result_file: REPORTS/MAP03_07_IMPLEMENT_CORE_CAPACITY_FLOOD_CHECK_RESULT.md
```

## TASK TYPE

```text
RUNTIME DETERMINISTIC CARDINAL FLOOD CAPACITY HARD GATE + DISJOINT CORE WITNESS ALLOCATION + EDITMODE TESTS
```

## Objective

MAP03_06의 잠정 six-site `SiteReservationSelectionPlan`에서 실제 Core seed가 되는 아래 네 site를 검사한다.

```text
Forge 1 + CoreResource 3 = Core capacity requirements 4
```

각 selected footprint 전체를 seed로 사용하고 exact Core rule의 cardinal buffer ring과 최소 Core sector count를 동시에 만족하는 connected capacity가 있는지 검사한다. 네 site가 같은 sector를 capacity 증거로 중복 사용하는 것을 막기 위해 canonical order로 서로 겹치지 않는 deterministic witness를 만든다.

capacity가 부족하면 선택 plan을 수정하거나 다른 option을 고르지 않는다. `CapacityRejected / RetryRequired`를 반환해 caller가 fresh attempt의 whole `PASS_SITE`를 재시도하게 한다.

이 Task의 성공 산출물은 **기존 selection plan + four immutable capacity witnesses를 묶은 `CoreCapacityApproval`**이다. MAP03_08 Village가 아직 없으므로 reservation ID, `CoreBiomeSeed`, `SiteReservation`, `SectorReservation[169]`, final snapshot은 만들지 않는다.

## 전체 연결

```text
MAP03_06 provisional six-site selection plan
  -> MAP03_07 this Task: Forge/Core 3 footprint + buffer + minimum-capacity flood approval
  -> MAP03_08 Village reservation
  -> MAP03_09 final site reservation validation/publication boundary
  -> MAP04 Core patch seed initialization/growth
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
12. `REPORTS/MAP03_06_IMPLEMENT_RESERVATION_BACKTRACKING_RESULT.md`

MAP03_06 Result의 exact `STATUS: PASS`, focused `248/248`, starter options/draws `3156/3156`, selected placements `6`, final distance `15/15`, regressions `270/270 / 239/239 / 170/170 / 268/268 / 81/81 / 667/667`, targeted `2790/2790`, full `2830/2830`, final Assets meta `3036`, existing Assets modification `0`을 확인한다.

MAP03_06 Result `NEXT`에 적힌 `MAP03_07_IMPLEMENT_CAPACITY_FLOOD_VALIDATION`은 205개 Master/Status에 존재하지 않는 설명상 별칭이다. 상태 전이의 authoritative exact key는 기존 backlog의 `MAP03_07_IMPLEMENT_CORE_CAPACITY_FLOOD_CHECK`이며 별칭 때문에 prior PASS를 repair하거나 Assets를 되돌리지 않는다.

## Map Package Reference

exact installed Map Package v1.0이 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/01_WORLD_FIXED_SPEC.md
01_FIXED_SPEC/02_COORDINATE_AND_ID_RULES.md
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
01_FIXED_SPEC/05_IMPLEMENTATION_ORDER.md
02_PHASE_ROADMAP/MAP03_SPECIAL_SITE_RESERVATION.md
02_PHASE_ROADMAP/MAP04_BIOME_PATCH_GENERATOR.md
04_CSV_STARTER/biome_types.csv
04_CSV_STARTER/biome_patch_rules.csv
04_CSV_STARTER/special_map_catalog.csv
```

reference가 없으면 이 Task의 frozen flood/buffer/witness contracts와 existing immutable APIs를 authoritative fallback으로 사용한다. Authoring CSV를 직접 읽거나 재파싱하지 않는다.

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
Assets/_Game/Map/Runtime/WorldGeneration/Generation/RngStreamScope.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/DeterministicRngStream.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/DeterministicRngStreamFactory.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationRngStreams.cs
```

### Existing grid / candidate / placement / distance / cost / search models

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedSectorRole.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedWorldData.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGridIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorNeighborIndices.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GridInitializationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GridInitializationPass.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteOriginCandidate.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateGroup.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCatalog.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateEnumerationError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateEnumerationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateEnumerator.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteFootprint.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacementEntry.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacement.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacementBlockers.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacementResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacementSolver.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SitePlacementKey.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceIndexResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistancePolicy.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceEvaluationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceIndexBuilder.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCostWeights.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCostContext.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCostBreakdown.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCostError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCostResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCostCalculator.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSearchOption.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSearchGroup.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSearchLimits.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SitePlacementConflictDetector.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSearchDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSelectionPlan.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSearchResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationBacktracker.cs
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
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

위 C#/asmdef와 matching meta, approved `Generation` Runtime/Test 직계 파일명의 path-only inventory, Authoring CSV/meta count·hash, 전체 meta GUID, task marker 이후 change-scope path만 확인할 수 있다.

금지:

- Authoring CSV body 직접 재파싱·수정
- MAP03_08 이후 Task body
- unrelated production/test C# body
- Legacy/Stage/P6/P11 generator body
- Scene/Prefab YAML

## WRITE ALLOWLIST

### 신규 Runtime production C# — exact 8

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityRequirement.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityFloodWitness.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityApproval.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityFloodDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityFloodRejection.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityFloodError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityFloodResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityFloodChecker.cs
```

### 신규 Runtime EditMode test — exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/CoreCapacityFloodCheckerTests.cs
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

## Exact Required Capacity Sites / Order

selection plan은 MAP03_06 exact six steps를 가져야 하며 capacity requirement는 아래 네 selected placement와 exact 대응한다.

```text
capacity 0: FORGE / SITE_MOON_SEAL_FORGE / 0
capacity 1: CORE_RESOURCE / SITE_CASSIA_SAP_HEART / 0
capacity 2: CORE_RESOURCE / SITE_DEEP_STAR_YEAST / 0
capacity 3: CORE_RESOURCE / SITE_MOON_CORE_METEOR / 0
```

- Start와 Boss는 selected footprint blocker에는 포함하지만 Core capacity requirement가 아니다.
- Village requirement/selection은 이 Task에서 거부한다.
- requirement caller insertion order는 위 canonical order를 바꾸지 않는다.
- exact four requirements가 아니면 structural `InvalidInput`이다.

## `CoreCapacityRequirement` Contract

constructor/public immutable properties:

```text
CoreCapacityRequirement(
    SitePlacementKey key,
    FootprintPlacement placement,
    SpecialMapDefinition specialMap,
    BiomeTypeDefinition primaryBiome,
    BiomePatchRuleDefinition corePatchRule)

SitePlacementKey Key
FootprintPlacement Placement
SpecialMapDefinition SpecialMap
BiomeTypeDefinition PrimaryBiome
BiomePatchRuleDefinition CorePatchRule
```

- input envelope는 reference를 바꾸거나 definition을 clone/mutate하지 않는다.
- checker가 null/identity/active/range를 accumulated structural error로 검증할 수 있도록 constructor는 config validation을 대신하지 않는다.
- key와 placement는 selection plan의 selected identity와 exact 일치해야 한다.
- placement equivalence는 key, candidate origin/index/ordinal, transform, occupied sector index list, transformed entry socket/exterior snapshot이 모두 exact 같은 것이다. reference equality만 요구하지 않는다.
- special map ID/role/primary biome, biome ID, active exact Core rule identity를 보존한다.

starter exact requirements:

| Site | Biome | CORE rule | Min | Max | Buffer | Touch edge |
|---|---|---|---:|---:|---:|---|
| Forge | `BIO_ABANDONED_MILL` | `PATCH_MILL_CORE` | 4 | 14 | 1 | false |
| Cassia | `BIO_CASSIA_ROOT` | `PATCH_ROOT_CORE` | 5 | 18 | 1 | false |
| Yeast | `BIO_MOON_DOUGH` | `PATCH_DOUGH_CORE` | 5 | 18 | 1 | true |
| Meteor | `BIO_MOON_CRATER` | `PATCH_CRATER_CORE` | 5 | 18 | 1 | true |

`BiomeTypeDefinition.MinCorePatchCount == 1`, rule role exact `CORE`, rule active true, `1 <= MinSectorCount <= MaxSectorCount <= 169`, `0 <= BufferRingSectors <= 12`를 검증한다.

## Public Checker API

```text
public sealed class CoreCapacityFloodChecker

CoreCapacityFloodResult Check(
    SiteReservationSelectionPlan selectionPlan,
    IEnumerable<CoreCapacityRequirement> requirements)
```

checker는 Registry, RNG, clock, filesystem, current Root/pass state를 자체 조회하지 않는다.

## Structural Preflight

RNG나 output allocation 전에 아래를 모두 accumulated/sorted validation한다.

- selection plan은 non-null이며 MAP03_06 exact six steps/depth/key order/selected count를 가진다.
- selection placement는 non-null, world-bound, key/option identity exact, occupied footprint overlap `0`이다.
- requirements collection/items는 non-null, key unique, exact four key set이다.
- requirement placement는 plan의 matching selected placement와 exact equivalent다.
- special map/biome/Core rule은 non-null, active, canonical identity/range가 위 계약과 일치한다.
- Forge exact one, CoreResource exact three, Start/Boss/Village requirement zero다.
- undefined enum/numeric cast, duplicate occupied sector, invalid entry exterior identity를 거부한다.
- structural invalid input은 flood를 시작하지 않고 `InvalidInput`, retry false, approval/diagnostics/rejections null-or-empty를 반환한다.

`CoreCapacityFloodErrorCode` exact frozen order:

```text
MissingSelectionPlan
InvalidSelectionPlan
MissingRequirements
NullRequirement
DuplicateRequirementKey
MissingRequiredRequirement
UnexpectedRequirement
InvalidRequirement
PlacementNotSelected
PlacementIdentityMismatch
MissingSpecialMap
InvalidSpecialMap
MissingPrimaryBiome
InvalidPrimaryBiome
MissingCorePatchRule
InvalidCorePatchRule
DefinitionIdentityMismatch
InvalidFootprint
InternalInvariantViolation
```

error는 code, site source ID canonical-or-empty, biome/rule ID canonical-or-empty, sector index `0..168|-1`, stable non-empty message를 보존한다. code, site ID, biome ID, rule ID, sector index, message ordinal로 sort/dedupe하며 path/stack/time/thread/culture exception text를 포함하지 않는다.

## Fixed Grid / Neighbor Contract

```text
grid width/height = 13/13
valid indices = 0..168
index = y*13+x
connectivity = cardinal only
diagonal connectivity = false
```

- 모든 output sector list는 `WorldGridIndex` 오름차순 copied read-only snapshot이다.
- traversal neighbor는 valid cardinal neighbors를 sector index 오름차순으로 방문한다.
- world wrap/clamp/diagonal/corner cutting이 없다.
- entry exterior는 일반 biome sector가 될 수 있으므로 capacity hard blocker가 아니다.

## Mandatory Buffer Contract

site footprint occupied sectors 전체가 multi-source seed다.

world sector `s`의 footprint distance:

```text
FootprintDistance(s) = min ManhattanDistance(s, footprintCell)
```

mandatory in-world buffer set:

```text
MandatoryBuffer = all in-world sectors where
                  FootprintDistance <= CorePatchRule.BufferRingSectors
```

- MandatoryBuffer는 footprint 전체를 포함한다.
- sparse/multi-cell footprint는 각 occupied cell union의 graph-distance ring을 사용한다.
- rectangle bounding box, Chebyshev/diagonal ring, origin-only ring을 사용하지 않는다.
- theoretical cardinal ring이 world 밖으로 나가고 `CanTouchWorldEdge == false`면 `BufferOutsideWorld` hard rejection이다.
- `CanTouchWorldEdge == true`면 outside theoretical cells는 무시하고 in-world portion만 mandatory다.
- outside theoretical coordinates는 `(x,y)` unique set으로 count한다. clamp/wrap해 in-world cell로 바꾸지 않는다.

hard blockers:

- matching own selected footprint: mandatory/available
- 다른 five selected placement의 occupied footprints: unavailable
- 다른 Core requirement의 mandatory buffer: unavailable/owned by that requirement
- selected entry exterior sectors: available; blocker 아님

mandatory buffer sector가 다른 selected footprint와 겹치면 `BufferBlockedBySelectedFootprint`다. 두 Core mandatory buffers가 겹치면 pair 양쪽에 `MandatoryBufferOverlap`을 기록한다. rejection을 숨기기 위해 buffer를 줄이거나 sector를 빼지 않는다.

## Required Capacity Contract

각 site의 exact capacity target:

```text
MinimumCoreSectorCount = CorePatchRule.MinSectorCount
RequiredWitnessSectorCount = max(
    MinimumCoreSectorCount,
    MandatoryBuffer.Count)
```

이 count는 future CorePatch가 반드시 포함할 own footprint + full in-world buffer + rule minimum을 모두 만족하는 최소 증거다. `MaxSectorCount`까지 채우거나 biome을 칠하지 않는다.

starter 1x1/buffer-1의 interior mandatory count는 `5`다. 따라서 Forge의 target은 `max(4,5)=5`, three Core target은 각각 `max(5,5)=5`; approved starter total witness count는 exact `20`이다. edge-touch Core의 truncated mandatory set이 5보다 작으면 connected additional cells로 exact target 5까지 보충한다.

## Exact Flood / Joint Witness Algorithm

preflight와 mandatory-buffer hard gate 후:

### A. Independent connected capacity

각 requirement에 대해 own MandatoryBuffer를 multi-source queue seed로 full cardinal flood한다.

eligible sector:

```text
in world
AND not occupied by another selected placement
AND not owned by another requirement's MandatoryBuffer
```

- own footprint/buffer와 entry exterior는 eligible이다.
- queue seeds와 neighbor는 sector index ascending이다.
- every eligible sector는 한 번만 visit한다.
- `ReachableSectorIndices`는 own mandatory set과 연결된 entire eligible component다.
- reachable count가 RequiredWitnessSectorCount보다 작으면 exact shortfall로 `InsufficientConnectedCapacity`다.

### B. Disjoint deterministic witness allocation

independent capacity가 four sites 모두 통과한 뒤 exact capacity order로 최소 witness를 할당한다.

1. 네 MandatoryBuffer set을 먼저 각 owner에게 claim한다.
2. current witness가 target보다 작으면 current witness 전체를 seed로 canonical cardinal BFS한다.
3. 다른 selected footprint, 다른 mandatory owner, 이전 site가 claim한 witness sector를 통과하거나 claim하지 않는다.
4. BFS distance ascending, 같은 distance는 sector index ascending으로 available cell을 claim한다.
5. target에 도달하면 해당 site allocation을 즉시 끝내고 다음 site로 이동한다.
6. target 전 frontier가 고갈되면 `InsufficientDisjointCapacity`이며 이후 site allocation은 수행하지 않는다.

각 witness는 own footprint와 mandatory buffer를 포함하고 cardinal-connected다. four witness sector sets는 pairwise disjoint다. greedy order는 위 exact Forge/Cassia/Yeast/Meteor order이며 alternative matching/max-flow/랜덤 tie-break로 바꾸지 않는다.

capacity failure에서 MAP03_06 option을 재선택하거나 기존 plan을 mutate하지 않는다. caller가 whole `PASS_SITE`를 fresh attempt로 재실행한다.

## `CoreCapacityFloodWitness` Contract

immutable properties/API:

```text
SitePlacementKey Key
string BiomeId
string CorePatchRuleId
int SeedSectorIndex
int MinimumCoreSectorCount
int BufferRingSectors
bool CanTouchWorldEdge
int RequiredWitnessSectorCount
int AvailableConnectedSectorCount
IReadOnlyList<int> FootprintSectorIndices
IReadOnlyList<int> MandatoryBufferSectorIndices
IReadOnlyList<int> ReachableSectorIndices
IReadOnlyList<int> WitnessSectorIndices
int AdditionalClaimedSectorCount
bool ContainsWitnessSector(int sectorIndex)
```

- `SeedSectorIndex`는 footprint sector 중 smallest WorldGridIndex이며 단일-cell seed로 flood 범위를 축소하지 않는다.
- Footprint ⊆ MandatoryBuffer ⊆ Reachable, Footprint ⊆ Witness다.
- Witness는 MandatoryBuffer 전체를 포함하고 count가 RequiredWitnessSectorCount와 exact 같다.
- AdditionalClaimed = Witness count - MandatoryBuffer count다.
- reachable list는 independent component evidence이며 다른 site reachable list와 겹칠 수 있다.
- witness list만 cross-site pairwise disjoint ownership proof다.

## `CoreCapacityApproval` Contract

immutable properties/API:

```text
SiteReservationSelectionPlan SelectionPlan
IReadOnlyList<CoreCapacityFloodWitness> Witnesses
int CapacitySiteCount
int TotalWitnessSectorCount
bool TryGetWitness(SitePlacementKey key, out CoreCapacityFloodWitness witness)
```

- exact four witnesses를 canonical capacity order로 보관한다.
- every witness가 selection plan matching placement identity와 정의 identity를 보존한다.
- total witness count는 checked sum이며 starter exact `20`이다.
- selection plan과 source lists를 mutate하지 않고 copied read-only witness snapshots를 보존한다.
- reservation ID, `CoreBiomeSeed`, patch ID/owner, Village, sector reservation을 포함하지 않는다.

## Diagnostics Contract

`CoreCapacitySiteDiagnostics` immutable fields:

```text
SitePlacementKey Key
int FootprintSectorCount
int MandatoryBufferSectorCount
int OutsideTheoreticalBufferCount
int BlockedMandatoryBufferCount
int OverlappingMandatoryBufferCount
int MinimumCoreSectorCount
int RequiredWitnessSectorCount
int FloodVisitedSectorCount
int AvailableConnectedSectorCount
int WitnessSectorCount
int CapacityShortfall
```

`CoreCapacityFloodDiagnostics` immutable fields:

```text
IReadOnlyList<CoreCapacitySiteDiagnostics> Sites
int SelectedPlacementCount
int CapacitySiteCount
int ReservedFootprintSectorCount
int TotalFloodVisitedSectorCount
int TotalWitnessSectorCount
```

- site diagnostics는 exact capacity order다.
- mandatory gate failure 전 계산되지 않은 flood/witness counts는 `0`이다.
- rejected capacity shortfall은 `max(0, RequiredWitness - actual available/witness)`다.
- diagnostics는 logging/MAP03_10 overlay용 immutable facts이며 approval을 바꾸지 않는다.

## Rejection / Status Contract

`CoreCapacityFloodRejectionReason` exact frozen order:

```text
BufferOutsideWorld
BufferBlockedBySelectedFootprint
MandatoryBufferOverlap
InsufficientConnectedCapacity
InsufficientDisjointCapacity
```

`CoreCapacityFloodRejection` fields:

```text
CoreCapacityFloodRejectionReason Reason
SitePlacementKey Key
SitePlacementKey OtherKey
int SectorIndex
int RequiredCount
int AvailableCount
int Shortfall
string Message
```

- OtherKey가 없으면 default invalid key, sector가 특정되지 않으면 `-1`이다.
- counts는 non-negative이며 `Shortfall == max(0, RequiredCount - AvailableCount)`다.
- reason, key, other key, sector index, counts, message ordinal로 sort/dedupe한다.

`CoreCapacityFloodStatus` exact values:

```text
Completed
CapacityRejected
InvalidInput
```

`CoreCapacityFloodResult` immutable properties:

```text
CoreCapacityFloodStatus Status
bool Succeeded
bool RetryRequired
CoreCapacityApproval Approval
CoreCapacityFloodDiagnostics Diagnostics
IReadOnlyList<CoreCapacityFloodRejection> Rejections
IReadOnlyList<CoreCapacityFloodError> Errors
```

- Completed: approval/diagnostics non-null, rejections/errors `0`, retry false.
- CapacityRejected: approval null, diagnostics non-null, rejections `>=1`, errors `0`, retry true.
- InvalidInput: approval/diagnostics null, rejections `0`, errors `>=1`, retry false.
- rejected/invalid result에 partial approval 또는 partially claimed witness를 publish하지 않는다.

## Exact Starter Integration Gates

MAP03_06 fixture가 만드는 exact plan과 typed definitions로 requirements를 구축한다.

```text
selection placements = 6
capacity requirements = 4
Forge/Core/Core/Core minimums = 4/5/5/5
buffer rings = 1/1/1/1
approved witnesses = 4
each witness target = 5
total witness sectors = 20
cross-witness overlap = 0
Village = 0
RNG draws consumed by this Task = 0
```

fresh MAP03_06 plan seeds `0`, `4660`, `ulong.MaxValue`는 각각 capacity `Completed`여야 한다. same plan/requirements를 fresh/reused checker로 100회 실행하면 exact witness/reachable/diagnostics/rejection/error snapshot이 같다.

## Determinism / Ownership

- requirement insertion order, array/list, selected plan source collection order가 달라도 same output이다.
- cardinal neighbor order와 all public sector lists는 WorldGridIndex canonical이다.
- RNG stream 생성/조회/소비, `System.Random`, `UnityEngine.Random` 사용 `0`이다.
- seeds는 MAP03_06 selected placement를 통해서만 결과에 영향을 주며 checker 내부 randomness가 없다.
- `en-US`/`tr-TR`, wall clock, frame, thread, filesystem에 무관하다.
- plan/requirements/definitions/placements/caller collections를 mutate하지 않는다.
- public setters/fields, mutable collection exposure, static mutable cache, lazy public enumeration이 없다.

## Scope Boundary / DO NOT

- MAP03_06 option ranking/RNG/backtracking을 복제·수정·재실행하지 않는다.
- capacity failure에서 한 site 이동, alternate option 선택, local repair를 하지 않는다.
- diagonal/Chebyshev buffer, world wrap/clamp, random flood tie-break 금지
- max-flow/biome ownership optimization, CorePatch growth/painting/PatchId 할당 금지
- `CoreBiomeSeed`, reservation ID/order, SiteReservation/SectorReservation/Snapshot 생성 금지
- Village bucket/layout/selection 금지
- final required-count/distance/entry validator 금지
- full `PASS_SITE` adapter/root retry/attempt increment 금지
- generated_special_sites/biome serializer/file I/O/replay 확장 금지
- route graph/tile movement/microchunk/biome grower 금지
- existing MAP03_01~06 models/tests 수정 금지
- Authoring/generated CSV/meta, asmdef/asmref, Scene/Prefab, Package/ProjectSettings 변경 금지
- test skip/ignore/assertion 완화, Git operation 금지
- MAP03_08 선행 작업 금지

## Collision Handling

1. 신규 destination이 없으면 생성한다.
2. exact 계약과 바이트 동일한 preexisting destination만 `PREEXISTING_IDENTICAL`로 재사용한다.
3. 다르면 overwrite/merge/delete하지 않고 `STATUS: BLOCKED`다.
4. 기존 `.meta` GUID와 사용자 변경을 보존한다.

## Required Tests

`CoreCapacityFloodCheckerTests.cs` actual NUnit cases 최소 `180`개다.

minimum groups:

- requirement null/reference/identity/typed-definition/read-only invariants
- exact plan six-step gate and exact four capacity key/order/count
- missing/null/duplicate/unexpected/Village requirement invalid-input aggregation
- placement equivalence key/origin/ordinal/transform/occupied/entry mismatch gates
- starter exact biome/Core rule `4/5/5/5`, max `14/18/18/18`, buffer `1`, edge flags
- WorldGridIndex exhaustive `169`, cardinal neighbor/index/no-wrap/no-diagonal vectors
- multi-source Manhattan distance and sparse/multi-cell footprint union vectors
- buffer radius `0/1/2`, sorted set, footprint inclusion, Chebyshev exclusion
- non-edge-touch outside rejection and edge-touch truncated-buffer approval
- other selected footprint buffer block and pairwise mandatory-buffer overlap evidence
- selected entry exterior is available and does not trigger blocker rejection
- minimum-vs-buffer `max` target formula and Forge exact target `5`
- connected flood corridor/pocket/wall, insufficient count/shortfall evidence
- disjoint witness canonical order, earlier claim blocking, insufficient-disjoint rejection
- each witness connected, mandatory-complete, exact target, pairwise overlap `0`
- Completed/CapacityRejected/InvalidInput result/retry/partial-publication invariants
- diagnostic/rejection/error exact order/dedupe/count/read-only invariants
- full starter seeds `0/4660/ulong.MaxValue` exact `4` witnesses / total `20` / RNG draw delta `0`
- reversed/shuffled requirements and array/list stability
- same input fresh/reused checker 100-run identity
- `en-US`/`tr-TR` culture and caller mutation isolation
- public mutation-surface/dependency audit
- Village/CoreBiomeSeed/final reservation/biome growth/pass/root/file-I/O production dependency `0`

금지:

- `[Ignore]`, `[Explicit]`, inconclusive, assumption-based skip
- reflection으로 private state를 바꿔 success를 만드는 test
- test order/current filesystem/wall clock 의존
- fixed seed answer lookup/hard-coded selected coordinate answer

## Regression / Verification

```text
New CoreCapacityFloodCheckerTests: >=180 PASS
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
Targeted Game.Map.Tests.EditMode: >=2970/2970 PASS
Full project EditMode: >=3010/3010 PASS
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
Assets meta files = 3036
duplicate GUID groups = 0
Authoring CSV = 50
Authoring CSV meta = 50
```

after expected:

```text
new C# = 9
new matching .cs.meta = 9
Assets meta files = 3045
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

`REPORTS/MAP03_07_IMPLEMENT_CORE_CAPACITY_FLOOD_CHECK_RESULT.md` 필수:

- `STATUS: PASS|BLOCKED|FAIL`
- implementation summary / exact changed-created paths
- exact four requirement/definition/plan identity evidence
- cardinal grid/Manhattan buffer/outside-edge vectors
- mandatory blocker/overlap and independent capacity evidence
- disjoint witness order/connectivity/shortfall/retry evidence
- full starter three-seed `4 witnesses / 20 sectors / RNG delta 0` evidence
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
- MAP03_07만 `COMPLETE`, `MAP03_08_IMPLEMENT_VILLAGE_RESERVATION`은 계속 `LOCKED`, 다른 future task도 `LOCKED`

BLOCKED 전제:

- collision/baseline/precondition/change-budget 문제가 있으면 Assets/status/master를 수정하지 않는다.
- Result에 exact blocker와 필요한 repair patch 범위를 기록한다.

Recommended Commit: `feat(map): validate connected core site capacity`
