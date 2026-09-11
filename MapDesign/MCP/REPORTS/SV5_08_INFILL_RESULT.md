TASK: SV5_08_INFILL

STATUS: PASS

# SV5_08 실제 일반 공간 셀 결과

## 결과 요약

SV5_07의 default/repeat ON 기준선을 보존한 채 624×416 월드의 미소유 공간에 실제
SOLID/AIR 셀로 된 EMPTY_ROOM, SMALL_CAVE, LANDING, DEAD_END와 부모 연결 통로를 생성했다.
생산 진입점은 `Sv5SpaceGraphPlanner.PlanWithInfill`이며, `Sv5SpaceInfill.Capture`가 과거 plan에서
좌표·예약·place·connection 사실만 immutable 입력으로 분리한다. 새 성공 판정은 과거 success
flag, proof 개수 또는 빈 컬렉션 assertion을 소비하지 않는다.

외부 연결 길이는 사용자가 확정한 대로 상하좌우 cardinal AIR 중심선 셀의 합이다. +1 계단은
가로 AIR와 세로 AIR를 각각 세며 최대 24셀이다. 생성기는 한 번의 결정적 후보 queue를 목표
도달 또는 실제 후보 고갈까지 처리한다. 같은 월드를 반복 생성하여 이전 leaf를 금지하는
방식은 제거했다.

## 두 고정 사례

| 항목 | default ON | repeat ON |
|---|---:|---:|
| 기준선 digest | `10080e53c3d4c47f6e93f49118c7c648f07c1f3162742ce866b6ecd8a0be3e46` | `94c9f3a353e39984633a755af7842e87989e86dc97c3545ce87a4ac4da4397b7` |
| 결과 plan digest | `ec2e35e4ffb165bdd113c03108582c40d55bf1104b554fc339a8a251385dc465` | `82c31034ea601389a257e773caf96fe146abde1b3016018ff3aeae60115f73eb` |
| infill digest | `0cd216b04eada31b29d0f0d781cb46b06173e6cd4f1a455746eb49706b06f951` | `e2d11ee1c11539b14e3b3c120e7866de5f40e723c7876ecb54264817a0382472` |
| 신규 방 | 213 | 232 |
| 신규 고유 셀 | 64,409 | 70,266 |
| 16구역 방 수 | `22,13,7,19,20,13,2,13,20,13,3,9,20,16,12,11` | `22,13,7,19,19,13,2,13,19,10,9,10,25,22,18,11` |
| recipe 분포 | DEAD_END 65, EMPTY_ROOM 68, LANDING 37, SMALL_CAVE 43 | DEAD_END 68, EMPTY_ROOM 71, LANDING 43, SMALL_CAVE 50 |
| 기존 ordinary 완성 | 6 | 6 |
| 최대 외부 AIR 중심선 | 24 | 24 |
| 종료 | `CANDIDATES_EXHAUSTED`, 목표 미달 43 | `CANDIDATES_EXHAUSTED`, 목표 미달 24 |
| 6×6 전체/완전 known/mixed pending | 254,409 / 26,369 / 228,040 | 254,409 / 29,131 / 225,278 |

두 사례 모두 최소 128방, 최소 고유 면적, 요구 분포와 모든 recipe 사용을 충족한다. 목표 256은
필수 최소가 아니며, 미달을 숨기거나 profile을 낮추지 않고 실제 후보 고갈과 shortfall로
기록했다. CELLS, PATTERNS, STATIC_SCREEN, CONTACT, PHYSICAL_PRODUCT는 모두 true이고 solid
6×6 실패는 0이다. 기존 place 24개, core, port, ordered centerline, gate geometry/predicate와
두 기준선 OFF digest는 보존됐다.

## 실제 검증

Unity 명령:

```text
unity test . --mode EditMode --filter StarNight.Map.Tests.EditMode.Sv5 --output MapDesign/MCP/GENERATED/SV5_08/focused_results.xml --timeout 1800
```

- Unity: `6000.3.8f1 (1c7db571dde0)`
- 실행: 2026-09-11 16:32:44Z ~ 16:47:07Z, 863.0671594초
- 발견/실행/통과: 95 / 95 / 95
- 실패/스킵: 0 / 0
- 기존 시험 이름: 82개 전부 유지
- 신규 시험: 13개, 계약 T01~T12 책임 전부 발견
- XML: `MapDesign/MCP/GENERATED/SV5_08/focused_results.xml`
- XML raw SHA-256 / bytes: `21d2d31470b6c9a85a74192e3a1dc142460f707aea1dd6368d5b76dc368f2a34` / 99,364
- XML Git-blob SHA-256 / bytes / OID: `f287ee528ff846fb1cdaa6a7bd372137350a62e58b2b8bf3b0fe7c1088ab232e` / 98,621 / `88a1372b4193d99697445cb78041cb63a82c2db0`

T01~T05는 recipe/mirror, 4×4 write mask 재구성, 결정성/후보 고갈, 이동 음성 반례와
보호 셀 침범을 production API로 검사한다. T06~T10은 두 실제 `PlanWithInfill` 결과의 밀도,
부모/host 연결, 실제 좌표 component, 지지면·왕복, gate/contact와 6 resource order × legal
FSM state product, OFF/기존 geometry 보존을 검사한다. T11은 8개 infill export를 다시
읽어 모든 셀과 pattern을 복원하고 셀·profile·개구 변조가 digest 또는 검증에 반영되는지
확인한다. T12는 645개 잠금과 기존 82개 시험 이름, 현재 obligations를 확인한다.

최종 별도 read-only 검사는 다음 명령으로 수행했다.

```text
python -X utf8 MapDesign/MCP/INPUTS/SV5_08/STAGE.py --mode post-readonly --project-root . --expected-manifest-sha b1893ce4937c6800c7cf6ecea6b862d3b1a6beb0c15c91c164cdb8c0c892363d
```

SOURCE_LOCK의 ALWAYS worktree 645개와 선행 commit blob 41개가 모두 일치했다. 작업 시작 시
기록한 비소유 dirty 373개도 mismatch 0, 예상 밖 비소유 변경 0으로 보존됐다.

## 산출물과 검증 한계

`GENERATED/SV5_08/default`와 `repeat`은 각 45개 파일, `preview`는 20개 파일이다. 동일 accepted
payload에서 infill JSON/CSV, pattern, window, validation, digest와 전체 624×416 전후 비교,
A1~D4 16개 확대, 1칸·4×4·12×8 경계 detail을 만들었다. 검은 셀은 확인된 SOLID, 밝은 셀은
확인된 AIR, 청색은 아직 조립되지 않은 기존 passage, 회색/갈색은 미배치·미조립 영역이다.
이번 실행의 `_work` 임시파일 339개(409,744,717 bytes)는 최종 증거 생성 뒤 삭제했고 과거
GENERATED나 사용자 dirty는 삭제하지 않았다.

이 결과는 좌표 셀과 국소 정적 이동 검증이다. 전체 지형 합성과 실제 Player 완주 증명이
아니므로 `ComposedGeometryReady=false`, `PlayerVerified=false`다. 상하좌우 좌표 연결을
Player 검증으로 표현하지 않는다.

## Finalize 시점

이 Result는 SV5_08_INFILL이 CURRENT인 동안 작성했다. 이 문서의 정확한 PASS를 검증한 뒤
native Finalize에서 이 행만 COMPLETE, Current Task를 NONE으로 닫고 이번 Task 소유 파일만
하나의 atomic commit으로 기록한다. 예상 최종 상태는 290행 = 251 COMPLETE / 0 CURRENT /
39 LOCKED이며 `SV5_09_LOOPS`는 LOCKED다. push와 다음 Task는 수행하지 않는다.
