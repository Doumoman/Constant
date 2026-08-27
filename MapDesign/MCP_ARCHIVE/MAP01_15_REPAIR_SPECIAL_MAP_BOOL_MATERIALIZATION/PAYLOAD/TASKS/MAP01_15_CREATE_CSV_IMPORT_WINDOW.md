# MAP01_15 — Create CSV Import Window

```yaml
status_control:
  task_key: MAP01_15_CREATE_CSV_IMPORT_WINDOW
  result_file: REPORTS/MAP01_15_CREATE_CSV_IMPORT_WINDOW_RESULT.md
```

## Objective

Unity Editor에서 fixed Authoring CSV 50개 전체를 one action으로 재임포트하고, MAP01_02~14 pipeline을 순서대로 실행한다. 파일별 row/error/diagnostic hash, 전역 ContentVersionHash, publish/version/report 결과를 표시하고 source error/FK target으로 이동한다.

## Mandatory Read / Allowlist

entrypoint → locked/work/CSV/Unity/change/patch/finalize rules → Master → Status → 이 Task → MAP01_14 Result. MAP01_02~14 production API/tests, importer, architecture fixtures, asmdef 4, fixed Authoring inventory, EditorWindow patterns, inventory/hash/BOM/meta/Console, WRITE ALLOWLIST만 읽는다. CSV data row는 pipeline 실행을 통해서만 읽고 수동 도메인 분석을 하지 마. later Task/Legacy/비승인 C#/Scene-Prefab YAML 금지.

## WRITE ALLOWLIST

Editor production C# 7:

```text
Assets/_Game/Map/Editor/WorldGeneration/Data/CsvImportFileStatus.cs
Assets/_Game/Map/Editor/WorldGeneration/Data/CsvImportSessionResult.cs
Assets/_Game/Map/Editor/WorldGeneration/Data/CsvImportReportFileWriter.cs
Assets/_Game/Map/Editor/WorldGeneration/Data/CsvImportNavigation.cs
Assets/_Game/Map/Editor/WorldGeneration/Data/CsvImportPipeline.cs
Assets/_Game/Map/Editor/WorldGeneration/Data/CsvImportWindowState.cs
Assets/_Game/Map/Editor/WorldGeneration/Data/CsvImportWindow.cs
```

Editor EditMode test 1:

```text
Assets/_Game/Tests/EditMode/Map/Editor/WorldGeneration/Data/CsvImportWindowTests.cs
```

신규 C# 8 + `.cs.meta` 8 + Result 1만 허용. `MapAuthoring.Editor` / `MapAuthoring.Tests.EditMode`, namespace `StarNight.Map.Editor.WorldGeneration.Data` / tests matching. Runtime C#/asmdef 수정 금지.

## Fixed Paths / Inventory

- source root: exact `Assets/_Game/Map/Data/WorldGeneration/Authoring/`; do not add folder picker or alternate roots.
- dictionary: exact `CSV_DATA_DICTIONARY.csv`; static data: exact 49 files; generated files excluded.
- report output: project-relative `MapDesign/MCP/REPORTS/CsvImportReport.json` using strict UTF-8 bytes from MAP01_14.
- report write uses temp file in same directory then atomic replace/move; failure becomes UI/report issue and must not roll back a successfully published Registry. Report persistence status is shown separately.
- CSV/meta are read-only; no automatic rewrite/import setting change.

## Full Reimport Pipeline

One explicit `Reimport All 50 CSV` action performs:

1. exact inventory/BOM/read bytes and per-file raw SHA-256 diagnostic hash
2. dictionary schema import/catalog
3. each static file RFC4180 read → header/field validation → PK index → scalar/list parse, accumulating all safe per-file errors
4. four definition builders using exact MAP01_07~10 source subsets
5. 49-source FK resolver
6. StaticDataRegistry builder
7. ContentVersionHash calculator
8. StaticDataAtomicPublisher with all accumulated issues
9. deterministic report JSON atomic file write

Later stage runs only when its required input exists; skipped dependency stages add one clear issue and do not throw. No first-error short circuit across independent files. UI remains responsive enough to repaint progress between named stages; do not introduce background-thread Unity API access.

## Session / File Status

Each exact file row displays filename, category (dictionary/static), byte size, parsed data row count, ERROR/WARNING counts, lowercase raw file SHA-256 (diagnostic only), state (`NOT_RUN`, `SUCCESS`, `WARNING`, `ERROR`).

Session displays running/complete state, named stage, progress `0..1`, total errors/warnings, previous/current Registry version/hash, candidate ContentVersionHash, published yes/no, report path/write success/error. Raw file hash must be clearly labeled and never called ContentVersionHash.

Session/result lists are immutable snapshots; a new run replaces window state only after completion. Reentry while running is blocked. Domain reload leaves no phantom running state.

## Window UI

- menu: `Tools/Star Night/Map/CSV Import`
- toolbar: `Reimport All 50 CSV`, disabled while running; `Open Report` enabled only when file exists
- summary header with publish/version/global hash/report status
- searchable/filterable file table and issue table; filters ALL/ERROR/WARNING
- issue row shows severity, stage, source file/record/field, message, target tuple
- double-click or button `Go to Source` opens exact CSV and 1-based line when available
- `Go to FK Target` uses target file/value and FK record index to open exact target record line; disabled with reason if unavailable
- selection/navigation never edits CSV; no auto-import on focus/change

## Navigation Contract

Resolve only paths under fixed Authoring root after canonical full-path containment check. Reject traversal/absolute injected filenames. Use Unity-supported external script/file opening at line; fallback selects/pings asset and reports unavailable line without throwing. Missing target/path is a non-destructive UI message.

## Report Persistence

`CsvImportReportFileWriter` accepts exact serializer bytes, validates strict UTF-8/no BOM/final LF, creates only fixed REPORTS directory if absent, writes same-directory temp, flushes, atomically replaces/moves destination, and cleans its own temp on failure. Never delete/overwrite unrelated files. Test with temp directories, not project report.

## DO NOT

- alter Runtime APIs, CSV/meta, asmdef, import settings, Scene/Prefab/Package/ProjectSettings
- folder/file picker, alternate source, auto watcher, auto reimport, background Unity API
- silently omit independent file errors or publish outside MAP01_14
- recompute ContentVersionHash from raw file hash
- edit/fix CSV from window, create failure fixtures (MAP01_16)
- runtime UI/singleton Managers integration, Git, MAP01_16 start

## Tests / Verification

Editor focused minimum 30 cases:

- exact 50 inventory/order, missing/unexpected/duplicate/BOM diagnostics
- full valid baseline reimport publishes and writes report
- independent file errors accumulate and block publish/last-good preserved
- stage skip dependency issues, no unexpected throw
- per-file rows/counts/severity/raw hash vs global hash labels
- state replacement/reentry/domain-reload-safe defaults
- menu/window/table/filter/search/summary behavior without layout exception
- report writer atomic success/failure/temp cleanup/UTF-8 validation
- source/target navigation containment, line, missing/path traversal safety
- issue filters/order and unavailable action reason

```text
New Editor import window: >=30 PASS
Atomic publish/report: 55 PASS
Content hash: 54 PASS
Registry: 47 PASS
FK: 54 PASS
Previous targeted baseline: 671/671 PASS
Targeted total: >=701 PASS
Full project EditMode: >=721 PASS
Unity 6000.3.8f1 / refresh PASS / compile error 0 / relevant warning 0
Visual smoke: window opens, 50 rows, no clipping/exception, buttons/filter/navigation states PASS
PlayMode NOT RUN / Scene-Prefab changes NONE
```

CSV/meta 50/50 unchanged, existing C#/tests/asmdef changes 0, new meta 8 valid/GUID duplicate 0. Unity/visual evidence absent면 `BLOCKED`.

## Result / Completion

Result `REPORTS/MAP01_15_CREATE_CSV_IMPORT_WINDOW_RESULT.md`. Required: TASK, STATUS, SUMMARY, READ, MASTER BACKLOG CHECK, MAP01_14 GATE CHECK, CREATED, PREEXISTING_IDENTICAL, FIXED PATHS, PIPELINE, WINDOW UI, NAVIGATION, REPORT WRITE, TEST, UNITY, VISUAL, ASSET META VALIDATION, CHANGE SCOPE, OUT_OF_SCOPE_FINDINGS, DONE CONDITIONS, NEXT, Recommended Commit.

PASS와 모든 조건 충족 시만 MAP01_15 COMPLETE, Current Task NONE으로 finalize. MAP01_16은 LOCKED로 유지하고 자동 시작하지 마.

Recommended Commit: `feat(map): add csv import editor window`

---

## BLOCKED Remediation v1.1 — Authoritative Schema Contracts

이 절은 최초 실행의 `CsvImportReport.json`(schema v1, 25 ERROR, 0 WARNING, `published=false`)에 대한 승인된 same-task 복구 계약이다. 기존 Objective/Result identity를 유지하고 MAP01_16을 열지 않는다.

### Root Cause / Authority

- `CSV_DATA_DICTIONARY.csv`와 fixed Authoring CSV/meta 50개는 authoritative read-only input이다. 수정·정규화·재발행하지 않는다.
- 직접 원인은 definition builder의 hard-coded expected type 18개와 parser의 empty enum vocabulary gate 1개다.
- 나머지 6개 `STAGE_SKIPPED`/`MISSING_*`는 파생 오류다. 숨기거나 special-case로 제거하지 말고 직접 원인을 고친 뒤 full pipeline으로 자연 해소한다.
- in-memory schema rewrite, issue suppression, filename/field-specific bypass는 금지한다.

### Expanded Read / WRITE ALLOWLIST

진단을 위해 dictionary의 아래 19개 row와 MAP01_06/08/09/10 production/tests만 읽을 수 있다. 기존 MAP01_15 Editor 7 + test 1에 더해 실제 설치된 경로를 inventory로 확정한 다음 아래 기존 Runtime production/test 파일만 수정한다:

```text
CsvScalarAndListParser.cs
BiomeBoundaryDefinitionBuilder.cs
SpecialVillageDefinitionBuilder.cs
MicrochunkPopulationItemDefinitionBuilder.cs
CsvScalarAndListParserTests.cs
BiomeBoundaryDefinitionBuilderTests.cs
SpecialVillageDefinitionBuilderTests.cs
MicrochunkPopulationItemDefinitionBuilderTests.cs
CsvImportWindowTests.cs
```

동일 basename 후보가 여러 개면 namespace/asmdef/MAP01_06·08·09·10 Result evidence로 하나를 확정하고, 불명확하면 BLOCKED. 신규 production class나 asmdef 변경은 금지한다.

### Exact Builder Contract Corrections

builder expected schema를 dictionary의 exact ordinal data type으로 교정한다:

```text
biome_types.csv.microchunk_pool_prefix = ID
biome_types.csv.sector_recipe_pool_prefix = ID
event_activation_routes.csv.requires_tool = BOOL
event_activation_routes.csv.requires_consumable = BOOL
special_map_catalog.csv.requires_tool = BOOL
map_element_definitions.csv.interaction_tags = ID_LIST
map_element_definitions.csv.forbidden_near_tags = ID_LIST
map_element_interactions.csv.target_tag = ID
microchunk_catalog.csv.route_roles = ID_LIST
microchunk_pool_entries.csv.required_tags = ID_LIST
microchunk_pool_entries.csv.forbidden_tags = ID_LIST
microchunk_sockets.csv.tool_requirement = ENUM
microchunk_variant_rules.csv.required_world_tags = ID_LIST
microchunk_variant_rules.csv.forbidden_world_tags = ID_LIST
spawn_pool_entries.csv.required_tags = ID_LIST
spawn_pool_entries.csv.forbidden_tags = ID_LIST
tile_code_dictionary.csv.semantic = STRING
tile_code_dictionary.csv.runtime_tag = ID
```

column inventory/order/required/default/PK/FK의 다른 계약은 건드리지 않는다. `microchunk_sockets.tool_requirement`의 allowed values는 기존 exact 6-token vocabulary를 계속 enforce한다.

### Empty ENUM / ENUM_LIST Vocabulary Semantics

`sector_recipe_cells.csv.required_usage_class`는 authoritative `ENUM_LIST`, optional, allowed_values empty다. parser를 일반 규칙으로 수정한다:

- allowed_values가 하나 이상이면 기존 ordinal membership validation을 유지한다.
- allowed_values가 비어 있으면 ENUM/ENUM_LIST schema construction이나 parse가 throw하지 않는다.
- empty vocabulary는 unconstrained token vocabulary이며, required/optional/default/list delimiter/empty-element 규칙은 그대로 적용한다.
- ENUM은 validated scalar token, ENUM_LIST는 existing list semantics의 immutable token list를 반환한다.
- token case/whitespace를 몰래 normalize하지 않는다.

### Regression Tests / Re-run

- 위 18개 field마다 authoritative exact type과 near-miss wrong type rejection을 table-driven regression으로 고정한다.
- empty-vocabulary ENUM 및 ENUM_LIST의 required/optional/non-empty/list/empty-element 동작과 non-empty vocabulary rejection을 최소 6 cases로 고정한다.
- 최초 25-issue report를 재현하는 integration test는 직접 원인 19개를 검증하고, 수정 후 full valid baseline이 모든 stage를 통과하는지 검증한다.
- repair-focused 신규/갱신 최소 24 PASS.
- 기존 window focused는 실패 1건 포함 48개 전부 PASS; targeted total 최소 743 PASS; full EditMode 최소 763 PASS.
- Unity refresh/compile error 0/relevant warning 0, visual smoke PASS, CSV/meta 50/50 byte/hash unchanged.
- `Reimport All 50 CSV` 실실행 결과: report schema v1, `error_count=0`, `warning_count=0`, `published=true`, candidate/current hash non-null and equal, current version 1, all 49 parsed sources available.

### Remediation Completion

기존 `REPORTS/MAP01_15_CREATE_CSV_IMPORT_WINDOW_RESULT.md`를 최종 evidence로 갱신하고 `REMEDIATION v1.1` 절에 최초 25건 → 직접 19/파생 6 분류, changed files, exact tests, final report tuple을 기록한다. 모든 조건 충족 시에만 MAP01_15를 COMPLETE/Current Task NONE으로 finalize한다. 실패 시 MAP01_15 CURRENT/BLOCKED를 유지한다. MAP01_16은 항상 LOCKED이며 자동 시작 금지.

---

## BLOCKED Remediation v1.2 — Special Map BOOL Materialization

v1.1 실행의 sole remaining blocker만 해소하는 same-task 추가 계약이다. v1.1에서 완료된 parser/builder 수정은 보존하고 다시 설계하지 않는다.

### Exact Remaining Failure

```text
event_activation_routes.csv.requires_tool = BOOL
event_activation_routes.csv.requires_consumable = BOOL
special_map_catalog.csv.requires_tool = BOOL
```

`SpecialVillageDefinitionBuilder`의 schema validation은 authoritative BOOL을 정확히 수용한다. 실패 지점은 `SpecialMapDefinitions.cs` materialization이 세 필드에 `WorldRouteDefinitionValueReader.String`을 호출하는 API 계약 불일치다.

### v1.2 Expanded WRITE ALLOWLIST

v1.1 allowlist에 아래 existing production file 하나를 추가한다:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Data/SpecialMapDefinitions.cs
```

대응 회귀는 이미 허용된 아래 test file에서 추가/갱신한다:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/SpecialVillageDefinitionBuilderTests.cs
```

세 public API의 compile reference inventory는 읽기만 허용한다. 실제 추가 consumer 변경이 필요하면 임의 확장하지 말고 BLOCKED한다.

### Required API Correction

- `EventActivationRouteDefinition.RequiresTool`: `string` → `bool`
- `EventActivationRouteDefinition.RequiresConsumable`: `string` → `bool`
- `SpecialMapDefinition.RequiresTool`: `string` → `bool`
- 관련 constructor parameter/backing assignment/equality or snapshot contract가 있으면 동일하게 `bool`로 일치시킨다.
- materialization은 세 필드 모두 `WorldRouteDefinitionValueReader.Bool`을 사용한다.
- BOOL token 자체를 문자열로 보존하는 compatibility shim, `ToString`, field-specific conversion, in-memory schema rewrite는 금지한다.
- 다른 Special/Village property, dictionary, CSV/meta, parser, schema expected type은 변경하지 않는다.

### Focused Verification

- 세 authoritative BOOL field의 `true`/`false` materialization 및 public property type/value를 각각 검증한다.
- STRING near-miss schema는 기존처럼 rejection한다.
- v1.1 special/village 실패 `0/3`을 최소 `3/3 PASS`로 전환한다.
- v1.1 repair-focused 24/24를 포함한 repair total 최소 `27/27 PASS`.
- window focused `48/48 PASS`; targeted total 최소 `746 PASS`; full EditMode 최소 `766 PASS`.
- full actual `Reimport All 50 CSV`: error/warning `0/0`, `published=true`, current version `1`, candidate/current content hash non-null and equal, exact 50 file rows and 49 parsed static sources.
- Unity refresh/compile error/relevant warning `0/0`, visual published state PASS, CSV/meta 50/50 hashes unchanged.

### v1.2 Completion

기존 Result의 `REMEDIATION v1.2` 절에 API before/after, exact changed files, test jobs/counts, final `CsvImportReport.json` tuple/hash, visual evidence를 기록한다. 모든 조건을 충족한 경우에만 MAP01_15 COMPLETE 및 Current Task NONE으로 finalize한다. 하나라도 실패하면 MAP01_15 CURRENT/BLOCKED 유지. MAP01_16은 계속 LOCKED이며 자동 시작하지 않는다.
