#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Interaction.State;
using UnityEngine;

namespace StarNight.Tools.Rope
{
    [DisallowMultipleComponent]
    public sealed class RopeInstallationRuntime : MonoBehaviour, IRuntimeRoomStateParticipant
    {
        [SerializeField] private RopeDefinition definition;
        [SerializeField] private string runtimeId;
        [SerializeField] private RopeAnchorKind anchorKind;
        [SerializeField] private Vector2Int anchorCell;
        [SerializeField] private RopeAnchorRuntime anchor;
        [SerializeField] private List<RopeSegmentRuntime> segments = new List<RopeSegmentRuntime>();
        [SerializeField] private float installationSeconds;
        [SerializeField] private float installationElapsed;

        public event Action<RopeInstallationRuntime> InstallationCompleted;
        public event Action<RopeInstallationRuntime, int> RopeBroken;

        public string RuntimeId => runtimeId ?? string.Empty;
        public Vector2Int AnchorCell => anchorCell;
        public RopeAnchorKind AnchorKind => anchorKind;
        public IReadOnlyList<RopeSegmentRuntime> Segments => segments;
        public bool IsInstallationComplete => installationElapsed >= installationSeconds;
        public string RuntimeRoomStateId => string.IsNullOrWhiteSpace(runtimeId)
            ? "rope:" + gameObject.name
            : runtimeId;

        private void OnEnable() => RopeInstallationRegistry.Register(this);
        private void OnDisable() => RopeInstallationRegistry.Unregister(this);

        private void Update()
        {
            if (IsInstallationComplete) return;
            installationElapsed = Mathf.Min(installationSeconds, installationElapsed + Time.deltaTime);
            RefreshInstallationVisibility();
            if (IsInstallationComplete) InstallationCompleted?.Invoke(this);
        }

        public bool Initialize(
            RopeDefinition configuredDefinition,
            RopePlacementPlan plan,
            GameObject segmentPrefab,
            GameObject ceilingAnchorPrefab,
            GameObject starKnotPrefab,
            Vector2 gridOrigin,
            long actionId)
        {
            if (configuredDefinition == null || segmentPrefab == null || plan.SegmentCells.Length == 0)
            {
                return false;
            }

            definition = configuredDefinition;
            runtimeId = $"rope:{actionId}:{plan.AnchorCell.x}:{plan.AnchorCell.y}";
            anchorKind = plan.AnchorKind;
            anchorCell = plan.AnchorCell;
            GameObject selectedAnchorPrefab = plan.AnchorKind == RopeAnchorKind.StarKnot
                ? starKnotPrefab
                : ceilingAnchorPrefab;
            if (selectedAnchorPrefab != null)
            {
                GameObject anchorObject = Instantiate(selectedAnchorPrefab, transform);
                anchorObject.name = plan.AnchorKind == RopeAnchorKind.StarKnot ? "StarKnotAnchor" : "RopeAnchor";
                anchorObject.transform.position = CellWorld(plan.AnchorCell, gridOrigin);
                anchor = anchorObject.GetComponent<RopeAnchorRuntime>();
                anchor?.Configure(this, plan.AnchorKind);
            }

            segments.Clear();
            for (int index = 0; index < plan.SegmentCells.Length; index++)
            {
                GameObject segmentObject = Instantiate(segmentPrefab, transform);
                segmentObject.name = $"RopeSegment_{index:00}";
                segmentObject.transform.position = CellWorld(plan.SegmentCells[index], gridOrigin);
                RopeSegmentRuntime segment = segmentObject.GetComponent<RopeSegmentRuntime>();
                if (segment == null) return false;
                segment.Configure(this, index, plan.SegmentCells[index]);
                segments.Add(segment);
            }

            float travelSeconds = plan.SegmentCells.Length / configuredDefinition.LaunchCellsPerSecond;
            installationSeconds = Mathf.Min(configuredDefinition.MaximumInstallSeconds, travelSeconds);
            installationElapsed = 0f;
            RefreshInstallationVisibility();
            RopeInstallationRegistry.Register(this);
            return true;
        }

        public bool BreakAt(int segmentIndex)
        {
            if (segmentIndex < 0 || segmentIndex >= segments.Count
                || segments[segmentIndex] == null || !segments[segmentIndex].IsAttached)
            {
                return false;
            }

            segments[segmentIndex].BreakImmediately();
            float fallSeconds = definition != null
                ? definition.FallenSegmentSeconds
                : RopeDefinition.ApprovedFallenSegmentSeconds;
            for (int index = segmentIndex + 1; index < segments.Count; index++)
            {
                RopeSegmentRuntime segment = segments[index];
                if (segment != null && segment.IsAttached) segment.BeginFalling(fallSeconds);
            }
            RopeBroken?.Invoke(this, segmentIndex);
            return true;
        }

        public bool RestoreFromSnapshot(
            RopeDefinition configuredDefinition,
            RopeSnapshot snapshot,
            GameObject segmentPrefab,
            GameObject ceilingAnchorPrefab,
            GameObject starKnotPrefab,
            Vector2 gridOrigin)
        {
            if (snapshot == null || snapshot.RemainingSegmentCells == null) return false;
            var plan = new RopePlacementPlan(
                snapshot.AnchorKind,
                snapshot.AnchorCell,
                snapshot.RemainingSegmentCells.ToArray());
            if (!Initialize(
                configuredDefinition,
                plan,
                segmentPrefab,
                ceilingAnchorPrefab,
                starKnotPrefab,
                gridOrigin,
                0))
            {
                return false;
            }
            runtimeId = snapshot.RuntimeId;
            CompleteInstallationImmediately();
            return true;
        }

        public RopeSnapshot CaptureSnapshot()
        {
            var snapshot = new RopeSnapshot
            {
                RuntimeId = runtimeId,
                AnchorKind = anchorKind,
                AnchorCell = anchorCell,
            };
            for (int index = 0; index < segments.Count; index++)
            {
                RopeSegmentRuntime segment = segments[index];
                if (segment != null && segment.IsAttached) snapshot.RemainingSegmentCells.Add(segment.Cell);
            }
            return snapshot;
        }

        public bool RestoreSnapshotInPlace(RopeSnapshot snapshot)
        {
            if (snapshot == null || snapshot.RemainingSegmentCells == null) return false;
            runtimeId = snapshot.RuntimeId;
            anchorKind = snapshot.AnchorKind;
            anchorCell = snapshot.AnchorCell;
            var remaining = new HashSet<Vector2Int>(snapshot.RemainingSegmentCells);
            for (int index = 0; index < segments.Count; index++)
            {
                RopeSegmentRuntime segment = segments[index];
                if (segment == null) continue;
                if (remaining.Contains(segment.Cell)) segment.RestoreAttached(this);
                else segment.BreakImmediately();
            }
            CompleteInstallationImmediately();
            return true;
        }

        public string CaptureRuntimeRoomState() => JsonUtility.ToJson(CaptureSnapshot());

        public void RestoreRuntimeRoomState(string payload)
        {
            if (!string.IsNullOrWhiteSpace(payload))
            {
                RestoreSnapshotInPlace(JsonUtility.FromJson<RopeSnapshot>(payload));
            }
        }

        public void CompleteInstallationImmediately()
        {
            installationElapsed = installationSeconds;
            RefreshInstallationVisibility();
        }

        private void RefreshInstallationVisibility()
        {
            int visibleCount = installationSeconds <= 0f
                ? segments.Count
                : Mathf.Clamp(
                    Mathf.CeilToInt(segments.Count * installationElapsed / installationSeconds),
                    0,
                    segments.Count);
            for (int index = 0; index < segments.Count; index++)
            {
                if (segments[index] != null) segments[index].gameObject.SetActive(index < visibleCount);
            }
        }

        private Vector2 CellWorld(Vector2Int cell, Vector2 gridOrigin)
        {
            float size = definition != null ? definition.CellSize : 1f;
            return gridOrigin + (Vector2)cell * size;
        }
    }
}

#endif
