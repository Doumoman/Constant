# MAP17_02 Build Tilemap Layers Bake and Seam Validation Result

TASK: MAP17_02_BUILD_TILEMAP_LAYERS_BAKE_AND_SEAM_VALIDATION
STATUS: PASS

## User-Facing Implementation Report

이번 Task는 MAP17_01의 immutable 1536-cell placement plan을 실제 Unity Tilemap에 쓰지 않고, 7개 logical layer별 1536개 record와 총 10752개 pure-data bake command로 변환한다. 각 record는 integer sector-local coordinate, tile code와 resolved key, occupancy, source owner, protection, claim과 provenance를 보존한다.

logical bake packet은 향후 adapter가 소비할 데이터 계약일 뿐 `Tilemap.SetTile`, `SetTiles`, `SetTilesBlock`, `ClearAllTiles`, `CompressBounds`를 호출하거나 Scene의 Tilemap component를 변경하지 않는다. 입력의 같은 layer/cell 중복·overlap, 누락·gap, out-of-bounds, 허용되지 않은 layer와 stale placement/registry를 atomic failure로 거부한다.

seam validator는 4x4 MicroPattern 경계 688쌍과 12x8 MicroChunk 경계 240쌍을 별도로 열거하고, 그중 4x4-only 448쌍을 구분한다. 연속·material transition·socket/opening·protected route는 승인 분류로 보존하고, solid/air·hazard/protection·provenance의 미승인 불연속과 missing/out-of-bounds neighbor는 repair하지 않고 실패시킨다.

MAP17_03에는 7개 immutable layer buffer, 10752개 logical command, 64개 socket reference, 24개 marker-slot reference, seam report, logical bake/seam canonical digest를 넘긴다. production Tile/Prefab registry가 아직 없다는 MAP17_01의 판단은 유지하며 reference registry를 production asset 승인으로 취급하지 않았다.

회귀 트리거는 발견되지 않았다. 첫 MAP17_02 focused run은 seam에 닿지 않는 SourceOwner record를 변이한 테스트 표적 오류로 9/10이었고, seam 경계 Terrain record를 변이하도록 focused test만 정정한 뒤 같은 category 최종 run이 10/10 PASS했다. prior task, legacy 19347, PlayMode, unfiltered, full regression은 실행하지 않았다.

## Responsibility and Added Scripts

| Script or file | Added or changed responsibility | Explicit non-ownership |
|---|---|---|
| `Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedTilemapLayerBakePlan.cs` | 7개 stable layer ID, layer-cell record, immutable layer buffer, pure-data bake command, request/plan/result/failure와 logical bake digest를 정의한다. | Unity Tilemap object, collider, runtime handle, streaming/save state를 소유하지 않는다. |
| `Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedTilemapLayerBaker.cs` | MAP17_01 placement/registry를 검증하고 7x1536 logical records를 atomic bake plan으로 변환하며 gap/overlap/stale asset을 거부한다. | 입력을 repair하거나 Tilemap/Scene/Prefab/GameObject에 쓰지 않는다. |
| `Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedTilemapSeamValidation.cs` | 4x4/12x8 seam coordinate와 exposure를 deterministic하게 열거·분류하고 forbidden discontinuity 및 seam digest를 제공한다. | 지형을 carve/widen/reroll하거나 seam을 자동 보정하지 않는다. |
| `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/GeneratedTilemapLayerBakerTests.cs` | 정확히 10개의 `MAP17_02` focused test로 layer coverage, asset/provenance handoff, atomic failure, seam count/classification, digest와 mutation boundary를 검증한다. | prior category, PlayMode, legacy/unfiltered/full regression을 선택하지 않는다. |
| matching `.meta` 4개 | 새 production/test C# asset의 Unity GUID를 보존한다. | Scene, Prefab, Tile asset을 생성하지 않는다. |

## Patch Apply and Preconditions

```text
single MCP_INBOX candidate: 1/1
candidate/task/sets_current identity: PASS
MAP17_01 status before apply: COMPLETE
MAP17_02 status before apply: LOCKED
MAP17_03 status before/after execution: LOCKED/LOCKED
Current Task before apply: NONE
unrelated staged files before apply: 0
MAP17_01 Result required/actual SHA-256:
1cb8a5cb86f5499639c64c94c8b5b59a6ad354c0aed88e67404f7acd2ae68776
MAP17_01 installed Task required/actual SHA-256:
33a65e88a0d6df1946a1d3ff835970814536fc6737c94ebb892b8ae04e4526cb
MAP17_02 inbox/installed/archive SHA-256:
78ce28910ba94eb56b8e77ebd93b2adeb91fb5db7c82dec05170e8822f8eb57b
installed/archive byte equality: YES
Phase A status delta: COMPLETE 0 / CURRENT +1 / LOCKED -1
```

## Logical Bake and Seam Evidence

```text
MAP17_01 placement digest reused:
d8dac9d9bf7c25b179cc2b33c6d0cf7b9323abd39de44b6ca2457216e23df334
MAP17_01 world projection digest reused:
5fb394e497fea2fa90e90177891dd5a971e3afa4af449e5be1935061fb6df8bf
source placement cells observed: 1536/1536
source placement layer refs observed: 10752/10752
source tile code registry refs observed/resolved/missing: 12/12/0
source prefab id registry refs observed/resolved/missing: 24/24/0
source socket side signatures preserved: 64/64
source marker slots preserved: 24/24

logical tilemap layer count: 7/7
logical records per layer: 1536/1536 each
logical total layer records: 10752/10752
unique layer-cell keys: 10752/10752
sector cell coverage: 1536/1536
missing/duplicate/out-of-bounds layer records: 0/0/0
forbidden overlap/gap failures detected by probes: 5/5

4x4 MicroPattern seam adjacency pairs: 688/688
12x8 MicroChunk seam adjacency pairs: 240/240
4x4-only seam adjacency pairs: 448/448
approved seam pairs: 928
unapproved seam pairs: 0
missing/out-of-bounds seam neighbor pairs: 0/0
seam failure probes passed: 5/5

logical bake digest lower-hex SHA-256: YES
logical bake digest: 139465f70d40e6b9a3fdd4bb55696c38e89d1856912f4bec2644edb4c6b47602
seam report digest lower-hex SHA-256: YES
seam report digest: d1a1febd5c9c10481817e5e6c027071fe2890bf2ea79c34252c2a7caaedc7fda
repeat/reverse/culture/registry-order digest mismatches: 0/0/0/0
mutation sensitivity probes passed: 2/2

Unity Tilemap component writes: 0
Tilemap.SetTile/SetTiles/SetTilesBlock/ClearAllTiles calls: 0/0/0/0
Tilemap.CompressBounds calls: 0
Tilemap bakes to Scene: 0
collider rebuilds: 0
GameObject/Prefab instantiation: 0/0
Scene/Prefab/Tilemap mutation: 0/0/0
Generated CSV/assets committed: 0
stable spawn ids created: 0
runtime objects spawned: 0
production seed approvals: 0
MAP17_03 started: NO
```

## Focused Verification

```text
Unity version: 6000.3.8f1
mode: EditMode
category_names: [MAP17_02]
final job_id: e4adf08460e54d94afa80459ffc84f6e
discovered: 10
executed: 10
passed: 10
failed: 0
skipped: 0
inconclusive: 0
duration seconds: 47.8373013
compile errors: 0
relevant Console errors after final clear: 0
relevant Console warnings after final clear: 0
Scene/Prefab Changes: NONE

REGRESSION TRIGGER DETECTED: NO
MAP17_02 FOCUSED EDITMODE RUNS: 2 (initial 9/10 + final 10/10)
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
FULL REGRESSION RUNS: 0
```

최초 focused failure는 production invariant 실패가 아니라 mutation-sensitivity test가 seam 비참여 record를 골랐던 테스트 표적 문제였다. 경계 Terrain record로 표적을 좁힌 뒤 동일 10개 test를 재실행해 모두 `Passed`를 확인했다. final compile 뒤 package-level automated-mode warning 1건을 확인했으나 task 코드와 무관했고, 최종 Console clear 후 error/warning은 0개다.

## Static and Write-Boundary Verification

- required focused test names present: 10/10
- production source의 `UnityEngine`, `UnityEditor`, Scene/Prefab/Tilemap call 및 file/directory I/O 의존: 0
- new source/test files와 matching meta는 Task write roots 내부에만 존재
- Scene/Prefab/Tilemap changed files: 0/0/0
- new production/test/Result source의 `git diff --check`: PASS
- installed/archive Task 원문은 기존 EOF blank 1건씩을 보고하지만 byte-for-byte SHA 계약 때문에 그대로 보존
- MAP17_03 status: `LOCKED`, execution: NOT STARTED
- 기존 `Constant.slnx`, TerrainClusters meta 파일, root repair instruction, PRE-MAP17 report는 수정하거나 stage하지 않음
- Git push: 0

MAP17_02는 PASS-only Status Finalize와 atomic commit을 수행할 수 있다. MAP17_03은 자동 시작하지 않는다.
