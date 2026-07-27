using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>룸 유형 — 크기 규칙별 4종.</summary>
public enum ConstantRoomType
{
    MainH,   // 가로형 메인 30~33 x 18~20
    MainV,   // 세로형 메인 23~25 x 45~48
    CorrH,   // 가로 복도 15~20 x 10~15 (이벤트 장소)
    CorrV,   // 세로 복도 10~15 x 25~30 (낙하/복귀 통로)
}

/// <summary>
/// 룸 변형 라이브러리 — 룸 라이브러리 씬(RoomLibrary.unity)에 저장된
/// '모든 크기 경우의 수' 방들을 베이크한 에셋. 플레이 씬이 런타임에 꺼내
/// 퍼즐처럼 짜집기한다.
/// 편집 흐름: 라이브러리 씬에서 방 수정 → Tools/Constant/Room Library/Bake.
/// </summary>
public class RoomVariantLibrary : ScriptableObject
{
    public const string ResourcePath = "RoomVariantLibrary";

    [Serializable]
    public struct TileRec
    {
        public int x, y;        // 방 원점(좌하단) 기준 로컬 좌표
        public TileType type;
        public int variant;
    }

    [Serializable]
    public struct SpotRec
    {
        public int x, y;
        public bool isEnemy;    // false = 일반 콘텐츠 자리
    }

    [Serializable]
    public class RoomVariant
    {
        public string name;
        public ConstantRoomType type;
        public int width, height;
        public List<TileRec> tiles = new List<TileRec>();
        public List<SpotRec> spots = new List<SpotRec>();
    }

    public List<RoomVariant> variants = new List<RoomVariant>();

    public List<RoomVariant> ByType(ConstantRoomType type) =>
        variants.FindAll(v => v.type == type);

    public static RoomVariantLibrary Load() =>
        Resources.Load<RoomVariantLibrary>(ResourcePath);
}
