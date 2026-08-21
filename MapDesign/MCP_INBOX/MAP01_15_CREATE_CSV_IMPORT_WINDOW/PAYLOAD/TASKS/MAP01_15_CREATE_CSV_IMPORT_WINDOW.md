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
