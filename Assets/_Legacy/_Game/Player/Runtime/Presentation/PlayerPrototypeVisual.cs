#if LEGACY_DISABLED
using StarNight.Interaction.Input;
using StarNight.Player.Motor;
using UnityEngine;

namespace StarNight.Player.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer), typeof(PlayerMotor2D))]
    public sealed class PlayerPrototypeVisual : MonoBehaviour
    {
        private static readonly Color PlayerGold = new Color(0.95f, 0.74f, 0.28f, 1f);
        private static readonly Color ActionTeal = new Color(0.25f, 0.92f, 0.82f, 1f);

        private SpriteRenderer spriteRenderer;
        private PlayerMotor2D motor;
        private PlayerActionRouter actionRouter;
        private float actionFlashRemaining;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            motor = GetComponent<PlayerMotor2D>();
            spriteRenderer.sprite = PrototypeSpriteFactory.GetWhitePixel();
            spriteRenderer.color = PlayerGold;
            spriteRenderer.sortingOrder = 20;
            spriteRenderer.drawMode = SpriteDrawMode.Sliced;
            spriteRenderer.size = new Vector2(0.68f, 0.88f);

            actionRouter = GetComponent<PlayerActionRouter>();
            if (actionRouter != null)
            {
                actionRouter.ActionRouted += HandleActionRouted;
            }
        }

        private void OnDestroy()
        {
            if (actionRouter != null)
            {
                actionRouter.ActionRouted -= HandleActionRouted;
            }
        }

        private void Update()
        {
            spriteRenderer.flipX = motor.Facing < 0;

            actionFlashRemaining = Mathf.Max(0f, actionFlashRemaining - Time.unscaledDeltaTime);
            spriteRenderer.color = actionFlashRemaining > 0f ? ActionTeal : PlayerGold;
        }

        private void HandleActionRouted(RoutedPlayerAction action)
        {
            actionFlashRemaining = 0.12f;
        }
    }
}

#endif
