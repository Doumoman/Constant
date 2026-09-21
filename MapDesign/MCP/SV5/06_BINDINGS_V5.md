# SV5 code and data bindings

This is an audited entrypoint for future SV5 Tasks, not a feature-completion
claim. It records current source bytes and ownership observed by SV5_03; every
future Task must recheck the current files before changing them.

## Canonical current sources

- `MCP/GENERATED/SV5/rmap15_sites.csv` is the physical-site index. Its eight
  sites are Start, three resources, Village, Forge, shared SealBoss, and Exit.
  Graph reservations, fixed-cell protection, access ports, slots, and SealBoss
  state geometry remain in their separate SV5 exports.
- `Sv5WorldDataContract`, `Sv5WorldGraphPlanner`, `Sv5WorldBiomePlanner`,
  and `Sv5SpecialReservationPlanner` are the read-only starting points for
  SV5_04 through SV5_06; see `BINDINGS.csv` for actual symbols and SHA values.
- `Sv5PatternCatalog`, `Sv5PortCatalog`, `Sv5PatternPool500`, and
  `Sv5ClusterAssemblyPlanner` are existing reuse/adaptation boundaries, not
  an authorization to reshape the world in this Task.
- SV5 final cells and SV5 bake artifacts provide current final-cell and
  static-bake provenance. Their historical PASS does not make SV5 rules or
  later Player/progression work complete.

## Audit groups and future ownership

| Group | Binding result | Future responsibility |
| --- | --- | --- |
| B01 | world data, RNG, stable ID reuse | SV5_04-10, 41, 43 |
| B02 | graph/state reuse | SV5_04-06, 09, 35-36, 44 |
| B03 | biome/ownership reuse | SV5_04, 06-07, 24-26, 41-42 |
| B04 | eight-site/protection reuse | SV5_04-06, 09, 35-39, 41-42, 44 |
| B05 | pattern/port reuse | SV5_06, 11, 13-14, 17, 24-26, 41 |
| B06 | cluster/space adaptation | SV5_06-11, 16, 21, 24-31, 39, 41-42 |
| B07 | final-cell/bake adaptation | SV5_41-43, 45 |
| B08 | Player/Grab/camera adaptation | SV5_12-20, 23, 25, 40, 43-44 |
| B09 | stair/elevator implementation absent | SV5_21-23, 27, 39-40 |
| B10 | slots/special shells reuse | SV5_28-31, 35-39 |
| B11 | rail/train implementation absent | SV5_32-34 |
| B12 | existing static validation reuse | SV5_18-20, 42-45 |
| B13 | VIS boundary reuse, not world approval | SV5_06, 41, 43, 45 |

`BINDINGS.csv`, `CORE_BINDINGS.csv`, `DATA_SCHEMAS.csv`, and
`TASK_COVERAGE.csv` are the detailed evidence and consumer index. `NEW` rows
state only an absence observation and future owner; they do not create APIs.

## Scope boundaries

SV5_03 performed no Unity run, compile, tests, build, bake, game-code change,
data change, SV5/19 work, or SV5_04+ execution. `07_FILE_FLOW_V5.md`
governs future package and temporary-file placement.
