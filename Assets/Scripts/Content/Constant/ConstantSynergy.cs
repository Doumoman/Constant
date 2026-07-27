using System.Collections.Generic;
using System.Text;

/// <summary>
/// 여행가방(4x3) 배치에서 계산된 시너지 v2.
/// 1) 인접 태그 링크(패시브 보너스) + 2) 제작 아이템의 소지 패시브 합산.
/// (조합 자체는 RunManager.TryAutoCraft 가 도감 레시피로 처리한다)
/// </summary>
public class ConstantSynergy
{
    public float moveSpeedMul = 1f;
    public float jumpMul = 1f;
    public float gatherMul = 1f;
    public float visionBonus = 0f;
    public bool lavaImmune = false;
    public int revives = 0;
    public bool bigBomb = false;      // 폭탄 5x5
    public bool slowMobs = false;     // 몹 30% 감속
    public int stageRope = 0;         // 스테이지 시작 로프 보급
    public int stageBomb = 0;

    public readonly List<(string label, int count)> links = new List<(string, int)>();
    public readonly List<string> craftedPassives = new List<string>(); // UI 표시용

    public string Summary()
    {
        var sb = new StringBuilder();
        if (moveSpeedMul > 1.001f) sb.Append($"이속 +{(moveSpeedMul - 1f) * 100f:0}%  ");
        if (jumpMul > 1.001f) sb.Append($"점프 +{(jumpMul - 1f) * 100f:0}%  ");
        if (gatherMul > 1.001f) sb.Append($"채집 +{(gatherMul - 1f) * 100f:0}%  ");
        if (visionBonus > 0.001f) sb.Append($"시야 +{visionBonus:0.#}  ");
        if (lavaImmune) sb.Append("용암 면역  ");
        if (revives > 0) sb.Append($"부활 {revives}회  ");
        if (bigBomb) sb.Append("폭탄 5x5  ");
        if (slowMobs) sb.Append("몹 감속  ");
        return sb.ToString().TrimEnd();
    }

    public static ConstantSynergy Compute(string[] slots)
    {
        var result = new ConstantSynergy();
        if (slots == null) return result;

        int width = RunManager.GridWidth;
        int height = RunManager.GridHeight;

        var propLinks = new Dictionary<PropertyTag, int>();
        var actLinks = new Dictionary<ActionTag, int>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = y * width + x;
                var def = ConstantItemDb.Get(SafeGet(slots, idx));
                if (def == null) continue;

                // 제작 아이템 소지 패시브
                if (def.isCrafted)
                {
                    result.jumpMul += def.pJump;
                    result.moveSpeedMul += def.pMove;
                    result.gatherMul += def.pGather;
                    result.visionBonus += def.pVision;
                    if (def.pLavaImmune) result.lavaImmune = true;
                    if (def.pBigBomb) result.bigBomb = true;
                    if (def.pSlowMobs) result.slowMobs = true;
                    result.revives += def.pRevive;
                    result.stageRope += def.pStageRope;
                    result.stageBomb += def.pStageBomb;

                    if (!def.isActive && !string.IsNullOrEmpty(def.useHint))
                        result.craftedPassives.Add($"{def.displayName}: {def.useHint.Replace("소지 패시브: ", "")}");
                }

                // 인접 태그 링크 (오른쪽/아래 이웃만 — 중복 방지)
                CheckPair(def, x + 1 < width ? SafeGet(slots, idx + 1) : null, propLinks, actLinks);
                CheckPair(def, y + 1 < height ? SafeGet(slots, idx + width) : null, propLinks, actLinks);
            }
        }

        int guardLinks = 0;
        foreach (var kv in propLinks)
        {
            switch (kv.Key)
            {
                case PropertyTag.Heat: result.gatherMul += 0.06f * kv.Value; break;
                case PropertyTag.Cold: result.jumpMul += 0.05f * kv.Value; break;
                case PropertyTag.Echo: result.visionBonus += 0.5f * kv.Value; break;
                case PropertyTag.Machine: result.moveSpeedMul += 0.05f * kv.Value; break;
            }
            result.links.Add(($"{ConstantDefine.NameOf(kv.Key)} 연결", kv.Value));
        }
        foreach (var kv in actLinks)
        {
            switch (kv.Key)
            {
                case ActionTag.Launch: result.jumpMul += 0.04f * kv.Value; break;
                case ActionTag.Burst: result.gatherMul += 0.05f * kv.Value; break;
                case ActionTag.Repeat: result.moveSpeedMul += 0.04f * kv.Value; break;
                case ActionTag.Guard: guardLinks += kv.Value; break;
            }
            result.links.Add(($"{ConstantDefine.NameOf(kv.Key)} 연결", kv.Value));
        }

        result.revives += guardLinks / 2; // 수호 링크 2개당 부활 1회

        return result;
    }

    private static string SafeGet(string[] slots, int idx) =>
        idx >= 0 && idx < slots.Length ? slots[idx] : null;

    private static void CheckPair(ConstantItemDef a, string otherId,
        Dictionary<PropertyTag, int> propLinks, Dictionary<ActionTag, int> actLinks)
    {
        var b = ConstantItemDb.Get(otherId);
        if (b == null) return;

        if (a.property == b.property)
            propLinks[a.property] = propLinks.TryGetValue(a.property, out int p) ? p + 1 : 1;
        if (a.action == b.action)
            actLinks[a.action] = actLinks.TryGetValue(a.action, out int c) ? c + 1 : 1;
    }
}
