# CHAR00_01 — 캐릭터·입력·물리·카메라·MAP 접점 조사

TASK ID: CHAR00_01  
PHASE: CHAR00  
STATE SOURCE: `CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md`  
DEPENDS ON: NONE

## 목표

기존 프로젝트를 조사하고 캐릭터 규칙·소유권·경로·테스트 기준을 확정한다.

현재 작업 범위는 **캐릭터·입력·물리·카메라·MAP 접점 조사**에 한정한다.

## READ ALLOWLIST

- `CharacterDesign/**`
- `Assets/**/*.cs`
- `Assets/**/*.asmdef`
- `Assets/**/*.inputactions`
- `Packages/manifest.json`
- `ProjectSettings/InputManager.asset`
- `ProjectSettings/ProjectSettings.asset`
- `MapDesign/**`

## WRITE ALLOWLIST

- `CharacterDesign/MCP/RESULTS/CHAR00_01_INVENTORY_CHARACTER_INPUT_PHYSICS_MAP_RESULT.md`
- `CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md`

ALLOWLIST NOTE: 조사 전용. Assets, Packages, ProjectSettings, MapDesign는 읽기만 허용한다.

## 구현 요구사항

- 현재 TASK의 유일한 구현 목표는 ‘캐릭터·입력·물리·카메라·MAP 접점 조사’이다.
- 현재 TASK에 필요하지 않은 기존 코드를 리팩터링하거나 공개 API 이름을 변경하지 않는다.
- 새 전역 싱글톤, 새 asmdef, 새 입력 프레임워크를 임의로 추가하지 않는다.
- 게임플레이 로직을 Animator 이벤트 또는 렌더 프레임에 종속시키지 않는다.
- 고정 규칙과 충돌하거나 ALLOWLIST가 부족하면 확장하지 말고 BLOCKED로 보고한다.
- 프로젝트 사실을 조사 결과와 추측으로 구분한다.
- 기존 사용자 변경을 수정하거나 정리하지 않는다.

## 금지사항

- WRITE ALLOWLIST 밖 파일 수정
- 다음 TASK 선행 구현
- 기존 파일 삭제·이동·이름 변경
- 테스트 수 감소, Ignore 처리 또는 통과 결과 조작
- 사용자 지시 없는 git push

## 고정 테스트

TEST COUNT: 5

1. SourceRegistryCreated: 조사한 경로·assembly·입력·물리·카메라·MAP 접점이 registry에 기록됨
2. NoProjectMutation: Assets/Packages/ProjectSettings/MapDesign 변경 파일 0개
3. ResultPathExact: 지정 RESULT 파일명과 경로 일치
4. UnknownsExplicit: 확인하지 못한 항목이 추측 대신 UNKNOWN/BLOCKER로 기록됨
5. StatusExact: 결과 상태가 PASS, FAIL, BLOCKED 중 하나로 정확히 기록됨

## RESULT 계약

RESULT PATH: `CharacterDesign/MCP/RESULTS/CHAR00_01_INVENTORY_CHARACTER_INPUT_PHYSICS_MAP_RESULT.md`

RESULT는 다음을 포함한다.

- 독립된 상태 줄 `STATUS: PASS`, `STATUS: FAIL` 또는 `STATUS: BLOCKED`
- 실제 변경 파일 전체
- 구현한 세부 내용
- 컴파일 결과
- 고정 테스트 5개의 개별 결과
- 잔여 문제와 재현 정보

PASS가 아니면 FINALIZE할 수 없다.

## 커밋 계약

권장 제목: `CHAR00_01: 캐릭터·입력·물리·카메라·MAP 접점 조사`

커밋 본문에는 구현 세부 사항, 테스트 결과, 남은 제한을 기록한다. RESULT 작성과 검증 전에는 커밋하지 않는다.
