---
mcp_patch:
  format: single_task_v1
  task_id: SV5_05_ROUTE_STATE
  task_file: TASKS/SV5_05_ROUTE_STATE.md
  requires_current_task: NONE
  requires_completed_task: SV5_04_CORE_RESERVE
  requires_result:
    path: REPORTS/SV5_04_CORE_RESERVE_RESULT.md
    status: PASS
    sha256: e7f75abeae77693f2190a6cc58dee50fd27cf2ea3bbf589f37140ebc242b073a
  requires_installed_task:
    path: TASKS/SV5_04_CORE_RESERVE.md
    sha256: fd1f879e9f3d1ab302a1b2f7eb0b610227d6a7d9d86e7d43d2724978cb05f9a2
  sets_current_task: SV5_05_ROUTE_STATE
---

# SV5_05_ROUTE_STATE — source-bound progression and shortcut analysis

```text
TASK: SV5_05_ROUTE_STATE
INPUT HANDOFF: MapDesign/MCP_INBOX/SV5_05_START.md
SOURCE SPEC: MapDesign/MCP/INPUTS/SV5_05/SV5_05_ROUTE_STATE.md
EXPECTED RESULT: MapDesign/MCP/REPORTS/SV5_05_ROUTE_STATE_RESULT.md
NEXT: SV5_06_SPACE_GRAPH remains LOCKED / DO NOT START
```

status_control:
  task_key: SV5_05_ROUTE_STATE
  result_file: REPORTS/SV5_05_ROUTE_STATE_RESULT.md

## Verified inputs

- SV5_04 actual Result and installed/Archive Task match `e7f75abe...` and
  `fd1f879e...`; the real Finalize/task-owned commit is
  `dbe770f8b385b7596a2d4f57627380395072515e`, parent
  `7fcd3447d8b233039ff77924447be21d4875ee03`.
- `VERIFY.py --manifest-sha cc2502e7d3fa623de5b290741f26623d0ab96d4d8bc6283ae3bb221327542b0d`
  and its actual-SV5_04-Result local precheck passed before Apply.
- The SOURCE_LOCK's 39 current bytes passed, including SV5_03/04 evidence,
  RMAP13 `RmapWorldGraphPlanner` `93a778...`, completion search `ce93c6...`,
  RMAP15/RMAP16 sources, and the SV5_04 adapter/test.
- Actual pre-Apply state is 285 = 242 COMPLETE / 0 CURRENT / 43 LOCKED;
  `SV5_04_CORE_RESERVE` is COMPLETE, `SV5_05_ROUTE_STATE` is LOCKED, and
  Current Task is NONE.

## Exact read and write scope

Read the bound SV5_03 tables and SV5_04 exports, the full review notes and
findings (including the three labeled contacts and 109-edge AIR witness),
the current RMAP13 graph/state/action APIs, completion-search boundary, and
the current special/route source objects. Route labels are descriptive and
must be bound to actual RMAP13 edges/predicates, not parsed as predicates.

Write only:

- `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/RmapWorldGraphPlanner.cs`:
  a backwards-compatible, pure analysis-node entry point if required for
  declared general shortcut anchors. Existing Plan/Evaluate semantics and
  default outputs must remain equivalent.
- New `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5RouteStatePolicy.cs`
  and its `.meta`: deterministic SV5_05 adapter over the exact SV5_04 core
  plan and RMAP13 graph. It evaluates six resource orders, actions, returns,
  shortcut candidate sets, condition-preserving contacts, and readiness.
- New `Assets/_Game/Tests/EditMode/Map/SV5/Sv5RouteStatePolicyTests.cs` and
  its `.meta`, under the existing Map EditMode assembly. The focused tests
  cover C01–C09 / T01–T10 and write only SV5_05 evidence from their tested
  result.
- `MCP/SV5/09_ROUTE_STATE_V5.md`, `MCP/GENERATED/SV5_05/` outputs, this
  Task's Archive/Result, and normal SV5_05 state transitions. `_work/` is
  permitted only while task-owned and must be removed before Result.

Do not modify SV5_04 sources/tests/outputs, existing RMAP13 meaning, prior
SV5 evidence, active bindings/rules/protocol/file flow, scenes, Player code,
settings, Build settings, or unrelated dirty files. Do not start SV5_06+,
RMAP18/19, full regression, PlayMode, build, Scene Bake, or push.

## Required implementation and evidence

1. Bind each of the 11 actual route IDs to its RMAP13 edge/node, endpoint
   access port/flow, concrete resource/Forge/Seal/Boss predicate, and physical
   readiness. Unknown IDs, mixed plans, unknown anchors, and unknown
   conditions fail closed with source IDs and diagnostics.
2. Use the existing RMAP13 state/action semantics to evaluate all six exact
   resource orders and their normal returns. Record acquire actions, cursor,
   Forge→Seal→Boss→Exit, and failure frontier without implicit state mutation.
3. Analyze immutable shortcut candidates and candidate sets before any world
   write. Declared general anchors may participate only through the pure
   RMAP13 analysis surface; candidates must not bypass required action,
   predicate, direction, or normal-return policy.
4. Collect shared-cell and cardinal passage/clearance contacts. Reproduce the
   three review coordinates and 109-edge AIR witness as diagnostic inputs;
   classify from actual predicates and retain unresolved physical obligations.
   Never report geometry or Player success from logical analysis.
5. Export the ten specified SV5_05 CSV/JSON/XML files from the same tested
   result, separating LOGICAL_STATE_VERIFIED from GEOMETRY_STATE_READY and
   PLAYER_VERIFIED. Assign Village/Start/physical-contact obligations to
   SV5_06, SV5_09, or SV5_41 as appropriate.
6. Run focused EditMode only for the new SV5_05 tests and the direct RMAP13
   regression class needed for the analysis extension. Write native XML to
   `MCP/GENERATED/SV5_05/focused_results.xml`, then static-check identities,
   deterministic output, scope, and temporary cleanup.

## Result, Finalize, and stop

Write a `STATUS: PASS` Result while CURRENT only after focused tests pass.
Finalize SV5_05 only, commit only listed task-owned source/test/MCP files,
then create the non-committed `MCP/GENERATED/SV5_05/SV5_05_REVIEW.zip` from
committed review inputs. Do not amend the immutable Result for that ZIP.
