# RUN MAP04_02

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP04_02_INITIALIZE_CORE_PATCH_SEEDS.md`, MAP04_01 PASS Result를 순서대로 읽어라.

Task의 exact READ/WRITE ALLOWLIST를 준수해 기존 approved Runtime/Test `Generation` 폴더에 production C# 6개, `CorePatchSeedInitializerTests.cs` 1개와 matching meta 7개만 추가하라. existing production/tests/meta/asmdef/CSV/Scene/Prefab를 수정하지 마.

exact `PATCHINST_CORE_<RESERVATION_ID>` factory와 atomic initializer를 구현하라. four `CoreBiomeSeed` 각각의 source reservation footprint **모든 sector**를 Core seed, patch cell, assigned PrimaryBiome/Patch ownership, site binding으로 만들고 나머지는 unassigned인 exact 169-row partial snapshot을 publish하라. starter counts는 `4 patches / 4 bindings / 4 seed cells / 4 assigned / 165 unassigned / RNG 0`이다.

actual focused `>=96`, BiomePatchModels `107/107`, SiteReservationValidator `268/268`, BiomeBoundary `38/38`, StaticRegistry `53/53`, GeneratedWorldData `56/56`, actual required total `>=618`, failed/skipped `0/0`을 실행하라. targeted/full은 각각 discovery `>=4052 / >=4121`로 확인하되 실행 PASS로 표기하지 마. compile/Console/relevant warning `0/0/0`, Authoring CSV/meta `50/50`, final Assets meta `3086`, duplicate GUID `0`, exact Assets changes `14`, existing Assets modification `0`을 확인하라.

buffer/minimum growth, capacity witness 복사, Satellite/Intrusion, RNG/cost, cleanup, `PASS_BIOME`, generated CSV, validator, overlay를 구현하지 마. 전부 PASS일 때 MAP04_02 COMPLETE/Current Task NONE으로만 finalize하고 `MAP04_03_IMPLEMENT_CORE_PATCH_GROWER`는 LOCKED로 유지하라.
