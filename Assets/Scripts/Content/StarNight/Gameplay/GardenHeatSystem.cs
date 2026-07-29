using System;
using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class GardenHeatSystem : MonoBehaviour
    {
        public const float OverheatThreshold = 70f;
        public const float FireThreshold = 95f;

        [SerializeField, Range(0f, 100f)] private float heat;
        [SerializeField] private bool overheatRecorded;
        [SerializeField] private bool fireRecorded;

        public float Heat => heat;
        public bool Overheated => heat >= OverheatThreshold;
        public bool Burning => heat >= FireThreshold;

        public event Action<float> HeatChanged;

        public void AddHeat(float amount, string reason, string sourceId = null)
        {
            if (Mathf.Approximately(amount, 0f))
            {
                return;
            }

            heat = Mathf.Clamp(heat + amount, 0f, 100f);
            HeatChanged?.Invoke(heat);
            StarNightRunState run = StarNightRunState.Instance;
            if (run == null)
            {
                return;
            }

            if (heat >= OverheatThreshold && !overheatRecorded)
            {
                overheatRecorded = true;
                run.SetFlag("CH5_GARDEN_OVERHEATED");
                run.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.GardenOverheated,
                    actorId = "GardenHeat",
                    targetId = sourceId,
                    detail = $"빛이 겹쳐 정원 온도가 임계점을 넘었다 · {reason}",
                    causedAccident = true,
                    witnessed = true
                });
                run.AccidentReport.Add("정원의 햇빛", "한곳에 겹쳐", "잠든 식물과 구조물을 말리기 시작했다",
                    run.Actions.LatestSequence);
            }

            if (heat >= FireThreshold && !fireRecorded)
            {
                fireRecorded = true;
                run.SetFlag("CH5_GARDEN_FIRE");
            }
        }

        public bool RestoreGarden(float cooling, string reason)
        {
            StarNightRunState run = StarNightRunState.Ensure();
            if (!Overheated && !run.GetFlag("CH5_GARDEN_OVERHEATED") &&
                !run.GetFlag("CH5_GARDEN_FIRE"))
            {
                return false;
            }

            heat = Mathf.Clamp(heat - Mathf.Abs(cooling), 0f, 100f);
            HeatChanged?.Invoke(heat);
            run.SetFlag("CH5_GARDEN_RESTORED");
            run.SetFlag("CH5_GARDEN_FIRE", false);
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.GardenRestored,
                actorId = "Player",
                targetId = "SleepingSunGarden",
                detail = reason,
                helpedResident = true,
                witnessed = true
            });
            return true;
        }

        public void SetInitialHeat(float value)
        {
            heat = Mathf.Clamp(value, 0f, 100f);
            overheatRecorded = heat >= OverheatThreshold;
            fireRecorded = heat >= FireThreshold;
            HeatChanged?.Invoke(heat);
        }

        public void ResetForChapter()
        {
            heat = 0f;
            overheatRecorded = false;
            fireRecorded = false;
            HeatChanged?.Invoke(heat);
        }
    }
}
