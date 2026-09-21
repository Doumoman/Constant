# SV5_02 R2 — GENERATED 경로 정정

## 원인과 정정

패키지 작성자가 SV5_01 Result의 GENERATED 축약 표기를 MapDesign 기준 경로로 해석하면서 MCP/를 누락했다.
사용자가 전달한 현지 실행 보고는 실제 산출물이 MapDesign/MCP/GENERATED/SV5_01에 있음을 확인한다.
기존 검증이 정상적으로 불일치를 잡아 Apply 이전에 중단했고, 실행 Task·상태·게임 코드 변경은 없었다고 보고되었다.
이 문서는 그 보고를 근거로 발행한 정정 입력이다. 이 작업환경에서 실제 저장소를 검사한 결과로 표시하지 않는다.

| 용도 | R1의 잘못된 MapDesign 상대 경로 | R2의 MapDesign 상대 경로 |
|---|---|---|
| 선행 기준 | GENERATED/SV5_01/BASELINE.json | MCP/GENERATED/SV5_01/BASELINE.json |
| 계획 연결 | GENERATED/SV5_01/PLAN_LINK.json | MCP/GENERATED/SV5_01/PLAN_LINK.json |
| 선행 바인딩 | GENERATED/SV5_01/BINDING.json | MCP/GENERATED/SV5_01/BINDING.json |
| 이번 출력 | GENERATED/SV5_02/ | MCP/GENERATED/SV5_02/ |

앞의 세 선행 파일의 SHA-256은 R1과 동일하다. 잘못된 경로로 파일을 옮겨 검사에 맞추지 않는다.
새 명세·INBOX·manifest는 R2의 별도 입력 경로에서 새 SHA를 갖는다. SOURCE_SPEC_SHA를 BOUND_TASK_SHA에 대입하지 않는다.
TASK_ID, 선행 Result/Task SHA, 74개 원문 문장, JUMP-01~14, 규칙 candidate와 허용 작업 책임은 유지한다.
R2 검증기는 정정된 세 경로를 검사하며 입력 폴더의 보고서 사본을 실제 Result로 받지 않는다.

## 전달된 현지 증거

- SV5_01 Finalize commit: 9e6a71489fb4881c4112c378f7ca043bbbd85e85
- 보고된 선행 commit: da1803447ac3275da4c674fc67a00844ff252ad5
- 상태: 285 = 239 COMPLETE / 0 CURRENT / 46 LOCKED; Current NONE; SV5_02/03 LOCKED.
- Status SHA: 143350621914313f3ba8ccf12dccada6b98078b25c7c4a7044892c9262c2071a
- Master SHA: cbe616e54c85a05181759dc87997f68e94f5ee92ff1a2a9896396a9e3c937d4c
- Result SHA: 78f9e84bf82b9640c51a5ee4bc2847610a296ebd26e0849706d3ce58601a37b6
- installed/Archive Task SHA: c63acc8cf464ae837d5e83efef9976f5e12f42f1cf8a32f2634ba32cf587d5ed

위 값은 사용자가 붙여 넣은 BLOCKED 보고의 관측값이다. 실행자는 현재 로컬 증거와 정상 계약을 다시 확인한다.
현재 HEAD가 위 commit 자체여야 하는 것은 아니다. 선행 commit의 존재·포함 증거·현재 계보를 확인한다.
Status/Master 값은 실행 전 확인 자료이며 정상 Apply/Finalize 이후에도 같도록 강제하지 않는다.

## 재시도 범위

1. 기존 R1을 그대로 두고 R2의 새 경로만 추가한다. R1 입력이 없다면 재설치할 필요는 없다.
2. 실제 계약/상태를 먼저 확인한다. 이미 COMPLETE면 정상 증거를 보고하고 중복 실행하지 않는다.
3. LOCKED / Current NONE일 때 R2 패키지 검사와 실제 선행 검사를 모두 통과한 뒤 이번 입력으로 바인딩한다.
4. 선행 바이트 불일치·다른 Current·충돌하는 bound Task가 있으면 정상 계약에 따라 중단/복구하고 상태를 수동 강제하지 않는다.
5. SV5_02_RULES만 정상 Apply·수행·Finalize하고 작업 소유 변경만 commit한다. 45개 계획 재등록은 없다.
6. 최종 콘솔에 R2 역할별 SHA, 실제 상태와 commit을 보고한다. 이후 작업·Unity·게임 코드·push는 시작하지 않는다.

이 정정은 묶음 실행 전환이 아니다. 기존의 한 TASK 후 종료 범위를 유지한다.
