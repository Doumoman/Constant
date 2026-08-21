# MAP00_02 — Folder / Namespace / Assembly Boundary Plan

## TASK TYPE

```text
READ-ONLY ARCHITECTURE PLAN
```

이 TASK는 기존 프로젝트에 코드를 추가하지 않는다.

실제 `Assets/` 폴더 생성도 하지 않는다.
asmdef를 생성하거나 수정하지 않는다.

결과 문서 하나만 생성한다.

---

# 1. Objective

`MAP00_01_PROJECT_AUDIT_RESULT.md`에서 확인된 실제 Unity 프로젝트 구조를 기준으로
새로운 `624×416 / 13×13 sector / 12×8 microchunk` 광역 월드 생성기를
기존 Stage/Map/Legacy 생성기와 충돌 없이 구현할 정확한 위치를 동결한다.

이번 TASK가 끝나면 이후 MCP 작업은
"어느 폴더에 어떤 파일을 만들어야 하는가"를 더 이상 추측해서는 안 된다.

---

# 2. 반드시 먼저 읽을 파일

MCP Starter:

- `00_MCP_ENTRYPOINT.md`
- `01_PROJECT_LOCKED_RULES.md`
- `02_MCP_WORK_RULES.md`
- `03_DATA_CSV_RULES.md`
- `04_UNITY_MCP_RULES.md`
- `05_CHANGE_CONTROL_RULES.md`
- `06_IMPLEMENTATION_STATUS.md`
- 현재 이 TASK

Audit:

- `REPORTS/MAP00_01_PROJECT_AUDIT_RESULT.md`

---

# 3. 추가 READ ALLOWLIST

Audit에서 이미 확인된 사실을 다시 전면 조사하지 않는다.

아래 파일은 assembly 경계를 최종 확인하기 위해서만 읽을 수 있다.

## Runtime Assembly

- `Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef`

## Editor Assembly

- `Assets/_Game/Editor/MapAuthoring/MapAuthoring.Editor.asmdef`

## Tests

- `Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef`
- `Assets/_Game/Tests/PlayMode/Map/Game.Map.Tests.PlayMode.asmdef`
- `Assets/_Game/Editor/MapAuthoring/Tests/EditMode/MapAuthoring.Tests.EditMode.asmdef`

## Existing collision references

다음 파일은 타입/namespace 충돌을 피하기 위한 이름 확인 용도로만 읽을 수 있다.

- `Assets/_Game/Map/Runtime/Core/GridCell.cs`
- `Assets/_Game/Stage/Runtime/Layout/Generation/StageMapGenerator.cs`
- `Assets/_Game/Stage/Runtime/Layout/Generation/StageMapProfile.cs`
- `Assets/_Game/Stage/Runtime/Layout/Generation/StageGeneratedLayout.cs`
- `Assets/_Game/Stage/Runtime/Layout/RoomTemplate.cs`
- `Assets/_Game/Stage/Runtime/Rooms/RoomGridTransform.cs`
- `Assets/StarNight/Scripts/Runtime/Grid/GridWorld.cs`
- `Assets/StarNight/Scripts/Runtime/Generation/P6/P6RoomGraphGenerator.cs`
- `Assets/StarNight/Scripts/Runtime/MapHarness/P11/P11MapStageHarness2D.cs`

이 목록 밖의 프로젝트 C# 본문을 읽지 않는다.

---

# 4. WRITE ALLOWLIST

아래 파일 하나만 생성한다.

```text
REPORTS/MAP00_02_FOLDER_AND_ASMDEF_PLAN_RESULT.md
```

수정 금지:

- `Assets/**`
- `Packages/**`
- `ProjectSettings/**`
- 기존 asmdef
- 기존 C#
- 기존 Scene/Prefab
- CSV
- `06_IMPLEMENTATION_STATUS.md`

---

# 5. 동결해야 하는 Runtime Folder Layout

아래 구조를 기본안으로 검증한다.

실제 프로젝트 convention과 충돌이 없다면 그대로 `APPROVED`한다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/
├─ Domain/
├─ Data/
├─ Generation/
├─ Validation/
├─ Random/
└─ Diagnostics/
```

각 폴더의 책임은 다음과 같이 고정하는 것을 검토한다.

## Domain

Unity Scene/MonoBehaviour와 독립적인 핵심 값 객체와 좌표 규칙.

예상 범위:

```text
WorldConstants
WorldTileCoordinate
SectorCoordinate
SectorLocalCoordinate
MicroChunkCoordinate
MicroChunkLocalCoordinate
WorldSector
RouteType
Direction4
```

주의:
- 실제 타입명은 이 TASK에서 생성하지 않는다.
- 기존 `GridCell`을 복사하거나 이름만 바꿔 재구현하지 않는다.
- 기존 Stage의 Room 좌표 개념과 분리한다.

## Data

CSV import 이후 Runtime generation이 소비할 typed read model.

예상 범위:

```text
WorldProfileData
BiomeRuleData
SectorRouteMaskData
SpecialSiteData
MicroChunkData
BoundaryData
```

주의:
- ScriptableObject는 Source of Truth가 아니다.
- CSV schema는 아직 만들지 않는다.

## Generation

월드 생성 Pass 및 solver.

예상 하위 역할:

```text
WorldGrid
SiteReservation
BiomePatch
MandatoryRoute
OptionalOverlay
SectorAssembly
```

주의:
- `StageMapGenerator` 재사용 금지
- `P6RoomGraphGenerator` 재사용 금지

## Validation

생성 결과 invariant 검사.

예상 범위:

```text
WorldGridValidator
SiteReservationValidator
MandatoryReachabilityValidator
BiomePatchValidator
SectorAssemblyValidator
```

후처리 auto-fix와 분리한다.

## Random

결정적 RNG stream 및 stable candidate selection.

예상 범위:

```text
GenerationSeed
GenerationRngStream
StableCandidateSelector
```

하나의 global RNG를 공유하지 않는다.

## Diagnostics

순수 생성 결과의 debug snapshot / diagnostic record.

Unity Editor rendering은 여기에 넣지 않는다.

---

# 6. 동결해야 하는 Editor Folder Layout

기본안:

```text
Assets/_Game/Editor/MapAuthoring/WorldGeneration/
├─ Import/
├─ Validation/
├─ Preview/
└─ Windows/
```

책임:

## Import

```text
CSV parsing adapter
CSV -> typed runtime data import
foreign-key validation bridge
```

## Validation

```text
authoring data validation
batch seed validation command
```

Runtime Validation 코드를 복제하지 않는다.

## Preview

```text
13×13 sector visualization
biome/debug overlays
microchunk preview adapter
```

## Windows

```text
WorldGenerationWindow
MicroChunkAuthoringWindow
SeedReplayWindow
```

실제 창은 후속 TASK에서만 만든다.

---

# 7. 동결해야 하는 Test Folder Layout

## Runtime Pure/EditMode

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/
├─ Domain/
├─ Data/
├─ Generation/
├─ Validation/
└─ Determinism/
```

Assembly:

```text
Game.Map.Tests.EditMode
```

## Runtime Adapter PlayMode

```text
Assets/_Game/Tests/PlayMode/Map/WorldGeneration/
```

Assembly:

```text
Game.Map.Tests.PlayMode
```

PlayMode 테스트는 실제 Unity runtime adapter가 필요한 경우에만 만든다.

## Editor Tools Test

```text
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/
├─ Import/
├─ Validation/
└─ Preview/
```

Assembly:

```text
MapAuthoring.Tests.EditMode
```

---

# 8. 동결해야 하는 Authoring Data Folder Layout

기본안:

```text
Assets/_Game/Map/Data/WorldGeneration/
├─ Authoring/
│  ├─ World/
│  ├─ Route/
│  ├─ Biome/
│  ├─ SpecialMap/
│  ├─ Village/
│  ├─ MicroChunk/
│  ├─ Boundary/
│  ├─ Population/
│  └─ Items/
│
├─ Imported/
└─ GeneratedDebug/
```

## Authoring

사람이 수정하는 CSV Source of Truth.

## Imported

후속 TASK에서 허용할 경우 생성되는 import cache.
사람이 수동 편집하지 않는다.

## GeneratedDebug

seed 재현/QA용 생성 CSV 및 snapshot.
Authoring 입력으로 재사용하지 않는다.

---

# 9. Namespace Boundary

기본 root:

```text
StarNight.Map.WorldGeneration
```

권장:

```text
StarNight.Map.WorldGeneration.Domain
StarNight.Map.WorldGeneration.Data
StarNight.Map.WorldGeneration.Generation
StarNight.Map.WorldGeneration.Validation
StarNight.Map.WorldGeneration.Random
StarNight.Map.WorldGeneration.Diagnostics
```

Editor:

```text
StarNight.MapAuthoring.Editor.WorldGeneration
StarNight.MapAuthoring.Editor.WorldGeneration.Import
StarNight.MapAuthoring.Editor.WorldGeneration.Validation
StarNight.MapAuthoring.Editor.WorldGeneration.Preview
```

Tests:

```text
StarNight.Map.Tests.WorldGeneration
```

테스트 하위 namespace는 파일 역할에 맞게 추가할 수 있다.

---

# 10. Assembly Boundary

Audit 결과를 기준으로 아래 안을 검증한다.

## Runtime

```text
Game.Map.Runtime
```

새 asmdef를 만들지 않는다.

중요:

`Game.Map.Runtime`은 Stage assembly를 참조하지 않는다.

새 WorldGeneration runtime 코드에서 다음 참조를 추가하지 않는다.

```text
Game.Stage.Runtime
StarNight.Runtime (legacy)
MapAuthoring.Editor
UnityEditor
```

## Editor

```text
MapAuthoring.Editor
```

기존 `Game.Map.Runtime` 참조를 사용한다.

## EditMode Tests

```text
Game.Map.Tests.EditMode
```

## PlayMode Tests

```text
Game.Map.Tests.PlayMode
```

## Editor Tests

```text
MapAuthoring.Tests.EditMode
```

---

# 11. Forbidden Dependency Directions

결과 문서에 반드시 아래 dependency rule을 명시한다.

허용:

```text
Game.Map.Runtime / WorldGeneration
        ↑
MapAuthoring.Editor
        ↑
MapAuthoring.Tests.EditMode
```

및

```text
Game.Map.Runtime / WorldGeneration
        ↑
Game.Map.Tests.EditMode
```

금지:

```text
Game.Map.Runtime
    -> Game.Stage.Runtime

Game.Map.Runtime
    -> StarNight.Runtime legacy

Game.Map.Runtime
    -> UnityEditor

WorldGeneration.Domain
    -> MonoBehaviour/Scene object ownership

WorldGeneration.Generation
    -> StageMapGenerator
```

기존 다른 assembly가 `Game.Map.Runtime`을 참조하는 것은 이번 TASK 범위 밖이며 유지한다.

---

# 12. Naming Collision Guardrails

Audit에서 확인된 충돌을 결과에 반영한다.

다음 이름은 새 광역 시스템에서 그대로 사용하지 않는다.

```text
GridWorld
StageMapGenerator
StageMapProfile
StageGeneratedLayout
RoomTemplate
RoomGridTransform
P6RoomGraphGenerator
TileMutationService
```

특히 기존 `12×8 Micro room`과 새 `12×8 MicroChunk`를 혼동하지 않는다.

금지:

```text
RoomTemplate == MicroChunkTemplate
RoomSizeCatalog.Micro == MicroChunk size
```

광역 시스템의 타입 이름에는 가능한 범위에서
`World`, `Sector`, `MicroChunk`, `WorldGeneration`
도메인 의미를 명시한다.

---

# 13. Result Document Required Structure

`REPORTS/MAP00_02_FOLDER_AND_ASMDEF_PLAN_RESULT.md`에는 정확히 아래 섹션을 만든다.

```text
1. STATUS
2. Runtime Folder Layout
3. Editor Folder Layout
4. Test Folder Layout
5. Authoring Data Folder Layout
6. Namespace Matrix
7. Assembly Matrix
8. Dependency Rules
9. Naming Collision Rules
10. Files/Folders MAP00_03 May Create
11. Files MAP00_03 Must Not Touch
12. Risks Remaining
13. DONE CONDITIONS
14. NEXT TASK READY
```

---

# 14. "Files/Folders MAP00_03 May Create" 필수 목록

MAP00_03이 실제 생성할 수 있는 경로를 정확히 나열한다.

폴더만 허용할지,
placeholder `.gitkeep` 또는 README를 허용할지 구분한다.

중요:
- MAP00_03에서는 실제 generation C# 구현을 시작하지 않는다.
- 새 asmdef 생성 금지안이 유지되는지 명시한다.
- CSV schema 생성도 아직 금지한다.

---

# 15. DONE CONDITIONS

아래가 모두 만족되어야 PASS.

- [ ] Runtime folder layout 확정
- [ ] Editor folder layout 확정
- [ ] EditMode/PlayMode/Editor test 위치 확정
- [ ] Authoring CSV root 위치 확정
- [ ] `StarNight.Map.WorldGeneration.*` namespace 경계 확정
- [ ] 기존 assembly 재사용 여부 확정
- [ ] 새 asmdef 필요 여부 확정
- [ ] Runtime -> Stage 역참조 금지 명시
- [ ] Legacy generator 참조 금지 명시
- [ ] 기존 12×8 Micro room과 새 MicroChunk 의미 분리 명시
- [ ] MAP00_03 생성 허용 경로 목록 작성
- [ ] 프로젝트 구현 파일 수정 0개
- [ ] 결과 문서 1개만 생성

---

# 16. PASS 후

PASS해도 MAP00_03을 자동 실행하지 않는다.

마지막:

```text
NEXT TASK READY:
MAP00_03_CREATE_MAP_MODULE_STRUCTURE = YES / NO
```

를 보고하고 종료한다.
