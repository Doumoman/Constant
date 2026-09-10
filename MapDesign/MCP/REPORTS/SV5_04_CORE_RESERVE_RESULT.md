# SV5_04_CORE_RESERVE Result

TASK: SV5_04_CORE_RESERVE
STATUS: PASS

## Outcome

SV5_04 adds a deterministic, non-mutating core-reservation consumer adapter.
It reads the same RMAP15 special-plan object and RMAP16 route-plan object,
preserves the core source, and returns diagnostics before a future terrain
writer can alter a protected cell. It does not create a new world definition,
RNG stream, site placement, progression state, Player result, or Bake.

## Input, predecessor, and normal Apply

| Role | Path or identity | SHA-256 / result |
| --- | --- | --- |
| handoff INBOX | `MCP_INBOX/SV5_04_START.md` | `349a6955ad8c03434c5a99d905600792632de162d71c17a9518e6cda610f9664` |
| source specification | `MCP/INPUTS/SV5_04/SV5_04_CORE_RESERVE.md` | `658aa2ccf67d1f4c3a4507cd4b1521d0fc56dc414fd946636410f682e4b54d3c` |
| package manifest | `MCP/INPUTS/SV5_04/FILES.json` | `48a7164e431cbaf4d6edc88007a8c1532e02654447a35a88e357e4a8160c5dce` |
| package verification | `VERIFY.py --manifest-sha` | `PASS_PACKAGE_ONLY` |
| local predecessor verification | actual SV5_03 Result via `--local-precheck` | `PASS_LOCAL_BYTES_ONLY` |
| SV5_03 Result | `MCP/REPORTS/SV5_03_BINDINGS_RESULT.md` | `0b6f9c0a8ba658de8e911781f3b5a3e614c27333ff25faf3c63720e7486ae422` |
| SV5_03 installed/Archive Task | `MCP/TASKS` and `MCP_ARCHIVE` | `b8f6f5865dae26d512a50a3f62061c1370daec8bc12e9c56578ebda3565677b3` |
| SV5_03 Finalize / task-owned commit | `7fcd3447d8b233039ff77924447be21d4875ee03` | parent `ffd81805cf3840b56c74944a284d0a4d3f512297` |
| bound / installed / Archive SV5_04 Task | `SV5_04_CORE_RESERVE.md` | `fd1f879e9f3d1ab302a1b2f7eb0b610227d6a7d9d86e7d43d2724978cb05f9a2` |

The local precheck used the actual committed `MCP/REPORTS` predecessor, not
the input reference. Its SV5_03 BINDINGS, CORE_BINDINGS, DATA_SCHEMAS, and
TASK_COVERAGE CSV bytes were opened and checked before the one normal Apply.

## Actual state

| Point | Total | COMPLETE | CURRENT | LOCKED | Current Task | Status SHA-256 |
| --- | ---: | ---: | ---: | ---: | --- | --- |
| before Apply | 285 | 241 | 0 | 44 | `NONE` | `b3779eb69b6227532f105de55a41cba0632db5eb934063c94cad8d34a4b6ff45` |
| after Apply / this Result | 285 | 241 | 1 | 43 | `SV5_04_CORE_RESERVE` | `43f786d561ed172205d466f07230895b92787a307d8e00a0f2b8bdcb9d648e0f` |

SV5_03 remains COMPLETE. SV5_05 and all later SV5 tasks remain LOCKED.

## Implemented binding and focused evidence

- New adapter: `Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/
  Sv5CoreReservationPlan.cs`
  (`128d98c42e092d917a58460ba980bfe09eda33cad41ccd56bff1e7b3fffe0cf3`).
  It reuses `RmapSpecialReservationPlan.EvaluateTerrainCells` and adds checks
  for conditional state cells and actual RMAP16 route body, clearance, and
  support. It neither mutates nor reimplements RMAP15/RMAP16.
- New focused test:
  `Assets/_Game/Tests/EditMode/Map/SV5/Sv5CoreReservationPlanTests.cs`
  (`0ad252215729eb7c2a325395ef81ae730f72f3ab5af244476313c921b4d0b4d8`)
  in the existing `Game.Map.Tests.EditMode` assembly.
- Final focused command was `unity test . --editor-version 6000.3.8f1 --mode
  EditMode --filter StarNight.Map.Tests.EditMode.Sv5.Sv5CoreReservationPlanTests
  --output MapDesign/MCP/GENERATED/SV5_04/focused_results.xml --report-format
  nunit --timeout 600`; it passed 8/8, failed/skipped 0/0. XML SHA is
  `42a550abf7c4bf618a22ddd52fbbf42eae5a83a1fbbc9cd077bd47d5dfc716b4`.
- Two initial invocations stopped before a test verdict for new-file compile
  scope corrections. A subsequent 7/8 run exposed a stale 12-route test
  expectation; the actual current RMAP16 graph has 11 routes. The expectation
  was corrected to the actual source and the final 8/8 run is the recorded
  evidence.

## Preserved source and generated plan

The same tested plan exports 8 sites, all 2,432 RMAP15 source cells exactly
(1,919 A, 483 S, 30 O), 10 slots, 45 physical port cells, and 6 SealBoss
state rows (3 OPEN/3 SEALED). The source-cell tuple set equals
`RMAP15/rmap15_fixed_cells.csv`; no source export was changed.

The current RMAP16 graph contributes 11 continuous, static-evidence routes:
3,019 passage rows, 3,019 clearance rows, and 1,037 existing support rows.
Twelve physical access groups are graph-route endpoints. Start's unselected
EXIT port and Village's two no-graph-reservation ports are preserved and
explicitly marked `PRESERVED_*`, rather than claimed as completed routes.

## Outputs

| Output | SHA-256 |
| --- | --- |
| `MCP/SV5/08_CORE_RESERVE_V5.md` | `795bb8af332ce0d2de3f22bd28a1db2c77ef56aa3a6ff24b4a7e89be37a6c45d` |
| `MCP/GENERATED/SV5_04/core_sites.csv` | `78dd267b778dbdbf04b2d53cb912bc334984f3ea4b3495c319c21270cedba9bd` |
| `MCP/GENERATED/SV5_04/core_cells.csv` | `10a6bc42eea1aebba755cc94d10d0d999688ecc850cc4901417c83a9ec8ad389` |
| `MCP/GENERATED/SV5_04/access_bindings.csv` | `9600d72d0df139937462aefab5bf0fde86ab8117eaa32831df47777f6fc10379` |
| `MCP/GENERATED/SV5_04/route_cells.csv` | `63e3d445377a0fdea658ed39b0a8765b5b66b14083a560b4c54b434b743cba13` |
| `MCP/GENERATED/SV5_04/state_geometry.csv` | `ac93edca65636b73a71c47af3c58e514cb6759d1d549f00575d08846245d8785` |
| `MCP/GENERATED/SV5_04/reservation_manifest.json` | `e567722745d1f15feef314b3efa047a7e53d44cc7704893ff14a1875ebdc00b5` |
| `MCP/GENERATED/SV5_04/BINDING.json` | `bc7d94a6c1fdb05a0e9067d4bc8488c6294e75b2293cafa979c19f4f05b5568f` |
| `MCP/GENERATED/SV5_04/validation.json` | `7dacd96e607b7dd362c03aec5d53f7f2ff5e8aa273f2d81277aa1caebe31fa77` |

The execution-time static contract passed 17/17, including source-cell and
route semantic reconstruction, source/output IDs, focused XML counts, Task
identity, state counts, MD length, write scope, and no `_work` directory.

## Exclusions and Finalize timing

No SV5_05+, full regression, PlayMode, Player validation, full Scene Bake,
RMAP18/19, build, or push was run. Pre-existing unrelated dirty changes were
retained and not staged.

This immutable Result was written while SV5_04 was CURRENT. Finalize and the
task-owned local commit have not occurred at Result-writing time. Finalize
only SV5_04, commit only its owned files, create the post-Finalize review ZIP
without amending this Result, and stop.
