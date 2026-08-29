```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP12_05_AUTHOR_STARTER_ACTIVITIES_AND_OVERLAYS
  task_file: TASKS/MAP12_05_AUTHOR_STARTER_ACTIVITIES_AND_OVERLAYS.md
  requires_current_task: NONE
  requires_completed_task: MAP12_04_IMPLEMENT_EVENT_OVERLAY_ASSIGNMENT_RULES
  requires_result:
    path: REPORTS/MAP12_04_IMPLEMENT_EVENT_OVERLAY_ASSIGNMENT_RULES_RESULT.md
    status: PASS
    sha256: 2bc6a25b274063ea2e6c7d49c6c1490a6de3579b41e697d0c15cff508a56aa9d
  requires_installed_task:
    path: TASKS/MAP12_04_IMPLEMENT_EVENT_OVERLAY_ASSIGNMENT_RULES.md
    sha256: e802edf042683f09e1c5f6ee5d3ad68c688a03f56ed095bc288a69254457916c
  sets_current_task: MAP12_05_AUTHOR_STARTER_ACTIVITIES_AND_OVERLAYS
```

# MAP12_05 — Author Starter Activities and Event Overlays

```text
TASK: MAP12_05_AUTHOR_STARTER_ACTIVITIES_AND_OVERLAYS
PHASE: MAP12 — ActivityStructure / EventOverlay
STATUS: CURRENT
NEXT: MAP12_06_CREATE_ACTIVITY_PREVIEW_AND_PLAYMODE_FIXTURES
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 목표와 사용자 보고

MAP12_01~04가 만든 Activity/Event 계약·컴파일·안전·선택 계층에 처음으로 실제 starter Authoring 데이터를 연결한다.

```text
Activity CSV 3개 + EventOverlay CSV 2개
→ exact importer
→ Activity 7종 / EventOverlay 4종 + Empty 1종 catalog
→ existing contract/shell/removal/candidate/planner API 소비 가능성 증명
```

이번 Task는 데이터와 importer만 만든다. 절구·기어·수레·달돌·운석·NPC·희귀 생물·마루의 실제 Prefab, 상태 머신, 물리, 보상 지급은 만들지 않는다.

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`다. 파일별 책임, 새 기능, 파이프라인 위치, 미구현, Editor/게임 가시성을 구체적으로 보고한다.

## 1. 책임과 비책임

| 소유 | 소유하지 않음 |
|---|---|
| approved Activity/EventOverlay 5개 CSV physical boundary | CSV schema/column/table 변경 |
| starter Activity 7종 계약·placement profile | 실제 사건 동작/Prefab/physics |
| EventOverlay 4종 + compatible Empty 1종 | 실제 NPC/Reward/State 실행 |
| exact import, immutable catalog, canonical digest | Activity/Event 선택 알고리즘 재구현 |
| 기존 MAP12_01~04 API 소비 가능성 | Scene/Tilemap/Generated/Runtime placement |

설계표준 PDF는 이름과 플레이 의도 참고 자료다. PDF의 색 선, 좌표, 고체 셀, `AS_*` ID는 Authoring authority가 아니다.

```text
PDF AS_* label → canonical ACT_* ID로 변환
PDF 도면 좌표     → 사용 금지
현재 MAP11 starter TerrainCluster/Spine/Canvas → 좌표·shell authority
```

## 2. Focused-Only 정책

정상 실행 선택은 category `MAP12_05` EditMode 하나뿐이다.

```text
MAP12_05 focused: required
MAP09/MAP10/MAP11/MAP12_01~04 selections: 0
legacy 19347: 0
PlayMode/unfiltered: 0/0
```

focused test 안에서 기존 public importer/compiler/planner API를 호출하는 것은 과거 category 재실행이 아니다.

compile/import/focused 실패가 Task-owned 파일 안에 있으면 그 파일만 수정하고 `MAP12_05`만 재실행한다. 기존 authority 결함이면 owner/invariant/원인/최소 검증 범위를 기록하고 기존 파일을 수정하지 않은 채 `BLOCKED`로 STOP한다.

## 3. Preflight와 representability gate

쓰기 전 다음을 읽기 전용으로 확인한다.

1. MAP12_04 Result `PASS`, Result/Task SHA exact, MAP12_05만 CURRENT
2. MAP09_07 이후 현재 V2 registry의 exact table/header/PK/FK/token authority
3. Activity 3개, EventOverlay 2개 descriptor가 §6~§8 의미를 손실 없이 표현 가능한지
4. MAP11 starter TerrainCluster 16종/32 SpineVariant와 current catalog digest
5. MAP12_01 shell compiler, MAP12_02 removal proof, MAP12_03 Activity profile/index/planner
6. MAP12_04 Event profile/index/planner와 `RNG_POPULATION` binding
7. target 5개 CSV가 없거나 exact header-only인지, matching folder/meta/GUID 상태
8. compile/Console, dirty/staged paths, existing Authoring/Generated inventory

### 3.1 필수 representability

기존 5개 descriptor만으로 다음을 round-trip해야 한다.

```text
Activity:
  ACT ID, referenced cluster/spine/static shell, slots/cues,
  Mechanism/Progression graph, removal safety,
  compatible biome/pacing/access/chunk/clearance,
  weight, Ordinary/Strong

EventOverlay:
  EVT ID/kind, marker operation/payload,
  referenced marker owner, compatible biome/pacing/access/activity,
  weight, MinimumProgressionGap, explicit Empty
```

누락 의미를 ID 파싱, filename, display name, C# hard-coded dictionary, default fallback, JSON blob, delimiter alias로 보충하지 않는다. 기존 schema가 부족하거나 non-header 사용자 데이터가 이미 있으면 아무 CSV/C#도 만들지 말고 누락 descriptor/column/semantic owner를 보고하여 `STATUS: BLOCKED`로 종료한다.

## 4. Exact 변경 범위

### 4.1 신규 Runtime authoring catalog

```text
Assets/_Game/Map/Runtime/WorldGeneration/Activities/Authoring/ActivityAuthoringCatalog.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/EventOverlays/Authoring/EventOverlayAuthoringCatalog.cs(.meta)
```

각 파일은 해당 parsed row model, validation result/error, immutable catalog/index, canonical digest를 한 책임 안에서 제공한다. filesystem, `UnityEditor`, RNG, Prefab에 의존하지 않는다.

### 4.2 신규 Editor importer와 focused test

```text
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Import/ActivityEventCsvImporterV2.cs(.meta)
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Import/ActivityEventStarterContentTests.cs(.meta)
```

별도 helper C#은 금지한다. importer는 기존 RFC4180/header/field/FK authorities를 재사용한다.

### 4.3 신규 physical Authoring CSV

```text
Assets/_Game/Map/Data/WorldGeneration/Authoring/Activity/activity_catalog_v2.csv(.meta)
Assets/_Game/Map/Data/WorldGeneration/Authoring/Activity/activity_cues_v2.csv(.meta)
Assets/_Game/Map/Data/WorldGeneration/Authoring/Activity/activity_graph_edges_v2.csv(.meta)
Assets/_Game/Map/Data/WorldGeneration/Authoring/EventOverlay/event_overlay_catalog_v2.csv(.meta)
Assets/_Game/Map/Data/WorldGeneration/Authoring/EventOverlay/event_overlay_markers_v2.csv(.meta)
```

이미 exact header-only 파일이 있으면 내용만 채우고 기존 meta/GUID는 보존한다. 파일이 없으면 registry exact header/order로 생성하고 Unity가 meta를 만들게 한다. UTF-8 BOM, RFC4180, LF, one final LF, canonical PK row order를 사용한다.

기존 C#/test/CSV/meta, V2 registry/schema/pass/RNG, TerrainCluster/MicroPattern/SpecialRegion, asmdef, Scene, Prefab, Tilemap, Generated, Settings, Packages 수정은 금지한다.

## 5. Authoring build contract

import는 다섯 파일을 하나의 atomic snapshot으로 처리한다.

```text
physical bytes
→ exact path/header/encoding/PK/FK validation
→ typed Activity/Event rows
→ MAP09_05 contract validator
→ MAP12_03/04 placement profile projection
→ immutable catalogs + aggregate digest
```

- any file/error/FK/contract/profile failure면 Activity/Event catalogs와 aggregate digest 전부 0/null이다.
- collection은 defensive-copy/read-only/canonical ordinal order다.
- digest는 five file identities, every semantic row, referenced authority digest를 포함한다.
- digest는 row enumeration, locale, time, object identity, absolute path, Unity lifecycle를 제외한다.
- importer는 Generated/asset/SO를 쓰거나 RNG stream을 만들지 않는다.

## 6. Exact starter Activity 7종

canonical ID는 exact 7개이며 추가/누락/duplicate를 금지한다.

| Activity ID | Starter static shell | Biome | Pacing compatibility | Strength | Weight | 플레이 책임 |
|---|---|---|---|---|---:|---|
| `ACT_CRATER_RICOCHET_MINE` | `TC_CRATER_BROKEN_SLOPE` baseline | MoonCrater | Activity, Reward | Strong | 1400 | 발사체 도탄 marker와 기본/고점 보상 |
| `ACT_MILL_PESTLE_WORKSHOP` | `TC_MILL_ORTHOGONAL_SHAFT_RECOVERY` baseline | AbandonedMill | Risk, Machinery, Activity | Strong | 1000 | 전조가 있는 낙하 장치와 안전 pocket |
| `ACT_MILL_GEAR_GRID` | `TC_MILL_BROKEN_PILLAR` baseline | AbandonedMill | Machinery, Activity | Ordinary | 1200 | 장치·reset·보상 marker |
| `ACT_CRATER_BOULDER_CHAIN` | `TC_CRATER_ROCK_SHELF_RECOVERY` baseline | MoonCrater | Risk, Activity | Strong | 1400 | 달돌 연쇄·압력판·대체 해법 marker |
| `ACT_DOUGH_TIME_TRIAL` | `TC_DOUGH_BOUNCE_CUP` baseline | MoonDough | Flow, Activity, Reward | Ordinary | 1800 | 선택적 시간 보상과 기본 통과 |
| `ACT_MILL_ESCORT_CART` | `TC_MILL_BEAM_OVERHANG` baseline | AbandonedMill | Machinery, Activity, Reward | Ordinary | 1200 | 회수 가능한 수레·도착 보상 marker |
| `ACT_MARU_REWIND_ANOMALY` | `TC_DOUGH_STICKY_RISE_RECOVERY` baseline | MoonDough | Risk, Activity, Narrative | Strong | 600 | 되감기 전조·불변 하단 복구 marker |

`baseline`은 current catalog가 게시한 exact baseline SpineVariant ID를 CSV에 명시한다. Activity ID에서 cluster/spine을 추론하지 않는다.

공통 계약:

- Access compatibility는 `OptionalNoTool`을 반드시 포함한다. 필요한 경우 `OptionalEnvironment`를 추가할 수 있으나 mandatory/tool/progression gate로 승격하지 않는다.
- active chunk bounds는 referenced starter shell을 포함하며 2..5 밖을 요구하지 않는다.
- open clearance는 player-independent starter graybox 값으로 width `>=3`, height `>=3`이며 실제 collider 승인은 MAP12_06 소유다.
- 각 Activity는 Cue/Trigger/Device/Hazard/Reward/Recovery/Reset marker를 최소 하나씩 가진다.
- Ricochet/Pestle에는 Projectile marker를 추가한다. Escort에는 Npc marker를 추가한다.
- Cue는 Activation보다 먼저 관측되고 SafePocket/Recovery는 non-empty다.
- Progression은 `Cue→Activation→Core→Reward→Recovery→Exit` 성공 순서를 가진다.
- Failure는 Recovery/Reset으로만, Reset은 Activation/Core로만 복귀한다.
- 제거 전후 static shell, Entry→Exit, AccessClass, traversal digest가 동일하다.
- 실제 device timing, physics, reward amount, NPC AI, Maru rewind 실행값은 저장하지 않는다.

좌표는 PDF에서 복사하지 않는다. current compiled Local Canvas의 active coordinate, role anchor, protected provenance를 사용해 한 번 결정한 explicit CSV 좌표로 기록한다. importer/runtime에서 자동 재배치하지 않는다.

## 7. Exact starter EventOverlay 4종 + Empty

| Event ID | Kind / operation | Payload token | Weight | MinimumProgressionGap | Compatibility intent |
|---|---|---|---:|---:|---|
| `EVT_METEOR_FALL` | State / SetState | `STATE_METEOR_FALL` | 3000 | 4 | MoonCrater의 existing Event marker |
| `EVT_WANDERING_MERCHANT` | Npc / SpawnNpc | `NPC_WANDERING_MERCHANT` | 2500 | 6 | Quiet/Recovery 또는 Special Npc slot |
| `EVT_RARE_CREATURE` | Npc / SpawnNpc | `NPC_RARE_CREATURE` | 1500 | 8 | non-Safe TerrainCluster/Activity Npc marker |
| `EVT_MARU_INTERVENTION` | State / SetState | `STATE_MARU_INTERVENTION` | 1000 | 10 | Activity/Event slot; static shell 불변 |
| `EVT_EMPTY` | Empty / no assignment | empty | 0 | 0 | 모든 valid opportunity의 exact one Empty |

- non-empty는 exact one starter marker assignment만 가진다.
- Empty는 assignment 0이며 weighted draw를 소비하지 않는다.
- Event는 existing marker만 참조하고 collision/solid/background/route/access/pacing/envelope를 변경하지 않는다.
- SpecialRegion에서는 Npc→Npc slot, State→Event slot만 허용한다.
- meteor는 marker state일 뿐 실제 낙하물·충돌·파괴를 생성하지 않는다.
- merchant/rare creature는 NPC payload token일 뿐 Prefab 존재나 spawn 성공을 주장하지 않는다.
- Maru intervention은 `ACT_MARU_REWIND_ANOMALY`와 별도 overlay이며 Activity graph를 소유하지 않는다.

## 8. Focused content validation

category `MAP12_05`에서 최소 다음을 검증한다.

1. five physical files exact path/BOM/header/LF/final-LF/RFC4180
2. exact Activity IDs 7, Event IDs 5, no extra/missing/duplicate
3. every FK/PK/token/profile field round-trip and immutable catalog
4. all seven through MAP09_05 Activity validation PASS
5. all seven through MAP12_01 shell compilation PASS
6. all seven through MAP12_02 removal/cue/safety proof PASS
7. all seven produce at least one MAP12_03 compatible focused opportunity
8. exact Strength `Strong 4 / Ordinary 3` and weights from §6
9. four non-empty Event profiles + exact one Empty through MAP12_04 index PASS
10. Special Npc/Event allowed matrix and forbidden fixed/port/other slot rejection
11. Event weights/gaps exact, Empty assignment/draw 0
12. reverse row/input order, repeat, `tr-TR` canonical digest equality
13. one invalid FK/duplicate ID/missing Empty/bad graph fixture each atomic zero publication
14. source/compiled coordinates and shell/protection/traversal before/after equality
15. filesystem write outside five CSVs, RNG during import, Generated/Prefab/Scene/Tilemap mutation 0

테스트 기대값은 production CSV를 다시 읽어 자기 자신을 정답으로 삼지 않는다. §6~§7 exact IDs/weights/kinds/gaps와 current authority digests를 test-owned golden constants로 독립 검증한다.

## 9. 정적 gate와 변경 감사

```text
Unity compile / Console error / warning: 0 / 0 / 0
MAP12_05 discovered = executed = passed
failed / skipped / inconclusive: 0 / 0 / 0

new Runtime C#/meta: 2 / 2
new Editor importer C#/meta: 1 / 1
new focused Editor test C#/meta: 1 / 1
new or populated target CSV/meta: 5 / 5
existing non-target C#/test/CSV/meta changes: 0
registry/schema/RNG/pass changes: 0
Generated/Scene/Prefab/Tilemap/asmdef/Settings/Packages changes: 0
duplicate GUID: 0
```

정상 보고:

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY TEST SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
```

trigger가 발생했을 때만 owner/reason/minimum selection을 기록한다. 자동으로 legacy/전체 회귀를 실행하지 않는다.

## 10. Required Result

```text
MapDesign/MCP/REPORTS/MAP12_05_AUTHOR_STARTER_ACTIVITIES_AND_OVERLAYS_RESULT.md
```

상단:

```text
TASK: MAP12_05_AUTHOR_STARTER_ACTIVITIES_AND_OVERLAYS
STATUS: PASS | FAIL | BLOCKED
MAP12_05: COMPLETE ELIGIBLE | NOT COMPLETE
MAP12_06_CREATE_ACTIVITY_PREVIEW_AND_PLAYMODE_FIXTURES: LOCKED / DO NOT START
```

필수 보고 순서:

1. `## User-Facing Implementation Report`
   - 이번에 실제 추가한 7 Activity와 5 Event variant가 무엇인지 한국어로 설명
   - 이것으로 가능해진 것과 아직 화면에서 동작하지 않는 것을 분리
2. `## Responsibility and Added Functions`
   - 추가/수정 스크립트·CSV 전체 목록
   - 각 파일의 입력→출력·개별 책임
   - 파이프라인 위치와 다음 MAP12_06이 받을 데이터
   - Editor/Test Runner/게임 화면 가시성
3. `Representability and Import Evidence`
   - registry tables/columns/digest, five headers, row counts, catalog digests
4. `Activity Starter Matrix`
   - 7 IDs, cluster/spine, biome/pacing/access, strength/weight
   - slot/cue/mechanism/progression/removal proof counts
5. `EventOverlay Starter Matrix`
   - 5 IDs, kind/operation/payload/weight/gap/marker owner
6. `Focused Validation`
   - exact selection/counts와 regression trigger
7. `Static and Change Scope`
   - new/modified/deleted, GUID, forbidden paths, staged/push evidence
8. `Not Implemented`
   - Prefab/physics/state/NPC/reward/preview/PlayMode/actual placement 명시

PASS여도 Result 작성, Status Finalize, atomic commit 후 STOP한다. MAP12_06은 시작하지 않는다.

## 11. Git 경계

```text
preflight unrelated staged: 0
final unrelated staged/included: 0
commit subject: MAP12_05: author starter activities and overlays
Git push: NOT PERFORMED
```

Task-owned 신규/대상 파일과 protocol Task/Archive/Result/Status만 stage/commit한다. 기존 unrelated untracked/modified 파일은 위치가 바뀌어 있어도 보존하고 stage하지 않는다.

## STOP

```text
MAP12_05 Result 작성
→ PASS일 때만 Finalize + atomic commit
→ MAP12_06 LOCKED 유지
→ STOP
```
