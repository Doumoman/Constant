#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Map
{
    [Serializable]
    public sealed class ElementSnapshot
    {
        public string RuntimeId;
        public string ElementId;
        public MapElementState State;
        public MapElementState SuspendedState;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation = Quaternion.identity;
        public Vector2 LinearVelocity;
        public float AngularVelocity;
        public float StateElapsedSeconds;
        public bool OccupancyRegistered;
    }
}

#endif
