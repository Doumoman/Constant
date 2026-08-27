# PATCH MANIFEST

```yaml
patch_id: MAP00_10_MAP00_EXIT_AUDIT
patch_version: 1.0
requires_status:
  current_task: NONE
  task_states:
    MAP00_01_PROJECT_AUDIT: COMPLETE
    MAP00_02_FOLDER_AND_ASMDEF_PLAN: COMPLETE
    MAP00_03_CREATE_MAP_MODULE_STRUCTURE: COMPLETE
    MAP00_04_CREATE_TEST_STRUCTURE: COMPLETE
    MAP00_05_DEFINE_WORLDGEN_CONSTANTS: COMPLETE
    MAP00_06_IMPLEMENT_COORDINATE_VALUE_TYPES: COMPLETE
    MAP00_07_IMPLEMENT_COORDINATE_CONVERSIONS: COMPLETE
    MAP00_08_CREATE_COORDINATE_TESTS: COMPLETE
    MAP00_09_CREATE_COORDINATE_DEBUG_VIEW: COMPLETE
requires_result:
  path: MapDesign/MCP/REPORTS/MAP00_09_CREATE_COORDINATE_DEBUG_VIEW_RESULT.md
  exact_status: PASS
forbids_started_task_prefixes:
  - MAP01
  - MAP02
sets_current_task: TASKS/MAP00_10_MAP00_EXIT_AUDIT.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/MAP00_10_MAP00_EXIT_AUDIT.md
    destination: MapDesign/MCP/TASKS/MAP00_10_MAP00_EXIT_AUDIT.md
    mode: create
forbidden_operations:
  - delete
  - move_existing_project_files
  - rename_existing_project_files
  - modify_assets_during_patch_apply
  - modify_assets_during_task_execution
  - modify_runtime_editor_or_test_code
  - modify_csv
  - modify_asmdef_or_asmref
  - modify_scene_or_prefab
  - modify_packages_or_projectsettings
  - run_existing_map01_patch
  - git_commit
  - git_push
```

## 적용 검증

- patch folder, `patch_id`, Task ID는 모두 `MAP00_10_MAP00_EXIT_AUDIT`로 일치한다.
- 기존 `MASTER_IMPLEMENTATION_TASK_LIST.md`가 205개 Task를 포함하고 MAP00_09를 정확한 직전 COMPLETE 단계로 기록하는지 확인한다.
- `REPORTS/MAP00_09_CREATE_COORDINATE_DEBUG_VIEW_RESULT.md`의 exact status가 `PASS`인지 확인한다.
- 이 패치의 Master 사본은 MAP00_01~09 COMPLETE, MAP00_10 NEXT를 반영한 승인 갱신본이므로 `replace`한다.
- `06_IMPLEMENTATION_STATUS.md`의 Current Task가 `TASKS/MAP00_10_MAP00_EXIT_AUDIT.md`다.
- 상태 표에서 `MAP00_10_MAP00_EXIT_AUDIT`가 `CURRENT`다.
- MAP01 이후 Task는 모두 `LOCKED`이며 기존 MAP01_01 패키지는 `HOLD / DO NOT RUN`이다.
- Task 파일이 존재하고 비어 있지 않다.
- PATCH APPLY와 TASK EXECUTION 모두에서 `Assets/**`를 변경하지 않는다. TASK가 생성할 수 있는 파일은 Result 1개뿐이다.

사전 조건이나 기존 Master의 Task 수·직전 상태가 다르면 임의 보정하거나 덮어쓰지 말고 `BLOCKED`로 종료한다.
