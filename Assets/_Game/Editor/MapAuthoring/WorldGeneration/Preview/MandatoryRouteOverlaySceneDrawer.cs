using System.Collections.Generic;
using StarNight.Map.WorldGeneration.Diagnostics;
using UnityEditor;
using UnityEngine;
namespace StarNight.MapAuthoring.Editor.WorldGeneration.Preview
{
    public sealed class MandatoryRouteOverlayDrawCommand
    {
        internal MandatoryRouteOverlayDrawCommand(int index,Vector3 position,string label){Index=index;Position=position;Label=label;}
        public int Index{get;} public Vector3 Position{get;} public string Label{get;}
    }
    public static class MandatoryRouteOverlaySceneDrawer
    {
        public static Vector3 ToWorldPosition(int index){var c=StarNight.Map.WorldGeneration.Generation.WorldGridIndex.ToCoordinate(index);return new Vector3(c.X*48f,c.Y*32f,0f);}
        public static IReadOnlyList<MandatoryRouteOverlayDrawCommand> BuildDrawCommands(MandatoryRouteOverlaySnapshot snapshot){var list=new List<MandatoryRouteOverlayDrawCommand>();if(snapshot==null)return list.AsReadOnly();foreach(var cell in snapshot.Cells)list.Add(new MandatoryRouteOverlayDrawCommand(cell.Index,ToWorldPosition(cell.Index),cell.Label));return list.AsReadOnly();}
        [DrawGizmo(GizmoType.Active|GizmoType.Selected|GizmoType.NonSelected)] public static void DrawMandatoryRouteOverlay(MandatoryRouteOverlay overlay,GizmoType gizmoType){if(overlay==null||!overlay.enabled||!overlay.gameObject.activeInHierarchy||!overlay.HasSnapshot||Event.current==null||SceneView.currentDrawingSceneView==null)return;Handles.BeginGUI();try{var view=SceneView.currentDrawingSceneView;MandatoryRouteOverlayGui.Draw(overlay.Snapshot,Event.current.mousePosition,view.position.width,view.position.height);}finally{Handles.EndGUI();}}
    }
}
