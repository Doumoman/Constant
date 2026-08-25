# MAP08_09 - Author Root Mill Boundaries Result

```text
TASK: MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES
STATUS: PASS
MAP08_09: COMPLETE ELIGIBLE
MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES: LOCKED / DO NOT START
```

## Patch Provenance

```text
Prior MAP08_08 Result SHA-256:
df4137defbb8e44cba12ef3b74cd8635044b886657525ec128e05dd5b1bd67c9

Prior MAP08_08 installed Task SHA-256:
92106729b49fa13c0fbb95f0338b3b619582a1696f10ac88604401469907d769

Applied MAP08_09 receipt SHA-256:
60ee7fecee950b5975f73d2bfb53ee5e9fa18950c4964153d499aeb0eb5ba75e

Installed MAP08_09 Task SHA-256:
c559168ad86e866124cc87843926d66b98f59ad1243a33755fce7f78621fd01f
```

## Authored Candidate Matrix

Exactly six active, reversible, positive-weight `PAIR_ROOT_MILL`
candidates and six backing microchunks exist:

```text
BOUND_RUIN / HORIZONTAL
  BCH_ROOT_MILL_H_RUIN_01
  MC_BOUND_ROOT_MILL_H_RUIN_01

BOUND_RUIN / VERTICAL
  BCH_ROOT_MILL_V_RUIN_01
  MC_BOUND_ROOT_MILL_V_RUIN_01

BOUND_TUNNEL / HORIZONTAL
  BCH_ROOT_MILL_H_TUNNEL_01
  MC_BOUND_ROOT_MILL_H_TUNNEL_01

BOUND_TUNNEL / VERTICAL
  BCH_ROOT_MILL_V_TUNNEL_01
  MC_BOUND_ROOT_MILL_V_TUNNEL_01

BOUND_SOFT_BLEND / HORIZONTAL
  BCH_ROOT_MILL_H_SOFT_01
  MC_BOUND_ROOT_MILL_H_SOFT_01

BOUND_SOFT_BLEND / VERTICAL
  BCH_ROOT_MILL_V_SOFT_01
  MC_BOUND_ROOT_MILL_V_SOFT_01
```

The exact candidate and microchunk ID sets match the Task contract. Pair
identity is `BIO_CASSIA_ROOT <-> BIO_ABANDONED_MILL`; the existing pair
rule remains `45|35|20` with `BOUND_RUIN` as default.

## CSV Ownership And Content Evidence

```text
boundary_chunk_catalog.csv:  +6 / -0
microchunk_catalog.csv:       +6 / -0
microchunk_tile_cells.csv:  +576 / -0
microchunk_sockets.csv:      +12 / -0

Owned candidates:       6
Owned microchunks:      6
Owned tile rows:      576 = 6 * 96
Owned socket rows:     12 = 6 * 2
CSV UTF-8 BOM:        4/4 preserved
Matching CSV meta changes: 0
```

Every owned microchunk has exact `12x8` coordinate coverage and contains
both `G_CASSIA_WOOD` and `G_MILL_METAL`, both `DB_ROOT` and
`DB_MILL`, `M_ROUTE_MAIN`, and `M_SOCKET`. Tile and Background warning
evidence is exactly two categories for every candidate.

Horizontal candidates have exactly L/R WALK sockets using
`EDGE_H_MID_WALK`. Vertical candidates have exactly U/D CLIMB sockets
using `EDGE_V_CENTER_CLIMB`. All twelve sockets are MANDATORY,
`mandatory_allowed=1`, `tool_requirement=NONE`, and
`minimum_safe_tiles=2`.

All four CSV diffs are additions only:

```text
ExistingRowsModified: 0
OtherPairRowsModified: 0
CraterRootRowsModified: 0
CraterMillRowsModified: 0
CraterDoughRowsModified: 0
GeneratedCsvCreated: 0
```

## Runtime And Test Surface

```text
New Runtime production C#/meta: 4/4
New Runtime EditMode test C#/meta: 2/2
New Runtime folder meta: 0
Existing MAP08 production C# modified: 0
Existing MAP08 test C# modified: 3
Matching existing C# meta modified: 0
New Editor production/test C#/meta: 0/0
```

Production code provides immutable Root-Mill authoring DTOs, the exact
six-entry candidate matrix, an immutable content report, and deterministic
validation of pair identity, exact ID sets, profile/orientation coverage,
positive weights, reversibility, 96-cell coverage, warning evidence, socket
shape, mandatory no-tool semantics, additive-only preservation, and zero
generated output.

The three existing pair test edits update only their expected non-owned
candidate counts after the six new candidates are installed:
Crater-Root `9 -> 15`, Crater-Mill `11 -> 17`, and
Crater-Dough `10 -> 16`.

## Unity Verification

```text
Project root: C:/Users/user/Documents/GitHub/Optimal-Selection/Constant
Unity: 6000.3.8f1
Mode: EditMode
```

Authoritative jobs:

```text
MAP08_09 focused:
b1542a5afb1744e3961ba42165b19269  720/720

MAP08 required focused total:
2665231d62a4425ea6745e2e36fe5d97  5580/5580

MAP07 required regression:
07ea8f98a0e34e9fa75664bbf0594de7  5422/5422

MAP06 required regression:
102bbfe25d4044229ed889559a0b63ba  2746/2746

MAP05 required regression:
58cdb3150f2b46d0873e6c3599ce8496  1959/1959
```

```text
MoonpalaceRootMillBoundaryAuthoringTests: 360/360
MoonpalaceRootMillBoundaryValidatorTests: 360/360
MAP08 required total: 5580/5580
MAP07 required total: 5422/5422
MAP06 required total: 2746/2746
MAP05 required total: 1959/1959
Required subset total: 15707/15707
Required failed/skipped: 0/0
Unity compile errors: 0
Final Console errors/warnings: 0/0
Relevant warnings: 0
```

The long aggregate jobs temporarily delayed or disconnected MCP transport
while Unity continued running. Each original Job ID was recovered and no
required job was duplicated. Transport lifecycle warnings were cleared;
the final Console read returned zero entries.

## Static Gates

```text
Global Assets meta: 3699 -> 3705
Assets/_Game/Map meta: 578 -> 582
Duplicate GUID groups: 0

Authoring CSV/matching meta: 50/50
Authoring tracked CSV changes: exact 4
Authoring row deltas: +6 / +6 / +576 / +12
Generated CSV files: 0

Authoring manifest before:
61d5462d00b7d4f435297523be15d0bef636dfc84a87b05004b209928bacce1b

Authoring manifest after:
b67b1235806a1acb4d5163917aa97ac93863e3cfba29c7842f656afc0d57096a

Scene/Prefab tracked changes: 0/0
ProjectSettings/Packages tracked changes: 0/0
asmdef/asmref tracked changes: 0/0
MAP08_10+/MAP09+ forbidden production symbol hits: 0/0
Unapplied MCP patches: 0
git diff --check errors: 0
```

The Authoring manifest is the SHA-256 of sorted Authoring-relative CSV path,
tab, and LF-normalized UTF-8-BOM content SHA-256 records joined with LF.

## Worktree And Commit Scope

Task-owned scope consists of the MAP08_09 patch payload and receipt,
installed Task/Master/Status documents, four Authoring CSV files, four
Runtime scripts with matching metas, two new EditMode tests with matching
metas, the three one-line prior-pair integration expectations, this Result,
and the finalized Status document.

`Constant.slnx` and the already-applied untracked
`MAP07_13_FINALIZE_MAP07_EXIT_APPROVED` inbox are pre-existing unrelated
items. They remain preserved and excluded from staging.

The commit hash is created in Phase D after this PASS Result and the
finalized Status are staged atomically. The exact hash is reported in the
final handoff. No push is authorized.
