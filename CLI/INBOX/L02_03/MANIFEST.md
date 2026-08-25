# MANIFEST

```yaml
patch_id: L02_03_ROOM_AUDIT
version: 1.0-short
requires_previous_task:
  path: CLI/MCP/REPORTS/L02_02_RESULT.md
  sha256: 01bfe28aa2f4bf00245cecae1000ba4ab383cea4a8c297fe962f69ce33ba61d6
  required_text:
    - "STATUS: PASS"
    - "None. Generated MAP adapter produces Character snapshots/readiness/routes/world query only."
    - "No scene or prefab wiring"
    - "Current Task after finalize: NONE"
payload:
  task: PAYLOAD/TASK.md
  task_sha256: 8beb857a8ce7a9195fe483eb39f563a1a54e8c2762d7d5b4c15684f1dd8be3fc
  status: PAYLOAD/STATUS.md
  status_sha256: 40151ced7f11844a9f83c7d0b05eb9f6fc982df6d2672eafbb03cc1be1fe8285
  master: PAYLOAD/MASTER.md
  master_sha256: f4c670b1ccc55d9e3fae5083000964610965d2cf4d8c3d1a56f5bc95356edaef
sets_current_task: CLI/MCP/TASKS/L02_03.md
copy:
  - PAYLOAD/MASTER.md -> CLI/MCP/MASTER.md
  - PAYLOAD/STATUS.md -> CLI/MCP/STATUS.md
  - PAYLOAD/TASK.md -> CLI/MCP/TASKS/L02_03.md
allowed_project_writes:
  - CLI/MCP/REPORTS/L02_03_RESULT.md
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
  - Builds/**
  - Temp/**
  - future tasks
  - git commit
  - git push
```
