```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP12_06_CREATE_ACTIVITY_PREVIEW_AND_PLAYMODE_FIXTURES
  task_file: TASKS/MAP12_06_CREATE_ACTIVITY_PREVIEW_AND_PLAYMODE_FIXTURES.md
  requires_current_task: NONE
  requires_completed_task: MAP12_05_AUTHOR_STARTER_ACTIVITIES_AND_OVERLAYS
  requires_result:
    path: REPORTS/MAP12_05_AUTHOR_STARTER_ACTIVITIES_AND_OVERLAYS_RESULT.md
    status: PASS
    sha256: 64c203c1aaec98e2923109bcbd8233369e171c1c07087fde3823394423f8c01e
  requires_installed_task:
    path: TASKS/MAP12_05_AUTHOR_STARTER_ACTIVITIES_AND_OVERLAYS.md
    sha256: 1399da3436d8e4ea1b3c29c0381ab45adf7908a5832e2718a3c574d915531ba3
  sets_current_task: MAP12_06_CREATE_ACTIVITY_PREVIEW_AND_PLAYMODE_FIXTURES
```

# MAP12_06 — Create Activity Preview and PlayMode Fixtures

```text
TASK: MAP12_06_CREATE_ACTIVITY_PREVIEW_AND_PLAYMODE_FIXTURES
PHASE: MAP12 — ActivityStructure / EventOverlay
STATUS: CURRENT
NEXT: MAP12_07_MAP12_ACTIVITY_EXIT_TESTS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 사용자 의미와 결과 보고

MAP12_05의 starter Activity 7종과 EventOverlay 5종을 사람이 Editor에서 처음으로 눈으로 비교하고, Activity/Event 제거·중단·재진입 상태를 test-only PlayMode graybox로 검증한다.

```text
10 physical Activity/Event CSV
→ existing importer/catalog
→ MAP12_01~04 public artifacts
→ immutable preview snapshot
→ Editor Static / Active / Removed / Compare
→ PlayMode ephemeral lifecycle fixture
```

이번 Task는 gameplay를 구현하지 않는다. Preview와 test fixture가 계약대로 움직이는지만 확인한다.

Result 첫 섹션은 exact `## User-Facing Implementation Report`, 두 번째는 exact `## Responsibility and Added Functions`다. 두 번째 섹션에는 반드시 모든 추가/수정 C# 파일 이름, 파일별 입력→출력, 개별 책임을 표로 쓴다.

## 1. 책임과 비소유

| 소유 | 소유하지 않음 |
|---|---|
| read-only Activity/Event preview snapshot/window | CSV 편집·자동 보정 |
| Static/Active/Removed/Compare 시각 구분 | 실제 Activity state machine |
| Cue/Safe/Recovery/Exit/Reward 증거 표시 | PlayerController reachability |
| Event marker/Empty/removal 표시 | NPC/Reward/meteor/Maru 실행 |
| test-only marker lifecycle graybox | production Scene/Prefab/Tilemap |
| MAP12_06 EditMode/PlayMode focused tests | 전체 PlayMode/legacy regression |

파이프라인 위치:

```text
MAP12_05 starter data
→ MAP12_06 human preview + lifecycle fixture
→ MAP12_07 phase exit audit
```

## 2. Focused test 정책

정상 실행에서 허용되는 선택은 두 개뿐이다.

```text
MAP12_06 EditMode category: required
MAP12_06 PlayMode category: required
```

PlayMode는 이번 Task가 소유하는 임시 graybox fixture만 실행한다. 전체 PlayMode suite 또는 unfiltered selection은 금지한다.

```text
MAP09/MAP10/MAP11 categories: 0
MAP12_01~05 categories: 0
legacy 19347: 0
unfiltered EditMode/PlayMode: 0/0
MAP12_07: 0
```

focused test 내부에서 기존 public API를 호출하는 것은 이전 category 선택이 아니다. Task-owned 신규 파일 결함이면 해당 MAP12_06 mode만 재실행한다. 기존 authority 결함이면 owner/invariant/원인/최소 검증 범위를 보고하고 기존 파일을 수정하지 않은 채 `BLOCKED`로 STOP한다.

## 3. Read-only preflight

쓰기 전에 확인한다.

1. MAP12_05 Result `PASS`, Result/Task SHA exact, Status COMPLETE, Current Task MAP12_06
2. MAP12_05R Task/Archive exact SHA `0c8f335a17a320971b4a59af8562bc285b983722e39fd0b4ec91a9e35385048b`
3. schema `29 tables / 189 columns / 59 FK`; Activity/Event `10 / 71`
4. Authoring CSV/meta `75/75`; Activity/Event CSV/meta `10/10`; Generated CSV `0`
5. Activity/Event entries `7/5`
6. aggregate/catalog digests:

```text
aggregate: 46330eb01dd302bf80dab6eacf88dea59f107cbecc9225b2243a395c1d0dbc8b
Activity:  3ef83fae74d935a2469ab587414d0498cb423609b171d1c7633423e297318c3a
Event:     2d2878f62605927a7b70a405a06079b3ebad7767e3bd7db9b6b2431177ea95a0
```

7. all seven MAP12_01 shell + MAP12_02 safety compile evidence available
8. MAP12_03/04 candidate-index public APIs available
9. compile/Console, meta/GUID, unrelated dirty/staged paths

Preflight를 위해 과거 test category를 실행하지 않는다. baseline drift면 신규 파일 생성 전 `BLOCKED`다.

## 4. Exact file boundary

New Editor production:

```text
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Activities/ActivityEventPreviewModel.cs(.meta)
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Activities/ActivityEventPreviewWindow.cs(.meta)
```

New EditMode focused test:

```text
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Activities/ActivityEventPreviewTests.cs(.meta)
```

New PlayMode focused fixture/test:

```text
Assets/_Game/Tests/PlayMode/Map/WorldGeneration/Activities/ActivityRemovalGrayboxPlayModeTests.cs(.meta)
```

PlayMode harness는 마지막 test 파일 안의 internal test-only type으로 둔다. 별도 helper C#은 금지한다.

Existing assembly/namespace를 사용하고 asmdef/asmref를 수정하지 않는다. assembly reference가 부족하면 production code를 복제하거나 asmdef를 바꾸지 말고 exact missing reference로 `BLOCKED`한다.

기존 C#/test/CSV/meta, Authoring/Generated, Scene, Prefab, SO, Tilemap, Texture, Material, Settings, Packages 수정은 금지한다.

## 5. Preview model contract

동등한 naming을 허용하되 최소 surface는 다음 책임을 가진다.

```text
ActivityEventPreviewRequest
ActivityPreviewState: Static / Active / Removed
ActivityEventPreviewCell / Marker / RouteWitness
ActivityStatePreviewSnapshot
EventOverlayPreviewSnapshot
ActivityEventComparisonSnapshot
ActivityEventPreviewBuildError / Result
ActivityEventPreviewModel.Build
```

모델은 existing authority를 다음 순서로 호출한다.

1. MAP12_05 exact 10-file importer/catalog
2. referenced MAP11 TerrainCluster catalog/baseline variant
3. MAP12_01 Activity shell/slot compiler
4. MAP12_02 Cue/removal/safety compiler
5. MAP12_03 Activity placement profile/index evidence
6. MAP12_04 Event marker/profile/index evidence

CSV parsing, contract validation, shell compile, route/safety proof, compatibility selection을 재구현하지 않는다.

모든 snapshot/collection은 immutable, defensive-copy/read-only, canonical ordinal order, culture independent이며 stable digest를 가진다. any error는 partial snapshot/digest를 게시하지 않는다.

## 6. Static / Active / Removed semantics

각 Activity 7종에 대해 세 snapshot을 만든다.

### Static

- referenced TerrainCluster working Canvas와 Static Shell만 표시
- Entry/Exit, baseline path, protected evidence 표시
- Activity/Event marker count 0

### Active

- Static과 exact 같은 underlying cell/route/access/protection digest
- Cue/Trigger/Device/Hazard/Projectile/Npc/Reward/Recovery/Reset marker 표시
- Cue-before-Activation ordinal, SafePocket/Recovery, reward/exit preservation 표시
- selected Event가 있으면 marker overlay만 추가

### Removed

- Activity/Event marker count 0
- underlying shell/cell/Entry/Exit/route/access/protection digest가 Static과 exact 동일
- residual marker, tile delta, collider delta, RNG draw 0
- reward/exit permanent-destruction evidence 0

Comparison은 세 상태의 cell/marker/digest delta를 명시적으로 게시한다. marker 차이를 geometry 차이로 계산하지 않는다.

## 7. EventOverlay preview

Event selector는 exact 5 IDs를 제공한다.

```text
EVT_METEOR_FALL
EVT_WANDERING_MERCHANT
EVT_RARE_CREATURE
EVT_MARU_INTERVENTION
EVT_EMPTY
```

- non-empty는 marker/source owner/slot/operation/payload/weight/gap을 표시한다.
- Empty는 `Explicit Empty`, marker 0, weight/gap 0/0을 표시한다.
- Event 적용 전후 shell/route/access/protection은 동일해야 한다.
- SpecialRegion marker이면 fixed shell/slot/persistence provenance를 read-only 표시한다.
- meteor/NPC/Maru payload를 실행하거나 Prefab lookup하지 않는다.

## 8. EditorWindow contract

```text
Menu: Tools/MapDesign/Activity & Event Preview
Title: Activity & Event Preview
```

Required controls/panels:

- explicit `Reload`; first-open read-only import
- exact 7 Activity selector
- `Static / Active / Removed / Compare` state selector
- `None + exact 5 EventOverlay` selector
- overlay toggles: Shell, Entry/Exit, Protected, Cue, Mechanism, Reward, Safe/Recovery, Event
- Activity local Canvas with 12×8 chunk boundaries
- Compare mode의 Static/Active/Removed side-by-side panels
- profile/weight/strength/compatibility and Event weight/gap details
- source/compiler/catalog/snapshot digest and inline error panel
- text labels and color legend; color alone으로 의미 표현 금지

Required tokens or equivalent text:

```text
EN Entry | EX Exit | AP Protected | C Cue | T Trigger
D Device | H Hazard | P Projectile | N Npc | RW Reward
SP SafePocket | RC Recovery | RS Reset | EV Event
```

Forbidden UI actions:

```text
CSV edit/save/export/auto-fix
Generate/Apply/Commit
Scene/Prefab/Tilemap creation
production RNG/placement controls
continuous file watcher or refresh loop
static mutable domain-reload cache
```

Window error는 inline으로 표시하고 Console spam/exception loop를 만들지 않는다.

## 9. Test-only PlayMode lifecycle fixture

PlayMode test는 production preview model이나 Editor importer를 복제하지 않는다. Physical data/compile proof는 EditMode가 소유한다. PlayMode는 Runtime public value types로 구성한 최소 representative immutable snapshot을 사용하고 source Activity/Event IDs와 expected semantic counts를 golden constant로 교차 확인한다.

대표 fixture:

```text
ACT_CRATER_RICOCHET_MINE + EVT_METEOR_FALL
ACT_MILL_ESCORT_CART + EVT_WANDERING_MERCHANT
ACT_MILL_ESCORT_CART + EVT_RARE_CREATURE
ACT_MARU_REWIND_ANOMALY + EVT_MARU_INTERVENTION
ACT_DOUGH_TIME_TRIAL + EVT_EMPTY
```

각 fixture는 temporary root 아래 marker GameObject만 생성한다.

Lifecycle:

1. `Static`: shell witness objects/labels only, Activity/Event marker 0
2. `Cue`: Cue marker visible, Core markers inactive
3. `Active`: expected Activity markers and optional Event marker active
4. `Interrupted`: Core 중단 후 hazard/device/projectile/Npc/Event marker 제거
5. `Reentered`: same snapshot으로 exact-once marker 재생성; duplicate 0
6. `Removed`: 모든 removable marker 제거, shell/Entry/Exit/Safe/Recovery witness 유지
7. one frame yield 후 temporary root와 생성 object 전부 destroy

Worst-position fixture는 authored Hazard/Projectile/Npc marker 각각의 좌표에서 가장 가까운 SafePocket/Recovery witness가 존재하고, interruption/removal 뒤 해당 witness와 Exit가 남는지만 검사한다. Player collider, jump, physics reachability를 주장하지 않는다.

금지:

```text
Scene load/save
Prefab/Material/Texture/Sprite/asset creation
Tilemap/Collider/Rigidbody/Physics2D/PlayerController
gameplay camera/root mutation
DontDestroyOnLoad/static persistent cache
RNG/time-dependent layout
actual NPC/reward/meteor/Maru execution
```

## 10. Focused verification

### EditMode `MAP12_06`

1. physical 7 Activity/5 Event selector coverage
2. all seven Static/Active/Removed snapshot success
3. underlying shell/route/access/protection identity across states
4. exact marker counts and zero residual/tile/collider/RNG deltas
5. Cue-before-Activation and Safe/Recovery/Exit/Reward evidence
6. four non-empty Events and Empty semantics
7. Compare deltas marker-only
8. reverse input/repeat/`tr-TR` deterministic digest
9. invalid ID/digest/source owner/slot atomic zero publication
10. window menu/title/control/legend and forbidden action absence

### PlayMode `MAP12_06`

1. exact five representative fixture coverage
2. Static→Cue→Active→Interrupted→Reentered→Removed sequence
3. cue visibility before Core
4. interruption marker cleanup and shell witness preservation
5. re-entry exact-once recreation, duplicate 0
6. worst-position Safe/Recovery/Exit witness preservation
7. Empty creates no Event object
8. one-frame yield and complete teardown; leaked object/static state 0

## 11. Static/change gates

```text
Unity compile / Console error / warning: 0 / 0 / 0
MAP12_06 EditMode discovered = executed = passed
MAP12_06 PlayMode discovered = executed = passed
all failed / skipped / inconclusive: 0

new Editor production C#/meta: 2 / 2
new EditMode test C#/meta: 1 / 1
new PlayMode fixture/test C#/meta: 1 / 1
existing C#/test/CSV/meta changes: 0
Authoring/Generated changes: 0 / 0
Scene/Prefab/SO/Tilemap/Texture/Material changes: 0
asmdef/Settings/Packages changes: 0
duplicate GUID: 0
```

Normal regression report:

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
```

## 12. Required Result

```text
MapDesign/MCP/REPORTS/MAP12_06_CREATE_ACTIVITY_PREVIEW_AND_PLAYMODE_FIXTURES_RESULT.md
```

Header:

```text
TASK: MAP12_06_CREATE_ACTIVITY_PREVIEW_AND_PLAYMODE_FIXTURES
STATUS: PASS | BLOCKED
MAP12_06: COMPLETE ELIGIBLE | NOT COMPLETE
MAP12_07_MAP12_ACTIVITY_EXIT_TESTS: LOCKED / DO NOT START
```

Required report order:

1. `## User-Facing Implementation Report`
2. `## Responsibility and Added Functions`
   - exact added/modified script table
   - every file's input→output and sole responsibility
3. `Preview Evidence`
4. `PlayMode Lifecycle Evidence`
5. `Focused Validation`
6. `Static and Change Scope`
7. `Not Implemented`
8. `Finalize and Commit Evidence`

반드시 보고:

- Editor에서 여는 메뉴와 눈에 보이는 항목
- 게임 화면에서는 왜 아직 production 기능이 아닌지
- 7 Activity/5 Event snapshot counts와 digests
- 각 PlayMode lifecycle 단계 object counts와 teardown
- task-owned EditMode/PlayMode exact jobs/counts
- regression trigger/selection counts
- scripts/CSV/assets changed/new/deleted 전체 목록
- unrelated staged/included 0
- Status Finalize 여부
- atomic commit subject와 commit hash 또는 Result-self convention
- Git push `NOT PERFORMED`

PASS일 때만 MAP12_06을 Finalize하고 Task-owned 파일만 atomic commit한다.

```text
Commit subject: MAP12_06: add activity preview and playmode fixtures
```

MAP12_07은 시작하지 않고 STOP한다.
