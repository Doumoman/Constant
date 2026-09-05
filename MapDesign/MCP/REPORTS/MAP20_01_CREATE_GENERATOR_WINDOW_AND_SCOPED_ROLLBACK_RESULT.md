TASK: MAP20_01_CREATE_GENERATOR_WINDOW_AND_SCOPED_ROLLBACK
STATUS: PASS

## User-Facing Implementation Report

Unity 메뉴 `Tools/MapDesign/Generated Terrain Generator`에서 여는 Generated Terrain Generator Window를 추가했다. Window를 여는 동작은 coordinator를 준비하고 현재 상태만 표시하며, generation run을 시작하지 않는다. 사용자는 seed, `Pattern` / `Sector` / `OneRing` / `World` scope, pattern id 또는 sector 좌표, World 명시 확인을 입력·선택할 수 있고 generator/data version, input hash, MAP19 exit hash, 마지막 pass/fail 상태, artifact hash, failure owner/reason/replay reference를 확인할 수 있다. Run, Dry-run plan, Rollback, Open output folder, Copy replay reference controls를 제공한다.

`Pattern`은 한 pattern artifact directory, `Sector`는 한 sector output directory, `OneRing`은 MAP15와 같은 center + in-bounds Moore radius 1의 최대 9개 sector directory, `World`는 해당 run id의 world output root까지만 실행·rollback 대상으로 삼는다. World는 명시 확인 없이는 실행할 수 없으며 이 확인은 production seed approval이 아니다.

모든 유효 run은 seed, scope, generator/data version, input/output digest, pass/failure, replay/failure reference, rollback 상태와 lower-hex SHA-256 canonical digest를 기록한다. `created_utc`는 표시용으로 저장하되 canonical digest에서 제외한다. 실행 전에 선택 scope의 generated files와 content hashes를 snapshot manifest 및 run-local backup에 저장한다. `Assets`, Scene, Prefab, ScriptableObject, source CSV는 output root 안전성 검사상 snapshot/restore 대상이 될 수 없다. 실패 후 output mutation이 있었다면 자동 rollback하고, 성공 후에는 사용자가 수동 rollback할 수 있다. 복원 전 backup digest를 검증하고, 복원 실패 시 guard copy로 원상태를 되돌려 성공 digest를 게시하지 않는다.

Window에는 generator solve, pattern render, cluster/activity/event/special/population, movement, physics, MAP19 proof 로직을 넣지 않았다. Coordinator는 기존 또는 이후 adapter가 준비한 in-memory output mutation plan의 안전한 적용만 소유하며, Window의 기본 실행은 zero-mutation orchestration/dry-run artifact를 만든다. MAP19_08 failure bundle은 reference URI로만 연결했고 MAP19_09 scale audit은 다시 실행하지 않았다. Legacy 19347, 이전 task category, PlayMode, unfiltered/full regression도 실행하지 않았다.

## Responsibility and Added Scripts

| Script or file | Added or changed responsibility | Explicit non-ownership |
|---|---|---|
| `Assets/_Game/Map/Runtime/WorldGeneration/Tooling/GeneratedTerrainRunScope.cs` | Exact four-scope catalog, output boundary resolution, MAP15 radius/max constants를 재사용한 in-bounds OneRing 좌표 | Generator solve/render, movement, file I/O, rollback execution |
| `Assets/_Game/Map/Runtime/WorldGeneration/Tooling/GeneratedTerrainRunArtifact.cs` | Run request/plan facts, required artifact schema, deterministic JSON/canonical digest, snapshot/rollback artifact models, centralized MAP19/MAP20 preconditions | Editor UI, disk mutation, generation/validation algorithms |
| `Assets/_Game/Editor/MapAuthoring/WorldGeneration/Tooling/GeneratedTerrainRunCoordinator.cs` | Run-active state, task-owned path enforcement, pre-mutation snapshot, scoped atomic writes, automatic/manual rollback, artifact file publication, deterministic sample publisher | Generator decision-making, MAP19 proof, Scene/Prefab/Tilemap/runtime object mutation |
| `Assets/_Game/Editor/MapAuthoring/WorldGeneration/Tooling/GeneratedTerrainGeneratorWindow.cs` | One menu item and seed/scope/confirmation/run/dry-run/rollback/status/digest/output/replay UI | Generator pipeline logic, validation logic, direct generated-file writes |
| `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Tooling/GeneratedTerrainGeneratorWindowTests.cs` | Exact eight MAP20_01 focused EditMode proofs for open safety, artifact contract, scopes, snapshot ordering, scoped restore, and failure references | Prior categories, PlayMode, legacy/full regression, gameplay verification |
| Matching five script `.meta` files and three new directory `.meta` files | Stable Unity asset identities for MAP20_01 additions | Existing asset identity changes |
| `MapDesign/MCP/GENERATED/MAP20_01/runs/sample-pattern-731/*` | One deterministic sample run artifact, pre-run snapshot manifest, and successful manual rollback result | Production output, production seed approval, generated map content retention |
| `MapDesign/MCP/TASKS/MAP20_01_CREATE_GENERATOR_WINDOW_AND_SCOPED_ROLLBACK.md` | Installed byte-identical task authority | Task body mutation |
| `MapDesign/MCP_ARCHIVE/MAP20_01_CREATE_GENERATOR_WINDOW_AND_SCOPED_ROLLBACK.md` | Archived byte-identical inbox candidate | Additional inbox candidates or MAP20_02 start |
| This Result | PASS evidence, exact counters, digest handoff, ownership boundaries | MAP20_02 execution or unlock |

## Generator Window and Scope Summary

```text
MAP19_09 Result SHA-256 required/actual:
1dfbe8c14380266b42bbd34d8cd0a39e394ff9e8b79ac9461c5a5af26dcbbadf
1dfbe8c14380266b42bbd34d8cd0a39e394ff9e8b79ac9461c5a5af26dcbbadf
MAP19_09 installed Task SHA-256 required/actual:
b772a2417567ffeaf7afe59935ca1fa325fd31cab737ce94fba06ec300b979a0
b772a2417567ffeaf7afe59935ca1fa325fd31cab737ce94fba06ec300b979a0
MAP20_01 handoff digest required/actual:
2e69b77f8720d30e6c1d9fcdd7bdeb883ea36540033422e4908367ce60377c3ce
2e69b77f8720d30e6c1d9fcdd7bdeb883ea36540033422e4908367ce60377c3ce
MAP19 exit digest reused: 0f5bac81722f727dd02772c76323c9fa69ce91ad3d1a3722cef2aa47381ccec2

window menu path: Tools/MapDesign/Generated Terrain Generator
window opens without run: YES; open invocation 1 / run request 0 focused proof
scope tokens: Pattern / Sector / OneRing / World
world scope confirmation: explicit worldConfirmed checkbox; production approval remains false
run controls: Run and Dry-run plan; both disabled while active, World also requires confirmation
rollback controls: Rollback disabled unless rollback_available and no run is active
status/digest/replay display: last pass_state, canonical_digest, owner/reason, replay, open-folder and copy controls

duplicated generator logic count: 0
duplicated validation logic count: 0
hard-coded scope list copies: 1 centralized definition
hard-coded digest string copies outside precondition/constants: 0
```

`GeneratedTerrainRunScopeCatalog` is the only production scope list. Window options, request validation, artifact tokens, boundary selection, and tests consume that catalog. OneRing directly reuses `WorldRollbackScope.Radius == 1` and `WorldRollbackScope.MaximumSectorCount == 9`; interior/corner focused evidence produced 9/4 in-bounds sectors.

## Run Artifact and Rollback Summary

```text
run artifact schema version: map20_01.generated_terrain_run.v1
run artifact required fields present/required: 25/25
sample run artifacts created: 1 run_artifact.json
sample rollback snapshots created: 1 rollback_snapshot_manifest.json before mutation
sample rollback operations executed: 1 manual PASS; created output removed and pre-run empty state restored
rollback output roots: MapDesign/MCP/GENERATED/MAP20_01/runs/sample-pattern-731
changed path count in focused tests: 4
created path count in focused tests: 1
deleted path count in focused tests: 0
paths touched outside MAP20_01 generated output: 0
Scene/Prefab/Tilemap mutation count: 0
runtime object mutation count: 0

run artifact schema digest lower-hex SHA-256: 4ccf7934d1f32a0294f0b8a824cb56d879a0cca42ec2b9ff6e9bbbcedbb922b3
sample run artifact digest lower-hex SHA-256: a88e67f4256e0153d3997288321362d2475979e97d361510e35e31d1c8f9f4cd
rollback snapshot digest lower-hex SHA-256: 726c923fa70c4b9b6a89984fa8487fe75745938f9612d6d190712a5fa25328dc
rollback result digest lower-hex SHA-256: 2407e57c30bcfa844c0907cf655479797241ae42831bfce49bc7d29254ea4dac
MAP20_02 handoff digest lower-hex SHA-256: ce4c0cb859a0b8cbd8b4179b5b2e346fa474b95ae2abbcd0142f34d396c46003
```

The MAP20_02 handoff is SHA-256 over LF-joined `MAP20_02_HANDOFF_V1`, task id, schema digest, sample run digest, snapshot digest, rollback result digest, MAP19 exit digest, incoming MAP20_01 handoff, exact scope token line, and `FOCUSED_EDITMODE|8|8|8|0`. It is published only because the focused result is PASS. It does not start or unlock MAP20_02.

The sample first wrote one task-owned pattern plan artifact through the coordinator and then invoked the same coordinator's manual rollback. Its final output digest equals the pre-run empty-output digest, `rollback_applied` is true, and the temporary generated pattern file no longer exists. Focused validation separately proved non-empty backup capture, existing-file restore, new-file removal, sibling-sector preservation, and automatic rollback after a synthetic post-mutation failure.

## Focused Validation Summary

```text
Unity Version: 6000.3.8f1
Compile Errors: 0
Relevant Warnings: 0
Relevant Console Errors: 0
EditMode category: MAP20_01 (the exact sole fixture carrying this category was selected)
Discovered: 8
Executed: 8
Passed: 8
Failed: 0
Skipped: 0
Inconclusive: 0
PlayMode Tests: NOT RUN
Scene/Prefab Changes: NONE
```

Final NUnit evidence recorded `testcasecount="8" result="Passed" total="8" passed="8" failed="0" inconclusive="0" skipped="0"`; the fixture property records `Category=MAP20_01`. All required test names passed:

```text
GeneratorWindowOpensWithoutStartingGeneration
GeneratorRunArtifactCapturesSeedVersionHashScopePassAndReplay
GeneratorRunScopesAreExactlyPatternSectorOneRingAndWorld
OneRingScopeUsesInBoundsMooreRadiusOneAndMaxNineSectors
RunCoordinatorCreatesRollbackSnapshotBeforeOutputMutation
RollbackRestoresOnlySelectedGeneratedOutputScope
FailureRunPublishesReplayAndNoMap20_02Handoff
GeneratorWindowDoesNotSelectLegacyRegressionPriorCategoriesPlayModeOrFullRegression
```

## No Legacy Regression Boundary Notes

Only the single MAP20_01 focused EditMode fixture was executed. No earlier category, PlayMode, legacy selection, unfiltered run, full regression, or MAP19_09 scale audit run was performed. The compile-only attempt and a name-filter probe executed no tests; the final proof is the exact eight-test fixture above.

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
FULL REGRESSION RUNS: 0
MAP19_09 SCALE AUDIT RERUNS: 0
```

## Final Status Evidence

- The required MAP19_09 Result and installed Task SHA-256 values match exactly, and the incoming MAP20_01 handoff matches exactly.
- Installed and archived MAP20_01 task copies are byte-identical at `ee0ee7d11296049995e04e753afc1e79b3ee95b0dbde104661d00deb46686bcb`.
- Window open is side-effect free; scope tokens are exact and World has an explicit confirmation gate.
- Every valid run publishes the required deterministic artifact and captures a scope-bounded snapshot before any output mutation.
- Manual and automatic rollback restore only selected task-owned generated output. No git rollback command exists in the implementation.
- Focused MAP20_01 EditMode validation is 8/8 PASS with zero task-relevant compile, warning, or Console findings.
- Generator solve, movement, physics, and MAP19 proof files were not changed.
- MAP20_02 remains `LOCKED`, was not started, and no MAP20_02 files were created.

Result: PASS
