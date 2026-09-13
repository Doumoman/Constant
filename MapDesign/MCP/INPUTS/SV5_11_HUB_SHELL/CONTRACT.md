# SV5_11_HUB_SHELL Contract

이 계약은 624×416 월드에 “여섯 갈래길”의 공간 shell, 외벽, 목, 지지 공간과 실제 Port를 만든다.
중앙 계수나무의 실제 Grab/등반 지형은 SV5_12_TREE_GRAB의 소유다.

## 사용자 확정 규칙

- H01: 허브 내부 인지 공간은 약 12×30개의 실제 1×1 타일 셀 규모다.
- H02: 좌측 3개, 우측 3개의 Port socket을 서로 다른 세 높이대에 둔다.
- H03: 각 Port의 유효 통행 높이는 6~7칸이다.
- H04: 목표는 서로 다른 외부 공간으로 이어지는 실제 연결 6개다.
- H05: 주변 연결 후보가 부족하면 실제 연결 4~6개를 허용한다.
- H06: 실제 연결이 4개 미만이면 그 후보를 채택하지 않고 다른 허브 후보를 선택하거나 미배치한다.
- H07: 같은 외부 공간에 여러 Port를 연결해 연결 수를 부풀리지 않는다.
- H08: 미사용 socket은 실제 연결 수에 포함하지 않는다.
- H09: 중앙에는 후속 계수나무를 위한 연속 예약 공간을 둔다. 배경 나무나 장식은 Grab surface가 아니다.
- H10: 실제 계수나무 충돌면, 가지, 중간 착지, Grab 판정과 1~2칸 등반은 SV5_12에서 구현한다.
- H11: 4×4 MicroPattern을 배치 소유 단위로 사용하되 최종 shell·Port·AIR·SOLID는 실제 1×1 셀로 기록한다.
- H12: 허브는 추상 그래프 노드나 사각형 표시만 추가해서는 안 된다. 실제 occupancy를 예약·변경해야 한다.
- H13: ProtectedAir, FixedSolid, Type0, core route, gate aperture, 기존 예약과 승인된 sidepath를 침범하거나 우회하지 않는다.
- H14: 활성 Port는 서로 다른 외부 RoomId/SpaceGroupId와 실제 연결 증거를 가져야 한다.
- H15: 허브 내부 circulation reservation은 모든 활성 Port를 연결하지만 PlayerVerified로 승격하지 않는다.

## SV5_10 샛길 해석 고정

- H16: SV5_10 샛길은 정방향 완주만 필수이며 역방향 완주는 필수가 아니다.
- H17: `SIDE_PATH_RETURNING`은 다른 통로로 재합류한다.
- H18: `SIDE_PATH_DEAD_END`는 `Rejoins=false`, `Returnable=false`여도 정상이다.
- H19: 단방향 낙하·점프·매달리기 구조를 허용하며 역방향 불가만으로 실패 처리하지 않는다.

## 구현 운영 기준

- H20: 최초 shell 점유 footprint는 24×40 셀을 우선 사용한다. 이는 내부 12×30과 외벽·목·지지 여유를 포함하는 구현 기준이다.
- H21: 기본 profile과 repeat profile에서 각각 허브 1개를 목표로 한다.
- H22: 여섯 socket은 좌우 각 3개여야 하지만 좌우 geometry의 완전한 거울대칭은 요구하지 않는다.
- H23: 후보 선정은 기존 Room/Port/reservation/occupancy 자료를 한 번 인덱싱해 수행한다.
- H24: 후보마다 624×416 dictionary 복사, 전체 foot graph 재구축, 전체 BFS를 수행하지 않는다.
- H25: 고정 48×32 Sector, SectorId, Sector 단위 packing·RNG·export를 재도입하지 않는다.
- H26: 동일 seed와 입력은 동일 HubId, footprint, socket, connection, cell digest를 만든다.
- H27: 허브 shell 채택 뒤 기존 core/loop/sidepath와 9 states×6 resource orders를 최종 batch에서 한 번 검증한다.

## 상태와 책임 경계

- H28: SV5_11은 shell과 연결 골격까지만 소유한다.
- H29: `TreeGrabGeometryReady=false`, `ComposedGeometryReady=false`, `PlayerVerified=false`를 유지한다.
- H30: tree slot은 예약만 하며 임시 사다리, 자동 Grab, 가짜 Player 통과 결과를 만들지 않는다.
- H31: 연결 후보가 부족하면 최소값을 낮추거나 같은 외부 공간을 중복 계산하지 않고 명시적으로 미배치 결과를 낸다.
- H32: 일반 코드·시험 실패는 구현 중 수정한다. source/state/hash/권한 충돌만 BLOCKED로 종료한다.

## 필수 export

각 `default`/`repeat` profile root:

- `hub_shell.json`
- `hub_candidates.csv`
- `hub_cells.csv`
- `hub_sockets.csv`
- `hub_ports.csv`
- `hub_connections.csv`
- `hub_validation.json`
- `preview/hub_shell.svg`

task root:

- `hub_comparison.json`
- `check_hub_shell.py`
- `independent_hub_shell_audit.json`
- `focused_results.xml`
- `BINDING.json`

`hub_ports.csv`는 각 Port의 `x`, `y` anchor를 기록한다. 독립 checker는 production C#을 import하지 않고
footprint, 1×1 occupancy, 4×4 ownership, 좌우 socket 수, 높이대, aperture 높이, distinct external connection,
최소 4개, Port anchor 사이 shell-level cell 연결, tree reservation, 보호영역 비침범, digest와 Sector 비의존을 다시 계산한다.
