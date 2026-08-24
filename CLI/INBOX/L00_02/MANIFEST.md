# MANIFEST

```yaml
patch_id: L00_02_LOCK
version: 1.0-short
requires_previous_task:
  path: CLI/MCP/REPORTS/L00_01_RESULT.md
  sha256: 4e982e431d05a0c01dccac9062327068ea51a7ff713dfe281796a3dd9846d69b
  required_text:
    - "STATUS: PASS"
    - "REGISTRY_STATE: FILLED_BY_L00_01"
    - "Current Task after finalize: NONE"
payload:
  task: PAYLOAD/TASK.md
  task_sha256: a9deef5830f213ef174a540d5674e4aa4a3f0698700fbd37e86f305caab768fc
  status: PAYLOAD/STATUS.md
  status_sha256: 246a1232d16ae5d5c20fb2c1202200e48adf1f17c8c5419b6fae80ece10fe898
  master: PAYLOAD/MASTER.md
  master_sha256: 988db5b9bb0b9b137b8da6ed56b17c637c625d2cc92e5637b0293259ae2b46c5
sets_current_task: CLI/MCP/TASKS/L00_02.md
copy:
  - PAYLOAD/MASTER.md -> CLI/MCP/MASTER.md
  - PAYLOAD/STATUS.md -> CLI/MCP/STATUS.md
  - PAYLOAD/TASK.md -> CLI/MCP/TASKS/L00_02.md
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
