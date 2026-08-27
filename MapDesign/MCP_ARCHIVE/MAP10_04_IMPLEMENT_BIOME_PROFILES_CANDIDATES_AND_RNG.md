```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP10_04_IMPLEMENT_BIOME_PROFILES_CANDIDATES_AND_RNG
  task_file: TASKS/MAP10_04_IMPLEMENT_BIOME_PROFILES_CANDIDATES_AND_RNG.md
  requires_current_task: NONE
  requires_completed_task: MAP10_03_IMPLEMENT_ORDERED_PATTERN_RENDERER
  requires_result:
    path: REPORTS/MAP10_03_IMPLEMENT_ORDERED_PATTERN_RENDERER_RESULT.md
    status: PASS
    sha256: 3890aa4087093ac8078ccd64038b2156d9177b0ac55066b3b5ff29e1cc5aa427
  requires_installed_task:
    path: TASKS/MAP10_03_IMPLEMENT_ORDERED_PATTERN_RENDERER.md
    sha256: 9138b1fdda796e324db5b977ee4b90373a13454e8fd66e55769b5a024552e39a
  sets_current_task: MAP10_04_IMPLEMENT_BIOME_PROFILES_CANDIDATES_AND_RNG
```

# MAP10_04 — Implement Biome Profiles, Candidates, and RNG

```text
TASK: MAP10_04_IMPLEMENT_BIOME_PROFILES_CANDIDATES_AND_RNG
PHASE: MAP10 — 4×4 MicroPattern Authoring / Rendering
STATUS: CURRENT
NEXT: MAP10_05_IMPLEMENT_REPETITION_SIGNATURE_AND_LOCAL_CLEANUP
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Responsibility

이번 Task는 validated 4×4 pattern과 successful MAP10_02 plan을 biome별 후보로 정규화하고, stable index에서 deterministic weighted selection을 수행한다.

```text
pattern catalog + biome + successful plans
→ biome profile/feature evidence
→ stable candidate index
→ MicroPattern-only deterministic selection session
→ immutable decisions and digest
```

| 소유 | 소유하지 않음 |
|---|---|
| four-biome profile 계약 | starter 24 pattern 작성 |
| density/silhouette feature evidence | 반복 signature/hash |
| 후보 eligibility/index/digest | local cleanup/물리 검증 |
| pattern 전용 RNG session/weighted pick | render/Tilemap/Sector mutation |

## 1. Regression and Preflight

정상 실행은 category `MAP10_04`만 선택한다.

```text
Prior MAP00~10_03 selections: 0
Legacy 19347 selections: 0
```

focused 실패, compile/Console 오류, 승인 digest/Authoring drift, 기존 파일 변경, asmdef/GUID 위반 중 하나가 실제 발생한 경우에만 owner·원인·최소 관련 selection을 먼저 기록하고 재검증한다.

읽기 전용 preflight:

1. MAP10_03 Result/installed/archive Task exact hash와 Status
2. MAP09_03 biome/pattern contract, MAP10_01 immutable catalog
3. MAP10_02 successful application plan, MAP10_03 renderer boundary
4. MAP02_02 deterministic RNG public API와 registered stream definitions
5. `generation_passes.csv`의 `PASS_MICRO_SOLVE -> RNG_SECTOR_RECIPE` exact binding
6. Authoring `52/52`, full manifest 아래 값, Generated CSV 0

```text
MAP10_01 catalog fixture digest:
1b2524bf8af6be7ae3b2d03134096a4efdf8f856ea500863ec5dcd26114f0c35

Full Authoring manifest:
b49b6ebc4a65c26302ff08deb9100dbf727200e16b3588092b7cd53f63d214ba
```

기존 RNG/plan/catalog authority를 수정해야만 진행 가능하면 자동 보정하지 말고 `BLOCKED`다.

## 2. Four-Biome Profile Contract

구현 위치:

```text
Runtime: Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/
Test:    Assets/_Game/Tests/EditMode/Map/WorldGeneration/MicroPatterns/
Namespace: StarNight.Map.WorldGeneration.MicroPatterns
```

최소 surface:

```text
MicroPatternBiomeProfile
MicroPatternBiomeProfileCatalog
MicroPatternFeatureSummary
MicroPatternSilhouetteClass
MicroPatternProfileValidationResult / Error
```

catalog은 exact four typed biome을 각각 한 번 포함한다.

| Biome | Canonical motif metadata | Safety meaning |
|---|---|---|
| MoonCrater | BrokenSlope, Bowl, RockShelf | wide view/projectile lane 보호; 무의미한 대형 평지 축소 |
| CassiaRoot | RootArch, VerticalTunnel, HollowPocket | 세로 이동/작은 공동; protected 통로 축소 금지 |
| AbandonedMill | BrokenPillar, BeamOverhang, OrthogonalCarve | 직교 구조 유지; gear/ladder는 pattern 비소유 |
| MoonDough | BounceCup, SoftPocket, StickyShelf | 둥근 홈/복구 바닥; bounce 없이 기본 통과 |

위 motif는 stable metadata이며 pattern ID 문자열 추측으로 eligibility를 판정하지 않는다. 실제 pool membership은 `MicroPatternDefinition.AllowedBiomes`가 소유한다.

feature summary는 transformed pattern과 successful plan에서 다음 raw integer evidence를 계산한다.

```text
AddSolid cell count / CarveAir cell count
Geometry-write cell count over exact 16
Total non-NoChange cell-layer write count
Protected-overlap count / forced-NoChange count
SilhouetteClass: NoGeometry | AddOnly | CarveOnly | Mixed
```

- 입력 순서/locale과 무관하고 같은 transformed plan이면 동일하다.
- density는 부동소수점 반올림 대신 `numerator / 16` raw pair로 보존한다.
- 승인 문서에 numeric biome threshold가 없으므로 임의 수치를 만들지 않는다.
- built-in four profiles의 density policy는 명시적 `Uncalibrated(0..16 evidence only)`다. 이는 숨은 default가 아니라 Result/digest에 포함된다.
- MAP10_06의 24개 실제 표본 이후 별도 승인 없이 threshold를 몰래 추가하지 않는다.
- MAP10_05가 소유하는 mirror-invariant silhouette hash/repetition 판정은 구현하지 않는다.

profile/catalog/error/result collections은 defensive copy/read-only, ordinal canonical order다. missing/duplicate/unknown biome, invalid motif token, invalid policy/class는 accumulated stable error이며 partial catalog/digest를 publish하지 않는다.

## 3. Stable Candidate Index

최소 surface:

```text
MicroPatternCandidateKey
MicroPatternCandidate
MicroPatternCandidateIndex
MicroPatternCandidateIndexBuilder
MicroPatternCandidateRejection
```

candidate eligibility는 모두 만족해야 한다.

1. validated immutable pattern definition
2. requested biome이 pattern allowlist에 존재
3. transform이 pattern allowlist에 존재
4. 해당 pattern/transform/origin의 MAP10_02 plan이 success
5. profile feature contract 계산 성공

stable key:

```text
pattern ID + transform token + application-plan digest
```

- input/file/dictionary/reflection order와 무관하게 ordinal sort한다.
- duplicate key, invalid plan digest, biome/transform mismatch는 accumulated rejection이다.
- candidate는 definition selection weight `1..10000`을 그대로 보존한다. profile이 숨은 random multiplier를 적용하지 않는다.
- rejected candidate는 이유와 source identity를 남기되 eligible index에 섞지 않는다.
- index digest는 biome/profile digest, candidate key, source pattern digest, plan digest, feature summary, exact weight를 포함한다.
- renderer를 호출하거나 working cell/SectorCanvas를 변경하지 않는다.

## 4. MicroPattern-Only RNG Session

기존 MAP02_02 RNG를 재사용하고 새 PRNG/새 stream CSV를 만들지 않는다.

```text
Registered definition: RNG_SECTOR_RECIPE
Reason: generation_passes.csv already binds PASS_MICRO_SOLVE to it
Scope: existing SECTOR scope + exact sector identity + non-negative attempt
Instance: fresh deterministic stream, owned only by one MicroPattern selection session
```

최소 surface:

```text
MicroPatternSelectionRequestId
MicroPatternSelectionRequest
MicroPatternSelectionDecision
MicroPatternSelectionBatchResult / Error
MicroPatternDeterministicSelector
MicroPatternSelectionCanonicalDigest
```

selection contract:

- request ID는 stable grammar `^MPS_[A-Z0-9_]+$`, batch 안에서 unique다.
- 모든 request/index를 먼저 검증하고 하나라도 invalid/empty면 stream을 만들거나 draw하지 않고 batch를 atomic reject한다.
- request는 request ID ordinal로 처리한다.
- 각 index는 candidate key canonical order다.
- total weight는 checked sum이며 `1..int.MaxValue`; overflow/zero는 reject한다.
- existing unbiased `NextInt(totalWeight)` 한 번으로 half-open ticket을 만들고 cumulative integer weight로 선택한다.
- fresh session의 InitialState, DrawCount before/after, ticket, chosen key/index digest를 evidence로 남긴다.
- same world seed, sector, attempt, profile/index/request set이면 same decisions/digest다.
- reversed input enumeration은 결과를 바꾸지 않는다.
- 다른 RNG stream/instance의 state/draw count에는 영향 0이다.
- `System.Random`, `UnityEngine.Random`, global/shared mutable RNG, time/GUID/object hash fallback은 금지한다.
- candidate 없음/invalid RNG definition/scope mismatch에서 first/last/default pattern fallback을 만들지 않는다.

selection digest는 ruleset version, world seed의 canonical unsigned value, registered stream ID, scope identity/attempt, InitialState/draw evidence, canonical requests/index digests/decisions를 포함하고 timestamp/display text/object hash는 제외한다.

## 5. Change Boundary

허용:

- `Runtime/.../MicroPatterns/` 신규 profile/candidate/selector C# + meta
- 대응 Runtime EditMode focused test C# + meta
- installed/archive Task, Result, PASS 후 Status Finalize

금지:

- 기존 production/test/CSV/meta 수정
- Authoring 52개 또는 Generated 변경
- 새 RNG stream ID/CSV row, existing RNG 구현/registry/pass binding 수정
- starter pattern/profile data row 작성(MAP10_06)
- renderer 호출/수정, repetition/hash/3연속 금지, cleanup(MAP10_05)
- SectorCanvas/TileValidation/Tilemap/Scene/Prefab/SO/Editor/asmdef 변경
- 문제 trigger 없는 이전/legacy test 실행
- unrelated stage/commit, Git push

## 6. Focused Validation

`MAP10_04` category만 실행하며 최소 다음을 증명한다.

1. exact four biome profile membership/order/motif metadata
2. uncalibrated density policy와 raw `n/16` evidence
3. four silhouette classes와 transformed-plan feature counts
4. biome/transform/plan eligibility와 rejection evidence
5. candidate key canonical order, duplicate/invalid accumulation
6. reversed input same index/digest
7. definition weight exact preservation와 checked total
8. `RNG_SECTOR_RECIPE` + SECTOR scope exact reuse
9. same seed/scope/attempt/index same decision/digest
10. seed/sector/attempt/index one-field sensitivity
11. weighted ticket boundary와 no-candidate no-draw atomic reject
12. other stream instance independence
13. immutable collections/input mutation resistance
14. forbidden RNG/file/Unity lifecycle/renderer side effect 0

Result 필수 수치:

```text
MAP10_04 focused: discovered/executed/pass/fail/skip
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
existing MAP00~10_03 modifications: 0
other roots/Editor/asmdef/Scene/Prefab/Settings/Packages changes: 0
duplicate GUID/unapplied candidate/diff-check: 0/0/0
unrelated staged/included: 0
```

## 7. Required Result

```text
MAP10_04_IMPLEMENT_BIOME_PROFILES_CANDIDATES_AND_RNG_RESULT.md
```

상단:

```text
TASK: MAP10_04_IMPLEMENT_BIOME_PROFILES_CANDIDATES_AND_RNG
STATUS: PASS | FAIL | BLOCKED
MAP10_04: COMPLETE ELIGIBLE | NOT COMPLETE
MAP10_05_IMPLEMENT_REPETITION_SIGNATURE_AND_LOCAL_CLEANUP: LOCKED / DO NOT START
```

첫 구현 섹션은 반드시 `Responsibility and Added Functions`다.

| Field | Required report |
|---|---|
| Task responsibility | four-biome profile, stable candidate index, deterministic selection boundary |
| Added functions | profile/feature/candidate/index/selector/result/digest types and behavior |
| Inputs consumed | MAP10_01 catalog, MAP10_02 plans, MAP02_02 RNG authority |
| Outputs produced | immutable profile catalog, candidate index, selection decisions/evidence |
| Explicit non-ownership | content/repetition/cleanup/render/Tilemap 미구현 |
| Downstream consumers | MAP10_05~08과 MAP11 cluster pattern renderer |

그 뒤 파일 inventory, profile/feature/index/RNG evidence, focused/regression policy, Unity/static/change scope, commit handoff를 기록한다.

PASS일 때만 Finalize하고 task-owned 파일만 atomic commit한다.

```text
Subject: MAP10_04: implement biome pattern selection
Push: NOT PERFORMED
```

MAP10_05를 자동 시작하지 않는다.
