# CHAR00_01 — 캐릭터·입력·물리·카메라·MAP 접점 조사

```yaml
status_control:
  task_key: CHAR00_01_INVENTORY_CHARACTER_INPUT_PHYSICS_MAP
  result_file: REPORTS/CHAR00_01_INVENTORY_CHARACTER_INPUT_PHYSICS_MAP_RESULT.md
```

## TASK TYPE

AUDIT

## Objective

활성·레거시 캐릭터 코드, 입력, 물리, 카메라룸, MAP 공용 계약을 읽기 전용 조사하고 source registry에 고정한다.

## READ ALLOWLIST

```text
CharacterDesign/**
Assets/**/*.cs
Assets/**/*.asmdef
Assets/**/*.inputactions
Packages/manifest.json
ProjectSettings/InputManager.asset
ProjectSettings/ProjectSettings.asset
MapDesign/**
```

## WRITE ALLOWLIST

```text
CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md
CharacterDesign/MCP/REPORTS/CHAR00_01_INVENTORY_CHARACTER_INPUT_PHYSICS_MAP_RESULT.md
```

## DO NOT

- Assets, Packages, ProjectSettings, MapDesign 수정
- 캐릭터 코드·입력 자산·asmdef 생성
- 다음 Task 시작
- commit/push

## Required Gates

1. `SourceRegistryCreated`
2. `NoProjectMutation`
3. `ResultPathExact`
4. `UnknownsExplicit`
5. `StatusExact`

## DONE CONDITIONS

- [x] registry가 `REGISTRY_STATE: FILLED_BY_CHAR00_01`이다.
- [x] 활성 캐릭터 런타임 부재와 레거시 compile 제외가 기록됐다.
- [x] 입력/물리/카메라/MAP 접점과 blocker가 기록됐다.
- [x] 프로젝트 구현 파일 변경이 0개다.
- [x] REPORT가 `STATUS: PASS`다.

## Result File

```text
REPORTS/CHAR00_01_INVENTORY_CHARACTER_INPUT_PHYSICS_MAP_RESULT.md
```

## Completion Rule

본 Task는 완료 이력 보존용이다. 다음 Task는 별도 MCP_INBOX patch로만 열린다.
