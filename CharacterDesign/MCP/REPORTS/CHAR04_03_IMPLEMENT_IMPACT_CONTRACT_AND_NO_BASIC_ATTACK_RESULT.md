# TASK RESULT

TASK: CHAR04_03_IMPLEMENT_IMPACT_CONTRACT_AND_NO_BASIC_ATTACK
STATUS: PASS

## SUMMARY

투척물·고체 월드 임팩트를 순수 요청/판정 계약으로 구현했다: 이동 중 투척물 + 적대 적 → 적 임팩트 피해 후보(HP·기절·제거·사망·점수 미적용), 고체 월드 → 오브젝트 정지 요청만(지형 불변), 소유자 유예 활성 시 소유자/자기 임팩트 억제, 정지·저속 소스 무이벤트, 결과는 오브젝트/적/플레이어 요청 3슬롯 분리. 일반 공격·금지 이동 부재를 액션 표면과 런타임 전수 스캔으로 재고정했다. EditMode 110/110 PASS(기존 100 + 신규 10).

## READ

- Entry Gate: CHAR04_02 REPORT sha `e6825958…` + required_text 3건, registry sha/marker, CHAR04_04 이후 LOCKED — 전부 일치(Phase A 6게이트)
- Mandatory Read Order 21개 항목(레거시 임팩트 선례 read-only)

## CHANGED

- 기존 파일 수정 0. 조건부 Interaction write 불필요 — CHAR04_01 투척 요청의 owner/grace 값은 `CharacterImpactSource` 스냅샷으로 소비 측이 전달(투척·유예 동작 재작성 없음). asmdef 무수정(폴더 자동 포함, 컴파일 증명)

## CREATED

Runtime (`Assets/_Game/Character/Runtime/Combat/`, 11개 — Combat 누적 12→22, +임팩트 계약 10 + 예약 슬롯 1):

- `CharacterImpactSourceKind.cs` — ThrownObject(폭발은 CHAR05 소관)
- `CharacterImpactTargetKind.cs` — Enemy/Player/SolidWorld
- `CharacterImpactSource.cs` — objectId/ownerId(+HasOwner)/kind/velocity/유예 잔여초, `IsOwnerGraceActive`
- `CharacterImpactTarget.cs` — kind/targetId/IsHostile(+SolidWorld 팩토리)
- `CharacterImpactSettings.cs` — MinimumImpactSpeed(>0, 기준선 1.5)/ThrownEnemyDamageAmount(>0, 기준선 1) 중앙 관리·검증
- `CharacterEnemyImpactDamageCandidate.cs` — source/target/방향(정규화)/양 — 요청 전용
- `CharacterPlayerImpactDamageCandidate.cs` — 예약 슬롯(현행 미발행 — 적 투척물의 플레이어 피해는 미래 계약, 문서화)
- `CharacterObjectStopRequest.cs` — 정지/안착 요청(지형 관련 멤버 없음)
- `CharacterImpactResult.cs` — 3슬롯 분리(object/enemy/player)
- `CharacterImpactPolicy.cs` — 규칙: 저속·정지 무이벤트 → 유예 중 소유자/자기 억제 → 고체=정지 요청만 → 적대 적=피해 후보 / 비적대=무이벤트 → Player 대상=예약(미발행). 순수·결정적, 밟기 흐름과 비병합

EditMode Tests (`Tests/EditMode/Character/Combat/`, 2개 — 누적 5): `CharacterImpactPolicyTests.cs`(7), `CharacterImpactGuardTests.cs`(3)

Unity 생성 `.meta`: 신규 .cs 13개 대응(허용 범위, 기록)

Report: 본 파일

## TEST

실행: Unity MCP `run_tests`(EditMode, assembly=Game.Character.Tests.EditMode 전체, job `0bf540fd…`)

| 항목 | 값 |
|---|---|
| 전체/성공/실패/스킵 | 110 / 110 / 0 / 0 (resultState=Passed, 3.58s — 최소 110 충족) |
| 기존 100개 | 전부 Passed |
| 신규 10개 | 전부 Passed |

요구 행위 ↔ 실제 테스트 매핑(10/10, 이름 동일): Impact_ThrownObjectEnemyTargetCreatesDamageCandidate(유예 중에도 적 대상 정상 임팩트 포함) / Impact_OwnerGraceSuppressesOwnerSelfImpact / Impact_OwnerGraceExpiredAllowsEligibleImpact / Impact_StationaryOrBelowThresholdSourceCreatesNoEvent(적·고체 대상 모두) / Impact_SolidWorldCreatesObjectStopRequestOnly(지형 멤버 부재 포함) / Impact_ResultSeparatesObjectEnemyAndPlayerRequests(3슬롯 구조 + 요청 전용 형태 + 예약 슬롯 미발행) / Impact_NonHostileTargetDoesNotCreateEnemyDamageCandidate(명시적 적대만 피해) / Impact_RuntimeDoesNotUseAnimatorEventsAsImpactAuthority(AnimationModule 0 + 표면 스캔 + 결정성) / NoBasicAttack_ActionSurfaceRemainsLocked(ActionId 5종 + 입력 스냅샷 표면) / NoBasicAttack_RuntimeDoesNotIntroduceForbiddenMovementOrAttackFeatures(전수 스캔 + Attack 타입명 0)

## UNITY

- Unity Version: 6000.3.8f1
- Asset Refresh: PASS, Compile Errors: 0(error CS 0건), Relevant New Warnings: 0
- EditMode: 110/110, PlayMode: NOT RUN(과제 지정), Scene/Prefab Changes: 0

## IMPACT_SOURCE_TARGET

소스 = objectId/owner(+유무)/종류/속도/유예 잔여초 스냅샷, 대상 = 종류/id/적대 여부 스냅샷. 최소 임팩트 속도는 설정 단일 소스와 비교(velocity.magnitude < 1.5 → 무이벤트). 순수 값 객체 — Unity 물리 콜백·Animator 비권한.

## THROWN_OBJECT_ENEMY_IMPACT

이동 중 투척물 + 적대 적 + (소유자 억제 비해당) → `CharacterEnemyImpactDamageCandidate`(source/target/정규화 방향/설정 피해량). HP·기절·제거·사망·점수·연출 미적용(멤버 부재 리플렉션 고정). 밟기 기절/제거 흐름(CHAR04_02)과 비병합 — 후보의 소비 규칙(기절 적 제거 등)은 소비 계층 소관.

## OWNER_GRACE_IMPACT_SUPPRESSION

CHAR04_01 유예 계약 존중: 소스 스냅샷의 잔여 유예 > 0 ∧ 대상 id == ownerId(비고체) → 무이벤트. 유예 만료 + 적격 대상 → 정상 판정. 유예 로직은 정책 한 곳에 중앙화(테스트 고정). 투척/유예 동작 재작성 없음(Interaction 무수정).

## SOLID_WORLD_IMPACT

이동 중 투척물 + 고체 월드 → `CharacterObjectStopRequest`만 발행(피해·연출·지형 변경 없음 — 정지 요청 타입에 Terrain/Tile 멤버 부재 고정). 지형 변경은 CHAR05_01 이연.

## NO_BASIC_ATTACK_GUARD

ActionId = 잠금 5종(EquivalentTo), 입력 스냅샷 표면에 Attack/Melee/Shoot 부재, 런타임 전수에 BasicAttack/Melee/Shoot/Dash/WallJump/DoubleJump 멤버 0 + Attack 포함 타입명 0. 레거시 공격류 명명은 read-only 참조로만 사용, 복사 없음.

## DEPENDENCY_DIRECTION

신규 코드는 UnityEngine(Vector2)+자기 어셈블리만 사용. AnimationModule 참조 0, 기존 의존성 가드 전부 유지 PASS, 전역 싱글톤 없음.

## SCOPE_VALIDATION

- `git status`: Character 트리 외 Assets 변경 0, MAP/Packages/MapDesign 0, ProjectSettings 기존 2건 외 0
- 신규 = Combat 런타임 11 + 테스트 2 + .meta. Interaction/asmdef/Scene/Prefab/물리 레이어 무변경
- 체력 차감/적 HP/사망/점수/내구도/지형 변경/폭탄/로프/HUD 미구현. CHAR04_04 미개방·미열람

## DEPENDENCY_LEDGER

```text
임팩트 입력 라이브 소스(물리 콜백→스냅샷 공급) : DEFERRED — 정책은 순수 함수, 공급 배선은 통합 소관
적 임팩트 후보 소비(HP/기절/제거 적용)          : DEFERRED — 적/월드 계층 + CHAR05 체력 계약
오브젝트 정지 요청 소비(물리 안착)              : DEFERRED — 오브젝트 계층 소관
플레이어 임팩트 슬롯(적 투척물)                 : RESERVED — 미래 계약(현행 미발행, 문서화)
지형 변경 요청(폭탄 등)                          : CHAR05_01
```

## OUT_OF_SCOPE_FINDINGS

- stale Map PlayMode asmdef 레거시 참조(기존 관찰 유지)

## DONE CONDITIONS

- [x] CHAR04_02 PASS 검증
- [x] registry marker/hash 검증
- [x] 이동 중 투척물의 적 임팩트 피해 후보 생성
- [x] 소유자 유예의 소유자/자기 임팩트 억제
- [x] 유예 만료 시 적격 임팩트 허용
- [x] 정지·저속 소스 무이벤트
- [x] 고체 월드 임팩트는 정지 요청만
- [x] 결과의 오브젝트/적/플레이어 요청 분리
- [x] 비적대 대상 무피해
- [x] 후보는 체력/HP/제거/사망/점수/연출 미적용
- [x] Animator 이벤트 비권한
- [x] 금지 공격/이동 기능 부재 유지
- [x] ActionId 잠금 세트 불변
- [x] EditMode 110개(≥110) 전부 PASS
- [x] compile error 0
- [x] 범위 검증 완료
- [x] CHAR04_04 LOCKED 유지

## NEXT

- Current Task after finalize: NONE
- Next Task auto-opened: NO (`CHAR04_04_CHAR04_INTERACTION_COMBAT_EXIT_AUDIT`는 LOCKED 유지, 새 MCP_INBOX 패키지로만 개방)
