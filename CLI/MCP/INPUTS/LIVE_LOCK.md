# Live Integration Lock

```text
LOCK_STATE: FILLED_BY_L00_02
OWNER_TASK: L00_02_LOCK
```

## ENTRY_ANCHORS

```text
Character final exit: CharacterDesign/MCP/REPORTS/CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT_RESULT.md
  sha256 6efc2ac08d7cb52fd8ba260888310dd403ae64d191767a9338b174a0897fc96c
Live survey: CLI/MCP/REPORTS/L00_01_RESULT.md
  sha256 4e982e431d05a0c01dccac9062327068ea51a7ff713dfe281796a3dd9846d69b
Live source registry: CLI/MCP/INPUTS/LIVE_SRC.md (REGISTRY_STATE: FILLED_BY_L00_01)
Character source registry: CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md
  sha256 be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
```

## PATH_TOKENS (잠금 — LIVE_SRC 권고 그대로, 차단 사유 없음)

```text
LIVE_RUNTIME: Assets/_Game/Live/Runtime/**
LIVE_INPUT: Assets/_Game/Live/Input/**
LIVE_PREFABS: Assets/_Game/Live/Prefabs/**
LIVE_SCENES: Assets/_Game/Scenes/Live/**
LIVE_PLAYMODE: Assets/_Game/Tests/PlayMode/Character/**
READONLY_PRECEDENT: Assets/_Legacy/**, Assets/2D Fantasy sprite bundle/**
FORBIDDEN_KEEP: Assets/_Game/Character/Runtime/**, Assets/_Game/Map/Runtime/**
```

## TASK_ALLOWLISTS (남은 10과제 읽기/쓰기)

공통 READ(전 과제): CLI/MCP/**, LIVE_SRC/LIVE_LOCK, Character/MAP 런타임·테스트(읽기), Legacy·에셋팩(읽기 전용 선례), Packages/manifest.json(읽기)

| Task | WRITE 허용 | 비고 |
|---|---|---|
| L01_01_INPUT | LIVE_RUNTIME(Input 하위 + `Game.Character.Live.asmdef` 최초 생성), LIVE_INPUT(.inputactions), REPORTS | 라이브 asmdef 신설은 이 과제 1회 |
| L01_02_PREFAB | LIVE_RUNTIME, LIVE_PREFABS(Player/CameraRig), LIVE_SCENES(`CharacterLiveTest.unity` 최초 생성), REPORTS | 씬 신설은 이 과제 1회 |
| L01_03_SPAWN | LIVE_RUNTIME, LIVE_PREFABS(RunBootstrap), LIVE_SCENES, REPORTS | 스폰 요청 소비·런 시작 |
| L02_01_ROUTE_CAMERA | LIVE_RUNTIME, LIVE_PREFABS(CameraRig), LIVE_SCENES, REPORTS | CHAR03 정책 소비, 재작성 금지 |
| L02_02_MAP_ADAPTER | LIVE_RUNTIME(Adapter 하위만), REPORTS | MAP 런타임 불가침 — 도메인 표면→스냅샷 투영만 |
| L02_03_ROOM_AUDIT | REPORTS(+ARTIFACTS)만 | report-only 감사 |
| L03_01_TOOLS | LIVE_RUNTIME, LIVE_PREFABS(도구 오브젝트), LIVE_SCENES, REPORTS | 폭탄/로프/휴대 소비자 |
| L03_02_HUD | LIVE_RUNTIME(Hud 하위), LIVE_PREFABS(Hud), LIVE_SCENES, REPORTS | uGUI Canvas + 바인더 |
| L04_01_PLAYMODE | LIVE_PLAYMODE(`Game.Character.Tests.PlayMode.asmdef` 신설 포함), REPORTS | 테스트 신설, 라이브 코드 수정은 버그픽스 한정 명시 필요 |
| L04_02_FINAL | REPORTS(+ARTIFACTS), Builds/Validation/L04_02/**, ProjectSettings/EditorBuildSettings.asset(라이브 씬 등록 1건 한정 예외) | 예외는 해당 과제 문서에 명시된 경우만 유효 |

전 과제 공통: 순수 Character/MAP 런타임 계약은 읽기 전용 — 수정 필요 시 별도 CHANGE CONTROL 패키지로만.

## READONLY_PRECEDENTS

```text
Assets/_Legacy/** — 플레이어/카메라/UI/도구 구현 선례, 수치 선례(최대 체력 4, 로프 6/4f, 시작 4/4 등 이미 캐릭터 계약에 반영됨)
Assets/2D Fantasy sprite bundle/** — 아트 소스(스프라이트/타일). 프리팹 참조는 복사 아닌 링크로
Assets/_Game/Tests/EditMode/** — 회귀 기준선(Character 177 / MAP 13,536), 수정 금지
```

## FORBIDDEN_GLOBALS

```text
Assets/_Game/Character/Runtime/** 재작성·수정 (순수 계약 잠금)
Assets/_Game/Map/Runtime/** 수정 (MAP 하니스 소관)
MapDesign/**, CharacterDesign/** 수정
기존 EditMode 테스트 수정·축소·Ignore
Legacy 코드 활성화·수정
MAP 런타임 → Character 의존 추가 (역방향 금지)
테스트/빌드/콘솔 결과 은폐·조작
```

## ACTION_ID_LOCK

```text
Move, Down, Jump, Action, Bomb, Rope — 정확히 이 6개 논리 입력만
런타임 열거형 CharacterActionId {Jump, Action, SafeDrop, Bomb, Rope} 불변 (Move/Down은 축, SafeDrop = Down+Action 파생 — 기존 계약 그대로)
신규 ActionId 추가 금지
```

## INPUT_BINDING_LOCK

```text
Move: A/D 또는 Left/Right
Down: S 또는 Down
Jump: Space
Action: X
Bomb: Z
Rope: C
도입 금지: basic attack / melee / shoot / dash / wall jump / double jump
공급 방식 잠금: Input System 1.18.0 폴링 기반 → 프레임당 CharacterInputSnapshot 구성(버퍼·코요테는 기존 순수 계약이 소유; 라이브 계층은 값 공급만)
레거시 StarNightControls.inputactions(E/F/Q) 사용 금지 — 신규 CharacterLiveControls.inputactions
```

## LIVE_ASSEMBLY_PLAN

```text
신규 asmdef: Assets/_Game/Live/Runtime/Game.Character.Live.asmdef
  references: ["Game.Character.Runtime", "Game.Map.Runtime", "Unity.InputSystem"]
  autoReferenced: false — 생성: L01_01
신규 asmdef: Assets/_Game/Tests/PlayMode/Character/Game.Character.Tests.PlayMode.asmdef
  references: ["Game.Character.Live", "Game.Character.Runtime", "Game.Map.Runtime", "Unity.InputSystem"] + TestAssemblies
  — 생성: L04_01
네임스페이스: StarNight.Character.Live.{Input, Bootstrap, Adapter, Consumers, Hud}
방향: Live → Character 계약 → MAP 공용 (RULES.md의 Allowed Direction 그대로)
```

## SCENE_PLAN

```text
Assets/_Game/Scenes/Live/CharacterLiveTest.unity — L01_02 생성
구성: Main Camera(CameraRig 프리팹) + Directional Light + RunBootstrap 1객체
기존 MapGenerationProgressTest.unity 불가침, EditorBuildSettings 등록은 L04_02에서만
```

## PREFAB_PLAN

```text
Assets/_Game/Live/Prefabs/Player.prefab — CapsuleCollider2D 0.72×0.90(잠금), SpriteRenderer, Live 구동 컴포넌트 — L01_02
Assets/_Game/Live/Prefabs/CameraRig.prefab — 방 스냅 카메라(CHAR03 정책 소비) — L01_02 골격, L02_01 완성
Assets/_Game/Live/Prefabs/RunBootstrap.prefab — 조립 진입점 — L01_03
Assets/_Game/Live/Prefabs/Hud.prefab — uGUI Canvas — L03_02
도구 표현물(폭탄/로프 표시) — L03_01
물리: Rigidbody2D kinematic + 자체 이동(순수 모터 출력 적용); Unity 물리 시뮬레이션·콜백은 판정 권위 아님(잠금)
```

## BOOTSTRAP_PLAN

```text
RunBootstrap(MonoBehaviour): 구성 루트 — L01_03
  조립: 입력 어댑터 → CharacterInputSnapshot → 순수 이동 코어(CHAR01) + UnityPhysics2DCharacterCollisionWorld 주입
  런 시작: 시작 스냅샷 → CharacterSpawnIntegrationPolicy → 스폰 요청 소비(플레이어 배치)
  틱: FixedUpdate에서 순수 정책 호출, 시간은 Time.fixedDeltaTime 주입(판정은 물리 틱 기준 잠금)
  상태 소유: CharacterRunState/CharacterHealthState/CharacterRunInventoryState 값 보관·갱신
```

## REQUEST_CONSUMER_PLAN

```text
L01_03: CharacterPlayerSpawnRequest → 플레이어 위치 배치·런 활성
L02_01: CharacterGeneratedRouteTransitionRequest + CharacterCameraRoomTransitionPolicy 결정 → 카메라 이동(KEEP: 입력·속도 무변조 잠금 준수)
L03_01: 폭탄(설치/소모/퓨즈/폭발/지형 요청→표시·라이브 셀 상태 반영은 어댑터 경유), 로프(설치/소모/세그먼트→표시, 등반 모터 요청→플레이어 수직 이동), 휴대/투척(CHAR04 계약 소비)
L03_02: CharacterPresentationBridge.NormalizeBatch 출력 → 연출 실행(사운드/파티클은 선택), CharacterHudSnapshot → HUD
생존 사슬(피해→사망→런 실패)은 L03_01에서 통합 소비(스파이크 등 위험 감지 포함)
소비자는 요청을 "적용"만 한다 — 판정 로직 재작성 금지
```

## MAP_ADAPTER_PLAN

```text
L02_02 소유. MAP 런타임 무수정 원칙:
  MicrochunkDefinition/TileCell/Transformer + MicrochunkDefinitions(Data) + MoonpalaceBoundaryCandidateIndex
  → CharacterGeneratedRunSnapshot(방/마이크로청크/루트/아이템/시작) 투영
  → ICharacterMapWorldQuery 라이브 구현(셀 상태: MicrochunkTileLayer→CharacterMapCellState.FromTileLayer)
  → ICharacterRoomReadinessSource 라이브 구현(생성 완료 방 등록)
  → 타일 시각화(경량 SpriteRenderer 또는 Tilemap 렌더 — 렌더 전용, 판정 권위 아님)
검증: CHAR06_02 CharacterGeneratedRunValidationPolicy 재사용(8시드 스윕)
MAP 파사드가 꼭 필요해지면: 캐릭터측 복제 금지, MAP 하니스 CHANGE CONTROL 별도 패키지로만
```

## HUD_PRESENTATION_PLAN

```text
L03_02 소유. Hud.prefab(uGUI Canvas): 체력/폭탄/로프/런 상태/복귀 토큰 표시 — CharacterHudSnapshot.FromRunState 폴링
연출: NormalizeBatch 배치를 프레임당 소비(우선순위·순번 그대로), 실행기는 교체 가능한 경량 컴포넌트
TextMeshPro 사용 가능(패키지 존재) — 단 데이터 소스는 스냅샷만
```

## PLAYMODE_TEST_PLAN

```text
L04_01 소유. Game.Character.Tests.PlayMode 신설:
  키보드 스모크: InputSystem 테스트 유틸로 Space/X/Z/C/축 주입 → 점프/폭탄/로프 요청 발생 확인
  생성 런 스모크: 어댑터 경유 스냅샷 → 스폰 → 2셀 점프/2셀 틈 통과·3셀 실패 재확인(라이브 물리 경로)
  결정성: 같은 시드 → 같은 시작 상태
주의: unity-mcp 브리지 소켓 이슈(10분+ 실행 중 사망→에러 로그 주입, CHAR06_03 기록) — PlayMode 실행 중 폴링 자제, 결과는 TestResults.xml로 교차 확인
```

## BUILD_PLAN

```text
L04_02 소유. StandaloneWindows64(활성 타깃 유지, 전환 금지)
EditorBuildSettings: 라이브 씬 1건 등록 — 본 잠금이 승인하는 유일한 ProjectSettings 쓰기 예외(L04_02 문서에 명시된 경우만)
출력: Builds/Validation/L04_02/** (gitignore, 비커밋)
기준선: CHAR06_03 빌드 성공(554.42MB/109.2s), Character 177 + MAP 13,536 EditMode 회귀 유지 필수
```

## RESULT_FORMAT_LOCK

```text
전 과제 공통: TASK / STATUS / SUMMARY / READ / CHANGED / CREATED / TESTS / BUILD / SCOPE_VALIDATION / FORBIDDEN_AUDIT / NEXT
구현 과제 추가: LIVE_CONTRACTS_USED / REQUESTS_CONSUMED / ASSETS_WIRED / MANUAL_VERIFICATION / REGRESSION_BASELINE
STATUS는 독립 라인 `STATUS: PASS|FAIL|BLOCKED` 정확히 1개
REGRESSION_BASELINE 최소치: Character EditMode 177 PASS 유지(감소 금지), 구현 과제는 매 실행 확인
```

## CHANGE_CONTROL_RULES

```text
순수 Character 계약·MAP 런타임·기존 테스트·잠금 바인딩·ActionId 변경 = 별도 CHANGE CONTROL 패키지로만
allowlist 밖 쓰기 필요 발견 시 = STATUS: BLOCKED + 정확한 경로·사유 기록(임의 확장 금지)
EditorBuildSettings 예외는 L04_02 한정(위 BUILD_PLAN)
버그 수정으로 라이브 코드(Assets/_Game/Live/**)를 후속 과제에서 고치는 것은 허용 — 단 RESULT에 CHANGED로 명기
```

## KNOWN_RISKS

```text
1. unity-mcp 브리지 소켓 사망(장시간 실행) — 위양성 로그 주입 위험, 완화: 실행 중 폴링 자제 + 파일 기반 교차 확인
2. MAP 생성 파사드 부재 — L02_02 어댑터가 도메인 표면 조합 필요(복잡도 위험), 완화: CHAR06_02 검증 정책 재사용으로 정합성 확인
3. 빌드 씬 전부 Legacy — L04_02에서 라이브 씬 등록 시 Legacy 씬 유지 여부는 사용자 결정 사항으로 보고
4. Rigidbody2D 상호작용 — 순수 모터 출력과 물리 이동의 이중 권위 위험, 완화: kinematic + 질의 전용(잠금 규칙 그대로)
5. 입력 지연/프레임 순서 — FixedUpdate 판정 vs Update 입력 수집, 완화: 스냅샷 버퍼(기존 CharacterInputBuffer 계약 소비)
```

## NEXT_TASK_GATE

```text
L01_01_INPUT 개방 조건(새 INBOX 패키지의 매니페스트가 검증해야 할 앵커):
  CLI/MCP/REPORTS/L00_02_RESULT.md 존재 + STATUS: PASS + "Current Task after finalize: NONE"
  CLI/MCP/INPUTS/LIVE_LOCK.md 존재 + LOCK_STATE: FILLED_BY_L00_02
  L01_02 이후 LOCKED 유지
```
