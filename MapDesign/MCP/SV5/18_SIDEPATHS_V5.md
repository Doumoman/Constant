# SV5 world-coordinate sidepath evidence

SV5_10_SIDEPATH layers deterministic returning and dead-end paths on the fixed SV5_09_FIX01
occupancy. Inclusive path length is the count of distinct ordered traversable AIR centerline cells,
including both endpoints for a returning path, and every production path is 20 to 50 cells.

Discovery uses actual room, host connection, port, endpoint, occupancy, supported-foot, and 4 x 4
MicroPattern ownership. Endpoints are ordered by world X, world Y, RoomId, and EndpointId. A sliding
X window stops when the coordinate difference exceeds 49, and only pairs with Manhattan distance at
most 49 proceed. Same-room and directly connected room pairs are rejected, and each normalized
EndpointId pair is evaluated once.

Candidate work reads immutable occupancy and returns worker-local results. Results are merged by
SpaceGroupId, RoomId, EndpointId, CandidateScore, and CandidateId before single-threaded packing.
Candidate evaluation keeps only changed-cell overlays and affected foot nodes; it performs no
per-candidate whole-world copy or whole-world BFS. The final accepted union alone enters global
topology and the nine-state by six-resource-order product.

Production requires at least eight accepted paths, five returning paths, six actual host connection
groups, and at most two accepted paths per group. Evidence records world-coordinate midpoint range
and actual group distribution without inventing a fixed spatial grid.

The originally applied package is preserved byte-for-byte as historical audit evidence. Its fixed-grid
clauses were retired by user correction before Finalize and are not production completion criteria.
Composed scene geometry and Player traversal remain outside this task:
`ComposedGeometryReady=false` and `PlayerVerified=false`.
