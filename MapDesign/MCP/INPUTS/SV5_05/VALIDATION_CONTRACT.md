# SV5_05 focused 검증 책임

기존 Unity 6000.3.8f1/Game.Map.Tests.EditMode 환경과 실제 실행 도구를 현지 확인한다.
이번 코드와 필요한 직접 소비자만 검사한다. 아래는 책임 목록이며 형식적으로 테스트 함수를 같은 수만큼 만드는 요구가 아니다.

| ID | 실제로 검사할 동작 |
|---|---|
| T01 | 실제 SV5_04/graph 입력 identity 및 11개 route의 edge·node·port·조건 대응; 잘못된 source/anchor/condition을 거부 |
| T02 | 자원 6가지 정확한 획득 순서마다 자원 행동·복귀·Forge→Seal→Boss→Exit trace 생성; action 선행 조건과 cursor 유지 |
| T03 | 자원 0~2개로 Forge 행동, Forge 전 Seal, Seal 전 Boss 완료, Boss 전 Exit 완료를 각각 실제 API에서 거부 |
| T04 | 같은 위치에 다른 자원 mask/cursor로 재방문 가능; 중복 방문이 획득 bit/cursor를 올리지 않음; missing return/일방향 역행 실패 |
| T05 | 같은 진행 단계의 정상 지름길 허용, 조건 생략/민감 node 직행/무조건 reverse 후보 거부, rejected 후보가 원본을 변경하지 않음 |
| T06 | 후보 전체의 합성 검사: 다중 후보·일반 연결 node를 통해 생기는 우회도 탐지; 각 후보 검사만 통과하고 끝내지 않음 |
| T07 | Optional의 shortcut 없는 6순서, Required의 실제 release 요구와 normal return 보존; 미구현 물리 shortcut을 완료로 승격하지 않음 |
| T08 | 서로 다른 route의 겹침/맞닿음과 passage/clearance 접촉을 수집; 합법 합류와 조건 경계를 실제 predicate로 분류 |
| T09 | 실제 리뷰 세 좌표/109-edge AIR witness를 재현해 상태 검토 결과와 unresolved physical 의무를 export; Player PASS/geometry ready로 승격하지 않음 |
| T10 | 입력/후보 열거 역순·같은 재실행에서 판정/digest 안정, unknown 조건은 fail-closed, 같은 평가 객체에서 export/trace/집계 일치 |

서로 다른 조건 문자열이라는 사실만으로 실패를 기대하지 않는다. 실제 state/edge/port 조건에 근거한 expected 결과를 쓴다.
새 API를 호출하지 않고 과거 RMAP13 보고의 6/6 숫자만 복사하면 T02 충족이 아니다.
논리 불변식을 검사하려고 fixture 시작 flags를 미리 true로 켜 필요한 획득/제작/action을 생략하지 않는다.
부정 상태/후보는 명시적 negative fixture이며 실제 baseline source를 수정하지 않는다.
각 부정 fixture는 오류가 포함된 입력을 public checker에 전달하고 진단/원본 불변을 확인한다.

## 실행 경계

실제 focused category/class를 bound Task에 적어 실행하고 명령/필터/XML SHA와 discovered/executed/passed/failed/skipped를 기록한다.
RmapWorldGraphPlanner에 read-only 분석 확장이 필요해 정상 의미를 유지한 경우 기존 RMAP13 직접 검사도 선택해 실행한다.
SV5_04 기존 테스트는 export 시 그 Task의 generated 파일에 쓴다. 이번 작업에서 그대로 무조건 재실행해 과거 증거를 갱신하지 않는다.
관련 기존 검사를 실행할 때는 실제 출력 부작용을 먼저 확인하고 이번 scoped evidence 경로를 사용한다.
전체/무필터/오래된 대량 회귀, Scene Bake, PlayMode, Player build는 실행하지 않는다.
검사 미실행/스킵/실패는 PASS로 표시하지 않는다. 원본/기대 SHA/게임 능력/실제 predicate를 느슨하게 바꿔 통과시키지 않는다.
