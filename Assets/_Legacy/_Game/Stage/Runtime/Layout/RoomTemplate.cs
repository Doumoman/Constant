#if LEGACY_DISABLED
using System.Collections.Generic;
using StarNight.Stage.Rooms;
using UnityEngine;

namespace StarNight.Stage.Layout
{
    [CreateAssetMenu(menuName = "Star Night/Stage Layout/Room Template", fileName = "RoomTemplate")]
    public sealed class RoomTemplate : ScriptableObject
    {
        public string RoomId;
        public RegionId Region;
        public RoomRole Role;
        public Vector2Int SizeCells = RoomSizeCatalog.Micro;
        public RoomCameraMode CameraMode = RoomCameraMode.Fixed;
        public GameObject RoomPrefab;
        public List<RoomSocketDefinition> Sockets = new List<RoomSocketDefinition>();
        public RoomBudget Budget = new RoomBudget();
        public List<string> ContentTags = new List<string>();
        public RoomGeometryHash GeometryHash = new RoomGeometryHash();
    }
}

#endif
