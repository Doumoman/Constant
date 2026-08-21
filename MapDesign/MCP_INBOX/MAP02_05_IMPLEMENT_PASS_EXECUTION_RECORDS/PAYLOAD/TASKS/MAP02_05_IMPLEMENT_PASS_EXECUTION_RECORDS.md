# MAP02_05 — Implement Pass Execution Records

```yaml
status_control:
  task_key: MAP02_05_IMPLEMENT_PASS_EXECUTION_RECORDS
  result_file: REPORTS/MAP02_05_IMPLEMENT_PASS_EXECUTION_RECORDS_RESULT.md
```

## Objective

`WorldGenerationRoot` 실행을 immutable execution records로 관찰 가능하게 만든다. root run에는 world/profile/seed/UTC 시작/monotonic 소요/최종 상태를 기록하고, 각 실제 pass attempt에는 pass ID, attempt ordinal, retry scope, UTC 시작, monotonic 소요, success/failure code·message를 기록한다. pass별 집계는 retry count와 최종 실패 원인을 정확히 보존한다.

시간·진단 기록은 artifact/RNG/retry/성공 판정의 입력이 아니며 같은 profile/seed의 generated artifacts를 바꾸지 않는다. 이 Task는 메모리 내 immutable records까지만 구현하고 CSV/manifest/file I/O는 MAP02_06으로 남긴다.

## Mandatory Read / Scope

entrypoint → locked/work/CSV/Unity/change/patch/finalize rules → Master → Status → 이 Task → MAP02_04 PASS Result 순서로 읽는다.

Map Package v1.0 exact path가 installed tree에 있으면 아래 부분만 읽는다.

```text
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md  # pass/retry ownership only
01_FIXED_SPEC/04_RUNTIME_ARCHITECTURE.md                # root seed recording only
02_PHASE_ROADMAP/MAP02_TOPOLOGY_GRAYBOX.md              # pass start/duration/retry requirement only
03_CSV_SCHEMA/CSV_DATA_DICTIONARY.csv                   # seed_manifest timing/retry rows only
05_GENERATED_OUTPUT_SCHEMA/seed_manifest.csv            # header only
```

exact 문서가 installed tree에 없으면 이 Task의 frozen contract를 authoritative fallback으로 사용한다. 대체 문서를 broad search하거나 Legacy/다른 generator를 읽지 않는다.

기존 public API 확인은 아래 exact files로 제한한다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/Data/WorldGenerationDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/WorldRouteDefinitionSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/StaticDataRegistry.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationFailurePolicy.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationArtifactStore.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationPassContracts.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationPassRegistry.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GridInitializationPassAdapter.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationRootResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationRoot.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/WorldGenerationRootTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/GridInitializationPassTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/DeterministicRngStreamTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

approved Runtime/test `Generation` 폴더의 `rg --files` path-only inventory는 허용한다. content search는 위 exact files로만 한정하고 다른 file match body를 출력하는 broad recursive search를 금지한다. MAP02_06 이후 Task body, Legacy/Stage/P6/P11 generator, unrelated CSV rows, Scene/Prefab YAML은 읽거나 사용하지 않는다.

## WRITE ALLOWLIST

Runtime C# 신규 5:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationClock.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationAttemptRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationPassExecutionRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationExecutionRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationExecutionResult.cs
```

Runtime C# existing 수정 1:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationRoot.cs
```

EditMode test C# 신규 1:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/WorldGenerationExecutionRecordTests.cs
```

신규 C# 6 + matching `.cs.meta` 6, existing C# exact 1 수정, Result 1만 허용한다. 모두 existing approved `Generation` folders에 두며 새 directory/folder meta를 만들지 않는다. Runtime namespace `StarNight.Map.WorldGeneration.Generation`, existing `Game.Map.Runtime` / `Game.Map.Tests.EditMode` assembly를 재사용한다.

existing `WorldGenerationRoot.cs` 수정은 clock 주입, attempt/pass/root record capture, 새 execution result 반환에만 한정한다. MAP02_04의 plan validation, artifact transaction, failure policy, retry scope, issue code/order, existing `WorldGenerationRootResult`와 public behavior를 바꾸지 않는다. 다른 MAP00/01/MAP02 C#/tests/meta, accepted legacy Editor folder meta 6개, Authoring CSV/meta, asmdef, Scene/Prefab/Package/ProjectSettings 수정 금지. `UnityEditor` reference와 신규 asmdef/asmref를 만들지 않는다.

## Clock Contract

시간 source는 explicit interface로 주입한다.

```text
public interface IWorldGenerationClock
{
    DateTimeOffset GetUtcNow();
    long GetTimestamp();
    TimeSpan GetElapsedTime(long startTimestamp, long endTimestamp);
}
```

`SystemWorldGenerationClock`은 sealed singleton `Instance`를 제공하되 mutable state는 없다.

- UTC 시작은 `DateTimeOffset.UtcNow`와 동등한 offset zero 값이다.
- elapsed는 monotonic `Stopwatch` timestamp로 계산한다. wall clock 차이로 duration을 계산하지 않는다.
- clock을 생성 알고리즘, RNG seed/scope, pass input, artifact, retry 판단에 전달하지 않는다.
- injected clock 반환값은 UTC offset zero와 non-negative elapsed를 만족해야 한다. 위반은 programming/instrumentation contract error로 `InvalidOperationException`을 던지며 음수 duration을 clamp해 숨기지 않는다. production 2-argument Root가 사용하는 system clock에서는 이 오류가 발생하지 않아야 한다.
- production code에서 `DateTime.Now`, `DateTime.UtcNow`, `Environment.TickCount`, Unity time/frame API를 직접 사용하지 않는다.

테스트 clock은 focused test file 안에서만 구현한다. production에 fake/manual clock을 추가하지 않는다.

## Attempt Record Contract

`WorldGenerationAttemptRecord`는 sealed immutable object다.

```text
string PassId
int PassOrder
int AttemptOrdinal
string RetryScopeId
ulong WorldSeed
DateTimeOffset StartedUtc
long DurationMilliseconds
bool Succeeded
string FailureCode
string FailureMessage
string ReturnedRetryScopeId
```

- attempt 1회 실제 invocation마다 exact record 1개를 남긴다.
- `AttemptOrdinal`은 context와 같은 `0..N`, `PassOrder`는 definition exact 값이다.
- `WorldSeed`는 root input exact ulong이며 text/hash로 바꾸지 않는다.
- first attempt와 `RETRY_PASS`의 `RetryScopeId`는 empty; `RETRY_SCOPE` 후속 attempt는 context에 전달된 exact scope다.
- success는 failure fields와 returned retry scope가 모두 empty다.
- expected failure는 pass result의 exact stable failure code/message/returned scope를 기록한다.
- null result, thrown exception, output mismatch, ownership conflict처럼 Root가 만든 실패는 기존 stable issue code/message를 기록한다. stack/path/thread/GUID는 기록하지 않는다.
- `DurationMilliseconds`는 monotonic elapsed의 non-negative whole millisecond이며 truncation은 `TimeSpan.Ticks / TimeSpan.TicksPerMillisecond`로 exact 고정한다. floating-point/rounding을 사용하지 않는다.

plan prevalidation failure는 pass invocation이 아니므로 attempt record `0`이다.

## Pass Execution Record Contract

`WorldGenerationPassExecutionRecord`는 requested plan의 실제로 시작된 pass 1개당 exact 1개인 sealed immutable aggregate다.

```text
string PassId
string ClassName
int PassOrder
string FailurePolicyToken
ulong WorldSeed
DateTimeOffset StartedUtc
long DurationMilliseconds
IReadOnlyList<WorldGenerationAttemptRecord> Attempts
int AttemptCount
int RetryCount
bool Succeeded
bool Terminal
string FailureCode
string FailureMessage
string FinalRetryScopeId
```

- `Attempts`는 attempt ordinal 오름차순 copied read-only snapshot이다.
- `AttemptCount == Attempts.Count`, 최소 1이다.
- `RetryCount == AttemptCount - 1`; CSV의 max retry가 아니라 실제 additional attempts 수다.
- pass 시작은 first attempt 시작과 exact 같거나 earlier이며 duration은 last attempt 종료까지의 monotonic interval이다.
- success는 마지막 attempt success, failure fields empty, `Terminal=false`다.
- `REPORT_ONLY` failure는 `Succeeded=false`, `Terminal=false`; failure를 기록하고 root가 계속한다.
- terminal pass failure는 `Succeeded=false`, `Terminal=true`, final root issue의 exact code/message/scope를 가진다.
- retry 후 성공하면 실패했던 attempts는 보존하되 aggregate success fields는 empty다.
- plan validation에서 invocation되지 않은 pass는 record를 만들지 않는다.

## Root Execution Record Contract

`WorldGenerationExecutionRecord`는 root 호출 1회당 exact 1개인 sealed immutable aggregate다.

```text
string GenerationProfileId
string WorldProfileId
ulong WorldSeed
string InclusivePassId
DateTimeOffset StartedUtc
long DurationMilliseconds
IReadOnlyList<WorldGenerationPassExecutionRecord> Passes
int PassCount
int AttemptCount
int RetryCountTotal
bool Succeeded
string LastCompletedPassId
string FailurePassId
string FailureCode
string FailureMessage
```

- `InclusivePassId`는 full `Execute`에서 empty, `ExecuteThrough`에서는 caller exact target이다.
- known profile이면 exact referenced world profile ID를 기록한다. profile lookup 전 실패면 world profile empty다.
- `Passes`는 actual start order의 copied read-only snapshot이며 invocation되지 않은 pass가 없다.
- `AttemptCount`와 `RetryCountTotal`은 pass aggregates 합계다. `RetryCountTotal`은 initial attempts를 포함하지 않는다.
- root duration은 plan validation을 포함한 호출 전체의 monotonic interval이다.
- success failure fields empty; failure는 existing terminal issue exact pass/code/message를 투영한다.
- plan failure는 pass/attempt/retry 0, last-completed empty, stable failure fields를 가진다.
- `REPORT_ONLY` issue만 있고 requested plan이 끝까지 완료되면 root `Succeeded=true`, failure fields empty이며 pass record에 nonterminal failure가 남는다.
- record는 timestamp/duration을 제외한 `WorldGenerationRootResult`의 success, last completed, terminal failure와 모순될 수 없다.

모든 constructor는 null, invalid UTC offset, negative duration/count/order/ordinal, inconsistent success/failure/count relationship을 거부한다. caller collection/order를 copy하고 public setter/mutable collection을 노출하지 않는다.

## Execution Result / Compatibility

`WorldGenerationExecutionResult`는 기존 result와 새 execution record를 묶는다.

```text
WorldGenerationRootResult Result
WorldGenerationExecutionRecord ExecutionRecord
```

`WorldGenerationRoot`는 기존 API를 그대로 유지한다.

```text
WorldGenerationRootResult Execute(string generationProfileId, ulong worldSeed)
WorldGenerationRootResult ExecuteThrough(string generationProfileId, ulong worldSeed, string inclusivePassId)
```

그리고 explicit recorded API를 추가한다.

```text
WorldGenerationExecutionResult ExecuteRecorded(string generationProfileId, ulong worldSeed)
WorldGenerationExecutionResult ExecuteThroughRecorded(string generationProfileId, ulong worldSeed, string inclusivePassId)
```

- 기존 API는 recorded core를 호출하고 `.Result`만 반환한다. 기존 결과/issue/artifact contract를 exact 유지한다.
- constructor overload `WorldGenerationRoot(staticData, passRegistry, clock)`을 추가한다. 기존 2-argument constructor는 `SystemWorldGenerationClock.Instance`를 사용한다.
- 기존 `WorldGenerationRootResult`는 수정하지 않는다. 기록이 필요한 caller와 MAP02_06은 explicit recorded API의 `WorldGenerationExecutionResult.ExecutionRecord`를 사용한다.
- recorded wrapper는 non-null 기존 `Result`와 non-null 새 `ExecutionRecord`를 exact 한 쌍으로 보존한다.
- 기록을 얻기 위해 pass를 재실행하지 않는다. 모든 API의 pass invocation count는 MAP02_04와 같다.

## Exact Capture Boundaries

Root execution:

```text
root startedUtc/timestamp capture
plan validation
for each actually started pass:
  pass startedUtc/timestamp capture
  for each actually invoked attempt:
    attempt startedUtc/timestamp capture
    pass.Execute(context) exactly once
    attempt end timestamp capture
    attempt record append
  pass end timestamp capture
  pass record append
root end timestamp capture
execution record + existing root result bind
```

- clock access count/order는 deterministic test vector로 검증한다.
- clock exception/invalid UTC/negative elapsed는 injected instrumentation contract error로 즉시 전파한다. 이를 pass failure로 기록하거나 retry하지 않으며, 이미 호출한 pass를 기록 목적으로 재호출하지 않는다.

## Failure Cause Mapping

attempt/pass/root record는 MAP02_04의 기존 stable issue semantics를 재사용한다.

```text
PASS_FAILED
RETRY_EXHAUSTED
MISSING_RETRY_SCOPE
NULL_PASS_RESULT
UNHANDLED_PASS_EXCEPTION
OUTPUT_SET_MISMATCH
ARTIFACT_OWNERSHIP_CONFLICT
MISSING_INPUT_ARTIFACT
```

원 pass가 반환한 failure code/message는 attempt에 보존한다. aggregate/root terminal code는 기존 Root가 최종 선택한 exact issue code를 사용한다. retry exhaustion에서 attempt의 원 failure를 `RETRY_EXHAUSTED`로 덮어쓰지 않는다.

## Determinism Boundary

- same profile/seed/registry에서 system clock 값은 달라도 `Result.Succeeded`, artifacts bytes/hash, issues, last-completed, pass/attempt identity·count·failure cause는 같다.
- timestamps/durations만 non-deterministic diagnostic fields다. serializer/content hash/RNG seed/replay identity에 자동 포함하지 않는다.
- two different fake clock schedules에서도 generated grid topology/CSV bytes와 issue semantics가 exact 같다.
- execution record를 읽거나 enumeration해도 Root/pass/RNG state를 변경하지 않는다.
- reused Root 호출은 각각 독립 record를 만들고 이전 record collection을 mutate하지 않는다.

## Baseline / Meta Stability

MAP02_04 PASS 이후 baseline:

```text
WorldGenerationRoot focused: 84/84
MAP02_01/02/03 focused: 56/103/90
Targeted EditMode: 1200/1200
Full EditMode: 1220/1220
Authoring CSV/meta: 50/50
Assets meta: 2967
accepted legacy Editor folder meta: 6/6
duplicate GUID groups: 0
```

legacy folder meta 6개는 정상 baseline이며 삭제·재작성·신규 drift로 분류하지 않는다. 새 directory/folder meta expected `0`. 신규 matching meta 6개 반영 clean final Assets meta는 `2973`이다.

## DO NOT

- MAP02_04 artifact/retry/failure/plan semantics 변경 금지
- clock/timestamp/duration을 RNG, artifact, pass input, retry 또는 success 판정에 사용 금지
- static/global mutable current-record, singleton recorder state, event bus/service locator 금지
- stack trace, machine path, thread/process ID, Unity frame/time 기록 금지
- production fake clock, reflection/assembly scan, UnityEditor dependency 금지
- seed manifest/CSV/JSON/file/directory output 구현 금지
- replay/content hash/generator build/approval contract 선행 구현 금지
- overlay/Gizmo/EditorWindow/Scene·Game integration 금지
- exception swallow, negative duration clamp, test skip/ignore/assertion 완화 금지
- new directory/folder meta/asmdef/asmref, Authoring CSV/meta/Scene/Prefab/Package/ProjectSettings/Git 변경 금지

## Tests / Verification

Focused minimum 72 cases:

- clock interface/system clock UTC offset zero and non-negative monotonic elapsed
- record constructor null/range/UTC/count/state consistency and copied read-only collections
- successful grid prefix exact root/pass/attempt IDs, seed, order, count, retry 0, empty failures
- exact fake clock start/duration millisecond truncation and capture order
- plan prevalidation failure root record with pass/attempt/retry 0
- fake 10-pass success exact 10 pass/10 attempt/0 retry aggregate
- FAIL_WORLD first failure exact attempt/pass/root cause
- RETRY_PASS failure→success and exhaustion exact attempt ordinals/retry counts
- RETRY_SCOPE exact incoming/returned scope chain and final scope
- REPORT_ONLY nonterminal pass failure plus continued success root record
- null result/throw/output mismatch/ownership/missing input mapping
- instrumentation clock invalid UTC/negative elapsed rejection without clamp or re-invocation
- existing Execute/ExecuteThrough behavior and invocation count compatibility
- recorded wrapper의 non-null Result/ExecutionRecord linkage
- two clock schedules change only timing fields, artifacts/issues/counts unchanged
- same seed 100 runs, fresh/reused Root record isolation
- no static mutable recorder, no timing in RNG/artifact/hash, no file I/O
- existing MAP02_01 `56/56`, MAP02_02 `103/103`, MAP02_03 `90/90`, MAP02_04 `84/84` regressions
- accepted meta 6 unchanged, existing modification exact 1, new directory 0

```text
New pass execution records: >=72 PASS
MAP02_01 GeneratedWorldData: 56/56 PASS
MAP02_02 deterministic RNG streams: 103/103 PASS
MAP02_03 GridInitializationPass: 90/90 PASS
MAP02_04 WorldGenerationRoot: 84/84 PASS
MAP00 coordinate/architecture regression: PASS
MAP01 Registry/content/import regression: PASS
Previous targeted baseline: 1200/1200 PASS
Targeted total: >=1272 PASS
Full project EditMode: >=1292 PASS
Unity 6000.3.8f1 / force refresh / compile error 0 / relevant warning 0
PlayMode NOT RUN / Visual NOT APPLICABLE / Scene-Prefab changes NONE
```

Authoring CSV/meta `50/50` unchanged, accepted folder meta `6/6` unchanged, 신규 matching meta `6/6` valid, final Assets meta `2973`, project duplicate GUID `0`을 확인한다. Task marker 이후 final Assets 변경은 신규 C# 6 + matching meta 6 + existing allowlisted C# exact 1 = `13`, unexpected `0`이어야 한다. Unity evidence가 없거나 한 조건이라도 실패하면 `BLOCKED`.

## Result / Completion

Result: `REPORTS/MAP02_05_IMPLEMENT_PASS_EXECUTION_RECORDS_RESULT.md`.

Required sections: TASK, STATUS, SUMMARY, READ, MASTER BACKLOG CHECK, MAP02_04 GATE CHECK, CREATED, MODIFIED, PREEXISTING_IDENTICAL, CLOCK, ATTEMPT RECORD, PASS RECORD, ROOT RECORD, EXECUTION RESULT, FAILURE CAUSE, DETERMINISM BOUNDARY, TEST, UNITY, ASSET META VALIDATION, CHANGE SCOPE, OUT_OF_SCOPE_FINDINGS, DONE CONDITIONS, NEXT, Recommended Commit.

모든 계약과 회귀가 PASS일 때만 MAP02_05 COMPLETE, Current Task NONE으로 finalize한다. `MAP02_06_IMPLEMENT_SEED_MANIFEST_AND_REPLAY_RECORDER`는 LOCKED로 유지하고 자동 시작하지 마.

Recommended Commit: `feat(map): record world generation execution`
