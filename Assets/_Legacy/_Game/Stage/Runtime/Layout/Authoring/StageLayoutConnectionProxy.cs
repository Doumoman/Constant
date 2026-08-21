#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Stage.Layout.Authoring
{
    public enum StageConnectionVisualKind { MainRoute, Branch, Secret, Maru, Loop }

    [DisallowMultipleComponent]
    public sealed class StageLayoutConnectionProxy : MonoBehaviour
    {
        [SerializeField] private string connectionGuid;
        [SerializeField] private StageRoomProxy sourceRoom;
        [SerializeField] private string sourceSocketGuid;
        [SerializeField] private StageRoomProxy targetRoom;
        [SerializeField] private string targetSocketGuid;
        [SerializeField] private StageConnectionVisualKind visualKind;

        public string ConnectionGuid => connectionGuid;
        public StageRoomProxy SourceRoom => sourceRoom;
        public string SourceSocketGuid => sourceSocketGuid;
        public StageRoomProxy TargetRoom => targetRoom;
        public string TargetSocketGuid => targetSocketGuid;
        public StageConnectionVisualKind VisualKind => visualKind;

        public void Configure(string guid, StageRoomProxy source, string sourceSocket, StageRoomProxy target, string targetSocket, StageConnectionVisualKind kind)
        {
            connectionGuid = guid;
            sourceRoom = source;
            sourceSocketGuid = sourceSocket;
            targetRoom = target;
            targetSocketGuid = targetSocket;
            visualKind = kind;
        }

        public SocketCompatibility GetCompatibility()
        {
            if (sourceRoom == null || targetRoom == null ||
                !sourceRoom.TryGetSocket(sourceSocketGuid, out RoomSocketDefinition source) ||
                !targetRoom.TryGetSocket(targetSocketGuid, out RoomSocketDefinition target))
                return SocketCompatibility.MissingSocket;
            return StageLayoutGraphUtility.GetCompatibility(source, target, sourceRoom == targetRoom);
        }

        private void OnDrawGizmos()
        {
            if (sourceRoom == null || targetRoom == null ||
                !sourceRoom.TryGetSocket(sourceSocketGuid, out RoomSocketDefinition source) ||
                !targetRoom.TryGetSocket(targetSocketGuid, out RoomSocketDefinition target)) return;
            Vector3 start = sourceRoom.GetSocketWorldPosition(source);
            Vector3 end = targetRoom.GetSocketWorldPosition(target);
            Gizmos.color = GetCompatibility() == SocketCompatibility.Compatible ? GetRouteColor(visualKind) : new Color(1f, 0.2f, 0.2f);
            if (visualKind == StageConnectionVisualKind.Secret)
            {
                const int segments = 14;
                for (int index = 0; index < segments; index += 2)
                    Gizmos.DrawLine(Vector3.Lerp(start, end, index / (float)segments), Vector3.Lerp(start, end, (index + 1) / (float)segments));
            }
            else Gizmos.DrawLine(start, end);
        }

        private static Color GetRouteColor(StageConnectionVisualKind kind)
        {
            switch (kind)
            {
                case StageConnectionVisualKind.Branch: return new Color(0.72f, 0.45f, 1f);
                case StageConnectionVisualKind.Secret: return new Color(0.2f, 0.95f, 1f);
                case StageConnectionVisualKind.Maru: return new Color(1f, 0.22f, 0.22f);
                case StageConnectionVisualKind.Loop: return Color.gray;
                default: return Color.white;
            }
        }
    }
}

#endif
