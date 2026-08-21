# RUN MAP04_05

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP04_05_IMPLEMENT_MULTI_SEED_BIOME_GROWER.md`, MAP04_04 PASS Result를 순서대로 읽어라.

Task의 exact READ/WRITE ALLOWLIST를 준수해 기존 approved Runtime/Test `Generation` 폴더에 production C# 8개, `MultiSeedBiomeGrowerTests.cs` 1개와 matching meta 9개만 추가하라. existing production/tests/meta/asmdef/CSV/Scene/Prefab를 수정하지 마.

MAP04_04 success Result와 continued same-attempt `RNG_BIOME_PATCH` stream을 입력으로 받고 `DrawCount == 13`을 starter에서 증명하라. noise 전 rule max·59·world-share capacity gate를 실행해 attempt-0 target/capacity `165/161`, shortfall `4`, RNG `13->13`, `RetryRequired`를 exact 증명하라. viable input에서만 PatchId ordinal×target sector index 순서로 `NextInt(1001)` noise를 전부 고정하고, under-minimum patch를 우선하는 stable frontier의 exact checked-integer cost와 `(cost, PatchId, SectorIndex)` tie-break로 claim하라. Core/Satellite patch 연결성, rule min/max, biome share cap, source 불변성을 지키고 성공 시 `165 assigned / 4 reserved-unassigned / IsComplete false`를 atomic publish하라.

actual focused `>=160`, SatelliteSeedPlacer `141/141`, CorePatchGrower `127/127`, BiomePatchModels `107/107`, DeterministicRngStream `103/103`, actual required total `>=638`, failed/skipped `0/0`을 실행하라. targeted/full은 각각 discovery arithmetic/resource `>=4505 / >=4574`로만 확인하고 실행 PASS로 표기하지 마. compile/Console/relevant warning `0/0/0`, Authoring CSV/meta `50/50`, final Assets meta `3110`, duplicate GUID `0`, exact Assets changes `18`, existing Assets modification `0`을 확인하라.

Core/Satellite seed 재선정, Intrusion, cleanup, checkerboard/neck 후처리, generated CSV/export, final validator, overlay, `PASS_BIOME` adapter/root/retry loop를 구현하지 마. 전부 PASS일 때 MAP04_05 COMPLETE/Current Task NONE으로만 finalize하고 `MAP04_06_IMPLEMENT_INTRUSION_PLACEMENT`는 LOCKED로 유지하라.
