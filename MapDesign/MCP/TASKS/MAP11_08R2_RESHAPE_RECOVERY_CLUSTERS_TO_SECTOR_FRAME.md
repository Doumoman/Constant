```yaml
mcp_repair:
  format: current_task_repair_v1
  repair_id: MAP11_08R2_RESHAPE_RECOVERY_CLUSTERS_TO_SECTOR_FRAME
  repairs_current_task: MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES
  requires_current_task: MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES
  requires_blocked_result:
    path: REPORTS/MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES_RESULT.md
    status: BLOCKED
    sha256: 9ebe415a79d26f83f473bd574548872c663531957b12891e74671673dd2c0ba9
  requires_installed_task:
    path: TASKS/MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES.md
    sha256: fe790c7380326e7b3b9a02d1332b7ad3ab3233af045485d0e552f44b22990e30
  preserves_current_task: MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES
  next_task_remains_locked: MAP11_09_MAP11_CLUSTER_EXIT_TESTS
```

# MAP11_08R2 — Reshape Recovery Clusters to Sector Frame

```text
REPAIR: MAP11_08R2_RESHAPE_RECOVERY_CLUSTERS_TO_SECTOR_FRAME
CURRENT TASK: MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES
STATUS EFFECT: NONE — MAP11_08 stays CURRENT
NEXT: MAP11_09_MAP11_CLUSTER_EXIT_TESTS stays LOCKED
```

## 0. Repair Decision

The MAP11_08 preview correctly detected that all four approved 5-chunk Recovery starters are authored as one horizontal row:

```text
(0,0), (1,0), (2,0), (3,0), (4,0)
local canvas: 60×8 tiles
```

A TerrainCluster intended for a `48×32` Sector cannot fit when its chunk bounding box is `5×1`. Enlarging only the diagnostic frame would hide a real downstream MAP14 placement failure.

This repair keeps every approved content identity and exact five active chunks, but reshapes the four Recovery footprints inside the existing `4×4` chunk Sector capacity. It reauthors only those four clusters' coordinate/topology evidence and adds a permanent authoring validation gate:

```text
chunk bounding width <= 4
chunk bounding height <= 4
compiled local width <= 48 tiles
compiled local height <= 32 tiles
```

No cluster is cropped, scaled, rotated, split across sectors, or reduced to four chunks.

## 1. Apply / Audit Procedure

This is not a new Master Task. Do not run the normal `NONE -> CURRENT` task-open flow.

Preflight must verify:

1. Current Task is exact `MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES` and remains `CURRENT`.
2. MAP11_09 remains `LOCKED`.
3. The current BLOCKED Result status/SHA matches this file's metadata.
4. Original MAP11_08 Task SHA matches this file's metadata.
5. Installed/archive MAP11_08R arithmetic repair is byte-identical:

```text
MAP11_08R_CORRECT_TERRAIN_CLUSTER_COLUMN_TOTAL.md
79a668030e333fe62e5e761f9e31830bf0f105f5b437778c33cf08e01ef9d170
```

6. The current authoritative schema remains full `24/143/44`, TerrainCluster `13/89`.
7. Authoring remains `65/65`, TerrainCluster `13/13`, Generated `0`.
8. Catalog remains `16/16`, and only the four exact Recovery clusters have `5×1` chunk bounds / `60×8` local canvases.
9. The current task-owned MAP11_08 files exist exactly as reported:

```text
TerrainClusterPreviewModel.cs(.meta)
TerrainClusterPreviewWindow.cs(.meta)
TerrainClusterPreviewTests.cs(.meta)
TerrainClusterGrayboxPlayModeTests.cs(.meta)
```

10. Latest focused results are MAP11_08 EditMode `3 PASS / 2 FAIL` and PlayMode `3 PASS / 1 FAIL`, with every remaining failure owned only by `SectorFrameOverflow`.
11. No unrelated staged path or other unapplied inbox candidate exists.

Install this repair byte-identically as:

```text
MCP/TASKS/MAP11_08R2_RESHAPE_RECOVERY_CLUSTERS_TO_SECTOR_FRAME.md
MCP_ARCHIVE/MAP11_08R2_RESHAPE_RECOVERY_CLUSTERS_TO_SECTOR_FRAME.md
```

Move/remove the inbox source after both copies match its SHA. Do not change Master or Status during repair installation. The original MAP11_08 Task plus R and R2 form the effective specification.

Any mismatch is `BLOCKED` without additional project modification.

## 2. Exact Recovery Footprints

Replace only the active chunk coordinates of these four clusters.

### 2.1 MoonCrater stepped shelf

```text
TC_CRATER_ROCK_SHELF_RECOVERY
(0,0), (1,0), (2,0), (2,1), (3,1)
bounds: 4×2 chunks = 48×16 tiles
```

### 2.2 CassiaRoot fork

```text
TC_ROOT_FORKED_CANOPY_RECOVERY
(0,1), (1,0), (1,1), (1,2), (2,1)
bounds: 3×3 chunks = 36×24 tiles
```

### 2.3 AbandonedMill orthogonal shaft

```text
TC_MILL_ORTHOGONAL_SHAFT_RECOVERY
(0,2), (1,0), (1,1), (1,2), (2,0)
bounds: 3×3 chunks = 36×24 tiles
```

### 2.4 MoonDough sticky rise

```text
TC_DOUGH_STICKY_RISE_RECOVERY
(0,0), (0,1), (1,1), (1,2), (2,2)
bounds: 3×3 chunks = 36×24 tiles
```

Each footprint must remain normalized, duplicate-free and four-neighbor connected with exact five active chunks. The four shapes must remain structurally distinct from each other and from the other 12 starters.

Preserve exact cluster IDs, biome, Recovery pacing, footprint/spine variant IDs, two variants, one baseline, `L -> D` primary sides, role kinds, benefit IDs and five-chunk classification.

## 3. Coordinate and Route Reauthoring

Reauthor only rows owned by the four clusters when necessary to make the approved shape truthful.

Permitted physical CSV files:

```text
terrain_cluster_cells_v2.csv
terrain_cluster_role_anchors_v2.csv
terrain_cluster_role_variant_links_v2.csv
terrain_cluster_ports_v2.csv
terrain_cluster_nodes_v2.csv
terrain_cluster_spine_edges_v2.csv
terrain_cluster_envelope_cells_v2.csv
terrain_cluster_high_routes_v2.csv
terrain_cluster_high_route_edges_v2.csv
terrain_cluster_high_route_failures_v2.csv
```

Do not modify rows owned by the other 12 clusters.

The following files/semantics remain unchanged:

```text
terrain_cluster_catalog_v2.csv
terrain_cluster_variants_v2.csv
terrain_cluster_high_route_benefits_v2.csv
all cluster/variant/role/port/high-route/benefit stable IDs unless an existing FK makes preservation impossible
```

Prefer preserving every stable node/edge ID and existing directed topology while moving explicit coordinates. If an ID/topology change is necessary, keep it inside the same cluster and report the exact before/after reference set; do not rename for aesthetics.

For every reauthored cluster and both variants:

- primary Entry is on the external `L` side;
- primary Exit is on the external `D` side and owns a different active chunk from Entry;
- Entry/BuildUp/Core/Recovery/Exit anchors remain explicit; Reward remains present for Recovery content;
- baseline visits all five intended active chunks in authored traversal order;
- alternate variant is structurally distinct from baseline;
- high route has explicit divergence/rejoin/high point, ordered edges and at least two existing benefits;
- failure node and preferred recovery target remain explicit;
- recovery uses source edges only and remains `2000..5000 ms`;
- node/edge coordinates, landing/recovery coordinates and envelope evidence agree exactly;
- Static Shell and AbsoluteProtected route coordinates remain valid and conflict-free;
- pattern-free working canvas publishes with exact `5×96 = 480` active-cell coverage.

Do not tunnel, synthesize teleport edges, infer route meaning from IDs, or change movement/AccessClass/RouteType authority.

All modified CSVs retain UTF-8 BOM, exact header, canonical PK order, LF-only and one final LF. Matching `.meta` files and GUIDs remain unchanged.

## 4. Permanent Sector-Fit Authoring Gate

The importer/validation boundary must reject future TerrainCluster content whose normalized active-chunk bounding box exceeds the one-sector chunk frame.

Permitted existing MAP11_07 owner files:

```text
Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/Authoring/TerrainClusterAuthoringValidation.cs
Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/Authoring/TerrainClusterAuthoringRows.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/TerrainClusters/TerrainClusterStarterContentTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Import/TerrainClusterCsvImporterV2Tests.cs
```

Modify `TerrainClusterAuthoringRows.cs` only if the current stable error surface has no appropriate footprint/bounds error. Add at most one exact stable error code; do not refactor the result model.

Validation rules:

1. Compute min/max active chunk coordinates after the existing normalization/connectedness checks.
2. `chunkWidth = maxX - minX + 1`; `chunkHeight = maxY - minY + 1`.
3. Require `chunkWidth <= 4` and `chunkHeight <= 4`.
4. Report cluster ID, observed width/height and allowed `4×4` in the stable error.
5. Any such error preserves existing atomic zero-publication behavior.
6. Add invalid fixtures for `5×1` and `1×5` plus valid connected five-chunk shapes.

Reuse existing 12×8 MicroChunk and 48×32 Sector constants/contracts when publicly available. If no public constant exists at this layer, centralize exact `4×4` in the validation source with a comment identifying the approved Sector/MicroChunk contract. Do not add a Sector Planner dependency.

## 5. Exact Change Boundary

Allowed:

- four-cluster rows in the ten CSVs listed in Section 3
- matching existing MAP11_07 owner source/tests in Section 4
- the already-created MAP11_08 task-owned files
- installed/archive R2, rewritten Result and PASS-only Status/commit

Forbidden:

- schema registry/descriptors/digests or MAP09 schema tests
- CSV headers, table count, column count or FK contract
- any row owned by the other 12 clusters
- catalog/variant/benefit CSV changes
- matching CSV/meta/GUID changes
- MAP11_01~06 production/test changes
- MicroPattern CSV/catalog change
- 5-chunk count reduction, rotation/scale/crop/reflow at preview time
- frame enlargement beyond 48×32
- Sector Planner, world placement, Scene/Prefab/Tilemap/physics
- asmdef/asmref/Settings/Packages
- unrelated modify/stage/commit or Git push

If the exact four shapes cannot satisfy existing MAP11_01~06 public contracts within this boundary, report the cluster, rows and failed authority as `BLOCKED`; do not substitute silent shapes.

## 6. Minimum Owner Verification

Because approved MAP11_07 content and its authoring validation change, run category `MAP11_07` only after reauthoring.

Required evidence:

```text
MAP11_07 focused: all discovered/executed PASS
catalog entries: 16/16
Recovery active chunks: 5/5/5/5
all 16 chunk bounds: <= 4×4
all 16 local canvas bounds: <= 48×32
variants/baselines: 32/16
structural signatures: 16, duplicates 0
Quiet pool: exact four, unchanged eligibility/query behavior
pattern-free compile chain: 16/16
```

Do not select MAP09, MAP10 or MAP11_01~06 categories. Calling their public APIs inside MAP11_07 tests is permitted.

If MAP11_07 fails for the four-cluster reauthoring or new validation gate, repair only Section 3/4 files and rerun MAP11_07. Do not widen scope.

## 7. Resume MAP11_08 Verification

Only after MAP11_07 focused is fully PASS, continue the existing MAP11_08 task-owned implementation.

Run only:

```text
MAP11_08 EditMode focused
MAP11_08 PlayMode focused
```

Required corrected proof:

- all 16 clusters × two variants publish preview snapshots;
- all 16 translation-only projections fit `[0..47]×[0..31]`;
- no `SectorFrameOverflow` remains;
- exact Pattern A/B representative pairs publish non-empty diffs and protected `0/0`;
- density/count/digest/overlay evidence remains deterministic;
- exact four PlayMode representatives render inside 48×32 and teardown cleanly;
- Editor menu/window Reload/selectors/modes/overlays render without Console error.

If a MAP11_08-owned preview/test defect remains, modify only new MAP11_08 files and rerun only the affected MAP11_08 mode. Do not rerun MAP11_07 unless Section 3/4 files change again.

## 8. Regression Limits

This is an actual owner trigger. The only permitted selections are:

```text
MAP11_07 focused after content/validation repair
MAP11_08 EditMode focused after MAP11_07 PASS
MAP11_08 PlayMode focused after MAP11_07 PASS
```

Do not select MAP09/MAP10/MAP11_01~06, legacy 19347, MAP11_09 or an unfiltered PlayMode suite.

Every test request, executed count and retry reason must be reported. Initialization attempts with executed 0 are separate tooling evidence, not PASS.

## 9. Required Result Rewrite

Rewrite:

```text
REPORTS/MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES_RESULT.md
```

Header:

```text
TASK: MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES
STATUS: PASS | FAIL | BLOCKED
MAP11_08: COMPLETE ELIGIBLE | NOT COMPLETE
MAP11_09_MAP11_CLUSTER_EXIT_TESTS: LOCKED / DO NOT START
```

First section must be Korean `## User-Facing Implementation Report`, followed by `## Responsibility and Added Functions`.

Report:

1. every added/modified script and its individual responsibility;
2. exact CSV files and four-cluster row responsibilities changed;
3. newly visible Editor Preview/PlayMode functionality;
4. actual pipeline position and remaining production work;
5. exact old/new footprints, local bounds, entry/exit and route evidence;
6. new permanent sector-fit validation/error behavior;
7. updated catalog/content/signature/full Authoring digests;
8. MAP11_07, MAP11_08 EditMode and MAP11_08 PlayMode focused counts separately;
9. prohibited/legacy/unfiltered selections `0`;
10. unrelated staged/included paths `0`, push not performed.

PASS finalization/atomic commit may include the original MAP11_08 Task, R/R2 addenda, current Task files, exact approved MAP11_07 content/validation repair files, Result and Status only.

PASS여도 MAP11_09는 자동 시작하지 않고 STOP한다.
