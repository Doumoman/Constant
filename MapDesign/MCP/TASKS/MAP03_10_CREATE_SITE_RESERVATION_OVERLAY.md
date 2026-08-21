# MAP03_10 — Create Site Reservation Overlay

```yaml
status_control:
  task_key: MAP03_10_CREATE_SITE_RESERVATION_OVERLAY
  result_file: REPORTS/MAP03_10_CREATE_SITE_RESERVATION_OVERLAY_RESULT.md
```

## TASK TYPE

```text
IMPLEMENTATION + EDITOR/RUNTIME DIAGNOSTIC OVERLAY + EDITMODE TESTS + VISUAL VERIFICATION
```

## Objective

MAP03_09의 successful immutable `SiteReservationPublication`과 MAP03_06/07/08/09의 completed diagnostics를 read-only 169-cell overlay snapshot으로 투영한다.

Game View와 Scene View는 동일한 runtime IMGUI renderer와 동일한 cell layout/hit-test를 사용한다. 화면에는 아래를 동시에 표시한다.

```text
seven site footprints with source-specific color/glyph
final-oriented local footprint cell coordinates
six entry-side arrows on the occupied footprint cells
four disjoint Core witness regions / 20 minimum expected sectors
MAP03_06 search rejection counts
MAP03_08 Village candidate rejection counts
MAP03_07 capacity shortfall and MAP03_09 final validation violations
selected altitude/capacity soft-cost facts, explicitly not mislabeled as rejection
```

logical x는 왼쪽에서 오른쪽 `0..12`, logical y는 아래에서 위 `0..12`다. visual top row는 y=12, bottom row는 y=0이다. overlay는 final publication과 diagnostics를 표시할 뿐 generation/pass/root/retry/RNG를 실행하거나 source data를 수정하지 않는다.

MAP03_05에서 altitude와 future capacity forecast는 ranking용 soft cost이고 hard rejection은 아니다. UI와 tooltip은 이 의미를 exact 유지한다. MAP03_07 witness는 future CorePatch 전체가 아니라 `footprint + mandatory buffer + minimum capacity`를 증명하는 최소 expected region이다.

## 전체 연결

```text
MAP03_06 completed search diagnostics + selected cost breakdowns
    + MAP03_07 completed capacity diagnostics / four witnesses
    + MAP03_08 completed Village diagnostics
    + MAP03_09 completed publication / validation diagnostics
        -> SiteReservationOverlaySnapshot (pure immutable projection)
        -> SiteReservationOverlayGui (shared fixed renderer)
        -> SiteReservationOverlay.OnGUI (Game View)
        -> SiteReservationOverlaySceneDrawer (Scene View)
```

## Mandatory Read Order

아래 순서로 읽는다.

1. `MapDesign/MCP/00_MCP_ENTRYPOINT.md`
2. locked/work/CSV/Unity/change/patch/finalize global rules
3. `MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md`
4. `MapDesign/MCP/06_IMPLEMENTATION_STATUS.md`
5. 이 Task
6. `MapDesign/MCP/REPORTS/MAP03_09_IMPLEMENT_SITE_RESERVATION_VALIDATOR_RESULT.md`

MAP03_09 Result가 exact `STATUS: PASS`가 아니거나 Current Task가 이 Task와 다르면 구현하지 않고 `BLOCKED`다. MAP03_11 Task body는 읽거나 생성하거나 실행하지 않는다.

## Map Package Reference

Map Package v1.0 exact installed tree가 있으면 아래 부분만 읽는다.

```text
01_FIXED_SPEC/01_WORLD_FIXED_SPEC.md             # 13x13 world and role vocabulary only
01_FIXED_SPEC/02_COORDINATE_AND_ID_RULES.md      # origin/direction only
02_PHASE_ROADMAP/MAP03_SPECIAL_SITE_RESERVATION.md # debug screen section only
02_PHASE_ROADMAP/MAP14_EDITOR_AND_DEBUG_TOOLS.md  # legend/non-color identity only
```

exact 문서가 없으면 이 Task의 frozen contract가 authoritative fallback이다. 대체 문서나 Legacy/Stage/P6/P11 generator를 broad search하지 않는다.

## READ ALLOWLIST

### Existing constants / coordinates / final models

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGridIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteFootprint.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteEntryAnchor.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreBiomeSeed.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorReservation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationPublication.cs
```

### Existing diagnostic / source identity APIs

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSearchDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSelectionPlan.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCostBreakdown.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityFloodWitness.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityApproval.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityFloodDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageReservationDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageReservationSelection.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationValidationDiagnostics.cs
```

### Existing shared-overlay precedent / Editor boundary

```text
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/WorldTopologyOverlayCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/WorldTopologyOverlaySnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/WorldTopologyOverlayGui.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/WorldTopologyOverlay.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Preview/WorldTopologyOverlaySceneDrawer.cs
```

### Focused tests / assemblies

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationBacktrackerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/CoreCapacityFloodCheckerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/VillageReservationSelectorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/WorldTopologyOverlayTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Preview/WorldTopologyOverlaySceneDrawerTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Editor/MapAuthoring/MapAuthoring.Editor.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/MapAuthoring.Tests.EditMode.asmdef
```

approved Runtime `Diagnostics`, Editor `Preview`, Runtime test `Generation`, Editor test `Preview` 폴더의 `rg --files` path-only inventory는 허용한다. content search는 위 exact files와 아래 WRITE ALLOWLIST의 생성 후 파일로만 한정한다. 다른 body를 출력하는 broad recursive search, MAP03_11 이후 Task body, Legacy generator, Authoring CSV body, Scene/Prefab YAML은 읽거나 사용하지 않는다.

## WRITE ALLOWLIST

Runtime C# 신규 exact 4:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/SiteReservationOverlayCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/SiteReservationOverlaySnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/SiteReservationOverlayGui.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/SiteReservationOverlay.cs
```

Editor C# 신규 exact 1:

```text
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Preview/SiteReservationOverlaySceneDrawer.cs
```

Runtime EditMode test C# 신규 exact 1:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationOverlayTests.cs
```

Editor EditMode test C# 신규 exact 1:

```text
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Preview/SiteReservationOverlaySceneDrawerTests.cs
```

신규 C# `7` + matching `.cs.meta` `7` + Result `1`만 허용한다. existing C#/test/meta/asmdef/asmref 수정은 exact `0`이다. 모두 existing approved folders에 두며 새 directory/folder meta를 만들지 않는다.

## Namespace / Assembly

```text
Runtime namespace:      StarNight.Map.WorldGeneration.Diagnostics
Editor namespace:       StarNight.MapAuthoring.Editor.WorldGeneration.Preview
Runtime test namespace: StarNight.Map.Tests.WorldGeneration.Generation
Editor test namespace:  StarNight.MapAuthoring.Editor.Tests.WorldGeneration.Preview
Runtime assembly:       Game.Map.Runtime
Editor assembly:        MapAuthoring.Editor
Runtime test assembly:  Game.Map.Tests.EditMode
Editor test assembly:   MapAuthoring.Tests.EditMode
```

Runtime assembly에 `UnityEditor` reference를 추가하지 않는다. record/record struct, `required`, `init`, nullable-reference directive, reflection factory, singleton/static mutable cache를 도입하지 않는다.

## `SiteReservationOverlayCell` Contract

sealed immutable view value object가 exact 아래 정보를 가진다.

```text
int Index
SectorCoord Coordinate
bool IsReserved
SiteReservationId? ReservationId
SiteReservationKind? Kind
string SourceDefinitionId
int LocalX
int LocalY
string LocalRole
bool IsCoreWitness
SiteReservationId? CoreWitnessOwnerId
IReadOnlyList<SiteEntrySide> EntrySides
string SiteGlyph
string CellLabel
string Tooltip
```

- source는 final `SiteReservationSnapshot.GetSector(index)`, matching `SiteReservation`, flattened entries, MAP03_07 witness ownership이다.
- index/coordinate/reservation/local/entry/witness identity를 다시 추측하거나 repair하지 않는다. 불일치는 snapshot construction을 거부한다.
- entries는 L/R/U/D enum order copied read-only unique list다. 같은 footprint cell의 multiple side는 표현 가능하다.
- final four witness sets는 pairwise disjoint이므로 한 cell의 Core owner는 최대 하나다. overlap은 숨기거나 색을 blend하지 않고 거부한다.
- reserved footprint가 own Core witness 안에 있으면 `IsReserved`와 `IsCoreWitness`가 동시에 true다.
- unreserved non-witness는 null IDs/kind, empty source/role, local `-1/-1`, empty entry list다.
- local coordinate는 final-oriented footprint coordinate이며 source transform을 다시 적용하지 않는다.

exact source glyph mapping:

```text
WORLD_MOONPALACE_V1      -> A
SITE_MOON_BOSS_VAULT     -> B
SITE_MOON_SEAL_FORGE     -> F
SITE_CASSIA_SAP_HEART    -> C
SITE_DEEP_STAR_YEAST     -> Y
SITE_MOON_CORE_METEOR    -> M
SITE_PRIMARY_VILLAGE     -> V
unreserved               -> empty
```

unknown/duplicate/missing required source는 fallback kind glyph로 숨기지 않고 `ArgumentException`으로 거부한다.

cell label exact:

```text
reserved:     {glyph}\n{localX},{localY}
witness only: +
empty:        empty string
```

tooltip은 exact six lines, invariant culture, trailing newline 없음이다.

```text
Sector: SectorCoord(6, 6) / Index 84
Reservation: RSV_02_SITE_MOON_SEAL_FORGE
Source/Kind: SITE_MOON_SEAL_FORGE / FORGE
Local: 0,0 / Role FORGE
Entry: R
Core Witness: RSV_02_SITE_MOON_SEAL_FORGE
```

none 값은 exact `NONE`, multiple entry는 `L|R|U|D` canonical token이다. tooltip/label/arrow/glyph로 color와 무관하게 identity를 판독할 수 있어야 한다.

## Diagnostic Row Contract

`SiteReservationOverlayCell.cs` 파일에는 immutable `SiteReservationOverlayDiagnosticRow`와 아래 exact enum을 함께 둔다.

```text
SiteReservationOverlayDiagnosticClass:
CandidateRejection
FinalGate
SoftCost

SiteReservationOverlayDiagnosticKind exact order:
SearchFootprintOverlap
SearchBlocksExistingEntryApproach
SearchEntryApproachOccupied
SearchDistanceConstraint
SearchCoreCluster
VillageEntryOutsideWorld
VillageFootprintOverlap
VillageProtectedCoreWitness
VillageBlocksExistingEntryApproach
VillageEntryApproachOccupied
VillageOtherSiteDistance
VillageStartBucketDistance
CapacityShortfall
ValidationViolations
SelectedAltitudeSoftUnits
SelectedCapacityForecastSoftUnits
```

row immutable fields:

```text
SiteReservationOverlayDiagnosticKind Kind
SiteReservationOverlayDiagnosticClass Class
string Key
string Label
long Value
```

- exact `16` rows를 enum order로 항상 만든다.
- search 5개는 six `SiteReservationGroupDiagnostics.GetReasonCount` 합이다.
- Village 7개는 all `VillageLayoutCandidateDiagnostics`의 matching count 합이다.
- `CapacityShortfall`은 four `CoreCapacitySiteDiagnostics.CapacityShortfall` checked sum이다.
- `ValidationViolations`는 MAP03_09 diagnostics exact count다.
- selected soft rows는 six selection step `IncrementalCost.AltitudeUnits`, `FutureCoreCapacityUnits` checked sum이다.
- first 12는 `CandidateRejection`, next 2는 `FinalGate`, last 2는 `SoftCost`다.
- UI label은 soft rows에 exact suffix `(SOFT COST, NOT REJECTION)`을 포함한다. altitude/forecast를 탈락 수로 바꾸지 않는다.
- negative count, wrong class/key/order, overflow를 거부한다.

## `SiteReservationOverlaySnapshot` Contract

sealed immutable snapshot public surface:

```text
ulong Seed
IReadOnlyList<SiteReservationOverlayCell> Cells
IReadOnlyList<SiteReservationOverlayDiagnosticRow> DiagnosticRows
int Count
int ReservationCount
int ReservedSectorCount
int EntryArrowCount
int CoreWitnessCount
int CoreWitnessSectorCount
int PassedValidationRuleCount

SiteReservationOverlayCell GetCell(int index)
SiteReservationOverlayCell GetCell(SectorCoord coordinate)
bool TryGetCell(int index, out SiteReservationOverlayCell cell)

static SiteReservationOverlaySnapshot Create(
    SiteReservationPublication publication,
    SiteReservationSearchDiagnostics searchDiagnostics,
    CoreCapacityFloodDiagnostics capacityDiagnostics,
    VillageReservationDiagnostics villageDiagnostics,
    SiteReservationValidationDiagnostics validationDiagnostics)
```

all five arguments are required and must represent the same completed attempt.

- publication은 exact seven reservations, 169 sector rows, six entries, four Core seeds와 non-null source approval을 가진 MAP03_09 completed publication이다.
- source approval plan은 exact six steps, capacity approval exact four witnesses, Village exact one selection이다.
- search groups/selected plan key set, capacity sites/witnesses, Village diagnostics, validation counts가 publication/source approval과 exact 일치해야 한다.
- diagnostics가 identity를 직접 보관하지 않는 count는 authoritative source collections로 independently recompute해 cross-check한다.
- validation `6/6 PASS`, violations `0`, reservation/reserved/unreserved/entry/witness/Core seed counts가 publication과 일치해야 한다.
- cells는 index `0..168` 오름차순으로 독립 copied read-only snapshot이다.
- `GetCell(SectorCoord)`는 existing `WorldGridIndex.ToIndex`를 사용한다. formula를 복제하지 않는다.
- invalid Get은 `ArgumentOutOfRangeException`, invalid Try는 false/null이다.
- source/caller collections를 mutate하거나 lazy enumeration/public mutable list를 노출하지 않는다.

starter exact snapshot:

```text
cells = 169
reservations = 7
reserved/unreserved = 8/161
entry arrows = 6
Core witnesses/regions = 4/20
passed validation rules = 6
diagnostic rows = 16
```

## Core Expected Region Contract

- renderer가 표시하는 expected Core region은 exact MAP03_07 `WitnessSectorIndices`다.
- owner는 witness `Key.SourceDefinitionId`를 `publication.TryGetReservationBySourceId`로 final reservation에 연결한다.
- Forge/Cassia/Yeast/Meteor exact four owners이고 target count `5/5/5/5`, union exact `20`, cross overlap `0`이다.
- witness full set을 표시하며 footprint만 또는 reachable entire component로 대체하지 않는다.
- witness unreserved cell은 owner color의 translucent fill + inset border + `+` label이다.
- witness reserved own footprint는 site fill을 유지하고 same owner inset border를 추가한다.
- legend는 exact `Core outline = minimum expected witness, not painted biome` 문구를 표시한다.
- future MAP04 patch ownership/biome fill/growth result를 추론하거나 저장하지 않는다.

## Frozen Site Colors / Non-Color Identity

exact `Color32` mapping:

```text
Unreserved                  = (60, 60, 68, 220)
WORLD_MOONPALACE_V1         = (40, 170, 240, 235)
SITE_MOON_BOSS_VAULT        = (220, 70, 70, 235)
SITE_MOON_SEAL_FORGE        = (240, 145, 45, 235)
SITE_CASSIA_SAP_HEART       = (70, 185, 105, 235)
SITE_DEEP_STAR_YEAST        = (235, 205, 70, 235)
SITE_MOON_CORE_METEOR       = (155, 95, 220, 235)
SITE_PRIMARY_VILLAGE        = (65, 125, 235, 235)
Core witness unreserved alpha = 72 using owner RGB
Core witness outline alpha    = 255 using owner RGB
```

color 외에 source glyph, local coordinate, entry arrow, tooltip, fixed legend를 항상 병기한다. undefined source를 black/gray fallback으로 표시하지 않는다.

## Frozen Overlay Layout

`SiteReservationOverlayGui`는 runtime static stateless renderer다. Game/Scene View는 이 class의 동일 public `Draw` method와 layout/hit-test를 사용한다.

constants:

```text
PanelOrigin        = (12, 12) GUI pixels
PanelPixelSize     = 1000 x 760
TitleOrigin        = (24, 22)
GridOrigin         = (24, 56)
CellSize           = 44 GUI pixels
GridColumns/Rows   = 13/13
GridPixelSize      = 572 x 572
SidebarOrigin      = (608, 56)
SidebarPixelSize   = 392 x 704
TooltipOrigin      = (24, 640)
TooltipPixelSize   = 572 x 120
RequiredViewport   = 1024 x 784
Title              = MAP03 SITE RESERVATION / Seed {seed invariant}
EmptyHoverText     = Hover a sector for reservation details.
```

- visual top-left cell은 `(0,12)`, top-right `(12,12)`, bottom-left `(0,0)`, bottom-right `(12,0)`다.
- snapshot order는 index `0..168`; rect 계산만 `visualRow = 12-y`다.
- hit-test는 left/top inclusive, right/bottom exclusive이며 grid 밖을 clamp/wrap하지 않는다.
- draw order는 panel/title -> 169 base fills -> labels -> Core witness inset outlines -> entry arrows -> hover outline -> sidebar legend/summary/16 diagnostics -> tooltip이다.
- all 169 cells를 한 panel에 표시하며 reserved local cell labels는 항상 보인다.
- entry arrow token은 exact `L:<`, `R:>`, `U:^`, `D:v`; arrow는 footprint cell의 matching edge에 표시한다.
- same cell multiple arrows는 L/R/U/D order로 각각 해당 edge에 표시한다.
- sidebar fixed legend는 seven source glyph/color rows, Core witness meaning, arrow tokens, `Candidate rejection / Final gate / Soft cost` class meaning을 포함한다.
- summary는 exact reservation/reserved/entry/witness/rule counts를 표시한다.
- 16 diagnostic rows를 exact order/label/value로 표시하며 nonzero candidate rejections는 final snapshot 실패로 오해시키지 않는다.
- viewport가 작으면 data 일부를 다른 좌표처럼 표시하지 않고 exact `Site reservation overlay requires 1024 x 784 pixels.` 안내만 표시한다.
- `GUI.color`, `GUI.backgroundColor`, `GUI.contentColor`, `GUI.enabled`, matrix 등 변경한 global state는 `try/finally`로 exact 복원한다.
- caller-owned `GUIStyle`을 mutate하지 않는다. persistent texture/material/font/style/cache를 만들지 않는다.
- Root/pass/RNG/file I/O, UnityEditor, Camera/Transform, singleton/current-world discovery를 참조하지 않는다.

public pure helpers는 최소 아래를 제공한다.

```text
Rect GetCellRect(SectorCoord coordinate)
bool TryHitTest(SiteReservationOverlaySnapshot snapshot,
    Vector2 mousePosition, out SiteReservationOverlayCell cell)
Color32 GetSiteColor(string sourceDefinitionId)
string GetEntryArrowToken(SiteEntrySide side)
```

undefined source/side와 out-of-grid coordinate는 거부하며 layout helpers가 y를 transpose/flip해 data identity를 바꾸지 않는다.

## Runtime `SiteReservationOverlay` Component

exact type/attributes:

```text
StarNight.Map.WorldGeneration.Diagnostics.SiteReservationOverlay

[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("WorldGen/Site Reservation Overlay")]
```

public surface:

```text
bool HasSnapshot
SiteReservationOverlaySnapshot Snapshot

void SetSnapshot(
    SiteReservationPublication publication,
    SiteReservationSearchDiagnostics searchDiagnostics,
    CoreCapacityFloodDiagnostics capacityDiagnostics,
    VillageReservationDiagnostics villageDiagnostics,
    SiteReservationValidationDiagnostics validationDiagnostics)

void ClearSnapshot()
```

- snapshot field는 `[NonSerialized]`이고 Scene/Prefab/asset에 generated data를 저장하지 않는다.
- `SetSnapshot`은 `Create` success 뒤에만 이전 snapshot을 교체하는 transactional method다. invalid/null input 실패는 이전 snapshot을 보존한다.
- initial/Clear는 null/false다.
- `OnGUI`는 enabled/active, snapshot present, `Event.current` present일 때 shared renderer exact once를 호출한다.
- generation pipeline, validator, root/pass/retry/RNG를 자동 호출하지 않는다.
- `Awake`, `OnEnable`, `Update`, `LateUpdate`, coroutine polling, object discovery, file I/O/log spam이 없다.
- Camera/Canvas/UI document/GameObject를 생성·탐색하거나 selection/transform/timeScale을 바꾸지 않는다.

## Scene Drawer / Custom Inspector Contract

`SiteReservationOverlaySceneDrawer.cs` 하나에 아래 두 type을 둔다.

```text
public static class SiteReservationOverlaySceneDrawer
internal sealed class SiteReservationOverlayEditor : UnityEditor.Editor
```

Scene drawer:

- exact `[DrawGizmo(GizmoType.Active | GizmoType.Selected | GizmoType.NonSelected)]` static method 하나로 `SiteReservationOverlay`를 받는다.
- component active/enabled/snapshot present일 때 `Handles.BeginGUI/EndGUI` 사이에서 runtime shared `Draw` exact once를 호출한다.
- `EndGUI`는 exception에도 `finally`로 실행한다.
- `SceneView.duringSceneGui`, `EditorApplication.update`, `[InitializeOnLoad]`, auto object/window creation, selection requirement, continuous repaint를 사용하지 않는다.
- Scene camera/selection/gizmo state를 변경하지 않는다.

Custom inspector:

- exact `[CustomEditor(typeof(SiteReservationOverlay))]`다.
- snapshot injection은 caller-owned completed generation result로 `SetSnapshot(...)`을 호출해야 한다는 안내문을 표시한다.
- snapshot present이면 seed, `7 / 8 / 6 / 4 / 20 / 6` summary와 16 diagnostic rows를 read-only 표시한다.
- exact `Clear` button 하나만 제공하고, click success 시 `ClearSnapshot`, `SceneView.RepaintAll`, `EditorApplication.QueuePlayerLoopUpdate`를 각각 once 호출한다.
- preview/generation/pass/root/RNG button을 만들지 않는다.
- `Undo`, `EditorUtility.SetDirty`, serialized property, Scene dirty/save를 사용하지 않는다.

## No Ownership / Data Mutation

- projection/rendering은 publication, snapshot, reservations, entries, witnesses, diagnostics, source approval/plan/selection을 mutate하지 않는다.
- diagnostic count를 바꾸거나 rejected candidate를 재선택하지 않는다.
- selected altitude/capacity soft cost는 표시 facts이며 hard rejection으로 승격하지 않는다.
- Core witness는 expected minimum outline이며 biome/patch owner/patch ID를 쓰지 않는다.
- overlay는 Authoring/generated CSV, seed manifest, replay bundle을 읽거나 쓰지 않는다.
- static mutable snapshot/style/texture/dictionary, service locator, global current publication을 만들지 않는다.

## Baseline / Meta Stability

MAP03_09 PASS 이후 clean baseline:

```text
SiteReservationValidator focused: 268/268
VillageReservationSelector focused: 339/339
CoreCapacityFloodChecker focused: 215/215
SiteReservationBacktracker focused: 248/248
SiteCandidateCost focused: 270/270
SiteDistanceIndex focused: 239/239
FootprintPlacementSolver focused: 170/170
SiteCandidateEnumeration focused: 268/268
SiteReservationModels focused: 81/81
MAP02 phase aggregate: 667/667
SpecialVillage/BiomeBoundary/StaticRegistry/ContentHash: 57/57 / 38/38 / 53/53 / 54/54
Targeted EditMode: 3612/3612
Full EditMode: 3652/3652
Authoring CSV/meta: 50/50
Assets meta: 3063
accepted legacy Editor folder meta: 6/6
duplicate GUID groups: 0
```

new matching meta `7` 반영 clean final Assets meta는 exact `3070`이다. 새 directory/folder meta expected `0`이다.

## DO NOT

- existing Domain/Generation/Diagnostics/Editor/test/asmdef 수정 금지
- automatic generation, `SiteReservationValidator` call, root/pass/retry, RNG stream create/read/consume 금지
- MAP03_06/07/08 selection/capacity/Village logic 복제·수정·repair 금지
- diagnostic count로 alternative candidate를 만들거나 selected candidate를 이동시키지 말 것
- altitude/future capacity forecast를 hard rejection으로 표시하지 말 것
- Core witness를 full reachable component나 future painted biome으로 표시하지 말 것
- 13×13 data y flip/clamp/wrap/transpose/index reorder 금지
- color-only identity, enum `ToString`/case-conversion 기반 token 생성 금지
- MAP03_11 100,000-seed batch/statistics/exit gate 선행 구현 금지
- MAP04 biome patch owner/growth/painting/overlay 선행 구현 금지
- Canvas/Camera/RenderTexture/new Texture2D/Material/Shader/font asset 생성 금지
- permanent Scene object, hidden saved object, Prefab/Scene 저장 금지
- automatic EditorWindow, Scene callback subscription, constant repaint/polling 금지
- exception swallow, invalid source repair, test skip/ignore/assertion 완화 금지
- new directory/folder meta/asmdef/asmref, Authoring/generated CSV/meta/Scene/Prefab/Package/ProjectSettings/Git 변경 금지

## Tests / Verification

new focused actual NUnit case minimum:

```text
SiteReservationOverlayTests: >=100 PASS
SiteReservationOverlaySceneDrawerTests: >=20 PASS
Combined new overlay focused: >=120 PASS
```

minimum coverage:

- null/mismatched publication and four diagnostics transactional rejection
- exact starter seed/count/source order and all 169 index/coordinate lookups
- all 8 reserved cells exact ID/source/kind/local/role/glyph/color
- all 161 unreserved cell defaults
- all six entry arrows at exact footprint cell and side; exterior identity unchanged
- four witness owners, exact `5/5/5/5`, union 20, overlap 0
- footprint+witness dual-layer and unreserved witness-only representation
- exact seven source glyph/color mappings and unknown source rejection
- exact labels/tooltips including Start, entry site, witness-only, empty cell
- exact 16 diagnostic row order/class/key/label/value
- search five reason aggregation across six groups
- Village seven reason aggregation across all layouts
- capacity shortfall/validation violation final-gate rows
- selected altitude/capacity soft units and exact NOT REJECTION label
- immutable defensive copy/public mutation-surface audit
- panel/grid/sidebar/tooltip dimensions and all 169 rects
- visual four corners and y orientation
- hit-test all cell centers plus left/top inclusive, right/bottom exclusive/outside rejection
- arrow token/edge order and multiple-arrow synthetic case
- GUI global-state restoration through exception path
- component exact attributes/public surface/initial/transactional set/clear/no auto generation
- Scene drawer exact target/mask/static method/shared draw entrypoint
- custom inspector target, read-only summary, exact Clear-only action, no dirty/save/polling
- production dependency audit for UnityEditor in runtime, RNG, root/pass, time, file I/O, static mutable cache `0`
- existing regression and exact meta/change gates

regression gates:

```text
MAP03_09 SiteReservationValidatorTests: 268/268 PASS
MAP03_08 VillageReservationSelectorTests: 339/339 PASS
MAP03_07 CoreCapacityFloodCheckerTests: 215/215 PASS
MAP03_06 SiteReservationBacktrackerTests: 248/248 PASS
MAP03_05 SiteCandidateCostTests: 270/270 PASS
MAP03_04 SiteDistanceIndexTests: 239/239 PASS
MAP03_03 FootprintPlacementSolverTests: 170/170 PASS
MAP03_02 SiteCandidateEnumerationTests: 268/268 PASS
MAP03_01 SiteReservationModelsTests: 81/81 PASS
MAP02 phase focused aggregate: 667/667 PASS
SpecialVillage/BiomeBoundary/StaticRegistry/ContentHash: 57/57 / 38/38 / 53/53 / 54/54 PASS
Previous targeted baseline: 3612/3612 PASS
Targeted Game.Map total: >=3712 PASS
Previous full baseline: 3652/3652 PASS
Full project EditMode: >=3772 PASS
failed/skipped = 0/0
Unity 6000.3.8f1 / forced refresh / compile error 0 / relevant warning 0
PlayMode NOT RUN / Scene-Prefab saved changes NONE
```

## Visual Verification

Unity MCP/Editor에서 충분한 크기의 Scene View와 Game View로 직접 검증한다. visual fixture는 `HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild` transient object + `SiteReservationOverlay`만 사용한다. existing public pipeline/test fixture와 typed definitions로 seed `4660` completed publication 및 same-attempt diagnostics를 만든 뒤 `SetSnapshot`한다. production overlay가 pipeline을 자동 호출하게 만들지 않는다.

exact visual checklist `18/18`:

1. Game/Scene title이 모두 exact `MAP03 SITE RESERVATION / Seed 4660`이다.
2. 두 View 모두 13 columns x 13 rows, total 169 cells가 한 panel에 전부 보인다.
3. top-left `(0,12)`, top-right `(12,12)`, bottom-left `(0,0)`, bottom-right `(12,0)` orientation이 일치한다.
4. seven source colors와 `A/B/F/C/Y/M/V` glyph legend가 모두 보인다.
5. reserved eight footprint cells에 final-oriented local `x,y`가 보인다.
6. exact six entry arrows가 matching footprint edge에 보인다.
7. four Core witness owner outlines가 exact `5/5/5/5`, union `20`으로 보인다.
8. Core footprint cells는 fill+outline, unreserved witness cells는 translucent+`+`로 구분된다.
9. legend가 witness를 minimum expected region이며 painted biome이 아니라고 표시한다.
10. summary가 exact `7 reservations / 8 reserved / 6 entries / 4 witnesses / 20 sectors / 6 rules`다.
11. 16 diagnostic rows가 CandidateRejection/FinalGate/SoftCost class와 exact order로 보인다.
12. altitude/capacity soft row에 `(SOFT COST, NOT REJECTION)`이 잘리지 않고 보인다.
13. Start hover가 reservation/source/kind/local/Core none을 정확히 표시한다.
14. entry site hover가 exact side token과 reservation/source/local 정보를 표시한다.
15. witness-only hover가 reservation none + exact Core owner를 표시한다.
16. grid 밖 hover는 exact empty text이며 nearest cell로 clamp되지 않는다.
17. Game/Scene data/hover가 same snapshot으로 일치하고 selection/camera/transform/timeScale을 바꾸지 않는다.
18. Clear/temporary object removal 뒤 overlay와 hierarchy residue가 없고 Scene/Prefab dirty-state delta가 없다.

Game/Scene captures, Start/entry/witness/outside hover captures와 visual `18/18` evidence가 없으면 automated tests PASS여도 `BLOCKED`다. 작업 전 Scene이 dirty하면 저장/정리하지 않고 exact before/after 상태가 같음을 기록한다.

## Asset / Meta / Change Gate

완료 시 exact:

```text
new Runtime production C# = 4
new Editor production C# = 1
new Runtime test C# = 1
new Editor test C# = 1
new matching cs.meta = 7
final Assets meta = 3070
task marker 이후 exact Assets changes = 14
existing Assets modifications = 0
unexpected Assets changes = 0
new directory/folder meta = 0
Authoring CSV/meta = 50/50 unchanged
accepted legacy Editor folder meta = 6/6 unchanged
duplicate GUID groups = 0
```

신규 meta는 `fileFormatVersion: 2`, `MonoImporter`, lowercase non-zero unique 32-hex GUID다. invalid/zero/duplicate GUID, destination collision, prior Task asset modification이 있으면 PASS하지 않는다.

## Collision / Failure Policy

1. 신규 destination이 없으면 생성한다.
2. 동일 경로가 exact 계약과 바이트 동일하면 `PREEXISTING_IDENTICAL`로 기록하고 재사용할 수 있다.
3. 다르면 덮어쓰기·병합·삭제하지 않고 `STATUS: BLOCKED`다.
4. existing `.meta`와 user changes를 보존한다.
5. compile/test/visual/meta/change-scope 중 하나라도 불일치하면 `FAIL` 또는 환경 접근 불가 시 `BLOCKED`다.
6. PASS가 아니면 STATUS FINALIZE를 수행하지 않고 MAP03_11을 열지 않는다.

## Exact Change Budget

```text
CREATE  Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/SiteReservationOverlayCell.cs
CREATE  Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/SiteReservationOverlayCell.cs.meta
CREATE  Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/SiteReservationOverlaySnapshot.cs
CREATE  Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/SiteReservationOverlaySnapshot.cs.meta
CREATE  Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/SiteReservationOverlayGui.cs
CREATE  Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/SiteReservationOverlayGui.cs.meta
CREATE  Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/SiteReservationOverlay.cs
CREATE  Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/SiteReservationOverlay.cs.meta
CREATE  Assets/_Game/Editor/MapAuthoring/WorldGeneration/Preview/SiteReservationOverlaySceneDrawer.cs
CREATE  Assets/_Game/Editor/MapAuthoring/WorldGeneration/Preview/SiteReservationOverlaySceneDrawer.cs.meta
CREATE  Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationOverlayTests.cs
CREATE  Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationOverlayTests.cs.meta
CREATE  Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Preview/SiteReservationOverlaySceneDrawerTests.cs
CREATE  Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Preview/SiteReservationOverlaySceneDrawerTests.cs.meta
CREATE  MapDesign/MCP/REPORTS/MAP03_10_CREATE_SITE_RESERVATION_OVERLAY_RESULT.md
```

그 외 CREATE/MODIFY/DELETE는 `0`이다. captures under project `Temp`는 transient verification evidence이며 Assets/source deliverable이 아니고 final cleanup에서 hierarchy/object residue만 제거한다.

## Result Contract

Result exact path:

```text
MapDesign/MCP/REPORTS/MAP03_10_CREATE_SITE_RESERVATION_OVERLAY_RESULT.md
```

required sections:

```text
TASK
STATUS
SUMMARY
PATCH APPLY
READ
MASTER BACKLOG CHECK
MAP03_09 GATE CHECK
CREATED
MODIFIED
PREEXISTING_IDENTICAL
OVERLAY CELL
DIAGNOSTIC ROWS
SNAPSHOT
CORE EXPECTED REGION
SHARED GUI
GAME VIEW COMPONENT
SCENE DRAWER
CUSTOM INSPECTOR
TEST
VISUAL VERIFICATION
UNITY
ASSET META VALIDATION
CHANGE SCOPE
OUT_OF_SCOPE_FINDINGS
DONE CONDITIONS
NEXT
Recommended Commit
```

모든 contract/regression/visual/meta/change gate가 PASS일 때만 MAP03_10 COMPLETE, Current Task NONE으로 finalize한다. `MAP03_11_MAP03_BATCH_AND_EXIT_TESTS`는 LOCKED로 유지하고 자동 시작하지 않는다.

Recommended Commit: `feat(map): add site reservation overlay`
