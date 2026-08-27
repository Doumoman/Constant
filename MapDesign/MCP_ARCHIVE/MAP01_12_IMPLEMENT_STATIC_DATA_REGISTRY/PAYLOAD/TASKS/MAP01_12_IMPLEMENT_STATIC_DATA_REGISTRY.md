# MAP01_12 — Implement Static Data Registry

```yaml
status_control:
  task_key: MAP01_12_IMPLEMENT_STATIC_DATA_REGISTRY
  result_file: REPORTS/MAP01_12_IMPLEMENT_STATIC_DATA_REGISTRY_RESULT.md
```

## Objective

MAP01_07~10의 네 immutable definition set과 MAP01_11의 successful FK resolution result를 하나의 immutable/read-only `StaticDataRegistry` snapshot으로 조립한다. 기존 ID dictionary/composite collection을 보존하고, FK graph를 기반으로 필요한 reverse index만 deterministic하게 publish한다.

## Mandatory Read Order

`00_MCP_ENTRYPOINT.md` → locked/work/CSV/Unity/change/patch/finalize rules → Master → Status → 이 Task → MAP01_11 Result.

## READ ALLOWLIST

- Mandatory Read Order
- MAP01_02~11 production API와 direct focused tests, importer, architecture fixtures, asmdef 4개
- Runtime Data/test 직계 inventory, CSV/meta 50 hash/BOM, meta GUID, Unity Console
- WRITE ALLOWLIST의 기존 파일/meta

Authoring CSV data row, later Task, Legacy, 비승인 C#, Scene/Prefab YAML은 읽지 마. CSV를 수정·재저장하지 마.

## WRITE ALLOWLIST

Runtime production C# 6개:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Data/StaticDataRegistryInput.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/StaticDataReverseIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/StaticDataRegistry.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/StaticDataRegistryBuildError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/StaticDataRegistryBuildResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/StaticDataRegistryBuilder.cs
```

EditMode test 1개:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/StaticDataRegistryBuilderTests.cs
```

신규 C# 7 + `.cs.meta` 7 + Result 1만 허용한다. 기존 C#/meta는 수정하지 마.

## Input Gate

`StaticDataRegistryInput` must contain non-null exact instances of:

```text
WorldRouteDefinitionSet
BiomeBoundaryDefinitionSet
SpecialVillageDefinitionSet
MicrochunkPopulationItemDefinitionSet
ForeignKeyResolutionResult (Success == true, Errors == 0, non-null index)
```

- all source-record identities represented by the four sets must belong to the FK result index
- no unexpected/missing/duplicate file-record identity
- all successful FK reference source/target identities must exist in the index
- FK graph must be internally consistent and ordinal stable
- gate error를 모두 누적하고 any error 시 Registry/reverse index를 publish하지 않음

49 static source의 모든 record가 typed definition으로 노출되어야 한다고 임의로 가정하지 마. Registry는 available typed definition sets과 complete FK record index를 함께 보존한다.

## Registry Contract

`StaticDataRegistry` is an immutable snapshot and exposes:

- the exact four definition-set instances as read-only roots
- exact `ForeignKeyRecordIndex`, resolved references, and zero errors from MAP01_11
- typed ID dictionary/composite collection access through the four roots without cloning or active filtering
- generic record lookup by `(fileName, recordNumber)` and referenced PK component lookup delegated to the immutable FK index
- stable enumeration of all records by filename then record number
- source record identity to typed definition object lookup for every definition materialized in MAP01_07~10
- no mutable dictionary/list/array exposure

## Reverse Index Contract

`StaticDataReverseIndex` is derived only from resolved schema-declared FK references:

- target record identity → incoming references
- source record identity → outgoing references
- `(targetFile, targetColumn, targetValue)` → incoming references
- query results are read-only, preserve distinct list-token references including duplicates, and use MAP01_11 stable reference order
- missing key returns an empty read-only view, never null

No inferred/polymorphic reverse edge, semantic alias, active-only index, domain-specific convenience index, or object mutation.

## Error Contract

minimum codes: `MissingDefinitionSet`, `UnsuccessfulForeignKeyResolution`, `DefinitionRecordMissingFromIndex`, `ForeignKeyGraphMismatch`, `DuplicateTypedDefinitionIdentity`. Errors preserve relevant file/record/type and nullable source location and sort deterministically. Any error yields null Registry.

## Scope Boundary / DO NOT

- global/static singleton, service locator, Managers/DataManager integration, Unity lifecycle install 금지
- current/previous Registry swap, atomic publish/rollback/last-good preservation 금지
- content normalization/SHA-256/ContentVersionHash 금지
- CSV import orchestration/report JSON/window 금지
- new FK inference, domain validation, active filtering, asset/addressable load 금지
- definition/FK/source record clone or mutation 금지
- existing loader/definitions/FK/CSV/asmdef/Scene/Prefab/Package/ProjectSettings 수정 금지
- external dependency/Git/MAP01_13 선행 금지

## Collision Handling

absent면 생성, exact byte-identical이면 `PREEXISTING_IDENTICAL`, 다르면 overwrite/merge 없이 `BLOCKED`. 기존 GUID/사용자 변경 보존.

## Tests / Verification

`StaticDataRegistryBuilderTests` minimum 36 cases:

- exact five-input success and exact instance preservation
- each missing/null definition set, unsuccessful/null/inconsistent FK result
- definition source identity present/missing/duplicate in index
- typed identity lookup for every definition family
- generic file/record and PK component lookup
- incoming/outgoing/target-value reverse queries
- list duplicate/order preservation and empty-query behavior
- shuffled registry input construction produces identical enumeration/index order
- immutable/read-only roots, dictionaries, collections, queries
- any gate error publishes no partial registry
- no active filtering, inferred FK, hash, singleton, atomic publish

```text
New StaticDataRegistry: >=36 PASS
FK resolver: 54/54 PASS
Microchunk/population/item: 64/64 PASS
Special/village: 48/48 PASS
Biome/boundary: 36/36 PASS
World/route: 59/59 PASS
Parser 97 + PK 32 + validator 29 + reader 31 + schema 23 + importer 9 + architecture 10: ALL PASS
Exact targeted total: >=528 PASS
Full project EditMode: >=571 PASS
Unity 6000.3.8f1 / refresh PASS / compile error 0 / relevant warning 0
PlayMode NOT RUN / Scene-Prefab changes NONE
```

CSV/meta 50/50, existing C#/tests/asmdef changes 0, new meta 7 valid/GUID duplicate 0. Unity evidence absent면 `BLOCKED`.

## Result

`REPORTS/MAP01_12_IMPLEMENT_STATIC_DATA_REGISTRY_RESULT.md`

필수 섹션: TASK, STATUS, SUMMARY, READ, MASTER BACKLOG CHECK, MAP01_11 GATE CHECK, CREATED, PREEXISTING_IDENTICAL, INPUT GATE, REGISTRY CONTRACT, REVERSE INDEX, TEST, UNITY, ASSET META VALIDATION, CHANGE SCOPE, OUT_OF_SCOPE_FINDINGS, DONE CONDITIONS, NEXT, Recommended Commit.

## DONE CONDITIONS

- [ ] Current MAP01_12, Master 205, MAP01_11 COMPLETE/PASS
- [ ] exact four definition roots + successful FK result; identity consistency gate
- [ ] immutable snapshot, typed/generic lookup, stable record enumeration
- [ ] schema-declared incoming/outgoing reverse index only
- [ ] error accumulation/no partial Registry
- [ ] hash/singleton/atomic publish/report/domain validation absent
- [ ] Runtime 6 + test 1 + meta 7 only; existing files/CSV/asmdef unchanged
- [ ] new >=36, targeted >=528, full >=571 PASS; compile/warning 0/0
- [ ] Result complete; MAP01_13 not started

## Completion Rule

exact `STATUS: PASS`와 모든 condition 충족 시만 MAP01_12를 COMPLETE로 finalize하고 Current Task를 NONE으로 만든다. MAP01_13은 LOCKED로 유지하며 자동 생성·실행하지 마.

## Recommended Commit

`feat(map): build immutable static data registry`
