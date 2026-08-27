```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP10_07_CREATE_PATTERN_PREVIEW_AND_FOCUSED_TESTS
  task_file: TASKS/MAP10_07_CREATE_PATTERN_PREVIEW_AND_FOCUSED_TESTS.md
  requires_current_task: NONE
  requires_completed_task: MAP10_06_AUTHOR_STARTER_24_MICROPATTERNS
  requires_result:
    path: REPORTS/MAP10_06_AUTHOR_STARTER_24_MICROPATTERNS_RESULT.md
    status: PASS
    sha256: 5cb5b408af6a7c04c42dcc530f25835c8b35eb4b4eeb85e4afa90c189c31915c
  requires_installed_task:
    path: TASKS/MAP10_06_AUTHOR_STARTER_24_MICROPATTERNS.md
    sha256: aef482a6cbed31ba2ab039bb5ef4c13006392156c856441e9590ba9e7de714d9
  sets_current_task: MAP10_07_CREATE_PATTERN_PREVIEW_AND_FOCUSED_TESTS
```

# MAP10_07 — Create Pattern Preview and Focused Tests

```text
TASK: MAP10_07_CREATE_PATTERN_PREVIEW_AND_FOCUSED_TESTS
PHASE: MAP10 — 4×4 MicroPattern Authoring / Rendering
STATUS: CURRENT
NEXT: MAP10_08_MAP10_PATTERN_EXIT_TESTS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Responsibility

이번 Task는 starter 24개를 사람이 직접 검사할 수 있는 read-only Editor Preview와, 실제 MAP10_01~06 API를 통과하는 focused inspection fixture를 만든다.

```text
physical CSV import
→ original/transformed cells
→ protected plan or rejection
→ ordered render before/after diff
→ signature/digest/evidence
```

| 소유 | 소유하지 않음 |
|---|---|
| read-only MicroPattern EditorWindow | 새 generation/runtime 알고리즘 |
| original/transform/protection/diff 표시 | CSV 편집/export/auto-fix |
| clean/protected/conflict fixtures | MAP10 Exit 승인 |
| focused preview-model tests | MAP11 Cluster 배치/Tilemap |

## 1. Regression and Preflight

정상 실행은 category `MAP10_07`만 선택한다.

```text
Prior MAP00~10_06 selections: 0
Legacy 19347 selections: 0
```

focused 실패, compile/Console 오류, 승인 CSV/hash/manifest drift, existing 파일 변경, asmdef/GUID 위반이 실제 발생한 경우에만 owner·원인·최소 관련 selection을 기록한다.

읽기 전용 baseline:

```text
Catalog definitions / cell rows: 24 / 453
Catalog digest:
6a5aefd2eb368348d594158cc3f14e94d0ea509ea2cdd207a7715e8da80d19ac

Catalog CSV SHA-256:
f9d9e9cc60c4e4d7561c5aa6502228c18fc9566e3e0febab206ea3264b408267
Cells CSV SHA-256:
e702ae5d02d7ec9d2cda129c1361699e37d942c280c8f9bd1f3200f155084381

Full 52-file Authoring manifest:
4415ae4af5196d6793f5d0152c0688e5bf35dc4ad23442791e45d3cfd81d0851
Generated CSV: 0
```

corrected content totals는 `AddSolid 54 / CarveAir 41 / Geometry NoChange 289 / all non-NoChange 164`다. MAP10_06의 상세 templates와 Result가 정본이며 잘못된 이전 aggregate `52/41/291/162`를 사용하지 않는다.

## 2. File and Assembly Boundary

신규 Editor production:

```text
Assets/_Game/Editor/MapAuthoring/WorldGeneration/MicroPatterns/MicroPatternPreviewModel.cs(.meta)
Assets/_Game/Editor/MapAuthoring/WorldGeneration/MicroPatterns/MicroPatternPreviewWindow.cs(.meta)
```

신규 focused Editor test:

```text
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/MicroPatterns/MicroPatternPreviewTests.cs(.meta)
```

```text
Editor namespace: StarNight.MapAuthoring.WorldGeneration.MicroPatterns
Test namespace: StarNight.MapAuthoring.Tests.EditMode.WorldGeneration.MicroPatterns
Assemblies: existing MapAuthoring.Editor / MapAuthoring.Tests.EditMode
```

기존 C#/test/CSV/meta/asmdef를 수정하지 않는다. Preview는 project data를 읽기만 하며 asset import/save, Scene/Prefab mutation, Generated write를 수행하지 않는다.

## 3. Preview Model — Existing Authority Only

최소 surface:

```text
MicroPatternPreviewFixtureKind
MicroPatternPreviewRequest
MicroPatternPreviewCell
MicroPatternPreviewWrite
MicroPatternPreviewDiff
MicroPatternPreviewSnapshot
MicroPatternPreviewBuildResult / Error
MicroPatternPreviewModel
MicroPatternPreviewCanonicalDigest
```

model은 다음 existing public API를 호출한다.

1. MAP10_01 physical two-file importer
2. MAP10_02 coordinate transform/protected application planner
3. MAP10_03 ordered renderer와 immutable delta
4. MAP10_05 silhouette signature

transform/mask/render/signature 로직을 Editor에서 재구현하거나 단순화한 복제본을 만들지 않는다. UI는 published immutable evidence만 표시한다.

## 4. Exact Preview Fixtures

```text
Clean
ProtectedOverlap
SameLayerConflict
```

### Clean

- 선택한 pattern과 allowed transform을 사용한다.
- protected mask는 empty다.
- `OperationWitness` before target을 사용한다.
  - effective AddSolid target은 before Air
  - effective CarveAir target은 before Solid
  - 다른 Geometry target은 before Air
  - five payload layers는 empty
- 모든 non-NoChange operation이 before/after diff에 보이도록 하는 inspection fixture이며 gameplay canvas라고 주장하지 않는다.

### ProtectedOverlap

- selected transformed pattern의 첫 canonical non-NoChange target을 protected source `TraversalEnvelope`로 지정한다.
- `RejectCandidate` pattern은 plan rejection, renderer 미호출, delta/digest 없음이 보여야 한다.
- `ForceNoChange` pattern은 해당 target이 effective NoChange가 되고 나머지 write만 render된다.
- protected provenance와 rejection/masked-write evidence를 표시한다.

### SameLayerConflict

- fixed starter pair `MP_CRATER_DUST_PATCH`와 `MP_ROOT_SAP_PATCH`를 같은 origin에서 Material layer가 겹치도록 사용한다.
- MAP10_03 actual renderer가 different semantic Material payload conflict를 반환해야 한다.
- partial delta/digest는 없어야 한다.
- 이는 failure inspection fixture이며 biome 조합 규칙이나 실제 generator 후보라고 주장하지 않는다.

모든 fixture는 input/data를 변경하지 않으며 같은 request는 같은 snapshot digest를 낸다.

## 5. Preview Snapshot Content

snapshot은 최소 다음을 포함한다.

```text
selected pattern ID / biome / role group / weight / protected policy
allowed transforms / selected transform
definition and catalog digests
original 4×4 canonical cells
transformed 4×4 cells
protected mask/provenance and plan status/digest
before and after six-layer states
stage-ordered writes and cell/layer diffs
silhouette Add/Carve masks and digest
errors/conflicts/no-publication evidence
```

Grid cell compact tokens:

```text
G+ AddSolid | G- CarveAir | S Surface | A Affordance
M Material  | H Hazard   | K Marker  | · NoChange
P protected border
```

actual payload ID는 tooltip/detail panel에 표시한다. color는 보조 표현이며 token/text 없이 색만으로 의미를 전달하지 않는다.

## 6. EditorWindow Contract

```text
Menu: Tools/MapDesign/MicroPattern Preview
Title: MicroPattern Preview
```

Window 기능:

- physical CSV를 명시적 `Reload` 버튼과 first open에서 read-only import
- biome filter와 exact 24 pattern selector
- selected pattern의 allowed transform selector만 표시
- fixture selector `Clean / ProtectedOverlap / SameLayerConflict`
- Original / Transformed / Protected-Effective / Before / After 4×4 panels
- stage `10 Geometry → 20 Surface → 30 Affordance → 40 Material → 50 Hazard → 60 Marker` audit list
- changed cell/layer diff와 digest/error panel
- import/build failure는 inline error로 표시하고 exception loop/Console spam 없음

금지:

- CSV edit/save/export, auto-repair, file watcher, continuous AssetDatabase refresh
- Generate/Apply/Commit 버튼
- Scene object, Tilemap, Prefab, SO, Texture asset 생성
- static mutable preview cache 또는 domain-reload persistence

## 7. Focused Verification

category `MAP10_07`만 실행한다.

1. physical import exact `24/453`, catalog digest/hash/manifest exact
2. biome filters `6/6/6/6`, exact role groups `12/4/8`
3. all exact 24 IDs selectable
4. all `56` allowed pattern-transform pairs build Clean snapshot successfully
5. original→transformed coordinates match MAP10_02 evidence
6. Clean OperationWitness exposes every non-NoChange write in diff
7. ordered write stages are exact and no cross-layer implicit clear
8. 12 RejectCandidate ProtectedOverlap fixtures reject/no renderer publication
9. 12 ForceNoChange ProtectedOverlap fixtures mask target and preserve provenance
10. SameLayerConflict returns MAP10_03 atomic conflict/no partial delta
11. definition/plan/render/signature/preview digests repeat and input order independence
12. zero/non-zero silhouette evidence matches MAP10_06 `12/12`
13. immutable snapshot/caller mutation resistance
14. menu/window opens, default pattern renders five 4×4 panels, reload succeeds, Console error 0
15. CSV/asset/Scene/Prefab/Generated mutation 0

UI test는 pixel-perfect screenshot 비교가 아니라 model evidence, panel cardinality, selector/menu binding과 exception-free render를 검증한다. 실제 layout은 Unity에서 window open/close로 확인한다.

## 8. Change Boundary

허용:

- 신규 Editor preview model/window C# + meta
- 신규 focused Editor test C# + meta
- installed/archive Task, Result, PASS 후 Status Finalize

금지:

- existing production/test/CSV/meta 수정
- Authoring/Generated/asset/SO/Scene/Prefab/Settings/Packages 변경
- MAP10_01~06 API/content 수정 또는 로직 복제
- cleanup/global validation/cluster generation/Tilemap bake
- MAP10_08 Exit audit 선실행
- 문제 trigger 없는 이전/legacy test 실행
- unrelated stage/commit, Git push

## 9. Required Result

```text
MAP10_07_CREATE_PATTERN_PREVIEW_AND_FOCUSED_TESTS_RESULT.md
```

상단:

```text
TASK: MAP10_07_CREATE_PATTERN_PREVIEW_AND_FOCUSED_TESTS
STATUS: PASS | FAIL | BLOCKED
MAP10_07: COMPLETE ELIGIBLE | NOT COMPLETE
MAP10_08_MAP10_PATTERN_EXIT_TESTS: LOCKED / DO NOT START
```

첫 구현 섹션은 반드시 `Responsibility and Added Functions`다.

| Field | Required report |
|---|---|
| Task responsibility | read-only preview와 actual-pipeline focused inspection |
| Added functions | model/window/fixture/grid/diff/digest/error 표시 기능 |
| Inputs consumed | MAP10_01 importer와 MAP10_02~06 public outputs |
| Outputs produced | immutable preview snapshots와 clean/protected/conflict evidence |
| Explicit non-ownership | data edit/Exit audit/cluster/Tilemap/runtime generation 미구현 |
| Downstream consumers | MAP10_08 Exit audit와 MAP11 authoring inspection |

이후 file inventory, window/model/fixture 기능, 24×transform/protection/diff/hash evidence, visual open check, focused/regression policy, static/change scope, commit handoff를 기록한다.

```text
MAP10_07 focused: discovered/executed/pass/fail/skip
REGRESSION TRIGGER DETECTED: NO | YES(owner/reason/minimum scope)
PRIOR TASK TEST SELECTIONS: 0 (normal path)
LEGACY TEST SELECTIONS: 0 (normal path)
```

static gate:

```text
compile/Console/relevant warning: 0/0/0
MicroPattern CSV hashes and 24/453 rows unchanged
full Authoring manifest 4415ae... unchanged
Generated CSV: 0
existing MAP00~10_06 production/test modifications: 0
other roots/asmdef/Scene/Prefab/Settings/Packages changes: 0
duplicate GUID/unapplied candidate/diff-check: 0/0/0
unrelated staged/included: 0
```

PASS일 때만 Finalize하고 task-owned preview/test/protocol 파일만 atomic commit한다.

```text
Subject: MAP10_07: add MicroPattern preview
Push: NOT PERFORMED
```

MAP10_08을 자동 시작하지 않는다.
