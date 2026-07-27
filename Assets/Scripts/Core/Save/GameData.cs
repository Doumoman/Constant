using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData
{
    /// <summary>
    /// Constant 도감 — 해금(발견)한 조합 레시피 결과 id 목록.
    /// 단판(런) 사이에도 유지되는 유일한 메타 진행: 남는 것은 지식뿐이다.
    /// </summary>
    public List<string> unlockedRecipes = new List<string>();
}
