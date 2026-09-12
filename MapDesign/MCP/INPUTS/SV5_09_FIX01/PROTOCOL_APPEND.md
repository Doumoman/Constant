
## SV5 loop topology evidence correction

- 재합류 loop의 alternate path, cycle rank, bridge 변화는 실제 baseline/final graph에서 계산한다. link 수를 더하거나 빼는 산술값과 고정 boolean은 증거가 아니다.
- loop의 내부 중심선은 baseline supported path와 분리되고 양 endpoint 두 곳에서만 접촉하며 최소 2개의 새 AIR 셀을 조각한다.
- shortcut 비용은 같은 endpoint의 baseline/final supported foot graph BFS로 비교한다. centerline 길이를 양쪽 비용으로 대입하지 않는다.
- 독립 검사는 occupancy와 edge CSV를 읽어 graph를 재구성해야 하며 production export의 판정 boolean을 신뢰하지 않는다.
