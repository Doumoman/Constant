# Map Implementation Status

## Generator Package

```text
Spec Baseline: GDD v0.3
Implementation Package Baseline: Map Package v1.0
MCP Starter Rules: v1.0
```

## Current Task

```text
TASKS/MAP00_02_FOLDER_AND_ASMDEF_PLAN.md
```

## Status

| Task | Status |
|---|---|
| MAP00_01_PROJECT_AUDIT | COMPLETE |
| MAP00_02_FOLDER_AND_ASMDEF_PLAN | CURRENT |
| MAP00_03_CREATE_MAP_MODULE_STRUCTURE | LOCKED |
| MAP00_04_CREATE_TEST_STRUCTURE | LOCKED |
| MAP01_* CSV FOUNDATION | LOCKED |
| MAP02_* WORLD GRID | LOCKED |
| MAP03_* SITE RESERVATION | LOCKED |
| MAP04_* BIOME PATCH | LOCKED |
| MAP05_* MANDATORY ROUTE | LOCKED |
| MAP06_* TYPE0 OPTIONAL | LOCKED |
| MAP07+ | LOCKED |

## MAP00_01 Confirmed Baseline

Audit result:

```text
REPORTS/MAP00_01_PROJECT_AUDIT_RESULT.md
STATUS: PASS
```

Confirmed project facts:

- Unity: `6000.3.8f1`
- Main map runtime assembly: `Game.Map.Runtime`
- Main map namespace root: `StarNight.Map`
- Map authoring editor assembly: `MapAuthoring.Editor`
- Map EditMode test assembly: `Game.Map.Tests.EditMode`
- Map PlayMode test assembly: `Game.Map.Tests.PlayMode`
- New broad world generator should use `StarNight.Map.WorldGeneration.*`
- Existing `StageMapGenerator`, legacy P6/P11 generator logic must not be used as the implementation base
- New asmdef is not currently required

## Current Rule

현재는 폴더·namespace·assembly 경계를 문서로 확정하는 작업만 수행한다.

실제 폴더 생성, C# 생성, asmdef 수정, CSV 생성은 하지 않는다.

`MAP00_02_FOLDER_AND_ASMDEF_PLAN`이 PASS한 뒤에만
`MAP00_03_CREATE_MAP_MODULE_STRUCTURE`를 시작한다.
