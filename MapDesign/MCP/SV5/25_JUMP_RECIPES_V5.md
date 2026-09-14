# SV5 Jump Recipes v5

`SV5_17_JUMP_RECIPES` packages the accepted SV5_16 local jump-room fixture as
two deterministic recipes: `JUMP012_MIXED_R0` and `JUMP012_MIXED_MX`. The
24×32 coordinates are local integer cells. They are not a Sector, a world
placement, or a 624×416 generation/search surface.

## Canonical source and counts

Both recipes derive from
`Sv5JumpOutlineGeometry.CreateCanonicalLocalFixture()`, whose locked digest is
`0ac93bf0ddc1a203baa47517096516c2df98d67978d3c80131a13edcedd5df9c`.
Each recipe retains exactly ten supports, 44 occupied cells, nine ordered
route links, four ordered route segments, and one explicit SOLID Grab edge.
The catalog totals are therefore 20 supports, 88 cells, 18 links, eight
segments, and two recipe-local Grab records.

The link mixture in each recipe is fixed:

| Segment | Ordered links | Meaning |
|---|---|---|
| `SEG_ASCENT_A` | `JS_LINK_00..02` | three `rise=+1` JUMPs |
| `SEG_GRAB_TURN` | `JS_LINK_03..04` | one `rise=+2` JUMP_GRAB, then one level JUMP |
| `SEG_ASCENT_B` | `JS_LINK_05..06` | two `rise=+1` JUMPs |
| `SEG_ASCENT_C` | `JS_LINK_07..08` | two `rise=+1` JUMPs |

This proves exactly seven `+1 JUMP`, one `+2 JUMP_GRAB`, and one horizontal
`JUMP`. Reverse-route completion is not required.

## R0 and MIRROR_X

`R0` preserves every SV5_16 support rectangle, occupied cell, link endpoint,
route order, and Grab coordinate. `MIRROR_X` applies `x' = 23 - x` to every
cell and point. A rectangle `[min_x,max_x]` becomes
`[23-max_x,23-min_x]`; route direction swaps LEFT/RIGHT; the Grab face and
approach direction also swap. Support IDs, link IDs, segment membership,
material type, `gap_air`, and `rise` remain unchanged.

The explicit Grab remains attached only to the transformed `JS04_SOLID`
exposed face. ONE_WAY and outline cells never acquire automatic Grab behavior.

## #012 reference boundary

The exported historical #012 before and draft-after grids are design-lineage
reference data only. Their digests are respectively
`9cf9f0de9e144951ccc63c4264dfb4ddfa4f1f611ff293e8df15dc28eefad02c`
and `36c4f5a585b15505e8a91d59bd519a212cb02ae2faa8b9cca493e7722e43f7e1`,
with exactly 141 changed cells. These 768-cell grids never enter production
occupancy and do not establish geometry equivalence or Player verification.

## Readiness boundary

The catalog digest is
`19b4b3c37a1b1b11856eb1c946ce8ea64cbc16517dbfe9ace2ae66e4a5504022`.
Recipe and composed-geometry readiness are true. Swept-clearance readiness,
recovery readiness, and Player verification remain false. World assembly,
global endpoint comparison, Player tuning, and 624×416 generation/search are
outside SV5_17.
