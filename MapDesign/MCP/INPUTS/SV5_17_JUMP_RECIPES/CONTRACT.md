# SV5_17 — mixed jump-route recipes and #012 lineage

## Purpose

Turn the accepted SV5_16 local object graph into a small, explicit recipe
catalog. A recipe is a deterministic transformation of actual 1x1 support,
outline, route, and Grab data. It is not a vague tag, a 4x4 thumbnail, a Sector,
or a new whole-world planner.

The catalog contains exactly two production recipes: the accepted R0 geometry
and its horizontal mirror. Both preserve one-way completion and the same mixed
movement grammar: seven +1 JUMPs, one +2 JUMP_GRAB, and one level JUMP. Reverse
completion is not required.

The historical #012 before/draft-after illustration is also recorded byte-
deterministically, but remains reference-only. It must not be reported as the
same geometry as the SV5_16 production fixture or as Player-verified.

## C01 — immutable SV5_16 production source

- Consume `Sv5JumpOutlineGeometry.CreateCanonicalLocalFixture()` as the only
  production geometry source.
- Preserve its exact 24x32 local canvas, ten supports, 27 base cells, 17 outline
  cells, 44 final occupied cells, nine ordered links, one Grab edge, and fixture
  digest `0ac93bf0ddc1a203baa47517096516c2df98d67978d3c80131a13edcedd5df9c`.
- R0 output must reproduce every support, cell, link, Grab coordinate, collision
  type, route order, gap, rise, and required-route flag without mutation.
- Do not modify SV5_13 through SV5_16 source, tests, evidence, Task, Archive, or
  Result.

## C02 — exact production recipe variants

Create exactly these two variants:

| Recipe | Transform | Meaning |
|---|---|---|
| `JUMP012_MIXED_R0` | `R0` | Exact SV5_16 canonical local fixture |
| `JUMP012_MIXED_MX` | `MIRROR_X` | Exact horizontal mirror around `x=11.5` |

For `MIRROR_X`, transform every occupied/support/route/Grab coordinate with
`x' = 23 - x`, keep y and route order unchanged, swap LEFT/RIGHT direction and
face labels, and retain all material, gap, rise, mode, ownership, and validation
fields. Rectangle intervals transform as `left'=23-right`, `right'=23-left`.

Each variant must contain exactly:

- 10 supports: five SOLID and five ONE_WAY;
- 27 base support cells + 17 outline cells = 44 unique occupied cells;
- 9 ordered route links;
- 7 `JUMP` links with `rise=1`;
- 1 `JUMP_GRAB` link with `rise=2`;
- 1 `JUMP` link with `rise=0`;
- 1 explicit Grab edge and zero automatically inferred Grab edges.

The two-recipe catalog therefore contains exactly 20 support rows, 88 occupancy
rows, 18 link rows, and two Grab-edge rows. A transformed identifier may prefix
the recipe ID but must retain an explicit `source_*_id` so R0/Mirror records can
be compared one-to-one.

## C03 — four ordered recipe segments

Within each production recipe, record these exact ordered segments:

| Segment | Source links | Required movement mixture |
|---|---|---|
| `SEG_ASCENT_A` | `JS_LINK_00..02` | three +1 JUMPs |
| `SEG_GRAB_TURN` | `JS_LINK_03..04` | one +2 JUMP_GRAB, then one level JUMP |
| `SEG_ASCENT_B` | `JS_LINK_05..06` | two +1 JUMPs |
| `SEG_ASCENT_C` | `JS_LINK_07..08` | two +1 JUMPs |

The four segments must partition the nine route links exactly once, preserve
order and endpoint continuity, and reconstruct the complete entry-to-exit route
when concatenated. They are data recipes, not claims about swept Player motion.

## C04 — exact #012 reference lineage

`REFERENCE_012_PROFILE.json` is an immutable package input. Validate both
24-character x 32-row grids with canonical bytes `row0\n...row31\n`.

- Before digest: `9cf9f0de9e144951ccc63c4264dfb4ddfa4f1f611ff293e8df15dc28eefad02c`.
- Draft-after digest: `36c4f5a585b15505e8a91d59bd519a212cb02ae2faa8b9cca493e7722e43f7e1`.
- Exact changed coordinates: 141.
- Before counts: SOLID 176, AIR 546, ONE_WAY 46, RESERVED 0.
- Draft counts: SOLID 249, AIR 491, ONE_WAY 28, RESERVED 0.

Export all 768 cells for before and draft-after plus the exact 141-coordinate
diff. Label the old world reservation `(360,224,24,32)` as illustrative
provenance only. Do not read or build a 624x416 world to reproduce it.

Write `reference_012_correspondence.csv` with exactly these five intent rows:

1. `MIXED_SOLID_ONE_WAY` -> production support material counts;
2. `PLUS_ONE_JUMP` -> seven production links per variant;
3. `PLUS_TWO_JUMP_GRAB` -> `JS_LINK_03` and the transformed explicit edge;
4. `LEVEL_CONNECTION` -> `JS_LINK_04`;
5. `IRREGULAR_SOLID_BACKING` -> the 17 SV5_16 outline cells per variant.

Each row must state `DESIGN_LINEAGE_ONLY`, `production_equivalence=false`, and
`player_verified=false`. The 141-cell draft is never inserted into runtime
occupancy and is never called an accepted movement proof.

## C05 — typed source and exports

Create `Sv5JumpRecipeCatalog` and `Sv5JumpRecipeExport` from the accepted typed
SV5_16 object. Do not duplicate an older planner or introduce a parallel legacy
model.

Export from one canonical catalog to
`MapDesign/MCP/GENERATED/SV5_17_JUMP_RECIPES/`:

- `jump_recipes.json`
- `recipe_catalog.csv`
- `recipe_segments.csv`
- `recipe_supports.csv`
- `recipe_occupancy.csv`
- `recipe_links.csv`
- `recipe_grab_edges.csv`
- `reference_012_before.csv`
- `reference_012_draft_after.csv`
- `reference_012_changes.csv`
- `reference_012_correspondence.csv`
- `jump_recipe_validation.json`
- `preview/jump_recipes.svg`
- `visual_export_audit.json`
- `independent_jump_recipe_audit.json`
- targeted/focused NUnit XML and final `BINDING.json`

The SVG shows R0 and MirrorX side by side on full 24x32 grids, distinguishes
SOLID/ONE_WAY/outline, overlays all nine links and the one explicit Grab, and
labels the counts `+1 x7`, `+2 Grab x1`, `level x1`. A smaller lower panel shows
the old #012 before/draft reference and labels it `REFERENCE ONLY — 141 cells —
NOT PRODUCTION`. Render to PNG and inspect it before the full regression.

## C06 — independent rejection and local tests

The independent checker must hard-reject:

- missing/extra recipe or segment;
- any R0 mutation;
- a MirrorX coordinate, interval, direction, face, source-ID, or order mismatch;
- duplicate/missing/extra occupied cells or route links;
- wrong per-recipe or catalog totals;
- a movement mixture other than 7 +1 / 1 +2 Grab / 1 level;
- rise above 2, automatic Grab creation, or a ONE_WAY Grab target;
- segment overlap, gap, reordering, or broken continuity;
- malformed reference rows, wrong cell counts/digests, or a diff other than 141;
- reference cells appearing in production occupancy;
- any production-equivalence or Player-verification claim;
- premature swept-clearance, recovery, or live-Player readiness;
- whole-world generation/search or Sector-based partitioning in the new suite.

Add at least 30 meaningful local tests. Tests operate only on the two 24x32
recipe variants and embedded #012 reference rows. New targeted tests perform
zero 624x416 builds/searches, global endpoint comparisons, or per-candidate
world work.

## C07 — readiness, test order, and scope

- Compile, run the new targeted suite, run the independent checker, render and
  inspect the SVG, then run the full SV5 regression exactly once.
- Final focused total is at least 340 with zero failed, skipped, or inconclusive.
- PASS sets `JumpRecipeReady=true` and `ComposedGeometryReady=true` in the new
  catalog. Keep `SweptClearanceReady=false`, `RecoveryReady=false`, and
  `PlayerVerified=false`.
- Do not modify Player, tree/Hub, world placement, scenes, prefabs, Packages,
  ProjectSettings, Master, or unrelated dirty files.
- Run post-readonly before Finalize, create one atomic Task-owned commit and an
  exact-blob Review ZIP, keep SV5_18 locked, and do not push.
