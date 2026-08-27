# MAP09_06 - Implement Special Canvas and Slice Contracts Result

```text
TASK: MAP09_06_IMPLEMENT_SPECIAL_CANVAS_AND_SLICE_CONTRACTS
STATUS: PASS
MAP09_06: COMPLETE ELIGIBLE
MAP09_07_EXTEND_CSV_REGISTRY_AND_CREATE_COMPATIBILITY_FIXTURES: LOCKED / DO NOT START
```

## Responsibility and Added Functions

| Field | Implemented responsibility |
|---|---|
| Task responsibility | Defines immutable data ownership and invariants for pre-reserved `SpecialRegion`, resolved 48x32 `SectorCanvas`, and validated 4x4 of 12x8 `GeneratedSlice` artifacts. |
| Added functions | Adds typed IDs and exact enums, defensive immutable models, stable-sorted accumulating validators, canonical SHA-256 digests, reservation/port/footprint binding, fixed-shell/slot and persistence separation, complete Canvas cell/layer/source/stamp validation, and exact-once Slice mapping/provenance validation. |
| Inputs consumed | Reuses `SiteReservationId`, `SiteReservation`, `SiteEntrySide`, `SectorCoord`, `LocalTileCoord`, `WorldGenConstants`, `AccessClass`, the V2 pass/layer catalog digests, MAP07 fixed 12x8 geometry, and MAP08 boundary stable source identity without redefining their authority. |
| Outputs produced | Publishes validated immutable `SpecialRegionContract`, `SectorCanvasContract`, `GeneratedSliceSet`, their validation Results, and semantic digests for downstream compatibility, assembly, projection, bake, and persistence consumers. |
| Explicit non-ownership | Does not implement reservation solving, cluster/content assembly, tile composition/cleanup/validation execution, stamp issuance, slicing, CSV writing, Authoring import, Tilemap bake, streaming, SaveData state, prefab/NPC/facility/boss logic, or any RNG/Unity lifecycle behavior. |
| Downstream consumers | MAP09_07 consumes the contract surface for additive CSV compatibility fixtures; MAP13 consumes reservation/fixed-shell/slot/persistence semantics; MAP16 consumes Canvas validation and Slice mapping/provenance; MAP17 consumes immutable slice storage coordinates and persistence provenance. |

## Predecessor, Status, and Dirty Preflight

The single root inbox candidate passed every `single_task_v1` gate and was installed and archived byte-identically before its body was executed.

```text
Preflight HEAD: 4b9374b37d1a9d529b2c707a7111d3ecd0a40098
MAP09_05 Result status: PASS
MAP09_05 Result SHA-256:
7089f72367eb6b0369a73c3322db8052ad689531ebc48dcd71785a1f3341413e
MAP09_05 installed/archive Task SHA-256:
ae54470791006b6e302f00f225ac92657c3e428d0d8f8088854770faca1bc2b5
MAP09_06 inbox/installed/archive SHA-256:
ebea8d166311b9fee8df2c89cb41be9ff6b438a475e0242c1b3fd019daa7a951
Installed/archive bytes: 13819/13819, byte-identical
Status before open: 215 rows; COMPLETE 112 / CURRENT 0 / LOCKED 103
Status after open:  215 rows; COMPLETE 112 / CURRENT 1 / LOCKED 102
Root unapplied candidates after apply: 0
Staged paths before task execution: 0
```

No pre-existing unrelated worktree change overlapped the allowlist. No unrelated path was modified or staged.

The compiled live predecessor baselines matched their approved Results exactly before implementation:

```text
MAP09_01 pass count/digest:
10 / 90a2614f9a95c29f1546f350190010524672d4b4aa2d1ad1dfe7dbd431be50d5
MAP09_02 layer count/digest:
7 / d0888c865cbdcc0884dc8abab9fac92900addd662a12a1ec30dc930f9cf4c94e
MAP09_03 MicroPattern fixture digest:
42c88cdb30154f098593d0e3be65063111613612fe5e9e1b9b11f2d9f1297a3d
MAP09_04 TerrainCluster fixture digest:
e8c3228e6f9df360637023d68e9c243cb70df4122342a3251740054bbcc8f9f1
MAP09_05 Activity fixture digest:
7a5357320d8e2634ab9416ae7c90fb80a83c1c7f799a8df7689ba37b8a0903bc
MAP09_05 Event fixture digest:
722a490f054e5bfc5a75ac81e03eee4978cd7f51d34e01fa1e01818c9d4ce904
```

## Implemented File Inventory

New SpecialRegion Runtime C# and matching metas:

```text
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionContract.cs
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionContract.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionValidation.cs
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionValidation.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionCanonicalDigest.cs
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionCanonicalDigest.cs.meta
```

New Baking Runtime C# and matching metas:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorCanvasContract.cs
Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorCanvasContract.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorCanvasValidation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorCanvasValidation.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/Baking/BakingCanonicalDigest.cs
Assets/_Game/Map/Runtime/WorldGeneration/Baking/BakingCanonicalDigest.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedSliceContract.cs
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedSliceContract.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedSliceValidation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedSliceValidation.cs.meta
```

New focused EditMode tests and matching metas:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SpecialRegions/SpecialRegionContractTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SpecialRegions/SpecialRegionContractTests.cs.meta
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/SectorCanvasAndSliceContractTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/SectorCanvasAndSliceContractTests.cs.meta
```

Task/protocol documents:

```text
MapDesign/MCP/TASKS/MAP09_06_IMPLEMENT_SPECIAL_CANVAS_AND_SLICE_CONTRACTS.md
MapDesign/MCP_ARCHIVE/MAP09_06_IMPLEMENT_SPECIAL_CANVAS_AND_SLICE_CONTRACTS.md
MapDesign/MCP/REPORTS/MAP09_06_IMPLEMENT_SPECIAL_CANVAS_AND_SLICE_CONTRACTS_RESULT.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

## SpecialRegion Contract and Digest

`SpecialRegionContract` carries a typed region ID/kind, the existing reservation ID, normalized connected `1x1`, `2x1`, or `1x2` sector offsets, immutable fixed-shell cells, replaceable slots, explicit Entry/Return ports, and deterministic persistence bindings. The validator requires exact reservation footprint and compatible reservation kind, in-footprint 48x32 local coordinates, unique fixed/slot positions, no shell overlap, published `AccessClass`, exact slot-to-port and reservation-anchor agreement, at least one Entry and Return, required Reward keys, and stable region/scope/slot key derivation. It rejects both local duplicate keys and cross-region collisions without owning mutable runtime state.

Compiled live fixture:

```text
ID: SR_LIVE_BASELINE
Reservation: RES_SPECIAL_VILLAGE
Footprint: 2x1 / 2 sector offsets
FixedShell/Slots/Ports/Persistence: 1/3/2/2
Validation: PASS
SHA-256: 73fd2085ecf65057f25eec8b2ff4fceb1a4d1a1a0eadfd60b7595071936a7066
```

The digest includes identity, reservation, footprint, every fixed-shell and slot coordinate, port side/AccessClass, and persistence scope/key/initial meaning. Display text, locale, time, input order, file/reflection order, RNG, and Unity state are excluded.

## SectorCanvas Contract and Digest

`SectorCanvasContract` stores exactly `48x32 = 1536` explicit cells in canonical `y * 48 + x` order. Every cell has the exact Solid/Background/Surface/Affordance/Material/Hazard/Marker/Owner resolved values, explicit-empty representation, stable source refs with kind/pass order/owned layers/protection, and optional SpecialRegion persistence provenance. The validator rejects dimension, gap, overlap, invalid payload/source, duplicate owner, and protected-owner loss. A Validated stamp is accepted only when its pass catalog, layer catalog, source artifact set, resolved-cell, and ruleset digests are complete and its catalog/source/cell digests match the live contract. It validates stamps but never issues a PASS stamp.

Compiled live fixture:

```text
ID: CANVAS_LIVE_BASELINE
Dimensions/Cells: 48x32 / 1536
Validation: PASS
Canvas SHA-256: 7c26d2d12d418a6f203e793bffd49216c003a6c0fc6f6f2bea06d210d3bded0c
Stamp SHA-256: cb909e6a1fc2a14bbd4e8b5a6ab103b5926e0428f535163f428f8dafda38a9f6
```

## Generated Slice Contract and Digest

`GeneratedSliceSet` stores exactly 16 canonical slice coordinates, each with 96 explicit local cells and Canvas ID/digest/stamp provenance. The validator derives `canvasX = sliceX * 12 + localX` and `canvasY = sliceY * 8 + localY`, proves all 1536 cells are covered exactly once, and compares every resolved value, boundary source, persistence key, and provenance record to the validated Canvas. Unvalidated sources, rotation/mirror/resample/padding, slice-time mutation, missing/duplicate coordinates, provenance loss, and Authoring-source promotion all fail publication.

Compiled live fixture:

```text
Slices/Cells: 16 / 96 each / 1536 exact once
Transform: None
Boundary role: GeneratedOutput
Validation: PASS
SHA-256: 2066f58b09e3ac8ef0118c54e243008f54bcefe1e3bb032fa67dbe5d25156368
```

The generated model does not reference or replace MAP07 `MicrochunkDefinition`; it is a validated storage projection only.

## Focused Validation and Regression Policy

Final authoritative focused execution:

| Selection | Discovered | Executed | Passed | Failed | Skipped | Inconclusive |
|---|---:|---:|---:|---:|---:|---:|
| MAP09_06 | 39 | 39 | 39 | 0 | 0 | 0 |

```text
MAP09_06 focused: 39 discovered / 39 executed / 39 passed / 0 failed / 0 skipped
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY TEST SELECTIONS: 0
```

Focused tests, compilation, Console, every predecessor live digest, Authoring manifest, asmdef hashes, GUID inventory, and change-boundary checks all passed without drift. Per the active user instruction and Task no-regression policy, no MAP00-MAP09_05 category and no legacy `19347` selection was executed. The zero regression selection is deliberate policy compliance and is not replaced by historical results as current PASS evidence.

## Unity and Static Gates

```text
Unity version: 6000.3.8f1
Compile errors: 0
Console errors: 0
Relevant warnings: 0
Focused EditMode: 39 discovered / 39 executed / 39 passed / 0 failed / 0 skipped / 0 inconclusive
PlayMode: NOT REQUIRED
Scene/Prefab changes: NONE

Runtime C#/matching meta: 8/8
EditMode test C#/matching meta: 2/2
All Assets meta/GUID: 3876/3876
Duplicate GUID groups: 0
Forbidden production symbol hits: 0
Authoring CSV/matching meta: 50/50
Authoring manifest: f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb
Generated CSV: 0
Runtime asmdef SHA-256: 1df0ed8fcdf1f7c668b12f29da71272f3133f64a1965fcf70237a6e5f0b34fef
EditMode asmdef SHA-256: 2d05060be8f0d602b97483b1e0bda2acadc9fb134aa0433d284626c5513d225a
Authoring/Generated task changes: 0/0
Scene/Prefab/Settings/Packages/asmdef task changes: 0
Existing MAP00-MAP09_05 modifications: 0
Other V2 root changes: 0
Unapplied root inbox candidates: 0
Unrelated staged/included: 0
Diff-check errors: 0
```

The final Console was empty. Production scope contains no RNG, file I/O, Unity lifecycle, solver/composer/tile validator/slicer/writer/streaming/save implementation, forbidden legacy symbol, or new authority enum. Existing folder metas and both assembly definitions remained byte-unchanged.

## Change Scope and Out-of-Scope Findings

All implementation/test changes are new files under the two approved Runtime roots and the corresponding two focused test roots. Existing MAP00-MAP09_05 production/test files, other V2 roots, Authoring/Generated data, Scene, Prefab, Settings, Packages, and asmdefs were not changed.

```text
OUT_OF_SCOPE_FINDING: NONE
MAP09_07 started: NO
Git push: NOT PERFORMED
```

## Atomic Commit Handoff

Only the installed/archived Task, eight Runtime C#/meta pairs, two focused test C#/meta pairs, this Result, and finalized Status are eligible for the atomic commit.

```text
Subject: MAP09_06: implement special canvas and slice contracts
Commit: SELF
Push: NOT PERFORMED
```
