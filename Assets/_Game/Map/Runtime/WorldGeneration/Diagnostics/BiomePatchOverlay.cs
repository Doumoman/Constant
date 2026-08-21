using System;
using StarNight.Map.WorldGeneration.Generation;
using UnityEngine;

namespace StarNight.Map.WorldGeneration.Diagnostics
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("WorldGen/Biome Patch Overlay")]
    public sealed class BiomePatchOverlay : MonoBehaviour
    {
        [NonSerialized]
        private BiomePatchOverlaySnapshot snapshot;

        public bool HasSnapshot => snapshot != null;
        public BiomePatchOverlaySnapshot Snapshot => snapshot;

        public void SetSnapshot(BiomePatchValidationPublication publication)
        {
            var nextSnapshot = BiomePatchOverlaySnapshot.Create(publication);
            snapshot = nextSnapshot;
        }

        public void ClearSnapshot()
        {
            snapshot = null;
        }

        private void OnGUI()
        {
            var currentEvent = Event.current;
            if (!enabled || !gameObject.activeInHierarchy || snapshot == null || currentEvent == null)
                return;

            BiomePatchOverlayGui.Draw(
                snapshot,
                currentEvent.mousePosition,
                Screen.width,
                Screen.height);
        }
    }
}
