# MAP01_16 — Create CSV Failure Fixtures and Tests Result

## TASK

`MAP01_16_CREATE_CSV_FAILURE_FIXTURES_AND_TESTS`

## STATUS

`PASS`

## SUMMARY

- fixed Authoring CSV 50개를 test-owned OS temp root로 byte copy하는 deterministic fixture factory와 37개 focused EditMode test를 설치했다.
- 10개 mandatory mutation, exact diagnostic tuple, atomic publish 거부 시 이전 Registry/version/hash identity 보존, row-order semantic hash 불변, recovery/session isolation을 검증했다.
- production Runtime/Editor C#, 기존 test, asmdef, actual CSV/meta, Scene/Prefab은 변경하지 않았다.

## READ

- `00_MCP_ENTRYPOINT.md`, locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 Task를 읽었다.
- MAP01_15 Result와 MAP01_02~15의 필요한 public CSV/schema/parser/PK/definition/FK/registry/hash/publisher API 및 해당 tests만 읽었다.
- actual Authoring에서는 exact inventory/hash 검증에 필요한 파일명과 fixture 대상 최소 row만 읽었다.
- later Task, Legacy, Scene/Prefab YAML, Package, ProjectSettings는 읽지 않았다.

## MASTER BACKLOG CHECK

- Master task count: `205`.
- 실행 전 상태: `25 COMPLETE / MAP01_16 CURRENT / 179 LOCKED`.
- MAP01_17 이후 Task 구현이나 상태 개방은 수행하지 않았다.

## MAP01_15 GATE CHECK

- `MAP01_15_CREATE_CSV_IMPORT_WINDOW_RESULT.md`: `PASS`.
- baseline: window `48/48`, targeted `764/764`, full EditMode `784/784`, compile/relevant warning `0/0`.
- actual Authoring CSV/meta exact `50/50`, CSV BOM `50/50`을 확인했다.

## CREATED

- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/CsvFailureFixtureFactory.cs`
  - SHA-256 `ce5b1a4e62e1b4d6c7e7855f023cb10edf4d3315be83aa5c9360f395f8dec733`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/CsvFailureFixtureTests.cs`
  - SHA-256 `a362a37406a751bfde5a32dcd0a82cd231fe3d77c4a4033b84bc6489075ff743`
- matching `.cs.meta`: `2`, fixed GUID `b4f6a9138f2d4f9e8db7d930b617a9d2`, `93c3489e7f35472aad82036d92177264`.
- preexisting identical reuse: `0`; collision: `0`.

## FIXTURE FACTORY

- each fixture는 OS temp 아래 unique owned root를 만들고 exact expected 50 filenames만 flat byte copy한다.
- canonical containment와 filename allowlist를 함께 검사하며 traversal, absolute path, unknown filename을 거부한다.
- mutation은 ordinal-stable target selection만 사용하고 filename/column/record/source line/before/after 및 before/after SHA-256을 immutable descriptor로 남긴다.
- suite는 `ParallelScope.None`으로 격리하고 각 test가 독립 `StaticDataRegistryStore`를 사용한다.
- `Dispose`는 factory 소유 root만 지우며 최종 잔류 temp root는 `0`이다.
- compound 검증은 upstream-invalid 파일만 pristine read-only Authoring parse로 보완해 서로 다른 파일의 FK 진단까지 누적한다. 이 동작은 test support에만 있으며 production hook이나 conditional bypass는 없다.

## FIXTURE MATRIX

- `DUPLICATE_PRIMARY_KEY`: data row를 같은 파일에 복제한다.
- `INVALID_ENUM_TOKEN`: allowed enum field를 `UNKNOWN_ENUM_TOKEN`으로 바꾼다.
- `INVALID_INT`: INT field를 `NOT_AN_INTEGER`로 바꾼다.
- `INVALID_FLOAT`: FLOAT field를 `NOT_A_FLOAT`로 바꾼다.
- `MISSING_SINGLE_FOREIGN_KEY`: required single FK를 missing identity로 바꾼다.
- `MISSING_LIST_FOREIGN_KEY`: required ID_LIST의 첫 항목을 missing identity로 바꾼다.
- `MISSING_UTF8_BOM`: exact leading `EF BB BF` 3 bytes만 제거한다.
- `ROW_ORDER_REVERSED`: header는 유지하고 data rows만 역순으로 바꾼다.
- `HEADER_ORDER_CHANGED`: 두 header column만 교환한다.
- `COMPOUND_INDEPENDENT_FAILURES`: 서로 다른 4개 파일에 duplicate/enum/int/FK mutation을 함께 적용한다.

## DIAGNOSTIC ASSERTIONS

- duplicate: `PRIMARY_KEYS/DUPLICATE_PRIMARY_KEY/<source>`.
- enum/int/float: `VALUE_PARSE/InvalidEnum|InvalidInteger|InvalidFloat/<source>`.
- single/list FK: `FOREIGN_KEYS/MissingTargetRecord/<source>`.
- BOM: `READ/MISSING_UTF8_BOM/<source>`.
- header: `HEADER_FIELDS/HeaderOrderMismatch/<source>`.
- 각 진단은 source line `>=1`, report error count와 ERROR issue count 일치, deterministic ordering을 검증했다.
- compound는 네 family를 모두 누적하고 두 unique fixture 실행의 serialized report bytes가 동일함을 검증했다.

## REGISTRY PRESERVATION

- duplicate/enum/int/float/single FK/list FK/BOM/header/compound마다 valid baseline을 먼저 publish했다.
- 각 rejection 뒤 `Store.Current`, Registry instance, ContentHash instance, Version이 baseline과 exact same임을 검증했다.
- invalid candidate는 `published=false`이며 Store에 반영되지 않는다.

## HASH/ORDERING

- `ROW_ORDER_REVERSED`는 mutated file raw SHA-256이 baseline과 달라진다.
- 같은 fixture는 publish에 성공하고 semantic `ContentVersionHash`는 valid baseline과 exact 동일하다.
- mutation descriptor와 issue/report ordering은 ordinal deterministic이다.

## RECOVERY

- seeded valid publish 다음 invalid fixture가 publish되지 않고 previous snapshot을 보존한다.
- 이어서 새 valid fixture를 실행하면 version이 정상 증가하고 publish가 회복된다.
- 이전 issue/report/session state와 fixture temp root가 다음 실행에 누출되지 않는다.

## REMEDIATION v1.1

- authority는 actual Authoring CSV `50/50` 모두 leading UTF-8 BOM을 가진다는 production 계약이다.
- repair v1.1.1에 따라 이전 반대 방향 요구를 폐기하고 temp copy에서만 첫 `EF BB BF`를 제거했다.
- 나머지 bytes는 exact 보존되고 `HadUtf8Bom=false`, `READ/MISSING_UTF8_BOM`, `published=false`, previous Registry/version/hash identity 보존을 검증했다.
- production RFC4180 reader/pipeline/error code와 actual Authoring CSV/meta는 변경하지 않았다.

## TEST

- MAP01_16 focused: `37/37 PASS`.
- CSV import window: `48/48 PASS`.
- Microchunk population/item definition: `77/77 PASS`.
- World route definition: `73/73 PASS`.
- atomic/hash/registry/FK regression: `210/210 PASS`.
- targeted `Game.Map.Tests.EditMode`: `801/801 PASS`.
- full project EditMode: `821/821 PASS`.
- PlayMode: `NOT RUN`.

## UNITY

- Unity: `6000.3.8f1`.
- instance: `Constant@ced6e0dfc4a31d45`.
- final script compile error / project-relevant warning: `0 / 0`.
- MCP transport 자체 WebSocket warning은 project warning에서 제외했다.
- Scene/Prefab changes: `NONE`.

## ASSET META VALIDATION

- Authoring CSV: `50`, UTF-8 BOM `50`, before/after manifest SHA-256 `4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3`, modified `0`.
- Authoring CSV meta: `50`, before/after manifest SHA-256 `a3cebbdd83484bea161983320bd5b3a1756f6c82de774f1dc0508d327c85c291`, modified `0`.
- manifest는 root-relative path + `|` + per-file SHA-256을 ordinal sort하고 LF join한 UTF-8 bytes의 SHA-256이다.
- final Assets meta: `2935` = baseline `2933` + approved new meta `2`.
- duplicate GUID group: `0`; Unity 자동 생성 비승인 folder meta 잔류: `0`.

## CHANGE SCOPE

- Phase A repair 적용: corrected current Task payload와 repair `.APPLIED` marker.
- Task 산출물: 신규 test support C# `1`, test C# `1`, matching meta `2`, 이 Result `1`.
- production C#, 기존 tests, asmdef, actual CSV/meta, report JSON, Scene/Prefab 변경: `0`.
- Unity refresh가 자동 생성한 비승인 folder meta 6개는 제거해 pre-task 상태로 복원했다.

## OUT_OF_SCOPE_FINDINGS

- `NONE`.

## DONE CONDITIONS

- [x] test-owned exact-50 byte-copy fixture factory와 canonical containment
- [x] mandatory deterministic mutation `10`
- [x] exact diagnostics, navigation line, report determinism
- [x] Registry/version/hash identity preservation과 atomic rejection
- [x] row-order raw hash 변화 및 semantic hash 불변
- [x] recovery/session isolation, owned temp cleanup
- [x] focused `37 >= 20`
- [x] targeted `801 >= 784`, full EditMode `821 >= 804`
- [x] compile error / project-relevant warning `0/0`
- [x] actual Authoring CSV/meta `50/50` byte/hash unchanged
- [x] production/existing test/asmdef/Scene/Prefab changes `0`
- [x] MAP01_17 자동 시작 없음

## NEXT

- 이 Result를 근거로 MAP01_16만 COMPLETE로 finalize한다.
- Current Task는 `NONE`, MAP01_17은 `LOCKED`로 유지한다.
- 다음 Task를 자동 시작하지 않는다.

## Recommended Commit

`test(map): add deterministic CSV failure fixtures and atomic publish regressions`
