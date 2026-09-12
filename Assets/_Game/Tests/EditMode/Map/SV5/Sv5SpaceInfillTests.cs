#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using StarNight.Map.WorldGeneration.SectorPlanning;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.MicroPatterns;

namespace StarNight.Map.Tests.EditMode.Sv5
{
    public sealed class Sv5SpaceInfillTests
    {
        private static readonly Lazy<Sv5SpaceGraphPlan> DefaultOn=new Lazy<Sv5SpaceGraphPlan>(()=>
            Sv5SpaceGraphPlanner.PlanWithInfill(Sv5RouteStatePolicyTests.RepresentativePlanForFix01,1304));
        private static readonly Lazy<Sv5SpaceGraphPlan> RepeatBaseline=new Lazy<Sv5SpaceGraphPlan>(()=>
            Sv5SpaceGraphPlanner.Plan(Sv5RouteStatePolicyTests.RepresentativePlanForFix01,1304,Sv5SpaceDiversity.RepeatProfile()));
        private static readonly Lazy<Sv5SpaceGraphPlan> RepeatOn=new Lazy<Sv5SpaceGraphPlan>(()=>
            Sv5SpaceGraphPlanner.PlanWithInfill(Sv5RouteStatePolicyTests.RepresentativePlanForFix01,1304,Sv5SpaceDiversity.RepeatProfile()));
        private static string Root => Path.GetFullPath(Path.Combine(Application.dataPath,".."));
        private static System.Collections.Generic.IEnumerable<(Sv5SpaceGraphPlan Before,Sv5SpaceGraphPlan After)> Cases()
        { yield return (Baseline.Value,DefaultOn.Value); yield return (RepeatBaseline.Value,RepeatOn.Value); }
        private static readonly Lazy<Sv5SpaceGraphPlan> Baseline=new Lazy<Sv5SpaceGraphPlan>(()=>
            Sv5SpaceGraphPlanner.Plan(Sv5RouteStatePolicyTests.RepresentativePlanForFix01,1304));
        // Mutation fixtures use the existing internal immutable constructors without widening production APIs.
        private static T Construct<T>(params object[] arguments) => (T)Activator.CreateInstance(typeof(T),
            System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic,null,arguments,
            System.Globalization.CultureInfo.InvariantCulture);
        private static Sv5SpaceGraphPlan Reverse(Sv5SpaceGraphPlan p,
            System.Collections.Generic.IEnumerable<Sv5SpaceReservationCell> reservations=null) =>
            Construct<Sv5SpaceGraphPlan>(p.Core,p.Seed,p.Profile,p.Places.Reverse(),p.Ports.Reverse(),p.Connections.Reverse(),
                // Preserve the historical reservation ledger: equal sort keys can contain ordered
                // Passage/Clearance provenance rows (e.g. 244,32). Do not rewrite the pinned 07 digest.
                p.Gates.Reverse(),reservations ?? p.Reservations,p.ContactDecisions.Reverse(),p.ProjectionProofs.Reverse(),
                // Selection trace is ordered historical evidence, not an unordered input collection.
                p.GateStateChecks.Reverse(),p.Diagnostics.Reverse(),p.Diversity.Profile,p.Diversity.Decisions);

        [Test] public void T03_RealGenerationIsDeterministicAndExhaustedCandidatesCannotPassDensity()
        {
            var baseline=Baseline.Value;
            // Small generation fixture only; the two integration profiles retain all frozen defaults.
            var profile=new Sv5InfillProfile(target:8,minimum:8,maximum:8,minimumSectors:0,roomsPerSector:6,minimumTiles:0);
            var state=UnityEngine.Random.state;
            try
            {
                UnityEngine.Random.InitState(17);
                var first=Sv5SpaceInfill.Build(baseline,profile);
                UnityEngine.Random.InitState(98213);
                var reversed=Reverse(baseline);
                var canonical=typeof(Sv5SpaceGraphPlan).GetMethod("CanonicalLines",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                var left=(System.Collections.Generic.IEnumerable<string>)canonical.Invoke(baseline,null);
                var right=(System.Collections.Generic.IEnumerable<string>)canonical.Invoke(reversed,null);
                string firstDifference=left.Zip(right,(a,b)=>a==b ? "" : a.Substring(0,Math.Min(a.Length,240))+"\n"+b.Substring(0,Math.Min(b.Length,240)))
                    .FirstOrDefault(line=>line.Length>0) ?? "NO_TOKEN_DIFFERENCE";
                Assert.That(reversed.Digest,Is.EqualTo(baseline.Digest),firstDifference);
                var second=Sv5SpaceInfill.Build(reversed,profile);
                Assert.That(first.Success,Is.True,string.Join("\n",first.Diagnostics));
                Assert.That(first.NewRoomCount,Is.EqualTo(8));
                Assert.That(second.Rooms.Select(r=>r.Token),Is.EqualTo(first.Rooms.Select(r=>r.Token)));
                Assert.That(second.Cells.Select(c=>c.Token),Is.EqualTo(first.Cells.Select(c=>c.Token)));
                Assert.That(second.Digest,Is.EqualTo(first.Digest));
            }
            finally { UnityEngine.Random.state=state; }
            var blocked=Reverse(baseline,Enumerable.Range(0,416).SelectMany(y=>Enumerable.Range(0,624).Select(x=>
                Construct<Sv5SpaceReservationCell>(new RmapSpecialWorldPoint(x,y),Sv5SpaceReservationKind.CorridorClearance,
                    "EXHAUSTION_FIXTURE","EXPLICIT_NON_WRITABLE_TEST_RESERVATION"))));
            var empty=Sv5SpaceInfill.Build(blocked);
            Assert.That(empty.NewRoomCount,Is.Zero);
            Assert.That(empty.Success,Is.False);
            Assert.That(empty.Termination,Is.EqualTo("CANDIDATES_EXHAUSTED"));
            Assert.That(empty.Rejections["ROOM_RESERVED_FOOTPRINT"],Is.GreaterThan(0));
            Assert.That(empty.Diagnostics.Any(d=>d.StartsWith("MINIMUM_ROOMS|0/128")),Is.True);
        }

        [Test] public void T05_ProductionProtectionRejectsPassageClearanceCoreType0AndForeignHost()
        {
            var p=Baseline.Value;
            var host=p.Connections.First(c=>c.Kind==Sv5SpaceConnectionKind.OptionalBranch && c.Flow=="BIDIRECTIONAL");
            string[] Check(RmapSpecialWorldPoint point,Sv5InfillCellValue value,string legacy="") =>
                Sv5SpaceInfill.ValidateCandidate(p,new[]{new Sv5InfillCell(point,value,"T05","FIXTURE",host:host.Id)},
                    Array.Empty<Sv5SpaceBoundaryFace>(),host.Id,legacy).ToArray();
            Assert.That(Check(host.Centerline.First(),Sv5InfillCellValue.Solid).Any(e=>e.StartsWith("SOLID_RESERVED_PASSAGE_CLEARANCE|")),Is.True);
            var passage=p.Connections.SelectMany(c=>c.Centerline.Concat(c.ApertureCells)).ToHashSet();
            var clearance=host.Envelope.First(c=>!passage.Contains(c));
            Assert.That(Check(clearance,Sv5InfillCellValue.Solid).Any(e=>e.StartsWith("SOLID_RESERVED_PASSAGE_CLEARANCE|")),Is.True);
            var coreAir=p.Core.CoreCells.First(c=>c.BaseCell==RmapPatternBaseCell.Air).World;
            Assert.That(Check(coreAir,Sv5InfillCellValue.Solid).Any(e=>e.StartsWith("CORE_PROTECTED|")),Is.True);
            Assert.That(p.Core.RouteSource.Secrets,Is.Not.Empty,"Fixture must include actual sealed Type0 geometry.");
            var secret=p.Core.RouteSource.Secrets.First();
            Assert.That(Check(secret.BreakableAccess,Sv5InfillCellValue.Air).Any(e=>e.StartsWith("TYPE0_PROTECTED|")),Is.True);
            var foreign=p.Connections.First(c=>c.Id!=host.Id);
            var from=foreign.Centerline.First(); var to=foreign.Centerline.Skip(1).First();
            Assert.That(Sv5SpaceInfill.ValidateCandidate(p,new[]{new Sv5InfillCell(from,Sv5InfillCellValue.Air,"T05","FIXTURE",host:host.Id,shared:true)},
                new[]{Construct<Sv5SpaceBoundaryFace>(from,to)},host.Id).Any(e=>e.StartsWith("DECLARED_FACE_FOREIGN_HOST|")),Is.True);
            var large=p.Places.First(c=>c.Kind==Sv5SpacePlaceKind.Large);
            Assert.That(Check(new RmapSpecialWorldPoint(large.Bounds.X,large.Bounds.Y),Sv5InfillCellValue.Air,large.Id)
                .Any(e=>e.StartsWith("INVALID_LEGACY_ORDINARY_SCOPE|")),Is.True);
        }

        [Test, Timeout(1200000)] public void T06_DefaultConnectedCellGenerationMeetsFrozenDensityAndLegacyCompletion()
        {
            var plan=DefaultOn.Value;
            var payload=plan.Infill;
            string detail="rooms="+payload.NewRoomCount+" area="+payload.NewOwnedTiles+" sectors="+string.Join(",",payload.SectorCounts)+
                "\n"+string.Join("\n",payload.Diagnostics)+"\n"+string.Join("\n",payload.Rejections.Select(p=>p.Key+"="+p.Value))+
                "\nplan_errors="+plan.Diagnostics.Count+"\n"+string.Join("\n",plan.Diagnostics.Take(24))+
                "\nphysical_product="+plan.PhysicalProduct.Success+"\nplan_digest="+plan.Digest;
            string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../MapDesign/MCP/GENERATED/SV5_09_LOOPS/_work/density_diagnostic.txt"));
            Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path,detail);
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(path),"density_rooms.csv"),"id,recipe,x,y,width,height,parent,depth,mirror\n"+
                string.Join("\n",payload.Rooms.Select(r=>string.Join(",",r.Id,r.Recipe,r.Bounds.X,r.Bounds.Y,r.Bounds.Width,r.Bounds.Height,r.Parent,r.Depth,r.Mirror))));
            Sv5InfillExport.WriteCase(plan,Path.Combine(Path.GetDirectoryName(path),"default_candidate"));
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(path),"candidate.svg"),
                "<!-- CURRENT CANDIDATE; NOT FINAL ACCEPTANCE -->\n"+Sv5InfillExport.Preview(plan,new Sv5SpaceBounds(0,0,624,416)));
            WriteCellPreview(plan,Path.Combine(Path.GetDirectoryName(path),"candidate_cells.png"));
            Assert.That(payload.Success,Is.True,detail);
            Assert.That(plan.Success,Is.True,detail);
            Assert.That(payload.Rooms.Count(r=>r.Legacy),Is.EqualTo(6));
            Assert.That(payload.Rooms.Where(r=>!r.Legacy).Select(r=>r.Recipe).Distinct(),Is.EquivalentTo(Sv5InfillPatterns.Recipes));
        }

        [Test, Timeout(1200000)] public void T07_RepeatOnPreservesTwentyFourPlacesAndMeetsIdenticalDensity()
        {
            var before=RepeatBaseline.Value; var after=RepeatOn.Value;
            Assert.That(before.Digest,Is.EqualTo("94c9f3a353e39984633a755af7842e87989e86dc97c3545ce87a4ac4da4397b7"));
            Assert.That(before.Places.Count,Is.EqualTo(24));
            Assert.That(after.Success,Is.True,string.Join("\n",after.Diagnostics));
            Assert.That(after.Infill.NewRoomCount,Is.InRange(128,384));
            Assert.That(after.Infill.NewOwnedTiles,Is.GreaterThanOrEqualTo(24576));
            Assert.That(after.Infill.SectorCounts.Count(n=>n>=6),Is.GreaterThanOrEqualTo(12));
            Assert.That(after.Infill.Rooms.Count(r=>r.Legacy),Is.EqualTo(6));
            Assert.That(after.Infill.Rooms.Where(r=>!r.Legacy).Select(r=>r.Recipe).Distinct(),Is.EquivalentTo(Sv5InfillPatterns.Recipes));
            Assert.That(after.Diversity.EligibleNearPairs,Is.EqualTo(before.Diversity.EligibleNearPairs));
            foreach(var place in before.Places)
            {
                var same=after.Places.Single(p=>p.Id==place.Id);
                Assert.That(same.Bounds,Is.EqualTo(place.Bounds));
                Assert.That(same.Family,Is.EqualTo(place.Family));
                Assert.That(same.FormationId,Is.EqualTo(place.FormationId));
            }
            TestContext.Out.WriteLine("REPEAT rooms="+after.Infill.NewRoomCount+";owned="+after.Infill.NewOwnedTiles+
                ";sectors="+string.Join(",",after.Infill.SectorCounts)+";digest="+after.Digest);
        }

        [Test, Timeout(1200000)] public void T08_AllNewAirJoinsTheActualCoordinateGraphAndEveryRoomHasSupportedReturn()
        {
            foreach(var pair in Cases())
            {
                var plan=pair.After; var payload=plan.Infill;
                Assert.That(Sv5SpaceInfill.ValidatePayload(pair.Before,payload),Is.Empty);
                var cells=payload.Cells.ToDictionary(c=>c.World,c=>c.Value);
                var graph=plan.Connections.SelectMany(c=>c.Centerline.Concat(c.ApertureCells)).ToHashSet();
                Assert.That(payload.NewRoomCount,Is.GreaterThanOrEqualTo(128));
                foreach(var c in payload.Cells)
                {
                    Assert.That(graph.Contains(c.World),Is.EqualTo(c.Value==Sv5InfillCellValue.Air),c.Token);
                    if(c.Value==Sv5InfillCellValue.Air)
                        foreach(string host in c.Host.Split(';'))
                            Assert.That(plan.Connections.Single(v=>v.Id==host).ApertureCells,Does.Contain(c.World),c.Token);
                }
                var components=new System.Collections.Generic.Dictionary<RmapSpecialWorldPoint,int>(); int componentId=0;
                foreach(var seed in graph.OrderBy(p=>p))
                {
                    if(components.ContainsKey(seed)) continue;
                    componentId++; components.Add(seed,componentId);
                    var componentQueue=new System.Collections.Generic.Queue<RmapSpecialWorldPoint>(); componentQueue.Enqueue(seed);
                    while(componentQueue.Count>0)
                    {
                        var p=componentQueue.Dequeue();
                        foreach(var d in new[]{new[]{1,0},new[]{-1,0},new[]{0,1},new[]{0,-1}})
                        {
                            var n=new RmapSpecialWorldPoint(p.X+d[0],p.Y+d[1]);
                            if(!graph.Contains(n) || components.ContainsKey(n)) continue;
                            components.Add(n,componentId); componentQueue.Enqueue(n);
                        }
                    }
                }
                foreach(var room in payload.Rooms)
                {
                    var link=payload.Links.Single(l=>l.Room==room.Id);
                    var parent=payload.Rooms.SingleOrDefault(r=>r.Id==room.Parent);
                    var origin=room.Legacy ? link.Path[0] : parent==null ? link.HostAccess[0] : parent.Entry;
                    Assert.That(components.ContainsKey(origin),Is.True,room.Id+" origin");
                    Assert.That(components[room.Entry],Is.EqualTo(components[origin]),room.Id+" entry");
                    Assert.That(components[room.Deep],Is.EqualTo(components[origin]),room.Id+" deep");
                    var proof=Sv5InfillPatterns.Screen(cells,parent==null ? link.Path[0] : parent.Entry,room.Deep);
                    Assert.That(proof.Success,Is.True,room.Id);
                    Assert.That(proof.PlayerVerified,Is.False);
                    foreach(var foot in proof.Approach.Concat(proof.Return))
                    {
                        Assert.That(cells[new RmapSpecialWorldPoint(foot.X,foot.Y-1)],Is.EqualTo(Sv5InfillCellValue.Solid));
                        Assert.That(cells[foot],Is.EqualTo(Sv5InfillCellValue.Air));
                        Assert.That(cells[new RmapSpecialWorldPoint(foot.X,foot.Y+1)],Is.EqualTo(Sv5InfillCellValue.Air));
                    }
                    if(room.Legacy) continue;
                    Assert.That(link.ConnectionCellCount,Is.InRange(1,24),room.Id);
                    Assert.That(link.ConnectionCenterline,Is.EqualTo(link.HostAccess.Count==0 ? link.Path :
                        link.HostAccess.Concat(link.Path.Skip(1))),room.Id+" inclusive endpoints");
                    Assert.That(link.ConnectionCenterline.First(),Is.EqualTo(parent==null ? link.HostAccess[0] : link.Path[0]),room.Id);
                    Assert.That(link.ConnectionCenterline.Last(),Is.EqualTo(room.Entry),room.Id);
                    Assert.That(link.ConnectionCenterline.Zip(link.ConnectionCenterline.Skip(1),(a,b)=>Math.Abs(a.X-b.X)+Math.Abs(a.Y-b.Y)),Is.All.EqualTo(1));
                    // ExternalCenterline remains an ownership diagnostic; it is deliberately not the length authority.
                    Assert.That(link.ExternalCenterline,Is.EqualTo(link.HostAccess.Concat(link.Path).Distinct()
                        .Where(p=>link.Cells.Any(c=>c.World.Equals(p)))));
                }
            }
        }

        [Test, Timeout(1200000)] public void T09_BothOnPlansRecomputeSixOrderProductAndRejectRemovedGateOrBypassAir()
        {
            foreach(var pair in Cases())
            {
                var p=pair.After; var product=p.PhysicalProduct;
                Assert.That(p.PhysicalMovement.Success,Is.True);
                Assert.That(Sv5SpaceGraphValidator.FindGateErrors(p.ContactDecisions,p.Gates),Is.Empty);
                Assert.That(product.Success,Is.True,string.Join(";",product.Diagnostics));
                Assert.That(product.Matrix.Count,Is.GreaterThan(0));
                Assert.That(product.Matrix.Select(r=>r.ResourceOrder).Distinct().Count(),Is.EqualTo(6));
                Assert.That(product.Matrix.Where(r=>!r.Success),Is.Empty);
                foreach(var proof in product.Proofs)
                {
                    Assert.That(proof.Success,Is.True); Assert.That(proof.DeadEnds,Is.Empty);
                    Assert.That(proof.GoalProof.Actions.Where(a=>a.StartsWith("ACQUIRE|")).Select(a=>a.Substring(8)),
                        Is.EqualTo(proof.GoalProof.RequestedOrder.Select(r=>r.ToString())));
                }
                Assert.That(Sv5SpacePhysicalMovement.FindStateErrors(p.Core,p.Connections,p.Gates.Skip(1)),Is.Not.Empty);
                var gate=p.Gates.First(); var face=gate.BlockingFaces.Single();
                var offset=face.First.X==face.Second.X ? new RmapSpecialWorldPoint(1,0) : new RmapSpecialWorldPoint(0,1);
                var bypass=new[]{new RmapSpecialWorldPoint(face.First.X+offset.X,face.First.Y+offset.Y),
                    new RmapSpecialWorldPoint(face.Second.X+offset.X,face.Second.Y+offset.Y)};
                var changed=p.Connections.Select(c=>c.Id!=gate.SourceConnectionId ? c :
                    Construct<Sv5SpaceConnection>(c.Id,c.Kind,c.FromPortId,c.ToPortId,c.FromPlaceId,c.ToPlaceId,c.Direction,
                        c.Flow,c.Condition,c.SourceGraphEdgeId,c.SelectionState,c.Centerline,c.Envelope.Concat(bypass),c.ApertureCells.Concat(bypass))).ToArray();
                Assert.That(Sv5SpacePhysicalMovement.FindStateErrors(p.Core,changed,p.Gates),Is.Not.Empty);
                TestContext.Out.WriteLine("PRODUCT "+p.Digest+";rows="+product.Matrix.Count+";orders="+product.Proofs.Count+";errors="+product.Diagnostics.Count);
            }
        }

        [Test, Timeout(1200000)] public void T10_InfillOffMatchesPinnedDigestsAndOnPreservesOriginalGeometry()
        {
            var profiles=new[]{Sv5SpaceGraphAuthoringProfile.RepresentativeV1(),Sv5SpaceDiversity.RepeatProfile()};
            int i=0;
            foreach(var pair in Cases())
            {
                var off=Sv5SpaceGraphPlanner.PlanWithInfill(pair.Before.Core,1304,profiles[i++],infill:new Sv5InfillProfile(enabled:false));
                Assert.That(off.Infill,Is.Null); Assert.That(off.Diversity.Profile.Enabled,Is.True);
                Assert.That(off.Digest,Is.EqualTo(pair.Before.Digest));
                Assert.That(pair.After.Core,Is.SameAs(pair.Before.Core));
                Assert.That(pair.After.Places.Select(p=>p.Id),Is.EqualTo(pair.Before.Places.Select(p=>p.Id)));
                Assert.That(WithoutPlanDigest(Sv5SpaceGraphExport.PlacesCsv(pair.After)),
                    Is.EqualTo(WithoutPlanDigest(Sv5SpaceGraphExport.PlacesCsv(pair.Before))));
                Assert.That(WithoutPlanDigest(Sv5SpaceGraphExport.PortsCsv(pair.After)),
                    Is.EqualTo(WithoutPlanDigest(Sv5SpaceGraphExport.PortsCsv(pair.Before))));
                Assert.That(pair.After.Digest,Is.Not.EqualTo(pair.Before.Digest));
                foreach(var c in pair.Before.Connections)
                {
                    var same=pair.After.Connections.Single(n=>n.Id==c.Id);
                    Assert.That(same.Centerline,Is.EqualTo(c.Centerline)); Assert.That(same.Condition,Is.EqualTo(c.Condition));
                    Assert.That(same.FromPortId,Is.EqualTo(c.FromPortId)); Assert.That(same.ToPortId,Is.EqualTo(c.ToPortId));
                }
                foreach(var g in pair.Before.Gates)
                {
                    var same=pair.After.Gates.Single(n=>n.Id==g.Id);
                    Assert.That(same.TypedPredicate.StableToken,Is.EqualTo(g.TypedPredicate.StableToken));
                    Assert.That(same.BlockingCells,Is.EqualTo(g.BlockingCells));
                    Assert.That(same.BlockingFaces.Select(f=>f.StableToken),Is.EqualTo(g.BlockingFaces.Select(f=>f.StableToken)));
                }
                Assert.That(pair.After.PlayerVerified,Is.False);
            }
            Assert.That(Baseline.Value.Digest,Is.EqualTo("10080e53c3d4c47f6e93f49118c7c648f07c1f3162742ce866b6ecd8a0be3e46"));
        }

        private static string WithoutPlanDigest(string csv) => string.Join("\n",csv.TrimEnd().Split('\n')
            .Select(line=>line.Substring(0,line.LastIndexOf(','))))+"\n";

        [Test, Timeout(1200000)] public void T11_ActualExportsReconstructEveryCellAndMutationsChangeDigestOrFailValidation()
        {
            string work=Path.Combine(Root,"MapDesign/MCP/GENERATED/SV5_09_LOOPS/_work/infill");
            foreach(var pair in Cases())
            {
                var p=pair.After; var payload=p.Infill;
                string first=Path.Combine(work,"export_a",p.Digest), second=Path.Combine(work,"export_b",p.Digest);
                Sv5InfillExport.WriteCase(p,first); Sv5InfillExport.WriteCase(p,second);
                var files=Directory.GetFiles(first).OrderBy(f=>f,StringComparer.Ordinal).ToArray();
                Assert.That(files.Length,Is.EqualTo(8));
                foreach(string file in files) Assert.That(File.ReadAllBytes(Path.Combine(second,Path.GetFileName(file))),Is.EqualTo(File.ReadAllBytes(file)));
                var rows=ReadCsv(Path.Combine(first,"infill_cells.csv"));
                Assert.That(rows.Length,Is.EqualTo(payload.Cells.Count));
                var exported=rows.ToDictionary(r=>new RmapSpecialWorldPoint(int.Parse(r[0]),int.Parse(r[1])),r=>r);
                foreach(var c in payload.Cells)
                {
                    var row=exported[c.World];
                    Assert.That(row[2],Is.EqualTo(c.Value.ToString())); Assert.That(row[3],Is.EqualTo(c.Owner));
                    Assert.That(row[11],Is.EqualTo(p.Digest));
                }
                var patterns=ReadCsv(Path.Combine(first,"infill_patterns.csv")).ToDictionary(r=>r[0],r=>
                    new Sv5InfillPattern(ushort.Parse(r[1]),r[2].Split('|').Select(v=>(Sv5InfillCellValue)Enum.Parse(typeof(Sv5InfillCellValue),v))));
                foreach(var pattern in patterns) Assert.That(pattern.Value.Id,Is.EqualTo(pattern.Key));
                var instances=ReadCsv(Path.Combine(first,"infill_instances.csv")).Select(r=>new Sv5InfillInstance(patterns[r[0]],
                    new RmapSpecialWorldPoint(int.Parse(r[1]),int.Parse(r[2])),r[3],r[5],r[6],r[7],bool.Parse(r[8]))).ToArray();
                var rebuilt=Sv5InfillPatterns.Reconstruct(instances);
                Assert.That(rebuilt.Count,Is.EqualTo(payload.Cells.Count));
                foreach(var c in payload.Cells) Assert.That(rebuilt[c.World],Is.EqualTo(c.Value),c.Token);
                var windowRows=ReadCsv(Path.Combine(first,"infill_windows.csv"));
                Assert.That(windowRows.Length,Is.EqualTo(169));
                Assert.That(windowRows.Sum(r=>int.Parse(r[5])),Is.EqualTo(payload.NewOwnedTiles));
                var validation=JsonUtility.FromJson<ValidationDto>(File.ReadAllText(Path.Combine(first,"infill_validation.json")));
                Assert.That(validation.plan_digest,Is.EqualTo(p.Digest));
                Assert.That(validation.CELLS && validation.PATTERNS && validation.STATIC_SCREEN && validation.CONNECTION_LENGTH && validation.CONTACT && validation.PHYSICAL_PRODUCT,Is.True);
                Assert.That(validation.length_policy,Is.EqualTo(Sv5InfillProfile.ConnectionLengthPolicy));
                Assert.That(validation.length_target_count,Is.EqualTo(payload.NewRoomCount));
                Assert.That(validation.maximum_connection_cell_count,Is.InRange(1,24));
                Assert.That(validation.over_length_count,Is.Zero);
                Assert.That(validation.endpoint_error_count,Is.Zero);
                Assert.That(validation.non_cardinal_count,Is.Zero);
                Assert.That(validation.COMPOSED || validation.PLAYER,Is.False);
                Assert.That(validation.fully_known_windows+validation.mixed_pending_windows,Is.EqualTo(254409));
                Assert.That(validation.mixed_pending_windows,Is.GreaterThan(0));
                string linkPath=Path.Combine(first,"infill_links.csv");
                Assert.That(File.ReadLines(linkPath).First(),Is.EqualTo(
                    "room_id,parent,host,ordered_cardinal_air_path,host_access,external_cardinal_air,external_cell_count,connection_centerline,connection_cell_count,length_policy,length_status,opening_faces,support_cells,approach,return,success,qualification,plan_digest"));
                var linkRows=ReadCsv(linkPath);
                Assert.That(linkRows.Length,Is.EqualTo(payload.Links.Count));
                foreach(var row in linkRows)
                {
                    var exportedLink=payload.Links.Single(l=>l.Room==row[0]);
                    var linkedRoom=payload.Rooms.Single(r=>r.Id==exportedLink.Room);
                    string expectedPoints="["+string.Join(",",exportedLink.ConnectionCenterline.Select(point=>"["+point.X+","+point.Y+"]"))+"]";
                    Assert.That(row[7],Is.EqualTo(expectedPoints),exportedLink.Room);
                    Assert.That(int.Parse(row[8]),Is.EqualTo(exportedLink.ConnectionCellCount),exportedLink.Room);
                    Assert.That(row[9],Is.EqualTo(Sv5InfillProfile.ConnectionLengthPolicy),exportedLink.Room);
                    Assert.That(row[10],Is.EqualTo(linkedRoom.Legacy ? "NOT_APPLICABLE_LEGACY_INTERIOR" : "PASS"),exportedLink.Room);
                    Assert.That(row[17],Is.EqualTo(p.Digest),exportedLink.Room);
                }
                string infillJson=File.ReadAllText(Path.Combine(first,"infill.json"));
                Assert.That(infillJson,Does.Contain("\"length_policy\":\""+Sv5InfillProfile.ConnectionLengthPolicy+"\"")
                    .And.Contain("\"infill_digest\":\""+payload.Digest+"\"").And.Contain("\"plan_digest\":\""+p.Digest+"\""));
                var svg=new System.Xml.XmlDocument(); svg.LoadXml(Sv5InfillExport.Preview(p,new Sv5SpaceBounds(0,0,624,416)));
                Assert.That(svg.GetElementsByTagName("metadata")[0].InnerText,Is.EqualTo(p.Digest));
                Assert.That(svg.GetElementsByTagName("path").Cast<System.Xml.XmlElement>().Count(n=>n.GetAttribute("fill")=="#111a20" || n.GetAttribute("fill")=="#f5f5e9"),
                    Is.EqualTo(Sv5SpaceInfill.KnownBase(p).Keys.Concat(payload.Cells.Select(c=>c.World)).Distinct().Count()));
                var room=payload.Rooms.First(r=>!r.Legacy);
                var floor=new RmapSpecialWorldPoint(room.Entry.X,room.Entry.Y-1);
                var changedCells=payload.Cells.Select(c=>!c.World.Equals(floor) ? c :
                    new Sv5InfillCell(c.World,Sv5InfillCellValue.Air,c.Owner,c.Recipe,c.Parent,c.Host,c.Shared)).ToArray();
                var changed=Construct<Sv5InfillPlan>(payload.BaselineDigest,payload.Profile,payload.Rooms,payload.Links,changedCells,
                    Array.Empty<string>(),payload.Rejections.ToDictionary(r=>r.Key,r=>r.Value),payload.Termination,payload.PreviouslyReservedCells.ToHashSet());
                Assert.That(changed.Digest,Is.Not.EqualTo(payload.Digest));
                Assert.That(Sv5SpaceInfill.ValidatePayload(pair.Before,changed),Is.Not.Empty);
                var policy=Construct<Sv5InfillPlan>(payload.BaselineDigest,new Sv5InfillProfile(target:257),payload.Rooms,payload.Links,payload.Cells,
                    payload.Diagnostics,payload.Rejections.ToDictionary(r=>r.Key,r=>r.Value),payload.Termination,payload.PreviouslyReservedCells.ToHashSet());
                Assert.That(policy.Digest,Is.Not.EqualTo(payload.Digest));
                var link=payload.Links.Single(l=>l.Room==room.Id);
                var changedLink=Construct<Sv5InfillLink>(link.Room,link.Parent,link.Host,link.Path,
                    Array.Empty<Sv5SpaceBoundaryFace>(),link.Cells,link.Proof,link.HostAccess);
                var aperture=Construct<Sv5InfillPlan>(payload.BaselineDigest,payload.Profile,payload.Rooms,
                    payload.Links.Select(l=>l.Room==room.Id ? changedLink : l),payload.Cells,payload.Diagnostics,
                    payload.Rejections.ToDictionary(r=>r.Key,r=>r.Value),payload.Termination,payload.PreviouslyReservedCells.ToHashSet());
                Assert.That(aperture.Digest,Is.Not.EqualTo(payload.Digest));
            }
            // Both immutable integration candidates already passed production validation above.
            // This is the sole final ON export pair, never an export into historical directories.
            Sv5InfillExport.WriteComparison(Path.Combine(Root,"MapDesign/MCP/GENERATED/SV5_09_LOOPS/_work/legacy_exports/sv5_08_fix01"),DefaultOn.Value,RepeatOn.Value);
        }

        [Test, Timeout(1200000)] public void T12_HistoricalLocksAndEightyTwoTestNamesRemainAndObligationsReflectActualState()
        {
            var locked=JsonUtility.FromJson<LockDto>(File.ReadAllText(Path.Combine(Root,"MapDesign/MCP/INPUTS/SV5_08/SOURCE_LOCK.json")));
            var always=locked.files.Where(f=>f.phase=="ALWAYS").ToArray();
            Assert.That(always.Length,Is.EqualTo(645));
            foreach(var entry in always)
            {
                byte[] bytes=File.ReadAllBytes(Path.Combine(Root,entry.path));
                Assert.That(bytes.LongLength,Is.EqualTo(entry.worktree_bytes),entry.path);
                using(var sha=System.Security.Cryptography.SHA256.Create())
                    Assert.That(BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant(),Is.EqualTo(entry.worktree_sha256),entry.path);
            }
            var xml=new System.Xml.XmlDocument(); xml.Load(Path.Combine(Root,"MapDesign/MCP/GENERATED/SV5_07/focused_results.xml"));
            var tests=xml.SelectNodes("//test-case").Cast<System.Xml.XmlElement>().ToArray();
            Assert.That(tests.Length,Is.EqualTo(82));
            foreach(var test in tests)
            {
                var type=typeof(Sv5SpaceInfillTests).Assembly.GetType(test.GetAttribute("classname"),true);
                var method=type.GetMethod(test.GetAttribute("methodname"));
                Assert.That(method,Is.Not.Null,test.GetAttribute("fullname"));
                Assert.That(method.GetCustomAttributes(typeof(TestAttribute),true),Is.Not.Empty);
                Assert.That(method.GetCustomAttributes(typeof(IgnoreAttribute),true),Is.Empty);
            }
            foreach(var pair in Cases())
            {
                string rows=Sv5SpaceGraphExport.ObligationsCsv(pair.After);
                Assert.That(rows,Does.Contain("APPLIED").And.Contain("LOCAL_CELLS_STATIC_SCREEN").And.Contain("LOOP_PENDING"));
                Assert.That(rows,Does.Not.Contain("sparse 22-place"));
                Assert.That(Sv5SpaceGraphExport.SpaceGraphJson(pair.Before),Does.Contain("INFILL_PENDING"));
                Assert.That(Sv5SpaceGraphExport.SpaceGraphJson(pair.After),Does.Contain(pair.After.Infill.Digest));
            }
            TestContext.Out.WriteLine("ALWAYS=645;EXISTING_TEST_NAMES=82;This test does not claim the final focused XML or final _work cleanup; external post-run audit is mandatory.");
        }

        private static string[][] ReadCsv(string path)
        {
            return File.ReadAllLines(path).Skip(1).Where(line=>line.Length>0).Select(line=>
            {
                var fields=new System.Collections.Generic.List<string>(); var field=new System.Text.StringBuilder(); bool quote=false;
                for(int i=0;i<line.Length;i++)
                {
                    if(line[i]=='"')
                    { if(quote && i+1<line.Length && line[i+1]=='"') { field.Append('"'); i++; } else quote=!quote; }
                    else if(line[i]==',' && !quote) { fields.Add(field.ToString()); field.Clear(); }
                    else field.Append(line[i]);
                }
                fields.Add(field.ToString()); return fields.ToArray();
            }).ToArray();
        }
        [Serializable] private sealed class LockDto { public LockEntry[] files; }
        [Serializable] private sealed class LockEntry { public string path,phase,worktree_sha256; public long worktree_bytes; }
        [Serializable] private sealed class ValidationDto
        {
            public string plan_digest,length_policy;
            public bool CELLS,PATTERNS,STATIC_SCREEN,CONNECTION_LENGTH,CONTACT,PHYSICAL_PRODUCT,COMPOSED,PLAYER;
            public int fully_known_windows,mixed_pending_windows,length_target_count,maximum_connection_cell_count,
                over_length_count,endpoint_error_count,non_cardinal_count;
        }

        private static void WriteCellPreview(Sv5SpaceGraphPlan plan,string path)
        {
            const int scale=3,width=624,height=416;
            var pixels=Enumerable.Repeat(new Color32(184,195,198,255),width*height*scale*scale).ToArray();
            void Paint(RmapSpecialWorldPoint p,Color32 color)
            {
                for(int dy=0;dy<scale;dy++) for(int dx=0;dx<scale;dx++)
                    pixels[(p.Y*scale+dy)*width*scale+p.X*scale+dx]=color;
            }
            foreach(var place in plan.Places.Where(p=>p.Kind!=Sv5SpacePlaceKind.Core))
                for(int y=place.Bounds.Y;y<place.Bounds.MaxYExclusive;y++)
                for(int x=place.Bounds.X;x<place.Bounds.MaxXExclusive;x++) Paint(new RmapSpecialWorldPoint(x,y),new Color32(215,201,170,255));
            foreach(var p in plan.Connections.SelectMany(c=>c.Centerline).Distinct()) Paint(p,new Color32(120,158,175,255));
            foreach(var c in Sv5SpaceInfill.KnownBase(plan)) Paint(c.Key,c.Value==Sv5InfillCellValue.Solid ? new Color32(17,26,32,255) : new Color32(245,245,233,255));
            foreach(var c in plan.Infill.Cells) Paint(c.World,c.Value==Sv5InfillCellValue.Solid ? new Color32(17,26,32,255) : new Color32(245,245,233,255));
            for(int y=0;y<height*scale;y++) for(int x=0;x<width*scale;x++)
                if(x%(4*scale)==0 || y%(4*scale)==0)
                {
                    int i=y*width*scale+x; var c=pixels[i];
                    pixels[i]=new Color32((byte)((c.r*3+23)/4),(byte)((c.g*3+75)/4),(byte)((c.b*3+101)/4),255);
                }
            var texture=new Texture2D(width*scale,height*scale,TextureFormat.RGBA32,false);
            try { texture.SetPixels32(pixels); texture.Apply(); File.WriteAllBytes(path,texture.EncodeToPNG()); }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }
    }
}
#endif
