# L00_01_RESULT

## TASK

CLI/MCP/TASKS/L00_01.md (L00_01_SURVEY — LIVE00_01_INVENTORY_LIVE_SCENE_INPUT_PREFAB_SURFACES)

## STATUS

STATUS: PASS

## SUMMARY

라이브 통합에 필요한 Unity 프로젝트 표면을 전수 조사해 소스 레지스트리(`CLI/MCP/INPUTS/LIVE_SRC.md`)를 생성했다. 핵심 발견: 활성 게임플레이 계층이 사실상 그린필드다 — 활성 씬 1개(MAP 진단 전용), `_Game` 프리팹 0개, 활성 .inputactions 0개(Legacy 2본은 바인딩 불일치·컴파일 제외), 활성 MonoBehaviour는 MAP 진단 4개뿐. 캐릭터 순수 계약(13모듈)과 MAP 공용 도메인 표면은 완비되어 있고 소비자/조립 계층만 부재. L00_02용 경로 토큰(Assets/_Game/Live/** 계열)을 권고했다. 프로젝트 코드/자산 변경 0건.

## READ

- CLI/MCP/ENTRY.md, RULES.md, STATUS.md, MASTER.md, INPUTS/CHAR_EXIT.md
- CharacterDesign/MCP/REPORTS/CHAR06_04(최종 EXIT)·CHAR06_01·CHAR06_02·CHAR06_03 RESULT, INPUTS/CHAR00_SOURCE_REGISTRY.md
- Assets/_Game/Character/Runtime/** (13모듈 133파일), Assets/_Game/Tests/EditMode/Character/** (177 tests)
- Assets/_Game/Map/Runtime/** (WorldGeneration: Domain/Generation/Microchunks/Boundaries/Data/Diagnostics/Validation/Random), Assets/_Game/Tests/EditMode/Map/**
- Assets/_Game/Scenes/**, Assets/**.unity(활성 1+에셋팩 데모+Legacy 19), Assets/**.inputactions(2, 전부 Legacy), Assets/**.prefab(_Game 0 / Legacy 226 / 에셋팩)
- Packages/manifest.json(57 의존성), ProjectSettings/EditorBuildSettings.asset(씬 10), ProjectSettings/ProjectSettings.asset(타깃 메타)

## CHANGED

- 없음 (프로젝트 코드/자산/설정 변경 0건 — Assets/Packages/ProjectSettings/MapDesign/CharacterDesign 불가침 준수)

## CREATED

- CLI/MCP/INPUTS/LIVE_SRC.md (라이브 소스 레지스트리, REGISTRY_STATE: FILLED_BY_L00_01)
- CLI/MCP/REPORTS/L00_01_RESULT.md (본 REPORT)

## ENTRY_GATE

- Current Task = CLI/MCP/TASKS/L00_01.md ✓ (매니페스트 게이트로 적용: 페이로드 3건 sha256 일치)
- Character final exit REPORT 존재 + sha256 `6efc2ac08d7cb52fd8ba260888310dd403ae64d191767a9338b174a0897fc96c` 일치 ✓
- required_text 3건("STATUS: PASS"/"CHARACTER_FINAL_EXIT_DECISION: APPROVED"/"Character harness final state: COMPLETE") 존재 ✓
- L00_02 이후 11개 과제 전부 LOCKED ✓ (STATUS.md)

## UNITY_PROJECT_SURFACES

- Unity 6000.3.8f1 / StandaloneWindows64 / URP 17.3.0 / Input System 1.18.0 / test-framework 1.6.0
- EditorBuildSettings 씬 10개 전부 Legacy — 라이브 씬 미등록
- 활성 어셈블리: Game.Character.Runtime(순수, MonoBehaviour 0) / Game.Map.Runtime(진단 MonoBehaviour 4) / 테스트 2 + 빈 PlayMode 1

## SCENE_PREFAB_SURFACES

- 활성 씬: `Assets/_Game/Scenes/MapGenerationProgressTest.unity` 1개 — MAP 생성 진행 진단 전용, 플레이어 통합 부적합 → 신규 라이브 씬 권고(`Assets/_Game/Scenes/Live/`)
- 프리팹: `_Game` 0개(전부 신규 제작 대상), Legacy 226개는 읽기 전용 선례, 에셋팩은 아트 소스
- 상세 후보/권고는 LIVE_SRC.md의 Scene/Prefab Candidates 절

## INPUT_SURFACES

- 활성 .inputactions 0개. Legacy `StarNightControls.inputactions` 2본은 Interact=E/UseBomb=F/UseRope=Q로 잠금 의미와 불일치(잠금 규칙이 명시한 비활성 선례)
- 신규 자산 권고: 잠금 바인딩 그대로(Move 수평/Down 하강/Jump=Space/Action=X/Bomb=Z/Rope=C) → `CharacterInputSnapshot` 공급, ActionId 5종 불변

## BOOTSTRAP_AND_REQUEST_CONSUMER_SURFACES

- 부트스트랩 부재(활성 MonoBehaviour는 MAP 진단 4개뿐) → 신규 조립 계층 필요: 제안 `Assets/_Game/Live/Runtime/` + `Game.Character.Live.asmdef`(references: Character.Runtime, Map.Runtime, InputSystem)
- 기성 라이브 어댑터: `UnityPhysics2DCharacterCollisionWorld`(CHAR01 승인 충돌 질의) — 주입 재사용
- 소비 대기 요청 계약 전목록(스폰/루트/이동/휴대·투척/폭탄/로프/생존/런 상태/연출) — LIVE_SRC.md에 삽입 지점과 함께 기록

## HUD_PRESENTATION_SURFACES

- 데이터 소스 완비: `CharacterHudSnapshot.FromRunState` + `CharacterPresentationBridge.NormalizeBatch`(우선순위·중복 제거·결정 순번)
- 활성 UI 자산 0 — 신규 Canvas 프리팹 + 바인더 권고(Legacy UI는 읽기 전용 선례)

## MAP_GENERATED_OUTPUT_SURFACES

- MAP 런타임에 생성 월드 파사드 부재(오케스트레이션은 테스트 어셈블리에만 존재) — L02_02는 공용 도메인 표면(MicrochunkDefinition/TileCell/Transformer/BoundaryCandidateIndex/좌표·상수)에서 캐릭터 스냅샷(`CharacterGeneratedRunSnapshot` 계열)으로 투영하는 어댑터 + `ICharacterMapWorldQuery`/`ICharacterRoomReadinessSource` 라이브 구현 방향
- MAP측 파사드 신설이 필요해지면 MAP 하니스 CHANGE CONTROL 소관(캐릭터측 생성 로직 복제 금지)

## TEST_AND_BUILD_SURFACES

- EditMode 기준선: Character 177 / MAP 13,536 전건 PASS(CHAR06_03) — 라이브 작업의 회귀 기준
- PlayMode: Game.Map.Tests.PlayMode는 테스트 0개 빈 asmdef(Game.Stage.Runtime 참조는 Legacy 실존 어셈블리), Character PlayMode asmdef 부재 → L04_01 신규 대상
- 빌드 기준선: StandaloneWindows64 성공(554.42MB/109.2s), 빌드 씬 전부 Legacy — L04_02에서 라이브 씬 등록 결정 필요(해당 과제 allowlist에 ProjectSettings 포함 여부 확인 요)

## RECOMMENDED_L00_02_TOKENS

```text
LIVE_RUNTIME: Assets/_Game/Live/Runtime/**
LIVE_INPUT: Assets/_Game/Live/Input/**
LIVE_PREFABS: Assets/_Game/Live/Prefabs/**
LIVE_SCENES: Assets/_Game/Scenes/Live/**
LIVE_PLAYMODE: Assets/_Game/Tests/PlayMode/Character/**
READONLY_PRECEDENT: Assets/_Legacy/**, Assets/2D Fantasy sprite bundle/**
FORBIDDEN_KEEP: Assets/_Game/Character/Runtime/** 재작성 금지, Assets/_Game/Map/Runtime/** 수정 금지
```

## BLOCKERS

- 차단 없음. 결손 표면은 전부 신규 제작으로 계획 가능(레지스트리에 목록화). 주의 2건(unity-mcp 브리지 소켓 이슈 — 장기 PlayMode 시 재현 가능 / 빌드 씬 전부 Legacy — L04_02 결정 필요)은 LIVE_SRC.md에 기록

## SCOPE_VALIDATION

- 쓰기 2건만: CLI/MCP/INPUTS/LIVE_SRC.md + 본 REPORT (허용 목록 그대로)
- Assets/Packages/ProjectSettings/MapDesign/CharacterDesign/Builds/Temp 변경 0건 (git status 확인)
- Pre-existing dirty files: 없음(직전 커밋까지 클린; 현재 더러움은 본 패키지 적용분 CLI/MCP 3건뿐)
- 후속 과제 미개방(L00_02_LOCK 이후 11개 LOCKED 유지)

## NEXT

Current Task after finalize: NONE
Next Task auto-opened: NO (`L00_02_LOCK`은 LOCKED 유지, 새 INBOX 패키지로만 개방)
