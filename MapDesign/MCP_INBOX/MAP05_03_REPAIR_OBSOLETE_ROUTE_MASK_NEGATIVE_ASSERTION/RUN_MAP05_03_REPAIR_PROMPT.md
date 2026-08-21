# RUN MAP05_03 REPAIR

control → Master/Status → current `TASKS/MAP05_03_IMPLEMENT_MINIMUM_CONNECTOR_TREE.md` → current FAIL Result를 읽고 precondition SHA를 검증하라.

production, MAP05_03 신규 implementation/test, Master, Status, CSV, asmdef, Scene, Prefab는 수정하지 마. 수정 허용 대상은 기존 `MandatoryRouteMaskLookupBuilderTests.cs` 하나와 `REPORTS/MAP05_03_IMPLEMENT_MINIMUM_CONNECTOR_TREE_RESULT.md`뿐이다.

`MandatoryRouteMaskLookupBuilderTests`의 obsolete negative assertion `LaterTaskProductionSymbolsAreAbsent("MandatoryConnectorTree")`만 교정하라. MAP05_02 범위에서는 MAP05_04 이후 심볼 부재를 계속 검사해야 하지만, MAP05_03의 정식 산출물인 `MandatoryConnectorTree` 계열은 더 이상 금지하면 안 된다.

수정 후 `MandatoryConnectorTreeBuilderTests >=118`, `MandatoryRouteMaskLookupBuilderTests 127/127`, `MandatoryTerminalBuilderTests 120/120`, `SiteReservationValidatorTests 268/268`, `BiomePatchValidatorTests 196/196`, `Map04ExitTests 110/110`, actual total `>=939`, failed/skipped `0/0`을 다시 실행하라. Assets meta는 `3179 -> 3179`, modified existing test C# `1`, production/new files/meta/unexpected `0`이어야 한다.

PASS일 때만 MAP05_03 COMPLETE/Current Task NONE으로 finalize한다. `MAP05_04_IMPLEMENT_HORIZONTAL_BACKBONE_ROUTER`는 LOCKED로 유지하고 자동 시작하지 않는다.
