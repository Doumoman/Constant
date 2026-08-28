```yaml
mcp_repair:
  format: current_task_repair_v1
  repair_id: MAP11_08R_CORRECT_TERRAIN_CLUSTER_COLUMN_TOTAL
  repairs_current_task: MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES
  requires_current_task: MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES
  requires_blocked_result:
    path: REPORTS/MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES_RESULT.md
    status: BLOCKED
    sha256: 5d9e87df632167dfc08128e9ef9c6a5edd9743b923a3168cd1614e088bc1efd6
  requires_installed_task:
    path: TASKS/MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES.md
    sha256: fe790c7380326e7b3b9a02d1332b7ad3ab3233af045485d0e552f44b22990e30
  preserves_current_task: MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES
  next_task_remains_locked: MAP11_09_MAP11_CLUSTER_EXIT_TESTS
```

# MAP11_08R — Correct TerrainCluster Column Total

```text
REPAIR: MAP11_08R_CORRECT_TERRAIN_CLUSTER_COLUMN_TOTAL
CURRENT TASK: MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES
STATUS EFFECT: NONE — MAP11_08 stays CURRENT
NEXT: MAP11_09_MAP11_CLUSTER_EXIT_TESTS stays LOCKED
```

## 0. Repair Decision

MAP11_08 stopped before creating assets because its preflight repeated an incorrect TerrainCluster subtotal from prior reporting:

```text
incorrect reporting subtotal: 13 tables / 91 columns
authoritative actual subtotal: 13 tables / 89 columns
```

The 13 exact registry/physical headers prove:

```text
5 + 8 + 6 + 4 + 5 + 5 + 6 + 6 + 10 + 5 + 4 + 22 + 3 = 89
```

The non-TerrainCluster V2 tables own 54 columns, therefore:

```text
TerrainCluster 89 + non-TerrainCluster 54 = full registry 143
```

The current authoritative registry, all 13 physical headers, full `24 tables / 143 columns / 44 FK`, importer/catalog, and focused tests agree. No schema column is missing.

This repair corrects only the MAP11_08 specification/preflight arithmetic. It authorizes no Runtime, registry, CSV, importer, or existing test change.

## 1. Apply / Audit Procedure

This is not a new Master Task. Do not run the normal `NONE -> CURRENT` task-open flow.

Preflight must verify:

1. Current Task is exact `MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES` and remains `CURRENT`.
2. `MAP11_09_MAP11_CLUSTER_EXIT_TESTS` remains `LOCKED`.
3. The current BLOCKED Result status/SHA matches this file's metadata.
4. Installed original MAP11_08 Task SHA matches this file's metadata.
5. The BLOCKED run created no Editor/EditMode/PlayMode C# or meta files.
6. Existing source/CSV/meta/Scene/Prefab/asmdef modifications from MAP11_08 are `0`.
7. Authoring CSV/meta remains `65/65`; TerrainCluster CSV/meta `13/13`; Generated CSV `0`.
8. Registry/physical exact totals are `24/143/44` overall and `13/89` for TerrainCluster.
9. Importer/catalog remains `16/16` with digest:

```text
cc9c88df963b2ac6ce462f76767b6de6252c09de05a5f38f8eb2c327a3c91582
```

10. No other unapplied inbox candidate or unrelated staged path exists.

Install this repair byte-identically as:

```text
MCP/TASKS/MAP11_08R_CORRECT_TERRAIN_CLUSTER_COLUMN_TOTAL.md
MCP_ARCHIVE/MAP11_08R_CORRECT_TERRAIN_CLUSTER_COLUMN_TOTAL.md
```

Move/remove the inbox source after both copies match its SHA. Do not modify Master or Status during repair installation. The original MAP11_08 Task plus this addendum form the effective specification.

Any state/SHA/collision mismatch is `BLOCKED` with zero project modification.

## 2. Exact Supersession

Every MAP11_08 original Task occurrence of:

```text
TerrainCluster 13 tables / 91 columns
```

is superseded by:

```text
TerrainCluster 13 tables / 89 columns
```

Required preflight authority becomes:

```text
full V2 schema: 24 tables / 143 columns / 44 FK
TerrainCluster slice: 13 tables / 89 columns
non-TerrainCluster slice: 11 tables / 54 columns
```

Do not change the installed original Task file to rewrite the typo. This additive repair is the audit record.

The MAP11_07 PASS Result's `13 tables / 91 columns` phrase is also a reporting arithmetic error. Do not rewrite or replace the completed historical Result. Current registry descriptors, the 13 physical headers, their hashes, full `143` total, and this repair are authoritative.

## 3. Forbidden Repair Actions

During the arithmetic repair itself, do not modify or create any project asset.

Forbidden:

- `V2AuthoringSchemaRegistry.cs` or schema tests
- TerrainCluster CSV/meta or importer/catalog
- MAP09/MAP10/MAP11_01~07 production/test files
- adding two placeholder/unused columns
- changing full 24/143/44 totals or digests
- running schema, prior, legacy, or PlayMode tests merely for this arithmetic correction
- Master/Status change during repair install
- unrelated modify/stage/commit or Git push

If current authority is not exact 13/89 and 24/143/44, return `BLOCKED`; do not self-correct.

## 4. Resume MAP11_08

After the corrected read-only preflight passes, resume the original MAP11_08 specification at Section 4.

All original responsibility and file boundaries remain unchanged:

```text
new Editor preview model/window
new MAP11_08 EditMode focused tests
new MAP11_08 PlayMode graybox focused tests
optional one test-only PlayMode lifecycle helper only when justified
```

Normal verification remains exactly:

```text
MAP11_08 EditMode focused only
MAP11_08 PlayMode focused only
```

Do not run MAP09/MAP10/MAP11_01~07 categories, legacy 19347, MAP11_09, or an unfiltered PlayMode suite.

Task-owned failures are repaired only in new MAP11_08 files and rerun only in the corresponding MAP11_08 mode. If an existing authority change is required, return `BLOCKED` without widening scope.

## 5. Required Result Rewrite

Rewrite the same Result path:

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

The first section remains Korean `## User-Facing Implementation Report`, followed by `## Responsibility and Added Functions`.

In addition to the original Task evidence, report:

- original MAP11_08 Task SHA
- MAP11_08R repair SHA
- prior BLOCKED Result SHA
- corrected `13/89` arithmetic and `24/143/44` preservation
- schema/CSV/importer existing-file modifications `0`
- every new preview/EditMode/PlayMode script and its responsibility
- newly visible Editor functionality, pipeline position and remaining non-production scope
- EditMode and PlayMode MAP11_08 focused counts separately
- prohibited category/unfiltered/legacy selections `0`
- unrelated staged/included paths `0`

PASS일 때만 MAP11_08을 Finalize and atomic commit한다. Git push는 하지 않는다.

PASS여도 MAP11_09는 자동 시작하지 않고 STOP한다.
