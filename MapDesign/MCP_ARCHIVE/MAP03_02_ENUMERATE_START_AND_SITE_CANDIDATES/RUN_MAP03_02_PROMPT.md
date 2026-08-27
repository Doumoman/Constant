# RUN MAP03_02

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP03_02_ENUMERATE_START_AND_SITE_CANDIDATES.md`, MAP03_01 PASS Result를 순서대로 읽어라.

Task의 exact READ/WRITE ALLOWLIST를 준수해 기존 approved Runtime/Test `Generation` 폴더에 production C# 6개, `SiteCandidateEnumerationTests.cs` 1개와 matching meta 7개만 추가하라. existing production/tests/meta/asmdef/CSV/Scene/Prefab를 수정하지 마.

Start ring `0..1` exact `88 = 48+40`, Boss 1×169, Forge 1×169, CoreResource 3×169을 raw origin으로 열거하라. exact group order와 count는 `6 groups / 845 site origins / 933 total / Village 0`이다. 2×1 Boss의 `(12,12)`도 유지해 footprint 배치 filtering을 하지 않았음을 증명하라.

actual focused cases 최소 72개, MAP03_01 `81/81`, MAP02 phase `667/667`, SpecialVillage `57/57`, BiomeBoundary `38/38`, StaticRegistry `53/53`, ContentVersionHash `54/54`, Game.Map targeted `>=1667`, full EditMode `>=1707`, failed/skipped `0/0`을 실행하라. compile/Console `0/0`, Authoring CSV/meta `50/50`, final Assets meta `3005`, duplicate GUID `0`, exact Assets changes `14`, existing Assets modification `0`을 확인하라.

transform 적용, footprint/entry 배치, 충돌, 거리/cost, RNG, 선택/backtracking, Core capacity, Village, `PASS_SITE`, serializer/file I/O를 구현하지 마. 전부 PASS일 때 MAP03_02 COMPLETE/Current Task NONE으로만 finalize하고 `MAP03_03_IMPLEMENT_FOOTPRINT_PLACEMENT_SOLVER`는 LOCKED로 유지하라.
