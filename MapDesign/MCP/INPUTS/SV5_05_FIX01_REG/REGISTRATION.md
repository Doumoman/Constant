# SV5_05_FIX01_REG — 일회성 등록·INBOX 원본 보관 패치

PATCH_ID: SV5_05_FIX01_REG
KIND: EXPLICIT_USER_APPROVED_ONE_TIME_REGISTRATION_CHANGE
DELIVERY_STATE: PREPARED_NOT_APPLIED
TARGET_TASK: SV5_05_FIX01
이 패치는 일반 single_task_v1 실행 Task가 아니다. 등록을 위해 다른 Task를 추가하지 않는다.
동봉 JSON의 정확한 변경과 보관 이동을 사용자가 승인한 뒤에만 REGISTER.py --apply-registration을 실행한다.
--approval 문자열은 해당 사용자 지시의 식별자이며 그 자체로 승인 권한을 만들어 주지 않는다.

## 현지 근거와 적용 범위

07_PATCH_APPLY_RULES.md §3.8은 미등록 ID에 별도 contract-change patch를 요구한다.
이번 REG_CONTEXT에는 독립된 등록 절차/스크립트가 없다고 명시돼 있다.
00_MCP_ENTRYPOINT.md §3은 현재 사용자 지시를 우선한다.
SV5_01의 실제 Task/BINDING에는 사용자 승인에 따른 일회성 사전 등록 경계가 기록돼 있다.
이번도 명시 승인된 다음 delta만 수행하고 일반 Phase A의 기존 검사는 그대로 사용한다.
새로운 상시 등록 예외·candidate 선별 규칙·승인 우회·자동 해제 정책을 만들지 않는다.
실제 00/01/05/07/08/APPLY 규약 파일과 기존 SV5 프로토콜은 이 등록 단계에서 수정하지 않는다.

## 정확한 등록 delta

- 현재 Status: 285행, 243 COMPLETE / 0 CURRENT / 42 LOCKED, Current NONE.
- Status의 SV5_06 행 바로 앞에 `| SV5_05_FIX01 | LOCKED |` 한 행만 삽입한다.
- Master의 SV5_06 행 바로 앞에 `| 05.F1 | SV5_05_FIX01 | LOCKED |` 한 행만 삽입한다.
- 등록 후 Status: 286행, 243 COMPLETE / 0 CURRENT / 43 LOCKED, Current NONE.
- 원래 SV5_01~45의 ID/상대 순서와 기존 모든 상태·본문·역사적 수치는 그대로 보존한다.
- SV5_05는 COMPLETE, SV5_06은 LOCKED다. 등록만으로 어떤 Task도 CURRENT/COMPLETE가 되지 않는다.
- 215-row라는 과거 규약 문구를 전체 215개로 축소하거나 다른 ID 자동 등록에 사용하지 않는다.
  이 사용자 승인 등록은 기존 정상 등록된 285개에 위 ID 하나만 더하는 경계다.

각 문서 전체 before SHA, 정확한 삽입 anchor/bytes, after SHA는 REGISTRATION.json에 고정돼 있다.
바이트 차이 미리보기는 REGISTRATION.diff에 있다. 입력 불일치를 자동 rebase하거나 기대 SHA를 수정하지 않는다.

## INBOX 원본 12개 보관과 실행 MD 준비

07의 후보 판별은 immediate-child *.md 및 미적용 legacy directory다. README도 후보다.
이 패치는 REGISTRATION.json에 실명/기존 SHA로 열거한 MD 12개만 보관 이동한다.
목적지는 MCP/GENERATED/SV5_05_FIX01_REG/INBOX_ORIGINALS/<기존이름>이다.
먼저 원본 bytes를 복사하고 SHA/size를 확인한 다음 원래 위치에서 제거해 보관 이동을 완료한다.
이 보관 폴더는 MCP_ARCHIVE가 아니며 Task 적용/완료 증거로 사용하지 않는다.
기존 Task/Archive/Result는 그대로 두고 INBOX의 오래된 동일 바이트 사본도 보관한다.
새 실행 MD를 MCP_INBOX/SV5_05_FIX01.md 한 개로 준비한다. 정확한 single_task_v1 metadata와 전체 Task body를 포함한다.
그 bytes/SHA는 이 패키지의 SV5_05_FIX01.md와 동일해야 한다.
다른 후보·legacy directory·원본 SHA 차이·보관 경로 충돌이 있으면 이동 전에 중단한다.
설치 Task/Archive/Result는 REGISTER.py가 생성하지 않는다. 그 생성은 정상 Apply/Execution이 담당한다.

## 옮겨진 과거 입력의 검증

기존 INPUTS/SV5_05_FIX01/VERIFY.py와 FILES.json은 원본 INBOX 경로를 직접 검사한다.
원본 이동 후에는 그 검증기의 역사적 경로 가정을 새 등록 패치의 검증 방식으로 보완한다.
기존 verifier·manifest·source spec·expected SHA는 수정하지 않는다.
REGISTER.py는 원래 FILES.json SHA와 그 모든 entry bytes를 검사한다.
그중 REGISTRATION.json에 실명/SHA로 고정된 이동 entry만 INBOX_ORIGINALS에서 같은 SHA로 읽는다.
경로 대응표는 임의 fallback이 아니다. 이 패치에서 실제 수행하고 검증한 12개 이동에 한정한다.
기존 SOURCE_LOCK 46개도 유지하며, 구현 후에는 원래 PRE_APPLY 허용 source만 이전 SHA 검사에서 제외한다.
새 bound Task는 이 검증 방식을 읽고 실행한다. 다른 기술 요구 C01~C06/T01~T07은 원래 계약을 유지한다.
과거 패키지를 역사적으로 감사할 때도 보관 위치와 원래 SHA를 사용한다. INBOX에 원본 사본을 다시 깔지 않는다.

## 실행·복구·commit 경계

REGISTER.py --check는 read-only다. 전체 preflight가 통과해야 --apply-registration이 동작한다.
적용 직전 파일을 다시 대조하고 기록한 소유 변경만 수행한다. 일반 오류 시 그 변경만 되돌린다.
전원 종료/외부 동시 변경 등으로 TRANSACTION.json이 남으면 자동 재개하지 않는다. journal·보관 bytes·실제 diff를 검토한다.
완료 receipt가 있는 재실행은 같은 보관 원본/등록/Task 바이트를 검증하고 쓰기 없이 종료한다.
helper는 Unity·정상 Task Apply·Finalize·git commit·push를 수행하지 않는다.
등록 후 --verify-staged가 통과하면 기존 APPLY_PATCH_AND_RUN_CURRENT_TASK.md로 FIX01 하나만 실행할 수 있다.
등록 변경은 FIX01의 소유 경계에 포함해 정상 Phase D atomic commit에 함께 넣는다. 등록만 별도 임의 commit하지 않는다.
INBOX 원본 이동 12개의 source/destination만 이번에 명시 승인된 이동 소유다. 다른 uncommitted 파일은 포함하지 않는다.
기존 immutable INPUTS를 통째로 다시 stage하거나 무관한 변경을 revert하지 않는다.

## 보고와 이후

REGISTRATION_RECEIPT.json/REGISTRATION_RECORD.md는 등록 증거이며 Task PASS Result가 아니다.
helper 종료 상태는 REGISTERED_LOCKED_TASK_NOT_APPLIED다.
정상 FIX01 구현 후에만 별도 PASS Result/Finalize를 수행한다.
FIX01의 최종 검토 ZIP에 이번 등록 receipt/record/실제 Master·Status SHA도 포함한다.
다음 SV5_06은 FIX01 완료 증거를 선행으로 확인해 별도 바인딩한다. 이번에 실행하지 않는다.
향후 새 source-spec/README는 INPUTS에 두고 INBOX에는 바인딩된 실행 MD 하나만 준비한다.
