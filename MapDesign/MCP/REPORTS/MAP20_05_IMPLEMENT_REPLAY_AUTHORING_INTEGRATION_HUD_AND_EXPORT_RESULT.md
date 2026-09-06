TASK: MAP20_05_IMPLEMENT_REPLAY_AUTHORING_INTEGRATION_HUD_AND_EXPORT
STATUS: PASS

## User-Facing Implementation Report

MAP20_05 adds the `Tools/MapDesign/Replay Authoring Integration HUD and Export`
window as the final read-only MAP20 authoring surface. It combines a searchable failure
browser, a deterministic `RequestOnly` replay-authoring preview, MAP07 fixed/generated
origin identities, all six MAP08 boundary-pair links, a display-only runtime HUD state,
and a seed-bundle sample export.

No actual MAP19 failure-bundle JSON exists at the allowed sample path. The browser
therefore publishes one explicit `MissingData` record and does not invent an actual
failure. Its one test-owned `FocusedFixture` record is separately typed, has
`actual_failure_available: false`, and is counted separately from actual failures.

The replay request records seed, world/sector/cell, MAP20_04 validation error and jump
kind, selection path, failure-bundle identity, authoring-context digest, requested
action, and deterministic request/canonical digests. Its execution state is permanently
`RequestOnly`; the contract has no execution method and invokes no replay, generator,
validator, or rollback entry point.

The MAP07 split preserves the exact `FixedMap07 / GeneratedMap16Slice /
GeneratedMap17Runtime / GeneratedMap18Population / MissingData` origin tokens without
rewriting source data. MAP08 links reference the six public pair-rule, candidate,
microchunk projection, and socket identities from the installed boundary authoring
contracts. The PDF four-pair subset is not used as source of truth.

The runtime HUD is immutable data plus a pure formatter. It displays seed, world,
sector/cell, active pass/tool mode, selected validation/failure/source/selection, and a
status line. It is not a `MonoBehaviour`, has no bootstrap or auto-spawn path, and makes
zero runtime object, Scene, or Prefab changes.

The seed bundle includes the source Task/Result/handoff digests, seed/version/pass
summary, navigation/jump/failure/request/HUD/split/boundary digests, the five origin
records, and six boundary links. The publisher writes only the five requested JSON
artifacts under `MapDesign/MCP/GENERATED/MAP20_05`; it does not approve a production
seed or write Authoring CSV.

Static scans found zero generator, validator runner, rollback, replay executor, prior
sample publisher, CSV parser/export, Tilemap, runtime spawn, or external-process entry
points in MAP20_05 production code. The final Unity selection was only
`EditMode / MAP20_05` and passed 10/10; legacy 19347, prior categories, PlayMode,
unfiltered, and full regression selections were never made.

## Responsibility and Added Scripts

| Script or file | Added or changed responsibility | Explicit non-ownership |
|---|---|---|
| `GeneratedReplayAuthoringIntegration.cs` | Defines immutable RequestOnly replay requests, failure records/index, exact MAP07 origin split, six MAP08 boundary links, seed bundle, canonical JSON, and lower-hex digests. | Replay/generator/validator/rollback execution, repair, CSV parsing/writing, source migration. |
| `GeneratedRuntimeDebugHud.cs` | Defines immutable HUD state and a pure five-line display formatter with zero mutation counters. | MonoBehaviour lifecycle, auto-registration, GameObject spawn, Scene/Prefab wiring. |
| `GeneratedReplayAuthoringWindow.cs` | Provides failure search/source filter/list/details, request/split/boundary/HUD previews, copy actions, sample export, and output-folder action. | Save, Apply, Fix, Import, Generate, Validate, Run, Replay, Rollback, production approval. |
| `GeneratedReplayAuthoringSamplePublisher.cs` | Hash-validates MAP20_04 inputs, reads existing MAP20_04 JSON and optional MAP19 failure JSON, builds deterministic samples, and writes only MAP20_05 JSON. | MAP20_04 regeneration, Authoring CSV writes, asset/Scene/Prefab mutation, external processes. |
| `GeneratedReplayAuthoringIntegrationTests.cs` | Owns exactly ten MAP20_05 EditMode tests for request safety, failure separation, seed bundle, origins, six boundary identities, HUD immutability, output root, determinism, handoff gate, and regression boundaries. | Prior categories, PlayMode, legacy/full regression. |
| Matching five `.cs.meta` files | Provide unique Unity asset identities for the four production scripts and one test script. | Folder, asmdef, Scene, Prefab, package, or project-setting changes. |
| `MapDesign/MCP/GENERATED/MAP20_05/*.json` | Publishes the failure index, request, HUD, seed bundle, and digest manifest. | Generated terrain output, validation results, MAP20_02/03/04 sample regeneration. |

## Replay Authoring and Failure Browser Summary

```text
MAP20_04 Result SHA-256 required/actual: f9bac092ce7e1177e9eea6ca61a46ea671330790b55355206c06027ac1e77235 / f9bac092ce7e1177e9eea6ca61a46ea671330790b55355206c06027ac1e77235
MAP20_04 installed Task SHA-256 required/actual: f398401662bcb9134db8269fa6dbc4d42752ae90b1a442b16a6b8e6610cf9106 / f398401662bcb9134db8269fa6dbc4d42752ae90b1a442b16a6b8e6610cf9106
MAP20_05 handoff digest required/actual: 729920113fefff78e3e5584d34c3ce682f3017a545e3e3bf8ae0bbdcc8b20f55 / 729920113fefff78e3e5584d34c3ce682f3017a545e3e3bf8ae0bbdcc8b20f55

replay request required fields present/required: 17/17
replay requests created: 1 sample
replay requests executed: 0
requested action / execution state: AuthorReplayRequest / RequestOnly
failure source kind tokens: ActualFailureBundle / FocusedFixture / MissingData
actual failure records: 0
focused fixture failure records: 1
missing failure records: 1
failure browser repair/rerun/replay actions: 0
```

## Fixed Generated and Boundary Link Summary

```text
fixed/generated origin tokens: FixedMap07 / GeneratedMap16Slice / GeneratedMap17Runtime / GeneratedMap18Population / MissingData
fixed origin records: 1
generated origin records: 3
missing origin records: 1
source files rewritten for split: 0
MAP08 approved pair capacity: 6
MAP08 boundary links created: 6
boundary regeneration executions: 0
PDF boundary subset used as source of truth: NO
```

The six links retain one installed candidate/projection/socket reference for each of
`PAIR_CRATER_ROOT`, `PAIR_CRATER_MILL`, `PAIR_CRATER_DOUGH`, `PAIR_ROOT_MILL`,
`PAIR_ROOT_DOUGH`, and `PAIR_MILL_DOUGH`.

## Runtime HUD Summary

```text
HUD state required fields present/required: 16/16
HUD component auto-spawn path count: 0
runtime GameObject mutation count: 0
Scene/Prefab HUD wiring count: 0
runtime HUD sample states: 1
display lines per state: 5
```

## Seed Bundle Export Summary

```text
seed bundle required fields present/required: 19/19
seed bundle sample files written: 1
seed bundle write roots: 1 / MapDesign/MCP/GENERATED/MAP20_05
production seed approvals: 0
authoring CSV writes: 0
generator/validator/replay/rollback executions: 0 / 0 / 0 / 0
MAP20_05 total JSON artifacts written: 5
external process launches by focused actions: 0
```

## Snapshot and Digest Summary

```text
sample artifacts created: 5
failure browser index digest lower-hex SHA-256: a008a2914108899f360f0b795634177d9f0390ead8a33f947e5d25267e1da2bf
replay authoring request digest lower-hex SHA-256: df8140b2586f702550ade088e393a41e179452202ab6101d5306b56d859a533d
runtime HUD state digest lower-hex SHA-256: 46cae11f423d4f0a2efa8441155160e18c7eb6ebd137ccba0146c2491a3e8f88
seed bundle export sample digest lower-hex SHA-256: 36410b172ff7c2945b491c5d86ef50d5a9eb7245c78bde539efec6a2e82d9278
digest manifest lower-hex SHA-256: a45d0b93e854718b46686bb7a172cd99736887d734388ade482062db369feee5
MAP20_06 handoff digest lower-hex SHA-256: 34d7e10a208547780d86c7aa985c4945d76956e8e9869a266d12a1cda27b13fb
```

All canonical digests exclude `created_utc`. Collections and JSON fields use stable
ordinal order, and repeat/culture/input-order checks passed.

## Focused Validation Summary

```text
Unity Version: 6000.3.8f1
Compile Errors: 0
Relevant Warnings: 0 (2 Unity Test Framework prebuild/cleanup infrastructure warnings)
Relevant Console Errors: 0 (1 non-failure TestResults-save infrastructure entry was classified as Exception)
EditMode category: MAP20_05
Discovered: 10
Executed: 10
Passed: 10
Failed: 0
Skipped: 0
Inconclusive: 0
PlayMode Tests: 0
Scene/Prefab Changes: NONE
```

The first category probe occurred before Unity imported the new test and returned 0/0;
it was not accepted as evidence. The first imported 10-test run exposed one test-only
NUnit matcher compatibility error. After replacing that assertion with a direct count
comparison, the final-code focused run passed 10/10. No wider selection was used.

Static gates:

```text
new production C#/meta: 4/4
new EditMode test C#/meta: 1/1
existing production/test C# modified: 0
new meta GUID collisions: 0
required output files present: 5/5
duplicated generator logic count: 0
duplicated validation logic count: 0
duplicated rollback logic count: 0
duplicated overlay/detail inspector logic count: 0
duplicated CSV parser/export logic count: 0
duplicated navigation publisher logic count: 0
duplicated jump target catalog count: 0
hard-coded source path copies outside sample/precondition constants: 0
hard-coded digest string copies outside precondition/constants: 0
Authoring CSV tracked changes: 0
Scene / Prefab / ProjectSettings / Packages changes: 0
MAP20_02/03/04 generated artifact changes: 0
```

## No Legacy Regression Boundary Notes

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
VALIDATION RUNNER EXECUTIONS: 0
REPLAY EXECUTIONS: 0
GENERATOR EXECUTIONS: 0
ROLLBACK EXECUTIONS: 0
CSV AUTHORING WRITES: 0
RUNTIME OBJECT SPAWNS: 0
SCENE PREFAB CHANGES: 0
```

## Final Status Evidence

```text
Result Task ID matches Current Task: PASS
Result exact STATUS line: PASS
MAP20_05 done conditions: PASS
MAP20_06 status before finalize: LOCKED
MAP20_06 started: NO
MAP20_06 files created: 0
Status finalize authorized: YES, only after this Result is revalidated
Git push authorized/performed: NO / NO
```
