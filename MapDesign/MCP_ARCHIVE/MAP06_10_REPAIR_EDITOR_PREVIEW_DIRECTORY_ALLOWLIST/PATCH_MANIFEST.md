# PATCH MANIFEST

```yaml
patch_id: MAP06_10_REPAIR_EDITOR_PREVIEW_DIRECTORY_ALLOWLIST
patch_version: 1.1
revision_note: MAP06_10 BLOCKED 후 Editor Preview directory/folder meta allowlist contradiction만 교정한다. MAP07은 시작하지 않는다.
requires_status:
  current_task: TASKS/MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS.md
  status_counts: 77 COMPLETE / 1 CURRENT / 127 LOCKED
  task_states_required:
    MAP06_09_IMPLEMENT_OPTIONAL_REGION_VALIDATOR: COMPLETE
    MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS: CURRENT
    MAP07_01_IMPLEMENT_MICROCHUNK_DEFINITION: LOCKED
requires_result:
  path: MapDesign/MCP/REPORTS/MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS_RESULT.md
  exact_status: BLOCKED
  sha256: d02204b7515e4818052f6e5e8dad0fc0740803f3af5f0753f652b5c715e3119e
repair_basis:
  current_task_sha256: 205ce60e1e591036a80bc7dc10a939ea95d0237d09babe106e86c09b78e70605
  revised_task_sha256: 623da5aaf2f8c72dd830fb5f859c4b05a631a93b7f4fa2a3aa67adc823f95cdb
  blocked_result_sha256: d02204b7515e4818052f6e5e8dad0fc0740803f3af5f0753f652b5c715e3119e
  prior_map06_09_result_sha256: 51a6f0dd621db698628ceef6ba7e7f2f18988b213ad564e7b35e00c52041d62a
  prior_map06_09_task_sha256: e5f430c29dcba4344feb1ba12fff73fc9052c3f3a386d672a7e8a3b016a2c97e
  original_map06_10_manifest_sha256: f13c8a59b01394e1c51ef0cb3538d59f345261f7283370eefbe28a60e7c12511
  original_map06_10_receipt_sha256: c1bc8e3c7512b7cbc63b4c0da67dfe8c29c6441e71526b9c021e90275dee0ff1
  blocked_reason: editor_preview_directory_absent_but_v1_0_forbade_new_directory_folder_meta
  absent_directory: Assets/_Game/Editor/MapAuthoring/Preview/
  absent_folder_meta: Assets/_Game/Editor/MapAuthoring/Preview.meta
  absent_predecessor_drawer: Assets/_Game/Editor/MapAuthoring/Preview/MandatoryRouteOverlaySceneDrawer.cs
  canonical_drawer_target: Assets/_Game/Editor/MapAuthoring/Preview/OptionalRegionOverlaySceneDrawer.cs
  assets_meta_baseline: 3311
  assets_meta_after_repair_task_execution: 3323
  new_csharp_matching_meta: 11/11
  new_editor_preview_folder_meta: 1/1
  other_new_directory_folder_meta: 0
  type4_contract: U+D mandatory; L/R independent; UD/LUD/RUD/LRUD legal
  authoring_csv_policy: original source artifact only; do not modify unless explicitly requested
forbids_started_task_prefixes:
  - MAP07
  - MAP08
  - MAP09
  - MAP10
  - MAP11
  - MAP12
  - MAP13
  - MAP14
  - MAP15
sets_current_task: TASKS/MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS.md
copy_operations:
  - source: PAYLOAD/TASKS/MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS.md
    destination: MapDesign/MCP/TASKS/MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS.md
    mode: replace_if_sha256_matches
    required_existing_sha256: 205ce60e1e591036a80bc7dc10a939ea95d0237d09babe106e86c09b78e70605
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

- 적용 전 Current Task는 `TASKS/MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS.md`다.
- 현재 Task SHA-256은 `205ce60e1e591036a80bc7dc10a939ea95d0237d09babe106e86c09b78e70605`다.
- 현재 MAP06_10 Result는 exact `BLOCKED`, SHA-256 `d02204b7515e4818052f6e5e8dad0fc0740803f3af5f0753f652b5c715e3119e`다.
- 205개 상태 행에서 MAP06_09까지 `COMPLETE`, MAP06_10 `CURRENT`, MAP07 이후는 `LOCKED`다.
- PATCH APPLY 단계에서는 Task 문서만 replace하고 Master/Status/Assets/CSV/C#/test/asmdef는 변경하지 않는다.
- 교정 Task SHA-256은 `623da5aaf2f8c72dd830fb5f859c4b05a631a93b7f4fa2a3aa67adc823f95cdb`다.
- Editor preview directory/folder meta는 exact `Assets/_Game/Editor/MapAuthoring/Preview/`와 `Assets/_Game/Editor/MapAuthoring/Preview.meta`만 신규 허용한다.
- MAP06_10 실행 완료 시 Assets meta gate는 `3311 -> 3323`이다.
- Type4는 U+D mandatory, L/R independent이며 `UD`, `LUD`, `RUD`, `LRUD` 네 조합 모두 legal이다.
- Authoring CSV는 source artifact로만 보존하고 수정하지 않는다.

Task destination이 required_existing_sha256과 다르면 `BLOCKED`다.
