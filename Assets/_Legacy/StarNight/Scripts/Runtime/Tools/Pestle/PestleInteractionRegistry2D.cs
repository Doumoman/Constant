#if LEGACY_DISABLED
using System.Collections.Generic;
using StarNight.Grid;
using UnityEngine;

namespace StarNight.Tools.Pestle
{
    [DisallowMultipleComponent]
    public sealed class PestleInteractionRegistry2D : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour[] initialTargets;

        private readonly List<IPestleTarget2D> targets =
            new List<IPestleTarget2D>();
        private readonly List<IPestleTarget2D> matchingTargets =
            new List<IPestleTarget2D>();

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

        public bool Register(IPestleTarget2D target)
        {
            if (target == null
                || target.PestleTargetObject == null
                || targets.Contains(target))
            {
                return false;
            }

            targets.Add(target);
            return true;
        }

        public bool Unregister(IPestleTarget2D target)
        {
            return target != null && targets.Remove(target);
        }

        public int ApplyStrike(
            GridPos cell,
            PestleStrikeContext context,
            List<PestleReactionRecord> output)
        {
            matchingTargets.Clear();
            for (int index = targets.Count - 1; index >= 0; index--)
            {
                IPestleTarget2D target = targets[index];
                if (target == null || target.PestleTargetObject == null)
                {
                    targets.RemoveAt(index);
                    continue;
                }

                if (target.CanReceivePestle && target.PestleCell == cell)
                {
                    matchingTargets.Add(target);
                }
            }

            matchingTargets.Sort(CompareTargets);
            int reactionCount = 0;
            for (int index = 0; index < matchingTargets.Count; index++)
            {
                IPestleTarget2D target = matchingTargets[index];
                PestleReactionKind reaction =
                    target.TryReceivePestle(context);
                if (reaction == PestleReactionKind.None)
                {
                    continue;
                }

                output?.Add(new PestleReactionRecord(cell, target, reaction));
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
                if (initialTargets[index] is IPestleTarget2D target)
                {
                    Register(target);
                }
            }
        }

        private static int CompareTargets(
            IPestleTarget2D left,
            IPestleTarget2D right)
        {
            int priority = left.PestlePriority.CompareTo(right.PestlePriority);
            if (priority != 0)
            {
                return priority;
            }

            int leftId = left.PestleTargetObject != null
                ? left.PestleTargetObject.GetInstanceID()
                : int.MaxValue;
            int rightId = right.PestleTargetObject != null
                ? right.PestleTargetObject.GetInstanceID()
                : int.MaxValue;
            return leftId.CompareTo(rightId);
        }
    }
}

#endif
