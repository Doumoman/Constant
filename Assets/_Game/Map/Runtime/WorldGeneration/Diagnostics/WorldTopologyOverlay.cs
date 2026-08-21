using System;
using StarNight.Map.WorldGeneration.Generation;
using UnityEngine;

namespace StarNight.Map.WorldGeneration.Diagnostics
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("WorldGen/World Topology Overlay")]
    public sealed class WorldTopologyOverlay : MonoBehaviour
    {
        [NonSerialized]
        private WorldTopologyOverlaySnapshot snapshot;

        public bool HasSnapshot => snapshot != null;
        public WorldTopologyOverlaySnapshot Snapshot => snapshot;

        public void SetSnapshot(GridInitializationResult result)
        {
            var nextSnapshot = WorldTopologyOverlaySnapshot.Create(result);
            snapshot = nextSnapshot;
        }

        public void ClearSnapshot()
        {
            snapshot = null;
        }

        private void OnGUI()
        {
            var currentEvent = Event.current;
            if (!enabled ||
                !gameObject.activeInHierarchy ||
                snapshot == null ||
                currentEvent == null)
            {
                return;
            }

            WorldTopologyOverlayGui.Draw(
                snapshot,
                currentEvent.mousePosition,
                Screen.width,
                Screen.height);
        }
    }
}
