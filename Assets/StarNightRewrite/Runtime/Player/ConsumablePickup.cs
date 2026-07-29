using UnityEngine;

namespace StarNight.Rewrite.Player
{
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public sealed class ConsumablePickup : MonoBehaviour, IPlayerInteractable
    {
        public enum ConsumableKind
        {
            Rope = 0,
            Bomb = 1
        }

        [SerializeField]
        private ConsumableKind kind;

        [SerializeField, Min(1)]
        private int amount = 1;

        public string Prompt =>
            kind == ConsumableKind.Rope ? $"E 밧줄 +{amount}" : $"E 폭탄 +{amount}";

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        public bool CanInteract(PlayerInteractor interactor)
        {
            return interactor != null;
        }

        public void Interact(PlayerInteractor interactor)
        {
            ConsumableInventory inventory =
                interactor.GetComponent<ConsumableInventory>();
            if (inventory == null)
            {
                return;
            }

            int added = kind == ConsumableKind.Rope
                ? inventory.AddRopes(amount)
                : inventory.AddBombs(amount);
            if (added > 0)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
