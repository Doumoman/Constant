using System.Collections.Generic;
using StarNight.Character.Equipment;

namespace StarNight.Character.Live.Tools
{
    /// <summary>
    /// 폭발 1건의 라이브 지형 명령 값 객체 — 캐릭터 폭발 요청과 파괴 가능
    /// 셀 한정 지형 변경 요청 목록(CHAR05_01 정책 산출)을 그대로 운반한다.
    /// Tilemap/MAP 데이터 적용은 이후 배선 과제의 소비자 소관이다.
    /// </summary>
    public readonly struct CharacterLiveTerrainCommand
    {
        public CharacterLiveTerrainCommand(
            CharacterExplosionRequest explosion,
            IReadOnlyList<CharacterTerrainMutationRequest> mutations)
        {
            Explosion = explosion;
            Mutations = mutations;
        }

        public CharacterExplosionRequest Explosion { get; }
        public IReadOnlyList<CharacterTerrainMutationRequest> Mutations { get; }
    }
}
