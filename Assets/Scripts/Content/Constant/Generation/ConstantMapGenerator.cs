using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// 런타임 맵 생성기 — 씬 로드마다 가변 크기 룸(TinyKeep식)을 새로 짜집기한다.
/// 메인 가로형 30~33x18~20 / 메인 세로형 23~25x45~48 / 복도 15~20x10~15, 10~15x25~30.
/// RoomVariantLibrary 에 같은 크기의 수제 방이 베이크되어 있으면 그 방을 스탬핑하고,
/// 없으면 절차 퍼니싱으로 내부를 채운다.
/// </summary>
public class ConstantMapGenerator
{
    private const int CanvasW = 220, CanvasH = 200;
    private const int MainRoomTarget = 9;

    private const int GStatic = 0, GPulse = 1;
    private const int DFake = 2;

    private readonly TileMapData _map;
    private readonly ConstantAssetLibrary _lib;
    private readonly ConstantPlanetProfile _profile;
    private readonly RoomVariantLibrary _variants;
    private System.Random _rng;
    private Transform _gameplay;
    private Transform _deco;
    private readonly HashSet<Vector2Int> _usedSpots = new HashSet<Vector2Int>();

    public Vector2Int SpawnGrid { get; private set; }

    private class RoomBox
    {
        public int x0, y0, x1, y1;
        public bool isMain, isVertical;
        public int depth, doorCount;
        public int soleDoorSide = -1;
        public readonly List<Vector2Int> spots = new List<Vector2Int>();
        public readonly List<Vector2Int> enemySpots = new List<Vector2Int>();

        public int W => x1 - x0 + 1;
        public int H => y1 - y0 + 1;
        public int CX => (x0 + x1) / 2;
        public Vector3 Center => new Vector3((x0 + x1 + 1) * 0.5f, (y0 + y1 + 1) * 0.5f, 0f);

        public bool Overlaps(RoomBox o, int margin) =>
            x0 - margin <= o.x1 && x1 + margin >= o.x0 &&
            y0 - margin <= o.y1 && y1 + margin >= o.y0;
    }

    public ConstantMapGenerator(TileMapData map, ConstantAssetLibrary lib, ConstantPlanetProfile profile)
    {
        _map = map;
        _lib = lib;
        _profile = profile;
        _variants = RoomVariantLibrary.Load(); // 없으면 null — 절차 퍼니싱 폴백
    }

    // ═════════════════════════════════════════════════════════════
    public void Generate(int seed)
    {
        _rng = new System.Random(seed);
        _gameplay = new GameObject("@Gameplay").transform;
        _deco = new GameObject("@Deco").transform;

        // 1) 방 배치 (트리)
        var rooms = new List<RoomBox>();
        var mains = new List<RoomBox>();
        var corridors = new List<RoomBox>();
        var carves = new List<Vector2Int>();

        var start = new RoomBox { x0 = CanvasW / 2 - 16, y0 = CanvasH - 60, isMain = true };
        var sz = MainSize(false);
        start.x1 = start.x0 + sz.x - 1; start.y1 = start.y0 + sz.y - 1;
        rooms.Add(start); mains.Add(start);

        int attempts = 0;
        while (mains.Count < MainRoomTarget && attempts++ < 800)
        {
            var a = mains[_rng.Next(mains.Count)];
            int side = WeightedSide();
            bool horizontal = side <= 1;
            bool bVertical = _rng.NextDouble() < 0.3;

            if (!TryPlace(a, side, CorridorSize(horizontal), MainSize(bVertical), rooms,
                out var corr, out var b, carves))
                continue;

            corr.depth = a.depth; b.depth = a.depth + 1;
            b.isMain = true; b.isVertical = bVertical;
            b.doorCount = 1; b.soleDoorSide = side switch { 0 => 1, 1 => 0, 2 => 3, _ => 2 };
            a.doorCount++;

            rooms.Add(corr); corridors.Add(corr);
            rooms.Add(b); mains.Add(b);
        }

        // 2) 셸 + 내부
        int lavaBudget = 18;
        foreach (var r in rooms)
        {
            PaintShell(r);
            if (!TryStampVariant(r))
                Furnish(r, ref lavaBudget);
        }

        // 3) 문
        foreach (var p in carves)
            _map.RemoveTile(p);

        // 4) 카메라 룸
        foreach (var r in rooms)
        {
            _map.AddCameraRoom(new Vector2Int(r.x0, r.y0), new Vector2Int(r.x1, r.y1));
            var room = _map.CameraRooms[_map.CameraRooms.Count - 1];
            room.roomName = $"Room {_map.CameraRooms.Count}";
            room.hasSpawn = true;
            room.spawnGridPos = new Vector2Int(r.x0 + 4, r.y0 + 2);
        }

        // 5) 콘텐츠
        var exit = PickExitRoom(mains, start);
        PlaceContent(rooms, mains, corridors, start, exit);

        // 6) 배경 드레싱 — 팩 데모 씬 문법 4겹:
        //    원거리 배경(시차 0.04) → 중거리 실루엣(시차 0.25) → 방 뒷벽 패널 → 바닥/천장 소품
        var assets = _lib != null ? _lib.For(_profile.planet) : null;
        if (assets != null)
        {
            SnapshotTiles();
            if (assets.bgSprites != null && assets.bgSprites.Length > 0)
                PlaceFarBackdrop(assets.bgSprites[0], rooms);
            PlaceFarAccent(assets, rooms);      // 행성/달 같은 원경 랜드마크 (bgSprites[1])
            PlaceMidSilhouettes(assets, rooms);
            PlaceVeil(assets, rooms);           // 배경 명도를 누르는 검은 베일
            foreach (var r in rooms)
                DressRoom(r, assets);
            if (assets.particles != null)
                for (int i = 0; i < assets.particles.Length && i < mains.Count; i++)
                    if (assets.particles[i] != null)
                        Object.Instantiate(assets.particles[i], mains[(i * 3 + 1) % mains.Count].Center, Quaternion.identity, _deco);
        }

        // 7) 실체화
        _map.RebuildAll();
        StyleGeneratedHazards();
    }

    // ═════════════════════════════════════════════════════════════
    // 배치 (v7 로직)
    // ═════════════════════════════════════════════════════════════
    private Vector2Int MainSize(bool vertical) => vertical
        ? new Vector2Int(_rng.Next(23, 26), _rng.Next(45, 49))
        : new Vector2Int(_rng.Next(30, 34), _rng.Next(18, 21));

    private Vector2Int CorridorSize(bool horizontal) => horizontal
        ? new Vector2Int(_rng.Next(15, 21), _rng.Next(10, 16))
        : new Vector2Int(_rng.Next(10, 16), _rng.Next(25, 31));

    private int WeightedSide()
    {
        int roll = _rng.Next(100);
        if (roll < 30) return 0;
        if (roll < 60) return 1;
        if (roll < 78) return 3;
        return 2;
    }

    private bool TryPlace(RoomBox a, int side, Vector2Int corrSz, Vector2Int mainSz,
        List<RoomBox> placed, out RoomBox corr, out RoomBox b, List<Vector2Int> carves)
    {
        corr = new RoomBox(); b = new RoomBox();

        if (side <= 1)
        {
            corr.y0 = a.y0; corr.y1 = corr.y0 + corrSz.y - 1;
            b.y0 = a.y0; b.y1 = b.y0 + mainSz.y - 1;
            if (side == 1)
            {
                corr.x0 = a.x1 + 1; corr.x1 = corr.x0 + corrSz.x - 1;
                b.x0 = corr.x1 + 1; b.x1 = b.x0 + mainSz.x - 1;
            }
            else
            {
                corr.x1 = a.x0 - 1; corr.x0 = corr.x1 - corrSz.x + 1;
                b.x1 = corr.x0 - 1; b.x0 = b.x1 - mainSz.x + 1;
            }
        }
        else
        {
            int cxMin = a.x0 + 3, cxMax = a.x1 - 3 - corrSz.x + 1;
            if (cxMax < cxMin) return false;
            corr.x0 = _rng.Next(cxMin, cxMax + 1); corr.x1 = corr.x0 + corrSz.x - 1;

            int bxMin = Mathf.Max(2, corr.x1 + 3 - mainSz.x + 1);
            int bxMax = corr.x0 - 3;
            if (bxMax < bxMin) return false;
            b.x0 = _rng.Next(bxMin, bxMax + 1); b.x1 = b.x0 + mainSz.x - 1;
            if (b.x0 > corr.x0 - 2 || b.x1 < corr.x1 + 2) return false;

            if (side == 3)
            {
                corr.y1 = a.y0 - 1; corr.y0 = corr.y1 - corrSz.y + 1;
                b.y1 = corr.y0 - 1; b.y0 = b.y1 - mainSz.y + 1;
            }
            else
            {
                corr.y0 = a.y1 + 1; corr.y1 = corr.y0 + corrSz.y - 1;
                b.y0 = corr.y1 + 1; b.y1 = b.y0 + mainSz.y - 1;
            }
        }

        foreach (var r in new[] { corr, b })
        {
            if (r.x0 < 2 || r.y0 < 2 || r.x1 > CanvasW - 3 || r.y1 > CanvasH - 3) return false;
            foreach (var p in placed)
                if (r.Overlaps(p, 0)) return false;
        }
        corr.isVertical = side >= 2;

        if (side <= 1)
        {
            int fy = a.y0;
            RoomBox left = side == 1 ? a : b;
            RoomBox right = side == 1 ? b : a;
            foreach (int wx in new[] { left.x1, left.x1 + 1, right.x0 - 1, right.x0 })
                for (int dy = 2; dy <= 4; dy++)
                    carves.Add(new Vector2Int(wx, fy + dy));
        }
        else
        {
            RoomBox top = side == 2 ? b : a;
            RoomBox bottom = side == 2 ? a : b;
            int cx = corr.CX;
            for (int dx = -1; dx <= 1; dx++)
            {
                carves.Add(new Vector2Int(cx + dx, top.y0));
                carves.Add(new Vector2Int(cx + dx, top.y0 + 1));
                carves.Add(new Vector2Int(cx + dx, corr.y1));
                carves.Add(new Vector2Int(cx + dx, corr.y0));
                carves.Add(new Vector2Int(cx + dx, corr.y0 + 1));
                carves.Add(new Vector2Int(cx + dx, bottom.y1));
            }
        }
        return true;
    }

    private RoomBox PickExitRoom(List<RoomBox> mains, RoomBox start)
    {
        var sideLeaves = mains.Where(m => m != start && m.doorCount == 1 && m.soleDoorSide <= 1).ToList();
        if (sideLeaves.Count > 0) return sideLeaves.OrderByDescending(m => m.depth).First();
        var leaves = mains.Where(m => m != start && m.doorCount == 1).ToList();
        var pool = leaves.Count > 0 ? leaves : mains.Where(m => m != start).ToList();
        if (pool.Count == 0) return start;
        return pool.OrderByDescending(m => m.depth).First();
    }

    // ═════════════════════════════════════════════════════════════
    // 셸/퍼니싱
    // ═════════════════════════════════════════════════════════════
    private void Tile(int x, int y, TileType t, int variant = 0)
    {
        var p = new Vector2Int(x, y);
        _map.AddOrReplace(p, t, Vector2.one);
        if (variant != 0)
        {
            var td = _map.GetTile(p);
            if (td != null) td.variant = variant;
        }
    }

    private void Span(int x0, int x1, int y, TileType t = TileType.Ground)
    { for (int x = x0; x <= x1; x++) Tile(x, y, t); }

    private void Col(int x, int y0, int y1, TileType t = TileType.Ground)
    { for (int y = y0; y <= y1; y++) Tile(x, y, t); }

    private void PaintShell(RoomBox r)
    {
        Span(r.x0, r.x1, r.y0);
        Span(r.x0, r.x1, r.y0 + 1);
        Span(r.x0, r.x1, r.y1);
        Col(r.x0, r.y0, r.y1);
        Col(r.x1, r.y0, r.y1);
    }

    /// <summary>룸 라이브러리에서 같은 유형·크기의 수제 방을 찾아 스탬핑. 성공 시 true.</summary>
    private bool TryStampVariant(RoomBox r)
    {
        if (_variants == null) return false;

        ConstantRoomType type = r.isMain
            ? (r.isVertical ? ConstantRoomType.MainV : ConstantRoomType.MainH)
            : (r.isVertical ? ConstantRoomType.CorrV : ConstantRoomType.CorrH);

        var matches = _variants.variants.FindAll(v => v.type == type && v.width == r.W && v.height == r.H);
        if (matches.Count == 0) return false;

        var variant = matches[_rng.Next(matches.Count)];
        foreach (var t in variant.tiles)
            Tile(r.x0 + t.x, r.y0 + t.y, t.type, t.variant);
        foreach (var s in variant.spots)
        {
            var pos = new Vector2Int(r.x0 + s.x, r.y0 + s.y);
            if (s.isEnemy) r.enemySpots.Add(pos);
            else r.spots.Add(pos);
        }
        return true;
    }

    private void Furnish(RoomBox r, ref int lavaBudget)
    {
        if (!r.isMain)
        {
            if (!r.isVertical)
            {
                r.spots.Add(new Vector2Int(r.CX - 3, r.y0 + 2));
                r.spots.Add(new Vector2Int(r.CX, r.y0 + 2));
                r.spots.Add(new Vector2Int(r.CX + 3, r.y0 + 2));
            }
            else
            {
                int cx = r.CX;
                Col(cx, r.y0 + 2, r.y1 - 1, TileType.Ladder);
                bool left = _rng.NextDouble() < 0.5;
                for (int y = r.y0 + 6; y <= r.y1 - 5; y += 6)
                {
                    if (left) Span(r.x0 + 2, cx - 1, y);
                    else Span(cx + 1, r.x1 - 2, y);
                    left = !left;
                }
                r.spots.Add(new Vector2Int(cx - 2, r.y0 + 2));
            }
            return;
        }

        if (!r.isVertical)
        {
            int[] layers = { r.y0 + 6, r.y0 + 12 };
            foreach (int ly in layers)
            {
                if (ly > r.y1 - 4) continue;
                int gap1 = _rng.Next(r.x0 + 4, r.x1 - 10);
                int gap2 = _rng.Next(gap1 + 5, Mathf.Min(gap1 + 14, r.x1 - 5));
                for (int x = r.x0 + 2; x <= r.x1 - 2; x++)
                {
                    if (x >= gap1 && x < gap1 + 4) continue;
                    if (x >= gap2 && x < gap2 + 4) continue;
                    Tile(x, ly, TileType.Ground);
                }
                r.spots.Add(new Vector2Int(_rng.Next(r.x0 + 3, r.x1 - 3), ly + 1));
            }

            if (_rng.NextDouble() < 0.6)
                Col(_rng.Next(r.x0 + 3, r.x1 - 3), r.y0 + 2, layers[1] < r.y1 - 3 ? layers[1] : layers[0], TileType.Ladder);

            int hy = layers[_rng.Next(layers.Length)];
            int hx = _rng.Next(r.x0 + 4, r.x1 - 7);
            for (int i = 0; i < 3; i++) PlaceHazard(hx + i, hy + 1, ref lavaBudget);

            r.spots.Add(new Vector2Int(r.x0 + 4, r.y0 + 2));
            r.spots.Add(new Vector2Int(r.CX, r.y0 + 2));
            r.spots.Add(new Vector2Int(r.x1 - 4, r.y0 + 2));
            r.enemySpots.Add(new Vector2Int(r.CX, r.y0 + 2));
        }
        else
        {
            bool gapLeft = _rng.NextDouble() < 0.5;
            int hz1 = _rng.Next(1, 4), hz2 = _rng.Next(4, 7);
            int li = 0;
            for (int ly = r.y0 + 6; ly <= r.y1 - 4; ly += 6, li++)
            {
                for (int x = r.x0 + 2; x <= r.x1 - 2; x++)
                {
                    bool inGap = gapLeft ? (x <= r.x0 + 6) : (x >= r.x1 - 6);
                    if (!inGap) Tile(x, ly, TileType.Ground);
                }
                if (li == hz1 || li == hz2)
                {
                    int hx = gapLeft ? r.x1 - 7 : r.x0 + 3;
                    for (int i = 0; i < 3; i++) PlaceHazard(hx + i, ly + 1, ref lavaBudget);
                }
                r.spots.Add(new Vector2Int(gapLeft ? r.x1 - 4 : r.x0 + 4, ly + 1));
                if (li == 2) r.enemySpots.Add(new Vector2Int(r.CX, ly + 1));
                gapLeft = !gapLeft;
            }
            if (_rng.NextDouble() < 0.7)
                Col(r.CX, r.y0 + 2, r.y1 - 2, TileType.Ladder);
            r.spots.Add(new Vector2Int(r.CX, r.y0 + 2));
        }
    }

    private void PlaceHazard(int x, int y, ref int lavaBudget)
    {
        switch (_profile.planet)
        {
            case ConstantPlanet.Lavernis:
                if (lavaBudget > 0) { Tile(x, y, TileType.Lava); lavaBudget--; }
                else Tile(x, y, TileType.Spike);
                break;
            case ConstantPlanet.Eidron: Tile(x, y, TileType.Gas, GPulse); break;
            case ConstantPlanet.Sylmare: Tile(x, y - 1, TileType.Disguised, DFake); break;
            default: Tile(x, y, TileType.Spike); break;
        }
    }

    // ═════════════════════════════════════════════════════════════
    // 콘텐츠 (v7 로직 — 런타임 팩토리)
    // ═════════════════════════════════════════════════════════════
    private void PlaceContent(List<RoomBox> rooms, List<RoomBox> mains, List<RoomBox> corridors,
        RoomBox start, RoomBox exit)
    {
        SpawnGrid = new Vector2Int(start.x0 + 4, start.y0 + 2);
        MakeObserver(_profile.observerNode, _profile.observerName, start.x0 + 10, start.y0 + 2, _profile.observerColor);
        Label(_profile.moodLabel, World(start.CX, start.y0 + 5), 2.6f, new Color(0.95f, 0.95f, 1f, 0.8f));
        Label("밸브 3개 = 출구 · R 로프 · F 폭탄 · I 가방 · V 도감", World(start.CX, start.y0 + 4), 2.0f, new Color(1f, 1f, 1f, 0.55f));
        start.spots.Clear(); start.enemySpots.Clear();

        bool doorOnLeft = exit.soleDoorSide == 0;
        int padX = doorOnLeft ? exit.x1 - 4 : exit.x0 + 4;
        int gateX = doorOnLeft ? padX - 5 : padX + 5;
        var assets = _lib != null ? _lib.For(_profile.planet) : null;
        MakePad(padX, exit.y0 + 2, assets?.padPrefab, _profile.padLabel);
        GameObject exitGate = MakeGate(gateX, exit.y0 + 2, exit.H - 3, new Color(0.5f, 0.55f, 0.6f));
        Label("출구 게이트 — 밸브 3개", World(gateX, exit.y0 + 5), 2.0f, new Color(1f, 1f, 1f, 0.6f));
        exit.spots.RemoveAll(s => Mathf.Abs(s.x - padX) < 8);
        exit.enemySpots.Clear();

        var questGo = new GameObject("@Quest_ExitValves");
        questGo.transform.SetParent(_gameplay, false);
        var valveQuest = questGo.AddComponent<ValveQuest>();
        GameObject relic = null;
        if (_profile.hasCore)
        {
            relic = MakeItem("coolantCore", padX + (doorOnLeft ? -2 : 2), exit.y0 + 2);
            relic.SetActive(false);
        }
        valveQuest.Init(3, exitGate, relic);

        // 밸브 4
        int valves = 0;
        foreach (var corr in corridors.OrderBy(_ => _rng.Next()).Take(2))
        {
            var s = TakeSpot(corr); if (s == null) continue;
            MakeValve(valveQuest, s.Value.x, s.Value.y);
            valves++;
        }
        foreach (var m in mains.Where(m => m != start && m != exit).OrderBy(_ => _rng.Next()))
        {
            if (valves >= 4) break;
            var s = TakeSpot(m); if (s == null) continue;
            MakeValve(valveQuest, s.Value.x, s.Value.y);
            valves++;
        }

        // 퀘스트 (보호핵/사당)
        var candidates = mains.Where(m => m != start && m != exit).OrderBy(_ => _rng.Next()).ToList();
        if ((_profile.hasCore || _profile.hasShrine) && candidates.Count > 0)
        {
            var qr = candidates[0];
            var s = TakeSpot(qr) ?? new Vector2Int(qr.CX, qr.y0 + 2);
            if (_profile.hasCore)
            {
                MakeCore(valveQuest, s.x, s.y);
                Label("보호핵 — 뜯으면 출구가 강제로 열린다. 마을은 식는다.", World(s.x, s.y + 3), 2.0f, new Color(1f, 0.6f, 0.5f, 0.8f));
            }
            else MakeShrine(s.x, s.y);
        }

        // 태그 금고
        if (candidates.Count > 1)
        {
            var vr = candidates[1];
            if (!vr.isVertical)
            {
                int vy = vr.y0 + 9;
                int vx = vr.x1 - 10;
                Span(vx, vr.x1 - 2, vy);
                GameObject door = MakeGate(vx + 2, vy + 1, 3, new Color(0.6f, 0.5f, 0.65f));
                MakeNode(_profile.richNodeName, vx + 4, vy + 1, 30f);
                MakeNode(_profile.richNodeName, vx + 6, vy + 1, 30f);
                MakeTagGate(vx + 1, vy + 1, _profile.vaultDoorName, _profile.vaultTag, 2, door);
            }
            else
            {
                var s = TakeSpot(vr) ?? new Vector2Int(vr.CX, vr.y0 + 2);
                GameObject door = MakeGate(s.x + 2, s.y, 3, new Color(0.6f, 0.5f, 0.65f));
                MakeNode(_profile.richNodeName, s.x + 4, s.y, 30f);
                MakeTagGate(s.x, s.y, _profile.vaultDoorName, _profile.vaultTag, 2, door);
            }
        }

        // 규약 스위치 (에이드론)
        if (_profile.protocolSwitches && candidates.Count > 2)
        {
            var pGo = new GameObject("@Quest_Protocol");
            pGo.transform.SetParent(_gameplay, false);
            var protocol = pGo.AddComponent<ProtocolQuest>();
            var swRooms = candidates.Skip(2).Take(3).OrderByDescending(m => m.y0).ToList();
            Vector2Int last = Vector2Int.zero;
            for (int i = 0; i < swRooms.Count; i++)
            {
                var s = TakeSpot(swRooms[i]) ?? new Vector2Int(swRooms[i].CX, swRooms[i].y0 + 2);
                MakeSwitch(protocol, i + 1, s.x, s.y);
                last = s;
            }
            Label("규약: 절차는 순서대로 (1, 2, 3)", World(last.x, last.y + 3), 2.0f, new Color(0.6f, 1f, 0.7f, 0.8f));
            GameObject ring = MakeItem("commandRing", last.x + 2, last.y);
            ring.SetActive(false);
            protocol.SetReward(ring);
        }

        // 이벤트 스테이션 (복도)
        var eventCorrs = corridors.Where(cr => cr.spots.Count > 0).OrderBy(_ => _rng.Next()).ToList();
        for (int i = 0; i < eventCorrs.Count && i < 4; i++)
        {
            var cr = eventCorrs[i];
            switch (i)
            {
                case 0:
                    for (int g = 0; g < 3; g++)
                    {
                        var s = TakeSpot(cr); if (s == null) break;
                        MakeShop(s.Value.x, s.Value.y, g);
                    }
                    break;
                case 1: { var s = TakeSpot(cr); if (s != null) MakeStation<SupplyCache>(s.Value, "보급 상자 [X]", new Color(0.7f, 0.55f, 0.35f)); break; }
                case 2: { var s = TakeSpot(cr); if (s != null) MakeStation<Replicator>(s.Value, "복제기 — 무작위 아이템 복제 [X]", new Color(0.55f, 0.85f, 0.8f)); break; }
                case 3: { var s = TakeSpot(cr); if (s != null) MakeStation<LoanTerminal>(s.Value, "여행사 대출 — 지금 +30, 다음부터 +20 [X]", new Color(0.85f, 0.7f, 0.9f)); break; }
            }
        }

        // 아이템 풀
        foreach (string itemId in _profile.itemPool)
        {
            var room = mains[_rng.Next(mains.Count)];
            var s = TakeSpot(room);
            if (s == null) continue;
            MakeItem(itemId, s.Value.x, s.Value.y);
        }

        // 남은 자리: 노드/소모품/몹
        foreach (var room in rooms)
        {
            foreach (var s in room.spots)
            {
                double roll = _rng.NextDouble();
                if (roll < 0.45) MakeNode(_profile.nodeName, s.x, s.y, 20f);
                else if (roll < 0.55) MakeNode(_profile.richNodeName, s.x, s.y, 30f);
                else if (roll < 0.75) MakeConsumable(s.x, s.y, _rng.NextDouble() < 0.5);
            }
            foreach (var e in room.enemySpots)
                if (_rng.NextDouble() < 0.65)
                    MakeEnemy(Mathf.Max(room.x0 + 2, e.x - 4), Mathf.Min(room.x1 - 2, e.x + 4), e.y);
        }
    }

    private Vector2Int? TakeSpot(RoomBox room)
    {
        while (room.spots.Count > 0)
        {
            int i = _rng.Next(room.spots.Count);
            var s = room.spots[i];
            room.spots.RemoveAt(i);
            if (_usedSpots.Add(s)) return s;
        }
        return null;
    }

    // ═════════════════════════════════════════════════════════════
    // 런타임 오브젝트 팩토리
    // ═════════════════════════════════════════════════════════════
    private Vector3 World(int gx, int gy) => _map.GridToWorld(new Vector2Int(gx, gy));
    private Sprite Square() => Resources.Load<Sprite>("Sprites/Square");

    // ═════════════════════════════════════════════════════════════
    // 드레싱 (팩 데모 씬 문법)
    // ═════════════════════════════════════════════════════════════
    // 소팅(모두 Default 레이어): 원거리 -50 < 실루엣 -45 < 뒷벽 패널 -30(복도 -29)
    //   < 타일 사각(5) < 지형 셰이프(6) < 소품 8 < 발광 9 < 플레이어(Objects/100)
    private readonly HashSet<Vector2Int> _solidGround = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> _anyTile = new HashSet<Vector2Int>();

    /// <summary>문 뚫기까지 끝난 최종 타일 상태 스냅샷 — 소품 배치용 표면 탐색에 쓴다.</summary>
    private void SnapshotTiles()
    {
        _solidGround.Clear();
        _anyTile.Clear();
        foreach (var t in _map.Tiles)
        {
            _anyTile.Add(t.gridPos);
            if (t.type == TileType.Ground) _solidGround.Add(t.gridPos);
        }
    }

    private static Rect MapBounds(List<RoomBox> rooms, Vector3 origin)
    {
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        foreach (var r in rooms)
        {
            minX = Mathf.Min(minX, r.x0); minY = Mathf.Min(minY, r.y0);
            maxX = Mathf.Max(maxX, r.x1); maxY = Mathf.Max(maxY, r.y1);
        }
        return new Rect(origin.x + minX, origin.y + minY, maxX - minX + 1, maxY - minY + 1);
    }

    /// <summary>원경 랜드마크(행성/달 등) — bgSprites[1]을 백드롭보다 살짝 앞 시차로 하나만.</summary>
    private void PlaceFarAccent(ConstantAssetLibrary.PlanetAssets assets, List<RoomBox> rooms)
    {
        if (assets.bgSprites == null || assets.bgSprites.Length < 2 || assets.bgSprites[1] == null) return;

        Rect b = MapBounds(rooms, _map.transform.position);
        Vector2 anchor = b.center + new Vector2(4.5f, 2.6f); // 화면 우상단쯤에 걸리게
        var go = SpriteFit(assets.bgSprites[1], new Vector3(anchor.x, anchor.y, 48f), "Default", -49, 5.5f);
        if (go != null)
            go.AddComponent<ConstantParallaxLayer>().Init(anchor, 0.03f);
    }

    /// <summary>검은 베일 — 배경(백드롭/실루엣/패널)과 플레이필드 사이에서 배경 명도를 눌러
    /// 시선을 플레이필드에 고정한다 (데모 씬 공통 문법).</summary>
    private void PlaceVeil(ConstantAssetLibrary.PlanetAssets assets, List<RoomBox> rooms)
    {
        if (assets.veilAlpha <= 0.01f || rooms.Count == 0) return;
        Sprite sq = Square();
        if (sq == null) return;

        // 여백을 크게 — 카메라가 맵 가장자리에서 밖을 볼 때 베일 경계선이 드러나지 않게
        Rect b = MapBounds(rooms, _map.transform.position);
        var go = SpriteFit(sq, new Vector3(b.center.x, b.center.y, 45f), "Default", -20, b.height + 160f,
            new Color(0f, 0f, 0f, assets.veilAlpha));
        if (go != null)
        {
            var s = go.transform.localScale;
            float needW = b.width + 160f;
            go.transform.localScale = new Vector3(needW / Mathf.Max(0.001f, sq.bounds.size.x), s.y, 1f);
        }
    }

    /// <summary>중거리 실루엣 밴드 — 맵 중심 앵커, 시차 (0.25, 0.1) 로 원거리 배경 위를 흐른다.</summary>
    private void PlaceMidSilhouettes(ConstantAssetLibrary.PlanetAssets assets, List<RoomBox> rooms)
    {
        if (assets.bgMidSprites == null || assets.bgMidSprites.Length == 0 || rooms.Count == 0) return;

        Rect bounds = MapBounds(rooms, _map.transform.position);
        Vector2 mapCenter = bounds.center;

        const float ratioX = 0.25f, ratioY = 0.10f, bandH = 20f;
        float needW = 16f + bounds.width * ratioX + 10f;

        var band = new GameObject("Deco_MidSilhouettes");
        band.transform.SetParent(_deco, false);
        band.transform.position = new Vector3(mapCenter.x, mapCenter.y, 40f);
        band.AddComponent<ConstantParallaxLayer>().Init(mapCenter, ratioX, ratioY);

        float x = -needW * 0.5f;
        int idx = 0;
        while (x < needW * 0.5f)
        {
            Sprite s = assets.bgMidSprites[idx++ % assets.bgMidSprites.Length];
            if (s == null || s.bounds.size.y < 0.001f) break;
            float scale = bandH / s.bounds.size.y;
            float w = s.bounds.size.x * scale;

            var go = new GameObject($"Mid_{s.name}_{idx}");
            go.transform.SetParent(band.transform, false);
            go.transform.localPosition = new Vector3(x + w * 0.5f, (float)(_rng.NextDouble() * 4.0 - 2.0), 0f);
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = -45;
            sr.color = assets.midTint;
            x += w * 0.82f; // 살짝 겹쳐 이음새를 숨긴다
        }
    }

    // ── 표면 판정 헬퍼 ──
    private bool FloorSurface(int x, int y) =>
        _solidGround.Contains(new Vector2Int(x, y))
        && !_anyTile.Contains(new Vector2Int(x, y + 1))
        && !_anyTile.Contains(new Vector2Int(x, y + 2));

    private bool CeilSurface(int x, int y) =>
        _solidGround.Contains(new Vector2Int(x, y))
        && !_anyTile.Contains(new Vector2Int(x, y - 1))
        && !_anyTile.Contains(new Vector2Int(x, y - 2));

    /// <summary>바닥 셀 y 위로 이어지는 빈 칸 수 (limit까지).</summary>
    private int AirAbove(int x, int y, int limit)
    {
        int n = 0;
        while (n < limit && !_anyTile.Contains(new Vector2Int(x, y + 1 + n))) n++;
        return n;
    }

    private Sprite Rand(Sprite[] arr) => arr[_rng.Next(arr.Length)];

    /// <summary>방 하나 드레싱 — 데모 씬 밀도: 뒷벽 패널 + 대형 세트피스 + 기둥
    /// + 표면 프린지(바닥/천장 라인) + 소품 군집 + 발광/빛 웅덩이 + 전경 실루엣.</summary>
    private void DressRoom(RoomBox r, ConstantAssetLibrary.PlanetAssets assets)
    {
        // ── 뒷벽 패널: 실내 영역을 세로 꽉 채움 + 가로 타일링. 벽 fill(order 6)이 넘침을 가려준다
        if (assets.wallPanelSprites != null && assets.wallPanelSprites.Length > 0)
        {
            Sprite panel = Rand(assets.wallPanelSprites);
            var interior = new Rect(r.x0 + 1, r.y0 + 2, r.W - 2, r.H - 3);
            interior.position += (Vector2)_map.transform.position;
            Color tint = r.isMain ? assets.wallTint : assets.wallTint * 0.78f;
            tint.a = 1f;
            PanelFit(panel, interior, r.isMain ? -30 : -29, tint);
        }

        PlaceSetPieces(r, assets);
        PlacePillars(r, assets);
        PlaceRoomFrame(r, assets);
        PlaceLightPools(r, assets);

        bool hasFloor = assets.floorPropSprites != null && assets.floorPropSprites.Length > 0;
        bool hasHang = assets.hangingPropSprites != null && assets.hangingPropSprites.Length > 0;
        bool hasGlow = assets.glowSprites != null && assets.glowSprites.Length > 0;
        bool hasFFringe = assets.floorFringeSprites != null && assets.floorFringeSprites.Length > 0;
        bool hasCFringe = assets.ceilingFringeSprites != null && assets.ceilingFringeSprites.Length > 0;
        float floorFringeChance = _profile.planet == ConstantPlanet.Eidron ? 0.16f : 0.34f;
        float ceilingFringeChance = _profile.planet == ConstantPlanet.Eidron ? 0.10f : 0.28f;

        for (int x = r.x0 + 2; x <= r.x1 - 2; x++)
        {
            for (int y = r.y0 + 1; y <= r.y1 - 2; y++)
            {
                // ── 바닥 표면 ──
                if (FloorSurface(x, y) && !NearSpot(r, x, y + 1))
                {
                    float floorTop = y + 1;

                    // 프린지: 거의 모든 바닥 라인을 따라 잔장식이 이어진다 (이끼/파편/잔해)
                    if (hasFFringe && _rng.NextDouble() < floorFringeChance)
                    {
                        float fh = _profile.planet == ConstantPlanet.Eidron
                            ? 0.30f + (float)_rng.NextDouble() * 0.28f
                            : 0.40f + (float)_rng.NextDouble() * 0.42f;
                        Vector3 fpos = _map.transform.position + new Vector3(x + 0.5f + ((float)_rng.NextDouble() - 0.5f) * 0.4f, floorTop + fh * 0.5f - 0.05f, 0f);
                        var fgo = SpriteFit(Rand(assets.floorFringeSprites), fpos, "Default", 7, fh, new Color(0.88f, 0.88f, 0.88f, 1f));
                        if (fgo != null && _rng.NextDouble() < 0.5)
                            fgo.transform.localScale = Vector3.Scale(fgo.transform.localScale, new Vector3(-1f, 1f, 1f));
                    }

                    double roll = _rng.NextDouble();
                    if (roll < 0.04 && hasFloor)
                    {
                        // 화면 중앙을 가리던 검은 전경 대신, 플레이 평면 뒤의 묵직한 배경 군집으로 쓴다.
                        float h = 1.8f + (float)_rng.NextDouble() * 1.2f;
                        Vector3 pos = _map.transform.position + new Vector3(x + 0.5f, floorTop + h * 0.5f, 0f);
                        Color backTint = Color.Lerp(assets.wallTint, Color.white, 0.12f);
                        backTint.a = 0.92f;
                        SpriteFit(Rand(assets.floorPropSprites), pos, "Default", -5, h, backTint);
                        y += 2;
                    }
                    else if (roll < 0.16 && hasFloor)
                    {
                        // 소품 군집: 큰 것 하나 + 작은 것 0~2개가 어우러진다 (데모의 clump 배치)
                        float bigH = 1.3f + (float)_rng.NextDouble() * 1.1f;
                        Vector3 bigPos = _map.transform.position + new Vector3(x + 0.5f, floorTop + bigH * 0.5f, 0f);
                        var bigGo = SpriteFit(Rand(assets.floorPropSprites), bigPos, "Default", 8, bigH);
                        if (bigGo != null && _rng.NextDouble() < 0.5)
                            bigGo.transform.localScale = Vector3.Scale(bigGo.transform.localScale, new Vector3(-1f, 1f, 1f));

                        int extras = _rng.Next(3);
                        for (int e = 0; e < extras; e++)
                        {
                            int ex = x + (_rng.NextDouble() < 0.5 ? -1 : 1);
                            if (!FloorSurface(ex, y)) continue;
                            bool glow = hasGlow && _rng.NextDouble() < 0.22;
                            float h = 0.55f + (float)_rng.NextDouble() * 0.6f;
                            Vector3 pos = _map.transform.position + new Vector3(ex + 0.5f + ((float)_rng.NextDouble() - 0.5f) * 0.5f, floorTop + h * 0.5f, 0f);
                            SpriteFit(glow ? Rand(assets.glowSprites) : Rand(assets.floorPropSprites), pos, "Default", glow ? 9 : 8, h);
                            if (glow && assets.lightSprite != null)
                            {
                                Color halo = assets.lightTint; halo.a = 0.22f;
                                SpriteFit(assets.lightSprite, pos, "Default", 8, h * 2.6f, halo);
                            }
                        }
                        y += 2;
                    }
                }

                // ── 천장 표면 ──
                if (CeilSurface(x, y))
                {
                    // 프린지: 천장 라인을 따라 종유석/케이블이 이어진다
                    if (hasCFringe && _rng.NextDouble() < ceilingFringeChance)
                    {
                        float fh = _profile.planet == ConstantPlanet.Eidron
                            ? 0.45f + (float)_rng.NextDouble() * 0.35f
                            : 0.65f + (float)_rng.NextDouble() * 0.65f;
                        Vector3 fpos = _map.transform.position + new Vector3(x + 0.5f, y - fh * 0.5f, 0f);
                        var fgo = SpriteFit(Rand(assets.ceilingFringeSprites), fpos, "Default", 7, fh, new Color(0.8f, 0.8f, 0.8f, 1f));
                        if (fgo != null && assets.hangingFlipY)
                            fgo.transform.localScale = Vector3.Scale(fgo.transform.localScale, new Vector3(1f, -1f, 1f));
                    }
                    // 큰 매달림 소품은 드물게
                    else if (hasHang && _rng.NextDouble() < 0.12)
                    {
                        float h = 1.2f + (float)_rng.NextDouble() * 1.4f;
                        Vector3 pos = _map.transform.position + new Vector3(x + 0.5f, y - h * 0.5f, 0f);
                        var go = SpriteFit(Rand(assets.hangingPropSprites), pos, "Default", 8, h);
                        if (go != null && assets.hangingFlipY)
                            go.transform.localScale = Vector3.Scale(go.transform.localScale, new Vector3(1f, -1f, 1f));
                    }
                }
            }
        }
    }

    /// <summary>대형 세트피스 — 방마다 1~2개의 랜드마크(거목/크리스탈군/코어)를 배경측에 세운다.
    /// 데모 씬이 화면마다 초점 오브젝트 하나로 공간의 정체성을 만드는 문법.</summary>
    private void PlaceSetPieces(RoomBox r, ConstantAssetLibrary.PlanetAssets assets)
    {
        if (assets.setPieceSprites == null || assets.setPieceSprites.Length == 0) return;

        int want = r.isMain ? 1 + _rng.Next(2) : (_rng.NextDouble() < 0.45 ? 1 : 0);
        int placed = 0;
        for (int attempt = 0; attempt < 14 && placed < want; attempt++)
        {
            int x = _rng.Next(r.x0 + 4, r.x1 - 3);
            for (int y = r.y0 + 1; y <= r.y1 - 6; y++)
            {
                if (!FloorSurface(x, y)) continue;
                int air = AirAbove(x, y, r.H);
                if (air < 6) break;

                float h = Mathf.Min(air - 1f, 4.8f + (float)_rng.NextDouble() * 3.0f);
                Vector3 pos = _map.transform.position + new Vector3(x + 0.5f, (y + 1) + h * 0.5f, 0f);
                Sprite piece = Rand(assets.setPieceSprites);
                // Lava foreground decor는 데모에서도 화면 모서리용이다. 방 중앙 랜드마크로 쓰면
                // 플레이 영역을 거대한 불투명 덩어리로 덮으므로 여기서는 제외한다.
                if (piece != null &&
                    (piece.name.Contains("foreground decor") || piece.name.Contains("Base boxes")))
                    continue;

                Color tint = Color.Lerp(assets.wallTint, Color.white, 0.3f);
                var go = SpriteFit(piece, pos, "Default", -14, h, tint);
                if (go != null && _rng.NextDouble() < 0.5)
                    go.transform.localScale = Vector3.Scale(go.transform.localScale, new Vector3(-1f, 1f, 1f));
                placed++;
                break;
            }
        }
    }

    /// <summary>바닥-천장 기둥 — 스프라이트를 세로로 이어 붙여 공간을 수직으로 끊어준다.</summary>
    private void PlacePillars(RoomBox r, ConstantAssetLibrary.PlanetAssets assets)
    {
        if (assets.pillarSprites == null || assets.pillarSprites.Length == 0 || !r.isMain) return;

        int want = 1 + _rng.Next(3);
        int placed = 0;
        for (int attempt = 0; attempt < 12 && placed < want; attempt++)
        {
            int x = _rng.Next(r.x0 + 3, r.x1 - 2);
            for (int y = r.y0 + 1; y <= r.y1 - 7; y++)
            {
                if (!FloorSurface(x, y)) continue;

                // 위쪽 천장(다음 솔리드)까지의 간격
                int gap = AirAbove(x, y, r.H);
                bool hasCeiling = _anyTile.Contains(new Vector2Int(x, y + 1 + gap));
                if (gap < 6 || !hasCeiling) break;

                Sprite s = Rand(assets.pillarSprites);
                if (s.bounds.size.y < 0.1f) break;
                float scale = (1.1f + (float)_rng.NextDouble() * 0.7f) / Mathf.Max(0.4f, s.bounds.size.x);
                float step = s.bounds.size.y * scale * 0.94f;
                int n = Mathf.CeilToInt(gap / step);

                var parent = new GameObject($"Pillar_{x}_{y}");
                parent.transform.SetParent(_deco, false);
                Color tint = assets.wallTint * 0.62f; tint.a = 1f;
                for (int i = 0; i < n; i++)
                {
                    var seg = new GameObject($"seg{i}");
                    seg.transform.SetParent(parent.transform, false);
                    seg.transform.position = _map.transform.position
                        + new Vector3(x + 0.5f, (y + 1) + step * (i + 0.5f), 0f);
                    seg.transform.localScale = Vector3.one * scale;
                    var sr = seg.AddComponent<SpriteRenderer>();
                    sr.sprite = s;
                    sr.sortingLayerName = "Default";
                    sr.sortingOrder = -10;
                    sr.color = tint;
                }
                placed++;
                break;
            }
        }
    }

    /// <summary>
    /// 메인 룸의 양 모서리에 반복되는 건축 프레임을 둔다.
    /// 랜덤 소품을 화면 중앙에 흩뿌리지 않고, 데모 씬처럼 시선을 중앙 플레이 공간으로 모은다.
    /// </summary>
    private void PlaceRoomFrame(RoomBox r, ConstantAssetLibrary.PlanetAssets assets)
    {
        if (!r.isMain || r.W < 16 || r.H < 12)
            return;

        Sprite[] verticals = assets.pillarSprites != null && assets.pillarSprites.Length > 0
            ? assets.pillarSprites
            : assets.setPieceSprites;

        if (verticals != null && verticals.Length > 0)
        {
            float height = Mathf.Min(9f, r.H - 4f);
            float y = r.y0 + 2f + height * 0.5f;
            Color tint = Color.Lerp(assets.wallTint, Color.white, 0.08f);
            tint.a = 0.94f;

            Sprite left = verticals[StableIndex($"{r.x0}:{r.y0}:L", verticals.Length)];
            Sprite right = verticals[StableIndex($"{r.x1}:{r.y1}:R", verticals.Length)];
            SpriteFit(left, _map.transform.position + new Vector3(r.x0 + 2.2f, y, 0f), "Default", -7, height, tint);
            var rightGo = SpriteFit(right, _map.transform.position + new Vector3(r.x1 - 1.2f, y, 0f),
                "Default", -7, height, tint);
            if (rightGo != null)
                rightGo.transform.localScale = Vector3.Scale(rightGo.transform.localScale, new Vector3(-1f, 1f, 1f));
        }

        if (assets.hangingPropSprites == null || assets.hangingPropSprites.Length == 0)
            return;

        float hangHeight = _profile.planet == ConstantPlanet.Eidron
            ? Mathf.Min(1.15f, r.H * 0.10f)
            : Mathf.Min(2.2f, r.H * 0.16f);
        Color hangTint = Color.Lerp(assets.wallTint, Color.white, 0.18f);
        hangTint.a = 1f;
        for (int side = 0; side < 2; side++)
        {
            float x = side == 0 ? r.x0 + 3.4f : r.x1 - 2.4f;
            Sprite sprite = assets.hangingPropSprites[
                StableIndex($"{r.x0}:{r.y0}:H:{side}", assets.hangingPropSprites.Length)];
            var go = SpriteFit(sprite,
                _map.transform.position + new Vector3(x, r.y1 - hangHeight * 0.5f, 0f),
                "Default", 4, hangHeight, hangTint);
            if (go != null && assets.hangingFlipY)
                go.transform.localScale = Vector3.Scale(go.transform.localScale, new Vector3(1f, -1f, 1f));
        }
    }

    /// <summary>빛 웅덩이 — 소프트 블롭을 낮은 알파로 깔아 데모의 빛기둥/채광 무드를 흉내낸다.</summary>
    private void PlaceLightPools(RoomBox r, ConstantAssetLibrary.PlanetAssets assets)
    {
        if (assets.lightSprite == null) return;
        if (assets.lightSprite.name.Contains("lamp") || assets.lightSprite.name.Contains("Shadow"))
            return; // 램프/Shadow 원화는 광원 마스크가 아니라 실제 기구/불투명 그림자다.
        int want = r.isMain ? 1 + _rng.Next(2) : (_rng.NextDouble() < 0.5 ? 1 : 0);

        int placed = 0;
        for (int attempt = 0; attempt < 10 && placed < want; attempt++)
        {
            int x = _rng.Next(r.x0 + 3, r.x1 - 2);
            for (int y = r.y0 + 1; y <= r.y1 - 4; y++)
            {
                if (!FloorSurface(x, y)) continue;
                float h = 5f + (float)_rng.NextDouble() * 3f;
                Color c = assets.lightTint; c.a = 0.10f;
                SpriteFit(assets.lightSprite, _map.transform.position + new Vector3(x + 0.5f, (y + 1) + h * 0.35f, 0f),
                    "Default", -8, h, c);
                placed++;
                break;
            }
        }
    }

    /// <summary>콘텐츠 마커(상점/아이템/몹 스폰) 근처인가 — 소품이 가리지 않게.</summary>
    private bool NearSpot(RoomBox r, int x, int y)
    {
        foreach (var s in r.spots)
            if (Mathf.Abs(s.x - x) <= 1 && Mathf.Abs(s.y - y) <= 2) return true;
        foreach (var s in r.enemySpots)
            if (Mathf.Abs(s.x - x) <= 1 && Mathf.Abs(s.y - y) <= 2) return true;
        return false;
    }

    /// <summary>월드 사각형을 정확히 채우는 패널 타일링 — 세로는 꽉, 가로는 살짝 압축해 맞춘다.</summary>
    private void PanelFit(Sprite sprite, Rect worldRect, int order, Color tint)
    {
        if (sprite == null) return;
        Vector2 spr = sprite.bounds.size;
        if (spr.x < 0.001f || spr.y < 0.001f || worldRect.width < 0.5f || worldRect.height < 0.5f) return;

        float scaleY = worldRect.height / spr.y;
        float naturalW = spr.x * scaleY;
        int n = Mathf.Max(1, Mathf.CeilToInt(worldRect.width / naturalW));
        float squeeze = worldRect.width / (n * naturalW);

        for (int i = 0; i < n; i++)
        {
            var go = new GameObject($"Panel_{sprite.name}_{i}");
            go.transform.SetParent(_deco, false);
            float cx = worldRect.xMin + naturalW * squeeze * (i + 0.5f);
            go.transform.position = new Vector3(cx, worldRect.center.y, 0f);
            go.transform.localScale = new Vector3(scaleY * squeeze, scaleY, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = order;
            sr.color = tint;
        }
    }

    // 원거리 단일 배경 — 맵 전체 뒤에 하나만 깔고 ConstantParallaxLayer 로
    // 거리 500 상당(비율 0.04)의 시차를 재현한다. 배경은 카메라를 거의 따라오므로
    // 크기는 화면(16×9) + 맵을 가로지를 때의 미끄러짐 + 여유만 있으면 된다.
    private const float BackdropDepthRatio = 0.04f; // 플레이평면거리 20 / 가상 배경거리 500
    private const float BackdropZ = 50f;

    private void PlaceFarBackdrop(Sprite sprite, List<RoomBox> rooms)
    {
        if (sprite == null || rooms == null || rooms.Count == 0) return;

        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        foreach (var r in rooms)
        {
            minX = Mathf.Min(minX, r.x0); minY = Mathf.Min(minY, r.y0);
            maxX = Mathf.Max(maxX, r.x1); maxY = Mathf.Max(maxY, r.y1);
        }

        Vector3 mapCenter = _map.transform.position
            + new Vector3((minX + maxX + 1) * 0.5f, (minY + maxY + 1) * 0.5f, 0f);

        float needW = 16f + (maxX - minX) * BackdropDepthRatio + 8f;
        float needH = 9f + (maxY - minY) * BackdropDepthRatio + 8f;

        Vector2 sprSize = sprite.bounds.size;
        float targetH = needH;
        if (sprSize.x > 0.001f)
            targetH = Mathf.Max(needH, needW * (sprSize.y / sprSize.x)); // 세로 기준 스케일이라 폭도 보장

        // 주의: 이 프로젝트 소팅 레이어는 Default(0) < Background(1) 순서라
        // "Background" 레이어에 두면 지형(Default/6)을 덮어버린다 → Default/-50 사용
        GameObject go = SpriteFit(sprite, new Vector3(mapCenter.x, mapCenter.y, BackdropZ), "Default", -50, targetH);
        if (go != null)
            go.AddComponent<ConstantParallaxLayer>().Init(mapCenter, BackdropDepthRatio);
    }

    private GameObject SpriteFit(Sprite sprite, Vector3 pos, string layer, int order, float targetH, Color? tint = null)
    {
        if (sprite == null) return null;
        var go = new GameObject($"Deco_{sprite.name}");
        go.transform.SetParent(_deco, false);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        if (tint.HasValue) sr.color = tint.Value;
        float h = sprite.bounds.size.y;
        if (h > 0.001f) go.transform.localScale = Vector3.one * (targetH / h);
        return go;
    }

    /// <summary>
    /// 기본 TileMapData의 가스는 충돌 범위를 보여 주는 연두색 사각형이다.
    /// Constant에서는 동일한 콜라이더/로직을 유지하되 소프트 마스크로 바꿔 안개처럼 보이게 한다.
    /// </summary>
    private void StyleGeneratedHazards()
    {
        Transform gasRoot = _map.transform.Find("GasZones");
        if (gasRoot == null) return;

        Sprite softMask = Resources.Load<Sprite>("Sprites/VisionMask");
        Color tint = _profile.planet switch
        {
            ConstantPlanet.Lavernis => new Color(0.82f, 0.26f, 0.10f, 0.20f),
            ConstantPlanet.Sylmare => new Color(0.30f, 0.72f, 0.78f, 0.18f),
            _ => new Color(0.36f, 0.66f, 0.56f, 0.17f),
        };

        foreach (var renderer in gasRoot.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (softMask != null)
                renderer.sprite = softMask;
            renderer.color = tint;
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 7;
        }
    }

    private void Label(string text, Vector3 pos, float size, Color color, Transform parent = null)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent != null ? parent : _deco, true);
        go.transform.position = pos;
        var tmp = go.AddComponent<TextMeshPro>();
        if (_lib != null && _lib.koreanFont != null) tmp.font = _lib.koreanFont;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.rectTransform.sizeDelta = new Vector2(10f, 1.4f);
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null) { mr.sortingLayerName = "Objects"; mr.sortingOrder = 20; }
    }

    private GameObject Gimmick(string name, int gx, int gy, Color color, float scale, string labelText, Color labelColor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_gameplay, false);
        go.transform.position = World(gx, gy);
        var vis = new GameObject("Visual");
        vis.transform.SetParent(go.transform, false);
        var sr = vis.AddComponent<SpriteRenderer>();
        Sprite art = PickGimmickSprite(name);
        sr.sprite = art != null ? art : Square();
        sr.color = art != null ? Color.Lerp(Color.white, color, 0.32f) : color;
        sr.sortingLayerName = "Objects";
        sr.sortingOrder = 9;
        if (art != null && art.bounds.size.y > 0.001f)
            vis.transform.localScale = Vector3.one * ((scale * 1.8f) / art.bounds.size.y);
        else
            vis.transform.localScale = Vector3.one * scale;

        AddGimmickAccent(go.transform, name, color, scale);
        Label(labelText, go.transform.position + new Vector3(0f, 0.95f), 2.0f, labelColor, go.transform);
        return go;
    }

    private Sprite PickGimmickSprite(string key)
    {
        var assets = _lib != null ? _lib.For(_profile.planet) : null;
        if (assets == null) return null;

        Sprite[] pool;
        if (key.StartsWith("Item_") && assets.glowSprites != null && assets.glowSprites.Length > 0)
            pool = assets.glowSprites;
        else if (assets.floorFringeSprites != null && assets.floorFringeSprites.Length > 0)
            pool = assets.floorFringeSprites;
        else
            pool = assets.floorPropSprites;

        return pool != null && pool.Length > 0 ? pool[StableIndex(key, pool.Length)] : null;
    }

    private void AddGimmickAccent(Transform parent, string key, Color color, float scale)
    {
        var assets = _lib != null ? _lib.For(_profile.planet) : null;
        if (assets?.glowSprites == null || assets.glowSprites.Length == 0)
            return;

        Sprite sprite = assets.glowSprites[StableIndex(key + ":accent", assets.glowSprites.Length)];
        if (sprite == null || sprite.bounds.size.y < 0.001f)
            return;

        var accent = new GameObject("Accent");
        accent.transform.SetParent(parent, false);
        accent.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        accent.transform.localScale = Vector3.one * ((scale * 0.72f) / sprite.bounds.size.y);
        var renderer = accent.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = Color.Lerp(Color.white, color, 0.2f);
        renderer.sortingLayerName = "Objects";
        renderer.sortingOrder = 10;
    }

    private static int StableIndex(string key, int length)
    {
        if (length <= 1) return 0;
        unchecked
        {
            int hash = 17;
            for (int i = 0; i < key.Length; i++)
                hash = hash * 31 + key[i];
            return (hash & int.MaxValue) % length;
        }
    }

    private GameObject MakeItem(string itemId, int gx, int gy)
    {
        var def = ConstantItemDb.Get(itemId);
        if (def == null) return null;
        var go = Gimmick($"Item_{itemId}", gx, gy, ConstantDefine.ColorOf(def.property), 0.62f,
            def.displayName, def.isRelic ? new Color(0.91f, 0.77f, 0.42f) : Color.white);
        go.AddComponent<ItemPickup>().Init(itemId, false);
        return go;
    }

    private void MakeNode(string name, int gx, int gy, float gauge)
    {
        var assets = _lib != null ? _lib.For(_profile.planet) : null;
        var go = new GameObject($"Node_{name}_{gx}_{gy}");
        go.transform.SetParent(_gameplay, false);
        go.transform.position = World(gx, gy);
        var vis = new GameObject("Visual");
        vis.transform.SetParent(go.transform, false);
        var sr = vis.AddComponent<SpriteRenderer>();
        if (assets != null && assets.nodeSprite != null)
        {
            sr.sprite = assets.nodeSprite;
            float h = assets.nodeSprite.bounds.size.y;
            if (h > 0.001f) vis.transform.localScale = Vector3.one * (1.25f / h);
        }
        else { sr.sprite = Square(); vis.transform.localScale = Vector3.one * 0.8f; }
        sr.color = _profile.nodeTint;
        sr.sortingLayerName = "Objects";
        sr.sortingOrder = 8;
        Label(name, go.transform.position + new Vector3(0f, 1.05f), 1.9f, new Color(1f, 1f, 1f, 0.6f), go.transform);
        go.AddComponent<ResourceNode>().Init(name, gauge, 1);
    }

    private void MakeValve(ValveQuest quest, int gx, int gy)
    {
        var go = Gimmick($"Valve_{gx}_{gy}", gx, gy, new Color(0.45f, 0.85f, 1f), 0.7f, "밸브", new Color(0.6f, 0.85f, 1f, 0.85f));
        go.AddComponent<CoolantValve>().Init(quest);
    }

    private void MakeCore(ValveQuest quest, int gx, int gy)
    {
        var go = new GameObject($"ProtectedCore_{gx}_{gy}");
        go.transform.SetParent(_gameplay, false);
        go.transform.position = World(gx, gy);
        var vis = new GameObject("Visual");
        vis.transform.SetParent(go.transform, false);
        var sr = vis.AddComponent<SpriteRenderer>();
        if (_lib != null && _lib.coreSprite != null)
        {
            sr.sprite = _lib.coreSprite;
            float h = _lib.coreSprite.bounds.size.y;
            if (h > 0.001f) vis.transform.localScale = Vector3.one * (2.0f / h);
        }
        else { sr.sprite = Square(); sr.color = new Color(1f, 0.5f, 0.4f); }
        sr.sortingLayerName = "Objects";
        sr.sortingOrder = 8;
        Label("보호핵", go.transform.position + new Vector3(0f, 1.5f), 2.2f, new Color(1f, 0.6f, 0.5f), go.transform);
        go.AddComponent<ProtectedCore>().Init(quest);
    }

    private void MakeShrine(int gx, int gy)
    {
        var go = new GameObject("SilenceShrine");
        go.transform.SetParent(_gameplay, false);
        go.transform.position = World(gx, gy);
        var vis = new GameObject("Visual");
        vis.transform.SetParent(go.transform, false);
        var sr = vis.AddComponent<SpriteRenderer>();
        if (_lib != null && _lib.crystalSprite != null)
        {
            sr.sprite = _lib.crystalSprite;
            float h = _lib.crystalSprite.bounds.size.y;
            if (h > 0.001f) vis.transform.localScale = Vector3.one * (1.8f / h);
        }
        else sr.sprite = Square();
        sr.sortingLayerName = "Objects";
        sr.sortingOrder = 8;

        var labelGo = new GameObject("Progress");
        labelGo.transform.SetParent(go.transform, false);
        labelGo.transform.position = go.transform.position + new Vector3(0f, 1.6f);
        var tmp = labelGo.AddComponent<TextMeshPro>();
        if (_lib != null && _lib.koreanFont != null) tmp.font = _lib.koreanFont;
        tmp.text = "…";
        tmp.fontSize = 2.4f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.95f, 0.9f, 0.75f);
        tmp.rectTransform.sizeDelta = new Vector2(10f, 1.4f);
        var mr = labelGo.GetComponent<MeshRenderer>();
        if (mr != null) { mr.sortingLayerName = "Objects"; mr.sortingOrder = 20; }

        Label("침묵의 사당 — 곁에서 가만히 기다려 보자", go.transform.position + new Vector3(0f, 2.5f), 2.0f, new Color(0.8f, 0.7f, 1f, 0.7f), go.transform);
        go.AddComponent<SilenceShrine>().Init(tmp, sr);
    }

    private void MakeSwitch(ProtocolQuest quest, int index, int gx, int gy)
    {
        var go = Gimmick($"Switch_{index}", gx, gy, new Color(0.95f, 0.8f, 0.4f), 0.7f,
            $"규약 스위치 {index}", new Color(0.95f, 0.9f, 0.6f, 0.85f));
        go.AddComponent<SequenceSwitch>().Init(quest, index);
    }

    private GameObject MakeGate(int gx, int gy, int height, Color color)
    {
        var go = new GameObject($"Gate_{gx}_{gy}");
        go.transform.SetParent(_gameplay, false);
        go.transform.position = World(gx, gy) + new Vector3(0f, (height - 1) * 0.5f);
        go.layer = LayerMask.NameToLayer("Ground");
        var sr = go.AddComponent<SpriteRenderer>();
        var assets = _lib != null ? _lib.For(_profile.planet) : null;
        Sprite gateSprite = assets?.pillarSprites != null && assets.pillarSprites.Length > 0
            ? assets.pillarSprites[StableIndex($"Gate:{gx}:{gy}", assets.pillarSprites.Length)]
            : (assets?.ceilingFringeSprites != null && assets.ceilingFringeSprites.Length > 0
                ? assets.ceilingFringeSprites[StableIndex($"Gate:{gx}:{gy}", assets.ceilingFringeSprites.Length)]
                : null);
        sr.sprite = gateSprite != null ? gateSprite : Square();
        sr.color = gateSprite != null ? Color.Lerp(Color.white, color, 0.28f) : color;
        sr.sortingLayerName = "Objects";
        sr.sortingOrder = 7;
        if (gateSprite != null && gateSprite.bounds.size.y > 0.001f)
        {
            float s = height / gateSprite.bounds.size.y;
            go.transform.localScale = new Vector3(
                Mathf.Min(s, 0.9f / Mathf.Max(0.001f, gateSprite.bounds.size.x)), s, 1f);
        }
        else
            go.transform.localScale = new Vector3(1f, height, 1f);
        go.AddComponent<BoxCollider2D>().size = Vector2.one;
        return go;
    }

    private void MakeTagGate(int gx, int gy, string name, PropertyTag tag, int count, GameObject blocker)
    {
        var go = Gimmick($"TagGate_{name}", gx, gy, new Color(0.8f, 0.7f, 0.9f), 0.7f,
            $"{name} [X]", new Color(0.8f, 0.7f, 0.9f, 0.9f));
        go.AddComponent<TagGate>().Init(name, tag, count, blocker, null, "봉인이 풀렸다 — 안쪽이 열린다");
    }

    private void MakeEnemy(int minGx, int maxGx, int gy)
    {
        var go = new GameObject($"Enemy_{minGx}_{gy}");
        go.transform.SetParent(_gameplay, false);
        go.transform.position = new Vector3((minGx + maxGx + 1) * 0.5f, gy + 0.55f, 0f);
        if (_lib != null && _lib.spiderPrefab != null)
        {
            var vis = Object.Instantiate(_lib.spiderPrefab, go.transform);
            vis.name = "Visual";
            vis.transform.localPosition = Vector3.zero;
            foreach (var r in vis.GetComponentsInChildren<SpriteRenderer>())
            { r.sortingLayerName = "Objects"; r.sortingOrder = 12; }
        }
        else
        {
            var vis = new GameObject("Visual");
            vis.transform.SetParent(go.transform, false);
            var sr = vis.AddComponent<SpriteRenderer>();
            sr.sprite = Square();
            sr.color = new Color(0.9f, 0.3f, 0.3f);
            sr.sortingLayerName = "Objects";
            sr.sortingOrder = 12;
            vis.transform.localScale = Vector3.one * 0.7f;
        }
        go.AddComponent<PatrolEnemy>().Init(minGx + 0.5f, maxGx + 0.5f,
            1.8f + (float)_rng.NextDouble() * 0.8f);
    }

    private void MakePad(int gx, int gy, GameObject padPrefab, string labelText)
    {
        var go = new GameObject("DeparturePad");
        go.transform.SetParent(_gameplay, false);
        go.transform.position = World(gx, gy);
        if (padPrefab != null)
        {
            var vis = Object.Instantiate(padPrefab, go.transform);
            vis.transform.position = World(gx, gy) + new Vector3(0f, -0.5f);
        }
        Label(labelText, go.transform.position + new Vector3(0f, 2.3f), 2.8f, new Color(0.91f, 0.77f, 0.42f), go.transform);
        go.AddComponent<DeparturePad>().Init(2.2f);
    }

    private void MakeShop(int gx, int gy, int goodsIndex)
    {
        string[] names = { "로프 2개 — 게이지 10%", "폭탄 2개 — 게이지 10%", "??? — 게이지 15%" };
        float[] costs = { 10f, 10f, 15f };
        int gi = Mathf.Clamp(goodsIndex, 0, 2);
        var go = Gimmick($"Shop_{gx}_{gy}", gx, gy, new Color(0.9f, 0.75f, 0.4f), 0.7f,
            $"{names[gi]} [X]", new Color(0.95f, 0.85f, 0.6f, 0.9f));
        go.AddComponent<ShopItem>().Init((ShopItem.Goods)gi, costs[gi]);
    }

    private void MakeStation<T>(Vector2Int pos, string labelText, Color color) where T : ConstantInteractable
    {
        var go = Gimmick($"{typeof(T).Name}_{pos.x}_{pos.y}", pos.x, pos.y, color, 0.8f,
            labelText, new Color(color.r, color.g, color.b, 0.9f));
        go.AddComponent<T>();
    }

    private void MakeConsumable(int gx, int gy, bool bomb)
    {
        var go = Gimmick(bomb ? $"Bomb_{gx}_{gy}" : $"Rope_{gx}_{gy}", gx, gy,
            bomb ? new Color(0.25f, 0.25f, 0.28f) : new Color(0.75f, 0.6f, 0.4f), 0.5f,
            bomb ? "폭탄 [X]" : "로프 [X]", new Color(1f, 1f, 1f, 0.7f));
        go.AddComponent<ConsumablePickup>().Init(bomb, 1);
    }

    private void MakeObserver(string node, string name, int gx, int gy, Color color)
    {
        var go = Gimmick($"Observer_{node}", gx, gy, color, 0.85f, $"{name} [X]",
            new Color(color.r, color.g, color.b, 0.9f));
        go.AddComponent<ConstantObserver>().Init(node, false, 2.0f);
    }
}
