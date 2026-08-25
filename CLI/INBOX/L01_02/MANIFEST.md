# MANIFEST

```yaml
patch_id: L01_02_PREFAB
version: 1.0-short
requires_previous_task:
  path: CLI/MCP/REPORTS/L01_01_RESULT.md
  sha256: 4e269dddc9d49ab8a146dd4fe759b2471c516229d7828a44f84541ab3a41c983
  required_text:
    - "STATUS: PASS"
    - "None. Input provider only."
    - "Input actions asset only. No scene or prefab wiring."
    - "Current Task after finalize: NONE"
payload:
  task: PAYLOAD/TASK.md
  task_sha256: dcdbd108bdbd0b79971882ed7aafbb2b0fb40beab055be288b07b2f62e6df532
  status: PAYLOAD/STATUS.md
  status_sha256: 481debf5c13e92fdef99aaa2993d991978459361f65f0eeed9a7000d66e1e98f
  master: PAYLOAD/MASTER.md
  master_sha256: fa2764af8a459c565da6043f081f546311f9f6855846b232781d1059b0918a7c
sets_current_task: CLI/MCP/TASKS/L01_02.md
copy:
  - PAYLOAD/MASTER.md -> CLI/MCP/MASTER.md
  - PAYLOAD/STATUS.md -> CLI/MCP/STATUS.md
  - PAYLOAD/TASK.md -> CLI/MCP/TASKS/L01_02.md
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
