#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Tools.Core
{
    public enum ToolAimMode
    {
        Facing,
        UpOrFacing,
        DownAutomatic,
        Toggle,
    }

    public enum ToolFailureFeedback
    {
        None,
        EmptySpace,
        InvalidTarget,
        NoResource,
        Blocked,
        MetalFail,
    }

    [Serializable]
    public sealed class ToolActionProfile
    {
        [Min(0f)] public float WindupSeconds;
        [Min(0f)] public float ImpactSeconds;
        [Min(0f)] public float ActiveSeconds;
        [Min(0f)] public float RecoverySeconds;
        [Range(0f, 1f)] public float MovementMultiplier = 1f;
        public ToolAimMode AimMode = ToolAimMode.Facing;
        public string AnimatorTrigger;
        public AudioClip ActionSfx;
        public GameObject ActionVfx;

        public float TotalSeconds => Mathf.Max(
            ImpactSeconds,
            WindupSeconds + ActiveSeconds + RecoverySeconds);
    }
}

#endif
