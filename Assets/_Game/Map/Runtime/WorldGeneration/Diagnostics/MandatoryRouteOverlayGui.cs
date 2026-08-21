using System;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;
using UnityEngine;

namespace StarNight.Map.WorldGeneration.Diagnostics
{
    public static class MandatoryRouteOverlayGui
    {
        public const int GridColumns=13, GridRows=13, CellSize=48, CompactCellSize=24; public const int RequiredViewportWidth=900, RequiredViewportHeight=760;
        public const string SmallViewportText="Mandatory route overlay: compact 13 x 13 view.";
        public static Rect GetCellRect(int index,bool compact=false){var c=WorldGridIndex.ToCoordinate(index);var size=compact?CompactCellSize:CellSize;return new Rect(20+c.X*size,56+(12-c.Y)*size,size,size);}
        public static Color32 GetRouteColor(int type){switch(type){case 1:return new Color32(45,140,220,235);case 2:return new Color32(70,195,230,235);case 3:return new Color32(130,110,235,235);case 4:return new Color32(240,155,55,235);default:throw new ArgumentOutOfRangeException(nameof(type));}}
        public static void Draw(MandatoryRouteOverlaySnapshot snapshot,Vector2 mouse,float width,float height)
        {
            if(snapshot==null)throw new ArgumentNullException(nameof(snapshot)); var compact=width<RequiredViewportWidth||height<RequiredViewportHeight;
            GUI.Label(new Rect(20,16,700,32),compact?SmallViewportText:snapshot.ValidationBanner);
            for(var i=0;i<WorldGenConstants.SectorCount;i++){var rect=GetCellRect(i,compact); if(snapshot.TryGetCell(i,out var cell)){var old=GUI.backgroundColor;GUI.backgroundColor=GetRouteColor(cell.RouteType);GUI.Box(rect,cell.Label);GUI.backgroundColor=old;}else GUI.Box(rect,string.Empty);}
        }
    }
}
