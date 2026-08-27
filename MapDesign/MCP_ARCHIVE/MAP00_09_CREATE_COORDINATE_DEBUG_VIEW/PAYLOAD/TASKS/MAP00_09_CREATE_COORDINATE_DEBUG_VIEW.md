# MAP00_09 — Create Coordinate Debug View

```yaml
status_control:
  task_key: MAP00_09_CREATE_COORDINATE_DEBUG_VIEW
  result_file: REPORTS/MAP00_09_CREATE_COORDINATE_DEBUG_VIEW_RESULT.md
```

## TASK TYPE

```text
EDITOR-ONLY COORDINATE DEBUG WINDOW + SCENE OVERLAY + EDITMODE TEST
```

## Objective

`WorldGen/Coordinates` EditorWindow를 열었을 때 Scene View의 마우스 위치를 z=0 논리 타일 평면에 투영하고, 같은 위치의 `WorldTileCoord`, `SectorCoord`, `MicroChunkCoord`, `LocalTileCoord`를 창과 Scene overlay에 동시에 표시한다.

이 TASK는 Editor 전용 표시 도구만 만든다. Runtime 좌표 계약, Scene object, Prefab, Tilemap, CSV loader, 생성 pass 또는 MAP00 exit audit은 구현하거나 수정하지 않는다.

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
11. 이 TASK
12. `REPORTS/MAP00_08_CREATE_COORDINATE_TESTS_RESULT.md`

## READ ALLOWLIST

본문 읽기 허용:

- Mandatory Read Order의 파일
- `Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef`
- `Assets/_Game/Editor/MapAuthoring/MapAuthoring.Editor.asmdef`
- `Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef`
- `Assets/_Game/Tests/PlayMode/Map/Game.Map.Tests.PlayMode.asmdef`
- `Assets/_Game/Editor/MapAuthoring/Tests/EditMode/MapAuthoring.Tests.EditMode.asmdef`
- `Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldTileCoord.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Domain/MicroChunkCoord.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Domain/LocalTileCoord.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldCoordinateUtility.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/WorldGenConstantsTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/CoordinateValueTypeTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/WorldCoordinateUtilityTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/CoordinateConversionBoundaryTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/WorldGenerationModuleStructureTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/WorldGenerationRuntimeBoundaryTests.cs`
- `Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/WorldGenerationEditorBoundaryTests.cs`
- 아래 WRITE ALLOWLIST의 신규 파일을 생성 후 재검증하기 위한 본문

제한적 검색 허용:

- 승인된 WorldGeneration 디렉터리 36개의 존재 여부와 직계 파일명
- `Assets/_Game/Map/Data/WorldGeneration/Authoring/**/*.csv` 경로만 열거
- Runtime/Editor WorldGeneration 아래 기존 `*.cs` 경로와 선언 namespace/type 이름만 확인
- 프로젝트 전체 `.meta`에서 `guid:` 값만 추출하는 GUID 중복 검사
- 작업 전후 변경 파일 경로 확인
- Unity Console의 현재 compile error와 이 TASK로 발생한 warning 확인
- Unity 메뉴 `WorldGen/Coordinates` 실행과 열린 EditorWindow/Scene View의 시각 확인

기존 coordinate/architecture test 실행 중 테스트 코드가 승인 범위를 검사하는 것은 허용한다.

금지:

- 승인되지 않은 프로젝트 C# 본문 스캔
- `Assets/_Legacy/**` 본문 열람 또는 수정
- Scene/Prefab YAML 열람
- CSV/GDD/과거 하네스 본문 열람
- 시각 검증을 위해 Scene/Prefab/GameObject를 생성·저장하는 행위
- 테스트 통과를 위해 Runtime 또는 기존 test 코드를 수정하는 행위

## Master Backlog Check

`MASTER_IMPLEMENTATION_TASK_LIST.md`에서 다음을 확인한다.

```text
MAP00_01~08 = COMPLETE
MAP00_09_CREATE_COORDINATE_DEBUG_VIEW = next/current
MAP00_10 = LOCKED
MAP01_01 premade patch = HOLD / DO NOT RUN
전체 Task = 205
```

하나라도 다르면 임의 보정하지 말고 `BLOCKED` Result를 작성한다.

## Preflight Preservation Check

MAP00_08 Result의 PASS와 아래 항목을 확인한다.

필수 디렉터리:

```text
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Preview/
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Windows/
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Preview/
Assets/_Game/Map/Runtime/WorldGeneration/Domain/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/
```

필수 assembly 파일:

```text
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Editor/MapAuthoring/MapAuthoring.Editor.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
Assets/_Game/Tests/PlayMode/Map/Game.Map.Tests.PlayMode.asmdef
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/MapAuthoring.Tests.EditMode.asmdef
```

필수 Runtime C# 6개:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldTileCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/MicroChunkCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/LocalTileCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldCoordinateUtility.cs
```

필수 기존 test C# 7개:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/WorldGenConstantsTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/CoordinateValueTypeTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/WorldCoordinateUtilityTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/CoordinateConversionBoundaryTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/WorldGenerationModuleStructureTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/WorldGenerationRuntimeBoundaryTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/WorldGenerationEditorBoundaryTests.cs
```

위 Runtime/test C# 각각의 대응 `.meta`도 존재해야 한다.

추가 순서 검증:

- `WorldGenConstants`의 `public const int` 15개 계약을 유지해야 한다.
- 네 좌표 타입과 `WorldCoordinateUtility` public method 14개 계약을 유지해야 한다.
- MAP00_08 Result는 exhaustive 8/8, utility 10/10, value type 12/12, constant 6/6, architecture 10/10, combined 46/46 PASS여야 한다.
- MAP00_08 coverage는 world corners 4, microchunk corners 10,816, world tiles 259,584여야 한다.
- `Assets/_Game/Map/Data/WorldGeneration/Authoring/**/*.csv`는 0개여야 한다.
- MAP01 이후 Result가 존재하거나 Task가 COMPLETE/CURRENT이면 안 된다.
- 아래 신규 target C#과 `.meta`가 이미 존재하면 안 된다.
- 기존 Editor WorldGeneration 경로에 같은 menu path 또는 같은 type name의 C#이 없어야 한다.

위 조건이 다르면 기존 파일을 이동·복원·삭제·덮어쓰지 말고 `BLOCKED`다.

## WRITE ALLOWLIST

정확히 다음 Editor Preview C# 1개와 Unity가 생성하는 대응 `.meta`를 생성할 수 있다.

```text
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Preview/WorldCoordinateDebugDisplay.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Preview/WorldCoordinateDebugDisplay.cs.meta
```

정확히 다음 EditorWindow C# 1개와 Unity가 생성하는 대응 `.meta`를 생성할 수 있다.

```text
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Windows/WorldCoordinateDebugWindow.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Windows/WorldCoordinateDebugWindow.cs.meta
```

정확히 다음 Editor EditMode test C# 1개와 Unity가 생성하는 대응 `.meta`를 생성할 수 있다.

```text
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Preview/WorldCoordinateDebugDisplayTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Preview/WorldCoordinateDebugDisplayTests.cs.meta
```

추가 생성 허용:

```text
MapDesign/MCP/REPORTS/MAP00_09_CREATE_COORDINATE_DEBUG_VIEW_RESULT.md
```

Runtime, 기존 Editor/test 파일은 WRITE ALLOWLIST가 아니다. TASK EXECUTION 중 `06_IMPLEMENTATION_STATUS.md`는 수정하지 않는다. 상태 변경은 Result PASS 이후 STATUS FINALIZE만 수행한다.

## Coordinate Debug Display Contract

파일:

```text
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Preview/WorldCoordinateDebugDisplay.cs
```

namespace와 type:

```csharp
namespace StarNight.MapAuthoring.Editor.WorldGeneration.Preview
{
    public static class WorldCoordinateDebugDisplay
    {
    }
}
```

public API는 다음 method 1개뿐이다.

```csharp
public static string Format(float worldX, float worldY)
```

### World position mapping

- Unity Editor world XY를 z=0 논리 타일 평면으로 본다.
- `1 Unity world unit = 1 logical tile`이다.
- 좌표 원점은 `(0,0)`이며 각 축을 `Mathf.FloorToInt`로 타일 좌표에 매핑한다.
- 예: `(61.99f, 42.01f)` → `WorldTileCoord(61, 42)`.
- 예: `(-0.01f, 0f)` → candidate `(-1,0)`이며 월드 밖이다. clamp하지 않는다.
- finite 여부를 floor보다 먼저 확인한다.

### Valid exact format

valid world tile은 `WorldCoordinateUtility.TryCreateWorldTile`과 `TryFromWorld`를 사용해 다음 exact four-line string을 반환한다.

```text
World: WorldTileCoord(61, 42)
Sector: SectorCoord(1, 1)
MicroChunk: MicroChunkCoord(1, 1)
Local: LocalTileCoord(1, 2)
```

- newline은 `\n`으로 고정한다.
- 마지막 줄 뒤 trailing newline은 없다.
- 좌표 type의 invariant `ToString()`을 재사용한다.
- 분해 공식을 Editor에서 다시 구현하지 않는다.

### Outside exact format

finite지만 월드 밖인 candidate tile은 다음 exact format이다.

```text
World: OUTSIDE (624, 0)
Sector: -
MicroChunk: -
Local: -
```

candidate 숫자는 `CultureInfo.InvariantCulture`로 출력한다. clamp/wrap 또는 가장 가까운 valid component를 표시하지 않는다.

### Unavailable exact format

X 또는 Y가 `NaN`, positive infinity, negative infinity이면 floor하지 않고 다음 exact format을 반환한다.

```text
World: UNAVAILABLE
Sector: -
MicroChunk: -
Local: -
```

### Display implementation rules

- `using System.Globalization;`, `UnityEngine`, `StarNight.Map.WorldGeneration.Domain`만 사용할 수 있다.
- field, property, constructor, nested type, mutable state를 만들지 않는다.
- SceneView, Handles, EditorWindow를 참조하지 않는다. 이 type은 formatting과 mapping만 담당한다.
- Runtime 좌표 source를 복제하거나 수정하지 않는다.

## EditorWindow Contract

파일:

```text
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Windows/WorldCoordinateDebugWindow.cs
```

namespace와 type:

```csharp
namespace StarNight.MapAuthoring.Editor.WorldGeneration
{
    public sealed class WorldCoordinateDebugWindow : EditorWindow
    {
    }
}
```

### Menu and lifecycle

다음 public static method 하나에 exact menu attribute를 적용한다.

```csharp
[MenuItem("WorldGen/Coordinates")]
public static void Open()
```

- `Open()`은 `GetWindow<WorldCoordinateDebugWindow>()`로 단일 창을 열거나 기존 창을 focus한다.
- 창 title은 exact `World Coordinates`다.
- `OnEnable`에서 `SceneView.duringSceneGui`를 중복 없이 subscribe한다.
- `OnDisable`에서 반드시 unsubscribe한다.
- 창이 닫히면 Scene overlay도 사라진다.
- `[InitializeOnLoad]`, 자동 창 열기, `EditorApplication.update` 상시 polling을 사용하지 않는다.

### Scene mouse projection

`SceneView.duringSceneGui` callback에서:

1. `Event.current.mousePosition`을 `HandleUtility.GUIPointToWorldRay`로 변환한다.
2. ray와 `z=0` 평면의 교차점을 계산한다.
3. direction Z가 0에 가깝거나 교차 거리가 음수면 unavailable text를 사용한다.
4. valid intersection이면 `WorldCoordinateDebugDisplay.Format(point.x, point.y)`를 호출한다.
5. Runtime 변환식이나 floor/bounds를 Window에 복제하지 않는다.

### Window content

창 본문은 다음을 포함한다.

- 설명 label: `Move the mouse over the Scene View.`
- latest four-line coordinate text
- mapping note: `z=0, 1 unit = 1 logical tile`

창을 처음 열었고 아직 Scene sample이 없으면 unavailable exact format을 표시한다.

### Scene overlay

- Scene View 좌상단 `(12,12)`에서 시작하는 고정 help-box 영역을 그린다.
- four-line coordinate text 전체가 잘리지 않는 크기여야 한다.
- label/box는 입력을 처리하거나 Scene selection을 바꾸지 않는다.
- latest text가 바뀔 때만 EditorWindow `Repaint()`를 요청한다.
- `SceneView.RepaintAll()` 또는 매 프레임 전체 월드 순회를 사용하지 않는다.
- hierarchy object, hidden GameObject, Component, Gizmo owner를 생성하지 않는다.

## Editor Runtime Rules

- 새 C#은 기존 `MapAuthoring.Editor` assembly에 포함된다. 신규 asmdef/asmref를 만들지 않는다.
- Editor assembly에서 기존 `Game.Map.Runtime` 좌표 API를 참조한다.
- Runtime assembly에 `UnityEditor` 참조를 추가하지 않는다.
- `Selection`, active Scene, Scene camera transform, Grid/Tilemap 설정을 변경하지 않는다.
- 창 위치/size, sample 좌표, 최근 text를 asset 또는 `EditorPrefs`에 저장하지 않는다.
- log spam, file I/O, reflection, LINQ, async, package dependency를 사용하지 않는다.

## Editor EditMode Test Contract

파일:

```text
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Preview/WorldCoordinateDebugDisplayTests.cs
```

namespace와 fixture:

```csharp
namespace StarNight.MapAuthoring.Tests.WorldGeneration.Preview
{
    public sealed class WorldCoordinateDebugDisplayTests
    {
    }
}
```

NUnit `[Test]` method를 정확히 7개 만든다. parameterized test와 `[TestCase]`를 사용하지 않는다.

1. `Format_OriginShowsAllCoordinateSpaces`
   - `(0f,0f)`의 exact valid four-line string 확인
2. `Format_FractionalPositionUsesFloor`
   - `(61.99f,42.01f)`가 World(61,42), Sector(1,1), MicroChunk(1,1), Local(1,2) exact string인지 확인
3. `Format_MidWorldShowsExpectedComponents`
   - `(300.5f,200.5f)`의 world/component exact string 확인
4. `Format_LastWorldTileShowsExpectedComponents`
   - `(623.999f,415.999f)`가 World(623,415), Sector(12,12), MicroChunk(3,3), Local(11,7)인지 확인
5. `Format_OutsideEdgesShowCandidateWithoutClamping`
   - `(-0.01f,0f)` → OUTSIDE `(-1,0)`
   - `(624f,0f)` → OUTSIDE `(624,0)`
   - `(0f,416f)` → OUTSIDE `(0,416)`
   - 세 경우 component 세 줄은 `-`
6. `Format_NonFiniteInputShowsUnavailable`
   - X/Y 각각의 `NaN`, positive infinity, negative infinity가 exact unavailable string인지 확인
7. `Window_HasLockedMenuPathAndEditorWindowType`
   - `WorldCoordinateDebugWindow`가 sealed `EditorWindow` subtype인지 확인
   - public static `Open()`이 존재하는지 확인
   - `Open()`의 `MenuItem` constructor 첫 문자열 argument가 exact `WorldGen/Coordinates`인지 reflection으로 확인

Test implementation rules:

- 기존 NUnit과 Unity Editor API만 사용한다.
- 실제 NUnit test case 수는 정확히 7개다.
- test는 window를 실제로 열거나 SceneView event를 subscribe하지 않는다.
- menu attribute 검사는 `CustomAttributeData`로 읽기만 한다.
- Scene, Prefab, GameObject, asset을 생성·수정·저장하지 않는다.
- test helper가 필요하면 파일 내부 private static member로만 둔다.

## Visual Verification

Unity MCP 또는 Unity Editor에서 다음을 직접 확인한다.

1. 메뉴 `WorldGen/Coordinates`를 실행한다.
2. title `World Coordinates`인 창이 정확히 하나 열리는지 확인한다.
3. 창 본문에 설명, four-line text, mapping note가 보이는지 확인한다.
4. Scene View 위에서 마우스를 움직일 때 overlay와 창 text가 함께 갱신되는지 확인한다.
5. valid 위치에서 World/Sector/MicroChunk/Local 네 줄이 동시에 보이는지 확인한다.
6. 월드 밖 위치에서 OUTSIDE와 component `-`가 보이고 clamp되지 않는지 확인한다.
7. overlay가 Scene selection/camera를 바꾸지 않는지 확인한다.
8. 창을 닫으면 overlay가 사라지는지 확인한다.
9. 검증 전후 Scene/Prefab 변경이 NONE인지 확인한다.

시각 검증을 수행할 수 없거나 overlay/window 동작을 확인하지 못하면 PASS로 종료하지 말고 `BLOCKED`로 기록한다.

## Collision Handling

1. 신규 target C#이 이미 존재하면 본문을 읽어 병합하거나 덮어쓰지 않는다.
2. target `.meta`만 orphan 상태로 존재하면 GUID를 검사한 뒤 `BLOCKED`로 보고한다. 이번 create-only Task에서 재사용하지 않는다.
3. 동일 menu path 또는 type name이 이미 있으면 기존 코드를 변경하지 않고 `BLOCKED`다.
4. 승인 경로에 예상하지 않은 다른 C#이 있으면 삭제하지 않고 경로만 기록한다. 충돌 시 `BLOCKED`다.
5. 기존 사용자 변경을 되돌리거나 정리하지 않는다.
6. MAP00_04~08 Runtime/test 파일은 존재와 계약만 검증하며 수정하지 않는다.

## DO NOT

- Runtime C# 생성·수정 금지
- 기존 Editor 또는 test C# 수정 금지
- Scene/Prefab/GameObject/Component/ScriptableObject 생성·수정 금지
- debug scene, runtime HUD, MonoBehaviour overlay 생성 금지
- Tilemap/Grid/Camera/Selection 설정 변경 금지
- coordinate/constant/utility API 변경 금지
- EditorWindow 자동 실행 또는 상시 polling 금지
- CSV, schema, loader, registry 생성·수정 금지
- asmdef/asmref 생성·수정 금지
- `Assets/_Legacy/**` 변경 금지
- 기존 Room/MacroChunk/Stage/P6/P11 타입 참조 금지
- Tile, Tile Palette, Animator, Addressables 변경 금지
- `Packages/**`, `ProjectSettings/**` 변경 금지
- 새 package/dependency 설치 금지
- 기존 파일·폴더 삭제/이동/이름 변경 금지
- 관련 없는 포맷팅·warning 수정 금지
- Git commit/push/branch/reset/rebase/force 금지
- MAP00_10 또는 MAP01 선행 작업 금지

## Inputs

- `MASTER_IMPLEMENTATION_TASK_LIST.md`
- MAP00_08 PASS Result
- `WorldGenConstants`, 네 coordinate value type, `WorldCoordinateUtility`
- 기존 exhaustive/utility/value type/constant/architecture tests
- 보존된 Editor/Preview/Windows/Editor test 구조와 assembly 경계
- Unity Editor `6000.3.8f1`

## Outputs

- `WorldCoordinateDebugDisplay.cs`와 `.meta`
- `WorldCoordinateDebugWindow.cs`와 `.meta`
- `WorldCoordinateDebugDisplayTests.cs`와 `.meta`
- `REPORTS/MAP00_09_CREATE_COORDINATE_DEBUG_VIEW_RESULT.md`

## Implementation Steps

1. `MASTER_IMPLEMENTATION_TASK_LIST.md`와 `06_IMPLEMENTATION_STATUS.md`에서 이 TASK가 정확한 next/CURRENT인지 확인한다.
2. MAP00_08 Result가 `STATUS: PASS`, exhaustive 8/8, utility 10/10, value type 12/12, constant 6/6, architecture 10/10, combined 46/46, compile error 0인지 확인한다.
3. 작업 전 변경 파일 경로를 기록하고 기존 무관 변경은 수정·복구하지 않는다.
4. Preflight Preservation Check의 디렉터리, asmdef, MAP00_04~08 파일과 MAP01 미시작 상태를 확인한다.
5. 신규 target C# 3개와 `.meta` 3개가 모두 absent이고 menu/type collision이 없는지 확인한다.
6. `WorldCoordinateDebugDisplay.cs`를 exact display contract로 생성한다.
7. `WorldCoordinateDebugWindow.cs`를 exact EditorWindow contract로 생성한다.
8. `WorldCoordinateDebugDisplayTests.cs`를 정확히 7개 test case로 생성한다.
9. Unity Asset Refresh와 compilation이 완료될 때까지 기다린다.
10. 신규 `.cs.meta` 3개의 GUID 형식과 project-wide uniqueness를 검사한다.
11. 신규 `WorldCoordinateDebugDisplayTests` fixture를 실행해 actual cases 7개가 모두 PASS인지 확인한다.
12. 기존 exhaustive 8 + utility 10 + value type 12 + constant 6 + architecture 10, actual cases 46개가 모두 PASS인지 확인한다.
13. 신규 7 + 기존 46, 총 actual cases 53개를 단일 targeted run으로 재검증한다.
14. Visual Verification 9개 항목을 수행하고 결과를 기록한다.
15. Runtime source/asmdef/Scene/Prefab이 변경되지 않았는지 확인한다.
16. 작업 후 Asset 변경이 허용된 C# 3개와 `.meta` 3개뿐인지 확인한다.
17. Result 문서를 작성한다.
18. 모든 DONE CONDITIONS가 PASS인 경우에만 Result에 `STATUS: PASS`를 기록한다.

## Tests

### T1 — Compile

```text
Compile Errors = 0
Relevant New Warnings = 0
```

### T2 — New Editor Display Fixture

```text
Fixture: StarNight.MapAuthoring.Tests.WorldGeneration.Preview.WorldCoordinateDebugDisplayTests
Actual cases: 7
Passed: 7
Failed: 0
Skipped: 0
```

### T3 — Existing Coordinate and Architecture Regression

```text
CoordinateConversionBoundaryTests: 8/8 PASS
WorldCoordinateUtilityTests: 10/10 PASS
CoordinateValueTypeTests: 12/12 PASS
WorldGenConstantsTests: 6/6 PASS
Architecture fixtures: 10/10 PASS
Actual cases: 46
Passed: 46
Failed: 0
Skipped: 0
```

### T4 — Combined Targeted EditMode Result

```text
Actual cases: 53
Passed: 53
Failed: 0
Skipped: 0
```

### T5 — Asset Meta Validation

- 신규 `.cs.meta` 3개 존재
- GUID 형식 유효
- 신규 GUID끼리 중복 0
- 프로젝트 전체 GUID와 중복 0

### T6 — Change Scope

이번 TASK의 Asset 변경은 신규 Editor C# 2개, Editor test C# 1개와 `.meta` 3개뿐이다.

기존 무관 변경은 Result에 별도로 기록하며 수정하지 않는다. Runtime, 기존 Editor/test, CSV, asmdef, Scene, Prefab, Package, ProjectSettings 변경은 0개여야 한다.

## Unity Verification

필수:

```text
Unity Version: 6000.3.8f1
Asset Refresh: PASS
Compile Errors: 0
Relevant New Warnings: 0
New Editor Display Tests: PASS (7/7)
Existing Coordinate/Architecture Tests: PASS (46/46)
Combined Targeted EditMode Tests: PASS (53/53)
WorldGen/Coordinates Menu: PASS
EditorWindow Content: PASS
Scene View Overlay: PASS
Valid Coordinate Display: PASS
Outside Coordinate Display: PASS
Close/Unsubscribe Behavior: PASS
PlayMode Tests: NOT RUN
Scene/Prefab Changes: NONE
```

Unity Editor 또는 Unity MCP에 접근할 수 없어 Asset Refresh, compilation, 대상 EditMode 결과 또는 시각 동작을 확인할 수 없으면 PASS로 종료하지 말고 `BLOCKED`로 기록한다.

## Result File

```text
REPORTS/MAP00_09_CREATE_COORDINATE_DEBUG_VIEW_RESULT.md
```

Result에는 반드시 다음 섹션을 포함한다.

```text
TASK
STATUS
SUMMARY
READ
MASTER BACKLOG CHECK
PREFLIGHT PRESERVATION CHECK
CREATED
DEBUG VIEW CONTRACT
CHANGED
TEST
VISUAL VERIFICATION
UNITY
ASSET META VALIDATION
OUT_OF_SCOPE_FINDINGS
DONE CONDITIONS
NEXT
Recommended Commit
```

## DONE CONDITIONS

- [ ] Current Task가 MAP00_09이고 master backlog의 정확한 next임을 확인했다.
- [ ] 전체 master backlog 205개와 MAP01 HOLD 상태를 확인했다.
- [ ] MAP00_08 Result의 PASS, exhaustive 8/8, utility 10/10, value type 12/12, constant 6/6, architecture 10/10, combined 46/46, compile error 0을 확인했다.
- [ ] 보존 대상 디렉터리 5개, asmdef 5개, MAP00_04~08 필수 파일과 `.meta`가 존재한다.
- [ ] `WorldGenConstants` 15개 const, 네 coordinate value type, utility public API 14개 계약을 보존했다.
- [ ] Authoring CSV 0개와 MAP01 이후 미시작 상태를 확인했다.
- [ ] target C#과 `.meta`가 작업 전에 absent이고 menu/type collision이 없다.
- [ ] 정확한 Editor Preview C# 1개, EditorWindow C# 1개, Editor test C# 1개만 생성했다.
- [ ] `WorldCoordinateDebugDisplay.Format` public API 1개만 만들었다.
- [ ] finite world position을 floor하고 Runtime coordinate API로 four-line text를 만든다.
- [ ] valid/outside/unavailable exact format과 invariant 숫자 출력이 일치한다.
- [ ] exact `WorldGen/Coordinates` menu와 `World Coordinates` title을 구현했다.
- [ ] Scene mouse ray를 z=0 평면에 투영하고 창/overlay가 같은 latest text를 표시한다.
- [ ] OnEnable/OnDisable이 SceneView callback을 중복 없이 subscribe/unsubscribe한다.
- [ ] Scene object, hidden GameObject, Prefab, runtime HUD, 상시 polling을 만들지 않았다.
- [ ] 실제 신규 Editor test case가 정확히 7개이며 모두 PASS다.
- [ ] 기존 coordinate/architecture actual cases 46개가 모두 PASS다.
- [ ] combined targeted EditMode actual cases 53개가 모두 PASS다.
- [ ] Visual Verification 9개 항목이 모두 PASS다.
- [ ] 신규 `.cs.meta` 3개가 존재하며 GUID가 유효하고 project-unique하다.
- [ ] Unity Asset Refresh가 PASS다.
- [ ] Unity Compile Error가 0개다.
- [ ] 관련 신규 Warning이 0개다.
- [ ] PlayMode 테스트를 실행하지 않았다.
- [ ] Runtime/기존 Editor·test/CSV/asmdef/Scene/Prefab/Package/ProjectSettings 변경이 0개다.
- [ ] Result 문서가 요구 형식을 충족한다.
- [ ] MAP00_10 또는 MAP01을 시작하지 않았다.

## Completion Rule

TASK EXECUTION은 Result에 `STATUS: PASS / FAIL / BLOCKED`만 기록한다.

Result가 정확히 `STATUS: PASS`이고 모든 DONE CONDITIONS가 완료된 경우에만 STATUS FINALIZE Phase가:

```text
MAP00_09_CREATE_COORDINATE_DEBUG_VIEW: CURRENT -> COMPLETE
Current Task: TASKS/MAP00_09_CREATE_COORDINATE_DEBUG_VIEW.md -> NONE
```

을 수행한다.

STATUS FINALIZE는 MAP00_10을 CURRENT로 바꾸지 않는다. 다음 TASK는 새 패치를 기다린다.

## Expected Next Task

```text
MAP00_10_MAP00_EXIT_AUDIT
```

다음 TASK는 별도 패치로만 연다.
