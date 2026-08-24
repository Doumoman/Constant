# MAP08_01 - Define Moonpalace Biome Pairs Result

```text
TASK: MAP08_01_DEFINE_MOONPALACE_BIOME_PAIRS
STATUS: PASS
MAP08_01: COMPLETE ELIGIBLE
MAP08_02_IMPLEMENT_BOUNDARY_CANDIDATE_INDEX: LOCKED / DO NOT START
```

## Applied patch and prior gate

```text
MAP07_13 Result SHA-256: 263a2bbf291e4df25dbe6bc101986e11ebf39bc0fc3d0074759fb7450b6df77e
MAP07_13 Task SHA-256:   698a330dcd7a8ba14ec33cec51b68ea56be9382abd0eefde96eb2a516c81effb
MAP08_01 patch receipt SHA-256:
b82282016e5d352cb6adbd0605ed474698bf4c569ce32d40473984c1ead56858
Unapplied MCP patches:   0
```

The MAP08_01 v1.0 manifest preconditions, prior Task/Result/receipt hashes,
payload hashes, `NONE -> MAP08_01 CURRENT` transition, Unity baseline, Assets
meta count, and Authoring inventory were verified before the exact payload was
applied. The `.APPLIED` receipt was created only after all destination hashes
matched the manifest.

## Immutable Moonpalace biome contract

The Runtime-only `StarNight.Map.WorldGeneration.Boundaries` contract defines the
four canonical biome IDs, display names, and stable orders below. Exact ordinal
parse/format rejects null, empty, case variants, whitespace variants, and unknown
IDs without fallback.

| Order | Canonical ID | Display name |
|---:|---|---|
| 0 | `MoonCrater` | `Moon Crater` |
| 1 | `CassiaRoot` | `Cassia Root` |
| 2 | `AbandonedMill` | `Abandoned Mill` |
| 3 | `MoonDough` | `Moon Dough` |

The exact unordered pair enumeration is canonicalized by biome order and remains
stable independently of input direction or dictionary enumeration:

```text
MoonCrater<->CassiaRoot
MoonCrater<->AbandonedMill
MoonCrater<->MoonDough
CassiaRoot<->AbandonedMill
CassiaRoot<->MoonDough
AbandonedMill<->MoonDough
```

Self-pairs fail at pair construction. Missing and duplicate definitions fail
catalog construction. Public biome, pair, definition, and orientation collections
are read-only copies. Pair equality/hash/string and catalog signatures are stable
under reversed pair input and non-default process cultures.

## Orientation, mandatory route, and warning preconditions

| Pair | Horizontal | Vertical | Mandatory tool | Mandatory route | Minimum distinct warning markers |
|---|---:|---:|---|---:|---:|
| `MoonCrater<->CassiaRoot` | YES | YES | `NONE` | YES | 2 |
| `MoonCrater<->AbandonedMill` | YES | YES | `NONE` | YES | 2 |
| `MoonCrater<->MoonDough` | YES | YES | `NONE` | YES | 2 |
| `CassiaRoot<->AbandonedMill` | YES | YES | `NONE` | YES | 2 |
| `CassiaRoot<->MoonDough` | YES | YES | `NONE` | YES | 2 |
| `AbandonedMill<->MoonDough` | YES | YES | `NONE` | YES | 2 |

All 12 pair/orientation combinations explicitly support `Horizontal` and
`Vertical`, require `tool_requirement=NONE`, set
`mandatory_route_allowed=true`, and require at least two distinct categories
from the exact warning marker set `Tile / Background / Resource / Audio`.
These are immutable authoring preconditions only; no candidate index, filter,
resolver, boundary content row, or warning renderer was added.

## Production scripts

```text
MoonpalaceBiomeId.cs
MoonpalaceBiomePair.cs
MoonpalaceBoundaryOrientation.cs
MoonpalaceBoundaryWarningMarker.cs
MoonpalaceBiomePairDefinition.cs
MoonpalaceBiomePairCatalog.cs
```

## Required test gates

```text
MoonpalaceBiomePairCatalogTests:          220 / 220 PASS
MoonpalaceBiomePairContractTests:         180 / 180 PASS

MAP07 required total:                   5422 / 5422 PASS
MAP06 required total:                   2746 / 2746 PASS
  Runtime/category group:               2706 / 2706 PASS
  OptionalRegion overlay Scene group:     40 /   40 PASS
MAP05 required total:                   1959 / 1959 PASS
  Runtime/category group:               1933 / 1933 PASS
  MandatoryRoute overlay Scene group:     26 /   26 PASS

Actually executed required total:      10527 / 10527 PASS
Required failed / skipped:                 0 / 0
Unity version:                         6000.3.8f1
Unity compile errors:                         0
Final Console errors / warnings:            0 / 0
Relevant warnings:                             0
PlayMode tests:                              N/A
Scene/Prefab changes:                       NONE
```

The Unity-MCP observer disconnected once while the MAP05 group continued inside
the same Editor. Reconnecting to the same `Constant@ced6e0dfc4a31d45`
instance and job ID recovered the final `1933/1933 PASS` result. No test failed
or skipped. The final Console was cleared and re-read with zero errors and zero
warnings.

## Static and change-scope gates

```text
Assets meta:                              3409 -> 3419
Folder-meta branch:                      BOTH BOUNDARIES FOLDERS CREATED
New Runtime production C# / matching meta:    6 / 6
New Runtime test C# / matching meta:           2 / 2
New Editor production C# / matching meta:      0 / 0
New Editor test C# / matching meta:            0 / 0
New folder meta:                                   2
Task-local existing boundary test C# modified:     0 <= 20
Matching existing boundary-test meta modified:     0
Assets duplicate GUID groups:                      0

Authoring CSV / matching meta:                  50 / 50
Preserved Authoring manifest SHA-256:
4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Authoring CSV tracked changes:                       0
Generated CSV files created:                         0

Scene / Prefab tracked changes:                    0 / 0
ProjectSettings / Packages tracked changes:        0 / 0
asmdef / asmref tracked changes:                   0 / 0

MAP08_02+ production symbol hits:                    0
MAP09+ production symbol hits:                       0
Unapplied MCP patches:                               0
```

The two allowed new folder meta paths are:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries.meta
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries.meta
```

The Authoring source was not written and has no tracked change; its approved
`50 CSV / 50 matching meta` inventory and baseline manifest remain unchanged.
No generated CSV, Scene, Prefab, ProjectSettings, Packages, asmdef, asmref,
Editor production code, or existing phase-boundary test was changed.

The pre-existing user modification to `Constant.slnx` was preserved and is not a
MAP08_01 change.

## Finalization eligibility

All patch, contract, focused/regression test, compile, Console, GUID,
source-preservation, forbidden-symbol, and change-scope gates pass. MAP08_01
alone is eligible to become `COMPLETE`; Current Task may become `NONE`.
MAP08_02 and every later Task remain locked and must not be read or started.
