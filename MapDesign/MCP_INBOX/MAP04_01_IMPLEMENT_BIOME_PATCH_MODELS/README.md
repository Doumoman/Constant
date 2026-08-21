# MAP04_01 — Implement Biome Patch Models

MAP03 exit approval 상태에서 MAP04의 첫 번째 Task만 여는 patch package다. Patch apply는 Master, Status, 새 Task 문서만 설치하고 Assets를 변경하지 않는다.

내부 `RUN_MAP04_01_PROMPT.md`로 실행한다. 기존 approved `Generation` 폴더에 immutable biome-patch production C# 7개와 focused EditMode test 1개만 추가한다. `BiomePatchId`, Core/Satellite/Intrusion, seed, 169-sector Primary/SecondaryBiome·Patch ownership, Core site binding, patch aggregate와 partial/complete snapshot을 구현한다.

Core seed 초기화, growth/RNG/cost, Satellite/Intrusion 배치, cleanup, export, validator, overlay, `PASS_BIOME` 실행은 MAP04_02 이후 범위이므로 시작하지 않는다. 기준선은 MAP03 exit approved, Assets meta `3071`, Authoring CSV/meta `50/50`이다.
