```yaml
mcp_repair:
  format: current_task_repair_v1
  repair_id: MAP11_07R3_MAKE_MAP09_08_INVENTORY_GATE_MONOTONIC
  repairs_current_task: MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS
  requires_current_task: MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS
  requires_blocked_result:
    path: REPORTS/MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS_RESULT.md
    status: BLOCKED
    sha256: fae1518b5e598745e09199db7fb3fe370364d1109d238223feaaee113f5693db
  requires_installed_task:
    path: TASKS/MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS.md
    sha256: 87c8a482ce73da2d4238926aa0976916b809eae28b517cec3a17fb573a9f8dfd
  preserves_current_task: MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS
  next_task_remains_locked: MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES
```

# MAP11_07R3 — Make MAP09_08 Inventory Gate Monotonic

```text
REPAIR: MAP11_07R3_MAKE_MAP09_08_INVENTORY_GATE_MONOTONIC
CURRENT TASK: MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS
STATUS EFFECT: NONE — MAP11_07 stays CURRENT
NEXT: MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES stays LOCKED
```

## 0. Repair Decision

R2 corrected the stale total Authoring count/manifest from `50` to the then-current `52`, but the same MAP09_08 test still required every registered V2 Authoring CSV to be physically absent.

That absence was true only at the MAP09 exit moment. It became false when MAP10 legitimately authored the two approved MicroPattern CSVs, and it would become false again whenever later registered V2 content is authored. Replacing `52` with each future total would create a permanently recurring maintenance failure.

The durable invariant is instead:

1. the approved legacy Authoring subset remains exact and unchanged;
2. every later physical V2 CSV is an exact registered schema path with its registered header and matching meta;
3. unregistered/unknown Authoring CSVs remain forbidden;
4. Generated CSV remains absent at this phase boundary.

This repair changes only the failing test's inventory semantics from a transient absence assertion to that monotonic contract. It changes no production behavior, schema descriptor, CSV, importer, or content.

## 1. Apply / Audit Procedure

This is not a new Master Task. Do not run the normal `NONE -> CURRENT` task-open flow.

Preflight must verify:

1. Current Task is exact `MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS` and remains `CURRENT`.
2. `MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES` remains `LOCKED`.
3. The current BLOCKED Result status/SHA matches this file's metadata.
4. Original MAP11_07 Task SHA matches this file's metadata.
5. Installed/archive repairs are byte-identical to:

```text
MAP11_07R_EXTEND_TERRAIN_CLUSTER_AUTHORING_SCHEMA.md
2eb3dde8186598000b366f8aa6ae807aed6aa77f9f0c7d89b32c42b8d987c9c8

MAP11_07R2_REBASE_MAP09_08_AUTHORING_INVENTORY_GATE.md
a151d29d14b90e1024bc97286f7f366d4b856f0a212ca62ef68891dc140253e7
```

6. Current schema remains exact `24 tables / 143 columns / 44 total FK`, canonical digest `78a0df2056db7b12241c127ba85c573e26859503856cd8c8ea1a12648c8f4b57`.
7. Physical Authoring CSV/meta remains `52/52`; TerrainCluster CSV/meta remains `0/0`; starter clusters remain `0/16`; Generated CSV remains `0`.
8. The latest MAP09_08 run is exact `12 executed / 11 passed / 1 failed`, and the only failure is the final registered-V2 physical-absence assertion in the named method below.
9. No other unapplied inbox candidate or unrelated staged path exists.

Install this repair byte-identically as:

```text
MCP/TASKS/MAP11_07R3_MAKE_MAP09_08_INVENTORY_GATE_MONOTONIC.md
MCP_ARCHIVE/MAP11_07R3_MAKE_MAP09_08_INVENTORY_GATE_MONOTONIC.md
```

Move/remove the inbox source after both copies match its SHA. Do not change Master or Status during repair installation. The original MAP11_07 Task plus R1, R2, and R3 form the effective specification.

Any state/SHA/collision mismatch is `BLOCKED` with zero source/content modification.

## 2. Exact Repair Write Boundary

Modify only:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Pipeline/Map09ContractPhaseExitTests.cs
```

Within that file, modify only:

```text
StarNight.Map.Tests.EditMode.WorldGeneration.Pipeline.Map09ContractPhaseExitTests
  .LegacyAuthoringManifestAndGeneratedInventoryRemainAtApprovedBoundary
```

No helper outside this test source may be modified. A private local/helper method used only by this exact test may be added inside the same test class when necessary for normalized relative paths, byte manifests, or exact header reads.

## 3. Required Monotonic Inventory Contract

Supersede R2's total-inventory `52` and total manifest assertion. Do not replace them with `65` or any other future total.

Inside the exact test, build the registered V2 Authoring path set from the current `V2AuthoringSchemaRegistry` descriptors. Normalize paths using the same canonical separator/case rules already used by the existing test authority.

Partition every physical Authoring CSV by exact normalized relative path:

```text
registered physical V2:
  physical path exactly exists in the registry descriptor path set

legacy physical:
  every other physical Authoring CSV path
```

Then assert all of the following.

### 3.1 Legacy subset remains frozen

```text
legacy CSV count: exact 50
legacy CSV.meta count: exact 50
legacy manifest: exact existing Map09ApprovedBaseline.AuthoringManifest
```

The legacy manifest must be computed over only the partitioned legacy files using the existing canonical manifest algorithm. Do not change `Map09ApprovedBaseline`, its constants, or legacy files.

An unregistered new CSV necessarily enters the legacy partition and must fail the count/manifest gate. Do not classify a file as V2 using its folder name, filename suffix, table prefix, or guessed content.

### 3.2 Physical V2 subset is schema-owned

For every physical CSV whose exact relative path is registered:

- exact one matching `.meta` exists;
- UTF-8 BOM is present;
- registered ordered header is exact;
- LF/final-LF requirements use the existing V2 authoring convention;
- the path is not under Generated;
- no duplicate normalized path exists.

The physical registered subset may grow monotonically as later Tasks author registered content. Do not assert that it is empty, exact 2, exact 15, or equal to the complete registry set.

At the present pre-content checkpoint, report exact evidence that the subset contains only:

```text
MicroPattern/micro_pattern_definitions_v2.csv
MicroPattern/micro_pattern_cells_v2.csv
```

This two-file observation is Result evidence, not a permanent hard-coded allowed list in the test.

### 3.3 Generated boundary remains unchanged

```text
Generated CSV count: exact 0
```

Keep the existing Generated path boundary and its assertion unchanged.

### 3.4 Remove only the transient rule

Remove/replace only the final assertion requiring all registered V2 Authoring CSV paths to be physically absent. Do not weaken any legacy, schema membership, header, meta, unknown-file, or Generated protection.

## 4. Forbidden Changes

- Runtime/Editor production source modification during this gate repair
- CSV/meta creation or modification before the repaired test passes
- schema registry/schema digest/schema test modification
- total Authoring count rebasing to 52, 65, or a dynamically accepted value
- allowlisting only the current two MicroPattern filenames in production/test policy
- deriving V2 ownership from folder/name convention instead of exact registry paths
- assertion removal without the replacement invariants in Section 3
- ignore/skip/retry/warning conversion
- another MAP09_08 category run
- MAP09_01~07, MAP10, MAP11_01~06, legacy 19347, or PlayMode selection
- asmdef/asmref, Scene, Prefab, Settings, Packages change
- unrelated path modify/stage/commit or Git push

If the monotonic contract cannot be implemented inside the exact test source, return `BLOCKED` and stop.

## 5. Minimum Verification for the Actual Failure

Refresh/compile, then select and execute only the exact fully-qualified failing EditMode test once:

```text
StarNight.Map.Tests.EditMode.WorldGeneration.Pipeline.Map09ContractPhaseExitTests.LegacyAuthoringManifestAndGeneratedInventoryRemainAtApprovedBoundary
```

Required:

```text
discovered/executed/passed: 1/1/1
compile errors: 0
relevant Console errors/warnings: 0/0
```

Do not run the full MAP09_08 category again. Its other 11 tests already passed in the immediately preceding R2 run, and no file owned by them may change.

If this one test fails, report the exact assertion and stop without widening scope.

## 6. Resume the Existing MAP11_07 Task

Only after the exact test passes, resume the effective MAP11_07 specification at MAP11_07R Section 6.

Binding targets remain:

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

Implement only the already-approved MAP11_07 importer/catalog/validation/test/CSV boundary. Then run only focused `MAP11_07` tests. Existing MAP11_01~06 public APIs may be invoked inside MAP11_07 tests, but their categories must not be separately selected.

Do not rerun the repaired MAP09_08 test after content authoring. MAP11_07 focused tests own the 13 new physical CSVs and their registry/header/meta/content validation.

Task-owned importer/content problems are repaired only in task-owned files and verified only by MAP11_07. All original MAP11_07, R1 schema/content, and R2 lineage rules remain binding unless R3 explicitly supersedes them.

## 7. Atomic Failure / Success Rules

If the exact MAP09_08 test fails:

- do not create TerrainCluster CSV/meta or MAP11_07 importer/catalog files;
- keep MAP11_07 `CURRENT` and MAP11_08 `LOCKED`;
- no Status Finalize, commit, or push.

If the test passes but MAP11_07 focused verification fails:

- repair only MAP11_07-owned importer/catalog/test/CSV files;
- rerun only MAP11_07;
- do not rerun prior categories;
- keep MAP11_08 locked until MAP11_07 is PASS and reviewed.

PASS finalization and atomic commit follow the original MAP11_07 plus R1/R2 scope, with R3 and the exact monotonic test change included. Do not stage or commit unrelated paths. Git push is forbidden.

## 8. Required Result Rewrite

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

Report, in concrete file-level terms:

1. every added/modified script and its individual responsibility;
2. every one of the 13 CSV files and its content responsibility;
3. exact 16-cluster biome/pacing matrix;
4. newly enabled functionality and actual pipeline position;
5. unfinished functionality and Editor/game visibility timing;
6. R3 legacy/V2 partition semantics and why transient absence was invalid;
7. exact single-test and MAP11_07 focused counts;
8. zero selections for all prohibited previous/legacy/PlayMode categories.

Mandatory lineage/evidence:

```text
original MAP11_07 Task SHA
R1 schema repair SHA
R2 count/manifest repair SHA
R3 monotonic gate repair SHA
latest prior BLOCKED Result SHA
exact modified test method and before/after invariant
legacy 50 subset count/meta/manifest
pre-content physical registered V2 subset paths
post-content Authoring 65/65 and 13 TerrainCluster CSV/meta
schema/import/catalog/digest/structural/Quiet evidence required by R1
unrelated staged paths: 0
Git push: NOT PERFORMED
```

PASS여도 MAP11_08은 자동 시작하지 않고 STOP한다.
