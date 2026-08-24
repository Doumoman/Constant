# TASK RESULT

TASK: CHAR04_01_IMPLEMENT_CARRY_DROP_AND_DIRECTIONAL_THROW
STATUS: PASS

## SUMMARY

첫 상호작용 코어를 순수 요청/판정 모델로 구현했다: 결정적 우선순위의 휴대 후보 질의(1×1 이하·휴대 가능·도달 가능만 적격, 기절 소형 적 포함), 단일 휴대 슬롯(수락된 drop/throw만 해제), 아래+행동 안전 내려놓기(막힌 목적지 겹침 없이 거부), 위/좌/우+행동 방향 투척(Up이 수평보다 우선), 중앙 관리 소유자 충돌 유예가 모든 요청에 포함. 임팩트 피해·적 처리·물리 레이어 변경은 범위 밖 유지. EditMode 88/88 PASS(기존 76 + 신규 12).

## READ

- Entry Gate: CHAR03_03 REPORT sha `28e83c35…` + APPROVED/ELIGIBLE/NONE 문구, registry sha/marker, CHAR04_02 이후 LOCKED — 전부 일치(Phase A 6게이트)
- Mandatory Read Order 20개 항목(레거시 CarrySystem/CarryableObject2D·구세대 Carry 선례는 read-only 참조)

## CHANGED

- 기존 파일 수정 0. asmdef 무수정 — 신규 파일은 기존 `Game.Character.Runtime`/`Game.Character.Tests.EditMode` 폴더 포함 규칙에 자동 포함됨(컴파일로 증명, BLOCK 불필요)

## CREATED

Runtime (`Assets/_Game/Character/Runtime/Interaction/`, namespace `StarNight.Character.Interaction`, 10개):

- `CharacterCarryCandidateKind.cs` — OrdinaryCarryable / StunnedSmallEnemy(기절 소형 적 계약 형태)
- `CharacterCarryCandidate.cs` — 안정 id, 셀 단위 크기, 종류, 휴대·도달 가능, 명시적 priority. `IsEligibleForCarry` = 휴대 가능 ∧ 1×1 이하(ε 포함)
- `CharacterCarryCandidateQuery.cs` — 정확히 하나 선택. 우선순위(보고 요구사항): ① reachable ∧ eligible 필터 → ② Priority 오름차순 → ③ 플레이어 제곱거리 오름차순 → ④ Id 오름차순 타이브레이크
- `CharacterCarryInteractionSettings.cs` — SafeDropOffset / ThrowSpeed(>0, 기준선 7) / OwnerCollisionGraceSeconds(≥0, 기준선 0.25 — 중앙 단일 소스, 검증)
- `ICharacterPlacementSpaceQuery.cs` — 내려놓기 목적지 점유 질의(read-only, 라이브 연결은 통합 소관)
- `CharacterThrowDirection.cs` — Up/Left/Right(아래+행동은 안전 내려놓기라 Down 없음)
- `CharacterThrowDirectionResolver.cs` — Up이 수평보다 우선(결정적), 방향 없으면 투척 의도 아님
- `CharacterCarryPlacementRequest.cs` / `CharacterCarryThrowRequest.cs` — heldObjectId/ownerId/(위치|방향·벡터·속도)/grace 값 객체
- `CharacterCarryInteraction.cs` — 단일 슬롯: TryPickUp(빈 슬롯+적격만), TryCreateSafeDrop(공간 확인→요청+해제 / 거부 시 유지), TryCreateThrow(휴대 중만→요청+해제). Carryable 내부 직접 수정 없음

EditMode Tests (`Tests/EditMode/Character/Interaction/`, 3개): `CharacterCarryCandidateQueryTests.cs`(2), `CharacterCarryInteractionTests.cs`(8), `CharacterInteractionBoundaryTests.cs`(2)

Unity 생성 `.meta`: Interaction 폴더 2 + .cs 13 = 15개(허용 범위, 기록)

Report: 본 파일

## TEST

실행: Unity MCP `run_tests`(EditMode, assembly=Game.Character.Tests.EditMode 전체, job `de2ca8a6…`)

| 항목 | 값 |
|---|---|
| 전체/성공/실패/스킵 | 88 / 88 / 0 / 0 (resultState=Passed, 3.04s — 최소 88 충족) |
| 기존 76개 | 전부 Passed |
| 신규 12개 | 전부 Passed |

요구 행위 ↔ 실제 테스트 매핑(12/12, 이름 동일): CarryCandidateQuery_SelectsSingleCandidateByDeterministicPriority / CarryCandidateQuery_RejectsOversizedOrNonCarryableCandidates / CarrySlot_PickupFillsSingleSlotAndRejectsSecondPickup / CarryDrop_DownActionCreatesSafeDropPlacementRequest(CHAR01 SafeDrop intent 연동 검증 포함) / CarryDrop_BlockedDestinationRejectsAndKeepsHeldObject / CarryThrow_RightActionCreatesRightThrowRequest / CarryThrow_LeftActionCreatesLeftThrowRequest / CarryThrow_UpActionCreatesUpThrowRequestAndHasPriority / CarryThrow_RejectedThrowKeepsHeldObject / CarryOwnerCollisionGrace_IsCentralizedAndIncludedInDropAndThrowRequests / CarryContract_UsesRequestsAndDoesNotMutateCarryableInternals / InteractionRuntime_DoesNotIntroduceBasicAttackDashWallJumpDoubleJumpOrShoot

## UNITY

- Unity Version: 6000.3.8f1
- Asset Refresh: PASS, Compile Errors: 0(error CS 0건), Relevant New Warnings: 0
- EditMode: 88/88, PlayMode: NOT RUN(과제 지정), Scene/Prefab Changes: 0

## CARRY_CANDIDATE_QUERY

문서화된 실제 우선순위 규칙: reachable+eligible 필터 → 낮은 Priority 번호 → 짧은 제곱거리 → 낮은 안정 Id. 후보 없음/전부 부적격 → 선택 없음(들기 없음). 기절 소형 적은 같은 계약(Kind)으로 선택 가능. 1×1 초과·비휴대·비도달 후보 거부 테스트 고정.

## SINGLE_CARRY_SLOT

들기가 빈 슬롯을 채우고 HeldObjectId/HeldKind 노출. 점유 중 두 번째 들기 거부(기존 휴대물 유지, 자동 스왑 없음). 슬롯 해제는 수락된 drop/throw에서만.

## SAFE_DROP

아래+행동 intent는 CHAR01 `CharacterInputSnapshot.SafeDropPressedThisFrame`과 연동(테스트에서 검증). 목적지 = 발 위치 + SafeDropOffset, `ICharacterPlacementSpaceQuery` 점유 확인 → 막히면 겹침 배치 없이 거부(휴대 유지), 비었으면 배치 요청 반환+슬롯 해제. 씬/물리 직접 수정 없음.

## DIRECTIONAL_THROW

Up/Left/Right 요청에 방향 enum+단위 벡터+속도+owner/held id+grace 포함. 위+수평 동시 입력은 Up 우선(결정적, 테스트 고정). 방향 없음 = 투척 의도 아님(휴대 유지), 빈 슬롯 투척 거부. 투척 임팩트 피해 미적용(CHAR04_03 소관).

## OWNER_COLLISION_GRACE

grace는 `CharacterCarryInteractionSettings.OwnerCollisionGraceSeconds` 단일 소스(음수 검증 예외). drop/throw 요청 모두에 동일 값 포함 테스트 고정. Unity 물리 레이어 직접 수정 없음(요청 값으로만 전달).

## FORBIDDEN_FEATURE_GUARD

Attack/BasicAttack/Melee/Shoot/Dash/WallJump/DoubleJump — 런타임 타입·공개 멤버 전수 0. CharacterActionId 잠금 5종 그대로(EquivalentTo).

## DEPENDENCY_DIRECTION

- 신규 코드는 UnityEngine(Vector2)와 자기 어셈블리만 사용 — MAP 추가 의존 없음, 기존 가드 테스트 전부 유지 PASS
- 요청/후보/질의 전부 immutable·read-only 계약(리플렉션 고정) — Carryable 내부 변조 경로 없음, 전역 싱글톤 없음

## SCOPE_VALIDATION

- `git status`: Character 트리 외 Assets 변경 0, MAP/Packages/MapDesign 0, ProjectSettings 기존 사용자 2건 외 0
- 신규 = Interaction 런타임 10 + 테스트 3 + .meta. asmdef/inputactions/Scene/Prefab/물리 레이어 자산 무변경
- 밟기/적 피해/제거/임팩트/폭탄/로프/체력/HUD 미구현. CHAR04_02 미개방·미열람

## DEPENDENCY_LEDGER

```text
휴대 후보 라이브 소스(월드 오브젝트 스캔)      : DEFERRED — 질의 계약만 고정, 소비는 통합 소관
배치 공간 라이브 소스(물리/맵 점유)            : DEFERRED — ICharacterPlacementSpaceQuery fake 검증
drop/throw 요청 소비(오브젝트 스폰·물리 적용)  : DEFERRED — 오브젝트/월드 계층 소관
투척물 임팩트 피해                             : CHAR04_03 (공용 충격·피해 계약)
밟기/기절/제거/접촉 피해                       : CHAR04_02
```

## OUT_OF_SCOPE_FINDINGS

- stale Map PlayMode asmdef 레거시 참조(기존 관찰 유지)

## DONE CONDITIONS

- [x] CHAR03_03 PASS·CHAR03 EXIT 승인 검증
- [x] registry marker/hash 검증
- [x] 후보 질의가 결정적 우선순위로 정확히 하나 선택
- [x] 1×1 초과·비휴대 후보 거부
- [x] 단일 슬롯: 첫 들기 수락, 두 번째 거부
- [x] 아래+행동 안전 내려놓기가 배치 요청 반환
- [x] 막힌 내려놓기는 겹침 없이 거부 + 휴대 유지
- [x] 위/좌/우 투척이 결정적 요청 반환
- [x] 거부된 투척은 휴대 유지
- [x] 소유자 충돌 유예 중앙화 + drop/throw 요청 포함
- [x] Carryable 내부 직접 변조 없음
- [x] 일반 공격/dash/wall jump/double jump/shoot 부재 유지
- [x] EditMode 88개(≥88) 전부 PASS
- [x] compile error 0
- [x] 범위 검증 완료
- [x] CHAR04_02 LOCKED 유지

## NEXT

- Current Task after finalize: NONE
- Next Task auto-opened: NO (`CHAR04_02_IMPLEMENT_STOMP_STUN_REMOVAL_AND_CONTACT_DAMAGE`는 LOCKED 유지, 새 MCP_INBOX 패키지로만 개방)
