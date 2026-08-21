# RUN MAP03_07

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP03_07_IMPLEMENT_CORE_CAPACITY_FLOOD_CHECK.md`, MAP03_06 PASS Result를 순서대로 읽어라.

Task의 exact READ/WRITE ALLOWLIST를 준수해 기존 approved Runtime/Test `Generation` 폴더에 production C# 8개, `CoreCapacityFloodCheckerTests.cs` 1개와 matching meta 9개만 추가하라. existing production/tests/meta/asmdef/CSV/Scene/Prefab를 수정하지 마.

MAP03_06 exact six-site selection plan에서 Forge와 CoreResource 3개의 typed requirements를 canonical order로 검사하라. occupied footprint 전체를 multi-source seed로 사용하고 Manhattan/cardinal buffer를 만든다. 다른 selected footprint와 다른 mandatory buffer를 hard-block하며, non-edge-touch rule의 out-of-world buffer를 거부한다. `max(Core rule minimum, mandatory buffer count)`까지 independent connected capacity와 disjoint deterministic BFS witness를 검증하라. entry exterior는 biome capacity blocker가 아니며 이 Task의 RNG draw는 exact `0`이다.

actual focused cases 최소 180개, MAP03_06 `248/248`, MAP03_05 `270/270`, MAP03_04 `239/239`, MAP03_03 `170/170`, MAP03_02 `268/268`, MAP03_01 `81/81`, MAP02 phase `667/667`, SpecialVillage `57/57`, BiomeBoundary `38/38`, StaticRegistry `53/53`, ContentVersionHash `54/54`, Game.Map targeted `>=2970`, full EditMode `>=3010`, failed/skipped `0/0`을 실행하라. compile/Console `0/0`, Authoring CSV/meta `50/50`, final Assets meta `3045`, duplicate GUID `0`, exact Assets changes `18`, existing Assets modification `0`을 확인하라.

성공 결과는 original plan과 exact four capacity witnesses를 가진 `CoreCapacityApproval`까지만 만든다. starter witness는 four sites each target `5`, total `20`, overlap `0`이다. Village, CoreBiomeSeed, final reservation/snapshot/ID publication, biome painting/growth, option 재선택, pass/root retry 실행, serializer/file I/O를 구현하지 마. 전부 PASS일 때 MAP03_07 COMPLETE/Current Task NONE으로만 finalize하고 `MAP03_08_IMPLEMENT_VILLAGE_RESERVATION`은 LOCKED로 유지하라.
