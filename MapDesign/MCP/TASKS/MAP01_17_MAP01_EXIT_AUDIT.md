# MAP01_17 — MAP01 Exit Audit

```yaml
status_control:
  task_key: MAP01_17_MAP01_EXIT_AUDIT
  result_file: REPORTS/MAP01_17_MAP01_EXIT_AUDIT_RESULT.md
```

## Objective

MAP01 전체 phase gate를 독립적으로 재검증한다. fixed Authoring dictionary 1 + static CSV 49를 production pipeline으로 import해 ERROR/FK failure 0, published Registry, stable ContentVersionHash와 필수 World/Biome/RouteMask/Battery ID 25개를 최종 승인한다.

## Mandatory Read / Scope

entrypoint → locked/work/CSV/Unity/change/patch/finalize rules → Master → Status → 이 Task → MAP01_15/16 PASS Results → fixed spec ID rule와 MAP01 exit criteria를 읽는다. MAP01 production public API/tests, exact 50 inventory, current report/Registry만 읽는다. later Task/MAP02 body, Legacy, 비승인 production, Scene/Prefab YAML 금지.

## WRITE ALLOWLIST

신규 EditMode audit test C# 1:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/Map01ExitAuditTests.cs
```

matching `.cs.meta` 1과 Result 1만 생성한다. production C#, 기존 tests, asmdef, Authoring CSV/meta, report serializer/schema, Scene/Prefab/Package/ProjectSettings 변경 금지.

## Exact Source / Publish Gate

- Authoring root exact 50 CSV: `CSV_DATA_DICTIONARY.csv` 1 + exact static source 49; missing/unexpected/duplicate 0.
- UTF-8 BOM exact 50/50; dictionary/catalog/header/field/PK/value/4 definitions/FK/Registry/hash/publish/report stages all success.
- final issues: ERROR 0, WARNING 0, FK failure 0, skipped stage 0.
- `published=true`, Registry non-null, version positive, candidate/current ContentVersionHash 64 lowercase hex and equal.
- same unchanged package second import produces the same semantic ContentVersionHash and same Registry membership; version behavior follows publisher contract without semantic drift.
- persisted `CsvImportReport.json` strict UTF-8/no BOM/final LF and semantic tuple matches live session.

## Required Registry IDs — Exact 25

World `1`:

```text
WORLD_MOONPALACE_V1
```

Biome `4`:

```text
BIO_MOON_CRATER
BIO_CASSIA_ROOT
BIO_ABANDONED_MILL
BIO_MOON_DOUGH
```

RouteMask `15`:

```text
ROUTE_T0_NONE
ROUTE_T0_L
ROUTE_T0_R
ROUTE_T0_U
ROUTE_T0_D
ROUTE_T0_LU
ROUTE_T0_LD
ROUTE_T0_RU
ROUTE_T0_RD
ROUTE_T0_UD
ROUTE_T0_LUD
ROUTE_T0_RUD
ROUTE_T1_LR
ROUTE_T2_LRD
ROUTE_T3_LRU
```

Battery `5`:

```text
BAT_MINI
BAT_AIR_CANNON
BAT_STANDARD
BAT_MEGA
BAT_GRENADE
```

각 ID는 correct typed Registry dictionary에서 ordinal exact lookup되고 SourceRecord filename/PK identity와 일치해야 한다. 네 source catalog의 전체 ID set과 Registry typed set도 exact set equality여야 하며 missing/extra/duplicate 0이다.

## Audit Tests / Evidence

최소 14 focused cases:

- exact 50 inventory/BOM and exact 49 successful parsed sources
- all stages success, ERROR/WARNING/FK/skip 0
- publish/version/hash/report tuple and second-import hash stability
- World/Biome/RouteMask/Battery exact set equality + individual 25 ID lookup
- SourceRecord filename/PK identity
- Registry collections immutable and lookup ordinal
- MAP01_16 representative invalid fixture after valid seed preserves exact Registry/version/hash identity
- invalid → valid recovery and report/session replacement
- Authoring bytes/meta and production/test/asmdef before-after preservation

```text
New MAP01 exit audit: >=14 PASS
MAP01_16 fixture: 37/37 PASS
CSV import window: 48/48 PASS
Microchunk population: 77/77 PASS
World route: 73/73 PASS
Atomic/hash/Registry/FK: 210/210 PASS
Previous targeted baseline: 801/801 PASS
Targeted total: >=815 PASS
Full project EditMode: >=835 PASS
Unity 6000.3.8f1 / refresh PASS / compile error 0 / relevant warning 0
Visual: CSV Import published, 50 files, 0 issues, hash/version/report shown, no IMGUI exception
PlayMode NOT RUN / Scene-Prefab changes NONE
```

## Phase Gate Decision

아래를 모두 evidence로 표기한다:

```text
CSV ERROR 0
필수 ID 조회 실패 0
외래키 실패 0
실패 import 시 previous Registry 보존 PASS
MAP01 PHASE GATE APPROVED
```

하나라도 실패하거나 Unity/visual evidence가 없으면 `BLOCKED`; MAP02를 열지 않는다.

## DO NOT

- production/schema/CSV/content를 audit에 맞춰 수정
- warning/error/ID 누락을 허용하거나 test skip/ignore/assertion 완화
- hard-coded fake Registry, production test hook, report 수동 위조
- MAP02 Task/body/code 생성 또는 자동 시작
- Git commit/push

## Result / Completion

Result `REPORTS/MAP01_17_MAP01_EXIT_AUDIT_RESULT.md`. Required: TASK, STATUS, SUMMARY, READ, MASTER BACKLOG CHECK, MAP01_15/16 GATE CHECK, CREATED, SOURCE INVENTORY, PIPELINE/REPORT, REQUIRED ID MATRIX, REGISTRY/FK, HASH/REIMPORT, FAILURE PRESERVATION, TEST, UNITY, VISUAL, ASSET META VALIDATION, CHANGE SCOPE, PHASE GATE DECISION, OUT_OF_SCOPE_FINDINGS, DONE CONDITIONS, NEXT, Recommended Commit.

모든 조건 PASS 시에만 MAP01_17 COMPLETE, Current Task NONE으로 finalize한다. MAP01 phase gate를 승인하되 MAP02_01은 LOCKED로 유지하고 자동 시작하지 않는다.

Recommended Commit: `test(map): approve map01 csv registry phase gate`

