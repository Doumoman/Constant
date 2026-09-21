<!-- SV5_05_FIX01_BEGIN -->
## SV5 진행 상태 보완 결과 소비

SV5_06/09/41을 바인딩하기 전에 MCP/SV5/09_ROUTE_STATE_FIX01.md와 SV5_05_FIX01의 실제 Result/Finalize를 확인한다.
SV5_05의 과거 6순서 PASS만으로 새 후보집합의 접근 조건·복귀 안전성이 확인됐다고 간주하지 않는다.
현재 candidate payload/digest와 보완된 상태 검사 결과를 사용한다. 미해결 geometry와 Player 검증은 별도로 남긴다.
SV5_41은 합성 geometry, SV5_44는 전체 Player 검증을 담당한다. 기존 45개 ID/순서와 한 Task 실행 원칙을 유지한다.
<!-- SV5_05_FIX01_END -->
