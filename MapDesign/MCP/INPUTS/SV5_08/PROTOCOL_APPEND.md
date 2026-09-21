
<!-- SV5_08_INFILL_BEGIN -->
## SV5 일반 공간 채움 소비

SV5_09 이후는 SV5_08의 실제 PASS Result/Finalize와 `SV5/16_INFILL_V5.md`를 읽는다.
현재 생산 진입점은 Sv5SpaceGraphPlanner.PlanWithInfill이며 기존 Plan은 07 기준선 생성/회귀용으로 유지한다.
새 방·굴·계단참의 실제 셀, 4×4 write mask, 출입구·부모 연결·왕복 증거와 같은 accepted plan을 소비한다.
미소유 잔여는 INFILL_PENDING이고 AIR/SOLID가 아니다. 방 수/예약률을 완성 지형 또는 Player 통과로 승격하지 않는다.
배경은 미조립, 확정한 infill 셀은 SOLID/AIR로 구분한다. SV5_41은 이 payload를 실제 합성하고 SV5_44는 Player를 검증한다.
08의 추가 공간은 기존 actionless 통로에 붙인 구조물이다. 별개 지역의 재합류/지름길은 SV5_09에서 현행 gate/product로 검증한다.
기존 4×4 패턴/12×8 청크, +1/+2 이동 한계, 3~4칸 구조와 6×6 검사, SV5_07 반복 억제를 유지한다.
이전 증거를 재생성하지 않고 다음 Task 소유 출력으로 격리한다. 45개 본 작업 순서를 바꾸지 않는다.
<!-- SV5_08_INFILL_END -->
