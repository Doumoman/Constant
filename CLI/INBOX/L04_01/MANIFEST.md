# MANIFEST

```yaml
patch_id: L04_01_PLAYMODE
version: 1.0-short
requires_previous_task:
  path: CLI/MCP/REPORTS/L03_02_RESULT.md
  sha256: 48c5b2c466fb2d70c88afac9770d522c1132640870563402ca76b22015acaa2c
  required_text:
    - "STATUS: PASS"
    - "live scene HUD binding"
    - "no input asset, audio, animation, save, Character runtime, MAP runtime, or tool consumer wiring"
    - "Current Task after finalize: NONE"
    - "Next Task auto-opened: NO"
payload:
  task: PAYLOAD/TASK.md
  task_sha256: 935e9b3ef6e9391dbfbc6ef7e1a7702cc8a3686b86ac595b3ff7aa40be0975ae
  status: PAYLOAD/STATUS.md
  status_sha256: 1994f797c26c855e0f4b706701ea7f81bfe3da43eaa61d9d7e28fe3390a8896a
  master: PAYLOAD/MASTER.md
  master_sha256: 84bcd0a12c3a8b60b060879056b96703dde228cc965d919f13f72178bedb3872
sets_current_task: CLI/MCP/TASKS/L04_01.md
copy:
  - PAYLOAD/MASTER.md -> CLI/MCP/MASTER.md
  - PAYLOAD/STATUS.md -> CLI/MCP/STATUS.md
  - PAYLOAD/TASK.md -> CLI/MCP/TASKS/L04_01.md
allowed_project_writes:
  - Assets/_Game/Tests/PlayMode/Character/**
  - CLI/MCP/REPORTS/L04_01_RESULT.md
forbidden:
  - Assets/_Game/Character/Runtime/**
  - Assets/_Game/Map/Runtime/**
  - Assets/_Game/Live/Runtime/**
  - Assets/_Game/Live/Input/**
  - Assets/_Game/Live/Prefabs/**
  - Assets/_Game/Scenes/**
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
