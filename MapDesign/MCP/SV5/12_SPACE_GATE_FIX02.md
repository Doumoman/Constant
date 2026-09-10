# SV5_06_FIX02 — 상태별 route-owned gate 보완

## 결론

SV5_06_FIX01의 7,188개 접촉 전수검사와 corridor 충돌 수정은 보존했다. FIX02는 서로 다른 route predicate를 물리 접촉 하나의 AND/OR gate로 합치지 않는다. 조건이 같은 route의 접촉만 FSM split join으로 연결하고, 조건이 다른 접촉은 계획상 분리 경계로 유지한다.

실제 RMAP13 guard가 있는 세 core connector에는 connector 소유의 typed gate를 각각 하나씩 배치했다. 각 gate는 resource mask, Forge/Seal/Boss 조건, source connection/route/port, target port, 흐름과 방향, source/target anchor, 전체 폭 cardinal face cut을 가진다. 차단 셀은 사용하지 않아 기존 ProtectedAir와 정본 OPEN cell을 점유하지 않는다.

## W01 / W02

- W01 `mask=7, ForgeMade=true, SealOpen=false, BossComplete=false`: `FORGE_GATED_SEAL_APPROACH` gate는 Forge 조건만 소비하며 `RMAP15_SITE_SEALBOSS_PORT_ENTRY`까지 OPEN이다. FIX01의 `SV5_GATE_F3608BF3D1EE0923F69A`가 막았던 `(533,311..313)` 포트를 새 gate가 점유하지 않는다.
- W02 `mask=7, ForgeMade=true, SealOpen=true, BossComplete=false`: `SEAL_GATED_BOSS_APPROACH`는 OPEN이고 Boss 접근이 가능하다. `BOSS_GATED_EXIT_APPROACH`는 같은 상태에서 SEALED이며 BossComplete 뒤에만 Exit로 열린다. FIX01의 `SV5_GATE_3699556D982E455C6E7E`가 정본 OPEN `(560,311..313)`을 재차단하던 소유 충돌을 제거했다.

## 생산 검증

상태 검사는 `INFILL_PENDING`을 통행 AIR로 간주하지 않는다. 승인된 connection envelope를 route별 이동 공간으로 만들고, 같은 typed predicate의 승인 접촉만 route 사이 연결로 사용한다. 검사 상태에서 모든 gate를 동시에 열거나 닫은 뒤 source anchor, target port, SEALED cut, OPEN path를 탐색한다.

gate face는 source port에서의 BFS 거리 분할 경계 전체를 사용하므로 일부 face를 제거하면 실제 side-bypass 반례가 검출된다. ConditionalGate 예약 검사는 셀 소유가 ProtectedAir에 겹치는 경우 실패시키며, 셀을 점유하지 않는 `FACE_ONLY_STATE_BOUNDARY`만 허용한다. connector 검증은 unknown port, endpoint, flow, direction뿐 아니라 port에서 연결되지 않은 aperture도 거부한다.

projection은 검증된 gate ID를 해당 source segment에 묶은 뒤 RMAP13의 세 자원 6순서를 다시 평가한다. 대표 plan의 모든 reachable state는 goal 또는 정상 복귀 상태로 역도달 가능하며 dead end는 0이다.

## 증거와 남은 책임

- focused EditMode: `StarNight.Map.Tests.EditMode.Sv5`, 51 passed / 0 failed / 0 skipped.
- G01–G08은 W01/W02, 잘린 cut 우회, ProtectedAir 상태 충돌, 연결/aperture 반례, connector 삭제와 one-way 반례, digest mutation, export/원본 보존을 실제 생산 API로 검사한다.
- `ComposedGeometryReady=false`, `PlayerVerified=false`를 유지한다. GameObject/runtime door, 세부 지형, Player body/jump, Scene Bake는 이번 작업 범위가 아니다.
- 22개 장소와 큰 `INFILL_PENDING`의 Family·밀도·재합류 책임은 기존 순서대로 SV5_07/08/09에 남는다. 이번 작업은 후속 Task를 열지 않는다.
