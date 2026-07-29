using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class StarSelfMailKiosk : MonoBehaviour, IStarNightInteractable
    {
        [SerializeField] private string destinationAddressId = "SORTING";
        [SerializeField] private string destinationLabel = "분류실";

        public string Prompt => $"자신을 {destinationLabel}(으)로 배송하기";

        public void Configure(string addressId, string label)
        {
            destinationAddressId = addressId;
            destinationLabel = label;
        }

        public void Interact(StarNightPlayerAgent player)
        {
            FableToolResult result = StarNightRunState.Ensure().Delivery.DeliverPlayer(player, destinationAddressId);
            StarNightHUD.Instance?.Toast(result.sentence, 3f);
        }
    }
}
