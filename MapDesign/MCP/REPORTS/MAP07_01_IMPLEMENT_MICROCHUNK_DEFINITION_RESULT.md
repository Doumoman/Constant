TASK: MAP07_01_IMPLEMENT_MICROCHUNK_DEFINITION
STATUS: PASS
MAP07_01: COMPLETE ELIGIBLE
MAP07_02_IMPLEMENT_TILE_LAYER_RULES: LOCKED / DO NOT START

## Patch And Preconditions

- Patch: `MAP07_01_IMPLEMENT_MICROCHUNK_DEFINITION / 1.0`
- Patch manifest SHA-256: `eb94d9f1401efa9e0c851d30a3d682051faf189a4577a8336b871da3645b3b92`
- Patch receipt SHA-256: `4a0864fbd9e335bffe47eb029badac098d475a7a39ba5b3d003bbf9ab7b737d5`
- Prior MAP06_10 Result SHA-256: `690a7cef9dbf1d22416e38b3675d76b0ef758062de2425e8e4841381f0d9bdeb`
- Prior MAP06_10 repaired Task SHA-256: `623da5aaf2f8c72dd830fb5f859c4b05a631a93b7f4fa2a3aa67adc823f95cdb`
- Current MAP07_01 Task SHA-256: `912028220492f7e9dff40db93711dd590dcd73531131d133cd0270c4862d368c`
- Prior gate: MAP06_10 `PASS`, `COMPLETE`, `MAP06 PHASE EXIT: APPROVED`.
- Applied status: `78 COMPLETE / 1 CURRENT / 126 LOCKED`; MAP07_01 was the sole CURRENT task.
- Unity/project baseline: `Constant@ced6e0dfc4a31d45`, Unity `6000.3.8f1`, exact project root.
- Starter source facts: catalog/tile/socket/slot rows `14 / 1344 / 25 / 9`; microchunk dimensions `12 x 8 = 96`.

The manifest's three copy operations were byte-identical after application. No Asset, CSV, runtime, editor, test, asmdef, Scene, or Prefab content was modified during Phase A.

## Implementation Summary

MAP07 now has an immutable runtime representation for one authored microchunk without opening any MAP07_02+ behavior. The model separates identity, local coordinates, enum vocabulary, tile-layer records, connection sockets, object slots, and the aggregate definition.

The aggregate snapshots every input collection, canonicalizes deterministic ordering, rejects duplicate tile coordinates/socket IDs/slot IDs, and enforces the 96-cell contract only when `TileDataComplete` is true. Partial authoring data remains legal only when the flag is false.

### Runtime scripts created

1. `MicrochunkConstants.cs`
   - Publishes exact `WidthTiles=12`, `HeightTiles=8`, `CellCount=96`, and `LayerCount=8` constants.
   - Reuses the locked world-generation microchunk dimensions.
2. `MicrochunkId.cs`
   - Immutable ordinal ID value object.
   - Rejects null/empty/whitespace while preserving accepted CSV spelling exactly.
3. `MicrochunkLocalCoord.cs`
   - Immutable local `(x,y)` value type with strict `0..11 / 0..7` bounds.
   - Provides `TryCreate`, equality/hash/string, comparison, and stable `y*12+x` row-major index.
4. `MicrochunkEnums.cs`
   - Defines usage class, transform, side, traversal kind, route layer, slot category, tool requirement, object orientation, and eight tile-layer enums.
   - Includes only the Map Package vocabulary and the represented safe `None` orientation.
5. `MicrochunkTileCell.cs`
   - Stores one local coordinate and exactly eight non-empty tile-code IDs.
   - Preserves `NONE` as explicit data.
6. `MicrochunkSocketDefinition.cs`
   - Stores typed side/traversal/tool/route layer plus socket ID, band, direction, mandatory flag, edge signature, minimum safe tiles, and notes.
   - Rejects undefined enums and negative minimum-safe values.
7. `MicrochunkObjectSlotDefinition.cs`
   - Stores typed anchor/category/orientation plus pool, required/visibility flags, forbidden radius, marker, and notes.
   - Rejects undefined enums and negative radius.
8. `MicrochunkDefinition.cs`
   - Immutable definition aggregate with read-only metadata, cells, sockets, and object slots.
   - Requires exact 12x8 dimensions, non-negative selection/threat/cognitive/chain values, stable metadata IDs, and at least one allowed transform.
   - Sorts tile cells by row-major coordinate and secondary collections by ordinal stable IDs.

Model inventory digest: `673f8a5057a28e6b2dbceac1a43f4eee4b30f0ec2a3738939107759e229cb7d5`.

Digest method: sort the eight Runtime C# paths by file name, create UTF-8 LF lines `<project-relative-path>=<lowercase-file-sha256>`, then SHA-256 the complete inventory text.

## Tests And Phase-Boundary Advance

Created:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkDefinitionTests.cs
```

The focused fixture covers all 96 valid coordinates, invalid bounds, row-major ordering, ID semantics, complete/partial definitions, duplicate/missing cells, eight tile-code layers, sockets, slots, negative numeric fields, transform set semantics, immutable collection snapshots, and explicit MAP07_02+ forbidden production symbols.

Existing boundary test C# modified (`4 <= 15`):

```text
MandatoryRouteMaskLookupBuilderTests.cs
Map06ExitTests.cs
OptionalAttachmentEnumeratorTests.cs
OptionalRegionGrowerTests.cs
```

The old MAP07_01 placeholder prohibition was replaced with the actual MAP07_02+ symbols. Existing test cases were not deleted, skipped, or weakened. Matching existing `.cs.meta` files were not modified.

## Unity EditMode Gates

```text
197ed6ff9e0b47c99516e291c08a891f  MicrochunkDefinitionTests                      146/146 PASS
62333c5d2a6d46379850e21ec02d273c  MAP06_01..MAP06_10 category union             2552/2552 PASS
dcad1333dae04da4980cd082b38fed15  OptionalRegionModelsTests                     194/194 PASS
d815b851f7d4465fb5b815c93f259def  MAP05_01..MAP05_11 category union             1832/1832 PASS
44ae4f6a64c54b46b6e3e24eec3165bc  MandatoryRouteMaskLookupBuilderTests          127/127 PASS
```

Required MAP06 composition remained exact:

```text
Map06ExitTests                              180/180 PASS
OptionalRegionOverlayTests                 180/180 PASS
OptionalRegionOverlaySceneDrawerTests       40/40 PASS
OptionalRegionValidatorTests               321/321 PASS
InactiveBufferAssignerTests                281/281 PASS
OptionalReturnPolicyResolverTests          289/289 PASS
OptionalRewardTierCalculatorTests          279/279 PASS
OptionalAccessRuleAssignerTests            289/289 PASS
Type0RouteMaskAssignerTests                257/257 PASS
OptionalRegionGrowerTests                  234/234 PASS
OptionalAttachmentEnumeratorTests          202/202 PASS
OptionalRegionModelsTests                  194/194 PASS
MAP06 total                               2746/2746 PASS
```

MAP05 aggregate remained `1832 + 127 = 1959/1959 PASS`.

Actually executed acceptance total: `146 + 2746 + 1959 = 4851/4851 PASS`; failed/skipped `0/0`.

Two preliminary zero-test jobs were not counted as acceptance evidence:

- `31f912bbb674492083e294a3f81ab752`: requested before the external scripts had been imported; Unity returned `0 tests`.
- `7b33d30d88bc4f5eaf0ba2f52e7fde6f`: short class-name filters were not accepted as exact Unity test names; Unity returned `0 tests`.

Both were replaced by valid imported/full-name/category runs above. No failed or skipped test was hidden.

## Compile, Static, Meta, CSV, And Change-Scope Gates

```text
Unity version:                         6000.3.8f1
Final compile errors:                  0
Final Console errors:                  0
Final relevant warnings:               0
PlayMode tests:                        NOT REQUIRED
Scene / Prefab changes:                0 / 0
Assets meta before / after:            3323 / 3334
New Runtime C# / matching meta:        8 / 8
New test C# / matching meta:           1 / 1
New Microchunks folder meta:           2
Duplicate Assets GUID groups:          0
Existing boundary test C# modified:    4 <= 15
Existing matching test meta modified:  0
Authoring CSV / matching meta:          50 / 50
Authoring body/meta changes:            0
Generated CSV files created:           0
ProjectSettings / Packages changes:    0 / 0
asmdef / asmref changes:               0 / 0
MAP06 production source changes:       0
Forbidden MAP07_02+ production hits:   0
```

Authoring manifest SHA-256 remains `4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3`. The approved value is preserved by exact `50/50` CSV/meta inventory and zero Authoring path changes from the committed baseline.

No tile-layer rule matrix, transform application, socket-edge validator, object-slot semantic validator, standalone 96-cell validator, reachability probe, CSV importer/exporter, editor UI, boundary resolver, sector recipe resolver, sector assembly, generated-sector writer, Scene, Prefab, ProjectSettings, package, asmdef, or asmref work was introduced.

## NEXT

- Finalize only `MAP07_01_IMPLEMENT_MICROCHUNK_DEFINITION` as COMPLETE.
- Set Current Task to `NONE`.
- Keep `MAP07_02_IMPLEMENT_TILE_LAYER_RULES` LOCKED.
- Do not read or start MAP07_02 until a separate patch opens it.
