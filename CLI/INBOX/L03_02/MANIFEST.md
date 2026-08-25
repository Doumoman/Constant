# MANIFEST

```yaml
patch_id: L03_02_HUD
version: 1.0-short
requires_previous_task:
  path: CLI/MCP/REPORTS/L03_01_RESULT.md
  sha256: 275fc4ff71a146ee819444f19a9b548ab00de379c5f6cb4a422dc941aa2b9563
  required_text:
    - "STATUS: PASS"
    - "None. Runtime consumers only; no scene, prefab, HUD, audio, animation, save, or input asset wiring."
    - "Current Task after finalize: NONE"
    - "Next Task auto-opened: NO"
payload:
  task: PAYLOAD/TASK.md
  task_sha256: 697c9524d4c689c6111d916a7830e8afef5afaf26a6f6a55b990f30adda8325e
  status: PAYLOAD/STATUS.md
  status_sha256: 7cd659e7d4e8efce3c00e9d10ed8cc24dc226e5e14d22196b973a1e50b40f530
  master: PAYLOAD/MASTER.md
  master_sha256: 63d23185e5564d48d6565f8a69aa825e1405860877566a80f41370123133b9df
sets_current_task: CLI/MCP/TASKS/L03_02.md
copy:
  - PAYLOAD/MASTER.md -> CLI/MCP/MASTER.md
  - PAYLOAD/STATUS.md -> CLI/MCP/STATUS.md
  - PAYLOAD/TASK.md -> CLI/MCP/TASKS/L03_02.md
allowed_project_writes:
  - Assets/_Game/Live/Runtime/Hud/**
  - Assets/_Game/Live/Runtime/Presentation/**
  - Assets/_Game/Live/Prefabs/**
  - Assets/_Game/Scenes/Live/**
  - CLI/MCP/REPORTS/L03_02_RESULT.md
forbidden:
  - Assets/_Game/Character/Runtime/**
  - Assets/_Game/Map/Runtime/**
  - Assets/_Game/Live/Input/**
  - Assets/_Game/Live/Runtime/Tools/**
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
