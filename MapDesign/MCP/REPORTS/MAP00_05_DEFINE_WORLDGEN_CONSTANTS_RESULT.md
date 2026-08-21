# MAP00_05 Define WorldGen Constants Result

## TASK

`MAP00_05_DEFINE_WORLDGEN_CONSTANTS`

## STATUS

STATUS: PASS

## SUMMARY

광역 WorldGeneration의 잠긴 월드·섹터·마이크로청크 크기와 파생 개수를 `WorldGenConstants` 단일 compile-time 상수 계약으로 구현했다. Runtime에는 `public const int` 15개만 추가했으며, 정확히 6개인 신규 EditMode fixture와 기존 architecture fixture를 모두 통과했다. 좌표 값 타입, 좌표 변환, ID, CSV, 생성 pass, debug view는 구현하지 않았다.

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
- `MapDesign/MCP/TASKS/MAP00_05_DEFINE_WORLDGEN_CONSTANTS.md`
- `MapDesign/MCP/REPORTS/MAP00_04_CREATE_TEST_STRUCTURE_RESULT.md`
- 허용된 asmdef 5개
- 허용된 MAP00_04 architecture test C# 3개
- 신규 Runtime/test C# 2개와 대응 `.meta`

## MASTER BACKLOG CHECK

- 전체 Task: 205
- MAP00_01~04: COMPLETE
- 정확한 next/current: `MAP00_05_DEFINE_WORLDGEN_CONSTANTS`
- MAP00_06~10: LOCKED
- MAP01 이후: LOCKED
- MAP01_01 premade patch: HOLD / DO NOT RUN
- MAP00_06 및 MAP01 선행 작업: NONE

## PREFLIGHT PRESERVATION CHECK

- 필수 WorldGeneration 디렉터리: 4/4 존재
- 필수 asmdef: 5/5 존재
- MAP00_04 architecture test: 3/3 존재
- `Assets/_Game/Map/Data/WorldGeneration/Authoring/**/*.csv`: 0개
- MAP01 이후 Result: 0개
- 작업 전 신규 target C#/`.meta`/Result: 0개
- Runtime WorldGeneration 기존 C#: 0개
- target 디렉터리의 예상하지 않은 직계 파일: 0개

## CREATED

- `Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs.meta`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/WorldGenConstantsTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/WorldGenConstantsTests.cs.meta`
- `MapDesign/MCP/REPORTS/MAP00_05_DEFINE_WORLDGEN_CONSTANTS_RESULT.md`

## CONSTANT CONTRACT

### Base dimensions

- `WorldWidthTiles = 624`
- `WorldHeightTiles = 416`
- `SectorWidthTiles = 48`
- `SectorHeightTiles = 32`
- `MicroChunkWidthTiles = 12`
- `MicroChunkHeightTiles = 8`

### Derived dimensions

- `SectorColumns = WorldWidthTiles / SectorWidthTiles = 13`
- `SectorRows = WorldHeightTiles / SectorHeightTiles = 13`
- `SectorCount = SectorColumns * SectorRows = 169`
- `MicroChunkColumnsPerSector = SectorWidthTiles / MicroChunkWidthTiles = 4`
- `MicroChunkRowsPerSector = SectorHeightTiles / MicroChunkHeightTiles = 4`
- `MicroChunksPerSector = MicroChunkColumnsPerSector * MicroChunkRowsPerSector = 16`
- `TilesPerMicroChunk = MicroChunkWidthTiles * MicroChunkHeightTiles = 96`
- `TilesPerSector = SectorWidthTiles * SectorHeightTiles = 1536`
- `WorldTileCount = WorldWidthTiles * WorldHeightTiles = 259584`

### Static contract audit

- Namespace: `StarNight.Map.WorldGeneration.Domain`
- Type: `public static class WorldGenConstants`
- `public const int`: 15개
- method/property/collection/mutable static state: 0개
- UnityEngine/UnityEditor/Legacy/Room/MacroChunk/Stage/P6/P11 dependency: 0개
- 파생값 initializer의 재하드코딩: 0개

## CHANGED

- MAP00_05의 Asset 변경은 신규 C# 2개와 Unity가 생성한 `.cs.meta` 2개뿐이다.
- 기존 C#, CSV, asmdef/asmref, Scene, Prefab, Package, ProjectSettings는 MAP00_05로 수정하지 않았다.
- 작업 전후 비-Task 변경 항목 수는 4,552개로 같고 상태 해시도 `50D238AB00344327A085078715DEEC73779FCD30B72A1B51073939C885971174`로 동일하다.

## TEST

### New Constant Contract Fixture

- Mode: `EditMode`
- Fixture: `StarNight.Map.Tests.WorldGeneration.Domain.WorldGenConstantsTests`
- Job: `18ba3d5b5d16430ebcc8d4b3dd859d1d`
- Actual cases: 6
- Passed: 6
- Failed: 0
- Skipped: 0
- Duration: 0.8817113 seconds

### Existing Architecture Regression

- Mode: `EditMode`
- Fixtures:
  - `StarNight.Map.Tests.WorldGeneration.WorldGenerationModuleStructureTests`
  - `StarNight.Map.Tests.WorldGeneration.WorldGenerationRuntimeBoundaryTests`
  - `StarNight.MapAuthoring.Tests.WorldGeneration.WorldGenerationEditorBoundaryTests`
- Job: `eed1841e16344dafb56af715b27d9bf5`
- Actual cases: 10
- Passed: 10
- Failed: 0
- Skipped: 0
- Duration: 1.2146194 seconds

### Combined Targeted EditMode Result

- 위 네 fixture를 단일 targeted run으로 재검증
- Job: `e8f83360fc7d4f88baf6d2e45f332fdf`
- Actual cases: 16
- Passed: 16
- Failed: 0
- Skipped: 0
- Duration: 1.7223934 seconds
- Result state: `Passed`
- PlayMode: NOT RUN

## UNITY

- Unity Version: `6000.3.8f1`
- Asset Refresh: PASS
- Compile Errors: 0
- Relevant New Warnings: 0
- New Constant Tests: PASS (6/6)
- Existing Architecture Tests: PASS (10/10)
- Combined Targeted EditMode Tests: PASS (16/16)
- PlayMode Tests: NOT RUN
- Scene/Prefab Changes: NONE
- Editor final state: idle, not compiling, not updating, ready for tools

## ASSET META VALIDATION

- `WorldGenConstants.cs.meta`: `9f1dded20ad91044399b2876886c1083`
- `WorldGenConstantsTests.cs.meta`: `9a28583a22bd23b4a8173a6602338b66`
- 신규 `.cs.meta`: 2/2 존재
- GUID 형식 오류: 0
- 신규 GUID 상호 중복: 0
- 프로젝트 전체 GUID 중복: 0; 각 GUID는 해당 target meta에서 정확히 1회 존재

## OUT_OF_SCOPE_FINDINGS

- 작업 전부터 존재한 4,552개 status 항목은 이전 사용자 지시의 Legacy 이동과 기존 미커밋 변경을 포함한다. MAP00_05는 이를 수정·복구·추가 조사하지 않았으며 비-Task 상태 해시는 작업 전후 동일하다.
- 활성 Scene은 이전 Legacy 이동 이후의 빈 unsaved scene 상태이며, MAP00_05에서는 Scene/Prefab을 변경하지 않았다.
- 작업 전 콘솔에는 현재 Task와 무관한 MCP WebSocket 경고 1건과 Unity AI Account API 접근 경고 1건이 있었다. 격리 compile 직후에는 error/warning 모두 0개였다.
- EditMode 실행 후 Unity Test Runner가 결과 저장 및 PerformanceTesting setup/cleanup 진단 로그를 남겼으나, 세 test job은 모두 명시적으로 succeeded/Passed이며 Task 관련 compile warning/error는 0개다.
- Phase A에서 manifest에 따라 Master backlog, 상태표, Current Task 문서를 적용한 변경은 MAP00_05 Runtime/Test Asset 변경과 별도다.

## DONE CONDITIONS

- [x] Current Task가 MAP00_05이고 master backlog의 정확한 next임을 확인했다.
- [x] 전체 master backlog 205개와 MAP01 HOLD 상태를 확인했다.
- [x] MAP00_04 Result의 PASS, 10/10 test, compile error 0을 확인했다.
- [x] 보존 대상 디렉터리 4개, asmdef 5개, architecture test C# 3개가 존재한다.
- [x] Authoring CSV 0개와 MAP01 이후 미시작 상태를 확인했다.
- [x] target C#과 `.meta`가 작업 전에 absent였다.
- [x] 정확한 Runtime C# 1개와 test C# 1개만 생성했다.
- [x] base dimension 624/416/48/32/12/8을 정확히 정의했다.
- [x] 13/169/4/16/96/1536/259584 파생값을 base constant 식으로 정의했다.
- [x] mutable static state, method, property, collection을 추가하지 않았다.
- [x] Legacy Room/MacroChunk/Stage/P6/P11 dependency가 없다.
- [x] 좌표 값 타입·좌표 변환·ID·CSV를 선행 구현하지 않았다.
- [x] 신규 `.cs.meta` 2개가 존재하며 GUID가 유효하고 project-unique하다.
- [x] Unity Asset Refresh가 PASS다.
- [x] Unity Compile Error가 0개다.
- [x] 관련 신규 Warning이 0개다.
- [x] 신규 constant test actual cases 6개가 모두 PASS다.
- [x] 기존 architecture test actual cases 10개가 모두 PASS다.
- [x] combined targeted EditMode actual cases 16개가 모두 PASS다.
- [x] PlayMode 테스트를 실행하지 않았다.
- [x] Scene/Prefab/Package/ProjectSettings 변경이 0개다.
- [x] Result 문서가 요구 형식을 충족한다.
- [x] MAP00_06 또는 MAP01을 시작하지 않았다.

## NEXT

STATUS FINALIZE 규칙에 따라 MAP00_05를 COMPLETE로 전환하고 Current Task를 `NONE`으로 설정한다. MAP00_06은 시작하지 않고 새 MCP_INBOX 패치를 기다린다.

## Recommended Commit

`feat(map): define locked world generation constants`
