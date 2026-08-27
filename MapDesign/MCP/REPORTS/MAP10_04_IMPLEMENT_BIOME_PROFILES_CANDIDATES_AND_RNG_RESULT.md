# MAP10_04 - Implement Biome Profiles, Candidates, and RNG Result

```text
TASK: MAP10_04_IMPLEMENT_BIOME_PROFILES_CANDIDATES_AND_RNG
STATUS: PASS
MAP10_04: COMPLETE ELIGIBLE
MAP10_05_IMPLEMENT_REPETITION_SIGNATURE_AND_LOCAL_CLEANUP: LOCKED / DO NOT START
```

## Responsibility and Added Functions

| Field | Implemented responsibility |
|---|---|
| Task responsibility | Defines the exact four-biome MicroPattern profile boundary, transformed-plan feature evidence, stable eligible candidate index, and deterministic weighted selection boundary. |
| Added functions | Adds immutable profile/catalog/validation types, uncalibrated integer density and silhouette summaries, candidate source/key/index/rejection types, checked ticket resolution, request/decision/error/result types, and canonical profile/index/selection digests. |
| Inputs consumed | Consumes validated MAP10_01 definitions/catalog semantics, successful MAP10_02 application plans, and the MAP02_02 deterministic RNG authority through the registered `RNG_SECTOR_RECIPE` definition. |
| Outputs produced | Publishes an immutable exact-four profile catalog, biome-specific canonical candidate index with rejection evidence, and immutable deterministic selection decisions with stream/draw/ticket evidence. |
| Explicit non-ownership | Does not author starter content, implement repetition signature/hash, local cleanup or physical validation, invoke the renderer, mutate SectorCanvas/Tilemap, or change RNG/CSV/plan/renderer authority. |
| Downstream consumers | MAP10_05 through MAP10_08 and the MAP11 cluster pattern renderer may consume these immutable indexes and decisions; no downstream task was started. |

## Predecessor, Status, and Patch Apply

The only immediate Inbox candidate passed the `single_task_v1` identity, predecessor, exact-hash, destination-collision, Status, Master membership, encoding, and empty-staging gates before mutation.

```text
Preflight HEAD: 1e4a53d6b92dc0e81bb6b081f5edb01c9d6c24e8
MAP10_03 Result SHA-256:
3890aa4087093ac8078ccd64038b2156d9177b0ac55066b3b5ff29e1cc5aa427
MAP10_03 installed/archive Task SHA-256:
9138b1fdda796e324db5b977ee4b90373a13454e8fd66e55769b5a024552e39a
MAP10_04 inbox/installed/archive SHA-256:
6a864e561b2426679dbb82ecb2d6c83fa27c818a223ebff812e3eba9f44051bf
Installed/archive byte-identical: YES

Status before open: 215 rows; COMPLETE 118 / CURRENT 0 / LOCKED 97
Status after open:  215 rows; COMPLETE 118 / CURRENT 1 / LOCKED 96
Root unapplied candidates after apply: 0
Staged paths before Task execution: 0
```

Read-only authorities remained exact without selecting a prior category:

```text
MAP10_01 catalog fixture digest:
1b2524bf8af6be7ae3b2d03134096a4efdf8f856ea500863ec5dcd26114f0c35
Full 52-file Authoring manifest:
b49b6ebc4a65c26302ff08deb9100dbf727200e16b3588092b7cd53f63d214ba
generation_passes.csv exact binding count:
PASS_MICRO_SOLVE -> RNG_SECTOR_RECIPE = 1
```

## Implemented File Inventory

Runtime files and Unity-generated matching metas:

```text
Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/MicroPatternBiomeProfiles.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/MicroPatternCandidates.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/MicroPatternSelection.cs(.meta)
```

Focused Runtime EditMode test and Unity-generated matching meta:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/MicroPatterns/MicroPatternBiomeSelectionTests.cs(.meta)
```

The remaining task-owned files are the installed Task, byte-identical Archive Task, this Result, and finalized Status.

## Profile and Feature Evidence

The built-in catalog contains exactly one profile for each canonical typed biome in approved order:

| Biome | Stable motif metadata | Density policy |
|---|---|---|
| MoonCrater | Bowl, BrokenSlope, RockShelf | `Uncalibrated`, raw `0..16 / 16` evidence only |
| CassiaRoot | HollowPocket, RootArch, VerticalTunnel | `Uncalibrated`, raw `0..16 / 16` evidence only |
| AbandonedMill | BeamOverhang, BrokenPillar, OrthogonalCarve | `Uncalibrated`, raw `0..16 / 16` evidence only |
| MoonDough | BounceCup, SoftPocket, StickyShelf | `Uncalibrated`, raw `0..16 / 16` evidence only |

No numeric biome threshold or random multiplier was introduced. Validation accumulates and stable-sorts missing/duplicate/unknown biome, motif, density-policy, and silhouette-class errors, and publishes no partial catalog/digest.

Feature summaries recompute the validated transformed definition and combine it with the matching successful application plan. They preserve AddSolid cells, CarveAir cells, geometry-write numerator over exact denominator 16, all non-NoChange cell-layer writes, protected-overlap cells, and forced-NoChange removed writes. `NoGeometry`, `AddOnly`, `CarveOnly`, and `Mixed` are exact computed classes; mirror-invariant silhouette hashing remains unimplemented.

## Candidate Index Evidence

Eligibility requires the validated immutable definition, requested biome allowlist membership, allowed transform, matching successful plan identity/source digest/transform, lowercase 64-hex plan digest, and a valid profile feature summary. Pattern IDs are never parsed for biome eligibility.

The stable key is `pattern ID + transform token + application-plan digest`. Eligible candidates are ordinal-key sorted independently of source enumeration. Every duplicate-key member is excluded rather than selecting a first/last winner. Invalid sources remain in stable rejection evidence and never enter the published index. Definition integer weights `1..10000` are preserved exactly.

The lowercase SHA-256 index digest includes ruleset version, catalog/profile/biome evidence, canonical candidate keys, source definition digests, plan digests, feature digests, and exact weights. It excludes input/file/dictionary/reflection order, locale, time, display fallback, and object hash.

## RNG and Selection Evidence

The selector uses only the existing `WorldGenerationRngStreams.SectorRecipeStreamId` constant and `DeterministicRngStreamFactory`:

```text
Registered stream: RNG_SECTOR_RECIPE
Reset scope: SECTOR
Scope identity: existing SectorCoord invariant x,y
Attempt: non-negative caller ordinal
Session: fresh stream per batch
```

All request IDs, uniqueness, indexes, digests, canonical order, candidate weights, and checked totals are validated before constructing a stream. Invalid/empty input therefore publishes no decision/digest, reports `StreamCreated = false`, and consumes zero draws. Valid requests are processed by request ID ordinal; every request calls existing unbiased `NextInt(totalWeight)` once and resolves its half-open integer ticket by cumulative exact weight.

Each decision records InitialState, DrawCount before/after, total weight, ticket, chosen ordinal/key, and index digest. The batch digest includes ruleset, canonical unsigned world seed, registered stream/scope/sector/attempt, initial/final draw evidence, canonical requests/index digests, and decisions. Same inputs are exact; seed, sector, attempt, and index one-field mutations change the digest. A separately created route-stream instance remains untouched.

## Focused Validation and Regression Policy

Only category `MAP10_04` was selected.

| Selection | Discovered | Executed | Passed | Failed | Skipped | Inconclusive |
|---|---:|---:|---:|---:|---:|---:|
| MAP10_04 | 13 | 13 | 13 | 0 | 0 | 0 |

```text
MAP10_04 focused: 13 discovered / 13 executed / 13 passed / 0 failed / 0 skipped
REGRESSION TRIGGER DETECTED: YES
Owner: MAP10_04 task-owned MicroPatternBiomeProfiles.cs
Reason: initial compile found one local-name collision and two transformed-cell coordinate property typos
Minimum scope: repair the new file, recompile, and rerun MAP10_04 focused only
Final compile/focused state: PASS
PRIOR TASK TEST SELECTIONS: 0
LEGACY TEST SELECTIONS: 0
PlayMode selections: 0
```

The trigger was fully owned by the new MAP10_04 file and did not indicate baseline drift. No prior or legacy selection was necessary or executed.

Focused coverage proves exact four profiles/motifs, uncalibrated integer density, four silhouettes, transformed/protected feature counts, eligibility and accumulated rejections, canonical/reversed index behavior, duplicate exclusion, exact weights/ticket boundaries, registered SECTOR RNG reuse, repeatability and one-field sensitivity, atomic no-draw rejection, other-stream independence, collection immutability, and forbidden dependency/side-effect exclusion.

## Unity and Static Gates

```text
Unity version: 6000.3.8f1
Final compile / Console error / relevant warning: 0 / 0 / 0
Focused EditMode: 13 / 13 PASS; fail 0; skip 0; inconclusive 0

Runtime C#/matching meta: 3/3
Focused test C#/matching meta: 1/1
All Assets meta/GUID after approved additions: 3902/3902
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

Existing MAP00-MAP10_03 production/test/CSV/meta modifications: 0
Other roots/Editor/asmdef/Scene/Prefab/Settings/Packages changes: 0
Unapplied root inbox candidates: 0
Duplicate GUID / unapplied candidate / diff-check errors: 0 / 0 / 0
Unrelated staged/included paths: 0
```

## Change Scope and Out-of-Scope Findings

Every implementation/test change is a new file in the Task allowlist. Existing C#, test, CSV/meta, RNG authority/registry/pass binding, catalog/plan/renderer, other V2 roots, Generated content, asmdef, Scene, Prefab, Settings, and Packages files remain unchanged.

```text
OUT_OF_SCOPE_FINDING: NONE
MAP10_05 started: NO
Git push: NOT PERFORMED
```

## Atomic Commit Handoff

Only the installed/archived MAP10_04 Task, three Runtime C#/meta pairs, one focused test C#/meta pair, this Result, and finalized Status are eligible for the atomic commit.

```text
Subject: MAP10_04: implement biome pattern selection
Commit: SELF
Push: NOT PERFORMED
```
