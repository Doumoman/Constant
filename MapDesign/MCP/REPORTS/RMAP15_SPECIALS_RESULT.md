# RMAP15_SPECIALS Result

TASK: RMAP15_SPECIALS

STATUS: PASS

## User-facing outcome

The representative seed `1304` now has eight concrete fixed special sites in the 624×416
world: Start, Mooncore Ore, Condensed Coefficient Sap, Deep Star Yeast, Village, Forge,
shared Seal/Boss, and Exit.  The plan publishes every owned 1×1 cell as typed `S`/`A`/`O`,
with a stable site/slot identity, its actual patch owner, all open port cells, and a terrain
consumer decision that rejects both fixed collision and protected-air intrusion.  It is input
for later general terrain and Bake work; it does not claim a completed external route,
Tilemap/Collider bake, Player traversal, economy, or boss encounter.

## Preconditions and normal Apply

- Supplied inbox SHA-256 matched the raw input before mutation:
  `96bb700defb9e494563850004455983022b793b9fe21948294403fb9b99afa80`.
- RMAP14 was verified as finalized and committed in ancestor commit
  `f879625d39d08b6d1259ecd5cdab73bbdcb7ab42`: RMAP14 is `COMPLETE`, Current Task was
  `NONE`, and its installed Task/archive are byte-identical at
  `9d99d0241eeb904ce0cd523c53349e290707cb9eca242e78719a0905ccc42f0b`.
- RMAP14's PASS Result hash is
  `38b22e7d5414aef3eb56a9b97cd7d44af9ea9af0fa488c3273f577e7a84b8c48`, exactly matching
  the RMAP15 metadata prerequisite.
- Normal `single_task_v1` Apply installed and archived this Task at the supplied SHA, changed
  only RMAP15 `LOCKED → CURRENT` and Current Task `NONE → RMAP15_SPECIALS`; the RMAP subset
  then became `14 COMPLETE / 1 CURRENT / 4 LOCKED`.

## Responsibility and adaptation

| Path | Decision | RMAP15 responsibility |
|---|---|---|
| `Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/RmapSpecialReservationPlanner.cs` | NEW | Deterministic RMAP14/RMAP13-consuming physical site selection, 1×1 S/A/O fixed shell, stable IDs, access/slot/state geometry export, and protected-cell consumer gate. |
| `RmapWorldBiomePlanner.cs` | KEEP | Supplies the actual 16 patches/ownership and the verified candidate patch sets; biome ownership is not modified. |
| `RmapWorldGraphPlanner.cs` | KEEP | Supplies all eight logical reservations and progression conditions; Seal and Boss remain distinct bindings. |
| `RmapWorldDataContract.cs` | KEEP | Supplies the existing RMAP12 `SpecialReservation` stream and stable-ID implementation. |
| `SpecialRegionFixedSlotLayers.cs`, `SpecialRegionSiteBridge.cs`, and existing SpecialRegions contracts | KEEP | Their fixed-shell, slot, and collision-protection semantics were retained. The RMAP15 planner is the missing 624×416 full-world adapter and exposes the focused future-consumer gate without changing legacy site data. |
| `Assets/_Game/Tests/EditMode/Map/RMAP15/RmapSpecialReservationPlannerTests.cs` | NEW | Focused RMAP15 proof, generated evidence, and visual exports from one public planning path. |

## W04 evidence

- `rmap15_sites.csv` has **8** physical sites and `rmap15_graph_bindings.csv` has all **8**
  RMAP13 reservations. Village has a distinct site/stable identity and does not consume a
  logical reservation. Seal and Boss bind separately to different local/world points in the
  one shared SealBoss site.
- `rmap15_fixed_cells.csv` has **2,432** owned fixed cells, each with local/world coordinate,
  RMAP14 patch ID, typed `S`/`A`/`O` base, protection class, and collision meaning.
  `rmap15_ownership.csv` separates FixedSolid from ProtectedAir per site; every general-terrain
  candidate is evaluated through `RmapSpecialReservationPlan.EvaluateTerrainCells`.
- `rmap15_access.csv` has **45** explicit open port cells (three per enabled port), direction,
  IN/OUT/BOTH flow, requirement, and graph node source. `rmap15_slots.csv` has **10** stable
  spawn/resource/NPC/shop/forge/seal/boss/exit slots with a concrete support cell and headroom.
- `rmap15_state_geometry.csv` has the shared SealBoss site’s three sealed SOLID gate cells and
  the same three OPEN AIR cells. This is state geometry input only; no door or encounter runtime
  controller is introduced.

## Placement and determinism

- RMAP15 consumes RMAP14’s real 16-patch plan instead of reducing it to four patches. Regular
  graph roles use their four eligible same-biome patches; Village has its explicit MoonCrater
  draft policy; the shared SealBoss site uses the union of its Seal/Boss candidates and is
  selected across an actual RMAP14 Dough/Crater patch boundary.
- Every footprint is checked cell-by-cell against its eligible patch set, world bounds, and
  already reserved cells. The planner uses the existing RMAP12 `SpecialReservation` stream,
  sorted candidates, and an explicit `32`-attempt cap. Site exports preserve attempt diagnostics,
  selected template/version/transform, occupied patch IDs, and origins.
- Seed `1304`, `CONTENT_V1`, and `GENERATOR_V1` produces plan digest
  `09d0feeecb89f1c6d25f9559301bf9f3e9ef09216233084327270f9550840f07` from definition digest
  `51eb1ebbb4760836260f3be27a8f1e7319cc424aec4cd196eb34004c6a882355` and RMAP14 digest
  `8b0fe1c4bdea5afdd0160681dacc19a4b59b77c13804f29bb4f4be95dce68a86`.

## Generated evidence and visible output

The focused test derives all listed material from the same plan; it does not hand-author a
parallel generated source of truth.

| File | SHA-256 |
|---|---|
| `rmap15_manifest.json` | `51abe03ebdde3c91fef9c77773b7164a79e975e6719558c05b15604e88a30bf7` |
| `rmap15_sites.csv` | `c88d1190130a828a28d6b4de9c863e66d6ee461926243e7088fb4fc3ae8f99aa` |
| `rmap15_fixed_cells.csv` | `d3226621678103b5aa108e4647e9443b5f6b7ac1804f854494db3b2d55fd5dfc` |
| `rmap15_access.csv` | `3be49c114ec044cf4ec44c8f99ad04e03267ecc2424500e4448cf7bb51fabf6f` |
| `rmap15_slots.csv` | `0f83262b35db042522c3fed675ae384d7be0e4374e885d720667bd9b7b787537` |
| `rmap15_ownership.csv` | `eb1401be4b04591e2e7c243dccb4695c4a007073d4c129b063d6bcfc96825255` |
| `rmap15_state_geometry.csv` | `587d15a07246987c38c552bff4d6d8dd38a3502768d2e768af822abddb60c18d` |
| `rmap15_layout.png` | `901da97c2e8473cc9f26ea25ef0b22cea4e6fa399641a351929ebc7480d8ef0a` |

`rmap15_layout.png` is the 624×416 whole-world reservation overlay. Eight `review/*.png`
images render every local cell at 16× scale, with role and world origin, S/A/O colors, green
access cells, and yellow slots. The layout and shared SealBoss review were visually inspected.

## Validation

- Unity `6000.3.8f1`, EditMode filter
  `StarNight.Map.Tests.EditMode.Rmap15.RmapSpecialReservationPlannerTests`: **4/4 PASS**,
  failed/skipped `0/0`; XML `rmap15_editmode_results.xml`, SHA-256
  `a91dd696ac4acb6fdeb7f8b3bc3bb04718095ed29851ed42bba6ce88e1235591`.
- Unity `6000.3.8f1`, direct input/contract filter containing `RmapWorldGraphPlannerTests`,
  `RmapWorldBiomePlannerTests`, and `SpecialRegionFixedSlotPersistenceTests`: **19/19 PASS**,
  failed/skipped `0/0`; XML `rmap15_input_contract_results.xml`, SHA-256
  `5ffec6a50a3c2f83f773055e0f7c80ceeadfdbbcbc66384425f1fb01caf574b1`.
- No broad/unfiltered regression, Player/Camera proof, full-world Tilemap/Collider bake, gameplay
  content implementation, build, push, or RMAP16/RMAP17 execution was performed.

## RMAP16/17 handoff and final evidence

- RMAP16 must submit candidate cells to `EvaluateTerrainCells` and leave all FixedSolid,
  ProtectedAir, ports, and slots intact; RMAP17 must consume the same fixed cells as Bake input.
  External inter-site traversal space remains a later RMAP16 obligation, and full Tilemap/Collider
  realization remains a later RMAP17 obligation.
- Installed Task and Archive are byte-identical at
  `96bb700defb9e494563850004455983022b793b9fe21948294403fb9b99afa80`.
- This Result is intentionally written before Status Finalize and the task-scoped atomic commit.
  Its own hash is recorded by the final CLI handoff rather than self-referencing here.

COMMIT: Pending required Status Finalize and RMAP15-only atomic commit.

NEXT: RMAP16_CLUSTERS remains LOCKED / NOT STARTED.
