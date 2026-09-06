```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP20_06_MAP20_TOOLING_EXIT_TESTS
  task_file: TASKS/MAP20_06_MAP20_TOOLING_EXIT_TESTS.md
  requires_current_task: NONE
  requires_completed_task: MAP20_05_IMPLEMENT_REPLAY_AUTHORING_INTEGRATION_HUD_AND_EXPORT
  requires_result:
    path: REPORTS/MAP20_05_IMPLEMENT_REPLAY_AUTHORING_INTEGRATION_HUD_AND_EXPORT_RESULT.md
    status: PASS
    sha256: b2d74b83ce7f700ed9c726403576d5087ecb82d80be9bcef29f9c9ad0481f6bf
  requires_installed_task:
    path: TASKS/MAP20_05_IMPLEMENT_REPLAY_AUTHORING_INTEGRATION_HUD_AND_EXPORT.md
    sha256: a8688dd6756ab7a750a6334ffbe60dafc5f50a5a7e0632b8e12a647dc445c459
  requires_handoff:
    name: MAP20_06 handoff digest
    sha256: 34d7e10a208547780d86c7aa985c4945d76956e8e9869a266d12a1cda27b13fb
  sets_current_task: MAP20_06_MAP20_TOOLING_EXIT_TESTS
```

# MAP20_06 - MAP20 Tooling Exit Tests

```text
TASK: MAP20_06_MAP20_TOOLING_EXIT_TESTS
PHASE: MAP20 - Tooling / Authoring Control Surface
STATUS: CURRENT
NEXT: MAP21_01_LOCK_MOONPALACE_PROFILES_AND_TILE_SHELL
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP20_01~05에서 만든 Editor/debug tooling을 **새 기능 추가 없이 출구 감사**한다.

이번 Task는 다음 여섯 가지를 승인한다.

| Exit area | 승인할 책임 | 명시적 비소유 |
|---|---|---|
| Coordinates | world/sector/cell/local/source 좌표가 서로 추적 가능한가 | 좌표 재계산 방식 변경 |
| Rollback scope | pattern/sector/1-ring/world rollback scope가 명시적이고 bounded인가 | rollback 실행, rollback logic 수정 |
| Source navigation | validation error id가 CSV file/row/column과 inspector target까지 이어지는가 | CSV 수정, validation 재실행 |
| Replay hash | replay request와 seed bundle digest가 deterministic이고 RequestOnly인가 | replay/generator/validator 실행 |
| HUD | runtime HUD state가 display-only이고 자동 wiring이 없는가 | Scene/Prefab/runtime object 변경 |
| 3-click access | 주요 tooling entry에서 원인 위치까지 3클릭 이내 접근 가능한가 | 새 UX 대개편, 기존 창 리팩터 |

이번 Task는 MAP20 phase exit audit이다. MAP21 콘텐츠 제작은 시작하지 않는다.

## 1. 사용자 보고 의무

Result 첫 부분에는 반드시 아래 두 섹션을 둔다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

`User-Facing Implementation Report`에는 아래 질문에 답한다.

```text
이번 audit이 무엇을 승인했는가?
좌표 추적은 어떤 산출물을 통해 확인했는가?
rollback scope는 왜 안전하다고 판단했는가?
source navigation과 replay hash는 어떻게 이어지는가?
HUD가 display-only라는 증거는 무엇인가?
3-click 접근성은 어떤 경로로 검증했는가?
이번 작업이 기존 MAP20_01~05 기능을 재실행하거나 수정하지 않았다는 증거는 무엇인가?
legacy regression을 돌리지 않았다는 증거는 무엇인가?
MAP21_01은 왜 아직 시작하지 않았는가?
```

`Responsibility and Added Scripts`는 반드시 표로 작성한다.

```text
| Script or file | Added or changed responsibility | Explicit non-ownership |
```

파일별 책임이 구체적이지 않으면 FAIL이다.

## 2. 선행조건

작업 시작 전에 다음을 확인한다.

```text
MapDesign/MCP/REPORTS/MAP20_05_IMPLEMENT_REPLAY_AUTHORING_INTEGRATION_HUD_AND_EXPORT_RESULT.md exists
MAP20_05 Result STATUS: PASS
MAP20_05 Result SHA-256:
b2d74b83ce7f700ed9c726403576d5087ecb82d80be9bcef29f9c9ad0481f6bf

MapDesign/MCP/TASKS/MAP20_05_IMPLEMENT_REPLAY_AUTHORING_INTEGRATION_HUD_AND_EXPORT.md exists
MAP20_05 installed Task SHA-256:
a8688dd6756ab7a750a6334ffbe60dafc5f50a5a7e0632b8e12a647dc445c459

MAP20_06 handoff digest:
34d7e10a208547780d86c7aa985c4945d76956e8e9869a266d12a1cda27b13fb

MAP21_01_LOCK_MOONPALACE_PROFILES_AND_TILE_SHELL: LOCKED
```

불일치 시:

```text
STATUS: BLOCKED
reason: precondition mismatch
created/changed code files: 0
MAP21_01 started: NO
STOP
```

## 3. 허용 범위

허용 파일:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Tooling/GeneratedToolingExitAudit.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Tooling/GeneratedToolingExitAuditPublisher.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Tooling/GeneratedToolingExitAuditTests.cs
MapDesign/MCP/GENERATED/MAP20_06/**
MapDesign/MCP/TASKS/MAP20_06_MAP20_TOOLING_EXIT_TESTS.md
MapDesign/MCP/REPORTS/MAP20_06_MAP20_TOOLING_EXIT_TESTS_RESULT.md
MapDesign/MCP_ARCHIVE/MAP20_06_MAP20_TOOLING_EXIT_TESTS.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

조건부 허용:

```text
MAP20_01~05 Result files may be read and hashed.
MAP20_01~05 generated JSON artifacts may be read and hashed.
MAP20_01~05 public tooling models may be referenced read-only.
The audit publisher may write only MAP20_06 generated audit JSON.
If an optional upstream artifact is absent, represent MissingData and fail only if a required exit invariant cannot be proven.
```

금지:

```text
editing MAP20_01~05 production code or tests
regenerating MAP20_01~05 sample artifacts
generator solve/reroll/execution
validation runner execution
actual replay execution
rollback execution
CSV authoring edit/save/import/export rewrite
auto-fix / auto-repair
Tilemap bake or Tilemap mutation
Scene / Prefab / ProjectSettings / Packages changes
runtime GameObject instantiate/enable/disable/destroy during tooling actions
production seed approval
external process launch
manual gameplay test
automatic MAP21_01 start
broad refactor or optimization rewrite
```

회귀 금지:

```text
MAP19_09 scale audit rerun
MAP20_01 generator run rerun
MAP20_02 overlay sample regeneration
MAP20_03 detail sample regeneration
MAP20_04 navigation sample regeneration
MAP20_05 replay/export sample regeneration
legacy 19347 regression
prior task category selection
PlayMode selection
unfiltered test run
full regression run
```

## 4. Exit Audit 계약

`GeneratedToolingExitAudit` must define immutable audit records only.

Required audit fields:

```text
schema_version
task_id
source_MAP20_05_result_digest
source_MAP20_05_task_digest
source_MAP20_06_handoff_digest
map20_result_chain
map20_generated_artifact_chain
coordinate_audit_records
rollback_scope_records
source_navigation_records
replay_hash_records
hud_state_records
access_path_records
exit_invariant_records
created_utc_excluded_from_canonical_digest
canonical_digest
```

Rules:

```text
All records are read-only snapshots.
All digests are lowercase SHA-256.
created_utc may be stored but is excluded from canonical digest.
Record order and JSON key order are deterministic.
No audit method may execute generator, validator, replay, rollback, or CSV write paths.
```

## 5. 승인해야 할 불변식

Required exit invariants:

```text
CoordinateTraceability
BoundedRollbackScope
SourceNavigationExactLocation
InspectorJumpTargetCoverage
ReplayRequestHashDeterminism
SeedBundleHashDeterminism
HudDisplayOnly
NoRuntimeOrSceneMutation
NoAuthoringCsvMutation
ThreeClickAccessCoverage
NoPriorArtifactRegeneration
NoLegacyRegressionSelection
```

각 invariant는 Result에 `PASS/FAIL/BLOCKED`, evidence source, digest, owner task를 기록한다.

### Coordinate Traceability

승인 기준:

```text
MAP20_01 seed/pass context -> MAP20_02 sector/cell -> MAP20_03 detail tab/record -> MAP20_04 CSV source location -> MAP20_05 replay/HUD/seed bundle로 좌표가 이어진다.
world/sector/cell/local/source 좌표가 MissingData 없이 증명되거나, MissingData가 명시적으로 격리된다.
```

### Bounded Rollback Scope

승인 기준:

```text
rollback scope tokens are explicit and bounded.
rollback dry-run/request data does not execute rollback.
No scope can silently expand to whole project or unrelated assets.
```

### Source Navigation and Replay Hash

승인 기준:

```text
validation error id resolves to exact CSV file/row/column/field/record when source exists.
Tile / Pattern / Cluster / Socket / Slot jump targets are covered.
Replay request remains RequestOnly.
Replay request, HUD, seed bundle, and digest manifest are deterministic across repeat/culture/input-order checks.
```

### HUD and 3-Click Access

승인 기준:

```text
HUD state has zero auto-spawn, zero runtime mutation, zero Scene/Prefab wiring.
Each primary investigation path is reachable within 3 user actions from a Tools/MapDesign menu entry or already-open MAP20 window.
```

Required access path records:

```text
GeneratorWindowToPassArtifact
GeneratorWindowToRollbackScopePreview
WorldOverlayToSectorCell
SectorCanvasToDetailInspector
ValidationErrorToCsvSource
ValidationErrorToInspectorTarget
FailureBrowserToReplayRequest
ReplayRequestToSeedBundle
HudPreviewToSelectedFailureOrValidation
SeedBundleToDigestManifest
```

Each access path record stores:

```text
access_path_id
entry_surface
target_surface
user_action_count
actions
owner_task
evidence_digest
passes_three_click_limit
```

## 6. Snapshot / Digest 계약

Required generated artifacts:

```text
MapDesign/MCP/GENERATED/MAP20_06/map20_tooling_exit_audit.json
MapDesign/MCP/GENERATED/MAP20_06/map20_access_path_audit.json
MapDesign/MCP/GENERATED/MAP20_06/map20_digest_chain_manifest.json
```

Digest manifest required fields:

```text
schema_version
task_id
source_MAP20_05_result_digest
source_MAP20_05_task_digest
MAP20_06_handoff_digest
tooling_exit_audit_digest
access_path_audit_digest
map20_phase_exit_digest
MAP21_01_handoff_digest
created_utc_excluded_from_canonical_digest
canonical_digest
```

## 7. 중복과 하드코딩 방지

Result에 아래 카운터를 기록하고 모두 0이어야 한다.

```text
MAP20_01_05 production C# modified:
MAP20_01_05 test C# modified:
duplicated generator logic count:
duplicated validation logic count:
duplicated rollback logic count:
duplicated replay execution logic count:
duplicated CSV parser/export logic count:
hard-coded source path copies outside sample/precondition constants:
hard-coded digest string copies outside precondition/constants:
```

## 8. Focused Tests

Focused test category:

```text
MAP20_06
```

Required test names:

```text
ToolingExitAuditLoadsMap20_01Through05ArtifactsWithoutRegenerating
CoordinateTraceabilityConnectsWorldSectorCellSourceReplayAndHud
RollbackScopeRecordsAreExplicitBoundedAndNeverExecuted
SourceNavigationPreservesExactCsvLocationAndInspectorTargets
ReplayAndSeedBundleHashesAreRequestOnlyAndDeterministic
RuntimeHudExitProofShowsDisplayOnlyAndNoWiring
ThreeClickAccessPathsReachAllPrimaryToolingSurfaces
ToolingExitAuditRejectsUnsafeRunFixApproveActions
DigestChainMatchesMap20_05PreconditionsAndPublishesMap21_01Handoff
ToolingExitAuditSerializesDeterministicallyAcrossRepeatCultureAndInputOrder
ToolingExitAuditRecordsAllExitInvariantsWithOwnerAndEvidence
ToolingExitAuditDoesNotSelectLegacyRegressionPriorCategoriesPlayModeOrFullRegression
```

Required count:

```text
discovered: 12
executed: 12
passed: 12
failed: 0
```

Focused validation may create sample artifacts only under `MapDesign/MCP/GENERATED/MAP20_06`.

## 9. 회귀 금지 정책

이번 Task의 검증은 focused MAP20_06 EditMode selection과 compile/console check까지만 허용한다.

문제가 실제로 발생하지 않은 상태에서 더 넓은 검증을 돌리지 않는다. 실제 문제가 발생해 더 넓은 검증이 필요하면 조용히 실행하지 말고, Result에 trigger owner, broken invariant, focused proof insufficiency, requested wider verification을 기록하고 STOP한다.

문제가 없다면 Result에 반드시 아래 블록을 포함한다.

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
FULL REGRESSION RUNS: 0
MAP19_09 SCALE AUDIT RERUNS: 0
MAP20_01 GENERATOR RUN RERUNS: 0
MAP20_02 OVERLAY SAMPLE REGENERATION RUNS: 0
MAP20_03 DETAIL SAMPLE REGENERATION RUNS: 0
MAP20_04 NAVIGATION SAMPLE REGENERATION RUNS: 0
MAP20_05 REPLAY EXPORT SAMPLE REGENERATION RUNS: 0
VALIDATION RUNNER EXECUTIONS: 0
REPLAY EXECUTIONS: 0
GENERATOR EXECUTIONS: 0
ROLLBACK EXECUTIONS: 0
CSV AUTHORING WRITES: 0
RUNTIME OBJECT SPAWNS: 0
SCENE PREFAB CHANGES: 0
```

## 10. Result 필수 기록

Result 파일:

```text
MapDesign/MCP/REPORTS/MAP20_06_MAP20_TOOLING_EXIT_TESTS_RESULT.md
```

Required sections:

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
## MAP20 Exit Audit Summary
## Coordinate Rollback and Navigation Summary
## Replay HUD and Access Summary
## Snapshot and Digest Summary
## Focused Validation Summary
## No Legacy Regression Boundary Notes
## Final Status Evidence
```

Required summary fields:

```text
MAP20_05 Result SHA-256 required/actual:
MAP20_05 installed Task SHA-256 required/actual:
MAP20_06 handoff digest required/actual:
MAP20_01_05 result files read:
MAP20_01_05 generated artifact roots read:
MAP20_01_05 regenerated artifact roots:
exit invariants required/passed/failed/blocked:
MAP20 phase exit approved:

coordinate trace records:
rollback scope records:
source navigation records:
inspector jump target kinds covered:
replay hash records:
HUD state records:
3-click access records:
max user action count:
unsafe action buttons allowed:

sample artifacts created:
tooling exit audit digest lower-hex SHA-256:
access path audit digest lower-hex SHA-256:
digest chain manifest lower-hex SHA-256:
MAP20 phase exit digest lower-hex SHA-256:
MAP21_01 handoff digest lower-hex SHA-256:

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

## 11. PASS / FAIL / BLOCKED

PASS 조건:

```text
Precondition SHA and handoff digest match
All 12 exit invariants are PASS
MAP20_01~05 outputs are read-only and not regenerated
Coordinate traceability reaches world/sector/cell/source/replay/HUD or explicit MissingData
Rollback scope is explicit, bounded, and never executed
CSV source navigation and five inspector target kinds are covered
Replay request and seed bundle hashes are deterministic and RequestOnly
HUD has zero auto-spawn, runtime mutation, and Scene/Prefab wiring
All required access paths are <= 3 user actions
No unsafe Run/Fix/Approve execution path is added
Focused MAP20_06 EditMode tests are 12/12 PASS
All no-regression and no-execution counters are zero
Responsibility and Added Scripts table is present and specific
MAP20 phase exit is approved
MAP21_01 remains LOCKED and not started
```

FAIL 조건:

```text
Any exit invariant fails
MAP20_01~05 artifacts are regenerated
Rollback, replay, generator, or validator is executed
CSV authoring file is written
Scene/Prefab/Tilemap/ProjectSettings/Packages are changed
Runtime objects are spawned, enabled, disabled, or destroyed
Any required access path exceeds 3 user actions
Unsafe Run/Fix/Approve action is exposed as executable
Any forbidden prior task rerun, PlayMode, legacy regression, unfiltered run, or full regression is executed
MAP21_01 starts
```

BLOCKED 조건:

```text
Precondition SHA mismatch
Compile error unrelated to this task that prevents focused validation
Required MAP20_01~05 artifact needed for an exit invariant is absent and cannot be represented as MissingData
Existing tooling cannot prove 3-click access without editing prior MAP20 windows
Existing HUD/replay/source data cannot prove display-only or RequestOnly safety
```

When BLOCKED:

```text
Do not finalize MAP20_06
Do not approve MAP20 phase exit
Do not start MAP21_01
Do not publish MAP21_01 handoff digest
Record exact owner, invariant, and minimum next action
STOP
```

## 12. Finalize / Commit / STOP

PASS일 때만:

```text
MAP20_06_MAP20_TOOLING_EXIT_TESTS: COMPLETE
MAP20 PHASE EXIT: APPROVED
MAP21_01_LOCK_MOONPALACE_PROFILES_AND_TILE_SHELL: LOCKED
```

FAIL 또는 BLOCKED일 때:

```text
MAP20_06 remains CURRENT
MAP20 PHASE EXIT is not approved
MAP21_01 remains LOCKED
STOP
```

PASS일 때만 atomic commit을 만든다.

Commit subject:

```text
MAP20_06 approve tooling exit tests
```

Commit body에는 다음을 포함한다.

```text
MAP20_06 responsibilities
focused test count
exit invariant counts
coordinate/rollback/source/replay/HUD/access evidence counts
MAP20_01~05 regeneration counters all zero
MAP20 phase exit digest
MAP21_01 handoff digest
legacy regression counters all zero
CSV authoring writes zero
```

Git push는 하지 않는다.

Result 작성, 상태 finalize, commit 이후 반드시 멈춘다.

```text
DO NOT START MAP21_01.
DO NOT CREATE MAP21_01 FILES.
DO NOT RERUN MAP19_09 SCALE AUDIT.
DO NOT RERUN MAP20_01 GENERATOR RUN.
DO NOT REGENERATE MAP20_02 OVERLAY SAMPLE.
DO NOT REGENERATE MAP20_03 DETAIL SAMPLE.
DO NOT REGENERATE MAP20_04 NAVIGATION SAMPLE.
DO NOT REGENERATE MAP20_05 REPLAY EXPORT SAMPLE.
DO NOT RUN VALIDATION RUNNER.
DO NOT EXECUTE REPLAY.
DO NOT EXECUTE GENERATOR.
DO NOT EXECUTE ROLLBACK.
DO NOT WRITE CSV AUTHORING FILES.
DO NOT MUTATE SCENE PREFAB TILEMAP OR RUNTIME OBJECTS.
DO NOT RUN LEGACY REGRESSION.
STOP.
```
