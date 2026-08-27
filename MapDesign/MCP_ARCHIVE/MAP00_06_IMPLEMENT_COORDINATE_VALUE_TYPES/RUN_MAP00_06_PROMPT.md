# MAP00_06 통합 실행 프롬프트

```text
MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 수행해.
```

수동 실행이 필요한 경우:

```text
MapDesign/MCP/00_MCP_ENTRYPOINT.md부터 읽어.
PATCH_MANIFEST.md의 사전 조건과 copy_operations를 검증해 패치를 적용한 뒤,
06_IMPLEMENTATION_STATUS.md의 Current Task인
TASKS/MAP00_06_IMPLEMENT_COORDINATE_VALUE_TYPES.md 하나만 수행해.

WorldTileCoord, SectorCoord, MicroChunkCoord, LocalTileCoord Runtime C# 4개와
값 타입 계약 EditMode test C# 1개, 각 meta와 Result만 만들어.
좌표 변환, bounds, TryCreate, CSV, asmdef, Scene, Prefab은 만들거나 수정하지 마.

신규 값 타입 테스트 12개, 기존 constant test 6개, 기존 architecture test 10개가
모두 PASS일 때만 STATUS FINALIZE를 수행하고 다음 TASK는 시작하지 마.
```
