# MAP03_08 — Implement Village Reservation

MAP03_07 PASS/finalize 상태에서 MAP03의 여덟 번째 Task 하나만 여는 patch package다. Patch apply는 Master, Status, 새 Task 문서만 설치하고 Assets를 변경하지 않는다.

내부 `RUN_MAP03_08_PROMPT.md`로 실행한다. 기존 approved `Generation` 폴더에 Runtime production C# 8개와 focused EditMode test 1개만 추가한다. MAP03_07의 approved six-site plan과 four Core witnesses를 보존하면서 `VIL_MOON_PRIMARY`의 Start 거리 `2-3:20 / 4-6:50 / 7-10:30`, allowed layout weight, `1x1 / 2x1 / 1x2` rectangular footprint, entry approach 충돌을 deterministic `RNG_WORLD_SITE`로 예약한다.

이 Task는 immutable `VillageReservationApproval`까지만 만든다. Village 내부 4x4 MicroChunk cell, facility/shop/merchant, final `SiteReservation`/snapshot/ID/CoreBiomeSeed publication, pass/root retry 실행은 후속 Task로 남긴다. 기준선은 focused `215/215`, targeted `3005/3005`, full EditMode `3045/3045`, Assets meta `3045`, Authoring CSV/meta `50/50`이다.
