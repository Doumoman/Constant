# CHAR00_01 RESULT

STATUS: PASS

TASK: CHAR00_01
COMMIT: NOT_COMMITTED

작성 시점 기준. 작업 규칙상 커밋은 PASS 검증 후 FINALIZE 단계에서 생성되고 본 파일이 그 커밋에 포함되므로 해시를 사전 기록할 수 없다. 커밋 제목은 `CHAR00_01: 캐릭터·입력·물리·카메라·MAP 접점 조사`를 사용한다.

## 변경 파일

- `CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md` — 사용자 제공 초안(DRAFT_FOR_CHAR00_01)을 실제 조사 결과로 갱신(REGISTRY_STATE: FILLED_BY_CHAR00_01)
- `CharacterDesign/MCP/RESULTS/CHAR00_01_INVENTORY_CHARACTER_INPUT_PHYSICS_MAP_RESULT.md` — 본 RESULT 신규 작성

위 2개 외 변경 없음(WRITE ALLOWLIST 전체와 일치). `CharacterDesign/**`는 Assets 밖이므로 `.meta` 생성 없음.

## 구현 내용

조사 전용 작업으로 프로젝트 코드 변경은 0이다. 조사 기준선: `main` @ `24cb1b9`, Unity 6000.3.8f1. 전체 조사표는 registry에 있고 요약은 다음과 같다.

- 활성 캐릭터 런타임 부재 확인: 컴파일되는 게임 코드는 `Assets/_Game/**`의 MAP 생성 파이프라인뿐
- 레거시 격리 확인: `Assets/_Legacy/**` `.cs` 672/672개가 `#if LEGACY_DISABLED`로 컴파일 제외(정의하는 asmdef 없음). 선례 2세대: 구세대 `_Legacy/_Game`, 신세대 `_Legacy/StarNight`(RW1)
- 캐릭터 선례 스택: `PlayerMotor2D`(Rigidbody2D+CapsuleCollider2D 0.72×0.90, 수동 중력 24, jumpHeight 2.2셀), `PlayerInputAdapter`, `GroundProbe2D`(CapsuleCast, probe 0.08), `JumpGraceState`(coyote 0.10/buffer 0.12, 지상 게이트 단일 점프), `P1MovementTuning`(SO), `PlayerRecovery`(maxHealth 4), `CarrySystem`(단일 슬롯), `Bomb2D`+`ExplosionService2D`(3×3 마스크), Rope/Mining/Pestle/Umbrella/Water/Grapple 도구군
- 입력 계약: InputSystem 1.18.0, activeInputHandler Both. 레거시 `StarNightControls.inputactions`(Jump=Space, Interact=E, UseBomb=F, UseRope=Q)는 잠금 의미(X=행동/Z=폭탄/C=로프)와 불일치. Jump 장치 키 선례 = Space. 자산 사본 2개 상이
- 물리 기준 선례: 1 world unit = 1 논리 셀(`GridWorld.CellSize=1f`), 경사 정책 NONE. Solid 레이어 값은 Scene/Prefab 소유라 UNKNOWN
- 카메라 선례: `RoomFocusCamera2D`+`RoomBounds2D`(기본 12×8) — 연출 전용, 이동 월드 비분리(잠금 규칙 호환). Hysteresis 구현 0건
- MAP 접점: 공용 좌표 계약 존재(`WorldTileCoord/SectorCoord/MicroChunkCoord/LocalTileCoord`, `WorldCoordinateUtility`, World 624×416/Sector 48×32/MicroChunk 12×8/레이어 8, `MicrochunkTileLayer`에 GroundSolid/OneWay/Breakable/Hazard/Liquid 등). 캐릭터용 월드 질의/지형 변경 요청/경계 게이트/방 준비 API는 부재 확인. MAP 상태: MAP07_02까지 COMPLETE, Current Task NONE
- 기존 이상 관찰(미수정): `Assets/_Game/Tests/PlayMode/Map/Game.Map.Tests.PlayMode.asmdef`가 테스트 0개 상태로 레거시 `Game.Stage.Runtime` 참조(stale)

## 컴파일

- command/tool: 코드 무변경(문서 2개만 작성)이므로 컴파일 대상 없음. Unity MCP `read_console`(신규 오류 확인), `execute_code`(`Application.unityVersion`, `EditorApplication.isCompiling` 조회)로 에디터 상태 확인
- result: Unity 6000.3.8f1, isCompiling=False, 신규 Console 오류 0. 기존/신규 오류 분리 이슈 없음(코드 무변경)

## 테스트

- expected count: 5
- executed count: 5
- passed: 5
- failed: 0

개별 결과:

1. SourceRegistryCreated: PASS — 조사한 경로·assembly·입력·물리·카메라·MAP 접점을 `CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md` §0~§10에 기록
2. NoProjectMutation: PASS — `git status --porcelain -- Assets Packages ProjectSettings MapDesign` 기준 본 작업 변경 0개. 잔존 M 2개(`ProjectSettings/Packages/dev.yarnspinner/Assembly-CSharp-generated.ysls.json`, `ProjectSettings/ShaderGraphSettings.asset`)는 작업 전부터 존재한 사용자 변경으로 registry §0 기준선에 기록, 본 작업 미접촉
3. ResultPathExact: PASS — 본 파일 경로가 TASK 지정 RESULT PATH와 일치
4. UnknownsExplicit: PASS — 미확정 항목은 추측 없이 UNKNOWN(Solid 레이어 값, 물리 레이어 구성 값) 또는 BLOCKER(registry §10, 3건)로 기록
5. StatusExact: PASS — 상태를 독립된 한 줄 `STATUS: PASS`로 기록

## 잔여 문제

- BLOCKER 3건(registry §10 상세):
  1. 신규 캐릭터 코드/테스트 배치 경로 및 asmdef 도입 여부 — OPEN 패치에서 사용자 결정 필요(후보: `Assets/_Game/Character/**`, `Assets/_Game/Tests/*/Character/**`)
  2. 잠금 의미(X/Z/C)를 만족하는 활성 inputactions 부재 — CHAR00_02 계약 고정 시 신규 액션 맵 정의 필요
  3. 캐릭터용 MAP 월드 질의/변경 요청/경계 게이트/준비 상태 API 부재 — CHAR00_02에는 비차단, CHAR03_01 전 범위 승인 필요
- 재현 정보: 기준 커밋 `24cb1b9`에서 READ ALLOWLIST 내 `find`/`grep` 및 Unity MCP 조회로 동일 결과 재현 가능
- 본 실행에서는 FINALIZE/OPEN을 수행하지 않으며 commit/push도 하지 않는다(각각 별도 패치·FINALIZE 단계 소관)
