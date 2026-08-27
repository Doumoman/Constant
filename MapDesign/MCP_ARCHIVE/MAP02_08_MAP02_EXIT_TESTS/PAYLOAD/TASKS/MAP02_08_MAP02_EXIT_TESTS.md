# MAP02_08 — MAP02 Exit Tests

```yaml
status_control:
  task_key: MAP02_08_MAP02_EXIT_TESTS
  result_file: REPORTS/MAP02_08_MAP02_EXIT_TESTS_RESULT.md
```

## Objective

MAP02_01~07의 production을 수정하지 않고 phase-level 통합 EditMode tests와 현재 프로젝트 시각 재검증으로 MAP02 Phase Gate를 최종 판정한다.

exit gate는 exact 13×13/169-cell row-major grid, 624 directed/312 undirected reciprocal links, six domain-separated RNG streams, recorded `PASS_GRID`, exact seed manifest/two-file bundle, atomic publish/load, one-call replay, 100회 동일 static sector CSV hash, timing isolation, 뒤집힘 없는 shared Game/Scene overlay를 하나의 evidence chain으로 묶는다.

현재 frozen generated output에는 JSON artifact가 없다. roadmap의 `동일 JSON/CSV hash` 중 이 구현의 authoritative static identity는 exact `generated_world_sectors.csv` bytes/SHA-256다. exit test를 위해 JSON이나 후속 output placeholder를 새로 만들지 않는다.

## Mandatory Read / Scope

entrypoint → locked/work/CSV/Unity/change/patch/finalize rules → Master → Status → 이 Task → MAP02_01~07 PASS Results 순서로 읽는다.

Map Package v1.0 exact path가 installed tree에 있으면 아래 부분만 읽는다.

```text
01_FIXED_SPEC/01_WORLD_FIXED_SPEC.md
01_FIXED_SPEC/02_COORDINATE_AND_ID_RULES.md
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
01_FIXED_SPEC/04_RUNTIME_ARCHITECTURE.md
02_PHASE_ROADMAP/MAP02_TOPOLOGY_GRAYBOX.md
05_GENERATED_OUTPUT_SCHEMA/README.md
05_GENERATED_OUTPUT_SCHEMA/seed_manifest.csv
05_GENERATED_OUTPUT_SCHEMA/generated_world_sectors.csv
```

exact 문서가 installed tree에 없으면 MAP02_01~07 Task의 frozen contracts를 authoritative fallback으로 사용한다. 대체 문서나 Legacy generator를 broad search하지 않는다.

기존 public API와 test fixture pattern 확인은 아래 exact 범위만 허용한다.

### MAP02 Runtime production — exact 37

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedSectorRole.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedWorldData.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedWorldDataCsvSerializer.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/RngResetScope.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/RngStreamScope.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/DeterministicRngStream.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/DeterministicRngSeedDeriver.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/DeterministicRngStreamFactory.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationRngStreams.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGridIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorNeighborIndices.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GridInitializationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GridInitializationPass.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationFailurePolicy.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationArtifactStore.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationPassContracts.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationPassRegistry.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GridInitializationPassAdapter.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationRootResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationRoot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationClock.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationAttemptRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationPassExecutionRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationExecutionRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationExecutionResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SeedManifest.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SeedManifestCsvSerializer.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SeedReplayBundle.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SeedReplayRecorder.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SeedReplayVerificationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SeedReplayPlayer.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SeedReplayPublisher.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/WorldTopologyOverlayCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/WorldTopologyOverlaySnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/WorldTopologyOverlayGui.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/WorldTopologyOverlay.cs
```

### MAP02 Editor production — exact 1

```text
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Preview/WorldTopologyOverlaySceneDrawer.cs
```

### Existing MAP02 focused tests — exact 8 files

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/GeneratedWorldDataTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/DeterministicRngStreamTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/GridInitializationPassTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/WorldGenerationRootTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/WorldGenerationExecutionRecordTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SeedReplayRecorderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/WorldTopologyOverlayTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Preview/WorldTopologyOverlaySceneDrawerTests.cs
```

### Supporting exact APIs

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/ContentVersionHash.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/WorldGenerationDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/WorldRouteDefinitionSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/StaticDataRegistry.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/ContentVersionHashCalculatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/WorldRouteDefinitionBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/StaticDataRegistryBuilderTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Editor/MapAuthoring/MapAuthoring.Editor.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/MapAuthoring.Tests.EditMode.asmdef
```

위 C#/asmdef와 matching meta, 승인된 WorldGeneration 폴더의 path-only inventory, Authoring CSV/meta count·hash, 전체 meta GUID, change-scope path만 검사할 수 있다. MAP03 이후 Task body, Legacy/Stage/P6/P11 body, unrelated CSV body, Scene/Prefab YAML은 읽거나 사용하지 않는다.

## WRITE ALLOWLIST

신규 Runtime EditMode test C# 1:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map02ExitTests.cs
```

matching `.cs.meta` 1과 Result 1만 생성한다. production C#, 기존 tests, asmdef/asmref, Authoring/generated CSV/meta, Scene/Prefab/Package/ProjectSettings는 생성·수정·삭제·이동하지 않는다. 새 directory/folder meta를 만들지 않는다.

test namespace/fixture:

```text
namespace StarNight.Map.Tests.WorldGeneration.Generation
public sealed class Map02ExitTests
```

private nested fake pass/clock/fixture builder와 private helper는 이 test 파일 안에서만 허용한다. production fake, reflection-based production discovery, shared static mutable test state는 만들지 않는다. test-owned filesystem은 `Path.GetTempPath()` 아래 fresh exact directory만 사용하고 `finally`에서 해당 directory만 정리한다.

## Result Chain / Inventory Gate

- Master unique Task `205`; MAP00 `10/10 COMPLETE`, MAP01 `17/17 COMPLETE`, MAP02_01~07 `COMPLETE`, MAP02_08 `CURRENT`, MAP03 이후 `LOCKED`다.
- MAP02_01~07 Result 각각의 task ID와 exact `STATUS: PASS`를 확인한다.
- latest handoff는 MAP02_07 focused `88/88`, targeted `1442/1442`, full `1482/1482`, visual `12/12`, final meta `2988`, exact Assets changes `14`, existing modification `0`이다.
- 위 MAP02 runtime production `37/37`, editor production `1/1`, existing focused test `8/8`과 matching metas가 존재한다.
- 신규 dedicated WorldGeneration asmdef/asmref `0`, Runtime `UnityEditor` dependency `0`, duplicate GUID `0`이다.
- Authoring CSV/meta `50/50`은 변경되지 않고 MAP01 phase gate remains approved다.

## Frozen Grid / Topology Exit Gate

locked values:

```text
World = 624 x 416 logical tiles
Sector = 48 x 32 tiles
Grid = 13 x 13 = 169 sectors
Index = y * 13 + x
Origin = lower-left
Directions = L(-1,0), R(1,0), U(0,1), D(0,-1)
```

new exit fixture가 독립 expected formula로 아래를 exhaustive 확인한다.

- index set `0..168`, coordinate set `(0..12,0..12)`, duplicate/missing/extra `0`
- visual/data orientation을 섞지 않고 data y=0은 bottom, y=12는 top
- corner `4 × 2`, non-corner boundary `44 × 3`, interior `121 × 4`
- directed valid links `624`, undirected edges `312`, reciprocal mismatch `0`, connected component `1`
- border out-of-world neighbor exact `-1`, wrap/diagonal/self/duplicate link `0`
- all P00 cells `Unassigned`, ID fields empty, distance `-1`, mandatory flag false
- seed `0`, `4660`, `ulong.MaxValue` exact preservation
- existing sector CSV exact BOM/CRLF/13 columns/169 data rows/final CRLF
- header-only template `210` bytes / SHA-256 `0721cfa4acb6bfb2d85e04ee295960a63844e4c5c72648f9e9cdb5d260aebf59`

## RNG Exit Gate

required stream/scopes exact:

```text
RNG_WORLD_SITE     / WORLD
RNG_BIOME_PATCH    / PASS
RNG_ROUTE          / PASS
RNG_TYPE0          / PASS
RNG_SECTOR_RECIPE  / SECTOR
RNG_POPULATION     / SPAWN
```

world seed `0x0123456789ABCDEF`, attempt `0` vector:

| Stream | Identity | Initial | First | Second |
|---|---|---:|---:|---:|
| RNG_WORLD_SITE | empty | `60D4B46EBF6EF00D` | `F627BD56683B33FC` | `4CA318D8E4EA97BA` |
| RNG_BIOME_PATCH | PASS_BIOME | `98BC23250806566B` | `D2E329C4A736E686` | `F63F41F61CC1B52C` |
| RNG_ROUTE | PASS_ROUTE | `8EDC9EB9BA0977DC` | `CA6E229CF519975D` | `2289076DA3C2FFE2` |
| RNG_TYPE0 | PASS_TYPE0 | `570969677634D631` | `3F79615689D9D77E` | `8A8D7006920CD2E8` |
| RNG_SECTOR_RECIPE | 6,6 | `08D7C54EF3F843DE` | `612FB5C8F12DDB0A` | `DD0D4A17DDF66EA1` |
| RNG_POPULATION | 6,6 | `36D00A33DAED7549` | `472FBC58241A8307` | `93591B6C5B950D32` |

- exit fixture 자체가 six vectors를 production API로 재계산한다. production vector lookup을 추가하지 않는다.
- stream creation/draw order를 forward/reverse/interleaved로 바꿔도 ID별 first/second values가 같다.
- 각 stream 하나에 extra draw/rejection/retry ordinal을 넣어도 다른 five stream sequence/draw count가 변하지 않는다.
- RNG 생성·소비를 grid calls 전후로 바꿔도 topology와 sector CSV bytes/hash는 같다.
- `System.Random`, `UnityEngine.Random`, wall clock, hash code, static/global mutable RNG를 MAP02 production에 도입하지 않는다.

## Root / Execution Record Exit Gate

- exact typed fixture에서 `ExecuteThroughRecorded("GEN_MOONPALACE_V1", seed, "PASS_GRID")`는 Root/pass/attempt success, pass/attempt count `1/1`, retry `0`, last/inclusive `PASS_GRID`, artifact exact `GRID`다.
- root/pass/attempt의 world seed와 pass identity가 exact caller input과 같다.
- 기존 `ExecuteThrough`는 same execution semantics의 `.Result` projection이며 record를 얻으려고 pass를 재실행하지 않는다.
- explicit test-local failing pass scenario는 execution record에 exact world seed, failure pass ID, original attempt failure, stable aggregate/root code를 남긴다. stack/path/thread/timestamp를 failure identity로 사용하지 않는다.
- plan prevalidation failure는 invocation/pass/attempt/retry `0`이고 seed/failure identity는 deterministic하다.
- timing schedule을 바꿔도 Result/artifact/issues/counts/sector bytes는 같고 UTC/duration만 diagnostic으로 달라질 수 있다.

## Seed Manifest / Replay Exit Gate

successful recorded grid를 `SeedReplayRecorder`에 exact 1회 전달해 아래를 확인한다.

```text
files = seed_manifest.csv, generated_world_sectors.csv
relative path = GeneratedWorlds/WORLD_MOONPALACE_V1/{seed D16}
approved = 0
failure_rule_ids = empty
notes = MAP02_GRID_CHECKPOINT_V1
retry_count_total = 0
```

- seed manifest header-only template `184` bytes / SHA-256 `fb45bfbb905f165b4702515484b97c83232fca9aa7bf775dd46cc52421761b0c`다.
- bundle은 exact two files만 가지며 edge/biome/site/route/JSON/sidecar placeholder가 없다.
- recorder는 supplied execution을 재실행하지 않고 filesystem에 쓰지 않는다.
- test-owned temp root에 publish→load하면 exact directory/file set과 bytes가 같다. staging/backup residue `0`이다.
- same content hash/build identity player는 precondition 후 Root grid prefix를 exact 1회 재생하고 static sector bytes를 비교해 success한다.
- invalid bundle/manifest/content/build는 expected stable code이며 precondition failure에서는 Root invocation `0`이다.
- different valid clocks의 manifest UTC/duration은 달라도 sector bytes/hash와 replay result는 같다.

## 100-Run Determinism Gate

frozen sample:

```text
seed = 4660
generated_world_sectors.csv bytes = 5865
SHA-256 = 94ea893d55e80e4ec0a5a4758b7d84bd8e999942064d3205600e0f5a8a1bd13b
```

current production에서 아래를 실제 100회 수행한다.

1. fresh/reused `GridInitializationPass`의 topology tuple과 sector bytes/hash exact 동일
2. RNG streams를 서로 다른 creation/draw order로 소비한 전후 grid bytes/hash exact 동일
3. fresh/reused Root의 recorded grid result와 recorder sector bytes/hash exact 동일
4. same valid replay bundle의 fresh/reused player verification success
5. prior snapshot/record/bundle collection이 후속 iteration에서 mutate되지 않음

`seed_manifest.csv`의 start/duration 또는 whole-directory hash가 100회 같다고 요구하지 않는다. static output hash는 `generated_world_sectors.csv`에만 적용한다.

## Overlay Exit Gate

- `WorldTopologyOverlaySnapshot.Create`가 replay/grid result의 exact seed, 169 cells, bounds, Role, neighbors를 copy한다.
- fixed `440×564` panel, `416×416` grid, `32×32` cell rect를 유지한다.
- visual top-left `(0,12)`, top-right `(12,12)`, bottom-left `(0,0)`, bottom-right `(12,0)`다.
- all P00 labels는 `x,y\nU`; Role은 color뿐 아니라 glyph/legend/token으로 식별된다.
- `(0,0)`, `(6,6)`, `(12,12)` hit/tooltip의 tile bounds와 neighbors가 exact다.
- Game `OnGUI`와 Scene gizmo drawer는 동일 runtime `WorldTopologyOverlayGui.Draw`를 사용한다.
- overlay는 Root/pass/RNG/replay/file을 자동 실행하지 않고 snapshot/source를 mutate하거나 Scene에 저장하지 않는다.

## New Exit Test Contract

`Map02ExitTests.cs`는 actual NUnit cases 최소 `48`개다. parameterized cases는 허용하되 각 vector/cell/sample이 Test Runner actual case로 집계돼야 한다.

Minimum groups:

- result/inventory-independent runtime contract smoke and locked constants
- exhaustive grid/topology/neutral state/CSV envelope
- six RNG vectors, permutations, extra-draw independence
- successful/failing Root execution records and single-invocation semantics
- exact manifest/bundle/atomic publish-load/replay integration
- 100-run direct/root/recorder/player determinism and timing isolation
- overlay snapshot/layout/orientation/hit/tooltip integration
- culture invariance under at least `en-US` and `tr-TR`
- source collections, records, bundle bytes and snapshot isolation

Rules:

- `[Ignore]`, `[Explicit]`, inconclusive, assumption-based skip, assertion weakening 금지
- test order, current culture, wall clock, existing filesystem, shared static Store에 의존 금지
- private scripted clock/counting pass는 deterministic diagnostic/invocation observation만 하며 production behavior를 대신하지 않음
- private registry fixture는 existing production definition constructors/public APIs와 frozen profile/pass vectors를 사용하고 production object를 mutate하지 않음
- failure가 나면 production을 이 Task에서 고치거나 expected value를 현재 오동작에 맞추지 않음

## Production / Ownership Audit

exact MAP02 production body를 read-only로 확인한다.

- Runtime production `37`, Editor production `1`; unexpected MAP02 production `0`
- existing tests `8` + new exit test `1`; matching metas present/project-unique
- Runtime `UnityEditor` dependency `0`, new asmdef/asmref `0`
- `System.Random`/`UnityEngine.Random` production dependency `0`
- file I/O production owner는 `SeedReplayPublisher`만 해당
- Editor API production owner는 exact editor overlay file만 해당
- static mutable RNG/record/replay/snapshot/GUI cache `0`; stateless system clock singleton은 허용
- later pass/biome/site/route/type0/recipe/population implementation `0`
- generated JSON/edge/biome/site placeholder output `0`
- Authoring/Registry/generated input mutation `0`

## Tests / Verification

```text
New MAP02 exit tests: >=48 PASS
MAP02_01 GeneratedWorldData: 56/56 PASS
MAP02_02 deterministic RNG streams: 103/103 PASS
MAP02_03 GridInitializationPass: 90/90 PASS
MAP02_04 WorldGenerationRoot: 84/84 PASS
MAP02_05 execution records: 77/77 PASS
MAP02_06 seed manifest/replay: 97/97 PASS
MAP02_07 topology overlay: 88/88 PASS
Existing MAP02 focused aggregate: 595/595 PASS
MAP02 phase focused aggregate: >=643 PASS
ContentVersionHash: 54/54 PASS
Previous Game.Map targeted baseline: 1442/1442 PASS
Game.Map targeted total: >=1490 PASS
Full project EditMode: >=1530 PASS
failed = 0 / skipped = 0
Unity 6000.3.8f1 / forced refresh / compile error 0 / relevant warning 0
PlayMode NOT RUN / saved Scene-Prefab changes NONE
```

Authoring CSV/meta `50/50` unchanged, accepted legacy folder meta `6/6` unchanged, 신규 matching meta `1/1` valid, final Assets meta `2989`, project duplicate GUID `0`을 확인한다. Task marker 이후 final Assets 변경은 신규 test C# 1 + matching meta 1 = `2`, unexpected `0`이어야 한다. existing Assets modification exact `0`이다.

## Current-Project Visual Revalidation

MAP02_07 Result/captures를 대신 인용하지 말고 Unity MCP/Editor에서 seed `4660`으로 현 프로젝트를 다시 확인한다. transient object는 `HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild`만 사용하고 종료 시 제거한다.

1. Scene/Game title과 seed 동일
2. 두 View 모두 13×13/169 cells 전부 표시
3. top y=12 / bottom y=0, four corners orientation exact
4. all coordinate/U glyph와 five-item legend 판독 가능
5. `(0,0)` tooltip exact bounds/neighbors
6. `(6,6)` tooltip exact index/bounds/neighbors
7. `(12,12)` tooltip exact index/bounds/neighbors
8. outside hover exact empty text, clamp 없음
9. Scene/Game renderer content와 snapshot identity 동일
10. selection/cameras/transform/timeScale 불변
11. Clear 후 Game/Scene overlays absent
12. transient object/capture cleanup 후 hierarchy residue와 saved Scene/Prefab/dirty-state delta `0`

visual `12/12` current evidence가 없으면 tests가 모두 PASS여도 `BLOCKED`다. capture가 필요하면 project `Temp/MAP02_08_Captures`만 사용하고 final source/change scope에서 제외·정리한다.

## Failure Policy

- new/existing test failure, vector/hash/topology/replay/visual mismatch, compile error, relevant warning, unexpected Assets delta면 `STATUS: FAIL`이다.
- Unity/Test Runner/Scene/Game visual 접근이 없어 실제 gate를 수행할 수 없으면 `STATUS: BLOCKED`다.
- FAIL/BLOCKED defect를 production 수정, assertion 완화, stale result 인용으로 해결하지 않는다.
- PASS가 아니면 MAP02 exit 승인이나 STATUS FINALIZE를 수행하지 않고 MAP03를 열지 않는다.

## Result / Completion

Result: `REPORTS/MAP02_08_MAP02_EXIT_TESTS_RESULT.md`.

Required sections: TASK, STATUS, SUMMARY, READ, MASTER BACKLOG CHECK, PRIOR RESULT CHAIN, CREATED, MODIFIED, PREEXISTING_IDENTICAL, PRODUCTION INVENTORY, GRID AND TOPOLOGY, RNG STREAMS, ROOT AND EXECUTION RECORDS, SEED MANIFEST AND REPLAY, 100-RUN DETERMINISM, OVERLAY, TEST, VISUAL REVALIDATION, UNITY, ASSET META VALIDATION, CHANGE SCOPE, PRODUCTION OWNERSHIP AUDIT, OUT_OF_SCOPE_FINDINGS, MAP02 EXIT DECISION, DONE CONDITIONS, NEXT, Recommended Commit.

PASS Result에는 exact lines가 있어야 한다.

```text
STATUS: PASS
MAP02 EXIT: APPROVED
MAP03 ENTRY: ELIGIBLE FOR SEPARATE PATCH
MAP03_01: LOCKED / DO NOT START
```

모든 조건 PASS 시에만 MAP02_08 COMPLETE, Current Task NONE으로 finalize한다. MAP03_01은 LOCKED로 유지하고 자동 시작하지 않는다.

Recommended Commit: `test(map): approve map02 topology phase gate`
