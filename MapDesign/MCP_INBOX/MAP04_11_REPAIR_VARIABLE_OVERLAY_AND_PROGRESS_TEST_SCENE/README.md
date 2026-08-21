# MAP04_11 Repair — Variable Overlay + Progress Test Scene

MAP04_11의 세 번째 실패와 사용자의 수동 테스트 씬 요청을 같은 CURRENT Task 안에서 처리한다. MAP05는 열지 않는다.

## 실패 원인

cleanup repair 자체는 성공했다. 기존 44개 snapshot은 모두 유효하며 `PatchCleanup:InvalidSourceSnapshot=0`이 됐다. 다음 단계인 `BiomePatchOverlaySnapshot.Create`가 viable fixture의 exact `17 = Core 4 / Satellite 10 / Intrusion 3`을 일반 계약으로 사용해 유효한 `15..19` patch publication 44개를 거부했다.

이번 repair는 overlay가 validator-approved publication의 실제 patch/role inventory를 투영하도록 수정한다. Invalid를 handoff로 재분류하거나 seed를 제외하지 않는다.

## 테스트 씬

`Assets/_Game/Scenes/MapGenerationProgressTest.unity`를 만든다. 현재까지 구현된 범위만 보여주는 Editor-only 수동 진단 씬이다.

- MAP02: 13×13 topology
- MAP03: seven-site reservation과 witness
- MAP04: biome patch, C/S/I, seed/site, boundaries, validation summary

씬을 열었다고 generation이 자동 실행되지 않는다. Inspector의 명시적 버튼으로 known viable fixture 또는 선택한 single attempt를 실행하고 Topology/Sites/Biomes 탭을 전환한다. MAP05 route, microchunk, tile bake는 아직 구현 전이라 표시하지 않는다.

## 적용 전

- 상태: `56 COMPLETE / MAP04_11 CURRENT / 148 LOCKED`
- Task SHA-256: `9da9fde9b65ffee5bd9ffbb408a0732d89a65c8f9e8c4be947798780ce3394ec`
- FAIL Result SHA-256: `9b5a4e8746a453f76a2a959ecdae7be2029f503316ef25398551c4762c14b03e`

Patch apply는 현재 Task 한 파일만 조건부 교체한다. Master, Status, Result, Assets는 적용 단계에서 변경하지 않는다.
