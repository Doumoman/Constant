# MAP08_07 - Author Crater Mill Boundaries Result

```text
TASK: MAP08_07_AUTHOR_CRATER_MILL_BOUNDARIES
STATUS: PASS
MAP08_07: COMPLETE ELIGIBLE
MAP08_08_AUTHOR_CRATER_DOUGH_BOUNDARIES: LOCKED / DO NOT START
```

## Patch Provenance And Task Repair

```text
Prior MAP08_06 Result SHA-256:
618cec23763ab38d4053a30ae348a4d6c187e2a8d4587d786247a514956a2ece

Prior MAP08_06 installed/repaired Task SHA-256:
24799620d254846e3a99c0a3dadcb00156ab3f6f4804bbf09d2ce4eacda70293

Applied MAP08_07 receipt SHA-256:
1493f0a393fbe4744393a7ee7b6c77f3e865442c7d83826f1b37ca4d43f3afc4

Installed/repaired MAP08_07 Task SHA-256:
bf9085abb16be5c0bc736fa78b709fd32972f5903ba332622860d41d13aa4577
```

The applied v1.0 Task named eight nonexistent
`Assets/_Game/Map/Runtime/Data/WorldGeneration/Authoring/*` paths. Before
implementation those read/write entries were repaired in the installed Task
only to the already-installed source-of-truth paths under
`Assets/_Game/Map/Data/WorldGeneration/Authoring/Boundary`,
`MicroChunk`, and `Route`. The correction did not open another pair or
change any CSV schema or ownership rule.

## Authored Candidate Matrix

Exactly four active `PAIR_CRATER_MILL` candidates and four backing
microchunks exist:

```text
BOUND_RUIN / HORIZONTAL
  BCH_CRATER_MILL_H_RUIN_01
  MC_BOUND_CRATER_MILL_H_01

BOUND_RUIN / VERTICAL
  BCH_CRATER_MILL_V_RUIN_01
  MC_BOUND_CRATER_MILL_V_RUIN_01

BOUND_SOFT_BLEND / HORIZONTAL
  BCH_CRATER_MILL_H_SOFT_01
  MC_BOUND_CRATER_MILL_H_SOFT_01

BOUND_SOFT_BLEND / VERTICAL
  BCH_CRATER_MILL_V_SOFT_01
  MC_BOUND_CRATER_MILL_V_SOFT_01
```

The existing horizontal RUIN starter candidate, its 96 cells, and its two
sockets were preserved. Three missing candidate/catalog rows, 288 tile rows,
and six socket rows were added.

## CSV Ownership And Content Evidence

```text
boundary_chunk_catalog.csv: +3 / -0
microchunk_catalog.csv:      +3 / -0
microchunk_tile_cells.csv: +288 / -0
microchunk_sockets.csv:      +6 / -0

Owned candidates:       4
Owned microchunks:      4
Owned tile rows:      384 = 4 * 96
Owned socket rows:      8 = 4 * 2
CSV UTF-8 BOM:        4/4 preserved
Matching CSV meta changes: 0
```

Every owned microchunk has exact 12x8 coordinate coverage and contains both
`G_MOON_ROCK` and `G_MILL_METAL`, both `DB_CRATER` and
`DB_MILL`, `M_ROUTE_MAIN`, and `M_SOCKET`. Therefore every owned
candidate has two warning marker categories: Tile and Background.

Horizontal candidates have exactly L/R WALK sockets using
`EDGE_H_MID_WALK`; vertical candidates have exactly U/D CLIMB sockets
using `EDGE_V_CENTER_CLIMB`. All eight sockets are MANDATORY,
`mandatory_allowed=1`, `tool_requirement=NONE`, and have
`minimum_safe_tiles=2`.

The four current CSVs were compared to HEAD after removing only the six newly
owned IDs. All remaining rows compared exactly. In particular all
`PAIR_CRATER_ROOT` candidate, catalog, tile, and socket rows compared
exactly in all four files:

```text
OtherPairRowsModified: 0
CraterRootRowsModified: 0
```

## Runtime And Test Surface

```text
New Runtime production C#/meta: 4/4
New Runtime EditMode test C#/meta: 2/2
New Runtime folder meta: 0
Existing MAP08 production C# modified: 0
Existing MAP08 test C# modified: 1
Existing matching C# meta modified: 0
New Editor production/test C#/meta: 0/0
```

Production code provides immutable Crater↔Mill authoring DTOs, the canonical
four-entry candidate matrix, an immutable validation report, and deterministic
validation of exact IDs, matrix coverage, 12x8 cells, warning categories,
socket shapes, no-tool mandatory traversal, generated-output count,
other-pair mutation count, and explicit Crater↔Root mutation count.

The existing Crater↔Root authoring fixture changed one expected non-owned
candidate count from 1 to 4. No Crater↔Root runtime or CSV content changed.
Existing resolver, mandatory filter, tool requirement, warning probe, index,
key, transform, and biome-pair production contracts were not modified.

## Unity Verification

```text
Project root: C:/Users/user/Documents/GitHub/Optimal-Selection/Constant
Unity: 6000.3.8f1
Mode: EditMode
```

Authoritative jobs:

```text
MAP08_07 focused:
950146774d3a46f5a4bb801b5e4e88e7  720/720

MAP08 required focused total:
3af2569a90a547d0ba2bb3809e093bd0  4140/4140

MAP07 required regression:
cb571e68ecc04f7bbb5c351adb1534cb  5422/5422

MAP06 required regression:
186c83648a89447f84309a57436f610e  2746/2746

MAP05 required regression:
c0e025fe961242eb80ad4b7a03ffa228  1959/1959
```

```text
MoonpalaceCraterMillBoundaryAuthoringTests: 360/360
MoonpalaceCraterMillBoundaryValidatorTests: 360/360
MoonpalaceCraterRootBoundaryAuthoringTests: 360/360
MoonpalaceCraterRootBoundaryValidatorTests: 360/360
MoonpalaceBoundaryWarningContractTests: 260/260
MoonpalaceBoundaryWarningProbeTests: 260/260
MoonpalaceMandatoryBoundaryFilterTests: 320/320
MoonpalaceBoundaryToolRequirementTests: 200/200
MoonpalaceBoundaryChunkResolverTests: 420/420
MoonpalaceBoundaryTransformPolicyTests: 260/260
MoonpalaceBoundaryCandidateIndexTests: 360/360
MoonpalaceBoundaryCandidateKeyTests: 220/220
MoonpalaceBiomePairCatalogTests: 220/220
MoonpalaceBiomePairContractTests: 180/180

Required subset total: 14267/14267
Required failed/skipped: 0/0
Unity compile errors: 0
Final Console errors/warnings: 0/0
Relevant warnings: 0
```

The first MAP08 integration run exposed only the stale Crater↔Root
non-owned-candidate assertion and was superseded after the allowed one-line
test integration repair. MAP06/MAP05 continued inside Unity during temporary
MCP transport disconnects and were recovered by job ID without duplicate
execution. Full unrelated EditMode tests were not run.

## Static Gates

```text
Assets meta: 3687 -> 3693
Assets/_Game/Map meta: 570 -> 574
Duplicate GUID groups: 0

Authoring CSV/matching meta: 50/50
Authoring tracked CSV changes: exact 4
Authoring row deltas: +3 / +3 / +288 / +6
Generated CSV changes: 0

Authoring manifest before:
c10083a3fe89e582cec9249eef6e556471a13b5b849ac2c3b5f0a3b3b940bdfa

Authoring manifest after:
d55393e0d60e907462fe6e406b3b8705c98ff82c08b839bd64b54b5cd53808a2

Scene/Prefab tracked changes: 0/0
ProjectSettings/Packages tracked changes: 0/0
asmdef/asmref tracked changes: 0/0
MAP08_08+/MAP09+ forbidden production symbol hits: 0/0
Unapplied MCP patches: 0
git diff --check errors: 0
```

The after-manifest is the SHA-256 of the sorted Authoring-relative CSV path
plus tab plus LF-normalized file SHA-256 records, joined with LF.

## Worktree And Commit Scope

The Task-owned scope is the MAP08_07 patch payload and receipt, repaired
installed Task, Master/Status patch documents, four Authoring CSV files, four
Runtime scripts with matching metas, two new EditMode test scripts with
matching metas, the one-line Crater↔Root integration test update, and this
Result.

`Constant.slnx` and the already-applied untracked
`MAP07_13_FINALIZE_MAP07_EXIT_APPROVED` inbox are pre-existing unrelated
worktree items. They remain preserved and excluded from staging.

The commit hash is pending Phase D because this PASS Result and the finalized
Status must be committed atomically. No push is authorized.
