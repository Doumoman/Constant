#if LEGACY_DISABLED
using System;

namespace StarNight.Map
{
    public enum MaruElementEventType
    {
        StatueWarning,
        StatueBroken,
        BellJarBroken,
        CollarCarryChanged,
        CollarCommittedAtExit,
        ReturnMarkerUsed,
        PawprintPoolTriggered,
        RecordTravelerFreed,
    }

    [Serializable]
    public struct MaruElementEventRequest
    {
        public MaruElementEventType EventType;
        public string ElementId;
        public string SourceRuntimeId;
        public int RewardMoney;
        public string RewardId;
        public float Seconds;
        public float GuidanceSeconds;
        public float RateMultiplier;
        public bool Active;
        public MaruMarkerCostType MarkerCostType;
        public int MarkerCostValue;
        public MaruRecordGuideEffect RecordGuideEffect;
        public float NoiseLevel;
    }

    [Serializable]
    public struct MaruElementEventResult
    {
        public bool Accepted;
        public bool RewardGranted;
        public bool PenaltyApplied;
        public string RewardText;
        public string PenaltyText;

        public static MaruElementEventResult Rejected(string reason)
        {
            return new MaruElementEventResult
            {
                Accepted = false,
                RewardText = string.Empty,
                PenaltyText = reason ?? string.Empty,
            };
        }
    }

    public interface IMaruElementEventSink
    {
        bool IsExitDiscovered { get; }
        MaruElementEventResult ApplyMaruElementEvent(MaruElementEventRequest request);
    }

    public static class MaruElementEventHub
    {
        private static IMaruElementEventSink sink;

        public static bool HasSink
        {
            get
            {
                if (sink is UnityEngine.Object unityObject && unityObject == null)
                {
                    sink = null;
                }
                return sink != null;
            }
        }

        public static void Bind(IMaruElementEventSink eventSink)
        {
            sink = eventSink;
        }

        public static void Unbind(IMaruElementEventSink eventSink)
        {
            if (ReferenceEquals(sink, eventSink))
            {
                sink = null;
            }
        }

        public static bool IsExitDiscovered()
        {
            return HasSink && sink.IsExitDiscovered;
        }

        public static MaruElementEventResult Dispatch(MaruElementEventRequest request)
        {
            return HasSink
                ? sink.ApplyMaruElementEvent(request)
                : MaruElementEventResult.Rejected("MaruDirector 연결 없음");
        }
    }
}

#endif
