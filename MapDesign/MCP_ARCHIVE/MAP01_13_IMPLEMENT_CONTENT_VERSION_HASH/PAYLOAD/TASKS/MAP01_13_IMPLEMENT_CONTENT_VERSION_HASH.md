# MAP01_13 — Implement Content Version Hash

```yaml
status_control:
  task_key: MAP01_13_IMPLEMENT_CONTENT_VERSION_HASH
  result_file: REPORTS/MAP01_13_IMPLEMENT_CONTENT_VERSION_HASH_RESULT.md
```

## Objective

successful immutable `StaticDataRegistry`, matching `ForeignKeySourceSet`, and exact `CsvSchemaCatalog` identity를 input으로 받아 49개 static CSV의 semantic content를 canonical binary stream으로 직렬화한 뒤 SHA-256 `ContentVersionHash`를 계산한다. 파일 제공 순서와 CSV row order에 독립적이고, semantic field/list 변경에는 민감해야 한다.

## Mandatory Read / Allowlist

entrypoint → locked/work/CSV/Unity/change/patch/finalize rules → Master → Status → 이 Task → MAP01_12 Result. MAP01_02~12 production API/direct tests, importer, architecture fixtures, asmdef 4, Runtime Data/test inventory, CSV/meta hash/BOM, meta GUID, Unity Console, WRITE ALLOWLIST만 읽는다. CSV data row/later Task/Legacy/비승인 C#/Scene-Prefab YAML은 직접 읽지 마.

## WRITE ALLOWLIST

Runtime C# 5:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Data/ContentVersionHash.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/ContentHashCanonicalWriter.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/ContentVersionHashError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/ContentVersionHashResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/ContentVersionHashCalculator.cs
```

Test C# 1:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/ContentVersionHashCalculatorTests.cs
```

신규 C# 6 + `.cs.meta` 6 + Result 1만 허용. existing files 수정 금지. Runtime namespace `StarNight.Map.WorldGeneration.Data`, existing Runtime/EditMode assemblies, 새 asmdef/UnityEditor reference 금지.

## Input Gate

- non-null successful `StaticDataRegistry`
- exact matching `ForeignKeySourceSet` with 49 successful static sources and same catalog/schema/record identities as Registry FK index
- no missing/unexpected/duplicate file-record identity
- all source schemas and parsed fields match catalog exact column inventory/order
- gate error accumulation; any error yields no hash/canonical payload

## Canonical Stream v1

Stream starts with exact ASCII magic/version `STARNIGHT_STATIC_DATA_CONTENT_V1` encoded by the same length-prefix primitive.

Every variable payload uses unsigned 64-bit big-endian byte length followed by exact bytes; integers used as structural counts are unsigned 64-bit big-endian. No delimiter-only encoding.

1. Files sort by filename `StringComparer.Ordinal`.
2. Each file writes filename, schema column count, then exact schema columns in declared order: column name and schema type token.
3. Records sort by canonical primary-key tuple using ordinal string component comparison; composite PK component order follows schema PK order. Record number/location/row order are never written.
4. Each record writes PK component count and canonical PK values, then every schema field in column order as `(column name, type token, value)`.
5. Scalar canonical values:
   - STRING/ID/ENUM/HEX: exact UTF-8 string; no trim, case fold, Unicode normalization, newline conversion, or null substitution.
   - INT: invariant signed decimal without leading `+` or redundant zero.
   - FLOAT: invariant round-trip representation (`R`), normalize negative zero to `0`; reject NaN/Infinity.
   - BOOL: single ASCII `0` or `1`.
6. Lists write item count followed by each canonical typed item in original parsed order. Duplicates remain. Empty list differs from one empty token.
7. `UsedDefault`, raw quoting/BOM/line ending/source location are not written because the effective typed semantic value is written.
8. All 49 static files and all schema columns, including inactive rows and notes, participate. Generated 10 files and dictionary CSV are excluded.

## Output Contract

`ContentVersionHash` is immutable, holds exact 32-byte digest privately and exposes lowercase 64-character hex plus safe copied/read-only bytes. Equality/hash code are value-based and ordinal. Result success = one hash, errors 0; failure = null hash, errors >0.

## Error Contract

minimum: `MissingRegistry`, `MissingSourceSet`, `CatalogMismatch`, `SourceInventoryMismatch`, `RecordIdentityMismatch`, `SchemaMismatch`, `UnsupportedValue`, `DuplicateCanonicalPrimaryKey`. Preserve file/record/field and nullable source location; deterministic sort. No partial digest.

## DO NOT

- hash raw CSV bytes, BOM, line endings, record number, file timestamp/path, source location, Unity GUID/meta 금지
- sort/reorder list items or remove duplicates 금지
- trim/case-fold/NFC strings or locale-sensitive formatting 금지
- include generated CSV or dictionary source 금지
- mutate Registry/source/schema or attach hash property to existing Registry 금지
- singleton install, atomic swap/publish/rollback, report JSON/window 금지
- cryptographic salt/HMAC/randomness, external dependency 금지
- existing loader/definitions/FK/Registry/CSV/asmdef/Scene/Prefab/Package/ProjectSettings/Git/MAP01_14 변경·선행 금지

## Tests / Verification

Focused minimum 32 cases:

- exact input success, known SHA-256 vector and 32-byte/64-hex contract
- file order and row order shuffle same hash
- PK tuple ordering independent of record number
- every scalar type canonical form, invariant culture, float round-trip/negative zero
- list order and duplicate affect/preserve hash; empty distinctions
- changes to filename/schema column/type/every value/inactive/notes change hash
- BOM/CRLF/LF/raw quoting/source location/record number do not change hash when semantics equal
- string case/whitespace/Unicode form differences do change hash
- generated/dictionary excluded, exact 49 gate enforced
- malformed/mismatched/NaN/Infinity/duplicate canonical PK errors and no digest
- deterministic canonical stream and immutable output

```text
New content hash: >=32 PASS
Registry: 47 PASS
FK: 54 PASS
Previous targeted baseline: 562/562 PASS
Targeted total: >=594 PASS
Full project EditMode: >=614 PASS
Unity 6000.3.8f1 / refresh PASS / compile error 0 / relevant warning 0
PlayMode NOT RUN / Scene-Prefab changes NONE
```

CSV/meta 50/50, existing C#/tests/asmdef changes 0, new meta 6 valid/GUID duplicate 0. Unity evidence absent면 `BLOCKED`.

## Result / Completion

Result: `REPORTS/MAP01_13_IMPLEMENT_CONTENT_VERSION_HASH_RESULT.md`. Required sections: TASK, STATUS, SUMMARY, READ, MASTER BACKLOG CHECK, MAP01_12 GATE CHECK, CREATED, PREEXISTING_IDENTICAL, INPUT GATE, CANONICAL STREAM V1, HASH CONTRACT, TEST, UNITY, ASSET META VALIDATION, CHANGE SCOPE, OUT_OF_SCOPE_FINDINGS, DONE CONDITIONS, NEXT, Recommended Commit.

PASS와 모든 조건 충족 시만 MAP01_13 COMPLETE, Current Task NONE으로 finalize. MAP01_14는 LOCKED로 유지하고 자동 시작하지 마.

Recommended Commit: `feat(map): add deterministic content version hash`
