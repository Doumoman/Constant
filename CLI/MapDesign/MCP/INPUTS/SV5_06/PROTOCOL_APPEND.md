
<!-- SV5_06_SPACE_GRAPH_BEGIN -->
## SV5 공간 그래프 소비

SV5_07~10 및 SV5_41은 MCP/SV5/10_SPACE_GRAPH_V5.md와 MCP/GENERATED/SV5_06의 실제 plan/validation을 읽는다.
장소·통로·접촉·보호 예약은 동일한 공간 plan digest로 연결한다. 과거 FIX01의 contact checked 표시를 물리 증거로 승격하지 않는다.
후속 배치 변경은 06의 접촉 전수검사와 현재 FSM 기반 전체 연결 검사를 다시 호출한다.
PLANNED_LAYOUT / LOGICAL_STATE / CONTACT_STATE / COMPOSED_GEOMETRY / PLAYER 검증을 분리한다.
SV5_06 완료는 세부 지형·기능·Scene Bake 완료가 아니다. 다음 Task 하나씩만 새 정상 입력으로 연다.
<!-- SV5_06_SPACE_GRAPH_END -->
