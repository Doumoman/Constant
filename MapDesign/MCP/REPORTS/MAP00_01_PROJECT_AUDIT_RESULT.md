# MAP00_01 Project Audit Result

```text
TASK: MAP00_01_PROJECT_AUDIT
STATUS: PASS
AUDIT TYPE: READ-ONLY
```

## 1. Audit Scope Summary

- MCP Starter 7개와 현재 TASK 문서를 지정 순서대로 읽었다.
- `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`, 존재하는 `Packages/packages-lock.json`을 확인했다.
- 금지 폴더를 제외한 프로젝트 asmdef 경로를 먼저 수집한 뒤 38개 전체 내용을 확인했다.
- asmref는 경로 검색 결과 0개였다.
- `Assets/` depth 1~3에서 폴더 216개, 파일 382개(메타 파일 포함)의 경로/이름을 확인했다.
- Map 키워드 C# 후보는 292개였다. TASK 제한에 따라 내용을 무차별 로드하지 않고 충돌 판단에 필요한 30개만 선정해 확인했다.
- Scene/Prefab은 경로와 파일명 외 내용을 읽지 않았다.
- GDD, CSV 본문, Scene/Prefab YAML, Texture/Sprite/Audio, `Library/`, `Temp/`, `Logs/`, `obj/`, 빌드 결과물, Git history는 읽지 않았다.

## 2. A. Unity

```text
Unity Version: 6000.3.8f1 (revision 1c7db571dde0)
Render Pipeline package: com.unity.render-pipelines.universal 17.3.0
2D Tilemap 관련 package: com.unity.2d.tilemap 1.0.0; com.unity.2d.tilemap.extras 6.0.1; com.unity.modules.tilemap 1.0.0
Unity Test Framework: com.unity.test-framework 1.6.0
Addressables 존재 여부: NOT FOUND
Input System 존재 여부: YES — com.unity.inputsystem 1.18.0
```

패키지 버전은 `manifest.json`과 `packages-lock.json`에서 교차 확인했다. 렌더 파이프라인은 URP다.

## 3. Assets Depth 1~3 Structure

### Top-level folders

```text
Assets/_Game
Assets/_Recovery
Assets/2D Fantasy sprite bundle
Assets/Screenshots
Assets/Settings
Assets/StarNight
Assets/TextMesh Pro
```

### Project-owned structure relevant to the Map module

```text
Assets/_Game/Core/{Camera,Flow,Grid,Inventory,Maru,Player,Rooms,Save,Secrets,State,Streaming,Tools}
Assets/_Game/Data/{Global,Legacy,Stages}
Assets/_Game/Editor/{CoreValidation,MapAuthoring,StageAuthoring,Tests,ToolAuthoring}
Assets/_Game/Map/{Data,Prefabs,Rooms,Runtime,VisualProfiles}
Assets/_Game/Stage/{Data,Editor,Runtime,Tests}
Assets/_Game/Tests/{EditMode,PlayMode}
Assets/_Game/Integration/{Data,Editor,Runtime,Tests}
Assets/_Game/Interaction/{Data,Runtime,Tests}
Assets/_Game/Tools/{Data,Prefabs,Runtime,Tests,VisualProfiles}
Assets/_Game/WorldObjects/{Data,Prefabs,Runtime,Tests}
Assets/StarNight/{Art,Audio,Captures,Data,Documentation,Input,Narrative,Prefabs,QA,Scenes,Scripts,Settings}
Assets/StarNight/Scripts/{Editor,Runtime,Tests}
```

`Assets/_Game`은 기능별 Runtime/Editor/Test assembly를 분리한 현재 구조이고, `Assets/StarNight/Scripts`는 별도의 레거시 통합 assembly 구조다. 새 광역 맵 모듈은 `_Game` 구조에 배치하는 것이 현재 convention과 맞다.

## 4. B. Assembly

asmdef 38개, asmref 0개.

| asmdef | path | root namespace | 주요 references | Editor only? | Test? |
|---|---|---|---|---|---|
| Game.Core.Runtime | `Assets/_Game/Core/Game.Core.Runtime.asmdef` | StarNight.Core | NONE | NO | NO |
| MapAuthoring.Editor | `Assets/_Game/Editor/MapAuthoring/MapAuthoring.Editor.asmdef` | StarNight.MapAuthoring.Editor | Game.Core.Runtime, Game.Map.Runtime, Game.Stage.Runtime, Game.Interaction.Runtime, Game.Tools.Runtime | YES | NO |
| MapAuthoring.Tests.EditMode | `Assets/_Game/Editor/MapAuthoring/Tests/EditMode/MapAuthoring.Tests.EditMode.asmdef` | StarNight.MapAuthoring.Tests | Game.Map.Runtime, Game.Stage.Runtime, MapAuthoring.Editor, UnityEditor.TestRunner, UnityEngine.TestRunner | YES | YES |
| ToolAuthoring.Tests.EditMode | `Assets/_Game/Editor/ToolAuthoring/Tests/EditMode/ToolAuthoring.Tests.EditMode.asmdef` | StarNight.ToolAuthoring.Tests | Game.Interaction.Runtime, Game.Tools.Runtime, Game.WorldObjects.Runtime, Game.Map.Runtime, ToolAuthoring.Editor, UnityEditor.TestRunner, UnityEngine.TestRunner | YES | YES |
| ToolAuthoring.Editor | `Assets/_Game/Editor/ToolAuthoring/ToolAuthoring.Editor.asmdef` | StarNight.ToolAuthoring.Editor | Game.Interaction.Runtime, Game.Integration.Runtime, Game.Tools.Runtime, Game.WorldObjects.Runtime, Game.Map.Runtime | YES | NO |
| Game.Integration.Editor | `Assets/_Game/Integration/Editor/Game.Integration.Editor.asmdef` | StarNight.Integration.Editor | Game.Integration.Runtime, Game.Stage.Runtime, Game.UI.Runtime, Game.Narrative.Runtime, Unity.TextMeshPro, YarnSpinner.Unity | YES | NO |
| Game.Integration.Runtime | `Assets/_Game/Integration/Runtime/Game.Integration.Runtime.asmdef` | StarNight.Integration | Game.Core.Runtime, Game.Interaction.Runtime, Game.Player.Runtime, Game.Stage.Runtime, Game.UI.Runtime, Game.Narrative.Runtime, Unity.TextMeshPro | NO | NO |
| Game.Integration.Tests.EditMode | `Assets/_Game/Integration/Tests/EditMode/Game.Integration.Tests.EditMode.asmdef` | StarNight.Integration.Tests | Game.Integration.Runtime, Game.Integration.Editor, Game.Stage.Runtime, Game.UI.Runtime, Game.Narrative.Runtime, Unity.TextMeshPro, YarnSpinner.Unity, UnityEditor.TestRunner, UnityEngine.TestRunner | YES | YES |
| Game.Integration.Tests.PlayMode | `Assets/_Game/Integration/Tests/PlayMode/Game.Integration.Tests.PlayMode.asmdef` | StarNight.Integration.Tests | Game.Core.Runtime, Game.Interaction.Runtime, Game.Player.Runtime, Game.Stage.Runtime, Game.UI.Runtime, Game.Narrative.Runtime, Game.Integration.Runtime, Unity.TextMeshPro, YarnSpinner.Unity, UnityEngine.TestRunner | NO | YES |
| Game.Interaction.Runtime | `Assets/_Game/Interaction/Runtime/Game.Interaction.Runtime.asmdef` | StarNight.Interaction | Unity.InputSystem, Game.Map.Runtime | NO | NO |
| Game.Interaction.Tests.EditMode | `Assets/_Game/Interaction/Tests/EditMode/Game.Interaction.Tests.EditMode.asmdef` | StarNight.Interaction.Tests | Game.Interaction.Runtime, Unity.InputSystem | YES | YES |
| Game.Map.Runtime | `Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef` | StarNight.Map | NONE | NO | NO |
| Game.Narrative.Editor | `Assets/_Game/Narrative/Editor/Game.Narrative.Editor.asmdef` | StarNight.Narrative.Editor | Game.Narrative.Runtime, Game.Stage.Runtime, Unity.TextMeshPro, YarnSpinner.Unity | YES | NO |
| Game.Narrative.Runtime | `Assets/_Game/Narrative/Runtime/Game.Narrative.Runtime.asmdef` | StarNight.Narrative | Game.Core.Runtime, Game.Interaction.Runtime, Game.Player.Runtime, Game.Stage.Runtime, Unity.InputSystem, Unity.TextMeshPro, UnityEngine.UI, YarnSpinner.Unity | NO | NO |
| Game.Narrative.Tests.EditMode | `Assets/_Game/Narrative/Tests/EditMode/Game.Narrative.Tests.EditMode.asmdef` | StarNight.Narrative.Tests | Game.Narrative.Runtime, Game.Core.Runtime, Game.Stage.Runtime, YarnSpinner.Unity | YES | YES |
| Game.Narrative.Tests.PlayMode | `Assets/_Game/Narrative/Tests/PlayMode/Game.Narrative.Tests.PlayMode.asmdef` | StarNight.Narrative.Tests | Game.Narrative.Runtime, Game.Core.Runtime, Game.Interaction.Runtime, Game.Player.Runtime, Game.Stage.Runtime, Unity.TextMeshPro, YarnSpinner.Unity | NO | YES |
| Game.Player.Runtime | `Assets/_Game/Player/Runtime/Game.Player.Runtime.asmdef` | StarNight.Player | Game.Core.Runtime, Game.Interaction.Runtime | NO | NO |
| Game.Player.Tests.EditMode | `Assets/_Game/Player/Tests/EditMode/Game.Player.Tests.EditMode.asmdef` | StarNight.Player.Tests | Game.Core.Runtime, Game.Player.Runtime, Game.Interaction.Runtime | YES | YES |
| Game.Player.Tests.PlayMode | `Assets/_Game/Player/Tests/PlayMode/Game.Player.Tests.PlayMode.asmdef` | StarNight.Player.Tests | Game.Core.Runtime, Game.Player.Runtime, Game.Interaction.Runtime | NO | YES |
| Game.Stage.Editor | `Assets/_Game/Stage/Editor/Game.Stage.Editor.asmdef` | StarNight.Stage.Editor | Game.Stage.Runtime | YES | NO |
| Game.Stage.Runtime | `Assets/_Game/Stage/Runtime/Game.Stage.Runtime.asmdef` | StarNight.Stage | Game.Core.Runtime, Game.Interaction.Runtime, Game.Map.Runtime, Game.Player.Runtime | NO | NO |
| Game.Stage.Tests.EditMode | `Assets/_Game/Stage/Tests/EditMode/Game.Stage.Tests.EditMode.asmdef` | StarNight.Stage.Tests | Game.Stage.Runtime, Game.Stage.Editor, Game.Core.Runtime, Game.Player.Runtime, Game.Interaction.Runtime, Game.Tools.Runtime, Game.Map.Runtime | YES | YES |
| Game.Stage.Tests.PlayMode | `Assets/_Game/Stage/Tests/PlayMode/Game.Stage.Tests.PlayMode.asmdef` | StarNight.Stage.Tests | Game.Stage.Runtime, Game.Core.Runtime, Game.Player.Runtime, Game.Interaction.Runtime, Game.Tools.Runtime, Game.Map.Runtime | NO | YES |
| Game.Core.Tests.EditMode | `Assets/_Game/Tests/EditMode/Core/Game.Core.Tests.EditMode.asmdef` | StarNight.Core.Tests | Game.Core.Runtime | YES | YES |
| Game.Map.Tests.EditMode | `Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef` | StarNight.Map.Tests | Game.Map.Runtime, UnityEditor.TestRunner, UnityEngine.TestRunner | YES | YES |
| Game.Core.Tests.PlayMode | `Assets/_Game/Tests/PlayMode/Core/Game.Core.Tests.PlayMode.asmdef` | StarNight.Core.Tests | Game.Core.Runtime, Game.UI.Runtime | NO | YES |
| Game.Map.Tests.PlayMode | `Assets/_Game/Tests/PlayMode/Map/Game.Map.Tests.PlayMode.asmdef` | StarNight.Map.Tests | Game.Map.Runtime, Game.Stage.Runtime | NO | YES |
| Game.Tools.Runtime | `Assets/_Game/Tools/Runtime/Game.Tools.Runtime.asmdef` | StarNight.Tools | Game.Core.Runtime, Game.Interaction.Runtime, Game.Map.Runtime | NO | NO |
| Game.Tools.Tests.EditMode | `Assets/_Game/Tools/Tests/EditMode/Game.Tools.Tests.EditMode.asmdef` | StarNight.Tools.Tests | Game.Tools.Runtime, Game.Core.Runtime, Game.Interaction.Runtime, Game.Map.Runtime, UnityEditor.TestRunner, UnityEngine.TestRunner | YES | YES |
| Game.UI.Editor | `Assets/_Game/UI/Editor/Game.UI.Editor.asmdef` | StarNight.UI.Editor | Game.UI.Runtime, Unity.TextMeshPro | YES | NO |
| Game.UI.Runtime | `Assets/_Game/UI/Game.UI.Runtime.asmdef` | StarNight.UI | Game.Core.Runtime, Game.Interaction.Runtime, Game.Player.Runtime, Game.Stage.Runtime, Unity.InputSystem, Unity.TextMeshPro, UnityEngine.UI | NO | NO |
| Game.UI.Tests.EditMode | `Assets/_Game/UI/Tests/EditMode/Game.UI.Tests.EditMode.asmdef` | StarNight.UI.Tests | Game.UI.Runtime, Game.Stage.Runtime | YES | YES |
| Game.UI.Tests.PlayMode | `Assets/_Game/UI/Tests/PlayMode/Game.UI.Tests.PlayMode.asmdef` | StarNight.UI.Tests | Game.UI.Runtime, Game.Core.Runtime, Game.Interaction.Runtime, Game.Player.Runtime, Game.Stage.Runtime, UnityEngine.UI | NO | YES |
| Game.WorldObjects.Runtime | `Assets/_Game/WorldObjects/Runtime/Game.WorldObjects.Runtime.asmdef` | StarNight.WorldObjects | Game.Interaction.Runtime, Game.Map.Runtime | NO | NO |
| StarNight.Editor | `Assets/StarNight/Scripts/Editor/StarNight.Editor.asmdef` | StarNight.Editor | StarNight.Runtime, Unity.InputSystem, Unity.TextMeshPro, UnityEngine.UI, YarnSpinner.Unity | YES | NO |
| StarNight.Runtime | `Assets/StarNight/Scripts/Runtime/StarNight.Runtime.asmdef` | StarNight | Unity.InputSystem, Unity.TextMeshPro, UnityEngine.UI, YarnSpinner.Unity | NO | NO |
| StarNight.Tests.EditMode | `Assets/StarNight/Scripts/Tests/EditMode/StarNight.Tests.EditMode.asmdef` | StarNight.Tests.EditMode | StarNight.Runtime, Unity.TextMeshPro, UnityEngine.UI, YarnSpinner.Unity | YES | YES |
| StarNight.Tests.PlayMode | `Assets/StarNight/Scripts/Tests/PlayMode/StarNight.Tests.PlayMode.asmdef` | StarNight.Tests.PlayMode | StarNight.Runtime, Unity.TextMeshPro, UnityEngine.UI, YarnSpinner.Unity | NO | YES |

## 5. C. Namespace Convention

판정: `StarNight.*` 기반의 계층형 namespace convention.

선정한 Map 관련 C# 30개 표본의 namespace 집계:

| namespace | files |
|---|---:|
| StarNight.Stage.Layout | 8 |
| StarNight.MapAuthoring.Editor | 5 |
| StarNight.Map | 5 |
| StarNight.Stage.Rooms | 2 |
| StarNight.Generation.P6 | 2 |
| StarNight.Map.Placement | 2 |
| StarNight.Stage.Data | 1 |
| StarNight.Stage.Flow | 1 |
| StarNight.Stage.Streaming | 1 |
| StarNight.Grid | 1 |
| StarNight.Tiles | 1 |
| StarNight.MapHarness.P11 | 1 |

표본에서 Global namespace는 0개였다. asmdef의 `rootNamespace` 38개도 모두 `StarNight` 또는 `StarNight.*` 패턴이다. 새 광역 맵 코드는 `StarNight.Map.WorldGeneration` 하위 namespace가 기존 convention과 충돌 위험을 가장 낮춘다.

## 6. D. Existing Map Systems

292개 후보 중 아래 30개만 내용을 확인했다. 읽지 않은 나머지 262개는 responsibility를 추측하지 않았다.

| class/file | path | responsibility guess based on code | conflict risk |
|---|---|---|---|
| GridCell | `Assets/_Game/Map/Runtime/Core/GridCell.cs` | 정수형 2D 셀 값 객체와 산술/비교/Vector2Int 변환 | MEDIUM |
| CellFootprint | `Assets/_Game/Map/Runtime/Core/CellFootprint.cs` | 요소의 점유·지지·여유·위험·트리거 셀 footprint와 검증 | LOW |
| MapElementRoomContracts | `Assets/_Game/Map/Runtime/Core/MapElementRoomContracts.cs` | 방 상태와 맵 요소 시뮬레이션/영속성 인터페이스 | LOW |
| MapElementDefinition | `Assets/_Game/Map/Runtime/Elements/MapElementDefinition.cs` | 맵 요소 footprint/시각/충돌/행동/배치 등을 담는 ScriptableObject | MEDIUM |
| TileMutationService | `Assets/_Game/Map/Runtime/Elements/TileMutationService.cs` | 도구 반응을 통해 맵 요소 상태 변경 결과를 계산하는 비-MonoBehaviour 서비스 | MEDIUM |
| GridOccupier | `Assets/_Game/Map/Runtime/Placement/GridOccupier.cs` | 방 좌표 기준 셀 점유 claim 생성 | LOW |
| RoomElementRegistry | `Assets/_Game/Map/Runtime/Placement/RoomElementRegistry.cs` | 방 안의 점유 레이어 충돌 검사 및 등록/해제 | LOW |
| StageDefinition | `Assets/_Game/Stage/Runtime/Data/StageDefinition.cs` | Stage 종류, 생성 모드, 방 역할, 연결 조건을 담는 ScriptableObject | MEDIUM |
| StageMapGenerator | `Assets/_Game/Stage/Runtime/Layout/Generation/StageMapGenerator.cs` | seed와 profile/template로 방 그래프 기반 StageGeneratedLayout을 만드는 결정적 생성기 | HIGH |
| StageMapProfile | `Assets/_Game/Stage/Runtime/Layout/Generation/StageMapProfile.cs` | main route 길이, branch/loop 범위, 방 크기/역할/예산 규칙을 담는 ScriptableObject | HIGH |
| StageGeneratedLayout | `Assets/_Game/Stage/Runtime/Layout/Generation/StageGeneratedLayout.cs` | 생성된 방·연결·잠금·요소 슬롯 결과 모델 | HIGH |
| StageLayoutContracts | `Assets/_Game/Stage/Runtime/Layout/StageLayoutContracts.cs` | Region/Room/Traversal/Socket 계약과 방 크기 카탈로그 | HIGH |
| StageLayoutGraphUtility | `Assets/_Game/Stage/Runtime/Layout/StageLayoutGraphUtility.cs` | 방 겹침, 배치 grid snap, socket 방향/호환성 계산 | MEDIUM |
| RoomTemplate | `Assets/_Game/Stage/Runtime/Layout/RoomTemplate.cs` | 방 크기, socket, budget, content tag, geometry hash ScriptableObject | HIGH |
| RoomInteriorGenerator | `Assets/_Game/Stage/Runtime/Layout/Generation/RoomInteriorGenerator.cs` | seed 기반 방 내부 레이아웃 생성 | HIGH |
| RoomInteriorValidator | `Assets/_Game/Stage/Runtime/Layout/Generation/RoomInteriorValidator.cs` | 생성된 방 내부 레이아웃 invariant 검증 | MEDIUM |
| StageAssembler | `Assets/_Game/Stage/Runtime/Flow/StageAssembler.cs` | RoomRuntime들을 StageAssemblyResult로 조립 | MEDIUM |
| StageRoomGraph | `Assets/_Game/Stage/Runtime/Rooms/StageRoomGraph.cs` | 방/간선 등록, 인접성, 다음 경로 탐색 | MEDIUM |
| RoomStreamingManager | `Assets/_Game/Stage/Runtime/Streaming/RoomStreamingManager.cs` | 방 warm-load/activate와 runtime 상태 관리 | MEDIUM |
| RoomGridTransform | `Assets/_Game/Stage/Runtime/Rooms/RoomGridTransform.cs` | 방 local cell과 world position 상호 변환 | HIGH |
| MapElementBakePipeline | `Assets/_Game/Editor/MapAuthoring/Scripts/Baking/MapElementBakePipeline.cs` | 맵 요소 authoring source를 runtime definition으로 bake | MEDIUM |
| StageLayoutSnapshotBaker | `Assets/_Game/Editor/MapAuthoring/Scripts/Baking/StageLayoutSnapshotBaker.cs` | 현재 Scene의 StageLayoutSnapshot bake와 asset path 생성 | MEDIUM |
| MapElementValidator | `Assets/_Game/Editor/MapAuthoring/Scripts/Validation/MapElementValidator.cs` | 맵 요소 source/baked definition 검증 및 제한된 auto-fix | MEDIUM |
| StageLayoutValidator | `Assets/_Game/Editor/MapAuthoring/Scripts/Validation/StageLayoutValidator.cs` | Scene의 방/연결 구조와 snap 상태 검증 | MEDIUM |
| StageSeedBatchValidator | `Assets/_Game/Editor/MapAuthoring/Scripts/Validation/StageSeedBatchValidator.cs` | seed 집합에 대한 Stage 생성 approval/batch 검증과 보고서 출력 | MEDIUM |
| GridWorld (legacy) | `Assets/StarNight/Scripts/Runtime/Grid/GridWorld.cs` | MonoBehaviour 기반 bounded cell grid, solid/hazard/occupancy, 좌표 변환 | HIGH |
| P6RoomGraphGenerator (legacy) | `Assets/StarNight/Scripts/Runtime/Generation/P6/P6RoomGraphGenerator.cs` | P6 Moon Palace용 deterministic room graph 생성 | HIGH |
| P6RoomGraphValidator (legacy) | `Assets/StarNight/Scripts/Runtime/Generation/P6/P6RoomGraphValidator.cs` | P6 생성 결과의 route/content invariant 검증 | HIGH |
| TileMutationService (legacy) | `Assets/StarNight/Scripts/Runtime/Tiles/TileMutationService.cs` | GridWorld의 tile 변경 queue, 보호 셀, 출구 도달성 검증 | HIGH |
| P11MapStageHarness2D (legacy) | `Assets/StarNight/Scripts/Runtime/MapHarness/P11/P11MapStageHarness2D.cs` | P11 방/경로/tool-free/cue 계약을 구성하고 검증하는 runtime harness | MEDIUM |

핵심 구분: 기존 `_Game/Stage` 생성기는 방 그래프/방 내부 단위이고, 새 TASK 묶음은 624×416 월드와 13×13 sector, 12×8 microchunk를 위한 광역 생성기다. 기능 목적이 다르더라도 `Map`, `Stage`, `Grid`, `Generator`, `Room`, `TileMutationService` 명칭과 좌표 개념이 겹치므로 명시적 경계가 필요하다.

## 7. E. Test Structure

### Existing EditMode locations

- `Assets/_Game/Tests/EditMode/Map` — `Game.Map.Tests.EditMode`, `Game.Map.Runtime` 참조
- `Assets/_Game/Editor/MapAuthoring/Tests/EditMode` — `MapAuthoring.Tests.EditMode`, runtime + editor authoring 검증
- `Assets/_Game/Stage/Tests/EditMode` — `Game.Stage.Tests.EditMode`
- 기능별 `Assets/_Game/*/Tests/EditMode`
- 레거시 `Assets/StarNight/Scripts/Tests/EditMode` — `StarNight.Tests.EditMode`

### Existing PlayMode locations

- `Assets/_Game/Tests/PlayMode/Map` — `Game.Map.Tests.PlayMode`, `Game.Map.Runtime`과 `Game.Stage.Runtime` 참조
- `Assets/_Game/Stage/Tests/PlayMode` — `Game.Stage.Tests.PlayMode`
- 기능별 `Assets/_Game/*/Tests/PlayMode`
- 레거시 `Assets/StarNight/Scripts/Tests/PlayMode` — `StarNight.Tests.PlayMode`

### Recommended test placement

- 광역 생성기의 순수 데이터/결정성/invariant EditMode tests: `Assets/_Game/Tests/EditMode/Map/WorldGeneration`
- Unity runtime adapter가 실제로 필요한 경우에만 PlayMode tests: `Assets/_Game/Tests/PlayMode/Map/WorldGeneration`
- CSV importer/EditorWindow/bake 도구 tests: `Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration`

신규 테스트 asmdef는 현재 구조상 필요하지 않다. 기존 Map test asmdef를 사용하면 된다.

## 8. F. Recommended Module Placement

아래는 제안만 하며 이 TASK에서 생성하지 않았다.

| concern | recommended path | namespace | basis |
|---|---|---|---|
| Runtime Map Domain | `Assets/_Game/Map/Runtime/WorldGeneration/Domain` | `StarNight.Map.WorldGeneration` | 기존 `Game.Map.Runtime`과 기능별 하위 폴더 convention |
| Runtime Map Data | `Assets/_Game/Map/Runtime/WorldGeneration/Data` | `StarNight.Map.WorldGeneration.Data` | runtime domain 가까이에 typed input/result 모델 배치 |
| Runtime Map Generation | `Assets/_Game/Map/Runtime/WorldGeneration/Generation` | `StarNight.Map.WorldGeneration.Generation` | 기존 Stage room generator와 경로/namespace 분리 |
| Runtime Validation | `Assets/_Game/Map/Runtime/WorldGeneration/Validation` | `StarNight.Map.WorldGeneration.Validation` | 생성 후 invariant 검증을 후처리 보정과 분리 |
| Editor Map Tools | `Assets/_Game/Editor/MapAuthoring/WorldGeneration` | `StarNight.MapAuthoring.Editor.WorldGeneration` | 기존 Editor-only MapAuthoring assembly 재사용 |
| Map EditMode Tests | `Assets/_Game/Tests/EditMode/Map/WorldGeneration` | `StarNight.Map.Tests.WorldGeneration` | 기존 Map EditMode test assembly 재사용 |
| Map PlayMode Tests | `Assets/_Game/Tests/PlayMode/Map/WorldGeneration` | `StarNight.Map.Tests.WorldGeneration` | 기존 Map PlayMode test assembly 재사용 |
| MapDesign Authoring Data | `Assets/_Game/Map/Data/WorldGeneration/Authoring` | N/A | 기존 `Assets/_Game/Map/Data` convention; CSV를 source of truth로 유지 |

배치 원칙:

- 새 광역 생성기는 기존 `StageMapGenerator`나 레거시 `P6RoomGraphGenerator`를 호출하지 않는다.
- `StarNight.Map.WorldGeneration` 하위 namespace로 기존 `StarNight.Map`, `StarNight.Stage.Layout`, `StarNight.Generation.P6`와 의미를 분리한다.
- 월드/sector/microchunk 좌표 변환의 단일 진입점을 새 Domain에 두고 기존 `RoomGridTransform`/`GridWorld` 변환을 복사하지 않는다.
- 기존 ScriptableObject 기반 Stage/MapElement 정의를 CSV authoring source of truth로 재사용하지 않는다.
- Scene/Prefab/Tile Asset/Addressables 변경은 해당 후속 TASK가 명시하기 전까지 하지 않는다.

## 9. G. Assembly Plan

### Decision

현재 단계에서는 새 asmdef가 필요하지 않다.

### Proposed reuse

| concern | assembly | required references |
|---|---|---|
| Runtime Domain/Data/Generation/Validation | `Game.Map.Runtime` | 기존 그대로 `NONE` |
| Editor importer/preview/validation | `MapAuthoring.Editor` | 기존 `Game.Map.Runtime` 참조 재사용 |
| Runtime EditMode tests | `Game.Map.Tests.EditMode` | 기존 `Game.Map.Runtime`, Unity test runner 참조 재사용 |
| Editor tool EditMode tests | `MapAuthoring.Tests.EditMode` | 기존 `Game.Map.Runtime`, `MapAuthoring.Editor`, Unity test runner 참조 재사용 |
| Runtime adapter PlayMode tests | `Game.Map.Tests.PlayMode` | 기존 `Game.Map.Runtime`, `Game.Stage.Runtime` 참조 재사용 |

근거:

- `Game.Map.Runtime`은 현재 assembly reference가 0개라 pure domain/generation 코드의 의존성을 낮게 유지할 수 있다.
- `Game.Stage.Runtime`, `Game.Interaction.Runtime`, `Game.Tools.Runtime`, `Game.WorldObjects.Runtime` 등이 이미 `Game.Map.Runtime`에 의존한다. 반대 방향의 Stage 의존성을 새 광역 생성기 안에 추가하면 순환 참조가 생길 수 있으므로 금지해야 한다.
- Editor와 Test assembly가 이미 정확한 Runtime assembly를 참조하고 있다.

MAP00_02에서 별도 asmdef를 다시 고려해야 하는 조건은 하나다: 새 생성기를 기존 Map 요소 코드의 UnityEngine/MonoBehaviour 표면과 compile-time으로 강하게 격리해야 한다는 명시적 요구가 생기는 경우. 현재 audit 증거만으로는 기존 assembly 재사용이 더 자연스럽다.

## 10. H. Collision / Risk

| risk | level | evidence | MAP00_02 guardrail |
|---|---|---|---|
| 기존 Stage 생성기와 새 광역 생성기의 개념 중복 | HIGH | `StageMapGenerator`, `StageMapProfile`, `StageGeneratedLayout`, `RoomInteriorGenerator` 존재 | 새 코드 경로/namespace/type에 `WorldGeneration`을 명시하고 기존 Stage generator를 호출하거나 수정하지 않음 |
| 레거시 생성기와 새 생성기의 병존 | HIGH | `StarNight.Runtime` 안에 P6 graph generator/validator, P11 map harness 존재 | 레거시 assembly를 참조하지 않고 삭제/마이그레이션은 별도 승인 TASK로 분리 |
| 좌표 변환 중복 | HIGH | `GridCell`, `RoomGridTransform`, 레거시 `GridWorld`가 각기 좌표 개념 보유 | 새 World/Sector/MicroChunk 변환은 단일 domain entry point로 만들고 기존 변환 코드를 복사하지 않음 |
| `12×8` 의미 충돌 | HIGH | 기존 `RoomSizeCatalog.Micro = 12×8`; 새 동결 규칙의 MicroChunk도 12×8 | 기존 `Micro` room size를 microchunk로 간주하거나 재사용하지 않음; 타입명으로 의미 분리 |
| Tile mutation 명칭 충돌 | HIGH | `StarNight.Map.TileMutationService`와 `StarNight.Tiles.TileMutationService` 두 타입 존재 | 새 지형 bake/streaming 타입에 같은 이름을 사용하지 않음 |
| ScriptableObject가 source of truth로 오인될 위험 | MEDIUM | `StageMapProfile`, `RoomTemplate`, `MapElementDefinition` 존재 | 신규 정적 authoring 원본은 CSV로 유지; SO는 허용된 후속 TASK에서 import cache/preview로만 사용 |
| Game.Map.Runtime의 넓은 downstream 영향 | MEDIUM | Stage/Interaction/Tools/WorldObjects assembly가 Game.Map.Runtime 참조 | public API 최소화, 새 하위 namespace 사용, Stage reference 역추가 금지 |
| Editor bake/validation 명칭 중복 | MEDIUM | 기존 MapAuthoring bake/validator/seed batch validator 존재 | 새 도구는 기존 `MapAuthoring.Editor` 아래 `WorldGeneration` 하위 경로/namespace로 분리 |
| Addressables 기반 위치를 추측할 위험 | LOW | Addressables package NOT FOUND | Addressables 연동을 제안/구현하지 않음 |

사용자의 최신 지시인 “기존 로직은 다 폐기”는 새 광역 생성기가 기존 generator 로직을 기반으로 하지 않는다는 방향으로 반영했다. 다만 이 READ-ONLY TASK는 기존 파일 삭제/수정 권한이 없으므로 실제 제거 또는 migration은 수행하지 않았다.

## 11. Unity MCP

```text
Unity Editor Reachable: NOT CHECKED
Existing Console Errors: NOT CHECKED
```

현재 세션에 Unity MCP server/resource/tool이 노출되지 않아 Editor state와 Console을 조회할 수 없었다. 코드/asset을 변경하지 않았으므로 compile, Asset Refresh, EditMode/PlayMode test를 강제로 유발하지 않았다.

## 12. Actual Content Read

### MCP Starter / Task

- `MapDesign/MCP/00_MCP_ENTRYPOINT.md`
- `MapDesign/MCP/01_PROJECT_LOCKED_RULES.md`
- `MapDesign/MCP/02_MCP_WORK_RULES.md`
- `MapDesign/MCP/03_DATA_CSV_RULES.md`
- `MapDesign/MCP/04_UNITY_MCP_RULES.md`
- `MapDesign/MCP/05_CHANGE_CONTROL_RULES.md`
- `MapDesign/MCP/06_IMPLEMENTATION_STATUS.md`
- `MapDesign/MCP/TASKS/MAP00_01_PROJECT_AUDIT.md`

### Unity / Package

- `ProjectSettings/ProjectVersion.txt`
- `Packages/manifest.json`
- `Packages/packages-lock.json`

### Assembly

- 위 Assembly 표의 asmdef 38개 전체
- asmref: NOT FOUND

### Map-related C# selected sample (30/292)

- `Assets/_Game/Map/Runtime/Core/GridCell.cs`
- `Assets/_Game/Map/Runtime/Core/CellFootprint.cs`
- `Assets/_Game/Map/Runtime/Core/MapElementRoomContracts.cs`
- `Assets/_Game/Map/Runtime/Elements/MapElementDefinition.cs`
- `Assets/_Game/Map/Runtime/Elements/TileMutationService.cs`
- `Assets/_Game/Map/Runtime/Placement/GridOccupier.cs`
- `Assets/_Game/Map/Runtime/Placement/RoomElementRegistry.cs`
- `Assets/_Game/Stage/Runtime/Data/StageDefinition.cs`
- `Assets/_Game/Stage/Runtime/Layout/Generation/StageMapGenerator.cs`
- `Assets/_Game/Stage/Runtime/Layout/Generation/StageMapProfile.cs`
- `Assets/_Game/Stage/Runtime/Layout/Generation/StageGeneratedLayout.cs`
- `Assets/_Game/Stage/Runtime/Layout/StageLayoutContracts.cs`
- `Assets/_Game/Stage/Runtime/Layout/StageLayoutGraphUtility.cs`
- `Assets/_Game/Stage/Runtime/Layout/RoomTemplate.cs`
- `Assets/_Game/Stage/Runtime/Layout/Generation/RoomInteriorGenerator.cs`
- `Assets/_Game/Stage/Runtime/Layout/Generation/RoomInteriorValidator.cs`
- `Assets/_Game/Stage/Runtime/Flow/StageAssembler.cs`
- `Assets/_Game/Stage/Runtime/Rooms/StageRoomGraph.cs`
- `Assets/_Game/Stage/Runtime/Streaming/RoomStreamingManager.cs`
- `Assets/_Game/Stage/Runtime/Rooms/RoomGridTransform.cs`
- `Assets/_Game/Editor/MapAuthoring/Scripts/Baking/MapElementBakePipeline.cs`
- `Assets/_Game/Editor/MapAuthoring/Scripts/Baking/StageLayoutSnapshotBaker.cs`
- `Assets/_Game/Editor/MapAuthoring/Scripts/Validation/MapElementValidator.cs`
- `Assets/_Game/Editor/MapAuthoring/Scripts/Validation/StageLayoutValidator.cs`
- `Assets/_Game/Editor/MapAuthoring/Scripts/Validation/StageSeedBatchValidator.cs`
- `Assets/StarNight/Scripts/Runtime/Grid/GridWorld.cs`
- `Assets/StarNight/Scripts/Runtime/Generation/P6/P6RoomGraphGenerator.cs`
- `Assets/StarNight/Scripts/Runtime/Generation/P6/P6RoomGraphValidator.cs`
- `Assets/StarNight/Scripts/Runtime/Tiles/TileMutationService.cs`
- `Assets/StarNight/Scripts/Runtime/MapHarness/P11/P11MapStageHarness2D.cs`

## 13. Done Conditions

- [x] Unity Version 확인
- [x] asmdef/asmref 목록과 내용 확인
- [x] Assets depth 1~3 구조 확인
- [x] Map 관련 기존 코드 후보 조사
- [x] Test 구조 확인
- [x] namespace convention 확인
- [x] 새 Map 모듈의 권장 위치 제안
- [x] asmdef 계획 제안
- [x] 충돌 위험 기록
- [x] 프로젝트 구현 파일 수정 0개
- [x] Audit Result 파일 1개만 생성

## 14. Completion Report

```text
TASK: MAP00_01_PROJECT_AUDIT
STATUS: PASS

READ:
- MCP Starter/Current Task 8 files
- ProjectSettings/ProjectVersion.txt
- Packages/manifest.json
- Packages/packages-lock.json
- asmdef 38 files; asmref 0 files
- Map-related C# candidate contents 30 of 292 files
- Assets depth 1~3 folder/file paths only

CHANGED:
- NONE

CREATED:
- REPORTS/MAP00_01_PROJECT_AUDIT_RESULT.md

TEST:
- Read-only audit done-condition verification: PASS
- Unity EditMode/PlayMode tests: NOT RUN (no code change; Unity MCP unavailable)

UNITY:
- Unity Version: 6000.3.8f1
- Compile Errors: NOT CHECKED
- Relevant Warnings: NOT CHECKED
- EditMode Tests: NOT RUN
- PlayMode Tests: NOT RUN
- Scene/Prefab Changes: NONE

OUT_OF_SCOPE_FINDINGS:
- NONE

NEXT TASK READY:
MAP00_02_FOLDER_AND_ASMDEF_PLAN = YES

NEXT:
- NO — this session does not start MAP00_02 automatically.

Recommended Commit:
docs(map): add MAP00_01 project audit result
```
