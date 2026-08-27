# PATCH MANIFEST

```yaml
patch_id: MAP09_00_CREATE_V2_MODULE_STRUCTURE
patch_version: 1.0
revision_note: MAP08_14 PASS/finalize 후 구조-only transition task 하나만 연다. 기존 MAP09 solver backlog는 V2 compact backlog로 교체하고 MAP09_01 이후는 locked 상태로 유지한다.
requires_status:
  current_task: NONE
  task_states_required:
    MAP08_14_MAP08_EXIT_TESTS: COMPLETE
    MAP09_01_IMPLEMENT_SECTOR_RECIPE_RESOLVER: LOCKED
requires_result:
  path: MapDesign/MCP/REPORTS/MAP08_14_MAP08_EXIT_TESTS_RESULT.md
  exact_status: PASS
  sha256: 5d0b2f0d478ef8479b93e1b9163445f6e736022b533dee77f81690b8670cf2d1
requires_previous_task_file:
  path: MapDesign/MCP/TASKS/MAP08_14_MAP08_EXIT_TESTS.md
  sha256: 6fffc0ed3f8ca333cf7d74d44c437ab6e4193871ce8b2a7a254405e4bcaa5e8e
requires_current_master:
  path: MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: 1ac17f29000452938937cce3dd4aa95d00bdf3e1c9f1d376f125f6d48cc10543
requires_current_status:
  path: MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
  sha256: 3f7877436797e4e061bc9b04decdd65e71955826b6054eb60b5fddca43b44d6c
current_task_payload:
  path: PAYLOAD/TASKS/MAP09_00_CREATE_V2_MODULE_STRUCTURE.md
  sha256: d3b4d6ffdb149823c1e2686ccded43897127aa0b8ea9bc74a3da0491f457ab63
payload_status:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: 6ea5cdd12f1512fed9ddf3ed727ad89a8f7436cbc4faca2da73aefe93d270687
payload_master:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: 2f1fa53df4eb3687507c68d51167f681872622ed818e4835773a9c121e8ef4a7
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks_before_apply: 205
  master_tasks_after_apply: 214
  status_counts_before_apply: 105 COMPLETE / 0 CURRENT / 100 LOCKED
  status_counts_after_apply: 105 COMPLETE / 1 CURRENT / 108 LOCKED
  map08_phase_exit: APPROVED
  map08_14_result_sha256: 5d0b2f0d478ef8479b93e1b9163445f6e736022b533dee77f81690b8670cf2d1
  map08_14_installed_task_sha256: 6fffc0ed3f8ca333cf7d74d44c437ab6e4193871ce8b2a7a254405e4bcaa5e8e
  map08_required_union: 9220/9220
  required_subset_total: 19347/19347
  boundary_coverage_digest: f7ff1c49f5bc33a4ad57799269bc3915806fe0cb60f347ed76eb16ea26f7fc68
  authoring_manifest_sha256: f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb
  global_assets_meta_after_map08_14: 3816
  map_assets_meta_after_map08_14: 596
  map09_00_scope: exact_24_additive_directories_and_folder_metas_only
forbids_started_task_keys:
  - MAP09_01_FREEZE_BASELINE_AND_REGISTER_V2_PASSES
  - MAP09_01_IMPLEMENT_SECTOR_RECIPE_RESOLVER
forbids_started_task_prefixes:
  - MAP10
  - MAP11
  - MAP12
  - MAP13
  - MAP14
  - MAP15
  - MAP16
  - MAP17
  - MAP18
  - MAP19
  - MAP20
  - MAP21
sets_current_task: TASKS/MAP09_00_CREATE_V2_MODULE_STRUCTURE.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/MAP09_00_CREATE_V2_MODULE_STRUCTURE.md
    destination: MapDesign/MCP/TASKS/MAP09_00_CREATE_V2_MODULE_STRUCTURE.md
    mode: create
forbidden_operations:
  - delete
  - overwrite_existing_task_file
  - move_existing_project_files
  - rename_existing_project_files
  - modify_assets_during_patch_apply
  - modify_csv_during_patch_apply
  - modify_runtime_or_editor_code_during_patch_apply
  - modify_tests_during_patch_apply
  - modify_asmdef
  - start_map09_01_or_later
  - run_retired_map09_to_map15_package
  - git_commit_during_patch_apply
  - git_push
```

## Apply Validation

- 적용 전 Current Task는 `NONE`, MAP08_14는 `COMPLETE`, 기존 205-task backlog의 MAP09+는 `LOCKED`다.
- MAP08_14 Result는 exact `PASS`, SHA-256 `5d0b2f0d478ef8479b93e1b9163445f6e736022b533dee77f81690b8670cf2d1`다.
- 적용 후 `214 rows = 105 COMPLETE / MAP09_00 CURRENT / 108 LOCKED`다.
- 새 Master는 기존 MAP09~15 solver backlog를 폐기하고 MAP09~21 V2 compact backlog로 교체한다.
- PATCH APPLY 단계는 Master/Status/Task 문서만 교체/생성한다. Unity 디렉터리와 folder meta 생성은 Task execution 단계에서만 수행한다.
- `MAP09_01_FREEZE_BASELINE_AND_REGISTER_V2_PASSES`와 이후 전체는 LOCKED다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 `BLOCKED`다.
