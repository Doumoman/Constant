# MAP04_11 Repair — Allow Scenes Folder Meta

v1.3 실행은 코드 오류가 아니라 잘못된 asset gate 때문에 `BLOCKED`됐다. 프로젝트에 `Assets/_Game/Scenes`가 없는데 scene 생성을 요구하면서 신규 folder meta는 금지했기 때문이다.

현재 완료분은 보존한다.

- variable-count `BiomePatchOverlaySnapshot` repair 완료
- `MapGenerationProgressSceneHarness.cs` 생성 및 compile 완료
- existing overlay tests `150/150 PASS`
- Assets meta `3149`

이 package는 같은 MAP04_11 Task만 교체하고 Unity가 다음 항목을 만들도록 허용한다.

```text
Assets/_Game/Scenes/
Assets/_Game/Scenes.meta
Assets/_Game/Scenes/MapGenerationProgressTest.unity
Assets/_Game/Scenes/MapGenerationProgressTest.unity.meta
```

최종 meta gate는 `3149 -> 3151`이다. scene/harness/tests/1,000-world exit를 완료한 뒤에만 MAP04_11을 finalize한다. MAP05_01은 계속 LOCKED다.

적용 전 SHA:

- Current Task: `7b034f722b7f445041dba9d791b4eec4731a34bce4526683a84e607d6eaa098c`
- BLOCKED Result: `af4ac6e406fd21c0c36f82e91cadd3258b4861615b4817b09d2b0e80f3c0f01e`
