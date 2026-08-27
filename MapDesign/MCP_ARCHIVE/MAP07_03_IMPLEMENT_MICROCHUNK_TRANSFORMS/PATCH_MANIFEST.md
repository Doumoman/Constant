# PATCH MANIFEST

```yaml
patch_id: MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS
patch_version: 1.0
revision_note: MAP07_02 PASS/finalize 후 microchunk transform task 하나만 연다. MAP07_04 socket-edge validation 이후는 locked 상태로 유지한다.
requires_status:
  current_task: NONE
  task_states_required:
    MAP07_02_IMPLEMENT_TILE_LAYER_RULES: COMPLETE
    MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS: LOCKED
    MAP07_04_IMPLEMENT_SOCKET_EDGE_VALIDATION: LOCKED
requires_result:
  path: MapDesign/MCP/REPORTS/MAP07_02_IMPLEMENT_TILE_LAYER_RULES_RESULT.md
  exact_status: PASS
  sha256: 98240add84d955ffdc50c3e22e18eb3a0255d9a1d397e9d6c2039e2488dafc4e
requires_previous_task_file:
  path: MapDesign/MCP/TASKS/MAP07_02_IMPLEMENT_TILE_LAYER_RULES.md
  sha256: c9cb155bdb0b9f2d047b8305c35f32392d691988f612bc107849d0a9f3292edb
current_task_payload:
  path: PAYLOAD/TASKS/MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS.md
  sha256: 82434805780000e3695cbdda45d5888c4234ba617bdc5bcded843643b4c7aac8
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 205
  status_counts_after_apply: 80 COMPLETE / 1 CURRENT / 124 LOCKED
  map07_02_result_sha256: 98240add84d955ffdc50c3e22e18eb3a0255d9a1d397e9d6c2039e2488dafc4e
  map07_02_task_sha256: c9cb155bdb0b9f2d047b8305c35f32392d691988f612bc107849d0a9f3292edb
  map07_02_tile_layer_digest: ace56d38399aa6ccea6e0e0e7361f3802b7eb7418b1ef1d081f6a40b04e08160
  map07_02_acceptance_total: 5001/5001
  map07_02_failed_skipped: 0/0
  map07_02_compile_console_warnings: 0/0/0
  map07_02_assets_meta_total: 3339
  map07_02_authoring_csv_meta: 50/50
  map07_02_authoring_manifest_sha256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
  map07_02_generated_csv_created: 0
  transform_scope: runtime_model_only
forbids_started_task_prefixes:
  - MAP07_04
  - MAP07_05
  - MAP07_06
  - MAP07_07
  - MAP07_08
  - MAP07_09
  - MAP07_10
  - MAP07_11
  - MAP07_12
  - MAP07_13
  - MAP08
  - MAP09
  - MAP10
  - MAP11
  - MAP12
  - MAP13
  - MAP14
  - MAP15
sets_current_task: TASKS/MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS.md
    destination: MapDesign/MCP/TASKS/MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS.md
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
  - git_commit
  - git_push
```

## 적용 검증

- 적용 전 Current Task는 `NONE`이다.
- MAP07_02 Result는 exact `PASS`, SHA-256 `98240add84d955ffdc50c3e22e18eb3a0255d9a1d397e9d6c2039e2488dafc4e`다.
- 이전 MAP07_02 Task file SHA-256은 `c9cb155bdb0b9f2d047b8305c35f32392d691988f612bc107849d0a9f3292edb`다.
- 새 MAP07_03 Task payload SHA-256은 `82434805780000e3695cbdda45d5888c4234ba617bdc5bcded843643b4c7aac8`다.
- MAP07_02는 COMPLETE 자격을 갖췄고 MAP07_03은 아직 `LOCKED / DO NOT START`였다.
- 205개 상태 행에서 MAP07_02까지 `COMPLETE`, MAP07_03 `CURRENT`, MAP07_04 이후는 `LOCKED`다.
- 새 Master/Status는 `80 COMPLETE / MAP07_03 CURRENT / 124 LOCKED`다.
- MAP07_02 tile-layer digest `ace56d38399aa6ccea6e0e0e7361f3802b7eb7418b1ef1d081f6a40b04e08160`, acceptance `5001/5001`, Assets meta `3339`, Authoring CSV/meta `50/50`를 입력 기준으로 보존한다.
- PATCH APPLY 단계에서는 Master/Status/Task 문서만 교체/생성하고 Assets, CSV, C#, test, asmdef는 변경하지 않는다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 `BLOCKED`다.
