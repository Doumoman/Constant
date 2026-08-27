# MAP01_06 통합 실행 프롬프트

```text
MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 수행해.
```

수동 실행이 필요한 경우:

```text
MapDesign/MCP/00_MCP_ENTRYPOINT.md부터 읽고 manifest를 적용한 뒤,
Current Task인 TASKS/MAP01_06_IMPLEMENT_SCALAR_AND_LIST_PARSERS.md 하나만 수행해.

적용 전 Current Task NONE, MAP01_05까지 COMPLETE,
MAP01_06 이후 LOCKED인 205개 exact 상태를 확인해.
MAP01_05의 PK 32/32, validator 29/29, reader 31/31,
schema 23/23, importer 9/9, architecture 10/10,
total 134/134, compile error/warning 0/0을 확인해.

successful validation과 PK index를 gate로 사용해
12개 schema type을 invariant typed value로 파싱해.
Boolean은 exact 0/1, enum은 ordinal AllowedValues,
list는 empty면 empty collection, 아니면 | split 후 component만 trim하고
leading/trailing/doubled pipe와 whitespace-only item을 오류로 보고해.

오류를 exact source field 위치로 전부 수집하고
하나라도 있으면 parsed records를 publish하지 마.
FK/domain definition/Registry/hash/UI는 구현하지 말고
reader/schema/validator/PK/importer/CSV/asmdef도 수정하지 마.

parser 최소 40개와 전체 targeted 최소 174개를 모두 PASS시키고
compile error 0 / 신규 warning 0을 확인해.
PASS일 때만 finalize하고 MAP01_07은 시작하지 마.
```
