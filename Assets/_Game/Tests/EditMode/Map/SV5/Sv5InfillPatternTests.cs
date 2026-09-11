#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.SectorPlanning;
using StarNight.Map.WorldGeneration.SpecialRegions;

namespace StarNight.Map.Tests.EditMode.Sv5
{
    public sealed class Sv5InfillPatternTests
    {
        private static RmapSpecialWorldPoint P(int x,int y) => new RmapSpecialWorldPoint(x,y);

        [Test] public void T01_FourExactRecipesMirrorsDoorsSupportedStepsAndSolidWindows()
        {
            foreach (string recipe in Sv5InfillPatterns.Recipes)
            {
                var bounds = Sv5InfillPatterns.Bounds(recipe);
                var cells = Sv5InfillPatterns.Recipe(recipe,P(0,0)).ToDictionary(c => c.World,c => c.Value);
                var mirror = Sv5InfillPatterns.Recipe(recipe,P(0,0),true).ToDictionary(c => c.World,c => c.Value);
                Assert.That(cells.Count,Is.EqualTo(bounds.Width*bounds.Height));
                foreach (var cell in cells) Assert.That(mirror[P(bounds.Width-1-cell.Key.X,cell.Key.Y)],Is.EqualTo(cell.Value));
                for (int y=0;y<bounds.Height;y++) for(int x=0;x<bounds.Width;x++)
                {
                    int floor=recipe=="LANDING" ? 3+Math.Min(2,Math.Max(0,(x-3)/2)) : 3;
                    int ceiling=recipe=="SMALL_CAVE" ? (x<6 ? 8 : x<10 ? 9 : x<14 ? 8 : 7) : recipe=="LANDING" ? 12 : 8;
                    bool air=(x>=3 && x<bounds.Width-3 && y>=floor && y<=ceiling) || (x<3 && y>=3 && y<=4);
                    if(recipe=="DEAD_END" && x==12 && y>=6 && y<=8) air=false;
                    Assert.That(cells[P(x,y)],Is.EqualTo(air ? Sv5InfillCellValue.Air : Sv5InfillCellValue.Solid),recipe+":"+x+":"+y);
                }
                int targetY=recipe=="LANDING" ? 5 : 3;
                var proof=Sv5InfillPatterns.Screen(cells,P(0,3),P(bounds.Width-4,targetY));
                Assert.That(proof.Success,Is.True,recipe+" "+proof.Reason);
                Assert.That(proof.Approach.First(),Is.EqualTo(P(0,3)));
                Assert.That(proof.Return.Last(),Is.EqualTo(P(0,3)));
                Assert.That(proof.Approach.Zip(proof.Approach.Skip(1),(a,b)=>Math.Abs(a.Y-b.Y)).All(d=>d<=1),Is.True);
                Assert.That(Sv5InfillPatterns.SolidWindows(cells,bounds.Width,bounds.Height),Is.Empty);
            }
            var solid=Enumerable.Range(0,6).SelectMany(x=>Enumerable.Range(0,6).Select(y=>P(x,y))).ToDictionary(p=>p,p=>Sv5InfillCellValue.Solid);
            Assert.That(Sv5InfillPatterns.SolidWindows(solid,6,6),Is.EqualTo(new[]{P(0,0)}));
        }

        [Test] public void T02_PatternPayloadReconstructsAcrossFourTwelveAndFortyEightBoundariesWithoutOwningUnknown()
        {
            var cells=Sv5InfillPatterns.Recipe("LANDING",P(43,27)).ToArray();
            var instances=Sv5InfillPatterns.Split(cells);
            Assert.That(instances.Any(i=>i.Pattern.Mask!=ushort.MaxValue),Is.True);
            Assert.That(instances.All(i=>i.Pattern.Values.Count==16),Is.True);
            var reconstructed=Sv5InfillPatterns.Reconstruct(instances.Reverse());
            Assert.That(reconstructed.Count,Is.EqualTo(256));
            foreach(var c in cells) Assert.That(reconstructed[c.World],Is.EqualTo(c.Value));
            Assert.That(reconstructed.ContainsKey(P(42,27)),Is.False);
            Assert.That(reconstructed.Keys.Any(p=>p.X<48) && reconstructed.Keys.Any(p=>p.X>=48 && p.Y>=32),Is.True);
            Assert.Throws<ArgumentException>(()=>Sv5InfillPatterns.Reconstruct(instances.Concat(instances.Take(1))));
            Assert.Throws<ArgumentException>(()=>Sv5InfillPatterns.Split(cells.Concat(cells.Take(1))));
            Assert.That(Sv5InfillPatterns.Split(cells.Reverse()).Select(i=>i.Token),Is.EqualTo(instances.Select(i=>i.Token)));
        }

        [Test] public void T04_AirFloodCannotCertifyPlusThreeMissingFloorHeadroomClosedDoorOrReturn()
        {
            var original=Sv5InfillPatterns.Recipe("EMPTY_ROOM",P(0,0)).ToDictionary(c=>c.World,c=>c.Value);
            Assert.That(Sv5InfillPatterns.Screen(original,P(0,3),P(11,3)).Success,Is.True);
            foreach(string mutation in new[]{"PLUS_THREE","NO_FLOOR","HEADROOM","CLOSED_DOOR","NO_RETURN"})
            {
                var cells=original.ToDictionary(k=>k.Key,k=>k.Value); var goal=P(11,3);
                if(mutation=="PLUS_THREE")
                {
                    for(int x=7;x<=12;x++) for(int y=3;y<6;y++) cells[P(x,y)]=Sv5InfillCellValue.Solid;
                    goal=P(11,6);
                }
                if(mutation=="NO_FLOOR") for(int x=4;x<=8;x++) cells[P(x,2)]=Sv5InfillCellValue.Unknown;
                if(mutation=="HEADROOM") cells[P(6,4)]=Sv5InfillCellValue.Solid;
                if(mutation=="CLOSED_DOOR") { cells[P(1,3)]=Sv5InfillCellValue.Solid; cells[P(1,4)]=Sv5InfillCellValue.Solid; }
                if(mutation=="NO_RETURN")
                {
                    for(int x=7;x<=12;x++) for(int y=0;y<=2;y++) cells[P(x,y)]=y==0 ? Sv5InfillCellValue.Solid : Sv5InfillCellValue.Air;
                    goal=P(11,1);
                }
                Assert.That(Sv5InfillPatterns.Screen(cells,P(0,3),goal).Success,Is.False,mutation);
            }
        }

        [Test] public void T02_CardinalAirLengthCountsBothAxesOfEveryPlusOneStep()
        {
            var feet=Enumerable.Range(0,13).Select(i=>P(i,3+i)).ToArray();
            var air=Sv5SpaceInfill.CardinalCenterline(feet);
            Assert.That(feet.Length,Is.EqualTo(13),"Support positions are NOT the connector length unit.");
            Assert.That(air.Count,Is.EqualTo(25));
            Assert.That(air.Count,Is.GreaterThan(new Sv5InfillProfile().MaximumLink));
            for(int i=1;i<air.Count;i++)
                Assert.That(Math.Abs(air[i].X-air[i-1].X)+Math.Abs(air[i].Y-air[i-1].Y),Is.EqualTo(1));
            Assert.That(Sv5SpaceInfill.CardinalCenterline(feet.Take(12)).Count,Is.EqualTo(23));
            Assert.That(Sv5SpaceInfill.CardinalCenterline(feet.Reverse()),Is.EqualTo(air.Reverse()));
            Assert.Throws<ArgumentException>(()=>Sv5SpaceInfill.CardinalCenterline(new[]{P(0,3),P(1,6)}));
        }
    }
}
#endif
