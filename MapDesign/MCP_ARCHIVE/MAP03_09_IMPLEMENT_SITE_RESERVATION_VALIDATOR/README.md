# MAP03_09 — Implement Site Reservation Validator

MAP03_08 PASS/finalize 상태에서 MAP03의 아홉 번째 Task 하나만 여는 patch package다. Patch apply는 Master, Status, 새 Task 문서만 설치하고 Assets를 변경하지 않는다.

내부 `RUN_MAP03_09_PROMPT.md`로 실행한다. 기존 approved `Generation` 폴더에 Runtime production C# 8개와 focused EditMode test 1개만 추가한다. MAP03_08의 seven-site Village approval을 required-count, world-bound, overlap, distance, entry direction, Core-capacity six-rule gate로 재검증한 뒤 deterministic reservation ID/order, 169 `SectorReservation`, six entry anchors, four `CoreBiomeSeed`를 가진 final immutable `SiteReservationSnapshot`을 atomic publish한다.

이 Task는 P01 final in-memory publication까지 수행한다. generated CSV serializer/file I/O, pass/root adapter와 retry 실행, overlay, 100,000-seed batch, biome patch growth는 후속 Task로 남긴다. 기준선은 focused `339/339`, targeted `3344/3344`, full EditMode `3384/3384`, Assets meta `3054`, Authoring CSV/meta `50/50`이다.
