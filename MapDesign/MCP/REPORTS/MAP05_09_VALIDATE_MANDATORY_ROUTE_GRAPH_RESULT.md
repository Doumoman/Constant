# MAP05_09_VALIDATE_MANDATORY_ROUTE_GRAPH Result

TASK: MAP05_09_VALIDATE_MANDATORY_ROUTE_GRAPH
STATUS: PASS
DONE: PASS

## Patch / Read Gate

- Patch `MAP05_09_VALIDATE_MANDATORY_ROUTE_GRAPH` was the only unapplied MCP_INBOX patch.
- Manifest preconditions, prior Result exact PASS/SHA-256, 205-task payload state, and project baseline: PASS.
- Master, Status, Task payload copies matched their destinations byte-for-byte after apply.
- Previous Result SHA-256: `7c9820290ec5269222b8c145603a9ae53a2ea7f8d1df7b0ca6029e1be3647a99`.
- Mandatory global/Task/prior Result/read-allowlist contracts were read.

## Created / Modified

- New Runtime production C#: 8/8.
- New Runtime EditMode test C#: 1/1.
- New matching `.cs.meta`: 9/9.
- Modified existing production C#: 0.
- Modified existing test C#: 8/8, limited to the MAP05_09 output-symbol / MAP05_10+ negative-audit transition.
- Graph, route mask, `SectorCell`, generated CSV producers, Authoring CSV/meta, asmdef, Scene, Prefab, Package, and ProjectSettings modifications: 0.

## Validation Contract

- Required validation rules registered/evaluated/passed: 12/12/12.
- Violations/errors/warnings: 0/0/0.
- Pass ID: `PASS_ROUTE`.
- Graph nodes/directed edges/undirected edges/route cells: `47/96/48/47`.
- Mask counts T1/T2/T3/T4-UD/T4-LUD/T4-RUD/T4-LRUD: `20/4/4/17/0/0/2`.
- Type4 U+D required and actual L/R preserved; `UD/LUD/RUD/LRUD` all legal.
- Mandatory terminals reachable from Start: 7/7.
- Accepted independent loops represented: 2/2.
- Generated sectors CSV bytes/columns: `16838/13`.
- Generated edges CSV bytes/rows/columns: `7094/96/11`.
- Edge reciprocity, side/reverse-side, open/layer/cost/source, row bijection, sector stamp, forbidden role/reserved interior, BFS, and loop checks: PASS.
- Deterministic culture/fresh/reuse/thread validation and ordered deduplicated immutable violations: PASS.
- RNG draws/filesystem reads/writes/clock reads/source mutations: `0/0/0/0/0`.

## Test Evidence

- `MandatoryRouteGraphValidatorTests`: 298/298 PASS (`>=240`).
- `MandatoryRouteGraphBuilderTests`: 281/281 PASS.
- `MandatoryRouteLoopPlannerTests`: 212/212 PASS.
- `UpDownConflictResolverTests`: 194/194 PASS.
- `VerticalGatewayPlannerTests`: 156/156 PASS.
- `HorizontalBackboneRouterTests`: 142/142 PASS.
- `MandatoryConnectorTreeBuilderTests`: 129/129 PASS.
- `MandatoryRouteMaskLookupBuilderTests`: 127/127 PASS after restoring two MAP05_10+ negative-audit cases.
- `MandatoryTerminalBuilderTests`: 120/120 PASS.
- `GeneratedWorldDataTests`: 56/56 PASS.
- `SiteReservationValidatorTests`: 268/268 PASS.
- `BiomePatchValidatorTests`: 196/196 PASS.
- `Map04ExitTests`: 110/110 PASS.
- Latest unique required suite aggregate: 2,289/2,289 PASS (`>=2,231`); failed/skipped 0/0.
- Actual test invocations including the focused negative-audit repair rerun: 2,414/2,414 PASS.
- Game.Map targeted discovery arithmetic: 7,024 (`>=6,966`).
- Full EditMode discovery: 7,136 (`>=7,078`).

## Unity / Asset / Ownership Gate

- Unity: `6000.3.8f1`.
- Forced script import/domain reload/compile completed; compile errors 0.
- Final Console errors/warnings: 0/0.
- Scene/Prefab changes: NONE.
- Final Assets meta/GUID: 3,238/3,238; duplicate GUID groups 0.
- Accepted legacy Editor folder meta: 6/6.
- Task-marker Assets changes: 26 exact = C# 17 + meta 9.
- New Runtime/test/meta: 8/1/9; existing production/test modifications: 0/8; unexpected Assets changes 0.
- Authoring CSV/meta: 50/50; modifications 0.
- Authoring CSV tree SHA-256: `898eed89163b37f6fe691294fce987b8a5c75e64a1b50bcef03bc8308a9f92c9`.
- Authoring meta tree SHA-256: `b99e724a05f3a9ece974bcb60c1104cb8aa104a6408a450c37025bfe1a4c35d0`.
- No UnityEditor/Unity object/System.IO/RNG/clock/nullable/record/init/required/static mutable dependency was introduced.

## Out-of-Scope Findings

- Optional references `06_ROUTE_TOPOLOGY_CONSTRAINTS.md`, `07_OPTIONAL_EDGE_OVERLAY.md`, and `MAP05_ROUTE_123_GENERATOR.md` are absent; Task policy says this is not a blocker.
- Available generated-sector/generated-edge schema headers were used; no Authoring CSV body was read or modified.

## Done / Next / Commit

- All Objective, Validator Contract, Required Tests, Asset/Meta/Change Gate, and failure-policy conditions: PASS.
- MAP05_09 may be finalized to COMPLETE; `MAP05_10_CREATE_MANDATORY_ROUTE_OVERLAY` must remain LOCKED.
- Git commit: NOT CREATED (automatic Git operations prohibited). Recommended commit remains `feat(map): validate mandatory route graph`.
