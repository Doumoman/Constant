---
mcp_patch:
  format: single_task_v1
  task_id: SV5_04_CORE_RESERVE
  task_file: TASKS/SV5_04_CORE_RESERVE.md
  requires_current_task: NONE
  requires_completed_task: SV5_03_BINDINGS
  requires_result:
    path: REPORTS/SV5_03_BINDINGS_RESULT.md
    status: PASS
    sha256: 0b6f9c0a8ba658de8e911781f3b5a3e614c27333ff25faf3c63720e7486ae422
  requires_installed_task:
    path: TASKS/SV5_03_BINDINGS.md
    sha256: b8f6f5865dae26d512a50a3f62061c1370daec8bc12e9c56578ebda3565677b3
  sets_current_task: SV5_04_CORE_RESERVE
---

# SV5_04_CORE_RESERVE — source-bound core and route reservation

```text
TASK: SV5_04_CORE_RESERVE
INPUT HANDOFF: MapDesign/MCP_INBOX/SV5_04_START.md
SOURCE SPEC: MapDesign/MCP/INPUTS/SV5_04/SV5_04_CORE_RESERVE.md
EXPECTED RESULT: MapDesign/MCP/REPORTS/SV5_04_CORE_RESERVE_RESULT.md
NEXT: SV5_05_ROUTE_STATE remains LOCKED / DO NOT START
```

status_control:
  task_key: SV5_04_CORE_RESERVE
  result_file: REPORTS/SV5_04_CORE_RESERVE_RESULT.md

## Verified inputs

- SV5_03 actual Result, installed Task, and Archive match `0b6f9e...` and
  `b8f6f5...`; its Finalize commit is `7fcd3447d8b233039ff77924447be21d4875ee03`
  with parent `ffd81805cf3840b56c74944a284d0a4d3f512297`.
- `VERIFY.py --manifest-sha 48a7164e431cbaf4d6edc88007a8c1532e02654447a35a88e357e4a8160c5dce`
  and the actual-Result local precheck passed before Apply.
- Current source bytes match SV5_03 bindings BND-01 through BND-06 and BND-10:
  world data `345458...`, graph `93a778...`, biome `36f531...`, special plan
  `e524df...`, site bridge `14f80c...`, slots `6b1fe2...`, cluster plan
  `48bc1b...`.

## Exact read and write scope

Read the bound SV5_03 CSVs and current RMAP12–16 APIs/exports, in particular
`RmapSpecialReservationPlan.EvaluateTerrainCells`, RMAP15 site/cell/access/
slot/state exports, and `Rmap16ClusterAssemblyPlan.Routes`/terrain cells.

Write only:

- `Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/Sv5CoreReservationPlan.cs`
  and its `.meta`: a deterministic adapter over the immutable RMAP15 plan and
  the current RMAP16 routes. It must expose protected-core/route candidate
  rejection with coordinate, owner, and reason; it must not regenerate sites,
  consume RNG, alter graph/state, or mutate RMAP15/16 sources.
- `Assets/_Game/Tests/EditMode/Map/SV5/Sv5CoreReservationPlanTests.cs`, its
  `.meta`, and the `SV5` folder `.meta`, under the existing Map EditMode
  assembly. The focused tests must create the same source plan, test C01–C08,
  and export the specified SV5_04 evidence from that tested plan.
- `MCP/SV5/08_CORE_RESERVE_V5.md`, `MCP/GENERATED/SV5_04/` outputs, this
  Task's Archive/Result, and normal SV5_04 state transitions. `_work/` is
  permitted only while tracked and must be removed before Result.

Do not edit RMAP12–17 sources/exports, prior SV5 evidence, active bindings,
rules, protocol, file flow, scenes, prefabs, Player code/settings, or other
dirty files. Do not run SV5_05+, RMAP18/19, PlayMode, full regression, full
Scene Bake, build, or push.

## Required implementation and evidence

1. Reuse the source special plan's eight sites, 2,432 fixed/protected cells,
   slots, graph bindings, access ports, and six SealBoss state cells by
   reference. Preserve Village as physical-only and Seal/Boss as one site.
2. Reuse each actual RMAP16 graph route. Record its exact endpoint ports,
   condition, cardinal path, body/headroom clearance, and only the existing
   support cells. Physical ports that no current graph edge selects must be
   recorded as preserved/unrouted, never claimed as completed routes.
3. Make the public consumer check reject protected S/A/O cells, slot support
   or headroom, state geometry, out-of-world, incompatible duplicates, route
   clearance blockage, and route support removal; allow normal unowned and
   compatible duplicate candidates with deterministic diagnostics/digest.
4. Export `core_sites.csv`, `core_cells.csv`, `access_bindings.csv`,
   `route_cells.csv`, `state_geometry.csv`, and `reservation_manifest.json`
   from the same tested object. Add binding/validation JSON after actual test
   output and preserve UTF-8 RFC4180 ordering.
5. Run only `StarNight.Map.Tests.EditMode.Sv5.Sv5CoreReservationPlanTests`
   in EditMode through Unity CLI, writing its native XML to
   `MCP/GENERATED/SV5_04/focused_results.xml`. Record actual counts/command.
6. Static-check source/output identities, 8/2432 preservation, route/access
   references, C01–C08, MD length, temporary cleanup, and scope before Result.

## Result, Finalize, and stop

Write a `STATUS: PASS` Result while CURRENT only after focused tests pass.
Finalize SV5_04 only, commit only the listed task-owned source/test/MCP files,
then create the non-committed `MCP/GENERATED/SV5_04/SV5_04_REVIEW.zip` from
the committed review inputs. Do not amend the immutable Result for that ZIP.
