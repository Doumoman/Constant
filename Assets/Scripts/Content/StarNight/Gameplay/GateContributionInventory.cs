using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class GateContributionInventory : MonoBehaviour
    {
        [SerializeField] private List<GateContribution> pending = new();

        public IReadOnlyList<GateContribution> Pending => pending;
        public int Count => pending.Count;
        public event Action Changed;

        public void ResetForChapter()
        {
            pending.Clear();
            Changed?.Invoke();
        }

        public bool TryAdd(GateContribution contribution)
        {
            if (contribution == null ||
                string.IsNullOrWhiteSpace(contribution.id) ||
                string.IsNullOrWhiteSpace(contribution.routeId) ||
                ContainsRoute(contribution.routeId) ||
                pending.Exists(item => item.id == contribution.id))
            {
                return false;
            }

            pending.Add(contribution.Copy());
            Changed?.Invoke();
            return true;
        }

        public bool TryTakeByRoute(string routeId, out GateContribution contribution)
        {
            int index = pending.FindIndex(item => item.routeId == routeId);
            if (index < 0)
            {
                contribution = null;
                return false;
            }

            contribution = pending[index];
            pending.RemoveAt(index);
            Changed?.Invoke();
            return true;
        }

        public bool RemoveByRoute(string routeId)
        {
            int index = pending.FindIndex(item => item.routeId == routeId);
            if (index < 0)
            {
                return false;
            }

            pending.RemoveAt(index);
            Changed?.Invoke();
            return true;
        }

        public bool ContainsRoute(string routeId) =>
            !string.IsNullOrWhiteSpace(routeId) && pending.Exists(item => item.routeId == routeId);
    }
}
