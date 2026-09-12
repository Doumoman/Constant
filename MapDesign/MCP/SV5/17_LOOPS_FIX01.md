# SV5 loop topology evidence correction

SV5_09_FIX01 replaces the former self-reported loop proof with evidence rebuilt from the actual
624 x 416 occupancy and room topology. It does not change the SV5_09 density contract: production
profiles still require at least 16 accepted links and at least 12 occupied sectors.

Each endpoint identifies an actual `Sv5InfillRoom.Id`. Aperture ownership remains separate in
`FromApertureOwner` and `ToApertureOwner`, so geometry ownership never substitutes for a topology
vertex. Legacy ordinary rooms whose Infill parent field points to themselves are connected to their
host-access vertex; self topology edges are not exported.

The baseline context is built once per generated plan and owns occupancy, supported-foot nodes,
adjacency, components, room edges, Tarjan bridges, and cached endpoint-pair shortest costs. Candidate
evaluation keeps only changed cells in an overlay and reevaluates affected foot nodes locally.

`ordered_foot_path` begins at the last baseline supported-foot node before leaving the source room
and ends at the first such node in the target room. Only those endpoints may overlap the baseline
foot graph; the internal path contains at least two newly carved AIR cells. Room approaches are
exported separately and do not affect loop length or contact counts.

The independent checker imports no production code. It reconstructs supported-foot nodes from
occupancy, runs BFS costs, calculates component and cycle rank values, computes bridges, verifies
typed loop-edge removal, and compares the reconstructed values with every exported proof. The final
accepted union is also checked over all nine legal states and six resource orders.

Final fixed evidence is in `GENERATED/SV5_09_FIX01/{default,repeat}` and is bound by
`GENERATED/SV5_09_FIX01/BINDING.json`. Composed scene geometry and Player traversal remain outside
this task: `ComposedGeometryReady=false` and `PlayerVerified=false`. SV5_10_SIDEPATH stays LOCKED.
