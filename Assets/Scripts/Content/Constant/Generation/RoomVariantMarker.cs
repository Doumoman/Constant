using UnityEngine;

/// <summary>
/// 룸 라이브러리 씬의 방 마커 — transform.position 이 방 원점(좌하단 그리드),
/// 베이크가 이 마커 기준으로 (width x height) 영역의 타일을 수확한다.
/// </summary>
public class RoomVariantMarker : MonoBehaviour
{
    public ConstantRoomType roomType;
    public int width;
    public int height;

    public Vector2Int Origin => new Vector2Int(
        Mathf.RoundToInt(transform.position.x),
        Mathf.RoundToInt(transform.position.y));

    private void OnDrawGizmos()
    {
        Gizmos.color = roomType switch
        {
            ConstantRoomType.MainH => new Color(1f, 0.3f, 0.3f, 0.6f),
            ConstantRoomType.MainV => new Color(1f, 0.55f, 0.2f, 0.6f),
            _ => new Color(0.3f, 0.5f, 1f, 0.6f),
        };
        Vector3 o = new Vector3(Origin.x, Origin.y, 0f);
        Gizmos.DrawWireCube(o + new Vector3(width * 0.5f, height * 0.5f), new Vector3(width, height, 0.1f));
    }
}
