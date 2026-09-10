# SV5_06_FIX01_REG - 별도 1회 등록 패치

이 문서는 정상 single_task_v1 Apply 입력이 아니다. 미등록 보완 ID를 등록하기 위한 별도 contract-change 패치다.
현재 06은 COMPLETE이고 07은 LOCKED다. 기존06을 재개방하거나07의 작업 의미를 바꾸지 않는다.
사용자가 동봉 실행 지시로 이 등록 패치를 승인한 뒤에만 수행한다. 과거05_FIX01 등록 승인을 자동 재사용하지 않는다.

## 정확한 변경

REGISTRATION.diff는 Status에 SV5_06_FIX01 LOCKED 한 행, Master에 06.F1 한 행만 넣는다.
기존 행/설명/날짜/Current/Last Completed/native 규칙은 수정하지 않는다.
286행 245C/0CURRENT/41L -> 287행 245C/0CURRENT/42L. 등록만으로 CURRENT가 되지 않는다.

## 순서

1. 실제 MCP entry/locked/apply/change/finalize 규약과 REGISTRATION.json을 읽는다.
2. PRECHECK.py --mode pre-registration으로 package/SHA/원본상태/선행실제commit/dirty충돌/INBOX 0개를 검증한다.
3. REGISTRATION.diff를 git apply --check로 검사하고 이 두 파일에만 적용한다. Unity root가 repository 하위이면 git -C를 Unity root로 지정한다.
4. 적용 뒤 두 파일 SHA가 REGISTRATION.json의 after 값과 정확히 같은지 확인한다. 다른 변경은 허용하지 않는다.
5. PRECHECK.py --mode pre-stage가 PASS인 경우에만 --mode stage로 단일 bound MD를 INBOX에 놓는다.
6. 이후부터는 native07의 정상 Apply, 설치Task 실행, Finalize, task-owned atomic commit을 따른다.

두 파일은 한 등록 변경으로 취급한다. 부분 적용/충돌이면 실행을 멈추고, 이번 등록이 추가한 바이트만 원본으로 되돌려 전 상태를 확인한다.
이미 등록된 경우 diff를 다시 적용하지 않는다. 정확한 after SHA와 phase를 검사해 stage 또는 post-readonly 단계로 재개한다.
등록/Task 결과는 GENERATED/SV5_06_FIX01/BINDING.json에 함께 남긴다. 별도 임시 CHECK/README를 프로젝트 root에 쌓지 않는다.
등록 경로 변경과 동봉 INPUTS 신규 파일도 이 명시적 등록 패치 소유로 기록한다. 구현 PASS 후 동일 원자 commit에 포함한다.
INBOX 후보를 삭제/이동해 수를 맞추거나 강제로 Task를 해제하지 않는다. PRECHECK는 Status/Master를 쓰지 않는다.
