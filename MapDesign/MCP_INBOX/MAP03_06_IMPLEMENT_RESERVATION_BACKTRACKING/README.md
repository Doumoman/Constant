# MAP03_06 — Implement Reservation Backtracking

MAP03_05 PASS 상태에서 MAP03의 여섯 번째 Task 하나만 여는 patch package다. Patch apply는 Master, Status, 새 Task 문서만 설치하고 Assets를 변경하지 않는다.

내부 `RUN_MAP03_06_PROMPT.md`로 실행한다. 기존 approved `Generation` 폴더에 Runtime production C# 8개와 focused EditMode test 1개만 추가한다. Start, Boss, Forge, CoreResource 3개의 exact six groups를 비용 오름차순과 fresh `RNG_WORLD_SITE` equal-cost tie-break로 탐색하고 collision·distance·Core cluster hard constraint를 통과하는 잠정 조합을 depth-first backtracking으로 선택한다.

이 Task는 `SiteReservationSelectionPlan`까지만 만든다. 실제 Core capacity flood, Village, final `SiteReservation`/snapshot/ID publication은 후속 Task로 남긴다. 기준선은 focused `270/270`, targeted `2542/2542`, full EditMode `2582/2582`, Assets meta `3027`, Authoring CSV/meta `50/50`이다.
