# MAP03_11 — MAP03 Batch and Exit Tests

```yaml
status_control:
  task_key: MAP03_11_MAP03_BATCH_AND_EXIT_TESTS
  result_file: REPORTS/MAP03_11_MAP03_BATCH_AND_EXIT_TESTS_RESULT.md
```

## TASK TYPE

```text
TEST-ONLY BATCH / STATISTICAL / DETERMINISM / PHASE EXIT AUDIT
```

## Objective

MAP03_01~10 production을 수정하지 않고 phase-level EditMode batch와 현재 프로젝트 visual revalidation으로 MAP03 Phase Gate를 최종 판정한다.

exit evidence chain은 아래를 함께 묶는다.

```text
exact six required non-Village search groups / 3156 options and RNG tie-break draws
four Core capacity witnesses / exact 20 disjoint minimum expected sectors
one Village with 20/50/30 Start-distance bucket
seven final reservation IDs / 169 sector rows / six entries / four Core seeds
six-rule atomic validation publication
same-seed same-attempt determinism and fresh-attempt retry isolation
100,000-world-seed Village bucket distribution
10,000-world-seed full attempt/retry/failure-reason audit
shared Game/Scene site reservation overlay
```

이 Task는 production batch runner, `PASS_SITE` adapter, Root integration, generated CSV/export, retry policy를 구현하지 않는다. private test fixture가 existing public APIs를 exact production order로 호출해 phase를 감사한다. 실패가 발견되면 production/expected를 이 Task에서 고치지 않는다.

## Mandatory Read Order

1. `MapDesign/MCP/00_MCP_ENTRYPOINT.md`
2. locked/work/CSV/Unity/change/patch/finalize global rules
3. `MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md`
4. `MapDesign/MCP/06_IMPLEMENTATION_STATUS.md`
5. 이 Task
6. MAP03_01~10 PASS Results, task order ascending
7. MAP02_08 PASS Result의 approved RNG/grid baseline

MAP03_10 Result가 exact `STATUS: PASS`가 아니거나 Current Task가 이 Task와 다르면 실행하지 않고 `BLOCKED`다. MAP04_01 이후 Task body는 읽거나 생성하거나 실행하지 않는다.

## Map Package Reference

Map Package v1.0 exact installed tree가 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/01_WORLD_FIXED_SPEC.md
01_FIXED_SPEC/02_COORDINATE_AND_ID_RULES.md
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
01_FIXED_SPEC/05_IMPLEMENTATION_ORDER.md
02_PHASE_ROADMAP/MAP03_SPECIAL_SITE_RESERVATION.md
04_CSV_STARTER/special_map_catalog.csv
04_CSV_STARTER/special_map_footprint_cells.csv
04_CSV_STARTER/special_map_entry_sockets.csv
04_CSV_STARTER/village_profiles.csv
04_CSV_STARTER/village_layout_catalog.csv
```

CSV reference는 frozen ID/count 의미 확인용이다. installed Authoring CSV body를 직접 읽거나 재파싱하지 않는다. exact reference가 없으면 MAP03_01~10 Task의 frozen contracts와 existing typed definitions가 authoritative fallback이다. substitute/Legacy generator를 broad search하지 않는다.

## READ ALLOWLIST

### Supporting typed definitions / grid / RNG

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
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
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGridIndex.cs
```

### MAP03 Runtime production — exact 69

MAP03_01 models:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteFootprint.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteEntryAnchor.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreBiomeSeed.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorReservation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSnapshot.cs
```

MAP03_02 candidate enumeration:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteOriginCandidate.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateGroup.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCatalog.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateEnumerationError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateEnumerationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateEnumerator.cs
```

MAP03_03 footprint placement:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteFootprintTransformer.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacementEntry.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacement.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacementBlockers.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacementResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacementSolver.cs
```

MAP03_04 distance:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SitePlacementKey.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceIndexResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistancePolicy.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceEvaluationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceIndexBuilder.cs
```

MAP03_05 cost:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCostWeights.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCostContext.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCostBreakdown.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCostError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCostResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCostCalculator.cs
```

MAP03_06 search/backtracking:

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

MAP03_07 capacity:

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

MAP03_08 Village:

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

MAP03_09 validation/publication:

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

MAP03_10 runtime diagnostics:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/SiteReservationOverlayCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/SiteReservationOverlaySnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/SiteReservationOverlayGui.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/SiteReservationOverlay.cs
```

### MAP03 Editor production — exact 1

```text
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Preview/SiteReservationOverlaySceneDrawer.cs
```

### Existing MAP03 focused tests — exact 11

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationModelsTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteCandidateEnumerationTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/FootprintPlacementSolverTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteDistanceIndexTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteCandidateCostTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationBacktrackerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/CoreCapacityFloodCheckerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/VillageReservationSelectorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationOverlayTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Preview/SiteReservationOverlaySceneDrawerTests.cs
```

assemblies:

```text
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Editor/MapAuthoring/MapAuthoring.Editor.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/MapAuthoring.Tests.EditMode.asmdef
```

위 exact C#/asmdef와 matching meta, approved WorldGeneration folders의 path-only inventory, Authoring CSV/meta count/hash, project meta GUID, task marker 이후 change-scope path만 검사할 수 있다. unrelated production/test body, MAP04 이후 body, Legacy/Stage/P6/P11 body, Authoring CSV body, Scene/Prefab YAML은 읽거나 사용하지 않는다.

## WRITE ALLOWLIST

신규 Runtime EditMode test C# exact 1:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map03ExitTests.cs
```

matching `.cs.meta` `1`과 Result `1`만 생성한다. production C#, existing test, asmdef/asmref, Authoring/generated CSV/meta, Scene/Prefab/Package/ProjectSettings는 생성·수정·삭제·이동하지 않는다. 새 directory/folder meta를 만들지 않는다.

test namespace/fixture:

```text
namespace StarNight.Map.Tests.WorldGeneration.Generation
public sealed class Map03ExitTests
```

private nested immutable batch record, aggregate, starter fixture builder, attempt runner, canonical digest helper는 이 test file 안에서만 허용한다. production runner/fake service, reflection-based production discovery, shared static mutable test state, filesystem output을 만들지 않는다.

## Result Chain / Inventory Gate

- Master unique Task `205`; MAP00 `10/10 COMPLETE`, MAP01 `17/17 COMPLETE`, MAP02 `8/8 COMPLETE`, MAP03_01~10 `COMPLETE`, MAP03_11 `CURRENT`, MAP04 이후 `LOCKED`다.
- MAP03_01~10 Result 각각의 task ID와 exact `STATUS: PASS`를 확인한다.
- latest MAP03_10 handoff: runtime/editor/combined `133/28/161`, MAP03_01~09 `2098`, targeted `3745`, full `3813`, visual `18/18`, final meta `3070`, exact Assets changes `14`, existing modification `0`이다.
- MAP03 runtime production `69/69`, editor production `1/1`, existing focused test `11/11`과 matching metas가 존재한다.
- Authoring CSV/meta `50/50`, accepted legacy Editor folder meta `6/6`, new WorldGeneration asmdef/asmref `0`, project duplicate GUID `0`이다.
- MAP00/01/02 exit gates remain approved; MAP04 production/task implementation is absent/not started.

## Test-Only Starter Fixture / Preparation

private fixture는 existing public typed definition constructors와 exact starter IDs/values만 사용한다. Authoring CSV/file/Registry singleton을 읽지 않는다.

seed-independent preparation exact once per test invocation:

1. exact world/special/biome/Core rule/Village profile/layout/entry definitions를 만든다.
2. MAP03_02 candidate enumerator로 exact `933` raw origins를 만든다.
3. MAP03_03 empty-blocker placement solver로 exact `3468` transform evaluations, `3156` success options, `312` source rejection을 만든다.
4. MAP03_04 exact six-key/15-constraint distance policy를 만든다.
5. MAP03_05 default weights와 capacity forecast `-1`로 six search groups를 만든다.
6. copied immutable prepared definitions/groups/policy를 every world seed/attempt에서 재사용한다.

preparation은 selected state, RNG tie-break, rank, approval, diagnostics, reservation을 cache하지 않는다. production object를 mutate하지 않는다.

## Exact Full Attempt Pipeline

private `RunAttempt(ulong worldSeed, int attemptOrdinal)`은 exact 아래 order다.

```text
1. fresh RNG_WORLD_SITE(worldSeed, attemptOrdinal)
2. SiteReservationBacktracker.Search
3. CoreCapacityFloodChecker.Check
4. VillageReservationSelector.Select using the same continued site stream
5. SiteReservationValidator.ValidateAndPublish(worldSeed, ...)
```

stage success contract:

```text
search: selected 6 / source options 3156 / tie-break draws 3156
capacity: witnesses 4 / witness sectors 20 / overlap 0
Village: approval 1 / existing+Village 6+1 / exact 3 NextInt method calls
validation: rules 6/6 / violations 0 / errors 0
publication: reservations 7 / sector rows 169 / reserved 8 / unreserved 161
entries 6 / Core seeds 4
```

terminal failure precedence:

```text
SearchNoSolution
SearchCombinationLimit
CoreCapacityRejected
VillageBucketRejected
FinalValidationRejected
InvalidInput
```

- first non-success stage에서 attempt를 끝내고 later stage를 실행하지 않는다.
- retry-required stage만 next `attemptOrdinal + 1`의 fresh whole attempt를 허용한다.
- selected option/Village/witness local repair, bucket fallback/redraw, same-stream continuation retry가 없다.
- `InvalidInput`은 retry하지 않으며 batch gate failure다.
- test-only observation budget은 attempt ordinal `0..7`, exact maximum `8` attempts/world다. 이것은 future production Root retry policy를 구현하거나 고정하지 않는다.
- prior failed/success result and caller collections remain immutable after later attempts.

## Canonical Batch Record / Retry Reasons

per-world canonical record는 최소 아래 observable facts를 ordinal/invariant format으로 digest한다.

```text
world seed
completed attempt ordinal
attempt count and ordered terminal statuses
search selected keys/origins/transforms/tie-breaks/costs
capacity witness owner/sector indices
Village bucket/range/layout/origin/side/rolls
seven reservation IDs/orders/origins/footprint local cells
169 sector reservation identities
six entry anchors
four Core seed identities
six rule results and diagnostics
```

retry aggregate:

```text
world count
initial success/failure world count
retried world count
resolved after retry count
unresolved count
total attempt count / total retry attempt count
maximum completed attempt ordinal
terminal failure count by six stage tokens
reason occurrence count by existing search/capacity/Village/validation enums
invalid input/error count
canonical SHA-256 of ordered per-world records
```

- terminal failure conservation: failed attempt count equals five retry-stage terminal counts plus invalid-input terminal count.
- capacity/validation result may contain multiple reason/violation rows; occurrence sum may exceed terminal attempt count and is recorded separately.
- all retry reasons preserve existing enum order/stable IDs/counts; exception text/path/time/thread/culture를 reason identity에 넣지 않는다.
- a batch with no natural retry is valid only if zero counts/conservation are recorded and synthetic retry contracts below pass. nonzero count를 만들기 위해 input을 조작하지 않는다.

## 100,000-World-Seed Village Distribution Gate

authoritative sample:

```text
world seeds = ulong 0..99,999 inclusive
attempt ordinal = 0
sample count = 100,000
site tie-break draws before bucket = 3156
bucket source = exact authoritative "2-3:20|4-6:50|7-10:30"
```

각 seed에서 existing factory로 fresh `RNG_WORLD_SITE`를 만들고 production schedule과 동일하게 exact `3156` `NextUInt64` tie-break draws를 소비한 뒤 production `NextInt(100)`으로 bucket roll을 한 번 얻는다. modulo/floating scaling/custom RNG/precomputed answer table을 사용하지 않는다.

exact roll mapping:

```text
0..19  -> distance 2..3 / expected 20%
20..69 -> distance 4..6 / expected 50%
70..99 -> distance 7..10 / expected 30%
```

acceptance:

```text
near count  19,250..20,750
middle count 49,250..50,750
far count   29,250..30,750
count sum = 100,000
absolute percentage-point deviation per bucket <= 0.75
Pearson chi-square(df=2) <= 13.815511
roll outside 0..99 = 0
```

observed exact counts, percentages to six invariant decimals, chi-square, schedule digest and elapsed diagnostic을 Result에 기록한다. elapsed time은 machine-dependent diagnostic일 뿐 PASS threshold가 아니다.

### Full-pipeline schedule bridge

10,000-seed full batch의 every attempt가 Village stage에 도달할 때 independent fresh stream을 같은 seed/attempt로 다시 만들고 exact 3156 draws 후 얻은 bucket roll이 actual `VillageReservationDiagnostics.BucketRoll`, selected bucket ordinal/range와 exact 일치해야 한다.

이 bridge가 통과해야만 100,000 schedule distribution을 actual Village production behavior의 evidence로 사용할 수 있다. direct schedule과 selector가 다른 draw order/roll/bucket을 사용하면 FAIL이다.

## 10,000-World-Seed Full Reservation / Retry Gate

authoritative full sample:

```text
world seeds = ulong 0..9,999 inclusive
world count = 10,000
attempts per world = 1..8
```

every world는 위 exact full attempt pipeline으로 실행한다.

phase acceptance:

```text
initial attempt retry-required worlds <= 500 (<=5.00%)
invalid-input worlds/attempts = 0
unresolved after 8 attempts = 0
resolved worlds = 10,000/10,000
successful worlds with exact seven required reservations = 10,000/10,000
successful rules = 6/6 per world
successful overlap/out-of-world/entry/capacity violations = 0
```

final successful Village bucket distribution over 10,000 resolved worlds:

```text
distance 2..3 = 1,800..2,200
distance 4..6 = 4,800..5,200
distance 7..10 = 2,800..3,200
sum = 10,000
```

final successful snapshot per world:

- exact sources `WORLD_MOONPALACE_V1`, Boss, Forge, Cassia, Yeast, Meteor, Village each once
- reservation ID/order exact seven contract
- 169 sector rows, reserved `8`, unreserved `161`, footprint overlap `0`
- entries total/required `6/6`, exterior world-bound/unreserved
- distance constraints `15/15`, Village checks `6/6`, cluster check `1/1`
- witnesses `4`, each target `5`, union `20`, cross overlap `0`, Village intersection `0`
- Core seeds exact Forge/Cassia/Yeast/Meteor `4`
- validator rules `6/6 PASS`, violations/errors `0/0`
- snapshot seed equals original world seed even when completed attempt ordinal is greater than zero

Result는 initial failure rate, retry count, max attempt, resolved-attempt histogram `0..7`, terminal/reason counts, final bucket counts, canonical digest를 exact 기록한다.

## Determinism / Attempt Isolation Exit Gate

- full seeds `0`, `4660`, `ulong.MaxValue`는 attempt `0`에서 exact same publication/diagnostics as prior contracts로 complete한다.
- seeds `0..1023` full pipeline batch를 fresh services와 reused stateless services로 각각 실행해 canonical records/digest exact 같다.
- same `0..1023` seeds를 reverse enumeration한 뒤 seed canonical order로 sort하면 exact same per-seed records/digest다.
- `en-US`와 `tr-TR`에서 representative seeds와 any naturally retried seeds의 status/reasons/publication digest가 같다.
- same world seed/attempt fresh RNG stream을 100회 재실행하면 same search/Village/publication record다.
- attempt ordinal one-field mutation changes only that site stream domain; other five RNG stream vectors and prepared source data remain unchanged.
- a failed attempt's diagnostics/selection/witness/Village result remains unchanged after next whole attempt.
- batch aggregate does not depend on NUnit order, wall clock, thread scheduling, filesystem, static mutable state, or collection insertion order.

observed digest는 Result evidence이지 production lookup/golden table로 추가하지 않는다.

## Synthetic Retry Classification Gates

natural sample counts와 무관하게 existing public APIs로 deterministic small fixtures를 만들어 아래를 각각 검증한다.

```text
SiteReservationSearchStatus.NoSolution -> RetryRequired true / plan null
SiteReservationSearchStatus.FailedCombinationLimitReached -> RetryRequired true / exact limit count
CoreCapacityFloodStatus.CapacityRejected -> RetryRequired true / approval null / canonical rejection(s)
VillageReservationStatus.ReservationRejected -> RetryRequired true / exact one bucket rejection
SiteReservationValidationStatus.ValidationRejected -> RetryRequired true / publication null / canonical violation(s)
all InvalidInput statuses -> RetryRequired false / partial output null
```

classification helper는 existing status/reason enums를 token으로 바꿀 뿐 production behavior를 대신하지 않는다. synthetic result를 10,000-seed failure rate에 섞지 않는다.

## Overlay Exit Gate

seed `4660` completed same-attempt publication/search/capacity/Village/validation diagnostics로 exact 확인한다.

- overlay snapshot `169` cells, `7` reservations, `8` reserved, `6` arrows, witnesses/sectors `4/20`, rules `6`, rows `16`
- seven source glyph/color/local-cell identities와 unknown-source rejection
- four witness owners `5/5/5/5`, union `20`, overlap `0`
- fixed `1000x760` panel, `572x572` grid, `44x44` cells, required viewport `1024x784`
- visual top y=12/bottom y=0 and all four corners/hit-test boundaries
- Game `OnGUI`와 Scene drawer가 same runtime `SiteReservationOverlayGui.Draw`를 사용
- overlay는 generation/pass/RNG/retry/file을 자동 실행하지 않고 source/snapshot/Scene을 mutate하지 않음

## Production / Ownership Audit

- MAP03 Runtime production `69`, Editor production `1`; unexpected MAP03 production `0`
- existing focused tests `11` + new exit test `1`; matching metas present/GUID unique
- Runtime `UnityEditor` dependency `0`, new asmdef/asmref `0`
- injected existing `RNG_WORLD_SITE` 이외 `System.Random`/`UnityEngine.Random` dependency `0`
- static mutable selection/approval/publication/diagnostic/overlay cache `0`
- production file I/O/export/batch runner/pass adapter/root integration `0`
- local repair/bucket fallback/rejection suppression `0`
- MAP04 biome patch model/owner/growth/painting implementation `0`
- Authoring/Registry/typed definitions/generated data mutation `0`

## New Exit Test Contract

`Map03ExitTests.cs` actual NUnit cases minimum `96`개다. 100,000/10,000 batch loop 자체를 parameterized 100,000/10,000 TestCases로 만들지 않는다. bounded batch tests는 streaming aggregate/digest를 사용해 Test Runner result와 memory를 안정적으로 유지한다.

minimum groups:

- Result/inventory-independent locked identity and public contract smoke
- exact seed-independent fixture matrix `933 / 3468 / 3156 / 312 / 6 / 15`
- attempt stage order, short-circuit, continued RNG, fresh retry attempt
- all required site/reservation/sector/entry/witness/Core-seed/rule invariants
- 100,000 schedule counts/tolerance/chi-square/digest
- 10,000 full batch success/failure/retry/reason conservation and final bucket distribution
- direct schedule ↔ actual selector bridge
- fresh/reused/reverse/culture/attempt determinism and immutability
- six synthetic terminal classifications and invalid-input non-retry semantics
- overlay snapshot/layout/orientation/hit/tooltip/shared-renderer integration
- production dependency/ownership/inventory/meta/change audit

rules:

- `[Ignore]`, `[Explicit]`, inconclusive, assumption skip, assertion weakening 금지
- batch failure를 reduced sample, wider tolerance, seed exclusion, retry beyond ordinal 7로 숨기지 않음
- current observed counts/hash를 expected success table로 hard-code하지 않음
- test order/current culture/wall clock/existing filesystem/shared static store 의존 금지
- production을 exit test에서 수정하거나 private reflection mutation으로 success를 만들지 않음

## Tests / Verification

```text
New MAP03 exit tests: >=96 PASS
MAP03_01 models: 81/81 PASS
MAP03_02 candidates: 268/268 PASS
MAP03_03 placement: 170/170 PASS
MAP03_04 distance: 239/239 PASS
MAP03_05 cost: 270/270 PASS
MAP03_06 backtracking: 248/248 PASS
MAP03_07 capacity: 215/215 PASS
MAP03_08 Village: 339/339 PASS
MAP03_09 validator: 268/268 PASS
MAP03_10 runtime/editor overlay: 133/133 + 28/28 PASS
Existing MAP03 focused aggregate: 2259/2259 PASS
MAP03 phase focused aggregate: >=2355 PASS
MAP02 phase focused aggregate: 667/667 PASS
SpecialVillage/BiomeBoundary/StaticRegistry/ContentHash: 57/57 / 38/38 / 53/53 / 54/54 PASS
Previous Game.Map targeted baseline: 3745/3745 PASS
Game.Map targeted total: >=3841 PASS
Previous full project EditMode baseline: 3813/3813 PASS
Full project EditMode: >=3909 PASS
failed/skipped = 0/0
Unity 6000.3.8f1 / forced refresh / compile error 0 / relevant warning 0
PlayMode NOT RUN / saved Scene-Prefab changes NONE
```

## Current-Project Visual Revalidation

MAP03_10 Result/captures만 인용하지 말고 Unity MCP/Editor에서 seed `4660` same-attempt completed input으로 현 프로젝트를 다시 확인한다. transient object는 `HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild`만 사용하고 종료 시 제거한다.

1. Game/Scene title exact `MAP03 SITE RESERVATION / Seed 4660`
2. 두 View 모두 13x13/169 cells와 four-corner orientation exact
3. seven source colors와 `A/B/F/C/Y/M/V` glyphs 판독 가능
4. eight reserved cells의 final local coordinates 판독 가능
5. six entry arrows가 matching footprint edges에 표시
6. four Core witness regions exact `5/5/5/5`, union `20`
7. footprint fill+outline과 unreserved translucent `+` 구분
8. witness legend가 minimum expected region/not painted biome 의미 보존
9. summary `7 / 8 / 6 / 4 / 20 / 6` exact
10. sixteen diagnostic rows order/class/value exact
11. soft rows `(SOFT COST, NOT REJECTION)` 판독 가능
12. Start hover exact
13. entry hover exact
14. witness-only hover exact
15. outside hover empty/no clamp
16. Game/Scene renderer content and snapshot identity same
17. selection/camera/transform/timeScale/Scene dirty state unchanged
18. Clear/transient cleanup after overlay absent, hierarchy/Scene/Prefab residue `0`

visual `18/18` current evidence가 없으면 batch/tests PASS여도 `BLOCKED`다. capture가 필요하면 project `Temp/MAP03_11_Captures`만 사용하고 Assets/source change scope에서 제외한다.

## Asset / Meta / Change Gate

clean baseline and expected final:

```text
Authoring CSV/meta = 50/50 unchanged
Assets meta baseline = 3070
new Runtime test C# = 1
new matching cs.meta = 1
final Assets meta = 3071
accepted legacy Editor folder meta = 6/6 unchanged
task-marker 이후 exact Assets changes = 2
existing Assets modifications = 0
unexpected Assets changes = 0
new directory/folder meta = 0
project duplicate GUID groups = 0
```

new meta는 `fileFormatVersion: 2`, `MonoImporter`, lowercase non-zero unique 32-hex GUID다.

## Failure Policy

- result chain/inventory, batch distribution, initial failure ceiling, retry resolution, reason conservation, determinism, successful snapshot, visual, compile, meta/change gate 중 하나라도 불일치하면 `STATUS: FAIL`이다.
- Unity/Test Runner/visual 접근이 없어 actual 100,000/10,000/current visual gate를 수행할 수 없으면 `STATUS: BLOCKED`다.
- FAIL/BLOCKED를 production modification, expected/tolerance relaxation, seed filtering, sample reduction, stale Result 인용으로 해결하지 않는다.
- PASS가 아니면 MAP03 exit approval이나 STATUS FINALIZE를 수행하지 않고 MAP04를 열지 않는다.

## Result / Completion

Result exact path:

```text
MapDesign/MCP/REPORTS/MAP03_11_MAP03_BATCH_AND_EXIT_TESTS_RESULT.md
```

required sections:

```text
TASK
STATUS
SUMMARY
PATCH APPLY
READ
MASTER BACKLOG CHECK
PRIOR RESULT CHAIN
CREATED
MODIFIED
PREEXISTING_IDENTICAL
PRODUCTION INVENTORY
STARTER FIXTURE AND PREPARATION
ATTEMPT PIPELINE
100000-SEED VILLAGE DISTRIBUTION
10000-SEED FULL BATCH
RETRY RATE AND REASONS
DETERMINISM AND ATTEMPT ISOLATION
FINAL RESERVATION GATE
OVERLAY
TEST
VISUAL REVALIDATION
UNITY
ASSET META VALIDATION
CHANGE SCOPE
PRODUCTION OWNERSHIP AUDIT
OUT_OF_SCOPE_FINDINGS
MAP03 EXIT DECISION
DONE CONDITIONS
NEXT
Recommended Commit
```

PASS Result에는 exact lines가 있어야 한다.

```text
STATUS: PASS
MAP03 EXIT: APPROVED
MAP04 ENTRY: ELIGIBLE FOR SEPARATE PATCH
MAP04_01: LOCKED / DO NOT START
```

모든 조건 PASS 시에만 MAP03_11 COMPLETE, Current Task NONE으로 finalize한다. `MAP04_01_IMPLEMENT_BIOME_PATCH_MODELS`는 LOCKED로 유지하고 자동 시작하지 않는다.

Recommended Commit: `test(map): approve map03 site reservation phase gate`
