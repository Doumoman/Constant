# SV5_07 독립 지형 반복 억제

## 생산 소비

`Sv5SpaceGraphPlanner.Plan → PlaceFamily → Sv5SpaceDiversity.Select`가 실제 합법 좌표를 선택한다. 정책은 기본 ON이며 기존 좌표 후보, 섹터 우선순위, stable rank, 2칸 예약 충돌 검사를 유지한다. 후보마다 난수를 다시 뽑지 않는다. `SV5_DIVERSITY_V1|seed|ordinal|family_key`의 UTF-8 SHA-256 앞 16 hex를 unsigned64로 해석한 `%100`을 한 요청 내내 재사용한다.

half-open bounds 사이 빈 타일 Manhattan gap이 36 이하인 독립 formation을 센다. 이웃 0/1/2/3+의 통과 weight는 100/70/49/34다. 이는 패키지 운영 기본값이지 사용자가 직접 지정한 확률이 아니다. 모든 soft 후보가 거절되면 합법 후보 중 이웃 수가 최소인 후보를 기존 안정 순서로 선택한다. 합법 후보 자체가 없으면 배치 실패다. 장소 삭제, 크기 축소, 보호 셀 무시는 허용하지 않는다.

## 식별과 분할

현재 place ID가 formation ID다. 독립 place를 다른 formation으로 합치는 입력은 거부한다. 분할 조각은 원래 formation ID와 고유 part ID를 전파하며 family 충돌을 거부한다. 쌍의 거리는 조각 간 최소 gap이고 formation 쌍은 한 번만 센다. 가짜 미래 청크를 생성하지 않는다.

family_key는 명시적 alias로만 결정한다. variant/표시 이름은 진단 정보이며 감점을 피하는 키가 아니다. ORDINARY_ROOM_A~F는 ORDINARY_ROOM으로 집계하지만 연결 공간이므로 감점 면제다. Core는 분포에 표시하되 고정 위치/감점 면제다. 알려지지 않은 family는 exact 문자열과 기본 감점을 사용한다.

## 배치와 안전성

기본 14개 요청과 반복 fixture(기본 요청 + ordinal 16/32, 각각 CAVE_BAND 60×24)를 seed 1304에서 OFF/ON 비교한다. OFF는 FIX04 기본 geometry 기준선이다. 기본 profile에 감점 가능한 반복 자체가 없으면 `NO_ELIGIBLE_REPEAT`로 기록한다. 반복 fixture의 실제 감소와 혼동하지 않는다.

배치 후 같은 plan에서 포트와 통로를 재생성하고 FIX04 `RepairGateCorridors`, exact local barrier, 전역 좌표 movement, canonical FSM의 6개 자원 순서 product를 그대로 소비한다. gate/FSM/validator 원본을 변경하지 않는다. 정책 profile·seed·실제 후보 trace·formation·쌍은 plan semantic digest에 포함한다.

## 출력과 완료 수준

최종 실제 수치와 시험은 `REPORTS/SV5_07_DIVERSITY_RESULT.md` 및 `GENERATED/SV5_07/`를 소비한다. default/repeat는 ON 출력 한 벌씩이며 OFF geometry는 diversity.json에 포함한다. pairs.csv/decisions.csv와 비교 그림은 같은 실제 plan에서 만든다. 기존 회귀의 임시 export는 SV5_07/_work/legacy_exports의 fixture별 경로에만 쓴다. 과거 FIX04/FIX03 출력은 읽기 전용이다.

그림의 외곽은 예정 장소 점유, 선은 예정 통로이며 내부는 미조립이다. 1×1 좌표 도달성은 바닥/착지/머리 여유를 갖춘 Player 이동 증명이 아니다. `ComposedGeometryReady=false`, `PlayerVerified=false`. SV5_08 밀도, SV5_09 loop, SV5_41 조립, SV5_44 Player는 후속 책임이다. 이 작업은 새 Task를 자동으로 열지 않는다.
