# SV5_06_FIX02_REG - 별도 1회 등록 패치

사용자가 동봉 실행 지시로 승인한 이 등록 변경은 normal single_task_v1 Task가 아니다.
07_PATCH_APPLY_RULES의 미등록 ID에 필요한 별도 contract-change 단계다. 기존 native 규약은 수정하지 않는다.
이전 FIX01 등록 승인과 구별한다. FIX01은 COMPLETE로 보존하고 새 FIX02 행만 등록한다.

## 허용 변경

- Status의 FIX01 COMPLETE 다음에 FIX02 LOCKED 한 행 추가.
- Master의 06.F1 다음에 06.F2 / FIX02 / LOCKED 한 행 추가.
- 287행 246 COMPLETE / 0 CURRENT / 41 LOCKED에서 288행 246 / 0 / 42로 바뀐다.
- Current NONE, 기존 행, baseline, Last Completed, 다음 SV5_07은 바꾸지 않는다.
- INPUTS/SV5_06_FIX02의 신규 패키지와 이 두 등록 행은 이번 보완 작업의 명시적 소유다.

## 적용 순서

1. 실제 Unity 프로젝트 루트와 native entry/change/apply/finalize 규약을 확인한다.
2. PRECHECK.py --mode pre-registration --project-root . --expected-manifest-sha <전달받은 SHA>를 실행한다.
3. PASS일 때 아래 명령을 Unity 루트에서 순서대로 실행한다. 각 명령 실패 시 다음 명령을 실행하지 않는다.

```text
git -c core.autocrlf=false -c core.eol=lf apply --check MapDesign/MCP/INPUTS/SV5_06_FIX02/REGISTRATION.diff
git -c core.autocrlf=false -c core.eol=lf apply MapDesign/MCP/INPUTS/SV5_06_FIX02/REGISTRATION.diff
```

4. PRECHECK.py --mode pre-stage로 두 파일의 after SHA와 전체 선행 조건을 검증한다.
5. PASS 후 --mode stage를 실행한다. helper는 bound Task MD 한 개만 INBOX에 만든다.
6. 실제 APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 통해 native Apply와 installed Task 수행을 시작한다.

Git -c 옵션은 해당 명령에만 적용한다. 전역/저장소 설정이나 파일 줄바꿈을 변환하지 않는다.
이전 등록 시도에서 겪은 자동 CRLF 출력을 피하면서 diff의 정확한 after bytes를 유지하기 위한 설정이다.
두 파일은 하나의 등록 변경이다. 부분 적용이면 이번 시도가 쓴 변경만 원래 before bytes로 복구해 확인하고 중단한다.
무관한 dirty 변경이나 다른 INBOX 후보를 정리 대상으로 삼지 않는다. expected SHA를 바꾸지 않는다.
이미 정확한 after SHA/LOCKED 상태이면 diff를 다시 적용하지 않고 pre-stage부터 재개한다.
CURRENT이면 post-readonly로 검증 후 installed Task만 재개한다. COMPLETE이면 재실행하지 않는다.
등록/정상 Apply/Finalize의 전후 상태와 SHA는 GENERATED/SV5_06_FIX02/BINDING.json에 기록한다.
PRECHECK는 Status/Master를 쓰지 않는다. 등록 단계를 정상 Task의 자동 unlock으로 대체하지 않는다.
