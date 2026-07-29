using System;
using System.Collections.Generic;

namespace StarFetchingNight
{
    [Serializable]
    public sealed class StarRoomNode
    {
        public string id;
        public string displayName;
        public bool guaranteed;
        public bool temptation;
        public int depth;
        public List<string> links = new();
    }

    public static class StarNightRoomGraphGenerator
    {
        public static List<StarRoomNode> GenerateMoonMill(int seed, int roomCount = 11)
        {
            Random random = new(seed);
            string[] guaranteed = { "도착 마당", "절구방", "멈춘 방앗간", "달떡 창고", "굴뚝길", "달배 선착장" };
            string[] optional = { "폭발 열매 온실", "토끼의 다락", "깨진 시계방", "달가루 우물", "달 뒤편 창고", "낮잠 저장고" };
            List<StarRoomNode> result = new();

            for (int i = 0; i < guaranteed.Length; i++)
            {
                result.Add(new StarRoomNode
                {
                    id = $"G{i:00}",
                    displayName = guaranteed[i],
                    guaranteed = true,
                    depth = i * 2
                });
            }

            List<string> optionalPool = new(optional);
            while (result.Count < Math.Max(guaranteed.Length, roomCount) && optionalPool.Count > 0)
            {
                int pick = random.Next(optionalPool.Count);
                string label = optionalPool[pick];
                optionalPool.RemoveAt(pick);
                int insertion = random.Next(1, result.Count);
                result.Insert(insertion, new StarRoomNode
                {
                    id = $"R{result.Count:00}",
                    displayName = label,
                    guaranteed = false,
                    temptation = label == "달 뒤편 창고"
                });
            }

            for (int i = 0; i < result.Count; i++)
            {
                result[i].depth = i;
                if (i > 0) result[i].links.Add(result[i - 1].id);
                if (i < result.Count - 1) result[i].links.Add(result[i + 1].id);
                if (i > 1 && i < result.Count - 2 && random.NextDouble() > 0.6)
                {
                    result[i].links.Add(result[i + 2].id);
                }
            }
            return result;
        }
    }
}
