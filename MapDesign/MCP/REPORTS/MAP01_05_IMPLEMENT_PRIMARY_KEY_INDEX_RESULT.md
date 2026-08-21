# MAP01_05 Implement Primary Key Index Result

## TASK

`MAP01_05_IMPLEMENT_PRIMARY_KEY_INDEX`

## STATUS

STATUS: PASS

## SUMMARY

- Implemented immutable structural CSV primary keys using exact ordinal, case-sensitive component vectors without delimiter concatenation, trimming, normalization, or typed parsing.
- Implemented file-scoped primary-key occurrence collection, deterministic unique-key lookup, and all-occurrence duplicate grouping.
- Duplicate input publishes no usable or partial index; unique/header-only input publishes a deterministic read-only index.
- Added `32` focused Runtime EditMode cases and passed the full `134/134` targeted regression.
- Did not implement scalar/list parsing, foreign-key resolution, domain definitions, Registry publication, content hashing, reporting UI, or MAP01_06-or-later behavior.

## READ

- Read the twelve Mandatory Read Order documents, including the MAP01_04 PASS Result.
- Read only the allowlisted MAP01_02 schema, MAP01_03 reader, and MAP01_04 validator Runtime production files needed to match the existing APIs.
- Read only allowlisted data-test, importer-test, architecture-test, and four Runtime/Editor/EditMode asmdef bodies.
- Used only permitted direct filenames, preservation hashes, Authoring CSV/meta path/hash/BOM projections, meta GUIDs, changed paths, Unity state, and Console projections for limited inspection.
- Did not read Authoring CSV domain contents, non-allowlisted C# bodies, Scene/Prefab YAML, Legacy bodies, or later Task bodies.

## MASTER BACKLOG CHECK

- Master backlog count: `205` task IDs.
- `MAP00_01` through `MAP00_10` and `MAP01_01` through `MAP01_04`: `COMPLETE`.
- `MAP01_05_IMPLEMENT_PRIMARY_KEY_INDEX`: sole `CURRENT` task during Task execution.
- `MAP01_06_IMPLEMENT_SCALAR_AND_LIST_PARSERS` and every later task remained `LOCKED / NOT STARTED`.

## MAP01_04 GATE CHECK

- MAP01_04 Result matched `MAP01_04_IMPLEMENT_HEADER_AND_FIELD_VALIDATION` and contained exact `STATUS: PASS`.
- Recorded baseline: validator `29/29`, reader `31/31`, schema `23/23`, importer `9/9`, architecture `10/10`, targeted `102/102`, compile errors `0`, relevant warnings `0`.
- Revalidated before patch execution with targeted EditMode job `dda66f0da621412f80e12d41d9909344`: `102 passed, 0 failed, 0 skipped`.
- Authoring CSV/meta baseline revalidated as `50/50`, with UTF-8 BOM missing `0`.

## CREATED

- `Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvPrimaryKey.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvPrimaryKeyOccurrence.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvDuplicatePrimaryKey.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvPrimaryKeyIndex.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvPrimaryKeyIndexBuildResult.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvPrimaryKeyIndexBuilder.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/CsvPrimaryKeyIndexBuilderTests.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvPrimaryKey.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvPrimaryKeyOccurrence.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvDuplicatePrimaryKey.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvPrimaryKeyIndex.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvPrimaryKeyIndexBuildResult.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvPrimaryKeyIndexBuilder.cs.meta`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/CsvPrimaryKeyIndexBuilderTests.cs.meta`
- `MapDesign/MCP/REPORTS/MAP01_05_IMPLEMENT_PRIMARY_KEY_INDEX_RESULT.md`

## PREEXISTING_IDENTICAL

NONE

## KEY CONTRACTS IMPLEMENTED

- `CsvPrimaryKey` copies a non-empty component vector into a read-only collection and implements structural equality, hashing, and ordering component by component with ordinal semantics.
- Component boundaries and component count are part of identity; composite keys never use a delimiter-joined identity string.
- `CsvPrimaryKeyIndexBuilder` takes one `CsvFileSchema` and its successful `CsvHeaderFieldValidationResult`; the two-argument overload uses the schema filename as source name and the three-argument overload preserves an explicit source name.
- PK fields are selected by `CsvFileSchema.PrimaryKeyColumns` order, which is `PrimaryKeyOrder`, rather than physical column order.
- Every component is the exact MAP01_04 `EffectiveValue`; whitespace, case, leading zeroes, defaults, and delimiter-like characters remain unchanged.
- Null input, unsuccessful validation, schema/field/source mismatch, zero PK columns, and an empty effective PK component are rejected with no published index.
- Header-only successful input creates a successful zero-entry index.
- `CsvPrimaryKeyIndex` provides read-only deterministic key enumeration and `TryGet` lookup for exactly one schema file.

## DUPLICATE CONTRACTS IMPLEMENTED

- Every occurrence preserves source name, schema filename, structural key, source record, validated record, representative first-PK-field location, and every PK field in `PrimaryKeyOrder`.
- Duplicate occurrences are ordered by source record and exact physical location; no placeholder negative location is used.
- Two occurrences report both rows, and three or more occurrences report the first and every later row in one group.
- All duplicate keys are collected; groups are ordered by component-wise ordinal lexicographic key order.
- Any duplicate group makes the build unsuccessful and publishes a null index, including when other unique rows were already collected.
- Index, key component, occurrence field, duplicate occurrence, and result duplicate collections are copied and read-only.

## TEST

- `CsvPrimaryKeyIndexBuilderTests`: `32/32 PASS`; isolated job `284223715c3642a7aa7f91451029c398`.
- `CsvHeaderAndFieldValidatorTests`: `29/29 PASS`.
- `Rfc4180CsvReaderTests`: `31/31 PASS`.
- `CsvSchemaCatalogTests`: `23/23 PASS`.
- `CsvSchemaDictionaryImporterTests`: `9/9 PASS`.
- Architecture fixtures: `3 + 3 + 4 = 10/10 PASS`.
- Combined targeted EditMode job `7ef99fea70294f29a0133c4632f3939f`: `134 passed, 0 failed, 0 skipped` (`Passed`, duration `2.3154387s`).
- PlayMode: `NOT RUN`.

## UNITY

- Unity version: `6000.3.8f1`.
- Active instance: `Constant@ced6e0dfc4a31d45`.
- Asset Refresh: `PASS`.
- Final Editor state after forced refresh/compile: idle, not compiling, no pending domain reload, no asset update, ready for tools.
- Compile errors: `0`; relevant new warnings: `0`; final error/warning Console entries: `0`.
- Targeted EditMode: `134/134 PASS`.
- PlayMode: `NOT RUN`.
- Scene/Prefab changes: `NONE`.

## ASSET META VALIDATION

- New `.cs.meta`: `7/7` present; missing: `0`.
- New GUID format: `7/7` valid lowercase 32-hex values.
- New GUID uniqueness: `7/7`; duplicate new GUIDs: `0`.
- New GUIDs: `5dfc7ffee7b04dea996417af2134e246`, `c98655a86f2940fd8289da43b012f599`, `48066b46e8e54e318c53b953aca8e1ec`, `e93c3dca17d444418d53dae648dbe38e`, `e2db986ac5bd41a0818312ba1aba7656`, `0db37f2bea3c4ed494f37b9e057df05b`, `01a61e2767044b54a3b8970c19081728`.
- Project meta/GUID count: `2853/2853`; project duplicate GUID groups: `0`.
- Existing `.meta` files were not modified by this Task.

## CHANGE SCOPE

- Exact new Asset paths: expected `14`, actual `14`, missing `0`, unexpected `0`.
- New Runtime production C#: `6`; new Runtime EditMode test C#: `1`; modified existing C#: `0`.
- Active `_Game` C# inventory changed from `42` to `49` only by the seven allowlisted files.
- Non-Task active C# snapshot remained `42|0F8B591CD80DBC561D73FA23C75CC174F40A345FD852EFA8ECF6DCCBBD2BAD28`.
- Task C# snapshot: `7|A4D017ED72C3354DDE45794611BE51EF3C1AFB5CEE365C268A7FAA99D53AA4F6`.
- MAP01_02 schema C# snapshot remained `8|886BA154335A61EE850F0EFF2B4D1CCA3650C5D28927BDF986105C77DF9010B5`.
- MAP01_03 reader C# snapshot remained `7|CD2F0EAAA2D86E36C592F600192F67544979344EF7A255DC8B1A08B60C89FAE0`.
- MAP01_04 validator C# snapshot remained `6|B83133C6F02AC2CCB3E5A7AF53CE863F7D5616665F42CB1092F117892D704E9C`.
- Seven allowlisted existing tests remained `7|07AB544C66A02C1DB5CD154158EB7DE0FDC8614063E095EC4188E344806B8194`.
- Authoring CSV snapshot remained `50|164FE578E28BB37FC125989FF6A9B8EE39CD286449DFD77089A46229370D69A4`; BOM missing: `0`.
- Authoring CSV meta snapshot remained `50|6B015D690451A263D15D2A47E9DFB5CFC1A93647C2FBF0F81702A854208DBC3E`.
- Four allowlisted asmdefs remained `4|CD1009CC962C620BFFBC3156D2F05EE54E0B73426DDA006E33FAA7F0B4E3BC2F`.
- Runtime `UnityEditor`, typed-parser, FK, Registry, and content-hash markers in the six new Runtime files: `0`.
- Task changes to existing C#, tests, importer, asmdef/asmref, CSV, non-C# assets, Scenes, Prefabs, Packages, and ProjectSettings: `0`.

## OUT_OF_SCOPE_FINDINGS

- The preexisting large dirty worktree and earlier Legacy asset reorganization were preserved and not treated as this Task's changes.
- Unity Test Runner emitted package-owned prebuild/cleanup messages and its results-save notification during test runs; they were cleared before final compile verification.
- MCP-for-Unity emitted its package-owned inactive-WebSocket warning after final compile; HTTP MCP remained healthy and the warning was cleared, leaving the final Console empty.
- No visual verification was required because this Task adds pure Runtime CSV primary-key models/logic and EditMode tests without Scene, Prefab, UI, or rendered behavior.

## DONE CONDITIONS

- [x] Current Task was confirmed as MAP01_05.
- [x] Master count `205` and MAP01_04 COMPLETE/PASS were confirmed.
- [x] File-scoped single/composite PKs are collected in `PrimaryKeyOrder`.
- [x] `EffectiveValue` is used without trimming, normalization, or typed parsing.
- [x] Key equality and hashing are exact ordinal per component.
- [x] Composite key identity does not depend on delimiter concatenation.
- [x] Both source locations of a two-row duplicate are reported.
- [x] The first and every later occurrence of a three-or-more-row duplicate are reported in one group.
- [x] Every duplicate group is returned in deterministic order.
- [x] Any duplicate prevents usable or partial index publication.
- [x] Index, key, occurrence, duplicate, and result models expose immutable/read-only collections.
- [x] Invalid validation/schema input is rejected without partial publication.
- [x] Exactly six Runtime C# files, one test C# file, and their seven metas were created.
- [x] All seven new meta GUIDs are valid and project GUID duplicates are zero.
- [x] Existing reader, schema, validator, importer, and tests were not modified.
- [x] Existing Authoring CSV `50` and metas `50` were not modified.
- [x] Typed parsers, FK processing, Registry, and MAP01_06-or-later behavior were not implemented.
- [x] asmdef, non-C# asset, Scene, Prefab, Package, and ProjectSettings changes are zero.
- [x] Unity refresh passed with compile errors and relevant warnings `0/0`.
- [x] Targeted EditMode passed `134/134`, above the required minimum of `126`.
- [x] PlayMode was neither run nor created.
- [x] This Result contains the actual inventory and every required section.
- [x] MAP01_06 was not started.

## NEXT

- Finalize MAP01_05 status only: `CURRENT -> COMPLETE`, then set Current Task to `NONE`.
- Do not start MAP01_06 automatically.

## Recommended Commit

`feat(map): build immutable CSV primary key indexes`
