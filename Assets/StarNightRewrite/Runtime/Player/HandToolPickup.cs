using StarNight.Rewrite.Core;
using UnityEngine;

namespace StarNight.Rewrite.Player
{
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public sealed class HandToolPickup : MonoBehaviour, IPlayerInteractable
    {
        [SerializeField]
        private HandToolId tool = HandToolId.Pickaxe;

        public string Prompt => $"E {GetKoreanName(tool)} 장착";

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        public bool CanInteract(PlayerInteractor interactor)
        {
            return interactor != null && tool != HandToolId.None;
        }

        public void Interact(PlayerInteractor interactor)
        {
            PlayerToolController controller =
                interactor.GetComponent<PlayerToolController>();
            if (controller == null)
            {
                return;
            }

            HandToolId previous = controller.Equip(tool);
            if (previous == HandToolId.None)
            {
                gameObject.SetActive(false);
                return;
            }

            tool = previous;
        }

        public static string GetKoreanName(HandToolId value)
        {
            return value switch
            {
                HandToolId.Pickaxe => "곡괭이",
                HandToolId.Shovel => "삽",
                HandToolId.WateringCan => "물뿌리개",
                HandToolId.Pestle => "절구",
                HandToolId.GrapplingHook => "갈고리",
                HandToolId.Umbrella => "우산",
                _ => "없음"
            };
        }
    }
}
