# RUN MAP02_08

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP02_08_MAP02_EXIT_TESTS.md`, MAP02_01~07 PASS Results를 순서대로 읽어라.

Task의 exact READ/WRITE ALLOWLIST를 준수해 신규 Runtime EditMode `Map02ExitTests.cs` 1개와 matching meta만 추가하라. production과 기존 tests는 수정하지 마. actual cases 최소 48개로 exact 169-cell/624 directed/312 undirected topology, six RNG known vectors와 draw-order independence, successful/failing recorded Root identity, seed manifest/two-file atomic publish-load/one-call replay, seed 4660 static sector hash `94ea893d55e80e4ec0a5a4758b7d84bd8e999942064d3205600e0f5a8a1bd13b`의 100회 결정론, timing isolation, overlay snapshot/layout/orientation/tooltip을 통합 검증하라.

JSON이나 later generated output placeholder를 만들지 마. 실패를 production 수정이나 assertion 완화로 고치지 마. test temp I/O는 OS temp의 test-owned directory만 사용하고 정리하라. 기존 MAP02 focused `595/595`, new exit >=48, ContentVersionHash `54/54`, Game.Map targeted >=1490, full EditMode >=1530, failed/skipped `0/0`을 모두 실행하라.

seed `4660`으로 현 프로젝트 Scene/Game overlay visual `12/12`를 다시 확인하고 prior result만 인용하지 마. compile/Console `0/0`, Authoring `50/50`, final Assets meta `2989`, duplicate GUID `0`, exact Assets changes `2`, existing Assets modification `0`을 확인하라.

전부 PASS일 때 Result에 exact `MAP02 EXIT: APPROVED`, `MAP03 ENTRY: ELIGIBLE FOR SEPARATE PATCH`, `MAP03_01: LOCKED / DO NOT START`를 기록하고 MAP02_08 COMPLETE/Current Task NONE으로만 finalize하라. MAP03_01은 자동 시작하지 마.
