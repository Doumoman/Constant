# MAP01_05 — Implement Primary Key Index

```yaml
status_control:
  task_key: MAP01_05_IMPLEMENT_PRIMARY_KEY_INDEX
  result_file: REPORTS/MAP01_05_IMPLEMENT_PRIMARY_KEY_INDEX_RESULT.md
```

## TASK TYPE

```text
RUNTIME CSV PRIMARY-KEY COLLECTION + EDITMODE TESTS
```

## Objective

MAP01_04에서 성공한 validated record의 `EffectiveValue`를 사용해 파일별 단일·복합 primary key를 1차 수집하고 immutable lookup index를 만든다.

키 비교는 component별 exact ordinal, case-sensitive raw-string 의미를 유지한다. 중복 키가 있으면 첫 행과 후속 행을 포함한 모든 occurrence의 file/record/field/physical line/column/offset을 한 duplicate group으로 보고하고 usable index를 publish하지 않는다.

이 TASK는 PK 수집만 수행한다. typed scalar/list parsing, FK resolution, domain definition, Registry publish는 구현하지 않는다.

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
12. `REPORTS/MAP01_04_IMPLEMENT_HEADER_AND_FIELD_VALIDATION_RESULT.md`

## READ ALLOWLIST

본문 읽기 허용:

- Mandatory Read Order의 파일
- MAP01_02 Runtime schema production C# 8개
- MAP01_03 Runtime reader production C# 7개
- MAP01_04 Runtime header/field validation production C# 6개
- `CsvSchemaCatalogTests.cs`
- `Rfc4180CsvReaderTests.cs`
- `CsvHeaderAndFieldValidatorTests.cs`
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
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvPrimaryKey.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvPrimaryKeyOccurrence.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvDuplicatePrimaryKey.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvPrimaryKeyIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvPrimaryKeyIndexBuildResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvPrimaryKeyIndexBuilder.cs
```

### 신규 Runtime EditMode test — 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/CsvPrimaryKeyIndexBuilderTests.cs
```

신규 C# 7개와 대응 `.cs.meta` 7개만 Asset 변경으로 허용한다. 기존 C#과 `.meta`는 수정하지 않는다.

추가 생성 허용:

```text
MapDesign/MCP/REPORTS/MAP01_05_IMPLEMENT_PRIMARY_KEY_INDEX_RESULT.md
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

`CsvPrimaryKeyIndexBuilder`는 정확히 한 `CsvFileSchema`와 그 파일의 successful `CsvHeaderFieldValidationResult`를 입력받는 reusable file pass다. 전체 import caller가 이 pass를 모든 입력 파일에 반복 적용한다.

- validation result가 `Success == true`이고 `Errors.Count == 0`이어야 한다.
- validated record/field 수와 schema mapping은 MAP01_04 계약과 일치해야 한다.
- PK 열은 `CsvFileSchema.PrimaryKeyColumns`의 `PrimaryKeyOrder` 순서다.
- null 입력은 `ArgumentNullException`, unsuccessful validation과 schema/field 불일치 입력은 `InvalidOperationException`으로 partial index 없이 명시적으로 거부한다.
- header-only successful file은 entry 0개의 successful index다.
- 입력 schema, validation result, records, fields를 수정하지 않는다.

## Key Contract

`CsvPrimaryKey`는 PK component 문자열의 immutable structural value object다.

1. component는 각 PK field의 `EffectiveValue`를 그대로 사용한다.
2. component sequence는 `PrimaryKeyOrder` 순서다.
3. 각 component 비교와 hash는 exact ordinal, case-sensitive다.
4. trim, Unicode normalization, case folding, numeric/enum/hex/bool conversion을 하지 않는다.
5. `"01"`과 `"1"`, `"A"`와 `"a"`는 서로 다른 키다.
6. composite key를 delimiter join 문자열로 만들지 않는다. component vector를 구조적으로 비교해 delimiter collision을 원천 차단한다.
7. component collection과 hash/equality 결과는 생성 후 변하지 않는다.

MAP01_02에서 PK 열은 required이고 MAP01_04가 empty/default를 해결했다. 방어적으로 empty effective PK component 또는 PK column 0개를 받으면 `InvalidOperationException`으로 거부하며 index를 만들지 않는다.

## Occurrence / Duplicate Contract

`CsvPrimaryKeyOccurrence`는 최소 다음 immutable context를 보존한다.

```text
SourceName
SchemaFileName
Key
RecordNumber
PhysicalLine
PhysicalColumn
CharOffset
SourceRecord
PrimaryKeyFields (PrimaryKeyOrder, read-only)
```

대표 위치는 첫 PK component source field의 start location이며, `PrimaryKeyFields`로 복합키의 모든 component 위치를 확인할 수 있어야 한다. 위치 없는 임의 `-1`은 사용하지 않는다.

`CsvDuplicatePrimaryKey`는 다음을 보존한다.

```text
SchemaFileName
Key
Occurrences (read-only, count >= 2)
```

- 하나의 duplicate key가 2회면 정확히 두 occurrence, 3회 이상이면 첫 행을 포함한 모든 occurrence를 한 group으로 반환한다.
- 서로 다른 duplicate key는 각각 별도 group이다.
- occurrence는 source record/location 오름차순, duplicate group은 key component의 ordinal lexicographic 순서로 고정한다.
- 메시지용 문자열을 만들더라도 identity/equality에는 사용하지 않는다.
- 입력 row enumeration 순서를 섞어도 같은 key membership과 deterministic group ordering을 얻어야 한다.

## Index / Result Contract

`CsvPrimaryKeyIndex`:

- 정확히 한 schema file의 unique key → occurrence lookup이다.
- `TryGet`과 read-only deterministic enumeration을 제공한다.
- key enumeration은 component ordinal lexicographic 순서다.
- 외부에서 dictionary, key component, occurrence collection을 수정할 수 없다.

`CsvPrimaryKeyIndexBuildResult`:

- success면 duplicate 0개, non-null index, entry count = validated record count다.
- duplicate가 하나라도 있으면 `Success == false`, usable index를 publish하지 않고 모든 duplicate group을 반환한다.
- 실패 시 먼저 발견한 일부 entry를 외부에 노출하지 않는다.
- 동일 입력은 실행마다 동일 result/order를 만든다.

## DO NOT

- RFC4180 reader, schema catalog/model/builder, header/field validator/model 수정 금지
- dictionary importer 수정 금지
- raw/effective string trim·normalize·case-fold 금지
- composite key delimiter concatenation 금지
- int/ulong/float/bool/hex/enum/list parse 금지
- FK target resolve 금지
- domain definition/StaticDataRegistry/hash/report/window 구현 금지
- CSV/ScriptableObject/asset 수정·생성 금지
- 기존 C#/test 수정 금지
- asmdef/Scene/Prefab/Package/ProjectSettings 변경 금지
- 외부 dependency 설치 금지
- Git operation 금지
- MAP01_06 선행 작업 금지

## Collision Handling

1. 신규 파일이 없으면 생성한다.
2. 동일 경로가 이미 있고 exact 계약과 바이트 동일하면 `PREEXISTING_IDENTICAL`로 기록한다.
3. 다르면 덮어쓰기·병합하지 않고 `BLOCKED`다.
4. 기존 `.meta` GUID를 보존한다.
5. 기존 사용자 변경을 되돌리지 않는다.

## Implementation Steps

1. Current Task MAP01_05, Master 205개, MAP01_04 COMPLETE/PASS를 확인한다.
2. MAP01_04 Result의 validator 29/29, reader 31/31, schema 23/23, importer 9/9, architecture 10/10, total 102/102, compile/warning 0/0을 확인한다.
3. 작업 전 C#/CSV/meta inventory와 preservation hash를 기록한다.
4. allowlisted schema/reader/validator/test/asmdef만 읽어 실제 API에 맞춘다.
5. structural key, occurrence, duplicate, immutable index/result를 구현한다.
6. single/composite PK 수집과 all-occurrence duplicate grouping builder를 구현한다.
7. 최소 24개 primary-key unit test를 구현한다.
8. Unity refresh/compile 후 신규 PK tests와 기존 validator/reader/schema/importer/architecture fixtures를 실행한다.
9. 신규 meta 7개의 GUID와 프로젝트 중복을 확인한다.
10. 기존 CSV/meta와 MAP01_02/03/04 C#의 비허용 변경이 0인지 확인한다.
11. Result를 작성하고 모든 조건이 충족될 때만 PASS를 기록한다.

## Required Tests

`CsvPrimaryKeyIndexBuilderTests` 최소 24 case:

- single PK success / lookup hit / lookup miss
- composite PK success / `PrimaryKeyOrder` component order
- exact ordinal case sensitivity
- `"01"` and `"1"` remain distinct
- whitespace remains significant
- effective default value is indexed unchanged
- non-empty raw effective value wins over schema default
- delimiter-like characters cannot collide between component vectors
- empty successful file creates empty index
- two-row duplicate reports both occurrences
- three-row duplicate reports all three occurrences in one group
- multiple duplicate keys create separate groups
- duplicate includes first and later source record/line/column/offset
- composite duplicate exposes every PK component source field
- duplicate result publishes no usable index
- all duplicate groups are collected, not first-error only
- shuffled input enumeration preserves key membership and stable duplicate ordering
- index enumeration is component-ordinal deterministic
- null/unsuccessful validation input is rejected without partial index
- schema/validated-field mismatch is rejected
- no-PK schema and empty effective PK component are rejected
- key component collection is immutable
- index/occurrence/duplicate collections are immutable
- input schema/validation/records/fields remain unchanged

Targeted regression:

```text
New primary-key index: >=24 / ALL PASS
Header/field validator: 29/29 PASS
RFC4180 reader: 31/31 PASS
Schema catalog: 23/23 PASS
Dictionary importer: 9/9 PASS
Architecture: 10/10 PASS
Targeted total: >=126 / ALL PASS
```

## Unity Verification

```text
Unity Version: 6000.3.8f1
Asset Refresh: PASS
Compile Errors: 0
Relevant New Warnings: 0
Targeted EditMode: >=126 / ALL PASS
PlayMode: NOT RUN
Scene/Prefab Changes: NONE
```

Unity compile/test 증거가 없으면 `BLOCKED`다.

## Result File

```text
REPORTS/MAP01_05_IMPLEMENT_PRIMARY_KEY_INDEX_RESULT.md
```

필수 섹션:

```text
TASK
STATUS
SUMMARY
READ
MASTER BACKLOG CHECK
MAP01_04 GATE CHECK
CREATED
PREEXISTING_IDENTICAL
KEY CONTRACTS IMPLEMENTED
DUPLICATE CONTRACTS IMPLEMENTED
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

- [ ] Current Task가 MAP01_05다.
- [ ] Master 205개와 MAP01_04 COMPLETE/PASS를 확인했다.
- [ ] 파일별 single/composite PK를 `PrimaryKeyOrder`로 수집한다.
- [ ] `EffectiveValue`를 trim/normalize/typed parse 없이 사용한다.
- [ ] key equality/hash가 component별 exact ordinal이다.
- [ ] composite key identity가 delimiter concatenation에 의존하지 않는다.
- [ ] duplicate 2행의 양쪽 위치를 모두 보고한다.
- [ ] 3회 이상 duplicate의 첫 행과 모든 후속 행을 한 group으로 보고한다.
- [ ] 모든 duplicate group을 deterministic order로 반환한다.
- [ ] duplicate가 하나라도 있으면 usable/partial index를 publish하지 않는다.
- [ ] index/key/occurrence/duplicate/result가 immutable/read-only다.
- [ ] invalid validation/schema input을 partial publish 없이 거부한다.
- [ ] 신규 Runtime C# 6개와 test 1개만 생성했다.
- [ ] 신규 meta 7개가 유효하고 GUID 중복이 없다.
- [ ] reader/schema/validator/importer/기존 tests를 수정하지 않았다.
- [ ] CSV 50개/meta 50개를 수정하지 않았다.
- [ ] typed parser/FK/Registry 등 MAP01_06 이후 기능을 구현하지 않았다.
- [ ] asmdef/asset/Scene/Prefab/Package/ProjectSettings 변경이 없다.
- [ ] Unity refresh, compile 0, warning 0을 확인했다.
- [ ] targeted EditMode 최소 126개가 전부 PASS다.
- [ ] PlayMode를 실행·생성하지 않았다.
- [ ] Result가 실제 inventory와 필수 섹션을 포함한다.
- [ ] MAP01_06을 시작하지 않았다.

## Completion Rule

Result가 exact `STATUS: PASS`이고 모든 조건이 완료된 경우에만 finalize가:

1. MAP01_05를 `CURRENT -> COMPLETE`로 바꾼다.
2. Last Completed/Last Result를 MAP01_05로 갱신한다.
3. Current Task를 `NONE`으로 만든다.
4. MAP01_06 이후는 모두 `LOCKED`로 유지한다.
5. `MAP01_06_IMPLEMENT_SCALAR_AND_LIST_PARSERS`를 자동 생성·실행하지 않는다.

FAIL/BLOCKED이면 MAP01_05 CURRENT를 유지하고 다음 Task로 진행하지 않는다.

## Recommended Commit

```text
feat(map): build immutable CSV primary key indexes
```
