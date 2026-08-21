# MAP04_11 — MAP04 Batch and Exit Tests (Repair v1.4 Scene Folder Resume)

```yaml
status_control:
  task_key: MAP04_11_MAP04_BATCH_AND_EXIT_TESTS
  result_file: REPORTS/MAP04_11_MAP04_BATCH_AND_EXIT_TESTS_RESULT.md
  repair_contract: VARIABLE_COUNT_OVERLAY_AND_MANUAL_PROGRESS_SCENE_FOLDER_RESUME
```

## Goals

1. valid variable-count MAP04 publications을 거부하는 overlay의 fixed `17=4/10/3` consumer assumption을 제거한다.
2. 현재 구현된 MAP00~04 결과를 사용자가 직접 확인할 Editor-only manual test scene을 만든다.
3. 1,000-world exit gate를 실제 재실행해 `Completed + PassSiteHandoffRequired = 1000`, `Invalid = 0`을 증명한다.

v1.3에서 1번 source repair와 harness compile은 완료됐다. 이 resume은 존재하지 않았던 `Assets/_Game/Scenes` directory와 folder meta를 명시적으로 허용하고 남은 scene/tests/exit gate를 끝낸다.

MAP05 route, microchunk, tile bake, population, save/streaming은 만들거나 표시하지 않는다.

## Preconditions / Current Evidence

control → Master/Status → 이 Task → current FAIL Result → MAP04_10 Task/Result → exact overlay APIs/tests → current repaired cleanup/exit sources 순서로 읽는다.

```text
Current Task SHA:
7b034f722b7f445041dba9d791b4eec4731a34bce4526683a84e607d6eaa098c

Current BLOCKED Result SHA:
af4ac6e406fd21c0c36f82e91cadd3258b4861615b4817b09d2b0e80f3c0f01e

Current exit test SHA:
cb629b481264e4ca9634e7a828fb394c00bab748f7bdf29db6a12256b216ef5a

Current PatchCleanup SHA / tests SHA:
5d6e5cc162671a532fc6fb5a7cd15565f6bf979c2035dcdb7a427e8abed8ebc4
b710b18cbc7f0b81607e94f9c7ec6cb8d6055d2493fdc1c7ee756c66fd6b250e

Batch: 5 Completed / 951 Handoff / 44 Invalid
Cleanup Invalid: 0
Remaining terminal: BiomePatchOverlaySnapshot / ExactProjectionRejected = 44
Valid observed patch inventory: 15..19, variable Satellite/Intrusion
Repaired overlay snapshot SHA: 981e2a4ea6d3b81fb2d8976f006577f6f78f6eea00d82669472b93eefe5be510
Compiled harness SHA: 1fa6957324e8fa75bb76729c0f165ea94684179ac397b8e4da74f116efd056a7
Existing overlay tests: 150/150 PASS
Assets meta: 3149
Scenes directory/scene: absent/absent
```

Current Task/Result가 다르면 `BLOCKED`. MAP05_01 이후 Task body는 읽거나 시작하지 않는다.

## Read Allowlist

- MAP04_10 exact seven overlay production/editor/test C#와 matching meta
- current `Map04ExitTests.cs`, `PatchCleanup.cs/tests`, 관련 validation/export publications
- biome patch snapshot/patch/ownership/seed/binding value types
- MAP02 topology and MAP03 site reservation overlay public components/snapshots
- current exit fixture가 사용하는 exact public MAP02~04 pipeline APIs와 typed fixture facts
- `Assets/_Game` direct-child path-only inventory, missing `Scenes` confirmation, build settings scene list, relevant asmdef

unrelated scene/prefab body, installed CSV body, other production/tests, Legacy/Stage/P6/P11, future Task는 금지한다.

## Write Allowlist

required overlay repair:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/BiomePatchOverlaySnapshot.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/BiomePatchOverlayTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Preview/BiomePatchOverlaySceneDrawerTests.cs
```

fixed-count presentation assumption이 실제 확인된 경우에만:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/BiomePatchOverlayGui.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Preview/BiomePatchOverlaySceneDrawer.cs
```

new manual scene artifacts:

```text
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Preview/MapGenerationProgressSceneHarness.cs
Assets/_Game/Scenes/MapGenerationProgressTest.unity
Assets/_Game/Scenes.meta
Assets/_Game/Scenes/MapGenerationProgressTest.unity.meta
```

current Result와 Phase C Status finalize만 추가 허용한다. harness matching meta는 blocked attempt에서 이미 존재하며 보존한다. `Map04ExitTests`, repaired cleanup/producer files, public models/API shape, asmdef, CSV, other Scene/Prefab, Packages, ProjectSettings는 수정하지 않는다.

## Resume-Specific Folder Contract

apply 후 execution 시작 시 아래 baseline을 exact 확인한다.

```text
Assets/_Game/Scenes directory absent
Assets/_Game/Scenes.meta absent
MapGenerationProgressTest.unity/meta absent
MapGenerationProgressSceneHarness.cs/meta present and compiling
Assets meta 3149
```

Unity `AssetDatabase.CreateFolder("Assets/_Game", "Scenes")`를 exact once 사용한다. shell/file API로 directory나 `.meta`를 쓰지 않는다. Unity가 만든 folder meta의 valid unique GUID를 기록한 뒤 `EditorSceneManager.SaveScene`으로 exact scene path를 생성한다. Unity가 scene meta를 생성하도록 하며 meta GUID를 수동 지정하지 않는다.

resume에서 허용되는 신규 항목은 exact directory `1`, folder meta `1`, scene `1`, scene meta `1`이다. 다른 directory/folder meta가 생기면 `BLOCKED`.

## Variable Overlay Contract

`BiomePatchOverlaySnapshot.Create(BiomePatchValidationPublication)`은 다음을 검증한다.

- publication approved, rules `15/15`, violations/errors `0/0`
- source chain identity와 exact 169 ownership rows
- assigned/unassigned `165/4`; patch size sum equals assigned
- patch/export/validation row counts are mutually exact and equal actual `Patches.Count`
- Core count equals actual required Core bindings (`4` in current contract)
- Satellite/Intrusion counts are derived from actual patch roles
- `Core + Satellite + Intrusion == actual patch count`
- each role/seed/site/biome/perimeter/compactness/ownership invariant unchanged

production acceptance에서 total `17`, Satellite `10`, Intrusion `3`을 요구하지 않는다. observed `15..19`는 repair coverage range이지 새 hard-coded production limit가 아니다. approved publication과 actual source inventory가 authoritative다.

Malformed publication, row-count mismatch, role-count mismatch, invalid binding/ownership은 계속 deterministic rejection한다. source repair, missing row inference, fallback role은 금지한다.

## GUI / Scene Drawer

summary와 patch rows는 actual counts/list를 표시한다.

- `Patches.Count`, Core/Satellite/Intrusion, assigned/unassigned, rules를 actual snapshot에서 표시
- 15,16,17,18,19 rows가 sidebar bounds 안에서 겹침/누락 없이 모두 판독 가능
- row order remains PatchId ordinal; cell/grid/orientation/hit-test/tooltip unchanged
- Game View와 Scene View는 같은 stateless `BiomePatchOverlayGui.Draw`를 사용
- unknown biome/role rejection, GUI state restore, no file/RNG/source mutation 유지

fixed `17` 텍스트/loop/bounds가 없으면 GUI/drawer production을 수정하지 말고 tests만 실제 가변 동작을 증명한다.

## Overlay Repair Tests

`BiomePatchOverlayTests` actual cases exact `150 -> 155`:

- five approved fixtures with actual patch totals `15,16,17,18,19`
- actual Core/Satellite/Intrusion count conservation
- rows/summary/lookup/immutability/culture deterministic
- malformed count/linkage rejection remains exact

`BiomePatchOverlaySceneDrawerTests` actual cases exact `24 -> 28`; 아래 progress scene 검사 네 건을 포함한다. 44 witnesses를 parameterized golden list로 만들지 않는다.

## Manual Progress Test Scene

repaired overlay/harness SHA와 compile을 확인한 뒤 위 folder contract로 scene을 생성한다. raw YAML 작성은 금지한다.

```text
Scene: Assets/_Game/Scenes/MapGenerationProgressTest.unity
Root:  MAP Generation Progress Test
Tag:   EditorOnly
Camera: Main Camera, orthographic, solid dark background
Canvas/EventSystem: none
Build Settings entry: none
```

Root exact components:

```text
Transform
WorldTopologyOverlay
SiteReservationOverlay
BiomePatchOverlay
MapGenerationProgressSceneHarness
```

`MapGenerationProgressSceneHarness`는 Editor assembly의 Editor-only manual adapter다. same file에 custom inspector와 private fixture builder를 둘 수 있다. Runtime production assembly와 public generator/overlay API를 호출하지만 새 production root/pass/retry adapter가 아니다.

serialized inputs:

```text
World seed text (decimal or 0x hex)
Attempt ordinal 0..99
Selected tab: Topology / Sites / Biomes
```

Inspector actions:

```text
Load Known Viable
Run Selected Single Attempt
Show Topology
Show Sites
Show Biomes
Clear
```

known viable action:

- seed `0x0123456789ABCDF9`, attempt `24`
- existing exit test의 approved P01 geometry와 exact typed definitions을 test-only fixture로 재사용
- public MAP02 grid, approved MAP03 snapshot/diagnostics, MAP04 stage order를 in-memory 실행
- final overlay exact `169`, patch `17=4/10/3`, assigned/unassigned `165/4`, rules `15/15`, RNG `1912`
- three overlay snapshots을 transactional injection하고 default Biomes tab을 표시

selected action:

- exact one attempt만 실행; implicit `0..99` loop나 automatic PASS_SITE re-reservation 없음
- Completed면 available three snapshots과 actual variable counts 표시
- RetryRequired/Invalid면 stable stage/status/reason/draw count를 Inspector에 표시하고 invalid downstream snapshot은 publish하지 않음

tab action은 exact one overlay component만 enabled로 만들고 snapshot을 재생성하지 않는다. Clear는 세 snapshot/status를 지운다.

금지:

- `Awake`, `OnEnable`, `Update`, delayCall, sceneOpened callback에서 generation
- polling, continuous repaint, hidden retry, file/CSV output, registry mutation
- generated object save, scene dirty on button action, Undo/SetDirty on transient snapshot
- scene build-list 등록, production scene/prefab 수정

scene 최초 open/reload 상태는 snapshots `0/0/0`, generation calls `0`, Scene dirty false다. Inspector action 후 `SceneView.RepaintAll`과 `QueuePlayerLoopUpdate`는 각 once만 허용한다.

## Scene Tests — Exact +4

1. scene additive load: exact root/camera/components/tag, Canvas/EventSystem `0`, build-list entry `0`.
2. load/reload/update: no automatic generation, snapshots empty, scene dirty delta `0`.
3. known viable action: three projections valid, expected counts/vectors, one active tab, source/RNG mutation `0`.
4. selected retry fixture + tab/Clear: stable reason, no invalid publication, toggles exactly one, cleanup/residue/active-scene restoration PASS.

tests는 scene을 저장/변경하지 않고 이전 active scene/setup을 finally 복원한다.

## Exit Batch / Frozen Gates

```text
world seeds 0..999; attempts 0..99
Completed + PassSiteHandoffRequired = 1000
Invalid + Unclassified + Lost = 0
BiomePatchOverlaySnapshot:ExactProjectionRejected = 0
PatchCleanup:InvalidSourceSnapshot = 0
```

Completed는 validator/CSV/overlay/site ownership 모든 MAP04 invariant를 통과한다. Handoff는 exact 100 allowed RetryRequired, no publication/mutation이다. 102 determinism cases와 known viable SHA/RNG vectors는 변경하지 않는다.

## Required Runs

```text
BiomePatchOverlayTests 155/155 PASS
BiomePatchOverlaySceneDrawerTests 28/28 PASS
Overlay combined 183/183 PASS
Original MAP04 focused baseline 1454/1454 PASS
Cleanup repair case +1 PASS
Overlay/scene repair cases +9 PASS
MAP04 focused total >=1464 PASS
Map04ExitTests exact 110/110 PASS
MAP04 phase actually executed >=1574 PASS
failed/skipped 0/0
Game.Map discovery >=5365
Full EditMode discovery >=5477
forced compile / Console errors / relevant warnings = 0/0/0
```

Visual actual:

- variable inventory 15 and 19 fixtures: Game/Scene shared overlay checklist `18/18` each
- saved progress scene known viable/three tabs/status/Clear/reload checklist `12/12`
- stale captures/Result만 인용하지 않는다.

Unity/Test Runner/Scene access가 없어 실제 gate를 완료하지 못하면 `BLOCKED`. `[Ignore]`, `[Explicit]`, skip/inconclusive, seed/sample/attempt 축소는 금지한다.

## Asset / Scope Gate

```text
Assets meta 3149 -> 3151
resume new directory/folder meta/scene/scene meta = 1/1/1/1
blocked-attempt preexisting harness C#/meta = 1/1
original pre-v1.3 baseline부터 cumulative new Editor C#/scene/meta = 1/1/3
modified existing C# = overlay allowlist only
all existing touched meta SHA/GUID unchanged
other production/tests/scenes/unexpected Assets changes 0
Authoring CSV/meta 50/50; duplicate GUID 0
Prefab/asmdef/Packages/ProjectSettings/generated files 0
```

## Result / Finalize

Result `<=200 lines`: STATUS, apply/SHA, prior conflict, modified/new paths+hash/GUID, variable inventories, scene hierarchy/actions, batch disposition/histogram/SHA, determinism/known vectors, tests/visual, compile/meta/scope, exit decision, NEXT만 기록한다.

PASS exact lines:

```text
STATUS: PASS
MAP04 EXIT: APPROVED
MAP PROGRESS TEST SCENE: READY
MAP05 ENTRY: ELIGIBLE FOR SEPARATE PATCH
MAP05_01_BUILD_MANDATORY_TERMINALS: LOCKED / DO NOT START
```

모든 gate가 PASS일 때만 MAP04_11을 COMPLETE, Current Task NONE, Last Completed/Result를 MAP04_11로 finalize한다. MAP05_01은 LOCKED로 유지하고 별도 patch 없이는 생성/시작하지 않는다.
