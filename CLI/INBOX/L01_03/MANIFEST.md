# MANIFEST

```yaml
patch_id: L01_03_SPAWN
version: 1.0-short
requires_previous_task:
  path: CLI/MCP/REPORTS/L01_02_RESULT.md
  sha256: 906657a671a710336a64a508f9f213d8172acfd855269efe67797fe25cabb6f3
  required_text:
    - "STATUS: PASS"
    - "None. Prefab and manual scene composition only."
    - "No generated run spawn wiring"
    - "Current Task after finalize: NONE"
payload:
  task: PAYLOAD/TASK.md
  task_sha256: 8bfd179f0c7124af3b38ccec9a409b41ddafe4e55e3fedef75c7722e21ebf0f5
  status: PAYLOAD/STATUS.md
  status_sha256: be08d9f735d945d02c098afed1f1ff06d3efb45329f22c4decdcd620fd46d7f1
  master: PAYLOAD/MASTER.md
  master_sha256: 54084c9effbdee695ddd28e15af23feee3e11cb021e632eb87577cca4ba3061b
sets_current_task: CLI/MCP/TASKS/L01_03.md
copy:
  - PAYLOAD/MASTER.md -> CLI/MCP/MASTER.md
  - PAYLOAD/STATUS.md -> CLI/MCP/STATUS.md
  - PAYLOAD/TASK.md -> CLI/MCP/TASKS/L01_03.md
allowed_project_writes:
  - Assets/_Game/Live/Runtime/**
  - Assets/_Game/Live/Prefabs/**
  - Assets/_Game/Scenes/Live/**
forbidden:
  - Assets/_Game/Character/Runtime/**
  - Assets/_Game/Map/Runtime/**
  - Assets/_Game/Live/Input/**
  - Assets/_Game/Tests/**
  - Packages/**
  - ProjectSettings/**
  - MapDesign/**
  - CharacterDesign/**
  - future tasks
  - git commit
  - git push
```
