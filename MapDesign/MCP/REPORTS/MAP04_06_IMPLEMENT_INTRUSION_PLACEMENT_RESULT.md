# MAP04_06 — Implement Intrusion Placement Result

STATUS: PASS

## PATCH APPLY

- Inbox patch: `MAP04_06_IMPLEMENT_INTRUSION_PLACEMENT`
- Manifest SHA-256: `b5a84f4033dc5b55e65bc4ff4a7e51b5e420bedb7bde80fef23fec8a9b402d91`
- `.APPLIED` SHA-256: `c021a42694bba60d6425b87fdc05b5d80beb56df8953a4c6e7d0da74fb67381c`
- Prior Result SHA-256: `ab23a2d0e30cb21df7fca6f098607cf20ccd5a3cc9a9da4f43f8fdb344ba6e2f`
- Manifest preconditions, exact payload hashes, exact three-copy apply, Current Task transition, and marker creation passed before implementation.

## ALLOWLIST / CHANGE CLASSIFICATION

- READ allowlist: MCP global/control documents, this Task, immediately prior Result, exact permitted checked-in APIs/tests, frozen Map Package reference CSVs, and permitted path/hash/meta inventories only.
- Installed Authoring CSV bodies, unrelated production/test bodies, Legacy generator bodies, Scene/Prefab YAML, and future Task bodies were not read.
- WRITE allowlist: exact 7 Runtime C#, 1 EditMode test, their 8 matching metas, this Result, and Phase C status finalization only.

### CREATED

- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/IntrusionPatchIdFactory.cs` — SHA-256 `9c3549f5340bdd616d9c61dc4dbc787c772c8945c4fcbb59d67e6072972156b1`; meta GUID `6d1597a670a44b348b52f5c4ac40c501`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/IntrusionPlacementError.cs` — SHA-256 `79996a0f53ae34c3e241a3700803860073ba5669c1339bf165cf751da8bb6994`; meta GUID `9cc70811fa88495b9754350524fc4de8`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/IntrusionPlacementRecord.cs` — SHA-256 `8c4a6a733037e83b0a75e078fcfc80d42c48fb4c5aa87b23eb2ae90dbc863148`; meta GUID `8279e707a82c457a819e38bcb976b388`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/IntrusionPlacementDiagnostics.cs` — SHA-256 `4fc808c77095388a061bdb1c28db90c84fac6d0768ee2294419a1e766943355e`; meta GUID `df9c142b05fd4fb6a30b6587982175a2`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/IntrusionPlacementPublication.cs` — SHA-256 `41a916ec5fd41f58d606e35e6c4a16eb4444b1b17fb12675f266d3de43f9ce64`; meta GUID `46df93d977fc4ebda94a13ea38970e89`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/IntrusionPlacementResult.cs` — SHA-256 `010d929909b8784866b54afa1980fba151862baf7a7d107d431c93011dfc9e68`; meta GUID `e8651fcff45742d5b2084f7d36cff93b`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/IntrusionPlacer.cs` — SHA-256 `b18492e9971d2a5da80837f7495f43ba75693a7c5a9955139f52e5c43daffe8b`; meta GUID `2b929481719b44cfbe6cbdd3db86a0e4`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/IntrusionPlacerTests.cs` — SHA-256 `7378f5ce8111f73c87e99ee6abe8072cf47512901b7289f0c9420131f908fe3f`; meta GUID `748a094e15ee4fd787b1af4192264ab9`
- All 8 matching metas have `fileFormatVersion: 2`, valid unique non-zero lowercase 32-hex GUID, and `MonoImporter`.

### MODIFIED

- Phase A exact payload copies: `MASTER_IMPLEMENTATION_TASK_LIST.md`, `06_IMPLEMENTATION_STATUS.md`.
- Phase C: `06_IMPLEMENTATION_STATUS.md` only, after this PASS Result is validated.
- Existing Assets production/tests/meta/asmdef/asmref modified: `0`.

### PREEXISTING_IDENTICAL

- MAP04_01~05 public models, RNG, P01/P02 publications, production/tests/meta, Authoring CSV/meta, accepted Legacy `Editor.meta`, asmdef/asmref, Scene/Prefab, Packages, and ProjectSettings were not modified.

## IMPLEMENTATION EVIDENCE

- Structural preflight accumulates, sorts, and deduplicates errors before RNG use. It validates completed growth/publication/diagnostics linkage, exact 169 rows, `165/4` ownership conservation, exact four Core/no prior Intrusion, Core/Satellite connectivity and rule limits, P01 reservation correspondence, exact four biomes/ten patch rules/six profiles/six pair rules, active `BOUND_TUNNEL/TUNNEL_INTRUSION`, generation profile identity, and continued RNG DrawCount.
- Count-first schedule consumes exact two inclusive `NextInt(0, 3)` calls in `MILL`, then `ROOT` order before candidate enumeration.
- Candidate filtering is cardinal-only and rejects reserved, edge, seed, site-binding, same-biome, disallowed-pair, missing-source-edge, donor-minimum, donor-disconnect, same-rule-distance, normal-share, and Intrusion-share violations before a single unbiased candidate draw.
- Each accepted sector is atomically removed from its connected Core/Satellite donor and assigned to a new exact one-cell Intrusion patch/seed/ownership. Prior Intrusion patches are never host or anchor candidates.
- Unchanged patches/ownership rows and every binding are reference-preserved; changed donors are rebuilt without mutating their source objects.
- Zero desired count publishes a new input-equivalent snapshot after count draws only. Candidate exhaustion uses `RetryRequired`, null publication, empty public records, and input final counts; partial working state is never published.

## EXACT BOUNDARY RELATIONS

| Intruder | Host | Pair | Result |
|---|---|---|---|
| `BIO_CASSIA_ROOT` | `BIO_MOON_CRATER` | `PAIR_CRATER_ROOT` | allowed |
| `BIO_CASSIA_ROOT` | `BIO_ABANDONED_MILL` | `PAIR_ROOT_MILL` | allowed |
| `BIO_CASSIA_ROOT` | `BIO_MOON_DOUGH` | `PAIR_ROOT_DOUGH` | allowed |
| `BIO_ABANDONED_MILL` | `BIO_CASSIA_ROOT` | `PAIR_ROOT_MILL` | allowed |
| `BIO_ABANDONED_MILL` | `BIO_MOON_DOUGH` | `PAIR_MILL_DOUGH` | allowed |
| `BIO_ABANDONED_MILL` | `BIO_MOON_CRATER` | `PAIR_CRATER_MILL` | rejected: `BOUND_TUNNEL` absent |

- Same-biome pairs and `PAIR_CRATER_DOUGH` are rejected.
- Pair identity is preserved exactly; only A/B membership is evaluated bidirectionally.
- Display names, notes, default profile, chunk catalog, and resource/element pools are not used as placement heuristics.

## ACTUAL VIABLE FACTORY EVIDENCE

- World seed / attempt: `0x0123456789ABCDF9 / 24`.
- Input patches / assigned / reserved-unassigned: `14 / 165 / 4`.
- Input RNG DrawCount: `1907`.
- Count raw outputs: `CB8386606F087EA4 / 9018672136A34305`.
- Desired `MILL / ROOT`: `1 / 2`; DrawCount after counts: `1909`.
- Candidate method/raw draws: `3 / 3`; total method/raw draws: `5 / 5`; final DrawCount: `1912`.

| Seq | Rule | Candidates / roll | Sector `(x,y)` | Host / donor | Size | Pair | Anchor | Same-rule distance |
|---:|---|---:|---|---|---:|---|---:|---:|
| 0 | `PATCH_MILL_INTRUSION` | `6 / 4` | `96 (5,7)` | `BIO_CASSIA_ROOT / PATCHINST_SAT_BIO_CASSIA_ROOT_02` | `14→13` | `PAIR_ROOT_MILL` | 83 | -1 |
| 1 | `PATCH_ROOT_INTRUSION` | `29 / 27` | `148 (5,11)` | `BIO_MOON_CRATER / PATCHINST_SAT_BIO_MOON_CRATER_00` | `14→13` | `PAIR_CRATER_ROOT` | 135 | -1 |
| 2 | `PATCH_ROOT_INTRUSION` | `26 / 17` | `113 (9,8)` | `BIO_MOON_CRATER / PATCHINST_CORE_RSV_05_SITE_MOON_CORE_METEOR` | `17→16` | `PAIR_CRATER_ROOT` | 114 | 7 |

- Output patches / assigned / reserved-unassigned: `17 / 165 / 4`.
- Final biome sectors `MILL / ROOT / CRATER / DOUGH`: `24 / 32 / 57 / 52`.
- Donor minimum/disconnect/protected/disallowed/reservation/overlap counters: `0 / 0 / 0 / 0 / 0 / 0`.
- Every new patch has exact `1` sector, exact `1` Intrusion seed, null source site, no binding, empty SecondaryBiome.
- Source growth result/publication/P01/P02 and caller definitions observable mutation: `0`.
- Zero-count fixture: Completed, `0` Intrusions, count calls `2`, candidate calls `0`, input-equivalent output, source mutation `0`.
- Exhaustion branch contract: `RetryRequired`, publication `null`, public records empty, rollback patch/assigned/unassigned counts equal input, with desired/attempted/candidate/RNG evidence retained in diagnostics.

## UNITY VERIFICATION

- Unity instance/version: `Constant@ced6e0dfc4a31d45` / `6000.3.8f1`.
- Focused job `3f5d38fe61344672a9e1498846e67193`: `IntrusionPlacerTests = 156/156 PASS`.
- Regression job `fecc332255ad4509a7f7c9d3789fdeea`: `MultiSeedBiomeGrowerTests = 164/164 PASS`.
- Regression job `6c6c97fb8caf4805a4635577e34db86c`: `SatelliteSeedPlacerTests = 141/141 PASS`.
- Regression job `ea5e6f5074794eed832beb07a81b7719`: `BiomePatchModelsTests = 107/107 PASS`.
- Regression job `828ebb2866d44af6abbdb04aecd3a9e3`: `DeterministicRngStreamTests = 103/103 PASS`.
- Required regression total: `515/515 PASS`; actually executed required total: `671/671 PASS`, failed/skipped `0/0`.
- Game.Map targeted discovery arithmetic only: prior `4509 + 156 = 4665`; no targeted-suite PASS claim is made.
- Full EditMode discovery arithmetic only: prior `4577 + 156 = 4733`; no full-suite PASS claim is made.
- Final forced compile: compile errors `0`, Console errors `0`, relevant new warnings `0`.
- One initial test-run cleanup verifier observed the newly imported allowlisted scripts; the clean rerun above passed `156/156`, and final compile/Console are clean.

## ASSET / META / CHANGE SCOPE

- Baseline / final Assets meta: `3110 / 3118`.
- Valid GUID rows / invalid GUID rows / duplicate GUID groups: `3118 / 0 / 0`.
- Exact Assets files newer than `.APPLIED`: `16`, consisting only of 7 Runtime C#, 1 test C#, and their 8 matching metas.
- Existing Assets modifications / unexpected Assets changes: `0 / 0`.
- Authoring CSV/meta: `50/50`, unchanged.
- Accepted Legacy `Editor.meta`: `6/6`, unchanged.
- Scene/Prefab/ProjectSettings/Packages changes: `0`.
- Production forbidden UnityEditor/System.Random/Unity RNG/time/filesystem/static-mutable dependency scan: `0` findings.

## OUT_OF_SCOPE_FINDINGS

- None.

## NEXT

- `MAP04_06_IMPLEMENT_INTRUSION_PLACEMENT` only becomes `COMPLETE`.
- Current Task becomes `NONE`.
- `MAP04_07_IMPLEMENT_PATCH_CLEANUP` remains `LOCKED`; it is not started and no Task file is created.
