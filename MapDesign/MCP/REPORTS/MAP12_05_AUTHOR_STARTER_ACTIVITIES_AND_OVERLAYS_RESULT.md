TASK: MAP12_05_AUTHOR_STARTER_ACTIVITIES_AND_OVERLAYS
STATUS: PASS
MAP12_05: COMPLETE ELIGIBLE
MAP12_06_CREATE_ACTIVITY_PREVIEW_AND_PLAYMODE_FIXTURES: LOCKED / DO NOT START

## 사용자용 구현 보고

승인된 schema repair addendum를 먼저 적용하여 Activity/EventOverlay authoring authority를 10 tables / 71 columns로 확장했다. 두 owner category가 각각 한 번에 통과한 뒤에만 starter 콘텐츠 구현을 재개했다.

이번 구현은 실제 gameplay, Prefab, physics, NPC AI, reward 지급, Maru rewind 실행을 추가하지 않는다. 명시적 CSV 좌표와 current TerrainCluster authority를 immutable contract/profile로 투영하고, 제거 후 static traversal/access/shell identity가 보존되는 authoring 계층만 제공한다.

### 추가한 Activity 7종

- `ACT_CRATER_RICOCHET_MINE`: 달 분화구 broken slope에 cue/trigger/device/hazard/projectile/reward/recovery/reset marker를 배치한다.
- `ACT_MILL_PESTLE_WORKSHOP`: 폐방앗간 recovery shaft에 사전 cue가 있는 장치·projectile·safe/recovery marker를 배치한다.
- `ACT_MILL_GEAR_GRID`: 폐방앗간 broken pillar에 장치·hazard·reward·reset marker를 배치한다.
- `ACT_CRATER_BOULDER_CHAIN`: 달 분화구 rock shelf recovery에 위험 연쇄와 대체 recovery marker를 배치한다.
- `ACT_DOUGH_TIME_TRIAL`: 달 반죽 bounce cup에 flow/reward용 선택 Activity marker를 배치한다.
- `ACT_MILL_ESCORT_CART`: 폐방앗간 beam overhang에 회수 가능한 Npc marker와 reward/recovery marker를 배치한다.
- `ACT_MARU_REWIND_ANOMALY`: 달 반죽 sticky rise recovery에 narrative cue와 불변 recovery marker를 배치한다.

### 추가한 EventOverlay 5종

- `EVT_METEOR_FALL`: TerrainCluster CORE marker에 `SetState / STATE_METEOR_FALL`, weight 3000, gap 4를 authoring한다.
- `EVT_WANDERING_MERCHANT`: Escort Activity Npc marker에 `SpawnNpc / NPC_WANDERING_MERCHANT`, weight 2500, gap 6을 authoring한다.
- `EVT_RARE_CREATURE`: Escort Activity Npc marker에 `SpawnNpc / NPC_RARE_CREATURE`, weight 1500, gap 8을 authoring한다.
- `EVT_MARU_INTERVENTION`: Maru Activity Device marker에 `SetState / STATE_MARU_INTERVENTION`, weight 1000, gap 10을 authoring한다.
- `EVT_EMPTY`: 명시적 shell/biome/pacing/access compatibility와 marker 0, weight/gap 0/0을 가진 유일한 Empty variant다.

## Schema Repair 증거

```text
repair TASK SHA-256:    0c8f335a17a320971b4a59af8562bc285b983722e39fd0b4ec91a9e35385048b
repair ARCHIVE SHA-256: 0c8f335a17a320971b4a59af8562bc285b983722e39fd0b4ec91a9e35385048b
original TASK SHA-256:  1399da3436d8e4ea1b3c29c0381ab45adf7908a5832e2718a3c574d915531ba3
original ARCHIVE SHA:   1399da3436d8e4ea1b3c29c0381ab45adf7908a5832e2718a3c574d915531ba3

whole registry tables / columns / FK: 29 / 189 / 59
Activity tables / columns:             7 / 51
EventOverlay tables / columns:         3 / 20
combined tables / columns:            10 / 71
Generated tables / targets:            0 / 0
```

Owner verification은 content 생성 전에 각각 정확히 한 번 실행했다.

```text
MAP09_07 job: 46c742e042d643f7996f4b1d3b8a56a6
discovered / executed / passed: 22 / 22 / 22
failed / skipped: 0 / 0

MAP09_08 job: e3d17c41850b40a1858614958167abab
discovered / executed / passed: 12 / 12 / 12
failed / skipped: 0 / 0
```

Non-Activity/Event owner digest slices는 repair 전후 동일하다.

```text
MicroPattern:  5d5423e226626de563c2dcb47b2c1aa7516ceae202f91082e1ebb70dba5b357c
TerrainCluster: e906cfa8ffb0e6b8bb3af8eeb879148deff169fe05ce0c660fa31e710ac73399
SpecialRegion: a0c5d9f97f0dc6e5281ef3d39fb69844d569656fd405af8c07c642b96eeb3b4e
```

## Authoring / Import 증거

Importer는 exact 10-file byte snapshot을 BOM/header/LF/one-final-LF/RFC4180/PK 순서로 검증한다. Activity build가 성공한 뒤 Event build를 수행하며, 어느 단계에서든 오류가 있으면 두 catalog와 aggregate digest를 모두 게시하지 않는다. filesystem output, Unity asset/SO, Generated output, RNG stream/draw는 없다.

```text
Authoring CSV / CSV.meta: 75 / 75
existing non-target CSV / CSV.meta: 65 / 65
new Activity/Event CSV / CSV.meta: 10 / 10
Generated CSV: 0

Activity entries: 7
Event entries: 5
aggregate digest: 46330eb01dd302bf80dab6eacf88dea59f107cbecc9225b2243a395c1d0dbc8b
Activity catalog digest: 3ef83fae74d935a2469ab587414d0498cb423609b171d1c7633423e297318c3a
Event catalog digest: 2d2878f62605927a7b70a405a06079b3ebad7767e3bd7db9b6b2431177ea95a0
```

### Activity Starter Matrix

| Activity | TerrainCluster | Strength | Weight | Slots |
|---|---|---:|---:|---:|
| `ACT_CRATER_BOULDER_CHAIN` | `TC_CRATER_ROCK_SHELF_RECOVERY` | Strong | 1400 | 7 |
| `ACT_CRATER_RICOCHET_MINE` | `TC_CRATER_BROKEN_SLOPE` | Strong | 1400 | 8 |
| `ACT_DOUGH_TIME_TRIAL` | `TC_DOUGH_BOUNCE_CUP` | Ordinary | 1800 | 7 |
| `ACT_MARU_REWIND_ANOMALY` | `TC_DOUGH_STICKY_RISE_RECOVERY` | Strong | 600 | 7 |
| `ACT_MILL_ESCORT_CART` | `TC_MILL_BEAM_OVERHANG` | Ordinary | 1200 | 8 |
| `ACT_MILL_GEAR_GRID` | `TC_MILL_BROKEN_PILLAR` | Ordinary | 1200 | 7 |
| `ACT_MILL_PESTLE_WORKSHOP` | `TC_MILL_ORTHOGONAL_SHAFT_RECOVERY` | Strong | 1000 | 8 |

모든 Activity는 MAP09_05 contract validation을 통과하며, explicit slot/node/cue/edge ownership, Cue→Activation→Core→Reward→Recovery→Exit 성공 경로, failure/reset 경로, safe/recovery cell, `true,true,false,false` removal flags, `OptionalNoTool`, 3x3 이상 clearance를 round-trip한다. MAP12_01 shell compiler, MAP12_02 removal-safety compiler, MAP12_03 candidate index public API도 focused test에서 확인했다.

All seven starter Activities passed the actual public `ActivityShellCompiler` (MAP12_01) and
`ActivityRemovalSafetyCompiler` (MAP12_02) chain against their physical TerrainCluster authority.
Each published cue, safe pocket, recovery witness, mandatory exit, and reward preservation proof;
residual overlays, underlying tile deltas, and RNG draws were all zero.

### EventOverlay Starter Matrix

| EventOverlay | Kind / operation | Weight | Gap | Markers |
|---|---|---:|---:|---:|
| `EVT_EMPTY` | Empty / none | 0 | 0 | 0 |
| `EVT_MARU_INTERVENTION` | State / SetState | 1000 | 10 | 1 |
| `EVT_METEOR_FALL` | State / SetState | 3000 | 4 | 1 |
| `EVT_RARE_CREATURE` | Npc / SpawnNpc | 1500 | 8 | 1 |
| `EVT_WANDERING_MERCHANT` | Npc / SpawnNpc | 2500 | 6 | 1 |

모든 Event profile은 MAP12_04 candidate index public API를 통과한다. non-empty는 exact one marker assignment를, Empty는 zero marker와 explicit compatibility만 가진다. marker coordinate/source owner/slot kind/payload/operation과 shell/activity removal evidence가 round-trip되며 non-marker mutation count는 모두 0이다.

## Focused Validation

```text
Unity compile errors: 0
final Console errors / relevant warnings: 0 / 0

MAP12_05 final job: 7a14974e51934ca8b08f8899518e0e45
discovered / executed / passed: 6 / 6 / 6
failed / skipped / inconclusive: 0 / 0 / 0

duplicate GUID groups: 0
RNG stream creation / draws during import/index: 0 / 0
```

Focused tests prove exact physical headers/encoding/inventory, exact 7/5 matrices, immutable indexes,
all seven Activities through the MAP12_01 shell and MAP12_02 removal-safety public compilers,
Activity/Event public validation, MAP12_03/MAP12_04 candidate-index compatibility, reverse
input/row enumeration, repeat and `tr-TR` digest stability, and invalid FK/duplicate ID/missing
Empty/bad graph atomic zero-publication.

```text
REGRESSION TRIGGER DETECTED: YES
Trigger owner: MAP09_07 schema authority
Reason: approved Activity/Event descriptor contract revision
Minimum verification: MAP09_07 + MAP09_08 once

PRIOR TASK TEST SELECTIONS: 2 (required owner selections only)
LEGACY 19347 SELECTIONS: 0
MAP10/MAP11/MAP12_01~04 CATEGORY SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
```

## Change Scope

```text
schema source/test files changed: 3
new Runtime C#/meta: 2 / 2
new Editor importer C#/meta: 1 / 1
new focused Editor test C#/meta: 1 / 1
new target CSV/meta: 10 / 10
Generated/Scene/Prefab/Tilemap/asmdef/Settings/Packages changes: 0
MAP12_06 started: NO
unrelated pre-existing untracked meta files preserved: 3
Status Finalize: PERFORMED (`MAP12_05` COMPLETE, Current Task `NONE`)
Git push: NOT PERFORMED
```

Status Finalize를 수행하여 `MAP12_05`만 COMPLETE로 전환하고 Current Task를 `NONE`으로 설정했다. `MAP12_06`은 LOCKED 상태로 유지했다.
