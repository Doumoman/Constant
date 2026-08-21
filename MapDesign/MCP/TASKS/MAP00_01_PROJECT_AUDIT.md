# MAP00_01 — Existing Unity Project Audit

## TASK TYPE

```text
READ-ONLY AUDIT
```

프로젝트 구현 파일을 수정하지 않는다.
감사 보고서 1개만 생성한다.

---

# 1. Objective

현재 Unity 프로젝트의 구조를 제한적으로 조사해서,
광역 맵 생성 모듈을 기존 코드와 충돌 없이 어디에 배치할지 결정할 근거를 만든다.

이 TASK에서는 실제 맵 생성 코드를 작성하지 않는다.

---

# 2. 반드시 먼저 읽을 파일

MCP Starter:

- `00_MCP_ENTRYPOINT.md`
- `01_PROJECT_LOCKED_RULES.md`
- `02_MCP_WORK_RULES.md`
- `03_DATA_CSV_RULES.md`
- `04_UNITY_MCP_RULES.md`
- `05_CHANGE_CONTROL_RULES.md`
- `06_IMPLEMENTATION_STATUS.md`
- 현재 이 TASK

---

# 3. Project READ ALLOWLIST

아래는 내용 확인 허용.

## Unity/Package

- `ProjectSettings/ProjectVersion.txt`
- `Packages/manifest.json`
- `Packages/packages-lock.json`가 존재하면 읽기

## Assembly

프로젝트 내 모든 `*.asmdef`:
- 먼저 경로 목록만 수집
- 그 후 각 asmdef 내용 확인

`*.asmref`가 존재하면 동일하게 확인.

## Folder Structure

`Assets/` 아래:
- depth 1~3 폴더 이름
- 파일명 목록

파일 내용은 아래 Map 관련 후보만 제한적으로 확인:

파일명/경로에 다음 키워드가 포함된 C#:

```text
Map
World
Level
Stage
Tile
Tilemap
Grid
Dungeon
Generation
Generator
Procedural
Room
Chunk
Biome
```

제한:
- 최대 30개 C# 파일 내용 확인
- 후보가 30개를 넘으면 우선 경로/클래스 이름만 보고하고 내용은 임의 확장하지 않는다

## Test

아래 이름이 포함된 테스트 폴더/asmdef/파일:
- Test
- Tests
- EditorTest
- PlayMode
- EditMode

---

# 4. READ 금지

이 TASK에서 내용 전체를 읽지 않는다.

- 전체 GDD
- 전체 CSV 패키지
- 모든 Scene YAML
- 모든 Prefab YAML
- Texture/Sprite/Audio
- Library/
- Temp/
- Logs/
- obj/
- 빌드 결과물
- Git history 전체

Scene/Prefab은 파일명 목록 정도만 필요하면 확인 가능하고 내용은 읽지 않는다.

---

# 5. WRITE ALLOWLIST

아래 보고서 하나만 생성한다.

```text
REPORTS/MAP00_01_PROJECT_AUDIT_RESULT.md
```

프로젝트의 Assets/Packages/ProjectSettings 파일은 수정 금지.

`06_IMPLEMENTATION_STATUS.md`도 이 TASK에서는 수정하지 않는다.

---

# 6. Audit 항목

보고서에 아래를 정확히 기록한다.

## A. Unity

```text
Unity Version:
Render Pipeline package:
2D Tilemap 관련 package:
Unity Test Framework:
Addressables 존재 여부:
Input System 존재 여부:
```

없는 것은 `NOT FOUND`.

## B. Assembly

표:

| asmdef | path | root namespace | 주요 references | Editor only? | Test? |
|---|---|---|---|---|---|

## C. Namespace Convention

기존 C#에서 가장 많이 사용되는 namespace 패턴을 확인한다.

예:

```text
Game.*
ProjectName.*
Global namespace
Mixed
```

추측하지 말고 실제 후보 파일 근거를 적는다.

## D. Existing Map Systems

Map/World/Level/Tilemap/Grid 관련 기존 코드:

| class/file | path | responsibility guess based on code | conflict risk |
|---|---|---|---|

`conflict risk`:
- NONE
- LOW
- MEDIUM
- HIGH

내용을 읽지 않은 파일은 responsibility를 추측하지 않는다.

## E. Test Structure

- EditMode test 위치
- PlayMode test 위치
- test asmdef
- 테스트를 추가할 권장 위치

## F. Recommended Module Placement

기존 구조를 따르는 배치안만 제안한다.

아직 생성하지 않는다.

필요하면 아래를 제안:

```text
Runtime Map Domain
Runtime Map Data
Runtime Map Generation
Editor Map Tools
Map Tests
MapDesign Authoring Data
```

각 경로는 프로젝트의 기존 convention을 기반으로 해야 한다.

## G. Assembly Plan

새 asmdef가 필요한지 판단한다.

필요하다면 이름/위치/참조만 제안하고 생성하지 않는다.

기존 asmdef에 넣는 것이 더 자연스러우면 그렇게 제안한다.

## H. Collision / Risk

MAP00_02 전에 해결해야 하는 충돌을 기록한다.

예:
- 기존 WorldGenerator 존재
- Map namespace 이름 충돌
- Test asmdef가 Runtime assembly 참조 불가
- CSV loader 이미 존재
- Tilemap wrapper 이미 존재

---

# 7. Unity MCP

이 TASK는 코드 변경이 없으므로 compile을 강제로 유발하지 않는다.

가능하면 Unity Editor가 프로젝트를 정상적으로 열고 있는지만 확인한다.

보고:

```text
Unity Editor Reachable: YES / NO / NOT CHECKED
Existing Console Errors: N / NOT CHECKED
```

기존 Console Error를 고치지 않는다.

---

# 8. DONE CONDITIONS

아래가 모두 충족되면 PASS.

- [ ] Unity Version 확인
- [ ] asmdef/asmref 목록과 내용 확인
- [ ] Assets depth 1~3 구조 확인
- [ ] Map 관련 기존 코드 후보 조사
- [ ] Test 구조 확인
- [ ] namespace convention 확인
- [ ] 새 Map 모듈의 권장 위치 제안
- [ ] asmdef 계획 제안
- [ ] 충돌 위험 기록
- [ ] 프로젝트 구현 파일 수정 0개
- [ ] Audit Result 파일 1개만 생성

---

# 9. PASS 후

PASS 후에도 MAP00_02를 자동으로 시작하지 않는다.

마지막 보고에만 다음을 적는다.

```text
NEXT TASK READY:
MAP00_02_FOLDER_AND_ASMDEF_PLAN = YES / NO
```
