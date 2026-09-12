# SV5 actual tile loops V5

SV5_09의 production 진입점은 `Sv5SpaceGraphPlanner.PlanWithLoops`다. 이 함수는 먼저 기존
`PlanWithInfill`을 그대로 생성하고, 그 immutable 결과에서 실제 AIR/SOLID 셀과 room/branch ownership을 읽는다.
과거 Infill payload는 변경하지 않으며 새 통로의 SOLID opening만 `APERTURE_OVERRIDE`로 기록한다.

각 accepted link는 서로 다른 비직결 infill room/branch를 같은 actionless host 안에서 잇는다. 양 endpoint를 포함한
ordered cardinal centerline은 4~24셀이고, supported foot path는 수평 열마다 최대 +1만 변한다. 모든 방문에는 두 칸 AIR
clearance와 SOLID support 증거가 있으며 +2 Jump+Grab, item, ladder, one-way platform은 사용하지 않는다.

후보는 seed, infill digest, endpoint, ordered path의 stable hash로 정렬하고 30% eligibility를 적용한다. 48×32 sector당
최대 3개, hard minimum 16개와 12 sector 분산을 강제한다. target 32 미달은 후보 고갈/reject 집계와 함께 허용한다.
동일 endpoint pair, parent-child, 동일 aperture/path, 보호/예약/core/Type0/gate/port 침범, 외부 AIR 의존, loop 간 겹침은
거부한다.

각 accepted link는 제거 상태에서 실제 baseline cardinal AIR 경로가 남고, 추가 시 cycle rank가 정확히 +1이다.
같은 endpoint의 baseline shortest cost보다 새 centerline cost가 엄격히 작을 때만 `RANDOM_SHORTCUT`, 그 외는 `LOOP`다.
20% 감소는 `significant` 지표일 뿐 분류 조건이 아니다.

새 AIR payload는 기존 graph connection의 centerline을 바꾸지 않고 해당 actionless host의 physical aperture union에만
합성하여 9개 gate state와 6개 resource acquisition order를 다시 검사한다. 결과의
`ComposedGeometryReady=false`, `PlayerVerified=false`는 유지한다. 20~50셀 sidepath와 Player 검증은 SV5_10 이후 책임이다.

증거는 `GENERATED/SV5_09_LOOPS/{default,repeat}`의 graph/infill/physical 산출물과 `loops.json`, loop CSV 5종,
`loop_validation.json`, 실제 셀 preview에 기록한다. 독립 checker는 production code를 import하지 않고 CSV/JSON만 읽는다.
