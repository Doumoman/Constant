# MAP01_16 — Create CSV Failure Fixtures and Tests

```yaml
status_control:
  task_key: MAP01_16_CREATE_CSV_FAILURE_FIXTURES_AND_TESTS
  result_file: REPORTS/MAP01_16_CREATE_CSV_FAILURE_FIXTURES_AND_TESTS_RESULT.md
```

## Objective

MAP01_15의 successful fixed-50 import pipeline을 test-owned temporary fixture package로 실행해 duplicate primary key, invalid enum/number, missing FK, BOM, ordering 변화와 failure 시 previous Registry 보존을 deterministic regression으로 고정한다. 실제 Authoring CSV는 단 한 바이트도 변경하지 않는다.

## Mandatory Read / Scope

entrypoint → locked/work/CSV/Unity/change/patch/finalize rules → Master → Status → 이 Task → MAP01_15 Result를 읽는다. MAP01_02~15의 public API와 해당 tests, exact 50 inventory/manifest/hash, test temp-directory patterns만 읽는다. later Task/Legacy/비승인 production/Scene-Prefab YAML/Package/ProjectSettings 금지.

CSV data row는 fixture 설계에 필요한 최소 header/첫 valid record와 FK tuple만 승인된 test helper를 통해 읽는다. 도메인 콘텐츠 분석이나 actual package 수정은 금지한다.

## WRITE ALLOWLIST

신규 EditMode test support C# 1 + test C# 1:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/CsvFailureFixtureFactory.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/CsvFailureFixtureTests.cs
```

matching `.cs.meta` 2와 Result 1만 생성한다. production Runtime/Editor C#, 기존 tests, asmdef, 실제 CSV/meta 수정 금지. exact destination이 이미 존재하면 payload-equivalent가 아니면 BLOCKED.

## Fixture Isolation Contract

- fixture root는 each test마다 OS temp 아래 새 unique directory다.
- fixed Authoring CSV 50개를 read-only source로 byte copy하고 mutation은 temp copy에만 수행한다.
- factory는 exact expected 50 filenames 외 파일을 만들지 않고 canonical containment를 검사한다.
- line ending/UTF-8/no-BOM은 원본에서 보존하고, BOM case만 의도적으로 UTF-8 BOM을 추가한다.
- cleanup은 factory가 만든 exact temp root만 finally/Dispose에서 제거한다. project/Assets/REPORTS나 unrelated temp는 삭제하지 않는다.
- test parallelism으로 Registry Store가 경합하지 않도록 non-parallel fixture suite로 격리하고 each case가 Store before/after를 명시적으로 검증·복구한다.
- production에 test hook, conditional compile bypass, fixture-aware branch를 추가하지 않는다.

## Mandatory Fixture Cases

최소 아래 10 named fixture mutation을 제공한다:

1. `DUPLICATE_PRIMARY_KEY` — valid file의 exact PK row를 복제한다.
2. `INVALID_ENUM_TOKEN` — non-empty vocabulary ENUM에 unknown ordinal token을 넣는다.
3. `INVALID_INT` — INT field에 non-integer token을 넣는다.
4. `INVALID_FLOAT` — FLOAT field에 non-finite/invalid token을 넣는다.
5. `MISSING_SINGLE_FOREIGN_KEY` — required single FK를 존재하지 않는 ID로 바꾼다.
6. `MISSING_LIST_FOREIGN_KEY` — list FK 중 하나만 missing ID로 바꾼다.
7. `UNEXPECTED_UTF8_BOM` — 허용되지 않는 static source 한 개에 BOM을 추가한다.
8. `ROW_ORDER_REVERSED` — data rows만 역순으로 복사한다.
9. `HEADER_ORDER_CHANGED` — 두 header column을 교환하고 rows도 같은 방식으로 바꾸지 않는다.
10. `COMPOUND_INDEPENDENT_FAILURES` — 서로 다른 파일에 duplicate/enum/number/FK 오류를 함께 넣는다.

mutation은 filename/column/record/before/after를 immutable descriptor로 남기며 random 선택을 하지 않는다. source line과 record number가 report에서 재현 가능해야 한다.

## Expected Behavior

- duplicate/enum/number/FK/BOM/header cases는 `published=false`, exact stage/code/source tuple, error count와 navigation line을 검증한다.
- compound case는 safe independent errors를 전부 누적하고 deterministic issue ordering/report bytes를 두 번 실행해 비교한다.
- 모든 failure case에서 seeded previous Registry instance/version/hash가 identity 포함 그대로 유지되고 candidate가 publish되지 않는다.
- `ROW_ORDER_REVERSED`는 성공 publish하며 semantic ContentVersionHash가 valid baseline과 exact 동일하다. raw file SHA-256 변화와 global hash 불변을 함께 검증한다.
- failure 뒤 valid fixture를 실행하면 publish가 정상 회복되며 previous failure가 session/report/Store에 누출되지 않는다.
- expected import failures는 throw하지 않고 session/report issue로 반환된다.

## Tests / Verification

최소 20 focused cases:

- factory exact 50 copy, containment, unique root, deterministic mutation, cleanup safety
- mandatory mutation 10개 각각의 exact changed bytes/field identity
- duplicate PK, invalid ENUM/INT/FLOAT, missing single/list FK, BOM/header diagnostics
- compound accumulation/order/report determinism
- previous Registry/version/hash identity preservation for every failure family
- row-order raw hash change + ContentVersionHash equality + successful publish
- failure-to-valid recovery and session isolation

```text
New failure fixture focused: >=20 PASS
CSV import window: 48/48 PASS
Microchunk population: 77/77 PASS
World route: 73/73 PASS
Atomic/hash/Registry/FK: 210/210 PASS
Previous targeted baseline: 764/764 PASS
Targeted total: >=784 PASS
Full project EditMode: >=804 PASS
Unity 6000.3.8f1 / refresh PASS / compile error 0 / relevant warning 0
PlayMode NOT RUN / Scene-Prefab changes NONE
```

실제 Authoring CSV/meta exact 50/50 before-after byte/hash unchanged, production C#/existing test/asmdef changes 0, new C#/meta 2/2, GUID duplicate 0을 증명한다.

## DO NOT

- actual Authoring CSV/meta 또는 successful Registry/report를 fixture로 사용해 직접 변형
- production correction/refactor, schema/dictionary 변경, test-only production hook
- first-error short circuit, issue suppression, assertion 완화, skip/ignore
- random/timestamp-dependent fixture content 또는 temp path를 report semantic assertion에 포함
- auto watcher, runtime UI, domain validation, Git, MAP01_17 시작

## Result / Completion

Result `REPORTS/MAP01_16_CREATE_CSV_FAILURE_FIXTURES_AND_TESTS_RESULT.md`. Required: TASK, STATUS, SUMMARY, READ, MASTER BACKLOG CHECK, MAP01_15 GATE CHECK, CREATED, FIXTURE FACTORY, FIXTURE MATRIX, DIAGNOSTIC ASSERTIONS, REGISTRY PRESERVATION, HASH/ORDERING, RECOVERY, TEST, UNITY, ASSET META VALIDATION, CHANGE SCOPE, OUT_OF_SCOPE_FINDINGS, DONE CONDITIONS, NEXT, Recommended Commit.

PASS와 모든 조건 충족 시만 MAP01_16 COMPLETE, Current Task NONE으로 finalize한다. MAP01_17은 LOCKED로 유지하고 자동 시작하지 않는다.

Recommended Commit: `test(map): add csv import failure fixtures`
