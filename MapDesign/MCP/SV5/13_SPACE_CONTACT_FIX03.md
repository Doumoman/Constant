# SV5_06_FIX03 — 전역 좌표 접촉과 상태 gate

## 결론

FIX02의 W01/W02 포트 보완과 RMAP13 상태 전이는 유지했다. FIX03은 route ID 또는 predicate 문자열을 공간 벽으로 사용하지 않는다. 실제 통행 노드는 모든 accepted connection의 `Centerline`과 `ApertureCells`를 world 좌표로 합친 집합이며, cardinal face로만 이동한다. `Clearance`와 `INFILL_PENDING`은 통행 노드가 아니다. 같은 world 좌표가 여러 route에 속해도 하나의 노드다.

7,188개 SHARED/FACE 접촉을 모두 생산 `Sv5SpacePhysicalMovement`에 넣었다. 같은 typed predicate는 `Join`, 다른 predicate의 225개 접촉은 nonempty boundary와 실제 typed global cut을 갖는 `ConditionalGate`다. SHARED 좌표 자체를 face-only separation으로 주장하지 않고, 소유 gate의 전역 region cut과 동시 상태 검사로 재진입을 차단한다. 빈/가짜 boundary와 SHARED의 직접 face-only 분리 fixture는 생산 validator에서 실패한다.

## 동시 gate 상태 증명

세 gate의 blocking cell/face는 route key 없이 world 좌표에 전역 적용한다. 모든 상태에서 세 gate를 동시에 열고 닫으며 source port, target port, 차단 여부와 열린 경로 witness를 기록한다.

- INITIAL: Forge→Seal, Seal→Boss, Boss→Exit가 모두 차단된다.
- Forge 완료: Forge→Seal은 열리고 Seal→Boss와 Boss→Exit는 차단된다.
- Seal 완료: Seal→Boss는 열리고 Boss→Exit는 차단된다.
- Boss 완료: Boss→Exit가 열린다.
- 세 열린 상태의 witness 길이는 각각 189, 47, 231이고, 여섯 닫힌 상태의 target 도달은 모두 false다.
- RMAP13 자원 3종의 6순서, 모든 reachable state의 역도달, dead-end 0은 그대로 유지된다.

gate 형상 자체의 SEALED/OPEN 검사는 대상 gate를 격리해 full cut을 확인한다. FIX03 물리 상태 검사는 별도로 모든 gate를 동시에 적용한다. 이 구분으로 다른 선행 gate가 단일 gate의 source fixture를 가리는 오류 없이, 실제 전역 우회는 숨기지 않는다.

## 의미 digest와 증거

physical movement digest는 전역 Passage/aperture cell, ordered centerline, port/flow, contact 좌표와 cell kind, crossing/boundary/owner, typed predicate, gate cell/face와 SEALED/OPEN 의미, 상태별 닫힘/열림 집합과 witness를 포함한다. 집합 입력 순서와 route/predicate 진단 label은 reachability를 바꾸지 않는다. 접촉·boundary·gate geometry·state 의미 mutation은 digest를 바꾼다.

`GENERATED/SV5_06_FIX03`의 JSON/CSV/proof/preview는 하나의 accepted plan digest를 소비한다. preview는 전체도, 16개 확대도, W01/W02, FIX02 우회 전후를 포함한다. focused EditMode 결과는 `StarNight.Map.Tests.EditMode.Sv5` 59 passed / 0 failed / 0 skipped다.

## 남은 책임

이번 증거는 계획 좌표/형상 검증이다. `ComposedGeometryReady=false`, `PlayerVerified=false`를 유지한다. 실제 지형 합성, Player body/jump, Family 분포, 일반 공간 밀도와 재합류는 기존 SV5_07/08/09 및 후속 소유 범위에 남는다. SV5_07은 LOCKED 상태를 유지한다.
