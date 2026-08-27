```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP10_02_IMPLEMENT_PATTERN_TRANSFORMS_AND_PROTECTED_MASK
  task_file: TASKS/MAP10_02_IMPLEMENT_PATTERN_TRANSFORMS_AND_PROTECTED_MASK.md
  requires_current_task: NONE
  requires_completed_task: MAP10_01_IMPLEMENT_PATTERN_CELL_SCHEMA_AND_VALIDATION
  requires_result:
    path: REPORTS/MAP10_01_IMPLEMENT_PATTERN_CELL_SCHEMA_AND_VALIDATION_RESULT.md
    status: PASS
    sha256: 326d24c70d490fe610e8b0abfaf4716d2ee06287f7ebb56330c0255a4a42dec8
  requires_installed_task:
    path: TASKS/MAP10_01_IMPLEMENT_PATTERN_CELL_SCHEMA_AND_VALIDATION.md
    sha256: 091750188c62b978bf4381c081610ac54be881a18c405ecd872c16e61eccfd34
  sets_current_task: MAP10_02_IMPLEMENT_PATTERN_TRANSFORMS_AND_PROTECTED_MASK
```

# MAP10_02 — Implement Pattern Transforms and Protected Mask

```text
TASK: MAP10_02_IMPLEMENT_PATTERN_TRANSFORMS_AND_PROTECTED_MASK
PHASE: MAP10 — 4×4 MicroPattern Authoring / Rendering
STATUS: CURRENT
NEXT: MAP10_03_IMPLEMENT_ORDERED_PATTERN_RENDERER
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Task Responsibility

이번 Task는 validated `MicroPatternDefinition`을 선택된 transform과 placement에 투영하고, 필수 이동/경계/특수 진입 보호영역을 침범하지 않는 immutable application plan으로 준비한다.

```text
validated 4×4 pattern
→ R0 / MirrorX / MirrorY / R180
→ target origin 배치
→ protected-source union 교차
→ ForceNoChange 또는 RejectCandidate
→ renderer-ready plan
```

| 책임 | 추가 기능 | 소유하지 않는 기능 |
|---|---|---|
| 좌표 변환 | exact 4 transform과 16-cell coverage | R90/R270/arbitrary transform |
| 보호 mask | 4개 source union과 provenance | Spine/Envelope 자체 계산 |
| 정책 적용 | `ForceNoChange`, `RejectCandidate` | 실제 Canvas/tile mutation |
| 결과 게시 | immutable plan, issue, digest | renderer order/conflict/RNG |

## 1. No-Regression Policy

정상 경로:

```text
MAP10_02 focused only
Prior MAP00~10_01 test selections: 0
Legacy 19347 selections: 0
```

회귀 허용 trigger:

- MAP10_02 focused 실패
- compile/Console error
- MAP10_01 catalog/schema/Authoring digest mismatch
- 기존 production/test/CSV/meta drift
- asmdef/GUID/ownership violation

trigger가 없으면 이전 Task/category와 legacy 회귀를 실행하지 않는다. Trigger가 있으면 Result에 owner·원인·최소 선택 범위를 먼저 기록하고 관련 범위만 실행한다.

## 2. Preflight

읽기 전용 확인:

1. MAP10_01 Result/설치/Archive Task SHA exact
2. MAP10_02만 CURRENT, root inbox candidate 0
3. MAP09_03 MicroPattern contract와 MAP10_01 builder/catalog live digest exact
4. MAP09_04 Spine/Envelope public protected coordinate API
5. MAP08 ProtectedOpen public evidence와 MAP09_06 Special fixed-entry contract
6. existing `LocalTileCoord`, transform/protected policy authority
7. Authoring CSV/meta `52/52`, Generated CSV `0`, 기존 file hash
8. asmdef/GUID/compile/Console/dirty worktree

```text
MAP09 MicroPattern fixture digest:
42c88cdb30154f098593d0e3be65063111613612fe5e9e1b9b11f2d9f1297a3d

MAP10_01 authoring catalog fixture digest:
1b2524bf8af6be7ae3b2d03134096a4efdf8f856ea500863ec5dcd26114f0c35

Full 52-file Authoring manifest:
b49b6ebc4a65c26302ff08deb9100dbf727200e16b3588092b7cd53f63d214ba
```

기존 authority type 수정/복제 필요, predecessor mismatch, allowlist collision이면 `BLOCKED`다.

## 3. Exact 4×4 Transforms

구현 위치:

```text
Runtime: Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/
Test:    Assets/_Game/Tests/EditMode/Map/WorldGeneration/MicroPatterns/
Namespace: StarNight.Map.WorldGeneration.MicroPatterns
```

기존 `MicroPatternTransform` enum을 재사용한다.

| Transform | `(x,y)` → transformed local coordinate |
|---|---|
| `R0` | `(x, y)` |
| `MirrorX` | `(3-x, y)` |
| `MirrorY` | `(x, 3-y)` |
| `R180` | `(3-x, 3-y)` |

규칙:

- source는 validated exact `4×4/16-cell` definition이어야 한다.
- requested transform은 definition의 allowed transform set에 포함돼야 한다.
- output도 exact 16 unique cells, coordinate `0..3`, canonical `y*4+x` order다.
- layer/operation/payload는 좌표 외에는 byte/semantic unchanged다.
- source definition과 collection을 수정하지 않는다.
- undefined enum, `R90`, `R270`, scale/translation alias를 거부한다.
- transform 결과 digest는 input enumeration order와 무관하다.

최소 surface:

```text
TransformedMicroPattern
MicroPatternTransformResult / Error
MicroPatternTransformer
```

## 4. Placement and Protected Sources

최소 surface:

```text
MicroPatternPlacement
MicroPatternProtectedSourceKind
MicroPatternProtectedCell
MicroPatternProtectedMask
MicroPatternProtectedMaskBuilder
```

placement는 target-local `LocalTileCoord Origin`을 가진다.

```text
targetX = originX + transformedX
targetY = originY + transformedY
```

coordinate overflow를 거부한다. Target Canvas bounds 판정은 renderer/상위 caller 책임이므로 이번 Task에서 임의 크기를 만들지 않는다.

exact protected source kinds:

```text
RouteSpine
TraversalEnvelope
BoundaryProtectedOpen
SpecialFixedEntry
```

- 각 protected cell은 target coordinate, source kind, stable source ID를 가진다.
- source ID는 non-empty stable ID여야 한다.
- 여러 source가 같은 target cell을 보호하면 coordinate는 하나로 합치고 provenance는 ordinal unique list로 보존한다.
- pattern의 4×4 placement와 교차하지 않는 protected cell은 결과 mask/digest에 포함하지 않는다.
- input enumeration과 duplicate provenance는 결과를 바꾸지 않는다.
- 이 Task는 Spine/Envelope/ProtectedOpen/entry를 계산하지 않고 승인된 coordinate set만 소비한다.

## 5. Protected Policy Application

non-write는 해당 cell의 모든 canonical instruction이 `NoChange`인 상태다. 하나라도 non-`NoChange` operation이면 protected write다.

### 5.1 `ForceNoChange`

- protected target cell과 겹치는 모든 write instruction을 canonical `NoChange`/empty payload로 바꾼다.
- 한 coordinate의 모든 6 layer 결과가 `NoChange`가 된다.
- unprotected cell의 instruction은 변형하지 않는다.
- masked coordinate, 제거된 write 수, source provenance를 audit evidence로 보존한다.
- source/transformed definition은 수정하지 않고 별도 application plan만 게시한다.

### 5.2 `RejectCandidate`

- protected cell에 write가 하나라도 겹치면 전체 candidate를 reject한다.
- partial plan, partially masked definition, digest를 publish하지 않는다.
- 충돌 coordinate와 모든 source provenance를 stable issue로 반환한다.
- protected cell의 source operation이 이미 all-`NoChange`라면 reject하지 않는다.

최소 surface:

```text
MicroPatternPreparedCell
MicroPatternProtectedHit
MicroPatternApplicationPlan
MicroPatternApplicationResult / Error
MicroPatternApplicationPlanner
MicroPatternApplicationCanonicalDigest
```

## 6. Atomicity and Digest

- error를 accumulated, deduplicated, stable-sort한다.
- transform/placement/mask/policy 오류가 하나라도 있으면 plan/digest를 publish하지 않는다.
- 성공 plan은 source pattern ID/digest, transform, origin, protected policy, 16 local/target cells, final instructions, intersecting mask provenance를 가진다.
- 모든 collection은 defensive copy/read-only다.
- 성공 digest는 위 semantic 값과 masked evidence를 canonical order로 포함한다. Reject 결과는 issue evidence만 반환하고 digest를 게시하지 않는다.
- timestamp, display text, object hash, input/reflection/file order, RNG는 제외한다.
- 같은 input은 반복/역순 enumeration에서도 같은 plan/digest를 낸다.

## 7. Change Boundary

허용:

- `Runtime/.../MicroPatterns/` 신규 transform/mask/plan C# + matching meta
- 대응 Runtime EditMode focused test C# + matching meta
- 설치/Archive Task, Result, PASS 후 Finalize Status

금지:

- 기존 C#/test/CSV/meta 수정
- Authoring/Generated CSV 변경 또는 생성
- Spine/Envelope/ProtectedOpen/Special entry 계산 로직 구현
- tile layer mutation, ordered renderer, conflict resolution
- candidate selection, biome pool, RNG, repetition, cleanup
- Scene/Prefab/SO/Editor/asmdef/Settings/Packages 변경
- 문제 trigger 없는 이전/legacy test 실행
- unrelated stage/commit, Git push

## 8. Focused Validation

Category `MAP10_02`만 실행한다.

1. R0/MirrorX/MirrorY/R180 exact asymmetric coordinate mapping
2. transformed 16-cell exact coverage/canonical order
3. instruction/payload preservation과 source immutability
4. transform allowlist/undefined/unsupported rejection
5. placement target coordinate와 overflow rejection
6. four protected source kinds와 duplicate union/provenance
7. transformed coordinate 기준 protected intersection
8. `ForceNoChange`의 6-layer masking과 unprotected preservation
9. `RejectCandidate` all-or-nothing과 all-NoChange overlap 허용
10. multiple-source hit evidence stable order
11. atomic publish/read-only/row-order-independent digest
12. renderer/RNG/file/Unity lifecycle side effect 0

Result 필수 기록:

```text
MAP10_02 focused: discovered/executed/pass/fail/skip
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
existing MAP00~10_01 modifications: 0
other roots/Editor/asmdef/Scene/Prefab/Settings/Packages changes: 0
duplicate GUID/unapplied candidate/diff-check: 0/0/0
unrelated staged/included: 0
```

## 9. Required Result Report

Result:

```text
MAP10_02_IMPLEMENT_PATTERN_TRANSFORMS_AND_PROTECTED_MASK_RESULT.md
```

상단:

```text
TASK: MAP10_02_IMPLEMENT_PATTERN_TRANSFORMS_AND_PROTECTED_MASK
STATUS: PASS | FAIL | BLOCKED
MAP10_02: COMPLETE ELIGIBLE | NOT COMPLETE
MAP10_03_IMPLEMENT_ORDERED_PATTERN_RENDERER: LOCKED / DO NOT START
```

첫 구현 섹션은 반드시 `Responsibility and Added Functions`다.

| 필드 | 필수 내용 |
|---|---|
| Task responsibility | transform/placement/protected policy 경계 |
| Added functions | 새 transformer, mask, planner, evidence, digest 기능 |
| Inputs consumed | validated pattern과 four protected coordinate sources |
| Outputs produced | immutable renderer-ready application plan 또는 rejection evidence |
| Explicit non-ownership | renderer/RNG/selector/cleanup/source 계산 미구현 |
| Downstream consumers | MAP10_03 renderer와 MAP11 cluster pattern zone |

그 뒤 predecessor/status/dirty preflight, 파일 inventory, four transform evidence, mask/policy/digest evidence, focused/regression policy, Unity/static/change scope, commit handoff를 기록한다.

PASS일 때만 Finalize하고 task-owned 파일만 atomic commit한다.

```text
Subject: MAP10_02: implement MicroPattern transforms and protected mask
Push: NOT PERFORMED
```

MAP10_03을 자동 시작하지 않는다.
