# MAP08_08 - Author Crater Dough Boundaries Result

```text
TASK: MAP08_08_AUTHOR_CRATER_DOUGH_BOUNDARIES
STATUS: PASS
MAP08_08: COMPLETE ELIGIBLE
MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES: LOCKED / DO NOT START
```

## Patch Provenance

```text
Prior MAP08_07 Result SHA-256:
59cc98beaa374a319c656c50f0c5aaf26a4f25a29f591eb017bf21d4a9eb995a

Prior MAP08_07 installed/repaired Task SHA-256:
bf9085abb16be5c0bc736fa78b709fd32972f5903ba332622860d41d13aa4577

Applied MAP08_08 receipt SHA-256:
133d2cb6e58da2e165365facc0ce22d8f9063c96c4c00967f65b1079387d448e

Installed MAP08_08 Task SHA-256:
92106729b49fa13c0fbb95f0338b3b619582a1696f10ac88604401469907d769
```

## Authored Candidate Matrix

Exactly five active, reversible, positive-weight `PAIR_CRATER_DOUGH`
candidates and five backing microchunks exist:

```text
BOUND_CLIFF / HORIZONTAL
  BCH_CRATER_DOUGH_H_CLIFF_01
  MC_BOUND_CRATER_DOUGH_H_CLIFF_01

BOUND_CLIFF / VERTICAL
  BCH_CRATER_DOUGH_V_CLIFF_01
  MC_BOUND_CRATER_DOUGH_V_CLIFF_01

BOUND_LAYER / VERTICAL
  BCH_CRATER_DOUGH_V_LAYER_01
  MC_BOUND_CRATER_DOUGH_V_LAYER_01

BOUND_SOFT_BLEND / HORIZONTAL
  BCH_CRATER_DOUGH_H_SOFT_01
  MC_BOUND_CRATER_DOUGH_H_SOFT_01

BOUND_SOFT_BLEND / VERTICAL
  BCH_CRATER_DOUGH_V_SOFT_01
  MC_BOUND_CRATER_DOUGH_V_SOFT_01
```

The exact candidate and microchunk ID sets match the Task contract.
`BOUND_LAYER/HORIZONTAL` candidate count is `0`.

## CSV Ownership And Content Evidence

```text
boundary_chunk_catalog.csv:  +5 / -0
microchunk_catalog.csv:       +5 / -0
microchunk_tile_cells.csv:  +480 / -0
microchunk_sockets.csv:      +10 / -0

Owned candidates:       5
Owned microchunks:      5
Owned tile rows:      480 = 5 * 96
Owned socket rows:     10 = 5 * 2
CSV UTF-8 BOM:        4/4 preserved
Matching CSV meta changes: 0
```

Every owned microchunk has exact `12x8` coordinate coverage. Each contains
both `G_MOON_ROCK` and `G_DOUGH_SOLID`, both `DB_CRATER` and
`DB_DOUGH`, `M_ROUTE_MAIN`, and `M_SOCKET`. Tile and Background warning
evidence is therefore `2` categories for every owned candidate.

Horizontal candidates have exactly L/R WALK sockets using
`EDGE_H_MID_WALK`. Vertical candidates have exactly U/D CLIMB sockets using
`EDGE_V_CENTER_CLIMB`. All ten sockets are MANDATORY,
`mandatory_allowed=1`, `tool_requirement=NONE`, and
`minimum_safe_tiles=2`.

All four CSV diffs are additions only. Existing rows compare unchanged:

```text
OtherPairRowsModified: 0
CraterRootRowsModified: 0
CraterMillRowsModified: 0
GeneratedCsvCreated: 0
```

## Runtime And Test Surface

```text
New Runtime production C#/meta: 4/4
New Runtime EditMode test C#/meta: 2/2
New Runtime folder meta: 0
Existing MAP08 production C# modified: 0
Existing MAP08 test C# modified: 2
Matching existing C# meta modified: 0
New Editor production/test C#/meta: 0/0
```

Production code provides immutable Crater-Dough authoring DTOs, the exact
five-entry candidate matrix, an immutable content report, and deterministic
validation of ID sets, profile/orientation coverage, 96-cell coverage,
warning evidence, socket shape, mandatory no-tool traversal, preservation
counters, and the explicit invalid Layer/Horizontal count.

The two existing test changes update only their expected non-owned candidate
counts after the five new candidates are installed: Crater-Root `4 -> 9` and
Crater-Mill `6 -> 11`. Resolver, filter, tool-requirement, warning, index,
key, transform, and biome-pair production contracts were not modified and
remain covered by the MAP08 aggregate.

## Unity Verification

```text
Project root: C:/Users/user/Documents/GitHub/Optimal-Selection/Constant
Unity: 6000.3.8f1
Mode: EditMode
```

Authoritative jobs:

```text
MAP08_08 focused:
5a734671a84a400facff7a8df40684df  720/720

MAP08 required focused total:
21d30dc62c644633962187ead3a0a036  4860/4860

MAP07 required regression:
83c30b490a3b46d9901b5c71d4d52839  5422/5422

MAP06 required regression body:
5dce03e8e8b747fd918fc6bc38433afd  2566/2566

MAP06 exit fixture:
b1b3eaf5ef3742f5a2acabfea51dce75   180/180

MAP05 required regression:
d88575d09ce84205aeb5fdadb2235b44  1959/1959
```

```text
MoonpalaceCraterDoughBoundaryAuthoringTests: 360/360
MoonpalaceCraterDoughBoundaryValidatorTests: 360/360
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

Required subset total: 14987/14987
Required failed/skipped: 0/0
Unity compile errors: 0
Final Console errors/warnings: 0/0
Relevant warnings: 0
```

The long MAP08/MAP07 jobs and MAP05 transport each temporarily disconnected
MCP while Unity continued running. Their original Job IDs were recovered;
no test job was duplicated. The MAP06 class selection first returned the
expected body `2566`; the separately named flat `Map06ExitTests` supplied
the required remaining `180`, for exact MAP06 `2746/2746`.

Test-runner lifecycle and MCP disconnect warnings were non-content transport
logs. After job recovery, the final Console was cleared and re-read at
`0 errors / 0 warnings`.

## Static Gates

```text
Assets meta: 3693 -> 3699
Assets/_Game/Map meta: 574 -> 578
Duplicate GUID groups: 0

Authoring CSV/matching meta: 50/50
Authoring tracked CSV changes: exact 4
Authoring row deltas: +5 / +5 / +480 / +10
Generated CSV files: 0

Authoring manifest before:
d55393e0d60e907462fe6e406b3b8705c98ff82c08b839bd64b54b5cd53808a2

Authoring manifest after:
61d5462d00b7d4f435297523be15d0bef636dfc84a87b05004b209928bacce1b

Scene/Prefab tracked changes: 0/0
ProjectSettings/Packages tracked changes: 0/0
asmdef/asmref tracked changes: 0/0
MAP08_09+/MAP09+ forbidden production symbol hits: 0/0
Unapplied MCP patches: 0
git diff --check errors: 0
```

The Authoring manifest is the SHA-256 of sorted Authoring-relative CSV path,
tab, and LF-normalized UTF-8-BOM content SHA-256 records joined with LF.

## Worktree And Commit Scope

Task-owned scope consists of the MAP08_08 patch payload and receipt, installed
Task/Master/Status documents, four Authoring CSV files, four Runtime scripts
with matching metas, two new EditMode tests with matching metas, the two
one-line prior-pair integration expectations, this Result, and the finalized
Status document.

`Constant.slnx` and the already-applied untracked
`MAP07_13_FINALIZE_MAP07_EXIT_APPROVED` inbox are pre-existing unrelated
items. They remain preserved and excluded from staging.

The commit hash is pending Phase D because this PASS Result and the finalized
Status must be included atomically. The exact created hash is reported in the
final handoff. No push is authorized.
