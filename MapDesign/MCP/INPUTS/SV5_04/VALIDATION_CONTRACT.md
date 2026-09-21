# SV5_04 검증 범위

실제 source/CSV를 먼저 바인딩하고 이번 새 코드의 위험을 확인하는 focused EditMode만 실행한다.
기존 프로젝트의 실행 가능한 Test Runner/Unity 버전/asmdef를 현지에서 확인한다. 명령이나 테스트 category를 존재한다고 추정하지 않는다.
실제 bound Task에 선택할 테스트와 출력 경로를 기록한다. 테스트 수는 아래 책임을 검증할 만큼만 둔다.
이 문서는 신규 테스트 책임이다. 여기서 Unity를 실행했다는 증거가 아니다.

| ID | 반드시 확인할 동작 |
|---|---|
| T01 | 실제 대표 plan의 8개 site와 2,432개 원본 셀, site/slot ID·좌표·patch·port·graph binding을 재현하고 원본 바이트/객체를 보존 |
| T02 | core의 고정 S/A/O 변경, protected-air 채움, slot 머리/지지 침범을 실제 소비자 호출로 거부하고 최초 좌표/owner/이유 보고 |
| T03 | 정상 비예약 후보 허용, 호환되는 중복 예약 병합, 모순되는 지지/clearance·owner 중복 거부 |
| T04 | 모든 필수 접근/복귀 요구가 실제 연속 route·올바른 port/방향/조건·지지/여유로 연결되고 끊긴 경로나 반대 흐름은 실패 |
| T05 | 닫힌 gate를 통로로 지우려는 후보, 열린 gate를 막는 후보, Type0/다른 고정 구역을 뚫는 후보 거부; 기존 상태 의미는 보존 |
| T06 | 범위 밖 좌표·중복 identity·서로 다른 원본 plan/version 혼합이 명시적 실패이며 silent relocation/carve는 0 |
| T07 | 같은 입력의 재생성과 예약 기여 순서 재배열에서 동일 의미/정렬/digest; 기존 RNG 소비·원본 배치 변경 없음 |
| T08 | 검사한 동일 plan에서 나온 CSV/manifest와 메모리 결과의 site/cell/route/condition이 일치하고 참조 ID가 유일/유효 |

T04는 실제 static movement contract의 적합성을 검사한다. AIR flood만으로 Jump+Grab/Player 통과를 선언하지 않는다.
외부 예약 영역에 경로를 새로 계산한다면 해당 계산의 지지/머리 충돌과 실패 진단도 T04에 포함한다.
기존 45개 port 셀/10개 slot/3개 gate 쌍은 RMAP15 대표 보고의 관측값이다. 현재 동일 정본에서 실제로 대조하고 manifest에 관측값을 적는다.
의도적 실패 케이스도 정상 원본에서 한 후보/기여만 변형해 실제 public validator를 호출한다. 별도 가짜 보호 로직을 시험하지 않는다.

## 실행과 기록

우선 이번 SV5_04 필터를 실행한다. 기존 공용 코드에 변경이 꼭 필요했다면 bound Write에 이미 포함된 변경의 직접 소비자 검사만 추가한다.
무필터·전체·오래된 대량 회귀, PlayMode·Player build·전체 월드 Scene 생성은 실행하지 않는다.
검사 실패는 코드/예약 원인으로 해결한다. 기대 SHA·보호 원본·플레이어 능력 변경으로 통과시키지 않는다.
테스트 실행 불가/스킵은 실제 사유와 NOT_RUN으로 기록하고 구현 PASS로 바꾸지 않는다.
Result에는 실제 discovered/executed/passed/failed/skipped, 정확한 필터/명령, XML 경로/SHA를 기록한다.
최종 정적 검사에는 code/source/export 경로·SHA, write scope, input 불변, 임시파일 정리, 새 루트 보조 파일 0개를 포함한다.
