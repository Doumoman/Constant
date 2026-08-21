#if LEGACY_DISABLED
using System.Collections.Generic;
using StarNight.Grid;
using UnityEngine;

namespace StarNight.Tools.Rope
{
    [DisallowMultipleComponent]
    public sealed class RopeSegment2D : MonoBehaviour
    {
        private static readonly HashSet<RopeSegment2D> ActiveInternal =
            new HashSet<RopeSegment2D>();

        [SerializeField] private RopeInstallation2D installation;
        [SerializeField] private Collider2D trigger;
        [SerializeField] private Vector2Int cell;
        [SerializeField] private bool isClimbable = true;

        public RopeInstallation2D Installation => installation;
        public Collider2D Trigger => trigger;
        public GridPos Cell => new GridPos(cell.x, cell.y);
        public bool IsClimbable =>
            isClimbable
            && installation != null
            && !installation.IsBroken
            && isActiveAndEnabled;
        public static IReadOnlyCollection<RopeSegment2D> ActiveSegments =>
            ActiveInternal;

        private void Reset()
        {
            trigger = GetComponent<Collider2D>();
            if (trigger != null)
            {
                trigger.isTrigger = true;
            }
        }

        private void Awake()
        {
            if (trigger == null)
            {
                trigger = GetComponent<Collider2D>();
            }

            if (trigger != null)
            {
                trigger.isTrigger = true;
            }
        }

        private void OnEnable()
        {
            ActiveInternal.Add(this);
        }

        private void OnDisable()
        {
            ActiveInternal.Remove(this);
        }

        private void OnDestroy()
        {
            ActiveInternal.Remove(this);
        }

        public void Configure(
            RopeInstallation2D owner,
            GridPos targetCell,
            Collider2D configuredTrigger = null)
        {
            installation = owner;
            cell = new Vector2Int(targetCell.X, targetCell.Y);
            trigger = configuredTrigger != null
                ? configuredTrigger
                : GetComponent<Collider2D>();
            if (trigger != null)
            {
                trigger.isTrigger = true;
            }

            isClimbable = true;
        }

        public void DisableImmediately()
        {
            isClimbable = false;
            if (trigger != null)
            {
                trigger.enabled = false;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryBreakFromFire(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryBreakFromFire(other);
        }

        private void TryBreakFromFire(Collider2D other)
        {
            if (installation == null || installation.IsBroken || other == null)
            {
                return;
            }

            RopeFireSource2D fire = other.GetComponentInParent<RopeFireSource2D>();
            if (fire != null && fire.IsBurning)
            {
                installation.Break(RopeDamageKind.Fire, fire);
            }
        }
    }
}

#endif
