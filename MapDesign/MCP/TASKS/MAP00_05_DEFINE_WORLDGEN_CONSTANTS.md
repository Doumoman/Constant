# MAP00_05 — Define WorldGen Constants

```yaml
status_control:
  task_key: MAP00_05_DEFINE_WORLDGEN_CONSTANTS
  result_file: REPORTS/MAP00_05_DEFINE_WORLDGEN_CONSTANTS_RESULT.md
```

## TASK TYPE

```text
RUNTIME CONSTANT CONTRACT + EDITMODE TEST
```

## Objective

광역 WorldGeneration의 월드·섹터·마이크로청크 크기와 파생 개수를 `WorldGenConstants` 단일 타입에 compile-time 상수로 고정한다.

이 TASK는 상수 계약만 만든다. 좌표 값 타입, 좌표 변환 함수, ID 타입, CSV loader, registry, 생성 pass 또는 debug view는 구현하지 않는다.

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
12. `REPORTS/MAP00_04_CREATE_TEST_STRUCTURE_RESULT.md`

## READ ALLOWLIST

본문 읽기 허용:

- Mandatory Read Order의 파일
- `Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef`
- `Assets/_Game/Editor/MapAuthoring/MapAuthoring.Editor.asmdef`
- `Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef`
- `Assets/_Game/Tests/PlayMode/Map/Game.Map.Tests.PlayMode.asmdef`
- `Assets/_Game/Editor/MapAuthoring/Tests/EditMode/MapAuthoring.Tests.EditMode.asmdef`
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

기존 MAP00_04 architecture test 실행 중 테스트 코드가 승인 범위를 검사하는 것은 허용한다.

금지:

- 승인되지 않은 프로젝트 C# 본문 스캔
- `Assets/_Legacy/**` 본문 열람 또는 수정
- Scene/Prefab YAML 열람
- CSV/GDD/과거 하네스 본문 열람
- 테스트 통과를 위해 기존 코드를 수정하는 행위

## Master Backlog Check

`MASTER_IMPLEMENTATION_TASK_LIST.md`에서 다음을 확인한다.

```text
MAP00_01~04 = COMPLETE
MAP00_05_DEFINE_WORLDGEN_CONSTANTS = next
MAP00_06~10 = LOCKED
MAP01_01 premade patch = HOLD / DO NOT RUN
전체 Task = 205
```

하나라도 다르면 임의 보정하지 말고 `BLOCKED` Result를 작성한다.

## Preflight Preservation Check

MAP00_04 Result에 기록된 별도 Legacy 이동을 되돌리거나 확장 조사하지 않는다. 다음 보존 계약만 확인한다.

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

필수 MAP00_04 테스트 파일:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/WorldGenerationModuleStructureTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/WorldGenerationRuntimeBoundaryTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/WorldGenerationEditorBoundaryTests.cs
```

추가 순서 검증:

- `Assets/_Game/Map/Data/WorldGeneration/Authoring/**/*.csv`는 0개여야 한다.
- MAP01 이후 Result가 존재하거나 Task가 COMPLETE/CURRENT이면 안 된다.
- 아래 신규 target `.cs`가 이미 존재하면 안 된다.

위 조건이 다르면 기존 파일을 이동·복원·삭제·덮어쓰지 말고 `BLOCKED`다.

## WRITE ALLOWLIST

정확히 다음 Runtime C# 1개와 Unity가 생성하는 대응 `.meta`를 생성할 수 있다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs.meta
```

정확히 다음 EditMode test C# 1개와 Unity가 생성하는 대응 `.meta`를 생성할 수 있다.

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/WorldGenConstantsTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/WorldGenConstantsTests.cs.meta
```

추가 생성 허용:

```text
MapDesign/MCP/REPORTS/MAP00_05_DEFINE_WORLDGEN_CONSTANTS_RESULT.md
```

TASK EXECUTION 중 `06_IMPLEMENTATION_STATUS.md`는 수정하지 않는다. 상태 변경은 Result PASS 이후 STATUS FINALIZE만 수행한다.

## Runtime Type Contract

파일:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
```

namespace와 type:

```csharp
namespace StarNight.Map.WorldGeneration.Domain
{
    public static class WorldGenConstants
    {
    }
}
```

`WorldGenConstants`는 다음 public compile-time `int const`만 구현한다.

### Base dimensions

```csharp
public const int WorldWidthTiles = 624;
public const int WorldHeightTiles = 416;
public const int SectorWidthTiles = 48;
public const int SectorHeightTiles = 32;
public const int MicroChunkWidthTiles = 12;
public const int MicroChunkHeightTiles = 8;
```

### Derived sector grid

다음 값은 숫자 13/169를 다시 쓰지 않고 위 base constant의 compile-time 식으로 정의한다.

```csharp
public const int SectorColumns = WorldWidthTiles / SectorWidthTiles; // 13
public const int SectorRows = WorldHeightTiles / SectorHeightTiles;  // 13
public const int SectorCount = SectorColumns * SectorRows;           // 169
```

### Derived microchunk grid

다음 값은 숫자 4/16/96을 다시 쓰지 않고 위 constant의 compile-time 식으로 정의한다.

```csharp
public const int MicroChunkColumnsPerSector = SectorWidthTiles / MicroChunkWidthTiles; // 4
public const int MicroChunkRowsPerSector = SectorHeightTiles / MicroChunkHeightTiles;  // 4
public const int MicroChunksPerSector = MicroChunkColumnsPerSector * MicroChunkRowsPerSector; // 16
public const int TilesPerMicroChunk = MicroChunkWidthTiles * MicroChunkHeightTiles; // 96
public const int TilesPerSector = SectorWidthTiles * SectorHeightTiles;             // 1536
public const int WorldTileCount = WorldWidthTiles * WorldHeightTiles;                // 259584
```

## Runtime Implementation Rules

- base dimension literal은 `624`, `416`, `48`, `32`, `12`, `8`만 사용한다.
- `13`, `169`, `4`, `16`, `96`, `1536`, `259584`는 derived constant initializer에 literal로 다시 쓰지 않는다.
- `public static class`와 `public const int`를 사용한다.
- method, property, constructor, collection, mutable static field, static readonly field를 만들지 않는다.
- `UnityEngine`, `UnityEditor`, LINQ, reflection, file I/O를 사용하지 않는다.
- 주석은 단위와 파생 관계만 설명하며 Legacy/Room/MacroChunk 개념을 가져오지 않는다.
- 최신 명칭은 `MicroChunk`다. 같은 12×8 크기의 기존 Legacy Room/MacroChunk 타입을 참조하거나 재사용하지 않는다.

## EditMode Test Contract

파일:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/WorldGenConstantsTests.cs
```

namespace와 fixture:

```csharp
namespace StarNight.Map.Tests.WorldGeneration.Domain
{
    public sealed class WorldGenConstantsTests
    {
    }
}
```

NUnit `[Test]` method를 정확히 6개 만든다.

1. `LockedWorldDimensions_AreExact`
   - `WorldWidthTiles == 624`
   - `WorldHeightTiles == 416`
2. `LockedSectorDimensions_AreExact`
   - `SectorWidthTiles == 48`
   - `SectorHeightTiles == 32`
3. `LockedMicroChunkDimensions_AreExact`
   - `MicroChunkWidthTiles == 12`
   - `MicroChunkHeightTiles == 8`
4. `DerivedSectorGrid_IsExact`
   - `SectorColumns == 13`
   - `SectorRows == 13`
   - `SectorCount == 169`
5. `DerivedMicroChunkGridAndTileCounts_AreExact`
   - columns/rows per sector `4/4`
   - microchunks per sector `16`
   - tiles per microchunk `96`
   - tiles per sector `1536`
   - world tile count `259584`
6. `Dimensions_ReconstructParentSpacesExactly`
   - sector width×columns = world width
   - sector height×rows = world height
   - microchunk width×columns per sector = sector width
   - microchunk height×rows per sector = sector height
   - `WorldGenConstants`의 모든 public static field가 `int`, `IsLiteral=true`, writable state 없음

Test implementation rules:

- 기존 Unity Test Framework와 NUnit만 사용한다.
- reflection은 여섯 번째 test 안에서 public field의 compile-time const 계약을 검사하는 용도로만 허용한다.
- parameterized test를 사용하지 않는다. 실제 test case 수는 정확히 6개여야 한다.
- 테스트 안의 기대값 literal은 잠긴 사양 검증을 위해 허용한다.
- test helper가 필요하면 test 파일 내부 private member로만 둔다.
- production 파일을 생성·수정·삭제하지 않는다.

## Collision Handling

1. 신규 target C#이 이미 존재하면 본문을 읽어 병합하거나 덮어쓰지 않는다.
2. target `.meta`만 orphan 상태로 존재하면 GUID를 검사한 뒤 `BLOCKED`로 보고한다. 이번 create-only Task에서 재사용하지 않는다.
3. 승인 경로에 예상하지 않은 다른 C#이 있으면 삭제하지 않고 `OUT_OF_SCOPE_FINDING`에 경로만 기록한다.
4. 예상하지 않은 파일 때문에 constant-only 완료를 보장할 수 없으면 `BLOCKED`다.
5. 기존 사용자 변경을 되돌리거나 정리하지 않는다.

## DO NOT

- `WorldTileCoord`, `SectorCoord`, `MicroChunkCoord`, `LocalTileCoord` 생성 금지
- 좌표 변환·bounds·`TryCreate` method 생성 금지
- direction/Side/transform/ID enum 생성 금지
- Runtime C#을 허용된 1개보다 더 만들거나 수정 금지
- 기존 또는 추가 test C#을 허용된 1개보다 더 만들거나 수정 금지
- Editor C# 또는 debug view 생성 금지
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
- MAP00_06 또는 MAP01 선행 작업 금지

## Inputs

- `MASTER_IMPLEMENTATION_TASK_LIST.md`
- MAP00_04 PASS Result
- 보존된 WorldGeneration 구조와 assembly/test 경계
- Unity Editor `6000.3.8f1`

## Outputs

- `WorldGenConstants.cs`
- `WorldGenConstants.cs.meta`
- `WorldGenConstantsTests.cs`
- `WorldGenConstantsTests.cs.meta`
- `REPORTS/MAP00_05_DEFINE_WORLDGEN_CONSTANTS_RESULT.md`

## Implementation Steps

1. `MASTER_IMPLEMENTATION_TASK_LIST.md`와 `06_IMPLEMENTATION_STATUS.md`에서 이 TASK가 정확한 next/CURRENT인지 확인한다.
2. MAP00_04 Result가 `STATUS: PASS`, actual cases 10/10, compile error 0인지 확인한다.
3. 작업 전 변경 파일 경로를 기록하고 기존 무관 변경은 수정·복구하지 않는다.
4. Preflight Preservation Check의 디렉터리, asmdef, architecture test와 MAP01 미시작 상태를 확인한다.
5. 두 target C#과 `.meta`가 모두 absent인지 확인한다.
6. 허용 경로에 `WorldGenConstants.cs`를 정확한 Runtime Type Contract로 생성한다.
7. 허용 경로에 `WorldGenConstantsTests.cs`를 정확히 6개 test case로 생성한다.
8. Unity Asset Refresh와 compilation이 완료될 때까지 기다린다.
9. 신규 `.cs.meta` 2개의 GUID 형식과 project-wide uniqueness를 검사한다.
10. 신규 `WorldGenConstantsTests` fixture를 실행해 actual cases 6개가 모두 PASS인지 확인한다.
11. MAP00_04 architecture fixture 3개를 실행해 기존 actual cases 10개가 모두 PASS인지 확인한다.
12. 신규 6 + 기존 10, 총 actual cases 16개가 PASS인지 기록한다.
13. Runtime source가 승인 namespace이고 UnityEditor/Legacy/Stage/P6/P11 dependency가 없는지 확인한다.
14. 작업 후 변경 파일 경로가 허용된 C# 2개, `.meta` 2개, Result뿐인지 확인한다.
15. Result 문서를 작성한다.
16. 모든 DONE CONDITIONS가 PASS인 경우에만 Result에 `STATUS: PASS`를 기록한다.

## Tests

### T1 — Compile

```text
Compile Errors = 0
Relevant New Warnings = 0
```

### T2 — New Constant Contract Fixture

```text
Fixture: StarNight.Map.Tests.WorldGeneration.Domain.WorldGenConstantsTests
Actual cases: 6
Passed: 6
Failed: 0
Skipped: 0
```

### T3 — Existing Architecture Regression

```text
WorldGenerationModuleStructureTests = PASS
WorldGenerationRuntimeBoundaryTests = PASS
WorldGenerationEditorBoundaryTests = PASS
Actual cases: 10
Passed: 10
Failed: 0
Skipped: 0
```

### T4 — Combined Targeted EditMode Result

```text
Actual cases: 16
Passed: 16
Failed: 0
Skipped: 0
```

### T5 — Asset Meta Validation

- 신규 `.cs.meta` 2개 존재
- GUID 형식 유효
- 신규 GUID끼리 중복 0
- 프로젝트 전체 GUID와 중복 0

### T6 — Change Scope

이번 TASK의 Asset 변경은 신규 C# 2개와 `.meta` 2개뿐이다.

기존 무관 변경은 Result에 별도로 기록하며 수정하지 않는다. C#, CSV, asmdef, Scene, Prefab, Package, ProjectSettings의 다른 변경은 0개여야 한다.

## Unity Verification

필수:

```text
Unity Version: 6000.3.8f1
Asset Refresh: PASS
Compile Errors: 0
Relevant New Warnings: 0
New Constant Tests: PASS (6/6)
Existing Architecture Tests: PASS (10/10)
Combined Targeted EditMode Tests: PASS (16/16)
PlayMode Tests: NOT RUN
Scene/Prefab Changes: NONE
```

Unity Editor 또는 Unity MCP에 접근할 수 없어 Asset Refresh, compilation, 대상 EditMode 결과를 확인할 수 없으면 PASS로 종료하지 말고 `BLOCKED`로 기록한다.

## Result File

```text
REPORTS/MAP00_05_DEFINE_WORLDGEN_CONSTANTS_RESULT.md
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
CONSTANT CONTRACT
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

- [ ] Current Task가 MAP00_05이고 master backlog의 정확한 next임을 확인했다.
- [ ] 전체 master backlog 205개와 MAP01 HOLD 상태를 확인했다.
- [ ] MAP00_04 Result의 PASS, 10/10 test, compile error 0을 확인했다.
- [ ] 보존 대상 디렉터리 4개, asmdef 5개, architecture test C# 3개가 존재한다.
- [ ] Authoring CSV 0개와 MAP01 이후 미시작 상태를 확인했다.
- [ ] target C#과 `.meta`가 작업 전에 absent였다.
- [ ] 정확한 Runtime C# 1개와 test C# 1개만 생성했다.
- [ ] base dimension 624/416/48/32/12/8을 정확히 정의했다.
- [ ] 13/169/4/16/96/1536/259584 파생값을 base constant 식으로 정의했다.
- [ ] mutable static state, method, property, collection을 추가하지 않았다.
- [ ] Legacy Room/MacroChunk/Stage/P6/P11 dependency가 없다.
- [ ] 좌표 값 타입·좌표 변환·ID·CSV를 선행 구현하지 않았다.
- [ ] 신규 `.cs.meta` 2개가 존재하며 GUID가 유효하고 project-unique하다.
- [ ] Unity Asset Refresh가 PASS다.
- [ ] Unity Compile Error가 0개다.
- [ ] 관련 신규 Warning이 0개다.
- [ ] 신규 constant test actual cases 6개가 모두 PASS다.
- [ ] 기존 architecture test actual cases 10개가 모두 PASS다.
- [ ] combined targeted EditMode actual cases 16개가 모두 PASS다.
- [ ] PlayMode 테스트를 실행하지 않았다.
- [ ] Scene/Prefab/Package/ProjectSettings 변경이 0개다.
- [ ] Result 문서가 요구 형식을 충족한다.
- [ ] MAP00_06 또는 MAP01을 시작하지 않았다.

## Completion Rule

TASK EXECUTION은 Result에 `STATUS: PASS / FAIL / BLOCKED`만 기록한다.

Result가 정확히 `STATUS: PASS`이고 모든 DONE CONDITIONS가 완료된 경우에만 STATUS FINALIZE Phase가:

```text
MAP00_05_DEFINE_WORLDGEN_CONSTANTS: CURRENT -> COMPLETE
Current Task: TASKS/MAP00_05_DEFINE_WORLDGEN_CONSTANTS.md -> NONE
```

을 수행한다.

STATUS FINALIZE는 MAP00_06을 CURRENT로 바꾸지 않는다. 다음 TASK는 새 패치를 기다린다.

## Expected Next Task

```text
MAP00_06_IMPLEMENT_COORDINATE_VALUE_TYPES
```

다음 TASK는 별도 패치로만 연다.
