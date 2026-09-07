using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Map.WorldGeneration.MoonPalace.RunGeneration
{
    /// <summary>Toggleable verification visual. It exposes the BFS route and never edits the preview grid.</summary>
    public sealed class MoonPalaceRunPreviewRouteGhost : MonoBehaviour
    {
        [SerializeField] private GameObject visualRoot;
        private IReadOnlyList<Vector2Int> route = Array.Empty<Vector2Int>();

        public IReadOnlyList<Vector2Int> Route => route;
        public bool IsVisible { get; private set; }
        public int RouteLength => route.Count;

        private void Awake()
        {
            if (visualRoot == null) visualRoot = gameObject;
        }

        private void Start()
        {
            if (route.Count == 0) Initialize(MoonPalaceRunPreviewGrid.CreateDefault(), true);
        }

        public void ConfigureVisualRoot(GameObject root) => visualRoot = root == null ? gameObject : root;

        public void Initialize(MoonPalaceRunPreviewGrid grid, bool visible)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            route = grid.FindRouteToExit();
            SetVisible(visible);
        }

        public void SetVisible(bool visible)
        {
            IsVisible = visible;
            if (visualRoot != null && visualRoot != gameObject) visualRoot.SetActive(visible);
            else if (visualRoot != null)
            {
                var renderer = visualRoot.GetComponent<Renderer>();
                if (renderer != null) renderer.enabled = visible;
            }
        }
    }
}
