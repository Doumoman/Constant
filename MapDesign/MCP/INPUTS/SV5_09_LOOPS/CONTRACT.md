# SV5_09_LOOPS Contract

이 문서는 사용자 피드백을 실행 가능한 타일 규칙으로 고정한다. `MUST`는 합격 조건, `DEFAULT`는 이번 구현 기본값이다.

## 고정 좌표와 표현

- L01 MUST: world는 624×416, tile은 1×1, pattern payload는 4×4, microchunk는 12×8 기준을 유지한다.
- L02 MUST: loop는 실제 좌표의 `AIR`, `SOLID_SUPPORT`, `APERTURE_OVERRIDE` 셀 목록과 순서 있는 중심선을 가진다.
  그래프 edge, 선분, room id만 있는 연결은 불합격이다.
- L03 MUST: 보행 중심선 한 칸마다 몸통·머리 AIR 두 칸과 밑의 SOLID 지지가 있어야 한다. 상승은 지지된 +1 계단이다.
  회전과 상승 landing에는 충돌 없는 머리 공간을 둔다.
- L04 MUST: 기본 이동만 사용한다. +2 Jump+Grab, 아이템, 폭발, 사다리, one-way 특수 발판을 필요로 하지 않는다.
- L05 MUST: 중심선 길이는 양 끝의 room-side AIR 셀을 포함해 상하좌우 AIR 중심선 셀 수 4~24다.
  대각선 한 걸음도 수평/수직 AIR 셀을 각각 세며 순간 대각 이동으로 축약하지 않는다.

## 후보와 실제 순환

- L06 MUST: 양 끝은 서로 다른 기존 Infill 방/가지이며 실제 room interior AIR에 닿는다. 기존 직접 연결 한 쌍,
  parent-child 한 간선, 동일 aperture/동일 ordered path의 중복은 후보에서 제외한다.
- L07 MUST: baseline에서도 양 끝 사이에 기존 합법 경로가 있어야 한다. 새 연결을 제거한 재검사에서 그 경로가 남아야 한다.
- L08 MUST: 새 연결 하나를 추가할 때 공간 그래프의 cycle rank가 정확히 1 증가해야 한다. dangling/stub는 0이다.
- L09 MUST: 연결 opening은 기존 SOLID 경계의 정확한 override로 기록한다. 과거 Infill data/export를 수정하거나 새 AIR를
  이미 존재했던 것처럼 재기록하지 않는다.
- L10 MUST: fixed core, Type0, gate barrier/aperture, port access, reservation, forbidden/outside world를 침범하지 않는다.
  다른 통로와의 교차는 실제 합법 junction으로 선언된 경우만 허용한다.
- L11 MUST: 새 연결을 넣은 실제 physical cell graph로 모든 legal FSM state와 6 resource acquisition order를 검사한다.
  Forge/Seal/Boss/Exit 조건을 앞당기거나 닫힌 gate를 우회하는 후보는 reject한다.

## LOOP와 RANDOM_SHORTCUT

- L12 MUST: 같은 시작·끝·FSM 상태의 baseline 최단비용과 loop 적용 후 비용을 실제 cardinal movement로 비교한다.
- L13 MUST: 새 비용이 엄격히 작을 때만 `RANDOM_SHORTCUT`이다. 그 외 cycle은 `LOOP`다.
  감소율 20% 이상은 `SIGNIFICANT=true` 보조 지표일 뿐 shortcut 자격의 추가 필수조건은 아니다.
- L14 MUST: seed가 같으면 후보 순서, accepted/rejected 이유, 셀, digest, export bytes가 같다.

## 밀도와 형태

- L15 DEFAULT: stable hash eligibility 30%, target 32, hard minimum 16, maximum 48, sector당 최대 3이다.
  accepted는 최소 12개의 48×32 sector에 분산하며 한 국소 구역에 같은 형태를 연속 복제하지 않는다.
- L16 MUST: minimum 16 또는 sector 12를 만족하지 못하면 PASS가 아니다. target 32 미달은 reject reason 집계와 함께 허용한다.
- L17 MUST: 4×4 패턴 인스턴스와 write mask로 통로/지지/opening을 표현한다. 최소 두 개 이상의 고체형 contour 변형을 쓰고,
  전부 반듯한 사각 tunnel 한 형태로 만들지 않는다. 다만 2칸 통행 여유와 지지는 깨지지 않는다.
- L18 MUST: 20~50칸 불규칙 샛길, hub shell, 특수 장소 콘텐츠, Player 실플레이는 각각 SV5_10 이후 책임이다.

## 필수 export와 검증

default와 repeat 각각 아래를 `MapDesign/MCP/GENERATED/SV5_09_LOOPS/{default,repeat}/`에 기록한다.

- `space_graph.json`, 기존 core/infill/physical 검증 산출물 전부.
- `loops.json`: profile/digest, 후보·accepted·rejected 집계, cycle rank/bridge 전후, 상태 보존 요약.
- `loop_candidates.csv`: stable rank/hash, endpoints, baseline cost, rejection code.
- `loop_links.csv`: id/type/endpoints/sector, old/new cost, reduction, alternate path, cycle delta.
- `loop_cells.csv`: loop id, ordered index, x/y, role, source value, final value, support/headroom ownership.
- `loop_patterns.csv`, `loop_instances.csv`: 4×4 payload/mask/transform/variant와 실제 셀 digest.
- `loop_validation.json`: L01~L18 중 이번 단계 적용 항목, 모두 boolean과 실제 수치/증거 경로.
- `preview/overview.svg`, `preview/A1.svg`~`D4.svg`, `preview/detail.svg`, `preview/index.html`.
  그림은 실제 1×1 AIR/SOLID 셀, 2칸 통로, 두 room interior, alternate route, rejected 보호 셀을 범례로 구분한다.

독립 `tools/check_loops.py`는 production C#을 import하지 않고 CSV/JSON을 다시 읽어 길이, 양끝 접촉, 2칸 여유,
SOLID support, cycle/alternate path, 비용 분류, 중복, 보호영역, digest와 두 seed의 minimum/분산을 재계산한다.
검사 결과는 `independent_loop_audit.json` 하나로 export한다.

## 시험 책임

- 실제 production entrypoint 결과를 검사하며 hand-authored test-only graph로 PASS하지 않는다.
- default와 repeat의 동일 seed 재실행 byte/digest 결정성을 확인한다.
- 길이 23/24 허용, 25 거부, +1 허용, +2 거부, 두 AIR/지지 누락 거부를 확인한다.
- 이미 직접 연결, stub, duplicate, Type0/core/gate/reservation 침범, 조건 우회 후보를 각각 거부한다.
- LOOP와 RANDOM_SHORTCUT 분류를 실제 최단거리로 검증한다.
- 기존 focused 102개 test fullname, assertion 책임, skip=0을 보존한다.
