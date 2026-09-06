TASK: MAP21_05_PRODUCTIONIZE_ACTIVITIES_AND_EVENT_OVERLAYS
STATUS: PASS

## User-Facing Implementation Report

MAP12에서 승인된 Activity 7종과 EventOverlay 5종을 MoonPalace production 정적
authoring 데이터로 승격했다. Activity ID와 MAP12 weight/strength/slot 의미를 보존하면서
MAP21_04의 48-cluster manifest에 존재하는 same-biome Terrain primary/fallback cluster만
연결했다. MAP12 원본 52 slot에 Activity별 SafePocket과 ExitPreservation 증거를 1개씩
추가하여 총 66개의 production slot record를 발행했다.

non-empty EventOverlay 4종은 payload identity와 marker만 보존하며 실행할 수 없다.
`EVT_EMPTY`는 7개 Activity와 unassigned cluster opportunity 모두에 대한 명시적 fallback이다.
NPC, meteor, reward, Maru, Activity state machine, Event payload를 실행하거나 생성하지 않았다.

MAP12 및 MAP21_01~04 source artifact는 읽기 전용으로 사용했으며 수정 수는 0이다.
생성물은 MAP21_05 전용 6 CSV와 4 JSON뿐이다. world/sector generation, renderer,
validation runner, replay, rollback, Tilemap, Scene/Prefab, Collider, Addressables,
runtime GameObject를 실행하거나 변경하지 않았다. MAP21_06은 시작하지 않았다.

## Responsibility and Added Scripts

| Script or file | Added or changed responsibility | Explicit non-ownership |
|---|---|---|
| `MoonPalaceActivityEventProduction.cs` | exact 7/5 inventory, same-biome Terrain binding, static slot/removal/event compatibility invariant, deterministic CSV/JSON serialization을 소유한다. | Activity/Event runtime 실행, generation, renderer, validation, replay, rollback을 소유하지 않는다. |
| `MoonPalaceActivityEventPublisher.cs` | MAP12 CSV와 MAP21_04 manifest/digest를 읽기 전용으로 검증하고 MAP21_05 전용 6 CSV/4 JSON만 쓴다. | upstream rewrite, world output, runtime object, Scene/Prefab/Tilemap write를 소유하지 않는다. |
| `MoonPalaceActivityEventProductionTests.cs` | 정확히 `MAP21_05` category의 13개 EditMode 정적 계약 test를 소유한다. | PlayMode, prior category, legacy/full regression을 선택하지 않는다. |
| `MAP21_05/*.csv` | Activity profile/binding/slot과 Event profile/marker/compatibility authoring source를 소유한다. | generated world coordinate, runtime state, payload execution 결과를 포함하지 않는다. |
| `GENERATED/MAP21_05/*.json` | 정적 manifest, removal proof, Event marker compatibility, digest/handoff sample을 소유한다. | runtime save, seed approval, generated world를 나타내지 않는다. |

## Activity Production Summary

- activity profile records: 7
- Activity IDs: exact seven MAP12 IDs
- unknown activity IDs: 0
- Activity primary/fallback binding rows: 7
- Activity slot records: 66
- Activity removal safety proofs: 7 / 7
- CassiaRoot Activity profiles: 0

| Activity ID | Biome | Strength | Weight | Primary | Fallback | Slots | Missing optional semantics |
|---|---|---:|---:|---|---|---:|---|
| `ACT_CRATER_BOULDER_CHAIN` | MoonCrater | Strong | 1400 | `TC_CRATER_PROD_ROCK_SHELF_CHAIN` | `TC_CRATER_PROD_FALL_RECOVERY` | 9 | Projectile, Npc |
| `ACT_CRATER_RICOCHET_MINE` | MoonCrater | Strong | 1400 | `TC_CRATER_PROD_BROKEN_SLOPE` | `TC_CRATER_PROD_BOWL_CROSS` | 10 | Npc |
| `ACT_DOUGH_TIME_TRIAL` | MoonDough | Ordinary | 1800 | `TC_DOUGH_PROD_BOUNCE_CUP` | `TC_DOUGH_PROD_SQUISH_CROSS` | 9 | Projectile, Npc |
| `ACT_MARU_REWIND_ANOMALY` | MoonDough | Strong | 600 | `TC_DOUGH_PROD_RECOVERY_PAD_CHAIN` | `TC_DOUGH_PROD_STICKY_SHELF` | 9 | Projectile, Npc |
| `ACT_MILL_ESCORT_CART` | AbandonedMill | Ordinary | 1200 | `TC_MILL_PROD_BEAM_OVERHANG` | `TC_MILL_PROD_RUST_LEDGE` | 10 | Projectile |
| `ACT_MILL_GEAR_GRID` | AbandonedMill | Ordinary | 1200 | `TC_MILL_PROD_GEAR_GALLERY` | `TC_MILL_PROD_BROKEN_PILLAR` | 9 | Projectile, Npc |
| `ACT_MILL_PESTLE_WORKSHOP` | AbandonedMill | Strong | 1000 | `TC_MILL_PROD_ORTHOGONAL_SHAFT` | `TC_MILL_PROD_FALL_RECOVERY` | 10 | Npc |

모든 Activity는 Cue, Trigger/Activation, Device/Core, Reward, SafePocket, Recovery,
Reset, ExitPreservation을 가진다. Projectile과 Npc는 MAP12 원본에서 존재하는 Activity에만
보존했다.

## EventOverlay Production Summary

- event profile records: 5
- Event IDs: `EVT_METEOR_FALL`, `EVT_WANDERING_MERCHANT`,
  `EVT_RARE_CREATURE`, `EVT_MARU_INTERVENTION`, `EVT_EMPTY`
- non-empty event marker records: 4
- empty variant records: 1
- empty marker count / weight / gap: 0 / 0 / 0
- event payload executions: 0

| Event ID | Kind / operation | Weight / gap | Marker compatibility | Execution |
|---|---|---:|---|---|
| `EVT_METEOR_FALL` | State / SetState | 3000 / 4 | Terrain CORE only; Quiet/Buffer excluded | marker-only |
| `EVT_WANDERING_MERCHANT` | Npc / SpawnNpc | 2500 / 6 | `ACT_MILL_ESCORT_CART` Npc only | marker-only |
| `EVT_RARE_CREATURE` | Npc / SpawnNpc | 1500 / 8 | `ACT_MILL_ESCORT_CART` Npc only | marker-only |
| `EVT_MARU_INTERVENTION` | State / SetState | 1000 / 10 | `ACT_MARU_REWIND_ANOMALY` Device only | marker-only |
| `EVT_EMPTY` | Empty / none | 0 / 0 | all 7 Activities + unassigned clusters | no marker/payload |

## Cluster Binding and Compatibility Summary

- MAP21_04 cluster descriptors read: 48
- known Activity cluster references: 14 / 14
- unknown cluster IDs: 0
- wrong-biome cluster bindings: 0
- Quiet/Buffer Activity bindings: 0
- Activity/Event compatibility rows: 12
- Meteor Terrain CORE rows: 1
- Merchant/RareCreature exact Npc rows: 2
- Maru exact Device rows: 1
- Empty fallback rows: 8 (7 Activity + 1 unassigned cluster scope)

## Static Safety and Non-Execution Summary

removal proof 7개 모두 static shell before/after digest와 critical target before/after
digest가 동일하다. 각 proof는 SafePocket 1개 이상, Recovery 1개 이상,
Reset/Exit preservation을 증명한다.

- static tile deltas: 0
- runtime mutations: 0
- Activity state machine executions: 0
- event payload executions: 0
- generated world count: 0
- Tilemap writes: 0
- runtime GameObject spawns: 0
- Scene/Prefab changes: 0
- Collider changes: 0
- Addressables changes: 0
- MAP12 source modifications: 0
- MAP21_03/04 cluster artifact modifications: 0

## Snapshot and Digest Summary

Source chain:

- MAP12 aggregate: `46330eb01dd302bf80dab6eacf88dea59f107cbecc9225b2243a395c1d0dbc8b`
- MAP12 Activity catalog: `3ef83fae74d935a2469ab587414d0498cb423609b171d1c7633423e297318c3a`
- MAP12 Event catalog: `2d2878f62605927a7b70a405a06079b3ebad7767e3bd7db9b6b2431177ea95a0`
- MAP21_04 all-biome cluster manifest: `d30baf04fcd6eceeabd7b6f467b24ce11ce7c5f44833887bf7b0457e6fbdda72`
- MAP21_04 digest manifest: `0a4ffb22812a37dbebc0b3e3f61d01888986c99a78088b6cd86ed61db2969aab`

| Published file | SHA-256 |
|---|---|
| `moonpalace_activity_cluster_bindings.csv` | `683439da2e2581003ecb4a6f51919585b5884282431230d848ba0838a85558bf` |
| `moonpalace_activity_event_compatibility.csv` | `27c1a1b2f5941780dd08252928ed299343fdb0061cfd3b4115974b737cc167b3` |
| `moonpalace_activity_profiles.csv` | `a360826de2002a999aff20297689fbd52ba17bc63a5501eeea5007a461110639` |
| `moonpalace_activity_slots.csv` | `27bd0795c80f6965dd85781569fd1362fbd95ce03736b6708d958984a047c03f` |
| `moonpalace_event_overlay_markers.csv` | `564b67b977fd840d629e2d36b561ffb3b4a2f772ecf8a9b6f0c6cb7851ed3faa` |
| `moonpalace_event_overlay_profiles.csv` | `ef7707c85a8f9b8eac8e909d596a4728f8fe0ad9b7f336531ddfa97830fdbfba` |
| `moonpalace_activity_event_manifest.json` | `9c0614b4006eb9d53575840f88ce806be8b2e5bc68106e9820334b9b0906b5ea` |
| `moonpalace_activity_removal_safety_manifest.json` | `de02488767202ad7d834fd24a9512b40cc1349ade86cd28d4acb276fca59d734` |
| `moonpalace_event_overlay_manifest.json` | `91734b671b5802f6e28fa636387409f2976d3f390f2b187224f24a5ce9177e5b` |

- digest manifest canonical digest: `645f95b8c58757179223b8e5b219b60b5a732cb898a69f99594178c76f166a1e`
- MAP21_06 handoff digest: `854cf1a08510898a1570676ce4ea30f1c0b3b3883c7694be49092f1fae88733b`
- CSV encoding: UTF-8 without BOM, LF, one final LF
- deterministic reverse-order/repeat/culture validation: PASS
- created_utc excluded from canonical digest: YES

## Focused Validation Summary

- Unity version: 6000.3.8f1
- compile errors in MAP21_05 files: 0
- relevant warnings: 0
- authoritative job: `ad56856ed1e54db7950baf7b4423cd0c`
- selection: EditMode category `MAP21_05` only
- discovered: 13
- executed: 13
- passed: 13
- failed: 0
- skipped: 0
- inconclusive: 0
- duration: 3.1713696 seconds
- PlayMode tests: not selected

최초 category 요청은 새 script asset import 전 상태여서 discovered/executed 0이었고 test
body는 실행되지 않았다. 전체 AssetDatabase refresh로 세 MAP21_05 script를 import한 뒤,
동일한 `MAP21_05` category만 다시 실행한 최종 authoritative 결과가 위의 13/13이다.
범위가 넓은 test selection은 수행하지 않았다.

## No Legacy Regression Boundary Notes

- REGRESSION TRIGGER DETECTED: NO
- PRIOR TASK TEST SELECTIONS: 0
- LEGACY 19347 SELECTIONS: 0
- PLAYMODE SELECTIONS: 0
- UNFILTERED TEST SELECTIONS: 0
- FULL REGRESSION RUNS: 0
- MAP19_09 SCALE AUDIT RERUNS: 0
- MAP20_01 GENERATOR RUN RERUNS: 0
- MAP20_02 OVERLAY SAMPLE REGENERATION RUNS: 0
- MAP20_03 DETAIL SAMPLE REGENERATION RUNS: 0
- MAP20_04 NAVIGATION SAMPLE REGENERATION RUNS: 0
- MAP20_05 REPLAY EXPORT SAMPLE REGENERATION RUNS: 0
- MAP20_06 EXIT AUDIT REGENERATION RUNS: 0
- MAP21_01 PROFILE TILE SHELL REGENERATION RUNS: 0
- MAP21_02 MICROPATTERN REGENERATION RUNS: 0
- MAP21_03 CRATER ROOT CLUSTER REGENERATION RUNS: 0
- MAP21_04 MILL DOUGH CLUSTER REGENERATION RUNS: 0
- VALIDATION RUNNER EXECUTIONS: 0
- REPLAY EXECUTIONS: 0
- GENERATOR EXECUTIONS: 0
- RENDERER EXECUTIONS: 0
- ROLLBACK EXECUTIONS: 0
- CSV AUTHORING WRITES OUTSIDE MAP21_05 AUTHORING FOLDER: 0
- RUNTIME OBJECT SPAWNS: 0
- SCENE PREFAB CHANGES: 0

## Final Status Evidence

Before Status Finalize:

- Current Task: `MAP21_05_PRODUCTIONIZE_ACTIVITIES_AND_EVENT_OVERLAYS`
- `MAP21_05_PRODUCTIONIZE_ACTIVITIES_AND_EVENT_OVERLAYS`: CURRENT
- `MAP21_06_EXPAND_ALL_SIX_BOUNDARY_POOLS`: LOCKED
- implementation rows: 216 = 208 COMPLETE / 1 CURRENT / 7 LOCKED
- Result decision: PASS
- MAP21_06 started: NO

Status Finalize may now change only the Current Task field and MAP21_05 status row.
