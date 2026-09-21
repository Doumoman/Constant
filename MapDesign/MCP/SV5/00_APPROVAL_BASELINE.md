# SV5 Approval Baseline and One-time Registration Boundary

## Purpose

This record registers the SV5 execution plan supplied through
`MCP_INBOX/SV5_START.md`. It is a documentation-only registration boundary;
it does not assert that the approved map is playable or that any Unity feature
has been implemented.

## Fixed input identity

| Role | Path | SHA-256 |
| --- | --- | --- |
| root handoff README | `SV5_README.md` | `d44aba4bc9ea03a2638df1682e4df5284ae4a88830050d55bd651f15a9b3c4d2` |
| manifest | `SV5_FILES.json` | `74d4435f8128d44943b54774416329e027a7c8f584e42225c0504e600e2f5456` |
| handoff | `MCP_INBOX/SV5_START.md` | `9f440662458d69a74127fa81c4af89e35649dd06426ea19071285f1fd0ddbbef` |
| source specification | `MCP/INPUTS/SV5/tasks/SV5_01_APPROVAL_BASELINE.md` | `4a4f6a1c9dcb74ae63e4ee30269551f133aabdf256caff5ee4374276d385dcdf` |

## Registration scope

The explicit SV5 start instruction authorizes the one-time addition of the 45
IDs in `01_SEQUENCE_V5.md` to the active Master and Status tables. They are
registered LOCKED first; the separately bound SV5_01 Task is then the only ID
permitted to move through the unchanged normal `single_task_v1` Apply and
Finalize flow. SV5_02~SV5_45 remain locked.

This is not a generic unknown-ID exception. Every later SV5 Task must use the
normal predecessor Result/installed-Task SHA checks in `02_PROTOCOL_V5.md`.
SV5 is the sole active map-design lineage; predecessor queues and histories are retired.

## Approval scope

- Approved visual baseline: 624×416, 1×1 tiles, 4×4 Patterns, 12×8
  MicroChunks, and 52×52 MicroChunks across the world.
- Large places connect through ordinary rooms, caves, side paths, and rejoin
  corridors; the example's 101 places and 166 links are measured evidence, not
  a per-seed quota.
- Future jump maps require reachable SOLID support and safe Grab edges, may use
  ONE_WAY platforms, preserve existing +1 / Jump+Grab +2 abilities, and use
  local 1–2 tile outline variation without violating protections.
- This registration is static/plan evidence only. Physics, Unity play,
  progression, device behavior, and full-world completion are `NOT_RUN`.
