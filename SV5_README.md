# SV5 첫 INBOX 패치

목적: 이 SV5 45개 Task 계획으로 작업을 진행한다는 결정을 등록하고 첫 SV5_01_APPROVAL_BASELINE만 수행한다.
패키지 작성 상태: INPUT_PACKAGE_READY. 실제 프로젝트 설치·Apply·Finalize·commit은 아직 수행하지 않았다.

## 배치

ZIP에는 상위 래퍼 폴더 없이 MapDesign/ 폴더와 이 인계문·검증 파일이 들어 있다.
프로젝트 루트(Assets와 MapDesign이 있는 위치)에 추가하면 INBOX 경로가 바로 맞는다.
동일 경로가 이미 있으면 같은 바이트는 재사용하고, 다른 바이트는 보존한 채 현지 정상 버전 전환 절차를 따른다.
기존 MCP_INBOX/SPACE_START.md 및 RMAP Task·Archive·Result를 덮어쓰는 파일은 포함하지 않는다.

## 고정 SHA-256

| 역할 | 파일 | SHA-256 |
|---|---|---|
| 입력 목록 | SV5_FILES.json | 74d4435f8128d44943b54774416329e027a7c8f584e42225c0504e600e2f5456 |
| INBOX | MapDesign/MCP_INBOX/SV5_START.md | 9f440662458d69a74127fa81c4af89e35649dd06426ea19071285f1fd0ddbbef |
| 첫 작업 원본 명세 | MapDesign/MCP/INPUTS/SV5/tasks/SV5_01_APPROVAL_BASELINE.md | 4a4f6a1c9dcb74ae63e4ee30269551f133aabdf256caff5ee4374276d385dcdf |

원본 명세와 현지 바인딩 실행 Task는 별개의 파일이다. 위 SHA를 새 실행 Task의 기대 SHA로 재사용하지 않는다.
기존 SV5 계획 원문은 변경하지 않고 복사했다. 입력의 SHA256.json은 이전 원문의 보존 검사에 사용한다.
승인한 지도 PDF·PNG·원본 ZIP·데이터를 모두 포함했으므로 별도 파일을 다시 찾을 필요가 없다.

## 읽기 전 입력 검사

```text
python SV5_VERIFY.py --manifest-sha 74d4435f8128d44943b54774416329e027a7c8f584e42225c0504e600e2f5456
```

검증기는 파일·45개 목록·전체 좌표·승인 원본의 일치만 읽기 전용으로 검사한다.
PASS_LOCAL_INPUTS는 현지 Task 완료나 Unity·플레이 검증을 의미하지 않는다.

## 실행 범위

INBOX를 읽고 현지 AGENTS/MCP 형식에 맞는 실제 실행 Task를 발행하여 정상 Apply한다.
SV5_01의 승인 근거·baseline·계획 연결·Result만 완료하고 정상 Finalize 결과를 보고한다.
SV5_02 이후·RMAP18/19·게임 코드 수정·전체 Unity 테스트/맵 Bake·push는 이번 범위에 포함하지 않는다.
첫 작업 결과에는 INBOX/source_spec/bound/installed/archive 각각의 SHA와 실제 전후 상태·commit을 구분한다.
