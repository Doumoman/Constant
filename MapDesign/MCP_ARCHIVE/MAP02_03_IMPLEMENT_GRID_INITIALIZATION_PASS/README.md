# MAP02_03 — Implement Grid Initialization Pass

MAP02_02 PASS 상태에서 MAP02의 세 번째 Task만 여는 patch package다. Patch apply는 Master, Status, 새 Task 문서만 설치하고 Assets를 변경하지 않는다.

내부 `RUN_MAP02_03_PROMPT.md`로 실행한다. 범위는 13×13 `P00 Grid`, `(y*13+x)` index, L/R/U/D precomputed neighbor, out-of-world `-1`, immutable result까지다. 현재 meta `2954`와 accepted legacy folder meta 6개를 baseline으로 고정하고 새 directory를 만들지 않아 이전 repair loop가 반복되지 않도록 했다. MAP02_04 이후는 계속 LOCKED다.
