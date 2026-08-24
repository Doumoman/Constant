# CHAR00 — 기준선과 하네스

## 목표

기존 프로젝트를 조사하고 캐릭터 규칙·소유권·경로·테스트 기준을 확정한다.

## 진입 조건

CharacterDesign 하네스 설치 완료, 캐릭터 코드 변경 전

## 종료 조건

기존 접점과 실제 경로가 등록되고 이후 구현 작업을 실행할 수 있음

## 작업 목록

| 작업 | 내용 | TASK | RESULT |
|---|---|---|---|
| CHAR00_01 | 캐릭터·입력·물리·카메라·MAP 접점 조사 | `CHAR00_01_INVENTORY_CHARACTER_INPUT_PHYSICS_MAP.md` | `CHAR00_01_INVENTORY_CHARACTER_INPUT_PHYSICS_MAP_RESULT.md` |
| CHAR00_02 | 게임플레이 계약·소유권·고정 테스트룸 확정 | `CHAR00_02_LOCK_GAMEPLAY_CONTRACTS_AND_TEST_FIXTURES.md` | `CHAR00_02_LOCK_GAMEPLAY_CONTRACTS_AND_TEST_FIXTURES_RESULT.md` |
| CHAR00_03 | 기준선·하네스 종료 감사 | `CHAR00_03_CHAR00_BASELINE_EXIT_AUDIT.md` | `CHAR00_03_CHAR00_BASELINE_EXIT_AUDIT_RESULT.md` |

## 단계 규칙

- 위 순서를 변경하지 않는다.
- 동시에 두 작업을 CURRENT로 만들지 않는다.
- 마지막 EXIT AUDIT가 PASS여도 다음 단계는 별도 OPEN 패치 전까지 LOCKED다.
