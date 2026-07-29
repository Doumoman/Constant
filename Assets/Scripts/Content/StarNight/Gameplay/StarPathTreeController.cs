using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class StarPathTreeController : MonoBehaviour
    {
        [SerializeField] private SunGrowthState growth;
        [SerializeField] private GameObject stableRoute;
        [SerializeField] private GameObject overgrownShortcut;
        [SerializeField] private GameObject burnedRoute;
        [SerializeField] private int recordedProgress;

        public SunGrowthState Growth => growth;

        public void Configure(SunGrowthState state, GameObject stable,
            GameObject overgrown, GameObject burned)
        {
            growth = state;
            stableRoute = stable;
            overgrownShortcut = overgrown;
            burnedRoute = burned;
        }

        private void Start()
        {
            if (growth == null)
            {
                growth = GetComponent<SunGrowthState>();
            }
            if (growth != null)
            {
                growth.LightChanged += OnLightChanged;
                growth.StageChanged += OnStageChanged;
                OnLightChanged(growth, growth.LightExposure);
                ApplyRouteVisual(growth.Stage);
            }
        }

        private void OnDestroy()
        {
            if (growth != null)
            {
                growth.LightChanged -= OnLightChanged;
                growth.StageChanged -= OnStageChanged;
            }
        }

        public void ForceGrowToReady()
        {
            if (growth == null)
            {
                growth = GetComponent<SunGrowthState>();
            }
            if (growth == null)
            {
                return;
            }
            int amount = Mathf.Max(0, 3 - growth.LightExposure);
            if (amount > 0)
            {
                growth.ApplySunlight(amount);
            }
        }

        public bool Stabilize()
        {
            StarNightRunState run = StarNightRunState.Ensure();
            if (!run.Chapter.DepartureReady || growth == null ||
                growth.Stage == SunGrowthStage.Burned)
            {
                return false;
            }

            growth.SetStoryStage(SunGrowthStage.Blooming, Mathf.Max(3, growth.LightExposure));
            run.SetFlag("CH5_STAR_PATH_TREE_STABLE");
            run.SetFlag("CH5_STAR_PATH_TREE_OVERGROWN", false);
            run.Heat.AddHeat(-10f, "별길 나무의 가지를 안정된 폭으로 다듬음", growth.GrowthId);
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.StarPathStabilized,
                actorId = "Player",
                targetId = growth.GrowthId,
                detail = "더 빠른 성장을 멈추고 별길 나무를 오래 버틸 항로로 다듬었다",
                helpedResident = true,
                witnessed = true
            });
            ApplyRouteVisual(SunGrowthStage.Blooming);
            return true;
        }

        public bool Overgrow()
        {
            StarNightRunState run = StarNightRunState.Ensure();
            if (!run.Chapter.DepartureReady || growth == null ||
                growth.Stage == SunGrowthStage.Burned)
            {
                return false;
            }
            if (growth.Stage != SunGrowthStage.Overgrown &&
                !run.SunSeeds.ConsumeCharge())
            {
                return false;
            }

            if (growth.Stage != SunGrowthStage.Overgrown)
            {
                growth.ApplySunlight();
            }
            run.SetFlag("CH5_STAR_PATH_TREE_OVERGROWN");
            run.SetFlag("CH5_STAR_PATH_TREE_STABLE", false);
            run.Heat.AddHeat(15f, "별길 나무를 지름길까지 급성장시킴", growth.GrowthId);
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.StarPathOvergrown,
                actorId = "Player",
                targetId = growth.GrowthId,
                detail = "별길 나무를 더 크게 키워 빠르지만 불안정한 지름길을 만들었다",
                causedAccident = true,
                witnessed = true
            });
            ApplyRouteVisual(SunGrowthStage.Overgrown);
            return true;
        }

        private void OnLightChanged(SunGrowthState state, int exposure)
        {
            StarNightRunState run = StarNightRunState.Instance;
            if (run == null || run.CurrentChapter != StarChapterId.SleepingSunGarden)
            {
                return;
            }

            int desired = Mathf.Min(3, exposure);
            int delta = desired - recordedProgress;
            if (delta > 0)
            {
                recordedProgress = desired;
                run.Chapter.AddDepartureProgress(delta, state.GrowthId);
            }
        }

        private void OnStageChanged(SunGrowthState state, SunGrowthStage previous, SunGrowthStage current)
        {
            StarNightRunState run = StarNightRunState.Ensure();
            ApplyRouteVisual(current);
            if (current == SunGrowthStage.Blooming)
            {
                run.SetFlag("CH5_STAR_PATH_TREE_BLOOMED");
                run.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.StarPathGrown,
                    actorId = "Player",
                    targetId = state.GrowthId,
                    detail = "북극성으로 이어지는 별길 나무가 항로를 만들 만큼 자랐다",
                    helpedResident = true,
                    witnessed = true
                });
            }
            else if (current == SunGrowthStage.Overgrown)
            {
                run.SetFlag("CH5_STAR_PATH_TREE_OVERGROWN");
            }
            else if (current == SunGrowthStage.Burned)
            {
                run.SetFlag("CH5_STAR_PATH_TREE_BURNED");
                run.SetFlag("CH5_STAR_PATH_TREE_STABLE", false);
                run.SetFlag("CH5_STAR_PATH_TREE_OVERGROWN", false);
                run.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.StarPathBurned,
                    actorId = "Player",
                    targetId = state.GrowthId,
                    detail = "별길 나무에 빛을 겹쳐 항로 가지를 말려 태웠다",
                    causedAccident = true,
                    witnessed = true
                });
                run.Heat.AddHeat(22f, "별길 나무의 마른 가지가 타기 시작함", state.GrowthId);
            }
        }

        private void ApplyRouteVisual(SunGrowthStage stage)
        {
            if (stableRoute != null)
            {
                stableRoute.SetActive(stage == SunGrowthStage.Blooming);
            }
            if (overgrownShortcut != null)
            {
                overgrownShortcut.SetActive(stage == SunGrowthStage.Overgrown);
            }
            if (burnedRoute != null)
            {
                burnedRoute.SetActive(stage == SunGrowthStage.Burned);
            }
        }
    }
}
