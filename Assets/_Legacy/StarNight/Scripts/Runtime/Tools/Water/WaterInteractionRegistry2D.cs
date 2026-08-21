#if LEGACY_DISABLED
using System.Collections.Generic;
using StarNight.Grid;
using UnityEngine;

namespace StarNight.Tools.Water
{
    [DisallowMultipleComponent]
    public sealed class WaterInteractionRegistry2D : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour[] initialTargets;

        private readonly List<IWaterReactive2D> targets =
            new List<IWaterReactive2D>();
        private readonly List<IWaterReactive2D> matchingTargets =
            new List<IWaterReactive2D>();

        public int RegisteredCount => targets.Count;

        private void Awake()
        {
            RegisterInitialTargets();
        }

        public void Configure(MonoBehaviour[] targetsToRegister)
        {
            initialTargets = targetsToRegister;
            targets.Clear();
            RegisterInitialTargets();
        }

        public bool Register(IWaterReactive2D target)
        {
            if (target == null
                || target.WaterTargetObject == null
                || targets.Contains(target))
            {
                return false;
            }

            targets.Add(target);
            return true;
        }

        public bool Unregister(IWaterReactive2D target)
        {
            return target != null && targets.Remove(target);
        }

        public int ApplyWater(
            GridPos cell,
            WaterApplication application,
            List<WaterReactionRecord> output)
        {
            matchingTargets.Clear();
            for (int index = targets.Count - 1; index >= 0; index--)
            {
                IWaterReactive2D target = targets[index];
                if (target == null || target.WaterTargetObject == null)
                {
                    targets.RemoveAt(index);
                    continue;
                }

                if (target.CanReceiveWater && target.WaterCell == cell)
                {
                    matchingTargets.Add(target);
                }
            }

            matchingTargets.Sort(CompareTargets);
            int reactionCount = 0;
            for (int index = 0; index < matchingTargets.Count; index++)
            {
                IWaterReactive2D target = matchingTargets[index];
                WaterReactionKind reaction = target.TryReceiveWater(application);
                if (reaction == WaterReactionKind.None)
                {
                    continue;
                }

                output?.Add(new WaterReactionRecord(cell, target, reaction));
                reactionCount++;
            }

            return reactionCount;
        }

        private void RegisterInitialTargets()
        {
            if (initialTargets == null)
            {
                return;
            }

            for (int index = 0; index < initialTargets.Length; index++)
            {
                if (initialTargets[index] is IWaterReactive2D target)
                {
                    Register(target);
                }
            }
        }

        private static int CompareTargets(
            IWaterReactive2D left,
            IWaterReactive2D right)
        {
            int priority = left.WaterPriority.CompareTo(right.WaterPriority);
            if (priority != 0)
            {
                return priority;
            }

            int leftId = left.WaterTargetObject != null
                ? left.WaterTargetObject.GetInstanceID()
                : int.MaxValue;
            int rightId = right.WaterTargetObject != null
                ? right.WaterTargetObject.GetInstanceID()
                : int.MaxValue;
            return leftId.CompareTo(rightId);
        }
    }
}

#endif
