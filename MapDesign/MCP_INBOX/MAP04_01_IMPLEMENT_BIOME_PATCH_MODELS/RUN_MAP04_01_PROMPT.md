# RUN MAP04_01

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP04_01_IMPLEMENT_BIOME_PATCH_MODELS.md`, MAP03_11 PASS Result를 순서대로 읽어라.

Task의 exact READ/WRITE ALLOWLIST를 준수해 기존 approved Runtime/Test `Generation` 폴더에 production C# 7개, `BiomePatchModelsTests.cs` 1개와 matching meta 8개만 추가하라. existing production/tests/meta/asmdef/CSV/Scene/Prefab를 수정하지 마.

typed `BiomePatchId`, exact Core/Satellite/Intrusion token, patch seed, exact 169-sector immutable Primary/SecondaryBiome·Patch ownership, Core site binding, patch aggregate와 partial/complete `BiomePatchSnapshot`을 구현하라. source/nested collections를 방어 복사하고 ordinal ordering, index-coordinate identity, patch↔sector↔seed↔site binding cross-consistency를 검증하라.

actual focused cases 최소 72개, SiteReservationModels `81/81`, SiteReservationValidator `268/268`, BiomeBoundary `38/38`, StaticRegistry `53/53`, GeneratedWorldData `56/56`, Game.Map targeted `>=3921`, full EditMode `>=3989`, failed/skipped `0/0`을 실행하라. compile/Console `0/0`, Authoring CSV/meta `50/50`, final Assets meta `3079`, duplicate GUID `0`, exact Assets changes `16`, existing Assets modification `0`을 확인하라.

Core seed initializer, growth/cost/RNG, Satellite/Intrusion 배치, cleanup, `PASS_BIOME`, generated CSV, validator, overlay를 구현하지 마. 전부 PASS일 때 MAP04_01 COMPLETE/Current Task NONE으로만 finalize하고 `MAP04_02_INITIALIZE_CORE_PATCH_SEEDS`는 LOCKED로 유지하라.
