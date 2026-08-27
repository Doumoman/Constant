# MAP00_03 — Create Map Module Structure

```yaml
status_control:
  task_key: MAP00_03_CREATE_MAP_MODULE_STRUCTURE
  result_file: REPORTS/MAP00_03_CREATE_MAP_MODULE_STRUCTURE_RESULT.md
```

## TASK TYPE

```text
UNITY PROJECT STRUCTURE
```

## Objective

`MAP00_02_FOLDER_AND_ASMDEF_PLAN_RESULT.md`에서 승인된 36개 광역 월드 생성기 디렉터리를 실제 Unity 프로젝트에 만들고, 각 Assets 디렉터리의 Unity `.meta`가 정상 생성됐음을 검증한다.

이 TASK는 디렉터리 구조만 만든다. 구현 코드·데이터·에셋을 만들지 않는다.

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
11. `REPORTS/MAP00_02_FOLDER_AND_ASMDEF_PLAN_RESULT.md`

## READ ALLOWLIST

본문 읽기 허용:

- 위 Mandatory Read Order의 파일
- `Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef`
- `Assets/_Game/Editor/MapAuthoring/MapAuthoring.Editor.asmdef`
- `Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef`
- `Assets/_Game/Tests/PlayMode/Map/Game.Map.Tests.PlayMode.asmdef`
- `Assets/_Game/Editor/MapAuthoring/Tests/EditMode/MapAuthoring.Tests.EditMode.asmdef`

제한적 검색 허용:

- 아래 승인 디렉터리의 존재 여부와 직계 파일명
- 아래 승인 디렉터리 및 대응 `.meta`의 상태
- 프로젝트 전체 `.meta`에서 `guid:` 값만 추출하는 GUID 중복 검사
- 작업 전후 변경 파일 경로 확인

금지:

- 프로젝트 C# 내용 검색/열람
- Scene/Prefab YAML 열람
- CSV/GDD/과거 하네스 본문 열람
- 승인 경로 밖 파일의 내용 스캔

## WRITE ALLOWLIST

### Runtime directories — 7

```text
Assets/_Game/Map/Runtime/WorldGeneration/
Assets/_Game/Map/Runtime/WorldGeneration/Domain/
Assets/_Game/Map/Runtime/WorldGeneration/Data/
Assets/_Game/Map/Runtime/WorldGeneration/Generation/
Assets/_Game/Map/Runtime/WorldGeneration/Validation/
Assets/_Game/Map/Runtime/WorldGeneration/Random/
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/
```

### Editor directories — 5

```text
Assets/_Game/Editor/MapAuthoring/WorldGeneration/
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Import/
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Validation/
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Preview/
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Windows/
```

### Runtime test directories — 7

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Validation/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Determinism/
Assets/_Game/Tests/PlayMode/Map/WorldGeneration/
```

### Editor test directories — 4

```text
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Import/
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Validation/
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Preview/
```

### Authoring data directories — 13

```text
Assets/_Game/Map/Data/WorldGeneration/
Assets/_Game/Map/Data/WorldGeneration/Authoring/
Assets/_Game/Map/Data/WorldGeneration/Authoring/World/
Assets/_Game/Map/Data/WorldGeneration/Authoring/Route/
Assets/_Game/Map/Data/WorldGeneration/Authoring/Biome/
Assets/_Game/Map/Data/WorldGeneration/Authoring/SpecialMap/
Assets/_Game/Map/Data/WorldGeneration/Authoring/Village/
Assets/_Game/Map/Data/WorldGeneration/Authoring/MicroChunk/
Assets/_Game/Map/Data/WorldGeneration/Authoring/Boundary/
Assets/_Game/Map/Data/WorldGeneration/Authoring/Population/
Assets/_Game/Map/Data/WorldGeneration/Authoring/Items/
Assets/_Game/Map/Data/WorldGeneration/Imported/
Assets/_Game/Map/Data/WorldGeneration/GeneratedDebug/
```

위 36개 디렉터리와 각 디렉터리의 대응 Unity 폴더 `.meta`만 생성할 수 있다.

추가 생성 허용:

```text
MapDesign/MCP/REPORTS/MAP00_03_CREATE_MAP_MODULE_STRUCTURE_RESULT.md
```

TASK EXECUTION 중 `06_IMPLEMENTATION_STATUS.md`는 수정하지 않는다. 상태 변경은 Result PASS 이후 STATUS FINALIZE Phase만 수행한다.

## DO NOT

- `.gitkeep` 생성 금지
- placeholder README 생성 금지
- C# 또는 빈 클래스 생성 금지
- CSV 또는 CSV 스키마 생성 금지
- asmdef/asmref 생성·수정 금지
- ScriptableObject 생성 금지
- Scene/Prefab/Tile/Tile Palette/Animator/Addressables 변경 금지
- `Assets/_Game/Stage/**` 변경 금지
- `Assets/StarNight/**` 변경 금지
- `Packages/**` 또는 `ProjectSettings/**` 변경 금지
- 기존 파일·폴더 삭제/이동/이름 변경 금지
- 기존 `.meta` GUID 변경 금지
- 관련 없는 포맷팅·정리 금지
- Git commit/push/branch/reset/rebase/force 금지
- MAP00_04 선행 작업 금지

## Collision Handling

1. 승인 디렉터리가 이미 존재하면 `PREEXISTING`으로 기록하고 수정하지 않는다.
2. 대응 `.meta`가 이미 존재하면 GUID를 변경하지 않는다.
3. 승인 대상 경로에 예상하지 않은 C#/CSV/asmdef/에셋 파일이 이미 있으면 삭제하거나 덮어쓰지 않는다.
4. 예상하지 않은 파일 때문에 구조-only 완료를 보장할 수 없으면 `BLOCKED`로 Result를 작성한다.
5. 작업 시작 전에 존재한 사용자 변경을 되돌리지 않는다.

## Inputs

- `REPORTS/MAP00_02_FOLDER_AND_ASMDEF_PLAN_RESULT.md`
- 현재 Unity 프로젝트의 승인 경로 존재 상태
- Unity Editor `6000.3.8f1`

## Outputs

- 승인된 36개 디렉터리
- 새로 필요했던 각 디렉터리의 Unity 폴더 `.meta`
- `REPORTS/MAP00_03_CREATE_MAP_MODULE_STRUCTURE_RESULT.md`

## Implementation Steps

1. `06_IMPLEMENTATION_STATUS.md`에서 이 TASK가 CURRENT인지 확인한다.
2. 작업 전 변경 파일 경로를 기록한다. 기존 변경은 수정·복구하지 않는다.
3. 36개 승인 디렉터리의 존재 상태를 `PREEXISTING` 또는 `MISSING`으로 분류한다.
4. 승인 대상 하위에 예상하지 않은 파일이 있는지 파일명만 확인한다.
5. 누락된 디렉터리만 만든다. 가능하면 Unity Editor/AssetDatabase를 통해 만들고, 그렇지 않으면 디렉터리를 만든 뒤 Unity Asset Refresh로 `.meta`를 생성한다.
6. Unity Asset Refresh 완료를 기다린다.
7. 36개 디렉터리와 대응 `.meta`가 모두 존재하는지 확인한다.
8. 새 `.meta`의 `guid:` 형식과 GUID 중복 여부를 검사한다. 기존 `.meta`를 고치지 않는다.
9. 작업 후 변경 파일 경로를 확인해 승인된 `.meta`와 Result 외 변경이 없는지 검증한다.
10. Unity compile 상태를 확인한다.
11. Result 문서를 작성한다.
12. 모든 DONE CONDITIONS가 PASS인 경우에만 Result에 `STATUS: PASS`를 기록한다.

## Tests

### T1 — Directory Count and Presence

- 승인 목록: 정확히 36개.
- 36개 모두 존재해야 한다.

### T2 — Folder Meta Presence

- 각 승인 디렉터리에 대응하는 `<DirectoryName>.meta`가 존재해야 한다.
- 새 `.meta`는 Unity folder meta 형식이어야 한다.

### T3 — GUID Validation

- 새 `.meta`마다 유효한 `guid:` 값이 하나 있어야 한다.
- 신규 GUID끼리 중복이 없어야 한다.
- 프로젝트의 기존 `.meta` GUID와도 중복되지 않아야 한다.

### T4 — Forbidden Artifact Scan

승인된 신규 구조 안에 다음이 없어야 한다.

```text
.gitkeep
*.cs
*.csv
*.asmdef
*.asmref
*.asset
*.unity
*.prefab
README*
```

### T5 — Change Scope

이번 TASK가 만든 변경은 다음뿐이어야 한다.

- 승인된 폴더 `.meta`
- Result 문서

기존 사용자 변경은 별도로 기록하고 이번 TASK 변경으로 간주하지 않는다.

## Unity Verification

필수:

```text
Unity Version: 6000.3.8f1
Asset Refresh: PASS
Compile Errors: 0
Relevant New Warnings: 0
Scene/Prefab Changes: NONE
```

코드를 만들지 않으므로 EditMode/PlayMode 테스트는 실행하지 않아도 된다. 대신 T1~T5 구조 검증 결과를 Result에 기록한다.

Unity Editor 또는 Unity MCP에 접근할 수 없어 Asset Refresh와 Compile Error 0을 확인할 수 없다면 PASS로 종료하지 말고 `BLOCKED`로 기록한다.

## Result File

```text
REPORTS/MAP00_03_CREATE_MAP_MODULE_STRUCTURE_RESULT.md
```

Result에는 반드시 다음 섹션을 포함한다.

```text
TASK
STATUS
SUMMARY
READ
PREEXISTING DIRECTORIES
CREATED DIRECTORIES
CREATED META FILES
CHANGED
TEST
UNITY
OUT_OF_SCOPE_FINDINGS
DONE CONDITIONS
NEXT
Recommended Commit
```

Result의 `CREATED DIRECTORIES`와 `CREATED META FILES`에는 실제 경로를 전부 나열한다.

## DONE CONDITIONS

- [ ] Current Task가 MAP00_03임을 확인했다.
- [ ] 승인 디렉터리 목록이 정확히 36개임을 확인했다.
- [ ] 승인된 36개 디렉터리가 모두 존재한다.
- [ ] 각 승인 디렉터리의 Unity 폴더 `.meta`가 존재한다.
- [ ] 새 `.meta` GUID가 유효하고 중복이 없다.
- [ ] `.gitkeep`, README placeholder, C#, CSV, asmdef 등 금지 산출물이 0개다.
- [ ] 기존 C#/CSV/asmdef/Scene/Prefab/Package/ProjectSettings 변경이 0개다.
- [ ] Unity Asset Refresh가 PASS다.
- [ ] Unity Compile Error가 0개다.
- [ ] 관련 신규 Warning이 0개다.
- [ ] Result 문서가 요구 형식을 충족한다.
- [ ] MAP00_04를 시작하지 않았다.

## Completion Rule

TASK EXECUTION은 Result에 `STATUS: PASS / FAIL / BLOCKED`만 기록한다.

Result가 정확히 `STATUS: PASS`이고 모든 DONE CONDITIONS가 완료된 경우에만 STATUS FINALIZE Phase가:

```text
MAP00_03_CREATE_MAP_MODULE_STRUCTURE: CURRENT -> COMPLETE
Current Task: TASKS/MAP00_03_CREATE_MAP_MODULE_STRUCTURE.md -> NONE
```

을 수행한다.

STATUS FINALIZE는 MAP00_04를 CURRENT로 바꾸지 않는다. 다음 TASK는 새 패치를 기다린다.

