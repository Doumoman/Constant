# MAP02_07 — Create World Topology Overlay

```yaml
status_control:
  task_key: MAP02_07_CREATE_WORLD_TOPOLOGY_OVERLAY
  result_file: REPORTS/MAP02_07_CREATE_WORLD_TOPOLOGY_OVERLAY_RESULT.md
```

## Objective

exact `GridInitializationResult`를 read-only 169-cell topology overlay snapshot으로 투영하고, 동일한 runtime GUI renderer로 Game View와 Scene View에 13×13 회색박스를 표시한다. 화면의 x는 왼쪽에서 오른쪽으로 `0..12`, y는 아래에서 위로 `0..12`이며 시각적 첫 행은 y=12, 마지막 행은 y=0이다.

모든 cell에는 좌표와 색상 외 Role glyph를 항상 표시한다. 마우스 hover tooltip에는 sector coordinate/index, inclusive world-tile X/Y 범위, exact Role token, L/R/U/D neighbor index를 표시한다. 이 Task는 기존 grid/result를 표시할 뿐 Root/pass/RNG/replay를 자동 실행하거나 generated/Authoring 데이터를 변경하지 않는다.

## Mandatory Read / Scope

entrypoint → locked/work/CSV/Unity/change/patch/finalize rules → Master → Status → 이 Task → MAP02_06 PASS Result 순서로 읽는다.

Map Package v1.0 exact path가 installed tree에 있으면 아래 부분만 읽는다.

```text
01_FIXED_SPEC/01_WORLD_FIXED_SPEC.md            # world/sector dimensions and role vocabulary only
01_FIXED_SPEC/02_COORDINATE_AND_ID_RULES.md     # origin/direction only
02_PHASE_ROADMAP/MAP02_TOPOLOGY_GRAYBOX.md      # overlay output/tooltip/exit orientation only
02_PHASE_ROADMAP/MAP14_EDITOR_AND_DEBUG_TOOLS.md # fixed legend and non-color identity only
```

exact 문서가 installed tree에 없으면 이 Task의 frozen contract를 authoritative fallback으로 사용한다. 대체 문서를 broad search하거나 Legacy/다른 generator를 읽지 않는다.

기존 public API 확인은 아래 exact files로 제한한다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedSectorRole.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedWorldData.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGridIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorNeighborIndices.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GridInitializationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GridInitializationPass.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Preview/WorldCoordinateDebugDisplay.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Windows/WorldCoordinateDebugWindow.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/GeneratedWorldDataTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/GridInitializationPassTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Preview/WorldCoordinateDebugDisplayTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Editor/MapAuthoring/MapAuthoring.Editor.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/MapAuthoring.Tests.EditMode.asmdef
```

approved Runtime `Diagnostics`, Editor `Preview`, Runtime test `Generation`, Editor test `Preview` 폴더의 `rg --files` path-only inventory는 허용한다. content search는 위 exact files와 아래 WRITE ALLOWLIST의 생성 후 파일로만 한정한다. 다른 file match body를 출력하는 broad recursive search, MAP02_08 이후 Task body, Legacy/Stage/P6/P11 generator, CSV body, Scene/Prefab YAML은 읽거나 사용하지 않는다.

## WRITE ALLOWLIST

Runtime C# 신규 4:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/WorldTopologyOverlayCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/WorldTopologyOverlaySnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/WorldTopologyOverlayGui.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/WorldTopologyOverlay.cs
```

Editor C# 신규 1:

```text
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Preview/WorldTopologyOverlaySceneDrawer.cs
```

Runtime EditMode test C# 신규 1:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/WorldTopologyOverlayTests.cs
```

Editor EditMode test C# 신규 1:

```text
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Preview/WorldTopologyOverlaySceneDrawerTests.cs
```

신규 C# 7 + matching `.cs.meta` 7 + Result 1만 허용한다. existing C#/test/meta 수정은 `0`이다. 모두 existing approved folders에 두며 새 directory/folder meta를 만들지 않는다.

Runtime namespace는 `StarNight.Map.WorldGeneration.Diagnostics`, Editor namespace는 `StarNight.MapAuthoring.Editor.WorldGeneration.Preview`다. existing `Game.Map.Runtime`, `MapAuthoring.Editor`, `Game.Map.Tests.EditMode`, `MapAuthoring.Tests.EditMode` assembly를 재사용한다. Runtime assembly에 `UnityEditor` reference를 추가하지 않는다.

다른 MAP00/01/MAP02 C#/tests/meta, accepted legacy Editor folder meta 6개, Authoring CSV/meta, generated output, asmdef, Scene/Prefab/Package/ProjectSettings 수정 금지. 시각 검증용 hierarchy object는 transient `HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild`로만 만들고 저장하지 않으며 검증 직후 제거한다.

## `WorldTopologyOverlayCell` Contract

sealed immutable view value object가 exact 아래 정보를 가진다.

```text
int Index
SectorCoord Coordinate
GeneratedSectorRole Role
int WorldTileMinX
int WorldTileMaxX
int WorldTileMinY
int WorldTileMaxY
int LeftIndex
int RightIndex
int UpIndex
int DownIndex
string RoleToken
string RoleGlyph
string CellLabel
string Tooltip
```

- source는 exact `SectorCell`과 같은 index의 `SectorNeighborIndices`다.
- index/coordinate/neighbor를 다시 추측하거나 repair하지 않고 source의 frozen grid contract가 틀리면 거부한다.
- world tile inclusive range는 `sectorX*48 .. sectorX*48+47`, `sectorY*32 .. sectorY*32+31`이며 locked `WorldGenConstants`를 사용한다.
- 모든 숫자 문자열은 invariant culture다.
- exact Role token/glyph mapping은 아래다.

```text
Unassigned   -> UNASSIGNED    / U
Mandatory    -> MANDATORY     / M
Type0        -> TYPE0         / 0
ReservedSite -> RESERVED_SITE / S
InactiveBuffer -> INACTIVE_BUFFER / X
```

- undefined enum은 임의 숫자/문자열로 표시하지 않고 `ArgumentOutOfRangeException`으로 거부한다.
- cell label은 exact `x,y\n{glyph}`이며 trailing newline이 없다.
- tooltip은 exact 네 줄이며 trailing newline이 없다.

```text
Sector: SectorCoord(6, 6) / Index 84
World Tiles: X 288..335 / Y 192..223
Role: UNASSIGNED
Neighbors: L=83 R=85 U=97 D=71
```

- tooltip/label은 color와 무관하게 cell identity를 판독할 수 있어야 한다.
- source object/collection을 보관하거나 public setter/mutable collection을 노출하지 않는다.

## `WorldTopologyOverlaySnapshot` Contract

sealed immutable snapshot public surface:

```text
ulong Seed
IReadOnlyList<WorldTopologyOverlayCell> Cells
int Count
WorldTopologyOverlayCell GetCell(int index)
WorldTopologyOverlayCell GetCell(SectorCoord coordinate)
bool TryGetCell(int index, out WorldTopologyOverlayCell cell)
static WorldTopologyOverlaySnapshot Create(GridInitializationResult result)
```

- non-null exact 169-cell `GridInitializationResult`만 입력받는다.
- result의 `WorldData.Cells`와 `Neighbors`를 index `0..168` 오름차순으로 copy해 독립 read-only snapshot을 만든다.
- `Index == y*13+x`, exact coordinate set, L/R/U/D, border `-1`, reciprocal topology를 검증한다. 틀린 값을 수정하거나 y flip으로 숨기지 않는다.
- snapshot 생성 후 source/caller collection과 관계없이 값이 변하지 않는다.
- `GetCell(SectorCoord)`는 existing `WorldGridIndex.ToIndex`를 사용한다. formula를 별도 복제하지 않는다.
- invalid Get은 `ArgumentOutOfRangeException`, invalid Try는 false/null이다.
- Unity object, GameObject/Scene/Camera, Root, Registry, file path를 보관하지 않는다.

## Frozen Overlay Layout

`WorldTopologyOverlayGui`는 runtime static stateless renderer다. Game/Scene View가 이 class의 동일 draw method와 layout/hit-test를 사용한다.

Constants:

```text
PanelOrigin        = (12, 12) GUI pixels
CellSize           = 32 GUI pixels
GridColumns/Rows   = 13/13
GridPixelSize      = 416×416
PanelPixelSize     = 440×564
Title              = MAP02 TOPOLOGY / Seed {seed invariant}
Legend             = U Unassigned | M Mandatory | 0 Type0 | S Reserved | X Inactive
EmptyHoverText     = Hover a sector for details.
```

- panel/grid rect, cell rect, draw order와 hit-test는 x/y axis-aligned GUI pixels로 고정한다.
- visual top-left cell은 `(0,12)`, top-right `(12,12)`, bottom-left `(0,0)`, bottom-right `(12,0)`다.
- cell index/draw order는 visual row와 무관하게 snapshot index `0..168`; rect 계산만 `visualRow = 12-y`를 사용한다.
- hit-test는 left/top inclusive, right/bottom exclusive이며 grid line 밖을 이웃 cell로 clamp하지 않는다.
- 모든 169 cell을 한 panel에 그리고 cell label을 항상 표시한다. hover cell은 outline을 추가하고 아래 tooltip box에 exact tooltip을 표시한다.
- panel은 fixed title, grid, legend, tooltip 순서이며 마우스가 grid 밖이면 empty hover text를 표시한다.
- 색상은 exact Role별 고정 `Color32`다.

```text
Unassigned     = (96, 96, 96, 230)
Mandatory      = (20, 150, 220, 230)
Type0          = (60, 180, 90, 230)
ReservedSite   = (235, 135, 35, 230)
InactiveBuffer = (35, 35, 35, 230)
```

- color만으로 Role을 구분하지 않고 glyph/legend/token을 병기한다.
- `GUI.color`, `GUI.backgroundColor`, `GUI.contentColor`, `GUI.enabled`, matrix 등 변경한 GUI global state는 `try/finally`로 exact 복원한다.
- caller-owned GUIStyle을 mutate하지 않는다. persistent texture/material/font/style을 만들지 않는다.
- `WorldTopologyOverlayGui`는 Root/pass/RNG/file I/O, UnityEditor, Camera/Transform, static mutable cache를 참조하지 않는다.
- viewport가 panel보다 작은 경우 data를 잘라 다른 좌표처럼 보이게 하지 않고 exact `World topology overlay requires 440 x 564 pixels.` 안내만 표시한다. visual verification은 충분한 Scene/Game View 크기에서 수행한다.

## Runtime `WorldTopologyOverlay` Component

파일/type:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/WorldTopologyOverlay.cs
StarNight.Map.WorldGeneration.Diagnostics.WorldTopologyOverlay
```

exact attributes:

```text
[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("WorldGen/World Topology Overlay")]
```

public surface:

```text
bool HasSnapshot
WorldTopologyOverlaySnapshot Snapshot
void SetSnapshot(GridInitializationResult result)
void ClearSnapshot()
```

- `SetSnapshot`은 `WorldTopologyOverlaySnapshot.Create`로 새 snapshot을 생성해 마지막 성공값을 교체한다. null/invalid input 실패 시 이전 snapshot을 바꾸지 않는다.
- `ClearSnapshot`은 null 상태로 만들며 initial state도 null/HasSnapshot false다.
- snapshot field는 `[NonSerialized]`이고 Scene/Prefab/asset에 생성 데이터를 저장하지 않는다.
- `OnGUI`는 component enabled/active, snapshot present, Event available일 때만 fixed panel을 exact 1회 그린다.
- Game View mouse position은 `Event.current.mousePosition`을 그대로 공유 renderer에 전달한다.
- `OnGUI`는 world generation/grid pass를 호출하지 않는다. `Awake`, `OnEnable`, `Update`, `LateUpdate`, coroutine에서 generation/polling을 하지 않는다.
- Camera/Canvas/UI document/GameObject를 생성·탐색하지 않고 selection/transform/timeScale를 변경하지 않는다.
- component 자체는 log/file I/O/AssetDatabase/Editor API를 사용하지 않는다.

## Scene Drawer / Inspector Contract

`WorldTopologyOverlaySceneDrawer.cs` 하나에 아래 Editor type 두 개를 둔다.

```text
public static class WorldTopologyOverlaySceneDrawer
internal sealed class WorldTopologyOverlayEditor : UnityEditor.Editor
```

Scene drawer:

- exact `[DrawGizmo(GizmoType.Active | GizmoType.Selected | GizmoType.NonSelected)]` static method 1개로 `WorldTopologyOverlay`를 받는다.
- component가 enabled/active이고 snapshot이 있을 때 `Handles.BeginGUI/EndGUI` 사이에서 runtime `WorldTopologyOverlayGui`의 동일 fixed panel/draw method를 exact 1회 호출한다.
- `EndGUI`는 exception에도 `finally`로 실행한다.
- `SceneView.duringSceneGui`, `EditorApplication.update`, `[InitializeOnLoad]`, 자동 object/window 생성, selection requirement를 사용하지 않는다.
- Scene camera/selection/grid/gizmo toggle을 변경하지 않는다.

Custom inspector:

- exact `[CustomEditor(typeof(WorldTopologyOverlay))]`다.
- 안내문, invariant seed text field, `Preview P00 Grid`, `Clear` 버튼만 제공한다.
- initial seed text는 exact `0`; parsing은 invariant `NumberStyles.None`이고 parse 후 `value.ToString(CultureInfo.InvariantCulture)`가 input과 exact 같아야 한다. whitespace, sign, separator, redundant leading zero를 포함한 invalid/non-canonical ulong 입력은 HelpBox로 표시하고 snapshot을 바꾸지 않는다.
- `Preview P00 Grid` user action에서만 `new GridInitializationPass().Execute(seed)`를 exact 1회 호출해 `overlay.SetSnapshot`에 넘긴다. Root/RNG/replay/file을 호출하지 않는다.
- `Clear`는 `ClearSnapshot` exact 1회다.
- 버튼 성공 시에만 `SceneView.RepaintAll()`과 `EditorApplication.QueuePlayerLoopUpdate()`를 각각 1회 요청한다. frame polling/continuous repaint는 없다.
- snapshot은 nonserialized이므로 `Undo`, `EditorUtility.SetDirty`, serialized property, Scene dirty/save를 사용하지 않는다.

## No Ownership / Data Mutation

- snapshot 생성과 rendering은 `GridInitializationResult`, `GeneratedWorldData`, `SectorCell`, neighbor source를 mutate하지 않는다.
- seed/role/neighbor/coordinate/world bounds는 표시용 복사값이며 pass output으로 되돌려 쓰지 않는다.
- overlay는 Authoring/generated CSV, seed manifest, replay bundle을 읽거나 쓰지 않는다.
- Role color/glyph는 표시 정보일 뿐 후속 biome/site/route assignment를 추론하지 않는다.
- P00 inspector preview의 169 cell은 모두 `Unassigned/U`이며 placeholder Role/ID를 넣지 않는다.

## Baseline / Meta Stability

MAP02_06 PASS 이후 clean baseline:

```text
Seed manifest/replay focused: 97/97
Pass execution record focused: 77/77
WorldGenerationRoot focused: 84/84
MAP02_01/02/03 focused: 56/103/90
ContentVersionHash focused: 54/54
Targeted EditMode: 1374/1374
Full EditMode: 1394/1394
Authoring CSV/meta: 50/50
Assets meta: 2981
accepted legacy Editor folder meta: 6/6
duplicate GUID groups: 0
```

legacy folder meta 6개는 정상 baseline이며 삭제·재작성·신규 drift로 분류하지 않는다. 새 directory/folder meta expected `0`. 신규 matching meta 7개 반영 clean final Assets meta는 `2988`이다.

## DO NOT

- existing Domain/Generation/Data/Random production/test 수정 금지
- explicit inspector `Preview P00 Grid`의 `GridInitializationPass.Execute(seed)` exact 1회를 제외한 Root/다른 pass/RNG/replay/CSV/file I/O 실행 또는 수정 금지
- overlay에서 자동 generation, singleton/current-world discovery, service locator/event bus 금지
- 13×13 data를 y flip, clamp, wrap, transpose하거나 index를 draw order로 재정의 금지
- color-only 표시, Role `ToString`/case conversion 기반 token 생성 금지
- biome/site/route/type0/recipe placeholder나 후속 overlay 선행 구현 금지
- MAP02 exit audit/hash batch/approval gate 선행 구현 금지
- Canvas/Camera/RenderTexture/Texture2D/Material/Shader/font asset 생성 금지
- permanent Scene object, hidden saved object, Prefab/Scene 저장 금지
- automatic EditorWindow, Scene callback subscription, constant repaint/polling 금지
- static mutable snapshot/style/texture/cache, reflection scan, async/threading 금지
- exception swallow, invalid source repair, test skip/ignore/assertion 완화 금지
- new directory/folder meta/asmdef/asmref, Authoring/generated CSV/meta/Scene/Prefab/Package/ProjectSettings/Git 변경 금지

## Tests / Verification

Focused minimum 60 cases across the two new fixtures:

- snapshot null rejection, exact seed/count/order/copy/read-only/lookup/TryGet
- all 169 index↔coordinate and tile inclusive ranges against independent expected values
- all 169 exact L/R/U/D, corner/edge/interior and reciprocal values preserved
- exact five Role token/glyph/Color32 mappings and undefined enum rejection
- exact label and tooltip strings for corners/center/last cell, invariant culture
- fixed panel/grid dimensions and all 169 cell rects
- visual top-left/right and bottom-left/right orientation without y flip
- hit-test all cell centers plus left/top inclusive, right/bottom exclusive and outside rejection
- component exact attributes/public surface, initial empty, transactional SetSnapshot, clear, no automatic pass call
- Scene drawer exact DrawGizmo target/mask/static method and CustomEditor target
- custom inspector seed parsing/button contract through pure helper or reflection without opening/saving Scene
- Game/Scene renderer identity uses exact same runtime draw entrypoint
- P00 preview all `U`, no placeholder role/ID
- existing `56/103/90/84/77/97/54` focused regressions
- accepted meta 6 unchanged, existing modification 0, new directory 0

```text
New topology overlay focused: >=60 PASS
MAP02_01 GeneratedWorldData: 56/56 PASS
MAP02_02 deterministic RNG streams: 103/103 PASS
MAP02_03 GridInitializationPass: 90/90 PASS
MAP02_04 WorldGenerationRoot: 84/84 PASS
MAP02_05 execution records: 77/77 PASS
MAP02_06 seed manifest/replay: 97/97 PASS
ContentVersionHash: 54/54 PASS
MAP00 coordinate/architecture regression: PASS
MAP01 Registry/content/import regression: PASS
Previous targeted baseline: 1374/1374 PASS
Targeted total: >=1434 PASS
Full project EditMode: >=1454 PASS
Unity 6000.3.8f1 / force refresh / compile error 0 / relevant warning 0
PlayMode NOT RUN / Scene-Prefab saved changes NONE
```

Authoring CSV/meta `50/50` unchanged, accepted folder meta `6/6` unchanged, 신규 matching meta `7/7` valid, final Assets meta `2988`, project duplicate GUID `0`을 확인한다. Task marker 이후 final Assets 변경은 신규 C# 7 + matching meta 7 = `14`, unexpected `0`이어야 한다. existing Assets modification exact `0`이다.

## Visual Verification

Unity MCP/Editor에서 충분한 크기의 Scene View와 Game View로 아래를 직접 확인한다. visual fixture는 `HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild` transient object + `WorldTopologyOverlay`만 사용하고 seed `4660`의 explicit `Preview P00 Grid` action 후 제거한다.

1. Scene View와 Game View에 같은 title `MAP02 TOPOLOGY / Seed 4660`이 보인다.
2. 두 View 모두 13 columns × 13 rows, total 169 cells가 한 panel에 전부 보인다.
3. top-left `(0,12)`, top-right `(12,12)`, bottom-left `(0,0)`, bottom-right `(12,0)`이 뒤집히지 않는다.
4. 모든 P00 cell에 coordinate와 `U` glyph가 색상과 함께 보인다.
5. fixed legend의 다섯 glyph/text가 잘리지 않는다.
6. `(0,0)` hover가 world X `0..47`, Y `0..31`, neighbors `L=-1 R=1 U=13 D=-1`을 표시한다.
7. `(6,6)` hover가 index `84`, world X `288..335`, Y `192..223`, neighbors `83/85/97/71`을 표시한다.
8. `(12,12)` hover가 index `168`, world X `576..623`, Y `384..415`, neighbors `167/-1/-1/155`를 표시한다.
9. grid 밖 hover는 exact empty hover text이며 nearest cell로 clamp되지 않는다.
10. Scene/Game 표시와 hover text가 같은 snapshot에서 일치한다.
11. overlay가 selection, Scene camera, active camera, transform, time scale을 바꾸지 않는다.
12. Clear/temporary object removal 후 두 overlay가 사라지고 hierarchy residue와 saved Scene/Prefab/dirty-state delta가 없다.

visual 12/12와 Game/Scene capture evidence가 없으면 automated tests가 PASS여도 `BLOCKED`다. 현재 Scene이 작업 전부터 dirty하면 그 상태를 지우거나 저장하지 말고 exact before/after state가 동일함을 기록한다.

## Result / Completion

Result: `REPORTS/MAP02_07_CREATE_WORLD_TOPOLOGY_OVERLAY_RESULT.md`.

Required sections: TASK, STATUS, SUMMARY, READ, MASTER BACKLOG CHECK, MAP02_06 GATE CHECK, CREATED, MODIFIED, PREEXISTING_IDENTICAL, OVERLAY CELL, SNAPSHOT, SHARED GUI, GAME VIEW COMPONENT, SCENE DRAWER, CUSTOM INSPECTOR, TEST, VISUAL VERIFICATION, UNITY, ASSET META VALIDATION, CHANGE SCOPE, OUT_OF_SCOPE_FINDINGS, DONE CONDITIONS, NEXT, Recommended Commit.

모든 계약·회귀·visual verification이 PASS일 때만 MAP02_07 COMPLETE, Current Task NONE으로 finalize한다. `MAP02_08_MAP02_EXIT_TESTS`는 LOCKED로 유지하고 자동 시작하지 마.

Recommended Commit: `feat(map): add world topology overlay`
