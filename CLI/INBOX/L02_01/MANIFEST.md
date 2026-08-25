# MANIFEST

```yaml
patch_id: L02_01_ROUTE_CAMERA
version: 1.0-short
requires_previous_task:
  path: CLI/MCP/REPORTS/L01_03_RESULT.md
  sha256: 6c652ca2728bb3a36a51048371494aac3d917b219627ef7c0ff010ab669da88b
  required_text:
    - "STATUS: PASS"
    - "CharacterPlayerSpawnRequest consumed exactly once per run start."
    - "No generated MAP adapter wiring"
    - "Current Task after finalize: NONE"
payload:
  task: PAYLOAD/TASK.md
  task_sha256: 16c725f1edc03bcbbbab4a88e79435df7eda856b3850e44e722d803accb942b6
  status: PAYLOAD/STATUS.md
  status_sha256: 9fc6e58048979eb675168190e436becc77c56dfa0037666f1978c95c8847d82e
  master: PAYLOAD/MASTER.md
  master_sha256: 5d22e0ec98a8e22a78b6d4e83bb284904479296b256ab90fccc6bdf6e2e2b7d5
sets_current_task: CLI/MCP/TASKS/L02_01.md
copy:
  - PAYLOAD/MASTER.md -> CLI/MCP/MASTER.md
  - PAYLOAD/STATUS.md -> CLI/MCP/STATUS.md
  - PAYLOAD/TASK.md -> CLI/MCP/TASKS/L02_01.md
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
