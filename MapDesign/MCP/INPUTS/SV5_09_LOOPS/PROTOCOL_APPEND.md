
## SV5 loop evidence amendment

- SV5_09의 LOOP/RANDOM_SHORTCUT은 실제 1×1 cell payload와 약 2칸 통행 공간을 가져야 한다. 공간 id 사이의 추상 edge만으로는 완료할 수 없다.
- 길이는 양 endpoint room-side AIR 셀을 포함한 상하좌우 중심선 셀 수이며 최대 24다. +1 지지 계단만 이번 단계에 포함한다.
- LOOP는 baseline alternate path와 cycle-rank +1을, RANDOM_SHORTCUT은 같은 상태·끝점에서 엄격한 실제 비용 감소를 증명한다.
- 새 연결은 모든 합법 FSM state와 6 resource order에서 진행 gate를 우회하지 않아야 한다.
- 20~50칸 불규칙 샛길은 SV5_10 책임이며 SV5_09 수치를 부풀리는 데 사용할 수 없다.
