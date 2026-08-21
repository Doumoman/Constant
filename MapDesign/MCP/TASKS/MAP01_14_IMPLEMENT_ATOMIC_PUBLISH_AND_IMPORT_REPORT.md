# MAP01_14 — Atomic Publish and Import Report

```yaml
status_control:
  task_key: MAP01_14_IMPLEMENT_ATOMIC_PUBLISH_AND_IMPORT_REPORT
  result_file: REPORTS/MAP01_14_IMPLEMENT_ATOMIC_PUBLISH_AND_IMPORT_REPORT_RESULT.md
```

## Objective

candidate `StaticDataRegistry` + `ContentVersionHash`를 immutable published snapshot으로 묶어 원자적으로 교체한다. import issue가 하나라도 있거나 candidate가 불완전하면 current last-good snapshot/version을 exact reference로 보존하고, 모든 issue와 publish 결과를 deterministic `CsvImportReport.json` contract로 직렬화한다.

## Mandatory Read / Allowlist

entrypoint → locked/work/CSV/Unity/change/patch/finalize rules → Master → Status → 이 Task → MAP01_13 Result. MAP01_02~13 production API/direct tests, importer, architecture fixtures, asmdef 4, inventory/hash/BOM/meta/Console, WRITE ALLOWLIST만 읽는다. CSV data row/later Task/Legacy/비승인 C#/Scene-Prefab YAML은 직접 읽지 마.

## WRITE ALLOWLIST

Runtime C# 7:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Data/PublishedStaticDataSnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/StaticDataRegistryStore.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvImportIssue.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvImportReport.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvImportReportJson.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/StaticDataPublishRequest.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/StaticDataAtomicPublisher.cs
```

Test C# 1:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/StaticDataAtomicPublisherTests.cs
```

신규 C# 8 + `.cs.meta` 8 + Result 1만 허용. existing files 수정 금지. Runtime namespace/assemblies 재사용, 새 asmdef/UnityEditor reference 금지.

## Snapshot / Store Contract

- `PublishedStaticDataSnapshot` holds exact non-null Registry + hash + monotonically increasing `long Version` as one immutable object.
- `StaticDataRegistryStore.Current` returns one snapshot reference or null before first success.
- successful publish replaces one snapshot reference atomically; readers never observe registry/hash/version from different attempts.
- first successful version is 1; subsequent successful changes increment exactly once.
- failure, cancellation marker, exception captured as issue, null candidate, or any issue count >0 leaves Current and version unchanged by reference.
- same content hash still publishes a new explicitly submitted successful snapshot/version; deduplication policy is out of scope.
- store exposes no public setter, mutable collection, clear/reset, or partial update.

## Publish Request / Issue Contract

`StaticDataPublishRequest` contains candidate Registry(nullable), hash(nullable), complete immutable issue sequence, and optional attempt ID string. It does not run CSV parsing/building itself.

`CsvImportIssue` fields:

```text
stage, severity, code, message,
source_file, record_number?, source_field?, line?, column?, offset?,
target_file?, target_column?, target_value?
```

- severity token exact `ERROR` or `WARNING`; ERROR blocks publish, WARNING does not.
- null/empty issue entries or invalid severity become blocking publisher issues rather than being ignored.
- caller issue order does not affect report order.
- issues sort severity(ERROR first) → stage → source file → record → field → target tuple → code → message ordinal.
- all supplied valid issues are preserved; no first-error short circuit or deduplication.

## Atomic Publish Rule

Publish succeeds only when candidate Registry/hash are non-null and there are zero ERROR issues. Warnings remain in report but allow publish. Publisher validates request fully before constructing/exchanging snapshot. Any internal validation/serialization failure becomes an ERROR report and preserves last-good snapshot.

The method returns `CsvImportReport`; it does not throw for expected import failure. Programmer misuse outside the declared request contract may throw only if explicitly tested and documented.

## CsvImportReport v1 / JSON Contract

Report immutable fields:

```text
schema_version = 1
attempt_id
published
previous_version
current_version
previous_content_hash (nullable)
candidate_content_hash (nullable)
current_content_hash (nullable)
error_count
warning_count
issues[]
```

- filename contract: exact `CsvImportReport.json`; serializer returns deterministic strict UTF-8 JSON string/bytes but does not write filesystem.
- property order exactly as above; issue property order exactly issue field order.
- JSON escaping is standards compliant; newline output exact LF and final newline one.
- invariant decimal numbers; null emitted explicitly; booleans lowercase.
- no wall-clock time, machine path/name, Unity instance, random ID, stack trace, locale-dependent text.
- failed attempt reports previous/current same snapshot/hash/version; candidate hash may be reported without publication.
- success reports new current snapshot/hash/version.

## DO NOT

- CSV read/validate/build/hash pipeline orchestration 금지; caller supplies candidate/issues
- EditorWindow/file picker/watcher/AssetDatabase/menu/progress UI 금지
- actual report file disk write 금지
- mutate Registry/hash/current snapshot or expose partial candidate 금지
- swallow/drop/deduplicate errors, publish on ERROR, clear last-good on failure 금지
- singleton Managers/DataManager integration or scene lifecycle 금지
- existing loader/definitions/FK/Registry/hash/CSV/asmdef/Scene/Prefab/Package/ProjectSettings/Git/MAP01_15 변경·선행 금지

## Tests / Verification

Focused minimum 36 cases:

- empty store, first success version 1, repeated success increment
- exact registry/hash pairing and atomic snapshot reference visibility
- null Registry/hash, one/multiple ERROR, invalid issue, captured internal failure preserve last-good
- WARNING-only publish, mixed warning/error block
- failed first attempt remains null/version0; failed later exact same reference/version/hash
- all issues preserved, duplicates preserved, deterministic sort independent input order
- report counts/hash/version for success/failure
- deterministic JSON property order, escaping, nulls, LF/final newline, UTF-8
- no timestamp/path/random/stack trace leakage
- immutable request/snapshot/report/issues and concurrent readers never see torn pair
- no CSV orchestration/window/disk write/singleton integration

```text
New atomic publish/report: >=36 PASS
Content hash: 54 PASS
Registry: 47 PASS
FK: 54 PASS
Previous targeted baseline: 616/616 PASS
Targeted total: >=652 PASS
Full project EditMode: >=672 PASS
Unity 6000.3.8f1 / refresh PASS / compile error 0 / relevant warning 0
PlayMode NOT RUN / Scene-Prefab changes NONE
```

CSV/meta 50/50, existing C#/tests/asmdef changes 0, new meta 8 valid/GUID duplicate 0. Unity evidence absent면 `BLOCKED`.

## Result / Completion

Result: `REPORTS/MAP01_14_IMPLEMENT_ATOMIC_PUBLISH_AND_IMPORT_REPORT_RESULT.md`. Required sections: TASK, STATUS, SUMMARY, READ, MASTER BACKLOG CHECK, MAP01_13 GATE CHECK, CREATED, PREEXISTING_IDENTICAL, SNAPSHOT STORE, ISSUE CONTRACT, ATOMIC PUBLISH, REPORT JSON, TEST, UNITY, ASSET META VALIDATION, CHANGE SCOPE, OUT_OF_SCOPE_FINDINGS, DONE CONDITIONS, NEXT, Recommended Commit.

PASS와 모든 조건 충족 시만 MAP01_14 COMPLETE, Current Task NONE으로 finalize. MAP01_15는 LOCKED로 유지하고 자동 시작하지 마.

Recommended Commit: `feat(map): publish static data atomically with import reports`
