using System.Collections.Generic;
using StarNight.Character.Movement;
using UnityEngine;

namespace StarNight.Character.Live.Player
{
    /// <summary>
    /// 라이브 플레이어 리그의 결정적 무부작용 검증(읽기 전용). 위반 문자열
    /// 목록만 반환하며 어떤 상태도 바꾸지 않는다. 잠금 규격의 원천은
    /// CharacterCapsuleGeometry.Default(0.72×0.90)다.
    /// </summary>
    public static class CharacterLivePlayerRigValidator
    {
        private const float SizeTolerance = 0.0001f;

        public static List<string> Validate(CharacterLivePlayerRig rig)
        {
            var violations = new List<string>();

            if (rig == null)
            {
                violations.Add("rig 없음");
                return violations;
            }

            if (!rig.IsBound)
            {
                violations.Add("바인딩 미완성(body/collider/inputSource)");
            }

            if (rig.Body != null)
            {
                if (rig.Body.bodyType != RigidbodyType2D.Kinematic)
                {
                    violations.Add("Rigidbody2D bodyType이 Kinematic이 아님: "
                        + rig.Body.bodyType);
                }

                if (rig.Body.gravityScale != 0f)
                {
                    violations.Add("Rigidbody2D gravityScale이 0이 아님: "
                        + rig.Body.gravityScale);
                }
            }

            if (rig.BodyCollider != null)
            {
                var expected = CharacterCapsuleGeometry.Default;
                Vector2 size = rig.BodyCollider.size;

                if (Mathf.Abs(size.x - expected.Width) > SizeTolerance
                    || Mathf.Abs(size.y - expected.Height) > SizeTolerance)
                {
                    violations.Add("캡슐 크기가 잠금 규격(0.72×0.90)과 다름: "
                        + size.x + "×" + size.y);
                }

                if (rig.BodyCollider.direction != CapsuleDirection2D.Vertical)
                {
                    violations.Add("캡슐 방향이 Vertical이 아님");
                }
            }

            // 런타임(Awake 후)에는 IsReady, 에디트 모드 자산 검증에는
            // HasActionsAsset이 준비 기준이다.
            if (rig.InputSource != null
                && !rig.InputSource.IsReady
                && !rig.InputSource.HasActionsAsset)
            {
                violations.Add("입력 공급자 미준비(actionsAsset 미지정)");
            }

            // 금지 구성 요소 부재(잠금: Animator/오디오/UI 권위 없음).
            if (rig.GetComponent<Animator>() != null)
            {
                violations.Add("금지 구성 요소 존재: Animator");
            }

            if (rig.GetComponent<AudioSource>() != null)
            {
                violations.Add("금지 구성 요소 존재: AudioSource");
            }

            return violations;
        }
    }
}
