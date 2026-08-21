TASK: MAP03_03_IMPLEMENT_FOOTPRINT_PLACEMENT_SOLVER
STATUS: PASS

## SUMMARY

- Implemented the deterministic single-option footprint placement boundary for Start, Boss, Forge, and CoreResource candidates.
- Added one shared pure transform rule for footprint coordinates, required-open sides, entry coordinates, and entry sides.
- Added immutable placement entry, placement, blocker, error, and result models.
- Added exact world-bound, occupied-footprint, protected-existing-entry-approach, own-footprint-entry, and occupied-entry-approach rejection gates.
- Distance, altitude, cost, weight, RNG, selection, reservation publication, backtracking, capacity, Village, pass integration, and file I/O remain absent.

## PATCH APPLY

- Applied inbox patch: `MAP03_03_IMPLEMENT_FOOTPRINT_PLACEMENT_SOLVER`, version `1.0`.
- Applied exact manifest copy operations: Master replace, Status replace, Current Task create.
- Payload/destination SHA-256 matched for all `3/3` files.
  - Master: `0e9746b978927a17d139c65680dec4fb95073e40f10ee2bbf9837ae0b6c0fd13`.
  - Status: `95273b744e20e135d9f9bc762db09f4fb5bdb108003c684b9ff811f1db7a55e0`.
  - Task: `9ed61b2b5e7ab4898af25f0a8b25eeab8042588df345f02670b9a968400f7e6e`.
- Manifest SHA-256: `67bc0a4d9ff41730cd941ba9be965110a7cfc3c3a316b4feb861dc603b04254e`.
- `.APPLIED` records exact `PATCH_ID`, `PATCH_VERSION`, `TASK_KEY`, `TASK_PATH`, `MANIFEST_SHA256`, and `TASK_SHA256` binding.
- Post-apply state was `37 COMPLETE / 1 CURRENT / 167 LOCKED` with only MAP03_03 CURRENT.

## READ

- Read the MCP entrypoint, global locked/work/CSV/Unity/change/patch/finalize rules, Master backlog, Status, this Task, and MAP03_02 Result in the mandated order.
- Read only the Current Task allowlisted typed definition, grid, reservation, candidate, focused-test, assembly, matching-meta, and permitted inventory/audit boundaries.
- The optional Map Package exact document/CSV tree was not installed; the Task's frozen contracts and immutable typed-definition builder output were authoritative.
- Authoring CSV bodies, MAP03_04 or later Task bodies, unrelated production/test bodies, Legacy generator bodies, and Scene/Prefab YAML were not read.

## PRIOR GATE

- MAP03_02 Result exact `STATUS: PASS`: confirmed.
- Previous `SiteCandidateEnumerationTests`: `268/268 PASS`.
- Previous `SiteReservationModelsTests`: `81/81 PASS`.
- Previous MAP02 phase focused aggregate: `667/667 PASS`.
- Previous targeted/full EditMode: `1863/1863` and `1903/1903 PASS`.
- Previous groups/candidates: `6 / 933`; final Assets meta: `3005`; existing Assets modifications: `0`.

## CREATED

Runtime production C# and matching meta:

- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteFootprintTransformer.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteFootprintTransformer.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacementEntry.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacementEntry.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacement.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacement.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacementBlockers.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacementBlockers.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacementResult.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacementResult.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacementSolver.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/FootprintPlacementSolver.cs.meta`

Focused test and matching meta:

- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/FootprintPlacementSolverTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/FootprintPlacementSolverTests.cs.meta`

Documentation:

- This Result file.

## MODIFIED

- Existing Assets production/test/meta/asmdef/asmref files: `0`.
- Authoring/generated CSV/meta: `0`.
- Scene/Prefab/Package/ProjectSettings: `0`.
- Status and Master were not modified during Task execution; they are reserved for Phase C finalization after this PASS Result.

## PREEXISTING_IDENTICAL

- None. All exact fourteen permitted Asset destinations were absent before implementation.

## TRANSFORM TABLE

The same source dimensions `W x H` are retained for every transform.

| Transform | Coordinate | Side mapping | Focused evidence |
|---|---|---|---:|
| `R0` | `(x, y)` | unchanged | PASS |
| `MirrorX` | `(W - 1 - x, y)` | `L<->R`; `U/D` unchanged | PASS |
| `MirrorY` | `(x, H - 1 - y)` | `U<->D`; `L/R` unchanged | PASS |
| `R180` | `(W - 1 - x, H - 1 - y)` | `L<->R`; `U<->D` | PASS |

- Exact asymmetric `3x2` coordinate table: `24/24 PASS`.
- Exact four-transform/four-side table: `16/16 PASS`.
- Invalid dimensions, source coordinates, transform enum, and side enum return `false` without clamp, wrap, or dimension swap.
- Footprint cells, required-open sides, entry local coordinates, and entry sides use the same transformer.
- Cell role, biome ID, recipe ID, socket ID, route types, required flag, and return-path flag are preserved.

## STARTER EVALUATION MATRIX

Empty blockers and raw candidates were evaluated in exact `R0, MirrorX, MirrorY, R180` caller order.

| Group | Evaluations | Success | `FootprintOutsideWorld` | `EntryOutsideWorld` |
|---|---:|---:|---:|---:|
| Start | 88 | 88 | 0 | 0 |
| Boss | 676 | 572 | 52 | 52 |
| Forge | 676 | 624 | 0 | 52 |
| Cassia Sap Heart | 676 | 624 | 0 | 52 |
| Deep Star Yeast | 676 | 624 | 0 | 52 |
| Moon Core Meteor | 676 | 624 | 0 | 52 |
| **Total** | **3468** | **3156** | **52** | **260** |

- Total rejections: `312`.
- All other rejection codes in the starter matrix: `0`.
- Boss raw origin `(12,12)` / index `168`, all four transforms: only `FootprintOutsideWorld`.
- Boundary entry exteriors never clamp or wrap.

## COLLISION / BLOCKER / SOURCE VALIDATION

- Placement precedence is Phase 1 source/transform, Phase 2 footprint world placement, then Phase 3 entry approaches.
- Phase 2 exact buckets: `FootprintOutsideWorld`, `FootprintOverlap`, `BlocksExistingEntryApproach`.
- Phase 3 exact buckets include `EntryNotOnFootprint`, `DuplicateEntryFace`, `EntryOutsideWorld`, `EntryFacesOwnFootprint`, and `EntryApproachOccupied`.
- Candidate entry exterior equal to a protected existing approach is allowed; exact footprint/protected overlap is rejected.
- Adjacent occupied sectors and shared candidate exterior sectors are allowed absent an exact prohibited collision.
- `FromReservations` validates nulls, duplicate reservation IDs, overlapping footprints, out-of-world entry exteriors, and occupied/protected inconsistency; shared valid approaches are deterministically deduplicated.
- Missing/null/mismatched/invalid candidate, blocker, map, footprint, entry, parent identity, route, side, and required-entry inputs return sorted failure results with no partial placement.

## RESULT / ERROR CONTRACT

- `FootprintPlacementErrorCode` has the exact frozen 24-value ordinal order.
- Errors preserve canonical-or-empty source/socket IDs, exact relevant sector index or `-1`, and stable messages.
- Errors sort by code, source ID, entry ID, sector index, and message; duplicate logical errors are removed deterministically.
- Success publishes one non-null immutable placement and zero errors.
- Failure publishes no placement and one or more copied read-only errors.

## DETERMINISM / IMMUTABILITY / OWNERSHIP

- Occupied sectors are copied and sorted by `WorldGridIndex`; entries are copied and sorted by socket ID.
- Blocker lists and route lists are unique, ascending, copied, and read-only.
- Reversed definition and blocker inputs produce the same observable placement/error snapshot.
- Fresh/reused solver behavior across 100 runs and `en-US` / `tr-TR` cultures is stable.
- Caller collection mutation cannot alter blockers, entries, placements, or results.
- Public setters/fields, mutable collection exposure, static mutable cache, Unity object/lifecycle, and lazy public enumeration: `0`.
- RNG, distance, cost, weight, selection, backtracking, capacity, Village, pass, and file-I/O production dependency audit matches: `0`.

## TEST

- Final new `FootprintPlacementSolverTests`: `170/170 PASS`, failed `0`, skipped `0`; job `df05611a001d43c491221e0d9d6c0646`.
- MAP03_02 `SiteCandidateEnumerationTests`: `268/268 PASS`; job `3b51e5d9d3434bd194110d0eb53dcf8f`.
- MAP03_01 `SiteReservationModelsTests`: `81/81 PASS`; job `237c59f67562446caa6395536d5c1233`.
- MAP02 Runtime focused fixtures: `647/647 PASS`.
  - `56`: job `d21f016f4cbc4b0798117cf670ba0c40`.
  - `103`: job `991b3bb206664401b94020c230482880`.
  - `90`: job `7ffa1ef30e9c4204988ecf3dabe46545`.
  - `84`: job `ed7e73a7416542c29f13d139fb9e5983`.
  - `77`: job `b9add9caa0184b96be86268f1092fb58`.
  - `97`: job `231bc7cc5f6c48afa4b02d3187f3550f`.
  - topology Runtime `68`: job `2297ac3e3dce4a009819fbe315e2b001`.
  - exit `72`: job `06f2257800b84518817c90f48bf6380e`.
- Approved MAP02 phase aggregate remains `667/667 PASS`; the additional topology Editor `20` are covered by the final full-project run.
- `SpecialVillageDefinitionBuilderTests`: `57/57 PASS`; job `422b595988544b5ea9c373d92a72b197`.
- `BiomeBoundaryDefinitionBuilderTests`: `38/38 PASS`; job `346fa4c77cc54e7eb33042f61c0ec389`.
- `StaticDataRegistryBuilderTests`: `53/53 PASS`; job `8e0e6333920046acb27493a1279a2533`.
- `ContentVersionHashCalculatorTests`: `54/54 PASS`; job `6a563642190b452ca1cfcb85f84781d4`.
- Final targeted `Game.Map.Tests.EditMode`: `2033/2033 PASS`, failed `0`, skipped `0`; job `88aad6d1fa8c4f3496f10e08b8e7b98f`.
- Final full project EditMode: `2073/2073 PASS`, failed `0`, skipped `0`; job `4367484fc173455c8d5d6595a9ebdc7a`.
- PlayMode: NOT RUN per Task scope.
- Visual: NOT APPLICABLE.

## UNITY

- Active instance: `Constant@ced6e0dfc4a31d45`.
- Unity: `6000.3.8f1`.
- Final forced asset/script refresh and requested compilation: PASS.
- Compile errors: `0`; relevant project-code warnings: `0`.
- Final isolated Console errors/warnings after clear: `0/0`.
- Final Editor state: not playing, not compiling, no domain reload pending.
- Saved Scene/Prefab changes: NONE.

## ASSET / META / AUTHORING

- Before Assets meta: `3005`; after Assets meta: `3012`.
- New matching `.cs.meta`: `7/7` with `fileFormatVersion: 2`, `MonoImporter`, unique lowercase non-zero 32-hex GUIDs.
- Invalid/missing GUID rows: `0`; duplicate GUID groups: `0`.
- Authoring CSV/meta: `50/50` unchanged.
- Authoring aggregate path/hash baseline before and after: `3387d5f899db12cb2cd73b1a0fa67b5a2d431fa63d063cc728d87a50e42f084c`.

## CHANGE SCOPE

- Assets files newer than the original patch marker: exact `14`.
- New Runtime production C#: `6`; matching meta: `6`.
- New focused test C#: `1`; matching meta: `1`.
- Existing Assets modifications: `0`; unexpected Assets changes: `0`; deleted Assets: `0`.
- New directory/folder meta: `0`.
- asmdef/asmref, Authoring/generated CSV/meta, Scene/Prefab, Package, and ProjectSettings changes: `0`.
- Git commit/push/branch/reset/rebase: not performed.

## DONE CONDITIONS

- [x] Patch prerequisites, exact copy operations, hash identity, Current Task binding, and `.APPLIED` binding PASS.
- [x] Exact four transforms, unchanged dimensions, coordinate/side table, and shared transform path PASS.
- [x] Immutable placement entry/placement/blocker/error/result contracts PASS.
- [x] Exact starter `3468 / 3156 / 312` matrix and `52 / 260` rejection breakdown PASS.
- [x] Source, world-bound, collision, protected approach, entry exterior, and precedence gates PASS.
- [x] Determinism, culture, input order, caller ownership, and public mutation-surface gates PASS.
- [x] No MAP03_04 distance/index or later-task work introduced.
- [x] Focused, regression, targeted, full EditMode, compile, Console, meta, GUID, Authoring hash, and exact change-scope gates PASS.

## NEXT

- Finalize only `MAP03_03_IMPLEMENT_FOOTPRINT_PLACEMENT_SOLVER` as COMPLETE and set Current Task to NONE.
- Keep `MAP03_04_IMPLEMENT_SITE_DISTANCE_INDEX` LOCKED.
- Await a separate future patch; do not auto-start the next Task.

## Recommended Commit

`feat(map): solve transformed site footprint placements`
