# MAP04_04 — Implement Satellite Seed Placer

MAP04_03 PASS 상태에서 MAP04의 네 번째 Task만 여는 patch package다. Patch apply는 Master, Status, 새 Task 문서만 설치하고 Assets를 변경하지 않는다.

내부 `RUN_MAP04_04_PROMPT.md`로 실행한다. MAP04_03의 Core-grown partial snapshot에서 exact four Satellite rule count를 `RNG_BIOME_PATCH`로 먼저 추첨하고, unassigned·unreserved sector 중 same-biome 최소 거리와 edge rule을 만족하는 one-cell Satellite seed patch를 atomic 배치한다.

count를 candidate retry와 섞지 않으며 failed seed만 최대 100회 redraw한다. Satellite 성장, remaining-world cost/noise ownership, Intrusion, cleanup, export, validator, overlay, `PASS_BIOME` 실행은 MAP04_05 이후 범위이므로 시작하지 않는다. 기준선은 MAP04_03 focused `127/127`, actual required `570/570`, Assets meta `3093`, Authoring CSV/meta `50/50`이다.
