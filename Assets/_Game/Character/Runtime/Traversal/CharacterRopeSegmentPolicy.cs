using System.Collections.Generic;
using StarNight.Character.Equipment;
using StarNight.Character.MapIntegration;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.Traversal
{
    /// <summary>
    /// 결정적 수직 로프 세그먼트 생성(순수). origin 셀에서 위로 한 열을
    /// 따라 올라가며, 월드 경계·고체 차단 셀·중앙 최대 길이에서 멈춘다.
    /// 요청만 생성하며 어떤 상태도 변조하지 않는다.
    /// </summary>
    public static class CharacterRopeSegmentPolicy
    {
        public static List<CharacterRopeSegmentRequest> GenerateSegmentRequests(
            int ropeId,
            WorldTileCoord originCell,
            in CharacterRopeSettings settings,
            ICharacterMapWorldQuery worldQuery)
        {
            var segments = new List<CharacterRopeSegmentRequest>(
                settings.MaxRopeLengthCells);

            if (worldQuery == null)
            {
                return segments;
            }

            for (int offset = 0; offset < settings.MaxRopeLengthCells; offset++)
            {
                WorldTileCoord candidate;
                if (!WorldCoordinateUtility.TryCreateWorldTile(
                    originCell.X, originCell.Y + offset, out candidate))
                {
                    break; // 월드 경계 — 생성 중단.
                }

                CharacterMapCellState state;
                if (worldQuery.TryGetCellState(candidate, out state) && state.IsSolid)
                {
                    break; // 고체 차단 셀 — 진입 전에 중단.
                }

                // 데이터 없는 셀은 empty/통과 가능으로 해석한다
                // (CharacterMapCellState.Empty 의미와 동일).
                segments.Add(new CharacterRopeSegmentRequest(
                    ropeId, candidate, offset));
            }

            return segments;
        }
    }
}
