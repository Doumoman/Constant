# MAP04_05 — Implement Multi-Seed Biome Grower

MAP04_04 PASS 상태에서 MAP04의 다섯 번째 Task만 여는 patch package다. Patch apply는 Master, Status, 새 Task 문서만 설치하고 Assets를 변경하지 않는다.

내부 `RUN_MAP04_05_PROMPT.md`로 실행한다. MAP04_04의 partial snapshot을 입력으로, capacity gate 통과 후 patch×target deterministic noise table을 먼저 고정하고 distance·altitude·noise·perimeter·reservation cost의 stable multi-seed frontier로 모든 미예약 sector를 atomic 소유한다.

Core/Satellite seed, Core binding, P01 reservation은 불변으로 보존하고 모든 일반 patch를 rule minimum/maximum 안에 두며 same-biome world-share 상한을 지킨다. actual attempt-0 `2/0/2/3`은 target/capacity `165/161`로 4칸 부족하므로 RNG 추가 소비 없이 `RetryRequired`를 내고, 용량이 충분한 attempt에서만 성장한다. Intrusion, cleanup, export, final validator, overlay, `PASS_BIOME` root adapter는 MAP04_06 이후 범위로 남겨둔다. 기준선은 MAP04_04 actual focused `141/141`, actual required `599/599`, Assets meta `3101`, Authoring CSV/meta `50/50`이다.
