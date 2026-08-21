using System;
using StarNight.Map.WorldGeneration.Generation;
using UnityEngine;
namespace StarNight.Map.WorldGeneration.Diagnostics
{
    [ExecuteAlways,DisallowMultipleComponent,AddComponentMenu("WorldGen/Mandatory Route Overlay")]
    public sealed class MandatoryRouteOverlay:MonoBehaviour
    {
        [NonSerialized] private MandatoryRouteOverlaySnapshot snapshot;
        public bool HasSnapshot=>snapshot!=null; public MandatoryRouteOverlaySnapshot Snapshot=>snapshot;
        public void SetSnapshot(MandatoryRouteValidationReport report){snapshot=MandatoryRouteOverlaySnapshot.Create(report);} public void ClearSnapshot(){snapshot=null;}
        private void OnGUI(){if(!enabled||!gameObject.activeInHierarchy||snapshot==null||Event.current==null)return;MandatoryRouteOverlayGui.Draw(snapshot,Event.current.mousePosition,Screen.width,Screen.height);}
    }
}
