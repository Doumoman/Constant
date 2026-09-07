# RUN06 - Tune Generated Run Shapes and Remove Bad Variants Result

TASK: RUN06_TUNE_GENERATED_RUN_SHAPES_AND_REMOVE_BAD_VARIANTS
STATUS: PASS

# User-Facing Implementation Report

Unity now has **Tools/MoonPalace/Run Curation**. It creates the bounded RUN05-derived 36-course batch, scores every generated course with visible-shape rules, retains the bad courses with measured reason codes, and rebuilds an isolated curation gallery Scene.

Accepted and rejected courses are intentionally displayed together: green-framed accepted runs remain valid RUN04-style preview candidates, while red-framed rejected runs are kept as evidence instead of repaired, deleted, or hidden. The batch found the WIDE recipe's nine consecutive same-row main frames to be a real corridor-shape problem; all 12 WIDE seed results are retained as `ROUTE_TOO_STRAIGHT` rejections. No generator recipe field was changed because the bounded evidence is best preserved for comparison rather than silently altering or repairing it.

The gallery marks `TALL_LOOP_RUN / 1924737067` as the recommended accepted active preview for a future RUN07 handoff. RUN07 itself, production player physics, full-world generation, production art, and Build Settings changes remain out of scope.

# Responsibility and Added Scripts

| Script or file | Added or changed responsibility | Explicit non-ownership |
| --- | --- | --- |
| MoonPalaceRunQualityRule.cs | Required rule IDs, severity and complete measured finding records | Terrain repair or seed approval lists |
| MoonPalaceRunQualityProfile.cs | Deterministic per-recipe visible-shape thresholds | RUN05 topology replacement |
| MoonPalaceRunQualityAnalyzer.cs | Measures route, room, branch, connector, split and reachability quality without tile mutation | Fallback carve or silent repair |
| MoonPalaceRunCurationBatch.cs | Bounded 3×12 generation and analysis batch | Reroll-until-valid behavior |
| MoonPalaceCuratedRunSet.cs | Retained accepted/rejected records, recommendation score and curation digest | Removing rejected evidence |
| MoonPalaceRunCurationWindow.cs | Required menu, batch/gallery actions and next-record inspection | Test/build execution or Build Settings |
| MoonPalaceCuratedRunGallerySceneBuilder.cs | RUN06-only Scene, color encoding and deterministic artifact publication | Existing Scene/Prefab changes |
| MoonPalaceRunShapeCurationTests.cs | RUN06-only 20-case EditMode proof | PlayMode or unfiltered tests |
| RUN06 CSV/JSON/Scene artifacts | Batch, rule, gallery, rejection and recommendation evidence | Full-world artifacts |

# Quality Profile Summary

All required rule IDs are applied to every one of the 36 records: `ROUTE_TOO_STRAIGHT`, `ROOM_TOO_EMPTY`, `ROOM_TOO_NOISY`, `BRANCH_TOO_SHORT`, `BRANCH_TOO_DEEP`, `CONNECTOR_CROWDING`, `VERTICAL_VARIATION_LOW`, `BACKTRACK_SHAPE_BAD`, `OPEN_ISLAND_REQUIRED`, and `REPAIR_POLICY_VIOLATION`.

Each finding records rule ID, `info|warning|reject` severity, measured value, threshold, affected IDs when available, and a human-readable reason. Quality profile digest: `d54dfe48e15ae83ca079fad34867ad594b21698aefdbd7b75ce1b9fbb95ca889`.

# Curation Batch Summary

- Editor menu item: `Tools/MoonPalace/Run Curation`
- Batch recipe count: `3`
- Batch seed range: `1924737067` through `1924737078`
- Total generated count: `36` (3 recipes × 12 seeds)
- Accepted count: `24`
- Rejected count: `12`
- Curation digest: `9bd5eb3156f812e16915b3541154059954ccc7e80f349a7fe423937cdd9abea0`
- Same batch inputs reproduce the same curation digest and recommendation.

# Accepted Run Summary

All 24 accepted records pass tile-level start-to-exit BFS, room/connector validation, reciprocal/open gates, and zero fallback/silent-repair/rotation policy checks. The gallery visibly shows 9 accepted courses in green frames.

Recommended RUN07 accepted recipe/seed: `TALL_LOOP_RUN / 1924737067`.

Recommended RUN07 course digest: `1420689ff69d7b8144514e8100f7942a09e0cc451272f021ebfbeebd6c0a98e6`.

# Rejected Run Summary

All 12 rejected records remain in the curation set with all findings and no terrain mutation. Top rejection reasons: `ROUTE_TOO_STRAIGHT=12`.

The concrete reason is measured, not seed-hardcoded: `WIDE_BRANCH_RUN` has 9 consecutive same-row main-room frames while its profile maximum is 6. Six retained rejected examples are shown in the Scene with red frames, primary reason labels, and red rule-hotspot markers.

# Recipe Tuning Summary

RUN05 recipe fields were not changed. The deterministic tuning delta is explicitly empty: the batch exposed a repeatable wide-corridor shape issue, and RUN06 retains those generated examples as rejected design evidence instead of silently modifying the RUN05 generator, masks, or placements. Outputs changed from rejected to accepted through tuning: `0`.

# Unity Scene Output Summary

- Scene path: `Assets/_Game/Map/Scenes/MoonPalace/RUN06/MoonPalaceCuratedRunGallery_RUN06.unity`
- Scene root: `MoonPalace_CuratedRunGallery_RUN06`
- Direct visual groups: `AcceptedGallery`, `RejectedGallery`, `QualityLegend`, `RuleOverlay`, `SeedLabels`, `Metadata`
- Accepted frame / rejected frame / warning marker / reject marker: green / red / yellow / red
- Main route / branch / split-rejoin / connector gate: green / blue / gold / magenta
- `TALL_LOOP_RUN / 1924737067` is visibly labeled `RUN07 RECOMMENDED` in AcceptedGallery.

# Artifact and Digest Summary

Exactly 14 deterministic artifacts were published: 8 JSON files in `MapDesign/MCP/GENERATED/RUN06/` and 6 CSV files in `Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN06/`. Tests verify stable output content/digests, UTF-8 without BOM, LF-only endings, invariant serialization, stable order, and one final LF.

Scene manifest digest: `dcd7810b4e5baa53ba7450b108baf169f06efe3fa9cf11182dd4a0958358dd58`.

# Focused Validation Summary

- Source RUN05 Result SHA-256: `1ac68e5f06c5837f95ef572b5f65c62a59653cc87c20c23feeac6ffb827fe831` — `STATUS: PASS` verified.
- Unity focused EditMode filter: `category / RUN06`
- Discovered / executed / passed / failed / skipped / inconclusive: `20 / 20 / 20 / 0 / 0 / 0`
- Unity console errors after final focused execution: `0`

# Explicitly Not Selected

- legacy 19347 selections: `0`
- automated PlayMode selections: `0`
- full/unfiltered selections: `0`
- Build Settings mutations: `0`
- Player build: not run
- Existing Scene/Prefab mutation outside RUN06: `0`
- RUN07 files/runs: `0 / 0`
- Git push: not performed

# Final Status Evidence

PASS is supported by RUN05 prerequisite verification, 36 deterministic direct-4x4 generated courses over all required seeds/recipes, 24 valid accepted records, 12 visible measured rejections, a marked recommended accepted course, required menu and isolated Scene groups, deterministic artifacts, zero forbidden repair counts, console error count 0, and the focused Unity EditMode result `20/20 PASS`.

Commit hash: `a409c7343c74d4220dc76e7476982296e8ef7cb4`
