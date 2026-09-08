# RMAP v4.2 Execution Sequence

## Registered queue

The following is the complete v4.2 order. A row is eligible only after its own
single-MD task has verified the preceding Task and Result SHA-256 values. The
first registration opens RMAP01 only; all other rows remain locked.

| Order | Task ID | Responsibility | Requirement IDs |
| ---: | --- | --- | --- |
| 01 | RMAP01_REBASE | Register plan; inventory current implementation and bindings | G01~G13 |
| 02 | RMAP02_PLAYER | Tilemap/Collider, baseline player movement/jump, continuous camera scene | P01~P03,C01,C02,E01 |
| 03 | RMAP03_GRAB | Ledge grab constraints and moving safe solids | P05~P08,P10 |
| 04 | RMAP04_CLIMB | Ladder/pole, exit jump, one-way platforms | P11~P13,P15 |
| 05 | RMAP05_FALL | Fall damage, stun/death, traversal reset | P09,P14,P16~P18 |
| 06 | RMAP06_LOOK | Tab look, input precedence, integrated movement scene | C03~C05,E02 |
| 07 | RMAP07_PATTERNS | 4×4 cell roles/tags/transforms and 40~60 starters | A01,A06~A12,A15 |
| 08 | RMAP08_PORTS | Type 0~4 ports, coordinates, direction and interior links | A05,A16~A20 |
| 09 | RMAP09_COMPOSER | 12×8 composition, protected space, overlay and bounded reselection | P04,P19,A22~A24 |
| 10 | RMAP10_SMALL_RUN | Seeded variable-size map to real Player exit | E03~E07 |
| 11 | RMAP11_POOL500 | Distinct role-qualified pool of 500 connected to generation | A13,A14 |
| 12 | RMAP12_WORLD_DATA | Seed/version/RNG/ID and base-versus-modified terrain state | W08~W12 |
| 13 | RMAP13_WORLD_GRAPH | Free resource order, forge, seal, boss, exit and shortcuts | W01,W02,W07 |
| 14 | RMAP14_BIOMES | Four biomes, density profiles and boundaries | A25,W05 |
| 15 | RMAP15_SPECIALS | Eight required special reservations, fixed terrain and access | W04 |
| 16 | RMAP16_CLUSTERS | Major terrain, secrets, density/activity ratio and assembly | A02,A21,A26,A27,W06 |
| 17 | RMAP17_WORLD_BAKE | 624×416 Tilemap/Collider bake and Player connection | A04,E08 |
| 18 | RMAP18_WORLD_STATE | Modification save/apply, regeneration and chunk lifecycle | W13,W14 |
| 19 | RMAP19_WORLD_PLAY | Six-resource order, representative real play and final handoff | A03,W03,W15,E09 |

## Gate rules

1. Only one RMAP row may be `CURRENT`.
2. `PASS` Result plus normal Finalize closes the current row; it never opens the
   next row.
3. `FAIL`, `BLOCKED`, missing SHA evidence, or a different installed/archive
   byte sequence stops issuance.
4. RMAP02 is the first actual Player scene; RMAP10 is the small seeded run;
   RMAP17 is the full bake; RMAP19 is the final representative play handoff.
5. Existing MAP/VIS/RUN completion evidence remains historical input and is not
   reclassified as an RMAP pass.

## Historical 29-task crosswalk

| Historical task family | v4.2 requirement sequence |
| --- | --- |
| RMAP00_01, RMAP00_02, RMAP00_03 | 01 |
| RMAP01_01 | 02,12 |
| RMAP01_02 | 02 |
| RMAP01_03 | 03 |
| RMAP01_04 | 04 |
| RMAP01_05 | 05 |
| RMAP01_06, RMAP01_07 | 02,06 |
| RMAP02_01, RMAP02_02, RMAP02_05 | 07 |
| RMAP02_03 | 08 |
| RMAP02_04 | 07,11 |
| RMAP02_06 | 09 |
| RMAP02_07 | 09,10 |
| RMAP03_01 | 02,10 |
| RMAP03_02, RMAP03_03, RMAP03_05 | 10 |
| RMAP03_04 | 10,11,16 |
| RMAP04_01 | 13 |
| RMAP04_02 | 14 |
| RMAP04_03 | 16 |
| RMAP04_04 | 15 |
| RMAP04_05 | 17 |
| RMAP04_06, RMAP04_07 | 19 |

This crosswalk is descriptive. It does not import old task bodies, inherit old
PASS states, or authorize those historical IDs as v4.2 execution work.
