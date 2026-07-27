using UnityEngine;

/// <summary>
/// 용암 해저드 — 플레이어가 용암에 잠기면 잠시 후 사망시킨다.
/// 작열(Heat) 효과(열충격 결정 조합 포함)가 있으면 면역.
/// 씬에 하나 배치하면 전역으로 동작한다.
/// </summary>
public class LavaHazard : MonoBehaviour
{
    [SerializeField] private float _graceTime = 0.35f; // 잠깐 스치는 것은 허용

    private PlayerFSM _player;
    private PlayerTaste _taste;
    private float _exposure;

    private void Update()
    {
        if (_player == null)
        {
            _player = FindFirstObjectByType<PlayerFSM>();
            if (_player == null) return;
            _taste = _player.GetComponent<PlayerTaste>();
        }

        if (_player.IsDead) { _exposure = 0f; return; }

        bool inLava = _player.PlayerData.isInLava;
        bool immune = _taste != null && _taste.HasHeat;

        if (inLava && !immune)
        {
            _exposure += Time.deltaTime;
            if (_exposure >= _graceTime)
            {
                SingletonManagers.Run?.Toast("용암에 삼켜졌다…");
                _player.Kill();
                _exposure = 0f;
            }
        }
        else
        {
            _exposure = 0f;
        }
    }
}
