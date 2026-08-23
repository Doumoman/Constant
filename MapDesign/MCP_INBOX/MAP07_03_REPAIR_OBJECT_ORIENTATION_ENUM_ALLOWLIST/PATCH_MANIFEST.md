# PATCH MANIFEST

```yaml
patch_id: MAP07_03_REPAIR_OBJECT_ORIENTATION_ENUM_ALLOWLIST
patch_version: 1.0
revision_note: MAP07_03 v1.0 BLOCKED 원인인 object-slot orientation enum allowlist 모순만 교정한다. MAP07_04 이후는 locked 상태로 유지한다.
requires_status:
  current_task: TASKS/MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS.md
  task_states_required:
    MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS: CURRENT
    MAP07_04_IMPLEMENT_SOCKET_EDGE_VALIDATION: LOCKED
requires_result:
  path: MapDesign/MCP/REPORTS/MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS_RESULT.md
  exact_status: BLOCKED
  sha256: e267a5439a52aebda336256e726a9dd8d74f8a3a7317992e24b1da57dc40ab80
requires_current_task_file:
  path: MapDesign/MCP/TASKS/MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS.md
  sha256: 82434805780000e3695cbdda45d5888c4234ba617bdc5bcded843643b4c7aac8
replacement_task_payload:
  path: PAYLOAD/TASKS/MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS.md
  sha256: f9aee2e6fe0c0a3222eae894cb562ef2100813c4a91e16461fd03e5d5d4cb170
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 205
  status_counts_before_apply: 80 COMPLETE / 1 CURRENT / 124 LOCKED
  status_counts_after_apply: 80 COMPLETE / 1 CURRENT / 124 LOCKED
  map07_02_result_sha256: 98240add84d955ffdc50c3e22e18eb3a0255d9a1d397e9d6c2039e2488dafc4e
  map07_02_task_sha256: c9cb155bdb0b9f2d047b8305c35f32392d691988f612bc107849d0a9f3292edb
  map07_03_blocked_result_sha256: e267a5439a52aebda336256e726a9dd8d74f8a3a7317992e24b1da57dc40ab80
  map07_03_previous_task_sha256: 82434805780000e3695cbdda45d5888c4234ba617bdc5bcded843643b4c7aac8
  existing_microchunk_enums_sha256: aef9b83a97e839dc67b16cdf1cae94f60add83121a863eb30dd8790ace9919d7
  existing_microchunk_object_slot_definition_sha256: 80dcfbb46f6216bc194a86bc0fc5ae20bf1965d98011f24026994fca6c6f0fc5
  repair_scope: task_contract_only
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
  - source: PAYLOAD/TASKS/MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS.md
    destination: MapDesign/MCP/TASKS/MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS.md
    mode: replace
forbidden_operations:
  - delete
  - modify_master_status_during_patch_apply
  - modify_assets_during_patch_apply
  - modify_csv_during_patch_apply
  - modify_runtime_or_editor_code_during_patch_apply
  - modify_tests_during_patch_apply
  - modify_asmdef
  - run_completed_task_package
  - start_next_task
  - git_commit
  - git_push
```

## 적용 검증

- 적용 전 Current Task는 `MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS`다.
- MAP07_03 Result는 exact `BLOCKED`, SHA-256 `e267a5439a52aebda336256e726a9dd8d74f8a3a7317992e24b1da57dc40ab80`다.
- 기존 MAP07_03 Task file SHA-256은 `82434805780000e3695cbdda45d5888c4234ba617bdc5bcded843643b4c7aac8`다.
- 교정 MAP07_03 Task payload SHA-256은 `f9aee2e6fe0c0a3222eae894cb562ef2100813c4a91e16461fd03e5d5d4cb170`다.
- Master/Status는 변경하지 않는다. 상태는 계속 `80 COMPLETE / MAP07_03 CURRENT / 124 LOCKED`다.
- PATCH APPLY 단계에서는 Task 문서만 교체하고 Assets, CSV, C#, test, asmdef는 변경하지 않는다.
- MAP07_04 이후는 계속 `LOCKED / DO NOT START`다.

Task destination SHA가 `82434805780000e3695cbdda45d5888c4234ba617bdc5bcded843643b4c7aac8`가 아니면 `BLOCKED`다.
