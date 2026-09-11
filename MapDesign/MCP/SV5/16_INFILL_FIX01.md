# SV5_08_FIX01 — 포괄 연결 중심선 교정

## 결론

SV5_08의 연결 길이는 `ExternalCenterline` 소유 셀 수가 아니라, 부모측 시작점부터 자식 입구까지 순서대로 이어지는 **양 끝점 포함 cardinal AIR 중심선 셀 수**로 판정한다. 새 정책 ID는 `SV5_INFILL_LENGTH_RULE_V2`이며 상한은 24셀이다. 기존 4×4 쓰기 마스크, 12×8 청크, 624×416 월드, 바닥 지지와 +1 상승/최대 2칸 여유 규칙은 변경하지 않았다.

## 원인과 수정

기존 구현은 `HostAccess.Concat(Path).Distinct()`를 구성한 뒤 새 방의 소유 셀만 남겼다. 그 결과 부모측 시작 셀과 자식 입구 셀이 소유권 필터에서 빠지고, 실제 25~26셀인 연결이 23~24셀로 축소 보고됐다. 또한 후보 예산에서 양 끝점을 할인해 같은 오류가 생성 단계에도 들어갔다.

FIX01은 다음 하나의 production 규칙을 생성, 최종 검증, export가 함께 사용하게 했다.

- root 연결: `HostAccess + Path.Skip(1)`
- child 연결: `Path`
- 순서를 보존하며 `Distinct`로 재방문을 숨기지 않음
- 첫 셀과 자식 `Entry`를 모두 포함
- 모든 연속 셀은 Manhattan 거리 1
- 빈 경로, 잘못된 접합, 대각 이동, 끝점 불일치, 24셀 초과를 오류로 판정

정책 ID와 authoritative 중심선은 profile/plan/link digest에 결합된다. `ExternalCenterline`은 하위 호환용 소유 셀 진단으로만 보존했다.

## 고정 맵과 export

고정 default 맵은 새 방 217개, 새 소유 타일 62,806개이며 최대 연결 길이는 24셀이다. repeat 맵은 새 방 234개, 새 소유 타일 67,625개이며 최대 연결 길이는 24셀이다. 두 맵 모두 초과, 끝점, 비-cardinal 오류가 0이고 `CANDIDATES_EXHAUSTED`로 종료된다. 목표 수와의 차이는 각각 39개와 22개이며, 24셀 규칙을 완화해 채우지 않았다.

`infill_links.csv`는 기존 열을 유지하면서 `connection_centerline`, `connection_cell_count`, `length_policy`, `length_status`를 추가한다. 새 연결은 `PASS`, 내부 경로를 복원할 수 없는 legacy 6개만 `NOT_APPLICABLE_LEGACY_INTERIOR`로 명시한다. 독립 Python audit도 CSV에서 중심선을 다시 계산한다.

`preview/before_after.svg`와 `repeat_before_after.svg`는 불변 SV5_08 CSV의 실제 셀과 FIX01 실제 셀을 624×416 전체 범위에서 비교한다. `preview/detail.svg`는 기존 26셀 반례 좌표 창을 1×1 격자와 4×4 경계로 확대하고, 기존 거부 경로와 현재 연결을 구분한다. A1~D4는 FIX01의 실제 셀을 16개 구역으로 나눈 현재 상태 확대도다. UNKNOWN/미배치 셀은 완성 지형처럼 표시하지 않는다.

## 레거시 읽기 전용 조사

`legacy_inventory.json`은 현재 `PlanWithInfill` 생산 체인, PhysicalMovement/Product/StateProjection, RMAP13 및 core/Type0/RMAP15/16 입력 소비자, 테스트/export helper의 과거 증거 참조를 읽기 전용으로 추적한다. 분류 결과는 ACTIVE 7, COMPAT 4, HISTORICAL 5, RETIRE_CANDIDATE 0, UNKNOWN 0이다. 이번 Task는 레거시 파일의 이동·삭제를 승인하지 않으므로 실제 소비자가 있는 항목은 보존했고, 향후 격리 판단은 별도 승인 작업으로 남긴다.

## 검증 범위와 한계

F01~F07은 23/24 경계 허용과 25/26 거부, root/child 끝점, 계단 전개와 접합, 기존 26셀 반례, default/repeat 전수 재구성, export/digest 결합, 기존 95개 시험의 발견·실행 보존을 검사한다. 최종 focused 실행 결과와 XML SHA는 Result 및 `BINDING.json`을 정본으로 한다.

이 결과는 좌표 그래프와 알려진 고체/공기 셀에 대한 계획·정적 검증이다. `ComposedGeometryReady=false`, `PlayerVerified=false`이며 실제 지형 합성과 플레이어 완주 검증을 주장하지 않는다. `SV5_09_LOOPS`는 LOCKED로 유지한다.
