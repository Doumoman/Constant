#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Rooms
{
    [CreateAssetMenu(
        menuName = "StarNight/Rooms/Prefab Library",
        fileName = "RoomPrefabLibrary")]
    public sealed class RoomPrefabLibrary : ScriptableObject
    {
        [SerializeField] private RoomRegion region = RoomRegion.MoonPalace;
        [SerializeField] private GameObject[] roomPrefabs = Array.Empty<GameObject>();

        public RoomRegion Region => region;
        public IReadOnlyList<GameObject> RoomPrefabs => roomPrefabs;

        public void Configure(RoomRegion libraryRegion, GameObject[] prefabs)
        {
            region = libraryRegion;
            roomPrefabs = prefabs ?? Array.Empty<GameObject>();
        }

        public RoomTemplate2D GetTemplate(int index)
        {
            if (index < 0
                || index >= roomPrefabs.Length
                || roomPrefabs[index] == null)
            {
                return null;
            }

            return roomPrefabs[index].GetComponent<RoomTemplate2D>();
        }

        public RoomTemplate2D[] GetTemplates()
        {
            RoomTemplate2D[] templates =
                new RoomTemplate2D[roomPrefabs.Length];
            for (int index = 0; index < roomPrefabs.Length; index++)
            {
                templates[index] = GetTemplate(index);
            }

            return templates;
        }
    }
}

#endif
