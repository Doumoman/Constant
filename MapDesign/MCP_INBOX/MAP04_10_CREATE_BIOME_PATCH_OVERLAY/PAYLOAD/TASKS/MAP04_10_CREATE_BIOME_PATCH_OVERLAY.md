# MAP04_10 — Create Biome Patch Overlay

```yaml
status_control:
  task_key: MAP04_10_CREATE_BIOME_PATCH_OVERLAY
  result_file: REPORTS/MAP04_10_CREATE_BIOME_PATCH_OVERLAY_RESULT.md
```

## Goal

MAP04_09 `Completed` publication을 immutable 169-cell snapshot으로 투영하고 Game View와 Scene View에 동일한 renderer로 표시한다. biome 색, PatchId 경계, Core/Satellite/Intrusion 역할, site/seed, patch size·perimeter·compactness를 색 이외의 glyph/label/tooltip과 함께 표시한다.

overlay는 generation, validation, repair, retry, RNG, file I/O를 실행하거나 source를 mutate하지 않는다. MAP04_11 batch/exit은 범위 밖이다.

## Prior Gate / Read

control → Master/Status → 이 Task → MAP04_09 Result 순으로 읽는다.

```text
Prior Result SHA-256 13cf132ed6fc3f10e2159352da64b1e9a8cde52fbae4c0918c78385e7a12dcb1
STATUS PASS; rules 15/15; violations/errors 0/0
focused/regressions/actual 196/248/444; failed/skipped 0/0
patches 17 = Core 4 / Satellite 10 / Intrusion 3
assigned/unassigned 165/4; RNG/mutation 0/0; Assets meta 3140
```

Result 또는 Current Task가 다르면 `BLOCKED`. Result의 `MAP04_10_GENERATE...`는 typo며 205-row Master의 canonical key `MAP04_10_CREATE_BIOME_PATCH_OVERLAY`를 사용한다.

Read body allowlist:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGridIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchRole.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchSeed.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomeSectorOwnership.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchSiteBinding.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatch.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchSnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedBiomePatchRow.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchExportPublication.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchValidationRule.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchValidationDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchValidationPublication.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchValidationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/WorldTopologyOverlayGui.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/SiteReservationOverlayCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/SiteReservationOverlaySnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/SiteReservationOverlayGui.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/SiteReservationOverlay.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Preview/SiteReservationOverlaySceneDrawer.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/BiomePatchValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/BiomePatchExporterTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/BiomePatchModelsTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Editor/MapAuthoring/MapAuthoring.Editor.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/MapAuthoring.Tests.EditMode.asmdef
```

Map reference는 `MAP04_BIOME_PATCH_GENERATOR.md` debug section과 `MAP14_EDITOR_AND_DEBUG_TOOLS.md` non-color identity만 읽는다. matching meta/inventory/hash/scope는 읽을 수 있다. Authoring CSV body, future Task, Legacy, Scene/Prefab YAML은 금지한다.

## Write Allowlist

```text
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/BiomePatchOverlayCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/BiomePatchOverlaySnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/BiomePatchOverlayGui.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/BiomePatchOverlay.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Preview/BiomePatchOverlaySceneDrawer.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/BiomePatchOverlayTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Preview/BiomePatchOverlaySceneDrawerTests.cs
```

exact 7 C# + matching meta 7 + Result만 생성한다. existing C#/meta/asmdef/CSV 수정, 신규 폴더/meta는 0이다.

Namespaces/assemblies:

```text
Runtime      StarNight.Map.WorldGeneration.Diagnostics / Game.Map.Runtime
Editor       StarNight.MapAuthoring.Editor.WorldGeneration.Preview / MapAuthoring.Editor
Runtime test StarNight.Map.Tests.WorldGeneration.Generation / Game.Map.Tests.EditMode
Editor test  StarNight.MapAuthoring.Editor.Tests.WorldGeneration.Preview / MapAuthoring.Tests.EditMode
```

Runtime의 UnityEditor, reflection, static mutable cache, persistent texture/material/font, nullable/record/required/init 도입을 금지한다.

## Cell / Patch Summary

`BiomePatchOverlayCell`은 sealed immutable projection이며 exact fields를 가진다.

```text
Index, Coordinate, IsAssigned
PrimaryBiomeId, PatchId?, Role?
PatchSize, Perimeter, CompactnessPermille
IsSeed, IsCoreSiteCell
BorderLeft, BorderRight, BorderUp, BorderDown
RoleToken, RoleGlyph, CellLabel, Tooltip
```

- source ownership/snapshot/export/publication을 index `0..168`로 cross-check한다. repair, y flip, inferred owner를 금지한다.
- boundary는 world outside 또는 cardinal neighbor의 PatchId가 다른 edge다.
- perimeter는 patch boundary edge 합이며 exported row와 exact 일치해야 한다.
- `CompactnessPermille = floor(16000 * size / (perimeter * perimeter))`, checked integer, range `1..1000`.
- role token/glyph: `CORE/C`, `SATELLITE/S`, `INTRUSION/I`; undefined role는 거부한다.
- assigned label: `{x},{y}\n{glyph}{marker}`. marker는 Core site `*`, 그 외 seed `+`, 그 외 empty. unassigned는 `{x},{y}\n--`.
- tooltip exact 7 lines: Sector/index, Biome, PatchId, Role, size/perimeter/compactness, Seed/CoreSite yes-no, boundary L/R/U/D tokens. invariant culture/trailing newline 0.

`BiomePatchOverlayPatchRow`는 동일 file의 immutable type으로 PatchId, BiomeId, Role, Size, Perimeter, CompactnessPermille, SeedCount, CoreSiteCellCount를 가지며 PatchId ordinal이다.

## Snapshot

```text
ulong WorldSeed
IReadOnlyList<BiomePatchOverlayCell> Cells
IReadOnlyList<BiomePatchOverlayPatchRow> Patches
int AssignedCount, UnassignedCount
int CoreCount, SatelliteCount, IntrusionCount
int PassedValidationRuleCount
GetCell(index/coord), TryGetCell(index)
static Create(BiomePatchValidationPublication publication)
```

- non-null approved publication, validation `15/15`, violations `0`, exact source chain/169 ownership/17 rows를 요구한다.
- viable exact `169`, `17=4/10/3`, `165/4`, Core bindings `4`, SecondaryBiome non-empty `0`.
- patch size sum = assigned; all perimeter/seed/site/role/biome values를 source와 독립 재계산해 cross-check한다.
- Cells/Patches는 defensive copied read-only; invalid Get은 throw, invalid Try는 false/null이다.
- shuffled source collections, culture/time/thread, repeated Create에서 identical해야 한다.

Frozen colors:

```text
BIO_MOON_CRATER    (90,145,220,235)
BIO_CASSIA_ROOT    (90,180,105,235)
BIO_ABANDONED_MILL (205,135,75,235)
BIO_MOON_DOUGH     (190,115,205,235)
Unassigned         (60,60,68,220)
Patch boundary     (20,20,24,255)
Core site marker   (255,230,80,255)
Seed marker        (245,245,245,255)
```

unknown biome는 fallback color로 숨기지 않고 거부한다.

## Shared GUI

`BiomePatchOverlayGui`는 runtime static stateless renderer이며 Game/Scene이 동일 `Draw`와 layout/hit-test를 사용한다.

```text
PanelOrigin 12,12; Panel 1200x820
GridOrigin 24,56; CellSize 44; Grid 13x13 = 572x572
SidebarOrigin 612,56; Sidebar 564x740
TooltipOrigin 24,646; Tooltip 572x150
RequiredViewport 1224x844
Title MAP04 BIOME PATCHES / Seed {seed}
```

- visual corners `(0,12)/(12,12)/(0,0)/(12,0)`; rect만 `visualRow=12-y`, data order는 index ascending.
- hit-test left/top inclusive, right/bottom exclusive; outside clamp/wrap 금지.
- draw order: panel/title → 169 fills → labels/markers → four-side PatchId boundaries → hover → legend/summary/17 patch rows → tooltip.
- legend는 four biome ID/color, `C/S/I`, `* Core site`, `+ seed`, boundary 의미를 포함한다.
- patch row는 ID/biome/role/size/perimeter/compactness permille을 색 없이 판독 가능하게 표시한다.
- small viewport에서 exact `Biome patch overlay requires 1224 x 844 pixels.`만 표시한다.
- GUI global state는 exception에도 `try/finally`로 복원한다. caller style/source를 mutate하지 않는다.

Pure helpers: `GetCellRect`, `TryHitTest`, `GetBiomeColor`, `GetRoleGlyph`, `FormatCompactness`.

## Runtime / Editor

Runtime component:

```text
[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("WorldGen/Biome Patch Overlay")]
BiomePatchOverlay: HasSnapshot, Snapshot,
SetSnapshot(BiomePatchValidationPublication), ClearSnapshot()
```

snapshot은 `[NonSerialized]`. Set은 Create 성공 후에만 transactional replace하고 invalid input에서 기존값을 보존한다. OnGUI는 active/enabled/snapshot/Event 있을 때 Draw once. Awake/Update/polling/object discovery/auto generation은 없다.

`BiomePatchOverlaySceneDrawer.cs`에 public static drawer와 internal custom editor를 둔다.

- `[DrawGizmo(GizmoType.Active | GizmoType.Selected | GizmoType.NonSelected)]`; `Handles.BeginGUI/EndGUI` + shared Draw once, finally EndGUI.
- `[CustomEditor(typeof(BiomePatchOverlay))]`; injection 안내, read-only seed/count/17 rows, exact `Clear` button만 표시.
- Clear success에서 RepaintAll/QueuePlayerLoopUpdate once. Undo/SetDirty/serialized save/Scene callback/continuous repaint 0.

## Tests / Visual / Gates

Actually run:

```text
BiomePatchOverlayTests >=100 PASS
BiomePatchOverlaySceneDrawerTests >=20 PASS
New combined >=120 PASS
BiomePatchValidatorTests 196/196 PASS
BiomePatchExporterTests 141/141 PASS
BiomePatchModelsTests 107/107 PASS
Required regressions 444/444 PASS
Actually executed total >=564 PASS; failed/skipped 0/0
```

coverage: null/invalid/mismatched publication; exact 169/17/4-10-3/165-4; all lookup/orientation; boundary/perimeter/compactness independent recompute; colors/role/label/tooltip; seed/site markers; immutability/shuffle/culture; all rect/hit-test; GUI state restoration; component transaction/attributes/no auto run; drawer/inspector/Clear-only/dependency audit.

discovery-only Game.Map `>=5249`, Full EditMode `>=5317`. forced compile/Console/warning `0/0/0`.

Visual transient fixture, Game/Scene checklist `18/18`:

1. exact title/seed, 2. 169 cells, 3. four corners/orientation, 4. four biome colors+IDs, 5. PatchId boundaries, 6. C/S/I legend+counts, 7. Core site `*`, 8. other seed `+`, 9. `17/165/4/15` summary, 10. 17 rows, 11. size/perimeter/compactness, 12. three one-cell Intrusions, 13. assigned hover, 14. unassigned hover, 15. Game/Scene identity, 16. outside/small viewport, 17. selection/camera/transform/timeScale/source unchanged, 18. Clear/cleanup residue·Scene dirty delta 0.

capture/evidence가 없거나 Editor access 불가능하면 visual PASS를 추정하지 말고 `BLOCKED`.

Asset gate:

```text
Assets meta 3140->3147
new Runtime/Editor/test/meta 4/1/2/7
exact Assets changes 14; existing/unexpected 0/0
new directory/folder meta 0; duplicate GUID 0
Authoring CSV/meta 50/50; generated CSV files 0
Scene/Prefab/Packages/ProjectSettings changes 0
```

## Result / Finalize

Result `<=140 lines`: STATUS, apply/SHA, created paths+GUID, snapshot/GUI/visual counters, tests, compile/meta/scope, findings, NEXT만 기록한다.

PASS일 때만 MAP04_10 COMPLETE, Current Task NONE, Last Completed/Result를 MAP04_10으로 finalize하고 MAP04_11은 LOCKED로 유지한다.

금지: source/CSV/asmdef 수정, generation/validator/RNG/file run, data repair, Canvas/Camera/asset 생성, Scene/Prefab save, MAP04_11 생성/시작, test 완화, Git commit/push.
