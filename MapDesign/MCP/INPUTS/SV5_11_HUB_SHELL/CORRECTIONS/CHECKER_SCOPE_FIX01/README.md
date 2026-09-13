# SV5_11 CHECKER_SCOPE_FIX01

이 보정은 이미 Apply되어 CURRENT인 `SV5_11_HUB_SHELL`의 원본 INPUTS를 수정하지 않는다.
기존 checker가 `SectorPlanning` 전체를 스캔해 Task 비소유 레거시 `SectorId`를 오탐한 문제만 교정한다.

`VERIFY.py`는 패키지, 원본 checker, 원본 manifest, predecessor HEAD와 CURRENT 상태를 읽기 전용으로 확인한다.
PASS 후 corrected checker를 SV5_11 최종 generated evidence의 `check_hub_shell.py`로 사용한다.

이 보정은 Apply, Status 변경, 구현, Unity, Finalize, commit, push를 수행하지 않는다.
