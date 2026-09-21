# SV5_14 — actual SOLID/ONE_WAY jump route geometry

## Purpose and boundary

Compile one deterministic 24x32 local jump-room fixture into actual 1x1 support
cells and an ordered bottom-to-upper ascent route. The route must use reachable
SOLID blocks as actual takeoff or landing supports; decorative solids do not
count. ONE_WAY supports remain valid but cannot be the entire route.

This Task owns support geometry only. SV5_15 adds the real Grab segment,
SV5_16 deforms the outline, SV5_17 adds recipe variants, SV5_18 performs full
body clearance, SV5_19 adds failure recovery, and SV5_20 performs live Player
verification.

## C01 — local canvas, not world placement

- Work in local integer coordinates 0 <= x < 24 and 0 <= y < 32.
- The approved example #012 origin (360,224) is review metadata only. It is not
  an authoritative production coordinate and must not overwrite world placement.
- Define explicit entry and exit sockets, an ordered required support sequence,
  and matching route links. All must be inside the local canvas.
- Do not generate, copy, scan, BFS, or mutate the 624x416 world in new targeted
  tests. Do not attach this room to progression/core/Type0 data yet.

## C02 — actual support geometry

- Emit every occupied support as actual 1x1 cells derived from typed supports.
- Required-route supports number at least 10. At least four are active,
  non-decorative SOLID and at least three are active, non-decorative ONE_WAY.
- A counted SOLID must be the source or target of a required route link. A nearby
  unreachable or decorative block never satisfies the SOLID requirement.
- Support widths are 2-4 cells. SOLID height is 1-3 cells; ONE_WAY height is one
  top-only row. Supports may be irregularly sequenced but may not overlap.
- No occupied 6x6 all-SOLID window is permitted.

## C03 — ordered ascent route

- The route begins on a supported lower entry and ends on a supported upper exit.
  Its top-surface vertical span is at least eight cells.
- Each required link uses the immutable SV5_13 contract calculation. Facing-edge
  gap_air is 0-3 and top-surface rise is 0 or +1.
- Each link records source, target, direction, takeoff, landing, gap, rise, mode,
  required-route flag, and sequence order.
- Every takeoff and landing is on the corresponding inclusive top support interval.
- At least one link lands on SOLID and at least one departs from SOLID.
- Required completion may be one-way. Do not fabricate a reverse route.
- Do not use JUMP_GRAB to satisfy this Task. SV5_15 owns a genuine +2 or other
  Grab-dependent segment and the associated contact/clearance evidence.

## C04 — conservative reserved clearance

- For every required-route support, record at least one foot position and two
  reserved AIR cells above it.
- These cells must lie within the canvas and may not overlap any SOLID or ONE_WAY
  cell.
- This is a static reservation only. It does not replace the SV5_18 full collider
  sweep or SV5_20 live Player run.

## C05 — deterministic source and exports

Create new Sv5JumpSolidGeometry and Sv5JumpSolidExport implementations that
consume Sv5JumpContract; do not copy or redefine its support kinds, formulas,
or validation states.

Export MapDesign/MCP/GENERATED/SV5_14_JUMP_SOLID/:

- jump_solid.json
- supports.csv
- support_cells.csv
- route_support_sequence.csv
- route_links.csv
- support_clearance.csv
- solid_use.csv
- jump_solid_validation.json
- preview/jump_solid.svg
- visual_export_audit.json
- independent_jump_solid_audit.json
- targeted/focused NUnit XML and final BINDING.json

All CSV, JSON, and SVG data comes from the same typed fixture. The SVG shows the
full 24x32 boundary, entry/exit, SOLID and ONE_WAY cells, ordered route, takeoff
and landing points, and a legend stating that Grab is deferred to SV5_15.

## C06 — independent checks and failure cases

The independent checker reconstructs support rectangles/cells, rejects overlap,
checks bounds, scans every 6x6 window, follows the exact ordered route, recomputes
every gap/rise from SV5_13 rules, checks takeoff/landing support, proves counted
SOLID use, and verifies clearance cells against final local occupancy.

Add at least 16 meaningful tests including: all-ONE_WAY rejection; decorative or
unreachable SOLID rejection; missing support cell; extra support cell; overlap;
out-of-bounds cell; 6x6 fill; broken route order; unsupported takeoff; unsupported
landing; gap above three; rise above one; insufficient vertical span; blocked
reserved clearance; deterministic repeat; export round-trip; and valid one-way
mixed route.

## C07 — readiness and time control

- New targeted tests use only the 24x32 fixture and finish without a production
  world build/search.
- Run compile and targeted tests first, then the independent checker and rendered
  SVG inspection. Only after all pass run the full SV5 regression exactly once.
- Final focused count is at least 247 with zero failed/skipped/inconclusive.
- On PASS set JumpSolidGeometryReady=true. Keep JumpGrabGeometryReady=false,
  ComposedGeometryReady=false, and PlayerVerified=false.
- Do not change Player tuning, SV5_13 contract source/evidence, tree/hub geometry,
  scenes, prefabs, Packages, ProjectSettings, Master, or unrelated dirty files.
- Do not introduce retired partition logic, all-endpoint-pair comparison, or
  per-candidate world copy/BFS. Keep SV5_15 locked and do not push.

