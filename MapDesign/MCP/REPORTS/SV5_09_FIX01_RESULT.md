# SV5_09_FIX01 Result

TASK: SV5_09_FIX01
TASK_ID: SV5_09_FIX01
STATUS: PASS

## Outcome

SV5_09_FIX01 now derives loop topology and cost evidence from actual occupancy and room identities.
Endpoint topology uses actual `Sv5InfillRoom.Id` values while aperture ownership remains separate for
geometry. Baseline and final supported-foot graphs drive BFS costs; component count, cycle rank, and
bridge sets are calculated from exported edges. Legacy ordinary self-parent records are represented as
host-access edges, leaving zero self or duplicate topology edges.

Candidate evaluation reuses the per-plan baseline context and endpoint-pair shortest-cost cache. Each
candidate applies only a changed-cell overlay and locally reevaluates affected foot nodes. Tarjan bridge
calculation runs once for baseline and final graphs, and the 9-state x 6-resource-order product is checked
only for the final accepted batch. Production minimums remain 16 links and 12 sectors.

## Final exports

| Profile | Candidates | Accepted | Sectors | Max length | Rank before/after | Bridges before/after |
|---|---:|---:|---:|---:|---:|---:|
| default | 19,260 | 16 | 15 | 24 | 0 / 16 | 223 / 193 |
| repeat | 22,507 | 16 | 16 | 24 | 0 / 16 | 240 / 208 |

The independent checker reconstructed occupancy, supported-foot nodes, BFS costs, cycle rank, Tarjan
bridges, alternate paths, newly carved AIR, and every exported proof without importing production code.
Result: `PASS_INDEPENDENT_TOPOLOGY`.

## Final focused regression

Command:

`unity test . --mode EditMode --filter StarNight.Map.Tests.EditMode.Sv5 --output MapDesign/MCP/GENERATED/SV5_09_FIX01/focused_results.xml --timeout 1800 --format json`

- Discovered/total/passed: 125 / 125 / 125
- Failed/skipped/inconclusive: 0 / 0 / 0
- Duration: 1386.897944 seconds
- Start/end UTC: 2026-09-12T15:15:37Z / 2026-09-12T15:38:44Z
- Focused XML SHA-256: `df7ec4713b96d8d9ae1656f351b9bd3a69649c672b4428e882d2477d9da84237`
- Independent audit SHA-256: `7cacd106e1de0003ff87426a0c3bd910b73311048554ca489c15b6455a08d840`

The exact source and final export hashes were checked before and after the focused run and did not
change. `BINDING.json` ties this final execution together as follows:

- Final run ID: `SV5_09_FIX01_FINAL_20260912T151537Z`
- Final tested source SHA-256: `8746e1babcf43d810a8e13b7ac1cf6bd65a941de845c3e293b8f705c32e69b4c`
- Final export SHA-256: `6d7f294e381af990595626a8fbe28219f8b0bf232f177644041a219904f51e4b`
- Focused XML SHA-256: `df7ec4713b96d8d9ae1656f351b9bd3a69649c672b4428e882d2477d9da84237`
- Independent audit SHA-256: `7cacd106e1de0003ff87426a0c3bd910b73311048554ca489c15b6455a08d840`

## Scope and readiness

- Compile errors: 0
- Temporary `T00_MinimumZeroProducesOnlyPerformanceDiagnostics`: absent
- ComposedGeometryReady=false
- PlayerVerified=false
- SV5_10_SIDEPATH remains LOCKED
- No push and no SV5_10 work were performed

The regenerable `_work` diagnostics are retained locally but excluded from the atomic task commit and
Review ZIP. Final default/repeat exports, the independent audit, focused XML, BINDING, source, tests,
protocol append, active document, Task/Archive, lifecycle rows, and this Result are task-owned evidence.
