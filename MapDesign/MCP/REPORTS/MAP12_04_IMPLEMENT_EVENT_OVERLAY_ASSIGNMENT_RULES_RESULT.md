TASK: MAP12_04_IMPLEMENT_EVENT_OVERLAY_ASSIGNMENT_RULES
STATUS: PASS
MAP12_04: COMPLETE ELIGIBLE
MAP12_05_AUTHOR_STARTER_ACTIVITIES_AND_OVERLAYS: LOCKED / DO NOT START

## User-Facing Implementation Report

MAP12_04는 검증된 EventOverlay 계약을 TerrainCluster, 선택된 Activity, SpecialRegion의 기존 marker 기회에 연결하는 불변 후보 인덱스와 assignment plan을 구현했다. 계획 결과만 게시하며 Canvas, Static Shell, geometry, collision, route, access, pacing, envelope, Prefab, Scene, Tilemap 또는 persistence 소유권을 변경하지 않는다.

- `EventOverlayAssignment.cs`: marker source/owner/좌표와 Canvas·Static Shell·protection·persistence 전후 증거, Event profile, progression opportunity, compatibility rejection, frequency policy/error 모델을 제공한다.
- `EventOverlayCandidateIndex.cs`: Event/Empty profile과 opportunity의 identity/digest/marker 계약을 검증하고, biome/pacing/access/Activity/SpecialRegion compatibility를 판정해 canonical immutable candidate/rejection index를 게시한다. 각 opportunity에는 정확히 하나의 compatible Empty와 하나 이상의 non-empty Event를 요구한다.
- `EventOverlayAssignmentPlanner.cs`: 30..80 permille 정책, round-half-up과 strict integer band, World→BiomePatch→Sector largest-remainder budget, ProgressionOrdinal cooldown, 명시적 Empty remainder, `RNG_POPULATION` SPAWN scope priority/weighted draw를 사용해 atomic plan을 게시한다.
- `EventOverlayAssignmentTests.cs`: TerrainCluster/Activity/Special marker, Special Npc/Reward/Event matrix, 금지 slot/Fixed Shell/persistence, Empty, 3~8% budget, cooldown, deterministic population RNG, MAP12_03 digest 보존, invalid-input zero draw, no-mutation/immutability를 검증하는 `MAP12_04` EditMode focused test 18개를 제공한다.

실제 100-opportunity fixture에서 non-empty/Empty 결정은 `8/92`, World rate는 `8/100 = 80 permille`, Patch budget은 `4/50 + 4/50`, Sector assigned 합계는 `8`이었다. 100개의 독립 Spawn stream에서 priority draw 100회와 선택 위치 weighted draw 8회, 총 108 draw를 게시했다. MAP12_05는 시작하지 않았다.

## Responsibility and Added Functions

### Inputs

- validated `EventOverlayContract`와 canonical SHA-256 digest, integer weight, non-negative `MinimumProgressionGap`, compatible biome/pacing/access, optional Activity identity
- stable opportunity ID, `SectorCoord`, `BiomePatchId`, unique non-negative `ProgressionOrdinal`, TerrainCluster/optional selected Activity identity, exact MAP12_03 plan digest
- marker ID/source kind/source owner/source·compiled coordinate/slot kind와 Canvas·Static Shell·protection·persistence 전후 증거
- optional validated SpecialRegion contract/digest와 exact ReplaceableSlot identity
- target permille, world seed, non-negative attempt ordinal, 기존 `DeterministicRngStreamFactory`

### Outputs

- canonical immutable `EventOverlayCandidateIndex`, compatible candidates, stable `EventOverlayCompatibilityRejection`
- World/Patch/Sector eligible/target/assigned/Empty/rational-rate evidence를 가진 `EventOverlayScopeBudget`
- Assigned/Empty, Event identity, candidate key, population scope, priority/weight/ticket/draw, previous/current ordinal, required/actual gap, cooldown exclusion evidence를 가진 `EventOverlayAssignmentDecision`
- atomic `EventOverlayAssignmentPlan` 또는 stable-sorted `EventOverlayAssignmentError`

### Non-Ownership

- 실제 Event/Npc/Reward spawn, state-machine 실행, marker mutation, Canvas/Static Shell/geometry/collision/route/access/pacing/envelope 변경을 수행하지 않는다.
- SpecialRegion persistence key의 소유권을 Event로 이전하지 않고 Entry/Return port, Fixed Shell, Facility/Enemy slot을 변경하지 않는다.
- Activity assignment/frequency plan, TerrainCluster/Activity/SpecialRegion 계약, RNG registry, pass catalog, CSV, Authoring/Generated, asmdef, Scene/Prefab/Tilemap을 수정하지 않는다.

### Downstream

- MAP12_05는 별도 검수 후 이 plan의 explicit Assigned/Empty 결정과 canonical evidence를 starter content 입력으로 사용할 수 있다.
- MAP12_05는 이번 실행에서 계속 LOCKED이며 자동 시작되지 않았다.

## Public Surface and Compatibility Evidence

| Surface | PASS observation |
|---|---|
| marker source | exact `TerrainCluster / Activity / SpecialRegion` source kind, owner, coordinate, owning slot evidence |
| profile | validated Event contract/digest, non-empty weight `1..10000`, cooldown `>= 0`, optional Activity identity |
| baseline fixture | profiles/opportunities/candidates/rejections = `3/100/300/0` |
| Activity marker | selected Activity and contract/profile Activity identity matched exactly |
| Special Npc | `SpawnNpc / Npc -> Npc` ReplaceableSlot |
| Special Reward | `SpawnReward / Reward -> Reward` ReplaceableSlot with persistence provenance preserved |
| Special Event | State/Cosmetic marker operations -> Event ReplaceableSlot |
| rejected Special kinds | Fixed Shell overlap and Facility/Enemy/Entry/Return targets rejected atomically |
| marker-only proof | source/compiled coordinate and underlying Canvas/Static Shell/protection/persistence before/after equal; all six non-marker mutation counts zero |

## Frequency, Empty, Cooldown, and RNG Evidence

| Scope | Eligible | Target | Assigned | Empty | Rate |
|---|---:|---:|---:|---:|---:|
| World | 100 | 8 | 8 | 92 | 80 permille |
| PATCH_A | 50 | 4 | 4 | 46 | 80 permille |
| PATCH_B | 50 | 4 | 4 | 46 | 80 permille |
| Sector total | 100×1 | 8 | 8 | 92 | discrete 0/1 |

- 30/80 permille inclusive 정책은 각각 3/8 non-empty 결정을 게시했고, 29/81은 stream/draw `0/0`에서 거부됐다.
- low-sample 1-opportunity fixture는 strict integer band가 불가능함을 `DiscreteApproximation=true`와 explicit Empty로 게시했다.
- World/Patch/Sector target 및 selected 합계가 각각 8로 정확히 닫혔다.
- 모든 미선택 opportunity는 정확히 하나의 Empty decision을 게시했고 Empty는 weighted draw를 소비하지 않았다.
- 동일 Event ID cooldown은 ProgressionOrdinal 차이만 사용했다. 다른 Event ID가 cooldown 후보를 대체하는 fixture에서 exclusion evidence가 게시됐고 모든 chosen gap이 required gap 이상이었다.
- same-ID only + gap 200 fixture는 quota를 채울 수 없어 `CooldownMakesTargetUnsatisfiable`로 plan을 원자적으로 미게시했다.
- scope identity는 정확히 `EVENT|<sector x,y>|<opportunity ID>`, stream/reset scope는 `RNG_POPULATION / SPAWN`이다.
- 동일 index/seed/attempt는 locale과 입력 순서가 달라도 동일 digest를 재현했고 seed 또는 attempt 변경은 digest를 변경했다.
- 100-opportunity fixture의 streams / priority draws / weighted draws / total draws는 `100 / 100 / 8 / 108`이다.
- MAP12_03 plan digest는 그대로 게시됐고 `RNG_SECTOR_RECIPE` 사용·draw는 0이었다.

## Negative Atomic Matrix

| Case | Publication | RNG evidence |
|---|---|---|
| null request / missing index | plan/digest 0 | streams/draws 0/0 |
| TargetPermille 29 or 81 | plan/digest 0 | streams/draws 0/0 |
| missing or duplicate Empty | index/digest 0 | streams/draws 0/0 |
| missing/duplicate marker or kind-operation mismatch | stable error/rejection; invalid candidate excluded | index RNG 0/0 |
| coordinate/Canvas/Shell/protection/non-marker mutation | index/digest 0 | streams/draws 0/0 |
| invalid Special slot/Fixed Shell/port/persistence provenance | index/digest 0 | streams/draws 0/0 |
| cooldown makes target unsatisfiable | plan/digest 0 | consumed population evidence retained only on failure result |
| invalid population binding/negative attempt | plan/digest 0 | streams/draws 0/0 |

## Focused Validation

```text
Unity compile / Console error / warning: 0 / 0 / 0
MAP12_04 EditMode discovered / executed / passed: 18 / 18 / 18
failed / skipped / inconclusive: 0 / 0 / 0
final focused job: 88aef579c093421786b1ad51d790a64b
initial import/runner retry: same MAP12_04 category only; no PASS claimed for executed 0

REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY TEST SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
```

## Static and Change Scope

```text
new Runtime C#/meta: 3 / 3
new focused test C#/meta: 1 / 1
existing C#/test/CSV/meta changes: 0
RNG registry/pass catalog changes: 0 / 0
Authoring/Generated changes: 0 / 0
asmdef/Scene/Prefab/Tilemap/Settings/Packages changes: 0
MAP09/MAP10/MAP11/MAP12_01~03 artifact/source modifications: 0
duplicate GUID: 0
installed/archive Task SHA-256: e802edf042683f09e1c5f6ee5d3ad68c688a03f56ed095bc288a69254457916c
installed/archive byte-identical: YES
inbox/diff-check/unrelated staged: 0 / 0 / 0
pre-existing unrelated untracked meta files preserved and excluded: 3
Git push: NOT PERFORMED
```

Atomic commit subject: `MAP12_04: implement event overlay assignment rules`.
