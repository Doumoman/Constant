# MAP06_07 — Implement Return Policy

```yaml
status_control:
  task_key: MAP06_07_IMPLEMENT_RETURN_POLICY
  result_file: REPORTS/MAP06_07_IMPLEMENT_RETURN_POLICY_RESULT.md
```

## TASK TYPE

```text
RUNTIME IMMUTABLE P04 OPTIONAL BACKTRACK RETURN-POLICY RESOLUTION + ALL-CELL RETURNABILITY PROOF + SOURCE-CHAIN VALIDATION + PHASE-BOUNDARY TEST ADVANCE + EDITMODE TESTS
```

## Objective

MAP06_04 Type0 snapshot, MAP06_05 access/clue assignment, MAP06_06 reward-tier reservation을 immutable source-chain으로 결합해 모든 optional region이 원래 mandatory attachment로 안전하게 복귀할 수 있음을 증명하고 existing `OptionalReturnPolicy.BacktrackToAttachment`를 기록한다.

approved fixture의 Type0 internal BaseEdge는 reciprocal이고 모든 region cell이 attachment cell과 연결돼 있다. 따라서 synthetic return gate나 별도 safe exit를 만들지 않고, 각 region의 가장 깊은 canonical cell에서 attachment까지의 deterministic shortest witness와 전체 cell returnability를 계산한다. 열린/discovered optional attachment boundary의 동일 면을 역방향으로 사용해 mandatory sector로 돌아간다. base mask는 계속 closed다.

MAP06_01의 existing `OptionalReturnPolicy: BacktrackToAttachment / ReturnGateToMandatory / SafeExitToMandatory`를 그대로 재사용한다. 이번 source에는 별도 return-device/exit candidate artifact가 없으므로 successful output은 `BacktrackToAttachment`만 허용한다. `ReturnGateToMandatory` 또는 `SafeExitToMandatory`를 임의 배정하거나 device ID/prefab/socket/edge를 합성하지 않는다.

MAP06_08 inactive buffer, MAP06_09 validator, MAP06_10 overlay/exit는 구현하지 않는다. MAP06_07 production symbols가 새로 생기므로 기존 phase-boundary negative assertions는 MAP06_07 symbols를 허용하고 MAP06_08+ future symbols만 금지하도록 필요한 boundary test만 교정한다.

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
12. `REPORTS/MAP06_06_CALCULATE_OPTIONAL_REWARD_TIER_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP06_06_CALCULATE_OPTIONAL_REWARD_TIER
STATUS: PASS
MAP06_06: COMPLETE ELIGIBLE
MAP06_07_IMPLEMENT_RETURN_POLICY: LOCKED / DO NOT START
SHA-256: 0acfcd73b6485e99a56dd4d44bff50f871548e266ed003607466961632ec449c
```

이 별도 patch가 적용된 뒤에만 MAP06_07을 실행한다. MAP06_08 이후 Task body는 읽거나 시작하지 않는다.

## Approved Input Baseline

```text
Optional regions / Type0 cell assignments: 12 / 39
Access assignments / reward-tier assignments: 12 / 12
Type0 assignment digest: a26e73f25ff7267760a2507ec55b1acda3a6c39a8f3698fc37b190620ceca525
Access assignment digest: 5268b7ed2342f197fb5717c85dbfdb8e51b3c520c217ecf93ad14dc944ecf74f
Reward-tier canonical digest: c3430c42a27937e143fa89c5839282b9533b62d5fb74fb26fdad490cb545958e
Growth digest: 1f00f718bdb8d79fbf88923be0d81e0075987267c024cc624086ee6194998caa
Tier distribution Low/Medium/High/Unique: 5/1/2/4
Reward score min/max: 2/12
Internal reciprocal BaseEdges: 30
Attachment boundaries base-closed: 12
Mandatory boundary base-open: 0
All OptionalRegionCell.RequiresReturnConnection: false (39)
RNG/source mutation/partial publication: 0/0/0
Assets meta: 3290
Authoring CSV/meta: 50/50
Authoring manifest SHA-256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Duplicate GUID groups: 0
```

approved per-region MaxDepth sequence in RegionId order:

```text
4 / 1 / 1 / 3 / 4 / 1 / 4 / 1 / 4 / 1 / 3 / 4
critical witness sector-count total = 31
critical witness edge-count total = 19
maximum critical witness sector-count = 4
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
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
01_FIXED_SPEC/06_ROUTE_TOPOLOGY_CONSTRAINTS.md
01_FIXED_SPEC/07_OPTIONAL_EDGE_OVERLAY.md
02_PHASE_ROADMAP/MAP06_TYPE0_OPTIONAL_REGIONS.md
03_CSV_SCHEMA/TILE_AUTHORING_FORMAT.md
```

reference는 “optional edge 제거 후 mandatory 완주 가능”, “같은 선택 edge 역방향 또는 별도 복귀”, `M_SAFE` future authoring boundary를 확인하는 용도다. Authoring CSV body를 runtime source로 다시 파싱하거나 수정하지 않는다. 이번 Task에는 concrete microchunk/sector recipe/return device candidate가 없으므로 logical backtrack policy와 witness만 publication한다.

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
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessAssignment.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessAssignmentDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessAssignmentResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRewardTierCalculationEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRewardTierSettings.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRewardTierAssignment.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRewardTierDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRewardTierResult.cs
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
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

위 파일과 matching meta, approved Runtime/Test `Generation` path-only inventory, Authoring CSV/meta count·aggregate hash, 전체 Assets meta GUID, patch marker 이후 change-scope path만 확인할 수 있다.

금지: installed Authoring CSV body runtime 재파싱·수정, generated CSV body source 사용, MAP06_08+ Task body, concrete return device/edge/socket/recipe body, unrelated production/test body, Legacy/Stage/P6/P11 generator body, Scene/Prefab YAML.

## WRITE ALLOWLIST

### 신규 Runtime production C# — exact 6

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalReturnPolicyResolutionEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalReturnPolicySettings.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalReturnPolicyAssignment.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalReturnPolicyDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalReturnPolicyResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalReturnPolicyResolver.cs
```

### 신규 Runtime EditMode test — exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalReturnPolicyResolverTests.cs
```

### 기존 phase-boundary test 수정 — exact up to 12

MAP06_07 production symbol 허용 및 MAP06_08+ future symbol 금지 유지를 위해 필요한 경우 아래 existing test C#만 수정할 수 있다. matching `.cs.meta`는 수정하지 않는다.

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
```

신규 C# 7개와 matching `.cs.meta` 7개, 위 boundary test C# 최대 12개 수정, Result 1개만 허용한다. MAP05/MAP06_01~06 production source, mandatory graph/mask, Type0/access/reward assignments, OptionalRegion models, Authoring/generated CSV/meta, asmdef, Scene, Prefab, Packages, ProjectSettings는 수정하지 않는다. 신규 directory/folder meta/asmdef/asmref를 만들지 않는다.

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
Input artifacts  = TYPE0_ROUTE_MASK_ASSIGNMENT + OPTIONAL_ACCESS_AND_CLUE + OPTIONAL_REWARD_TIER
Read-only guards = OPTIONAL_REGION_TOPOLOGY + MAP05 MANDATORY GRAPH IDENTITY
Output artifact  = OPTIONAL_RETURN_POLICY_AND_BACKTRACK_WITNESS_SNAPSHOT
Pass ID          = PASS_OPTIONAL
RNG consumption  = exact 0
```

output은 logical policy/proof다. return gate/safe exit ID, prefab, socket, overlay edge, recipe, tile marker, generated edge/CSV를 만들지 않는다.

## Existing Return Policy / Resolution Enum Contract

MAP06_01의 `OptionalRegionEnums.cs`에 이미 정의된 아래 enum/token codec을 read-only로 재사용한다.

```text
OptionalReturnPolicy:
  BacktrackToAttachment | ReturnGateToMandatory | SafeExitToMandatory
tokens:
  BACKTRACK | RETURN_GATE | SAFE_EXIT
```

`OptionalRegionEnums.cs`를 수정하거나 duplicate `OptionalReturnPolicy` type/codec을 만들지 않는다. successful approved output은 `BacktrackToAttachment`만 사용한다.

신규 `OptionalReturnPolicyResolutionEnums.cs`:

```text
OptionalReturnPolicyResolutionStatus:
  Completed | InvalidInput | InvalidSettings | InvalidSource
  InvalidTopology | UnsupportedReturnRequirement

OptionalReturnPolicyResolutionErrorCode:
  NullInput | InvalidStatus | InvalidDigest | SourceMismatch | InvalidAccounting
  MissingRegion | DuplicateRegion | InvalidAttachment | InvalidBaseEdge
  NonReciprocalBaseEdge | UnreachableCell | PathLimitExceeded
  UnsupportedReturnRequirement
```

undefined enum을 거부한다. internal resolution status/error enum에 별도 authoring token codec을 만들지 않는다.

## `OptionalReturnPolicySettings` Contract

sealed immutable object다.

```text
int MaximumBacktrackSectorCount
bool RequireAllCellsReturnable
```

- `MaximumBacktrackSectorCount`는 `1..169`; approved `6`.
- `RequireAllCellsReturnable`는 exact `true`; false는 invalid settings다.
- static mutable/default settings instance를 만들지 않는다.
- settings는 return gate/safe exit/device 생성을 허용하는 switch를 갖지 않는다.

## Source-Chain Validation

resolver는 아래 exact identity를 순서대로 확인한다.

1. Type0, access, reward-tier results가 모두 `Completed`다.
2. access source Type0 digest와 reward source Type0 digest가 Type0 `CanonicalDigest`와 일치한다.
3. reward source access digest가 access `CanonicalDigest`와 일치한다.
4. 세 result의 source growth digest가 일치한다.
5. source snapshot regions, Type0 cell assignments, access assignments, reward-tier assignments를 RegionId로 exact one-to-one join한다.
6. attachment order/entry/mandatory direction, access rule/clue/preview, reward score/tier identity가 보존된다.
7. approved fixture exact counts/digests는 test/Result에서만 검증하고 production 일반 입력 수량으로 하드코딩하지 않는다.

## Internal Return Graph Algorithm

각 region을 RegionId ordinal canonical order로 처리한다.

1. Type0 per-cell `OpenMask`에서 같은 region neighbor로 향한 open side만 internal BaseEdge로 만든다.
2. neighbor의 opposite side가 open인지 검증해 reciprocal undirected edge를 canonical pair로 한 번만 기록한다.
3. attachment cell을 root로 BFS한다. fixed neighbor order는 `L, R, U, D`, then SectorIndex다.
4. 모든 region cell이 root에 도달해야 한다.
5. critical source는 `Depth` descending, then `SectorIndex` ascending의 첫 cell이다.
6. critical source에서 BFS parent를 따라 attachment cell까지 reverse shortest witness를 만든다. path는 source와 attachment를 모두 포함한다.
7. path sector count가 `MaximumBacktrackSectorCount` 이하여야 한다.
8. 모든 cell의 returnability와 witness를 검증한 뒤에만 `BacktrackToAttachment` assignment를 원자적으로 publish한다.

approved fixture:

```text
regions / cells / internal undirected BaseEdges = 12 / 39 / 30
returnable / non-returnable cells = 39 / 0
Backtrack / ReturnGate / SafeExit assignments = 12 / 0 / 0
critical witness sector-count / edge-count totals = 31 / 19
maximum witness sector-count = 4
same opened attachment returns = 12
return device/extra exit reservations = 0/0
attachment base-open = 0
```

모든 source cell의 `RequiresReturnConnection`은 false여야 한다. true가 하나라도 있거나 backtrack 불가능하면 candidate artifact 없이 device를 합성하지 않고 `UnsupportedReturnRequirement` 또는 `InvalidTopology`로 atomic failure한다.

## Attachment Reverse-Use Contract

- Basic/Tool/Environment/Explosive의 `OptionalBreak` boundary는 성공적으로 열린 뒤 같은 attachment 면을 역방향으로 사용한다.
- Hidden boundary는 발견 뒤 같은 `Hidden` passage를 역방향으로 사용한다.
- Explosive preview reservation은 그대로 `2`이며 return 경로에 reward item/device를 추가하지 않는다.
- attachment→mandatory base side는 계속 closed다. return은 base mask를 열지 않고 existing logical optional access boundary의 reverse-use policy를 기록할 뿐이다.
- mandatory graph/route mask와 Type4를 수정하지 않는다.

## `OptionalReturnPolicyAssignment` Contract

sealed immutable object다.

```text
OptionalRegionId RegionId
int RegionOrdinal
int AttachmentOrder
OptionalRegionAccessRule AccessRule
OptionalRewardTier RewardTier
OptionalReturnPolicy ReturnPolicy
int CriticalSourceSectorIndex
OptionalRegionDepth CriticalSourceDepth
int AttachmentEntrySectorIndex
int ReturnDestinationMandatorySectorIndex
IReadOnlyList<int> CriticalReturnPathSectorIndices
int CriticalReturnEdgeCount
int ReturnableCellCount
bool UsesSameOpenedAttachmentBoundary
bool RequiresReturnDevice
```

- `ReturnPolicy` exact `BacktrackToAttachment`.
- path는 critical source에서 attachment entry까지 canonical order이며 copied read-only다.
- `CriticalReturnEdgeCount = path.Count - 1`.
- `ReturnableCellCount`는 source region cell count와 같다.
- `UsesSameOpenedAttachmentBoundary=true`, `RequiresReturnDevice=false`.
- source attachment/access/reward/region/Type0 assignment를 mutate하지 않는다.

## Diagnostics / Result Contract

`OptionalReturnPolicyDiagnostics` sealed immutable fields:

```text
int SourceRegionCount
int SourceType0CellAssignmentCount
int SourceAccessAssignmentCount
int SourceRewardTierAssignmentCount
int AssignmentCount
int BacktrackCount
int ReturnGateCount
int SafeExitCount
int ReturnableCellCount
int NonReturnableCellCount
int InternalUndirectedBaseEdgeCount
int CriticalWitnessSectorCountTotal
int CriticalWitnessEdgeCountTotal
int MaximumCriticalWitnessSectorCount
int SameOpenedAttachmentReturnCount
int ReturnDeviceReservationCount
int ExtraSafeExitReservationCount
int AttachmentBoundaryBaseOpenCount
int RngDrawCount
int SourceMutationCount
```

`OptionalReturnPolicyResult` sealed immutable properties:

```text
OptionalReturnPolicyResolutionStatus Status
IReadOnlyList<OptionalReturnPolicyAssignment> Assignments
OptionalReturnPolicyDiagnostics Diagnostics
IReadOnlyList<OptionalReturnPolicyResolutionError> Errors
string SourceType0AssignmentDigest
string SourceAccessAssignmentDigest
string SourceRewardTierDigest
string SourceGrowthDigest
string CanonicalDigest
int RngDrawCount
bool IsSuccess
```

errors는 code, RegionId, sector index, attachment order, source field, message 순으로 stable sort/dedupe한다. 실패는 assignments empty, canonical digest empty, return device/safe exit/RNG/mutation `0/0/0/0`, partial publication `0`이다.

CanonicalDigest는 source digests, copied settings, assignments, paths, diagnostics를 stable UTF-8 ordinal field order로 SHA-256한 lowercase 64-hex다.

## `OptionalReturnPolicyResolver` Contract

stateless sealed service다.

```text
OptionalReturnPolicyResult Resolve(
    Type0RouteMaskAssignmentResult type0Assignments,
    OptionalAccessAssignmentResult accessAssignments,
    OptionalRewardTierResult rewardTiers,
    OptionalReturnPolicySettings settings)
```

- reference input null, invalid status/digest/accounting, source-chain mismatch를 거부한다.
- input collections를 copied canonical order로 처리한다.
- source snapshot/cell/mask/access/clue/reward tier/mandatory graph를 mutate하지 않는다.
- input caller order, culture, service reuse, thread/time에 독립적이다.
- hidden cache, static mutable collection, reflection, filesystem, Registry singleton, RNG는 0이다.
- path parent map/dictionary iteration order를 output order로 사용하지 않는다.

## Boundary Test Advance

허용할 MAP06_07 symbols:

```text
OptionalReturnPolicyResolutionStatus
OptionalReturnPolicyResolutionErrorCode
OptionalReturnPolicySettings
OptionalReturnPolicyAssignment
OptionalReturnPolicyDiagnostics
OptionalReturnPolicyResolutionError
OptionalReturnPolicyResult
OptionalReturnPolicyResolver
OptionalReturnPolicyResolverTests
```

계속 금지할 MAP06_08+ examples:

```text
InactiveBufferAssigner
OptionalRegionValidator
OptionalRegionOverlay
Map06ExitTests
GeneratedOptionalRegionCsvWriter
```

MAP05 Type4, MAP06_04 Type0 base-closed/L+R, MAP06_05 access/clue, MAP06_06 score/tier assertions를 약화하지 않는다. MAP06_08+ 금지 case 수를 줄이지 않는다.

## Required Tests

새 `OptionalReturnPolicyResolverTests`는 최소 `270` actual PASS cases를 가져야 한다.

- existing return enum/settings/resolution enum immutability `>=28`
- internal BaseEdge graph construction and reciprocal validation `>=40`
- all-cell returnability and canonical BFS `>=42`
- critical source selection and shortest witness paths `>=32`
- five access-rule reverse-use and base-closed preservation `>=32`
- Type0/access/reward/growth source-chain identity `>=28`
- atomic invalid source/topology/return-requirement failure `>=26`
- canonical digest/culture/order/service reuse determinism `>=22`
- source mutation/RNG/Type4/boundary advance `>=20`

Existing required gates:

```text
OptionalReturnPolicyResolverTests >=270 PASS
OptionalRewardTierCalculatorTests 279/279 PASS
OptionalAccessRuleAssignerTests 289/289 PASS
Type0RouteMaskAssignerTests 257/257 PASS
MAP06 prior combined selection 630/630 PASS
Existing MAP05 aggregate 1959/1959 PASS
Actually executed required total >=3684 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
Assets meta 3290 -> 3297
new C#/meta 7/7
existing boundary test C# modified <=12
Authoring CSV/meta 50/50
Authoring manifest SHA-256 unchanged
duplicate GUID groups 0
MAP05/MAP06_01~06 production/graph/mask/models/assignments/CSV/asmdef/Scene/Prefab/Packages/ProjectSettings modifications 0
generated CSV files created by this task 0
return gate/safe exit/device/socket/edge/recipe generated 0
```

Discovery-only count나 arithmetic total을 PASS로 기록하지 않는다. 위 total은 Unity EditMode Test Runner에서 실제 실행한 결과여야 한다.

## Result Format

Write exactly `MapDesign/MCP/REPORTS/MAP06_07_IMPLEMENT_RETURN_POLICY_RESULT.md`.

Required top lines:

```text
TASK: MAP06_07_IMPLEMENT_RETURN_POLICY
STATUS: PASS|FAIL|BLOCKED
MAP06_07: COMPLETE ELIGIBLE|NOT COMPLETE
MAP06_08_ASSIGN_INACTIVE_BUFFERS: LOCKED / DO NOT START
```

Result must include patch/receipt, prior Result and Task SHA gates, created/changed files, source-chain digests, settings, per-region critical source/path/policy/access/reward identity, all-cell returnability, reciprocal BaseEdges, witness totals/max, same-attachment reverse use, base-closed preservation, device/extra-exit zero, atomic failure and mutation evidence, MAP05/Type4 and MAP06_04~06 preservation, exact test jobs, Unity gate, asset/meta/CSV/GUID/change-scope gate, and NEXT that finalizes only MAP06_07 while keeping MAP06_08 locked.

## Done Conditions

- [ ] Preconditions, prior Result SHA, current Task SHA verified; MAP06_07 is sole CURRENT.
- [ ] Type0/access/reward/growth digest chain and one-to-one region identity verified.
- [ ] Every source cell has a reciprocal BaseEdge path to its attachment.
- [ ] Every region has one canonical critical backtrack witness and `BacktrackToAttachment` assignment.
- [ ] Same opened/discovered optional attachment is used in reverse while its base side remains closed.
- [ ] No synthetic return gate/safe exit/device/socket/edge/recipe/generated CSV.
- [ ] MAP06_07 symbols allowed; MAP06_08+ forbidden.
- [ ] No inactive-buffer/validator/overlay/exit behavior.
- [ ] Mandatory graph/masks, Type4, Type0/access/reward assignments and Authoring CSV unchanged.
- [ ] Required Unity EditMode gates actually executed and Result evidence complete.

If any required gate fails or Unity MCP is unavailable, write `STATUS: FAIL` or `STATUS: BLOCKED`, keep MAP06_07 CURRENT, and do not create or start MAP06_08.

