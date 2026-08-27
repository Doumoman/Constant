# MAP04_11 — MAP04 Batch and Exit Tests

```yaml
status_control:
  task_key: MAP04_11_MAP04_BATCH_AND_EXIT_TESTS
  result_file: REPORTS/MAP04_11_MAP04_BATCH_AND_EXIT_TESTS_RESULT.md
```

## Goal

MAP04_01~10 production을 수정하지 않고 test-only 1,000-world-seed batch, whole-attempt retry, 102-seed determinism, known viable replay, CSV/validator/overlay revalidation으로 MAP04 Phase Gate를 판정한다.

production batch runner, `PASS_BIOME` adapter/root integration, retry policy, generated file writer는 만들지 않는다. private test runner가 existing public APIs를 frozen production order로 호출한다.

## Prior Gate / Read

control → Master/Status → 이 Task → MAP04_01~10 Tasks/Results ascending 순서로 읽는다.

```text
Prior Result SHA-256 76a982a0258f4348bdc52e1e73e6ffe56a3a05ad42d38a9db11186f32df84dca
STATUS PASS; overlay runtime/editor/combined 150/24/174
regressions/actual 444/618; failed/skipped 0/0
cells/patches 169/17; roles 4/10/3; assigned/unassigned 165/4
Game/Scene visual 18/18 each; Assets meta 3147; scope conflict 0
```

Result 또는 Current Task가 다르면 `BLOCKED`. MAP05_01 이후 Task body는 읽거나 시작하지 않는다.

Body read allowlist:

- MAP04_01~10 Task `WRITE ALLOWLIST`에 나열된 exact production/test C#
- MAP04_01~10 Results 및 matching meta/GUID, approved folder path-only inventory
- `WorldGenConstants`, `SectorCoord`, `GeneratedWorldData`, `SiteReservationSnapshot`
- typed biome/patch/boundary definitions, deterministic RNG stream/factory/scopes
- Game.Map Runtime/Editor/Runtime-test/Editor-test asmdef
- MAP04 roadmap/ownership/fixed coordinate reference만; Authoring CSV body는 금지

unrelated production/test, Legacy/Stage/P6/P11, Scene/Prefab YAML, future Task는 금지한다.

## Write Allowlist

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map04ExitTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map04ExitTests.cs.meta
MapDesign/MCP/REPORTS/MAP04_11_MAP04_BATCH_AND_EXIT_TESTS_RESULT.md
```

exact test C# 1 + matching meta 1 + Result만 생성한다. production/existing test/CSV/asmdef/Scene/Prefab/Packages/ProjectSettings 수정 0, 신규 폴더/meta 0.

```text
namespace StarNight.Map.Tests.WorldGeneration.Generation
public sealed class Map04ExitTests
```

private immutable attempt record/aggregate, typed fixture builder, canonical digest helper만 test file 내에 허용한다. shared static mutable state, reflection mutation, filesystem output, production fake/runner는 금지한다.

## Inventory / Prior Chain

- Master rows `205`; MAP00~03 exit approved, MAP04_01~10 COMPLETE, MAP04_11 CURRENT, MAP05+ LOCKED.
- MAP04_01~10 Result task/status/SHA chain을 검증한다.
- focused counts exact `107/121/127/141/164/156/127/141/196/174`, aggregate `1454`.
- MAP04_10 기준 Assets meta `3147`, Authoring CSV/meta `50/50`, duplicate GUID `0`, existing/unexpected `0/0`.
- MAP04 production dependency의 UnityEditor/file I/O/static mutable RNG/cache와 production batch/root adapter 추가 `0`.

## Test Fixture / Attempt Pipeline

private fixture는 existing public constructors와 MAP03 approved seven-reservation geometry, exact four biome/ten patch rule/six boundary profile/six pair rule definitions을 사용한다. CSV/Registry singleton/file을 읽지 않는다.

per world seed에 exact 169-row neutral `GeneratedWorldData`와 immutable P01 `SiteReservationSnapshot`을 만들며 seed/source identities를 일치시킨다. prepared definitions/geometry는 copy-only이고 selected patch/RNG/result를 cache하지 않는다.

`RunAttempt(worldSeed, attemptOrdinal)` exact order:

```text
1 CorePatchSeedInitializer
2 CorePatchGrower
3 SatelliteSeedPlacer          \
4 MultiSeedBiomeGrower          > same continued fresh-attempt RNG_BIOME_PATCH
5 IntrusionPlacer              /
6 PatchCleanup
7 BiomePatchExporter (memory bytes only)
8 BiomePatchValidator
9 BiomePatchOverlaySnapshot.Create on success
```

- first non-completed stage에서 short-circuit; later stage/RNG를 실행하지 않는다.
- public `RetryRequired=true`만 next ordinal의 fresh whole attempt를 허용한다. local seed redraw/repair, same stream continuation retry 금지.
- InvalidInput/error는 non-retry batch failure. attempt ordinal `0..99`, max 100 attempts/world.
- cleanup/export/validator/overlay RNG draws은 `0`; source/definitions/prior result mutation `0`.

Canonical per-attempt record:

```text
world seed / attempt ordinal / ordered stage statuses / retry flag and stable reasons
RNG draw counts / PatchId-biome-role-seed-site identities
169 ownership rows / patch sizes-perimeters / cleanup diagnostics
patch/world CSV row-byte-SHA / 15 rule rows / overlay digest
```

exception text/path/time/thread은 digest/reason identity에 넣지 않는다.

## 1,000-Seed Full Batch Gate

authoritative sample:

```text
world seeds = ulong 0..999 inclusive
attempt ordinal = 0..99
world count = 1000
```

acceptance:

- resolved within 100 attempts `1000/1000`; invalid/error/unresolved `0/0/0`
- every success: validator rules `15/15`, violations/errors `0/0`, overlay creation PASS
- required Core biomes/bindings `4/4`; every bound footprint cell matching Core biome/PatchId; site misownership `0`
- assigned/unassigned/patch-sector sum `165/4/165`; unassigned nonallowed `0`; SecondaryBiome nonempty `0`
- Core/Satellite sizes `2..59`, Intrusion exact `1`; disconnected/overlap/orphan `0`
- normal/repeat/share/seed-distance/edge/Intrusion-boundary violations `0`
- patch CSV row count = current patch count, world rows `169`; reserialize byte mismatch `0`
- source mutation/filesystem write `0`; final snapshot seed = original world seed after retry

Result에 initial success/retry worlds, total retries, max ordinal, resolved-attempt histogram `0..99`, terminal stage/reason counts, patch-count/min-max/role aggregates, canonical SHA-256 digest를 기록한다. observed values는 production lookup/golden table로 하드코딩하지 않는다.

## Known Viable / Determinism / Retry Isolation

known viable exact:

```text
world seed 0x0123456789ABCDF9 / attempt 24
patches 17 = 4/10/3; assigned/unassigned 165/4
patch/world bytes 1956/16380
patch SHA 7ccf1fc1e6ebd298cc97bed3914395170fc38fe85b2d2392c80c9f30ec000543
world SHA 07daa96fe5f6ea985aa9e32aa0609d65b95c620a0b05a99426d3093275f8ee1d
rules 15/15; RNG final 1912; mutation/file 0/0
```

- seeds `0..101` full resolution을 fresh services, reused stateless services, reverse enumeration+seed sort로 실행해 record/digest exact 일치.
- representative/naturally retried seeds를 `en-US`, `tr-TR` 및 shuffled definitions에서 재실행해 일치.
- same seed+attempt repeated 100회의 status/RNG/patch/CSV/rule/overlay digest exact 일치.
- failed attempt diagnostics/source와 completed publication은 later attempt 후에도 불변.
- no natural failure에 의존하지 않고 public invalid/capacity/placement/validation fixtures로 retry/non-retry classification·short-circuit·attempt reset을 각각 검증.

102-seed determinism은 bounded audit sample이며 1,000-seed full acceptance를 축소한 것으로 표현하지 않는다.

## Overlay / Ownership Exit Gate

MAP04_10 exact transient fixture(seed display `-4502`)를 현재 Editor에서 재생성해 Game/Scene shared renderer를 재검증한다.

- cells/patches `169/17`, roles `4/10/3`, assigned/unassigned `165/4`, rules `15`
- four colors/IDs, PatchId boundaries, `C/S/I`, Core site `*`, seed `+`, 17 rows
- size/perimeter/compactness, corner orientation, hit-test/tooltip, undersized message
- runtime/editor same Draw; automatic generation/RNG/file/Scene save/source mutation `0`

current visual checklist Game/Scene `18/18` each. stale MAP04_10 Result/capture만 인용하지 않는다. transient object/capture는 project `Temp` 아래만 사용하고 cleanup/residue/Scene dirty delta `0`.

## Tests / Gates

`Map04ExitTests.cs` actual NUnit cases `>=110`. batch loop를 1,000 parameterized TestCases로 만들지 말고 streaming aggregate/digest를 사용한다.

minimum groups: result-chain/inventory, fixture/source invariants, stage order/short-circuit, 1,000 batch conservation, known viable vectors, 102 fresh/reused/reverse/culture, synthetic retry classification, validator/CSV/overlay integration, immutability/dependency/meta/scope audit.

Actually run:

```text
New Map04ExitTests >=110 PASS
MAP04 focused exact:
107 + 121 + 127 + 141 + 164 + 156 + 127 + 141 + 196 + 174
Existing MAP04 aggregate 1454/1454 PASS
MAP04 phase aggregate / actually executed >=1564 PASS
failed/skipped 0/0
```

discovery-only Game.Map `>=5359`, Full EditMode `>=5467`. forced compile/Console/warning `0/0/0`.

`[Ignore]`, `[Explicit]`, inconclusive/assumption skip, sample/attempt reduction, seed exclusion, tolerance/expected relaxation, production fix, reflection mutation 금지. Unity/Test Runner/visual 접근 불가능으로 actual gate를 실행하지 못하면 `BLOCKED`.

Asset gate:

```text
Assets meta 3147->3148
new Runtime test/meta 1/1; exact Assets changes 2
existing/unexpected 0/0; new directory/folder meta 0
duplicate GUID 0; Authoring CSV/meta 50/50
generated CSV files 0; Scene/Prefab/Packages/ProjectSettings 0
```

## Result / Finalize

Result `<=180 lines`: STATUS, apply/SHA, prior chain, created path/GUID, batch/retry/determinism/known vectors, tests/visual, compile/meta/scope, findings, exit decision, NEXT만 기록한다.

PASS Result exact lines:

```text
STATUS: PASS
MAP04 EXIT: APPROVED
MAP05 ENTRY: ELIGIBLE FOR SEPARATE PATCH
MAP05_01_BUILD_MANDATORY_TERMINALS: LOCKED / DO NOT START
```

PASS일 때만 MAP04_11 COMPLETE, Current Task NONE, Last Completed/Result를 MAP04_11로 finalize한다. MAP05_01은 LOCKED로 유지하고 자동 생성/시작하지 않는다.

금지: production/existing test 수정, local repair/retry 숨김, generated file 쓰기, MAP05 선행, Git commit/push.
