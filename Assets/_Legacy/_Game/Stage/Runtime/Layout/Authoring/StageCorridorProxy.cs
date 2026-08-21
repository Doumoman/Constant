#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Stage.Layout.Authoring
{
    [DisallowMultipleComponent]
    public sealed class StageCorridorProxy : MonoBehaviour
    {
        [SerializeField] private StageRoomProxy sourceRoom;
        [SerializeField] private string sourceSocketGuid;
        [SerializeField] private StageRoomProxy targetRoom;
        [SerializeField] private string targetSocketGuid;

        public void Configure(StageRoomProxy source, string sourceSocket, StageRoomProxy target, string targetSocket)
        {
            sourceRoom = source;
            sourceSocketGuid = sourceSocket;
            targetRoom = target;
            targetSocketGuid = targetSocket;
        }

        private void OnDrawGizmos()
        {
            if (sourceRoom == null || targetRoom == null ||
                !sourceRoom.TryGetSocket(sourceSocketGuid, out RoomSocketDefinition source) ||
                !targetRoom.TryGetSocket(targetSocketGuid, out RoomSocketDefinition target)) return;
            Vector3 start = sourceRoom.GetSocketWorldPosition(source);
            Vector3 end = targetRoom.GetSocketWorldPosition(target);
            Vector3 middle = new Vector3(end.x, start.y, 0.02f);
            Gizmos.color = new Color(0.48f, 0.52f, 0.58f, 0.9f);
            Gizmos.DrawLine(start, middle);
            Gizmos.DrawLine(middle, end);
        }
    }
}

#endif
