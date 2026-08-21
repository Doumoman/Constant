# MAP05_08_BUILD_MANDATORY_ROUTE_GRAPH Result

STATUS: PASS
DONE: PASS

## Scope

- Current Task: `MAP05_08_BUILD_MANDATORY_ROUTE_GRAPH`
- Patch gate: `MAP05_08_BUILD_MANDATORY_ROUTE_GRAPH` manifest validated and applied byte-exactly.
- Prior Result SHA-256 gate: `cbe4f9a136d488df134a6eee676e13950d5dfd15238abf3188a81ce532fbdf65` PASS.
- Implemented only the Task READ/WRITE allowlist scope.
- Next Task `MAP05_09_VALIDATE_MANDATORY_ROUTE_GRAPH` was not started and remains LOCKED.

## Implementation

- New Runtime production C#: 13/13.
- New Runtime test C#: 1/1.
- New matching `.cs.meta`: 14/14.
- Modified existing production C#: 0 (`<=3`).
- Modified existing test C#: 7/7.
- Registered the exact seven route-mask families: Type1, Type2, Type3, Type4 UD, Type4 LUD, Type4 RUD, Type4 LRUD.
- Built immutable route graph node/edge/cell snapshots with deterministic IDs, ordering, reciprocity checks, BFS distances, and route-mask stamping.
- Imported horizontal backbone, vertical gateway, conflict-resolution, accepted-loop, terminal, reservation, and biome-publication identities with fail-closed validation.
- Added deterministic generated-world edge records and exact 11-column CSV serialization with UTF-8 BOM, CRLF, final CRLF, stable order, tokens, and costs.
- No RNG, filesystem, wall-clock, UnityEditor, Unity object, static mutable state, or source mutation was introduced.

## Frozen Starter Vector

- Build result: Completed; errors 0.
- Graph: nodes 47; directed edges 96; undirected edges 48; route cells 47.
- Mask counts Type1/Type2/Type3/Type4-UD/Type4-LUD/Type4-RUD/Type4-LRUD: `20/4/4/17/0/0/2`.
- Mandatory terminals reachable from Start: 7/7.
- Accepted loops represented: 2/2.
- Generated sectors CSV: 16,838 bytes, existing 13-column v1 contract preserved.
- Generated edges CSV: 7,094 bytes, 96 data rows, exact 11-column contract.

## Test Evidence

- `MandatoryRouteGraphBuilderTests`: 281/281 PASS.
- `MandatoryRouteLoopPlannerTests`: 212/212 PASS.
- `UpDownConflictResolverTests`: 194/194 PASS.
- `VerticalGatewayPlannerTests`: 156/156 PASS.
- `HorizontalBackboneRouterTests`: 142/142 PASS.
- `MandatoryConnectorTreeBuilderTests`: 129/129 PASS.
- `MandatoryRouteMaskLookupBuilderTests`: 127/127 PASS.
- `MandatoryTerminalBuilderTests`: 120/120 PASS.
- `GeneratedWorldDataTests`: 56/56 PASS.
- `SiteReservationValidatorTests`: 268/268 PASS.
- `BiomePatchValidatorTests`: 196/196 PASS.
- `Map04ExitTests`: 110/110 PASS.
- Required unique aggregate: 1,991/1,991 PASS (`>=1,962`); failed/skipped 0/0.
- Final changed-file revalidation: `GeneratedWorldDataTests` 56/56 PASS.
- Game.Map targeted discovery: 6,726 (`>=6,697`).
- Full EditMode discovery: 6,838 (`>=6,809`).
- Final Unity Console errors/warnings: 0/0.

## Asset / Change Audit

- Baseline Authoring CSV/meta: 50/50; body/meta modifications 0.
- Final Assets meta/GUID: 3,229/3,229; duplicate GUID 0.
- Task-marker changes: 35 (`<=38`): C# 21, matching C# meta 14.
- New folder meta: 0.
- asmdef/Scene/Prefab/Package/ProjectSettings modifications: 0.
- Unexpected Assets changes: 0.

## Documentation Finding

- The three separately referenced package notes `06_ROUTE_TOPOLOGY_CONSTRAINTS.md`, `07_OPTIONAL_EDGE_OVERLAY.md`, and `MAP05_ROUTE_123_GENERATOR.md` were absent from the repository.
- The frozen Current Task contract and the available generated-edge CSV schema/dictionary were sufficient; no out-of-scope file was created or modified.

## Done Conditions

- Exact Task deliverables, graph semantics, deterministic serialization, negative-audit transition, test gates, discovery gates, Console gate, and asset/change gates: PASS.
- Current Task completion is authorized; no subsequent Task execution is authorized.
