# MAP03_04 — Implement Site Distance Index

MAP03_03 PASS 상태에서 MAP03의 네 번째 Task만 여는 patch package다. Patch apply는 Master, Status, 새 Task 문서만 설치하고 Assets를 변경하지 않는다.

내부 `RUN_MAP03_04_PROMPT.md`로 실행한다. 기존 approved `Generation` 폴더에 Runtime production C# 7개와 focused EditMode test 1개만 추가한다. 성공 `FootprintPlacement`들의 occupied cells 사이 P00 4-neighbor minimum distance를 canonical pair index로 만들고, typed special-map fields로 exact six-key / 15-constraint required-site policy를 생성·평가한다.

starter policy는 minimum distribution `2×5 / 3×9 / 4×1`이다. 비용·고도·quadrant clustering·RNG·선택·backtracking·Core 용량·Village bucket·route/tile 실제 이동 거리는 MAP03_05 이후 범위로 유지한다. 기준선은 targeted `2033/2033`, full EditMode `2073/2073`, Assets meta `3012`, Authoring CSV/meta `50/50`이다.
