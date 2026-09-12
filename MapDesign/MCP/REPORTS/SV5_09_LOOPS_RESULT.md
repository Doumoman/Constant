# SV5_09_LOOPS Result

TASK: SV5_09_LOOPS
TASK_ID: SV5_09_LOOPS
STATUS: PASS

## 구현 결과

production `PlanWithInfill`의 실제 방·가지 셀에서 서로 다른 endpoint를 선택해 1×1 타일 중심선 loop를 생성했다. 각 중심선은 양 끝 포함 4~24칸, 보행 AIR 2칸과 SOLID 지지, cardinal 이동 및 지지된 +1 상승만 허용한다. 기존 SOLID 입구는 provenance가 있는 aperture override로만 열며 기존 Infill 객체와 역사적 산출물은 변경하지 않는다.

각 후보를 실제 physical movement union에 하나씩 넣어 모든 합법 FSM 상태와 6개 resource order를 검증한다. 직접 parent-child, 동일 출입구, 보호/예약/core 침범, 외부 AIR 의존, stub, 중복 및 자기교차는 제외한다. 새 연결을 제거해도 기존 경로가 남고, 연결 하나마다 cycle rank가 정확히 1 증가한다. 기존 동일 상태 최단거리보다 엄격히 짧은 연결만 `RANDOM_SHORTCUT`으로 분류한다.

## 고정 맵 결과

| profile | candidates | eligible | accepted | rejected | sectors | max inclusive | LOOP | RANDOM_SHORTCUT | cycle delta | bypass | dangling | duplicate |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| default | 259 | 79 | 18 | 241 | 14 | 24 | 18 | 0 | 18 | 0 | 0 | 0 |
| repeat | 429 | 140 | 17 | 412 | 13 | 24 | 17 | 0 | 17 | 0 | 0 | 0 |

default rejection counters는 `DUPLICATE_ENDPOINT_PAIR=61`, `EXTERNAL_AIR_DEPENDENCY=820`, `FOREIGN_SOLID_OPENING=83`, `INELIGIBLE_HASH=180`, `SUPPORT_NOT_SOLID=4822`다. repeat는 각각 `123`, `750`, `85`, `289`, `5182`이며 `PROTECTED_OR_RESERVED=7`이 추가된다. 한 후보가 여러 cell-level 거부 사유를 가질 수 있어 사유 합계는 rejected candidate 수와 동일하지 않다.

default plan/loop digest는 `208044b6478412daf2bad89002ce662f66af6b2f7a9c907a0071786d735e4e4d` / `a2c2aba25e18f03f8e9948f0e2956a191f5ff1fe726447e9285264f6b475258f`다. repeat는 `1e6b72cdd899037faa6eca523eb247245b83a2238d7e72a8406e27f2fad5c603` / `f8678f45677a5e4ee41863fd7528af0d64484d92b5fc0e8aef80e0cbd170f826`다.

`loop_validation.json`의 L01~L18은 두 profile 모두 PASS다. 독립 `check_loops.py`도 final CSV/JSON을 재구성해 `PASS_INDEPENDENT_LOOPS`를 반환했다. 감사 파일 SHA-256은 `dbe083b47b1e3d24329f6dd4aa224e7da45a04006e6817de9c3bb43a5db4f9be`, 1,428 bytes다.

## Unity 시험

최종 명령:

`unity test . --mode EditMode --filter StarNight.Map.Tests.EditMode.Sv5 --output MapDesign/MCP/GENERATED/SV5_09_LOOPS/focused_results.xml --timeout 1800 --format json`

- 발견/실행/통과: 116 / 116 / 116
- 실패/스킵/inconclusive: 0 / 0 / 0
- 기존/신규: 102 / 14
- 실행 시간: 1483.8076675초
- 실행 시각: 2026-09-12T06:25:18Z ~ 2026-09-12T06:50:01Z
- XML raw SHA-256: `dd0df8e3da04bf4cdeb645e12061c721d45b0eef8207ac6aef896cb8cd0d763d`, 117,753 bytes
- XML Git LF blob SHA-256/OID: `ad902f85cba9241e5421c4fa20e2bb8577084a5706700cd37ec1b5b245336d0e` / `25d40b2346906105b17720e232d1bc9426e116cd`
- 최종 tested source SHA-256: `70a4f2e15f0ed6f3425080ab11639443af47badecb6d8194c6949800d28934be`

상태 표기 복구 전후 코드·생성 결과·XML·독립 감사는 변경되지 않았다. 복구 후 pre-Finalize post-readonly는 CURRENT 253/1/38, 911개 source pin과 149개 commit blob에서 `PASS_READONLY_LOCAL_AND_GIT`이었다. 따라서 기존 최종 116/116 증거를 재사용했으며 전체 회귀를 다시 실행하지 않았다.

## 범위와 준비도

loop runtime은 현재 `PlanWithInfill` 결과에만 의존하며 레거시 구현 의존성을 추가하지 않았다. 이전 Generated는 회귀 비교를 위한 읽기 전용 증거로만 사용했다. 실제 scene geometry 합성이나 Player 완주를 주장하지 않는다.

- ComposedGeometryReady=false
- PlayerVerified=false
- SV5_10_SIDEPATH는 LOCKED 유지
- push 및 SV5_10 작업 없음

## commit 및 Review 증거 정책

이 Result는 native Finalize와 atomic commit 전에 고정되는 불변 증거다. parent는 `483fb4b56ec46ae764d5634f17e39eb19b3e8cfa`다. 실제 final commit/parent와 각 entry의 Git blob OID·SHA·bytes는 commit 후 `_REVIEW_MANIFEST.json`에 기록하고, Review ZIP SHA-256과 함께 최종 CLI 보고에 남긴다. Result에 자신의 commit 또는 ZIP SHA를 사후 삽입해 순환 hash/amend를 만들지 않는다.
