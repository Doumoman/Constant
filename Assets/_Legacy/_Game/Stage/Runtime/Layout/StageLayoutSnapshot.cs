#if LEGACY_DISABLED
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Stage.Layout
{
    [CreateAssetMenu(menuName = "Star Night/Stage Layout/Layout Snapshot", fileName = "StageLayoutSnapshot")]
    public sealed class StageLayoutSnapshot : ScriptableObject
    {
        public string StageId;
        public int Seed;
        public List<RoomNodeSnapshot> Rooms = new List<RoomNodeSnapshot>();
        public List<RoomConnectionSnapshot> Connections = new List<RoomConnectionSnapshot>();
        public string ValidationHash;
    }
}

#endif
