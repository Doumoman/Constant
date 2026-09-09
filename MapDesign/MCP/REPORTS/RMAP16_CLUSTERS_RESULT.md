# RMAP16_CLUSTERS Result

STATUS: PASS

## Scope and upstream verification

- Applied only `RMAP16_CLUSTERS`; inbox source SHA-256, installed task SHA-256, and archive SHA-256 are all `9b7c78e2c8257a5baf1d679b562763a9db4e478f42676e4d94e730ef4b2691ca`.
- RMAP15 was already finalized in commit `cabbd24b94aa3bcbcc9ae852b2f5da5ff4356d3f`; its PASS Result SHA-256 is `734a09ce776e95986b3fca0ee2e8c1ad6323a48d64ae0ba8347779f209c1274c` and its installed/archive Task SHA-256 is `96bb700defb9e494563850004455983022b793b9fe21948294403fb9b99afa80`.
- `RmapClusterAssemblyPlanner.Plan(RmapWorldDefinition, WorldGenerationRngStreams)` is the public RMAP12 -> RMAP13 -> RMAP14 -> RMAP15 consumer. The explicit overload accepts the already-verified definition, graph, biome plan, special reservation plan, and production RNG streams for the future RMAP17 bake adapter.
- RMAP17 was not started. No push was issued.

## Implemented static assembly

- Full world: 624 x 416 = 259,584 typed base cells; 52 x 52 = 2,704 microchunks. Every cell is exactly S, A, or O and records patch, source owner, and pattern provenance where applicable.
- Four contiguous TerrainClusters are actual SLOPE (4 chunks), CAVE (4), CORRIDOR (3), and HALF_PIPE (2) masks. The close repair reserves 16 genuinely all-S microchunks before assembly; no air-containing chunk is labelled `INACTIVE_SOLID`.
- The final order is fixed reservation -> large cluster/secret masks -> explicit RMAP15 ports and directed routes -> 13 route-free 3x2 pattern fields (78 final 16-cell placements) -> separate overlays -> remaining density and balance. Every pattern's exported 16 S/A/O tokens now equal its final cells.
- RMAP15's 2,432 protected cells, 45 ports, 10 slots, and Seal/Boss closed/open geometry remain exact. Every general write still calls `RmapSpecialReservationPlan.EvaluateTerrainCells`; the repaired prefilter yields 2,786 calls and zero rejected writes, and the focused assertion compares all final protected cells to RMAP15.
- Eleven RMAP13 directed edges retain their actual source/target port IDs, all route cells, base/headroom checks, and condition. Horizontal transitions are WALK; every vertical transition receives a `LADDER` overlay whose authority is `CharacterLiveClimbSurface.StaticSafe`, paired with `CharacterLiveMovementSettings.ConfigureRmap02`. No independent `JUMP_MAX_2` physics claim remains.
- The two-chunk secret remains sealed except for its declared BreakableAccess state transition. Its two discovery clues are actual ordinary route cells (`431,142` and `432,142`), not secret-interior claims; no route writes enter the secret chunks.

## A26 density result

Density is measured as `S / (S + A + O)` per actual RMAP14 patch; O is reported independently in `rmap16_density.csv`.

- Open profiles: 474 permille (target 400-550).
- Balanced profiles: 599 permille (target 550-650).
- Dense profiles: 699 permille (target 650-750).
- All 16 patch measurements are within their selected RMAP14 profile range.

## Verification

- RMAP16 close-focused Unity EditMode: 4/4 passed (`rmap16_close_editmode_results.xml`). It verifies final pattern cells, all-solid inactive chunks, route/headroom/ladder linkage, protected RMAP15 cells, secret clues on ordinary routes, and the exact RMAP17 snapshot handoff.
- RMAP15 predecessor focused Unity EditMode: 4/4 passed (`rmap15_predecessor_editmode_results.xml`).
- Visual inspection completed for the full terrain map, route overlay, slope, half-pipe, cave, and secret enlarged maps.
- `git diff --check` completed without whitespace errors for the RMAP16 changes.

## RMAP16_CLOSE audit supplement (C1-C4)

- C1: `rmap16_cells.csv` is the complete 259,584-cell typed/source/provenance aggregate. `rmap16_assembly_summary.csv` accounts for all 2,704 chunks (2,628 active, 2 secret, 16 all-solid inactive, 58 special-reserved), and final pattern-cell equality is an executable assertion.
- C2: `rmap16_routes.csv` publishes every graph edge's OUT/IN port IDs, condition, contiguous cells, walk/climb counts, headroom result, and live-climb authority. Route reuse is explicit source-kind reuse; routes never write protected RMAP15 cells. Progression conditions remain the RMAP13 graph's directed condition values; this work does not invent a gameplay unlock controller.
- C3: RMAP15 is re-run and its protected cells, 45 ports, and 10 slots are compared to final RMAP16 cells. The secret is a two-chunk enclosed shell with one breakable state point; clues are on ordinary route cells, and no mandatory RMAP15 role/slot is inside the secret. Dynamic destruction/gameplay state is intentionally deferred.
- C4: `rmap16_scope_density.csv` publishes world, every chunk state, and every cluster S/A/O denominator with O kept separate. `rmap16_assembly_summary.csv` separates public Type0 from sealed secret counts. `Rmap16BakeSnapshot` gives RMAP17 the exact same read-only cell/chunk/overlay lists and `cell_digest` (`3a40b473d497178d19e11d746d4655cc9a0856c10c0a03e981341ca7b1bda733`), with no second terrain generation path.

## Close evidence hashes

| Artifact | SHA-256 |
|---|---|
| `rmap16_manifest.json` | `9f19f90c5e935dd90c5576e52577cff87eab974cb2f9c0051dd68d445646293a` |
| `rmap16_cells.csv` | `1f074823557f6bd4e9dd48cd9eb1ca939a910fa525baeb89b91335f1950b7d9d` |
| `rmap16_routes.csv` | `f151a12f3b88bcf214c33801b50c7cde0bb9b26d9d85effd66cb97bf49b8d098` |
| `rmap16_scope_density.csv` | `fe832e5c5e608513bddd1e6834e18a7183252bf66eac42494f11839db0790983` |
| `rmap16_assembly_summary.csv` | `a58b1b4e99e1dabb185d7a8026d7814bbc92089ddf4311c9313f1bd502a3d249` |
| RMAP16 close EditMode XML | `ce692c0e01de4a34535afe44eacf72a378c17132cbfe5afeb20c192fd81ecb75` |
| RMAP15 predecessor EditMode XML | `95eea8bc983b49caf331b914c248d77640643f2c2d19a2921cfad7a984d3d6e0` |
| terrain overview PNG | `63c0d3dc872e5766c2d07742dc6ec081ededf8ac67a8f3de1200e0452c02ff0a` |
| route overlay PNG | `1e30fd469f2bae7055528e85d65af4ff38f5793840732c26bbaaf8f28d1827f7` |
| secret enlarged PNG | `ffd124affed5c8a6e77744e094b6b118e44c538171471c10a3f52251422d11b3` |

## Delivered review package

`MapDesign/MCP/GENERATED/RMAP16/RMAP16_REVIEW.zip` contains this Result, both focused XML reports, all current RMAP16 CSV/manifest artifacts (including scope/assembly audit), `rmap16_layout.png`, and the `review/` terrain overview, route overlay, and enlarged shape/secret maps.

The Finalize step is limited to setting RMAP16 COMPLETE and Current Task NONE, followed by an RMAP16-only commit. RMAP17 remains LOCKED.
