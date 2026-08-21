# MAP06_08 — Assign Inactive Buffers

```yaml
status_control:
  task_key: MAP06_08_ASSIGN_INACTIVE_BUFFERS
  result_file: REPORTS/MAP06_08_ASSIGN_INACTIVE_BUFFERS_RESULT.md
```

## TASK TYPE

```text
RUNTIME IMMUTABLE P04 EXCLUSIVE-SECTOR ACCOUNTING + INACTIVE-BUFFER ASSIGNMENT + DECORATIVE-BOUNDARY CLASSIFICATION + SOURCE-CHAIN VALIDATION + PHASE-BOUNDARY TEST ADVANCE + EDITMODE TESTS
```

## Objective

MAP06_07 PASS/finalize 뒤 approved 13×13 world, P01 site reservations, P02 biome publication, MAP05 mandatory graph, MAP06_04 Type0 assignments, MAP06_07 return-policy result를 immutable source-chain으로 결합한다. P01 site footprint, MAP05 mandatory graph, Type0 cells의 source membership을 먼저 검증한 뒤, approved reserved-adapter overlap을 반영한 exclusive projected ownership을 확정한다. 어느 projected protected owner에도 속하지 않은 모든 sector를 explicit `SectorRole.InactiveBuffer`로 원자적으로 발행한다.

inactive sector가 protected sector와 cardinally 맞닿아 있으면 `DecorativeBoundary`, 그렇지 않으면 `InteriorInactive` 하위 분류를 기록한다. 이 분류는 MAP06 roadmap의 “InactiveBuffer 또는 장식용 경계”를 표현하는 logical content hint다. 새 `SectorRole`, boundary profile ID, sector recipe ID, microchunk, tile, socket, edge 또는 generated CSV를 만들지 않는다. 두 분류 모두 최종 sector role projection은 기존 `InactiveBuffer`다.

approved fixture의 source counts는 `8 ReservedSite source sectors`, `47 Mandatory graph route cells`, `39 Type0 cells`다. 단, sectors `0`, `28`, `106`은 approved reserved adapters라서 site footprint와 mandatory graph source membership에 동시에 존재한다. exclusive projected accounting은 `169 = 8 ReservedSite + 44 MandatoryOnly + 39 Type0 + 78 InactiveBuffer`이며 protected union은 `91`이다. decorative/interior exact split은 source topology에서 독립 oracle로 계산해 test와 Result에 기록하되 production service에 특정 split이나 canonical digest를 하드코딩하지 않는다.

MAP06_09 validator와 MAP06_10 overlay/exit는 구현하지 않는다. MAP06_08 production symbols가 새로 생겼으므로 기존 phase-boundary negative assertions는 MAP06_08 symbols를 허용하고 MAP06_09+ future symbols만 금지하도록 필요한 boundary test만 교정한다.

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
13. `REPORTS/MAP06_07_IMPLEMENT_RETURN_POLICY_RESULT.md`

Repair Result exact gate:

```text
TASK: MAP06_08_ASSIGN_INACTIVE_BUFFERS
STATUS: BLOCKED
MAP06_08: NOT COMPLETE
MAP06_09_IMPLEMENT_OPTIONAL_REGION_VALIDATOR: LOCKED / DO NOT START
SHA-256: 759de495f3e2608fba844e5cca5ab3c6d7cd0479a73c8a3928c1ac4b964045fa
Blocked current Task SHA-256 before repair: 778d5beb1944ddd01e4541254f6d63d55ce255c3eaeab0f79143ee4de2de9ec7
```

Prior source-chain Result exact gate:

```text
TASK: MAP06_07_IMPLEMENT_RETURN_POLICY
STATUS: PASS
MAP06_07: COMPLETE ELIGIBLE
MAP06_08_ASSIGN_INACTIVE_BUFFERS: LOCKED / DO NOT START
SHA-256: 2815e6b35df71be1477812594435ed4793c3c9a03c60f1ef602267e4a2e12329
```

이 repair patch가 적용된 뒤에만 MAP06_08을 재실행한다. MAP06_09 이후 Task body는 읽거나 시작하지 않는다.

## Approved Input Baseline

```text
World sectors / dimensions: 169 / 13x13
Site reservations / reserved sectors / entries / Core seeds: 7 / 8 / 6 / 4
Biome publication sectors / assigned / reserved-unassigned: 169 / 165 / 4
Mandatory graph nodes / directed / undirected / route cells: 47 / 96 / 48 / 47
Optional regions / Type0 cells: 12 / 39
Return assignments / returnable / non-returnable: 12 / 39 / 0
Source protected counts ReservedSite/Mandatory/Type0: 8 / 47 / 39
Approved source overlap Site ∩ Mandatory: 3 at sectors 0,28,106
Approved source overlap Site ∩ Type0 / Mandatory ∩ Type0: 0 / 0
Exclusive projected protected ReservedSite/MandatoryOnly/Type0: 8 / 44 / 39
Protected union: 91
Expected remaining InactiveBuffer assignments: 78
Type0 assignment digest: a26e73f25ff7267760a2507ec55b1acda3a6c39a8f3698fc37b190620ceca525
Growth digest: 1f00f718bdb8d79fbf88923be0d81e0075987267c024cc624086ee6194998caa
Access digest: 5268b7ed2342f197fb5717c85dbfdb8e51b3c520c217ecf93ad14dc944ecf74f
Reward digest: c3430c42a27937e143fa89c5839282b9533b62d5fb74fb26fdad490cb545958e
Return-policy digest: cff0556a59e66fcc16b886ecf3082779efe9535bb79dcf45b401d12ff0971f6b
Attachment base-closed / mandatory base-open: 12 / 0
RNG/source mutation/partial publication: 0 / 0 / 0
MAP06_07 source baseline Assets meta: 3297
Current repair execution Assets meta: 3304
Authoring CSV/meta: 50 / 50
Authoring manifest SHA-256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Duplicate GUID groups: 0
```

MAP05 Type4 규칙과 approved reserved-adapter topology는 그대로 보존한다.

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
02_PHASE_ROADMAP/MAP06_TYPE0_OPTIONAL_REGIONS.md
03_CSV_SCHEMA/ENUM_REFERENCE.md
03_CSV_SCHEMA/TILE_AUTHORING_FORMAT.md
```

reference는 exclusive final role, P04 ownership, open edge→InactiveBuffer 금지, 남은 셀 inactive/장식 boundary 전환 규칙을 확인하는 용도다. Authoring CSV body를 runtime source로 다시 파싱하거나 수정하지 않는다. `generation_profiles.csv`의 정책 범위 검증은 MAP06_09 validator 소유이므로 이번 Task에서 profile row를 재파싱하거나 role을 임의 재분배하지 않는다.

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
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalReturnPolicySettings.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalReturnPolicyAssignment.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalReturnPolicyDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalReturnPolicyResult.cs
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
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

위 파일과 matching meta, approved Runtime/Test `Generation` path-only inventory, Authoring CSV/meta count·aggregate hash, 전체 Assets meta GUID, patch marker 이후 change-scope path만 확인할 수 있다.

금지: installed Authoring CSV body runtime 재파싱·수정, generated CSV body source 사용, MAP06_09+ Task body, boundary profile/recipe/microchunk body, unrelated production/test body, Legacy/Stage/P6/P11 generator body, Scene/Prefab YAML.

## WRITE ALLOWLIST

### MAP06_08 Runtime production C# — exact 6

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/InactiveBufferAssignmentEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/InactiveBufferAssignmentSettings.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/InactiveBufferAssignment.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/InactiveBufferAssignmentDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/InactiveBufferAssignmentResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/InactiveBufferAssigner.cs
```

### MAP06_08 Runtime EditMode test — exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/InactiveBufferAssignerTests.cs
```

### 기존 phase-boundary test 수정 — exact up to 13

MAP06_08 production symbol 허용 및 MAP06_09+ future symbol 금지 유지를 위해 필요한 경우 아래 existing test C#만 수정할 수 있다. matching `.cs.meta`는 수정하지 않는다.

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
```

MAP06_08 C# 7개와 matching `.cs.meta` 7개 생성 또는 수정, 위 boundary test C# 최대 13개 수정, Result 1개만 허용한다. 이미 BLOCKED 시도에서 생성된 MAP06_08 files/metas는 보존하고 같은 allowlist 안에서만 교정한다. MAP05/MAP06_01~07 production source, world/site/biome/mandatory/Type0/access/reward/return artifacts, Authoring/generated CSV/meta, asmdef, Scene, Prefab, Packages, ProjectSettings는 수정하지 않는다. 신규 directory/folder meta/asmdef/asmref를 만들지 않는다.

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
Input artifacts  = P00 GRID + P01 SITE RESERVATION + P02 BIOME PUBLICATION + P03 MANDATORY GRAPH + P04 TYPE0/RETURN
Read-only guards = OPTIONAL REGION/ACCESS/REWARD SOURCE CHAIN
Output artifact  = INACTIVE_BUFFER_ASSIGNMENT_SNAPSHOT
Pass ID          = PASS_OPTIONAL
RNG consumption  = exact 0
```

output은 immutable role/classification snapshot이다. `GeneratedWorldData`, `SectorCell`, site/biome/mandatory/Type0/return source를 in-place mutate하지 않는다.

## Existing Sector Role Contract

`SectorCell.cs`의 existing `SectorRole`과 token contract를 그대로 재사용한다.

```text
Unassigned | Mandatory | Type0 | ReservedSite | InactiveBuffer
CSV token for output projection: INACTIVE_BUFFER
```

새 `SectorRole`, `BOUNDARY` sector role 또는 duplicate codec을 만들지 않는다. `DecorativeBoundary`는 이번 result 내부의 presentation classification이며 role은 항상 `InactiveBuffer`다.

신규 `InactiveBufferAssignmentEnums.cs`:

```text
InactiveBufferAssignmentStatus:
  Completed | InvalidInput | InvalidSettings | InvalidSource
  InvalidAccounting | InvalidTopology

InactiveBufferAssignmentErrorCode:
  NullInput | InvalidStatus | InvalidDigest | SourceMismatch
  InvalidWorld | InvalidSectorIndex | DuplicateOwnership | OwnershipOverlap
  InvalidSiteReservation | InvalidBiomePublication | InvalidMandatoryGraph
  InvalidType0Assignment | InvalidReturnPolicy | OpenEdgeToInactive
  IncompleteAccounting

InactiveBufferKind:
  InteriorInactive | DecorativeBoundary
```

undefined enum을 거부한다. internal status/error/kind에 authoring token codec을 만들지 않는다.

## `InactiveBufferAssignmentSettings` Contract

sealed immutable object다.

```text
bool RequireFullWorldAccounting
bool RequireClosedInactiveBoundaries
bool ClassifyClaimAdjacentAsDecorativeBoundary
```

approved settings는 모두 exact `true`다. false는 이번 Task의 frozen contract와 맞지 않으므로 invalid settings다. static mutable/default settings instance를 만들지 않는다.

## Source-Chain Validation

assigner는 다음을 순서대로 검증한다.

1. world는 exact 13×13, 169 cells, unique row-major sector index/coordinate identity다.
2. site snapshot은 approved valid publication이며 7 reservations, 8 unique reserved footprint sectors, 6 entries, 4 Core seeds다.
3. biome publication은 approved valid state이며 169 sectors, 165 assigned, 4 reserved-unassigned accounting이다. biome ownership을 수정하지 않는다.
4. mandatory validation report는 valid이고 graph identity는 nodes/directed/undirected/route cells `47/96/48/47`이다.
5. Type0 result와 return result는 `Completed`, lowercase 64-hex digest, 자체 accounting이 valid하다.
6. return source Type0 digest는 Type0 `CanonicalDigest`, return source growth digest는 source snapshot growth digest와 일치한다.
7. return assignment region identity와 Type0 39-cell identity가 one-to-one이며 returnable/non-returnable `39/0`이다.
8. site footprint와 mandatory route cell source membership은 approved reserved adapters에서만 겹칠 수 있다. approved fixture에서는 exact `Site ∩ Mandatory = {0,28,106}`이고 세 sector는 world/SectorCell의 approved reserved-adapter marker와 일치해야 한다.
9. Site ∩ Type0, Mandatory ∩ Type0은 empty여야 한다. Type0이 site 또는 mandatory source와 겹치면 illegal ownership overlap이다.
10. reserved-adapter overlap은 duplicate ownership이나 atomic failure가 아니다. exclusive projection에서는 ReservedSite가 final sector role owner이고, mandatory graph membership은 route topology/source-chain 검증에 계속 사용한다.
11. approved fixture exact counts/digests는 test/Result에서 검증하고 production 일반 입력 수량으로 하드코딩하지 않는다.

invalid source 하나라도 발견하면 assignment를 하나도 publish하지 않는다.

## Exclusive Ownership / Classification Algorithm

모든 source collection을 copied canonical order로 처리한다.

1. 169-length source membership table을 만들고 site footprint, mandatory graph route cell, Type0 assignment cell membership을 별도로 표시한다.
2. source 내부 duplicate sector identity, out-of-range sector, illegal protected overlap을 stable error로 수집한다.
3. legal source overlap은 approved reserved-adapter overlap뿐이다. site+mandatory overlap이 approved adapter가 아니거나 Type0이 다른 protected source와 겹치면 `OwnershipOverlap` atomic failure다.
4. error가 없을 때 exclusive projected ownership table을 만든다. site footprint는 항상 `ReservedSite`, site에 없는 mandatory graph route cell은 `Mandatory`, site/mandatory 어디에도 없는 Type0 cell은 `Type0`로 표시한다.
5. sector index `0..168`을 순회하고 projected protected가 아닌 sector마다 exact one `InactiveBufferAssignment`를 만든다.
6. fixed cardinal neighbor order는 `L, R, U, D`다. world 밖은 neighbor로 추가하지 않는다.
7. projected protected cardinal neighbor가 하나 이상이면 `DecorativeBoundary`, 없으면 `InteriorInactive`다.
8. protected neighbor indices와 inactive neighbor indices는 fixed direction order에서 copied read-only list로 보존한다.
9. mandatory graph open edge와 Type0 base open edge가 inactive sector를 향하지 않는지 검사한다. 하나라도 있으면 `OpenEdgeToInactive` atomic failure다. approved reserved adapters는 inactive가 아니므로 이 검사에서 protected로 취급한다.
10. `ReservedSite + MandatoryOnly + Type0 + InactiveBuffer == 169`, projected ownership uniqueness `169`, Unassigned `0`, illegal overlap `0`을 확인한 뒤에만 result를 publish한다.

approved fixture exact accounting:

```text
world = 169
source ReservedSite/Mandatory/Type0 = 8/47/39
approved Site∩Mandatory adapter overlap = 3 at 0,28,106
source Site∩Type0 / Mandatory∩Type0 = 0/0
exclusive projected ReservedSite/MandatoryOnly/Type0 = 8/44/39
protected union = 91
inactive assignments = 78
unassigned / illegal overlap / duplicate = 0/0/0
open mandatory-or-Type0 edge to inactive = 0
RNG / source mutation / partial publication = 0/0/0
```

DecorativeBoundary/InteriorInactive split, protected↔inactive cardinal edge count, inactive↔inactive undirected edge count, world-edge inactive count는 actual source topology에서 계산해 test/Result에 exact 숫자로 기록한다. production에 expected split을 literal로 하드코딩하지 않는다.

## `InactiveBufferAssignment` Contract

sealed immutable object다.

```text
int SectorIndex
SectorCoord Coord
SectorRole Role
InactiveBufferKind Kind
IReadOnlyList<int> ProtectedNeighborSectorIndices
IReadOnlyList<int> InactiveNeighborSectorIndices
bool TouchesWorldEdge
```

- `SectorIndex`/`Coord`는 world identity와 exact 일치한다.
- `Role`은 exact `SectorRole.InactiveBuffer`다.
- protected neighbor가 있으면 `DecorativeBoundary`, 없으면 `InteriorInactive`다.
- neighbor list는 copied read-only이며 duplicate/out-of-bounds가 없다.
- route mask, optional region ID, site ID, boundary profile ID, sector recipe ID, open edge를 소유하지 않는다.

## Diagnostics / Result Contract

`InactiveBufferAssignmentDiagnostics` sealed immutable fields:

```text
int WorldSectorCount
int SiteReservationCount
int ReservedSiteSectorCount
int MandatoryRouteCellCount
int MandatoryExclusiveSectorCount
int Type0CellCount
int SiteMandatoryOverlapCount
int ApprovedReservedAdapterOverlapCount
int ProtectedUnionCount
int AssignmentCount
int DecorativeBoundaryCount
int InteriorInactiveCount
int WorldEdgeInactiveCount
int ProtectedToInactiveCardinalEdgeCount
int InactiveToInactiveUndirectedEdgeCount
int UnassignedSectorCount
int IllegalOwnershipOverlapCount
int DuplicateSectorCount
int OpenEdgeToInactiveCount
int RngDrawCount
int SourceMutationCount
```

`InactiveBufferAssignmentResult` sealed immutable properties:

```text
InactiveBufferAssignmentStatus Status
IReadOnlyList<InactiveBufferAssignment> Assignments
InactiveBufferAssignmentDiagnostics Diagnostics
IReadOnlyList<InactiveBufferAssignmentError> Errors
string SourceMandatoryGraphDigest
string SourceType0AssignmentDigest
string SourceGrowthDigest
string SourceReturnPolicyDigest
string CanonicalDigest
int RngDrawCount
bool IsSuccess
```

errors는 code, sector index, source owner, source field, message 순으로 stable sort/dedupe한다. 실패는 assignments empty, canonical digest empty, unassigned publication/RNG/source mutation `0/0/0`, partial publication `0`이다.

CanonicalDigest는 source digests, copied settings, sector-index ordered assignments, classification/neighbor lists, diagnostics를 stable UTF-8 ordinal field order로 SHA-256한 lowercase 64-hex다.

## `InactiveBufferAssigner` Contract

stateless sealed service다.

```text
InactiveBufferAssignmentResult Assign(
    GeneratedWorldData world,
    SiteReservationSnapshot siteReservations,
    BiomePatchValidationPublication biomePublication,
    MandatoryRouteGraph graph,
    MandatoryRouteValidationReport validationReport,
    Type0RouteMaskAssignmentResult type0Assignments,
    OptionalReturnPolicyResult returnPolicies,
    string sourceMandatoryGraphDigest,
    InactiveBufferAssignmentSettings settings)
```

- reference input null, invalid status/digest/accounting, source-chain mismatch를 거부한다.
- source object/collection을 copied deterministic order로 읽고 mutate하지 않는다.
- caller order, culture, service reuse, thread/time에 독립적이다.
- hidden cache, static mutable collection, reflection, filesystem, Registry singleton, RNG는 0이다.
- dictionary/hash-set iteration order를 output order로 사용하지 않는다.
- `GeneratedWorldData`/`SectorCell` role을 in-place 변경하지 않고 immutable assignment snapshot만 반환한다.

## Boundary Test Advance

허용할 MAP06_08 symbols:

```text
InactiveBufferAssignmentStatus
InactiveBufferAssignmentErrorCode
InactiveBufferKind
InactiveBufferAssignmentSettings
InactiveBufferAssignment
InactiveBufferAssignmentDiagnostics
InactiveBufferAssignmentError
InactiveBufferAssignmentResult
InactiveBufferAssigner
InactiveBufferAssignerTests
```

계속 금지할 MAP06_09+ examples:

```text
OptionalRegionValidator
OptionalRegionValidationReport
OptionalRegionOverlay
Map06ExitTests
GeneratedOptionalRegionCsvWriter
```

MAP05 Type4, MAP06_04 Type0 base-closed/L+R, MAP06_05 access/clue, MAP06_06 score/tier, MAP06_07 returnability assertions를 약화하지 않는다. MAP06_09+ 금지 case 수를 줄이지 않는다.

## Required Tests

새 `InactiveBufferAssignerTests`는 최소 `281` actual PASS cases를 가져야 한다.

- enum/settings/assignment/result immutability `>=30`
- 169-cell row-major world and source validation `>=34`
- protected ownership/approved adapter overlap/exclusive accounting `>=38`
- inactive assignment completeness and canonical order `>=32`
- decorative/interior cardinal classification oracle `>=30`
- neighbor lists/world-edge/topology counters `>=30`
- mandatory/Type0 open-edge-to-inactive atomic rejection `>=28`
- canonical digest/culture/order/service reuse determinism `>=26`
- source mutation/RNG/Type4/boundary advance `>=32`

Existing required gates:

```text
InactiveBufferAssignerTests >=281 PASS
OptionalReturnPolicyResolverTests 289/289 PASS
OptionalRewardTierCalculatorTests 279/279 PASS
OptionalAccessRuleAssignerTests 289/289 PASS
Type0RouteMaskAssignerTests 257/257 PASS
MAP06 prior combined selection 630/630 PASS
Existing MAP05 aggregate 1959/1959 PASS
Actually executed required total >=3984 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
Assets meta 3304
existing MAP06_08 C#/meta preserved 7/7
existing boundary test C# modified <=13
Authoring CSV/meta 50/50
Authoring manifest SHA-256 unchanged
duplicate GUID groups 0
MAP05/MAP06_01~07 production/world/site/biome/graph/mask/models/assignments/CSV/asmdef/Scene/Prefab/Packages/ProjectSettings modifications 0
generated CSV files created by this task 0
boundary profile/recipe/microchunk/tile/socket/edge artifacts created 0
```

Discovery-only count나 arithmetic total을 PASS로 기록하지 않는다. 위 total은 Unity EditMode Test Runner에서 실제 실행한 결과여야 한다.

## Result Format

Write exactly `MapDesign/MCP/REPORTS/MAP06_08_ASSIGN_INACTIVE_BUFFERS_RESULT.md`.

Required top lines:

```text
TASK: MAP06_08_ASSIGN_INACTIVE_BUFFERS
STATUS: PASS|FAIL|BLOCKED
MAP06_08: COMPLETE ELIGIBLE|NOT COMPLETE
MAP06_09_IMPLEMENT_OPTIONAL_REGION_VALIDATOR: LOCKED / DO NOT START
```

Result must include patch/receipt, prior Result and Task SHA gates, created/changed files, source-chain digests, settings, world/site/biome/mandatory/Type0/return source accounting, source protected set intersections, approved reserved-adapter overlap evidence, exclusive projected ownership accounting, exact inactive/decorative/interior counts, per-sector canonical classification or digest-backed fixture table, neighbor/edge counters, full-world accounting, open-edge-to-inactive zero, atomic failure and mutation evidence, MAP05/Type4 and MAP06_04~07 preservation, exact test jobs, Unity gate, asset/meta/CSV/GUID/change-scope gate, and NEXT that finalizes only MAP06_08 while keeping MAP06_09 locked.

## Done Conditions

- [ ] Preconditions, prior Result SHA, current Task SHA verified; MAP06_08 is sole CURRENT.
- [ ] P00/P01/P02/MAP05/Type0/return source identities and statuses verified.
- [ ] Source counts are ReservedSite/Mandatory/Type0 `8/47/39`.
- [ ] Approved reserved-adapter overlap is exact `Site ∩ Mandatory = {0,28,106}` and no other source overlap exists.
- [ ] Exclusive projection is ReservedSite/MandatoryOnly/Type0 `8/44/39`, protected union `91`.
- [ ] Every unclaimed sector has exactly one immutable `InactiveBuffer` assignment.
- [ ] DecorativeBoundary iff at least one protected cardinal neighbor; remaining cells are InteriorInactive.
- [ ] Full accounting is `169 = 8 + 44 + 39 + 78`, Unassigned/illegal-overlap/duplicate `0/0/0`.
- [ ] No mandatory or Type0 open edge targets an inactive sector.
- [ ] No boundary profile/recipe/microchunk/tile/socket/edge/generated CSV is synthesized.
- [ ] MAP06_08 symbols allowed; MAP06_09+ forbidden.
- [ ] Mandatory graph/masks, Type4, Type0/access/reward/return assignments and Authoring CSV unchanged.
- [ ] Required Unity EditMode gates actually executed and Result evidence complete.

If any required gate fails or Unity MCP is unavailable, write `STATUS: FAIL` or `STATUS: BLOCKED`, keep MAP06_08 CURRENT, and do not create or start MAP06_09.
