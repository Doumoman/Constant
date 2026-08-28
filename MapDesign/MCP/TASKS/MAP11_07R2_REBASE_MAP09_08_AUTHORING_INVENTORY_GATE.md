```yaml
mcp_repair:
  format: current_task_repair_v1
  repair_id: MAP11_07R2_REBASE_MAP09_08_AUTHORING_INVENTORY_GATE
  repairs_current_task: MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS
  requires_current_task: MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS
  requires_blocked_result:
    path: REPORTS/MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS_RESULT.md
    status: BLOCKED
    sha256: 78b6d931c661c8c5ca52341d56ca1daeb5fec4f2b23463864e0660a0cdf92c1d
  requires_installed_task:
    path: TASKS/MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS.md
    sha256: 87c8a482ce73da2d4238926aa0976916b809eae28b517cec3a17fb573a9f8dfd
  preserves_current_task: MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS
  next_task_remains_locked: MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES
```

# MAP11_07R2 — Rebase MAP09_08 Authoring Inventory Gate

```text
REPAIR: MAP11_07R2_REBASE_MAP09_08_AUTHORING_INVENTORY_GATE
CURRENT TASK: MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS
STATUS EFFECT: NONE — MAP11_07 stays CURRENT
NEXT: MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES stays LOCKED
```

## 0. Repair Decision

The first MAP11_07 repair successfully changed the approved schema from `15 tables / 83 columns` to `24 tables / 143 columns`, and its MAP09_07 owner verification passed `22/22`.

The dependent MAP09_08 verification then exposed one stale inventory gate:

```text
Test:
StarNight.Map.Tests.EditMode.WorldGeneration.Pipeline.Map09ContractPhaseExitTests.LegacyAuthoringManifestAndGeneratedInventoryRemainAtApprovedBoundary

Stale expectation: Authoring CSV count 50
Approved current baseline: Authoring CSV count 52
Approved current manifest:
4415ae4af5196d6793f5d0152c0688e5bf35dc4ad23442791e45d3cfd81d0851
```

The extra two files are the already-approved MicroPattern V2 physical CSVs. This is a test-baseline bookkeeping defect, not a Runtime, importer, TerrainCluster, or content defect.

This repair authorizes only the exact stale MAP09_08 inventory expectation update. It does not authorize a broader regression run or any production behavior change.

## 1. Apply / Audit Procedure

This is not a new Master Task. Do not run the normal `NONE -> CURRENT` task-open flow.

Preflight must verify:

1. Current Task is exact `MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS` and remains `CURRENT`.
2. `MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES` remains `LOCKED`.
3. The current BLOCKED Result status/SHA matches this file's metadata.
4. The installed original MAP11_07 Task and first MAP11_07R repair SHA values match this file's metadata.
5. The current schema evidence is exact `24 tables / 143 columns / 44 total FK`, canonical digest `78a0df2056db7b12241c127ba85c573e26859503856cd8c8ea1a12648c8f4b57`.
6. Authoring CSV/meta inventory is still exact `52/52` and its manifest is exact `4415ae4af5196d6793f5d0152c0688e5bf35dc4ad23442791e45d3cfd81d0851`.
7. TerrainCluster physical CSV/meta inventory is still `0/0`; starter clusters are still `0/16`.
8. No other unapplied inbox candidate or unrelated staged path exists.

Install this repair byte-identically as:

```text
MCP/TASKS/MAP11_07R2_REBASE_MAP09_08_AUTHORING_INVENTORY_GATE.md
MCP_ARCHIVE/MAP11_07R2_REBASE_MAP09_08_AUTHORING_INVENTORY_GATE.md
```

Move/remove the inbox source after both copies match its SHA. Do not change Master or Status during repair installation. The original MAP11_07 Task plus both repair addenda form the effective specification.

Any state/SHA/collision mismatch is `BLOCKED` with zero source/content modification.

## 2. Exact Repair Write Boundary

Modify only the existing test source:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Pipeline/Map09ContractPhaseExitTests.cs
```

Within that file, modify only the exact existing test method:

```text
LegacyAuthoringManifestAndGeneratedInventoryRemainAtApprovedBoundary
```

Update its Authoring baseline to exact:

```text
Authoring CSV count: 52
Authoring CSV manifest:
4415ae4af5196d6793f5d0152c0688e5bf35dc4ad23442791e45d3cfd81d0851
Generated CSV count: 0 (unchanged)
```

If the test stores the inventory expectation in a private constant used only by this exact method, that single constant may be updated instead. Do not change a shared public authority or another test's expectation.

Forbidden:

- production/Runtime/Editor source modification
- CSV or meta creation/modification during this gate repair
- schema registry or schema test modification
- dynamic/self-derived expected count or manifest
- assertion removal, weakening, ignore, skip, retry, or warning conversion
- recursive discovery or path-boundary change
- Generated inventory expectation change
- asmdef/asmref, Scene, Prefab, Settings, Packages change
- unrelated path modify/stage/commit

If the current method cannot be corrected inside this exact boundary, return `BLOCKED` and stop.

## 3. Minimal Owner Verification

After the exact test edit, refresh/compile and run only the focused `MAP09_08` category once.

Expected discovery/execution:

```text
MAP09_08 focused: 12 discovered / 12 executed / 12 passed
compile errors: 0
relevant Console errors/warnings: 0 / 0
```

This additional run is authorized because the prior run exposed the stale MAP09_08-owned expectation. It is the minimum owner verification for the changed test.

Do not select:

```text
MAP09_01~07 categories
MAP10 categories
MAP11_01~06 categories
legacy 19347
PlayMode
```

Do not rerun MAP09_07: its `22/22` schema-owner result already passed and the schema files must not change in this repair.

If MAP09_08 fails for any reason other than the exact authorized count/manifest expectation, do not widen scope. Report the failing test and return `BLOCKED`.

## 4. Resume the Existing MAP11_07 Task

Only after the single MAP09_08 focused verification passes, resume the effective MAP11_07 specification at MAP11_07R Section 6.

The binding content targets remain:

```text
TerrainCluster physical CSV/meta: 13/13
Authoring CSV/meta after content: 65/65
Starter TerrainClusters: 16/16
Biome matrix: 4 biomes x 4 pacing roles
SpineVariants: at least 2 per cluster, exact one baseline
Quiet candidates: exact 4, one per biome
Structural signature duplicates: 0
Generated CSV: 0
```

Implement only the already-approved MAP11_07 importer/catalog/validation/test/CSV boundary. Then run only the focused `MAP11_07` category. Public MAP11_01~06 APIs may be called by MAP11_07 tests, but their categories must not be separately selected.

Do not repeat MAP09_08 after content authoring unless this exact test source is changed again. A task-owned importer/content defect is repaired only in task-owned files and verified only by MAP11_07.

All original MAP11_07 rules and MAP11_07R schema/content rules remain binding unless this repair explicitly supersedes them.

## 5. Atomic Failure / Success Rules

If the minimal MAP09_08 verification fails:

- do not create TerrainCluster CSV/meta or MAP11_07 importer/catalog files
- keep MAP11_07 `CURRENT`
- keep MAP11_08 `LOCKED`
- no Status Finalize, commit, or push

If MAP09_08 passes but MAP11_07 focused verification fails:

- repair only MAP11_07-owned importer/catalog/test/CSV files
- rerun only MAP11_07
- do not rerun prior categories
- keep MAP11_08 locked until MAP11_07 is PASS and reviewed

PASS finalization and atomic commit follow the original MAP11_07 plus MAP11_07R scope, with this repair addendum and the exact MAP09_08 test change included. Do not stage or commit unrelated paths. Git push is forbidden.

## 6. Required Result Rewrite

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

The first section must remain Korean `## User-Facing Implementation Report`, followed by `## Responsibility and Added Functions`.

It must report:

1. every added/modified script and its individual responsibility
2. all 13 CSV files and the content responsibility of each table
3. the exact 16-cluster biome/pacing matrix
4. newly enabled functionality and pipeline position
5. what remains unimplemented
6. when the result becomes visible in Editor/game
7. the one stale MAP09_08 gate change and why it was necessary
8. exact MAP09_08 and MAP11_07 focused counts
9. explicit zero selection counts for all prohibited prior/legacy/PlayMode categories

Mandatory lineage/evidence:

```text
original MAP11_07 Task SHA
MAP11_07R schema repair SHA
MAP11_07R2 inventory repair SHA
latest prior BLOCKED Result SHA
exact modified MAP09_08 test method and before/after expectation
pre/post Authoring counts/manifests
schema/CSV/import/catalog/digest/structural/Quiet evidence required by MAP11_07R
unrelated staged paths: 0
Git push: NOT PERFORMED
```

PASS여도 MAP11_08은 자동 시작하지 않고 STOP한다.
