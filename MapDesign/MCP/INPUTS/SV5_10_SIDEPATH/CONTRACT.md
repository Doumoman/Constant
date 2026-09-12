# SV5_10_SIDEPATH Contract

이 계약은 사용자가 확정한 “약 2칸 통로, 20~50칸, 매우 불규칙한 샛길”을 실제 1×1 셀로 구현한다.
수치는 사용자 확정값과 구현용 운영 기준을 구분한다.

## 사용자 확정 규칙

- S01: 샛길은 624×416 월드의 실제 1×1 타일 셀을 변경한다. 추상 선만 추가하지 않는다.
- S02: 길이 단위는 상하좌우로 이어진 `AIR 중심선 셀 수`다. 첫·마지막 통행 AIR 셀을 포함해 20~50개다.
- S03: SOLID aperture, 바닥 지지 셀, 별도 approach 셀은 S02 길이에 넣지 않는다.
- S04: 일반 수평 구간의 `SUPPORTED_FOOT` 셀은 발 위치 AIR·머리 AIR·아래 SOLID 지지를 가진다.
- S05: 짧은 상승·회전·점프 접점은 낮은 쪽에 국소 3칸 머리 여유를 둘 수 있다.
- S06: 긴 순수 수직 관을 통로로 세지 않는다. 기본 이동과 +1칸 지지 계단으로 왕복 가능해야 한다.
- S07: 일반 이동에서 +2칸 상승이나 아이템·폭발·물·열차·미구현 장치에 의존하지 않는다.
- S08: 반복 파형이나 매 셀 교대 톶니가 아니라 비대칭 굴곡·평탄·짧은 상승·작은 공동을 섞는다.
- S09: 의도하지 않은 자기 교차를 거부한다. 교차가 필요하면 JunctionId·소유·간선을 명시한다.
- S10: 다른 공간에 재합류하거나, 막다른 경우 들어온 길로 실제 복귀할 수 있어야 한다.
- S11: 보상 없는 샛길·막다른 굴도 허용한다. 자동으로 상자나 이벤트를 배정하지 않는다.
- S12: 핵심 진행, ProtectedAir/FixedSolid, Type0, 예약 slot, gate aperture를 침범하거나 우회하지 않는다.

## 운영 기준

- S13: default/repeat 각각 accepted>=8, returning>=5, distinct home sectors>=6, sector당 최대 2개다.
- S14: 각 중심선은 방향 전환>=2, 서로 다른 run length>=2이며 반복 1셀 sawtooth를 거부한다.
- S15: 각 샛길은 새로 굴착한 중심선 AIR가 8개 이상이며 중심선의 40% 이상이다.
- S16: 재합류 샛길의 두 RoomId는 달라야 한다. 기존 direct pair와 동일 기하를 중복하지 않는다.
- S17: 최종 비용이 baseline보다 엄격히 작을 때만 RANDOM_SHORTCUT 보조 분류를 붙인다. 지름길 수는 강제하지 않는다.
- S18: SIDE_PATH_RETURNING과 SIDE_PATH_DEAD_END를 별도로 기록하고 모든 DEAD_END에 왕복 증거를 둔다.

## Sector-local 성능 계약

- S19: 48×32 sector별 endpoint/occupancy index를 계획당 한 번 만든다.
- S20: 50 AIR 셀 경로가 가능한 sector pair만 AABB Manhattan lower bound로 비교한다. 단순 전 sector×전 sector 비교를 금지한다.
- S21: 정규화한 sector pair와 endpoint pair는 각각 한 번만 생성·평가하며 stable ID 순으로 결정한다.
- S22: 후보마다 624×416 dictionary 복사, 전체 foot graph 재구축, 전체 BFS를 수행하지 않는다.
- S23: changed-cell overlay와 영향 foot node만 국소 재평가하고 FIX01 baseline context/shortest cache를 재사용한다.
- S24: 선택 완료 뒤 합쳐진 batch에 대해서만 전역 topology와 9 states×6 orders를 정확히 한 번 검증한다.
- S25: 같은 sector 내부만 보지 않는다. S20 범위의 이웃 sector를 포함해 경계 횡단 샛길을 보존한다.

## 필수 export와 독립 검사

각 profile root에 다음을 기록한다.

- `sidepaths.json`
- `sidepath_candidates.csv`
- `sidepath_links.csv`
- `sidepath_cells.csv` (`movement_role`은 `SUPPORTED_FOOT` 또는 `STEP_TRANSITION`)
- `sidepath_checks.csv`
- `sector_index.csv`
- `sector_pairs.csv`
- `sidepath_validation.json`
- `preview/sidepaths.svg`

root에는 `sidepath_comparison.json`, `independent_sidepath_audit.json`, `focused_results.xml`, `BINDING.json`을 둔다.
독립 checker는 production C#을 import하지 않고 CSV의 중심선, 길이, 인접성, 중복, AIR/head/support, 굴착량,
sector pair 완전성·중복, 분산, 복귀·우회 지표를 다시 계산한다.

`GENERATED/SV5_09_FIX01`은 읽기 전용 선행 증거다. 새 결과는 `GENERATED/SV5_10_SIDEPATH`에만 쓴다.
ComposedGeometryReady=false, PlayerVerified=false를 유지한다. 실제 Player 성공으로 승격하지 않는다.
