#if LEGACY_DISABLED
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.World
{
    [DisallowMultipleComponent]
    public sealed class RoomBounds2D : MonoBehaviour
    {
        private static readonly List<RoomBounds2D> ActiveRoomsInternal =
            new List<RoomBounds2D>();

        [SerializeField] private string roomId = string.Empty;
        [SerializeField] private Rect worldRect = new Rect(0f, 0f, 12f, 8f);
        [SerializeField] private bool isMainRoute = true;

        public static IReadOnlyList<RoomBounds2D> ActiveRooms
        {
            get
            {
                PruneRegistry();
                return ActiveRoomsInternal;
            }
        }

        public string RoomId => roomId;
        public Rect WorldRect => worldRect;
        public bool IsMainRoute => isMainRoute;
        public Vector2 Center => worldRect.center;

        public void Configure(string id, Rect rect, bool mainRoute = true)
        {
            roomId = id ?? string.Empty;
            worldRect = Normalize(rect);
            isMainRoute = mainRoute;
            Register();
        }

        public bool Contains(Vector2 worldPoint)
        {
            return ContainsWithMargin(worldPoint, 0f);
        }

        public bool ContainsWithMargin(Vector2 worldPoint, float margin)
        {
            return worldPoint.x >= worldRect.xMin - margin
                && worldPoint.x <= worldRect.xMax + margin
                && worldPoint.y >= worldRect.yMin - margin
                && worldPoint.y <= worldRect.yMax + margin;
        }

        public static RoomBounds2D FindContaining(Vector2 worldPoint)
        {
            PruneRegistry();
            RoomBounds2D best = null;
            float bestDistance = float.MaxValue;
            for (int index = 0; index < ActiveRoomsInternal.Count; index++)
            {
                RoomBounds2D room = ActiveRoomsInternal[index];
                if (!room.Contains(worldPoint))
                {
                    continue;
                }

                float distance = (room.Center - worldPoint).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = room;
                }
            }

            return best;
        }

        private void OnEnable()
        {
            Register();
        }

        private void OnDisable()
        {
            ActiveRoomsInternal.Remove(this);
        }

        private void Register()
        {
            if (!ActiveRoomsInternal.Contains(this))
            {
                ActiveRoomsInternal.Add(this);
            }
        }

        private static void PruneRegistry()
        {
            for (int index = ActiveRoomsInternal.Count - 1; index >= 0; index--)
            {
                RoomBounds2D room = ActiveRoomsInternal[index];
                if (room == null || !room.isActiveAndEnabled)
                {
                    ActiveRoomsInternal.RemoveAt(index);
                }
            }
        }

        private static Rect Normalize(Rect rect)
        {
            return Rect.MinMaxRect(
                Mathf.Min(rect.xMin, rect.xMax),
                Mathf.Min(rect.yMin, rect.yMax),
                Mathf.Max(rect.xMin, rect.xMax),
                Mathf.Max(rect.yMin, rect.yMax));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isMainRoute
                ? new Color(1f, 0.85f, 0.3f, 0.8f)
                : new Color(0.5f, 0.7f, 1f, 0.8f);
            Gizmos.DrawWireCube(worldRect.center, worldRect.size);
        }
    }
}

#endif
