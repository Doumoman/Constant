# PATCH MANIFEST

```yaml
patch_id: MAP00_06_IMPLEMENT_COORDINATE_VALUE_TYPES
patch_version: 1.0
requires_status:
  current_task: NONE
  task_states:
    MAP00_01_PROJECT_AUDIT: COMPLETE
    MAP00_02_FOLDER_AND_ASMDEF_PLAN: COMPLETE
    MAP00_03_CREATE_MAP_MODULE_STRUCTURE: COMPLETE
    MAP00_04_CREATE_TEST_STRUCTURE: COMPLETE
    MAP00_05_DEFINE_WORLDGEN_CONSTANTS: COMPLETE
requires_result:
  path: MapDesign/MCP/REPORTS/MAP00_05_DEFINE_WORLDGEN_CONSTANTS_RESULT.md
  exact_status: PASS
forbids_started_task_prefixes:
  - MAP01
  - MAP02
sets_current_task: TASKS/MAP00_06_IMPLEMENT_COORDINATE_VALUE_TYPES.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/MAP00_06_IMPLEMENT_COORDINATE_VALUE_TYPES.md
    destination: MapDesign/MCP/TASKS/MAP00_06_IMPLEMENT_COORDINATE_VALUE_TYPES.md
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

- patch folder, `patch_id`, Task ID는 모두 `MAP00_06_IMPLEMENT_COORDINATE_VALUE_TYPES`로 일치한다.
- 기존 `MASTER_IMPLEMENTATION_TASK_LIST.md`가 205개 Task를 포함하고 MAP00_05를 정확한 직전 단계로 기록하는지 먼저 확인한다.
- 이 패치의 Master 사본은 MAP00_05를 COMPLETE, MAP00_06을 NEXT로 반영한 승인 갱신본이므로 `replace`한다.
- `06_IMPLEMENTATION_STATUS.md`의 Current Task가 `TASKS/MAP00_06_IMPLEMENT_COORDINATE_VALUE_TYPES.md`다.
- 상태 표에서 `MAP00_06_IMPLEMENT_COORDINATE_VALUE_TYPES`가 `CURRENT`다.
- MAP01 이후 Task는 모두 `LOCKED`다.
- Task 파일이 존재하고 비어 있지 않다.
- PATCH APPLY 단계에서는 `Assets/**`가 변경되지 않는다.

사전 조건이나 기존 Master의 Task 수·직전 상태가 다르면 임의 보정하거나 덮어쓰지 말고 `BLOCKED`로 종료한다.
