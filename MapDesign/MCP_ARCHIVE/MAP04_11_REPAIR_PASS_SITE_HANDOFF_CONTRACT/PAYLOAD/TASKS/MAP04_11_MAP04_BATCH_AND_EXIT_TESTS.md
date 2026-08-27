# MAP04_11 — MAP04 Batch and Exit Tests (Repair v1.1)

```yaml
status_control:
  task_key: MAP04_11_MAP04_BATCH_AND_EXIT_TESTS
  result_file: REPORTS/MAP04_11_MAP04_BATCH_AND_EXIT_TESTS_RESULT.md
  repair_contract: PASS_SITE_HANDOFF_AFTER_100_RETRY_REQUIRED_ATTEMPTS
```

## Goal

MAP04_01~10 production을 수정하지 않고 기존 MAP04 exit test의 과도한 same-P01 completion 조건을 교정한다. 1,000 seeds가 각각 MAP04에서 완성되거나, exact 100회 정상 `RetryRequired` 뒤 상위 `PASS_SITE` 재예약이 필요하다는 결정적 disposition에 도달함을 검증한다.

`PASS_SITE handoff required`는 성공한 world publication도 MAP04 오류도 아니다. P01 site reservation을 새로 선택해 전체 하위 pass를 다시 실행해야 한다는 test-only phase-boundary 판정이다. production adapter, batch runner, retry policy 또는 generated writer는 만들지 않는다.

## Preconditions / Prior Failure

control → Master/Status → 이 Task → 현재 FAIL Result → MAP04_01~10 관련 Task/Result/API 순서로 읽는다.

```text
Current Task SHA before repair:
1740f43b49a9e91675dc024d460690bba3f375929dfde0e33a9c4e96a9e66ef7

FAIL Result SHA:
26e949c34c01091a66e5727b408ef413483c267ab48854b20bcfe04f2173eedf

Existing test SHA:
55641357fcd6c2ca586a23ba71a210830069c685dfb61307553e0e9c1857411b

Existing test meta SHA / GUID:
8d01540bc77905fcfc0169efab907c34e3ece8dcef285539d812b2a0292dcb15
b609b3b1f6af4aa3979c42583f08974f

Observed batch: processed/completed/unresolved 1000/5/995
Observed terminal: MultiSeedBiomeGrower / InsufficientAggregateCapacity
Known viable replay: 1/1 PASS
Assets meta: 3148
```

Current Task나 FAIL Result가 다르면 `BLOCKED`. MAP05_01 이후 Task body는 읽거나 시작하지 않는다.

## Root Cause / Authority

- MAP04_05는 `InsufficientAggregateCapacity`를 noise draw 전의 정상 `RetryRequired` capacity gate로 정의한다.
- MAP04 roadmap은 동일 P01 입력에서 최대 100 whole-attempt retries 후 상위 `PASS_SITE` 재예약 요청을 허용한다.
- 1,000-seed exit gate의 목적은 ownership, determinism, retry boundary, publication integrity 검증이다. 모든 고정 P01 fixture가 반드시 MAP04에서 완성되어야 한다는 보장은 아니다.
- 따라서 기존 `resolved within 100 attempts = 1000/1000` assertion만 계약 오류다. frozen RNG/vector와 MAP04 production 동작은 변경하지 않는다.

## Read / Write Allowlist

READ:

- 현재 `Map04ExitTests.cs`와 matching meta
- 이 Task, 현재 FAIL Result, MAP04_01~10 Task/Result의 관련 계약
- 기존 test가 이미 사용하는 exact public MAP03/MAP04 APIs와 approved asmdef/path inventory

MODIFY:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map04ExitTests.cs
MapDesign/MCP/REPORTS/MAP04_11_MAP04_BATCH_AND_EXIT_TESTS_RESULT.md
```

`Map04ExitTests.cs.meta`는 읽기만 하며 SHA/GUID를 보존한다. production, 다른 test, Master, CSV, asmdef, Scene, Prefab, Packages, ProjectSettings 수정은 금지한다.

## Test-Local Final Disposition

기존 private aggregate에 다음 의미의 immutable test-only disposition을 둔다. 이름은 기존 helper 구조에 맞춰도 되지만 의미는 정확해야 한다.

```text
Completed
PassSiteHandoffRequired
Invalid
```

`RunWorld(worldSeed)`는 attempt ordinal `0..99`만 실행한다.

- 어느 attempt가 완료되면 즉시 `Completed`.
- public invalid/error/non-retry failure면 즉시 `Invalid`.
- 100 attempts가 모두 허용된 `RetryRequired`로 끝나면 `PassSiteHandoffRequired`.
- attempt `100` 실행, local repair, RNG continuation, seed exclusion, production state mutation은 금지한다.

Handoff record는 exact 100 attempt records, 최종 ordinal `99`, 각 stable terminal stage/reason, RNG draws와 mutation/publication absence를 보존한다. handoff를 completed output에 합치거나 validator/overlay 성공으로 표시하지 않는다.

## 1,000-Seed Acceptance

authoritative sample은 `ulong 0..999`, world count `1000`, max attempts/world `100`이다.

```text
Completed + PassSiteHandoffRequired = 1000
Invalid + Unclassified + Lost = 0
Completed > 0
PassSiteHandoffRequired > 0
```

모든 `Completed` world는 기존 MAP04 exit 조건을 그대로 만족해야 한다.

- validator rules `15/15`, violations/errors `0/0`, overlay creation PASS
- required Core biomes/bindings `4/4`, bound footprint misownership `0`
- assigned/unassigned/patch-sector sum `165/4/165`, nonallowed unassigned `0`
- Core/Satellite size, Intrusion size, connectivity/overlap/orphan/boundary rules 전부 PASS
- patch/world CSV row/bytes/roundtrip, source immutability, filesystem write, final seed identity PASS

모든 `PassSiteHandoffRequired` world는 다음을 만족해야 한다.

- exact 100회가 모두 `RetryRequired`; invalid/error/non-retry stage `0`
- terminal stage/reason은 public stable allowed set에 속함
- failed attempts에서 cleanup/export/validator/overlay publication `0`
- source/definitions/P01 snapshot mutation, filesystem output, leaked partial result `0`
- 같은 seed의 fresh/reused/reverse/culture 실행에서 disposition과 ordered attempt digest 일치

Result에는 completed/handoff/invalid counts, total attempts/retries, max ordinal, terminal stage/reason histogram, completed aggregate 및 canonical SHA-256를 관측값 그대로 기록한다. handoff 비율에 임의 합격 임계값을 추가하지 않는다.

## Determinism / Known Viable

seeds `0..101`의 기존 102 NUnit cases는 “반드시 resolution”을 요구하지 않는다. 각각 fresh services, reused stateless services, reverse enumeration 후 seed sort에서 다음이 exact 일치해야 한다.

```text
final disposition
ordered per-attempt status/reason/RNG digest
Completed일 때 publication/CSV/rule/overlay digest
Handoff일 때 100-record digest와 no-publication evidence
```

`en-US`, `tr-TR`, shuffled definitions, same seed+attempt repeated 100회, failed-attempt isolation, synthetic retry/non-retry/short-circuit checks는 기존 강도를 유지한다.

known viable vector는 변경 없이 exact PASS해야 한다.

```text
world seed 0x0123456789ABCDF9 / attempt 24
patches 17 = 4/10/3; assigned/unassigned 165/4
patch/world bytes 1956/16380
patch SHA 7ccf1fc1e6ebd298cc97bed3914395170fc38fe85b2d2392c80c9f30ec000543
world SHA 07daa96fe5f6ea985aa9e32aa0609d65b95c620a0b05a99426d3093275f8ee1d
rules 15/15; RNG final 1912; mutation/file 0/0
```

## Required Runs

기존 test file의 actual NUnit cases `110`을 유지한다. `[Ignore]`, `[Explicit]`, inconclusive/assumption skip, seed/sample/attempt 축소는 금지한다.

```text
Repaired Map04ExitTests 110/110 PASS
Existing MAP04 focused 1454/1454 PASS
MAP04 phase actually executed >=1564 PASS
failed/skipped 0/0
Game.Map discovery >=5359
Full EditMode discovery >=5467
forced compile / Console errors / relevant new warnings = 0/0/0
```

MAP04_10 transient fixture를 현재 Editor에서 재생성하고 Game/Scene visual checklist `18/18` each를 다시 수행한다. stale Result/capture만 인용할 수 없다.

Asset/scope gate:

```text
Assets meta 3148 -> 3148
modified existing test C# 1; new test/meta 0/0
test meta SHA/GUID unchanged
production/other existing tests/unexpected Assets changes 0/0/0
Authoring CSV/meta 50/50; duplicate GUID delta 0
generated file, Scene dirty/residue, source mutation 0
```

Unity/Test Runner/visual gate를 실제 실행하지 못하면 `BLOCKED`. test 실패를 production 변경, relaxed invariant, hidden retry 또는 excluded seed로 우회하지 않는다.

## Result / Finalize

현재 Result를 `<=180 lines`로 교체한다. 적용/SHA, 이전 실패, repaired source SHA, disposition counts/histogram, determinism, known vector, test/discovery/visual, compile/meta/scope, findings, exit decision, NEXT만 기록한다.

PASS Result exact lines:

```text
STATUS: PASS
MAP04 EXIT: APPROVED
MAP05 ENTRY: ELIGIBLE FOR SEPARATE PATCH
MAP05_01_BUILD_MANDATORY_TERMINALS: LOCKED / DO NOT START
```

모든 gate가 PASS일 때만 MAP04_11을 COMPLETE, Current Task NONE, Last Completed/Result를 MAP04_11로 finalize한다. MAP05_01은 LOCKED로 유지하며 별도 patch 없이는 생성/시작하지 않는다.
