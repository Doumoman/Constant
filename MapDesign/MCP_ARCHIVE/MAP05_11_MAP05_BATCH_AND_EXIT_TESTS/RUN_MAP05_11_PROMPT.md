# RUN MAP05_11

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP05_11_MAP05_BATCH_AND_EXIT_TESTS.md`, MAP05_01~10 PASS Results를 순서대로 읽어라.

Prior result는 `MapDesign/MCP/REPORTS/MAP05_10_CREATE_MANDATORY_ROUTE_OVERLAY_RESULT.md`이며 SHA-256은 exact `2f8ef4e027c1abd8f93721f840b5a6ab43d812b1bcb9bd6ae71fd8d694823c6f`다. 이 값이 다르면 Phase A에서 `BLOCKED`하고 변경하지 마.

Task의 exact READ/WRITE ALLOWLIST를 지켜 approved Runtime EditMode `Generation` 폴더에 `Map05ExitTests.cs` 1개와 matching meta 1개만 추가하라. 기존 production/test/CSV/meta/asmdef/Scene/Prefab/Package/ProjectSettings는 수정하지 마.

MAP05_01~10의 mandatory route chain을 검증하라. final graph nodes/directed/undirected/route cells `47/96/48/47`, mask counts `20/4/4/17/0/0/2`, terminals `7/7`, loops `2/2`, validation `12/12/12`, overlay visual `18/18`, generated sector/edge bytes/rows `16838/7094/96`을 authoritative starter vector로 사용한다.

Type4 규칙을 반드시 지켜라. Type4는 U+D 필수이며 L/R은 actual graph adjacency에서 보존한다. `UD`, `LUD`, `RUD`, `LRUD` 네 조합 모두 합법이다. L/R canonicalization, forced open/close, graph repair, CSV rewrite, `SectorCell` mutation은 금지한다.

new `Map05ExitTests >=120`, MAP05 focused aggregate `1827/1827`, MAP05 phase aggregate/actual `>=1947`, failed/skipped `0/0`, 10,000 seed mandatory reachability batch `10000/10000`, mandatory reachability failure `0`, retry/unresolved/invalid `0/0/0`, determinism sample `102/102`, visual Game/Scene checklist `18/18`, compile/Console/warning `0/0/0`을 확인하라. Assets meta는 `3245 -> 3246`, exact Assets changes `2`, existing/unexpected `0/0`, Authoring CSV/meta `50/50`, duplicate GUID `0`이어야 한다.

전부 PASS일 때 MAP05 EXIT을 APPROVED하고 MAP05_11 COMPLETE/Current Task NONE으로만 finalize한다. `MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS`는 LOCKED로 유지하고 자동 시작하지 않는다.
