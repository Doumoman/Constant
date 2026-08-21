# MAP00_07 Implement Coordinate Conversions Result

## TASK

`MAP00_07_IMPLEMENT_COORDINATE_CONVERSIONS`

## STATUS

STATUS: PASS

## SUMMARY

`WorldCoordinateUtility` 단일 진입점에 네 좌표 공간의 범위 검사와 `TryCreate`, Sector/MicroChunk/LocalTile 조합 및 WorldTile 분해 계약을 구현했다. Runtime public API는 명시된 14개 메서드만 제공하며, 잘못된 입력을 clamp/wrap하지 않고 `false`와 `default` 또는 정확한 `ArgumentOutOfRangeException`으로 거부한다.

## READ

- `MapDesign/MCP/00_MCP_ENTRYPOINT.md`
- `MapDesign/MCP/01_PROJECT_LOCKED_RULES.md`
- `MapDesign/MCP/02_MCP_WORK_RULES.md`
- `MapDesign/MCP/03_DATA_CSV_RULES.md`
- `MapDesign/MCP/04_UNITY_MCP_RULES.md`
- `MapDesign/MCP/05_CHANGE_CONTROL_RULES.md`
- `MapDesign/MCP/07_PATCH_APPLY_RULES.md`
- `MapDesign/MCP/08_STATUS_FINALIZE_RULES.md`
- `MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md`
- `MapDesign/MCP/06_IMPLEMENTATION_STATUS.md`
- `MapDesign/MCP/TASKS/MAP00_07_IMPLEMENT_COORDINATE_CONVERSIONS.md`
- `MapDesign/MCP/REPORTS/MAP00_06_IMPLEMENT_COORDINATE_VALUE_TYPES_RESULT.md`
- 허용된 asmdef 5개
- 허용된 MAP00_04 architecture test C# 3개
- 허용된 MAP00_05/06 Runtime 및 test C# 7개
- 신규 Runtime/test C# 2개와 대응 `.meta`

## MASTER BACKLOG CHECK

- 전체 Task: 205
- MAP00_01~06: COMPLETE
- 정확한 next/current: `MAP00_07_IMPLEMENT_COORDINATE_CONVERSIONS`
- MAP00_08~10: LOCKED
- MAP01 이후: LOCKED
- MAP01_01 premade patch: HOLD / DO NOT RUN
- MAP00_08 및 MAP01 선행 작업: NONE

## PREFLIGHT PRESERVATION CHECK

- 필수 WorldGeneration 디렉터리: 4/4 존재
- 필수 asmdef: 5/5 존재
- MAP00_04~06 필수 C# 및 대응 `.meta`: 20/20 존재
- `WorldGenConstants`: `public const int` 15개 계약 유지
- 좌표 값 타입: readonly/raw X/Y/equality/hash/string 계약 4/4 유지
- `Assets/_Game/Map/Data/WorldGeneration/Authoring/**/*.csv`: 0개
- MAP01 이후 Result: 0개
- 작업 전 신규 target C#/`.meta`/Result: 0개
- 작업 전 Runtime WorldGeneration C#: 정확히 5개

## CREATED

- `Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldCoordinateUtility.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldCoordinateUtility.cs.meta`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/WorldCoordinateUtilityTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/WorldCoordinateUtilityTests.cs.meta`
- `MapDesign/MCP/REPORTS/MAP00_07_IMPLEMENT_COORDINATE_CONVERSIONS_RESULT.md`

## COORDINATE CONTRACT

- Namespace: `StarNight.Map.WorldGeneration.Domain`
- Type: `public static class WorldCoordinateUtility`
- Public API: 정확히 14개 메서드
- Bounds: 네 좌표 공간 모두 `WorldGenConstants` 기반 half-open 범위
- TryCreate: valid 입력은 원본 좌표와 `true`, invalid 입력은 `default`와 `false`
- Compose: `sector * sector size + microChunk * microChunk size + localTile`
- Decompose: sector quotient, sector remainder 기반 microChunk, microChunk remainder 기반 localTile
- Direct projection: `TryFromWorld`와 동일한 분해 경로 공유
- Invalid throwing API: 정확한 `ParamName`의 `ArgumentOutOfRangeException`
- Unity/Legacy/collection/LINQ/file I/O dependency: 0개
- Runtime 차원 매직 값: 0개

## CHANGED

- MAP00_07 Asset 변경은 신규 Runtime C# 1개, test C# 1개와 Unity 생성 `.cs.meta` 2개뿐이다.
- 기존 C#, CSV, asmdef/asmref, Scene, Prefab, Package, ProjectSettings는 수정하지 않았다.
- 비-Task 상태 항목은 작업 전후 4,584개로 동일하며 상태 해시는 `8C39AC6088D64F7C82EC55C7DAA444228CAC905A1BD7F340F6B7B8948DD99642`로 동일하다.

## TEST

### New Coordinate Utility Fixture

- Mode: `EditMode`
- Fixture: `StarNight.Map.Tests.WorldGeneration.Domain.WorldCoordinateUtilityTests`
- Job: `3001fe3acac34433913ededa712a1eed`
- Actual cases: 10
- Passed: 10
- Failed: 0
- Skipped: 0
- Duration: 0.0412186 seconds

### Existing Coordinate Value Type Regression

- Fixture: `StarNight.Map.Tests.WorldGeneration.Domain.CoordinateValueTypeTests`
- Job: `8284d3dbc8384e84a8e543232a86d289`
- Actual cases: 12
- Passed: 12
- Failed: 0
- Skipped: 0
- Duration: 0.8625777 seconds

### Existing Constant Contract Regression

- Fixture: `StarNight.Map.Tests.WorldGeneration.Domain.WorldGenConstantsTests`
- Job: `a69a5e64e9f44c169aed1d7e4ca3b803`
- Actual cases: 6
- Passed: 6
- Failed: 0
- Skipped: 0
- Duration: 0.853729 seconds

### Existing Architecture Regression

- Fixtures:
  - `StarNight.Map.Tests.WorldGeneration.WorldGenerationModuleStructureTests`
  - `StarNight.Map.Tests.WorldGeneration.WorldGenerationRuntimeBoundaryTests`
  - `StarNight.MapAuthoring.Tests.WorldGeneration.WorldGenerationEditorBoundaryTests`
- Job: `295321acdd06400d819e3273c863b948`
- Actual cases: 10
- Passed: 10
- Failed: 0
- Skipped: 0
- Duration: 1.5042918 seconds

### Combined Targeted EditMode Result

- 위 여섯 fixture를 단일 targeted run으로 재검증
- Job: `a72911a74a314917ad800642dcf01a27`
- Actual cases: 38
- Passed: 38
- Failed: 0
- Skipped: 0
- Duration: 1.9296335 seconds
- Result state: `Passed`
- PlayMode: NOT RUN

## UNITY

- Unity Version: `6000.3.8f1`
- Asset Refresh: PASS
- Compile Errors: 0
- Relevant New Warnings: 0
- New Coordinate Utility Tests: PASS (10/10)
- Existing Coordinate Value Type Tests: PASS (12/12)
- Existing Constant Tests: PASS (6/6)
- Existing Architecture Tests: PASS (10/10)
- Combined Targeted EditMode Tests: PASS (38/38)
- PlayMode Tests: NOT RUN
- Scene/Prefab Changes: NONE
- Editor last observed state: idle, not compiling, not updating

## ASSET META VALIDATION

- `WorldCoordinateUtility.cs.meta`: `0f4276a19f2da514fb25eeaeee5d88ee`
- `WorldCoordinateUtilityTests.cs.meta`: `9bdebc912bf852d4f99a52c230711935`
- 신규 `.cs.meta`: 2/2 존재
- GUID 형식 오류: 0
- 신규 GUID 상호 중복: 0
- 프로젝트 전체 GUID 중복: 0; 각 신규 GUID는 해당 target meta에서 정확히 1회 존재

## OUT_OF_SCOPE_FINDINGS

- 작업 전부터 존재한 4,584개 비-Task status 항목은 이전 Legacy 이동 및 기존 미커밋 변경과 이번 Phase A 문서 적용을 포함한다. MAP00_07은 이를 수정·복구하지 않았고 작업 전후 상태 지문은 동일하다.
- 활성 Scene은 이전 작업에서 이어진 빈 unsaved scene 상태이며, MAP00_07에서는 Scene/Prefab을 변경하지 않았다.
- 최초 scripts-only refresh에서는 Asset import가 생략되어 신규 meta가 생성되지 않았으나, 허용된 강제 all-scope Asset refresh 후 정상 생성·컴파일되었다.
- 첫 compile 확인 시 Task와 무관한 MCP WebSocket 경고 1건이 관찰되었다. 강제 refresh 및 최종 compile 후 error/warning 조회 결과는 0건이었다.
- Phase A에서 manifest에 따라 Master backlog, 상태표, Current Task 문서를 적용한 변경은 MAP00_07 Runtime/Test Asset 변경과 별도다.

## DONE CONDITIONS

- [x] Current Task가 MAP00_07이고 master backlog의 정확한 next임을 확인했다.
- [x] 전체 master backlog 205개와 MAP01 HOLD 상태를 확인했다.
- [x] MAP00_06 Result의 PASS, new 12/12, constant 6/6, architecture 10/10, combined 28/28, compile error 0을 확인했다.
- [x] 보존 대상 디렉터리 4개, asmdef 5개, MAP00_04~06 필수 파일과 `.meta`가 존재한다.
- [x] `WorldGenConstants` 15개 const와 네 coordinate value type 계약을 보존했다.
- [x] Authoring CSV 0개와 MAP01 이후 미시작 상태를 확인했다.
- [x] target C#과 `.meta`가 작업 전에 absent였다.
- [x] 정확히 Runtime utility C# 1개와 test C# 1개만 생성했다.
- [x] 네 좌표 공간의 `IsValid`가 constants 기반 exact half-open 범위를 사용한다.
- [x] 네 `TryCreate`가 invalid output을 default로 두고 false를 반환한다.
- [x] `TryToWorld`와 `ToWorld`가 exact 조합 공식을 사용한다.
- [x] `TryFromWorld`와 direct projection이 exact 분해 공식을 공유한다.
- [x] invalid 입력은 clamp/wrap하지 않고 false 또는 `ArgumentOutOfRangeException`으로 거부한다.
- [x] public API가 명시된 method 14개뿐이다.
- [x] exhaustive MAP00_08 test, debug view, CSV, 생성 로직을 선행 구현하지 않았다.
- [x] Unity/Legacy Room/MacroChunk/Stage/P6/P11 dependency가 없다.
- [x] 신규 `.cs.meta` 2개가 존재하며 GUID가 유효하고 project-unique하다.
- [x] Unity Asset Refresh가 PASS했다.
- [x] Unity Compile Error가 0개다.
- [x] 관련 신규 Warning이 0개다.
- [x] 신규 coordinate utility test actual cases 10개가 모두 PASS했다.
- [x] 기존 value type test actual cases 12개가 모두 PASS했다.
- [x] 기존 constant test actual cases 6개가 모두 PASS했다.
- [x] 기존 architecture test actual cases 10개가 모두 PASS했다.
- [x] combined targeted EditMode actual cases 38개가 모두 PASS했다.
- [x] PlayMode 테스트를 실행하지 않았다.
- [x] Scene/Prefab/Package/ProjectSettings 변경이 0개다.
- [x] Result 문서가 요구 형식을 충족한다.
- [x] MAP00_08 또는 MAP01을 시작하지 않았다.

## NEXT

STATUS FINALIZE 규칙에 따라 MAP00_07을 COMPLETE로 전환하고 Current Task를 `NONE`으로 설정한다. MAP00_08은 시작하지 않고 새 MCP_INBOX 패치를 기다린다.

## Recommended Commit

`feat(map): add coordinate conversion utility`
