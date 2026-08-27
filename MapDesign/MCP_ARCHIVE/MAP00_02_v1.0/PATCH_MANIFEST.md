# PATCH_MANIFEST

```yaml
patch_id: MAP00_02_v1.0
requires_status:
  MAP00_01_PROJECT_AUDIT: COMPLETE
sets_current_task: TASKS/MAP00_02_FOLDER_AND_ASMDEF_PLAN.md

copy_operations:
  - source: payload/06_IMPLEMENTATION_STATUS.md
    destination: MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace

  - source: payload/TASKS/MAP00_02_FOLDER_AND_ASMDEF_PLAN.md
    destination: MapDesign/MCP/TASKS/MAP00_02_FOLDER_AND_ASMDEF_PLAN.md
    mode: create

forbidden_operations:
  - Assets modification
  - asmdef modification
  - C# generation
  - CSV authoring modification
  - Scene/Prefab modification
```

## Apply Completion

- `MapDesign/MCP/06_IMPLEMENTATION_STATUS.md`의 Current Task가
  `TASKS/MAP00_02_FOLDER_AND_ASMDEF_PLAN.md`여야 한다.
- Current Task 파일이 존재해야 한다.
