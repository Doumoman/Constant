# SV5_08_FIX01_REG — 정확히 한 번의 보완 Task 등록

현재 사용자의 보완 작업 진행 승인을 이 두 행 등록에 적용한다. 일반 single_task_v1 Apply가 아니다.
07_PATCH_APPLY_RULES §3.8의 별도 contract-change 단계이며 native 규약 파일은 변경하지 않는다.
Status의 SV5_08 COMPLETE 다음에 FIX01 LOCKED, Master의08 다음에08.F1 한 행만 추가한다.
290=251/0/39 → 등록291=251/0/40 → Apply291=251/1/39 → Finalize291=252/0/39.
기존08은 COMPLETE,09는 LOCKED, 등록 중 Current NONE이다. 기존 행/순서/Last 필드는 그대로다.

1. 실제 Unity 프로젝트 루트에서 native00/01/05/07/08/APPLY 문서를 읽는다.
2. tools/PRECHECK.py --mode pre-registration --project-root . --expected-manifest-sha <발급 SHA>를 실행한다.
3. PASS이면 다음 두 명령을 순서대로 실행한다. 실패 시 다음 명령을 실행하지 않는다.

```text
git -c core.autocrlf=false -c core.eol=lf apply --check MapDesign/MCP/INPUTS/SV5_08_FIX01/REGISTRATION.diff
git -c core.autocrlf=false -c core.eol=lf apply MapDesign/MCP/INPUTS/SV5_08_FIX01/REGISTRATION.diff
```

4. 같은 helper --mode stage가 등록 후 두 문서 전체 SHA와 나머지 조건을 다시 검사하고 단일 INBOX MD만 쓴다.
5. native APPLY_PATCH_AND_RUN_CURRENT_TASK.md로 정상 Apply 후 installed Task 하나를 수행한다.

등록 후 정확한 after SHA/LOCKED라면 diff를 재적용하지 않고 stage부터 재개한다.
CURRENT라면 post-readonly 후 installed Task만 재개한다. COMPLETE라면 post-readonly로 확인하고 재실행하지 않는다.
두 등록 파일 중 하나만 바뀌었거나 내용이 달라지면 자동 보정/재등록하지 말고 구체적인 차이를 보고한다.
위 git 옵션은 두 명령에만 적용한다. 저장소 EOL 설정 변경/원본 SHA 수정/restore/reset/clean은 하지 않는다.
등록 두 행과 동봉 INPUTS는 FIX01 최종 소유 commit에 함께 들어간다. 등록만 별도 commit하지 않는다.
helper는 등록 문서/Task 설치/Archive/Status open/Finalize/commit을 쓰지 않는다.
INBOX에 다른 후보가 있으면 경로를 보고하고 보존한다. 후보 삭제/자동 이동으로 조건을 맞추지 않는다.
