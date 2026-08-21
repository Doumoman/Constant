# MAP00_06 — Implement Coordinate Value Types

```yaml
status_control:
  task_key: MAP00_06_IMPLEMENT_COORDINATE_VALUE_TYPES
  result_file: REPORTS/MAP00_06_IMPLEMENT_COORDINATE_VALUE_TYPES_RESULT.md
```

## TASK TYPE

```text
RUNTIME VALUE TYPE CONTRACT + EDITMODE TEST
```

## Objective

광역 WorldGeneration에서 서로 다른 좌표 공간을 혼용하지 않도록 `WorldTileCoord`, `SectorCoord`, `MicroChunkCoord`, `LocalTileCoord` 네 종류를 독립된 immutable readonly 값 타입으로 구현한다.

이 TASK는 좌표의 원시 `X`, `Y` 성분과 값 동등성 계약만 만든다. 좌표 변환, 유효 범위, `TryCreate`, clamp, 인덱스 계산, 방향, ID, CSV loader, 생성 pass 또는 debug view는 구현하지 않는다.

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
12. `REPORTS/MAP00_05_DEFINE_WORLDGEN_CONSTANTS_RESULT.md`

## READ ALLOWLIST

본문 읽기 허용:

- Mandatory Read Order의 파일
- `Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef`
- `Assets/_Game/Editor/MapAuthoring/MapAuthoring.Editor.asmdef`
- `Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef`
- `Assets/_Game/Tests/PlayMode/Map/Game.Map.Tests.PlayMode.asmdef`
- `Assets/_Game/Editor/MapAuthoring/Tests/EditMode/MapAuthoring.Tests.EditMode.asmdef`
- `Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/WorldGenConstantsTests.cs`
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

기존 constant/architecture test 실행 중 테스트 코드가 승인 범위를 검사하는 것은 허용한다.

금지:

- 승인되지 않은 프로젝트 C# 본문 스캔
- `Assets/_Legacy/**` 본문 열람 또는 수정
- Scene/Prefab YAML 열람
- CSV/GDD/과거 하네스 본문 열람
- 테스트 통과를 위해 기존 코드를 수정하는 행위

## Master Backlog Check

`MASTER_IMPLEMENTATION_TASK_LIST.md`에서 다음을 확인한다.

```text
MAP00_01~05 = COMPLETE
MAP00_06_IMPLEMENT_COORDINATE_VALUE_TYPES = next/current
MAP00_07~10 = LOCKED
MAP01_01 premade patch = HOLD / DO NOT RUN
전체 Task = 205
```

하나라도 다르면 임의 보정하지 말고 `BLOCKED` Result를 작성한다.

## Preflight Preservation Check

MAP00_05 Result의 PASS와 아래 항목을 확인한다.

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

필수 MAP00_04/05 파일:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/WorldGenerationModuleStructureTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/WorldGenerationRuntimeBoundaryTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/WorldGenerationEditorBoundaryTests.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs.meta
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/WorldGenConstantsTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/WorldGenConstantsTests.cs.meta
```

추가 순서 검증:

- `WorldGenConstants`는 MAP00_05 Result의 15개 `public const int` 계약을 유지해야 한다.
- `WorldGenConstantsTests` actual cases 6개와 architecture actual cases 10개가 직전 PASS 상태여야 한다.
- `Assets/_Game/Map/Data/WorldGeneration/Authoring/**/*.csv`는 0개여야 한다.
- MAP01 이후 Result가 존재하거나 Task가 COMPLETE/CURRENT이면 안 된다.
- 아래 신규 target C#과 `.meta`가 이미 존재하면 안 된다.
- Runtime WorldGeneration의 기존 C#은 `WorldGenConstants.cs` 1개뿐이어야 한다. 예상하지 않은 C#이 있으면 삭제·수정하지 말고 `BLOCKED`다.

위 조건이 다르면 기존 파일을 이동·복원·삭제·덮어쓰지 말고 `BLOCKED`다.

## WRITE ALLOWLIST

정확히 다음 Runtime C# 4개와 Unity가 생성하는 대응 `.meta`를 생성할 수 있다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldTileCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldTileCoord.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/Domain/MicroChunkCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/MicroChunkCoord.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/Domain/LocalTileCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/LocalTileCoord.cs.meta
```

정확히 다음 EditMode test C# 1개와 Unity가 생성하는 대응 `.meta`를 생성할 수 있다.

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/CoordinateValueTypeTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/CoordinateValueTypeTests.cs.meta
```

추가 생성 허용:

```text
MapDesign/MCP/REPORTS/MAP00_06_IMPLEMENT_COORDINATE_VALUE_TYPES_RESULT.md
```

TASK EXECUTION 중 `06_IMPLEMENTATION_STATUS.md`는 수정하지 않는다. 상태 변경은 Result PASS 이후 STATUS FINALIZE만 수행한다.

## Runtime Type Contract

네 파일 모두 다음 namespace를 사용한다.

```csharp
namespace StarNight.Map.WorldGeneration.Domain
```

각 파일은 파일명과 같은 public type 하나만 선언한다.

```csharp
public readonly struct WorldTileCoord : IEquatable<WorldTileCoord>
public readonly struct SectorCoord : IEquatable<SectorCoord>
public readonly struct MicroChunkCoord : IEquatable<MicroChunkCoord>
public readonly struct LocalTileCoord : IEquatable<LocalTileCoord>
```

각 type은 같은 형태의 다음 public API만 제공한다. 아래 `T`는 해당 type 자체다.

```csharp
public int X { get; }
public int Y { get; }
public T(int x, int y)
public bool Equals(T other)
public override bool Equals(object obj)
public override int GetHashCode()
public override string ToString()
public static bool operator ==(T left, T right)
public static bool operator !=(T left, T right)
```

### Component storage

- constructor는 전달된 `x`, `y`를 각각 `X`, `Y`에 그대로 저장한다.
- 음수, 0, 양수, `int.MinValue`, `int.MaxValue`를 검사·거부·보정하지 않는다.
- 이는 유효 좌표를 의미하는 것이 아니다. 범위 판정과 `TryCreate`는 MAP00_07의 책임이다.
- setter, mutable field, additional state를 만들지 않는다.

### Equality and hash

- 두 좌표는 동일한 concrete type이고 `X`, `Y`가 모두 같을 때만 같다.
- `Equals(T)`, `Equals(object)`, `==`, `!=`는 같은 결과를 반환한다.
- 동일한 좌표는 반드시 동일한 hash code를 반환한다.
- hash는 process마다 달라지는 randomized API나 문자열 변환에 의존하지 않는다.
- 정확한 구현식은 각 type에서 다음으로 고정한다.

```csharp
unchecked
{
    return (X * 397) ^ Y;
}
```

### Stable string

`ToString()`은 `CultureInfo.InvariantCulture`를 사용해 다음 exact format을 반환한다.

```text
WorldTileCoord(X, Y)
SectorCoord(X, Y)
MicroChunkCoord(X, Y)
LocalTileCoord(X, Y)
```

예:

```text
WorldTileCoord(12, -3)
SectorCoord(4, 9)
MicroChunkCoord(2, 1)
LocalTileCoord(11, 7)
```

문자열 형식 구현은 각 파일에서 다음 패턴을 사용한다.

```csharp
return string.Format(
    CultureInfo.InvariantCulture,
    "TypeName({0}, {1})",
    X,
    Y);
```

## Runtime Implementation Rules

- `using System;`과 `using System.Globalization;`만 사용할 수 있다.
- 모든 좌표는 class/record가 아닌 `public readonly struct`다.
- 네 type 사이 상속, 암시적·명시적 변환 operator, 공통 base/interface를 추가하지 않는다.
- `UnityEngine.Vector2Int`, tuple, array, collection, LINQ, reflection, file I/O를 사용하지 않는다.
- 좌표 arithmetic, offset, neighbor, index, bounds, clamp, normalize, validate, parse를 구현하지 않는다.
- `WorldGenConstants`를 참조해 유효성을 검사하지 않는다.
- Unity 직렬화용 field/attribute 또는 custom drawer를 추가하지 않는다.
- 최신 명칭은 `MicroChunkCoord`다. `ChunkCoord`, `RoomCoord`, `MacroChunkCoord`를 별칭이나 호환 type으로 만들지 않는다.
- 기존 Legacy Room/MacroChunk/Stage/P6/P11 타입을 참조하거나 재사용하지 않는다.

## EditMode Test Contract

파일:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/CoordinateValueTypeTests.cs
```

namespace와 fixture:

```csharp
namespace StarNight.Map.Tests.WorldGeneration.Domain
{
    public sealed class CoordinateValueTypeTests
    {
    }
}
```

NUnit `[Test]` method를 정확히 12개 만든다. parameterized test와 `[TestCase]`는 사용하지 않는다.

### WorldTileCoord — 3 cases

1. `WorldTileCoord_StoresRawComponents`
   - type이 value type이며 `IsReadOnlyAttribute`가 존재함을 확인
   - 음수와 `int.MaxValue`를 constructor에 전달하고 `X`, `Y`가 그대로 저장되는지 확인
2. `WorldTileCoord_ImplementsValueEquality`
   - 동일값/다른 X/다른 Y에 대해 typed/object Equals, `==`, `!=` 확인
   - 동일값의 hash code가 같은지 확인
3. `WorldTileCoord_ToStringIsStable`
   - `(12, -3)`이 exact `WorldTileCoord(12, -3)`인지 확인

### SectorCoord — 3 cases

4. `SectorCoord_StoresRawComponents`
   - readonly value type과 raw `X`, `Y` 저장 확인
5. `SectorCoord_ImplementsValueEquality`
   - typed/object Equals, operator, hash 계약 확인
6. `SectorCoord_ToStringIsStable`
   - `(4, 9)`가 exact `SectorCoord(4, 9)`인지 확인

### MicroChunkCoord — 3 cases

7. `MicroChunkCoord_StoresRawComponents`
   - readonly value type과 raw `X`, `Y` 저장 확인
8. `MicroChunkCoord_ImplementsValueEquality`
   - typed/object Equals, operator, hash 계약 확인
9. `MicroChunkCoord_ToStringIsStable`
   - `(2, 1)`이 exact `MicroChunkCoord(2, 1)`인지 확인

### LocalTileCoord — 3 cases

10. `LocalTileCoord_StoresRawComponents`
    - readonly value type과 raw `X`, `Y` 저장 확인
11. `LocalTileCoord_ImplementsValueEquality`
    - typed/object Equals, operator, hash 계약 확인
12. `LocalTileCoord_ToStringIsStable`
    - `(11, 7)`이 exact `LocalTileCoord(11, 7)`인지 확인

Test implementation rules:

- 기존 Unity Test Framework와 NUnit만 사용한다.
- readonly 확인에는 `System.Runtime.CompilerServices.IsReadOnlyAttribute` reflection만 허용한다.
- test helper가 필요하면 test 파일 내부 private member로만 둔다.
- 네 equality test는 다른 coordinate type과의 비교를 강제하거나 boxing 변환을 추가하지 않는다.
- 유효 범위, 변환, 경계, 왕복, 모든 169×16 영역을 테스트하지 않는다. 이는 MAP00_07/08의 범위다.
- production 파일을 생성·수정·삭제하지 않는다.

## Collision Handling

1. 신규 target C#이 이미 존재하면 본문을 읽어 병합하거나 덮어쓰지 않는다.
2. target `.meta`만 orphan 상태로 존재하면 GUID를 검사한 뒤 `BLOCKED`로 보고한다. 이번 create-only Task에서 재사용하지 않는다.
3. 승인 경로에 예상하지 않은 다른 C#이 있으면 삭제하지 않고 경로만 기록한 뒤 `BLOCKED`다.
4. 기존 사용자 변경을 되돌리거나 정리하지 않는다.
5. MAP00_05 파일은 존재와 계약만 검증하며 수정하지 않는다.

## DO NOT

- 좌표 변환 method 또는 extension method 생성 금지
- bounds/`IsValid`/`TryCreate`/clamp/exception 생성 금지
- direction/Side/transform/index/ID enum 생성 금지
- Runtime C#을 허용된 4개보다 더 만들거나 기존 Runtime을 수정 금지
- test C#을 허용된 1개보다 더 만들거나 기존 test를 수정 금지
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
- MAP00_07 또는 MAP01 선행 작업 금지

## Inputs

- `MASTER_IMPLEMENTATION_TASK_LIST.md`
- MAP00_05 PASS Result
- `WorldGenConstants`와 constant contract test
- 보존된 WorldGeneration 구조와 assembly/test 경계
- Unity Editor `6000.3.8f1`

## Outputs

- `WorldTileCoord.cs`와 `.meta`
- `SectorCoord.cs`와 `.meta`
- `MicroChunkCoord.cs`와 `.meta`
- `LocalTileCoord.cs`와 `.meta`
- `CoordinateValueTypeTests.cs`와 `.meta`
- `REPORTS/MAP00_06_IMPLEMENT_COORDINATE_VALUE_TYPES_RESULT.md`

## Implementation Steps

1. `MASTER_IMPLEMENTATION_TASK_LIST.md`와 `06_IMPLEMENTATION_STATUS.md`에서 이 TASK가 정확한 next/CURRENT인지 확인한다.
2. MAP00_05 Result가 `STATUS: PASS`, new 6/6, architecture 10/10, combined 16/16, compile error 0인지 확인한다.
3. 작업 전 변경 파일 경로를 기록하고 기존 무관 변경은 수정·복구하지 않는다.
4. Preflight Preservation Check의 디렉터리, asmdef, MAP00_04/05 파일과 MAP01 미시작 상태를 확인한다.
5. 신규 target C# 5개와 `.meta` 5개가 모두 absent인지 확인한다.
6. Runtime 좌표 C# 4개를 정확한 Runtime Type Contract로 생성한다.
7. `CoordinateValueTypeTests.cs`를 정확히 12개 test case로 생성한다.
8. Unity Asset Refresh와 compilation이 완료될 때까지 기다린다.
9. 신규 `.cs.meta` 5개의 GUID 형식과 project-wide uniqueness를 검사한다.
10. 신규 `CoordinateValueTypeTests` fixture를 실행해 actual cases 12개가 모두 PASS인지 확인한다.
11. MAP00_05 `WorldGenConstantsTests` fixture를 실행해 actual cases 6개가 모두 PASS인지 확인한다.
12. MAP00_04 architecture fixture 3개를 실행해 기존 actual cases 10개가 모두 PASS인지 확인한다.
13. 신규 12 + constant 6 + architecture 10, 총 actual cases 28개를 단일 targeted run으로 재검증한다.
14. Runtime source 4개가 승인 namespace, readonly struct, exact API이고 변환/bounds/Unity/Legacy dependency가 없는지 확인한다.
15. 작업 후 Asset 변경이 허용된 C# 5개와 `.meta` 5개뿐인지 확인한다.
16. Result 문서를 작성한다.
17. 모든 DONE CONDITIONS가 PASS인 경우에만 Result에 `STATUS: PASS`를 기록한다.

## Tests

### T1 — Compile

```text
Compile Errors = 0
Relevant New Warnings = 0
```

### T2 — New Coordinate Value Type Fixture

```text
Fixture: StarNight.Map.Tests.WorldGeneration.Domain.CoordinateValueTypeTests
Actual cases: 12
Passed: 12
Failed: 0
Skipped: 0
```

### T3 — Existing Constant Contract Regression

```text
Fixture: StarNight.Map.Tests.WorldGeneration.Domain.WorldGenConstantsTests
Actual cases: 6
Passed: 6
Failed: 0
Skipped: 0
```

### T4 — Existing Architecture Regression

```text
WorldGenerationModuleStructureTests = PASS
WorldGenerationRuntimeBoundaryTests = PASS
WorldGenerationEditorBoundaryTests = PASS
Actual cases: 10
Passed: 10
Failed: 0
Skipped: 0
```

### T5 — Combined Targeted EditMode Result

```text
Actual cases: 28
Passed: 28
Failed: 0
Skipped: 0
```

### T6 — Asset Meta Validation

- 신규 `.cs.meta` 5개 존재
- GUID 형식 유효
- 신규 GUID끼리 중복 0
- 프로젝트 전체 GUID와 중복 0

### T7 — Change Scope

이번 TASK의 Asset 변경은 신규 C# 5개와 `.meta` 5개뿐이다.

기존 무관 변경은 Result에 별도로 기록하며 수정하지 않는다. C#, CSV, asmdef, Scene, Prefab, Package, ProjectSettings의 다른 변경은 0개여야 한다.

## Unity Verification

필수:

```text
Unity Version: 6000.3.8f1
Asset Refresh: PASS
Compile Errors: 0
Relevant New Warnings: 0
New Coordinate Value Type Tests: PASS (12/12)
Existing Constant Tests: PASS (6/6)
Existing Architecture Tests: PASS (10/10)
Combined Targeted EditMode Tests: PASS (28/28)
PlayMode Tests: NOT RUN
Scene/Prefab Changes: NONE
```

Unity Editor 또는 Unity MCP에 접근할 수 없어 Asset Refresh, compilation, 대상 EditMode 결과를 확인할 수 없으면 PASS로 종료하지 말고 `BLOCKED`로 기록한다.

## Result File

```text
REPORTS/MAP00_06_IMPLEMENT_COORDINATE_VALUE_TYPES_RESULT.md
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
VALUE TYPE CONTRACT
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

- [ ] Current Task가 MAP00_06이고 master backlog의 정확한 next임을 확인했다.
- [ ] 전체 master backlog 205개와 MAP01 HOLD 상태를 확인했다.
- [ ] MAP00_05 Result의 PASS, new 6/6, architecture 10/10, combined 16/16, compile error 0을 확인했다.
- [ ] 보존 대상 디렉터리 4개, asmdef 5개, MAP00_04/05 필수 파일이 존재한다.
- [ ] `WorldGenConstants` 15개 const 계약을 보존했다.
- [ ] Authoring CSV 0개와 MAP01 이후 미시작 상태를 확인했다.
- [ ] target C#과 `.meta`가 작업 전에 absent였다.
- [ ] 정확한 Runtime readonly struct C# 4개와 test C# 1개만 생성했다.
- [ ] 네 type이 raw `X`, `Y`를 보정 없이 저장한다.
- [ ] 네 type이 typed/object equality, `==`, `!=`, deterministic hash 계약을 구현한다.
- [ ] 네 type이 invariant exact `TypeName(X, Y)` 문자열을 구현한다.
- [ ] mutable state, conversion, bounds, `TryCreate`, arithmetic, Unity dependency가 없다.
- [ ] Legacy Room/MacroChunk/Stage/P6/P11 dependency가 없다.
- [ ] 신규 `.cs.meta` 5개가 존재하며 GUID가 유효하고 project-unique하다.
- [ ] Unity Asset Refresh가 PASS다.
- [ ] Unity Compile Error가 0개다.
- [ ] 관련 신규 Warning이 0개다.
- [ ] 신규 value type test actual cases 12개가 모두 PASS다.
- [ ] 기존 constant test actual cases 6개가 모두 PASS다.
- [ ] 기존 architecture test actual cases 10개가 모두 PASS다.
- [ ] combined targeted EditMode actual cases 28개가 모두 PASS다.
- [ ] PlayMode 테스트를 실행하지 않았다.
- [ ] Scene/Prefab/Package/ProjectSettings 변경이 0개다.
- [ ] Result 문서가 요구 형식을 충족한다.
- [ ] MAP00_07 또는 MAP01을 시작하지 않았다.

## Completion Rule

TASK EXECUTION은 Result에 `STATUS: PASS / FAIL / BLOCKED`만 기록한다.

Result가 정확히 `STATUS: PASS`이고 모든 DONE CONDITIONS가 완료된 경우에만 STATUS FINALIZE Phase가:

```text
MAP00_06_IMPLEMENT_COORDINATE_VALUE_TYPES: CURRENT -> COMPLETE
Current Task: TASKS/MAP00_06_IMPLEMENT_COORDINATE_VALUE_TYPES.md -> NONE
```

을 수행한다.

STATUS FINALIZE는 MAP00_07을 CURRENT로 바꾸지 않는다. 다음 TASK는 새 패치를 기다린다.

## Expected Next Task

```text
MAP00_07_IMPLEMENT_COORDINATE_CONVERSIONS
```

다음 TASK는 별도 패치로만 연다.
