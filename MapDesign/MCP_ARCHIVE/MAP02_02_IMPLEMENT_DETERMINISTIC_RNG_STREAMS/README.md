# MAP02_02 — Implement Deterministic RNG Streams

MAP02_01 PASS 상태에서 MAP02의 두 번째 Task만 여는 patch package다. Patch apply는 Master, Status, 새 Task 문서만 설치하고 Assets를 변경하지 않는다.

내부 `RUN_MAP02_02_PROMPT.md`로 실행한다. 범위는 SHA-256 seed derivation v1, SplitMix64, reset scope, required 6-stream factory/independence까지이며 MAP02_03 이후는 계속 LOCKED다.
