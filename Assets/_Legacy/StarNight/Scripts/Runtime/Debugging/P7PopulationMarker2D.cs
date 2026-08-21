#if LEGACY_DISABLED
using System;
using StarNight.Population.P7;
using UnityEngine;

namespace StarNight.Debugging
{
    [DisallowMultipleComponent]
    public sealed class P7PopulationMarker2D : MonoBehaviour
    {
        [SerializeField] private int nodeId;
        [SerializeField] private string prefabId;
        [SerializeField] private string slotId;
        [SerializeField] private Vector2Int worldCell;
        [SerializeField] private P7PopulationKind kind;
        [SerializeField] private P7PopulationRoute route;
        [SerializeField] private int value;
        [SerializeField] private int riskScore;
        [SerializeField] private P7EnemyKind enemyKind;
        [SerializeField] private P7TrapKind trapKind;

        public int NodeId => nodeId;
        public string PrefabId => prefabId;
        public string SlotId => slotId;
        public Vector2Int WorldCell => worldCell;
        public P7PopulationKind Kind => kind;
        public P7PopulationRoute Route => route;
        public int Value => value;
        public int RiskScore => riskScore;
        public P7EnemyKind EnemyKind => enemyKind;
        public P7TrapKind TrapKind => trapKind;

        public void Configure(P7PopulationPlacement placement)
        {
            if (placement == null)
            {
                throw new ArgumentNullException(nameof(placement));
            }

            nodeId = placement.NodeId;
            prefabId = placement.PrefabId;
            slotId = placement.SlotId;
            worldCell = placement.WorldCell;
            kind = placement.Kind;
            route = placement.Route;
            value = placement.Value;
            riskScore = placement.RiskScore;
            enemyKind = placement.EnemyKind;
            trapKind = placement.TrapKind;
        }
    }
}

#endif
