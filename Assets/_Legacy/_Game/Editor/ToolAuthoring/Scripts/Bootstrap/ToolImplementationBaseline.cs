#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.ToolAuthoring.Editor
{
    [Serializable]
    public sealed class LegacyToolCodeRecord
    {
        public string AssetPath;
        public string Status;
        public string IsolationRule;
    }

    public sealed class ToolImplementationBaseline : ScriptableObject
    {
        public string Milestone;
        public string CoreStandardVersion;
        public string MapHarnessVersion;
        public string ToolHarnessVersion;
        public string InputActionAssetPath;
        public string InputActionBackupPath;
        public string InputActionSha256;
        public string InputActionBackupSha256;
        public string[] RequiredLayers;
        public string[] RequiredUnityTags;
        public string[] CapturedLayers;
        public string[] CapturedUnityTags;
        public string[] PendingLayerAssignments;
        public LegacyToolCodeRecord[] DisabledLegacyToolCode;
        public string AssemblyBoundaryStatus;
        public string LayerMigrationStatus;
        public string GeneratedUtc;
    }
}

#endif
