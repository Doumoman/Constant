TASK: MAP03_02_ENUMERATE_START_AND_SITE_CANDIDATES
STATUS: PASS

## SUMMARY

- Implemented deterministic raw-origin enumeration for Start, Boss, Forge, and the three fixed CoreResource sites over the approved 13 x 13 / 169-sector P00 grid.
- Added an immutable candidate, group, catalog, error, and result boundary without footprint placement, transform, collision, distance scoring, RNG selection, or backtracking.
- Exact output is six groups and 933 candidates: Start 88 plus five special-site groups of 169 each.
- Existing production, tests, asmdef/asmref, Authoring data, Scene/Prefab, Package, and ProjectSettings assets were not modified.

## PATCH APPLY

- Applied inbox patch: `MAP03_02_ENUMERATE_START_AND_SITE_CANDIDATES`, version `1.0`.
- Applied exact manifest copy operations: Master replace, Status replace, Current Task create.
- Payload/destination SHA-256 matched for all `3/3` files.
  - Master: `682FA0203492003BBAFDB4DA124E3D55C3596299360C7A69EBB7AE0B0BC63ED0`.
  - Status: `3D7D02BA610EF7187AC776BE5F08902D9967F2A0C4B3231E56F94FA1229D4364`.
  - Task: `CE05EF0B8D55A37F260BE4549AB296E08226E6414745523B5231055B02AB8360`.
- Marker created: `MapDesign/MCP_INBOX/MAP03_02_ENUMERATE_START_AND_SITE_CANDIDATES/.APPLIED`.
- Post-apply state was `36 COMPLETE / 1 CURRENT / 168 LOCKED` with only MAP03_02 CURRENT.

## READ

- Read the MCP entrypoint, global locked/work/CSV/Unity/change/patch/finalize rules, Master backlog, Status, this Task, and MAP03_01 Result in the mandated order.
- Read only the Current Task allowlisted typed data/grid/reservation APIs, tests/assemblies, matching meta, and permitted path-only inventories/audits.
- The complete optional Map Package document tree was not installed; this Task's frozen contracts were used as the authoritative fallback.
- MAP03_03 or later Task bodies, unrelated production/test bodies, Legacy generator bodies, and Scene/Prefab YAML were not read.

## MASTER BACKLOG CHECK

- Master rows / unique task IDs: `205 / 205`.
- MAP00: `10/10 COMPLETE`; MAP01: `17/17 COMPLETE`; MAP02: `8/8 COMPLETE`; MAP03_01: `COMPLETE`.
- MAP03_02 was the exact single Current Task; MAP03_03 and all later tasks remained LOCKED.
- Auto-start remained `NO`.

## MAP03_01 GATE CHECK

- Prior Result exact `STATUS: PASS`: confirmed.
- `SiteReservationModelsTests`: `81/81 PASS`, failed `0`, skipped `0`.
- Previous targeted `Game.Map.Tests.EditMode`: `1595/1595 PASS`.
- Previous full project EditMode: `1635/1635 PASS`.
- Previous final Assets meta: `2998`; existing Assets modifications: `0`.

## CREATED

Runtime production C# and matching meta:

- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteOriginCandidate.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateGroup.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCatalog.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateEnumerationError.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateEnumerationResult.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateEnumerator.cs`
- Matching `.cs.meta`: `6/6`.

Focused test and matching meta:

- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteCandidateEnumerationTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteCandidateEnumerationTests.cs.meta`

Documentation:

- This Result file.

## MODIFIED

- Existing Assets production/test/meta/asmdef/asmref files: `0`.
- Authoring/generated CSV/meta: `0`.
- Scene/Prefab/Package/ProjectSettings: `0`.
- Status was not modified during Task execution.

## PREEXISTING_IDENTICAL

- None. All exact fourteen permitted Asset destinations were absent before implementation.

## INPUT GATE

- Null grid/profile/special-map inputs return accumulated deterministic errors and no partial catalog.
- Exact 169 cell/index/coordinate/neighbor identity is checked without cloning or mutating grid data.
- Active world/generation profiles, ordinal world identity, exact fixed dimensions, and exact starter ring `0..1` are enforced.
- Special-map IDs are ordinal-unique; inactive maps are excluded and active Village definitions are validly excluded.
- The exact five required site identities, roles, required count `1`, biome, footprint dimensions, non-negative distances, and unique route types `1|2|3` are validated.
- Missing, inactive, duplicate, null, unexpected required, role/count mismatch, and invalid definitions return failure results.

## SITE ORIGIN CANDIDATE

- Immutable identity is exact: kind, canonical source ID, instance ordinal, grid origin/index, edge ring, and candidate ordinal.
- `OriginIndex == WorldGridIndex.ToIndex(Origin)` and exact edge-ring identity are constructor invariants.
- No reservation ID, footprint transform/cells, entry, score, cost, weight, or selection flag is present.

## CANDIDATE GROUP

- Exact placement priorities are Start `0`, Boss `10`, Forge `20`, and CoreResource `30`; Village groups are rejected.
- Candidate identity must match the group and origin/index values must be unique.
- Caller order is copied into an OriginIndex-ascending read-only snapshot, with candidate ordinal matching exact position.
- Ordinal and origin lookups are stable and expose no mutable collection.

## CANDIDATE CATALOG

- Exact group order and identity:
  1. `START / WORLD_MOONPALACE_V1 / 0`
  2. `BOSS / SITE_MOON_BOSS_VAULT / 0`
  3. `FORGE / SITE_MOON_SEAL_FORGE / 0`
  4. `CORE_RESOURCE / SITE_CASSIA_SAP_HEART / 0`
  5. `CORE_RESOURCE / SITE_DEEP_STAR_YEAST / 0`
  6. `CORE_RESOURCE / SITE_MOON_CORE_METEOR / 0`
- Group keys are unique and ordered by placement priority, source ID ordinal, and instance ordinal.
- Seed and profile identities are copied exactly; Start/Site/All group views are defensive read-only snapshots.

## START ENUMERATION

- Exact edge-ring distribution over all 169 origins: `48 / 40 / 32 / 24 / 16 / 8 / 1` for rings `0..6`.
- Starter membership is rings `0..1`: ring 0 `48`, ring 1 `40`, total `88`.
- All four corners are included; rings 2 through 6 are excluded; missing/extra/duplicate origins are `0/0/0`.
- Candidate order is exact ascending `WorldGridIndex` among matching origins.

## SPECIAL SITE ENUMERATION

- Boss: `169/169`; Forge: `169/169`.
- Cassia Sap Heart: `169/169`; Deep Star Yeast: `169/169`; Moon Core Meteor: `169/169`.
- Special-site raw origins: `845`; Start: `88`; total: `933`; groups: `6`; Village: `0`.
- Boss raw boundary origin `(12,12)` / index `168` is retained, proving no footprint/world-bound filtering occurred.

## ERROR AND RESULT CONTRACT

- All sixteen required error codes are present in the frozen ordinal order.
- Each error preserves its exact code, canonical-or-empty source ID, and stable non-empty message.
- Errors are copied and sorted by source ID ordinal, code ordinal, then message ordinal.
- Success publishes one non-null catalog and zero errors; failure publishes no catalog and one or more errors.

## DETERMINISM AND OWNERSHIP

- Reversed/shuffled array/list inputs produce identical membership and order.
- Seeds `0`, `4660`, and `ulong.MaxValue` are preserved without changing candidates.
- Fresh/reused enumerators for 100 runs and `en-US`/`tr-TR` cultures produce identical output.
- Caller collections remain unmodified, and completed output remains stable after caller-list mutation.
- RNG streams/draws, wall clock, frame, thread, filesystem, lazy public enumeration, and mutable public state: `0`.

## TEST

- New `SiteCandidateEnumerationTests`: `268/268 PASS`, failed `0`, skipped `0` (minimum 72 exceeded).
- MAP03_01 `SiteReservationModelsTests`: `81/81 PASS`.
- MAP02 Runtime focused fixtures: `647/647 PASS`; approved MAP02 phase focused aggregate gate remains `667/667 PASS`.
- `SpecialVillageDefinitionBuilderTests`: `57/57 PASS`.
- `BiomeBoundaryDefinitionBuilderTests`: `38/38 PASS`.
- `StaticDataRegistryBuilderTests`: `53/53 PASS`.
- `ContentVersionHashCalculatorTests`: `54/54 PASS`.
- Targeted `Game.Map.Tests.EditMode`: `1863/1863 PASS`, failed `0`, skipped `0` (`1595 + 268`).
- Full project EditMode: `1903/1903 PASS`, failed `0`, skipped `0` (`1635 + 268`).
- PlayMode: NOT RUN per Task scope; Visual: NOT APPLICABLE.

## UNITY

- Active instance: `Constant@ced6e0dfc4a31d45`.
- Unity: `6000.3.8f1`.
- Forced asset/script refresh and requested compilation: PASS.
- Compile errors: `0`; relevant/project-code warnings: `0`; final isolated Console errors/warnings: `0/0`.
- Test Runner pre/post informational warnings and one MCP transport warning were external to project code and cleared before the final Console read.
- Final editor state: idle, not playing, not compiling, no domain reload pending, ready for tools.
- Saved Scene/Prefab changes: NONE.

## ASSET META VALIDATION

- Authoring CSV/meta: `50/50`; files newer than the Task marker: `0/0`; approved prior byte/hash baseline preserved.
- Accepted legacy `Editor.meta`: `6/6`, all older than the Task marker.
- New matching `.cs.meta`: `7/7`, exact `fileFormatVersion: 2`, unique lowercase 32-hex GUIDs.
- Final Assets meta: `3005`; invalid/missing GUID rows: `0`; duplicate GUID groups: `0`.

## CHANGE SCOPE

- Assets files newer than the patch marker: exact `14`.
- New Runtime production C#: `6`; matching meta: `6`.
- New focused test C#: `1`; matching meta: `1`.
- Existing Assets modifications: `0`; unexpected Assets changes: `0`; new directory/folder meta: `0`.
- asmdef/asmref, Authoring/generated CSV/meta, Scene/Prefab, Package, and ProjectSettings changes: `0`.
- Git commit/push/branch/reset/rebase: not performed.

## PRODUCTION OWNERSHIP AUDIT

- Runtime namespace: exact `StarNight.Map.WorldGeneration.Generation`; existing `Game.Map.Runtime` assembly reused.
- Test namespace: exact `StarNight.Map.Tests.WorldGeneration.Generation`; existing `Game.Map.Tests.EditMode` assembly reused.
- New asmdef/asmref: `0`.
- Public setters/fields, exposed mutable collections, static mutable fields, Unity object/lifecycle, reflection factory, singleton/service locator: `0`.
- RNG draw/dependency, transform application, footprint placement, collision/occupancy solving, distance/cost/weight scoring, backtracking/retry, Core capacity flood, village candidate generation, pass integration, serializer/file I/O: `0`.

## OUT_OF_SCOPE_FINDINGS

- None.

## DONE CONDITIONS

- [x] Patch prerequisites, exact copy operations, hash identity, and Current Task transition PASS.
- [x] Input gates and immutable candidate/group/catalog/error/result contracts PASS.
- [x] Exact six-group identity/order, ring counts, five `169/169`, total `933`, and raw boundary retention PASS.
- [x] Determinism, culture, seed, input order, ownership, and public mutation-surface gates PASS.
- [x] No later-task placement/solver/RNG/pass/serializer/visual work introduced.
- [x] Focused, prior-model, MAP02/data regressions, targeted, full EditMode, compile, Console, meta, GUID, and exact change-scope gates PASS.

## NEXT

- Finalize only `MAP03_02_ENUMERATE_START_AND_SITE_CANDIDATES` as COMPLETE and set Current Task to NONE.
- Keep `MAP03_03_IMPLEMENT_FOOTPRINT_PLACEMENT_SOLVER` LOCKED.
- Await a separate future patch; do not auto-start the next Task.

## Recommended Commit

`feat(map): enumerate deterministic site origins`
