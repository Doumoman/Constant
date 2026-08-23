# CHAR06 — 생성 맵·최종 검증

## 목표

실제 생성 맵에서 필수·선택 경로와 전체 런을 확인하고 최종 컴파일·빌드·문서 게이트를 통과시킨다.

## 진입 조건

CHAR05 EXIT 승인 및 캐릭터 기능 코어 완료

## 종료 조건

생성 맵 회귀·Unity 테스트·빌드·최종 EXIT AUDIT가 모두 PASS

## 작업 목록

| 작업 | 내용 | TASK | RESULT |
|---|---|---|---|
| CHAR06_01 | 생성 맵 플레이어 생성·Type1/2/3 필수·Type0 선택 경로 검증 | `CHAR06_01_INTEGRATE_PLAYER_WITH_GENERATED_MAP_AND_ROUTES.md` | `CHAR06_01_INTEGRATE_PLAYER_WITH_GENERATED_MAP_AND_ROUTES_RESULT.md` |
| CHAR06_02 | 마이크로청크·방 전환·휴대물·폭탄·로프·무작위 런 검증 | `CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS.md` | `CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS_RESULT.md` |
| CHAR06_03 | 전체 컴파일·EditMode·PlayMode·빌드 검증 | `CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD.md` | `CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD_RESULT.md` |
| CHAR06_04 | RESULT·ALLOWLIST·커밋 증빙 및 최종 EXIT 감사 | `CHAR06_04_AUDIT_RESULTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT.md` | `CHAR06_04_AUDIT_RESULTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT_RESULT.md` |

## 단계 규칙

- 위 순서를 변경하지 않는다.
- 동시에 두 작업을 CURRENT로 만들지 않는다.
- 마지막 EXIT AUDIT가 PASS여도 다음 단계는 별도 OPEN 패치 전까지 LOCKED다.
