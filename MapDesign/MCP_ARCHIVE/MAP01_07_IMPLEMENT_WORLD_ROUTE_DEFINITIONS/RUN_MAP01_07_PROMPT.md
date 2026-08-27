# MAP01_07 통합 실행 프롬프트

```text
MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 수행해.
```

수동 실행이 필요한 경우:

```text
MapDesign/MCP/00_MCP_ENTRYPOINT.md부터 읽고 manifest를 적용한 뒤,
Current Task인 TASKS/MAP01_07_IMPLEMENT_WORLD_ROUTE_DEFINITIONS.md 하나만 수행해.

적용 전 Current Task NONE, MAP01_06까지 COMPLETE,
MAP01_07 이후 LOCKED인 205개 exact 상태를 확인해.
MAP01_06의 parser 97/97, PK 32/32, validator 29/29,
reader 31/31, schema 23/23, importer 9/9, architecture 10/10,
targeted 231/231, full EditMode 274/274,
compile error/warning 0/0을 확인해.

Task에 적힌 exact 13개 source와 column inventory만 사용해
13개 immutable typed row definition과 WorldRouteDefinitionSet을 만들어.
모든 column, optional empty, inactive row, parsed source record를 보존하고
single/composite collection을 CSV row order와 무관하게 안정 정렬해.

FK는 string ID로만 유지하고 resolve하지 마.
world/route/recipe domain invariant, 16-cell, path/socket compatibility,
later definitions, Registry/hash/UI를 구현하지 마.
reader/schema/validator/PK/parser/importer/CSV/asmdef도 수정하지 마.

definition 최소 44개, targeted 최소 275개,
full EditMode 최소 318개를 모두 PASS시키고
compile error 0 / 신규 warning 0을 확인해.
PASS일 때만 finalize하고 MAP01_08은 시작하지 마.
```
