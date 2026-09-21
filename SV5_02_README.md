# SV5_02 규칙 등록 패치

이번 패치는 기존 SV5_01 위에 추가하는 작은 후속 입력이다. 첫 패키지의 지도·기존 입력을 다시 배포하거나 덮지 않는다.
ZIP의 MapDesign 폴더와 SV5_02_* 루트 파일을 Assets가 있는 프로젝트 루트에 추가한다.
동일 파일이 이미 있으면 같은 바이트를 재사용하며, 다른 바이트는 현지 정상 버전 전환 절차로 처리한다.

## 원본 검증

- INBOX: MapDesign/MCP_INBOX/SV5_02_START.md
- INBOX_SHA: c27f7a0d34694b79264cbc5155b68abaf4d9b13e9c09af9829c9715ba5396c46
- SOURCE_SPEC_SHA: 47de1a12a07e7c76ce0156cea451a36129161aa11c17b569fef47389315a52e0
- MANIFEST_SHA: 5c319bffcbdd1cff841a7b2a11a8cf96bcbec6dddfc01b36a16033ecd55ff11d
- PREVIOUS_RESULT_SHA: 78f9e84bf82b9640c51a5ee4bc2847610a296ebd26e0849706d3ce58601a37b6
- PREVIOUS_INSTALLED_ARCHIVE_SHA: c63acc8cf464ae837d5e83efef9976f5e12f42f1cf8a32f2634ba32cf587d5ed

```text
python -X utf8 SV5_02_VERIFY.py --manifest-sha 5c319bffcbdd1cff841a7b2a11a8cf96bcbec6dddfc01b36a16033ecd55ff11d
```

이 호출은 새 패키지와 원문 추적만 검사한다. 현지 SV5_01 완료·commit 확인을 대신하지 않는다.
현지 코드 에이전트는 실제 계약에서 이전 Result 경로를 찾은 뒤 같은 명령에 --local-precheck --prior-result 실제경로를 추가한다.
MapDesign 모듈 경로가 기본값과 다르면 --map-root로 실제 모듈 루트를 지정한다. 가짜 Result 경로나 reference 사본을 넣지 않는다.
이 로컬 검사 모드는 Apply 이전용이다. SV5_02가 허용한 프로토콜 변경 뒤 이전 프로토콜 SHA를 다시 강제하지 않는다.
기존 SV5_VERIFY.py는 동결된 이전 입력이므로 고치지 않는다. 필요시 python -X utf8로 실행한다.

## 선행 확인과 수행

SV5_01 보고서는 PASS이며 bound/installed/archive 일치를 보고한다. 최종 commit SHA는 현지에서 확인한다.
기대 상태는 285 = 239 COMPLETE / 0 CURRENT / 46 LOCKED, SV5_02 LOCKED다. 45개 계획을 다시 추가하지 않는다.
현지 계약과 SOURCE_SPEC을 읽고 SV5_02만 별도 bound single_task_v1으로 발행·정상 Apply한다.
규칙 인덱스·Coverage·ReadSet을 등록하고 기존 프로토콜에서 읽기 경로를 연결한다.
정상 Finalize 뒤 결과·실제 상태·각 역할 SHA·commit을 보고한다. SV5_03 이후·RMAP18/19·게임 코드·Unity·push는 진행하지 않는다.

원본 명세의 SHA와 현지 실행 Task SHA는 역할이 다르다. 선행 Result·설치/Archive SHA도 각자의 필드로 비교한다.
이 패키지는 원문 규칙을 등록하는 작업이며 점프맵 고체·외곽 지형 수정은 후속 13~20번에서 수행한다.
