# CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS_RESULT

## TASK

TASKS/CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS.md

## STATUS

STATUS: PASS

## SUMMARY

생성 런 스냅샷(방/마이크로청크/루트/아이템/시작/출구 표식) 캐릭터측 계약, 방·마이크로청크 구조 검증(유일성·월드 경계·12×8 정렬·소유 방 포함·중복 점유 거부), CHAR06_01 위임 루트 검증, 아이템 예약 셀(스폰/루트 이탈·진입/명시 금지) 검증, 폭탄/로프 어포던스 검증, 고정 8시드 결정적 스윕(재현 가능 다이제스트·실패 무은폐)을 순수 정책으로 구현했다. MAP 데이터·Tilemap·씬·자산 어떤 것도 변조하지 않는다. 신규 7개 테스트 포함 Game.Character.Tests.EditMode 177/177 PASS(요구 최소 175), 컴파일 에러 0건, 수정 반복 없이 1차 통과.

## READ

- CharacterDesign/MCP/00_MCP_ENTRYPOINT.md ~ 05_CHANGE_CONTROL_RULES.md, 06_IMPLEMENTATION_STATUS.md, MASTER_IMPLEMENTATION_TASK_LIST.md, INPUTS/CHAR00_SOURCE_REGISTRY.md
- CharacterDesign/MCP/TASKS/CHAR06_01 + REPORTS: CHAR06_01, CHAR03_03, CHAR04_04, CHAR05_05
- CharacterDesign/01_FIXED_SPEC/01·03·04·05·06·07, 03_DATA_SCHEMA/ACTION·INVENTORY
- Assets/_Game/Character/Runtime/ 현행 전체 — 재사용 계약: CharacterGeneratedMapStartSnapshot/CharacterGeneratedRouteEdgeSnapshot/CharacterIntegrationBatchPolicy/CharacterRouteCapabilityPolicy(CHAR06_01), CharacterRoomId, ICharacterRoomReadinessSource, CharacterRunInventoryState
- MAP 공용 계약(소스 레지스트리): WorldTileCoord, WorldCoordinateUtility.IsValid, WorldGenConstants(MicroChunkWidthTiles=12, MicroChunkHeightTiles=8, 월드 624×416)
- MAP 검증 테스트 선례(Assets/_Game/Tests/EditMode/Map/) 읽기 전용 참조

### Entry gate verification result

- Current Task = TASKS/CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS.md 확인 — 전 게이트 통과
- CHAR06_01 report hash used: `c93702d78bea0da3260a02594157b5dd40e764ae786325ee4dd93e753eb694ca` (일치) + required_text 4건("170/170 PASS"/"Current Task after finalize: NONE"/과제명/"LOCKED 유지") 확인
- CHAR06_01 Task sha256 `b85b6097...` 일치
- source registry hash used: `be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7` + marker `REGISTRY_STATE: FILLED_BY_CHAR00_01` (일치)
- CHAR06_03·CHAR06_04 LOCKED 확인
- 패키지 배송 이상 1건: `MCP_INBOX/CharacterDesign/MCP_INBOX/CHAR06_02_...` 중첩 경로로 도착 — 확립 절차대로 내용 무변경 이동으로 정규화 후 전 해시 재검증(전부 일치). `requires_locked_task_template` 게이트는 목적지 task 파일 부재 상태라 create 경로로 정상 진행(기존 파일이 있었다면 template sha 비교 대상)

## CHANGED

- 없음 (기존 파일 수정 0건 — Integration/MapIntegration 허용 경로도 변경 불필요: CHAR06_01 정책을 무수정 위임 호출; 조건부 어댑터 경로 미사용; asmdef 변경 없이 신규 폴더 자동 포함을 컴파일로 확인)

## CREATED

Runtime — `Assets/_Game/Character/Runtime/GeneratedRunValidation/` (namespace `StarNight.Character.GeneratedRunValidation`, 신규 폴더, 9파일):

1. CharacterGeneratedRoomSnapshot.cs — {RoomId, MinCell, MaxCell(포함)} + ContainsCell
2. CharacterGeneratedMicrochunkSnapshot.cs — {OwnerRoomId, MinCell, MaxCell}
3. CharacterGeneratedItemPlacementSnapshot.cs — {ItemId, RoomId, Cell}
4. CharacterGeneratedRunSnapshot.cs — {RunId, Seed, Start(CHAR06_01 재사용), Rooms, Microchunks, Routes(CHAR06_01 엣지 재사용), ItemPlacements, ExitMarkers, BlockedValidationCells} — null 방어 빈 목록, 읽기 전용
5. CharacterGeneratedRunValidationDiagnosticKind.cs — 진단 12종
6. CharacterGeneratedRunValidationDiagnostic.cs — {Kind, Subject(item/room/route ID·셀 식별)}
7. CharacterGeneratedRunValidationResult.cs — {RunId, Seed, SpawnRequestCount, RouteRequestCount, Diagnostics, Digest, Passed(진단 0 ∧ 스폰 1)}
8. CharacterGeneratedRunValidationPolicy.cs — Validate: 방 유일성/월드 경계 → 마이크로청크 정렬·소유·중복 → 루트 구조(방 존재·셀 포함) → 아이템(방/월드/예약 셀) → CHAR06_01 BuildBatch 위임(스폰/전환 요청 + 역량/준비 게이트) → FNV-1a 결정적 다이제스트
9. CharacterGeneratedRunSeedSweepPolicy.cs — DefaultSeeds 고정 8종 {11,23,37,41,53,67,79,97}, Sweep(입력 순서 결정적), CountOutcomes(통과/실패/진단 집계 — 숨김 없음)

Tests — `Assets/_Game/Tests/EditMode/Character/GeneratedRunValidation/` (신규 폴더, 5파일):

10. CharacterGeneratedRunFixtures.cs — 테스트 전용 결정적 픽스처 빌더(난수·생성기 없음; 방 A/B, 시드 매개변수 시작 셀·아이템 셀)
11. CharacterGeneratedRoomValidationTests.cs (2 tests)
12. CharacterGeneratedItemsAndAffordanceTests.cs (2 tests)
13. CharacterGeneratedRunSweepTests.cs (1 test)
14. CharacterGeneratedRunValidationGuardTests.cs (2 tests)

(+ Unity 자동 생성 .meta)

## TEST

Unity Version: 6000.3.8f1
EditMode Assembly: Game.Character.Tests.EditMode
Result: **177/177 PASS** (failed 0, skipped 0, 2.053s) — 이전 170 + 신규 7 ≥ 요구 최소 175

요구 동작 7건 → 실제 테스트명 매핑 (전부 요구명 그대로 사용):

| 요구 동작/테스트명 | 실제 테스트 | 파일 |
|---|---|---|
| GeneratedRoom_MicrochunksStayWithinRoomAndWorldBounds | 동일명 (유효 통과 + 비정렬/소유 밖/중복 점유/월드 밖/ID 중복 5결함) | CharacterGeneratedRoomValidationTests.cs |
| GeneratedRoom_RoutesReferenceExistingRoomsAndCreateCharacterRequests | 동일명 (요청 생성 + 방 부재/셀 이탈/미준비 게이트 위임) | CharacterGeneratedRoomValidationTests.cs |
| GeneratedItems_DoNotOccupySpawnEntryExitOrBlockedCells | 동일명 (예약 4종 + 방 밖/방 부재 + 자유 셀 통과) | CharacterGeneratedItemsAndAffordanceTests.cs |
| GeneratedRun_BombAndRopeAffordancesMatchLockedCapabilities | 동일명 (보유 유/무 + 잠금 밖 거부 + 무소모) | CharacterGeneratedItemsAndAffordanceTests.cs |
| RandomRun_SeedSweepIsDeterministicAndReportsReproducibleDiagnostics | 동일명 | CharacterGeneratedRunSweepTests.cs |
| GeneratedRunValidation_DoesNotMutateMapTilemapScenePrefabPlayerTransformRunStateInventoryOrAssets | 동일명 | CharacterGeneratedRunValidationGuardTests.cs |
| GeneratedRunValidation_DoesNotUseAnimatorPhysicsUiAudioSceneSaveOrForbiddenActions | 동일명 | CharacterGeneratedRunValidationGuardTests.cs |

## UNITY

- refresh_unity(force + compile) 1회: 컴파일 정상, `error CS` 필터 콘솔 에러 0건 (Unity compile error count: 0)
- run_tests(EditMode, Game.Character.Tests.EditMode): 177/177 PASS (수정 반복 없이 1차 통과)

## GENERATED_RUN_SNAPSHOT_SOURCE

- 스냅샷은 MAP 공용 도메인 값(WorldTileCoord, WorldGenConstants)과 CHAR03/CHAR06_01 캐릭터 계약(CharacterRoomId, 시작/루트 엣지 스냅샷)만 담는 읽기 전용 값 데이터 — 생성기를 소유하지 않으며 MAP 데이터 편집·Tilemap 쓰기 코드가 없다
- **actual snapshot source used**: 소스 레지스트리 기록대로 MAP에 캐릭터 소비용 공용 "생성 런 투영" 계약이 없어(CHAR06_01에서 확립), 과제가 허용한 "narrow character-side snapshot interface" 방식 채택 — 런타임은 CharacterGeneratedRunSnapshot 계약만 소유하고, 테스트는 시드 매개변수 **결정적 픽스처 빌더**(CharacterGeneratedRunFixtures — 난수·생성기 없이 고정 데이터 조립)로 스냅샷을 공급. 실제 MAP 생성 출력→스냅샷 투영 어댑터는 라이브 통합 계층(후속) 소관으로 DEPENDENCY_LEDGER에 기록. 캐릭터 런타임 안에 방/마이크로청크를 "생성"하는 코드는 없음(금지 조항 준수 — 검증만 소유)
- null/불완전 입력(빈 목록·누락 시작 셀·월드 밖 셀)은 예외 없이 진단으로 흡수

## ROOM_MICROCHUNK_VALIDATION

- 방: ID 유일성(CharacterRoomId 동등성), 경계 사각형이 월드(624×416) 안 + 정방향(min≤max) — 위반 시 DuplicateRoomId/RoomOutsideWorldBounds
- 마이크로청크: 크기 정확히 12×8(WorldGenConstants) + 격자 정렬(min%12==0, min%8==0) → MicrochunkMisaligned; 소유 방 존재 → MicrochunkOwnerRoomMissing; 소유 방 경계 내 포함 → MicrochunkOutsideOwnerRoom; 같은 방 내 같은 위치 중복 → DuplicateMicrochunkOccupancy
- 루트: 출발/도착 방이 방 목록에 존재 → RouteRoomMissing; 이탈/진입 셀이 선언 방 안 → RouteCellOutsideDeclaredRoom
- 전환 요청 생성은 CHAR06_01 CharacterIntegrationBatchPolicy.BuildBatch에 **무수정 위임** — 준비 게이트(CHAR03)·역량 정책까지 그대로 통과하며, 통합 진단은 IntegrationRejected로 래핑되어 원인 종류가 Subject에 보존(미준비 방 거부가 흘러오는 것을 테스트로 확인)

## ITEM_AND_TOOL_AFFORDANCE_VALIDATION

- 아이템: 선언 방 존재(ItemRoomMissing) → 월드 유효 + 방 경계 내(ItemOutsideRoomOrWorld) → 예약 셀 검사(ItemOnReservedCell)
- 예약 셀 = 플레이어 스폰 셀 + 모든 루트의 이탈/진입 셀 + 명시 금지 검증 셀 — 4종 전부 개별 테스트
- 진단 Subject가 아이템 ID·방·셀·사유를 식별("item:5 room:... cell:12,3" + Kind)
- 어포던스: BombSupport/RopeSupport 루트는 CharacterRunInventoryState 보유>0일 때만 요청 생성(CHAR06_01 역량 정책 경유 — MissingBombSupport/MissingRopeSupport 진단 확인), 잠금 밖 요구는 UnsupportedRouteRequirement로 항상 거부 — 판정은 인벤토리를 소모하지 않고 아이템을 생성하지 않음(불변 단언)

## RANDOM_SEED_SWEEP

- **seed list**: DefaultSeeds 고정 8종 `{11, 23, 37, 41, 53, 67, 79, 97}` (요구 최소 8 충족)
- **per-seed result summary**:
  - 유효 픽스처 스윕: 8/8 통과, 진단 0 — 반복 스윕 시 시드별 다이제스트 완전 동일(결정적)
  - 결함 픽스처 스윕(홀수 시드에 루트 진입 셀 침범 아이템 주입 — 기본 8시드 전부 홀수): 8/8 실패, 진단 8건, 각 실패는 시드(result.Seed)·아이템 ID("item:9")·셀("cell:12,3")·사유(ItemOnReservedCell)를 식별, 반복 스윕 다이제스트 동일(재현 가능), 유효/실패 다이제스트 상이(요약이 실제 결과 반영)
- 다이제스트: FNV-1a(runId·seed·스폰/루트 수·전 진단 kind+subject) — 시간/난수 미사용, 같은 입력이면 항상 같음
- CountOutcomes 집계는 통과/실패/진단 수를 그대로 보고 — 실패 은폐·테스트 무시·자산 변조 없음

## AUTHORITY_AND_FORBIDDEN_FEATURE_GUARD

- GeneratedRunValidation 전 타입(9개 이상): MonoBehaviour/Component 아님 + OnCollision*/OnTrigger* 부재 스캔
- 어셈블리 참조: AnimationModule·TilemapModule·UIModule·UnityEngine.UI·AudioModule·TMP 전부 부재 (Physics2DModule은 CHAR01 승인 질의 어댑터 소관 — 확립 검증 레벨)
- 표면 타입 스캔: Animator/Tilemap/Rigidbody/Collider/RaycastHit/Scene/Canvas/Audio/GameObject/PlayerPrefs 부재; 변조형 명명(Instantiate/Destroy/Teleport/LoadScene/SetTile/Mutate/Spend) 부재; 공개 setter·가변 공개 인스턴스 필드 0건
- 행동 검증: 검증 실행 후 인벤토리·준비 소스(질의 카운터)·스냅샷 목록 전부 불변
- 금지 개념 명명 부재 + ActionId 잠금 5종 동등성 단언

## DEPENDENCY_DIRECTION

- StarNight.Character.GeneratedRunValidation → StarNight.Character.Integration (스냅샷 재사용·배치 위임) + MapIntegration (RoomId·준비 소스) + RunState (인벤토리 읽기) + Map.WorldGeneration.Domain (좌표·상수)
- 역방향 없음: Integration/RunState/기타 모듈은 GeneratedRunValidation을 모름(기존 파일 무수정이 구조적 증거); MAP 런타임은 Character를 모름
- asmdef 변경 0건

## SCOPE_VALIDATION

- **all files changed and created**: 신규 14파일(런타임 9 + 테스트 5, 위 CREATED 목록) + .meta — 전부 허용 GeneratedRunValidation 경로. 변경(기존 파일 수정) 0건
- 조건부 어댑터 쓰기(RunState/Equipment) 미사용
- **확인**: MAP 런타임·MAP 저작 데이터(CSV/authoring)·MAP 루트 그래프·MAP 아이템 생성기·MAP 검증 테스트·Tilemap·Scene·prefab·ProjectSettings·Packages·inputactions·UI·audio·save·legacy 코드 변경 0건 (git status 확인; ProjectSettings 기존 사용자 수정 2건은 계속 미접촉)
- 캐릭터 런타임 내 방/마이크로청크 생성 코드 없음(MAP 출력 대체 금지 준수); PlayMode/빌드 검증 미수행(CHAR06_03 소관 준수)

## DEPENDENCY_LEDGER

- 사용(기존 승인): CharacterGeneratedMapStartSnapshot·CharacterGeneratedRouteEdgeSnapshot·CharacterIntegrationBatchPolicy·CharacterIntegrationDiagnostic(CHAR06_01), CharacterRoomId·ICharacterRoomReadinessSource(CHAR03), CharacterRunInventoryState(CHAR05_04), WorldTileCoord·WorldCoordinateUtility.IsValid·WorldGenConstants(MAP 공용), CharacterActionId(테스트 잠금)
- 신규 공개 계약(후속 소비 예정): CharacterGeneratedRunSnapshot(실 MAP 생성 출력→스냅샷 투영 어댑터 — 라이브 통합 계층 소관, 미구현 명시), CharacterGeneratedRunValidationResult/Digest(CHAR06_03 전체 검증·CHAR06_04 감사 증거), CharacterGeneratedRunSeedSweepPolicy(회귀 스윕 재사용)
- 미사용: Tilemap, Animator, Physics2D(신규 범위), UI/Audio/Scene/Save, MAP 생성기 내부, Stage/레거시, 에디터 API

## OUT_OF_SCOPE_FINDINGS

- 실제 MAP 생성 출력(MoonPalace 생성 파이프라인)을 CharacterGeneratedRunSnapshot으로 투영하는 생산자 어댑터는 공용 계약 부재로 본 과제 밖 — MAP측 공용 투영 계약이 생기면 필드 호환으로 연결 가능(CHAR06_01 OUT_OF_SCOPE와 동일 계보, 라이브 배선 단계 소관)
- 패키지 중첩 배송(`MCP_INBOX/CharacterDesign/MCP_INBOX/...`)이 재발(CHAR02/03 시대와 동일 증상) — 내용 무변경 정규화로 처리했으나 패키지 생성측 압축 경로 점검 권장
- Assets/_Game/Tests/PlayMode/Map asmdef의 stale `Game.Stage.Runtime` 참조 — MAP 하니스 소관, 계속 미수정 유지 (CHAR06_03 PlayMode 검증 시 재확인 예정)

## DONE CONDITIONS

- [x] Entry gate 전부 검증 (CHAR06_01 PASS/hash, source registry marker/hash, CHAR06_03·04 LOCKED).
- [x] 생성 런 스냅샷이 MAP 공용 계약만 사용하며 MAP 데이터를 편집하지 않음.
- [x] 방 유일성·월드 경계·마이크로청크 정렬/소유/중복 검증 구현·통과.
- [x] 루트 참조 검증 + CHAR06_01 위임 요청 생성 확인.
- [x] 아이템 예약 셀(스폰/이탈/진입/금지) 검증 + 진단 식별자 포함.
- [x] 폭탄/로프 어포던스가 잠금 역량·보유량과 일치, 무소모.
- [x] 고정 8시드 스윕 결정적 + 실패 무은폐 + 재현 가능 진단.
- [x] Character EditMode 177/177 PASS (≥175).
- [x] Unity compile errors 0.
- [x] 감사 범위 확인: MAP/Tilemap/scene/prefab/ProjectSettings/Packages/inputactions/UI/audio/save/legacy 변경 0건.
- [x] CHAR06_03 remains locked.

## NEXT

Current Task after finalize: NONE
Next Task auto-opened: NO (`CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD`는 LOCKED 유지, 새 MCP_INBOX 패키지로만 개방)
