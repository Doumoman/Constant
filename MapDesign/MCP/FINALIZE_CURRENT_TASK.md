# FINALIZE CURRENT TASK ONLY

현재 TASK는 이미 수행되었고 Result가 존재하지만
`06_IMPLEMENTATION_STATUS.md`가 아직 CURRENT일 때 사용하는 복구 명령서다.

1. `MapDesign/MCP/00_MCP_ENTRYPOINT.md` 읽기
2. `MapDesign/MCP/08_STATUS_FINALIZE_RULES.md` 읽기
3. `MapDesign/MCP/06_IMPLEMENTATION_STATUS.md` 확인
4. Current Task와 대응 Result 확인
5. Result가 PASS라면 STATUS FINALIZE만 수행
6. 구현 코드, Result, Assets, CSV는 수정하지 않음
7. STATUS FINALIZE가 PASS하면 `05_CHANGE_CONTROL_RULES.md`에 따라 해당 Task 변경을 commit
8. 자동 push 금지
9. 다음 TASK 시작 금지

완료 후 STATUS FINALIZE와 commit SHA를 함께 보고하고 종료.
