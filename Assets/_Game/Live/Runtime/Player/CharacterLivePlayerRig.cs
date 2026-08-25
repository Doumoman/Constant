using StarNight.Character.Input;
using StarNight.Character.Live.Input;
using UnityEngine;

namespace StarNight.Character.Live.Player
{
    /// <summary>
    /// 라이브 플레이어 조립 바인딩 표면. L01_03+ 소비자가 Rigidbody2D·
    /// Collider2D·입력 공급자를 찾고 고정 스텝 입력 스냅샷을 읽는 진입점이다.
    /// 이 컴포넌트는 바인딩만 소유한다 — 이동/스폰/요청 소비/판정 없음
    /// (이동 조립은 L01_03 소관, 판정은 순수 캐릭터 계약 소유).
    /// </summary>
    public sealed class CharacterLivePlayerRig : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private CapsuleCollider2D bodyCollider;
        [SerializeField] private CharacterLiveInputSource inputSource;

        public Rigidbody2D Body
        {
            get { return body; }
        }

        public CapsuleCollider2D BodyCollider
        {
            get { return bodyCollider; }
        }

        public CharacterLiveInputSource InputSource
        {
            get { return inputSource; }
        }

        /// <summary>세 바인딩이 전부 연결되어 있으면 true.</summary>
        public bool IsBound
        {
            get { return body != null && bodyCollider != null && inputSource != null; }
        }

        /// <summary>고정 스텝 입력 소비 위임(누적 에지 포함, 소비 시 에지 소거).</summary>
        public CharacterInputSnapshot ConsumeFixedSnapshot(long physicsTick)
        {
            return inputSource.ConsumeFixedSnapshot(physicsTick);
        }

        /// <summary>같은 GameObject의 구성 요소로 바인딩을 채운다(에디터 조립용).</summary>
        public void BindLocalComponents()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<CapsuleCollider2D>();
            inputSource = GetComponent<CharacterLiveInputSource>();
        }

        private void Reset()
        {
            BindLocalComponents();
        }
    }
}
