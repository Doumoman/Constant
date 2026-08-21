# MAP01_04 통합 실행 프롬프트

```text
MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 수행해.
```

수동 실행이 필요한 경우:

```text
MapDesign/MCP/00_MCP_ENTRYPOINT.md부터 읽고 manifest를 적용한 뒤,
Current Task인 TASKS/MAP01_04_IMPLEMENT_HEADER_AND_FIELD_VALIDATION.md 하나만 수행해.

적용 전 Current Task NONE, MAP01_03까지 COMPLETE,
MAP01_04 이후 LOCKED인 205개 exact 상태를 확인해.
MAP01_03의 reader 31/31, schema 23/23, importer 9/9,
architecture 10/10, total 73/73, compile 0을 확인해.

header missing/unexpected/duplicate/order, field count,
required/default만 exact file/record/field/line/column/offset으로 검증해.
raw/effective value와 UsedDefault를 보존하고 오류 시 row를 publish하지 마.

typed scalar/list, PK/FK, Registry는 구현하지 말고
reader/schema/importer/CSV/asmdef도 수정하지 마.

validator 최소 24개와 전체 targeted 최소 97개를 모두 PASS시키고
compile error 0 / 신규 warning 0을 확인해.
PASS일 때만 finalize하고 MAP01_05는 시작하지 마.
```
