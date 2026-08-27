# MAP01_11 — Implement Foreign-Key Resolver

```yaml
status_control:
  task_key: MAP01_11_IMPLEMENT_FOREIGN_KEY_RESOLVER
  result_file: REPORTS/MAP01_11_IMPLEMENT_FOREIGN_KEY_RESOLVER_RESULT.md
```

## Objective

49개 Authoring static CSV의 successful parsed records와 immutable schema catalog을 기준으로, schema에 명시된 single/list FK를 deterministic 2차/3차 pass로 해석한다. 성공 참조는 exact target record를 가리키고, 끊긴 참조는 source file/record/field/location과 target file/column/value를 보고한다.

## Mandatory Read Order

`00_MCP_ENTRYPOINT.md` → `01_PROJECT_LOCKED_RULES.md` → `02_MCP_WORK_RULES.md` → `03_DATA_CSV_RULES.md` → `04_UNITY_MCP_RULES.md` → `05_CHANGE_CONTROL_RULES.md` → `07_PATCH_APPLY_RULES.md` → `08_STATUS_FINALIZE_RULES.md` → Master → Status → 이 Task → MAP01_10 Result.

## READ ALLOWLIST

- Mandatory Read Order
- MAP01_02~10 schema/reader/validation/PK/parser/definition production APIs와 direct focused tests
- importer test, architecture fixtures 3개, asmdef 4개
- Authoring CSV/meta 50개의 inventory/hash/BOM과 exact 49 static filename의 schema header
- WRITE ALLOWLIST 경로의 기존 파일/meta

CSV data row, later Task, Legacy, 비승인 C#, Scene/Prefab YAML은 직접 읽지 마. resolver test fixture의 synthetic records만 사용한다.

## WRITE ALLOWLIST

Runtime production C# 7개:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Data/ForeignKeySourceSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/ForeignKeyRecordIdentity.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/ResolvedForeignKeyReference.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/ForeignKeyResolutionError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/ForeignKeyResolutionResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/ForeignKeyRecordIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/ForeignKeyResolver.cs
```

EditMode test 1개:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/ForeignKeyResolverTests.cs
```

신규 C# 8 + `.cs.meta` 8 + Result 1만 허용한다. 기존 C#/meta는 수정하지 마.

## Namespace / Assembly

Runtime `StarNight.Map.WorldGeneration.Data`, tests `StarNight.Map.Tests.WorldGeneration.Data`, existing `Game.Map.Runtime` / `Game.Map.Tests.EditMode`. 새 asmdef/asmref/package와 runtime `UnityEditor` 참조 금지.

## Input Gate

`ForeignKeySourceSet` is immutable and contains exact successful `CsvScalarAndListParseResult` inputs for all 49 non-dictionary Authoring static CSV files plus the matching `CsvSchemaCatalog`.

- exact 49 filenames, each once; generated 10개와 dictionary는 제외
- parse success/errors 0, schema filename/column inventory/order, parsed/validated/source identity 일치
- every FK target file/column declared by schema exists in the catalog
- null/missing/duplicate/unexpected/unsuccessful/schema mismatch를 안전한 범위에서 누적
- gate error 시 index/reference graph를 publish하지 않음

## Pass Contract

### Pass 2 — Record Index

- MAP01_05 PK semantics를 재사용하여 target lookup index를 만든다.
- referenced target column은 schema에서 declared PK column이어야 한다.
- `ForeignKeyRecordIdentity` is `(FileName, CsvParsedRecord SourceRecord)`; fake row/location을 만들지 마.
- filename/column/value comparison is `StringComparer.Ordinal`.
- duplicate PK나 invalid structural input은 existing validation/PK gate failure로 취급하고 임의로 하나를 고르지 마.

### Pass 3 — FK Resolution

- schema foreign-key metadata가 non-empty인 column만 해석한다.
- scalar `ID` FK는 non-empty value 1개를 target record 1개에 연결한다.
- `ID_LIST` FK는 parsed list order와 duplicate를 보존하여 token별 reference를 만든다.
- optional empty scalar/list는 reference/error를 만들지 않는다.
- `ResolvedForeignKeyReference` preserves source identity, source field, list index(nullable), raw value, target filename/column/value and exact target identity.
- success references는 source filename → record number → source column order → list index → target value ordinal로 stable sort.
- broken reference는 성공 reference와 분리하고 partial success list를 제공하되 overall `Success == false`다.

## Error Contract

minimum codes:

```text
MissingSource
UnexpectedSource
DuplicateSource
UnsuccessfulParse
SchemaMismatch
InvalidForeignKeyDeclaration
MissingTargetRecord
```

`MissingTargetRecord`는 exact source location(nullable), source file/record/field, list index, target file/column/value를 보존한다. errors는 source filename → record → column order → list index → target file/column/value → code ordinal로 정렬한다.

## Scope Boundary / DO NOT

- schema에 FK가 없는 polymorphic ID (`entry_id`, interaction source/target, effect ID 등) 추론 금지
- ID 대소문자 보정, trim, alias, fallback, reverse reference 생성 금지
- domain validation, active filtering, asset/addressable load, placement/candidate 선택 금지
- definition object clone/mutation·typed navigation property 주입 금지
- StaticDataRegistry, content hash, atomic publish/report/window 금지
- reader/schema/validator/PK/parser/definitions/CSV/asmdef 수정 금지
- Scene/Prefab/Package/ProjectSettings/external dependency/Git/MAP01_12 선행 금지

## Collision Handling

absent면 생성, exact byte-identical이면 `PREEXISTING_IDENTICAL`, 다르면 overwrite/merge 없이 `BLOCKED`. 기존 meta GUID와 사용자 변경을 보존한다.

## Tests / Verification

`ForeignKeyResolverTests` minimum 40 cases:

- exact 49-source gate, missing/duplicate/unexpected/unsuccessful/schema mismatch
- scalar/list FK success, optional empty, list order/duplicate, case sensitivity
- missing target single/list token별 exact error provenance
- same ID in different target tables remains isolated
- shuffled source/row order gives identical index/reference/error order
- source and target `CsvParsedRecord` identity preservation
- immutable/read-only results and no publication on gate failure
- broken references make overall failure while preserving independently resolved references
- schema without FK and polymorphic implicit IDs are untouched
- existing parser/PK/definition inputs remain unchanged

```text
New FK resolver: >=40 PASS
Microchunk/population/item definitions: 64/64 PASS
Special/village: 48/48 PASS
Biome/boundary: 36/36 PASS
World/route: 59/59 PASS
Parser 97 + PK 32 + validator 29 + reader 31 + schema 23 + importer 9 + architecture 10: ALL PASS
Targeted total: >=478 PASS
Full project EditMode: >=521 PASS
Unity 6000.3.8f1 / refresh PASS / compile error 0 / relevant warning 0
PlayMode NOT RUN / Scene-Prefab changes NONE
```

CSV/meta 50/50, 기존 C#/tests/asmdef 변경 0, 신규 meta 8 GUID 유효·중복 0을 확인한다. Unity 증거가 없으면 `BLOCKED`.

## Result

`REPORTS/MAP01_11_IMPLEMENT_FOREIGN_KEY_RESOLVER_RESULT.md`

필수 섹션: TASK, STATUS, SUMMARY, READ, MASTER BACKLOG CHECK, MAP01_10 GATE CHECK, CREATED, PREEXISTING_IDENTICAL, SOURCE SET CONTRACT, PASS 2 INDEX, PASS 3 RESOLUTION, TEST, UNITY, ASSET META VALIDATION, CHANGE SCOPE, OUT_OF_SCOPE_FINDINGS, DONE CONDITIONS, NEXT, Recommended Commit.

## DONE CONDITIONS

- [ ] Current MAP01_11, Master 205, MAP01_10 COMPLETE/PASS
- [ ] exact 49 static source gate와 schema-declared scalar/list FK only
- [ ] ordinal stable index, references, broken-reference errors and exact provenance
- [ ] optional empty/list order/duplicate/source-target identity preserved
- [ ] gate failure no publication; broken reference overall failure + resolved subset preserved
- [ ] polymorphic inference/domain validation/Registry/hash/publish absent
- [ ] Runtime 7 + test 1 + meta 8 only; existing files/CSV/asmdef unchanged
- [ ] new >=40, targeted >=478, full >=521 PASS; compile/warning 0/0
- [ ] Result complete; MAP01_12 not started

## Completion Rule

exact `STATUS: PASS`와 모든 condition 충족 시만 MAP01_11을 COMPLETE로 finalize하고 Current Task를 NONE으로 만든다. MAP01_12는 LOCKED로 유지하며 자동 생성·실행하지 마.

## Recommended Commit

`feat(map): resolve schema-declared foreign keys`
