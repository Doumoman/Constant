using StarNight.Character.Live.Cameras;
using StarNight.Character.Live.Player;
using StarNight.Character.Live.Run;
using StarNight.Character.MapIntegration;
using StarNight.Character.RoomTransition;
using UnityEngine;

namespace StarNight.Character.Live.Rooms
{
    /// <summary>
    /// 방 전환 드라이버. 플레이어 위치를 고정 스텝마다 샘플해 CHAR03_02
    /// 카메라룸 전환 정책(경계+hysteresis)에 평가를 위임하고, 전환 요청은
    /// 선언 루트 소비자를 거쳐 세션/카메라에 반영한다. 루트 거부 시 정책
    /// 활성 방을 세션 방으로 재정착시켜 정책-세션 일관성을 유지한다.
    /// 입력·속도·플레이어 위치는 일절 변조하지 않는다.
    /// </summary>
    public sealed class CharacterLiveRoomTransitionDriver : MonoBehaviour
    {
        [SerializeField] private CharacterLiveRunBootstrap bootstrap;
        [SerializeField] private CharacterLivePlayerRig playerRig;
        [SerializeField] private CharacterLiveManualRouteSource routeSource;
        [SerializeField] private CharacterLiveCameraRoomDriver cameraDriver;

        private readonly CharacterLiveRouteTransitionConsumer consumer =
            new CharacterLiveRouteTransitionConsumer();

        private CharacterCameraRoomTransitionPolicy policy;
        private bool anchored;

        public CharacterLiveRouteTransitionConsumer Consumer
        {
            get { return consumer; }
        }

        public CharacterRoomTransitionDecision LastDecision { get; private set; }

        private void Awake()
        {
            if (routeSource == null)
            {
                Debug.LogWarning(
                    "CharacterLiveRoomTransitionDriver: routeSource 미배선.", this);
                return;
            }

            var gate = new CharacterRoomBoundaryGate(routeSource.ReadinessSource);
            policy = new CharacterCameraRoomTransitionPolicy(
                gate, CharacterRoomTransitionSettings.Default);
        }

        private void FixedUpdate()
        {
            if (policy == null || bootstrap == null || playerRig == null
                || !bootstrap.IsRunStarted)
            {
                return;
            }

            // 최초 정착: 스폰 셀의 방을 활성 방으로 + 카메라 초기 스냅.
            if (!anchored)
            {
                var spawn = bootstrap.Session.SpawnRequest;
                policy.SetActiveRoom(spawn.StartCell);
                if (cameraDriver != null)
                {
                    cameraDriver.MoveToRoom(spawn.StartRoomId);
                }

                anchored = true;
            }

            CharacterRoomTransitionResult result =
                policy.Evaluate(playerRig.Body.position);
            LastDecision = result.Decision;

            if (!result.HasRequest)
            {
                return;
            }

            CharacterRoomTransitionRequest request = result.Request;
            if (consumer.TryConsume(
                in request,
                routeSource.DeclaredEdges,
                routeSource.ReadinessSource,
                bootstrap.Session))
            {
                if (cameraDriver != null)
                {
                    cameraDriver.MoveToRoom(bootstrap.Session.CurrentRoomId);
                }

                return;
            }

            // 루트 거부(미선언 등): 전환은 일어나지 않은 것 — 정책 활성 방을
            // 세션의 현재 방으로 재정착시켜 상태 일관성을 유지한다.
            policy.SetActiveRoom(
                CharacterLiveRoomCenterResolver.GetRoomAnchorTile(
                    bootstrap.Session.CurrentRoomId));
        }
    }
}
