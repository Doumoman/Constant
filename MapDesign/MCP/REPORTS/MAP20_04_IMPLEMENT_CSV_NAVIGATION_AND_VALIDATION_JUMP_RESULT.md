TASK: MAP20_04_IMPLEMENT_CSV_NAVIGATION_AND_VALIDATION_JUMP
STATUS: PASS

## User-Facing Implementation Report

MAP20_04 adds a read-only CSV navigation index and the `Tools/MapDesign/CSV Navigation and Validation Jump` window. A validation error id resolves to one immutable record containing the normalized CSV path, exact 1-based physical row and field column, column/field name, stable record id, file digest, and source-line digest. The publisher reuses the existing `Rfc4180CsvReader` location metadata; it does not add another CSV parser.

The same error id resolves to exactly one `Tile / Pattern / Cluster / Socket / Slot` jump kind and a deterministic inspector selection path. Tile targets select MAP20_02 sector/cell view state. Pattern, Cluster, Socket, and Slot targets select MAP20_03 tab/record view state. Existing MAP20_03 data does not contain a real selected Pattern record, so that record remains explicit `MissingData`; its row and column are zero sentinels, both digests are `NONE`, and the intended selection path is retained without inventing inspector data.

The window exposes search, kind and availability filters, a result list, source and inspector panels, three copy actions, one selection-only inspector jump, and the requested generated-output-folder action. Opening, searching, filtering, copying, and jumping do not execute validation, generation, rollback, replay, import/export, repair, or CSV writes. MAP20_05 retains ownership of replay authoring integration, failure-oriented tooling, runtime HUD, and seed bundle export.

All five indexed Authoring CSV inputs were hash-compared before and after the focused actions, and Git reports zero Authoring CSV changes. The production sources contain one reuse of `Rfc4180CsvReader`, zero validation-runner or generator entry points, and no CSV writer in the window. Unity was invoked only with the focused `EditMode / MAP20_04` category; legacy 19347, prior categories, PlayMode, unfiltered, and full regression were not selected.

## Responsibility and Added Scripts

| Script or file | Added or changed responsibility | Explicit non-ownership |
|---|---|---|
| `GeneratedCsvNavigationIndex.cs` | Defines the immutable source-kind catalog, exact source-location contract, validation-error lookup, canonical JSON, and index digest. | CSV parsing, authoring mutation, validation execution, repair. |
| `GeneratedValidationJumpTarget.cs` | Defines the sole five-kind jump catalog, immutable selection target, required kind payload checks, and deterministic jump-sample JSON. | Inspector rendering, generation, replay, runtime mutation. |
| `GeneratedCsvNavigationWindow.cs` | Provides search/filter/result panels, copy actions, output-folder action, and selection-only inspector dispatch. | Save, apply, fix, import, export, generate, rollback, validate, run, replay. |
| `GeneratedCsvNavigationSamplePublisher.cs` | Reads existing MAP20_03 JSON and five Authoring CSV sources, reuses `Rfc4180CsvReader`, and writes only three MAP20_04 JSON artifacts after an explicit focused-pass receipt. | Authoring CSV writes, MAP20_02/03 regeneration, generator/validator/rollback execution. |
| `GeneratedWorldOverlayInspectorWindow.cs` | Accepts a sector/cell navigation selection as EditorWindow view state. | Artifact publication, validation, runtime/asset writes. |
| `GeneratedDetailInspectorWindow.cs` | Accepts an intended tab/record/path and reports unresolved records as `MissingData` without rebuilding or publishing detail samples. | Detail reconstruction, fallback generation, validation, project-asset writes. |
| `GeneratedCsvNavigationTests.cs` | Owns exactly eight MAP20_04 EditMode tests for location, target kinds, MissingData, UI safety, selection-only behavior, determinism, handoff gating, and regression boundaries. | Prior categories, PlayMode, legacy/full regression. |
| Matching five `.cs.meta` files | Provide Unity asset identities for the five new scripts. | Folder, asmdef, Scene, Prefab, or package changes. |
| `MapDesign/MCP/GENERATED/MAP20_04/*.json` | Publishes the navigation index, jump sample, and digest manifest. | Generated terrain output, MAP20_02 overlay output, MAP20_03 detail output. |

## CSV Navigation Summary

```text
MAP20_03 Result SHA-256 required/actual: 519fede231c4d4837e076ac87aa61b3c1067d288bc382e91e9ccbc6ac5c691bd / 519fede231c4d4837e076ac87aa61b3c1067d288bc382e91e9ccbc6ac5c691bd
MAP20_03 installed Task SHA-256 required/actual: 402ec4eef2419b7510fc8c51628c500eb7ef012da2ca7a68e9f3bd124e56260c / 402ec4eef2419b7510fc8c51628c500eb7ef012da2ca7a68e9f3bd124e56260c
MAP20_04 handoff digest required/actual: 863018695423be601328975e69356f2ece76ec4db57b024db9406d7131a43285 / 863018695423be601328975e69356f2ece76ec4db57b024db9406d7131a43285
MAP20_03 detail snapshot digest reused: 30222f392a62b2892cb47a605df4aa48d5d69178da852ed887a18471a0dd8ee8
MAP20_03 combined detail digest reused: dd3a94a2fece3e6e893343caf31493945c7c2641f6f7cbf48345032d9a2a956f

source kind tokens: AuthoringCsv / GeneratedCsv / GeneratedJson / InMemoryOnly / MissingData
source location required fields present/required: 12/12
source records: 6
source available records: 5
source missing records: 1
file/row/column records: 5
copy path-row-column action count: 1 control / 0 validation-time invocations
CSV authoring writes: 0

duplicated generator logic count: 0
duplicated validation logic count: 0
duplicated rollback logic count: 0
duplicated overlay/detail inspector logic count: 0
duplicated CSV parser/export logic count: 0
duplicated jump target catalog count: 0
hard-coded source path copies outside sample/precondition constants: 0
hard-coded digest string copies outside precondition/constants: 0
```

## Validation Jump Summary

```text
jump target kind tokens: Tile / Pattern / Cluster / Socket / Slot
jump target required fields present/required: 20/20
validation error id records: 6
Tile targets: 1
Pattern targets: 2 (1 exact CSV source, 1 explicit MissingData)
Cluster targets: 1
Socket targets: 1
Slot targets: 1
MissingData targets: 1
selection-only inspector jumps: 6 mappings / 1 focused UI action exercised
validation runner executions: 0
external process launches: 0 by navigation feature/test actions; Unity Editor itself was opened for compile and focused test execution
```

## Snapshot and Digest Summary

```text
sample artifacts created: 3
csv navigation index required fields present/required: 10/10
validation jump sample required fields present/required: 10/10
source navigation digest manifest required fields present/required: 8/8
csv navigation index digest lower-hex SHA-256: 6af989c49229b7403d3817efbff2ee9a95e2bf73aeef811d4ffab900a4959f38
validation jump sample digest lower-hex SHA-256: eedf60c3d2128fe51e529bf10276f00c7bb5c3c1c3dd25d590cac90518b9d8f7
source navigation digest manifest lower-hex SHA-256: d1b8a48f4b0841bd05e3856650d880f87f981996ab2578581069e04cfa30049d
MAP20_05 handoff digest lower-hex SHA-256: 729920113fefff78e3e5584d34c3ce682f3017a545e3e3bf8ae0bbdcc8b20f55
```

## Focused Validation Summary

```text
Unity Version: 6000.3.8f1
Compile Errors: 0
Relevant Warnings: 0 (one Unity Pipeline non-automated-editor infrastructure warning)
Relevant Console Errors: 0
EditMode category: MAP20_04
Discovered: 8
Executed: 8
Passed: 8
Failed: 0
Skipped: 0
Inconclusive: 0
PlayMode Tests: 0
Scene/Prefab Changes: NONE
```

The first discovery probe occurred before Unity imported the five new scripts and returned `0/0`; it was not accepted as evidence. After domain reload, the exact MAP20_04 category completed `8/8`, and the final-code rerun again reported progress `8/8` with no failures. No wider selection was used.

Static gates:

```text
new production C#/meta: 4/4
new EditMode test C#/meta: 1/1
existing inspector C# modified: 2
new meta GUID collisions: 0
Rfc4180CsvReader constructor sites in MAP20_04 production: 1
generator / validation runner / rollback entry-point references: 0 / 0 / 0
MAP20_02/03 publisher references: 0
Tilemap mutation references: 0
runtime GameObject mutation references: 0
Authoring CSV tracked changes: 0
Scene / Prefab / ProjectSettings / Packages changes: 0
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
VALIDATION RUNNER EXECUTIONS: 0
CSV AUTHORING WRITES: 0
```

## Final Status Evidence

```text
Result Task ID matches Current Task: PASS
Result exact STATUS line: PASS
MAP20_04 done conditions: PASS
MAP20_05 status before finalize: LOCKED
MAP20_05 started: NO
MAP20_05 files created: 0
Status finalize authorized: YES, only after this Result is revalidated
Git push authorized/performed: NO / NO
```
