# MAP06_09 — Implement Optional Region Validator

```yaml
status_control:
  task_key: MAP06_09_IMPLEMENT_OPTIONAL_REGION_VALIDATOR
  result_file: REPORTS/MAP06_09_IMPLEMENT_OPTIONAL_REGION_VALIDATOR_RESULT.md
```

## TASK TYPE

```text
RUNTIME IMMUTABLE P04 OPTIONAL REGION VALIDATION REPORT + SOURCE-CHAIN DIGEST VALIDATION + RULE/ACCOUNTING/RETURNABILITY CHECKS + PHASE-BOUNDARY TEST ADVANCE + EDITMODE TESTS
```

## Objective

MAP06_08 PASS/finalize 뒤 approved MAP06 source-chain을 하나의 immutable validation report로 결합한다. 이 Task는 optional region 결과가 MAP05 mandatory graph를 변경하지 않았고, Type0 route-mask/access/clue/reward/return/inactive-buffer 계약을 모두 만족하는지 검증한다.

검증 대상은 source identity, mandatory graph identity, Type0 `!(L&&R)`, returnability, visible clue presence, mandatory reward 금지, inactive full-world accounting, approved reserved-adapter overlap, digest chain, RNG/source mutation zero다. 결과는 runtime-only `OptionalRegionValidationReport`다.

MAP06_10 overlay/exit, generated CSV writer, access color overlay, debug view, boundary profile, recipe, microchunk, tile, socket, edge, scene/prefab authoring은 구현하지 않는다. MAP06_09 production symbols가 새로 생기므로 기존 phase-boundary negative assertions는 MAP06_09 symbols를 허용하고 MAP06_10+ future symbols만 금지하도록 필요한 boundary test만 교정한다.

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
12. `REPORTS/MAP06_08_ASSIGN_INACTIVE_BUFFERS_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP06_08_ASSIGN_INACTIVE_BUFFERS
STATUS: PASS
MAP06_08: COMPLETE ELIGIBLE
MAP06_09_IMPLEMENT_OPTIONAL_REGION_VALIDATOR: LOCKED / DO NOT START
SHA-256: 43dd272802bfe6094ac5f1dff91ddb30229acf0c5a0885742509945a496bf58b
```

이 별도 patch가 적용된 뒤에만 MAP06_09을 실행한다. MAP06_10 이후 Task body는 읽거나 시작하지 않는다.

## Approved Input Baseline

```text
World sectors / dimensions: 169 / 13x13
Site reservations / reserved sectors / entries / Core seeds: 7 / 8 / 6 / 4
Biome publication sectors / assigned / reserved-unassigned: 169 / 165 / 4
Mandatory graph nodes / directed / undirected / route cells: 47 / 96 / 48 / 47
Optional regions / Type0 cells: 12 / 39
Access assignments / clues / perceptible clues: 12 / 12 / 12
Reward-tier assignments: 12
Return assignments / returnable / non-returnable: 12 / 39 / 0
InactiveBuffer assignments: 78
DecorativeBoundary / InteriorInactive: 52 / 26
Protected union: 91
Source counts ReservedSite/Mandatory/Type0: 8 / 47 / 39
Approved Site ∩ Mandatory overlap: 3 at sectors 0,28,106
Exclusive ReservedSite/MandatoryOnly/Type0: 8 / 44 / 39
Full-world accounting: 169 = 8 + 44 + 39 + 78
Unassigned / illegal overlap / duplicate / open edge to inactive: 0 / 0 / 0 / 0
Mandatory graph digest: MAP05_GRAPH_47_96_48_47
Growth digest: 1f00f718bdb8d79fbf88923be0d81e0075987267c024cc624086ee6194998caa
Type0 assignment digest: a26e73f25ff7267760a2507ec55b1acda3a6c39a8f3698fc37b190620ceca525
Access assignment digest: 5268b7ed2342f197fb5717c85dbfdb8e51b3c520c217ecf93ad14dc944ecf74f
Reward-tier digest: c3430c42a27937e143fa89c5839282b9533b62d5fb74fb26fdad490cb545958e
Return-policy digest: cff0556a59e66fcc16b886ecf3082779efe9535bb79dcf45b401d12ff0971f6b
Inactive assignment digest: 426f269e39d8a2d75a93020a00c7bb617612c00dd60a663fdbeffc60f8ea9578
Attachment base-closed / mandatory base-open: 12 / 0
RNG/source mutation/partial publication: 0 / 0 / 0
Assets meta: 3304
Authoring CSV/meta: 50 / 50
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
01_FIXED_SPEC/06_ROUTE_TOPOLOGY_CONSTRAINTS.md
01_FIXED_SPEC/07_OPTIONAL_EDGE_OVERLAY.md
02_PHASE_ROADMAP/MAP06_TYPE0_OPTIONAL_REGIONS.md
03_CSV_SCHEMA/ENUM_REFERENCE.md
03_CSV_SCHEMA/TILE_AUTHORING_FORMAT.md
```

reference는 optional region phase gate, final role ownership, mandatory path preservation, returnability, clue/reward rules, and MAP06 overlay handoff를 확인하는 용도다. Authoring CSV body를 runtime source로 다시 파싱하거나 수정하지 않는다. overlay와 generated output은 MAP06_10 소유다.

## READ ALLOWLIST

### Existing domain / P00~P04 production

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGridIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedWorldData.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchValidationPublication.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphNodeId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphNode.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphEdge.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraph.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteValidationReport.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionAttachment.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegion.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionSnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteOpenMask.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteMaskAssignment.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteMaskAssignmentDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteMaskAssignmentResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessAssignmentEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessAssignment.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessAssignmentDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessAssignmentResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRewardTierCalculationEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRewardTierAssignment.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRewardTierDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRewardTierResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalReturnPolicyAssignment.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalReturnPolicyDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalReturnPolicyResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/InactiveBufferAssignment.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/InactiveBufferAssignmentDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/InactiveBufferAssignmentResult.cs
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
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRewardTierCalculatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalReturnPolicyResolverTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/InactiveBufferAssignerTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

위 파일과 matching meta, approved Runtime/Test `Generation` path-only inventory, Authoring CSV/meta count·aggregate hash, 전체 Assets meta GUID, patch marker 이후 change-scope path만 확인할 수 있다.

금지: installed Authoring CSV body runtime 재파싱·수정, generated CSV body source 사용, MAP06_10+ Task body, boundary profile/recipe/microchunk/tile/socket/edge body, unrelated production/test body, Legacy/Stage/P6/P11 generator body, Scene/Prefab YAML.

## WRITE ALLOWLIST

### 신규 Runtime production C# — exact 6

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionValidationEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionValidationSettings.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionValidationIssue.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionValidationDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionValidationReport.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionValidator.cs
```

### 신규 Runtime EditMode test — exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionValidatorTests.cs
```

### 기존 phase-boundary test 수정 — exact up to 14

MAP06_09 production symbol 허용 및 MAP06_10+ future symbol 금지 유지를 위해 필요한 경우 아래 existing test C#만 수정할 수 있다. matching `.cs.meta`는 수정하지 않는다.

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
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRewardTierCalculatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalReturnPolicyResolverTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/InactiveBufferAssignerTests.cs
```

신규 C# 7개와 matching `.cs.meta` 7개, 위 boundary test C# 최대 14개 수정, Result 1개만 허용한다. MAP05/MAP06_01~08 production source, world/site/biome/mandatory/Type0/access/reward/return/inactive artifacts, Authoring/generated CSV/meta, asmdef, Scene, Prefab, Packages, ProjectSettings는 수정하지 않는다. 신규 directory/folder meta/asmdef/asmref를 만들지 않는다.

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
Input artifacts  = OPTIONAL REGION SNAPSHOT + TYPE0 + ACCESS/CLUE + REWARD + RETURN + INACTIVE BUFFER
Read-only guards = P00/P01/P02 + MAP05 MANDATORY GRAPH IDENTITY
Output artifact  = OPTIONAL_REGION_VALIDATION_REPORT
Pass ID          = PASS_OPTIONAL
RNG consumption  = exact 0
```

output은 immutable validation report다. source artifacts를 in-place mutate하지 않고 generated CSV, overlay, edge, recipe, socket, tile marker를 만들지 않는다.

## New Runtime Contracts

신규 `OptionalRegionValidationEnums.cs`:

```text
OptionalRegionValidationStatus:
  Valid | InvalidInput | InvalidSettings | InvalidSource
  InvalidTopology | InvalidAccounting | InvalidRules

OptionalRegionValidationIssueCode:
  NullInput | InvalidStatus | InvalidDigest | SourceMismatch
  InvalidWorld | InvalidMandatoryGraph | InvalidOptionalRegionSnapshot
  InvalidType0Assignment | InvalidAccessAssignment | InvalidRewardAssignment
  InvalidReturnPolicy | InvalidInactiveBufferAssignment
  MissingAccessRule | MissingVisibleClue | MissingRewardTier
  MissingReturnPolicy | NonReturnableOptionalCell | MandatoryRewardAssigned
  Type0LeftRightOpen | OpenEdgeToInactive | InactiveAccountingMismatch
  ReservedAdapterMismatch | DuplicateRegion | DuplicateSector
  RegionIdentityMismatch | RngConsumed | SourceMutation
```

undefined enum을 거부한다. authoring token codec을 새로 만들지 않는다.

`OptionalRegionValidationSettings` sealed immutable object:

```text
bool RequireMandatoryGraphIdentity
bool RequireSourceDigests
bool RequireRegionIdentity
bool RequireType0NoLeftRight
bool RequireReturnability
bool RequireVisibleClues
bool ForbidMandatoryRewards
bool RequireInactiveFullAccounting
bool RequireNoRngOrSourceMutation
```

approved settings는 모두 exact `true`다. false는 이번 Task의 frozen contract와 맞지 않으므로 invalid settings다. static mutable/default settings instance를 만들지 않는다.

`OptionalRegionValidationIssue` sealed immutable object:

```text
OptionalRegionValidationIssueCode Code
OptionalRegionId RegionId
int SectorIndex
string Source
string Field
string Message
```

Issues are stable sorted and deduped by code, region id, sector index, source, field, message. No culture-sensitive ordering.

`OptionalRegionValidationDiagnostics` sealed immutable fields:

```text
int WorldSectorCount
int MandatoryRouteCellCount
int OptionalRegionCount
int Type0CellCount
int AccessAssignmentCount
int VisibleClueCount
int RewardAssignmentCount
int MandatoryRewardAssignmentCount
int ReturnAssignmentCount
int ReturnableCellCount
int NonReturnableCellCount
int InactiveBufferAssignmentCount
int DecorativeBoundaryCount
int InteriorInactiveCount
int ProtectedUnionCount
int ApprovedReservedAdapterOverlapCount
int OpenEdgeToInactiveCount
int Type0LeftRightOpenCount
int MissingClueCount
int MissingReturnPolicyCount
int IssueCount
int RngDrawCount
int SourceMutationCount
```

`OptionalRegionValidationReport` sealed immutable properties:

```text
OptionalRegionValidationStatus Status
OptionalRegionValidationDiagnostics Diagnostics
IReadOnlyList<OptionalRegionValidationIssue> Issues
string SourceMandatoryGraphDigest
string SourceGrowthDigest
string SourceType0AssignmentDigest
string SourceAccessAssignmentDigest
string SourceRewardTierDigest
string SourceReturnPolicyDigest
string SourceInactiveAssignmentDigest
string CanonicalDigest
int RngDrawCount
bool IsValid
```

failure는 `IsValid == false`, canonical digest empty, RNG/source mutation `0/0`, partial publication `0`이다. CanonicalDigest는 source digests, copied settings, region-index ordered validation facts, diagnostics, and issues를 stable UTF-8 ordinal field order로 SHA-256한 lowercase 64-hex다.

## `OptionalRegionValidator` Contract

stateless sealed service다.

```text
OptionalRegionValidationReport Validate(
    GeneratedWorldData world,
    SiteReservationSnapshot siteReservations,
    BiomePatchValidationPublication biomePublication,
    MandatoryRouteGraph graph,
    MandatoryRouteValidationReport mandatoryValidation,
    OptionalRegionSnapshot optionalRegions,
    Type0RouteMaskAssignmentResult type0Assignments,
    OptionalAccessAssignmentResult accessAssignments,
    OptionalRewardTierResult rewardTiers,
    OptionalReturnPolicyResult returnPolicies,
    InactiveBufferAssignmentResult inactiveBuffers,
    OptionalRegionValidationSettings settings)
```

- reference input null, invalid status/digest/accounting, source-chain mismatch를 거부한다.
- source object/collection을 copied deterministic order로 읽고 mutate하지 않는다.
- caller order, culture, service reuse, thread/time에 독립적이다.
- hidden cache, static mutable collection, reflection, filesystem, Registry singleton, RNG는 0이다.
- dictionary/hash-set iteration order를 output order로 사용하지 않는다.

## Validation Rules

validator는 다음을 모두 검증한다.

1. world는 exact 13x13, 169 cells, unique row-major sector index/coordinate identity다.
2. site, biome, mandatory validation report는 approved valid publication이며 mandatory graph identity는 `47/96/48/47`이다.
3. optional region snapshot은 12 regions, 39 unique cells, attachment identities, depth buckets, and growth digest를 보존한다.
4. Type0 result는 `Completed`, 12 assignments, 39 cells, digest `a26e...`, no L+R open, attachment base-closed `12`, mandatory base-open `0`이다.
5. access result는 `Completed`, 12 assignments, 12 visible/perceptible clues, digest `5268...`, source Type0/growth digest를 보존한다.
6. reward result는 `Completed`, 12 assignments, tier distribution `5/1/2/4`, mandatory reward assignments `0`, digest `c343...`, source access/growth digest를 보존한다.
7. return result는 `Completed`, 12 assignments, Backtrack/ReturnGate/SafeExit `12/0/0`, returnable/non-returnable `39/0`, digest `cff0...`, source Type0/growth digest를 보존한다.
8. inactive result는 `Completed`, approved adapter overlap `{0,28,106}`, protected union `91`, inactive assignments `78`, DecorativeBoundary/InteriorInactive `52/26`, open edge to inactive `0`, digest `426f...`이다.
9. region identity is one-to-one across optional region, Type0, access, reward, and return artifacts. Missing, duplicate, or reordered source collections must not affect canonical output.
10. no optional reward is assigned to mandatory route cells, ReservedSite cells, inactive cells, or other non-Type0 cells.
11. no optional route opening points into inactive cells; MAP06_08's approved reserved adapters remain protected and not inactive.
12. RNG draw count, source mutation count, partial publication count are all `0`.

approved fixture exact counts/digests는 test/Result에서 검증하고 production service에 특정 split이나 canonical digest를 하드코딩하지 않는다.

## Boundary Test Advance

허용할 MAP06_09 symbols:

```text
OptionalRegionValidationStatus
OptionalRegionValidationIssueCode
OptionalRegionValidationSettings
OptionalRegionValidationIssue
OptionalRegionValidationDiagnostics
OptionalRegionValidationReport
OptionalRegionValidator
OptionalRegionValidatorTests
```

계속 금지할 MAP06_10+ examples:

```text
OptionalRegionOverlay
Map06ExitTests
GeneratedOptionalRegionCsvWriter
OptionalRegionOverlayRenderer
OptionalRegionValidationOverlayWindow
```

MAP05 Type4, MAP06_04 Type0 base-closed/L+R, MAP06_05 access/clue, MAP06_06 score/tier, MAP06_07 returnability, MAP06_08 inactive accounting assertions를 약화하지 않는다. MAP06_10+ 금지 case 수를 줄이지 않는다.

## Required Tests

새 `OptionalRegionValidatorTests`는 최소 `320` actual PASS cases를 가져야 한다.

- enum/settings/report immutability `>=36`
- source status and digest validation `>=40`
- region identity across artifacts `>=38`
- Type0 mask, L+R, and base-boundary rules `>=36`
- access/clue/reward/mandatory reward rules `>=38`
- return policy and returnability proof `>=36`
- inactive accounting and reserved-adapter validation `>=36`
- issue ordering, determinism, no mutation/RNG, boundary advance `>=60`

Existing required gates:

```text
OptionalRegionValidatorTests >=320 PASS
InactiveBufferAssignerTests 281/281 PASS
OptionalReturnPolicyResolverTests 289/289 PASS
OptionalRewardTierCalculatorTests 279/279 PASS
OptionalAccessRuleAssignerTests 289/289 PASS
Type0RouteMaskAssignerTests 257/257 PASS
MAP06 prior combined selection 630/630 PASS
Existing MAP05 aggregate 1959/1959 PASS
Actually executed required total >=4304 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
Assets meta 3304 -> 3311
new C#/meta 7/7
existing boundary test C# modified <=14
Authoring CSV/meta 50/50
Authoring manifest SHA-256 unchanged
duplicate GUID groups 0
MAP05/MAP06_01~08 production/world/site/biome/graph/mask/models/assignments/CSV/asmdef/Scene/Prefab/Packages/ProjectSettings modifications 0
generated CSV files created by this task 0
boundary profile/recipe/microchunk/tile/socket/edge/overlay artifacts created 0
```

Discovery-only count나 arithmetic total을 PASS로 기록하지 않는다. 위 total은 Unity EditMode Test Runner에서 실제 실행한 결과여야 한다.

## Result Format

Write exactly `MapDesign/MCP/REPORTS/MAP06_09_IMPLEMENT_OPTIONAL_REGION_VALIDATOR_RESULT.md`.

Required top lines:

```text
TASK: MAP06_09_IMPLEMENT_OPTIONAL_REGION_VALIDATOR
STATUS: PASS|FAIL|BLOCKED
MAP06_09: COMPLETE ELIGIBLE|NOT COMPLETE
MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS: LOCKED / DO NOT START
```

Result must include patch/receipt, prior Result and Task SHA gates, created/changed files, source-chain digests, settings, world/site/biome/mandatory/optional/Type0/access/reward/return/inactive accounting, exact validation diagnostics, zero issue proof or exact sorted issues, canonical validation digest, MAP05/Type4 and MAP06_04~08 preservation, exact test jobs, Unity gate, asset/meta/CSV/GUID/change-scope gate, and NEXT that finalizes only MAP06_09 while keeping MAP06_10 locked.

## Done Conditions

- [ ] Preconditions, prior Result SHA, current Task SHA verified; MAP06_09 is sole CURRENT.
- [ ] P00/P01/P02/MAP05/MAP06_01~08 source identities and statuses verified.
- [ ] Mandatory graph identity `47/96/48/47` and Type4 contract preserved.
- [ ] Type0 cells are `39`, region count `12`, and no L+R open mask exists.
- [ ] Every region has access assignment, visible clue, reward tier, and return policy.
- [ ] Mandatory reward assignments are `0`.
- [ ] Optional returnability is `39/0` returnable/non-returnable with no synthetic return gate or safe exit.
- [ ] Inactive accounting is `169 = 8 + 44 + 39 + 78`, protected union `91`, adapter overlap `{0,28,106}`.
- [ ] No mandatory or Type0 open edge targets an inactive sector.
- [ ] No boundary profile/recipe/microchunk/tile/socket/edge/overlay/generated CSV is synthesized.
- [ ] MAP06_09 symbols allowed; MAP06_10+ forbidden.
- [ ] Required Unity EditMode gates actually executed and Result evidence complete.

If any required gate fails or Unity MCP is unavailable, write `STATUS: FAIL` or `STATUS: BLOCKED`, keep MAP06_09 CURRENT, and do not create or start MAP06_10.
