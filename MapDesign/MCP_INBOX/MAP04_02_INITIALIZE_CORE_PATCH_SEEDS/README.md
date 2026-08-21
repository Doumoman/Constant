# MAP04_02 — Initialize Core Patch Seeds

MAP04_01 PASS 상태에서 MAP04의 두 번째 Task만 여는 patch package다. Patch apply는 Master, Status, 새 Task 문서만 설치하고 Assets를 변경하지 않는다.

내부 `RUN_MAP04_02_PROMPT.md`로 실행한다. MAP03 final snapshot의 exact four Core source reservation footprint 전체를 deterministic Core PatchId, Core seed, initial owned sector, PrimaryBiome ownership, site binding으로 초기화하고 exact 169-row partial `BiomePatchSnapshot`을 만든다.

buffer/minimum growth, Satellite/Intrusion, RNG/cost, cleanup, export, validator, overlay, `PASS_BIOME` 실행은 MAP04_03 이후 범위이므로 시작하지 않는다. 기준선은 MAP04_01 focused `107/107`, actual required `603/603`, Assets meta `3079`, Authoring CSV/meta `50/50`이다.
