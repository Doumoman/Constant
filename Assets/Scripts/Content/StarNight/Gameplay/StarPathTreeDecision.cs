using UnityEngine;

namespace StarFetchingNight
{
    public enum StarPathTreeDecisionMode
    {
        Stabilize,
        Overgrow
    }

    [DisallowMultipleComponent]
    public sealed class StarPathTreeDecision : MonoBehaviour, IStarNightInteractable
    {
        [SerializeField] private StarPathTreeDecisionMode mode;
        [SerializeField] private StarPathTreeController tree;

        public string Prompt => mode == StarPathTreeDecisionMode.Stabilize
            ? "별길 나무 가지를 잘라 안정된 항로 만들기"
            : "햇빛을 더 주어 거대한 지름길 만들기";

        public void Configure(StarPathTreeDecisionMode decisionMode, StarPathTreeController target)
        {
            mode = decisionMode;
            tree = target;
        }

        public void Interact(StarNightPlayerAgent player)
        {
            bool success = mode == StarPathTreeDecisionMode.Stabilize
                ? tree != null && tree.Stabilize()
                : tree != null && tree.Overgrow();
            StarNightHUD.Instance?.Toast(success
                ? (mode == StarPathTreeDecisionMode.Stabilize
                    ? "가지를 다듬었다. 느리지만 흔들리지 않는 별길이다."
                    : "가지가 천장까지 뻗었다. 빠른 지름길과 과열 위험이 함께 열렸다.")
                : "별길 나무가 아직 충분히 자라지 않았거나 남은 저장 햇빛이 없다.");
        }
    }
}
