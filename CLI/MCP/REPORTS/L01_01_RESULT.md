# L01_01_RESULT

## TASK

CLI/MCP/TASKS/L01_01.md (L01_01_INPUT — LIVE01_01_IMPLEMENT_KEYBOARD_INPUT_TO_LOCKED_CHARACTER_ACTIONS)

## STATUS

STATUS: PASS

## SUMMARY

첫 라이브 통합 표면을 구현했다: Unity Input System 기반 키보드 입력(신규 `CharacterLiveControls.inputactions`, Player 맵, 잠금 바인딩 6종)을 완성된 캐릭터 입력 계약(`CharacterInputSnapshot`/`CharacterButtonSnapshot`)으로 변환하는 공급자 계층(`Game.Character.Live` 어셈블리 신설, 런타임 4파일). Update 수집 에지를 고정 스텝 소비까지 보존하는 누적기 패턴으로 구현했고, 신규 ActionId 0·기존 API 무수정·요청 소비 0(공급자 전용). 컴파일 에러 0, Character EditMode 177/177 PASS 유지, 입력 자산 감사(6액션·바인딩 정확·E/F/Q 부재) 에디터에서 실검증.

## READ

- CLI/MCP/ENTRY.md, RULES.md, STATUS.md, MASTER.md, INPUTS/CHAR_EXIT.md, LIVE_SRC.md, LIVE_LOCK.md, REPORTS/L00_01·L00_02 RESULT
- CharacterDesign/MCP/REPORTS/CHAR06_04 RESULT, INPUTS/CHAR00_SOURCE_REGISTRY.md
- Assets/_Game/Character/Runtime/Input/** 전문(CharacterActionId/ButtonSnapshot/InputSnapshot/InputBuffer/InputLockSet — 생성자·팩토리·CaptureFrame 의미론 확인, 추측 API 없음), Map 런타임(참조 불필요 확인), EditMode 테스트(기준선), *.inputactions(레거시 2본 읽기 전용), Packages/manifest.json(Unity.InputSystem 1.18.0)

## CHANGED

- 없음 (기존 파일 수정 0건 — Character/MAP 런타임·테스트·기존 자산 불변)

## CREATED

- Assets/_Game/Live/Input/CharacterLiveControls.inputactions — Player 맵, 6액션(Move Value/Axis, Down·Jump·Action·Bomb·Rope Button)
- Assets/_Game/Live/Runtime/Game.Character.Live.asmdef — references ["Game.Character.Runtime", "Unity.InputSystem"], autoReferenced false (허용된 신설 1회)
- Assets/_Game/Live/Runtime/Input/CharacterLiveButtonFrame.cs — 렌더 프레임 1회분 버튼 관측값(값 객체)
- Assets/_Game/Live/Runtime/Input/CharacterLiveInputState.cs — 버튼 누적기: 에지 OR 보존·held 최신화·소비 시 에지만 소거
- Assets/_Game/Live/Runtime/Input/CharacterLiveInputAdapter.cs — 순수 변환기(장치 API 무의존): AccumulateFrame(Update) → ConsumeFixedSnapshot(고정 스텝) → CharacterInputSnapshot
- Assets/_Game/Live/Runtime/Input/CharacterLiveInputSource.cs — MonoBehaviour 공급자: Player 맵 resolve/Enable, Update에서 WasPressed/WasReleased/IsPressed 샘플링 누적, 공개 표면 {Adapter, IsReady, ConsumeFixedSnapshot(tick)} — L01_02 배선용
- (+ Unity 자동 생성 .meta)

## TESTS

- Character EditMode 기준선: **177/177 PASS** (failed 0, skipped 0, 7.19s) — 감소 없음
- MAP EditMode 기준선: 13,536/13,536 PASS 앵커 기록(CHAR06_03 + 본 하니스 잠금상 재실행 불요 — 로컬 정책 요구 없음, MAP 코드 무접촉)
- 신규 EditMode 테스트 없음: 라이브 계층 테스트는 L04_01 PlayMode 소관(LIVE_LOCK PLAYMODE_TEST_PLAN), 순수 변환기(Adapter/State)는 그때 함께 커버 — Assets/_Game/Tests/** 는 본 과제 금지 경로

## BUILD

- 본 과제 빌드 검증 없음(비요구) — 컴파일 클린으로 대체: `error CS` 0건, `warning CS` 0건, inputactions 임포트 에러/경고 0건. 빌드 기준선은 CHAR06_03(StandaloneWindows64 성공) 유지

## LIVE_CONTRACTS_USED

- CharacterInputSnapshot(horizontal, downHeld, jump, action, bomb, rope) 생성자 — SafeDrop은 스냅샷 내부 계산(DownHeld && Action.PressedThisFrame)에 위임, 라이브 계층은 별도 SafeDrop 신호를 만들지 않음(과제의 "existing contract expects" 그대로)
- CharacterButtonSnapshot(pressed, held, released, consumed:false, tick) 생성자 — 팩토리 대신 누적 에지 조합이 필요한 지점이라 5-인자 생성자 사용
- CharacterActionId — 참조만(신규 값 없음); CharacterInputBuffer는 미사용(버퍼 소비는 부트스트랩 조립 소관 — L01_03+, 스냅샷의 CaptureFrame 호환성은 계약 그대로 유지됨)

## REQUESTS_CONSUMED

None. Input provider only.

## ASSETS_WIRED

Input actions asset only. No scene or prefab wiring.

## MANUAL_VERIFICATION

- 에디터 내 실검증(execute_code): 임포트된 InputActionAsset 로드 → Player 맵 1개·액션 6개 확인, 바인딩 전수 열거 — Move=1DAxis(a/d)+1DAxis(leftArrow/rightArrow), Down=s/downArrow, Jump=space, Action=x, Bomb=z, Rope=c — 잠금 표와 정확 일치
- 금지 바인딩 스캔: `<Keyboard>/e`·`/f`·`/q` 부재 확인(False)
- 씬 배치·플레이 검증은 의도적으로 미수행(씬/프리팹 금지 — L01_02~L01_03에서 배선 후 검증)

## REGRESSION_BASELINE

- Character EditMode 177/177 PASS (본 실행에서 재확인) — LIVE_LOCK 요구 "177 유지·감소 금지" 충족
- MAP EditMode 13,536 앵커 유지(무접촉), 컴파일 0 에러

## SCOPE_VALIDATION

- 신규 파일 전부 허용 경로: Assets/_Game/Live/Runtime/**(asmdef 포함 5파일) + Assets/_Game/Live/Input/**(1파일) + 본 REPORT
- 금지 경로 변경 0건: Character/MAP 런타임, Live/Prefabs, Scenes, Tests, Packages, ProjectSettings, MapDesign, CharacterDesign, Builds, Temp 전부 무접촉 (git status 확인)
- MAP 참조 미추가(입력 계약이 요구하지 않음 — BLOCKED 사유 없음)

## FORBIDDEN_AUDIT

- 레거시 Input.GetKey 폴링 없음 — Input System InputAction API만 사용(WasPressedThisFrame/WasReleasedThisFrame/IsPressed/ReadValue)
- E/F/Q 바인딩 없음(에디터 실스캔), 레거시 inputactions 미사용
- 신규 CharacterActionId 없음, 기존 공개 API 개명/재작성 없음
- basic attack/melee/shoot/dash/wall jump/double jump 없음 — Move/Down은 축·상태 입력으로만
- Rigidbody2D 이동/텔레포트/스폰/디스폰/피해/인벤토리 소모/게임플레이 요청 소비 없음 — 값 공급 전용

## NEXT

Current Task after finalize: NONE
Next Task auto-opened: NO (`L01_02_PREFAB`은 LOCKED 유지, 새 INBOX 패키지로만 개방 — 공급자 표면 {CharacterLiveInputSource.Adapter/IsReady/ConsumeFixedSnapshot}이 배선 준비 완료)
