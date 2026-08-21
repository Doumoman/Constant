# RUN MAP02_02 REMEDIATION v1.2

entrypoint/rules, Master, Status, original MAP02_02 Task, v1.1 addendum, `MAP02_02_IMPLEMENT_DETERMINISTIC_RNG_STREAMS_REMEDIATION_V1_2.md`, 최신 BLOCKED Result를 순서대로 읽어라.

v1.2에 열거된 legacy Editor folder `.meta` exact 6개를 삭제·재작성하지 말고 path, folder meta 형식, GUID uniqueness, pre/post hash를 감사하라. v1.1의 Assets drift 0 gate만 `original 14 + accepted six = exact 20, unexpected 0`, final global Assets meta `2954`로 교체하고 나머지 gate는 유지하라.

Assets/C#/test/CSV/asmdef를 수정하지 않은 채 focused `103/103` → vectors `6/6` each → MAP02_01 `56/56` → targeted `1026/1026` → full `1046/1046` → final refresh/compile/Console → meta/GUID/change-scope 순으로 재검증하라.

모두 PASS면 Result에 v1.2 evidence를 추가하고 `STATUS: PASS`, MAP02_02 COMPLETE, Current Task NONE으로 finalize하라. exact six 이외 drift나 gate 실패가 있으면 파일을 고치지 말고 `BLOCKED`를 유지하라. MAP02_03은 시작하지 마.
