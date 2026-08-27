```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP10_03_IMPLEMENT_ORDERED_PATTERN_RENDERER
  task_file: TASKS/MAP10_03_IMPLEMENT_ORDERED_PATTERN_RENDERER.md
  requires_current_task: NONE
  requires_completed_task: MAP10_02_IMPLEMENT_PATTERN_TRANSFORMS_AND_PROTECTED_MASK
  requires_result:
    path: REPORTS/MAP10_02_IMPLEMENT_PATTERN_TRANSFORMS_AND_PROTECTED_MASK_RESULT.md
    status: PASS
    sha256: cc7363af39dcd11fa7a545aa6a2301306dec94b268cf85fd33b9003a41865a03
  requires_installed_task:
    path: TASKS/MAP10_02_IMPLEMENT_PATTERN_TRANSFORMS_AND_PROTECTED_MASK.md
    sha256: 9eaa39d6063127b4d4bd19533b0b586aff29094807841ad16fc3320c076ad163
  sets_current_task: MAP10_03_IMPLEMENT_ORDERED_PATTERN_RENDERER
```

# MAP10_03 — Implement Ordered Pattern Renderer

```text
TASK: MAP10_03_IMPLEMENT_ORDERED_PATTERN_RENDERER
PHASE: MAP10 — 4×4 MicroPattern Authoring / Rendering
STATUS: CURRENT
NEXT: MAP10_04_IMPLEMENT_BIOME_PROFILES_CANDIDATES_AND_RNG
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Task Responsibility

이번 Task는 MAP10_02의 renderer-ready application plan을 working cell snapshot에 **정해진 layer 순서와 명시적 충돌 규칙**으로 적용해 immutable delta를 만든다.

```text
validated application plans
→ same-layer conflict 검사
→ Geometry Add/Carve
→ Surface
→ Affordance
→ Material
→ Hazard
→ Marker
→ immutable before/after delta
```

| 책임 | 추가 기능 | 소유하지 않는 기능 |
|---|---|---|
| stage ordering | exact 6-stage write sequence | candidate 선택/RNG |
| layer mutation | bool geometry와 stable payload layer delta | Tilemap/Unity object mutation |
| overlap policy | identical coalesce / conflicting reject | 임의 last-write fallback |
| provenance | request/pattern/plan/write evidence | cleanup/physical validity 판정 |

## 1. No-Regression Policy

정상 경로:

```text
MAP10_03 focused only
Prior MAP00~10_02 test selections: 0
Legacy 19347 selections: 0
```

실제 문제 trigger:

- MAP10_03 focused 실패
- compile/Console error
- MAP10_02 plan behavior 또는 Authoring manifest mismatch
- 기존 production/test/CSV/meta drift
- asmdef/GUID/ownership violation

trigger가 없으면 이전 Task/category와 legacy 회귀를 실행하지 않는다. Trigger가 있으면 owner·원인·최소 selection을 Result에 먼저 기록하고 관련 범위만 실행한다.

## 2. Preflight

읽기 전용 확인:

1. MAP10_02 Result/설치/Archive Task SHA exact
2. MAP10_03만 CURRENT, root inbox candidate 0
3. MAP10_01 authoring catalog와 MAP10_02 application-plan public API compile/live
4. MAP09_06 SectorCanvas layer/provenance semantics와 Unvalidated/Validated 경계
5. existing `LocalTileCoord`, stable ID/digest helpers
6. Authoring CSV/meta `52/52`, full manifest exact, Generated CSV 0
7. asmdef/GUID/compile/Console/dirty worktree

```text
MAP10_01 catalog fixture digest:
1b2524bf8af6be7ae3b2d03134096a4efdf8f856ea500863ec5dcd26114f0c35

Full 52-file Authoring manifest:
b49b6ebc4a65c26302ff08deb9100dbf727200e16b3588092b7cd53f63d214ba
```

MAP10_02 plan을 수정해야 하거나 existing authority와 type collision이 있으면 자동 보정하지 말고 `BLOCKED`다.

## 3. Working Render Target

구현 위치:

```text
Runtime: Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/
Test:    Assets/_Game/Tests/EditMode/Map/WorldGeneration/MicroPatterns/
Namespace: StarNight.Map.WorldGeneration.MicroPatterns
```

최소 surface:

```text
MicroPatternRenderRequestId
MicroPatternRenderRequest
MicroPatternRenderCellState
MicroPatternRenderTarget
```

request:

- stable request ID grammar `^MPR_[A-Z0-9_]+$`
- successful MAP10_02 `MicroPatternApplicationPlan`
- request ID는 batch 안에서 unique하다.

working cell state:

```text
Target coordinate
Solid: bool
Surface: stable ID or empty
Affordance: stable ID or empty
Material: stable ID or empty
Hazard: stable ID or empty
Marker: stable ID or empty
Per-layer existing provenance
```

- render target은 모든 request target coordinate의 exact union을 한 번씩 가진다.
- missing/duplicate/extra target coordinate를 거부한다.
- input state와 provenance는 defensive copy/read-only다.
- `Validated SectorCanvas`를 직접 수정하거나 stamp를 재발급하지 않는다.
- output은 후속 working-canvas composer가 소비할 delta이며 최종 Tilemap이 아니다.

## 4. Exact Render Stages

```text
10 Geometry:   AddSolid / CarveAir
20 Surface:    SetSurface
30 Affordance: SetAffordance
40 Material:   SetMaterial
50 Hazard:     SetHazard
60 Marker:     SetMarker
```

규칙:

- 모든 request의 stage 10 write를 완료한 뒤 stage 20으로 이동한다.
- stage 안에서는 target coordinate `y,x`, layer, request ID ordinal로 audit record를 정렬한다.
- `NoChange`는 write와 conflict를 만들지 않고 기존 값/provenance를 그대로 둔다.
- `AddSolid`는 `Solid=true`, `CarveAir`는 `Solid=false`다.
- `Set*`는 해당 payload layer 하나만 바꾼다.
- 다른 layer를 암묵적으로 clear하거나 추론하지 않는다.
- CarveAir 뒤 Surface/Material 등이 남는 cross-layer 상태의 물리 적합성은 TileValidation/cleanup 책임이다. renderer가 임의 삭제하지 않는다.
- protected cell은 MAP10_02 plan에서 이미 all-`NoChange`이며 renderer가 보호 정책을 다시 계산하지 않는다.

## 5. Same-Cell Conflict Contract

write identity:

```text
target coordinate + destination layer + semantic value
```

같은 `(target coordinate, destination layer)`에 여러 non-`NoChange` write가 있을 때:

1. semantic value가 같으면 하나의 write로 coalesce한다.
2. 참여한 request/pattern/plan provenance는 모두 ordinal unique list로 보존한다.
3. semantic value가 다르면 `ConflictingLayerWrite`다.
4. conflict가 하나라도 있으면 batch 전체를 atomic reject한다.
5. first/last input order, request ID 순서, weight로 승자를 고르지 않는다.

예시:

```text
AddSolid + AddSolid          -> coalesce
CarveAir + CarveAir          -> coalesce
AddSolid + CarveAir          -> reject
Material(MAT_A) + MAT_A      -> coalesce
Material(MAT_A) + MAT_B      -> reject
Surface(SURF_A) + Hazard(HZ) -> different layer, both allowed
NoChange + any write         -> write only
```

conflict evidence는 coordinate, layer, 각 semantic value, request ID, pattern ID, plan digest를 stable-sort해 보존한다. Reject 시 partial target/delta/digest를 publish하지 않는다.

## 6. Immutable Delta and Provenance

최소 surface:

```text
MicroPatternRenderStage
MicroPatternLayerWrite
MicroPatternRenderConflict
MicroPatternRenderedCellDelta
MicroPatternRenderDelta
MicroPatternRenderResult / Error
MicroPatternOrderedRenderer
MicroPatternRenderCanonicalDigest
```

성공 delta:

- touched coordinate만 canonical order로 포함한다.
- 각 cell의 before/after six-layer state를 포함한다.
- 각 applied/coalesced write의 stage, operation, semantic value를 포함한다.
- request ID, source pattern ID/digest, application plan digest와 protected-mask provenance를 보존한다.
- input target/plan/provenance 객체를 변경하지 않는다.
- idempotent write도 write evidence와 provenance는 보존하되 before/after equality를 명시한다.

digest 포함:

```text
render ruleset version
canonical request identities and plan digests
canonical input target cells/provenance
stage-ordered coalesced writes
before/after deltas and source provenance
```

timestamp, display text, object hash, input/reflection/file order, RNG는 제외한다.

## 7. Atomic Validation

최소 error groups:

```text
MissingInput | InvalidRequestId | DuplicateRequestId
InvalidApplicationPlan | PlanDigestMismatch
MissingTargetCell | DuplicateTargetCell | ExtraTargetCell
InvalidLayerState | InvalidExistingProvenance
UnsupportedOperation | LayerOperationMismatch
ConflictingLayerWrite | AtomicRenderRejected
```

- error/conflict를 accumulated, deduplicated, stable-sort한다.
- error가 하나라도 있으면 delta/digest를 publish하지 않는다.
- success collection은 defensive copy/read-only다.
- reversed request/target enumeration은 같은 delta/digest를 낸다.

## 8. Change Boundary

허용:

- `Runtime/.../MicroPatterns/` 신규 render target/conflict/delta/renderer C# + meta
- 대응 Runtime EditMode focused test C# + meta
- 설치/Archive Task, Result, PASS 후 Finalize Status

금지:

- 기존 C#/test/CSV/meta 수정
- Authoring/Generated CSV 변경 또는 생성
- MAP10_02 transform/mask/plan 수정
- candidate pool, biome profile, RNG, selection 구현
- repetition signature, cleanup, TileValidation 구현
- actual SectorCanvas stamp, Tilemap, Scene, Prefab, SO, Editor 변경
- asmdef/Settings/Packages 변경
- 문제 trigger 없는 이전/legacy test 실행
- unrelated stage/commit, Git push

## 9. Focused Validation

Category `MAP10_03`만 실행한다.

1. exact six-stage order across multiple requests/cells
2. AddSolid/CarveAir bool mutation과 layer-local Set mutation
3. NoChange preserves value/provenance
4. exact target union, missing/duplicate/extra rejection
5. same-layer identical write coalescing/provenance union
6. same-layer different write atomic rejection
7. different-layer same-cell writes allowed in stage order
8. idempotent write evidence와 before/after equality
9. protected all-NoChange cell mutation 0
10. immutable input/output와 accumulated stable issues
11. reversed enumeration same delta/digest
12. renderer scope에 RNG/file/Unity lifecycle/Tilemap side effect 0

Result 필수 기록:

```text
MAP10_03 focused: discovered/executed/pass/fail/skip
REGRESSION TRIGGER DETECTED: NO | YES(reason)
PRIOR TASK TEST SELECTIONS: 0 (정상 경로)
LEGACY TEST SELECTIONS: 0 (정상 경로)
```

Static gate:

```text
compile/Console/relevant warning: 0/0/0
Authoring CSV/meta: 52/52 byte-unchanged
full Authoring manifest: b49b6e... exact
Generated CSV: 0
existing MAP00~10_02 modifications: 0
other roots/Editor/asmdef/Scene/Prefab/Settings/Packages changes: 0
duplicate GUID/unapplied candidate/diff-check: 0/0/0
unrelated staged/included: 0
```

## 10. Required Result Report

Result:

```text
MAP10_03_IMPLEMENT_ORDERED_PATTERN_RENDERER_RESULT.md
```

상단:

```text
TASK: MAP10_03_IMPLEMENT_ORDERED_PATTERN_RENDERER
STATUS: PASS | FAIL | BLOCKED
MAP10_03: COMPLETE ELIGIBLE | NOT COMPLETE
MAP10_04_IMPLEMENT_BIOME_PROFILES_CANDIDATES_AND_RNG: LOCKED / DO NOT START
```

첫 구현 섹션은 반드시 `Responsibility and Added Functions`다.

| 필드 | 필수 내용 |
|---|---|
| Task responsibility | ordered layer mutation과 overlap conflict 경계 |
| Added functions | target/request/write/conflict/delta/renderer/digest 기능 |
| Inputs consumed | MAP10_02 renderer-ready plans와 working cell states |
| Outputs produced | immutable render delta 또는 atomic conflict evidence |
| Explicit non-ownership | selection/RNG/cleanup/TileValidation/Tilemap 미구현 |
| Downstream consumers | MAP10_04~08과 MAP11 cluster pattern renderer |

이후 predecessor/status/dirty preflight, 파일 inventory, stage/conflict/delta/digest evidence, focused/regression policy, Unity/static/change scope, commit handoff를 기록한다.

PASS일 때만 Finalize하고 task-owned 파일만 atomic commit한다.

```text
Subject: MAP10_03: implement ordered MicroPattern renderer
Push: NOT PERFORMED
```

MAP10_04를 자동 시작하지 않는다.
