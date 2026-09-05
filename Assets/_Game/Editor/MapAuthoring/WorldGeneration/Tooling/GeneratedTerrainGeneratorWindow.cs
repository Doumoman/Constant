using System;
using System.Globalization;
using System.IO;
using StarNight.Map.WorldGeneration.Tooling;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.WorldGeneration.Tooling
{
    public sealed class GeneratedTerrainGeneratorWindow : EditorWindow
    {
        public const string MenuPath = "Tools/MapDesign/Generated Terrain Generator";
        public const string WindowTitle = "Generated Terrain Generator";
        public const string GeneratorVersion = "map20-tooling-v1";
        public const string DataVersion = "map-v2-authoring";

        private static int openInvocationCount;
        private static int runRequestCount;

        [NonSerialized] private GeneratedTerrainRunCoordinator coordinator;
        [NonSerialized] private Vector2 scroll;
        [NonSerialized] private string lastUiError = string.Empty;
        [SerializeField] private string seedText = "731";
        [SerializeField] private GeneratedTerrainRunScope scope = GeneratedTerrainRunScope.Pattern;
        [SerializeField] private string patternId = "auto";
        [SerializeField] private int sectorX = 6;
        [SerializeField] private int sectorY = 6;
        [SerializeField] private bool worldConfirmed;

        public static int OpenInvocationCount => openInvocationCount;
        public static int RunRequestCount => runRequestCount;
        public bool IsRunActive => coordinator != null && coordinator.IsRunning;
        public GeneratedTerrainRunArtifact LastArtifact => coordinator == null
            ? null
            : coordinator.LastArtifact;

        [MenuItem(MenuPath)]
        public static GeneratedTerrainGeneratorWindow Open()
        {
            openInvocationCount++;
            var window = GetWindow<GeneratedTerrainGeneratorWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(650f, 480f);
            window.EnsureCoordinator();
            window.Show();
            return window;
        }

        public static void ResetInvocationDiagnostics()
        {
            openInvocationCount = 0;
            runRequestCount = 0;
        }

        private void OnEnable()
        {
            titleContent = new GUIContent(WindowTitle);
            minSize = new Vector2(650f, 480f);
            EnsureCoordinator();
        }

        private void EnsureCoordinator()
        {
            if (coordinator == null) coordinator = new GeneratedTerrainRunCoordinator();
        }

        private void OnGUI()
        {
            EnsureCoordinator();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Deterministic run identity", EditorStyles.boldLabel);
            seedText = EditorGUILayout.TextField("Seed", seedText);
            var tokens = GeneratedTerrainRunScopeCatalog.Tokens;
            var nextScope = EditorGUILayout.Popup("Scope", (int)scope,
                new System.Collections.Generic.List<string>(tokens).ToArray());
            scope = GeneratedTerrainRunScopeCatalog.Scopes[nextScope];

            if (scope == GeneratedTerrainRunScope.Pattern)
                patternId = EditorGUILayout.TextField("Biome/profile pattern", patternId);
            if (scope == GeneratedTerrainRunScope.Sector || scope == GeneratedTerrainRunScope.OneRing)
            {
                sectorX = EditorGUILayout.IntField("Sector X", sectorX);
                sectorY = EditorGUILayout.IntField("Sector Y", sectorY);
            }
            if (scope == GeneratedTerrainRunScope.World)
                worldConfirmed = EditorGUILayout.ToggleLeft(
                    "I confirm this World-scoped tooling run (not production approval).", worldConfirmed);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generator version", GeneratorVersion);
            EditorGUILayout.LabelField("Data version", DataVersion);
            EditorGUILayout.SelectableLabel("Input hash  " + CurrentInputDigest(),
                EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.SelectableLabel("MAP19 exit  " +
                GeneratedTerrainRunPreconditions.Map19ExitDigest,
                EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(IsRunActive || !CanRunWorld()))
                {
                    if (GUILayout.Button("Run")) Execute(false);
                    if (GUILayout.Button("Dry-run plan")) Execute(true);
                }
                using (new EditorGUI.DisabledScope(IsRunActive || !coordinator.RollbackAvailable))
                {
                    if (GUILayout.Button("Rollback")) Rollback();
                }
            }

            DrawLastRun();
            EditorGUILayout.EndScrollView();
        }

        private bool CanRunWorld() => scope != GeneratedTerrainRunScope.World || worldConfirmed;

        private string CurrentInputDigest()
        {
            ulong seed;
            if (!ulong.TryParse(seedText, NumberStyles.None, CultureInfo.InvariantCulture, out seed))
                seed = 0;
            return GeneratedTerrainRunRequest.ComputeInputDigest(seed, scope,
                GeneratorVersion, DataVersion, patternId, sectorX, sectorY);
        }

        private void Execute(bool dryRun)
        {
            ulong seed;
            if (!ulong.TryParse(seedText, NumberStyles.None, CultureInfo.InvariantCulture, out seed))
            {
                lastUiError = "Seed must be an unsigned integer.";
                return;
            }

            runRequestCount++;
            lastUiError = string.Empty;
            try
            {
                var runId = "window-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ",
                    CultureInfo.InvariantCulture) + "-" + runRequestCount.ToString("D4",
                    CultureInfo.InvariantCulture);
                var request = new GeneratedTerrainRunRequest(runId, scope, seed,
                    GeneratorVersion, DataVersion, CurrentInputDigest(),
                    GeneratedTerrainRunCoordinator.RelativeOutputRoot + "/workspaces",
                    patternId, sectorX, sectorY, worldConfirmed, dryRun);

                // MAP20_01 only supplies orchestration and rollback. Existing/future generator
                // adapters provide an in-memory mutation plan without moving solver logic here.
                coordinator.Run(request, GeneratedTerrainRunPlan.DryRun());
            }
            catch (Exception exception)
            {
                lastUiError = exception.Message;
            }
            Repaint();
        }

        private void Rollback()
        {
            lastUiError = string.Empty;
            try
            {
                coordinator.RollbackLastRun();
            }
            catch (Exception exception)
            {
                lastUiError = exception.Message;
            }
            Repaint();
        }

        private void DrawLastRun()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Last run", EditorStyles.boldLabel);
            if (!string.IsNullOrEmpty(lastUiError))
                EditorGUILayout.HelpBox(lastUiError, MessageType.Error);
            var artifact = LastArtifact;
            if (artifact == null)
            {
                EditorGUILayout.HelpBox("No generation has been started by opening this window.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Status", artifact.pass_state);
            EditorGUILayout.SelectableLabel("Artifact digest  " + artifact.canonical_digest,
                EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            if (!string.Equals(artifact.pass_state, "PASS", StringComparison.Ordinal))
            {
                EditorGUILayout.HelpBox(artifact.failure_owner + ": " + artifact.failure_reason,
                    MessageType.Error);
                EditorGUILayout.SelectableLabel(artifact.replay_reference, EditorStyles.textField,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open output folder") &&
                    Directory.Exists(coordinator.LastRunDirectory))
                    EditorUtility.RevealInFinder(coordinator.LastRunDirectory);
                if (GUILayout.Button("Copy replay reference"))
                    EditorGUIUtility.systemCopyBuffer = artifact.replay_reference;
            }
        }
    }
}
