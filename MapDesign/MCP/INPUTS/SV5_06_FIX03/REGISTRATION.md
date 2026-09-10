
# SV5_06_FIX03_REG - 별도 1회 등록 패치

사용자가 동봉 실행 지시로 승인한 이 등록 변경은 normal single_task_v1 Task가 아니다.
07_PATCH_APPLY_RULES의 미등록 ID에 필요한 별도 contract-change 단계다. 기존 native 규약은 수정하지 않는다.
FIX02는 COMPLETE로 보존하고 새 FIX03 행만 등록한다.

## 허용 변경

- Status의 FIX02 COMPLETE 다음에 FIX03 LOCKED 한 행 추가.
- Master의 06.F2 다음에 06.F3 / FIX03 / LOCKED 한 행 추가.
- 288행 247 COMPLETE / 0 CURRENT / 41 LOCKED에서 289행 247 / 0 / 42로 바뀐다.
- Current NONE, 기존 행, Last Completed, 다음 SV5_07은 바꾸지 않는다.
- INPUTS/SV5_06_FIX03의 신규 패키지와 이 두 등록 행은 이번 보완 작업의 명시적 소유다.

## 적용 순서

1. 실제 Unity 프로젝트 루트와 native entry/change/apply/finalize 규약을 확인한다.
2. PRECHECK.py --mode pre-registration --project-root . --expected-manifest-sha <전달받은 SHA>를 실행한다.
3. PASS일 때 Unity 루트에서 아래를 순서대로 실행한다. 실패하면 다음 명령을 실행하지 않는다.

```text
git -c core.autocrlf=false -c core.eol=lf apply --check MapDesign/MCP/INPUTS/SV5_06_FIX03/REGISTRATION.diff
git -c core.autocrlf=false -c core.eol=lf apply MapDesign/MCP/INPUTS/SV5_06_FIX03/REGISTRATION.diff
```

4. PRECHECK.py --mode pre-stage로 after SHA와 전체 선행 조건을 확인한다.
5. PASS 후 --mode stage를 실행한다. helper는 bound Task MD 한 개만 INBOX에 만든다.
6. 실제 APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 통해 native Apply와 installed Task 수행을 시작한다.

Git -c 옵션은 위 두 명령에만 적용한다. 전역/저장소 줄바꿈 설정을 바꾸거나 SHA를 보정하지 않는다.
부분 적용이면 이번 시도가 쓴 두 파일만 exact before bytes로 복구하고 중단한다.
무관한 dirty 변경이나 다른 INBOX 후보를 자동 정리하지 않는다.
이미 exact after SHA/LOCKED면 diff를 다시 적용하지 않고 pre-stage부터 재개한다.
CURRENT이면 post-readonly 확인 뒤 installed Task만 재개하며 COMPLETE이면 재실행하지 않는다.
PRECHECK는 Status/Master를 쓰지 않는다. 등록을 normal Apply의 자동 unlock으로 바꾸지 않는다.
