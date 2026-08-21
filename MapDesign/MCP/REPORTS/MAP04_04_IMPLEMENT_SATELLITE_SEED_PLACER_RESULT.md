# MAP04_04 — Implement Satellite Seed Placer Result

STATUS: PASS

## PATCH APPLY

- Inbox patch: `MAP04_04_IMPLEMENT_SATELLITE_SEED_PLACER`
- Manifest SHA-256: `b229d83b674b1b610fbfbc3a6884c167fee60ddd6bc10b0cd47f6c0061f2cdea`
- `.APPLIED` SHA-256: `edcde376c60a2ea9ccb243d90f21b13448a5f06dbfd6bea9cecff73d0c5aee40`
- Manifest preconditions passed: prior Current Task `NONE`, prior Result `STATUS: PASS`, 205 task rows with zero state mismatch, destination Task absent, and all three payload files non-empty with exact declared hashes.
- Exact three-copy apply was verified before implementation. No other inbox patch was unapplied.

## ALLOWLIST / CHANGE CLASSIFICATION

- READ allowlist: only the MCP control documents, this Task, the immediately prior Result, explicitly allowed existing APIs/tests, reference starter contracts, new Task outputs, and permitted path/hash/meta inventories were inspected.
- Installed Authoring CSV bodies, unrelated production/test bodies, Legacy generator bodies, Scene/Prefab YAML, and future Task bodies were not read.
- WRITE allowlist: writes were limited to the manifest-declared MCP files/marker, the exact 7 new Runtime C# files, 1 new EditMode test, their 8 matching metas, this Result, and Phase C status finalization.

### CREATED

- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SatellitePatchIdFactory.cs` — SHA-256 `69a6f82329711ef203a5f39bf74537f2726459f0b9c804e2a3640b67c29dc94a`; meta GUID `9cf183fac6828774ebca9e9c42d8c9fe`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SatelliteSeedPlacementError.cs` — SHA-256 `9433a1dc6aaec15520db13a29e671cf8c11ed9f5ad0ec24597e3cb5273160420`; meta GUID `c3ca5eb8ec4b18640ae752dea16f3d5a`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SatelliteSeedPlacementRecord.cs` — SHA-256 `fcdccd8473a798b77fd606aa9a51218435b9569e6e137b13119f972caf41ecb2`; meta GUID `3811bbd45d6230e49a2a432989277b6e`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SatelliteSeedPlacementDiagnostics.cs` — SHA-256 `5ba29761842d4b07d2e5bff4e2f0de60863bcba58ab1b04d30f7fdf1aee4a846`; meta GUID `255f47a71fde9f049bc86fb9123f96c7`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SatelliteSeedPlacementPublication.cs` — SHA-256 `9387865bf0378ab50ddc17c9afd89cf21644b79d971e7500dbcb91157ba27e51`; meta GUID `32a2a313c16163948ba5c64faf1f7ee0`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SatelliteSeedPlacementResult.cs` — SHA-256 `41972029181929b7e18646387d03596954aa2b75fc0b359c7fb9a3cfe386e607`; meta GUID `d902bf2c3984c8d489395296d8b58175`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SatelliteSeedPlacer.cs` — SHA-256 `2a5ae628261123684ff404137c992f17d0fd624d2dff84a90f89a8956443b6dd`; meta GUID `41cb8d229a6a5724ca71224b4fdeba23`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SatelliteSeedPlacerTests.cs` — SHA-256 `a4aa1405f5fee9b7e0483a64964080b1bede0b4ed296edd2f92285748af96e5f`; meta GUID `265a5de008f20674593afb85e155c5cf`
- All 8 matching metas have `fileFormatVersion: 2`, a valid unique non-zero lowercase 32-hex GUID, and `MonoImporter`.
- MCP additions: `TASKS/MAP04_04_IMPLEMENT_SATELLITE_SEED_PLACER.md`, inbox `.APPLIED`, and this Result.

### MODIFIED

- Phase A exact payload copies: `MASTER_IMPLEMENTATION_TASK_LIST.md`, `06_IMPLEMENTATION_STATUS.md`.
- Phase C: `06_IMPLEMENTATION_STATUS.md` only, after this PASS Result is validated.
- Existing Assets production/tests/meta/asmdef/asmref modified: `0`.

### PREEXISTING_IDENTICAL

- Output collisions: `0`; all 8 requested C# destinations and the Task destination were absent at their required preconditions.
- Existing allowed dependencies were consumed through their checked-in public APIs and were not modified.

## IMPLEMENTATION EVIDENCE

- Canonical rule order: `PATCH_CRATER_SAT`, `PATCH_DOUGH_SAT`, `PATCH_MILL_SAT`, `PATCH_ROOT_SAT`.
- Exact count-first schedule: 4 count method calls occur before any candidate call. Accepted candidates are removed globally; rejected candidates are removed only from the current rule/seed attempt universe.
- Same-biome Manhattan distance uses every Core patch sector and every earlier accepted same-biome Satellite seed. Different-biome adjacency is not filtered.
- Edge rejection follows each rule's exact `CanTouchWorldEdge`; reserved or already assigned sectors are excluded from the raw universe.
- Satellite patches are immutable one-cell patches with one seed, no site binding, and exact `PATCHINST_SAT_<BIOME>_<D2>` IDs. Core patch/seed/binding instances remain preserved.
- Structural invalid input accumulates stable sorted/deduplicated errors with RNG consumption `0`. Candidate exhaustion returns `RetryRequired` with no publication and atomic rollback.

## ACTUAL STARTER VECTOR

- RNG factory/state: world seed `0x0123456789ABCDEF`, `RNG_BIOME_PATCH`, `PASS_BIOME`, retry `0`, initial state `0x98BC23250806566B`.
- Desired count rolls `CRATER/DOUGH/MILL/ROOT = 2/0/2/3`.
- RNG method/raw draws: count `4`, candidate `9`, total `13`; DrawCount `0 -> 13`.
- Raw candidates: `145`; aggregate edge/distance rejections: `2/0`.

| Patch ID | Sector | Coord | Attempts | Same-biome distance / min | Edge / distance rejects |
|---|---:|---:|---:|---:|---:|
| `PATCHINST_SAT_BIO_MOON_CRATER_00` | 155 | `(12,11)` | 1 | `6/3` | `0/0` |
| `PATCHINST_SAT_BIO_MOON_CRATER_01` | 149 | `(6,11)` | 1 | `4/3` | `0/0` |
| `PATCHINST_SAT_BIO_ABANDONED_MILL_00` | 133 | `(3,10)` | 1 | `8/3` | `0/0` |
| `PATCHINST_SAT_BIO_ABANDONED_MILL_01` | 123 | `(6,9)` | 1 | `4/3` | `0/0` |
| `PATCHINST_SAT_BIO_CASSIA_ROOT_00` | 71 | `(6,5)` | 1 | `4/3` | `0/0` |
| `PATCHINST_SAT_BIO_CASSIA_ROOT_01` | 16 | `(3,1)` | 1 | `5/3` | `0/0` |
| `PATCHINST_SAT_BIO_CASSIA_ROOT_02` | 122 | `(5,9)` | 3 | `5/3` | `2/0` |

- Per-rule attempts: `CRATER 2`, `DOUGH 0`, `MILL 2`, `ROOT 5`.
- Snapshot conservation: patches `4 -> 11`, assigned `20 -> 27`, unassigned `149 -> 142`.
- Reservation intrusion / patch overlap: `0 / 0`.
- Exhaustion fixture: raw candidates `0`, `RetryRequired`, publication `null`, records `0`, and rollback remains `4 patches / 20 assigned / 149 unassigned`.
- Individual redraw, remove-without-replacement, rule-local rejection isolation, prior-seed distance participation, and atomic exhaustion assertions all passed in the focused job.

## UNITY VERIFICATION

- Unity instance/version: `Constant@ced6e0dfc4a31d45` / `6000.3.8f1`.
- Focused job `502fc221291e402ab956c484ed638fc6`: `SatelliteSeedPlacerTests` = `141/141 PASS`, failed/skipped `0/0`.
- Starter evidence job `f3ed427cc4f240c395b2b358597090fb`: exact known-vector case = `1/1 PASS` and emitted the values recorded above.
- Regression job `176137056c9a456ba55ed94fabf56002`: `CorePatchGrowerTests 127`, `CorePatchSeedInitializerTests 121`, `BiomePatchModelsTests 107`, `DeterministicRngStreamTests 103` = `458/458 PASS`, failed/skipped `0/0`.
- Final required executed total: `599/599 PASS`, failed/skipped `0/0`.
- Targeted discovery arithmetic only: prior `4204 + 141 = 4345`; this was not represented as an executed PASS suite.
- Full EditMode discovery resource only: `4414`; no full-suite PASS claim is made.
- Final forced compile: compile errors `0`, Console errors `0`, relevant new warnings `0`.

## ASSET / META / CHANGE SCOPE

- Baseline / final Assets meta: `3093 / 3101`.
- Valid GUID rows / duplicate GUID groups: `3101 / 0`.
- Exact Assets files newer than `.APPLIED`: `16`, consisting only of the 7 Runtime C# files, 1 test C# file, and their 8 matching metas.
- Existing Assets modifications / unexpected Assets changes: `0 / 0`.
- Authoring CSV/meta: `50/50`, none newer than `.APPLIED`; prior approved path/hash baselines remain `3387d5f899db12cb2cd73b1a0fa67b5a2d431fa63d063cc728d87a50e42f084c` and `299d68f9afb66e1cd0fc17d6cc30ba6be88181c6d5c522b548a18a12130bf664`.
- Accepted Legacy `Editor.meta`: `6/6`, none newer than `.APPLIED`.
- Scene/Prefab/ProjectSettings/Packages changes: `0`.
- Production forbidden dependency/static-mutable scans: `0` findings.

## OUT_OF_SCOPE_FINDINGS

- None.

## NEXT

- `MAP04_04_IMPLEMENT_SATELLITE_SEED_PLACER` only becomes `COMPLETE`.
- Current Task becomes `NONE`.
- `MAP04_05_IMPLEMENT_MULTI_SEED_BIOME_GROWER` remains `LOCKED`; it is not started and no Task file is created.
