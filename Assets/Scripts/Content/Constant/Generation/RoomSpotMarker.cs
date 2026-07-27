using UnityEngine;

/// <summary>
/// 룸 라이브러리 씬의 콘텐츠 자리 마커 — 노드/아이템/밸브/이벤트가 앉을 후보 지점.
/// isEnemy 면 순찰 몹 스폰 후보.
/// </summary>
public class RoomSpotMarker : MonoBehaviour
{
    public bool isEnemy;

    private void OnDrawGizmos()
    {
        Gizmos.color = isEnemy ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.35f);
    }
}
