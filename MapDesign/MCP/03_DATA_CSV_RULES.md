# CSV·데이터 원본 규칙 v1.0

# 1. Source of Truth

사람이 작성하는 정적 맵 데이터의 원본은 CSV다.

```text
CSV Authoring Data = Source of Truth
ScriptableObject   = Import Cache / Editor Preview
Generated CSV      = Seed Output / QA Result
SaveData           = Runtime Mutation
Prefab/Tile Asset  = Visual/Collision Asset
```

ScriptableObject나 Prefab에 배치 규칙을 중복 저장하지 않는다.

---

# 2. CSV 스키마 변경 금지

일반 구현 TASK에서는 다음을 하지 않는다.

- 컬럼 추가
- 컬럼 삭제
- 컬럼 이름 변경
- 의미 변경
- ID 규칙 변경
- 외래키 대상 변경

필요하면 아래 형식의 별도 제안 파일만 작성한다.

```text
SPEC_CHANGE_PROPOSAL
- 대상 CSV:
- 변경 이유:
- 제안 컬럼:
- 기존 데이터 영향:
- 마이그레이션 필요 여부:
```

사용자의 승인 전에는 적용하지 않는다.

---

# 3. ID

- 모든 정적 데이터는 안정적인 명시적 ID를 가진다.
- index/row number를 ID로 사용하지 않는다.
- ID는 저장 후 임의 rename하지 않는다.
- 참조는 문자열 ID 또는 명시적 typed ID로 수행한다.
- 외래키가 끊어지면 import 실패로 처리한다.

---

# 4. CSV 순서와 RNG

CSV 행 순서는 의미가 없어야 한다.

후보 추첨 절차:

```text
1. 조건 필터
2. Stable ID ascending sort
3. 단계 전용 RNG stream으로 선택
```

CSV 행 순서를 바꿨는데 같은 seed 결과가 달라지면 버그다.

---

# 5. 정적 입력과 생성 결과 분리

정적 Authoring CSV와 Generated Output CSV를 같은 폴더에 두지 않는다.

Generated Output 예:

- seed_manifest.csv
- generated_world_sectors.csv
- generated_world_edges.csv
- generated_biome_patches.csv
- generated_special_sites.csv
- generated_sector_microchunks.csv
- generated_spawns.csv
- generated_validation_results.csv

Generated Output을 다시 Authoring 원본처럼 수정해서 사용하지 않는다.

---

# 6. MicroChunk 데이터

하나의 MicroChunk:

```text
12 × 8 = 96 tile cells
```

`microchunk_tile_cells.csv`에서 각 chunk는 정확히 96개의 unique local coordinate를 가져야 한다.

허용 범위:

```text
local_x = 0..11
local_y = 0..7
```

금지:
- 좌표 누락
- 좌표 중복
- 범위 초과
- 90도 회전으로 12×8을 8×12처럼 취급

좌우 반전 등 허용 변형은 별도 variant rule로 명시한다.

---

# 7. Sector Recipe 데이터

하나의 48×32 Sector:

```text
4 × 4 MicroChunk cells = 16
```

SectorRecipe의 4×4 좌표는 정확히 16개를 가져야 한다.

---

# 8. Socket / Edge 데이터

외부 소켓은 방향, 위치 band, signature가 명시되어야 한다.

두 청크/섹터를 연결할 때:
- 방향이 반대여야 한다.
- EdgeSignature compatibility를 통과해야 한다.
- 필수 route라면 traversal validation을 통과해야 한다.

"비슷해 보이므로 연결" 같은 런타임 추측은 금지한다.

---

# 9. Validation

CSV import 시 최소 검증:

- Duplicate ID
- Missing Foreign Key
- Invalid Enum
- Required Field Empty
- Numeric Range
- MicroChunk 96 cells
- SectorRecipe 16 cells
- Socket direction validity
- Type 0/1/2/3 mask validity
- Battery/Resource 등 관련 TASK가 열렸을 때 해당 도메인 invariant

ERROR가 있으면 해당 데이터셋을 런타임에 사용하지 않는다.

---

# 10. Encoding / Parsing

- CSV는 UTF-8 기준으로 취급한다.
- 프로젝트 기존 CSV convention이 audit에서 확인되면 그 규칙을 우선한다.
- locale-dependent float parsing 금지
- 숫자는 invariant culture로 파싱한다.
- bool/enum 파싱 실패를 silent default로 숨기지 않는다.
