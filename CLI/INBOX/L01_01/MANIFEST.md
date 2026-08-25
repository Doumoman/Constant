# MANIFEST

```yaml
patch_id: L01_01_INPUT
version: 1.0-short
requires_previous_task:
  path: CLI/MCP/REPORTS/L00_02_RESULT.md
  sha256: bc4b91f03d9177fc41044430081374d0f9c3f575aefeba9bab7d302f16ac24a4
  required_text:
    - "STATUS: PASS"
    - "LOCK_STATE: FILLED_BY_L00_02"
    - "Current Task after finalize: NONE"
payload:
  task: PAYLOAD/TASK.md
  task_sha256: 4afd5d6a6a6f93488263355a4ac1fef0b73f16f69f3a4745a2d3383818637500
  status: PAYLOAD/STATUS.md
  status_sha256: f31dd8f2bb72ba15065637143099da0f30255867c32946875766e8f9447693be
  master: PAYLOAD/MASTER.md
  master_sha256: 2be35ee66bb24254af8ca64ba0638b47e6d2a755d3f2175cc1f71e91795c0e11
sets_current_task: CLI/MCP/TASKS/L01_01.md
copy:
  - PAYLOAD/MASTER.md -> CLI/MCP/MASTER.md
  - PAYLOAD/STATUS.md -> CLI/MCP/STATUS.md
  - PAYLOAD/TASK.md -> CLI/MCP/TASKS/L01_01.md
allowed_project_writes:
  - Assets/_Game/Live/Runtime/**
  - Assets/_Game/Live/Input/**
forbidden:
  - Assets/_Game/Character/Runtime/**
  - Assets/_Game/Map/Runtime/**
  - Assets/_Game/Live/Prefabs/**
  - Assets/_Game/Scenes/**
  - Assets/_Game/Tests/**
  - Packages/**
  - ProjectSettings/**
  - MapDesign/**
  - CharacterDesign/**
  - future tasks
  - git commit
  - git push
```
