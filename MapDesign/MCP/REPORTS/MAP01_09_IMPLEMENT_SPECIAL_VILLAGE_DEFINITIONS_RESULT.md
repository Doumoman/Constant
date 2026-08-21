# MAP01_09 Implement Special Village Definitions Result

## TASK

`MAP01_09_IMPLEMENT_SPECIAL_VILLAGE_DEFINITIONS`

## STATUS

STATUS: PASS

## SUMMARY

MAP01_06의 successful typed parse 결과에서 exact 12개 special-map/village/shop source를 compile-time typed immutable definition 12종과 deterministic `SpecialVillageDefinitionSet`으로 materialize하는 경계를 구현했다. 입력 gate는 source inventory, successful parse, exact schema, parsed/validated/source identity를 추적 검증하며, 오류가 하나라도 있으면 partial set을 publish하지 않는다.

## READ

- Mandatory Read Order의 전역 규칙, Master, Status, Current Task, MAP01_08 Result를 순서대로 확인했다.
- READ ALLOWLIST의 MAP01_02~08 schema/reader/validation/PK/parser/world-route/biome-boundary production API와 focused tests, importer test, architecture fixtures, asmdef를 확인했다.
- Authoring CSV data row, later Task, Legacy, Scene/Prefab 본문은 읽지 않았다.

## MASTER BACKLOG CHECK

- Master backlog rows: `205`
- `MAP00_01` through `MAP01_08`: `COMPLETE`
- `MAP01_09_IMPLEMENT_SPECIAL_VILLAGE_DEFINITIONS`: `CURRENT`
- `MAP01_10` and later: `LOCKED`
- Current Task before implementation: `TASKS/MAP01_09_IMPLEMENT_SPECIAL_VILLAGE_DEFINITIONS.md`

## MAP01_08 GATE CHECK

- MAP01_08 Result: `STATUS: PASS`
- Biome/boundary definitions: `36/36 PASS`
- World/route definitions: `59/59 PASS`
- Scalar/list parser: `97/97 PASS`
- Primary-key index: `32/32 PASS`
- Header/field validator: `29/29 PASS`
- RFC4180 reader: `31/31 PASS`
- Schema catalog: `23/23 PASS`
- Dictionary importer: `9/9 PASS`
- Architecture: `10/10 PASS`
- Previous targeted/full: `326/326`, `369/369 PASS`
- Previous compile errors/relevant warnings: `0/0`

## CREATED

Runtime production C#:

- `Assets/_Game/Map/Runtime/WorldGeneration/Data/SpecialMapDefinitions.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Data/VillageDefinitions.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Data/SpecialVillageDefinitionSource.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Data/SpecialVillageDefinitionSet.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Data/SpecialVillageDefinitionBuildError.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Data/SpecialVillageDefinitionBuildResult.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Data/SpecialVillageDefinitionBuilder.cs`

Runtime EditMode test:

- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/SpecialVillageDefinitionBuilderTests.cs`

Unity metadata:

- Exact corresponding `.cs.meta` files: `8`

Result:

- `MapDesign/MCP/REPORTS/MAP01_09_IMPLEMENT_SPECIAL_VILLAGE_DEFINITIONS_RESULT.md`

## PREEXISTING_IDENTICAL

`NONE`

All eight C# destinations were absent before implementation.

## SOURCE FILE CONTRACT

Only the following exact 12 filenames are accepted, each exactly once:

1. `event_activation_routes.csv`
2. `special_map_catalog.csv`
3. `special_map_entry_sockets.csv`
4. `special_map_footprint_cells.csv`
5. `special_map_rewards.csv`
6. `shop_archetypes.csv`
7. `shop_inventory_rules.csv`
8. `shopkeeper_species.csv`
9. `village_facilities.csv`
10. `village_layout_catalog.csv`
11. `village_layout_cells.csv`
12. `village_profiles.csv`

Null, missing, unexpected, duplicate, unsuccessful parse, schema mismatch, and field identity mismatch inputs produce deterministic accumulated errors and no definition set.

## DEFINITIONS IMPLEMENTED

- `EventActivationRouteDefinition`
- `SpecialMapDefinition`
- `SpecialMapEntrySocketDefinition`
- `SpecialMapFootprintCellDefinition`
- `SpecialMapRewardDefinition`
- `ShopArchetypeDefinition`
- `ShopInventoryRuleDefinition`
- `ShopkeeperSpeciesDefinition`
- `VillageFacilityDefinition`
- `VillageLayoutDefinition`
- `VillageLayoutCellDefinition`
- `VillageProfileDefinition`

Every exact schema column is exposed as a PascalCase typed property. Optional empty values, defaults and `UsedDefault`, enum tokens, list order/duplicates, inactive rows, and exact `CsvParsedRecord SourceRecord` identity are preserved. FK values remain unresolved strings, `start_distance_buckets` remains an opaque string, and no domain validation is performed.

The exact build error codes are `MissingSource`, `UnexpectedSource`, `DuplicateSource`, `UnsuccessfulParse`, `SchemaMismatch`, and `FieldMappingFailed`. Errors accumulate and sort by filename, record number, column order, and code while preserving nullable source locations; any error yields a null definition set.

## DEFINITION SET CONTRACT

- Ordinal sorted read-only dictionaries: `EventActivationRoutes`, `SpecialMaps`, `ShopArchetypes`, `ShopkeeperSpecies`, `VillageFacilities`, `VillageLayouts`, `VillageProfiles`
- Stable sorted read-only composite collections: `SpecialMapEntrySockets`, `SpecialMapFootprintCells`, `SpecialMapRewards`, `ShopInventoryRules`, `VillageLayoutCells`
- Stable read-only parent queries exist for entry sockets, footprint cells, and rewards by special-map ID; inventory rules by shop-archetype ID; layout cells by village-layout ID.
- Input source/row shuffling does not change membership or output ordering.
- Nested list payloads are copied into read-only collections.

## TEST

- New `SpecialVillageDefinitionBuilderTests`: `48/48 PASS` (required `>=48`), failed `0`, skipped `0`; job `de45274ad81f48ff86d91c0ae71255e7`
- Biome/boundary definitions: `36/36 PASS`
- World/route definitions: `59/59 PASS`
- Scalar/list parser: `97/97 PASS`
- Primary-key index: `32/32 PASS`
- Header/field validator: `29/29 PASS`
- RFC4180 reader: `31/31 PASS`
- Schema catalog: `23/23 PASS`
- Dictionary importer: `9/9 PASS`
- Architecture fixtures: `10/10 PASS`
- Targeted total: `374/374 PASS` (required `>=374`), failed `0`, skipped `0`; job `f3476d4baa9d4d0081c53e7570c28a05`
- Full project EditMode: `417/417 PASS` (required `>=417`), failed `0`, skipped `0`; job `7b5a245934c948ab82bfc52784a69a1f`
- PlayMode: `NOT RUN`

## UNITY

- Unity version: `6000.3.8f1`
- Instance: `Constant@ced6e0dfc4a31d45`
- Asset refresh: `PASS`
- Script compilation: `PASS`
- Compile errors: `0`
- Relevant new warnings after final clean refresh: `0`
- Scene/Prefab changes: `NONE`

## ASSET META VALIDATION

- New `.cs.meta`: `8/8` present and valid
- New GUIDs: `8/8` unique
- All project metadata after import: `2886`
- Invalid metadata files: `0`
- Global GUID duplicate groups: `0`

New GUIDs:

- `SpecialMapDefinitions.cs.meta`: `353a83f808e6cc24fb7faedc9ef6efcb`
- `VillageDefinitions.cs.meta`: `7095248518be2c1489c86a58fcd129d3`
- `SpecialVillageDefinitionSource.cs.meta`: `0b65f38e7f853e348ba36a3eab3760db`
- `SpecialVillageDefinitionSet.cs.meta`: `d1b809a7bff44b04fa36af2b5aa4fd18`
- `SpecialVillageDefinitionBuildError.cs.meta`: `b0d5a41189c4ae94c9e6695c7add8f2f`
- `SpecialVillageDefinitionBuildResult.cs.meta`: `325ad4138d2a282499ccb5cf556d5ccd`
- `SpecialVillageDefinitionBuilder.cs.meta`: `70619ed7902fb80449f6e12079569946`
- `SpecialVillageDefinitionBuilderTests.cs.meta`: `3e69e519d100a414c9c86f9a7f58566e`

## CHANGE SCOPE

- Existing active `_Game` C#: `74`, fingerprint before/after `52E9E2BD4BA248A27AEED4C61CD392EA2CDC6D4E157AB400AF4A8AF7C5B3DE75`
- Authoring CSV: `50`, fingerprint before/after `164FE578E28BB37FC125989FF6A9B8EE39CD286449DFD77089A46229370D69A4`, UTF-8 BOM `50/50`
- Authoring CSV metadata: `50`, fingerprint before/after `6B015D690451A263D15D2A47E9DFB5CFC1A93647C2FBF0F81702A854208DBC3E`
- Runtime/Editor/EditMode asmdef: `4`, fingerprint before/after `CD1009CC962C620BFFBC3156D2F05EE54E0B73426DDA006E33FAA7F0B4E3BC2F`
- Existing reader/schema/validator/PK/parser/world-route/biome-boundary/importer production and tests modified: `0`
- CSV, asmdef, Scene, Prefab, Package, ProjectSettings modified: `0`
- Task implementation writes are limited to the exact 7 runtime C#, 1 EditMode test, 8 matching metadata files, and this Result.

## OUT_OF_SCOPE_FINDINGS

`NONE`

No FK resolution, special/village/shop domain validation, `start_distance_buckets` parsing, event activation, placement/candidate selection, Registry, content hash, report/window, asset generation, or MAP01_10 implementation was added.

## DONE CONDITIONS

- [x] Current Task was MAP01_09 and Master has 205 rows with MAP01_08 COMPLETE/PASS.
- [x] Exact 12 source files only are accepted.
- [x] All 12 row definitions expose every schema column as typed compile-time properties.
- [x] Optional empty/default/list/enum/source provenance and inactive rows are preserved.
- [x] Seven ordinal dictionaries, five composite collections, query results, and nested payloads are immutable/read-only and input-order independent.
- [x] Parent queries follow the deterministic contract.
- [x] Build errors accumulate, sort deterministically, preserve nullable locations, and prevent partial publication.
- [x] FK IDs remain unresolved strings, `start_distance_buckets` remains opaque, and no domain validation runs.
- [x] No parsing-time event activation or placement/candidate selection was implemented.
- [x] Only the exact 7 runtime C#, 1 test C#, and 8 matching metadata files were created for implementation.
- [x] New metadata is valid and all project GUIDs are unique.
- [x] Existing runtime/editor/test code, CSV 50, CSV metadata 50, asmdefs, Scenes, Prefabs, Packages, and ProjectSettings are unchanged.
- [x] Unity refresh and compilation passed with errors/warnings `0/0`.
- [x] New `48/48`, targeted `374/374`, and full EditMode `417/417` passed.
- [x] PlayMode was not run or created.
- [x] Result contains the required sections and actual inventory.
- [x] MAP01_10 was not started.

## NEXT

- Finalize only `MAP01_09_IMPLEMENT_SPECIAL_VILLAGE_DEFINITIONS`: `CURRENT -> COMPLETE`, Current Task -> `NONE`.
- Do not unlock or start `MAP01_10`.
- Await the next MCP_INBOX patch.

## Recommended Commit

```text
feat(map): build immutable special village definitions
```
