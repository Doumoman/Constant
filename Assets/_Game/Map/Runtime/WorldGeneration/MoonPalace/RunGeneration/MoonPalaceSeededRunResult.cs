using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.MoonPalace.Visualization;
using UnityEngine;

namespace StarNight.Map.WorldGeneration.MoonPalace.RunGeneration
{
    public enum MoonPalaceSeededRunRoomRole { Start, Transit, Vertical, Split, Exit, Branch }

    public sealed class MoonPalaceSeededRunRoomRecord
    {
        public MoonPalaceSeededRunRoomRecord(string roomId, MoonPalaceSeededRunRoomRole role, int minX, int minY, int maxX, int maxY, int routeOrder)
        {
            RoomId = roomId ?? string.Empty; Role = role; MinPatternX = minX; MinPatternY = minY; MaxPatternX = maxX; MaxPatternY = maxY; RouteOrder = routeOrder;
        }
        public string RoomId { get; }
        public MoonPalaceSeededRunRoomRole Role { get; }
        public int MinPatternX { get; }
        public int MinPatternY { get; }
        public int MaxPatternX { get; }
        public int MaxPatternY { get; }
        public int RouteOrder { get; }
        public int PatternWidth => MaxPatternX - MinPatternX + 1;
        public int PatternHeight => MaxPatternY - MinPatternY + 1;
        public int TileMinX => MinPatternX * 4;
        public int TileMinY => MinPatternY * 4;
        public int TileWidth => PatternWidth * 4;
        public int TileHeight => PatternHeight * 4;
        public bool IsMain => Role != MoonPalaceSeededRunRoomRole.Branch;
        public bool Contains(PatternSlotCoordinate slot) => slot.X >= MinPatternX && slot.X <= MaxPatternX && slot.Y >= MinPatternY && slot.Y <= MaxPatternY;
        public bool ContainsTile(int x, int y) => x >= TileMinX && x < TileMinX + TileWidth && y >= TileMinY && y < TileMinY + TileHeight;
    }

    public sealed class MoonPalaceSeededRunRoomFrame
    {
        public MoonPalaceSeededRunRoomFrame(MoonPalaceSeededRunRoomRecord room)
        {
            RoomId = room.RoomId; TileMinX = room.TileMinX; TileMinY = room.TileMinY; TileWidth = room.TileWidth; TileHeight = room.TileHeight;
            Center = new Vector2(TileMinX + TileWidth * 0.5f, TileMinY + TileHeight * 0.5f);
            OrthographicSize = Mathf.Max(TileHeight * 0.5f + 2f, TileWidth * 0.5f + 2f);
        }
        public string RoomId { get; }
        public int TileMinX { get; }
        public int TileMinY { get; }
        public int TileWidth { get; }
        public int TileHeight { get; }
        public Vector2 Center { get; }
        public float OrthographicSize { get; }
    }

    public sealed class MoonPalaceSeededRunConnectorRecord
    {
        public MoonPalaceSeededRunConnectorRecord(string connectorId, string fromRoomId, string toRoomId, PatternSlotCoordinate fromSlot, PatternSlotCoordinate toSlot,
            MoonPalaceRunDirection direction, string kind, bool required, Vector2Int fromGateTile, Vector2Int toGateTile)
        {
            ConnectorId = connectorId ?? string.Empty; FromRoomId = fromRoomId ?? string.Empty; ToRoomId = toRoomId ?? string.Empty;
            FromPatternSlot = fromSlot; ToPatternSlot = toSlot; Direction = direction; Kind = kind ?? string.Empty; IsRequiredForCompletion = required;
            FromGateTile = fromGateTile; ToGateTile = toGateTile;
        }
        public string ConnectorId { get; }
        public string FromRoomId { get; }
        public string ToRoomId { get; }
        public PatternSlotCoordinate FromPatternSlot { get; }
        public PatternSlotCoordinate ToPatternSlot { get; }
        public MoonPalaceRunDirection Direction { get; }
        public string Kind { get; }
        public bool IsRequiredForCompletion { get; }
        public Vector2Int FromGateTile { get; }
        public Vector2Int ToGateTile { get; }
        public int RequiredSocketBit => MoonPalaceCameraRoomPatternComposer.RouteSocketBit;
        public bool IsReciprocal { get; internal set; }
        public bool IsOpen { get; internal set; }
    }

    public sealed class MoonPalaceSeededRunPatternPlacement
    {
        public MoonPalaceSeededRunPatternPlacement(int slotIndex, PatternSlotCoordinate slot, string roomId, string routeOwnership, MoonPalaceMicroPatternCandidate candidate, IEnumerable<MoonPalaceRunDirection> requiredDirections, int attempts)
        {
            SlotIndex = slotIndex; PatternSlot = slot; RoomId = roomId ?? string.Empty; RouteOwnership = routeOwnership ?? string.Empty;
            Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
            RequiredDirections = new ReadOnlyCollection<MoonPalaceRunDirection>((requiredDirections ?? Array.Empty<MoonPalaceRunDirection>()).Distinct().OrderBy(direction => direction).ToList());
            AttemptCount = attempts;
        }
        public int SlotIndex { get; }
        public PatternSlotCoordinate PatternSlot { get; }
        public string RoomId { get; }
        public string RouteOwnership { get; }
        public MoonPalaceMicroPatternCandidate Candidate { get; }
        public IReadOnlyList<MoonPalaceRunDirection> RequiredDirections { get; }
        public int AttemptCount { get; }
        public string Transform => "IDENTITY_NO_ROTATION";
    }

    public sealed class MoonPalaceSeededRunRouteEdge
    {
        public MoonPalaceSeededRunRouteEdge(PatternSlotCoordinate from, PatternSlotCoordinate to, string ownership, string ownerId)
        {
            From = from; To = to; Ownership = ownership ?? string.Empty; OwnerId = ownerId ?? string.Empty; Direction = DirectionFor(from, to);
        }
        public PatternSlotCoordinate From { get; }
        public PatternSlotCoordinate To { get; }
        public MoonPalaceRunDirection Direction { get; }
        public string Ownership { get; }
        public string OwnerId { get; }
        public static MoonPalaceRunDirection DirectionFor(PatternSlotCoordinate from, PatternSlotCoordinate to)
        {
            if (to.X == from.X + 1 && to.Y == from.Y) return MoonPalaceRunDirection.East;
            if (to.X == from.X - 1 && to.Y == from.Y) return MoonPalaceRunDirection.West;
            if (to.X == from.X && to.Y == from.Y + 1) return MoonPalaceRunDirection.North;
            if (to.X == from.X && to.Y == from.Y - 1) return MoonPalaceRunDirection.South;
            throw new InvalidOperationException("RUN05 routes require cardinally adjacent 4x4 pattern slots.");
        }
    }

    /// <summary>Complete deterministic course result; every open bit is copied directly from a selected audited candidate mask.</summary>
    public sealed class MoonPalaceSeededRunResult
    {
        private readonly bool[] open;
        private readonly string[] roomsByTile;
        private readonly string[] connectorsByTile;
        private readonly string[] ownershipByTile;
        private readonly IReadOnlyList<Vector2Int> routeToExit;

        internal MoonPalaceSeededRunResult(MoonPalaceSeededRunRecipe recipe, int seed, MoonPalaceMicroPatternCandidateSet candidates,
            IEnumerable<MoonPalaceSeededRunRoomRecord> rooms, IEnumerable<MoonPalaceSeededRunConnectorRecord> connectors,
            IEnumerable<MoonPalaceSeededRunPatternPlacement> placements, IEnumerable<MoonPalaceSeededRunRouteEdge> edges,
            bool[] openCells, string[] roomCells, string[] connectorCells, string[] ownershipCells, Vector2Int start, Vector2Int exit,
            IEnumerable<Vector2Int> route, string roomGraphDigest, string placementDigest, string courseDigest, int attempts)
        {
            Recipe = recipe; Seed = seed; CandidateSet = candidates;
            Rooms = ReadOnly(rooms); Connectors = ReadOnly(connectors); Placements = ReadOnly(placements); Edges = ReadOnly(edges);
            RoomFrames = ReadOnly(Rooms.OrderBy(room => room.RoomId, StringComparer.Ordinal).Select(room => new MoonPalaceSeededRunRoomFrame(room)));
            open = openCells ?? Array.Empty<bool>(); roomsByTile = roomCells ?? Array.Empty<string>(); connectorsByTile = connectorCells ?? Array.Empty<string>(); ownershipByTile = ownershipCells ?? Array.Empty<string>();
            StartTile = start; ExitTile = exit; routeToExit = ReadOnly(route); RoomGraphDigest = roomGraphDigest ?? string.Empty; PlacementDigest = placementDigest ?? string.Empty; CourseDigest = courseDigest ?? string.Empty; SelectionAttemptCount = attempts;
        }
        public MoonPalaceSeededRunRecipe Recipe { get; }
        public int Seed { get; }
        public MoonPalaceMicroPatternCandidateSet CandidateSet { get; }
        public IReadOnlyList<MoonPalaceSeededRunRoomRecord> Rooms { get; }
        public IReadOnlyList<MoonPalaceSeededRunRoomFrame> RoomFrames { get; }
        public IReadOnlyList<MoonPalaceSeededRunConnectorRecord> Connectors { get; }
        public IReadOnlyList<MoonPalaceSeededRunPatternPlacement> Placements { get; }
        public IReadOnlyList<MoonPalaceSeededRunRouteEdge> Edges { get; }
        public int TileWidth => Recipe.TileWidth;
        public int TileHeight => Recipe.TileHeight;
        public int PatternPlacementCount => Placements.Count;
        public int CandidatePoolCount => CandidateSet.Candidates.Count;
        public int RotationCount => 0;
        public int FallbackCarveCount => 0;
        public int SilentRepairCount => 0;
        public int SelectionAttemptCount { get; }
        public Vector2Int StartTile { get; }
        public Vector2Int ExitTile { get; }
        public IReadOnlyList<Vector2Int> RouteToExit => routeToExit;
        public string RoomGraphDigest { get; }
        public string PlacementDigest { get; }
        public string CourseDigest { get; }
        public MoonPalaceSeededRunValidation Validation { get; internal set; }
        public int OpenCellCount => open.Count(value => value);
        public bool IsInside(int x, int y) => x >= 0 && x < TileWidth && y >= 0 && y < TileHeight;
        public bool IsOpen(int x, int y) => IsInside(x, y) && open[(y * TileWidth) + x];
        public bool CanStep(int fromX, int fromY, int toX, int toY) => IsOpen(fromX, fromY) && IsOpen(toX, toY) && Mathf.Abs(toX - fromX) + Mathf.Abs(toY - fromY) == 1;
        public string GetRoomId(int x, int y) => IsInside(x, y) ? roomsByTile[(y * TileWidth) + x] : string.Empty;
        public string GetConnectorId(int x, int y) => IsInside(x, y) ? connectorsByTile[(y * TileWidth) + x] : string.Empty;
        public string GetRouteOwnership(int x, int y) => IsInside(x, y) ? ownershipByTile[(y * TileWidth) + x] : string.Empty;
        public IReadOnlyList<Vector2Int> FindRouteToExit() => routeToExit;
        public MoonPalaceSeededRunRoomFrame GetRoomFrame(string roomId) => RoomFrames.Single(frame => string.Equals(frame.RoomId, roomId, StringComparison.Ordinal));
        public bool TryGetConnectorTransition(string currentRoomId, Vector2Int from, Vector2Int to, out MoonPalaceSeededRunConnectorRecord connector, out string destinationRoomId)
        {
            connector = Connectors.FirstOrDefault(value =>
                (value.FromRoomId == currentRoomId && value.FromGateTile.Equals(from) && value.ToGateTile.Equals(to)) ||
                (value.ToRoomId == currentRoomId && value.ToGateTile.Equals(from) && value.FromGateTile.Equals(to)));
            if (connector == null || !connector.IsOpen) { destinationRoomId = string.Empty; return false; }
            destinationRoomId = connector.FromRoomId == currentRoomId ? connector.ToRoomId : connector.FromRoomId;
            return true;
        }
        private static IReadOnlyList<T> ReadOnly<T>(IEnumerable<T> values) => new ReadOnlyCollection<T>((values ?? Array.Empty<T>()).ToList());
    }

    /// <summary>RUN05's serialized manual marker adapter. It intentionally mirrors RUN04 inspection controls without owning production movement.</summary>
    public sealed class MoonPalaceSeededRunPreviewController : MonoBehaviour
    {
        [SerializeField] private string recipeId = "WIDE_BRANCH_RUN";
        [SerializeField] private int seed = 1924737067;
        [SerializeField] private TextMesh currentTileLabel;
        [SerializeField] private TextMesh currentRoomLabel;
        [SerializeField] private MoonPalaceSeededRunPreviewCameraController previewCamera;
        [SerializeField] private MoonPalaceSeededRunPreviewRouteGhost routeGhost;
        private MoonPalaceSeededRunResult result;
        public Vector2Int CurrentTile { get; private set; }
        public string CurrentRoomId { get; private set; }
        public string EnteredConnectorId { get; private set; }
        public int SuccessfulStepCount { get; private set; }
        public int BlockedInputCount { get; private set; }
        public MoonPalaceSeededRunResult Result => result;
        private void Start() { if (result == null) Initialize(MoonPalaceSeededRunGenerator.Generate(MoonPalaceSeededRunRecipeCatalog.Get(recipeId), seed)); }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) TryStep(0, 1);
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) TryStep(0, -1);
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) TryStep(-1, 0);
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) TryStep(1, 0);
            else if (Input.GetKeyDown(KeyCode.R)) ResetToStart();
            else if (Input.GetKeyDown(KeyCode.G) && routeGhost != null) routeGhost.SetVisible(!routeGhost.IsVisible);
        }
        public void Configure(string selectedRecipeId, int selectedSeed, TextMesh tileLabel, TextMesh roomLabel, MoonPalaceSeededRunPreviewCameraController camera, MoonPalaceSeededRunPreviewRouteGhost ghost)
        {
            recipeId = selectedRecipeId; seed = selectedSeed; currentTileLabel = tileLabel; currentRoomLabel = roomLabel; previewCamera = camera; routeGhost = ghost;
        }
        public void Initialize(MoonPalaceSeededRunResult generated)
        {
            result = generated ?? throw new ArgumentNullException(nameof(generated)); CurrentTile = result.StartTile; CurrentRoomId = result.GetRoomId(CurrentTile.x, CurrentTile.y); EnteredConnectorId = string.Empty; SuccessfulStepCount = 0; BlockedInputCount = 0;
            transform.position = new Vector3(CurrentTile.x + 0.5f, CurrentTile.y + 0.5f, -3f);
            if (previewCamera != null) previewCamera.Initialize(result, CurrentRoomId);
            if (routeGhost != null) routeGhost.Initialize(result, true);
            RefreshLabels();
        }
        public bool TryStep(int deltaX, int deltaY)
        {
            if (result == null) Initialize(MoonPalaceSeededRunGenerator.Generate(MoonPalaceSeededRunRecipeCatalog.Get(recipeId), seed));
            var next = new Vector2Int(CurrentTile.x + deltaX, CurrentTile.y + deltaY);
            if (!result.CanStep(CurrentTile.x, CurrentTile.y, next.x, next.y)) { BlockedInputCount++; RefreshLabels(); return false; }
            var prior = CurrentTile; CurrentTile = next; SuccessfulStepCount++; EnteredConnectorId = string.Empty;
            if (result.TryGetConnectorTransition(CurrentRoomId, prior, next, out var connector, out var destination)) { EnteredConnectorId = connector.ConnectorId; CurrentRoomId = destination; if (previewCamera != null) previewCamera.FocusRoom(destination); }
            transform.position = new Vector3(CurrentTile.x + 0.5f, CurrentTile.y + 0.5f, -3f); RefreshLabels(); return true;
        }
        public void ResetToStart() { if (result == null) return; CurrentTile = result.StartTile; CurrentRoomId = result.GetRoomId(CurrentTile.x, CurrentTile.y); EnteredConnectorId = string.Empty; SuccessfulStepCount = 0; transform.position = new Vector3(CurrentTile.x + 0.5f, CurrentTile.y + 0.5f, -3f); if (previewCamera != null) previewCamera.FocusRoom(CurrentRoomId); RefreshLabels(); }
        private void RefreshLabels() { if (currentTileLabel != null) currentTileLabel.text = "Tile: " + CurrentTile.x + "," + CurrentTile.y + " steps=" + SuccessfulStepCount + " blocked=" + BlockedInputCount; if (currentRoomLabel != null) currentRoomLabel.text = "Current room: " + CurrentRoomId + (string.IsNullOrEmpty(EnteredConnectorId) ? string.Empty : " via " + EnteredConnectorId); }
    }

    public sealed class MoonPalaceSeededRunPreviewCameraController : MonoBehaviour
    {
        [SerializeField] private Camera previewCamera;
        [SerializeField] private bool useShortLerp = true;
        [SerializeField] private float lerpSeconds = 0.18f;
        private MoonPalaceSeededRunResult result; private Vector3 targetPosition; private float targetSize;
        public string CurrentRoomId { get; private set; }
        public string PreviousRoomId { get; private set; }
        private void Awake() { if (previewCamera == null) previewCamera = GetComponent<Camera>(); }
        private void LateUpdate() { if (previewCamera == null || result == null) return; var factor = useShortLerp ? Mathf.Clamp01(Time.unscaledDeltaTime / Mathf.Max(0.01f, lerpSeconds)) : 1f; previewCamera.transform.position = Vector3.Lerp(previewCamera.transform.position, targetPosition, factor); previewCamera.orthographicSize = Mathf.Lerp(previewCamera.orthographicSize, targetSize, factor); }
        public void Configure(Camera camera, bool shortLerp, float seconds) { previewCamera = camera; useShortLerp = shortLerp; lerpSeconds = Mathf.Max(0.01f, seconds); }
        public void Initialize(MoonPalaceSeededRunResult generated, string startRoomId) { result = generated ?? throw new ArgumentNullException(nameof(generated)); Focus(startRoomId, true); }
        public void FocusRoom(string roomId) => Focus(roomId, false);
        private void Focus(string roomId, bool immediate) { if (result == null || string.IsNullOrEmpty(roomId)) return; var frame = result.GetRoomFrame(roomId); PreviousRoomId = CurrentRoomId ?? string.Empty; CurrentRoomId = roomId; targetPosition = new Vector3(frame.Center.x, frame.Center.y, -10f); targetSize = frame.OrthographicSize; if (previewCamera != null && (immediate || !useShortLerp)) { previewCamera.transform.position = targetPosition; previewCamera.orthographicSize = targetSize; } }
    }

    public sealed class MoonPalaceSeededRunPreviewRouteGhost : MonoBehaviour
    {
        private IReadOnlyList<Vector2Int> route = Array.Empty<Vector2Int>();
        public bool IsVisible { get; private set; }
        public IReadOnlyList<Vector2Int> Route => route;
        public void Initialize(MoonPalaceSeededRunResult generated, bool visible) { route = generated == null ? Array.Empty<Vector2Int>() : generated.FindRouteToExit(); SetVisible(visible); }
        public void SetVisible(bool visible) { IsVisible = visible; var renderer = GetComponent<Renderer>(); if (renderer != null) renderer.enabled = visible; }
    }
}
