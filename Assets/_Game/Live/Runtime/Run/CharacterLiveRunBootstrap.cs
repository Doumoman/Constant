using StarNight.Character.Integration;
using StarNight.Character.Live.Movement;
using StarNight.Character.Live.Player;
using UnityEngine;

namespace StarNight.Character.Live.Run
{
    /// <summary>
    /// 라이브 런 부트스트랩(씬 진입점). 수동 시작 소스 → CHAR06_01 스폰
    /// 정책 → 스폰 요청 1회 소비 → 세션 시작 순서로 조립한다. 실패는
    /// 예외 대신 경고 로그 + 미시작 상태로 남는다.
    /// L02_02에서 수동 소스가 MAP 어댑터로 교체되어도 이 조립은 불변이다.
    /// </summary>
    public sealed class CharacterLiveRunBootstrap : MonoBehaviour
    {
        private const int PlayerActorId = 1;

        [SerializeField] private CharacterLiveManualStartSource startSource;
        [SerializeField] private CharacterLivePlayerRig playerRig;
        [SerializeField] private CharacterLiveMovementDriver movementDriver;

        private readonly CharacterLiveSpawnConsumer spawnConsumer =
            new CharacterLiveSpawnConsumer();

        private readonly CharacterLiveRunSession session =
            new CharacterLiveRunSession();

        /// <summary>L02/L03 소비자용 세션 표면.</summary>
        public CharacterLiveRunSession Session
        {
            get { return session; }
        }

        public bool IsRunStarted
        {
            get { return session.IsRunStarted; }
        }

        private void Start()
        {
            if (startSource == null || playerRig == null)
            {
                Debug.LogWarning(
                    "CharacterLiveRunBootstrap: startSource/playerRig 미배선.", this);
                return;
            }

            CharacterGeneratedMapStartSnapshot startSnapshot;
            if (!startSource.TryCreateStartSnapshot(out startSnapshot))
            {
                return;
            }

            CharacterPlayerSpawnRequest spawnRequest;
            CharacterIntegrationDiagnostic diagnostic;
            if (!CharacterSpawnIntegrationPolicy.TryCreateSpawnRequest(
                in startSnapshot, PlayerActorId,
                out spawnRequest, out diagnostic))
            {
                Debug.LogWarning(
                    "CharacterLiveRunBootstrap: 스폰 요청 거부 — "
                    + diagnostic.Kind + " " + diagnostic.Subject, this);
                return;
            }

            if (!spawnConsumer.TryConsume(in spawnRequest, playerRig, movementDriver))
            {
                Debug.LogWarning(
                    "CharacterLiveRunBootstrap: 스폰 소비 실패(중복 또는 리그 미바인딩).",
                    this);
                return;
            }

            session.TryStartRun(in spawnRequest);
        }
    }
}
