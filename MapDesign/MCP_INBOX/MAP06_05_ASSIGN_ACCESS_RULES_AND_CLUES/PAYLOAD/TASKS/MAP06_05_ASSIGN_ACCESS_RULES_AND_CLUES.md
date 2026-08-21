# MAP06_05 — Assign Access Rules and Clues

```yaml
status_control:
  task_key: MAP06_05_ASSIGN_ACCESS_RULES_AND_CLUES
  result_file: REPORTS/MAP06_05_ASSIGN_ACCESS_RULES_AND_CLUES_RESULT.md
```

## TASK TYPE

```text
RUNTIME IMMUTABLE P04 OPTIONAL ACCESS/CLUE RESERVATION + COST INPUT PUBLICATION + PHASE-BOUNDARY TEST ADVANCE + EDITMODE TESTS
```

## Objective

MAP06_04의 immutable Type0 route-mask assignment를 입력으로 사용해 12개 optional region의 유일한 attachment boundary에 `Basic / Tool / Environment / Explosive / Hidden` access rule과 perceptible clue를 deterministic하게 배정한다.

MAP06_04가 보존한 attachment→mandatory base-closed 상태는 절대 바꾸지 않는다. 이번 Task의 output은 이후 OptionalOverlayEdge/sector recipe가 사용할 logical access-boundary reservation이다. 실제 `edge_signature_id`, microchunk socket, generated world edge/CSV, tile 파괴, door/device object를 생성하지 않는다.

MAP06_06 reward tier가 사용할 tool cost tier, explosive fuel cost, hidden clue difficulty를 명시적 settings에서 depth별로 투영한다. reward tier 자체, reward item, return policy/device, inactive buffer, validator, overlay/debug view는 구현하지 않는다.

MAP06_05 production symbol이 새로 생기므로 기존 phase-boundary negative assertions는 MAP06_05 symbols를 허용하고 MAP06_06+ future symbols만 금지하도록 필요한 기존 boundary test만 교정한다.

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
12. `REPORTS/MAP06_04_ASSIGN_TYPE0_ROUTE_MASKS_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP06_04_ASSIGN_TYPE0_ROUTE_MASKS
STATUS: PASS
MAP06_04: COMPLETE ELIGIBLE
MAP06_05_ASSIGN_ACCESS_RULES_AND_CLUES: LOCKED / DO NOT START
SHA-256: 7cfb055bb6cb1df24206b25a1a5f046936c7fbdf58bd4b307d476ead4f28ed7a
```

이 별도 patch가 적용된 뒤에만 MAP06_05를 실행한다. MAP06_06 이후 Task body는 읽거나 시작하지 않는다.

## Approved Input Baseline

```text
Type0 registered catalog: 12
Type0 catalog digest: a96d0c6860ea0ebf62ac9763efcb7a03fa61df932fde85b30cec76c4b0c50506
Optional regions/cells/assignments: 12/39/39
Internal reciprocal BaseEdges: 30
Attachment boundaries base-closed: 12
Mandatory boundary base-open: 0
Closed cross-region undirected adjacencies: 13
Type0 assignment digest: a26e73f25ff7267760a2507ec55b1acda3a6c39a8f3698fc37b190620ceca525
Growth digest: 1f00f718bdb8d79fbf88923be0d81e0075987267c024cc624086ee6194998caa
Mandatory graph nodes/directed/undirected/route cells: 47/96/48/47
Mandatory masks T1/T2/T3/T4_UD/T4_LUD/T4_RUD/T4_LRUD: 20/4/4/17/0/0/2
Assets meta: 3274
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
04_CSV_STARTER/edge_signatures.csv
04_CSV_STARTER/generation_profiles.csv
05_GENERATED_OUTPUT_SCHEMA/generated_world_edges.csv
```

reference는 access/clue/OptionalOverlayEdge 용어와 future output boundary 확인용이다. installed Authoring CSV body를 runtime source로 다시 파싱하거나 수정하지 않는다. 이번 Task의 source of truth는 approved MAP06_04 result와 caller-supplied immutable settings다.

## READ ALLOWLIST

### Existing domain / P03~P04 production

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraph.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteValidationReport.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionAttachment.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegion.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionSnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionGrowthResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteOpenMask.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteMaskId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteMaskRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteMaskAssignment.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteMaskAssignmentDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteMaskAssignmentResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteMaskAssigner.cs
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
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

위 파일과 matching meta, approved Runtime/Test `Generation` path-only inventory, Authoring CSV/meta count·aggregate hash, 전체 Assets meta GUID, patch marker 이후 change-scope path만 확인할 수 있다.

금지: installed Authoring CSV body runtime 재파싱·수정, generated CSV source 사용, MAP06_06+ Task body, unrelated production/test body, Legacy/Stage/P6/P11 generator body, Scene/Prefab YAML.

## WRITE ALLOWLIST

### 신규 Runtime production C# — exact 8

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessClueId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessAssignmentEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessClue.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessAssignmentSettings.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessAssignment.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessAssignmentDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessAssignmentResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessRuleAssigner.cs
```

### 신규 Runtime EditMode test — exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalAccessRuleAssignerTests.cs
```

### 기존 phase-boundary test 수정 — exact up to 10

MAP06_05 production symbol 허용 및 MAP06_06+ future symbol 금지 유지를 위해 필요한 경우 아래 existing test C#만 수정할 수 있다. matching `.cs.meta`는 수정하지 않는다.

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
```

신규 C# 9개와 matching `.cs.meta` 9개, 위 boundary test C# 최대 10개 수정, Result 1개만 허용한다. MAP05/MAP06_01~04 production source, mandatory graph/mask, Type0 assignment, OptionalRegion models, Authoring/generated CSV/meta, asmdef, Scene, Prefab, Packages, ProjectSettings는 수정하지 않는다. 신규 directory/folder meta/asmdef/asmref를 만들지 않는다.

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
Input artifact   = TYPE0_ROUTE_MASK_ASSIGNMENT_SNAPSHOT
Read-only guards = OPTIONAL_REGION_TOPOLOGY + MAP05 MANDATORY GRAPH IDENTITY
Output artifact  = OPTIONAL_ACCESS_AND_CLUE_RESERVATION_SNAPSHOT
Pass ID          = PASS_OPTIONAL
RNG consumption  = exact 0
```

logical access reservation은 generated edge가 아니다. base mask/edge를 열지 않고 concrete edge signature/socket/recipe/CSV를 선택하지 않는다.

## Enum / Token Contract

`OptionalAccessAssignmentEnums.cs`는 아래 exact enum과 ordinal token codec을 제공한다.

```text
OptionalAccessRequirement: None, Pickaxe, Shovel, Rope, Explosive, Environment
OptionalAccessClueKind: BasicOpening, ToolSurface, EnvironmentDevice,
                        ExplosiveRewardPreview, HiddenCrack, HiddenLight, HiddenSound
OptionalAccessTraversalKind: OptionalBreak, Hidden
```

exact tokens:

```text
NONE | PICKAXE | SHOVEL | ROPE | EXPLOSIVE | ENVIRONMENT
BASIC_OPENING | TOOL_SURFACE | ENVIRONMENT_DEVICE | EXPLOSIVE_REWARD_PREVIEW
HIDDEN_CRACK | HIDDEN_LIGHT | HIDDEN_SOUND
OPTIONAL_BREAK | HIDDEN
```

null/empty/space/case variation/numeric/undefined enum을 거부한다. locale case-fold나 `Enum.Parse`를 계약 구현으로 사용하지 않는다.

## `OptionalAccessClueId` / `OptionalAccessClue` Contract

`OptionalAccessClueId`는 `public readonly struct`, `IEquatable`, `IComparable`이다.

```text
string Value
bool IsValid
OptionalAccessClueId(string value)
bool TryCreate(string value, out OptionalAccessClueId result)
```

grammar exact `^CLUE_OPT_REGION_[0-9]{4}_[A-Z0-9_]+$`; default invalid; equality/order는 ordinal case-sensitive, hash는 deterministic이다.

`OptionalAccessClue` sealed immutable properties:

```text
OptionalAccessClueId ClueId
OptionalRegionId RegionId
OptionalAccessClueKind Kind
int AttachmentOrder
bool IsPerceptibleFromMandatory
bool RequiresRewardPreview
```

- 모든 region은 exact one clue를 가진다.
- 모든 clue는 mandatory side에서 perceptible해야 한다.
- Explosive는 `ExplosiveRewardPreview`, `RequiresRewardPreview=true`다. reward ID/item은 아직 없다.
- Hidden은 `HiddenCrack | HiddenLight | HiddenSound` 중 하나다.
- Basic/Tool/Environment는 각각 exact clue kind를 가진다.

## `OptionalAccessAssignmentSettings` Contract

sealed immutable object다. constructor에서 아래 explicit copied lists를 모두 요구한다.

```text
IReadOnlyList<OptionalRegionAccessRule> AccessRulePattern
IReadOnlyList<OptionalAccessRequirement> ToolRequirementPattern
IReadOnlyList<OptionalAccessClueKind> HiddenCluePattern
IReadOnlyList<int> ToolCostTierByDepth
IReadOnlyList<int> ExplosiveFuelCostByDepth
IReadOnlyList<int> HiddenClueDifficultyByDepth
```

- AccessRulePattern은 non-empty이며 undefined enum이 없다.
- approved fixture pattern은 `Basic/Tool/Environment/Explosive/Hidden`이다.
- ToolRequirementPattern은 non-empty, only `Pickaxe/Shovel/Rope`; approved `Pickaxe/Shovel/Rope`.
- HiddenCluePattern은 non-empty, only `HiddenCrack/HiddenLight/HiddenSound`; approved `Crack/Light/Sound`.
- depth lists는 exact 4 entries for depth 1..4다.
- ToolCostTier는 `1..4`; approved `1/2/3/4`.
- ExplosiveFuelCost는 `1..100`; approved `10/20/30/40`.
- HiddenClueDifficulty는 `1..4`; approved `1/2/3/4`.
- caller list mutation, culture, enumeration order가 settings를 바꾸면 안 된다.
- static mutable/default settings instance를 만들지 않는다.

## Assignment Algorithm

1. MAP06_04 result가 `Completed`이고 source regions/cells/assignments `12/39/39`, attachment base-closed `12`, mandatory base-open `0` identity를 fixture gate에서 확인한다.
2. region은 RegionId ordinal canonical order로 copied 정렬하고 contiguous `OPT_REGION_0000..` identity를 검증한다.
3. access rule은 `regionOrdinal % AccessRulePattern.Count`로 선택한다.
4. Tool region은 tool-region ordinal로 ToolRequirementPattern을 순환한다.
5. Hidden region은 hidden-region ordinal로 HiddenCluePattern을 순환한다.
6. cost inputs는 region `MaxDepth.Value - 1` index의 depth list에서 선택한다.
7. clue ID는 exact `CLUE_<RegionId.Value>_<AccessRule token>`으로 만든다.
8. 모든 region을 검증한 뒤에만 immutable result를 원자적으로 publish한다.

approved 12-region pattern distribution은 `Basic/Tool/Environment/Explosive/Hidden = 3/3/2/2/2`다.

## Rule Consistency Matrix

| AccessRule | Requirement | Traversal | Clue | Cost fields |
|---|---|---|---|---|
| Basic | None | OptionalBreak | BasicOpening | all 0 |
| Tool | Pickaxe/Shovel/Rope | OptionalBreak | ToolSurface | tool tier by depth only |
| Environment | Environment | OptionalBreak | EnvironmentDevice | all 0 |
| Explosive | Explosive | OptionalBreak | ExplosiveRewardPreview | fuel by depth only |
| Hidden | None | Hidden | HiddenCrack/Light/Sound | clue difficulty by depth only |

undefined/mismatched matrix, duplicate region/clue ID, non-perceptible clue, invalid boundary direction, non-closed attachment base side, or source digest/accounting mismatch는 atomic failure다.

## `OptionalAccessAssignment` Contract

sealed immutable object다.

```text
OptionalRegionId RegionId
int RegionOrdinal
int AttachmentOrder
int MandatoryRouteSectorIndex
SectorCoord MandatoryRouteSector
int EntrySectorIndex
SectorCoord EntrySector
int EntrySideFromMandatoryDx
int EntrySideFromMandatoryDy
OptionalRegionAccessRule AccessRule
OptionalAccessRequirement Requirement
OptionalAccessTraversalKind TraversalKind
OptionalAccessClue Clue
int ToolCostTier
int ExplosiveFuelCost
int HiddenClueDifficulty
bool RequiresPartialRewardPreview
```

source attachment identity를 그대로 보존한다. assignment는 source OptionalRegion/Type0 result를 mutate하지 않는다.

## Diagnostics / Result Contract

`OptionalAccessAssignmentDiagnostics` sealed immutable fields:

```text
int SourceRegionCount
int SourceCellCount
int SourceType0AssignmentCount
int AssignmentCount
int ClueCount
int BasicCount
int ToolCount
int EnvironmentCount
int ExplosiveCount
int HiddenCount
int PickaxeCount
int ShovelCount
int RopeCount
int HiddenCrackCount
int HiddenLightCount
int HiddenSoundCount
int PerceptibleClueCount
int RewardPreviewReservationCount
int AttachmentBoundaryBaseOpenCount
int RngDrawCount
int SourceMutationCount
```

`OptionalAccessAssignmentResult` sealed immutable properties:

```text
OptionalAccessAssignmentStatus Status
IReadOnlyList<OptionalAccessAssignment> Assignments
IReadOnlyList<OptionalAccessClue> Clues
OptionalAccessAssignmentDiagnostics Diagnostics
IReadOnlyList<OptionalAccessAssignmentError> Errors
string SourceType0AssignmentDigest
string SourceGrowthDigest
string CanonicalDigest
int RngDrawCount
bool IsSuccess
```

status exact `Completed | InvalidInput | InvalidSettings | InvalidBoundary | InvalidAssignment`. errors는 code, RegionId, attachment order, clue ID, message 순으로 stable sort/dedupe한다. 실패는 assignments/clues empty, canonical digest empty, RNG/mutation 0, partial publication 0이다.

CanonicalDigest는 source digests, copied settings, assignments, clues, diagnostics를 stable UTF-8 ordinal field order로 SHA-256한 lowercase 64-hex다.

## `OptionalAccessRuleAssigner` Contract

stateless sealed service다.

```text
OptionalAccessAssignmentResult Assign(
    Type0RouteMaskAssignmentResult type0Assignments,
    OptionalAccessAssignmentSettings settings)
```

- reference input null과 invalid source digest/accounting을 거부한다.
- MAP06_04 base masks/assignments, OptionalRegion, attachment, mandatory graph identity를 mutate하지 않는다.
- approved fixture exact counts/digests는 fixture test/Result에서 검증하고 production 일반 입력 수량으로 하드코딩하지 않는다.
- input caller order, culture, service reuse, thread/time에 독립적이다.
- hidden cache, static mutable collection, reflection, filesystem, Registry singleton, RNG는 0이다.

## Boundary Test Advance

허용할 MAP06_05 symbols:

```text
OptionalAccessClueId
OptionalAccessAssignmentEnums
OptionalAccessClue
OptionalAccessAssignmentSettings
OptionalAccessAssignment
OptionalAccessAssignmentDiagnostics
OptionalAccessAssignmentResult
OptionalAccessRuleAssigner
OptionalAccessRuleAssignerTests
```

계속 금지할 MAP06_06+ examples:

```text
OptionalRewardTierCalculator
OptionalReturnPolicyResolver
InactiveBufferAssigner
OptionalRegionValidator
OptionalRegionOverlay
GeneratedOptionalRegionCsvWriter
```

MAP05 Type4와 MAP06_04 base-closed/L+R assertions를 약화하지 않는다. MAP06_06+ 금지 case 수를 줄이지 않는다.

## Required Tests

새 `OptionalAccessRuleAssignerTests`는 최소 `250` actual PASS cases를 가져야 한다.

- clue ID/enums/token codec/value immutability `>=34`
- settings validation/copy/depth tables `>=34`
- five-rule distribution and rule matrix `>=40`
- tool requirement/hidden clue cycling `>=26`
- attachment identity/base-closed preservation `>=30`
- clue perceptibility/explosive preview reservation `>=24`
- costs/digests/culture/order/service reuse `>=24`
- atomic invalid input/settings/boundary failure `>=18`
- RNG/mutation/Type4/boundary advance `>=20`

Existing required gates:

```text
OptionalAccessRuleAssignerTests >=250 PASS
Type0RouteMaskAssignerTests 257/257 PASS
MAP06 prior combined selection 630/630 PASS
Existing MAP05 aggregate 1959/1959 PASS
Actually executed required total >=3096 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
Assets meta 3274 -> 3283
new C#/meta 9/9
existing boundary test C# modified <=10
Authoring CSV/meta 50/50
Authoring manifest SHA-256 unchanged
duplicate GUID groups 0
MAP05/MAP06_01~04 production/graph/mask/models/assignments/CSV/asmdef/Scene/Prefab/Packages/ProjectSettings modifications 0
generated CSV files created by this task 0
```

Discovery-only count나 arithmetic total을 PASS로 기록하지 않는다. 위 total은 Unity EditMode Test Runner에서 실제 실행한 결과여야 한다.

## Result Format

Write exactly `MapDesign/MCP/REPORTS/MAP06_05_ASSIGN_ACCESS_RULES_AND_CLUES_RESULT.md`.

Required top lines:

```text
TASK: MAP06_05_ASSIGN_ACCESS_RULES_AND_CLUES
STATUS: PASS|FAIL|BLOCKED
MAP06_05: COMPLETE ELIGIBLE|NOT COMPLETE
MAP06_06_CALCULATE_OPTIONAL_REWARD_TIER: LOCKED / DO NOT START
```

Result must include patch/receipt, prior Result and Task SHA gates, created/changed files, approved settings, five-rule/tool/hidden-clue distribution, all assignment and cost inputs, perceptible clue and preview counts, attachment identity/base-closed evidence, source/canonical digests, atomic failure and mutation evidence, MAP05/Type4 and MAP06_04 preservation, exact test jobs, Unity gate, asset/meta/CSV/GUID/change-scope gate, and NEXT that finalizes only MAP06_05 while keeping MAP06_06 locked.

## Done Conditions

- [ ] Preconditions, prior Result SHA, current Task SHA verified; MAP06_05 is sole CURRENT.
- [ ] Every optional region has exact one access assignment and perceptible clue.
- [ ] Five access rules obey the exact requirement/traversal/clue/cost matrix.
- [ ] Explosive reserves partial reward preview; Hidden has Crack/Light/Sound clue.
- [ ] Attachment boundary remains base-closed and no generated edge/signature/CSV is created.
- [ ] MAP06_05 symbols allowed; MAP06_06+ forbidden.
- [ ] No reward tier/return/inactive/validator/overlay behavior.
- [ ] Mandatory graph/masks, Type4, Type0 assignment and Authoring CSV unchanged.
- [ ] Required Unity EditMode gates actually executed and Result evidence complete.

If any required gate fails or Unity MCP is unavailable, write `STATUS: FAIL` or `STATUS: BLOCKED`, keep MAP06_05 CURRENT, and do not create or start MAP06_06.

