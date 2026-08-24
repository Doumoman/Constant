# CHAR05_01_IMPLEMENT_BOMB_AND_TERRAIN_MUTATION_REQUEST_RESULT

## TASK

TASKS/CHAR05_01_IMPLEMENT_BOMB_AND_TERRAIN_MUTATION_REQUEST.md

## STATUS

STATUS: PASS

## SUMMARY

폭탄 설치/소모 요청, 퓨즈 모델, 폭발 요청, 파괴 가능 셀 한정 지형 변경 요청, 적/플레이어 폭발 피해 후보를 순수 값 객체 + 정적/결정적 정책으로 구현했다. 모든 산출물은 "요청"이며 인벤토리·체력·MAP/Tilemap·연출 어떤 것도 직접 변조하지 않는다. 신규 12개 테스트를 포함해 Game.Character.Tests.EditMode 122/122 PASS, 컴파일 에러 0건.

## READ

- CharacterDesign/MCP/TASKS/CHAR05_01_IMPLEMENT_BOMB_AND_TERRAIN_MUTATION_REQUEST.md (전문)
- CharacterDesign/MCP/00_MCP_ENTRYPOINT.md, 01_CHARACTER_LOCKED_RULES.md, 02_MCP_WORK_RULES.md
- CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md
- CharacterDesign/01_FIXED_SPEC/01_CHARACTER_GAMEPLAY_RULES.md, 02_CHARACTER_INPUT_RULES.md, 05_CHARACTER_COMBAT_RULES.md, 06_CHARACTER_MAP_INTEGRATION_RULES.md, 07_CHARACTER_TEST_RULES.md
- CharacterDesign/03_DATA_SCHEMA/CHARACTER_INVENTORY_SCHEMA.md, CHARACTER_DAMAGE_SCHEMA.md, CHARACTER_ACTION_SCHEMA.md
- CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md
- Assets/_Game/Character/Runtime/MapIntegration/ (CharacterMapCoordinateBridge, CharacterMapCellState, ICharacterMapWorldQuery)
- Assets/_Game/Character/Runtime/Input/CharacterActionId.cs
- Assets/_Game/Tests/EditMode/Character/Combat/CharacterImpactGuardTests.cs (가드 관용구 참조)

게이트 검증:

- CHAR04_04 REPORT: STATUS: PASS + CHAR04_EXIT_DECISION: APPROVED + sha256 일치 (Phase A에서 확인)
- Source Registry marker: `REGISTRY_STATE: FILLED_BY_CHAR00_01` 존재 확인
- Source Registry SHA-256: `be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7` 일치

## CHANGED

- 없음 (기존 파일 수정 0건 — asmdef 변경 없이 기존 Game.Character.Runtime / Game.Character.Tests.EditMode가 신규 폴더를 자동 포함함을 컴파일로 확인)

## CREATED

Runtime — `Assets/_Game/Character/Runtime/Equipment/` (namespace `StarNight.Character.Equipment`):

1. CharacterBombSettings.cs — fuse 2.5s / radius 1.5셀 / damage 2 기본값, 유효성 검증
2. CharacterBombPlacementInput.cs — 설치 판정 입력 스냅샷(값 객체)
3. CharacterBombPlacementRequest.cs — 설치 요청(값 객체)
4. CharacterBombSpendRequest.cs — 폭탄 1개 소모 요청(인벤토리 변조 없음)
5. CharacterBombPlacementPolicy.cs — 보유>0 ∧ 유효 셀 ∧ 설치 가능 셀 → 설치+소모 요청 동시 발행
6. CharacterExplosionRequest.cs — 폭발 요청(중심 셀, 반경, 피해량, 소유자)
7. CharacterBombFuse.cs — 퓨즈 타이머(주입 tick, 음수 clamp, 정확히 1회 폭발 요청, latch)
8. CharacterTerrainMutationIntent.cs — enum {DestroyBreakable}
9. CharacterTerrainMutationRequest.cs — 지형 변경 요청(셀 좌표 + intent + 출처 폭발 ID만 기록)
10. CharacterExplosionTerrainPolicy.cs — 결정적 영향 셀 열거(y승순→x승순, dx²+dy²≤r², 경계 밖 제외, 중복 없음) + 파괴 가능 셀만 변경 요청 생성
11. CharacterExplosionTargetSnapshot.cs — 피해 판정용 대상 스냅샷(적/플레이어)
12. CharacterEnemyExplosionDamageCandidate.cs — 적 피해 후보(값 객체)
13. CharacterPlayerExplosionDamageCandidate.cs — 플레이어 피해 후보(자기 폭탄 포함, 값 객체)
14. CharacterExplosionDamagePolicy.cs — 반경 내 대상 → 피해 후보(방향 포함, zero-distance는 Vector2.up)

Tests — `Assets/_Game/Tests/EditMode/Character/Equipment/` (namespace `StarNight.Character.Tests.Equipment`):

15. CharacterBombPlacementAndFuseTests.cs (5 tests)
16. CharacterExplosionPolicyTests.cs (5 tests, 테스트 전용 FakeMapWorldQuery 포함)
17. CharacterBombGuardTests.cs (2 tests)

(+ Unity 자동 생성 .meta 17건)

## TEST

Unity Version: 6000.3.8f1
EditMode Assembly: Game.Character.Tests.EditMode
Result: 122/122 PASS (failed 0, skipped 0, 0.363s) — 이전 110 + 신규 12

요구 테스트명 12건 → 실제 테스트명 매핑 (전부 요구명 그대로 사용):

| 요구 동작/테스트명 | 실제 테스트 | 파일 |
|---|---|---|
| BombPlacement_AvailableBombCreatesPlacementAndSpendRequest | 동일명 | CharacterBombPlacementAndFuseTests.cs |
| BombPlacement_NoAvailableBombCreatesNoPlacement | 동일명 (0개·음수 모두) | CharacterBombPlacementAndFuseTests.cs |
| BombPlacement_BlockedOrOutOfBoundsCellRefusesPlacement | 동일명 | CharacterBombPlacementAndFuseTests.cs |
| BombFuse_PositiveRemainingTimeCreatesNoExplosion | 동일명 (음수 delta clamp 포함) | CharacterBombPlacementAndFuseTests.cs |
| BombFuse_ReachesZeroCreatesSingleExplosionRequest | 동일명 (재발행 없음 latch 포함) | CharacterBombPlacementAndFuseTests.cs |
| Explosion_TerrainMutationRequestIncludesOnlyDestructibleCells | 동일명 | CharacterExplosionPolicyTests.cs |
| Explosion_TerrainMutationRequestIsDeterministicAndDeduplicated | 동일명 (3×3=9셀, 재호출 순서 동일) | CharacterExplosionPolicyTests.cs |
| Explosion_IndestructibleEmptyAndOutOfBoundsCellsAreSkipped | 동일명 (원점 모서리 4셀) | CharacterExplosionPolicyTests.cs |
| Explosion_EnemyAndPlayerTargetsWithinRadiusCreateDamageCandidates | 동일명 (자기 폭탄 자해 후보 포함) | CharacterExplosionPolicyTests.cs |
| Explosion_TargetsOutsideRadiusCreateNoDamageCandidate | 동일명 | CharacterExplosionPolicyTests.cs |
| Explosion_DamageCandidatesAndTerrainRequestsDoNotApplySideEffects | 동일명 | CharacterBombGuardTests.cs |
| BombRuntime_DoesNotUseAnimatorPhysicsTilemapOrForbiddenActions | 동일명 | CharacterBombGuardTests.cs |

## UNITY

- refresh_unity(force + compile) 2회: 컴파일 정상, `error CS` 필터 콘솔 에러 0건
- run_tests(EditMode, Game.Character.Tests.EditMode): 1차 121/122 (가드 과잉 단언 1건, 아래 AUTHORITY 절 참조) → 수정 후 2차 122/122 PASS

## BOMB_PLACEMENT

- 판정식: `AvailableBombCount > 0 && HasValidTargetCell && IsTargetCellPlaceable` 전부 참일 때만 설치 요청 생성
- 설치 요청과 소모 요청(Amount=1)은 항상 쌍으로 발행 — 소모는 요청일 뿐 인벤토리 수량을 어디서도 변조하지 않음(적용은 후속 단계 소관, CHARACTER_INVENTORY_SCHEMA 계약 유지)
- 보유 0/음수, 막힌 셀(IsTargetCellPlaceable=false), 월드 밖(HasValidTargetCell=false) → 설치·소모 요청 모두 없음
- 입력은 Bomb=Z 논리 행동의 하류 스냅샷이며 ActionId 잠금 5종의 기존 `Bomb` 슬롯을 사용(신규 ActionId 없음)

## BOMB_FUSE_AND_EXPLOSION

- CharacterBombFuse: 생성 시 settings.FuseSeconds(기본 2.5s)로 시작, 주입된 deltaSeconds로만 진행(시간 직접 조회 없음), 음수 delta는 0 clamp
- remaining > 0 동안 폭발 요청 없음; remaining ≤ 0 도달 프레임에 정확히 1회 CharacterExplosionRequest 발행, HasExploded latch로 재발행 차단
- CharacterExplosionRequest는 중심 셀(WorldTileCoord), 반경 1.5셀, 피해량 2, 소유자/폭발 ID를 담는 순수 값 객체

## TERRAIN_MUTATION_REQUEST

- 영향 셀 열거: 중심 셀 기준 dx²+dy² ≤ r² (r=1.5 → 3×3 = 9셀, 대각 포함 — 레거시 3×3 선례와 일치), y 승순→x 승순 고정 순서로 결정적, 구조상 중복 없음
- 경계 밖 셀은 WorldCoordinateUtility.TryCreateWorldTile 실패로 열거에서 제외 (원점 모서리 중심 시 4셀)
- 변경 요청 생성: ICharacterMapWorldQuery.TryGetCellState로 조회해 `IsBreakable` 셀만 CharacterTerrainMutationRequest(셀 좌표 + DestroyBreakable intent + 출처 폭발 ID) 생성
- 비파괴 고체·빈 셀·데이터 없는 셀은 요청 미생성; 요청 생성 후에도 맵 상태 불변을 테스트로 확인 — MAP/Tilemap/타일 에셋 실제 변조는 이 과제에 존재하지 않음
- MAP 접근은 CHAR03에서 승인된 공용 계약(WorldTileCoord, WorldCoordinateUtility, ICharacterMapWorldQuery 브리지)만 사용, 별도 DTO 브리지 불필요

## EXPLOSION_DAMAGE_CANDIDATES

- 중심 = CharacterMapCoordinateBridge.GetCellCenter(중심 셀), 반경 = RadiusCells × WorldUnitsPerCell(1셀=1u)
- 거리 ≤ 반경인 대상만 후보: 적 → CharacterEnemyExplosionDamageCandidate, 플레이어 → CharacterPlayerExplosionDamageCandidate (자기 폭탄도 반경 안이면 자해 후보 — 잠금 전투 규칙의 공용 폭발 계약)
- 후보는 대상 ID + 출처 폭발 ID + 피해량 + 중심→대상 정규화 방향만 기록(zero-distance는 Vector2.up 대체), 반경 밖 대상은 후보 없음
- HP 차감·기절·제거·사망·점수·넉백 적용·연출 어떤 것도 수행하지 않음 — 전부 CHAR05_03 이후 소비자 소관

## AUTHORITY_AND_FORBIDDEN_FEATURE_GUARD

- Equipment 전 타입: MonoBehaviour/Component 아님(리플렉션 검증) → Unity 물리 콜백이 애초에 불가능, 추가로 OnCollision*/OnTrigger* 메서드 부재를 직접 스캔
- 어셈블리 참조: UnityEngine.AnimationModule·UnityEngine.TilemapModule 부재 확인 — Animator 이벤트/Tilemap 직접 접근이 권위가 될 수 없음
- 투명성 기록: 1차 실행에서 가드가 어셈블리 차원 `UnityEngine.Physics2DModule` 부재까지 단언해 1건 실패했다. 이 참조는 CHAR01에서 승인된 충돌 "질의" 어댑터 `UnityPhysics2DCharacterCollisionWorld`(Movement) 소관으로 정당하다. 과제 요구는 "no Unity physics callback authority"이므로 가드를 Equipment 타입 범위의 콜백 부재 + 표면 타입(Animator/Tilemap/Rigidbody/Collider/RaycastHit) 부재 검증으로 교정했다. Movement 어댑터 코드는 건드리지 않았다.
- 표면 명명 가드: Health/Hp/Death/Kill/Score/Knockback/Inventory/Apply 명명 부재, BasicAttack/Melee/Shoot/Dash/WallJump/DoubleJump 부재
- 값 객체 불변: 공개 setter 0건, enum 제외 공개 가변 인스턴스 필드 0건
- ActionId 잠금 5종 {Jump, Action, SafeDrop, Bomb, Rope} 그대로 (Enum.GetNames 동등성 단언)

## DEPENDENCY_DIRECTION

- StarNight.Character.Equipment → StarNight.Character.MapIntegration (좌표 브리지·셀 상태 질의) → StarNight.Map.WorldGeneration.Domain (WorldTileCoord, 공용 계약)
- 역방향 없음: MAP 런타임은 Character를 모름; Equipment는 Input/State/Movement/Combat 어느 것도 참조하지 않음(테스트만 Input의 ActionId 잠금 확인에 참조)
- asmdef 변경 0건 — 기존 Game.Character.Runtime(references: Game.Map.Runtime)이 Equipment 폴더를 자동 포함

## SCOPE_VALIDATION

- git status 확인: 신규 파일은 전부 `Assets/_Game/Character/Runtime/Equipment/**`(14 .cs + meta)와 `Assets/_Game/Tests/EditMode/Character/Equipment/**`(3 .cs + meta) — 허용 쓰기 경로 내
- 조건부 Combat 경로는 사용하지 않음(피해 후보는 폭발 전용이라 Equipment에 배치)
- 기존 파일 수정 0건; Scene/prefab/physics asset/inputactions/Packages/MapDesign/MAP 런타임/Tilemap/카메라/애니메이션/레거시 변경 0건
- ProjectSettings 추적 변경 2건(`ProjectSettings/Packages/dev.yarnspinner/Assembly-CSharp-generated.ysls.json`, `ProjectSettings/ShaderGraphSettings.asset`)은 이 과제 이전부터 존재한 사용자 수정으로 본 실행에서 건드리지 않음

## DEPENDENCY_LEDGER

- 사용(기존 승인): WorldTileCoord, WorldCoordinateUtility(경계 판정, 브리지 경유), CharacterMapCoordinateBridge.GetCellCenter/WorldUnitsPerCell, CharacterMapCellState.IsBreakable, ICharacterMapWorldQuery.TryGetCellState, CharacterActionId(테스트 잠금 확인)
- 신규 공개 계약(후속 과제 소비 예정): CharacterBombPlacementRequest/CharacterBombSpendRequest(인벤토리 적용 — CHAR05_03+), CharacterExplosionRequest, CharacterTerrainMutationRequest(MAP 변조 적용 — 후속 통합 단계), CharacterEnemyExplosionDamageCandidate/CharacterPlayerExplosionDamageCandidate(체력/생존 적용 — CHAR05_03)
- 미사용: Tilemap, Animator, Physics2D(Equipment 범위), Stage/레거시, 에디터 API

## OUT_OF_SCOPE_FINDINGS

- 과제 Read Order가 `01_FIXED_SPEC/06_CHARACTER_EQUIPMENT_SURVIVAL_RULES.md`를 지시하나 해당 파일은 존재하지 않음 — 실제 06은 `06_CHARACTER_MAP_INTEGRATION_RULES.md`. 장비/생존 규칙은 01_CHARACTER_GAMEPLAY_RULES.md와 스키마 문서로 커버되어 진행에 지장 없음(문서 오타, 비차단)
- Assets/_Game/Tests/PlayMode/Map asmdef의 stale `Game.Stage.Runtime` 참조 — MAP 하니스 소관, 계속 미수정 유지
- MCP 브리지 콘솔의 "Client handler error: Cannot access a disposed object" 노이즈 — 코드 에러 아님, 기록만

## DONE CONDITIONS

- [x] CHAR04 EXIT approved and CHAR04_04 PASS/hash verified.
- [x] Source registry marker/hash verified.
- [x] Available bomb and valid target produce placement request.
- [x] No bomb count or invalid target refuses placement.
- [x] Placement emits spend request but does not mutate inventory.
- [x] Positive fuse produces no explosion.
- [x] Fuse zero or below produces exactly one explosion request.
- [x] Explosion affected-cell enumeration is deterministic and deduplicated.
- [x] Only destructible cells produce terrain mutation requests.
- [x] Terrain mutation request does not mutate MAP, Tilemap, or tile assets.
- [x] Enemy/player targets inside radius produce damage candidates.
- [x] Targets outside radius produce no damage candidate.
- [x] Damage candidates do not apply health, life, HP, stun, removal, death, score, knockback, or presentation.
- [x] Animator events and physics callbacks are not authority.
- [x] Forbidden basic attack/movement features remain absent.
- [x] ActionId locked set remains unchanged.
- [x] Character EditMode tests pass with at least 122 tests. (122/122)
- [x] Unity compile errors 0.
- [x] Scope validation completed.
- [x] CHAR05_02 remains locked.

## NEXT

Current Task after finalize: NONE
Next Task auto-opened: NO (`CHAR05_02_IMPLEMENT_ROPE_CLIMBING_AND_TRAVERSAL_SUPPORT`는 LOCKED 유지, 새 MCP_INBOX 패키지로만 개방)
