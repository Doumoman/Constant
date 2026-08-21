#if LEGACY_DISABLED
using StarNight.Tools.Bomb;
using StarNight.Tools.Rope;
using UnityEngine;

namespace StarNight.ToolAuthoring
{
    [ExecuteAlways]
    public sealed class ToolInteractionLabController : MonoBehaviour
    {
        [SerializeField] private GameObject playerTestRig;
        [SerializeField] private bool allToolsGranted;
        [SerializeField] private bool showCellOverlay = true;
        [SerializeField] private bool showImpactScore;
        [SerializeField] private bool showInteractionPriority;
        [SerializeField] private bool showPlacementCandidates;

        public bool AllToolsGranted => allToolsGranted;
        public bool ShowCellOverlay => showCellOverlay;
        public bool ShowImpactScore => showImpactScore;
        public bool ShowInteractionPriority => showInteractionPriority;
        public bool ShowPlacementCandidates => showPlacementCandidates;

        public void Configure(GameObject player) => playerTestRig = player;

        public void GiveAllTools()
        {
            allToolsGranted = true;
            SetBombRope99();
        }

        public void SetBombRope99()
        {
            GameObject player = playerTestRig != null ? playerTestRig : GameObject.Find("PlayerTestRig");
            player?.GetComponent<BombInventoryState>()?.Restore(99);
            player?.GetComponent<RopeInventoryState>()?.Restore(99);
        }

        public void ToggleCellOverlay() => showCellOverlay = !showCellOverlay;
        public void ToggleImpactScore() => showImpactScore = !showImpactScore;
        public void ToggleInteractionPriority() => showInteractionPriority = !showInteractionPriority;
        public void TogglePlacementCandidates() => showPlacementCandidates = !showPlacementCandidates;

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(16f, 16f, 340f, 150f), GUI.skin.box);
            GUILayout.Label("TOOL-05 · Tool Interaction Lab");
            GUILayout.Label($"Tools: {(allToolsGranted ? "ALL" : "Stations")}");
            GUILayout.Label($"Cells {showCellOverlay} · Impact {showImpactScore}");
            GUILayout.Label($"Priority {showInteractionPriority} · Placement {showPlacementCandidates}");
            GUILayout.EndArea();
        }
    }
}

#endif
