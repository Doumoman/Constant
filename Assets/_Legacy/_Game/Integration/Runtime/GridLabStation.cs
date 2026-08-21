#if LEGACY_DISABLED
using StarNight.Interaction.Input;
using StarNight.Narrative;
using StarNight.Player.Motor;
using StarNight.Player.Presentation;
using UnityEngine;

namespace StarNight.Integration
{
    public enum GridLabStationRole
    {
        Interaction,
        Narrative,
    }

    [DisallowMultipleComponent]
    public sealed class GridLabStation : MonoBehaviour
    {
        public const float ActivationRange = 2f;

        private GridLabStationRole role;
        private string yarnNode = string.Empty;
        private PlayerMotor2D player;
        private GameplayInputReader inputReader;
        private NarrativeSystemController narrative;
        private SpriteRenderer body;

        public GridLabStationRole Role => role;
        public int ActivationCount { get; private set; }
        public bool IsPlayerInRange => player != null &&
            Vector2.Distance(player.transform.position, transform.position) <= ActivationRange;

        public void Configure(GridLabStationRole stationRole, string dialogueNode)
        {
            role = stationRole;
            yarnNode = dialogueNode ?? string.Empty;
            EnsureVisual();
        }

        private void Start()
        {
            ResolveDependencies();
        }

        private void OnDestroy()
        {
            if (inputReader != null)
            {
                inputReader.PrimaryActionPressed -= HandlePrimaryAction;
            }
        }

        public bool ActivateForTests()
        {
            ResolveDependencies();
            return Activate();
        }

        private void HandlePrimaryAction()
        {
            if (IsPlayerInRange)
            {
                Activate();
            }
        }

        private bool Activate()
        {
            if (role == GridLabStationRole.Narrative)
            {
                if (narrative?.Service == null || !narrative.Service.TryRunNode(yarnNode))
                {
                    return false;
                }
            }

            ActivationCount++;
            if (body != null)
            {
                body.color = role == GridLabStationRole.Narrative
                    ? new Color(0.48f, 0.82f, 1f, 1f)
                    : new Color(1f, 0.82f, 0.32f, 1f);
            }
            return true;
        }

        private void ResolveDependencies()
        {
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerMotor2D>();
            }
            if (narrative == null)
            {
                narrative = FindFirstObjectByType<NarrativeSystemController>();
            }

            GameplayInputReader resolved = player != null
                ? player.GetComponent<GameplayInputReader>()
                : FindFirstObjectByType<GameplayInputReader>();
            if (resolved == inputReader)
            {
                return;
            }
            if (inputReader != null)
            {
                inputReader.PrimaryActionPressed -= HandlePrimaryAction;
            }
            inputReader = resolved;
            if (inputReader != null)
            {
                inputReader.PrimaryActionPressed += HandlePrimaryAction;
            }
        }

        private void EnsureVisual()
        {
            body = GetComponent<SpriteRenderer>();
            if (body == null)
            {
                body = gameObject.AddComponent<SpriteRenderer>();
            }
            body.sprite = PrototypeSpriteFactory.GetWhitePixel();
            body.color = role == GridLabStationRole.Narrative
                ? new Color(0.22f, 0.55f, 0.78f, 1f)
                : new Color(0.72f, 0.48f, 0.16f, 1f);
            body.sortingOrder = 30;
            transform.localScale = new Vector3(0.9f, 1.4f, 1f);
        }
    }
}

#endif
