```yaml
mcp_repair:
  format: current_task_repair_v1
  repair_id: MAP11_07R4_ALLOW_PATTERN_FREE_WORKING_CANVAS
  repairs_current_task: MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS
  requires_current_task: MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS
  requires_blocked_result:
    path: REPORTS/MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS_RESULT.md
    status: BLOCKED
    sha256: adb155824b1baebe650f117888df4938a8c6ac27dad56e43ca71b83ca184d588
  requires_installed_task:
    path: TASKS/MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS.md
    sha256: 87c8a482ce73da2d4238926aa0976916b809eae28b517cec3a17fb573a9f8dfd
  preserves_current_task: MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS
  next_task_remains_locked: MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES
```

# MAP11_07R4 — Allow Pattern-Free Working Canvas

```text
REPAIR: MAP11_07R4_ALLOW_PATTERN_FREE_WORKING_CANVAS
CURRENT TASK: MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS
STATUS EFFECT: NONE — MAP11_07 stays CURRENT
NEXT: MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES stays LOCKED
```

## 0. Repair Decision

MAP11_07 successfully authored/imported the 13 TerrainCluster CSVs and 16 starter clusters, then reached the original Section 9 pattern-free bridge.

That bridge intentionally supplies:

```text
authored nonprotected zones: 0
caller-selected placements: 0
Static Shell / AbsoluteProtected: exact predecessor evidence
```

The current public `TerrainClusterPatternRenderer` rejects every zero-placement request with:

```text
MissingInput|placements|At least one caller-selected placement is required.
```

This makes it impossible to publish an unpatterned Static Shell as a legitimate MAP11_05 full working canvas. A filler or implicit `NoChange` placement would falsify authored intent and is forbidden.

This repair adds one exact no-pattern success branch. It does not relax normal placement validation, select a pattern, consume RNG, or change MAP10 authority.

## 1. Apply / Audit Procedure

This is not a new Master Task. Do not run the normal `NONE -> CURRENT` task-open flow.

Preflight must verify:

1. Current Task is exact `MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS` and remains `CURRENT`.
2. `MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES` remains `LOCKED`.
3. The current BLOCKED Result status/SHA matches this file's metadata.
4. Original MAP11_07 Task SHA matches this file's metadata.
5. Installed/archive repair lineage is byte-identical:

```text
MAP11_07R_EXTEND_TERRAIN_CLUSTER_AUTHORING_SCHEMA.md
2eb3dde8186598000b366f8aa6ae807aed6aa77f9f0c7d89b32c42b8d987c9c8

MAP11_07R2_REBASE_MAP09_08_AUTHORING_INVENTORY_GATE.md
a151d29d14b90e1024bc97286f7f366d4b856f0a212ca62ef68891dc140253e7

MAP11_07R3_MAKE_MAP09_08_INVENTORY_GATE_MONOTONIC.md
2eeb89c1b2a8aa853712efaf029ecb69a0e60a903ce567434308543b8566efc7
```

6. R3 exact MAP09_08 method result is `1/1/1` PASS and must not be rerun.
7. Current MAP11_07 content state is exact:

```text
Authoring CSV/meta: 65/65
TerrainCluster CSV/meta: 13/13
starter clusters: 16/16
catalog entries: 16/16
variants: 32, exact 2 per cluster, exact one baseline
structural signatures: 16, duplicates 0
authored Quiet candidates: 4, one per biome
Generated CSV: 0
```

8. Latest MAP11_07 focused result is exact `10 executed / 8 passed / 2 failed`; both failures share only the empty-placement error above.
9. No other unapplied inbox candidate or unrelated staged path exists.

Install this repair byte-identically as:

```text
MCP/TASKS/MAP11_07R4_ALLOW_PATTERN_FREE_WORKING_CANVAS.md
MCP_ARCHIVE/MAP11_07R4_ALLOW_PATTERN_FREE_WORKING_CANVAS.md
```

Move/remove the inbox source after both copies match its SHA. Do not change Master or Status during repair installation. The original MAP11_07 Task plus R1 through R4 form the effective specification.

Any state/SHA/collision mismatch is `BLOCKED` with zero additional source/content modification.

## 2. Exact Owner Repair Boundary

Modify only these existing MAP11_05 files:

```text
Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/TerrainClusterPatternRenderer.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/TerrainClusters/TerrainClusterPatternRendererTests.cs
```

Do not modify their `.meta` files.

The Runtime edit owns only the exact pattern-free request branch in Sections 3–5. The test edit owns focused proof of that branch and preservation of the existing non-empty placement behavior.

No MAP10 source/test, MAP11_01~04 source/test, `TerrainClusterPatternZone.cs`, schema, importer, CSV, meta, Scene, Prefab, asmdef/asmref, Settings, or Packages file may be changed for this owner repair.

## 3. Exact Pattern-Free Request Predicate

A request is pattern-free only when both are true after ordinary null/identity/artifact validation and canonicalization:

```text
authored nonprotected zone count = 0
caller-selected placement intent count = 0
```

`authored nonprotected zone` means authored `GeometryAdd`, `GeometryCarve`, `Affordance`, or `Marker`. Derived `AbsoluteProtected` evidence from MAP11_03/04 does not make the request patterned and remains required/validated normally.

The pattern-free branch still requires all ordinary predecessor inputs and exact identities/digests:

- MAP11_01 active Local Canvas
- MAP11_03 traversal/protection compilation
- MAP11_04 Static Shell and route-witness evidence
- MicroPattern authoring catalog and expected catalog identity when required by the existing request surface
- canonical request ownership/cluster identity

Null/missing predecessor data, identity mismatch, digest mismatch, invalid protection evidence, coverage mismatch, or any existing non-placement error must still fail atomically.

The following are not the pattern-free success case:

```text
nonprotected zones > 0, placements = 0  -> keep exact MissingInput placements failure
placements > 0, nonprotected zones = 0  -> normal planner/permission validation; no bypass
nonprotected zones > 0, placements > 0  -> existing MAP11_05 behavior unchanged
```

Do not accept null placement/zone collections as aliases for empty. Use the existing request construction/null rules; only explicit valid empty collections qualify.

## 4. Pattern-Free Publication

For a valid pattern-free request:

1. Build/validate canonical AbsoluteProtected evidence exactly as before.
2. Build the full pre-render working canvas from the exact MAP11_04 Static Shell active-cell union.
3. Preserve each cell's Static Shell geometry and canonical provenance.
4. Seed no `GeometryCarveSubstrate` cells.
5. Create no placement, transform, protected-mask plan, or application plan.
6. Do not call the MAP10 ordered renderer with a fabricated empty target.
7. Publish an immutable final working canvas byte/semantic-identical to the initial working canvas.
8. Publish a successful atomic report/result/digest with explicit zero pattern evidence.

Required report evidence:

```text
canonical placements: 0
application plans: 0
MAP10 plan-union target coordinates: 0
GeometryCarve substrate coordinates: 0
renderer invocations: 0
renderer delta coordinates: 0
changed coordinates: 0
AbsoluteProtected renderer writes: 0
AbsoluteProtected final value changes: 0
full initial/final canvas coverage: exact active-cell count
initial/final canvas equality: exact
```

If the current report model lacks an unambiguous renderer-invocation/no-pattern field, add the minimum immutable count/Boolean property inside `TerrainClusterPatternRenderer.cs`. Include it in the canonical digest and tests. Do not add a new Runtime file.

Do not construct a fake MAP10 plan/render digest. Represent the absence of plans/rendering using the existing canonical empty-collection convention or a new explicit internal no-render marker owned only by MAP11_05. It must be deterministic, culture-independent, and distinguishable from an executed non-empty render.

## 5. Existing Patterned Behavior Remains Exact

For any request with one or more placements, preserve the approved MAP11_05/R contract:

```text
definition resolve
-> transform
-> protected-mask build
-> application plan
-> zone permission validation
-> exact plan-union MAP10 target
-> one atomic MAP10 ordered render
-> delta applied to immutable full working canvas
```

Preserve:

- `ForceNoChange` and `RejectCandidate` behavior
- GeometryAdd/GeometryCarve/Affordance/Marker permissions
- GeometryCarve substrate rules
- AbsoluteProtected write/change `0/0`
- plan-union target rather than full-canvas MAP10 target
- identical-write coalescing and conflict rejection
- atomic error publication
- canonical ordering/digest/culture independence

Do not synthesize filler, implicit `NoChange`, dummy placement IDs, default pattern IDs, or RNG draws in either branch.

## 6. Minimum MAP11_05 Owner Verification

Refresh/compile and run category `MAP11_05` only.

Add focused cases covering at least:

1. valid empty authored zones + empty placements publishes success;
2. initial/final full working canvases equal the Static Shell and each other;
3. plans/target/render invocation/delta/change/protected-write counts are all zero;
4. derived AbsoluteProtected evidence remains present and validated;
5. reversed empty input enumeration/culture preserves result/digest;
6. a missing/mismatched predecessor still fails atomically in the empty branch;
7. nonprotected zone present + empty placements retains `MissingInput|placements`;
8. one normal placement still uses actual MAP10 planner/renderer and preserves the prior golden behavior.

All existing MAP11_05 focused tests must remain discovered, executed, and PASS. Skip/inconclusive is forbidden.

Normal owner verification ledger:

```text
MAP11_05 focused category: required once after the repair
MAP09 categories: 0
MAP10 categories: 0
MAP11_01~04 categories: 0
legacy 19347: 0
PlayMode: 0
```

Calling existing MAP10/MAP11_01~04 public APIs inside MAP11_05 tests is not selecting their categories.

If MAP11_05 fails, repair only the two exact owner files above and rerun MAP11_05. Do not widen scope.

## 7. Resume MAP11_07 Focused Verification

Only after MAP11_05 focused is fully PASS, rerun category `MAP11_07` only.

The two previously blocked fixtures must now prove:

```text
All 16 clusters:
  13 CSV -> importer/catalog -> MAP11_01~04
  -> pattern-free MAP11_05 working canvas

Four Quiet clusters:
  pattern-free MAP11_05 working canvas
  -> MAP11_06 pool compile
  -> exact one candidate per biome per supported-use query
  -> RNG draws 0
```

Required final MAP11_07 evidence:

- all 10 currently discovered tests execute and PASS, plus any strictly task-owned cases added during repair;
- 16/16 catalog publication and compiler chain;
- 16 structural signatures and duplicate count 0;
- exact four Quiet candidates, one per biome;
- every supported biome/use query returns exact one deterministic candidate;
- renderer invocation/plan/delta/change counts are 0 for pattern-free bridge;
- Generated CSV, Scene, Prefab, Tilemap, RNG, selection side effects remain 0.

If MAP11_07 fails for a task-owned content/import/test reason, modify only MAP11_07-owned files and rerun MAP11_07. Do not rerun MAP11_05 unless its two owner files change again.

## 8. Exact Regression Limits

This repair is the explicit actual-problem trigger for MAP11_05. Permitted selections are only:

```text
MAP11_05 focused after owner edit
MAP11_07 focused after owner PASS
```

Do not select:

```text
MAP09 categories or repaired single method
MAP10 categories
MAP11_01~04 or MAP11_06 categories
legacy 19347
PlayMode
```

Do not repeatedly run either permitted category without a corresponding owner-file change. Test-runner initialization attempts executing zero tests must be reported separately and are not PASS evidence.

## 9. Atomic Failure / Success Rules

If MAP11_05 owner verification fails:

- do not widen source/test scope;
- do not claim MAP11_07 completion;
- keep MAP11_07 `CURRENT` and MAP11_08 `LOCKED`;
- no Status Finalize, commit, or push.

If MAP11_05 passes but MAP11_07 fails:

- keep the successfully repaired MAP11_05 files;
- repair only MAP11_07-owned importer/catalog/test/CSV files when necessary;
- rerun only MAP11_07;
- keep MAP11_08 locked until MAP11_07 PASS is reviewed.

PASS finalization and atomic commit may include:

- original MAP11_07 Task and R1–R4 addenda;
- exact R1/R2/R3 authority changes already reported;
- the two exact MAP11_05 owner files in Section 2;
- MAP11_07-owned Runtime/Editor/test/meta and 13 CSV/meta files;
- PASS Result and finalized Status.

Do not stage or commit unrelated paths. Git push is forbidden.

## 10. Required Result Rewrite

Rewrite the same Result path:

```text
REPORTS/MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS_RESULT.md
```

Header:

```text
TASK: MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS
STATUS: PASS | BLOCKED
MAP11_07: COMPLETE ELIGIBLE | NOT COMPLETE
MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES: LOCKED / DO NOT START
```

The first section remains Korean `## User-Facing Implementation Report`, followed by `## Responsibility and Added Functions`.

Report at file level:

1. every added/modified script and its individual responsibility;
2. every one of the 13 CSV files and its content responsibility;
3. exact 16-cluster biome/pacing matrix;
4. newly enabled functions and actual pipeline position;
5. unfinished work and Editor/game visibility timing;
6. exact MAP11_05 pattern-free predicate, output evidence, and patterned-behavior preservation;
7. MAP11_05 and MAP11_07 focused counts;
8. zero selections for prohibited previous/legacy/PlayMode categories.

Mandatory lineage/evidence:

```text
original MAP11_07 Task SHA
R1/R2/R3/R4 repair SHA values
latest prior BLOCKED Result SHA
exact two MAP11_05 modified files and reason
MAP11_05 no-pattern report/digest/count evidence
13 CSV bytes/SHA/header/row counts
16 catalog/structural/compiler/Quiet evidence
pre/post Authoring inventory/manifests
unrelated staged paths: 0
Git push: NOT PERFORMED
```

PASS여도 MAP11_08은 자동 시작하지 않고 STOP한다.
