# MAP01_03 — Implement RFC4180 Reader

```yaml
status_control:
  task_key: MAP01_03_IMPLEMENT_RFC4180_READER
  result_file: REPORTS/MAP01_03_IMPLEMENT_RFC4180_READER_RESULT.md
```

## TASK TYPE

```text
RUNTIME CSV SYNTAX READER + EDITMODE TESTS
```

## Objective

UTF-8 CSV bytes에서 comma, quoted field, escaped quote, CRLF/LF, multiline field, UTF-8 BOM을 결정적으로 읽고 각 record/field 및 오류의 정확한 위치를 제공하는 generic `Rfc4180CsvReader`를 구현한다.

MAP01_02의 restricted `CsvSchemaDictionaryImporter`는 새 reader를 사용하도록 교체한다. 이 TASK는 CSV syntax만 읽으며 schema header/required/default 검증, typed scalar/list parsing, PK/FK 처리, Registry publish는 구현하지 않는다.

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
12. `REPORTS/MAP01_02_IMPLEMENT_CSV_SCHEMA_CATALOG_RESULT.md`
13. `Assets/_Game/Map/Data/WorldGeneration/Authoring/CSV_DATA_DICTIONARY.csv`

## READ ALLOWLIST

본문 읽기 허용:

- Mandatory Read Order의 파일
- MAP01_02에서 생성한 Runtime schema C# 8개
- `Assets/_Game/Editor/MapAuthoring/WorldGeneration/Import/CsvSchemaDictionaryImporter.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/CsvSchemaCatalogTests.cs`
- `Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Import/CsvSchemaDictionaryImporterTests.cs`
- 기존 architecture fixture 3개
- Runtime/Editor/test asmdef 4개
- 이 TASK의 WRITE ALLOWLIST에 해당하는 기존 파일과 `.meta`

제한적 검색 허용:

- `Assets/_Game/Map/Runtime/WorldGeneration/Data/`와 대응 test 디렉터리의 직계 파일명
- 설치된 Authoring CSV 50개의 경로·SHA-256·BOM만 확인
- 프로젝트 전체 `.meta`의 GUID 값 중복 검사
- 작업 전후 변경 파일 경로
- Unity Console compile error와 이 TASK 관련 warning

금지:

- dictionary 이외 Authoring CSV의 셀 의미 분석
- 승인되지 않은 C# 본문 스캔
- Scene/Prefab YAML, `Assets/_Legacy/**`, later Task 본문 읽기
- CSV 재저장·정규화·자동 수정

## WRITE ALLOWLIST

### 신규 Runtime production C# — 7

```text
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvSourceLocation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvField.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvReadErrorCode.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvReadError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvReadResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/Rfc4180CsvReader.cs
```

### 신규 Runtime EditMode test — 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/Rfc4180CsvReaderTests.cs
```

### 수정 허용 — 2

```text
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Import/CsvSchemaDictionaryImporter.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Import/CsvSchemaDictionaryImporterTests.cs
```

신규 C# 8개와 대응 `.cs.meta` 8개, 기존 C# 2개 수정만 허용한다. 기존 `.meta`는 수정하지 않는다.

추가 생성 허용:

```text
MapDesign/MCP/REPORTS/MAP01_03_IMPLEMENT_RFC4180_READER_RESULT.md
```

TASK EXECUTION 중 status는 수정하지 않는다. Result PASS 이후 STATUS FINALIZE만 상태를 변경한다.

## Namespace Contract

```text
Runtime production: StarNight.Map.WorldGeneration.Data
Runtime tests      : StarNight.Map.Tests.WorldGeneration.Data
Editor importer    : 기존 StarNight.MapAuthoring.WorldGeneration.Import 유지
Editor tests       : 기존 StarNight.MapAuthoring.Tests.WorldGeneration.Import 유지
```

Runtime production은 `UnityEditor`를 참조하지 않는다. 새 asmdef/asmref와 package를 만들지 않는다.

## Public Syntax Model Contract

### CsvSourceLocation

정확한 위치 기준:

```text
CharOffset     : BOM 제거 후 decoded text 기준 0-based UTF-16 code-unit offset
PhysicalLine   : 1-based
PhysicalColumn : 1-based UTF-16 code-unit column
RecordNumber   : 1-based logical record number
FieldNumber    : 1-based logical field number
```

CRLF는 하나의 line boundary다. CR 위치와 LF 위치를 별도 line으로 세지 않는다. quoted multiline field 내부에서도 physical line/column은 실제 입력을 따라 증가한다.

### CsvField

다음을 immutable로 보존한다.

```text
Value
WasQuoted
StartLocation
EndLocationExclusive
```

outer quote는 Value에 포함하지 않고 escaped quote `""`는 하나의 `"`로 decode한다. quoted field 내부 CRLF/LF는 원문 line ending sequence를 그대로 Value에 보존한다.

### CsvRecord

```text
RecordNumber
Fields (ordered read-only)
StartLocation
EndLocationExclusive
```

### CsvReadError

최소 error code:

```text
InvalidUtf8
UnsupportedBom
BareCarriageReturn
UnexpectedQuoteInUnquotedField
UnexpectedCharacterAfterClosingQuote
UnterminatedQuotedField
```

오류는 source name, error code, deterministic message, exact location을 가진다.

### CsvReadResult

- `Success`, `HadUtf8Bom`, `Records`, `Errors`를 immutable/read-only로 노출한다.
- syntax error가 하나라도 있으면 `Success = false`이고 usable partial records를 publish하지 않는다.
- 성공 시 errors는 empty다.
- 실패 시 records는 empty다.

## Reader Input Contract

권장 API:

```text
CsvReadResult Read(byte[] utf8Bytes, string sourceName)
```

- null argument는 명시적 argument exception이다.
- strict UTF-8 decoder를 사용한다. invalid byte sequence를 replacement character로 숨기지 않는다.
- UTF-8 BOM `EF BB BF`는 허용하고 `HadUtf8Bom = true`다.
- BOM 없음도 허용하고 `HadUtf8Bom = false`다.
- UTF-16 LE/BE 또는 UTF-32 BOM은 `UnsupportedBom`이다.
- 입력 bytes와 원본 파일을 수정하지 않는다.

## RFC4180 State Contract

명시적 parser state는 최소 다음 의미를 구분한다.

```text
StartField
InUnquotedField
InQuotedField
AfterClosingQuote
```

규칙:

1. comma는 field separator다.
2. CRLF와 LF는 record separator다.
3. bare CR은 허용하지 않고 exact location error다.
4. quoted field는 field 첫 문자 `"`로만 시작한다.
5. quoted field 내부 comma와 CRLF/LF는 field content다.
6. quoted field 내부 `""`는 escaped quote다.
7. closing quote 뒤에는 comma, CRLF, LF, EOF만 허용한다.
8. unquoted field 내부 `"`는 오류다.
9. EOF가 quoted field 안에서 오면 `UnterminatedQuotedField`다.
10. trailing comma는 마지막 empty field를 만든다.
11. terminal record separator는 phantom empty record를 만들지 않는다.
12. empty input은 record 0개 성공이다.
13. blank line은 empty field 1개인 record다.
14. input line ending을 임의로 normalize하지 않는다.

parser는 현재 culture, OS line ending, dictionary row order에 의존하지 않는다.

## MAP01_02 Importer Migration

기존 `CsvSchemaDictionaryImporter`에서 restricted comma split/tokenizer를 제거하고 `Rfc4180CsvReader`의 record/field 결과를 사용한다.

dictionary 전용 계약은 유지한다.

- exact path
- UTF-8 BOM required
- exact 10-column header
- 각 data record field count 10
- canonical baseline 679 rows / 60 files
- Runtime `CsvSchemaCatalogBuilder`에 raw rows 전달

reader는 quoted field를 지원하므로 importer의 기존 “quote가 있으면 거부” 계약과 테스트는 제거한다. 대신 quoted description에 comma, escaped quote, multiline이 있어도 정확히 10 fields로 읽히는 importer test로 교체한다.

## DO NOT

- schema header를 catalog와 대조하는 generic validation 구현 금지
- required/default 규칙 적용 금지
- row DTO/domain definition 생성 금지
- actual data PK 수집·중복 검사 금지
- int/ulong/float/bool/hex/enum/list typed parser 구현 금지
- FK target resolution 금지
- StaticDataRegistry, ContentVersionHash, import report/window 구현 금지
- CSV 파일 수정·재저장 금지
- ScriptableObject/asset 생성 금지
- MAP01_02 Runtime schema C# 수정 금지
- 기존 reader 외 C# 또는 test 수정 금지
- asmdef/asmref, Scene, Prefab, Package, ProjectSettings 변경 금지
- 외부 CSV parser package 설치 금지
- 기존 파일 삭제/이동/이름 변경 금지
- Git operation 금지
- MAP01_04 선행 작업 금지

## Collision Handling

1. 신규 경로가 없으면 생성한다.
2. 신규 경로가 이미 있고 payload 계약과 바이트 동일하면 `PREEXISTING_IDENTICAL`로 기록한다.
3. 신규 경로가 다르면 덮어쓰기·병합하지 않고 `BLOCKED`다.
4. 수정 허용 2개 파일은 작업 전 SHA-256을 기록하고 필요한 reader migration만 최소 변경한다.
5. 기존 `.meta` GUID를 보존한다.
6. 기존 사용자 변경을 되돌리지 않는다.

## Implementation Steps

1. Current Task MAP01_03, Master 205개, MAP01_02 COMPLETE/PASS를 확인한다.
2. MAP01_02 Result의 60/679 catalog, 30/30 schema tests, 10/10 architecture, compile 0을 확인한다.
3. 작업 전 C#/CSV/meta 및 수정 대상 2개 파일 hash를 기록한다.
4. 허용된 기존 schema/importer/test/asmdef만 읽어 실제 API와 style을 확인한다.
5. 위치·field·record·error·result immutable model을 구현한다.
6. strict UTF-8/BOM 및 4-state RFC4180 reader를 구현한다.
7. dictionary importer의 restricted tokenizer를 reader 호출로 교체한다.
8. reader unit tests와 importer quoted regression tests를 구현·수정한다.
9. Unity refresh/compile을 완료한다.
10. reader, schema catalog, dictionary importer, architecture fixtures만 대상으로 EditMode를 실행한다.
11. 신규 meta 8개 GUID와 프로젝트 중복을 확인한다.
12. 기존 CSV 50개/meta 50개와 MAP01_02 schema files의 비허용 변경이 0인지 확인한다.
13. Result를 작성하고 모든 DONE CONDITIONS가 PASS일 때만 `STATUS: PASS`를 기록한다.

## Required Tests

### Rfc4180CsvReaderTests — 최소 20 case

다음을 각각 명시적으로 검증한다.

- basic unquoted fields
- leading/middle/trailing empty fields
- quoted comma
- escaped quote
- quoted CRLF multiline
- quoted LF multiline
- CRLF records
- LF records
- mixed CRLF/LF records
- UTF-8 BOM present/absent
- strict UTF-8 Korean text
- empty input
- blank line record
- terminal newline without phantom record
- field/record ordinal order
- multiline physical location
- bare CR error
- quote inside unquoted error
- character after closing quote error
- unterminated quote error
- invalid UTF-8 error
- UTF-16/32 BOM error
- failure publishes zero records
- source bytes unchanged

### Importer regression

- canonical dictionary still imports `60 files / 679 columns`.
- quoted description containing comma, escaped quote, CRLF/LF multiline fixture is accepted through the new reader.
- BOM/header/10-field dictionary-specific failures remain deterministic.

### Full targeted regression

```text
Rfc4180CsvReaderTests: >=20 / ALL PASS
CsvSchemaCatalogTests: existing 23/23 PASS
CsvSchemaDictionaryImporterTests: existing-or-expanded >=7 / ALL PASS
Architecture fixtures: 10/10 PASS
Targeted total: >=60 / ALL PASS
```

실제 discovered case 수를 Result에 기록한다.

## Unity Verification

```text
Unity Version: 6000.3.8f1
Asset Refresh: PASS
Compile Errors: 0
Relevant New Warnings: 0
Targeted EditMode: >=60 / ALL PASS
PlayMode: NOT RUN
Scene/Prefab Changes: NONE
```

Unity 접근이나 compile/test 증거가 없으면 PASS가 아니라 `BLOCKED`다.

## Result File

```text
REPORTS/MAP01_03_IMPLEMENT_RFC4180_READER_RESULT.md
```

필수 섹션:

```text
TASK
STATUS
SUMMARY
READ
MASTER BACKLOG CHECK
MAP01_02 GATE CHECK
CREATED
MODIFIED
PREEXISTING_IDENTICAL
RFC4180 CONTRACTS IMPLEMENTED
IMPORTER MIGRATION
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

- [ ] Current Task가 MAP01_03이다.
- [ ] Master 205개와 MAP01_02 COMPLETE/PASS를 확인했다.
- [ ] strict UTF-8와 UTF-8 BOM present/absent를 읽는다.
- [ ] quoted comma, escaped quote, CRLF/LF multiline을 읽는다.
- [ ] record/field/error 위치가 exact location contract와 일치한다.
- [ ] bare CR, invalid quote transition, unterminated quote, invalid UTF-8/BOM을 거부한다.
- [ ] syntax failure가 partial records를 publish하지 않는다.
- [ ] dictionary importer가 새 reader를 사용하고 기존 restricted tokenizer를 제거했다.
- [ ] canonical dictionary가 계속 60 files / 679 columns로 import된다.
- [ ] 신규 Runtime C# 7개와 test C# 1개만 생성했다.
- [ ] importer/test 기존 C# 2개만 필요한 범위로 수정했다.
- [ ] 신규 meta 8개가 유효하고 GUID 중복이 없다.
- [ ] CSV 50개와 meta 50개가 변경되지 않았다.
- [ ] MAP01_02 schema model/builder 비허용 변경이 0개다.
- [ ] generic header/field validation 및 MAP01_04 이후 기능을 구현하지 않았다.
- [ ] 새 asmdef/package/asset/Scene/Prefab 변경이 없다.
- [ ] Unity refresh, compile 0, warning 0을 확인했다.
- [ ] targeted EditMode 최소 60개가 전부 PASS다.
- [ ] PlayMode를 실행·생성하지 않았다.
- [ ] Result가 실제 created/modified/test inventory를 포함한다.
- [ ] MAP01_04를 시작하지 않았다.

## Completion Rule

Result가 exact `STATUS: PASS`이고 모든 조건이 완료된 경우에만 STATUS FINALIZE가:

```text
MAP01_03_IMPLEMENT_RFC4180_READER: CURRENT -> COMPLETE
Current Task: TASKS/MAP01_03_IMPLEMENT_RFC4180_READER.md -> NONE
```

을 수행한다. MAP01_04를 자동으로 CURRENT로 바꾸지 않는다.
