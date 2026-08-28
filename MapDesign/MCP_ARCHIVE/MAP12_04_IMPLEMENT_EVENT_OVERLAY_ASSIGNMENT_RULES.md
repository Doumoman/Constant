```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP12_04_IMPLEMENT_EVENT_OVERLAY_ASSIGNMENT_RULES
  task_file: TASKS/MAP12_04_IMPLEMENT_EVENT_OVERLAY_ASSIGNMENT_RULES.md
  requires_current_task: NONE
  requires_completed_task: MAP12_03_IMPLEMENT_ACTIVITY_COMPATIBILITY_FREQUENCY_AND_CAPS
  requires_result:
    path: REPORTS/MAP12_03_IMPLEMENT_ACTIVITY_COMPATIBILITY_FREQUENCY_AND_CAPS_RESULT.md
    status: PASS
    sha256: 28cea501e31fd0a8e56ce8a1b43ca5eacc7a1faca3f71e4a2dd6ba42a7a0a4c4
  requires_installed_task:
    path: TASKS/MAP12_03_IMPLEMENT_ACTIVITY_COMPATIBILITY_FREQUENCY_AND_CAPS.md
    sha256: 36956a1f8fb339a0dd52d8e98d5875d9c2505da7a5c923b08f75e17c520ded89
  sets_current_task: MAP12_04_IMPLEMENT_EVENT_OVERLAY_ASSIGNMENT_RULES
```

# MAP12_04 — EventOverlay Assignment Rules

```text
TASK: MAP12_04_IMPLEMENT_EVENT_OVERLAY_ASSIGNMENT_RULES
PHASE: MAP12 — ActivityStructure / EventOverlay
STATUS: CURRENT
NEXT: MAP12_05_AUTHOR_STARTER_ACTIVITIES_AND_OVERLAYS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

검증된 `EventOverlayContract`를 TerrainCluster/Activity/SpecialRegion marker opportunity에 연결하고, non-empty Event `3~8%`, progression cooldown, explicit Empty 결정과 Activity와 분리된 RNG 증거를 가진 immutable assignment plan을 만든다.

```text
EventOverlay contracts + marker opportunities
→ marker-only compatibility/Special overlap index
→ 3~8% non-empty budget + cooldown
→ RNG_POPULATION decisions + explicit Empty remainder
```

결과는 assignment 계획 데이터다. 실제 NPC/Reward/State/Prefab을 생성하지 않는다.

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 추가/수정 스크립트, 각 책임, 새 기능, 파이프라인 위치, 미구현, Editor/게임 가시성을 실제 파일 단위로 보고한다.

## 1. Scope

| 소유 | 소유하지 않음 |
|---|---|
| Event marker target/compatibility index | actual marker/NPC/Reward spawn |
| SpecialRegion replaceable-slot overlap rules | Special reservation/footprint 변경 |
| non-empty 3~8%와 explicit Empty plan | Activity 6~12% plan 수정 |
| progression-ordinal cooldown | wall-clock/save cooldown |
| separate `RNG_POPULATION` decisions | RNG registry/stream 정의 변경 |

production content는 MAP12_05, Preview/PlayMode는 MAP12_06 소유다. EventOverlay는 MechanismGraph/ProgressionGraph, collision, route, pacing 또는 Static Shell을 소유하지 않는다.

## 2. Focused-Only Policy

```text
MAP12_04 EditMode: required
MAP09/MAP10/MAP11/MAP12_01~03 categories: 0
legacy 19347: 0
PlayMode/unfiltered: 0/0
```

기존 public API 호출은 과거 category 재실행이 아니다. upstream defect면 기존 파일을 수정하지 말고 owner/invariant/원인/최소 검증 범위를 기록해 `BLOCKED`로 STOP한다. Task-owned 신규 파일 문제만 `MAP12_04`에서 수정·재실행한다.

## 3. Preflight

```text
MAP12_03 Result: PASS
Result SHA-256: 28cea501e31fd0a8e56ce8a1b43ca5eacc7a1faca3f71e4a2dd6ba42a7a0a4c4
installed Task SHA-256: 36956a1f8fb339a0dd52d8e98d5875d9c2505da7a5c923b08f75e17c520ded89
MAP12_03 COMPLETE / MAP12_04 CURRENT / MAP12_05 LOCKED
inbox candidate / unrelated staged: 0/0
Unity compile / relevant Console error: 0/0
```

Approved Activity evidence:

```text
100 opportunities / 8 non-empty Activity decisions / 80 permille
Activity stream: RNG_SECTOR_RECIPE
priority/weighted/total draws: 100/8/108
Activity plan/source modification: 0/0 required
```

Required existing authority:

```text
MAP09_05 EventOverlayContract/validator/digest and exact kinds/operations
MAP09_06 SpecialRegionContract/fixed shell/replaceable slots/persistence
MAP12_01 projected Activity slots/markers
MAP12_02 removal safety proof
MAP12_03 opportunities and ActivityFrequencyPlan
SectorCoord / BiomePatchId ownership
DeterministicRngStreamFactory + RNG_POPULATION (SPAWN scope)
```

`RNG_POPULATION` missing/inactive/wrong scope, artifact drift 또는 missing reference면 `BLOCKED`다.

## 4. Exact Files

정상 범위는 Runtime 3개, focused test 1개와 matching meta다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/EventOverlays/EventOverlayAssignment.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/EventOverlays/EventOverlayCandidateIndex.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/EventOverlays/EventOverlayAssignmentPlanner.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/EventOverlays/EventOverlayAssignmentTests.cs(.meta)
```

```text
Runtime: Game.Map.Runtime / StarNight.Map.WorldGeneration.EventOverlays
Tests: Game.Map.Tests.EditMode / StarNight.Map.Tests.EditMode.WorldGeneration.EventOverlays
Category: MAP12_04
```

기존 C#/test/CSV/meta/asmdef, RNG registry/pass catalog, Authoring/Generated, Scene/Prefab/SO/Tilemap, Settings/Packages는 수정하지 않는다. helper 추가는 금지한다.

## 5. Assignment Model and Marker Targets

이름은 style에 맞출 수 있으나 동등한 surface를 제공한다.

```text
EventMarkerTargetSourceKind: TerrainCluster / Activity / SpecialRegion
EventSpecialOverlapKind: None / ReplaceableSlot
EventMarkerTargetEvidence
EventOverlayAssignmentProfile
EventOverlayOpportunity
EventOverlayCompatibilityRejectionCode / Rejection
EventOverlayCandidate
EventOverlayCandidateIndex / Request / Result / Error
EventOverlayCandidateIndexCompiler.Compile
```

profile은 validated `EventOverlayContract`/digest, integer weight `1..10000`, non-negative `MinimumProgressionGap`, compatible biome/pacing/access와 optional referenced Activity ID를 가진다.

opportunity는 stable ID, `SectorCoord`, `BiomePatchId`, unique non-negative `ProgressionOrdinal`, TerrainCluster/optional selected Activity identity, MAP12_03 plan digest와 available marker target evidence를 가진다.

marker evidence는 marker ID, source kind/owner ID, source/compiled coordinate, owning slot kind, underlying Canvas/Static Shell/protection/persistence digest를 게시한다.

- Event assignment target ID/operation/payload는 existing contract exact 값이다.
- target marker는 opportunity evidence에 exact 하나 존재해야 한다.
- marker-only이므로 geometry/collision/route/access/pacing/envelope mutation field는 0/부재다.
- assignment coordinate의 underlying value/digest는 plan 전후 동일하다.
- duplicate target marker, unknown marker, kind/operation mismatch는 rejection이다.

## 6. Empty and Non-Empty Candidate Rules

Existing exact overlay kinds/operations를 그대로 사용한다.

```text
Kinds: Npc / Reward / State / Cosmetic / Empty
Operations: EnableMarker / DisableMarker / SpawnNpc / SpawnReward / SetState
```

- `Empty` contract는 assignment exact `0`이며 weight/cooldown selection에 참여하지 않는다.
- 각 opportunity는 compatible Empty candidate exact 하나를 가져야 한다.
- non-empty candidate는 assignment `1+`, unique targets와 positive weight를 가진다.
- planner의 `non-empty selected count`만 3~8% 빈도에 포함한다.
- 선택되지 않은 모든 eligible opportunity는 explicit Empty decision을 받는다.
- Empty decision은 RNG weighted draw를 소비하지 않는다.
- missing/duplicate Empty candidate는 atomic failure다.

## 7. SpecialRegion Overlap

SpecialRegion은 pre-reserved authority다. Event는 fixed shell/footprint/port/persistence ownership을 바꾸지 않는다.

허용되는 replaceable-slot mapping:

| Event operation/kind | Special slot kind |
|---|---|
| SpawnNpc / Npc | Npc |
| SpawnReward / Reward | Reward |
| EnableMarker, DisableMarker, SetState / State or Cosmetic | Event |

- Special target은 validated `SpecialRegionContract`의 exact slot ID/coordinate다.
- `FixedShell`, Facility, Enemy, Entry, Return slot overlap은 거부한다.
- slot과 FixedShell overlap, out-of-footprint, port coordinate mutation은 거부한다.
- existing persistence key가 있으면 provenance를 보존하고 Event가 새 key/state owner가 되지 않는다.
- SpecialRegion 없는 opportunity는 `EventSpecialOverlapKind.None`이다.

## 8. Frequency, Cooldown and Empty Remainder

```text
EventOverlayAssignmentPolicy
TargetPermille: 30..80 inclusive
```

non-empty count는 eligible opportunity 대비 exact rational 비율이다. float를 사용하지 않는다.

MAP12_03과 같은 deterministic hierarchy를 사용하되 Event 전용 model/digest를 게시한다.

1. World target round-half-up 및 feasible `30..80` integer band clamp
2. World→Patch→Sector largest-remainder allocation
3. stable `BiomePatchId`/`SectorCoord` tie-break
4. low-sample scope는 `DiscreteApproximation=true`
5. child non-empty sum = parent/world target exact
6. 나머지 opportunity는 explicit Empty

cooldown은 wall-clock이 아니라 `ProgressionOrdinal` 차이다.

- 같은 non-empty EventOverlay ID의 consecutive selection gap은 `MinimumProgressionGap` 이상이어야 한다.
- 다른 Event ID와 Empty는 그 Event의 cooldown을 소비하거나 초기화하지 않는다.
- cooldown으로 후보가 제외되면 다른 compatible non-empty candidate를 선택한다.
- quota를 채울 후보가 없으면 cooldown을 무시하지 않고 `CooldownMakesTargetUnsatisfiable` atomic failure다.
- decision은 previous/current ordinal, required/actual gap과 exclusion evidence를 게시한다.

## 9. Separate RNG and Assignment Decisions

```text
EventOverlayAssignmentDecisionKind: Assigned / Empty
EventOverlayAssignmentDecision
EventOverlayScopeBudget
EventOverlayAssignmentPlan / Request / Result / Error
EventOverlayAssignmentPlanner.Plan
```

Event planner는 existing `RNG_POPULATION`만 사용한다.

```text
reset scope: SPAWN
scope identity: canonical "EVENT|<sector x,y>|<opportunity ID>"
attempt: non-negative caller ordinal
fresh stream per opportunity using the existing factory
```

- all inputs/index/budgets/cooldown을 검증한 뒤 stream을 만든다.
- eligible opportunity마다 non-empty priority draw 1회.
- allocated non-empty position마다 cooldown-filtered candidates에서 weighted draw 1회.
- Empty decision draw는 0이다.
- invalid/empty input stream/draw는 0/0이다.
- same input은 same decisions/digest; seed/attempt one-field change는 digest를 바꾼다.
- `RNG_SECTOR_RECIPE` Activity stream의 state/draw/first value를 변경하지 않는다.
- `RNG_POPULATION` definition/registry는 수정하지 않는다.

decision은 opportunity, Assigned/Empty, Event ID/kind/digest, assignment/marker/Special provenance, scope budget, priority/weight/ticket/draw, cooldown evidence를 기록한다.

## 10. Atomicity and Digest

collections은 defensive-copy/read-only/canonical order이며 errors는 accumulated/deduplicated/stable-sorted다.

digest는 profiles/opportunities/candidates/rejections, MAP12_03 plan, Event/Special/marker digests, 3~8 budgets, Assigned/Empty decisions, cooldown, population stream/draw evidence를 포함한다. locale/time/input order/object identity/Unity lifecycle은 제외한다.

any error는 index 또는 plan/digest atomic zero publication이다.

최소 error groups:

```text
MissingInput | InvalidProfile | InvalidOpportunity | IdentityMismatch
ArtifactDigestMismatch | MissingMarker | DuplicateMarker | InvalidMarkerOperation
NonMarkerMutation | InvalidSpecialOverlap | FixedShellOverlap
PersistenceProvenanceMismatch | MissingEmptyVariant | DuplicateEmptyVariant
InvalidFrequencyPolicy | InvalidCooldown | CooldownMakesTargetUnsatisfiable
InvalidRngBinding | BudgetMismatch | NonCanonicalPublication
```

## 11. Focused Tests

production Event content가 없으므로 MAP09_05 validator로 test-owned Npc/Reward/State/Cosmetic/Empty contracts를 만든다. MAP12_03의 100-opportunity plan과 validated test-owned SpecialRegion contracts를 사용한다.

`MAP12_04` category에서 검증:

1. TerrainCluster/Activity marker candidate compatibility
2. Npc/Reward/Event Special replaceable-slot overlap
3. fixed shell/Facility/Enemy/Entry/Return overlap rejection
4. persistence provenance 보존과 non-marker mutation rejection
5. exact one Empty candidate와 all unselected Empty decisions
6. TargetPermille 30/80 inclusive, 29/81 atomic rejection
7. World→Patch→Sector non-empty budgets/sums/rates
8. progression cooldown pass/exclusion/unsatisfiable failure
9. population RNG repeat/reverse/culture determinism
10. Activity `RNG_SECTOR_RECIPE` independence
11. invalid input population stream/draw 0
12. Canvas/StaticShell/Activity plan/Special contract mutation 0

Result에는 100-opportunity fixture로 non-empty/Empty counts, rate, scope budgets, cooldown exclusions와 population draw counts를 보고한다.

## 12. Static Gates

```text
Unity compile / Console error / warning: 0/0/0
MAP12_04 discovered = executed = passed; fail/skip/inconclusive 0
new Runtime C#/meta: 3/3
new focused test C#/meta: 1/1
existing C#/test/CSV/meta changes: 0
RNG registry/pass catalog changes: 0/0
Authoring/Generated changes: 0/0
asmdef/Scene/Prefab/Tilemap/Settings/Packages changes: 0
MAP11/MAP12_01~03/SpecialRegion artifact/source modifications: 0
duplicate GUID: 0
inbox/diff-check/unrelated staged: 0/0/0
prior/legacy/PlayMode/unfiltered selections: 0/0/0/0
Git push: NOT PERFORMED
```

initialization/import timeout으로 executed 0이면 PASS로 세지 않고 같은 category만 재시도한다.

## 13. Required Result

```text
MapDesign/MCP/REPORTS/MAP12_04_IMPLEMENT_EVENT_OVERLAY_ASSIGNMENT_RULES_RESULT.md
```

상단:

```text
TASK: MAP12_04_IMPLEMENT_EVENT_OVERLAY_ASSIGNMENT_RULES
STATUS: PASS | BLOCKED
MAP12_04: COMPLETE ELIGIBLE | NOT COMPLETE
MAP12_05_AUTHOR_STARTER_ACTIVITIES_AND_OVERLAYS: LOCKED / DO NOT START
```

`## User-Facing Implementation Report`에서 파일/책임/새 기능/파이프라인/미구현/가시성을 먼저 보고하고, `## Responsibility and Added Functions`에서 inputs/outputs/non-ownership/downstream을 명시한다.

이후 actual evidence:

- file/class/public surface
- Event/Empty profiles, opportunities, candidates/rejections
- TerrainCluster/Activity/Special marker compatibility
- Special overlap/persistence matrix
- World/Patch/Sector non-empty/Empty budgets와 rates
- cooldown selection/exclusion/gap
- population stream/draw/ticket와 Activity-stream independence
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

PASS일 때만 MAP12_04를 Finalize하고 task-owned 파일만 atomic commit한다.

```text
Subject: MAP12_04: implement event overlay assignment rules
Push: NOT PERFORMED
```

Result가 PASS여도 MAP12_05를 자동 시작하지 않는다. 별도 검수 전까지 계속 LOCKED다.
