# MANIFEST

```yaml
patch_id: L00_01_SURVEY
version: 1.0-short
requires_character_final_exit:
  path: CharacterDesign/MCP/REPORTS/CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT_RESULT.md
  sha256: 6efc2ac08d7cb52fd8ba260888310dd403ae64d191767a9338b174a0897fc96c
  required_text:
    - "STATUS: PASS"
    - "CHARACTER_FINAL_EXIT_DECISION: APPROVED"
    - "Character harness final state: COMPLETE"
payload:
  task: PAYLOAD/TASK.md
  task_sha256: a0a8fa3e0ac59ec2539c35c7894a8af6aeee3ed02f906504d74682f61770f92e
  status: PAYLOAD/STATUS.md
  status_sha256: 14a93c705e4e14f0a362dae6ab41d0be868fd2d8a3342fd35ec70180b015f355
  master: PAYLOAD/MASTER.md
  master_sha256: bc7e6a6ee028125d3dc24481d433b23f508001a3d37a544df3f2a978ef78d91f
sets_current_task: CLI/MCP/TASKS/L00_01.md
copy:
  - PAYLOAD/MASTER.md -> CLI/MCP/MASTER.md
  - PAYLOAD/STATUS.md -> CLI/MCP/STATUS.md
  - PAYLOAD/TASK.md -> CLI/MCP/TASKS/L00_01.md
forbidden:
  - Assets/**
  - Packages/**
  - ProjectSettings/**
  - MapDesign/**
  - CharacterDesign/**
  - future tasks
  - git commit
  - git push
```
