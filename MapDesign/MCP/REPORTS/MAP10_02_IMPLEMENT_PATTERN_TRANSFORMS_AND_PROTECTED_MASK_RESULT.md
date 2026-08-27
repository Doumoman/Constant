# MAP10_02 - Implement Pattern Transforms and Protected Mask Result

```text
TASK: MAP10_02_IMPLEMENT_PATTERN_TRANSFORMS_AND_PROTECTED_MASK
STATUS: PASS
MAP10_02: COMPLETE ELIGIBLE
MAP10_03_IMPLEMENT_ORDERED_PATTERN_RENDERER: LOCKED / DO NOT START
```

## Responsibility and Added Functions

| Field | Implemented responsibility |
|---|---|
| Task responsibility | Projects a validated exact 4x4 `MicroPatternDefinition` through one approved transform and origin placement, intersects the result with supplied protected coordinates, and applies the definition's protected policy without mutating a canvas. |
| Added functions | Adds immutable transform Error/Result and `TransformedMicroPattern`, exact `MicroPatternTransformer`, placement and four-kind protected evidence, unioning `MicroPatternProtectedMaskBuilder`, immutable prepared-cell/hit/plan Error/Result, `MicroPatternApplicationPlanner`, and canonical application digest. |
| Inputs consumed | Consumes the validated MicroPattern definition/digest, existing transform and protected-policy authorities, `LocalTileCoord`, and caller-supplied RouteSpine, TraversalEnvelope, BoundaryProtectedOpen, and SpecialFixedEntry target-coordinate evidence with stable source IDs. |
| Outputs produced | Publishes either an immutable renderer-ready 16-cell application plan with canonical instructions, intersecting provenance, masking evidence, and digest, or stable rejection/error evidence with no partial plan or digest. |
| Explicit non-ownership | Does not calculate Spine/Envelope/ProtectedOpen/Special entry sources and does not implement renderer ordering, tile/canvas mutation, selector, RNG, repetition, cleanup, bounds policy, file I/O, or Unity lifecycle work. |
| Downstream consumers | MAP10_03 ordered renderer and the later MAP11 cluster pattern zone may consume the plan; neither downstream task was started. |

## Predecessor, Status, and Dirty Preflight

The only root inbox Markdown candidate passed `single_task_v1` identity, predecessor, exact-hash, destination-collision, Status, Master membership, encoding, and empty-staging gates before installation.

```text
Preflight HEAD: 2dafe8a44a4b0e28f2bbdf847ae022de9db9584d
MAP10_01 Result: STATUS PASS / MAP10_01 COMPLETE ELIGIBLE
MAP10_01 Result SHA-256:
326d24c70d490fe610e8b0abfaf4716d2ee06287f7ebb56330c0255a4a42dec8
MAP10_01 installed/archive Task SHA-256:
091750188c62b978bf4381c081610ac54be881a18c405ecd872c16e61eccfd34
MAP10_02 inbox/installed/archive SHA-256:
9eaa39d6063127b4d4bd19533b0b586aff29094807841ad16fc3320c076ad163
Installed/archive byte-identical: YES
Status before open: 215 rows; COMPLETE 116 / CURRENT 0 / LOCKED 99
Status after open:  215 rows; COMPLETE 116 / CURRENT 1 / LOCKED 98
Root unapplied candidates after apply: 0
Staged paths before task execution: 0
```

No unrelated path existed at preflight. No unrelated path was modified or staged. Read-only predecessor evidence remained exact without selecting a prior test category:

```text
MAP09_03 MicroPattern fixture digest:
42c88cdb30154f098593d0e3be65063111613612fe5e9e1b9b11f2d9f1297a3d
MAP10_01 authoring catalog fixture digest:
1b2524bf8af6be7ae3b2d03134096a4efdf8f856ea500863ec5dcd26114f0c35
Full 52-file Authoring manifest:
b49b6ebc4a65c26302ff08deb9100dbf727200e16b3588092b7cd53f63d214ba
```

The runtime consumes coordinates already published by the MAP09_04 Spine/`TraversalEnvelope.ProtectedTiles`, MAP08 protected-open evidence, and MAP09_06 fixed-entry contract. It duplicates none of their calculations.

## Implemented File Inventory

Runtime files and Unity-generated matching metas:

```text
Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/MicroPatternTransforms.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/MicroPatternProtectedMask.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/MicroPatternApplicationPlan.cs(.meta)
```

Focused Runtime EditMode test and Unity-generated matching meta:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/MicroPatterns/MicroPatternTransformAndProtectedMaskTests.cs(.meta)
```

The remaining task-owned files are the installed Task, byte-identical Archive Task, this Result, and finalized Status.

## Exact Transform and Placement Evidence

The transformer reuses `MicroPatternValidator`, requires the requested transform in the source allowlist, and accepts only:

```text
R0:      (x,y) -> (x,y)
MirrorX: (x,y) -> (3-x,y)
MirrorY: (x,y) -> (x,3-y)
R180:    (x,y) -> (3-x,3-y)
```

Every successful transform publishes 16 unique cells in canonical `y*4+x` order. Instruction layer/operation/payload semantics are defensively copied unchanged, source state remains unchanged, and digest material is input-order independent. Invalid sources, undefined enum values, and disallowed transforms publish no output or digest.

Placement uses `checked(origin + transformedLocal)` independently for x/y. Overflow is rejected atomically. No canvas-size or bounds authority is introduced.

## Protected Mask, Policy, and Digest Evidence

The exact protected kinds are `RouteSpine`, `TraversalEnvelope`, `BoundaryProtectedOpen`, and `SpecialFixedEntry`. Each evidence item has a target coordinate, defined kind, and ordinal stable source ID. The mask validates evidence, excludes non-intersecting coordinates, merges same-coordinate evidence, removes duplicate provenance, and stable-sorts coordinate/kind/source ID. Repeated or reversed input has the same result and digest.

`ForceNoChange` publishes all six canonical layers as `NoChange`/empty payload at a protected write coordinate while preserving unprotected operations. It records masked local/target coordinates, removed write count, and every source provenance without mutating source or transformed input.

`RejectCandidate` permits an all-`NoChange` protected overlap. Any protected write returns accumulated stable coordinate/provenance evidence, rejected hits, and no partial plan/digest.

A successful immutable plan contains source ID/digest, transform, origin, policy, 16 local/target cells, six final instructions per cell, intersecting mask provenance, masked-hit evidence, and a canonical lowercase SHA-256 digest. Timestamp, object hash, display text, file/reflection order, and RNG are excluded.

## Focused Validation and Regression Policy

Only category `MAP10_02` was selected.

| Selection | Discovered | Executed | Passed | Failed | Skipped | Inconclusive |
|---|---:|---:|---:|---:|---:|---:|
| MAP10_02 | 12 | 12 | 12 | 0 | 0 | 0 |

```text
MAP10_02 focused: 12 discovered / 12 executed / 12 passed / 0 failed / 0 skipped
REGRESSION TRIGGER DETECTED: YES (task-owned initial compiler CS0165)
Trigger owner/cause: MAP10_02 planner / short-circuit out variable lacked explicit initialization
Repair/minimum selection: one task-owned initialization / recompile / MAP10_02 focused only
Baseline drift: NONE
PRIOR TASK TEST SELECTIONS: 0
LEGACY TEST SELECTIONS: 0
PlayMode selections: 0
```

The focused cases cover asymmetric transforms, canonical coverage, semantic preservation/source immutability, allowlist/undefined rejection, placement/overflow, four-source union and provenance, transformed-coordinate intersection, both policies, stable multi-source evidence, atomic/read-only/order-independent publication, and side-effect exclusion.

## Unity and Static Gates

```text
Unity version: 6000.3.8f1
Final compile / Console error / relevant warning: 0 / 0 / 0
Focused EditMode: 12 / 12 PASS; fail 0; skip 0; inconclusive 0

Runtime C#/matching meta: 3/3
Focused test C#/matching meta: 1/1
All Assets meta/GUID after approved additions: 3894/3894
Duplicate GUID groups: 0

Authoring CSV/meta: 52/52 byte-unchanged
Full Authoring manifest:
b49b6ebc4a65c26302ff08deb9100dbf727200e16b3588092b7cd53f63d214ba
Generated CSV: 0

Runtime asmdef SHA-256:
1df0ed8fcdf1f7c668b12f29da71272f3133f64a1965fcf70237a6e5f0b34fef
Runtime EditMode asmdef SHA-256:
2d05060be8f0d602b97483b1e0bda2acadc9fb134aa0433d284626c5513d225a
Editor asmdef SHA-256:
11ef7812e0049b053c077d1cefa0b51bc4b60eea6609d046fe78d60d74197c17
Editor test asmdef SHA-256:
3cfa706a0462c146089ac42f7e2254f7bb42cdf175e85a58a7c1660c7dde76d2

Existing MAP00-MAP10_01 production/test/CSV/meta modifications: 0
Other roots/Editor/asmdef/Scene/Prefab/Settings/Packages changes: 0
Unapplied root inbox candidates: 0
Duplicate GUID / unapplied candidate / diff-check errors: 0 / 0 / 0
Unrelated staged/included paths: 0
```

## Change Scope and Out-of-Scope Findings

Every implementation/test change is a new file in the Task allowlist. Existing C#, test, CSV/meta, source authority, other V2 root, Generated content, asmdef, Scene, Prefab, Settings, and Packages files were unchanged.

```text
OUT_OF_SCOPE_FINDING: NONE
MAP10_03 started: NO
Git push: NOT PERFORMED
```

## Atomic Commit Handoff

Only the installed/archived MAP10_02 Task, three Runtime C#/meta pairs, one focused test C#/meta pair, this Result, and finalized Status are eligible for the atomic commit.

```text
Subject: MAP10_02: implement MicroPattern transforms and protected mask
Commit: SELF
Push: NOT PERFORMED
```
