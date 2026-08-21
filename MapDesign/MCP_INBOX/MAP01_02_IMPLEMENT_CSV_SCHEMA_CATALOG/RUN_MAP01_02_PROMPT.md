# MAP01_02 통합 실행 프롬프트

```text
MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 수행해.
```

수동 실행이 필요한 경우:

```text
MapDesign/MCP/00_MCP_ENTRYPOINT.md부터 읽어.
PATCH_MANIFEST.md의 사전 조건과 copy_operations를 검증해 패치를 적용한 뒤,
06_IMPLEMENTATION_STATUS.md의 Current Task인
TASKS/MAP01_02_IMPLEMENT_CSV_SCHEMA_CATALOG.md 하나만 수행해.

적용 전 Current Task는 NONE이고, 205개 개별 상태 행에서
MAP00_01~10과 MAP01_01은 COMPLETE, MAP01_02 이후는 LOCKED여야 해.
MAP01_01 Result의 STATUS: PASS, Authoring CSV/meta 50/50,
hash/BOM 50/50, architecture tests 10/10, compile error 0을 확인해.

설치된 CSV_DATA_DICTIONARY.csv의 exact 10-column header와
679 rows / 60 files baseline을 immutable schema catalog로 읽어.
Runtime model/builder 8개, Editor bootstrap importer 1개,
Runtime/Editor EditMode test 2개만 Task WRITE ALLOWLIST에 따라 구현해.

일반 CSV RFC4180 reader, 실제 데이터 행 validation, PK index,
scalar/list parser, FK resolution, StaticDataRegistry는 구현하지 마.
CSV/asmdef/Scene/Prefab/Package/ProjectSettings도 수정하지 마.

새 schema tests 최소 15개와 기존 architecture 10개를 모두 통과시키고,
compile error 0 / 관련 신규 warning 0을 확인해.
Result가 PASS일 때만 STATUS FINALIZE를 수행하고 MAP01_03은 시작하지 마.
```
