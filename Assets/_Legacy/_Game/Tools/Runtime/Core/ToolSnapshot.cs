#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Tools.Core
{
    [Serializable]
    public sealed class ToolSnapshot
    {
        public string ToolId;
        public Vector2 Position;
        public float Rotation;
        public int CurrentResource;
        public int MaximumResource;
        public bool Active;
    }
}

#endif
