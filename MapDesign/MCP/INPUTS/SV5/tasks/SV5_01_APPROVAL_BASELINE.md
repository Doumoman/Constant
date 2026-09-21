# SV5_01_APPROVAL_BASELINE — 승인 기준 등록 명세

FORMAT: source_spec_v1
STATUS: PLANNED_NOT_INSTALLED
TASK_ID: SV5_01_APPROVAL_BASELINE
SCOPE: 승인 근거와 45개 계획의 문서·등록 작업
이 원본 명세는 현지 single_task_v1 실행 Task가 아니다. 바인딩 후 정상 발행·Apply한다.

## 목표

사용자가 승인한 624×416 공간 구성과 점프맵 수정 요청을 이후 작업이 계속 참조할 기준으로 등록한다.
첫 작업은 기준·입력·계획 연결까지다. 새 이동 규칙의 구체화는 SV5_02, 전체 코드 접점 조사는 SV5_03, 지형 수정은 해당 후속 Task다.

## 필수 입력

- 루트 SV5_README.md와 SV5_FILES.json, MapDesign/MCP_INBOX/SV5_START.md.
- MapDesign/MCP/INPUTS/SV5/SPACE_V5_MEMORY.md, SPACE_V5_RULES.md, SPACE_V5_TASKS.md.
- 같은 입력 폴더의 TASKS.csv·TASKS.json·SHA256.json·PLAN_ORIGIN.json.
- baseline/BASELINE_SHA.json, world_cells.csv, regions.json, connections.json, validation.json.
- baseline/의 승인 PDF·전체 PNG·확대 PNG·원본 예시 ZIP 4개. 해시만 적힌 누락 파일이 없게 함께 제공한다.
- 현지 AGENTS 및 실제 MCP 진입점, Task 형식·등록·Apply·Finalize 계약, 현재/전체 작업표와 관련 이력.
- reference/reported/SV517_WORLD_BAKE_RESULT.md는 제출된 과거 보고다. 현재 저장소 상태·선행 SHA를 대신하지 않는다.

큰 CSV·JSON·ZIP은 스크립트로 검사하고 필요한 요약만 읽는다. 259,584행을 MCP 문서 입력에 통째로 넣지 않는다.
이 작업에서는 다른 장소의 세부 명세를 전부 읽거나 구현하지 않는다. 승인 범위 확인에 필요한 자료만 읽는다.

## 현지 바인딩

1. 변경 전 실제 Task 상태와 관련 작업 이력을 기록한다. 제출 Result만으로 현재 COMPLETE 수를 추측하지 않는다.
2. 실제 문서/계획 소유 경로를 확인해 Read·Write 허용 목록을 작성한다. 없는 API·Task 형식은 만들어 낸 것으로 가정하지 않는다.
3. 기존 SP/SV2/SV3/SV4/SV5가 있으면 INSTALLED/COMPLETE/PLANNED를 구분하고 결과 SHA로 재사용 근거를 남긴다.
4. 미설치 구계획은 현지 정상 전환 절차에 따라 SV5로 연결한다. 완료·설치 원본은 보존한다.
5. 다른 CURRENT와 충돌하지 않게 정상 인계/등록 절차를 사용한다. 자동 잠금 해제나 수동 상태 숫자 맞추기는 하지 않는다.
6. 실제 선행 Result, 설치/Archive Task, 기준 문서와 새 원본 명세 SHA를 서로 다른 필드로 바인딩한다.
7. 300줄 이하의 현지 single_task_v1을 별도 경로에 발행하고 그 실제 바이트 SHA를 외부 인계문에 기록한다.
8. 발행된 실행 Task를 정상 Apply한다. source_spec_v1 파일을 이름만 바꿔 실행 Task로 설치하지 않는다.

## 허용 쓰기 범위

- 이 입력 패키지의 동일 바이트 배치와 등록.
- 현지 계획/문서 등록 경로에 45개 SV5 순서와 첫 작업 책임을 연결하는 최소 변경.
- 기존의 계속 읽는 프로젝트 결정/인계 문서에 승인 기준의 경로·SHA와 SV5 진입점을 추가.
- 현지 정상 절차가 관리하는 실행 Task·Archive·Result·상태 파일.
- 아래 결과물을 담는 이 Task 소유 출력 폴더.

Assets·Scene·Prefab·Player·충돌/이동 수치·맵 생성 코드·런타임 CSV는 이 작업의 Write 범위에 포함하지 않는다.
운영 승인·해시·잠금·Finalize 검사를 통과시키려는 목적으로 AGENTS나 검증기를 변경하지 않는다.
입력 원본과 baseline은 그대로 보존한다. 바인딩 차이는 별도 출력으로 기록한다.

## 등록할 승인 결정

1. 타일 1×1, Pattern 4×4, MicroChunk 12×8, 전체 624×416, 52×52 청크.
2. 큰 장소와 그 사이의 작은 방·굴·샛길·재합류 통로가 하나의 지형으로 이어지는 공간 구성을 승인.
3. 예시의 101개 장소·166개 연결은 이 시안의 측정값. 모든 시드의 고정 개수나 분포 규칙이 아님.
4. 점프맵은 실제 이동에 쓰이는 SOLID와 안전 Grab 모서리를 포함. ONE_WAY 발판 사용도 허용.
5. 점프맵 외곽 일부를 1~2칸 변형. 출입구·머리 공간·Grab·착지·주변 보호를 보존.
6. 일반 +1칸, Jump+Grab 최대 +2칸. 기존 Player 능력을 올려 통과시키지 않음.
7. 시각 승인과 실제 Unity 완주·장치 구현·상태 진행 검증은 별개. baseline은 physics NOT_RUN.
8. 예시 핵심 site 좌표를 SV5 보호 데이터에 바로 덮지 않음. 실제 정본 연결은 후속 책임.
9. Grab 동작 최소 1회·외곽 구간 2곳은 초기 제작 운영 기준이며 사용자 지정 숫자로 확대 기록하지 않음.

## 결과물 계약

현지 같은 책임의 출력 규칙이 있으면 그 경로를 사용하고 실제 위치를 Result에 적는다.
별도 규칙이 없을 때의 제안 경로는 MapDesign/MCP/GENERATED/SV5_01/ 이다.

- APPROVAL.md: 사용자 승인 문맥, 승인 대상 파일, 새 요청, 구현/검증과의 구분.
- BASELINE.json: 승인 artifact SHA와 크기, 좌표 원점, world/chunk/tile 크기, 검증 상태, 보존할 입력 경로.
- PLAN_LINK.json: 45개 Task ID·순서, 현지 문서/계획 등록 경로, 구계획 재사용·전환 판정, 현재 작업 범위.
- BINDING.json: 실제 Read/Write, 선행 근거와 해시 종류, source_spec와 bound_task의 구분, 현지 절차 경로.
- SV5_01_APPROVAL_BASELINE_RESULT.md: 수행·검증·상태·commit·남은 범위의 최종 보고.

## 완료 기준

- 패키지·INBOX·원본 명세·기존 계획 입력·baseline 바이트 검사 성공.
- 45개 Task ID와 순서가 CSV/JSON/MD에서 일치하며 중복 없음.
- baseline 좌표가 624×416 전체를 정확히 한 번씩 포함하고 타일 종류·장소 ID가 유효함.
- 원본 예시의 PDF/PNG/ZIP/데이터 SHA가 BASELINE_SHA와 일치함.
- 계속 읽는 문서에서 SV5 승인 기준과 45개 계획을 찾아갈 수 있음.
- 기존 완료/Archive/무관한 작업 상태가 보존됨. 실제 게임 구현 변경 없음.
- 실제 normal Apply·Finalize 계약을 따르고, 실행 Task와 Archive의 일치 여부를 실제 바이트로 보고.

## 보고와 종료

INBOX_SHA / SOURCE_SPEC_SHA / BOUND_TASK_SHA / INSTALLED_TASK_SHA / ARCHIVE_TASK_SHA를 각각 이름 붙여 보고한다.
결과 경로·Result SHA·실제 변경 파일·검증 항목·미실행 항목·전후 상태·commit을 보고한다.
현지 소유 commit 절차가 있으면 해당 변경만 처리하고, push는 하지 않는다.
SV5_02 이후는 실행하지 않는다. 다음 상태는 현지 정상 Finalize 결과를 그대로 보고한다.
동일 SV5_01이 이미 완료됐다면 기존 증거와 입력 일치 여부를 확인하고 중복 등록/재실행하지 않는다.
계약 충돌로 실행을 못 하면 단계·expected/actual·현재 소유·보존 상태를 적는다. 원본 기대 SHA를 바꿔 숨기지 않는다.
