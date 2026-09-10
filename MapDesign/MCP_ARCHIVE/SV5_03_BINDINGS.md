---
mcp_patch:
  format: single_task_v1
  task_id: SV5_03_BINDINGS
  task_file: TASKS/SV5_03_BINDINGS.md
  requires_current_task: NONE
  requires_completed_task: SV5_02_RULES
  requires_result:
    path: REPORTS/SV5_02_RULES_RESULT.md
    status: PASS
    sha256: ed4e9ee6fd6f271bd81915040e1d6e103832a3737bd90d5eb90cc9371956a496
  requires_installed_task:
    path: TASKS/SV5_02_RULES.md
    sha256: b0bc1a19dc1ff100cdf8116ba5e1447af6080556a98011b05bbc84deb1619fc1
  sets_current_task: SV5_03_BINDINGS
---

# SV5_03_BINDINGS — RMAP/SPACE source-audit binding

```text
TASK: SV5_03_BINDINGS
INPUT HANDOFF: MapDesign/MCP_INBOX/SV5_03_START.md
SOURCE SPEC: MapDesign/MCP/INPUTS/SV5_03/SV5_03_BINDINGS.md
EXPECTED RESULT: MapDesign/MCP/REPORTS/SV5_03_BINDINGS_RESULT.md
NEXT: SV5_04_CORE_RESERVE remains LOCKED / DO NOT START
```

status_control:
  task_key: SV5_03_BINDINGS
  result_file: REPORTS/SV5_03_BINDINGS_RESULT.md

## Objective

Produce a read-only, evidence-backed binding map for SV5_04 through SV5_45.
Audit real RMAP12 through RMAP17, SPACE/SV5 records, source code, generated
exports, and existing visualisation boundaries. Record only actual REUSE,
ADAPT, and NEW decisions; do not implement a future API, map, feature, or
game-data change.

## Required reads

- `MCP_INBOX/SV5_03_START.md`, `MCP/INPUTS/SV5_03/{VERIFY.py,FILES.json,
  SOURCE_LOCK.json,AUDIT_SCOPE.json,OUTPUT_CONTRACT.md,FILE_FLOW.md,
  PROTOCOL_APPEND.md,SV5_03_BINDINGS.md}`, plus the R2 actual predecessor
  Result, installed Task, Archive, generated evidence, commit, and status.
- MCP entry/apply/change/finalize/status/Master documents, `MCP/SV5/00` through
  `05`, and the actual RMAP12 through RMAP17 Tasks, Archives, Results, and
  generated exports.
- Actual source only as needed for B01 through B13: world data/graph/biome,
  special-site/protection, pattern/port/cluster, bake/final-cell, Player
  movement/Grab/camera, validation, and VIS01/VIS02 source/scene boundaries.
- Use `rg --files` plus targeted `rg` for absence evidence. Search hints are
  not verified API names; record proposed names only in `planned_change`.

## Write allowlist

- `MCP/SV5/06_BINDINGS_V5.md`, byte-identical `MCP/SV5/07_FILE_FLOW_V5.md`,
  and exactly one `SV5_03_BINDINGS_BEGIN/END` append in `02_PROTOCOL_V5.md`.
- `MCP/GENERATED/SV5_03/{BINDINGS.csv,CORE_BINDINGS.csv,DATA_SCHEMAS.csv,
  TASK_COVERAGE.csv,SOURCE_SNAPSHOT.json,BINDING.json,validation.json}`.
- This Task's Result, installed Task, Archive, and normal SV5_03 Apply/Finalize
  state transitions only. A temporary `_work/` child is permitted only if its
  exact task-owned contents are removed before Result writing.

Do not edit inputs, prior evidence, Master, Assets, Scenes, Prefabs, meta,
game source, runtime CSV, settings, RMAP18/19, SV5_04+, or unrelated dirty
files. Do not push.

## Required work

1. Require the UTF-8 package and actual-local-predecessor checks to pass.
   Verify SV5_02 is Finalized and committed (`ffd81805...`), without amending
   its immutable Result. Record the actual state and commit in the snapshot.
2. Audit all 13 unique `AUDIT_SCOPE` groups. For each binding, capture actual
   path, current SHA, source symbol/header/key, ownership, consumer tasks, and
   source-read/data-read/absence-search evidence. Separate source inspection
   from any historical PASS and from runtime execution.
3. Extract the eight physical RMAP15 sites, their graph bindings, fixed-cell
   source, access/return, slots, and Seal/Boss state geometry from actual
   exports. Do not replace canonical IDs with example-map coordinates.
4. Map each unique SV5_04 through SV5_45 Task to audit and binding IDs with
   READ_READY, NEEDS_IMPLEMENTATION, or BLOCKED evidence. NEW needs a recorded
   search and a later-owner reason; it is not an implementation request.
5. Register the active bindings summary, byte-identical file-flow document,
   and one protocol marker. Validate CSV headers, unique cross-references,
   13-group coverage, 42-task coverage, eight-site coverage, source SHA
   records, 300-line MD limit, write scope, and explicit temporary cleanup.
6. Write a matching `STATUS: PASS` Result only after the static audit passes.
   It must say that it is written before Finalize/commit and that Unity,
   compile, tests, build, bake, and game-code changes were not run.

## Done conditions

- Exactly 13 audit groups and 42 future Task rows are covered with actual
  evidence. SV5_04 through SV5_06 have real core/state/space source bindings.
- Exactly eight RMAP15 physical sites are linked to existing protection,
  access, slot, graph, and state-geometry exports; shared Seal/Boss location
  is not collapsed into one progression state.
- REUSE/ADAPT entries name real source elements. NEW entries document an actual
  absence search and a future owner; no source path or API is invented.
- `07_FILE_FLOW_V5.md` equals the supplied input byte-for-byte; the protocol
  marker occurs once; no root helper file or task temporary survives.
- No Unity or game implementation work occurs, and SV5_04 remains LOCKED.

## Result, Finalize, and stop

After PASS, Finalize only SV5_03 and commit only Task-owned documents,
evidence, installed/archive Task, Result, and final Status. Do not amend the
Result to add the post-Finalize commit SHA. Stop with SV5_04 locked.
