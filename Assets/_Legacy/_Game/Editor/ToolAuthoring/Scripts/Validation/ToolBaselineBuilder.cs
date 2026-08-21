#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace StarNight.ToolAuthoring.Editor
{
    public static class ToolBaselineBuilder
    {
        private static readonly string[] RuntimeAssemblyPaths =
        {
            "Assets/_Game/Interaction/Runtime/Game.Interaction.Runtime.asmdef",
            "Assets/_Game/Tools/Runtime/Game.Tools.Runtime.asmdef",
            "Assets/_Game/WorldObjects/Runtime/Game.WorldObjects.Runtime.asmdef",
            "Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef"
        };

        [MenuItem("Tools/Star Night/Tool 00/Build Baseline")]
        public static void BuildFromMenu()
        {
            ToolImplementationBaseline baseline = Build();
            string[] errors = Validate(baseline);
            if (errors.Length == 0)
            {
                Debug.Log("[TOOL-00] Baseline fixed. Layer migration remains non-destructive and pending where listed.");
                Selection.activeObject = baseline;
                return;
            }

            Debug.LogError("[TOOL-00] Baseline validation failed:\n" + string.Join("\n", errors));
        }

        public static ToolImplementationBaseline Build()
        {
            EnsureParentFolder(ToolBaselineContract.InputActionBackupPath);
            EnsureParentFolder(ToolBaselineContract.BaselineAssetPath);
            EnsureParentFolder(ToolBaselineContract.ReportAssetPath);
            EnsureInputBackup();

            ToolImplementationBaseline baseline =
                AssetDatabase.LoadAssetAtPath<ToolImplementationBaseline>(
                    ToolBaselineContract.BaselineAssetPath);
            if (baseline == null)
            {
                baseline = ScriptableObject.CreateInstance<ToolImplementationBaseline>();
                AssetDatabase.CreateAsset(baseline, ToolBaselineContract.BaselineAssetPath);
            }

            string[] capturedLayers = CaptureLayers();
            string[] capturedTags = InternalEditorUtility.tags
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            baseline.Milestone = ToolBaselineContract.Milestone;
            baseline.CoreStandardVersion = ToolBaselineContract.CoreStandardVersion;
            baseline.MapHarnessVersion = ToolBaselineContract.MapHarnessVersion;
            baseline.ToolHarnessVersion = ToolBaselineContract.ToolHarnessVersion;
            baseline.InputActionAssetPath = ToolBaselineContract.InputActionAssetPath;
            baseline.InputActionBackupPath = ToolBaselineContract.InputActionBackupPath;
            baseline.InputActionSha256 = ComputeAssetSha256(ToolBaselineContract.InputActionAssetPath);
            baseline.InputActionBackupSha256 = ComputeAssetSha256(ToolBaselineContract.InputActionBackupPath);
            baseline.RequiredLayers = (string[])ToolBaselineContract.RequiredLayers.Clone();
            baseline.RequiredUnityTags = (string[])ToolBaselineContract.RequiredUnityTags.Clone();
            baseline.CapturedLayers = capturedLayers;
            baseline.CapturedUnityTags = capturedTags;
            baseline.PendingLayerAssignments = ToolBaselineContract.RequiredLayers
                .Except(capturedLayers, StringComparer.Ordinal)
                .ToArray();
            baseline.DisabledLegacyToolCode = ToolBaselineContract.LegacyToolCodePaths
                .Select(path => new LegacyToolCodeRecord
                {
                    AssetPath = path,
                    Status = "InactiveForNewHarness",
                    IsolationRule = "No assembly or source dependency from Assets/_Game"
                })
                .ToArray();
            baseline.AssemblyBoundaryStatus = "IsolatedFromLegacyAssemblyCSharp";
            baseline.LayerMigrationStatus = baseline.PendingLayerAssignments.Length == 0
                ? "Applied"
                : "FrozenContract_MigrationPendingExplicitApproval";
            baseline.GeneratedUtc = DateTime.UtcNow.ToString("O");

            EditorUtility.SetDirty(baseline);
            AssetDatabase.SaveAssets();
            WriteReport(baseline, Validate(baseline));
            AssetDatabase.ImportAsset(ToolBaselineContract.ReportAssetPath, ImportAssetOptions.ForceUpdate);
            return baseline;
        }

        public static string[] Validate(ToolImplementationBaseline baseline)
        {
            var errors = new List<string>();
            if (baseline == null)
            {
                return new[] { "Baseline asset is missing." };
            }

            if (baseline.Milestone != ToolBaselineContract.Milestone
                || baseline.CoreStandardVersion != ToolBaselineContract.CoreStandardVersion
                || baseline.MapHarnessVersion != ToolBaselineContract.MapHarnessVersion
                || baseline.ToolHarnessVersion != ToolBaselineContract.ToolHarnessVersion)
            {
                errors.Add("Approved document versions are not fixed to the TOOL-00 contract.");
            }

            if (!AssetExists(ToolBaselineContract.InputActionAssetPath)
                || !AssetExists(ToolBaselineContract.InputActionBackupPath))
            {
                errors.Add("Input Action source or immutable TOOL-00 backup is missing.");
            }
            else if (!string.Equals(
                         baseline.InputActionSha256,
                         baseline.InputActionBackupSha256,
                         StringComparison.Ordinal))
            {
                errors.Add("Input Action backup hash differs from its TOOL-00 source hash.");
            }

            string[] tags = InternalEditorUtility.tags;
            foreach (string requiredTag in ToolBaselineContract.RequiredUnityTags)
            {
                if (!tags.Contains(requiredTag, StringComparer.Ordinal))
                {
                    errors.Add("Required Unity tag is missing: " + requiredTag);
                }
            }

            if (!ToolBaselineContract.RequiredLayers.SequenceEqual(baseline.RequiredLayers))
            {
                errors.Add("Required physics layer contract was modified.");
            }

            if (!ToolBaselineContract.RequiredUnityTags.SequenceEqual(baseline.RequiredUnityTags))
            {
                errors.Add("Required Unity tag contract was modified.");
            }

            ValidateAssemblyBoundaries(errors);
            return errors.ToArray();
        }

        private static void EnsureInputBackup()
        {
            if (!AssetExists(ToolBaselineContract.InputActionAssetPath))
            {
                throw new FileNotFoundException(
                    "Input Action Asset was not found.",
                    ToolBaselineContract.InputActionAssetPath);
            }

            if (AssetExists(ToolBaselineContract.InputActionBackupPath))
            {
                string sourceHash = ComputeAssetSha256(ToolBaselineContract.InputActionAssetPath);
                string backupHash = ComputeAssetSha256(ToolBaselineContract.InputActionBackupPath);
                if (!string.Equals(sourceHash, backupHash, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "The immutable TOOL-00 input backup exists but differs from the current source.");
                }

                return;
            }

            File.Copy(
                ToFullPath(ToolBaselineContract.InputActionAssetPath),
                ToFullPath(ToolBaselineContract.InputActionBackupPath),
                false);
            AssetDatabase.ImportAsset(ToolBaselineContract.InputActionBackupPath);
        }

        private static void ValidateAssemblyBoundaries(List<string> errors)
        {
            foreach (string assetPath in RuntimeAssemblyPaths)
            {
                string fullPath = ToFullPath(assetPath);
                if (!File.Exists(fullPath))
                {
                    errors.Add("Runtime assembly definition is missing: " + assetPath);
                    continue;
                }

                string json = File.ReadAllText(fullPath);
                if (json.IndexOf("Assembly-CSharp", StringComparison.OrdinalIgnoreCase) >= 0
                    || json.IndexOf("Editor", StringComparison.Ordinal) >= 0)
                {
                    errors.Add("Runtime assembly crosses a legacy/editor boundary: " + assetPath);
                }
            }
        }

        private static string[] CaptureLayers()
        {
            var layers = new List<string>();
            for (int index = 0; index < 32; index++)
            {
                string layer = LayerMask.LayerToName(index);
                if (!string.IsNullOrEmpty(layer))
                {
                    layers.Add(layer);
                }
            }

            return layers.ToArray();
        }

        private static string ComputeAssetSha256(string assetPath)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(File.ReadAllBytes(ToFullPath(assetPath)));
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }

        private static void WriteReport(ToolImplementationBaseline baseline, string[] errors)
        {
            var report = new ToolBaselineReport
            {
                Milestone = baseline.Milestone,
                BaselinePassed = errors.Length == 0,
                ValidationErrors = errors,
                CoreStandardVersion = baseline.CoreStandardVersion,
                MapHarnessVersion = baseline.MapHarnessVersion,
                ToolHarnessVersion = baseline.ToolHarnessVersion,
                InputActionAssetPath = baseline.InputActionAssetPath,
                InputActionBackupPath = baseline.InputActionBackupPath,
                InputActionSha256 = baseline.InputActionSha256,
                RequiredLayers = baseline.RequiredLayers,
                CapturedLayers = baseline.CapturedLayers,
                PendingLayerAssignments = baseline.PendingLayerAssignments,
                RequiredUnityTags = baseline.RequiredUnityTags,
                CapturedUnityTags = baseline.CapturedUnityTags,
                DisabledLegacyToolCode = baseline.DisabledLegacyToolCode,
                AssemblyBoundaryStatus = baseline.AssemblyBoundaryStatus,
                LayerMigrationStatus = baseline.LayerMigrationStatus,
                GeneratedUtc = baseline.GeneratedUtc
            };
            File.WriteAllText(
                ToFullPath(ToolBaselineContract.ReportAssetPath),
                JsonUtility.ToJson(report, true));
        }

        private static bool AssetExists(string assetPath)
        {
            return File.Exists(ToFullPath(assetPath));
        }

        private static string ToFullPath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static void EnsureParentFolder(string assetPath)
        {
            string fullPath = ToFullPath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        }

        [Serializable]
        private sealed class ToolBaselineReport
        {
            public string Milestone;
            public bool BaselinePassed;
            public string[] ValidationErrors;
            public string CoreStandardVersion;
            public string MapHarnessVersion;
            public string ToolHarnessVersion;
            public string InputActionAssetPath;
            public string InputActionBackupPath;
            public string InputActionSha256;
            public string[] RequiredLayers;
            public string[] CapturedLayers;
            public string[] PendingLayerAssignments;
            public string[] RequiredUnityTags;
            public string[] CapturedUnityTags;
            public LegacyToolCodeRecord[] DisabledLegacyToolCode;
            public string AssemblyBoundaryStatus;
            public string LayerMigrationStatus;
            public string GeneratedUtc;
        }
    }
}

#endif
