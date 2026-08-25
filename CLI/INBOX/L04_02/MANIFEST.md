# MANIFEST

```yaml
patch_id: L04_02_FINAL
version: 1.0-short
requires_previous_task:
  path: CLI/MCP/REPORTS/L04_01_RESULT.md
  sha256: 1f005cffa58cd3c2e409a797a151642d19e7750643512986e43a2714a3dac299
  required_text:
    - "STATUS: PASS"
    - "신규 PlayMode: 9/9 PASS"
    - "Character EditMode 177/177 PASS"
    - "Current Task after finalize: NONE"
    - "Next Task auto-opened: NO"
payload:
  task: PAYLOAD/TASK.md
  task_sha256: 9a73b74459416f51adc837fa70d390ef856935f61fa5b3568bd5c1f582c4db40
  status: PAYLOAD/STATUS.md
  status_sha256: e3fe84de5f06b586aa74da25f3ff52cbace93b180933da6c8054fbe09ecf6a06
  master: PAYLOAD/MASTER.md
  master_sha256: 087c8e2e01d757cb0a5570123c2cb403292eeb1198efd74dd0d887f1af21b2e4
sets_current_task: CLI/MCP/TASKS/L04_02.md
copy:
  - PAYLOAD/MASTER.md -> CLI/MCP/MASTER.md
  - PAYLOAD/STATUS.md -> CLI/MCP/STATUS.md
  - PAYLOAD/TASK.md -> CLI/MCP/TASKS/L04_02.md
allowed_project_writes:
  - Builds/CLI_Live_Final/**
  - CLI/MCP/REPORTS/L04_02_RESULT.md
forbidden:
  - Assets/**
  - Packages/**
  - ProjectSettings/**
  - MapDesign/**
  - CharacterDesign/**
  - CLI/MCP/STATUS.md
  - CLI/MCP/MASTER.md
  - CLI/MCP/TASKS/**
  - CLI/MCP/INPUTS/**
  - Temp/**
  - future tasks
  - git commit
  - git push
```
