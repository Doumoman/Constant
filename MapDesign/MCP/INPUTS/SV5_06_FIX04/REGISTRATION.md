# SV5_06_FIX04_REG - 별도 1회 등록 패치

사용자가 동봉 실행 지시로 승인한 이 등록 변경은 normal `single_task_v1` Task가 아니다.
현행 native 규약에서 미등록 ID에 요구하는 별도 contract-change 단계이며 기존 규약 자체는 수정하지 않는다.
FIX03은 COMPLETE로 보존하고 새 FIX04 행만 등록한다.

## 허용 변경

- Status의 FIX03 COMPLETE 다음에 `| SV5_06_FIX04 | LOCKED |` 한 행 추가.
- Master의 06.F3 다음에 `| 06.F4 | SV5_06_FIX04 | LOCKED |` 한 행 추가.
- 289행 248 COMPLETE / 0 CURRENT / 41 LOCKED에서 290행 248 / 0 / 42로 바뀐다.
- Current NONE, 기존 행, Last Completed, 다음 SV5_07은 바꾸지 않는다.
- INPUTS/SV5_06_FIX04의 신규 패키지와 이 두 등록 행은 이번 보완 작업의 명시적 소유다.

## 적용 순서

1. 실제 Unity 프로젝트 루트와 native entry/change/apply/finalize 규약을 확인한다.
2. `PRECHECK.py --mode pre-registration --project-root . --expected-manifest-sha <SHA>`를 실행한다.
3. PASS일 때 프로젝트 루트에서 다음 두 명령을 순서대로 실행한다. 실패하면 다음 단계로 가지 않는다.

```text
git -c core.autocrlf=false -c core.eol=lf apply --check MapDesign/MCP/INPUTS/SV5_06_FIX04/REGISTRATION.diff
git -c core.autocrlf=false -c core.eol=lf apply MapDesign/MCP/INPUTS/SV5_06_FIX04/REGISTRATION.diff
```

4. `PRECHECK.py --mode pre-stage`로 exact after SHA와 선행 조건을 확인한다.
5. PASS 후 `--mode stage`를 실행한다. helper는 bound Task MD 한 개만 INBOX에 만든다.
6. native `APPLY_PATCH_AND_RUN_CURRENT_TASK.md`를 통해 Apply와 installed Task 수행을 시작한다.

위 `git -c` 옵션은 두 diff 명령에만 적용한다. 저장소/전역 줄바꿈 설정을 바꾸거나 SHA를 보정하지 않는다.
부분 적용이면 이번 시도가 쓴 두 파일만 exact before bytes로 복구하고 중단한다.
무관한 dirty 변경이나 다른 INBOX 후보를 자동 정리하지 않는다.
이미 exact after SHA/LOCKED면 diff를 다시 적용하지 않고 pre-stage부터 재개한다.
CURRENT이면 post-readonly 확인 뒤 installed Task만 재개하고 COMPLETE이면 재실행하지 않는다.
PRECHECK는 Status/Master를 쓰지 않는다. 등록을 normal Apply의 자동 unlock으로 바꾸지 않는다.
