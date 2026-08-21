# MAP06_04 — Assign Type0 Route Masks

```yaml
status_control:
  task_key: MAP06_04_ASSIGN_TYPE0_ROUTE_MASKS
  result_file: REPORTS/MAP06_04_ASSIGN_TYPE0_ROUTE_MASKS_RESULT.md
```

## TASK TYPE

```text
RUNTIME IMMUTABLE P04 REGISTERED TYPE0 ROUTE-MASK CATALOG + CELL ASSIGNMENT + PHASE-BOUNDARY TEST ADVANCE + EDITMODE TESTS
```

## Objective

MAP06_03에서 승인된 optional-region topology snapshot의 모든 cell에 MAP01 typed `SectorRouteMaskDefinition`에서 검증한 registered Type0 mask ID를 deterministic하게 배정한다.

각 cell의 base required open sides는 같은 region의 cardinal neighbor만으로 정확히 계산한다. attachment→mandatory 면은 base route mask에서 닫고 MAP06_05 이후 `OptionalOverlayEdge`가 소유하도록 예약한다. 내부 required shape와 exact match인 active Type0 row만 사용하며 추가 side를 임의로 열거나 필요한 내부 side를 닫거나 bool 조합을 합성하지 않는다.

이번 Task는 immutable Type0 mask catalog와 per-cell assignment snapshot까지만 구현한다. 기존 `OptionalRegionCell`, MAP05 mandatory graph/mask, `SectorCell`, Authoring CSV, generated CSV를 수정하지 않는다. access/clue, reward tier algorithm, return path/device, inactive buffer, optional validator, overlay, generated edge/sector CSV writer는 구현하지 않는다.

MAP06_04 production symbol이 새로 생기므로 기존 phase-boundary negative assertions는 MAP06_04 symbols를 허용하고 MAP06_05+ future symbols만 금지하도록 필요한 기존 boundary test만 교정한다.

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
12. `REPORTS/MAP06_03_IMPLEMENT_OPTIONAL_REGION_GROWER_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP06_03_IMPLEMENT_OPTIONAL_REGION_GROWER
STATUS: PASS
MAP06_03: COMPLETE ELIGIBLE
MAP06_04_ASSIGN_TYPE0_ROUTE_MASKS: LOCKED / DO NOT START
SHA-256: 370a15f504d46492a591d064ee70dbc35d27b5b55ab4b621617aedae95d489b0
```

이 별도 patch가 적용된 뒤에만 MAP06_04를 실행한다. MAP06_05 이후 Task body는 읽거나 시작하지 않는다.

## Approved Input Baseline

```text
Optional regions/cells: 12/39
Growth settings: MaxRegions=12, MaxCellsPerRegion=6, TargetDepthPattern=1/2/3/4
Growth source/attempted/accepted/rejected/limit-skipped: 51/32/12/20/19
Growth depth buckets 1/2/3/4: 5/0/2/5
Growth canonical digest: 1f00f718bdb8d79fbf88923be0d81e0075987267c024cc624086ee6194998caa
Optional attachment digest: 68b438c523645c2f6721fa0c104c3cd4c282076292cd2e035cd20a2b272aaee6
Mandatory graph nodes/directed/undirected/route cells: 47/96/48/47
Mandatory masks T1/T2/T3/T4_UD/T4_LUD/T4_RUD/T4_LRUD: 20/4/4/17/0/0/2
Assets meta: 3266
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

Type4의 `LRUD` 허용은 Type0의 L/R 동시 개방 허용을 뜻하지 않는다. Type0에는 `LR` 또는 `LRUD` row가 없고 모든 assignment는 `!(L&&R)`를 만족해야 한다.

## Map Package Reference

exact installed Map Package v1.0이 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/01_WORLD_FIXED_SPEC.md
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
01_FIXED_SPEC/06_ROUTE_TOPOLOGY_CONSTRAINTS.md
01_FIXED_SPEC/07_OPTIONAL_EDGE_OVERLAY.md
02_PHASE_ROADMAP/MAP06_TYPE0_OPTIONAL_REGIONS.md
03_CSV_SCHEMA/CSV_RELATIONSHIPS.md
04_CSV_STARTER/sector_route_masks.csv
05_GENERATED_OUTPUT_SCHEMA/generated_world_sectors.csv
05_GENERATED_OUTPUT_SCHEMA/generated_world_edges.csv
```

reference는 exact Type0 ID/mask matrix와 later output boundary 확인용이다. installed Authoring CSV body를 production source로 다시 파싱하거나 수정하지 않는다. 실제 source of truth는 MAP01 typed `SectorRouteMaskDefinition` objects와 MAP06_03 immutable growth result다.

## READ ALLOWLIST

### Existing MAP01 typed route definitions

```text
Assets/_Game/Map/Runtime/WorldGeneration/Data/RouteDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/WorldRouteDefinitionSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/WorldRouteDefinitionBuilder.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/WorldRouteDefinitionBuildResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/StaticDataRegistry.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/StaticDataRegistrySnapshot.cs
```

존재하지 않는 파일명은 실제 MAP01_07/MAP01_12 typed API 이름에 맞춰 allowlisted equivalent를 읽어도 된다. Registry publish 의미를 바꾸거나 Authoring CSV body를 직접 파싱하지 않는다.

### Existing domain / P03~P04 production

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGridIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteOpenMask.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteMaskId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteMaskRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteMaskLookup.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilder.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraph.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteValidationReport.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionAttachment.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegion.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionSnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionGrowthSettings.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionGrowthDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionGrowthResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionGrower.cs
```

### Existing tests / assemblies

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/WorldRouteDefinitionBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/HorizontalBackboneRouterTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteGraphValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map05ExitTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/UpDownConflictResolverTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/VerticalGatewayPlannerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionModelsTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalAttachmentEnumeratorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionGrowerTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

위 파일과 matching meta, approved Runtime/Test `Generation`/`Data` path-only inventory, Authoring CSV/meta count·aggregate hash, 전체 Assets meta GUID, patch marker 이후 change-scope path만 확인할 수 있다.

금지: installed Authoring CSV body 재파싱·수정, generated CSV body source 사용, MAP06_05+ Task body, unrelated production/test body, Legacy/Stage/P6/P11 generator body, Scene/Prefab YAML.

## WRITE ALLOWLIST

### 신규 Runtime production C# — exact 7

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteOpenMask.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteMaskId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteMaskRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteMaskAssignment.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteMaskAssignmentDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteMaskAssignmentResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteMaskAssigner.cs
```

### 신규 Runtime EditMode test — exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Type0RouteMaskAssignerTests.cs
```

### 기존 phase-boundary test 수정 — exact up to 9

MAP06_04 production symbol 허용 및 MAP06_05+ future symbol 금지 유지를 위해 필요한 경우 아래 existing test C#만 수정할 수 있다. matching `.cs.meta`는 수정하지 않는다.

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
```

신규 C# 8개와 matching `.cs.meta` 8개, 위 boundary test C# 최대 9개 수정, Result 1개만 허용한다. MAP05/MAP06_01~03 production source, mandatory graph/mask, `OptionalRegionCell`, `SectorCell`, Authoring CSV/meta, generated CSV, asmdef, Scene, Prefab, Packages, ProjectSettings는 수정하지 않는다. 신규 directory/folder meta/asmdef/asmref를 만들지 않는다.

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
Input artifacts  = WORLD_ROUTE_DEFINITIONS.RouteMasks + OPTIONAL_REGION_TOPOLOGY_SNAPSHOT
Read-only guards = MAP05 MANDATORY GRAPH IDENTITY + MAP06_03 GROWTH RESULT
Output artifact  = TYPE0_ROUTE_MASK_ASSIGNMENT_SNAPSHOT
Pass ID          = PASS_OPTIONAL
Grid             = 13 x 13 / 169 sectors / lower-left origin
RNG consumption  = exact 0
```

typed input과 immutable snapshot만 사용한다. RNG, clock, filesystem, Registry singleton, Unity object order, unordered dictionary iteration을 사용하지 않는다.

## Exact Registered Type0 Matrix

아래 12개 row를 모두 active exact shape로 preflight한다. 순서는 아래 canonical order다.

| ID | L | R | U | D |
|---|---:|---:|---:|---:|
| `ROUTE_T0_NONE` | 0 | 0 | 0 | 0 |
| `ROUTE_T0_L` | 1 | 0 | 0 | 0 |
| `ROUTE_T0_R` | 0 | 1 | 0 | 0 |
| `ROUTE_T0_U` | 0 | 0 | 1 | 0 |
| `ROUTE_T0_D` | 0 | 0 | 0 | 1 |
| `ROUTE_T0_LU` | 1 | 0 | 1 | 0 |
| `ROUTE_T0_LD` | 1 | 0 | 0 | 1 |
| `ROUTE_T0_RU` | 0 | 1 | 1 | 0 |
| `ROUTE_T0_RD` | 0 | 1 | 0 | 1 |
| `ROUTE_T0_UD` | 0 | 0 | 1 | 1 |
| `ROUTE_T0_LUD` | 1 | 0 | 1 | 1 |
| `ROUTE_T0_RUD` | 0 | 1 | 1 | 1 |

모든 row는 `route_type=0`, `mandatory_allowed=false`, `active=true`여야 한다. active Type0 unexpected ID, duplicate ID/open mask, missing/inactive required ID, wrong route type/shape, `mandatory_allowed=true`, L/R simultaneous shape를 거부한다. Type1/2/3 등 non-Type0 rows는 count만 기록하고 catalog에 넣지 않는다.

`ROUTE_T0_LR`, `ROUTE_T0_LRUD` 또는 임의 합성 ID/shape는 금지다. `ROUTE_T0_NONE`은 same-region neighbor가 없는 single-cell region을 표현하는 실제 legal assignment다.

## `Type0RouteOpenMask` / ID / Record Contract

`Type0RouteOpenMask`는 `public readonly struct`, `IEquatable`, `IComparable`이며 `OpenLeft/OpenRight/OpenUp/OpenDown`, `OpenCount`, `HasHorizontalThrough`를 제공한다. equality/order/hash는 fixed L/R/U/D bits와 deterministic integer hash를 사용한다. `UD/LUD/RUD`는 legal이고 `L+R`은 invalid다.

`Type0RouteMaskId`는 `public readonly struct`, `IEquatable`, `IComparable`이며 `Value`, `IsValid`, constructor, `TryCreate`를 제공한다. grammar exact `^ROUTE_T0_[A-Z0-9_]+$`; default invalid; ordinal case-sensitive다.

`Type0RouteMaskRecord` sealed immutable properties:

```text
Type0RouteMaskId MaskId
int RouteType
Type0RouteOpenMask OpenMask
bool MandatoryAllowed
bool Active
string DescriptionKo
SectorRouteMaskDefinition SourceDefinition
```

실제 typed class name이 다르면 MAP01 checked-in equivalent를 사용한다. SourceDefinition exact reference를 보존하고 source row를 clone/mutate하지 않는다.

## Per-Cell Required Shape Algorithm

각 region을 `RegionId`, 각 cell을 `SectorIndex` canonical order로 처리한다.

1. 같은 region에 cardinal neighbor가 있으면 그 방향만 base required open으로 설정한다.
2. `IsAttachmentCell=true`인 entry의 `entry -> mandatory` 면은 base required open에 넣지 않고 exact closed인지 검증한다.
3. 다른 region adjacent cell, unowned cell, world 밖, site/mandatory cell의 모든 boundary side는 열지 않는다.
4. resulting internal shape가 L+R simultaneous거나 exact matching registered mask가 없으면 `UnsupportedTopology`로 atomic reject한다.
5. 모든 cell이 성공한 뒤에만 immutable assignment snapshot을 publish한다.

mask에 extra open side를 허용하지 않는다. 따라서 same-region adjacency는 양쪽 assignment에서 reciprocal BaseEdge이고, cross-region 및 mandatory attachment boundary는 base mask상 closed다. attachment boundary의 access kind와 `OptionalOverlayEdge` 생성은 MAP06_05+ 책임이며 이번 Task는 edge를 만들지 않는다.

## Assignment / Diagnostics / Result Contract

`Type0RouteMaskAssignment` sealed immutable properties:

```text
OptionalRegionId RegionId
int SectorIndex
SectorCoord Sector
OptionalRegionDepth Depth
bool IsAttachmentCell
Type0RouteMaskRecord Mask
Type0RouteMaskId MaskId
Type0RouteOpenMask OpenMask
```

source cell identity/depth/attachment flag를 보존하고 assignment order는 RegionId ordinal, SectorIndex ascending이다. source `OptionalRegionCell`을 mutate하거나 route mask property를 추가하지 않는다.

`Type0RouteMaskAssignmentDiagnostics` sealed immutable fields:

```text
int SourceRouteMaskDefinitionCount
int RegisteredType0MaskCount
int IgnoredNonType0DefinitionCount
int SourceRegionCount
int SourceCellCount
int AssignmentCount
int InternalUndirectedEdgeCount
int AttachmentBoundaryClosedCount
int MandatoryBoundaryBaseOpenCount
int ClosedCrossRegionAdjacencyCount
int HorizontalThroughCount
int UnsupportedRequiredMaskCount
int RngDrawCount
int SourceMutationCount
```

successful approved fixture는 registered `12`, regions/cells/assignments `12/39/39`, attachment boundary closed `12`, mandatory boundary base opens `0`, horizontal through/unsupported/RNG/mutation `0/0/0/0`이다. internal edge와 cross-region closed counts 및 per-mask usage는 actual topology에서 계산해 Result에 기록하며 하드코딩하지 않는다.

`Type0RouteMaskAssignmentResult` sealed immutable properties:

```text
Type0RouteMaskAssignmentStatus Status
OptionalRegionSnapshot SourceSnapshot
IReadOnlyList<Type0RouteMaskRecord> RegisteredMasks
IReadOnlyList<Type0RouteMaskAssignment> Assignments
Type0RouteMaskAssignmentDiagnostics Diagnostics
IReadOnlyList<Type0RouteMaskAssignmentError> Errors
string SourceGrowthDigest
string SourceRouteMaskCatalogDigest
string CanonicalDigest
int RngDrawCount
bool IsSuccess
```

status exact `Completed | InvalidInput | InvalidCatalog | UnsupportedTopology`. error는 code, RegionId, SectorIndex, mask ID, message의 stable fields를 가지며 stable sort/dedupe한다. 실패는 assignments empty, canonical output digest empty, RNG/mutation 0, partial publication 0이다.

SourceRouteMaskCatalogDigest는 exact 12 records의 ID/type/bits/mandatory/active를 canonical order UTF-8 SHA-256한다. CanonicalDigest는 source growth digest, catalog digest, assignments, diagnostics를 stable field order로 SHA-256한 lowercase 64-hex다.

## `Type0RouteMaskAssigner` Contract

stateless sealed service다.

```text
Type0RouteMaskAssignmentResult Assign(OptionalRegionGrowthResult growth, WorldRouteDefinitionSet definitionSet)
Type0RouteMaskAssignmentResult Assign(OptionalRegionGrowthResult growth, IEnumerable<SectorRouteMaskDefinition> routeMasks)
```

checked-in public typed API shape가 다르면 exact equivalent를 사용한다. null input, invalid/empty source digest, invalid growth accounting/snapshot identity를 거부한다. source graph identity `47/96/47`, source attachment/growth digest identity를 보존한다. approved fixture의 exact region/cell/digest는 fixture test/Result에서 검증하고 production 일반 입력 수량으로 하드코딩하지 않는다.

input collections를 copied deterministic order로 다루며 source definition/growth/snapshot/region/cell/mandatory graph mutation은 0이다. hidden cache, static mutable collection, reflection, filesystem, RNG는 0이다.

## Boundary Test Advance

허용할 MAP06_04 symbols: `Type0RouteOpenMask`, `Type0RouteMaskId`, `Type0RouteMaskRecord`, `Type0RouteMaskAssignment`, `Type0RouteMaskAssignmentDiagnostics`, `Type0RouteMaskAssignmentResult`, `Type0RouteMaskAssigner`, `Type0RouteMaskAssignerTests`.

계속 금지할 MAP06_05+ examples: `OptionalAccessRuleAssigner`, `OptionalClueAssigner`, `OptionalRewardTierCalculator`, `OptionalReturnPolicyResolver`, `InactiveBufferAssigner`, `OptionalRegionValidator`, `OptionalRegionOverlay`, `GeneratedOptionalRegionCsvWriter`.

MAP05 Type4 negative/assertion logic을 약화하거나 `LRUD`를 금지하지 않는다. MAP06_05+ 금지 case 수를 줄여 test count를 우회하지 않는다.

## Required Tests

새 `Type0RouteMaskAssignerTests`는 최소 `220` actual PASS cases를 가져야 한다.

- open-mask/ID/record equality, order, hash, immutability `>=32`
- exact 12-row catalog matrix/order/source identity `>=32`
- missing/duplicate/inactive/unexpected/wrong-shape/mandatory-allowed rejection `>=34`
- per-cell internal required-side calculation and exact registered lookup `>=36`
- reciprocal internal BaseEdge, attachment/mandatory boundary base-closed, cross-region closed sides `>=28`
- L/R through and unsupported topology atomic rejection `>=20`
- diagnostics/digests/culture/order/service reuse `>=18`
- source mutation, RNG 0, Type4 preservation, boundary advance `>=20`

Existing required gates:

```text
Type0RouteMaskAssignerTests >=220 PASS
OptionalRegionGrowerTests 234/234 PASS
OptionalAttachmentEnumeratorTests 202/202 PASS
OptionalRegionModelsTests 194/194 PASS
Existing MAP05 aggregate 1959/1959 PASS
Actually executed required total >=2809 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
Assets meta 3266 -> 3274
new C#/meta 8/8
existing boundary test C# modified <=9
Authoring CSV/meta 50/50
Authoring manifest SHA-256 unchanged
duplicate GUID groups 0
MAP05/MAP06_01~03 production/graph/mask/OptionalRegionCell/SectorCell/CSV/asmdef/Scene/Prefab/Packages/ProjectSettings modifications 0
generated CSV files created by this task 0
```

Discovery-only count나 arithmetic total을 PASS로 기록하지 않는다. 위 total은 Unity EditMode Test Runner에서 실제 실행한 결과여야 한다.

## Result Format

Write exactly `MapDesign/MCP/REPORTS/MAP06_04_ASSIGN_TYPE0_ROUTE_MASKS_RESULT.md`.

Required top lines:

```text
TASK: MAP06_04_ASSIGN_TYPE0_ROUTE_MASKS
STATUS: PASS|FAIL|BLOCKED
MAP06_04: COMPLETE ELIGIBLE|NOT COMPLETE
MAP06_05_ASSIGN_ACCESS_RULES_AND_CLUES: LOCKED / DO NOT START
```

Result must include patch/receipt, prior Result and Task SHA gates, created/changed files, exact 12-row matrix/catalog digest, source growth regions/cells/digest, assignments and per-mask usage, internal reciprocal BaseEdge, attachment boundary closed, mandatory boundary base-open 0, cross-region closed evidence, through/unsupported/partial counts, canonical digest, mutation evidence, MAP05 identity/mask counts/Type4 preservation, exact test jobs, Unity gate, asset/meta/CSV/GUID/change-scope gate, and NEXT that finalizes only MAP06_04 while keeping MAP06_05 locked.

## Done Conditions

- [ ] Preconditions, prior Result SHA, current Task SHA verified; MAP06_04 is sole CURRENT.
- [ ] Exact 12 active registered Type0 rows validated from typed definitions without Authoring CSV modification.
- [ ] Every source optional cell has one exact registered mask.
- [ ] Internal BaseEdge opens reciprocal; every attachment/mandatory and cross-region boundary is base-closed; every Type0 `!(L&&R)`.
- [ ] MAP06_04 symbols allowed; MAP06_05+ forbidden.
- [ ] No access/clue/reward/return/inactive/validator/overlay/generated CSV behavior.
- [ ] Mandatory graph/masks and Type4 rule unchanged.
- [ ] Required Unity EditMode gates actually executed and Result evidence complete.

If any required gate fails or Unity MCP is unavailable, write `STATUS: FAIL` or `STATUS: BLOCKED`, keep MAP06_04 CURRENT, and do not create or start MAP06_05.

