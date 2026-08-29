TASK: MAP12_07_MAP12_ACTIVITY_EXIT_TESTS
STATUS: PASS
MAP12 PHASE EXIT: APPROVED
MAP12_07: COMPLETE ELIGIBLE
MAP13_01_IMPLEMENT_SPECIAL_FOOTPRINT_SITE_BRIDGE_AND_COORDINATES: LOCKED / DO NOT START

## User-Facing Implementation Report

추가/수정 스크립트:

- `Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Activities/Map12ActivityPhaseExitTests.cs` — 신규. MAP12_01~06의 current public importer/compiler/index/planner/preview API와 physical authoring을 하나의 MAP12 phase-exit category로 연결한다.
- `Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Activities/Map12ActivityPhaseExitTests.cs.meta` — 신규. exit-test MonoScript의 고유 Unity GUID를 제공한다.
- production C#, 기존 test C#, CSV/meta, asmdef/asmref, Scene/Prefab/Tilemap/Settings/Packages 수정은 0이다.

스크립트 책임:

- `PhysicalAuthorityAtomicImportAndCultureDeterminismExitGate` — 29/189/59 전체 schema, Activity/Event 10/71 schema, 75/75 authoring, 7/5 content, 세 승인 digest, atomic invalid import와 reverse/repeat/`tr-TR` 결정을 검증한다.
- `AllSevenShellRemovalAndStaticSoftlockExitGate` — physical Activity 7개를 TerrainCluster validation부터 footprint, role socket, traversal, route witness, pattern-free canvas, Activity shell, cue, removal proof까지 public chain으로 compile하고 정적 softlock candidate 7종을 합산한다.
- `ActivityFrequencyCompatibilityStrongCapAndRateExitGate` — physical Activity profile 7개, mismatch rejection, 60/80/120 permille, 계층 budget, Strong cap, Ordinary fallback과 Strong-only atomic failure를 검증한다.
- `EventMarkerOnlyCooldownEmptyAndRateExitGate` — physical Event profile 5개, marker-only invariants, 30/50/80 permille, explicit Empty, cooldown evidence와 cooldown-unsatisfiable atomic failure를 검증한다.
- `CrossPlannerDeterminismRngIsolationAndImmutabilityExitGate` — reverse/repeat/culture/seed/attempt determinism, `RNG_SECTOR_RECIPE`/`RNG_POPULATION` isolation, invalid-input zero RNG, immutable publication과 duplicate key 0을 검증한다.
- `PreviewReadOnlyAndPriorLifecycleEvidenceExitGate` — selector 7/5, Static/Active/Removed/Compare, Event marker 1/1/1/1/0, preview window 계약, authoring/generated 무변경과 MAP12_06 lifecycle Result exact SHA를 검증한다.
- `NegativeAtomicFixturesExitGate` — duplicate candidate, missing/duplicate Empty, clearance/protected overlap, removal identity mismatch, invalid Event operation/source owner, Strong cap/cooldown unsatisfied를 publication 0으로 차단한다.

이번 Task로 새로 가능해진 것은 current MAP12 physical authority가 shell/removal, Activity/Event planning, RNG, preview와 prior lifecycle 증거를 함께 만족할 때만 MAP12 phase exit를 승인하는 단일 focused 판정이다. 생성 파이프라인에서 physical CSV import 뒤, shell/removal compile과 Activity/Event plan 뒤, read-only preview/lifecycle evidence를 확인한 다음 exit verdict를 게시하는 위치다.

아직 구현하지 않은 것은 실제 Activity state machine, meteor/NPC/reward/Maru payload 실행, world placement, 저장 gameplay object, MAP13 SpecialRegion bridge/site coordinates, Tilemap/collider/physics/player reachability다. 이 test는 static contract/witness softlock candidate를 판정하며 실제 물리 도달성을 주장하지 않는다.

Editor/게임 가시성:

- 신규 gameplay 화면 또는 runtime object: 0.
- 기존 `Tools/MapDesign/Activity & Event Preview` 메뉴와 `Activity & Event Preview` window: 유지되며 7 Activity/5 Event와 3개 상태 panel을 read-only로 표시한다.
- 신규 exit test: Unity Test Runner의 `MAP12_07` EditMode category에서만 보인다.

## Responsibility and Added Functions

| Field | Evidence |
|---|---|
| Task responsibility | current MAP12 code/data의 phase-exit 판정 |
| Added script | `Map12ActivityPhaseExitTests.cs`와 matching `.meta` exact 1/1 |
| Added functions | 위 7개 test gate와 test-owned evidence builder; production 기능 추가 0 |
| Inputs consumed | MAP12_01~06 public authorities, physical 10 CSV, 7 Activity, 5 Event, MAP12_06 Result |
| Outputs produced | shell/removal/frequency/cap/cooldown/determinism/static-softlock/preview exit verdict |
| Explicit non-ownership | repair, balance tuning, gameplay, MAP13, world/Tilemap/physics 구현 |
| Downstream consumer | 별도 검토 뒤 새 inbox patch만 MAP13_01을 열 수 있음 |

## Preflight Authority and Inventory

| Evidence | Result |
|---|---|
| MAP12_06 Result SHA-256 | `a2c9dfb7e78c94b57b4362b5026c271de9c606a4ff6cb8998516fd4bc641d569` |
| MAP12_06 installed Task SHA-256 | `96c93459690878d35e7f1175fdebd3d2ebad60860918edc54d1f007533efc52f` |
| MAP12_07 installed/archive SHA-256 | `9cc540315e11798536669f344908acbf201675314185dd6aa44e6c93564c39f8` / same |
| schema | 29 tables / 189 columns / 59 FK |
| Activity/Event schema | 10 tables / 71 columns |
| Authoring CSV/meta | 75 / 75 |
| Activity/Event CSV/meta | 10 / 10 |
| Generated CSV | 0 |
| entries / strength / slots | Activity 7, Event 5 / Strong 4, Ordinary 3 / 52 |
| Event non-empty / explicit Empty | 4 / 1 |
| aggregate authoring digest | `46330eb01dd302bf80dab6eacf88dea59f107cbecc9225b2243a395c1d0dbc8b` |
| Activity catalog digest | `3ef83fae74d935a2469ab587414d0498cb423609b171d1c7633423e297318c3a` |
| Event catalog digest | `2d2878f62605927a7b70a405a06079b3ebad7767e3bd7db9b6b2431177ea95a0` |

Reverse enumeration, repeat import와 `tr-TR` import는 세 digest를 그대로 게시했다. duplicate/FK/missing Empty/bad source owner input은 `Published=false`, 두 catalog와 aggregate digest publication 0으로 종료했다.

## Seven Activity Shell and Removal Matrix

| Activity | Shell digest | Removal digest | cue / safe / recovery / critical | residual / tile delta / RNG |
|---|---|---|---:|---:|
| `ACT_CRATER_BOULDER_CHAIN` | `863c3843f4681cb3e02cb5aab8290137f44c85a8c34469139d3b3dd6366eb863` | `db024599456c445af6cf9ebecb7d3f9b701192be2b745b8475daf63d137498b8` | 1 / 1 / 1 / 2 | 0 / 0 / 0 |
| `ACT_CRATER_RICOCHET_MINE` | `9cc2f5ae59a50a5de28dac67819300e96b3fb985d6c23dfa0d83ada10c03fd95` | `49542819be5e4f0848c348c546b197a494cf675d5506c448c2d1454ec57e8bc1` | 1 / 1 / 1 / 2 | 0 / 0 / 0 |
| `ACT_DOUGH_TIME_TRIAL` | `62eddc814ca5ea891aad88585a200372088f86b640aaf2e5c0427d23409f0760` | `82b16a8231b4e691e9eb5974e3aec55ba5d9016def95aa281cdbaad07619b4e6` | 1 / 1 / 1 / 2 | 0 / 0 / 0 |
| `ACT_MARU_REWIND_ANOMALY` | `30a34d64d7cd4fe768d94c2e37a22fa61bd616388bfc307b0e922517f68aa95f` | `c532b85c9fc47aa88e18426d75ba9c960d4fad5842d4f99841b4d81ac5c835c9` | 1 / 1 / 1 / 2 | 0 / 0 / 0 |
| `ACT_MILL_ESCORT_CART` | `d1262c3ef086105b5a2619ebba4e17602e2b256761f93635f5cd07f71b5b230e` | `a3a1301c0b843a74f498d71d6a860de75d713784d193d2b7f8bfe278c8ae5583` | 1 / 1 / 1 / 2 | 0 / 0 / 0 |
| `ACT_MILL_GEAR_GRID` | `658c6999e2e1f3c247b15c97c471ab47c6b18849ddffaae585b8a482335125ee` | `a1f8143aff491274447fe544d216af5eba01c8dec4f9932dbb82aee57c12618e` | 1 / 1 / 1 / 2 | 0 / 0 / 0 |
| `ACT_MILL_PESTLE_WORKSHOP` | `d82e6e083e6f578a1d4315f9944fc67d0fc534971d8f3ec658e736e87da3da9d` | `3024f8f75bac2147f9d08580213040757e1c7a7b04bb0753b77fb2725047cf87` | 1 / 1 / 1 / 2 | 0 / 0 / 0 |

모든 cue는 activation boundary 전이고 모든 recovery proof는 source edge only, synthetic edge/teleport 0이다. Active/Removed static shell, working canvas, traversal, route witness, route type와 access identity가 동일하다. Entry/Exit, SafePocket/Recovery, mandatory Exit/reward preservation을 모두 게시했다.

Static softlock candidate counts:

```text
missing or broken Entry/Exit witness: 0
Removed shell/access/traversal/protection identity mismatch: 0
missing SafePocket or Recovery witness: 0
permanent Exit or mandatory reward destruction: 0
residual Activity/Event marker after removal: 0
missing lifecycle witness or duplicate marker: 0
synthetic carve/teleport/fallback edge: 0
```

## Activity Frequency, Compatibility and Strong Caps

| Target permille | Eligible | Selected | Strong | patch target sum | sector target sum |
|---:|---:|---:|---:|---:|---:|
| 60 | 100 | 6 | 4 | 6 | 6 |
| 80 | 100 | 8 | 5 | 8 | 8 |
| 120 | 100 | 12 | 8 | 12 | 12 |

Physical profile 7개가 모두 최소 한 candidate를 게시했고 duplicate candidate key는 0이다. biome/pacing/access/active-chunk/cluster/variant/shell/removal/clearance/reserved/protected mismatch code가 안정적으로 게시됐다. 59/121 permille은 plan/stream/draw `0/0/0`이다. Strong cap `0/0/0`의 Ordinary-only index는 8 Ordinary/0 Strong을 선택했고, Strong-only index는 `StrongCapUnsatisfiable`로 plan 0을 게시했다.

## Event Frequency, Cooldown and Explicit Empty

| Target permille | Eligible | Assigned non-empty | Explicit Empty |
|---:|---:|---:|---:|
| 30 | 100 | 3 | 97 |
| 50 | 100 | 5 | 95 |
| 80 | 100 | 8 | 92 |

Physical non-empty Event 4개는 각각 assignment/marker 1개이고 `EVT_EMPTY`는 marker/weight/gap `0/0/0`이다. main 100-opportunity index는 physical 5 profile을 소비하고 compatible Meteor/Empty candidate를 각각 100개 게시했다. 모든 assigned decision은 prior ordinal이 있으면 actual gap이 required gap 이상이다. physical Meteor gap 4를 유지했고 test-owned high-gap probe는 cooldown exclusion evidence 7개를 게시했다. gap 200 단독 non-empty fixture는 `CooldownMakesTargetUnsatisfiable`로 plan 0을 게시했다. 29/81 permille은 plan/stream/draw `0/0/0`이다. geometry/collision/route/access/pacing/envelope mutation은 0이다.

## RNG Isolation and Deterministic Publication

- Activity canonical plan digest: `6ddc51511f8f22edd46f3d2158150edc98424b1887da0c0cd1d2a983cd65eb68`.
- Event canonical plan digest: `543a3de8f234fdf41fc55bc96674a25c29c902d300571033c5114ea3765219ce`.
- reverse order, repeat와 `tr-TR`/`ko-KR` 전환은 각 canonical digest와 decision evidence를 유지했다.
- seed 또는 attempt 한 필드 변경은 두 planner 모두 valid하지만 다른 plan digest를 게시했다.
- Activity는 `RNG_SECTOR_RECIPE`, Event는 `RNG_POPULATION`만 사용했다. 반대 stream의 first value는 전/후 동일했다.
- invalid input은 publication/stream/draw 0이고 candidate/decision duplicate key 0이다.
- Activity/Event plan decision collection은 immutable이며 world mutation surface는 0이다.

## Preview and Prior Lifecycle Evidence

Preview는 selector 7/5, Activity snapshot 7개, representative Event pair 5개, marker count `1/1/1/1/0`을 게시했다. 모든 Activity의 Static/Active/Removed underlying, route, access, protection identity가 유지되고 Compare는 marker-only다. window open/reload/compare/close 전후 Authoring/Generated tree digest가 동일하다.

MAP12_06 Result bytes는 승인 SHA `a2c9dfb7e78c94b57b4362b5026c271de9c606a4ff6cb8998516fd4bc641d569`와 일치했고 아래 5 lifecycle fixture와 prior PlayMode `2/2/2` evidence를 포함했다.

```text
ACT_CRATER_RICOCHET_MINE + EVT_METEOR_FALL
ACT_MILL_ESCORT_CART + EVT_WANDERING_MERCHANT
ACT_MILL_ESCORT_CART + EVT_RARE_CREATURE
ACT_MARU_REWIND_ANOMALY + EVT_MARU_INTERVENTION
ACT_DOUGH_TIME_TRIAL + EVT_EMPTY
```

이번 Task의 PlayMode selection은 0이다.

## Negative Atomic Fixtures

| Fixture | Verdict |
|---|---|
| duplicate Activity candidate | `DuplicateCandidate`, index 0, RNG 0 |
| missing / duplicate Empty | `MissingEmptyVariant` / `DuplicateEmptyVariant`, index 0 |
| malformed clearance / protected overlap | candidate rejection, placement 0 |
| removal identity mismatch | `InvalidActiveSnapshot`, proof 0 |
| invalid Event operation | `InvalidMarkerOperation`, index 0 |
| invalid physical source owner | atomic import publication 0 |
| Strong cap / cooldown unsatisfied | explicit error, plan 0 |

## Focused Verification

Final allowed job:

```text
Unity: 6000.3.8f1
MAP12_07 EditMode final job: d0df108660ff4e98ad32de3384835710
discovered / executed / passed: 7 / 7 / 7
failed / skipped / inconclusive: 0 / 0 / 0
duration: 3.1062241 seconds
compile / final relevant Console error / warning: 0 / 0 / 0
```

Task-owned corrective history:

- `454ff8f99544494783bbc0161b6d42e0`: pre-import Test Runner initialization returned discovered/executed 0/0 and was not accepted.
- `e1a804022125463c94ff2036db94bd66`: 7 executed, 3 PASS / 4 FAIL; 신규 fixture ID prefix, valid mismatch enum과 duplicate ownership construction을 corrected.
- `b78be0ba16334296b402a5d64876b457`: 7 executed, 5 PASS / 2 FAIL; cooldown evidence probe와 earlier atomic invalid-operation contract assertion을 corrected.
- 모든 correction은 신규 `Map12ActivityPhaseExitTests.cs` 안에서만 수행했고 같은 `MAP12_07` category만 다시 선택했다.

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
```

## Static Scope and Commit Handoff

```text
new Map12ActivityPhaseExitTests.cs/meta: 1/1
existing production C#/test/CSV/meta modifications: 0
Authoring/Generated content modifications: 0
asmdef/Scene/Prefab/Settings/Packages modifications: 0
current aggregate/Activity/Event digests unchanged: YES
new GUID occurrence: 1
unapplied candidate/diff-check/unrelated staged: 0/0/0
pre-existing unrelated untracked meta excluded: 3
Git push: NOT PERFORMED
```

Atomic commit handoff:

```text
base HEAD: facdffa20698f0cb4dfb0b4ca3e3f798a47043d0
subject: MAP12_07: approve Activity and Event phase exit
allowlist: installed Task, archive, exit test/meta, Result, status finalize only
MAP13_01 start: NOT PERFORMED
```

Result: PASS
