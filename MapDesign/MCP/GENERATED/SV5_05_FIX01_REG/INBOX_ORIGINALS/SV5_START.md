# SV5_START — 승인된 전체 맵 구성의 작업 착수

MODE: design_plan_handoff_v1
FIRST_TASK: SV5_01_APPROVAL_BASELINE
NEXT_TASK: SV5_02_RULES (이번에는 실행하지 않음)
PLAN_COUNT: 45
INPUT_ROOT: MapDesign/MCP/INPUTS/SV5
SOURCE_SPEC: MapDesign/MCP/INPUTS/SV5/tasks/SV5_01_APPROVAL_BASELINE.md
SOURCE_SPEC_SHA256: 4a4f6a1c9dcb74ae63e4ee30269551f133aabdf256caff5ee4374276d385dcdf

이 파일은 INBOX 인계문이다. single_task_v1 실행 Task 또는 기준 명세와 같은 파일로 취급하지 않는다.
사용자는 SV5 계획으로 작업을 시작하는 첫 INBOX 패치를 요청했다. 현지 정상 절차로 첫 Task만 수행한다.

## 이번 결정

- 사용자가 승인한 624×416 예시의 장소 구성·밀도·연결 방식을 기준으로 등록한다.
- 큰 장소 사이에 작은 방·굴·샛길·재합류 통로를 두어 하나의 지형으로 읽히게 한다.
- 점프맵에는 실제 이동에 쓰이는 SOLID·안전 Grab을 포함하고 내부 외곽 일부에 1~2칸 굴곡을 추가한다.
- 일반 +1칸, Jump+Grab 최대 +2칸의 기존 Player 한계를 유지한다.
- 승인된 정적 예시와 실제 전 구간 완주·장치 구현·진행 검증은 구분한다.

## 읽기 순서

1. 루트 SV5_README.md의 외부 INBOX/명세/목록 SHA와 원본을 비교한다.
2. SV5_FILES.json과 SV5_VERIFY.py로 패키지 입력과 승인 원본을 검사한다.
3. 현지 AGENTS/MCP 진입점·실제 상태·Task 형식·등록/Apply/Finalize 계약과 관련 이력을 읽는다.
4. INPUT_ROOT의 SPACE_V5_MEMORY.md → SPACE_V5_RULES.md → SPACE_V5_TASKS.md를 읽는다.
5. tasks/SV5_01_APPROVAL_BASELINE.md를 읽고 이번 작업에 필요한 경로만 바인딩한다.
6. 큰 baseline CSV/JSON/ZIP은 스크립트 요약으로 확인한다. 한 번에 전체 문서나 전체 타일을 입력하지 않는다.

## 해시의 종류를 분리

- INBOX_SHA는 이 SV5_START.md의 원본 바이트 SHA이며 루트 인계문에 고정한다.
- SOURCE_SPEC_SHA는 위에 고정한 계획 명세의 SHA다.
- BOUND_TASK_SHA는 현지 실제 Read/Write·선행 근거를 바인딩해 발행한 실행 Task의 SHA다.
- INSTALLED_TASK_SHA와 ARCHIVE_TASK_SHA는 실제 설치·Archive 파일을 읽어 계산한다.
- 서로 다른 파일끼리 SHA가 같아야 한다고 요구하지 않는다. 각 역할에 연결된 원본끼리 비교한다.
- 동일 역할의 기대값/실제값이 다르면 그 파일을 기록한다. 줄바꿈 정규화·기대 SHA 수정으로 맞추지 않는다.

## 현지 수행 순서

1. 변경 전 상태와 기존 SP/SV2/SV3/SV4/SV5 이력을 확인한다. 알려지지 않은 완료 수·CURRENT를 추정하지 않는다.
2. 45개 계획을 현지 정상 등록/전환 절차로 연결한다. 완료·설치 원본과 다른 CURRENT는 보존한다.
3. 이 SOURCE_SPEC을 바탕으로 실제 계약의 300줄 이하 실행 Task를 별도 발행하고 외부 SHA를 기록한다.
4. 정상 Apply 후 SV5_01만 수행하여 승인 근거·baseline·계획 연결·현지 바인딩을 기록한다.
5. 기준 문서와 입력의 대응을 검사하고 정상 Result·Finalize·현지 소유 commit 절차를 따른다.
6. 결과·각 역할의 SHA·전후 실제 상태·commit과 미실행 범위를 보고하고 종료한다.

이번에는 Unity 씬·플레이어·점프맵·생성 코드를 수정하거나 전체 테스트/맵 Bake를 실행하지 않는다.
SV5_02 이후, RMAP18/19, push는 실행하지 않는다. 다음 상태는 현지 정상 계약의 결과를 보고한다.
원본·잠금·보호·승인 검사를 우회하지 않는다. 필요한 정상 인계 절차가 있으면 먼저 그 절차를 따른다.
이 패키지를 만든 외부 도구가 현지 MCP 설치/Finalize/commit을 완료한 것으로 보고하지 않는다.
