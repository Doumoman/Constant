# TASK RESULT

TASK: CHAR00_02_LOCK_GAMEPLAY_CONTRACTS_AND_TEST_FIXTURES
STATUS: PASS

## READ

- `CharacterDesign/MCP/00_MCP_ENTRYPOINT.md`, `01~05`, `07_PATCH_APPLY_RULES.md`, `08_STATUS_FINALIZE_RULES.md`
- `CharacterDesign/MCP/TASKS/CHAR00_02_LOCK_GAMEPLAY_CONTRACTS_AND_TEST_FIXTURES.md` (Current Task)
- `CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md` (sha256 `be6cadc4…` 검증)
- `CharacterDesign/MCP/REPORTS/CHAR00_01_INVENTORY_CHARACTER_INPUT_PHYSICS_MAP_RESULT.md` (`STATUS: PASS`, sha256 `1bc1a931…` 검증)
- `CharacterDesign/MCP/TEMPLATES/TASK_RESULT_TEMPLATE.md`
- WRITE 대상 16개 문서의 기존 내용(01_FIXED_SPEC 8, 03_DATA_SCHEMA 4, 04_TEST_FIXTURES 4)

## CHANGED

- `CharacterDesign/01_FIXED_SPEC/01_CHARACTER_GAMEPLAY_RULES.md` — 좌표(1 cell = 1 world unit)·콜라이더(0.72×0.90)·입력 의미(Jump=Space, X/Down+X/Z/C)·이동 문법·전투 경로·방 전환 KEEP·MAP 계약을 v2.0으로 잠금
- `CharacterDesign/01_FIXED_SPEC/02_CHARACTER_INPUT_RULES.md` — 논리 입력 7종과 키보드 기준 바인딩 잠금, 레거시 E/F/Q 불일치와 활성 자산 부재 기록, 조합 우선순위·스냅샷 원칙 고정
- `CharacterDesign/01_FIXED_SPEC/03_CHARACTER_MOVEMENT_RULES.md` — 결과 기반 이동 문법 + 물리·접지 기준선(CapsuleCast, probe 0.08, vy≤0.05, 경사 NONE) 잠금
- `CharacterDesign/01_FIXED_SPEC/04_CHARACTER_INTERACTION_RULES.md` — 단일 휴대 슬롯, 1×1 이하, Down+X 안전 내려놓기 overlap reject, 방향 투척 잠금
- `CharacterDesign/01_FIXED_SPEC/05_CHARACTER_COMBAT_RULES.md` — 일반 공격 금지, 6개 전투 경로 한정, 밟기 기절→두 번째 충격 제거 흐름 잠금
- `CharacterDesign/01_FIXED_SPEC/06_CHARACTER_MAP_INTEGRATION_RULES.md` — MAP 공용 계약 사용·Tilemap 내부 접근 금지·요청/결과 지형 변경·KEEP·Hysteresis 잠금, CHAR03_01 전 MAP API 의존성 명시
- `CharacterDesign/01_FIXED_SPEC/07_CHARACTER_TEST_RULES.md` — fixture ID 16개 canonical 규칙과 EditMode/PlayMode 분담 잠금
- `CharacterDesign/01_FIXED_SPEC/08_IMPLEMENTATION_ORDER.md` — 코드·테스트 배치 후보(Assets/_Game/Character/**, Tests/*/Character/**)와 asmdef 미승인 상태, CHAR03 전 외부 의존성 기록
- `CharacterDesign/03_DATA_SCHEMA/CHARACTER_ACTION_SCHEMA.md` — 고정 논리 행동 ID(Move/Down/Jump/Action/Bomb/Rope)와 조합·소비 규칙 잠금
- `CharacterDesign/03_DATA_SCHEMA/CHARACTER_DAMAGE_SCHEMA.md` — cause 열거를 전투 경로와 정합화(Stomp/ThrownObject/Explosion/ToolHit/EnemyContact/Spike/Fall/Crush/Environment), 기절→제거 규칙 잠금
- `CharacterDesign/03_DATA_SCHEMA/CHARACTER_INVENTORY_SCHEMA.md` — handSlot(정확히 1개) 필드 추가, 소모품과 휴대 슬롯 분리 잠금
- `CharacterDesign/03_DATA_SCHEMA/CHARACTER_MOVEMENT_TUNING_SCHEMA.md` — 단일 authoritative source, SO 기준선(형식 확정은 CHAR01), colliderSize/groundProbeDistance/groundedVyThreshold 기준선 필드 추가
- `CharacterDesign/04_TEST_FIXTURES/MOVEMENT_COURSE_SPEC.md` — fixture 6개 정의
- `CharacterDesign/04_TEST_FIXTURES/INTERACTION_COURSE_SPEC.md` — fixture 3개 정의
- `CharacterDesign/04_TEST_FIXTURES/COMBAT_COURSE_SPEC.md` — fixture 3개 정의
- `CharacterDesign/04_TEST_FIXTURES/ROOM_TRANSITION_COURSE_SPEC.md` — fixture 4개 정의

## CREATED

- `CharacterDesign/MCP/REPORTS/CHAR00_02_LOCK_GAMEPLAY_CONTRACTS_AND_TEST_FIXTURES_RESULT.md` (본 REPORT)

WRITE ALLOWLIST 밖 파일 변경 0. `06_IMPLEMENTATION_STATUS.md`와 `MASTER_IMPLEMENTATION_TASK_LIST.md`는 TASK EXECUTION에서 수정하지 않았다.

## TEST

| Test | Result |
|---|---|
| ContractsLocked | PASS — 좌표(1 cell=1 unit, 캡슐 0.72×0.90)·이동 문법(2셀 도달/2셀 통과/3셀 실패/금지 3종)·입력(Space/X/Down+X/Z/C)·물리(CapsuleCast, 0.08, vy≤0.05, 경사 NONE)·전투(일반 공격 금지, 6경로)·MAP(계약만 사용, 요청/결과, KEEP, Hysteresis)이 16개 문서에 모순 없이 고정됨 |
| FixturesLocked | PASS — 필수 fixture ID 16/16 존재(MOVEMENT 6, INTERACTION 3, COMBAT 3, ROOM_TRANSITION 4), 각각 setup/action/expected/failure 4요소 명시(grep 검증 16/16) |
| NoRuntimeMutation | PASS — `git status --porcelain -- Assets Packages MapDesign` 0건, ProjectSettings는 기존 사용자 변경 2건(ysls.json, ShaderGraphSettings — CHAR00_01 registry §0 기준선 기록분) 외 0건. C#/inputactions/asmdef/Scene/Prefab 생성·수정 0 |
| ReportExact | PASS — `status_control.result_file` 지정 경로 단일 REPORT, 독립된 `STATUS: PASS`, 실제 변경 목록 포함 |

## UNITY

- Unity Version: 6000.3.8f1 (`Application.unityVersion`)
- Compile Errors: 0 신규 (Console Error 필터 조회 0건)
- Relevant Warnings: 0 신규
- EditMode Tests: 미실행 — 코드·asset 무변경 문서 Task(no-code compile rationale: 컴파일 대상 변경이 없어 focused 테스트 지정 없음)
- PlayMode Tests: 미실행 — 동일 사유
- Scene/Prefab Changes: 0
- `EditorApplication.isCompiling = False`

## BLOCKERS

- NONE (CHAR00_02 범위 기준). 이월 의존성: 캐릭터용 MAP world query/mutation/boundary/readiness API 부재 — `CHAR03_01` 시작 전 별도 MAP 계약 승인 필요(06_CHARACTER_MAP_INTEGRATION_RULES와 08_IMPLEMENTATION_ORDER에 고정 기록).

## OUT_OF_SCOPE_FINDINGS

- `Assets/_Game/Tests/PlayMode/Map/Game.Map.Tests.PlayMode.asmdef`가 테스트 0개 상태로 레거시 `Game.Stage.Runtime`을 참조(stale) — CHAR00_01에서 최초 관찰, 본 Task 범위 밖이라 미수정.

## NEXT

- Current Task after finalize: NONE
- Next Task auto-opened: NO (`CHAR00_03_CHAR00_BASELINE_EXIT_AUDIT`는 LOCKED 유지, 새 MCP_INBOX 패키지로만 개방)
