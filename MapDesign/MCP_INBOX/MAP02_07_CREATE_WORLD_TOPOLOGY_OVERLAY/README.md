# MAP02_07 — Create World Topology Overlay

MAP02_06 PASS 상태에서 MAP02의 일곱 번째 Task만 여는 patch package다. Patch apply는 Master, Status, 새 Task 문서만 설치하고 Assets를 변경하지 않는다.

내부 `RUN_MAP02_07_PROMPT.md`로 실행한다. 범위는 exact 169-cell grid를 immutable 표시 snapshot으로 복사하고, 같은 runtime GUI renderer로 Game View와 Scene View에 뒤집힘 없는 13×13 좌표·Role glyph·월드 타일 범위·L/R/U/D hover tooltip을 표시하는 것까지다. Root/pass/RNG/replay/CSV를 실행하거나 변경하지 않고 MAP02 exit test도 선행하지 않는다. 현재 Assets meta `2981`과 accepted legacy folder meta `6/6`을 baseline으로 고정했고 새 directory는 만들지 않는다.
