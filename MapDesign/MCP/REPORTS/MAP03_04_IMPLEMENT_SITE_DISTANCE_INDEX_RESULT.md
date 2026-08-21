TASK: MAP03_04_IMPLEMENT_SITE_DISTANCE_INDEX
STATUS: PASS

## SUMMARY

- Implemented an immutable footprint-aware pairwise sector-distance index for `FootprintPlacement` snapshots.
- Implemented deterministic site identity keys, canonical pair records, O(1) pair lookup, sorted build errors, and complete-policy evaluation.
- Implemented the exact typed required-site policy for Start, Boss, Forge, and the three CoreResource sites.
- Cost, weight, RNG, option selection, reservation publication, backtracking, capacity, Village placement, route generation, tile traversal, and file I/O remain absent.

## PATCH APPLY

- Applied inbox patch `MAP03_04_IMPLEMENT_SITE_DISTANCE_INDEX`, version `1.0`.
- Applied exact manifest copy operations: Master replace, Status replace, Current Task create.
- Payload/destination SHA-256 matched for all `3/3` files.
  - Master: `c95662ce4a08eb80a21b4c38137625b4340a80f95eeab810cd585ce5b81b9d6c`.
  - Status: `b5bce5f52b9f697ab070897a19d10b8711a8fd907fbb6afb731928a4c173d35c`.
  - Task: `c540dbfcd5686b4284898e62f653eff653eac15fa27b982c266d6ee665c98d26`.
- Manifest SHA-256: `c4ddc44ffcd00710eddab47cf887e7f632fdd68045201db13bcab89e90a7edca`.
- `.APPLIED` records the exact `PATCH_ID`, `PATCH_VERSION`, `TASK_KEY`, `TASK_PATH`, `MANIFEST_SHA256`, and `TASK_SHA256` binding.
- Post-apply state was `38 COMPLETE / 1 CURRENT / 166 LOCKED` with only MAP03_04 CURRENT; MAP03_05 remained LOCKED.

## READ

- Read the MCP entrypoint, global locked/work/CSV/Unity/change/patch/finalize rules, Master backlog, Status, this Task, and MAP03_03 Result in the mandated order.
- Read only the Current Task allowlisted typed definitions, grid/candidate/reservation/placement models, focused tests, assemblies, matching metas, and permitted path/count/hash audit boundaries.
- The optional Map Package exact reference tree was not installed; the Task's frozen contract and immutable typed definitions were authoritative.
- Authoring CSV bodies, MAP03_05 or later Task bodies, unrelated production/test bodies, Legacy generator bodies, and Scene/Prefab YAML were not read.

## PRIOR GATE

- MAP03_03 Result exact `STATUS: PASS`: confirmed.
- Prior focused/regression counts were `170 / 268 / 81 / 667 PASS`.
- Prior targeted/full EditMode were `2033/2033` and `2073/2073 PASS`.
- Prior Assets meta was `3012`; existing Assets modifications were `0`.

## CREATED

Runtime production C# and matching meta:

- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SitePlacementKey.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SitePlacementKey.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceRecord.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceRecord.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceIndexResult.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceIndexResult.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistancePolicy.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistancePolicy.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceEvaluationResult.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceEvaluationResult.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceIndex.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceIndex.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceIndexBuilder.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteDistanceIndexBuilder.cs.meta`

Focused test and matching meta:

- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteDistanceIndexTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteDistanceIndexTests.cs.meta`

Documentation:

- This Result file.

## MODIFIED

- Existing Assets production/test/meta/asmdef/asmref files: `0`.
- Authoring/generated CSV/meta: `0`.
- Scene/Prefab/Package/ProjectSettings: `0`.
- Status and Master were not modified during Task execution; they are reserved for Phase C finalization after this PASS Result.

## PREEXISTING_IDENTICAL

- None. All exact sixteen permitted Asset destinations were absent before implementation.

## DISTANCE / INDEX EVIDENCE

- `SectorDistance(a,b) = abs(a.X-b.X) + abs(a.Y-b.Y)` on the exact 13x13 P00 sector grid.
- `PlacementDistance(A,B)` is the minimum sector distance across the two copied occupied-footprint cell sets.
- Exhaustive `169 x 169 = 28561` sector-pair reference comparisons, identity, symmetry, cardinal adjacency, diagonal behavior, and maximum distance `24`: PASS.
- Exact vectors `(0,0)-(0,0)=0`, `(0,0)-(1,0)=1`, `(0,0)-(0,1)=1`, `(0,0)-(12,12)=24`, `(2,9)-(11,3)=15`: PASS.
- Sparse and 2x1 footprint minimum-distance vectors: PASS.
- Equal minimum pairs use lexicographically smallest `(FirstSectorIndex, SecondSectorIndex)` in canonical key orientation: PASS.
- Keys are sorted by exact priority `0/10/20/30/40`, ordinal source ID, then required-instance ordinal.
- Empty/single indexes publish `0/0` and `1/0` placement/pair counts; an n-placement index publishes exactly `n*(n-1)/2` records.
- Same existing key returns distance `0` and no self record; invalid/missing keys return `false`, distance `-1`, and null record.
- Pair records are held in a private normalized-key dictionary, providing O(1) lookup without mutable dictionary exposure.
- Null collections/items, invalid placement state, duplicate keys, invalid occupied cells, and cross-placement overlap fail without partial index publication.
- The exact passing six-placement set publishes `6` keys and `15` records.

## REQUIRED-SITE POLICY EVIDENCE

- Exact policy keys: Start plus Boss, Forge, Cassia Sap Heart, Deep Star Yeast, and Moon Core Meteor; Village keys: `0`.
- Exact typed start distances: `4 / 2 / 2 / 2 / 2`.
- Exact typed other-site distances: `2 / 2 / 3 / 3 / 3`.
- Exact constraints: `15`; Start-to-site `5`; site-to-site `10`.
- Minimum-distance distribution: `2 x 5`, `3 x 9`, `4 x 1`.
- Site-to-site minimum is the maximum of the two typed other-site distances.
- Missing/null/duplicate/unexpected/inactive/wrong-role/wrong-count/invalid-distance policy inputs return sorted errors and no partial policy.
- Active required Village definitions are validly excluded for the future Village-owned phase.

## EVALUATION EVIDENCE

- Exact passing set Start `(0,0)`, Boss 2x1 `(4,0)/(5,0)`, Forge `(8,0)`, Cassia `(0,4)`, Yeast `(4,6)`, Meteor `(9,6)`: all `15/15` constraints satisfied.
- Exact-threshold `4`, `3`, and `2` comparisons all PASS; equality is not a violation.
- Start-Boss `3/4`, Core-Core `2/3`, and Forge-Boss `1/2` each produce exact deficit `1` violations.
- Violations preserve canonical pair orientation and exact closest-sector evidence and are sorted/deduplicated.
- Null policy and missing/unexpected key sets produce structural failure with zero partial violations.
- Partial states remain queryable through `Policy.TryGetConstraint` and `Index.TryGetDistance` without being treated as complete evaluations.

## DETERMINISM / IMMUTABILITY / OWNERSHIP

- Placement input order, list/array form, reversed input, fresh/reused builders, and 100 repeated builds preserve the exact key/record snapshot.
- Seeds `0`, `4660`, and `ulong.MaxValue`, candidate ordinal, and footprint transform do not alter distance from the occupied footprint cells.
- `en-US` and `tr-TR` observable order/results are identical.
- Input placements, definition collections, keys, records, constraints, violations, errors, and results are copied and exposed read-only.
- Public setters/fields, mutable collection exposure, mutable static cache, Unity object/lifecycle dependency, lazy public enumeration: `0`.
- Cost, weight, RNG, selection, backtracking, capacity, Village distance, pass, route distance, tile distance, and file-I/O production dependency audit: `0`.

## TEST

- Final new `SiteDistanceIndexTests`: `239/239 PASS`, failed `0`, skipped `0`; job `c6b7bca3334f43ad8529a5369b1cb122`.
- MAP03_03 `FootprintPlacementSolverTests`: `170/170 PASS`; job `eb33e108299b4467bf64c93b984ab376`.
- MAP03_02 `SiteCandidateEnumerationTests`: `268/268 PASS`; job `887943dddf1a43ffa0c77125d150d262`.
- MAP03_01 `SiteReservationModelsTests`: `81/81 PASS`; job `2c6eee8acc6b44c3bd0b346fc0c23f89`.
- MAP02 Runtime focused: `647/647 PASS`.
  - Generated/RNG/grid/root/execution/replay/topology Runtime aggregate `575`: job `18eb7938bf1a41ccbd294721c6fd7234`.
  - Exit fixture `72`: job `749657bdf0404bcb8f6d0f111484e695`.
- Approved MAP02 phase aggregate remains `667/667 PASS`; topology Editor `20` are covered by the final full-project run.
- `SpecialVillageDefinitionBuilderTests`: `57/57 PASS`; job `69ff890afc494ba598940fc4d1013d6a`.
- `BiomeBoundaryDefinitionBuilderTests`: `38/38 PASS`; job `f7ef2fdf6c7246908b8173785df2f484`.
- `StaticDataRegistryBuilderTests`: `53/53 PASS`; job `4f35904fbb58410797b00e17ecf411d5`.
- `ContentVersionHashCalculatorTests`: `54/54 PASS`; job `f36790545cd24258b1ee770a195c94dc`.
- Final targeted `Game.Map.Tests.EditMode`: `2272/2272 PASS`, failed `0`, skipped `0`; job `710b818aab8547b5a9a7e49e0877d449`.
- Final full project EditMode: `2312/2312 PASS`, failed `0`, skipped `0`; job `a2f444f508b948e09c2f687f02950fbc`.
- PlayMode: NOT RUN per Task scope.
- Visual: NOT APPLICABLE.

## UNITY

- Active instance: `Constant@ced6e0dfc4a31d45`.
- Unity: `6000.3.8f1`.
- Final forced asset/script refresh and requested compilation: PASS.
- Compile errors: `0`; relevant project-code warnings: `0`.
- A transient MCP package WebSocket warning appeared during tool-driven refresh; it is outside project code. Final isolated Console errors/warnings after clear: `0/0`.
- Final Editor state: not playing, not compiling, no domain reload pending.
- Saved Scene/Prefab changes: NONE.

## ASSET / META / AUTHORING

- Before Assets meta: `3012`; after Assets meta: `3020`.
- New matching `.cs.meta`: `8/8` with `fileFormatVersion: 2`, `MonoImporter`, and unique lowercase non-zero 32-hex GUIDs.
- Invalid/missing GUID rows: `0`; duplicate GUID groups: `0`.
- Authoring CSV/meta: `50/50` unchanged.
- Authoring aggregate path/hash baseline before and after: `378648cc026c688cf41218740e013ebb4f75123d7f1eb42f0b0961f6234fbc8b`.

## CHANGE SCOPE

- Assets files newer than the original patch marker: exact `16`.
- New Runtime production C#: `7`; matching meta: `7`.
- New focused test C#: `1`; matching meta: `1`.
- Existing Assets modifications: `0`; unexpected Assets changes: `0`; deleted Assets: `0`.
- New directory/folder meta: `0`.
- asmdef/asmref, Authoring/generated CSV/meta, Scene/Prefab, Package, and ProjectSettings changes: `0`.
- Git commit/push/branch/reset/rebase: not performed.

## DONE CONDITIONS

- [x] Patch prerequisites, exact copy operations, hash identity, Current Task binding, and `.APPLIED` binding PASS.
- [x] Exact footprint-aware Manhattan metric, exhaustive 28561 comparisons, closest-pair tie-break, and overlap gate PASS.
- [x] Immutable key/record/index/error/result and O(1) lookup contracts PASS.
- [x] Exact six-key/15-constraint typed policy and `2x5 / 3x9 / 4x1` distribution PASS.
- [x] Passing set, equality boundaries, deficit violations, structural evaluation gates, and closest-sector evidence PASS.
- [x] Determinism, culture, input order, caller ownership, and public mutation-surface gates PASS.
- [x] No MAP03_05 cost/index consumer or later-task work introduced.
- [x] Focused, regression, targeted, full EditMode, compile, Console, meta, GUID, Authoring hash, and exact change-scope gates PASS.

## NEXT

- Finalize only `MAP03_04_IMPLEMENT_SITE_DISTANCE_INDEX` as COMPLETE and set Current Task to NONE.
- Keep `MAP03_05_IMPLEMENT_SITE_CANDIDATE_COST` LOCKED.
- Await a separate future patch; do not auto-start the next Task.

## Recommended Commit

`feat(map): index required site footprint distances`
