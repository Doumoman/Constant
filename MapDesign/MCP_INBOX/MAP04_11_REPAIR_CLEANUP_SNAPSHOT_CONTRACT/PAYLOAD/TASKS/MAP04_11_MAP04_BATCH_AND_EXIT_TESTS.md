# MAP04_11 — MAP04 Batch and Exit Tests (Repair v1.2)

```yaml
status_control:
  task_key: MAP04_11_MAP04_BATCH_AND_EXIT_TESTS
  result_file: REPORTS/MAP04_11_MAP04_BATCH_AND_EXIT_TESTS_RESULT.md
  repair_contract: CLEANUP_SNAPSHOT_PRODUCER_CONSUMER_CONFORMANCE
```

## Goal

1,000-world batch에서 확인된 44개 `PatchCleanup / InvalidSourceSnapshot`의 첫 계약 위반을 재현하고, MAP04_06 producer 또는 MAP04_07 consumer 중 실제 소유자를 최소 수정한다. public Invalid를 handoff/retry로 재분류하지 않고 `Completed + PassSiteHandoffRequired = 1000`, `Invalid = 0`을 달성한다.

MAP05 production/Task, 새 batch/root adapter, generated writer, CSV 변경은 범위 밖이다.

## Preconditions

control → Master/Status → 이 Task → 현재 FAIL Result → MAP04_06/07 Task·Result → exact relevant checked-in APIs/tests 순서로 읽는다.

```text
Current Task SHA before repair:
d3ba74fd176a07b6d010013806bf229891fd6b132d88861ab09a8b55b41d9733

Current FAIL Result SHA:
8bfd2a9132e9a97e755c5e35c31e544e89a8f679e20b0a515801baa9818719bc

Current Map04ExitTests SHA:
38d7b5ba20b9d65f71bf284a4b7b19d39bf47777b385b3c3113e4517c0f6409a

Test meta SHA / GUID:
8d01540bc77905fcfc0169efab907c34e3ece8dcef285539d812b2a0292dcb15
b609b3b1f6af4aa3979c42583f08974f

Run: 109 PASS / 1 FAIL
Batch: 1000 processed = 5 Completed + 951 Handoff + 44 Invalid
Attempts/retries: 97640/97635
Batch SHA: a13e9e16d42ac47ff041b339a64195f883bec232c753486cf9997df085d5d7f2
Assets meta: 3148
```

Current Task/Result가 다르면 `BLOCKED`. MAP05_01 이후 Task body는 읽거나 시작하지 않는다.

## Exact Read Allowlist

- current `Map04ExitTests.cs`와 matching meta
- MAP04_06 `IntrusionPlacer`, error/record/diagnostics/publication/result 및 tests
- MAP04_07 `PatchCleanup`, error/record/diagnostics/publication/result 및 tests
- `BiomePatchSnapshot`과 직접 구성 value types, P01 snapshot, biome/patch definitions
- MAP04_05 successful result/publication/diagnostics와 public rule contracts
- matching meta, approved Generation path-only inventory, asmdef, scope/meta/hash counts

unrelated C#, installed CSV body, Legacy/Stage/P6/P11, Scene/Prefab YAML, future Task는 금지한다.

## Conditional Write Allowlist

항상 허용:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map04ExitTests.cs
MapDesign/MCP/REPORTS/MAP04_11_MAP04_BATCH_AND_EXIT_TESTS_RESULT.md
```

producer fault가 증명된 경우에만:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/IntrusionPlacer.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/IntrusionPlacerTests.cs
```

consumer fault가 증명된 경우에만:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/PatchCleanup.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/PatchCleanupTests.cs
```

두 production branch를 함께 수정하려면 서로 독립된 두 root cause의 exact witness가 필요하다. 추측성 동시 수정은 `BLOCKED`. 위 파일의 matching meta는 읽기 전용이며 SHA/GUID를 보존한다. public model/API shape, 다른 production/test, Master, CSV, asmdef, Scene, Prefab, Packages, ProjectSettings는 수정하지 않는다.

## Phase 1 — 44-Case Failure Ledger

현재 test helper를 사용해 seeds `0..999`, attempts `0..99`를 동일 순서로 재실행한다. 44 Invalid마다 다음 canonical row를 Result용 in-memory aggregate에 남긴다.

```text
world seed / attempt ordinal
Intrusion completed status and source/output digest
PatchCleanup ordered error code + identifier/field/value if public data exposes it
first violated invariant from the conformance matrix
patch role/biome/rule/id and sector index when applicable
RNG draw count / mutation digest / publication presence
```

exception text, stack path, time/thread는 identity에 넣지 않는다. 동일 입력의 fresh/reused/reverse/culture run에서 ledger digest가 같아야 한다. 기존처럼 error code만 연결해 원인을 잃는 것은 금지한다.

## Producer/Consumer Conformance Matrix

`IntrusionPlacer Completed` output이 cleanup input으로 유효하려면 다음을 모두 만족한다.

- same world seed/P01 identity; exact 169 unique row-major ownership rows
- assigned/unassigned `165/4`; every reserved sector unowned
- ownership↔patch membership bidirectional; overlap/orphan `0`
- Core/Satellite patch는 own rule min/max/hard-59, connected, own seed/binding 보존
- Intrusion patch는 exact one sector, matching Intrusion rule with `AllowSingleSector`, exact one seed, null site/binding
- biome normal `MinPatchCount/MaxPatchCount`는 Core/Satellite만 계수; Intrusion role은 별도 계수
- all seed/binding sector ownership correct; site misownership `0`
- Intrusion host transfer가 donor minimum/connectivity, edge/reservation/seed/binding, allowed pair, distance/share caps를 보존
- source growth/intrusion objects와 definitions mutation `0`; completed publication non-null

Cleanup은 이 matrix를 만족하는 모든 completed intrusion snapshot을 받아야 한다. viable seed `0x0123456789ABCDF9/24`, patch count `17`, role count `4/10/3`만을 일반 입력 조건으로 고정할 수 없다.

## Root Owner Decision

### Producer fault

cleanup 호출 전 matrix가 이미 깨졌다면 `IntrusionPlacer`가 소유자다.

- candidate simulation과 final atomic pre-publish gate에서 해당 invariant를 보장한다.
- full desired placement를 유효하게 publish할 수 없으면 기존 public `RetryRequired` branch로 rollback한다.
- partial publication/records, source mutation, local repair는 `0`이다.
- 기존 successful input의 candidate ordering/RNG draw schedule과 known viable vectors는 변경하지 않는다.

### Consumer fault

matrix는 모두 PASS인데 cleanup이 거부하면 `PatchCleanup`이 소유자다.

- viable fixture 전용 patch/role/count 가정 또는 Intrusion을 normal count에 합산한 검사를 제거한다.
- matrix와 frozen protection/cleanup transfer gate는 유지한다.
- malformed synthetic snapshot은 계속 `InvalidInput / InvalidSourceSnapshot`; 이를 RetryRequired로 완화하지 않는다.
- cleanup action ordering, score, no-op, RNG `0` 계약은 변경하지 않는다.

### Forbidden outcome

exit test에서 `InvalidSourceSnapshot`을 handoff로 바꾸기, seed 제외, error 무시, 44건 golden allowlist, attempt `100`, invariant 완화는 금지한다.

## Repair Regression

증명된 owner test file에 actual NUnit case 정확히 `+1`을 추가한다. 44개 witness를 44 parameterized cases로 만들지 말고 한 streaming conformance case에서 전부 검사한다.

이 case는 최소 다음을 증명한다.

- original 44 witness가 repair 후 cleanup Invalid `0`
- producer fault이면 invalid publication `0`이며 Completed 또는 atomic RetryRequired
- consumer fault이면 contract-valid snapshots cleanup accepted/retry only, Invalid `0`
- malformed snapshots는 여전히 deterministic Invalid
- fresh/reused/reverse/culture digest 동일, source mutation/file output `0`

`Map04ExitTests` actual cases는 exact `110`을 유지하며 batch disposition assertion을 바꾸지 않는다.

## Frozen Gates

authoritative seeds와 retry policy:

```text
world seeds 0..999; attempt ordinal 0..99
Completed + PassSiteHandoffRequired = 1000
Invalid + Unclassified + Lost = 0
PatchCleanup:InvalidSourceSnapshot = 0
```

Completed worlds는 기존 ownership/validator `15/15`/CSV/overlay invariant를 모두 통과한다. Handoff는 exact 100 allowed RetryRequired attempts, no publication/mutation을 증명한다. 102-seed disposition/ordered digest와 known viable frozen vector는 그대로 유지한다.

known viable:

```text
seed 0x0123456789ABCDF9 / attempt 24
patches 17 = 4/10/3; assigned/unassigned 165/4
bytes 1956/16380; rules 15/15; RNG 1912
patch SHA 7ccf1fc1e6ebd298cc97bed3914395170fc38fe85b2d2392c80c9f30ec000543
world SHA 07daa96fe5f6ea985aa9e32aa0609d65b95c620a0b05a99426d3093275f8ee1d
```

## Required Runs

```text
Map04ExitTests exact 110/110 PASS
Original MAP04 focused baseline 1454/1454 PASS
Repair-specific owner case +1 PASS
MAP04 phase actually executed >=1565 PASS
failed/skipped 0/0
Game.Map discovery >=5360
Full EditMode discovery >=5468
Game/Scene visual checklist 18/18 each
forced compile / Console errors / relevant new warnings = 0/0/0
```

`[Ignore]`, `[Explicit]`, inconclusive/assumption skip, sample/attempt 축소, stale visual 인용은 금지한다. 실제 Unity/Test Runner/visual gate를 완료하지 못하면 `BLOCKED`.

Asset gate:

```text
Assets meta 3148 -> 3148
new C#/meta 0/0; all touched meta SHA/GUID unchanged
existing C# changes = Map04ExitTests + exact proven owner production/test only
other production/tests/unexpected Assets changes 0/0/0
Authoring CSV/meta 50/50; duplicate GUID delta 0
Scene/Prefab/Packages/ProjectSettings/generated files 0
```

## Result / Finalize

Result `<=180 lines`: STATUS, apply/SHA, prior failure, 44-case root-cause ledger aggregate, modified paths+hashes, repaired batch dispositions/histogram/SHA, determinism/known vectors, tests/discovery/visual, compile/meta/scope, exit decision, NEXT만 기록한다.

PASS exact lines:

```text
STATUS: PASS
MAP04 EXIT: APPROVED
MAP05 ENTRY: ELIGIBLE FOR SEPARATE PATCH
MAP05_01_BUILD_MANDATORY_TERMINALS: LOCKED / DO NOT START
```

모든 gate가 PASS일 때만 MAP04_11을 COMPLETE, Current Task NONE, Last Completed/Result를 MAP04_11로 finalize한다. MAP05_01은 LOCKED로 유지하며 별도 patch 없이는 생성/시작하지 않는다.
