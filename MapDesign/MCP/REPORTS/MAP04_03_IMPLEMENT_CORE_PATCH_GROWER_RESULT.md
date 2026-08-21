# MAP04_03 — Implement Core Patch Grower Result

STATUS: PASS

## TASK

- Task: `MAP04_03_IMPLEMENT_CORE_PATCH_GROWER`
- Implemented deterministic atomic P02 Core mandatory-buffer/minimum growth from the immutable MAP04_02 initialization publication.
- The grower accepts no MAP03 capacity witness, RNG, root/pass, clock, file, Registry singleton, Unity object, or future MAP04 dependency.
- Satellite/Intrusion seeds, remaining-world fill, altitude/noise cost, cleanup, serialization, validation, overlay, and `PASS_BIOME` integration remain out of scope.

## PATCH APPLY

- Inbox patch: `MCP_INBOX/MAP04_03_IMPLEMENT_CORE_PATCH_GROWER`
- Manifest SHA-256: `bba3969f607b538021aa876b988291cf9948ddd334f02ef026c881cb548f65e4`
- Master payload/destination SHA-256: `e1e33bbc209ff8682a688acc6d7e75cccef995aa9a4fe7deab2cfb150e03afae`
- Status payload/destination SHA-256: `117483fec65ec3277baacebc3ef4af33369ab46ab8629e6f8cdf24e237dfa20c`
- Task payload/destination SHA-256: `85c70ea26f11e12db1c08d8dc81969d05d3e938e8aaffc46a6a46a89c9af7cf0`
- `.APPLIED` SHA-256: `4e3a16d7ce2f85071c541503e9272615814883dbb9a09ad4728efef5440ffa0a`
- Preconditions matched: Current Task `NONE`, MAP04_02 `COMPLETE`, MAP04_03 `LOCKED`, prior exact `STATUS: PASS`, and prior Result SHA-256 `d10a2350723ebe2d47b26a89f59d0c605eb242fa9d1fe432e811bb39ee608ee8`.
- After the manifest copy, Current Task was exact `TASKS/MAP04_03_IMPLEMENT_CORE_PATCH_GROWER.md`, with `48 COMPLETE / 1 CURRENT / 156 LOCKED`; MAP04_04 remained `LOCKED`.

## ALLOWLIST

- The mandatory MCP documents, Master/status, current Task, and MAP04_02 Result were read in the required order.
- Only Task-allowed model/definition/test bodies and permitted path/hash/meta inventories were inspected.
- Writes were confined to the exact 6 new Runtime files, 1 new EditMode fixture, their 7 matching metas, this Result, the manifest-declared Phase A MCP copies/marker, and Phase C status finalization.
- Installed Authoring CSV bodies, unrelated production/test bodies, existing MAP04_01/02 files, asmdef/asmref, Legacy generator bodies, Scene/Prefab YAML, and future Task bodies were not modified.

## CREATED

Runtime production:

- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchGrowthError.cs` — SHA-256 `62e6e98c267795f7f73d971954d96a67ffeff540c94f64bc947ea2fc2ca9e926`; meta GUID `2250b78272ffe524ea4f297bd35ef5f3`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchGrowthRecord.cs` — SHA-256 `d1a97ef7c6ae8f1bcf5ad70cd05177ef03b20ceb50c62c08fb16ca5da2ddcf52`; meta GUID `e86d4bb05e07b3f489bc44fe0979680b`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchGrowthDiagnostics.cs` — SHA-256 `1221646caafe4b12f853bac0eb92b5e5db374e17bdb0cc8786c2e4feec8f64e1`; meta GUID `9ca4b7e4d450cc34c94e7fa6ecc9c9e1`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchGrowthPublication.cs` — SHA-256 `737696a36adf4f39629dc8e9c0d4f94828aaddaeb8928bc44b4dcdf63682195e`; meta GUID `a531af79ae6ed4144a00288849da505c`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchGrowthResult.cs` — SHA-256 `4b6f9797df580afd19813c6262b50ac66fb3e99ec99f5c9aa35f73e062d5bbf6`; meta GUID `fb6f42511a04df949bc20e3b4286a31b`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/CorePatchGrower.cs` — SHA-256 `33d1f07d71589623969358f959400090e6caf40d49115e8f635f36fef7f0b06b`; meta GUID `4a362534c73663d488da2135640806d5`

Focused test:

- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/CorePatchGrowerTests.cs` — SHA-256 `7247de6358aab5fe78ebbe676f0c5d0307f078d2ec2af2db680f39ee8cfb51c0`; meta GUID `b2c361e731d52764bb4ed899c5bf4a1f`

All 7 metas use `fileFormatVersion: 2`, a valid unique non-zero lowercase 32-hex GUID, and `MonoImporter`.

## MODIFIED

- Existing Assets files modified: `0`
- Existing production/tests/meta/asmdef/asmref modified: `0`
- Existing MAP04_01/02 production/test artifacts modified: `0`
- Authoring CSV/meta modified: `0`
- Scene/Prefab/ProjectSettings/Packages modified: `0`

## PREEXISTING_IDENTICAL

- Existing approved Runtime/Test directories and assemblies were reused unchanged.
- Authoring remains `50 CSV / 50 CSV.meta`; no Authoring file is newer than the Task `.APPLIED` marker. The prior approved Authoring path/hash baselines remain `3387d5f899db12cb2cd73b1a0fa67b5a2d431fa63d063cc728d87a50e42f084c` and `299d68f9afb66e1cd0fc17d6cc30ba6be88181c6d5c522b548a18a12130bf664`.
- Accepted `_Legacy` Editor folder metas remain `6/6`, with none newer than the Task marker.

## GROWTH CONTRACT EVIDENCE

- Exact source-ID canonical record order is Forge, Cassia, Yeast, Meteor, with exact Core patch/biome/rule mappings preserved from MAP04_02.
- Mandatory buffer is independently recomputed from every source-footprint cell using the union of cardinal Manhattan-distance rings; it does not use origin-only, bounding-box, Chebyshev, wrap, clamp, diagonal, entry-exterior, or MAP03 witness logic.
- Buffer vectors cover radius `0/1/2`, interior, edge truncation, and a two-cell footprint union. The two-cell radius-1 vector is exact `[15,16,27,28,29,30,41,42]`, excluding diagonal/bounding-box cell `14`.
- `CanTouchWorldEdge == false` returns atomic `BufferOutsideWorld`; `true` records the unique outside-theoretical count and grows from the in-world buffer.
- Every P01 reservation other than the matching source footprint is an absolute blocker. Start, Boss, Village, and other-Core blocker cases return retry-required errors; unreserved entry-exterior sectors remain eligible.
- Cross-Core mandatory overlap emits stable pair diagnostics for both sources and never applies last-write-wins.
- Target is exact `max(MandatoryBuffer.Count, MinSectorCount)` and never `MaxSectorCount`; `TargetExceedsMaximum` is structural invalid input.
- Supplemental proposals use `(FootprintDistance, ExposedPerimeterDelta, SectorIndex)` per patch and global `(distance, perimeter, SourceReservationId, SectorIndex)` order. Each deficient patch claims at most one cell per round, then recomputes its frontier.
- A zero-success deficient round returns `InsufficientUnreservedCapacity`, retry true, no publication, empty records, zero observable added counts, and rollback input conservation.
- Successful output creates a new immutable partial `BiomePatchSnapshot`; source initialization and source P01 snapshot reference identities, world seed, Core patch IDs, seeds, and bindings are preserved.
- Patch membership and ownership are bidirectionally consistent; SecondaryBiomeId remains empty; orphan/cross-overlap/reservation-intrusion counts are zero.
- Runtime source scan found `0` forbidden Unity/RNG/time/filesystem/pass/root/capacity-witness dependencies, `0` forbidden language declarations, and `0` private static mutable fields.

## STARTER EVIDENCE

- Core patches / seeds / bindings: `4 / 4 / 4`
- Initial assigned / unassigned: `4 / 165`
- Per-patch target and final count: `5 / 5 / 5 / 5`
- Mandatory added / supplemental added / total added: `16 / 0 / 16`
- Final assigned / unassigned: `20 / 149`
- Reservation intrusion / cross-patch overlap / RNG draws: `0 / 0 / 0`

## MAP03 WITNESS INDEPENDENCE

- `CorePatchGrower.Grow` parameters are exactly initialization publication, biome definitions, and patch-rule definitions.
- `CoreCapacityApproval`, `CoreCapacityFloodWitness`, or witness sector lists are not accepted, queried, copied, unioned, or used as tie-break input.
- Growth is recomputed only from source footprint, the authoritative 169-row P01 reservation table, current P02 ownership, and typed rules.

## VERIFICATION

Actually executed reduced profile:

- New `CorePatchGrowerTests`: `127/127 PASS`, failed `0`, skipped `0`; job `d9e9f091cbd64756b8d168243a67af74`.
- Required regressions: `443/443 PASS`, failed `0`, skipped `0`; job `2871732b92a0426da7dc04f1cbf90f7f`.
  - `CorePatchSeedInitializerTests`: `121/121`
  - `BiomePatchModelsTests`: `107/107`
  - `CoreCapacityFloodCheckerTests`: `215/215`
- Actually executed required total: `570/570 PASS`, failed `0`, skipped `0`.
- Game.Map targeted discovery arithmetic: approved prior `4077` + new focused `127` = `4204`, satisfying `>=4197`; not executed as a targeted suite under the user-directed reduced profile.
- Full EditMode current discovery: `4273`, satisfying `>=4266`; not executed as a full suite under the same profile.
- Unity `6000.3.8f1` final forced compile completed with compile errors `0`, Console errors `0`, and relevant new warnings `0`.
- One MCP transport warning (`WebSocket is not initialised`) is infrastructure-only and unrelated to project code.
- PlayMode tests: not required and not run.

## ASSET / META / CHANGE SCOPE

- Baseline Assets meta: `3086`
- Final Assets meta / valid GUID rows: `3093 / 3093`
- Duplicate GUID groups: `0`
- New Runtime C#: `6`
- New test C#: `1`
- New matching meta: `7`
- Exact Assets changes newer than `.APPLIED`: `14`, consisting only of the 7 new C# files and their 7 matching metas.
- Existing Assets modifications: `0`
- Unexpected Assets changes: `0`
- Authoring CSV/meta: `50/50` unchanged
- Accepted Legacy Editor folder meta: `6/6` unchanged

## OUT_OF_SCOPE_FINDINGS

- Initial selection job `42d7a9fd09cc40aeac7f2fb2c31eb459` ran before the new fixture entered Unity discovery and returned `0 tests`; it is not counted as PASS.
- Fixture-development jobs `b18728e9d03747a399fb99d1c794ebda` and `af410219f1194a4ea4814aa7f64c7d42` exposed fixture/setup assertions and are not counted as PASS. The corrected final fixture job is the separately recorded `127/127 PASS` job.
- Per the user's reduced-verification instruction, targeted `4204` and full `4273` suites were not executed; their discovery/arithmetic evidence is kept separate from executed PASS counts.

## DONE CONDITIONS

- Exact Core mandatory-buffer/minimum growth: PASS
- P01 reservation hard blockers and cross-Core conflicts: PASS
- Deterministic frontier ordering and connected supplement: PASS
- Atomic completed/invalid/retry result semantics: PASS
- Starter `4 -> 20 assigned`, `165 -> 149 unassigned`, mandatory `+16`, supplemental `0`, RNG `0`: PASS
- Source/input/output immutability and reference identity: PASS
- MAP03 capacity witness non-dependency: PASS
- Required focused minimum `>=120`: `127 PASS`
- Required regressions: `443 PASS`
- Actually executed required total `>=563`: `570 PASS`
- Compile / Console errors / relevant new warnings: `0 / 0 / 0`
- Asset/meta/GUID/Authoring/change-scope gates: PASS
- Existing approved artifacts unchanged: PASS

## NEXT

- Finalize only `MAP04_03_IMPLEMENT_CORE_PATCH_GROWER` as `COMPLETE`.
- Set `Current Task: NONE`.
- Keep `MAP04_04_IMPLEMENT_SATELLITE_SEED_PLACER: LOCKED`.
- Do not automatically start the next Task.

Recommended commit message (not executed): `feat(map): grow core biome patches to mandatory size`
