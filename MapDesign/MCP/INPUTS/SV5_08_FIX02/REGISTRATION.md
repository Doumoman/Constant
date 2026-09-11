# SV5_08_FIX02_REG — 완료 증거 형식 보완 Task의 정확한 1회 등록

이 단계는 normal `single_task_v1` Apply가 아니라 현행 규약이 미등록 ID에 요구하는 별도 contract-change다.
현재 사용자가 Result/Review를 주면 검토와 다음 보완 패키지를 바로 제공하라고 승인한 범위에서 FIX02 한 개만 등록한다.

- Status의 FIX01 COMPLETE 바로 다음에 `| SV5_08_FIX02 | LOCKED |` 한 행을 추가한다.
- Master의 08.F1 바로 다음에 `| 08.F2 | SV5_08_FIX02 | LOCKED |` 한 행을 추가한다.
- 등록 중 FIX01 COMPLETE, SV5_09 LOCKED, Current NONE과 나머지 모든 행·본문을 보존한다.
- 291=252 COMPLETE/0 CURRENT/39 LOCKED에서 292=252/0/40으로 바뀐다.
- 등록만으로 Task를 CURRENT/COMPLETE로 만들거나 Archive/Result를 만들지 않는다.

실제 Unity 프로젝트 루트에서 native 00/01/05/07/08/APPLY 규약을 먼저 읽는다.
`tools/PRECHECK.py --mode pre-registration`이 PASS일 때만 아래 두 명령을 순서대로 실행한다.

```text
git -c core.autocrlf=false -c core.eol=lf apply --check MapDesign/MCP/INPUTS/SV5_08_FIX02/REGISTRATION.diff
git -c core.autocrlf=false -c core.eol=lf apply MapDesign/MCP/INPUTS/SV5_08_FIX02/REGISTRATION.diff
```

그 뒤 같은 helper의 `--mode stage`로 전체 after SHA를 확인하고 단일 INBOX MD를 준비한다.
정상 native Apply만 Task 설치·Status open·Archive를 수행한다.

이미 두 문서가 exact after SHA이고 FIX02가 LOCKED면 diff를 재적용하지 않고 stage부터 재개한다.
CURRENT면 stage/등록을 반복하지 않고 post-readonly 뒤 installed Task만 재개한다.
COMPLETE면 post-readonly로 확인하고 재실행하지 않는다.
한 문서만 적용됐거나 bytes가 다르면 자동 수정·rebase·restore하지 말고 BLOCKED로 보고한다.
INBOX의 다른 후보와 무관한 dirty는 보존한다. 후보 삭제로 개수를 맞추지 않는다.
등록 두 행과 신규 INPUTS는 FIX02 최종 atomic commit 소유다. 등록만 따로 commit하거나 push하지 않는다.
