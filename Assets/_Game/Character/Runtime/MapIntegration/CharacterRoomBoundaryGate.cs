using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.MapIntegration
{
    /// <summary>
    /// 방 경계 준비 게이트. 출발/도착 타일의 방을 비교해 통과 가능 여부
    /// 판정만 반환한다. 카메라 이동, 플레이어 스냅, 입력 억제, 속도 재작성,
    /// hysteresis는 수행하지 않는다(카메라 전환·KEEP 적용·hysteresis는
    /// CHAR03_02 소관). 시그니처가 좌표만 받으므로 입력 스냅샷과 속도를
    /// 구조적으로 변조할 수 없다.
    /// </summary>
    public sealed class CharacterRoomBoundaryGate
    {
        private readonly ICharacterRoomReadinessSource readinessSource;

        public CharacterRoomBoundaryGate(ICharacterRoomReadinessSource readinessSource)
        {
            if (readinessSource == null)
            {
                throw new ArgumentNullException(nameof(readinessSource));
            }

            this.readinessSource = readinessSource;
        }

        public CharacterBoundaryCrossDecision Evaluate(
            WorldTileCoord fromTile,
            WorldTileCoord toTile)
        {
            CharacterRoomId fromRoom = CharacterRoomId.FromWorldTile(fromTile);
            CharacterRoomId toRoom = CharacterRoomId.FromWorldTile(toTile);

            if (fromRoom.Equals(toRoom))
            {
                return CharacterBoundaryCrossDecision.NotABoundaryCrossing;
            }

            bool isReady;
            if (!readinessSource.TryGetRoomReadiness(toRoom, out isReady))
            {
                return CharacterBoundaryCrossDecision.BlockedMissingRoom;
            }

            return isReady
                ? CharacterBoundaryCrossDecision.Allowed
                : CharacterBoundaryCrossDecision.BlockedUnpreparedRoom;
        }

        /// <summary>이동 허용 여부(방 내부 이동 포함).</summary>
        public static bool MayCross(CharacterBoundaryCrossDecision decision)
        {
            return decision == CharacterBoundaryCrossDecision.Allowed
                || decision == CharacterBoundaryCrossDecision.NotABoundaryCrossing;
        }
    }
}
