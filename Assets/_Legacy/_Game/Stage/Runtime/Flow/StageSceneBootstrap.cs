#if LEGACY_DISABLED
using System.Collections;
using StarNight.Core.Flow;
using StarNight.Core.State;
using StarNight.Player.Motor;
using StarNight.Stage.Data;
using StarNight.Stage.Lab;
using StarNight.Stage.Maru;
using UnityEngine;

namespace StarNight.Stage.Flow
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Core04TwoRoomLab))]
    public sealed class StageSceneBootstrap : MonoBehaviour
    {
        [SerializeField] private StageDefinition definition;
        private bool entered;

        public StageDefinition Definition => definition;

        private void Awake()
        {
            if (FeatureFlag.NewStageArchitecture)
            {
                enabled = false;
                return;
            }

            ApplyStageArt();
            TryEnter();
        }

        private IEnumerator Start()
        {
            if (FeatureFlag.NewStageArchitecture)
            {
                yield break;
            }

            for (int attempt = 0; !entered && attempt < 10; attempt++)
            {
                yield return null;
                TryEnter();
            }
        }

        public void Configure(StageDefinition stageDefinition)
        {
            definition = stageDefinition;
            ApplyStageArt();
        }

        private void TryEnter()
        {
            if (FeatureFlag.NewStageArchitecture)
            {
                return;
            }

            ApplyStageArt();
            if (entered || definition == null || !GameBootstrap.IsReady)
            {
                return;
            }

            GameBootstrap bootstrap = GameBootstrap.Instance;
            RunManagerDependencies(bootstrap, out StageFlowController flow);
            PlayerMotor2D player = Object.FindFirstObjectByType<PlayerMotor2D>();
            Camera camera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
            if (player == null || camera == null)
            {
                return;
            }

            Core04TwoRoomLab lab = GetComponent<Core04TwoRoomLab>();
            entered = flow.EnterStage(definition, lab, player, camera, null);
        }

        private void ApplyStageArt()
        {
            if (FeatureFlag.NewStageArchitecture)
            {
                return;
            }

            if (definition == null)
            {
                return;
            }

            Core04TwoRoomLab lab = GetComponent<Core04TwoRoomLab>();
            lab?.ApplyArtProfile(definition.artProfile);
        }

        private static void RunManagerDependencies(GameBootstrap bootstrap, out StageFlowController flow)
        {
            flow = bootstrap.GetComponent<StageFlowController>();
            if (flow == null)
            {
                flow = bootstrap.gameObject.AddComponent<StageFlowController>();
                flow.Initialize(
                    bootstrap.Services.GetRequired<RunManager>(),
                    bootstrap.Services.GetRequired<SceneTransitionService>(),
                    bootstrap.Services.GetRequired<GameFlowController>());
            }

            if (!bootstrap.Services.TryGet<StageFlowController>(out _))
            {
                bootstrap.Services.Register(flow);
            }

            MaruDirector maru = bootstrap.GetComponent<MaruDirector>();
            if (maru == null)
            {
                maru = bootstrap.gameObject.AddComponent<MaruDirector>();
                maru.Initialize(
                    bootstrap.Services.GetRequired<RunManager>(),
                    bootstrap.Services.GetRequired<GameFlowController>(),
                    flow);
            }
            if (!bootstrap.Services.TryGet<MaruDirector>(out _))
            {
                bootstrap.Services.Register(maru);
            }
        }
    }
}

#endif
