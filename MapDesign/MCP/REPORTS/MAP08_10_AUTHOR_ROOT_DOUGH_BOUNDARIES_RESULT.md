# MAP08_10 - Author Root Dough Boundaries Result

```text
TASK: MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES
STATUS: PASS
MAP08_10: COMPLETE ELIGIBLE
MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES: LOCKED / DO NOT START
```

## Patch Provenance

```text
Prior MAP08_09 Result SHA-256:
c923b445d9dc7b1d057ec368cb154a5745f2e473e67a736fcf8ee20f66a9ef87

Prior MAP08_09 installed Task SHA-256:
c559168ad86e866124cc87843926d66b98f59ad1243a33755fce7f78621fd01f

Applied MAP08_10 receipt SHA-256:
100a876159c634a82e48c04a2c07cad7857f9571c5041cbf97ee3e669078bb75

Installed MAP08_10 Task SHA-256:
f8050e209aa342602616afadc7cf0c3258731c8034ab9aff94f2bd94918d04f8
```

## Authored Candidate Matrix

Exactly five active, reversible, positive-weight `PAIR_ROOT_DOUGH`
candidates and five backing microchunks exist:

```text
BOUND_TUNNEL / HORIZONTAL
  BCH_ROOT_DOUGH_H_TUNNEL_01
  MC_BOUND_ROOT_DOUGH_H_TUNNEL_01

BOUND_TUNNEL / VERTICAL
  BCH_ROOT_DOUGH_V_TUNNEL_01
  MC_BOUND_ROOT_DOUGH_V_TUNNEL_01

BOUND_LAYER / VERTICAL
  BCH_ROOT_DOUGH_V_LAYER_01
  MC_BOUND_ROOT_DOUGH_V_LAYER_01

BOUND_SOFT_BLEND / HORIZONTAL
  BCH_ROOT_DOUGH_H_SOFT_01
  MC_BOUND_ROOT_DOUGH_H_SOFT_01

BOUND_SOFT_BLEND / VERTICAL
  BCH_ROOT_DOUGH_V_SOFT_01
  MC_BOUND_ROOT_DOUGH_V_SOFT_01
```

`BOUND_LAYER/HORIZONTAL` candidate count is exactly zero. Pair identity is
`BIO_CASSIA_ROOT <-> BIO_MOON_DOUGH`; the installed pair rule remains
`45|30|25` with `BOUND_TUNNEL` as default.

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

Every owned microchunk has exact `12x8` coordinate coverage and contains
both `G_CASSIA_WOOD` and `G_DOUGH_SOLID`, both `DB_ROOT` and
`DB_DOUGH`, `M_ROUTE_MAIN`, and `M_SOCKET`. Tile and Background warning
evidence is exactly two categories for every candidate.

Horizontal candidates expose a two-cell-high clear L/R corridor with WALK
sockets using `EDGE_H_MID_WALK`. Vertical candidates expose a three-cell-wide
clear U/D corridor with CLIMB sockets using `EDGE_V_CENTER_CLIMB`. All sockets
are MANDATORY, `mandatory_allowed=1`, `tool_requirement=NONE`, and
`minimum_safe_tiles=2`. MAP07 socket-clearance and reachability validation
passes for R0 and the allowed mirror transform.

All four CSV diffs are additions only:

```text
ExistingRowsModified: 0
OtherPairRowsModified: 0
CraterRootRowsModified: 0
CraterMillRowsModified: 0
CraterDoughRowsModified: 0
RootMillRowsModified: 0
GeneratedCsvCreated: 0
```

## Runtime And Test Surface

```text
New Runtime production C#/meta: 4/4
New Runtime EditMode test C#/meta: 2/2
New Runtime folder meta: 0
Existing MAP08 production C# modified: 0
Existing MAP08 test C# modified: 4
Matching existing C# meta modified: 0
New Editor production/test C#/meta: 0/0
```

Production code provides immutable Root-Dough authoring DTOs, an exact
five-entry candidate matrix, an immutable content report, and deterministic
validation of pair identity, exact ID sets, the Vertical-only Layer rule,
positive weights, reversibility, 96-cell coverage, exact warning evidence,
socket shape, mandatory no-tool semantics, additive-only preservation, and
zero generated output.

The four existing pair authoring test edits update only their expected
non-owned candidate counts after the five new candidates are installed:
Crater-Root `15 -> 20`, Crater-Mill `17 -> 22`, Crater-Dough `16 -> 21`,
and Root-Mill `15 -> 20`.

## Unity Verification

```text
Project root: C:/Users/user/Documents/GitHub/Optimal-Selection/Constant
Unity: 6000.3.8f1
Mode: EditMode
```

Authoritative final jobs:

```text
MAP08_10 focused:
bfce93b6c4244393968cd4f66ea98f0a  720/720

MAP08 required focused total:
8d96d9cff7bd4e528715dd36d10be669  6300/6300

MAP07 required regression:
b2bca751c73949d3903e1d22f9a92b83  5422/5422

MAP06 required regression:
ee07658928844d22a15fefd81c29e8ff  2746/2746

MAP05 required regression:
7bbce9affcfc4b91b7ebf8e1ebeb9fb6  1959/1959
```

```text
MoonpalaceRootDoughBoundaryAuthoringTests: 360/360
MoonpalaceRootDoughBoundaryValidatorTests: 360/360
MAP08 required total: 6300/6300
MAP07 required total: 5422/5422
MAP06 required total: 2746/2746
MAP05 required total: 1959/1959
Required subset total: 16427/16427
Required failed/skipped: 0/0
Unity compile errors: 0
Final Console errors/warnings: 0/0
Relevant warnings: 0
```

The initial MAP07 aggregate exposed blocked socket clearances in the newly
authored rows. The owned rows were repaired to clear deterministic horizontal
and vertical corridors. After a domain reload cleared the CSV harness cache,
the focused MAP07 exit/round-trip repair gate passed `800/800`, followed by
the authoritative full MAP07 pass above. Long aggregate runs disconnected the
MCP transport transiently; every authoritative Job ID was recovered without
duplicating an in-flight job.

## Static Gates

```text
Global Assets meta at current HEAD baseline: 3782 -> 3788
Task arithmetic after accepted non-Map baseline drift: +6
Assets/_Game/Map meta: 582 -> 586
Duplicate GUID groups: 0

Authoring CSV/matching meta: 50/50
Authoring tracked CSV changes: exact 4
Authoring row deltas: +5 / +5 / +480 / +10
Generated CSV files: 0

Authoring manifest before:
b67b1235806a1acb4d5163917aa97ac93863e3cfba29c7842f656afc0d57096a

Authoring manifest after:
0842d140f399da076cf41218b360e784cee776c62266bd251f4debb18657a950

Scene/Prefab tracked changes: 0/0
ProjectSettings/Packages tracked changes: 0/0
asmdef/asmref tracked changes: 0/0
MAP08_11+/MAP09+ forbidden production symbol hits: 0/0
Unapplied MCP patches: 0
git diff --check errors: 0
```

The Task document inherited the prior global Assets meta value `3705`.
Current HEAD includes 77 committed non-Map metas from parallel Live/character
work, so the preserved actual baseline is `3782`. No one of those 77 metas is
under `Assets/_Game/Map`; MAP08_10 adds exactly six new C# metas globally and
four under Map, preserving the Task's required arithmetic.

The Authoring manifest is the SHA-256 of sorted Authoring-relative CSV path,
tab, and LF-normalized UTF-8-BOM content SHA-256 records joined with LF.

## Worktree And Commit Scope

Task-owned scope consists of the MAP08_10 patch payload and receipt, installed
Task/Master/Status documents, four Authoring CSV files, four Runtime scripts
with matching metas, two new EditMode tests with matching metas, the four
one-line prior-pair integration expectations, this Result, and the finalized
Status document.

`Constant.slnx` and the already-applied untracked
`MAP07_13_FINALIZE_MAP07_EXIT_APPROVED` inbox are pre-existing unrelated
items. They remain preserved and excluded from staging.

The commit hash is created in Phase D after this PASS Result and the finalized
Status are staged atomically. The exact hash is reported in the final handoff.
No push is authorized.
