# SV5 Ordered Execution Sequence

Source: `MCP/INPUTS/SV5/TASKS.csv`, `TASKS.json`, and `SPACE_V5_TASKS.md`.
All 45 IDs are registered once. Only SV5_01 may be opened by this request;
each later Task requires a separate normal inbox patch and stays LOCKED.

| Order | Task ID | Scope |
| ---: | --- | --- |
| 01 | SV5_01_APPROVAL_BASELINE | approved-structure baseline registration |
| 02 | SV5_02_RULES | SPACE v5 rules registration |
| 03 | SV5_03_BINDINGS | existing code/data bindings |
| 04 | SV5_04_CORE_RESERVE | core area and return-route reservation |
| 05 | SV5_05_ROUTE_STATE | progression and shortcut state |
| 06 | SV5_06_SPACE_GRAPH | place/corridor graph |
| 07 | SV5_07_DIVERSITY | nearby terrain-family diversity |
| 08 | SV5_08_INFILL | ordinary rooms/caves/landings |
| 09 | SV5_09_LOOPS | rejoin corridors and shortcuts |
| 10 | SV5_10_SIDEPATH | irregular side paths |
| 11 | SV5_11_HUB_SHELL | six-way hub shell |
| 12 | SV5_12_TREE_GRAB | reachable tree-grab terrain |
| 13 | SV5_13_JUMP_CONTRACT | jump-map data/distance contract |
| 14 | SV5_14_JUMP_SOLID | SOLID-supported jump routes |
| 15 | SV5_15_JUMP_GRAB | Grab edges and landing clearance |
| 16 | SV5_16_JUMP_OUTLINE | local outline variation |
| 17 | SV5_17_JUMP_RECIPES | mixed jump-route recipes |
| 18 | SV5_18_JUMP_CLEARANCE | full-body jump clearance |
| 19 | SV5_19_JUMP_RECOVERY | retry and return routes |
| 20 | SV5_20_JUMP_PLAYER | real Player jump validation |
| 21 | SV5_21_LIBRARY | library terrain |
| 22 | SV5_22_STAIR_DATA | stair contact data |
| 23 | SV5_23_STAIR_MOTOR | stair movement implementation |
| 24 | SV5_24_CAVE | cave terrain |
| 25 | SV5_25_RATDEN | rat-den connection |
| 26 | SV5_26_CANYON | narrowing canyon |
| 27 | SV5_27_HOUSING | asymmetric housing |
| 28 | SV5_28_HALL | high hall |
| 29 | SV5_29_RANCH | ranch/lake relation |
| 30 | SV5_30_LAKE | lake and dry path |
| 31 | SV5_31_FARM | asymmetric farm |
| 32 | SV5_32_RAIL_PATH | rail path |
| 33 | SV5_33_RAIL_RIDE | rail ride |
| 34 | SV5_34_RAIL_CRASH | rail crash state |
| 35 | SV5_35_CAPSULE | Type0 capsule |
| 36 | SV5_36_PRISON | Type0 prison |
| 37 | SV5_37_PINBALL_SHELL | pinball reservation |
| 38 | SV5_38_WORKSHOP | 36×16 workshop reservation |
| 39 | SV5_39_RABBIT_VILLAGE | rabbit village |
| 40 | SV5_40_ELEVATOR | elevator safety |
| 41 | SV5_41_COMPOSE | full-world composition |
| 42 | SV5_42_FINAL_SCAN | protection/fill/connectivity scan |
| 43 | SV5_43_WORLD_BAKE | CSV/Unity/collider bake |
| 44 | SV5_44_WORLD_PLAYER | full progression/player validation |
| 45 | SV5_45_REVIEW_HANDOFF | numbered review handoff |

The source plan's phase, detailed completion contract, and SV4 mapping remain
in the immutable input files. This concise registry does not replace them.
