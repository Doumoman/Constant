```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP13_08_CREATE_SPECIAL_VALIDATOR_AND_PREVIEW
  task_file: TASKS/MAP13_08_CREATE_SPECIAL_VALIDATOR_AND_PREVIEW.md
  requires_current_task: NONE
  requires_completed_task: MAP13_07_AUTHOR_FORGE_BOSS_AND_OPTIONAL_REGIONS
  requires_result:
    path: REPORTS/MAP13_07_AUTHOR_FORGE_BOSS_AND_OPTIONAL_REGIONS_RESULT.md
    status: PASS
    sha256: 6098f22f0eab0f05342ef228edfdfea8039e37d86c957c3c6706d71d476f0ee9
  requires_installed_task:
    path: TASKS/MAP13_07_AUTHOR_FORGE_BOSS_AND_OPTIONAL_REGIONS.md
    sha256: 1ddd61aacf2d8e35c03a790ef459f08286e3a4afc526b9194a8a2c456048e20e
  sets_current_task: MAP13_08_CREATE_SPECIAL_VALIDATOR_AND_PREVIEW
```

# MAP13_08 — SpecialRegion Validator and Preview

```text
TASK: MAP13_08_CREATE_SPECIAL_VALIDATOR_AND_PREVIEW
PHASE: MAP13 — SpecialRegion / Village / Mandatory Landmarks
STATUS: CURRENT
NEXT: MAP13_09_MAP13_SPECIAL_REGION_EXIT_TESTS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP13_01~07이 게시한 public plan을 하나의 read-only validation audit로 검사하고, Village/CoreResource/Landmark의 shell·buffer·layers·routes·states·reset을 Unity Editor에서 선택해 볼 수 있는 preview를 추가한다.

```text
current public MAP13 plans
→ SpecialRegionValidationAuditor
→ immutable audit report / stable errors
→ read-only Editor preview model/window
```

이번 Task의 검증은 과거 category나 legacy `19347`을 다시 실행하는 회귀가 아니다. 새 auditor/preview 기능 자체를 `MAP13_08` focused EditMode로만 검증한다.

현재 프로젝트에는 physical SpecialRegion CSV 또는 live world-placement catalog가 없다. Editor preview는 public compilers로 만든 deterministic in-memory **REFERENCE FIXTURE**를 표시하며 실제 생성 월드라고 주장하지 않는다. Merchant/Maru는 exact **DEFERRED TO MAP14**로 표시한다.

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 정확한 script 경로, class/method별 input→output, audit/preview 실제 수치, 새 기능, 파이프라인 위치, 미구현, Editor/게임 가시성을 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| exact MAP13 validation audit model/compiler | MAP03/MAP14 placement solver |
| footprint/buffer/site/layer/route/state/reset cross-proof | player physics/gameplay simulation |
| deterministic reference-fixture preview projection | physical CSV/importer/live world source |
| read-only Editor selector/grid/legend/details/errors | authoring edit·auto-fix·save/export |
| audit/preview digest and stable errors | Scene/Prefab/Tilemap mutation |
| focused Editor test | MAP13_09 Phase Exit approval |

Preview가 PASS여도 실제 월드 placement, player reachability, gameplay object 또는 production content 완료를 의미하지 않는다.

## 2. Focused-Only and No-Regression Policy

정상 실행은 EditMode category `MAP13_08`만 선택한다.

```text
MAP13_08 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13_01~07 selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
```

신규 focused test 안에서 current public API를 호출하는 것은 과거 category 재실행이 아니다. task-owned failure는 신규 allowlist 파일만 수정하고 `MAP13_08`만 재실행한다.

existing authority defect를 발견하면 기존 파일을 수정하거나 회귀를 실행하지 않는다. owner/invariant/reason/minimum verification을 Result에 기록하고 `BLOCKED`로 STOP한다.

## 3. Read-Only Preflight

```text
MAP13_07 Result: PASS
MAP13_07 Result SHA-256:
6098f22f0eab0f05342ef228edfdfea8039e37d86c957c3c6706d71d476f0ee9

MAP13_07 installed Task SHA-256:
1ddd61aacf2d8e35c03a790ef459f08286e3a4afc526b9194a8a2c456048e20e

MAP13_07 COMPLETE / MAP13_08 CURRENT / MAP13_09 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required authority:

```text
MAP13_01 site bridge and coordinate provenance
MAP13_02 entry/return/apron/buffer/collision/priority plans
MAP13_03 fixed collision/access/replaceable layers and persistence safety
MAP13_04 three Village shell/facility/access plans
MAP13_05 five Village state variants
MAP13_06 three CoreResource solution plans
MAP13_07 four landmark placed/deferred plans
```

public authority가 없거나 existing source modification이 필요하면 `BLOCKED`다.

## 4. Exact Write Boundary

정상 범위는 Runtime 2개, Editor production 2개, focused Editor test 1개와 matching meta다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionValidationAudit.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionValidationAuditor.cs(.meta)

Assets/_Game/Editor/MapAuthoring/WorldGeneration/SpecialRegions/SpecialRegionPreviewModel.cs(.meta)
Assets/_Game/Editor/MapAuthoring/WorldGeneration/SpecialRegions/SpecialRegionPreviewWindow.cs(.meta)
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/SpecialRegions/SpecialRegionValidationPreviewTests.cs(.meta)
```

필요한 신규 `SpecialRegions` folder meta는 허용하지만 기존 folder meta 수정은 금지한다.

```text
Runtime: Game.Map.Runtime / StarNight.Map.WorldGeneration.SpecialRegions
Editor: Game.Map.Editor / StarNight.Map.Editor.WorldGeneration.SpecialRegions
Tests: Game.Map.Editor.Tests / StarNight.Map.Editor.Tests.WorldGeneration.SpecialRegions
Category: MAP13_08
```

수정·생성 금지:

```text
existing C# / test / CSV / meta
V2 schema registry/test and Authoring/Generated CSV/meta
asmdef / asmref
Scene / Prefab / ScriptableObject / Tilemap / Material / Texture
Settings / Packages
PlayMode test/helper
```

watcher, static mutable cache, auto refresh loop, serializer, export file는 추가하지 않는다.

## 5. Exact Audit Input Matrix

Auditor는 exact 10개 artifact를 canonical family/ID 순서로 받는다.

```text
Village:       3 — 1×1 / 2×1 / 1×2
CoreResource:  3 — MoonCore / CassiaSap / StarNuruk
Landmark:      4 — MoonSealForge / BossSealArena / MerchantCave / MaruShrine
Total:        10
```

Editor preview model은 current public constructors/compilers로 exact 10개 deterministic reference input을 만든다. parser/validator/graph logic을 복제하지 않고 MAP13_01~07 public API의 결과만 auditor에 전달한다.

표시와 audit identity:

| Artifact | Audit binding label | 의미 |
|---|---|---|
| Village 3, Core 3, Forge, Boss | `REFERENCE FIXTURE` | contract-compatible in-memory placed example; live world가 아님 |
| Merchant, Maru | `DEFERRED TO MAP14` | local authoring plan; world/reservation claim 0 |

`REFERENCE FIXTURE`를 `PLACED`, `LIVE`, `GENERATED`, `PRODUCTION`으로 표시하지 않는다.

## 6. Runtime Validation Audit

프로젝트 naming/style에 맞추되 다음 semantic surface를 제공한다.

```text
SpecialRegionAuditFamily: Village / CoreResource / Landmark
SpecialRegionAuditBinding: ReferenceFixture / DeferredToMAP14
SpecialRegionAuditSection
SpecialRegionAuditArtifactInput
SpecialRegionAuditRequest
SpecialRegionAuditSectionResult
SpecialRegionAuditArtifactResult
SpecialRegionValidationReport
SpecialRegionValidationAuditor.Audit
SpecialRegionValidationCanonicalDigest
SpecialRegionValidationErrorCode / Error / Result
```

Auditor는 source object를 변경하지 않고 existing public plan/digest/witness를 교차 검증한다.

### 6.1 Footprint, binding and buffer

- placed reference는 `1×1 / 2×1 / 1×2` footprint dimensions, region-wide bounds와 sector coverage를 exact 확인한다.
- 2×1/1×2는 internal sector seam crossing evidence가 있어야 한다.
- bridge reservation/region/kind/transform/sector row identity가 source digests와 일치해야 한다.
- Entry/Return exterior ports, internal aprons, bidirectional witness와 Before/After Quiet buffer evidence를 확인한다.
- collision priority/accepted/rejected owner evidence를 보존하고 global reorder/removal을 주장하지 않는다.
- deferred optional은 footprint/world origin/reservation/bridge/buffer/fixed-slot/placed ownership claim이 exact `0`이어야 한다.

### 6.2 Fixed and replaceable layers

- FixedCollision과 FixedAccess를 별도 section으로 표시하고 replaceable target이 아님을 확인한다.
- Facility/Npc/Enemy/Event/Reward exact slot kind, coordinate, required flag, persistence identity를 확인한다.
- fixed/access/slot overlap, missing owner, source digest drift는 atomic audit failure다.
- replacement 전후 shell/route/persistence digest가 동일해야 한다.

### 6.3 Ordered route proof

Family별 ordered witness:

```text
Village:
  Entry → central road → Facility door/access → road → Return

CoreResource:
  Entry → environment trigger(s) → required Reward → Return
  Failure → RecoveryJoin → Low route

Forge:
  Entry → Grind → Mix → Press → MoonlightCure → MoonSeal → Return
  stage failure → ManualReset → SafeCorridor → Low route

Boss:
  SealGate → Arena → Return
  fall/failure → central recovery

Merchant / Maru:
  local Entry → optional interaction/choice → same local Return
```

- mandatory witnesses are no-tool.
- High/optional branches rejoin Recovery/Return.
- synthetic edge, teleport, carve, solver-generated path와 world mutation은 `0`이다.
- static graph proof이며 player collider/physics reachability를 주장하지 않는다.

### 6.4 State, reset and persistence proof

- Village exact five variants와 shell identity 불변
- Core required Reward exact one, 7 checkpoints, permanent loss/duplicate risk 0
- Forge process order/resource-return/MoonSeal key proof
- Boss gate/encounter reset/seal acceptance preservation and new-movement-rule 0
- Merchant Available/Visited/Departed shell/return identity
- Maru preview-before-choice, PersistentChoice, reroll/duplicate benefit 0
- state/marker 변화의 collision/route/coordinate/persistence writes 0

### 6.5 Atomic report

Success report:

```text
10 canonical artifact results
section PASS/FAIL counts
binding labels
source/component/artifact/aggregate digests
route/state/reset/persistence summary counts
zero mutation/solver/gameplay counters
```

Any error는 report/digests `0`; errors는 accumulated, deduped, stable-sorted다. reverse input/repeat/`tr-TR`에서 동일하다.

Minimum error groups:

```text
MissingInput | DuplicateArtifact | MissingArtifact | IdentityMismatch | DigestMismatch
FootprintMismatch | MissingSectorCoverage | MissingSeamCrossing
SiteBindingMismatch | BufferMismatch | CollisionOwnerMismatch
FixedReplaceableOverlap | PersistenceMismatch
MissingRouteWitness | RouteOrderMismatch | MandatoryToolDependency | UnrecoverableFailure
StateVariantMismatch | ResetMismatch | ResourceLossRisk | DuplicateBenefitRisk
DeferredWorldClaim | MutationClaim | NonCanonicalPublication
```

## 7. Read-Only Editor Preview Model

`SpecialRegionPreviewModel` public entry:

```text
BuildDefault()
Build(selection, viewMode, toggles)
TrySelectArtifact(...)
TrySelectViewMode(...)
Reload()
```

Model output은 immutable snapshot 또는 ordered atomic error다.

Exact selectors:

```text
Family: Village / CoreResource / Landmark
Artifact: exact 10
View: Overview / Footprint / Layers / Routes / States / Reset / Audit / Compare
```

Exact overlay toggles:

```text
DesignChunks / SectorSeams / EntryReturn / ApronsBuffers
FixedCollision / FixedAccess / ReplaceableSlots
LowRoute / HighRoute / RecoveryRoute / RequiredReward
StateMarkers / ResetMarkers
```

Preview snapshot은 다음을 포함한다.

- grid/frame bounds와 scale-to-fit cells/markers
- region ID, family, kind/theme, design size, active chunks
- `REFERENCE FIXTURE` 또는 `DEFERRED TO MAP14` status banner
- Entry/Return, buffer, fixed/access/slot, route, reward, state/reset tokens
- audit section PASS/FAIL, error code/path/detail
- source/component/artifact/audit digests
- provenance label과 `PHYSICS NOT VERIFIED` 표시

Meaning은 text/token/shape와 color를 함께 사용하며 color alone으로 전달하지 않는다.

## 8. Editor Window

Exact menu/title:

```text
Menu:  Tools/MapDesign/Special Region Validator & Preview
Title: Special Region Validator & Preview
Minimum size: 1000×680
```

Window responsibilities:

- explicit `Reload` button
- family/artifact/view selectors와 overlay toggles
- scrollable scale-to-fit grid, text legend, details/audit/error panel
- selected artifact의 binding/provenance/physics warning을 항상 표시
- invalid snapshot은 inline errors만 표시하고 exception/partial grid를 게시하지 않음

금지 UI/actions:

```text
Save / Apply / Fix / Generate / Bake / Export
CSV/asset/Scene/Prefab/Tilemap edit
automatic filesystem watcher or auto-reload loop
gameplay object spawn
```

창을 열고 Reload/selection/toggle을 사용해도 Scene hierarchy/root count, dirty state, selection, Authoring/Generated inventory가 변하지 않아야 한다.

## 9. Focused Tests

`SpecialRegionValidationPreviewTests`에서 최소 다음을 검증한다.

1. exact 10 artifact selector와 3/3/4 family matrix
2. 8 `REFERENCE FIXTURE` + 2 `DEFERRED TO MAP14` label
3. placed footprint/bounds/coverage/seam/site/buffer audit
4. fixed/access/five slot kind/persistence separation
5. family별 ordered Entry→trigger/facility/reward→Return witness
6. every failure→Recovery/Return과 no-tool/synthetic mutation 0
7. Village states, Core loss, Forge refund, Boss reset, Merchant/Maru optional state audit
8. deferred optional world/reservation/bridge/placed claims 0
9. reverse/repeat/`tr-TR`/immutability/digest 안정성
10. invalid digest/duplicate/missing/overlap/route/state/reset/deferred claim atomic failure
11. window menu/title/min-size/selectors/toggles/banner/legend/error contract
12. open→Reload→selection/toggle→close에서 Scene/asset/filesystem/Generated mutation 0

test가 existing validator/graph traversal을 복제하거나 physics/PlayMode/gameplay를 흉내 내지 않는다.

## 10. Verification and Required Result

Unity refresh/compile 후 `MAP13_08` EditMode만 실행한다.

```text
discovered = executed = passed
failed / skipped / inconclusive = 0 / 0 / 0
compile / relevant Console error = 0 / 0
prior category / legacy / PlayMode / unfiltered selections = 0 / 0 / 0 / 0
```

실제 Unity Editor에서 window를 한 번 열어 메뉴/title/min-size/default snapshot/labels를 확인하고 mutation 없이 닫는다. 이것은 `MAP13_08` 신규 기능 확인이며 PlayMode/회귀 실행이 아니다.

Static gate:

```text
new Runtime C#/meta: 2/2
new Editor production C#/meta: 2/2
new focused Editor test C#/meta: 1/1
existing C#/test/CSV/meta modifications: 0
new/modified Authoring or Generated CSV/meta: 0
schema registry/test modifications: 0
PlayMode/Scene/Prefab/Tilemap/Settings/Packages changes: 0
duplicate GUID: 0
unapplied candidate/diff-check/unrelated staged: 0/0/0
Git push: NOT PERFORMED
```

Result 경로:

```text
MapDesign/MCP/REPORTS/MAP13_08_CREATE_SPECIAL_VALIDATOR_AND_PREVIEW_RESULT.md
```

상단 verdict:

```text
TASK: MAP13_08_CREATE_SPECIAL_VALIDATOR_AND_PREVIEW
STATUS: PASS | BLOCKED
MAP13_08: COMPLETE ELIGIBLE | NOT COMPLETE
MAP13_09_MAP13_SPECIAL_REGION_EXIT_TESTS: LOCKED / DO NOT START
```

`## User-Facing Implementation Report`에서 다음을 실제 수치로 보고한다.

- 신규/수정 script 전체 경로와 class/method별 input→output
- audit artifact/section/pass/fail/error와 family별 route/state/reset 실제 수
- selector/view/toggle/legend/banner 수와 실제 window open evidence
- preview에서 사용자가 무엇을 확인할 수 있는지
- `REFERENCE FIXTURE`/`DEFERRED TO MAP14`/`PHYSICS NOT VERIFIED` 구분
- 새로 가능해진 기능과 파이프라인 위치
- 아직 미구현한 physical/live placement, CSV, gameplay/physics, MAP13_09+
- Editor/게임 가시성

`## Responsibility and Added Functions`에는 아래를 표로 보고한다.

| Field | Required evidence |
|---|---|
| Task responsibility | exact MAP13 audit + read-only Editor preview |
| Added scripts | Runtime 2 + Editor 2 + focused Editor test 1 exact paths |
| Added functions | public type/method별 sole responsibility와 input→output |
| Inputs consumed | MAP13_01~07 public plans/catalogs/digests |
| Outputs produced | immutable audit report/preview snapshots/digests/errors |
| Explicit non-ownership | live placement/CSV, physics/gameplay, edit/apply/generate/bake/export, Scene/Prefab/Tilemap |
| Downstream consumer | 별도 검수 후 MAP13_09만 unlock 가능 |

그 뒤 focused test, window/static side effects, regression selections, task-owned files와 commit handoff를 기록한다.

정상 문구:

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
```

PASS일 때만 Status Finalize 후 task-owned 파일만 atomic commit한다.

```text
Subject: MAP13_08: add SpecialRegion validator and preview
Push: NOT PERFORMED
```

Result가 PASS여도 MAP13_09를 자동 시작하지 않고 별도 검수까지 LOCKED로 유지한다.
