# PATCH MANIFEST

```yaml
patch_id: MAP00_05_DEFINE_WORLDGEN_CONSTANTS
patch_version: 1.0
requires_status:
  current_task: NONE
  task_states:
    MAP00_01_PROJECT_AUDIT: COMPLETE
    MAP00_02_FOLDER_AND_ASMDEF_PLAN: COMPLETE
    MAP00_03_CREATE_MAP_MODULE_STRUCTURE: COMPLETE
    MAP00_04_CREATE_TEST_STRUCTURE: COMPLETE
requires_result:
  path: MapDesign/MCP/REPORTS/MAP00_04_CREATE_TEST_STRUCTURE_RESULT.md
  exact_status: PASS
forbids_started_task_prefixes:
  - MAP01
  - MAP02
sets_current_task: TASKS/MAP00_05_DEFINE_WORLDGEN_CONSTANTS.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: create_or_verify_identical
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/MAP00_05_DEFINE_WORLDGEN_CONSTANTS.md
    destination: MapDesign/MCP/TASKS/MAP00_05_DEFINE_WORLDGEN_CONSTANTS.md
    mode: create
forbidden_operations:
  - delete
  - move_existing_project_files
  - rename_existing_project_files
  - modify_assets_during_patch_apply
  - modify_runtime_or_test_code_during_patch_apply
  - modify_csv
  - modify_asmdef
  - modify_scene_or_prefab
  - git_commit
  - git_push
```

## 적용 검증

- patch folder, `patch_id`, Task ID는 모두 `MAP00_05_DEFINE_WORLDGEN_CONSTANTS`로 일치한다.
- `MASTER_IMPLEMENTATION_TASK_LIST.md`가 존재하고 전체 205개 Task를 포함한다.
- `06_IMPLEMENTATION_STATUS.md`의 Current Task가 `TASKS/MAP00_05_DEFINE_WORLDGEN_CONSTANTS.md`다.
- 상태 표에서 `MAP00_05_DEFINE_WORLDGEN_CONSTANTS`가 `CURRENT`다.
- MAP01 이후 Task는 모두 `LOCKED`다.
- Task 파일이 존재하고 비어 있지 않다.
- PATCH APPLY 단계에서는 `Assets/**`가 변경되지 않는다.

`MASTER_IMPLEMENTATION_TASK_LIST.md`가 이미 존재하면 이 패치와 바이트 단위로 동일할 때만 재사용한다. 다르면 덮어쓰지 말고 `BLOCKED`로 종료한다.
