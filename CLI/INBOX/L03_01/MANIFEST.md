# MANIFEST

```yaml
patch_id: L03_01_TOOLS
version: 1.0-short
requires_previous_task:
  path: CLI/MCP/REPORTS/L02_03_RESULT.md
  sha256: 35f955f1026035be0300aeb33d623a4f241ccac22fb1b901e53f114fce53f518
  required_text:
    - "STATUS: PASS"
    - "LIVE02_EXIT_DECISION: APPROVED"
    - "L03_01 ENTRY: ELIGIBLE FOR SEPARATE PACKAGE"
    - "Current Task after finalize: NONE"
payload:
  task: PAYLOAD/TASK.md
  task_sha256: 827c1b40ee7c92cd0e9c5adae8804e56950ece962d6caa4d202ce3f23b876771
  status: PAYLOAD/STATUS.md
  status_sha256: 2a2b8b01207eb55203597877189adf7c09247194319abfabe2f769e789f3d22a
  master: PAYLOAD/MASTER.md
  master_sha256: 1d7636a809d8c8467fc962fe3c073c349c783b110fcca616ba29f7a9a21d7edb
sets_current_task: CLI/MCP/TASKS/L03_01.md
copy:
  - PAYLOAD/MASTER.md -> CLI/MCP/MASTER.md
  - PAYLOAD/STATUS.md -> CLI/MCP/STATUS.md
  - PAYLOAD/TASK.md -> CLI/MCP/TASKS/L03_01.md
allowed_project_writes:
  - Assets/_Game/Live/Runtime/Tools/**
  - CLI/MCP/REPORTS/L03_01_RESULT.md
forbidden:
  - Assets/_Game/Character/Runtime/**
  - Assets/_Game/Map/Runtime/**
  - Assets/_Game/Live/Input/**
  - Assets/_Game/Live/Prefabs/**
  - Assets/_Game/Scenes/**
  - Assets/_Game/Tests/**
  - Packages/**
  - ProjectSettings/**
  - MapDesign/**
  - CharacterDesign/**
  - CLI/MCP/STATUS.md
  - CLI/MCP/MASTER.md
  - CLI/MCP/TASKS/**
  - CLI/MCP/INPUTS/**
  - Builds/**
  - Temp/**
  - future tasks
  - git commit
  - git push
```
