#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Stage.Layout.Authoring
{
    [DisallowMultipleComponent]
    public sealed class StageLayoutGridGuide : MonoBehaviour
    {
        [SerializeField, Min(8)] private int halfExtentCells = 56;
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.25f, 0.45f, 0.65f, 0.18f);
            float extent = halfExtentCells * StageRoomProxy.PreviewCellScale;
            float step = StageLayoutGraphUtility.PlacementSnapCells * StageRoomProxy.PreviewCellScale;
            for (float value = -extent; value <= extent; value += step)
            {
                Gizmos.DrawLine(new Vector3(value, -extent), new Vector3(value, extent));
                Gizmos.DrawLine(new Vector3(-extent, value), new Vector3(extent, value));
            }
        }
    }
}

#endif
