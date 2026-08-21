# MAP00_08 통합 실행 프롬프트

```text
MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 수행해.
```

수동 실행이 필요한 경우:

```text
MapDesign/MCP/00_MCP_ENTRYPOINT.md부터 읽어.
PATCH_MANIFEST.md의 사전 조건과 copy_operations를 검증해 패치를 적용한 뒤,
06_IMPLEMENTATION_STATUS.md의 Current Task인
TASKS/MAP00_08_CREATE_COORDINATE_TESTS.md 하나만 수행해.

CoordinateConversionBoundaryTests EditMode C# 1개와 meta, Result만 만들어.
Runtime 상수/좌표/utility, 기존 test, debug view, CSV, asmdef, Scene, Prefab은 수정하거나 만들지 마.

신규 exhaustive 테스트 8개와 기존 utility 10개, value type 12개,
constant 6개, architecture 10개가 모두 PASS일 때만 STATUS FINALIZE를 수행하고
다음 TASK는 시작하지 마.
```
