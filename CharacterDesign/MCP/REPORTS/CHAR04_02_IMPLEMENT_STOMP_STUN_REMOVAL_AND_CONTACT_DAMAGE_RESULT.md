# TASK RESULT

TASK: CHAR04_02_IMPLEMENT_STOMP_STUN_REMOVAL_AND_CONTACT_DAMAGE
STATUS: PASS

## SUMMARY

플레이어-적 접촉 전투를 순수 판정 모델로 구현했다: 결정적 AABB 접촉 분류(하강 상단 접촉만 유효 밟기), 첫 밟기 기절 → 재밟기 제거 흐름(소형 적 전용), 적 결과와 플레이어 반동의 분리, 측면·하단 적대 접촉의 피해 후보(체력 차감 아님 — CHAR05 이연), 기절 소형 적의 CHAR04_01 휴대 후보 브리지. Animator/물리 콜백은 판정 권한이 아니다. EditMode 100/100 PASS(기존 88 + 신규 12).

## READ

- Entry Gate: CHAR04_01 REPORT sha `115949eb…` + required_text 3건, registry sha/marker, CHAR04_03 이후 LOCKED — 전부 일치(Phase A 6게이트)
- Mandatory Read Order 20개 항목(레거시 Player/Objects/Interaction 선례 read-only)

## CHANGED

- 기존 파일 수정 0. 조건부 Interaction write 불필요 — 기절 적 노출은 Combat 측 브리지가 기존 `CharacterCarryCandidate` 계약을 그대로 생성(carry/drop/throw 재작성 없음). asmdef 무수정(폴더 자동 포함, 컴파일 증명)

## CREATED

Runtime (`Assets/_Game/Character/Runtime/Combat/`, namespace `StarNight.Character.Combat`, 12개):

- `CharacterContactSide.cs` — None/Top/Side/Bottom
- `CharacterContactClassification.cs` — Side + IsValidStomp(상단 ∧ 하강)
- `CharacterEnemyContactClassifier.cs` — AABB 겹침 + 상대 중심 오프셋 결정 분류(겹침 얕은 축 우선), 분리=None. Animator/물리 콜백 비의존
- `CharacterEnemyContactTarget.cs` — EnemyId/IsSmallEnemy/IsHostile/IsStunned 판정 스냅샷
- `CharacterContactCombatSettings.cs` — StompReboundVelocity(>0, 기준선 6)/StunDurationSeconds(≥0, 기준선 5)/ContactDamageAmount(>0, 기준선 1) 중앙 관리·검증
- `CharacterStompOutcome.cs` — None/Stunned/Removed
- `CharacterStompEnemyResult.cs` — 적 측 결과(플레이어 속도 필드 없음)
- `CharacterStompReboundRequest.cs` — 플레이어 반동 요청(적 상태 필드 없음)
- `CharacterPlayerDamageCandidate.cs` — 피해 후보 요청(체력 차감 아님)
- `CharacterContactCombatResult.cs` — 적 결과/반동/피해 후보 분리 보관
- `CharacterContactCombatPolicy.cs` — 규칙: 유효 밟기+소형 일반→기절, +기절→제거, +비소형→반동만(기절/제거 흐름 없음, 문서화); 측면/하단+적대→피해 후보; 비적대(기절 휴대물 등)→비피해(문서화); 상승/정지 상단·분리→중립
- `CharacterStunnedEnemyCarryBridge.cs` — 기절 소형 적 → `CharacterCarryCandidate`(Kind=StunnedSmallEnemy) 생성. 비기절·비소형 거부

EditMode Tests (`Tests/EditMode/Character/Combat/`, 3개): `CharacterContactClassifierTests.cs`(3), `CharacterStompPolicyTests.cs`(4), `CharacterContactDamageAndGuardTests.cs`(5)

Unity 생성 `.meta`: Combat 폴더 2 + .cs 15 = 17개(허용 범위, 기록)

Report: 본 파일

## TEST

실행: Unity MCP `run_tests`(EditMode, assembly=Game.Character.Tests.EditMode 전체, job `56ec25cd…`)

| 항목 | 값 |
|---|---|
| 전체/성공/실패/스킵 | 100 / 100 / 0 / 0 (resultState=Passed, 3.32s — 최소 100 충족) |
| 기존 88개 | 전부 Passed |
| 신규 12개 | 전부 Passed |

요구 행위 ↔ 실제 테스트 매핑(12/12, 이름 동일): ContactClassifier_DescendingTopContactIsValidStomp / ContactClassifier_RisingOrStationaryTopContactIsNotStomp(분리=None 포함) / ContactClassifier_SideAndBottomContactBecomePlayerDamageCandidate / Stomp_FirstStompOnNormalSmallEnemyProducesStunAndRebound / Stomp_SecondStompOnStunnedSmallEnemyProducesRemoval / Stomp_SeparatesPlayerReboundFromEnemyResult(타입 형태 분리 + 비소형 반동만 검증 포함) / Stomp_ValidTopContactDoesNotCreatePlayerDamageCandidate(상단 비밟기 중립 포함) / ContactDamage_SideContactCreatesDamageRequestWithoutApplyingHealth(비적대 비피해 문서화 검증 포함) / ContactDamage_BottomContactCreatesDamageRequestWithoutApplyingHealth / StunnedSmallEnemy_CanBeExposedAsCarryCandidate(실제 슬롯 픽업 호환까지) / CombatRuntime_DoesNotUseAnimatorEventsAsDamageAuthority(AnimationModule 참조 0 + 표면 스캔 + 결정성) / CombatRuntime_DoesNotIntroduceBasicAttackDashWallJumpDoubleJumpOrShoot

## UNITY

- Unity Version: 6000.3.8f1
- Asset Refresh: PASS, Compile Errors: 0(error CS 0건), Relevant New Warnings: 0
- EditMode: 100/100, PlayMode: NOT RUN(과제 지정), Scene/Prefab Changes: 0

## CONTACT_CLASSIFICATION

AABB 겹침(겹침 얕은 축이 접촉 축) + 상대 중심 부호로 Top/Side/Bottom 결정, 겹침 없으면 None. 유효 밟기 = Top ∧ vy<0 (상승·정지 제외). 순수 함수 — 물리 콜백은 이후 입력으로만 소비 가능.

## STOMP_AND_REBOUND

유효 밟기 시 적 결과와 반동 요청을 별도 값 객체로 반환. 반동 속도는 설정 단일 소스(검증됨). 적 결과에 플레이어 필드 없음/반동에 적 필드 없음(리플렉션 고정) — 상호 직접 변조 불가.

## ENEMY_STUN_REMOVAL_FLOW

일반 소형 적: 첫 유효 밟기 → Stunned(+StunDurationSeconds), 기절 소형 적: 재밟기 → Removed. 비소형·비밟기 접촉은 이 흐름 미발동(비소형 밟기는 반동만 — 문서화). 투척물 임팩트 제거는 CHAR04_03 소관으로 미구현.

## PLAYER_CONTACT_DAMAGE

측면/하단 + 적대 → 피해 후보(EnemyId, ContactSide, Amount — DAMAGE 스키마의 EnemyContact 계열, 요청 값 객체). 유효 밟기 상단 접촉은 피해 후보 없음. 기절 비적대 대상 접촉은 비피해(문서화+테스트). 체력/생존 적용은 CHAR05 이연.

## STUNNED_ENEMY_CARRY_BRIDGE

`CharacterStunnedEnemyCarryBridge`가 기절 소형 적을 기존 휴대 계약(Kind=StunnedSmallEnemy, 1×1, carryable)으로 노출 — 실제 `CharacterCarryInteraction.TryPickUp` 호환까지 테스트. 비기절/비소형 거부. CHAR04_01 동작 재작성 없음(Interaction 파일 무수정).

## FORBIDDEN_FEATURE_GUARD

Attack/BasicAttack/Melee/Shoot/Dash/WallJump/DoubleJump — 런타임 전수 0. ActionId 잠금 5종 유지.

## DEPENDENCY_DIRECTION

신규 코드는 UnityEngine(Vector2/Mathf)+자기 어셈블리(Interaction 브리지 포함)만 사용. AnimationModule 참조 0, 기존 의존성 가드 전부 유지 PASS, 전역 싱글톤 없음.

## SCOPE_VALIDATION

- `git status`: Character 트리 외 Assets 변경 0, MAP/Packages/MapDesign 0, ProjectSettings 기존 2건 외 0
- 신규 = Combat 런타임 12 + 테스트 3 + .meta. Interaction/asmdef/Scene/Prefab/물리 레이어 무변경
- 투척 임팩트/폭탄/로프/체력 차감/사망/HUD 미구현. CHAR04_03 미개방·미열람

## DEPENDENCY_LEDGER

```text
접촉 입력 라이브 소스(물리 콜백→분류기 공급)   : DEFERRED — 분류기는 순수 함수, 공급 배선은 통합 소관
적 상태 실체(기절 타이머·제거 처리 소비)        : DEFERRED — 적/월드 계층이 결과 값 객체를 소비
플레이어 반동 적용(모터 소비)                   : DEFERRED — 통합 소관
피해 후보 → 체력/무적/사망 적용                 : CHAR05_03
투척물/환경 임팩트 계약                          : CHAR04_03
```

## OUT_OF_SCOPE_FINDINGS

- stale Map PlayMode asmdef 레거시 참조(기존 관찰 유지)

## DONE CONDITIONS

- [x] CHAR04_01 PASS 검증
- [x] registry marker/hash 검증
- [x] 하강 상단 접촉 = 유효 밟기
- [x] 상승/정지 상단 접촉 = 밟기 아님
- [x] 측면·하단 접촉 = 플레이어 피해 후보
- [x] 소형 일반 적 첫 밟기 = 기절
- [x] 기절 소형 적 재밟기 = 제거
- [x] 플레이어 반동과 적 결과 분리
- [x] 유효 밟기는 피해 후보 없음
- [x] 피해 후보는 체력 차감 아님
- [x] 기절 소형 적 휴대 후보 표현 가능
- [x] Animator 이벤트 비권한
- [x] 금지 기능 부재 유지
- [x] EditMode 100개(≥100) 전부 PASS
- [x] compile error 0
- [x] 범위 검증 완료
- [x] CHAR04_03 LOCKED 유지

## NEXT

- Current Task after finalize: NONE
- Next Task auto-opened: NO (`CHAR04_03_IMPLEMENT_IMPACT_CONTRACT_AND_NO_BASIC_ATTACK`는 LOCKED 유지, 새 MCP_INBOX 패키지로만 개방)
