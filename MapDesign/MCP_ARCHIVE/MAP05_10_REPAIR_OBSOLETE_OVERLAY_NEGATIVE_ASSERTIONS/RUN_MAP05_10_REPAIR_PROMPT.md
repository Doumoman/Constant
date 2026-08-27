# RUN MAP05_10 REPAIR

control → Master/Status → current `TASKS/MAP05_10_CREATE_MANDATORY_ROUTE_OVERLAY.md` → current BLOCKED Result를 읽고 precondition SHA를 검증하라.

Current Task SHA는 exact `b2ec466044db9a35cdb84bd691eb5f5c8c318db761947a1019c5634716642039`, BLOCKED Result SHA는 exact `601d11f6fe3ee15b094f5d17e9bd679dafe8682523c48bff31d80e93a6295e3f`다. 다르면 Phase A에서 `BLOCKED`하고 변경하지 마.

production, MAP05_10 신규 overlay 파일, Master, Status, CSV, asmdef, Scene, Prefab는 수정하지 마. 수정 허용 대상은 기존 negative-audit test C# 4개와 `REPORTS/MAP05_10_CREATE_MANDATORY_ROUTE_OVERLAY_RESULT.md`뿐이다.

실패한 4개 테스트의 obsolete “MAP05_10+ symbol absent” 경계를 “MAP05_11+ symbol absent” 경계로 교정하라. `MandatoryRouteOverlayCell`, `MandatoryRouteOverlaySnapshot`, `MandatoryRouteOverlayGui`, `MandatoryRouteOverlay`는 MAP05_10 정식 산출물이므로 더 이상 금지하면 안 된다. 단 MAP05_11 이후 심볼은 계속 금지한다.

수정 후 MAP05_10 focused `168/168`, required regression `1206/1206`, repaired four suites `127/127 + 212/212 + 281/281 + 298/298`, actual total `>=1374`, failed/skipped `0/0`, visual Game/Scene checklist `18/18`, compile/Console/warning `0/0/0`을 확인하라. Assets meta는 `3245 -> 3245`, modified existing test C# `4`, production/new files/meta/unexpected `0`이어야 한다.

PASS일 때만 MAP05_10 COMPLETE/Current Task NONE으로 finalize한다. `MAP05_11_MAP05_BATCH_AND_EXIT_TESTS`는 LOCKED로 유지하고 자동 시작하지 않는다.
