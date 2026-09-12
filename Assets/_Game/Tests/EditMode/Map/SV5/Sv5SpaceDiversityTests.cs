#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.SectorPlanning;
using StarNight.Map.WorldGeneration.SpecialRegions;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.Sv5
{
    [Category("SV5_07")]
    public sealed class Sv5SpaceDiversityTests
    {
        private static readonly Lazy<Sv5CoreReservationPlan> Core = new Lazy<Sv5CoreReservationPlan>(() => Sv5RouteStatePolicyTests.RepresentativePlanForFix01);
        private static readonly Lazy<Sv5SpaceGraphPlan> DefaultOff = new Lazy<Sv5SpaceGraphPlan>(() => Sv5SpaceGraphPlanner.Plan(Core.Value,1304,diversity:new Sv5DiversityProfile(false)));
        private static readonly Lazy<Sv5SpaceGraphPlan> DefaultOn = new Lazy<Sv5SpaceGraphPlan>(() => Sv5SpaceGraphPlanner.Plan(Core.Value,1304));
        // Freeze D07 before any integration run: same seed/base requests plus ordinal 16/32, 60x24 caves.
        private static readonly Lazy<Sv5SpaceGraphPlan> RepeatOff = new Lazy<Sv5SpaceGraphPlan>(() => Sv5SpaceGraphPlanner.Plan(Core.Value,1304,Sv5SpaceDiversity.RepeatProfile(),new Sv5DiversityProfile(false)));
        private static readonly Lazy<Sv5SpaceGraphPlan> RepeatOn = new Lazy<Sv5SpaceGraphPlan>(() => Sv5SpaceGraphPlanner.Plan(Core.Value,1304,Sv5SpaceDiversity.RepeatProfile()));
        private static Sv5SpaceFamilySpec Cave(int ordinal = 0) => new Sv5SpaceFamilySpec(ordinal,"CAVE_BAND",Sv5SpacePlaceKind.Large,12,8,"SV5_24_CAVE");
        private static Sv5FormationPart Part(string id, int x, int y = 0, int w = 12, int h = 8,
            string family = "CAVE_BAND", string part = "WHOLE") => new Sv5FormationPart(id,id,family,new Sv5SpaceBounds(x,y,w,h),partId:part);
        private static Sv5PlacementCandidate Candidate(int x, ulong rank) => new Sv5PlacementCandidate(new Sv5SpaceBounds(x,0,12,8),0,0,rank);
        private static string Root => Path.GetFullPath(Path.Combine(Application.dataPath,".."));

        [Test] public void V01_EmptyBoundsGapUsesInclusiveRadiusAndExactTileSpacing()
        {
            var profile = new Sv5DiversityProfile();
            foreach (int gap in new[] { 0,1,35,36,37 })
            {
                var a = Part("A",0); var b = Part("B",12+gap);
                Assert.That(Sv5SpaceDiversity.Gap(a.Bounds,b.Bounds),Is.EqualTo(gap));
                var pair = Sv5SpaceDiversity.Pairs(new[] { a,b },profile).Single();
                Assert.That(pair.Near,Is.EqualTo(gap<=36));
            }
            Assert.That(Sv5SpaceDiversity.Gap(new Sv5SpaceBounds(0,0,12,8),new Sv5SpaceBounds(20,18,12,8)),Is.EqualTo(18));
        }

        [Test] public void V02_FormationSubdivisionsCountOnceAndConflictingOwnershipIsRejected()
        {
            var tiles = Enumerable.Range(0,5).SelectMany(x => Enumerable.Range(0,4).Select(y =>
                Part("CAVE_A",x*4,y*4,4,4,part:x+":"+y))).ToArray();
            var chunks = new[] { Part("CAVE_A",0,0,12,8,part:"a"),Part("CAVE_A",12,0,8,8,part:"b"),
                Part("CAVE_A",0,8,12,8,part:"c"),Part("CAVE_A",12,8,8,8,part:"d") };
            var other = Part("CAVE_B",24);
            Assert.That(Sv5SpaceDiversity.Pairs(tiles,new Sv5DiversityProfile()),Is.Empty);
            var tilePair = Sv5SpaceDiversity.Pairs(tiles.Append(other).Reverse(),new Sv5DiversityProfile()).Single();
            var chunkPair = Sv5SpaceDiversity.Pairs(chunks.Append(other),new Sv5DiversityProfile()).Single();
            Assert.That(tilePair.Gap,Is.EqualTo(4)); Assert.That(chunkPair.Gap,Is.EqualTo(tilePair.Gap));
            Assert.Throws<ArgumentException>(() => Sv5SpaceDiversity.Pairs(new[] { Part("A",0,part:"a"),
                Part("A",20,family:"LIBRARY_STACK",part:"b") },new Sv5DiversityProfile()));
            Assert.Throws<ArgumentException>(() => new Sv5FormationPart("INDEPENDENT_B","CAVE_A","CAVE_BAND",other.Bounds));
        }

        [Test] public void V03_FamilyAliasesIgnoreDisplayVariantsAndOrdinaryRoomsRemainExempt()
        {
            var profile = new Sv5DiversityProfile();
            var a = new Sv5FormationPart("A","A","CAVE_BAND",new Sv5SpaceBounds(0,0,12,8),variantId:"v1",displayName:"blue");
            var b = new Sv5FormationPart("B","B","CAVE_BAND",new Sv5SpaceBounds(20,0,12,8),variantId:"v99",displayName:"not a cave");
            Assert.That(Sv5SpaceDiversity.Pairs(new[] { a,b },profile).Single().EligibleNear,Is.True);
            Assert.That(profile.Weight("CAVE_BAND",false,1),Is.EqualTo(70));
            var rooms = Sv5SpaceDiversity.Pairs(new[] { Part("R1",0,family:"ORDINARY_ROOM_A"),Part("R2",20,family:"ORDINARY_ROOM_F") },profile);
            Assert.That(rooms.Single().Near,Is.True); Assert.That(rooms.Single().Exclusion,Is.EqualTo("ORDINARY_CONNECTION_SPACE"));
            Assert.That(profile.Weight("ORDINARY_ROOM_F",false,9),Is.EqualTo(100));
            Assert.That(profile.FamilyKey("CAVE_BAND_NEW"),Is.EqualTo("CAVE_BAND_NEW"));
            Assert.That(profile.Weight("UNMAPPED",false,2),Is.EqualTo(49));
            Assert.That(profile.Weight("UNMAPPED",true,3),Is.EqualTo(100));
        }

        [Test] public void V04_RequestScopedRollAndStableSelectionMatchTheFrozenProfile()
        {
            var p = new Sv5DiversityProfile();
            Assert.That(Enumerable.Range(0,5).Select(n=>p.Weight("CAVE_BAND",false,n)),Is.EqualTo(new[] {100,70,49,34,34}));
            string json = File.ReadAllText(Path.Combine(Root,"MapDesign/MCP/INPUTS/SV5_07/DIVERSITY_PROFILE.json"));
            var dto = JsonUtility.FromJson<ProfileDto>(json);
            Assert.That(dto.id,Is.EqualTo(p.Id)); Assert.That(dto.version,Is.EqualTo(p.Version)); Assert.That(dto.enabled,Is.EqualTo(p.Enabled));
            Assert.That(dto.near_gap_tiles_inclusive,Is.EqualTo(p.Radius)); Assert.That(dto.pass_percent_by_near_count,Is.EqualTo(p.Weights));
            Assert.That(dto.random_salt,Is.EqualTo(Sv5DiversityProfile.Salt)); Assert.That(dto.distance_metric,Is.EqualTo(Sv5DiversityProfile.Metric));
            foreach (var kv in p.Aliases) Assert.That(json,Does.Match("\""+kv.Key+"\"\\s*:\\s*\""+kv.Value+"\""));
            var candidates = new[] { Candidate(20,1),Candidate(100,2) }; var placed = new[] { Part("A",0) };
            for (ulong seed=0;seed<64;seed++)
            {
                string token = "SV5_DIVERSITY_V1|"+seed.ToString(CultureInfo.InvariantCulture)+"|0|CAVE_BAND";
                string hash = Hash(Encoding.UTF8.GetBytes(token));
                Assert.That(Sv5SpaceDiversity.Roll(seed,0,"CAVE_BAND"),Is.EqualTo((int)(ulong.Parse(hash.Substring(0,16),NumberStyles.HexNumber)%100)));
                var first = Sv5SpaceDiversity.Select(Cave(),seed,candidates,placed,p);
                var second = Sv5SpaceDiversity.Select(Cave(),seed,candidates.Reverse(),placed.Reverse(),p);
                Assert.That(first.Chosen.Bounds,Is.EqualTo(second.Chosen.Bounds));
                Assert.That(first.Trace.Select(d=>d.Roll).Distinct().Count(),Is.EqualTo(1));
                Assert.That(Sv5SpaceDiversity.Select(Cave(),seed,candidates,placed,new Sv5DiversityProfile(false)).Chosen.Bounds,Is.EqualTo(candidates[0].Bounds));
                Assert.That(Sv5SpaceDiversity.Select(Cave(),seed,candidates,Array.Empty<Sv5FormationPart>(),p).Chosen.Bounds,Is.EqualTo(candidates[0].Bounds));
            }
        }

        [Test] public void V05_ProductionSelectorActuallyMovesAwayFromNearbyRepeatsAcross64Seeds()
        {
            int off=0,on=0;
            for (ulong seed=0;seed<64;seed++)
            {
                var candidates=new[] {Candidate(20,1),Candidate(100,2)}; var placed=new[] {Part("A",0)};
                if(Sv5SpaceDiversity.Select(Cave(),seed,candidates,placed,new Sv5DiversityProfile(false)).Chosen.Bounds.X==20)off++;
                if(Sv5SpaceDiversity.Select(Cave(),seed,candidates,placed,new Sv5DiversityProfile()).Chosen.Bounds.X==20)on++;
            }
            Assert.That(off,Is.EqualTo(64)); Assert.That(on,Is.GreaterThan(0).And.LessThan(off));
            TestContext.Out.WriteLine("PURE_SELECTOR_NEAR_OFF="+off+";ON="+on);
        }

        [Test] public void V06_AllSoftFailuresUseMinimumRepeatLegalFallbackNotInvalidOrMissingPlaces()
        {
            var candidates = new[] {Candidate(20,1),Candidate(100,2),Candidate(200,3)};
            var selection = Sv5SpaceDiversity.Select(Cave(),0,candidates.Reverse(),new[] {Part("A",0)},
                new Sv5DiversityProfile(true,36,new[] {0,0,0,0}),c=>c.Bounds.X!=200);
            Assert.That(selection.Chosen.Bounds.X,Is.EqualTo(100));
            Assert.That(selection.Reason,Is.EqualTo("MIN_REPEAT_LEGAL_FALLBACK"));
            Assert.That(selection.Trace.Last().Fallback,Is.True); Assert.That(selection.Trace.Last().Neighbors,Is.Empty);
            Assert.That(selection.Trace.Count,Is.EqualTo(3));
            Assert.That(Sv5SpaceDiversity.Select(Cave(),0,candidates,Array.Empty<Sv5FormationPart>(),new Sv5DiversityProfile(),c=>false).Chosen,Is.Null);
        }

        [Test] public void V07_DefaultRealPlanKeepsFix04GeometryAndReportsNoEligibleRepeat()
        {
            var off=DefaultOff.Value; var on=DefaultOn.Value;
            Assert.That(off.Success,Is.True,string.Join(";",off.Diagnostics)); Assert.That(on.Success,Is.True,string.Join(";",on.Diagnostics));
            Assert.That(on.Diversity.Observation,Is.EqualTo("NO_ELIGIBLE_REPEAT"));
            Assert.That(on.Diversity.EligibleNearPairs,Is.Zero); Assert.That(off.Diversity.EligibleNearPairs,Is.Zero);
            Assert.That(off.Places.Select(p=>p.Id+"|"+p.Bounds),Is.EqualTo(on.Places.Select(p=>p.Id+"|"+p.Bounds)));
            foreach(string file in new[] {"places.csv","ports.csv","connections.csv"})
            {
                string before=File.ReadAllText(Path.Combine(Root,"MapDesign/MCP/GENERATED/SV5_06_FIX04",file)).Replace("79447cbf21957effa060878c3618d3f831a383e151422458c1c214e27869e4bd","DIGEST").Replace("\r\n","\n");
                string after=(file=="places.csv"?Sv5SpaceGraphExport.PlacesCsv(off):file=="ports.csv"?Sv5SpaceGraphExport.PortsCsv(off):Sv5SpaceGraphExport.ConnectionsCsv(off)).Replace(off.Digest,"DIGEST");
                Assert.That(after,Is.EqualTo(before),file);
            }
            Assert.That(off.Digest,Is.Not.EqualTo(on.Digest),"Policy identity remains in digest even when geometry does not move.");
        }

        [Test] public void V08_FrozenThreeCaveIntegrationBuildsBothActualWorldPlans()
        {
            var profile=Sv5SpaceDiversity.RepeatProfile();
            Assert.That(profile.Families.Where(f=>f.Family=="CAVE_BAND").Select(f=>f.Ordinal),Is.EqualTo(new[] {0,16,32}));
            Assert.That(profile.Families.Where(f=>f.Family=="CAVE_BAND").All(f=>f.Width==60&&f.Height==24&&f.FutureOwner=="SV5_24_CAVE"),Is.True);
            foreach(var plan in new[] {RepeatOff.Value,RepeatOn.Value})
            {
                TestContext.Out.WriteLine("REPEAT|enabled="+plan.Diversity.Profile.Enabled+"|pairs="+plan.Diversity.EligibleNearPairs+"|"+string.Join(";",plan.Diagnostics));
                Assert.That(plan.Places.Count,Is.EqualTo(24));
                Assert.That(plan.Places.Count(p=>p.Family=="CAVE_BAND"),Is.EqualTo(3));
                Assert.That(plan.Success,Is.True,string.Join(";",plan.Diagnostics));
            }
        }

        [Test] public void V09_RealRepeatedPlanReducesPairsWithoutDeletingShrinkingOrRelabeling()
        {
            var off=RepeatOff.Value; var on=RepeatOn.Value;
            Assert.That(off.Diversity.EligibleNearPairs,Is.GreaterThanOrEqualTo(1));
            Assert.That(on.Diversity.EligibleNearPairs,Is.LessThan(off.Diversity.EligibleNearPairs));
            Assert.That(off.Places.Select(p=>p.Family+"|"+p.Kind+"|"+p.Bounds.Width+"x"+p.Bounds.Height).OrderBy(s=>s),
                Is.EqualTo(on.Places.Select(p=>p.Family+"|"+p.Kind+"|"+p.Bounds.Width+"x"+p.Bounds.Height).OrderBy(s=>s)));
            Assert.That(off.Diversity.Decisions.Where(d=>d.Selected).Select(d=>d.RequestId),Is.EqualTo(on.Diversity.Decisions.Where(d=>d.Selected).Select(d=>d.RequestId)));
            Assert.That(off.Core,Is.SameAs(on.Core));
            TestContext.Out.WriteLine("REAL_REPEAT_NEAR_PAIRS_OFF="+off.Diversity.EligibleNearPairs+";ON="+on.Diversity.EligibleNearPairs);
        }

        [Test] public void V10_BothChangedPlansUseUnchangedProductionSafetyAndRejectNegativeGeometry()
        {
            foreach(var plan in new[] {DefaultOn.Value,RepeatOn.Value})
            {
                Assert.That(plan.Core.CoreCells.Count,Is.EqualTo(2432)); Assert.That(plan.Core.Sites.Count,Is.EqualTo(8));
                Assert.That(plan.PhysicalProduct.Success,Is.True,string.Join(";",plan.PhysicalProduct.Diagnostics));
                Assert.That(plan.PhysicalProduct.Proofs.Count,Is.EqualTo(6));
                Assert.That(plan.PhysicalProduct.Matrix.Count,Is.GreaterThan(0));
                Assert.That(plan.PhysicalProduct.Matrix.Where(r=>!r.Success),Is.Empty);
                Assert.That(plan.PhysicalProduct.Proofs.SelectMany(p=>p.DeadEnds),Is.Empty);
                Assert.That(Sv5SpaceGraphValidator.FindGateErrors(plan.ContactDecisions,plan.Gates),Is.Empty);
                Assert.That(Sv5SpacePhysicalMovement.Analyze(plan.Core,plan.Connections,plan.ContactDecisions,plan.Gates.Skip(1)).Success,Is.False);
                var cell=plan.Core.CoreCells.First(c=>c.Protection==RmapSpecialProtectionKind.ProtectedAir);
                Assert.That(Sv5SpaceGraphValidator.FindRequiredReservationConflicts(plan.Core,new[] {
                    new Sv5SpaceReservationProbe(cell.World,Sv5SpaceReservationKind.ConditionalGate,"FOREIGN") }),Is.Not.Empty);
            }
        }

        [Test] public void V11_ExportsBindActualSelectionsGeometryAndPolicyAndAreByteDeterministic()
        {
            var plans=new[]{DefaultOff.Value,DefaultOn.Value,RepeatOff.Value,RepeatOn.Value};
            string directory=Path.Combine(Root,"MapDesign/MCP/GENERATED/SV5_09_LOOPS/_work/legacy_exports/sv5_07_v11");
            Sv5SpaceGraphExport.WriteDiversityComparison(directory,plans);
            var files=new[]{"diversity.json","decisions.csv","pairs.csv","preview/before_after.svg","preview/detail.svg","preview/index.html"}
                .Concat(Directory.GetFiles(Path.Combine(directory,"default"),"*",SearchOption.AllDirectories))
                .Concat(Directory.GetFiles(Path.Combine(directory,"repeat"),"*",SearchOption.AllDirectories))
                .Select(f=>Path.IsPathRooted(f)?f:Path.Combine(directory,f)).ToArray();
            var hashes=files.ToDictionary(f=>f,f=>Hash(File.ReadAllBytes(f)));
            Assert.That(files.Length,Is.GreaterThan(70));
            string json=File.ReadAllText(Path.Combine(directory,"diversity.json"));
            var comparison=JsonUtility.FromJson<ComparisonDto>(json);
            Assert.That(comparison.cases.Length,Is.EqualTo(4));
            for(int i=0;i<4;i++)
            {
                var p=plans[i]; var dto=comparison.cases[i];
                Assert.That(dto.plan.plan_digest,Is.EqualTo(p.Digest));
                Assert.That(dto.plan.seed,Is.EqualTo(p.Seed));
                Assert.That(dto.diversity_digest,Is.EqualTo(p.Diversity.Digest));
                Assert.That(dto.eligible_near_pairs,Is.EqualTo(p.Diversity.EligibleNearPairs));
                Assert.That(dto.plan.places.Select(v=>v.id+"|"+v.bounds.x+","+v.bounds.y+","+v.bounds.width+","+v.bounds.height),
                    Is.EqualTo(p.Places.Select(v=>v.Id+"|"+v.Bounds)));
                Assert.That(dto.plan.ports.Select(v=>v.id),Is.EqualTo(p.Ports.Select(v=>v.Id)));
                Assert.That(dto.plan.connections.Select(v=>v.id),Is.EqualTo(p.Connections.Select(v=>v.Id)));
                Assert.That(dto.plan.gates.Select(v=>v.id),Is.EqualTo(p.Gates.Select(v=>v.Id)));
                foreach(var d in p.Diversity.Decisions.Where(d=>d.Selected))
                    Assert.That(File.ReadAllText(Path.Combine(directory,"decisions.csv")),Does.Contain(d.FormationId));
                Assert.That(File.ReadAllText(Path.Combine(directory,"decisions.csv")),Does.Contain(p.Digest));
                Assert.That(File.ReadAllText(Path.Combine(directory,"pairs.csv")),Does.Contain(p.Digest));
            }
            foreach(string name in new[]{"before_after.svg","detail.svg"})
            {
                string svg=File.ReadAllText(Path.Combine(directory,"preview",name));
                var xml=new System.Xml.XmlDocument(); xml.LoadXml(svg);
                Assert.That(svg,Does.Contain(plans[2].Digest).And.Contain(plans[3].Digest).And.Contain(plans[3].Diversity.Profile.Digest));
                Assert.That(xml.GetElementsByTagName("polyline").Count,Is.EqualTo(plans[2].Connections.Count+plans[3].Connections.Count));
            }
            Sv5SpaceGraphExport.WriteDiversityComparison(directory,plans);
            foreach(string file in files) Assert.That(Hash(File.ReadAllBytes(file)),Is.EqualTo(hashes[file]),file);
            var changed=Sv5SpaceGraphPlanner.Plan(Core.Value,1304,diversity:new Sv5DiversityProfile(true,37));
            Assert.That(changed.Digest,Is.Not.EqualTo(DefaultOn.Value.Digest));
            var randomState=UnityEngine.Random.state;
            try
            {
                UnityEngine.Random.InitState(99871);
                var reverse=new Sv5SpaceGraphAuthoringProfile(DefaultOn.Value.Profile.Id,DefaultOn.Value.Profile.Version,DefaultOn.Value.Profile.Families.Reverse());
                var rebuilt=Sv5SpaceGraphPlanner.Plan(Core.Value,1304,reverse);
                Assert.That(rebuilt.Digest,Is.EqualTo(DefaultOn.Value.Digest));
                Assert.That(Sv5SpaceGraphExport.SpaceGraphJson(rebuilt),Is.EqualTo(Sv5SpaceGraphExport.SpaceGraphJson(DefaultOn.Value)));
            }
            finally { UnityEngine.Random.state=randomState; }
            TestContext.Out.WriteLine("EXPORT_IDENTICAL_FILES="+files.Length+";DEFAULT_OFF="+plans[0].Digest+";DEFAULT_ON="+plans[1].Digest+
                ";REPEAT_OFF="+plans[2].Digest+";REPEAT_ON="+plans[3].Digest);
        }

        [Test] public void V12_HistoricalAlwaysBytesAndSeventyExistingTestNamesRemainUnchanged()
        {
            var locked=JsonUtility.FromJson<LockDto>(File.ReadAllText(Path.Combine(Root,"MapDesign/MCP/INPUTS/SV5_07/SOURCE_LOCK.json")));
            var always=locked.files.Where(f=>f.phase=="ALWAYS").ToArray();
            Assert.That(always.Length,Is.EqualTo(543));
            foreach(var entry in always)
            {
                byte[] bytes=File.ReadAllBytes(Path.Combine(Root,entry.path));
                Assert.That(bytes.LongLength,Is.EqualTo(entry.worktree_bytes),entry.path);
                Assert.That(Hash(bytes),Is.EqualTo(entry.worktree_sha256),entry.path);
            }
            var old=new System.Xml.XmlDocument(); old.Load(Path.Combine(Root,"MapDesign/MCP/GENERATED/SV5_06_FIX04/focused_results.xml"));
            var cases=old.SelectNodes("//test-case").Cast<System.Xml.XmlElement>().ToArray();
            Assert.That(cases.Length,Is.EqualTo(70));
            foreach(var test in cases)
            {
                var type=typeof(Sv5SpaceDiversityTests).Assembly.GetType(test.GetAttribute("classname"),true);
                var method=type.GetMethod(test.GetAttribute("methodname"));
                Assert.That(method,Is.Not.Null,test.GetAttribute("fullname"));
                Assert.That(method.GetCustomAttributes(typeof(TestAttribute),true),Is.Not.Empty);
                Assert.That(method.GetCustomAttributes(typeof(IgnoreAttribute),true),Is.Empty);
            }
            TestContext.Out.WriteLine("ALWAYS_RAW_BYTES_CHECKED="+always.Length+";EXISTING_TEST_NAMES="+cases.Length+
                ";Final focused XML and external pre/post audit remain required; this test does not claim their results.");
        }

        private static string Hash(byte[] bytes) { using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant(); }
        [Serializable] private sealed class ComparisonDto { public CaseDto[] cases; }
        [Serializable] private sealed class CaseDto { public PlanDto plan; public int eligible_near_pairs; public string diversity_digest; }
        [Serializable] private sealed class PlanDto { public string plan_digest; public ulong seed; public PlaceDto[] places; public IdDto[] ports,connections,gates; }
        [Serializable] private sealed class PlaceDto { public string id; public BoundsDto bounds; }
        [Serializable] private sealed class BoundsDto { public int x,y,width,height; }
        [Serializable] private sealed class IdDto { public string id; }
        [Serializable] private sealed class LockDto { public LockEntry[] files; }
        [Serializable] private sealed class LockEntry { public string path,phase,worktree_sha256; public long worktree_bytes; }
        [Serializable] private sealed class ProfileDto { public string id,version,random_salt,distance_metric; public bool enabled; public int near_gap_tiles_inclusive; public int[] pass_percent_by_near_count; }
    }
}
#endif
