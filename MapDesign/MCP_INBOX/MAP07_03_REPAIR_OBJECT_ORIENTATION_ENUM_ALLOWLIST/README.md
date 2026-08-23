# MAP07_03 Repair — Object Orientation Enum Allowlist

`MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS` v1.0 Result가 `BLOCKED`였기 때문에 MAP07_04는 열지 않고, MAP07_03 Task 문서만 v1.1로 교체하는 repair package다.

기준선:

```text
Blocked Result: MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS_RESULT.md
Blocked Result STATUS: BLOCKED
Blocked Result SHA-256: e267a5439a52aebda336256e726a9dd8d74f8a3a7317992e24b1da57dc40ab80
Previous MAP07_03 Task SHA-256: 82434805780000e3695cbdda45d5888c4234ba617bdc5bcded843643b4c7aac8
Revised MAP07_03 Task SHA-256: f9aee2e6fe0c0a3222eae894cb562ef2100813c4a91e16461fd03e5d5d4cb170
State after repair apply: 80 COMPLETE / MAP07_03 CURRENT / 124 LOCKED
```

Repair 범위:

- `MicrochunkEnums.cs`를 exact write allowlist에 추가.
- `MicrochunkObjectOrientation`에 package tokens `L`, `R`, `U`, `D` 대응 값을 추가하는 기계적 enum repair만 허용.
- `None` 및 기존 `NONE` object slot semantics 보존.
- static gate를 `MAP07_01 production source changes 0`에서 `MicrochunkEnums.cs` exact 1개 승인 변경으로 교정.
- MAP07_04+ production symbols와 Task body는 계속 LOCKED.

Master/Status payload는 포함하지 않는다.
