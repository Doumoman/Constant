using System;
using UnityEngine;

namespace StarNight.Character.Live.Movement
{
    /// <summary>
    /// 라이브 이동 드라이버 구성. 이동 수치는 전부 순수 계약의 Default가
    /// 원천이며 여기는 배선 값만 둔다.
    /// </summary>
    [Serializable]
    public sealed class CharacterLiveMovementSettings
    {
        [Tooltip("지형 판정 대상 레이어(기본: Default). 플레이어는 이 마스크 밖 레이어에 둔다.")]
        [SerializeField] private LayerMask solidLayers = 1;

        [Tooltip("잠금 입력에 달리기 수정자가 없어 기본 달리기(이동 문법 기준 속도)로 구동한다.")]
        [SerializeField] private bool alwaysRun = true;

        public LayerMask SolidLayers
        {
            get { return solidLayers; }
        }

        public bool AlwaysRun
        {
            get { return alwaysRun; }
        }
    }
}
