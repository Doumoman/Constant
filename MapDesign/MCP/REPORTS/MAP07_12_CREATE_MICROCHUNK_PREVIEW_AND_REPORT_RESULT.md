# MAP07_12 - Create Microchunk Preview And Report Result

```text
TASK: MAP07_12_CREATE_MICROCHUNK_PREVIEW_AND_REPORT
STATUS: PASS
MAP07_12: COMPLETE ELIGIBLE
MAP07_13_MAP07_STARTER_AND_EXIT_TESTS: LOCKED / DO NOT START
```

## Applied patch and prior gate

```text
MAP07_11 Result SHA-256: 340cbed5424208ebeef144028c1806ea6a9039e8a6c14a5f39a824b042b062c6
MAP07_11 Task SHA-256:   1359b31bd70bd8288f86fb2d994267d480b7130a96a45e25541de1c05ba7e6ca
MAP07_12 Task SHA-256:   73544122f13653fa3762e87fdc75b7a415482b7336e0ac3fe3a75760d51ec9b0
Patch receipt SHA-256:   b20eea84c198e6ffa1e939d0eac548bc7d0cee621f1f089a3d64fa58a5d58c8d
Unapplied MCP patches:   0
```

The MAP07_12 v1.0 inbox manifest, dependency hashes, before/after status counts, payload hashes, destination absence, Unity baseline, Assets meta count, and Authoring CSV baseline were validated before the exact payload was copied. The `.APPLIED` receipt was written only after every post-apply hash and status transition matched the manifest.

## Implementation

Six Editor-only production files implement a detached, explicit, side-effect-free selected-microchunk preview layer:

- `MicrochunkPreviewRequest` freezes one canonical selected ID, the detached editor state, the exact supported transform set (`R0`, `MIRROR_X`, `MIRROR_Y`, `R180`), overlay toggles, validator options, and input diagnostics.
- `MicrochunkPreviewBuilder` projects editor state through the existing `MicrochunkTransformer`; invokes the existing tile-layer, 96-cell, socket-edge, object-slot, and reachability services; catches validator failures as deterministic report issues; and never mutates the detached source.
- `MicrochunkPreviewCellOverlay` emits exactly 96 row-major local-coordinate cells per transform and distinguishes disabled, unreachable, reachable, mandatory-pair path witness, socket entry, socket exit, and blocked/solid states while retaining tile/socket/object-slot detail according to the toggles.
- `MicrochunkPreviewReport` exposes immutable transform projections, coordinate-addressable cells, validator feedback, deterministically sorted issues, and the existing probe's mandatory socket-pair witnesses.
- `MicrochunkPreviewWindow` provides an explicit Generate action, transform and overlay selection, a visible 12x8 grid, issue list, and coordinate detail. Empty/invalid selection and missing editor state are converted to UI errors without Scene or source mutation.

No 90-degree transform, world traversal, sector assembly, starter catalog round-trip, generated writer, or runtime production rule was added.

```text
Preview/report deterministic Editor model/API digest:
4545e7962dc4da03ec04fe57d3b90d28bb60c50474a8c6d93b63eb392168191b

Preserved MAP07_11 CSV exporter digest:
abd090a627f295cc91593e49b78e2c7871ff3210c5ace87af43677027898f976
```

The preview digest uses the established deterministic manifest method: sort the six repository-relative Editor production C# paths ordinally, append `path=lowercase-file-sha256\n`, then SHA-256 the UTF-8 manifest.

## Required test gates

```text
MicrochunkPreviewAndReportTests:          520 / 520 PASS
MicrochunkCsvExporterTests:               460 / 460 PASS
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

Actually executed required total:        9327 / 9327 PASS
Required failed / skipped:                  0 / 0
Unity compile errors:                          0
Final Console errors / warnings:             0 / 0
Relevant warnings:                              0
```

One initial focused-test request timed out during Unity Test Runner initialization before any test began. Test discovery was then confirmed and the exact fixture was run successfully. A later Unity-MCP observer session disconnected while the MAP05 category job continued inside the same Editor; reconnecting to the same instance recovered its final `1832/1832 PASS` result. Neither transport event produced a qualifying test failure or skip.

After the final validator-failure tolerance adjustment, Unity recompiled with zero errors/warnings and the focused fixture was rerun at `520/520 PASS`.

## Static and change-scope gates

```text
Assets meta:                              3400 -> 3407
New Editor production C# / matching meta:    6 / 6
New Editor test C# / matching meta:            1 / 1
New folder meta:                                  0
New Runtime C# / matching meta:               0 / 0
Task-local existing boundary test C# modified:    4 <= 18
Matching existing boundary-test meta modified:    0
Assets duplicate GUID groups:                      0

Authoring CSV / matching meta:                 50 / 50
Preserved Authoring manifest SHA-256:
4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Authoring CSV tracked changes:                       0
Generated CSV files created:                         0

Scene / Prefab tracked changes:                    0 / 0
ProjectSettings / Packages tracked changes:        0 / 0
asmdef / asmref tracked changes:                   0 / 0

MAP07_01~MAP07_11 production source changes:          0
MAP06 production source changes:                      0
Forbidden MAP07_13+ production symbol hits:           0
Unapplied MCP patches:                                0
```

The Authoring source remains at the approved `50 CSV / 50 matching meta` baseline with no tracked Authoring-path change, preserving its approved canonical manifest hash. Authoring CSV, generated CSV, Scene, Prefab, ProjectSettings, Packages, asmdef, asmref, and runtime production code were not changed. MAP07_13+ Task bodies were not read and no MAP07_13+ work was started.

The pre-existing user modification to `Constant.slnx` was preserved and is not a MAP07_12 change.

## Finalization eligibility

All implementation, focused and regression test, compile, Console, GUID, source-preservation, forbidden-symbol, and change-scope gates pass. MAP07_12 alone is eligible to become `COMPLETE`; Current Task may become `NONE`. `MAP07_13_MAP07_STARTER_AND_EXIT_TESTS` remains `LOCKED` and must not start without a separate patch.
