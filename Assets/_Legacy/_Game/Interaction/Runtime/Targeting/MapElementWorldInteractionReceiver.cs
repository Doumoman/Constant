#if LEGACY_DISABLED
using StarNight.Interaction.Input;
using StarNight.Map;
using UnityEngine;

namespace StarNight.Interaction.Targeting
{
    [DisallowMultipleComponent]
    public sealed class MapElementWorldInteractionReceiver : MonoBehaviour, IWorldInteractionReceiver
    {
        public bool CanInteract(GameObject actor)
        {
            return FindReceiver() != null;
        }

        public bool TryInteract(PlayerActionContext action, GameObject actor)
        {
            var receiver = FindReceiver();
            return receiver != null && receiver.TryInteract(actor);
        }

        private IMapElementInteractionReceiver FindReceiver()
        {
            var behaviours = GetComponentsInParent<MonoBehaviour>(true);
            for (var index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IMapElementInteractionReceiver receiver &&
                    !string.IsNullOrWhiteSpace(receiver.InteractionPrompt))
                {
                    return receiver;
                }
            }

            return null;
        }
    }
}

#endif
