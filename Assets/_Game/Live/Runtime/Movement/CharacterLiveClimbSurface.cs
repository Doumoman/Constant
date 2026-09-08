using UnityEngine;

namespace StarNight.Character.Live.Movement
{
    /// <summary>
    /// RMAP04의 통과형 climb axis. 사다리와 기둥은 같은 Player motor를
    /// 공유하며, 이 marker 자체는 지지면이나 일반 solid를 만들지 않는다.
    /// </summary>
    public sealed class CharacterLiveClimbSurface : MonoBehaviour
    {
        public enum SurfaceKind
        {
            Ladder,
            Pole
        }

        [SerializeField] private SurfaceKind kind = SurfaceKind.Ladder;
        [SerializeField] private Collider2D axisTrigger;

        public SurfaceKind Kind { get { return kind; } }
        public Collider2D AxisTrigger { get { return axisTrigger; } }
        public float AxisWorldX
        {
            get { return axisTrigger != null ? axisTrigger.bounds.center.x : transform.position.x; }
        }

        public bool IsUsable
        {
            get { return axisTrigger != null && axisTrigger.enabled && axisTrigger.isTrigger; }
        }

        public void Configure(SurfaceKind surfaceKind, Collider2D triggerCollider)
        {
            kind = surfaceKind;
            axisTrigger = triggerCollider;
            if (axisTrigger != null)
            {
                axisTrigger.isTrigger = true;
            }
        }
    }
}
