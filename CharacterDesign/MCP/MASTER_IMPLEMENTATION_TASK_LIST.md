# Character Master Implementation Task List

VERSION: 1.2  
CREATED: 2026-08-23  
TOTAL PHASES: 7  
TOTAL TASKS: 26

## 고정 체인

`CHAR00 → CHAR01 → CHAR02 → CHAR03 → CHAR04 → CHAR05 → CHAR06`

## 작업 목록

| 작업 | 단계 | 내용 | 선행 작업 | 초기 상태 | TASK 파일 |
|---|---|---|---|---|---|
| CHAR00_01 | 기준선과 하네스 | 캐릭터·입력·물리·카메라·MAP 접점 조사 | NONE | CURRENT | `CHAR00_01_INVENTORY_CHARACTER_INPUT_PHYSICS_MAP.md` |
| CHAR00_02 | 기준선과 하네스 | 게임플레이 계약·소유권·고정 테스트룸 확정 | CHAR00_01 | LOCKED | `CHAR00_02_LOCK_GAMEPLAY_CONTRACTS_AND_TEST_FIXTURES.md` |
| CHAR00_03 | 기준선과 하네스 | 기준선·하네스 종료 감사 | CHAR00_02 | LOCKED | `CHAR00_03_CHAR00_BASELINE_EXIT_AUDIT.md` |
| CHAR01_01 | 핵심 이동 | 논리 입력 스냅샷·버퍼와 플레이어 상태 머신 구현 | CHAR00_03 | LOCKED | `CHAR01_01_IMPLEMENT_INPUT_SNAPSHOT_AND_PLAYER_STATES.md` |
| CHAR01_02 | 핵심 이동 | 충돌 질의·지지체 추적·걷기·달리기·가감속 구현 | CHAR01_01 | LOCKED | `CHAR01_02_IMPLEMENT_COLLISION_QUERIES_AND_GROUND_MOTOR.md` |
| CHAR01_03 | 핵심 이동 | 점프·가변 높이·코요테·공중 제어·낙하·착지 구현 | CHAR01_02 | LOCKED | `CHAR01_03_IMPLEMENT_JUMP_AIR_CONTROL_AND_LANDING.md` |
| CHAR01_04 | 핵심 이동 | 핵심 이동 종료 감사 | CHAR01_03 | LOCKED | `CHAR01_04_CHAR01_CORE_MOVEMENT_EXIT_AUDIT.md` |
| CHAR02_01 | 이동 문법 검증 | 2셀 높이 점프와 동일 높이 2셀 틈 달리기 검증 | CHAR01_04 | LOCKED | `CHAR02_01_VALIDATE_TWO_CELL_HEIGHT_AND_GAP_RULES.md` |
| CHAR02_02 | 이동 문법 검증 | 3셀 기본 통과 실패와 벽 점프·대시·이중 점프 부재 검증 | CHAR02_01 | LOCKED | `CHAR02_02_VALIDATE_THREE_CELL_FAILURE_AND_FORBIDDEN_MOVEMENT.md` |
| CHAR02_03 | 이동 문법 검증 | 이동 문법 종료 감사 | CHAR02_02 | LOCKED | `CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT.md` |
| CHAR03_01 | MAP·방 전환 연동 | MAP 좌표·월드 질의와 방 경계 준비 게이트 연결 | CHAR02_03 | LOCKED | `CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE.md` |
| CHAR03_02 | MAP·방 전환 연동 | 카메라룸 전환·입력 KEEP·속도 KEEP·Hysteresis 구현 | CHAR03_01 | LOCKED | `CHAR03_02_IMPLEMENT_CAMERA_ROOM_TRANSITION_POLICY.md` |
| CHAR03_03 | MAP·방 전환 연동 | MAP·방 전환 종료 감사 | CHAR03_02 | LOCKED | `CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT.md` |
| CHAR04_01 | 상호작용과 접촉 전투 | Carryable 검색·휴대·안전 내려놓기·방향 투척 구현 | CHAR03_03 | LOCKED | `CHAR04_01_IMPLEMENT_CARRY_DROP_AND_DIRECTIONAL_THROW.md` |
| CHAR04_02 | 상호작용과 접촉 전투 | 밟기·반동·첫 기절·두 번째 제거·측면 피격 구현 | CHAR04_01 | LOCKED | `CHAR04_02_IMPLEMENT_STOMP_STUN_REMOVAL_AND_CONTACT_DAMAGE.md` |
| CHAR04_03 | 상호작용과 접촉 전투 | 투척·환경 충격 계약과 일반 공격 부재 검증 | CHAR04_02 | LOCKED | `CHAR04_03_IMPLEMENT_IMPACT_CONTRACT_AND_NO_BASIC_ATTACK.md` |
| CHAR04_04 | 상호작용과 접촉 전투 | 상호작용·접촉 전투 종료 감사 | CHAR04_03 | LOCKED | `CHAR04_04_CHAR04_INTERACTION_COMBAT_EXIT_AUDIT.md` |
| CHAR05_01 | 장비·생존·런 상태 | 폭탄·퓨즈·폭발 피해·MAP 지형 변경 요청 구현 | CHAR04_04 | LOCKED | `CHAR05_01_IMPLEMENT_BOMB_AND_TERRAIN_MUTATION_REQUEST.md` |
| CHAR05_02 | 장비·생존·런 상태 | 로프·등반·방 경계 제한과 보조 이동 구현 | CHAR05_01 | LOCKED | `CHAR05_02_IMPLEMENT_ROPE_CLIMBING_AND_TRAVERSAL_SUPPORT.md` |
| CHAR05_03 | 장비·생존·런 상태 | 체력·피해·위험·사망·런 실패·복귀 구현 | CHAR05_02 | LOCKED | `CHAR05_03_IMPLEMENT_HEALTH_HAZARDS_DEATH_AND_RUN_FAILURE.md` |
| CHAR05_04 | 장비·생존·런 상태 | 런 상태·인벤토리·HUD·애니메이션/사운드 이벤트 연결 | CHAR05_03 | LOCKED | `CHAR05_04_IMPLEMENT_RUN_STATE_HUD_AND_PRESENTATION_BRIDGE.md` |
| CHAR05_05 | 장비·생존·런 상태 | 장비·생존·런 상태 종료 감사 | CHAR05_04 | LOCKED | `CHAR05_05_CHAR05_EQUIPMENT_SURVIVAL_EXIT_AUDIT.md` |
| CHAR06_01 | 생성 맵·최종 검증 | 생성 맵 플레이어 생성·Type1/2/3 필수·Type0 선택 경로 검증 | CHAR05_05 | LOCKED | `CHAR06_01_INTEGRATE_PLAYER_WITH_GENERATED_MAP_AND_ROUTES.md` |
| CHAR06_02 | 생성 맵·최종 검증 | 마이크로청크·방 전환·휴대물·폭탄·로프·무작위 런 검증 | CHAR06_01 | LOCKED | `CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS.md` |
| CHAR06_03 | 생성 맵·최종 검증 | 전체 컴파일·EditMode·PlayMode·빌드 검증 | CHAR06_02 | LOCKED | `CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD.md` |
| CHAR06_04 | 생성 맵·최종 검증 | RESULT·ALLOWLIST·커밋 증빙 및 최종 EXIT 감사 | CHAR06_03 | LOCKED | `CHAR06_04_AUDIT_RESULTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT.md` |

## 게이트

- 작업은 위 순서대로만 열린다.
- 각 작업은 RESULT의 정확한 `STATUS: PASS`가 필요하다.
- FINALIZE는 다음 작업을 자동으로 열지 않는다.
- FAIL/BLOCKED면 같은 작업을 CURRENT로 유지한다.
- 단계 마지막 EXIT AUDIT가 PASS여도 다음 단계는 별도 OPEN 패치 전까지 LOCKED다.
