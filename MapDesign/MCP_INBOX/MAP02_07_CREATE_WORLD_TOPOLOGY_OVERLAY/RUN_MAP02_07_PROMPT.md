# RUN MAP02_07

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP02_07_CREATE_WORLD_TOPOLOGY_OVERLAY.md`, MAP02_06 PASS Result를 순서대로 읽어라.

Task의 exact READ/WRITE ALLOWLIST를 준수해 `GridInitializationResult`를 independent immutable 169-cell overlay snapshot으로 복사하라. x는 왼쪽→오른쪽, y는 아래→위이며 visual top row는 y=12, bottom row는 y=0이다. 모든 cell에 `x,y`와 Role glyph를 표시하고 hover에 inclusive world-tile range, exact Role token, L/R/U/D neighbor index를 표시하라.

Game View `WorldTopologyOverlay.OnGUI`와 Editor Scene drawer는 exact 같은 runtime `WorldTopologyOverlayGui` layout/draw/hit-test를 사용해야 한다. color와 glyph/legend를 함께 쓰고 fixed `440×564` panel에 169 cells를 전부 표시하라. inspector preview는 명시적 버튼에서만 P00 `GridInitializationPass`를 한 번 실행하며 Root/RNG/replay/file I/O를 호출하지 마. 자동 generation, global current-world lookup, saved/hidden persistent object, y flip/clamp, 후속 Role/ID placeholder, MAP02 exit test 선행 구현을 금지한다.

focused >=60, 기존 `56/103/90/84/77/97/54`, targeted >=1434, full EditMode >=1454, visual `12/12`, compile/Console `0/0`, Authoring `50/50`, final Assets meta `2988`, duplicate GUID `0`, exact Assets changes `14`, existing Assets modification `0`을 모두 PASS하라.

전부 PASS일 때만 MAP02_07 COMPLETE/Current Task NONE으로 finalize하고 MAP02_08은 LOCKED로 유지하라.
