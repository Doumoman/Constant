using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class StarNightHazard : MonoBehaviour
    {
        [SerializeField] private int damage = 1;
        [SerializeField] private string reason = "사고에 휘말렸다.";

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.relativeVelocity.sqrMagnitude < 16f)
            {
                return;
            }
            if (collision.collider.GetComponentInParent<StarNightPlayerAgent>() is { } player)
            {
                player.TakeDamage(damage, reason);
                StarNightRunState.Instance?.AccidentReport.Add(name, "세게 부딪혀", "별 한 칸을 떨어뜨렸다");
            }
        }
    }
}
