# MAP09_07 - Extend CSV Registry and Create Compatibility Fixtures Result

```text
TASK: MAP09_07_EXTEND_CSV_REGISTRY_AND_CREATE_COMPATIBILITY_FIXTURES
STATUS: PASS
MAP09_07: COMPLETE ELIGIBLE
MAP09_08_MAP09_CONTRACT_EXIT_AUDIT: LOCKED / DO NOT START
```

## Responsibility and Added Functions

| Field | Implemented responsibility |
|---|---|
| Task responsibility | Adds the immutable schema registry, PK/FK graph, exact lookup index, canonical digest, and compatibility proof needed to author the MAP09_03~06 contracts as future V2 CSV data without creating those CSV files now. |
| Added functions | Adds `V2AuthoringTableDescriptor`, `V2AuthoringColumnDescriptor`, `V2AuthoringForeignKey`, `V2AuthoringSchemaRegistry`, accumulating validation Result/Error, `V2AuthoringForeignKeyIndex`, canonical SHA-256 digest, and focused MAP07/MAP08 read-only compatibility fixtures. |
| Inputs consumed | Reuses `CsvSchemaDataType`, the approved legacy `CsvSchemaCatalog`, MAP09 contract enum semantics, MAP07 public `MicrochunkDefinition`, and the current MAP08 Authoring boundary evidence/digest. |
| Outputs produced | Publishes a validated immutable 15-table/83-column registry, exact path/column/PK/FK indexes, digest `272ec4f449a17179158720c94e92f6982cb5a32427ce6f6ea8ffc5eb92050621`, and read-only compatibility evidence. Invalid descriptors publish no registry, index, or digest. |
| Explicit non-ownership | Does not create physical V2 CSV/meta, dictionary rows, Generated mirrors, importer/exporter/writer/editor UI, solver/composer/renderer/slicer, streaming/save logic, RNG, or Unity lifecycle behavior. |
| Downstream consumers | MAP09_08 audits the complete MAP09 contract surface; MAP10~17 content authoring, assembly, generation, validation, baking, and streaming phases may consume the approved schema descriptors without changing legacy MAP07/MAP08 sources. |

## Predecessor, Status, and Dirty Preflight

The only immediate root inbox Markdown candidate passed every `single_task_v1` identity, predecessor, hash, destination, status, membership, encoding, and staging gate. It was installed and archived byte-identically before implementation began.

```text
Preflight HEAD: b1736da231aa931ea0bfcd4ee5446c2e89ac1e4b
MAP09_06 Result status: PASS
MAP09_06 Result SHA-256:
bb665f3a7e61f6d8804923afaae1f805eca89f3642b67b89c4ed9730ef2b3135
MAP09_06 installed/archive Task SHA-256:
ebea8d166311b9fee8df2c89cb41be9ff6b438a475e0242c1b3fd019daa7a951
MAP09_07 inbox/installed/archive bytes: 13464/13464/13464
MAP09_07 inbox/installed/archive SHA-256:
49aca5871b2c93ab3e002d54c457d08d92abaff1213ce4917a49cad8b7c976e6
Installed/archive byte-identical: YES
Status before open: 215 rows; COMPLETE 113 / CURRENT 0 / LOCKED 102
Status after open:  215 rows; COMPLETE 113 / CURRENT 1 / LOCKED 101
Root unapplied candidates after apply: 0
Staged paths before task execution: 0
```

No unrelated dirty path existed at preflight. No unrelated path was modified or staged.

Compiled live predecessor evidence matched the approved Results:

```text
MAP09_01 pass count/digest:
10 / 90a2614f9a95c29f1546f350190010524672d4b4aa2d1ad1dfe7dbd431be50d5
MAP09_02 layer count/digest:
7 / d0888c865cbdcc0884dc8abab9fac92900addd662a12a1ec30dc930f9cf4c94e
MAP09_03 MicroPattern fixture digest:
42c88cdb30154f098593d0e3be65063111613612fe5e9e1b9b11f2d9f1297a3d
MAP09_04 TerrainCluster fixture digest:
e8c3228e6f9df360637023d68e9c243cb70df4122342a3251740054bbcc8f9f1
MAP09_05 Activity fixture digest:
7a5357320d8e2634ab9416ae7c90fb80a83c1c7f799a8df7689ba37b8a0903bc
MAP09_05 Event fixture digest:
722a490f054e5bfc5a75ac81e03eee4978cd7f51d34e01fa1e01818c9d4ce904
MAP09_06 SpecialRegion fixture digest:
73fd2085ecf65057f25eec8b2ff4fceb1a4d1a1a0eadfd60b7595071936a7066
MAP09_06 Canvas/Stamp/Slice fixture digests:
7c26d2d12d418a6f203e793bffd49216c003a6c0fc6f6f2bea06d210d3bded0c
cb909e6a1fc2a14bbd4e8b5a6ab103b5926e0428f535163f428f8dafda38a9f6
2066f58b09e3ac8ef0118c54e243008f54bcefe1e3bb032fa67dbe5d25156368
```

## Implemented File Inventory

New Runtime schema files and Unity-generated matching metas:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Data/V2AuthoringSchemaContracts.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/V2AuthoringSchemaContracts.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/Data/V2AuthoringSchemaRegistry.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/V2AuthoringSchemaRegistry.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/Data/V2AuthoringSchemaValidation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/V2AuthoringSchemaValidation.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/Data/V2AuthoringSchemaCanonicalDigest.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/V2AuthoringSchemaCanonicalDigest.cs.meta
```

New focused compatibility test and matching meta:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/V2AuthoringSchemaRegistryTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/V2AuthoringSchemaRegistryTests.cs.meta
```

Task/protocol documents:

```text
MapDesign/MCP/TASKS/MAP09_07_EXTEND_CSV_REGISTRY_AND_CREATE_COMPATIBILITY_FIXTURES.md
MapDesign/MCP_ARCHIVE/MAP09_07_EXTEND_CSV_REGISTRY_AND_CREATE_COMPATIBILITY_FIXTURES.md
MapDesign/MCP/REPORTS/MAP09_07_EXTEND_CSV_REGISTRY_AND_CREATE_COMPATIBILITY_FIXTURES_RESULT.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

## V2 Authoring Schema and Digest

The registry publishes exactly these 15 future Authoring paths under the five approved roots and no others:

```text
MicroPattern/micro_pattern_catalog_v2.csv
MicroPattern/micro_pattern_cells_v2.csv
TerrainCluster/terrain_cluster_catalog_v2.csv
TerrainCluster/terrain_cluster_cells_v2.csv
TerrainCluster/terrain_cluster_spine_edges_v2.csv
TerrainCluster/terrain_cluster_envelope_cells_v2.csv
Activity/activity_catalog_v2.csv
Activity/activity_cues_v2.csv
Activity/activity_graph_edges_v2.csv
EventOverlay/event_overlay_catalog_v2.csv
EventOverlay/event_overlay_markers_v2.csv
SpecialRegion/special_region_catalog_v2.csv
SpecialRegion/special_region_cells_v2.csv
SpecialRegion/special_region_ports_v2.csv
SpecialRegion/special_region_persistence_v2.csv
```

The 83 ordered columns preserve the MAP09 contract meanings, including MicroPattern operations/layers/protected policy, TerrainCluster role/access/spine/envelope tokens, Activity Mechanism/Progression graph kind and edge order, EventOverlay weighted Empty variants, and SpecialRegion fixed-shell/slot/port/persistence semantics.

```text
Registry validation: PASS
Tables/columns: 15/83
Total V2 descriptor FK edges: 13
Approved legacy FK edges: 2
Canonical SHA-256:
272ec4f449a17179158720c94e92f6982cb5a32427ce6f6ea8ffc5eb92050621
```

The digest includes relative table path, table ID/owner, ordered column name/order/type/required/default/allowed values, PK order, and FK domain/file/column. It excludes display text, descriptions, timestamps, reflection order, and input enumeration order.

## PK, FK, Validation, and Index Evidence

Every table has at least one required PK and every composite PK order is contiguous from one. Child tables carry explicit parent stable-ID FKs. V2 targets and legacy targets must exist and target a PK column.

The only legacy edges are:

```text
terrain_cluster_cells_v2.csv.source_microchunk_id
  -> microchunk_catalog.csv.microchunk_id
terrain_cluster_cells_v2.csv.source_boundary_chunk_id
  -> boundary_chunk_catalog.csv.boundary_chunk_id
```

Both source columns are optional provenance only. The validator rejects duplicate identity/order/PK/FK, missing targets, non-PK targets, unapproved legacy targets, case-insensitive collisions, V2 cycles, Generated targets, and Generated paths. Errors accumulate, deduplicate, and stable-sort. Any error leaves `Registry`, `ForeignKeyIndex`, and `CanonicalDigest` null.

The published index performs ordinal exact lookup by relative path, `(file,column)`, ordered PK columns, and deterministic incoming/outgoing FKs. All descriptor, registry, allowed-value, PK, and edge collections reject external mutation.

## MAP07 and MAP08 Compatibility Evidence

The focused fixture read the current active MAP07 public model without modifying, cloning to disk, or resaving it. The projection contains only legacy-source evidence for `terrain_cluster_cells_v2.source_microchunk_id`; it is not promoted to a 4x4 MicroPattern or GeneratedSlice source.

```text
MAP07 source ID: MC_GRAY_H_STRAIGHT_01
Geometry/cells: 12x8 / 96 unique row-major cells
Sockets: 2 with exact socket/edge identity
Tile payload: all eight existing layer codes preserved
Repeated projection digest:
ff43e5f2a9c0ad71822885b695ecfdcb2bd7ee2b768957cc1b76cac1a0728823
Invalid fixture rejection: missing ID, invalid geometry, missing cell,
                           duplicate cell, unknown legacy FK
```

The current MAP08 Authoring snapshot was recalculated through its existing boundary coverage contracts:

```text
Biome pairs: 6
Candidates / source microchunks: 31 / 31
Tile rows / socket rows: 2976 / 62
Directional projections: 62
Mandatory NONE candidates: 31
Aggregate digest:
f7ff1c49f5bc33a4ad57799269bc3915806fe0cb60f347ed76eb16ea26f7fc68
```

Candidate/source/pair/orientation/RouteType/signature identities remain unchanged. `source_boundary_chunk_id` is optional provenance only; MAP08 data and transform policy were not modified.

## Authoring and Generated Separation

All registry paths stay below the five approved Authoring roots. No path contains `Generated`, no table begins `generated_`, no Authoring FK targets a Generated domain, and no Canvas, validation stamp, slice, or provenance artifact is promoted into an Authoring source row.

```text
Legacy Authoring CSV/meta: 50/50
Legacy Authoring manifest:
f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb
Physical V2 Authoring CSV/meta created: 0/0
Generated CSV: 0
```

## Focused Validation and Regression Policy

The first focused execution discovered 22 cases and produced 19 PASS / 3 FAIL. All three failures were localized to new test assertions using an NUnit `Has.Count` property constraint on `IEnumerable` projections; the registry, compatibility data, compiled predecessors, and baselines were unaffected. The assertions were changed to explicit LINQ counts and the same focused selection was rerun. No previous Task category or legacy selection was needed to localize or repair the issue.

Final authoritative execution:

| Selection | Discovered | Executed | Passed | Failed | Skipped | Inconclusive |
|---|---:|---:|---:|---:|---:|---:|
| MAP09_07 | 22 | 22 | 22 | 0 | 0 | 0 |

```text
MAP09_07 focused: 22 discovered / 22 executed / 22 passed / 0 failed / 0 skipped
REGRESSION TRIGGER DETECTED: YES
Trigger owner: MAP09_07 new focused test assertion only
Baseline drift: NONE
Repair/minimum selection: MAP09_07 focused only
PRIOR TASK TEST SELECTIONS: 0
LEGACY TEST SELECTIONS: 0
```

No MAP00-MAP09_06 category and no legacy aggregate selection was executed. The zero wider-regression selection follows the user's focused-only instruction because the failure was fully localized inside the new MAP09_07 test file.

## Unity and Static Gates

```text
Unity version: 6000.3.8f1
Compile errors: 0
Console errors: 0
Relevant warnings: 0
Focused EditMode: 22 discovered / 22 executed / 22 passed / 0 failed / 0 skipped / 0 inconclusive
PlayMode: NOT REQUIRED

Runtime C#/matching meta: 4/4
EditMode test C#/matching meta: 1/1
All Assets meta/GUID: 3881/3881
Duplicate GUID groups: 0
Forbidden production symbol hits: 0
Authoring CSV/matching meta: 50/50
Authoring manifest: f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb
Physical V2 Authoring CSV/meta: 0/0
Generated CSV: 0
Runtime asmdef SHA-256: 1df0ed8fcdf1f7c668b12f29da71272f3133f64a1965fcf70237a6e5f0b34fef
EditMode asmdef SHA-256: 2d05060be8f0d602b97483b1e0bda2acadc9fb134aa0433d284626c5513d225a
Existing MAP00-MAP09_06 modifications: 0
Asmdef/Scene/Prefab/Settings/Packages changes: 0
Unapplied root inbox candidates: 0
Duplicate/unapplied/diff-check errors: 0/0/0
Unrelated staged/included paths: 0
```

The final Console was empty. Production scope contains no RNG, file read/write, Unity lifecycle, importer/exporter/writer, solver/composer/renderer/slicer, streaming, or save implementation.

## Change Scope and Out-of-Scope Findings

Every implementation/test change is a new file under the two approved Data roots. Existing runtime/test/CSV/meta files, other MAP09 contract roots, Authoring/Generated content, scenes, prefabs, settings, packages, and assembly definitions were not changed.

```text
OUT_OF_SCOPE_FINDING: NONE
MAP09_08 started: NO
Git push: NOT PERFORMED
```

## Atomic Commit Handoff

Only the installed/archived MAP09_07 Task, four Runtime C#/meta pairs, one focused test C#/meta pair, this Result, and finalized Status are eligible for the atomic commit.

```text
Subject: MAP09_07: add V2 CSV schema compatibility registry
Commit: SELF
Push: NOT PERFORMED
```
