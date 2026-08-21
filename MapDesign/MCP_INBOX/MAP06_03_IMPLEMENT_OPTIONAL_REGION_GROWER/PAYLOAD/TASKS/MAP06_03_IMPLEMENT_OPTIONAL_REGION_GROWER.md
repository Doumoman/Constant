# MAP06_03 — Implement Optional Region Grower

```yaml
status_control:
  task_key: MAP06_03_IMPLEMENT_OPTIONAL_REGION_GROWER
  result_file: REPORTS/MAP06_03_IMPLEMENT_OPTIONAL_REGION_GROWER_RESULT.md
```

## TASK TYPE

```text
RUNTIME IMMUTABLE P04 OPTIONAL-REGION TOPOLOGY GROWTH + PHASE-BOUNDARY TEST ADVANCE + EDITMODE TESTS
```

## Objective

MAP06_02에서 승인된 optional attachment candidate 목록을 입력으로 사용해, 각 accepted attachment에서 깊이 `1..4`의 connected optional region 군집을 deterministic하게 성장시킨다.

각 region은 MAP05 mandatory graph와 정확히 하나의 attachment bridge만 공유해야 한다. optional cell은 mandatory route cell, site reservation, biome reserved/inactive cell과 겹치면 안 되고 region끼리 sector를 중복 소유하면 안 된다. entry cell의 depth는 exact `1`이며 모든 cell depth는 entry에서 region 내부 cardinal shortest distance `+1`이어야 한다. 또한 어느 region cell도 같은 region 안에서 left와 right neighbor를 동시에 가져서는 안 된다.

이번 Task는 topology growth와 immutable snapshot publication까지만 구현한다. Type0 route mask ID/edge, access/clue, reward tier algorithm, return path/device, inactive buffer, optional validator, overlay, generated CSV writer는 구현하지 않는다.

MAP06_03 production symbol이 새로 생기므로 기존 phase-boundary negative assertions는 MAP06_03 symbols를 허용하고 MAP06_04+ future symbols만 금지하도록 필요한 기존 boundary test만 교정한다.

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
12. `REPORTS/MAP06_02_ENUMERATE_OPTIONAL_ATTACHMENTS_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP06_02_ENUMERATE_OPTIONAL_ATTACHMENTS
STATUS: PASS
MAP06_02: COMPLETE ELIGIBLE
MAP06_03_IMPLEMENT_OPTIONAL_REGION_GROWER: LOCKED / DO NOT START
SHA-256: 69b6dbc5b379de297805ba8d9b3523779e26486a9244b3f2306523e70c9c123c
```

이 별도 patch가 적용된 뒤에만 MAP06_03을 실행한다. MAP06_04 이후 Task body는 읽거나 시작하지 않는다.

## Approved Input Baseline

```text
Unity: 6000.3.8f1
Mandatory graph nodes/directed/undirected/route cells: 47/96/48/47
Mandatory masks T1/T2/T3/T4_UD/T4_LUD/T4_RUD/T4_LRUD: 20/4/4/17/0/0/2
Optional attachment raw probes/accepted: 188/51
Optional attachment canonical digest: 68b438c523645c2f6721fa0c104c3cd4c282076292cd2e035cd20a2b272aaee6
Assets meta: 3261
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

Type4의 `LRUD` 허용은 Type0의 L/R 동시 관통 허용을 뜻하지 않는다. MAP06_03은 route mask를 배정하지 않으며 MAP06_04가 적용할 Type0 `!(L&&R)` 계약을 방해하는 topology를 만들지 않는다.

## Map Package Reference

exact installed Map Package v1.0이 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/01_WORLD_FIXED_SPEC.md
01_FIXED_SPEC/02_COORDINATE_AND_ID_RULES.md
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
01_FIXED_SPEC/06_ROUTE_TOPOLOGY_CONSTRAINTS.md
02_PHASE_ROADMAP/MAP06_TYPE0_OPTIONAL_REGION.md
03_CSV_SCHEMA/CSV_RELATIONSHIPS.md
04_CSV_STARTER/sector_route_masks.csv
05_GENERATED_OUTPUT_SCHEMA/generated_world_sectors.csv
05_GENERATED_OUTPUT_SCHEMA/generated_world_edges.csv
```

reference는 Type0/non-through/depth 용어와 이후 output boundary 확인용이다. installed Authoring CSV body를 다시 파싱하거나 수정하지 않는다. 이번 Task의 source of truth는 approved typed MAP05 graph, MAP06_01 models, MAP06_02 enumeration result다.

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
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAttachmentCandidateId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAttachmentCandidate.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAttachmentEnumerationSettings.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAttachmentEnumerationDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAttachmentEnumerationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAttachmentEnumerator.cs
```

MAP06_03 신규 runtime file은 Result에서 확정된 exact directory인 아래에만 만든다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/
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
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

위 파일과 matching meta, approved Runtime/Test `Generation` path-only inventory, Authoring CSV/meta count·aggregate hash, 전체 Assets meta GUID, patch marker 이후 change-scope path만 확인할 수 있다.

금지:

- installed Authoring CSV body 재파싱·수정
- generated CSV body를 disk에서 읽어 source of truth로 사용
- MAP06_04 이후 Task body
- unrelated production/test C# body
- Legacy/Stage/P6/P11 generator body
- Scene/Prefab YAML

## WRITE ALLOWLIST

### 신규 Runtime production C# — exact 4

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionGrowthSettings.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionGrowthDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionGrowthResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionGrower.cs
```

### 신규 Runtime EditMode test — exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionGrowerTests.cs
```

### 기존 phase-boundary test 수정 — exact up to 8

MAP06_03 production symbol 허용 및 MAP06_04+ future symbol 금지 유지를 위해 필요한 경우 아래 existing test C#만 수정할 수 있다. matching `.cs.meta`는 수정하지 않는다.

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/HorizontalBackboneRouterTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteGraphValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map05ExitTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/UpDownConflictResolverTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/VerticalGatewayPlannerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionModelsTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalAttachmentEnumeratorTests.cs
```

신규 C# 5개와 matching `.cs.meta` 5개, 위 boundary test C# 최대 8개 수정, Result 1개만 허용한다. MAP05/MAP06_01/MAP06_02 production source, graph, CSV, SectorCell, Authoring CSV/meta, asmdef, Scene, Prefab, Packages, ProjectSettings는 수정하지 않는다. 신규 directory/folder meta/asmdef/asmref를 만들지 않는다.

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
Input artifact   = OPTIONAL_ATTACHMENT_CANDIDATES
Read-only guards = MANDATORY_ROUTE_GRAPH + VALIDATION_REPORT + GENERATED_WORLD_DATA + SITE_RESERVATIONS + BIOME_PATCH_PUBLICATION
Output artifact  = OPTIONAL_REGION_TOPOLOGY_SNAPSHOT
Pass ID          = PASS_OPTIONAL
Grid             = 13 x 13 / 169 sectors / lower-left origin
Depth            = exact 1..4 from entry; entry depth 1
Mandatory bridge = exactly one per accepted region
RNG consumption  = exact 0
```

이번 Task는 input과 explicit settings만으로 결과를 계산한다. RNG, clock, filesystem, Registry, Unity object order, unordered dictionary iteration을 사용하지 않는다.

## `OptionalRegionGrowthSettings` Contract

sealed immutable object다. world tuning을 숨은 default로 하드코딩하지 않고 constructor 입력을 명시적으로 요구한다.

```text
int MaxRegions
int MaxCellsPerRegion
IReadOnlyList<OptionalRegionDepth> TargetDepthPattern
```

- `MaxRegions`는 `1..9999`다.
- `MaxCellsPerRegion`은 `1..16`이다.
- `TargetDepthPattern`은 non-null, non-empty copied read-only list이며 각 값은 `1..4`다.
- `MaxCellsPerRegion`은 pattern의 최대 depth 이상이어야 한다.
- candidate의 target depth는 `candidate.AttachmentOrder % TargetDepthPattern.Count`로 선택한다.
- caller list mutation, culture, enumeration order가 settings의 결과를 바꾸면 안 된다.
- static mutable/default settings instance를 만들지 않는다.

## Growth Algorithm Contract

1. input candidate는 `OptionalAttachmentEnumerationResult.Candidates` canonical order를 그대로 검증한다.
2. CandidateId/AttachmentOrder는 contiguous `0..Count-1`이며 source candidate digest가 Result gate와 일치해야 한다.
3. candidate entry를 region depth `1`의 attachment cell로 사용한다.
4. target depth까지 cardinal simple path를 찾는다. frontier key는 `depth`, parent sector index, direction `L,R,U,D`, child sector index ascending이다.
5. target path가 없으면 그 candidate를 reject하고 다음 candidate를 시도한다. rejected candidate는 RegionId를 소비하지 않는다.
6. target path 확보 후 `MaxCellsPerRegion`까지 같은 canonical frontier order로 connected cell을 채울 수 있다. 모든 stored depth는 region 내부 entry 기준 cardinal shortest distance `+1`로 다시 계산한다.
7. accepted region이 `MaxRegions`에 도달하면 남은 candidate는 `RegionLimitSkipped`로 기록한다.

모든 growth probe에서 아래를 강제한다.

- grid 밖 sector 거부
- MAP05 mandatory route sector 거부
- site footprint/reservation sector 거부
- biome reserved/inactive sector 거부
- 앞서 accepted된 optional region이 소유한 sector 거부
- 같은 region frontier duplicate 거부
- entry 외 optional cell이 mandatory route cell과 cardinal adjacency를 가지면 거부
- entry가 source mandatory sector 이외의 mandatory sector와 cardinal adjacency를 가지면 해당 candidate 거부
- accepted region의 mandatory↔optional cardinal edge는 exact `1`, 즉 source mandatory sector↔entry sector뿐
- 새 cell을 추가했을 때 자신 또는 기존 region cell에 same-region left+right neighbor가 동시에 생기면 거부

region끼리 sector overlap은 금지한다. 최종 cell set의 모든 cell은 `!(hasLeft && hasRight)`를 만족해야 하며, 이는 MAP06_04의 Type0 mask assignment가 연결 topology를 실현할 수 있게 하는 선행 조건이다. 인접하지만 서로 다른 optional region 사이의 실제 socket open/closed 결정은 MAP06_04 책임이므로 이번 Task에서 edge나 mask를 만들지 않는다.

## Region Publication Contract

accepted region order에 따라 RegionId를 contiguous하게 부여한다.

```text
OPT_REGION_0000
OPT_REGION_0001
...
```

- RegionId는 existing `OptionalRegionId` grammar를 그대로 사용한다.
- attachment는 source candidate의 mandatory node/sector, entry sector, direction, initial depth `1`을 보존한다.
- `AttachmentOrder`는 source candidate AttachmentOrder를 보존한다.
- exactly one cell만 `IsAttachmentCell=true`이며 entry sector와 일치한다.
- 이번 단계의 staging values는 `AccessRule=Basic`, `RewardTier=None`, `ReturnPolicy=BacktrackToAttachment`다.
- staging values는 MAP06_05/06/07의 최종 assignment가 아니며 clue, reward spawn, return path/device를 만들지 않는다.
- 모든 cell의 `RequiresReturnConnection=false`다. 실제 return connection은 MAP06_07 책임이다.
- `OptionalRegionSnapshot`은 source mandatory identity `47/96/47`과 non-empty caller-supplied graph digest를 보존한다.
- source graph/world/site/biome/enumeration result와 existing model objects를 mutate하지 않는다.

## `OptionalRegionGrowthDiagnostics` Contract

sealed immutable object다.

```text
int SourceCandidateCount
int AttemptedCandidates
int AcceptedRegionCount
int RejectedCandidateCount
int RegionLimitSkipped
int AcceptedCellCount
int RawCellProbes
int OutOfBoundsCellRejected
int MandatoryCellRejected
int AdditionalMandatoryBridgeRejected
int SiteReservationCellRejected
int BiomeReservedCellRejected
int ClaimedCellRejected
int DuplicateFrontierRejected
int HorizontalThroughCellRejected
int NoTargetDepthPathRejected
int Depth1RegionCount
int Depth2RegionCount
int Depth3RegionCount
int Depth4RegionCount
IReadOnlyList<string> RejectionCodes
```

- source candidate accounting은 `SourceCandidateCount = AttemptedCandidates + RegionLimitSkipped`다.
- attempted accounting은 `AttemptedCandidates = AcceptedRegionCount + RejectedCandidateCount`다.
- depth bucket 합은 AcceptedRegionCount와 같다.
- probe counters는 candidate accounting과 별도이며 canonical traversal에서 실제 발생한 횟수다.
- `RejectionCodes`는 rejected candidate canonical order의 stable copied read-only list다.

## `OptionalRegionGrowthResult` Contract

sealed immutable object다.

```text
OptionalRegionSnapshot Snapshot
OptionalRegionGrowthDiagnostics Diagnostics
string SourceAttachmentDigest
string SourceMandatoryGraphDigest
string CanonicalDigest
int RngDrawCount
```

- SourceAttachmentDigest는 input `OptionalAttachmentEnumerationResult.CanonicalDigest`를 그대로 보존한다. approved fixture에서는 exact `68b438c523645c2f6721fa0c104c3cd4c282076292cd2e035cd20a2b272aaee6`여야 하지만 production service에 특정 candidate count/digest를 하드코딩하지 않는다.
- SourceMandatoryGraphDigest는 caller-supplied non-empty canonical identity를 그대로 보존한다.
- RngDrawCount는 exact `0`이다.
- CanonicalDigest는 source digests, settings values, accepted region/attachment/cell records, diagnostics를 stable UTF-8 ordinal field order로 SHA-256한 lowercase 64-hex다.
- caller order shuffle, culture `en-US`/`tr-TR`, service reuse, repeated call이 digest를 바꾸면 안 된다.

## `OptionalRegionGrower` Contract

stateless sealed service다.

```text
OptionalRegionGrowthResult Grow(
    GeneratedWorldData world,
    MandatoryRouteGraph graph,
    MandatoryRouteValidationReport validationReport,
    SiteReservationSnapshot siteReservations,
    BiomePatchValidationPublication biomePublication,
    OptionalAttachmentEnumerationResult attachments,
    string sourceMandatoryGraphDigest,
    OptionalRegionGrowthSettings settings)
```

- 모든 reference input null과 invalid/empty source digest를 거부한다.
- validation report가 approved valid 상태인지 검사한다.
- attachments의 graph counts가 input graph `47/96/47` identity와 일치하고 CandidateId/order/dedup/digest가 자체 result 계약상 유효한지 검증한다.
- accepted `51`과 baseline candidate digest는 approved fixture test/result gate에서 검증하며 production service의 일반 입력 제한으로 하드코딩하지 않는다.
- input collection을 copied deterministic order로 다룬다.
- no hidden cache, no static mutable collection, no reflection, no filesystem, no RNG.

## Boundary Test Advance

반드시 허용해야 하는 MAP06_03 symbols:

```text
OptionalRegionGrowthSettings
OptionalRegionGrowthDiagnostics
OptionalRegionGrowthResult
OptionalRegionGrower
OptionalRegionGrowerTests
```

계속 금지해야 하는 MAP06_04+ future symbols examples:

```text
Type0RouteMaskAssigner
OptionalAccessRuleAssigner
OptionalClueAssigner
OptionalRewardTierCalculator
OptionalReturnPolicyResolver
InactiveBufferAssigner
OptionalRegionValidator
OptionalRegionOverlay
GeneratedOptionalRegionCsvWriter
```

MAP05 Type4 negative/assertion logic을 약화하거나 `LRUD`를 금지하지 않는다. MAP06_04+ 금지 case 수를 줄여 test count를 우회하지 않는다.

## Required Tests

새 `OptionalRegionGrowerTests`는 최소 `200` actual PASS cases를 가져야 한다.

필수 test groups:

- settings validation/copy/immutability/pattern mapping `>=20`
- RegionId contiguous assignment and source attachment preservation `>=20`
- depth `1..4`, target path, shortest-distance labeling, connectedness `>=36`
- exact single mandatory bridge, no same-region L+R neighbors, non-through rejection `>=30`
- boundary/reservation/biome/claimed/duplicate frontier filters `>=30`
- region overlap/limit/rejection accounting `>=18`
- diagnostics/canonical digest/culture/order/service reuse `>=18`
- source mutation guard and RNG draw exact `0` `>=10`
- Type4 U+D mandatory, L/R independent, UD/LUD/RUD/LRUD legal `>=8`
- boundary negative assertions advanced to MAP06_04+ `>=10`

Existing required gates:

```text
OptionalRegionGrowerTests >=200 PASS
OptionalAttachmentEnumeratorTests 202/202 PASS
OptionalRegionModelsTests 194/194 PASS
Existing MAP05 aggregate 1959/1959 PASS
Actually executed required total >=2555 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
Assets meta 3261 -> 3266
new C#/meta 5/5
existing boundary test C# modified <=8
Authoring CSV/meta 50/50
Authoring manifest SHA-256 unchanged
duplicate GUID groups 0
MAP05/MAP06_01/MAP06_02 production/graph/CSV/SectorCell/asmdef/Scene/Prefab/Packages/ProjectSettings modifications 0
generated CSV files created by this task 0
```

Discovery-only count나 arithmetic total을 PASS로 기록하지 않는다. 위 total은 Unity EditMode Test Runner에서 실제 실행한 결과여야 한다.

## Result Format

Write exactly one Result file:

```text
MapDesign/MCP/REPORTS/MAP06_03_IMPLEMENT_OPTIONAL_REGION_GROWER_RESULT.md
```

Required top lines:

```text
TASK: MAP06_03_IMPLEMENT_OPTIONAL_REGION_GROWER
STATUS: PASS|FAIL|BLOCKED
MAP06_03: COMPLETE ELIGIBLE|NOT COMPLETE
MAP06_04_ASSIGN_TYPE0_ROUTE_MASKS: LOCKED / DO NOT START
```

The Result must include:

- patch id/version and `.APPLIED` receipt status
- prior Result exact status and SHA check
- current Task file SHA check
- created/changed file list
- source candidate count/digest
- growth settings used by the approved fixture
- accepted/rejected/limit-skipped region counts, total cells, depth bucket, horizontal-through/probe rejection counters, canonical digest
- exact-one mandatory bridge, connectedness, overlap and mutation evidence
- MAP05 graph identity `47/96/48/47`, mask counts `20/4/4/17/0/0/2`
- Type4 preservation: U+D mandatory, L/R independent, UD/LUD/RUD/LRUD legal
- test jobs and exact pass/fail/skipped counts
- Unity compile/Console/warning gate
- asset/meta/CSV/GUID/change-scope gate
- clear NEXT: finalize MAP06_03 only, keep MAP06_04 LOCKED, do not auto-start next task

## Done Conditions

- [ ] Preconditions, prior Result SHA, current Task SHA verified.
- [ ] MAP06_03 is the only CURRENT task.
- [ ] Optional regions grow only from approved MAP06_02 candidates.
- [ ] Every accepted region is connected, depth `1..4`, overlap-free, has no same-region L+R neighbor pair and has exactly one mandatory bridge.
- [ ] MAP06_03 symbols are allowed; MAP06_04+ symbols remain forbidden.
- [ ] No Type0 mask/access/clue/reward/return/inactive/validator/overlay/generated CSV behavior implemented.
- [ ] Mandatory graph and Type4 rule remain unchanged.
- [ ] Required Unity EditMode gates actually executed and passed.
- [ ] Result file written with complete evidence.

If any required gate fails or Unity MCP is unavailable, write `STATUS: FAIL` or `STATUS: BLOCKED`, keep MAP06_03 CURRENT, and do not create or start MAP06_04.
