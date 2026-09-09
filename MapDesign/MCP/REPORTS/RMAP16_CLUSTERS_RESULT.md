# RMAP16_CLUSTERS Result

STATUS: PASS

## Scope and upstream verification

- Applied only `RMAP16_CLUSTERS`; inbox source SHA-256, installed task SHA-256, and archive SHA-256 are all `9b7c78e2c8257a5baf1d679b562763a9db4e478f42676e4d94e730ef4b2691ca`.
- RMAP15 was already finalized in commit `cabbd24b94aa3bcbcc9ae852b2f5da5ff4356d3f`; its PASS Result SHA-256 is `734a09ce776e95986b3fca0ee2e8c1ad6323a48d64ae0ba8347779f209c1274c` and its installed/archive Task SHA-256 is `96bb700defb9e494563850004455983022b793b9fe21948294403fb9b99afa80`.
- `RmapClusterAssemblyPlanner.Plan(RmapWorldDefinition, WorldGenerationRngStreams)` is the public RMAP12 -> RMAP13 -> RMAP14 -> RMAP15 consumer. The explicit overload accepts the already-verified definition, graph, biome plan, special reservation plan, and production RNG streams for the future RMAP17 bake adapter.
- RMAP17 was not started. No push was issued.

## Implemented static assembly

- Full world: 624 x 416 = 259,584 typed base cells; 52 x 52 = 2,704 microchunks. Every cell is exactly S, A, or O and records patch, source owner, and pattern provenance where applicable.
- Four contiguous TerrainClusters were assembled as actual cell masks: SLOPE (4 chunks), CAVE (4), CORRIDOR (3), and HALF_PIPE (2). Their 78 six-slot pattern placements consume the fixed RMAP11 500-pool. Base geometry and overlays remain separate.
- RMAP15's 2,432 fixed/protected cells, 45 port cells, 10 slots, and Seal/Boss closed/open geometry remain exact. General density, patterns, cluster masks, route clearance/support, and secret geometry route all candidate writes through `RmapSpecialReservationPlan.EvaluateTerrainCells`; 10,096 gate calls rejected protected cells instead of overwriting them.
- Eleven RMAP13 graph edges publish protected-safe static cell spines from explicit RMAP15 OUT/BOTH ports to IN/BOTH ports. The retained static contract is `PLAYER_0.4x0.8 | STEP_MAX_1 | JUMP_MAX_2 | GRAB_REQUIRED_FOR_VERTICAL`; this is static route evidence, not a Player scene run.
- One Type0 secret cavity uses two contiguous chunks, one declared breakable entry, closed/open state geometry, and two positioned observation clues. Non-active chunks are explicitly `INACTIVE_SOLID`, not secret space.

## A26 density result

Density is measured as `S / (S + A + O)` per actual RMAP14 patch; O is reported independently in `rmap16_density.csv`.

- Open profiles: 474 permille (target 400-550).
- Balanced profiles: 599 permille (target 550-650).
- Dense profiles: 699 permille (target 650-750).
- All 16 patch measurements are within their selected RMAP14 profile range.

## Verification

- RMAP16 focused Unity EditMode: 3/3 passed (`rmap16_editmode_results.xml`).
- RMAP12 input contract: 4/4 passed.
- RMAP13 input contract: 4/4 passed.
- RMAP14 input contract: 4/4 passed.
- RMAP15 input contract: 4/4 passed.
- Visual inspection completed for the full terrain map, route overlay, slope, half-pipe, cave, and secret enlarged maps.
- `git diff --check` completed without whitespace errors for the RMAP16 changes.

## Delivered review package

`MapDesign/MCP/GENERATED/RMAP16/RMAP16_REVIEW.zip` contains this Result, the focused XML evidence, all RMAP16 CSV/manifest artifacts, `rmap16_layout.png`, and the `review/` terrain overview, route overlay, and enlarged shape/secret maps.

The Finalize step is limited to setting RMAP16 COMPLETE and Current Task NONE, followed by an RMAP16-only commit. RMAP17 remains LOCKED.
