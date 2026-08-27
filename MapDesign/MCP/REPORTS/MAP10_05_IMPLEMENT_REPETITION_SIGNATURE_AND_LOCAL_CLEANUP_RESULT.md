# MAP10_05 - Implement Repetition Signature and Local Cleanup Result

```text
TASK: MAP10_05_IMPLEMENT_REPETITION_SIGNATURE_AND_LOCAL_CLEANUP
STATUS: PASS
MAP10_05: COMPLETE ELIGIBLE
MAP10_06_AUTHOR_STARTER_24_MICROPATTERNS: LOCKED / DO NOT START
```

## Responsibility and Added Functions

| Field | Implemented responsibility |
|---|---|
| Task responsibility | Defines mirror-invariant effective-geometry signatures, exact third-repeat source filtering, and exact snapshot-based local cleanup boundaries. |
| Added functions | Adds immutable signature/builder/digest, accepted-history/context/exclusion/guard/result/error, cleanup cell/snapshot/issue/proposal/cell-delta/delta/result/error, proposal resolver, exact rule detector, and cleanup canonical digest types. |
| Inputs consumed | Consumes successful MAP10_02 application plans, the MAP10_03 effective `Solid` cell-state contract represented by cleanup snapshot cells, and MAP10_04 candidate sources/index/selector authority. |
| Outputs produced | Publishes canonical silhouette evidence, canonical allowed sources and exclusions, explicit no-candidate errors, stable cleanup issues/proposals, and immutable changed-owned-cell deltas. |
| Explicit non-ownership | Does not author pattern content, draw/reroll RNG, tune biome density, infer global physics/reachability/TileValidation, invoke the renderer, or mutate SectorCanvas/Tilemap/Scene/Prefab/SO state. |
| Downstream consumers | MAP10_06 through MAP10_08, the MAP11 pattern renderer, and MAP16/MAP19 validators may consume the contracts; no downstream task was started. |

## Predecessor, Status, and Patch Apply

The only immediate Inbox candidate passed the `single_task_v1` identity, predecessor, exact-hash, destination-collision, Status, Master membership, encoding, and empty-staging gates before mutation.

```text
Preflight HEAD: e1d50c0241fdbbe7772318ae00fa6c53f72b0ce8
MAP10_04 Result SHA-256:
c5179d833cf74c0db26b8c729600f2bd8ecd8a099722c3b99d814eb9d54feb6d
MAP10_04 installed/archive Task SHA-256:
6a864e561b2426679dbb82ecb2d6c83fa27c818a223ebff812e3eba9f44051bf
MAP10_05 inbox/installed/archive SHA-256:
a11c6a03294b2aea017793747a1dfdb7b6ac2d38ff4ce487394e2246e2753e7a
Installed/archive byte-identical: YES

Status before open: 215 rows; COMPLETE 119 / CURRENT 0 / LOCKED 96
Status after open:  215 rows; COMPLETE 119 / CURRENT 1 / LOCKED 95
Root unapplied candidates after apply: 0
Staged paths before Task execution: 0
```

Read-only baseline authorities remained exact without selecting a prior category:

```text
Full 52-file Authoring manifest:
b49b6ebc4a65c26302ff08deb9100dbf727200e16b3588092b7cd53f63d214ba
Authoring modifications from HEAD: 0
Generated CSV: 0
```

## Implemented File Inventory

Runtime files and Unity-generated matching metas:

```text
Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/MicroPatternSilhouetteSignature.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/MicroPatternRepetitionGuard.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/MicroPatternLocalCleanup.cs(.meta)
```

Focused Runtime EditMode test and Unity-generated matching meta:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/MicroPatterns/MicroPatternRepetitionAndCleanupTests.cs(.meta)
```

The remaining task-owned files are the installed Task, byte-identical Archive Task, this Result, and finalized Status.

## Silhouette Signature Evidence

The signature builder accepts only a successful MAP10_02 application plan with exact 4x4 prepared coverage and a canonical lowercase plan digest. It reads only the protected-post effective Geometry instruction from each local cell. `AddSolid` and `CarveAir` become separate 16-bit masks at `y*4+x`; `NoChange` contributes no bit, including writes removed by protection.

Both masks are transformed together under `R0`, `MirrorX`, `MirrorY`, and `R180`. The smallest unsigned packed `(AddMask, CarveMask)` pair is selected, with enumeration order implementing the exact tie order `R0 < MirrorX < MirrorY < R180`. The lowercase SHA-256 digest records only `MAP10_05_SILHOUETTE_V1` and the canonical pair. Payloads, other layers, weight, biome, source ID/digest, RNG, and display state are excluded. Mirror-equivalent geometry therefore matches; non-equivalent geometry remains distinct; no-geometry plans publish the explicit zero signature.

## Third-Repeat Guard Evidence

Accepted history items carry caller-owned non-negative placement sequence, exact placement ID, exact `MicroPatternId`, and signature evidence. The context defensively copies and canonicalizes history by sequence then placement ID, rejecting invalid or duplicate placements.

Candidate sources are validated against matching successful application-plan identity, transformed plan, digest, and computed signature, then ordinal-canonicalized before filtering. Only when the latest two accepted items have the same exact ordinal Pattern ID are all sources of that ID excluded. Transform and silhouette do not weaken exact-ID exclusion; a different ID with the same silhouette remains allowed; mismatched latest IDs exclude nothing.

Filtering runs before the unchanged MAP10_04 candidate index and selector. The focused integration passes only allowed sources into those authorities, selects without reroll or discarded draw, and leaves the existing RNG contract untouched. An all-excluded input publishes `NoCandidateAfterThirdRepeatGuard`, no allowed source/digest, and performs no fallback or RNG draw.

## Local Cleanup Evidence

The immutable snapshot records target coordinate, owned versus read-only one-cell halo, `Solid`, explicit protected state, and canonical protected provenance. Validation accumulates missing/duplicate/invalid coordinate, ownership, halo, and protection errors before rule detection.

Every rule reads the same original snapshot and may propose only an owned target:

```text
SolidSpeck:     Solid + cardinal four Air -> Air
AirPinhole:     Air + cardinal four Solid -> Solid
HeadSnag:       Solid; Up/UpLeft/UpRight Solid; Left/Right/Down Air -> Air
BoxedBottomPit: Air; Down/Left/Right Solid; Up Air; UpLeft/UpRight Solid -> Solid
```

Missing required neighbors produce stable `InsufficientNeighborhood` issues and skip only that rule. Protected proposal targets produce `ProtectedWriteBlocked` with canonical provenance and zero mutation while other valid proposals remain eligible. Same-target/same-value proposals coalesce and union rule/neighborhood evidence. Same-target/different-value proposals publish `ConflictingCleanupProposal` plus `AtomicCleanupRejected`, no delta, and no digest.

Successful deltas contain changed owned cells only in `(y,x)` order with before/after Solid state, unioned rule and original-neighborhood evidence, and protection evidence. The source snapshot is never mutated and no result is fed back into another rule, so no cascade pass occurs. The `MAP10_05_CLEANUP_V1` digest includes canonical snapshot Solid/protection state, issues, proposals, and deltas while excluding time, display, object/file/enumeration order, RNG, and Unity lifecycle state.

## Focused Validation and Regression Policy

Only category `MAP10_05` was selected.

| Selection | Discovered | Executed | Passed | Failed | Skipped | Inconclusive |
|---|---:|---:|---:|---:|---:|---:|
| MAP10_05 final | 15 | 15 | 15 | 0 | 0 | 0 |

```text
MAP10_05 focused: 15 discovered / 15 executed / 15 passed / 0 failed / 0 skipped
REGRESSION TRIGGER DETECTED: YES
Owner: MAP10_05 task-owned focused test fixture
Reason: the initial broader-pit near-miss fixture accidentally matched the separate exact AirPinhole rule
Minimum scope: change that fixture's Down cell to Air, recompile, and rerun MAP10_05 focused only
Initial focused state: 15 discovered / 15 executed / 14 passed / 1 failed / 0 skipped
Final compile/focused state: PASS
PRIOR TASK TEST SELECTIONS: 0
LEGACY TEST SELECTIONS: 0
PlayMode selections: 0
```

The trigger was fully owned by the new MAP10_05 test input and did not indicate a production defect or baseline drift. No prior or legacy selection was relevant or executed.

Focused coverage proves exact mask/canonical-transform behavior, mirror equivalence and geometry distinction, metadata independence, protection-post geometry, exact-ID repetition rules, different-ID same-signature allowance, mismatch behavior, guard/index/selector order, all-excluded no-draw behavior, every exact cleanup rule and near-miss, protected/missing-halo evidence, no cascade, coalesce/conflict atomicity, immutable collections, reversed enumeration stability, structural atomic rejection, and forbidden side-effect exclusion.

## Unity and Static Gates

```text
Unity version: 6000.3.8f1
Final compile / Console error / relevant warning: 0 / 0 / 0
Focused EditMode: 15 / 15 PASS; fail 0; skip 0; inconclusive 0

Runtime C#/matching meta: 3/3
Focused test C#/matching meta: 1/1
All Assets meta/GUID after approved additions: 3906/3906
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

Existing MAP00-MAP10_04 production/test/CSV/meta modifications: 0
Other roots/Editor/asmdef/Scene/Prefab/Settings/Packages changes: 0
Unapplied root inbox candidates: 0
Duplicate GUID / unapplied candidate / diff-check errors: 0 / 0 / 0
Unrelated staged/included paths: 0
```

## Change Scope and Out-of-Scope Findings

Every implementation/test change is a new file in the Task allowlist. Existing C#, test, CSV/meta, MAP10_02 plan/transform/protection authority, MAP10_03 renderer, MAP10_04 profile/index/RNG authority, other V2 roots, Generated content, asmdef, Scene, Prefab, Settings, and Packages files remain unchanged.

```text
OUT_OF_SCOPE_FINDING: NONE
MAP10_06 started: NO
Git push: NOT PERFORMED
```

## Atomic Commit Handoff

Only the installed/archived MAP10_05 Task, three Runtime C#/meta pairs, one focused test C#/meta pair, this Result, and finalized Status are eligible for the atomic commit.

```text
Subject: MAP10_05: add pattern repetition and cleanup
Commit: SELF
Push: NOT PERFORMED
```
