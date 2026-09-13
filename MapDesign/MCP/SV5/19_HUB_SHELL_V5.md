# SV5 actual-cell six-way hub shell

SV5_11 adds one deterministic hub shell after the accepted SV5_10 sidepath payload. Candidate discovery indexes
the immutable final occupancy, rooms, reservations, ports, gates, loops, sidepaths, and supported external
endpoints once. A candidate uses only a changed-cell overlay and local cardinal adjacency; no candidate copies the
whole world or runs a whole-world breadth-first search.

The preferred footprint is exactly 24×40 actual cells and contains a 12×30 passable inner volume. Every written
cell records its HubId, final AIR/SOLID/reserved/neck role, and owning 4×4 MicroPattern coordinate. The central
vertical tree slot is continuous reservation evidence only. It creates no trunk collision, branch, landing,
automatic ladder, Grab decision, or Player result.

Six sockets are declared: left and right LOW/MID/HIGH. Each shell aperture is 6–7 actual cells high. Only sockets
with a cardinal connection to a distinct existing RoomId and SpaceGroupId become active Ports. Four through six
connections are accepted; fewer than four rejects the candidate, and unused sockets never count as connections.

The accepted batch preserves protected AIR/SOLID, core route, gates, Type0 reservations, approved loops, and
sidepaths. The pre-existing physical/FSM product remains successful over nine legal states and six resource
orders. Default and repeat profiles export the same evidence schema with deterministic profile-specific HubId,
cell digest, sockets, Ports, connections, validation, and SVG.

Readiness remains deliberately limited: `TreeGrabGeometryReady=false`, `ComposedGeometryReady=false`, and
`PlayerVerified=false`. SV5_12_TREE_GRAB owns all actual tree traversal work and remains locked.
