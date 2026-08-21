#if LEGACY_DISABLED
using StarNight.Rooms;
using UnityEngine;

namespace StarNight.Debugging
{
    [DisallowMultipleComponent]
    public sealed class P4RoomLibraryContract : MonoBehaviour
    {
        public const int RequiredRoomCount = 33;

        [SerializeField] private RoomPrefabLibrary library;
        [SerializeField] private RoomTemplate2D[] roomInstances =
            System.Array.Empty<RoomTemplate2D>();
        [SerializeField, TextArea(2, 8)] private string lastValidation = "Not validated.";

        public RoomPrefabLibrary Library => library;
        public RoomTemplate2D[] RoomInstances => roomInstances;
        public string LastValidation => lastValidation;

        public void Configure(
            RoomPrefabLibrary roomLibrary,
            RoomTemplate2D[] rooms)
        {
            library = roomLibrary;
            roomInstances = rooms ?? System.Array.Empty<RoomTemplate2D>();
            RefreshValidation();
        }

        [ContextMenu("Validate P4 Room Library")]
        public bool RefreshValidation()
        {
            RoomValidationReport report =
                RoomPrefabValidator.ValidateLibrary(roomInstances);
            lastValidation = report.ToString();
            return report.Passed;
        }
    }
}

#endif
