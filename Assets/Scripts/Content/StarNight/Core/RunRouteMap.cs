using System;
using System.Text;
using UnityEngine;

namespace StarFetchingNight
{
    [Serializable]
    public sealed class RunRouteMapSnapshot
    {
        public bool[] restoredGates = new bool[RunRouteMap.GateCount];
        public int playerStationIndex;
        public int maruStationIndex = 1;
    }

    [DisallowMultipleComponent]
    public sealed class RunRouteMap : MonoBehaviour
    {
        public const int GateCount = 5;
        public const int StationCount = 6;

        private static readonly string[] StationNames =
        {
            "달토끼 방앗간",
            "까치다리 정거장",
            "구름고래 목장",
            "별 우체국",
            "잠든 해의 정원",
            "북극성 관측소"
        };

        [SerializeField] private bool[] restoredGates = new bool[GateCount];
        [SerializeField] private int restoredGateCount;
        [SerializeField] private int playerStationIndex;
        [SerializeField] private int maruStationIndex = 1;

        public int RestoredGateCount => restoredGateCount;
        public int PlayerStationIndex => playerStationIndex;
        public int MaruStationIndex => maruStationIndex;
        public event Action Changed;

        public void ResetForRun()
        {
            restoredGates = new bool[GateCount];
            restoredGateCount = 0;
            playerStationIndex = 0;
            maruStationIndex = 1;
            Changed?.Invoke();
        }

        public void BeginChapter(StarChapterId chapter)
        {
            int index = GetGateIndex(chapter);
            if (index >= 0)
            {
                playerStationIndex = index;
                maruStationIndex = Mathf.Clamp(index + 1, 0, StationCount - 1);
                Changed?.Invoke();
            }
        }

        public bool RegisterGateRestored(StarChapterId chapter)
        {
            int index = GetGateIndex(chapter);
            if (index < 0 || restoredGates[index])
            {
                return false;
            }

            restoredGates[index] = true;
            restoredGateCount++;
            playerStationIndex = Mathf.Clamp(index + 1, 0, StationCount - 1);
            maruStationIndex = Mathf.Clamp(index + 2, 0, StationCount - 1);
            Changed?.Invoke();
            return true;
        }

        public bool IsGateRestored(StarChapterId chapter)
        {
            int index = GetGateIndex(chapter);
            return index >= 0 && restoredGates[index];
        }

        public string BuildTicketText()
        {
            StringBuilder builder = new();
            builder.Append("여행 티켓 · 되찾은 별문 ")
                .Append(restoredGateCount)
                .Append('/')
                .Append(GateCount);

            for (int i = 0; i < StationCount; i++)
            {
                bool isGate = i < GateCount;
                string stamp = isGate ? restoredGates[i] ? "◆" : "◇" : restoredGateCount >= GateCount ? "◆" : "○";
                builder.Append('\n').Append(stamp).Append(' ').Append(StationNames[i]);
                if (i == playerStationIndex)
                {
                    builder.Append(" 〈나〉");
                }
                if (i == maruStationIndex)
                {
                    builder.Append(" 〈마루〉");
                }
            }
            return builder.ToString();
        }

        public RunRouteMapSnapshot CaptureSnapshot()
        {
            bool[] gates = new bool[GateCount];
            Array.Copy(restoredGates, gates, Mathf.Min(restoredGates.Length, GateCount));
            return new RunRouteMapSnapshot
            {
                restoredGates = gates,
                playerStationIndex = playerStationIndex,
                maruStationIndex = maruStationIndex
            };
        }

        public void RestoreSnapshot(RunRouteMapSnapshot snapshot)
        {
            restoredGates = new bool[GateCount];
            if (snapshot?.restoredGates != null)
            {
                Array.Copy(snapshot.restoredGates, restoredGates,
                    Mathf.Min(snapshot.restoredGates.Length, GateCount));
            }

            restoredGateCount = 0;
            for (int i = 0; i < restoredGates.Length; i++)
            {
                if (restoredGates[i])
                {
                    restoredGateCount++;
                }
            }

            playerStationIndex = Mathf.Clamp(snapshot?.playerStationIndex ?? 0, 0, StationCount - 1);
            maruStationIndex = Mathf.Clamp(snapshot?.maruStationIndex ?? 1, 0, StationCount - 1);
            Changed?.Invoke();
        }

        public static string GetStationName(int index) =>
            StationNames[Mathf.Clamp(index, 0, StationNames.Length - 1)];

        public static int GetGateIndex(StarChapterId chapter)
        {
            return chapter switch
            {
                StarChapterId.MoonRabbitMill => 0,
                StarChapterId.MagpieBridge => 1,
                StarChapterId.CloudWhaleRanch => 2,
                StarChapterId.StarPostOffice => 3,
                StarChapterId.SleepingSunGarden => 4,
                _ => -1
            };
        }
    }
}
