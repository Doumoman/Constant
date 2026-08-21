# MAP06_10 — MAP06 Overlay And Exit Tests

```yaml
status_control:
  task_key: MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS
  result_file: REPORTS/MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS_RESULT.md
```

## TASK TYPE

```text
RUNTIME IMMUTABLE MAP06 OPTIONAL REGION OVERLAY SNAPSHOT + EDITOR SCENE DRAWER + MAP06 PHASE EXIT TESTS + PHASE-BOUNDARY TEST ADVANCE
```

## Objective

MAP06_09 PASS/finalize 뒤 approved MAP06 source-chain and validation report를 시각 점검 가능한 immutable overlay snapshot으로 결합한다. Overlay는 13x13 sector grid 위에 access rule color, depth label, attachment/contact marker, return witness arrow, reward tier marker, inactive kind, validation issue state를 표시한다.

이 Task는 MAP06 phase exit gate까지 완료한다. MAP06_01~09 source artifacts의 identity, digest, accounting, returnability, Type4, Type0 mask, access/clue, reward, inactive, validation report를 다시 검증하고, overlay snapshot과 Scene drawer가 같은 immutable data를 표시하는지 확인한다.

MAP07 microchunk definition, tile layer rules, socket/edge validation, sector recipe, boundary chunk, generated CSV writer, gameplay tile destruction, door/device prefab, scene/prefab authoring은 구현하지 않는다. `OptionalRegionOverlayConnection`은 debug presentation connector이며 generated `OptionalOverlayEdge`, `edge_signature_id`, socket, or CSV artifact가 아니다.

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
12. `REPORTS/MAP06_09_IMPLEMENT_OPTIONAL_REGION_VALIDATOR_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP06_09_IMPLEMENT_OPTIONAL_REGION_VALIDATOR
STATUS: PASS
MAP06_09: COMPLETE ELIGIBLE
MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS: LOCKED / DO NOT START
SHA-256: 51a6f0dd621db698628ceef6ba7e7f2f18988b213ad564e7b35e00c52041d62a
```

이 별도 patch가 적용된 뒤에만 MAP06_10을 실행한다. MAP07 이후 Task body는 읽거나 시작하지 않는다.

## Approved Input Baseline

```text
World sectors / dimensions: 169 / 13x13
Site reservations / reserved sectors / entries / Core seeds: 7 / 8 / 6 / 4
Biome publication sectors / assigned / reserved-unassigned: 169 / 165 / 4
Mandatory graph nodes / directed / undirected / route cells: 47 / 96 / 48 / 47
Optional regions / Type0 cells: 12 / 39
Type0 attachment base-closed / mandatory base-open / L+R-open: 12 / 0 / 0
Access assignments / visible-perceptible clues: 12 / 12
Reward assignments / Low-Medium-High-Unique: 12 / 5-1-2-4
Mandatory reward assignments: 0
Return assignments / Backtrack-ReturnGate-SafeExit: 12 / 12-0-0
Returnable / non-returnable cells: 39 / 0
Inactive assignments / DecorativeBoundary / InteriorInactive: 78 / 52 / 26
Source ReservedSite / Mandatory / Type0: 8 / 47 / 39
Approved ReservedSite-Mandatory overlap: {0,28,106}
Exclusive ReservedSite / MandatoryOnly / Type0 / Inactive: 8 / 44 / 39 / 78
Protected union / full accounting: 91 / 169
Unassigned / illegal overlap / duplicate / open edge to inactive: 0 / 0 / 0 / 0
Validation status / issues: Valid / 0
RNG draws / source mutation / partial publication: 0 / 0 / 0
Mandatory graph digest: MAP05_GRAPH_47_96_48_47
Growth digest: 1f00f718bdb8d79fbf88923be0d81e0075987267c024cc624086ee6194998caa
Type0 assignment digest: a26e73f25ff7267760a2507ec55b1acda3a6c39a8f3698fc37b190620ceca525
Access assignment digest: 5268b7ed2342f197fb5717c85dbfdb8e51b3c520c217ecf93ad14dc944ecf74f
Reward-tier digest: c3430c42a27937e143fa89c5839282b9533b62d5fb74fb26fdad490cb545958e
Return-policy digest: cff0556a59e66fcc16b886ecf3082779efe9535bb79dcf45b401d12ff0971f6b
Inactive assignment digest: 426f269e39d8a2d75a93020a00c7bb617612c00dd60a663fdbeffc60f8ea9578
Validation digest: 1180f6a784b29739a2ca640d2c45398066ec7e636a8cb69ee307315cc20cc84e
Assets meta: 3311
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

reference는 debug overlay semantics, optional edge terminology, MAP06 phase gate, and future MAP07 handoff를 확인하는 용도다. Authoring CSV body를 runtime source로 다시 파싱하거나 수정하지 않는다. Generated CSV body를 source of truth로 사용하지 않는다.

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
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionValidationReport.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionValidationDiagnostics.cs
```

### Existing diagnostics/editor/tests

```text
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/MandatoryRouteOverlaySnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/MandatoryRouteOverlayCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/MandatoryRouteOverlayBuilder.cs
Assets/_Game/Editor/MapAuthoring/Preview/MandatoryRouteOverlaySceneDrawer.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Diagnostics/MandatoryRouteOverlayTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Preview/MandatoryRouteOverlaySceneDrawerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionValidatorTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

위 파일과 matching meta, approved Runtime/Test `Generation` and `Diagnostics` path-only inventory, Authoring CSV/meta count·aggregate hash, 전체 Assets meta GUID, patch marker 이후 change-scope path만 확인할 수 있다.

금지: installed Authoring CSV body runtime 재파싱·수정, generated CSV body source 사용, MAP07+ Task body, boundary profile/recipe/microchunk/tile/socket/edge body, unrelated production/test body, Legacy/Stage/P6/P11 generator body, Scene/Prefab YAML.

## WRITE ALLOWLIST

### 신규 Runtime diagnostics C# — exact 7

```text
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/OptionalRegionOverlayEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/OptionalRegionOverlaySettings.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/OptionalRegionOverlayCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/OptionalRegionOverlayConnection.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/OptionalRegionOverlayLegendEntry.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/OptionalRegionOverlaySnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/OptionalRegionOverlayBuilder.cs
```

### 신규 Editor preview C# — exact 1

```text
Assets/_Game/Editor/MapAuthoring/Preview/OptionalRegionOverlaySceneDrawer.cs
```

### 신규 EditMode tests — exact 3

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Diagnostics/OptionalRegionOverlayTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Preview/OptionalRegionOverlaySceneDrawerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map06ExitTests.cs
```

### 기존 phase-boundary test 수정 — exact up to 15

MAP06_10 production/editor symbols 허용 및 MAP07+ future symbols 금지 유지를 위해 필요한 경우 아래 existing test C#만 수정할 수 있다. matching `.cs.meta`는 수정하지 않는다.

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
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionValidatorTests.cs
```

신규 C# 11개와 matching `.cs.meta` 11개, 위 boundary test C# 최대 15개 수정, Result 1개만 허용한다. MAP05/MAP06_01~09 production source, world/site/biome/mandatory/Type0/access/reward/return/inactive/validation artifacts, Authoring/generated CSV/meta, asmdef, Scene, Prefab, Packages, ProjectSettings는 수정하지 않는다. 신규 directory/folder meta/asmdef/asmref를 만들지 않는다.

## Namespace / Assembly

```text
Runtime diagnostics namespace: StarNight.Map.WorldGeneration.Diagnostics
Runtime generation namespace:   StarNight.Map.WorldGeneration.Generation
Editor preview namespace:       StarNight.MapAuthoring.Preview
Test namespace:                 StarNight.Map.Tests.WorldGeneration
Runtime assembly:               Game.Map.Runtime
Test assembly:                  Game.Map.Tests.EditMode
```

`UnityEditor`는 Editor preview/test files에서만 허용한다. Runtime diagnostics에는 Unity object/lifecycle, `UnityEditor`, record/record struct, `required`, `init`, nullable-reference directive, reflection factory, singleton/static mutable state를 도입하지 않는다.

## Frozen P04 Boundary

```text
Input artifacts  = MAP06_01~09 APPROVED SOURCE CHAIN + VALIDATION REPORT
Read-only guards = P00/P01/P02 + MAP05 MANDATORY GRAPH IDENTITY
Output artifact  = MAP06_OPTIONAL_REGION_OVERLAY_SNAPSHOT + MAP06_EXIT_AUDIT_RESULT
Pass ID          = PASS_OPTIONAL
RNG consumption  = exact 0
```

output은 immutable debug/exit-test artifact다. source artifacts를 in-place mutate하지 않고 generated CSV, gameplay edge, recipe, socket, tile marker를 만들지 않는다.

## New Runtime Contracts

신규 `OptionalRegionOverlayEnums.cs`:

```text
OptionalRegionOverlayStatus:
  Completed | InvalidInput | InvalidSettings | InvalidSource | InvalidValidationReport

OptionalRegionOverlayLayer:
  BaseRole | AccessRule | Depth | AttachmentContact | ReturnWitness
  RewardTier | InactiveKind | ValidationIssue

OptionalRegionOverlayCellKind:
  Mandatory | ReservedSite | Type0 | InactiveInterior | InactiveDecorative

OptionalRegionOverlayConnectionKind:
  AttachmentContact | ReturnWitness

OptionalRegionOverlayColorToken:
  Mandatory | ReservedSite | Type0Basic | Type0Tool | Type0Environment
  Type0Explosive | Type0Hidden | RewardLow | RewardMedium | RewardHigh
  RewardUnique | ReturnBacktrack | InactiveInterior | InactiveDecorative
  ValidationIssue
```

undefined enum을 거부한다. authoring token codec을 새로 만들지 않는다.

`OptionalRegionOverlaySettings` sealed immutable object:

```text
bool ShowAccessRuleColors
bool ShowDepthLabels
bool ShowAttachmentContacts
bool ShowReturnWitness
bool ShowRewardTierMarkers
bool ShowInactiveKinds
bool ShowValidationIssues
bool RequireValidReport
```

approved settings는 모두 exact `true`다. false는 이번 Task의 frozen contract와 맞지 않으므로 invalid settings다.

`OptionalRegionOverlayCell` sealed immutable object:

```text
int SectorIndex
SectorCoord Coord
OptionalRegionOverlayCellKind Kind
OptionalRegionId RegionId
int Depth
OptionalAccessRule AccessRule
OptionalRewardTier RewardTier
OptionalReturnPolicy ReturnPolicy
InactiveBufferKind InactiveKind
OptionalRegionOverlayColorToken ColorToken
string Label
IReadOnlyList<OptionalRegionOverlayLayer> Layers
```

Type0 cell labels are depth `1..4`. Inactive cells are `D` for DecorativeBoundary and `I` for InteriorInactive. Mandatory/Reserved cells have stable role labels. Collections are copied read-only.

`OptionalRegionOverlayConnection` sealed immutable object:

```text
OptionalRegionOverlayConnectionKind Kind
OptionalRegionId RegionId
int FromSectorIndex
int ToSectorIndex
string Label
OptionalAccessRule AccessRule
OptionalReturnPolicy ReturnPolicy
```

Connections are presentation-only. They do not create BaseEdge, OptionalOverlayEdge, generated_world_edges rows, sockets, recipes, or traversal rules.

`OptionalRegionOverlaySnapshot` sealed immutable properties:

```text
OptionalRegionOverlayStatus Status
IReadOnlyList<OptionalRegionOverlayCell> Cells
IReadOnlyList<OptionalRegionOverlayConnection> Connections
IReadOnlyList<OptionalRegionOverlayLegendEntry> Legend
string SourceValidationDigest
string SourceInactiveDigest
string CanonicalDigest
int RngDrawCount
bool IsSuccess
```

failure is atomic: `IsSuccess=false`, cells/connections/legend empty, canonical digest empty, RNG/source mutation `0/0`, partial publication `0`.

`OptionalRegionOverlayBuilder` is a stateless sealed service:

```text
OptionalRegionOverlaySnapshot Build(
    GeneratedWorldData world,
    OptionalRegionSnapshot optionalRegions,
    Type0RouteMaskAssignmentResult type0Assignments,
    OptionalAccessAssignmentResult accessAssignments,
    OptionalRewardTierResult rewardTiers,
    OptionalReturnPolicyResult returnPolicies,
    InactiveBufferAssignmentResult inactiveBuffers,
    OptionalRegionValidationReport validationReport,
    OptionalRegionOverlaySettings settings)
```

It copies source collections in canonical order, uses fixed direction order `L,R,U,D`, consumes RNG `0`, and is independent of caller order, service reuse, culture, time, and thread scheduling.

## Overlay Publication Rules

1. Overlay `Cells` contains exactly 169 entries in sector-index order.
2. Mandatory, ReservedSite, Type0, InactiveInterior, and InactiveDecorative cell counts are exact `44`, `8`, `39`, `26`, `52` in exclusive projection.
3. Approved reserved adapters `{0,28,106}` render as `ReservedSite` but preserve a stable adapter marker/label.
4. Type0 cell overlays preserve region ID, depth, access rule, reward tier, return policy, and no `L+R` open mask.
5. Attachment contact connections are exact `12`, one per optional region, and point from mandatory attachment source to the region entry/attachment cell. They display access rule color but do not open base route masks.
6. Return witness connections are exact `19` directed witness steps across all regions, matching MAP06_07 critical witness edge total.
7. Validation issue layer is empty for approved fixture; issue overlays render only if validation report is invalid.
8. Legend contains stable entries for five access colors, four reward tiers, return backtrack, inactive interior/decorative, mandatory, reserved, and validation issue.
9. CanonicalDigest is stable SHA-256 over settings, source digests, cells, connections, legend, and diagnostics in UTF-8 ordinal field order.

## Editor Scene Drawer

`OptionalRegionOverlaySceneDrawer` consumes only `OptionalRegionOverlaySnapshot`. It has no source-chain parsing logic and no project mutation side effects.

Required visual semantics:

```text
13x13 cell grid
access color swatch per Type0 cell
depth label centered on Type0 cells
attachment/contact marker on 12 entries
return witness arrow chain for 19 witness edges
reward tier marker
inactive D/I marker
red issue marker only when validation issues exist
legend entries stable ordered
```

Scene drawer tests should assert deterministic draw-command model rather than requiring a live Scene mutation. Game/Scene visual checklist may render a snapshot preview, but Scene/Prefab assets must not be saved or dirtied.

## MAP06 Exit Audit

`Map06ExitTests` must verify:

1. MAP06_01~09 source-chain and digest gates are still exact.
2. Mandatory graph identity and MAP05 Type4 contract are unchanged.
3. Removing all optional overlay connections leaves mandatory completion unchanged.
4. Type0 cells have no L+R base-open mask and no mandatory base-open attachment boundary.
5. Every optional region has access, visible clue, reward tier, return policy, and returnability.
6. Mandatory reward assignments are `0`.
7. Inactive full-world accounting is `169 = 8 + 44 + 39 + 78`.
8. Validation report status is `Valid`, issue count `0`, digest `1180f6...`.
9. Overlay snapshot is deterministic and represents all approved MAP06 facts.
10. No generated CSV, boundary/recipe/microchunk/tile/socket/edge, Scene, Prefab, asmdef, Package, or ProjectSettings changes are introduced.

On PASS Result, record `MAP06 PHASE EXIT: APPROVED`. Do not open MAP07.

## Boundary Test Advance

허용할 MAP06_10 symbols:

```text
OptionalRegionOverlayStatus
OptionalRegionOverlayLayer
OptionalRegionOverlayCellKind
OptionalRegionOverlayConnectionKind
OptionalRegionOverlayColorToken
OptionalRegionOverlaySettings
OptionalRegionOverlayCell
OptionalRegionOverlayConnection
OptionalRegionOverlayLegendEntry
OptionalRegionOverlaySnapshot
OptionalRegionOverlayBuilder
OptionalRegionOverlaySceneDrawer
OptionalRegionOverlayTests
OptionalRegionOverlaySceneDrawerTests
Map06ExitTests
```

계속 금지할 MAP07+ examples:

```text
MicroChunkDefinition
TileLayerRules
MicroChunkTransform
SocketEdgeValidator
ObjectSlotValidator
MicroChunkReachabilityProbe
GeneratedOptionalRegionCsvWriter
SectorRecipeResolver
BoundaryCandidateIndex
GeneratedWorldBundleWriter
```

MAP05 Type4, MAP06_04 Type0 base-closed/L+R, MAP06_05 access/clue, MAP06_06 score/tier, MAP06_07 returnability, MAP06_08 inactive accounting, MAP06_09 validation assertions를 약화하지 않는다. MAP07+ 금지 case 수를 줄이지 않는다.

## Required Tests

New required tests:

```text
OptionalRegionOverlayTests >=180 PASS
OptionalRegionOverlaySceneDrawerTests >=40 PASS
Map06ExitTests >=180 PASS
```

Required total gates:

```text
OptionalRegionOverlayTests >=180 PASS
OptionalRegionOverlaySceneDrawerTests >=40 PASS
Map06ExitTests >=180 PASS
OptionalRegionValidatorTests 321/321 PASS
InactiveBufferAssignerTests 281/281 PASS
OptionalReturnPolicyResolverTests 289/289 PASS
OptionalRewardTierCalculatorTests 279/279 PASS
OptionalAccessRuleAssignerTests 289/289 PASS
Type0RouteMaskAssignerTests 257/257 PASS
OptionalRegionGrowerTests 234/234 PASS
OptionalAttachmentEnumeratorTests 202/202 PASS
OptionalRegionModelsTests 194/194 PASS
Existing MAP05 aggregate 1959/1959 PASS
Actually executed required total >=4705 PASS
Visual checklist Game/Scene >=24/24 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
Assets meta 3311 -> 3322
new C#/meta 11/11
existing boundary test C# modified <=15
Authoring CSV/meta 50/50
Authoring manifest SHA-256 unchanged
duplicate GUID groups 0
MAP05/MAP06_01~09 production/world/site/biome/graph/mask/models/assignments/CSV/asmdef/Scene/Prefab/Packages/ProjectSettings modifications 0
generated CSV files created by this task 0
boundary profile/recipe/microchunk/tile/socket/edge artifacts created 0
```

Discovery-only count나 arithmetic total을 PASS로 기록하지 않는다. 위 total은 Unity EditMode Test Runner에서 실제 실행한 결과여야 한다.

## Result Format

Write exactly `MapDesign/MCP/REPORTS/MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS_RESULT.md`.

Required top lines:

```text
TASK: MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS
STATUS: PASS|FAIL|BLOCKED
MAP06_10: COMPLETE ELIGIBLE|NOT COMPLETE
MAP06 PHASE EXIT: APPROVED|NOT APPROVED
MAP07_01_IMPLEMENT_MICROCHUNK_DEFINITION: LOCKED / DO NOT START
```

Result must include patch/receipt, prior Result and Task SHA gates, created/changed files, source-chain digests, overlay settings, exact overlay cell/connection/legend counts, visual checklist, MAP06 exit audit evidence, MAP05/Type4 and MAP06_01~09 preservation, exact test jobs, Unity gate, asset/meta/CSV/GUID/change-scope gate, and NEXT that finalizes only MAP06_10 while keeping MAP07_01 locked.

## Done Conditions

- [ ] Preconditions, prior Result SHA, current Task SHA verified; MAP06_10 is sole CURRENT.
- [ ] P00/P01/P02/MAP05/MAP06_01~09 source identities and statuses verified.
- [ ] Overlay snapshot contains 169 cells and exact role/kind counts `44/8/39/26/52`.
- [ ] Overlay connections contain 12 attachment contacts and 19 return witness steps.
- [ ] Access colors, depth labels, reward markers, inactive D/I markers, validation issue markers, and legend are deterministic.
- [ ] MAP06 exit audit proves mandatory graph unchanged, Type0 no L+R, returnability `39/0`, mandatory reward `0`, inactive accounting `169`.
- [ ] No generated CSV, boundary profile/recipe/microchunk/tile/socket/edge, Scene, Prefab, asmdef, Package, or ProjectSettings changes are introduced.
- [ ] MAP06_10 symbols allowed; MAP07+ forbidden.
- [ ] Required Unity EditMode and visual gates actually executed and Result evidence complete.

If any required gate fails or Unity MCP is unavailable, write `STATUS: FAIL` or `STATUS: BLOCKED`, keep MAP06_10 CURRENT, do not approve MAP06 exit, and do not create or start MAP07_01.
