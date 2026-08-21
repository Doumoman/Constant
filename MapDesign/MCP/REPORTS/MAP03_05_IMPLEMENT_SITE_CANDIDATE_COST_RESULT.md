TASK: MAP03_05_IMPLEMENT_SITE_CANDIDATE_COST
STATUS: PASS

## SUMMARY

- Implemented deterministic checked-integer cost evaluation for one successful `FootprintPlacement` option.
- Implemented immutable weights, context snapshots, exact breakdown, sorted/deduplicated errors, and success/failure result ownership.
- Implemented the exact five-component formula: altitude, edge clearance, partial distance constraints, optional future Core capacity, and three-Core 4x4 clustering.
- Candidate enumeration/ranking/selection, RNG, reservation publication, backtracking, flood capacity, Village placement, route/tile distance, pass adapters, serialization, and file I/O were not introduced.

## PATCH APPLY

- Applied inbox patch `MAP03_05_IMPLEMENT_SITE_CANDIDATE_COST`, version `1.0`.
- Applied exact manifest operations: Master replace, Status replace, Current Task create.
- Payload/destination SHA-256 matched for all `3/3` files after application.
  - Master: `ab42214995d0f2bc06bc7303f2f0bd3600035c224f69d264ce2956e3b77225b8`.
  - Status: `9b0daf1bff69abfe7f50df7f32bf23b8a338df38c236bc9ab272d2e895b7fa16`.
  - Task: `16f144c2c73b5c6195c6be9fdb65bc82929492131d97514e1bd0c8c100344ecf`.
- Manifest SHA-256: `1a4bfbec0fc281c1d970ba38af782475f1740a37a570b83091bf0d9ee2824f18`.
- `.APPLIED` exact manifest/task binding was reverified before finalization.
- Post-apply state was `39 COMPLETE / 1 CURRENT / 165 LOCKED` with only MAP03_05 CURRENT; MAP03_06 remained LOCKED.

## READ

- Read the MCP entrypoint, global locked/work/CSV/Unity/change/patch/finalize rules, Master backlog, Status, this Task, and MAP03_04 Result in the mandated order.
- Read only Current Task allowlisted typed definitions, grid/site/distance models, focused tests and assemblies, matching metas, and permitted path/count/hash audit boundaries.
- The optional exact Map Package reference tree was not installed; this Task's frozen definitions and typed models were authoritative.
- Authoring CSV bodies, MAP03_06 or later Task bodies, unrelated production/test bodies, Legacy generator bodies, and Scene/Prefab YAML were not read.

## PRIOR GATE

- MAP03_04 Result exact `STATUS: PASS`: confirmed.
- Prior focused `239/239` plus exhaustive `28561`, policy `6 keys / 15 records / 15 constraints`: confirmed.
- Prior regressions `170/170`, `268/268`, `81/81`, MAP02 `667/667`, targeted `2272/2272`, and full `2312/2312`: confirmed.
- Prior Assets meta `3020`, Authoring CSV/meta `50/50`, duplicate GUID groups `0`, and existing Assets modifications `0`: confirmed.

## CREATED

Runtime production C# and matching meta:

- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCostWeights.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCostWeights.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCostContext.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCostContext.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCostBreakdown.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCostBreakdown.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCostError.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCostError.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCostResult.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCostResult.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCostCalculator.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCostCalculator.cs.meta`

Focused test and matching meta:

- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteCandidateCostTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteCandidateCostTests.cs.meta`

Documentation:

- This Result file.

## MODIFIED

- Existing Assets production/test/meta/asmdef/asmref files: `0`.
- Authoring/generated CSV/meta: `0`.
- Scene/Prefab/Package/ProjectSettings: `0`.
- Status and Master were not modified during Task execution; they are reserved for Phase C finalization after this PASS Result.

## PREEXISTING_IDENTICAL

- None. All exact fourteen permitted Asset destinations were absent before implementation.

## WEIGHTS / FORMULA EVIDENCE

- Exact defaults: altitude `10`, edge `25`, distance `1000`, future capacity `100`, Core cluster `10000`.
- Every custom weight is immutable and non-negative; negative constructor input is rejected.
- Component penalties and the total are calculated using checked `long` arithmetic.
- Exact total identity: `AltitudePenalty + EdgePenalty + DistanceConstraintPenalty + FutureCoreCapacityPenalty + QuadrantClusteringPenalty`.
- Success publishes one complete immutable breakdown; every expected invalid input publishes sorted errors and no partial breakdown.

## STARTER DEFINITION GATE

- Meteor -> `BIO_MOON_CRATER`, preferred Y `0..7`, Core minimum `5`, edge touch `true`, buffer `1`.
- Cassia -> `BIO_CASSIA_ROOT`, preferred Y `2..12`, Core minimum `5`, edge touch `false`, buffer `1`.
- Yeast -> `BIO_MOON_DOUGH`, preferred Y `0..7`, Core minimum `5`, edge touch `true`, buffer `1`.
- Forge and Boss -> `BIO_ABANDONED_MILL`, preferred Y `1..11`, Core minimum `4`, edge touch `false`, buffer `1`.
- Core capacity forecast applies only to the three CoreResource sites and Forge. Boss and Start require unavailable count `-1`.

## COMPONENT VECTOR EVIDENCE

- Altitude exact vectors PASS: Crater `0/7 -> 0`, `8 -> 1`, `12 -> 5`; Cassia `0 -> 2`, `2/12 -> 0`; Mill `0/12 -> 1`, `1/11 -> 0`; Dough `0/7 -> 0`, `8 -> 1`.
- Multi-cell altitude uses the maximum occupied-cell distance outside the preferred band.
- Edge exact vectors PASS: Root/Mill ring `0 -> 1`, ring `1 -> 0`; Crater/Dough valid rings -> `0`.
- All `169` world sectors independently validate exact occupied-cell edge-ring deficit.
- Distance exact vectors PASS: actual `3`, required `4` -> units `1`, violation `1`; equality -> no deficit; multiple applicable pairs sum exact deficits using footprint cells.
- Capacity exact vectors PASS: Forge `4/3 -> 1`; Core `5/4 -> 1`; equality -> `0`; unavailable `-1` -> units/penalty `0` and `HasEstimate == false`.
- Three exact Core sources in union bbox `4x4` -> cluster units `1`; bbox `5x4` -> `0`; fewer than three -> unavailable window `-1/-1`.
- Exact default aggregate units `2/1/1/1/1` publish penalties `20/25/1000/100/10000`, `TotalCost = 11145`, and `HardConstraintsSatisfied == false`.
- Altitude/edge/capacity costs remain soft and do not flip the hard signal. Distance or cluster units independently flip it without selection or exception.

## INPUT / ERROR / OWNERSHIP EVIDENCE

- Context copies placements into canonical key order, exposes a read-only snapshot, preserves the policy, rejects null/duplicate/unexpected/overlapping placements, and accepts capacity only at `-1` or `0..169`.
- Start accepts only null typed definitions and unavailable capacity; special candidates require exact active special-map, source/kind/ordinal, primary-biome, and active exact Core-rule identities.
- Village candidates are explicitly rejected as outside this Task.
- Candidate policy membership, existing membership, duplicate candidate key, cross-footprint overlap, and every distinct candidate-existing constraint are gated before calculation.
- Error enum ordinals match the frozen `26`-member order; error fields use canonical-or-empty IDs, sector `-1|0..168`, stable messages, and ordinal sort/dedupe.
- Reversed/shuffled array/list inputs, caller-list mutation, candidate ordinal, and transform evidence preserve deterministic cost components.
- Seeds `0`, `4660`, and `ulong.MaxValue`, fresh/reused calculators across `100` runs, and `en-US`/`tr-TR` cultures preserve observable results.
- Public setters/fields, mutable public collections, static mutable caches, Unity object/lifecycle dependencies, and lazy public enumeration: `0`.

## TEST

- Final new `SiteCandidateCostTests`: `270/270 PASS`, failed `0`, skipped `0`; job `abe853bfe18049f8ad8f81490c5a5240`.
- MAP03_04 `SiteDistanceIndexTests`: `239/239 PASS`; job `0eee851cb93c4c1ea3c97de4d100bed5`.
- MAP03_03 `FootprintPlacementSolverTests`: `170/170 PASS`; job `3b893f45fce94465820de5c842ed1e83`.
- MAP03_02 `SiteCandidateEnumerationTests`: `268/268 PASS`; job `586ca109b7ce47bba3bdd0bc97d83e40`.
- MAP03_01 `SiteReservationModelsTests`: `81/81 PASS`; job `fd9e3635dbf54042a2f885ebd66b1e0d`.
- Approved MAP02 phase aggregate `667/667 PASS`; covered by the final full-project job `daa2a1453fec4b62818f9c4f57a2824d`.
- `SpecialVillageDefinitionBuilderTests`: `57/57 PASS`; job `ecdc33ead8e146a98075233c9842bbf9`.
- `BiomeBoundaryDefinitionBuilderTests`: `38/38 PASS`; job `560eea173f034d829c700548d563fe66`.
- `StaticDataRegistryBuilderTests`: `53/53 PASS`; job `c5bee2d91c1b4126b06df8d7051d9166`.
- `ContentVersionHashCalculatorTests`: `54/54 PASS`; job `182614a945a44fb09989964b8bd779b6`.
- Final targeted `Game.Map.Tests.EditMode`: `2542/2542 PASS`, failed `0`, skipped `0`; job `065c7f7992d646c4b61d66954a6c6213`.
- Final full project EditMode: `2582/2582 PASS`, failed `0`, skipped `0`; job `daa2a1453fec4b62818f9c4f57a2824d`.
- PlayMode: NOT RUN per Task scope.
- Visual: NOT APPLICABLE.

## UNITY

- Unity `6000.3.8f1`, MCP instance `Constant@ced6e0dfc4a31d45`.
- Final forced refresh and compilation completed with editor ready, no pending compilation/domain reload.
- Final Console after clear/compile: errors `0`, warnings `0`; relevant new warnings `0`.

## ASSET / META / AUTHORING

- Assets meta before: `3020`; after: `3027`.
- New matching `.cs.meta`: `7/7`, each with `fileFormatVersion: 2`, `MonoImporter`, unique lowercase non-zero 32-hex GUID.
- Invalid/missing GUID rows: `0`; duplicate GUID groups: `0`.
- Authoring CSV/meta: `50/50` unchanged.
- Authoring CSV aggregate path/hash before and after: `378648cc026c688cf41218740e013ebb4f75123d7f1eb42f0b0961f6234fbc8b`.

## CHANGE SCOPE

- Assets files newer than the applied-patch marker: exact `14`, all exact WRITE ALLOWLIST destinations.
- New Runtime production C#: `6`; matching meta: `6`.
- New focused test C#: `1`; matching meta: `1`.
- Created Assets: `14`; modified existing Assets: `0`; deleted Assets: `0`; unexpected Assets changes: `0`.
- Created report: `1`.

## TASK CHECKLIST

- [x] Exact immutable weights, context, breakdown, error, result, and one-option calculator implemented.
- [x] Exact altitude, edge, partial distance, optional capacity, clustering, total, and hard-signal formulas implemented.
- [x] Exact starter typed-definition gates and reference vectors PASS.
- [x] Invalid input returns sorted failure with no partial cost.
- [x] Determinism, culture, input order, caller ownership, and public mutation-surface gates PASS.
- [x] No MAP03_06 selection/backtracking or later-task work introduced.
- [x] Focused, regression, targeted, full EditMode, compile, Console, meta, GUID, Authoring hash, and exact change-scope gates PASS.

## NEXT

- Finalize MAP03_05 only. Keep `MAP03_06_IMPLEMENT_RESERVATION_BACKTRACKING` and every later Task `LOCKED`.
- Do not automatically start the next Task.

Recommended Commit: `feat(map): score deterministic site placement candidates`
