# SV5_01 Approval Record

## User-approved baseline

The approved reference is the static 624×416 spatial composition supplied in
the SV5 baseline package: 1×1 tiles, 4×4 Patterns, 12×8 MicroChunks, and a
52×52 MicroChunk world. Large places are read as one terrain through ordinary
rooms, caves, side paths, and rejoin corridors. The example's 101 regions and
166 named links are measured reference data, not a fixed per-seed quota.

## New rules registered for later ownership

- A jump-map route needs reachable SOLID support and safe exposed SOLID Grab
  edges; ONE_WAY platforms remain allowed but cannot be the only support type.
- Existing ability bounds remain ordinary +1 rise and Jump+Grab maximum +2
  rise. This record does not change Player parameters.
- Local 1–2 tile outline changes are allowed only when entrances, headroom,
  support, Grab, landing, and protected neighboring space remain valid.
- The initial operating targets of one real Grab and two visibly varied outline
  areas are production/review criteria, not fabricated user-specified quotas.

## Explicit separation

This is a document and static-input baseline. Unity physics, actual Player
traversal, progression, devices, runtime generation, and full-world completion
are `NOT_RUN`. Later SV5 Tasks own those claims and cannot infer them here.

## Immutable sources

| Source | SHA-256 |
| --- | --- |
| `SV5_README.md` | `d44aba4bc9ea03a2638df1682e4df5284ae4a88830050d55bd651f15a9b3c4d2` |
| `MCP_INBOX/SV5_START.md` | `9f440662458d69a74127fa81c4af89e35649dd06426ea19071285f1fd0ddbbef` |
| source specification | `4a4f6a1c9dcb74ae63e4ee30269551f133aabdf256caff5ee4374276d385dcdf` |
| input manifest | `74d4435f8128d44943b54774416329e027a7c8f584e42225c0504e600e2f5456` |
