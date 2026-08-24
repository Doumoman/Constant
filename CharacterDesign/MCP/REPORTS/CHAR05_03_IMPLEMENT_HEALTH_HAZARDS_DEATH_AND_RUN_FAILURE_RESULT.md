# CHAR05_03_IMPLEMENT_HEALTH_HAZARDS_DEATH_AND_RUN_FAILURE_RESULT

## TASK

TASKS/CHAR05_03_IMPLEMENT_HEALTH_HAZARDS_DEATH_AND_RUN_FAILURE.md

## STATUS

STATUS: PASS

## SUMMARY

불변 체력 상태와 피해 적용 정책, 접촉/임팩트/폭발/위험 후보를 하나로 받는 통합 생존 피해 요청, 위험 후보(스파이크/압착/화염/일반/Void), 치명 피해 → 사망 요청, 플레이어 사망·Void 이탈 → 런 실패 요청(복귀 토큰은 데이터 전용)을 순수 값 객체 + 정적 정책으로 구현했다. HUD·씬 리로드·세이브·오디오·연출·GameObject 어떤 것도 건드리지 않는다. 신규 12개 테스트 포함 Game.Character.Tests.EditMode 146/146 PASS, 컴파일 에러 0건.

## READ

- CharacterDesign/MCP/00_MCP_ENTRYPOINT.md ~ 05_CHANGE_CONTROL_RULES.md (전역 규칙)
- CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md, MASTER_IMPLEMENTATION_TASK_LIST.md
- CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md
- CharacterDesign/MCP/TASKS + REPORTS: CHAR05_01, CHAR05_02(과제/결과), CHAR04_02·CHAR04_03 결과
- CharacterDesign/01_FIXED_SPEC/01_CHARACTER_GAMEPLAY_RULES.md, 02_CHARACTER_INPUT_RULES.md, 05_CHARACTER_COMBAT_RULES.md, 06_CHARACTER_MAP_INTEGRATION_RULES.md, 07_CHARACTER_TEST_RULES.md
- CharacterDesign/03_DATA_SCHEMA/CHARACTER_DAMAGE_SCHEMA.md (cause 잠금 9종·bypassInvulnerability 기본 false), CHARACTER_ACTION_SCHEMA.md
- CharacterDesign/04_TEST_FIXTURES/COMBAT_COURSE_SPEC.md
- Assets/_Game/Character/Runtime/ 현행 전체 — 특히 소비 대상 후보 표면: CharacterPlayerDamageCandidate{SourceEnemyId, ContactSide, Amount}, CharacterPlayerImpactDamageCandidate{SourceObjectId, Amount}, CharacterEnemyImpactDamageCandidate{SourceObjectId, TargetEnemyId, ImpactDirection, Amount}, CharacterPlayerExplosionDamageCandidate/CharacterEnemyExplosionDamageCandidate
- 레거시 읽기 전용 선례: Assets/_Legacy/_Game/Core/State/RunState.cs (health=4), Assets/_Legacy/StarNight/Scripts/Runtime/Player/PlayerRecovery.cs (MaxHealth 기본 4, amount≤0 무시·0 clamp 흐름), Assets/_Legacy/_Game/Core/Player/PlayerGridContract.cs (VoidRecoveryInvulnerabilitySeconds=0.8f)

Entry Gate 검증 (Phase A에서 수행):

- Current Task = TASKS/CHAR05_03_IMPLEMENT_HEALTH_HAZARDS_DEATH_AND_RUN_FAILURE.md 확인
- CHAR05_02 REPORT: STATUS: PASS + sha256 `940e5cf9909cc55a6562704c530ee7abba2d9638ac52627d9b0146922cb98fef` 일치 + required_text 3건 확인
- CHAR05_02 Task sha256 `0ba1e7c1...` 일치
- Source Registry marker `REGISTRY_STATE: FILLED_BY_CHAR00_01` + sha256 `be6cadc4...` 일치
- CHAR05_04 이후 전부 LOCKED 확인

## CHANGED

- 없음 (기존 파일 수정 0건 — 전투/폭탄/로프/이동 코드 및 기존 후보 타입 파일 전부 불변; asmdef 변경 없이 기존 어셈블리가 신규 Survival 폴더를 자동 포함함을 컴파일로 확인)

## CREATED

Runtime — `Assets/_Game/Character/Runtime/Survival/` (namespace `StarNight.Character.Survival`, 신규 폴더, 15파일):

1. CharacterSurvivalTargetKind.cs — enum {Player, Enemy}
2. CharacterDamageSourceKind.cs — enum, DAMAGE_SCHEMA cause 잠금 9종과 정확히 일치(확장 없음)
3. CharacterHazardKind.cs — enum {Spike, Crush, Fire, Generic, Void}
4. CharacterSurvivalSettings.cs — 중앙 설정: 최대 체력 4 / 피격 후 무적 0.8s (레거시 선례), 유효성 검증
5. CharacterHealthState.cs — 불변 체력 상태(actor/kind/current/max/무적 잔여); 생성자 검증·clamp; CreateFull; TickInvulnerability(음수 clamp)
6. CharacterSurvivalDamageRequest.cs — 통합 피해 요청 {SourceKind, SourceId, TargetId, TargetKind, Amount, Direction, BypassInvulnerability(기본 false 계약)}
7. CharacterSurvivalDamageAdapters.cs — 기존 후보 → 통합 요청 변환(Survival측; 기존 파일 무수정): FromContact/FromImpact×2/FromExplosion×2
8. CharacterHazardDamageCandidate.cs — 위험 피해 후보(kind, source id, target, amount, direction, HasCell+셀 좌표 선택 기록)
9. CharacterHazardPolicy.cs — 위험→피해 요청(cause 사상: Spike→Spike, Crush→Crush, Fire/Generic→Environment), Void 사망 요청(cause Fall), Void 런 실패(플레이어만)
10. CharacterDamageApplicationResult.cs — 적용 결과 {NewState, AppliedAmount, WasSuppressedByInvulnerability, HasDeathRequest, DeathRequest}
11. CharacterHealthDamagePolicy.cs — 피해 적용 정책(입력 불변·새 상태 반환): 0 이하/대상 불일치/소진 상태 무시, 무적 억제(명시 bypass만 관통), 0 clamp, 비치명 플레이어 피격에 무적 부여, 치명 시 사망 요청 동봉
12. CharacterDeathRequest.cs — {ActorId, TargetKind, Cause, SourceId}
13. CharacterRunFailureReason.cs — enum {PlayerDeath, VoidOrOutOfBounds}
14. CharacterRunFailureRequest.cs — {Reason, ActorId, ReturnDestinationToken(불투명 데이터), HasReturnDestination}
15. CharacterRunFailurePolicy.cs — 사망→런 실패(플레이어만; 적 사망은 절대 아님)

Tests — `Assets/_Game/Tests/EditMode/Character/Survival/` (신규 폴더, 4파일):

16. CharacterHealthAndDamageTests.cs (5 tests)
17. CharacterUnifiedDamageAndHazardTests.cs (3 tests)
18. CharacterDeathAndRunFailureTests.cs (3 tests)
19. CharacterSurvivalGuardTests.cs (1 test)

(+ Unity 자동 생성 .meta — 신규 폴더 Survival 2곳 포함)

## TEST

Unity Version: 6000.3.8f1
EditMode Assembly: Game.Character.Tests.EditMode
Result: 146/146 PASS (failed 0, skipped 0, 1.235s) — 이전 134 + 신규 12

요구 테스트명 12건 → 실제 테스트명 매핑 (전부 요구명 그대로 사용):

| 요구 동작/테스트명 | 실제 테스트 | 파일 |
|---|---|---|
| Health_DamageReducesHealthAndClampsAtZero | 동일명 (1→3, 과대 피해 0 clamp, 생성자 clamp) | CharacterHealthAndDamageTests.cs |
| Health_NonPositiveDamageCreatesNoChange | 동일명 (0·음수·대상 불일치) | CharacterHealthAndDamageTests.cs |
| Health_InvulnerabilitySuppressesDamageUnlessBypassed | 동일명 (억제/관통/틱 감소·음수 clamp/만료 후 재적용+무적 부여) | CharacterHealthAndDamageTests.cs |
| Health_LethalDamageCreatesDeathRequest | 동일명 (적·플레이어, 소진 후 재발행 없음) | CharacterHealthAndDamageTests.cs |
| Health_NonLethalDamageCreatesNoDeathOrRunFailure | 동일명 | CharacterHealthAndDamageTests.cs |
| Damage_ContactImpactExplosionAndHazardCanBecomeUnifiedRequests | 동일명 (4경로 전부 + 통합 적용 확인) | CharacterUnifiedDamageAndHazardTests.cs |
| Hazard_SpikeCrushFireCreateDamageCandidates | 동일명 (cause 사상 4종 + 셀 좌표 선택 기록) | CharacterUnifiedDamageAndHazardTests.cs |
| Hazard_VoidOrOutOfBoundsCreatesRunFailureRequest | 동일명 (피해 요청 아님·사망 cause Fall·플레이어만 런 실패) | CharacterUnifiedDamageAndHazardTests.cs |
| Death_EnemyDeathDoesNotCreatePlayerRunFailure | 동일명 (직접·낙사·치명 사슬 경유 전부) | CharacterDeathAndRunFailureTests.cs |
| RunFailure_PlayerDeathCreatesRunFailureRequest | 동일명 (치명 피해→사망→런 실패 전체 사슬 포함) | CharacterDeathAndRunFailureTests.cs |
| RunFailure_ReturnDestinationIsDataOnlyAndDoesNotReloadSceneOrSave | 동일명 (토큰 데이터 + 표면 부작용 명명 부재 리플렉션) | CharacterDeathAndRunFailureTests.cs |
| SurvivalRuntime_DoesNotUseAnimatorPhysicsSceneHudSaveOrForbiddenActions | 동일명 | CharacterSurvivalGuardTests.cs |

## UNITY

- refresh_unity(force + compile): 1차에서 CS8156 2건(속성 반환값을 명시적 `in` 인자로 전달 불가 — 테스트 코드) → 지역 변수 경유로 수정, 2차 컴파일 정상·`error CS` 필터 콘솔 에러 0건
- run_tests(EditMode, Game.Character.Tests.EditMode): 146/146 PASS (테스트 실행은 1차 전건 통과)

## HEALTH_STATE_AND_DAMAGE

- CharacterHealthState: {ActorId, TargetKind, CurrentHealth, MaxHealth, InvulnerabilityRemainingSeconds} 불변 값 객체 — 생성자에서 max≥1, current∈[0,max], 무적≥0으로 검증·clamp
- 적용 정책은 입력 상태를 변조하지 않고 결과(새 상태)만 반환 — readonly struct + 새 인스턴스 반환으로 구조적 보장, 테스트에서 원본 불변 확인
- 양수 피해만 적용: current−amount를 0에서 clamp; amount≤0·대상 ID/kind 불일치·이미 소진 상태 → 변화 없음(AppliedAmount 0)
- 무적: 잔여>0이면 억제(WasSuppressedByInvulnerability=true), 요청의 명시적 BypassInvulnerability=true만 관통(스키마 기본 false)
- 피격 후 무적: 비치명 플레이어 피격에만 0.8s 부여(레거시 VoidRecoveryInvulnerabilitySeconds 선례; 적 경직은 CHAR04 기절 계약 소관이라 미부여); TickInvulnerability로 시간 주입 감소(음수 delta clamp)
- 기준 수치: 최대 체력 4(레거시 RunState/PlayerRecovery 선례) — CharacterSurvivalSettings.Default 중앙화

## UNIFIED_DAMAGE_REQUESTS

- CharacterSurvivalDamageRequest {SourceKind, SourceId, TargetId, TargetKind, Amount, Direction, BypassInvulnerability} — HUD·점수·넉백·기절·제거·사망·연출을 직접 적용하지 않는 순수 요청
- 4경로 변환(Survival측 어댑터, 기존 후보 파일 무수정 — 과제의 "prefer Survival-side adapter" 선택지 채택으로 조건부 브리지 쓰기 불필요):
  - 접촉(CHAR04_02): CharacterPlayerDamageCandidate → EnemyContact (ContactSide는 방향 벡터가 아닌 기하 분류라 Direction=zero로 두고 문서화 — 넉백 방향 해석은 소비자 소관)
  - 임팩트(CHAR04_03): 플레이어/적 임팩트 후보 → ThrownObject (적 쪽은 ImpactDirection 유지)
  - 폭발(CHAR05_01): 플레이어(자해 포함)/적 폭발 후보 → Explosion (DirectionFromCenter 유지)
  - 위험(신규): CharacterHazardDamageCandidate → CharacterHazardPolicy 경유
- SourceKind enum은 DAMAGE_SCHEMA cause 잠금 9종과 정확히 일치하며 가드 테스트가 Enum.GetNames 동등성으로 고정

## HAZARD_CANDIDATES

- CharacterHazardDamageCandidate: kind(Spike/Crush/Fire/Generic), 원인 ID, 대상, 피해량, 방향, HasCell+WorldTileCoord(알려진 경우만 기록)
- cause 사상: Spike→Spike, Crush→Crush, Fire/Generic→Environment — 스키마 cause를 확장하지 않음(화염 전용 cause 추가는 CHANGE CONTROL 소관으로 회피)
- Void는 피해 요청을 만들지 않음(TryCreateDamageRequest=false) — 치명 경로 전용: CreateVoidDeathRequest(대상 무관, cause Fall) + TryCreateVoidRunFailure(플레이어만)
- 라이브 물리 질의·MAP/Tilemap 변조 없음 — 위험 감지 자체(위치×위험 셀 판정)는 라이브 통합(CHAR06) 소관이며 여기서는 후보/요청 계약만 소유

## DEATH_REQUEST

- 치명 피해(적용 후 체력 0 도달)에서 정확히 한 번 CharacterDeathRequest {ActorId, TargetKind, Cause=요청 SourceKind, SourceId} 동봉 — 이미 소진된 상태에는 재발행 없음
- 비치명 피해는 사망 요청을 만들지 않음
- 적/비플레이어 사망은 어떤 경로로도 플레이어 런 실패를 만들지 않음(직접 요청·낙사·치명 사슬 3경로 테스트)
- GameObject 파괴·애니메이션·연출 없음 — 요청 값 객체뿐

## RUN_FAILURE_AND_RETURN_REQUEST

- 플레이어 사망 → CharacterRunFailureRequest {Reason=PlayerDeath, ActorId, 토큰}; Void/월드 이탈 → {Reason=VoidOrOutOfBounds, ...} — 두 생성 경로 모두 플레이어 한정
- 복귀/재시도 목적지는 불투명 문자열 토큰 데이터일 뿐(HasReturnDestination으로 유무 판별) — 씬 리로드·세이브 변조·UI·플레이어 transform 이동 없음을 표면 리플렉션(LoadScene/Reload/SceneManager/Save/PlayerPrefs/Hud/Teleport/Transform 명명 부재)으로 보증
- 런 상태 HUD/연출 브리지는 CHAR05_04 소관으로 미구현

## AUTHORITY_AND_FORBIDDEN_FEATURE_GUARD

- Survival 전 타입(15개 이상): MonoBehaviour/Component 아님 → 물리 콜백 호출 불가, 추가로 OnCollision*/OnTrigger* 메서드 부재 직접 스캔
- 어셈블리 참조: AnimationModule·TilemapModule·UIModule·AudioModule·UnityEngine.UI 전부 부재 — Animator 이벤트/Tilemap/HUD/오디오가 권위가 될 수 없음 (Physics2DModule 참조는 CHAR01 승인 질의 어댑터 소관 — CHAR05_01/02와 동일한 확립 검증 레벨)
- 표면 타입 스캔: Animator/Tilemap/Rigidbody/Collider/RaycastHit/Scene/Canvas/Audio/GameObject 부재
- 금지 개념: 타입·멤버명에서 BasicAttack/Melee/Shoot/Dash/WallJump/DoubleJump 부재
- ActionId 잠금 5종 {Jump, Action, SafeDrop, Bomb, Rope} 그대로 + cause 잠금 9종 동등성 단언

## DEPENDENCY_DIRECTION

- StarNight.Character.Survival → StarNight.Character.Combat + StarNight.Character.Equipment (후보 소비, 어댑터 방향) + StarNight.Map.WorldGeneration.Domain (WorldTileCoord)
- 역방향 없음: Combat/Equipment/Traversal/Movement는 Survival을 모름(기존 파일 무수정이 구조적 증거); MAP 런타임은 Character를 모름
- asmdef 변경 0건 — 기존 Game.Character.Runtime이 Survival 폴더 자동 포함

## SCOPE_VALIDATION

- git status 확인: 신규 파일 전부 허용 경로 내 — Survival 런타임 15 + Survival 테스트 4 (+ .meta)
- 조건부 브리지 쓰기(Combat/Equipment) 미사용 — 어댑터를 Survival측에 두어 기존 후보 파일 무수정으로 소비(과제가 선호로 명시한 방식)
- 기존 파일 수정 0건(폭탄/로프/전투 동작 불변); Scene/prefab/physics asset/inputactions/Packages/MapDesign/MAP 런타임/Tilemap/카메라/애니메이션/오디오/세이브/레거시 변경 0건
- ProjectSettings 추적 변경 2건(dev.yarnspinner json, ShaderGraphSettings.asset)은 과제 이전부터 존재한 사용자 수정으로 본 실행에서 건드리지 않음

## DEPENDENCY_LEDGER

- 사용(기존 승인): CharacterPlayerDamageCandidate·CharacterPlayerImpactDamageCandidate·CharacterEnemyImpactDamageCandidate(Combat), CharacterPlayerExplosionDamageCandidate·CharacterEnemyExplosionDamageCandidate(Equipment), WorldTileCoord/WorldCoordinateUtility(Map 공용), CharacterActionId(테스트 잠금 확인)
- 신규 공개 계약(후속 과제 소비 예정): CharacterHealthState/CharacterHealthDamagePolicy(런 상태 집계 — CHAR05_04), CharacterDeathRequest(제거/연출 소비 — CHAR05_04+), CharacterRunFailureRequest+ReturnDestinationToken(런 상태 HUD·재시작 흐름 — CHAR05_04, 씬/세이브 적용은 그 밖), CharacterHazardDamageCandidate(라이브 위험 감지 통합 — CHAR06)
- 레거시 수치 선례 채택: 최대 체력 4, 피격 후 무적 0.8s (읽기 전용 참조, 코드 미변경)
- 미사용: Tilemap, Animator, Physics2D(Survival 범위), UI/Audio/SceneManagement, PlayerPrefs, Stage/레거시, 에디터 API

## OUT_OF_SCOPE_FINDINGS

- CHAR04 기절/제거 계약(CharacterStompEnemyResult 등)과 신규 사망 요청의 관계: 일반 적의 "기절 후 제거" 흐름은 CHAR04 계약이 소유하고, 체력형 사망 요청은 통합 피해 경로의 치명 결과다 — 두 경로의 소비 시 우선순위 정리는 라이브 통합(CHAR06) 소관
- 낙하 피해(cause Fall의 비치명 낙하 데미지, Spelunky식)는 어느 과제도 아직 소유하지 않음 — 필요 시 별도 과제/CHANGE CONTROL 소관(현재 Fall은 Void 치명 경로에만 사용)
- Assets/_Game/Tests/PlayMode/Map asmdef의 stale `Game.Stage.Runtime` 참조 — MAP 하니스 소관, 계속 미수정 유지

## DONE CONDITIONS

- [x] CHAR05_02 PASS/hash verified.
- [x] Source registry marker/hash verified.
- [x] Positive damage reduces health and clamps at zero.
- [x] Non-positive damage creates no health change.
- [x] Invulnerability suppresses damage unless bypassed.
- [x] Contact, impact, explosion, and hazard candidates can become unified survival damage requests.
- [x] Spike/crush/fire hazards create damage candidates.
- [x] Void/out-of-bounds hazard creates run failure request.
- [x] Lethal damage creates death request.
- [x] Non-lethal damage creates no death or run failure.
- [x] Enemy/non-player death does not create player run failure.
- [x] Player death creates run failure request.
- [x] Return destination is data only.
- [x] No scene reload, save mutation, HUD, audio, animation, or presentation side effect exists.
- [x] Animator events and physics callbacks are not authority.
- [x] Forbidden basic attack/movement features remain absent.
- [x] ActionId locked set remains unchanged.
- [x] Character EditMode tests pass with at least 146 tests. (146/146)
- [x] Unity compile errors 0.
- [x] Scope validation completed.
- [x] CHAR05_04 remains locked.

## NEXT

Current Task after finalize: NONE
Next Task auto-opened: NO (`CHAR05_04_IMPLEMENT_RUN_STATE_HUD_AND_PRESENTATION_BRIDGE`는 LOCKED 유지, 새 MCP_INBOX 패키지로만 개방)
