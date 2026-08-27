# MAP03_04 — Implement Site Distance Index

```yaml
status_control:
  task_key: MAP03_04_IMPLEMENT_SITE_DISTANCE_INDEX
  result_file: REPORTS/MAP03_04_IMPLEMENT_SITE_DISTANCE_INDEX_RESULT.md
```

## TASK TYPE

```text
RUNTIME DETERMINISTIC FOOTPRINT-AWARE SECTOR-GRAPH DISTANCE INDEX + REQUIRED-SITE POLICY + EDITMODE TESTS
```

## Objective

MAP03_03의 성공 `FootprintPlacement` 집합을 immutable pairwise distance index로 만든다. 거리는 13×13 P00의 L/R/U/D sector graph에서 두 footprint의 occupied cells 사이 최단 간선 수다.

이번 Task는 다음만 소유한다.

1. placement identity를 stable key로 정규화
2. footprint-to-footprint minimum sector-graph distance와 closest-cell pair 계산
3. pair record O(1) lookup이 가능한 immutable index 생성
4. typed special-map distance fields로 exact required-site policy 생성
5. complete placement set이 policy를 만족하는지 deterministic violation 목록으로 평가

비용/penalty, 고도, edge preference, quadrant clustering, RNG, 후보 선택, reservation 생성, backtracking, Core capacity, Village 거리 bucket, route 생성, 실제 tile 이동 비용은 수행하지 않는다.

## Distance Meaning — Frozen Boundary

P00는 world 안의 모든 cardinal neighbor edge가 존재하는 exact 13×13 graph다.

```text
SectorDistance(a, b) = abs(a.X - b.X) + abs(a.Y - b.Y)

PlacementDistance(A, B) = min SectorDistance(a, b)
                          for a in A.OccupiedSectors
                              b in B.OccupiedSectors
```

- 같은 sector는 `0`, cardinal adjacent sector는 `1`, `(0,0) ↔ (12,12)`는 `24`다.
- index build는 placement overlap을 거부하므로 published pair record distance는 `1..24`다.
- origin/sector 중심 Euclidean distance, diagonal/Chebyshev distance, empty-cell gap count가 아니다.
- entry exterior, blocker, biome, altitude, planned route, wall/tool state는 거리를 늘리거나 줄이지 않는다.
- 이것은 P01 예약용 **sector separation constraint**다. 완성 tile graph의 실제 이동 비용/완주 거리는 MAP13 소유권이다.
- wrap/clamp/portal/blocked-edge/weighted-edge가 없다.

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
12. `REPORTS/MAP03_03_IMPLEMENT_FOOTPRINT_PLACEMENT_SOLVER_RESULT.md`

MAP03_03 Result의 exact `STATUS: PASS`, focused `170/170`, candidate/model regressions `268/268 / 81/81`, targeted `2033/2033`, full `2073/2073`, exact matrix `3468/3156/312`, final Assets meta `3012`, existing Assets modification `0`을 확인한다.

## Map Package Reference

Map Package v1.0 exact installed path가 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/01_WORLD_FIXED_SPEC.md
01_FIXED_SPEC/02_COORDINATE_AND_ID_RULES.md
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
01_FIXED_SPEC/05_IMPLEMENTATION_ORDER.md
02_PHASE_ROADMAP/MAP03_SPECIAL_SITE_RESERVATION.md
04_CSV_STARTER/special_map_catalog.csv
```

exact reference가 없으면 이 Task의 frozen distance/policy contract와 현재 immutable typed definitions를 authoritative fallback으로 사용한다. Authoring CSV를 직접 읽거나 재파싱하지 않는다.

## READ ALLOWLIST

### Existing typed definitions

```text
Assets/_Game/Map/Runtime/WorldGeneration/Data/SpecialMapDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/SpecialVillageDefinitionSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/StaticDataRegistry.cs
```

### Existing grid, candidate, reservation, placement models

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGridIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteOriginCandidate.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteFootprint.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacementEntry.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacement.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacementResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacementSolver.cs
```

### Focused tests / assemblies

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/SpecialVillageDefinitionBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationModelsTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteCandidateEnumerationTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/FootprintPlacementSolverTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

위 C#/asmdef와 matching meta, approved `Generation` Runtime/Test 직계 파일명의 path-only inventory, Authoring CSV/meta count·hash, 전체 meta GUID, task marker 이후 change-scope path만 확인할 수 있다.

금지:

- Authoring CSV body 직접 재파싱·수정
- MAP03_05 이후 Task body
- unrelated production/test C# body
- Legacy/Stage/P6/P11 generator body
- Scene/Prefab YAML

## WRITE ALLOWLIST

### 신규 Runtime production C# — exact 7

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SitePlacementKey.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceIndexResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistancePolicy.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceEvaluationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceIndexBuilder.cs
```

### 신규 Runtime EditMode test — exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteDistanceIndexTests.cs
```

신규 C# 8개와 matching `.cs.meta` 8개, Result 1만 생성한다. 기존 production/tests/meta/asmdef/asmref는 수정하지 않는다. 기존 approved directory를 재사용하며 새 directory/folder meta를 만들지 않는다.

## Namespace / Assembly

```text
Runtime namespace: StarNight.Map.WorldGeneration.Generation
Test namespace:    StarNight.Map.Tests.WorldGeneration.Generation
Runtime assembly:  Game.Map.Runtime
Test assembly:     Game.Map.Tests.EditMode
```

`UnityEditor`, Unity object/lifecycle, record/record struct, `required`, `init`, nullable-reference directive, reflection factory, singleton/static mutable state를 도입하지 않는다.

## `SitePlacementKey` Contract

immutable value type properties:

```text
SiteReservationKind Kind
string SourceDefinitionId
int RequiredInstanceOrdinal
int PlacementPriority
bool IsValid
```

construction / equality / comparison:

```text
SitePlacementKey(SiteReservationKind kind,
    string sourceDefinitionId,
    int requiredInstanceOrdinal)

static SitePlacementKey FromPlacement(FootprintPlacement placement)
```

- defined kind, canonical non-empty source ID, instance ordinal `>=0`을 요구한다.
- candidate origin/index/ordinal/transform은 site identity가 아니므로 key에 포함하지 않는다.
- exact priority는 `Start 0 / Boss 10 / Forge 20 / CoreResource 30 / Village 40`이다.
- canonical order는 priority, SourceDefinitionId ordinal, instance ordinal이다.
- equality/hash/order는 culture/runtime-randomized string hash에 의존하지 않는 deterministic ordinal contract다.
- default struct는 `IsValid == false`; 다른 distance model은 invalid key를 거부한다.
- Village key 표현은 future generic distance query를 위해 허용하지만 required-site policy에는 포함하지 않는다.

## `SiteDistanceRecord` Contract

immutable properties:

```text
SitePlacementKey First
SitePlacementKey Second
int Distance
SectorCoord FirstClosestSector
SectorCoord SecondClosestSector
int FirstClosestSectorIndex
int SecondClosestSectorIndex
```

- `First < Second` canonical key order다.
- Distance는 exact `1..24`이며 두 closest sectors의 Manhattan distance와 같다.
- sector/index는 exact `WorldGridIndex` identity다.
- 여러 cell pair가 같은 minimum이면 canonical key orientation에서 `(FirstSectorIndex, SecondSectorIndex)` lexicographic minimum을 보존한다.
- origin distance, center distance, entry distance를 별도 field로 만들지 않는다.

## `SiteDistanceIndexBuilder` Contract

public API:

```text
public sealed class SiteDistanceIndexBuilder

SiteDistanceIndexResult Build(
    IEnumerable<FootprintPlacement> placements)
```

input / build rules:

- placements collection은 non-null이다. empty와 single-item set은 valid하며 partial backtracking state query에 사용 가능하다.
- null placement, invalid candidate/key, empty/invalid/duplicate occupied sector를 거부한다.
- placement key는 unique다. 같은 required site의 두 candidate option을 한 index에 함께 넣지 않는다.
- 서로 다른 placements의 occupied sector overlap은 오류다. distance `0` record나 winner를 publish하지 않는다.
- caller 순서와 무관하게 keys를 `SitePlacementKey` canonical order로 copied snapshot한다.
- unordered pair마다 exact one record를 만든다. count `n*(n-1)/2`다.
- source placement/footprint/candidate/collection을 mutate하지 않는다.
- 어떤 오류든 있으면 null Index와 accumulated sorted Errors만 반환한다. partial index publish 금지다.

## `SiteDistanceIndex` Contract

immutable properties/API:

```text
IReadOnlyList<SitePlacementKey> Keys
IReadOnlyList<SiteDistanceRecord> Records
int PlacementCount
int PairCount

bool Contains(SitePlacementKey key)

bool TryGetDistance(
    SitePlacementKey first,
    SitePlacementKey second,
    out int distance)

bool TryGetRecord(
    SitePlacementKey first,
    SitePlacementKey second,
    out SiteDistanceRecord record)

SiteDistanceEvaluationResult Evaluate(
    SiteDistancePolicy policy)
```

- Keys/Records는 canonical copied read-only snapshots다.
- pair lookup은 normalized unordered key pair로 O(1) observable behavior를 제공한다. public mutable dictionary는 노출하지 않는다.
- 존재하는 동일 key query는 `TryGetDistance == true`, distance `0`; self record는 없으므로 `TryGetRecord == false`다.
- missing/invalid key query는 false이고 distance `-1` / record null이다.
- `PairCount == Records.Count == n*(n-1)/2`다.
- `Evaluate`는 complete required-site policy 평가이며 아래 Evaluation Contract를 따른다.

## Error / Build Result Contract

`SiteDistanceErrorCode` exact frozen ordinal order:

```text
MissingPlacements
NullPlacement
InvalidPlacement
DuplicatePlacementKey
InvalidOccupiedSector
OverlappingPlacements
MissingStartSourceId
InvalidStartSourceId
MissingSpecialMapInput
NullSpecialMap
DuplicateSpecialMapId
MissingRequiredSite
UnexpectedRequiredSite
InactiveRequiredSite
SiteRoleMismatch
InvalidRequiredCount
InvalidDistanceRule
MissingPolicy
MissingPolicyKey
UnexpectedIndexKey
MissingDistanceRecord
```

`SiteDistanceError` immutable properties:

```text
SiteDistanceErrorCode Code
string FirstSourceDefinitionId
string SecondSourceDefinitionId
int SectorIndex
string Message
```

- IDs는 canonical-or-empty, sector index는 relevant exact `0..168` 또는 `-1`이다.
- message는 stable non-empty이며 path/stack/timestamp/thread/current-culture exception text를 포함하지 않는다.
- errors는 code ordinal, first ID ordinal, second ID ordinal, sector index, message ordinal로 sort/dedupe한다.

`SiteDistanceIndexResult`:

```text
bool Succeeded
SiteDistanceIndex Index
IReadOnlyList<SiteDistanceError> Errors
```

- success: non-null Index, errors `0`
- failure: null Index, errors `>=1`
- expected invalid input은 exception이 아니다.

## Required-Site Policy Contract

`SiteDistancePolicyBuilder`는 `SiteDistancePolicy.cs`에 둔다.

```text
SiteDistancePolicyResult BuildRequiredSitePolicy(
    string startSourceDefinitionId,
    IEnumerable<SpecialMapDefinition> specialMaps)
```

input gate:

- start source ID는 canonical non-empty world/profile ID이며 Start key instance는 exact `0`이다.
- specialMaps와 item은 non-null이고 map IDs는 ordinal unique다.
- active required Village는 MAP03_08 소유이므로 유효하게 제외한다.
- exact active required non-village definitions는 아래 five, required count `1`, role/kind exact다.
- expected 누락/비활성/role mismatch/count mismatch와 unexpected active required Boss/Forge/CoreResource는 오류다.

| Key order | Kind | Source | Start distance | Other required-site distance |
|---:|---|---|---:|---:|
| 1 | Boss | `SITE_MOON_BOSS_VAULT` | `4` | `2` |
| 2 | Forge | `SITE_MOON_SEAL_FORGE` | `2` | `2` |
| 3 | CoreResource | `SITE_CASSIA_SAP_HEART` | `2` | `3` |
| 4 | CoreResource | `SITE_DEEP_STAR_YEAST` | `2` | `3` |
| 5 | CoreResource | `SITE_MOON_CORE_METEOR` | `2` | `3` |

policy generation:

- exact six keys: one Start + five required sites.
- Start↔site constraint는 그 site의 typed `MinGraphDistanceFromStart`를 사용한다.
- site↔site constraint는 `max(first.MinGraphDistanceToOtherCoreSites, second.MinGraphDistanceToOtherCoreSites)`를 사용한다.
- 이것은 Core가 포함된 모든 pair를 minimum `3`, Forge↔Boss를 minimum `2`로 고정한다.
- fixed spec의 Start↔first-special minimum `2`는 모든 start constraint가 `>=2`라서 충족된다. 어떤 site가 first인지는 선택/backtracking 소유권이므로 이번 Task에서 고르지 않는다.
- Boss의 typed Start rule `4`는 fixed minimum `2`보다 stricter한 authoritative content constraint다.

`SiteDistanceRuleKind` exact values:

```text
StartToRequiredSite
RequiredSiteToRequiredSite
```

`SiteDistanceConstraint` immutable properties:

```text
SiteDistanceRuleKind RuleKind
SitePlacementKey First
SitePlacementKey Second
int MinimumDistance
```

`SiteDistancePolicy` immutable properties/API:

```text
IReadOnlyList<SitePlacementKey> Keys
IReadOnlyList<SiteDistanceConstraint> Constraints
int ConstraintCount

bool TryGetConstraint(
    SitePlacementKey first,
    SitePlacementKey second,
    out SiteDistanceConstraint constraint)
```

- constraint는 canonical unordered key pair마다 exact one, self pair 없음, minimum `1..24`다.
- canonical pair order로 copied read-only 보관하며 lookup은 O(1) observable behavior다.
- starter exact constraints는 `15`; minimum distribution은 `2 x 5`, `3 x 9`, `4 x 1`이다.
- Start pair `5`, special-site pair `10`, Village pair `0`이다.

`SiteDistancePolicyResult`는 success에서 Policy non-null/errors 0, failure에서 Policy null/errors >=1이며 partial policy를 publish하지 않는다.

## Policy Evaluation Contract

`SiteDistanceIndex.Evaluate(policy)`는 full required set gate다.

- null policy는 `MissingPolicy` failure다.
- index keys와 policy keys가 exact set match하지 않으면 missing/unexpected key errors를 누적하고 violation을 publish하지 않는다.
- exact keys가 일치하면 모든 15 constraint에 corresponding distance record가 있어야 한다.
- `ActualDistance < MinimumDistance`일 때만 violation이다. equal은 PASS다.

`SiteDistanceViolation` immutable properties:

```text
SiteDistanceRuleKind RuleKind
SitePlacementKey First
SitePlacementKey Second
int ActualDistance
int MinimumDistance
int Deficit
SectorCoord FirstClosestSector
SectorCoord SecondClosestSector
```

- `Deficit == MinimumDistance - ActualDistance`이고 `>=1`이다.
- violations는 rule kind, First key, Second key, actual, minimum 순으로 sort/dedupe한다.

`SiteDistanceEvaluationResult`:

```text
bool Succeeded
bool Satisfied
IReadOnlyList<SiteDistanceViolation> Violations
IReadOnlyList<SiteDistanceError> Errors
```

- structurally valid evaluation: `Succeeded == true`, errors `0`; `Satisfied`는 violations `0`일 때만 true다.
- structural failure: `Succeeded == false`, `Satisfied == false`, violations `0`, errors `>=1`.
- partial backtracking caller는 `Policy.TryGetConstraint`와 `Index.TryGetDistance`로 present pair를 직접 검사한다. missing required sites를 silently complete로 간주하지 않는다.

## Exact Reference Vectors

sector metric:

```text
(0,0) ↔ (0,0)   = 0
(0,0) ↔ (1,0)   = 1
(0,0) ↔ (0,1)   = 1
(0,0) ↔ (12,12) = 24
(2,9) ↔ (11,3)  = 15
```

footprint-aware vectors:

```text
single (0,0) vs Boss 2x1 at origin (3,0), occupied (3,0)/(4,0) = 3
Boss 2x1 at origin (5,5), occupied (5,5)/(6,5) vs single (8,5) = 2
single (4,4) vs single (4,5) = 1
```

passing exact-six synthetic set:

```text
Start       (0,0)
Boss 2x1    origin (4,0), occupied (4,0)/(5,0)
Forge       (8,0)
Cassia      (0,4)
Deep Yeast  (4,6)
Moon Core   (9,6)
```

이 set은 exact 15 records와 15 policy constraints를 모두 만족한다.

violation boundary vectors:

- Start↔Boss actual `3` / required `4`: deficit `1`.
- Core↔Core actual `2` / required `3`: deficit `1`.
- Forge↔Boss actual `1` / required `2`: deficit `1`.
- actual == required `4/3/2`는 각각 PASS다.

## Determinism / Ownership

- placement/definition input order, collection implementation이 달라도 keys/records/constraints/errors/violations가 exact 동일하다.
- all `169×169 = 28561` sector pairs가 Manhattan/reference topology와 일치한다.
- seeds `0`, `4660`, `ulong.MaxValue`, candidate ordinal, transform은 distance formula나 policy membership을 바꾸지 않는다.
- fresh/reused builder/index evaluation 100회, `en-US`/`tr-TR`에서 observable output이 동일하다.
- caller/source placement/definition/list mutation이 completed output을 바꾸지 않는다.
- public setters/fields, mutable collection exposure, static mutable cache, lazy public enumeration이 없다.
- RNG/time/frame/thread/filesystem/Unity object state에 의존하지 않는다.

## Scope Boundary / DO NOT

- footprint transform/world-bound/collision을 다시 solve하지 않는다.
- candidate catalog 전체 option을 enumerate/filter하지 않는다.
- altitude/edge/distance penalty/candidate cost/weight 금지
- four-core 4×4 clustering 판정 금지
- RNG draw/shuffle/weighted choice/best option selection 금지
- reservation ID/order, SectorReservation, SiteReservation, snapshot 생성 금지
- backtracking/retry/max-200/PASS_SITE 금지
- Core capacity flood-fill/CoreBiomeSeed 생성 금지
- Village 20/50/30 bucket/layout/placement 금지
- route graph 생성, blocked-edge/route-aware distance 금지
- tile traversal/actual completion movement distance 금지
- pass/root adapter, generated serializer/file I/O/replay 확장 금지
- existing MAP03_01~03 models/tests 수정 금지
- Authoring/generated CSV/meta, asmdef/asmref, Scene/Prefab, Package/ProjectSettings 변경 금지
- test skip/ignore/assertion 완화, Git operation 금지
- MAP03_05 선행 작업 금지

## Collision Handling

1. 신규 destination이 없으면 생성한다.
2. exact 계약과 바이트 동일한 preexisting destination만 `PREEXISTING_IDENTICAL`로 재사용한다.
3. 다르면 overwrite/merge/delete하지 않고 `STATUS: BLOCKED`다.
4. 기존 `.meta` GUID와 사용자 변경을 보존한다.

## Required Tests

`SiteDistanceIndexTests.cs` actual NUnit cases 최소 `128`개다.

minimum groups:

- SitePlacementKey default/invalid/kind/source/ordinal/priority/equality/hash/order tests
- exact priority/order Start/Boss/Forge/CoreResource/Village and ordinal source ordering
- exhaustive all `28561` sector-pair Manhattan distances, symmetry, diagonal non-edge, max 24
- reference vectors and no wrap/clamp/Euclidean/Chebyshev/empty-gap interpretation
- single/sparse/2×1 footprint minimum and deterministic closest-pair tie-break
- builder null item/invalid placement/duplicate key/overlap/out-of-world/duplicate occupied rejection
- empty/single index valid, pair count `n*(n-1)/2`, same-key distance zero/no self record
- canonical input-order-independent key/record snapshots and O(1) lookup behavior
- exact five starter definitions and distance field values `4/2/2/2/2` + `2/2/3/3/3`
- policy missing/inactive/duplicate/null/unexpected/wrong-role/wrong-count/invalid-distance errors
- exact six keys / 15 constraints / distribution `2×5, 3×9, 4×1`
- Start pair5, special pair10, Core-involving pair3, Forge↔Boss2, Village pair0
- passing synthetic six-placement set exact 15/15 satisfied
- exact threshold equal PASS and three deficit boundary violations
- multi-violation canonical ordering and closest-sector evidence
- missing/unexpected policy keys, missing policy, no partial violation result
- partial lookup through policy/index without treating partial as complete
- entry position, blocker, biome, altitude, seed, candidate ordinal, transform do not affect occupied-footprint distance
- reversed/shuffled array/list inputs and caller mutation isolation
- seeds `0/4660/ulong.MaxValue`, fresh/reused 100-run identity
- `en-US`/`tr-TR` culture invariance and public mutation-surface audit
- cost/RNG/selection/backtracking/capacity/village/pass/route/tile-distance/file-I/O production dependency `0`

금지:

- `[Ignore]`, `[Explicit]`, inconclusive, assumption-based skip
- reflection으로 private state를 바꿔 success를 만드는 test
- test order/current filesystem/wall clock 의존
- fixed six results를 hard-coded return하는 fake index

## Regression / Verification

```text
New SiteDistanceIndexTests: >=128 PASS
MAP03_03 FootprintPlacementSolverTests: 170/170 PASS
MAP03_02 SiteCandidateEnumerationTests: 268/268 PASS
MAP03_01 SiteReservationModelsTests: 81/81 PASS
MAP02 phase focused aggregate: 667/667 PASS
SpecialVillageDefinitionBuilderTests: 57/57 PASS
BiomeBoundaryDefinitionBuilderTests: 38/38 PASS
StaticDataRegistryBuilderTests: 53/53 PASS
ContentVersionHashCalculatorTests: 54/54 PASS
Targeted Game.Map.Tests.EditMode: >=2161/2161 PASS
Full project EditMode: >=2201/2201 PASS
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
Assets meta files = 3012
duplicate GUID groups = 0
Authoring CSV = 50
Authoring CSV meta = 50
```

after expected:

```text
new C# = 8
new matching .cs.meta = 8
Assets meta files = 3020
duplicate GUID groups = 0
Authoring CSV = 50 unchanged
Authoring CSV meta = 50 unchanged
```

GUID는 32 lowercase hex, non-zero, project-wide unique다. `.meta`는 `fileFormatVersion: 2`와 `MonoImporter`를 사용한다.

## Exact Change Budget

```text
Created Assets:  16
Modified Assets: 0
Deleted Assets:  0
Created report:  1
```

exact 16 Assets destinations 외 변경이 있으면 `BLOCKED`다.

## Result Contract

`REPORTS/MAP03_04_IMPLEMENT_SITE_DISTANCE_INDEX_RESULT.md` 필수:

- `STATUS: PASS|BLOCKED|FAIL`
- implementation summary / exact changed-created paths
- distance formula / exhaustive 28561 / closest-pair tie-break evidence
- index key/pair count/lookup/overlap error evidence
- exact policy key/constraint/count/distribution evidence
- passing set + threshold/violation/evaluation evidence
- determinism/immutability/ownership evidence
- focused/regression/targeted/full test counts + job IDs
- Unity refresh/compile/warnings
- before/after meta, duplicate GUID, Authoring count/hash
- scope audit / existing modification count / PREEXISTING_IDENTICAL
- task checklist, recommended commit

PASS 전제:

- exact change set + all compile/test/meta/count/hash/scope gates PASS
- existing Asset modification `0`
- `.APPLIED`는 정확한 `PATCH_ID`, `PATCH_VERSION`, `TASK_KEY`, `TASK_PATH`, `MANIFEST_SHA256`, `TASK_SHA256`를 기록
- `07_PATCH_APPLY_RULES.md` current-task binding에 따라 `.APPLIED`가 존재하고 exact manifest/task SHA와 일치해야 `06_IMPLEMENTATION_STATUS.md`와 `MASTER_IMPLEMENTATION_TASK_LIST.md`를 finalize
- MAP03_04만 `COMPLETE`, `MAP03_05_IMPLEMENT_SITE_CANDIDATE_COST`는 계속 `LOCKED`, 다른 future task도 `LOCKED`

BLOCKED 전제:

- collision/baseline/precondition/change-budget 문제가 있으면 Assets/status/master를 수정하지 않는다.
- Result에 exact blocker와 필요한 repair patch 범위를 기록한다.

Recommended Commit: `feat(map): index required site footprint distances`
