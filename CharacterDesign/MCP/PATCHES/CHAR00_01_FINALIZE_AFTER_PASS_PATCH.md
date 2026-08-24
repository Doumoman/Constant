# CHAR00_01 FINALIZE AFTER PASS PATCH

PATCH ID: CHAR00_01_FINALIZE_AFTER_PASS  
PATCH TYPE: FINALIZE  
TARGET TASK: CHAR00_01  
NEXT TASK CANDIDATE: CHAR00_02  
APPLY ORDER: 1 of 2  
DOES NOT OPEN NEXT TASK: true

## 판정

`CHAR00_01`은 최종 PASS로 판정한다.

근거:

- `CharacterDesign/MCP/RESULTS/CHAR00_01_INVENTORY_CHARACTER_INPUT_PHYSICS_MAP_RESULT.md`에 독립된 상태 줄 `STATUS: PASS`가 있다.
- `CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md`의 `REGISTRY_STATE`가 `FILLED_BY_CHAR00_01`이다.
- 실제 변경 파일은 `CHAR00_01` WRITE ALLOWLIST 안의 2개 문서로 제한되어 있다.
- `Assets/**`, `Packages/**`, `ProjectSettings/**`, `MapDesign/**` 변경은 본 작업 범위에서 발생하지 않았다고 RESULT가 보고한다.
- 조사에서 남은 BLOCKER 3건은 `CHAR00_01` 실패 사유가 아니라 `CHAR00_02` 이후 계약 고정·OPEN 범위에서 다룰 후속 결정 사항이다.

## 적용 전 필수 확인

1. `CharacterDesign/MCP/RESULTS/CHAR00_01_INVENTORY_CHARACTER_INPUT_PHYSICS_MAP_RESULT.md`가 존재한다.
2. 위 RESULT에 정확히 독립된 줄 `STATUS: PASS`가 있다.
3. `CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md`가 존재한다.
4. registry에 정확히 `REGISTRY_STATE: FILLED_BY_CHAR00_01`가 있다.
5. `git status --porcelain -- Assets Packages ProjectSettings MapDesign`에서 본 작업 신규 변경이 없다.
6. `CHAR00_01` PASS 확인 후 커밋을 만든다. 권장 커밋:

   제목:

   `CHAR00_01: 캐릭터·입력·물리·카메라·MAP 접점 조사`

   본문 포함 항목:

   - 활성 캐릭터 런타임 부재 확인
   - 레거시 캐릭터 선례 범위
   - 입력 계약 불일치(X/Z/C vs 레거시 E/F/Q)
   - MAP 좌표 계약 존재와 캐릭터용 MAP API 부재
   - 변경 파일 2개
   - 테스트 5/5 PASS
   - 후속 BLOCKER 3건

커밋을 만들 수 없으면 이 FINALIZE 패치를 적용하지 말고 BLOCKED로 보고한다.

## 허용 수정 범위

- `CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md`
- `CharacterDesign/MCP/PATCHES/CHAR00_01_FINALIZE_AFTER_PASS_PATCH.md`

## 금지

- `CHAR00_02`를 CURRENT로 변경하지 않는다.
- `CHAR00_02` TASK 파일을 이 패치에서 수정하지 않는다.
- `Assets/**`, `Packages/**`, `ProjectSettings/**`, `MapDesign/**`를 수정하지 않는다.
- git push를 수행하지 않는다.

## 상태 파일 변경

`CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md`를 아래 내용으로 교체한다.

```md
# Character Implementation Status

schema_version: 1
harness_version: 1.3
current_phase: CHAR00
current_task: NONE
last_finalized_task: CHAR00_01
next_task_candidate: CHAR00_02

requires_result:
  path: NONE
  exact_status: NONE

transition_policy:
  finalize_opens_next: false
  open_requires_separate_patch: true
  fail_keeps_current: true
  blocked_keeps_current: true
  commit_after_pass: true
  push_requires_user_instruction: true

task_states:
  CHAR00_01: COMPLETE
  CHAR00_02: LOCKED
  CHAR00_03: LOCKED
  CHAR01_01: LOCKED
  CHAR01_02: LOCKED
  CHAR01_03: LOCKED
  CHAR01_04: LOCKED
  CHAR02_01: LOCKED
  CHAR02_02: LOCKED
  CHAR02_03: LOCKED
  CHAR03_01: LOCKED
  CHAR03_02: LOCKED
  CHAR03_03: LOCKED
  CHAR04_01: LOCKED
  CHAR04_02: LOCKED
  CHAR04_03: LOCKED
  CHAR04_04: LOCKED
  CHAR05_01: LOCKED
  CHAR05_02: LOCKED
  CHAR05_03: LOCKED
  CHAR05_04: LOCKED
  CHAR05_05: LOCKED
  CHAR06_01: LOCKED
  CHAR06_02: LOCKED
  CHAR06_03: LOCKED
  CHAR06_04: LOCKED

## 상태 변경 규칙

이 파일은 별도 FINALIZE 또는 OPEN 패치에서만 수정한다. 구현 패치가 CURRENT를 완료 처리하거나 다음 작업을 열 수 없다.
```

## 적용 후 확인

- `current_task: NONE`
- `last_finalized_task: CHAR00_01`
- `next_task_candidate: CHAR00_02`
- `CHAR00_01: COMPLETE`
- `CHAR00_02: LOCKED`
- CURRENT 작업 수 0개

## 다음 단계

이 패치가 적용된 뒤, 별도 패치 `CHAR00_02_OPEN_AFTER_CHAR00_01_PATCH.md`를 적용해 `CHAR00_02`를 CURRENT로 연다.
