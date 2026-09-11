#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using StarNight.Map.WorldGeneration.SectorPlanning;
using StarNight.Map.WorldGeneration.SpecialRegions;

namespace StarNight.Map.Tests.EditMode.Sv5
{
    public sealed class Sv5InfillLengthFix01Tests
    {
        private static string Root => Path.GetFullPath(Path.Combine(Application.dataPath,".."));
        private static RmapSpecialWorldPoint P(int x,int y) => new RmapSpecialWorldPoint(x,y);
        private static RmapSpecialWorldPoint[] Horizontal(int count) => Enumerable.Range(0,count).Select(x=>P(x,0)).ToArray();

        [Test] public void F01_InclusiveBoundaryAcceptsTwentyThreeAndTwentyFourButRejectsTwentyFiveAndTwentySix()
        {
            var access=new[]{P(-1,0),P(0,0)};
            foreach(int count in new[]{23,24})
            {
                var path=Horizontal(count-1);
                Assert.That(Sv5SpaceInfill.ConnectionCenterline(path,access).Count,Is.EqualTo(count));
                Assert.That(Sv5SpaceInfill.FindConnectionLengthErrors(path,access,24,true),Is.Empty,"count="+count);
            }
            foreach(int count in new[]{25,26})
            {
                var path=Horizontal(count-1);
                var errors=Sv5SpaceInfill.FindConnectionLengthErrors(path,access,24,true);
                Assert.That(Sv5SpaceInfill.ConnectionCenterline(path,access).Count,Is.EqualTo(count));
                Assert.That(errors,Does.Contain("OVER_MAXIMUM|"+count+"/24"),"count="+count);
            }
        }

        [Test] public void F02_RootNeckSidePortalAndChildCountOnlyTheirDefinedEndpoints()
        {
            var neck=Sv5SpaceInfill.ConnectionCenterline(new[]{P(0,0),P(1,0)},new[]{P(0,1),P(0,0)});
            Assert.That(neck,Is.EqualTo(new[]{P(0,1),P(0,0),P(1,0)}));
            var side=Sv5SpaceInfill.ConnectionCenterline(new[]{P(0,0),P(1,0)},new[]{P(-2,0),P(-1,0),P(0,0)});
            Assert.That(side,Is.EqualTo(new[]{P(-2,0),P(-1,0),P(0,0),P(1,0)}));
            var child=Sv5SpaceInfill.ConnectionCenterline(new[]{P(10,2),P(11,2),P(12,2)});
            Assert.That(child,Is.EqualTo(new[]{P(10,2),P(11,2),P(12,2)}));
            var parentInteriorWitness=Enumerable.Range(0,9).Select(x=>P(1+x,2)).ToArray();
            Assert.That(child,Has.None.EqualTo(parentInteriorWitness[0]),"Parent Entry-to-exit witness is a separate traversal.");
        }

        [Test] public void F03_CardinalExpansionJoinFailuresAndRevisitsCannotHideLength()
        {
            var expanded=Sv5SpaceInfill.CardinalCenterline(new[]{P(0,0),P(1,1)});
            Assert.That(expanded,Is.EqualTo(new[]{P(0,0),P(0,1),P(1,1)}));
            Assert.That(Sv5SpaceInfill.FindConnectionLengthErrors(expanded,Array.Empty<RmapSpecialWorldPoint>(),24,false),Is.Empty);
            Assert.That(Sv5SpaceInfill.FindConnectionLengthErrors(new[]{P(0,0),P(1,1)},Array.Empty<RmapSpecialWorldPoint>(),24,false),
                Does.Contain("NON_CARDINAL_STEP"));
            Assert.That(Sv5SpaceInfill.FindConnectionLengthErrors(Array.Empty<RmapSpecialWorldPoint>(),Array.Empty<RmapSpecialWorldPoint>(),24,false),
                Does.Contain("EMPTY_PATH"));
            Assert.That(Sv5SpaceInfill.FindConnectionLengthErrors(new[]{P(0,0),P(1,0)},new[]{P(-1,0),P(9,0)},24,true),
                Does.Contain("ROOT_JOIN_MISMATCH"));
            Assert.That(Sv5SpaceInfill.FindConnectionLengthErrors(new[]{P(0,0),P(1,0)},new[]{P(0,0)},24,false),
                Does.Contain("CHILD_HAS_HOST_ACCESS"));
            var revisit=new[]{P(0,0),P(1,0),P(0,0),P(1,0)};
            Assert.That(Sv5SpaceInfill.ConnectionCenterline(revisit).Count,Is.EqualTo(4));
            Assert.That(Sv5SpaceInfill.ConnectionCenterline(revisit).Distinct().Count(),Is.EqualTo(2));
        }

        [Test] public void F04_RecordedTwentySixCellCounterexampleExposesTheOldOwnershipFilteredMetric()
        {
            var full=new[]{P(571,399),P(572,399),P(572,398),P(573,398),P(573,397),P(574,397),P(574,396),
                P(575,396),P(575,395),P(576,395),P(576,394),P(577,394),P(577,393),P(578,393),P(578,392),
                P(579,392),P(579,391),P(580,391),P(581,391),P(582,391),P(583,391),P(584,391),P(585,391),
                P(586,391),P(587,391),P(588,391)};
            var oldOwnershipFiltered=full.Skip(1).Take(24).ToArray();
            Assert.That(oldOwnershipFiltered.Length,Is.EqualTo(24));
            Assert.That(full.Length,Is.EqualTo(26));
            Assert.That(Sv5SpaceInfill.FindConnectionLengthErrors(full,Array.Empty<RmapSpecialWorldPoint>(),24,false),
                Does.Contain("OVER_MAXIMUM|26/24"));
        }

        [Test, Timeout(1200000)] public void F05_DefaultAndRepeatReconstructEveryActualConnectorAtTwentyFourOrLess()
        {
            foreach(var pair in ActualCases())
            {
                var baseline=pair.Before; var plan=pair.After; var payload=plan.Infill;
                var oldAir=baseline.Connections.SelectMany(c=>c.Centerline.Concat(c.ApertureCells)).ToHashSet();
                var newAir=payload.Cells.Where(c=>c.Value==Sv5InfillCellValue.Air).Select(c=>c.World).ToHashSet();
                foreach(var room in payload.Rooms.Where(r=>!r.Legacy))
                {
                    var link=payload.Links.Single(l=>l.Room==room.Id);
                    var parent=payload.Rooms.SingleOrDefault(r=>r.Id==room.Parent);
                    var independent=(link.HostAccess.Count==0 ? link.Path : link.HostAccess.Concat(link.Path.Skip(1))).ToArray();
                    Assert.That(link.ConnectionCenterline,Is.EqualTo(independent),room.Id);
                    Assert.That(independent.Length,Is.InRange(1,24),room.Id);
                    Assert.That(independent.Last(),Is.EqualTo(room.Entry),room.Id);
                    Assert.That(independent.Zip(independent.Skip(1),(a,b)=>Math.Abs(a.X-b.X)+Math.Abs(a.Y-b.Y)),Is.All.EqualTo(1),room.Id);
                    Assert.That(independent.All(point=>oldAir.Contains(point)||newAir.Contains(point)),Is.True,room.Id);
                    if(parent==null)
                        Assert.That(baseline.Connections.Single(c=>c.Id==room.Host).Centerline,Does.Contain(independent[0]),room.Id);
                    else
                        Assert.That(parent.Bounds.Contains(independent[0]) && (independent[0].X==parent.Bounds.X ||
                            independent[0].X==parent.Bounds.MaxXExclusive-1 || independent[0].Y==parent.Bounds.Y ||
                            independent[0].Y==parent.Bounds.MaxYExclusive-1),Is.True,room.Id);
                }
                Assert.That(Sv5SpaceInfill.ValidatePayload(baseline,payload),Is.Empty,string.Join("\n",payload.Diagnostics));
            }
        }

        [Test, Timeout(1200000)] public void F06_ExportsBindAuthoritativeLengthPolicyCountsAndPlanDigest()
        {
            var plan=ActualPlan("DefaultOn"); var payload=plan.Infill;
            string directory=Path.Combine(Root,"MapDesign/MCP/GENERATED/SV5_08_FIX01/_work/fix01_f06/default");
            Sv5InfillExport.WriteCase(plan,directory);
            string links=File.ReadAllText(Path.Combine(directory,"infill_links.csv"));
            string infill=File.ReadAllText(Path.Combine(directory,"infill.json"));
            string validation=File.ReadAllText(Path.Combine(directory,"infill_validation.json"));
            Assert.That(links.Split('\n')[0],Does.Contain("connection_centerline,connection_cell_count,length_policy,length_status"));
            Assert.That(links,Does.Contain(Sv5InfillProfile.ConnectionLengthPolicy).And.Contain(plan.Digest));
            Assert.That(infill,Does.Contain(Sv5InfillProfile.ConnectionLengthPolicy).And.Contain(payload.Digest).And.Contain(plan.Digest));
            Assert.That(validation,Does.Contain("\"CONNECTION_LENGTH\":true").And.Contain("\"over_length_count\":0")
                .And.Contain("\"endpoint_error_count\":0").And.Contain(plan.Digest));
            Assert.That(new Sv5InfillProfile(target:257).Digest,Is.Not.EqualTo(payload.Profile.Digest));
        }

        [Test] public void F07_AllNinetyFivePredecessorTestsRemainDiscoverableAndEnabled()
        {
            string json=File.ReadAllText(Path.Combine(Root,"MapDesign/MCP/INPUTS/SV5_08_FIX01/LENGTH_EVIDENCE.json"));
            var evidence=JsonUtility.FromJson<EvidenceDto>(json);
            Assert.That(evidence.prior_focused_fullnames.Length,Is.EqualTo(95));
            Assert.That(evidence.prior_focused_fullnames.Distinct().Count(),Is.EqualTo(95));
            foreach(string fullName in evidence.prior_focused_fullnames)
            {
                int split=fullName.LastIndexOf('.');
                var type=typeof(Sv5InfillLengthFix01Tests).Assembly.GetType(fullName.Substring(0,split),true);
                var method=type.GetMethod(fullName.Substring(split+1));
                Assert.That(method,Is.Not.Null,fullName);
                Assert.That(method.GetCustomAttributes(typeof(TestAttribute),true),Is.Not.Empty,fullName);
                Assert.That(method.GetCustomAttributes(typeof(IgnoreAttribute),true),Is.Empty,fullName);
            }
        }

        private static IEnumerable<(Sv5SpaceGraphPlan Before,Sv5SpaceGraphPlan After)> ActualCases()
        {
            yield return (ActualPlan("Baseline"),ActualPlan("DefaultOn"));
            yield return (ActualPlan("RepeatBaseline"),ActualPlan("RepeatOn"));
        }

        private static Sv5SpaceGraphPlan ActualPlan(string fieldName)
        {
            var field=typeof(Sv5SpaceInfillTests).GetField(fieldName,BindingFlags.Static|BindingFlags.NonPublic);
            Assert.That(field,Is.Not.Null,fieldName);
            return ((Lazy<Sv5SpaceGraphPlan>)field.GetValue(null)).Value;
        }

        [Serializable] private sealed class EvidenceDto { public string[] prior_focused_fullnames; }
    }
}
#endif
