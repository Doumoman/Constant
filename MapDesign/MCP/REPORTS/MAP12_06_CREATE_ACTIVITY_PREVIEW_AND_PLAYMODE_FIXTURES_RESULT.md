TASK: MAP12_06_CREATE_ACTIVITY_PREVIEW_AND_PLAYMODE_FIXTURES
STATUS: PASS
MAP12_06: COMPLETE ELIGIBLE
MAP12_07_MAP12_ACTIVITY_EXIT_TESTS: LOCKED / DO NOT START

## User-Facing Implementation Report

MAP12_05가 만든 physical Activity 7종과 EventOverlay 5종을 read-only로 불러와 `Static / Active / Removed / Compare` 상태로 비교하는 Editor preview를 추가했다. 메뉴는 `Tools/MapDesign/Activity & Event Preview`이며 실제 Unity Editor에서 같은 제목의 900x620 window가 열린 것을 확인했다.

창에서는 Activity/Event selector, 명시적 Reload, state selector, Shell·Entry/Exit·Protected·Cue·Mechanism·Reward·Safe/Recovery·Event toggle, 12x8 chunk boundary, `EN/EX/AP/C/T/D/H/P/N/RW/SP/RC/RS/EV` legend, 세 상태 병렬 비교, profile/operation/payload/source owner, removal proof와 digest를 볼 수 있다. 오류는 inline으로 표시하며 Scene, Prefab, Tilemap 또는 authoring file을 수정하지 않는다.

게임 화면에는 아직 production 기능이 나타나지 않는다. PlayMode 검증은 test assembly가 임시 root 아래 marker GameObject만 만들고 각 fixture가 끝날 때 모두 파괴한다. 실제 Activity state machine, meteor/NPC/reward/Maru 실행이나 저장되는 gameplay object는 추가하지 않았다.

## Responsibility and Added Functions

기존 script 수정은 없고 아래 4개 C# script를 추가했다.

| Added script (full project path) | Input → output | Sole responsibility / added functions |
|---|---|---|
| `Assets/_Game/Editor/MapAuthoring/WorldGeneration/Activities/ActivityEventPreviewModel.cs` | physical Terrain/MicroPattern/Activity/Event authoring bytes와 선택 ID → immutable Static/Active/Removed/Event/Compare snapshot 또는 ordered atomic error | `BuildDefault`, `Build`, compiler-chain projection, marker/route/proof/digest 생성. MAP11 renderer부터 MAP12_01 shell, MAP12_02 removal safety, MAP12_03 activity index, MAP12_04 event index까지 public authority를 재사용하며 filesystem output은 만들지 않는다. |
| `Assets/_Game/Editor/MapAuthoring/WorldGeneration/Activities/ActivityEventPreviewWindow.cs` | preview model result와 UI 선택/toggle → read-only Editor canvas/detail panel | `Open`, `Reload`, `TrySelectViewMode`, `OnGUI`로 메뉴/selector/legend/12x8 boundary/compare/inline error를 표시한다. Scene·asset mutation과 watcher/cache는 없다. |
| `Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Activities/ActivityEventPreviewTests.cs` | physical CSV catalog와 public preview API/window → 7 Activity/5 Event snapshot, 결정성, 원자 실패, UI 계약 증거 | `PhysicalSelectorsCoverExactSevenActivitiesAndFiveEvents`, `AllSevenPublishStaticActiveRemovedIdentityAndRemovalProofs`, `FourNonEmptyEventsAndExplicitEmptyPublishExactMarkerSemantics`, `ReverseInputRepeatAndTurkishCultureKeepStableImmutableSnapshots`, `InvalidIdDigestSourceOwnerAndSlotPublishNothing`, `WindowPublishesMenuTitleControlsLegendAndNoMutationActions`. |
| `Assets/_Game/Tests/PlayMode/Map/WorldGeneration/Activities/ActivityRemovalGrayboxPlayModeTests.cs` | Runtime public ID/coordinate golden snapshots → ephemeral marker lifecycle와 teardown 증거 | `RepresentativeDefinitions_AreExactImmutableGoldenValues`, `ExactFiveFixtures_RunLifecycleAndTearDownWithoutLeaks` 및 같은 파일 내부의 internal `ActivityRemovalGrayboxHarness`. Static/Cue/Active/Interrupted/Reentered/Removed, exact-once, witness 보존, Empty, leak 0만 검증한다. |

각 script의 matching `.meta`도 신규 생성되었고 기존 C#/test script는 수정하지 않았다.

## Preview Evidence

Physical selector와 aggregate authority:

```text
Activity selector entries: 7
Event selector entries: 5
aggregate digest: 46330eb01dd302bf80dab6eacf88dea59f107cbecc9225b2243a395c1d0dbc8b
Activity catalog digest: 3ef83fae74d935a2469ab587414d0498cb423609b171d1c7633423e297318c3a
Event catalog digest: 2d2878f62605927a7b70a405a06079b3ebad7767e3bd7db9b6b2431177ea95a0
```

각 행의 marker 수는 `Static / Active / Removed` 순서다. Event를 선택하지 않은 Activity snapshot이며 세 상태의 underlying shell/route/access/protection identity는 동일하고 cell/collider/RNG delta는 모두 0이다.

| Activity | Cells | Markers (S/A/R) | Preview digest |
|---|---:|---:|---|
| `ACT_CRATER_BOULDER_CHAIN` | 480 | 0 / 8 / 0 | `27ba85575af7b16035046c1b36f5e705820ed6b20bcd04a7ea2f490ea9e1ad4c` |
| `ACT_CRATER_RICOCHET_MINE` | 384 | 0 / 9 / 0 | `13af1ba931fa7a7954d4c46971d11d86eff6183622e6014dcf038ce8c21a0066` |
| `ACT_DOUGH_TIME_TRIAL` | 288 | 0 / 8 / 0 | `c4a543d9b6f5cb49c97417304d626d9ca5cd96eeaf56e0ca88616185846d09d4` |
| `ACT_MARU_REWIND_ANOMALY` | 480 | 0 / 8 / 0 | `57cde8813b9be188f42f4a1a60137258b27c56269c2614277869f42f86d8cb57` |
| `ACT_MILL_ESCORT_CART` | 288 | 0 / 9 / 0 | `d7683c4fd2f783ffafa910323bde3372f2c2669abac376b0339fc9edf96b8d4f` |
| `ACT_MILL_GEAR_GRID` | 384 | 0 / 8 / 0 | `3bae7157be7e324a5e96e5aaa2b4b14f73fa3fcf18c3be813d87e2118381f7ce` |
| `ACT_MILL_PESTLE_WORKSHOP` | 480 | 0 / 9 / 0 | `83022c006ef98513022a15d73c652f4d5f5d88092d7a37a72c1a873887b9beac` |

Event snapshot은 non-empty 4종이 exact one marker를, `EVT_EMPTY`가 explicit zero marker/weight/gap을 게시한다.

| Event / paired Activity | Markers | Event snapshot digest |
|---|---:|---|
| `EVT_METEOR_FALL` / `ACT_CRATER_RICOCHET_MINE` | 1 | `771a4606ad518bd54cc1fc95c3e36a4cd7adce3871a0430041f75344014ad5c8` |
| `EVT_WANDERING_MERCHANT` / `ACT_MILL_ESCORT_CART` | 1 | `045ef737b09ad0d46edffff6bb647e81b6e892429f40e7f5f464e1f1dac18f9b` |
| `EVT_RARE_CREATURE` / `ACT_MILL_ESCORT_CART` | 1 | `e75d93e39829dc6edc29e7d14d56cfaee21d8f845ef62b20fe841e4334c1ac13` |
| `EVT_MARU_INTERVENTION` / `ACT_MARU_REWIND_ANOMALY` | 1 | `7866b8615e30ba2ad467ba73b9c207dad8aa8204c8a166d804b46fa10216ffeb` |
| `EVT_EMPTY` / `ACT_DOUGH_TIME_TRIAL` | 0 | `2f88fec980ca70ba8739e096700ebde0dd41019b65be496a3d14e266dac2c65a` |

Reverse input, repeat build와 `tr-TR` culture에서 digest가 동일했고 published collections는 immutable이었다. invalid ID/digest/source owner/slot은 snapshot과 digest를 게시하지 않고 ordered error만 반환했다.

## PlayMode Lifecycle Evidence

수치는 각 단계의 `shell witness / active Activity marker / active Event marker`다. 모든 fixture에서 duplicate marker는 0이었다.

| Fixture | Static | Cue | Active | Interrupted | Reentered | Removed | Teardown |
|---|---:|---:|---:|---:|---:|---:|---|
| `ACT_CRATER_RICOCHET_MINE + EVT_METEOR_FALL` | 4/0/0 | 4/1/0 | 4/8/1 | 4/5/0 | 4/8/1 | 4/0/0 | one frame, leaked root/object 0 |
| `ACT_MILL_ESCORT_CART + EVT_WANDERING_MERCHANT` | 4/0/0 | 4/1/0 | 4/8/1 | 4/5/0 | 4/8/1 | 4/0/0 | one frame, leaked root/object 0 |
| `ACT_MILL_ESCORT_CART + EVT_RARE_CREATURE` | 4/0/0 | 4/1/0 | 4/8/1 | 4/5/0 | 4/8/1 | 4/0/0 | one frame, leaked root/object 0 |
| `ACT_MARU_REWIND_ANOMALY + EVT_MARU_INTERVENTION` | 4/0/0 | 4/1/0 | 4/7/1 | 4/5/0 | 4/7/1 | 4/0/0 | one frame, leaked root/object 0 |
| `ACT_DOUGH_TIME_TRIAL + EVT_EMPTY` | 4/0/0 | 4/1/0 | 4/7/0 | 4/5/0 | 4/7/0 | 4/0/0 | one frame, leaked root/object 0 |

`Interrupted`와 `Removed` 뒤에도 Entry/Exit/SafePocket/Recovery witness 4개가 남았다. authored Hazard/Projectile/Npc golden coordinate마다 Manhattan-distance 기준 가장 가까운 SafePocket/Recovery witness와 Exit가 존재함을 검증했으며 physics reachability를 주장하지 않는다. `EVT_EMPTY`는 모든 단계에서 Event object 0개다.

## Focused Validation

최종 허용 selection만 PASS 판정에 사용했다.

```text
Unity 6000.3.8f1 compile errors: 0
final Console errors / warnings: 0 / 0

MAP12_06 EditMode final job: c9120c0cd0b04a5a845f344559e0c69d
discovered / executed / passed: 6 / 6 / 6
failed / skipped / inconclusive: 0 / 0 / 0

MAP12_06 PlayMode final job: 90941728b38c47d88f69750cc7c9cac4
discovered / executed / passed: 2 / 2 / 2
failed / skipped / inconclusive: 0 / 0 / 0
```

EditMode의 첫 task-owned attempt `3d00a0fe3f61407aa79a4f15cf448361`는 6개를 실행해 5 PASS / 1 FAIL이었다. 실패 원인은 실패-result digest property가 null인 상태에서 empty assertion을 적용한 MAP12_06 신규 API/test 계약 결함이었고, digest property를 non-null empty로 원자화한 뒤 같은 category의 위 최종 job에서 6/6 PASS했다. 다른 category는 이 과정에서도 실행하지 않았다.

```text
REGRESSION TRIGGER DETECTED: NO
MAP12_06 EDITMODE CATEGORY SELECTIONS: 2 (one corrective rerun)
MAP12_06 PLAYMODE CATEGORY SELECTIONS: 1
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
UNFILTERED EDITMODE / PLAYMODE SELECTIONS: 0 / 0
FULL PLAYMODE SELECTIONS: 0
```

## Static and Change Scope

```text
input TASK / installed TASK / ARCHIVE SHA-256:
96c93459690878d35e7f1175fdebd3d2ebad60860918edc54d1f007533efc52f

new Editor production C#/meta: 2 / 2
new EditMode test C#/meta: 1 / 1
new PlayMode fixture/test C#/meta: 1 / 1
new Activities folder meta: 3
existing C#/test/CSV/meta changes: 0
Authoring/Generated changes: 0 / 0
Scene/Prefab/SO/Tilemap/Texture/Material changes: 0
asmdef/Settings/Packages changes: 0
duplicate GUID groups across 4000 meta files: 0
unrelated staged/included: 0 / 0
```

Task-owned new asset inventory:

```text
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Activities.meta
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Activities/ActivityEventPreviewModel.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Activities/ActivityEventPreviewModel.cs.meta
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Activities/ActivityEventPreviewWindow.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Activities/ActivityEventPreviewWindow.cs.meta
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Activities.meta
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Activities/ActivityEventPreviewTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Activities/ActivityEventPreviewTests.cs.meta
Assets/_Game/Tests/PlayMode/Map/WorldGeneration/Activities.meta
Assets/_Game/Tests/PlayMode/Map/WorldGeneration/Activities/ActivityRemovalGrayboxPlayModeTests.cs
Assets/_Game/Tests/PlayMode/Map/WorldGeneration/Activities/ActivityRemovalGrayboxPlayModeTests.cs.meta
```

Task-owned documentation/state inventory:

```text
NEW MapDesign/MCP/TASKS/MAP12_06_CREATE_ACTIVITY_PREVIEW_AND_PLAYMODE_FIXTURES.md
NEW MapDesign/MCP_ARCHIVE/MAP12_06_CREATE_ACTIVITY_PREVIEW_AND_PLAYMODE_FIXTURES.md
NEW MapDesign/MCP/REPORTS/MAP12_06_CREATE_ACTIVITY_PREVIEW_AND_PLAYMODE_FIXTURES_RESULT.md
MODIFIED MapDesign/MCP/06_IMPLEMENTATION_STATUS.md (open/finalize fields and Last Completed/Result only)
```

Modified existing scripts: none. New/modified/deleted CSV: none/none/none. New production or test asset other than the listed C#/matching meta/folder meta: none. Deleted tracked files: none. The three pre-existing unrelated untracked `TerrainClusters.meta` files remain untouched and are excluded from staging/commit.

## Not Implemented

- 실제 Activity state machine, interruption/re-entry gameplay 또는 runtime preview UI
- PlayerController/collider/jump/physics reachability
- meteor, merchant/rare-creature NPC, reward, Maru rewind payload 실행
- production Scene/Prefab/ScriptableObject/Tilemap/Texture/Material, gameplay camera/root
- authoring/Generated data 변경, RNG/time-dependent layout, persistent/static runtime cache
- MAP12_07 및 이후 task

## Finalize and Commit Evidence

```text
Result: PASS
Status Finalize: PERFORMED as the ordered next protocol phase before the enclosing commit
Final state: MAP12_06 COMPLETE / Current Task NONE
Next state: MAP12_07 LOCKED / DO NOT START
Atomic commit subject: MAP12_06: add activity preview and playmode fixtures
Commit hash: enclosing atomic Git commit (Result-self convention; verify with git show --format=%H HEAD)
Unrelated staged/included: 0 / 0
Git push: NOT PERFORMED
```

이 Result 자체가 atomic commit에 포함되므로 자기 commit hash를 파일 안에 직접 기록할 수 없다. commit 후 `git show --format=%H HEAD`와 제목을 검증하며 Result는 다시 수정하지 않는다.
