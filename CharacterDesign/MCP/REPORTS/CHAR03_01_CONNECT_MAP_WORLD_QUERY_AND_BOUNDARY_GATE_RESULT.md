# TASK RESULT

TASK: CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE
STATUS: PASS

## SUMMARY

repair revision(MAP_REFERENCE_GUARD_SCOPE)에 따라 구식 CHAR01 시대 의존성 가드를 수리(삭제 아님)하고, 캐릭터 런타임을 MAP 공용 좌표·도메인 계약에 연결했다: 좌표 브리지(WorldCoordinateUtility 위임, clamp 없는 범위 거부), 캐릭터용 read-only 월드 질의 계약(solid/one-way/hazard/liquid/breakable/empty), 방 경계 준비 게이트(판정 전용 — prepared 허용, unprepared/missing 차단, 방 내부 무영향, 입력·속도 비변조). EditMode 66/66 PASS(수리된 가드 포함 기존 57 + 신규 9). 라이브 생성 맵 데이터 소스는 과제 허용대로 CHAR06 이연.

## READ

- Entry Gate: 직전 BLOCKED REPORT sha `b4e37ef7…` + 차단 문구 3건, CHAR02_03 PASS sha `e118ac9d…` + APPROVED 문구, registry sha/marker, CHAR03_02 이후 LOCKED — 전부 일치(Phase A에서 7게이트 검증)
- Mandatory Read Order 22개 항목(수리 대상 가드 파일 포함, MAP Domain/Microchunks 공용 모델)

## CHANGED

- `Assets/_Game/Character/Runtime/Game.Character.Runtime.asmdef` — references `[]` → `["Game.Map.Runtime"]` (승인된 유일한 변경)
- `Assets/_Game/Tests/EditMode/Character/Game.Character.Tests.EditMode.asmdef` — `Game.Map.Runtime` 참조 추가(신규 테스트가 MAP 도메인 타입 사용)
- `Assets/_Game/Tests/EditMode/Character/CharacterGroundProbeTests.cs` — 구식 가드 `GroundProbe_DoesNotRequireMapOrTilemapTypes`("Game.Map* 전면 금지")를 `GroundProbe_RuntimeDependsOnlyOnApprovedAssemblies`로 수리·개명: `Game.Map.Runtime` 정확히 1개 허용(EquivalentTo 고정), TilemapModule/InputSystem/Game.Stage.Runtime/StarNight.Runtime/MapAuthoring*/editor·test 어셈블리는 계속 금지. 의존 방향 커버리지는 삭제되지 않고 강화됨

## CREATED

Runtime (`Assets/_Game/Character/Runtime/MapIntegration/`, namespace `StarNight.Character.MapIntegration`, 7개):

- `CharacterMapCoordinateBridge.cs` — 월드 좌표↔타일. floor 후 `WorldCoordinateUtility.TryCreateWorldTile`에 경계 검증 위임(clamp 없음, 실패 시 default), `GetCellOrigin/GetCellCenter`. `WorldUnitsPerCell = 1f`(잠금 스케일 계약의 캐릭터 측 단일 소스 — MAP은 타일 단위만 정의)
- `CharacterMapCellState.cs` — solid/oneWay/hazard/liquid/breakable + IsEmpty 값 객체. `FromTileLayer(MicrochunkTileLayer)` 매핑(GroundSolid→solid, Breakable→solid+breakable, Decoration/Marker→empty), `Combine` 레이어 합성
- `ICharacterMapWorldQuery.cs` — `TryGetCellState(WorldTileCoord, out state)` read-only 계약(false=미생성 데이터)
- `CharacterRoomId.cs` — (Sector, MicroChunk) 방 식별자, `FromWorldTile`은 `WorldCoordinateUtility.ToSector/ToMicroChunk` 위임
- `ICharacterRoomReadinessSource.cs` — 방 준비 상태 read-only 소스(false=방 정보 없음)
- `CharacterBoundaryCrossDecision.cs` — NotABoundaryCrossing/Allowed/BlockedUnpreparedRoom/BlockedMissingRoom
- `CharacterRoomBoundaryGate.cs` — `Evaluate(from, to)` 판정 전용 + `MayCross`. 카메라/스냅/입력 억제/속도 재작성/hysteresis 없음(CHAR03_02 소관)

EditMode Tests (`Tests/EditMode/Character/MapIntegration/`, 4개): `CharacterMapDependencyDirectionTests.cs`, `CharacterMapCoordinateBridgeTests.cs`, `CharacterMapWorldQueryTests.cs`(fake 질의 소스), `CharacterRoomBoundaryGateTests.cs`(fake 준비 소스)

Unity 생성 `.meta`: MapIntegration 폴더 2 + 신규 .cs 11 = 13개(허용 범위, 기록)

Report: 본 파일(직전 BLOCKED 판을 지정대로 교체 — 이전 판은 repair manifest 해시로 이력 고정)

## TEST

실행: Unity MCP `run_tests`(EditMode, assembly=Game.Character.Tests.EditMode 전체, job `272083a8…`)

| 항목 | 값 |
|---|---|
| 전체/성공/실패/스킵 | 66 / 66 / 0 / 0 (resultState=Passed, 0.94s — 최소 66 충족) |
| 기존 57개(가드 1개는 수리·개명) | 전부 Passed |
| 신규 9개 | 전부 Passed |

요구 행위 ↔ 실제 테스트 매핑(9/9):

1. DependencyGuard… → `DependencyGuard_AllowsOnlyGameMapRuntimeAndRejectsTilemapAuthoringLegacy` (+수리된 `GroundProbe_RuntimeDependsOnlyOnApprovedAssemblies`)
2. `CoordinateBridge_UsesMapWorldCoordinateUtility`
3. `CoordinateBridge_RejectsOutOfBoundsWithoutClamping`
4. `MapWorldQuery_ReportsSolidHazardOneWayLiquidBreakableAndEmpty`
5. `MapWorldQuery_DoesNotUseTilemapOrMicroChunkInternals`
6. `BoundaryGate_BlocksUnpreparedDestinationRoom`
7. `BoundaryGate_BlocksMissingDestinationRoom`
8. `BoundaryGate_AllowsPreparedDestinationRoom`
9. `BoundaryGate_DoesNotMutateInputOrVelocity`

MAP 테스트 어셈블리 미실행 근거: 조건부 `PublicContracts/**` 미사용 — MAP 런타임·asmdef·데이터 파일 변경 0(`git status -- Assets/_Game/Map` 0건)이므로 MAP 컴파일 산출물이 변하지 않았다.

## UNITY

- Unity Version: 6000.3.8f1
- Asset Refresh: PASS (asmdef 변경에 따른 에디터 도메인 리로드로 MCP 브리지가 일시 재시작 — 재연결 후 완료)
- Compile Errors: 0 (CS 0건. 브리지 "disposed object" 재연결 로그와 기존 환경 로그(InputManager 안내, Unity AI NoSubscription)는 코드 무관, 기록만)
- Relevant New Warnings: 0
- EditMode: 66/66, PlayMode: NOT RUN(과제 지정), Scene/Prefab Changes: 0

## CHANGE_CONTROL

- 승인 문서: `CHAR03_01_REPAIR_MAP_REFERENCE_GUARD_SCOPE` 패치(직전 BLOCKED REPORT sha 게이트)
- 변경 전: 캐릭터 런타임 asmdef references `[]` + "Game.Map* 전면 금지" 가드
- 변경 후: references `["Game.Map.Runtime"]` + "정확히 Game.Map.Runtime 1개 허용, 나머지 금지 유지" 가드
- 가드는 삭제가 아니라 수리·개명(약화 아님 — 금지 목록은 오히려 확대: Game.Stage.Runtime/legacy/editor·test 명시)

## MAP_COORDINATE_BRIDGE

사용한 MAP 공용 API: `StarNight.Map.WorldGeneration.Domain.WorldTileCoord`, `WorldCoordinateUtility.TryCreateWorldTile`(경계 검증), `WorldGenConstants`(테스트 경계값), `WorldCoordinateUtility.ToSector/ToMicroChunk`(방 식별). 좌표 수학 복제 0 — floor+스케일(1u/cell 계약)만 캐릭터 측이 소유. 범위 밖은 clamp 없이 거부(검증: -0.5/상한+0.5/상한 경계 셀).

## MAP_WORLD_QUERY_CONTRACT

`ICharacterMapWorldQuery` + `CharacterMapCellState`. 타일 의미 원천은 `MicrochunkTileLayer`(공용 enum)뿐이며 Tilemap/scene/CSV/배치 내부/생성 pass 비접촉(표면 타입 스캔 테스트로 고정). 라이브 데이터 소스: **DEFERRED to CHAR06**(과제 명시 허용) — fake 기반 결정적 검증 완료.

## ROOM_BOUNDARY_READINESS_GATE

방 단위 = 마이크로청크(12×8), 식별 = (Sector, MicroChunk). 판정: 동일 방 → NotABoundaryCrossing(무영향), 준비 방 → Allowed, 미준비 → BlockedUnpreparedRoom, 정보 없음 → BlockedMissingRoom. 입력·속도 비변조(값 검증 + Evaluate 시그니처가 WorldTileCoord 2개뿐임을 리플렉션 고정). hysteresis/카메라/KEEP 적용은 CHAR03_02 소관으로 미구현.

## DEPENDENCY_DIRECTION

- 캐릭터 런타임 참조 중 Game.Map* = 정확히 `["Game.Map.Runtime"]`(테스트 2중 고정)
- TilemapModule/InputSystem/MapAuthoring*/editor·test/legacy/Game.Stage.Runtime 참조 0
- one-way 확인: `Game.Map.Runtime`의 참조 어셈블리에 `Game.Character*` 0(테스트 고정)
- 전역 싱글톤 lookup 없음: MapIntegration 타입의 public static은 상수/readonly뿐(테스트 고정)

## SCOPE_VALIDATION

- `git status -- Assets`: Character 트리 외 변경 0. `Assets/_Game/Map` 변경 0, Packages/MapDesign 0, ProjectSettings 기존 사용자 2건 외 0
- 수정 파일 = 허용 3개(asmdef 2 + 가드 1), 신규 = MapIntegration 런타임 7 + 테스트 4 + .meta
- 카메라 전환/hysteresis/지형 변경 요청/생성 맵 통합 미구현. CHAR03_02 미개방·미열람

## DEPENDENCY_LEDGER

```text
MAP world query / coordinate conversion    : CONNECTED (공용 좌표 API 위임 + 질의 계약 신설)
Live generated-map query data source       : DEFERRED (CHAR06 — fake 검증 완료)
Room boundary detection and readiness gate : IMPLEMENTED (판정 전용, 소스는 CHAR06 연결)
Camera room transition policy              : DEFERRED (CHAR03_02)
Terrain mutation request API               : DEFERRED (CHAR05 연계)
Generated map route integration            : DEFERRED (CHAR06)
```

## OUT_OF_SCOPE_FINDINGS

- asmdef 변경발 도메인 리로드 시 MCP 브리지가 일시 끊김("No Unity Editor instances") 후 자동 복구 — 환경 특성 기록(코드 무관)
- stale Map PlayMode asmdef 레거시 참조(기존 관찰 유지)

## DONE CONDITIONS

- [x] 직전 CHAR03_01 BLOCKED REPORT를 정확한 SHA·차단 문구로 검증
- [x] CHAR02_03 PASS·CHAR02 EXIT 승인 검증
- [x] registry marker/hash 검증
- [x] 구식 map-reference 가드를 삭제가 아닌 수리로 갱신
- [x] 캐릭터 런타임의 MAP 참조는 승인 범위뿐
- [x] 좌표 브리지: MAP 유틸리티 위임 + clamp 없는 범위 거부
- [x] 월드 질의 계약: solid/one-way/hazard/liquid/breakable/empty 커버
- [x] 경계 게이트: unprepared/missing 차단
- [x] 경계 게이트: prepared 허용
- [x] 판정이 입력·속도 비변조
- [x] 카메라 전환·hysteresis 미구현
- [x] Tilemap/scene/prefab/inputactions/Packages/ProjectSettings/MapDesign/legacy 무변경
- [x] EditMode 66개(≥66) 전부 PASS
- [x] compile error 0
- [x] CHAR03_02 LOCKED 유지

## NEXT

- Current Task after finalize: NONE
- Next Task auto-opened: NO (`CHAR03_02_IMPLEMENT_CAMERA_ROOM_TRANSITION_POLICY`는 LOCKED 유지, 새 MCP_INBOX 패키지로만 개방)
