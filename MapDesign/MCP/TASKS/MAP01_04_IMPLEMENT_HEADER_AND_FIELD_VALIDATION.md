# MAP01_04 — Implement Header and Field Validation

```yaml
status_control:
  task_key: MAP01_04_IMPLEMENT_HEADER_AND_FIELD_VALIDATION
  result_file: REPORTS/MAP01_04_IMPLEMENT_HEADER_AND_FIELD_VALIDATION_RESULT.md
```

## TASK TYPE

```text
RUNTIME CSV STRUCTURAL VALIDATION + EDITMODE TESTS
```

## Objective

MAP01_03의 RFC4180 syntax 결과를 MAP01_02의 `CsvFileSchema`와 대조해 header 누락·추가·중복·순서 불일치, data record field count, required/default 규칙을 파일·record·field·physical line/column 위치와 함께 보고한다.

검증 오류가 하나라도 있으면 validated row를 publish하지 않는다. 이 TASK는 raw string/default 적용까지만 하며 typed scalar/list parsing, PK/FK 처리, domain definition, Registry는 구현하지 않는다.

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
12. `REPORTS/MAP01_03_IMPLEMENT_RFC4180_READER_RESULT.md`

## READ ALLOWLIST

본문 읽기 허용:

- Mandatory Read Order의 파일
- MAP01_02 Runtime schema production C# 8개
- MAP01_03 Runtime reader production C# 7개
- `CsvSchemaCatalogTests.cs`
- `Rfc4180CsvReaderTests.cs`
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

### 신규 Runtime production C# — 6

```text
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvHeaderFieldErrorCode.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvHeaderFieldError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvValidatedField.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvValidatedRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvHeaderFieldValidationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvHeaderAndFieldValidator.cs
```

### 신규 Runtime EditMode test — 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/CsvHeaderAndFieldValidatorTests.cs
```

신규 C# 7개와 대응 `.cs.meta` 7개만 Asset 변경으로 허용한다. 기존 C#과 `.meta`는 수정하지 않는다.

추가 생성 허용:

```text
MapDesign/MCP/REPORTS/MAP01_04_IMPLEMENT_HEADER_AND_FIELD_VALIDATION_RESULT.md
```

TASK 중 status는 수정하지 않고 PASS 이후 finalize만 수행한다.

## Namespace / Assembly Contract

```text
Runtime production: StarNight.Map.WorldGeneration.Data
Runtime tests      : StarNight.Map.Tests.WorldGeneration.Data
Assembly           : existing Game.Map.Runtime / Game.Map.Tests.EditMode
```

`UnityEditor` 참조와 새 asmdef/asmref/package는 금지한다.

## Error Contract

`CsvHeaderFieldErrorCode`는 최소 다음 exact 의미를 구분한다.

```text
SyntaxReadFailed
MissingHeader
UnexpectedHeader
DuplicateHeader
HeaderOrderMismatch
FieldCountMismatch
RequiredFieldEmpty
```

모든 오류는 다음 context를 immutable하게 보존한다.

```text
SourceName
SchemaFileName
ErrorCode
Message
RecordNumber
FieldNumber
PhysicalLine
PhysicalColumn
CharOffset
ExpectedValue
ActualValue
```

위 위치는 MAP01_03 `CsvSourceLocation` 계약을 그대로 사용한다. 위치 없는 임의 `-1`은 사용하지 않는다.

위치 규칙:

- unexpected/duplicate/order: 해당 actual header field의 start location
- missing header: header record의 end-exclusive location; header record 자체가 없으면 file start `(offset 0, line 1, column 1, record 1, field 1)`
- extra data field: 첫 extra field start
- missing data field: record end-exclusive
- required empty: 해당 data field start
- syntax failure: reader의 첫 exact error location

## Header Validation Contract

1. expected header는 `CsvFileSchema.Columns`의 `column_order` 순서다.
2. 비교는 exact `StringComparer.Ordinal`, case-sensitive다.
3. header record가 없으면 모든 expected column을 `MissingHeader`로 보고한다.
4. actual duplicate name은 첫 occurrence를 유지하고 두 번째부터 `DuplicateHeader`다.
5. expected에 없는 actual name은 `UnexpectedHeader`다.
6. actual에 없는 expected name은 `MissingHeader`다.
7. duplicate/missing/unexpected가 없고 set이 같지만 순서가 다르면 first mismatching position부터 각 mismatch를 `HeaderOrderMismatch`로 보고한다.
8. header error가 하나라도 있으면 field-to-schema mapping이 안전하지 않으므로 data record validation을 수행하지 않고 validated records를 0개 publish한다.

오류 정렬은 header actual position, missing expected order, error code의 고정 ordinal 순서를 사용해 실행마다 동일해야 한다.

## Field Count Contract

- header 다음 record가 data record다.
- 각 data record field count는 schema column count와 정확히 같아야 한다.
- field가 많으면 first extra field 위치에서 `FieldCountMismatch`다.
- field가 적으면 record end 위치에서 `FieldCountMismatch`다.
- count mismatch record는 required/default 처리를 하지 않는다.
- 다른 정상 count record는 계속 검사해 전체 오류를 한 번에 수집한다.

## Required / Default Contract

각 field는 raw string을 변경하지 않고 다음 precedence로 effective value를 결정한다.

```text
raw value != ""                    -> effective = raw, UsedDefault = false
raw value == "" and default != "" -> effective = default, UsedDefault = true
raw value == "" and default == "" -> effective = "", UsedDefault = false
```

- empty는 exact `string.Empty`다. whitespace는 trim하지 않으며 empty로 취급하지 않는다.
- default는 raw schema string 그대로 적용하고 typed parse하지 않는다.
- default 적용 후에도 effective value가 empty이고 `IsRequired`면 `RequiredFieldEmpty`다.
- optional empty/default empty는 성공이다.
- optional field에도 non-empty default가 있으면 같은 precedence로 적용한다.
- non-empty raw value는 default보다 항상 우선한다.

## Validated Model Contract

`CsvValidatedField`:

```text
Schema
SourceField
RawValue
EffectiveValue
UsedDefault
```

`CsvValidatedRecord`:

```text
RecordNumber
Fields (schema order, read-only)
SourceRecord
```

`CsvHeaderFieldValidationResult`:

- `Success`, `Records`, `Errors`를 immutable/read-only로 노출한다.
- success면 errors 0이며 header를 제외한 validated data records를 publish한다.
- error가 하나라도 있으면 records 0개다.
- 입력 reader result, schema, record, field를 수정하지 않는다.

`CsvHeaderAndFieldValidator`는 unsuccessful `CsvReadResult`를 받으면 reader의 첫 오류를 `SyntaxReadFailed`로 보존하고 records 0개로 실패한다.

## DO NOT

- RFC4180 reader 또는 schema catalog/model/builder 수정 금지
- dictionary importer 수정 금지
- header 자동 수정·재정렬·추가·삭제 금지
- raw CSV field trim/normalize 금지
- int/ulong/float/bool/hex/enum/list parse 금지
- PK 수집·중복 검사 금지
- FK target resolve 금지
- domain definition/StaticDataRegistry/hash/report/window 구현 금지
- CSV/ScriptableObject/asset 수정·생성 금지
- 기존 C#/test 수정 금지
- asmdef/Scene/Prefab/Package/ProjectSettings 변경 금지
- 외부 dependency 설치 금지
- Git operation 금지
- MAP01_05 선행 작업 금지

## Collision Handling

1. 신규 파일이 없으면 생성한다.
2. 동일 경로가 이미 있고 exact 계약과 바이트 동일하면 `PREEXISTING_IDENTICAL`로 기록한다.
3. 다르면 덮어쓰기·병합하지 않고 `BLOCKED`다.
4. 기존 `.meta` GUID를 보존한다.
5. 기존 사용자 변경을 되돌리지 않는다.

## Implementation Steps

1. Current Task MAP01_04, Master 205개, MAP01_03 COMPLETE/PASS를 확인한다.
2. MAP01_03 Result의 reader 31/31, schema 23/23, importer 9/9, architecture 10/10, total 73/73, compile 0을 확인한다.
3. 작업 전 C#/CSV/meta inventory와 preservation hash를 기록한다.
4. allowlisted reader/schema/test/asmdef만 읽어 실제 API에 맞춘다.
5. error code/model, validated field/record/result를 구현한다.
6. header, field count, required/default validator를 구현한다.
7. 최소 24개 validator unit test를 구현한다.
8. Unity refresh/compile 후 validator와 기존 reader/schema/importer/architecture fixtures를 실행한다.
9. 신규 meta 7개의 GUID와 프로젝트 중복을 확인한다.
10. 기존 CSV/meta와 MAP01_02/03 C#의 비허용 변경이 0인지 확인한다.
11. Result를 작성하고 모든 조건이 충족될 때만 PASS를 기록한다.

## Required Tests

`CsvHeaderAndFieldValidatorTests` 최소 24 case:

- exact header success / case sensitivity
- missing header record / missing one header
- unexpected header
- duplicate header
- reordered header
- missing+unexpected deterministic inventory
- header error publishes zero rows
- header-only file success with zero data rows
- exact field count
- too few / too many fields with exact location
- mismatch row skipped while later rows still inspected
- required non-empty
- required empty without default error
- required empty with default success
- optional empty without default
- optional empty with default
- non-empty raw overrides default
- whitespace is not empty
- quoted comma and multiline field remain one field
- multiple row errors stable ordering
- syntax reader failure becomes `SyntaxReadFailed`
- any error publishes zero records
- successful record/field collections immutable
- input reader/schema models and source bytes unchanged

Targeted regression:

```text
New validator: >=24 / ALL PASS
RFC4180 reader: 31/31 PASS
Schema catalog: 23/23 PASS
Dictionary importer: 9/9 PASS
Architecture: 10/10 PASS
Targeted total: >=97 / ALL PASS
```

## Unity Verification

```text
Unity Version: 6000.3.8f1
Asset Refresh: PASS
Compile Errors: 0
Relevant New Warnings: 0
Targeted EditMode: >=97 / ALL PASS
PlayMode: NOT RUN
Scene/Prefab Changes: NONE
```

Unity compile/test 증거가 없으면 `BLOCKED`다.

## Result File

```text
REPORTS/MAP01_04_IMPLEMENT_HEADER_AND_FIELD_VALIDATION_RESULT.md
```

필수 섹션:

```text
TASK
STATUS
SUMMARY
READ
MASTER BACKLOG CHECK
MAP01_03 GATE CHECK
CREATED
PREEXISTING_IDENTICAL
HEADER CONTRACTS IMPLEMENTED
FIELD CONTRACTS IMPLEMENTED
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

- [ ] Current Task가 MAP01_04다.
- [ ] Master 205개와 MAP01_03 COMPLETE/PASS를 확인했다.
- [ ] header missing/unexpected/duplicate/order를 ordinal로 검증한다.
- [ ] field count mismatch를 exact source location으로 보고한다.
- [ ] required/default precedence가 raw string 계약과 일치한다.
- [ ] whitespace를 empty로 취급하지 않는다.
- [ ] 모든 오류가 source/schema/record/field/line/column/offset을 가진다.
- [ ] header 오류 시 data mapping을 수행하지 않는다.
- [ ] 오류를 가능한 범위에서 누적하고 deterministic order로 반환한다.
- [ ] 오류 하나라도 있으면 validated records를 publish하지 않는다.
- [ ] success result와 validated models가 immutable/read-only다.
- [ ] 신규 Runtime C# 6개와 test 1개만 생성했다.
- [ ] 신규 meta 7개가 유효하고 GUID 중복이 없다.
- [ ] reader/schema/importer/기존 tests를 수정하지 않았다.
- [ ] CSV 50개/meta 50개를 수정하지 않았다.
- [ ] typed parser/PK/FK/Registry 등 MAP01_05 이후 기능을 구현하지 않았다.
- [ ] asmdef/asset/Scene/Prefab/Package/ProjectSettings 변경이 없다.
- [ ] Unity refresh, compile 0, warning 0을 확인했다.
- [ ] targeted EditMode 최소 97개가 전부 PASS다.
- [ ] PlayMode를 실행·생성하지 않았다.
- [ ] Result가 실제 inventory와 필수 섹션을 포함한다.
- [ ] MAP01_05를 시작하지 않았다.

## Completion Rule

Result가 exact `STATUS: PASS`이고 모든 조건이 완료된 경우에만 finalize가:

```text
MAP01_04_IMPLEMENT_HEADER_AND_FIELD_VALIDATION: CURRENT -> COMPLETE
Current Task: TASKS/MAP01_04_IMPLEMENT_HEADER_AND_FIELD_VALIDATION.md -> NONE
```

을 수행한다. MAP01_05를 자동 시작하지 않는다.
