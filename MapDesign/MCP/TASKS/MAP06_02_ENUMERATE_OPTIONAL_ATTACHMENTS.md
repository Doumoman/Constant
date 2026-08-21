# MAP06_02 — Enumerate Optional Attachments

```yaml
status_control:
  task_key: MAP06_02_ENUMERATE_OPTIONAL_ATTACHMENTS
  result_file: REPORTS/MAP06_02_ENUMERATE_OPTIONAL_ATTACHMENTS_RESULT.md
```

## TASK TYPE

```text
RUNTIME IMMUTABLE P04 OPTIONAL-ATTACHMENT CANDIDATE ENUMERATION + MAP05 BOUNDARY TEST ADVANCE + EDITMODE TESTS
```

## Objective

MAP05에서 승인된 mandatory route graph와 MAP06_01에서 확정된 optional region model 계약을 입력으로 사용해, 필수망에 인접한 미사용 sector의 optional attachment candidate를 deterministic하게 열거한다.

이번 Task의 산출물은 attachment candidate ID, candidate value, enumeration settings, diagnostics, result aggregate, enumerator다. 후보의 identity/order/filter/diagnostics와 immutability만 구현한다.

optional region grower, Type0 route mask assignment, access/clue assignment, reward tier calculation algorithm, return device placement, inactive buffer assignment, validator, overlay, generated CSV writer는 구현하지 않는다.

MAP06_02 production symbol이 새로 생기므로 MAP05 phase-boundary negative assertions는 MAP06_02 symbols를 허용하고 MAP06_03+ future symbols만 금지하도록 필요한 기존 boundary test만 교정한다.


## Repair v1.1 Boundary Correction

현재 MAP06_02 FAIL Result에서 유일한 구현 차단 원인은 `OptionalRegionModelsTests.RuntimeSurfaceHasNoMutableStaticUnityEditorOrMap06_02PlusSymbols`가 MAP06_02 production symbol `OptionalAttachmentEnumerator`를 여전히 금지한 것이다.

이 revised Task는 기존 MAP06_02 구현 산출물을 보존하고, `OptionalRegionModelsTests.cs`의 phase-boundary assertion을 MAP06_02 허용 / MAP06_03+ 금지 기준으로 한 단계 전진시키는 수정 권한만 추가한다. production implementation, candidate output, generated CSV, Master, Status는 이 repair로 변경하지 않는다.

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
12. `REPORTS/MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS
STATUS: PASS
MAP06_01: COMPLETE ELIGIBLE
MAP06_02_ENUMERATE_OPTIONAL_ATTACHMENTS: LOCKED / DO NOT START
SHA-256: 8d8f2b8bae5b08c9bf5fd258a225db89d16bffa5ca8faa058ef78ac02334442e
```

이 별도 patch가 적용된 뒤에만 MAP06_02를 실행한다. MAP06_03 이후 Task body는 읽거나 시작하지 않는다.

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

reference는 Type0 optional attachment 용어 확인용이다. installed Authoring CSV body를 다시 읽거나 파싱하지 않는다. source of truth는 approved typed MAP05 graph, validation publication, MAP06_01 optional region model types다.

## READ ALLOWLIST

### Existing domain / P00~P04

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGridIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedWorldData.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchValidationPublication.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteTerminalSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteMaskFamily.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphNodeId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphEdgeId.cs
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
Assets/Scripts/MapDesign/Runtime/OptionalRegionId.cs
Assets/Scripts/MapDesign/Runtime/OptionalRegionEnums.cs
Assets/Scripts/MapDesign/Runtime/OptionalRegionAttachment.cs
Assets/Scripts/MapDesign/Runtime/OptionalRegionCell.cs
Assets/Scripts/MapDesign/Runtime/OptionalRegion.cs
Assets/Scripts/MapDesign/Runtime/OptionalRegionSnapshot.cs
```

`OptionalRegionId.cs`가 실제 존재하는 runtime directory를 MAP06_02 신규 runtime file의 target directory로 사용한다. `_Game/.../Generation`과 `Assets/Scripts/MapDesign/Runtime` 중 하나만 선택하고 duplicate type을 만들지 않는다.

### Focused tests / assemblies

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/HorizontalBackboneRouterTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteGraphValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map05ExitTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/UpDownConflictResolverTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/VerticalGatewayPlannerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteGraphBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Diagnostics/MandatoryRouteOverlayTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Preview/MandatoryRouteOverlaySceneDrawerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionModelsTests.cs
Assets/Tests/EditMode/MapDesign/OptionalRegionModelsTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

위 파일과 matching meta, approved Runtime/Test `Generation` 및 실제 OptionalRegion directory의 path-only inventory, Authoring CSV/meta count·hash, 전체 meta GUID, task marker 이후 change-scope path만 확인할 수 있다.

금지:

- installed Authoring CSV body 재파싱·수정
- generated CSV body를 disk에서 읽어 source of truth로 사용
- MAP06_03 이후 Task body
- unrelated production/test C# body
- Legacy/Stage/P6/P11 generator body
- Scene/Prefab YAML

## WRITE ALLOWLIST

### 신규 Runtime production C# — exact 6 basenames, one target directory only

실제 존재하는 `OptionalRegionId.cs`와 같은 runtime directory에 아래 6개만 만든다.

```text
OptionalAttachmentCandidateId.cs
OptionalAttachmentCandidate.cs
OptionalAttachmentEnumerationSettings.cs
OptionalAttachmentEnumerationDiagnostics.cs
OptionalAttachmentEnumerationResult.cs
OptionalAttachmentEnumerator.cs
```

허용 target directory는 둘 중 하나다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/
Assets/Scripts/MapDesign/Runtime/
```

### 신규 Runtime EditMode test — exact 1 basename, one target directory only

실제 존재하는 `OptionalRegionModelsTests.cs`와 같은 test directory에 아래 1개만 만든다.

```text
OptionalAttachmentEnumeratorTests.cs
```

허용 target directory는 둘 중 하나다.

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/
Assets/Tests/EditMode/MapDesign/
```

### 기존 boundary test 수정 — exact up to 7 C# files

MAP06_02 production symbol 허용 및 MAP06_03+ future symbol 금지 유지를 위해 필요한 경우 아래 기존 test C#만 수정할 수 있다. matching `.cs.meta`는 수정하지 않는다.

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/HorizontalBackboneRouterTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteGraphValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map05ExitTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/UpDownConflictResolverTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/VerticalGatewayPlannerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionModelsTests.cs
```

신규 C# 7개와 matching `.cs.meta` 7개, 위 boundary test C# 최대 7개 수정, Result 1만 허용한다. existing production graph/CSV/SectorCell/Authoring CSV/asmdef/Scene/Prefab/Packages/ProjectSettings는 수정하지 않는다. 기존 approved directory를 재사용하고 folder meta를 만들지 않는다.

## Namespace / Assembly

```text
Runtime namespace: StarNight.Map.WorldGeneration.Generation
Test namespace:    StarNight.Map.Tests.WorldGeneration.Generation
Runtime assembly:  Game.Map.Runtime
Test assembly:     Game.Map.Tests.EditMode
```

실제 MAP06_01 files가 기존 프로젝트 namespace/assembly bridge를 사용했다면 같은 namespace/assembly에 맞춘다. `UnityEditor`, Unity object/lifecycle, record/record struct, `required`, `init`, nullable-reference directive, reflection factory, singleton/static mutable state를 도입하지 않는다.

## Frozen P04 Boundary

```text
Input artifacts  = MANDATORY_ROUTE_GRAPH + OPTIONAL_REGION_MODELS
Read-only guards = SITE_RESERVATIONS + BIOME_PATCH_PUBLICATION + GENERATED_WORLD_DATA
Output artifact  = OPTIONAL_ATTACHMENT_CANDIDATES
Pass ID          = PASS_OPTIONAL
RNG stream       = RNG_OPTIONAL / WORLD
Grid             = 13 x 13 / 169 sectors / lower-left origin
Depth            = candidate initial depth 1 only
Type0 invariant  = no route mask assignment in this Task; later Type0 cannot have L+R simultaneous
Mandatory guard  = mandatory route graph unchanged
```

이번 Task의 RNG consumption은 exact `0`이다. enumeration은 graph와 reserved/used sector sets에서 deterministic 후보 목록을 계산할 뿐, optional region cell growth나 route mask를 배정하지 않는다.

MAP05 Type4 규칙은 보존한다.

```text
Type4 requires U+D open.
L/R are independent and preserve actual mandatory graph adjacency.
UD, LUD, RUD, LRUD are all legal.
```

## Candidate Definition

한 후보는 mandatory route sector에서 cardinal neighbor인 unused entry sector로 나가는 단일 optional attachment를 뜻한다.

```text
CandidateId
AttachmentOrder
MandatoryRouteSectorIndex
SectorCoord MandatoryRouteSector
MandatoryRouteGraphNodeId MandatoryRouteNodeId
int EntrySectorIndex
SectorCoord EntrySector
int DirectionDx
int DirectionDy
int InitialDepth
bool IsAllowed
string RejectionCode
```

필수 조건:

- mandatory sector는 approved MAP05 mandatory route graph cell이어야 한다.
- entry sector는 mandatory sector의 cardinal neighbor이고 grid 안에 있어야 한다.
- direction은 mandatory→entry 방향과 일치하는 `(-1,0)`, `(1,0)`, `(0,1)`, `(0,-1)` 중 하나다.
- entry sector는 mandatory route cell, mandatory terminal sector, site footprint/reservation sector, biome reserved/inactive sector에 포함되면 후보에서 제외한다.
- entry sector가 이미 다른 accepted candidate의 entry로 쓰이면 deterministic order상 먼저 선택된 후보만 accepted다.
- candidate 자체는 region을 생성하지 않으며 `OptionalRegionId`를 할당하지 않는다.
- candidate initial depth는 exact `1`이다.
- candidate는 `OptionalRegionAttachment`를 만들 수 있는 충분한 source data를 보유하되, 실제 OptionalRegion aggregate는 만들지 않는다.

## Deterministic Ordering

Accepted candidates의 canonical order는 아래 key의 ascending order다.

```text
mandatory graph BFS distance from Start terminal
mandatory route sector index
direction order L, R, U, D
entry sector index
mandatory route graph node id ordinal
```

동점은 ordinal string 비교만 사용한다. culture-sensitive compare, hash iteration order, LINQ over unordered dictionary, Unity object order, file system order, random/RNG draw를 사용하지 않는다.

Candidate ID grammar:

```text
OPT_ATTACH_0000
OPT_ATTACH_0001
...
```

ID는 accepted canonical order에 따라 0부터 contiguous하게 부여한다. rejected candidate는 diagnostics에만 남기며 accepted ID를 소비하지 않는다.

## `OptionalAttachmentCandidateId` Contract

`public readonly struct`, `IEquatable<OptionalAttachmentCandidateId>`, `IComparable<OptionalAttachmentCandidateId>`다.

```text
string Value
bool IsValid
OptionalAttachmentCandidateId(string value)
bool TryCreate(string value, out OptionalAttachmentCandidateId result)
OptionalAttachmentCandidateId FromOrdinal(int ordinal)
bool TryGetOrdinal(out int ordinal)
```

- grammar exact `^OPT_ATTACH_[0-9]{4}$`; default invalid.
- ordinal range는 `0..9999`다.
- equality/order는 ordinal case-sensitive, hash는 deterministic이다.
- valid `ToString()`은 exact `Value`다.
- lowercase, missing zero padding, negative, overflow, whitespace, non-ASCII를 거부한다.

## `OptionalAttachmentCandidate` Contract

sealed immutable object다.

```text
OptionalAttachmentCandidateId CandidateId
int AttachmentOrder
int MandatoryRouteSectorIndex
SectorCoord MandatoryRouteSector
MandatoryRouteGraphNodeId MandatoryRouteNodeId
int EntrySectorIndex
SectorCoord EntrySector
int DirectionDx
int DirectionDy
OptionalRegionDepth InitialDepth
```

- CandidateId valid, node ID valid.
- AttachmentOrder는 CandidateId ordinal과 일치한다.
- mandatory/entry sector index-coordinate identity는 `WorldGridIndex`와 일치해야 한다.
- mandatory→entry가 cardinal neighbor인지 검증한다.
- InitialDepth는 exact `1`이다.
- mutation 없이 constructor에서 inputs를 검증한다.

## `OptionalAttachmentEnumerationSettings` Contract

sealed immutable object다.

```text
int MaxCandidates
bool ExcludeMandatoryTerminals
bool ExcludeSiteReservations
bool ExcludeBiomeReservedOrInactive
bool DeduplicateEntrySector
```

기본값:

```text
MaxCandidates = 9999
ExcludeMandatoryTerminals = true
ExcludeSiteReservations = true
ExcludeBiomeReservedOrInactive = true
DeduplicateEntrySector = true
```

MaxCandidates는 `1..9999`만 허용한다. 기본 설정은 allocation 이후 deterministic canonical result를 바꾸지 않는다.

## `OptionalAttachmentEnumerationDiagnostics` Contract

sealed immutable object다.

```text
int RawNeighborProbes
int OutOfBoundsRejected
int MandatoryRejected
int TerminalRejected
int SiteReservationRejected
int BiomeReservedRejected
int DuplicateEntryRejected
int AcceptedCount
IReadOnlyList<string> RejectionCodes
```

- counters 합은 deterministic하고 accepted/rejected accounting과 일치해야 한다.
- `RejectionCodes`는 canonical order의 stable string list다.
- diagnostics는 Result와 tests 검증용이며 generation state를 mutate하지 않는다.

## `OptionalAttachmentEnumerationResult` Contract

sealed immutable object다.

```text
IReadOnlyList<OptionalAttachmentCandidate> Candidates
OptionalAttachmentEnumerationDiagnostics Diagnostics
int MandatoryRouteGraphNodeCount
int MandatoryRouteGraphDirectedEdgeCount
int MandatoryRouteCellCount
string CanonicalDigest
```

- candidates는 accepted canonical order로 정렬된다.
- CandidateId/AttachmentOrder는 `0..Count-1` contiguous다.
- duplicate candidate ID, duplicate entry sector, mandatory cell overlap을 거부한다.
- MAP05 graph identity `47/96/47`을 보존한다.
- `CanonicalDigest`는 candidate records와 diagnostics의 deterministic stable digest다.

## `OptionalAttachmentEnumerator` Contract

stateless sealed service 또는 static-free immutable service다.

```text
OptionalAttachmentEnumerationResult Enumerate(
    GeneratedWorldData world,
    MandatoryRouteGraph graph,
    MandatoryRouteValidationReport validationReport,
    SiteReservationSnapshot siteReservations,
    BiomePatchValidationPublication biomePublication,
    OptionalAttachmentEnumerationSettings settings)
```

구현 요구:

- world/graph/validationReport/siteReservations/biomePublication/settings null을 거부한다.
- mandatory graph node/edge/cell collection을 복사한 뒤 deterministic 정렬한다.
- approved MAP05 validation이 errors/warnings/violations 없이 valid인지 확인한다.
- mandatory route graph, SectorCell, GeneratedWorldData, CSV를 mutate하지 않는다.
- RNG draw count exact `0`.
- no hidden global cache, no static mutable collections, no reflection.

## Boundary Test Advance

MAP06_01 repair가 MAP06_02+ symbols를 금지한 상태라면, 이번 Task에서 금지 목록을 MAP06_03+로 한 단계 전진시킨다.

반드시 허용해야 하는 MAP06_02 symbols:

```text
OptionalAttachmentCandidateId
OptionalAttachmentCandidate
OptionalAttachmentEnumerationSettings
OptionalAttachmentEnumerationDiagnostics
OptionalAttachmentEnumerationResult
OptionalAttachmentEnumerator
OptionalAttachmentEnumeratorTests
```

계속 금지해야 하는 future symbols examples:

```text
OptionalRegionGrower
Type0RouteMaskAssigner
OptionalAccessRuleAssigner
OptionalRewardTierCalculator
OptionalReturnPolicyResolver
InactiveBufferAssigner
OptionalRegionValidator
OptionalRegionOverlay
GeneratedOptionalRegionCsvWriter
```

MAP05 Type4 negative/assertion logic은 약화하지 않는다. Type4 U+D mandatory 및 L/R independent rule을 유지한다.

## Required Tests

새 test suite `OptionalAttachmentEnumeratorTests`는 최소 160 actual PASS cases를 가져야 한다.

필수 test groups:

- CandidateId grammar/default/equality/order/ordinal `>=24`
- settings validation/default immutability `>=12`
- candidate constructor invariants/cardinal neighbor/index-coordinate identity `>=28`
- deterministic enumeration order and contiguous IDs `>=24`
- exclusion filters: out-of-bounds, mandatory, terminal, site reservation, biome reserved/inactive, duplicate entry `>=36`
- diagnostics accounting/canonical digest `>=18`
- graph mutation guard and RNG draw `0` `>=10`
- Type4 preservation: U+D mandatory, L/R independent, `UD/LUD/RUD/LRUD` legal `>=8`
- boundary negative assertions advanced to MAP06_03+ `>=10`

Existing required gates:

```text
OptionalAttachmentEnumeratorTests >=160 PASS
OptionalRegionModelsTests 194/194 PASS
Existing MAP05 aggregate 1959/1959 PASS
Actually executed required total >=2355 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
Assets meta 3261
new C#/meta 7/7
existing boundary test C# modified <=7
repair-only additional existing test C# modification: OptionalRegionModelsTests.cs
new production/test C# from v1.0 preserved 7/7
Authoring CSV/meta 50/50
duplicate GUID groups 0
production graph/CSV/SectorCell/asmdef/Scene/Prefab/Packages/ProjectSettings modifications 0
generated CSV files created by this task 0
```

Do not count discovery-only or arithmetic-only totals as PASS. Required totals must be actually executed Unity EditMode Test Runner results.

## Result Format

Write exactly one Result file:

```text
MapDesign/MCP/REPORTS/MAP06_02_ENUMERATE_OPTIONAL_ATTACHMENTS_RESULT.md
```

Required top lines:

```text
TASK: MAP06_02_ENUMERATE_OPTIONAL_ATTACHMENTS
STATUS: PASS|FAIL|BLOCKED
MAP06_02: COMPLETE ELIGIBLE|NOT COMPLETE
MAP06_03_IMPLEMENT_OPTIONAL_REGION_GROWER: LOCKED / DO NOT START
```

The Result must include:

- patch id/version and `.APPLIED` receipt status
- prior Result SHA check result
- selected actual runtime/test target directory for new files
- changed/created file list
- candidate output summary: raw probes, accepted, rejection counters, canonical digest
- MAP05 graph identity `47/96/47`, mask counts `20/4/4/17/0/0/2`
- Type4 preservation statement: U+D mandatory, L/R independent, UD/LUD/RUD/LRUD legal
- test jobs and exact pass/fail/skipped counts
- Unity compile/Console/warning gate
- asset/meta/CSV/GUID/change-scope gate
- clear NEXT statement: finalize MAP06_02 only, keep MAP06_03 LOCKED, do not auto-start next task

## Done Conditions

- [ ] Preconditions and prior Result SHA verified.
- [ ] MAP06_02 is the only CURRENT task.
- [ ] Candidate enumeration models/services implemented only in the existing OptionalRegion runtime directory.
- [ ] MAP06_02 symbols allowed in boundary tests; MAP06_03+ future symbols still forbidden.
- [ ] No optional grower/mask/access/reward/return/inactive/validator/overlay/generated CSV behavior implemented.
- [ ] Mandatory graph unchanged and Type4 rule preserved.
- [ ] Required Unity EditMode gates actually executed and passed.
- [ ] Result file written with complete evidence.

If any required gate fails or Unity MCP is unavailable, write `STATUS: FAIL` or `STATUS: BLOCKED`, keep MAP06_02 CURRENT, and do not create or start MAP06_03.
