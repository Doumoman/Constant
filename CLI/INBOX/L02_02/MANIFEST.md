# MANIFEST

```yaml
patch_id: L02_02_MAP_ADAPTER
version: 1.0-short
requires_previous_task:
  path: CLI/MCP/REPORTS/L02_01_RESULT.md
  sha256: a0e4288ba390cbed70263681efd7ee235ee84a82baa3b434f2a0ad15309e4585
  required_text:
    - "STATUS: PASS"
    - "CharacterRoomTransitionRequest and CharacterGeneratedRouteTransitionRequest consumed by live route/camera layer."
    - "No generated MAP adapter wiring"
    - "Current Task after finalize: NONE"
payload:
  task: PAYLOAD/TASK.md
  task_sha256: eec606d043f6d86ac76c3ea039ba979088c5715eb79eddd21ccd6ec4aab070d6
  status: PAYLOAD/STATUS.md
  status_sha256: fc0f800c5d7da2f0a64c156d55641536442569e4cca657de843aecc4edce0794
  master: PAYLOAD/MASTER.md
  master_sha256: 25360607bd2337eff210a526ec48b14726afc947a893964fc689e40b845af4aa
sets_current_task: CLI/MCP/TASKS/L02_02.md
copy:
  - PAYLOAD/MASTER.md -> CLI/MCP/MASTER.md
  - PAYLOAD/STATUS.md -> CLI/MCP/STATUS.md
  - PAYLOAD/TASK.md -> CLI/MCP/TASKS/L02_02.md
allowed_project_writes:
  - Assets/_Game/Live/Runtime/Adapters/**
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
  - future tasks
  - git commit
  - git push
```
