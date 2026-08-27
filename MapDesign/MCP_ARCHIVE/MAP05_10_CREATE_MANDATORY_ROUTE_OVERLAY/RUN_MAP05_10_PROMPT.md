# RUN MAP05_10

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP05_10_CREATE_MANDATORY_ROUTE_OVERLAY.md`, MAP05_09 PASS Result를 순서대로 읽어라.

Prior result는 `MapDesign/MCP/REPORTS/MAP05_09_VALIDATE_MANDATORY_ROUTE_GRAPH_RESULT.md`이며 SHA-256은 exact `72df536b5d51c7db7ff364e74e7bd7141f0399465e38b3a75d366640a1d3b33a`다. 이 값이 다르면 Phase A에서 `BLOCKED`하고 변경하지 마.

Task의 exact READ/WRITE ALLOWLIST를 지켜 approved runtime `Diagnostics` 폴더에 `MandatoryRouteOverlay*` production C# 4개, approved editor preview 폴더에 Scene drawer C# 1개, runtime/editor EditMode tests 2개와 matching meta 7개만 추가하라. 기존 production/test/CSV/meta/asmdef/Scene/Prefab/Package/ProjectSettings는 수정하지 마.

MAP05_09의 validated graph를 시각화하라. graph nodes `47`, directed edges `96`, undirected edges `48`, route cells `47`, terminals reachable `7/7`, accepted loops represented `2/2`, validation rules `12/12/12`, generated sectors CSV bytes `16838`, generated edges CSV bytes/rows `7094/96`을 starter vector로 사용한다.

Type4 규칙을 반드시 지켜라. Type4는 U+D 필수이며 L/R은 actual graph adjacency에서 보존한다. `UD`, `LUD`, `RUD`, `LRUD` 네 조합 모두 표시 대상이고 합법이다. L/R canonicalization, forced open/close, graph repair, CSV rewrite, `SectorCell` mutation은 금지한다.

focused `MandatoryRouteOverlayTests >=130`, `MandatoryRouteOverlaySceneDrawerTests >=24`, combined new `>=154`, MandatoryRouteGraphValidator `298/298`, MandatoryRouteGraphBuilder `281/281`, MandatoryRouteLoopPlanner `212/212`, MandatoryRouteMaskLookupBuilder `127/127`, MandatoryTerminalBuilder `120/120`, GeneratedWorldData `56/56`, actual total `>=1248`, failed/skipped `0/0`을 실행하라. compile/Console/warning `0/0/0`, visual Game/Scene checklist `18/18`, Authoring CSV/meta `50/50`, final Assets meta `3245`, duplicate GUID `0`, exact Assets changes `14`, unexpected changes `0`을 확인하라.

전부 PASS일 때 MAP05_10 COMPLETE/Current Task NONE으로만 finalize하고 `MAP05_11_MAP05_BATCH_AND_EXIT_TESTS`는 LOCKED로 유지하라.
