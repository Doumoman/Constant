using System;
using StarNight.Map.WorldGeneration.Generation;
using UnityEngine;

namespace StarNight.Map.WorldGeneration.Diagnostics
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("WorldGen/Site Reservation Overlay")]
    public sealed class SiteReservationOverlay : MonoBehaviour
    {
        [NonSerialized]
        private SiteReservationOverlaySnapshot snapshot;

        public bool HasSnapshot => snapshot != null;
        public SiteReservationOverlaySnapshot Snapshot => snapshot;

        public void SetSnapshot(
            SiteReservationPublication publication,
            SiteReservationSearchDiagnostics searchDiagnostics,
            CoreCapacityFloodDiagnostics capacityDiagnostics,
            VillageReservationDiagnostics villageDiagnostics,
            SiteReservationValidationDiagnostics validationDiagnostics)
        {
            var nextSnapshot = SiteReservationOverlaySnapshot.Create(
                publication,
                searchDiagnostics,
                capacityDiagnostics,
                villageDiagnostics,
                validationDiagnostics);
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

            SiteReservationOverlayGui.Draw(
                snapshot,
                currentEvent.mousePosition,
                Screen.width,
                Screen.height);
        }
    }
}
