# SV5_09_FIX01 correction contract

## 실제 계산 기준

- F01: `baseline_occupancy.csv`와 `final_occupancy.csv`는 x/y/value/provenance/owner를 가지며 중복 좌표의 충돌은 0이다.
- F02: supported foot node는 `(x,y)`와 `(x,y+1)`이 AIR, `(x,y-1)`이 SOLID인 실제 셀이다.
- F03: foot edge는 `abs(dx)=1 && abs(dy)<=1`이고 전이 중 body/head가 충돌하지 않는다. 순수 수직과 +2는 없다.
- F04: baseline room topology의 vertex/edge/component/cycle rank와 bridge id를 실제 알고리즘으로 계산한다.
- F05: accepted link는 별도 topology edge다. baseline에서 endpoint 사이 alternate path가 실제로 존재한다.
- F06: 각 link 단독 추가 전후 `cycleRank = E - V + components` delta가 정확히 1이다.
- F07: bridge before/after는 Tarjan 또는 edge-removal BFS로 재계산한다. `before-links.Count` 산술 대입은 금지한다.
- F08: loop 내부 path는 baseline supported graph와 겹치지 않으며, baseline 접촉은 양 endpoint junction 두 곳뿐이다.
- F09: 각 link에는 최소 2개의 `NEWLY_CARVED_AIR` 중심선 셀이 있다. 기존 통로를 그대로 재라벨링하지 않는다.
- F10: baseline/final shortest cost는 동일 endpoint의 supported foot graph BFS 결과다. final<baseline일 때만 shortcut이다.
- F11: default/repeat 각각 accepted>=16, sectors>=12, max endpoint-inclusive length<=24, bypass/dangling/duplicate=0이다.
- F12: 9 legal states와 6 resource orders를 최종 누적 tile union에서 검사한다.
- F13: 기존 116개 test fullname/책임과 09 immutable evidence를 보존하고 신규 topology 회귀를 추가한다.
- F14: deterministic rerun의 모든 신규 CSV/JSON/SVG bytes가 일치한다.

## 필수 산출물

`GENERATED/SV5_09_FIX01/{default,repeat}`에 기존 09 호환 export와 함께 다음을 둔다.

- `baseline_occupancy.csv`, `final_occupancy.csv`
- `baseline_foot_nodes.csv`, `final_foot_nodes.csv`
- `baseline_topology_edges.csv`, `final_topology_edges.csv`
- `loop_topology_proofs.csv`: link, endpoints, baseline/final cost, alternate path, V/E/C/rank 전후, bridge 전후,
  baseline 접촉 수, newly carved 수, 실제 계산 digest.
- `loop_cells.csv`, `loop_links.csv`, `loop_validation.json`, `topology_validation.json`
- 실제 셀 기반 `preview/overview.svg`, `preview/detail.svg`, A1~D4와 index.html

독립 checker는 위 CSV에서 그래프를 직접 재구성한다. `alternate_path_exists`, `cycle_delta`, `body_air` 같은 결과 boolean을
합격 근거로 사용하지 않고 원시 occupancy/node/edge 행으로 다시 계산한다.

## 필수 negative witness

- 고정 cycle delta/alternate true만 기록한 payload는 실패한다.
- baseline과 동일한 path를 새 loop로 재라벨링하면 실패한다.
- baseline 접촉이 3곳 이상인 path는 실패한다.
- topology edge 없이 host aperture union만 넓히면 실패한다.
- baseline/new cost에 같은 centerline count를 대입하면 실패한다.
- occupancy에서 headroom/support 하나를 제거하면 독립 checker가 실패한다.
