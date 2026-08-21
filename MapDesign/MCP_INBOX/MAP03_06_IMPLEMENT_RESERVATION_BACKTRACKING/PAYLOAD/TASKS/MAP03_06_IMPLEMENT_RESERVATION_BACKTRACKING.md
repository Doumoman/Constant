# MAP03_06 — Implement Reservation Backtracking

```yaml
status_control:
  task_key: MAP03_06_IMPLEMENT_RESERVATION_BACKTRACKING
  result_file: REPORTS/MAP03_06_IMPLEMENT_RESERVATION_BACKTRACKING_RESULT.md
```

## TASK TYPE

```text
RUNTIME DETERMINISTIC SIX-GROUP OPTION ORDERING + COLLISION/HARD-CONSTRAINT FILTERING + DEPTH-FIRST BACKTRACKING + EDITMODE TESTS
```

## Objective

MAP03_03의 성공 placement options, MAP03_04의 distance policy, MAP03_05의 cost calculator를 조합해 아래 exact six groups에서 각각 하나를 선택한다.

```text
Start 1 + Boss 1 + Forge 1 + CoreResource 3 = selected placements 6
```

후보는 soft `TotalCost`가 낮은 순서로 시도하며 같은 cost 안에서는 fresh `RNG_WORLD_SITE` draw를 stable tie-break로 사용한다. 새 option이 기존 선택과 footprint/entry 충돌하거나 distance/three-Core-cluster hard signal을 위반하면 거부한다. 현재 group의 후보가 고갈되면 직전 group 선택을 취소하고 다음 option을 시도한다.

이 Task의 성공 산출물은 **잠정 `SiteReservationSelectionPlan`**이다. MAP03_07의 실제 Core capacity flood와 MAP03_08 Village가 아직 없으므로 `SiteReservation`, `SectorReservation[169]`, `SiteReservationSnapshot`, reservation ID를 확정하지 않는다.

## 전체 연결

```text
MAP03_02 raw origins
  -> MAP03_03 valid transformed placements
  -> MAP03_04 footprint distances / hard policy
  -> MAP03_05 one-option costs / hard signals
  -> MAP03_06 this Task: six-option combination selection + backtracking
  -> MAP03_07 actual Core capacity hard gate
  -> MAP03_08 Village
  -> MAP03_09 final reservation validation/publication boundary
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
12. `REPORTS/MAP03_05_IMPLEMENT_SITE_CANDIDATE_COST_RESULT.md`

MAP03_05 Result의 exact `STATUS: PASS`, focused `270/270`, exact weights `10/25/1000/100/10000`, aggregate `11145`, regressions `239/239 / 170/170 / 268/268 / 81/81 / 667/667`, targeted `2542/2542`, full `2582/2582`, final Assets meta `3027`, existing Assets modification `0`을 확인한다.

## Map Package Reference

exact installed Map Package v1.0이 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/01_WORLD_FIXED_SPEC.md
01_FIXED_SPEC/02_COORDINATE_AND_ID_RULES.md
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
01_FIXED_SPEC/05_IMPLEMENTATION_ORDER.md
02_PHASE_ROADMAP/MAP03_SPECIAL_SITE_RESERVATION.md
04_CSV_STARTER/generation_passes.csv
```

reference가 없으면 이 Task의 frozen search/RNG/budget contracts와 existing immutable APIs를 authoritative fallback으로 사용한다. Authoring CSV를 직접 읽거나 재파싱하지 않는다.

## READ ALLOWLIST

### Existing typed definitions / RNG

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

### Existing candidate / placement / distance / cost models

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGridIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GridInitializationResult.cs
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
```

### Focused tests / assemblies

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/DeterministicRngStreamTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationModelsTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteCandidateEnumerationTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/FootprintPlacementSolverTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteDistanceIndexTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteCandidateCostTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

위 C#/asmdef와 matching meta, approved `Generation` Runtime/Test 직계 파일명의 path-only inventory, Authoring CSV/meta count·hash, 전체 meta GUID, task marker 이후 change-scope path만 확인할 수 있다.

금지:

- Authoring CSV body 직접 재파싱·수정
- MAP03_07 이후 Task body
- unrelated production/test C# body
- Legacy/Stage/P6/P11 generator body
- Scene/Prefab YAML

## WRITE ALLOWLIST

### 신규 Runtime production C# — exact 8

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSearchOption.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSearchGroup.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSearchLimits.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SitePlacementConflictDetector.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSearchDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSelectionPlan.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSearchResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationBacktracker.cs
```

### 신규 Runtime EditMode test — exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationBacktrackerTests.cs
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

## `SiteReservationSearchOption` Contract

immutable properties:

```text
FootprintPlacement Placement
int FutureCoreAvailableSectorCount
```

- placement는 non-null MAP03_03 success output이다.
- capacity count는 MAP03_05 exact `-1 | 0..169` domain이다.
- option identity는 `(SitePlacementKey, Candidate.OriginIndex, Footprint.Transform)`이다.
- cost, random tie-break, rank, selected state를 option source에 저장하지 않는다.
- source placement/candidate/footprint를 mutate하지 않는다.

## `SiteReservationSearchGroup` Contract

constructor/public properties:

```text
SiteReservationSearchGroup(
    SitePlacementKey key,
    SpecialMapDefinition specialMap,
    BiomeTypeDefinition primaryBiome,
    BiomePatchRuleDefinition corePatchRule,
    IEnumerable<SiteReservationSearchOption> options)

SitePlacementKey Key
SpecialMapDefinition SpecialMap
BiomeTypeDefinition PrimaryBiome
BiomePatchRuleDefinition CorePatchRule
IReadOnlyList<SiteReservationSearchOption> Options
int OptionCount
```

- 모든 option placement key가 group Key와 exact 같다.
- option identity는 unique다.
- caller order와 무관하게 `OriginIndex`, transform enum ordinal, CandidateOrdinal 순으로 copied read-only 보관한다.
- Start group은 typed definitions null, options R0, capacity `-1`만 허용한다.
- Boss/Forge/CoreResource group은 MAP03_05가 요구하는 exact typed definition identity를 보존한다.
- Village group은 이 Task에서 거부한다.

## Exact Required Groups / Search Order

distance policy와 groups의 key set은 exact 동일한 six keys다.

```text
depth 0: START / WORLD_MOONPALACE_V1 / 0
depth 1: BOSS / SITE_MOON_BOSS_VAULT / 0
depth 2: FORGE / SITE_MOON_SEAL_FORGE / 0
depth 3: CORE_RESOURCE / SITE_CASSIA_SAP_HEART / 0
depth 4: CORE_RESOURCE / SITE_DEEP_STAR_YEAST / 0
depth 5: CORE_RESOURCE / SITE_MOON_CORE_METEOR / 0
```

이 order는 `SitePlacementKey` priority/source/instance canonical order다. Start anchor를 먼저 고정한 뒤 largest Boss, Forge, three Core 순으로 진행한다. caller group insertion order가 search order를 바꾸지 않는다.

starter integration input은 MAP03_03 empty-blocker success placements exact:

```text
Start 88
Boss 572
Forge 624
Cassia 624
Deep Star Yeast 624
Moon Core 624
Total options 3156
```

## `SiteReservationSearchLimits` Contract

immutable property:

```text
int MaxFailedCombinations
static SiteReservationSearchLimits Default { get; }
```

- exact default와 production maximum은 `200`이다.
- custom test value는 `1..200`; `0`, negative, `>200`을 거부한다.
- candidate rejection count는 이 budget이 아니다.
- `FailedCombinationCount`는 deeper dead-end 때문에 선택 option을 pop한 횟수이며 `BacktrackCount`와 exact 같다.
- 200번째 failed combination을 기록하면 즉시 `FailedCombinationLimitReached`; 201번째를 시도하지 않는다.

## `SitePlacementConflictDetector` Contract

candidate placement와 current selected placements의 exact pairwise compatibility를 pure하게 평가한다.

`SiteReservationRejectionReason` exact order:

```text
FootprintOverlap
BlocksExistingEntryApproach
EntryApproachOccupied
DistanceConstraint
CoreCluster
```

collision reasons:

1. candidate occupied ∩ selected occupied -> `FootprintOverlap`
2. candidate occupied ∩ selected entry exterior -> `BlocksExistingEntryApproach`
3. candidate entry exterior ∩ selected occupied -> `EntryApproachOccupied`

- selected/candidate entry exteriors가 같은 일반 sector를 공유하는 것은 MAP03_03 계약대로 허용한다.
- diagonal/one-cell buffer/entry-facing/route compatibility를 새로 만들지 않는다.
- multiple independent reasons는 canonical order로 모두 반환한다.
- source placements를 mutate하지 않는다.

## RNG / Stable Tie-Break Contract

public search는 caller가 existing factory의 exact fresh `RNG_WORLD_SITE` stream을 전달받는다.

```text
DeterministicRngStream siteRng
```

- null 또는 `DrawCount != 0` stream은 input error이며 draw `0`이다.
- selector는 다른 RNG를 생성/조회/소비하지 않는다.
- 모든 input/group/option/typed-definition preflight가 성공한 뒤에만 draw한다.
- canonical group order, canonical option order로 option마다 `NextUInt64()` exact 한 번을 호출해 stable `RandomTieBreak`를 할당한다.
- full starter input은 exact `3156` draws다. search path/backtracking 수가 draw count를 바꾸지 않는다.
- state별 viable option order는 아래 exact tuple 오름차순이다.

```text
(TotalCost, RandomTieBreak, OriginIndex, TransformOrdinal, CandidateOrdinal)
```

- RNG는 equal-cost option 순서만 바꾼다. 낮은 TotalCost보다 높은 TotalCost를 앞세우지 않는다.
- common known site stream initial state `60D4B46EBF6EF00D`에서 first two tie-breaks는 `F627BD56683B33FC`, `4CA318D8E4EA97BA`; equal-cost canonical option 1이 option 0보다 먼저다.
- pass retry는 caller가 `AttemptOrdinal + 1`로 fresh site stream을 다시 만든다. 이 Task가 attempt를 증가시키거나 Root/pass를 재실행하지 않는다.

## Public Backtracker API

```text
public sealed class SiteReservationBacktracker

SiteReservationSearchResult Search(
    IEnumerable<SiteReservationSearchGroup> groups,
    SiteDistancePolicy distancePolicy,
    SiteCandidateCostWeights weights,
    SiteReservationSearchLimits limits,
    DeterministicRngStream siteRng)
```

## Preflight / Structural Gate

- groups/policy/weights/limits/siteRng은 non-null이다.
- group collection과 items는 non-null, group key unique, exact six policy key set과 일치한다.
- 각 group option은 non-null/non-empty, exact identity unique, typed definitions/capacity domain이 MAP03_05 input gate를 통과한다.
- Start exact one, Boss exact one, Forge exact one, Core exact three, Village zero다.
- policy는 exact 15 constraints를 가진 required-site policy다.
- option끼리 같은 group 안에서 overlap하는 것은 alternatives이므로 허용한다.
- structural invalid input은 RNG draw 없이 `InvalidInput`과 sorted Errors, no plan이다.

`SiteReservationSearchErrorCode` exact frozen order:

```text
MissingGroups
NullGroup
DuplicateGroupKey
MissingRequiredGroup
UnexpectedGroup
InvalidGroup
EmptyGroup
NullOption
DuplicateOptionIdentity
InvalidOption
MissingDistancePolicy
PolicyKeyMismatch
InvalidDistancePolicy
MissingWeights
MissingLimits
InvalidLimits
MissingSiteRng
SiteRngAlreadyConsumed
CostEvaluationFailed
FinalDistanceEvaluationFailed
InternalInvariantViolation
```

error는 code, group/candidate source ID canonical-or-empty, option origin index `0..168|-1`, stable non-empty message를 보존한다. code, group ID, candidate ID, origin index, message ordinal로 sort/dedupe하며 path/stack/time/thread/culture exception text를 포함하지 않는다.

## Exact Search Algorithm

preflight 후:

1. 모든 option에 stable RNG tie-break를 한 번 할당한다.
2. depth 0부터 current selected placements snapshot으로 모든 group options를 평가한다.
3. conflict detector reason이 있으면 cost 계산 없이 reject/count한다.
4. conflict가 없으면 MAP03_05 calculator를 exact group definitions/capacity forecast와 current selected context로 호출한다.
5. cost structural failure는 전체 `InvalidInput`; 그 option을 silently skip하지 않는다.
6. `DistanceUnits > 0`이면 `DistanceConstraint`, `CoreClusterUnits > 0`이면 `CoreCluster` reason을 count하고 hard reject한다. 두 reason이 동시에 존재할 수 있다.
7. hard-satisfied options를 exact order tuple로 정렬한다.
8. 첫 option을 push하고 다음 depth로 진행한다.
9. deeper depth가 exhausted이면 선택을 pop하고 failed-combination/backtrack count를 1 증가한 뒤 같은 depth의 다음 option을 시도한다.
10. depth 6에 도달하면 complete plan을 만들고 MAP03_04 index/policy complete evaluation으로 final postcondition을 확인한다.

각 동일 state visit에서 group의 모든 options를 평가해 ranking을 만든다. 같은 option은 다른 partial selection state에서 다시 평가될 수 있으며 `CandidateEvaluationCount`에 각각 포함된다.

선택 조합을 완성한 뒤 한 site만 강제로 옮기는 repair는 없다. 실패는 exact DFS pop으로만 되돌린다.

## Search Status / Retry Semantics

`SiteReservationSearchStatus` exact values:

```text
Completed
NoSolution
FailedCombinationLimitReached
InvalidInput
```

- `Completed`: SelectionPlan non-null, RetryRequired false, errors 0.
- `NoSolution`: exhaustive search가 budget 전에 끝남, plan null, RetryRequired true, errors 0.
- `FailedCombinationLimitReached`: failed combinations exact limit, plan null, RetryRequired true, errors 0.
- `InvalidInput`: plan null, RetryRequired false, errors >=1.
- 실패/invalid 결과에 partial selected list를 publish하지 않는다.

`NoSolution`/limit은 future `PASS_SITE` adapter가 whole-pass retry해야 한다는 신호다. 이 Task는 retry를 직접 실행하지 않는다.

## Diagnostics Contract

`SiteReservationGroupDiagnostics` immutable fields:

```text
SitePlacementKey Key
int SourceOptionCount
int StateVisitCount
int CandidateEvaluationCount
int SelectionPushCount
int BacktrackPopCount
int ExhaustionCount
int RejectedOptionEvaluationCount
int GetReasonCount(SiteReservationRejectionReason reason)
```

`SiteReservationSearchDiagnostics` immutable fields:

```text
IReadOnlyList<SiteReservationGroupDiagnostics> Groups
int TotalSourceOptionCount
int CandidateEvaluationCount
int SelectionPushCount
int FailedCombinationCount
int BacktrackCount
int DeepestSelectedDepth
ulong RngInitialState
ulong RngDrawCountBefore
ulong TieBreakDrawCount
ulong RngDrawCountAfter
```

- group diagnostics는 exact search order다.
- one option evaluation with two hard reasons increments rejected-option count once and both reason counts once.
- `FailedCombinationCount == BacktrackCount`, each `<=200`.
- successful exact starter input은 source options `3156`, tie-break draws `3156`, deepest depth `6`이다.
- diagnostics는 logging/overlay용 immutable facts이며 selection을 바꾸지 않는다.

## `SiteReservationSelectionPlan` Contract

`SiteReservationSelectionStep` immutable properties:

```text
int Depth
SitePlacementKey Key
SiteReservationSearchOption Option
SiteCandidateCostBreakdown IncrementalCost
ulong RandomTieBreak
int CanonicalOptionOrdinal
```

`SiteReservationSelectionPlan` immutable properties:

```text
IReadOnlyList<SiteReservationSelectionStep> Steps
IReadOnlyList<FootprintPlacement> SelectedPlacements
int SelectedCount
long TotalCost
```

- exact six steps, depth `0..5`, required search key order다.
- every IncrementalCost is hard-satisfied and was calculated against only earlier selected placements.
- 따라서 final successful plan의 checked distance constraints 합은 exact `0+1+2+3+4+5 = 15`다.
- TotalCost는 six incremental total costs의 checked sum이다.
- selected placements는 footprint overlap, existing-entry blocking, entry-approach occupancy가 없다.
- final MAP03_04 index는 `6 keys / 15 records`, policy evaluation satisfied, violations/errors `0`이다.
- three-Core union은 하나의 `4×4` 이하 bounding box 안에 갇히지 않으며 `CoreClusterUnits == 0`이다.
- source option/group/list mutation이 plan을 바꾸지 않는다.
- reservation ID/order, SiteReservation, sectors, CoreBiomeSeed, Village를 포함하지 않는다.

## `SiteReservationSearchResult` Contract

immutable properties:

```text
SiteReservationSearchStatus Status
bool Succeeded
bool RetryRequired
SiteReservationSelectionPlan SelectionPlan
SiteReservationSearchDiagnostics Diagnostics
IReadOnlyList<SiteReservationSearchError> Errors
```

status/plan/retry/errors invariant는 위 exact semantics와 일치해야 한다.

## Exact Integration Gates

starter typed definitions와 MAP03_02/03 outputs로 exact six groups를 구축한다.

```text
groups 6
source options 3156 = 88 + 572 + 624*4
RNG_WORLD_SITE tie-break draws 3156
selected placements 6
final distance records/constraints 15/15
Village selected 0
```

default future capacity estimate는 MAP03_07 전이므로 exact `-1`을 사용할 수 있다. completed plan은 collision/distance/cluster hard gates를 모두 통과해야 한다. seeds `0`, `4660`, `ulong.MaxValue` 각각 fresh site stream으로 complete해야 하며 같은 seed/attempt 100회는 exact same selection/diagnostics를 만든다. 서로 다른 seed가 반드시 다른 selection이어야 한다고 가정하지 않는다.

synthetic backtracking gates:

- depth N 첫 option이 다음 group dead-end를 만들고 second option이 성공하면 exact previous-depth pop 후 success.
- two-depth forced dead-end는 LIFO order로 두 번 pop한다.
- custom limit `1`은 first pop 기록 뒤 `FailedCombinationLimitReached`, pop `1`, partial plan null이다.
- root group viable option zero는 `NoSolution`, pop `0`, retry true다.
- input group/order reversal이 output/search order를 바꾸지 않는다.

## Determinism / Ownership

- group/option/definition input order와 list implementation이 달라도 same stream input에서 exact output/diagnostics다.
- RNG draw count는 option count만의 함수이며 branching/backtracking과 무관하다.
- same seed/attempt fresh stream, fresh/reused backtracker 100회 결과가 같다.
- `en-US`/`tr-TR`, wall clock, frame, thread, filesystem에 무관하다.
- source definitions/options/placements/policy/weights/limits를 mutate하지 않는다.
- injected siteRng만 consume하고 other stream instances의 state/draw count 영향 `0`이다.
- public setters/fields, mutable collection exposure, static mutable cache, lazy public enumeration이 없다.

## Scope Boundary / DO NOT

- raw origin enumeration, footprint transform/world-bound solve를 다시 수행하지 않는다.
- cost formula를 복제/변경하지 않고 MAP03_05 calculator를 사용한다.
- RNG weighted probability, higher-cost override, reshuffle per backtrack 금지
- reservation ID/order, SiteReservation/SectorReservation/Snapshot/CoreBiomeSeed 생성 금지
- actual Core capacity flood/connectivity/buffer hard gate 금지
- Village bucket/layout/selection 금지
- full PASS_SITE adapter/root retry/attempt increment 금지
- generated_special_sites/sector serializer/file I/O/replay 확장 금지
- route graph/tile movement/biome growth 금지
- existing MAP03_01~05 models/tests 수정 금지
- Authoring/generated CSV/meta, asmdef/asmref, Scene/Prefab, Package/ProjectSettings 변경 금지
- test skip/ignore/assertion 완화, Git operation 금지
- MAP03_07 선행 작업 금지

## Collision Handling

1. 신규 destination이 없으면 생성한다.
2. exact 계약과 바이트 동일한 preexisting destination만 `PREEXISTING_IDENTICAL`로 재사용한다.
3. 다르면 overwrite/merge/delete하지 않고 `STATUS: BLOCKED`다.
4. 기존 `.meta` GUID와 사용자 변경을 보존한다.

## Required Tests

`SiteReservationBacktrackerTests.cs` actual NUnit cases 최소 `160`개다.

minimum groups:

- option/group/limits null/range/identity/duplicate/order/read-only invariants
- exact six key/search order and wrong/missing/extra/Village group rejection
- exact starter option counts `88/572/624/624/624/624 = 3156`
- fresh/missing/preconsumed site RNG gate and invalid-input draw `0`
- known first two tie-breaks and equal-cost second canonical option precedence
- one draw per canonical option, draw count independent of branch/backtrack
- footprint overlap / blocks existing entry / entry approach occupied reasons
- shared entry exterior and ordinary adjacency allowed
- distance and Core cluster hard rejections, two-reason count semantics
- altitude/edge/capacity soft cost ranking without hard rejection
- exact order tuple cost/tie/origin/transform/candidate ordinal
- one-level and two-level forced backtrack LIFO behavior
- exact failed-combination/backtrack count and default/custom limit `200/1`
- NoSolution vs limit vs InvalidInput vs Completed status invariants
- no partial plan on every failure status
- successful plan six steps/depth/key order/total identity/distance-check sum 15
- final collision-free, distance `15/15` satisfied, Core 4×4 cluster absent
- full starter seeds `0/4660/ulong.MaxValue` complete with exact `3156` draws
- reversed/shuffled groups/options and array/list stability
- synthetic same stream input fresh/reused 100-run identity
- other five RNG stream state/draw-count independence
- `en-US`/`tr-TR` culture and caller mutation isolation
- public mutation-surface/dependency audit
- capacity flood/Village/final snapshot/pass/root/file-I/O production dependency `0`

금지:

- `[Ignore]`, `[Explicit]`, inconclusive, assumption-based skip
- reflection으로 private state를 바꿔 success를 만드는 test
- test order/current filesystem/wall clock 의존
- fixed selection lookup/hard-coded seed answer

## Regression / Verification

```text
New SiteReservationBacktrackerTests: >=160 PASS
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
Targeted Game.Map.Tests.EditMode: >=2702/2702 PASS
Full project EditMode: >=2742/2742 PASS
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
Assets meta files = 3027
duplicate GUID groups = 0
Authoring CSV = 50
Authoring CSV meta = 50
```

after expected:

```text
new C# = 9
new matching .cs.meta = 9
Assets meta files = 3036
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

`REPORTS/MAP03_06_IMPLEMENT_RESERVATION_BACKTRACKING_RESULT.md` 필수:

- `STATUS: PASS|BLOCKED|FAIL`
- implementation summary / exact changed-created paths
- required group/order/option-count and RNG draw evidence
- collision/hard/soft rejection and stable order evidence
- forced backtrack/limit/no-solution/status evidence
- full starter three-seed selection/final policy evidence
- diagnostics/plan/result/determinism/immutability evidence
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
- MAP03_06만 `COMPLETE`, `MAP03_07_IMPLEMENT_CORE_CAPACITY_FLOOD_CHECK`는 계속 `LOCKED`, 다른 future task도 `LOCKED`

BLOCKED 전제:

- collision/baseline/precondition/change-budget 문제가 있으면 Assets/status/master를 수정하지 않는다.
- Result에 exact blocker와 필요한 repair patch 범위를 기록한다.

Recommended Commit: `feat(map): backtrack deterministic site placement combinations`
