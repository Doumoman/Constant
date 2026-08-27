# MAP01_03 통합 실행 프롬프트

```text
MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 수행해.
```

수동 실행이 필요한 경우:

```text
MapDesign/MCP/00_MCP_ENTRYPOINT.md부터 읽어.
manifest를 검증해 패치를 적용한 뒤 Current Task인
TASKS/MAP01_03_IMPLEMENT_RFC4180_READER.md 하나만 수행해.

적용 전 Current Task는 NONE, MAP01_02까지 COMPLETE,
MAP01_03 이후 LOCKED인 205개 exact 상태여야 해.
MAP01_02 Result의 60 files/679 columns, 30/30 schema,
10/10 architecture, 40/40 targeted, compile 0을 확인해.

UTF-8 comma/quote/escaped quote/CRLF/LF/multiline/BOM과
exact physical line/column/record/field 위치를 읽는 reader만 구현해.
dictionary importer는 기존 comma split을 제거하고 이 reader를 사용하게 해.

header/required/default validation, PK index, scalar/list parser,
FK resolution, Registry는 구현하지 마. 기존 CSV/schema builder/asmdef도 수정하지 마.

reader tests 최소 20, schema catalog 23, importer 최소 7,
architecture 10을 포함해 targeted 최소 60개를 전부 PASS시키고
compile error 0 / 신규 warning 0을 확인해.
PASS일 때만 finalize하고 MAP01_04는 시작하지 마.
```
