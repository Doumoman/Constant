using StarNight.Character.Live.Rooms;
using StarNight.Character.MapIntegration;
using UnityEngine;

namespace StarNight.Character.Live.Cameras
{
    /// <summary>
    /// 카메라룸 드라이버. 수락된 목표 방의 중심으로 씬 카메라를 결정적으로
    /// 스냅한다(z 유지). 속도·입력·인벤토리·체력·런 상태·세이브·오디오·
    /// 애니메이션을 일절 변조하지 않는다. Cinemachine 불사용.
    /// </summary>
    public sealed class CharacterLiveCameraRoomDriver : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;

        public CharacterRoomId CurrentCameraRoom { get; private set; }
        public bool HasCameraRoom { get; private set; }

        /// <summary>수락된 방으로 카메라 스냅(결정적 — 방 중심, z 유지).</summary>
        public void MoveToRoom(CharacterRoomId room)
        {
            if (targetCamera == null)
            {
                return;
            }

            Vector2 center = CharacterLiveRoomCenterResolver.GetRoomCenter(room);
            Vector3 position = targetCamera.transform.position;
            targetCamera.transform.position = new Vector3(
                center.x, center.y, position.z);

            CurrentCameraRoom = room;
            HasCameraRoom = true;
        }
    }
}
