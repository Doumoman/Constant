# PATCH MANIFEST

```yaml
patch_id: MAP08_14_MAP08_EXIT_TESTS
patch_version: 1.0
revision_note: MAP08_13 PASS/finalize 후 MAP08_14 task 하나만 연다. MAP09 이후는 locked 상태로 유지한다.
requires_status:
  current_task: NONE
  task_states_required:
    MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW: COMPLETE
    MAP08_14_MAP08_EXIT_TESTS: LOCKED
    MAP09_01_IMPLEMENT_SECTOR_RECIPE_RESOLVER: LOCKED
requires_result:
  path: MapDesign/MCP/REPORTS/MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW_RESULT.md
  exact_status: PASS
  sha256: cb72264380c94a35ab6abe42f672c06e994f30deadc5a867546a31279b9bf7cd
requires_previous_task_file:
  path: MapDesign/MCP/TASKS/MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW.md
  sha256: 5e932e82ae7ed78e732c26907ac4cd480e43d7bf14ee9978df46d2917310153d
current_task_payload:
  path: PAYLOAD/TASKS/MAP08_14_MAP08_EXIT_TESTS.md
  sha256: 6fffc0ed3f8ca333cf7d74d44c437ab6e4193871ce8b2a7a254405e4bcaa5e8e
payload_status:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: cb1bc788c4c724d896b652c9f6430ff322d5be7829b88794122d6c1ac5ab655b
payload_master:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: 1ac17f29000452938937cce3dd4aa95d00bdf3e1c9f1d376f125f6d48cc10543
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 205
  status_counts_before_apply: 104 COMPLETE / 0 CURRENT / 101 LOCKED
  status_counts_after_apply: 104 COMPLETE / 1 CURRENT / 100 LOCKED
  map08_phase_before_apply: 13 COMPLETE / 0 CURRENT / 1 LOCKED
  map08_phase_after_apply: 13 COMPLETE / 1 CURRENT / 0 LOCKED
  map08_13_result_sha256: cb72264380c94a35ab6abe42f672c06e994f30deadc5a867546a31279b9bf7cd
  map08_13_installed_task_sha256: 5e932e82ae7ed78e732c26907ac4cd480e43d7bf14ee9978df46d2917310153d
  map08_12_result_sha256: 26801f7bc31d354b9639278ec133970e7840f3c7dacb9f0f2a4b6a0e0288896b
  map08_12_installed_task_sha256: cdaadd203562acdcad7024349499a2df3ac0ccd9860ef580473e984ae9490966
  map08_12_coverage_digest: f7ff1c49f5bc33a4ad57799269bc3915806fe0cb60f347ed76eb16ea26f7fc68
  map08_13_projection_counts: 31 candidates / 31 microchunks / 2976 tile rows / 62 socket rows
  map08_13_focused_tests: 640/640
  map08_required_union: 8380/8380
  map07_required_total: 5422/5422
  map06_required_total: 2746/2746
  map05_required_total: 1959/1959
  map08_13_acceptance_subset_total: 18507/18507
  map08_13_failed_skipped: 0/0
  map08_13_compile_console_warnings: 0/0/0
  map08_13_global_assets_meta_total: 3813
  map08_13_map_assets_meta_total: 596
  map08_13_authoring_csv_meta: 50/50
  map08_13_authoring_manifest_sha256: f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb
  map08_13_generated_csv_created: 0
  map08_14_scope: phase_exit_tests_only
forbids_started_task_prefixes:
  - MAP09
  - MAP10
  - MAP11
  - MAP12
  - MAP13
  - MAP14
  - MAP15
sets_current_task: TASKS/MAP08_14_MAP08_EXIT_TESTS.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/MAP08_14_MAP08_EXIT_TESTS.md
    destination: MapDesign/MCP/TASKS/MAP08_14_MAP08_EXIT_TESTS.md
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
  - run_completed_task_package
  - git_commit_during_patch_apply
  - git_push
```

## 적용 검증

- 적용 전 Current Task는 `NONE`이고 MAP08_13은 `COMPLETE`다.
- MAP08_13 Result는 exact `PASS`, SHA-256 `cb72264380c94a35ab6abe42f672c06e994f30deadc5a867546a31279b9bf7cd`다.
- 새 MAP08_14 Task payload SHA-256은 `6fffc0ed3f8ca333cf7d74d44c437ab6e4193871ce8b2a7a254405e4bcaa5e8e`다.
- 205개 상태 행에서 MAP08_13까지 `COMPLETE`, MAP08_14 `CURRENT`, MAP09 이후는 `LOCKED`다.
- 새 Master/Status는 `104 COMPLETE / MAP08_14 CURRENT / 100 LOCKED`다.
- PATCH APPLY 단계에서는 Master/Status/Task 문서만 교체/생성하고 Assets, CSV, C#, test, asmdef는 변경하지 않는다.
- 구현 완료 후에는 Task 본문에 정의된 커밋 요구사항을 따른다. Patch apply 단계에서는 커밋하지 않는다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 `BLOCKED`다.
