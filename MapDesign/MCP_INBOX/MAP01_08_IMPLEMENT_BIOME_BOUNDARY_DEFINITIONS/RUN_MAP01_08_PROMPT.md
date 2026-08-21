# MAP01_08 통합 실행 프롬프트

```text
MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 수행해.
```

수동 실행이 필요한 경우:

```text
MapDesign/MCP/00_MCP_ENTRYPOINT.md부터 읽고 manifest를 적용한 뒤,
Current Task인 TASKS/MAP01_08_IMPLEMENT_BIOME_BOUNDARY_DEFINITIONS.md 하나만 수행해.

적용 전 Current Task NONE, MAP01_07까지 COMPLETE,
MAP01_08 이후 LOCKED인 205개 exact 상태를 확인해.
MAP01_07의 definitions 59/59, parser 97/97, PK 32/32,
validator 29/29, reader 31/31, schema 23/23,
importer 9/9, architecture 10/10, targeted 290/290,
full EditMode 333/333, compile error/warning 0/0을 확인해.

Task의 exact 5개 source와 column inventory만 사용해
5개 immutable typed row definition과 BiomeBoundaryDefinitionSet을 만들어.
모든 column, optional empty, inactive row, list order/duplicate,
parsed source record를 보존하고 ordinal stable ordering을 유지해.

biome A/B 방향을 canonicalize하거나 reverse pair를 만들지 마.
FK resolve, weight/profile 대응, min/max/patch/boundary domain 검증,
candidate/transform 선택, later definitions, Registry/hash/UI를 구현하지 마.
기존 reader/schema/validator/PK/parser/world-route/importer/CSV/asmdef도 수정하지 마.

definition 최소 36개, targeted 최소 326개,
full EditMode 최소 369개를 모두 PASS시키고
compile error 0 / 신규 warning 0을 확인해.
PASS일 때만 finalize하고 MAP01_09는 시작하지 마.
```
