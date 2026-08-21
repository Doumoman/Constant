# 광역 맵 프로젝트 동결 규칙 v1.0

> 이 문서는 맵 구현에서 이미 확정된 값을 짧게 보존하는 전역 규칙이다.
> 일반 구현 TASK는 이 값을 변경하지 않는다.

# 1. 좌표와 공간 계층

## 1.1 월드

```text
World Width  = 624 logical tiles
World Height = 416 logical tiles
```

월드 밖에 런타임 지형을 생성하지 않는다.

## 1.2 탐사 섹터

```text
Sector Width  = 48 tiles
Sector Height = 32 tiles

624 / 48 = 13
416 / 32 = 13

World Grid = 13 × 13 = 169 sectors
```

섹터 좌표 범위:

```text
sx = 0..12
sy = 0..12
```

## 1.3 마이크로청크

```text
MicroChunk Width  = 12 tiles
MicroChunk Height = 8 tiles

Sector = 4 × 4 MicroChunks
       = 16 MicroChunks
```

한 마이크로청크의 타일 셀은 정확히 `12×8 = 96`개다.

## 1.4 좌표 원칙

- 논리 좌표는 정수 셀이다.
- 동일 변환 함수가 Editor/Runtime/Test에서 공용으로 사용되어야 한다.
- 좌표 변환은 중복 구현하지 않는다.
- 월드 전체 624×416 타일을 수동 CSV 한 장으로 작성하지 않는다.

---

# 2. 탐사 섹터 Route Type

`0·1·2·3`은 섹터 내부 모양이 아니라 섹터 외곽의 기본 이동 소켓 규격이다.

## Type 0

```text
L/R 중 최소 하나가 Closed
U/D = 자유
```

- 좌우 완전 관통이 불가능한 선택/폐쇄 섹터다.
- 실제 L/R/U/D 마스크는 별도 데이터로 가진다.
- 필수 진행의 유일한 연결로 사용하지 않는다.
- 보물, 희귀 도구, 특수 아이템, 설계도 조각, 비밀 상점 등에 사용한다.

## Type 1

```text
L = Open
R = Open
U = Closed
D = Closed
```

기본 수평 관통 섹터다.

## Type 2

```text
L = Open
R = Open
U = Closed
D = Open
```

- 도구 없이 아래 방향으로 지나갈 수 있다.
- 필수 하강 분기를 만든다.

## Type 3

```text
L = Open
R = Open
U = Open
D = Closed
```

- 도구 없이 위 방향으로 올라갈 수 있다.
- 필수 상승/재합류를 만든다.

## 필수 수직 연결

필수 수직 간선은 아래 쌍으로만 만든다.

```text
Upper Sector: Type 2.D
        ↕
Lower Sector: Type 3.U
```

---

# 3. 필수 진행망

다음은 모두 도구 없이 `Type 1·2·3` 기본 그래프만으로 접근 가능해야 한다.

- Start
- 모든 필수 핵심 바이옴의 SpecialMap
- 모든 필수 핵심 자원 이벤트
- 열쇠/인장 제작 시설
- 주요 마을 입구
- 보스 봉인지 진입 지점
- 스테이지 완료에 필수인 모든 이벤트 트리거

필수 진행 검증 상태에서는 아래를 모두 `0개`로 가정한다.

- 곡괭이
- 삽
- 로프
- 폭약 연료
- 모든 전지
- 특수 아이템

Type 0을 제거한 그래프에서도 필수 진행이 100% 가능해야 한다.

---

# 4. 필수 수평 경로의 종단

Type 1·2·3은 L/R이 모두 열려 있으므로 일반 섹터에서 필수 수평 run을 막아 끝내지 않는다.

유효한 종단은 아래뿐이다.

- Reserved SpecialMap entry adapter
- Village entry adapter
- Forge/제작 시설 adapter
- Boss site adapter
- 다른 필수 run으로 이어지는 예약 site 내부 junction
- 별도로 정의된 월드 경계 종단 adapter

금지:

```text
Type1 -> Type1 -> Wall
Type1 -> Type0 dead-end
Open Socket -> World Outside
Open Socket -> Inactive/No Socket Cell
```

---

# 5. 반복 바이옴 패치

같은 BiomeType은 한 월드에 여러 패치로 반복될 수 있다.

패치 종류:

- `CorePatch`: 해당 바이옴의 필수 SpecialMap을 포함하는 핵심 패치
- `SatellitePatch`: 일반 자원/선택 콘텐츠를 제공하는 반복 패치
- `BoundarySector`: 두 바이옴의 전환 구간
- `SpecialMap`: 핵심 자원/제작 시설/보스 등 기능이 예약된 지형

## CorePatch 보장

스테이지에 선택된 모든 핵심 BiomeType은 최소 하나의 CorePatch를 가진다.
해당 CorePatch에는 필수 SpecialMap이 최소 하나 존재한다.

SpecialMap은 바이옴을 다 만든 뒤 빈칸에 삽입하지 않는다.

순서:

```text
Special Site 예약
-> CorePatch seed 확정
-> CorePatch 성장
-> SatellitePatch 추가
-> Boundary 생성
```

## 패치 크기

- 일반 패치 최소: 2 sectors
- CorePatch 최소: SpecialMap footprint + 인접 일반 바이옴 완충 1 sector
- 동일 바이옴 단일 패치 최대: 전체 169 sectors의 35%
- 1 sector 패치는 의도된 소형 랜드마크/TunnelIntrusion만 허용
- 같은 바이옴 덩어리가 월드 한쪽 절반을 과도하게 독점하지 않게 분산 검증한다.

---

# 6. 바이옴 경계

허용 Boundary Profile:

- SoftBlend
- CliffBoundary
- TunnelIntrusion
- LayerBoundary
- RuinBoundary
- HardStarstone

원칙:

- 경계는 단순 직선 스킨 변경이 아니다.
- 하나의 패치 전체 경계를 같은 타입 하나로만 도배하지 않는다.
- 필수 경계 통과는 도구를 요구하지 않는다.
- 경계 도달 전 타일/배경/자원/오디오 중 최소 2개로 다음 바이옴을 예고한다.
- HardStarstone은 월드 외곽, SpecialMap 보호, 소프트락 방지에 우선 사용한다.

---

# 7. 월드 사용과 이동 거리

필수 자원 수집과 제작/보스 진입을 포함한 상태 기반 완료 경로 목표:

```text
Minimum Completion Travel : 500~900 tiles
Normal Completion Travel  : 800~1400 tiles
Optional Exploration      : 1500~2800 tiles
Repeated Corridor Ratio   : <= 35%
```

단순 Start-Boss 직선거리가 아니라 아래 상태 전이를 포함한다.

```text
Start
-> Required Resource A
-> Required Resource B
-> Required Resource C
-> Crafting Site
-> Key/Seal Complete
-> Boss Site
```

거리 목표를 채우기 위해 같은 복도를 억지로 왕복시키지 않는다.

---

# 8. 주요 마을의 맵 규칙

스테이지마다 주요 마을 1개를 보장한다.

- Start 고정 아님
- 방문하지 않아도 완주 가능
- Type 1·2·3 기본 이동으로 접근 가능
- SpecialMap footprint와 겹치지 않음
- 보스 봉인지 내부 금지
- 발견 후 지도에 영구 표시

Start 기준 그래프 거리 가중치:

```text
2~3 sectors  : 20%
4~6 sectors  : 50%
7~10 sectors : 30%
```

마을 크기:

```text
Standard   = 1×1 sector
Horizontal = 2×1 sectors
Vertical   = 1×2 sectors
Max        = 2 sectors
```

시설은 5~6개이며 반드시 포함:

- 음식/공용 부엌
- 도구 수리점

마을의 세부 경제/NPC 평판 구현은 관련 TASK가 열리기 전까지 맵 생성기에서 구현하지 않는다.

---

# 9. 생성 순서의 상위 원칙

상위 단계 순서는 아래를 따른다.

```text
Fixed World Grid
-> Required Site Reservation
-> Biome Core/Satellite Patches
-> Mandatory Type 1/2/3 Network
-> Type 0 Optional Overlay
-> MicroChunk Authoring/Selection
-> Boundary Chunk Resolution
-> Sector Assembly
-> SpecialMap/Village Assembly
-> Tilemap Bake/Streaming
-> Spawn Population
-> Validation
```

앞 단계의 데이터 오류를 뒤 단계가 임의 후처리로 보정하지 않는다.

---

# 10. 결정적 생성

- 모든 생성기는 명시적 Seed를 받는다.
- 같은 `Seed + DataVersion + GeneratorVersion`은 같은 결과를 생성한다.
- 하나의 RNG 스트림을 모든 단계가 공유하지 않는다.
- 단계별 RNG stream을 분리한다.
- 후보 선택 전 Stable ID로 정렬한다.
- CSV 행 순서가 랜덤 결과를 바꾸면 안 된다.
