TASK: MAP03_01_IMPLEMENT_SITE_RESERVATION_MODELS
STATUS: PASS

## SUMMARY

- Implemented the immutable, compile-time typed P01 site-reservation model boundary over the approved 13 x 13 / 169-sector P00 grid.
- Added exactly eight Runtime production C# files, one focused EditMode fixture, and nine matching `.cs.meta` files.
- Kept candidate enumeration, transform application, placement solving, distance/cost, backtracking, Core capacity flood, village selection, `PASS_SITE`, serialization, and file I/O out of scope.
- Existing production, tests, asmdef/asmref, Authoring data, Scene/Prefab, Package, and ProjectSettings assets were not modified.

## PATCH APPLY

- Applied inbox patch: `MAP03_01_IMPLEMENT_SITE_RESERVATION_MODELS`, version `1.0`.
- Applied exact manifest copy operations: Master replace, Status replace, Current Task create.
- Payload/destination SHA-256 matched for all `3/3` files.
  - Master: `6171234C524F8CD146B38CE4475EBBEB24E33C4615213E843D76EB4FF5EE5252`.
  - Status: `801618B14CA705D9874B7B4896550D833D3D7DA0BDE74B61EA42E12D7206F580`.
  - Task: `77334BEEBBAC7AEA207D82D49F7F6A90059AAE82072CAC367635A53108CFF44C`.
- Marker created: `MapDesign/MCP_INBOX/MAP03_01_IMPLEMENT_SITE_RESERVATION_MODELS/.APPLIED`.
- Post-apply state was `35 COMPLETE / 1 CURRENT / 169 LOCKED` with only MAP03_01 CURRENT.

## READ

- Read the MCP entrypoint, global locked/work/CSV/Unity/change/patch/finalize rules, Master backlog, Status, this Task, and MAP02_08 Result.
- Read only the Current Task allowlisted domain/data/generation APIs, allowlisted tests/asmdefs, matching meta, and permitted path-only inventories/audits.
- The complete optional Map Package document tree was not installed; this Task's frozen contracts were used as the authoritative fallback without substitute GDD or Legacy generator search.
- MAP03_02 or later Task bodies were not read.

## MASTER BACKLOG CHECK

- Master rows / unique task IDs: `205 / 205`.
- MAP00: `10/10 COMPLETE`; MAP01: `17/17 COMPLETE`; MAP02: `8/8 COMPLETE`.
- MAP03_01 was the exact single Current Task; MAP03_02 and all later tasks remained LOCKED.
- Auto-start remained `NO`.

## MAP02 EXIT GATE CHECK

- `STATUS: PASS`: confirmed.
- `MAP02 EXIT: APPROVED`: confirmed.
- `MAP03 ENTRY: ELIGIBLE FOR SEPARATE PATCH`: confirmed.
- The prior `MAP03_01: LOCKED / DO NOT START` state was respected until this separate patch was validated and applied.
- Approved baseline confirmed: MAP02 phase focused `667/667`, targeted `1514/1514`, full EditMode `1554/1554`, visual `12/12`, compile/Console `0/0`, Assets meta `2989`, duplicate GUID groups `0`.

## CREATED

Runtime production C# and matching meta:

- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationId.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationEnums.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteFootprint.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteEntryAnchor.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreBiomeSeed.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorReservation.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservation.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSnapshot.cs`
- Matching `.cs.meta`: `8/8`.

Focused test and matching meta:

- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationModelsTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationModelsTests.cs.meta`

Documentation:

- This Result file.

## MODIFIED

- Existing Assets production/test/meta/asmdef/asmref files: `0`.
- Authoring/generated CSV/meta: `0`.
- Scene/Prefab/Package/ProjectSettings: `0`.
- Status was not modified during Task execution.

## PREEXISTING_IDENTICAL

- None. All exact eighteen permitted Asset destinations were absent before implementation.

## RESERVATION ID

- `SiteReservationId` is a `public readonly struct` implementing `IEquatable` and `IComparable`.
- Exact ASCII grammar `^[A-Z0-9_]+$`, ordinal equality/order, deterministic FNV-style hash, `TryCreate`, and invalid default semantics are implemented.
- Empty, whitespace, lowercase, hyphen, punctuation, and non-ASCII IDs are rejected.
- No automatic ID generation, seed prefix, order prefix, or random suffix was introduced.

## ENUM AND TOKENS

- Exact ordered kinds: Start, CoreResource, Forge, Boss, Village.
- Exact transforms: R0, MirrorX, MirrorY, R180.
- Exact sides: L, R, U, D.
- Exact case-sensitive parse/format tokens, undefined/numeric rejection, opposite sides, and deltas are implemented.
- No local-coordinate transform application is present.

## FOOTPRINT

- `SiteFootprintCell` preserves final-oriented local coordinates, canonical IDs, and a copied read-only L/R/U/D-ordered unique side set.
- `SiteFootprint` validates dimensions/bounds/duplicates, permits sparse footprints, sorts by LocalY then LocalX, snapshots caller input, and provides stable lookup.
- It does not fill rectangles or reapply R0/Mirror/R180 transforms.

## ENTRY ANCHOR

- Valid reservation identity, canonical socket ID, occupied footprint sector, defined side, and exact non-empty unique route type domain `1|2|3` are enforced.
- Route types are copied and sorted ascending.
- Exterior lookup applies the exact side delta once and returns false outside the grid without clamp, wrap, or normalization.
- Compatibility, exterior occupancy, connection, and required-count validation remain later-task responsibilities.

## CORE BIOME SEED

- Immutable source reservation ID, biome ID, Core patch rule ID, world-valid seed sector, minimum count `>=1`, and buffer `>=0` are implemented.
- No flood-fill, capacity, altitude, edge-contact, or patch-growth calculation is present.

## SECTOR RESERVATION

- Unreserved entries have null ID/kind, local `-1/-1`, and empty role.
- Reserved entries require valid ID/kind, non-negative local coordinates, and a canonical non-empty role.
- Every entry enforces exact `WorldGridIndex` index/coordinate identity.
- No overlap resolution or mutation of `GeneratedWorldData`/`SectorCell` is present.

## SITE RESERVATION

- Occupied sectors are derived only from origin plus handed-in final-oriented footprint cells and must remain inside the world grid.
- Source/biome/order contracts, anchor self-ID/footprint membership, ordinal-unique socket IDs, socket ordering, and WorldGridIndex occupied ordering are enforced.
- Empty intermediate entry lists remain representable.
- Source collections are defensively copied and exposed through read-only interfaces.

## SITE RESERVATION SNAPSHOT

- Enforces non-empty reservations, unique reservation IDs/orders, exactly one Start, and exact 169 indexed sector entries.
- Enforces exact reservation footprint-to-sector ID/kind/local/role correspondence, rejects overlap/orphan/wrong/unreserved occupied state, and never selects a winner.
- Flattens entry anchors in `(ReservationId, EntrySocketId)` ordinal order.
- Core seed sources must exist, be CoreResource or Forge, and occur at most once per source reservation.
- Reservations, sectors, anchors, seeds, and lookups are immutable copied snapshots with deterministic order.
- Required site counts, distance, village distribution, entry exterior collision, and Core capacity are intentionally not approved here.

## DETERMINISM AND IMMUTABILITY

- Collection insertion order does not alter public ordering.
- Exact `en-US` and `tr-TR` cases passed.
- Caller list/array mutations do not change completed models or snapshots.
- Public setters/fields, exposed mutable collections, static mutable fields, lazy public enumeration, global current snapshot, RNG/time/filesystem/Unity object dependencies: `0`.

## TEST

- New `SiteReservationModelsTests`: `81/81 PASS`, failed `0`, skipped `0` (minimum 64 exceeded).
- MAP02 Runtime focused fixtures: `647/647 PASS`.
- MAP02 Editor overlay fixture: `20/20 PASS`.
- MAP02 phase focused aggregate: `667/667 PASS`, failed `0`, skipped `0`.
- `SpecialVillageDefinitionBuilderTests`: frozen required baseline `48/48 PASS`; current Runner actual fixture `57/57 PASS`.
- `BiomeBoundaryDefinitionBuilderTests`: frozen required baseline `36/36 PASS`; current Runner actual fixture `38/38 PASS`.
- `StaticDataRegistryBuilderTests`: `53/53 PASS`.
- `ContentVersionHashCalculatorTests`: `54/54 PASS`.
- Targeted `Game.Map.Tests.EditMode`: `1595/1595 PASS`, failed `0`, skipped `0` (`1514 + 81`).
- Full project EditMode: `1635/1635 PASS`, failed `0`, skipped `0` (`1554 + 81`).
- PlayMode: NOT RUN per Task scope.
- Visual: NOT APPLICABLE.

## UNITY

- Active instance: `Constant@ced6e0dfc4a31d45`.
- Unity: `6000.3.8f1`.
- Forced all-asset refresh and requested script compilation: PASS.
- Compile errors: `0`.
- Relevant/project-code warnings: `0`.
- One MCP WebSocket transport warning and Test Runner pre/post informational warnings occurred outside project code; after Console isolation the final error/warning read was `0/0`.
- Final editor state: idle, not playing, not compiling, no domain reload pending, ready for tools.
- Saved Scene/Prefab changes: NONE.

## ASSET META VALIDATION

- Authoring CSV/meta: `50/50`; files newer than task marker: `0/0`; approved combined baseline SHA-256 `3B0C89A05781B207361E34E64B1F9560969D29BBFD901B03C108FC496806968F` preserved.
- Accepted legacy Editor folder meta: `6/6`, all older than task marker and hash-preserved.
- New matching `.cs.meta`: `9/9` with exact `fileFormatVersion: 2` and unique lowercase 32-hex GUIDs.
- Final Assets meta: `2998`.
- Invalid/missing meta GUID rows: `0`.
- Duplicate GUID groups: `0`.

## CHANGE SCOPE

- Assets files newer than patch marker: exact `18`.
- Expected new Runtime production C#: `8`; matching meta: `8`.
- Expected new focused test C#: `1`; matching meta: `1`.
- Existing Assets modifications: `0`.
- Unexpected Assets changes: `0`.
- New directory/folder meta: `0`.
- asmdef/asmref, Authoring/generated CSV/meta, Scene/Prefab, Package, ProjectSettings changes: `0`.
- Git commit/push/branch/reset/rebase: not performed.

## PRODUCTION OWNERSHIP AUDIT

- Runtime namespace: exact `StarNight.Map.WorldGeneration.Generation`.
- Runtime assembly: existing `Game.Map.Runtime`; test assembly: existing `Game.Map.Tests.EditMode`.
- New asmdef/asmref: `0`.
- `UnityEditor`, `UnityEngine.Object`, ScriptableObject, MonoBehaviour, serialization callbacks, reflection factories, service locator, singleton/static mutable state: `0`.
- `PASS_SITE`, pass adapter/execution, candidate enumeration, RNG draw, coordinate transform application, solver/distance/cost/backtracking, capacity flood, village bucket/layout selection, generated serializer/file I/O/replay extension: `0`.
- Existing `GeneratedWorldData`, `SectorCell`, root, manifest, replay, overlay, Registry definitions: unchanged.

## OUT_OF_SCOPE_FINDINGS

- None.

## DONE CONDITIONS

- [x] Patch prerequisites, exact copy operations, hash identity, and Current Task transition PASS.
- [x] Typed reservation ID, enum/token, footprint, entry, Core seed, sector/site/snapshot contracts PASS.
- [x] Exact grid identity, overlap/orphan/mismatch, one-Start, core-seed ownership invariants PASS.
- [x] Deterministic ordinal/index ordering, culture invariance, defensive-copy, and public mutation-surface gates PASS.
- [x] No later-task algorithm/pass/serializer/visual work introduced.
- [x] Focused, MAP02 phase, data regressions, targeted, full EditMode, compile, Console, meta, GUID, and exact change-scope gates PASS.

## NEXT

- Finalize only `MAP03_01_IMPLEMENT_SITE_RESERVATION_MODELS` as COMPLETE and set Current Task to NONE.
- Keep `MAP03_02_ENUMERATE_START_AND_SITE_CANDIDATES` LOCKED.
- Await a separate future patch; do not auto-start the next Task.

## Recommended Commit

`feat(map): define immutable site reservation models`
