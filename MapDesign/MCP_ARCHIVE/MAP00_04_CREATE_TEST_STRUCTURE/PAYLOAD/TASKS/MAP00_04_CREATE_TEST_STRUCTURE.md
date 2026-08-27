# MAP00_04 — Create Test Structure

```yaml
status_control:
  task_key: MAP00_04_CREATE_TEST_STRUCTURE
  result_file: REPORTS/MAP00_04_CREATE_TEST_STRUCTURE_RESULT.md
```

## TASK TYPE

```text
EDITMODE TEST FOUNDATION
```

## Objective

MAP00_01~03에서 확정한 WorldGeneration 폴더·namespace·assembly·dependency 경계를 이후 구현이 깨뜨리지 못하도록 기존 테스트 assembly 안에 최소 EditMode 아키텍처 테스트 3개를 만든다.

이 TASK는 테스트 안전망만 만든다. 프로덕션 WorldGeneration 타입, CSV loader, 좌표 타입, 생성 알고리즘은 구현하지 않는다.

## Mandatory Read Order

1. `00_MCP_ENTRYPOINT.md`
2. `01_PROJECT_LOCKED_RULES.md`
3. `02_MCP_WORK_RULES.md`
4. `03_DATA_CSV_RULES.md`
5. `04_UNITY_MCP_RULES.md`
6. `05_CHANGE_CONTROL_RULES.md`
7. `07_PATCH_APPLY_RULES.md`
8. `08_STATUS_FINALIZE_RULES.md`
9. `06_IMPLEMENTATION_STATUS.md`
10. 이 TASK
11. `REPORTS/MAP00_01_PROJECT_AUDIT_RESULT.md`
12. `REPORTS/MAP00_02_FOLDER_AND_ASMDEF_PLAN_RESULT.md`
13. `REPORTS/MAP00_03_CREATE_MAP_MODULE_STRUCTURE_RESULT.md`

## READ ALLOWLIST

본문 읽기 허용:

- Mandatory Read Order의 파일
- `Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef`
- `Assets/_Game/Editor/MapAuthoring/MapAuthoring.Editor.asmdef`
- `Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef`
- `Assets/_Game/Tests/PlayMode/Map/Game.Map.Tests.PlayMode.asmdef`
- `Assets/_Game/Editor/MapAuthoring/Tests/EditMode/MapAuthoring.Tests.EditMode.asmdef`

기존 테스트 스타일 확인을 위한 제한적 검색:

- `Assets/_Game/Tests/EditMode/Map/`에서 WorldGeneration 밖의 기존 NUnit 테스트 파일명을 먼저 확인하고 관련성이 높은 최대 2개 본문만 읽을 수 있다.
- `Assets/_Game/Editor/MapAuthoring/Tests/EditMode/`에서 WorldGeneration 밖의 기존 NUnit 테스트 파일명을 먼저 확인하고 관련성이 높은 최대 2개 본문만 읽을 수 있다.
- 승인된 36개 WorldGeneration 디렉터리의 경로와 직계 파일명을 확인할 수 있다.
- 작업 전후 변경 파일 경로만 확인할 수 있다.

새 테스트 실행 중 테스트 코드가 검사할 수 있는 범위:

- 승인된 Runtime WorldGeneration 루트 아래의 `*.cs` 텍스트
- 위 5개 asmdef 텍스트
- 승인된 36개 디렉터리의 존재 여부

금지:

- 승인되지 않은 프로젝트 C# 본문 스캔
- Scene/Prefab YAML 열람
- CSV/GDD/과거 하네스 본문 열람
- 테스트 통과를 위해 기존 코드를 수정하는 행위

## WRITE ALLOWLIST

정확히 다음 테스트 파일 3개와 Unity가 생성하는 대응 `.meta`만 생성할 수 있다.

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/WorldGenerationModuleStructureTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/WorldGenerationRuntimeBoundaryTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/WorldGenerationEditorBoundaryTests.cs
```

추가 생성 허용:

```text
MapDesign/MCP/REPORTS/MAP00_04_CREATE_TEST_STRUCTURE_RESULT.md
```

TASK EXECUTION 중 `06_IMPLEMENTATION_STATUS.md`는 수정하지 않는다. 상태 변경은 Result PASS 이후 STATUS FINALIZE만 수행한다.

## DO NOT

- `Assets/_Game/Map/Runtime/WorldGeneration/**/*.cs` 생성·수정 금지
- `Assets/_Game/Editor/MapAuthoring/WorldGeneration/**/*.cs` 생성·수정 금지
- PlayMode 테스트 생성 금지
- 기존 테스트 수정 금지
- asmdef/asmref 생성·수정 금지
- CSV 또는 CSV 스키마 생성·수정 금지
- ScriptableObject, Scene, Prefab, Tile, Tile Palette, Animator, Addressables 변경 금지
- `Assets/_Game/Stage/**` 변경 금지
- `Assets/StarNight/**` 변경 금지
- `Packages/**`, `ProjectSettings/**` 변경 금지
- 새 package/dependency 설치 금지
- 기존 파일·폴더 삭제/이동/이름 변경 금지
- 관련 없는 포맷팅·warning 수정 금지
- Git commit/push/branch/reset/rebase/force 금지
- MAP01 선행 작업 금지

## Required Test Contracts

### A. WorldGenerationModuleStructureTests

namespace:

```text
StarNight.Map.Tests.WorldGeneration
```

필수 검증:

1. MAP00_03의 승인 디렉터리 36개가 모두 존재한다.
2. Runtime/Editor/Test/Authoring Data 주요 루트가 서로 구분된다.
3. WorldGeneration 하위에 새로운 asmdef/asmref가 존재하지 않는다.
4. 경로 검사는 프로젝트 루트를 안전하게 계산하며 사용자 PC의 절대 경로를 하드코딩하지 않는다.

### B. WorldGenerationRuntimeBoundaryTests

namespace:

```text
StarNight.Map.Tests.WorldGeneration
```

필수 검증:

1. `Game.Map.Runtime.asmdef`의 name이 `Game.Map.Runtime`이다.
2. Runtime asmdef에 새 assembly reference가 추가되지 않았고 `UnityEditor` 참조가 없다.
3. Runtime WorldGeneration 아래에 향후 생성되는 모든 `.cs`는 namespace가 `StarNight.Map.WorldGeneration` 또는 그 하위다.
4. Runtime WorldGeneration `.cs`에서 다음 의존성·식별자를 금지한다.

```text
using UnityEditor
StarNight.Stage
StarNight.Generation.P6
StarNight.MapHarness.P11
StageMapGenerator
P6RoomGraphGenerator
P11MapStageHarness2D
```

5. 다음 기존 타입명을 신규 광역 타입 선언명으로 재사용하지 못하게 한다.

```text
GridWorld
StageMapGenerator
StageMapProfile
StageGeneratedLayout
RoomTemplate
RoomGridTransform
P6RoomGraphGenerator
TileMutationService
P11MapStageHarness2D
```

주석이나 문자열의 단순 단어 출현 때문에 오탐하지 않도록, 가능한 범위에서 using/namespace/type declaration을 대상으로 검사한다. Runtime 소스가 아직 0개인 현재 상태는 허용한다.

### C. WorldGenerationEditorBoundaryTests

namespace:

```text
StarNight.MapAuthoring.Tests.WorldGeneration
```

필수 검증:

1. `MapAuthoring.Editor`가 Editor-only다.
2. `MapAuthoring.Editor`가 `Game.Map.Runtime`을 참조한다.
3. `Game.Map.Tests.EditMode`가 `Game.Map.Runtime`과 필요한 Test Runner를 참조한다.
4. `MapAuthoring.Tests.EditMode`가 `Game.Map.Runtime`, `MapAuthoring.Editor`, 필요한 Test Runner를 참조한다.
5. 새 WorldGeneration 전용 asmdef가 존재하지 않는다.

## Implementation Rules

- NUnit과 프로젝트에 이미 존재하는 Unity Test Framework만 사용한다.
- 외부 JSON/NUnit/helper package를 추가하지 않는다.
- 테스트 전용 작은 helper는 위 3개 파일 내부의 private/internal 멤버로만 둔다.
- 세 테스트 파일 사이에 테스트 assembly를 넘는 compile-time dependency를 만들지 않는다.
- OS 경로 separator 차이를 처리한다.
- 파일 열거 결과는 정렬해 실패 메시지가 결정적이어야 한다.
- 실패 메시지에는 위반 파일의 프로젝트 상대 경로와 위반 규칙을 포함한다.
- 검사 대상 파일이 없음을 테스트 실패로 처리하지 않는다.
- 테스트가 프로덕션 파일을 생성·수정·삭제하면 안 된다.

## Implementation Steps

1. Current Task가 MAP00_04인지 확인한다.
2. MAP00_03 Result가 PASS이고 36개 디렉터리가 존재하는지 확인한다.
3. 작업 전 변경 파일 경로를 기록하고 기존 무관 변경은 건드리지 않는다.
4. 5개 asmdef와 허용된 기존 테스트 최대 4개를 읽어 실제 NUnit/namespace/assembly convention을 확인한다.
5. `WorldGenerationModuleStructureTests.cs`를 구현한다.
6. `WorldGenerationRuntimeBoundaryTests.cs`를 구현한다.
7. `WorldGenerationEditorBoundaryTests.cs`를 구현한다.
8. Unity Asset Refresh와 compilation을 완료한다.
9. 새 테스트 3개가 포함된 대상 EditMode 테스트만 실행한다.
10. 테스트 수, PASS/FAIL, duration을 기록한다.
11. 새 `.meta` GUID 유효성과 프로젝트 중복 여부를 확인한다.
12. 작업 후 변경 파일 경로가 허용된 6개 Asset 파일과 Result뿐인지 확인한다.
13. Result를 작성한다.

## Tests

### T1 — Compile

```text
Compile Errors = 0
Relevant New Warnings = 0
```

### T2 — Runtime EditMode Architecture Tests

- `WorldGenerationModuleStructureTests`
- `WorldGenerationRuntimeBoundaryTests`

모든 test case PASS.

### T3 — Editor EditMode Architecture Tests

- `WorldGenerationEditorBoundaryTests`

모든 test case PASS.

### T4 — Asset Meta Validation

- 신규 `.cs.meta` 3개 존재
- GUID 형식 유효
- 신규 및 프로젝트 전체 GUID 중복 0

### T5 — Change Scope

이번 TASK의 Asset 변경은 새 `.cs` 3개와 `.cs.meta` 3개뿐이다.

기존 무관 변경은 Result에 별도로 기록하며 수정하지 않는다.

## Unity Verification

필수:

```text
Unity Version: 6000.3.8f1
Asset Refresh: PASS
Compile Errors: 0
Relevant New Warnings: 0
Targeted EditMode Tests: PASS
PlayMode Tests: NOT RUN
Scene/Prefab Changes: NONE
```

Unity Editor 또는 Unity MCP에 접근할 수 없어 compile과 EditMode 결과를 확인할 수 없으면 PASS로 종료하지 말고 `BLOCKED`로 기록한다.

## Result File

```text
REPORTS/MAP00_04_CREATE_TEST_STRUCTURE_RESULT.md
```

Result 필수 섹션:

```text
TASK
STATUS
SUMMARY
READ
CREATED
CHANGED
TEST CONTRACTS IMPLEMENTED
TEST
UNITY
OUT_OF_SCOPE_FINDINGS
DONE CONDITIONS
NEXT
Recommended Commit
```

테스트별 실제 실행 case 수와 PASS/FAIL을 기록한다. 테스트 필터가 포함한 정확한 fixture 이름도 기록한다.

## DONE CONDITIONS

- [ ] Current Task가 MAP00_04임을 확인했다.
- [ ] MAP00_03 Result PASS를 확인했다.
- [ ] 지정된 테스트 `.cs` 3개만 생성했다.
- [ ] 대응 `.cs.meta` 3개가 존재하고 GUID가 유효·고유하다.
- [ ] Module Structure 계약이 구현됐다.
- [ ] Runtime Boundary 계약이 구현됐다.
- [ ] Editor/Test Assembly Boundary 계약이 구현됐다.
- [ ] 새 WorldGeneration 전용 asmdef를 만들지 않았다.
- [ ] 프로덕션 Runtime/Editor C#을 변경하지 않았다.
- [ ] CSV/Scene/Prefab/Package/ProjectSettings 변경이 0개다.
- [ ] Unity Asset Refresh가 PASS다.
- [ ] Compile Error가 0개다.
- [ ] 관련 신규 Warning이 0개다.
- [ ] 대상 EditMode test case가 모두 PASS다.
- [ ] PlayMode 테스트를 실행·생성하지 않았다.
- [ ] Result 문서가 요구 형식을 충족한다.
- [ ] MAP01을 시작하지 않았다.

## Completion Rule

TASK EXECUTION은 Result에 `STATUS: PASS / FAIL / BLOCKED`만 기록한다.

Result가 정확히 `STATUS: PASS`이고 모든 DONE CONDITIONS가 완료된 경우에만 STATUS FINALIZE가:

```text
MAP00_04_CREATE_TEST_STRUCTURE: CURRENT -> COMPLETE
Current Task: TASKS/MAP00_04_CREATE_TEST_STRUCTURE.md -> NONE
```

을 수행한다.

STATUS FINALIZE는 MAP01을 열거나 시작하지 않는다. 다음 단계는 새 패치를 기다린다.

