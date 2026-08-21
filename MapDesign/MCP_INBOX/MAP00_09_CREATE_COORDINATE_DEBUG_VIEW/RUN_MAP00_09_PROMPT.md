# MAP00_09 통합 실행 프롬프트

```text
MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 수행해.
```

수동 실행이 필요한 경우:

```text
MapDesign/MCP/00_MCP_ENTRYPOINT.md부터 읽어.
PATCH_MANIFEST.md의 사전 조건과 copy_operations를 검증해 패치를 적용한 뒤,
06_IMPLEMENTATION_STATUS.md의 Current Task인
TASKS/MAP00_09_CREATE_COORDINATE_DEBUG_VIEW.md 하나만 수행해.

WorldCoordinateDebugDisplay Editor Preview C# 1개,
WorldCoordinateDebugWindow EditorWindow C# 1개,
Editor EditMode test C# 1개와 각 meta, Result만 만들어.
Runtime, 기존 test, Scene/Prefab, CSV, asmdef는 수정하지 마.

신규 Editor test 7개와 기존 coordinate/architecture test 46개가 모두 PASS하고,
WorldGen/Coordinates 창과 Scene overlay 시각 검증이 PASS일 때만
STATUS FINALIZE를 수행한 뒤 다음 TASK는 시작하지 마.
```
