# SV5_02 R2 경로 정정 패치

기존 SV5_02의 Apply 전 BLOCKED를 해소하기 위한 별도 수정 입력이다. 작업 ID는 SV5_02_RULES다.
ZIP의 MapDesign 폴더와 SV5_02_R2_* 루트 파일을 Assets가 있는 프로젝트 루트에 추가한다.
기존 SV5_02_* 루트 파일·INPUTS/SV5_02·SV5_02_START는 보존하고 새 R2 START로 재시도한다.
새 경로에 파일이 이미 있으면 동일 바이트만 재사용한다. 다른 바이트를 조용히 덮지 않는다.
첫 지도 패키지나 기존 45개 계획은 다시 설치/등록하지 않는다.

## 입력과 SHA

- INBOX: MapDesign/MCP_INBOX/SV5_02_R2_START.md
- INBOX_SHA: 558ca42645c58d3c2ed9bf4e6975caf4e4b1af4bc45f00f9cae65b2c0d68ae94
- SOURCE_SPEC: MapDesign/MCP/INPUTS/SV5_02_R2/SV5_02_RULES.md
- SOURCE_SPEC_SHA: 4b371aaddbfd4f1946fbf32b1da2096e522a7dacef45bd4958f0cb477653a321
- MANIFEST_SHA: a664020b4a0d548bfec05fca6ed20d6751fcea151c4fc98f225566e950b15bdf
- PREVIOUS_RESULT_SHA: 78f9e84bf82b9640c51a5ee4bc2847610a296ebd26e0849706d3ce58601a37b6
- PREVIOUS_INSTALLED_ARCHIVE_SHA: c63acc8cf464ae837d5e83efef9976f5e12f42f1cf8a32f2634ba32cf587d5ed

## 검증 순서

실제 AGENTS/MCP 계약·Status·Current를 먼저 읽는다. 이미 COMPLETE라면 정상 증거를 확인하고 종료한다.
새 실행은 SV5_02 LOCKED / Current NONE인 정상 선행 상태에서만 진행한다.

```text
python -X utf8 SV5_02_R2_VERIFY.py --manifest-sha a664020b4a0d548bfec05fca6ed20d6751fcea151c4fc98f225566e950b15bdf
```

PASS_PACKAGE_ONLY는 패키지 바이트·규칙 추적 검사다. 현지 상태·Finalize·commit 검사가 아니다.
실행자는 정상 계약에서 실제 선행 Result 경로를 찾아 위 명령 뒤에 --local-precheck --map-root "MapDesign" --prior-result "실제 Result 경로"를 붙여 실행한다.
따옴표 안의 Result 경로는 현지 확인값으로 바꾼다. R1/R2 reference 사본은 사용할 수 없다.
MapDesign 경로가 다르면 실제 모듈 루트를 사용하되 MapDesign/MCP를 루트로 주어 오류를 우회하지 않는다.
PASS_LOCAL_BYTES_ONLY도 상태·Finalize·commit을 확인하지 않는다. 해당 검증은 정상 MCP 절차로 별도 수행한다.
보고된 SV5_01 Finalize commit은 9e6a71489fb4881c4112c378f7ca043bbbd85e85다. 현지에서 재확인한다.
SHA 불일치는 바이트/줄바꿈/기대값을 고치지 말고 근거와 함께 보고한다.

## 수행

새 입력 REPAIR.md와 REVISION.json에서 경로 정정과 기존 SHA 불변을 확인한다.
SV5_02_RULES만 별도 single_task_v1로 바인딩해 정상 Apply·수행·Finalize한다.
출력 경로는 MapDesign/MCP/GENERATED/SV5_02이며 규칙 candidate 3개와 프로토콜 추가 블록은 기존과 같다.
02_PROTOCOL의 이전 SHA는 PRE_APPLY_ONLY다. 허용된 추가 작업 뒤에도 이전 SHA를 강제하지 않는다.
각 역할 SHA와 실제 최종 상태·commit을 보고한다. Result 작성/Finalize/commit 시점을 구분한다.
SV5_03 이후·RMAP18/19·게임 코드·Unity·push는 실행하지 않는다.

동봉 CHECK는 이 패키지 제작 환경의 검사 기록이다. fixture 검사는 임시 가상 증거로 검증기 동작을 시험한 것이며 실제 저장소 PASS가 아니다.
