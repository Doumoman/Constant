using UnityEngine;

namespace StarNight.Rewrite.Player
{
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public sealed class HealingPickup : MonoBehaviour, IPlayerInteractable
    {
        [SerializeField, Min(1)]
        private int healing = 1;

        public string Prompt => "E 회복 달떡 +1";

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        public bool CanInteract(PlayerInteractor interactor)
        {
            PlayerHealth health = interactor != null
                ? interactor.GetComponent<PlayerHealth>()
                : null;
            return health != null && health.Current < health.Maximum;
        }

        public void Interact(PlayerInteractor interactor)
        {
            PlayerHealth health = interactor.GetComponent<PlayerHealth>();
            if (health != null && health.Heal(healing) > 0)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
