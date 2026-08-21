# MAP05_01_BUILD_MANDATORY_TERMINALS Result

TASK: MAP05_01_BUILD_MANDATORY_TERMINALS
STATUS: PASS

## SUMMARY

- Built the immutable P03 `MANDATORY_TERMINALS` artifact from the approved P01 site snapshot and P02 biome publication.
- Published exactly `7 = 1 Start + 6 SiteEntry` terminals in reservation order `0..6`.
- Preserved exact P01 source references, anchors, exterior approaches, entry sides, routes, flags, and world seed.
- RNG draws / source mutations / filesystem reads: `0 / 0 / 0`.
- No route mask, connector tree, router, gateway, loop, graph, CSV, root, retry, overlay, Scene, or Prefab work was started.

## PATCH APPLY

- Applied `MAP05_01_BUILD_MANDATORY_TERMINALS` v1.0.
- Manifest SHA-256: `d28c66ac381e84f992ccda5003c79f6012f37a60e0d9a6616e0ce489f39220a1`.
- Master/Status/Task payloads matched destinations byte-for-byte after apply.
- Patch apply changed no Assets, CSV, C#, tests, asmdef, Scene, or Prefab.

## READ

- Read the mandatory global rule set, Master, current Status, this Task, and the MAP04_11 PASS Result in exact order.
- Read only Task-allowlisted P01/P02 domain/publication code, focused tests/assemblies, matching meta/inventory, and path-only audit data.
- Installed full Map Package v1.0 reference documents were not present; installed Authoring CSV bodies were not read.

## CREATED

- Runtime production C#: `8`.
- Runtime EditMode test C#: `1`.
- Matching `.cs.meta`: `9`.
- Result: `REPORTS/MAP05_01_BUILD_MANDATORY_TERMINALS_RESULT.md`.

## MODIFIED

- Existing Assets / CSV / meta / asmdef / Scene / Prefab: `0`.

## PREEXISTING_IDENTICAL

- Existing P01/P02 public contracts and assembly definitions remained byte-untouched.
- Authoring CSV/meta remained `50/50`.
- Progress test Scene and all accepted legacy meta remained untouched.

## TERMINAL IDS

1. `TERM_00_START`
2. `TERM_01_SITE_MOON_BOSS_VAULT_ENTRY_L`
3. `TERM_02_SITE_MOON_SEAL_FORGE_ENTRY_L`
4. `TERM_03_SITE_CASSIA_SAP_HEART_ENTRY_L`
5. `TERM_04_SITE_DEEP_STAR_YEAST_ENTRY_L`
6. `TERM_05_SITE_MOON_CORE_METEOR_ENTRY_L`
7. `TERM_06_SITE_PRIMARY_VILLAGE_ENTRY_L`

## START TERMINAL

- Exact order/kind: `0 / Start`; anchor and approach are the exact `SiteReservationSnapshot.StartAnchor`.
- Side/socket: `null / empty`; routes `1|2|3`; required/return `true/true`.

## SITE ENTRY TERMINALS

- Exact count/order: `6 / 1..6`.
- Anchor is each source `FootprintSector`; approach is exact `TryGetExteriorSector` output.
- Each approach is world-bound and unreserved; source socket/side/routes/flags are preserved.
- Shared approach identities are representable and never merged or dropped.

## TERMINAL SET

- Exact immutable counters: terminals/start/site entries/required/return `7/1/6/7/7`.
- Ordinal terminal-ID and reservation-ID lookups preserve terminal object identity.
- Duplicate ID/order/reservation identities, missing Start, source mismatch, and reserved approaches are rejected.

## SOURCE IDENTITY

- Source Site snapshot and source Biome publication are preserved by reference.
- P02's published `SourceSiteSnapshot` must be the exact P01 input reference.
- World seed equality and approved P02 `15/15`, `17 patches`, `165/4 sectors` gates are enforced.

## DETERMINISM

- Fresh/reused builder, `en-US`/`tr-TR`, and parallel thread calls produced one exact signature.
- Terminal IDs depend only on reservation order, source definition ID, and entry socket ID.
- RNG method calls/raw draws: `0/0`.

## IMMUTABILITY

- Terminal routes, terminal list, lookups, errors, and diagnostics expose copied read-only state.
- Before/after source signatures matched; source mutation count `0`.
- Static mutable state, cache/current set, Unity lifecycle, clock, filesystem, and culture sorting dependencies: `0`.

## TEST

- `MandatoryTerminalBuilderTests`: `120/120 PASS` (required minimum `>=96`).
- `SiteReservationValidatorTests`: `268/268 PASS`.
- `BiomePatchValidatorTests`: `196/196 PASS`.
- `Map04ExitTests`: `110/110 PASS`.
- Actually executed total: `694/694 PASS`; failed/skipped `0/0`.
- Game.Map targeted discovery arithmetic: `5485` (`5365 + 120`), required `>=5461`.
- Full EditMode discovery: `5596`, required `>=5573`.

## UNITY

- Unity: `6000.3.8f1`.
- Final forced refresh/compile: PASS.
- Compile errors / Console errors / relevant warnings: `0/0/0`.
- PlayMode tests: not required.
- Scene/Prefab changes: `NONE`.

## ASSET META

- Assets meta: `3152 -> 3161`.
- New meta format/GUID: `9/9`; duplicate GUID groups: `0`.
- Authoring CSV/meta: `50/50`.
- Authoring CSV aggregate digest: `4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3`.
- Authoring meta aggregate digest: `a3cebbdd83484bea161983320bd5b3a1756f6c82de774f1dc0508d327c85c291`.

## CHANGE SCOPE

- Exact new Assets changes: `18 = 8 Runtime C# + 1 test C# + 9 meta`.
- Existing Assets modifications / unexpected Assets changes / new folder meta: `0/0/0`.
- Forbidden later-task production symbol audit: `0`.

## OWNERSHIP AUDIT

- Output ownership is `MANDATORY_TERMINALS`; input ownership remains `SITE_RESERVATIONS + BIOME_PATCHES`.
- No input collection or nested list was mutated or cloned into a competing source of truth.

## OUT_OF_SCOPE_FINDINGS

- NONE.

## DONE CONDITIONS

- Contract / compile / focused tests / required regressions / determinism / immutability / meta / change scope: PASS.
- MAP05_01 is eligible for STATUS FINALIZE.

## NEXT

- Finalize MAP05_01 only to COMPLETE and Current Task NONE.
- Keep `MAP05_02_IMPLEMENT_ROUTE_MASK_LOOKUP` LOCKED; do not auto-start it.

Recommended Commit: `feat(map): build immutable mandatory route terminals`
