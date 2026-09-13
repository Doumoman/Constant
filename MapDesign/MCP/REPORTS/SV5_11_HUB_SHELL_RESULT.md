# SV5_11_HUB_SHELL Result

TASK: SV5_11_HUB_SHELL
TASK_ID: SV5_11_HUB_SHELL
STATUS: PASS

## Outcome

SV5_11_HUB_SHELL now selects one deterministic 24×40 actual-cell hub shell per profile. Each accepted hub owns 960 unique 1×1 cells, a 12×30 passable inner volume, a continuous reserved tree slot, left/right LOW/MID/HIGH sockets with 6–7-cell apertures, and five active Ports connected to five distinct external RoomId/SpaceGroupId pairs.

The external cardinal centerlines are non-owning connection skeleton evidence. Actual Hub writes stay inside the accepted footprint, which is independently checked against all predecessor Room, reservation, core/Type0/gate/port, loop, and sidepath cells. Tree Grab collision, branches, landing surfaces, and Player verification remain deferred to SV5_12.

## Final exports

| Profile | Candidates | Accepted | Footprint | Actual cells | Sockets | Ports | Distinct rooms/groups | Build time |
|---|---:|---:|---|---:|---:|---:|---:|---:|
| default | 3,220 | 1 | 272,264,24,40 | 960 | 6 | 5 | 5 / 5 | 3422.247 ms |
| repeat | 3,220 | 1 | 272,208,24,40 | 960 | 6 | 5 | 5 / 5 | 2363.884 ms |

Every changed cell carries HubId and 4×4 MicroPattern ownership. Candidate evaluation performs zero whole-world copies and zero whole-world BFS runs per candidate. The accepted batch preserves the predecessor physical product and validates 9 legal states × 6 resource orders exactly once per profile.

## Verification

- Compile: 0 errors, 77 existing warnings.
- Final Hub targeted: 16 / 16 PASS in 213.8665364 seconds.
- Independent checker: `PASS_INDEPENDENT_HUB_SHELL` using CHECKER_SCOPE_FIX02.
- Final focused SV5 regression: 155 / 155 PASS; failed/skipped/inconclusive = 0 / 0 / 0.
- Existing predecessor test names preserved: 139 / 139; missing = 0.
- Final focused regression executions: exactly 1.
- Final focused duration: 1230.6790154 seconds (2026-09-13T05:47:00Z–06:07:31Z).

The full regression's export test regenerated the same semantic payload from the fixed source. The resulting final export tree was independently checked again without repeating the full regression.

## Evidence binding

- Final tested source SHA-256: `650506d58903c6b734c5ae61dd58053867e6079f7efa1a3f040c0c5135dd8528`
- Final export SHA-256: `351ded83a3ba431f5e34edc95b25663ea252bb8e829389697a8d628cdb90c5d4`
- Focused XML SHA-256: `b7d409692bcc2c96ebc94f590ef8a0573ea6060658b2103fa8a61eb9f412e9cd`
- Independent audit SHA-256: `bdfa26d9e370bf57a594e51dcae102f3525dab68b02b062bfb7ec3712ab69c53`
- Bound Task/Archive SHA-256: `491f9b9df0b498bc6351efe1c9ff60415fa2bdb91b7e559cc20af558e5f8b97b`
- Original package manifest SHA-256: `ab9a99b7e3d3e924908bcfff062a4b1c764ecf2fa88058d6e50ee208a4adac1b`
- Final checker correction manifest SHA-256: `6358f177a22e40d6bc00fff0a0bdb60d49708e1efca6fd29ceff683e86883e3e`

## Scope and readiness

- `TreeGrabGeometryReady=false`
- `ComposedGeometryReady=false`
- `PlayerVerified=false`
- `SV5_12_TREE_GRAB` remains `LOCKED`; no SV5_12 work was started.
- No push was performed.
- Local `_work` diagnostics are excluded from the atomic task commit and Review ZIP.
