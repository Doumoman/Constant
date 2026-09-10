
<!-- SV5_06_FIX03_BEGIN -->
## SV5 물리 접촉과 상태 gate 증명의 후속 소비

SV5_07 이후 공간 작업은 SV5_06_FIX03의 실제 PASS Result/Finalize와 SV5/13_SPACE_CONTACT_FIX03.md를 선행으로 확인한다.
route ID 또는 predicate 차이만으로 동일 world cell이나 cardinal face 접촉을 분리하지 않는다.
이동 검증은 accepted geometry의 전역 좌표 통행 의미와 실제 boundary/gate를 사용한다.
서로 다른 predicate 접촉은 reroute로 제거하거나 명시적인 분리 형상과 상태 의미를 가져야 한다.
FIX02의 W01/W02 포트 수정과 protected AIR 검사는 유지한다.
Family 분포/일반 공간 밀도/재합류의 남은 책임은 SV5_07/08/09에 유지하며 다음 Task를 자동으로 열지 않는다.
<!-- SV5_06_FIX03_END -->
