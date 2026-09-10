# SV5_06_FIX02 Result

TASK: SV5_06_FIX02
STATUS: PASS

## 선행·등록·Apply

- manifest SHA-256: `955e37751fb515300b4c8eac69a300124074959c3f6ea82adc65631626bb1974`
- 선행 SV5_06_FIX01 Result SHA-256: `ed7f1f81911c5209eddcb3cbffd7f6aeee5bb246c08e5588d7ff3534bca9de5c`
- 선행 installed Task SHA-256: `e7695992c081b856fc2e05757cc68fa8121178a83b6b1c3e352d4ec172e74276`
- 선행 소유 commit / parent: `de57f7b1454d2b138bda9915bdb03ad1ae09cbe3` / `7b7dc99527b878eadc398388e27b3fce173971ef`
- 등록 전: 246 COMPLETE / 0 CURRENT / 41 LOCKED / Current NONE
- 등록 후: 246 COMPLETE / 0 CURRENT / 42 LOCKED / Current NONE
- Apply 후: 246 COMPLETE / 1 CURRENT / 41 LOCKED / Current `TASKS/SV5_06_FIX02.md`
- 등록 Status SHA: `90f0189def2c85346ae87b1df678f67ab1bc886c2ba82eb4e156da089e6d047c` → `e834c9a2adbd1edacb750052602332a96a06b9e7a28e77eaf9447ad4e05f3578`
- 등록 Master SHA: `083c324fe00531b47f140048f2b1729db2bdadaf9c880560d708f2ba95cc5b03` → `882675204091f954c2c2e06a4196574a7202b403751ae3cdbc29d385da46322e`
- Task / Archive SHA-256: `149d300e2010a08d9f399a94e8fdfa6289e12752e6af332b5115b89cd6c0ba5d`
- `pre-registration`, `pre-stage`, `stage`, Apply 뒤 `post-readonly`: 모두 PASS
- 등록 diff는 문서 지시대로 `core.autocrlf=false`, `core.eol=lf`로 검사·적용했다. 기대 SHA, package bytes, 기존 XML 줄바꿈은 수정하지 않았다.

## 구현 결과

- 서로 다른 route predicate의 접촉을 하나의 AND/OR gate로 합치지 않는다. typed predicate가 같은 접촉만 shared split join이고, 다른 접촉은 계획상 separated boundary다.
- RMAP13 guard가 있는 세 실제 core connector에 route-owned full-width face gate를 각각 하나씩 배치했다. source connection/route/port, target port, typed resource/Forge/Seal/Boss predicate, 방향과 흐름, anchor와 cardinal face cut을 고정했다.
- 승인된 connection envelope만 통행 공간으로 사용하고 `INFILL_PENDING`을 AIR로 간주하지 않는다. 모든 관련 gate의 동시 상태를 소비해 SEALED cut, OPEN path, source anchor, target port를 탐색한다.
- W01은 `mask=7 / Forge=true / Seal=false / Boss=false`에서 Seal 진입이 OPEN이다. W02는 `mask=7 / Forge=true / Seal=true / Boss=false`에서 Boss 접근은 OPEN, Exit는 SEALED다.
- gate blocking cell을 사용하지 않고 face boundary로 차단해 FIX01의 `(533,311..313)` 및 정본 OPEN `(560,311..313)` port 셀 재점유를 제거했다.
- ConditionalGate의 ProtectedAir cell 소유를 생산 충돌 검사에서 거부한다. `FACE_ONLY_STATE_BOUNDARY`는 cell을 점유하지 않으므로 허용한다.
- connector 생산 검증기는 unknown port, endpoint, flow/direction, full width와 함께 port에서 연결되지 않은 aperture를 거부한다.
- projection segment는 검증된 gate ID를 source edge guard에 결합한다. 실제 RMAP13 자원 6순서에서 goal 도달과 모든 reachable state의 정상 역도달을 재검증했다.

## G01–G08와 focused EditMode

- G01/G02: FIX01 W01/W02를 원본 검토 바이트로 재현하고 새 상태별 port 접근/차단을 확인했다.
- G03: 전체 폭 face 중 일부만 남긴 mutation에서 `GATE_STATE_SIDE_BYPASS`가 실제 탐색으로 발생하고 채택 cut은 통과한다.
- G04: ProtectedAir cell gate는 실패하고 face-only state boundary와 채택 plan은 충돌 0이다.
- G05: production connector 경로가 unknown port, invalid flow/direction, 분리 aperture를 거부한다.
- G06: 실제 connector 삭제와 optional ONE_WAY 반례가 실패하고, 채택 plan의 6순서/역도달/dead-end 0은 통과한다.
- G07: port boundary, flow, envelope, gate predicate, OPEN/SEALED semantic mutation은 각각 digest를 바꾸고 집합 열거 순서만 바꾸면 동일하다.
- G08: 하나의 accepted plan이 JSON/CSV/19 SVG preview를 생성하며 기존 FIX01 Result/Task/generated SHA를 보존한다.
- Unity Editor: `6000.3.8f1 (1c7db571dde0)`; Unity CLI: `1.0.0-beta.9`
- 명령: `unity test . --mode EditMode --filter "StarNight.Map.Tests.EditMode.Sv5" --output MapDesign/MCP/GENERATED/SV5_06_FIX02/focused_results.xml --timeout 1800 --no-color`
- 발견/실행/통과/실패/스킵: 51 / 51 / 51 / 0 / 0
- XML raw SHA-256 / bytes: `d938215bae8766ba45ddadebe1b87642d579c7c52b78d7249b13669025b3eafc` / 48,793
- 무필터 전체 회귀, PlayMode, build, Scene Bake, Player 검증은 실행하지 않았다.

## 채택 계획과 증거

- plan digest: `e31b49b72f3c90857ac4ebce75ffab1cfef27a40b7b65a87e9d518a07226b905`
- 28 connections / 16,222 contact input cells / 7,188 complete contact pairs / 3 route-owned conditional gates / 9 gate state checks / 6 projection orders
- contact coverage 오류 0 / reservation 충돌 0 / gate geometry 오류 0 / gate state 오류 0 / diagnostics 0
- `ComposedGeometryReady=false`, `PlayerVerified=false`
- BINDING SHA-256: `fd3cd9c5360354cf171382a8c213b3fbe75518627ee8d3351eb9ce57b9300f56`
- `space_graph.json`: `1494100f3e35f0cd44ba67d3641bfd90940a5b3cac4daff8b7b656659f2f0100`
- `reservation_cells.csv`: `8bb24230f88e63d2a8855fbda2bc4c6345637df7d68f9a386959687ea096fe05`
- `contact_checks.csv`: `9f1a09897682f5874553aa994fa46173e0357320062dc73884b2ee22598c88c8`
- `gate_geometry.json`: `0cab19392162c1dc8aae6dc78295a2719c37e488fdf4068021ad926380d49446`
- `gate_state_checks.json`: `2cdf4f071c57b8bef9cbc04b8cd5f0c1c2a77c9186e062669b52b2125873c413`
- `state_proofs.json`: `a697385a66cae10498dd7550242ad1d47dbd64dc3b5da3dcc57b7508a55fadf4`
- `validation.json`: `c1c90dda82d3dd1854f0e9d4c86bb1d98100c1599b9e748a6e4a2a8f2a7231cd`
- `obligations.csv`: `b4757449d2669405b61fd7af9e17ddc04f3b29897721a826e667c1eee040a98e`
- 활성 설명 `SV5/12_SPACE_GATE_FIX02.md`: `b8e3691ac3d0e36d46aa6974cbfeefd9b2ba04db9874acd97f984a4f329f9249`
- 제공 protocol suffix SHA-256 `6781feb17f4994bf04aa17464d6e7d041d3a7f70a81aa404a51248ca6dbd8dc7`가 `SV5/02_PROTOCOL_V5.md` 끝에 정확히 1회 존재한다.

## 남은 책임과 중지

22개 장소와 큰 `INFILL_PENDING`의 Family 분포·일반 공간 밀도·재합류는 기존 순서대로 SV5_07/08/09가 담당한다. 합성 geometry와 Player 검증은 여전히 별도 후속 책임이다. SV5_07 이후 작업은 열거나 실행하지 않고, push도 수행하지 않는다.
