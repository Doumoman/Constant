#if LEGACY_DISABLED
using StarNight.Grid;
using UnityEngine;

namespace StarNight.Tools.Pestle
{
    public abstract class PestleTargetCell2D : MonoBehaviour, IPestleTarget2D
    {
        [SerializeField] private PestleInteractionRegistry2D registry;
        [SerializeField] private GridWorld gridWorld;
        [SerializeField] private bool useFixedCell;
        [SerializeField] private Vector2Int fixedCell;
        [SerializeField] private int pestlePriority;

        public GridPos PestleCell
        {
            get
            {
                if (useFixedCell)
                {
                    return new GridPos(fixedCell.x, fixedCell.y);
                }

                return gridWorld != null
                    ? gridWorld.WorldToCell(transform.position)
                    : new GridPos(
                        Mathf.FloorToInt(transform.position.x),
                        Mathf.FloorToInt(transform.position.y));
            }
        }

        public int PestlePriority => pestlePriority;
        public abstract bool CanReceivePestle { get; }
        public UnityEngine.Object PestleTargetObject => this;

        protected virtual void Awake()
        {
            if (gridWorld == null)
            {
                gridWorld = FindFirstObjectByType<GridWorld>();
            }

            if (registry == null)
            {
                registry = FindFirstObjectByType<PestleInteractionRegistry2D>();
            }
        }

        protected virtual void OnEnable()
        {
            registry?.Register(this);
        }

        protected virtual void OnDisable()
        {
            registry?.Unregister(this);
        }

        public void ConfigureCell(
            PestleInteractionRegistry2D targetRegistry,
            GridWorld world,
            GridPos cell,
            bool lockToCell = true,
            int priority = 0)
        {
            registry?.Unregister(this);
            registry = targetRegistry;
            gridWorld = world;
            fixedCell = new Vector2Int(cell.X, cell.Y);
            useFixedCell = lockToCell;
            pestlePriority = priority;
            if (isActiveAndEnabled)
            {
                registry?.Register(this);
            }
        }

        public void SetCellForTests(GridPos cell)
        {
            fixedCell = new Vector2Int(cell.X, cell.Y);
            useFixedCell = true;
        }

        public abstract PestleReactionKind TryReceivePestle(
            PestleStrikeContext context);
    }
}

#endif
