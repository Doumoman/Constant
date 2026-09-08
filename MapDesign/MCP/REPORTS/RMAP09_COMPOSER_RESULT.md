TASK: RMAP09_COMPOSER
STATUS: PASS

# RMAP09 Composer implementation report

## User-facing outcome

An isolated RMAP09 Composer Lab now turns six real RMAP07 4x4 selections into one
12x8 Type3 composition. Its required L-to-U route is protected before selection,
the ladder is a separate overlay, and the saved scene verifies the route with the
real Player, TilemapCollider2D, and existing climb input. This is a bounded
composition fixture only; it is not RMAP10's seeded small run.

## Preconditions actually verified

- User-supplied inbox full-file SHA-256 and actual bytes matched:
  `e9819a2dd3962ab32f1c3872fda23251d8be280198b9007f59a6e47729d16384`.
- RMAP08 was independently complete: Result SHA-256
  `b14ae1ded7c4ee62846c03819fa3808188b4938626cda26083d010eb92db5204`,
  installed Task and archive SHA-256
  `e2aa3586d09ff87d18144a24c21100dd9168081d764bdb4b8b5ce72efd95cdac`,
  and committed Result history `083dc5a4713fd92faefd691028af8394dc0d8b6b`
  (`RMAP08 implement chunk types ports and directed connections`).
- Before the open transaction, Status was 229 COMPLETE / 0 CURRENT / 11 LOCKED;
  it is now 229 COMPLETE / 1 CURRENT / 10 LOCKED with RMAP09 only CURRENT.
- Installed RMAP09 Task and archive are byte-identical and both have SHA-256
  `e9819a2dd3962ab32f1c3872fda23251d8be280198b9007f59a6e47729d16384`.

## Responsibility and files

| Path | Change | Responsibility |
| --- | --- | --- |
| `Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/RmapComposer.cs` | NEW | Deterministic six-slot 3x2 selection, Type3 port/profile checks, protected walk/climb/support cells, independent ladder overlay data, bounded trace, and immutable failure output. |
| `Assets/_Game/Live/Editor/RMAP09/CharacterLiveComposerLabSceneBuilder.cs` | NEW | Builds the isolated physical lab, bakes only composed base solids to a TilemapCollider2D, creates the separate ladder trigger/tile overlay, writes ordered escaped CSV evidence, and validates each CSV envelope. |
| `Assets/_Game/Live/Editor/RMAP09/Game.Character.Live.Rmap09.Editor.asmdef` | NEW | Minimal editor assembly reference boundary for the existing Live and Map runtime assemblies. |
| `Assets/_Game/Map/Scenes/MoonPalace/RMAP09/MoonPalaceComposerLab_RMAP09.unity` | NEW generated scene | Saved RMAP09 Player, physical base terrain, separate ladder overlay, camera, labels, and six slot labels. |
| `Assets/_Game/Tests/EditMode/Map/RMAP09/RmapComposerTests.cs` | NEW | A22/A23/A24 selection, protection, base/overlay separation, rejection trace, and one-attempt bound checks. |
| `Assets/_Game/Tests/PlayMode/Character/RMAP09/RmapComposerLabPlayModeTests.cs` | NEW | P04/P19 physical scene checks with an instantiated Player driven through the existing input adapter. |
| `Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RMAP09/*.csv` | NEW builder-owned derived data | CSV views of the trace, six selections, 96 base cells, and seven overlay cells. |
| `MapDesign/MCP/GENERATED/RMAP09/*` | NEW evidence | Byte-identical CSV copies, fixture manifest, and filtered Unity test reports. |

## Reuse and adaptation decisions

- KEEP/read-only: `RmapPatternCatalog` supplies the real 4x4 source cells and
  transforms; no RMAP07 source or CSV was altered.
- KEEP/read-only: `RmapPortCatalog` supplies `T3_CLIMB`, `P_T3_L`, `P_T3_U`,
  the explicit required L-to-U link, and the shared traversal-profile digest.
- KEEP/read-only: `GeneratedTraversalProfileCatalog` supplies the verified
  digest. The composer does not create an independent movement profile.
- KEEP/read-only: the existing Player, movement driver, input adapter, climb
  surface, camera follow driver, and RMAP02 terrain assets are consumed in the
  isolated lab without changing their contracts.
- NOT ADAPTED: `GeneratedTileMovementGraphBuilder` requires the full validated
  SectorCanvas/GeneratedSlice graph contract, so using it for this 12x8 fixture
  would be a scope expansion. Historical MoonPalace RUN composers also remain
  untouched because their 500-pool/seeded-run responsibilities belong after this
  task.

## Requirement evidence

- P04: the accepted output is exactly 12x8/96 base cells and six 4x4 selections.
  The physical scene has `RMAP09_BaseTerrain` with TilemapCollider2D plus a
  static Rigidbody2D, and a real `RMAP09_Player` with the existing 0.4x0.8
  foot-pivot setup.
- P19: required route support is checked at x=0..5/y=0 and clearance at the
  walk and climb spine before a plan is accepted. No health, fall, grab, or
  recovery rule was weakened or added. The optional route remains recorded as
  `PROFILE_CONTEXT_UNKNOWN_NOT_PROMOTED_TO_REQUIRED_PASS`.
- A22: the accepted trace records six candidate IDs, transforms, final 16-cell
  values, and 3x2 slot origins. Required Type3 ports remain open and use RMAP08's
  profile digest.
- A23: the accepted base has 18 solid cells; its seven x=5/y=1..7 ladder cells
  remain AIR. The separate overlay owns the ladder tiles and trigger, so it does
  not convert SOLID or ONE_WAY base terrain.
- A24: `blocked-left-spine` records `REQUIRED_PORT_BLOCKED:P_T3_L`; then
  `blocked-up-port` records `REQUIRED_PORT_BLOCKED:P_T3_U`; only attempt 3
  (`accepted-type3-ladder`) succeeds. The one-attempt request stops after the
  first rejection rather than carving, changing a port, or continuing beyond
  its declared bound.

## Composition and physical evidence

- Source sequence: RMAP08 Type3/required ports/profile -> protected spine and
  support constraints -> six RMAP07 selections -> separate ladder overlay ->
  base Tilemap bake -> actual Player verification.
- `rmap09_attempt_trace.csv` has three ordered attempts; its fields containing
  coordinate commas are RFC4180 quoted. The builder reparses every emitted CSV
  for its exact header-column count and data-row count before writing it.
- Generated and authoring copies of the four CSV artifacts are byte-identical.
- Fixture manifest values: base digest
  `c186cda2026fb6d6081ca46404ee6502bb7caadcca2a73bd04a584caa78131f0`;
  composition digest
  `3f483c6f2e85fc0afcbc255666bf2c8f628bb25e053824d60a5bb7a92dd6ca5a`.

## Validation

- Unity 6000.3.8f1 batch builder completed successfully using
  `CharacterLiveComposerLabSceneBuilder.Build`.
- Filtered EditMode `StarNight.Map.Tests.EditMode.Rmap09.RmapComposerTests`:
  3 total, 3 passed, 0 failed. It covers A22/A23/A24 including the bounded
  one-attempt failure path.
- Filtered PlayMode
  `StarNight.Character.Tests.PlayMode.Rmap09.RmapComposerLabPlayModeTests`:
  2 total, 2 passed, 0 failed. It verifies the saved Player/TilemapCollider2D/
  ladder overlay and drives an instantiated real Player through the protected
  spine to the physical Type3 U-side clearance.
- Broad/unfiltered regressions, legacy full suites, Player builds, seed batches,
  and RMAP10 work were not run: the installed Task explicitly excludes them.

## Out of scope and next binding

- RMAP10_SMALL_RUN remains LOCKED and was not opened, generated, or executed.
  Its future binding inputs are the public RMAP09 request/result/attempt trace,
  source candidate/transform provenance, RMAP08 port/profile digest, and the
  physical base/overlay bake boundary. None is a claim that a variable-size run
  or real exit has been implemented.
- No existing scene, Player prefab, RMAP07/RMAP08 data, global source-of-truth
  rules, package, or unrelated dirty worktree path was changed.

## Final evidence and handoff

- Current Task is `RMAP09_COMPOSER`; RMAP08 is COMPLETE; RMAP10_SMALL_RUN is
  exactly LOCKED. The only status changes so far are the Phase A open fields.
- Task/archive byte equality remains verified. The required Result file now
  matches the installed task key and has no terminal conflict marker.
- COMMIT: pending the separate Phase C finalize and Phase D atomic commit. The
  final CLI report will record the resulting commit SHA and Result SHA-256.

NEXT: RMAP10_SMALL_RUN LOCKED / NOT STARTED
