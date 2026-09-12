# SV5_10_SIDEPATH Result

TASK: SV5_10_SIDEPATH
TASK_ID: SV5_10_SIDEPATH
STATUS: PASS

## Outcome

SV5_10_SIDEPATH now generates irregular 20–50-cell AIR-centerline sidepaths from actual world-coordinate endpoints, RoomId, SpaceGroupId, occupancy, and supported-foot cells. The implementation uses an X-sorted sliding window, Manhattan distance <=49, normalized EndpointId pairs, bounded dead-end search, changed-cell overlays, and local affected-foot-node validation. Packing and the final global topology plus 9-state x 6-order validation run once on the accepted batch.

The original applied contract contained retired 48-by-32 Sector clauses. The user correction superseded those clauses before Finalize, the original INPUTS bytes were preserved unchanged for audit, and the production implementation contains no Sector dependency. The checker shipped inside the original INPUTS package was neither run nor modified because it validates the retired contract. `CONTRACT_CORRECTION.json` records the replacement and is bound by raw SHA-256.

## Final exports

| Profile | Endpoints | Nearby normalized pairs | Generated | Accepted | Returning | SpaceGroups | AIR length | Center range X / Y |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| default | 1,119 | 134,739 | 452 | 12 | 8 | 12 | 27–35 | 65–611 / 11–384 |
| repeat | 1,255 | 148,648 | 484 | 12 | 5 | 12 | 27–31 | 65–611 / 11–392 |

The near-distance filter removes 78.460% of all default endpoint pairs and 81.109% of all repeat endpoint pairs before path search. There are no whole-world copies or whole-world BFS runs per candidate. Each final production export used one sequential worker.

The approved targeted timing measured candidate generation at 88.221 ms with worker=1 and 162.786 ms with worker=4. At this candidate volume, parallel overhead is larger than the useful parallel work, so production selects worker=1. Parallel mode remains only as an optional determinism regression path; worker=1 and worker=4 produce the same sidepath digest. Parallel speedup is not a PASS condition.

No retired symbol or export name was found in the active runtime source, active test source, `02_PROTOCOL_V5.md`, `18_SIDEPATHS_V5.md`, or final default/repeat exports. Historical INPUTS, installed Task, and Archive were excluded from that scan and preserved byte-identically.

## Independent verification

The non-Sector checker imports no production C#. It independently recalculates endpoint ordering, X-window completeness, Manhattan eligibility, normalized-pair uniqueness, same-room/direct-pair exclusion, AIR length and cardinal continuity, self-crossing, head clearance and support, newly carved AIR, return/dead-end evidence, RoomId/SpaceGroupId ownership, protected/Type0/progression violations, and worker digest equality.

Result: `PASS_INDEPENDENT_SIDEPATH`.

## Final focused regression

Command:

`unity test . --mode EditMode --filter StarNight.Map.Tests.EditMode.Sv5 --output MapDesign/MCP/GENERATED/SV5_10_SIDEPATH/focused_results.xml --timeout 2700 --format json`

- Discovered/total/passed: 139 / 139 / 139
- Failed/skipped/inconclusive: 0 / 0 / 0
- Existing tests preserved: 125
- New non-Sector sidepath tests: 14 / 14 PASS
- Duration: 1327.1063598 seconds
- Start/end UTC: 2026-09-12T17:43:35Z / 2026-09-12T18:05:43Z

The exact production source and export hashes were checked before and after this single final focused run and did not change.

## Final evidence binding

- Final run ID: `SV5_10_SIDEPATH_FINAL_20260912T174335Z`
- Final tested source SHA-256: `b67cd33dc721d24f5796dc0a65fc82c582c748f1a2a9b0bdafa5af9453f7dabb`
- Final export SHA-256: `0b8b3a8c6b04fe0b5669ac0d6069038ce17a540521740ba3c45a37d7bd6f1e11`
- Contract correction SHA-256: `b52a407889004d5a4556ce81bafe337e80cc2aadaa54c5524698a22b28fc8933`
- Focused XML SHA-256: `f2870fa7f9600795727c3a783ae8311aa2e05b3f0104ff3c929f97cafa48f65b`
- Independent audit SHA-256: `dc449ba908eac8e3c0f0bd263dc5c683749a94ee70f495cc90eca6d8b4e32a31`
- Bound Task/Archive SHA-256: `d3e266c6e6bfe55d6ce2e7a1f4e832da90e416fb4587b98ec1986db717b66300`
- Original package manifest SHA-256: `42cd94a2d61f8a274655b3e16aa23e53ee68f115d918928b5b840e40b41e01b8`

## Scope and readiness

- Compile errors: 0
- Targeted tests: 14 / 14 PASS
- Final focused SV5 regression executions on frozen non-Sector source/export: 1
- ComposedGeometryReady=false
- PlayerVerified=false
- SV5_11_HUB_SHELL remains LOCKED
- No SV5_11 work and no push were performed

The local `_work` tree, including obsolete pre-correction generated evidence, is excluded from the atomic task commit and Review ZIP.
