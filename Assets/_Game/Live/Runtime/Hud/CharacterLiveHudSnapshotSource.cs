using StarNight.Character.Live.Presentation;
using StarNight.Character.Live.Run;
using StarNight.Character.MapIntegration;
using StarNight.Character.Presentation;

namespace StarNight.Character.Live.Hud
{
    /// <summary>
    /// 라이브 런 상태 → HUD 뷰 모델 투영(순수·결정적). 체력/폭탄/로프/런
    /// 상태는 캐릭터 계약 CharacterHudSnapshot.FromRunState에 위임하고,
    /// 방/피드백만 라이브 표면(세션 현재 방, 피드백 로그)에서 읽는다.
    /// 세션 미시작·부재 시 안정 빈 값을 반환한다.
    /// </summary>
    public static class CharacterLiveHudSnapshotSource
    {
        public static CharacterLiveHudSnapshot Project(
            CharacterLiveRunSession session,
            CharacterLiveFeedbackLog feedbackLog)
        {
            string feedback =
                feedbackLog == null ? string.Empty : feedbackLog.LatestText;

            if (session == null || !session.IsRunStarted)
            {
                var empty = CharacterLiveHudSnapshot.Empty;
                return new CharacterLiveHudSnapshot(
                    false, 0, 0, false, 0, 0,
                    empty.RunStatusLabel, empty.RoomLabel, feedback);
            }

            CharacterHudSnapshot hud =
                CharacterHudSnapshot.FromRunState(session.RunState);

            return new CharacterLiveHudSnapshot(
                true,
                hud.CurrentHealth,
                hud.MaxHealth,
                hud.IsInvulnerable,
                hud.BombCount,
                hud.RopeCount,
                hud.RunStatus.ToString(),
                RoomLabelOf(session.CurrentRoomId),
                feedback);
        }

        private static string RoomLabelOf(CharacterRoomId roomId)
        {
            return "S" + roomId.Sector.X + "," + roomId.Sector.Y
                + " C" + roomId.MicroChunk.X + "," + roomId.MicroChunk.Y;
        }
    }
}
