# PATCH MANIFEST

```yaml
patch_id: MAP00_03_CREATE_MAP_MODULE_STRUCTURE_v1.0
requires_status:
  current_task: NONE
  task_states:
    MAP00_01_PROJECT_AUDIT: COMPLETE
    MAP00_02_FOLDER_AND_ASMDEF_PLAN: COMPLETE
    MAP00_03_CREATE_MAP_MODULE_STRUCTURE: LOCKED
sets_current_task: TASKS/MAP00_03_CREATE_MAP_MODULE_STRUCTURE.md
copy_operations:
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/MAP00_03_CREATE_MAP_MODULE_STRUCTURE.md
    destination: MapDesign/MCP/TASKS/MAP00_03_CREATE_MAP_MODULE_STRUCTURE.md
    mode: create
forbidden_operations:
  - delete
  - move_existing_project_files
  - rename_existing_project_files
  - modify_assets_during_patch_apply
  - modify_csv_schema
  - modify_asmdef
  - git_commit
  - git_push
```

## 적용 검증

- `MapDesign/MCP/06_IMPLEMENTATION_STATUS.md`의 Current Task가 `TASKS/MAP00_03_CREATE_MAP_MODULE_STRUCTURE.md`다.
- 상태 표에서 `MAP00_03_CREATE_MAP_MODULE_STRUCTURE`가 `CURRENT`다.
- Task 파일이 존재하고 비어 있지 않다.
- PATCH APPLY 단계에서는 `Assets/**`가 변경되지 않는다.

