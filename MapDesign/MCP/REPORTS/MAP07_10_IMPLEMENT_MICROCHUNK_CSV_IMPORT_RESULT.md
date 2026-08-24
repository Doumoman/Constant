# MAP07_10 - Implement Microchunk CSV Import Result

```text
TASK: MAP07_10_IMPLEMENT_MICROCHUNK_CSV_IMPORT
STATUS: PASS
MAP07_10: COMPLETE ELIGIBLE
MAP07_11_IMPLEMENT_MICROCHUNK_CSV_EXPORT: LOCKED / DO NOT START
```

## Patch and predecessor gates

- Applied patch receipt SHA-256: `a3d07e1fe10aeeff7059ee2dbf7d6461dc5ec3869cb8c51b7cf4173204d70aa8`
- MAP07_09 Result SHA-256: `7bc550e92359f4f24c642b24000be1e1a8198fdeb014ce1685555bf5f83a0340`
- MAP07_09 Task SHA-256: `5e870b792acdaff3ffb12058919f8973cd0fa50dcfd505b662c323f47a6f1a87`
- MAP07_10 Task SHA-256: `a21f95a87c1f962fed4672376d55eb740af6fa5d8b0aa8ec286ba782b2f54735`
- Unapplied MCP patches after application: `0`

The single inbox patch satisfied its manifest preconditions and was copied verbatim before implementation. The payload destination hashes matched the manifest and `.APPLIED` was written before the Current Task began.

## Implementation

Six new `MapAuthoring.Editor` production files implement a read-only selected-ID importer. `MicrochunkCsvImportSource` owns detached byte snapshots for catalog, tile, socket, socket-band, object-slot, variant, and reference tables. `MicrochunkCsvImporter` reuses `Rfc4180CsvReader`, requires one canonical selected ID, rejects missing or duplicate catalog rows, and orders diagnostics by file, selected ID, source row, column, and code.

Complete tile sources require exactly 96 unique in-bounds cells. Non-complete sources still hydrate the fixed row-major 12x8 grid, leave absent layer values as exact `NONE`, and publish deterministic warnings. Socket, band, and object-slot rows hydrate the existing MAP07_09 authoring collections in canonical ID order. Variant and dictionary rows remain detached metadata only. Existing tile-layer, 96-cell, socket-edge, and object-slot validators publish in-memory feedback without mutating imported state.

`MicrochunkCsvImportWindow` only selects one microchunk and imports Authoring CSV into the detached grid/socket/slot state. It contains no export, row replacement, asset creation, generated CSV, Scene/Prefab, or settings mutation command.

- CSV importer deterministic Editor model/API digest: `14bf29aa6edab12ed11caffbd38770690a16ac0a13c82e1ec3fc2c25739b26c6`
- Preserved socket/slot editor model/API digest: `fee0842a7066866ec9f665fbd924c2fead24300c190d9be8e6e42ff0b435dffa`
- Preserved authoring-grid model/API digest: `fe55586945da9aaa3b4bcebb3dd38ac82d2f5287e9f99bc31dc50fd30163abe9`
- Preserved reachability model/API digest: `f488c8a65dacb8f7bdd2c107478074c131e3011110058375c06e165bfb1ddaf3`
- Preserved 96-cell validator model/API digest: `54a09f7327c37405a826e4fbc3bea9443e1472ec3f92f15b9495edfba422710c`
- Preserved object-slot validator model/API digest: `9a3b86991302add138c36acccd18789ad79195cd27cab7fb5fe2c5bb8a520e6a`
- Preserved socket-edge validator model/API digest: `fdfbcb7bf651eb963d899f7e9800e0a8f23826d43ec4ffed25e3c32ee7a0c048`
- Preserved transform model/API digest: `7031695ec2c4bb333be69ba490c03aef003124d50953d577b4943c634336b031`
- Preserved tile-layer rules model/API digest: `ace56d38399aa6ccea6e0e0e7361f3802b7eb7418b1ef1d081f6a40b04e08160`
- Preserved MAP07_01 model/API digest: `5ed21ca7b86cfebf0095eba6d14bf4bb27be75ce39ffaf46f3c32a516d77613b`

The new importer digest uses the established deterministic manifest method: sort the six repository-relative Editor production C# paths ordinally, append `path=lowercase-file-sha256\n`, then SHA-256 the UTF-8 manifest.

## Required test gates

```text
MicrochunkCsvImporterTests:               420 / 420 PASS
MicrochunkSocketAndSlotEditorTests:       380 / 380 PASS
MicrochunkAuthoringGridTests:             320 / 320 PASS
MicrochunkReachabilityProbeTests:         522 / 522 PASS

Microchunk96CellValidatorTests:           406 / 406 PASS
MicrochunkObjectSlotValidatorTests:       483 / 483 PASS
MicrochunkSocketEdgeValidatorTests:       332 / 332 PASS
MicrochunkTransformerTests:               483 / 483 PASS
MicrochunkTileLayerRulesTests:            150 / 150 PASS
MicrochunkDefinitionTests:                146 / 146 PASS
Existing MAP07 regression union:         2000 / 2000 PASS

MAP06 category union:                    2552 / 2552 PASS
OptionalRegionModelsTests:                194 / 194 PASS
MAP06 required total:                    2746 / 2746 PASS

MAP05 category union:                    1832 / 1832 PASS
MandatoryRouteMaskLookupBuilderTests:     127 / 127 PASS
MAP05 required total:                    1959 / 1959 PASS

Actually executed required total:        8347 / 8347 PASS
Required failed / skipped:                  0 / 0
Unity compile errors:                          0
Final Console errors / warnings:             0 / 0
Relevant warnings:                            0
```

One development run exposed duplicate insertion when multiple sockets shared one edge-signature ID; the importer was corrected to hydrate each signature once and the final `420/420` importer gate passed. One subsequent Test Runner job did not initialize and executed zero tests; it is excluded from the qualifying gate. Every required final fixture above executed with zero failures and zero skips.

## Static and change-scope gates

```text
Assets meta:                              3386 -> 3393
New Editor production C# / matching meta:    6 / 6
New Editor test C# / matching meta:           1 / 1
New folder meta:                                0
New Runtime C# / matching meta:               0 / 0
Task-local existing boundary test C# modified:    2 <= 17
Matching existing boundary-test meta modified:    0
Assets duplicate GUID groups:                     0

Authoring CSV / matching meta:                 50 / 50
Preserved Authoring manifest SHA-256:
4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Authoring CSV tracked changes:                     0
Generated CSV files created:                       0

Scene / Prefab tracked changes:                  0 / 0
ProjectSettings / Packages tracked changes:      0 / 0
asmdef / asmref tracked changes:                 0 / 0

MAP07_01 production source changes:                  0
MAP07_02 production source changes:                  0
MAP07_03 production source changes:                  0
MAP07_04 production source changes:                  0
MAP07_05 production source changes:                  0
MAP07_06 production source changes:                  0
MAP07_07 production source changes:                  0
MAP07_08 production source changes:                  0
MAP07_09 production source changes:                  0
MAP06 production source changes:                     0
Forbidden MAP07_11+ production symbol hits:          0
Unapplied MCP patches:                               0
```

The Authoring source remained at the approved `50 CSV / 50 matching meta` baseline with no tracked Authoring-path change, preserving its approved canonical manifest hash. The pre-existing user modification to `Constant.slnx`, present before Phase A, was preserved and not treated as a Task-local change. No Task-local write remains outside the exact Task allowlist, Unity-generated matching metas, the Phase A patch payload, this Result, and the Phase C status finalization target.

## Finalization eligibility

All implementation, test, compile, Console, GUID, source-preservation, forbidden-symbol, and change-scope gates pass. MAP07_10 alone is eligible to become `COMPLETE`; Current Task may become `NONE`. `MAP07_11_IMPLEMENT_MICROCHUNK_CSV_EXPORT` remains `LOCKED` and was not read or started.
