using System;
using UnityEngine;

namespace StarNight.Map.WorldGeneration.Diagnostics
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("WorldGen/Map Generation Progress Scene Adapter")]
    public sealed class MapGenerationProgressSceneAdapter : MonoBehaviour
    {
        public enum OverlayTab { Topology, Sites, Biomes }

        [SerializeField] private string seedText = "0x0123456789ABCDF9";
        [SerializeField, Range(0, 99)] private int attemptOrdinal = 24;
        [SerializeField] private OverlayTab selectedTab = OverlayTab.Biomes;
        [SerializeField] private WorldTopologyOverlay topologyOverlay;
        [SerializeField] private SiteReservationOverlay siteOverlay;
        [SerializeField] private BiomePatchOverlay biomeOverlay;

        [NonSerialized] private int generationCalls;
        [NonSerialized] private string status = "Ready. No generation has run.";

        public string SeedText { get => seedText; set => seedText = value ?? string.Empty; }
        public int AttemptOrdinal { get => attemptOrdinal; set => attemptOrdinal = Mathf.Clamp(value, 0, 99); }
        public OverlayTab SelectedTab => selectedTab;
        public WorldTopologyOverlay TopologyOverlay => topologyOverlay;
        public SiteReservationOverlay SiteOverlay => siteOverlay;
        public BiomePatchOverlay BiomeOverlay => biomeOverlay;
        public int GenerationCalls => generationCalls;
        public string Status => status;

        public void Configure(
            WorldTopologyOverlay topology,
            SiteReservationOverlay sites,
            BiomePatchOverlay biomes)
        {
            topologyOverlay = topology ?? throw new ArgumentNullException(nameof(topology));
            siteOverlay = sites ?? throw new ArgumentNullException(nameof(sites));
            biomeOverlay = biomes ?? throw new ArgumentNullException(nameof(biomes));
            ShowBiomes();
        }

        public void PublishStatus(string value, bool generationAttempted)
        {
            status = value ?? string.Empty;
            if (generationAttempted) generationCalls++;
        }

        public void ShowTopology() => Select(OverlayTab.Topology);
        public void ShowSites() => Select(OverlayTab.Sites);
        public void ShowBiomes() => Select(OverlayTab.Biomes);

        public void Clear()
        {
            RequireOverlays();
            topologyOverlay.ClearSnapshot();
            siteOverlay.ClearSnapshot();
            biomeOverlay.ClearSnapshot();
            status = "Cleared.";
        }

        private void Select(OverlayTab tab)
        {
            RequireOverlays();
            selectedTab = tab;
            topologyOverlay.enabled = tab == OverlayTab.Topology;
            siteOverlay.enabled = tab == OverlayTab.Sites;
            biomeOverlay.enabled = tab == OverlayTab.Biomes;
        }

        private void RequireOverlays()
        {
            if (topologyOverlay == null || siteOverlay == null || biomeOverlay == null)
                throw new InvalidOperationException("All three overlay references are required.");
        }
    }
}
