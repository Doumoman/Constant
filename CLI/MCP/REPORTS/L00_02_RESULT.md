# L00_02_RESULT

## TASK

CLI/MCP/TASKS/L00_02.md (L00_02_LOCK — LIVE00_02_LOCK_LIVE_CONTRACTS_ALLOWLISTS_TEST_SCENES_AND_RESULT_FORMATS)

## STATUS

STATUS: PASS

## SUMMARY

라이브 통합 구현 착수 전 계획을 잠갔다(`CLI/MCP/INPUTS/LIVE_LOCK.md`, LOCK_STATE: FILLED_BY_L00_02). 경로 토큰 7종은 과제 지정값 그대로(LIVE_SRC 조사에서 차단 사유 없음 확인), 남은 10과제 전부의 읽기/쓰기 allowlist, ActionId 6종·키보드 바인딩 잠금, 라이브 asmdef 2본 계획, 씬/프리팹/부트스트랩/요청 소비/MAP 어댑터/HUD/PlayMode/빌드 계획, RESULT 형식 잠금(공통 11절 + 구현 5절), CHANGE CONTROL 규칙, 위험 5건을 확정했다. 프로젝트 코드/자산 변경 0건 — 보고·계약만.

## READ

- CLI/MCP/ENTRY.md, RULES.md, STATUS.md, MASTER.md, INPUTS/CHAR_EXIT.md, INPUTS/LIVE_SRC.md, REPORTS/L00_01_RESULT.md
- CharacterDesign/MCP/REPORTS/CHAR06_04 RESULT, INPUTS/CHAR00_SOURCE_REGISTRY.md
- Assets/_Game/Character/Runtime/**(계약 표면), Assets/_Game/Map/Runtime/**(도메인 표면), 테스트 트리 2종(기준선), Assets/_Game/Scenes/**, *.inputactions(레거시 2), *.prefab(_Game 0/레거시 226), Packages/manifest.json, ProjectSettings/EditorBuildSettings.asset(씬 10)

## CHANGED

- 없음 (씬/프리팹/입력 자산/MonoBehaviour/테스트/asmdef/UI/빌드 설정/런타임 코드 생성·수정 0건 — 과제의 report-only 제약 준수)

## CREATED

- CLI/MCP/INPUTS/LIVE_LOCK.md (라이브 잠금 — 요구 20절 전부 포함)
- CLI/MCP/REPORTS/L00_02_RESULT.md (본 REPORT)

## ENTRY_GATE

- Current Task = CLI/MCP/TASKS/L00_02.md ✓ (매니페스트 적용: 페이로드 3건 sha256 일치)
- L00_01 RESULT 존재 + sha256 `4e982e431d05a0c01dccac9062327068ea51a7ff713dfe281796a3dd9846d69b` 일치 + "STATUS: PASS"/"REGISTRY_STATE: FILLED_BY_L00_01" 존재 ✓
- CLI/MCP/INPUTS/LIVE_SRC.md 존재 + REGISTRY_STATE 마커 ✓
- L01_01 이후 10개 과제 LOCKED ✓

## LOCKED_PATHS

과제 지정 토큰 그대로 잠금(LIVE_SRC 조사상 차단 사유 없음):

```text
LIVE_RUNTIME: Assets/_Game/Live/Runtime/**
LIVE_INPUT: Assets/_Game/Live/Input/**
LIVE_PREFABS: Assets/_Game/Live/Prefabs/**
LIVE_SCENES: Assets/_Game/Scenes/Live/**
LIVE_PLAYMODE: Assets/_Game/Tests/PlayMode/Character/**
READONLY_PRECEDENT: Assets/_Legacy/**, Assets/2D Fantasy sprite bundle/**
FORBIDDEN_KEEP: Assets/_Game/Character/Runtime/**, Assets/_Game/Map/Runtime/**
```

## TASK_ALLOWLISTS

남은 10과제 전부 구체 allowlist 확정 — LIVE_LOCK.md TASK_ALLOWLISTS 표 참조. 요지:

- L01_01(입력): LIVE_RUNTIME+LIVE_INPUT, `Game.Character.Live.asmdef` 신설 1회
- L01_02(프리팹): +LIVE_PREFABS, `CharacterLiveTest.unity` 신설 1회
- L01_03(스폰)/L02_01(루트·카메라)/L03_01(도구)/L03_02(HUD): LIVE_* 조합
- L02_02(MAP 어댑터): LIVE_RUNTIME Adapter 하위만 — MAP 런타임 불가침
- L02_03(방 감사): REPORTS만
- L04_01(PlayMode): LIVE_PLAYMODE + asmdef 신설
- L04_02(최종): REPORTS + Builds/Validation + EditorBuildSettings 1건 한정 예외
- 전 과제: 순수 Character/MAP 계약 읽기 전용, 수정은 별도 CHANGE CONTROL 패키지로만

## CONTRACT_LOCKS

- ActionId 6종 {Move, Down, Jump, Action, Bomb, Rope} — 런타임 열거형 CharacterActionId {Jump, Action, SafeDrop, Bomb, Rope} 불변(Move/Down은 축, SafeDrop 파생)
- 키보드: Move=A/D·Left/Right, Down=S·Down, Jump=Space, Action=X, Bomb=Z, Rope=C — 레거시 E/F/Q 자산 사용 금지
- 금지 유지: basic attack/melee/shoot/dash/wall jump/double jump/신규 ActionId
- 방향 잠금: Live → Character 계약 → MAP 공용 (역방향 금지), 라이브 계층은 값 공급·요청 적용만(판정 재작성 금지)
- 물리: Rigidbody2D kinematic + 질의 전용 — Unity 물리 시뮬레이션/콜백은 판정 권위 아님

## TEST_AND_BUILD_LOCKS

- 회귀 기준선: Character EditMode 177 PASS 유지(감소 금지, 구현 과제마다 확인), MAP 13,536 기준 기록
- PlayMode: L04_01에서 `Game.Character.Tests.PlayMode` 신설 — 키보드 스모크 + 생성 런 스모크 + 이동 문법 라이브 재확인
- 빌드: StandaloneWindows64 유지(전환 금지), 출력 Builds/Validation/L04_02/**(비커밋), EditorBuildSettings 쓰기는 L04_02 한정 예외
- 브리지 리스크 완화 절차(실행 중 폴링 자제 + TestResults.xml 교차 확인) 잠금

## RESULT_FORMAT_LOCK

- 공통 11절: TASK/STATUS/SUMMARY/READ/CHANGED/CREATED/TESTS/BUILD/SCOPE_VALIDATION/FORBIDDEN_AUDIT/NEXT
- 구현 과제 추가 5절: LIVE_CONTRACTS_USED/REQUESTS_CONSUMED/ASSETS_WIRED/MANUAL_VERIFICATION/REGRESSION_BASELINE
- STATUS는 독립 라인 정확히 1개

## CHANGE_CONTROL

- 순수 계약·MAP 런타임·기존 테스트·잠금 바인딩 변경 = 별도 CHANGE CONTROL 패키지 필수
- allowlist 밖 쓰기 필요 = STATUS: BLOCKED + 경로·사유 기록(임의 확장 금지)
- 라이브 코드 버그픽스는 후속 과제에서 허용하되 CHANGED 명기

## RISKS

LIVE_LOCK.md KNOWN_RISKS 5건: ① unity-mcp 브리지 소켓 사망(완화 절차 잠금) ② MAP 파사드 부재로 어댑터 복잡도(CHAR06_02 검증 정책 재사용으로 완화) ③ 빌드 씬 전부 Legacy(L04_02에서 사용자 결정 보고) ④ 이동 이중 권위 위험(kinematic 잠금으로 차단) ⑤ 입력 프레임 순서(기존 CharacterInputBuffer 소비로 해결)

## SCOPE_VALIDATION

- 쓰기 2건만: CLI/MCP/INPUTS/LIVE_LOCK.md + 본 REPORT (허용 목록 그대로)
- Assets/Packages/ProjectSettings/MapDesign/CharacterDesign/Builds/Temp 변경 0건 (git status 확인)
- 후속 과제 미개방(L01_01 이후 10개 LOCKED 유지)

## NEXT

Current Task after finalize: NONE
Next Task auto-opened: NO (`L01_01_INPUT`은 LOCKED 유지, 새 INBOX 패키지로만 개방 — 개방 게이트는 LIVE_LOCK.md NEXT_TASK_GATE 참조)
