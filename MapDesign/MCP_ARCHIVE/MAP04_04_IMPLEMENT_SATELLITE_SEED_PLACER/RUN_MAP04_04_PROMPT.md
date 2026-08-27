# RUN MAP04_04

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP04_04_IMPLEMENT_SATELLITE_SEED_PLACER.md`, MAP04_03 PASS Result를 순서대로 읽어라.

Task의 exact READ/WRITE ALLOWLIST를 준수해 기존 approved Runtime/Test `Generation` 폴더에 production C# 7개, `SatelliteSeedPlacerTests.cs` 1개와 matching meta 8개만 추가하라. existing production/tests/meta/asmdef/CSV/Scene/Prefab를 수정하지 마.

MAP04_03 publication과 fresh `RNG_BIOME_PATCH` stream을 입력으로 rule ID ordinal `CRATER/DOUGH/MILL/ROOT` count를 모두 먼저 inclusive draw하라. 이후 unassigned·unreserved candidate에서 same-biome Core 전체 sector와 prior same-biome Satellite seed까지 Manhattan distance `>=3`, edge 허용 규칙을 지켜 seed를 배치하라. individual seed만 최대 100회 redraw하며 exhaustion은 atomic retry-required다.

actual focused `>=136`, CorePatchGrower `127/127`, CorePatchSeedInitializer `121/121`, BiomePatchModels `107/107`, DeterministicRngStream `103/103`, actual required total `>=594`, failed/skipped `0/0`을 실행하라. targeted/full은 각각 discovery `>=4340 / >=4409`로 확인하되 실행 PASS로 표기하지 마. compile/Console/relevant warning `0/0/0`, Authoring CSV/meta `50/50`, final Assets meta `3101`, duplicate GUID `0`, exact Assets changes `16`, existing Assets modification `0`을 확인하라.

Core 수정, Satellite minimum-size/full-map 성장, Intrusion, altitude/noise/perimeter ownership cost, cleanup, `PASS_BIOME`, generated CSV, validator, overlay를 구현하지 마. 전부 PASS일 때 MAP04_04 COMPLETE/Current Task NONE으로만 finalize하고 `MAP04_05_IMPLEMENT_MULTI_SEED_BIOME_GROWER`는 LOCKED로 유지하라.
