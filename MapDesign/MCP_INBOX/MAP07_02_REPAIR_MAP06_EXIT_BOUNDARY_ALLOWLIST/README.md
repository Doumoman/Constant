# MAP07_02 Repair — MAP06 Exit Boundary Allowlist

MAP07_02 repair v1.1 재개 결과의 BLOCKED 원인만 교정하는 v1.2 package다. Apply는 현재 `MAP07_02` Task 문서만 교체하며 Master, Status, Assets, CSV, C#, test, asmdef는 변경하지 않는다.

기준선:

```text
Current Task: MAP07_02_IMPLEMENT_TILE_LAYER_RULES
Current Task SHA-256 before repair: 18d7d4c330b7a3614f155914aea8247412f65eb9ba04335ad5ea1dfffd5231f4
Current Result: MAP07_02_IMPLEMENT_TILE_LAYER_RULES_RESULT(1).md
Current Result STATUS: BLOCKED
Current Result SHA-256: 5d51872d14f925bea341cd880755ce87ae4bb2bf23da3da410ac4db3ac681e7c
Revised Task SHA-256: c9cb155bdb0b9f2d047b8305c35f32392d691988f612bc107849d0a9f3292edb
State remains: 79 COMPLETE / MAP07_02 CURRENT / 125 LOCKED
```

Repair 범위:

- 기존 `Map06ExitTests.cs`를 exact 1개 추가 write-allow 한다.
- `Map06ApprovedSourceChainAndPhaseExitRemainExact`의 obsolete `MicrochunkTileLayerRules` absence entry만 `MicrochunkTransformer`로 교체할 수 있다.
- fixture case count, MAP06 aggregate `2746/2746`, MAP06 exit-approved assertion은 변경하지 않는다.
- v1.1의 `MicrochunkDefinitionTests.cs` replacement와 구현 산출물은 유지한다.
- `MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS`는 `LOCKED / DO NOT START`를 유지한다.

Authoring CSV 불변, generated CSV `0`, Scene/Prefab/asmdef/ProjectSettings 변경 금지 조건도 유지한다.
