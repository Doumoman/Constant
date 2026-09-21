using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.WorldData;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public sealed class Sv5InfillProfile
    {
        public const string ConnectionLengthPolicy = "SV5_INFILL_LENGTH_RULE_V2";

        public Sv5InfillProfile(bool enabled = true, int target = 256, int minimum = 128,
            int maximum = 384, int minimumSectors = 12, int roomsPerSector = 6, int minimumTiles = 24576)
        {
            if (minimum < 0 || target < minimum || maximum < target) throw new ArgumentException("Invalid density bounds.");
            Enabled=enabled; Target=target; Minimum=minimum; Maximum=maximum; MinimumSectors=minimumSectors;
            RoomsPerSector=roomsPerSector; MinimumTiles=minimumTiles;
        }
        public bool Enabled { get; }
        public int Target { get; }
        public int Minimum { get; }
        public int Maximum { get; }
        public int MinimumSectors { get; }
        public int RoomsPerSector { get; }
        public int MinimumTiles { get; }
        public int MaximumDepth => 12;
        public int MaximumLink => 24;
        public string Digest => Sv5WorldDefinition.Hash("SV5_ORDINARY_INFILL_V1|2.0.0|"+Enabled+"|"+Target+"|"+Minimum+"|"+Maximum+"|"+
            MinimumSectors+"|"+RoomsPerSector+"|"+MinimumTiles+"|4|12|24|3|2|1|0.4x0.8|"+ConnectionLengthPolicy);
    }

    public sealed class Sv5InfillRoom
    {
        internal Sv5InfillRoom(string id,string recipe,Sv5SpaceBounds bounds,bool mirror,string parent,string host,int depth,
            IEnumerable<Sv5InfillCell> cells,Sv5SpecialWorldPoint entry,Sv5SpecialWorldPoint deep)
        { Id=id; Recipe=recipe; Bounds=bounds; Mirror=mirror; Parent=parent; Host=host; Depth=depth; Cells=Array.AsReadOnly(cells.ToArray()); Entry=entry; Deep=deep; }
        public string Id { get; }
        public string Recipe { get; }
        public Sv5SpaceBounds Bounds { get; }
        public bool Mirror { get; }
        public string Parent { get; }
        public string Host { get; }
        public int Depth { get; }
        public IReadOnlyList<Sv5InfillCell> Cells { get; }
        public Sv5SpecialWorldPoint Entry { get; }
        public Sv5SpecialWorldPoint Deep { get; }
        public int Sector => Math.Min(3,(Bounds.Y+Bounds.Height/2)/104)*4+Math.Min(3,(Bounds.X+Bounds.Width/2)/156);
        public string Token => Id+"|"+Recipe+"|"+Bounds+"|"+Mirror+"|"+Parent+"|"+Host+"|"+Depth+"|"+Entry+"|"+Deep;
    }

    public sealed class Sv5InfillLink
    {
        internal Sv5InfillLink(string room,string parent,string host,IEnumerable<Sv5SpecialWorldPoint> path,
            IEnumerable<Sv5SpaceBoundaryFace> faces,IEnumerable<Sv5InfillCell> cells,Sv5InfillStaticProof proof,
            IEnumerable<Sv5SpecialWorldPoint> hostAccess=null)
        {
            Room=room; Parent=parent; Host=host; Path=Array.AsReadOnly(path.ToArray()); Faces=Array.AsReadOnly(faces.ToArray());
            Cells=Array.AsReadOnly(cells.ToArray()); Proof=proof;
            HostAccess=Array.AsReadOnly((hostAccess ?? Array.Empty<Sv5SpecialWorldPoint>()).ToArray());
            ConnectionCenterline=Sv5SpaceInfill.ConnectionCenterline(Path,HostAccess);
            var external=new HashSet<Sv5SpecialWorldPoint>(Cells.Select(c=>c.World));
            ExternalCenterline=Array.AsReadOnly(HostAccess.Concat(Path).Distinct().Where(external.Contains).ToArray());
        }
        public string Room { get; }
        public string Parent { get; }
        public string Host { get; }
        public IReadOnlyList<Sv5SpecialWorldPoint> Path { get; }
        /// <summary>The authoritative ordered cardinal AIR visits for the ordinary connector.
        /// A root includes its old-host anchor and removes only the HostAccess/Path join once;
        /// a child is exactly Path. Visits are never ownership-filtered or de-duplicated.</summary>
        public IReadOnlyList<Sv5SpecialWorldPoint> ConnectionCenterline { get; }
        public int ConnectionCellCount => ConnectionCenterline.Count;
        public IReadOnlyList<Sv5SpecialWorldPoint> ExternalCenterline { get; }
        public IReadOnlyList<Sv5SpecialWorldPoint> HostAccess { get; }
        public IReadOnlyList<Sv5SpaceBoundaryFace> Faces { get; }
        public IReadOnlyList<Sv5InfillCell> Cells { get; }
        public Sv5InfillStaticProof Proof { get; }
        public string Token => Room+"|"+Parent+"|"+Host+"|"+string.Join(";",Path)+"|"+string.Join(";",Faces.Select(f=>f.StableToken))+"|"+
            string.Join(";",Proof.Approach)+"|"+string.Join(";",Proof.Return)+"|"+Proof.Reason+"|"+string.Join(";",HostAccess)+"|"+
            Sv5InfillProfile.ConnectionLengthPolicy+"|"+string.Join(";",ConnectionCenterline);
    }

    public sealed class Sv5InfillPlan
    {
        internal Sv5InfillPlan(string baseline,Sv5InfillProfile profile,IEnumerable<Sv5InfillRoom> rooms,
            IEnumerable<Sv5InfillLink> links,IEnumerable<Sv5InfillCell> cells,IEnumerable<string> diagnostics,
            IDictionary<string,int> rejections,string termination,ISet<Sv5SpecialWorldPoint> previouslyReserved=null)
        {
            BaselineDigest=baseline; Profile=profile; Rooms=Array.AsReadOnly(rooms.OrderBy(r=>r.Id,StringComparer.Ordinal).ToArray());
            Links=Array.AsReadOnly(links.OrderBy(l=>l.Room,StringComparer.Ordinal).ToArray());
            Cells=Array.AsReadOnly(cells.OrderBy(c=>c.World).ToArray());
            Instances=Array.AsReadOnly(Rooms.SelectMany(r=>Sv5InfillPatterns.Split(Cells.Where(c=>c.Owner==r.Id),r.Mirror))
                .Concat(Sv5InfillPatterns.Split(Cells.Where(c=>!Rooms.Any(r=>r.Id==c.Owner)))).ToArray());
            Diagnostics=Array.AsReadOnly(diagnostics.Distinct().OrderBy(x=>x,StringComparer.Ordinal).ToArray());
            Rejections=new ReadOnlyDictionary<string,int>(new SortedDictionary<string,int>(rejections,StringComparer.Ordinal)); Termination=termination;
            PreviouslyReservedCells=Array.AsReadOnly((previouslyReserved ?? new HashSet<Sv5SpecialWorldPoint>()).OrderBy(p=>p).ToArray());
            NewOwnedTiles=Cells.Count(c=>!c.Shared &&
                (previouslyReserved==null || !previouslyReserved.Contains(c.World)));
            SectorCounts=Array.AsReadOnly(Enumerable.Range(0,16).Select(s=>Rooms.Count(r=>r.Sector==s)).ToArray());
            Digest=Sv5WorldDefinition.Hash(baseline+"\n"+profile.Digest+"\n"+string.Join("\n",Rooms.Select(r=>r.Token))+
                "\n"+string.Join("\n",Cells.Select(c=>c.Token))+"\n"+string.Join("\n",Instances.Select(i=>i.Token))+
                "\n"+string.Join("\n",Links.Select(l=>l.Token))+"\n"+string.Join("\n",Diagnostics)+"\n"+termination);
        }
        public string BaselineDigest { get; }
        public Sv5InfillProfile Profile { get; }
        public IReadOnlyList<Sv5InfillRoom> Rooms { get; }
        public IReadOnlyList<Sv5InfillLink> Links { get; }
        public IReadOnlyList<Sv5InfillCell> Cells { get; }
        public IReadOnlyList<Sv5InfillInstance> Instances { get; }
        public IReadOnlyList<string> Diagnostics { get; }
        public IReadOnlyDictionary<string,int> Rejections { get; }
        public IReadOnlyList<int> SectorCounts { get; }
        public string Termination { get; }
        public string Digest { get; }
        public bool Success => Diagnostics.Count==0;
        public int NewRoomCount => Rooms.Count;
        public int NewOwnedTiles { get; }
        public IReadOnlyList<Sv5SpecialWorldPoint> PreviouslyReservedCells { get; }
    }

    /// <summary>Small immutable boundary between the historical graph and the SV5_08 generator.
    /// Generation consumes these copied facts; it never consumes the historical plan's success flag,
    /// projection proofs, contact decisions, gate checks, diversity trace, or exporter state.</summary>
    public sealed class Sv5InfillSource
    {
        internal Sv5InfillSource(Sv5SpaceGraphPlan plan)
        {
            if(plan==null) throw new ArgumentNullException(nameof(plan));
            BaselineDigest=plan.Digest; Seed=plan.Seed; Core=plan.Core;
            Places=Array.AsReadOnly(plan.Places.Select(p=>new SourcePlace(p.Id,p.Kind,p.Bounds,p.FutureOwner)).ToArray());
            Connections=Array.AsReadOnly(plan.Connections.Select(c=>new SourceConnection(c.Id,c.Kind,c.Flow,
                c.FromPlaceId,c.ToPlaceId,c.Centerline,c.ApertureCells,c.Envelope)).ToArray());
            ReservationCells=Array.AsReadOnly(plan.Reservations.Select(r=>r.World).Distinct().OrderBy(p=>p).ToArray());
            Envelope=new HashSet<Sv5SpecialWorldPoint>(Connections.SelectMany(c=>c.Envelope));
            Passage=new HashSet<Sv5SpecialWorldPoint>(Connections.SelectMany(c=>c.Centerline.Concat(c.ApertureCells)));
            Protected=new HashSet<Sv5SpecialWorldPoint>(Core.CoreCells.Select(c=>c.World));
            Reserved=new HashSet<Sv5SpecialWorldPoint>(Core.RouteCells.Select(c=>c.World));
            PlaceFootprint=new HashSet<Sv5SpecialWorldPoint>();
            foreach(var place in Places) for(int y=place.Bounds.Y;y<place.Bounds.MaxYExclusive;y++)
                for(int x=place.Bounds.X;x<place.Bounds.MaxXExclusive;x++) PlaceFootprint.Add(new Sv5SpecialWorldPoint(x,y));
            Type0=new HashSet<Sv5SpecialWorldPoint>(Core.RouteSource.Secrets.SelectMany(secret=>secret.Chunks)
                .SelectMany(chunk=>Enumerable.Range(0,8).SelectMany(y=>Enumerable.Range(0,12)
                    .Select(x=>new Sv5SpecialWorldPoint(chunk.X*12+x,chunk.Y*8+y)))));
            PassageOwners=Connections.SelectMany(c=>c.Centerline.Concat(c.ApertureCells).Distinct().Select(p=>new { Point=p,c.Id }))
                .GroupBy(v=>v.Point).ToDictionary(g=>g.Key,g=>new HashSet<string>(g.Select(v=>v.Id),StringComparer.Ordinal));
        }
        public string BaselineDigest { get; }
        public ulong Seed { get; }
        public Sv5CoreReservationPlan Core { get; }
        public IReadOnlyList<SourcePlace> Places { get; }
        public IReadOnlyList<SourceConnection> Connections { get; }
        public IReadOnlyList<Sv5SpecialWorldPoint> ReservationCells { get; }
        internal HashSet<Sv5SpecialWorldPoint> Envelope { get; }
        internal HashSet<Sv5SpecialWorldPoint> Passage { get; }
        internal HashSet<Sv5SpecialWorldPoint> Protected { get; }
        internal HashSet<Sv5SpecialWorldPoint> Reserved { get; }
        internal HashSet<Sv5SpecialWorldPoint> Type0 { get; }
        internal HashSet<Sv5SpecialWorldPoint> PlaceFootprint { get; }
        internal Dictionary<Sv5SpecialWorldPoint,HashSet<string>> PassageOwners { get; }
    }

    public sealed class SourcePlace
    {
        internal SourcePlace(string id,Sv5SpacePlaceKind kind,Sv5SpaceBounds bounds,string futureOwner)
        { Id=id; Kind=kind; Bounds=bounds; FutureOwner=futureOwner; }
        public string Id { get; }
        public Sv5SpacePlaceKind Kind { get; }
        public Sv5SpaceBounds Bounds { get; }
        public string FutureOwner { get; }
    }

    public sealed class SourceConnection
    {
        internal SourceConnection(string id,Sv5SpaceConnectionKind kind,string flow,string from,string to,
            IEnumerable<Sv5SpecialWorldPoint> centerline,IEnumerable<Sv5SpecialWorldPoint> aperture,
            IEnumerable<Sv5SpecialWorldPoint> envelope)
        {
            Id=id; Kind=kind; Flow=flow; FromPlaceId=from; ToPlaceId=to;
            Centerline=Array.AsReadOnly(centerline.ToArray()); ApertureCells=Array.AsReadOnly(aperture.ToArray());
            Envelope=Array.AsReadOnly(envelope.ToArray());
        }
        public string Id { get; }
        public Sv5SpaceConnectionKind Kind { get; }
        public string Flow { get; }
        public string FromPlaceId { get; }
        public string ToPlaceId { get; }
        public IReadOnlyList<Sv5SpecialWorldPoint> Centerline { get; }
        public IReadOnlyList<Sv5SpecialWorldPoint> ApertureCells { get; }
        public IReadOnlyList<Sv5SpecialWorldPoint> Envelope { get; }
    }

    public static class Sv5SpaceInfill
    {
        public static Sv5InfillSource Capture(Sv5SpaceGraphPlan baseline) => new Sv5InfillSource(baseline);
        private static Sv5SpecialWorldPoint P(int x,int y) => new Sv5SpecialWorldPoint(x,y);
        private static IEnumerable<Sv5SpecialWorldPoint> Neighbors(Sv5SpecialWorldPoint p)
        { yield return P(p.X-1,p.Y); yield return P(p.X+1,p.Y); yield return P(p.X,p.Y-1); yield return P(p.X,p.Y+1); }
        private static IEnumerable<Sv5SpecialWorldPoint> Footprint(Sv5SpaceBounds b)
        { for(int y=b.Y;y<b.MaxYExclusive;y++) for(int x=b.X;x<b.MaxXExclusive;x++) yield return P(x,y); }

        /// <summary>Builds the authoritative connector witness without ownership filtering or
        /// global de-duplication. HostAccess is present only for a root and shares Path[0].</summary>
        public static IReadOnlyList<Sv5SpecialWorldPoint> ConnectionCenterline(
            IEnumerable<Sv5SpecialWorldPoint> path,IEnumerable<Sv5SpecialWorldPoint> hostAccess=null)
        {
            var route=(path ?? Array.Empty<Sv5SpecialWorldPoint>()).ToArray();
            var access=(hostAccess ?? Array.Empty<Sv5SpecialWorldPoint>()).ToArray();
            return Array.AsReadOnly((access.Length==0 ? route : access.Concat(route.Skip(1))).ToArray());
        }

        /// <summary>Shared production rule used by candidate acceptance and final payload validation.</summary>
        public static IReadOnlyList<string> FindConnectionLengthErrors(IEnumerable<Sv5SpecialWorldPoint> path,
            IEnumerable<Sv5SpecialWorldPoint> hostAccess,int maximum,bool root)
        {
            var route=(path ?? Array.Empty<Sv5SpecialWorldPoint>()).ToArray();
            var access=(hostAccess ?? Array.Empty<Sv5SpecialWorldPoint>()).ToArray();
            var errors=new List<string>();
            if(route.Length==0) errors.Add("EMPTY_PATH");
            if(root)
            {
                if(access.Length<2) errors.Add("ROOT_ACCESS_TOO_SHORT");
                if(route.Length>0 && access.Length>0 && !access[access.Length-1].Equals(route[0])) errors.Add("ROOT_JOIN_MISMATCH");
            }
            else if(access.Length!=0) errors.Add("CHILD_HAS_HOST_ACCESS");
            var complete=ConnectionCenterline(route,access);
            if(complete.Zip(complete.Skip(1),(a,b)=>Math.Abs(a.X-b.X)+Math.Abs(a.Y-b.Y)).Any(d=>d!=1)) errors.Add("NON_CARDINAL_STEP");
            if(complete.Count>maximum) errors.Add("OVER_MAXIMUM|"+complete.Count+"/"+maximum);
            return Array.AsReadOnly(errors.ToArray());
        }

        /// <summary>Expand supported +1 steps into the actual cardinal AIR cells. A diagonal
        /// support-to-support step is TWO graph edges, never a single centerline cell.</summary>
        public static IReadOnlyList<Sv5SpecialWorldPoint> CardinalCenterline(IEnumerable<Sv5SpecialWorldPoint> source)
        {
            var feet=source.ToArray(); var result=new List<Sv5SpecialWorldPoint>();
            if(feet.Length==0) return Array.AsReadOnly(result.ToArray());
            result.Add(feet[0]);
            for(int i=1;i<feet.Length;i++)
            {
                var a=feet[i-1]; var b=feet[i];
                if(Math.Abs(a.X-b.X)!=1 || Math.Abs(a.Y-b.Y)>1)
                    throw new ArgumentException("Supported centerline requires adjacent columns and at most +1/-1 rise.");
                if(b.Y>a.Y) result.Add(P(a.X,b.Y));
                if(b.Y<a.Y) result.Add(P(b.X,a.Y));
                result.Add(b);
            }
            return Array.AsReadOnly(result.ToArray());
        }

        public static IReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5InfillCellValue> KnownBase(Sv5SpaceGraphPlan plan)
            => KnownBase(Capture(plan));

        public static IReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5InfillCellValue> KnownBase(Sv5InfillSource input)
        {
            var cells=new Dictionary<Sv5SpecialWorldPoint,Sv5InfillCellValue>();
            foreach(var c in input.Core.CoreCells)
                if(c.BaseCell==Sv5PatternBaseCell.Air || c.BaseCell==Sv5PatternBaseCell.Solid)
                    cells[c.World]=c.BaseCell==Sv5PatternBaseCell.Solid ? Sv5InfillCellValue.Solid : Sv5InfillCellValue.Air;
            foreach(var c in input.Core.RouteCells.Where(c=>c.RequiredBaseCell==Sv5PatternBaseCell.Solid)) cells[c.World]=Sv5InfillCellValue.Solid;
            foreach(var c in input.Core.RouteSource.Cells.Where(c=>c.SourceKind==
                StarNight.Map.WorldGeneration.TerrainClusters.Sv5TerrainSourceKind.Secret))
                if(c.BaseCell==Sv5PatternBaseCell.Solid || c.BaseCell==Sv5PatternBaseCell.Air)
                    cells[P(c.X,c.Y)]=c.BaseCell==Sv5PatternBaseCell.Solid ? Sv5InfillCellValue.Solid : Sv5InfillCellValue.Air;
            return new ReadOnlyDictionary<Sv5SpecialWorldPoint,Sv5InfillCellValue>(cells);
        }

        public static IReadOnlyList<string> ValidateCandidate(Sv5SpaceGraphPlan baseline,IEnumerable<Sv5InfillCell> source,
            IEnumerable<Sv5SpaceBoundaryFace> allowedFaces,string host)
            => ValidateCandidate(Capture(baseline),source,allowedFaces,host);

        public static IReadOnlyList<string> ValidateCandidate(Sv5InfillSource input,IEnumerable<Sv5InfillCell> source,
            IEnumerable<Sv5SpaceBoundaryFace> allowedFaces,string host)
        {
            var cells=source.ToArray(); var errors=new List<string>();
            allowedFaces=(allowedFaces ?? Array.Empty<Sv5SpaceBoundaryFace>()).ToArray();
            if(!input.Connections.Any(c=>c.Id==host &&
                c.Kind==Sv5SpaceConnectionKind.OptionalBranch && c.Flow=="BIDIRECTIONAL"))
                errors.Add("UNKNOWN_OR_GUARDED_HOST|"+host);
            var envelope=input.Envelope; var passage=input.Passage;
            var protectedCells=input.Protected; var reserved=input.Reserved;
            var faces=new HashSet<string>(allowedFaces.Select(f=>f.StableToken));
            var air=new HashSet<Sv5SpecialWorldPoint>(cells.Where(c=>c.Value==Sv5InfillCellValue.Air).Select(c=>c.World));
            foreach(var face in allowedFaces)
            foreach(var point in new[]{face.First,face.Second})
                if(input.PassageOwners.TryGetValue(point,out var owners) && (owners.Count!=1 || !owners.Contains(host)))
                    errors.Add("DECLARED_FACE_FOREIGN_HOST|"+point);
            foreach(var c in cells)
            {
                if(c.World.X<0 || c.World.X>=624 || c.World.Y<0 || c.World.Y>=416) errors.Add("OUT_OF_WORLD|"+c.World);
                if(protectedCells.Contains(c.World)) errors.Add("CORE_PROTECTED|"+c.World);
                if(input.Type0.Contains(c.World)) errors.Add("TYPE0_PROTECTED|"+c.World);
                if(input.PlaceFootprint.Contains(c.World)) errors.Add("OLD_PLACE_OVERLAP|"+c.World);
                if(c.Value==Sv5InfillCellValue.Solid && (envelope.Contains(c.World) || reserved.Contains(c.World))) errors.Add("SOLID_RESERVED_PASSAGE_CLEARANCE|"+c.World);
                if(c.Value!=Sv5InfillCellValue.Air) continue;
                if(passage.Contains(c.World) && !(c.Shared &&
                    allowedFaces.Any(f=>f.First.Equals(c.World) || f.Second.Equals(c.World)) &&
                    input.Connections.Where(connection=>connection.Id==host).Any(connection=>connection.Centerline.Contains(c.World))))
                    errors.Add("UNDECLARED_SHARED_PASSAGE|"+c.World);
                foreach(var n in Neighbors(c.World).Where(passage.Contains))
                    if(!air.Contains(n) && !faces.Contains(new Sv5SpaceBoundaryFace(c.World,n).StableToken)) errors.Add("SECOND_OR_UNDECLARED_HOST_CONTACT|"+c.World+">"+n);
            }
            var protectedDecision=input.Core.EvaluateTerrainCandidates(cells.Where(c=>!protectedCells.Contains(c.World)).Select(c=>
                new Sv5CoreTerrainCandidate(c.Owner,c.World,c.Value==Sv5InfillCellValue.Air ? Sv5PatternBaseCell.Air : Sv5PatternBaseCell.Solid)));
            errors.AddRange(protectedDecision.Diagnostics.Select(d=>"CORE_RESERVATION|"+d.World+"|"+d.Code));
            return Array.AsReadOnly(errors.Distinct().ToArray());
        }

        private sealed class Candidate
        {
            public string Parent,Host,Recipe,Id;
            public int X,Y,Depth,Direction,Sector;
            public ulong Rank;
            public bool Mirror;
            public Sv5SpecialWorldPoint Start;
            public bool SidePortal;
            public Sv5SpecialWorldPoint HostAnchor;
            public Sv5InfillRoom ParentRoom;
        }

        public static IReadOnlyList<string> ValidatePayload(Sv5SpaceGraphPlan baseline,Sv5InfillPlan payload)
            => ValidatePayload(Capture(baseline),payload);

        public static IReadOnlyList<string> ValidatePayload(Sv5InfillSource input,Sv5InfillPlan payload)
        {
            var errors=new List<string>(payload.Diagnostics);
            if(payload.BaselineDigest!=input.BaselineDigest) errors.Add("BASELINE_DIGEST_MISMATCH");
            if(payload.Cells.GroupBy(c=>c.World).Any(g=>g.Count()!=1))
                return Array.AsReadOnly(new[]{"DUPLICATE_OWNED_WORLD_CELL"});
            var cells=payload.Cells.ToDictionary(c=>c.World,c=>c.Value);
            var prior=new HashSet<Sv5SpecialWorldPoint>(input.ReservationCells);
            prior.UnionWith(input.Envelope); prior.UnionWith(input.Type0);
            if(!prior.SetEquals(payload.PreviouslyReservedCells)) errors.Add("PRIOR_OWNERSHIP_MISMATCH");
            var newRooms=payload.Rooms.ToArray();
            int area=payload.Cells.Count(c=>!c.Shared && !prior.Contains(c.World));
            if(area!=payload.NewOwnedTiles) errors.Add("NEW_OWNED_TILE_COUNT_MISMATCH");
            if(newRooms.Length<payload.Profile.Minimum || newRooms.Length>payload.Profile.Maximum) errors.Add("ROOM_DENSITY_OUT_OF_RANGE");
            if(area<payload.Profile.MinimumTiles) errors.Add("OWNED_AREA_BELOW_MINIMUM");
            if(Enumerable.Range(0,16).Count(s=>newRooms.Count(r=>r.Sector==s)>=payload.Profile.RoomsPerSector)<payload.Profile.MinimumSectors)
                errors.Add("SECTOR_DENSITY_BELOW_MINIMUM");
            if(payload.Rooms.Select(r=>r.Id).Distinct().Count()!=payload.Rooms.Count ||
                !payload.Rooms.Select(r=>r.Id).OrderBy(id=>id).SequenceEqual(payload.Links.Select(l=>l.Room).OrderBy(id=>id)))
                return Array.AsReadOnly(errors.Concat(new[]{"ROOM_LINK_BIJECTION_MISMATCH"}).ToArray());
            var oldPassage=new HashSet<Sv5SpecialWorldPoint>(input.Connections.SelectMany(c=>c.Centerline.Concat(c.ApertureCells)));
            try
            {
                var rebuilt=Sv5InfillPatterns.Reconstruct(payload.Instances);
                if(rebuilt.Count!=cells.Count || cells.Any(c=>!rebuilt.TryGetValue(c.Key,out var value) || value!=c.Value))
                    errors.Add("PATTERN_RECONSTRUCTION_MISMATCH");
            }
            catch(ArgumentException e) { errors.Add("PATTERN_OWNERSHIP|"+e.Message); }
            foreach(var group in payload.Cells.GroupBy(c=>c.Host))
                errors.AddRange(ValidateCandidate(input,group,payload.Links.Where(l=>l.Host==group.Key).SelectMany(l=>l.Faces),group.Key));
            foreach(var room in payload.Rooms)
            {
                if(!room.Cells.OrderBy(c=>c.World).Select(c=>c.Token).SequenceEqual(
                    payload.Cells.Where(c=>c.Owner==room.Id).OrderBy(c=>c.World).Select(c=>c.Token)))
                    errors.Add("ROOM_CELL_VIEW_MISMATCH|"+room.Id);
                if(!Sv5InfillPatterns.Screen(cells,room.Entry,room.Deep).Success)
                    errors.Add("ROOM_STATIC_SCREEN|"+room.Id);
            }
            foreach(var link in payload.Links)
            {
                var room=payload.Rooms.SingleOrDefault(r=>r.Id==link.Room);
                if(room==null || link.Path.Count==0) { errors.Add("LINK_WITHOUT_ROOM_OR_PATH|"+link.Room); continue; }
                var parent=payload.Rooms.SingleOrDefault(r=>r.Id==room.Parent);
                bool root=parent==null;
                    if(!link.Path.Last().Equals(room.Entry)) errors.Add("CONNECTOR_CHILD_ENDPOINT_MISMATCH|"+link.Room);
                    errors.AddRange(FindConnectionLengthErrors(link.Path,link.HostAccess,payload.Profile.MaximumLink,root)
                        .Select(error=>"CONNECTOR_"+error+"|"+link.Room));
                    if(!root)
                    {
                        var first=link.Path[0]; var bounds=parent.Bounds;
                        if(!bounds.Contains(first) || (first.X!=bounds.X && first.X!=bounds.MaxXExclusive-1 &&
                            first.Y!=bounds.Y && first.Y!=bounds.MaxYExclusive-1))
                            errors.Add("CONNECTOR_PARENT_BOUNDARY_MISMATCH|"+link.Room);
                    }
                    if(link.ConnectionCenterline.Any(p=>!oldPassage.Contains(p) &&
                        (!cells.TryGetValue(p,out var value) || value!=Sv5InfillCellValue.Air)))
                        errors.Add("CONNECTOR_CENTERLINE_NOT_AIR|"+link.Room);
                if(room.Depth>payload.Profile.MaximumDepth ||
                    room.Depth!=(parent==null ? 1 : parent.Depth+1) || (parent!=null && parent.Host!=room.Host))
                    errors.Add("PARENT_DEPTH_OR_HOST|"+link.Room);
                if(parent==null && (link.HostAccess.Count<2 || !link.HostAccess.Last().Equals(link.Path[0]) ||
                    !input.Connections.Single(c=>c.Id==link.Host).Centerline.Contains(link.HostAccess[0]) ||
                    link.HostAccess.Any(p=>!oldPassage.Contains(p) && (!cells.TryGetValue(p,out var v) || v!=Sv5InfillCellValue.Air)) ||
                    link.HostAccess.Zip(link.HostAccess.Skip(1),(a,b)=>Math.Abs(a.X-b.X)+Math.Abs(a.Y-b.Y)).Any(d=>d!=1)))
                    errors.Add("ROOT_HOST_APERTURE_DISCONNECTED|"+room.Id);
                var start=parent==null ? link.Path[0] : parent.Entry;
                if(!Sv5InfillPatterns.Screen(cells,start,room.Deep).Success) errors.Add("PARENT_RETURN_STATIC_SCREEN|"+room.Id);
                if(link.Path.Any(p=>!cells.TryGetValue(p,out var v) || v!=Sv5InfillCellValue.Air)) errors.Add("LINK_PATH_NOT_OWNED_AIR|"+room.Id);
                if(!link.Proof.Success || link.Proof.Approach.Any(p=>!cells.TryGetValue(p,out var v) || v!=Sv5InfillCellValue.Air) ||
                    link.Proof.Return.Any(p=>!cells.TryGetValue(p,out var v) || v!=Sv5InfillCellValue.Air))
                    errors.Add("STALE_STATIC_WITNESS|"+room.Id);
            }
            var known=KnownBase(input).ToDictionary(c=>c.Key,c=>c.Value);
            foreach(var c in payload.Cells) known[c.World]=c.Value;
            errors.AddRange(Sv5InfillPatterns.SolidWindows(known).Select(p=>"SOLID_6X6|"+p));
            return Array.AsReadOnly(errors.Distinct().OrderBy(e=>e,StringComparer.Ordinal).ToArray());
        }

        public static Sv5InfillPlan Build(Sv5SpaceGraphPlan baseline,Sv5InfillProfile profile=null)
            => Build(Capture(baseline),profile);

        public static Sv5InfillPlan Build(Sv5InfillSource input,Sv5InfillProfile profile=null)
        {
            profile=profile ?? new Sv5InfillProfile();
            // One deterministic queue is evaluated to TARGET_REACHED or genuine exhaustion.
            // Rebuilding the complete world up to 32 times and banning the previous leaves did
            // not prove anything new: each run repeated the same acceptance predicates. Candidate
            // rejection counts from this one
            // exhaustive queue are the authoritative shortfall evidence.
            return BuildAttempt(input,profile,new HashSet<string>(StringComparer.Ordinal));
        }

        private static Sv5InfillPlan BuildAttempt(Sv5InfillSource input,Sv5InfillProfile profile,ISet<string> banned)
        {
            if(input==null) throw new ArgumentNullException(nameof(input));
            profile=profile ?? new Sv5InfillProfile();
            var rooms=new List<Sv5InfillRoom>(); var links=new List<Sv5InfillLink>();
            var all=new Dictionary<Sv5SpecialWorldPoint,Sv5InfillCell>(); var diagnostics=new List<string>();
            var rejected=new Dictionary<string,int>();
            void Reject(string reason) { rejected[reason]=rejected.TryGetValue(reason,out int n) ? n+1 : 1; }
            var reserved=new HashSet<Sv5SpecialWorldPoint>(input.ReservationCells);
            reserved.UnionWith(input.Envelope); reserved.UnionWith(input.Type0);
            var passage=new HashSet<Sv5SpecialWorldPoint>(input.Passage);
            var baseCells=KnownBase(input);
            var candidateComparer=Comparer<Candidate>.Create((a,b)=>
            {
                int n=a.Rank.CompareTo(b.Rank); if(n!=0) return n;
                n=string.CompareOrdinal(a.Id,b.Id); if(n!=0) return n;
                n=a.Start.Y.CompareTo(b.Start.Y); if(n!=0) return n;
                n=a.Start.X.CompareTo(b.Start.X); return n!=0 ? n : a.SidePortal.CompareTo(b.SidePortal);
            });
            var queue=Enumerable.Range(0,16).Select(s=>new SortedSet<Candidate>(candidateComparer)).ToArray();
            var sectors=new int[16];
            var seen=new HashSet<string>(); var usedDoors=new HashSet<string>();
            var footprintFree=new Dictionary<Sv5SpaceBounds,bool>();
            bool BaselineFootprintFree(Sv5SpaceBounds bounds)
            {
                if(!footprintFree.TryGetValue(bounds,out var free)) footprintFree[bounds]=free=!Footprint(bounds).Any(reserved.Contains);
                return free;
            }

            void Enqueue(string parent,string host,Sv5SpecialWorldPoint start,int direction,int depth,Sv5InfillRoom parentRoom,
                bool sidePortal=false,Sv5SpecialWorldPoint hostAnchor=default)
            {
                if(depth>profile.MaximumDepth) return;
                foreach(string recipe in Sv5InfillPatterns.Recipes)
                foreach(int distance in Enumerable.Range(1,profile.MaximumLink))
                {
                    var rb=Sv5InfillPatterns.Bounds(recipe);
                    int doorX=start.X+direction*distance;
                    int x=direction>0 ? doorX : doorX-rb.Width+1;
                    if(x%4!=0) continue;
                    for(int y=(int)Math.Floor((start.Y-distance-3)/4.0)*4;y<=start.Y+distance-3;y+=4)
                    {
                        int delta=y+3-start.Y;
                        int prefix=parentRoom==null ? (sidePortal ? 2 : 1) : 0;
                        if(Math.Abs(delta)>distance || 1+distance+Math.Abs(delta)+prefix>profile.MaximumLink) continue;
                        if(x<2 || y<2 || x+rb.Width>622 || y+rb.Height>414) { Reject("CANDIDATE_WORLD_BOUNDS"); continue; }
                        var b=new Sv5SpaceBounds(x,y,rb.Width,rb.Height);
                        if(!BaselineFootprintFree(b)) { Reject("ROOM_RESERVED_FOOTPRINT"); continue; }
                        string token="SV5_INFILL_V1|"+input.Seed+"|"+parent+"|"+x+"|"+y+"|"+recipe+"|"+(direction<0);
                        // A room's stable rank does not include its possible parent aperture.
                        // Keep ALL exact aperture candidates: the first enumerated one may be blocked.
                        if(!seen.Add(token+"|"+start+"|"+sidePortal)) continue;
                        string hash=Sv5WorldDefinition.Hash(token);
                        if(banned.Contains("SV5_INFILL_ROOM_"+hash.Substring(0,20))) { Reject("BACKTRACKED_DEAD_END_CHOICE"); continue; }
                        int sector=(y+rb.Height/2)/104*4+(x+rb.Width/2)/156;
                        queue[sector].Add(new Candidate { Parent=parent,Host=host,Recipe=recipe,X=x,Y=y,Depth=depth,Direction=direction,
                            Mirror=direction<0,Start=start,ParentRoom=parentRoom,Id="SV5_INFILL_ROOM_"+hash.Substring(0,20),
                            SidePortal=sidePortal,HostAnchor=hostAnchor,
                            Rank=ulong.Parse(hash.Substring(0,16),NumberStyles.HexNumber,CultureInfo.InvariantCulture),
                            Sector=sector });
                    }
                }
            }
            foreach(var host in input.Connections.Where(c=>c.Kind==Sv5SpaceConnectionKind.OptionalBranch && c.Flow=="BIDIRECTIONAL")
                .OrderBy(c=>c.Id,StringComparer.Ordinal))
            foreach(var p in host.Centerline.Distinct().OrderBy(p=>p))
            {
                // Two exposed cardinal contacts at a descending, supported neck. Old clearance is never made SOLID.
                if(!passage.Contains(P(p.X+1,p.Y)) || !passage.Contains(P(p.X-1,p.Y))) continue;
                foreach(int direction in new[]{-1,1}) Enqueue(host.Id,host.Id,P(p.X,p.Y-1),direction,1,null);
            }
            foreach(var host in input.Connections.Where(c=>c.Kind==Sv5SpaceConnectionKind.OptionalBranch && c.Flow=="BIDIRECTIONAL")
                .OrderBy(c=>c.Id,StringComparer.Ordinal))
            {
                var hostCells=new HashSet<Sv5SpecialWorldPoint>(host.Centerline);
                // Pattern-aligned supported ledges face an existing two-cell-high shaft aperture.
                // The immutable shaft remains uncomposed; its clearance receives AIR only, never floor SOLID.
                foreach(var p in hostCells.Where(p=>p.Y%4==3 && hostCells.Contains(P(p.X,p.Y+1))).OrderBy(p=>p))
                foreach(int direction in new[]{-1,1})
                {
                    var start=P(p.X+2*direction,p.Y);
                    if(start.X<0 || start.X>=624 || p.Y<3 || p.Y+4>=416) continue;
                    if(new[]{p,P(p.X,p.Y+1)}.Any(q=>!input.PassageOwners.TryGetValue(q,out var owners) || owners.Count!=1 || !owners.Contains(host.Id))) continue;
                    if(Enumerable.Range(-3,8).Select(d=>P(start.X,start.Y+d)).Any(reserved.Contains)) continue;
                    Enqueue(host.Id,host.Id,start,direction,1,null,true,p);
                }
            }

            while(queue.Any(q=>q.Count>0) && rooms.Count<profile.Target)
            {
                var candidate=queue.Where(q=>q.Count>0).Select(q=>q.Min).OrderBy(c=>sectors[c.Sector]).ThenBy(c=>c.Rank)
                    .ThenBy(c=>c.Y).ThenBy(c=>c.X).ThenBy(c=>c.Recipe,StringComparer.Ordinal).First();
                queue[candidate.Sector].Remove(candidate);
                if(candidate.ParentRoom!=null && usedDoors.Contains(candidate.Parent)) { Reject("PARENT_DOOR_USED"); continue; }
                var b=Sv5InfillPatterns.Bounds(candidate.Recipe,candidate.X,candidate.Y);
                if(Footprint(b).Any(all.ContainsKey)) { Reject("ROOM_OCCUPIED"); continue; }
                var entry=P(candidate.Mirror ? b.MaxXExclusive-1 : b.X,b.Y+3);
                var deep=P(candidate.Mirror ? b.X+3 : b.MaxXExclusive-4,b.Y+(candidate.Recipe=="LANDING" ? 5 : 3));
                var roomCells=Sv5InfillPatterns.Recipe(candidate.Recipe,P(b.X,b.Y),candidate.Mirror,candidate.Id,candidate.Parent,candidate.Host);
                var proposed=roomCells.ToDictionary(c=>c.World,c=>c);
                var parentEdits=new Dictionary<Sv5SpecialWorldPoint,Sv5InfillCell>();
                if(candidate.ParentRoom!=null)
                    foreach(var c in Sv5InfillPatterns.Recipe(candidate.ParentRoom.Recipe,P(candidate.ParentRoom.Bounds.X,candidate.ParentRoom.Bounds.Y),
                        candidate.ParentRoom.Mirror,candidate.ParentRoom.Id,candidate.ParentRoom.Parent,candidate.Host,true)) parentEdits[c.World]=c;
                // Search a bounded supported stair profile, rather than imposing a straight slope.
                // X is monotone; each next support differs by at most one tile. Every column is
                // checked against the actual old reservations and accepted owned cells BEFORE use.
                // The final whole-cell validator and bidirectional AABB screen remain mandatory.
                int dx=candidate.Direction, distance=Math.Abs(entry.X-candidate.Start.X);
                // The authoritative connector includes both endpoints. A root adds the old-host
                // access prefix once; a child is exactly the supported path from its parent boundary.
                int connectionPrefix=candidate.ParentRoom==null ? (candidate.SidePortal ? 2 : 1) : 0;
                bool ColumnFree(Sv5SpecialWorldPoint foot)
                {
                    if(foot.X<0 || foot.X>=624 || foot.Y<3 || foot.Y+4>=416) return false;
                    for(int offset=-3;offset<=4;offset++)
                    {
                        var p=P(foot.X,foot.Y+offset);
                        var value=offset>=0 && offset<=1 ? Sv5InfillCellValue.Air : Sv5InfillCellValue.Solid;
                        bool neck=candidate.ParentRoom==null && Math.Abs(p.X-candidate.Start.X)<=2;
                        if(candidate.ParentRoom==null && Math.Abs(p.X-candidate.Start.X)<=5 && offset>=2 &&
                            (reserved.Contains(p) || passage.Contains(P(p.X,p.Y+1)))) continue;
                        if(parentEdits.TryGetValue(p,out var pc))
                        { if(value==Sv5InfillCellValue.Air && pc.Value!=value) return false; continue; }
                        if(all.ContainsKey(p) || b.Contains(p) || input.PlaceFootprint.Contains(p)) return false;
                        if(value==Sv5InfillCellValue.Solid && reserved.Contains(p)) return false;
                        if(value==Sv5InfillCellValue.Air && !neck &&
                            (passage.Contains(p) || Neighbors(p).Any(passage.Contains))) return false;
                    }
                    return true;
                }
                var paths=new Dictionary<int,List<Sv5SpecialWorldPoint>>();
                int CardinalCount(IReadOnlyList<Sv5SpecialWorldPoint> feet) => feet.Count+
                    Enumerable.Range(1,feet.Count-1).Sum(i=>Math.Abs(feet[i].Y-feet[i-1].Y));
                if(ColumnFree(candidate.Start)) paths[candidate.Start.Y]=new List<Sv5SpecialWorldPoint>{candidate.Start};
                for(int step=1;step<distance && paths.Count>0;step++)
                {
                    var next=new Dictionary<int,List<Sv5SpecialWorldPoint>>();
                    foreach(var previous in paths.OrderBy(k=>Math.Abs(k.Key-entry.Y)).ThenBy(k=>k.Key))
                    foreach(int rise in new[]{0,1,-1})
                    {
                        int height=previous.Key+rise;
                        if(Math.Abs(height-entry.Y)>distance-step) continue;
                        var point=P(candidate.Start.X+dx*step,height);
                        if(!ColumnFree(point)) continue;
                        var trial=previous.Value.Concat(new[]{point}).ToList();
                        int cost=CardinalCount(trial);
                        if(cost+distance-step+Math.Abs(height-entry.Y)+connectionPrefix>profile.MaximumLink) continue;
                        if(!next.TryGetValue(height,out var oldPath) || cost<CardinalCount(oldPath)) next[height]=trial;
                    }
                    paths=next;
                }
                var selected=paths.Where(p=>Math.Abs(p.Key-entry.Y)<=1 &&
                    CardinalCount(p.Value)+1+Math.Abs(p.Key-entry.Y)+connectionPrefix<=profile.MaximumLink)
                    .OrderBy(p=>CardinalCount(p.Value)+Math.Abs(p.Key-entry.Y)).ThenBy(p=>p.Key).FirstOrDefault();
                if(selected.Value==null) { Reject("NO_SUPPORTED_CONNECTOR_PROFILE"); continue; }
                var path=selected.Value.Concat(new[]{entry}).ToList();
                var linkCells=new Dictionary<Sv5SpecialWorldPoint,Sv5InfillCell>(); bool conflict=false;
                if(candidate.SidePortal)
                    foreach(int rise in new[]{0,1})
                    {
                        var p=P(candidate.HostAnchor.X+dx,candidate.HostAnchor.Y+rise);
                        if(all.ContainsKey(p)) { conflict=true; break; }
                        linkCells[p]=new Sv5InfillCell(p,Sv5InfillCellValue.Air,candidate.Id+"_LINK","CONNECTOR",candidate.Parent,candidate.Host);
                    }
                foreach(var foot in path.Take(path.Count-1))
                for(int offset=-3;offset<=4;offset++)
                {
                    var p=P(foot.X,foot.Y+offset);
                    var value=offset>=0 && offset<=1 ? Sv5InfillCellValue.Air : Sv5InfillCellValue.Solid;
                    // The two head cells of the declared parent neck meet existing passage; the roof is open only there.
                    if(candidate.ParentRoom==null && Math.Abs(p.X-candidate.Start.X)<=5 && offset>=2 &&
                        (reserved.Contains(p) || passage.Contains(P(p.X,p.Y+1)))) continue;
                    if(parentEdits.TryGetValue(p,out var pc)) { if(pc.Value!=value && value==Sv5InfillCellValue.Air) conflict=true; continue; }
                    if(all.ContainsKey(p) || b.Contains(p)) { conflict=true; continue; }
                    if(linkCells.TryGetValue(p,out var previous) && previous.Value!=value) { conflict=true; continue; }
                    linkCells[p]=new Sv5InfillCell(p,value,candidate.Id+"_LINK","CONNECTOR",candidate.Parent,candidate.Host,
                        candidate.ParentRoom==null && passage.Contains(p) && Math.Abs(p.X-candidate.Start.X)<=2 && p.Y==candidate.Start.Y+1);
                }
                if(conflict) { Reject("LINK_OWNERSHIP"); continue; }
                var centerline=CardinalCenterline(path);
                var hostAccess=candidate.ParentRoom!=null ? Array.Empty<Sv5SpecialWorldPoint>() : candidate.SidePortal ?
                    new[]{candidate.HostAnchor,P(candidate.HostAnchor.X+dx,candidate.HostAnchor.Y),candidate.Start} :
                    new[]{P(candidate.Start.X,candidate.Start.Y+1),candidate.Start};
                var connectionErrors=FindConnectionLengthErrors(centerline,hostAccess,profile.MaximumLink,candidate.ParentRoom==null);
                if(connectionErrors.Count>0)
                { Reject("CONNECTION_CENTERLINE_LIMIT"); continue; }
                foreach(var c in linkCells.Values) proposed.Add(c.World,c);
                var faces=new List<Sv5SpaceBoundaryFace>();
                if(candidate.SidePortal)
                {
                    // This root owns exactly the declared two-cell shaft aperture, not every
                    // nearby contact that a candidate happens to create.
                    foreach(int rise in new[]{0,1}) faces.Add(new Sv5SpaceBoundaryFace(
                        P(candidate.HostAnchor.X,candidate.HostAnchor.Y+rise),
                        P(candidate.HostAnchor.X+dx,candidate.HostAnchor.Y+rise)));
                }
                else if(candidate.ParentRoom==null)
                {
                    foreach(var c in proposed.Values.Where(c=>c.Value==Sv5InfillCellValue.Air &&
                        Math.Abs(c.World.X-candidate.Start.X)<=2))
                    foreach(var n in Neighbors(c.World).Where(passage.Contains)) faces.Add(new Sv5SpaceBoundaryFace(c.World,n));
                }
                else
                    foreach(var c in proposed.Values.Where(c=>c.Value==Sv5InfillCellValue.Air))
                    foreach(var n in Neighbors(c.World))
                        if(parentEdits.TryGetValue(n,out var parentCell) && parentCell.Value==Sv5InfillCellValue.Air)
                            faces.Add(new Sv5SpaceBoundaryFace(c.World,n));
                var errors=ValidateCandidate(input,proposed.Values,faces,candidate.Host);
                if(errors.Count>0) { Reject(errors[0].Split('|')[0]); continue; }
                var merged=all.ToDictionary(k=>k.Key,k=>k.Value.Value);
                foreach(var c in proposed.Values) merged[c.World]=c.Value;
                foreach(var c in parentEdits.Values) merged[c.World]=c.Value;
                var proof=Sv5InfillPatterns.Screen(merged,candidate.ParentRoom==null ? candidate.Start : candidate.ParentRoom.Entry,deep);
                if(!proof.Success) { Reject("STATIC_SCREEN"); continue; }
                // Screen only windows touched by this proposal, against the complete known-solid union.
                // Touching two individually thin shells must not silently create a six-wide solid block.
                bool Solid(Sv5SpecialWorldPoint p) => merged.TryGetValue(p,out var v) ? v==Sv5InfillCellValue.Solid :
                    baseCells.TryGetValue(p,out var old) && old==Sv5InfillCellValue.Solid;
                var windowOrigins=new HashSet<Sv5SpecialWorldPoint>();
                foreach(var c in proposed.Values.Where(c=>c.Value==Sv5InfillCellValue.Solid))
                for(int wy=Math.Max(0,c.World.Y-5);wy<=Math.Min(410,c.World.Y);wy++)
                for(int wx=Math.Max(0,c.World.X-5);wx<=Math.Min(618,c.World.X);wx++) windowOrigins.Add(P(wx,wy));
                if(windowOrigins.Any(o=>Enumerable.Range(0,6).All(wy=>Enumerable.Range(0,6).All(wx=>Solid(P(o.X+wx,o.Y+wy))))))
                { Reject("JOIN_SOLID_6X6"); continue; }
                // No contact to another branch, including branches with the same host label.
                if(proposed.Values.Where(c=>c.Value==Sv5InfillCellValue.Air).Any(c=>Neighbors(c.World).Any(n=>
                    all.TryGetValue(n,out var neighbor) && neighbor.Value==Sv5InfillCellValue.Air && neighbor.Owner!=candidate.Parent)))
                { Reject("FOREIGN_BRANCH_FACE"); continue; }
                foreach(var c in proposed.Values) all.Add(c.World,c);
                foreach(var c in parentEdits.Values) all[c.World]=c;
                var room=new Sv5InfillRoom(candidate.Id,candidate.Recipe,b,candidate.Mirror,candidate.Parent,candidate.Host,candidate.Depth,
                    roomCells,entry,deep); rooms.Add(room);
                sectors[room.Sector]++;
                if(candidate.ParentRoom!=null) usedDoors.Add(candidate.Parent);
                links.Add(new Sv5InfillLink(room.Id,room.Parent,room.Host,centerline,faces.Distinct(),linkCells.Values,proof,hostAccess));
                int childY=b.Y+(candidate.Recipe=="LANDING" ? 5 : 3);
                Enqueue(room.Id,room.Host,P(candidate.Mirror ? b.X : b.MaxXExclusive-1,childY),candidate.Direction,candidate.Depth+1,room);
            }
            // Rebuild parent cell views after exact, used child openings were adopted.
            rooms=rooms.Select(r=>new Sv5InfillRoom(r.Id,r.Recipe,r.Bounds,r.Mirror,r.Parent,r.Host,r.Depth,
                all.Values.Where(c=>c.Owner==r.Id),r.Entry,r.Deep)).ToList();
            int count=rooms.Count, area=all.Values.Count(c=>!c.Shared && !reserved.Contains(c.World));
            if(count<profile.Minimum) diagnostics.Add("MINIMUM_ROOMS|"+count+"/"+profile.Minimum);
            if(area<profile.MinimumTiles) diagnostics.Add("MINIMUM_OWNED_TILES|"+area+"/"+profile.MinimumTiles);
            int populated=Enumerable.Range(0,16).Count(s=>rooms.Count(r=>r.Sector==s)>=profile.RoomsPerSector);
            if(populated<profile.MinimumSectors) diagnostics.Add("MINIMUM_POPULATED_SECTORS|"+populated+"/"+profile.MinimumSectors);
            var known=baseCells.ToDictionary(k=>k.Key,k=>k.Value);
            foreach(var c in all.Values) known[c.World]=c.Value;
            diagnostics.AddRange(Sv5InfillPatterns.SolidWindows(known).Select(p=>"SOLID_6X6|"+p));
            return new Sv5InfillPlan(input.BaselineDigest,profile,rooms,links,all.Values,diagnostics,rejected,
                count>=profile.Target ? "TARGET_REACHED" : "CANDIDATES_EXHAUSTED",reserved);
        }
    }
}
