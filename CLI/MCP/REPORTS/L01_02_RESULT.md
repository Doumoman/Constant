# L01_02_RESULT

## TASK

CLI/MCP/TASKS/L01_02.md (L01_02_PREFAB — LIVE01_02_CREATE_PLAYER_PREFAB_AND_MINIMAL_LIVE_TEST_SCENE)

## STATUS

STATUS: PASS

## SUMMARY

라이브 플레이어 프리팹(`CharacterLivePlayer.prefab`)과 최소 라이브 테스트 씬(`CharacterLiveTest.unity`)을 생성했다. 프리팹은 Kinematic Rigidbody2D(gravityScale 0) + 잠금 규격 캡슐(0.72×0.90, CharacterCapsuleGeometry 계약 참조) + 입력 공급자(inputactions 지정) + 조립 바인딩 리그로 구성되며, 씬은 직교 카메라 + 최소 바닥 콜라이더 + 프리팹 인스턴스 1개뿐이다. 리그/검증기 2파일로 L01_03 소비 표면을 노출했고, 에디터에서 프리팹·씬 감사를 실행해 필수 구성 전부 존재·금지 구성 전부 부재·검증기 위반 0을 실측했다. 컴파일 0 에러, Character EditMode 177/177 유지, 빌드 설정 무변경, 이동/스폰/요청 소비 없음.

## READ

- CLI/MCP/ENTRY.md, RULES.md, STATUS.md, MASTER.md, LIVE_SRC.md, LIVE_LOCK.md, REPORTS/L00_02·L01_01 RESULT
- CharacterDesign/MCP/REPORTS/CHAR06_04 RESULT, INPUTS/CHAR00_SOURCE_REGISTRY.md
- Assets/_Game/Character/Runtime/** — CharacterCapsuleGeometry(BaselineWidth 0.72/BaselineHeight 0.90/Default) 계약 확인
- Assets/_Game/Live/Runtime/**(L01_01 공급자 표면 — Adapter/IsReady/ConsumeFixedSnapshot), Assets/_Game/Live/Input/CharacterLiveControls.inputactions
- Map 런타임(무접촉 확인), Scenes(활성 1 + 신규 Live 폴더), *.prefab(_Game 기존 0), Packages/manifest.json, EditorBuildSettings(씬 10 — 무변경 확인)

## CHANGED

- Assets/_Game/Live/Runtime/Input/CharacterLiveInputSource.cs — 추가형 속성 `HasActionsAsset` 1건(에디트 모드 검증용; 기존 API 무변경·허용 경로 내). 사유: 프리팹 자산 상태에선 Awake 전이라 IsReady가 항상 false — 검증기가 "자산 지정됨"을 에디트 모드에서 판별할 표면 필요

## CREATED

- Assets/_Game/Live/Runtime/Player/CharacterLivePlayerRig.cs — 조립 바인딩 표면 {Body, BodyCollider, InputSource, IsBound, ConsumeFixedSnapshot(tick), BindLocalComponents} — L01_03 소비 진입점, 이동/판정 없음
- Assets/_Game/Live/Runtime/Player/CharacterLivePlayerRigValidator.cs — 결정적 무부작용 검증(읽기 전용): Kinematic/gravity 0/캡슐 규격(계약 원천 참조)/Vertical/입력 준비/Animator·AudioSource 부재 — 위반 문자열 목록만 반환
- Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab — 요구 구성 그대로(아래 MANUAL_VERIFICATION 실측)
- Assets/_Game/Scenes/Live/CharacterLiveTest.unity — Main Camera(직교, size 5) + Floor(BoxCollider2D 12×1, 정적) + CharacterLivePlayer 인스턴스 1(프리팹 연결)
- (+ Unity 자동 생성 .meta/폴더)

## TESTS

- Character EditMode 기준선: **177/177 PASS** (failed 0, skipped 0, 1.18s) — 감소 없음
- MAP EditMode: 13,536 앵커 유지(본 과제 MAP 무접촉 — 과제 문서가 명시 허용)
- 신규 테스트 없음(Assets/_Game/Tests/** 금지 경로; 라이브 검증은 L04_01 소관)

## BUILD

- 본 과제 빌드 검증 없음(비요구) — 컴파일 클린으로 대체: `error CS` 0건. EditorBuildSettings 무변경(씬 10 그대로 — 라이브 씬 미등록, 과제 요구 준수). 빌드 기준선 CHAR06_03 유지

## LIVE_CONTRACTS_USED

- CharacterCapsuleGeometry.BaselineWidth/BaselineHeight(잠금 0.72×0.90) — 프리팹 캡슐 크기의 원천이자 검증기 기대값(수치 복제 없이 계약 참조)
- CharacterLiveInputSource(L01_01) — 프리팹 배선 + 리그 위임 표면; CharacterInputSnapshot — 리그 ConsumeFixedSnapshot 반환 타입
- 순수 이동/전투/생존 정책 미사용(조립은 L01_03 소관)

## REQUESTS_CONSUMED

None. Prefab and manual scene composition only.

## ASSETS_WIRED

- CharacterLiveControls.inputactions -> CharacterLiveInputSource -> CharacterLivePlayer prefab (SerializedObject로 actionsAsset 지정, 에디터 실검증 "actionsAsset=CharacterLiveControls")
- CharacterLivePlayer prefab -> CharacterLiveTest scene instance (PrefabUtility 인스턴스 — 프리팹 연결 경로 실검증)
- No generated run spawn wiring

## MANUAL_VERIFICATION

에디터 내 실감사(execute_code):

- 프리팹: root "CharacterLivePlayer" / Rigidbody2D Kinematic·gravityScale 0 / CapsuleCollider2D 0.72×0.9 Vertical / InputSource actionsAsset=CharacterLiveControls / Rig IsBound=True
- 금지 구성 부재(자식 포함): Animator=부재, AudioSource=부재, Canvas(UI)=부재, Camera=부재 — MAP 생성기/SceneManager/스폰·루트 소비자 컴포넌트 없음(존재 자체가 없음)
- RigValidator: 위반 0 (1차 실행에서 에디트 모드 특성 1건 검출 → IsReady/HasActionsAsset 이원화로 해소, CHANGED에 기록)
- 씬: 루트 3개(Main Camera 직교 True / Floor / CharacterLivePlayer), 플레이어 인스턴스 1, 카메라 1, 금지 구성 False, 인스턴스의 프리팹 링크 = Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab
- 입력 자산 불변: git diff 0건(L01_01 바인딩 그대로, E/F/Q 부재 상태 유지)
- 플레이 모드 구동/이동 검증은 의도적 미수행(이동 조립은 L01_03, PlayMode는 L04_01 소관)

## REGRESSION_BASELINE

- Character EditMode 177/177 PASS (본 실행 재확인) — LIVE_LOCK "177 유지" 충족
- MAP 13,536 앵커·빌드 성공 앵커 유지, 컴파일 0 에러

## SCOPE_VALIDATION

- 쓰기: 허용 경로만 — Live/Runtime(신규 Player 2파일 + InputSource 추가 속성 1건), Live/Prefabs(프리팹), Scenes/Live(씬), 본 REPORT
- 금지 경로 0건: Character/MAP 런타임, Live/Input(자산 불변 — diff 0), Tests, Packages, ProjectSettings(EditorBuildSettings 무변경 확인), MapDesign, CharacterDesign, Builds, Temp
- 후속 과제 미개방(L01_03 이후 LOCKED 유지)

## FORBIDDEN_AUDIT

- Rigidbody2D 이동 없음 — 리그는 바인딩만, 검증기는 읽기 전용(결정적 무부작용)
- 스폰/루트/방 전환/폭탄/로프/피해/사망/런 실패/HUD/연출 요청 소비 없음
- CharacterActionId 추가·수정 없음, 순수 정책 재작성 없음, MAP 셀 상수/방 로직 복제 없음(캡슐 규격도 계약 참조)
- Animator/AudioSource/UI/MAP 생성기/SceneManager 없음(프리팹·씬 실감사), basic attack/melee/shoot/dash/wall jump/double jump 없음

## NEXT

Current Task after finalize: NONE
Next Task auto-opened: NO (`L01_03_SPAWN`은 LOCKED 유지, 새 INBOX 패키지로만 개방 — 소비 표면 {CharacterLivePlayerRig.Body/BodyCollider/InputSource/ConsumeFixedSnapshot}이 배선 준비 완료)
