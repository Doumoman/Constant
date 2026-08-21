#if LEGACY_DISABLED
using StarNight.Interaction.Input;
using UnityEngine;

namespace StarNight.UI.HUD
{
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public sealed class HUDInputCoordinator : MonoBehaviour
    {
        private HUDModelSource source;
        private GameplayInputReader inputReader;
        private PlayerActionRouter actionRouter;

        public bool IsMapOpen => source != null && source.Model.MapOpen;

        private void Awake()
        {
            source = GetComponent<HUDModelSource>();
        }

        private void Update()
        {
            ResolvePlayerInput();
            if (source == null)
            {
                return;
            }

            if (inputReader != null && inputReader.ConsumeMapPressed())
            {
                SetMapOpen(!source.Model.MapOpen);
            }

            if (source.Model.Visibility == HUDVisibility.Hidden && source.Model.MapOpen)
            {
                SetMapOpen(false);
            }
        }

        private void OnDisable()
        {
            if (source != null)
            {
                source.SetMapOpen(false);
            }
            actionRouter?.SetMapOverlayOpen(false);
        }

        public void Configure(HUDModelSource modelSource)
        {
            source = modelSource;
        }

        public void SetMapOpenForTests(bool open)
        {
            ResolvePlayerInput();
            SetMapOpen(open);
        }

        private void SetMapOpen(bool open)
        {
            source.SetMapOpen(open);
            actionRouter?.SetMapOverlayOpen(source.Model.MapOpen);
        }

        private void ResolvePlayerInput()
        {
            if (inputReader == null)
            {
                inputReader = UnityEngine.Object.FindFirstObjectByType<GameplayInputReader>();
            }

            if (actionRouter == null && inputReader != null)
            {
                actionRouter = inputReader.GetComponent<PlayerActionRouter>();
                actionRouter?.SetMapOverlayOpen(source != null && source.Model.MapOpen);
            }
        }
    }
}

#endif
