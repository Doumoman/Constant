# MAP03_03 — Implement Footprint Placement Solver

MAP03_02 PASS 상태에서 MAP03의 세 번째 Task만 여는 patch package다. Patch apply는 Master, Status, 새 Task 문서만 설치하고 Assets를 변경하지 않는다.

내부 `RUN_MAP03_03_PROMPT.md`로 실행한다. 기존 approved `Generation` 폴더에 Runtime production C# 6개와 focused EditMode test 1개만 추가한다. MAP03_02 raw origin 하나와 transform 하나를 받아 footprint cell·required side·entry socket을 함께 변환하고, 13×13 world-bound, 기존 footprint overlap, 보호된 entry approach, candidate entry exterior를 판정한다.

starter empty-blocker 기준 exact `3468` option 중 `3156` success, `312` rejection이며 breakdown은 `FootprintOutsideWorld 52 / EntryOutsideWorld 260`이다. 거리·비용·RNG·선택·reservation 생성·backtracking·Core 용량·Village·`PASS_SITE`는 MAP03_04 이후 범위로 유지한다. 기준선은 targeted `1863/1863`, full EditMode `1903/1903`, Assets meta `3005`, Authoring CSV/meta `50/50`이다.
