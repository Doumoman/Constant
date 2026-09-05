```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP19_09_MAP19_SCALE_AND_EXIT_AUDIT
  task_file: TASKS/MAP19_09_MAP19_SCALE_AND_EXIT_AUDIT.md
  requires_current_task: NONE
  requires_completed_task: MAP19_08_CREATE_FAILURE_BUNDLE_AND_HEADLESS_RUNNER
  requires_result:
    path: REPORTS/MAP19_08_CREATE_FAILURE_BUNDLE_AND_HEADLESS_RUNNER_RESULT.md
    status: PASS
    sha256: 35d9e149fb174da911fe9271e26b8f59c48f4dac3920858e2f3e2e65da197009
  requires_installed_task:
    path: TASKS/MAP19_08_CREATE_FAILURE_BUNDLE_AND_HEADLESS_RUNNER.md
    sha256: efbb97b10bca089f99de68e41cb92be1ad149a5d2ce621bf83636c856122fb95
  sets_current_task: MAP19_09_MAP19_SCALE_AND_EXIT_AUDIT
```

# MAP19_09 - MAP19 Scale and Exit Audit

```text
TASK: MAP19_09_MAP19_SCALE_AND_EXIT_AUDIT
PHASE: MAP19 - Traversal Validation / Seed QA
STATUS: CURRENT
NEXT: MAP20_01_CREATE_GENERATOR_WINDOW_AND_SCOPED_ROLLBACK
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP19_02~08에서 만든 validation chain, measurement proof, failure bundle, headless runner를 사용해 MAP19 Phase를 닫는다.

이번 Task는 **planned MAP19 seed-scale audit**이다.  
이것은 기존 전체 회귀나 legacy `19347` regression이 아니다.

이번 Task의 책임:

```text
1. MAP19_08 runner/bundle handoff를 검증하고 scale audit entrypoint를 완성한다.
2. 1k -> 10k -> 100k seed tier를 순차 실행하되, 이전 tier가 PASS하지 않으면 다음 tier를 실행하지 않는다.
3. 각 tier에서 pass/fail/crash/retry/performance/failure owner를 기록한다.
4. 실패가 발생하면 최초 실패들을 MAP19_08 failure bundle 형식으로 저장하고 즉시 STOP한다.
5. 모든 tier가 통과할 때만 MAP19 exit digest와 MAP20_01 handoff digest를 게시한다.
6. Result 첫머리에 어떤 스크립트가 어떤 책임을 가졌는지 사용자용 표로 보고한다.
```

핵심 원칙:

```text
Scale audit은 기존 MAP19 validation chain을 소비한다.
Scale audit은 map generation 동작을 수정하지 않는다.
Scale audit은 실패를 고치지 않는다.
Scale audit은 실패를 재현 가능한 bundle로 남기고 멈춘다.
Scale audit은 legacy regression을 대신하거나 호출하지 않는다.
```

## 1. 사용자 보고 의무

Result 첫 부분에는 반드시 아래 두 섹션을 둔다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

`User-Facing Implementation Report`에는 아래 질문에 답한다.

```text
이번 작업이 어떤 기능을 추가했는가?
이번 작업이 검증한 seed 규모는 어디까지인가?
각 tier가 어떤 조건으로 다음 tier를 열었는가?
실패가 있었다면 어떤 bundle이 생성되었고 왜 멈췄는가?
실패가 없었다면 MAP19 exit으로 무엇을 보증하는가?
이번 작업에서 legacy regression을 돌리지 않았다는 증거는 무엇인가?
이번 작업에서 map generator 동작을 수정하지 않았다는 증거는 무엇인가?
```

`Responsibility and Added Scripts`는 표로 작성한다.

```text
| Script or file | Added or changed responsibility | Explicit non-ownership |
```

파일별 책임이 없으면 FAIL이다.

## 2. 선행조건

작업 시작 전에 다음을 확인한다.

```text
MapDesign/MCP/REPORTS/MAP19_08_CREATE_FAILURE_BUNDLE_AND_HEADLESS_RUNNER_RESULT.md exists
MAP19_08 Result STATUS: PASS
MAP19_08 Result SHA-256:
35d9e149fb174da911fe9271e26b8f59c48f4dac3920858e2f3e2e65da197009

MapDesign/MCP/TASKS/MAP19_08_CREATE_FAILURE_BUNDLE_AND_HEADLESS_RUNNER.md exists
MAP19_08 installed Task SHA-256:
efbb97b10bca089f99de68e41cb92be1ad149a5d2ce621bf83636c856122fb95

MAP19_08 handoff digest:
6cbc9ab8fa12eaf353d527a3455b76651a93e7ef07b2a3b72406b2309701b443

MAP20_01_CREATE_GENERATOR_WINDOW_AND_SCOPED_ROLLBACK: LOCKED
```

불일치 시:

```text
STATUS: BLOCKED
reason: precondition mismatch
created/changed code files: 0
MAP20_01 started: NO
STOP
```

## 3. 허용 범위

허용:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Validation/GeneratedScaleAudit*.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Validation/GeneratedScaleAudit*.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Validation/GeneratedHeadlessValidationRunner.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Validation/GeneratedScaleAudit*Tests.cs
MapDesign/MCP/TASKS/MAP19_09_MAP19_SCALE_AND_EXIT_AUDIT.md
MapDesign/MCP/REPORTS/MAP19_09_MAP19_SCALE_AND_EXIT_AUDIT_RESULT.md
MapDesign/MCP_ARCHIVE/MAP19_09_MAP19_SCALE_AND_EXIT_AUDIT.md
MapDesign/MCP/GENERATED/MAP19_09/**
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

조건부 허용:

```text
Existing MAP19_08 runner files may be extended only to expose actual MAP19_09 scale-audit entrypoints.
Existing MAP19 validation files may be read or called, but their proof logic must not be rewritten.
Generated output may be written only under MapDesign/MCP/GENERATED/MAP19_09.
```

금지:

```text
legacy 19347 regression
prior task category selection
PlayMode selection
unfiltered test run
full regression run
manual gameplay test
Scene / Prefab / Tilemap mutation
Tilemap.SetTile / SetTiles / SetTilesBlock / ClearAllTiles in production code
PlayerController / Rigidbody / Collider / Physics2D behavior change
actual movement tuning
new movement rule profile
MAP19_01 rule registry mutation
MAP19_02 movement graph logic rewrite
MAP19_03 completion search logic rewrite
MAP19_04 recovery/density proof rewrite
MAP19_05 repetition/event-removal proof rewrite
MAP19_06 worst-case proof rewrite
MAP19_07 measurement proof rewrite
MAP19_08 failure bundle schema rewrite
map generator solve algorithm behavior change
activity/event/special-region/content placement behavior change
runtime object instantiate/enable/disable/destroy for audit
real screenshot capture from camera
external non-Unity process launch
production seed approval
optimization rewrite or broad refactor
MAP20_01 start
```

## 4. 중복과 하드코딩 방지

이번 Task는 새 검증 표면을 만들 수 있지만 validation logic을 복사하지 않는다.

필수:

```text
Use existing MAP19_02~08 public surfaces wherever available.
Centralize tier definitions in one small data structure.
Centralize pass/fail counters in one summary model.
Centralize digest inputs in one canonical builder.
Do not duplicate hard-coded seed loops across tests and runner.
Do not duplicate failure owner/reason formatting.
Do not create a separate copy of movement graph, BFS, recovery, density, repetition, event-removal, or worst-case validation logic.
```

Result에 다음을 기록한다.

```text
duplicated validation logic count:
hard-coded tier loop copies:
hard-coded digest string copies outside precondition/constants:
```

목표값:

```text
duplicated validation logic count: 0
hard-coded tier loop copies: 0 or 1 centralized definition
```

## 5. Scale Tier 정책

Seed tier는 누적 범위가 아니라 추가 범위로 실행한다.

| Tier | Seed indexes | Count | Unlock condition |
|---|---:|---:|---|
| A | 0..999 | 1,000 | preconditions pass |
| B | 1000..9999 | 9,000 | Tier A pass |
| C | 10000..99999 | 90,000 | Tier B pass and runtime budget accepted |

Cumulative totals:

| After tier | Cumulative count |
|---|---:|
| A | 1,000 |
| B | 10,000 |
| C | 100,000 |

Tier PASS 조건:

```text
executed seeds == tier count
unexpected exceptions == 0
validation failures == 0
mandatory route failures == 0
completion failures == 0
recovery failures == 0
density failures == 0
repetition/removal failures == 0
worst-case failures == 0
deterministic replay mismatch count == 0
failure bundle write failures == 0
```

Tier FAIL 조건:

```text
Any validation failure or unexpected exception occurs.
Failure bundle is written for first failures.
Next tier is not started.
Result status is FAIL unless the failure is infrastructure-only.
```

Tier BLOCKED 조건:

```text
Missing precondition
Compile error
Runner cannot launch in the local environment
Output directory cannot be safely created
Runtime budget for Tier C is projected to exceed the local budget before C starts
```

Runtime budget policy:

```text
Tier A and Tier B are required for this Task.
Before Tier C, estimate runtime from Tier B.
If projected Tier C wall-clock time exceeds 30 minutes, do not run Tier C.
Record STATUS: BLOCKED_RUNTIME_BUDGET with Tier A/B evidence and STOP.
Do not silently downgrade the requirement to PASS.
```

## 6. Failure Bundle 정책

When a tier fails:

```text
Create failure bundles for the first 5 distinct failing seeds, or fewer if fewer failures exist.
Use the MAP19_08 failure bundle schema unchanged.
Write bundles under MapDesign/MCP/GENERATED/MAP19_09/failures/.
Include pass snapshot, failed rule, owner, reason, expected/actual, coordinates, upstream digests, seed, generator/data version, and replay command.
Do not capture real screenshots.
Use screenshot reference slots only.
Do not publish MAP20_01 handoff digest.
Do not run the next tier.
```

If failure bundle writing itself fails:

```text
STATUS: BLOCKED
reason: failure bundle write failed
atomic success digest: NONE
MAP20_01 handoff digest: NONE
STOP
```

## 7. MAP19 Exit Digest

Only after all required tiers pass, publish:

```text
MAP19 scale audit digest
MAP19 exit digest
MAP20_01 handoff digest
```

Digest inputs must include:

```text
MAP19_08 Result SHA
MAP19_08 installed Task SHA
MAP19_08 handoff digest
MAP19_08 failure bundle schema digest
MAP19_08 runner plan digest
MAP19_09 installed Task SHA
Scale tier definitions
Actual tier counts
Actual pass/fail/crash counters
Deterministic replay sample summary
Performance summary
Failure bundle count
Legacy regression counters
```

Digest must be lowercase SHA-256 and deterministic across repeat calculation.

## 8. Focused Tests

Add focused EditMode tests for the audit orchestration without running 100k inside NUnit.

Focused test category:

```text
MAP19_09
```

Required test names:

```text
ScaleAuditBuildsNonOverlappingOneKTenKHundredKTiers
ScaleAuditConsumesMap19_08HandoffAndRejectsDigestMismatch
ScaleAuditStopsBeforeNextTierWhenFailureAppears
ScaleAuditWritesFailureBundlesForFirstDistinctFailuresOnly
ScaleAuditPublishesNoMap20HandoffOnFailureOrBlockedRuntime
ScaleAuditSummaryDigestIsStableAcrossRepeatCultureAndWorkerOrder
ScaleAuditDoesNotSelectLegacyRegressionPriorCategoriesOrPlayMode
ScaleAuditReportsScriptResponsibilitiesAndNonOwnership
```

Required count:

```text
discovered: 8
executed: 8
passed: 8
failed: 0
```

## 9. 허용되는 실제 실행

Focused NUnit tests 외에 실제 staged seed audit 실행은 이번 Task에서 허용된다.

허용 실행:

```text
MAP19_09 scale audit Tier A: 1,000 seeds
MAP19_09 scale audit Tier B: 9,000 additional seeds
MAP19_09 scale audit Tier C: 90,000 additional seeds only if Tier B passes and runtime budget allows
```

금지 실행:

```text
legacy 19347 selections: 0
prior task test selections: 0
PlayMode selections: 0
unfiltered test selections: 0
full regression runs: 0
```

Result에는 focused tests와 staged seed audit을 분리해서 기록한다.

## 10. Result 필수 기록

Result 파일:

```text
MapDesign/MCP/REPORTS/MAP19_09_MAP19_SCALE_AND_EXIT_AUDIT_RESULT.md
```

Result에는 아래 섹션을 포함한다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
## MAP19 Scale Audit Summary
## Stage Gate Results
## Failure Bundle Output
## Focused Validation Summary
## No Legacy Regression Boundary Notes
## Final Status Evidence
```

`MAP19 Scale Audit Summary`에는 아래 값을 채운다.

```text
MAP19_08 Result SHA-256 required/actual:
MAP19_08 installed Task SHA-256 required/actual:
MAP19_08 handoff digest required/actual:
MAP19_08 failure bundle schema digest reused:
MAP19_08 runner plan digest reused:

scale audit schema version:
scale audit runner entrypoint:
scale audit output root:
seed tier definitions digest:
scale audit summary digest:
MAP19 exit digest lower-hex SHA-256:
MAP20_01 handoff digest lower-hex SHA-256 or NONE:

duplicated validation logic count:
hard-coded tier loop copies:
hard-coded digest string copies outside precondition/constants:

legacy 19347 selections:
prior task test selections:
PlayMode selections:
unfiltered test selections:
full regression runs:
```

`Stage Gate Results`에는 아래 표를 채운다.

```text
| Tier | Seed index range | Requested | Executed | Passed | Failed | Crashed | Started | Completed | Wall time | p50 ms | p95 ms | max ms | Next tier decision |
```

`Failure Bundle Output`에는 아래 값을 채운다.

```text
failure bundle schema version:
failure bundle count:
failure bundle output directory:
first failure seeds:
first failure owners:
first failure reasons:
failure bundle write failures:
real screenshot captures:
screenshot reference slots used:
MAP20_01 handoff published after failure:
```

`Focused Validation Summary`에는 아래 값을 채운다.

```text
Unity Version:
Compile Errors:
Relevant Warnings:
Relevant Console Errors:
EditMode category:
Discovered:
Executed:
Passed:
Failed:
Skipped:
Inconclusive:
PlayMode Tests:
Scene/Prefab Changes:
```

`No Legacy Regression Boundary Notes`에는 반드시 아래 블록을 포함한다.

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
FULL REGRESSION RUNS: 0
```

## 11. PASS 조건

PASS 조건:

```text
MAP19_08 Result and installed Task SHA match
MAP19_08 handoff digest matches
Focused MAP19_09 EditMode tests are 8/8 PASS
Tier A executes 1,000 seeds and passes
Tier B executes 9,000 additional seeds and cumulative 10,000 pass
Tier C executes 90,000 additional seeds and cumulative 100,000 pass
All validation failure counters are 0
All unexpected exception/crash counters are 0
Deterministic replay mismatch count is 0
MAP19 exit digest is published
MAP20_01 handoff digest is published only after all tiers pass
Legacy regression, prior categories, PlayMode, unfiltered tests, and full regression are all 0
Scene/Prefab/Tilemap/runtime behavior mutation counters are 0
Responsibility and Added Scripts table is present and specific
```

## 12. FAIL 조건

FAIL 조건:

```text
Any seed validation failure is found
Mandatory route/completion/recovery/density/repetition/event-removal/worst-case validation fails
Failure occurs but no failure bundle is produced
Next tier starts after a failed prior tier
MAP20_01 handoff digest is published after failure
Legacy 19347 or full regression is executed
PlayMode is executed
Map generator behavior is changed to make the audit pass
MAP20_01 starts
```

## 13. BLOCKED 조건

BLOCKED 조건:

```text
Precondition SHA mismatch
Compile error unrelated to this task that prevents focused validation
Runner cannot execute in the local environment
Output root safety check fails
Tier C projected runtime exceeds 30 minutes before C starts
Failure bundle writer itself cannot produce atomic output
```

When BLOCKED:

```text
Do not finalize MAP19_09
Do not start MAP20_01
Do not publish MAP20_01 handoff digest
Record exact owner, invariant, and minimum next action
STOP
```

## 14. Finalize 규칙

PASS일 때만:

```text
MAP19_09_MAP19_SCALE_AND_EXIT_AUDIT: COMPLETE
MAP20_01_CREATE_GENERATOR_WINDOW_AND_SCOPED_ROLLBACK: LOCKED
```

FAIL 또는 BLOCKED일 때:

```text
MAP19_09 remains CURRENT
MAP20_01 remains LOCKED
STOP
```

다음 Task는 내가 Result를 검수한 뒤 별도 MD로 준다.

## 15. Commit 규칙

PASS일 때만 atomic commit을 만든다.

Commit subject:

```text
MAP19_09 scale and exit audit
```

Commit body에는 다음을 포함한다.

```text
MAP19_09 responsibilities
focused test count
scale tier result summary
MAP19 exit digest
MAP20_01 handoff digest
legacy regression counters all zero
```

Git push는 하지 않는다.

## 16. STOP

Result 작성, 상태 finalize, commit 이후 반드시 멈춘다.

```text
DO NOT START MAP20_01.
DO NOT CREATE MAP20_01 FILES.
DO NOT RUN LEGACY REGRESSION.
STOP.
```
