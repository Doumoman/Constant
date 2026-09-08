TASK: RMAP10_SMALL_RUN
STATUS: PASS

# RMAP10 small seeded run implementation report

## User-facing implementation report

`Tools/MoonPalace/RMAP10/Small Run Generator` now accepts an integer Seed,
Width, Height, and `PortGalleryV1` recipe, then writes the dedicated
`MoonPalaceSmallRun_RMAP10` scene. Supported requests are deliberately narrow:
`36x24` and `48x24` only (positive 12x8 multiples). Unsupported or unaligned
requests are rejected with a reason and do not reuse a prior success. The
representative saved scene is Seed `1107`, `36x24`, with a Production Player at
Start and an observed `RMAP10_Exit`; move with A/D or arrows. RMAP11 remains
responsible for the 500-pattern pool and is not implemented here.

## Preconditions actually verified

- Branch/starting HEAD: `main` / `cc9120409229178b7b79fa6552f0c0123882ec4c`.
- User-provided full-file inbox SHA-256 matched actual bytes:
  `1981435b4790eb84514d04dfe3842864a2be24390d965065f4278250682dc77a`.
- RMAP09 Result contains exactly one matching Task and PASS marker; SHA-256 is
  `b7710e2062ae70adfba9dc138181531e6a1ceba8598ad5a959b15c3c3da2ef07`.
- RMAP09 installed Task and archive are byte-identical, each SHA-256
  `e9819a2dd3962ab32f1c3872fda23251d8be280198b9007f59a6e47729d16384`;
  actual Finalize commit is `cc9120409229178b7b79fa6552f0c0123882ec4c`.
- Before opening, status was `230 COMPLETE / 0 CURRENT / 10 LOCKED`; Phase A
  installed/archive-copied RMAP10 byte-for-byte and changed only Current
  `NONE -> RMAP10_SMALL_RUN` plus RMAP10 `LOCKED -> CURRENT`.

## Responsibility and files

| Path | Change | Responsibility / non-ownership |
| --- | --- | --- |
| `Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/RmapSmallRunHarness.cs` | NEW | Bounded Seed/size/recipe request validation, 3x3 or 4x3 chunk plan, six RMAP07 selections per chunk, Base/overlay provenance, global Port coordinates, digest, and explicit failures. It owns neither saves nor the 500-pool. |
| `Assets/_Game/Live/Editor/RMAP10/RmapSmallRunSceneBuilder.cs` | NEW | Rebuilds only the dedicated scene, bakes generated Solid/ONE_WAY/Ladder layers, binds existing Player/Camera/Exit, and emits deterministic RMAP10 evidence. |
| `Assets/_Game/Live/Editor/RMAP10/RmapSmallRunGeneratorWindow.cs` | NEW | Small-run Seed/Width/Height/Recipe entry and visible Generate/Regenerate outcome; no game UI or load/save surface. |
| `Assets/_Game/Live/Editor/RMAP10/Game.Character.Live.Rmap10.Editor.asmdef` | NEW | Minimal Editor-only `Game.Character.Live` + `Game.Map.Runtime` references. |
| `Assets/_Game/Map/Scenes/MoonPalace/RMAP10/MoonPalaceSmallRun_RMAP10.unity` | NEW generated scene | Current RMAP10 Base TilemapCollider2D, one-way effector Tilemap, ladder trigger overlay, Production Player, continuous camera, Exit, and visible settings label. |
| `Assets/_Game/Tests/EditMode/Map/RMAP10/RmapSmallRunHarnessTests.cs` | NEW | Focused E03/E06/E07 generation, Port, reproducibility, and rejection checks. |
| `Assets/_Game/Tests/PlayMode/Character/RMAP10/RmapSmallRunPlayModeTests.cs` | NEW | Saved physical Bake inspection and input-driven Production Player Start-to-Exit proof. |
| `MapDesign/MCP/GENERATED/RMAP10/*` | NEW evidence | Three-case Seed data, chunk/selection/port/base/overlay CSV, manifest, and filtered Unity reports. |

Matching Unity `.meta` files are generated assets for the new RMAP10-owned
directories/files only.

## Reuse and adaptation decisions

- KEEP/read-only: `RmapPatternCatalog` supplies the 48-candidate source and
  transformed 16-cell selections. The harness records six selections per
  12x8 chunk rather than changing catalog data.
- KEEP/read-only: `RmapPortCatalog` supplies Type0/1/2/3/4 semantics, all edge
  coordinates, directional flow, breakable Type0 condition, and the shared
  traversal-profile digest `12415531…c2d68`.
- KEEP/read-only: `RmapComposer` supplies the verified RMAP09 Type3 six-slot
  composition and separate seven-cell ladder overlay. No fixed fixture is
  relabelled as a whole run; it occupies the generated Type3 chunk only.
- KEEP/read-only: `CharacterLivePlayer`, movement/input adapter,
  `CharacterLiveMapRunExit`, climb surface, one-way platform, terrain tiles,
  and continuous camera remain their existing contracts. The final Base uses a
  direct `TilemapCollider2D` path; the one-way Tilemap uses its existing
  `PlatformEffector2D` convention.
- NOT ADAPTED: `MoonPalaceSeededRunGenerator` and its publisher require the
  historical audited `65,536-mask / 500-candidate` pipeline and preview
  ownership. Consuming or changing them would start RMAP11-scale work.

## Requirement evidence

- E03: `rmap10_ports.csv` records global L/R/U/D coordinates and flow for
  Type1 through Type4. The representative plan contains Type1/2/3/4 plus both
  active single-entrance and sealed-breakable Type0 forms. The EditMode port
  test checks every declared opening against generated Base cells, and records
  the T2 drop/T3 climb/T4 vertical directions without promoting the RMAP08
  context-unknown hang link to a required pass.
- E04: no monsters, economy, boss, puzzle, tool/break system, multiplayer,
  save/load, or world-content generation was added. The sealed Type0 access is
  described as `BREAKABLE_TOOL_REQUIRED`, not treated as mandatory access.
- E05: the saved scene has `RMAP10_BaseTerrain` TilemapCollider2D, a separate
  `RMAP10_OneWayOverlay` TilemapCollider2D + PlatformEffector2D, and a
  trigger-only `RMAP10_LadderOverlay`. PlayMode creates the actual existing
  prefab with its `0.4 x 0.8` CapsuleCollider, feeds its existing input
  adapter from Start `(1,1)`, and observes that same Player enter
  `RMAP10_Exit` at `(34,1)`; no position overwrite is used during traversal.
- E06: `rmap10_seed_evidence.csv` records the declared three cases: `1107 /
  36x24`, `2203 / 48x24`, and `3301 / 36x24`, all `PortGalleryV1`. Digests are
  respectively `eb1c…6869`, `6f62…a10a`, and `abb2…4d20`; repeated `1107 /
  36x24` is exactly reproducible. Only the first case receives PlayMode,
  exactly as requested; the other two are generation/connection evidence.
- E07: the RMAP10 window exposes the input and rejection state; the builder
  replaces only the isolated RMAP10 scene root and writes the current manifest
  and CSVs. Scene labels show Seed, size, recipe, controls, and digest.

## Validation

- Unity compile/refresh and representative-scene generation:
  `StarNight.Character.Live.Rmap10.Editor.RmapSmallRunSceneBuilder.BuildRepresentativeScene`.
- EditMode filtered `RmapSmallRunHarnessTests`: 3 total, 3 passed, 0 failed
  (`rmap10_editmode_results.xml`).
- PlayMode filtered `RmapSmallRunPlayModeTests`: 2 total, 2 passed, 0 failed
  (`rmap10_playmode_results.xml`). This includes the real input-driven Player
  collision and Exit observation.
- Broad/full/unfiltered regression, legacy `19347`, Player build, 20-Seed
  sweep, and historical RUN generator regression were not run because the
  installed Task explicitly excludes them. No unverified Seed or gameplay
  behaviour is claimed.

## Out-of-scope findings and RMAP11 bindings

- RMAP11_POOL500 is still `LOCKED`; no pool expansion, world planner, save
  state, full bake, or RMAP11 execution occurred.
- RMAP11 binding: `RmapSmallRunHarness.cs` is the existing consumer boundary
  for `RmapPatternCatalog` selections and `PortGalleryV1`; the proposed future
  selector path is
  `Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/RmapPatternPool500.cs`.
  It can replace only the harness candidate source after its own task opens;
  current recipe/Port/scene and validation boundaries remain RMAP10-owned.

## Final evidence and follow-up

- RMAP10 installed Task and archive are byte-identical, each SHA-256
  `1981435b4790eb84514d04dfe3842864a2be24390d965065f4278250682dc77a`.
- This Result is written before Finalize/commit. At write time Current Task is
  `RMAP10_SMALL_RUN`; Phase C must only close this row and Current Task.
- COMMIT: pending Phase D atomic commit from starting HEAD `cc912040…ec4c`.

NEXT: RMAP11_POOL500 LOCKED / NOT STARTED
