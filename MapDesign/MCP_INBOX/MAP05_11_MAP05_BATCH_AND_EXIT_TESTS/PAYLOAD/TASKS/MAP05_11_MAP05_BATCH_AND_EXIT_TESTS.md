# MAP05_11 — MAP05 Batch and Exit Tests

```yaml
status_control:
  task_key: MAP05_11_MAP05_BATCH_AND_EXIT_TESTS
  result_file: REPORTS/MAP05_11_MAP05_BATCH_AND_EXIT_TESTS_RESULT.md
```

## TASK TYPE

```text
TEST-ONLY BATCH / DETERMINISM / VISUAL / PHASE EXIT AUDIT
```

## Objective

MAP05_01~10 production을 수정하지 않고 mandatory route phase를 batch, determinism, validation, generated CSV, overlay visual, phase-boundary audit로 최종 판정한다.

exit evidence chain은 아래를 함께 묶는다.

```text
seven mandatory terminals = 1 Start + 6 SiteEntry
Type1/2/3 exact mask lookup and Type4 U+D mandatory / L/R independent rule
minimum connector tree + horizontal backbone + vertical gateways + U/D conflict resolution
two accepted independent loops
final mandatory route graph 47 nodes / 96 directed / 48 undirected / 47 route cells
route-stamped GeneratedWorldData and generated edge records / CSV bytes
12-rule mandatory route validation report
shared Game/Scene mandatory route overlay
10,000-seed mandatory reachability audit
```

이 Task는 production batch runner, `PASS_ROUTE` adapter, root integration, generated CSV writer, retry policy를 구현하지 않는다. private test fixture가 existing public APIs를 exact production order로 호출해 phase를 감사한다. 실패가 발견되면 production/expected를 이 Task에서 고치지 않는다.

## Mandatory Read Order

1. `MapDesign/MCP/00_MCP_ENTRYPOINT.md`
2. locked/work/CSV/Unity/change/patch/finalize global rules
3. `MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md`
4. `MapDesign/MCP/06_IMPLEMENTATION_STATUS.md`
5. this Task
6. MAP05_01~10 PASS Results, task order ascending
7. MAP04_11 PASS Result and approved generated world/site/biome baseline

MAP05_10 Result가 exact `STATUS: PASS`가 아니거나 Current Task가 이 Task와 다르면 실행하지 않고 `BLOCKED`다. MAP06_01 이후 Task body는 읽거나 생성하거나 실행하지 않는다.

Prior Result exact gate:

```text
TASK: MAP05_10_CREATE_MANDATORY_ROUTE_OVERLAY
STATUS: PASS
MAP05_10 focused overlay suite: 168/168 PASS
Required regression aggregate: 1206/1206 PASS
Actually executed final gates: 1374/1374 PASS
Visual checklist: 18/18 PASS
Assets meta: 3245
SHA-256: 2f8ef4e027c1abd8f93721f840b5a6ab43d812b1bcb9bd6ae71fd8d694823c6f
DONE CONDITIONS: PASS
```

## READ ALLOWLIST

Read only these categories:

- MAP05_01~10 Task documents and PASS Results, ascending order.
- MAP05_01~10 Task `WRITE ALLOWLIST`에 나열된 exact production/test C# and matching meta.
- MAP04_11 exit-approved generated world/site/biome baseline Results only.
- `WorldGenConstants`, coordinate value types, `GeneratedWorldData`, `SectorCell`, generated sector/edge serializers.
- `MandatoryTerminal*`, `MandatoryRouteMask*`, connector tree, horizontal backbone, vertical gateway, U/D conflict, loop, graph, validator, overlay runtime/editor types.
- Game.Map Runtime/Editor/Runtime-test/Editor-test asmdef files.
- approved WorldGeneration folder path-only inventory, Authoring CSV/meta count/hash, project meta GUID, task marker 이후 change-scope path.

Do not read unrelated production/test body, MAP06+ Task body, Legacy/Stage/P6/P11 body, Authoring CSV body, generated CSV body from disk, Scene/Prefab YAML, Package/ProjectSettings body unless the exact rule requires path-only dirty detection.

## WRITE ALLOWLIST

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map05ExitTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map05ExitTests.cs.meta
MapDesign/MCP/REPORTS/MAP05_11_MAP05_BATCH_AND_EXIT_TESTS_RESULT.md
```

exact test C# 1 + matching meta 1 + Result만 생성한다. production/existing test/CSV/asmdef/Scene/Prefab/Packages/ProjectSettings 수정 0, 신규 폴더/meta 0.

```text
namespace StarNight.Map.Tests.WorldGeneration.Generation
public sealed class Map05ExitTests
```

private immutable batch record/aggregate, typed fixture builder, canonical digest helper만 test file 내에 허용한다. shared static mutable state, reflection mutation, filesystem output, production fake/runner는 금지한다.

## Inventory / Prior Chain

- Master rows `205`; MAP00~04 exit approved, MAP05_01~10 COMPLETE, MAP05_11 CURRENT, MAP06+ LOCKED.
- MAP05_01~10 Result task/status/SHA chain을 검증한다.
- focused counts exact by class:

```text
120 + 127 + 129 + 142 + 156 + 194 + 212 + 281 + 298 + 168 = 1827
```

- MAP05_10 기준 graph nodes/directed/undirected/route cells `47/96/48/47`.
- mask counts `T1/T2/T3/T4_UD/T4_LUD/T4_RUD/T4_LRUD = 20/4/4/17/0/0/2`.
- terminals reachable `7/7`, accepted independent loops `2/2`.
- validation rules/violations/errors/warnings `12/12/12 / 0/0/0`.
- generated sector/edge bytes/edge rows `16838 / 7094 / 96`.
- visual Game/Scene checklist `9/9 + 9/9 = 18/18`.
- Assets meta `3245`, Authoring CSV/meta `50/50`, duplicate GUID `0`.
- MAP05 production dependency의 UnityEditor/file I/O/static mutable RNG/cache와 production batch/root adapter 추가 `0`.

## Test Fixture / Attempt Pipeline

private fixture는 existing public constructors와 MAP04-approved 169-sector generated world baseline, MAP03-approved seven-site reservation baseline, MAP04-approved biome patch baseline, and MAP05-approved mandatory terminal/mask/graph contracts를 사용한다. CSV/Registry singleton/file을 읽지 않는다.

`RunMandatoryRouteAttempt(worldSeed, attemptOrdinal)` exact order:

```text
1 MandatoryTerminalBuilder
2 MandatoryRouteMaskLookupBuilder
3 MandatoryConnectorTreeBuilder
4 HorizontalBackboneRouter
5 VerticalGatewayPlanner
6 UpDownConflictResolver
7 MandatoryRouteLoopPlanner
8 MandatoryRouteGraphBuilder
9 MandatoryRouteGraphValidator
10 MandatoryRouteOverlaySnapshot/Create shared Game/Scene commands
```

- MAP05 is expected to resolve without retry for the approved starter geometry.
- first non-completed stage에서 short-circuit; later stage/visual validation을 실행하지 않는다.
- retry/unresolved/invalid counters must remain `0/0/0` for the approved mandatory route vector.
- graph/CSV/SectorCell/Authoring CSV/source mutation `0`.
- validator/overlay RNG draws, filesystem reads/writes, clock reads `0`.

Canonical per-attempt record:

```text
world seed / attempt ordinal / ordered stage statuses
terminal set / mask counts / graph edge digest / BFS reachability
generated sector/edge CSV byte counts / validation rule summary / overlay digest
source mutation and filesystem counters / Type4 token counts
```

exception text/path/time/thread은 digest/reason identity에 넣지 않는다.

## 10,000-Seed Full Batch Gate

authoritative sample:

```text
world seeds = ulong 0..9999 inclusive
attempt ordinal = 0
world count = 10000
```

acceptance:

- completed `10000/10000`; retry/unresolved/invalid `0/0/0`
- mandatory terminal reachability failure `0`
- route mask mismatch `0`
- Type4 U+D missing `0`
- Type4 L/R canonicalization or forced-open/forced-close mismatch `0`
- graph directed edge reciprocity failure `0`
- generated edge row bijection failure `0`
- validation rules `12/12`, violations/errors `0/0`
- overlay snapshot creation `10000/10000`, visual command digest stable
- source mutation/filesystem write/clock/RNG unexpected dependency `0`

Result에 total worlds, completed, retry/unresolved/invalid, terminal reachability failures, Type4 token aggregate, graph digest aggregate, validation failure reasons, overlay digest, canonical SHA-256 digest를 기록한다. observed values는 production lookup/golden table로 하드코딩하지 않는다.

## Known Vector / Determinism / Boundary Audit

known vector exact:

```text
graph nodes/directed/undirected/route cells = 47/96/48/47
mask T1/T2/T3/T4_UD/T4_LUD/T4_RUD/T4_LRUD = 20/4/4/17/0/0/2
terminals reachable = 7/7
loops represented = 2/2
generated sectors/edges bytes/rows = 16838/7094/96
validation rules/violations/errors = 12/0/0
visual Game/Scene = 9/9 + 9/9
```

- seeds `0..101` full resolution을 fresh services, reused stateless services, reverse enumeration+seed sort로 실행해 record/digest exact 일치.
- `en-US`, `tr-TR`, and shuffled candidate/definition order에서 재실행해 일치.
- same seed repeated 100회의 graph/CSV/rule/overlay digest exact 일치.
- MAP05_10 phase boundary tests allow overlay symbols but continue to forbid MAP06+ symbols.
- no natural failure에 의존하지 않고 public invalid/missing-terminal/broken-Type4/missing-reverse-edge/generated-edge-mismatch fixtures로 classification·short-circuit을 각각 검증.

102-seed determinism은 bounded audit sample이며 10,000-seed full acceptance를 축소한 것으로 표현하지 않는다.

## Overlay / Type4 Exit Gate

MAP05_10 exact transient fixture를 현재 Editor에서 재생성해 Game/Scene shared renderer를 재검증한다.

- 13×13 grid, PASS banner, T1/T2/T3/T4 colors/tokens, L/R/U/D side glyphs
- Start/Core/Forge/Boss/Village terminal labels
- BFS distance labels, loop markers, generated edge row count
- Type4 tokens `UD`, `LUD`, `RUD`, `LRUD` are all legal and preserved
- automatic generation/RNG/file/Scene save/source mutation `0`

current visual checklist Game/Scene `9/9` each, combined `18/18`. stale MAP05_10 Result/capture만 인용하지 않는다. transient object/capture는 project `Temp` 아래만 사용하고 cleanup/residue/Scene dirty delta `0`.

## Tests / Gates

`Map05ExitTests.cs` actual NUnit cases `>=120`. batch loop를 10,000 parameterized TestCases로 만들지 말고 streaming aggregate/digest를 사용한다.

minimum groups: result-chain/inventory, fixture/source invariants, production stage order/short-circuit, 10,000 batch conservation, known vector, 102 fresh/reused/reverse/culture, synthetic failure classification, validator/CSV/overlay integration, Type4 side preservation, phase-boundary symbol audit, immutability/dependency/meta/scope audit.

Actually run:

```text
New Map05ExitTests >=120 PASS
MAP05 focused exact:
120 + 127 + 129 + 142 + 156 + 194 + 212 + 281 + 298 + 168
Existing MAP05 focused aggregate 1827/1827 PASS
MAP05 phase aggregate / actually executed >=1947 PASS
failed/skipped 0/0
```

discovery-only Game.Map `>=7320`, Full EditMode `>=7408`. forced compile/Console/warning `0/0/0`.

`[Ignore]`, `[Explicit]`, inconclusive/assumption skip, sample/attempt reduction, seed exclusion, tolerance/expected relaxation, production fix, reflection mutation 금지. Unity/Test Runner/visual 접근 불가능으로 actual gate를 실행하지 못하면 `BLOCKED`.

Asset gate:

```text
Assets meta 3245->3246
new Runtime test/meta 1/1; exact Assets changes 2
existing/unexpected 0/0; new directory/folder meta 0
duplicate GUID 0; Authoring CSV/meta 50/50
generated CSV files 0; Scene/Prefab/Packages/ProjectSettings 0
```

## Result / Finalize

Result `<=180 lines`: STATUS, apply/SHA, prior chain, created path/GUID, 10,000 batch, determinism, known vector, tests/visual, compile/meta/scope, findings, exit decision, NEXT만 기록한다.

PASS Result exact lines:

```text
STATUS: PASS
MAP05 EXIT: APPROVED
MAP06 ENTRY: ELIGIBLE FOR SEPARATE PATCH
MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS: LOCKED / DO NOT START
```

PASS일 때만 MAP05_11 COMPLETE, Current Task NONE, Last Completed/Result를 MAP05_11로 finalize한다. MAP06_01은 LOCKED로 유지하고 자동 생성/시작하지 않는다.

금지: production/existing test 수정, local repair/retry 숨김, generated file 쓰기, MAP06 선행, Git commit/push.
