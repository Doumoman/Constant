TASK: MAP07_07_IMPLEMENT_MICROCHUNK_REACHABILITY_PROBE
STATUS: PASS
MAP07_07: COMPLETE ELIGIBLE
MAP07_08_CREATE_MICROCHUNK_AUTHORING_GRID: LOCKED / DO NOT START

## Patch and input integrity

- Applied patch: `MAP07_07_IMPLEMENT_MICROCHUNK_REACHABILITY_PROBE` v1.0
- Applied receipt SHA-256: `e870f3e1221a3927db934fdafb32b711fdb107f69d1ac396109b4fab3a0c18d5`
- Patched Master SHA-256: `41e9a65d7424bf58a9dcbfdca007751bc1bb06b68991839e6d3d89ce701d9493`
- Patched pre-finalization Status SHA-256: `10b1564ff84b2f695d0e6e65de50cbbeeaf51cddb8ba648349c2ffbb6bdc3508`
- MAP07_06 Result SHA-256: `81681d92aac6bff244dc7f655014c89cabb43baa178b3355fe701c6046b1a6e0`
- MAP07_06 Task SHA-256: `38a601ca63dff23622564cf36b3c02aa2f55849808c69b3a58bf60d2a8d7c6fa`
- MAP07_07 Task SHA-256: `0d9ec87691cf31db249b2fed7b411ea6b69a1d8c456469672c96999145add103`
- Reachability probe model/API digest: `f488c8a65dacb8f7bdd2c107478074c131e3011110058375c06e165bfb1ddaf3`
- Preserved 96-cell validator digest: `54a09f7327c37405a826e4fbc3bea9443e1472ec3f92f15b9495edfba422710c`
- Preserved object-slot validator digest: `9a3b86991302add138c36acccd18789ad79195cd27cab7fb5fe2c5bb8a520e6a`
- Preserved socket-edge validator digest: `fdfbcb7bf651eb963d899f7e9800e0a8f23826d43ec4ffed25e3c32ee7a0c048`
- Preserved transform model/API digest: `7031695ec2c4bb333be69ba490c03aef003124d50953d577b4943c634336b031`
- Preserved tile-layer rules digest: `ace56d38399aa6ccea6e0e0e7361f3802b7eb7418b1ef1d081f6a40b04e08160`
- Preserved MAP07_01 model/API digest: `5ed21ca7b86cfebf0095eba6d14bf4bb27be75ce39ffaf46f3c32a516d77613b`
- Preserved Authoring manifest SHA-256: `4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3`

The patch was the only unapplied MCP inbox item. Its predecessor state, Current Task transition, 205-task Master count, prior hashes, Unity version, Assets meta count, Authoring counts, and tracked Assets preconditions matched the manifest. The three payload destinations matched their source SHA-256 values, the `.APPLIED` receipt was written, and no unapplied patch remains.

The reachability digest uses the established deterministic manifest method: sort the six repository-relative runtime C# paths ordinally, append `path=lowercase-file-sha256\n`, and SHA-256 the UTF-8 manifest. All prior MAP07 production files and Authoring CSV/meta inputs have zero tracked changes, preserving their approved digests.

## Implemented runtime graph and reachability

- `MicrochunkTraversalNode.cs`
  - Adds an immutable node carrying the source microchunk ID and exact 12x8 local coordinate.
- `MicrochunkTraversalEdge.cs`
  - Adds immutable source/target coordinates, exact movement-kind token, and non-negative deterministic cost.
  - Publishes exact `FLOOD`, `WALK`, `JUMP`, `DROP`, `CLIMB`, and `SOCKET_ENTRY` tokens with stable enum conversion.
- `MicrochunkReachabilityPolicy.cs`
  - Freezes maximum jump rise/span, maximum drop distance, optional climb-marker codes, and caller-controlled deterministic neighbor ordering.
  - Default policy remains no-tool local traversal and has no player physics, input, animation, or world-traversal dependency.
- `MicrochunkReachabilityViolation.cs`
  - Adds immutable microchunk/socket/pair/coordinate diagnostics with exact `CELL_COVERAGE_INVALID`, `MANDATORY_SOCKET_ENTRY_UNREACHABLE`, and `MANDATORY_SOCKET_PAIR_UNREACHABLE` reasons.
- `MicrochunkReachabilityResult.cs`
  - Freezes canonical nodes, edges, socket-entry snapshots, violations, and ordered path witnesses.
  - Exposes evaluated socket/pair counts, reachable count, issue count, success, and immutable witness cost/coordinates/edges.
- `MicrochunkReachabilityProbe.cs`
  - Gates graph construction on successful complete 96-cell coverage, including strict checks on supplied prior coverage results.
  - Excludes GroundSolid, Breakable, Hazard, and Liquid cells while leaving OneWay, decorations, Marker, and `NONE` traversable.
  - Filters exact mandatory/no-tool sockets and resolves their supplied in-memory bands to L/R/D/U edge cells.
  - Builds deterministic cardinal flood, horizontal walk, policy-limited jump/drop, marker-qualified climb, and socket-entry edges.
  - Evaluates every unordered mandatory socket pair with deterministic multi-source BFS and publishes the chosen shortest ordered local-coordinate witness.

The probe does not perform tile-code FK checks, tile-layer compatibility, full socket-edge validation, object-slot validation, CSV import/export, editor UI, preview generation, boundary/sector assembly, or world traversal. Definitions, cells, sockets, bands, policies, transform outputs, and prior validator results remain unmodified.

## EditMode coverage and boundary advance

`MicrochunkReachabilityProbeTests.cs` executes 522 deterministic cases. Coverage includes:

- all 96 exact unblocked node coordinates;
- all 96 coordinates independently blocked by each of GroundSolid, Breakable, Hazard, and Liquid (`384` cases);
- OneWay, DecorationBack, DecorationFront, Marker, and explicit `NONE` non-blocking behavior;
- immutable node, edge, policy, violation, result, socket-entry, and witness snapshots;
- exact movement tokens, policy ordering, and flood/walk/jump/drop/climb/socket-entry edges;
- mandatory/no-tool filtering and L/R/D/U band-to-entry projection;
- missing/invalid/blocked entry diagnostics and zero/one/two/multiple socket pair enumeration;
- deterministic row-major BFS witness and movement-order tie-breaking;
- unreachable wall fixtures, coverage-gate false-success prevention, and supplied prior-result gating;
- all four MAP07_03 transforms and source non-mutation;
- absence of all MAP07_08+ forbidden production symbols.

Five allowlisted existing boundary tests were advanced by replacing only the newly implemented `MicrochunkReachabilityProbe` future-boundary symbol with the still-forbidden `MicrochunkAuthoringGrid` symbol. Each existing-file diff is exactly one addition and one deletion; no fixture, assertion, case, skip, or ignore was removed or weakened.

## Unity verification

Unity version: `6000.3.8f1`

```text
MicrochunkReachabilityProbeTests:          522 / 522 PASS
Microchunk96CellValidatorTests:            406 / 406 PASS
MicrochunkObjectSlotValidatorTests:        483 / 483 PASS
MicrochunkSocketEdgeValidatorTests:        332 / 332 PASS
MicrochunkTransformerTests:                483 / 483 PASS
MicrochunkTileLayerRulesTests:             150 / 150 PASS
MicrochunkDefinitionTests:                 146 / 146 PASS
Existing MAP07 regression union:          2000 / 2000 PASS
MAP06 category union:                     2552 / 2552 PASS
OptionalRegionModelsTests:                 194 / 194 PASS
MAP06 required total:                     2746 / 2746 PASS
MAP05 category union:                     1832 / 1832 PASS
MandatoryRouteMaskLookupBuilderTests:      127 / 127 PASS
MAP05 required total:                     1959 / 1959 PASS
Actually executed total:                  7227 / 7227 PASS
Failed / skipped:                            0 / 0
Compilation errors:                              0
Final Console errors / warnings:             0 / 0
Relevant warnings:                                0
```

The initial focused test request followed the one-time new-script import/domain reload and timed out during Test Runner initialization before executing any test. After the editor returned ready, the exact fixture passed `522/522`. A later OptionalRegionModels filter used an incomplete namespace and selected `0` tests; the corrected exact fixture name then passed `194/194`. Neither zero-execution request is included in the required execution total.

## Static and change-scope verification

```text
Assets meta before / after:              3362 / 3369
Assets GUID rows / duplicate groups:     3369 / 0
New Runtime C# / matching meta:              6 / 6
New test C# / matching meta:                 1 / 1
New folder meta:                             0
Existing boundary test C# modified:          5 <= 17
Per existing test diff:                      1 add / 1 delete
Existing matching test meta modified:        0
Authoring CSV / matching meta:               50 / 50
Authoring manifest changes:                   0
Generated CSV files created:                  0
Scene / Prefab tracked changes:              0 / 0
ProjectSettings / Packages changes:          0 / 0
asmdef / asmref changes:                     0 / 0
MAP07_01 production source changes:           0
MAP07_02 production source changes:           0
MAP07_03 production source changes:           0
MAP07_04 production source changes:           0
MAP07_05 production source changes:           0
MAP07_06 production source changes:           0
MAP06 production source changes:              0
Forbidden MAP07_08+ production hits:          0
Assets duplicate GUID groups:                 0
Unapplied MCP patches:                        0
```

Unity's generated solution inventory was restored after refresh. No Scene, Prefab, asset body outside the allowlist, ProjectSettings, Packages, assembly definition/reference, Authoring CSV/meta, prior MAP07 production source, MAP06 production source, or later-Task production file remains changed.

## Finalization boundary

MAP07_07 is eligible to transition from `CURRENT` to `COMPLETE`. Finalization may update only the MAP07_07 row/current-task and last-completed/result fields in `06_IMPLEMENTATION_STATUS.md`. `MAP07_08_CREATE_MICROCHUNK_AUTHORING_GRID` remains `LOCKED`; its Task body was not read and its implementation was not started.
