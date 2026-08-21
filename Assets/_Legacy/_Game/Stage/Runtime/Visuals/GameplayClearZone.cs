#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Stage.Visuals
{
    [DisallowMultipleComponent]
    public sealed class GameplayClearZone : MonoBehaviour
    {
        [SerializeField] private Vector2 sizeCells = new Vector2(2f, 3f);
        [SerializeField] private Vector2 offsetCells;

        public Vector2 SizeCells => sizeCells;
        public Vector2 OffsetCells => offsetCells;
        public Bounds WorldBounds => new Bounds(transform.TransformPoint(offsetCells), sizeCells);

        public void Configure(Vector2 size, Vector2 offset = default)
        {
            sizeCells = new Vector2(Mathf.Max(0.01f, size.x), Mathf.Max(0.01f, size.y));
            offsetCells = offset;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.76f, 0.15f, 0.35f);
            Gizmos.DrawWireCube(WorldBounds.center, WorldBounds.size);
        }
#endif
    }
}

#endif
