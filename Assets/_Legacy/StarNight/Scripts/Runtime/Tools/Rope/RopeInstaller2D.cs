#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Grid;
using StarNight.Tiles;
using UnityEngine;

namespace StarNight.Tools.Rope
{
    [DisallowMultipleComponent]
    public sealed class RopeInstaller2D : MonoBehaviour
    {
        public const int DefaultStartingStock = 4;
        public const int DefaultMaximumLength =
            RopePlacementSolver.DefaultMaximumLength;

        [SerializeField] private GridWorld gridWorld;
        [SerializeField] private TileMutationService tileMutationService;
        [SerializeField] private RopeSegment2D segmentPrefab;
        [SerializeField] private Transform installationParent;
        [SerializeField] private Sprite fallbackSegmentSprite;
        [SerializeField, Range(1, DefaultMaximumLength)]
        private int maximumLength = DefaultMaximumLength;
        [SerializeField, Range(0.08f, 0.8f)]
        private float fallbackTriggerWidth = 0.24f;

        public event Action<RopeInstallation2D> Installed;

        public GridWorld GridWorld => gridWorld;
        public TileMutationService TileMutationService => tileMutationService;
        public int MaximumLength => maximumLength;

        private void Awake()
        {
            ResolveDependencies();
        }

        private void OnValidate()
        {
            maximumLength = Mathf.Clamp(
                maximumLength,
                1,
                DefaultMaximumLength);
            fallbackTriggerWidth = Mathf.Clamp(
                fallbackTriggerWidth,
                0.08f,
                0.8f);
        }

        public void Configure(
            GridWorld world,
            TileMutationService mutationService,
            RopeSegment2D configuredSegmentPrefab = null,
            Transform configuredParent = null,
            int configuredMaximumLength = DefaultMaximumLength,
            Sprite configuredFallbackSprite = null)
        {
            gridWorld = world;
            tileMutationService = mutationService;
            segmentPrefab = configuredSegmentPrefab;
            installationParent = configuredParent;
            maximumLength = Mathf.Clamp(
                configuredMaximumLength,
                1,
                DefaultMaximumLength);
            fallbackSegmentSprite = configuredFallbackSprite;
        }

        public bool TryInstall(
            GridPos useCell,
            out RopeInstallation2D installation,
            out RopeInstallFailure failure)
        {
            ResolveDependencies();
            installation = null;
            if (!RopePlacementSolver.TryBuildPlan(
                    gridWorld,
                    tileMutationService,
                    useCell,
                    maximumLength,
                    out RopeInstallPlan plan,
                    out failure))
            {
                return false;
            }

            GameObject root = new GameObject(
                $"Rope_{plan.UseCell.X}_{plan.UseCell.Y}");
            root.transform.SetParent(
                installationParent != null ? installationParent : transform,
                false);

            installation = root.AddComponent<RopeInstallation2D>();
            List<RopeSegment2D> segments =
                new List<RopeSegment2D>(plan.ClimbableCells.Count);

            for (int index = 0; index < plan.ClimbableCells.Count; index++)
            {
                GridPos cell = plan.ClimbableCells[index];
                RopeSegment2D segment = CreateSegment(root.transform, cell);
                segments.Add(segment);
            }

            installation.Configure(gridWorld, plan, segments);
            for (int index = 0; index < segments.Count; index++)
            {
                segments[index].Configure(
                    installation,
                    plan.ClimbableCells[index],
                    segments[index].GetComponent<Collider2D>());
            }

            failure = RopeInstallFailure.None;
            Installed?.Invoke(installation);
            return true;
        }

        public bool TryBuildPlanForTests(
            GridPos useCell,
            out RopeInstallPlan plan,
            out RopeInstallFailure failure)
        {
            ResolveDependencies();
            return RopePlacementSolver.TryBuildPlan(
                gridWorld,
                tileMutationService,
                useCell,
                maximumLength,
                out plan,
                out failure);
        }

        private RopeSegment2D CreateSegment(
            Transform parent,
            GridPos cell)
        {
            Vector2 position = gridWorld.CellToWorldCenter(cell);
            RopeSegment2D segment;

            if (segmentPrefab != null)
            {
                segment = Instantiate(
                    segmentPrefab,
                    position,
                    Quaternion.identity,
                    parent);
            }
            else
            {
                GameObject segmentObject = new GameObject(
                    $"Segment_{cell.X}_{cell.Y}");
                segmentObject.transform.SetParent(parent, false);
                segmentObject.transform.position = position;

                BoxCollider2D box = segmentObject.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(fallbackTriggerWidth, 0.96f);

                if (fallbackSegmentSprite != null)
                {
                    SpriteRenderer renderer =
                        segmentObject.AddComponent<SpriteRenderer>();
                    renderer.sprite = fallbackSegmentSprite;
                    renderer.drawMode = SpriteDrawMode.Sliced;
                    renderer.size = new Vector2(
                        Mathf.Max(fallbackTriggerWidth, 0.12f),
                        1f);
                }

                segment = segmentObject.AddComponent<RopeSegment2D>();
            }

            segment.transform.position = new Vector3(
                position.x,
                position.y,
                segment.transform.position.z);
            return segment;
        }

        private void ResolveDependencies()
        {
            if (gridWorld == null)
            {
                gridWorld = GetComponentInParent<GridWorld>();
                if (gridWorld == null)
                {
                    gridWorld = FindFirstObjectByType<GridWorld>();
                }
            }

            if (tileMutationService == null)
            {
                tileMutationService = GetComponentInParent<TileMutationService>();
                if (tileMutationService == null)
                {
                    tileMutationService =
                        FindFirstObjectByType<TileMutationService>();
                }
            }
        }
    }
}

#endif
