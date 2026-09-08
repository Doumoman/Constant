# RMAP08 Port Catalog and Physical Lab Result

TASK: RMAP08_PORTS
STATUS: PASS

## User-facing implementation report

RMAP08 adds an explicit, fixed 12x8 chunk-port contract and an isolated Port Lab scene. Type, space state, normal EdgePort, breakable access, adjacent-boundary record, and directed interior link are separate values. The lab shows every declared edge coordinate and validates the open boundary, inactive-solid boundary, Type 2 downward route, and Type 3 upward route with the real Player, Tilemap, and Collider path. It does not start automatic 3x2 composition, protected-space composition, global traversal search, or pool expansion.

## Responsibility and files

| Path | Change | Responsibility / reuse decision |
| --- | --- | --- |
| `Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/RmapPortCatalog.cs` | NEW | Owns fixed 12x8 `SpaceState`, nullable Type 0~4, EdgePort fields, BreakableAccess, explicit adjacency/interior records, CSV export, and validation. It queries the RMAP07 public catalog for a retained `VoidClear` source candidate at offset `4,2`, but never derives ports or requiredness from RMAP07 tags or characteristics. |
| `Assets/_Game/Live/Editor/RMAP08/CharacterLivePortLabSceneBuilder.cs` | NEW | Owns only the RMAP08 scene and builder-owned derived CSV/manifest outputs. It reuses the RMAP02 terrain Tile and `SetTiles` physical Tilemap application pattern, the RMAP02 Player collision configuration, and the RMAP04 `CharacterLiveClimbSurface`. |
| `Assets/_Game/Map/Scenes/MoonPalace/RMAP08/MoonPalacePortLab_RMAP08.unity` | NEW, builder generated | Isolated visual/physical fixture: 11 non-empty Tilemaps and 11 `TilemapCollider2D` components; one saved `RMAP08_Player`. |
| `Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RMAP08/*.csv` | NEW, derived | Builder-produced authoring copies of the port catalog, edge-port, adjacency, interior-link, and breakable-access rows; not independent hand-edited sources. |
| `Assets/_Game/Tests/EditMode/Map/RMAP08/RmapPortCatalogTests.cs` | NEW | Validates A05/A16~A20 data, full coordinates, Type side sets, explicit directionality, profile digest, and CSV stability. |
| `Assets/_Game/Tests/PlayMode/Character/RMAP08/RmapPortLabPlayModeTests.cs` | NEW | Runs the actual Player capsule against saved TilemapCollider2D geometry for boundary passage/blocking and Type 2/3 routes. |
| `MapDesign/MCP/GENERATED/RMAP08/*` | NEW | Deterministic catalog/connection exports, fixture manifest, and focused Unity result XML. |

`RmapPatternCatalog.cs` is KEEP/read-only: it remains the RMAP07 4x4 source geometry, origin, transform, and characteristics owner. `GeneratedTraversalProfileCatalog` is KEEP/read-only and supplies profile digest `12415531bfa37bc8db427f672fefe47c92fee65bf61fccd94887af09669c2d68`. `GeneratedTileMovementGraphBuilder` is KEEP/read-only and remains the later global generated-terrain validation API; this fixed RMAP08 fixture does not claim it has performed a world graph validation.

## Preconditions

- Inbox full-file SHA-256: expected and observed `e2aa3586d09ff87d18144a24c21100dd9168081d764bdb4b8b5ce72efd95cdac`.
- RMAP07 installed Task and archive SHA-256: `b976a386c798d7cd5052f46a0c65e858eaf97ccbd23981874298c1cc58d16d3b`; bytes are identical.
- RMAP07 Result SHA-256: `c25de6ab55df577bc8587ecccdc610b1bef309cfd24fd6f5fb6483a66172c61e`, with its independent RMAP07 task and pass markers.
- Actual predecessor record: `ada890cb630136cfef691e4c3221ee3ec720ea2e` — `RMAP07 implement role patterns and initial candidate pool`.
- RMAP08 installed Task and archive SHA-256: `e2aa3586d09ff87d18144a24c21100dd9168081d764bdb4b8b5ce72efd95cdac`; bytes are identical.
- Apply-state evidence before this Result: Current Task `RMAP08_PORTS`; `228 COMPLETE / 1 CURRENT / 11 LOCKED`; RMAP08 is the one `CURRENT` row and RMAP09 is the one `LOCKED` row.

## Requirement and port evidence

- A05: `Active`, `Secret`, `InactiveSolid`, and `SpecialReserved` are independent `SpaceState` values. Type 0 supplies either one normal entrance or zero normal ports plus a separate `BreakableAccess`; the inactive solid record has no Type and all 96 cells solid.
- A16: Type 1 accepts only LR; Type 2 accepts LD/RD/LRD; Type 3 accepts LU/RU/LRU; Type 4 requires U and D while L/R remain optional.
- A17: `T0_SINGLE_ENTRANCE` has one EdgePort. `T0_BREAKABLE_SECRET` has no normal EdgePort and preserves its right-side solid shell/condition separately. `T4_SPLIT` retains two independent L-side port IDs rather than collapsing them into Type 0.
- A18: each EdgePort retains `Side`, all `OpenCells`, `TraversalKind`, `FlowDirection`, `Required`, and an independent entrance-group ID. L/R values are y=0..7, U/D values are x=0..11; the RMAP07 placement is retained as lower-left `4,2` provenance.
- A19: `ADJ_T1_A_TO_B` preserves R-to-L intersection `{1,2}` and compatible OUT-to-IN flow. The inactive boundary is recorded as a distinct blocked expectation with no target port, and the HANG-to-WALK case remains explicitly profile-context unknown. `INT_T4_LOW_TO_RIGHT` does not imply a link for the separate high L-side port.
- A20: only `INT_T2_LEFT_TO_DOWN` and `INT_T3_LEFT_TO_UP` are required directed records. Their reverse records are absent. Both bind the shared traversal profile digest and state their specific physical condition.

## Validation and Unity-visible output

- No connected Unity Editor was available; Unity 6000.3.8f1 batch execution built the isolated scene and generated evidence.
- `StarNight.Map.Tests.EditMode.Rmap08.RmapPortCatalogTests`: 7/7 passed. Result: `MapDesign/MCP/GENERATED/RMAP08/rmap08_editmode_results.xml`.
- `StarNight.Character.Tests.PlayMode.Rmap08.RmapPortLabPlayModeTests`: 3/3 passed. Result: `MapDesign/MCP/GENERATED/RMAP08/rmap08_playmode_results.xml`.
- The saved scene contains 11 non-empty serialized Tilemaps and 11 `TilemapCollider2D` components. The actual Player crosses the `T1_A.R -> T1_B.L` boundary, is stopped by `INACTIVE_SOLID_WALL`, drops through Type 2's four-cell D opening to the lower Tilemap landing, and climbs out of Type 3 through U using the existing vertical-input surface.
- Broad/full regression, legacy 19347, Player build, automatic RMAP09 composition, pool-500 selection, and world traversal validation were not run because they are outside this Task.

## RMAP09 bindings and scope boundary

RMAP09 can query `RmapPortCatalog.BuildFixture()` and its `RmapPortCatalogSnapshot` for `ChunkId`, space state, nullable type, source candidate/offset, named ports, all edge coordinates, explicit adjacency records, and interior links. The generated CSV copies are derived evidence only. The RMAP07 candidate catalog remains an existing source reference; `RmapComposer.cs` remains proposed and is not created or started here.

## Final evidence and follow-up

- Installed Task and archive remain byte-identical at the specified RMAP08 SHA-256.
- The only current execution record is RMAP08; its matching Result is this document.
- RMAP09_COMPOSER remains LOCKED and is not started.
- Atomic commit state: pending Phase C Finalize and Phase D scoped staging of RMAP08-owned files only.

NEXT: RMAP09_COMPOSER LOCKED / NOT STARTED
