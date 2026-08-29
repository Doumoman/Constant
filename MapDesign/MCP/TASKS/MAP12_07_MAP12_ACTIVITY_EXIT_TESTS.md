```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP12_07_MAP12_ACTIVITY_EXIT_TESTS
  task_file: TASKS/MAP12_07_MAP12_ACTIVITY_EXIT_TESTS.md
  requires_current_task: NONE
  requires_completed_task: MAP12_06_CREATE_ACTIVITY_PREVIEW_AND_PLAYMODE_FIXTURES
  requires_result:
    path: REPORTS/MAP12_06_CREATE_ACTIVITY_PREVIEW_AND_PLAYMODE_FIXTURES_RESULT.md
    status: PASS
    sha256: a2c9dfb7e78c94b57b4362b5026c271de9c606a4ff6cb8998516fd4bc641d569
  requires_installed_task:
    path: TASKS/MAP12_06_CREATE_ACTIVITY_PREVIEW_AND_PLAYMODE_FIXTURES.md
    sha256: 96c93459690878d35e7f1175fdebd3d2ebad60860918edc54d1f007533efc52f
  sets_current_task: MAP12_07_MAP12_ACTIVITY_EXIT_TESTS
```

# MAP12_07 — MAP12 Activity / Event Phase Exit Tests

```text
TASK: MAP12_07_MAP12_ACTIVITY_EXIT_TESTS
PHASE: MAP12 — ActivityStructure / EventOverlay
STATUS: CURRENT
NEXT: MAP13_01_IMPLEMENT_SPECIAL_FOOTPRINT_SITE_BRIDGE_AND_COORDINATES
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User-Facing Report

이번 Task는 새 Activity/Event production 기능을 만들지 않는다. MAP12_01~06이 만든 current public API와 physical authoring을 **전용 EditMode Phase Exit test 하나**로 연결해 다음만 승인하거나 차단한다.

```text
7 Activity + 5 Event physical authoring
→ Static Shell / slot compile
→ removal cue / safety proof
→ Activity 6~12% + Strong cap plan
→ Event 3~8% + cooldown + explicit Empty plan
→ read-only preview / prior lifecycle evidence
→ MAP12 Phase Exit verdict
```

Result의 첫 섹션은 반드시 한국어 `## User-Facing Implementation Report`로 작성하고 다음을 파일 단위로 보고한다.

1. 추가하거나 수정한 스크립트의 정확한 전체 경로
2. 각 스크립트와 test method가 맡은 책임
3. 이 Task로 새로 가능해진 것
4. 전체 생성 파이프라인에서의 위치
5. 아직 구현하지 않은 것
6. Unity Editor 또는 실제 게임 화면에서 보이는지 여부

두 번째 섹션은 반드시 `## Responsibility and Added Functions`로 작성한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| current MAP12 code/data의 Phase Exit 판정 | 새 Runtime compiler/planner/renderer |
| 7 Activity shell/removal 안전성 통합 승인 | 기존 MAP12 C#/CSV repair 또는 balance tuning |
| Activity/Event 빈도·cap·cooldown·결정론 승인 | 실제 Activity state machine, NPC, reward, meteor 실행 |
| marker-only Event와 explicit Empty 승인 | MAP13 SpecialRegion 구현 |
| static contract/witness 기준 softlock candidate 0 판정 | PlayerController/collider/physics 도달성 보증 |
| preview/current artifact 일치와 side-effect 0 확인 | legacy `19347` 또는 과거 category 재실행 |

Exit test는 current public importer/compiler/index/planner/preview API를 호출한다. CSV parser, graph traversal, route solver, RNG, frequency allocator 또는 preview renderer를 test 안에 다시 구현하지 않는다.

## 2. Focused-Only / No-Broad-Regression Policy

정상 실행은 category `MAP12_07` EditMode 하나뿐이다.

```text
MAP12_07 dedicated EditMode selection: required
MAP09/MAP10/MAP11 selections: 0
MAP12_01~06 category selections: 0
legacy 19347 selections: 0
PlayMode selections: 0
unfiltered EditMode/PlayMode selections: 0/0
```

MAP12_07 test에서 기존 public API를 호출하는 것은 이전 category를 재실행하는 것이 아니다. MAP12_06에서 승인된 PlayMode lifecycle `2/2 PASS`는 production/data가 바뀌지 않으므로 이번 Task에서 다시 실행하지 않는다.

실제 current artifact 문제를 발견한 경우:

1. 실패 invariant, 소유 Task, 원인과 최소 owner verification 범위를 Result에 기록한다.
2. 기존 production C#/CSV/test를 이 Task에서 수정하지 않는다.
3. 관련 없는 회귀나 전체 suite를 실행하지 않는다.
4. `STATUS: BLOCKED`, `MAP12 PHASE EXIT: NOT APPROVED`로 STOP한다.

Task-owned 신규 exit test의 compile/assertion 실수만 신규 test 파일에서 수정하고 `MAP12_07` category만 재실행할 수 있다.

## 3. Read-Only Preflight Authority

쓰기 전에 exact 확인한다.

```text
MAP12_06 Result: PASS
MAP12_06 Result SHA-256:
a2c9dfb7e78c94b57b4362b5026c271de9c606a4ff6cb8998516fd4bc641d569

MAP12_06 installed Task SHA-256:
96c93459690878d35e7f1175fdebd3d2ebad60860918edc54d1f007533efc52f

MAP12_06: COMPLETE
MAP12_07: CURRENT
MAP13_01: LOCKED
unrelated staged path: 0
Unity compile / relevant Console error: 0 / 0
```

Current physical authority:

```text
whole schema: 29 tables / 189 columns / 59 FK
Activity/Event schema: 10 tables / 71 columns
Authoring CSV/meta: 75 / 75
Activity/Event CSV/meta: 10 / 10
Generated CSV: 0
Activity entries: 7
Event entries: 5
Activity strength: Strong 4 / Ordinary 3
Activity authored slots: 52
non-empty Event / explicit Empty: 4 / 1
```

Current approved digests:

```text
aggregate authoring:
46330eb01dd302bf80dab6eacf88dea59f107cbecc9225b2243a395c1d0dbc8b

Activity catalog:
3ef83fae74d935a2469ab587414d0498cb423609b171d1c7633423e297318c3a

Event catalog:
2d2878f62605927a7b70a405a06079b3ebad7767e3bd7db9b6b2431177ea95a0
```

Baseline drift가 있으면 신규 test를 만들기 전에 `BLOCKED`로 보고한다. 승인 수치를 맞추기 위해 CSV나 production code를 자동 수정하지 않는다.

## 4. Exact Write Boundary

정상 경로에서는 신규 focused Exit test와 matching meta만 허용한다.

```text
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Activities/Map12ActivityPhaseExitTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Activities/Map12ActivityPhaseExitTests.cs.meta
```

```text
Assembly: MapAuthoring.Tests.EditMode
Namespace: StarNight.MapAuthoring.Tests.EditMode.WorldGeneration.Activities
Category: MAP12_07
```

수정 금지:

```text
existing production C# / test C# / CSV / meta
asmdef / asmref
Authoring / Generated content
Scene / Prefab / ScriptableObject / Tilemap / Material / Texture
Settings / Packages
```

새 production audit model, exporter, fixture asset, menu/window 또는 report asset을 만들지 않는다. assembly reference가 부족하면 production logic을 복사하지 말고 `BLOCKED`로 보고한다.

## 5. Dedicated Exit Matrix

`Map12ActivityPhaseExitTests`는 current physical content와 public API를 직접 사용한다. method 수는 가독성에 맞게 조정할 수 있지만, Result에는 아래 각 gate의 실제 PASS 증거를 분리해서 기록한다.

### A. Physical authority and atomic import

- exact 10개 Activity/Event CSV 경로에서만 atomic import한다.
- exact `7 Activity / 5 Event`, `10 tables / 71 columns`, whole `29/189/59`를 확인한다.
- Authoring `75/75`, target `10/10`, Generated `0`을 확인한다.
- aggregate/Activity/Event digest가 preflight와 exact 같다.
- missing/duplicate/FK 오류, partial publication과 filesystem output은 `0`이다.
- input reverse enumeration, repeat import, invariant culture와 `tr-TR` culture에서 catalog/digest가 같다.

### B. All seven Activity shell and removal proof

모든 physical Activity를 referenced TerrainCluster/SpineVariant의 current public chain으로 compile한다.

- Activity ID와 TerrainCluster/variant, profile, strength, weight, slot/node/cue/edge가 source authority와 같다.
- shell compile, cue compile와 removal safety proof가 모두 성공한다.
- cue→activation→core→reward→recovery→exit success witness와 failure/reset witness가 존재한다.
- Entry, Exit, SafePocket, Recovery, mandatory reward preservation witness가 존재한다.
- Static/Active/Removed에서 underlying shell, access, traversal, protection identity가 같다.
- Removed 상태의 Activity/Event residual marker, underlying tile/collider delta, synthetic carve/edge와 RNG draw는 모두 `0`이다.
- all seven aggregate error/partial publication은 `0`이다.

### C. Activity compatibility, 6~12% frequency and Strong caps

Physical 7 Activity profiles와 current public candidate index/planner를 사용한다. test-owned opportunity는 기존 value type과 valid BiomePatch/Sector/TerrainCluster evidence만 조립하며 production content로 저장하지 않는다.

- 모든 physical profile이 최소 하나의 valid opportunity에 compatible이다.
- biome/pacing/access/footprint/clearance/protected-cell mismatch는 stable rejection을 낸다.
- 100 eligible opportunity에서 TargetPermille `60`, `80`, `120`은 각각 exact `6`, `8`, `12` selected를 게시한다.
- `59`와 `121`은 stream/draw `0/0`에서 atomic rejection한다.
- World→Patch→Sector child budget 합계가 parent/world target과 exact 같다.
- World/Patch/Sector Strong cap은 한 번도 초과하지 않는다.
- Strong cap `0/0/0`에서는 Ordinary fallback으로 feasible target을 채우고 Strong selected/counter는 `0`이다.
- Strong-only와 cap 충돌은 cap을 깨지 않고 `StrongCapUnsatisfiable` 또는 current equivalent error로 atomic failure한다.

### D. Event marker-only, 3~8%, cooldown and Empty

Physical 5 Event profiles와 current public Event candidate index/planner를 사용한다.

- non-empty exact 4종은 marker assignment exact `1`, `EVT_EMPTY`는 marker/weight/gap `0/0/0`이다.
- Event plan 전후 geometry, collision, route, access, pacing, Static Shell과 protection digest가 같다.
- 100 eligible opportunity에서 TargetPermille `30`, `50`, `80`은 non-empty exact `3`, `5`, `8`과 Empty `97`, `95`, `92`를 게시한다.
- `29`와 `81`은 population stream/draw `0/0`에서 atomic rejection한다.
- 같은 Event ID의 consecutive selection은 `MinimumProgressionGap` 이상이고 exclusion evidence가 존재한다.
- 다른 Event와 Empty는 해당 Event cooldown을 소비하거나 초기화하지 않는다.
- cooldown으로 target을 채울 수 없으면 cooldown을 무시하지 않고 atomic failure한다.
- non-empty가 아닌 모든 eligible opportunity는 explicit Empty decision exact 하나를 받는다.

### E. RNG isolation and deterministic publication

- Activity planner는 `RNG_SECTOR_RECIPE`, Event planner는 `RNG_POPULATION`만 사용한다.
- 한 planner 실행이 다른 stream의 state/draw/first value를 바꾸지 않는다.
- same input/order/seed/attempt는 decision, budget, witness와 canonical digest가 같다.
- reverse input/order, repeat와 `tr-TR` culture에서도 digest가 같다.
- seed 또는 attempt의 one-field change는 valid plan을 유지하면서 plan digest를 바꾼다.
- invalid input은 publication/digest/stream/draw가 원자적으로 `0`이다.
- collections은 immutable/canonical order이고 duplicate decision/candidate key는 `0`이다.

### F. Removal, lifecycle evidence and static softlock candidates

이번 gate의 `softlock candidate 0`은 current static contract/witness 판정이다. 실제 PlayerController, collider, jump arc 또는 physics reachability를 주장하지 않는다.

모든 7 Activity와 compatible Event 조합에서 아래 candidate count가 `0`인지 current public proof/witness로 확인한다.

```text
missing or broken Entry→Exit witness
Removed shell/access/traversal/protection identity mismatch
missing SafePocket or Recovery witness
permanent Exit or mandatory reward destruction
residual Activity/Event marker after removal
missing interrupted/re-entry witness or duplicate marker
synthetic carve/teleport/fallback edge
```

MAP12_06 PlayMode가 이미 증명한 exact 다섯 representative lifecycle은 prior approved evidence로 Result에 인용만 한다.

```text
Ricochet Mine + Meteor Fall
Escort Cart + Wandering Merchant
Escort Cart + Rare Creature
Maru Rewind Anomaly + Maru Intervention
Time Trial + Empty
```

이번 Task의 PlayMode selection은 `0`이어야 한다. MAP12_06 Result SHA가 preflight와 같고 current production/data가 수정되지 않았다는 것으로 prior lifecycle evidence의 유효성을 확인한다.

### G. Preview consistency and read-only boundary

- preview selector는 physical Activity `7`, Event `5`를 exact 표시한다.
- 모든 Activity가 Static/Active/Removed/Compare snapshot과 removal proof를 게시한다.
- non-empty Event 네 개 marker count는 각각 `1`, Empty는 `0`이다.
- preview snapshot의 shell/route/access/protection/source owner/digest가 compiler artifact와 같다.
- preview API/window는 read-only이고 Authoring/Generated/Scene/Prefab/Tilemap mutation이 `0`이다.
- 메뉴 `Tools/MapDesign/Activity & Event Preview`와 title 계약은 유지한다.

### H. Negative atomic fixtures

physical CSV를 수정하지 않고 test-owned in-memory input으로 최소 아래 실패를 확인한다.

```text
duplicate Activity/Event ID or candidate key → atomic failure / publication 0
missing Empty or duplicate Empty → atomic failure / plan 0
invalid clearance or protected overlap → candidate rejection / no placement
removal proof identity mismatch → compile failure / proof 0
invalid Event marker/operation/source owner → assignment rejection / no mutation
Strong cap or cooldown unsatisfiable → cap/cooldown 유지 / plan 0
```

Failure fixture는 기존 public validation API에 직접 입력한다. production defect를 가리는 fixture-only parser, solver, fallback 또는 carve를 만들지 않는다.

## 6. Focused Verification and Static Gates

Unity refresh/compile 후 category `MAP12_07` EditMode만 실행한다.

```text
MAP12_07 discovered = executed
pass = executed
fail / skip / inconclusive = 0 / 0 / 0
compile / relevant Console error = 0 / 0
prior category selections = 0
legacy selections = 0
PlayMode selections = 0
unfiltered selections = 0
```

Test Runner initialization timeout으로 executed `0`이면 PASS로 세지 않는다. filter를 확인해 재시도하고 계속 실행 불가하면 `BLOCKED`로 보고한다.

Static scope:

```text
new Map12ActivityPhaseExitTests.cs/meta: exact 1/1
existing C#/test/CSV/meta modifications: 0
Authoring/Generated content modifications: 0
asmdef/Scene/Prefab/Settings/Packages modifications: 0
current three catalog digests unchanged
duplicate GUID: 0
unapplied candidate/diff-check/unrelated staged: 0/0/0
Git push: NOT PERFORMED
```

## 7. Required Result and User-Visible Reporting

Result 경로:

```text
MapDesign/MCP/REPORTS/MAP12_07_MAP12_ACTIVITY_EXIT_TESTS_RESULT.md
```

상단 verdict:

```text
TASK: MAP12_07_MAP12_ACTIVITY_EXIT_TESTS
STATUS: PASS | BLOCKED
MAP12 PHASE EXIT: APPROVED | NOT APPROVED
MAP12_07: COMPLETE ELIGIBLE | NOT COMPLETE
MAP13_01_IMPLEMENT_SPECIAL_FOOTPRINT_SITE_BRIDGE_AND_COORDINATES: LOCKED / DO NOT START
```

첫 섹션은 다음 형식과 책임을 빠짐없이 채운다.

```text
## User-Facing Implementation Report

추가/수정 스크립트:
- exact path와 신규/수정 여부

스크립트 책임:
- test class/method별 실제 책임과 input → verdict

이번에 새로 가능해진 것:
- MAP12 Phase를 어떤 증거로 승인/차단할 수 있게 되었는지

파이프라인 위치:
- physical CSV → shell/removal → Activity/Event plan → preview/lifecycle evidence → Exit verdict

아직 미구현:
- actual gameplay state machine, MAP13 SpecialRegion, world placement, Tilemap/physics 등

Editor/게임 가시성:
- 신규 gameplay 화면 0
- 기존 Activity & Event Preview 메뉴 유지 여부
- 신규 test는 Test Runner에서만 보이는지
```

두 번째 `## Responsibility and Added Functions`에는 아래를 보고한다.

| Field | Required evidence |
|---|---|
| Task responsibility | MAP12 current-code/data Phase Exit 판정 |
| Added script | 신규 test class/meta exact 경로 |
| Added functions | test method별 gate와 production 기능 추가 0 |
| Inputs consumed | MAP12_01~06 public authorities와 physical content |
| Outputs produced | shell/removal/frequency/cap/cooldown/determinism/softlock-candidate verdict |
| Explicit non-ownership | repair, balance tuning, gameplay, MAP13, world/Tilemap/physics 미구현 |
| Downstream consumer | 별도 검수 후 MAP13_01만 unlock 가능 |

그 뒤 actual evidence를 생략 없이 기록한다.

- added/modified file manifest와 각 책임
- preflight inventory와 세 digest
- 7 Activity shell/removal matrix와 witness/residual totals
- Activity `60/80/120` permille와 Strong cap 결과
- Event `30/50/80` permille, cooldown, Empty 결과
- Activity/Event RNG isolation과 deterministic digest 결과
- static softlock candidate 각 종류 count
- prior MAP12_06 lifecycle evidence가 재사용 가능한 이유와 PlayMode selection `0`
- preview selector/snapshot/read-only consistency
- negative fixture와 atomic failure 결과
- focused test discovered/executed/pass/fail/skip
- regression selection `0` 또는 실제 trigger owner/reason/minimum scope
- static/change scope와 unrelated staged `0`
- commit handoff

정상 경로 문구:

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
```

PASS일 때만 `MAP12 PHASE EXIT: APPROVED`로 기록하고 Status Finalize 후 task-owned test/meta/protocol 파일만 atomic commit한다.

```text
Subject: MAP12_07: approve Activity and Event phase exit
Push: NOT PERFORMED
```

Result가 PASS여도 MAP13_01을 자동 시작하지 않는다. 사용자가 Result를 전달하고 별도 검수받을 때까지 계속 LOCKED다.
