# MAP01_15 — Create CSV Import Window Result

## TASK

`MAP01_15_CREATE_CSV_IMPORT_WINDOW`

## STATUS

`PASS`

## SUMMARY

- fixed Authoring CSV 50개를 one action으로 읽어 schema/header/PK/value/definition/FK/Registry/ContentVersionHash/atomic publish/report write까지 실행하는 Unity Editor pipeline과 window를 완성했다.
- v1.1~v1.3에서 authoritative schema와 typed materialization을 교정했고, v1.4에서 남은 stale `interaction_tags` test expectation 한 줄을 authoritative `ID_LIST` sample로 교정했다.
- compile, focused, targeted, full EditMode, actual reimport, visual, input/meta 보존 조건을 모두 충족했다.

## READ

- entrypoint, locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 이 Task, MAP01_14 Result를 읽었다.
- READ ALLOWLIST 안의 MAP01_02~14 API/tests, approved repair files, fixed inventory, Console 및 Unity resources만 확인했다.
- later Task, Legacy, 비승인 Scene/Prefab YAML은 읽지 않았다.

## MASTER BACKLOG CHECK

- Master task count: `205`.
- 수행 전 상태: `24 COMPLETE / MAP01_15 CURRENT / 180 LOCKED`.
- MAP01_16 이후 구현이나 상태 개방은 수행하지 않았다.

## MAP01_14 GATE CHECK

- `MAP01_14_IMPLEMENT_ATOMIC_PUBLISH_AND_IMPORT_REPORT_RESULT.md`: `PASS`.
- atomic/report `55`, content hash `54`, Registry `47`, FK `54`, targeted `671`, full `691`의 승인 baseline을 보존했다.

## CREATED

Editor production C# 7:

- `Assets/_Game/Map/Editor/WorldGeneration/Data/CsvImportFileStatus.cs`
- `Assets/_Game/Map/Editor/WorldGeneration/Data/CsvImportSessionResult.cs`
- `Assets/_Game/Map/Editor/WorldGeneration/Data/CsvImportReportFileWriter.cs`
- `Assets/_Game/Map/Editor/WorldGeneration/Data/CsvImportNavigation.cs`
- `Assets/_Game/Map/Editor/WorldGeneration/Data/CsvImportPipeline.cs`
- `Assets/_Game/Map/Editor/WorldGeneration/Data/CsvImportWindowState.cs`
- `Assets/_Game/Map/Editor/WorldGeneration/Data/CsvImportWindow.cs`

Editor EditMode test C# 1:

- `Assets/_Game/Tests/EditMode/Map/Editor/WorldGeneration/Data/CsvImportWindowTests.cs`

matching `.cs.meta` 8, report JSON, 이 Result를 생성했다.

## PREEXISTING_IDENTICAL

- 최초 MAP01_15 생성 파일의 preexisting identical reuse: `0`.
- collision: `0`.

## FIXED PATHS

- source: `Assets/_Game/Map/Data/WorldGeneration/Authoring/`
- dictionary: `CSV_DATA_DICTIONARY.csv`
- static sources: exact `49`
- report: `MapDesign/MCP/REPORTS/CsvImportReport.json`
- alternate root, picker, watcher, automatic CSV mutation: 없음.

## PIPELINE

- inventory/BOM/raw SHA-256 → dictionary/catalog → RFC4180/header/PK/value → four definition builders → FK → Registry → ContentVersionHash → atomic publisher → report write 순서다.
- independent issue accumulation과 dependency-stage skip reporting을 유지한다.
- session/file/issue collections은 immutable snapshot이며 run reentry와 phantom running state를 차단한다.

## WINDOW UI

- menu: `Tools/Star Night/Map/CSV Import`
- actions: `Reimport All 50 CSV`, `Open Report`
- publish/version/global hash/report 상태, fixed 50 file rows, issue rows, search 및 ALL/ERROR/WARNING filter를 표시한다.
- source/FK navigation은 CSV를 수정하지 않는다.

## NAVIGATION

- fixed Authoring root의 canonical containment를 검사한다.
- absolute/traversal/injected filename을 거부한다.
- source line 및 FK target line open, select/ping fallback, unavailable reason을 제공한다.

## REPORT WRITE

- strict UTF-8, no BOM, final LF, same-directory temp, flush, atomic replace/move, own-temp cleanup 계약을 유지한다.
- v1.4 적용 전 successful report SHA-256 `a93a0a771fd9e5d1c07094ef6186d87717e7845f5762935fd4127e99237e13b3`를 precondition으로 검증했다.
- final report tuple: schema `1`, ERROR/WARNING `0/0`, `published=true`, previous/current version `0/1`, issues `0`.
- final candidate/current ContentVersionHash: `1c41b14c2734200999e779ad1317c5bc2ef5208da1c3b4bc30347e47182cfeaf` / same.
- final report SHA-256: `106f466079d55dd92998ce4b41f6789f8e93e81737731bdc64ca09f7e5330ff4`. per-attempt ID만 새로 기록됐고 semantic tuple과 ContentVersionHash는 유지됐다.

## TEST

- v1.4 exact focused case: `1/1 PASS`, job `579d9d080b8248ea9b25426803f60772`.
- MicrochunkPopulationItemDefinitionBuilder full: `77/77 PASS`, job `a5fb5902b46942e2ad04610fb0d41f80`.
- targeted Map assembly: `764/764 PASS`, job `98da8e0a114648478744d0d180a0c086`.
- full project EditMode: `784/784 PASS`, job `52c762f5013f45b99ca479e6150a0a3c`.
- v1.3 focused `14/14`, cumulative repair `47/47`, world-route `73/73`, window `48/48`, atomic/hash/Registry/FK `210/210` evidence를 유지한다.
- PlayMode: NOT RUN.

## UNITY

- Unity: `6000.3.8f1`
- instance: `Constant@ced6e0dfc4a31d45`
- force script refresh/domain reload: PASS
- compile error / relevant warning: `0 / 0`
- final Console error / warning: `0 / 0`
- Scene/Prefab changes: NONE

## VISUAL

- `CSV Import` window open/focused: PASS
- size: `1040 x 680`
- live state: stage `COMPLETE`, progress `1`, files `50`, issues `0`, published, version `1`.
- Reimport/Open Report actions enabled after completion; IMGUI exception `0`.

## ASSET META VALIDATION

- Authoring CSV: `50`, manifest SHA-256 `f0cf35a9bcc63bb1cb2c24ac6416386f8725d4d1a7ac0549f18a8cdaafe31e9e`, unchanged.
- Authoring CSV meta: `50`, manifest SHA-256 `c7f6876120f27b4aebd85db69360479afedc2e128fff49ed8d3ae960b9da728a`, unchanged.
- final global Assets meta: `2933`; duplicate GUID group: `0`.
- Unity가 재생성한 비승인 folder meta 6개는 제거했다.

## CHANGE SCOPE

- v1.4 exact changed file:
  - `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/MicrochunkPopulationItemDefinitionBuilderTests.cs`
  - before SHA-256 `3c49ad5417b429401af15ca84316bee7dcbbe4ec7897a5b9bda0219768d00276`
  - after SHA-256 `7bf4d6d9b31ec78952be7020d45824b1bdab5c11a4176671cc2c72cdb2e035fb`
- one-line change:
  - before `ENUM_A|ENUM_B|ENUM_A`
  - after `LIST_A|LIST_B|LIST_A`
- in-memory reverse reconstruction이 before SHA와 exact 일치하여 다른 test content 변경이 없음을 검증했다.
- production C#, 다른 test, dictionary, CSV/meta, asmdef, Scene/Prefab 변경은 없다.

## OUT_OF_SCOPE_FINDINGS

- 없음.

## REMEDIATION v1.1

- authoritative builder type 18개와 empty ENUM/ENUM_LIST vocabulary 일반 규칙을 교정했다.
- CSV/dictionary를 수정하거나 issue를 숨기지 않았다.

## REMEDIATION v1.2

- Special Map 세 BOOL property/materialization을 `bool` / `Bool` accessor로 교정했다.

## REMEDIATION v1.3

- World Route five-row schema와 필요한 FLOAT/list materialization을 교정했다.
- actual 50 CSV publish와 visual state를 승인했다.

## REMEDIATION v1.4

- patch: `MAP01_15_REPAIR_STALE_INTERACTION_TAGS_TEST`, payload SHA-256 `85d6ea7fb80b5ada3500c7e18bff356d4ba70380e05601aaa89eb17846390b96`.
- marker: `MapDesign/MCP_INBOX/MAP01_15_REPAIR_STALE_INTERACTION_TAGS_TEST/.APPLIED`.
- `CoreContractCase(13)`의 `interaction_tags` stale expected sample만 authoritative `ID_LIST` 값으로 교정했다.
- fixture, production materialization, generic helper, 다른 ENUM_LIST expectation은 변경하지 않았다.
- focused, class, targeted, full EditMode가 모두 통과했다.

## DONE CONDITIONS

- [x] exact fixed 50 inventory and immutable snapshots
- [x] complete one-action pipeline and atomic report persistence
- [x] window/search/filter/navigation/visual state
- [x] authoritative schema/materialization repairs
- [x] v1.4 exact one-line stale test correction
- [x] focused `1/1`, Microchunk `77/77`
- [x] targeted `764/764`, full EditMode `784/784`
- [x] compile/Console error and relevant warning `0/0`
- [x] actual report published, version `1`, content hashes non-null/equal
- [x] CSV/meta unchanged, meta count `2933`, duplicate GUID `0`
- [x] PlayMode not run, Scene/Prefab changes NONE

## NEXT

- STATUS FINALIZE에서 MAP01_15를 COMPLETE, Current Task를 NONE으로 변경한다.
- MAP01_16은 LOCKED로 유지한다.
- 다음 Task는 자동 시작하지 않는다.

## Recommended Commit

`feat(map): add csv import editor window`
