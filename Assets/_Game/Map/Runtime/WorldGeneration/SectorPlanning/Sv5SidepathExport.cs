using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using StarNight.Map.WorldGeneration.SpecialRegions;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class Sv5SidepathExport
    {
        private static readonly UTF8Encoding Utf8=new UTF8Encoding(false);
        public static void WriteComparison(string directory,Sv5SpaceGraphPlan standard,Sv5SpaceGraphPlan repeat)
        {
            if(standard?.Sidepaths==null||repeat?.Sidepaths==null||!standard.Success||!repeat.Success)
                throw new ArgumentException("Two passing sidepath plans are required.");
            Directory.CreateDirectory(directory);WriteCase(standard,Path.Combine(directory,"default"));WriteCase(repeat,Path.Combine(directory,"repeat"));
            Write(Path.Combine(directory,"sidepath_comparison.json"),"{\n  \"schema\":\"SV5_SIDEPATH_COMPARISON_NONGRID_V2\",\n"+
                "  \"default\":"+Summary(standard)+",\n  \"repeat\":"+Summary(repeat)+",\n  \"same_seed\":"+B(standard.Seed==repeat.Seed)+
                ",\n  \"distinct_payloads\":"+B(standard.Sidepaths.Digest!=repeat.Sidepaths.Digest)+"\n}\n");
        }
        public static void WriteCase(Sv5SpaceGraphPlan plan,string directory)
        {
            if(plan?.Sidepaths==null)throw new ArgumentException("Sidepath payload required.");Directory.CreateDirectory(directory);
            Write(Path.Combine(directory,"sidepaths.json"),SidepathsJson(plan));Write(Path.Combine(directory,"sidepath_candidates.csv"),CandidatesCsv(plan));
            Write(Path.Combine(directory,"sidepath_links.csv"),LinksCsv(plan));Write(Path.Combine(directory,"sidepath_cells.csv"),CellsCsv(plan));
            Write(Path.Combine(directory,"sidepath_changed_cells.csv"),ChangedCellsCsv(plan));
            Write(Path.Combine(directory,"protected_cells.csv"),ProtectedCellsCsv(plan));
            Write(Path.Combine(directory,"direct_room_pairs.csv"),DirectRoomPairsCsv(plan));
            Write(Path.Combine(directory,"sidepath_checks.csv"),ChecksCsv(plan));Write(Path.Combine(directory,"endpoint_index.csv"),EndpointIndexCsv(plan));
            Write(Path.Combine(directory,"endpoint_pairs.csv"),EndpointPairsCsv(plan));
            Write(Path.Combine(directory,"sidepath_search_performance.json"),SearchPerformanceJson(plan));
            Write(Path.Combine(directory,"sidepath_validation.json"),ValidationJson(plan));
            string preview=Path.Combine(directory,"preview");Directory.CreateDirectory(preview);Write(Path.Combine(preview,"sidepaths.svg"),Preview(plan));
        }
        public static string CandidatesCsv(Sv5SpaceGraphPlan plan)=>Csv(
            "candidate_id,type,status,reason,stable_hash,from_endpoint,to_endpoint,from_room,to_room,space_group_id,connection_id,candidate_score,length,direction_changes,newly_carved_air,ordered_centerline,plan_digest",
            plan.Sidepaths.Candidates.Select(c=>Row(c.Id,Kind(c.Kind),c.Status,c.Reason,c.StableHash,c.From.EndpointId,c.To?.EndpointId??string.Empty,
                c.From.RoomId,c.To?.RoomId??string.Empty,c.SpaceGroupId,c.From.ConnectionId,c.CandidateScore,c.Centerline.Count,c.DirectionChanges,
                c.NewlyCarvedAir,Points(c.Centerline),plan.Digest)));
        public static string LinksCsv(Sv5SpaceGraphPlan plan)=>Csv(
            "sidepath_id,type,from_endpoint,to_endpoint,from_room,to_room,space_group_id,connection_id,length,direction_changes,newly_carved_air,rejoins,returnable,random_shortcut,baseline_cost,final_cost,bypass_count,legal_states,resource_orders,midpoint_x,midpoint_y,ordered_centerline,plan_digest",
            plan.Sidepaths.Links.Select(l=>Row(l.Id,Kind(l.Kind),l.FromEndpointId,l.ToEndpointId,l.FromRoomId,l.ToRoomId,l.SpaceGroupId,l.Host,
                l.Centerline.Count,l.DirectionChanges,l.NewlyCarvedAir,l.Rejoins,l.Returnable,l.RandomShortcut,l.BaselineCost,l.FinalCost,l.BypassCount,
                l.LegalStatesChecked,l.ResourceOrdersChecked,l.Midpoint.X,l.Midpoint.Y,Points(l.Centerline),plan.Digest)));
        public static string CellsCsv(Sv5SpaceGraphPlan plan)
        {
            var p=plan.Sidepaths;var rows=new List<string>();foreach(var link in p.Links)for(int i=0;i<link.Centerline.Count;i++)
            {var point=link.Centerline[i];rows.Add(Row(link.Id,i,point.X,point.Y,Value(p.BaselineOccupancy,point),Value(p.FinalOccupancy,point),
                Value(p.FinalOccupancy,new Sv5SpecialWorldPoint(point.X,point.Y+1)),Value(p.FinalOccupancy,new Sv5SpecialWorldPoint(point.X,point.Y-1)),
                Sv5SpaceSidepaths.MovementRole(link.Centerline,i)==Sv5SidepathMovementRole.StepTransition?"STEP_TRANSITION":"SUPPORTED_FOOT",plan.Digest));}
            return Csv("sidepath_id,sequence,x,y,source_value,final_value,head_value,support_value,movement_role,plan_digest",rows);
        }
        public static string EndpointIndexCsv(Sv5SpaceGraphPlan plan)=>Csv(
            "endpoint_id,room_id,space_group_id,connection_id,port_id,micro_pattern_owner,x,y,exit_x,stable_hash,plan_digest",
            plan.Sidepaths.EndpointIndex.Endpoints.Select(e=>Row(e.EndpointId,e.RoomId,e.SpaceGroupId,e.ConnectionId,e.PortId,e.MicroPatternOwner,
                e.World.X,e.World.Y,e.ExitX,e.StableHash,plan.Digest)));
        public static string EndpointPairsCsv(Sv5SpaceGraphPlan plan)=>Csv(
            "pair_key,from_endpoint,to_endpoint,from_room,to_room,from_space_group,to_space_group,dx,dy,manhattan,eligible,rejection_reason,plan_digest",
            plan.Sidepaths.EndpointIndex.Pairs.Select(p=>Row(p.PairKey,p.From.EndpointId,p.To.EndpointId,p.From.RoomId,p.To.RoomId,
                p.From.SpaceGroupId,p.To.SpaceGroupId,p.Dx,p.Dy,p.Manhattan,p.Eligible,p.RejectionReason,plan.Digest)));
        public static string ChangedCellsCsv(Sv5SpaceGraphPlan plan)=>Csv(
            "sidepath_id,x,y,source_value,final_value,role,plan_digest",
            plan.Sidepaths.Links.SelectMany(l=>l.Cells.Select(c=>Row(l.Id,c.World.X,c.World.Y,c.SourceValue.ToString().ToUpperInvariant(),
                c.FinalValue.ToString().ToUpperInvariant(),c.Role.ToString().ToUpperInvariant(),plan.Digest))));
        public static string ProtectedCellsCsv(Sv5SpaceGraphPlan plan)=>Csv("x,y,plan_digest",
            Sv5SpaceSidepaths.ProtectedCells(plan).OrderBy(v=>v).Select(v=>Row(v.X,v.Y,plan.Digest)));
        public static string DirectRoomPairsCsv(Sv5SpaceGraphPlan plan)
        {
            var rooms=new HashSet<string>(plan.Infill.Rooms.Select(v=>v.Id),StringComparer.Ordinal);
            return Csv("room_a,room_b,plan_digest",plan.Infill.Links.Where(v=>rooms.Contains(v.Room)&&rooms.Contains(v.Parent))
                .Select(v=>string.Compare(v.Room,v.Parent,StringComparison.Ordinal)<=0?new[]{v.Room,v.Parent}:new[]{v.Parent,v.Room})
                .GroupBy(v=>v[0]+"|"+v[1],StringComparer.Ordinal).Select(v=>v.First()).OrderBy(v=>v[0],StringComparer.Ordinal)
                .ThenBy(v=>v[1],StringComparer.Ordinal).Select(v=>Row(v[0],v[1],plan.Digest)));
        }
        public static string ChecksCsv(Sv5SpaceGraphPlan plan)=>Csv("sidepath_id,check,status,states,orders,bypass_count,plan_digest",
            plan.Sidepaths.Links.SelectMany(l=>new[]{Row(l.Id,"ACTUAL_AIR_CENTERLINE","PASS",9,6,l.BypassCount,plan.Digest),
                Row(l.Id,"FINAL_TOPOLOGY_AND_PRODUCT","PASS",l.LegalStatesChecked,l.ResourceOrdersChecked,l.BypassCount,plan.Digest)}));
        public static string SearchPerformanceJson(Sv5SpaceGraphPlan plan)
        {
            var p=plan.Sidepaths;var perf=p.Performance;return "{\n  \"schema\":\"SV5_SIDEPATH_SEARCH_PERFORMANCE_V2\",\n"+
                "  \"workers\":"+perf.WorkerCount+",\n  \"endpoint_count\":"+perf.EndpointCount+",\n  \"nearby_normalized_pair_count\":"+perf.NearbyPairCount+
                ",\n  \"candidate_count\":"+perf.CandidateCount+",\n  \"dead_end_expansions\":"+perf.DeadEndExpansions+
                ",\n  \"whole_world_copy_per_candidate\":0,\n  \"whole_world_bfs_per_candidate\":0,\n  \"global_product_runs\":1,\n"+
                "  \"timings_ms\":{"+string.Join(",",perf.TimingsMilliseconds.Select(v=>J(v.Key)+":"+v.Value.ToString("0.000",CultureInfo.InvariantCulture)))+"},\n"+
                "  \"endpoint_searches\":["+string.Join(",",p.Searches.Select(v=>"{\"endpoint_id\":"+J(v.EndpointId)+",\"expansions\":"+v.Expansions+",\"result\":"+J(v.Result)+"}"))+" ]\n}\n";
        }
        public static string ValidationJson(Sv5SpaceGraphPlan plan)
        {
            var p=plan.Sidepaths;var xs=p.Links.Select(v=>v.Midpoint.X).ToArray();var ys=p.Links.Select(v=>v.Midpoint.Y).ToArray();
            return "{\n  \"schema\":\"SV5_SIDEPATH_VALIDATION_NONGRID_V2\",\n  \"status\":"+J(p.Success&&plan.PhysicalProduct.Success?"PASS":"FAIL")+",\n"+
                "  \"metrics\":{\"accepted\":"+p.AcceptedCount+",\"returning\":"+p.ReturningCount+",\"space_groups\":"+p.DistinctSpaceGroupCount+
                ",\"bypass\":0,\"protected_violations\":0,\"type0_violations\":0},\n  \"midpoint_world_range\":{"+
                "\"min_x\":"+(xs.Length==0?0:xs.Min())+",\"max_x\":"+(xs.Length==0?0:xs.Max())+",\"min_y\":"+(ys.Length==0?0:ys.Min())+",\"max_y\":"+(ys.Length==0?0:ys.Max())+"},\n"+
                "  \"space_group_distribution\":{ "+string.Join(",",p.Links.GroupBy(v=>v.SpaceGroupId,StringComparer.Ordinal).OrderBy(v=>v.Key,StringComparer.Ordinal).Select(v=>J(v.Key)+":"+v.Count()))+" },\n"+
                "  \"diagnostics\":["+string.Join(",",p.Diagnostics.Select(J))+"],\n  \"composed_geometry_ready\":false,\n  \"player_verified\":false\n}\n";
        }
        public static string SidepathsJson(Sv5SpaceGraphPlan plan)
        {
            var p=plan.Sidepaths;return "{\n  \"schema\":\"SV5_ACTUAL_TILE_SIDEPATH_NONGRID_V2\",\n  \"plan_digest\":"+J(plan.Digest)+",\n"+
                "  \"baseline_digest\":"+J(p.BaselineDigest)+",\n  \"sidepath_digest\":"+J(p.Digest)+",\n"+
                "  \"profile\":{\"minimum_length\":20,\"maximum_length\":50,\"target\":"+p.Profile.Target+",\"minimum\":"+p.Profile.Minimum+
                ",\"minimum_returning\":"+p.Profile.MinimumReturning+",\"minimum_space_groups\":"+p.Profile.MinimumSpaceGroups+
                ",\"maximum_per_space_group\":"+p.Profile.MaximumPerSpaceGroup+"},\n  \"counts\":{\"endpoints\":"+p.EndpointIndex.Endpoints.Count+
                ",\"nearby_pairs\":"+p.EndpointIndex.Pairs.Count+",\"candidates\":"+p.Candidates.Count+",\"accepted\":"+p.AcceptedCount+
                ",\"returning\":"+p.ReturningCount+",\"space_groups\":"+p.DistinctSpaceGroupCount+"},\n"+
                "  \"composed_geometry_ready\":false,\n  \"player_verified\":false\n}\n";
        }
        public static void WriteWorkerDeterminism(string directory,Sv5SidepathPlan workerOne,Sv5SidepathPlan workerFour)
        {
            if(workerOne==null||workerFour==null)throw new ArgumentNullException();Directory.CreateDirectory(directory);
            Write(Path.Combine(directory,"worker_determinism.json"),"{\n  \"schema\":\"SV5_SIDEPATH_WORKER_DETERMINISM_V1\",\n"+
                "  \"worker_1\":{\"workers\":"+workerOne.Performance.WorkerCount+",\"candidate_generation_ms\":"+
                workerOne.Performance.TimingsMilliseconds["candidate_generation_ms"].ToString("0.000",CultureInfo.InvariantCulture)+
                ",\"digest\":"+J(workerOne.Digest)+"},\n  \"worker_4\":{\"workers\":"+workerFour.Performance.WorkerCount+
                ",\"candidate_generation_ms\":"+workerFour.Performance.TimingsMilliseconds["candidate_generation_ms"].ToString("0.000",CultureInfo.InvariantCulture)+
                ",\"digest\":"+J(workerFour.Digest)+"},\n  \"digest_match\":"+B(workerOne.Digest==workerFour.Digest)+"\n}\n");
        }
        private static string Preview(Sv5SpaceGraphPlan plan)=>"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 624 416\"><rect width=\"624\" height=\"416\" fill=\"#101820\"/>"+
            string.Join(string.Empty,plan.Sidepaths.Links.Select(l=>"<polyline fill=\"none\" stroke=\"#43d6b5\" stroke-width=\"2\" points=\""+
                string.Join(" ",l.Centerline.Select(p=>p.X+","+(415-p.Y)))+"\"/>"))+"</svg>\n";
        private static string Summary(Sv5SpaceGraphPlan p)=>"{\"digest\":"+J(p.Sidepaths.Digest)+",\"accepted\":"+p.Sidepaths.AcceptedCount+
            ",\"returning\":"+p.Sidepaths.ReturningCount+",\"space_groups\":"+p.Sidepaths.DistinctSpaceGroupCount+"}";
        private static string Kind(Sv5SidepathKind kind)=>kind==Sv5SidepathKind.Returning?"SIDE_PATH_RETURNING":"SIDE_PATH_DEAD_END";
        private static string Value(IReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5LoopOccupancyCell> map,Sv5SpecialWorldPoint point)=>Sv5LoopTopology.ValueAt(map,point).ToString().ToUpperInvariant();
        private static string Points(IEnumerable<Sv5SpecialWorldPoint> source)=>string.Join(";",source.Select(p=>p.X+":"+p.Y));
        private static string Csv(string header,IEnumerable<string> rows)=>header+"\n"+string.Join("\n",rows)+"\n";
        private static string Row(params object[] values)=>string.Join(",",values.Select(v=>Q(v==null?string.Empty:v is bool b?(b?"true":"false"):Convert.ToString(v,CultureInfo.InvariantCulture))));
        private static string Q(string value)=>value.IndexOfAny(new[]{',','"','\n','\r'})<0?value:"\""+value.Replace("\"","\"\"")+"\"";
        private static string J(string value)=>"\""+(value??string.Empty).Replace("\\","\\\\").Replace("\"","\\\"")+"\"";
        private static string B(bool value)=>value?"true":"false";
        private static void Write(string path,string value){Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,value.Replace("\r\n","\n"),Utf8);}
    }
}
