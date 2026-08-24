# Character Master Implementation Task List

```text
VERSION: 2.0
CREATED: 2026-08-25
TOTAL PHASES: 7
TOTAL TASKS: 26
WORKFLOW: MCP_INBOX → TASK → REPORTS → STATUS FINALIZE → STOP
```

## 전역 고정값

| 항목 | 고정값 |
|---|---|
| Logical cell | 1 world unit |
| Player collider baseline | Capsule 0.72 × 0.90 |
| Jump grammar | 2-cell height reachable |
| Gap grammar | 2-cell pass / 3-cell basic fail |
| Forbidden movement | wall jump / dash / double jump |
| Input | Space / X / Down+X / Z / C |
| Combat | no separate basic attack |
| Room transition | input KEEP / velocity KEEP / readiness gate / hysteresis |
| MAP dependency | public coordinate/query/mutation contract only |

## 실행 규칙

1. 아래 순서대로만 진행한다.
2. Task는 별도 MCP_INBOX patch로만 CURRENT가 된다.
3. REPORT가 정확히 PASS일 때만 STATUS FINALIZE한다.
4. STATUS FINALIZE는 다음 Task를 자동으로 열지 않는다.
5. 미래 Task body는 patch가 열기 전 `MCP/TASKS/`에 설치하지 않는다.

## Phase 요약

| Phase | Task 수 | 기준 상태 |
|---|---:|---|
| CHAR00 기준선·하네스 | 3 | COMPLETE |
| CHAR01 핵심 이동 | 4 | COMPLETE |
| CHAR02 이동 문법 검증 | 3 | COMPLETE |
| CHAR03 MAP·방 전환 | 3 | COMPLETE |
| CHAR04 상호작용·접촉 전투 | 4 | COMPLETE |
| CHAR05 장비·생존·런 | 5 | COMPLETE |
| CHAR06 생성 맵·최종 검증 | 4 | 3 COMPLETE / 1 CURRENT |
| **합계** | **26** | **25 COMPLETE / 1 CURRENT / 0 LOCKED** |

---

## CHAR00 — 기준선과 하네스

- [x] `CHAR00_01_INVENTORY_CHARACTER_INPUT_PHYSICS_MAP` — 캐릭터·입력·물리·카메라·MAP 접점을 조사한다.
- [x] `CHAR00_02_LOCK_GAMEPLAY_CONTRACTS_AND_TEST_FIXTURES` — 게임플레이 계약·소유권·고정 테스트룸을 확정한다.
- [x] `CHAR00_03_CHAR00_BASELINE_EXIT_AUDIT` — 기준선과 하네스의 완전성·모순·미해결 의존성을 감사한다.

## CHAR01 — 핵심 이동

- [x] `CHAR01_01_IMPLEMENT_INPUT_SNAPSHOT_AND_PLAYER_STATES` — 논리 입력 스냅샷·버퍼와 플레이어 상태를 구현한다.
- [x] `CHAR01_02_IMPLEMENT_COLLISION_QUERIES_AND_GROUND_MOTOR` — 충돌 질의·지지체·걷기·달리기·가감속을 구현한다.
- [x] `CHAR01_03_IMPLEMENT_JUMP_AIR_CONTROL_AND_LANDING` — 점프·가변 높이·코요테·공중 제어·착지를 구현한다.
- [x] `CHAR01_04_CHAR01_CORE_MOVEMENT_EXIT_AUDIT` — 핵심 이동 구현과 회귀를 감사한다.

## CHAR02 — 이동 문법 검증

- [x] `CHAR02_01_VALIDATE_TWO_CELL_HEIGHT_AND_GAP_RULES` — 2셀 높이와 동일 높이 2셀 틈 통과를 검증한다.
- [x] `CHAR02_02_VALIDATE_THREE_CELL_FAILURE_AND_FORBIDDEN_MOVEMENT` — 3셀 기본 실패와 금지 이동 부재를 검증한다.
- [x] `CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT` — 이동 문법 전체를 감사하고 CHAR02 exit 여부를 판정한다.

## CHAR03 — MAP·방 전환

- [x] `CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE` — MAP 좌표·월드 질의와 방 경계 준비 게이트를 연결한다.
- [x] `CHAR03_02_IMPLEMENT_CAMERA_ROOM_TRANSITION_POLICY` — 카메라룸 전환·입력 KEEP·속도 KEEP·Hysteresis를 구현한다.
- [x] `CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT` — MAP 의존성 방향과 방 전환 회귀를 감사한다.

## CHAR04 — 상호작용·접촉 전투

- [x] `CHAR04_01_IMPLEMENT_CARRY_DROP_AND_DIRECTIONAL_THROW` — 단일 휴대·안전 내려놓기·방향 투척을 구현한다.
- [x] `CHAR04_02_IMPLEMENT_STOMP_STUN_REMOVAL_AND_CONTACT_DAMAGE` — 밟기·기절·제거·측면 접촉 피해 후보를 구현한다.
- [x] `CHAR04_03_IMPLEMENT_IMPACT_CONTRACT_AND_NO_BASIC_ATTACK` — 투척·환경 충격 계약과 일반 공격 부재를 검증한다.
- [x] `CHAR04_04_CHAR04_INTERACTION_COMBAT_EXIT_AUDIT` — 상호작용·접촉 전투 전체를 감사한다.

## CHAR05 — 장비·생존·런

- [x] `CHAR05_01_IMPLEMENT_BOMB_AND_TERRAIN_MUTATION_REQUEST` — 폭탄·폭발 피해·MAP 지형 변경 요청을 구현한다.
- [x] `CHAR05_02_IMPLEMENT_ROPE_CLIMBING_AND_TRAVERSAL_SUPPORT` — 로프 설치·등반·경계 제한을 구현한다.
- [x] `CHAR05_03_IMPLEMENT_HEALTH_HAZARDS_DEATH_AND_RUN_FAILURE` — 체력·위험·사망·런 실패·복귀를 구현한다.
- [x] `CHAR05_04_IMPLEMENT_RUN_STATE_HUD_AND_PRESENTATION_BRIDGE` — 런 상태·인벤토리·HUD·프레젠테이션 이벤트를 연결한다.
- [x] `CHAR05_05_CHAR05_EQUIPMENT_SURVIVAL_EXIT_AUDIT` — 장비·생존·런 상태를 감사한다.

## CHAR06 — 생성 맵·최종 검증

- [x] `CHAR06_01_INTEGRATE_PLAYER_WITH_GENERATED_MAP_AND_ROUTES` — 생성 맵 시작·필수·선택 경로와 플레이어 통합 요청을 구현한다.
- [x] `CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS` — 방·마이크로청크·아이템·도구·무작위 런을 검증한다.
- [x] `CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD` — 전체 compile·EditMode·PlayMode·build를 검증한다.
- [>] `CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT` — CURRENT: REPORTS·ALLOWLIST·변경 증빙과 최종 EXIT를 감사한다.

