```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP10_05_IMPLEMENT_REPETITION_SIGNATURE_AND_LOCAL_CLEANUP
  task_file: TASKS/MAP10_05_IMPLEMENT_REPETITION_SIGNATURE_AND_LOCAL_CLEANUP.md
  requires_current_task: NONE
  requires_completed_task: MAP10_04_IMPLEMENT_BIOME_PROFILES_CANDIDATES_AND_RNG
  requires_result:
    path: REPORTS/MAP10_04_IMPLEMENT_BIOME_PROFILES_CANDIDATES_AND_RNG_RESULT.md
    status: PASS
    sha256: c5179d833cf74c0db26b8c729600f2bd8ecd8a099722c3b99d814eb9d54feb6d
  requires_installed_task:
    path: TASKS/MAP10_04_IMPLEMENT_BIOME_PROFILES_CANDIDATES_AND_RNG.md
    sha256: 6a864e561b2426679dbb82ecb2d6c83fa27c818a223ebff812e3eba9f44051bf
  sets_current_task: MAP10_05_IMPLEMENT_REPETITION_SIGNATURE_AND_LOCAL_CLEANUP
```

# MAP10_05 — Implement Repetition Signature and Local Cleanup

```text
TASK: MAP10_05_IMPLEMENT_REPETITION_SIGNATURE_AND_LOCAL_CLEANUP
PHASE: MAP10 — 4×4 MicroPattern Authoring / Rendering
STATUS: CURRENT
NEXT: MAP10_06_AUTHOR_STARTER_24_MICROPATTERNS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Responsibility

이번 Task는 effective 4×4 geometry의 mirror-invariant silhouette signature를 만들고, 같은 Pattern의 세 번째 연속 후보를 RNG 전에 제거하며, 정확히 판별 가능한 local geometry 결함만 immutable cleanup delta로 제안한다.

```text
effective plans → silhouette signature
accepted history + candidate sources → third-repeat exclusion → MAP10_04 index/selector
rendered solid snapshot + halo + protection → local defect proposals → immutable cleanup delta
```

| 소유 | 소유하지 않음 |
|---|---|
| mirror-invariant geometry signature | biome/content 작성 |
| exact Pattern 3연속 방지 | RNG 재추첨/후보 생성 |
| 1셀 noise/head snag/boxed-bottom pit local cleanup | 전역 경로·물리 완주 판정 |
| cleanup evidence/delta/digest | Tilemap/SectorCanvas 직접 mutation |

## 1. Regression and Preflight

정상 실행은 category `MAP10_05`만 선택한다.

```text
Prior MAP00~10_04 selections: 0
Legacy 19347 selections: 0
```

focused 실패, compile/Console 오류, 승인 digest/Authoring drift, existing 파일 변경, asmdef/GUID 위반이 실제 발생한 경우에만 owner·원인·최소 관련 selection을 Result에 먼저 기록하고 재검증한다.

읽기 전용 확인:

1. MAP10_04 Result/installed/archive Task exact hash와 Status
2. MAP10_02 successful plan/effective transformed operations/protected provenance
3. MAP10_03 immutable render target/delta와 six-layer cell state
4. MAP10_04 candidate source/index/selection public API
5. MAP09_04 envelope protection sources와 `LocalTileCoord`
6. Authoring `52/52`, full manifest 아래 값, Generated CSV 0

```text
MAP10_01 catalog fixture digest:
1b2524bf8af6be7ae3b2d03134096a4efdf8f856ea500863ec5dcd26114f0c35

Full Authoring manifest:
b49b6ebc4a65c26302ff08deb9100dbf727200e16b3588092b7cd53f63d214ba
```

기존 MAP10_02~04 authority를 수정해야 진행 가능하면 자동 보정하지 말고 `BLOCKED`다.

## 2. Mirror-Invariant Silhouette Signature

구현 위치:

```text
Runtime: Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/
Test:    Assets/_Game/Tests/EditMode/Map/WorldGeneration/MicroPatterns/
Namespace: StarNight.Map.WorldGeneration.MicroPatterns
```

최소 surface:

```text
MicroPatternSilhouetteSignature
MicroPatternSilhouetteSignatureBuilder
MicroPatternSilhouetteCanonicalDigest
```

입력은 successful MAP10_02 application plan의 **protected 처리 후 effective geometry operations**다.

```text
AddSolid mask: 16-bit, canonical index y*4+x
CarveAir mask: 16-bit, canonical index y*4+x
```

signature 규칙:

1. effective `AddSolid`와 `CarveAir`만 포함한다.
2. Surface/Affordance/Material/Hazard/Marker, payload, weight, biome, RNG는 제외한다.
3. `(AddMask, CarveMask)`를 R0/MirrorX/MirrorY/R180 네 방식으로 변환한다.
4. packed unsigned pair가 가장 작은 variant를 canonical pair로 선택한다.
5. tie는 `R0 < MirrorX < MirrorY < R180` 순서다.
6. SHA-256 ruleset `MAP10_05_SILHOUETTE_V1`에 canonical pair와 ruleset만 기록한다.

따라서 서로 mirror-equivalent인 effective geometry는 같은 signature이고, 다른 geometry는 mask evidence로 구분된다. geometry write가 없는 pattern도 명시적 zero signature를 가질 수 있지만, 이것만으로 서로 다른 Pattern ID를 동일 pattern으로 간주하지 않는다.

## 3. Exact Three-In-A-Row Guard

최소 surface:

```text
MicroPatternAcceptedHistoryItem
MicroPatternRepetitionContext
MicroPatternRepetitionExclusion
MicroPatternRepetitionGuardResult / Error
MicroPatternThirdRepeatGuard
```

caller가 stable placement sequence 순서와 직전 accepted history를 제공한다. 이 Task가 world adjacency나 route 순서를 추측하지 않는다.

정책:

- 직전 두 accepted item의 exact `MicroPatternId`가 같을 때만 그 ID의 candidate source를 제외한다.
- transform과 silhouette signature가 달라도 같은 Pattern ID면 세 번째 연속 후보에서 제외한다.
- 직전 두 ID가 다르면 제외 0이다.
- 다른 Pattern ID가 같은 silhouette signature를 가져도 이 Task에서는 제외하지 않는다.
- 비교는 exact ordinal ID이며 prefix/substring/biome 추측을 금지한다.
- filtering은 MAP10_04 candidate index/RNG보다 먼저 수행한다. 선택 후 reroll하거나 RNG draw를 버리지 않는다.
- allowed source와 exclusion evidence는 canonical order/read-only다.
- 모두 제외되면 explicit `NoCandidateAfterThirdRepeatGuard`; fallback pattern/RNG draw 없이 상위 solver retry 소유로 반환한다.

focused integration은 다음 순서를 증명한다.

```text
candidate sources
→ ThirdRepeatGuard
→ MAP10_04 CandidateIndexBuilder
→ MAP10_04 selector
```

기존 MAP10_04 파일/API를 수정하거나 selection 로직을 복제하지 않는다.

## 4. Local Cleanup Snapshot

최소 surface:

```text
MicroPatternCleanupCell
MicroPatternCleanupSnapshot
MicroPatternCleanupIssue
MicroPatternCleanupProposal
MicroPatternCleanupCellDelta
MicroPatternLocalCleanupResult / Error
MicroPatternLocalCleanup
MicroPatternCleanupCanonicalDigest
```

snapshot은 다음을 immutable하게 가진다.

```text
owned target coordinates
read-only one-cell halo coordinates
Solid bool
protected bool + canonical protected provenance
```

- proposal은 owned target에만 가능하며 halo는 판정용이다.
- target이 protected면 어떤 cleanup write도 금지한다.
- 필요한 이웃이 없으면 추측하지 않고 `InsufficientNeighborhood` issue만 남긴다.
- 모든 rule은 원본 snapshot에서 동시에 검출한다. 앞선 수정 결과를 다음 rule 입력으로 재사용하는 cascade pass는 금지한다.
- Geometry `Solid`만 변경한다. 다른 five layer를 clear/infer하지 않고 후속 validation 대상으로 남긴다.

## 5. Exact Local Cleanup Rules

좌표의 `Up/Down/Left/Right`는 existing `LocalTileCoord` 방향 규약을 재사용한다.

### A. One-cell noise

```text
Solid speck: center Solid, cardinal 4 cells all Air  → center Air
Air pinhole: center Air, cardinal 4 cells all Solid → center Solid
```

### B. Head snag

아래 exact one-cell ceiling tooth만 제거한다.

```text
center Solid
Up, UpLeft, UpRight Solid
Left, Right, Down Air
→ center Air
```

### C. Boxed-bottom pit

전역 jump/wall-climb 능력을 추측하지 않고 아래 exact one-wide boxed bottom만 한 칸 메운다.

```text
center Air
Down, Left, Right Solid
Up Air
UpLeft, UpRight Solid
→ center Solid
```

이 규칙은 broader pit reachability를 PASS로 선언하지 않는다. assembled-canvas 검증은 MAP16_02, movement-graph 기반 head snag/pit 검증은 MAP19_04 소유다.

proposal 규칙:

- 같은 target/same desired value는 coalesce하고 rule provenance를 union한다.
- 같은 target/different desired value가 나오면 batch 전체 `ConflictingCleanupProposal`로 atomic reject한다.
- success delta는 changed owned cells만 `(y,x)` canonical order로 before/after, rule, neighborhood evidence, protection evidence와 함께 가진다.
- input snapshot/render delta/SectorCanvas를 직접 변경하지 않는다.

## 6. Atomicity and Digest

최소 errors/issues:

```text
MissingInput | InvalidCoordinate | DuplicateCoordinate
MissingOwnedCell | UnexpectedOwnedCell | InvalidHalo
InvalidProtection | ProtectedWriteBlocked
InsufficientNeighborhood | InvalidHistory | DuplicateHistoryPlacement
InvalidCandidateSource | NoCandidateAfterThirdRepeatGuard
InvalidApplicationPlan | ConflictingCleanupProposal | AtomicCleanupRejected
```

- structural invalid/error 또는 conflicting proposal이 있으면 delta/digest를 publish하지 않는다.
- protected match와 insufficient neighborhood는 stable issue/evidence이며 mutation 0이다. 다른 유효 proposal까지 자동 폐기하지 않는다.
- collections은 accumulated, deduplicated, stable-sort, defensive copy/read-only다.
- reversed snapshot/source/history enumeration은 같은 signature/filter/delta/digest를 낸다.
- digest는 ruleset, canonical input solid/protection state, detected issues, proposals, before/after delta를 포함하고 time/display/object/file order/RNG를 제외한다.

## 7. Change Boundary

허용:

- `Runtime/.../MicroPatterns/` 신규 signature/repetition/cleanup C# + meta
- 대응 Runtime EditMode focused test C# + meta
- installed/archive Task, Result, PASS 후 Status Finalize

금지:

- existing production/test/CSV/meta 수정
- Authoring 52개/Generated 변경 또는 starter 24 pattern 작성
- MAP10_02~04 transform/plan/renderer/profile/index/RNG 수정·복제
- RNG draw/reroll, biome tuning, pattern authoring
- 전역 pathfinding/physics/TileValidation/reachability/density 판정
- SectorCanvas/Tilemap/Scene/Prefab/SO/Editor/asmdef 변경
- 문제 trigger 없는 이전/legacy test 실행
- unrelated stage/commit, Git push

## 8. Focused Validation

category `MAP10_05`만 실행하며 최소 다음을 증명한다.

1. exact Add/Carve 16-bit masks와 four-transform canonicalization
2. mirror-equivalent same signature, non-equivalent mask distinction
3. payload/weight/biome/RNG가 signature에 영향 0
4. same Pattern ID의 third candidate만 transform 무관 제외
5. different ID/same signature 허용, two-history mismatch 제외 0
6. guard→MAP10_04 index→selector integration과 no reroll/draw discard
7. all-excluded explicit no-candidate/no draw
8. solid speck/air pinhole exact cleanup
9. head snag exact cleanup과 near-miss mutation 0
10. boxed-bottom pit exact cleanup과 broader pit PASS 주장 0
11. protected target write 0과 provenance evidence
12. missing halo skip/evidence, no cascade
13. coalesce/conflict atomicity와 immutable delta
14. reversed enumeration same outputs/digests
15. renderer/Sector/Tilemap/RNG/file/Unity lifecycle side effect 0

Result 필수 수치:

```text
MAP10_05 focused: discovered/executed/pass/fail/skip
REGRESSION TRIGGER DETECTED: NO | YES(owner/reason/minimum scope)
PRIOR TASK TEST SELECTIONS: 0 (normal path)
LEGACY TEST SELECTIONS: 0 (normal path)
```

Static gate:

```text
compile/Console/relevant warning: 0/0/0
Authoring CSV/meta: 52/52 byte-unchanged
full Authoring manifest: b49b6e... exact
Generated CSV: 0
existing MAP00~10_04 modifications: 0
other roots/Editor/asmdef/Scene/Prefab/Settings/Packages changes: 0
duplicate GUID/unapplied candidate/diff-check: 0/0/0
unrelated staged/included: 0
```

## 9. Required Result

```text
MAP10_05_IMPLEMENT_REPETITION_SIGNATURE_AND_LOCAL_CLEANUP_RESULT.md
```

상단:

```text
TASK: MAP10_05_IMPLEMENT_REPETITION_SIGNATURE_AND_LOCAL_CLEANUP
STATUS: PASS | FAIL | BLOCKED
MAP10_05: COMPLETE ELIGIBLE | NOT COMPLETE
MAP10_06_AUTHOR_STARTER_24_MICROPATTERNS: LOCKED / DO NOT START
```

첫 구현 섹션은 반드시 `Responsibility and Added Functions`다.

| Field | Required report |
|---|---|
| Task responsibility | mirror signature, third-repeat guard, exact local cleanup boundary |
| Added functions | signature/history/filter/snapshot/proposal/delta/digest types and behavior |
| Inputs consumed | MAP10_02 plans, MAP10_03 cell state, MAP10_04 sources/index selector authority |
| Outputs produced | signature evidence, filtered sources/exclusions, immutable cleanup delta/issues |
| Explicit non-ownership | content/RNG/global physics/TileValidation/Tilemap 미구현 |
| Downstream consumers | MAP10_06~08, MAP11 pattern renderer, MAP16/MAP19 validators |

그 뒤 file inventory, signature/repetition/cleanup evidence, focused/regression policy, Unity/static/change scope, commit handoff를 기록한다.

PASS일 때만 Finalize하고 task-owned 파일만 atomic commit한다.

```text
Subject: MAP10_05: add pattern repetition and cleanup
Push: NOT PERFORMED
```

MAP10_06을 자동 시작하지 않는다.
