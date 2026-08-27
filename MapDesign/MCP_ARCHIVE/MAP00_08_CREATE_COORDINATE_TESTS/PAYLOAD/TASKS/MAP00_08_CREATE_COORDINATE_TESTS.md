# MAP00_08 — Create Coordinate Tests

```yaml
status_control:
  task_key: MAP00_08_CREATE_COORDINATE_TESTS
  result_file: REPORTS/MAP00_08_CREATE_COORDINATE_TESTS_RESULT.md
```

## TASK TYPE

```text
EXHAUSTIVE EDITMODE COORDINATE VALIDATION
```

## Objective

MAP00_05~07에서 구현한 상수, 좌표 값 타입, `WorldCoordinateUtility`를 변경하지 않고 광역 WorldGeneration 좌표 계약을 exhaustive EditMode fixture로 검증한다.

이 TASK는 월드 네 모서리, 모든 169 sector × 16 microchunk × 네 local corner, 전체 624×416 world tile, invalid 좌표와 throwing API를 검증한다. Runtime 구현, 좌표 debug view, CSV loader 또는 생성 pass는 만들지 않는다.

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
12. `REPORTS/MAP00_07_IMPLEMENT_COORDINATE_CONVERSIONS_RESULT.md`

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

기존 utility/value type/constant/architecture test 실행 중 테스트 코드가 승인 범위를 검사하는 것은 허용한다.

금지:

- 승인되지 않은 프로젝트 C# 본문 스캔
- `Assets/_Legacy/**` 본문 열람 또는 수정
- Scene/Prefab YAML 열람
- CSV/GDD/과거 하네스 본문 열람
- 테스트 통과를 위해 production 또는 기존 test 코드를 수정하는 행위

## Master Backlog Check

`MASTER_IMPLEMENTATION_TASK_LIST.md`에서 다음을 확인한다.

```text
MAP00_01~07 = COMPLETE
MAP00_08_CREATE_COORDINATE_TESTS = next/current
MAP00_09~10 = LOCKED
MAP01_01 premade patch = HOLD / DO NOT RUN
전체 Task = 205
```

하나라도 다르면 임의 보정하지 말고 `BLOCKED` Result를 작성한다.

## Preflight Preservation Check

MAP00_07 Result의 PASS와 아래 항목을 확인한다.

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

필수 Runtime C# 6개:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldTileCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/MicroChunkCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/LocalTileCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldCoordinateUtility.cs
```

필수 기존 test C# 6개:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/WorldGenConstantsTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/CoordinateValueTypeTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/WorldCoordinateUtilityTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/WorldGenerationModuleStructureTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/WorldGenerationRuntimeBoundaryTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/WorldGenerationEditorBoundaryTests.cs
```

위 Runtime/test C# 각각의 대응 `.meta`도 존재해야 한다.

추가 순서 검증:

- `WorldGenConstants`의 `public const int` 15개 계약을 유지해야 한다.
- 네 좌표 타입은 MAP00_06의 readonly/raw X/Y/equality/hash/string 계약을 유지해야 한다.
- `WorldCoordinateUtility`는 MAP00_07의 public method 14개와 exact 변환 계약을 유지해야 한다.
- MAP00_07 Result는 utility 10/10, value type 12/12, constant 6/6, architecture 10/10, combined 38/38 PASS여야 한다.
- `Assets/_Game/Map/Data/WorldGeneration/Authoring/**/*.csv`는 0개여야 한다.
- MAP01 이후 Result가 존재하거나 Task가 COMPLETE/CURRENT이면 안 된다.
- 아래 신규 target C#과 `.meta`가 이미 존재하면 안 된다.
- Runtime WorldGeneration C#은 위 6개뿐이어야 한다.
- Domain EditMode test C#은 기존 3개뿐이어야 한다.

위 조건이 다르면 기존 파일을 이동·복원·삭제·덮어쓰지 말고 `BLOCKED`다.

## WRITE ALLOWLIST

정확히 다음 EditMode test C# 1개와 Unity가 생성하는 대응 `.meta`를 생성할 수 있다.

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/CoordinateConversionBoundaryTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/CoordinateConversionBoundaryTests.cs.meta
```

추가 생성 허용:

```text
MapDesign/MCP/REPORTS/MAP00_08_CREATE_COORDINATE_TESTS_RESULT.md
```

Runtime, Editor, 기존 test C#은 WRITE ALLOWLIST가 아니다. TASK EXECUTION 중 `06_IMPLEMENTATION_STATUS.md`는 수정하지 않는다. 상태 변경은 Result PASS 이후 STATUS FINALIZE만 수행한다.

## Locked Coverage Counts

```text
World corners                         = 4
Sector cells                          = 13 × 13 = 169
MicroChunks per sector                = 4 × 4 = 16
Local corners per MicroChunk          = 4
Exhaustive MicroChunk corner samples  = 169 × 16 × 4 = 10,816
All world tile samples                = 624 × 416 = 259,584
```

반복문의 한계값과 계산에는 `WorldGenConstants`를 사용한다. 테스트 마지막에는 실제 방문 count가 `10,816`, `259,584`와 각각 일치하는지 확인한다. 기대값 literal은 잠긴 사양 검증을 위해 test에서만 허용한다.

## EditMode Test Contract

파일:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/CoordinateConversionBoundaryTests.cs
```

namespace와 fixture:

```csharp
namespace StarNight.Map.Tests.WorldGeneration.Domain
{
    public sealed class CoordinateConversionBoundaryTests
    {
    }
}
```

NUnit `[Test]` method를 정확히 8개 만든다. parameterized test, `[TestCase]`, `[TestCaseSource]`를 사용하지 않는다.

### 1. World corners

```text
WorldCorners_RoundTripExactly
```

다음 네 좌표를 각각 `TryFromWorld`로 분해하고 `TryToWorld`와 `ToWorld`로 재합성한다.

```text
(0, 0)     -> Sector(0,0),   MicroChunk(0,0), LocalTile(0,0)
(623, 0)   -> Sector(12,0),  MicroChunk(3,0), LocalTile(11,0)
(0, 415)   -> Sector(0,12),  MicroChunk(0,3), LocalTile(0,7)
(623, 415) -> Sector(12,12), MicroChunk(3,3), LocalTile(11,7)
```

- `IsValid` 네 종류가 모두 true인지 확인한다.
- direct `ToSector`, `ToMicroChunk`, `ToLocalTile`도 같은 결과인지 확인한다.
- 네 sample이 원본 world 좌표로 정확히 왕복하는지 확인한다.

### 2. Every sector and microchunk corner

```text
EverySectorAndMicroChunkCorner_RoundTripsExactly
```

다음 모든 조합을 nested loop로 방문한다.

```text
sector.X     = 0 .. SectorColumns - 1
sector.Y     = 0 .. SectorRows - 1
microChunk.X = 0 .. MicroChunkColumnsPerSector - 1
microChunk.Y = 0 .. MicroChunkRowsPerSector - 1
local corner = (0,0), (width-1,0), (0,height-1), (width-1,height-1)
```

각 sample에서:

- independent expected world 좌표를 `WorldGenConstants` 기반 잠긴 식으로 계산한다.
- `TryToWorld`와 `ToWorld`가 expected world와 같은지 확인한다.
- `TryFromWorld`와 세 direct projection이 원본 sector/microChunk/localTile과 같은지 확인한다.
- 네 좌표가 모두 valid인지 확인한다.
- 방문 count가 정확히 `10,816`인지 확인한다.

### 3. Every world tile

```text
EveryWorldTile_RoundTripsExactly
```

`Y = 0..WorldHeightTiles-1`, `X = 0..WorldWidthTiles-1`의 모든 world tile을 방문한다.

각 sample에서:

- world가 valid인지 확인한다.
- `TryFromWorld`가 성공하고 세 component가 valid인지 확인한다.
- `TryToWorld`와 `ToWorld`가 원본 world와 같은지 확인한다.
- 세 direct projection이 분해 결과와 같은지 확인한다.
- 방문 count가 `WorldTileCount == 259,584`와 일치하는지 확인한다.

259,584회 loop 내부에서 성공할 때마다 동적 문자열을 만들거나 `TestCase`를 생성하지 않는다. 불일치 시에만 좌표와 component를 포함한 `Assert.Fail` 메시지를 만들 수 있다.

### 4. Invalid TryCreate boundaries

```text
TryCreate_RejectsImmediateAndIntegerExtremeOutOfRangeAxes
```

네 좌표 타입 각각에 대해 X축과 Y축을 독립적으로 확인한다.

- lower invalid: `-1`, `int.MinValue`
- upper invalid: 해당 half-open max, `int.MaxValue`
- 반대 축은 `0`
- `IsValid == false`
- 해당 `TryCreate == false`
- output은 해당 좌표 타입의 `default`

valid max-1이 각 타입에서 성공하는 것도 같은 test 안에서 확인한다.

### 5. Invalid compose boundaries

```text
TryToWorld_RejectsEveryInvalidComponentEdge
```

sector, microChunk, localTile 각각의 X/Y에 대해 다음 invalid를 독립적으로 전달한다.

- `-1`
- 해당 half-open max
- `int.MinValue`
- `int.MaxValue`

나머지 두 component는 `(0,0)` valid 값을 사용한다. 모든 경우 `TryToWorld == false`, `worldTile == default`여야 하며 clamp/wrap 결과가 나오면 실패다.

### 6. Invalid decompose boundaries

```text
TryFromWorld_RejectsEveryOutsideWorldEdgeWithoutPartialOutputs
```

world X/Y 각 축에 대해 `-1`, 해당 half-open max, `int.MinValue`, `int.MaxValue`를 독립적으로 전달한다.

- `TryFromWorld == false`
- sector, microChunk, localTile output이 모두 `default`
- partial output 또는 clamp 결과가 있으면 실패

### 7. Throwing compose API

```text
ToWorld_RejectsEachInvalidComponentWithExactParamName
```

- invalid sector X/Y 각각은 `ArgumentOutOfRangeException`과 `ParamName == "sector"`
- invalid microChunk X/Y 각각은 `ArgumentOutOfRangeException`과 `ParamName == "microChunk"`
- invalid localTile X/Y 각각은 `ArgumentOutOfRangeException`과 `ParamName == "localTile"`
- 각 공간에서 lower `-1`과 upper half-open max를 모두 확인

### 8. Throwing projection API

```text
DirectProjections_RejectEveryOutsideWorldEdgeWithExactParamName
```

`ToSector`, `ToMicroChunk`, `ToLocalTile` 각각에 world X/Y의 lower `-1`과 upper half-open max를 전달한다.

- 모두 `ArgumentOutOfRangeException`
- 모두 `ParamName == "worldTile"`
- clamp/wrap 또는 다른 exception type이면 실패

## Test Implementation Rules

- 기존 Unity Test Framework와 NUnit만 사용한다.
- fixture의 실제 NUnit test case 수는 정확히 8개다.
- exhaustive loop는 `WorldGenConstants`와 기존 public coordinate API만 소비한다.
- 검증 공식을 production utility에서 복사한 private 변환 함수로 만들지 않는다. expected compose 값은 test 2 내부에서 constants 기반 식으로만 계산한다.
- 반복되는 assertion helper는 test 파일 내부 private static method로만 둘 수 있다.
- 전체 world loop에서 per-sample allocation과 성공 메시지 생성을 피한다.
- test 실행은 production file, CSV, Scene, Prefab을 생성·수정·삭제하지 않는다.
- 결과는 실행 순서, culture, wall clock, random seed에 의존하지 않는다.

## Collision Handling

1. 신규 target C#이 이미 존재하면 본문을 읽어 병합하거나 덮어쓰지 않는다.
2. target `.meta`만 orphan 상태로 존재하면 GUID를 검사한 뒤 `BLOCKED`로 보고한다. 이번 create-only Task에서 재사용하지 않는다.
3. 승인 경로에 예상하지 않은 다른 C#이 있으면 삭제하지 않고 경로만 기록한 뒤 `BLOCKED`다.
4. 기존 사용자 변경을 되돌리거나 정리하지 않는다.
5. MAP00_04~07 Runtime/test 파일은 존재와 계약만 검증하며 수정하지 않는다.

## DO NOT

- Runtime 또는 Editor C# 생성·수정 금지
- 기존 coordinate/constant/utility/architecture test 수정 금지
- `WorldCoordinateUtility` API 변경 또는 test 전용 method 추가 금지
- coordinate debug view, EditorWindow, Gizmo, Scene GUI 선행 생성 금지
- direction/Side/transform/index/neighbor/ID enum 생성 금지
- test C#을 허용된 1개보다 더 만들기 금지
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
- MAP00_09 또는 MAP01 선행 작업 금지

## Inputs

- `MASTER_IMPLEMENTATION_TASK_LIST.md`
- MAP00_07 PASS Result
- `WorldGenConstants`, 네 coordinate value type, `WorldCoordinateUtility`
- 기존 utility/value type/constant/architecture tests
- 보존된 WorldGeneration 구조와 assembly 경계
- Unity Editor `6000.3.8f1`

## Outputs

- `CoordinateConversionBoundaryTests.cs`
- `CoordinateConversionBoundaryTests.cs.meta`
- `REPORTS/MAP00_08_CREATE_COORDINATE_TESTS_RESULT.md`

## Implementation Steps

1. `MASTER_IMPLEMENTATION_TASK_LIST.md`와 `06_IMPLEMENTATION_STATUS.md`에서 이 TASK가 정확한 next/CURRENT인지 확인한다.
2. MAP00_07 Result가 `STATUS: PASS`, utility 10/10, value type 12/12, constant 6/6, architecture 10/10, combined 38/38, compile error 0인지 확인한다.
3. 작업 전 변경 파일 경로를 기록하고 기존 무관 변경은 수정·복구하지 않는다.
4. Preflight Preservation Check의 디렉터리, asmdef, MAP00_04~07 파일과 MAP01 미시작 상태를 확인한다.
5. 신규 target C#과 `.meta`가 모두 absent인지 확인한다.
6. `CoordinateConversionBoundaryTests.cs`를 정확한 EditMode Test Contract로 생성한다.
7. Unity Asset Refresh와 compilation이 완료될 때까지 기다린다.
8. 신규 `.cs.meta` 1개의 GUID 형식과 project-wide uniqueness를 검사한다.
9. 신규 `CoordinateConversionBoundaryTests` fixture를 실행해 actual cases 8개가 모두 PASS인지 확인한다.
10. 방문 count `10,816` microchunk corners와 `259,584` world tiles가 test 내부에서 검증됐는지 확인한다.
11. MAP00_07 `WorldCoordinateUtilityTests` fixture를 실행해 actual cases 10개가 모두 PASS인지 확인한다.
12. MAP00_06 `CoordinateValueTypeTests` fixture를 실행해 actual cases 12개가 모두 PASS인지 확인한다.
13. MAP00_05 `WorldGenConstantsTests` fixture를 실행해 actual cases 6개가 모두 PASS인지 확인한다.
14. MAP00_04 architecture fixture 3개를 실행해 actual cases 10개가 모두 PASS인지 확인한다.
15. 신규 8 + utility 10 + value type 12 + constant 6 + architecture 10, 총 actual cases 46개를 단일 targeted run으로 재검증한다.
16. 작업 후 Asset 변경이 허용된 test C# 1개와 `.meta` 1개뿐인지 확인한다.
17. Result 문서를 작성한다.
18. 모든 DONE CONDITIONS가 PASS인 경우에만 Result에 `STATUS: PASS`를 기록한다.

## Tests

### T1 — Compile

```text
Compile Errors = 0
Relevant New Warnings = 0
```

### T2 — New Exhaustive Coordinate Fixture

```text
Fixture: StarNight.Map.Tests.WorldGeneration.Domain.CoordinateConversionBoundaryTests
Actual cases: 8
Passed: 8
Failed: 0
Skipped: 0
World corners visited: 4
MicroChunk corner samples visited: 10,816
World tile samples visited: 259,584
```

### T3 — Existing Coordinate Utility Regression

```text
Fixture: StarNight.Map.Tests.WorldGeneration.Domain.WorldCoordinateUtilityTests
Actual cases: 10
Passed: 10
Failed: 0
Skipped: 0
```

### T4 — Existing Coordinate Value Type Regression

```text
Fixture: StarNight.Map.Tests.WorldGeneration.Domain.CoordinateValueTypeTests
Actual cases: 12
Passed: 12
Failed: 0
Skipped: 0
```

### T5 — Existing Constant Contract Regression

```text
Fixture: StarNight.Map.Tests.WorldGeneration.Domain.WorldGenConstantsTests
Actual cases: 6
Passed: 6
Failed: 0
Skipped: 0
```

### T6 — Existing Architecture Regression

```text
WorldGenerationModuleStructureTests = PASS
WorldGenerationRuntimeBoundaryTests = PASS
WorldGenerationEditorBoundaryTests = PASS
Actual cases: 10
Passed: 10
Failed: 0
Skipped: 0
```

### T7 — Combined Targeted EditMode Result

```text
Actual cases: 46
Passed: 46
Failed: 0
Skipped: 0
```

### T8 — Asset Meta Validation

- 신규 `.cs.meta` 1개 존재
- GUID 형식 유효
- 프로젝트 전체 GUID와 중복 0

### T9 — Change Scope

이번 TASK의 Asset 변경은 신규 test C# 1개와 `.meta` 1개뿐이다.

기존 무관 변경은 Result에 별도로 기록하며 수정하지 않는다. Runtime, Editor, 기존 test, CSV, asmdef, Scene, Prefab, Package, ProjectSettings 변경은 0개여야 한다.

## Unity Verification

필수:

```text
Unity Version: 6000.3.8f1
Asset Refresh: PASS
Compile Errors: 0
Relevant New Warnings: 0
New Exhaustive Coordinate Tests: PASS (8/8)
Existing Coordinate Utility Tests: PASS (10/10)
Existing Coordinate Value Type Tests: PASS (12/12)
Existing Constant Tests: PASS (6/6)
Existing Architecture Tests: PASS (10/10)
Combined Targeted EditMode Tests: PASS (46/46)
PlayMode Tests: NOT RUN
Scene/Prefab Changes: NONE
```

Unity Editor 또는 Unity MCP에 접근할 수 없어 Asset Refresh, compilation, 대상 EditMode 결과를 확인할 수 없으면 PASS로 종료하지 말고 `BLOCKED`로 기록한다.

## Result File

```text
REPORTS/MAP00_08_CREATE_COORDINATE_TESTS_RESULT.md
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
EXHAUSTIVE COVERAGE
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

- [ ] Current Task가 MAP00_08이고 master backlog의 정확한 next임을 확인했다.
- [ ] 전체 master backlog 205개와 MAP01 HOLD 상태를 확인했다.
- [ ] MAP00_07 Result의 PASS, utility 10/10, value type 12/12, constant 6/6, architecture 10/10, combined 38/38, compile error 0을 확인했다.
- [ ] 보존 대상 디렉터리 4개, asmdef 5개, MAP00_04~07 필수 파일과 `.meta`가 존재한다.
- [ ] `WorldGenConstants` 15개 const, 네 coordinate value type, utility public API 14개 계약을 보존했다.
- [ ] Authoring CSV 0개와 MAP01 이후 미시작 상태를 확인했다.
- [ ] target C#과 `.meta`가 작업 전에 absent였다.
- [ ] 정확한 exhaustive EditMode test C# 1개만 생성했다.
- [ ] 실제 NUnit test case 수가 정확히 8개다.
- [ ] 월드 네 모서리 4개가 exact component와 왕복을 통과했다.
- [ ] 169×16×4 = 10,816 microchunk corner sample이 모두 왕복을 통과했다.
- [ ] 624×416 = 259,584 world tile sample이 모두 왕복을 통과했다.
- [ ] 네 coordinate `TryCreate`가 immediate/extreme invalid axis를 거부했다.
- [ ] `TryToWorld`가 모든 component invalid edge를 default/false로 거부했다.
- [ ] `TryFromWorld`가 모든 outside world edge를 partial output 없이 거부했다.
- [ ] throwing compose/projection API의 exception type과 ParamName이 exact다.
- [ ] Runtime, Editor, 기존 test, debug view, CSV, 생성 로직을 수정·선행 구현하지 않았다.
- [ ] Unity/Legacy Room/MacroChunk/Stage/P6/P11 dependency가 없다.
- [ ] 신규 `.cs.meta` 1개가 존재하며 GUID가 유효하고 project-unique하다.
- [ ] Unity Asset Refresh가 PASS다.
- [ ] Unity Compile Error가 0개다.
- [ ] 관련 신규 Warning이 0개다.
- [ ] 신규 exhaustive coordinate test actual cases 8개가 모두 PASS다.
- [ ] 기존 utility test actual cases 10개가 모두 PASS다.
- [ ] 기존 value type test actual cases 12개가 모두 PASS다.
- [ ] 기존 constant test actual cases 6개가 모두 PASS다.
- [ ] 기존 architecture test actual cases 10개가 모두 PASS다.
- [ ] combined targeted EditMode actual cases 46개가 모두 PASS다.
- [ ] PlayMode 테스트를 실행하지 않았다.
- [ ] Scene/Prefab/Package/ProjectSettings 변경이 0개다.
- [ ] Result 문서가 요구 형식을 충족한다.
- [ ] MAP00_09 또는 MAP01을 시작하지 않았다.

## Completion Rule

TASK EXECUTION은 Result에 `STATUS: PASS / FAIL / BLOCKED`만 기록한다.

Result가 정확히 `STATUS: PASS`이고 모든 DONE CONDITIONS가 완료된 경우에만 STATUS FINALIZE Phase가:

```text
MAP00_08_CREATE_COORDINATE_TESTS: CURRENT -> COMPLETE
Current Task: TASKS/MAP00_08_CREATE_COORDINATE_TESTS.md -> NONE
```

을 수행한다.

STATUS FINALIZE는 MAP00_09를 CURRENT로 바꾸지 않는다. 다음 TASK는 새 패치를 기다린다.

## Expected Next Task

```text
MAP00_09_CREATE_COORDINATE_DEBUG_VIEW
```

다음 TASK는 별도 패치로만 연다.
