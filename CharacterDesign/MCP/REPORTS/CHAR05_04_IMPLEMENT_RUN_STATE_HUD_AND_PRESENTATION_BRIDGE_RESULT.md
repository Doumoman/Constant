# CHAR05_04_IMPLEMENT_RUN_STATE_HUD_AND_PRESENTATION_BRIDGE_RESULT

## TASK

TASKS/CHAR05_04_IMPLEMENT_RUN_STATE_HUD_AND_PRESENTATION_BRIDGE.md

## STATUS

STATUS: PASS

## SUMMARY

불변 런 인벤토리 상태(시작 폭탄 4/로프 4 중앙화)와 CHAR05_01/02 소모 요청 소비, Survival 계약 기반 런 상태 스냅샷(활성/실패 + 복귀 토큰), HUD 스냅샷 데이터 브리지, 연출 이벤트 요청(피해/사망/런 실패/폭탄/로프/인벤토리)과 결정적 정렬·중복 제거 배치 정규화를 순수 값 객체 + 정적 정책으로 구현했다. 실제 HUD/Canvas/TMP/오디오/애니메이션/씬/세이브/GameObject 어떤 것도 건드리지 않는다. 신규 12개 테스트 포함 Game.Character.Tests.EditMode 158/158 PASS, 컴파일 에러 0건, 수정 반복 없이 1차 통과.

## READ

- CharacterDesign/MCP/00_MCP_ENTRYPOINT.md ~ 05_CHANGE_CONTROL_RULES.md (전역 규칙)
- CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md, MASTER_IMPLEMENTATION_TASK_LIST.md
- CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md
- CharacterDesign/MCP/TASKS + REPORTS: CHAR05_01, CHAR05_02, CHAR05_03 (과제/결과 전부)
- CharacterDesign/01_FIXED_SPEC/01·02·05·06·07 규칙 문서
- CharacterDesign/03_DATA_SCHEMA/CHARACTER_INVENTORY_SCHEMA.md (bombCount/ropeCount는 소모품 수량, 휴대 슬롯과 별개), CHARACTER_DAMAGE_SCHEMA.md, CHARACTER_ACTION_SCHEMA.md
- Assets/_Game/Character/Runtime/ 현행 전체 — 특히 소비 대상: CharacterBombSpendRequest/CharacterRopeSpendRequest(Equipment), CharacterHealthState/CharacterDamageApplicationResult/CharacterDeathRequest/CharacterRunFailureRequest(Survival), CharacterBombPlacementRequest/CharacterExplosionRequest/CharacterRopePlacementRequest
- 레거시 읽기 전용 선례: Assets/_Legacy/_Game/Core/State/RunState.cs — CreateNew에서 health=4, ropes=4, bombs=4, failureReason 문자열 데이터

Entry Gate 검증 (Phase A에서 수행):

- Current Task = TASKS/CHAR05_04_IMPLEMENT_RUN_STATE_HUD_AND_PRESENTATION_BRIDGE.md 확인
- CHAR05_03 REPORT: STATUS: PASS + sha256 `d982d596a0efad856db4e8dbaf475538172b9ac8ab11baf4af85bb87b982c03c` 일치 + required_text 3건 확인
- CHAR05_03 Task sha256 `4d775009...` 일치
- Source Registry marker `REGISTRY_STATE: FILLED_BY_CHAR00_01` + sha256 `be6cadc4...` 일치
- CHAR05_05 이후 전부 LOCKED 확인

## CHANGED

- 없음 (기존 파일 수정 0건 — 체력/위험/폭탄/로프/전투/이동/MAP 코드 전부 불변; asmdef 변경 없이 기존 어셈블리가 신규 RunState/Presentation 폴더를 자동 포함함을 컴파일로 확인)

## CREATED

Runtime — `Assets/_Game/Character/Runtime/RunState/` (namespace `StarNight.Character.RunState`, 신규 폴더, 6파일):

1. CharacterRunStateSettings.cs — 중앙 설정: 시작 폭탄 4 / 로프 4 (레거시 RunState.CreateNew 선례), 유효성 검증
2. CharacterRunInventoryState.cs — 불변 인벤토리 {ActorId, BombCount, RopeCount}, 음수 clamp, CreateStarting(중앙 설정 사용)
3. CharacterRunInventoryApplyResult.cs — 적용 결과 {NewState, AppliedAmount, Changed}
4. CharacterRunInventoryPolicy.cs — CHAR05_01/02 소모 요청을 그대로 소비(RunState측 어댑터): 대상 불일치·비양수 무시, 보유량까지만 적용(0 미만 불가), 새 상태 반환
5. CharacterRunStatus.cs — enum {Active, Failed}
6. CharacterRunState.cs — 불변 런 상태 {ActorId, Health(Survival), Inventory, Status, ReturnDestinationToken}; CreateActive/WithHealth/WithInventory/ApplyRunFailure(본인 대상만 Failed+토큰)

Runtime — `Assets/_Game/Character/Runtime/Presentation/` (namespace `StarNight.Character.Presentation`, 신규 폴더, 4파일):

7. CharacterHudSnapshot.cs — HUD 데이터 {CurrentHealth, MaxHealth, IsInvulnerable, BombCount, RopeCount, RunStatus, ReturnDestinationToken}; FromRunState(결정적 파생)
8. CharacterPresentationEventType.cs — enum {RunFailure, Death, Damage, BombExploded, BombPlaced, RopePlaced, InventoryChanged}
9. CharacterPresentationEventRequest.cs — 이벤트 요청 {Type, ActorOrSourceId, HasAmount+Amount, HasCell+WorldTileCoord, SequenceId}
10. CharacterPresentationBridge.cs — 변환기(TryCreateDamageEvent(적용분>0만)/CreateDeathEvent/CreateRunFailureEvent/CreateBombPlacedEvent/CreateBombExplodedEvent/CreateRopePlacedEvent/TryCreateInventoryChangedEvent(변화 시만)) + NormalizeBatch(우선순위→입력 순서 안정 정렬, 동등 이벤트 1회, 출력 순서로 SequenceId 부여)

Tests (신규 폴더 2곳, 4파일):

11. Assets/_Game/Tests/EditMode/Character/RunState/CharacterRunInventoryTests.cs (3 tests)
12. Assets/_Game/Tests/EditMode/Character/RunState/CharacterRunStateTests.cs (3 tests)
13. Assets/_Game/Tests/EditMode/Character/Presentation/CharacterHudSnapshotAndBridgeTests.cs (5 tests)
14. Assets/_Game/Tests/EditMode/Character/Presentation/CharacterRunStatePresentationGuardTests.cs (1 test)

(+ Unity 자동 생성 .meta)

## TEST

Unity Version: 6000.3.8f1
EditMode Assembly: Game.Character.Tests.EditMode
Result: 158/158 PASS (failed 0, skipped 0, 5.553s) — 이전 146 + 신규 12, 1차 전건 통과

요구 테스트명 12건 → 실제 테스트명 매핑 (전부 요구명 그대로 사용):

| 요구 동작/테스트명 | 실제 테스트 | 파일 |
|---|---|---|
| RunInventory_DefaultBombAndRopeCountsAreCentralized | 동일명 (4/4 중앙 설정 + 음수 clamp) | CharacterRunInventoryTests.cs |
| RunInventory_BombAndRopeSpendRequestsDecreaseCounts | 동일명 (CHAR05_01/02 요청 타입 직접 소비) | CharacterRunInventoryTests.cs |
| RunInventory_SpendCannotGoBelowZeroOrMutateInput | 동일명 (0 바닥·입력 불변·불일치/비양수 무시) | CharacterRunInventoryTests.cs |
| RunState_HealthSnapshotReflectsSurvivalState | 동일명 (Survival 적용 결과 반영 + 원본 불변) | CharacterRunStateTests.cs |
| RunState_PlayerRunFailureMarksRunFailedWithReturnToken | 동일명 (사망→실패 요청→Failed+토큰 사슬) | CharacterRunStateTests.cs |
| RunState_NonPlayerDeathDoesNotFailPlayerRun | 동일명 (적 사망·타 액터 실패 요청 모두 무시) | CharacterRunStateTests.cs |
| HudSnapshot_ContainsHealthInventoryStatusAndReturnToken | 동일명 (활성/실패 양쪽 + 결정성) | CharacterHudSnapshotAndBridgeTests.cs |
| HudSnapshot_IsDataOnlyAndDoesNotUseUnityUiSceneAudioOrSave | 동일명 (속성 타입 화이트리스트 + setter 부재 + 명명 가드) | CharacterHudSnapshotAndBridgeTests.cs |
| PresentationBridge_DamageDeathAndRunFailureCreateEventRequests | 동일명 (적용분>0만·억제 시 없음 포함) | CharacterHudSnapshotAndBridgeTests.cs |
| PresentationBridge_BombRopeAndInventoryEventsAreRequestsOnly | 동일명 (셀 좌표 운반·변화 시만·불변 값 객체) | CharacterHudSnapshotAndBridgeTests.cs |
| PresentationBridge_EventsAreDeterministicOrderedAndDeduplicated | 동일명 (뒤섞인 입력+중복 2건 → 4건 우선순위 순, 반복 호출 동일) | CharacterHudSnapshotAndBridgeTests.cs |
| RunStatePresentationRuntime_DoesNotUseAnimatorPhysicsSceneHudSaveAudioOrForbiddenActions | 동일명 | CharacterRunStatePresentationGuardTests.cs |

## UNITY

- refresh_unity(force + compile) 1회: 컴파일 정상, `error CS` 필터 콘솔 에러 0건
- run_tests(EditMode, Game.Character.Tests.EditMode): 158/158 PASS (수정 반복 없이 1차 통과)

## RUN_INVENTORY_STATE

- CharacterRunInventoryState {ActorId, BombCount, RopeCount} 불변 값 객체 — 생성자 음수 clamp; CHARACTER_INVENTORY_SCHEMA의 소모품 수량 계약(휴대 슬롯과 별개) 대응
- 시작 수량은 CharacterRunStateSettings.Default 한 곳에 중앙화: 폭탄 4/로프 4 (레거시 RunState.CreateNew bombs=4, ropes=4 선례)
- 소모 적용은 CHAR05_01 CharacterBombSpendRequest·CHAR05_02 CharacterRopeSpendRequest를 그대로 입력으로 소비(RunState측 어댑터 — 기존 파일 무수정, 과제의 "prefer RunState-side adapter" 채택으로 조건부 브리지 쓰기 불필요)
- 규칙: 대상 ActorId 불일치·비양수 요청 → 변화 없음(AppliedAmount 0); 요청량 > 보유량이면 보유량까지만 적용해 0 바닥 보장; 항상 새 상태 반환·입력 불변(테스트로 확인)

## RUN_STATUS_AND_HEALTH_SNAPSHOT

- CharacterRunState {ActorId, Health(CharacterHealthState 스냅샷), Inventory, Status, ReturnDestinationToken} 불변 값 객체
- CreateActive → Active 시작; WithHealth/WithInventory로 Survival/인벤토리 적용 결과를 스냅샷 갱신(새 상태 반환, 원본 불변)
- ApplyRunFailure: 본인 대상 런 실패 요청만 Status=Failed + 복귀 토큰 기록; 타 액터 대상 요청은 무시
- 적/비플레이어 사망은 CHAR05_03 정책상 런 실패 요청 자체가 생성되지 않음(사슬 테스트) + 타 액터 실패 요청 무시(방어) — 이중 보장
- 복귀 토큰은 끝까지 불투명 문자열 데이터 — 씬 리로드·세이브 변조 없음

## HUD_SNAPSHOT_BRIDGE

- CharacterHudSnapshot: 체력(현재/최대/무적 플래그), 폭탄/로프 수량, 런 상태, 복귀 토큰 — FromRunState로 런 상태에서 결정적으로 파생(같은 입력 → 같은 출력 테스트)
- 데이터 전용 보증: 공개 속성 타입이 {Int32, Boolean, String, CharacterRunStatus} 화이트리스트에 한정됨을 리플렉션으로 단언, 공개 setter 0건, Canvas/TextMesh/Audio/Scene/PlayerPrefs/GameObject/Animator 명명 부재
- 실제 HUD 바인딩(UI 위젯·텍스트 할당)은 이 과제 밖 — 데이터 브리지만 소유

## PRESENTATION_EVENT_REQUESTS

- 6종 변환: 피해(CharacterDamageApplicationResult, 실제 적용분>0만 — 무적 억제 시 이벤트 없음), 사망(CharacterDeathRequest), 런 실패(CharacterRunFailureRequest), 폭탄 설치(CharacterBombPlacementRequest, 셀 운반), 폭발(CharacterExplosionRequest, 셀+피해량 운반), 로프 설치(CharacterRopePlacementRequest, 셀 운반), 인벤토리 변화(CharacterRunInventoryApplyResult, Changed일 때만)
- 이벤트 기록: 종류, 액터/원인 ID, 선택적 수량(HasAmount), 선택적 셀 좌표(HasCell+WorldTileCoord), 결정적 SequenceId
- 요청 값 객체일 뿐 오디오·애니메이션·파티클·카메라·UI·씬 효과를 재생하지 않음(공개 setter 부재 단언 포함)

## EVENT_ORDERING_AND_DEDUPLICATION

- NormalizeBatch: 우선순위 버킷(런 실패 0 → 사망 1 → 피해 2 → 폭발 3 → 폭탄 설치 4 → 로프 설치 5 → 인벤토리 6)을 오름차순 순회하며 입력 순서를 보존 — List.Sort의 불안정성을 피한 안정·결정적 정렬
- 동등 이벤트(SequenceId 제외 전 필드 비교)는 같은 배치에서 한 번만 방출; SequenceId는 출력 순서대로 0..n-1 재부여
- 테스트: 뒤섞인 입력 6건(중복 2건 포함) → 4건이 정확한 우선순위 순서로, 반복 호출 시 완전히 동일한 출력

## AUTHORITY_AND_FORBIDDEN_FEATURE_GUARD

- RunState/Presentation 전 타입(10개 이상): MonoBehaviour/Component 아님 → 물리 콜백 불가 + OnCollision*/OnTrigger* 부재 직접 스캔
- 어셈블리 참조: AnimationModule·TilemapModule·UIModule·UnityEngine.UI·AudioModule·Unity.TextMeshPro 전부 부재 — Animator/UI/오디오/TMP가 권위가 될 수 없음 (Physics2DModule 참조는 CHAR01 승인 질의 어댑터 소관 — CHAR05_01~03과 동일한 확립 검증 레벨)
- 표면 타입 스캔: Animator/Tilemap/Rigidbody/Collider/RaycastHit/Scene/Canvas/Audio/GameObject/PlayerPrefs 부재
- 명명 가드: BasicAttack/Melee/Shoot/Dash/WallJump/DoubleJump/LoadScene/Reload/PlayerPrefs/PlayAudio/PlayAnimation 부재
- ActionId 잠금 5종 그대로 + CharacterRunStatus는 {Active, Failed} 데이터 2종뿐임을 동등성 단언

## DEPENDENCY_DIRECTION

- StarNight.Character.RunState → StarNight.Character.Survival (체력 상태·런 실패 요청 소비) + StarNight.Character.Equipment (소모 요청 소비)
- StarNight.Character.Presentation → RunState + Survival + Equipment + StarNight.Map.WorldGeneration.Domain (WorldTileCoord)
- 역방향 없음: Survival/Equipment/Combat/Movement는 RunState/Presentation을 모름(기존 파일 무수정이 구조적 증거); MAP 런타임은 Character를 모름
- asmdef 변경 0건 — 기존 Game.Character.Runtime이 신규 폴더 자동 포함

## SCOPE_VALIDATION

- git status 확인: 신규 파일 전부 허용 경로 내 — RunState 런타임 6 + Presentation 런타임 4 + RunState 테스트 2 + Presentation 테스트 2 (+ .meta)
- 조건부 브리지 쓰기(Survival/Equipment) 미사용 — 어댑터를 RunState/Presentation측에 두어 기존 요청 타입을 무수정 소비
- 기존 파일 수정 0건(체력/위험/폭탄/로프/전투/이동/방 전환/MAP 불변); Scene/prefab/physics asset/inputactions/Packages/MapDesign/Tilemap/카메라/애니메이션/오디오/UI prefab/Canvas/TMP/세이브/레거시 변경 0건
- ProjectSettings 추적 변경 2건(dev.yarnspinner json, ShaderGraphSettings.asset)은 과제 이전부터 존재한 사용자 수정으로 본 실행에서 건드리지 않음

## DEPENDENCY_LEDGER

- 사용(기존 승인): CharacterBombSpendRequest/CharacterRopeSpendRequest/CharacterBombPlacementRequest/CharacterExplosionRequest/CharacterRopePlacementRequest(Equipment), CharacterHealthState/CharacterDamageApplicationResult/CharacterDeathRequest/CharacterRunFailureRequest/CharacterSurvivalTargetKind(Survival), WorldTileCoord/WorldCoordinateUtility(Map 공용, 테스트), CharacterActionId(테스트 잠금 확인)
- 신규 공개 계약(후속 소비 예정): CharacterRunState/CharacterHudSnapshot(실 HUD 바인딩·리트라이 흐름 — CHAR06/후속 UI 단계), CharacterPresentationEventRequest+NormalizeBatch(실제 연출 재생기 — 통합 단계), CharacterRunInventoryPolicy(라이브 인벤토리 파이프라인 — CHAR06)
- 레거시 수치 선례 채택: 시작 폭탄 4/로프 4 (읽기 전용 참조, 코드 미변경)
- 미사용: Unity UI/Canvas/TMP, Audio, SceneManagement, PlayerPrefs, Tilemap, Animator, Physics2D(신규 범위), Stage/레거시, 에디터 API

## OUT_OF_SCOPE_FINDINGS

- 스키마 문서상 인벤토리 필드명은 소문자(bombCount/ropeCount)지만 C# 속성 관례(PascalCase: BombCount/RopeCount)로 구현 — 데이터 직렬화 계약이 아니라 순수 런타임 값 객체이므로 충돌 없음(직렬화 단계가 생기면 그 과제 소관)
- 폭발 이벤트의 ActorOrSourceId는 ExplosionId를 담는다(OwnerId 아님) — 연출 소비자가 소유자 정보가 필요해지면 이벤트 확장은 별도 과제 소관
- 레거시 RunState의 moneyWon/items/flags 등 확장 런 데이터는 어느 과제도 아직 소유하지 않음 — 필요 시 CHANGE CONTROL/후속 과제 소관
- Assets/_Game/Tests/PlayMode/Map asmdef의 stale `Game.Stage.Runtime` 참조 — MAP 하니스 소관, 계속 미수정 유지

## DONE CONDITIONS

- [x] CHAR05_03 PASS/hash verified.
- [x] Source registry marker/hash verified.
- [x] Default bomb and rope counts are centralized.
- [x] Bomb and rope spend requests decrease counts.
- [x] Spend cannot reduce counts below zero or mutate input state.
- [x] Run health snapshot reflects Survival health state.
- [x] Player run failure marks run failed with return token.
- [x] Non-player death does not fail player run.
- [x] HUD snapshot exposes health, bombs, ropes, status, and return token.
- [x] HUD snapshot is data only.
- [x] Damage, death, and run failure create presentation event requests.
- [x] Bomb, rope, and inventory events are presentation requests only.
- [x] Event ordering is deterministic and deduplicated.
- [x] No actual UI, scene, save, audio, animation, camera, prefab, GameObject, or presentation side effect exists.
- [x] Animator events and physics callbacks are not authority.
- [x] Forbidden basic attack/movement features remain absent.
- [x] ActionId locked set remains unchanged.
- [x] Character EditMode tests pass with at least 158 tests. (158/158)
- [x] Unity compile errors 0.
- [x] Scope validation completed.
- [x] CHAR05_05 remains locked.

## NEXT

Current Task after finalize: NONE
Next Task auto-opened: NO (`CHAR05_05_CHAR05_EQUIPMENT_SURVIVAL_EXIT_AUDIT`는 LOCKED 유지, 새 MCP_INBOX 패키지로만 개방)
