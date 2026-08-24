# CHAR00_01 RESULT — 캐릭터·입력·물리·카메라·MAP 접점 조사

TASK ID: CHAR00_01  
PHASE: CHAR00  
RESULT PATH: `CharacterDesign/MCP/REPORTS/CHAR00_01_INVENTORY_CHARACTER_INPUT_PHYSICS_MAP_RESULT.md`  
조사 기준: `main` @ `24cb1b9`, Unity 6000.3.8f1

STATUS: PASS

## 실제 변경 파일 전체

1. `CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md` — 사용자 제공 초안(DRAFT_FOR_CHAR00_01)을 실제 조사 결과로 갱신(FILLED_BY_CHAR00_01)
2. `CharacterDesign/MCP/REPORTS/CHAR00_01_INVENTORY_CHARACTER_INPUT_PHYSICS_MAP_RESULT.md` — 새 하네스의 REPORTS 경로로 이관

위 2개 파일 외 변경 없음. 둘 다 WRITE ALLOWLIST 내 경로다. `.meta` 생성 여부: `CharacterDesign/**`는 Assets 밖이므로 Unity `.meta` 대상이 아니며 생성되지 않았다.

## 구현한 세부 내용

조사 전용 작업. 코드 구현 없음. 조사 결과 전체는 registry에 기록했고 핵심은 다음과 같다.

### 캐릭터
- 활성(컴파일되는) 캐릭터 런타임은 존재하지 않는다. 활성 게임 코드는 `Assets/_Game/**`의 MAP 생성 파이프라인뿐이다.
- 기존 캐릭터 구현은 전부 `Assets/_Legacy/**` 선례이며 `.cs` 672/672개가 `#if LEGACY_DISABLED`로 컴파일 제외 상태다(어떤 asmdef도 이 심볼을 define하지 않음).
- 선례 두 세대: 구세대 `Assets/_Legacy/_Game/**`, 신세대 `Assets/_Legacy/StarNight/**`(RW1). 신세대 기준 Player 스택 = PlayerMotor2D(Rigidbody2D+CapsuleCollider2D, 수동 중력) / PlayerInputAdapter / GroundProbe2D(CapsuleCast) / JumpGraceState(coyote 0.10, buffer 0.12) / P1MovementTuning(SO: 캡슐 0.72×0.90, runSpeed 3.75, jumpHeight 2.2, gravity 24, maxHealth 4) / PlayerRecovery / SafeCellTracker.
- 휴대·전투 선례: CarrySystem(단일 슬롯), Bomb2D+ExplosionService2D(3×3 마스크), Rope, Mining/Pestle/Umbrella/Water/Grapple 도구군. 별도 일반 공격 버튼 선례 없음(잠금 규칙과 부합).

### 입력
- `com.unity.inputsystem` 1.18.0, `activeInputHandler: 2`(Both). `InputManager.asset`은 기본 axes + Debug axes.
- 레거시 `StarNightControls.inputactions`(Gameplay 맵): Move=WASD/화살표, Jump=Space, Interact=E, UseHeldTool=LMB/J, UseRope=Q, UseBomb=F. 사본 2개가 서로 다름(둘 다 레거시).
- 잠금 의미(X=행동/아래+X=내려놓기/Z=폭탄/C=로프)와 불일치 확인. Jump 장치 키의 프로젝트 유일 선례는 Space.

### 물리
- 선례: Rigidbody2D + 수동 중력, CapsuleCast 접지(probe 0.08, vy≤0.05), 1 world unit = 1 논리 셀(`GridWorld.CellSize=1f`). 경사 정책 NONE. Solid 레이어 이름 값은 Scene/Prefab 소유라 코드 조사로 미확정(UNKNOWN).

### 카메라
- 선례: RoomFocusCamera2D + RoomBounds2D(기본 12×8 rect) — 카메라 연출 전용, 이동 월드 비분리(잠금 규칙과 호환). Hysteresis 구현은 레거시 전체 0건(신규 필요).

### MAP 접점
- 공용 좌표 계약 존재: `WorldTileCoord/SectorCoord/MicroChunkCoord/LocalTileCoord` + `WorldCoordinateUtility`, 상수 World 624×416 / Sector 48×32 / MicroChunk 12×8 / 레이어 8.
- 타일 의미 원천: `MicrochunkTileLayer`(GroundSolid/OneWay/Breakable/Hazard/Liquid/DecorationBack/DecorationFront/Marker).
- 캐릭터용 월드 질의·지형 변경 요청·경계 게이트·방 준비 API는 부재 확인(생성 파이프라인·데이터 모델만 존재). MAP 상태: MAP07_02까지 COMPLETE, Current Task NONE.
- 활성 MAP 런타임은 UnityEngine.Tilemaps 참조 0건(순수 로직).

### 관찰된 기존 이상(수정하지 않음)
- `Assets/_Game/Tests/PlayMode/Map/Game.Map.Tests.PlayMode.asmdef`가 테스트 0개 상태로 레거시 `Game.Stage.Runtime`을 참조(stale). 사실 기록만 하고 미수정.

## 컴파일 결과

- 본 작업은 코드를 변경하지 않았다(문서 2개만 작성).
- Unity Editor 연결 확인: MCP 브리지 정상(`StdioBridgeHost started on port 6401`), 조사 시점 `EditorApplication.isCompiling = False`, Console에 신규 오류 없음.
- 기존 오류/신규 오류 분리: 신규 오류 0(코드 무변경이므로 해당 없음).

## 고정 테스트 결과 (5/5 PASS)

1. SourceRegistryCreated: PASS — 조사한 경로·assembly·입력·물리·카메라·MAP 접점을 `CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md`에 기록(§0~§10 전 섹션 갱신, REGISTRY_STATE: FILLED_BY_CHAR00_01).
2. NoProjectMutation: PASS — `git status --porcelain -- Assets Packages ProjectSettings MapDesign` 결과 본 작업 변경 0개. 잔존 M 2개(`ProjectSettings/Packages/dev.yarnspinner/Assembly-CSharp-generated.ysls.json`, `ProjectSettings/ShaderGraphSettings.asset`)는 작업 시작 전부터 존재한 사용자 변경으로 registry §0에 기준선으로 기록했고 본 작업에서 접촉하지 않았다.
3. ResultPathExact: PASS — 원본 PASS 결과를 새 하네스의 `REPORTS` 표준 경로로 이관했으며 내용과 판정은 보존했다.
4. UnknownsExplicit: PASS — 미확정 항목은 추측 없이 UNKNOWN(예: Solid 레이어 값, 물리 레이어 구성 값) 또는 BLOCKER(registry §10의 3건: 신규 코드 배치 경로, 입력 자산 불일치, 캐릭터용 MAP API 부재)로 기록.
5. StatusExact: PASS — 상태는 독립된 한 줄 `STATUS: PASS`로 기록.

## 잔여 문제와 재현 정보

- BLOCKER 3건(다음 단계 진행 조건, registry §10):
  1. 신규 캐릭터 코드/테스트 배치 경로 및 asmdef 도입 여부 — OPEN 패치에서 사용자 결정 필요(후보: `Assets/_Game/Character/**`, `Assets/_Game/Tests/*/Character/**`).
  2. 잠금 입력 의미(X/Z/C)를 만족하는 활성 inputactions 부재 — CHAR00_02 계약 고정 시 신규 액션 맵 정의 필요.
  3. 캐릭터용 MAP 월드 질의/변경 요청/경계 게이트/준비 상태 API 부재 — CHAR00_02에는 비차단, CHAR03_01 전 MAP 측 계약 범위 승인 필요.
- 재현 정보: 조사 커맨드는 repo-relative 경로 기준 `find`/`grep`(READ ALLOWLIST 내), Unity 버전은 Unity MCP `Application.unityVersion` 조회. 기준 커밋 `24cb1b9`에서 동일 결과 재현 가능.
- 본 실행에서는 규칙에 따라 FINALIZE/OPEN을 수행하지 않았고 commit/push도 하지 않았다.
