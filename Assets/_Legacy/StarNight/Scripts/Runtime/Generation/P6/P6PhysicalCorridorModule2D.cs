#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Generation.P6
{
    [Flags]
    public enum P6CorridorConnectionSides
    {
        None = 0,
        Left = 1 << 0,
        Right = 1 << 1,
        Down = 1 << 2,
        Up = 1 << 3
    }

    public enum P6CorridorModuleKind
    {
        End = 0,
        Horizontal = 1,
        VerticalStairs = 2,
        Corner = 3,
        Junction = 4,
        AccessBridge = 5
    }

    [DisallowMultipleComponent]
    public sealed class P6PhysicalCorridorModule2D : MonoBehaviour
    {
        [SerializeField] private Vector2Int routingCell;
        [SerializeField] private P6CorridorModuleKind moduleKind;
        [SerializeField] private P6CorridorConnectionSides connections;
        [SerializeField] private bool accessBridge;
        [SerializeField] private int accessId = -1;
        [SerializeField] private BoxCollider2D[] surfaces =
            Array.Empty<BoxCollider2D>();

        public Vector2Int RoutingCell => routingCell;
        public P6CorridorModuleKind ModuleKind => moduleKind;
        public P6CorridorConnectionSides Connections => connections;
        public bool IsAccessBridge => accessBridge;
        public int AccessId => accessId;
        public IReadOnlyList<BoxCollider2D> Surfaces => surfaces;
        public bool HasPhysicalCollision
        {
            get
            {
                if (surfaces == null || surfaces.Length == 0)
                {
                    return false;
                }

                for (int index = 0; index < surfaces.Length; index++)
                {
                    BoxCollider2D surface = surfaces[index];
                    if (surface != null
                        && surface.enabled
                        && !surface.isTrigger)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void Configure(
            Vector2Int cell,
            P6CorridorModuleKind kind,
            P6CorridorConnectionSides connectedSides,
            bool isAccessBridge,
            int roomAccessId,
            BoxCollider2D[] physicalSurfaces)
        {
            routingCell = cell;
            moduleKind = kind;
            connections = connectedSides;
            accessBridge = isAccessBridge;
            accessId = isAccessBridge ? roomAccessId : -1;
            surfaces = physicalSurfaces
                ?? Array.Empty<BoxCollider2D>();
        }
    }
}

#endif
