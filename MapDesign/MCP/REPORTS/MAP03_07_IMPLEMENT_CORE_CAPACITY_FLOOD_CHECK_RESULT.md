STATUS: PASS

# MAP03_07_IMPLEMENT_CORE_CAPACITY_FLOOD_CHECK Result

## SUMMARY

- Implemented the immutable Core capacity requirement, flood witness, approval, diagnostics, rejection, error, result, and checker contracts.
- Implemented exact four-site structural preflight, 13x13 cardinal/Manhattan mandatory buffers, outside-edge handling, selected-footprint and mandatory-owner blockers, independent connected-capacity flood, and canonical greedy disjoint witness allocation.
- Added `215` actual NUnit cases covering the frozen enum/grid surface, invalid input aggregation, placement identity, buffer radius `0/1/2`, edge truncation, hard overlap, independent/disjoint shortfall, immutable snapshots, determinism, three full-starter seeds, and public dependency/mutation boundaries.
- Corrected diagnostics so an independent-flood rejection reports shortfall from actual reachable capacity, while a disjoint rejection reports shortfall from the partial witness.

## READ / PATCH BINDING

- Entrypoint, pipeline, global rules, Master backlog, Status, Current Task, and MAP03_06 Result were read in the mandated order and only within the Task READ ALLOWLIST.
- Applied patch: `MAP03_07_IMPLEMENT_CORE_CAPACITY_FLOOD_CHECK`, version `1.0`.
- `.APPLIED` manifest hash: `83e9441ea4bfc2946a298869d648c1cc33b725aef41640aa6691e8cc142bcaa7`.
- `.APPLIED` Task hash: `d1efae0c2eabbba6a3bc2a4d4159ab4b034e1b30587d8a3bd84c16dbe27a47af`.
- Current Task/status binding: exact `MAP03_07_IMPLEMENT_CORE_CAPACITY_FLOOD_CHECK` / `CURRENT`.
- Patch destination collisions or `PREEXISTING_IDENTICAL` Task outputs: `0`.

## CREATED

Runtime C# and matching meta:

- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityRequirement.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityRequirement.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityFloodWitness.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityFloodWitness.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityApproval.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityApproval.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityFloodDiagnostics.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityFloodDiagnostics.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityFloodRejection.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityFloodRejection.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityFloodError.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityFloodError.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityFloodResult.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityFloodResult.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityFloodChecker.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreCapacityFloodChecker.cs.meta`

EditMode test and matching meta:

- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/CoreCapacityFloodCheckerTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/CoreCapacityFloodCheckerTests.cs.meta`

Report:

- `MapDesign/MCP/REPORTS/MAP03_07_IMPLEMENT_CORE_CAPACITY_FLOOD_CHECK_RESULT.md`

## MODIFIED

- Existing Assets production/test/meta/asmdef/asmref files: `0`.
- Authoring/generated CSV/meta: `0`.
- Scene/Prefab/Package/ProjectSettings: `0`.
- Existing MAP03_01 through MAP03_06 implementation and tests: `0`.
- Status/Master were not modified during Task execution; Status is reserved for Phase C finalization after this PASS Result.

## REQUIREMENT / DEFINITION / PLAN IDENTITY

- Selection plan gate requires exact six steps and keys in Start, Boss, Forge, Cassia, Yeast, Meteor order with depths `0..5`, selected count `6`, placement equivalence, valid in-world unique footprints, and overlap `0`.
- Capacity requirements are sorted and validated as exact Forge `SITE_MOON_SEAL_FORGE`, Core `SITE_CASSIA_SAP_HEART`, Core `SITE_DEEP_STAR_YEAST`, Core `SITE_MOON_CORE_METEOR`; Start/Boss remain blockers and Village is rejected.
- Requirement envelopes preserve exact placement/special-map/primary-biome/Core-rule references. Matching selected placement checks key, origin/index/ordinal, transform, occupied indices, and transformed entry/exterior snapshots.
- Typed definitions enforce canonical IDs, active/role/count/range, special-map dimension, primary-biome, and Core-rule biome identity; starter minimums are exact `4/5/5/5`, maxima `14/18/18/18`, buffers `1/1/1/1`, and edge flags `false/false/true/true`.
- Null, duplicate, missing, unexpected, invalid enum/footprint/entry, placement mismatch, and definition mismatch inputs accumulate sorted/deduplicated structural errors and publish no flood output.

## GRID / BUFFER / FLOOD EVIDENCE

- Exhaustive grid cases: indices `0..168`, exact `index = y*13+x`, no wrap/clamp/diagonal, and only sorted valid cardinal neighbors.
- Mandatory buffer is the unique in-world union of sectors at minimum footprint Manhattan distance `<= BufferRingSectors`; own full footprint is always included.
- Radius `0`, starter radius `1`, and wide radius `2` vectors PASS. Rectangle/Chebyshev/diagonal/origin-only shortcuts are not used.
- Unique theoretical outside coordinates are counted. Non-edge-touch Forge rejects with `BufferOutsideWorld`; edge-touch Yeast truncates to four mandatory sectors and deterministically claims one connected sector to reach target `5`.
- Other five selected footprints are blockers, other Core mandatory buffers are owned blockers, and selected entry exteriors remain available unless independently occupied by another selected footprint.
- Mandatory selected-footprint blockage and pairwise mandatory-buffer overlap preserve both owners and exact sector evidence without shrinking buffers.
- Independent flood is multi-source, cardinal, canonical, and visits the entire eligible connected component. Required target is exact `max(MinSectorCount, MandatoryBuffer.Count)`.

## DISJOINT WITNESS / STATUS EVIDENCE

- All four mandatory buffers are preclaimed, then Forge/Cassia/Yeast/Meteor allocate by BFS distance and sector index; previous witness claims and other mandatory owners cannot be crossed or claimed.
- Completed witnesses contain footprint and mandatory buffer, are cardinal-connected, exact target-sized, sorted/read-only, and pairwise disjoint.
- Independent capacity pressure rejects with exact reachable-count shortfall. Joint pressure passes all independent gates and rejects at the first canonical disjoint failure with no partial approval and exact witness shortfall/retry facts.
- `Completed`: approval/diagnostics present, retry false, errors/rejections `0`.
- `CapacityRejected`: approval null, diagnostics present, retry true, errors `0`, sorted/deduplicated rejections non-empty.
- `InvalidInput`: approval/diagnostics null, retry false, rejections `0`, sorted/deduplicated errors non-empty.
- Diagnostics preserve exact canonical site order, selected/capacity/reserved totals, flood/witness totals, stage-appropriate actual capacity, and shortfall.

## STARTER / DETERMINISM / OWNERSHIP

- Full fresh MAP03_06 plans for seeds `0`, `4660`, and `ulong.MaxValue`: selected placements `6`, source options `3156`, RNG draws before/after checker `3156/3156`, witnesses `4`, each target `5`, total witness sectors `20`, overlap `0`, Village `0`.
- Same plan and requirements across fresh/reused checker `100` runs produce identical status/witness/reachable/diagnostic/rejection/error snapshots.
- Reversed requirement arrays/lists and `en-US`/`tr-TR` cultures produce identical output. Caller collection mutation after return does not affect published read-only snapshots.
- Checker reads no Registry, RNG, clock, filesystem, Unity object/lifecycle, Root/pass state, Village, CoreBiomeSeed, final reservation, patch growth, or later Task dependency.
- Public setters/fields, mutable public collections, lazy public output enumeration, and static mutable caches: `0`.

## TEST

- Final `CoreCapacityFloodCheckerTests`: `215/215 PASS`, failed `0`, skipped `0`; job `87bfe2f512614e34bdbc36ba1b6d3080`.
- MAP03_06 `SiteReservationBacktrackerTests`: `248/248 PASS`; job `380d1132ffa4479bb9beb9ab6df90333`.
- MAP03_05 `SiteCandidateCostTests`: `270/270 PASS`; job `d4e7c0f0f6c345508f1a88a03cd1ec2d`.
- MAP03_04 `SiteDistanceIndexTests`: `239/239 PASS`; job `b5a9f4c925c64f9d9fa3fe850cb865de`.
- MAP03_03 `FootprintPlacementSolverTests`: `170/170 PASS`; job `d1e81ee599b1411593defd8ca6fc65de`.
- MAP03_02 `SiteCandidateEnumerationTests`: `268/268 PASS`; job `82dbc9044d0d4f56ad705d296084604c`.
- MAP03_01 `SiteReservationModelsTests`: `81/81 PASS`; job `7b419c9d3f804db7bd655ae0e059d4bf`.
- Approved MAP02 phase aggregate remains exact `667/667 PASS` (`647` Runtime plus `20` overlay companion), confirmed by the prior PASS result and the final targeted/full regression runs.
- `SpecialVillageDefinitionBuilderTests`: `57/57 PASS`; job `0ee1ca0bb6624ddea8f847c569b76352`.
- `BiomeBoundaryDefinitionBuilderTests`: `38/38 PASS`; job `5f84663ac83145a88e8b7944e00b90ce`.
- `StaticDataRegistryBuilderTests`: `53/53 PASS`; job `b840dcb098594818bee224b6dd425c80`.
- `ContentVersionHashCalculatorTests`: `54/54 PASS`; job `e952d4f43a134cd39cbf5ded1661f0a5`.
- Final targeted `Game.Map.Tests.EditMode`: `3005/3005 PASS`, failed `0`, skipped `0`; job `acbf5867bcaf4e9792e0315a68a5e46a`.
- Final full project EditMode: `3045/3045 PASS`, failed `0`, skipped `0`; job `adc3065b34b745049f861d4539197a81`.
- PlayMode: `NOT RUN` per Task scope. Visual: `NOT APPLICABLE`.

## UNITY GATE

- Unity `6000.3.8f1`, MCP instance `Constant@ced6e0dfc4a31d45`.
- Forced script refresh and compilation completed; compile errors `0`.
- Final Console after test-run noise clear: errors `0`, warnings `0`; relevant new warnings `0`.
- Editor not playing, not compiling, not updating assets; final refresh returned `idle`.

## META / AUTHORING / CHANGE SCOPE

- Assets meta before: `3036`; after: `3045`.
- New matching `.cs.meta`: `9/9`, each `fileFormatVersion: 2`, `MonoImporter`, lowercase non-zero unique 32-hex GUID.
- Duplicate GUID groups across Assets: `0`.
- Authoring CSV/meta recursive counts: `50/50`, unchanged.
- Authoring CSV canonical aggregate path/hash: `378648cc026c688cf41218740e013ebb4f75123d7f1eb42f0b0961f6234fbc8b`, unchanged from the exact prior PASS baseline; marker audit confirms no Authoring path changed.
- Applied-patch marker UTC: `2026-08-12T14:10:29.1691846Z`.
- Assets files newer than marker: exact `18`; all are the exact nine new C# destinations and their nine matching meta destinations.
- Existing Assets modification count: `0`.
- Created Assets destinations: `18`; created report: `1`; `PREEXISTING_IDENTICAL`: `0`.

## TASK CHECKLIST

- [x] Exact four requirement and six-step selection-plan structural gates implemented.
- [x] Cardinal 13x13 grid, multi-source Manhattan buffer, outside-edge, blocker, and overlap contracts implemented.
- [x] Independent capacity and deterministic canonical disjoint witness stages implemented.
- [x] Immutable approval/witness/diagnostics/rejection/error/result status contracts implemented.
- [x] Full starter three-seed `4 / 20 / RNG delta 0` and `100`-run determinism gates PASS.
- [x] Focused, regression, targeted, full EditMode, compile, Console, meta, GUID, Authoring, and exact scope gates PASS.
- [x] No existing Assets, Authoring, scene/prefab, package/settings, or future Task work modified.

## NEXT

- Finalize MAP03_07 only. Keep `MAP03_08_IMPLEMENT_VILLAGE_RESERVATION` and every later Task `LOCKED`.

Recommended Commit: `feat(map): validate connected core site capacity`
