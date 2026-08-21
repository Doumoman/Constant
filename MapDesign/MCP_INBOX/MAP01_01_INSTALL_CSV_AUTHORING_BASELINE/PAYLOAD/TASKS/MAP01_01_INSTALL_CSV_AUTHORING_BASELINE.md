# MAP01_01 — Install CSV Authoring Baseline

```yaml
status_control:
  task_key: MAP01_01_INSTALL_CSV_AUTHORING_BASELINE
  result_file: REPORTS/MAP01_01_INSTALL_CSV_AUTHORING_BASELINE_RESULT.md
```

## TASK TYPE

```text
CSV AUTHORING DATA BASELINE
```

## Objective

MAP00 Exit 승인 뒤 다시 검증한 Map Package v1.0의 정적 Authoring CSV 49개와 `CSV_DATA_DICTIONARY.csv`를 MAP00에서 확정한 WorldGeneration Authoring 폴더에 바이트 그대로 설치한다.

이 TASK는 이후 CSV loader와 registry가 읽을 단 하나의 프로젝트 입력 기준선을 만드는 단계다. CSV loader, row DTO, definition, registry, import window, ScriptableObject cache 또는 Generated Output은 구현하지 않는다.

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
12. `REPORTS/MAP00_10_MAP00_EXIT_AUDIT_RESULT.md`
13. `INPUTS/MAP01_01_CSV_PACKAGE/AUTHORING_FILE_MAP.csv`
14. `INPUTS/MAP01_01_CSV_PACKAGE/SOURCE_VALIDATION_BASELINE.txt`

## READ ALLOWLIST

본문 또는 바이트 읽기 허용:

- Mandatory Read Order의 파일
- `INPUTS/MAP01_01_CSV_PACKAGE/03_CSV_SCHEMA/CSV_DATA_DICTIONARY.csv`
- `INPUTS/MAP01_01_CSV_PACKAGE/04_CSV_STARTER/*.csv`
- `INPUTS/MAP01_01_CSV_PACKAGE/05_GENERATED_OUTPUT_SCHEMA/*.csv`
- `INPUTS/MAP01_01_CSV_PACKAGE/07_TOOLS/validate_csv_package.py`
- 아래 WRITE ALLOWLIST에 해당하는 기존 destination CSV와 `.meta`
- 아래 Preflight Preservation Check의 정확한 asmdef·test 파일

제한적 검색 허용:

- 승인된 WorldGeneration 디렉터리 36개의 존재 여부와 직계 파일명
- `Assets/_Game/Map/Data/WorldGeneration/Authoring/**/*.csv`의 경로만 열거
- 프로젝트 전체 `.meta`에서 `guid:` 값만 추출하는 GUID 중복 검사
- 작업 전후 변경 파일 경로 확인
- Unity Console의 현재 compile error와 이 TASK로 발생한 warning 확인

기존 아키텍처 테스트 실행 시 테스트 코드 자체가 MAP00_04에서 승인된 범위를 검사하는 것은 허용한다.

금지:

- 승인되지 않은 프로젝트 C# 본문 스캔
- `Assets/_Legacy/**` 본문 열람 또는 수정
- Scene/Prefab YAML 열람
- Map Package의 다른 GDD/로드맵/CSV 스키마 문서 임의 열람
- 입력 CSV의 자동 수정 또는 정규화 저장

## Patch Input Contract

입력 루트:

```text
MapDesign/MCP/INPUTS/MAP01_01_CSV_PACKAGE/
```

정확한 구성:

| 항목 | 개수 | 용도 |
|---|---:|---|
| `03_CSV_SCHEMA/CSV_DATA_DICTIONARY.csv` | 1 | 60개 CSV의 컬럼 사전 |
| `04_CSV_STARTER/*.csv` | 49 | 프로젝트에 설치할 정적 Authoring 원본 |
| `05_GENERATED_OUTPUT_SCHEMA/*.csv` | 11 | 원본 패키지 validator 입력 전용; Assets에 설치 금지 |
| `07_TOOLS/validate_csv_package.py` | 1 | 정본 패키지 검증기 |
| `AUTHORING_FILE_MAP.csv` | 1 | 49개 정적 파일의 정확한 destination mapping |
| `SOURCE_VALIDATION_BASELINE.txt` | 1 | 기대 결과 `ERROR 0`, `WARNING 10` |

총 파일 수는 정확히 64개다.

재발행 source identity:

```text
relative-manifest SHA-256 = 2b0d40ea2d67173168b452b722bc6af91268c28636abda6ae3a6a63457e7109e
validator exit = 0
ERROR = 0
WARNING = 10
validator output/baseline diff = 0
dictionary unique file_name = 60
file map rows = 49
category counts = 6/9/2/5/7/7/3/6/4
static 49 + dictionary 1 missing UTF-8 BOM = 0
```

입력은 read-only다. TASK 중 입력 파일을 변경하거나 삭제하지 않는다.

## Master Backlog and MAP00 Exit Gate

`MASTER_IMPLEMENTATION_TASK_LIST.md`, `06_IMPLEMENTATION_STATUS.md`, MAP00_10 Result에서 다음 exact state를 확인한다.

```text
Master task count = 205
MAP00_01~10 = COMPLETE
MAP00 EXIT: APPROVED
MAP01_01_INSTALL_CSV_AUTHORING_BASELINE = CURRENT
MAP01_02 이후 = LOCKED / NOT STARTED
Authoring CSV before install = 0
```

MAP00_10 Result는 다음을 포함해야 한다.

```text
STATUS: PASS
MAP00 EXIT: APPROVED
targeted EditMode = 53/53 PASS
compile errors = 0
relevant new warnings = 0
Authoring CSV = 0
MAP01 = NOT STARTED
```

하나라도 다르면 상태를 임의로 보정하거나 구버전 MAP01_01 package를 실행하지 말고 `BLOCKED` Result를 작성한다.

## Preflight Preservation Check

MAP00 Exit 승인 기준을 보존한다. 기존 unrelated dirty worktree나 Legacy 이동을 되돌리거나 조사 범위를 넓히지 말고 다음 보존 계약만 확인한다.

필수 디렉터리:

```text
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

하나라도 없으면 경로를 임의로 다시 만들거나 Legacy에서 복원하지 말고 `BLOCKED` Result를 작성한다.

추가 확인:

```text
locked WorldGeneration directories = 36/36
folder metas = 36/36
new WorldGeneration asmdef/asmref = 0
Runtime production C# = 6/6
Editor production C# = 2/2
MAP00 test C# = 8/8
Assets Authoring CSV before install = 0
```

## WRITE ALLOWLIST

정적 CSV 원본 49개는 `AUTHORING_FILE_MAP.csv`의 각 행에 따라 정확히 다음 기준 루트 아래에 설치한다.

```text
Assets/_Game/Map/Data/WorldGeneration/Authoring/<destination_relative>
```

데이터 사전 1개는 다음 정확한 경로에 설치한다.

```text
Assets/_Game/Map/Data/WorldGeneration/Authoring/CSV_DATA_DICTIONARY.csv
```

Unity가 생성하는 위 50개 `.csv.meta`만 추가 허용한다.

```text
<각 destination CSV 경로>.meta
```

추가 생성 허용:

```text
MapDesign/MCP/REPORTS/MAP01_01_INSTALL_CSV_AUTHORING_BASELINE_RESULT.md
```

TASK EXECUTION 중 `06_IMPLEMENTATION_STATUS.md`는 수정하지 않는다. 상태 변경은 Result PASS 이후 STATUS FINALIZE만 수행한다.

## Exact Category Contract

`AUTHORING_FILE_MAP.csv`는 정확히 49개의 unique `source_file`과 49개의 unique `destination_relative`을 가져야 한다.

허용되는 첫 경로 segment와 파일 수:

| Category | CSV 수 |
|---|---:|
| `World` | 6 |
| `Route` | 9 |
| `Biome` | 2 |
| `SpecialMap` | 5 |
| `Village` | 7 |
| `MicroChunk` | 7 |
| `Boundary` | 3 |
| `Population` | 6 |
| `Items` | 4 |
| 합계 | 49 |

`source_file` 집합은 `04_CSV_STARTER/*.csv` 파일명 집합과 정확히 같아야 한다. 절대 경로, `..`, 중복 destination, 매핑되지 않은 source를 허용하지 않는다.

## Collision Handling

1. destination CSV가 없으면 source bytes를 그대로 복사한다.
2. destination CSV가 이미 있고 source와 SHA-256이 같으면 `PREEXISTING_IDENTICAL`로 기록하고 덮어쓰지 않는다.
3. destination CSV가 이미 있고 source와 한 바이트라도 다르면 덮어쓰거나 병합하지 말고 `BLOCKED`다.
4. `Authoring/**/*.csv`에 허용된 50개 이외의 CSV가 있으면 삭제·이동하지 말고 `BLOCKED`다.
5. 기존 `.meta`가 있으면 GUID를 보존한다.
6. destination CSV가 없지만 대응 `.meta`만 있으면 GUID가 유효하고 project-unique한지 먼저 검사한다. 유효하면 보존해 사용할 수 있고, 아니면 `BLOCKED`다.
7. 기존 사용자 변경을 되돌리거나 정리하지 않는다.

## Byte Preservation Rules

- 복사는 텍스트 재저장이 아니라 binary byte copy로 수행한다.
- 줄바꿈, 인용부호, 열 순서, 행 순서, 공백을 바꾸지 않는다.
- 49개 starter CSV와 데이터 사전은 UTF-8 BOM `EF BB BF`를 유지해야 한다.
- destination SHA-256은 대응 source SHA-256과 정확히 같아야 한다.
- CSV 내용에 문제가 있어 보이더라도 고치지 않고 Result에 보고한다.

## DO NOT

- CSV loader, parser, row DTO, definition, registry, import report, import window 구현 금지
- 프로덕션 Runtime/Editor C# 생성·수정 금지
- 기존 또는 신규 test C# 생성·수정 금지
- asmdef/asmref 생성·수정 금지
- ScriptableObject 및 `.asset` 생성 금지
- Generated schema CSV 11개를 `Assets/**`에 복사 금지
- `Imported/` 또는 `GeneratedDebug/`에 파일 생성 금지
- CSV 헤더·행·셀·스키마 수정 금지
- `Assets/_Legacy/**` 수정 금지
- `Assets/_Game/Stage/**`, `Assets/StarNight/**` 변경 금지
- Scene, Prefab, Tile, Tile Palette, Animator, Addressables 변경 금지
- `Packages/**`, `ProjectSettings/**` 변경 금지
- 새 package/dependency 설치 금지
- 기존 파일·폴더 삭제/이동/이름 변경 금지
- 관련 없는 포맷팅·warning 수정 금지
- Git commit/push/branch/reset/rebase/force 금지
- MAP01_02 또는 MAP02 선행 작업 금지

## Inputs

- `INPUTS/MAP01_01_CSV_PACKAGE/`
- MAP00_10 PASS / `MAP00 EXIT: APPROVED` Result
- 보존된 WorldGeneration Authoring 구조
- Unity Editor `6000.3.8f1`

## Outputs

- 정본 Authoring CSV 49개
- `CSV_DATA_DICTIONARY.csv` 1개
- 대응 Unity `.csv.meta` 50개
- `REPORTS/MAP01_01_INSTALL_CSV_AUTHORING_BASELINE_RESULT.md`

## Implementation Steps

1. `06_IMPLEMENTATION_STATUS.md`에서 이 TASK가 CURRENT인지 확인한다.
2. Master 205개, MAP00_01~10 COMPLETE, MAP00_10 `STATUS: PASS`와 `MAP00 EXIT: APPROVED`, MAP01 미시작을 확인한다.
3. 작업 전 변경 파일 경로를 기록하고 기존 무관 변경은 수정·복구하지 않는다.
4. Preflight Preservation Check의 locked 디렉터리 36개와 meta, asmdef 5개, production C# 8개, MAP00 test C# 8개, Authoring CSV 0을 확인한다.
5. 입력 트리의 총 파일 수, 유형별 개수, relative-manifest SHA-256이 Patch Input Contract와 일치하는지 확인한다. 각 파일 SHA-256과 입력 root 기준 relative path를 path 오름차순으로 한 줄씩 기록한 manifest bytes를 SHA-256한다.
6. 제공된 validator를 입력 루트에 실행한다. 출력과 exit code를 기록한다.
7. validator 결과가 `ERROR 0`, `WARNING 10`, exit code 0이고 `SOURCE_VALIDATION_BASELINE.txt`와 동일한지 확인한다. 출력 비교에 한해 OS의 LF/CRLF 차이는 정규화하며 메시지·순서·개수는 정확히 같아야 한다.
8. 데이터 사전의 unique `file_name` 수가 60개이며 starter 49개와 generated schema 11개를 정확히 포괄하는지 확인한다.
9. `AUTHORING_FILE_MAP.csv`의 exact set, uniqueness, category counts와 안전한 상대 경로를 검증한다.
10. 기존 `Authoring/**/*.csv` 경로와 대응 `.meta` 상태를 확인해 Collision Handling을 적용한다.
11. 허용된 missing destination에만 source bytes를 복사한다. 데이터 사전은 Authoring root에 복사한다.
12. Unity Asset Refresh가 완료될 때까지 기다린다.
13. destination CSV가 정확히 50개인지 확인한다.
14. 50개 모두 source/destination SHA-256 동일성과 UTF-8 BOM을 확인한다.
15. 대응 `.csv.meta` 50개가 존재하고 GUID가 유효하며 project-unique한지 확인한다.
16. Unity compile 상태를 확인한다.
17. MAP00_04의 architecture fixture 3개만 다시 실행하고 actual case 수와 결과를 기록한다.
18. 작업 후 변경 파일 경로가 허용된 CSV, `.meta`, Result뿐인지 확인한다.
19. Result 문서를 작성한다.
20. 모든 DONE CONDITIONS가 PASS인 경우에만 Result에 `STATUS: PASS`를 기록한다.

## Tests

### T1 — Canonical Source Package Validation

```text
Validator Exit Code = 0
ERROR 0
WARNING 10
Output lines = SOURCE_VALIDATION_BASELINE.txt (LF/CRLF 차이만 무시)
```

WARNING 10은 정본 starter package의 알려진 콘텐츠 제작량 경고다. 이 TASK에서 수정하지 않는다.

### T2 — Dictionary and Mapping Coverage

```text
CSV_DATA_DICTIONARY unique file_name = 60
04_CSV_STARTER CSV = 49
05_GENERATED_OUTPUT_SCHEMA CSV = 11
AUTHORING_FILE_MAP rows = 49
Unmapped starter CSV = 0
Duplicate source/destination = 0
Invalid relative path = 0
Category counts = 6/9/2/5/7/7/3/6/4
```

### T3 — Installed Authoring Data

```text
Assets Authoring CSV total = 50
Installed static CSV = 49
Installed data dictionary = 1
Installed generated schema/output CSV = 0
Unexpected CSV = 0
SHA-256 mismatch = 0
Missing UTF-8 BOM = 0
```

### T4 — Asset Meta Validation

- `.csv.meta` 50개 존재
- GUID 형식 유효
- 대상 GUID끼리 중복 0
- 프로젝트 전체 GUID와 중복 0
- preexisting meta GUID 변경 0

### T5 — Compile and Architecture Regression

```text
Compile Errors = 0
Relevant New Warnings = 0
WorldGenerationModuleStructureTests = PASS
WorldGenerationRuntimeBoundaryTests = PASS
WorldGenerationEditorBoundaryTests = PASS
Actual cases = 10
Failed = 0
Skipped = 0
```

### T6 — Change Scope

이번 TASK의 Asset 변경은 허용된 destination CSV와 그 `.meta`뿐이다. C#, asmdef, ScriptableObject, Scene, Prefab, Package, ProjectSettings 변경은 0개다.

기존 무관 변경과 PATCH APPLY가 만든 `MapDesign/MCP/INPUTS/**`는 별도로 기록하며 TASK Asset 변경으로 섞지 않는다.

## Unity Verification

필수:

```text
Unity Version: 6000.3.8f1
Asset Refresh: PASS
Compile Errors: 0
Relevant New Warnings: 0
Targeted EditMode Architecture Tests: PASS (10/10)
PlayMode Tests: NOT RUN
Scene/Prefab Changes: NONE
```

Unity Editor 또는 Unity MCP에 접근할 수 없어 Asset Refresh, compilation, 대상 EditMode 결과를 확인할 수 없으면 PASS로 종료하지 말고 `BLOCKED`로 기록한다.

## Result File

```text
REPORTS/MAP01_01_INSTALL_CSV_AUTHORING_BASELINE_RESULT.md
```

Result에는 반드시 다음 섹션을 포함한다.

```text
TASK
STATUS
SUMMARY
READ
MASTER BACKLOG CHECK
MAP00 EXIT CHECK
PREFLIGHT PRESERVATION CHECK
INPUT PACKAGE IDENTITY
SOURCE PACKAGE VALIDATION
DICTIONARY AND FILE MAP VALIDATION
PREEXISTING IDENTICAL CSV
CREATED CSV
CREATED META FILES
HASH AND ENCODING VALIDATION
CHANGED
TEST
UNITY
ASSET META VALIDATION
CHANGE SCOPE
OUT_OF_SCOPE_FINDINGS
DONE CONDITIONS
NEXT
Recommended Commit
```

`PREEXISTING IDENTICAL CSV`, `CREATED CSV`, `CREATED META FILES`에는 실제 경로를 전부 나열한다. 0개인 섹션도 `NONE`으로 남긴다.

## DONE CONDITIONS

- [ ] Current Task가 MAP01_01임을 확인했다.
- [ ] Master task count 205와 MAP00_01~10 COMPLETE를 확인했다.
- [ ] MAP00_10 Result의 `STATUS: PASS`, `MAP00 EXIT: APPROVED`, 53/53 test, compile error 0을 확인했다.
- [ ] MAP01_02 이후가 LOCKED이고 MAP01이 아직 시작되지 않았음을 확인했다.
- [ ] 보존 대상 WorldGeneration 디렉터리 36개와 folder meta, asmdef 5개, production C# 8개, MAP00 test C# 8개가 존재한다.
- [ ] 설치 전 Authoring CSV가 0개다.
- [ ] 입력 트리 파일 수와 유형별 개수가 exact contract와 일치한다.
- [ ] 입력 root 기준 relative-manifest SHA-256이 `2b0d40ea2d67173168b452b722bc6af91268c28636abda6ae3a6a63457e7109e`다.
- [ ] 원본 패키지 validator가 exit code 0, ERROR 0, WARNING 10이다.
- [ ] validator 출력이 제공된 baseline과 일치한다.
- [ ] 데이터 사전의 unique file set이 static 49 + generated 11과 정확히 일치한다.
- [ ] file map 49행과 category count가 정확하며 중복·누락·invalid path가 없다.
- [ ] 정본 static CSV 49개가 exact destination에 존재한다.
- [ ] `CSV_DATA_DICTIONARY.csv`가 exact destination에 존재한다.
- [ ] Authoring 아래 CSV 총수가 정확히 50개이고 unexpected/generated CSV가 없다.
- [ ] destination 50개 모두 source와 SHA-256이 같고 UTF-8 BOM을 유지한다.
- [ ] `.csv.meta` 50개가 존재하며 GUID가 유효하고 project-unique하다.
- [ ] 기존 differing CSV를 덮어쓰지 않았다.
- [ ] CSV 내용을 자동 수정하거나 정규화 저장하지 않았다.
- [ ] 프로덕션/Editor/Test C#, asmdef, ScriptableObject, Scene, Prefab, Package, ProjectSettings 변경이 0개다.
- [ ] Unity Asset Refresh가 PASS다.
- [ ] Unity Compile Error가 0개다.
- [ ] 관련 신규 Warning이 0개다.
- [ ] 대상 architecture test actual cases 10개가 모두 PASS다.
- [ ] PlayMode 테스트를 실행하지 않았다.
- [ ] Result 문서가 요구 형식을 충족한다.
- [ ] MAP01_02 또는 MAP02를 시작하지 않았다.

## Completion Rule

TASK EXECUTION은 Result에 `STATUS: PASS / FAIL / BLOCKED`만 기록한다.

Result가 정확히 `STATUS: PASS`이고 모든 DONE CONDITIONS가 완료된 경우에만 STATUS FINALIZE Phase가:

```text
MAP01_01_INSTALL_CSV_AUTHORING_BASELINE: CURRENT -> COMPLETE
Current Task: TASKS/MAP01_01_INSTALL_CSV_AUTHORING_BASELINE.md -> NONE
```

을 수행한다.

STATUS FINALIZE는 `MAP01_02_IMPLEMENT_CSV_SCHEMA_CATALOG`를 CURRENT로 바꾸지 않는다. 다음 TASK는 새 패치를 기다린다.

## Expected Next Task

```text
MAP01_02_IMPLEMENT_CSV_SCHEMA_CATALOG
```

다음 TASK는 별도 패치로만 연다.
