# MAP01_05 통합 실행 프롬프트

```text
MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 수행해.
```

수동 실행이 필요한 경우:

```text
MapDesign/MCP/00_MCP_ENTRYPOINT.md부터 읽고 manifest를 적용한 뒤,
Current Task인 TASKS/MAP01_05_IMPLEMENT_PRIMARY_KEY_INDEX.md 하나만 수행해.

적용 전 Current Task NONE, MAP01_04까지 COMPLETE,
MAP01_05 이후 LOCKED인 205개 exact 상태를 확인해.
MAP01_04의 validator 29/29, reader 31/31, schema 23/23,
importer 9/9, architecture 10/10, total 102/102,
compile error/warning 0/0을 확인해.

validated EffectiveValue를 PrimaryKeyOrder로 수집하고
single/composite key를 delimiter join 없는 structural vector로 비교해.
duplicate의 첫 행과 모든 후속 행의 exact source 위치를 보고하고,
오류가 하나라도 있으면 usable/partial index를 publish하지 마.

typed scalar/list, FK, domain definition, Registry는 구현하지 말고
reader/schema/validator/importer/CSV/asmdef도 수정하지 마.

PK index 최소 24개와 전체 targeted 최소 126개를 모두 PASS시키고
compile error 0 / 신규 warning 0을 확인해.
PASS일 때만 finalize하고 MAP01_06은 시작하지 마.
```
