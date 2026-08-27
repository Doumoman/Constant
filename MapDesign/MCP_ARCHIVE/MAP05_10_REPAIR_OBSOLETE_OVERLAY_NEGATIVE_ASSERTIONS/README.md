# MAP05_10 Repair — Obsolete Overlay Negative Assertions

MAP05_10 implementation은 compile되고 focused overlay tests `168/168`을 통과했지만, 기존 negative-audit tests 4개가 이제 정식 산출물인 `MandatoryRouteOverlay` 심볼을 아직 금지해서 `STATUS: BLOCKED`가 됐다.

이 repair package는 MAP05_10 현재 Task 문서만 교체한다. Master/Status/Assets는 patch apply 단계에서 변경하지 않는다.

실행 시 수정 허용 대상은 실패한 기존 test C# 4개와 `MAP05_10_CREATE_MANDATORY_ROUTE_OVERLAY_RESULT.md`뿐이다. Overlay production, graph, CSV, `SectorCell`, Authoring CSV, asmdef, Scene, Prefab은 수정하지 않는다.
