#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Stage.Rooms
{
    [DisallowMultipleComponent]
    public sealed class ObjectRecoveryCell : MonoBehaviour
    {
        public Vector2 Position => transform.position;
    }
}

#endif
