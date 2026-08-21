TASK: MAP03_06_IMPLEMENT_RESERVATION_BACKTRACKING
STATUS: PASS

## SUMMARY

- Implemented deterministic six-group site reservation selection with exact depth-first backtracking.
- Implemented immutable search options/groups/limits, canonical collision detection, stable RNG tie-breaks, exact retry/status semantics, diagnostics, selection plans, and search results.
- Integrated the existing MAP03_03 placement solver, MAP03_04 distance policy/index, and MAP03_05 cost calculator without modifying them.
- Reservation publication, capacity flood validation, Village selection, full pass retry, snapshots, serialization, file I/O, and MAP03_07 work were not introduced.

## PATCH APPLY

- Applied inbox patch `MAP03_06_IMPLEMENT_RESERVATION_BACKTRACKING`, version `1.0`.
- Applied exact manifest operations: Master replace, Status replace, Current Task create.
- Payload/destination SHA-256 matched for all `3/3` files after application.
  - Master: `a29efe1279cb248694d28e459bacd506dcd55772d9115f16f62309d8007dd884`.
  - Status: `7b77f797d2c672cc40450bd5d06919b67481f368c0d095db446009188a01b708`.
  - Task: `b3093dc7c729db92e2eb46e3b6ec3629d53f20c4302d3472b06deb0dfa5c49b0`.
- Manifest SHA-256: `d68d7890275ef0c34abc61f9a562bcaadd01a77fec7c4dbdaeedaa23093baedb`.
- `.APPLIED` exact manifest/task binding was reverified before finalization.
- Post-apply state was `40 COMPLETE / 1 CURRENT / 164 LOCKED` with only MAP03_06 CURRENT; MAP03_07 remained LOCKED.

## READ

- Read the MCP entrypoint, pipeline, global locked/work/CSV/Unity/change/patch/finalize rules, Master backlog, Status, this Task, and MAP03_05 Result in the mandated order.
- Read only Current Task allowlisted typed definitions, RNG, candidate/placement/distance/cost models, focused tests and assemblies, matching metas, and permitted path/count/hash audit boundaries.
- Used the Unity MCP orchestration workflow for editor state, compilation, Console, and EditMode verification.
- Authoring CSV bodies, MAP03_07 or later Task bodies, unrelated production/test bodies, Legacy generator bodies, and Scene/Prefab YAML were not read.

## PRIOR GATE

- MAP03_05 Result exact `STATUS: PASS`: confirmed.
- Prior focused MAP03_05 `270/270`, MAP03_04 `239/239`, MAP03_03 `170/170`, MAP03_02 `268/268`, and MAP03_01 `81/81`: confirmed by regression runs.
- Approved MAP02 aggregate `667/667`: confirmed as Runtime `647/647` plus overlay editor companion `20/20`.
- Definition/registry regressions: Special Village `57/57`, Biome Boundary `38/38`, Content Hash `54/54`, and Static Registry `53/53` PASS.
- Prior Assets meta `3027`, Authoring CSV/meta `50/50`, duplicate GUID groups `0`, and existing Assets modifications `0`: confirmed.

## CREATED

Runtime production C# and matching meta:

- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSearchOption.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSearchOption.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSearchGroup.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSearchGroup.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSearchLimits.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSearchLimits.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SitePlacementConflictDetector.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SitePlacementConflictDetector.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSearchDiagnostics.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSearchDiagnostics.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSelectionPlan.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSelectionPlan.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSearchResult.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSearchResult.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationBacktracker.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationBacktracker.cs.meta`

Focused test and matching meta:

- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationBacktrackerTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationBacktrackerTests.cs.meta`

Documentation:

- This Result file.

## MODIFIED

- Existing Assets production/test/meta/asmdef/asmref files: `0`.
- Authoring/generated CSV/meta: `0`.
- Scene/Prefab/Package/ProjectSettings: `0`.
- Existing MAP03_01 through MAP03_05 implementation and tests: `0`.
- Status and Master were not modified during Task execution; Status is reserved for Phase C finalization after this PASS Result.

## PREEXISTING_IDENTICAL

- None. All exact eighteen permitted Asset destinations were absent before implementation.

## GROUP / OPTION / LIMIT EVIDENCE

- Search order is frozen to Start, Boss, Forge, Cassia Sap Heart, Deep Star Yeast, and Moon Core Meteor at depths `0..5`, independent of caller group order.
- Groups validate the exact key/typed-definition identities, reject Village or unexpected keys, canonicalize options by origin/transform/candidate ordinal, and publish copied read-only collections.
- Options preserve immutable successful placements and the exact future Core capacity domain `-1 | 0..169`.
- Limits accept only `1..200`; `Default.MaxFailedCombinations == 200`.
- Exact starter source counts are `88 / 572 / 624 / 624 / 624 / 624`, total `3156`.

## PREFLIGHT / RNG / ORDER EVIDENCE

- Null, missing, duplicate, unexpected, empty, structurally invalid, policy-mismatched, missing-weight/limit/RNG, and pre-consumed RNG inputs return sorted/deduplicated stable errors as `InvalidInput` without consuming RNG.
- Every canonical option receives exactly one `NextUInt64()` draw after successful preflight; draw count depends only on source option count, not branching or backtracking.
- Known initial site-stream state `60D4B46EBF6EF00D` produces tie-breaks `F627BD56683B33FC` then `4CA318D8E4EA97BA`, so the second equal-cost canonical option precedes the first.
- Viable options use exact ascending order `(TotalCost, RandomTieBreak, OriginIndex, TransformOrdinal, CandidateOrdinal)`; RNG never promotes a higher total cost above a lower one.
- The injected site stream is the only consumed RNG stream; other stream state/draw counts remain unchanged.

## CONFLICT / COST EVIDENCE

- Conflict reasons use the frozen order `FootprintOverlap`, `BlocksExistingEntryApproach`, `EntryApproachOccupied`, `DistanceConstraint`, `CoreCluster`.
- Pairwise occupied/entry-exterior collision checks return every independent reason in canonical order while allowing shared entry exterior and ordinary adjacency.
- Options with collision reasons are rejected before cost evaluation.
- Existing MAP03_05 costs are evaluated against only the current earlier selections. Distance and Core-cluster units are hard rejection reasons; altitude, edge, and capacity remain soft ranking costs.
- One evaluation with multiple hard reasons increments rejected-option count once and each applicable reason count once.

## BACKTRACK / STATUS EVIDENCE

- Exact deterministic DFS selects the first ranked viable option, descends, and uses LIFO selection pops only when a deeper state is exhausted.
- Synthetic one-level backtracking selects a previous-depth alternative and completes; forced multi-depth dead ends pop in exact LIFO order.
- Each selection pop increments both failed-combination and backtrack counts, preserving `FailedCombinationCount == BacktrackCount` and the configured maximum.
- Custom limit `1` stops immediately after the first pop as `FailedCombinationLimitReached`; the default stops at exact combination `200` without attempting `201`.
- Root exhaustion before any pop returns `NoSolution`. `NoSolution` and limit results require caller retry; `InvalidInput` does not.
- `Completed` is the only status with a non-null plan; every failure status publishes no partial plan.

## PLAN / DIAGNOSTICS / INTEGRATION EVIDENCE

- Successful plans contain exact six immutable steps at depths `0..5`, the required key order, collision-free placements, checked incremental-cost sum, and read-only copied snapshots.
- Every incremental breakdown is hard-satisfied and was computed only from earlier selections.
- Final MAP03_04 postcondition is exact `6 keys / 15 records / 15 constraints`, with all policy constraints satisfied and zero violations/errors.
- Full starter seeds `0`, `4660`, and `ulong.MaxValue` each complete with source options `3156`, tie-break draws `3156`, selected placements `6`, deepest depth `6`, and Village selections `0`.
- Diagnostics expose exact group search order, visits/evaluations/pushes/pops/exhaustions/rejections, per-reason counts, initial/draw states, and aggregate invariants without affecting selection.
- Reversed/shuffled arrays or lists, caller mutation, fresh/reused backtrackers across `100` runs, and `en-US`/`tr-TR` cultures preserve observable results for the same stream input.
- Public setters/fields, mutable public collections, lazy public enumeration, static mutable caches, Unity lifecycle dependencies, capacity flood, Village, final snapshot, pass/root, and file-I/O production dependencies: `0`.

## TEST

- Final new `SiteReservationBacktrackerTests`: `248/248 PASS`, failed `0`, skipped `0`; job `227a2b31f075457bba6a0f0b3c024302`.
- MAP03_05 `SiteCandidateCostTests`: `270/270 PASS`; job `2647393682d0491593c944fe96cf56ce`.
- MAP03_04 `SiteDistanceIndexTests`: `239/239 PASS`; job `328c98abd41b4a3b9d4802f91d628b3a`.
- MAP03_03 `FootprintPlacementSolverTests`: `170/170 PASS`; job `084affc7a3244d45a92c0480e091986c`.
- MAP03_02 `SiteCandidateEnumerationTests`: `268/268 PASS`; job `0529e19228f9463eb90ecf1a33cb1564`.
- MAP03_01 `SiteReservationModelsTests`: `81/81 PASS`; job `6a812ffa97eb4f1a83fe5a3a2baa22cf`.
- Approved MAP02 Runtime aggregate: `647/647 PASS`; jobs `3127d583978b4dbf99cf4067e46f1ffe` (`175/175`) and `bdc645e855554215adb1850982aa9bcf` (`472/472`).
- MAP02 overlay editor companion: `20/20 PASS`; job `0c2081ab844c4b4d8ff2fc945c457390`. Approved MAP02 total: `667/667 PASS`.
- Definition combined Special Village, Biome Boundary, and Content Hash: `149/149 PASS`; job `f4b838f04e1d46b890f3c49aa3bbe558`.
- `StaticDataRegistryBuilderTests`: `53/53 PASS`; job `b8866a74b4f048e8961af40aa9cee8b6`.
- Final targeted `Game.Map.Tests.EditMode`: `2790/2790 PASS`, failed `0`, skipped `0`; job `26c5e065073f4270a0248c303dcb9866`.
- Final full project EditMode: `2830/2830 PASS`, failed `0`, skipped `0`; job `5c4886d5ece34c728b46350742f8cf84`.
- PlayMode: NOT RUN per Task scope.
- Visual: NOT APPLICABLE.

## UNITY

- Unity `6000.3.8f1`, MCP instance `Constant@ced6e0dfc4a31d45`.
- Final forced refresh and compilation completed with editor ready and no pending compilation/domain reload.
- Final Console after clear/compile: errors `0`, warnings `0`; relevant new warnings `0`.

## ASSET / META / AUTHORING

- Assets meta before: `3027`; after: `3036`.
- New matching `.cs.meta`: `9/9`, each with `fileFormatVersion: 2`, `MonoImporter`, and a unique lowercase non-zero 32-hex GUID.
- Invalid new meta rows: `0`; duplicate new GUID groups: `0`; duplicate GUID groups across Assets: `0`.
- Authoring CSV/meta: `50/50` unchanged.
- Authoring CSV aggregate path/hash before and after: `378648cc026c688cf41218740e013ebb4f75123d7f1eb42f0b0961f6234fbc8b`.

## CHANGE SCOPE

- Assets files newer than the applied-patch marker: exact `18`, all exact WRITE ALLOWLIST destinations.
- New Runtime production C#: `8`; matching meta: `8`.
- New focused test C#: `1`; matching meta: `1`.
- Created Assets: `18`; modified existing Assets: `0`; deleted Assets: `0`; unexpected Assets changes: `0`.
- Created report: `1`.

## TASK CHECKLIST

- [x] Exact immutable option, group, limits, conflict, diagnostics, selection-plan, result, and backtracker contracts implemented.
- [x] Exact six-group canonical search order, starter counts, typed-definition gates, and structural preflight implemented.
- [x] Stable one-draw-per-option RNG tie-break and exact total/tie/origin/transform/candidate ranking implemented.
- [x] Exact collision, distance, Core-cluster hard rejection and altitude/edge/capacity soft ranking implemented.
- [x] Exact DFS push/pop, failed-combination limit, retry/status, and no-partial-plan semantics implemented.
- [x] Exact diagnostics, six-step plan, checked total, and final `6/15/15` distance postcondition implemented.
- [x] Full starter three-seed, determinism, ownership, culture, RNG isolation, and public mutation-surface gates PASS.
- [x] No reservation publication, capacity flood, Village, pass/root retry, snapshot, serialization, file-I/O, or MAP03_07 work introduced.
- [x] Focused, regression, targeted, full EditMode, compile, Console, meta, GUID, Authoring hash, and exact change-scope gates PASS.

## NEXT

- Finalize MAP03_06 only. Keep `MAP03_07_IMPLEMENT_CAPACITY_FLOOD_VALIDATION` and every later Task `LOCKED`.
- Do not automatically start the next Task.

Recommended Commit: `feat(map): backtrack deterministic site reservations`
