# MAP01_16 — CSV Failure Fixtures and Tests

MAP01_15 PASS 상태에서 exact next Task인 MAP01_16만 여는 patch package다. patch apply는 Master, Status, 새 Task 문서만 설치하며 Assets를 수정하지 않는다.

1. `PATCH_MANIFEST.md` precondition을 검증한다.
2. payload를 적용한다.
3. `RUN_MAP01_16_PROMPT.md`를 새 채팅에 붙여 넣어 Current Task를 실행한다.

MAP01_17 이후는 계속 LOCKED다.
