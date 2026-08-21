# MAP01_02 — Implement CSV Schema Catalog

```yaml
status_control:
  task_key: MAP01_02_IMPLEMENT_CSV_SCHEMA_CATALOG
  result_file: REPORTS/MAP01_02_IMPLEMENT_CSV_SCHEMA_CATALOG_RESULT.md
```

## TASK TYPE

```text
RUNTIME DATA CONTRACT + EDITOR IMPORT FOUNDATION + EDITMODE TESTS
```

## Objective

MAP01_01에서 설치한 `CSV_DATA_DICTIONARY.csv`의 정확한 10개 열을 읽어 파일·열·타입·필수·복합 PK·default·allowed values·FK 구조를 immutable `CsvSchemaCatalog`로 만든다.

이 TASK는 스키마 사전만 읽는다. 일반 Authoring CSV의 RFC4180 파싱, 실제 데이터 행 검증, 기본키 인덱스, scalar/list 값 파싱, 외래키 해석, StaticDataRegistry publish는 구현하지 않는다.

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
12. `REPORTS/MAP01_01_INSTALL_CSV_AUTHORING_BASELINE_RESULT.md`
13. `Assets/_Game/Map/Data/WorldGeneration/Authoring/CSV_DATA_DICTIONARY.csv`

## READ ALLOWLIST

본문 또는 바이트 읽기 허용:

- Mandatory Read Order의 파일
- `Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef`
- `Assets/_Game/Editor/MapAuthoring/MapAuthoring.Editor.asmdef`
- `Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef`
- `Assets/_Game/Editor/MapAuthoring/Tests/EditMode/MapAuthoring.Tests.EditMode.asmdef`
- `Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/WorldGenerationModuleStructureTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/WorldGenerationRuntimeBoundaryTests.cs`
- `Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/WorldGenerationEditorBoundaryTests.cs`
- 이 TASK의 WRITE ALLOWLIST에 해당하는 기존 파일과 `.meta`

제한적 검색 허용:

- `Assets/_Game/Map/Runtime/WorldGeneration/Data/`와 대응 Runtime test 디렉터리의 직계 파일명
- `Assets/_Game/Editor/MapAuthoring/WorldGeneration/Import/`와 대응 Editor test 디렉터리의 직계 파일명
- `Assets/_Game/Map/Data/WorldGeneration/Authoring/**/*.csv`의 경로·파일 수·SHA-256·BOM만 확인
- 프로젝트 전체 `.meta`에서 `guid:` 값만 추출하는 GUID 중복 검사
- 작업 전후 변경 파일 경로 확인
- Unity Console의 현재 compile error와 이 TASK로 발생한 warning 확인

금지:

- `CSV_DATA_DICTIONARY.csv` 이외 Authoring CSV의 셀 본문 읽기
- 승인되지 않은 프로젝트 C# 본문 스캔
- Scene/Prefab YAML 및 `Assets/_Legacy/**` 본문 읽기
- MAP01_03 이후 TASK 본문 읽기
- CSV 자동 수정·정규화 저장

## Installed Dictionary Baseline

정확한 경로:

```text
Assets/_Game/Map/Data/WorldGeneration/Authoring/CSV_DATA_DICTIONARY.csv
```

정확한 header와 순서:

```text
file_name,column_order,column_name,data_type,required,primary_key_order,default_value,allowed_values,foreign_key,description
```

MAP01_01 PASS 기준:

```text
UTF-8 BOM = PRESENT
line ending = CRLF
quoted field = 0
header + data lines = 680
data rows = 679
unique file_name = 60
fields per row = 10
```

baseline `data_type` token과 수량:

| Token | Count |
|---|---:|
| `STRING` | 75 |
| `ID` | 174 |
| `INT` | 210 |
| `ULONG` | 10 |
| `FLOAT` | 18 |
| `BOOL` | 83 |
| `ENUM` | 61 |
| `ID_LIST` | 30 |
| `ENUM_LIST` | 7 |
| `INT_LIST` | 5 |
| `HEX` | 4 |
| `DATETIME` | 2 |
| 합계 | 679 |

추가 baseline:

```text
required=1: 557
required=0: 122
PK column rows: 103
foreign_key non-empty: 84
default_value non-empty: 33
```

위 수량은 production builder에 하드코딩하지 않는다. canonical installed dictionary를 읽는 통합 테스트에서만 baseline 회귀값으로 검증한다.

## WRITE ALLOWLIST

### Runtime production C# — 8

```text
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvSchemaDataType.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvSchemaDictionaryRow.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvForeignKeyReference.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvSchemaCatalogError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvColumnSchema.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvFileSchema.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvSchemaCatalog.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvSchemaCatalogBuilder.cs
```

### Editor production C# — 1

```text
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Import/CsvSchemaDictionaryImporter.cs
```

### EditMode test C# — 2

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/CsvSchemaCatalogTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Import/CsvSchemaDictionaryImporterTests.cs
```

위 11개 C#과 Unity가 생성하는 대응 `.cs.meta` 11개만 Asset 변경으로 허용한다.

추가 생성 허용:

```text
MapDesign/MCP/REPORTS/MAP01_02_IMPLEMENT_CSV_SCHEMA_CATALOG_RESULT.md
```

TASK EXECUTION 중 `06_IMPLEMENTATION_STATUS.md`는 수정하지 않는다. 상태 변경은 Result PASS 이후 STATUS FINALIZE만 수행한다.

## Namespace Contract

```text
Runtime production: StarNight.Map.WorldGeneration.Data
Editor production : StarNight.MapAuthoring.WorldGeneration.Import
Runtime tests      : StarNight.Map.Tests.WorldGeneration.Data
Editor tests       : StarNight.MapAuthoring.Tests.WorldGeneration.Import
```

Runtime production은 `UnityEditor`를 참조하지 않는다. 새 asmdef/asmref를 만들지 않고 기존 `Game.Map.Runtime`, `MapAuthoring.Editor`, Runtime/EditMode 및 Editor/EditMode test assembly를 그대로 사용한다.

## Required Runtime Contracts

### 1. CsvSchemaDataType

exact token mapping:

```text
STRING    -> String
ID        -> Id
INT       -> Int
ULONG     -> ULong
FLOAT     -> Float
BOOL      -> Bool
ENUM      -> Enum
ID_LIST   -> IdList
ENUM_LIST -> EnumList
INT_LIST  -> IntList
HEX       -> Hex
DATETIME  -> DateTime
```

mapping은 case-sensitive ordinal이다. unknown token을 silent default로 바꾸지 않는다.

### 2. CsvSchemaDictionaryRow

dictionary의 10개 raw field와 1-based source row number를 보존한다. typed scalar/default 변환은 하지 않는다.

### 3. CsvForeignKeyReference

빈 `foreign_key`는 참조 없음이다. 값이 있으면 마지막 `.`을 기준으로 정확히 다음 구조로 분리한다.

```text
<target_file_name>.csv.<target_column_name>
```

target file은 `.csv`로 끝나야 하고 target column은 비어 있으면 안 된다. 이 TASK에서는 대상 파일/열 존재 여부를 resolve하지 않는다. 실제 FK resolution은 MAP01_11이다.

### 4. CsvColumnSchema

다음을 immutable/read-only로 노출한다.

```text
FileName
ColumnOrder
ColumnName
DataType
IsRequired
PrimaryKeyOrder (nullable)
DefaultValue (raw string)
AllowedValues (ordered read-only list)
ForeignKey (nullable structural reference)
Description
SourceRowNumber
```

`allowed_values`가 비어 있으면 empty list다. 값이 있으면 `|`로 분리하고 각 항목 trim 후 빈 항목과 ordinal duplicate를 거부한다.

### 5. CsvFileSchema

- `Columns`는 `column_order` 오름차순이다.
- `PrimaryKeyColumns`는 `primary_key_order` 오름차순이다.
- column lookup은 `StringComparer.Ordinal`을 사용한다.
- 외부에서 collection을 수정할 수 없어야 한다.

### 6. CsvSchemaCatalog

- file lookup은 `StringComparer.Ordinal`이다.
- deterministic enumeration은 `file_name` ordinal ascending이다.
- `Files`, `FileCount`, `ColumnCount`, `TryGetFile`, missing file을 명시적으로 실패시키는 lookup을 제공한다.
- 외부에서 catalog·file·column collection을 수정할 수 없어야 한다.

### 7. CsvSchemaCatalogBuilder

입력 row 순서에 의존하지 않고 다음을 전부 검사한 뒤 catalog 또는 deterministic error list를 반환한다.

1. `file_name`, `column_name`, `data_type` 비어 있음 금지
2. `column_order` positive integer
3. `(file_name, column_name)` ordinal duplicate 금지
4. `(file_name, column_order)` duplicate 금지
5. 파일별 `column_order`가 1부터 끊김 없이 연속
6. `required`는 exact `0` 또는 `1`
7. `primary_key_order`는 빈 문자열 또는 positive integer
8. PK column은 required여야 함
9. 파일마다 PK가 1개 이상 존재
10. 파일별 PK order가 1부터 끊김 없이 연속
11. 12개 data type token만 허용
12. allowed values의 empty/duplicate 항목 금지
13. foreign key structural syntax 검사

오류는 source row와 file/column context를 포함하고 stable sort되어야 한다. 오류가 하나라도 있으면 partial catalog를 publish하지 않는다.

## Dictionary Importer Boundary

`CsvSchemaDictionaryImporter`는 MAP01_02의 bootstrap 전용이다.

- project root를 안전하게 계산하고 위 exact dictionary 경로만 읽는다.
- strict UTF-8로 decode하고 UTF-8 BOM 존재를 확인한다.
- exact 10-column header를 검증한다.
- 현재 dictionary baseline은 quoted field 0, 모든 행 comma 9개이므로 이 restricted dialect만 읽는다.
- `"` 문자가 하나라도 있거나 한 행이 10개 field가 아니면 임의 해석하지 않고 MAP01_03 RFC4180 reader가 필요하다는 명시적 오류를 반환한다.
- CRLF/LF를 모두 line boundary로 읽을 수 있지만 원본을 저장하거나 줄바꿈을 바꾸지 않는다.
- importer는 raw `CsvSchemaDictionaryRow`만 만들고 typed catalog 규칙은 Runtime builder에 위임한다.

MAP01_03에서는 이 bootstrap tokenizer를 일반 CSV에 복사하지 않고 RFC4180 reader로 교체·연결한다.

## DO NOT

- `CSV_DATA_DICTIONARY.csv` 및 다른 CSV 수정·재저장 금지
- 일반 Authoring CSV 49개의 데이터 행 import 금지
- generic RFC4180 reader 구현 금지
- quoted field, escaped quote, multiline field를 임의 지원하는 별도 parser 구현 금지
- 실제 데이터 header/required/default 검증 금지
- 실제 CSV 기본키 수집·중복 검사 금지
- int/ulong/float/bool/hex/enum/list typed value parser 구현 금지
- 외래키 대상 존재 해석 금지
- domain definition, `StaticDataRegistry`, `ContentVersionHash`, import report/window 구현 금지
- ScriptableObject 또는 `.asset` 생성 금지
- 기존 C# 및 test 수정 금지
- asmdef/asmref 수정·생성 금지
- Scene, Prefab, Tile, Tile Palette, Animator, Addressables 변경 금지
- `Assets/_Legacy/**`, `Assets/_Game/Stage/**`, `Assets/StarNight/**` 변경 금지
- `Packages/**`, `ProjectSettings/**` 변경 금지
- 새 package/dependency 설치 금지
- 기존 파일·폴더 삭제/이동/이름 변경 금지
- 관련 없는 포맷팅·warning 수정 금지
- Git commit/push/branch/reset/rebase/force 금지
- MAP01_03 선행 작업 금지

## Collision Handling

1. WRITE ALLOWLIST 파일이 없으면 생성한다.
2. 같은 경로 파일이 이미 있고 이 TASK의 exact 계약과 바이트가 같으면 `PREEXISTING_IDENTICAL`로 기록하고 덮어쓰지 않는다.
3. 같은 경로 파일이 이미 있고 한 바이트라도 다르면 덮어쓰기·병합하지 말고 `BLOCKED`다.
4. 기존 `.meta`가 있으면 GUID를 보존한다.
5. 허용 경로에 예상하지 않은 production/test C#이 있으면 삭제·이동하지 말고 Result에 기록한다. 정확한 완료 범위를 보장할 수 없으면 `BLOCKED`다.
6. 기존 사용자 변경을 되돌리거나 정리하지 않는다.

## Inputs

- `REPORTS/MAP01_01_INSTALL_CSV_AUTHORING_BASELINE_RESULT.md`
- 설치된 `CSV_DATA_DICTIONARY.csv`
- 기존 `Game.Map.Runtime` / `MapAuthoring.Editor` assembly 경계
- Unity Editor `6000.3.8f1`

## Outputs

- immutable CSV schema value types와 catalog/builder
- dictionary bootstrap importer
- Runtime/EditMode와 Editor/EditMode schema tests
- `REPORTS/MAP01_02_IMPLEMENT_CSV_SCHEMA_CATALOG_RESULT.md`

## Implementation Steps

1. Current Task가 MAP01_02이고 Master 205개 중 MAP01_01까지 COMPLETE인지 확인한다.
2. MAP01_01 Result의 `STATUS: PASS`, Authoring CSV `50`, meta `50`, hash/BOM `50/50`, compile error `0`, architecture tests `10/10`을 확인한다.
3. 작업 전 변경 파일 경로와 production/test inventory를 기록하고 기존 무관 변경은 건드리지 않는다.
4. 설치된 dictionary의 SHA-256, BOM, header, line/row/file/type/PK/FK/default baseline을 읽기 전용으로 확인한다.
5. 허용된 기존 C#/asmdef/test만 읽어 namespace·style·assembly convention을 확인한다.
6. Runtime schema value types, immutable models, error type, builder를 구현한다.
7. Editor bootstrap dictionary importer를 restricted boundary 그대로 구현한다.
8. Runtime pure unit tests와 canonical dictionary importer tests를 구현한다.
9. Unity Asset Refresh와 compilation을 완료한다.
10. 새 Runtime test fixture, 새 Editor test fixture, 기존 architecture fixture 3개만 실행한다.
11. 신규 `.cs.meta` 11개의 GUID 형식과 프로젝트 중복을 확인한다.
12. dictionary 및 기존 50 CSV/meta가 작업 전과 바이트/GUID 동일한지 확인한다.
13. 작업 후 변경 파일이 허용된 C# 11개, meta 11개, Result뿐인지 확인한다.
14. Result를 작성하고 모든 DONE CONDITIONS가 PASS일 때만 `STATUS: PASS`를 기록한다.

## Required Tests

### Runtime fixture — CsvSchemaCatalogTests

최소 9개 case:

1. exact 12 data type token mapping
2. file/column ordinal lookup과 deterministic order
3. required/default/allowed-values 보존
4. composite PK ordering
5. FK structural parsing
6. duplicate name/order 거부
7. non-contiguous column/PK order 거부
8. invalid required/type/PK/FK/allowed-values 오류 context
9. input row shuffle 후 동일 catalog 결과

### Editor fixture — CsvSchemaDictionaryImporterTests

최소 6개 case:

1. canonical dictionary `60 files / 679 columns`
2. 12개 type count가 baseline과 정확히 일치
3. required `557/122`, PK `103`, FK `84`, default `33`
4. exact header와 UTF-8 BOM 검증
5. quoted field와 10-field 위반을 명시적으로 거부
6. importer가 source file을 수정하지 않고 before/after SHA-256 동일

### Regression fixtures

기존 architecture fixture 3개를 다시 실행하고 기존 actual case `10/10` 이상이 모두 PASS해야 한다.

새 fixture case는 최소 `15`, architecture regression은 기존 `10`; targeted total actual case는 최소 `25`다. 실제 discovery 수와 PASS/FAIL을 Result에 기록한다.

## Unity Verification

필수:

```text
Unity Version: 6000.3.8f1
Asset Refresh: PASS
Compile Errors: 0
Relevant New Warnings: 0
New Schema EditMode Cases: >=15 / ALL PASS
Architecture Regression Cases: 10/10 PASS
Targeted Total Cases: >=25 / ALL PASS
PlayMode Tests: NOT RUN
Scene/Prefab Changes: NONE
```

Unity Editor 또는 Unity MCP에 접근할 수 없어 compile과 대상 EditMode 결과를 확인할 수 없다면 PASS로 종료하지 말고 `BLOCKED`로 기록한다.

## Result File

```text
REPORTS/MAP01_02_IMPLEMENT_CSV_SCHEMA_CATALOG_RESULT.md
```

Result 필수 섹션:

```text
TASK
STATUS
SUMMARY
READ
MASTER BACKLOG CHECK
MAP01_01 GATE CHECK
DICTIONARY BASELINE
CREATED
PREEXISTING_IDENTICAL
SCHEMA CONTRACTS IMPLEMENTED
TEST
UNITY
ASSET META VALIDATION
CHANGE SCOPE
OUT_OF_SCOPE_FINDINGS
DONE CONDITIONS
NEXT
Recommended Commit
```

## DONE CONDITIONS

- [ ] Current Task가 MAP01_02임을 확인했다.
- [ ] Master 205개와 MAP01_01 COMPLETE/PASS를 확인했다.
- [ ] 설치된 dictionary가 BOM/header/679 rows/60 files baseline과 일치한다.
- [ ] 12개 exact data type token mapping을 구현했다.
- [ ] file/column schema와 composite PK가 immutable/read-only다.
- [ ] required/default/allowed-values/FK structural contract를 보존한다.
- [ ] duplicate/order/token/PK/FK 오류가 deterministic context와 함께 보고된다.
- [ ] 오류가 있으면 partial catalog를 publish하지 않는다.
- [ ] canonical dictionary가 `60 files / 679 columns` catalog로 import된다.
- [ ] Runtime production C# 8개, Editor production C# 1개, test C# 2개만 생성했다.
- [ ] 대응 `.cs.meta` 11개가 존재하고 GUID가 유효·고유하다.
- [ ] 새 asmdef/asmref를 만들거나 수정하지 않았다.
- [ ] 기존 CSV 50개와 meta 50개를 수정하지 않았다.
- [ ] 일반 CSV/RFC4180/데이터 행 검증/PK index/scalar/FK resolve/registry를 구현하지 않았다.
- [ ] Unity Asset Refresh가 PASS다.
- [ ] Compile Error가 0개이고 관련 신규 Warning이 0개다.
- [ ] 새 schema test 최소 15개와 architecture 10개가 모두 PASS다.
- [ ] PlayMode를 실행·생성하지 않았다.
- [ ] Scene/Prefab/Package/ProjectSettings 변경이 0개다.
- [ ] Result 문서가 필수 섹션과 실제 inventory를 포함한다.
- [ ] MAP01_03을 시작하지 않았다.

## Completion Rule

TASK EXECUTION은 Result에 `STATUS: PASS / FAIL / BLOCKED`만 기록한다.

Result가 정확히 `STATUS: PASS`이고 모든 DONE CONDITIONS가 완료된 경우에만 STATUS FINALIZE가:

```text
MAP01_02_IMPLEMENT_CSV_SCHEMA_CATALOG: CURRENT -> COMPLETE
Current Task: TASKS/MAP01_02_IMPLEMENT_CSV_SCHEMA_CATALOG.md -> NONE
```

을 수행한다.

STATUS FINALIZE는 MAP01_03을 CURRENT로 바꾸지 않는다. 다음 Task는 새 패치를 기다린다.
