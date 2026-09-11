# SV5_08_FIX02 — FIX01 완료 사후검증 증거 정정

## 판정

SV5_08_FIX01의 게임 코드와 데이터 결과는 그대로 유효하다.

- commit `875122b716952827c44c363e8a2b0621905e66f1`, parent `953b833cb22904721f9e8687283a65d5bae64cb7`의 manifest 149개 blob은 Review ZIP과 일치한다.
- focused XML은 102/102 PASS다.
- 독립 길이 재검사는 default 217개, repeat 234개 연결을 모두 확인하며 최대24/초과0이다.
- old FIX01 Result SHA-256은 `aca0c0e2f726bf928b771ef6ebb5e7d58db6ec5b61b71b4f48c2aaa2136c46bb`다.

그러나 Result는 `TASK_ID: SV5_08_FIX01`만 기록했고 설치된 FIX01 Task 및 immutable PRECHECK는
Finalize 뒤 정확한 `TASK: SV5_08_FIX01` 독립 행을 요구한다. 따라서 FIX01 Result에 적힌
“Finalize 후 post-readonly PASS” 주장은 완료 상태에서 재현되지 않는다.

## 보완 원칙

과거 commit/Result/helper를 고쳐 쓰지 않는다. FIX02는 다음만 수행한다.

1. 과거 bytes와 상충하는 주장 자체를 독립 audit로 고정한다.
2. 코드·타일·102-test evidence가 변하지 않았음을 commit blob/raw SHA로 확인한다.
3. FIX02 Result에는 정확한 `TASK: SV5_08_FIX02`, `TASK_ID: SV5_08_FIX02`, `STATUS: PASS`를 모두 기록한다.
4. native Finalize 뒤 FIX02 PRECHECK post-readonly를 실제로 실행하여 COMPLETE 상태의 Result/audit를 검증한다.
5. 후속 SV5_09는 FIX02 Result와 Task를 선행으로 바인딩한다.

이 보완은 Unity 로직 수정, 시험 재실행, 레거시 이동·삭제, 과거 Result 수정 권한이 아니다.
