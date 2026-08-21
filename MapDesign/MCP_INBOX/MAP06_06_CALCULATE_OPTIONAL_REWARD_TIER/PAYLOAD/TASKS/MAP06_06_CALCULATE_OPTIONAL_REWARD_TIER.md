# MAP06_06 — Calculate Optional Reward Tier

```yaml
status_control:
  task_key: MAP06_06_CALCULATE_OPTIONAL_REWARD_TIER
  result_file: REPORTS/MAP06_06_CALCULATE_OPTIONAL_REWARD_TIER_RESULT.md
```

## TASK TYPE

```text
RUNTIME IMMUTABLE P04 OPTIONAL REWARD-SCORE/TIER RESERVATION + SOURCE-CHAIN VALIDATION + PHASE-BOUNDARY TEST ADVANCE + EDITMODE TESTS
```

## Objective

MAP06_04의 immutable Type0 snapshot과 MAP06_05의 immutable access/clue assignment를 source-chain으로 결합해 각 optional region의 reward score와 reward tier를 deterministic하게 계산한다.

공식 roadmap의 계산식을 exact integer formula로 고정한다.

```text
RewardScore = MaxDepth * DepthWeight
            + ToolCostTier
            + ExplosiveFuelCost / ExplosiveFuelDivisor
            + HiddenClueDifficulty
```

approved settings는 `DepthWeight=2`, `ExplosiveFuelDivisor=10`, tier minimum scores `0/4/8/12`다. MAP06_01의 existing `OptionalRewardTier`를 그대로 재사용해 가장 높은 충족 threshold의 `Low/Medium/High/Unique`로 분류하며 `Unique`는 상한 없이 포화한다. `None`은 successful calculation output으로 배정하지 않는다.

이번 Task는 score/tier reservation까지만 만든다. 실제 reward ID, item, pool, quantity, spawn slot, unique reward, mandatory/core reward, chest, generated spawn/CSV를 선택하거나 생성하지 않는다. attachment base mask, access/clue, Type0 topology, mandatory graph를 수정하지 않는다.

MAP06_07 return policy, MAP06_08 inactive buffer, MAP06_09 validator, MAP06_10 overlay/exit는 구현하지 않는다. MAP06_06 production symbols가 새로 생기므로 기존 phase-boundary negative assertions는 MAP06_06 symbols를 허용하고 MAP06_07+ future symbols만 금지하도록 필요한 boundary test만 교정한다.

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
12. `REPORTS/MAP06_05_ASSIGN_ACCESS_RULES_AND_CLUES_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP06_05_ASSIGN_ACCESS_RULES_AND_CLUES
STATUS: PASS
MAP06_05: COMPLETE ELIGIBLE
MAP06_06_CALCULATE_OPTIONAL_REWARD_TIER: LOCKED / DO NOT START
SHA-256: 0f8d8ba09d8c6f36cd75a8bdcdc808eb00bcc1d63031981425a580a64d481630
```

이 별도 patch가 적용된 뒤에만 MAP06_06을 실행한다. MAP06_07 이후 Task body는 읽거나 시작하지 않는다.

## Approved Input Baseline

```text
Optional access assignments/clues/perceptible clues: 12/12/12
Access distribution Basic/Tool/Environment/Explosive/Hidden: 3/3/2/2/2
Tool requirements Pickaxe/Shovel/Rope: 1/1/1
Hidden clues Crack/Light/Sound: 1/1/0
Explosive reward-preview reservations: 2
Source Type0 assignment digest: a26e73f25ff7267760a2507ec55b1acda3a6c39a8f3698fc37b190620ceca525
Source growth digest: 1f00f718bdb8d79fbf88923be0d81e0075987267c024cc624086ee6194998caa
Access assignment canonical digest: 5268b7ed2342f197fb5717c85dbfdb8e51b3c520c217ecf93ad14dc944ecf74f
Optional regions/cells/Type0 assignments: 12/39/39
Attachment boundaries base-closed: 12
Mandatory boundary base-open: 0
RNG/source mutation/partial publication: 0/0/0
Assets meta: 3283
Authoring CSV/meta: 50/50
Authoring manifest SHA-256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Duplicate GUID groups: 0
```

MAP05 Type4 규칙은 그대로 보존한다.

```text
Type4 requires U+D open.
L/R are independent and preserve actual mandatory graph adjacency.
UD, LUD, RUD, LRUD are all legal.
```

## Map Package Reference

exact installed Map Package v1.0이 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/01_WORLD_FIXED_SPEC.md
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
01_FIXED_SPEC/07_OPTIONAL_EDGE_OVERLAY.md
02_PHASE_ROADMAP/MAP06_TYPE0_OPTIONAL_REGIONS.md
03_CSV_SCHEMA/ENUM_REFERENCE.md
03_CSV_SCHEMA/CSV_RELATIONSHIPS.md
04_CSV_STARTER/content_budget_profiles.csv
04_CSV_STARTER/population_profiles.csv
04_CSV_STARTER/spawn_pool_entries.csv
04_CSV_STARTER/resource_spawn_rules.csv
```

reference는 roadmap score 식, Type0 optional-only reward boundary와 future population ownership을 확인하는 용도다. Authoring CSV body를 runtime source로 다시 파싱하거나 수정하지 않는다. reward item/pool 선택은 P08/MAP12 책임이며 이번 Task source of truth는 caller-supplied immutable MAP06_04/MAP06_05 results와 explicit settings다.

## READ ALLOWLIST

### Existing P03~P04 production

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionAttachment.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegion.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionSnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteOpenMask.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteMaskId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteMaskRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteMaskAssignment.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteMaskAssignmentDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteMaskAssignmentResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessClueId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessAssignmentEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessClue.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessAssignmentSettings.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessAssignment.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessAssignmentDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessAssignmentResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessRuleAssigner.cs
```

### Existing tests / assemblies

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/HorizontalBackboneRouterTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteGraphValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map05ExitTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/UpDownConflictResolverTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/VerticalGatewayPlannerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionModelsTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalAttachmentEnumeratorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionGrowerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Type0RouteMaskAssignerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalAccessRuleAssignerTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

위 파일과 matching meta, approved Runtime/Test `Generation` path-only inventory, Authoring CSV/meta count·aggregate hash, 전체 Assets meta GUID, patch marker 이후 change-scope path만 확인할 수 있다.

금지: installed Authoring CSV body runtime 재파싱·수정, generated CSV body source 사용, MAP06_07+ Task body, reward population/unique allocator body, unrelated production/test body, Legacy/Stage/P6/P11 generator body, Scene/Prefab YAML.

## WRITE ALLOWLIST

### 신규 Runtime production C# — exact 6

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRewardTierCalculationEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRewardTierSettings.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRewardTierAssignment.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRewardTierDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRewardTierResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRewardTierCalculator.cs
```

### 신규 Runtime EditMode test — exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRewardTierCalculatorTests.cs
```

### 기존 phase-boundary test 수정 — exact up to 11

MAP06_06 production symbol 허용 및 MAP06_07+ future symbol 금지 유지를 위해 필요한 경우 아래 existing test C#만 수정할 수 있다. matching `.cs.meta`는 수정하지 않는다.

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/HorizontalBackboneRouterTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteGraphValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map05ExitTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/UpDownConflictResolverTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/VerticalGatewayPlannerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionModelsTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalAttachmentEnumeratorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionGrowerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Type0RouteMaskAssignerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalAccessRuleAssignerTests.cs
```

신규 C# 7개와 matching `.cs.meta` 7개, 위 boundary test C# 최대 11개 수정, Result 1개만 허용한다. MAP05/MAP06_01~05 production source, mandatory graph/mask, Type0/access assignments, OptionalRegion models, Authoring/generated CSV/meta, asmdef, Scene, Prefab, Packages, ProjectSettings는 수정하지 않는다. 신규 directory/folder meta/asmdef/asmref를 만들지 않는다.

## Namespace / Assembly

```text
Runtime namespace: StarNight.Map.WorldGeneration.Generation
Test namespace:    StarNight.Map.Tests.WorldGeneration.Generation
Runtime assembly:  Game.Map.Runtime
Test assembly:     Game.Map.Tests.EditMode
```

`UnityEditor`, Unity object/lifecycle, record/record struct, `required`, `init`, nullable-reference directive, reflection factory, singleton/static mutable state를 도입하지 않는다.

## Frozen P04 Boundary

```text
Input artifacts  = TYPE0_ROUTE_MASK_ASSIGNMENT_SNAPSHOT + OPTIONAL_ACCESS_AND_CLUE_RESERVATION_SNAPSHOT
Read-only guards = OPTIONAL_REGION_TOPOLOGY + MAP05 MANDATORY GRAPH IDENTITY
Output artifact  = OPTIONAL_REWARD_SCORE_AND_TIER_RESERVATION_SNAPSHOT
Pass ID          = PASS_OPTIONAL
RNG consumption  = exact 0
```

output은 reward placement가 아니다. reward ID/item/pool/quantity/slot을 포함하지 않고 mandatory/core/unique reward를 선택하지 않는다.

## Existing Tier / Calculation Enum Contract

MAP06_01의 `OptionalRegionEnums.cs`에 이미 정의된 아래 enum/token codec을 read-only로 재사용한다.

```text
OptionalRewardTier: None, Low, Medium, High, Unique
tokens: NONE | LOW | MEDIUM | HIGH | UNIQUE
```

`OptionalRegionEnums.cs`를 수정하거나 duplicate `OptionalRewardTier` type/codec을 만들지 않는다. successful calculation은 `Low/Medium/High/Unique` 중 하나만 배정하며 `None`은 배정하지 않는다. `Unique` tier는 실제 unique reward ID/item 선택이 아니라 score 등급 예약일 뿐이다.

신규 `OptionalRewardTierCalculationEnums.cs`:

```text
OptionalRewardTierCalculationStatus:
  Completed | InvalidInput | InvalidSettings | InvalidSource | ArithmeticOverflow

OptionalRewardTierCalculationErrorCode:
  NullInput | InvalidStatus | InvalidDigest | SourceMismatch | InvalidAccounting
  MissingRegion | DuplicateRegion | InvalidDepth | InvalidMatrix
  OpenAttachmentBoundary | ArithmeticOverflow
```

undefined enum을 거부한다. 이 internal calculation status/error enum에 별도 authoring token codec을 만들지 않으며 locale case-fold 또는 `Enum.Parse`를 계약 구현으로 사용하지 않는다.

## `OptionalRewardTierSettings` Contract

sealed immutable object다.

```text
int DepthWeight
int ExplosiveFuelDivisor
IReadOnlyList<int> TierMinimumScores
```

- `DepthWeight`는 `1..100`; approved `2`.
- `ExplosiveFuelDivisor`는 `1..100`; approved `10`.
- `TierMinimumScores`는 exact 4 copied entries이며 first `0`, strictly increasing, each `0..1,000,000`; approved `0/4/8/12`.
- index `0/1/2/3`은 existing tier `Low/Medium/High/Unique` minimum이며 score가 여러 threshold를 충족하면 가장 높은 tier다.
- `Unique`는 `score >= fourth minimum` 전체를 포함한다. successful output에서 `None`은 금지한다.
- caller list mutation, culture, enumeration order가 settings를 바꾸면 안 된다.
- static mutable/default settings instance를 만들지 않는다.

## Exact Score Algorithm

각 region을 RegionId ordinal canonical order로 처리한다.

1. Type0 result와 access result 모두 `Completed`인지 확인한다.
2. access `SourceType0AssignmentDigest`가 Type0 `CanonicalDigest`와, 두 `SourceGrowthDigest`가 서로 exact 일치하는지 확인한다.
3. Type0 `SourceSnapshot.Regions`, Type0 per-cell assignments, access assignments를 RegionId로 exact one-to-one join한다.
4. region `MaxDepth.Value`, access rule/clue/cost matrix, attachment identity, base-closed evidence를 검증한다.
5. checked integer arithmetic으로 아래 component를 계산한다.

```text
DepthScore          = MaxDepth.Value * DepthWeight
ToolCostScore       = ToolCostTier
ExplosiveFuelScore  = ExplosiveFuelCost / ExplosiveFuelDivisor
HiddenClueScore     = HiddenClueDifficulty
RewardScore         = DepthScore + ToolCostScore
                    + ExplosiveFuelScore + HiddenClueScore
```

`ExplosiveFuelScore`는 non-negative integer division의 truncation toward zero를 명시적으로 사용한다. source matrix상 unused cost field는 반드시 0이므로 한 region에 서로 다른 access cost를 임의 중첩하지 않는다.

6. threshold를 낮은 순서로 비교해 가장 높은 충족 tier를 선택한다.
7. 모든 source와 arithmetic을 검증한 뒤에만 immutable result를 원자적으로 publish한다.

production은 approved fixture의 12개 수량/분포/고정 digest를 하드코딩하지 않는다. exact fixture gate는 test와 Result에서 검증한다.

## Rule / Cost Preservation Matrix

| AccessRule | Required score inputs | Must remain zero | Other preserved evidence |
|---|---|---|---|
| Basic | depth | tool/fuel/hidden | BasicOpening clue |
| Tool | depth + tool tier | fuel/hidden | Pickaxe/Shovel/Rope + ToolSurface |
| Environment | depth | tool/fuel/hidden | EnvironmentDevice |
| Explosive | depth + fuel/divisor | tool/hidden | ExplosiveRewardPreview + preview true |
| Hidden | depth + clue difficulty | tool/fuel | Hidden traversal + Crack/Light/Sound |

모든 clue는 perceptible이어야 한다. invalid/undefined rule, mismatched requirement/traversal/clue, unused nonzero cost, duplicate/missing region, invalid depth, digest/accounting mismatch, open attachment base side는 atomic failure다.

## `OptionalRewardTierAssignment` Contract

sealed immutable object다.

```text
OptionalRegionId RegionId
int RegionOrdinal
int AttachmentOrder
OptionalAccessClueId ClueId
OptionalRegionAccessRule AccessRule
int MaxDepth
int ToolCostTier
int ExplosiveFuelCost
int HiddenClueDifficulty
int DepthScore
int ToolCostScore
int ExplosiveFuelScore
int HiddenClueScore
int RewardScore
OptionalRewardTier RewardTier
bool RequiresPartialRewardPreview
```

source identity/cost/preview fields를 그대로 보존하고 source result/snapshot을 mutate하지 않는다. reward ID/item/pool/quantity/slot 또는 mandatory reward field를 추가하지 않는다.

## Diagnostics / Result Contract

`OptionalRewardTierDiagnostics` sealed immutable fields:

```text
int SourceRegionCount
int SourceType0CellAssignmentCount
int SourceAccessAssignmentCount
int TierAssignmentCount
int LowCount
int MediumCount
int HighCount
int UniqueCount
int DepthContributionTotal
int ToolContributionTotal
int ExplosiveContributionTotal
int HiddenContributionTotal
int RewardScoreMinimum
int RewardScoreMaximum
int RewardPreviewReservationCount
int MandatoryRewardSelectionCount
int RngDrawCount
int SourceMutationCount
```

`MandatoryRewardSelectionCount`는 이번 Task에서 exact `0`이다.

`OptionalRewardTierResult` sealed immutable properties:

```text
OptionalRewardTierCalculationStatus Status
IReadOnlyList<OptionalRewardTierAssignment> Assignments
OptionalRewardTierDiagnostics Diagnostics
IReadOnlyList<OptionalRewardTierCalculationError> Errors
string SourceType0AssignmentDigest
string SourceAccessAssignmentDigest
string SourceGrowthDigest
string CanonicalDigest
int RngDrawCount
bool IsSuccess
```

errors는 code, RegionId, attachment order, source field, message 순으로 stable sort/dedupe한다. 실패는 assignments empty, canonical digest empty, RNG/mutation/mandatory reward selection `0/0/0`, partial publication `0`이다.

CanonicalDigest는 source digests, copied settings, tier assignments, diagnostics를 stable UTF-8 ordinal field order로 SHA-256한 lowercase 64-hex다.

## `OptionalRewardTierCalculator` Contract

stateless sealed service다.

```text
OptionalRewardTierResult Calculate(
    Type0RouteMaskAssignmentResult type0Assignments,
    OptionalAccessAssignmentResult accessAssignments,
    OptionalRewardTierSettings settings)
```

- reference input null, invalid status/digest/accounting, source-chain mismatch를 거부한다.
- input collections를 copied canonical order로 처리한다.
- Type0/source snapshot/access/clue/cost/mask/mandatory graph를 mutate하지 않는다.
- input caller order, culture, service reuse, thread/time에 독립적이다.
- hidden cache, static mutable collection, reflection, filesystem, Registry singleton, RNG는 0이다.
- checked overflow는 `ArithmeticOverflow`로 atomic failure하며 예외/partial output을 외부로 누출하지 않는다.

## Boundary Test Advance

허용할 MAP06_06 symbols:

```text
OptionalRewardTierCalculationStatus
OptionalRewardTierCalculationErrorCode
OptionalRewardTierSettings
OptionalRewardTierAssignment
OptionalRewardTierDiagnostics
OptionalRewardTierCalculationError
OptionalRewardTierResult
OptionalRewardTierCalculator
OptionalRewardTierCalculatorTests
```

계속 금지할 MAP06_07+ examples:

```text
OptionalReturnPolicyResolver
OptionalReturnConnection
InactiveBufferAssigner
OptionalRegionValidator
OptionalRegionOverlay
GeneratedOptionalRegionCsvWriter
```

MAP05 Type4, MAP06_04 Type0 base-closed/L+R, MAP06_05 access/clue matrix assertions를 약화하지 않는다. MAP06_07+ 금지 case 수를 줄이지 않는다.

## Required Tests

새 `OptionalRewardTierCalculatorTests`는 최소 `260` actual PASS cases를 가져야 한다.

- tier enum/token/settings validation/copy/thresholds `>=32`
- score formula components, integer division, checked overflow `>=36`
- Low/Medium/High/Unique threshold boundaries, None rejection, Unique saturation `>=32`
- five access-rule matrix and unused-cost rejection `>=34`
- source-chain join/digest/accounting/canonical order `>=30`
- approved fixture per-region score/tier/component evidence `>=30`
- atomic invalid input/source/settings/arithmetic failure `>=24`
- canonical digest/culture/order/service reuse determinism `>=22`
- source mutation/RNG/base-closed/Type4/boundary advance `>=20`

Existing required gates:

```text
OptionalRewardTierCalculatorTests >=260 PASS
OptionalAccessRuleAssignerTests 289/289 PASS
Type0RouteMaskAssignerTests 257/257 PASS
MAP06 prior combined selection 630/630 PASS
Existing MAP05 aggregate 1959/1959 PASS
Actually executed required total >=3395 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
Assets meta 3283 -> 3290
new C#/meta 7/7
existing boundary test C# modified <=11
Authoring CSV/meta 50/50
Authoring manifest SHA-256 unchanged
duplicate GUID groups 0
MAP05/MAP06_01~05 production/graph/mask/models/assignments/CSV/asmdef/Scene/Prefab/Packages/ProjectSettings modifications 0
generated CSV files created by this task 0
actual reward IDs/items/pools/quantities/spawns selected 0
```

Discovery-only count나 arithmetic total을 PASS로 기록하지 않는다. 위 total은 Unity EditMode Test Runner에서 실제 실행한 결과여야 한다.

## Result Format

Write exactly `MapDesign/MCP/REPORTS/MAP06_06_CALCULATE_OPTIONAL_REWARD_TIER_RESULT.md`.

Required top lines:

```text
TASK: MAP06_06_CALCULATE_OPTIONAL_REWARD_TIER
STATUS: PASS|FAIL|BLOCKED
MAP06_06: COMPLETE ELIGIBLE|NOT COMPLETE
MAP06_07_IMPLEMENT_RETURN_POLICY: LOCKED / DO NOT START
```

Result must include patch/receipt, prior Result and Task SHA gates, created/changed files, approved score settings/formula/thresholds, source-chain digests, all per-region depth/components/score/tier, tier distribution and score min/max, preview/base-closed/access-clue preservation, atomic overflow/failure and mutation evidence, actual reward selection zero, MAP05/Type4 and MAP06_04/05 preservation, exact test jobs, Unity gate, asset/meta/CSV/GUID/change-scope gate, and NEXT that finalizes only MAP06_06 while keeping MAP06_07 locked.

## Done Conditions

- [ ] Preconditions, prior Result SHA, current Task SHA verified; MAP06_06 is sole CURRENT.
- [ ] Type0/access/growth digest chain and one-to-one region identity verified.
- [ ] Every optional region has one exact checked score and existing Low/Medium/High/Unique tier reservation.
- [ ] Formula components and thresholds match explicit approved settings.
- [ ] Access/clue/cost/preview matrix and attachment base-closed state preserved.
- [ ] No reward item/pool/quantity/spawn or mandatory/core/unique reward selected.
- [ ] MAP06_06 symbols allowed; MAP06_07+ forbidden.
- [ ] No return/inactive/validator/overlay behavior.
- [ ] Mandatory graph/masks, Type4, Type0/access assignments and Authoring CSV unchanged.
- [ ] Required Unity EditMode gates actually executed and Result evidence complete.

If any required gate fails or Unity MCP is unavailable, write `STATUS: FAIL` or `STATUS: BLOCKED`, keep MAP06_06 CURRENT, and do not create or start MAP06_07.




