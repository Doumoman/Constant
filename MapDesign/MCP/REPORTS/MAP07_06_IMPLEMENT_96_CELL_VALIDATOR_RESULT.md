TASK: MAP07_06_IMPLEMENT_96_CELL_VALIDATOR
STATUS: PASS
MAP07_06: COMPLETE ELIGIBLE
MAP07_07_IMPLEMENT_MICROCHUNK_REACHABILITY_PROBE: LOCKED / DO NOT START

## Patch and input integrity

- Applied patch: `MAP07_06_IMPLEMENT_96_CELL_VALIDATOR` v1.0
- Applied receipt SHA-256: `192429ba90ec61a0cc351e0bff35f488d186ab59d28744ab17b3c1ee45448ff7`
- Patched Master SHA-256: `8d03a25c944dbfbc917257b373fa7f9e067bb0d3fbf9a01fac9c26ef9f93570a`
- Patched pre-finalization Status SHA-256: `a0f8355d9ae291cdca2f9b05ae7ba1eed3775b255c2ec1d4b79ab1e3dc8ccd01`
- MAP07_05 Result SHA-256: `4d805c6ff1702e4e8ecea3be7a337584e4e2856b7d5106d51d1e42c31954029c`
- MAP07_05 Task SHA-256: `141ba64ee4fadee918c69daa94693a89aac21efb10d14f65576c04c4e66515fc`
- MAP07_06 Task SHA-256: `38a601ca63dff23622564cf36b3c02aa2f55849808c69b3a58bf60d2a8d7c6fa`
- 96-cell validator model/API digest: `54a09f7327c37405a826e4fbc3bea9443e1472ec3f92f15b9495edfba422710c`
- Preserved object-slot validator digest: `9a3b86991302add138c36acccd18789ad79195cd27cab7fb5fe2c5bb8a520e6a`
- Preserved socket-edge validator digest: `fdfbcb7bf651eb963d899f7e9800e0a8f23826d43ec4ffed25e3c32ee7a0c048`
- Preserved transform model/API digest: `7031695ec2c4bb333be69ba490c03aef003124d50953d577b4943c634336b031`
- Preserved tile-layer rules digest: `ace56d38399aa6ccea6e0e0e7361f3802b7eb7418b1ef1d081f6a40b04e08160`
- Preserved MAP07_01 model/API digest: `5ed21ca7b86cfebf0095eba6d14bf4bb27be75ce39ffaf46f3c32a516d77613b`
- Preserved Authoring manifest SHA-256: `4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3`

The patch was the only unapplied MCP inbox item. Its predecessor state, Current Task transition, 205-task Master count, prior hashes, Unity version, Assets meta count, Authoring counts, and tracked Assets preconditions matched the manifest. The three payload destinations matched their source SHA-256 values, the `.APPLIED` receipt was written, and no unapplied patch remains.

The new validator digest uses the established deterministic manifest method: sort the five repository-relative runtime C# paths ordinally, append `path=lowercase-file-sha256\n`, and SHA-256 the UTF-8 manifest. Preserved MAP07 model sources and all Authoring CSV/meta inputs have zero tracked changes.

## Implemented runtime model and validation

- `Microchunk96CellRecord.cs`
  - Adds an immutable record carrying microchunk ID, non-negative source ordinal, raw local X/Y, and optional normalized `MicrochunkTileCell` data.
  - Retains raw integer coordinates so invalid rows can be diagnosed without constructing an out-of-range `MicrochunkLocalCoord`.
- `Microchunk96CellValidationPolicy.cs`
  - Adds immutable complete and partial/draft policies.
  - Uses complete coverage by default and selects complete/partial behavior from `MicrochunkDefinition.TileDataComplete` for definition projections.
- `Microchunk96CellValidationViolation.cs`
  - Records immutable microchunk ID, optional source ordinal, optional raw coordinate, optional normalized coordinate, and exact stable reason.
  - Exposes `MISSING_CELL_RECORD`, `DUPLICATE_CELL_COORDINATE`, and `CELL_COORDINATE_OUT_OF_RANGE` reason constants.
- `Microchunk96CellValidationResult.cs`
  - Freezes and canonically orders violations by microchunk ID, reason category, coordinate, and source ordinal.
  - Exposes evaluated microchunk/record counts, expected count and row-count mismatch summary, in-range unique count, missing/duplicate/out-of-range counts, issue count, and success.
- `Microchunk96CellValidator.cs`
  - Validates one or multiple in-memory microchunk record groups without mutating source collections.
  - Requires every one of the exact 12x8 coordinates once under complete policy; explicit all-`NONE` rows remain valid records.
  - Reports every missing coordinate, each extra in-range duplicate row, and every out-of-range row deterministically.
  - Keeps missing summary counts non-blocking in partial/draft mode while duplicates and out-of-range rows remain failures.
  - Projects existing definitions without mutation and preserves full 96-cell coverage across all four approved transforms.

The implementation deliberately does not perform tile-code foreign-key checks, layer compatibility, socket/object-slot validation, reachability, CSV import/export, editor UI, preview generation, or sector assembly.

## EditMode coverage and boundary advance

`Microchunk96CellValidatorTests.cs` executes 406 deterministic cases. Coverage includes:

- exact 12x8 constants and all 96 row-major coordinates;
- valid complete and all-`NONE` record sets;
- every one of the 96 single-coordinate omissions;
- every one of the 96 legal coordinates duplicated once;
- every one of the 96 coordinates as a sparse draft row;
- multiple missing coordinates, duplicate/missing interaction, four out-of-range boundary directions, and out-of-range/missing interaction;
- complete versus partial policy semantics and row-count mismatch summaries;
- immutable record, policy, violation, result, record collection, normalized cell, and definition snapshots;
- definition projection, deliberate layer-rule separation, stable multi-microchunk ordering, and all four approved transforms.

Five allowlisted existing boundary tests were advanced by replacing only the newly implemented `Microchunk96CellValidator` future-boundary symbol with a still-forbidden later symbol. Each existing-file diff is exactly one addition and one deletion; no fixture, assertion, case, skip, or ignore was removed or weakened.

## Unity verification

Unity version: `6000.3.8f1`

```text
Microchunk96CellValidatorTests:           406 / 406 PASS
MicrochunkObjectSlotValidatorTests:       483 / 483 PASS
MicrochunkSocketEdgeValidatorTests:       332 / 332 PASS
MicrochunkTransformerTests:               483 / 483 PASS
MicrochunkTileLayerRulesTests:            150 / 150 PASS
MicrochunkDefinitionTests:                146 / 146 PASS
MAP06 category union:                    2552 / 2552 PASS
OptionalRegionModelsTests:                194 / 194 PASS
MAP06 required total:                    2746 / 2746 PASS
MAP05 category union:                    1832 / 1832 PASS
MandatoryRouteMaskLookupBuilderTests:     127 / 127 PASS
MAP05 required total:                    1959 / 1959 PASS
Actually executed total:                 6705 / 6705 PASS
Failed / skipped:                           0 / 0
Compilation errors:                             0
Final Console errors / warnings:            0 / 0
Relevant warnings:                               0
```

The final MAP07_06 execution is `406/406 PASS`. The first preflight test request overlapped Unity's one-time meta import/domain reload and timed out before executing any test; the post-import retry passed. During the long MAP05 category run the MCP bridge reconnected once; the original job was recovered by ID and completed `1832/1832 PASS` with no failures or skips.

## Static and change-scope verification

```text
Assets meta before / after:             3356 / 3362
Assets GUID rows / duplicate groups:    3362 / 0
New Runtime C# / matching meta:             5 / 5
New test C# / matching meta:                1 / 1
New folder meta:                            0
Existing boundary test C# modified:         5 <= 17
Per existing test diff:                     1 add / 1 delete
Existing matching test meta modified:       0
Authoring CSV / matching meta:              50 / 50
Authoring manifest changes:                  0
Generated CSV files created:                 0
Scene / Prefab tracked changes:             0 / 0
ProjectSettings / Packages changes:         0 / 0
asmdef / asmref changes:                    0 / 0
MAP07_01 production source changes:          0
MAP07_02 production source changes:          0
MAP07_03 production source changes:          0
MAP07_04 production source changes:          0
MAP07_05 production source changes:          0
MAP06 production source changes:             0
Forbidden MAP07_07+ production hits:         0
Assets duplicate GUID groups:                0
Unapplied MCP patches:                       0
```

Unity's generated solution inventory was restored after refresh. No Scene, Prefab, asset body outside the allowlist, ProjectSettings, Packages, assembly definition/reference, Authoring CSV/meta, prior MAP07 production source, MAP06 production source, or later-Task production file remains changed.

## Finalization boundary

MAP07_06 is eligible to transition from `CURRENT` to `COMPLETE`. Finalization may update only the MAP07_06 row/current-task and last-completed/result fields in `06_IMPLEMENTATION_STATUS.md`. `MAP07_07_IMPLEMENT_MICROCHUNK_REACHABILITY_PROBE` remains `LOCKED`; its Task body was not read and its implementation was not started.
