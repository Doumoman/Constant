# RUN05 - Create Seed Regenerate and Variant Selection Tool Result

TASK: RUN05_CREATE_SEED_REGENERATE_AND_VARIANT_SELECTION_TOOL
STATUS: PASS

# User-Facing Implementation Report

Unity now provides **Tools/MoonPalace/Run Preview Generator**. Choose an integer seed and one of three structural recipes, then use **Generate Preview Scene** to replace only the RUN05 isolated preview. The generated ActivePreview is the direct 4x4 MicroPattern course, with the RUN04-style marker controls: WASD/Arrow movement, blocked-step rejection, R reset, G route-ghost toggle, and room-frame/camera updates on connector crossing.

The deterministic generator uses the audited 500-item candidate pool for every pattern slot. It records an explicit failure instead of carving or silently repairing when bounded compatible-candidate selection cannot satisfy a route requirement. The fixed gallery shows four independently generated courses, so the selected preview is not presented as a hardcoded sample.

The scope remains an editor inspection tool: it does not add a production player controller, full-world generation, production art, Build Settings entries, or RUN06 work.

# Responsibility and Added Scripts

| Script or file | Added or changed responsibility | Explicit non-ownership |
| --- | --- | --- |
| MoonPalaceSeededRunRecipe.cs | Immutable direct-4x4 recipe contract and `pattern × 4` tile sizing | Sector/world generation |
| MoonPalaceSeededRunRecipeCatalog.cs | Stable WIDE/TALL/COMPACT recipe catalog | Recipe tuning beyond RUN05 |
| MoonPalaceSeededRunGenerator.cs | Deterministic candidate placement, topology, BFS route, digests, visible bounded failure | Fallback carve, rotation, full world |
| MoonPalaceSeededRunResult.cs | Course records and RUN04-style preview marker/camera/ghost adapters | Production player ownership |
| MoonPalaceSeededRunValidator.cs | Start/exit, room, connector, branch/split, repair-policy validation | Broad regression selection |
| MoonPalaceRunPreviewGeneratorWindow.cs | Required Unity menu, seed/recipe controls and generation/validation/gallery actions | Builds or Build Settings |
| MoonPalaceSeedRegenerateSceneBuilder.cs | RUN05-only Scene, gallery, debug tiles, deterministic CSV/JSON publication | Existing Scene/Prefab modification |
| MoonPalaceSeedRegenerateToolTests.cs | RUN05-only 18-case EditMode proof | PlayMode or unfiltered tests |
| RUN05 CSV/JSON/Scene artifacts | Inspectable seed, topology, placement, validation and digest evidence | MAP21 authoring rewrite |

# Tool Usage Summary

- Editor menu item: `Tools/MoonPalace/Run Preview Generator`
- Seed input: signed integer field; active seed `1924737067`
- Recipe dropdown: `WIDE_BRANCH_RUN`, `TALL_LOOP_RUN`, `COMPACT_SPLIT_RUN`
- Actions: `Generate Preview Scene`, `Validate Current`, `Rebuild Seed Gallery`
- Generation output: only `Assets/_Game/Map/Scenes/MoonPalace/RUN05/` and the RUN05 CSV/JSON directories.

# Recipe Catalog Summary

| Recipe | Pattern grid | Tile grid | Structural shape |
| --- | ---: | ---: | --- |
| WIDE_BRANCH_RUN | 96x24 | 384x96 | Wide horizontal route with three side branches and split/rejoin alternatives |
| TALL_LOOP_RUN | 72x28 | 288x112 | Vertical climb/drop with loop-like return and branches |
| COMPACT_SPLIT_RUN | 64x20 | 256x80 | Compact route with a clear split/rejoin |

Every tile grid is calculated from its pattern grid times four; no recipe has 48x32 as a primary dimension.

# Seed Determinism Summary

The active `WIDE_BRANCH_RUN` seed `1924737067` produced room-graph digest `243f1b03b092a98a4a5e80f64f4260d9a5d56ecfec3853dd08afafa7de1601ab`, placement digest `33427e09e11fc5aa5606405d18423fbe28bfb429053f6f34ec3961fcf2920eed`, and course digest `3409bf42ac231b3e261381c309bbbf19ee1a522180b4841c6ca1f2de4447afbd`.

Focused checks prove same recipe + same seed has identical room/placement/course/validation digests; same recipe + a different seed has a different placement digest; and a different recipe with the same seed has a different room-graph digest. Canonical ordering, invariant formatting, and no clock/editor-object-state input are used.

# Active Preview Generation Summary

- Active recipe id / seed: `WIDE_BRANCH_RUN` / `1924737067`
- Active pattern grid / tile grid: `96x24` / `384x96`
- Rooms / connectors: `12` / `11`
- Candidate pool / direct placements: `500` / `2304`
- Start / exit: `9:45` / `373:45`
- Validation digest: `3ca656f745d210703501164eccada3b9f0b3b43f07a95bbd1d034503faf8a26c`
- Start, exit, BFS, room bounds, reciprocal/open gates, main sequence, branch entries, and split/rejoin paths: PASS
- Fallback carve / silent repair / 90-degree rotation: `0 / 0 / 0`

# Seed Gallery Summary

The in-Scene `SeedGallery` contains four generated courses using fixed seeds `1924737067`, `1924737068`, `1924737069`, and `1924737070`. It displays WIDE, TALL, COMPACT, and a second WIDE comparison respectively, including generated terrain, route, room frames, connector gates, labels, and each placement digest. All four gallery courses pass tile-level BFS.

# Unity Scene Output Summary

- Scene path: `Assets/_Game/Map/Scenes/MoonPalace/RUN05/MoonPalaceSeedRegeneratePreview_RUN05.unity`
- Scene root: `MoonPalace_SeedRegeneratePreview_RUN05`
- Required direct child groups verified: `ActivePreview`, `SeedGallery`, `PreviewPlayer`, `PreviewCamera`, `RouteGhost`, `RoomFrames`, `ConnectorGates`, `Labels`, `Metadata`
- Active preview starts the marker and camera in the start room. The route ghost is visible and toggleable.

# Artifact and Digest Summary

The publication contains exactly 16 deterministic artifacts: 9 JSON files in `MapDesign/MCP/GENERATED/RUN05/` and 7 CSV files in `Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN05/`. Tests verify stable artifact content/digests, UTF-8 without BOM, LF-only line endings, culture-invariant serialization, stable ordering, and exactly one final LF.

# Focused Validation Summary

- Source RUN04 Result SHA-256: `fb632fce9d8905ed92443517e07e152ef3ba59752e7b2255ff4c0298a3302914` — `STATUS: PASS` verified.
- Unity focused EditMode filter: `category / RUN05`
- Discovered / executed / passed / failed / skipped / inconclusive: `18 / 18 / 18 / 0 / 0 / 0`
- Unity console errors after the final focused execution: `0`

# Explicitly Not Selected

- legacy 19347 selections: `0`
- automated PlayMode selections: `0`
- full/unfiltered selections: `0`
- Build Settings mutations: `0`
- Player build: not run
- Existing Scene/Prefab mutation outside RUN05: `0`
- RUN06 files/runs: `0 / 0`
- Git push: not performed

# Final Status Evidence

PASS is supported by the RUN04 prerequisite verification, required menu/window and isolated Scene presence, direct 500-candidate placement across the active run and gallery, deterministic recipe/seed checks, zero repair counts, artifact byte-format checks, and the focused Unity EditMode result `18/18 PASS`.

Commit hash: `7f0b7bbce71491281462d9ffca7829a91abb1cf3`
