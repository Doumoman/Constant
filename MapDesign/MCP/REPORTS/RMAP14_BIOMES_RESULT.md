# RMAP14_BIOMES Result

TASK: RMAP14_BIOMES

STATUS: PASS

## User-facing outcome

The generated planning layout owns every one of the 52x52 microchunk coordinates in the
624x416 tile world. It has four Moonpalace biomes, 16 non-empty spatial patches, deterministic
density-profile selections, and 312 actual neighboring boundary faces. This is a planning and
preview result only: measured Base SOLID density remains `PENDING_GEOMETRY`, no Tilemap/Collider
was baked, and no RMAP15 special footprint was placed.

## Preconditions and normal Apply

- Supplied inbox SHA-256 was verified before mutation: `9d99d0241eeb904ce0cd523c53349e290707cb9eca242e78719a0905ccc42f0b`.
- RMAP13 was already finalized and committed at `50bd209abf715d9586a878669baaddd6be1f11fc`.
- RMAP13 PASS Result SHA matched the installed metadata: `2aeae5f7b259787b2b70d5ce07f61145e4b40f0c3717cbff079d948cc3e01a01`.
- RMAP13 installed Task and archive were byte-identical and their required SHA was
  `00b926dcfcb3fc5ed4e76a5f541fe68064346a2036fe473aeaa7308063685f12`.
- Before Apply the RMAP rows were `13 COMPLETE / 0 CURRENT / 6 LOCKED`; the one
  `single_task_v1` inbox candidate was RMAP14 and both destinations were absent.
- Normal Apply installed and archived the identical RMAP14 body at SHA-256
  `9d99d0241eeb904ce0cd523c53349e290707cb9eca242e78719a0905ccc42f0b`, changed only
  `Current Task: NONE -> RMAP14_BIOMES` and RMAP14 `LOCKED -> CURRENT`, then removed the
  inbox source because the byte-identical Archive now preserves it. The post-Apply rows were
  `13 COMPLETE / 1 CURRENT / 5 LOCKED`; RMAP15 remained `LOCKED`.

## Responsibilities and reuse

| Path | Decision | Responsibility |
|---|---|---|
| `Assets/_Game/Map/Runtime/WorldGeneration/Biomes/RmapWorldBiomePlanner.cs` | NEW | Pure RMAP14 spatial planner and RFC4180/JSON export API. It owns 52x52 planned biome ownership, profile selection, actual adjacency records, and RMAP15 handoff inputs; it performs no file I/O, Tilemap bake, Collider work, Player work, or fixed special placement. |
| `Assets/_Game/Map/Runtime/WorldGeneration/WorldData/RmapWorldDataContract.cs` | REUSED unchanged | Supplies the immutable seed/content/generator definition, RMAP12 biome stream binding, pool identity, and stable world identity. |
| `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/RmapWorldGraphPlanner.cs` | REUSED unchanged | Supplies the passing RMAP13 graph and eight location-less reservations. RMAP14 maps them only to candidate biome-patch sets; it does not turn anchor IDs into full-world coordinates. |
| `Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/Moonpalace*BoundaryAuthoringContract.cs` and `MoonpalaceBiomePairCatalog.cs` | REUSED unchanged | Six actual pair contracts provide pair IDs, candidate/profile sets, supported orientations, no-tool rule, mandatory-route rule, and edge signatures for each recorded adjacency. Historical coverage validators were not repurposed because their approved source chain requires no generated CSV mutation. |
| `Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/*` | KEEP unchanged | RMAP15 remains owner of fixed special-region slots, footprints, access, and collision decisions. |
| `Assets/_Game/Tests/EditMode/Map/RMAP14/RmapWorldBiomePlannerTests.cs` | NEW | Focused Unity proof plus deterministic generated CSV/JSON/PNG material. |

## W05 and A25 evidence

- W05: `rmap14_patch_ownership.csv` has `2704` rows and `2704` unique `micro_x,micro_y` pairs.
  The grid is exactly `52x52`, each microchunk maps to its actual `12x8` tile extent, all four
  biome keys occur, and every one of the 16 patches owns 169 non-empty microchunks.
- W05 boundaries: `rmap14_boundaries.csv` records 312 neighboring faces. All six canonical
  Moonpalace biome pairs occur and all six existing authoring-contract sources occur. Each row
  records source/target microchunk coordinate, direction, patch IDs, canonical pair, orientation,
  pair rule, existing edge signature, candidate/profile source sets, no-tool requirement, and
  mandatory-route permission. These are adjacency/authoring constraints, not a final port or
  physical traversal claim.
- A25: the shared source catalog has `OPEN=400..550`, `BALANCED=550..650`, and `DENSE=650..750`
  permille target intervals. Each patch has positive Open/Balanced/Dense configured weights and
  is selected deterministically from the distinct `RNG_BIOME_PATCH` scope
  `RMAP14_PROFILE_SELECTION`; no RunGraph stream is used for profile selection.
- Density uses actual patch scopes and records `PENDING_GEOMETRY` for every row rather than
  fabricating SOLID percentages before a Base Tilemap exists. Ownership state is planning
  `Active`, not a Player mobility boolean or a collision statement.

## RMAP13 to RMAP15 handoff

- `rmap14_reservation_inputs.csv` contains all eight RMAP13 reservation IDs, graph node IDs,
  anchor stable IDs, entry/release conditions, eligible biome patch sets, and candidate
  microchunk-bound unions.
- Every row is explicitly `PATCH_SET_ONLY` and `NOT_PLACED_RMAP15`. No fixed terrain, entrance,
  exit, footprint, conflict ownership, or special site was claimed early.

## Generated material and inspection

All material below is generated by the passing Unity RMAP14 test from one plan for seed `1304`,
`CONTENT_V1`, `GENERATOR_V1`, not a hand-authored catalog. The two PNGs were visually inspected:
their 624x416 layout area retains the 12x8 cell aspect ratio, draws boundary faces, includes a
compact seed/version header and color swatches; exact versions and all IDs are in the JSON/CSV
sidecars.

| File | SHA-256 |
|---|---|
| `rmap14_manifest.json` | `78b15475f87b4ca87313182e98e4c0cb2cc315200e908a549eb6366bee7a9833` |
| `rmap14_patch_ownership.csv` | `fa004b0c14336e962c8616393619e5e31638b424e3e1dc9a95074a93179de907` |
| `rmap14_patches.csv` | `1ca573a626f6885ba5553901525d771e6cb91ff92ab2832a5014e449e904ff26` |
| `rmap14_boundaries.csv` | `228d6c9b2bc63bb5e7baaf37157e378664edff95daee0a10f549be354116cf82` |
| `rmap14_profiles.csv` | `54ea92386a816d80ef2f57dbcad0dbe9befff171fd66fc1e34de5e97f4edf8fe` |
| `rmap14_reservation_inputs.csv` | `15f321eeed95c8aef5594e03126de9ae4f6528d3562eacb07ff2685f70c2ef34` |
| `rmap14_biome_layout.png` | `03338ecd94af4626d5f9e80dd4e201d6a801f7d90bc57e19b7c21af68632f6d2` |
| `rmap14_density_profiles.png` | `35703eb78f222c1e8db1279f45d296307d44ef5386db69907b13caefce1e886f` |

## Focused validation

- Unity `6000.3.8f1`, EditMode `RmapWorldBiomePlannerTests`: **4/4 PASS**, failed/skipped `0/0`;
  XML `rmap14_editmode_results.xml`, SHA-256
  `5e4f931542852767cb13a5113f732e4eff96cc5f215a1fb9ac202b3c7436c141`.
- Unity `6000.3.8f1`, direct existing EditMode `RmapWorldGraphPlannerTests`: **4/4 PASS**;
  XML `rmap14_existing_graph_results.xml`, SHA-256
  `0b4502ba84c491c4a9447fe99d06a4ed3f8e2f5cfc9eee6ee112455402321523`.
- Unity `6000.3.8f1`, direct existing EditMode `MoonpalaceBiomePairContractTests`: **180/180 PASS**;
  XML `rmap14_existing_boundary_results.xml`, SHA-256
  `10a6c185ddc47827e2ed95b993de7439a7406a93518c6c1d83ea2c363caca9c2`.
- No broad/unfiltered regression, build, Player/Camera/Input test, scene/prefab mutation, Tilemap/
  Collider bake, full world simulation, RMAP15 work, or push was run.

## Finalization boundary

PASS is limited to RMAP14's deterministic biome-space planning, density-profile contract,
existing-boundary source linkage, and preview/export evidence. RMAP15_SPECIALS remains
`LOCKED` / `NOT STARTED`. Finalize RMAP14 only, then commit only RMAP14-owned files.
