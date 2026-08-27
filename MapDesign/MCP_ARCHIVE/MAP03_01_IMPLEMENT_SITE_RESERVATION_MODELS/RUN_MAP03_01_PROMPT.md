# RUN MAP03_01

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP03_01_IMPLEMENT_SITE_RESERVATION_MODELS.md`, MAP02_08 PASS Result를 순서대로 읽어라.

Task의 exact READ/WRITE ALLOWLIST를 준수해 기존 approved Runtime/Test `Generation` 폴더에 production C# 8개, `SiteReservationModelsTests.cs` 1개와 matching meta 9개만 추가하라. existing production/tests/meta/asmdef/CSV/Scene/Prefab를 수정하지 마.

typed `SiteReservationId`, exact kind/transform/side token, final-oriented immutable footprint, entry anchor, `CoreBiomeSeed`, per-sector reservation, `SiteReservation`, exact 169-cell `SiteReservationSnapshot`을 구현하라. source/nested collections를 방어 복사하고 ordinal ordering, exact one Start, footprint↔sector cross-consistency, overlap/orphan rejection을 검증하라.

actual focused cases 최소 64개, MAP02 phase focused `667/667`, SpecialVillage `48/48`, BiomeBoundary `36/36`, StaticRegistry `53/53`, ContentVersionHash `54/54`, Game.Map targeted `>=1578`, full EditMode `>=1618`, failed/skipped `0/0`을 실행하라. compile/Console `0/0`, Authoring CSV/meta `50/50`, final Assets meta `2998`, duplicate GUID `0`, exact Assets changes `18`, existing Assets modification `0`을 확인하라.

후보 열거, mirror coordinate 적용, 거리/cost/backtracking, capacity flood, village bucket, `PASS_SITE`, ID 자동 생성, generated CSV를 구현하지 마. 전부 PASS일 때 MAP03_01 COMPLETE/Current Task NONE으로만 finalize하고 `MAP03_02_ENUMERATE_START_AND_SITE_CANDIDATES`는 LOCKED로 유지하라.
