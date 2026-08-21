# MAP01_13_IMPLEMENT_CONTENT_VERSION_HASH RESULT

## TASK

`MAP01_13_IMPLEMENT_CONTENT_VERSION_HASH`

## STATUS

`PASS`

## SUMMARY

- successful immutable `StaticDataRegistry`, exact matching `ForeignKeySourceSet`, exact `CsvSchemaCatalog` identity를 입력으로 받는 deterministic SHA-256 content hash를 구현했다.
- exact 49 static source의 typed semantic content만 canonical binary stream v1에 기록한다.
- file 제공 순서, CSV row 순서, record number, BOM, line ending, raw quoting에는 독립적이며 schema와 모든 semantic field/list 변화에는 민감하다.
- input/canonical gate error가 하나라도 있으면 hash와 canonical payload를 게시하지 않는다.

## READ

- `00_MCP_ENTRYPOINT.md`, locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 이 Task, MAP01_12 Result를 확인했다.
- READ ALLOWLIST 안에서 MAP01_02~12 production API/direct tests, importer, architecture fixtures, asmdef 4, Runtime Data/test 직계 inventory, CSV/meta hash/BOM, meta GUID, Unity Console만 확인했다.
- Authoring CSV data row, later Task, Legacy, 비승인 C#, Scene/Prefab YAML은 읽지 않았다.

## MASTER BACKLOG CHECK

- Master exact task count: `205`
- 실행 시점 상태: MAP00_01~MAP01_12 COMPLETE, MAP01_13 CURRENT, MAP01_14 이후 LOCKED
- MAP01_14 task/result/code는 만들거나 실행하지 않았다.

## MAP01_12 GATE CHECK

- `MAP01_12_IMPLEMENT_STATIC_DATA_REGISTRY_RESULT.md`: `STATUS: PASS`
- Registry focused cases `47`, targeted `562/562`, full EditMode `582/582`, compile error/relevant warning `0/0`을 확인했다.
- Authoring CSV/meta `50/50`, 기존 production/test/asmdef 보존 evidence를 확인했다.

## CREATED

Runtime C# 5:

- `Assets/_Game/Map/Runtime/WorldGeneration/Data/ContentVersionHash.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Data/ContentHashCanonicalWriter.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Data/ContentVersionHashError.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Data/ContentVersionHashResult.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Data/ContentVersionHashCalculator.cs`

Focused test C# 1:

- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/ContentVersionHashCalculatorTests.cs`

Unity-generated `.cs.meta` 6과 이 Result를 생성했다.

## PREEXISTING_IDENTICAL

- WRITE ALLOWLIST의 Runtime/test C# 6개 및 해당 `.meta` 6개는 실행 전 모두 없었다.
- preexisting identical reuse: `0`
- collision: `0`

## INPUT GATE

- non-null successful Registry, non-null exact 49-source set, source-set과 exact same catalog instance를 요구한다.
- missing/unexpected/duplicate source, catalog/schema instance 불일치, unsuccessful parse, schema column inventory/order/type 불일치, file-record identity 불일치와 중복을 누적 검증한다.
- minimum error codes `MissingRegistry`, `MissingSourceSet`, `CatalogMismatch`, `SourceInventoryMismatch`, `RecordIdentityMismatch`, `SchemaMismatch`, `UnsupportedValue`, `DuplicateCanonicalPrimaryKey`를 구현했다.
- error는 file/record/field/source location을 보존하고 deterministic sort한다.
- gate failure result는 `Hash == null`, errors `> 0`이며 partial digest/canonical payload를 노출하지 않는다.

## CANONICAL STREAM V1

- magic/version: exact ASCII `STARNIGHT_STATIC_DATA_CONTENT_V1`, 동일 length-prefix primitive 사용
- 모든 문자열/variable payload: unsigned 64-bit big-endian byte length + strict UTF-8 bytes
- 모든 구조 count: unsigned 64-bit big-endian
- file: filename ordinal sort 후 filename, column schema, record count 기록
- record: canonical PK tuple ordinal sort; record number/location/row order 미기록
- field: column name, exact schema type token, canonical typed value를 schema order로 기록
- STRING/ID/ENUM/HEX는 exact UTF-8 string, INT/ULONG은 invariant decimal, FLOAT는 invariant `R`과 negative-zero `0`, BOOL은 `0`/`1`, DATETIME은 canonical UTC fixed fractional form으로 기록한다.
- list는 original parsed order와 duplicate를 유지하고 item count로 empty list와 one-item list를 구분한다.
- exact 49 static files만 기록하며 11 generated catalog schemas는 제외됨을 검증했다.

## HASH CONTRACT

- exact SHA-256 digest `32` bytes, lowercase hex `64` characters
- digest bytes는 private copy이며 read-only view와 defensive `ToByteArray()` copy를 제공한다.
- equality, operators, hash code, string representation은 digest value 기반이다.
- regression vector: `5cb9e42a22ad4cf89190c3b106c34db5bea420d7c0c5ebbeeff4b3bb9a4a4cdb`

## TEST

- 신규 focused test cases: `54` (`>= 32`)
- Map EditMode assembly targeted: `616/616 PASS`, failed `0`, skipped `0` (`>= 594`)
- full project EditMode: `636/636 PASS`, failed `0`, skipped `0` (`>= 614`)
- final targeted job: `a6cda8bc25da4b739993166c3421a6fe`
- final full EditMode job: `53ddd1e88f6a41c1b4bf40236ae5baee`
- PlayMode: 실행하지 않음

## UNITY

- Unity: `6000.3.8f1`
- active instance: `Constant@ced6e0dfc4a31d45`
- force Asset refresh/domain reload: PASS
- compile error: `0`
- relevant new warning: `0`
- Test Runner infrastructure warning 3건은 production/test code warning이 아니며, console clear 후 error/warning `0`을 재확인했다.
- Scene/Prefab changes: `NONE`

## ASSET META VALIDATION

- 신규 `.cs.meta`: `6/6`
- 신규 GUID 형식/고유성: `6/6`, 신규 중복 `0`
- global Assets meta: `2917`, parsed GUID `2917`, duplicate GUID group `0`
- Authoring CSV: `50`, fingerprint before/after `F5D9DBE84050D8807BBDF5E4E85A46D29294A7EEC8A06F5EE84245942E67B174`, UTF-8 BOM `50/50`
- Authoring CSV meta: `50`, fingerprint before/after `4A717451008C39300A2E235AB6EFF65CAD718D1AF8EFD16C61AC26DA9AB9BA70`
- Runtime/Editor/EditMode asmdef 4: fingerprint before/after `7E3B3E34828C2FCE1BF40169B59C675180B8E20A85104DA7D95A7570FDACB369`

## CHANGE SCOPE

- 실행 전 기존 direct Runtime Data/test C#: `89`, fingerprint `6C35C8A9355FC5152D8091AA58081C00DFCDABD5DE47AAF7510B7F126DEF7094`
- 실행 후 신규 6개 제외 동일 C#: `89`, 동일 fingerprint `6C35C8A9355FC5152D8091AA58081C00DFCDABD5DE47AAF7510B7F126DEF7094`
- 기존 loader/definitions/FK/Registry/tests/CSV/asmdef 변경: `0`
- Runtime 5 + test 1 + meta 6 + Result만 생성했다.
- Scene, Prefab, Package, ProjectSettings 변경: `0`

## OUT_OF_SCOPE_FINDINGS

- 없음.
- singleton/global install, atomic swap/publish/rollback, import report/window, domain validation, salt/HMAC/randomness는 구현하지 않았다.

## DONE CONDITIONS

- [x] exact Registry + 49-source set + catalog identity input gate
- [x] exact source/schema/parsed record/FK index identity 검증
- [x] canonical stream v1 magic, U64 big-endian length/count, ordinal file/PK ordering
- [x] typed scalar/list canonicalization, duplicates/order/empty distinction
- [x] file/row order independence 및 semantic/schema sensitivity
- [x] NaN/Infinity/invalid UTF-8/canonical PK duplicate failure with no digest
- [x] immutable 32-byte/64-hex value hash와 value equality
- [x] focused `54`, targeted `616/616`, full EditMode `636/636`
- [x] compile error/relevant warning `0/0`, final console `0`
- [x] Runtime 5 + test 1 + meta 6 only; existing C#/CSV/asmdef unchanged
- [x] new meta 6 valid; GUID duplicate 0
- [x] PlayMode not run; Scene/Prefab changes NONE

## NEXT

- STATUS FINALIZE 후 Current Task를 `NONE`으로 만든다.
- `MAP01_14_IMPLEMENT_ATOMIC_PUBLISH_AND_IMPORT_REPORT`는 `LOCKED`로 유지하며 자동 시작하지 않는다.

## Recommended Commit

`feat(map): add deterministic content version hash`
