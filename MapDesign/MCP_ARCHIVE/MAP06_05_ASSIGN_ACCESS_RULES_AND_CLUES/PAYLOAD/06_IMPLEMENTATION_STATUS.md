# Map Implementation Status

## Generator Package

```text
Spec Baseline: GDD v0.3
Implementation Package Baseline: Map Package v1.0
MCP Starter Rules: v1.2
Status Finalize Rules: v1.0
Master Task Backlog: v1.0 / 205 tasks
```

## Current Task

```text
TASKS/MAP06_05_ASSIGN_ACCESS_RULES_AND_CLUES.md
```

## Status

| Task | Status |
|---|---|
| MAP00_01_PROJECT_AUDIT | COMPLETE |
| MAP00_02_FOLDER_AND_ASMDEF_PLAN | COMPLETE |
| MAP00_03_CREATE_MAP_MODULE_STRUCTURE | COMPLETE |
| MAP00_04_CREATE_TEST_STRUCTURE | COMPLETE |
| MAP00_05_DEFINE_WORLDGEN_CONSTANTS | COMPLETE |
| MAP00_06_IMPLEMENT_COORDINATE_VALUE_TYPES | COMPLETE |
| MAP00_07_IMPLEMENT_COORDINATE_CONVERSIONS | COMPLETE |
| MAP00_08_CREATE_COORDINATE_TESTS | COMPLETE |
| MAP00_09_CREATE_COORDINATE_DEBUG_VIEW | COMPLETE |
| MAP00_10_MAP00_EXIT_AUDIT | COMPLETE |
| MAP01_01_INSTALL_CSV_AUTHORING_BASELINE | COMPLETE |
| MAP01_02_IMPLEMENT_CSV_SCHEMA_CATALOG | COMPLETE |
| MAP01_03_IMPLEMENT_RFC4180_READER | COMPLETE |
| MAP01_04_IMPLEMENT_HEADER_AND_FIELD_VALIDATION | COMPLETE |
| MAP01_05_IMPLEMENT_PRIMARY_KEY_INDEX | COMPLETE |
| MAP01_06_IMPLEMENT_SCALAR_AND_LIST_PARSERS | COMPLETE |
| MAP01_07_IMPLEMENT_WORLD_ROUTE_DEFINITIONS | COMPLETE |
| MAP01_08_IMPLEMENT_BIOME_BOUNDARY_DEFINITIONS | COMPLETE |
| MAP01_09_IMPLEMENT_SPECIAL_VILLAGE_DEFINITIONS | COMPLETE |
| MAP01_10_IMPLEMENT_MICROCHUNK_POPULATION_ITEM_DEFINITIONS | COMPLETE |
| MAP01_11_IMPLEMENT_FOREIGN_KEY_RESOLVER | COMPLETE |
| MAP01_12_IMPLEMENT_STATIC_DATA_REGISTRY | COMPLETE |
| MAP01_13_IMPLEMENT_CONTENT_VERSION_HASH | COMPLETE |
| MAP01_14_IMPLEMENT_ATOMIC_PUBLISH_AND_IMPORT_REPORT | COMPLETE |
| MAP01_15_CREATE_CSV_IMPORT_WINDOW | COMPLETE |
| MAP01_16_CREATE_CSV_FAILURE_FIXTURES_AND_TESTS | COMPLETE |
| MAP01_17_MAP01_EXIT_AUDIT | COMPLETE |
| MAP02_01_IMPLEMENT_GENERATED_WORLD_DATA | COMPLETE |
| MAP02_02_IMPLEMENT_DETERMINISTIC_RNG_STREAMS | COMPLETE |
| MAP02_03_IMPLEMENT_GRID_INITIALIZATION_PASS | COMPLETE |
| MAP02_04_IMPLEMENT_WORLD_GENERATION_ROOT | COMPLETE |
| MAP02_05_IMPLEMENT_PASS_EXECUTION_RECORDS | COMPLETE |
| MAP02_06_IMPLEMENT_SEED_MANIFEST_AND_REPLAY_RECORDER | COMPLETE |
| MAP02_07_CREATE_WORLD_TOPOLOGY_OVERLAY | COMPLETE |
| MAP02_08_MAP02_EXIT_TESTS | COMPLETE |
| MAP03_01_IMPLEMENT_SITE_RESERVATION_MODELS | COMPLETE |
| MAP03_02_ENUMERATE_START_AND_SITE_CANDIDATES | COMPLETE |
| MAP03_03_IMPLEMENT_FOOTPRINT_PLACEMENT_SOLVER | COMPLETE |
| MAP03_04_IMPLEMENT_SITE_DISTANCE_INDEX | COMPLETE |
| MAP03_05_IMPLEMENT_SITE_CANDIDATE_COST | COMPLETE |
| MAP03_06_IMPLEMENT_RESERVATION_BACKTRACKING | COMPLETE |
| MAP03_07_IMPLEMENT_CORE_CAPACITY_FLOOD_CHECK | COMPLETE |
| MAP03_08_IMPLEMENT_VILLAGE_RESERVATION | COMPLETE |
| MAP03_09_IMPLEMENT_SITE_RESERVATION_VALIDATOR | COMPLETE |
| MAP03_10_CREATE_SITE_RESERVATION_OVERLAY | COMPLETE |
| MAP03_11_MAP03_BATCH_AND_EXIT_TESTS | COMPLETE |
| MAP04_01_IMPLEMENT_BIOME_PATCH_MODELS | COMPLETE |
| MAP04_02_INITIALIZE_CORE_PATCH_SEEDS | COMPLETE |
| MAP04_03_IMPLEMENT_CORE_PATCH_GROWER | COMPLETE |
| MAP04_04_IMPLEMENT_SATELLITE_SEED_PLACER | COMPLETE |
| MAP04_05_IMPLEMENT_MULTI_SEED_BIOME_GROWER | COMPLETE |
| MAP04_06_IMPLEMENT_INTRUSION_PLACEMENT | COMPLETE |
| MAP04_07_IMPLEMENT_PATCH_CLEANUP | COMPLETE |
| MAP04_08_EXPORT_BIOME_PATCH_RESULTS | COMPLETE |
| MAP04_09_IMPLEMENT_BIOME_PATCH_VALIDATOR | COMPLETE |
| MAP04_10_CREATE_BIOME_PATCH_OVERLAY | COMPLETE |
| MAP04_11_MAP04_BATCH_AND_EXIT_TESTS | COMPLETE |
| MAP05_01_BUILD_MANDATORY_TERMINALS | COMPLETE |
| MAP05_02_IMPLEMENT_ROUTE_MASK_LOOKUP | COMPLETE |
| MAP05_03_IMPLEMENT_MINIMUM_CONNECTOR_TREE | COMPLETE |
| MAP05_04_IMPLEMENT_HORIZONTAL_BACKBONE_ROUTER | COMPLETE |
| MAP05_05_IMPLEMENT_VERTICAL_GATEWAY_PLANNER | COMPLETE |
| MAP05_06_RESOLVE_UP_DOWN_CONFLICTS | COMPLETE |
| MAP05_07_ADD_MANDATORY_ROUTE_LOOPS | COMPLETE |
| MAP05_08_BUILD_MANDATORY_ROUTE_GRAPH | COMPLETE |
| MAP05_09_VALIDATE_MANDATORY_ROUTE_GRAPH | COMPLETE |
| MAP05_10_CREATE_MANDATORY_ROUTE_OVERLAY | COMPLETE |
| MAP05_11_MAP05_BATCH_AND_EXIT_TESTS | COMPLETE |
| MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS | COMPLETE |
| MAP06_02_ENUMERATE_OPTIONAL_ATTACHMENTS | COMPLETE |
| MAP06_03_IMPLEMENT_OPTIONAL_REGION_GROWER | COMPLETE |
| MAP06_04_ASSIGN_TYPE0_ROUTE_MASKS | COMPLETE |
| MAP06_05_ASSIGN_ACCESS_RULES_AND_CLUES | CURRENT |
| MAP06_06_CALCULATE_OPTIONAL_REWARD_TIER | LOCKED |
| MAP06_07_IMPLEMENT_RETURN_POLICY | LOCKED |
| MAP06_08_ASSIGN_INACTIVE_BUFFERS | LOCKED |
| MAP06_09_IMPLEMENT_OPTIONAL_REGION_VALIDATOR | LOCKED |
| MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS | LOCKED |
| MAP07_01_IMPLEMENT_MICROCHUNK_DEFINITION | LOCKED |
| MAP07_02_IMPLEMENT_TILE_LAYER_RULES | LOCKED |
| MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS | LOCKED |
| MAP07_04_IMPLEMENT_SOCKET_EDGE_VALIDATION | LOCKED |
| MAP07_05_IMPLEMENT_OBJECT_SLOT_VALIDATION | LOCKED |
| MAP07_06_IMPLEMENT_96_CELL_VALIDATOR | LOCKED |
| MAP07_07_IMPLEMENT_MICROCHUNK_REACHABILITY_PROBE | LOCKED |
| MAP07_08_CREATE_MICROCHUNK_AUTHORING_GRID | LOCKED |
| MAP07_09_CREATE_SOCKET_AND_SLOT_EDITOR | LOCKED |
| MAP07_10_IMPLEMENT_MICROCHUNK_CSV_IMPORT | LOCKED |
| MAP07_11_IMPLEMENT_MICROCHUNK_CSV_EXPORT | LOCKED |
| MAP07_12_CREATE_MICROCHUNK_PREVIEW_AND_REPORT | LOCKED |
| MAP07_13_MAP07_STARTER_AND_EXIT_TESTS | LOCKED |
| MAP08_01_DEFINE_MOONPALACE_BIOME_PAIRS | LOCKED |
| MAP08_02_IMPLEMENT_BOUNDARY_CANDIDATE_INDEX | LOCKED |
| MAP08_03_IMPLEMENT_BOUNDARY_CHUNK_RESOLVER | LOCKED |
| MAP08_04_FILTER_MANDATORY_BOUNDARIES | LOCKED |
| MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT | LOCKED |
| MAP08_06_AUTHOR_CRATER_ROOT_BOUNDARIES | LOCKED |
| MAP08_07_AUTHOR_CRATER_MILL_BOUNDARIES | LOCKED |
| MAP08_08_AUTHOR_CRATER_DOUGH_BOUNDARIES | LOCKED |
| MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES | LOCKED |
| MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES | LOCKED |
| MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES | LOCKED |
| MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR | LOCKED |
| MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW | LOCKED |
| MAP08_14_MAP08_EXIT_TESTS | LOCKED |
| MAP09_01_IMPLEMENT_SECTOR_RECIPE_RESOLVER | LOCKED |
| MAP09_02_FIX_EXTERNAL_SOCKET_CELLS | LOCKED |
| MAP09_03_FIX_BOUNDARY_MICROCHUNK_CELLS | LOCKED |
| MAP09_04_FIX_MANDATORY_PATH_CELLS | LOCKED |
| MAP09_05_BUILD_STABLE_MICROCHUNK_CANDIDATES | LOCKED |
| MAP09_06_IMPLEMENT_MICROCHUNK_COMPATIBILITY | LOCKED |
| MAP09_07_IMPLEMENT_MRV_CONSTRAINT_SOLVER | LOCKED |
| MAP09_08_IMPLEMENT_BACKTRACK_AND_RETRY_LIMITS | LOCKED |
| MAP09_09_INTEGRATE_TILE_REACHABILITY_PROBE | LOCKED |
| MAP09_10_EXPORT_SECTOR_ASSEMBLY_RESULTS | LOCKED |
| MAP09_11_CREATE_SECTOR_SOLVER_DEBUG_VIEW | LOCKED |
| MAP09_12_CREATE_SECTOR_ASSEMBLY_UNIT_TESTS | LOCKED |
| MAP09_13_MAP09_BATCH_AND_EXIT_TESTS | LOCKED |
| MAP10_01_VALIDATE_SITE_FOOTPRINT_LOCAL_CELLS | LOCKED |
| MAP10_02_IMPLEMENT_SPECIAL_MAP_ASSEMBLER | LOCKED |
| MAP10_03_CONNECT_SPECIAL_MAP_ENTRIES | LOCKED |
| MAP10_04_PLACE_EVENT_AND_REWARD_ANCHORS | LOCKED |
| MAP10_05_IMPLEMENT_VILLAGE_LAYOUT_RESOLVER | LOCKED |
| MAP10_06_PLACE_FIXED_VILLAGE_FACILITIES | LOCKED |
| MAP10_07_PLACE_OPTIONAL_VILLAGE_FACILITIES | LOCKED |
| MAP10_08_IMPLEMENT_EVACUATED_VILLAGE_VARIANT | LOCKED |
| MAP10_09_IMPLEMENT_SPECIAL_MAP_REACHABILITY_VALIDATOR | LOCKED |
| MAP10_10_IMPLEMENT_VILLAGE_VALIDATOR | LOCKED |
| MAP10_11_EXPORT_SITE_AND_VILLAGE_RESULTS | LOCKED |
| MAP10_12_MAP10_DEBUG_AND_EXIT_TESTS | LOCKED |
| MAP11_01_IMPLEMENT_TILE_AND_PREFAB_RESOLVER | LOCKED |
| MAP11_02_IMPLEMENT_TRANSFORMED_CELL_PLACEMENT | LOCKED |
| MAP11_03_CREATE_SECTOR_TILEMAP_LAYERS | LOCKED |
| MAP11_04_IMPLEMENT_TILEMAP_SECTOR_BAKER | LOCKED |
| MAP11_05_IMPLEMENT_SECTOR_COLLIDER_REBUILD | LOCKED |
| MAP11_06_IMPLEMENT_SECTOR_RUNTIME_HANDLE | LOCKED |
| MAP11_07_IMPLEMENT_7X7_PRELOAD_WINDOW | LOCKED |
| MAP11_08_IMPLEMENT_5X5_ACTIVE_STREAMING | LOCKED |
| MAP11_09_IMPLEMENT_BOUNDARY_PREACTIVATION | LOCKED |
| MAP11_10_IMPLEMENT_SECTOR_MODIFICATION_BITSETS | LOCKED |
| MAP11_11_IMPLEMENT_WORLD_SAVE_MANIFEST | LOCKED |
| MAP11_12_IMPLEMENT_REGENERATE_AND_APPLY_SAVE | LOCKED |
| MAP11_13_CREATE_BAKE_STREAM_SAVE_TESTS | LOCKED |
| MAP11_14_MAP11_PERFORMANCE_AND_EXIT_AUDIT | LOCKED |
| MAP12_01_IMPLEMENT_POPULATION_SLOT_INDEX | LOCKED |
| MAP12_02_IMPLEMENT_STABLE_SPAWN_IDS | LOCKED |
| MAP12_03_PLACE_MANDATORY_EVENTS_AND_CORE_RESOURCES | LOCKED |
| MAP12_04_IMPLEMENT_UNIQUE_REWARD_ALLOCATOR | LOCKED |
| MAP12_05_IMPLEMENT_SHOP_POPULATION | LOCKED |
| MAP12_06_IMPLEMENT_RESOURCE_SPAWN_FILTERS | LOCKED |
| MAP12_07_IMPLEMENT_MAP_ELEMENT_PLACEMENT | LOCKED |
| MAP12_08_IMPLEMENT_HAZARD_AND_ENEMY_PLACEMENT | LOCKED |
| MAP12_09_IMPLEMENT_HIERARCHICAL_BUDGETS | LOCKED |
| MAP12_10_IMPLEMENT_REPETITION_AND_NEIGHBOR_RULES | LOCKED |
| MAP12_11_PLACE_REWARDS_AND_DECORATION | LOCKED |
| MAP12_12_EXPORT_GENERATED_SPAWNS | LOCKED |
| MAP12_13_IMPLEMENT_POPULATION_VALIDATOR_AND_DEBUG | LOCKED |
| MAP12_14_MAP12_DETERMINISM_AND_EXIT_TESTS | LOCKED |
| MAP13_01_IMPLEMENT_VALIDATION_RULE_REGISTRY | LOCKED |
| MAP13_02_BUILD_TILE_TRAVERSAL_NODES | LOCKED |
| MAP13_03_BUILD_MOVEMENT_EDGES | LOCKED |
| MAP13_04_CONNECT_INTERSECTOR_TRAVERSAL | LOCKED |
| MAP13_05_IMPLEMENT_NAKED_MANDATORY_BFS | LOCKED |
| MAP13_06_IMPLEMENT_COMPLETION_STATE_SEARCH | LOCKED |
| MAP13_07_MEASURE_COMPLETION_DISTANCE | LOCKED |
| MAP13_08_MEASURE_REVISIT_RATIO | LOCKED |
| MAP13_09_VALIDATE_ZERO_TOOL_SCENARIO | LOCKED |
| MAP13_10_VALIDATE_VILLAGE_SKIPPED_SCENARIO | LOCKED |
| MAP13_11_VALIDATE_HOSTILE_SHOPS_SCENARIO | LOCKED |
| MAP13_12_VALIDATE_DESTRUCTION_AND_MOVING_WORST_CASE | LOCKED |
| MAP13_13_EXPORT_VALIDATION_RESULTS | LOCKED |
| MAP13_14_CREATE_SEED_FAILURE_BUNDLE | LOCKED |
| MAP13_15_IMPLEMENT_HEADLESS_BATCH_SEED_RUNNER | LOCKED |
| MAP13_16_MAP13_SCALE_AND_EXIT_AUDIT | LOCKED |
| MAP14_01_CREATE_WORLD_GENERATOR_WINDOW_SHELL | LOCKED |
| MAP14_02_IMPLEMENT_PASS_STEP_AND_ROLLBACK | LOCKED |
| MAP14_03_CREATE_WORLD_OVERLAY_TABS | LOCKED |
| MAP14_04_CREATE_SECTOR_INSPECTOR_PANEL | LOCKED |
| MAP14_05_IMPLEMENT_CSV_SOURCE_NAVIGATION | LOCKED |
| MAP14_06_IMPLEMENT_VALIDATION_CAMERA_JUMP | LOCKED |
| MAP14_07_CREATE_SEED_REPLAY_BROWSER | LOCKED |
| MAP14_08_VALIDATE_REPLAY_CONTENT_HASH | LOCKED |
| MAP14_09_INTEGRATE_MICROCHUNK_AUTHORING_WINDOW | LOCKED |
| MAP14_10_INTEGRATE_BOUNDARY_PREVIEW | LOCKED |
| MAP14_11_CREATE_RUNTIME_WORLD_DEBUG_HUD | LOCKED |
| MAP14_12_IMPLEMENT_GENERATED_OUTPUT_EXPORT | LOCKED |
| MAP14_13_MAP14_UI_AND_EXIT_TESTS | LOCKED |
| MAP15_01_REPLACE_GRAYBOX_WITH_MOONPALACE_TILE_SHELL | LOCKED |
| MAP15_02_AUTHOR_MOON_CRATER_CHUNK_POOLS | LOCKED |
| MAP15_03_AUTHOR_CASSIA_ROOT_CHUNK_POOLS | LOCKED |
| MAP15_04_AUTHOR_ABANDONED_MILL_CHUNK_POOLS | LOCKED |
| MAP15_05_AUTHOR_MOON_DOUGH_CHUNK_POOLS | LOCKED |
| MAP15_06_EXPAND_ALL_SIX_BOUNDARY_POOLS | LOCKED |
| MAP15_07_EXPAND_SECTOR_RECIPE_POOLS | LOCKED |
| MAP15_08_IMPLEMENT_THREE_CORE_RESOURCE_SITES | LOCKED |
| MAP15_09_IMPLEMENT_FORGE_AND_SEAL_FLOW | LOCKED |
| MAP15_10_IMPLEMENT_BOSS_SITE_GRAYBOX | LOCKED |
| MAP15_11_COMPLETE_MOONPALACE_VILLAGE | LOCKED |
| MAP15_12_CONNECT_TOOLS_AND_BATTERIES_TO_TYPE0 | LOCKED |
| MAP15_13_IMPLEMENT_RECENT_USE_REPETITION_LIMITS | LOCKED |
| MAP15_14_SELECT_AND_LOCK_30_QA_SEEDS | LOCKED |
| MAP15_15_CREATE_MOONPALACE_PLAYMODE_TESTS | LOCKED |
| MAP15_16_COLLECT_30_PLAYTEST_TELEMETRY_RUNS | LOCKED |
| MAP15_17_TUNE_REPETITION_DISTANCE_AND_CONTENT_GAPS | LOCKED |
| MAP15_18_VERTICAL_SLICE_RELEASE_AUDIT | LOCKED |

## Last Completed Task

```text
MAP06_04_ASSIGN_TYPE0_ROUTE_MASKS
```

## Last Result

```text
REPORTS/MAP06_04_ASSIGN_TYPE0_ROUTE_MASKS_RESULT.md
STATUS: PASS
SHA-256: 7cfb055bb6cb1df24206b25a1a5f046936c7fbdf58bd4b307d476ead4f28ed7a
```

## Confirmed Baseline

- Unity: `6000.3.8f1`
- Runtime assembly: `Game.Map.Runtime`
- Runtime namespace boundary: `StarNight.Map.WorldGeneration.*`
- Editor assembly: `MapAuthoring.Editor`
- Runtime EditMode assembly: `Game.Map.Tests.EditMode`
- Editor EditMode assembly: `MapAuthoring.Tests.EditMode`
- PlayMode assembly: `Game.Map.Tests.PlayMode`
- New asmdef/asmref: `NO`
- MAP00_03: approved 36 directories and folder `.meta` files present
- MAP00_04: architecture fixtures 3개, actual EditMode cases 10/10 PASS
- MAP00_05: `WorldGenConstants` 15개 const, constant tests 6/6 PASS
- MAP00_06: coordinate value types 4개, value type tests 12/12 PASS
- MAP00_07: `WorldCoordinateUtility` public API 14개, utility tests 10/10 PASS
- MAP00_08: exhaustive tests 8/8, microchunk corners 10,816, world tiles 259,584 PASS
- MAP00_09: coordinate debug display/window, Editor tests 7/7, visual 9/9 PASS
- MAP00_10: combined targeted EditMode 53/53, compile error 0, magic-number/Legacy dependency 0, exit approved
- MAP00 production inventory: Runtime C# 6개, Editor C# 2개
- MAP00 test inventory: C# 8개
- Authoring CSV before MAP01_01: `0`
- Revalidated MAP01_01 input tree: 64 files
- Source validator: exit `0`, `ERROR 0`, `WARNING 10`
- Dictionary unique file names: `60`
- File map: `49` rows, category `6/9/2/5/7/7/3/6/4`
- Install source UTF-8 BOM missing: `0`
- Input relative-manifest SHA-256: `2b0d40ea2d67173168b452b722bc6af91268c28636abda6ae3a6a63457e7109e`
- MAP01_01 installed Authoring CSV: static `49` + dictionary `1` = `50`
- MAP01_01 source/destination SHA-256: `50/50` identical, mismatch `0`
- MAP01_01 UTF-8 BOM: `50/50`, missing `0`
- MAP01_01 `.csv.meta`: `50/50`, GUID duplicate `0`
- MAP01_01 targeted architecture EditMode: `10/10 PASS`
- MAP01_01 compile error / relevant new warning: `0 / 0`
- Installed dictionary baseline: `679` rows / `60` files / exact 10-column header
- MAP01_02 immutable schema catalog: `60` files / `679` columns
- MAP01_02 Runtime production C#: `8`, Editor production C#: `1`
- MAP01_02 schema tests: `30/30 PASS`
- MAP01_02 architecture regression: `10/10 PASS`
- MAP01_02 targeted EditMode: `40/40 PASS`
- MAP01_02 compile error / relevant new warning: `0 / 0`
- MAP01_03 RFC4180 reader tests: `31/31 PASS`
- MAP01_03 schema/importer regression: `23/23 + 9/9 PASS`
- MAP01_03 architecture regression: `10/10 PASS`
- MAP01_03 targeted EditMode: `73/73 PASS`
- MAP01_03 compile error / relevant new warning: `0 / 0`
- MAP01_03 new Runtime C#/meta: `8 / 8`, GUID duplicate `0`
- MAP01_04 header/field validator tests: `29/29 PASS`
- MAP01_04 reader/schema/importer/architecture regression: `31/31 + 23/23 + 9/9 + 10/10 PASS`
- MAP01_04 targeted EditMode: `102/102 PASS`
- MAP01_04 compile error / relevant new warning: `0 / 0`
- MAP01_04 new Runtime C#/test C#/meta: `6 / 1 / 7`, GUID duplicate `0`
- MAP01_04 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP01_05 primary-key index tests: `32/32 PASS`
- MAP01_05 validator/reader/schema/importer/architecture regression: `29/29 + 31/31 + 23/23 + 9/9 + 10/10 PASS`
- MAP01_05 targeted EditMode: `134/134 PASS`
- MAP01_05 compile error / relevant new warning: `0 / 0`
- MAP01_05 new Runtime C#/test C#/meta: `6 / 1 / 7`, GUID duplicate `0`
- MAP01_05 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP01_06 scalar/list parser tests: `97/97 PASS`
- MAP01_06 PK/validator/reader/schema/importer/architecture regression: `32/32 + 29/29 + 31/31 + 23/23 + 9/9 + 10/10 PASS`
- MAP01_06 targeted EditMode: `231/231 PASS`
- MAP01_06 full project EditMode: `274/274 PASS`
- MAP01_06 compile error / relevant new warning: `0 / 0`
- MAP01_06 new Runtime C#/test C#/meta: `7 / 1 / 8`, GUID duplicate `0`
- MAP01_06 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP01_07 world/route definition tests: `59/59 PASS`
- MAP01_07 parser/PK/validator/reader/schema/importer/architecture regression: `97/97 + 32/32 + 29/29 + 31/31 + 23/23 + 9/9 + 10/10 PASS`
- MAP01_07 targeted EditMode: `290/290 PASS`
- MAP01_07 full project EditMode: `333/333 PASS`
- MAP01_07 compile error / relevant new warning: `0 / 0`
- MAP01_07 new Runtime C#/test C#/meta: `8 / 1 / 9`, GUID duplicate `0`
- MAP01_08 biome/boundary definition tests: `36/36 PASS`
- MAP01_08 targeted EditMode: `326/326 PASS`
- MAP01_08 full project EditMode: `369/369 PASS`
- MAP01_08 compile error / relevant new warning: `0 / 0`
- MAP01_08 new Runtime C#/test C#/meta: `7 / 1 / 8`, GUID duplicate `0`
- MAP01_08 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP01_09 special/village definition tests: `48/48 PASS`
- MAP01_09 targeted EditMode: `374/374 PASS`
- MAP01_09 full project EditMode: `417/417 PASS`
- MAP01_09 compile error / relevant new warning: `0 / 0`
- MAP01_09 new Runtime C#/test C#/meta: `7 / 1 / 8`, GUID duplicate `0`
- MAP01_09 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP01_10 microchunk/population/item definition tests: `64/64 PASS`
- MAP01_10 targeted EditMode: `438/438 PASS`
- MAP01_10 full project EditMode: `481/481 PASS`
- MAP01_10 compile error / relevant new warning: `0 / 0`
- MAP01_10 new Runtime C#/test C#/meta: `9 / 1 / 10`, GUID duplicate `0`
- MAP01_10 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP01_11 FK resolver tests: `54/54 PASS`
- MAP01_11 exact targeted EditMode: `492/492 PASS`
- MAP01_11 full project EditMode: `535/535 PASS`
- MAP01_11 compile error / relevant new warning: `0 / 0`
- MAP01_11 new Runtime C#/test C#/meta: `7 / 1 / 8`, GUID duplicate `0`
- MAP01_11 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP01_12 Registry focused tests: `47 PASS`
- MAP01_12 targeted EditMode: `562/562 PASS`
- MAP01_12 full project EditMode: `582/582 PASS`
- MAP01_12 compile error / relevant new warning: `0 / 0`
- MAP01_12 new Runtime C#/test C#/meta: `6 / 1 / 7`, GUID duplicate `0`
- MAP01_12 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP01_13 content hash focused tests: `54 PASS`
- MAP01_13 targeted EditMode: `616/616 PASS`
- MAP01_13 full project EditMode: `636/636 PASS`
- MAP01_13 compile error / relevant new warning: `0 / 0`
- MAP01_13 new Runtime C#/test C#/meta: `5 / 1 / 6`, GUID duplicate `0`
- MAP01_13 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP01_14 atomic publish/report tests: `55/55 PASS`
- MAP01_14 targeted EditMode: `671/671 PASS`
- MAP01_14 full project EditMode: `691/691 PASS`
- MAP01_14 compile error / relevant new warning: `0 / 0`
- MAP01_14 new Runtime C#/test C#/meta: `7 / 1 / 8`, GUID duplicate `0`
- MAP01_14 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP01_15 CSV import window focused: `48/48 PASS`
- MAP01_15 targeted EditMode: `764/764 PASS`
- MAP01_15 full project EditMode: `784/784 PASS`
- MAP01_15 actual import: exact 50 files, issues `0`, published `true`, version `1`
- MAP01_15 ContentVersionHash: `1c41b14c2734200999e779ad1317c5bc2ef5208da1c3b4bc30347e47182cfeaf`
- MAP01_15 compile error / relevant warning: `0 / 0`
- MAP01_15 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP01_16 failure fixture focused: `37/37 PASS`
- MAP01_16 targeted EditMode: `801/801 PASS`
- MAP01_16 full project EditMode: `821/821 PASS`
- MAP01_16 compile error / relevant warning: `0 / 0`
- MAP01_16 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP01_16 new test support/test/meta: `1 / 1 / 2`, GUID duplicate `0`
- MAP01_17 Battery typed definitions: `5/5`, required Registry IDs: `25/25`
- MAP01_17 CSV ERROR/WARNING/FK failure: `0/0/0`
- MAP01_17 Microchunk/Registry focused: `150/150 PASS`
- MAP01_17 fixture + exit audit: `77/77 PASS` = `37/37 + 40/40`
- MAP01_17 targeted EditMode: `867/867 PASS`
- MAP01_17 full project EditMode: `887/887 PASS`
- MAP01_17 actual import: `PUBLISHED`, exact `50` files, issues `0`, stable hash
- MAP01_17 compile error / relevant warning: `0 / 0`
- MAP01_17 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP01 phase gate: `APPROVED`
- MAP02_01 generated world data focused: `56/56 PASS`
- MAP02_01 targeted EditMode: `923/923 PASS`
- MAP02_01 full project EditMode: `943/943 PASS`
- MAP02_01 exact 169-cell snapshot / 13-column CSV v1 bytes: `PASS`
- MAP02_01 compile error / warning / Console error / warning: `0 / 0 / 0 / 0`
- MAP02_01 new Runtime/test/meta: `4 / 1 / 5`, GUID duplicate `0`
- MAP02_01 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP02_02 deterministic RNG stream focused: `103/103 PASS`
- MAP02_02 required InitialState/first/second known vectors: `6/6 PASS each`
- MAP02_02 targeted EditMode: `1026/1026 PASS`
- MAP02_02 full project EditMode: `1046/1046 PASS`
- MAP02_02 compile error / warning / Console error / warning: `0 / 0 / 0 / 0`
- MAP02_02 new Runtime/test/matching meta: `6 / 1 / 7`
- MAP02_02 accepted legacy Editor folder meta: `6/6`, final Assets meta `2954`, duplicate GUID `0`
- MAP02_02 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP02_03 grid initialization focused: `90/90 PASS`
- MAP02_03 exact grid/topology: `169 cells / 624 directed links / 312 undirected edges / 1 component`
- MAP02_03 targeted EditMode: `1116/1116 PASS`
- MAP02_03 full project EditMode: `1136/1136 PASS`
- MAP02_03 compile error / warning / Console error / warning: `0 / 0 / 0 / 0`
- MAP02_03 new Runtime/test/matching meta: `4 / 1 / 5`
- MAP02_03 final Assets meta: `2959`, accepted legacy Editor folder meta `6/6`, duplicate GUID `0`
- MAP02_03 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP02_04 WorldGenerationRoot focused: `84/84 PASS`
- MAP02_04 exact starter plan / four failure policies / retry semantics / artifact transaction: `PASS`
- MAP02_04 targeted EditMode: `1200/1200 PASS`
- MAP02_04 full project EditMode: `1220/1220 PASS`
- MAP02_04 compile error / warning / Console error / warning: `0 / 0 / 0 / 0`
- MAP02_04 new Runtime/test/matching meta: `7 / 1 / 8`
- MAP02_04 final Assets meta: `2967`, accepted legacy Editor folder meta `6/6`, duplicate GUID `0`
- MAP02_04 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP02_05 pass execution record focused: `77/77 PASS`
- MAP02_05 immutable root/pass/attempt record, UTC/monotonic clock, retry/failure projection: `PASS`
- MAP02_05 targeted EditMode: `1277/1277 PASS`
- MAP02_05 full project EditMode: `1297/1297 PASS`
- MAP02_05 compile error / warning / Console error / warning: `0 / 0 / 0 / 0`
- MAP02_05 new Runtime/test/matching meta and existing Runtime modification: `5 / 1 / 6 / 1`
- MAP02_05 exact Assets changes: `13`, unexpected `0`
- MAP02_05 final Assets meta: `2973`, accepted legacy Editor folder meta `6/6`, duplicate GUID `0`
- MAP02_05 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP02_06 seed manifest/replay focused: `97/97 PASS`
- MAP02_06 exact P00 two-file bundle / atomic publisher / ordered one-call replay: `PASS`
- MAP02_06 ContentVersionHash focused confirmation: `54/54 PASS`
- MAP02_06 targeted EditMode: `1374/1374 PASS`
- MAP02_06 full project EditMode: `1394/1394 PASS`
- MAP02_06 compile error / warning / Console error / warning: `0 / 0 / 0 / 0`
- MAP02_06 new Runtime/test/matching meta: `7 / 1 / 8`
- MAP02_06 exact Assets changes: `16`, existing modifications `0`, unexpected `0`
- MAP02_06 final Assets meta: `2981`, accepted legacy Editor folder meta `6/6`, duplicate GUID `0`
- MAP02_06 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP02_07 topology overlay focused: `88/88 PASS`
- MAP02_07 immutable 169-cell snapshot / shared Game-Scene renderer / orientation and hover: `PASS`
- MAP02_07 combined existing MAP02 focused: `507/507 PASS`
- MAP02_07 targeted EditMode: `1442/1442 PASS`
- MAP02_07 full project EditMode: `1482/1482 PASS`
- MAP02_07 visual checklist: `12/12 PASS`
- MAP02_07 compile error / warning / Console error / warning: `0 / 0 / 0 / 0`
- MAP02_07 new Runtime/Editor/runtime-test/editor-test/matching meta: `4 / 1 / 1 / 1 / 7`
- MAP02_07 exact Assets changes: `14`, existing modifications `0`, unexpected `0`
- MAP02_07 final Assets meta: `2988`, accepted legacy Editor folder meta `6/6`, duplicate GUID `0`
- MAP02_07 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP02_08 new exit fixture: `72/72 PASS`
- MAP02_08 existing MAP02 focused aggregate: `595/595 PASS`
- MAP02_08 MAP02 phase focused aggregate: `667/667 PASS`
- MAP02_08 ContentVersionHash focused: `54/54 PASS`
- MAP02_08 targeted EditMode: `1514/1514 PASS`
- MAP02_08 full project EditMode: `1554/1554 PASS`
- MAP02_08 exact grid/topology/RNG/root/manifest/replay/100-run static identity: `PASS`
- MAP02_08 current-project visual checklist: `12/12 PASS`
- MAP02_08 compile error / warning / Console error / warning: `0 / 0 / 0 / 0`
- MAP02_08 new Runtime test/matching meta: `1 / 1`
- MAP02_08 exact Assets changes: `2`, existing modifications `0`, unexpected `0`
- MAP02_08 final Assets meta: `2989`, accepted legacy Editor folder meta `6/6`, duplicate GUID `0`
- MAP02_08 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP02 phase gate: `APPROVED`
- MAP03_01 site reservation models focused: `81/81 PASS`
- MAP03_01 MAP02 phase focused aggregate: `667/667 PASS`
- MAP03_01 SpecialVillage/BiomeBoundary/StaticRegistry/ContentHash: `57/57 / 38/38 / 53/53 / 54/54 PASS`
- MAP03_01 targeted EditMode: `1595/1595 PASS`
- MAP03_01 full project EditMode: `1635/1635 PASS`
- MAP03_01 immutable reservation ID/token/footprint/entry/Core seed/169-sector snapshot: `PASS`
- MAP03_01 later-task production dependencies: `0`
- MAP03_01 compile error / warning / Console error / warning: `0 / 0 / 0 / 0`
- MAP03_01 new Runtime/test/matching meta: `8 / 1 / 9`
- MAP03_01 exact Assets changes: `18`, existing modifications `0`, unexpected `0`
- MAP03_01 final Assets meta: `2998`, accepted legacy Editor folder meta `6/6`, duplicate GUID `0`
- MAP03_01 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP03_02 raw-origin candidate focused: `268/268 PASS`
- MAP03_02 exact rings `48/40/32/24/16/8/1`, Start `88`, five sites `169/169`, total `933`: `PASS`
- MAP03_02 MAP03_01 reservation model regression: `81/81 PASS`
- MAP03_02 MAP02 phase focused aggregate: `667/667 PASS`
- MAP03_02 SpecialVillage/BiomeBoundary/StaticRegistry/ContentHash: `57/57 / 38/38 / 53/53 / 54/54 PASS`
- MAP03_02 targeted EditMode: `1863/1863 PASS`
- MAP03_02 full project EditMode: `1903/1903 PASS`
- MAP03_02 later-task transform/placement/distance/cost/RNG/backtracking dependencies: `0`
- MAP03_02 compile error / warning / Console error / warning: `0 / 0 / 0 / 0`
- MAP03_02 new Runtime/test/matching meta: `6 / 1 / 7`
- MAP03_02 exact Assets changes: `14`, existing modifications `0`, unexpected `0`
- MAP03_02 final Assets meta: `3005`, accepted legacy Editor folder meta `6/6`, duplicate GUID `0`
- MAP03_02 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP03_03 footprint placement solver focused: `170/170 PASS`
- MAP03_03 exact transforms/asymmetric coordinate table/side table: `PASS`
- MAP03_03 starter evaluations/success/rejections: `3468 / 3156 / 312 PASS`
- MAP03_03 rejection breakdown FootprintOutsideWorld/EntryOutsideWorld/other: `52 / 260 / 0`
- MAP03_03 MAP03_02 candidate regression: `268/268 PASS`
- MAP03_03 MAP03_01 reservation model regression: `81/81 PASS`
- MAP03_03 MAP02 phase focused aggregate: `667/667 PASS`
- MAP03_03 SpecialVillage/BiomeBoundary/StaticRegistry/ContentHash: `57/57 / 38/38 / 53/53 / 54/54 PASS`
- MAP03_03 targeted EditMode: `2033/2033 PASS`
- MAP03_03 full project EditMode: `2073/2073 PASS`
- MAP03_03 later-task distance/cost/RNG/selection/backtracking/capacity/village/pass dependencies: `0`
- MAP03_03 compile error / warning / Console error / warning: `0 / 0 / 0 / 0`
- MAP03_03 new Runtime/test/matching meta: `6 / 1 / 7`
- MAP03_03 exact Assets changes: `14`, existing modifications `0`, unexpected `0`
- MAP03_03 final Assets meta: `3012`, accepted legacy Editor folder meta `6/6`, duplicate GUID `0`
- MAP03_03 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP03_04 site distance index focused: `239/239 PASS`
- MAP03_04 exhaustive sector-pair reference comparisons: `28561/28561 PASS`
- MAP03_04 exact six keys / pair records / constraints: `6 / 15 / 15 PASS`
- MAP03_04 minimum-distance distribution `2×5 / 3×9 / 4×1`: `PASS`
- MAP03_04 passing synthetic set / exact-threshold / deficit vectors: `PASS`
- MAP03_04 MAP03_03 placement regression: `170/170 PASS`
- MAP03_04 MAP03_02 candidate regression: `268/268 PASS`
- MAP03_04 MAP03_01 reservation model regression: `81/81 PASS`
- MAP03_04 MAP02 phase focused aggregate: `667/667 PASS`
- MAP03_04 SpecialVillage/BiomeBoundary/StaticRegistry/ContentHash: `57/57 / 38/38 / 53/53 / 54/54 PASS`
- MAP03_04 targeted EditMode: `2272/2272 PASS`
- MAP03_04 full project EditMode: `2312/2312 PASS`
- MAP03_04 later-task cost/RNG/selection/backtracking/capacity/village/pass/route/tile-distance dependencies: `0`
- MAP03_04 compile error / warning / Console error / warning: `0 / 0 / 0 / 0`
- MAP03_04 new Runtime/test/matching meta: `7 / 1 / 8`
- MAP03_04 exact Assets changes: `16`, existing modifications `0`, unexpected `0`
- MAP03_04 final Assets meta: `3020`, accepted legacy Editor folder meta `6/6`, duplicate GUID `0`
- MAP03_04 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP03_05 site candidate cost focused: `270/270 PASS`
- MAP03_05 exact default weights altitude/edge/distance/capacity/cluster: `10 / 25 / 1000 / 100 / 10000 PASS`
- MAP03_05 aggregate units `2/1/1/1/1`, total cost `11145`, hard false: `PASS`
- MAP03_05 exact starter biome/Core-rule identity and component vectors: `PASS`
- MAP03_05 MAP03_04 distance regression: `239/239 PASS`
- MAP03_05 MAP03_03 placement regression: `170/170 PASS`
- MAP03_05 MAP03_02 candidate regression: `268/268 PASS`
- MAP03_05 MAP03_01 reservation model regression: `81/81 PASS`
- MAP03_05 MAP02 phase focused aggregate: `667/667 PASS`
- MAP03_05 SpecialVillage/BiomeBoundary/StaticRegistry/ContentHash: `57/57 / 38/38 / 53/53 / 54/54 PASS`
- MAP03_05 targeted EditMode: `2542/2542 PASS`
- MAP03_05 full project EditMode: `2582/2582 PASS`
- MAP03_05 later-task ranking/RNG/selection/backtracking/flood/village/pass dependencies: `0`
- MAP03_05 compile error / warning / Console error / warning: `0 / 0 / 0 / 0`
- MAP03_05 new Runtime/test/matching meta: `6 / 1 / 7`
- MAP03_05 exact Assets changes: `14`, existing modifications `0`, unexpected `0`
- MAP03_05 final Assets meta: `3027`, accepted legacy Editor folder meta `6/6`, duplicate GUID `0`
- MAP03_05 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP03_06 reservation backtracking focused: `248/248 PASS`
- MAP03_06 exact six-group source options / selected placements: `3156 / 6 PASS`
- MAP03_06 full starter seeds `0 / 4660 / ulong.MaxValue`: `3/3 COMPLETE`
- MAP03_06 per-option RNG tie-break draws / final distance records / constraints: `3156 / 15 / 15 PASS`
- MAP03_06 failed-combination default maximum / custom limit: `200 / 1 PASS`
- MAP03_06 MAP03_05 cost regression: `270/270 PASS`
- MAP03_06 MAP03_04 distance regression: `239/239 PASS`
- MAP03_06 MAP03_03 placement regression: `170/170 PASS`
- MAP03_06 MAP03_02 candidate regression: `268/268 PASS`
- MAP03_06 MAP03_01 reservation model regression: `81/81 PASS`
- MAP03_06 MAP02 phase focused aggregate: `667/667 PASS`
- MAP03_06 SpecialVillage/BiomeBoundary/StaticRegistry/ContentHash: `57/57 / 38/38 / 53/53 / 54/54 PASS`
- MAP03_06 targeted EditMode: `2790/2790 PASS`
- MAP03_06 full project EditMode: `2830/2830 PASS`
- MAP03_06 later-task capacity flood/Village/final snapshot/pass dependencies: `0`
- MAP03_06 compile error / warning / Console error / warning: `0 / 0 / 0 / 0`
- MAP03_06 new Runtime/test/matching meta: `8 / 1 / 9`
- MAP03_06 exact Assets changes: `18`, existing modifications `0`, unexpected `0`
- MAP03_06 final Assets meta: `3036`, accepted legacy Editor folder meta `6/6`, duplicate GUID `0`
- MAP03_06 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP03_07 core capacity flood focused: `215/215 PASS`
- MAP03_07 exact capacity requirements / witnesses / total witness sectors / overlap: `4 / 4 / 20 / 0 PASS`
- MAP03_07 full starter seeds `0 / 4660 / ulong.MaxValue`: `3/3 Completed`
- MAP03_07 starter selected placements / RNG before-after / Village: `6 / 3156->3156 / 0 PASS`
- MAP03_07 MAP03_06 backtracker regression: `248/248 PASS`
- MAP03_07 MAP03_05 cost regression: `270/270 PASS`
- MAP03_07 MAP03_04 distance regression: `239/239 PASS`
- MAP03_07 MAP03_03 placement regression: `170/170 PASS`
- MAP03_07 MAP03_02 candidate regression: `268/268 PASS`
- MAP03_07 MAP03_01 reservation model regression: `81/81 PASS`
- MAP03_07 MAP02 phase focused aggregate: `667/667 PASS`
- MAP03_07 SpecialVillage/BiomeBoundary/StaticRegistry/ContentHash: `57/57 / 38/38 / 53/53 / 54/54 PASS`
- MAP03_07 targeted EditMode: `3005/3005 PASS`
- MAP03_07 full project EditMode: `3045/3045 PASS`
- MAP03_07 later-task Village/CoreBiomeSeed/final reservation/biome growth/pass dependencies: `0`
- MAP03_07 compile error / warning / Console error / warning: `0 / 0 / 0 / 0`
- MAP03_07 new Runtime/test/matching meta: `8 / 1 / 9`
- MAP03_07 exact Assets changes: `18`, existing modifications `0`, unexpected `0`
- MAP03_07 final Assets meta: `3045`, accepted legacy Editor folder meta `6/6`, duplicate GUID `0`
- MAP03_07 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP03_08 Village reservation focused: `339/339 PASS`
- MAP03_08 exact bucket roll distribution `20/50/30` and strict source string: `PASS`
- MAP03_08 starter raw/source/entry-out candidate counts: `676 / 624 / 52 PASS`
- MAP03_08 full starter seeds `0 / 4660 / ulong.MaxValue`: `3/3 Completed`
- MAP03_08 starter existing+Village / witnesses / witness sectors / overlap / entry conflict: `6+1 / 4 / 20 / 0 / 0 PASS`
- MAP03_08 continued RNG draws / method calls: `3156->3159 / 3 PASS`
- MAP03_08 MAP03_07 capacity regression: `215/215 PASS`
- MAP03_08 MAP03_06 backtracker regression: `248/248 PASS`
- MAP03_08 MAP03_05 cost regression: `270/270 PASS`
- MAP03_08 MAP03_04 distance regression: `239/239 PASS`
- MAP03_08 MAP03_03 placement regression: `170/170 PASS`
- MAP03_08 MAP03_02 candidate regression: `268/268 PASS`
- MAP03_08 MAP03_01 reservation model regression: `81/81 PASS`
- MAP03_08 MAP02 phase focused aggregate: `667/667 PASS`
- MAP03_08 SpecialVillage/BiomeBoundary/StaticRegistry/ContentHash: `57/57 / 38/38 / 53/53 / 54/54 PASS`
- MAP03_08 targeted EditMode: `3344/3344 PASS`
- MAP03_08 full project EditMode: `3384/3384 PASS`
- MAP03_08 later-task final snapshot/facility/biome/pass/root/file-I/O dependencies: `0`
- MAP03_08 compile error / warning / Console error / warning: `0 / 0 / 0 / 0`
- MAP03_08 new Runtime/test/matching meta: `8 / 1 / 9`
- MAP03_08 exact Assets changes: `18`, existing modifications `0`, unexpected `0`
- MAP03_08 final Assets meta: `3054`, accepted legacy Editor folder meta `6/6`, duplicate GUID `0`
- MAP03_08 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP03_09 Site reservation validator focused: `268/268 PASS`
- MAP03_09 exact validation rules: `6/6 PASS`
- MAP03_09 reservations / sector rows / reserved / unreserved: `7 / 169 / 8 / 161 PASS`
- MAP03_09 entries / witnesses / witness sectors / Core seeds: `6 / 4 / 20 / 4 PASS`
- MAP03_09 non-Village/Village/cluster checks: `15 / 6 / 1 PASS`
- MAP03_09 starter seeds `0 / 4660 / ulong.MaxValue`: `3/3 Completed`
- MAP03_09 RNG consumed: `0`
- MAP03_09 MAP03_08 Village regression: `339/339 PASS`
- MAP03_09 MAP03_07 capacity regression: `215/215 PASS`
- MAP03_09 MAP03_06 backtracker regression: `248/248 PASS`
- MAP03_09 MAP03_05 cost regression: `270/270 PASS`
- MAP03_09 MAP03_04 distance regression: `239/239 PASS`
- MAP03_09 MAP03_03 placement regression: `170/170 PASS`
- MAP03_09 MAP03_02 candidate regression: `268/268 PASS`
- MAP03_09 MAP03_01 reservation model regression: `81/81 PASS`
- MAP03_09 MAP02 phase focused aggregate: `667/667 PASS`
- MAP03_09 SpecialVillage/BiomeBoundary/StaticRegistry/ContentHash: `57/57 / 38/38 / 53/53 / 54/54 PASS`
- MAP03_09 targeted EditMode: `3612/3612 PASS`
- MAP03_09 full project EditMode: `3652/3652 PASS`
- MAP03_09 later-task overlay/batch/biome/pass/root/file-I/O dependencies: `0`
- MAP03_09 compile error / warning / Console error / warning: `0 / 0 / 0 / 0`
- MAP03_09 new Runtime/test/matching meta: `8 / 1 / 9`
- MAP03_09 exact Assets changes: `18`, existing modifications `0`, unexpected `0`
- MAP03_09 final Assets meta: `3063`, accepted legacy Editor folder meta `6/6`, duplicate GUID `0`
- MAP03_09 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP03_10 runtime/editor/combined overlay focused: `133/133 / 28/28 / 161/161 PASS`
- MAP03_10 MAP03_01~09 aggregate: `2098/2098 PASS`
- MAP03_10 overlay cells / reservations / reserved / entries: `169 / 7 / 8 / 6 PASS`
- MAP03_10 Core witnesses / witness sectors / overlap: `4 / 20 / 0 PASS`
- MAP03_10 validation rules / diagnostic rows: `6 / 16 PASS`
- MAP03_10 visual checklist: `18/18 PASS`
- MAP03_10 targeted EditMode: `3745/3745 PASS`
- MAP03_10 full project EditMode: `3813/3813 PASS`
- MAP03_10 compile error / relevant warning / Console error: `0 / 0 / 0`
- MAP03_10 new Runtime/Editor/test/matching meta: `4+1 / 1+1 / 7`
- MAP03_10 exact Assets changes: `14`, existing modifications `0`, unexpected `0`
- MAP03_10 final Assets meta: `3070`, accepted legacy Editor folder meta `6/6`, duplicate GUID `0`
- MAP03_10 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP03_11 exit fixture discovery: `104`, reduced non-full `103/103 PASS`, reduced full batch `1/1 PASS` over 1,000 seeds
- MAP03_11 Village distribution: `2007 / 4945 / 3048` over 10,000 seeds, outside `0`, chi-square `1.3975`
- MAP03_11 determinism/isolation seeds: `102`; full-batch unresolved / invalid / retry: `0 / 0 / 0`
- MAP03_11 MAP03 phase focused by baseline addition: `2363`; Game.Map targeted by baseline addition: `3849`
- MAP03_11 current Unity EditMode discovery: `3918`; original large aggregate suites were not rerun under the user-directed 1/10 profile
- MAP03_11 current overlay revalidation: `169` cells, `7` reservations, `8` reserved sectors, `6` entries, `4/20` Core witnesses/sectors, `6` rules
- MAP03_11 compile error / relevant warning: `0 / 0`; Scene dirty `False -> False`; transient residue `0`
- MAP03_11 new test/matching meta: `1 / 1`; exact Assets changes `2`, existing modifications `0`, unexpected `0`
- MAP03_11 final Assets meta: `3071`, accepted legacy Editor folder meta `6/6`, duplicate GUID `0`
- MAP03_11 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP03 EXIT: `APPROVED`; MAP04 entry: `ELIGIBLE FOR SEPARATE PATCH`
- MAP04_01 immutable biome-patch model focused: `107/107 PASS`
- MAP04_01 required five regression fixtures: `496/496 PASS`; actually executed final validation `603/603 PASS`
- MAP04_01 Game.Map targeted discovery arithmetic: `3956`; full EditMode current discovery: `4025`; large suites not executed under the user-directed reduced profile
- MAP04_01 new Runtime/test/matching meta: `7 / 1 / 8`; exact Assets changes `16`, existing modifications `0`, unexpected `0`
- MAP04_01 compile error / Console error / relevant new warning: `0 / 0 / 0`
- MAP04_01 final Assets meta: `3079`, accepted legacy Editor folder meta `6/6`, duplicate GUID `0`
- MAP04_01 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP04_02 Core patch seed initializer focused: `121/121 PASS`
- MAP04_02 required regressions: `522/522 PASS`; actually executed required total: `643/643 PASS`
- MAP04_02 starter initialization: `4 patches / 4 bindings / 4 seed cells / 4 assigned / 165 unassigned / RNG draws 0`
- MAP04_02 Game.Map targeted discovery arithmetic: `4077`; full EditMode current discovery: `4146`; large suites not executed under the user-directed reduced profile
- MAP04_02 new Runtime/test/matching meta: `6 / 1 / 7`; exact Assets changes `14`, existing modifications `0`, unexpected `0`
- MAP04_02 compile error / Console error / relevant new warning: `0 / 0 / 0`
- MAP04_02 final Assets meta: `3086`, accepted legacy Editor folder meta `6/6`, duplicate GUID `0`
- MAP04_02 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP04_03 Core patch grower focused: `127/127 PASS`
- MAP04_03 required regressions: `443/443 PASS`; actually executed required total: `570/570 PASS`
- MAP04_03 starter growth: `4 patches / 20 assigned / 149 unassigned / mandatory +16 / supplemental 0 / RNG 0`
- MAP04_03 reservation intrusion / cross-patch overlap: `0 / 0`
- MAP04_03 Game.Map targeted discovery arithmetic: `4204`; full EditMode current discovery: `4273`; large suites not executed under the user-directed reduced profile
- MAP04_03 new Runtime/test/matching meta: `6 / 1 / 7`; exact Assets changes `14`, existing modifications `0`, unexpected `0`
- MAP04_03 compile error / Console error / relevant new warning: `0 / 0 / 0`
- MAP04_03 final Assets meta: `3093`, accepted legacy Editor folder meta `6/6`, duplicate GUID `0`
- MAP04_03 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP04_04 Satellite seed placer focused: `141/141 PASS`
- MAP04_04 required regressions: `458/458 PASS`; actually executed required total: `599/599 PASS`
- MAP04_04 starter placement: desired `CRATER/DOUGH/MILL/ROOT = 2/0/2/3`, patches `4->11`, assigned `20->27`, unassigned `149->142`
- MAP04_04 RNG method/raw draws: count `4`, candidate `9`, total `13`; DrawCount `0->13`
- MAP04_04 raw candidates / edge rejects / distance rejects / reservation intrusion / overlap: `145 / 2 / 0 / 0 / 0`
- MAP04_04 Game.Map targeted discovery arithmetic: `4345`; full EditMode current discovery: `4414`; large suites not executed under the user-directed reduced profile
- MAP04_04 new Runtime/test/matching meta: `7 / 1 / 8`; exact Assets changes `16`, existing modifications `0`, unexpected `0`
- MAP04_04 compile error / Console error / relevant new warning: `0 / 0 / 0`
- MAP04_04 final Assets meta: `3101`, accepted legacy Editor folder meta `6/6`, duplicate GUID `0`
- MAP04_04 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP04_05 multi-seed biome grower focused: `164/164 PASS`
- MAP04_05 required regressions: `478/478 PASS`; actually executed required total: `642/642 PASS`
- MAP04_05 attempt-0 capacity: target/legal/shortfall `165/161/4`, RetryRequired, RNG `13->13`, mutation `0`
- MAP04_05 viable attempt: world seed `0x0123456789ABCDF9`, attempt `24`, Satellite count `3/3/1/3`
- MAP04_05 viable snapshot: `14 patches / 165 assigned / 4 reserved-unassigned`, target/noise `135 / 1890`
- MAP04_05 viable RNG / claims: `17->1907`, minimum/competitive/total `10/125/135`
- MAP04_05 final biome counts MILL/ROOT/CRATER/DOUGH: `23/31/59/52`, cap `59` each
- MAP04_05 overlap / disconnected / source mutation: `0 / 0 / 0`
- MAP04_05 Game.Map targeted discovery arithmetic: `4509`; full EditMode current discovery: `4577`; large suites not executed under the user-directed reduced profile
- MAP04_05 new Runtime/test/matching meta: `8 / 1 / 9`; exact Assets changes `18`, existing modifications `0`, unexpected `0`
- MAP04_05 compile error / Console error / relevant new warning: `0 / 0 / 0`
- MAP04_05 final Assets meta: `3110`, accepted legacy Editor folder meta `6/6`, duplicate GUID `0`
- MAP04_05 Authoring CSV/meta preservation: `50/50`, modified `0`
- MAP04_06: focused `156/156`, regressions `515/515`, actual `671/671`, failed/skipped `0/0`
- MAP04_06 viable: desired MILL/ROOT `1/2`, patches `14->17`, assigned/reserved-unassigned `165/4`, RNG `1907->1912`
- MAP04_06 integrity: violations/mutation `0`, Assets meta `3118`, exact changes `16`, existing/unexpected `0/0`, compile/Console/warning `0/0/0`
- MAP04_06 discovery-only targeted/full: `4665/4733`; Authoring CSV/meta `50/50`, duplicate GUID `0`
- MAP04_07: focused `127/127`, regressions `263/263`, actual `390/390`, failed/skipped `0/0`
- MAP04_07 viable: score `0/0/100 -> 0/0/100`, moves/protected anomalies `0/0`, patches/assigned/reserved-unassigned `17/165/4`
- MAP04_07 integrity: RNG `1912->1912`, violations/mutation `0`, Assets meta `3125`, exact changes `14`, existing/unexpected `0/0`
- MAP04_07 discovery-only targeted/full `4792/4861`; compile/Console/warning `0/0/0`, Authoring CSV/meta `50/50`, duplicate GUID `0`
- MAP04_08: focused `141/141`, regressions `290/290`, actual `431/431`, failed/skipped `0/0`
- MAP04_08 viable: patch/world rows `17/169`, assigned/unassigned `165/4`, patch sector sum `165`, SecondaryBiome non-empty `0`
- MAP04_08 bytes: patch/world `1956/16380`, SHA `7ccf1fc1…0543 / 07daa96f…ee1d`; filesystem/RNG/mutation `0/0/0`
- MAP04_08 integrity: Assets meta `3132`, exact changes `14`, existing/unexpected `0/0`, discovery-only `4933/5002`, compile/Console/warning `0/0/0`
- MAP04_09: focused `196/196`, regressions `248/248`, actual `444/444`, rules `15/15`, violations/errors `0/0`
- MAP04_09 viable: patches `17 = 4/10/3`, assigned/unassigned/sector sum `165/4/165`, Core bindings `4/4`, RNG/mutation `0/0`
- MAP04_09 bytes: patch/world `1956/16380`, SHA `7ccf1fc1…0543 / 07daa96f…ee1d`; discovery-only `5129/5197`
- MAP04_09 integrity: Assets meta `3140`, exact changes `16`, existing/unexpected `0/0`, compile/Console/warning `0/0/0`
- MAP04_10: runtime/editor/combined `150/24/174`, regressions `444/444`, actual `618/618`, failed/skipped `0/0`
- MAP04_10 snapshot: cells/patches `169/17`, roles `4/10/3`, assigned/unassigned `165/4`, rules `15`, source/RNG/mutation `0/0/0`
- MAP04_10 visual: Game/Scene `18/18 / 18/18`, viewport `1224x844`, residue/Scene dirty delta `0/0`
- MAP04_10 integrity: Assets meta `3147`, exact changes `14`, existing/unexpected `0/0`, full discovery `5357`, compile/Console/warning `0/0/0`
- MAP04_11 exit: `110/110 PASS`; focused `1464/1464`; actually executed `1574/1574`; failed/skipped `0/0`
- MAP04_11 1,000-world batch: Completed/PASS_SITE handoff/Invalid `49/951/0`; determinism seeds `102`; retry max ordinal `99`
- MAP04_11 overlay runtime/editor/combined: `155/155 / 28/28 / 183/183`; visual `18/18`
- MAP04_11 progress scene: exact root/adapter/tabs/status/clear/reload `12/12 PASS`; `MAP PROGRESS TEST SCENE: READY`
- MAP04_11 discovery-only Game.Map/full: `5365/5477`; compile/Console/warning `0/0/0`
- MAP04_11 final Assets meta `3152`, Authoring CSV/meta `50/50`, duplicate GUID `0`, existing/unexpected `0/0`
- MAP05_01 mandatory terminals: exact `7 = 1 Start + 6 SiteEntry`; terminal IDs/order/source identity PASS
- MAP05_01 tests: MandatoryTerminalBuilder `120/120`, SiteReservationValidator `268/268`, BiomePatchValidator `196/196`, Map04Exit `110/110`, actually executed `694/694`, failed/skipped `0/0`
- MAP05_01 final Assets meta `3161`, Authoring CSV/meta `50/50`, duplicate GUID `0`, exact changes `18`, existing/unexpected `0/0`, compile/Console/warning `0/0/0`
- MAP05_02 route mask lookup: exact `3 = Type1 + Type2 + Type3`; IDs `ROUTE_T1_LR / ROUTE_T2_LRD / ROUTE_T3_LRU`; Type0 ignored `12`
- MAP05_02 tests: MandatoryRouteMaskLookupBuilder `127/127`, required existing regression `694/694`, actually executed `821/821`, failed/skipped `0/0`
- MAP05_02 final Assets meta `3170`, Authoring CSV/meta `50/50`, duplicate GUID `0`, exact changes `18`, existing/unexpected `0/0`, compile/Console/warning `0/0/0`
- MAP05_03 connector tree: nodes/candidates/tree edges `7/21/6`; deterministic Kruskal and cost model PASS
- MAP05_03 tests: ConnectorTree `129/129`, RouteMaskLookup `127/127`, Terminal `120/120`, SiteReservation `268/268`, BiomePatch `196/196`, Map04Exit `110/110`, actually executed `950/950`, failed/skipped `0/0`
- MAP05_03 final Assets meta `3179`, Authoring CSV/meta `50/50`, duplicate GUID `0`, exact repair changes `1 existing test C#`, production/unexpected `0/0`, compile/Console/warning `0/0/0`
- MAP05_04 horizontal backbone: tree edges/segments `6/6`, same-row/different-row `2/4`, horizontal cells `28`, pending gateway anchors `8`, route graph/generated CSV `0/0`
- MAP05_04 tests: HorizontalBackbone `142/142`, ConnectorTree `129/129`, RouteMaskLookup `127/127`, Terminal `120/120`, SiteReservation `268/268`, BiomePatch `196/196`, Map04Exit `110/110`, actually executed `1092/1092`, failed/skipped `0/0`
- MAP05_04 final Assets meta `3188`, Authoring CSV/meta `50/50`, duplicate GUID `0`, exact changes `20`, existing production/unexpected `0/0`, compile/Console/warning `0/0/0`
- MAP05_05 tests: VerticalGatewayPlanner `156/156`, required regressions `1092/1092`, actually executed `1248/1248`, failed/skipped `0/0`
- MAP05_05 output: gateway pairs/anchors `4/8`, Type4 junctions `11`, conflict pending `0`, route graph/generated CSV/mask writes `0/0/0`
- MAP05_05 final Assets meta `3197`, Authoring CSV/meta `50/50`, duplicate GUID `0`, exact changes `21`, existing production/unexpected `0/0`, compile/Console/warning `0/0/0`
- MAP05_06 tests: UpDownConflictResolver `194/194`, required regressions `1248/1248`, actually executed `1442/1442`, failed/skipped `0/0`
- MAP05_06 output: Type4-expressible conflict/resolved/unresolved `0/0/0`, resolution pairs/total cost `0/0`, route-mask/graph/filesystem writes `0/0/0`
- MAP05_06 final Assets meta `3206`, Authoring CSV/meta `50/50`, duplicate GUID `0`, exact changes `22`, existing production/unexpected `0/0`, compile/Console/warning `0/0/0`
- MAP05_07 tests: MandatoryRouteLoopPlanner `212/212`, required regressions `1442/1442`, actually executed `1654/1654`, failed/skipped `0/0`
- MAP05_07 output: candidates/eligible `7/7`, accepted/independent loops `2/2`, shared cell/total cost `4/17`, graph/generated CSV/RouteMaskId writes `0/0/0`
- MAP05_07 final Assets meta `3215`, Authoring CSV/meta `50/50`, duplicate GUID `0`, exact changes `23`, existing production/unexpected `0/0`, compile/Console/warning `0/0/0`
- MAP05_09 tests: MandatoryRouteGraphValidator `298/298`, latest unique required suite `2289/2289`, actually executed `2414/2414`, failed/skipped `0/0`
- MAP05_09 output: validation rules `12/12/12`, graph nodes/directed edges/undirected edges/route cells `47/96/48/47`, terminals reachable `7/7`, accepted loops represented `2/2`
- MAP05_09 final Assets meta `3238`, Authoring CSV/meta `50/50`, duplicate GUID `0`, task-marker changes `26`, existing production/unexpected `0/0`, compile/Console/warning `0/0/0`
- MAP05_10 tests: overlay focused `168/168`, required regression `1206/1206`, actual final gates `1374/1374`, failed/skipped `0/0`
- MAP05_10 output: graph `47/96/48/47`, masks `20/4/4/17/0/0/2`, validation `12/12/12`, visual Game/Scene `9/9 + 9/9`
- MAP05_10 final Assets meta `3245`, Authoring CSV/meta `50/50`, duplicate GUID `0`, modified existing test C# from repair `4`, production modifications `0`, compile/Console/warning `0/0/0`
- MAP05_11 exit: MAP05 focused `1827/1827`, exit `132/132`, phase aggregate `1959/1959`, failed/skipped `0/0`
- MAP05_11 10,000-seed batch: completed/retry/unresolved/invalid `10000/0/0/0`; Type4 U+D/LR failures `0/0`; overlay snapshots `10000/10000`
- MAP05_11 visual Game/Scene `9/9 + 9/9`, graph `47/96/48/47`, masks `20/4/4/17/0/0/2`, validation `12/12/0/0/0`
- MAP05_11 final Assets meta `3247` using approved regenerated Diagnostics folder meta branch, Authoring CSV/meta `50/50`, duplicate GUID `0`, production/CSV/Scene/Prefab/asmdef modifications `0`
- MAP05 EXIT: `APPROVED`; MAP06 entry: `ELIGIBLE FOR SEPARATE PATCH`
- MAP06_01 optional region models: OptionalRegionModelsTests `194/194`, MAP05 aggregate `1959/1959`, actual required `2153/2153`, failed/skipped `0/0`
- MAP06_01 Unity gate: forced refresh/import/domain reload complete, compile/Console/relevant warnings `0/0/0`
- MAP06_01 asset/static baseline: OptionalRegion model/test C# `7/7`, matching meta `7/7`, Assets meta `3254`, Authoring CSV/meta `50/50`, duplicate GUID `0`
- MAP06_01 boundary repair preserved: MAP06_01 OptionalRegion model symbols are allowed; before MAP06_02 patch, MAP06_02+ production symbols remain locked/forbidden
- MAP06_02 optional attachment enumeration: raw probes/accepted `188/51`, rejection counters OOB/mandatory/terminal/site/biome/duplicate `20/96/0/4/0/17`, canonical digest `68b438c523645c2f6721fa0c104c3cd4c282076292cd2e035cd20a2b272aaee6`
- MAP06_02 tests: OptionalAttachmentEnumerator `202/202`, OptionalRegionModels `194/194`, MAP05 aggregate `1959/1959`, actual required `2355/2355`, failed/skipped `0/0`
- MAP06_02 Unity gate: forced refresh/import/domain reload complete, compile/Console/relevant warnings `0/0/0`
- MAP06_02 asset/static baseline: new C#/meta `7/7`, Assets meta `3261`, Authoring CSV/meta `50/50`, Authoring manifest `4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3`, duplicate GUID `0`
- MAP06_02 boundary repair preserved: MAP06_02 symbols are allowed; before MAP06_03 patch, MAP06_03+ production symbols remain locked/forbidden
- MAP06_03 optional region grower: source/attempted/accepted/rejected/limit-skipped `51/32/12/20/19`, accepted regions/cells `12/39`, depth buckets `5/0/2/5`, canonical digest `1f00f718bdb8d79fbf88923be0d81e0075987267c024cc624086ee6194998caa`
- MAP06_03 topology: connected `12/12`, exact-one mandatory bridge `12/12`, stored depth `39/39`, same-region L+R through `0`, all overlap categories `0`, source mutation `0`
- MAP06_03 tests: OptionalRegionGrower `234/234`, attachment `202/202`, models `194/194`, MAP05 aggregate `1959/1959`, actual required `2589/2589`, failed/skipped `0/0`
- MAP06_03 Unity gate: forced refresh/import/domain reload complete, compile/Console/relevant warnings `0/0/0`
- MAP06_03 asset/static baseline: new C#/meta `5/5`, Assets meta `3266`, Authoring CSV/meta `50/50`, Authoring manifest `4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3`, duplicate GUID `0`, generated CSV `0`
- MAP06_03 boundary advance: MAP06_03 symbols are allowed; before MAP06_04 patch, MAP06_04+ production symbols remained locked/forbidden
- MAP06_04 Type0 catalog: registered `12`, ignored non-Type0 `3`, catalog digest `a96d0c6860ea0ebf62ac9763efcb7a03fa61df932fde85b30cec76c4b0c50506`, source definition identity `12/12`
- MAP06_04 assignments: regions/cells/assignments `12/39/39`, internal reciprocal BaseEdges `30`, attachment base-closed `12`, mandatory base-open `0`, cross-region closed adjacency `13`, assignment digest `a26e73f25ff7267760a2507ec55b1acda3a6c39a8f3698fc37b190620ceca525`
- MAP06_04 tests: Type0RouteMaskAssigner `257/257`, MAP06 prior combined `630/630`, MAP05 aggregate `1959/1959`, actual required `2846/2846`, failed/skipped `0/0`
- MAP06_04 Unity gate: forced refresh/import/domain reload complete, compile/Console/relevant warnings `0/0/0`
- MAP06_04 asset/static baseline: new C#/meta `8/8`, Assets meta `3274`, Authoring CSV/meta `50/50`, Authoring manifest `4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3`, duplicate GUID `0`, generated CSV `0`
- MAP06_04 boundary advance: MAP06_04 symbols are allowed; before MAP06_05 patch, MAP06_05+ production symbols remained locked/forbidden

- MAP04 EXIT: `APPROVED`; MAP05 entry: `ELIGIBLE FOR SEPARATE PATCH`

## Current Rule

현재는 MAP06_05_ASSIGN_ACCESS_RULES_AND_CLUES 하나만 CURRENT다. MAP06_04는 PASS 및 COMPLETE 확정 기준으로만 상태 전환됐다. MAP06_06 이후 Task body는 읽거나 생성하거나 실행하지 않는다.

이번 Task는 MAP06_04 Type0 assignment를 입력으로 사용해 각 optional region의 base-closed attachment boundary에 Basic/Tool/Environment/Explosive/Hidden access rule, exact one perceptible clue, MAP06_06용 depth-based cost inputs를 immutable하게 예약하는 것까지만 수행한다. concrete edge signature/socket/generated edge/CSV와 reward tier는 만들지 않는다.

MAP06_05에서 새 `OptionalAccess*` production symbol이 생기므로, phase-boundary negative assertions는 MAP06_05 symbol을 허용하고 MAP06_06+ future symbols만 금지하도록 필요한 기존 boundary test 파일만 교정할 수 있다. 그 외 MAP05/MAP06_01~04 production graph/mask/models/assignments, Authoring/generated CSV/meta와 asmdef/Scene/Prefab/Package/ProjectSettings는 수정하지 않는다.

Mandatory route graph 기준은 고정이다: graph nodes/directed/undirected/route cells `47/96/48/47`, mask counts `20/4/4/17/0/0/2`, validation `12/12/0/0/0`. MAP05 Type4 규칙은 계속 보존한다: U+D mandatory, L/R actual adjacency preserved, UD/LUD/RUD/LRUD all legal.

MAP06_05가 PASS하면 MAP06_05만 COMPLETE, Current Task NONE으로 finalize한다. `MAP06_06_CALCULATE_OPTIONAL_REWARD_TIER`는 LOCKED로 유지하고 별도 patch 없이는 시작하지 않는다.


