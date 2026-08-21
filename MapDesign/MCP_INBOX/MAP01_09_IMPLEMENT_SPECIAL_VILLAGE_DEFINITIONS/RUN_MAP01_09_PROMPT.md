# MAP01_09 통합 실행 프롬프트

```text
MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 수행해.
```

수동 실행이 필요하면:

```text
MapDesign/MCP/00_MCP_ENTRYPOINT.md부터 읽고 manifest를 적용한 뒤,
Current Task인 TASKS/MAP01_09_IMPLEMENT_SPECIAL_VILLAGE_DEFINITIONS.md 하나만 수행해.

적용 전 Current Task NONE, MAP01_08까지 COMPLETE,
MAP01_09 이후 LOCKED인 205개 exact 상태와 MAP01_08 Result PASS,
definition 36/36, targeted 326/326, full 369/369, error/warning 0/0을 확인해.

Task의 exact 12개 source/column만 사용해 12개 immutable typed row definition과
SpecialVillageDefinitionSet을 만들고 optional empty, inactive, list order/duplicate,
source record를 보존해. FK resolve와 domain validation, placement, Registry는 하지 마.

신규 최소 48개, targeted 최소 374개, full EditMode 최소 417개를
모두 PASS시키고 compile error 0 / 신규 warning 0을 확인해.
PASS일 때만 finalize하고 MAP01_10은 시작하지 마.
```
