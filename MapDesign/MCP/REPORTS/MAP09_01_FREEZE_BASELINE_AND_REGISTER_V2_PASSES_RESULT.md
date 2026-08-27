# MAP09_01 - Freeze Baseline and Register V2 Passes Result

```text
TASK: MAP09_01_FREEZE_BASELINE_AND_REGISTER_V2_PASSES
STATUS: PASS
MAP09_01: COMPLETE ELIGIBLE
MAP09_02_DEFINE_LAYER_PACING_AND_ACCESS_CONTRACTS: LOCKED / DO NOT START
```

## Preflight Audit

The task resumed from a partially implemented worktree. The installed Task and archive were byte-identical, the Status named MAP09_01 as the only CURRENT task, and no live single-MD inbox candidate remained.

```text
Unity: 6000.3.8f1
Editor: ready, play mode stopped, compiling false
Runtime assembly: Game.Map.Runtime
Editor assembly: MapAuthoring.Editor
Runtime EditMode tests: Game.Map.Tests.EditMode
Editor EditMode tests: MapAuthoring.Tests.EditMode
PlayMode tests: Game.Map.Tests.PlayMode

Runtime folder: Assets/_Game/Map/Runtime/WorldGeneration/Pipeline
Runtime namespace: StarNight.Map.WorldGeneration.Pipeline
Test folder: Assets/_Game/Tests/EditMode/Map/WorldGeneration/Pipeline
Test namespace: StarNight.Map.Tests.EditMode.WorldGeneration.Pipeline
Existing WorldGenerationRoot V2 execution link: NONE
Existing V2 pass/catalog production type before this task: NONE
```

Approved assembly definitions remained byte-unchanged:

```text
Game.Map.Runtime.asmdef:            1df0ed8fcdf1f7c668b12f29da71272f3133f64a1965fcf70237a6e5f0b34fef
MapAuthoring.Editor.asmdef:          11ef7812e0049b053c077d1cefa0b51bc4b60eea6609d046fe78d60d74197c17
Game.Map.Tests.EditMode.asmdef:      2d05060be8f0d602b97483b1e0bda2acadc9fb134aa0433d284626c5513d225a
MapAuthoring.Tests.EditMode.asmdef:  3cfa706a0462c146089ac42f7e2254f7bb42cdf175e85a58a7c1660c7dde76d2
Game.Map.Tests.PlayMode.asmdef:      4bfa3245b43ee4d419c48f7103c8b9e40b2ca47ca974fd45f60959069a75580c
```

Patch/protocol preconditions:

```text
MAP09_00R Result STATUS/hash: PASS / 0fbd1448b6bac27ff51774aac8d5198cc19f34d7ff97ad11be9b31ace5e43d8a
Installed MAP09_00R Task hash: 35185c5ea8a584cf89e97928e16fcf88c14684e5aaa7e6658a33e12aa741fd2f
Installed MAP09_01 Task hash: 52cb1e4c1ce89691478d270fc4a7761a8e1b7f6d97a241a2e64947a78c6d41d8
Archived MAP09_01 Task hash:  52cb1e4c1ce89691478d270fc4a7761a8e1b7f6d97a241a2e64947a78c6d41d8
Single-MD inbox candidates: 0
Unapplied legacy patch folders: 0
```

Pre-existing unrelated dirty files were identified before task completion and preserved:

```text
Constant.slnx
Packages/manifest.json
Packages/packages-lock.json
MapDesign/MCP_INBOX/MAP07_13_FINALIZE_MAP07_EXIT_APPROVED/
```

The MAP09_00R Result already records the same four paths as pre-existing and out of scope. None is staged or included by MAP09_01.

## Baseline Evidence

```text
MicroPattern: 4x4
MicroChunk: 12x8
Sector Canvas: 48x32
Generation philosophy: Cluster-first > Pattern-second > Chunk-slice-last

Boundary pair count: 6
Boundary candidates: 31
Boundary microchunks: 31
Boundary tile rows: 2976
Boundary socket rows: 62
Directional projections: 62/62
Mandatory tool_requirement NONE: 31/31
Boundary aggregate digest:
f7ff1c49f5bc33a4ad57799269bc3915806fe0cb60f347ed76eb16ea26f7fc68

Authoring CSV/meta: 50/50
Authoring manifest:
f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb
Generated CSV: 0
```

The focused baseline tests recomputed the boundary evidence and Authoring manifest from the current project; these are not copied-only assertions.

## Implemented File Inventory

New Runtime production C# and matching metas:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Pipeline/V2PassContract.cs
Assets/_Game/Map/Runtime/WorldGeneration/Pipeline/V2PassContract.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/Pipeline/V2PassCatalog.cs
Assets/_Game/Map/Runtime/WorldGeneration/Pipeline/V2PassCatalog.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/Pipeline/V2PassCatalogValidator.cs
Assets/_Game/Map/Runtime/WorldGeneration/Pipeline/V2PassCatalogValidator.cs.meta
```

New Runtime EditMode test C# and matching meta:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Pipeline/V2PassCatalogTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Pipeline/V2PassCatalogTests.cs.meta
```

Task/protocol documents:

```text
MapDesign/MCP/TASKS/MAP09_01_FREEZE_BASELINE_AND_REGISTER_V2_PASSES.md
MapDesign/MCP_ARCHIVE/MAP09_01_FREEZE_BASELINE_AND_REGISTER_V2_PASSES.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
MapDesign/MCP/REPORTS/MAP09_01_FREEZE_BASELINE_AND_REGISTER_V2_PASSES_RESULT.md
```

No existing MAP00~08 production or test file was modified. No folder meta, fixture/data asset, ScriptableObject, Scene, Prefab, asmdef, or asmref was created or changed.

## V2 Pass Catalog Evidence

The immutable catalog contains exactly ten entries in explicit numeric order:

| Order | Pass ID | Input | Output | Failure owner | Policy / retry | RNG stream |
|---:|---|---|---|---|---|---|
| 10 | Pacing | ApprovedMapBaseline | PacingPlan | PacingPlanner | ImmediateFailure / None | Pacing |
| 20 | SpecialRegionReservation | PacingPlan | SpecialRegionReservationPlan | SpecialRegionPlanner | ReselectWithinScope / Footprint | SpecialRegionReservation |
| 30 | TerrainClusterReservation | SpecialRegionReservationPlan | TerrainClusterPlacementPlan | TerrainClusterPlanner | ReselectWithinScope / Cluster | TerrainClusterReservation |
| 40 | RouteSpine | TerrainClusterPlacementPlan | RouteSpinePlan | RouteSpinePlanner | ReselectWithinScope / Footprint | RouteSpine |
| 50 | TraversalEnvelope | RouteSpinePlan | TraversalEnvelopePlan | TraversalEnvelopePlanner | ImmediateFailure / None | NONE |
| 60 | MicroPattern | TraversalEnvelopePlan | PatternApplicationPlan | MicroPatternPlanner | ReselectWithinScope / Pattern | MicroPattern |
| 70 | TerrainCleanup | PatternApplicationPlan | CleanTerrainCanvas | TerrainCleanupPlanner | ReselectWithinScope / Pattern | NONE |
| 80 | ActivityEventOverlay | CleanTerrainCanvas | ActivityEventPlacementPlan | ActivityEventPlanner | ImmediateFailure / None | ActivityEventOverlay |
| 90 | TileValidation | ActivityEventPlacementPlan | ValidatedSectorCanvas | TileValidator | OrderedEscalation / Pattern > Cluster > Footprint | NONE |
| 100 | MicroChunkSlice | ValidatedSectorCanvas | GeneratedMicroChunkSlices | MicroChunkSlicer | ImmediateFailure / None; preserve validated canvas | NONE |

```text
Pass count: 10
Duplicate Pass IDs/orders/output artifacts: 0/0/0
Missing inputs: 0
Inputs produced too late: 0
Dependency cycles: 0
Unused intermediate outputs: 0
Final pass: MicroChunkSlice
Final validation escalation: Pattern > Cluster > Footprint
Catalog/entry collection external mutation: rejected
Display text included in digest: NO
Reflection/file enumeration order used for registration: NO
Catalog digest:
90a2614f9a95c29f1546f350190010524672d4b4aa2d1ad1dfe7dbd431be50d5
```

Configuration, schema, and approved-baseline failures are registered as immediate failures with no retry and no silent fallback. The validator also reports duplicate Pass IDs as validation issues without throwing.

## Focused Tests

Authoritative final-code run:

```text
Selection: EditMode category MAP09_01
Transport: live Unity Pipeline synchronous run
Transport job ID: N/A (the synchronous API does not emit a job-id field)
Execution timestamp: 2026-08-27T04:53:36.3690728Z
Discovered/executed: 26
Passed: 26
Failed: 0
Skipped: 0
Inconclusive: 0
Duration: 7.40 seconds
```

An earlier partial-code focused run was `25/25 PASS`. A diagnostic prefix category `MAP08` selected zero tests because this runner matches categories exactly; it was excluded from all evidence. All authoritative regressions below use exact category names or exact uncovered fixture names.

## Required Regression

The complete required selection was rerun after the final validator change and compile:

```text
MAP08_01..MAP08_14 exact category union: 9220/9220 PASS
MAP07_01..MAP07_13 exact category union: 5422/5422 PASS
MAP06_02..MAP06_10 categorized subset:   2552/2552 PASS
OptionalRegionModelsTests uncovered:       194/194 PASS
MAP06 required total:                     2746/2746 PASS
MAP05_01, MAP05_03..MAP05_11 subset:      1832/1832 PASS
MandatoryRouteMaskLookupBuilderTests:      127/127 PASS
MAP05 required total:                     1959/1959 PASS

Required distinct total:                19347/19347 PASS
Failed/skipped/inconclusive: 0/0/0
Transport job IDs: N/A (live synchronous API)
```

The repeated final-code run preserved every approved per-category count, not only the aggregate.

## Unity Verification

```text
Unity: 6000.3.8f1
Forced recompile: completed
Compile errors: 0
Console errors: 0
Relevant warnings: 0
Editor status: ready
ready_for_tools equivalent: true
is_compiling: false
domain reload in progress: false
Play mode: stopped
```

## Static Gates

```text
New Runtime production C#/matching meta: 3/3
New Runtime EditMode test C#/matching meta: 1/1
New folder meta: 0
Global Assets meta: 3840 -> 3844 (+4 task-owned)
Assets/_Game/Map meta: 611 -> 614 (+3 task-owned; test meta is outside Map)
Duplicate GUID groups: 0

Authoring CSV/matching meta: 50/50
Authoring tracked CSV changes: 0
Authoring manifest: f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb
Generated CSV created: 0

Scene/Prefab tracked changes: 0/0
ProjectSettings task-owned changes: 0
Packages task-owned changes: 0
asmdef/asmref tracked changes: 0/0
Existing MAP00~08 production/test modifications: 0/0
Forbidden production symbol hits: 0
Unapplied MCP patches: 0
git diff --check errors before Result: 0
```

The repository still has two pre-existing dirty Package files, recorded before this Task. They are excluded from task ownership and commit scope; no task-owned Package delta exists.

## Out-of-Scope Findings

```text
Constant.slnx: pre-existing generated solution change; preserved and excluded
Packages/manifest.json: pre-existing Unity Pipeline tooling change; preserved and excluded
Packages/packages-lock.json: pre-existing Unity Pipeline tooling change; preserved and excluded
MapDesign/MCP_INBOX/MAP07_13_FINALIZE_MAP07_EXIT_APPROVED/: pre-existing applied legacy folder; preserved and excluded
```

No other out-of-scope issue was found.

## Commit and Phase Decision

```text
Atomic commit subject: MAP09_01: freeze baseline and register V2 passes
Atomic commit hash: SELF (the commit containing this Result; reported after creation)
Unrelated worktree files included: 0
Push: NOT PERFORMED
MAP09_01: COMPLETE
MAP09_02_DEFINE_LAYER_PACING_AND_ACCESS_CONTRACTS: LOCKED / DO NOT START
```

MAP09_01 implements catalog contracts and validation only. It does not implement or execute a Sector solver, footprint placer, Pattern renderer, graph compiler, runtime Tilemap/Collider/Streaming/Save path, or any MAP09_02+ model. The next Task is not started automatically.
