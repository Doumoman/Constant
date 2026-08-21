# MAP01_04 Implement Header and Field Validation Result

## TASK

`MAP01_04_IMPLEMENT_HEADER_AND_FIELD_VALIDATION`

## STATUS

STATUS: PASS

## SUMMARY

- Implemented ordinal, case-sensitive CSV header validation for missing, unexpected, duplicate, and order-mismatch cases with exact source positions.
- Implemented data-record field-count validation and raw-string required/default precedence without trimming, normalization, or typed parsing.
- Added immutable error, validated-field, validated-record, and validation-result models; any error publishes zero validated records.
- Added `29` focused Runtime EditMode cases and passed the full `102/102` targeted regression.
- Did not implement primary-key indexing, scalar/list parsing, foreign-key resolution, domain definitions, Registry publication, or MAP01_05-or-later behavior.

## READ

- Read the twelve Mandatory Read Order documents in their specified order, including the MAP01_03 PASS Result.
- Read only the eight allowlisted MAP01_02 Runtime schema files, seven MAP01_03 Runtime reader files, three allowlisted schema/reader/importer fixtures, three architecture fixtures, four allowlisted asmdefs, and this Task's write-allowlisted files.
- Used only the permitted direct filenames, hashes, BOM bytes, meta GUIDs, changed paths, Unity state, and Console projections for limited inspection.
- Did not read Authoring CSV domain contents, non-allowlisted C# bodies, Scene/Prefab YAML, Legacy bodies, or later Task bodies.

## MASTER BACKLOG CHECK

- Master backlog count: `205` task IDs.
- `MAP00_01` through `MAP00_10` and `MAP01_01` through `MAP01_03`: `COMPLETE`.
- `MAP01_04_IMPLEMENT_HEADER_AND_FIELD_VALIDATION`: sole `CURRENT` task during Task execution.
- `MAP01_05_IMPLEMENT_PRIMARY_KEY_INDEX` and every later task remained `LOCKED / NOT STARTED`.

## MAP01_03 GATE CHECK

- MAP01_03 Result matched `MAP01_03_IMPLEMENT_RFC4180_READER` and contained exact `STATUS: PASS`.
- Recorded baseline: reader `31/31`, schema `23/23`, importer `9/9`, architecture `10/10`, targeted `73/73`, compile errors `0`, relevant warnings `0`.
- Revalidated before patch execution with targeted EditMode job `3833a64c346143ce9316fbb783c7d3f1`: `73 passed, 0 failed, 0 skipped`.
- Authoring CSV/meta baseline revalidated as `50/50`, with UTF-8 BOM missing `0`.

## CREATED

- `Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvHeaderFieldErrorCode.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvHeaderFieldError.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvValidatedField.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvValidatedRecord.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvHeaderFieldValidationResult.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvHeaderAndFieldValidator.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/CsvHeaderAndFieldValidatorTests.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvHeaderFieldErrorCode.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvHeaderFieldError.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvValidatedField.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvValidatedRecord.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvHeaderFieldValidationResult.cs.meta`
- `Assets/_Game/Map/Runtime/WorldGeneration/Data/CsvHeaderAndFieldValidator.cs.meta`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/CsvHeaderAndFieldValidatorTests.cs.meta`
- `MapDesign/MCP/REPORTS/MAP01_04_IMPLEMENT_HEADER_AND_FIELD_VALIDATION_RESULT.md`

## PREEXISTING_IDENTICAL

NONE

## HEADER CONTRACTS IMPLEMENTED

- Expected headers come from `CsvFileSchema.Columns` in column-order sequence and are compared with exact ordinal, case-sensitive semantics.
- An empty reader record set reports every expected column as `MissingHeader` at file start.
- Unexpected headers and every duplicate after the first occurrence report the actual field start; missing headers report the header record end-exclusive position.
- When duplicate, missing, and unexpected inventories are empty, every mismatching actual position is reported as `HeaderOrderMismatch`.
- Header errors are emitted deterministically in actual-position order, then missing expected-column order, with fixed enum ordinal order at the same position.
- Any header error stops field-to-schema mapping, skips data validation, and publishes zero validated records.

## FIELD CONTRACTS IMPLEMENTED

- Data records require exactly the schema column count; the first extra field start or short record end-exclusive position is reported as `FieldCountMismatch`.
- A count-mismatched record skips required/default processing while later records continue validation.
- Non-empty raw values win unchanged; empty raw values use a non-empty raw schema default; otherwise the effective value remains empty.
- Whitespace is not trimmed or treated as empty, and defaults are not parsed or normalized.
- Required empty effective values report `RequiredFieldEmpty` at the source field start.
- Every error preserves immutable source name, schema file, code, message, record, field, physical line, physical column, UTF-16 char offset, expected value, and actual value.
- Successful records and fields preserve source model references and expose copied read-only collections; any accumulated error discards every candidate validated record.
- An unsuccessful reader result maps its first exact error source/location to one `SyntaxReadFailed` error.

## TEST

- `CsvHeaderAndFieldValidatorTests`: `29/29 PASS`; isolated job `e24c7337b5864a7fba0b7e7efa5c76fe`.
- `Rfc4180CsvReaderTests`: `31/31 PASS`.
- `CsvSchemaCatalogTests`: `23/23 PASS`.
- `CsvSchemaDictionaryImporterTests`: `9/9 PASS`.
- Architecture fixtures: `3 + 3 + 4 = 10/10 PASS`.
- Combined targeted EditMode job `96a898c0230c44aa962ed3441191c208`: `102 passed, 0 failed, 0 skipped` (`Passed`, duration `0.1977623s`).
- PlayMode: `NOT RUN`.

## UNITY

- Unity version: `6000.3.8f1`.
- Active instance: `Constant@ced6e0dfc4a31d45`.
- Asset Refresh: `PASS`.
- Final Editor state after forced refresh/compile: idle, not compiling, no pending domain reload, no asset update, ready for tools.
- Compile errors: `0`; relevant new warnings: `0`; final error/warning Console entries: `0`.
- Targeted EditMode: `102/102 PASS`.
- PlayMode: `NOT RUN`.
- Scene/Prefab changes: `NONE`.

## ASSET META VALIDATION

- New `.cs.meta`: `7/7` present; missing: `0`.
- New GUID format: `7/7` valid lowercase 32-hex values.
- New GUID uniqueness: `7/7`; duplicate new GUIDs: `0`.
- Project meta/GUID count: `2846/2846`; project duplicate GUID groups: `0`.
- Existing `.meta` files were not modified by this Task.

## CHANGE SCOPE

- Exact new Asset paths: expected `14`, actual `14`, missing `0`, unexpected `0`.
- New Runtime production C#: `6`; new Runtime EditMode test C#: `1`; modified existing C#: `0`.
- Active `_Game` C# inventory changed from `35` to `42` only by the seven allowlisted files.
- Non-Task active C# snapshot remained `35|FB732D8F4D84F325114768125C2A1BA13B5FF70946F332E07902FD1C7F93D45C`.
- MAP01_02 schema C# snapshot remained `8|886BA154335A61EE850F0EFF2B4D1CCA3650C5D28927BDF986105C77DF9010B5`.
- MAP01_03 reader C# snapshot remained `7|CD2F0EAAA2D86E36C592F600192F67544979344EF7A255DC8B1A08B60C89FAE0`.
- Authoring CSV snapshot remained `50|164FE578E28BB37FC125989FF6A9B8EE39CD286449DFD77089A46229370D69A4`; BOM missing: `0`.
- Authoring CSV meta snapshot remained `50|6B015D690451A263D15D2A47E9DFB5CFC1A93647C2FBF0F81702A854208DBC3E`.
- Four allowlisted asmdefs remained `4|CD1009CC962C620BFFBC3156D2F05EE54E0B73426DDA006E33FAA7F0B4E3BC2F`.
- Runtime namespace violations, `UnityEditor` references, typed-parser markers, and later-feature markers in the six new Runtime files: `0`.
- Task changes to existing tests, importer, asmdef/asmref, non-C# assets, Scenes, Prefabs, Packages, and ProjectSettings: `0`.

## OUT_OF_SCOPE_FINDINGS

- The preexisting large dirty worktree and earlier Legacy asset reorganization were preserved and not treated as this Task's changes.
- Unity Test Runner emitted package-owned prebuild/cleanup messages and its results-save notification during test runs; they were cleared before the final compile verification.
- MCP-for-Unity intermittently logged its package-owned inactive-WebSocket warning; it did not affect HTTP MCP execution, compilation, or tests and was cleared from the final Console.
- No visual verification was required because this Task adds pure Runtime CSV validation models/logic and EditMode tests without Scene, Prefab, UI, or rendered behavior.

## DONE CONDITIONS

- [x] Current Task was confirmed as MAP01_04.
- [x] Master count `205` and MAP01_03 COMPLETE/PASS were confirmed.
- [x] Header missing, unexpected, duplicate, and order mismatch use ordinal validation.
- [x] Field-count mismatches report exact first-extra or record-end positions.
- [x] Required/default precedence follows the raw-string contract.
- [x] Whitespace is not treated as empty.
- [x] Every error carries complete source, schema, record, field, line, column, and offset context.
- [x] Header errors prevent data mapping.
- [x] Errors accumulate where safe and return in deterministic order.
- [x] Any error publishes zero validated records.
- [x] Successful validated models and collections are immutable/read-only.
- [x] Exactly six Runtime C# files, one test C# file, and their seven metas were created.
- [x] All seven new meta GUIDs are valid and project GUID duplicates are zero.
- [x] Existing reader, schema, importer, and tests were not modified.
- [x] Existing Authoring CSV `50` and metas `50` were not modified.
- [x] Typed parsers, PK/FK processing, Registry, and MAP01_05-or-later behavior were not implemented.
- [x] asmdef, asset, Scene, Prefab, Package, and ProjectSettings changes are zero.
- [x] Unity refresh passed with compile errors and relevant warnings `0/0`.
- [x] Targeted EditMode passed `102/102`, above the required minimum of `97`.
- [x] PlayMode was neither run nor created.
- [x] This Result contains the actual inventory and every required section.
- [x] MAP01_05 was not started.

## NEXT

- Finalize MAP01_04 status only: `CURRENT -> COMPLETE`, then set Current Task to `NONE`.
- Do not start MAP01_05 automatically.

## Recommended Commit

`feat(map): validate CSV headers and fields`
