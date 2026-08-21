# PATCH MANIFEST

```yaml
patch_id: MAP06_08_REPAIR_RESERVED_ADAPTER_ACCOUNTING
patch_version: 1.1
revision_note: MAP06_08 BLOCKED 후 구현 산출물은 보존하고 approved reserved-adapter source overlap을 반영하도록 Task accounting contract만 교정한다. MAP06_09는 시작하지 않는다.
requires_status:
  current_task: TASKS/MAP06_08_ASSIGN_INACTIVE_BUFFERS.md
  status_counts: 75 COMPLETE / 1 CURRENT / 129 LOCKED
  task_states_required:
    MAP06_07_IMPLEMENT_RETURN_POLICY: COMPLETE
    MAP06_08_ASSIGN_INACTIVE_BUFFERS: CURRENT
    MAP06_09_IMPLEMENT_OPTIONAL_REGION_VALIDATOR: LOCKED
    MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS: LOCKED
requires_result:
  path: MapDesign/MCP/REPORTS/MAP06_08_ASSIGN_INACTIVE_BUFFERS_RESULT.md
  exact_status: BLOCKED
  sha256: 759de495f3e2608fba844e5cca5ab3c6d7cd0479a73c8a3928c1ac4b964045fa
repair_basis:
  current_task_sha256: 778d5beb1944ddd01e4541254f6d63d55ce255c3eaeab0f79143ee4de2de9ec7
  revised_task_sha256: 0e45ed924cd515ca497abca85e0ede2a6efddefa9648c72c21b0d00a93647340
  blocked_result_sha256: 759de495f3e2608fba844e5cca5ab3c6d7cd0479a73c8a3928c1ac4b964045fa
  prior_map06_07_result_sha256: 2815e6b35df71be1477812594435ed4793c3c9a03c60f1ef602267e4a2e12329
  blocked_reason: old_task_required_zero_overlap_and_protected_union_94_but_approved_fixture_has_site_mandatory_adapter_overlap
  approved_reserved_adapter_overlap: [0, 28, 106]
  source_counts_reserved_mandatory_type0: 8/47/39
  exclusive_projected_reserved_mandatoryonly_type0_inactive: 8/44/39/78
  protected_union: 91
  full_accounting: 169
  focused_blocker: OwnershipOverlap at sectors 0,28,106
  observed_new_suite_cases: 281
  observed_assets_meta_after_blocked_attempt: 3304
  observed_authoring_csv_meta: 50/50
  observed_duplicate_guid_groups: 0
  type4_contract: U+D mandatory; L/R independent; UD/LUD/RUD/LRUD legal
forbids_started_task_prefixes:
  - MAP06_09
  - MAP06_10
  - MAP07
  - MAP08
  - MAP09
  - MAP10
  - MAP11
  - MAP12
  - MAP13
  - MAP14
  - MAP15
sets_current_task: TASKS/MAP06_08_ASSIGN_INACTIVE_BUFFERS.md
copy_operations:
  - source: PAYLOAD/TASKS/MAP06_08_ASSIGN_INACTIVE_BUFFERS.md
    destination: MapDesign/MCP/TASKS/MAP06_08_ASSIGN_INACTIVE_BUFFERS.md
    mode: replace_if_sha256_matches
    required_existing_sha256: 778d5beb1944ddd01e4541254f6d63d55ce255c3eaeab0f79143ee4de2de9ec7
forbidden_operations:
  - delete
  - modify_master_implementation_task_list
  - modify_implementation_status_during_patch_apply
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

- 적용 전 Current Task는 `TASKS/MAP06_08_ASSIGN_INACTIVE_BUFFERS.md`다.
- 현재 Task SHA-256은 `778d5beb1944ddd01e4541254f6d63d55ce255c3eaeab0f79143ee4de2de9ec7`다.
- 현재 MAP06_08 Result는 exact `BLOCKED`, SHA-256 `759de495f3e2608fba844e5cca5ab3c6d7cd0479a73c8a3928c1ac4b964045fa`다.
- 205개 상태 행에서 MAP06_07까지 `COMPLETE`, MAP06_08 `CURRENT`, MAP06_09 이후는 `LOCKED`다.
- PATCH APPLY 단계에서는 Task 문서만 replace하고 Master/Status/Assets/CSV/C#/test/asmdef는 변경하지 않는다.
- 교정 Task SHA-256은 `0e45ed924cd515ca497abca85e0ede2a6efddefa9648c72c21b0d00a93647340`다.
- Type4는 U+D mandatory, L/R independent이며 `UD`, `LUD`, `RUD`, `LRUD` 네 조합 모두 legal이다.
- Authoring CSV는 source artifact로만 보존하고 수정하지 않는다.

Task destination이 required_existing_sha256과 다르면 `BLOCKED`다.
