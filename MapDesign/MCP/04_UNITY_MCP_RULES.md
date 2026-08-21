# Unity MCP 실행 규칙 v1.0

# 1. Unity MCP의 역할

Unity MCP는 다음을 확인하는 실행/검증 도구로 사용한다.

- Asset Refresh
- Script Compile
- Console Error 확인
- EditMode Test 실행
- 필요한 경우 PlayMode Test 실행
- Debug Scene/Editor Window의 결과 확인
- 명시적으로 허용된 GameObject/Component 생성

Unity MCP를 대량 기획 문서 독해 도구로 사용하지 않는다.

---

# 2. 프로젝트 구조를 추측하지 않는다

아래는 첫 audit 전 임의 확정하지 않는다.

- namespace
- asmdef 이름
- Map 코드 위치
- Test asmdef 위치
- Editor 폴더 위치
- 기존 Tilemap 관리자
- Addressables 사용 여부
- 기존 Data Loader 사용 여부

`MAP00_01_PROJECT_AUDIT` 결과가 이후 TASK의 기준이 된다.

---

# 3. Scene / Prefab / Asset 변경

현재 TASK에 명시되어 있지 않으면 변경 금지:

- Scene
- Prefab
- Tile Palette
- Tile asset
- Animator
- Addressable Group
- Project Settings
- Input Settings
- Physics Settings

디버그 시각화가 필요한 TASK라면 별도 Debug용 객체/EditorWindow만 허용한다.

---

# 4. Compile

코드 변경 TASK 종료 전에 반드시 Unity compile 결과를 확인한다.

PASS 조건:

```text
Compile Error = 0
```

관련 없는 기존 Warning은 보고하되 현재 TASK 범위 밖이면 수정하지 않는다.

---

# 5. Tests

순수 맵 데이터/알고리즘은 가능한 한 UnityEngine 오브젝트 없이 테스트 가능한 구조를 선호한다.

권장:

```text
Pure C# Domain
    ↓
EditMode Test
    ↓
Unity Adapter/Renderer
```

Tilemap, Scene, MonoBehaviour를 알고리즘 핵심의 유일한 데이터 저장소로 사용하지 않는다.

---

# 6. Debug Visualization

각 단계는 최종 타일맵 대신 그 단계의 데이터를 직접 볼 수 있어야 한다.

예:

```text
MAP02: 13×13 Sector Grid
MAP03: Reserved Site Footprint
MAP04: Biome Patch Color
MAP05: Mandatory Route
MAP06: Type0 Optional Overlay
MAP08: Boundary
MAP09: MicroChunk IDs
```

시각화 실패를 데이터 검증 성공으로 간주하지 않고,
데이터 테스트 실패를 시각화가 그럴듯하다는 이유로 무시하지 않는다.

---

# 7. Editor-only 분리

EditorWindow, Gizmo helper, CSV importer UI 등 Editor 전용 코드는 Runtime assembly에 섞지 않는다.
정확한 asmdef 구조는 audit 후 확정한다.

---

# 8. Runtime 성능 최적화

초기 MAP00~MAP10 단계에서는 정답성과 재현성을 우선한다.

다음은 별도 성능 TASK 전까지 하지 않는다.

- premature pooling
- custom job system
- Burst 전환
- NativeArray 전면화
- 비동기 복잡화

단, 명백한 전체 624×416 매 프레임 순회 같은 구조는 만들지 않는다.

---

# 9. Unity 변경 결과 보고

Unity 작업 후 다음을 기록한다.

```text
Unity Version:
Compile Errors:
Relevant Warnings:
EditMode Tests:
PlayMode Tests:
Scene/Prefab Changes:
```

Scene/Prefab Changes는 현재 TASK가 허용하지 않았다면 `NONE`이어야 한다.
