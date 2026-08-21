STATUS: PASS

# MAP03_08_IMPLEMENT_VILLAGE_RESERVATION Result

## SUMMARY

- Implemented immutable Village distance-bucket, candidate, diagnostics, rejection, error, selection, approval, result, and selector contracts.
- Implemented RNG-free accumulated structural preflight followed by the exact continued-site-stream schedule: bucket `NextInt(100)`, viable-layout weighted `NextInt`, and selected-layout candidate `NextInt`.
- Implemented canonical rectangular candidate enumeration, lower-median entry anchors, first-failure footprint/entry/witness/distance filtering, no bucket fallback, and retry-required selected-bucket exhaustion publication.
- Added `339` actual NUnit cases covering exhaustive bucket rolls, strict parser vectors, candidate world/rectangle invariants, enums/status, invalid-input publication, exact starter conservation, RNG schedule, identity, immutability, culture/order stability, 100-run determinism, and three full-starter continued-stream seeds.

## READ / PATCH BINDING

- Entrypoint, apply-and-run pipeline, global rules, Master backlog, Status, Current Task, and MAP03_07 Result were read in the mandated order and only within the Task READ ALLOWLIST.
- Applied patch: `MAP03_08_IMPLEMENT_VILLAGE_RESERVATION`, version `1.0`.
- `.APPLIED` manifest hash: `2524265131f0b63bcbf10088ab8c07ed5efc7d3c064469f847f98805f7bae101`.
- `.APPLIED` Task hash: `9746f88ea5de3047434774cbc99e49c1451c40cb99229568f8dbadc54e313876`.
- Current Task/status binding before finalization: exact `MAP03_08_IMPLEMENT_VILLAGE_RESERVATION` / `CURRENT`.
- Patch destination collisions or `PREEXISTING_IDENTICAL` Task outputs: `0`.

## CREATED

Runtime C# and matching meta:

- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageDistanceBucket.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageDistanceBucket.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageReservationCandidate.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageReservationCandidate.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageReservationDiagnostics.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageReservationDiagnostics.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageReservationRejection.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageReservationRejection.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageReservationError.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageReservationError.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageReservationSelection.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageReservationSelection.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageReservationResult.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageReservationResult.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageReservationSelector.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/VillageReservationSelector.cs.meta`

EditMode test and matching meta:

- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/VillageReservationSelectorTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/VillageReservationSelectorTests.cs.meta`

Report:

- `MapDesign/MCP/REPORTS/MAP03_08_IMPLEMENT_VILLAGE_RESERVATION_RESULT.md`

## MODIFIED

- Existing Assets production/test/meta/asmdef/asmref files: `0`.
- Authoring/generated CSV/meta: `0`.
- Scene/Prefab/Package/ProjectSettings: `0`.
- Existing MAP03_01 through MAP03_07 implementation and tests: `0`.
- Status/Master were not modified during Task execution; Status is reserved for Phase C finalization after this PASS Result.

## PROFILE / SPECIAL-MAP / ENTRY / LAYOUT / BUCKET IDENTITY

- Profile preflight requires exact active `VIL_MOON_PRIMARY`, world `WORLD_MOONPALACE_V1`, facilities `5..6`, maximum sector count `2`, canonical unique allowed layouts, and exact supplied-layout coverage.
- Special-map preflight requires exact active `SITE_PRIMARY_VILLAGE`, role `VILLAGE`, required count `1`, mode `VILLAGE_LAYOUT`, empty primary biome, Start minimum `0`, other-core minimum `2`, and routes `1|2|3`.
- Entry preflight requires exact one `ENTRY_L` owned by `SITE_PRIMARY_VILLAGE`, local `(0,0)`, source side `L`, required/return true, and routes `1|2|3`.
- Starter layouts preserve exact identities `VLAY_STANDARD_5_A` / `VLAY_STANDARD_6_A`, `1x1`, targets `5/6`, entry sides `L|R`, and weights `100/70`.
- Layout validation accepts only supported `1x1`, `2x1`, or `1x2` rectangles with area at most `2`, positive weight, active state, in-profile facility target, and canonical unique entry sides.
- Strict bucket parser accepts only `2-3:20|4-6:50|7-10:30`; ASCII decimal grammar, leading-zero/sign/whitespace/overflow/delimiter/order/overlap/gap/weight/total variants are rejected.
- Exhaustive rolls `0..99` map exactly to bucket ordinals/ranges/weights `0 / 2..3 / 20`, `1 / 4..6 / 50`, and `2 / 7..10 / 30`.

## RNG / CANDIDATE ENUMERATION EVIDENCE

- Structural invalid input returns before RNG consumption with approval/diagnostics absent, retry false, and sorted/deduplicated errors.
- Completed calls use exactly three `NextInt` method calls in order: bucket `100`, viable-layout weight sum, selected-layout viable-candidate count. No modulo, float scaling, alternate RNG, redraw, or bucket fallback is used.
- Full-starter seeds `0`, `4660`, and `ulong.MaxValue` continue the exact MAP03_06 site stream at draw `3156` and finish at draw `3159`; MAP03_07 consumes no draw.
- Candidate enumeration is canonical by layout ID ordinal, row-major origin index, then sides `L,R,U,D`. Origins cover `y=0..13-H`, `x=0..13-W`; occupied cells cover the complete rectangle.
- Entry anchors use the frozen lower-median rule and exact one-step side exterior. World-outside exteriors are counted and never published as candidates.
- Starter raw/source/entry-out counts are exact `676 / 624 / 52` across two `1x1`, `L|R` layouts.
- `CandidateOrdinal` follows the global canonical world-bound source order and is independent of caller collection order. Occupied indices are copied, sorted, unique, and exactly `W*H`.

## FILTER / SELECTION / PUBLICATION EVIDENCE

- Existing occupied sectors are the six selected footprints; existing entry approaches are the six-placement entry exteriors; protected sectors are the exact four MAP03_07 witnesses.
- Candidate first-failure order is exact: footprint overlap, protected witness, blocked existing approach, occupied Village exterior, other non-Start site distance `<2`, then selected Start-bucket mismatch.
- Start distance is minimum cardinal Manhattan distance between complete Village and Start footprints. Other-site distance uses complete footprints and excludes Start from the `<2` gate.
- Shared existing/Village entry exteriors and witness-only Village entry exteriors remain allowed; no diagonal halo, route-mask, altitude, quadrant, or later-task rule is introduced.
- Per-layout diagnostics conserve `EntryOutside + Source == Raw` and all six source-filter reasons plus viable count equal Source. Layout diagnostics are sorted by layout ID and copied read-only.
- Only layouts with viable candidates enter the weight table with original weights; the chosen layout candidate uses a uniform index into its canonical viable list.
- Completed publication preserves exact profile/special-map/entry/layout/bucket/candidate references, original `CoreCapacityApproval` identity, existing site count `6`, witness count `4`, total selected sites `7`, and one starter Village occupied sector.
- Selected-bucket exhaustion publishes no partial approval, exact one `SelectedBucketHasNoViableCandidate` rejection, diagnostics with method-call count `1`, later rolls `-1/-1`, and retry true.
- Status, error, candidate-rejection, and reservation-rejection enum orders match the frozen contract. Results sort/deduplicate stable errors/rejections and expose no mutable public collections.

## STARTER / DETERMINISM / OWNERSHIP

- Full fresh starter seeds `0`, `4660`, and `ulong.MaxValue`: existing selections `6`, Village selections `1`, witnesses `4`, witness sectors `20`, selected-footprint overlap `0`, protected-witness overlap `0`, entry conflict `0`, and completed RNG calls `3`.
- Same definitions/approval/continued state produce the same bucket, layout, origin, entry side, candidate ordinal, rolls, and post-call RNG count across fresh/reused selector runs.
- Reversed entry/layout caller collections and `en-US`/`tr-TR` cultures produce identical canonical snapshots. Caller collection mutation after return cannot change published output.
- Selector reads no Registry singleton, filesystem, CSV, Root/pass state, Unity object/lifecycle, internal Village layout cells/facilities, final reservation tables, biome pass, or later Task dependency.
- Public setters/fields, mutable public collection exposure, lazy public output enumeration, and production static mutable state: `0`.

## TEST

- Final `VillageReservationSelectorTests`: `339/339 PASS`, failed `0`, skipped `0`; job `7b5d4b0cf55346be8870298040c9a903`.
- MAP03_07 `CoreCapacityFloodCheckerTests`: `215/215 PASS`; job `39c41fcae94f437aab82adb886ed62cc`.
- MAP03_06 `SiteReservationBacktrackerTests`: `248/248 PASS`; job `e7d16e96d6a04cd4ae85afb4eecc32bf`.
- MAP03_05 `SiteCandidateCostTests`: `270/270 PASS`; job `de82c8cc93254874ac6822f41c0b0eb8`.
- MAP03_04 `SiteDistanceIndexTests`: `239/239 PASS`; job `3ec89db98ae24258935f24a1bd5e479a`.
- MAP03_03 `FootprintPlacementSolverTests`: `170/170 PASS`; job `9b373e7629294b16b2e2187750022f54`.
- MAP03_02 `SiteCandidateEnumerationTests`: `268/268 PASS`; job `8463e9d19ca34bdf916d2009368b3523`.
- MAP03_01 `SiteReservationModelsTests`: `81/81 PASS`; job `ebceac0863eb44e890916b9669293a20`.
- Approved MAP02 phase aggregate remains exact `667/667 PASS`, confirmed by the prior PASS result and final targeted/full regression runs.
- `SpecialVillageDefinitionBuilderTests`: `57/57 PASS`; job `59d33d5f6e244115b71beabe11a67995`.
- `BiomeBoundaryDefinitionBuilderTests`: `38/38 PASS`; job `7b8a40c0c90049f0ac85e14e54bf9be1`.
- `StaticDataRegistryBuilderTests`: `53/53 PASS`; job `d2d727282fa74c19928cc56106029a9e`.
- `ContentVersionHashCalculatorTests`: `54/54 PASS`; job `b64ff04e80114bfdac7e39bc06508c35`.
- Final targeted `Game.Map.Tests.EditMode`: `3344/3344 PASS`, failed `0`, skipped `0`; job `59a04688239342bf819442da83fd57da`.
- Final full project EditMode: `3384/3384 PASS`, failed `0`, skipped `0`; job `fa9d626cdb0c4345bf7476236061019b`.
- PlayMode: `NOT RUN` per Task scope. Visual: `NOT APPLICABLE`.

## UNITY GATE

- Unity `6000.3.8f1`, MCP instance `Constant@ced6e0dfc4a31d45`.
- Forced all-asset refresh and compilation completed; compile errors `0`, relevant new warnings `0`.
- One transient MCP transport warning was observed during refresh; it originated in the MCP package, not Task code. Final Console after clearing tool transport noise: errors `0`, warnings `0`.
- Final editor state: idle, ready for tools, not playing, not compiling, no pending domain reload, and not updating assets.

## META / AUTHORING / CHANGE SCOPE

- Assets meta before: `3045`; after: `3054`.
- New matching `.cs.meta`: `9/9`, each `fileFormatVersion: 2`, `MonoImporter`, lowercase non-zero unique 32-hex GUID.
- Duplicate GUID groups across Assets: `0`; invalid/zero GUID meta files: `0`.
- Authoring CSV/meta recursive counts: `50/50`, unchanged.
- Authoring CSV canonical aggregate path/hash: `378648cc026c688cf41218740e013ebb4f75123d7f1eb42f0b0961f6234fbc8b`, unchanged from the exact prior PASS baseline.
- Applied-patch marker UTC: `2026-08-12T16:26:38.5000877Z`.
- Assets files newer than marker: exact `18`; all are the exact nine new C# destinations and their nine matching meta destinations.
- Existing Assets modification count: `0`.
- Created Assets destinations: `18`; created report: `1`; `PREEXISTING_IDENTICAL`: `0`.

## TASK CHECKLIST

- [x] Exact approval/profile/special-map/entry/layout/bucket structural gates implemented before RNG draws.
- [x] Exact strict bucket parser, roll mapping, canonical rectangular enumeration, lower-median entry, and immutable candidate contracts implemented.
- [x] Exact first-failure footprint/witness/entry/distance filtering and no-fallback rejection implemented.
- [x] Exact viable-layout weighted selection, uniform candidate selection, continued RNG schedule, diagnostics, approval, and result publication implemented.
- [x] Full starter three-seed `6+1 / 4 / 20 / overlap 0 / RNG 3156->3159` and 100-run determinism gates PASS.
- [x] Focused, regression, targeted, full EditMode, compile, Console, meta, GUID, Authoring, and exact scope gates PASS.
- [x] No existing Assets, Authoring, scene/prefab, package/settings, or future Task work modified.

## NEXT

- Finalize MAP03_08 only. Keep `MAP03_09_IMPLEMENT_SITE_RESERVATION_VALIDATOR` and every later Task `LOCKED`.

Recommended Commit: `feat(map): reserve deterministic primary village`
