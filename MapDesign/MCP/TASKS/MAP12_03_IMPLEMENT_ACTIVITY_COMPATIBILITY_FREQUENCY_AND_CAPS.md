```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP12_03_IMPLEMENT_ACTIVITY_COMPATIBILITY_FREQUENCY_AND_CAPS
  task_file: TASKS/MAP12_03_IMPLEMENT_ACTIVITY_COMPATIBILITY_FREQUENCY_AND_CAPS.md
  requires_current_task: NONE
  requires_completed_task: MAP12_02_IMPLEMENT_ACTIVITY_REMOVAL_CUE_AND_SAFETY_PROOF
  requires_result:
    path: REPORTS/MAP12_02_IMPLEMENT_ACTIVITY_REMOVAL_CUE_AND_SAFETY_PROOF_RESULT.md
    status: PASS
    sha256: 2bd67c83fc147aa8f33f5c9c08dd56b44b5fd49e5e9c31425cd9a2dabdce12ab
  requires_installed_task:
    path: TASKS/MAP12_02_IMPLEMENT_ACTIVITY_REMOVAL_CUE_AND_SAFETY_PROOF.md
    sha256: 6fecbd8580fe6cd4d5d739ce8dc0933f03affa99adaca929f120614fe07c1284
  sets_current_task: MAP12_03_IMPLEMENT_ACTIVITY_COMPATIBILITY_FREQUENCY_AND_CAPS
```

# MAP12_03 — Activity Compatibility, Frequency and Caps

```text
TASK: MAP12_03_IMPLEMENT_ACTIVITY_COMPATIBILITY_FREQUENCY_AND_CAPS
PHASE: MAP12 — ActivityStructure / EventOverlay
STATUS: CURRENT
NEXT: MAP12_04_IMPLEMENT_EVENT_OVERLAY_ASSIGNMENT_RULES
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

검증된 Activity가 어떤 placement opportunity와 호환되는지 색인하고, World→BiomePatch→Sector 계층에 `6~12%` Activity 목표와 Strong Activity cap을 적용한 deterministic placement plan을 만든다.

```text
validated Activity + shell/removal proof
→ biome/pacing/footprint/clearance candidate index
→ hierarchical frequency budgets
→ deterministic weighted decisions under strong caps
```

이번 출력은 placement **계획**이다. Cluster/Sector Canvas에 실제 slot·Prefab을 쓰지 않는다.

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 추가/수정 스크립트, 각 책임, 새 기능, 파이프라인 위치, 미구현, Editor/게임 가시성을 실제 파일 단위로 보고한다.

## 1. Scope

| 소유 | 소유하지 않음 |
|---|---|
| Activity compatibility profile/opportunity | starter Activity production CSV |
| stable candidate/rejection index | TerrainCluster/Sector 실제 placement |
| World/Patch/Sector frequency budget | Tilemap/Prefab/state machine 실행 |
| weighted Activity decision | EventOverlay assignment/RNG |
| Strong Activity hard cap | gameplay 난이도 자동 튜닝 |

Event `3~8%`, cooldown, Empty variant와 별도 stream은 MAP12_04 소유다. MAP12_05 content, MAP12_06 Preview/PlayMode도 시작하지 않는다.

## 2. Focused-Only Policy

```text
MAP12_03 EditMode: required
MAP09/MAP10/MAP11/MAP12_01~02 categories: 0
legacy 19347: 0
PlayMode/unfiltered: 0/0
```

public API 호출은 과거 category 재실행이 아니다. upstream defect면 기존 파일을 고치지 말고 owner/invariant/원인/최소 검증 범위를 기록해 `BLOCKED`로 STOP한다. Task-owned 신규 파일 문제만 `MAP12_03` 범위에서 수정·재실행한다.

## 3. Preflight

```text
MAP12_02 Result: PASS
Result SHA-256: 2bd67c83fc147aa8f33f5c9c08dd56b44b5fd49e5e9c31425cd9a2dabdce12ab
installed Task SHA-256: 6fecbd8580fe6cd4d5d739ce8dc0933f03affa99adaca929f120614fe07c1284
MAP12_02 COMPLETE / MAP12_03 CURRENT / MAP12_04 LOCKED
inbox candidate / unrelated staged: 0/0
Unity compile / relevant Console error: 0/0
```

Approved representative chain:

```text
cluster/variant: TC_CRATER_BOWL_ASCENT / SPINE_CRATER_BOWL_ASCENT_BASE
MAP12_01 shell: 22a61392b9e1474c65dcf089f5caf1d14e20eb19250e1c8e06886143fd12fdd4
MAP12_02 removal safety proof:
5c9c27d0e52b9465a9fcc0ab3c83b51aa968a8088cf55b22a026ce2ea6934334
MAP11 catalog: 9d26786af477731d57503f16cc899210da6636f48dfb0542791e8fa591bd3bf7
MAP11 signature-set: 2884a639d9cef923e8b86a7fba2c0430cdfad2de11a63fd138d51dacdce13d8a
Authoring manifest: ff4761537986a4c9433775359d9b62ad806914ef30462a320c97b355126a5b6c
```

Required existing authority:

```text
MoonpalaceBiomeId / PacingRole / AccessClass
SectorCoord / BiomePatchId / BiomePatchSnapshot ownership
TerrainCluster footprint/local/working Canvas evidence
ActivityStructureContract + ActivityShellCanvas + ActivityRemovalSafetyProof
DeterministicRngStreamFactory + RNG_SECTOR_RECIPE
```

drift, inactive/missing RNG definition 또는 assembly reference 문제면 `BLOCKED`다.

## 4. Exact Files

정상 범위는 Runtime 3개, focused test 1개와 matching meta다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/Activities/ActivityCompatibility.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Activities/ActivityCandidateIndex.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Activities/ActivityFrequencyPlanner.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Activities/ActivityCompatibilityFrequencyTests.cs(.meta)
```

```text
Runtime: Game.Map.Runtime / StarNight.Map.WorldGeneration.Activities
Tests: Game.Map.Tests.EditMode / StarNight.Map.Tests.EditMode.WorldGeneration.Activities
Category: MAP12_03
```

기존 C#/test/CSV/meta/asmdef, RNG registry/pass catalog, Authoring/Generated, Scene/Prefab/SO/Tilemap, Settings/Packages는 수정하지 않는다. helper 추가는 금지한다.

## 5. Compatibility Model and Index

이름은 style에 맞출 수 있으나 동등한 surface를 제공한다.

```text
ActivityStrengthClass: Ordinary / Strong
ActivityPlacementProfile
ActivityPlacementClearanceEvidence
ActivityPlacementOpportunity
ActivityCompatibilityRejectionCode / Rejection
ActivityPlacementCandidate
ActivityCandidateIndex / CompileRequest / Result / Error
ActivityCandidateIndexCompiler.Compile
```

`ActivityPlacementProfile`은 validated Activity identity/digests와 함께 다음을 가진다.

```text
allowed biomes (1+)
allowed pacing roles (1+)
allowed access classes (1+)
minimum/maximum active chunk count
required open clearance width/height
integer weight 1..10000
Ordinary or Strong
```

`ActivityPlacementOpportunity`은 stable ID, `SectorCoord`, `BiomePatchId`, primary biome, TerrainCluster/variant, pacing/access, active chunk count, clearance evidence와 exact MAP11/MAP12 artifact digests를 가진다. Patch/Sector identity는 caller evidence가 아니라 existing `BiomePatchSnapshot` ownership과 일치해야 한다.

clearance evidence는 explicit rectangle origin/width/height/coordinates다.

- exact `width × height`, unique, active bounds다.
- final working Canvas에서 모두 Air다.
- Core의 Device/Hazard/Projectile reserved coordinate와 겹치지 않는다.
- Activity clearance rectangle의 AbsoluteProtected overlap은 항상 거부한다. MAP12_01 slot marker 자체의 protected overlap evidence와 physical clearance를 혼동하지 않는다.
- 새 rectangle search/packing을 하지 않고 caller rectangle만 검증한다.

candidate eligibility:

```text
Activity and shell/removal proof success + digest identity
exact referenced TerrainCluster/variant
biome + pacing + access membership
active chunks within profile range
verified clearance >= required dimensions
```

candidate key는 `opportunity ID + Activity ID + profile/shell/safety digests`다. duplicate key의 모든 source를 제외하며 rejection은 stable evidence로 남긴다. Index/query는 RNG를 사용하지 않는다.

## 6. Frequency Policy and Hierarchical Budget

```text
ActivityFrequencyPolicy
TargetPermille: 60..120 inclusive
MaxStrongPerWorld: non-negative
MaxStrongPerPatch: non-negative
MaxStrongPerSector: non-negative
```

`6~12%`는 eligible opportunity 대비 selected opportunity 비율이다. float를 사용하지 않고 integer cross-multiplication으로 판정한다.

planner는 다음 순서로 exact budget을 만든다.

1. World target count를 `eligible × TargetPermille / 1000`의 round-half-up으로 계산한다.
2. strict `60..120 permille` integer band가 존재하면 target을 그 band 안으로 clamp한다.
3. World count를 patch eligible mass에 비례해 largest-remainder 방식으로 배분한다.
4. 각 patch count를 sector eligible mass에 같은 방식으로 배분한다.
5. tie는 `BiomePatchId`, `SectorCoord` ordinal/index order로 해결한다.

각 `ActivityScopeBudget`은 scope kind/ID, eligible/target/selected/ordinary/strong, lower/upper count, exact rational rate, `BandFeasible`, `DiscreteApproximation`을 게시한다.

표본이 작아 6~12% 사이 정수 count가 존재하지 않으면 cap을 깨거나 임의 Activity를 추가하지 않는다. parent allocation에서 받은 0/1 count와 `DiscreteApproximation=true`를 명시한다. World target과 child budget 합계는 exact 같아야 한다.

## 7. Deterministic Decisions and Strong Caps

```text
ActivityFrequencyPlanRequest
ActivityPlacementDecision
ActivityScopeBudget
ActivityFrequencyPlan
ActivityFrequencyPlanResult / Error
ActivityFrequencyPlanner.Plan
```

RNG는 existing `RNG_SECTOR_RECIPE`만 사용한다.

```text
world seed: caller exact ulong
scope: existing SectorCoord
attempt: non-negative caller ordinal
session: fresh per Sector batch
```

- 모든 profile/index/opportunity/budget/cap을 검증한 뒤에만 stream을 만든다.
- sector 안 opportunity는 stable ID order다.
- eligible opportunity마다 priority draw 1회를 사용한다.
- allocated position마다 current caps로 허용된 candidate set에서 weighted draw 1회를 사용한다.
- weight 합은 checked integer이며 half-open ticket으로 해결한다.
- same input은 same decision/digest/draw evidence다.
- invalid/empty input은 stream created 0, draw 0이다.
- unrelated RNG stream을 생성하거나 소비하지 않는다.

Strong 선택 전 World/Patch/Sector cap을 모두 확인한다. cap을 초과할 Strong candidate는 allowed set에서 제외하고 Ordinary candidate가 있으면 같은 opportunity에서 선택한다. quota를 채울 compatible candidate가 부족하면 cap을 초과하지 않고 `TargetUnsatisfied`/`StrongCapUnsatisfiable` atomic failure를 게시한다.

decision은 opportunity/activity/candidate key, scope IDs, strength, weight/total/ticket, priority, draw before/after와 all three cap counters를 기록한다.

## 8. Atomicity and Digest

index/plan collections은 defensive-copy/read-only/canonical order다. errors는 accumulated/deduplicated/stable-sorted다.

digest 포함:

```text
ruleset, profiles/opportunities/candidates/rejections
MAP11/MAP12 artifact digests
frequency policy and hierarchy budgets
world seed/stream/scope/attempt/draw evidence
placement decisions and cap counters
```

locale/time/input/reflection order/object identity/Unity lifecycle은 제외한다. any error는 index 또는 plan/digest의 atomic zero publication이다.

최소 error groups:

```text
MissingInput | InvalidProfile | InvalidOpportunity | IdentityMismatch
ArtifactDigestMismatch | PatchOwnershipMismatch | InvalidClearance
DuplicateCandidate | EmptyCandidateIndex | InvalidFrequencyPolicy
InvalidStrongCap | InvalidRngBinding | BudgetMismatch
TargetUnsatisfied | StrongCapUnsatisfiable | NonCanonicalPublication
```

## 9. Focused Tests

production Activity content가 없으므로 validated test-owned Ordinary/Strong profiles를 사용한다. 실제 `TC_CRATER_BOWL_ASCENT` MAP11/MAP12 chain을 후보 근거로 사용하고, scope 계획에는 existing value types와 valid `BiomePatchSnapshot` ownership을 사용한다.

`MAP12_03` category에서 검증:

1. biome/pacing/access/footprint/clearance eligible candidate
2. 각 compatibility mismatch의 stable rejection
3. clearance rectangle Air/size/protection validation
4. duplicate candidate all-excluded와 atomic empty-index failure
5. canonical/reverse/culture-stable index
6. TargetPermille 60/120 inclusive 및 59/121 rejection
7. World→Patch→Sector largest-remainder budget sums
8. feasible scope rate `6~12%`, small scope discrete evidence
9. weighted deterministic decisions and one-field seed/sector/attempt sensitivity
10. World/Patch/Sector Strong caps exact enforcement
11. cap-unsatisfiable target atomic failure
12. invalid/empty input stream/draw 0와 other-stream independence
13. no Canvas/geometry/Prefab/Scene/Tilemap mutation

Result에는 최소 하나의 100-opportunity fixture로 actual world/patch/sector selected counts, achieved rate와 strong cap counters를 보고한다.

## 10. Static Gates

```text
Unity compile / Console error / warning: 0/0/0
MAP12_03 discovered = executed = passed; fail/skip/inconclusive 0
new Runtime C#/meta: 3/3
new focused test C#/meta: 1/1
existing C#/test/CSV/meta changes: 0
RNG registry/pass catalog changes: 0/0
Authoring/Generated changes: 0/0
asmdef/Scene/Prefab/Tilemap/Settings/Packages changes: 0
MAP11/MAP12_01~02 artifact/source modifications: 0
duplicate GUID: 0
inbox/diff-check/unrelated staged: 0/0/0
prior/legacy/PlayMode/unfiltered selections: 0/0/0/0
Git push: NOT PERFORMED
```

initialization/import timeout으로 executed 0이면 PASS로 세지 않고 같은 category만 재시도한다.

## 11. Required Result

```text
MapDesign/MCP/REPORTS/MAP12_03_IMPLEMENT_ACTIVITY_COMPATIBILITY_FREQUENCY_AND_CAPS_RESULT.md
```

상단:

```text
TASK: MAP12_03_IMPLEMENT_ACTIVITY_COMPATIBILITY_FREQUENCY_AND_CAPS
STATUS: PASS | BLOCKED
MAP12_03: COMPLETE ELIGIBLE | NOT COMPLETE
MAP12_04_IMPLEMENT_EVENT_OVERLAY_ASSIGNMENT_RULES: LOCKED / DO NOT START
```

`## User-Facing Implementation Report`에서 파일/책임/새 기능/파이프라인/미구현/가시성을 먼저 보고하고, `## Responsibility and Added Functions`에서 inputs/outputs/non-ownership/downstream을 명시한다.

이후 actual evidence:

- file/class/public surface
- profiles/opportunities/candidate/rejection counts
- compatibility and clearance matrix
- World/Patch/Sector budgets와 exact rates
- Ordinary/Strong selections와 three-scope caps
- RNG stream/draw/ticket/determinism evidence
- negative atomic matrix
- focused counts/regression selections
- static/change scope와 commit

정상 경로:

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY TEST SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
```

PASS일 때만 MAP12_03을 Finalize하고 task-owned 파일만 atomic commit한다.

```text
Subject: MAP12_03: implement activity compatibility frequency and caps
Push: NOT PERFORMED
```

Result가 PASS여도 MAP12_04를 자동 시작하지 않는다. 별도 검수 전까지 계속 LOCKED다.
