# MAP01_06 — Implement Scalar and List Parsers

```yaml
status_control:
  task_key: MAP01_06_IMPLEMENT_SCALAR_AND_LIST_PARSERS
  result_file: REPORTS/MAP01_06_IMPLEMENT_SCALAR_AND_LIST_PARSERS_RESULT.md
```

## TASK TYPE

```text
RUNTIME CSV TYPED VALUE PARSING + EDITMODE TESTS
```

## Objective

MAP01_04의 successful validated records와 MAP01_05의 successful file-scoped PK index를 gate로 받아 각 field의 `EffectiveValue`를 MAP01_02 `CsvSchemaDataType`에 맞는 immutable typed value로 파싱한다.

숫자는 invariant culture, Boolean은 exact `0/1`, enum은 schema `AllowedValues`의 ordinal exact match다. list는 empty string이면 empty collection이고, non-empty면 `|` split 후 component만 trim하며 empty component를 금지한다. 오류는 file/record/field/physical line/column/offset과 함께 전부 수집하고 parsed record를 publish하지 않는다.

이 TASK는 typed value parsing까지만 수행한다. FK resolution, domain definition, Registry publish, content hash와 import UI는 구현하지 않는다.

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
12. `REPORTS/MAP01_05_IMPLEMENT_PRIMARY_KEY_INDEX_RESULT.md`

## READ ALLOWLIST

본문 읽기 허용:

- Mandatory Read Order의 파일
- MAP01_02 Runtime schema production C# 8개
- MAP01_03 Runtime reader production C# 7개
- MAP01_04 Runtime header/field validation production C# 6개
- MAP01_05 Runtime PK production C# 6개
- `CsvSchemaCatalogTests.cs`
- `Rfc4180CsvReaderTests.cs`
- `CsvHeaderAndFieldValidatorTests.cs`
- `CsvPrimaryKeyIndexBuilderTests.cs`
- `CsvSchemaDictionaryImporterTests.cs`
- architecture fixture 3개
- Runtime/Editor/test asmdef 4개
- 이 TASK WRITE ALLOWLIST의 기존 파일과 `.meta`

제한적 검색 허용:

- Runtime `WorldGeneration/Data/`와 Runtime Data test 디렉터리의 직계 파일명
- Authoring CSV/meta 50개의 경로·SHA-256·BOM만 확인
- 전체 `.meta` GUID 중복 검사
- 작업 전후 변경 경로와 Unity Console 상태

금지:

- Authoring CSV의 도메인 의미 분석
- 승인되지 않은 C# 본문, Scene/Prefab YAML, Legacy, later Task 본문 읽기
- CSV 수정·재저장

## WRITE ALLOWLIST

### 신규 Runtime production C# — 7

```text
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvHexValue.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvParsedValue.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvParsedField.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvParsedRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvValueParseError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvScalarAndListParseResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvScalarAndListParser.cs
```

### 신규 Runtime EditMode test — 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/CsvScalarAndListParserTests.cs
```

신규 C# 8개와 대응 `.cs.meta` 8개만 Asset 변경으로 허용한다. 기존 C#과 `.meta`는 수정하지 않는다.

추가 생성 허용:

```text
MapDesign/MCP/REPORTS/MAP01_06_IMPLEMENT_SCALAR_AND_LIST_PARSERS_RESULT.md
```

TASK 중 status는 수정하지 않고 PASS 이후 finalize만 수행한다.

## Namespace / Assembly Contract

```text
Runtime production: StarNight.Map.WorldGeneration.Data
Runtime tests      : StarNight.Map.Tests.WorldGeneration.Data
Assembly           : existing Game.Map.Runtime / Game.Map.Tests.EditMode
```

`UnityEditor` 참조와 새 asmdef/asmref/package는 금지한다.

## Input Gate Contract

`CsvScalarAndListParser`는 한 schema file 단위로 다음 세 입력을 받는다.

```text
CsvFileSchema
successful CsvHeaderFieldValidationResult
successful CsvPrimaryKeyIndexBuildResult
```

- validation result는 `Success == true`, errors 0이어야 한다.
- PK result는 `Success == true`, duplicates 0, non-null index여야 한다.
- schema filename, validated fields, PK index schema와 record/entry count가 서로 일치해야 한다.
- header-only successful file은 record/entry 0개 입력으로 성공한다.
- null은 `ArgumentNullException`, unsuccessful/mismatched input은 `InvalidOperationException`으로 partial output 없이 거부한다.
- schema, validation models, PK models와 source record/field를 수정하지 않는다.

## Empty Value Contract

MAP01_04가 required/default를 완료한 `EffectiveValue`만 입력으로 사용한다.

- exact `string.Empty` scalar는 optional empty typed value로 성공하며 `IsEmpty == true`다.
- whitespace-only scalar는 empty가 아니며 각 scalar 문법으로 검사한다.
- exact `string.Empty` list는 read-only empty collection으로 성공한다.
- default가 적용된 field는 default의 effective string을 동일 규칙으로 파싱한다.
- scalar field 자체를 trim, normalize, case-fold하지 않는다.

## Scalar Contract

`CsvParsedValue`는 schema data type, `IsEmpty`, 정확한 typed payload를 immutable하게 보존한다. 잘못된 type accessor는 `InvalidOperationException`으로 명시적으로 거부한다.

| Schema type | Typed payload | Exact parse contract |
|---|---|---|
| `STRING` | `string` | `EffectiveValue` 그대로 보존 |
| `ID` | `string` | empty가 아니면 ASCII `^[A-Z0-9_]+$`, ordinal 원문 보존 |
| `INT` | `int` | invariant, optional leading `+/-`, decimal digits, whitespace/thousands/decimal point 금지, overflow 실패 |
| `ULONG` | `ulong` | invariant decimal digits only, sign/whitespace/thousands 금지, overflow 실패 |
| `FLOAT` | `float` | invariant `.` decimal, optional sign/exponent 허용, comma/thousands/whitespace 금지, `NaN`/`Infinity`/overflow 금지 |
| `BOOL` | `bool` | exact `0 -> false`, `1 -> true`만 허용 |
| `ENUM` | `string` | `AllowedValues` 중 `StringComparer.Ordinal` exact match, 원문 보존 |
| `HEX` | `CsvHexValue` | optional `0x`/`0X` 뒤 ASCII hex digit 1개 이상; sign/whitespace/underscore 금지 |
| `DATETIME` | `DateTimeOffset` | invariant ISO-8601 UTC `yyyy-MM-dd'T'HH:mm:ss[.fffffff]Z`, offset/local/whitespace 금지 |

`ENUM` schema의 `AllowedValues`가 비어 있으면 data error가 아니라 schema contract 불일치이므로 `InvalidOperationException`으로 input을 거부한다.

`CsvHexValue`:

- 원본 effective string과 prefix를 제외해 해석한 read-only byte sequence를 보존한다.
- odd hex digit count는 byte 변환에서 leading zero nibble 하나만 보완하며 원본은 변경하지 않는다.
- hex letter case와 optional prefix는 허용하되 sign, 내부 공백, separator는 허용하지 않는다.
- 외부에서 byte collection을 수정할 수 없다.

## List Contract

지원 list type:

```text
ID_LIST   -> IReadOnlyList<string>
ENUM_LIST -> IReadOnlyList<string>
INT_LIST  -> IReadOnlyList<int>
```

1. 전체 effective string이 empty면 empty list다.
2. non-empty면 exact `|`로 split한다.
3. split한 각 component에만 `Trim()`을 적용한다.
4. trim 전후를 불문하고 component가 empty면 오류다. leading/trailing `|`, `||`, whitespace-only item을 silent drop하지 않는다.
5. ID item은 trim 뒤 ASCII `^[A-Z0-9_]+$`다.
6. ENUM item은 trim 뒤 schema `AllowedValues`의 ordinal exact match다.
7. INT item은 trim 뒤 scalar INT와 같은 invariant/overflow 규칙이다.
8. 원래 component 순서와 duplicate item을 그대로 보존한다. duplicate 금지는 이 TASK의 일반 규칙이 아니다.
9. ID/ENUM list의 FK 존재 여부는 검사하지 않는다.

`ENUM_LIST` schema의 `AllowedValues`가 비어 있으면 `InvalidOperationException`으로 input을 거부한다.

## Parsed Model Contract

`CsvParsedField`:

```text
Schema
ValidatedField
RawValue
EffectiveValue
UsedDefault
Value (CsvParsedValue)
```

`CsvParsedRecord`:

```text
RecordNumber
Fields (schema order, read-only)
ValidatedRecord
SourceRecord
```

`CsvScalarAndListParseResult`:

- `Success`, `Records`, `Errors`를 immutable/read-only로 노출한다.
- success면 errors 0이고 모든 parsed records를 source record order로 publish한다.
- parse error가 하나라도 있으면 records 0개이고 가능한 모든 field error를 반환한다.
- PK index는 읽기 gate일 뿐 변경·재구축·대체하지 않는다.

## Error Contract

`CsvValueParseErrorCode`는 `CsvValueParseError.cs` 안에 선언하고 최소 다음 exact 의미를 구분한다.

```text
InvalidId
InvalidInteger
InvalidUnsignedInteger
InvalidFloat
InvalidBoolean
InvalidEnum
InvalidHex
InvalidDateTime
EmptyListItem
InvalidListItem
```

모든 `CsvValueParseError`는 다음 immutable context를 가진다.

```text
SourceName
SchemaFileName
ColumnName
DataType
ErrorCode
Message
RecordNumber
FieldNumber
PhysicalLine
PhysicalColumn
CharOffset
EffectiveValue
ListItemIndex (scalar 또는 field-level이면 null)
ListItemValue
AllowedValues (read-only)
```

오류 위치는 해당 `CsvValidatedField.SourceField`의 exact start location이다. 임의 `-1` 위치를 사용하지 않는다.

- 한 field 오류 뒤에도 안전한 다른 field와 후속 record를 계속 검사한다.
- 오류 정렬은 source record number → schema `ColumnOrder` → list item index → error code ordinal이다.
- enum 오류는 allowed values를, list item 오류는 item index/value를 보존한다.
- 같은 입력은 실행마다 동일 error inventory/order를 만든다.

## DO NOT

- reader, schema catalog/model/builder, header/field validator/model, PK index/model 수정 금지
- dictionary importer 수정 금지
- scalar 전체 문자열 trim/normalize/case-fold 금지
- invalid number/bool/enum/hex/date를 default로 대체 금지
- list empty item silent drop 금지
- FK target lookup/resolve 금지
- numeric domain range, ID 존재성, 도메인 상호 제약 검증 금지
- domain definition/StaticDataRegistry/hash/report/window 구현 금지
- CSV/ScriptableObject/asset 수정·생성 금지
- 기존 C#/test 수정 금지
- asmdef/Scene/Prefab/Package/ProjectSettings 변경 금지
- 외부 dependency 설치 금지
- Git operation 금지
- MAP01_07 선행 작업 금지

## Collision Handling

1. 신규 파일이 없으면 생성한다.
2. 동일 경로가 이미 있고 exact 계약과 바이트 동일하면 `PREEXISTING_IDENTICAL`로 기록한다.
3. 다르면 덮어쓰기·병합하지 않고 `BLOCKED`다.
4. 기존 `.meta` GUID를 보존한다.
5. 기존 사용자 변경을 되돌리지 않는다.

## Implementation Steps

1. Current Task MAP01_06, Master 205개, MAP01_05 COMPLETE/PASS를 확인한다.
2. MAP01_05 Result의 PK 32/32, validator 29/29, reader 31/31, schema 23/23, importer 9/9, architecture 10/10, total 134/134, compile/warning 0/0을 확인한다.
3. 작업 전 C#/CSV/meta inventory와 preservation hash를 기록한다.
4. allowlisted schema/reader/validator/PK/test/asmdef만 읽어 실제 API에 맞춘다.
5. hex/parsed value/field/record/error/result immutable models를 구현한다.
6. 12개 schema type과 list item parser를 exact contract로 구현한다.
7. 최소 40개 scalar/list unit test를 구현한다.
8. Unity refresh/compile 후 신규 parser tests와 기존 PK/validator/reader/schema/importer/architecture fixtures를 실행한다.
9. 신규 meta 8개의 GUID와 프로젝트 중복을 확인한다.
10. 기존 CSV/meta와 MAP01_02/03/04/05 C#의 비허용 변경이 0인지 확인한다.
11. Result를 작성하고 모든 조건이 충족될 때만 PASS를 기록한다.

## Required Tests

`CsvScalarAndListParserTests` 최소 40 case:

- STRING exact preservation / optional empty / whitespace preservation
- ID valid / lowercase invalid / hyphen invalid / optional empty
- INT zero / positive / negative / leading plus / min/max / overflow / whitespace invalid / decimal invalid
- ULONG zero / max / negative invalid / plus invalid / overflow
- FLOAT integer form / decimal / negative / exponent / locale comma invalid / whitespace invalid / NaN invalid / Infinity invalid / overflow invalid
- BOOL exact 0/1 / true-false invalid / case variant invalid
- ENUM allowed exact / case mismatch / unknown / empty optional / empty allowed-values schema rejected
- HEX upper/lower / optional prefix / odd digit / invalid digit / sign-space-underscore invalid / immutable bytes
- DATETIME UTC whole seconds / fractional seconds / invalid date / offset invalid / missing Z / whitespace invalid
- ID_LIST empty / one / multiple / component trim / invalid ID
- ENUM_LIST empty / component trim / exact allowed / invalid item / duplicate preserved
- INT_LIST empty / negative and positive / component trim / overflow item
- leading/trailing pipe / doubled pipe / whitespace-only item errors
- default effective value parsing / raw value precedence preservation
- multiple field/record errors accumulated in deterministic order
- any error publishes zero parsed records
- success publishes schema-order immutable fields and source references
- unsuccessful/mismatched validation or PK gate rejects without partial output
- schema/validation/PK/source models remain unchanged

Targeted regression:

```text
New scalar/list parser: >=40 / ALL PASS
Primary-key index: 32/32 PASS
Header/field validator: 29/29 PASS
RFC4180 reader: 31/31 PASS
Schema catalog: 23/23 PASS
Dictionary importer: 9/9 PASS
Architecture: 10/10 PASS
Targeted total: >=174 / ALL PASS
```

## Unity Verification

```text
Unity Version: 6000.3.8f1
Asset Refresh: PASS
Compile Errors: 0
Relevant New Warnings: 0
Targeted EditMode: >=174 / ALL PASS
PlayMode: NOT RUN
Scene/Prefab Changes: NONE
```

Unity compile/test 증거가 없으면 `BLOCKED`다.

## Result File

```text
REPORTS/MAP01_06_IMPLEMENT_SCALAR_AND_LIST_PARSERS_RESULT.md
```

필수 섹션:

```text
TASK
STATUS
SUMMARY
READ
MASTER BACKLOG CHECK
MAP01_05 GATE CHECK
CREATED
PREEXISTING_IDENTICAL
SCALAR CONTRACTS IMPLEMENTED
LIST CONTRACTS IMPLEMENTED
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

- [ ] Current Task가 MAP01_06이다.
- [ ] Master 205개와 MAP01_05 COMPLETE/PASS를 확인했다.
- [ ] successful validation과 PK index만 입력 gate로 받는다.
- [ ] STRING/ID/INT/ULONG/FLOAT/BOOL/ENUM/HEX/DATETIME을 exact contract로 파싱한다.
- [ ] ID_LIST/ENUM_LIST/INT_LIST의 empty/split/component trim/empty-item 규칙을 지킨다.
- [ ] number/date parsing이 invariant culture이며 locale에 의존하지 않는다.
- [ ] bool이 exact 0/1이고 enum이 ordinal AllowedValues와 일치한다.
- [ ] invalid value를 silent default로 숨기지 않는다.
- [ ] 모든 오류가 source/schema/column/record/field/line/column/offset context를 가진다.
- [ ] 가능한 모든 field 오류를 deterministic order로 반환한다.
- [ ] 오류 하나라도 있으면 parsed records를 publish하지 않는다.
- [ ] parsed value/field/record/error/result와 collection이 immutable/read-only다.
- [ ] 신규 Runtime C# 7개와 test 1개만 생성했다.
- [ ] 신규 meta 8개가 유효하고 GUID 중복이 없다.
- [ ] reader/schema/validator/PK/importer/기존 tests를 수정하지 않았다.
- [ ] CSV 50개/meta 50개를 수정하지 않았다.
- [ ] FK/domain definitions/Registry 등 MAP01_07 이후 기능을 구현하지 않았다.
- [ ] asmdef/asset/Scene/Prefab/Package/ProjectSettings 변경이 없다.
- [ ] Unity refresh, compile 0, warning 0을 확인했다.
- [ ] targeted EditMode 최소 174개가 전부 PASS다.
- [ ] PlayMode를 실행·생성하지 않았다.
- [ ] Result가 실제 inventory와 필수 섹션을 포함한다.
- [ ] MAP01_07을 시작하지 않았다.

## Completion Rule

Result가 exact `STATUS: PASS`이고 모든 조건이 완료된 경우에만 finalize가:

1. MAP01_06을 `CURRENT -> COMPLETE`로 바꾼다.
2. Last Completed/Last Result를 MAP01_06으로 갱신한다.
3. Current Task를 `NONE`으로 만든다.
4. MAP01_07 이후는 모두 `LOCKED`로 유지한다.
5. `MAP01_07_IMPLEMENT_WORLD_ROUTE_DEFINITIONS`를 자동 생성·실행하지 않는다.

FAIL/BLOCKED이면 MAP01_06 CURRENT를 유지하고 다음 Task로 진행하지 않는다.

## Recommended Commit

```text
feat(map): parse invariant CSV scalar and list values
```
