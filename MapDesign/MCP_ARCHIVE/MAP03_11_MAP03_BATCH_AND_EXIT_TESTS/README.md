# MAP03_11 — MAP03 Batch and Exit Tests

MAP03_10 PASS/finalize 상태에서 MAP03의 마지막 Task 하나만 여는 patch package다. Patch apply는 Master, Status, 새 Task 문서만 설치하고 Assets를 변경하지 않는다.

내부 `RUN_MAP03_11_PROMPT.md`로 실행한다. 기존 approved Runtime test `Generation` 폴더에 `Map03ExitTests.cs`와 matching meta 하나만 추가한다. MAP03_01~10 production은 수정하지 않는다.

exit test는 exact 100,000-world-seed Village bucket RNG schedule의 `20/50/30` 분포, 10,000-world-seed full reservation attempt와 최대 8회 test-observation retry, retry terminal/reason conservation, required seven reservations/entries/Core capacity/six-rule publication, same-seed determinism, current Game/Scene overlay를 검증한다.

이 Task는 production batch runner, pass/root adapter, retry policy, generated export, biome growth를 구현하지 않는다. 기준선은 existing MAP03 focused `2259/2259`, targeted `3745/3745`, full EditMode `3813/3813`, visual `18/18`, Assets meta `3070`, Authoring CSV/meta `50/50`이다.
