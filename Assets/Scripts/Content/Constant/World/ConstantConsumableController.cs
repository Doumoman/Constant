using System.Collections;
using UnityEngine;

/// <summary>
/// 소모품 사용 컨트롤러 — 씬에 하나 배치.
/// R: 로프 (머리 위로 밧줄을 쏘아 올려 기어오를 수 있는 임시 사다리 생성)
/// F: 폭탄 (1.2초 후 폭발 — 주변 3x3 지형 파괴 + 근처 몹 제거)
/// 올라갈 수 없는 방·막힌 벽을 파훼하는 탈출 도구다. (legacy Input 폴링 — 프로젝트는 Both 모드)
/// </summary>
public class ConstantConsumableController : MonoBehaviour
{
    [SerializeField] private int _ropeLength = 8;
    [SerializeField] private float _bombFuse = 1.2f;
    [SerializeField] private float _bombRadius = 1.6f;

    private PlayerFSM _player;
    private TileMapData _map;

    // 시간 역재생기 (조합: 태엽 나침반 + 위치 고정못) — 방 진입 지점 추적
    private CameraRoomData _currentRoom;
    private Vector3 _currentRoomEntry;
    private Vector3 _prevRoomEntry;
    private bool _hasPrevEntry;
    private float _rewindCooldownUntil;

    private void Start()
    {
        _player = FindFirstObjectByType<PlayerFSM>();
        _map = FindFirstObjectByType<TileMapData>();

        if (_player != null)
        {
            _currentRoomEntry = _player.transform.position;
            if (_map != null && _map.TryGetCameraRoom(_player.transform.position, out var room, out _))
                _currentRoom = room;
        }

        // Input System 액션 배선 (GameControls: Rope=R, Bomb=B, Diary=V)
        var input = SingletonManagers.Input;
        if (input != null)
        {
            input.OnRopePressed -= HandleRope;
            input.OnRopePressed += HandleRope;
            input.OnBombPressed -= HandleBomb;
            input.OnBombPressed += HandleBomb;
            input.OnDiaryPressed -= HandleDiary;
            input.OnDiaryPressed += HandleDiary;
        }
    }

    private void OnDestroy()
    {
        var input = SingletonManagers.Input;
        if (input != null)
        {
            input.OnRopePressed -= HandleRope;
            input.OnBombPressed -= HandleBomb;
            input.OnDiaryPressed -= HandleDiary;
        }
    }

    private void Update()
    {
        if (_player == null || _player.IsDead) return;
        TrackRoomEntry();
    }

    private bool CanAct()
    {
        if (_player == null || _player.IsDead) return false;
        if (SingletonManagers.UI != null && SingletonManagers.UI.PopupCount > 0) return false;
        if (SingletonManagers.Story != null && SingletonManagers.Story.IsRunning) return false;
        return true;
    }

    private void HandleRope() { if (CanAct()) TryRope(); }
    private void HandleBomb() { if (CanAct()) TryBomb(); }

    private void HandleDiary()
    {
        if (!CanAct()) return;
        Time.timeScale = 0f;
        SingletonManagers.Input.SetInputModeUI(true);
        SingletonManagers.UI.ShowPopupUI<UI_Popup_Codex>();
    }

    // ─────────────────────────────────────────────────────────────
    // 제작 아이템 효과 실행기 (여행가방 F 사용 → RunManager 가 호출)
    // ─────────────────────────────────────────────────────────────
    public void ExecuteCraftedEffect(string effectId, float power)
    {
        var run = SingletonManagers.Run;
        if (run == null || _player == null) return;

        switch (effectId)
        {
            case "shield": // 일정 시간 용암/가스 면역 (작열 부여)
                var taste = _player.GetComponent<PlayerTaste>();
                taste?.Eat(TasteKind.Heat, power);
                run.Toast($"방호막 전개 — {power:0}초간 용암/가스 면역");
                break;

            case "stun": // 씬 전체 몹 기절
                foreach (var e in FindObjectsByType<PatrolEnemy>(FindObjectsSortMode.None))
                    e.Stun(power);
                run.Toast($"굉음! 모든 몹이 {power:0}초 기절했다");
                break;

            case "bombPack": run.AddBomb((int)power); break;
            case "ropePack": run.AddRope((int)power); break;

            case "gaugeCell": run.AddGauge(power); run.Toast($"연료 변환 — 게이지 +{power:0}%"); break;

            case "sonar":
            {
                CoolantValve best = null; float bd = float.MaxValue;
                foreach (var v in FindObjectsByType<CoolantValve>(FindObjectsSortMode.None))
                {
                    float dist = Vector2.Distance(v.transform.position, _player.transform.position);
                    if (dist < bd) { bd = dist; best = v; }
                }
                if (best == null) { run.Toast("탐지 실패 — 반응하는 밸브가 없다"); break; }
                Vector2 dir = best.transform.position - _player.transform.position;
                string h = dir.x > 3 ? "동" : dir.x < -3 ? "서" : "";
                string vdir = dir.y > 3 ? "북" : dir.y < -3 ? "남" : "";
                run.Toast($"탐지음 — 가장 가까운 밸브: {vdir}{h}쪽 {bd:0}m");
                break;
            }

            case "rewind": RewindNow(); break;

            case "torch": StartCoroutine(CoTorch(power)); break;

            case "orbitalHook": SpawnRope((int)power); run.Toast("강철 로프가 하늘로 뻗었다"); break;

            case "blink":
            {
                float dir = Mathf.Sign(_player.transform.localScale.x);
                Vector3 dest = _player.transform.position + new Vector3(dir * power, 0f);
                _player.Teleport(dest);
                run.Toast("공간이 접혔다");
                break;
            }

            case "bridgeKit":
            {
                Vector2Int g = _map.WorldToGrid(_player.transform.position + Vector3.down);
                int half = (int)power / 2;
                for (int dx = -half; dx <= half; dx++)
                {
                    var p = new Vector2Int(g.x + dx, g.y);
                    if (_map.GetTile(p) == null)
                        _map.AddOrReplace(p, TileType.Ground, Vector2.one);
                }
                _map.RebuildAll(); // 임시 발판 실체화
                run.Toast("임시 발판이 조립되었다");
                break;
            }

            case "drillDown": Drill(Vector2Int.down, (int)power); break;
            case "drillUp": Drill(Vector2Int.up, (int)power); break;
            case "drillForward":
                Drill(_player.transform.localScale.x >= 0 ? Vector2Int.right : Vector2Int.left, (int)power);
                break;

            case "valveRemote":
            {
                foreach (var v in FindObjectsByType<CoolantValve>(FindObjectsSortMode.None))
                {
                    var canProp = typeof(CoolantValve).GetProperty("CanInteract",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (canProp != null && (bool)canProp.GetValue(v))
                    {
                        var m = typeof(CoolantValve).GetMethod("OnInteract",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        m?.Invoke(v, null);
                        run.Toast("중계 종이 울리자 어딘가의 밸브가 저절로 돌았다");
                        return;
                    }
                }
                run.Toast("돌릴 밸브가 남아 있지 않다");
                break;
            }

            case "singularity":
            {
                var quest = FindObjectsByType<ValveQuest>(FindObjectsSortMode.None);
                foreach (var q in quest) q.ForceOpenGate();
                run.Toast("특이점이 열렸다 — 출구 게이트가 개방되었다");
                break;
            }
        }
    }

    /// <summary>지정 방향으로 n칸 지형 관통 파괴 (드릴 계열).</summary>
    private void Drill(Vector2Int dir, int length)
    {
        if (_map == null) return;
        Vector2Int start = _map.WorldToGrid(_player.transform.position);
        int destroyed = 0;

        for (int i = 1; i <= length; i++)
        {
            foreach (int side in new[] { 0, 1 }) // 2칸 폭 (플레이어 통행 확보)
            {
                Vector2Int p = start + dir * i;
                if (dir.x != 0) p.y += side; else p.x += side == 0 ? 0 : 1;

                if (p.x <= 0 || p.x >= 250 || p.y <= 1 || p.y >= 250) continue;
                var tile = _map.GetTile(p);
                if (tile == null) continue;
                if (tile.type != TileType.Ground && tile.type != TileType.Spike &&
                    tile.type != TileType.Disguised) continue;

                if (_map.RemoveTile(p)) { destroyed++; DestroyTileObject(p, tile.type); }
            }
        }

        if (destroyed > 0) _map.RebuildGroundSpriteShapes();
        SingletonManagers.Run?.Toast(destroyed > 0 ? $"관통! 지형 {destroyed}칸 파괴" : "…단단한 벽이 아니었다");
        StartCoroutine(CoFlash(_player.transform.position));
    }

    private System.Collections.IEnumerator CoTorch(float duration)
    {
        var vision = FindFirstObjectByType<PlayerVision>();
        if (vision == null) { SingletonManagers.Run?.Toast("이곳은 이미 충분히 밝다"); yield break; }
        float old = vision.BaseRadius;
        vision.BaseRadius = old + 3f;
        SingletonManagers.Run?.Toast($"별빛 점등 — {duration:0}초간 시야 +3");
        yield return new WaitForSeconds(duration);
        vision.BaseRadius = old;
    }

    // ─────────────────────────────────────────────────────────────
    // 시간 역재생기 — T: 이전 방 진입 지점으로 되감기
    // ─────────────────────────────────────────────────────────────
    private void TrackRoomEntry()
    {
        if (_map == null) return;
        if (!_map.TryGetCameraRoom(_player.transform.position, out var room, out _)) return;

        bool changed = _currentRoom == null
            || room.startGridPos != _currentRoom.startGridPos
            || room.endGridPos != _currentRoom.endGridPos;
        if (!changed) return;

        _prevRoomEntry = _currentRoomEntry;
        _hasPrevEntry = _currentRoom != null;
        _currentRoom = room;
        _currentRoomEntry = _player.transform.position;
    }

    /// <summary>시간 역재생 — [시간 역재생기]/[시간 닻] 사용 효과.</summary>
    public void RewindNow()
    {
        var run = SingletonManagers.Run;
        if (run == null) return;

        if (!_hasPrevEntry)
        {
            run.Toast("되감을 기억이 아직 없다 — 다른 방을 지나온 뒤에 쓸 수 있다");
            return;
        }

        Vector3 dest = _prevRoomEntry;
        (_prevRoomEntry, _currentRoomEntry) = (_currentRoomEntry, dest);

        _player.Teleport(dest);
        run.Toast("시간이 되감겼다 — 이전 방의 진입 지점으로");
    }

    // ─────────────────────────────────────────────────────────────
    // 로프
    // ─────────────────────────────────────────────────────────────
    private void TryRope()
    {
        var run = SingletonManagers.Run;
        if (run == null) return;

        if (run.Ropes <= 0)
        {
            run.Toast("로프가 없다 — 보급 상자나 상점을 찾아보자");
            return;
        }

        // 머리 위로 뻗을 수 있는 높이 계산 (천장까지)
        Vector2 origin = _player.transform.position + Vector3.up * 0.6f;
        int height = _ropeLength;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.up, _ropeLength,
            LayerMask.GetMask("Ground"));
        if (hit.collider != null)
            height = Mathf.Max(2, Mathf.FloorToInt(hit.distance));

        if (!run.UseRope()) return;

        SpawnRope(height);
        run.Toast($"로프 사용 — 위/아래 키로 오르내릴 수 있다 (남은 로프 {run.Ropes})");
    }

    /// <summary>로프 실체 생성 (소모 없이) — 궤도 갈고리 등도 사용.</summary>
    public void SpawnRope(int maxHeight)
    {
        Vector2 origin = _player.transform.position + Vector3.up * 0.6f;
        int height = maxHeight;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.up, maxHeight, LayerMask.GetMask("Ground"));
        if (hit.collider != null)
            height = Mathf.Max(2, Mathf.FloorToInt(hit.distance));

        GameObject rope = new GameObject("Rope");
        rope.layer = LayerMask.NameToLayer("Ladder");
        rope.transform.position = new Vector3(
            Mathf.Floor(origin.x) + 0.5f,
            origin.y + height * 0.5f - 0.3f, 0f);

        BoxCollider2D col = rope.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.6f, height);

        SpriteRenderer sr = rope.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("Sprites/Square");
        sr.color = new Color(0.75f, 0.6f, 0.4f, 0.9f);
        sr.sortingLayerName = "Objects";
        sr.sortingOrder = 5;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(0.18f, height);
    }

    // ─────────────────────────────────────────────────────────────
    // 폭탄
    // ─────────────────────────────────────────────────────────────
    private void TryBomb()
    {
        var run = SingletonManagers.Run;
        if (run == null) return;

        if (run.Bombs <= 0)
        {
            run.Toast("폭탄이 없다 — 보급 상자나 상점을 찾아보자");
            return;
        }

        if (!run.UseBomb()) return;

        StartCoroutine(CoBomb(_player.transform.position));
        run.Toast($"폭탄 설치! (남은 폭탄 {run.Bombs})");
    }

    private IEnumerator CoBomb(Vector3 pos)
    {
        // 폭탄 비주얼
        GameObject bomb = new GameObject("Bomb");
        bomb.transform.position = pos;
        SpriteRenderer sr = bomb.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("Sprites/Square");
        sr.color = new Color(0.15f, 0.15f, 0.15f);
        sr.sortingLayerName = "Objects";
        sr.sortingOrder = 15;
        bomb.transform.localScale = Vector3.one * 0.45f;

        // 퓨즈 점멸
        float t = 0f;
        while (t < _bombFuse)
        {
            t += Time.deltaTime;
            sr.color = (Mathf.FloorToInt(t * 8f) % 2 == 0)
                ? new Color(0.15f, 0.15f, 0.15f)
                : new Color(0.9f, 0.3f, 0.2f);
            yield return null;
        }

        Explode(pos);
        Destroy(bomb);
    }

    private void Explode(Vector3 center)
    {
        var run = SingletonManagers.Run;
        int destroyed = 0;

        // 도화선 회로/폭발 기관 패시브 — 5x5 확장
        int radius = SingletonManagers.Run != null && SingletonManagers.Run.Synergy.bigBomb ? 2 : 1;

        if (_map != null)
        {
            Vector2Int c = _map.WorldToGrid(center);
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    Vector2Int p = new Vector2Int(c.x + dx, c.y + dy);

                    // 외곽 경계는 파괴 불가 (맵 밖 추락 방지)
                    if (p.x <= 0 || p.x >= 79 || p.y <= 1 || p.y >= 127) continue;

                    TileData tile = _map.GetTile(p);
                    if (tile == null) continue;
                    // 파괴 대상: 지형/가시/위장 (유체·가스·사다리는 유지)
                    if (tile.type != TileType.Ground && tile.type != TileType.Spike &&
                        tile.type != TileType.Disguised) continue;

                    if (_map.RemoveTile(p))
                    {
                        destroyed++;
                        DestroyTileObject(p, tile.type);
                    }
                }
            }

            if (destroyed > 0)
                _map.RebuildGroundSpriteShapes(); // 지형 스킨 갱신
        }

        // 근처 몹 제거
        foreach (var enemy in FindObjectsByType<PatrolEnemy>(FindObjectsSortMode.None))
        {
            if (Vector2.Distance(enemy.transform.position, center) <= _bombRadius + 0.6f)
                Destroy(enemy.gameObject);
        }

        // 연출: 섬광
        StartCoroutine(CoFlash(center));

        run?.Toast(destroyed > 0 ? $"폭발! 지형 {destroyed}칸 파괴" : "폭발! …단단한 곳이었다");
    }

    private void DestroyTileObject(Vector2Int p, TileType type)
    {
        // 규약된 부모/이름으로 콜라이더 오브젝트 제거 (Ground/Spikes/Disguised)
        string[] parents = { "Ground", "Spikes", "Disguised" };
        foreach (string parentName in parents)
        {
            GameObject parent = GameObject.Find(parentName);
            if (parent == null) continue;
            Transform tile = parent.transform.Find($"Tile_{p.x}_{p.y}");
            if (tile != null)
            {
                Destroy(tile.gameObject);
                return;
            }
        }
    }

    private IEnumerator CoFlash(Vector3 center)
    {
        GameObject flash = new GameObject("BombFlash");
        flash.transform.position = center;
        SpriteRenderer sr = flash.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("Sprites/Square");
        sr.color = new Color(1f, 0.85f, 0.5f, 0.85f);
        sr.sortingLayerName = "Objects";
        sr.sortingOrder = 30;

        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            float k = t / 0.25f;
            flash.transform.localScale = Vector3.one * Mathf.Lerp(1f, 4.2f, k);
            sr.color = new Color(1f, 0.85f, 0.5f, 0.85f * (1f - k));
            yield return null;
        }
        Destroy(flash);
    }
}
