# SV5_20_FIX01 — physical JS_LINK_02 geometry correction

## Authority and lifecycle

This document supplements the already-installed `SV5_20_JUMP_PLAYER` Task
while that Task is CURRENT. It does not create a new lifecycle Task and must not
be copied to MCP_INBOX. Do not rerun native Apply. The final SV5_20 Result and
BINDING must record this amendment's FILES SHA and `GEOMETRY_PATCH.json` SHA.

All original SV5_20 clauses remain in force except where C01–C06 below
explicitly add the FIX01 overlay and corrected effective-link binding.

## C01 — proven defect

- The accepted SV5_18 JS_LINK_02 logical trace rises from body y=4 to y=8,
  while the actual Player rises only about 1.23 cells.
- In MIRROR_X, SOLID `(9,4)` and `(9,5)` stop leftward run-up from takeoff
  `(10,4)`. R0 has the exact mirror at `(14,4)` and `(14,5)`.
- The PlayMode failure XML is byte-locked by this package. Do not classify this
  as Player tuning failure and do not weaken the actual-Player test.

## C02 — exact six-cell overlay

- Apply exactly the six operations in `GEOMETRY_PATCH.json` at composition time
  inside `Sv5JumpPlayerVerification`; do not modify SV5_17 occupancy CSV/source.
- Remove lower obstruction cells `(14,4)`, `(14,5)` in R0 and `(9,4)`, `(9,5)`
  in MIRROR_X. Preserve the SOLID overhang/support cells `(14,6)` and `(9,6)`.
- Extend the receiving TOP_ONLY platform by one cell at R0 `(17,4)` and MX
  `(6,4)`. These new platform cells are not grabbable.
- The six operations are exact horizontal mirrors under `x'=23-x`, have one
  FIX01 owner, and are the entire geometry correction budget. No fallback carve,
  widening, extra platform, or world-scale repair is permitted.

## C03 — corrected effective link

- R0 JS_LINK_02 is takeoff `(13,4)` to landing `(17,5)`, followed by a supported
  one-cell walk to the unchanged JS_LINK_03 takeoff `(18,5)`.
- MIRROR_X JS_LINK_02 is takeoff `(10,4)` to landing `(6,5)`, followed by a
  supported one-cell walk to the unchanged JS_LINK_03 takeoff `(5,5)`.
- Preserve 9 main links per recipe. The landing override does not create a new
  jump link and does not alter JS_LINK_03.
- Replace the impossible +4-cell logical arc only in SV5_20 effective evidence.
  Actual fixed-step Player traces remain authoritative; do not fabricate a new
  discrete parabola and call it physical proof.

## C04 — immutable predecessors and recovery

- Do not edit or regenerate SV5_17, SV5_18, or SV5_19 committed source,
  CSV/JSON/SVG/XML, Task, Archive, Result, or Binding files.
- Compose their accepted data plus this explicit overlay into
  `player_composed_occupancy.csv` and `player_effective_links.csv` owned by
  SV5_20.
- Preserve all 18 SV5_19 representative probes and recovery checkpoint orders.
  Re-run all 18 physical recovery cases against the corrected composed fixture.
- Preserve Player runtime/settings/input/prefab bytes. No run, jump, Grab,
  collider, gravity, one-way, or timing retune is authorized.

## C05 — required FIX01 evidence

Add to `MapDesign/MCP/GENERATED/SV5_20_JUMP_PLAYER/`:

- `player_geometry_fix01.json`
- `player_geometry_patch.csv`
- `player_composed_occupancy.csv`
- `player_effective_links.csv`
- `independent_jump_player_fix01_audit.json`

`player_cases.csv` must additionally expose effective takeoff/landing integer
cells and `geometry_patch_id`. The visual SVG must mark the four removed cells,
two added TOP_ONLY cells, corrected link-02 landings, and actual trace.

## C06 — resumed verification

1. Keep the existing failed XML unchanged as `targeted_playmode_before_fix01.xml`
   or byte-identical source evidence; never rewrite it into PASS.
2. Add EditMode negative checks for any seventh operation, missing mirror,
   removed overhang, non-TOP_ONLY extension, changed predecessor, and Player
   retune.
3. Re-run targeted EditMode and PlayMode using the actual Player. Test input may
   choose the jump moment while still on the edge support; it may not teleport
   after START or bypass collision.
4. Run the original `check_jump_player.py`, then this package's
   `check_jump_player_fix01.py`. Both must pass.
5. Inspect the updated SVG. Then continue the original RMAP02–04 direct
   regression, one full SV5 regression, post-readonly, Finalize, commit, Result,
   and exact-blob Review ZIP sequence.

If any other physical link fails after this exact correction, stop with that
typed link and trace. Do not spend the six-cell budget elsewhere and do not
silently add another repair.

