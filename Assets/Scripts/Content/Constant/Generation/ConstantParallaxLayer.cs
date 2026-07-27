using UnityEngine;

/// <summary>
/// 원거리 배경 시차 — 맵 중심을 앵커로 하는 가상 원거리 평면을 재현한다.
/// 매 프레임 배경을 카메라 위치와 앵커 사이의 ratio 지점에 두면,
/// 원근 카메라로 거리 (플레이평면거리/ratio) 에 놓인 평면을 보는 것과 화면상 동일하다.
/// (URP 2D 렌더러가 SpriteShape 지형을 perspective 카메라에서 그리지 못해
///  실제 원근 대신 동일 수식의 팔로우로 입체감을 낸다)
/// </summary>
public class ConstantParallaxLayer : MonoBehaviour
{
    private Transform _cam;
    private Vector2 _anchor;       // 가상 평면의 월드 앵커 (맵 중심)
    private float _ratioX = 0.04f; // 플레이평면거리 20 / 가상 배경거리 500 — 작을수록 멀다
    private float _ratioY = 0.04f; // 세로는 더 강하게 따라붙게(값↓) 따로 조절 가능
    private float _z;

    public void Init(Vector2 anchor, float ratio) => Init(anchor, ratio, ratio);

    public void Init(Vector2 anchor, float ratioX, float ratioY)
    {
        _anchor = anchor;
        _ratioX = Mathf.Clamp01(ratioX);
        _ratioY = Mathf.Clamp01(ratioY);
        _z = transform.position.z;
    }

    private void LateUpdate()
    {
        if (_cam == null)
        {
            var main = Camera.main;
            if (main == null) return;
            _cam = main.transform;
        }

        Vector3 c = _cam.position;
        transform.position = new Vector3(
            c.x + (_anchor.x - c.x) * _ratioX,
            c.y + (_anchor.y - c.y) * _ratioY,
            _z);
    }
}
