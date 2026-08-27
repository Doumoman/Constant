# MAP00_07 — Implement Coordinate Conversions

```yaml
status_control:
  task_key: MAP00_07_IMPLEMENT_COORDINATE_CONVERSIONS
  result_file: REPORTS/MAP00_07_IMPLEMENT_COORDINATE_CONVERSIONS_RESULT.md
```

## TASK TYPE

```text
RUNTIME COORDINATE CONTRACT + EDITMODE API TEST
```

## Objective

광역 WorldGeneration의 `WorldTileCoord`, `SectorCoord`, `MicroChunkCoord`, `LocalTileCoord`를 `WorldCoordinateUtility` 단일 진입점에서 검증하고 상호 변환한다.

이 TASK는 네 좌표 공간의 범위, `TryCreate`, Sector+MicroChunk+LocalTile→World 합성, World→Sector+MicroChunk+LocalTile 분해, invalid 입력 거부 계약을 구현한다. 모든 169 sector × 16 microchunk 경계 조합과 네 모서리를 전수 검사하는 exhaustive fixture는 MAP00_08에서 별도로 만든다.

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
12. `REPORTS/MAP00_06_IMPLEMENT_COORDINATE_VALUE_TYPES_RESULT.md`

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
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/WorldGenConstantsTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/CoordinateValueTypeTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/WorldGenerationModuleStructureTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/WorldGenerationRuntimeBoundaryTests.cs`
- `Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/WorldGenerationEditorBoundaryTests.cs`
- 아래 WRITE ALLOWLIST의 신규 파일을 생성 후 재검증하기 위한 본문

제한적 검색 허용:

- 승인된 WorldGeneration 디렉터리 36개의 존재 여부와 직계 파일명
- `Assets/_Game/Map/Data/WorldGeneration/Authoring/**/*.csv` 경로만 열거
- Runtime WorldGeneration 아래 기존 `*.cs` 경로와 선언된 namespace/type 이름만 확인
- 프로젝트 전체 `.meta`에서 `guid:` 값만 추출하는 GUID 중복 검사
- 작업 전후 변경 파일 경로 확인
- Unity Console의 현재 compile error와 이 TASK로 발생한 warning 확인

기존 value type/constant/architecture test 실행 중 테스트 코드가 승인 범위를 검사하는 것은 허용한다.

금지:

- 승인되지 않은 프로젝트 C# 본문 스캔
- `Assets/_Legacy/**` 본문 열람 또는 수정
- Scene/Prefab YAML 열람
- CSV/GDD/과거 하네스 본문 열람
- 테스트 통과를 위해 기존 코드를 수정하는 행위

## Master Backlog Check

`MASTER_IMPLEMENTATION_TASK_LIST.md`에서 다음을 확인한다.

```text
MAP00_01~06 = COMPLETE
MAP00_07_IMPLEMENT_COORDINATE_CONVERSIONS = next/current
MAP00_08~10 = LOCKED
MAP01_01 premade patch = HOLD / DO NOT RUN
전체 Task = 205
```

하나라도 다르면 임의 보정하지 말고 `BLOCKED` Result를 작성한다.

## Preflight Preservation Check

MAP00_06 Result의 PASS와 아래 항목을 확인한다.

필수 디렉터리:

```text
Assets/_Game/Map/Runtime/WorldGeneration/
Assets/_Game/Map/Runtime/WorldGeneration/Domain/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/
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

필수 MAP00_04~06 파일:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/WorldGenerationModuleStructureTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/WorldGenerationRuntimeBoundaryTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/WorldGenerationEditorBoundaryTests.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldTileCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/MicroChunkCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/LocalTileCoord.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/WorldGenConstantsTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/CoordinateValueTypeTests.cs
```

위 Runtime/test C# 각각의 대응 `.meta`도 존재해야 한다.

추가 순서 검증:

- `WorldGenConstants`의 `public const int` 15개 계약을 유지해야 한다.
- 네 좌표 타입은 MAP00_06의 readonly/raw X/Y/equality/hash/string 계약을 유지해야 한다.
- MAP00_06 Result는 신규 12/12, constant 6/6, architecture 10/10, combined 28/28 PASS여야 한다.
- `Assets/_Game/Map/Data/WorldGeneration/Authoring/**/*.csv`는 0개여야 한다.
- MAP01 이후 Result가 존재하거나 Task가 COMPLETE/CURRENT이면 안 된다.
- 아래 신규 target C#과 `.meta`가 이미 존재하면 안 된다.
- Runtime WorldGeneration의 기존 C#은 `WorldGenConstants.cs`와 네 coordinate type, 총 5개뿐이어야 한다.

위 조건이 다르면 기존 파일을 이동·복원·삭제·덮어쓰지 말고 `BLOCKED`다.

## WRITE ALLOWLIST

정확히 다음 Runtime C# 1개와 Unity가 생성하는 대응 `.meta`를 생성할 수 있다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldCoordinateUtility.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldCoordinateUtility.cs.meta
```

정확히 다음 EditMode test C# 1개와 Unity가 생성하는 대응 `.meta`를 생성할 수 있다.

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/WorldCoordinateUtilityTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/WorldCoordinateUtilityTests.cs.meta
```

추가 생성 허용:

```text
MapDesign/MCP/REPORTS/MAP00_07_IMPLEMENT_COORDINATE_CONVERSIONS_RESULT.md
```

TASK EXECUTION 중 `06_IMPLEMENTATION_STATUS.md`는 수정하지 않는다. 상태 변경은 Result PASS 이후 STATUS FINALIZE만 수행한다.

## Coordinate Spaces

모든 논리 좌표의 원점은 월드 왼쪽 아래다.

```text
WorldTileCoord:  X = 0..623, Y = 0..415
SectorCoord:     X = 0..12,  Y = 0..12
MicroChunkCoord: X = 0..3,   Y = 0..3    (sector 내부)
LocalTileCoord:  X = 0..11,  Y = 0..7    (microchunk 내부)
```

Runtime에서 위 숫자를 literal로 다시 쓰지 않는다. 모든 범위와 변환은 `WorldGenConstants`를 참조한다.

## Runtime Type Contract

파일:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldCoordinateUtility.cs
```

namespace와 type:

```csharp
namespace StarNight.Map.WorldGeneration.Domain
{
    public static class WorldCoordinateUtility
    {
    }
}
```

`WorldCoordinateUtility`는 다음 public method 14개만 제공한다.

### Bounds — 4 overloads

```csharp
public static bool IsValid(WorldTileCoord coordinate)
public static bool IsValid(SectorCoord coordinate)
public static bool IsValid(MicroChunkCoord coordinate)
public static bool IsValid(LocalTileCoord coordinate)
```

exact 범위식:

```text
WorldTile: 0 <= X < WorldWidthTiles, 0 <= Y < WorldHeightTiles
Sector: 0 <= X < SectorColumns, 0 <= Y < SectorRows
MicroChunk: 0 <= X < MicroChunkColumnsPerSector, 0 <= Y < MicroChunkRowsPerSector
LocalTile: 0 <= X < MicroChunkWidthTiles, 0 <= Y < MicroChunkHeightTiles
```

### TryCreate — 4 methods

```csharp
public static bool TryCreateWorldTile(int x, int y, out WorldTileCoord coordinate)
public static bool TryCreateSector(int x, int y, out SectorCoord coordinate)
public static bool TryCreateMicroChunk(int x, int y, out MicroChunkCoord coordinate)
public static bool TryCreateLocalTile(int x, int y, out LocalTileCoord coordinate)
```

- valid이면 해당 좌표를 생성하고 `true`를 반환한다.
- invalid이면 output을 `default`로 설정하고 `false`를 반환한다.
- clamp, wrap, absolute value, exception을 사용하지 않는다.

### Compose to world — 2 methods

```csharp
public static bool TryToWorld(
    SectorCoord sector,
    MicroChunkCoord microChunk,
    LocalTileCoord localTile,
    out WorldTileCoord worldTile)

public static WorldTileCoord ToWorld(
    SectorCoord sector,
    MicroChunkCoord microChunk,
    LocalTileCoord localTile)
```

exact 변환식:

```text
worldX = sector.X * SectorWidthTiles
       + microChunk.X * MicroChunkWidthTiles
       + localTile.X

worldY = sector.Y * SectorHeightTiles
       + microChunk.Y * MicroChunkHeightTiles
       + localTile.Y
```

`TryToWorld`:

- 세 입력이 모두 valid일 때만 계산한다.
- 성공 결과도 `IsValid(WorldTileCoord)`로 최종 확인한다.
- 실패하면 `worldTile = default`, `false`를 반환한다.

`ToWorld`:

- invalid `sector`는 `ArgumentOutOfRangeException(nameof(sector))`로 거부한다.
- invalid `microChunk`는 `ArgumentOutOfRangeException(nameof(microChunk))`로 거부한다.
- invalid `localTile`은 `ArgumentOutOfRangeException(nameof(localTile))`로 거부한다.
- valid이면 위 exact 식의 좌표를 반환한다.
- 어떤 경우에도 clamp하거나 가장 가까운 valid 좌표로 보정하지 않는다.

### Decompose from world — 4 methods

```csharp
public static bool TryFromWorld(
    WorldTileCoord worldTile,
    out SectorCoord sector,
    out MicroChunkCoord microChunk,
    out LocalTileCoord localTile)

public static SectorCoord ToSector(WorldTileCoord worldTile)
public static MicroChunkCoord ToMicroChunk(WorldTileCoord worldTile)
public static LocalTileCoord ToLocalTile(WorldTileCoord worldTile)
```

exact 변환식:

```text
sectorX = worldTile.X / SectorWidthTiles
sectorY = worldTile.Y / SectorHeightTiles

microChunkX = (worldTile.X % SectorWidthTiles) / MicroChunkWidthTiles
microChunkY = (worldTile.Y % SectorHeightTiles) / MicroChunkHeightTiles

localTileX = worldTile.X % MicroChunkWidthTiles
localTileY = worldTile.Y % MicroChunkHeightTiles
```

`TryFromWorld`:

- 시작 시 세 output을 모두 `default`로 설정한다.
- invalid `worldTile`이면 partial output 없이 `false`를 반환한다.
- valid이면 위 exact 식으로 세 좌표를 만들고 모두 valid인지 확인한 뒤 `true`를 반환한다.

세 direct projection method:

- invalid `worldTile`이면 각각 `ArgumentOutOfRangeException(nameof(worldTile))`를 던진다.
- valid이면 `TryFromWorld`와 동일한 식의 해당 좌표를 반환한다.
- 서로 다른 변환식을 중복 작성하지 않는다. private helper 또는 `TryFromWorld` 재사용으로 공식을 단일화한다.

## Runtime Implementation Rules

- `using System;`만 사용할 수 있다.
- `WorldCoordinateUtility`는 `public static class`이며 field, property, constructor, nested type을 만들지 않는다.
- public API는 명시된 14개 method뿐이다. private helper는 허용한다.
- 모든 dimension/grid 값은 `WorldGenConstants`만 참조한다.
- 좌표 타입 C# 4개 또는 `WorldGenConstants.cs`를 수정하지 않는다.
- integer division과 remainder는 valid non-negative world 좌표에만 적용한다.
- `UnityEngine`, `UnityEditor`, Vector, tuple, array, collection, LINQ, reflection, file I/O를 사용하지 않는다.
- extension method, implicit/explicit conversion operator, arithmetic operator를 추가하지 않는다.
- CSV의 visual row Y 반전, tile transform, direction, index, neighbor 계산을 구현하지 않는다.
- exception message 또는 pass ID 기록 시스템을 새로 만들지 않는다. 생성 pass의 문맥 보고는 후속 생성기 Task의 책임이다.
- 기존 Legacy Room/MacroChunk/Stage/P6/P11 타입을 참조하거나 재사용하지 않는다.

## EditMode Test Contract

파일:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/WorldCoordinateUtilityTests.cs
```

namespace와 fixture:

```csharp
namespace StarNight.Map.Tests.WorldGeneration.Domain
{
    public sealed class WorldCoordinateUtilityTests
    {
    }
}
```

NUnit `[Test]` method를 정확히 10개 만든다. parameterized test와 `[TestCase]`는 사용하지 않는다.

1. `WorldTileBoundsAndTryCreate_AreConsistent`
   - `(0,0)`, `(623,415)` valid
   - 음수 X/Y와 `(624,0)`, `(0,416)` invalid
   - invalid output은 `default`
2. `SectorBoundsAndTryCreate_AreConsistent`
   - `(0,0)`, `(12,12)` valid
   - 음수 X/Y와 `(13,0)`, `(0,13)` invalid/default
3. `MicroChunkBoundsAndTryCreate_AreConsistent`
   - `(0,0)`, `(3,3)` valid
   - 음수 X/Y와 `(4,0)`, `(0,4)` invalid/default
4. `LocalTileBoundsAndTryCreate_AreConsistent`
   - `(0,0)`, `(11,7)` valid
   - 음수 X/Y와 `(12,0)`, `(0,8)` invalid/default
5. `TryToWorld_CombinesCoordinateSpaces`
   - `(sector 1,1 / micro 1,1 / local 1,2)`가 world `(61,42)`
   - 모든 output이 valid
6. `TryToWorld_RejectsInvalidComponents`
   - sector, microChunk, localTile을 각각 하나씩 invalid로 전달
   - 세 경우 모두 `false`, world output `default`
7. `ToWorld_RejectsInvalidComponentsWithoutClamping`
   - invalid sector/microChunk/localTile 각각에 `ArgumentOutOfRangeException`
   - 각 `ParamName`이 `sector`, `microChunk`, `localTile`과 일치
8. `TryFromWorld_DecomposesCoordinateSpaces`
   - world `(61,42)`가 sector `(1,1)`, micro `(1,1)`, local `(1,2)`
   - 세 output이 모두 valid
9. `TryFromWorld_RejectsInvalidWorldWithoutPartialOutputs`
   - `(-1,0)`, `(624,0)`, `(0,-1)`, `(0,416)`을 확인
   - 모두 `false`, 세 output 모두 `default`
10. `DirectProjectionMethods_MatchDecompositionAndRejectInvalidWorld`
    - valid `(61,42)`에서 세 direct projection이 test 8 결과와 일치
    - invalid world에서 각 direct projection이 `ArgumentOutOfRangeException(nameof(worldTile))`

Test implementation rules:

- 기존 Unity Test Framework와 NUnit만 사용한다.
- test helper가 필요하면 test 파일 내부 private member로만 둔다.
- 테스트의 기대값 literal은 잠긴 좌표 사양과 수식 검증을 위해 허용한다.
- 전수 loop로 169×16 microchunk와 모든 모서리를 검사하지 않는다. 이는 MAP00_08의 범위다.
- production 파일을 생성·수정·삭제하지 않는다.

## Collision Handling

1. 신규 target C#이 이미 존재하면 본문을 읽어 병합하거나 덮어쓰지 않는다.
2. target `.meta`만 orphan 상태로 존재하면 GUID를 검사한 뒤 `BLOCKED`로 보고한다. 이번 create-only Task에서 재사용하지 않는다.
3. 승인 경로에 예상하지 않은 다른 C#이 있으면 삭제하지 않고 경로만 기록한 뒤 `BLOCKED`다.
4. 기존 사용자 변경을 되돌리거나 정리하지 않는다.
5. MAP00_05/06 파일은 존재와 계약만 검증하며 수정하지 않는다.

## DO NOT

- 기존 coordinate value type 또는 constants 수정 금지
- exhaustive `CoordinateConversionBoundaryTests` 선행 생성 금지
- 169×16 전수 loop test 선행 구현 금지
- debug view, EditorWindow, Gizmo, Scene GUI 생성 금지
- direction/Side/transform/index/neighbor/ID enum 생성 금지
- Runtime C#을 허용된 1개보다 더 만들거나 기존 Runtime을 수정 금지
- test C#을 허용된 1개보다 더 만들거나 기존 test를 수정 금지
- CSV, schema, loader, registry, ScriptableObject 생성·수정 금지
- asmdef/asmref 생성·수정 금지
- `Assets/_Legacy/**` 변경 금지
- 기존 Room/MacroChunk/Stage/P6/P11 타입 참조 금지
- Scene, Prefab, Tile, Tile Palette, Animator, Addressables 변경 금지
- `Packages/**`, `ProjectSettings/**` 변경 금지
- 새 package/dependency 설치 금지
- 기존 파일·폴더 삭제/이동/이름 변경 금지
- 관련 없는 포맷팅·warning 수정 금지
- Git commit/push/branch/reset/rebase/force 금지
- MAP00_08 또는 MAP01 선행 작업 금지

## Inputs

- `MASTER_IMPLEMENTATION_TASK_LIST.md`
- MAP00_06 PASS Result
- `WorldGenConstants`와 네 coordinate value type
- 기존 constant/value type/architecture tests
- 보존된 WorldGeneration 구조와 assembly 경계
- Unity Editor `6000.3.8f1`

## Outputs

- `WorldCoordinateUtility.cs`
- `WorldCoordinateUtility.cs.meta`
- `WorldCoordinateUtilityTests.cs`
- `WorldCoordinateUtilityTests.cs.meta`
- `REPORTS/MAP00_07_IMPLEMENT_COORDINATE_CONVERSIONS_RESULT.md`

## Implementation Steps

1. `MASTER_IMPLEMENTATION_TASK_LIST.md`와 `06_IMPLEMENTATION_STATUS.md`에서 이 TASK가 정확한 next/CURRENT인지 확인한다.
2. MAP00_06 Result가 `STATUS: PASS`, new 12/12, constant 6/6, architecture 10/10, combined 28/28, compile error 0인지 확인한다.
3. 작업 전 변경 파일 경로를 기록하고 기존 무관 변경은 수정·복구하지 않는다.
4. Preflight Preservation Check의 디렉터리, asmdef, MAP00_04~06 파일과 MAP01 미시작 상태를 확인한다.
5. 신규 target C# 2개와 `.meta` 2개가 모두 absent인지 확인한다.
6. `WorldCoordinateUtility.cs`를 정확한 Runtime Type Contract로 생성한다.
7. `WorldCoordinateUtilityTests.cs`를 정확히 10개 test case로 생성한다.
8. Unity Asset Refresh와 compilation이 완료될 때까지 기다린다.
9. 신규 `.cs.meta` 2개의 GUID 형식과 project-wide uniqueness를 검사한다.
10. 신규 `WorldCoordinateUtilityTests` fixture를 실행해 actual cases 10개가 모두 PASS인지 확인한다.
11. MAP00_06 `CoordinateValueTypeTests` fixture를 실행해 actual cases 12개가 모두 PASS인지 확인한다.
12. MAP00_05 `WorldGenConstantsTests` fixture를 실행해 actual cases 6개가 모두 PASS인지 확인한다.
13. MAP00_04 architecture fixture 3개를 실행해 actual cases 10개가 모두 PASS인지 확인한다.
14. 신규 10 + value type 12 + constant 6 + architecture 10, 총 actual cases 38개를 단일 targeted run으로 재검증한다.
15. Runtime source가 승인 namespace, exact public API, constants-only dimension dependency이고 clamp/Unity/Legacy dependency가 없는지 확인한다.
16. 작업 후 Asset 변경이 허용된 C# 2개와 `.meta` 2개뿐인지 확인한다.
17. Result 문서를 작성한다.
18. 모든 DONE CONDITIONS가 PASS인 경우에만 Result에 `STATUS: PASS`를 기록한다.

## Tests

### T1 — Compile

```text
Compile Errors = 0
Relevant New Warnings = 0
```

### T2 — New Coordinate Utility Fixture

```text
Fixture: StarNight.Map.Tests.WorldGeneration.Domain.WorldCoordinateUtilityTests
Actual cases: 10
Passed: 10
Failed: 0
Skipped: 0
```

### T3 — Existing Coordinate Value Type Regression

```text
Fixture: StarNight.Map.Tests.WorldGeneration.Domain.CoordinateValueTypeTests
Actual cases: 12
Passed: 12
Failed: 0
Skipped: 0
```

### T4 — Existing Constant Contract Regression

```text
Fixture: StarNight.Map.Tests.WorldGeneration.Domain.WorldGenConstantsTests
Actual cases: 6
Passed: 6
Failed: 0
Skipped: 0
```

### T5 — Existing Architecture Regression

```text
WorldGenerationModuleStructureTests = PASS
WorldGenerationRuntimeBoundaryTests = PASS
WorldGenerationEditorBoundaryTests = PASS
Actual cases: 10
Passed: 10
Failed: 0
Skipped: 0
```

### T6 — Combined Targeted EditMode Result

```text
Actual cases: 38
Passed: 38
Failed: 0
Skipped: 0
```

### T7 — Asset Meta Validation

- 신규 `.cs.meta` 2개 존재
- GUID 형식 유효
- 신규 GUID끼리 중복 0
- 프로젝트 전체 GUID와 중복 0

### T8 — Change Scope

이번 TASK의 Asset 변경은 신규 C# 2개와 `.meta` 2개뿐이다.

기존 무관 변경은 Result에 별도로 기록하며 수정하지 않는다. C#, CSV, asmdef, Scene, Prefab, Package, ProjectSettings의 다른 변경은 0개여야 한다.

## Unity Verification

필수:

```text
Unity Version: 6000.3.8f1
Asset Refresh: PASS
Compile Errors: 0
Relevant New Warnings: 0
New Coordinate Utility Tests: PASS (10/10)
Existing Coordinate Value Type Tests: PASS (12/12)
Existing Constant Tests: PASS (6/6)
Existing Architecture Tests: PASS (10/10)
Combined Targeted EditMode Tests: PASS (38/38)
PlayMode Tests: NOT RUN
Scene/Prefab Changes: NONE
```

Unity Editor 또는 Unity MCP에 접근할 수 없어 Asset Refresh, compilation, 대상 EditMode 결과를 확인할 수 없으면 PASS로 종료하지 말고 `BLOCKED`로 기록한다.

## Result File

```text
REPORTS/MAP00_07_IMPLEMENT_COORDINATE_CONVERSIONS_RESULT.md
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
COORDINATE CONTRACT
CHANGED
TEST
UNITY
ASSET META VALIDATION
OUT_OF_SCOPE_FINDINGS
DONE CONDITIONS
NEXT
Recommended Commit
```

## DONE CONDITIONS

- [ ] Current Task가 MAP00_07이고 master backlog의 정확한 next임을 확인했다.
- [ ] 전체 master backlog 205개와 MAP01 HOLD 상태를 확인했다.
- [ ] MAP00_06 Result의 PASS, new 12/12, constant 6/6, architecture 10/10, combined 28/28, compile error 0을 확인했다.
- [ ] 보존 대상 디렉터리 4개, asmdef 5개, MAP00_04~06 필수 파일과 `.meta`가 존재한다.
- [ ] `WorldGenConstants` 15개 const와 네 coordinate value type 계약을 보존했다.
- [ ] Authoring CSV 0개와 MAP01 이후 미시작 상태를 확인했다.
- [ ] target C#과 `.meta`가 작업 전에 absent였다.
- [ ] 정확한 Runtime utility C# 1개와 test C# 1개만 생성했다.
- [ ] 네 좌표 공간의 `IsValid`가 constants 기반 exact half-open 범위를 사용한다.
- [ ] 네 `TryCreate`가 invalid output을 default로 두고 false를 반환한다.
- [ ] `TryToWorld`와 `ToWorld`가 exact 합성 공식을 사용한다.
- [ ] `TryFromWorld`와 direct projection이 exact 분해 공식을 공유한다.
- [ ] invalid 입력을 clamp/wrap하지 않고 false 또는 `ArgumentOutOfRangeException`으로 거부한다.
- [ ] public API가 명시된 method 14개뿐이다.
- [ ] exhaustive MAP00_08 test, debug view, CSV, 생성 로직을 선행 구현하지 않았다.
- [ ] Unity/Legacy Room/MacroChunk/Stage/P6/P11 dependency가 없다.
- [ ] 신규 `.cs.meta` 2개가 존재하며 GUID가 유효하고 project-unique하다.
- [ ] Unity Asset Refresh가 PASS다.
- [ ] Unity Compile Error가 0개다.
- [ ] 관련 신규 Warning이 0개다.
- [ ] 신규 coordinate utility test actual cases 10개가 모두 PASS다.
- [ ] 기존 value type test actual cases 12개가 모두 PASS다.
- [ ] 기존 constant test actual cases 6개가 모두 PASS다.
- [ ] 기존 architecture test actual cases 10개가 모두 PASS다.
- [ ] combined targeted EditMode actual cases 38개가 모두 PASS다.
- [ ] PlayMode 테스트를 실행하지 않았다.
- [ ] Scene/Prefab/Package/ProjectSettings 변경이 0개다.
- [ ] Result 문서가 요구 형식을 충족한다.
- [ ] MAP00_08 또는 MAP01을 시작하지 않았다.

## Completion Rule

TASK EXECUTION은 Result에 `STATUS: PASS / FAIL / BLOCKED`만 기록한다.

Result가 정확히 `STATUS: PASS`이고 모든 DONE CONDITIONS가 완료된 경우에만 STATUS FINALIZE Phase가:

```text
MAP00_07_IMPLEMENT_COORDINATE_CONVERSIONS: CURRENT -> COMPLETE
Current Task: TASKS/MAP00_07_IMPLEMENT_COORDINATE_CONVERSIONS.md -> NONE
```

을 수행한다.

STATUS FINALIZE는 MAP00_08을 CURRENT로 바꾸지 않는다. 다음 TASK는 새 패치를 기다린다.

## Expected Next Task

```text
MAP00_08_CREATE_COORDINATE_TESTS
```

다음 TASK는 별도 패치로만 연다.
