<!-- SV5_06_FIX04_BEGIN -->
## SV5 물리 접촉과 FSM product 증명의 후속 소비

SV5_07 이후 공간 작업은 SV5_06_FIX04의 실제 PASS Result/Finalize와 `SV5/14_SPACE_PRODUCT_FIX04.md`를 선행으로 확인한다.
ConditionalGate 접촉은 해당 SHARED cell 또는 해당 FACE에 실제 barrier geometry가 있어야 하며, route 소유권이나 원격 gate만으로 분리를 선언하지 않는다.
모든 닫힌 gate를 동시에 적용한 물리 이동과 canonical RMAP13 FSM의 product에서 현재 열려야 하는 required approach/return/recovery를 보존한다.
gate cut 합성은 선택된 guarded endpoint뿐 아니라 모든 reachable state의 open transition 보존 검사를 통과해야 한다.
FIX03의 전역 좌표 이동, FIX02의 W01/W02 포트 수정과 protected AIR 검사를 유지한다.
Family 분포/일반 공간 밀도/재합류의 남은 책임은 SV5_07/08/09에 유지하며 다음 Task를 자동으로 열지 않는다.
<!-- SV5_06_FIX04_END -->
