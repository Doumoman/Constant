TASK: MAP11_03_COMPILE_ROUTE_SPINE_AND_TRAVERSAL_ENVELOPE
STATUS: PASS
MAP11_03: COMPLETE ELIGIBLE
MAP11_04_IMPLEMENT_BASE_HIGH_AND_RECOVERY_ROUTES: LOCKED / DO NOT START

## Responsibility and Added Functions

| Field | Actual result |
|---|---|
| Task responsibility | Every validated MAP09_04 SpineVariant node, movement edge, and authored envelope tile was projected through the exact MAP11_01 Local Canvas mapping and bound to the MAP11_02 role/socket/node contract. |
| Added functions | Immutable compiled nodes, edges, seven-set envelopes, all-variant route spines, coalesced protected tiles with lossless provenance, atomic request/result/errors, and deterministic SHA-256 digest publication. |
| Inputs consumed | Validated MAP09_04 `TerrainClusterContract` plus digest, successful MAP11_01 `TerrainClusterLocalCanvas` plus digest, and successful MAP11_02 `TerrainClusterRoleSocketContract` plus digest. |
| Outputs produced | Immutable `TerrainClusterTraversalCompilation` containing every compiled variant/node/edge/envelope and per-variant/whole-artifact protection unions, or stable accumulated errors with no partial output. |
| Explicit non-ownership | No endpoint-derived line/arc/floor generation, physics/jump simulation, route witness classification, RNG/variant selection, shell/pattern generation, MAP10 planner/renderer execution, starter/CSV generation, sector placement, or world assembly was implemented. |
| Downstream consumers | MAP11_04 may consume the compiled graph to build route witnesses; MAP11_05 may consume the protected provenance for pattern zones. MAP11_04 remains locked and was not started. |

## Predecessor, Apply, and Status Evidence

- Starting HEAD: `c97630c7d293ad1a9f5bc3a7b229450093b9d7d7` (`MAP11_02: implement cluster roles and socket contract`).
- MAP11_02 Result SHA-256: `824c0b93c791539507a92390a0b1a26ec2f41748de2373b1f6499fbc272d6ded` (exact required value, PASS).
- MAP11_02 installed/archive Task SHA-256: `cafac59a3ad2dff40ce51c6dba249da02505b847ea1e9a9730ce3aaf1bcf89d3` (exact required value).
- MAP11_03 inbox/installed/archive Task SHA-256: `cbd56cd697bc0674c1e58f3fef47e14ca52bf18a696d5d7c118b55d41f3b177f` (byte-identical).
- Apply state was exactly one CURRENT row for MAP11_03, 125 COMPLETE / 1 CURRENT / 89 LOCKED across 215 rows; inbox candidates were 0 after archival.
- MAP11_04 remained `LOCKED` throughout. No MAP11_04 implementation or test was started.

## Added Files and Public Surface

| File | SHA-256 | Unity GUID |
|---|---|---|
| `TerrainClusterRouteSpine.cs` | `85807438e2d0e8213eb6f16e6e6683a83c0688b2ebc9981a868d6da977106f67` | `763c324e845fb9f49beaed1648daba29` |
| `TerrainClusterTraversalEnvelope.cs` | `fcbccd67efc70b82b85149d0e564a228ec50ba7fc4f2f48955ff4370bd0380f5` | `8406722336587dc4ea2a023a64f5c6ac` |
| `TerrainClusterTraversalCompiler.cs` | `5561d24b1cfcdd04a7ee12d07f70a49ddd4c771cfbb98a17aeec68b6b9cfb3d2` | `d625252a51d857a4f9e41376e4fca898` |
| `TerrainClusterTraversalCompilerTests.cs` | `c39b283e1cffc1c6aaf79798627d01a294e0d74650afe575fea231aa9fa09079` | `0a6e9aa3c9330324b893c54f03a7f1b9` |

The four matching `.meta` SHA-256 values are, in the same order: `120fd85de4921542f2d22fd8e0a4fbc172bb88e75f543b359277ec3a6b7b1413`, `22fce40a6379fbb084fad1b750042cdd9c082e24cdf3b11a50cd5f6c785da15e`, `453594ed75794fdb1d4e04c3194b3244882029273f7a1d08ed8b84d6b6a2138b`, and `c067e804799d7b5782ae493509e7da103a23c491db35a3a0f3593dfbefa7be0e`.

Unity reflection loaded these task-owned public types from `Game.Map.Runtime`: `CompiledTraversalNode`, `CompiledTraversalEdge`, `CompiledTraversalEnvelope`, `ClusterTraversalProtectionSourceKind`, `ClusterTraversalProtectedTile`, `ClusterTraversalProtectedTileProvenance`, `CompiledClusterSpineVariant`, `TerrainClusterTraversalCompileRequest`, `TerrainClusterTraversalCompilation`, `TerrainClusterTraversalCompileError`, `TerrainClusterTraversalCompileResult`, and `TerrainClusterTraversalCompiler`.

## Variant, Node, and Edge Projection Evidence

- Every source SpineVariant is compiled in canonical variant-ID order; no variant is selected, weighted, or discarded.
- Nodes preserve variant membership, node ID, source and compiled coordinates, active owning compiled chunk, mandatory flag, graph/source role provenance, and all MAP11_02 linked role IDs/kinds.
- R0, MirrorX, MirrorY, and R180 node/edge/envelope transformations use MAP11_01 lookup results rather than a duplicate coordinate authority.
- Edges preserve exact Walk/Jump/Drop/Climb/Slide/Bounce movement kinds, From/To node IDs, source/compiled Start/End, clearance dimensions, source/compiled Landing/Recovery, mandatory flag, source graph kind, and authored envelope provenance.
- Compiled Start/End exactly equal the compiled From/To node coordinates. Landing/Recovery and every published tile are confirmed active in both tile and owning-chunk masks.
- MAP11_02 Entry/Exit role/port/node links are reproduced and checked against each variant before reachability validation.
- Directed Entry-to-Exit reachability through mandatory edges and reachability of every mandatory node/edge are preserved in the compiled graph.

## Seven-Set Envelope Evidence

Each edge publishes the exact authored sets `Centerline`, `Floor`, `Clearance`, `JumpArc`, `DropColumn`, `Landing`, and `Recovery`. Each entry carries source coordinate, compiled coordinate, owning compiled chunk, and set kind. Set cardinality is preserved and compiled coordinates are canonical `(y,x)` order.

| Movement | Required | Must be empty |
|---|---|---|
| Walk | Floor, Clearance, Landing, Recovery | JumpArc, DropColumn |
| Jump | Clearance, JumpArc, Landing, Recovery | DropColumn |
| Drop | Clearance, DropColumn, Landing, Recovery | JumpArc |
| Climb | Clearance, Landing, Recovery | JumpArc, DropColumn |
| Slide | Floor, Clearance, Landing, Recovery | JumpArc, DropColumn |
| Bounce | Clearance, JumpArc, Landing, Recovery | DropColumn |

Common invariants require non-empty Centerline and Clearance, Start/End membership in Centerline, explicit Landing/Recovery membership, active coordinates, and disjoint Floor/Clearance. Invalid, duplicate, inactive, or out-of-bounds authored evidence fails atomically; no geometry is inferred from endpoints.

## Protected Tile Provenance Evidence

- `RouteSpine` publishes every node, every edge Start/End, and every Centerline tile.
- `TraversalEnvelope` publishes Floor, Clearance, JumpArc, DropColumn, Landing, and Recovery tiles.
- Provenance preserves source kind, variant ID, node and/or edge ID, envelope set kind where applicable, source/compiled coordinate, and mandatory fact.
- Same-coordinate protection is coalesced into one tile while retaining every unique, canonical provenance record, including overlapping node/endpoint/centerline facts and different variants.
- Deterministic protected collections are available per variant and as the whole compilation union.
- MAP10 protected-source names are preserved semantically; no MAP10 mask planner, renderer, selector, or mutation policy is invoked.

## Immutability, Digest, and Error Evidence

- Requests defensively copy the six-chunk allowlist; compilation/result collections are read-only defensive copies with canonical lookup maps.
- MAP09_04, MAP11_01, and MAP11_02 artifacts are independently regenerated or semantically compared before compilation. Identity and digest mismatches are distinct atomic errors.
- Errors are accumulated, deduplicated, and stable-sorted. Failure exposes zero compilation, variants, nodes, edges, envelopes, protected tiles, and digest.
- All 23 required error distinctions are published from `MissingInput` through `NonCanonicalPublication`.
- The digest includes the ruleset, all three input digests, transform, every variant/node/edge field, all seven envelope sets, and every protection provenance record. Locale, display text, timestamp, object identity, reflection/file order, and input order are excluded.
- Reversed input and culture changes preserve publication/digest; a semantic clearance change alters the digest while a display-text-only change does not.

## Focused Verification and No-Regression Evidence

Unity Editor: `6000.3.8f1`, instance `Constant@ced6e0df`.

```text
MAP11_03 focused: discovered 27 / executed 27 / pass 27 / fail 0 / skip 0 / inconclusive 0
REGRESSION TRIGGER DETECTED: YES (owner: MAP11_03 task-owned new-file import/test initialization; reason: the first focused launch overlapped new asset import/cleanup verification and discovered 0 tests; minimum scope: refresh/import and rerun MAP11_03 only; final focused run passed 27/27)
PRIOR TASK TEST SELECTIONS: 0 (normal path)
LEGACY TEST SELECTIONS: 0 (normal path)
PLAYMODE TEST SELECTIONS: 0
```

- Final focused job: `4edf7752ad7c4c5fb2a68ec9f7c43af5`, result `Passed`, duration 2.1976203 seconds.
- The initial import-gated job discovered/executed 0 category tests and was not accepted as PASS evidence. It reported no C# compilation or assertion failure; only the same focused category was rerun.
- Unity compilation errors: 0.
- Final cleared Console: errors 0 / relevant warnings 0 / total entries 0.
- No MAP09, MAP10, MAP11_01, MAP11_02, legacy 19347, PlayMode, or unfiltered test selection was run.
- Scene/Prefab changes: NONE.

## Static Gates and Change Scope

| Gate | Actual result |
|---|---|
| Existing MAP11_01-MAP11_02 production/test/meta modifications | 0 |
| Existing MAP00-MAP11_02 production/test/CSV/meta modifications | 0 |
| MicroPattern definitions / physical rows | 24 / 453 |
| Catalog CSV SHA-256 | `f9d9e9cc60c4e4d7561c5aa6502228c18fc9566e3e0febab206ea3264b408267` |
| Cells CSV SHA-256 | `e702ae5d02d7ec9d2cda129c1361699e37d942c280c8f9bd1f3200f155084381` |
| Full 52-file Authoring manifest | `4415ae4af5196d6793f5d0152c0688e5bf35dc4ad23442791e45d3cfd81d0851` |
| Generated CSV | 0 |
| Valid Unity GUID rows / duplicate GUID groups | 3925 / 0 |
| Missing task-owned `.meta` | 0 |
| Existing asmdef/asmref/Scene/Prefab/Settings/Packages changes | 0 |
| Unapplied inbox candidate / legacy collision | 0 / 0 |
| Staged paths before Finalize | 0 |

Only the three allowed runtime C# files and metas, one focused test and meta, installed/archive task documents, this Result, and the implementation status file are eligible for the atomic commit. No unrelated path is included.

## Commit Handoff

```text
Subject: MAP11_03: compile route spine and traversal envelope
Push: NOT PERFORMED
```

Finalize is eligible only from this PASS Result: set Current Task to NONE, set only the MAP11_03 row from CURRENT to COMPLETE, preserve MAP11_04 as LOCKED, explicitly stage only task-owned paths, and commit atomically. MAP11_04 is not auto-started.
