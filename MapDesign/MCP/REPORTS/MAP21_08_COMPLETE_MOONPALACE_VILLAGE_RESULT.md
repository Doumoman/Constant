TASK: MAP21_08_COMPLETE_MOONPALACE_VILLAGE
STATUS: PASS

## User-Facing Implementation Report

MoonPalace Village를 gameplay가 아닌 production 정적 데이터로 완성했다. 세 layout은 `1x1` 48x32, `2x1` 96x32, `1x2` 48x64 region-local bounds를 사용하고 각각 48, 96, 64개의 cardinally continuous central-road cell과 5, 6, 5개의 facility를 가진다. `2x1`은 x=47/48, `1x2`는 y=31/32에서 정확한 cardinal seam pair를 보존한다.

각 layout에는 fixed `Kitchen` 1개와 fixed `Repair` 1개가 있고, `OptionalRest`, `OptionalStorage`, `OptionalMarket`를 공통 제공한다. `2x1`에만 `OptionalLore`를 더해 optional facility가 4개가 된다. 모든 facility는 고유 door marker, forward access witness, road-return witness를 하나씩 가진다.

layout마다 NPC marker 3개, inventory marker 2개가 있으며 NPC marker 중 정확히 하나가 `Shopkeeper`다. Shopkeeper는 facility binding과 위치를 설명하는 static payload일 뿐 controller, inventory, price, purchase, dialogue, repair, crafting 또는 save data를 만들지 않는다.

`Normal`, `Friendly`, `IndividualHostile`, `AllHostile`, `Evacuation`은 marker snapshot 5종이다. `IndividualHostile`은 layout마다 NPC 하나만 Hostile로 지정하고, 다른 variant도 road/facility/door coordinate, access witness, collision 또는 persistence를 변경하지 않는다.

MAP13_04/05/09의 Village shell/state 계약과 MAP18_06 special export surface는 file identity와 승인 상태를 읽기 전용으로 확인했다. MAP21_04/05/06/07 manifest도 digest chain만 읽었으며 rewrite/regeneration하지 않았다. NPC spawn/AI/combat, shop transaction, door runtime, save/load, world placement, renderer, validation runner, replay, rollback, Tilemap/Scene/Prefab/Collider/Addressables/runtime object는 구현하거나 실행하지 않았다.

legacy regression과 prior-category test는 선택하지 않았고, 유일한 실행은 EditMode category `MAP21_08`였다. MAP21_09는 이번 PASS handoff digest만 게시했으며 Status row는 계속 `LOCKED`다.

## Responsibility and Added Scripts

| Script or file | Added or changed responsibility | Explicit non-ownership |
|---|---|---|
| `MoonPalaceVillageProduction.cs` | layout/facility/road/door/marker/state record, invariant 검증, 결정적 CSV/JSON, forbidden-operation zero counters | MonoBehaviour, runtime state, NPC/shop/door/save/world mutation |
| `MoonPalaceVillagePublisher.cs` | MAP13/MAP18 Result TASK/PASS 확인과 observed SHA 기록, MAP21 strict digest 확인, MAP21_08 전용 정적 파일 게시 | upstream rewrite, generation, renderer, validation, replay, rollback |
| `MoonPalaceVillageProductionTests.cs` | MAP21_08 계약과 결정성·write boundary·runtime non-ownership을 검증하는 15개 EditMode test | PlayMode, prior category, legacy/full regression |
| `MAP21_08/moonpalace_village_profiles.csv` | 세 layout profile과 bounds/count/child digest | world/sector placement |
| `MAP21_08/moonpalace_village_facilities.csv` | fixed/optional facility 16개와 region-local anchor | facility gameplay logic |
| `MAP21_08/moonpalace_village_roads.csv` | central road cell 208개와 no-tool access class | Tilemap/pathfinding mutation |
| `MAP21_08/moonpalace_village_doors.csv` | door 16개와 forward/road-return witness | collider, lock, open/close, path blocking |
| `MAP21_08/moonpalace_village_markers.csv` | NPC 9, inventory 6, shopkeeper subset 3의 static payload | spawn, AI, inventory/price data |
| `MAP21_08/moonpalace_village_state_variants.csv` | layout별 marker snapshot 5종 | runtime hostile/evacuation state machine |
| `MapDesign/MCP/GENERATED/MAP21_08/*.json` | Village/access/state/digest 정적 manifest와 MAP21_09 handoff | runtime save 또는 generated world |

## Repair Note

MAP21_08 initially blocked on a MAP13_04 source Result byte SHA mismatch.
The repair changed historical MAP13/MAP18 source Result checks from exact byte SHA gates to read-only TASK/STATUS/PASS checks with observed SHA recording.
MAP21_07 remains the exact immediate predecessor gate.
No MAP13/MAP18/MAP21_04/05/06/07 source artifact was rewritten or regenerated.

Repaired installed Task and archive SHA-256:

```text
e3b149d0ddfac0138688e7284aad0afb8a8118abcb056f52f2dd91bf6e863e7b
```

Repair archive SHA-256:

```text
568a9226ae5b06b5ecef3abd8c53be454a434c8988391dbe145c59cd4dd959fe
```

## Village Layout and Facility Summary

| Layout ID | Shape | Bounds | Active sectors | Road cells | Facilities | Fixed | Optional |
|---|---:|---:|---:|---:|---:|---:|---:|
| `VLG_MOONPALACE_1X1_OVERVIEW` | 1x1 | 48x32 | 1 | 48 | 5 | 2 | 3 |
| `VLG_MOONPALACE_2X1_MARKET` | 2x1 | 96x32 | 2 | 96 | 6 | 2 | 4 |
| `VLG_MOONPALACE_1X2_ASCENT` | 1x2 | 48x64 | 2 | 64 | 5 | 2 | 3 |

```text
Village layout profile records: 3
facility records: 16
fixed facilities: Kitchen 3 / Repair 3
optional facilities: 10
facility count per layout: 5 / 6 / 5
duplicate layout/facility records: 0 / 0
region-local coordinate violations: 0
```

## Road Door and Access Witness Summary

```text
road cells: 48 / 96 / 64
total road cells: 208
2x1 seam evidence: x=47/48 cardinal pair 1
1x2 seam evidence: y=31/32 cardinal pair 1
door markers: 16
forward access witnesses: 16
road-return witnesses: 16
door collision/lock/path-block writes: 0
duplicate road/door records: 0 / 0
```

## State Variant and Marker Summary

```text
NPC markers: 9
inventory markers: 6
shopkeeper markers: 3
state variant records: 15
five variants per layout: 5 / 5 / 5
IndividualHostile variant count / exact target count: 3 / 3
AllHostile variant count: 3
Evacuation variant count: 3
road/facility/door/access mutation count across variants: 0
duplicate marker/state records: 0 / 0
Village progression blocker count: 0
Village required reward dependency count: 0
CoreResource/Forge/Boss dependency count: 0
```

## Static Safety and Non-Execution Summary

```text
NPC spawn/AI/combat executions: 0 / 0 / 0
shop inventory/price/purchase writes: 0
door collision/lock/path-block writes: 0
hostile/evacuation runtime executions: 0
save file writes/reads: 0 / 0
PlayerPrefs writes/reads: 0 / 0
world/sector placements: 0
generated world count: 0
Tilemap writes: 0
runtime GameObject spawns: 0
Scene/Prefab changes: 0
Collider/Addressables changes: 0
MAP13 source modifications: 0
MAP18 source modifications: 0
MAP21_04/05/06/07 source modifications: 0
CSV authoring writes outside MAP21_08 authoring folder: 0
```

## Snapshot and Digest Summary

Historical source Result verification used exact file/TASK/PASS checks and recorded these observed byte hashes:

```text
MAP13_04_RESULT: f5419f1218885ebe89a24d8106a481df93da80d9c25821f4398748f2ab96ab26
MAP13_05_RESULT: 005ad4993c1db449b6199f1e0d2842d10465b8f48b2eec78a37542c5038a9fa6
MAP13_09_RESULT: 637fec406f42bf845be5ae9313a036b3ec49f66467539a3552c1f94ad68bd5e2
MAP18_06_RESULT: ad2b88be043cb7e18289909a7ad44d76c9143a65e5228c11c24f1a60b86831fd
```

Immediate predecessor strict gates remained unchanged and passed:

```text
MAP21_07 Result SHA-256: 68170bda01e3f2df4fbf14e9e841b04be58b3b61b1b18de9af3e5e2a3a486c8b
MAP21_07 installed Task SHA-256: 4bee068adbf40ee2d55afaea0b35d6e3d635079de2ee6f23f9e0636b0fbf4321
MAP21_08 handoff digest: 7cd8adb0e27cffad1a4187400c6e72cd47e24291663aa5fe69457096efb535e0
```

CSV SHA-256:

```text
moonpalace_village_profiles.csv: efafda8f9c8f14b7ed79c4b6f246f2da61946212980942056e25821fb3b76b36
moonpalace_village_facilities.csv: a9bb2a3f67a1ee0bca4462cafdc7f67ab6c0246306093d51c9dd48af5b52310b
moonpalace_village_roads.csv: 39059952125b1f2bd3808fd18654f09ecefe733559c44e2396ade951b494d66b
moonpalace_village_doors.csv: 412981003647fa0b0b5385579518bccb14a985d83c66a36735a1065c5b306974
moonpalace_village_markers.csv: eefab202de7827467191892bb13022254196f099f6794b8abf91506b7b07c345
moonpalace_village_state_variants.csv: 02e98e0e348646821f6b36e40bcea317979b25300fd4b29f8e43268624a4d50d
```

JSON SHA-256:

```text
moonpalace_village_manifest.json: 1af40985aa34173d4f4c000227c34e4b4e8c6e69c227b713e3fe11cce452ad66
moonpalace_village_access_manifest.json: 3f77a72c9f1610d3126a30d60edfb5871de104647c725cda76ffacdccee62016
moonpalace_village_state_manifest.json: 7c35a8861cbbb2d35e9054d078853353eef74d3665af4ecb2adf3b0c81e62b65
moonpalace_village_digest_manifest.json: 1a137eeed1c59c6f1afd6801c590639a7c556a4982e3ca658a7b5a64d4d2726b
digest manifest canonical digest: 9308092afbee03a8c08d5a5fbc2b4eb4a02fc747e5bbc6bd9afe7ce5ec018a73
MAP21_09 handoff digest: 36e124fe4f63258eeca5b5097d45a71e80587efaa1fbb50fb6eeee7e36ef224b
```

All six CSV and four JSON files are UTF-8 without BOM, use LF only, and end in exactly one LF. Runtime, publisher, and focused test source SHA-256 values are respectively `f5d43a3a568359a25471088286397e61d362131c1703983918c01acc649d14f4`, `1b6301b822b9b185ece25721bff8a5878c8cea974025071d9a0bf7dffa10a1a1`, and `aa5e04c0b92faf02bae4b93a5605e428f4fe31f312589c232b65e7861d02a39e`.

## Focused Validation Summary

```text
Unity Version: 6000.3.8f1
Compile Errors: 0
Relevant MoonPalaceVillage Warnings: 0
Mode: EditMode
Category: MAP21_08
Job ID: 1a3f0fcbf94a4a66baf84b9dac730f5f
Discovered: 15
Executed: 15
Passed: 15
Failed: 0
Skipped: 0
Inconclusive: 0
Duration: 2.3692147 seconds
PlayMode Tests: NOT RUN
Scene/Prefab Changes: NONE
```

Unity Console also retained an MCP transport disposed-stream diagnostic from reconnection and a Test Framework cleanup-verification diagnostic because the focused publisher test intentionally created the permanent task-owned authoring files. Neither was a C# compiler diagnostic nor a failed/skipped/inconclusive focused test; the test job result was `Passed` 15/15.

## No Legacy Regression Boundary Notes

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
FULL REGRESSION RUNS: 0
MAP13 CATEGORY RERUNS: 0
MAP18 CATEGORY RERUNS: 0
MAP19_09 SCALE AUDIT RERUNS: 0
MAP20_01 GENERATOR RUN RERUNS: 0
MAP20_02 OVERLAY SAMPLE REGENERATION RUNS: 0
MAP20_03 DETAIL SAMPLE REGENERATION RUNS: 0
MAP20_04 NAVIGATION SAMPLE REGENERATION RUNS: 0
MAP20_05 REPLAY EXPORT SAMPLE REGENERATION RUNS: 0
MAP20_06 EXIT AUDIT REGENERATION RUNS: 0
MAP21_01 PROFILE TILE SHELL REGENERATION RUNS: 0
MAP21_02 MICROPATTERN REGENERATION RUNS: 0
MAP21_03 CRATER ROOT CLUSTER REGENERATION RUNS: 0
MAP21_04 MILL DOUGH CLUSTER REGENERATION RUNS: 0
MAP21_05 ACTIVITY EVENT REGENERATION RUNS: 0
MAP21_06 BOUNDARY REGENERATION RUNS: 0
MAP21_07 CORE RESOURCE REGENERATION RUNS: 0
VALIDATION RUNNER EXECUTIONS: 0
REPLAY EXECUTIONS: 0
GENERATOR EXECUTIONS: 0
RENDERER EXECUTIONS: 0
ROLLBACK EXECUTIONS: 0
CSV AUTHORING WRITES OUTSIDE MAP21_08 AUTHORING FOLDER: 0
RUNTIME OBJECT SPAWNS: 0
SCENE PREFAB CHANGES: 0
SAVE FILE WRITES/READS: 0 / 0
NPC SPAWNS: 0
SHOP TRANSACTIONS: 0
DOOR COLLISION OR LOCK WRITES: 0
```

## Final Status Evidence

At Result write time the local protocol still has MAP21_08 as `CURRENT` and MAP21_09 as `LOCKED`. This Result is the matching exact `STATUS: PASS` evidence required before Status Finalize. Status Finalize and atomic commit are intentionally pending until this Result is re-read and hashed.

```text
Result: PASS
Current Task before finalize: MAP21_08_COMPLETE_MOONPALACE_VILLAGE
MAP21_08 row before finalize: CURRENT
MAP21_09 row: LOCKED
Status Finalize performed at Result write time: NO
Atomic commit performed at Result write time: NO
Git push performed: NO
MAP21_09 started: NO
```
