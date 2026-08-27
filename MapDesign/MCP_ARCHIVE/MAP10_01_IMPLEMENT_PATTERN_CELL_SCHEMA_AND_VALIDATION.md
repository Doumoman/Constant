```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP10_01_IMPLEMENT_PATTERN_CELL_SCHEMA_AND_VALIDATION
  task_file: TASKS/MAP10_01_IMPLEMENT_PATTERN_CELL_SCHEMA_AND_VALIDATION.md
  requires_current_task: NONE
  requires_completed_task: MAP09_08_MAP09_CONTRACT_EXIT_AUDIT
  requires_result:
    path: REPORTS/MAP09_08_MAP09_CONTRACT_EXIT_AUDIT_RESULT.md
    status: PASS
    sha256: 2f10d253e0966436db688682242b9d9527a9f307c859d2cc112feb96e95ae45e
  requires_installed_task:
    path: TASKS/MAP09_08_MAP09_CONTRACT_EXIT_AUDIT.md
    sha256: 4fe0df3798ad504118b5d09719b8eead3a1ef045842fbdfaec18f7d4f373e72d
  sets_current_task: MAP10_01_IMPLEMENT_PATTERN_CELL_SCHEMA_AND_VALIDATION
```

# MAP10_01 — Implement Pattern Cell Schema and Validation

```text
TASK: MAP10_01_IMPLEMENT_PATTERN_CELL_SCHEMA_AND_VALIDATION
PHASE: MAP10 — 4×4 MicroPattern Authoring / Rendering
STATUS: CURRENT
NEXT: MAP10_02_IMPLEMENT_PATTERN_TRANSFORMS_AND_PROTECTED_MASK
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Task Responsibility

이번 Task는 MAP09에서 승인한 MicroPattern 계약과 V2 CSV schema를 실제 Authoring 입력 경계에 연결한다.

```text
두 MicroPattern CSV의 exact header 설치
→ RFC4180 row 읽기
→ catalog/cell row grouping
→ 4×4의 16 unique cell 검증
→ MAP09_03 MicroPatternDefinition으로 atomic publish
```

| 이번 Task가 책임지는 기능 | 책임지지 않는 기능 |
|---|---|
| 두 CSV의 exact path/header와 전용 importer | starter 24개 콘텐츠 제작 |
| catalog/cell row DTO와 token codec | transform 좌표 실행 |
| 16-cell coverage, layer/operation/payload 검증 | protected mask 적용 |
| 오류 누적과 atomic catalog publish | renderer, selector, RNG, cleanup |

## 1. No-Regression Policy

정상 경로:

```text
MAP10_01 focused only
Prior MAP00~10_00 test selections: 0
Legacy 19347 selections: 0
```

실제 문제 trigger:

- MAP10_01 focused 실패
- compile/Console error
- MAP09 Phase Exit live digest mismatch
- 허용된 두 CSV 이외 기존 Authoring/production/test drift
- asmdef/GUID/ownership violation

trigger가 없으면 이전 Task/category 및 legacy 회귀 실행을 금지한다. Trigger가 있으면 owner/원인/최소 selection을 Result에 먼저 기록하고 관련 범위만 실행한다.

이번 Task가 정확히 두 header-only CSV/meta를 추가해 Authoring 전체 수를 `50→52`로 바꾸는 것은 승인된 task delta이며 regression trigger가 아니다. 기존 50-file subset의 bytes/hash가 달라질 때만 drift다.

## 2. Preflight

읽기 전용 확인:

1. MAP09_08 Result/설치/Archive Task SHA exact, `MAP09 PHASE EXIT: APPROVED`
2. MAP10_01만 CURRENT, inbox candidate 0
3. MAP09_03 MicroPattern contract/digest와 MAP09_07 15-table schema/digest exact
4. 기존 RFC4180 reader, header/field validator, scalar/list parser의 public API
5. approved Runtime `MicroPatterns`, Editor `Import`, 두 focused test roots
6. 기존 Authoring CSV/meta `50/50`, legacy subset manifest exact
7. 두 target CSV/meta가 아직 없고 Generated CSV가 0
8. asmdef/GUID/compile/Console/dirty worktree

```text
MAP09 MicroPattern digest:
42c88cdb30154f098593d0e3be65063111613612fe5e9e1b9b11f2d9f1297a3d

V2 schema registry digest:
272ec4f449a17179158720c94e92f6982cb5a32427ce6f6ea8ffc5eb92050621

Legacy 50-file Authoring subset manifest:
f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb
```

기존 API 수정, target collision, predecessor mismatch면 자동 보정하지 말고 `BLOCKED`다.

## 3. Physical Schema Files

다음 두 파일만 생성한다.

```text
Assets/_Game/Map/Data/WorldGeneration/Authoring/MicroPattern/
  micro_pattern_catalog_v2.csv
  micro_pattern_cells_v2.csv
```

규칙:

- UTF-8 BOM + MAP09_07 registry descriptor 순서의 exact header + final newline만 가진다.
- data row는 `0`이다. starter pattern은 MAP10_06 책임이다.
- matching `.csv.meta` 두 개만 Unity가 생성한다.
- 기존 `CSV_DATA_DICTIONARY.csv`와 legacy 50 CSV/meta는 수정하지 않는다.
- V2 importer는 두 exact path만 읽으며 recursive discovery를 하지 않는다.
- legacy 49-file source set/registry에 두 V2 파일을 주입하지 않는다.
- `Generated/`에는 아무 파일도 만들지 않는다.

Result에 두 exact header, CSV/meta SHA, GUID, 전체 Authoring `52/52`, legacy subset manifest, 새 전체 manifest를 기록한다.

## 4. Runtime Row-to-Contract Builder

구현 위치:

```text
Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/
Namespace: StarNight.Map.WorldGeneration.MicroPatterns
```

최소 surface:

```text
MicroPatternCatalogRowV2
MicroPatternCellRowV2
MicroPatternAuthoringCatalog
MicroPatternCellSchemaError / Result
MicroPatternCellSchemaBuilder
```

기존 `MicroPatternId`, layer/operation/transform/protected enums, `MicroPatternDefinition`, `MicroPatternValidator`, biome identity를 재사용한다. 중복 authority type을 만들지 않는다.

Builder 입력은 parsed catalog rows와 cell rows다. Runtime production에서는 filesystem, UnityEditor, clock, RNG를 사용하지 않는다.

### 4.1 Catalog rows

- header/field 의미는 MAP09_07의 `micro_pattern_catalog_v2.csv` descriptor가 정본이다.
- pattern ID는 `^MP_[A-Z0-9_]+$`이며 unique하다.
- weight, biome IDs, transforms, protected policy를 기존 MAP09_03 타입으로 exact parse한다.
- unknown/duplicate/empty token과 unsupported enum fallback을 금지한다.
- catalog row가 없는 orphan cell과 cell row가 없는 catalog pattern을 거부한다.

### 4.2 Cell rows and exact 16 cells

CSV operation token → Runtime operation:

| CSV token | Runtime |
|---|---|
| `NO_CHANGE` | `NoChange` |
| `ADD_SOLID` | `AddSolid` |
| `CARVE_AIR` | `CarveAir` |
| `SURFACE` | `SetSurface` |
| `AFFORDANCE` | `SetAffordance` |
| `MATERIAL` | `SetMaterial` |
| `HAZARD` | `SetHazard` |
| `MARKER` | `SetMarker` |

CSV layer token:

```text
GEOMETRY | SURFACE | AFFORDANCE | MATERIAL | HAZARD | MARKER
```

한 pattern마다:

- `(x,y)` unique coordinate set은 exact 16개다.
- 좌표 범위는 `x=0..3`, `y=0..3`, canonical index는 `y*4+x`다.
- 같은 coordinate에 여러 layer row는 허용하지만 `(pattern,x,y,layer)` duplicate는 금지한다.
- 각 coordinate는 최소 한 row를 가져 explicit cell이어야 한다.
- 생략 layer는 기존 contract의 canonical `NoChange`로 정규화한다.
- 완전 빈 셀 표현은 `GEOMETRY + NO_CHANGE + empty payload` 한 row를 사용한다.

Exact compatibility:

| Layer | 허용 operation |
|---|---|
| `GEOMETRY` | `NO_CHANGE`, `ADD_SOLID`, `CARVE_AIR` |
| `SURFACE` | `NO_CHANGE`, `SURFACE` |
| `AFFORDANCE` | `NO_CHANGE`, `AFFORDANCE` |
| `MATERIAL` | `NO_CHANGE`, `MATERIAL` |
| `HAZARD` | `NO_CHANGE`, `HAZARD` |
| `MARKER` | `NO_CHANGE`, `MARKER` |

- `NO_CHANGE`, `ADD_SOLID`, `CARVE_AIR` payload는 empty여야 한다.
- 나머지는 `^[A-Z][A-Z0-9_]*$` stable payload가 필요하다.
- trimmed alias, case-insensitive fallback, unknown operation/layer는 거부한다.

### 4.3 Atomic publication

- 모든 row/schema/domain 오류를 accumulated, deduplicated, stable-sort한다.
- 오류가 하나라도 있으면 catalog/definition/digest를 publish하지 않는다.
- 성공 시 pattern ID ordinal order의 immutable catalog를 게시한다.
- 같은 semantic input은 row order와 상관없이 같은 pattern/catalog digest를 낸다.
- builder는 마지막에 기존 `MicroPatternValidator`를 호출하며 그 불변식을 우회하지 않는다.

Header-only project CSV는 schema 설치 상태로 허용하지만 content catalog로 publish하지 않는다. Focused tests의 in-memory/temp fixture에서 성공/실패 content import를 검증한다.

## 5. Exact Editor Importer

구현 위치:

```text
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Import/
Namespace: StarNight.MapAuthoring.WorldGeneration.Import
```

최소 surface:

```text
MicroPatternCsvImporterV2
MicroPatternCsvImportResult / Error
```

- 두 exact Authoring path만 입력으로 받는다.
- 기존 RFC4180 reader와 schema/header/field validation public API를 재사용한다.
- BOM, exact header/order, row provenance(file/record/field)를 보존한다.
- parse error와 Runtime builder error를 하나의 stable report로 합친다.
- 오류 시 partial catalog, asset, cache, generated file을 쓰지 않는다.
- import 성공도 Scene/SO/prefab/global singleton을 변경하지 않는다.
- export, auto-repair, file watcher, Editor Window는 구현하지 않는다.

## 6. Error Groups

최소 구분:

```text
MissingInputFile | InvalidBom | HeaderMismatch | RowFieldCountMismatch
InvalidCatalogField | DuplicatePatternId | OrphanCellRow | MissingCellRows
InvalidCoordinate | MissingCell | DuplicateCellLayer
UnknownLayer | UnknownOperation | LayerOperationMismatch
MissingPayload | UnexpectedPayload | InvalidPayload
DomainValidationFailed | AtomicPublishRejected
```

모든 오류는 정확한 file/record/pattern/coordinate/layer context를 가능한 범위에서 가진다.

## 7. Change Boundary

허용:

- 두 header-only MicroPattern CSV + matching meta
- `Runtime/.../MicroPatterns/` 신규 builder/row/catalog C# + meta
- `Editor/.../Import/` 신규 V2 importer C# + meta
- 대응 Runtime/Editor focused test C# + meta
- 설치/Archive Task, Result, PASS 후 Finalize Status

금지:

- 기존 C#/test/CSV/meta 수정
- 실제 pattern data row 또는 starter pattern 생성
- transform/protected mask/renderer/RNG/cleanup 구현
- legacy dictionary/registry/source set 변경
- Generated CSV, asset, SO, Scene, Prefab, Editor Window 생성
- asmdef/Settings/Packages 변경
- 문제 trigger 없는 이전/legacy test 실행
- unrelated stage/commit, Git push

## 8. Focused Validation

Category `MAP10_01`만 실행한다.

1. 두 exact header/BOM/path와 header-only project state
2. exact operation/layer token codec, unknown token rejection
3. 4×4/16 unique coordinate 성공
4. missing/duplicate/out-of-range coordinate 실패
5. same-cell distinct layers 허용, duplicate layer 실패
6. layer/operation/payload matrix
7. catalog/cell FK, orphan/missing pattern rejection
8. existing MAP09_03 validator reuse와 atomic publish
9. immutable catalog, row-order-independent digest
10. exact importer path/header/provenance/error ordering
11. legacy 50 subset unchanged, total Authoring 52, Generated 0
12. transform/renderer/RNG/file write side effect 없음

Result 필수 기록:

```text
MAP10_01 focused: discovered/executed/pass/fail/skip
REGRESSION TRIGGER DETECTED: NO | YES(reason)
PRIOR TASK TEST SELECTIONS: 0 (정상 경로)
LEGACY TEST SELECTIONS: 0 (정상 경로)
```

Static gate:

```text
compile/Console/relevant warning: 0/0/0
legacy Authoring subset CSV/meta: 50/50 byte-unchanged
new V2 MicroPattern CSV/meta: 2/2, header rows only
total Authoring CSV/meta: 52/52
Generated CSV: 0
existing MAP00~09 modifications: 0
other V2 roots/asmdef/Scene/Prefab/Settings/Packages changes: 0
duplicate GUID/unapplied candidate/diff-check: 0/0/0
unrelated staged/included: 0
```

## 9. Required Result Report

Result:

```text
MAP10_01_IMPLEMENT_PATTERN_CELL_SCHEMA_AND_VALIDATION_RESULT.md
```

상단:

```text
TASK: MAP10_01_IMPLEMENT_PATTERN_CELL_SCHEMA_AND_VALIDATION
STATUS: PASS | FAIL | BLOCKED
MAP10_01: COMPLETE ELIGIBLE | NOT COMPLETE
MAP10_02_IMPLEMENT_PATTERN_TRANSFORMS_AND_PROTECTED_MASK: LOCKED / DO NOT START
```

첫 구현 섹션은 반드시 `Responsibility and Added Functions`다.

| 필드 | 필수 내용 |
|---|---|
| Task responsibility | MicroPattern CSV schema/import/16-cell validation 책임 |
| Added functions | 새 row/builder/catalog/importer와 실제 기능 |
| Inputs consumed | MAP09_03 contract, MAP09_07 schema, existing CSV APIs |
| Outputs produced | header schema, immutable authoring catalog, import evidence/digest |
| Explicit non-ownership | transform/mask/renderer/RNG/starter content 등 미구현 |
| Downstream consumers | MAP10_02~08이 소비할 validated pattern input |

이후 predecessor/status/dirty preflight, 파일 inventory, CSV header/hash/GUID, builder/importer/error evidence, focused/regression policy, Unity/static/change scope, commit handoff를 기록한다.

PASS일 때만 Finalize하고 task-owned 파일만 atomic commit한다.

```text
Subject: MAP10_01: implement MicroPattern CSV cell validation
Push: NOT PERFORMED
```

MAP10_02를 자동 시작하지 않는다.
