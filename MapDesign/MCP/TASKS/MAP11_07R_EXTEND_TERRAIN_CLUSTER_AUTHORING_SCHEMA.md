```yaml
mcp_repair:
  format: current_task_repair_v1
  repair_id: MAP11_07R_EXTEND_TERRAIN_CLUSTER_AUTHORING_SCHEMA
  repairs_current_task: MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS
  requires_current_task: MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS
  requires_blocked_result:
    path: REPORTS/MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS_RESULT.md
    status: BLOCKED
    sha256: 0e2b164e7e513d7989329104f8a3590fde1530c223ec337d0c5f580a1de80d4e
  requires_installed_task:
    path: TASKS/MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS.md
    sha256: 87c8a482ce73da2d4238926aa0976916b809eae28b517cec3a17fb573a9f8dfd
  preserves_current_task: MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS
  next_task_remains_locked: MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES
```

# MAP11_07R — Extend TerrainCluster Authoring Schema

```text
REPAIR: MAP11_07R_EXTEND_TERRAIN_CLUSTER_AUTHORING_SCHEMA
CURRENT TASK: MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS
STATUS EFFECT: NONE — MAP11_07 stays CURRENT
NEXT: MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES stays LOCKED
```

## 0. Repair Decision

The approved MAP09_07 TerrainCluster schema cannot losslessly represent the already-approved MAP09_04/MAP11_04 contracts. The BLOCKED preflight established exact missing ownership for variants, roles, ports, nodes, high-route intent, benefits, failure nodes, and timing.

This repair explicitly approves a schema contract revision before any starter rows are authored.

```text
MAP09_07 schema before: 15 tables / 83 columns
MAP09_07 schema after:  24 tables / 143 columns
```

The revision:

- preserves all non-TerrainCluster descriptors byte/semantic-identically
- preserves the four approved TerrainCluster paths
- adds nine normalized TerrainCluster companion tables
- expands only the existing spine-edge/envelope descriptors
- keeps Authoring and Generated strictly separated
- creates no alias, JSON/blob column, ID-inference rule, or C# content fallback

## 1. Apply / Audit Procedure

This is not a new Master Task. Do not run the normal `NONE → CURRENT` task-open flow.

Preflight must verify:

1. Current Task is exact MAP11_07 and Status is `CURRENT`.
2. MAP11_08 remains `LOCKED`.
3. BLOCKED Result status/SHA matches metadata.
4. Installed original MAP11_07 Task SHA matches metadata.
5. Current MAP09_07 registry is exact `15/83`, digest `272ec4f449a17179158720c94e92f6982cb5a32427ce6f6ea8ffc5eb92050621`.
6. No TerrainCluster V2 physical CSV currently exists.
7. No other unapplied inbox candidate or unrelated staged path exists.

Install this repair byte-identically as:

```text
MCP/TASKS/MAP11_07R_EXTEND_TERRAIN_CLUSTER_AUTHORING_SCHEMA.md
MCP_ARCHIVE/MAP11_07R_EXTEND_TERRAIN_CLUSTER_AUTHORING_SCHEMA.md
```

Move/remove the inbox source after both copies match its SHA. Do not change Master or Status during repair installation. The original MAP11_07 Task plus this addendum form the effective specification.

Any state/SHA/collision mismatch is `BLOCKED` with zero source/content modification.

## 2. Exact Revised TerrainCluster Table Set

The complete TerrainCluster Authoring set becomes 13 tables.

### 2.1 Preserved catalog descriptor

```text
TerrainCluster/terrain_cluster_catalog_v2.csv
cluster_id,pacing_role,biome_id,footprint_variant_id,spine_variant_id
```

`spine_variant_id` is the explicit baseline variant ID. The importer validates that it exists in the companion variant table and that exactly one baseline is designated per cluster through this catalog reference. Do not infer baseline from ordering or suffix.

### 2.2 Revised footprint-cell semantics

```text
TerrainCluster/terrain_cluster_cells_v2.csv
cluster_id,chunk_x,chunk_y,cell_role,port_id,access_class,source_microchunk_id,source_boundary_chunk_id
```

The header remains unchanged. Exact revised semantics:

- `(cluster_id,chunk_x,chunk_y)` remains the active-footprint PK.
- `cell_role`, `port_id`, and `access_class` are optional legacy compatibility summaries and must be empty in normalized V2 starter rows.
- authoritative role/port/access data comes from the new normalized tables.
- the two approved legacy source IDs remain optional provenance with their existing FK owners.

No importer may merge summary fields with normalized role/port rows. A non-empty summary field in new starter content is rejected as ambiguous authority.

### 2.3 Revised spine edge descriptor

```text
TerrainCluster/terrain_cluster_spine_edges_v2.csv
cluster_id,spine_variant_id,edge_id,from_node_id,to_node_id,movement,start_x,start_y,end_x,end_y,mandatory,graph_kind,clearance_width,clearance_height,landing_width,landing_x,landing_y,recovery_width,recovery_x,recovery_y,estimated_duration_ms,timing_ruleset_id
```

Rules:

- `edge_id` is globally unique within the TerrainCluster authoring catalog.
- `spine_variant_id`, `from_node_id`, and `to_node_id` use explicit FKs.
- start/end coordinates must exactly equal referenced node coordinates.
- `mandatory` is exact Boolean token through the existing schema data type.
- `graph_kind` exact allowed token is `TRAVERSAL`.
- landing/recovery coordinates are explicit; no width-only center inference.
- duration is integer `>0` milliseconds.
- timing ruleset ID grammar is `^TRS_[A-Z0-9_]+$`.

### 2.4 Revised envelope descriptor

```text
TerrainCluster/terrain_cluster_envelope_cells_v2.csv
cluster_id,spine_variant_id,edge_id,envelope_kind,local_x,local_y
```

`edge_id` remains globally unique and is an explicit FK. `spine_variant_id` must equal the owning edge variant. Same-named edge IDs across variants are forbidden rather than disambiguated by file order.

### 2.5 New variant table

```text
TerrainCluster/terrain_cluster_variants_v2.csv
cluster_id,spine_variant_id,graph_kind
```

PK: `spine_variant_id` globally unique. `cluster_id` FK to catalog. `graph_kind` exact `TRAVERSAL`.

### 2.6 New role-anchor table

```text
TerrainCluster/terrain_cluster_role_anchors_v2.csv
cluster_id,role_anchor_id,role_kind,local_x,local_y
```

PK: globally unique `role_anchor_id`. Exact existing role tokens only. Coordinates are cluster-local tile coordinates and must belong to an active chunk.

### 2.7 New role-to-variant-node link table

```text
TerrainCluster/terrain_cluster_role_variant_links_v2.csv
cluster_id,spine_variant_id,role_anchor_id,node_id
```

PK: `(spine_variant_id,role_anchor_id)`. Explicit FKs to variant, role anchor, and node. This table is the sole authority for every all-variant role↔node link.

### 2.8 New port table

```text
TerrainCluster/terrain_cluster_ports_v2.csv
cluster_id,port_id,port_kind,is_primary,role_anchor_id,local_x,local_y,outward_side,compatible_route_types,access_class
```

Rules:

- globally unique `port_id`
- `port_kind`: exact `ENTRY` or `EXIT`
- `is_primary`: exact Boolean
- role anchor and tile coordinate must match
- outward side exact `L/R/U/D`
- compatible RouteTypes use canonical ascending `|`-separated integers from `0..4`, no spaces/duplicates/aliases
- access uses existing exact AccessClass token

### 2.9 New traversal-node table

```text
TerrainCluster/terrain_cluster_nodes_v2.csv
cluster_id,spine_variant_id,node_id,local_x,local_y,mandatory
```

PK: globally unique `node_id`. Explicit variant FK. Coordinates must be active and exact. Role ownership is expressed only through the role-variant link table.

### 2.10 New high-route header table

```text
TerrainCluster/terrain_cluster_high_routes_v2.csv
cluster_id,spine_variant_id,high_route_id,divergence_node_id,rejoin_node_id,high_point_node_id
```

PK: globally unique `high_route_id` matching existing grammar. All node references are explicit FKs and belong to the same variant.

### 2.11 New high-route ordered-edge table

```text
TerrainCluster/terrain_cluster_high_route_edges_v2.csv
cluster_id,spine_variant_id,high_route_id,edge_order,edge_id
```

PK: `(high_route_id,edge_order)`, with contiguous zero-based order. Explicit route/edge FKs; every edge belongs to the same variant and forms one directed contiguous path.

### 2.12 New high-route benefit table

```text
TerrainCluster/terrain_cluster_high_route_benefits_v2.csv
cluster_id,spine_variant_id,high_route_id,benefit_id
```

PK: `(high_route_id,benefit_id)`. `benefit_id` uses existing `^BENEFIT_[A-Z0-9_]+$` grammar. At least two distinct rows per high route.

### 2.13 New high-route failure table

```text
TerrainCluster/terrain_cluster_high_route_failures_v2.csv
cluster_id,spine_variant_id,high_route_id,failure_node_id,preferred_recovery_target_node_id
```

PK: `(high_route_id,failure_node_id)`. Failure node explicit FK on high path. Preferred target is optional; when present it is an explicit node FK on the baseline path. Recovery edges/timings remain source traversal edges, never synthetic rows.

## 3. Registry and FK Rules

After repair the registry must publish exact:

```text
all V2 tables: 24
all ordered columns: 143
TerrainCluster tables: 13
approved legacy FK edges: exact 2 unchanged
Generated table/FK target: 0
```

Use current single-column FK infrastructure by requiring parent stable IDs (`spine_variant_id`, `role_anchor_id`, `port_id`, `node_id`, `edge_id`, `high_route_id`) to be globally unique within the TerrainCluster catalog.

Do not modify descriptor model/validation/digest infrastructure merely to add composite-FK grouping. Composite PKs remain allowed for child row uniqueness; their parent references use the globally unique stable-ID columns.

Every row repeats `cluster_id` for ownership. Runtime importer must cross-check that all referenced stable IDs share the same cluster and, where applicable, the same variant. A valid FK with cross-owner mismatch is still an import error.

The revised canonical schema digest is computed by the existing authority and reported; do not hard-code or predict it in production.

## 4. Exact Existing Source/Test Repair Boundary

Schema repair may modify only:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Data/V2AuthoringSchemaRegistry.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/V2AuthoringSchemaRegistryTests.cs
```

Additionally, locate the exact single existing focused test source whose category is `MAP09_08` and whose golden evidence asserts `15 tables / 83 columns / 13 FK`. Modify only its schema expectation block to the revised table/column/FK membership and digest. Do not relax any other exit gate or alter its meta/GUID.

If the current descriptor infrastructure cannot express the revised tables using the existing public descriptor types, return `BLOCKED`. Do not modify:

```text
V2AuthoringSchemaContracts.cs
V2AuthoringSchemaValidation.cs
V2AuthoringSchemaCanonicalDigest.cs
```

No other MAP09 production/test file is allowed.

## 5. Owner Verification Trigger

This is an actual regression trigger owned by MAP09_07 schema authority. After the schema edit and before starter content authoring, run only:

```text
MAP09_07 focused
MAP09_08 focused
```

Requirements:

- all discovered executed and PASS
- compile/Console/relevant warning `0/0/0`
- exact 24/143/13-table membership
- two legacy FKs unchanged
- non-TerrainCluster descriptors semantic digest slices unchanged
- Generated target/table 0
- reversed enumeration/culture deterministic

Do not run MAP09_01~06, MAP10, MAP11_01~06, legacy 19347, or PlayMode categories during this owner verification.

## 6. Expanded MAP11_07 Physical CSV Boundary

After owner verification PASS, create the exact 13 TerrainCluster physical CSVs and matching metas under:

```text
Assets/_Game/Map/Data/WorldGeneration/Authoring/TerrainCluster/
```

This includes the original four plus all nine companion filenames in Section 2. Reuse the existing folder/meta. Every file uses UTF-8 BOM, exact descriptor header, LF only, one final LF, canonical PK row order.

Authoring inventory changes:

```text
before: 52 CSV/meta
after:  65 CSV/meta
TerrainCluster new physical CSV/meta: 13/13
Generated CSV: 0
```

The original MAP11_07 importer/catalog/tests write boundary remains allowed, expanded from four to thirteen exact input files. The importer reads all thirteen exact paths and no others.

## 7. Effective MAP11_07 Content Rules

All original MAP11_07 starter rules remain binding except these superseded points:

1. Representability now uses this repaired 24-table/143-column schema.
2. Physical TerrainCluster CSV count is 13, not 4.
3. Post-task Authoring count is 65, not 56.
4. Role/port/access authority comes from normalized companion tables; legacy summary fields in `terrain_cluster_cells_v2.csv` remain empty.
5. Multiple variants, exact baseline, route intent, high point, ordered high edges, benefits, failure nodes, timing and recovery evidence use the explicit repaired tables/columns.

The exact 16-cluster biome/pacing/footprint/side matrix, structural signature uniqueness, MAP11_01~06 compile proof, pattern-free bridge, Quiet pool proof, no RNG/placement, and all non-ownership rules are unchanged.

No starter semantic may be generated from cluster ID parsing except the original explicitly approved `QBUF_ + validated TC_ suffix` projection for MAP11_06 profile identity.

## 8. MAP11_07 Focused Verification After Repair

Run `MAP11_07` category only after the two owner categories PASS.

In addition to the original 20 cases, verify:

1. exact 13 TerrainCluster descriptors/headers and 24/143 total
2. global stable-ID uniqueness across the 16-cluster catalog
3. every new FK plus repeated cluster/variant ownership agreement
4. exact one catalog baseline variant reference per cluster
5. all-variant role links are explicit and complete
6. primary Entry/Exit port role/tile/side/RouteType/access exact
7. node/edge coordinate equality and graph ownership
8. high route ordered edges/benefits/failures explicit, no inference
9. edge duration/ruleset and landing/recovery coordinates explicit
10. old summary role/port/access cells empty

Normal corrected selection summary:

```text
MAP09_07: permitted once as triggered owner verification
MAP09_08: permitted once as dependent exit verification
MAP11_07: required implementation verification
MAP09_01~06 selections: 0
MAP10/MAP11_01~06 selections: 0
Legacy 19347 selections: 0
PlayMode selections: 0
```

Prior public APIs may be called inside MAP11_07 tests without selecting their categories.

## 9. Atomic Failure and Rollback

If schema owner verification fails:

- do not create any physical TerrainCluster CSV or starter code
- report modified schema files and exact failure
- keep MAP11_07 CURRENT and MAP11_08 LOCKED
- no Status Finalize/commit/push

If MAP11_07 task-owned implementation fails after schema verification:

- repair only task-owned importer/catalog/test/CSV files
- rerun MAP11_07 only
- do not repeatedly rerun MAP09_07/08 unless the schema files changed again

Any error publishes no partial imported catalog/compiled starter matrix. Physical CSV rows may exist in the worktree while repairing but PASS requires all 16 and all 13 files to satisfy the atomic importer/content gates.

## 10. Required PASS Result

Rewrite the same Result path:

```text
REPORTS/MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS_RESULT.md
```

Header remains:

```text
TASK: MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS
STATUS: PASS | BLOCKED
MAP11_07: COMPLETE ELIGIBLE | NOT COMPLETE
MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES: LOCKED / DO NOT START
```

The first section remains Korean `## User-Facing Implementation Report` and must report all added/modified scripts, each responsibility, 13 CSV data changes, newly enabled functionality, pipeline position, unfinished work, and game visibility.

Then `## Responsibility and Added Functions` with functions, inputs, outputs, non-ownership, and downstream consumers.

Additional mandatory evidence:

```text
original MAP11_07 Task SHA
MAP11_07R repair SHA
prior BLOCKED Result SHA
schema before/after tables/columns/FKs/digests
exact modified MAP09_07/08 source/test files and reason
MAP09_07 and MAP09_08 focused counts
all 13 CSV headers/rows/bytes/SHA/BOM/LF/final-LF
pre/post Authoring inventory/manifests
16 cluster matrix and structural signatures
MAP11_01~06 in-category compile artifacts
MAP11_07 focused counts
all previous/legacy/PlayMode selection counts
REGRESSION TRIGGER owner/reason/minimum scope/resolution
```

PASS finalization/commit scope:

- original Task and repair addendum
- exact schema registry source and its MAP09_07 test
- exact one MAP09_08 test source if required by Section 4
- task-owned importer/catalog/test files
- 13 TerrainCluster CSV/meta files
- PASS Result and finalized Status

```text
Subject: MAP11_07: author starter 16 terrain clusters
Push: NOT PERFORMED
```

PASS여도 MAP11_08은 자동 시작하지 않고 STOP한다.
