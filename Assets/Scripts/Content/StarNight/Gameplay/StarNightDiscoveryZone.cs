using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class StarNightDiscoveryZone : MonoBehaviour
    {
        [SerializeField] private string roomId;
        [SerializeField] private string displayName;
        [SerializeField] private bool optional;
        private bool discovered;

        public void Configure(string id, string label, bool isOptional)
        {
            roomId = id;
            displayName = label;
            optional = isOptional;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (discovered || other.GetComponentInParent<StarNightPlayerAgent>() == null)
            {
                return;
            }

            discovered = true;
            StarNightRunState run = StarNightRunState.Ensure();
            run.SetFlag($"room.{roomId}.visited");
            if (optional)
            {
                run.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.RareRoomEntered,
                    actorId = "Player",
                    targetId = roomId,
                    detail = $"{displayName}에 발을 들였다",
                    witnessed = true
                });
                run.Chapter.AddScent(3f, "곁방의 오래된 별가루가 깨어났다", roomId);
            }
            StarNightHUD.Instance?.Toast(displayName, 1.8f);
        }
    }
}
