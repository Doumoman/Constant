using System.Collections.Generic;
using UnityEngine;

namespace StarFetchingNight
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class StarNightPrologueBootstrap : MonoBehaviour
    {
        [SerializeField] private int fixedSeed = 173;
        [SerializeField] private bool useRandomSeed;

        private void Awake()
        {
            StarNightRunState run = StarNightRunState.Ensure();
            run.BeginNewRun(useRandomSeed ? null : fixedSeed);
            run.BeginChapter(CreateDefinition());
        }

        public static StarChapterDefinition CreateDefinition()
        {
            return new StarChapterDefinition
            {
                chapter = StarChapterId.Prologue,
                displayName = "프롤로그 · 별길을 잃은 밤",
                coreVerb = FableVerb.Resize,
                oneSentenceRule = "마루는 잃어버린 것을 집으로 돌려보내지만, 그 길의 별까지 물어온다.",
                requiredDepartureItems = 1,
                useGateLoop = false,
                objectiveNoun = "사건의 진실",
                objectiveInstruction = "표지판과 동행자를 확인하고 귀환떡 사건을 끝까지 목격하자.",
                guaranteedRooms = new List<string>
                {
                    "고장 난 여행 우주선", "비상 착륙장", "귀환떡 보관함",
                    "달 표지판", "마루 구조 구역", "사라진 길잡이별", "여행 티켓 승강장"
                }
            };
        }
    }
}
