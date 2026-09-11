# SV5_08_FIX02 — FIX01 Finalize 증거 감사

## 판정

SV5_08_FIX01의 게임 구현과 생성·시험 증거는 유효하다. FIX01 commit `875122b716952827c44c363e8a2b0621905e66f1`의 149개 blob, focused 102/102 PASS XML, default 217개·repeat 234개 연결의 양 끝점 포함 최대 24셀/초과 0 결과가 다시 확인됐다.

다만 FIX01 Result에는 `TASK_ID: SV5_08_FIX01`과 `STATUS: PASS`만 있고, immutable FIX01 PRECHECK가 COMPLETE 상태에서 요구하는 독립 행 `TASK: SV5_08_FIX01`이 없다. 따라서 FIX01 Result가 주장한 **Finalize 뒤 post-readonly PASS**는 재현할 수 없다. 이 결함은 게임 코드·타일·시험 결과의 실패가 아니라 완료 사후검증 marker 형식의 결함이다.

## 정정 방식

- 과거 FIX01 Result, Task, PRECHECK, BINDING, Generated, 코드, 테스트를 수정하지 않는다.
- `GENERATED/SV5_08_FIX02/finalize_audit.json`이 과거 raw bytes와 commit blob을 읽기 전용으로 검증한다.
- 후속 체인은 과거 Result를 소급 변경하는 대신 독립적인 FIX02 Task/Result/audit를 선행 증거로 사용한다.
- FIX02 Result는 `TASK`, `TASK_ID`, `STATUS` 세 marker를 각각 정확한 독립 행으로 기록한다.
- Unity는 이 감사 Task에서 다시 실행하지 않는다.

이 감사로 새 게임 기능, 합성 지형 또는 Player 검증을 승격하지 않는다. `ComposedGeometryReady=false`, `PlayerVerified=false`이며 `SV5_09_LOOPS`는 LOCKED로 유지한다.
