# MAP07_02 Repair — MicrochunkDefinition Boundary Allowlist

MAP07_02 BLOCKED 원인만 보정하는 repair package다. Apply는 현재 `MAP07_02` Task 문서만 교체하고 Master, Status, Assets, CSV, C#, test, asmdef는 변경하지 않는다.

기준선:

```text
Current Task: MAP07_02_IMPLEMENT_TILE_LAYER_RULES
Current Task SHA-256 before repair: 0b69d8f46654bd2af5e441d603210a1889351cff478b688a23b6b87c697ea9c7
Current Result: MAP07_02_IMPLEMENT_TILE_LAYER_RULES_RESULT.md
Current Result STATUS: BLOCKED
Current Result SHA-256: 8691d0976dd9ab51794c39d076a58625196191ec0195497734883eff9868ef1c
Revised Task SHA-256: 18d7d4c330b7a3614f155914aea8247412f65eb9ba04335ad5ea1dfffd5231f4
State remains: 79 COMPLETE / MAP07_02 CURRENT / 125 LOCKED
```

Repair 범위:

- `MicrochunkTileLayerRules` required API와 `MicrochunkDefinitionTests.cs` obsolete absence assertion의 allowlist 모순만 교정.
- `MicrochunkDefinitionTests.cs`를 exact 1개 추가 write-allow한다.
- 허용 수정은 `Map0702PlusProductionSymbolsAreAbsent`의 obsolete `MicrochunkTileLayerRules` absence case를 MAP07_03+ forbidden symbol로 교체하는 것뿐이다.
- `MicrochunkDefinitionTests` 총 case count `146`은 유지해야 한다.
- tile-layer rule production files, new test file, acceptance gates는 기존 MAP07_02 범위를 유지한다.
- `MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS`는 `LOCKED / DO NOT START` 유지.

Authoring CSV 불변, generated CSV `0`, Scene/Prefab/asmdef/ProjectSettings 변경 금지 조건도 유지한다.
